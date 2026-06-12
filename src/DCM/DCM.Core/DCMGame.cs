using DCM.Core.Entities;
using DCM.Core.Rendering;
using DCM.Core.UI;
using DCM.Core.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace DCM.Core
{
    public class DCMGame : Game
    {
        private enum GameState { Menu, Playing }

        private GraphicsDeviceManager _graphics;
        private SpriteBatch           _spriteBatch;
        private SpriteFont            _font;

        // Menu state
        private GameState _state = GameState.Menu;
        private MainMenu  _mainMenu;

        // Gameplay state (null until Start is pressed)
        private RaycasterRenderer _renderer;
        private HUD               _hud;
        private Player            _player;
        private Map               _map;
        private List<Enemy>       _enemies;

        private bool _gameOver = false;
        private bool _won      = false;
        private bool _paused   = false;

        private bool _showFullMap = false;
        private bool _prevM       = false;
        private bool _prevEsc     = false;

        private MouseState _prevMouse;

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

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _font        = Content.Load<SpriteFont>("Fonts/Hud");
            _mainMenu    = new MainMenu(_spriteBatch, _font, GraphicsDevice);
        }

        private void LoadLevel()
        {
            _map     = Map.Level1;
            _player  = new Player(_map.StartX, _map.StartY, _map.StartAngle);
            _enemies = new List<Enemy>();
            foreach (var spawn in _map.EnemySpawns)
                _enemies.Add(new Enemy(spawn.x, spawn.y));

            var wallTex  = Content.Load<Texture2D>("TextureWall0");
            var wallPix  = new Color[wallTex.Width * wallTex.Height];
            wallTex.GetData(wallPix);

            var floorTex = Content.Load<Texture2D>("TextureFloor0");
            var floorPix = new Color[floorTex.Width * floorTex.Height];
            floorTex.GetData(floorPix);

            var ceilTex  = Content.Load<Texture2D>("TextureCeiling0");
            var ceilPix  = new Color[ceilTex.Width * ceilTex.Height];
            ceilTex.GetData(ceilPix);

            var enemySheet = Content.Load<Texture2D>("SpritesheetEnemy0");
            var enemyPix   = new Color[enemySheet.Width * enemySheet.Height];
            enemySheet.GetData(enemyPix);

            _renderer = new RaycasterRenderer(GraphicsDevice,
                wallPix,  wallTex.Width,  wallTex.Height,
                floorPix, floorTex.Width, floorTex.Height,
                ceilPix,  ceilTex.Width,  ceilTex.Height,
                enemyPix, enemySheet.Width, enemySheet.Height, 6);
            _hud = new HUD(_spriteBatch, _font, GraphicsDevice);

            _gameOver = false;
            _won      = false;
            _paused   = false;

            ResetMouse();
        }

        protected override void Update(GameTime gameTime)
        {
            var kb    = Keyboard.GetState();
            var mouse = Mouse.GetState();

            if (_state == GameState.Menu)
            {
                MenuAction action = _mainMenu.Update(mouse, _prevMouse);
                if (action == MenuAction.Start)
                {
                    LoadLevel();
                    IsMouseVisible = false;
                    _state = GameState.Playing;
                }
                else if (action == MenuAction.Exit)
                {
                    Exit();
                }
            }
            else // Playing
            {
                if (kb.IsKeyDown(Keys.Q)) Exit();

                bool escDown = kb.IsKeyDown(Keys.Escape);
                if (escDown && !_prevEsc && !_gameOver && !_won)
                    _paused = !_paused;
                _prevEsc = escDown;

                IsMouseVisible = _paused || _gameOver || _won;

                if (!_paused && !_gameOver && !_won)
                {
                    bool mDown = kb.IsKeyDown(Keys.M);
                    if (mDown && !_prevM) _showFullMap = !_showFullMap;
                    _prevM = mDown;

                    Point center = new Point(
                        _graphics.PreferredBackBufferWidth  / 2,
                        _graphics.PreferredBackBufferHeight / 2);
                    _player.Update(gameTime, _map, center);

                    foreach (var e in _enemies)
                        e.Update(gameTime, _player, _map);

                    bool lmbDown = mouse.LeftButton == ButtonState.Pressed;
                    bool lmbPrev = _prevMouse.LeftButton == ButtonState.Pressed;
                    if (lmbDown && !lmbPrev && _player.TryAttack())
                    {
                        _renderer.MuzzleFlash = 1f;
                        var hit = _renderer.RaycastShoot(_player, _enemies);
                        hit?.Hit(30);
                    }

                    if (_player.IsDead)       _gameOver = true;
                    if (_player.ReachedExit)  _won      = true;
                }
            }

            _prevMouse = mouse;
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            if (_state == GameState.Menu)
            {
                _mainMenu.Draw(gameTime);
            }
            else
            {
                _renderer.Render(gameTime, _player, _map, _enemies);
                _hud.Draw(gameTime, _player, _enemies, _map, _gameOver, _won, _paused);
            }

            base.Draw(gameTime);
        }

        private void ResetMouse()
        {
            Mouse.SetPosition(
                _graphics.PreferredBackBufferWidth  / 2,
                _graphics.PreferredBackBufferHeight / 2);
        }

        protected override void UnloadContent()
        {
            _renderer?.Dispose();
            _hud?.Dispose();
            _mainMenu?.Dispose();
            base.UnloadContent();
        }
    }
}