using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

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


    // Rewrite the GetSpawnPoints() logic for more than 4 players
    // TODO: Choose fairer locations (not above/near lava, not next to another player)
    [HarmonyPatch(typeof(LobbyController), nameof(LobbyController.GetSpawnPoints))]
    class LobbyController_Patch_GetSpawnPoints
    {
        [HarmonyPrefix]
        static bool Prefix(LobbyController __instance, ref Transform[] __result)
        {
            // Check if the player would be spawned in a wall/lava
            static bool IsLegalSpawn(Vector3 pos)
            {
                RaycastHit2D raycastHit2D = Physics2D.Raycast(pos, Vector2.up, 0.01f, GameController.instance.worldLayers);
                return (!raycastHit2D.collider);
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
            List<Collider2D> platforms = new List<Collider2D>();
            string[] platformNames = new string[] { "Base", "Box", "Floor", "Platform", "WorldShape" };

            foreach (Collider2D col in colliders)
            {
                if (platformNames.Any(name => col.name.StartsWith(name)))
                {
                    platforms.Add(col);
                }
            }

            // Insert default spawns, and determine if any are attached to a platform
            for (int i = 0; i < defaultSpawns.childCount; i++)
            {
                spawns[i] = defaultSpawns.GetChild(i);
                foreach (Collider2D platform in platforms)
                {
                    Vector2 point = platform.ClosestPoint(spawns[i].position);
                    if (Vector2.Distance(point, spawns[i].position) < 50f) // 50f is the wall magnet distance
                    {
                        platforms.Remove(platform);
                        platforms.Insert(0, platform);
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
                        if (dist < minDist) minDist = dist;
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

                    // Choose a random legal spawn on the edge of a platform
                    // Equally distributes the players among available platforms
                    Collider2D platform = platforms[i%platforms.Count];
                    Bounds bounds = platform.bounds;
                    do
                    {
                        // Choose a random point inside the platform
                        Vector2 point = new Vector2(
                            UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                            UnityEngine.Random.Range(bounds.min.y, bounds.max.y));

                        // Magnetise to a point slightly outside the platform edge, to prevent clipping
                        spawn.position = platform.ClosestPoint(point);
                        spawn.position += (Vector3)((Vector2)spawn.position - (Vector2)bounds.center).normalized * 10f;
                    }
                    while (!IsLegalSpawn(spawn.position));

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
