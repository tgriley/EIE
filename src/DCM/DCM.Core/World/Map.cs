#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Text.Json;

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

    // Enemy spawn positions with enemy type index
    public readonly (int x, int y, int type)[] EnemySpawns;

    // Torch positions (tile coords where wall has a torch)
    public readonly (int x, int y)[] TorchPositions;

    // Texture set suffix used by PlayScreen to load wall/floor/ceiling assets
    public readonly int TextureVariant;

    // Health pickup spawn (one per level, chosen randomly at startup)
    public readonly (int x, int y) PickupSpawn;

    public const int LevelCount = 10;

    public static Map GetLevel(int index)
    {
        if (index < 0 || index >= LevelCount)
            throw new ArgumentOutOfRangeException(nameof(index));
        return LoadLevel(index);
    }

    private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    private static Map LoadLevel(int index)
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "Content", "Levels", $"level{index + 1:D2}.json");
        var json = File.ReadAllText(path);
        var data = JsonSerializer.Deserialize<MapData>(json, _jsonOpts)!;

        var rows = data.Tiles.Length;
        var cols = data.Tiles[0].Length;
        var tiles = new int[rows, cols];
        for (var r = 0; r < rows; r++)
            for (var c = 0; c < cols; c++)
                tiles[r, c] = data.Tiles[r][c];

        return new Map(
            tiles,
            data.StartX, data.StartY, data.StartAngle,
            data.EnemySpawns.Select(p => (p[0], p[1], p.Length > 2 ? p[2] : 0)).ToArray(),
            data.TorchPositions.Select(p => (p[0], p[1])).ToArray(),
            data.TextureVariant ?? 0);
    }

    private sealed class MapData
    {
        public float StartX { get; set; }
        public float StartY { get; set; }
        public float StartAngle { get; set; }
        public int[][] Tiles { get; set; } = [];
        public int[][] EnemySpawns { get; set; } = [];
        public int[][] TorchPositions { get; set; } = [];
        public int? TextureVariant { get; set; }
    }

    public Map(int[,] tiles, float startX, float startY, float startAngle,
        (int x, int y, int type)[] enemySpawns, (int x, int y)[] torchPositions,
        int textureVariant = 0)
    {
        _tiles = tiles;
        Height = tiles.GetLength(0);
        Width = tiles.GetLength(1);
        StartX = startX;
        StartY = startY;
        StartAngle = startAngle;
        EnemySpawns = enemySpawns;
        TorchPositions = torchPositions;
        TextureVariant = textureVariant;
        PickupSpawn = ChoosePickupSpawn();
    }

    private (int x, int y) ChoosePickupSpawn()
    {
        var rng = new Random();
        var startTileX = (int)StartX;
        var startTileY = (int)StartY;

        var candidates = new System.Collections.Generic.List<(int, int)>();
        for (var ty = 0; ty < Height; ty++)
        for (var tx = 0; tx < Width; tx++)
        {
            if (_tiles[ty, tx] != Tile.Empty) continue;
            if (Math.Abs(tx - startTileX) <= 3 && Math.Abs(ty - startTileY) <= 3) continue;
            var blocked = false;
            foreach (var s in EnemySpawns)
                if (s.x == tx && s.y == ty) { blocked = true; break; }
            if (blocked || !IsValidSpawn(tx, ty)) continue;
            candidates.Add((tx, ty));
        }

        return candidates.Count > 0
            ? candidates[rng.Next(candidates.Count)]
            : (startTileX + 4, startTileY);
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
