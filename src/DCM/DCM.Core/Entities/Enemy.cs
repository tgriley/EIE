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
    Dazed
}

public class Enemy : IBillboard
{
    public const double ChaseRange = 8.0;

    private const float PatrolFps = 3f;
    private const float ChaseFps = 10f;
    private const double AttackRange = 0.9;
    private const double MoveSpeed = 1.5;
    private const double PatrolSpeed = 0.8;
    private const double CameraImmuneSpeedFactor = 0.55;
    private const int AttackDamage = 15;
    private const float AttackCooldown = 1.2f;
    private const double LookAtDot = 0.85;
    private const double LoseInterestFactor = 1.5;
    private const double AttackBreakFactor = 1.2;
    private const double WallMargin = 0.3;

    public double PosX { get; private set; }
    public double PosY { get; private set; }
    public EnemyState State { get; private set; } = EnemyState.Patrol;
    public int AnimFrame { get; private set; }
    public EnemySpriteSheet SpriteSheet { get; }
    public EnemySpriteSheet HideSpriteSheet { get; }
    public bool CameraImmune { get; }

    private EnemySpriteSheet ActiveSheet => State is EnemyState.Flee or EnemyState.Dazed ? HideSpriteSheet : SpriteSheet;

    private readonly double _moveSpeed;
    private readonly double _patrolSpeed;
    private readonly float _stunDuration;
    private float _attackCooldown;
    private float _dazeTimer;
    private float _animTimer;
    private float _patrolTimer;
    private double _patrolDirX;
    private double _patrolDirY;

    private static readonly Random _rng = new();

    public Enemy(int tileX, int tileY, EnemySpriteSheet spriteSheet, EnemySpriteSheet hideSpriteSheet,
        bool cameraImmune, float stunDuration)
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
        PickPatrolDirection();
    }

    public void Update(GameTime gameTime, IPlayerTarget player, IMap map,
        IReadOnlyList<Enemy> others, bool cameraRaised, bool flashFired)
    {
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_attackCooldown > 0) _attackCooldown -= dt;
        if (_dazeTimer > 0) _dazeTimer -= dt;

        var dx = player.PosX - PosX;
        var dy = player.PosY - PosY;
        var dist = Math.Sqrt(dx * dx + dy * dy);
        var canSee = dist < ChaseRange && HasLineOfSight(player, map);
        var inFlashRange = !CameraImmune && canSee && IsLookedAt(player);
        var scared = inFlashRange && cameraRaised;
        var dazed  = inFlashRange && flashFired;

        if (dazed)
        {
            EnterDazed();
        }
        else
        {
            switch (State)
            {
                case EnemyState.Patrol:
                    DoPatrol(dt, map, player, others);
                    if (canSee) State = scared ? EnemyState.Flee : EnemyState.Chase;
                    break;

                case EnemyState.Chase:
                    if (scared)                                    State = EnemyState.Flee;
                    else if (dist < AttackRange)                   State = EnemyState.Attack;
                    else if (dist > ChaseRange * LoseInterestFactor) State = EnemyState.Patrol;
                    else MoveToward(dx / dist, dy / dist, _moveSpeed * dt, map, player, others);
                    break;

                case EnemyState.Attack:
                    if (scared)                                    State = EnemyState.Flee;
                    else if (dist > AttackRange * AttackBreakFactor) State = EnemyState.Chase;
                    else if (_attackCooldown <= 0)
                    {
                        player.TakeDamage(AttackDamage, PosX, PosY);
                        _attackCooldown = AttackCooldown;
                    }
                    break;

                case EnemyState.Flee:
                    if (!scared)          State = canSee ? EnemyState.Chase : EnemyState.Patrol;
                    else if (dist > 0.01) MoveToward(-dx / dist, -dy / dist, _moveSpeed * dt, map, player, others);
                    break;

                case EnemyState.Dazed:
                    if (_dazeTimer <= 0) State = EnemyState.Patrol;
                    break;
            }
        }

        if (State != EnemyState.Dazed) Animate(dt);
    }

    private void EnterDazed()
    {
        State = EnemyState.Dazed;
        _dazeTimer = _stunDuration;
        AnimFrame = 0;
    }

    private void Animate(float dt)
    {
        var frameTime = 1f / (State is EnemyState.Chase or EnemyState.Flee ? ChaseFps : PatrolFps);
        _animTimer += dt;
        if (_animTimer < frameTime) return;
        _animTimer -= frameTime;
        AnimFrame = (AnimFrame + 1) % SpriteSheet.FrameCount;
    }

    // ── IBillboard ───────────────────────────────────────────────────────────

    Color[]         IBillboard.Pixels        => ActiveSheet.Pixels;
    int             IBillboard.TexWidth      => ActiveSheet.FrameWidth;
    int             IBillboard.TexHeight     => ActiveSheet.Height;
    int             IBillboard.TexStride     => ActiveSheet.Width;
    int             IBillboard.PixelOffsetX  => AnimFrame * ActiveSheet.FrameWidth;
    bool            IBillboard.IsVisible     => true;
    int             IBillboard.HeightDivisor => 1;
    double          IBillboard.VerticalShift => 0.0;
    float?          IBillboard.OverheadCountdown => State == EnemyState.Dazed ? _dazeTimer : null;

    // ── Private helpers ──────────────────────────────────────────────────────

    private void PickPatrolDirection()
    {
        var a = _rng.NextDouble() * Math.PI * 2;
        _patrolDirX = Math.Cos(a);
        _patrolDirY = Math.Sin(a);
    }

    private void DoPatrol(float dt, IMap map, IPlayerTarget player, IReadOnlyList<Enemy> others)
    {
        _patrolTimer -= dt;
        var speed = _patrolSpeed * dt;
        var nx = PosX + _patrolDirX * speed;
        var ny = PosY + _patrolDirY * speed;

        var blocked = map.IsWall((int)nx, (int)PosY) || map.IsWall((int)PosX, (int)ny);
        if (blocked || _patrolTimer <= 0)
        {
            PickPatrolDirection();
            _patrolTimer = (float)(_rng.NextDouble() * 2.0 + 1.0);
        }
        else
        {
            MoveToward(_patrolDirX, _patrolDirY, speed, map, player, others);
        }
    }

    private void MoveToward(double dx, double dy, double speed, IMap map,
        IPlayerTarget player, IReadOnlyList<Enemy> others)
    {
        var nx = PosX + dx * speed;
        var ny = PosY + dy * speed;

        if (!map.IsWall((int)(nx + Math.Sign(dx) * WallMargin), (int)PosY)
            && !TouchesAny(nx, PosY, player, others)) PosX = nx;
        if (!map.IsWall((int)PosX, (int)(ny + Math.Sign(dy) * WallMargin))
            && !TouchesAny(PosX, ny, player, others)) PosY = ny;
    }

    private bool TouchesAny(double x, double y, IPlayerTarget player, IReadOnlyList<Enemy> others)
    {
        if (Collision.EntitiesOverlap(x, y, player.PosX, player.PosY)) return true;
        foreach (var e in others)
            if (!ReferenceEquals(e, this) && Collision.EntitiesOverlap(x, y, e.PosX, e.PosY)) return true;
        return false;
    }

    private bool IsLookedAt(ICamera camera)
    {
        var dx = PosX - camera.PosX;
        var dy = PosY - camera.PosY;
        var dist = Math.Sqrt(dx * dx + dy * dy);
        if (dist < 0.001) return true;
        var dot = dx / dist * camera.DirX + dy / dist * camera.DirY;
        return dot > LookAtDot;
    }

    private bool HasLineOfSight(ICamera target, IMap map)
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
