#nullable enable
using DCM.Core.Input;
using DCM.Core.World;
using System;

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
    public bool IsDead => Health <= 0;
    public float HurtTimer { get; private set; } = 0f;
    public bool ReachedExit { get; private set; }

    public Action? OnDamaged { get; set; }
    public Action? OnDied    { get; set; }

    private float _damageCooldown = 0f;

    public float SprintStamina { get; private set; } = 1f;
    private bool _sprintDepleted;

    private const double MoveSpeed = 2.8;
    private const double RunSpeed = 4.5;
    private const double TurnSpeed = 2.0;
    private const double MouseSens = 0.0015;
    private const float SprintMax        = 1f;
    private const float SprintRecharge   = 1f / 5f;

    public Player(double posX, double posY, double angle, int startHealth = 100)
    {
        PosX = posX;
        PosY = posY;
        Health = Math.Clamp(startHealth, 1, 100);
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

    public void Update(float dt, IMap map, PlayerInput input)
    {
        if (HurtTimer > 0) HurtTimer -= dt;
        if (_damageCooldown > 0) _damageCooldown -= dt;

        var isSprinting = input.Running && !_sprintDepleted;
        var baseSpeed = isSprinting ? RunSpeed : MoveSpeed;
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

        if (!map.IsWall((int)(newX + Math.Sign(newX - PosX) * margin), (int)PosY)) PosX = newX;
        if (!map.IsWall((int)PosX, (int)(newY + Math.Sign(newY - PosY) * margin))) PosY = newY;

        var isMoving = input.MoveForward || input.MoveBack || input.StrafeLeft || input.StrafeRight;
        if (isSprinting && isMoving)
        {
            SprintStamina = Math.Max(0f, SprintStamina - dt / SprintMax);
            if (SprintStamina <= 0f) _sprintDepleted = true;
        }
        else
        {
            SprintStamina = Math.Min(1f, SprintStamina + dt * SprintRecharge);
            if (_sprintDepleted && SprintStamina >= 0.25f) _sprintDepleted = false;
        }

        ReachedExit = map.IsExit((int)PosX, (int)PosY);

        if (input.TurnLeft) Rotate(-TurnSpeed * dt);
        if (input.TurnRight) Rotate(TurnSpeed * dt);
        if (input.MouseDeltaX != 0) Rotate(input.MouseDeltaX * MouseSens);
    }

    public void TakeDamage(int amount)
    {
        if (_damageCooldown > 0) return;
        Health = Math.Max(0, Health - amount);
        HurtTimer = 0.35f;
        _damageCooldown = 0.5f;
        if (Health <= 0) OnDied?.Invoke();
        else             OnDamaged?.Invoke();
    }

    public void Heal(int amount)
    {
        Health = Math.Min(100, Health + amount);
    }

}