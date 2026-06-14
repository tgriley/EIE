using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace DCM.Core.UI;

public enum MenuAction
{
    None,
    Start,
    Settings,
    Exit
}

public class MainMenu : IDisposable
{
    private readonly UIPainter _painter;
    private readonly SoundEffect _clickSound;
    private readonly MenuNavigator _nav = new(3);

    private const int SW = 1280;
    private const int SH = 720;

    private static readonly Color ColBg    = new(10, 8, 8);
    private static readonly Color ColTitle = new(220, 180, 80);

    private readonly Button _startButton;
    private readonly Button _settingsButton;
    private readonly Button _exitButton;

    public MainMenu(SpriteBatch sb, SpriteFont font, GraphicsDevice gd, SoundEffect clickSound)
    {
        _painter    = new UIPainter(sb, font, gd);
        _clickSound = clickSound;

        int btnW = 240, btnH = 52, btnX = (SW - 240) / 2;
        _startButton    = new Button(new Rectangle(btnX, SH / 2 + 20,  btnW, btnH), "START",    _painter);
        _settingsButton = new Button(new Rectangle(btnX, SH / 2 + 90,  btnW, btnH), "SETTINGS", _painter);
        _exitButton     = new Button(new Rectangle(btnX, SH / 2 + 160, btnW, btnH), "EXIT",     _painter);
    }

    public MenuAction Update(GameTime gameTime, MouseState mouse, MouseState prevMouse)
    {
        _nav.Update(gameTime);

        if (_startButton.IsClicked(mouse, prevMouse))    { _clickSound.Play(); return MenuAction.Start; }
        if (_settingsButton.IsClicked(mouse, prevMouse)) { _clickSound.Play(); return MenuAction.Settings; }
        if (_exitButton.IsClicked(mouse, prevMouse))     { _clickSound.Play(); return MenuAction.Exit; }

        if (_nav.JustConfirmed)
        {
            _clickSound.Play();
            return _nav.SelectedIndex switch
            {
                0 => MenuAction.Start,
                1 => MenuAction.Settings,
                _ => MenuAction.Exit
            };
        }

        return MenuAction.None;
    }

    public void Draw(GameTime gameTime)
    {
        var mousePos = Mouse.GetState().Position;

        _painter.Begin();

        _painter.DrawRect(0, 0, SW, SH, ColBg);

        const string title = "Escape From Island Epsteinstien";
        var titleScale = 2f;
        var titleSize = _painter.Measure(title);
        _painter.DrawTextShadow(title,
            new Vector2((SW - titleSize.X * titleScale) / 2f, SH / 2f - 180),
            ColTitle, titleScale);

        _startButton.Draw(mousePos,    _nav.IsSelected(0));
        _settingsButton.Draw(mousePos, _nav.IsSelected(1));
        _exitButton.Draw(mousePos,     _nav.IsSelected(2));

        const string hint = "W A S D + Mouse to play   |   Esc to pause";
        var hintSize = _painter.Measure(hint);
        _painter.DrawTextShadow(hint,
            new Vector2((SW - hintSize.X * 0.75f) / 2f, SH - 50),
            new Color(80, 70, 60), 0.75f);

        _painter.End();
    }

    public void Dispose()
    {
        _painter.Dispose();
    }
}
