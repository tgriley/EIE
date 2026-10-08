#nullable enable
using DCM.Core.Input;
using DCM.Core.Localization;
using Microsoft.Xna.Framework;

namespace DCM.Core.UI;

public enum EndAction
{
    NextLevel,
    MainMenu
}

// Shown after escaping or dying. Escaping offers NEXT LEVEL above MAIN MENU;
// dying offers only MAIN MENU, in the same slot NEXT LEVEL would occupy.
public sealed class EndOverlay
{
    private static readonly Color ColDim  = new(0, 0, 0, 180);
    private static readonly Color ColWon  = new(60, 220, 60);
    private static readonly Color ColLost = new(220, 40, 40);

    private readonly UIPainter _painter;
    private readonly ButtonMenu<EndAction> _wonMenu;
    private readonly ButtonMenu<EndAction> _lostMenu;

    public EndOverlay(UIContext ui)
    {
        _painter = ui.Painter;
        var x = UITheme.CenteredX(UITheme.ButtonW);
        var y = UITheme.ScreenH / 2;
        _wonMenu = new ButtonMenu<EndAction>(ui, null,
            (MakeButton(x, y + 10, "NextLevel"), EndAction.NextLevel),
            (MakeButton(x, y + 80, "MainMenu"),  EndAction.MainMenu));
        _lostMenu = new ButtonMenu<EndAction>(ui, null,
            (MakeButton(x, y + 80, "MainMenu"),  EndAction.MainMenu));
    }

    private Button MakeButton(int x, int y, string key) =>
        new(new Rectangle(x, y, UITheme.ButtonW, UITheme.ButtonH), GameText.Get(key), _painter);

    public EndAction? Update(GameTime gameTime, InputSnapshot input, bool won) =>
        (won ? _wonMenu : _lostMenu).Update(gameTime, input);

    public void Draw(Point mousePos, bool won, float elapsed)
    {
        _painter.FillScreen(ColDim);

        var cy = UITheme.ScreenH / 2;
        _painter.DrawCentered(GameText.Get(won ? "YouEscaped" : "YouDied"), cy - 120, won ? ColWon : ColLost);
        if (won)
            _painter.DrawCentered($"{GameText.Get("Time")}  {TimeFormat.Format(elapsed)}", cy - 60, UITheme.Text);

        (won ? _wonMenu : _lostMenu).Draw(mousePos);
    }
}
