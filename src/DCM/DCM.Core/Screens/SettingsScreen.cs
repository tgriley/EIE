#nullable enable
using DCM.Core.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace DCM.Core.Screens;

public class SettingsScreen : IGameScreen
{
    private readonly UIPainter _painter;
    private readonly SoundEffect _clickSound;
    private readonly Func<IGameScreen> _toMenu;
    private readonly Action _toggleMute;
    private readonly Action _toggleFullscreen;

    private const int SW = 1280;
    private const int SH = 720;
    private const int RowW = 480;
    private const int RowH = 52;

    private static readonly Color ColBg    = new(10, 8, 8);
    private static readonly Color ColTitle = new(220, 180, 80);

    private readonly Rectangle _soundBounds;
    private readonly Rectangle _fullscreenBounds;
    private readonly Button _backButton;

    public bool IsMouseVisible => true;

    public SettingsScreen(SpriteBatch sb, SpriteFont font, GraphicsDevice gd,
        Func<IGameScreen> toMenu, SoundEffect clickSound,
        Action toggleMute, Action toggleFullscreen)
    {
        _painter          = new UIPainter(sb, font, gd);
        _clickSound       = clickSound;
        _toMenu           = toMenu;
        _toggleMute       = toggleMute;
        _toggleFullscreen = toggleFullscreen;

        var rowX = (SW - RowW) / 2;
        _soundBounds      = new Rectangle(rowX, SH / 2 - 40, RowW, RowH);
        _fullscreenBounds = new Rectangle(rowX, SH / 2 + 30, RowW, RowH);
        _backButton       = new Button(
            new Rectangle((SW - 240) / 2, SH / 2 + 130, 240, RowH), "BACK", _painter);
    }

    public IGameScreen? Update(GameTime gameTime, MouseState mouse, MouseState prevMouse)
    {
        if (IsReleased(_soundBounds, mouse, prevMouse))
        {
            _clickSound.Play();
            _toggleMute();
        }
        if (IsReleased(_fullscreenBounds, mouse, prevMouse))
        {
            _clickSound.Play();
            _toggleFullscreen();
        }
        if (_backButton.IsClicked(mouse, prevMouse))
        {
            _clickSound.Play();
            return _toMenu();
        }
        return this;
    }

    public void Draw(GameTime gameTime)
    {
        var mousePos = Mouse.GetState().Position;
        _painter.Begin();

        _painter.DrawRect(0, 0, SW, SH, ColBg);

        const string title = "SETTINGS";
        var titleSize = _painter.Measure(title);
        _painter.DrawTextShadow(title,
            new Vector2((SW - titleSize.X * 2f) / 2f, SH / 2f - 180),
            ColTitle, 2f);

        DrawToggleRow("SOUND",      !GameSettings.MuteSound,    _soundBounds,      mousePos);
        DrawToggleRow("FULLSCREEN", GameSettings.IsFullscreen,   _fullscreenBounds, mousePos);

        _backButton.Draw(mousePos);

        _painter.End();
    }

    private void DrawToggleRow(string label, bool enabled, Rectangle bounds, Point mousePos)
    {
        var hover     = bounds.Contains(mousePos);
        var bgCol     = hover ? new Color(60,  50,  40) : new Color(40,  35, 30);
        var borderCol = hover ? new Color(160, 130, 90) : new Color(100, 80, 60);
        var labelCol  = hover ? new Color(255, 240, 200) : new Color(200, 190, 170);

        _painter.DrawRect(bounds.X - 2, bounds.Y - 2, bounds.Width + 4, bounds.Height + 4, borderCol);
        _painter.DrawRect(bounds.X, bounds.Y, bounds.Width, bounds.Height, bgCol);

        var charH = _painter.Measure("A").Y;
        var textY = bounds.Y + (bounds.Height - charH) / 2f;

        _painter.DrawTextShadow(label, new Vector2(bounds.X + 20, textY), labelCol);

        var valText  = enabled ? "ON" : "OFF";
        var valColor = enabled ? new Color(80, 210, 80) : new Color(200, 80, 60);
        var valW     = _painter.Measure(valText).X;
        _painter.DrawTextShadow(valText, new Vector2(bounds.Right - valW - 20, textY), valColor);
    }

    private static bool IsReleased(Rectangle bounds, MouseState mouse, MouseState prevMouse) =>
        mouse.LeftButton == ButtonState.Released &&
        prevMouse.LeftButton == ButtonState.Pressed &&
        bounds.Contains(mouse.Position);

    public void Dispose()
    {
        _painter.Dispose();
    }
}
