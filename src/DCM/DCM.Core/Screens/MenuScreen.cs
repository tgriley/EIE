#nullable enable
using DCM.Core.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace DCM.Core.Screens
{
    public class MenuScreen : IGameScreen
    {
        private readonly MainMenu          _menu;
        private readonly Func<IGameScreen> _createPlayScreen;

        public bool IsMouseVisible => true;

        public MenuScreen(SpriteBatch sb, SpriteFont font, GraphicsDevice gd,
                          Func<IGameScreen> createPlayScreen)
        {
            _menu             = new MainMenu(sb, font, gd);
            _createPlayScreen = createPlayScreen;
        }

        public IGameScreen? Update(GameTime gameTime, MouseState mouse, MouseState prevMouse)
        {
            MenuAction action = _menu.Update(mouse, prevMouse);
            if (action == MenuAction.Start) return _createPlayScreen();
            if (action == MenuAction.Exit)  return null;
            return this;
        }

        public void Draw(GameTime gameTime) => _menu.Draw(gameTime);

        public void Dispose() => _menu.Dispose();
    }
}
