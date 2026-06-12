using DCM.Core.Entities;
using DCM.Core.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace DCM.Core.UI
{
    /// <summary>
    /// Draws all HUD elements at 1280×720 resolution on top of the raycaster view.
    /// Matches the style from the reference image:
    ///  • Top center:   objective text
    ///  • Top left:     minimap
    ///  • Bottom left:  HEALTH + bar
    ///  • Bottom right: MONSTER + count
    ///  • Left side:    controls hint (first 10 seconds)
    /// </summary>
    public class HUD : IDisposable
    {
        private readonly SpriteBatch  _sb;
        private readonly SpriteFont   _font;
        private readonly Texture2D    _pixel;

        // Minimap config
        private const int MapTileSize = 8;   // pixels per tile on minimap
        private const int MapPadding  = 12;
        private const int MapBorder   = 2;

        // Colors
        private static readonly Color ColWall      = new Color(160, 150, 140);
        private static readonly Color ColFloor     = new Color(30, 28, 25);
        private static readonly Color ColPlayer    = new Color(60, 200, 60);
        private static readonly Color ColEnemy     = new Color(220, 30, 30);
        private static readonly Color ColExit      = new Color(60, 180, 220);
        private static readonly Color ColHpBar     = new Color(180, 20, 20);
        private static readonly Color ColHpBarFull = new Color(220, 50, 40);
        private static readonly Color ColBarBg     = new Color(30, 25, 22);
        private static readonly Color ColPanelBg   = new Color(0, 0, 0, 160);
        private static readonly Color ColText       = new Color(235, 225, 200);
        private static readonly Color ColTextDim    = new Color(160, 150, 130);
        private static readonly Color ColCrosshair  = new Color(255, 255, 255, 180);
        private static readonly Color ColHurtFlash  = new Color(200, 0, 0, 0);

        // Screen dimensions (always 1280×720)
        private const int SW = 1280;
        private const int SH = 720;

        private float _controlsTimer = 12f;

        public HUD(SpriteBatch sb, SpriteFont font, GraphicsDevice gd)
        {
            _sb    = sb;
            _font  = font;
            _pixel = new Texture2D(gd, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }

        public void Draw(GameTime gameTime, Player player, List<Enemy> enemies,
                         Map map, bool gameOver, bool won, bool paused = false)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (!paused) _controlsTimer -= dt;

            _sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                      SamplerState.PointClamp, null, null);

            if (paused)
            {
                DrawMinimap(player, enemies, map);
                DrawPauseScreen();
            }
            else if (gameOver || won)
            {
                DrawEndScreen(won, player);
            }
            else
            {
                DrawMinimap(player, enemies, map);
                DrawHealthBar(player);
                DrawMonsterCounter(enemies);
                DrawObjectiveText();
                DrawCrosshair();
                if (_controlsTimer > 0) DrawControls();
                DrawHurtFlash(player);
            }

            _sb.End();
        }

        // ── Objective text ────────────────────────────────────────────────────

        private void DrawObjectiveText()
        {
            string text = "Find the exit and escape!";
            Vector2 size = _font.MeasureString(text);
            float x = (SW - size.X) / 2f;
            DrawTextShadow(text, new Vector2(x, 14), ColText);
        }

        // ── Minimap ───────────────────────────────────────────────────────────

        private void DrawMinimap(Player player, List<Enemy> enemies, Map map)
        {
            int mapW = map.Width  * MapTileSize;
            int mapH = map.Height * MapTileSize;
            int ox = MapPadding;
            int oy = MapPadding;

            // Panel background
            DrawRect(ox - MapBorder, oy - MapBorder,
                     mapW + MapBorder * 2, mapH + MapBorder * 2,
                     new Color(0, 0, 0, 200));

            // Tiles
            for (int ty = 0; ty < map.Height; ty++)
            for (int tx = 0; tx < map.Width; tx++)
            {
                int tile = map.GetTile(tx, ty);
                Color c = tile == 0         ? ColFloor
                        : tile == Tile.Exit ? ColExit
                        : ColWall;
                DrawRect(ox + tx * MapTileSize, oy + ty * MapTileSize,
                         MapTileSize - 1, MapTileSize - 1, c);
            }

            // Enemies
            foreach (var e in enemies)
            {
                if (e.IsDead) continue;
                int ex = ox + (int)(e.PosX * MapTileSize) - 2;
                int ey = oy + (int)(e.PosY * MapTileSize) - 2;
                DrawRect(ex, ey, 4, 4, ColEnemy);
            }

            // Player (dot + direction tick)
            int px = ox + (int)(player.PosX * MapTileSize);
            int py = oy + (int)(player.PosY * MapTileSize);
            DrawRect(px - 2, py - 2, 5, 5, ColPlayer);

            // Direction arrow
            int arrowX = px + (int)(player.DirX * 6);
            int arrowY = py + (int)(player.DirY * 6);
            DrawLine(px, py, arrowX, arrowY, ColPlayer);
        }

        // ── Health bar ────────────────────────────────────────────────────────

        private void DrawHealthBar(Player player)
        {
            int panelX = 12;
            int panelY = SH - 60;
            int barW   = 160;
            int barH   = 18;

            DrawRect(panelX - 4, panelY - 26, barW + 8, barH + 32, ColPanelBg);
            DrawTextShadow("HEALTH", new Vector2(panelX, panelY - 20), ColText, 0.85f);

            // Background
            DrawRect(panelX, panelY, barW, barH, ColBarBg);

            // Fill
            float frac = player.Health / 100f;
            int fillW = (int)(barW * frac);
            Color barCol = frac > 0.5f ? ColHpBarFull : ColHpBar;
            if (fillW > 0)
                DrawRect(panelX, panelY, fillW, barH, barCol);

            // Health number
            DrawTextShadow(player.Health.ToString(),
                           new Vector2(panelX + barW + 8, panelY), ColText, 0.85f);
        }

        // ── Monster counter ───────────────────────────────────────────────────

        private void DrawMonsterCounter(List<Enemy> enemies)
        {
            int alive = 0;
            foreach (var e in enemies) if (!e.IsDead) alive++;

            int panelW = 160;
            int panelX = SW - panelW - 12;
            int panelY = SH - 60;
            int barW   = 160;
            int barH   = 18;

            DrawRect(panelX - 4, panelY - 26, barW + 8, barH + 32, ColPanelBg);
            DrawTextShadow("MONSTER", new Vector2(panelX, panelY - 20), ColText, 0.85f);

            // Dotted bar showing remaining enemies
            int total = enemies.Count;
            if (total > 0)
            {
                int dotW = (barW - (total - 1) * 2) / total;
                dotW = Math.Max(dotW, 4);
                for (int i = 0; i < total; i++)
                {
                    bool dead = enemies[i].IsDead;
                    Color c = dead ? ColBarBg : ColEnemy;
                    DrawRect(panelX + i * (dotW + 2), panelY, dotW, barH, c);
                }
            }

            DrawTextShadow($"{alive}/{total}",
                           new Vector2(panelX - 36, panelY), ColText, 0.85f);
        }

        // ── Crosshair ─────────────────────────────────────────────────────────

        private void DrawCrosshair()
        {
            int cx = SW / 2;
            int cy = SH / 2;
            int len = 8;
            int gap = 3;
            DrawRect(cx - len - gap, cy - 1, len, 2, ColCrosshair);
            DrawRect(cx + gap + 1,   cy - 1, len, 2, ColCrosshair);
            DrawRect(cx - 1, cy - len - gap, 2, len, ColCrosshair);
            DrawRect(cx - 1, cy + gap + 1,   2, len, ColCrosshair);
        }

        // ── Controls hint ─────────────────────────────────────────────────────

        private void DrawControls()
        {
            float alpha = Math.Min(1f, _controlsTimer / 3f);
            Color c = ColTextDim * alpha;

            int x = 14;
            int y = SH / 2 - 60;
            int lineH = 18;
            var lines = new[]
            {
                "W A S D  -  Move",
                "Mouse    -  Look",
                "< >      -  Turn",
                "Shift    -  Run",
                "LClick   -  Attack",
                "M        -  Map",
                "Esc      -  Quit",
            };
            foreach (var line in lines)
            {
                DrawTextShadow(line, new Vector2(x, y), c, 0.75f);
                y += lineH;
            }
        }

        // ── Hurt flash ────────────────────────────────────────────────────────

        private void DrawHurtFlash(Player player)
        {
            if (player.HurtTimer <= 0) return;
            byte alpha = (byte)(player.HurtTimer * 180 / 0.35f);
            DrawRect(0, 0, SW, SH, new Color(180, 0, 0, (int)alpha));
        }

        // ── Pause screen ──────────────────────────────────────────────────────

        private void DrawPauseScreen()
        {
            DrawRect(0, 0, SW, SH, new Color(0, 0, 0, 160));

            string title   = "PAUSED";
            string resume  = "Esc  -  Resume";
            string quit    = "Q    -  Quit";

            Vector2 ts = _font.MeasureString(title);
            Vector2 rs = _font.MeasureString(resume);
            Vector2 qs = _font.MeasureString(quit);

            DrawTextShadow(title,  new Vector2((SW - ts.X) / 2, SH / 2 - 60), ColText);
            DrawTextShadow(resume, new Vector2((SW - rs.X) / 2, SH / 2),      ColTextDim, 0.85f);
            DrawTextShadow(quit,   new Vector2((SW - qs.X) / 2, SH / 2 + 30), ColTextDim, 0.85f);
        }

        // ── End screens ───────────────────────────────────────────────────────

        private void DrawEndScreen(bool won, Player player)
        {
            DrawRect(0, 0, SW, SH, new Color(0, 0, 0, 180));

            string title = won ? "YOU ESCAPED!" : "YOU DIED";
            string sub   = won ? "The babushka's curse is broken."
                               : "She found you in the dark...";
            string hint  = "Press Esc to quit";

            Vector2 ts = _font.MeasureString(title);
            Vector2 ss = _font.MeasureString(sub);
            Vector2 hs = _font.MeasureString(hint);

            Color tc = won ? new Color(60, 220, 60) : new Color(220, 40, 40);

            DrawTextShadow(title, new Vector2((SW - ts.X) / 2, SH / 2 - 60), tc);
            DrawTextShadow(sub,   new Vector2((SW - ss.X) / 2, SH / 2),      ColText, 0.85f);
            DrawTextShadow(hint,  new Vector2((SW - hs.X) / 2, SH / 2 + 60), ColTextDim, 0.75f);
        }

        // ── Drawing helpers ───────────────────────────────────────────────────

        private void DrawRect(int x, int y, int w, int h, Color c)
        {
            _sb.Draw(_pixel, new Rectangle(x, y, w, h), c);
        }

        private void DrawTextShadow(string text, Vector2 pos, Color color, float scale = 1f)
        {
            // Shadow: opaque black with same alpha as the foreground color
            Color shadow = new Color(0, 0, 0, (int)color.A);
            _sb.DrawString(_font, text, pos + new Vector2(1, 1),
                           shadow, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            _sb.DrawString(_font, text, pos, color,
                           0, Vector2.Zero, scale, SpriteEffects.None, 0);
        }

        private void DrawLine(int x0, int y0, int x1, int y1, Color c)
        {
            int dx = Math.Abs(x1 - x0), dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            while (true)
            {
                DrawRect(x0, y0, 1, 1, c);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 <  dx) { err += dx; y0 += sy; }
            }
        }

        public void Dispose()
        {
            _pixel.Dispose();
        }
    }
}
