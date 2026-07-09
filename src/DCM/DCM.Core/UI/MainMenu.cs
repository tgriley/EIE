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

    private static readonly Color ColBg = new(10, 8, 8);

    private readonly SpriteFont _titleFont;
    private readonly SpriteFont _titleFont2;
    private readonly Button _startButton;
    private readonly Button _settingsButton;
    private readonly Button _exitButton;

    public MainMenu(SpriteBatch sb, SpriteFont font, SpriteFont titleFont, SpriteFont titleFont2, GraphicsDevice gd, SoundEffect clickSound)
    {
        _painter    = new UIPainter(sb, font, gd);
        _titleFont  = titleFont;
        _titleFont2 = titleFont2;
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

        const string title1 = "Escape From Island";
        var title1Size = _painter.Measure(_titleFont, title1);
        float titleY = SH / 2f - 256;
        _painter.DrawTextShadow(_titleFont, title1,
            new Vector2((SW - title1Size.X) / 2f, titleY),
            Color.White);

        const string title2 = "Epsteinstien";
        var title2Size = _painter.Measure(_titleFont2, title2);
        _painter.DrawTextShadow(_titleFont2, title2,
            new Vector2((SW - title2Size.X) / 2f, titleY + title1Size.Y),
            Color.Red);

        _startButton.Draw(mousePos,    _nav.IsSelected(0));
        _settingsButton.Draw(mousePos, _nav.IsSelected(1));
        _exitButton.Draw(mousePos,     _nav.IsSelected(2));

        _painter.End();
    }

    public void Dispose()
    {
        _painter.Dispose();
    }
}
