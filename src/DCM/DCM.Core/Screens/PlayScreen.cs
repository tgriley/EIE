#nullable enable
using DCM.Core.Audio;
using DCM.Core.Configuration;
using DCM.Core.Entities;
using DCM.Core.Input;
using DCM.Core.Rendering;
using DCM.Core.Services;
using DCM.Core.UI;
using DCM.Core.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DCM.Core.Screens;

public class PlayScreen : IGameScreen
{
    private readonly GraphicsDevice _gd;
    private readonly UIPainter _painter;
    private readonly GameplayConfiguration _config;
    private readonly PlaySounds _sounds;
    private readonly LevelCompletionService _completion;
    private readonly Func<IGameScreen> _toMainMenu;
    private readonly Func<int, IGameScreen> _toNextLevel;
    private readonly int _stage;

    private readonly Map _map;
    private readonly Player _player;
    private readonly List<Enemy> _enemies;
    private readonly List<IPickup> _pickups;
    private readonly CameraFlash _flash;
    private readonly FogOfWar _fog;

    private readonly RaycasterRenderer _renderer;
    private readonly Minimap _minimap;
    private readonly GameplayHud _hud;
    private readonly PauseOverlay _pauseOverlay;
    private readonly EndOverlay _endOverlay;

    private bool _gameOver;
    private bool _won;
    private bool _paused;
    private bool _firstInputFrame = true;
    private float _elapsed;
    private float _bobPhase;
    private Point _mousePos;

    public bool IsMouseVisible => _paused || IsEnded;

    private bool IsEnded => _gameOver || _won;

    public PlayScreen(UIContext ui, GraphicsDevice gd, StageSetup setup, PlaySounds sounds,
        GameplayConfiguration config, LevelCompletionService completion,
        Func<IGameScreen> toMainMenu, Func<int, IGameScreen> toNextLevel)
    {
        _gd          = gd;
        _painter     = ui.Painter;
        _config      = config;
        _sounds      = sounds;
        _completion  = completion;
        _toMainMenu  = toMainMenu;
        _toNextLevel = toNextLevel;
        _stage       = setup.Stage;
        _map         = setup.Map;

        var buffs = setup.Buffs;
        _player = new Player(_map.StartX, _map.StartY, _map.StartAngle, setup.StartHealth, buffs);
        _player.OnDamaged = () => _sounds.PlayerOuch.Play();
        _player.OnDied    = () => _sounds.PlayerDeath.Play();

        _enemies = LevelFactory.CreateEnemies(_map, setup.Assets, buffs.StunDuration);
        _pickups = LevelFactory.CreatePickups(_map);
        _flash   = new CameraFlash(buffs.CameraCooldown, config.CameraRaiseTime);
        _fog     = new FogOfWar(_map.Width, _map.Height, buffs.MinimapReveal);

        var a = setup.Assets;
        _renderer     = new RaycasterRenderer(gd, a.Wall, a.Door, a.Floor, a.Ceiling, a.Weapon, buffs.SightRange);
        _minimap      = new Minimap(_painter);
        _hud          = new GameplayHud(_painter, buffs);
        _pauseOverlay = new PauseOverlay(ui);
        _endOverlay   = new EndOverlay(ui);

        RecenterMouse();
    }

    // Auto-pause on window focus loss: active gameplay warps the OS cursor to
    // the window centre every frame, which would fight the user's cursor
    // outside the game. The end overlays already leave the mouse free.
    public void OnFocusLost()
    {
        if (!IsEnded) _paused = true;
    }

    // Park the OS cursor at the window centre. Screens receive mouse coords in
    // logical space, so the window centre reads back as the logical centre and
    // the per-frame look delta stays zero when the mouse is still.
    private void RecenterMouse()
    {
        var pp = _gd.PresentationParameters;
        Mouse.SetPosition(pp.BackBufferWidth / 2, pp.BackBufferHeight / 2);
    }

    private int ReadMouseLookDelta(InputSnapshot input)
    {
        var lookScale = _gd.PresentationParameters.BackBufferWidth / (float)UITheme.ScreenW;
        var delta = _firstInputFrame ? 0 : (int)((input.Mouse.X - UITheme.ScreenW / 2) * lookScale);
        _firstInputFrame = false;
        RecenterMouse();
        return delta;
    }

    public IGameScreen? Update(GameTime gameTime, InputSnapshot input)
    {
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _mousePos = input.MousePosition;

        if (PlayerInputMapper.PausePressed(input) && !IsEnded)
            _paused = !_paused;

        if (_paused)
        {
            switch (_pauseOverlay.Update(gameTime, input))
            {
                case PauseAction.Resume:   _paused = false; break;
                case PauseAction.MainMenu: return _toMainMenu();
                case PauseAction.Quit:     return null;
            }
        }
        else if (IsEnded)
        {
            if (_gameOver) _player.UpdateDeathSpin(dt);
            _flash.Update(dt, false, false);
            switch (_endOverlay.Update(gameTime, input, _won))
            {
                case EndAction.NextLevel: return _toNextLevel(_player.Health);
                case EndAction.MainMenu:  return _toMainMenu();
            }
        }
        else
        {
            UpdateGameplay(gameTime, dt, input);
        }

        return this;
    }

    private void UpdateGameplay(GameTime gameTime, float dt, InputSnapshot input)
    {
        _elapsed += dt;

        _fog.Update(_player.PosX, _player.PosY, _map);
        _player.Update(dt, _map,
            PlayerInputMapper.Movement(input, _config, ReadMouseLookDelta(input), _flash.IsRaised),
            _enemies);
        _bobPhase += dt * _config.WeaponBobSpeed * _player.MovementRatio;

        _flash.Update(dt,
            PlayerInputMapper.CameraRaiseHeld(input, _config),
            PlayerInputMapper.CameraShootPressed(input));
        if (_flash.JustFired) _sounds.CameraShutter.Play();

        foreach (var p in _pickups)
            if (p.TryCollect(_player)) _sounds.Pickup.Play();
        foreach (var e in _enemies)
            e.Update(gameTime, _player, _map, _enemies, _flash.IsReady, _flash.JustFired);

        if (_player.IsDead) _gameOver = true;

        if (_player.ReachedExit && !_won)
        {
            _won = true;
            _sounds.Win.Play();
            _completion.Complete(_stage);
        }
    }

    public void Draw(GameTime gameTime)
    {
        _renderer.Render(_player, _map, _enemies.Concat<IBillboard>(_pickups), _flash.Pose(_bobPhase));

        _painter.Begin();
        if (_paused)
        {
            _minimap.Draw(_player, _enemies, _pickups, _map, _fog);
            _pauseOverlay.Draw(_mousePos);
        }
        else if (IsEnded)
        {
            _endOverlay.Draw(_mousePos, _won, _elapsed);
        }
        else
        {
            _minimap.Draw(_player, _enemies, _pickups, _map, _fog);
            _hud.Draw(gameTime, _player, _flash, _elapsed);
        }
        _painter.End();
    }

    public void Dispose()
    {
        _renderer.Dispose();
    }
}
