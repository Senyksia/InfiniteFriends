using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace InfiniteFriends.Patches
{
    /*
     * The following patches statically increase the number of customization panels and preview spiders.
     * Ideally this would be done dynamically, but the network manager is uncooperative after initialising its children.
     */

    [HarmonyPatch(typeof(CustomizationMenu), "Start")]
    class CustomizationMenu_Patch_Start
    {
        [HarmonyPrefix]
        static bool Prefix(ref CustomizationPanel[] ___customizationPanels)
        {
            List<CustomizationPanel> panels = ___customizationPanels.ToList();

            // Extend the number of customization panels
            while (panels.Count < InfiniteFriends.MaxPlayerHardCap)
            {
                // Generate panel
                CustomizationPanel panel = CustomizationPanel.Instantiate(___customizationPanels[0], ___customizationPanels[0].transform.parent);
                panel.name = $"{___customizationPanels[0].name} ({panels.Count})";

                Image image = panel.transform.Find("RenderTexturePreview").GetComponent<Image>();
                image.material = Material.Instantiate(image.material);

                panels.Add(panel);
            }

            // Rescale materials for texture atlasing
            for (int i = 0; i < panels.Count; i++)
            {
                Material material = panels[i].transform.Find("RenderTexturePreview").GetComponent<Image>().material;
                material.mainTextureOffset = new Vector2(i/(float)InfiniteFriends.MaxPlayerHardCap, 0f);
                material.mainTextureScale = new Vector2(1f/(float)InfiniteFriends.MaxPlayerHardCap, 1f);
            }

            ___customizationPanels = panels.ToArray();
            return true;
        }
    }

    [HarmonyPatch(typeof(CustomizationPreviewManager), "Awake")]
    class CustomizationPreviewManager_Patch_Awake
    {
        [HarmonyPrefix]
        static bool Prefix(ref List<SpiderCustomizer> ___previewCustomizers, ref List<Animator> ___previewAnimators)
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
