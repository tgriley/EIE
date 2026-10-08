#nullable enable
using DCM.Core.Entities;
using DCM.Core.Input;
using DCM.Core.Localization;
using DCM.Core.UI;
using Microsoft.Xna.Framework;
using System;

namespace DCM.Core.Screens;

// Shown after clicking NEXT LEVEL: pick one of three randomly rolled buffs.
// Displays the player's current stats and each buff's stacked level so
// repeat picks are an informed choice. The chosen buff is applied to the
// shared PlayerBuffs before the next level is created.
public class BuffSelectScreen : IGameScreen
{
    private const int ChoiceCount = 3;

    private readonly UIContext _ui;
    private readonly UIPainter _painter;
    private readonly PlayerBuffs _buffs;
    private readonly Func<int, IGameScreen> _onContinue;
    private readonly BuffType[] _choices;
    private readonly Rectangle[] _cards;
    private readonly Button _skipButton;
    private readonly MenuNavigator _nav = new(ChoiceCount + 1, ChoiceCount);
    private int _health;
    private Point _mousePos;

    public bool IsMouseVisible => true;

    public BuffSelectScreen(UIContext ui, PlayerBuffs buffs, int health, Func<int, IGameScreen> onContinue)
    {
        _ui         = ui;
        _painter    = ui.Painter;
        _buffs      = buffs;
        _health     = health;
        _onContinue = onContinue;
        _choices    = PlayerBuffs.RollChoices(ChoiceCount, Random.Shared);

        const int cardW = 360, cardH = 170, gap = 40;
        var x0 = UITheme.CenteredX(cardW * ChoiceCount + gap * (ChoiceCount - 1));
        _cards = new Rectangle[ChoiceCount];
        for (var i = 0; i < ChoiceCount; i++)
            _cards[i] = new Rectangle(x0 + i * (cardW + gap), 160, cardW, cardH);

        _skipButton = new Button(new Rectangle(UITheme.CenteredX(180), 337, 180, 38), GameText.Get("Skip"), _painter);
    }

    public IGameScreen? Update(GameTime gameTime, InputSnapshot input)
    {
        _nav.Update(gameTime, input);
        _mousePos = input.MousePosition;

        if (_nav.JustConfirmed)
            return _nav.SelectedIndex == ChoiceCount ? Skip() : Pick(_choices[_nav.SelectedIndex]);

        if (_skipButton.IsClicked(input)) return Skip();

        for (var i = 0; i < ChoiceCount; i++)
            if (input.LeftReleasedIn(_cards[i]))
                return Pick(_choices[i]);

        return this;
    }

    private IGameScreen Skip()
    {
        _ui.Click.Play();
        return _onContinue(_health);
    }

    private IGameScreen Pick(BuffType type)
    {
        _ui.Click.Play();
        var oldMaxHealth = _buffs.MaxHealth;
        _buffs.Apply(type);
        if (type == BuffType.MaxHealth)
            _health = (int)Math.Round(_health * (double)_buffs.MaxHealth / oldMaxHealth);
        return _onContinue(_health);
    }

    public void Draw(GameTime gameTime)
    {
        _painter.Begin();
        _painter.FillScreen(UITheme.Background);

        _painter.DrawCentered(GameText.Get("LevelComplete"), 44, UITheme.Title, 1.6f);
        _painter.DrawCentered(GameText.Get("ChooseBuff"), 104, UITheme.Text, 0.9f);

        for (var i = 0; i < ChoiceCount; i++) DrawCard(i);

        _skipButton.Draw(_mousePos, _nav.IsSelected(ChoiceCount));

        DrawStatsPanel();

        _painter.End();
    }

    private void DrawCard(int i)
    {
        var r = _cards[i];
        var hover = r.Contains(_mousePos) || _nav.IsSelected(i);
        _painter.DrawFramedPanel(r, hover);

        var def   = PlayerBuffs.Definition(_choices[i]);
        var level = _buffs.GetLevel(def.Type);
        _painter.DrawCenteredIn(r, def.Name, r.Y + 26, hover ? Color.White : UITheme.Text);
        _painter.DrawCenteredIn(r, def.Description, r.Y + 76, UITheme.TextDim, 0.75f);
        _painter.DrawCenteredIn(r, $"{Lv(level)} -> {Lv(level + 1)}", r.Y + 118, UITheme.Gold, 0.85f);
    }

    private void DrawStatsPanel()
    {
        const int panelX = 240, panelY = 380, panelW = 800, panelH = 296;
        _painter.DrawRect(panelX, panelY, panelW, panelH, UITheme.PanelBg);

        _painter.DrawCentered(GameText.Get("YourStats"), panelY + 18, UITheme.Title);
        _painter.DrawCentered($"{GameText.Get("Health")}  {_health} / {_buffs.MaxHealth}", panelY + 56, UITheme.Text, 0.85f);

        const int rowH = 36, colW = 400, rowsPerCol = 4, startY = panelY + 104;
        for (var i = 0; i < PlayerBuffs.All.Length; i++)
        {
            var x = panelX + 36 + i / rowsPerCol * colW;
            var y = startY + i % rowsPerCol * rowH;
            var t = PlayerBuffs.All[i];
            _painter.DrawTextShadow(PlayerBuffs.Definition(t).Name, new Vector2(x, y), UITheme.TextDim, 0.75f);
            _painter.DrawTextShadow(_buffs.StatValue(t), new Vector2(x + 220, y), UITheme.Text, 0.75f);
            _painter.DrawTextShadow(Lv(_buffs.GetLevel(t)), new Vector2(x + 310, y), UITheme.Gold, 0.75f);
        }
    }

    private static string Lv(int level) => $"{GameText.Get("Level")} {level}";

    public void Dispose()
    {
    }
}
