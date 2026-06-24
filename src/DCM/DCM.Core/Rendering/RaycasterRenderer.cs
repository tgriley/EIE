#nullable enable
using DCM.Core.Entities;
using DCM.Core.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace DCM.Core.Rendering;

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

    // Wall / floor / ceiling / door textures (pixel data from content pipeline)
    private readonly Color[] _wallTexPix;
    private readonly int _wallTexW;
    private readonly int _wallTexH;

    private readonly Color[] _doorTexPix;
    private readonly int _doorTexW;
    private readonly int _doorTexH;

    private readonly Color[] _floorTexPix;
    private readonly int _floorTexW;
    private readonly int _floorTexH;

    private readonly Color[] _ceilTexPix;
    private readonly int _ceilTexW;
    private readonly int _ceilTexH;

    // 1×1 white pixel for rectangles (muzzle flash overlay)
    private readonly Texture2D _pixel;

    // Weapon sprite (camera held by player)
    private readonly Texture2D? _weaponTex;

    // Fog: beyond this perpDist everything is pitch black
    private const double FogStart = 1.5;
    private const double FogEnd = 9.0;

    // Screen-space target (draw scaled frame buffer here)
    private Rectangle _destRect;

    // Shoot flash overlay
    public float MuzzleFlash { get; set; } = 0f;

    // Weapon bob accumulator — advance when player is moving
    public float WeaponBobPhase { get; set; }

    // Camera raise: set true while LMB is held; internally smoothed
    public bool WeaponRaiseTarget { get; set; }
    private float _weaponRaise;

    public RaycasterRenderer(GraphicsDevice gd,
        Color[] wallTexPix, int wallTexW, int wallTexH,
        Color[] doorTexPix, int doorTexW, int doorTexH,
        Color[] floorTexPix, int floorTexW, int floorTexH,
        Color[] ceilTexPix, int ceilTexW, int ceilTexH,
        Texture2D? weaponTex = null)
    {
        _gd = gd;
        _sb = new SpriteBatch(gd);

        _wallTexPix = wallTexPix;
        _wallTexW = wallTexW;
        _wallTexH = wallTexH;
        _doorTexPix = doorTexPix;
        _doorTexW = doorTexW;
        _doorTexH = doorTexH;
        _floorTexPix = floorTexPix;
        _floorTexW = floorTexW;
        _floorTexH = floorTexH;
        _ceilTexPix = ceilTexPix;
        _ceilTexW = ceilTexW;
        _ceilTexH = ceilTexH;

        _fbTex = new Texture2D(gd, RW, RH);

        _pixel = new Texture2D(gd, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _weaponTex = weaponTex;

        _destRect = new Rectangle(0, 0, RW * 2, RH * 2);
    }

    // ── Main render entry ─────────────────────────────────────────────────

    public void Render(GameTime gameTime, ICamera camera, IMap map, IEnumerable<IBillboard> billboards)
    {
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        var raiseTarget = WeaponRaiseTarget ? 1f : 0f;
        _weaponRaise += (raiseTarget - _weaponRaise) * Math.Min(1f, dt * 12f);

        DrawCeiling(camera);
        DrawFloor(camera);
        CastWalls(camera, map);
        RenderBillboards(camera, billboards);

        _fbTex.SetData(_fb);

        _sb.Begin(SpriteSortMode.Deferred, BlendState.Opaque,
            SamplerState.PointClamp, null, null);
        _sb.Draw(_fbTex, _destRect, Color.White);
        _sb.End();

        DrawWeapon();

        if (MuzzleFlash > 0)
        {
            _sb.Begin();
            var alpha = (byte)(MuzzleFlash * 180);
            _sb.Draw(_pixel, new Rectangle(0, 0, RW * 2, RH * 2),
                new Color(255, 255, 255, (int)alpha));
            _sb.End();
            MuzzleFlash = Math.Max(0, MuzzleFlash - dt * 6f);
        }
    }

    // ── Ceiling / floor projection ────────────────────────────────────────

    private void DrawCeiling(ICamera camera)
    {
        var half = RH / 2;

        var rayDirX0 = camera.DirX - camera.PlaneX;
        var rayDirY0 = camera.DirY - camera.PlaneY;
        var rayDirX1 = camera.DirX + camera.PlaneX;
        var rayDirY1 = camera.DirY + camera.PlaneY;

        for (var y = 0; y < half; y++)
        {
            var p = half - y;
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

                _fb[rowOff + x] = new Color((byte)(raw.R * fog), (byte)(raw.G * fog), (byte)(raw.B * fog));

                floorX += stepX;
                floorY += stepY;
            }
        }
    }

    private void DrawFloor(ICamera camera)
    {
        var half = RH / 2;

        var rayDirX0 = camera.DirX - camera.PlaneX;
        var rayDirY0 = camera.DirY - camera.PlaneY;
        var rayDirX1 = camera.DirX + camera.PlaneX;
        var rayDirY1 = camera.DirY + camera.PlaneY;

        for (var y = half; y < RH; y++)
        {
            var p = y - half;
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

                var tx = Math.Clamp((int)(fx * _floorTexW), 0, _floorTexW - 1);
                var ty = Math.Clamp((int)(fy * _floorTexH), 0, _floorTexH - 1);

                var raw = _floorTexPix[ty * _floorTexW + tx];

                _fb[rowOff + x] = new Color((byte)(raw.R * fog), (byte)(raw.G * fog), (byte)(raw.B * fog));

                floorX += stepX;
                floorY += stepY;
            }
        }
    }

    // ── DDA wall casting ──────────────────────────────────────────────────

    private void CastWalls(ICamera camera, IMap map)
    {
        for (var col = 0; col < RW; col++)
        {
            var camX = 2.0 * col / RW - 1.0;

            var rayDX = camera.DirX + camera.PlaneX * camX;
            var rayDY = camera.DirY + camera.PlaneY * camX;

            var mapX = (int)camera.PosX;
            var mapY = (int)camera.PosY;

            var deltaDX = rayDX == 0 ? double.MaxValue : Math.Abs(1.0 / rayDX);
            var deltaDY = rayDY == 0 ? double.MaxValue : Math.Abs(1.0 / rayDY);

            int stepX, stepY;
            double sideX, sideY;

            if (rayDX < 0) { stepX = -1; sideX = (camera.PosX - mapX) * deltaDX; }
            else           { stepX =  1; sideX = (mapX + 1.0 - camera.PosX) * deltaDX; }

            if (rayDY < 0) { stepY = -1; sideY = (camera.PosY - mapY) * deltaDY; }
            else           { stepY =  1; sideY = (mapY + 1.0 - camera.PosY) * deltaDY; }

            var side = 0;
            var hitTile = 0;
            for (var safety = 0; safety < 64; safety++)
            {
                if (sideX < sideY) { sideX += deltaDX; mapX += stepX; side = 0; }
                else               { sideY += deltaDY; mapY += stepY; side = 1; }

                hitTile = map.GetTile(mapX, mapY);
                if (hitTile != 0) break;
            }

            var perpDist = side == 0 ? sideX - deltaDX : sideY - deltaDY;
            if (perpDist < 0.001) perpDist = 0.001;

            _zBuf[col] = perpDist;

            var wallH = (int)(RH / perpDist);
            var drawTop = Math.Max(0, RH / 2 - wallH / 2);
            var drawBot = Math.Min(RH - 1, RH / 2 + wallH / 2);

            var isDoor = hitTile == Tile.Exit;
            var texPix = isDoor ? _doorTexPix : _wallTexPix;
            var texSzW = isDoor ? _doorTexW : _wallTexW;
            var texSzH = isDoor ? _doorTexH : _wallTexH;

            var wallX = side == 0
                ? camera.PosY + perpDist * rayDY
                : camera.PosX + perpDist * rayDX;
            wallX -= Math.Floor(wallX);
            var texX = (int)(wallX * texSzW);
            if ((side == 0 && rayDX > 0) || (side == 1 && rayDY < 0))
                texX = texSzW - texX - 1;
            texX = Math.Clamp(texX, 0, texSzW - 1);

            var fog = (float)Math.Clamp(1.0 - (perpDist - FogStart) / (FogEnd - FogStart), 0, 1);
            var sideFactor = side == 0 ? 1.0f : 0.6f;
            var bright = fog * sideFactor;
            var warmth = (float)Math.Clamp(0.3 - perpDist * 0.04, 0, 0.3f);

            var step = (double)texSzH / wallH;
            var texY = drawTop > RH / 2 - wallH / 2
                ? (drawTop - (RH / 2 - wallH / 2)) * step
                : 0;

            for (var y = drawTop; y <= drawBot; y++)
            {
                var ty = Math.Clamp((int)texY, 0, texSzH - 1);
                var raw = texPix[ty * texSzW + texX];

                var r = (byte)Math.Clamp(raw.R * bright + warmth * 200, 0, 255);
                var g = (byte)Math.Clamp(raw.G * bright + warmth * 120, 0, 255);
                var b = (byte)Math.Clamp(raw.B * bright + warmth * 30, 0, 255);

                _fb[y * RW + col] = new Color(r, g, b);
                texY += step;
            }
        }
    }

    // ── Billboard rendering ───────────────────────────────────────────────

    private void RenderBillboards(ICamera camera, IEnumerable<IBillboard> billboards)
    {
        var list = new List<(IBillboard b, double distSq)>();
        foreach (var b in billboards)
        {
            if (!b.IsVisible) continue;
            var dx = b.PosX - camera.PosX;
            var dy = b.PosY - camera.PosY;
            list.Add((b, dx * dx + dy * dy));
        }
        list.Sort((a, b) => b.distSq.CompareTo(a.distSq));
        foreach (var (b, _) in list)
            DrawBillboard(camera, b);
    }

    private void DrawBillboard(ICamera camera, IBillboard b)
    {
        var relX = b.PosX - camera.PosX;
        var relY = b.PosY - camera.PosY;

        var invDet = 1.0 / (camera.PlaneX * camera.DirY - camera.DirX * camera.PlaneY);
        var transX = invDet * (camera.DirY * relX - camera.DirX * relY);
        var transY = invDet * (-camera.PlaneY * relX + camera.PlaneX * relY);

        if (transY <= 0.05) return;

        var screenX  = (int)(RW / 2 * (1.0 + transX / transY));
        var screenH  = Math.Abs((int)(RH / transY)) / b.HeightDivisor;
        var screenW  = (int)(screenH * b.TexWidth / (double)b.TexHeight);
        var drawTopY = RH / 2 - screenH / 2 + (int)(screenH * b.VerticalShift);
        var drawBotY = drawTopY + screenH;
        var drawLeft = screenX - screenW / 2;
        var drawRight = screenX + screenW / 2;

        var fog    = (float)Math.Clamp(1.0 - (transY - FogStart) / (FogEnd - FogStart), 0, 1);
        var warmth = (float)Math.Clamp(0.3 - transY * 0.04, 0, 0.3f);

        for (var col = Math.Max(0, drawLeft); col < Math.Min(RW, drawRight); col++)
        {
            if (_zBuf[col] < transY) continue;

            var texX = b.PixelOffsetX + (int)((col - drawLeft) * b.TexWidth / (double)screenW);
            texX = Math.Clamp(texX, b.PixelOffsetX, b.PixelOffsetX + b.TexWidth - 1);

            for (var row = Math.Max(0, drawTopY); row < Math.Min(RH, drawBotY); row++)
            {
                var texY = (int)((row - drawTopY) * b.TexHeight / (double)screenH);
                texY = Math.Clamp(texY, 0, b.TexHeight - 1);

                var c = b.Pixels[texY * b.TexStride + texX];
                if (c.A < 10) continue;

                var r = (byte)Math.Clamp(c.R * fog + warmth * 180, 0, 255);
                var g = (byte)Math.Clamp(c.G * fog + warmth * 90, 0, 255);
                var bv = (byte)Math.Clamp(c.B * fog, 0, 255);

                if (b.ApplyHurtTint)
                {
                    r  = (byte)Math.Min(255, r + 80);
                    g  = (byte)Math.Max(0, g - 40);
                    bv = (byte)Math.Max(0, bv - 40);
                }

                _fb[row * RW + col] = new Color(r, g, bv, c.A);
            }
        }

    }

    // ── Weapon sprite ─────────────────────────────────────────────────────

    private void DrawWeapon()
    {
        if (_weaponTex == null) return;

        const int screenW = RW * 2;
        const int screenH = RH * 2;

        // Scale so the weapon occupies ~50% of screen height
        float scale = screenH * 0.50f / _weaponTex.Height;
        int dw = (int)(_weaponTex.Width  * scale);
        int dh = (int)(_weaponTex.Height * scale);

        // Bob dampens to 20% when fully raised (steady hands for a photo)
        var bobAmp = 1f - _weaponRaise * 0.8f;
        var bobY = (float)Math.Sin(WeaponBobPhase)       * 10f * bobAmp;
        var bobX = (float)Math.Sin(WeaponBobPhase * 0.5) *  5f * bobAmp;

        // Raised: bottom of sprite aligns with bottom of screen.
        // Lowered: sprite slides 150 px further down so hands are mostly off-screen.
        var slideY = (int)((1f - _weaponRaise) * 150f);

        int x = (screenW - dw) / 2 + (int)bobX;
        int y = screenH - dh + slideY + (int)bobY;

        _sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
            SamplerState.PointClamp, null, null);
        _sb.Draw(_weaponTex, new Rectangle(x, y, dw, dh), Color.White);
        _sb.End();
    }

    public void Dispose()
    {
        _sb.Dispose();
        _fbTex.Dispose();
        _pixel.Dispose();
    }
}
