#nullable enable
using DCM.Core;
using DCM.Core.Audio;
using DCM.Core.Entities;
using DCM.Core.Input;
using DCM.Core.Rendering;
using DCM.Core.UI;
using DCM.Core.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DCM.Core.Screens;

public class PlayScreen : IGameScreen
{
    private readonly GraphicsDevice _gd;
    private readonly RaycasterRenderer _renderer;
    private readonly HUD _hud;
    private readonly Player _player;
    private readonly Map _map;
    private readonly List<Enemy> _enemies;
    private readonly List<IPickup> _pickups;
    private readonly Func<IGameScreen> _toLevelSelect;
    private readonly Func<int, IGameScreen>? _toNextLevel;
    private readonly PlaySounds _sounds;
    private readonly FogOfWar _fog;
    private readonly bool _hasNextLevel;
    private readonly bool _endless;
    private readonly int _levelIndex;

    private bool _gameOver;
    private bool _won;
    private bool _paused;
    private bool _prevEsc;
    private bool _firstInputFrame = true;
    private bool _cameraRaised;
    private bool _cameraFired;
    private float _cameraCooldown;
    private float _cameraRaiseTimer;
    private float _elapsed;
    private bool _isNewBest;
    private GamePadState _prevGamePad;

    private const float StickDeadZone    = 0.20f;
    private const float TriggerThreshold = 0.50f;
    private const int   ControllerLookSens = 50;
    private const float CameraUseDuration = 5f;
    private const float CameraRaiseTime   = 0.4f;

    public bool IsMouseVisible => _paused || _gameOver || _won;

    public PlayScreen(SpriteBatch sb, SpriteFont font, GraphicsDevice gd, ContentManager content,
        int levelIndex, Func<IGameScreen> toLevelSelect, Func<int, IGameScreen>? toNextLevel,
        SoundEffect clickSound, PlaySounds sounds, int startHealth = 100,
        Map? map = null, bool endless = false)
    {
        _gd            = gd;
        _levelIndex    = levelIndex;
        _endless       = endless;
        _map           = map ?? Map.GetLevel(levelIndex);
        _toLevelSelect = toLevelSelect;
        _toNextLevel   = toNextLevel;
        _hasNextLevel  = toNextLevel != null;
        _sounds        = sounds;

        var sheets     = new EnemySpriteSheet[EnemyCatalog.SpriteCount];
        var hideSheets = new EnemySpriteSheet[EnemyCatalog.HideSpriteCount];
        for (var i = 0; i < sheets.Length; i++)
        {
            var (pix, w, h) = LoadPixels(content, $"SpritesheetEnemy{i}");
            sheets[i] = new EnemySpriteSheet(pix, w, h, 6);
        }
        for (var i = 0; i < hideSheets.Length; i++)
        {
            var (pix, w, h) = LoadPixels(content, $"SpritesheetEnemy{i}_hide");
            hideSheets[i] = new EnemySpriteSheet(pix, w, h, 6);
        }

        _player = new Player(_map.StartX, _map.StartY, _map.StartAngle, startHealth);
        _player.OnDamaged = () => _sounds.PlayerOuch.Play();
        _player.OnDied    = () => _sounds.PlayerDeath.Play();

        _pickups = new List<IPickup> { };

        _enemies = new List<Enemy>();
        foreach (var spawn in _map.EnemySpawns)
        {
            if (!_map.IsValidSpawn(spawn.x, spawn.y)) continue;
            var si = spawn.type % EnemyCatalog.SpriteCount;
            var enemy = new Enemy(spawn.x, spawn.y, sheets[si], hideSheets[si % hideSheets.Length],
                cameraImmune: EnemyCatalog.IsCameraImmune(spawn.type));
            enemy.OnHurt = () => _sounds.EnemyOuch.Play();
            enemy.OnDied = () => _sounds.EnemyDeath.Play();
            _enemies.Add(enemy);
        }

        var texVariant = _map.TextureVariant;

        var (wallPix,  wallW,  wallH)  = LoadPixels(content, $"TextureWall{texVariant}");
        var (doorPix,  doorW,  doorH)  = LoadPixels(content, "TextureDoor0");
        var (floorPix, floorW, floorH) = LoadPixels(content, $"TextureFloor{texVariant}");
        var (ceilPix,  ceilW,  ceilH)  = LoadPixels(content, $"TextureCeiling{texVariant}");
        var weaponTex = content.Load<Texture2D>("Camera");

        _renderer = new RaycasterRenderer(gd,
            wallPix,  wallW,  wallH,
            doorPix,  doorW,  doorH,
            floorPix, floorW, floorH,
            ceilPix,  ceilW,  ceilH,
            weaponTex);
        _hud = new HUD(sb, font, gd, clickSound);
        _fog = new FogOfWar(_map.Width, _map.Height);

        RecenterMouse();
    }

    // Park the OS cursor at the window centre. Screens receive mouse coords in
    // logical space, so the window centre reads back as the logical centre
    // (RW, RH) and the per-frame look delta stays zero when the mouse is still.
    private void RecenterMouse()
    {
        var pp = _gd.PresentationParameters;
        Mouse.SetPosition(pp.BackBufferWidth / 2, pp.BackBufferHeight / 2);
    }

    public IGameScreen? Update(GameTime gameTime, MouseState mouse, MouseState prevMouse)
    {
        var kb = Keyboard.GetState();
        var gp = GamePad.GetState(PlayerIndex.One, GamePadDeadZone.None);
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        var escDown          = kb.IsKeyDown(Keys.Escape);
        var startJustPressed = gp.Buttons.Start == ButtonState.Pressed &&
                               _prevGamePad.Buttons.Start != ButtonState.Pressed;
        if ((escDown && !_prevEsc || startJustPressed) && !_gameOver && !_won)
            _paused = !_paused;
        _prevEsc = escDown;

        if (_paused)
        {
            var action = _hud.UpdatePause(gameTime, mouse, prevMouse);
            if (action == HudAction.Resume)   _paused = false;
            if (action == HudAction.MainMenu) return _toLevelSelect();
            if (action == HudAction.Quit)     return null;
        }
        else if (_gameOver || _won)
        {
            if (_gameOver) _player.UpdateDeathSpin(dt);
            var action = _hud.UpdateEnd(gameTime, mouse, prevMouse, _hasNextLevel);
            if (action == HudAction.NextLevel) return _toNextLevel!(_player.Health);
            if (action == HudAction.MainMenu)  return _toLevelSelect();
        }
        else
        {
            _elapsed += dt;

            var leftX  = Math.Abs(gp.ThumbSticks.Left.X)  > StickDeadZone ? gp.ThumbSticks.Left.X  : 0f;
            var leftY  = Math.Abs(gp.ThumbSticks.Left.Y)  > StickDeadZone ? gp.ThumbSticks.Left.Y  : 0f;
            var rightX = Math.Abs(gp.ThumbSticks.Right.X) > StickDeadZone ? gp.ThumbSticks.Right.X : 0f;

            var lookScale = _gd.PresentationParameters.BackBufferWidth / (float)DCMGame.LogicalW;
            var mouseDeltaX = _firstInputFrame ? 0 : (int)((mouse.X - RaycasterRenderer.RW) * lookScale);
            _firstInputFrame = false;
            RecenterMouse();
            mouseDeltaX += (int)(rightX * ControllerLookSens);

            var moving = kb.IsKeyDown(Keys.W) || kb.IsKeyDown(Keys.S) ||
                         kb.IsKeyDown(Keys.A) || kb.IsKeyDown(Keys.D) ||
                         kb.IsKeyDown(Keys.Up) || kb.IsKeyDown(Keys.Down) ||
                         leftX != 0 || leftY != 0;

            _fog.Update(_player.PosX, _player.PosY, _map);
            _player.Update(dt, _map, new PlayerInput(
                kb.IsKeyDown(Keys.W) || kb.IsKeyDown(Keys.Up)   || leftY >  0,
                kb.IsKeyDown(Keys.S) || kb.IsKeyDown(Keys.Down)  || leftY <  0,
                kb.IsKeyDown(Keys.A)                             || leftX < 0,
                kb.IsKeyDown(Keys.D)                             || leftX >  0,
                kb.IsKeyDown(Keys.Left),
                kb.IsKeyDown(Keys.Right),
                kb.IsKeyDown(Keys.LeftShift) || kb.IsKeyDown(Keys.RightShift) || gp.Triggers.Left > TriggerThreshold,
                mouseDeltaX,
                _cameraRaised), _enemies);

            if (moving)
                _renderer.WeaponBobPhase += dt * 7f;

            var raiseHeld   = mouse.RightButton == ButtonState.Pressed ||
                              gp.Triggers.Right > TriggerThreshold;
            var shootJustPressed = mouse.LeftButton    == ButtonState.Pressed &&
                                   prevMouse.LeftButton != ButtonState.Pressed ||
                                   gp.Buttons.RightShoulder    == ButtonState.Pressed &&
                                   _prevGamePad.Buttons.RightShoulder != ButtonState.Pressed;

            if (_cameraCooldown > 0) _cameraCooldown -= dt;
            if (!raiseHeld) { _cameraFired = false; _cameraRaiseTimer = 0f; }
            _cameraRaised = raiseHeld && _cameraCooldown <= 0 && !_cameraFired;
            _renderer.WeaponRaiseTarget = _cameraRaised;

            if (_cameraRaised && _cameraRaiseTimer < CameraRaiseTime)
                _cameraRaiseTimer += dt;

            var cameraReady    = _cameraRaised && _cameraRaiseTimer >= CameraRaiseTime;
            var flashJustFired = false;
            if (cameraReady && shootJustPressed)
            {
                _renderer.MuzzleFlash = 1f;
                _cameraCooldown = CameraUseDuration;
                _cameraFired = true;
                _cameraRaiseTimer = 0f;
                flashJustFired = true;
                _sounds.CameraShutter.Play();
            }

            foreach (var p in _pickups) p.TryCollect(_player);
            foreach (var e in _enemies) e.Update(gameTime, _player, _player, _map, _enemies, cameraReady, flashJustFired);

            if (_player.IsDead) _gameOver = true;

            if (_player.ReachedExit && !_won)
            {
                _won = true;
                _sounds.Win.Play();
                if (!_endless)
                {
                    var prevBest = LevelProgress.GetBestTime(_levelIndex);
                    LevelProgress.RecordTime(_levelIndex, _elapsed);
                    _isNewBest = _elapsed < prevBest || prevBest >= float.MaxValue;
                    if (_hasNextLevel) LevelProgress.Unlock(_levelIndex + 1);
                }
            }
        }

        _prevGamePad = gp;
        return this;
    }

    public void Draw(GameTime gameTime)
    {
        _renderer.Render(gameTime, _player, _map, _enemies.Concat<IBillboard>(_pickups));
        _hud.Draw(gameTime, _player, _enemies, _map, _gameOver, _won, _paused, _hasNextLevel,
            _elapsed, _endless ? float.MaxValue : LevelProgress.GetBestTime(_levelIndex), _isNewBest,
            _cameraCooldown, CameraUseDuration, _player.SprintStamina, _fog);
    }

    private static (Color[] pixels, int width, int height) LoadPixels(ContentManager content, string name)
    {
        var tex = content.Load<Texture2D>(name);
        var pixels = new Color[tex.Width * tex.Height];
        tex.GetData(pixels);
        return (pixels, tex.Width, tex.Height);
    }

    public void Dispose()
    {
        _renderer.Dispose();
        _hud.Dispose();
    }
}
