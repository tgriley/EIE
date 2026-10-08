using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace DCM.Core.UI;

public class UIPainter : IDisposable
{
    private readonly SpriteBatch _sb;
    private readonly SpriteFont _font;
    private readonly Texture2D _pixel;

    public UIPainter(SpriteBatch sb, SpriteFont font, GraphicsDevice gd)
    {
        _sb = sb;
        _font = font;
        _pixel = new Texture2D(gd, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    public void Begin()
    {
        _sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
    }

    public void End()
    {
        _sb.End();
    }

    public void DrawRect(int x, int y, int w, int h, Color c)
    {
        _sb.Draw(_pixel, new Rectangle(x, y, w, h), c);
    }

    public void DrawRect(Rectangle r, Color c) => DrawRect(r.X, r.Y, r.Width, r.Height, c);

    public void FillScreen(Color c) => DrawRect(0, 0, UITheme.ScreenW, UITheme.ScreenH, c);

    public void DrawFramedPanel(Rectangle r, bool highlighted)
    {
        DrawRect(r.X - 2, r.Y - 2, r.Width + 4, r.Height + 4, highlighted ? UITheme.BorderHover : UITheme.Border);
        DrawRect(r, highlighted ? UITheme.ControlBgHover : UITheme.ControlBg);
    }

    public void DrawTextShadow(string text, Vector2 pos, Color color, float scale = 1f)
        => DrawTextShadow(_font, text, pos, color, scale);

    public void DrawTextShadow(SpriteFont font, string text, Vector2 pos, Color color, float scale = 1f)
    {
        var shadow = new Color(0, 0, 0, (int)color.A);
        _sb.DrawString(font, text, pos + new Vector2(2, 2),
            shadow, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        _sb.DrawString(font, text, pos,
            color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
    }

    public void DrawCentered(string text, float y, Color color, float scale = 1f)
        => DrawCenteredIn(new Rectangle(0, 0, UITheme.ScreenW, UITheme.ScreenH), text, y, color, scale);

    public void DrawCenteredIn(Rectangle r, string text, float y, Color color, float scale = 1f)
    {
        var width = Measure(text).X * scale;
        DrawTextShadow(text, new Vector2(r.X + (r.Width - width) / 2f, y), color, scale);
    }

    public Vector2 Measure(string text) => _font.MeasureString(text);

    public Vector2 Measure(SpriteFont font, string text) => font.MeasureString(text);

    public void DrawLine(int x0, int y0, int x1, int y1, Color c)
    {
        int dx = Math.Abs(x1 - x0), dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
        var err = dx - dy;
        while (true)
        {
            DrawRect(x0, y0, 1, 1, c);
            if (x0 == x1 && y0 == y1) break;
            var e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    public void Dispose()
    {
        _pixel.Dispose();
    }
}
