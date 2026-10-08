using DCM.Core.Entities;
using DCM.Core.Localization;
using Microsoft.Xna.Framework;
using System;

namespace DCM.Core.UI;

// In-play overlay: status bars, objective, timer, crosshair, the fading
// controls hint and the red damage flash.
public sealed class GameplayHud
{
    private const float ControlsHintDuration = 12f;
    private const float ControlsHintFade = 3f;

    private static readonly Color ColCrosshair = new(255, 255, 255, 180);

    private static readonly string[] ControlKeys =
    {
        "ControlsMove",
        "ControlsRun",
        "ControlsLook",
        "ControlsCamera",
        "ControlsPhoto",
        "ControlsPause"
    };

    private readonly UIPainter _painter;
    private readonly PlayerBuffs _buffs;
    private readonly StatusBarPanel _healthBar;
    private readonly StatusBarPanel _sprintBar;
    private readonly StatusBarPanel _cameraBar;
    private float _controlsTimer = ControlsHintDuration;

    public GameplayHud(UIPainter painter, PlayerBuffs buffs)
    {
        _painter = painter;
        _buffs   = buffs;

        // Bar widths scale with the buffed stat so upgrades are visible on the
        // HUD: bigger health/stamina pools widen, a faster flash cooldown narrows.
        _healthBar = Bar(2, "Health", new(220, 50, 40), new(180, 20, 20), BuffType.MaxHealth, _buffs.MaxHealth);
        _sprintBar = Bar(1, "Sprint", new(220, 200, 50), new(100, 90, 25), BuffType.MaxStamina, _buffs.MaxStamina);
        _cameraBar = Bar(0, "Flash", new(80, 200, 220), new(50, 110, 150), BuffType.CameraCooldown, _buffs.CameraCooldown);
    }

    private static StatusBarPanel Bar(int slotFromBottom, string labelKey, Color high, Color low,
        BuffType stat, double current)
    {
        const int baseBarW = 160, x = 12, panelH = 50, gap = 6;
        var y = UITheme.ScreenH - 60 - slotFromBottom * (panelH + gap);
        var width = (int)(baseBarW * current / PlayerBuffs.BaseValue(stat));
        return new StatusBarPanel(x, y, GameText.Get(labelKey), high, low, width);
    }

    public void Draw(GameTime gameTime, Player player, CameraFlash flash, float elapsed)
    {
        _controlsTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

        _healthBar.Draw(_painter, player.Health / (float)player.MaxHealth, player.Health.ToString(), 0.5f);
        _sprintBar.Draw(_painter, player.SprintStamina / player.SprintMax,
            player.SprintStamina >= player.SprintMax ? GameText.Get("Ready") : $"{player.SprintStamina:F1}s");
        _cameraBar.Draw(_painter, flash.ChargeFraction,
            flash.ChargeFraction >= 1f ? GameText.Get("Ready") : $"{flash.Cooldown:F1}s");

        _painter.DrawCentered(GameText.Get("Objective"), 14, UITheme.Text);
        DrawTimer(elapsed);
        DrawCrosshair();
        if (_controlsTimer > 0) DrawControls();
        DrawHurtFlash(player);
    }

    private void DrawTimer(float elapsed)
    {
        var text = TimeFormat.Format(elapsed);
        var size = _painter.Measure(text);
        int px = UITheme.ScreenW - (int)size.X - 22, py = 8;
        _painter.DrawRect(px - 8, py - 4, (int)size.X + 16, 28, UITheme.PanelBg);
        _painter.DrawTextShadow(text, new Vector2(px, py), UITheme.Text);
    }

    private void DrawCrosshair()
    {
        int cx = UITheme.ScreenW / 2, cy = UITheme.ScreenH / 2, len = 8, gap = 3;
        _painter.DrawRect(cx - len - gap, cy - 1, len, 2, ColCrosshair);
        _painter.DrawRect(cx + gap + 1,   cy - 1, len, 2, ColCrosshair);
        _painter.DrawRect(cx - 1, cy - len - gap, 2, len, ColCrosshair);
        _painter.DrawRect(cx - 1, cy + gap + 1,   2, len, ColCrosshair);
    }

    private void DrawControls()
    {
        var color = UITheme.TextDim * Math.Min(1f, _controlsTimer / ControlsHintFade);
        var y = UITheme.ScreenH / 2 - 60;
        foreach (var key in ControlKeys)
        {
            _painter.DrawTextShadow(GameText.Get(key), new Vector2(14, y), color, 0.75f);
            y += 18;
        }
    }

    private void DrawHurtFlash(Player player)
    {
        if (player.HurtTimer <= 0) return;
        var alpha = (int)(player.HurtTimer * 180 / Player.HurtDuration);
        _painter.FillScreen(new Color(180, 0, 0, alpha));
    }
}
