using DCM.Core.World;
using Microsoft.Xna.Framework;
using System;

namespace DCM.Core.Entities
{
    public enum EnemyState { Patrol, Chase, Attack, Dead }

    public class Enemy
    {
        public double    PosX      { get; private set; }
        public double    PosY      { get; private set; }
        public int       Health    { get; private set; } = 60;
        public bool      IsDead    => State == EnemyState.Dead;
        public EnemyState State    { get; private set; } = EnemyState.Patrol;
        public int              AnimFrame    { get; private set; } = 0;
        public bool             IsHurt       => _hurtTimer > 0;
        public double           DistSq       { get; set; }
        public EnemySpriteSheet SpriteSheet  { get; }
        private const float PatrolFps   = 3f;
        private const float ChaseFps    = 10f;

        private float  _attackCooldown = 0f;
        private float  _hurtTimer      = 0f;
        private float  _animTimer      = 0f;
        private float  _patrolTimer    = 0f;
        private double _patrolDirX     = 1;
        private double _patrolDirY     = 0;

        private const double ChaseRange  = 8.0;
        private const double AttackRange = 1.5;
        private const double MoveSpeed   = 1.5;
        private const double PatrolSpeed = 0.8;

        private static readonly Random _rng = new Random();

        public Enemy(int tileX, int tileY, EnemySpriteSheet spriteSheet)
        {
            PosX        = tileX + 0.5;
            PosY        = tileY + 0.5;
            SpriteSheet = spriteSheet;
            double a    = _rng.NextDouble() * Math.PI * 2;
            _patrolDirX = Math.Cos(a);
            _patrolDirY = Math.Sin(a);
        }

        public void Update(GameTime gameTime, IDamageable target, IMap map)
        {
            if (IsDead) return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_attackCooldown > 0) _attackCooldown -= dt;
            if (_hurtTimer      > 0) _hurtTimer      -= dt;

            double dx   = target.PosX - PosX;
            double dy   = target.PosY - PosY;
            double dist = Math.Sqrt(dx * dx + dy * dy);

            switch (State)
            {
                case EnemyState.Patrol:
                    DoPatrol(dt, map);
                    if (dist < ChaseRange && HasLineOfSight(target, map))
                        State = EnemyState.Chase;
                    break;

                case EnemyState.Chase:
                    if (dist < AttackRange)
                        State = EnemyState.Attack;
                    else if (dist > ChaseRange * 1.5)
                        State = EnemyState.Patrol;
                    else
                        MoveToward(dx / dist, dy / dist, MoveSpeed * dt, map);
                    break;

                case EnemyState.Attack:
                    if (dist > AttackRange * 1.2)
                        State = EnemyState.Chase;
                    else if (_attackCooldown <= 0)
                    {
                        target.TakeDamage(15);
                        _attackCooldown = 1.2f;
                    }
                    break;
            }

            float fps = State == EnemyState.Chase ? ChaseFps : PatrolFps;
            _animTimer += dt;
            if (_animTimer >= 1f / fps)
            {
                _animTimer -= 1f / fps;
                AnimFrame = (AnimFrame + 1) % SpriteSheet.FrameCount;
            }
        }

        private void DoPatrol(float dt, IMap map)
        {
            _patrolTimer -= dt;
            double speed = PatrolSpeed * dt;
            double nx = PosX + _patrolDirX * speed;
            double ny = PosY + _patrolDirY * speed;

            bool blocked = map.IsWall((int)nx, (int)PosY) || map.IsWall((int)PosX, (int)ny);
            if (blocked || _patrolTimer <= 0)
            {
                double a = _rng.NextDouble() * Math.PI * 2;
                _patrolDirX  = Math.Cos(a);
                _patrolDirY  = Math.Sin(a);
                _patrolTimer = (float)(_rng.NextDouble() * 2.0 + 1.0);
            }
            else
            {
                MoveToward(_patrolDirX, _patrolDirY, speed, map);
            }
        }

        private void MoveToward(double dx, double dy, double speed, IMap map)
        {
            double nx = PosX + dx * speed;
            double ny = PosY + dy * speed;
            const double margin = 0.3;

            if (!map.IsWall((int)(nx + Math.Sign(dx) * margin), (int)PosY)) PosX = nx;
            if (!map.IsWall((int)PosX, (int)(ny + Math.Sign(dy) * margin))) PosY = ny;
        }

        private bool HasLineOfSight(IDamageable target, IMap map)
        {
            double dx    = target.PosX - PosX;
            double dy    = target.PosY - PosY;
            double dist  = Math.Sqrt(dx * dx + dy * dy);
            int    steps = (int)(dist * 4);
            for (int i = 1; i < steps; i++)
            {
                double t  = i / (double)steps;
                int    cx = (int)(PosX + dx * t);
                int    cy = (int)(PosY + dy * t);
                if (map.IsWall(cx, cy)) return false;
            }
            return true;
        }

        public void Hit(int damage)
        {
            if (IsDead) return;
            Health    -= damage;
            _hurtTimer = 0.3f;
            State      = EnemyState.Chase;
            if (Health <= 0) { Health = 0; State = EnemyState.Dead; }
        }
    }
}
