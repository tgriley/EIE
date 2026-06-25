using DCM.Core;
using DCM.Core.Entities;
using DCM.Core.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
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
    private readonly SoundEffect _clickSound;
    private readonly Button _resumeButton;
    private readonly Button _quitButton;
    private readonly Button _mainMenuButton;
    private readonly Button _nextLevelButton;

    // index: 0=Resume, 1=LevelSelect, 2=Quit
    private readonly MenuNavigator _navPause = new(3);
    // index: 0=NextLevel (if present), last=LevelSelect
    private readonly MenuNavigator _navEnd   = new(1);

    private const int MapTileSize = 8;
    private const int MapPadding  = 12;
    private const int MapBorder   = 2;
    private const int SW = 1280;
    private const int SH = 720;

    private static readonly Color ColWall       = new(160, 150, 140);
    private static readonly Color ColFloor      = new(30, 28, 25);
    private static readonly Color ColPlayer     = new(60, 200, 60);
    private static readonly Color ColEnemy      = new(220, 30, 30);
    private static readonly Color ColExit       = new(60, 180, 220);
    private static readonly Color ColPanelBg  = new(0, 0, 0, 160);
    private static readonly Color ColText     = new(235, 225, 200);
    private static readonly Color ColTextDim  = new(160, 150, 130);
    private static readonly Color ColCrosshair = new(255, 255, 255, 180);

    private readonly StatusBarPanel _healthBar;
    private readonly StatusBarPanel _sprintBar;
    private readonly StatusBarPanel _cameraBar;

    private float _controlsTimer = 12f;

    public HUD(SpriteBatch sb, SpriteFont font, GraphicsDevice gd, SoundEffect clickSound)
    {
        _painter    = new UIPainter(sb, font, gd);
        _clickSound = clickSound;

        const int barX = 12, panelH = 50, gap = 6, step = panelH + gap;
        int yCamera = SH - 60, ySprint = yCamera - step, yHealth = ySprint - step;
        _healthBar = new(barX, yHealth, "HEALTH", new(220, 50, 40),  new(180, 20, 20));
        _sprintBar = new(barX, ySprint, "SPRINT", new(220, 200, 50), new(100, 90, 25));
        _cameraBar = new(barX, yCamera, "FLASH",  new(80, 200, 220), new(50, 110, 150));

        int btnW = 240, btnH = 52, btnX = (SW - 240) / 2;
        _resumeButton    = new Button(new Rectangle(btnX, SH / 2 + 10,  btnW, btnH), "RESUME",       _painter);
        _mainMenuButton  = new Button(new Rectangle(btnX, SH / 2 + 80,  btnW, btnH), "LEVEL SELECT", _painter);
        _quitButton      = new Button(new Rectangle(btnX, SH / 2 + 150, btnW, btnH), "QUIT",         _painter);
        _nextLevelButton = new Button(new Rectangle(btnX, SH / 2 + 10,  btnW, btnH), "NEXT LEVEL",   _painter);
    }

    public HudAction UpdatePause(GameTime gameTime, MouseState mouse, MouseState prevMouse)
    {
        _navPause.Update(gameTime);

        if (_resumeButton.IsClicked(mouse, prevMouse))   { _clickSound.Play(); return HudAction.Resume; }
        if (_mainMenuButton.IsClicked(mouse, prevMouse)) { _clickSound.Play(); return HudAction.MainMenu; }
        if (_quitButton.IsClicked(mouse, prevMouse))     { _clickSound.Play(); return HudAction.Quit; }

        if (_navPause.JustConfirmed)
        {
            _clickSound.Play();
            return _navPause.SelectedIndex switch
            {
                0 => HudAction.Resume,
                1 => HudAction.MainMenu,
                _ => HudAction.Quit
            };
        }
        if (_navPause.JustCancelled) { _clickSound.Play(); return HudAction.Resume; }

        return HudAction.None;
    }

    public HudAction UpdateEnd(GameTime gameTime, MouseState mouse, MouseState prevMouse, bool hasNextLevel)
    {
        _navEnd.ItemCount = hasNextLevel ? 2 : 1;
        _navEnd.Update(gameTime);

        if (hasNextLevel && _nextLevelButton.IsClicked(mouse, prevMouse)) { _clickSound.Play(); return HudAction.NextLevel; }
        if (_mainMenuButton.IsClicked(mouse, prevMouse))                  { _clickSound.Play(); return HudAction.MainMenu; }

        if (_navEnd.JustConfirmed)
        {
            _clickSound.Play();
            return hasNextLevel && _navEnd.SelectedIndex == 0 ? HudAction.NextLevel : HudAction.MainMenu;
        }

        return HudAction.None;
    }

    public void Draw(GameTime gameTime, Player player, List<Enemy> enemies,
        IMap map, bool gameOver, bool won, bool paused = false, bool hasNextLevel = false,
        float elapsed = 0f, float bestTime = float.MaxValue, bool isNewBest = false,
        float cameraCooldown = 0f, float cameraMaxCooldown = 5f, float sprintStamina = 1f)
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
            _healthBar.Draw(_painter, player.Health / 100f, player.Health.ToString(), 0.5f);
            _sprintBar.Draw(_painter, sprintStamina,
                sprintStamina >= 1f ? "READY" : $"{sprintStamina:F1}s");
            var chargeFrac = cameraMaxCooldown > 0 ? 1f - cameraCooldown / cameraMaxCooldown : 1f;
            _cameraBar.Draw(_painter, chargeFrac,
                chargeFrac >= 1f ? "READY" : $"{cameraCooldown:F1}s");
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
        var alpha = Math.Min(1f, _controlsTimer / 3f);
        var c = ColTextDim * alpha;
        int x = 14, y = SH / 2 - 60;
        foreach (var line in new[]
                 {
                     "W A S D  -  Move",   
                     "Shift    -  Run", 
                     "Mouse    -  Look",   
                     "RClick   -  Camera",    
                     "LClick   -  Photo",
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
        _resumeButton.Draw(mousePos,   _navPause.IsSelected(0));
        _mainMenuButton.Draw(mousePos, _navPause.IsSelected(1));
        _quitButton.Draw(mousePos,     _navPause.IsSelected(2));
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
        var tc    = won ? new Color(60, 220, 60) : new Color(220, 40, 40);
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

        if (won && hasNextLevel)
            _nextLevelButton.Draw(mousePos, _navEnd.IsSelected(0));

        var menuNavIndex = (won && hasNextLevel) ? 1 : 0;
        _mainMenuButton.Draw(mousePos, _navEnd.IsSelected(menuNavIndex));
    }

    public void Dispose()
    {
        _painter.Dispose();
    }
}
