using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace DCM.Core.UI
{
    public enum MenuAction { None, Start, Exit }

    public class MainMenu : IDisposable
    {
        private readonly UIPainter _painter;

        private const int SW = 1280;
        private const int SH = 720;

        private static readonly Color ColBg    = new Color(10, 8, 8);
        private static readonly Color ColTitle = new Color(220, 180, 80);
        private static readonly Color ColSub   = new Color(160, 140, 110);
        private static readonly Color ColSep   = new Color(100, 80, 60);

        private readonly Button _startButton;
        private readonly Button _exitButton;

        public MainMenu(SpriteBatch sb, SpriteFont font, GraphicsDevice gd)
        {
            _painter = new UIPainter(sb, font, gd);

            int btnW = 240, btnH = 52, btnX = (SW - 240) / 2;
            _startButton = new Button(new Rectangle(btnX, SH / 2 + 20, btnW, btnH), "START", _painter);
            _exitButton  = new Button(new Rectangle(btnX, SH / 2 + 90, btnW, btnH), "EXIT",  _painter);
        }

        public MenuAction Update(MouseState mouse, MouseState prevMouse)
        {
            if (_startButton.IsClicked(mouse, prevMouse)) return MenuAction.Start;
            if (_exitButton.IsClicked(mouse, prevMouse))  return MenuAction.Exit;
            return MenuAction.None;
        }

        public void Draw(GameTime gameTime)
        {
            Point mousePos = Mouse.GetState().Position;

            _painter.Begin();

            _painter.DrawRect(0, 0, SW, SH, ColBg);

            const string title = "BABUSHKA";
            float   titleScale = 2f;
            Vector2 titleSize  = _painter.Measure(title);
            _painter.DrawTextShadow(title,
                new Vector2((SW - titleSize.X * titleScale) / 2f, SH / 2f - 180),
                ColTitle, titleScale);

            const string sub = "Find the Exit";
            Vector2 subSize = _painter.Measure(sub);
            _painter.DrawTextShadow(sub,
                new Vector2((SW - subSize.X) / 2f, SH / 2f - 90),
                ColSub);

            _painter.DrawRect((SW - 300) / 2, SH / 2 - 20, 300, 1, ColSep);

            _startButton.Draw(mousePos);
            _exitButton.Draw(mousePos);

            const string hint = "W A S D + Mouse to play   |   Esc to pause";
            Vector2 hintSize = _painter.Measure(hint);
            _painter.DrawTextShadow(hint,
                new Vector2((SW - hintSize.X * 0.75f) / 2f, SH - 50),
                new Color(80, 70, 60), 0.75f);

            _painter.End();
        }

        public void Dispose() => _painter.Dispose();
    }
}
