using HarmonyLib;
using InfiniteFriends.Algorithms;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InfiniteFriends.Patches
{
    // Extend the array length for player indexes
    [HarmonyPatch(typeof(LobbyController), MethodType.Constructor)]
    internal class LobbyController_Patch_Ctor
    {
        // Transpiles
        //    > private PlayerController[] _playerIndexes = new PlayerController[4]
        // to > private PlayerController[] _playerIndexes = new PlayerController[InfiniteFriends.MaxPlayerHardCap];
        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
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

    // Inject our generated spawn points
    [HarmonyPatch(typeof(LobbyController), nameof(LobbyController.GetSpawnPoints))]
    internal class LobbyController_Patch_GetSpawnPoints
    {
        [HarmonyPrefix]
        internal static bool Prefix(LobbyController __instance, ref Transform[] __result)
        {
            // Regenerate spawn points for a new level
            if (SpawnPoints.lastLevel != LevelController.instance.activeLevel || SpawnPoints.spawns[0] == null)
            {
                SpawnPoints.lastLevel = LevelController.instance.activeLevel;
                SpawnPoints.spawns = SpawnPoints.GetDefaultSpawnPoints().ToList();

                SpawnPoints.GenerateSpawnPoints(LobbyController.instance.CountPlayers() - SpawnPoints.spawns.Count);
            }

            __result = SpawnPoints.spawns.ToArray();
            return false;
        }
    }

    // Generate a spawn point for a new player
    [HarmonyPatch(typeof(LobbyController), "OnPlayerJoined")]
    internal class LobbyController_Patch_OnPlayerJoined
    {
        [HarmonyPrefix]
        internal static bool Prefix()
        {
            if (SpawnPoints.spawns.Count < PlayerInputManager.instance.playerCount)
            {
                SpawnPoints.GenerateSpawnPoints(1);
            }
            return true;
        }
    }

    // Intercept maxPlayers and reassign to InfiniteFriends.MaxPlayerHardCap
    [HarmonyPatch(typeof(LobbyController), nameof(LobbyController.SetMaxPlayer))]
    internal class LobbyController_Patch_SetMaxPlayer
    {
        [HarmonyPrefix]
        internal static bool Prefix(ref int maxPlayers)
        {
            maxPlayers = InfiniteFriends.MaxPlayerHardCap;
            return true;
        }
    }

    // Override the initial max players value
    [HarmonyPatch(typeof(LobbyController), "Start")]
    internal class LobbyController_Patch_Start
    {
        [HarmonyPostfix]
        internal static void Postfix(ref LobbyController __instance)
        {
            __instance.SetMaxPlayer(InfiniteFriends.MaxPlayerHardCap);
        }
    }
}
