using UnityEngine;

// Put on a security camera's root (the object with the Box Collider, on the
// Interactable layer). Clicking it makes the player's camera fill the view
// with this screen; clicking again goes back to normal.
public class ZoomableScreen : MonoBehaviour, IInteractable
{
    [Tooltip("The quad showing the camera image (ScreenImage)")]
    [SerializeField] Transform screenQuad;

    [Tooltip("Extra space around the screen when zoomed (1 = edge to edge)")]
    [SerializeField, Min(1f)] float padding = 1.05f;

    public Transform ScreenQuad => screenQuad;
    public float Padding => padding;

    static ScreenZoomController controller;

    private InteractableOutline outline;

    void Awake()
    {
        if (screenQuad == null)
        {
            var r = GetComponentInChildren<MeshRenderer>();
            foreach (var mr in GetComponentsInChildren<MeshRenderer>())
                if (mr.name.StartsWith("ScreenImage")) { r = mr; break; }
            if (r != null) screenQuad = r.transform;
        }

        outline = GetComponent<InteractableOutline>();
    }

    public void Interact()
    {
        if (controller == null) controller = FindFirstObjectByType<ScreenZoomController>();
        if (controller == null)
        {
            Debug.LogWarning("No ScreenZoomController found on the player", this);
            return;
        }
        controller.ZoomTo(this);

        SetOutlineEnabled(false);
    }
    public void OnHoverExit()
    {
        SetOutlineEnabled(true);
    }
    public void SetOutlineEnabled(bool isEnabled)
    {
        if (outline != null)
        {
            outline.enabled = isEnabled;
        }
    }
}
