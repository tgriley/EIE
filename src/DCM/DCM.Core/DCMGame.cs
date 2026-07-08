#nullable enable
using DCM.Core.Audio;
using DCM.Core.Rendering;
using DCM.Core.Screens;
using DCM.Core.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace DCM.Core;

public class DCMGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private IGameScreen _currentScreen = null!;
    private SoundEffect _clickSound = null!;
    private PlaySounds _playSounds = null!;
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
        SaveManager.Load();
        if (GameSettings.IsFullscreen)
        {
            _graphics.IsFullScreen = true;
            _graphics.ApplyChanges();
        }

        _spriteBatch = new SpriteBatch(GraphicsDevice);
        var font      = Content.Load<SpriteFont>("Fonts/Hud");
        var titleFont  = Content.Load<SpriteFont>("Fonts/MenuTitle");
        var titleFont2 = Content.Load<SpriteFont>("Fonts/MenuTitle2");
        _clickSound = SoundSynth.CreateClick();
        _playSounds = SoundSynth.CreatePlaySounds();

        IGameScreen CreateMenu() =>
            new MenuScreen(_spriteBatch, font, titleFont, titleFont2, GraphicsDevice, CreateIntro, CreateSettings, _clickSound);

        IGameScreen CreateIntro() =>
            new IntroScreen(_spriteBatch, font, titleFont, GraphicsDevice, Content,
                () => CreatePlay(0), CreateMenu, _clickSound);

        IGameScreen CreateSettings() =>
            new SettingsScreen(_spriteBatch, font, GraphicsDevice, CreateMenu, _clickSound,
                GameSettings.ToggleMute,
                () =>
                {
                    GameSettings.ToggleFullscreen();
                    _graphics.IsFullScreen = GameSettings.IsFullscreen;
                    _graphics.ApplyChanges();
                },
                LevelProgress.Reset);

        IGameScreen CreatePlay(int levelIndex, int startHealth = 100)
        {
            Func<int, IGameScreen>? nextLevel = levelIndex < Map.LevelCount - 1
                ? health => CreatePlay(levelIndex + 1, health)
                : null;
            return new PlayScreen(_spriteBatch, font, GraphicsDevice, Content,
                levelIndex, CreateMenu, nextLevel, _clickSound, _playSounds, startHealth);
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
        _clickSound?.Dispose();
        _playSounds?.Dispose();
        base.UnloadContent();
    }
}
