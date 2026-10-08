#nullable enable
using DCM.Core.Input;
using DCM.Core.Localization;
using DCM.Core.Services;
using DCM.Core.UI;
using Microsoft.Xna.Framework;
using System;

namespace DCM.Core.Screens;

public class SettingsScreen : IGameScreen
{
    private enum SettingsAction
    {
        ToggleSound,
        ToggleFullscreen,
        ResetSave,
        ConfirmReset,
        CancelReset,
        Back
    }

    private const int RowW = 480;

    private readonly UIPainter _painter;
    private readonly Func<IGameScreen> _toMenu;
    private readonly Action _toggleMute;
    private readonly Action _toggleFullscreen;
    private readonly Action _resetSave;

    private readonly Button _soundRow;
    private readonly Button _fullscreenRow;
    private readonly ButtonMenu<SettingsAction> _mainMenu;
    private readonly ButtonMenu<SettingsAction> _confirmMenu;
    private bool _confirmPending;
    private Point _mousePos;

    public bool IsMouseVisible => true;

    public SettingsScreen(UIContext ui, GameSettings settings, Func<IGameScreen> toMenu,
        Action toggleMute, Action toggleFullscreen, Action resetSave)
    {
        _painter          = ui.Painter;
        _toMenu           = toMenu;
        _toggleMute       = toggleMute;
        _toggleFullscreen = toggleFullscreen;
        _resetSave        = resetSave;

        var cy       = UITheme.ScreenH / 2;
        var rowX     = UITheme.CenteredX(RowW);
        var btnX     = UITheme.CenteredX(UITheme.ButtonW);
        const int confirmW = 115, gap = 10;
        var confirmX = UITheme.CenteredX(confirmW * 2 + gap);

        _soundRow      = new ToggleButton(new Rectangle(rowX, cy - 40, RowW, UITheme.ButtonH),
            GameText.Get("Sound"), _painter, () => !settings.MuteSound);
        _fullscreenRow = new ToggleButton(new Rectangle(rowX, cy + 30, RowW, UITheme.ButtonH),
            GameText.Get("Fullscreen"), _painter, () => settings.IsFullscreen);
        var reset   = MakeButton(btnX, cy + 110, UITheme.ButtonW, "ResetSave");
        var back    = MakeButton(btnX, cy + 190, UITheme.ButtonW, "Back");
        var confirm = MakeButton(confirmX, cy + 110, confirmW, "Confirm");
        var cancel  = MakeButton(confirmX + confirmW + gap, cy + 110, confirmW, "Cancel");

        _mainMenu = new ButtonMenu<SettingsAction>(ui, SettingsAction.Back,
            (_soundRow,      SettingsAction.ToggleSound),
            (_fullscreenRow, SettingsAction.ToggleFullscreen),
            (reset,          SettingsAction.ResetSave),
            (back,           SettingsAction.Back));
        _confirmMenu = new ButtonMenu<SettingsAction>(ui, SettingsAction.CancelReset,
            (confirm, SettingsAction.ConfirmReset),
            (cancel,  SettingsAction.CancelReset),
            (back,    SettingsAction.Back));
    }

    private Button MakeButton(int x, int y, int w, string key) =>
        new(new Rectangle(x, y, w, UITheme.ButtonH), GameText.Get(key), _painter);

    public IGameScreen? Update(GameTime gameTime, InputSnapshot input)
    {
        _mousePos = input.MousePosition;

        var action = (_confirmPending ? _confirmMenu : _mainMenu).Update(gameTime, input);
        switch (action)
        {
            case SettingsAction.ToggleSound:      _toggleMute(); break;
            case SettingsAction.ToggleFullscreen: _toggleFullscreen(); break;
            case SettingsAction.ResetSave:
                _confirmPending = true;
                _confirmMenu.ResetSelection();
                break;
            case SettingsAction.ConfirmReset:
                _resetSave();
                _confirmPending = false;
                break;
            case SettingsAction.CancelReset:      _confirmPending = false; break;
            case SettingsAction.Back:             return _toMenu();
        }

        return this;
    }

    public void Draw(GameTime gameTime)
    {
        _painter.Begin();
        _painter.FillScreen(UITheme.Background);
        _painter.DrawCentered(GameText.Get("Settings"), UITheme.ScreenH / 2f - 180, UITheme.Title, 2f);

        if (_confirmPending)
        {
            _soundRow.Draw(_mousePos);
            _fullscreenRow.Draw(_mousePos);
            _confirmMenu.Draw(_mousePos);
        }
        else
        {
            _mainMenu.Draw(_mousePos);
        }

        _painter.End();
    }

    public void Dispose()
    {
    }
}
