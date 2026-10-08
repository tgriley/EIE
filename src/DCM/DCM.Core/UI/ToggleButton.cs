using DCM.Core.Localization;
using Microsoft.Xna.Framework;
using System;

namespace DCM.Core.UI;

// A settings row: label on the left, live ON/OFF value on the right.
public class ToggleButton : Button
{
    private readonly Func<bool> _isOn;

    public ToggleButton(Rectangle bounds, string label, UIPainter painter, Func<bool> isOn)
        : base(bounds, label, painter)
    {
        _isOn = isOn;
    }

    protected override void DrawContent(Color textColor)
    {
        var textY = Bounds.Y + (Bounds.Height - Painter.Measure("A").Y) / 2f;
        Painter.DrawTextShadow(Label, new Vector2(Bounds.X + 20, textY), textColor);

        var on       = _isOn();
        var valText  = GameText.Get(on ? "On" : "Off");
        var valWidth = Painter.Measure(valText).X;
        Painter.DrawTextShadow(valText, new Vector2(Bounds.Right - valWidth - 20, textY),
            on ? UITheme.ToggleOn : UITheme.ToggleOff);
    }
}
