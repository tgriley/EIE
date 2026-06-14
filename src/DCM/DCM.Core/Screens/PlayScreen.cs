#nullable enable
using DCM.Core;
using DCM.Core.Entities;
using DCM.Core.Input;
using DCM.Core.Rendering;
using DCM.Core.UI;
using DCM.Core.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace DCM.Core.Screens;

public class PlayScreen : IGameScreen
{
    private readonly RaycasterRenderer _renderer;
    private readonly HUD _hud;
    private readonly Player _player;
    private readonly Map _map;
    private readonly List<Enemy> _enemies;
    private readonly Func<IGameScreen> _toLevelSelect;
    private readonly Func<IGameScreen>? _toNextLevel;
    private readonly bool _hasNextLevel;
    private readonly int _levelIndex;

    private bool _gameOver;
    private bool _won;
    private bool _paused;
    private bool _prevEsc;
    private bool _prevM;
    private bool _firstInputFrame = true;
    private float _elapsed;
    private bool _isNewBest;

    public bool IsMouseVisible => _paused || _gameOver || _won;

    public PlayScreen(SpriteBatch sb, SpriteFont font, GraphicsDevice gd, ContentManager content,
        int levelIndex, Func<IGameScreen> toLevelSelect, Func<IGameScreen>? toNextLevel)
    {
        _levelIndex = levelIndex;
        _map = Map.GetLevel(levelIndex);
        _toLevelSelect = toLevelSelect;
        _toNextLevel = toNextLevel;
        _hasNextLevel = toNextLevel != null;

        // Load enemy spritesheets (one per sheet index, assigned round-robin to spawns)
        const int sheetCount = 5;
        var sheets = new EnemySpriteSheet[sheetCount];
        for (var i = 0; i < sheetCount; i++)
        {
            var tex = content.Load<Texture2D>($"SpritesheetEnemy{i}");
            var pix = new Color[tex.Width * tex.Height];
            tex.GetData(pix);
            sheets[i] = new EnemySpriteSheet(pix, tex.Width, tex.Height, 6);
        }

        _player = new Player(_map.StartX, _map.StartY, _map.StartAngle);
        _enemies = new List<Enemy>();
        var sheetIndex = 0;
        foreach (var spawn in _map.EnemySpawns)
        {
            if (!_map.IsValidSpawn(spawn.x, spawn.y)) continue;
            _enemies.Add(new Enemy(spawn.x, spawn.y, sheets[sheetIndex % sheetCount]));
            sheetIndex++;
        }

        var wallTex = content.Load<Texture2D>("TextureWall0");
        var wallPix = new Color[wallTex.Width * wallTex.Height];
        wallTex.GetData(wallPix);

        var floorTex = content.Load<Texture2D>("TextureFloor0");
        var floorPix = new Color[floorTex.Width * floorTex.Height];
        floorTex.GetData(floorPix);

        var ceilTex = content.Load<Texture2D>("TextureCeiling0");
        var ceilPix = new Color[ceilTex.Width * ceilTex.Height];
        ceilTex.GetData(ceilPix);

        _renderer = new RaycasterRenderer(gd,
            wallPix, wallTex.Width, wallTex.Height,
            floorPix, floorTex.Width, floorTex.Height,
            ceilPix, ceilTex.Width, ceilTex.Height);
        _hud = new HUD(sb, font, gd);

        Mouse.SetPosition(RaycasterRenderer.RW, RaycasterRenderer.RH);
    }

    public IGameScreen? Update(GameTime gameTime, MouseState mouse, MouseState prevMouse)
    {
        var kb = Keyboard.GetState();
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        var escDown = kb.IsKeyDown(Keys.Escape);
        if (escDown && !_prevEsc && !_gameOver && !_won)
            _paused = !_paused;
        _prevEsc = escDown;

        if (_paused)
        {
            var action = _hud.UpdatePause(mouse, prevMouse);
            if (action == HudAction.Resume) _paused = false;
            if (action == HudAction.Quit) return null;
        }
        else if (_gameOver || _won)
        {
            var action = _hud.UpdateEnd(mouse, prevMouse, _hasNextLevel);
            if (action == HudAction.NextLevel) return _toNextLevel!();
            if (action == HudAction.MainMenu) return _toLevelSelect();
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

            var mouseDeltaX = _firstInputFrame ? 0 : mouse.X - RaycasterRenderer.RW;
            _firstInputFrame = false;
            Mouse.SetPosition(RaycasterRenderer.RW, RaycasterRenderer.RH);

            _player.Update(dt, _map, new PlayerInput(
                kb.IsKeyDown(Keys.W) || kb.IsKeyDown(Keys.Up),
                kb.IsKeyDown(Keys.S) || kb.IsKeyDown(Keys.Down),
                kb.IsKeyDown(Keys.A),
                kb.IsKeyDown(Keys.D),
                kb.IsKeyDown(Keys.Left),
                kb.IsKeyDown(Keys.Right),
                kb.IsKeyDown(Keys.LeftShift) || kb.IsKeyDown(Keys.RightShift),
                mouseDeltaX));

            foreach (var e in _enemies)
                e.Update(gameTime, _player, _map);

            var lmbJustPressed = mouse.LeftButton == ButtonState.Pressed &&
                                 prevMouse.LeftButton != ButtonState.Pressed;
            if (lmbJustPressed && _player.TryAttack())
            {
                _renderer.MuzzleFlash = 1f;
                _renderer.RaycastShoot(_player, _enemies)?.Hit(30);
            }

            if (_player.IsDead) _gameOver = true;
            if (_player.ReachedExit && !_won)
            {
                _won = true;
                var prevBest = LevelProgress.GetBestTime(_levelIndex);
                LevelProgress.RecordTime(_levelIndex, _elapsed);
                _isNewBest = _elapsed < prevBest || prevBest >= float.MaxValue;
                if (_hasNextLevel) LevelProgress.Unlock(_levelIndex + 1);
            }
        }

        return this;
    }

    public void Draw(GameTime gameTime)
    {
        _renderer.Render(gameTime, _player, _map, _enemies);
        _hud.Draw(gameTime, _player, _enemies, _map, _gameOver, _won, _paused, _hasNextLevel,
            _elapsed, LevelProgress.GetBestTime(_levelIndex), _isNewBest);
    }

    public void Dispose()
    {
        _renderer.Dispose();
        _hud.Dispose();
    }
}