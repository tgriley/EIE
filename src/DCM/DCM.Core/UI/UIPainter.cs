using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace DCM.Core.UI
{
    public class UIPainter : IDisposable
    {
        private readonly SpriteBatch _sb;
        private readonly SpriteFont  _font;
        private readonly Texture2D   _pixel;

        public UIPainter(SpriteBatch sb, SpriteFont font, GraphicsDevice gd)
        {
            _sb    = sb;
            _font  = font;
            _pixel = new Texture2D(gd, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }

        public void Begin()
            => _sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

        public void End() => _sb.End();

        public void DrawRect(int x, int y, int w, int h, Color c)
            => _sb.Draw(_pixel, new Rectangle(x, y, w, h), c);

        public void DrawTextShadow(string text, Vector2 pos, Color color, float scale = 1f)
        {
            Color shadow = new Color(0, 0, 0, (int)color.A);
            _sb.DrawString(_font, text, pos + new Vector2(2, 2),
                           shadow, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            _sb.DrawString(_font, text, pos,
                           color,  0, Vector2.Zero, scale, SpriteEffects.None, 0);
        }

        public Vector2 Measure(string text) => _font.MeasureString(text);

        public void DrawLine(int x0, int y0, int x1, int y1, Color c)
        {
            int dx = Math.Abs(x1 - x0), dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            while (true)
            {
                DrawRect(x0, y0, 1, 1, c);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 <  dx) { err += dx; y0 += sy; }
            }
        }

        public void Dispose() => _pixel.Dispose();
    }
}
