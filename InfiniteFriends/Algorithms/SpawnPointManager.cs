using System;
using System.Collections.Generic;
using System.Linq;
using InfiniteFriends.Extensions;
using Pathfinding;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InfiniteFriends.Algorithms;

public static class SpawnPointManager
{
    public static List<Transform> SpawnPoints = new Transform[4].ToList();
    public static GameLevel LastLevel;
    private static readonly NNConstraint WalkableConstraint = NNConstraint.Default;

    static SpawnPointManager()
    {
        SpawnPointManager.WalkableConstraint.constrainWalkability = true;
        SpawnPointManager.WalkableConstraint.walkable = true;
    }

    // TODO: Why not just raycast?
    /// <summary>
    /// Check the distance between each default spawn point and every platform.
    /// If no spawn points are near a platform, the level is considered airborne.
    /// </summary>
    private static bool IsAirborneLevel(List<Collider2D> platforms)
    {
        if (LevelController.instance.activeLevel.zeroGravity) return true;

        foreach (Transform spawn in GetDefaultSpawnPoints().ToList())
        {
            foreach (Collider2D platform in platforms)
            {
                Vector2 closest = platform.ClosestPoint(spawn.position);
                if (Vector2.Distance(closest , spawn.position) < 50f) // 50f is the wall magnet distance
                {
                    return false;
                }
            }
        }
        return true;
    }

    // TODO: Detect if an unobstructed deathzone is below on airborne-gravity maps
    private static bool IsLegalSpawn(Vector3 pos)
    {
        Collider2D suffocate = Physics2D.OverlapCircle(pos, 0.02f, GameController.instance.worldLayers);

        bool old = Physics2D.queriesHitTriggers; // Just Physics2D things
        Physics2D.queriesHitTriggers = true;
        Collider2D deathZone = Physics2D.OverlapCircle(pos, 15f, LayerMask.GetMask("Hazard"));
        Physics2D.queriesHitTriggers = old;

        return (!suffocate && !deathZone);
    }

    /// <summary>
    /// Get the 4 static spawn points for the current scene.
    /// </summary>
    /// <remarks>Replicates <c>LobbyController.GetSpawnPoints</c>.</remarks>
    public static Transform[] GetDefaultSpawnPoints()
    {
        Transform[] defaultSpawns = new Transform[4];
        GameObject spawnPoints = GameObject.Find("SpawnPoints");
        if (!spawnPoints) return defaultSpawns;

        for (int i = 0; i < spawnPoints.transform.childCount; i++)
        {
            defaultSpawns[i] = spawnPoints.transform.GetChild(i);
        }

        return defaultSpawns;
    }

    // TODO: Choose fairer locations (not next to another player)
    /// <summary>
    /// Adds new dynamic spawn points to <c>SpawnPointManager.SpawnPoints</c>.
    /// </summary>
    /// <param name="spawnCount">Number of spawn points to generate</param>
    public static void GenerateSpawnPoints(int spawnCount)
    {
        if (spawnCount < 1 || SpawnPointManager.SpawnPoints[0] == null) return;
        Transform[] defaultSpawns = GetDefaultSpawnPoints();

        // Keep spawns relatively close together, particularly on
        // very large maps with dense default spawns (E.g. Lobby)
        Bounds spawnBounds = new();
        defaultSpawns.ToList().ForEach(t => spawnBounds.Encapsulate(t.position));
        spawnBounds.size = 1.5f * new Vector3(Mathf.Max(spawnBounds.size.x, spawnBounds.size.y), Mathf.Max(spawnBounds.size.x, spawnBounds.size.y));

        // Get approximate level bounds
        Collider2D confiner = GameObject.Find("Confiner")?.GetComponent<Collider2D>();
        if (confiner == null) InfiniteFriends.Logger.LogWarning("This level is missing a confiner!?");
        Bounds levelBounds = confiner ? new(confiner.bounds.center, confiner.bounds.size) : new(new(0f, 0f), new(200f, 200f));
        levelBounds.Expand(200f);

        // Get A* pathfinding nodes for the default spawns.
        // These are used to validate that a path exists between
        // a generated point and at least one default spawn, to avoid OOB spawns.
        if (AstarPath.active == null) InfiniteFriends.Logger.LogWarning("This level is missing an active pathfinder!"); // TODO: Unlikely, but we should still handle this

        List<GraphNode> defaultNodes = defaultSpawns
            .Select(t => AstarPath.active.GetNearest(t.position, WalkableConstraint).node)
            .ToList();
        if (defaultNodes.All(n => n == null)) InfiniteFriends.Logger.LogWarning("Failed to map any default spawn to an A* graph node");
        bool PathExists(Vector3 pos) => defaultNodes.Any(n => PathUtilities.IsPathPossible(n, AstarPath.active.GetNearest(pos).node));

        // Filter for valid platforms to spawn on
        Collider2D[] colliders = Object.FindObjectsOfType<Collider2D>();
        List<Collider2D> platforms = colliders
            .Where(c => GameController.instance.worldLayers.Contains(c.gameObject.layer) && !c.isTrigger && levelBounds.Intersects(c.bounds))
            .ToList();

        // Some platforms are erroneously disabled at this point,
        // so we need to temporarily enable them to prevent
        // `Collider2D.ClosestPoint` returning 0.
        List<Collider2D> disabled = platforms
            .Where(p => !p.enabled)
            .ToList();
        disabled.ForEach(p => p.enabled = true);

        bool isAirborne = IsAirborneLevel(platforms);

        // Generate spawns
        InfiniteFriends.Logger.LogDebug($"Generating {spawnCount} spawn points. Viable platforms: {platforms.Count} | Airborne: {isAirborne}");
        int prevCount = SpawnPointManager.SpawnPoints.Count;
        for (int i = 0; i < spawnCount; i++)
        {
            // Initialise a new spawn point
            Transform spawn = new GameObject().transform;
            spawn.gameObject.name = (prevCount + i + 1).ToString(); // Consistency with default spawns

            do
            {
                int atmpt = 0;
                do
                {
                    if (atmpt++ >= 100)
                    {
                        InfiniteFriends.Logger.LogWarning("Failed to generate valid spawn point after maximum attempts. Something has gone very wrong!");
                        goto Finalize; // ew
                    }

                    // Choose a random point within level bounds
                    spawn.position = new Vector2(
                        UnityEngine.Random.Range(spawnBounds.min.x, spawnBounds.max.x),
                        UnityEngine.Random.Range(spawnBounds.min.y, spawnBounds.max.y)
                    );
                } while (!(IsLegalSpawn(spawn.position) && PathExists(spawn.position)));

                if (isAirborne) continue;

                // Magnetise to closest platform
                Vector2 closest = platforms
                    .Select(p => p.ClosestPoint(spawn.position))
                    .Where(v => v != (Vector2)spawn.position)
                    .OrderBy(v => Vector2.Distance(v, spawn.position))
                    .DefaultIfEmpty(spawn.position) // This is unlikely to occur
                    .First();
                if (closest == (Vector2)spawn.position) InfiniteFriends.Logger.LogWarning("Failed to find a valid nearby platform for the current spawn point");

                spawn.position = closest + 5f * ((Vector2)spawn.position - closest).normalized; // Add some padding between the spawn and platform
            } while (!IsLegalSpawn(spawn.position));

            Finalize:
            InfiniteFriends.Logger.LogDebug($"Adding spawn point {spawn.name}: {spawn.position}");
            SpawnPointManager.SpawnPoints.Add(spawn);
        }

        disabled.ForEach(p => p.enabled = false);
    }
}
