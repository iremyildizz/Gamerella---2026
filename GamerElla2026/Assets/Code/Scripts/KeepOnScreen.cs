using UnityEngine;
using UnityEngine.UI;

// Put on a UI element (e.g. TacticalPanel, Warning). Measures everything it
// actually draws, including children that stick out of its rectangle, and
// nudges it so all of it stays inside the screen with a margin.
// Works for Screen Space - Overlay canvases.
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class KeepOnScreen : MonoBehaviour
{
    [Tooltip("Space to keep from the screen edges (pixels at 1920x1080)")]
    [SerializeField] float margin = 16f;

    RectTransform rt;
    Canvas canvas;
    Vector2 basePos;
    bool hasBase;
    readonly Vector3[] corners = new Vector3[4];

    void OnEnable()
    {
        rt = (RectTransform)transform;
        canvas = GetComponentInParent<Canvas>();
        basePos = rt.anchoredPosition;
        hasBase = true;
    }

    void OnDisable()
    {
        // Leave the element where it was designed in the editor.
        if (hasBase && rt != null) rt.anchoredPosition = basePos;
    }

    void LateUpdate()
    {
        if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay) return;

        // In edit mode, treat manual moves as the new design position.
        if (!Application.isPlaying) { basePos = rt.anchoredPosition; return; }

        rt.anchoredPosition = basePos;

        // Bounds (in screen pixels) of every visible graphic under this element.
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        bool any = false;
        foreach (var g in GetComponentsInChildren<Graphic>())
        {
            if (!g.enabled || g.color.a <= 0.01f) continue;
            g.rectTransform.GetWorldCorners(corners);
            foreach (var c in corners)
            {
                minX = Mathf.Min(minX, c.x); maxX = Mathf.Max(maxX, c.x);
                minY = Mathf.Min(minY, c.y); maxY = Mathf.Max(maxY, c.y);
            }
            any = true;
        }
        if (!any) return;

        float m = margin * canvas.scaleFactor;
        float dx = 0f, dy = 0f;
        if (maxX - minX > Screen.width - 2 * m) dx = (Screen.width * 0.5f) - (minX + maxX) * 0.5f; // too wide: center
        else if (minX < m) dx = m - minX;
        else if (maxX > Screen.width - m) dx = Screen.width - m - maxX;

        if (maxY - minY > Screen.height - 2 * m) dy = (Screen.height * 0.5f) - (minY + maxY) * 0.5f;
        else if (minY < m) dy = m - minY;
        else if (maxY > Screen.height - m) dy = Screen.height - m - maxY;

        if (dx != 0f || dy != 0f)
            rt.anchoredPosition = basePos + new Vector2(dx, dy) / canvas.scaleFactor;
    }
}
