using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace InfiniteFriends.Patches
{
    /*
     * Statically increase the number of preview spiders.
     */

    [HarmonyPatch(typeof(CustomizationPreviewManager), "Awake")]
    internal class CustomizationPreviewManager_Patch_Awake
    {
        [HarmonyPrefix]
        internal static bool Prefix(ref List<SpiderCustomizer> ___previewCustomizers, ref List<Animator> ___previewAnimators)
        {
            // Widen texture atlas
            Camera previewCamera = ___previewCustomizers[0].transform.parent.parent.GetComponentInChildren<Camera>(true);
            previewCamera.targetTexture.width = 512*InfiniteFriends.MaxPlayerHardCap;
            previewCamera.rect = new Rect(0f, 0f, InfiniteFriends.MaxPlayerHardCap, 1f);
            previewCamera.pixelRect = new Rect(0f, 0f, 512f*InfiniteFriends.MaxPlayerHardCap, 512f);

            // Extend the number of customizers and animators
            while (___previewCustomizers.Count < InfiniteFriends.MaxPlayerHardCap)
            {
                // Generate preview transform and customizer
                GameObject playerPreview = GameObject.Instantiate(___previewCustomizers[0].transform.parent.gameObject, ___previewCustomizers[0].transform.parent.parent);
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
            leftmost.x = (InfiniteFriends.MaxPlayerHardCap-1) * (previewWidth/2f) * -1f;

            for (int i = 0; i < ___previewCustomizers.Count; i++)
            {
                Transform playerPreview = ___previewCustomizers[i].transform.parent;
                playerPreview.position = leftmost + new Vector3(previewWidth*i, 0f, 0f);
            }

            return true;
        }
    }
}
