using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RepairSpot : MonoBehaviour, IInteractable
{
    [Header("Nails")]
    [Tooltip("Leave empty to use every child whose name contains \"Nail\"")]
    [SerializeField] List<Transform> nails = new();

    [Header("View")]
    [Tooltip("Optional: an empty child placed where the camera should be, looking at the planks (blue arrow toward them). Leave empty to frame the object automatically.")]
    [SerializeField] Transform viewPoint;
    [Tooltip("On: zoom so the outermost nails sit near the screen edges. Off: fit the whole object.")]
    [SerializeField] bool frameNails = true;
    [Tooltip("Space kept between the outermost nails and the screen edge (meters)")]
    [SerializeField, Min(0f)] float nailMargin = 0.15f;
    [Tooltip("Multiplier on the framed size: 1 = tight, higher = further away")]
    [SerializeField, Min(0.5f)] float viewPadding = 1.0f;

    [Header("Game")]
    [Tooltip("Number of nails to hit in one repair")]
    [SerializeField, Min(1)] int minRounds = 3;
    [Tooltip("Seconds for the ring to close on the first nail")]
    [SerializeField, Min(1)] int maxRounds = 8;
    [Tooltip("Seconds for the ring to close on the first nail")]

    [SerializeField] float shrinkTimeStart = 1.6f;
    [Tooltip("Seconds for the ring to close on the last nail (gets faster)")]
    [SerializeField] float shrinkTimeEnd = 0.8f;
    [Tooltip("Pause between two nails (seconds)")]
    [SerializeField] float pauseBetween = 0.35f;
    [Tooltip("If on, clicking away from the nail counts as a miss")]
    [SerializeField] bool missOnWrongClick = true;

    [Header("Ring (pixels)")]
    [SerializeField] Texture ringTexture;
    [SerializeField] float ringStartRadius = 140f;
    [SerializeField] float ringEndRadius = 14f;
    [Tooltip("How close to the nail (pixels) a click must be")]
    [SerializeField] float hitRadius = 40f;
    [SerializeField] Color ringColor = Color.white;
    [SerializeField] Color hitColor = new Color(0.3f, 1f, 0.4f);
    [SerializeField] Color missColor = new Color(1f, 0.3f, 0.3f);

    [Header("Ring glow")]
    [Tooltip("Soft halo drawn behind the ring (RepairRingGlow.png). Leave empty for no glow.")]
    [SerializeField] Texture glowTexture;
    [Tooltip("Glow image size relative to the ring (1.52 matches RepairRingGlow.png)")]
    [SerializeField] float glowScale = 1.52f;
    [SerializeField, Range(0f, 1f)] float glowIntensity = 0.85f;
    [Tooltip("Pulses per second (0 = steady)")]
    [SerializeField] float glowPulseSpeed = 3f;
    [SerializeField, Range(0f, 1f)] float glowPulseAmount = 0.35f;

    [Header("Score")]
    [SerializeField] int pointsPerHit = 100;
    [Tooltip("Extra points for clicking early (full bonus at the start, 0 when the ring closes)")]
    [SerializeField] int maxSpeedBonus = 50;
    [Tooltip("Optional text that shows the score during the repair")]
    [SerializeField] TMP_Text scoreText;

    [Header("Events (sounds, game logic...)")]
    public UnityEvent onRepairStarted;
    public UnityEvent onNailHit;
    public UnityEvent onNailMissed;
    public UnityEvent<int> onRepairFinished;   // passes the score

    public bool IsRunning { get; private set; }
    public int LastScore { get; private set; }

    ScreenZoomController zoom;
    RawImage ring;
    RawImage glow;
    RectTransform ringRoot;
    Color currentRingColor;
    Canvas canvas;
    int lastNail = -1;

    public InteractableOutline outline;

    void Awake()
    {
        if (nails.Count == 0)
            nails = GetComponentsInChildren<Transform>(true)
                .Where(t => t != transform && t.name.ToLower().Contains("nail")).ToList();
        if (nails.Count == 0) Debug.LogWarning($"{name}: no nails found", this);

        outline = GetComponent<InteractableOutline>();
    }

    public void Interact()
    {
        if (IsRunning || nails.Count == 0) return;
        if (zoom == null) zoom = FindFirstObjectByType<ScreenZoomController>();
        if (zoom == null) { Debug.LogWarning("No ScreenZoomController on the player", this); return; }

        GetViewPose(out var pos, out var rot);
        if (zoom.ZoomToPose(pos, rot, exitOnClick: false, onArrived: () => StartCoroutine(Play())))
        {
            IsRunning = true;
            SetOutlineEnabled(false);
        } 
    }

    void GetViewPose(out Vector3 pos, out Quaternion rot)
    {
        if (viewPoint != null)
        {
            pos = viewPoint.position;
            rot = viewPoint.rotation;
            return;
        }

        // Bounds of every renderer, measured in this object's own axes so the
        // door's width / height / thickness are right even if it's rotated.
        var rends = GetComponentsInChildren<Renderer>();
        Vector3 min = Vector3.one * float.MaxValue, max = Vector3.one * float.MinValue;
        foreach (var r in rends)
        {
            var wb = r.bounds;
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3(
                    (i & 1) == 0 ? wb.min.x : wb.max.x,
                    (i & 2) == 0 ? wb.min.y : wb.max.y,
                    (i & 4) == 0 ? wb.min.z : wb.max.z);
                var local = transform.InverseTransformPoint(corner);
                min = Vector3.Min(min, local);
                max = Vector3.Max(max, local);
            }
        }
        if (rends.Length == 0) { min = -Vector3.one * 0.5f; max = Vector3.one * 0.5f; }

        Vector3 s = transform.lossyScale;
        Vector3 size = Vector3.Scale(max - min, new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z)));
        Vector3 center = transform.TransformPoint((min + max) * 0.5f);

        // The thinnest horizontal side is the door's front/back.
        bool thinX = size.x < size.z;
        Vector3 normal = thinX ? transform.right : transform.forward;
        float width = thinX ? size.z : size.x;
        float thickness = thinX ? size.x : size.z;
        Vector3 up = transform.up;

        // Use the side the player is standing on.
        var cam = zoom.PlayerCamera.transform;
        if (Vector3.Dot(normal, cam.position - center) < 0f) normal = -normal;

        float height = size.y;
        Vector3 target = center;

        if (frameNails && nails.Count > 0)
        {
            // Frame the area covered by the nails (plus a margin) instead of
            // the whole object; keep the depth of the object's front face.
            Vector3 nMin = Vector3.one * float.MaxValue, nMax = Vector3.one * float.MinValue;
            foreach (var n in nails)
            {
                if (n == null) continue;
                var local = transform.InverseTransformPoint(n.position);
                nMin = Vector3.Min(nMin, local);
                nMax = Vector3.Max(nMax, local);
            }
            Vector3 nSize = Vector3.Scale(nMax - nMin, new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z)));
            width = (thinX ? nSize.z : nSize.x) + nailMargin * 2f;
            height = nSize.y + nailMargin * 2f;

            Vector3 nCenter = transform.TransformPoint((nMin + nMax) * 0.5f);
            // Move the object's center onto the nails' center, in the door plane only.
            Vector3 offset = nCenter - center;
            target = center + offset - normal * Vector3.Dot(offset, normal);
        }

        float dist = zoom.FitDistance(width * viewPadding, height * viewPadding) + thickness * 0.5f;

        pos = target + normal * dist;
        rot = Quaternion.LookRotation(-normal, up);
    }

    IEnumerator Play()
    {
        var prevLock = Cursor.lockState;
        var prevVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        EnsureRing();
        int score = 0;
        UpdateScore(score);
        onRepairStarted.Invoke();
        var cam = zoom.PlayerCamera;

        int rounds = Random.Range(minRounds, maxRounds + 1);
        for (int i = 0; i < rounds; i++)
        {
            Transform nail = PickNail();
            float shrinkTime = Mathf.Lerp(shrinkTimeStart, shrinkTimeEnd,
                                          rounds > 1 ? i / (float)(rounds - 1) : 0f);
            bool hit = false, decided = false;
            float t = 0f;

            SetRingColor(ringColor);
            ringRoot.gameObject.SetActive(true);

            while (!decided)
            {
                if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    ringRoot.gameObject.SetActive(false);
                    yield return Finish(score, prevLock, prevVisible);
                    yield break;
                }

                t += Time.deltaTime / Mathf.Max(0.05f, shrinkTime);
                Vector2 nailScreen = cam.WorldToScreenPoint(nail.position);
                PlaceRing(nailScreen, Mathf.Lerp(ringStartRadius, ringEndRadius, t));
                UpdateGlow();

                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    float d = Vector2.Distance(Mouse.current.position.ReadValue(), nailScreen);
                    if (d <= hitRadius)
                    {
                        hit = true; decided = true;
                        score += pointsPerHit + Mathf.RoundToInt(maxSpeedBonus * (1f - Mathf.Clamp01(t)));
                    }
                    else if (missOnWrongClick) decided = true;
                }
                if (t >= 1f) decided = true;   // ring closed
                yield return null;
            }

            SetRingColor(hit ? hitColor : missColor);
            UpdateGlow(flash: true);
            UpdateScore(score);
            if (hit) onNailHit.Invoke(); else onNailMissed.Invoke();
            yield return new WaitForSeconds(pauseBetween);
            ringRoot.gameObject.SetActive(false);
        }

        yield return Finish(score, prevLock, prevVisible);
    }

    IEnumerator Finish(int score, CursorLockMode prevLock, bool prevVisible)
    {
        LastScore = score;
        yield return new WaitForSeconds(0.4f);
        Cursor.lockState = prevLock;
        Cursor.visible = prevVisible;
        zoom.ZoomOut(() =>
        {
            IsRunning = false;
            onRepairFinished.Invoke(score);
        });
        SetOutlineEnabled(true);
    }

    Transform PickNail()
    {
        int idx = Random.Range(0, nails.Count);
        if (nails.Count > 1 && idx == lastNail) idx = (idx + 1 + Random.Range(0, nails.Count - 1)) % nails.Count;
        lastNail = idx;
        return nails[idx];
    }

    void UpdateScore(int score)
    {
        if (scoreText != null) scoreText.text = $"Score: {score}";
    }

    void EnsureRing()
    {
        if (ring != null) return;
        var cgo = new GameObject("RepairRingCanvas");
        canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        // RingRoot is positioned/sized each frame; Glow (behind) and Ring
        // (in front) stretch to fill it.
        var root = new GameObject("RingRoot", typeof(RectTransform));
        root.transform.SetParent(cgo.transform, false);
        ringRoot = (RectTransform)root.transform;
        ringRoot.anchorMin = ringRoot.anchorMax = Vector2.zero;   // bottom-left = screen pixels
        ringRoot.pivot = new Vector2(0.5f, 0.5f);

        if (glowTexture != null)
        {
            glow = CreateImage("Glow", glowTexture);
            glow.rectTransform.localScale = Vector3.one * glowScale;
        }
        ring = CreateImage("Ring", ringTexture);
        root.SetActive(false);
    }

    RawImage CreateImage(string objName, Texture tex)
    {
        var go = new GameObject(objName, typeof(RectTransform));
        go.transform.SetParent(ringRoot, false);
        var img = go.AddComponent<RawImage>();
        img.texture = tex;
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return img;
    }

    void SetRingColor(Color c)
    {
        currentRingColor = c;
        ring.color = c;
    }

    void UpdateGlow(bool flash = false)
    {
        if (glow == null) return;
        float pulse = glowPulseSpeed > 0f
            ? 1f - glowPulseAmount * (0.5f + 0.5f * Mathf.Sin(Time.time * glowPulseSpeed * Mathf.PI * 2f))
            : 1f;
        var c = currentRingColor;
        c.a = Mathf.Clamp01(glowIntensity * (flash ? 1.3f : pulse));
        glow.color = c;
    }

    void PlaceRing(Vector2 screenPos, float radius)
    {
        float scale = canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
        ringRoot.anchoredPosition = screenPos / scale;
        ringRoot.sizeDelta = Vector2.one * (radius * 2f / scale);
    }

    public void SetOutlineEnabled(bool isEnabled)
    {
        if (outline != null)
        {
            outline.enabled = isEnabled;
        }
    }
}
