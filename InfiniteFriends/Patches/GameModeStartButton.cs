using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace InfiniteFriends.Patches;

// Bypass 1 player limit for certain gamemode buttons
[HarmonyPatch(typeof(GameModeStartButton), nameof(GameModeStartButton.StartCountDown))]
internal class GameModeStartButton_Patch_StartCountDown
{
    // Transpiles
    //    > playerCount != 1
    // to > playerCount < 1
    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        using IEnumerator<CodeInstruction> enumerator = instructions.GetEnumerator();
        List<CodeInstruction> match =
        [
            new(OpCodes.Ldloc_0),
            new(OpCodes.Ldc_I4_1),
            new(OpCodes.Beq)
        ];
        CodeInstruction replace = new(OpCodes.Bge);

        // Match and replace last instruction
        int score = 0;
        while (enumerator.MoveNext())
        {
            if (enumerator.Current != null && enumerator.Current.opcode == match[score].opcode)
            {
                score++;
                if (score == match.Count)
                {
                    enumerator.Current.opcode = replace.opcode;
                    score = 0;
                }
            }
            else score = 0;

            yield return enumerator.Current;
        }
    }
}

// The parkour button has an additional 1 player check
[HarmonyPatch(typeof(GameModeStartButton), nameof(GameModeStartButton.ShowGameModePrompt))]
internal class GameModeStartButton_Patch_ShowGameModePrompt
{
    // Transpiles
    //    > LobbyController.instance.GetPlayerCount() == 1
    // to > LobbyController.instance.GetPlayerCount() >= 1
    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        using IEnumerator<CodeInstruction> enumerator = instructions.GetEnumerator();
        List<CodeInstruction> match =
        [
            new(OpCodes.Ldsfld),
            new(OpCodes.Callvirt),
            new(OpCodes.Ldc_I4_1),
            new(OpCodes.Bne_Un)
        ];
        CodeInstruction replace = new(OpCodes.Blt_Un);

        // Match and replace last instruction
        int score = 0;
        while (enumerator.MoveNext())
        {
            if (enumerator.Current != null && enumerator.Current.opcode == match[score].opcode)
            {
                score++;
                if (score == match.Count)
                {
                    enumerator.Current.opcode = replace.opcode;
                    score = 0;
                }
            }
            else score = 0;

            yield return enumerator.Current;
        }

    }
}

// Remove "ONLY 1 PLAYER ALLOWED" text that shows above start buttons
[HarmonyPatch(typeof(GameModeStartButton), nameof(GameModeStartButton.Start))]
internal class GameModeStartButton_Patch_Start
{
    [HarmonyPrefix]
    internal static bool Prefix(ref string ____onePlayerMaxString)
    {
        ____onePlayerMaxString = ""; // Sledgehammer approach
        return true;
    }
}
