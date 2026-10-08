using DCM.Core.Entities;
using DCM.Core.World;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace DCM.Core.UI;

// Top-left fog-of-war map: only tiles and entities the player has seen.
public sealed class Minimap
{
    private const int TileSize = 8;
    private const int Padding  = 12;
    private const int Border   = 2;

    private static readonly Color ColBg     = new(0, 0, 0, 200);
    private static readonly Color ColWall   = new(160, 150, 140);
    private static readonly Color ColFloor  = new(30, 28, 25);
    private static readonly Color ColExit   = new(60, 200, 220);
    private static readonly Color ColPlayer = new(60, 200, 60);
    private static readonly Color ColEnemy  = new(220, 30, 30);
    private static readonly Color ColPickup = new(240, 240, 235);

    private readonly UIPainter _painter;

    public Minimap(UIPainter painter)
    {
        _painter = painter;
    }

    public void Draw(Player player, IReadOnlyList<Enemy> enemies, IReadOnlyList<IPickup> pickups,
        IMap map, FogOfWar fog)
    {
        _painter.DrawRect(Padding - Border, Padding - Border,
            map.Width * TileSize + Border * 2, map.Height * TileSize + Border * 2, ColBg);

        for (var ty = 0; ty < map.Height; ty++)
        for (var tx = 0; tx < map.Width; tx++)
        {
            if (!fog.IsSeen(tx, ty)) continue;
            _painter.DrawRect(Padding + tx * TileSize, Padding + ty * TileSize,
                TileSize - 1, TileSize - 1, TileColor(map.GetTile(tx, ty)));
        }

        foreach (var p in pickups)
            if (p.IsVisible) DrawMarker(p.PosX, p.PosY, 4, ColPickup, fog);

        foreach (var e in enemies)
            DrawMarker(e.PosX, e.PosY, 4, ColEnemy, fog);

        var px = Padding + (int)(player.PosX * TileSize);
        var py = Padding + (int)(player.PosY * TileSize);
        _painter.DrawRect(px - 2, py - 2, 5, 5, ColPlayer);
        _painter.DrawLine(px, py, px + (int)(player.DirX * 6), py + (int)(player.DirY * 6), ColPlayer);
    }

    private static Color TileColor(int tile) => tile switch
    {
        Tile.Empty => ColFloor,
        Tile.Exit  => ColExit,
        _          => ColWall
    };

    private void DrawMarker(double x, double y, int size, Color color, FogOfWar fog)
    {
        if (!fog.IsSeen((int)x, (int)y)) return;
        _painter.DrawRect(Padding + (int)(x * TileSize) - size / 2,
            Padding + (int)(y * TileSize) - size / 2, size, size, color);
    }
}
