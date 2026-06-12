using DCM.Core.Entities;
using DCM.Core.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace DCM.Core.Rendering
{
    /// <summary>
    /// Classic DDA raycaster: perspective-correct textured walls, floor, and ceiling,
    /// distance fog, and Z-buffer sprite hit detection.
    /// Renders to an internal 640×360 frame buffer then scales 2× to 1280×720.
    /// </summary>
    public class RaycasterRenderer : IDisposable
    {
        // Internal (low-res) render dimensions — 2× pixel scaling gives retro feel
        public const int RW = 640;
        public const int RH = 360;

        private readonly GraphicsDevice _gd;
        private readonly SpriteBatch    _sb;

        // Frame buffer uploaded to GPU each frame
        private readonly Color[]   _fb = new Color[RW * RH];
        private readonly Texture2D _fbTex;

        // Z-buffer (perpendicular wall distance per column)
        private readonly double[] _zBuf = new double[RW];

        // Wall / floor / ceiling textures (pixel data from content pipeline)
        private readonly Color[] _wallTexPix;
        private readonly int     _wallTexW;
        private readonly int     _wallTexH;

        private readonly Color[] _floorTexPix;
        private readonly int     _floorTexW;
        private readonly int     _floorTexH;

        private readonly Color[] _ceilTexPix;
        private readonly int     _ceilTexW;
        private readonly int     _ceilTexH;

        // Enemy spritesheet (horizontal strip, 6 frames)
        private readonly Color[] _enemySheetPix;
        private readonly int     _enemySheetW;
        private readonly int     _enemySheetH;
        private readonly int     _enemyFrameCount;
        private readonly int     _enemyFrameW;  // width of one frame = sheetW / frameCount

        // 1×1 white pixel for rectangles (muzzle flash overlay)
        private readonly Texture2D _pixel;

        // Fog: beyond this perpDist everything is pitch black
        private const double FogStart = 1.5;
        private const double FogEnd   = 9.0;

        // Screen-space target (draw scaled frame buffer here)
        private Rectangle _destRect;

        // Shoot flash overlay
        public float MuzzleFlash { get; set; } = 0f;

        public RaycasterRenderer(GraphicsDevice gd,
                                 Color[] wallTexPix,   int wallTexW,   int wallTexH,
                                 Color[] floorTexPix,  int floorTexW,  int floorTexH,
                                 Color[] ceilTexPix,   int ceilTexW,   int ceilTexH,
                                 Color[] enemySheetPix, int enemySheetW, int enemySheetH,
                                 int enemyFrameCount)
        {
            _gd = gd;
            _sb = new SpriteBatch(gd);

            _wallTexPix  = wallTexPix;
            _wallTexW    = wallTexW;
            _wallTexH    = wallTexH;
            _floorTexPix = floorTexPix;
            _floorTexW   = floorTexW;
            _floorTexH   = floorTexH;
            _ceilTexPix  = ceilTexPix;
            _ceilTexW    = ceilTexW;
            _ceilTexH    = ceilTexH;

            _enemySheetPix   = enemySheetPix;
            _enemySheetW     = enemySheetW;
            _enemySheetH     = enemySheetH;
            _enemyFrameCount = enemyFrameCount;
            _enemyFrameW     = enemySheetW / enemyFrameCount;

            _fbTex = new Texture2D(gd, RW, RH);

            // 1×1 white pixel — used for the muzzle flash overlay
            _pixel = new Texture2D(gd, 1, 1);
            _pixel.SetData(new[] { Color.White });

            _destRect = new Rectangle(0, 0, RW * 2, RH * 2);
        }

        // ── Main render entry ─────────────────────────────────────────────────

        public void Render(GameTime gameTime, Player player, Map map, List<Enemy> enemies)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            DrawCeiling(player);
            DrawFloor(player);
            CastWalls(player, map);
            RenderEnemies(player, enemies);

            // Upload frame buffer
            _fbTex.SetData(_fb);

            // Draw scaled to screen
            _sb.Begin(SpriteSortMode.Deferred, BlendState.Opaque,
                      SamplerState.PointClamp, null, null);
            _sb.Draw(_fbTex, _destRect, Color.White);
            _sb.End();

            // Hurt flash overlay
            if (MuzzleFlash > 0)
            {
                _sb.Begin();
                byte alpha = (byte)(MuzzleFlash * 180);
                _sb.Draw(_pixel, new Rectangle(0, 0, RW * 2, RH * 2),
                         new Color(255, 200, 50, (int)alpha));
                _sb.End();
                MuzzleFlash = Math.Max(0, MuzzleFlash - dt * 6f);
            }
        }

        // ── Ceiling / floor projection ────────────────────────────────────────

        /// <summary>
        /// Perspective-correct ceiling texture projection (mirror of DrawFloor).
        /// For each row above the horizon, compute the real-world position and
        /// sample the ceiling texture with distance fog.
        /// </summary>
        private void DrawCeiling(Player player)
        {
            int half = RH / 2;

            double rayDirX0 = player.DirX - player.PlaneX;
            double rayDirY0 = player.DirY - player.PlaneY;
            double rayDirX1 = player.DirX + player.PlaneX;
            double rayDirY1 = player.DirY + player.PlaneY;

            for (int y = 0; y < half; y++)
            {
                int p = half - y; // rows above horizon (1 at horizon edge)
                if (p == 0) p = 1;

                double rowDist = (0.5 * RH) / p;

                double stepX = rowDist * (rayDirX1 - rayDirX0) / RW;
                double stepY = rowDist * (rayDirY1 - rayDirY0) / RW;

                double floorX = player.PosX + rowDist * rayDirX0;
                double floorY = player.PosY + rowDist * rayDirY0;

                float fog = (float)Math.Clamp(1.0 - (rowDist - FogStart) / (FogEnd - FogStart), 0, 1);

                int rowOff = y * RW;
                for (int x = 0; x < RW; x++)
                {
                    double fx = floorX - Math.Floor(floorX);
                    double fy = floorY - Math.Floor(floorY);

                    int tx = Math.Clamp((int)(fx * _ceilTexW), 0, _ceilTexW - 1);
                    int ty = Math.Clamp((int)(fy * _ceilTexH), 0, _ceilTexH - 1);

                    Color raw = _ceilTexPix[ty * _ceilTexW + tx];

                    byte r = (byte)(raw.R * fog);
                    byte g = (byte)(raw.G * fog);
                    byte b = (byte)(raw.B * fog);
                    _fb[rowOff + x] = new Color(r, g, b);

                    floorX += stepX;
                    floorY += stepY;
                }
            }
        }

        /// <summary>
        /// Perspective-correct floor texture projection.
        /// For each row below the horizon, compute the real-world floor position
        /// each pixel maps to, sample the floor texture, and apply distance fog.
        /// </summary>
        private void DrawFloor(Player player)
        {
            int half = RH / 2;

            // Camera-space edge ray directions
            double rayDirX0 = player.DirX - player.PlaneX;
            double rayDirY0 = player.DirY - player.PlaneY;
            double rayDirX1 = player.DirX + player.PlaneX;
            double rayDirY1 = player.DirY + player.PlaneY;

            for (int y = half; y < RH; y++)
            {
                int p = y - half; // rows below horizon (1 at horizon edge)
                if (p == 0) p = 1;

                // Horizontal distance from camera to floor at this row
                double rowDist = (0.5 * RH) / p;

                // World-space step per pixel in this row
                double stepX = rowDist * (rayDirX1 - rayDirX0) / RW;
                double stepY = rowDist * (rayDirY1 - rayDirY0) / RW;

                // World-space position of the leftmost pixel
                double floorX = player.PosX + rowDist * rayDirX0;
                double floorY = player.PosY + rowDist * rayDirY0;

                // Distance fog (same range as wall fog for consistency)
                float fog = (float)Math.Clamp(1.0 - (rowDist - FogStart) / (FogEnd - FogStart), 0, 1);

                int rowOff = y * RW;
                for (int x = 0; x < RW; x++)
                {
                    // Fractional tile position → texture coordinates
                    double fx = floorX - Math.Floor(floorX);
                    double fy = floorY - Math.Floor(floorY);

                    int tx = Math.Clamp((int)(fx * _floorTexW), 0, _floorTexW - 1);
                    int ty = Math.Clamp((int)(fy * _floorTexH), 0, _floorTexH - 1);

                    Color raw = _floorTexPix[ty * _floorTexW + tx];

                    byte r = (byte)(raw.R * fog);
                    byte g = (byte)(raw.G * fog);
                    byte b = (byte)(raw.B * fog);
                    _fb[rowOff + x] = new Color(r, g, b);

                    floorX += stepX;
                    floorY += stepY;
                }
            }
        }

        // ── DDA wall casting ──────────────────────────────────────────────────

        private void CastWalls(Player player, Map map)
        {
            int texSzW = _wallTexW;
            int texSzH = _wallTexH;

            for (int col = 0; col < RW; col++)
            {
                // Camera-space X: -1 (left) to +1 (right)
                double camX = 2.0 * col / RW - 1.0;

                double rayDX = player.DirX + player.PlaneX * camX;
                double rayDY = player.DirY + player.PlaneY * camX;

                int mapX = (int)player.PosX;
                int mapY = (int)player.PosY;

                // Avoid division by zero
                double deltaDX = rayDX == 0 ? double.MaxValue : Math.Abs(1.0 / rayDX);
                double deltaDY = rayDY == 0 ? double.MaxValue : Math.Abs(1.0 / rayDY);

                int stepX, stepY;
                double sideX, sideY;

                if (rayDX < 0) { stepX = -1; sideX = (player.PosX - mapX) * deltaDX; }
                else           { stepX =  1; sideX = (mapX + 1.0 - player.PosX) * deltaDX; }

                if (rayDY < 0) { stepY = -1; sideY = (player.PosY - mapY) * deltaDY; }
                else           { stepY =  1; sideY = (mapY + 1.0 - player.PosY) * deltaDY; }

                // DDA loop
                int side = 0;
                int hitTile = 0;
                for (int safety = 0; safety < 64; safety++)
                {
                    if (sideX < sideY) { sideX += deltaDX; mapX += stepX; side = 0; }
                    else               { sideY += deltaDY; mapY += stepY; side = 1; }
                    hitTile = map.GetTile(mapX, mapY);
                    if (hitTile != 0) break;
                }

                double perpDist = side == 0
                    ? sideX - deltaDX
                    : sideY - deltaDY;
                if (perpDist < 0.001) perpDist = 0.001;

                _zBuf[col] = perpDist;

                int wallH   = (int)(RH / perpDist);
                int drawTop = Math.Max(0, RH / 2 - wallH / 2);
                int drawBot = Math.Min(RH - 1, RH / 2 + wallH / 2);

                // Texture X coordinate (where did the ray hit the wall face?)
                double wallX = side == 0
                    ? player.PosY + perpDist * rayDY
                    : player.PosX + perpDist * rayDX;
                wallX -= Math.Floor(wallX);
                int texX = (int)(wallX * texSzW);
                if ((side == 0 && rayDX > 0) || (side == 1 && rayDY < 0))
                    texX = texSzW - texX - 1;
                texX = Math.Clamp(texX, 0, texSzW - 1);

                // Fog factor
                float fog = (float)Math.Clamp(1.0 - (perpDist - FogStart) / (FogEnd - FogStart), 0, 1);
                // Y-side walls are darker (classic Wolfenstein shadow trick)
                float sideFactor = side == 0 ? 1.0f : 0.6f;
                float bright = fog * sideFactor;

                // Torchlight: warm glow near player
                float warmth = (float)Math.Clamp(0.3 - perpDist * 0.04, 0, 0.3f);

                // Draw textured wall column
                double step = (double)texSzH / wallH;
                double texY = drawTop > RH / 2 - wallH / 2
                    ? (drawTop - (RH / 2 - wallH / 2)) * step
                    : 0;

                for (int y = drawTop; y <= drawBot; y++)
                {
                    int ty = Math.Clamp((int)texY, 0, texSzH - 1);
                    Color raw = _wallTexPix[ty * texSzW + texX];

                    byte r = (byte)Math.Clamp(raw.R * bright + warmth * 200, 0, 255);
                    byte g = (byte)Math.Clamp(raw.G * bright + warmth * 120, 0, 255);
                    byte b = (byte)Math.Clamp(raw.B * bright + warmth * 30, 0, 255);

                    _fb[y * RW + col] = new Color(r, g, b);
                    texY += step;
                }
            }
        }

        // ── Enemy sprite rendering ────────────────────────────────────────────

        private void RenderEnemies(Player player, List<Enemy> enemies)
        {
            // Sort farthest-first so nearer enemies overdraw distant ones
            foreach (var e in enemies)
                e.DistSq = (e.PosX - player.PosX) * (e.PosX - player.PosX) +
                           (e.PosY - player.PosY) * (e.PosY - player.PosY);

            var sorted = new List<Enemy>(enemies);
            sorted.Sort((a, b) => b.DistSq.CompareTo(a.DistSq));

            foreach (var e in sorted)
            {
                if (e.IsDead) continue;
                DrawEnemy(player, e);
            }
        }

        private void DrawEnemy(Player player, Enemy enemy)
        {
            double relX = enemy.PosX - player.PosX;
            double relY = enemy.PosY - player.PosY;

            // Camera-space transform (inverse of view matrix)
            double invDet = 1.0 / (player.PlaneX * player.DirY - player.DirX * player.PlaneY);
            double transX = invDet * ( player.DirY * relX - player.DirX * relY);
            double transY = invDet * (-player.PlaneY * relX + player.PlaneX * relY);

            if (transY <= 0.05) return; // behind player

            int screenX = (int)((RW / 2) * (1.0 + transX / transY));
            int screenH = Math.Abs((int)(RH / transY));
            int screenW = (int)(screenH * _enemyFrameW / (double)_enemySheetH);

            int drawTopY = RH / 2 - screenH / 2;
            int drawBotY = RH / 2 + screenH / 2;
            int drawLeft = screenX - screenW / 2;
            int drawRight = screenX + screenW / 2;

            float fog    = (float)Math.Clamp(1.0 - (transY - FogStart) / (FogEnd - FogStart), 0, 1);
            float warmth = (float)Math.Clamp(0.3 - transY * 0.04, 0, 0.3f);

            // Frame offset into the horizontal spritesheet
            int frameOffX = enemy.AnimFrame * _enemyFrameW;

            for (int col = Math.Max(0, drawLeft); col < Math.Min(RW, drawRight); col++)
            {
                if (_zBuf[col] < transY) continue; // depth test

                int texX = frameOffX + (int)((col - drawLeft) * _enemyFrameW / (double)screenW);
                texX = Math.Clamp(texX, frameOffX, frameOffX + _enemyFrameW - 1);

                for (int row = Math.Max(0, drawTopY); row < Math.Min(RH, drawBotY); row++)
                {
                    int texY = (int)((row - drawTopY) * _enemySheetH / (double)screenH);
                    texY = Math.Clamp(texY, 0, _enemySheetH - 1);

                    Color c = _enemySheetPix[texY * _enemySheetW + texX];
                    if (c.A < 10) continue; // transparent pixel

                    byte r = (byte)Math.Clamp(c.R * fog + warmth * 180, 0, 255);
                    byte g = (byte)Math.Clamp(c.G * fog + warmth *  90, 0, 255);
                    byte b = (byte)Math.Clamp(c.B * fog,                0, 255);

                    // Red tint when hurt
                    if (enemy.IsHurt)
                    {
                        r = (byte)Math.Min(255, r + 80);
                        g = (byte)Math.Max(0,   g - 40);
                        b = (byte)Math.Max(0,   b - 40);
                    }

                    _fb[row * RW + col] = new Color(r, g, b, c.A);
                }
            }
        }

        // ── Shooting hit-test ─────────────────────────────────────────────────

        /// <summary>
        /// Returns the closest enemy that the center crosshair ray hits,
        /// within the given range. Call this when the player fires.
        /// </summary>
        public Enemy? RaycastShoot(Player player, List<Enemy> enemies, double maxRange = 6.0)
        {
            // Find which enemy is closest to the center column
            Enemy? best = null;
            double bestDist = maxRange;

            foreach (var e in enemies)
            {
                if (e.IsDead) continue;

                double relX = e.PosX - player.PosX;
                double relY = e.PosY - player.PosY;

                double invDet = 1.0 / (player.PlaneX * player.DirY - player.DirX * player.PlaneY);
                double transX = invDet * ( player.DirY * relX - player.DirX * relY);
                double transY = invDet * (-player.PlaneY * relX + player.PlaneX * relY);

                if (transY <= 0.1 || transY > maxRange) continue;

                // Sprite screen center X
                int screenX = (int)((RW / 2) * (1.0 + transX / transY));
                int screenH = Math.Abs((int)(RH / transY));
                int halfW   = screenH / 4;

                // Is center column within sprite's screen X range?
                if (Math.Abs(screenX - RW / 2) < halfW && transY < bestDist &&
                    _zBuf[RW / 2] >= transY)
                {
                    bestDist = transY;
                    best = e;
                }
            }

            return best;
        }

        public void Dispose()
        {
            _sb.Dispose();
            _fbTex.Dispose();
            _pixel.Dispose();
        }
    }
}
