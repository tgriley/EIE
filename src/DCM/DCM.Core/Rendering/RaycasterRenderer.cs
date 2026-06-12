#nullable enable
using DCM.Core.Entities;
using DCM.Core.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace DCM.Core.Rendering;

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
    private readonly SpriteBatch _sb;

    // Frame buffer uploaded to GPU each frame
    private readonly Color[] _fb = new Color[RW * RH];
    private readonly Texture2D _fbTex;

    // Z-buffer (perpendicular wall distance per column)
    private readonly double[] _zBuf = new double[RW];

    // Wall / floor / ceiling textures (pixel data from content pipeline)
    private readonly Color[] _wallTexPix;
    private readonly int _wallTexW;
    private readonly int _wallTexH;

    private readonly Color[] _floorTexPix;
    private readonly int _floorTexW;
    private readonly int _floorTexH;

    private readonly Color[] _ceilTexPix;
    private readonly int _ceilTexW;
    private readonly int _ceilTexH;

    // 1×1 white pixel for rectangles (muzzle flash overlay)
    private readonly Texture2D _pixel;

    // Fog: beyond this perpDist everything is pitch black
    private const double FogStart = 1.5;
    private const double FogEnd = 9.0;

    // Screen-space target (draw scaled frame buffer here)
    private Rectangle _destRect;

    // Shoot flash overlay
    public float MuzzleFlash { get; set; } = 0f;

    public RaycasterRenderer(GraphicsDevice gd,
        Color[] wallTexPix, int wallTexW, int wallTexH,
        Color[] floorTexPix, int floorTexW, int floorTexH,
        Color[] ceilTexPix, int ceilTexW, int ceilTexH)
    {
        _gd = gd;
        _sb = new SpriteBatch(gd);

        _wallTexPix = wallTexPix;
        _wallTexW = wallTexW;
        _wallTexH = wallTexH;
        _floorTexPix = floorTexPix;
        _floorTexW = floorTexW;
        _floorTexH = floorTexH;
        _ceilTexPix = ceilTexPix;
        _ceilTexW = ceilTexW;
        _ceilTexH = ceilTexH;

        _fbTex = new Texture2D(gd, RW, RH);

        // 1×1 white pixel — used for the muzzle flash overlay
        _pixel = new Texture2D(gd, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _destRect = new Rectangle(0, 0, RW * 2, RH * 2);
    }

    // ── Main render entry ─────────────────────────────────────────────────

    public void Render(GameTime gameTime, ICamera camera, IMap map, List<Enemy> enemies)
    {
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        DrawCeiling(camera);
        DrawFloor(camera);
        CastWalls(camera, map);
        RenderEnemies(camera, enemies);

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
            var alpha = (byte)(MuzzleFlash * 180);
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
    private void DrawCeiling(ICamera camera)
    {
        var half = RH / 2;

        var rayDirX0 = camera.DirX - camera.PlaneX;
        var rayDirY0 = camera.DirY - camera.PlaneY;
        var rayDirX1 = camera.DirX + camera.PlaneX;
        var rayDirY1 = camera.DirY + camera.PlaneY;

        for (var y = 0; y < half; y++)
        {
            var p = half - y; // rows above horizon (1 at horizon edge)
            if (p == 0) p = 1;

            var rowDist = 0.5 * RH / p;

            var stepX = rowDist * (rayDirX1 - rayDirX0) / RW;
            var stepY = rowDist * (rayDirY1 - rayDirY0) / RW;

            var floorX = camera.PosX + rowDist * rayDirX0;
            var floorY = camera.PosY + rowDist * rayDirY0;

            var fog = (float)Math.Clamp(1.0 - (rowDist - FogStart) / (FogEnd - FogStart), 0, 1);

            var rowOff = y * RW;
            for (var x = 0; x < RW; x++)
            {
                var fx = floorX - Math.Floor(floorX);
                var fy = floorY - Math.Floor(floorY);

                var tx = Math.Clamp((int)(fx * _ceilTexW), 0, _ceilTexW - 1);
                var ty = Math.Clamp((int)(fy * _ceilTexH), 0, _ceilTexH - 1);

                var raw = _ceilTexPix[ty * _ceilTexW + tx];

                var r = (byte)(raw.R * fog);
                var g = (byte)(raw.G * fog);
                var b = (byte)(raw.B * fog);
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
    private void DrawFloor(ICamera camera)
    {
        var half = RH / 2;

        // Camera-space edge ray directions
        var rayDirX0 = camera.DirX - camera.PlaneX;
        var rayDirY0 = camera.DirY - camera.PlaneY;
        var rayDirX1 = camera.DirX + camera.PlaneX;
        var rayDirY1 = camera.DirY + camera.PlaneY;

        for (var y = half; y < RH; y++)
        {
            var p = y - half; // rows below horizon (1 at horizon edge)
            if (p == 0) p = 1;

            // Horizontal distance from camera to floor at this row
            var rowDist = 0.5 * RH / p;

            // World-space step per pixel in this row
            var stepX = rowDist * (rayDirX1 - rayDirX0) / RW;
            var stepY = rowDist * (rayDirY1 - rayDirY0) / RW;

            // World-space position of the leftmost pixel
            var floorX = camera.PosX + rowDist * rayDirX0;
            var floorY = camera.PosY + rowDist * rayDirY0;

            // Distance fog (same range as wall fog for consistency)
            var fog = (float)Math.Clamp(1.0 - (rowDist - FogStart) / (FogEnd - FogStart), 0, 1);

            var rowOff = y * RW;
            for (var x = 0; x < RW; x++)
            {
                // Fractional tile position → texture coordinates
                var fx = floorX - Math.Floor(floorX);
                var fy = floorY - Math.Floor(floorY);

                var tx = Math.Clamp((int)(fx * _floorTexW), 0, _floorTexW - 1);
                var ty = Math.Clamp((int)(fy * _floorTexH), 0, _floorTexH - 1);

                var raw = _floorTexPix[ty * _floorTexW + tx];

                var r = (byte)(raw.R * fog);
                var g = (byte)(raw.G * fog);
                var b = (byte)(raw.B * fog);
                _fb[rowOff + x] = new Color(r, g, b);

                floorX += stepX;
                floorY += stepY;
            }
        }
    }

    // ── DDA wall casting ──────────────────────────────────────────────────

    private void CastWalls(ICamera camera, IMap map)
    {
        var texSzW = _wallTexW;
        var texSzH = _wallTexH;

        for (var col = 0; col < RW; col++)
        {
            // Camera-space X: -1 (left) to +1 (right)
            var camX = 2.0 * col / RW - 1.0;

            var rayDX = camera.DirX + camera.PlaneX * camX;
            var rayDY = camera.DirY + camera.PlaneY * camX;

            var mapX = (int)camera.PosX;
            var mapY = (int)camera.PosY;

            // Avoid division by zero
            var deltaDX = rayDX == 0 ? double.MaxValue : Math.Abs(1.0 / rayDX);
            var deltaDY = rayDY == 0 ? double.MaxValue : Math.Abs(1.0 / rayDY);

            int stepX, stepY;
            double sideX, sideY;

            if (rayDX < 0)
            {
                stepX = -1;
                sideX = (camera.PosX - mapX) * deltaDX;
            }
            else
            {
                stepX = 1;
                sideX = (mapX + 1.0 - camera.PosX) * deltaDX;
            }

            if (rayDY < 0)
            {
                stepY = -1;
                sideY = (camera.PosY - mapY) * deltaDY;
            }
            else
            {
                stepY = 1;
                sideY = (mapY + 1.0 - camera.PosY) * deltaDY;
            }

            // DDA loop
            var side = 0;
            var hitTile = 0;
            for (var safety = 0; safety < 64; safety++)
            {
                if (sideX < sideY)
                {
                    sideX += deltaDX;
                    mapX += stepX;
                    side = 0;
                }
                else
                {
                    sideY += deltaDY;
                    mapY += stepY;
                    side = 1;
                }

                hitTile = map.GetTile(mapX, mapY);
                if (hitTile != 0) break;
            }

            var perpDist = side == 0
                ? sideX - deltaDX
                : sideY - deltaDY;
            if (perpDist < 0.001) perpDist = 0.001;

            _zBuf[col] = perpDist;

            var wallH = (int)(RH / perpDist);
            var drawTop = Math.Max(0, RH / 2 - wallH / 2);
            var drawBot = Math.Min(RH - 1, RH / 2 + wallH / 2);

            // Texture X coordinate (where did the ray hit the wall face?)
            var wallX = side == 0
                ? camera.PosY + perpDist * rayDY
                : camera.PosX + perpDist * rayDX;
            wallX -= Math.Floor(wallX);
            var texX = (int)(wallX * texSzW);
            if ((side == 0 && rayDX > 0) || (side == 1 && rayDY < 0))
                texX = texSzW - texX - 1;
            texX = Math.Clamp(texX, 0, texSzW - 1);

            // Fog factor
            var fog = (float)Math.Clamp(1.0 - (perpDist - FogStart) / (FogEnd - FogStart), 0, 1);
            // Y-side walls are darker (classic Wolfenstein shadow trick)
            var sideFactor = side == 0 ? 1.0f : 0.6f;
            var bright = fog * sideFactor;

            // Torchlight: warm glow near player
            var warmth = (float)Math.Clamp(0.3 - perpDist * 0.04, 0, 0.3f);

            // Draw textured wall column
            var step = (double)texSzH / wallH;
            var texY = drawTop > RH / 2 - wallH / 2
                ? (drawTop - (RH / 2 - wallH / 2)) * step
                : 0;

            for (var y = drawTop; y <= drawBot; y++)
            {
                var ty = Math.Clamp((int)texY, 0, texSzH - 1);
                var raw = _wallTexPix[ty * texSzW + texX];

                var r = (byte)Math.Clamp(raw.R * bright + warmth * 200, 0, 255);
                var g = (byte)Math.Clamp(raw.G * bright + warmth * 120, 0, 255);
                var b = (byte)Math.Clamp(raw.B * bright + warmth * 30, 0, 255);

                _fb[y * RW + col] = new Color(r, g, b);
                texY += step;
            }
        }
    }

    // ── Enemy sprite rendering ────────────────────────────────────────────

    private void RenderEnemies(ICamera camera, List<Enemy> enemies)
    {
        // Sort farthest-first so nearer enemies overdraw distant ones
        foreach (var e in enemies)
            e.DistSq = (e.PosX - camera.PosX) * (e.PosX - camera.PosX) +
                       (e.PosY - camera.PosY) * (e.PosY - camera.PosY);

        var sorted = new List<Enemy>(enemies);
        sorted.Sort((a, b) => b.DistSq.CompareTo(a.DistSq));

        foreach (var e in sorted)
        {
            if (e.IsDead) continue;
            DrawEnemy(camera, e);
        }
    }

    private void DrawEnemy(ICamera camera, Enemy enemy)
    {
        var relX = enemy.PosX - camera.PosX;
        var relY = enemy.PosY - camera.PosY;

        // Camera-space transform (inverse of view matrix)
        var invDet = 1.0 / (camera.PlaneX * camera.DirY - camera.DirX * camera.PlaneY);
        var transX = invDet * (camera.DirY * relX - camera.DirX * relY);
        var transY = invDet * (-camera.PlaneY * relX + camera.PlaneX * relY);

        if (transY <= 0.05) return; // behind player

        var sheet = enemy.SpriteSheet;

        var screenX = (int)(RW / 2 * (1.0 + transX / transY));
        var screenH = Math.Abs((int)(RH / transY));
        var screenW = (int)(screenH * sheet.FrameWidth / (double)sheet.Height);

        var drawTopY = RH / 2 - screenH / 2;
        var drawBotY = RH / 2 + screenH / 2;
        var drawLeft = screenX - screenW / 2;
        var drawRight = screenX + screenW / 2;

        var fog = (float)Math.Clamp(1.0 - (transY - FogStart) / (FogEnd - FogStart), 0, 1);
        var warmth = (float)Math.Clamp(0.3 - transY * 0.04, 0, 0.3f);

        var frameOffX = enemy.AnimFrame * sheet.FrameWidth;

        for (var col = Math.Max(0, drawLeft); col < Math.Min(RW, drawRight); col++)
        {
            if (_zBuf[col] < transY) continue; // depth test

            var texX = frameOffX + (int)((col - drawLeft) * sheet.FrameWidth / (double)screenW);
            texX = Math.Clamp(texX, frameOffX, frameOffX + sheet.FrameWidth - 1);

            for (var row = Math.Max(0, drawTopY); row < Math.Min(RH, drawBotY); row++)
            {
                var texY = (int)((row - drawTopY) * sheet.Height / (double)screenH);
                texY = Math.Clamp(texY, 0, sheet.Height - 1);

                var c = sheet.Pixels[texY * sheet.Width + texX];
                if (c.A < 10) continue; // transparent pixel

                var r = (byte)Math.Clamp(c.R * fog + warmth * 180, 0, 255);
                var g = (byte)Math.Clamp(c.G * fog + warmth * 90, 0, 255);
                var b = (byte)Math.Clamp(c.B * fog, 0, 255);

                // Red tint when hurt
                if (enemy.IsHurt)
                {
                    r = (byte)Math.Min(255, r + 80);
                    g = (byte)Math.Max(0, g - 40);
                    b = (byte)Math.Max(0, b - 40);
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
    public Enemy? RaycastShoot(ICamera camera, List<Enemy> enemies, double maxRange = 6.0)
    {
        // Find which enemy is closest to the center column
        Enemy? best = null;
        var bestDist = maxRange;

        foreach (var e in enemies)
        {
            if (e.IsDead) continue;

            var relX = e.PosX - camera.PosX;
            var relY = e.PosY - camera.PosY;

            var invDet = 1.0 / (camera.PlaneX * camera.DirY - camera.DirX * camera.PlaneY);
            var transX = invDet * (camera.DirY * relX - camera.DirX * relY);
            var transY = invDet * (-camera.PlaneY * relX + camera.PlaneX * relY);

            if (transY <= 0.1 || transY > maxRange) continue;

            // Sprite screen center X
            var screenX = (int)(RW / 2 * (1.0 + transX / transY));
            var screenH = Math.Abs((int)(RH / transY));
            var halfW = screenH / 4;

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