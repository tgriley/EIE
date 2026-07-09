#nullable enable
using DCM.Core.World;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace DCM.Core.Entities;

public enum EnemyState
{
    Patrol,
    Chase,
    Attack,
    Flee,
    Dazed,
    Dead
}

public class Enemy : IBillboard
{
    public const int MaxHealth = 60;
    public const double CollisionRadius = 0.35;

    public double PosX { get; private set; }
    public double PosY { get; private set; }
    public int Health { get; private set; } = MaxHealth;
    public bool IsDead => State == EnemyState.Dead;
    public EnemyState State { get; private set; } = EnemyState.Patrol;
    public int AnimFrame { get; private set; } = 0;
    public bool IsHurt => _hurtTimer > 0;
    public EnemySpriteSheet SpriteSheet { get; }
    public EnemySpriteSheet HideSpriteSheet { get; }
    public bool CameraImmune { get; }

    private EnemySpriteSheet ActiveSheet => (State == EnemyState.Flee || State == EnemyState.Dazed) ? HideSpriteSheet : SpriteSheet;

    public Action? OnHurt { get; set; }
    public Action? OnDied { get; set; }

    private const float PatrolFps = 3f;
    private const float ChaseFps = 10f;

    private float _attackCooldown = 0f;
    private float _hurtTimer = 0f;
    private float _dazeTimer = 0f;
    private float _animTimer = 0f;
    private float _patrolTimer = 0f;
    private double _patrolDirX = 1;
    private double _patrolDirY = 0;

    public const double ChaseRange = 8.0;

    private const double AttackRange = 0.9;
    private const double MoveSpeed = 1.5;
    private const double PatrolSpeed = 0.8;
    private const double CameraImmuneSpeedFactor = 0.55;

    private readonly double _moveSpeed;
    private readonly double _patrolSpeed;
    private readonly float _stunDuration;

    private static readonly Random _rng = new();

    public Enemy(int tileX, int tileY, EnemySpriteSheet spriteSheet, EnemySpriteSheet hideSpriteSheet, bool cameraImmune = false, float stunDuration = 5f)
    {
        PosX = tileX + 0.5;
        PosY = tileY + 0.5;
        SpriteSheet = spriteSheet;
        HideSpriteSheet = hideSpriteSheet;
        CameraImmune = cameraImmune;
        _stunDuration = stunDuration;
        var speedFactor = cameraImmune ? CameraImmuneSpeedFactor : 1.0;
        _moveSpeed   = MoveSpeed * speedFactor;
        _patrolSpeed = PatrolSpeed * speedFactor;
        var a = _rng.NextDouble() * Math.PI * 2;
        _patrolDirX = Math.Cos(a);
        _patrolDirY = Math.Sin(a);
    }

    public void Update(GameTime gameTime, IDamageable target, ICamera camera, IMap map,
        IReadOnlyList<Enemy> others, bool cameraRaised = false, bool flashFired = false)
    {
        if (IsDead) return;

        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_attackCooldown > 0) _attackCooldown -= dt;
        if (_hurtTimer > 0) _hurtTimer -= dt;
        if (_dazeTimer > 0) _dazeTimer -= dt;

        var dx = target.PosX - PosX;
        var dy = target.PosY - PosY;
        var dist = Math.Sqrt(dx * dx + dy * dy);
        var inSightRange = !CameraImmune && dist < ChaseRange && IsLookedAt(camera) && HasLineOfSight(target, map);
        var scared = inSightRange && cameraRaised;
        var dazed  = inSightRange && flashFired;

        switch (State)
        {
            case EnemyState.Patrol:
                DoPatrol(dt, map, target, others);
                if (dist < ChaseRange && HasLineOfSight(target, map))
                {
                    if (dazed)       { State = EnemyState.Dazed; _dazeTimer = _stunDuration; AnimFrame = 0; }
                    else if (scared) { State = EnemyState.Flee; }
                    else               State = EnemyState.Chase;
                }
                break;

            case EnemyState.Chase:
                if (dazed)                        { State = EnemyState.Dazed; _dazeTimer = _stunDuration; AnimFrame = 0; }
                else if (scared)                  { State = EnemyState.Flee; }
                else if (dist < AttackRange)        State = EnemyState.Attack;
                else if (dist > ChaseRange * 1.5)   State = EnemyState.Patrol;
                else MoveToward(dx / dist, dy / dist, _moveSpeed * dt, map, target, others);
                break;

            case EnemyState.Attack:
                if (dazed)                        { State = EnemyState.Dazed; _dazeTimer = _stunDuration; AnimFrame = 0; }
                else if (scared)                  { State = EnemyState.Flee; }
                else if (dist > AttackRange * 1.2)  State = EnemyState.Chase;
                else if (_attackCooldown <= 0)
                {
                    target.TakeDamage(15, PosX, PosY);
                    _attackCooldown = 1.2f;
                }
                break;

            case EnemyState.Flee:
                if (dazed)        { State = EnemyState.Dazed; _dazeTimer = _stunDuration; AnimFrame = 0; }
                else if (!scared)   State = dist < ChaseRange && HasLineOfSight(target, map) ? EnemyState.Chase : EnemyState.Patrol;
                else if (dist > 0.01) MoveToward(-dx / dist, -dy / dist, _moveSpeed * dt, map, target, others);
                break;

            case EnemyState.Dazed:
                if (dazed) { _dazeTimer = _stunDuration; AnimFrame = 0; }
                else if (_dazeTimer <= 0) State = EnemyState.Patrol;
                break;
        }

        if (State != EnemyState.Dazed)
        {
            var fps = (State == EnemyState.Chase || State == EnemyState.Flee) ? ChaseFps : PatrolFps;
            _animTimer += dt;
            if (_animTimer >= 1f / fps)
            {
                _animTimer -= 1f / fps;
                AnimFrame = (AnimFrame + 1) % SpriteSheet.FrameCount;
            }
        }
    }

    public void Hit(int damage)
    {
        if (IsDead) return;
        Health -= damage;
        _hurtTimer = 0.3f;
        State = EnemyState.Chase;
        if (Health <= 0)
        {
            Health = 0;
            State = EnemyState.Dead;
            OnDied?.Invoke();
        }
        else
        {
            OnHurt?.Invoke();
        }
    }

    // ── IBillboard ───────────────────────────────────────────────────────────

    Color[]         IBillboard.Pixels        => ActiveSheet.Pixels;
    int             IBillboard.TexWidth      => ActiveSheet.FrameWidth;
    int             IBillboard.TexHeight     => ActiveSheet.Height;
    int             IBillboard.TexStride     => ActiveSheet.Width;
    int             IBillboard.PixelOffsetX  => AnimFrame * ActiveSheet.FrameWidth;
    bool            IBillboard.IsVisible     => !IsDead;
    bool            IBillboard.ApplyHurtTint => IsHurt;
    int             IBillboard.HeightDivisor => 1;
    double          IBillboard.VerticalShift => 0.0;
    (int, int)?     IBillboard.HealthBar     => (Health, MaxHealth);
    float?          IBillboard.OverheadCountdown => State == EnemyState.Dazed ? _dazeTimer : null;

    // ── Private helpers ──────────────────────────────────────────────────────

    private void DoPatrol(float dt, IMap map, IDamageable player, IReadOnlyList<Enemy> others)
    {
        _patrolTimer -= dt;
        var speed = _patrolSpeed * dt;
        var nx = PosX + _patrolDirX * speed;
        var ny = PosY + _patrolDirY * speed;

        var blocked = map.IsWall((int)nx, (int)PosY) || map.IsWall((int)PosX, (int)ny);
        if (blocked || _patrolTimer <= 0)
        {
            var a = _rng.NextDouble() * Math.PI * 2;
            _patrolDirX = Math.Cos(a);
            _patrolDirY = Math.Sin(a);
            _patrolTimer = (float)(_rng.NextDouble() * 2.0 + 1.0);
        }
        else
        {
            MoveToward(_patrolDirX, _patrolDirY, speed, map, player, others);
        }
    }

    private void MoveToward(double dx, double dy, double speed, IMap map,
        IDamageable player, IReadOnlyList<Enemy> others)
    {
        var nx = PosX + dx * speed;
        var ny = PosY + dy * speed;
        const double margin = 0.3;

        if (!map.IsWall((int)(nx + Math.Sign(dx) * margin), (int)PosY)
            && !TouchesAny(nx, PosY, player, others)) PosX = nx;
        if (!map.IsWall((int)PosX, (int)(ny + Math.Sign(dy) * margin))
            && !TouchesAny(PosX, ny, player, others)) PosY = ny;
    }

    private bool TouchesAny(double x, double y, IDamageable player, IReadOnlyList<Enemy> others)
    {
        const double minDist = CollisionRadius * 2;
        const double minDistSq = minDist * minDist;
        var pdx = x - player.PosX; var pdy = y - player.PosY;
        if (pdx * pdx + pdy * pdy < minDistSq) return true;
        foreach (var e in others)
        {
            if (ReferenceEquals(e, this) || e.IsDead) continue;
            var edx = x - e.PosX; var edy = y - e.PosY;
            if (edx * edx + edy * edy < minDistSq) return true;
        }
        return false;
    }

    private bool IsLookedAt(ICamera camera)
    {
        var dx = PosX - camera.PosX;
        var dy = PosY - camera.PosY;
        var dist = Math.Sqrt(dx * dx + dy * dy);
        if (dist < 0.001) return true;
        var dot = dx / dist * camera.DirX + dy / dist * camera.DirY;
        return dot > 0.85;
    }

    private bool HasLineOfSight(IDamageable target, IMap map)
    {
        var dx = target.PosX - PosX;
        var dy = target.PosY - PosY;
        var dist = Math.Sqrt(dx * dx + dy * dy);
        var steps = (int)(dist * 4);
        for (var i = 1; i < steps; i++)
        {
            var t = i / (double)steps;
            var cx = (int)(PosX + dx * t);
            var cy = (int)(PosY + dy * t);
            if (map.IsWall(cx, cy)) return false;
        }
        return true;
    }
}
