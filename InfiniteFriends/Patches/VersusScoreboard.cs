using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;

namespace InfiniteFriends.Patches;

[HarmonyPatch(typeof(VersusScoreboard), nameof(VersusScoreboard.Awake))]
internal class VersusScoreboard_Patch_Awake
{
    // Replace hardcoded max player values
    // Transpiles
    //    > this._versusScoreDisplay = new VersusScoreUi[4]
    // to > this._versusScoreDisplay = new VersusScoreUi[InfiniteFriends.MaxPlayerHardCap]
    //    > for (int i = 0; i < 4; i++)
    // to > for (int i = 0; i < InfiniteFriends.MaxPlayerHardCap; i++)
    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        using IEnumerator<CodeInstruction> enumerator = instructions.GetEnumerator();
        CodeInstruction replace = new(OpCodes.Ldc_I4, InfiniteFriends.MaxPlayerHardCap);

        while (enumerator.MoveNext())
        {
            if (enumerator.Current != null && enumerator.Current.opcode == OpCodes.Ldc_I4_4)
            {
                yield return replace; // ldc.i4.4 -> ldc.i4 (InfiniteFriends.MaxPlayerHardCap)
            }
            else
            {
                yield return enumerator.Current;
            }
        }
    }

    // Swap out HorizontalLayoutGroup for GridLayoutGroup to display more scores
    // As GridLayoutGroup doesn't have forceExpandChildren, scores won't fill the entire width
    [HarmonyPostfix]
    internal static void Postfix(VersusScoreUi[] ____versusScoreDisplay)
    {
        GameObject layoutGroup = ____versusScoreDisplay[0].transform.parent.gameObject;
        Object.DestroyImmediate(layoutGroup.GetComponent<HorizontalLayoutGroup>());
        GridLayoutGroup grid = layoutGroup.AddComponent<GridLayoutGroup>();
        grid.childAlignment = TextAnchor.MiddleCenter;
        grid.cellSize = new Vector2(100f, 40f);
        grid.padding = new RectOffset(Mathf.RoundToInt(-grid.cellSize.x), 0, 40, 0);
    }
}

// Replace hardcoded max player values
[HarmonyPatch(typeof(VersusScoreboard), nameof(VersusScoreboard.Update))]
internal class VersusScoreboard_Patch_Update
{
    // Transpiles
    //    > for (int j = this._playerScores.Count; j < 4; j++)
    // to > for (int j = this._playerScores.Count; j < InfiniteFriends.MaxPlayerHardCap; j++)
    //    > for (int k = 0; k < 4; k++)
    // to > for (int k = 0; k < InfiniteFriends.MaxPlayerHardCap; k++)
    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        using IEnumerator<CodeInstruction> enumerator = instructions.GetEnumerator();
        CodeInstruction replace = new(OpCodes.Ldc_I4, InfiniteFriends.MaxPlayerHardCap);

        while (enumerator.MoveNext())
        {
            if (enumerator.Current != null && enumerator.Current.opcode == OpCodes.Ldc_I4_4)
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
