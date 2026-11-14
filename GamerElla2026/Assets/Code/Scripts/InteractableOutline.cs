using System.Collections.Generic;
using UnityEngine;

public class InteractableOutline : MonoBehaviour
{
    [Tooltip("Assign your OutlineMat here")]
    public Material outlineMaterial;

    private Renderer[] renderers;
    private readonly Dictionary<Renderer, Material[]> originalMaterials = new();
    private bool isOutlined = false;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            originalMaterials[rend] = rend.sharedMaterials;
        }
    }

    public void SetOutline(bool active)
    {
        if (isOutlined == active || outlineMaterial == null) return;
        isOutlined = active;

        foreach (Renderer rend in renderers)
        {
            if (rend == null) continue;

            if (active)
            {
                Material[] orig = originalMaterials[rend];
                Material[] withOutline = new Material[orig.Length + 1];
                orig.CopyTo(withOutline, 0);
                withOutline[orig.Length] = outlineMaterial;
                rend.materials = withOutline;
            }
            else
            {
                rend.materials = originalMaterials[rend];
            }
        }
    }
}