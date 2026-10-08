using DCM.Core.Configuration;
using Microsoft.Xna.Framework;

namespace DCM.Core.UI;

public static class UITheme
{
    public static int ScreenW => GameConfiguration.Default.Display.LogicalWidth;
    public static int ScreenH => GameConfiguration.Default.Display.LogicalHeight;

    public const int ButtonW = 240;
    public const int ButtonH = 52;

    public static readonly Color Background       = new(10, 8, 8);
    public static readonly Color Title            = new(220, 180, 80);
    public static readonly Color Text             = new(235, 225, 200);
    public static readonly Color TextDim          = new(160, 150, 130);
    public static readonly Color Gold             = new(255, 215, 0);
    public static readonly Color PanelBg          = new(0, 0, 0, 160);

    public static readonly Color ControlBg        = new(40, 35, 30);
    public static readonly Color ControlBgHover   = new(60, 50, 40);
    public static readonly Color Border           = new(100, 80, 60);
    public static readonly Color BorderHover      = new(160, 130, 90);
    public static readonly Color ControlText      = new(200, 190, 170);
    public static readonly Color ControlTextHover = new(255, 240, 200);

    public static readonly Color ToggleOn         = new(80, 210, 80);
    public static readonly Color ToggleOff        = new(200, 80, 60);

    public static int CenteredX(int width) => (ScreenW - width) / 2;
}
