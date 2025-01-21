using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace InfiniteFriends.Patches;

/// <summary>
/// Acts like a GridLayoutGroup, but uses localScale to shrink and fit more children.
/// </summary>
public class ScalingGridLayoutGroup : LayoutGroup
{
    public float aspectRatio = 1f;
    public Vector3 defaultScale = Vector3.one;

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
    }

    public override void CalculateLayoutInputVertical() { }
    public override void SetLayoutHorizontal() => this.SetCells();
    public override void SetLayoutVertical() { }

    private void SetCells()
    {
        int count = this.rectChildren.Count;
        if (count < 1) return;

        float selfWidth = this.rectTransform.rect.width;
        float selfHeight = this.rectTransform.rect.height;
        int rows = Mathf.CeilToInt(Mathf.Sqrt(count*selfHeight*0.97f*this.aspectRatio/selfWidth));
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
                this.rectChildren[i].localScale = this.defaultScale / Mathf.Pow(2, rows-1);
                this.SetChildAlongAxis(this.rectChildren[i], 0, (col+0.5f) * cellWidth);
                this.SetChildAlongAxis(this.rectChildren[i], 1, (row)      * cellHeight - shiftUp);
            }
        }
    }
}

/// <summary>
/// Statically increase the number of customization panels.
/// Ideally this would be done dynamically, but the network manager is uncooperative after initialising its children.
/// </summary>

[HarmonyPatch(typeof(CustomizationMenu), nameof(CustomizationMenu.Start))]
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
        Object.DestroyImmediate(layoutGroup.GetComponent<HorizontalLayoutGroup>());
        ScalingGridLayoutGroup grid = layoutGroup.AddComponent<ScalingGridLayoutGroup>();
        grid.aspectRatio = panelWidth/panelHeight;
        grid.defaultScale = panels[0].transform.localScale;

        // Extend the number of customization panels
        while (panels.Count < InfiniteFriends.MAX_PLAYER_HARD_CAP)
        {
            // Generate panel
            CustomizationPanel panel = Object.Instantiate(___customizationPanels[0], ___customizationPanels[0].transform.parent);
            panel.name = $"{___customizationPanels[0].name} ({panels.Count})";

            Image image = panel.transform.Find("RenderTexturePreview").GetComponent<Image>();
            image.material = Object.Instantiate(image.material);

            panels.Add(panel);
        }

        // Rescale materials for texture atlasing
        for (int i = 0; i < panels.Count; i++)
        {
            Material material = panels[i].transform.Find("RenderTexturePreview").GetComponent<Image>().material;
            material.mainTextureOffset = new Vector2(i/(float)InfiniteFriends.MAX_PLAYER_HARD_CAP, 0f);
            material.mainTextureScale = new Vector2(1f/(float)InfiniteFriends.MAX_PLAYER_HARD_CAP, 1f);
        }

        ___customizationPanels = panels.ToArray();
        return true;
    }
}
