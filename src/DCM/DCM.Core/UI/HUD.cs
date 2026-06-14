using DCM.Core;
using DCM.Core.Entities;
using DCM.Core.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace DCM.Core.UI;

public enum HudAction
{
    None,
    Resume,
    Quit,
    MainMenu,
    NextLevel
}

public class HUD : IDisposable
{
    private readonly UIPainter _painter;
    private readonly Button _resumeButton;
    private readonly Button _quitButton;
    private readonly Button _mainMenuButton;
    private readonly Button _nextLevelButton;

    private const int MapTileSize = 8;
    private const int MapPadding = 12;
    private const int MapBorder = 2;
    private const int SW = 1280;
    private const int SH = 720;

    private static readonly Color ColWall = new(160, 150, 140);
    private static readonly Color ColFloor = new(30, 28, 25);
    private static readonly Color ColPlayer = new(60, 200, 60);
    private static readonly Color ColEnemy = new(220, 30, 30);
    private static readonly Color ColExit = new(60, 180, 220);
    private static readonly Color ColHpBar = new(180, 20, 20);
    private static readonly Color ColHpBarFull = new(220, 50, 40);
    private static readonly Color ColBarBg = new(30, 25, 22);
    private static readonly Color ColPanelBg = new(0, 0, 0, 160);
    private static readonly Color ColText = new(235, 225, 200);
    private static readonly Color ColTextDim = new(160, 150, 130);
    private static readonly Color ColCrosshair = new(255, 255, 255, 180);

    private float _controlsTimer = 12f;

    public HUD(SpriteBatch sb, SpriteFont font, GraphicsDevice gd)
    {
        _painter = new UIPainter(sb, font, gd);

        int btnW = 240, btnH = 52, btnX = (SW - 240) / 2;
        _resumeButton   = new Button(new Rectangle(btnX, SH / 2 + 10, btnW, btnH), "RESUME",       _painter);
        _quitButton     = new Button(new Rectangle(btnX, SH / 2 + 80, btnW, btnH), "QUIT",         _painter);
        _nextLevelButton = new Button(new Rectangle(btnX, SH / 2 + 10, btnW, btnH), "NEXT LEVEL",  _painter);
        _mainMenuButton = new Button(new Rectangle(btnX, SH / 2 + 80, btnW, btnH), "LEVEL SELECT", _painter);
    }

    public HudAction UpdatePause(MouseState mouse, MouseState prevMouse)
    {
        if (_resumeButton.IsClicked(mouse, prevMouse)) return HudAction.Resume;
        if (_quitButton.IsClicked(mouse, prevMouse)) return HudAction.Quit;
        return HudAction.None;
    }

    public HudAction UpdateEnd(MouseState mouse, MouseState prevMouse, bool hasNextLevel)
    {
        if (hasNextLevel && _nextLevelButton.IsClicked(mouse, prevMouse)) return HudAction.NextLevel;
        if (_mainMenuButton.IsClicked(mouse, prevMouse)) return HudAction.MainMenu;
        return HudAction.None;
    }

    public void Draw(GameTime gameTime, Player player, List<Enemy> enemies,
        IMap map, bool gameOver, bool won, bool paused = false, bool hasNextLevel = false,
        float elapsed = 0f, float bestTime = float.MaxValue, bool isNewBest = false)
    {
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (!paused) _controlsTimer -= dt;

        var mousePos = Mouse.GetState().Position;

        _painter.Begin();

        if (paused)
        {
            DrawMinimap(player, enemies, map);
            DrawPauseOverlay(mousePos);
        }
        else if (gameOver || won)
        {
            DrawEndOverlay(won, hasNextLevel, elapsed, bestTime, isNewBest, mousePos);
        }
        else
        {
            DrawMinimap(player, enemies, map);
            DrawHealthBar(player);
            DrawMonsterCounter(enemies);
            DrawObjectiveText();
            DrawTimer(elapsed);
            DrawCrosshair();
            if (_controlsTimer > 0) DrawControls();
            DrawHurtFlash(player);
        }

        _painter.End();
    }

    private void DrawObjectiveText()
    {
        var text = "Find the exit and escape!";
        var size = _painter.Measure(text);
        _painter.DrawTextShadow(text, new Vector2((SW - size.X) / 2f, 14), ColText);
    }

    private void DrawMinimap(Player player, List<Enemy> enemies, IMap map)
    {
        int mapW = map.Width * MapTileSize, mapH = map.Height * MapTileSize;
        int ox = MapPadding, oy = MapPadding;

        _painter.DrawRect(ox - MapBorder, oy - MapBorder,
            mapW + MapBorder * 2, mapH + MapBorder * 2, new Color(0, 0, 0, 200));

        for (var ty = 0; ty < map.Height; ty++)
        for (var tx = 0; tx < map.Width; tx++)
        {
            var tile = map.GetTile(tx, ty);
            var c = tile == 0 ? ColFloor
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

        var px = ox + (int)(player.PosX * MapTileSize);
        var py = oy + (int)(player.PosY * MapTileSize);
        _painter.DrawRect(px - 2, py - 2, 5, 5, ColPlayer);
        _painter.DrawLine(px, py, px + (int)(player.DirX * 6), py + (int)(player.DirY * 6), ColPlayer);
    }

    private void DrawHealthBar(Player player)
    {
        int panelX = 12, panelY = SH - 60, barW = 160, barH = 18;

        _painter.DrawRect(panelX - 4, panelY - 26, barW + 8, barH + 32, ColPanelBg);
        _painter.DrawTextShadow("HEALTH", new Vector2(panelX, panelY - 20), ColText, 0.85f);
        _painter.DrawRect(panelX, panelY, barW, barH, ColBarBg);

        var frac = player.Health / 100f;
        var fillW = (int)(barW * frac);
        if (fillW > 0)
            _painter.DrawRect(panelX, panelY, fillW, barH, frac > 0.5f ? ColHpBarFull : ColHpBar);

        _painter.DrawTextShadow(player.Health.ToString(),
            new Vector2(panelX + barW + 8, panelY), ColText, 0.85f);
    }

    private void DrawMonsterCounter(List<Enemy> enemies)
    {
        var alive = 0;
        foreach (var e in enemies)
            if (!e.IsDead)
                alive++;

        int panelX = SW - 172, panelY = SH - 60, barW = 160, barH = 18;

        _painter.DrawRect(panelX - 4, panelY - 26, barW + 8, barH + 32, ColPanelBg);
        _painter.DrawTextShadow("MONSTER", new Vector2(panelX, panelY - 20), ColText, 0.85f);

        var total = enemies.Count;
        if (total > 0)
        {
            var dotW = Math.Max(4, (barW - (total - 1) * 2) / total);
            for (var i = 0; i < total; i++)
                _painter.DrawRect(panelX + i * (dotW + 2), panelY, dotW, barH,
                    enemies[i].IsDead ? ColBarBg : ColEnemy);
        }

        _painter.DrawTextShadow($"{alive}/{total}", new Vector2(panelX - 36, panelY), ColText, 0.85f);
    }

    private void DrawCrosshair()
    {
        int cx = SW / 2, cy = SH / 2, len = 8, gap = 3;
        _painter.DrawRect(cx - len - gap, cy - 1, len, 2, ColCrosshair);
        _painter.DrawRect(cx + gap + 1, cy - 1, len, 2, ColCrosshair);
        _painter.DrawRect(cx - 1, cy - len - gap, 2, len, ColCrosshair);
        _painter.DrawRect(cx - 1, cy + gap + 1, 2, len, ColCrosshair);
    }

    private void DrawControls()
    {
        var alpha = Math.Min(1f, _controlsTimer / 3f);
        var c = ColTextDim * alpha;
        int x = 14, y = SH / 2 - 60;
        foreach (var line in new[]
                 {
                     "W A S D  -  Move", "Mouse    -  Look", "< >      -  Turn",
                     "Shift    -  Run", "LClick   -  Attack", "M        -  Map",
                     "Esc      -  Pause"
                 })
        {
            _painter.DrawTextShadow(line, new Vector2(x, y), c, 0.75f);
            y += 18;
        }
    }

    private void DrawHurtFlash(Player player)
    {
        if (player.HurtTimer <= 0) return;
        var alpha = (byte)(player.HurtTimer * 180 / 0.35f);
        _painter.DrawRect(0, 0, SW, SH, new Color(180, 0, 0, (int)alpha));
    }

    private void DrawPauseOverlay(Point mousePos)
    {
        _painter.DrawRect(0, 0, SW, SH, new Color(0, 0, 0, 160));
        var title = "PAUSED";
        var ts = _painter.Measure(title);
        _painter.DrawTextShadow(title, new Vector2((SW - ts.X) / 2, SH / 2 - 60), ColText);
        _resumeButton.Draw(mousePos);
        _quitButton.Draw(mousePos);
    }

    private void DrawTimer(float elapsed)
    {
        var text = LevelProgress.FormatTime(elapsed);
        var size = _painter.Measure(text);
        int px = SW - (int)size.X - 22, py = 8;
        _painter.DrawRect(px - 8, py - 4, (int)size.X + 16, 28, ColPanelBg);
        _painter.DrawTextShadow(text, new Vector2(px, py), ColText);
    }

    private void DrawEndOverlay(bool won, bool hasNextLevel,
        float elapsed, float bestTime, bool isNewBest, Point mousePos)
    {
        _painter.DrawRect(0, 0, SW, SH, new Color(0, 0, 0, 180));

        var title = won ? "YOU ESCAPED!" : "YOU DIED";
        var tc = won ? new Color(60, 220, 60) : new Color(220, 40, 40);
        _painter.DrawTextShadow(title,
            new Vector2((SW - _painter.Measure(title).X) / 2, SH / 2 - 120), tc);

        if (won)
        {
            var timeText = $"TIME  {LevelProgress.FormatTime(elapsed)}";
            _painter.DrawTextShadow(timeText,
                new Vector2((SW - _painter.Measure(timeText).X) / 2, SH / 2 - 60), ColText);

            if (isNewBest)
            {
                const string newBestText = "NEW BEST!";
                _painter.DrawTextShadow(newBestText,
                    new Vector2((SW - _painter.Measure(newBestText).X) / 2, SH / 2 - 30),
                    new Color(255, 215, 0));
            }
            else if (bestTime < float.MaxValue)
            {
                var bestText = $"BEST  {LevelProgress.FormatTime(bestTime)}";
                _painter.DrawTextShadow(bestText,
                    new Vector2((SW - _painter.Measure(bestText).X) / 2, SH / 2 - 30), ColTextDim);
            }
        }

        if (won && hasNextLevel) _nextLevelButton.Draw(mousePos);
        _mainMenuButton.Draw(mousePos);
    }

    public void Dispose()
    {
        _painter.Dispose();
    }
}