using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DCM.Core.UI
{
    public class Button
    {
        private static readonly Color ColTextNorm  = new Color(200, 190, 170);
        private static readonly Color ColTextHov   = new Color(255, 240, 200);
        private static readonly Color ColBg        = new Color(40, 35, 30);
        private static readonly Color ColBgHov     = new Color(60, 50, 40);
        private static readonly Color ColBorder    = new Color(100, 80, 60);
        private static readonly Color ColBorderHov = new Color(160, 130, 90);

        private readonly UIPainter _painter;
        private readonly Rectangle _bounds;
        private readonly string    _label;

        public Button(Rectangle bounds, string label, UIPainter painter)
        {
            _bounds  = bounds;
            _label   = label;
            _painter = painter;
        }

        public bool IsClicked(MouseState mouse, MouseState prevMouse)
        {
            bool released = mouse.LeftButton     == ButtonState.Released &&
                            prevMouse.LeftButton == ButtonState.Pressed;
            return released && _bounds.Contains(mouse.Position);
        }

        public void Draw(Point mousePos)
        {
            bool  hover  = _bounds.Contains(mousePos);
            Color bg     = hover ? ColBgHov     : ColBg;
            Color textC  = hover ? ColTextHov   : ColTextNorm;
            Color border = hover ? ColBorderHov : ColBorder;

            _painter.DrawRect(_bounds.X - 2, _bounds.Y - 2, _bounds.Width + 4, _bounds.Height + 4, border);
            _painter.DrawRect(_bounds.X,     _bounds.Y,     _bounds.Width,     _bounds.Height,     bg);

            Vector2 size = _painter.Measure(_label);
            float tx = _bounds.X + (_bounds.Width  - size.X) / 2f;
            float ty = _bounds.Y + (_bounds.Height - size.Y) / 2f;
            _painter.DrawTextShadow(_label, new Vector2(tx, ty), textC);
        }
    }
}
