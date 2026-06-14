#nullable enable
using DCM.Core;
using DCM.Core.UI;
using DCM.Core.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace DCM.Core.Screens;

public class LevelSelectScreen : IGameScreen
{
    private readonly UIPainter _painter;
    private readonly Button[] _levelButtons;
    private readonly Rectangle[] _levelRects;
    private readonly Button _backButton;
    private readonly Func<int, IGameScreen> _createPlay;
    private readonly Func<IGameScreen> _createMenu;

    public bool IsMouseVisible => true;

    private const int SW = 1280;
    private const int SH = 720;

    private static readonly Color ColBg = new(10, 8, 8);
    private static readonly Color ColTitle = new(220, 180, 80);
    private static readonly Color ColLockedBg = new(60, 55, 50);
    private static readonly Color ColLockedBorder = new(50, 45, 40);
    private static readonly Color ColLockedText = new(80, 75, 70);

    public LevelSelectScreen(SpriteBatch sb, SpriteFont font, GraphicsDevice gd, Func<int, IGameScreen> createPlay, Func<IGameScreen> createMenu)
    {
        _painter = new UIPainter(sb, font, gd);
        _createPlay = createPlay;
        _createMenu = createMenu;

        const int cols = 5;
        const int btnW = 200, btnH = 60, gapX = 20, gapY = 24;
        int gridW = cols * btnW + (cols - 1) * gapX;
        int startX = (SW - gridW) / 2;
        int startY = SH / 2 - 80;

        _levelButtons = new Button[Map.LevelCount];
        _levelRects = new Rectangle[Map.LevelCount];
        for (var i = 0; i < Map.LevelCount; i++)
        {
            var col = i % cols;
            var row = i / cols;
            var rect = new Rectangle(startX + col * (btnW + gapX), startY + row * (btnH + gapY), btnW, btnH);
            _levelRects[i] = rect;
            _levelButtons[i] = new Button(rect, $"LEVEL {i + 1}", _painter);
        }

        int backW = 160, backH = 48;
        _backButton = new Button(new Rectangle((SW - backW) / 2, SH - 80, backW, backH), "BACK", _painter);
    }

    public IGameScreen? Update(GameTime gameTime, MouseState mouse, MouseState prevMouse)
    {
        if (_backButton.IsClicked(mouse, prevMouse)) return _createMenu();

        for (var i = 0; i < _levelButtons.Length; i++)
        {
            if (LevelProgress.IsUnlocked(i) && _levelButtons[i].IsClicked(mouse, prevMouse))
                return _createPlay(i);
        }

        return this;
    }

    public void Draw(GameTime gameTime)
    {
        var mousePos = Mouse.GetState().Position;

        _painter.Begin();

        _painter.DrawRect(0, 0, SW, SH, ColBg);

        const string title = "SELECT LEVEL";
        var titleScale = 1.8f;
        var titleSize = _painter.Measure(title);
        _painter.DrawTextShadow(title,
            new Vector2((SW - titleSize.X * titleScale) / 2f, 80),
            ColTitle, titleScale);

        for (var i = 0; i < _levelButtons.Length; i++)
        {
            if (LevelProgress.IsUnlocked(i))
            {
                _levelButtons[i].Draw(mousePos);
                if (LevelProgress.HasBestTime(i))
                {
                    var r = _levelRects[i];
                    var timeStr = LevelProgress.FormatTime(LevelProgress.GetBestTime(i));
                    var ts = _painter.Measure($"Record: {timeStr}");
                    _painter.DrawTextShadow(timeStr,
                        new Vector2(r.X + (r.Width - ts.X * 0.7f) / 2f, r.Bottom + 4),
                        new Color(180, 160, 80), 0.7f);
                }
            }
            else
            {
                DrawLockedButton(i + 1, _levelRects[i]);
            }
        }

        _backButton.Draw(mousePos);

        _painter.End();
    }

    private void DrawLockedButton(int levelNum, Rectangle r)
    {
        _painter.DrawRect(r.X - 2, r.Y - 2, r.Width + 4, r.Height + 4, ColLockedBorder);
        _painter.DrawRect(r.X, r.Y, r.Width, r.Height, ColLockedBg);
        var label = $"LEVEL {levelNum}";
        var size = _painter.Measure(label);
        _painter.DrawTextShadow(label,
            new Vector2(r.X + (r.Width - size.X) / 2f, r.Y + (r.Height - size.Y) / 2f),
            ColLockedText);
    }

    public void Dispose()
    {
        _painter.Dispose();
    }
}
