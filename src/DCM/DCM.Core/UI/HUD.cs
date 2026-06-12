using DCM.Core.Entities;
using DCM.Core.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace DCM.Core.UI
{
    public enum HudAction { None, Resume, Quit }

    public class HUD : IDisposable
    {
        private readonly UIPainter _painter;
        private readonly Button    _resumeButton;
        private readonly Button    _quitButton;

        private const int MapTileSize = 8;
        private const int MapPadding  = 12;
        private const int MapBorder   = 2;
        private const int SW = 1280;
        private const int SH = 720;

        private static readonly Color ColWall      = new Color(160, 150, 140);
        private static readonly Color ColFloor     = new Color(30, 28, 25);
        private static readonly Color ColPlayer    = new Color(60, 200, 60);
        private static readonly Color ColEnemy     = new Color(220, 30, 30);
        private static readonly Color ColExit      = new Color(60, 180, 220);
        private static readonly Color ColHpBar     = new Color(180, 20, 20);
        private static readonly Color ColHpBarFull = new Color(220, 50, 40);
        private static readonly Color ColBarBg     = new Color(30, 25, 22);
        private static readonly Color ColPanelBg   = new Color(0, 0, 0, 160);
        private static readonly Color ColText      = new Color(235, 225, 200);
        private static readonly Color ColTextDim   = new Color(160, 150, 130);
        private static readonly Color ColCrosshair = new Color(255, 255, 255, 180);

        private float _controlsTimer = 12f;

        public HUD(SpriteBatch sb, SpriteFont font, GraphicsDevice gd)
        {
            _painter = new UIPainter(sb, font, gd);

            int btnW = 240, btnH = 52, btnX = (SW - 240) / 2;
            _resumeButton = new Button(new Rectangle(btnX, SH / 2 + 10, btnW, btnH), "RESUME", _painter);
            _quitButton   = new Button(new Rectangle(btnX, SH / 2 + 80, btnW, btnH), "QUIT",   _painter);
        }

        public HudAction Update(MouseState mouse, MouseState prevMouse)
        {
            if (_resumeButton.IsClicked(mouse, prevMouse)) return HudAction.Resume;
            if (_quitButton.IsClicked(mouse, prevMouse))   return HudAction.Quit;
            return HudAction.None;
        }

        public void Draw(GameTime gameTime, Player player, List<Enemy> enemies,
                         IMap map, bool gameOver, bool won, bool paused = false)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (!paused) _controlsTimer -= dt;

            Point mousePos = Mouse.GetState().Position;

            _painter.Begin();

            if (paused)
            {
                DrawMinimap(player, enemies, map);
                DrawPauseOverlay(mousePos);
            }
            else if (gameOver || won)
            {
                DrawEndOverlay(won);
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

            _painter.End();
        }

        private void DrawObjectiveText()
        {
            string  text = "Find the exit and escape!";
            Vector2 size = _painter.Measure(text);
            _painter.DrawTextShadow(text, new Vector2((SW - size.X) / 2f, 14), ColText);
        }

        private void DrawMinimap(Player player, List<Enemy> enemies, IMap map)
        {
            int mapW = map.Width * MapTileSize, mapH = map.Height * MapTileSize;
            int ox = MapPadding, oy = MapPadding;

            _painter.DrawRect(ox - MapBorder, oy - MapBorder,
                              mapW + MapBorder * 2, mapH + MapBorder * 2, new Color(0, 0, 0, 200));

            for (int ty = 0; ty < map.Height; ty++)
            for (int tx = 0; tx < map.Width;  tx++)
            {
                int tile = map.GetTile(tx, ty);
                Color c  = tile == 0         ? ColFloor
                         : tile == Tile.Exit ? ColExit
                         : ColWall;
                _painter.DrawRect(ox + tx * MapTileSize, oy + ty * MapTileSize,
                                  MapTileSize - 1, MapTileSize - 1, c);
            }

            foreach (var e in enemies)
            {
                if (e.IsDead) continue;
                _painter.DrawRect(ox + (int)(e.PosX * MapTileSize) - 2,
                                  oy + (int)(e.PosY * MapTileSize) - 2, 4, 4, ColEnemy);
            }

            int px = ox + (int)(player.PosX * MapTileSize);
            int py = oy + (int)(player.PosY * MapTileSize);
            _painter.DrawRect(px - 2, py - 2, 5, 5, ColPlayer);
            _painter.DrawLine(px, py, px + (int)(player.DirX * 6), py + (int)(player.DirY * 6), ColPlayer);
        }

        private void DrawHealthBar(Player player)
        {
            int panelX = 12, panelY = SH - 60, barW = 160, barH = 18;

            _painter.DrawRect(panelX - 4, panelY - 26, barW + 8, barH + 32, ColPanelBg);
            _painter.DrawTextShadow("HEALTH", new Vector2(panelX, panelY - 20), ColText, 0.85f);
            _painter.DrawRect(panelX, panelY, barW, barH, ColBarBg);

            float frac  = player.Health / 100f;
            int   fillW = (int)(barW * frac);
            if (fillW > 0)
                _painter.DrawRect(panelX, panelY, fillW, barH, frac > 0.5f ? ColHpBarFull : ColHpBar);

            _painter.DrawTextShadow(player.Health.ToString(),
                                    new Vector2(panelX + barW + 8, panelY), ColText, 0.85f);
        }

        private void DrawMonsterCounter(List<Enemy> enemies)
        {
            int alive = 0;
            foreach (var e in enemies) if (!e.IsDead) alive++;

            int panelX = SW - 172, panelY = SH - 60, barW = 160, barH = 18;

            _painter.DrawRect(panelX - 4, panelY - 26, barW + 8, barH + 32, ColPanelBg);
            _painter.DrawTextShadow("MONSTER", new Vector2(panelX, panelY - 20), ColText, 0.85f);

            int total = enemies.Count;
            if (total > 0)
            {
                int dotW = Math.Max(4, (barW - (total - 1) * 2) / total);
                for (int i = 0; i < total; i++)
                    _painter.DrawRect(panelX + i * (dotW + 2), panelY, dotW, barH,
                                      enemies[i].IsDead ? ColBarBg : ColEnemy);
            }

            _painter.DrawTextShadow($"{alive}/{total}", new Vector2(panelX - 36, panelY), ColText, 0.85f);
        }

        private void DrawCrosshair()
        {
            int cx = SW / 2, cy = SH / 2, len = 8, gap = 3;
            _painter.DrawRect(cx - len - gap, cy - 1, len, 2, ColCrosshair);
            _painter.DrawRect(cx + gap + 1,   cy - 1, len, 2, ColCrosshair);
            _painter.DrawRect(cx - 1, cy - len - gap, 2, len, ColCrosshair);
            _painter.DrawRect(cx - 1, cy + gap + 1,   2, len, ColCrosshair);
        }

        private void DrawControls()
        {
            float alpha = Math.Min(1f, _controlsTimer / 3f);
            Color c = ColTextDim * alpha;
            int x = 14, y = SH / 2 - 60;
            foreach (var line in new[] { "W A S D  -  Move", "Mouse    -  Look", "< >      -  Turn",
                                         "Shift    -  Run",  "LClick   -  Attack", "M        -  Map",
                                         "Esc      -  Pause" })
            {
                _painter.DrawTextShadow(line, new Vector2(x, y), c, 0.75f);
                y += 18;
            }
        }

        private void DrawHurtFlash(Player player)
        {
            if (player.HurtTimer <= 0) return;
            byte alpha = (byte)(player.HurtTimer * 180 / 0.35f);
            _painter.DrawRect(0, 0, SW, SH, new Color(180, 0, 0, (int)alpha));
        }

        private void DrawPauseOverlay(Point mousePos)
        {
            _painter.DrawRect(0, 0, SW, SH, new Color(0, 0, 0, 160));
            string  title = "PAUSED";
            Vector2 ts    = _painter.Measure(title);
            _painter.DrawTextShadow(title, new Vector2((SW - ts.X) / 2, SH / 2 - 60), ColText);
            _resumeButton.Draw(mousePos);
            _quitButton.Draw(mousePos);
        }

        private void DrawEndOverlay(bool won)
        {
            _painter.DrawRect(0, 0, SW, SH, new Color(0, 0, 0, 180));

            string title = won ? "YOU ESCAPED!" : "YOU DIED";
            string sub   = won ? "The babushka's curse is broken." : "She found you in the dark...";
            string hint  = "Press Esc to quit";
            Color  tc    = won ? new Color(60, 220, 60) : new Color(220, 40, 40);

            _painter.DrawTextShadow(title, new Vector2((SW - _painter.Measure(title).X) / 2, SH / 2 - 60), tc);
            _painter.DrawTextShadow(sub,   new Vector2((SW - _painter.Measure(sub).X)   / 2, SH / 2),      ColText,    0.85f);
            _painter.DrawTextShadow(hint,  new Vector2((SW - _painter.Measure(hint).X)  / 2, SH / 2 + 60), ColTextDim, 0.75f);
        }

        public void Dispose() => _painter.Dispose();
    }
}
