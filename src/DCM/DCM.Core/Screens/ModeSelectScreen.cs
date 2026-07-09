#nullable enable
using DCM.Core.UI;
using DCM.Core.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace DCM.Core.Screens;

// Shown after START on the main menu: pick Story or Endless, with each mode's
// furthest progress displayed under its button.
public class ModeSelectScreen : IGameScreen
{
    private const int SW = 1280;
    private const int SH = 720;

    private static readonly Color ColBg       = new(10, 8, 8);
    private static readonly Color ColTitle    = new(220, 180, 80);
    private static readonly Color ColProgress = new(180, 160, 80);

    private readonly UIPainter _painter;
    private readonly Button _storyButton;
    private readonly Button _endlessButton;
    private readonly Button _backButton;
    private readonly Func<IGameScreen> _createStory;
    private readonly Func<IGameScreen> _createEndless;
    private readonly Func<IGameScreen> _createMenu;
    private readonly SoundEffect _clickSound;
    private readonly MenuNavigator _nav = new(3);

    public bool IsMouseVisible => true;

    public ModeSelectScreen(SpriteBatch sb, SpriteFont font, GraphicsDevice gd,
        Func<IGameScreen> createStory, Func<IGameScreen> createEndless, Func<IGameScreen> createMenu,
        SoundEffect clickSound)
    {
        _painter       = new UIPainter(sb, font, gd);
        _createStory   = createStory;
        _createEndless = createEndless;
        _createMenu    = createMenu;
        _clickSound    = clickSound;

        int btnW = 300, btnH = 64, btnX = (SW - btnW) / 2;
        _storyButton   = new Button(new Rectangle(btnX, SH / 2 - 80, btnW, btnH), "STORY",   _painter);
        _endlessButton = new Button(new Rectangle(btnX, SH / 2 + 40, btnW, btnH), "ENDLESS", _painter);
        _backButton    = new Button(new Rectangle((SW - 160) / 2, SH - 100, 160, 48), "BACK", _painter);
    }

    public IGameScreen? Update(GameTime gameTime, MouseState mouse, MouseState prevMouse)
    {
        _nav.Update(gameTime);

        if (_nav.JustCancelled) { _clickSound.Play(); return _createMenu(); }
        if (_nav.JustConfirmed)
        {
            _clickSound.Play();
            return _nav.SelectedIndex switch
            {
                0 => _createStory(),
                1 => _createEndless(),
                _ => _createMenu()
            };
        }

        if (_storyButton.IsClicked(mouse, prevMouse))   { _clickSound.Play(); return _createStory(); }
        if (_endlessButton.IsClicked(mouse, prevMouse)) { _clickSound.Play(); return _createEndless(); }
        if (_backButton.IsClicked(mouse, prevMouse))    { _clickSound.Play(); return _createMenu(); }

        return this;
    }

    public void Draw(GameTime gameTime)
    {
        var mousePos = Mouse.GetState().Position;
        _painter.Begin();

        _painter.DrawRect(0, 0, SW, SH, ColBg);

        const string title = "SELECT MODE";
        var titleScale = 1.8f;
        var titleSize  = _painter.Measure(title);
        _painter.DrawTextShadow(title,
            new Vector2((SW - titleSize.X * titleScale) / 2f, 100), ColTitle, titleScale);

        _storyButton.Draw(mousePos, _nav.IsSelected(0));
        DrawProgress(StoryProgress(), SH / 2 - 80 + 64 + 8);

        _endlessButton.Draw(mousePos, _nav.IsSelected(1));
        DrawProgress(EndlessProgress(), SH / 2 + 40 + 64 + 8);

        _backButton.Draw(mousePos, _nav.IsSelected(2));

        _painter.End();
    }

    private static string StoryProgress()
        => $"Reached: Level {LevelProgress.MaxUnlockedLevel() + 1} / {Map.LevelCount}";

    private static string EndlessProgress()
        => LevelProgress.BestEndlessStage > 0
            ? $"Best: Stage {LevelProgress.BestEndlessStage}"
            : "Best: --";

    private void DrawProgress(string text, int y)
    {
        const float scale = 0.8f;
        var size = _painter.Measure(text);
        _painter.DrawTextShadow(text,
            new Vector2((SW - size.X * scale) / 2f, y), ColProgress, scale);
    }

    public void Dispose()
    {
        _painter.Dispose();
    }
}
