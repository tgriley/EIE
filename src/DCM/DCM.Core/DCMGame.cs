using DCM.Core.Rendering;
using DCM.Core.Screens;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DCM.Core
{
    public class DCMGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch  _spriteBatch;
        private IGameScreen  _currentScreen;
        private MouseState   _prevMouse;

        public DCMGame()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _graphics.PreferredBackBufferWidth  = RaycasterRenderer.RW * 2;
            _graphics.PreferredBackBufferHeight = RaycasterRenderer.RH * 2;
            _graphics.ApplyChanges();
            Window.Title = "Babushka — Find the Exit";
        }

        protected override void Initialize() => base.Initialize();

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            var font = Content.Load<Microsoft.Xna.Framework.Graphics.SpriteFont>("Fonts/Hud");
            _currentScreen = new MenuScreen(_spriteBatch, font, GraphicsDevice,
                () => new PlayScreen(_spriteBatch, font, GraphicsDevice, Content));
        }

        protected override void Update(GameTime gameTime)
        {
            var mouse = Mouse.GetState();
            var next  = _currentScreen.Update(gameTime, mouse, _prevMouse);

            if (next == null) { Exit(); return; }

            if (!ReferenceEquals(next, _currentScreen))
            {
                _currentScreen.Dispose();
                _currentScreen = next;
            }

            IsMouseVisible = _currentScreen.IsMouseVisible;
            _prevMouse     = mouse;
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            _currentScreen.Draw(gameTime);
            base.Draw(gameTime);
        }

        protected override void UnloadContent()
        {
            _currentScreen?.Dispose();
            base.UnloadContent();
        }
    }
}
