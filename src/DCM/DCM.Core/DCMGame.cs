#nullable enable
using DCM.Core.Audio;
using DCM.Core.Entities;
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
    // The whole game is drawn at this fixed logical resolution and then scaled
    // up to the window, so all screen layout can assume a 1280x720 canvas.
    public const int LogicalW = RaycasterRenderer.RW * 2;
    public const int LogicalH = RaycasterRenderer.RH * 2;
    private const int WindowW = 1920;
    private const int WindowH = 1080;

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private RenderTarget2D _screen = null!;
    private IGameScreen _currentScreen = null!;
    private SoundEffect _clickSound = null!;
    private PlaySounds _playSounds = null!;
    private MouseState _prevMouse;

    public DCMGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        _graphics.PreferredBackBufferWidth = WindowW;
        _graphics.PreferredBackBufferHeight = WindowH;
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
        _screen = new RenderTarget2D(GraphicsDevice, LogicalW, LogicalH);
        var font      = Content.Load<SpriteFont>("Fonts/Hud");
        var titleFont  = Content.Load<SpriteFont>("Fonts/MenuTitle");
        var titleFont2 = Content.Load<SpriteFont>("Fonts/MenuTitle2");
        _clickSound = SoundSynth.CreateClick();
        _playSounds = SoundSynth.CreatePlaySounds();

        IGameScreen CreateMenu() =>
            new MenuScreen(_spriteBatch, font, titleFont, titleFont2, GraphicsDevice, CreateModeSelect, CreateSettings, _clickSound);

        IGameScreen CreateModeSelect() =>
            new ModeSelectScreen(_spriteBatch, font, GraphicsDevice, CreateIntro, CreateEndlessRun, CreateMenu, _clickSound);

        // Each run gets a fresh base seed; stage N within a run is
        // deterministic so a "next level" retry after death would rebuild the
        // same map for the same run.
        IGameScreen CreateEndlessRun() =>
            CreateEndlessStage(0, Random.Shared.Next(), 100, new PlayerBuffs());

        // Completing a stage goes through a buff pick before the next stage;
        // the shared PlayerBuffs accumulates for the whole run.
        IGameScreen CreateEndlessStage(int stage, int runSeed, int health, PlayerBuffs buffs) =>
            new PlayScreen(_spriteBatch, font, GraphicsDevice, Content,
                stage, CreateMenu,
                h => new BuffSelectScreen(_spriteBatch, font, GraphicsDevice, buffs, h,
                    h2 => CreateEndlessStage(stage + 1, runSeed, h2, buffs), _clickSound),
                _clickSound, _playSounds, health,
                MapGenerator.Generate(stage, runSeed + stage), endless: true, buffs: buffs);

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

        IGameScreen CreatePlay(int levelIndex, int startHealth = 100, PlayerBuffs? buffs = null)
        {
            var runBuffs = buffs ?? new PlayerBuffs();
            Func<int, IGameScreen>? nextLevel = levelIndex < Map.LevelCount - 1
                ? health => new BuffSelectScreen(_spriteBatch, font, GraphicsDevice, runBuffs, health,
                    h => CreatePlay(levelIndex + 1, h, runBuffs), _clickSound)
                : null;
            return new PlayScreen(_spriteBatch, font, GraphicsDevice, Content,
                levelIndex, CreateMenu, nextLevel, _clickSound, _playSounds, startHealth, buffs: runBuffs);
        }

        _currentScreen = CreateMenu();
    }

    protected override void Update(GameTime gameTime)
    {
        var mouse = ToLogical(Mouse.GetState());
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

    protected override void OnDeactivated(object sender, EventArgs args)
    {
        (_currentScreen as PlayScreen)?.OnFocusLost();
        base.OnDeactivated(sender, args);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.SetRenderTarget(_screen);
        GraphicsDevice.Clear(Color.Black);
        _currentScreen.Draw(gameTime);

        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);
        var pp = GraphicsDevice.PresentationParameters;
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp);
        _spriteBatch.Draw(_screen, new Rectangle(0, 0, pp.BackBufferWidth, pp.BackBufferHeight), Color.White);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    // Map a raw window-space mouse position into the fixed logical canvas so
    // every screen's hit-testing works regardless of window size.
    private MouseState ToLogical(MouseState raw)
    {
        var pp = GraphicsDevice.PresentationParameters;
        var lx = raw.X * LogicalW / pp.BackBufferWidth;
        var ly = raw.Y * LogicalH / pp.BackBufferHeight;
        return new MouseState(lx, ly, raw.ScrollWheelValue,
            raw.LeftButton, raw.MiddleButton, raw.RightButton, raw.XButton1, raw.XButton2);
    }

    protected override void UnloadContent()
    {
        _currentScreen?.Dispose();
        _screen?.Dispose();
        _clickSound?.Dispose();
        _playSounds?.Dispose();
        base.UnloadContent();
    }
}
