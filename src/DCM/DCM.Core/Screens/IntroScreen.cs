#nullable enable
using DCM.Core.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace DCM.Core.Screens;

// Shown once before the first level: teaches that the camera scares most
// creatures, but some are immune to it — and shows what those look like.
public class IntroScreen : IGameScreen
{
    private const int SW = 1280;
    private const int SH = 720;
    private const int FrameCount = 6;

    private static readonly Color ColBg      = new(10, 8, 8);
    private static readonly Color ColTitle    = new(220, 60, 50);
    private static readonly Color ColBody     = new(210, 200, 185);
    private static readonly Color ColWarn      = new(230, 180, 70);
    private static readonly Color ColPanel     = new(24, 18, 16);
    private static readonly Color ColPanelEdge = new(120, 40, 35);

    private static readonly string[] Body =
    {
        "Raise your camera (hold RIGHT MOUSE) and most creatures recoil in fear.",
        "Snap a photo (LEFT MOUSE) while it is raised and the flash leaves them dazed.",
        "",
        "But the camera does not work on everything. Some creatures are BLIND to it —",
        "they feel no fear, cannot be dazed, and will hunt you no matter what you do.",
        "",
        "Learn to recognise these two. When you see them, run or fight:"
    };

    private readonly UIPainter _painter;
    private readonly SpriteFont _titleFont;
    private readonly SoundEffect _clickSound;
    private readonly Func<IGameScreen> _onContinue;
    private readonly Func<IGameScreen> _onBack;

    private readonly Texture2D _immuneA;
    private readonly Texture2D _immuneB;

    private readonly Button _startButton;
    private readonly Button _backButton;

    private KeyboardState _prevKb;
    private GamePadState _prevGamePad;

    public bool IsMouseVisible => true;

    public IntroScreen(SpriteBatch sb, SpriteFont font, SpriteFont titleFont, GraphicsDevice gd,
        ContentManager content, Func<IGameScreen> onContinue, Func<IGameScreen> onBack,
        SoundEffect clickSound)
    {
        _painter    = new UIPainter(sb, font, gd);
        _titleFont  = titleFont;
        _onContinue = onContinue;
        _onBack     = onBack;
        _clickSound = clickSound;

        _immuneA = content.Load<Texture2D>("SpritesheetEnemy5");
        _immuneB = content.Load<Texture2D>("SpritesheetEnemy6");

        _startButton = new Button(new Rectangle(SW / 2 - 130, SH - 74, 260, 52), "ENTER LEVEL 1", _painter);
        _backButton  = new Button(new Rectangle(40, SH - 74, 140, 52), "BACK", _painter);

        _prevKb = Keyboard.GetState();
    }

    public IGameScreen? Update(GameTime gameTime, MouseState mouse, MouseState prevMouse)
    {
        var kb = Keyboard.GetState();
        var gp = GamePad.GetState(PlayerIndex.One);

        var confirm =
            KeyJustPressed(kb, Keys.Enter) || KeyJustPressed(kb, Keys.Space) ||
            gp.Buttons.A     == ButtonState.Pressed && _prevGamePad.Buttons.A     != ButtonState.Pressed ||
            gp.Buttons.Start == ButtonState.Pressed && _prevGamePad.Buttons.Start != ButtonState.Pressed ||
            _startButton.IsClicked(mouse, prevMouse);

        var cancel =
            KeyJustPressed(kb, Keys.Escape) ||
            gp.Buttons.B == ButtonState.Pressed && _prevGamePad.Buttons.B != ButtonState.Pressed ||
            _backButton.IsClicked(mouse, prevMouse);

        _prevKb        = kb;
        _prevGamePad   = gp;

        if (confirm) { _clickSound.Play(); return _onContinue(); }
        if (cancel)  { _clickSound.Play(); return _onBack(); }
        return this;
    }

    private bool KeyJustPressed(KeyboardState kb, Keys key)
        => kb.IsKeyDown(key) && _prevKb.IsKeyUp(key);

    public void Draw(GameTime gameTime)
    {
        var mousePos = Mouse.GetState().Position;
        _painter.Begin();

        _painter.DrawRect(0, 0, SW, SH, ColBg);

        const string title = "KNOW YOUR ENEMY";
        var titleSize = _painter.Measure(_titleFont, title);
        _painter.DrawTextShadow(_titleFont, title,
            new Vector2((SW - titleSize.X) / 2f, 50), ColTitle);

        var y = 190f;
        foreach (var line in Body)
        {
            if (line.Length > 0)
            {
                var col  = line.Contains("BLIND") ? ColWarn : ColBody;
                var size = _painter.Measure(line);
                _painter.DrawTextShadow(line, new Vector2((SW - size.X) / 2f, y), col);
            }
            y += 32f;
        }

        DrawImmune(_immuneA, SW / 2 - 220);
        DrawImmune(_immuneB, SW / 2 + 40);

        const string caption = "CAMERA-IMMUNE";
        var capSize = _painter.Measure(caption);
        _painter.DrawTextShadow(caption,
            new Vector2((SW - capSize.X) / 2f, 600), ColWarn);

        _startButton.Draw(mousePos);
        _backButton.Draw(mousePos);

        _painter.End();
    }

    private void DrawImmune(Texture2D sheet, int x)
    {
        var frameW = sheet.Width / FrameCount;
        const int destH = 180;
        var destW = destH * frameW / sheet.Height;
        var dest  = new Rectangle(x + (180 - destW) / 2, 400, destW, destH);

        _painter.DrawRect(x - 6, 394, 192, destH + 12, ColPanelEdge);
        _painter.DrawRect(x - 4, 396, 188, destH + 8, ColPanel);
        _painter.DrawTexture(sheet, dest, new Rectangle(0, 0, frameW, sheet.Height), Color.White);
    }

    public void Dispose()
    {
        _painter.Dispose();
    }
}
