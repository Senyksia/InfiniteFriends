using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace InfiniteFriends.Patches;

/// <summary>
/// Statically increase the number of preview spiders.
/// </summary>
[HarmonyPatch(typeof(CustomizationPreviewManager), nameof(CustomizationPreviewManager.Awake))]
internal class CustomizationPreviewManager_Patch_Awake
{
    [HarmonyPrefix]
    internal static bool Prefix(ref List<SpiderCustomizer> ___previewCustomizers, ref List<Animator> ___previewAnimators)
    {
        // Widen texture atlas
        Camera previewCamera = ___previewCustomizers[0].transform.parent.parent.GetComponentInChildren<Camera>(true);
        previewCamera.targetTexture.width = 512*InfiniteFriends.MAX_PLAYER_HARD_CAP;
        previewCamera.rect = new Rect(0f, 0f, InfiniteFriends.MAX_PLAYER_HARD_CAP, 1f);
        previewCamera.pixelRect = new Rect(0f, 0f, 512f*InfiniteFriends.MAX_PLAYER_HARD_CAP, 512f);

        // Extend the number of customizers and animators
        while (___previewCustomizers.Count < InfiniteFriends.MAX_PLAYER_HARD_CAP)
        {
            // Generate preview transform and customizer
            GameObject playerPreview = Object.Instantiate(___previewCustomizers[0].transform.parent.gameObject, ___previewCustomizers[0].transform.parent.parent);
            SpiderCustomizer customizer = playerPreview.GetComponentInChildren<SpiderCustomizer>(true);
            playerPreview.name = $"PlayerPreview {___previewCustomizers.Count+1}";
            ___previewCustomizers.Add(customizer);

            // Add animator
            Animator animator = customizer.GetComponentInChildren<Animator>(true);
            ___previewAnimators.Add(animator);
        }

        // Evenly space preview spiders for texture atlasing
        const float previewWidth = 22f;
        Vector3 leftmost = ___previewCustomizers[0].transform.parent.position;
        leftmost.x = (InfiniteFriends.MAX_PLAYER_HARD_CAP-1) * (previewWidth/2f) * -1f;

        for (int i = 0; i < ___previewCustomizers.Count; i++)
        {
            Transform playerPreview = ___previewCustomizers[i].transform.parent;
            playerPreview.position = leftmost + new Vector3(previewWidth*i, 0f, 0f);
        }

        return true;
    }
}
