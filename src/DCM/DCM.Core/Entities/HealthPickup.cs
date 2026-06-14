namespace DCM.Core.Entities;

public class HealthPickup
{
    public double PosX { get; }
    public double PosY { get; }
    public bool IsCollected { get; private set; }

    public const int HealAmount = 50;
    private const double CollectRadiusSq = 0.6 * 0.6;

    public HealthPickup(int tileX, int tileY)
    {
        PosX = tileX + 0.5;
        PosY = tileY + 0.5;
    }

    public bool TryCollect(double playerX, double playerY)
    {
        if (IsCollected) return false;
        var dx = playerX - PosX;
        var dy = playerY - PosY;
        if (dx * dx + dy * dy >= CollectRadiusSq) return false;
        IsCollected = true;
        return true;
    }
}
