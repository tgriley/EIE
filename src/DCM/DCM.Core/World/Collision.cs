namespace DCM.Core.World;

public static class Collision
{
    public const double EntityRadius = 0.35;

    private const double MinDistSq = 4 * EntityRadius * EntityRadius;

    public static double DistSq(double ax, double ay, double bx, double by)
    {
        var dx = ax - bx;
        var dy = ay - by;
        return dx * dx + dy * dy;
    }

    public static bool EntitiesOverlap(double ax, double ay, double bx, double by)
        => DistSq(ax, ay, bx, by) < MinDistSq;
}
