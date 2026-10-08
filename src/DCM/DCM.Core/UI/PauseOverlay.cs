#nullable enable
using DCM.Core.Input;
using DCM.Core.Localization;
using Microsoft.Xna.Framework;

namespace DCM.Core.UI;

public enum PauseAction
{
    Resume,
    MainMenu,
    Quit
}

public sealed class PauseOverlay
{
    private static readonly Color ColDim = new(0, 0, 0, 160);

    private readonly UIPainter _painter;
    private readonly ButtonMenu<PauseAction> _menu;

    public PauseOverlay(UIContext ui)
    {
        _painter = ui.Painter;
        var x = UITheme.CenteredX(UITheme.ButtonW);
        var y = UITheme.ScreenH / 2;
        _menu = new ButtonMenu<PauseAction>(ui, PauseAction.Resume,
            (MakeButton(x, y + 10,  "Resume"),   PauseAction.Resume),
            (MakeButton(x, y + 80,  "MainMenu"), PauseAction.MainMenu),
            (MakeButton(x, y + 150, "Quit"),     PauseAction.Quit));
    }

    private Button MakeButton(int x, int y, string key) =>
        new(new Rectangle(x, y, UITheme.ButtonW, UITheme.ButtonH), GameText.Get(key), _painter);

    public PauseAction? Update(GameTime gameTime, InputSnapshot input) => _menu.Update(gameTime, input);

    public void Draw(Point mousePos)
    {
        _painter.FillScreen(ColDim);
        _painter.DrawCentered(GameText.Get("Paused"), UITheme.ScreenH / 2 - 60, UITheme.Text);
        _menu.Draw(mousePos);
    }
}
