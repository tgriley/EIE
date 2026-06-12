using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace DCM.Core.Entities
{
    public class Player
    {
        // World position
        public double PosX { get; set; }
        public double PosY { get; set; }

        // Direction vector (unit length)
        public double DirX { get; private set; }
        public double DirY { get; private set; }

        // Camera plane (perpendicular to dir, length controls FOV ~66°)
        public double PlaneX { get; private set; }
        public double PlaneY { get; private set; }

        public int Health { get; private set; } = 100;
        public bool IsDead => Health <= 0;

        // Hurt flash state
        public float HurtTimer { get; private set; } = 0f;
        private float _attackCooldown = 0f;
        private float _damageCooldown = 0f;

        // Track whether we just entered the exit tile
        public bool ReachedExit { get; private set; }

        private const double MoveSpeed  = 2.8;
        private const double RunSpeed   = 4.5;
        private const double TurnSpeed  = 2.0;
        private const double MouseSens  = 0.0015;

        private int _prevMouseX;
        private bool _firstFrame = true;

        public Player(double posX, double posY, double angle)
        {
            PosX = posX;
            PosY = posY;
            SetAngle(angle);
        }

        private void SetAngle(double angle)
        {
            DirX   =  Math.Cos(angle);
            DirY   =  Math.Sin(angle);
            PlaneX = -Math.Sin(angle) * 0.66;
            PlaneY =  Math.Cos(angle) * 0.66;
        }

        // Rotate direction and camera plane by radians
        private void Rotate(double rad)
        {
            double cosR = Math.Cos(rad);
            double sinR = Math.Sin(rad);

            double oldDirX = DirX;
            DirX   = DirX   * cosR - DirY   * sinR;
            DirY   = oldDirX * sinR + DirY   * cosR;

            double oldPlaneX = PlaneX;
            PlaneX = PlaneX * cosR - PlaneY * sinR;
            PlaneY = oldPlaneX * sinR + PlaneY * cosR;
        }

        public void Update(GameTime gameTime, World.Map map, Point windowCenter)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var kb    = Keyboard.GetState();
            var mouse = Mouse.GetState();

            // Timers
            if (HurtTimer > 0)      HurtTimer      -= dt;
            if (_attackCooldown > 0) _attackCooldown -= dt;
            if (_damageCooldown > 0) _damageCooldown -= dt;

            bool running = kb.IsKeyDown(Keys.LeftShift) || kb.IsKeyDown(Keys.RightShift);
            double speed = (running ? RunSpeed : MoveSpeed) * dt;

            // Movement
            double newX = PosX;
            double newY = PosY;
            const double margin = 0.25;

            if (kb.IsKeyDown(Keys.W) || kb.IsKeyDown(Keys.Up))
            {
                newX += DirX * speed;
                newY += DirY * speed;
            }
            if (kb.IsKeyDown(Keys.S) || kb.IsKeyDown(Keys.Down))
            {
                newX -= DirX * speed;
                newY -= DirY * speed;
            }
            if (kb.IsKeyDown(Keys.A))
            {
                newX -= PlaneX / 0.66 * speed;
                newY -= PlaneY / 0.66 * speed;
            }
            if (kb.IsKeyDown(Keys.D))
            {
                newX += PlaneX / 0.66 * speed;
                newY += PlaneY / 0.66 * speed;
            }

            // Slide collision: try X and Y independently
            if (!map.IsWall((int)(newX + Math.Sign(newX - PosX) * margin), (int)PosY))
                PosX = newX;
            if (!map.IsWall((int)PosX, (int)(newY + Math.Sign(newY - PosY) * margin)))
                PosY = newY;

            // Exit check
            ReachedExit = map.IsExit((int)PosX, (int)PosY);

            // Keyboard turning
            if (kb.IsKeyDown(Keys.Left))  Rotate(-TurnSpeed * dt);
            if (kb.IsKeyDown(Keys.Right)) Rotate( TurnSpeed * dt);

            // Mouse look
            if (!_firstFrame)
            {
                int dx = mouse.X - _prevMouseX;
                if (dx != 0) Rotate(dx * MouseSens);
            }
            // Re-center mouse
            Mouse.SetPosition(windowCenter.X, windowCenter.Y);
            _prevMouseX = windowCenter.X;
            _firstFrame = false;

        }

        public void TakeDamage(int amount)
        {
            if (_damageCooldown > 0) return;
            Health = Math.Max(0, Health - amount);
            HurtTimer = 0.35f;
            _damageCooldown = 0.5f;
        }

        public void Heal(int amount)
        {
            Health = Math.Min(100, Health + amount);
        }

        public bool TryAttack()
        {
            if (_attackCooldown > 0) return false;
            _attackCooldown = 0.5f;
            return true;
        }
    }
}
