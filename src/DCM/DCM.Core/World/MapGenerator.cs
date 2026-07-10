#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace DCM.Core.World;

// Seeded BSP map generator for endless mode. Produces the same shape of data
// as the hand-authored JSON levels, so the resulting Map is a drop-in for
// PlayScreen: solid border, connected rooms and corridors, start in the
// top-left region, and far-wall tiles left open so Map.RelocateExit can place
// a reachable exit. Difficulty (size, enemy volume, immune-dominant mix)
// scales with the stage number; the same (stage, seed) pair always yields the
// same map.
public static class MapGenerator
{
    private const int MinSize = 16;
    private const int MaxSize = 32;
    private const int MinLeaf = 6;
    private const int MaxAttempts = 20;

    public static Map Generate(int stage, int seed)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var map = TryGenerate(stage, seed + attempt * 7919);
            if (map != null) return map;
        }
        return FallbackArena(stage, seed);
    }

    private static Map? TryGenerate(int stage, int seed)
    {
        var rng  = new Random(seed);
        var size = Math.Min(MinSize + stage * 2, MaxSize);

        var tiles = new int[size, size];
        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
            tiles[y, x] = Tile.Wall1;

        var rooms = new List<(int x, int y, int w, int h)>();
        BuildRegion(tiles, rng, 1, 1, size - 2, size - 2, rooms);

        // Extra corridors between random room pairs so the layout has loops
        // rather than a pure tree of dead-ends.
        var centers = rooms.Select(RoomCenter).ToList();
        for (var i = 0; i < rooms.Count / 2; i++)
        {
            var a = centers[rng.Next(centers.Count)];
            var b = centers[rng.Next(centers.Count)];
            if (a != b) CarveCorridor(tiles, rng, a, b);
        }

        var start = centers.OrderBy(c => c.x * c.x + c.y * c.y).First();

        EnsureExitCandidates(tiles, rng, size, centers);

        if (!AllFloorReachable(tiles, size, start)) return null;

        var spawns = PlaceEnemies(tiles, rng, size, start, stage);
        if (spawns == null) return null;

        var cx = size / 2.0 - start.x;
        var cy = size / 2.0 - start.y;
        var angle = (float)Math.Atan2(cy, cx);

        return new Map(tiles, start.x + 0.5f, start.y + 0.5f, angle,
            spawns, [], rng.Next(2));
    }

    // ── BSP ──────────────────────────────────────────────────────────────────

    private static (int x, int y) BuildRegion(int[,] tiles, Random rng,
        int x, int y, int w, int h, List<(int x, int y, int w, int h)> rooms)
    {
        var canSplitW = w >= MinLeaf * 2;
        var canSplitH = h >= MinLeaf * 2;
        if (!canSplitW && !canSplitH)
            return CarveRoom(tiles, rng, x, y, w, h, rooms);

        bool vertical;
        if (canSplitW && canSplitH)
            vertical = w != h ? w > h : rng.Next(2) == 0;
        else
            vertical = canSplitW;

        (int x, int y) a, b;
        if (vertical)
        {
            var cut = rng.Next(MinLeaf, w - MinLeaf + 1);
            a = BuildRegion(tiles, rng, x, y, cut, h, rooms);
            b = BuildRegion(tiles, rng, x + cut, y, w - cut, h, rooms);
        }
        else
        {
            var cut = rng.Next(MinLeaf, h - MinLeaf + 1);
            a = BuildRegion(tiles, rng, x, y, w, cut, rooms);
            b = BuildRegion(tiles, rng, x, y + cut, w, h - cut, rooms);
        }
        CarveCorridor(tiles, rng, a, b);
        return rng.Next(2) == 0 ? a : b;
    }

    private static (int x, int y) CarveRoom(int[,] tiles, Random rng,
        int x, int y, int w, int h, List<(int x, int y, int w, int h)> rooms)
    {
        var rw = Math.Min(w, 3 + rng.Next(6));
        var rh = Math.Min(h, 3 + rng.Next(6));
        var rx = x + rng.Next(w - rw + 1);
        var ry = y + rng.Next(h - rh + 1);

        for (var ty = ry; ty < ry + rh; ty++)
        for (var tx = rx; tx < rx + rw; tx++)
            tiles[ty, tx] = Tile.Empty;

        var room = (rx, ry, rw, rh);
        var center = RoomCenter(room);

        // A pillar for cover in larger rooms; never on the room centre, which
        // doubles as a corridor endpoint and possible player start.
        if (rw >= 5 && rh >= 5)
        {
            var px = rx + 1 + rng.Next(rw - 2);
            var py = ry + 1 + rng.Next(rh - 2);
            if ((px, py) != center)
                tiles[py, px] = rng.Next(2) == 0 ? Tile.Wall2 : Tile.Wall3;
        }

        rooms.Add(room);
        return center;
    }

    private static (int x, int y) RoomCenter((int x, int y, int w, int h) r)
        => (r.x + r.w / 2, r.y + r.h / 2);

    private static void CarveCorridor(int[,] tiles, Random rng,
        (int x, int y) a, (int x, int y) b)
    {
        var (mx, my) = rng.Next(2) == 0 ? (b.x, a.y) : (a.x, b.y);
        CarveLine(tiles, a.x, a.y, mx, my);
        CarveLine(tiles, mx, my, b.x, b.y);
    }

    private static void CarveLine(int[,] tiles, int x0, int y0, int x1, int y1)
    {
        var dx = Math.Sign(x1 - x0);
        var dy = Math.Sign(y1 - y0);
        var x = x0; var y = y0;
        tiles[y, x] = Tile.Empty;
        while (x != x1) { x += dx; tiles[y, x] = Tile.Empty; }
        while (y != y1) { y += dy; tiles[y, x] = Tile.Empty; }
    }

    // ── Validation and population ────────────────────────────────────────────

    // Map.RelocateExit needs at least one open tile on the inner ring of the
    // far walls (row size-2 or column size-2). If the layout left none, carve
    // a corridor from the nearest room out to a random far-wall point.
    private static void EnsureExitCandidates(int[,] tiles, Random rng, int size,
        List<(int x, int y)> centers)
    {
        for (var i = 1; i < size - 1; i++)
            if (tiles[size - 2, i] == Tile.Empty || tiles[i, size - 2] == Tile.Empty)
                return;

        var target = rng.Next(2) == 0
            ? (x: rng.Next(2, size - 2), y: size - 2)
            : (x: size - 2, y: rng.Next(2, size - 2));
        var from = centers
            .OrderBy(c => (c.x - target.x) * (c.x - target.x) + (c.y - target.y) * (c.y - target.y))
            .First();
        CarveCorridor(tiles, rng, from, target);
    }

    private static bool AllFloorReachable(int[,] tiles, int size, (int x, int y) start)
    {
        if (tiles[start.y, start.x] != Tile.Empty) return false;

        var seen = new bool[size, size];
        var queue = new Queue<(int x, int y)>();
        seen[start.y, start.x] = true;
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            foreach (var (nx, ny) in new[] { (x, y - 1), (x, y + 1), (x - 1, y), (x + 1, y) })
            {
                if (seen[ny, nx] || tiles[ny, nx] != Tile.Empty) continue;
                seen[ny, nx] = true;
                queue.Enqueue((nx, ny));
            }
        }

        for (var y = 1; y < size - 1; y++)
        for (var x = 1; x < size - 1; x++)
            if (tiles[y, x] == Tile.Empty && !seen[y, x])
                return false;
        return true;
    }

    // Farthest-point sampling over open tiles, mirroring the campaign's spawn
    // spread: an even scatter, immune types (5,6) outnumbering the distinct
    // camera-sensitive types (0-4) at every stage.
    private static (int x, int y, int type)[]? PlaceEnemies(int[,] tiles, Random rng,
        int size, (int x, int y) start, int stage)
    {
        // Nothing spawns inside the enemy aggro radius of the player start, so
        // a level never opens with an instant chase.
        var minDistSq = Entities.Enemy.ChaseRange * Entities.Enemy.ChaseRange;
        var candidates = new List<(int x, int y)>();
        for (var y = 1; y < size - 1; y++)
        for (var x = 1; x < size - 1; x++)
        {
            if (tiles[y, x] != Tile.Empty) continue;
            var dx = x - start.x; var dy = y - start.y;
            if (dx * dx + dy * dy < minDistSq) continue;
            candidates.Add((x, y));
        }
        if (candidates.Count == 0) return null;

        // Uncapped growth: one more enemy per stage, limited only by how many
        // valid spawn tiles the map actually has.
        var total     = Math.Min(3 + stage, candidates.Count);
        var sensitive = Math.Min(5, (total - 1) / 2);
        var immune    = total - sensitive;

        var chosen = new List<(int x, int y)>
        {
            candidates.OrderByDescending(c =>
                (c.x - start.x) * (c.x - start.x) + (c.y - start.y) * (c.y - start.y)).First()
        };
        while (chosen.Count < total)
        {
            var best = candidates
                .Where(c => !chosen.Contains(c))
                .OrderByDescending(c => chosen.Min(ch =>
                    (c.x - ch.x) * (c.x - ch.x) + (c.y - ch.y) * (c.y - ch.y)))
                .First();
            chosen.Add(best);
        }

        var types = new List<int>();
        var s = 0; var im = 0;
        while (s < sensitive || im < immune)
        {
            if (s < sensitive) types.Add(s++);
            if (im < immune)   types.Add(im++ % 2 == 0 ? 5 : 6);
        }

        chosen.Sort((a, b) =>
            ((a.x - start.x) * (a.x - start.x) + (a.y - start.y) * (a.y - start.y))
            .CompareTo((b.x - start.x) * (b.x - start.x) + (b.y - start.y) * (b.y - start.y)));

        return chosen.Select((c, i) => (c.x, c.y, types[i])).ToArray();
    }

    // Guaranteed-valid open arena used only if every generation attempt fails.
    private static Map FallbackArena(int stage, int seed)
    {
        var rng  = new Random(seed);
        var size = Math.Min(MinSize + stage * 2, MaxSize);
        var tiles = new int[size, size];
        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
            tiles[y, x] = x == 0 || y == 0 || x == size - 1 || y == size - 1
                ? Tile.Wall1 : Tile.Empty;

        var spawns = PlaceEnemies(tiles, rng, size, (2, 2), stage) ?? [];
        return new Map(tiles, 2.5f, 2.5f, 0f, spawns, [], rng.Next(2));
    }
}
