using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class SecurityScreen : MonoBehaviour
{
    public enum State { Empty, Far, Close }

    [SerializeField] Renderer screen;
    [SerializeField] Material emptyMat, farMat, closeMat;
    [Tooltip("Leave empty to use the child object named \"RecDot\" of this camera")]
    [SerializeField] GameObject recDot;
    [SerializeField] float blinkInterval = 0.5f;
    [Tooltip("Seconds of static when the image changes (needs the Custom/SurveillanceFeed shader). 0 = off")]
    [SerializeField, Min(0f)] float switchStaticDuration = 0.35f;

    [Header("Closeness Stage Events (Per Camera)")]
    public UnityEvent onDetectedFar;
    public UnityEvent onReachedClose;
    public UnityEvent onCleared;

    public State Current { get; private set; } = State.Empty;
    public bool IsFree => Current == State.Empty;

    SecurityCameraManager manager;
    Coroutine routine;

    void Awake()
    {
        if (recDot == null || !recDot.transform.IsChildOf(transform))
        {
            var child = FindChild(transform, "RecDot");
            if (child != null) recDot = child.gameObject;
            else Debug.LogWarning($"{name}: no RecDot child found", this);
        }
        SetState(State.Empty);
    }

    public void Activate(SecurityCameraManager owner)
    {
        manager = owner;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Run());
    }

    public void Clear()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        SetState(State.Empty);
        onCleared?.Invoke();
    }

    IEnumerator Run()
    {
        SetState(State.Far);
        onDetectedFar?.Invoke();
        yield return Hold(() => manager.CloseDelay);

        SetState(State.Close);
        onReachedClose?.Invoke();
        yield return Hold(() => manager.CloseDuration);

        routine = null;
        SetState(State.Empty);
        onCleared?.Invoke();
    }

    IEnumerator Hold(System.Func<float> duration)
    {
        float progress = 0f;
        while (progress < 1f)
        {
            progress += Time.deltaTime / Mathf.Max(0.01f, duration());
            BlinkDot();
            yield return null;
        }
    }

    void BlinkDot()
    {
        if (recDot != null)
            recDot.SetActive(Mathf.FloorToInt(Time.time / blinkInterval) % 2 == 0);
    }

    void SetState(State s)
    {
        bool changed = s != Current;
        Current = s;
        var mats = screen.sharedMaterials;
        if (mats.Length == 0) mats = new Material[1];
        mats[0] = s switch
        {
            State.Far => farMat,
            State.Close => closeMat,
            _ => emptyMat,
        };
        screen.sharedMaterials = mats;
        if (s == State.Empty && recDot != null) recDot.SetActive(false);
        if (changed && Application.isPlaying && isActiveAndEnabled && switchStaticDuration > 0f)
        {
            if (staticRoutine != null) StopCoroutine(staticRoutine);
            staticRoutine = StartCoroutine(SwitchStatic());
        }
    }

    // ---------- Static when the image changes ----------

    static readonly int StaticBurstId = Shader.PropertyToID("_StaticBurst");
    MaterialPropertyBlock staticBlock;
    Coroutine staticRoutine;

    IEnumerator SwitchStatic()
    {
        staticBlock ??= new MaterialPropertyBlock();
        for (float t = 0f; t < 1f; t += Time.deltaTime / switchStaticDuration)
        {
            SetStaticBurst(1f - t * t);   // full static, fading out
            yield return null;
        }
        SetStaticBurst(0f);
        staticRoutine = null;
    }

    void SetStaticBurst(float amount)
    {
        screen.GetPropertyBlock(staticBlock);
        staticBlock.SetFloat(StaticBurstId, amount);
        screen.SetPropertyBlock(staticBlock);
    }

    // ---------- Laser zap ----------

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    MeshRenderer flashQuad;
    Light flashLight;
    MaterialPropertyBlock flashBlock;
    Coroutine flashRoutine;

    public bool HasSilhouette => !IsFree;

    // Plays a white flash over this screen and removes the silhouette if
    // there is one. Returns true if a silhouette was removed.
    public bool Zap(Material flashMaterial, float duration, AnimationCurve alpha,
                    float lightIntensity, float lightRange)
    {
        bool hadSilhouette = HasSilhouette;
        if (hadSilhouette) Clear();   // hidden under the flash

        if (flashMaterial != null)
        {
            EnsureFlashObjects(flashMaterial, lightRange);
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(Flash(duration, alpha, lightIntensity));
        }
        return hadSilhouette;
    }

    void EnsureFlashObjects(Material flashMaterial, float lightRange)
    {
        if (flashQuad == null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "LaserFlash";
            Destroy(go.GetComponent<Collider>());   // must not block raycasts

            var src = screen.transform;
            go.transform.SetParent(src.parent, false);
            go.transform.localPosition = src.localPosition;
            go.transform.localRotation = src.localRotation;
            go.transform.localScale = src.localScale;
            // A quad's visible side faces -forward: push it toward the viewer
            // so it covers the image, the red dot and the label.
            go.transform.position -= src.forward * 0.01f;

            flashQuad = go.GetComponent<MeshRenderer>();
            flashQuad.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            flashQuad.receiveShadows = false;

            flashLight = go.AddComponent<Light>();
            flashLight.type = LightType.Point;
            flashLight.color = Color.white;
            flashLight.shadows = LightShadows.None;

            flashBlock = new MaterialPropertyBlock();
            go.SetActive(false);
        }
        flashQuad.sharedMaterial = flashMaterial;
        flashLight.range = lightRange;
    }

    IEnumerator Flash(float duration, AnimationCurve alpha, float lightIntensity)
    {
        var go = flashQuad.gameObject;
        go.SetActive(true);
        for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, duration))
        {
            float a = Mathf.Clamp01(alpha.Evaluate(t));
            flashBlock.SetColor(BaseColorId, new Color(1f, 1f, 1f, a));
            flashQuad.SetPropertyBlock(flashBlock);
            flashLight.intensity = lightIntensity * a;
            yield return null;
        }
        go.SetActive(false);
        flashRoutine = null;
    }

    static Transform FindChild(Transform parent, string childName)
    {
        foreach (Transform t in parent.GetComponentsInChildren<Transform>(true))
            if (t != parent && t.name.StartsWith(childName)) return t;
        return null;
    }

    [ContextMenu("Preview/Empty")] void PreviewEmpty() => PreviewState(State.Empty);
    [ContextMenu("Preview/Far")] void PreviewFar() => PreviewState(State.Far);
    [ContextMenu("Preview/Close")] void PreviewClose() => PreviewState(State.Close);

    void PreviewState(State s)
    {
        SetState(s);
        if (recDot != null) recDot.SetActive(s != State.Empty);
    }
}