using DCM.Core.Input;
using Microsoft.Xna.Framework;

namespace DCM.Core.UI;

public class Button
{
    protected readonly UIPainter Painter;
    protected readonly string Label;

    public Rectangle Bounds { get; }

    public Button(Rectangle bounds, string label, UIPainter painter)
    {
        Bounds = bounds;
        Label = label;
        Painter = painter;
    }

    public bool IsClicked(InputSnapshot input) => input.LeftReleasedIn(Bounds);

    public void Draw(Point mousePos, bool selected = false)
    {
        var highlighted = selected || Bounds.Contains(mousePos);
        Painter.DrawFramedPanel(Bounds, highlighted);
        DrawContent(highlighted ? UITheme.ControlTextHover : UITheme.ControlText);
    }

    protected virtual void DrawContent(Color textColor)
    {
        var size = Painter.Measure(Label);
        var tx = Bounds.X + (Bounds.Width - size.X) / 2f;
        var ty = Bounds.Y + (Bounds.Height - size.Y) / 2f;
        Painter.DrawTextShadow(Label, new Vector2(tx, ty), textColor);
    }
}
