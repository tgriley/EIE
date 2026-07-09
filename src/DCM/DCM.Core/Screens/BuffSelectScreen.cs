#nullable enable
using DCM.Core.Entities;
using DCM.Core.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace DCM.Core.Screens;

// Shown after clicking NEXT LEVEL: pick one of three randomly rolled buffs.
// Displays the player's current stats and each buff's stacked level so
// repeat picks are an informed choice. The chosen buff is applied to the
// shared PlayerBuffs before the next level is created.
public class BuffSelectScreen : IGameScreen
{
    private const int SW = 1280;
    private const int SH = 720;
    private const int ChoiceCount = 3;

    private static readonly Color ColBg        = new(10, 8, 8);
    private static readonly Color ColTitle     = new(220, 180, 80);
    private static readonly Color ColText      = new(235, 225, 200);
    private static readonly Color ColTextDim   = new(160, 150, 130);
    private static readonly Color ColGold      = new(255, 215, 0);
    private static readonly Color ColCardBg    = new(40, 35, 30);
    private static readonly Color ColCardBgHov = new(60, 50, 40);
    private static readonly Color ColBorder    = new(100, 80, 60);
    private static readonly Color ColBorderHov = new(160, 130, 90);
    private static readonly Color ColPanelBg   = new(0, 0, 0, 160);

    private readonly UIPainter _painter;
    private readonly PlayerBuffs _buffs;
    private readonly Func<int, IGameScreen> _onContinue;
    private readonly SoundEffect _clickSound;
    private readonly BuffType[] _choices;
    private readonly Rectangle[] _cards;
    private readonly MenuNavigator _nav = new(ChoiceCount, ChoiceCount);
    private int _health;
    private Point _mousePos;

    public bool IsMouseVisible => true;

    public BuffSelectScreen(SpriteBatch sb, SpriteFont font, GraphicsDevice gd,
        PlayerBuffs buffs, int health, Func<int, IGameScreen> onContinue, SoundEffect clickSound)
    {
        _painter    = new UIPainter(sb, font, gd);
        _buffs      = buffs;
        _health     = health;
        _onContinue = onContinue;
        _clickSound = clickSound;
        _choices    = PlayerBuffs.RollChoices(ChoiceCount, Random.Shared);

        const int cardW = 360, cardH = 170, gap = 40;
        var x0 = (SW - (cardW * ChoiceCount + gap * (ChoiceCount - 1))) / 2;
        _cards = new Rectangle[ChoiceCount];
        for (var i = 0; i < ChoiceCount; i++)
            _cards[i] = new Rectangle(x0 + i * (cardW + gap), 160, cardW, cardH);
    }

    public IGameScreen? Update(GameTime gameTime, MouseState mouse, MouseState prevMouse)
    {
        _nav.Update(gameTime);
        _mousePos = mouse.Position;

        if (_nav.JustConfirmed) return Pick(_choices[_nav.SelectedIndex]);

        var released = mouse.LeftButton == ButtonState.Released &&
                       prevMouse.LeftButton == ButtonState.Pressed;
        if (released)
            for (var i = 0; i < ChoiceCount; i++)
                if (_cards[i].Contains(mouse.Position))
                    return Pick(_choices[i]);

        return this;
    }

    private IGameScreen Pick(BuffType type)
    {
        _clickSound.Play();
        var oldMaxHealth = _buffs.MaxHealth;
        _buffs.Apply(type);
        if (type == BuffType.MaxHealth)
            _health = (int)Math.Round(_health * (double)_buffs.MaxHealth / oldMaxHealth);
        return _onContinue(_health);
    }

    public void Draw(GameTime gameTime)
    {
        _painter.Begin();
        _painter.DrawRect(0, 0, SW, SH, ColBg);

        DrawCentered("LEVEL COMPLETE", 44, ColTitle, 1.6f);
        DrawCentered("CHOOSE A BUFF", 104, ColText, 0.9f);

        for (var i = 0; i < ChoiceCount; i++) DrawCard(i);

        DrawStatsPanel();

        _painter.End();
    }

    private void DrawCard(int i)
    {
        var r = _cards[i];
        var hover = r.Contains(_mousePos) || _nav.IsSelected(i);

        _painter.DrawRect(r.X - 2, r.Y - 2, r.Width + 4, r.Height + 4, hover ? ColBorderHov : ColBorder);
        _painter.DrawRect(r.X, r.Y, r.Width, r.Height, hover ? ColCardBgHov : ColCardBg);

        var type  = _choices[i];
        var level = _buffs.GetLevel(type);
        DrawCenteredIn(r, PlayerBuffs.Name(type), r.Y + 26, hover ? Color.White : ColText, 1f);
        DrawCenteredIn(r, PlayerBuffs.Description(type), r.Y + 76, ColTextDim, 0.75f);
        DrawCenteredIn(r, $"LV {level} -> LV {level + 1}", r.Y + 118, ColGold, 0.85f);
    }

    private void DrawStatsPanel()
    {
        const int panelX = 240, panelY = 380, panelW = 800, panelH = 296;
        _painter.DrawRect(panelX, panelY, panelW, panelH, ColPanelBg);

        DrawCentered("YOUR STATS", panelY + 18, ColTitle, 1f);
        DrawCentered($"HEALTH  {_health} / {_buffs.MaxHealth}", panelY + 56, ColText, 0.85f);

        const int rowH = 36, colW = 400, rowsPerCol = 4, startY = panelY + 104;
        for (var i = 0; i < PlayerBuffs.All.Length; i++)
        {
            var x = panelX + 36 + i / rowsPerCol * colW;
            var y = startY + i % rowsPerCol * rowH;
            var t = PlayerBuffs.All[i];
            _painter.DrawTextShadow(PlayerBuffs.Name(t), new Vector2(x, y), ColTextDim, 0.75f);
            _painter.DrawTextShadow(_buffs.StatValue(t), new Vector2(x + 220, y), ColText, 0.75f);
            _painter.DrawTextShadow($"LV {_buffs.GetLevel(t)}", new Vector2(x + 310, y), ColGold, 0.75f);
        }
    }

    private void DrawCentered(string text, int y, Color color, float scale)
    {
        var size = _painter.Measure(text);
        _painter.DrawTextShadow(text, new Vector2((SW - size.X * scale) / 2f, y), color, scale);
    }

    private void DrawCenteredIn(Rectangle r, string text, int y, Color color, float scale)
    {
        var size = _painter.Measure(text);
        _painter.DrawTextShadow(text, new Vector2(r.X + (r.Width - size.X * scale) / 2f, y), color, scale);
    }

    public void Dispose()
    {
        _painter.Dispose();
    }
}
