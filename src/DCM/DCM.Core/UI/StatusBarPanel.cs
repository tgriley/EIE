using Microsoft.Xna.Framework;

namespace DCM.Core.UI;

internal readonly struct StatusBarPanel
{
    private readonly int _x;
    private readonly int _y;
    private readonly int _barW;
    private readonly string _label;
    private readonly Color _highColor;
    private readonly Color _lowColor;

    private const int BarH = 18;

    private static readonly Color ColPanelBg = new(0, 0, 0, 160);
    private static readonly Color ColBarBg   = new(30, 25, 22);
    private static readonly Color ColText    = new(235, 225, 200);

    public StatusBarPanel(int x, int y, string label, Color highColor, Color lowColor, int barW = 160)
    {
        _x         = x;
        _y         = y;
        _barW      = barW;
        _label     = label;
        _highColor = highColor;
        _lowColor  = lowColor;
    }

    public void Draw(UIPainter painter, float fraction, string valueText, float highThreshold = 1f)
    {
        painter.DrawRect(_x - 4, _y - 26, _barW + 8, BarH + 32, ColPanelBg);
        painter.DrawTextShadow(_label, new Vector2(_x, _y - 20), ColText, 0.85f);
        painter.DrawRect(_x, _y, _barW, BarH, ColBarBg);

        var fillW = (int)(_barW * fraction);
        if (fillW > 0)
            painter.DrawRect(_x, _y, fillW, BarH, fraction >= highThreshold ? _highColor : _lowColor);

        painter.DrawTextShadow(valueText, new Vector2(_x + _barW + 8, _y), ColText, 0.85f);
    }
}
