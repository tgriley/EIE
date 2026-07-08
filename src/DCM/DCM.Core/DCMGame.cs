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
        _clickSound = CreateClickSound();
        _playSounds = new PlaySounds(
            CreateGunshotSound(),
            CreatePlayerOuchSound(),
            CreatePlayerDeathSound(),
            CreateEnemyOuchSound(),
            CreateEnemyDeathSound(),
            CreateWinSound(),
            CreateCameraShutterSound());

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

    // ── Sound synthesis helpers ──────────────────────────────────────────────

    private static short ClipSample(double v) =>
        (short)(Math.Max(-1.0, Math.Min(1.0, v)) * short.MaxValue);

    private static void WriteSample(byte[] data, int i, short s)
    {
        data[i * 2]     = (byte)(s & 0xFF);
        data[i * 2 + 1] = (byte)((s >> 8) & 0xFF);
    }

    private static SoundEffect CreateClickSound()
    {
        const int sr = 44100;
        var n    = sr * 80 / 1000;
        var data = new byte[n * 2];
        for (var i = 0; i < n; i++)
        {
            var t    = (double)i / sr;
            var wave = Math.Sin(2 * Math.PI * 800 * t) * 0.6
                     + Math.Sin(2 * Math.PI * 1600 * t) * 0.4;
            WriteSample(data, i, ClipSample(Math.Exp(-t * 45) * wave * 0.8));
        }
        return new SoundEffect(data, sr, AudioChannels.Mono);
    }

    private static SoundEffect CreateGunshotSound()
    {
        const int sr = 44100;
        var n    = sr * 130 / 1000;
        var data = new byte[n * 2];
        var rng  = new Random(42);
        for (var i = 0; i < n; i++)
        {
            var t     = (double)i / sr;
            var noise = (rng.NextDouble() * 2 - 1) * Math.Exp(-t * 80);
            var boom  = Math.Sin(2 * Math.PI * 75 * t) * Math.Exp(-t * 15);
            WriteSample(data, i, ClipSample((noise * 0.65 + boom * 0.45) * 0.9));
        }
        return new SoundEffect(data, sr, AudioChannels.Mono);
    }

    private static SoundEffect CreatePlayerOuchSound()
    {
        const int sr = 44100;
        var n    = sr * 220 / 1000;
        var data = new byte[n * 2];
        var rng  = new Random(1);
        for (var i = 0; i < n; i++)
        {
            var t     = (double)i / sr;
            var decay = Math.Exp(-t * 14);
            var freq  = Math.Max(60, 200 - t * 500);
            var wave  = Math.Sin(2 * Math.PI * freq * t);
            var noise = (rng.NextDouble() * 2 - 1) * 0.25;
            WriteSample(data, i, ClipSample((wave * 0.75 + noise) * decay * 0.85));
        }
        return new SoundEffect(data, sr, AudioChannels.Mono);
    }

    private static SoundEffect CreatePlayerDeathSound()
    {
        const int sr = 44100;
        var n     = sr * 1100 / 1000;
        var data  = new byte[n * 2];
        var phase = 0.0;
        for (var i = 0; i < n; i++)
        {
            var t    = (double)i / sr;
            var freq = Math.Max(40, 320 * Math.Exp(-t * 2.0));
            phase   += 2 * Math.PI * freq / sr;
            var vibrato = Math.Sin(2 * Math.PI * 5 * t) * 0.08;
            WriteSample(data, i, ClipSample(Math.Sin(phase * (1 + vibrato)) * Math.Exp(-t * 1.5) * 0.85));
        }
        return new SoundEffect(data, sr, AudioChannels.Mono);
    }

    private static SoundEffect CreateEnemyOuchSound()
    {
        const int sr = 44100;
        var n    = sr * 160 / 1000;
        var data = new byte[n * 2];
        var rng  = new Random(3);
        for (var i = 0; i < n; i++)
        {
            var t     = (double)i / sr;
            var decay = Math.Exp(-t * 22);
            var freq  = Math.Max(80, 380 - t * 1800);
            var wave  = Math.Sin(2 * Math.PI * freq * t);
            var noise = (rng.NextDouble() * 2 - 1) * 0.3;
            WriteSample(data, i, ClipSample((wave * 0.7 + noise) * decay * 0.8));
        }
        return new SoundEffect(data, sr, AudioChannels.Mono);
    }

    private static SoundEffect CreateEnemyDeathSound()
    {
        const int sr = 44100;
        var n     = sr * 700 / 1000;
        var data  = new byte[n * 2];
        var rng   = new Random(4);
        var phase = 0.0;
        for (var i = 0; i < n; i++)
        {
            var t     = (double)i / sr;
            var freq  = Math.Max(50, 700 * Math.Exp(-t * 4.0));
            phase    += 2 * Math.PI * freq / sr;
            var vibrato = Math.Sin(2 * Math.PI * 10 * t) * 0.12;
            var noise   = (rng.NextDouble() * 2 - 1) * 0.1;
            WriteSample(data, i, ClipSample((Math.Sin(phase * (1 + vibrato)) * 0.85 + noise) * Math.Exp(-t * 2.5) * 0.8));
        }
        return new SoundEffect(data, sr, AudioChannels.Mono);
    }

    private static SoundEffect CreateCameraShutterSound()
    {
        const int sr = 44100;
        var n    = sr * 120 / 1000;
        var data = new byte[n * 2];
        var rng  = new Random(7);
        for (var i = 0; i < n; i++)
        {
            var t = (double)i / sr;
            var click1 = Math.Exp(-t * 350) * (Math.Sin(2 * Math.PI * 3200 * t) * 0.6 + (rng.NextDouble() * 2 - 1) * 0.4);
            var t2     = Math.Max(0, t - 0.04);
            var click2 = Math.Exp(-t2 * 350) * (Math.Sin(2 * Math.PI * 2600 * t2) * 0.5 + (rng.NextDouble() * 2 - 1) * 0.35) * 0.75;
            WriteSample(data, i, ClipSample((click1 + click2) * 0.65));
        }
        return new SoundEffect(data, sr, AudioChannels.Mono);
    }

    private static SoundEffect CreateWinSound()
    {
        const int sr = 44100;
        double[] freqs   = { 262, 330, 392, 523 };
        double[] lengths = { 0.12, 0.12, 0.12, 0.50 };
        var totalSamples = 0;
        foreach (var l in lengths) totalSamples += (int)(l * sr);
        var data  = new byte[totalSamples * 2];
        var phase = 0.0;
        var idx   = 0;
        for (var ni = 0; ni < freqs.Length; ni++)
        {
            var noteSamples = (int)(lengths[ni] * sr);
            var freq        = freqs[ni];
            var dur         = lengths[ni];
            for (var j = 0; j < noteSamples && idx < totalSamples; j++, idx++)
            {
                var t   = (double)j / sr;
                var env = ni < freqs.Length - 1
                    ? Math.Min(1.0, t * 120)                        // attack + sustain
                    : Math.Min(1.0, t * 120) * Math.Exp(-t * 3.5); // last note fades
                phase += 2 * Math.PI * freq / sr;
                var wave = Math.Sin(phase) + Math.Sin(phase * 2) * 0.25;
                WriteSample(data, idx, ClipSample(wave * env * 0.55));
            }
        }
        return new SoundEffect(data, sr, AudioChannels.Mono);
    }

    // ── MonoGame overrides ───────────────────────────────────────────────────

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
