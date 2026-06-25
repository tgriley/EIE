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
    private readonly RaycasterRenderer _renderer;
    private readonly HUD _hud;
    private readonly Player _player;
    private readonly Map _map;
    private readonly List<Enemy> _enemies;
    private readonly List<IPickup> _pickups;
    private readonly Func<IGameScreen> _toLevelSelect;
    private readonly Func<int, IGameScreen>? _toNextLevel;
    private readonly PlaySounds _sounds;
    private readonly bool _hasNextLevel;
    private readonly int _levelIndex;

    private bool _gameOver;
    private bool _won;
    private bool _paused;
    private bool _prevEsc;
    private bool _prevM;
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
        SoundEffect clickSound, PlaySounds sounds, int startHealth = 100)
    {
        _levelIndex    = levelIndex;
        _map           = Map.GetLevel(levelIndex);
        _toLevelSelect = toLevelSelect;
        _toNextLevel   = toNextLevel;
        _hasNextLevel  = toNextLevel != null;
        _sounds        = sounds;

        const int sheetCount     = 7;
        const int hideSheetCount = 5;
        var sheets     = new EnemySpriteSheet[sheetCount];
        var hideSheets = new EnemySpriteSheet[hideSheetCount];
        for (var i = 0; i < sheetCount; i++)
        {
            var tex = content.Load<Texture2D>($"SpritesheetEnemy{i}");
            var pix = new Color[tex.Width * tex.Height];
            tex.GetData(pix);
            sheets[i] = new EnemySpriteSheet(pix, tex.Width, tex.Height, 6);
        }
        for (var i = 0; i < hideSheetCount; i++)
        {
            var hideTex = content.Load<Texture2D>($"SpritesheetEnemy{i}_hide");
            var hidePix = new Color[hideTex.Width * hideTex.Height];
            hideTex.GetData(hidePix);
            hideSheets[i] = new EnemySpriteSheet(hidePix, hideTex.Width, hideTex.Height, 6);
        }

        _player = new Player(_map.StartX, _map.StartY, _map.StartAngle, startHealth);
        _player.OnDamaged = () => _sounds.PlayerOuch.Play();
        _player.OnDied    = () => _sounds.PlayerDeath.Play();

        _pickups = new List<IPickup> { };

        _enemies = new List<Enemy>();
        foreach (var spawn in _map.EnemySpawns)
        {
            if (!_map.IsValidSpawn(spawn.x, spawn.y)) continue;
            var si = spawn.type % sheetCount;
            var enemy = new Enemy(spawn.x, spawn.y, sheets[si], hideSheets[si % hideSheetCount], cameraImmune: si >= 5);
            enemy.OnHurt = () => _sounds.EnemyOuch.Play();
            enemy.OnDied = () => _sounds.EnemyDeath.Play();
            _enemies.Add(enemy);
        }

        var texVariant = _map.TextureVariant;

        var wallTex = content.Load<Texture2D>($"TextureWall{texVariant}");
        var wallPix = new Color[wallTex.Width * wallTex.Height];
        wallTex.GetData(wallPix);

        var doorTex = content.Load<Texture2D>("TextureDoor0");
        var doorPix = new Color[doorTex.Width * doorTex.Height];
        doorTex.GetData(doorPix);

        var floorTex = content.Load<Texture2D>($"TextureFloor{texVariant}");
        var floorPix = new Color[floorTex.Width * floorTex.Height];
        floorTex.GetData(floorPix);

        var ceilTex = content.Load<Texture2D>($"TextureCeiling{texVariant}");
        var ceilPix = new Color[ceilTex.Width * ceilTex.Height];
        ceilTex.GetData(ceilPix);

        var weaponTex = content.Load<Texture2D>("Camera");

        _renderer = new RaycasterRenderer(gd,
            wallPix, wallTex.Width, wallTex.Height,
            doorPix, doorTex.Width, doorTex.Height,
            floorPix, floorTex.Width, floorTex.Height,
            ceilPix, ceilTex.Width, ceilTex.Height,
            weaponTex);
        _hud = new HUD(sb, font, gd, clickSound);

        Mouse.SetPosition(RaycasterRenderer.RW, RaycasterRenderer.RH);
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
            var action = _hud.UpdateEnd(gameTime, mouse, prevMouse, _hasNextLevel);
            if (action == HudAction.NextLevel) return _toNextLevel!(_player.Health);
            if (action == HudAction.MainMenu)  return _toLevelSelect();
        }
        else
        {
            var mDown = kb.IsKeyDown(Keys.M);
            if (mDown && !_prevM)
            {
                /* _showFullMap toggle — wired when minimap supports it */
            }
            _prevM = mDown;

            _elapsed += dt;

            var leftX  = Math.Abs(gp.ThumbSticks.Left.X)  > StickDeadZone ? gp.ThumbSticks.Left.X  : 0f;
            var leftY  = Math.Abs(gp.ThumbSticks.Left.Y)  > StickDeadZone ? gp.ThumbSticks.Left.Y  : 0f;
            var rightX = Math.Abs(gp.ThumbSticks.Right.X) > StickDeadZone ? gp.ThumbSticks.Right.X : 0f;

            var mouseDeltaX = _firstInputFrame ? 0 : mouse.X - RaycasterRenderer.RW;
            _firstInputFrame = false;
            Mouse.SetPosition(RaycasterRenderer.RW, RaycasterRenderer.RH);
            mouseDeltaX += (int)(rightX * ControllerLookSens);

            var moving = kb.IsKeyDown(Keys.W) || kb.IsKeyDown(Keys.S) ||
                         kb.IsKeyDown(Keys.A) || kb.IsKeyDown(Keys.D) ||
                         kb.IsKeyDown(Keys.Up) || kb.IsKeyDown(Keys.Down) ||
                         leftX != 0 || leftY != 0;

            _player.Update(dt, _map, new PlayerInput(
                kb.IsKeyDown(Keys.W) || kb.IsKeyDown(Keys.Up)   || leftY >  0,
                kb.IsKeyDown(Keys.S) || kb.IsKeyDown(Keys.Down)  || leftY <  0,
                kb.IsKeyDown(Keys.A)                             || leftX < 0,
                kb.IsKeyDown(Keys.D)                             || leftX >  0,
                kb.IsKeyDown(Keys.Left),
                kb.IsKeyDown(Keys.Right),
                kb.IsKeyDown(Keys.LeftShift) || kb.IsKeyDown(Keys.RightShift) || gp.Triggers.Left > TriggerThreshold,
                mouseDeltaX,
                _cameraRaised));

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
            foreach (var e in _enemies) e.Update(gameTime, _player, _player, _map, cameraReady, flashJustFired);

            if (_player.IsDead) _gameOver = true;

            if (_player.ReachedExit && !_won)
            {
                _won = true;
                _sounds.Win.Play();
                var prevBest = LevelProgress.GetBestTime(_levelIndex);
                LevelProgress.RecordTime(_levelIndex, _elapsed);
                _isNewBest = _elapsed < prevBest || prevBest >= float.MaxValue;
                if (_hasNextLevel) LevelProgress.Unlock(_levelIndex + 1);
            }
        }

        _prevGamePad = gp;
        return this;
    }

    public void Draw(GameTime gameTime)
    {
        _renderer.Render(gameTime, _player, _map, _enemies.Concat<IBillboard>(_pickups));
        _hud.Draw(gameTime, _player, _enemies, _map, _gameOver, _won, _paused, _hasNextLevel,
            _elapsed, LevelProgress.GetBestTime(_levelIndex), _isNewBest,
            _cameraCooldown, CameraUseDuration, _player.SprintStamina);
    }

    public void Dispose()
    {
        _renderer.Dispose();
        _hud.Dispose();
    }
}
