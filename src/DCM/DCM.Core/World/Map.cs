using System;

namespace DCM.Core.World;

/// <summary>
/// Tile type constants. 0 = empty, 1-7 = wall variants, 9 = exit door.
/// </summary>
public static class Tile
{
    public const int Empty = 0;
    public const int Wall1 = 1; // Primary stone
    public const int Wall2 = 2; // Darker stone
    public const int Wall3 = 3; // Brick variant
    public const int Exit = 9;
}

public class Map : IMap
{
    public int Width { get; }
    public int Height { get; }
    private readonly int[,] _tiles;

    // Player start position (tile coords)
    public float StartX { get; }
    public float StartY { get; }
    public float StartAngle { get; }

    // Enemy spawn positions
    public readonly (int x, int y)[] EnemySpawns;

    // Torch positions (tile coords where wall has a torch)
    public readonly (int x, int y)[] TorchPositions;

    // 16x16 level. Row 0 = top (y=0), Col 0 = left (x=0).
    private static readonly int[,] _level1Tiles = new int[,]
    {
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
        { 1, 0, 2, 2, 0, 0, 0, 2, 2, 0, 0, 0, 2, 0, 0, 1 },
        { 1, 0, 2, 0, 0, 0, 0, 0, 2, 0, 0, 0, 2, 0, 0, 1 },
        { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
        { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 3, 3, 0, 0, 0, 1 },
        { 1, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 3, 0, 0, 0, 1 },
        { 1, 0, 2, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0, 1 },
        { 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 2, 0, 1 },
        { 1, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 1 },
        { 1, 0, 2, 0, 0, 0, 0, 0, 0, 3, 0, 0, 0, 0, 0, 1 },
        { 1, 0, 2, 0, 0, 0, 0, 0, 0, 3, 0, 0, 2, 0, 0, 1 },
        { 1, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 2, 0, 0, 1 },
        { 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
        { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 9, 1 }
    };

    public static Map Level1 { get; } = new(
        _level1Tiles,
        1.5f, 1.5f, 0f,
        new[] { (7, 7), (13, 3), (3, 10), (10, 12), (14, 8) },
        new[] { (5, 1), (10, 1), (1, 7), (14, 7), (1, 13), (14, 13) }
    );

    public Map(int[,] tiles, float startX, float startY, float startAngle,
        (int x, int y)[] enemySpawns, (int x, int y)[] torchPositions)
    {
        _tiles = tiles;
        Height = tiles.GetLength(0);
        Width = tiles.GetLength(1);
        StartX = startX;
        StartY = startY;
        StartAngle = startAngle;
        EnemySpawns = enemySpawns;
        TorchPositions = torchPositions;
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

    public bool IsBlocking(int x, int y)
    {
        var t = GetTile(x, y);
        return t != Tile.Empty;
    }
}