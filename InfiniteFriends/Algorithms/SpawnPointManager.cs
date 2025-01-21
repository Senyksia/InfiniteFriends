using System.Collections.Generic;
using System.Linq;
using InfiniteFriends.Extensions;
using Pathfinding;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InfiniteFriends.Algorithms;

internal static class SpawnPointManager
{
    public static List<Transform> SpawnPoints = new Transform[4].ToList();
    public static GameLevel LastLevel;
    private static readonly NNConstraint WalkableConstraint = NNConstraint.Default;

    private static float _minDist = 100f;
    private static List<GraphNode> _defaultNodes;

    static SpawnPointManager()
    {
        WalkableConstraint.constrainWalkability = true;
        WalkableConstraint.walkable = true;
    }

    /// <summary>
    /// Determines if a level is airborne.
    /// If gravity is disabled, or no default spawn points are near a platform, the level is considered airborne.
    /// </summary>
    private static bool IsAirborneLevel()
    {
        return LevelController.instance.activeLevel.zeroGravity
               || GetDefaultSpawnPoints()
                   .All(t => !Physics2D.OverlapCircle(t.position, 50f, GameController.instance.worldLayers));
    }

    // TODO: Hopefully temporary
    /// <summary>
    /// Determines if the given point is far enough from existing spawn points.
    /// Failing this conditional eases the distance required.
    /// </summary>
    private static bool IsCorrectDistance(Vector3 pos)
    {
        bool ret = SpawnPoints.All(t => Vector2.Distance(t.position, pos) > _minDist);
        if (!ret) _minDist *= 0.9f;
        return ret;
    }

    private static bool PathExists(Vector3 pos) => _defaultNodes.Any(n => PathUtilities.IsPathPossible(n, AstarPath.active.GetNearest(pos).node));

    // TODO: Detect if an unobstructed death-zone is below on airborne-gravity maps
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

    /// <summary>
    /// Adds new dynamic spawn points to <c>SpawnPointManager.SpawnPoints</c>.
    /// </summary>
    /// <param name="spawnCount">Number of spawn points to generate</param>
    public static void GenerateSpawnPoints(int spawnCount)
    {
        if (spawnCount < 1 || SpawnPoints[0] == null) return;
        Transform[] defaultSpawns = GetDefaultSpawnPoints();

        // TODO: This is ugly
        // Handle levels with stacked spawns by simply duplicating the default points
        if (defaultSpawns
                .Select(t => Vector2.Distance(t.position, defaultSpawns[0].position))
                .Max()
            < 50f)
        {
            InfiniteFriends.Logger.LogDebug($"Parkour-style level; duplicating {spawnCount} spawn points.");
            for (int i = 0; i < spawnCount; i++)
            {
                Transform original = defaultSpawns[i % defaultSpawns.Length];
                Transform spawn = Object.Instantiate(original.gameObject, original.parent).transform;
                spawn.name = (SpawnPoints.Count+1).ToString();
                InfiniteFriends.Logger.LogDebug($"Adding spawn point {spawn.name}: {spawn.position}");
                SpawnPoints.Add(spawn);
            }
            return;
        }

        // Keep spawns relatively close together, particularly on
        // very large maps with dense default spawns (E.g. Lobby)
        Bounds spawnBounds = new();
        defaultSpawns.ToList().ForEach(t => spawnBounds.Encapsulate(t.position));
        spawnBounds.size = 1.2f * new Vector3(Mathf.Max(spawnBounds.size.x, spawnBounds.size.y), Mathf.Max(spawnBounds.size.x, spawnBounds.size.y));
        _minDist = spawnBounds.extents.x;

        // Get approximate level bounds
        Collider2D confiner = GameObject.Find("Confiner")?.GetComponent<Collider2D>();
        if (confiner == null) InfiniteFriends.Logger.LogWarning("This level is missing a confiner!?");
        Bounds levelBounds = confiner ? new(confiner.bounds.center, confiner.bounds.size) : new(new(0f, 0f), new(200f, 200f));
        levelBounds.Expand(200f);

        // Get A* pathfinding nodes for the default spawns.
        // These are used to validate that a path exists between
        // a generated point and at least one default spawn, to avoid OOB spawns.
        if (AstarPath.active == null) InfiniteFriends.Logger.LogWarning("This level is missing an active pathfinder!"); // TODO: Unlikely, but we should still handle this

        _defaultNodes = defaultSpawns
            .Select(t => AstarPath.active.GetNearest(t.position, WalkableConstraint).node)
            .ToList();
        if (_defaultNodes.All(n => n == null)) InfiniteFriends.Logger.LogWarning("Failed to map any default spawn to an A* graph node");

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

        bool isAirborne = IsAirborneLevel();

        // Generate spawns
        InfiniteFriends.Logger.LogDebug($"Generating {spawnCount} spawn points. Viable platforms: {platforms.Count} | Airborne: {isAirborne}");
        for (int i = 0; i < spawnCount; i++)
        {
            Transform spawn = isAirborne
                ? GenerateAirborneSpawnPoint(spawnBounds)
                : GenerateGroundedSpawnPoint(spawnBounds, platforms);
            spawn.gameObject.name = (SpawnPoints.Count+1).ToString(); // Consistency with default spawns
            InfiniteFriends.Logger.LogDebug($"Adding spawn point {spawn.name}: {spawn.position}");
            SpawnPoints.Add(spawn);
        }

        disabled.ForEach(p => p.enabled = false);
    }

    private static Transform GenerateAirborneSpawnPoint(Bounds spawnBounds)
    {
        Transform spawn = new GameObject().transform;

        int attempt = 0;
        do
        {
            if (++attempt > 100)
            {
                InfiniteFriends.Logger.LogWarning("Failed to generate valid spawn point after maximum attempts. Something has gone very wrong!");
                break;
            }

            // Choose a random point within level bounds
            spawn.position = new Vector2(
                Random.Range(spawnBounds.min.x, spawnBounds.max.x),
                Random.Range(spawnBounds.min.y, spawnBounds.max.y)
            );
        } while (!(IsLegalSpawn(spawn.position) && PathExists(spawn.position)));

        return spawn;
    }

    private static Transform GenerateGroundedSpawnPoint(Bounds spawnBounds, List<Collider2D> platforms)
    {
        Transform spawn = GenerateAirborneSpawnPoint(spawnBounds);

        do
        {
            // Magnetise to closest platform
            Vector2 closest = platforms
                .Select(p => p.ClosestPoint(spawn.position))
                .Where(v => v != (Vector2)spawn.position)
                .OrderBy(v => Vector2.Distance(v, spawn.position))
                .DefaultIfEmpty(spawn.position) // This is unlikely to occur
                .First();
            if (closest == (Vector2)spawn.position) InfiniteFriends.Logger.LogWarning("Failed to find a valid nearby platform for the current spawn point");

            spawn.position = closest + 5f * ((Vector2)spawn.position - closest).normalized; // Add some padding between the spawn and platform
        } while (!(IsLegalSpawn(spawn.position) && IsCorrectDistance(spawn.position)));

        return spawn;
    }
}
