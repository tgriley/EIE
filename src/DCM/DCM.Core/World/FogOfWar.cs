using System;

namespace DCM.Core.World;

public class FogOfWar
{
    private readonly bool[,] _seen;
    private readonly int _width;
    private readonly int _height;
    private readonly double _revealDist;

    private const int RayCount = 360;

    public FogOfWar(int width, int height, double revealDist = 10.0)
    {
        _width      = width;
        _height     = height;
        _revealDist = revealDist;
        _seen       = new bool[width, height];
    }

    public bool IsSeen(int tx, int ty)
        => (uint)tx < (uint)_width && (uint)ty < (uint)_height && _seen[tx, ty];

    public void Update(double posX, double posY, IMap map)
    {
        double step = 2 * Math.PI / RayCount;
        for (int i = 0; i < RayCount; i++)
            CastRay(posX, posY, Math.Cos(i * step), Math.Sin(i * step), map);
    }

    private void CastRay(double ox, double oy, double dx, double dy, IMap map)
    {
        int mx = (int)ox, my = (int)oy;

        double ddx = Math.Abs(dx) < 1e-10 ? double.MaxValue : Math.Abs(1.0 / dx);
        double ddy = Math.Abs(dy) < 1e-10 ? double.MaxValue : Math.Abs(1.0 / dy);
        int stepX = dx < 0 ? -1 : 1;
        int stepY = dy < 0 ? -1 : 1;
        double sdx = dx < 0 ? (ox - mx) * ddx : (mx + 1.0 - ox) * ddx;
        double sdy = dy < 0 ? (oy - my) * ddy : (my + 1.0 - oy) * ddy;

        while (true)
        {
            if ((uint)mx >= (uint)_width || (uint)my >= (uint)_height) break;
            _seen[mx, my] = true;
            if (map.IsWall(mx, my)) break;

            if (sdx < sdy)
            {
                if (sdx >= _revealDist) break;
                sdx += ddx;
                mx  += stepX;
            }
            else
            {
                if (sdy >= _revealDist) break;
                sdy += ddy;
                my  += stepY;
            }
        }
    }
}
