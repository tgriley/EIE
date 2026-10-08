#nullable enable
using DCM.Core.Input;
using DCM.Core.World;
using System;
using System.Collections.Generic;

namespace DCM.Core.Entities;

public class Player : IPlayerTarget, IHealable
{
    public const double FovPlane = 0.66;
    public const float HurtDuration = 0.35f;

    private const double WalkSpeed = 2.8;
    private const double TurnSpeed = 2.0;
    private const double MouseSens = 0.0015;
    private const double WallMargin = 0.25;
    private const double CameraRaisedSpeedFactor = 0.5;
    private const float DamageCooldown = 0.5f;
    private const int BumpDamage = 8;
    private const float SprintRecoverFraction = 0.25f;
    private const double DeathSpinSpeed = 4.0;

    public double PosX { get; private set; }
    public double PosY { get; private set; }
    public double DirX { get; private set; }
    public double DirY { get; private set; }
    public double PlaneX { get; private set; }
    public double PlaneY { get; private set; }

    public int Health { get; private set; }
    public int MaxHealth => _buffs.MaxHealth;
    public bool IsDead => Health <= 0;
    public float HurtTimer { get; private set; }
    public bool ReachedExit { get; private set; }
    public (double X, double Y)? KillerPos { get; private set; }

    public float SprintStamina { get; private set; }
    public float SprintMax => _buffs.MaxStamina;
    public float MovementRatio { get; private set; }

    public Action? OnDamaged { get; set; }
    public Action? OnDied    { get; set; }

    private readonly PlayerBuffs _buffs;
    private float _damageCooldown;
    private bool _sprintDepleted;

    public Player(double posX, double posY, double angle, int startHealth, PlayerBuffs buffs)
    {
        _buffs = buffs;
        PosX = posX;
        PosY = posY;
        Health = Math.Clamp(startHealth, 0, _buffs.MaxHealth);
        SprintStamina = _buffs.MaxStamina;
        SetAngle(angle);
    }

    private void SetAngle(double angle)
    {
        DirX = Math.Cos(angle);
        DirY = Math.Sin(angle);
        PlaneX = -Math.Sin(angle) * FovPlane;
        PlaneY = Math.Cos(angle) * FovPlane;
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

    public void Update(float dt, IMap map, PlayerInput input, IReadOnlyList<Enemy> enemies)
    {
        if (HurtTimer > 0) HurtTimer -= dt;
        if (_damageCooldown > 0) _damageCooldown -= dt;

        var isSprinting = input.Running && !_sprintDepleted;
        var baseSpeed = isSprinting ? _buffs.RunSpeed : WalkSpeed;
        var raiseFactor = input.CameraRaising ? CameraRaisedSpeedFactor : 1.0;
        var speed = baseSpeed * raiseFactor * dt;

        var forward = (input.MoveForward ? 1 : 0) - (input.MoveBack ? 1 : 0);
        var strafe  = (input.StrafeRight ? 1 : 0) - (input.StrafeLeft ? 1 : 0);
        var newX = PosX + (DirX * forward + PlaneX / FovPlane * strafe) * speed;
        var newY = PosY + (DirY * forward + PlaneY / FovPlane * strafe) * speed;

        var xClear = !map.IsWall((int)(newX + Math.Sign(newX - PosX) * WallMargin), (int)PosY);
        var xBlocker = xClear ? TouchingEnemy(newX, PosY, enemies) : null;
        if (xClear && xBlocker == null) PosX = newX;
        else if (xBlocker != null) TakeDamage(BumpDamage, xBlocker.PosX, xBlocker.PosY);

        var yClear = !map.IsWall((int)PosX, (int)(newY + Math.Sign(newY - PosY) * WallMargin));
        var yBlocker = yClear ? TouchingEnemy(PosX, newY, enemies) : null;
        if (yClear && yBlocker == null) PosY = newY;
        else if (yBlocker != null) TakeDamage(BumpDamage, yBlocker.PosX, yBlocker.PosY);

        var isMoving = input.MoveForward || input.MoveBack || input.StrafeLeft || input.StrafeRight;
        MovementRatio = isMoving ? (float)(baseSpeed / WalkSpeed * raiseFactor) : 0f;
        if (isSprinting && isMoving)
        {
            SprintStamina = Math.Max(0f, SprintStamina - dt);
            if (SprintStamina <= 0f) _sprintDepleted = true;
        }
        else
        {
            SprintStamina = Math.Min(SprintMax, SprintStamina + dt * _buffs.StaminaRegen);
            if (_sprintDepleted && SprintStamina >= SprintRecoverFraction * SprintMax) _sprintDepleted = false;
        }

        ReachedExit = map.IsExit((int)PosX, (int)PosY);

        if (input.TurnLeft) Rotate(-TurnSpeed * dt);
        if (input.TurnRight) Rotate(TurnSpeed * dt);
        if (input.MouseDeltaX != 0) Rotate(input.MouseDeltaX * MouseSens);
    }

    public void TakeDamage(int amount, double sourceX, double sourceY)
    {
        if (_damageCooldown > 0) return;
        Health = Math.Max(0, Health - amount);
        HurtTimer = HurtDuration;
        _damageCooldown = DamageCooldown;
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
        var turn = Math.Min(DeathSpinSpeed * dt, Math.Acos(Math.Clamp(dot, -1.0, 1.0)));
        Rotate(Math.Sign(cross) >= 0 ? turn : -turn);
    }

    public void Heal(int amount)
    {
        Health = Math.Min(_buffs.MaxHealth, Health + amount);
    }

    private static Enemy? TouchingEnemy(double x, double y, IReadOnlyList<Enemy> enemies)
    {
        foreach (var e in enemies)
            if (Collision.EntitiesOverlap(x, y, e.PosX, e.PosY)) return e;
        return null;
    }
}
