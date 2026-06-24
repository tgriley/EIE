using Microsoft.Xna.Framework;

namespace DCM.Core.Entities;

public class HealthPickup : IPickup
{
    public double PosX { get; }
    public double PosY { get; }
    public bool IsCollected { get; private set; }

    public const int HealAmount = 50;
    private const double CollectRadiusSq = 0.6 * 0.6;
    private const int TexSize = 16;
    private static readonly Color[] _pixels = GeneratePixels();

    public HealthPickup(int tileX, int tileY)
    {
        PosX = tileX + 0.5;
        PosY = tileY + 0.5;
    }

    public bool TryCollect(IHealable target)
    {
        if (IsCollected) return false;
        var dx = target.PosX - PosX;
        var dy = target.PosY - PosY;
        if (dx * dx + dy * dy >= CollectRadiusSq) return false;
        IsCollected = true;
        target.Heal(HealAmount);
        return true;
    }

    Color[]     IBillboard.Pixels        => _pixels;
    int         IBillboard.TexWidth      => TexSize;
    int         IBillboard.TexHeight     => TexSize;
    int         IBillboard.TexStride     => TexSize;
    int         IBillboard.PixelOffsetX  => 0;
    bool        IBillboard.IsVisible     => !IsCollected;
    bool        IBillboard.ApplyHurtTint => false;
    int         IBillboard.HeightDivisor => 2;
    double      IBillboard.VerticalShift => 0.5;
    (int, int)? IBillboard.HealthBar     => null;

    private static Color[] GeneratePixels()
    {
        const int sz = TexSize;
        var pixels = new Color[sz * sz];
        for (var y = 0; y < sz; y++)
        for (var x = 0; x < sz; x++)
        {
            var inBox    = x >= 2 && x < 14 && y >= 2 && y < 14;
            var onBorder = inBox && (x == 2 || x == 13 || y == 2 || y == 13);
            var inCross  = (x >= 6 && x < 10 && y >= 2 && y < 14) ||
                           (x >= 2 && x < 14 && y >= 6 && y < 10);

            pixels[y * sz + x] = !inBox  ? Color.Transparent
                : inCross                 ? new Color(220, 40,  40)
                : onBorder                ? new Color(50,  50,  50)
                :                          new Color(200, 200, 195);
        }
        return pixels;
    }
}
