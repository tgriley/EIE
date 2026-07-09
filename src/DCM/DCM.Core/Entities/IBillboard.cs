using Microsoft.Xna.Framework;

namespace DCM.Core.Entities;

public interface IBillboard
{
    double PosX    { get; }
    double PosY    { get; }
    bool   IsVisible { get; }

    Color[] Pixels       { get; }
    int     TexWidth     { get; }   // sampling width per frame
    int     TexHeight    { get; }
    int     TexStride    { get; }   // pixel-row width in the Pixels array (>= TexWidth for animated sheets)
    int     PixelOffsetX { get; }  // absolute X offset into Pixels for the current frame

    bool   ApplyHurtTint { get; }
    int    HeightDivisor  { get; }  // 1 = full sprite height; 2 = half (floor items)
    double VerticalShift  { get; }  // fraction of screenH to shift down from horizon-centre

    (int current, int max)? HealthBar { get; }  // null = no bar

    float? OverheadCountdown { get; }  // seconds shown above the sprite; null = none
}
