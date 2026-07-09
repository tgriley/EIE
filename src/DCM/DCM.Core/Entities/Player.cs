#nullable enable
using DCM.Core.Input;
using DCM.Core.World;
using System;
using System.Collections.Generic;

namespace DCM.Core.Entities;

public class Player : ICamera, IDamageable, IHealable
{
    public double PosX { get; set; }
    public double PosY { get; set; }
    public double DirX { get; private set; }
    public double DirY { get; private set; }
    public double PlaneX { get; private set; }
    public double PlaneY { get; private set; }

    public int Health { get; private set; } = 100;
    public int MaxHealth => _buffs.MaxHealth;
    public bool IsDead => Health <= 0;
    public float HurtTimer { get; private set; } = 0f;
    public bool ReachedExit { get; private set; }
    public (double X, double Y)? KillerPos { get; private set; }

    public Action? OnDamaged { get; set; }
    public Action? OnDied    { get; set; }

    private float _damageCooldown = 0f;
    private readonly PlayerBuffs _buffs;

    public float SprintStamina { get; private set; }
    public float SprintMax => _buffs.MaxStamina;
    private bool _sprintDepleted;

    public const double CollisionRadius = 0.35;

    private const double MoveSpeed = 2.8;
    private const double TurnSpeed = 2.0;
    private const double MouseSens = 0.0015;

    public Player(double posX, double posY, double angle, int startHealth = 100, PlayerBuffs? buffs = null)
    {
        _buffs = buffs ?? new PlayerBuffs();
        PosX = posX;
        PosY = posY;
        Health = Math.Clamp(startHealth, 1, _buffs.MaxHealth);
        SprintStamina = _buffs.MaxStamina;
        SetAngle(angle);
    }

    private void SetAngle(double angle)
    {
        DirX = Math.Cos(angle);
        DirY = Math.Sin(angle);
        PlaneX = -Math.Sin(angle) * 0.66;
        PlaneY = Math.Cos(angle) * 0.66;
    }

    private void Rotate(double rad)
    {
        double cosR = Math.Cos(rad), sinR = Math.Sin(rad);
        var oldDirX = DirX;
        DirX = DirX * cosR - DirY * sinR;
        DirY = oldDirX * sinR + DirY * cosR;
        var oldPlaneX = PlaneX;
        PlaneX = PlaneX * cosR - PlaneY * sinR;
        PlaneY = oldPlaneX * sinR + PlaneY * cosR;
    }

    public void Update(float dt, IMap map, PlayerInput input, IReadOnlyList<Enemy>? enemies = null)
    {
        if (HurtTimer > 0) HurtTimer -= dt;
        if (_damageCooldown > 0) _damageCooldown -= dt;

        var isSprinting = input.Running && !_sprintDepleted;
        var baseSpeed = isSprinting ? _buffs.RunSpeed : MoveSpeed;
        var speed = baseSpeed * (input.CameraRaising ? 0.5 : 1.0) * dt;
        const double margin = 0.25;

        double newX = PosX, newY = PosY;

        if (input.MoveForward)
        {
            newX += DirX * speed;
            newY += DirY * speed;
        }

        if (input.MoveBack)
        {
            newX -= DirX * speed;
            newY -= DirY * speed;
        }

        if (input.StrafeLeft)
        {
            newX -= PlaneX / 0.66 * speed;
            newY -= PlaneY / 0.66 * speed;
        }

        if (input.StrafeRight)
        {
            newX += PlaneX / 0.66 * speed;
            newY += PlaneY / 0.66 * speed;
        }

        var xClear = !map.IsWall((int)(newX + Math.Sign(newX - PosX) * margin), (int)PosY);
        var xBlocker = xClear ? TouchingEnemy(newX, PosY, enemies) : null;
        if (xClear && xBlocker == null) PosX = newX;
        else if (xBlocker != null) TakeDamage(8, xBlocker.PosX, xBlocker.PosY);

        var yClear = !map.IsWall((int)PosX, (int)(newY + Math.Sign(newY - PosY) * margin));
        var yBlocker = yClear ? TouchingEnemy(PosX, newY, enemies) : null;
        if (yClear && yBlocker == null) PosY = newY;
        else if (yBlocker != null) TakeDamage(8, yBlocker.PosX, yBlocker.PosY);

        var isMoving = input.MoveForward || input.MoveBack || input.StrafeLeft || input.StrafeRight;
        if (isSprinting && isMoving)
        {
            SprintStamina = Math.Max(0f, SprintStamina - dt);
            if (SprintStamina <= 0f) _sprintDepleted = true;
        }
        else
        {
            SprintStamina = Math.Min(SprintMax, SprintStamina + dt * _buffs.StaminaRegen);
            if (_sprintDepleted && SprintStamina >= 0.25f * SprintMax) _sprintDepleted = false;
        }

        ReachedExit = map.IsExit((int)PosX, (int)PosY);

        if (input.TurnLeft) Rotate(-TurnSpeed * dt);
        if (input.TurnRight) Rotate(TurnSpeed * dt);
        if (input.MouseDeltaX != 0) Rotate(input.MouseDeltaX * MouseSens);
    }

    public void TakeDamage(int amount, double sourceX = 0, double sourceY = 0)
    {
        if (_damageCooldown > 0) return;
        Health = Math.Max(0, Health - amount);
        HurtTimer = 0.35f;
        _damageCooldown = 0.5f;
        if (Health <= 0) { KillerPos = (sourceX, sourceY); OnDied?.Invoke(); }
        else             OnDamaged?.Invoke();
    }

    public void UpdateDeathSpin(float dt)
    {
        if (KillerPos == null) return;
        var dx = KillerPos.Value.X - PosX;
        var dy = KillerPos.Value.Y - PosY;
        var dist = Math.Sqrt(dx * dx + dy * dy);
        if (dist < 0.001) return;
        var tx = dx / dist; var ty = dy / dist;
        var dot   = DirX * tx + DirY * ty;
        if (dot >= 0.9999) return;
        var cross = DirX * ty - DirY * tx;
        const double spinSpeed = 4.0;
        var turn = Math.Min(spinSpeed * dt, Math.Acos(Math.Clamp(dot, -1.0, 1.0)));
        Rotate(Math.Sign(cross) >= 0 ? turn : -turn);
    }

    public void Heal(int amount)
    {
        Health = Math.Min(_buffs.MaxHealth, Health + amount);
    }

    private static Enemy? TouchingEnemy(double x, double y, IReadOnlyList<Enemy>? enemies)
    {
        if (enemies == null) return null;
        const double minDist = CollisionRadius + Enemy.CollisionRadius;
        const double minDistSq = minDist * minDist;
        foreach (var e in enemies)
        {
            if (e.IsDead) continue;
            var dx = x - e.PosX;
            var dy = y - e.PosY;
            if (dx * dx + dy * dy < minDistSq) return e;
        }
        return null;
    }
}