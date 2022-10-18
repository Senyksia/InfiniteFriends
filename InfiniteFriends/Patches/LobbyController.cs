using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.Rendering;

namespace InfiniteFriends.Patches
{
    // Extend the array length for player indexes
    [HarmonyPatch(typeof(LobbyController), MethodType.Constructor)]
    class LobbyController_Patch_Ctor
    {
        // Transpiles
        //    > private PlayerController[] _playerIndexes = new PlayerController[4]
        // to > private PlayerController[] _playerIndexes = new PlayerController[InfiniteFriends.MaxPlayerHardCap];
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeInstruction[] array = instructions.ToArray();
            CodeInstruction match = new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(LobbyController), "_playerIndexes"));

            // Search for our match
            int i = 0;
            for (; i < array.Length; i++)
            {
                if (array[i].Is(match.opcode, match.operand)) break;
            }

            // Backtrack to the correct instruction
            if (i != array.Length-1)
            {
                array[i-2] = new CodeInstruction(OpCodes.Ldc_I4, InfiniteFriends.MaxPlayerHardCap); // ldc.i4.4 -> ldc.i4 (InfiniteFriends.MaxPlayerHardCap)
            }

            return array.AsEnumerable();
        }
    }

    // A Collider2D wrapper that holds spawning information
    public class SpawnPlatform : IComparable<SpawnPlatform>
    {
        public Collider2D collider;
        public Bounds bounds;
        public readonly float area;
        public int spawnCount = 0;
        public float weight
        {
            get { return this.area / (2*this.spawnCount + 1); }
        }

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
            if (other != null)
            {
                if (this.weight > other.weight) return 1;
                if (this.weight < other.weight) return -1;
                return 0;
            }
            return 1;
        }
    }

    // Rewrite the GetSpawnPoints() logic for more than 4 players
    // TODO: Choose fairer locations (not next to another player)
    // TODO: Move spawn generation out of GetSpawnPoints, to prevent unnecessary calls
    [HarmonyPatch(typeof(LobbyController), nameof(LobbyController.GetSpawnPoints))]
    class LobbyController_Patch_GetSpawnPoints
    {
        [HarmonyPrefix]
        static bool Prefix(LobbyController __instance, ref Transform[] __result)
        {
            // Choose a random legal spawn on the edge of a platform
            // Distributes the players among available platforms, weighted based on platform bounded area
            // TODO: Weight based on perimeter, rather than area
            static int ChoosePlatform(ref List<SpawnPlatform> platforms)
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

            // Check if the player would be spawned in a wall/lava
            static bool IsLegalSpawn(Vector3 pos)
            {
                RaycastHit2D worldRaycast = Physics2D.Raycast(pos, Vector2.up, 0.01f, GameController.instance.worldLayers);

                bool old = Physics2D.queriesHitTriggers; // Just Physics2D things
                Physics2D.queriesHitTriggers = true;
                Collider2D deathZone = Physics2D.OverlapCircle(pos, 15f, LayerMask.GetMask("Hazard"));
                Physics2D.queriesHitTriggers = old;

                return (!worldRaycast.collider && !deathZone);
            }

            // Init
            Transform[] spawns = new Transform[Math.Max(4, __instance.spawnedPlayers.Count)];
            GameObject spawnPoints = GameObject.Find("SpawnPoints");
            bool airborne = true; // Are all default spawns airborne?

            if (!spawnPoints)
            {
                __result = spawns;
                return false;
            }
            Transform defaultSpawns = spawnPoints.transform;

            // Get all platform colliders
            Collider2D[] colliders = GameObject.FindObjectsOfType<Collider2D>();
            List<SpawnPlatform> platforms = new List<SpawnPlatform>();
            string[] platformNames = new string[] { "Base", "Box", "Floor", "Platform", "WorldShape" };

            // Get approximate level bounds
            Collider2D confiner = (from c in colliders where c.name == "Confiner" select c).First();
            Bounds inbounds = new Bounds(confiner.bounds.center, confiner.bounds.size + new Vector3(200f, 200f, 0.2f));

            foreach (Collider2D collider in colliders)
            {
                // Is a type of platform AND inbounds
                if (platformNames.Any(name => collider.name.StartsWith(name))
                 && inbounds.Intersects(collider.bounds))
                {
                    platforms.Add(new SpawnPlatform(collider, inbounds));
                }
            }

            // Insert default spawns, and determine if any are attached to a platform
            for (int i = 0; i < defaultSpawns.childCount; i++)
            {
                spawns[i] = defaultSpawns.GetChild(i);
                foreach (SpawnPlatform platform in platforms)
                {
                    Vector2 point = platform.collider.ClosestPoint(spawns[i].position);
                    if (Vector2.Distance(point, spawns[i].position) < 50f) // 50f is the wall magnet distance
                    {
                        platform.spawnCount++;
                        airborne = false;
                        break;
                    }
                }
            }

            // Generate new spawns to match player count
            if (airborne)
            {
                // Find the minimum distance between any two default spawns
                float minDist = float.PositiveInfinity;
                for (int i = 0; i < defaultSpawns.childCount; i++)
                {
                    for (int j = i+1; j < defaultSpawns.childCount; j++)
                    {
                        float dist = Vector3.Distance(spawns[i].position, spawns[j].position);
                        minDist = Math.Min(minDist, dist);
                    }
                }

                // Generate spawns
                for (int i = defaultSpawns.childCount; i < __instance.spawnedPlayers.Count; i++)
                {
                    // Initialise a new spawn point
                    Transform spawn = new GameObject().transform;
                    spawn.gameObject.name = (i+1).ToString();

                    // Search for legal spawn locations around a random prior spawn
                    Vector2 initial = spawns[UnityEngine.Random.Range(0, i-1)].position;
                    do
                    {
                        spawn.position = initial + UnityEngine.Random.insideUnitCircle.normalized * minDist;
                    }
                    while (!IsLegalSpawn(spawn.position));

                    spawns[i] = spawn;
                }
            }
            else
            {
                // Generate spawns
                for (int i = defaultSpawns.childCount; i < __instance.spawnedPlayers.Count; i++)
                {
                    // Initialise a new spawn point
                    Transform spawn = new GameObject().transform;
                    spawn.gameObject.name = (i+1).ToString();

                    int index = ChoosePlatform(ref platforms);
                    SpawnPlatform platform = platforms[index];

                    int attempts = 0;
                    do
                    {
                        if (++attempts > 25)
                    {
                            PatchLogger.LogWarning($"Spawn platform '{platform.collider.name}' exceeded maximum spawning attempts ({attempts+1}), removing from spawning pool.");
                            platforms.RemoveAt(index);
                            index = ChoosePlatform(ref platforms);
                            platform = platforms[index];
                            attempts = 1;
                    }

                        // Choose a random point inbounds
                        Vector2 point = new Vector2(
                            UnityEngine.Random.Range(inbounds.min.x, inbounds.max.x),
                            UnityEngine.Random.Range(inbounds.min.y, inbounds.max.y));

                        // Magnetise to the platform perimeter
                        spawn.position = platform.collider.ClosestPoint(point);
                    }
                    while (!IsLegalSpawn(spawn.position));

                    platform.spawnCount++;
                    spawns[i] = spawn;
                }
            }

            __result = spawns;
            return false;
        }
    }

    // Alter the maxPlayers input of SetMaxPlayer() to always be InfiniteFriends.MaxPlayerHardCap
    [HarmonyPatch(typeof(LobbyController), nameof(LobbyController.SetMaxPlayer))]
    class LobbyController_Patch_SetMaxPlayer
    {
        [HarmonyPrefix]
        static bool Prefix(ref int maxPlayers)
        {
            maxPlayers = InfiniteFriends.MaxPlayerHardCap;
            return true;
        }
    }

    // Override the initial max players value
    [HarmonyPatch(typeof(LobbyController), "Start")]
    class LobbyController_Patch_Start
    {
        [HarmonyPostfix]
        static void Postfix(ref LobbyController __instance)
        {
            __instance.SetMaxPlayer(InfiniteFriends.MaxPlayerHardCap);
        }
    }
}
