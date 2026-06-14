#nullable enable
using DCM.Core.Rendering;
using DCM.Core.Screens;
using DCM.Core.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace DCM.Core;

public class DCMGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private IGameScreen _currentScreen = null!;
    private MouseState _prevMouse;

    public DCMGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        _graphics.PreferredBackBufferWidth = RaycasterRenderer.RW * 2;
        _graphics.PreferredBackBufferHeight = RaycasterRenderer.RH * 2;
        _graphics.ApplyChanges();
        Window.Title = "Find the Exit";
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        var font = Content.Load<SpriteFont>("Fonts/Hud");

        IGameScreen CreateMenu() =>
            new MenuScreen(_spriteBatch, font, GraphicsDevice, CreateLevelSelect);

        IGameScreen CreateLevelSelect() =>
            new LevelSelectScreen(_spriteBatch, font, GraphicsDevice, CreatePlay, CreateMenu);

        IGameScreen CreatePlay(int levelIndex)
        {
            Func<IGameScreen>? nextLevel = levelIndex < Map.LevelCount - 1
                ? () => CreatePlay(levelIndex + 1)
                : null;
            return new PlayScreen(_spriteBatch, font, GraphicsDevice, Content,
                levelIndex, CreateLevelSelect, nextLevel);
        }

        _currentScreen = CreateMenu();
    }

    protected override void Update(GameTime gameTime)
    {
        var mouse = Mouse.GetState();
        var next = _currentScreen.Update(gameTime, mouse, _prevMouse);

        if (next == null)
        {
            Exit();
            return;
        }

        if (!ReferenceEquals(next, _currentScreen))
        {
            _currentScreen.Dispose();
            _currentScreen = next;
        }

        IsMouseVisible = _currentScreen.IsMouseVisible;
        _prevMouse = mouse;
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