using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace DCM.Core.UI
{
    public enum MenuAction { None, Start, Exit }

    public class MainMenu : IDisposable
    {
        private readonly SpriteBatch _sb;
        private readonly SpriteFont  _font;
        private readonly Texture2D   _pixel;

        private const int SW = 1280;
        private const int SH = 720;

        private static readonly Color ColBg      = new Color(10, 8, 8);
        private static readonly Color ColTitle   = new Color(220, 180, 80);
        private static readonly Color ColSub     = new Color(160, 140, 110);
        private static readonly Color ColBtnNorm = new Color(200, 190, 170);
        private static readonly Color ColBtnHov  = new Color(255, 240, 200);
        private static readonly Color ColBtnBg   = new Color(40, 35, 30);
        private static readonly Color ColBtnBgH  = new Color(60, 50, 40);
        private static readonly Color ColBorder  = new Color(100, 80, 60);
        private static readonly Color ColBorderH = new Color(160, 130, 90);

        private readonly Rectangle _startRect;
        private readonly Rectangle _exitRect;

        public MainMenu(SpriteBatch sb, SpriteFont font, GraphicsDevice gd)
        {
            _sb    = sb;
            _font  = font;
            _pixel = new Texture2D(gd, 1, 1);
            _pixel.SetData(new[] { Color.White });

            int btnW = 240;
            int btnH = 52;
            int btnX = (SW - btnW) / 2;
            _startRect = new Rectangle(btnX, SH / 2 + 20, btnW, btnH);
            _exitRect  = new Rectangle(btnX, SH / 2 + 90, btnW, btnH);
        }

        public MenuAction Update(MouseState mouse, MouseState prevMouse)
        {
            bool clicked = mouse.LeftButton     == ButtonState.Released &&
                           prevMouse.LeftButton == ButtonState.Pressed;

            if (clicked)
            {
                Point p = mouse.Position;
                if (_startRect.Contains(p)) return MenuAction.Start;
                if (_exitRect.Contains(p))  return MenuAction.Exit;
            }
            return MenuAction.None;
        }

        public void Draw(GameTime gameTime)
        {
            Point mousePos = Mouse.GetState().Position;

            _sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                      SamplerState.PointClamp, null, null);

            DrawRect(0, 0, SW, SH, ColBg);

            // Title
            const string title = "BABUSHKA";
            Vector2 titleMeas = _font.MeasureString(title);
            float   titleScale = 2f;
            Vector2 titlePos = new Vector2(
                (SW - titleMeas.X * titleScale) / 2f,
                SH / 2f - 180);
            DrawTextShadow(title, titlePos, ColTitle, titleScale);

            // Subtitle
            const string sub = "Find the Exit";
            Vector2 subMeas = _font.MeasureString(sub);
            Vector2 subPos  = new Vector2((SW - subMeas.X) / 2f, SH / 2f - 90);
            DrawTextShadow(sub, subPos, ColSub);

            // Separator
            DrawRect((SW - 300) / 2, SH / 2 - 20, 300, 1, ColBorder);

            DrawButton(_startRect, "START", mousePos);
            DrawButton(_exitRect,  "EXIT",  mousePos);

            // Controls hint at bottom
            const string hint = "W A S D + Mouse to play   |   Esc to pause";
            Vector2 hintMeas = _font.MeasureString(hint);
            DrawTextShadow(hint,
                           new Vector2((SW - hintMeas.X * 0.75f) / 2f, SH - 50),
                           new Color(80, 70, 60), 0.75f);

            _sb.End();
        }

        private void DrawButton(Rectangle rect, string label, Point mousePos)
        {
            bool  hover  = rect.Contains(mousePos);
            Color bg     = hover ? ColBtnBgH  : ColBtnBg;
            Color textC  = hover ? ColBtnHov  : ColBtnNorm;
            Color border = hover ? ColBorderH : ColBorder;

            DrawRect(rect.X - 2, rect.Y - 2, rect.Width + 4, rect.Height + 4, border);
            DrawRect(rect.X,     rect.Y,     rect.Width,     rect.Height,     bg);

            Vector2 textMeas = _font.MeasureString(label);
            float tx = rect.X + (rect.Width  - textMeas.X) / 2f;
            float ty = rect.Y + (rect.Height - textMeas.Y) / 2f;
            DrawTextShadow(label, new Vector2(tx, ty), textC);
        }

        private void DrawRect(int x, int y, int w, int h, Color c)
            => _sb.Draw(_pixel, new Rectangle(x, y, w, h), c);

        private void DrawTextShadow(string text, Vector2 pos, Color color, float scale = 1f)
        {
            Color shadow = new Color(0, 0, 0, (int)color.A);
            _sb.DrawString(_font, text, pos + new Vector2(2, 2),
                           shadow, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            _sb.DrawString(_font, text, pos,
                           color,  0, Vector2.Zero, scale, SpriteEffects.None, 0);
        }

        public void Dispose() => _pixel.Dispose();
    }
}
