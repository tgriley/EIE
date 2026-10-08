#nullable enable
using System;
using System.Collections.Generic;

namespace DCM.Core.World;

// Tile type constants. 0 = empty, 1-3 = wall variants, 9 = exit door.
public static class Tile
{
    public const int Empty = 0;
    public const int Wall1 = 1;
    public const int Wall2 = 2;
    public const int Wall3 = 3;
    public const int Exit = 9;
}

public class Map : IMap
{
    private const int PickupMinDistFromStart = 4;

    public int Width { get; }
    public int Height { get; }
    private readonly int[,] _tiles;

    // Player start position (tile coords)
    public float StartX { get; }
    public float StartY { get; }
    public float StartAngle { get; }

    // Enemy spawn positions with enemy type index
    public (int x, int y, int type)[] EnemySpawns { get; }

    // Texture set suffix used to load wall/floor/ceiling assets
    public int TextureVariant { get; }

    // Health pickup spawn (one per level)
    public (int x, int y) PickupSpawn { get; }

    // rng drives exit and pickup placement; pass a seeded Random so the same
    // generator seed always yields the same map.
    public Map(int[,] tiles, float startX, float startY, float startAngle,
        (int x, int y, int type)[] enemySpawns, int textureVariant, Random rng)
    {
        _tiles = tiles;
        Height = tiles.GetLength(0);
        Width = tiles.GetLength(1);
        StartX = startX;
        StartY = startY;
        StartAngle = startAngle;
        EnemySpawns = enemySpawns;
        TextureVariant = textureVariant;
        RelocateExit(rng);
        PickupSpawn = ChoosePickupSpawn(rng);
    }

    // Move the exit to a random tile on the far walls (the bottom row and right
    // column, opposite the top-left start) whose inner neighbour is open, so it
    // is reachable but no longer always parked in the same corner.
    private void RelocateExit(Random rng)
    {
        var candidates = new List<(int x, int y)>();
        for (var x = 1; x < Width - 1; x++)
            if (_tiles[Height - 2, x] == Tile.Empty) candidates.Add((x, Height - 1));
        for (var y = 1; y < Height - 1; y++)
            if (_tiles[y, Width - 2] == Tile.Empty) candidates.Add((Width - 1, y));

        if (candidates.Count == 0) return;

        for (var y = 0; y < Height; y++)
        for (var x = 0; x < Width; x++)
            if (_tiles[y, x] == Tile.Exit) _tiles[y, x] = Tile.Wall1;

        var (ex, ey) = candidates[rng.Next(candidates.Count)];
        _tiles[ey, ex] = Tile.Exit;
    }

    private (int x, int y) ChoosePickupSpawn(Random rng)
    {
        var startTileX = (int)StartX;
        var startTileY = (int)StartY;

        var candidates = new List<(int, int)>();
        for (var ty = 0; ty < Height; ty++)
        for (var tx = 0; tx < Width; tx++)
        {
            if (_tiles[ty, tx] != Tile.Empty) continue;
            if (Math.Abs(tx - startTileX) < PickupMinDistFromStart &&
                Math.Abs(ty - startTileY) < PickupMinDistFromStart) continue;
            if (Array.Exists(EnemySpawns, s => s.x == tx && s.y == ty)) continue;
            if (!IsValidSpawn(tx, ty)) continue;
            candidates.Add((tx, ty));
        }

        return candidates.Count > 0
            ? candidates[rng.Next(candidates.Count)]
            : (startTileX, startTileY);
    }

    public int GetTile(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return Tile.Wall1;
        return _tiles[y, x];
    }

    public bool IsWall(int x, int y)
    {
        var t = GetTile(x, y);
        return t != Tile.Empty && t != Tile.Exit;
    }

    public bool IsExit(int x, int y)
    {
        return GetTile(x, y) == Tile.Exit;
    }

    public bool IsValidSpawn(int x, int y)
    {
        if (IsWall(x, y) || IsExit(x, y)) return false;
        return !IsWall(x, y - 1) || !IsWall(x, y + 1) ||
               !IsWall(x - 1, y) || !IsWall(x + 1, y);
    }
}
