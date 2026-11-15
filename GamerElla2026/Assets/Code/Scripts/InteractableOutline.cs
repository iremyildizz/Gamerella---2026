using System.Linq;
using UnityEngine;

public class InteractableOutline : MonoBehaviour
{
    [Tooltip("Assign your OutlineMat here")]
    public Material outlineMaterial;

    private Renderer[] renderers;
    private bool isOutlined = false;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
    }

    public void SetOutline(bool active)
    {
        if (isOutlined == active || outlineMaterial == null) return;
        isOutlined = active;

        foreach (Renderer rend in renderers)
        {
            if (rend == null) continue;

            Material[] current = rend.sharedMaterials;
            Material[] withoutOutline = current.Where(m => m != outlineMaterial).ToArray();

            rend.sharedMaterials = active
                ? withoutOutline.Append(outlineMaterial).ToArray()
                : withoutOutline;
        }
    }
}
