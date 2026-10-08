#nullable enable
using DCM.Core.Input;
using DCM.Core.Localization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DCM.Core.UI;

public enum MenuAction
{
    Start,
    Settings,
    Exit
}

public class MainMenu
{
    private const string Title = "Escape From Island";
    private const string Subtitle = "Epsteinstien";

    private readonly UIPainter _painter;
    private readonly SpriteFont _titleFont;
    private readonly SpriteFont _subtitleFont;
    private readonly ButtonMenu<MenuAction> _menu;
    private Point _mousePos;

    public MainMenu(UIContext ui, SpriteFont titleFont, SpriteFont subtitleFont)
    {
        _painter      = ui.Painter;
        _titleFont    = titleFont;
        _subtitleFont = subtitleFont;

        var x = UITheme.CenteredX(UITheme.ButtonW);
        var y = UITheme.ScreenH / 2;
        _menu = new ButtonMenu<MenuAction>(ui, null,
            (MakeButton(x, y + 20,  "Start"),    MenuAction.Start),
            (MakeButton(x, y + 90,  "Settings"), MenuAction.Settings),
            (MakeButton(x, y + 160, "Exit"),     MenuAction.Exit));
    }

    private Button MakeButton(int x, int y, string key) =>
        new(new Rectangle(x, y, UITheme.ButtonW, UITheme.ButtonH), GameText.Get(key), _painter);

    public MenuAction? Update(GameTime gameTime, InputSnapshot input)
    {
        _mousePos = input.MousePosition;
        return _menu.Update(gameTime, input);
    }

    public void Draw()
    {
        _painter.Begin();
        _painter.FillScreen(UITheme.Background);

        var titleSize = _painter.Measure(_titleFont, Title);
        var titleY = UITheme.ScreenH / 2f - 256;
        _painter.DrawTextShadow(_titleFont, Title,
            new Vector2((UITheme.ScreenW - titleSize.X) / 2f, titleY), Color.White);

        var subtitleSize = _painter.Measure(_subtitleFont, Subtitle);
        _painter.DrawTextShadow(_subtitleFont, Subtitle,
            new Vector2((UITheme.ScreenW - subtitleSize.X) / 2f, titleY + titleSize.Y), Color.Red);

        _menu.Draw(_mousePos);

        _painter.End();
    }
}
