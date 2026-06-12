using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DCM.Core.UI;

public class Button
{
    private static readonly Color ColTextNorm = new(200, 190, 170);
    private static readonly Color ColTextHov = new(255, 240, 200);
    private static readonly Color ColBg = new(40, 35, 30);
    private static readonly Color ColBgHov = new(60, 50, 40);
    private static readonly Color ColBorder = new(100, 80, 60);
    private static readonly Color ColBorderHov = new(160, 130, 90);

    private readonly UIPainter _painter;
    private readonly Rectangle _bounds;
    private readonly string _label;

    public Button(Rectangle bounds, string label, UIPainter painter)
    {
        _bounds = bounds;
        _label = label;
        _painter = painter;
    }

    public bool IsClicked(MouseState mouse, MouseState prevMouse)
    {
        var released = mouse.LeftButton == ButtonState.Released &&
                       prevMouse.LeftButton == ButtonState.Pressed;
        return released && _bounds.Contains(mouse.Position);
    }

    public void Draw(Point mousePos)
    {
        var hover = _bounds.Contains(mousePos);
        var bg = hover ? ColBgHov : ColBg;
        var textC = hover ? ColTextHov : ColTextNorm;
        var border = hover ? ColBorderHov : ColBorder;

        _painter.DrawRect(_bounds.X - 2, _bounds.Y - 2, _bounds.Width + 4, _bounds.Height + 4, border);
        _painter.DrawRect(_bounds.X, _bounds.Y, _bounds.Width, _bounds.Height, bg);

        var size = _painter.Measure(_label);
        var tx = _bounds.X + (_bounds.Width - size.X) / 2f;
        var ty = _bounds.Y + (_bounds.Height - size.Y) / 2f;
        _painter.DrawTextShadow(_label, new Vector2(tx, ty), textC);
    }
}