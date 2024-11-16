using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InfiniteFriends.Algorithms;

public static class SpawnPointManager
{
    public static List<Transform> SpawnPoints = new Transform[4].ToList();
    public static GameLevel LastLevel;

    /// <summary>
    /// A Collider2D wrapper that holds spawning information.
    /// </summary>
    private class SpawnPlatform : IComparable<SpawnPlatform>
    {
        public readonly Collider2D collider;
        public readonly Bounds bounds;
        public readonly float area;
        public int spawnCount = 0;
        public float weight => this.area / (2*this.spawnCount + 1);

        public SpawnPlatform(Collider2D collider, Bounds inbounds)
        {
            this.collider = collider;

            this.bounds = new Bounds(collider.bounds.center, collider.bounds.size);
            this.bounds.min = new Vector3(
                Math.Max(this.bounds.min.x, inbounds.min.x),
                Math.Max(this.bounds.min.y, inbounds.min.y),
                0);
            this.bounds.max = new Vector3(
                Math.Min(this.bounds.max.x, inbounds.max.x),
                Math.Min(this.bounds.max.y, inbounds.max.y),
                0);

            this.area = this.bounds.size.x * this.bounds.size.y;
        }

        public int CompareTo(SpawnPlatform other)
        {
            if (other == null) return 1;
            if (this.weight > other.weight) return 1;
            if (this.weight < other.weight) return -1;
            return 0;
        }
    }

    // TODO: Why not just raycast?
    /// <summary>
    /// Check the distance between each default spawn point and every platform.
    /// If no spawn points are near a platform, the level is considered airborne.
    /// </summary>
    private static bool IsAirborneLevel(List<SpawnPlatform> platforms)
    {
        if (LevelController.instance.activeLevel.zeroGravity) return true;

        foreach (Transform spawn in GetDefaultSpawnPoints().ToList())
        {
            foreach (SpawnPlatform platform in platforms)
            {
                Vector2 closest = platform.collider.ClosestPoint(spawn.position);
                if (Vector2.Distance(closest , spawn.position) < 50f) // 50f is the wall magnet distance
                {
                    return false;
                }
            }
        }
        return true;
    }

    private static bool IsLegalSpawn(Vector3 pos)
    {
        Collider2D suffocate = Physics2D.OverlapCircle(pos, 0.02f, GameController.instance.worldLayers);

        bool old = Physics2D.queriesHitTriggers; // Just Physics2D things
        Physics2D.queriesHitTriggers = true;
        Collider2D deathZone = Physics2D.OverlapCircle(pos, 15f, LayerMask.GetMask("Hazard"));
        Physics2D.queriesHitTriggers = old;

        return (!suffocate && !deathZone);
    }

    // TODO: Weight based on perimeter, rather than area.
    /// <summary>
    /// Chooses a random platform, weighted based on its bounded area and spawn count.
    /// </summary>
    /// <returns>The chosen platform's index.</returns>
    private static int ChoosePlatformWeighted(ref List<SpawnPlatform> platforms)
    {
        // Ordered list of weights
        platforms.Sort();
        List<float> weights = (from p in platforms select p.weight).ToList();

        // Cumulatively sum weights
        for (int i = 1; i < weights.Count; i++)
        {
            weights[i] += weights[i-1];
        }

        // Normalise weights
        for (int i = 0; i < weights.Count; i++)
        {
            weights[i] /= weights.Last();
        }

        // Pull a random float, and determine which weight region it falls under
        float rand = UnityEngine.Random.value;
        int index = 0;
        while (rand > weights[index]) index++;
        return index;
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
    /// <param name="spawnCount"></param>
    public static void GenerateSpawnPoints(int spawnCount)
    {
        if (spawnCount < 1 || SpawnPointManager.SpawnPoints[0] == null) return;

        // Get all platform colliders
        Collider2D[] colliders = Object.FindObjectsOfType<Collider2D>();
        List<SpawnPlatform> platforms = [];
        string[] platformNames = ["Base", "BottomPlat", "Box", "Floor", "Platform", "Spire", "Support", "WorldShape"];

        // Get approximate level bounds
        Collider2D confiner = (from c in colliders where c.name == "Confiner" select c).First();
        Bounds inbounds = new Bounds(confiner.bounds.center, confiner.bounds.size + new Vector3(200f, 200f, 0.5f));

        foreach (Collider2D collider in colliders)
        {
            // Is a type of platform AND inbounds
            if (platformNames.Any(name => collider.name.StartsWith(name))
             && inbounds.Intersects(collider.bounds))
            {
                platforms.Add(new SpawnPlatform(collider, inbounds));
            }
        }

        if (IsAirborneLevel(platforms))
        {
            GenerateAirborneSpawnPoints(spawnCount);
        }
        else
        {
            GenerateGroundedSpawnPoints(spawnCount, platforms, inbounds);
        }
    }

    private static void GenerateAirborneSpawnPoints(int spawnCount)
    {
        List<Transform> defaultSpawns = GetDefaultSpawnPoints().ToList();

        // Find the minimum distance between any two default spawns
        float minDist = float.PositiveInfinity;
        for (int i = 0; i < defaultSpawns.Count; i++)
        {
            for (int j = i+1; j < defaultSpawns.Count; j++)
            {
                float dist = Vector3.Distance(defaultSpawns[i].position, defaultSpawns[j].position);
                minDist = Math.Min(minDist, dist);
            }
        }

        // Generate spawns
        int currentCount = SpawnPointManager.SpawnPoints.Count;
        for (int i = currentCount; i < currentCount+spawnCount; i++)
        {
            // Initialise a new spawn point
            Transform spawn = new GameObject().transform;
            spawn.gameObject.name = (i+1).ToString(); // Consistency with default spawns

            // Search for legal spawn locations around a random prior spawn
            Vector2 initial = SpawnPointManager.SpawnPoints[UnityEngine.Random.Range(0, SpawnPointManager.SpawnPoints.Count)].position;
            do
            {
                spawn.position = initial + UnityEngine.Random.insideUnitCircle.normalized * minDist;
            }
            while (!IsLegalSpawn(spawn.position));

            SpawnPointManager.SpawnPoints.Add(spawn);
        }
    }

    private static void GenerateGroundedSpawnPoints(int spawnCount, List<SpawnPlatform> platforms, Bounds inbounds)
    {
        // Generate spawns
        int currentCount = SpawnPointManager.SpawnPoints.Count;
        for (int i = currentCount; i < currentCount+spawnCount; i++)
        {
            // Initialise a new spawn point
            Transform spawn = new GameObject().transform;
            spawn.gameObject.name = (i+1).ToString(); // Consistency with default spawns

            int platformIndex = ChoosePlatformWeighted(ref platforms);
            SpawnPlatform platform = platforms[platformIndex];

            // Attempt to find a legal spawn on the chosen platform
            int attempts = 0;
            do
            {
                if (++attempts > 25)
                {
                    InfiniteFriends.Logger.LogWarning((string)$"Spawn platform '{platform.collider.name}' exceeded maximum spawning attempts ({attempts+1}), removing from spawning pool.");
                    platforms.RemoveAt(platformIndex);
                    platformIndex = ChoosePlatformWeighted(ref platforms);
                    platform = platforms[platformIndex];
                    attempts = 1;
                }

                // Choose a random point inbounds
                Vector2 point = new Vector2(
                    UnityEngine.Random.Range(inbounds.min.x, inbounds.max.x),
                    UnityEngine.Random.Range(inbounds.min.y, inbounds.max.y));

                // Magnetise to the platform perimeter
                Vector2 closest = platform.collider.ClosestPoint(point);
                spawn.position = closest + 5f*(point - closest).normalized; // This isn't smart but it works
            }
            while (!IsLegalSpawn(spawn.position));

            platform.spawnCount++;
            SpawnPointManager.SpawnPoints.Add(spawn);
        }
    }
}
