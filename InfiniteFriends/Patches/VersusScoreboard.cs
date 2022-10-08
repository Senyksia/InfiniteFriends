using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace InfiniteFriends.Patches
{
    // Replace hardcoded max player values
    [HarmonyPatch(typeof(VersusScoreboard), "Awake")]
    class VersusScoreboard_Patch_Awake
    {
        // Transpiles
        //    > this._versusScoreDisplay = new VersusScoreUi[4]
        // to > this._versusScoreDisplay = new VersusScoreUi[InfiniteFriends.MaxPlayerHardCap]
        //    > for (int i = 0; i < 4; i++)
        // to > for (int i = 0; i < InfiniteFriends.MaxPlayerHardCap; i++)
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            IEnumerator<CodeInstruction> enumerator = instructions.GetEnumerator();
            CodeInstruction replace = new CodeInstruction(OpCodes.Ldc_I4, InfiniteFriends.MaxPlayerHardCap);

            while (enumerator.MoveNext())
            {
                if (enumerator.Current.opcode == OpCodes.Ldc_I4_4)
                {
                    yield return replace; // ldc.i4.4 -> ldc.i4 (InfiniteFriends.MaxPlayerHardCap)
                }
                else
                {
                    yield return enumerator.Current;
                }
            }
        }
    }

    // Replace hardcoded max player values
    [HarmonyPatch(typeof(VersusScoreboard), "Update")]
    class VersusScoreboard_Patch_Update
    {
        // Transpiles
        //    > for (int j = this._playerScores.Count; j < 4; j++)
        // to > for (int j = this._playerScores.Count; j < InfiniteFriends.MaxPlayerHardCap; j++)
        //    > for (int k = 0; k < 4; k++)
        // to > for (int k = 0; k < InfiniteFriends.MaxPlayerHardCap; k++)
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            IEnumerator<CodeInstruction> enumerator = instructions.GetEnumerator();
            CodeInstruction replace = new CodeInstruction(OpCodes.Ldc_I4, InfiniteFriends.MaxPlayerHardCap);

            while (enumerator.MoveNext())
            {
                if (enumerator.Current.opcode == OpCodes.Ldc_I4_4)
                {
                    yield return replace; // ldc.i4.4 -> ldc.i4 (InfiniteFriends.MaxPlayerHardCap)
                }
                else
                {
                    yield return enumerator.Current;
                }
            }
        }
    }
}
