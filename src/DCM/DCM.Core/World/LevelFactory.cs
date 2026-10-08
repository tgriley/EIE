using DCM.Core.Entities;
using System.Collections.Generic;

namespace DCM.Core.World;

// Builds a stage's entities from its Map and loaded assets.
public static class LevelFactory
{
    public static List<Enemy> CreateEnemies(Map map, LevelAssets assets, float stunDuration)
    {
        var enemies = new List<Enemy>();
        foreach (var spawn in map.EnemySpawns)
        {
            if (!map.IsValidSpawn(spawn.x, spawn.y)) continue;
            var si = spawn.type % assets.EnemySheets.Length;
            enemies.Add(new Enemy(spawn.x, spawn.y,
                assets.EnemySheets[si],
                assets.EnemyHideSheets[si % assets.EnemyHideSheets.Length],
                EnemyCatalog.IsCameraImmune(spawn.type),
                stunDuration));
        }
        return enemies;
    }

    public static List<IPickup> CreatePickups(Map map)
        => new() { new HealthPickup(map.PickupSpawn.x, map.PickupSpawn.y) };
}
