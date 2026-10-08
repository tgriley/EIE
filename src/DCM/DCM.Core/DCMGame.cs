#nullable enable
using DCM.Core.Audio;
using DCM.Core.Configuration;
using DCM.Core.Input;
using DCM.Core.Persistence;
using DCM.Core.Screens;
using DCM.Core.Services;
using DCM.Core.UI;
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
    public static readonly GameConfiguration Configuration = GameConfiguration.Default;
    public static int LogicalW => Configuration.Display.LogicalWidth;
    public static int LogicalH => Configuration.Display.LogicalHeight;

    private readonly GraphicsDeviceManager _graphics;
    private readonly GameStateStore _state;
    private SpriteBatch _spriteBatch = null!;
    private RenderTarget2D _screen = null!;
    private IGameScreen _currentScreen = null!;
    private UIPainter _painter = null!;
    private SoundEffect _clickSound = null!;
    private PlaySounds _playSounds = null!;
    private KeyboardState _prevKeyboard;
    private MouseState _prevMouse;
    private GamePadState _prevPad;

    public DCMGame()
    {
        var savePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FindTheExit", "save.json");
        _state = new GameStateStore(
            new JsonGameSaveRepository(savePath, ex => System.Diagnostics.Debug.WriteLine(ex)),
            new GameSettings(), new LevelProgress());
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        _graphics.PreferredBackBufferWidth = Configuration.Display.WindowWidth;
        _graphics.PreferredBackBufferHeight = Configuration.Display.WindowHeight;
        _graphics.ApplyChanges();
        Window.Title = "Find the Exit";
    }

    protected override void LoadContent()
    {
        _state.Load();
        if (_state.Settings.IsFullscreen) SetFullscreen(true);

        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _screen = new RenderTarget2D(GraphicsDevice, LogicalW, LogicalH);
        _painter = new UIPainter(_spriteBatch, Content.Load<SpriteFont>("Fonts/Hud"), GraphicsDevice);
        _clickSound = SoundSynth.CreateClick();
        _playSounds = SoundSynth.CreatePlaySounds();

        var screens = new ScreenFactory(new UIContext(_painter, _clickSound), GraphicsDevice, Content,
            Configuration, _state, _playSounds,
            Content.Load<SpriteFont>("Fonts/MenuTitle"), Content.Load<SpriteFont>("Fonts/MenuTitle2"),
            SetFullscreen);
        _currentScreen = screens.Menu();
    }

    private void SetFullscreen(bool fullscreen)
    {
        _graphics.IsFullScreen = fullscreen;
        _graphics.ApplyChanges();
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        var mouse = ToLogical(Mouse.GetState());
        var pad = GamePad.GetState(PlayerIndex.One, GamePadDeadZone.None);
        var input = new InputSnapshot(keyboard, _prevKeyboard, mouse, _prevMouse, pad, _prevPad);
        _prevKeyboard = keyboard;
        _prevMouse = mouse;
        _prevPad = pad;

        var next = _currentScreen.Update(gameTime, input);

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
        base.Update(gameTime);
    }

    protected override void OnDeactivated(object sender, EventArgs args)
    {
        _currentScreen?.OnFocusLost();
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
        _painter?.Dispose();
        _clickSound?.Dispose();
        _playSounds?.Dispose();
        base.UnloadContent();
    }
}
