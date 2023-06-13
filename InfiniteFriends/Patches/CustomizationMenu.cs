using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace InfiniteFriends.Patches
{
    /*
     * Acts like a GridLayoutGroup, but uses localScale to shrink and fit more children.
     */

    public class ScalingGridLayoutGroup : LayoutGroup
    {
        public float aspectRatio = 1f;
        public Vector3 defaultScale = Vector3.one;

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
        }

        public override void CalculateLayoutInputVertical()
        {
        }

        public override void SetLayoutHorizontal()
        {
            SetCells();
        }

        public override void SetLayoutVertical()
        {
        }

        private void SetCells()
        {
            int count = rectChildren.Count;
            if (count < 1) return;

            float selfWidth = rectTransform.rect.width;
            float selfHeight = rectTransform.rect.height;
            int rows = Mathf.CeilToInt(Mathf.Sqrt(count*selfHeight*0.97f*aspectRatio/selfWidth));
            int cols = Mathf.CeilToInt((float)count/rows);
            float cellWidth = selfWidth/cols;
            float cellHeight = selfHeight/rows;

            // Position and scale children horizontally and vertically
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    int i = cols*row + col;
                    if (i >= count) break;

                    float shiftUp = (rows-1) * cellHeight/2f; // Fix vertical axis always positioning from the centre
                    rectChildren[i].localScale = defaultScale / Mathf.Pow(2, rows-1);
                    SetChildAlongAxis(rectChildren[i], 0, (col+0.5f) * cellWidth);
                    SetChildAlongAxis(rectChildren[i], 1, (row)      * cellHeight - shiftUp);
                }
            }
        }
    }

    /*
     * Statically increase the number of customization panels.
     * Ideally this would be done dynamically, but the network manager is uncooperative after initialising its children.
     */

    [HarmonyPatch(typeof(CustomizationMenu), "Start")]
    internal class CustomizationMenu_Patch_Start
    {
        [HarmonyPrefix]
        internal static bool Prefix(ref CustomizationPanel[] ___customizationPanels)
        {
            List<CustomizationPanel> panels = ___customizationPanels.ToList();

            // Replace HorizontalLayoutGroup with a ScalingGridLayoutGroup
            const float panelWidth  = 229f;
            const float panelHeight = 436.95f;
            GameObject layoutGroup = panels[0].transform.parent.gameObject;
            HorizontalLayoutGroup.DestroyImmediate(layoutGroup.GetComponent<HorizontalLayoutGroup>());
            ScalingGridLayoutGroup grid = layoutGroup.AddComponent<ScalingGridLayoutGroup>();
            grid.aspectRatio = panelWidth/panelHeight;
            grid.defaultScale = panels[0].transform.localScale;

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
}
