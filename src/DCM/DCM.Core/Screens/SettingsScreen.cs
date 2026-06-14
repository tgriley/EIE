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
    private readonly Action _resetSave;

    // index: 0=Sound, 1=Fullscreen, 2=ResetSave, 3=Back
    private readonly MenuNavigator _navNormal  = new(4);
    // index: 0=Confirm, 1=Cancel
    private readonly MenuNavigator _navConfirm = new(2);

    private const int SW   = 1280;
    private const int SH   = 720;
    private const int RowW = 480;
    private const int RowH = 52;

    private static readonly Color ColBg    = new(10, 8, 8);
    private static readonly Color ColTitle = new(220, 180, 80);

    private readonly Rectangle _soundBounds;
    private readonly Rectangle _fullscreenBounds;
    private readonly Button _resetButton;
    private readonly Button _confirmButton;
    private readonly Button _cancelButton;
    private readonly Button _backButton;
    private bool _confirmPending;

    public bool IsMouseVisible => true;

    public SettingsScreen(SpriteBatch sb, SpriteFont font, GraphicsDevice gd,
        Func<IGameScreen> toMenu, SoundEffect clickSound,
        Action toggleMute, Action toggleFullscreen, Action resetSave)
    {
        _painter          = new UIPainter(sb, font, gd);
        _clickSound       = clickSound;
        _toMenu           = toMenu;
        _toggleMute       = toggleMute;
        _toggleFullscreen = toggleFullscreen;
        _resetSave        = resetSave;

        var rowX    = (SW - RowW) / 2;
        var btnX    = (SW - 240) / 2;
        const int confirmW = 115, gap = 10;
        var confirmX = (SW - (confirmW * 2 + gap)) / 2;

        _soundBounds      = new Rectangle(rowX,                        SH / 2 - 40,  RowW,     RowH);
        _fullscreenBounds = new Rectangle(rowX,                        SH / 2 + 30,  RowW,     RowH);
        _resetButton      = new Button(new Rectangle(btnX,             SH / 2 + 110, 240,      RowH), "RESET SAVE", _painter);
        _confirmButton    = new Button(new Rectangle(confirmX,         SH / 2 + 110, confirmW, RowH), "CONFIRM",    _painter);
        _cancelButton     = new Button(new Rectangle(confirmX + confirmW + gap, SH / 2 + 110, confirmW, RowH), "CANCEL", _painter);
        _backButton       = new Button(new Rectangle(btnX,             SH / 2 + 190, 240,      RowH), "BACK",       _painter);
    }

    public IGameScreen? Update(GameTime gameTime, MouseState mouse, MouseState prevMouse)
    {
        if (!_confirmPending)
        {
            _navNormal.Update(gameTime);

            // Mouse
            if (IsReleased(_soundBounds, mouse, prevMouse))      { _clickSound.Play(); _toggleMute(); }
            if (IsReleased(_fullscreenBounds, mouse, prevMouse)) { _clickSound.Play(); _toggleFullscreen(); }
            if (_resetButton.IsClicked(mouse, prevMouse))        { _clickSound.Play(); EnterConfirm(); }
            if (_backButton.IsClicked(mouse, prevMouse))         { _clickSound.Play(); return _toMenu(); }

            // Controller
            if (_navNormal.JustConfirmed)
            {
                _clickSound.Play();
                switch (_navNormal.SelectedIndex)
                {
                    case 0: _toggleMute();       break;
                    case 1: _toggleFullscreen(); break;
                    case 2: EnterConfirm();      break;
                    case 3: return _toMenu();
                }
            }
            if (_navNormal.JustCancelled) { _clickSound.Play(); return _toMenu(); }
        }
        else
        {
            _navConfirm.Update(gameTime);

            // Mouse
            if (_confirmButton.IsClicked(mouse, prevMouse)) { _clickSound.Play(); _resetSave(); _confirmPending = false; }
            if (_cancelButton.IsClicked(mouse, prevMouse))  { _clickSound.Play(); _confirmPending = false; }

            // Controller
            if (_navConfirm.JustConfirmed)
            {
                _clickSound.Play();
                if (_navConfirm.SelectedIndex == 0) _resetSave();
                _confirmPending = false;
            }
            if (_navConfirm.JustCancelled) { _clickSound.Play(); _confirmPending = false; }
        }

        if (_backButton.IsClicked(mouse, prevMouse)) { _clickSound.Play(); return _toMenu(); }

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

        DrawToggleRow("SOUND",      !GameSettings.MuteSound,  _soundBounds,      mousePos, _navNormal.IsSelected(0));
        DrawToggleRow("FULLSCREEN", GameSettings.IsFullscreen, _fullscreenBounds, mousePos, _navNormal.IsSelected(1));

        if (!_confirmPending)
        {
            _resetButton.Draw(mousePos, _navNormal.IsSelected(2));
        }
        else
        {
            _confirmButton.Draw(mousePos, _navConfirm.IsSelected(0));
            _cancelButton.Draw(mousePos,  _navConfirm.IsSelected(1));
        }

        _backButton.Draw(mousePos, _navNormal.IsSelected(3));

        _painter.End();
    }

    private void EnterConfirm()
    {
        _confirmPending = true;
        _navConfirm.Reset();
    }

    private void DrawToggleRow(string label, bool enabled, Rectangle bounds, Point mousePos, bool selected = false)
    {
        var hover     = selected || bounds.Contains(mousePos);
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
