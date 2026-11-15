using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class SecurityScreen : MonoBehaviour
{
    // Lurking = the zombie left the frame but is still out there (not killed yet).
    public enum State { Empty, Far, Close, Lurking }

    [SerializeField] Renderer screen;
    [SerializeField] Material emptyMat, farMat, closeMat;
    [Tooltip("Leave empty to use the child object named \"RecDot\" of this camera")]
    [SerializeField] GameObject recDot;
    [SerializeField] float blinkInterval = 0.5f;
    [Tooltip("Seconds of static when the image changes (needs the Custom/SurveillanceFeed shader). 0 = off")]
    [SerializeField, Min(0f)] float switchStaticDuration = 0.35f;

    [Header("Battery")]
    [Tooltip("Seconds a full battery lasts")]
    [SerializeField, Min(1f)] float batteryLifeSeconds = 180f;
    [Tooltip("Random starting charge in percent (min, max), so the cameras don't all die at the same time")]
    [SerializeField] Vector2 startChargeRange = new Vector2(50f, 100f);
    [Tooltip("The low-battery icon blinks on the screen at or below this percent")]
    [SerializeField, Range(0f, 100f)] float lowBatteryPercent = 10f;
    [Tooltip("At or below this percent the image is lost: static only")]
    [SerializeField, Range(0f, 100f)] float feedLostPercent = 0f;
    public UnityEvent onBatteryLow;
    public UnityEvent onFeedLost;
    public UnityEvent onBatteryReplaced;

    [Header("Lurking (zombie off-camera, not killed)")]
    [Tooltip("Seconds the zombie lurks after leaving the frame before it breaks in")]
    [SerializeField, Min(0f)] float lurkDuration = 60f;
    [Tooltip("Random wait between two static flashes while lurking (min, max seconds)")]
    [SerializeField] Vector2 lurkStaticInterval = new Vector2(2f, 6f);
    [Tooltip("Length of each static flash while lurking (seconds)")]
    [SerializeField, Min(0.05f)] float lurkStaticDuration = 0.25f;
    [Tooltip("House protection lost when a lurking zombie breaks in (protection is 0-100)")]
    [SerializeField, Min(0f)] float breachProtectionDamage = 25f;
    public UnityEvent onLurking;     // zombie left the frame but is still alive
    public UnityEvent onBreached;    // lurked too long: damaged the house and despawned

    [Header("Closeness Stage Events (Per Camera)")]
    public UnityEvent onDetectedFar;
    public UnityEvent onReachedClose;
    public UnityEvent onZombieDied;
    public UnityEvent onCleared;

    public State Current { get; private set; } = State.Empty;
    public bool IsFree => Current == State.Empty;

    public float BatteryPercent { get; private set; } = 100f;
    public bool IsBatteryLow => BatteryPercent <= lowBatteryPercent;
    public bool IsFeedLost => BatteryPercent <= feedLostPercent;

    SecurityCameraManager manager;
    Coroutine routine;

    void Awake()
    {
        float lo = Mathf.Min(startChargeRange.x, startChargeRange.y);
        float hi = Mathf.Max(startChargeRange.x, startChargeRange.y);
        BatteryPercent = Mathf.Clamp(Random.Range(lo, hi), 0f, 100f);
        wasLow = IsBatteryLow;
        wasLost = IsFeedLost;
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

        // Out of frame but still alive: rec dot keeps blinking and the feed
        // glitches now and then until it's zapped or breaks in.
        SetState(State.Lurking);
        onLurking?.Invoke();
        float lo = Mathf.Max(0.1f, Mathf.Min(lurkStaticInterval.x, lurkStaticInterval.y));
        float hi = Mathf.Max(lo, Mathf.Max(lurkStaticInterval.x, lurkStaticInterval.y));
        float nextFlash = Random.Range(lo, hi);
        for (float t = 0f; t < lurkDuration; t += Time.deltaTime)
        {
            BlinkDot();
            nextFlash -= Time.deltaTime;
            if (nextFlash <= 0f)
            {
                PlayStatic(lurkStaticDuration);
                nextFlash = Random.Range(lo, hi);
            }
            yield return null;
        }

        if (HouseProtection.Instance != null) HouseProtection.Instance.Drain(breachProtectionDamage);
        onBreached?.Invoke();

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
            recDot.SetActive(!IsFeedLost && Mathf.FloorToInt(Time.time / blinkInterval) % 2 == 0);
    }

    bool wasLow, wasLost;

    void Update()
    {
        if (BatteryPercent > 0f)
            BatteryPercent = Mathf.Max(0f, BatteryPercent - 100f / batteryLifeSeconds * Time.deltaTime);

        bool low = IsBatteryLow, lost = IsFeedLost;
        if (low && !wasLow) onBatteryLow?.Invoke();
        if (lost && !wasLost)
        {
            onFeedLost?.Invoke();
            if (recDot != null) recDot.SetActive(false);
        }
        wasLow = low;
        wasLost = lost;

        ApplyScreenProps();
    }

    public void ReplaceBattery()
    {
        BatteryPercent = 100f;
        wasLow = wasLost = false;
        onBatteryReplaced?.Invoke();
        PlayStatic(switchStaticDuration);   // short static as the feed comes back
        ApplyScreenProps();
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
        if (changed) PlayStatic(switchStaticDuration);
    }

    void PlayStatic(float duration)
    {
        if (!Application.isPlaying || !isActiveAndEnabled || duration <= 0f) return;
        if (staticRoutine != null) StopCoroutine(staticRoutine);
        staticRoutine = StartCoroutine(SwitchStatic(duration));
    }


    static readonly int StaticBurstId = Shader.PropertyToID("_StaticBurst");
    static readonly int LowBatteryId = Shader.PropertyToID("_LowBattery");
    MaterialPropertyBlock staticBlock;
    Coroutine staticRoutine;
    float switchBurst;
    float appliedBurst = -1f, appliedLow = -1f;

    IEnumerator SwitchStatic(float duration)
    {
        staticBlock ??= new MaterialPropertyBlock();
        for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
        {
            SetStaticBurst(1f - t * t);   // full static, fading out
            yield return null;
        }
        SetStaticBurst(0f);
        staticRoutine = null;
    }

    void SetStaticBurst(float amount)
    {
        switchBurst = amount;
        ApplyScreenProps();
    }

    void ApplyScreenProps()
    {
        if (screen == null) return;
        float burst = IsFeedLost ? 1f : switchBurst;
        float low = IsBatteryLow ? 1f : 0f;
        if (Mathf.Approximately(burst, appliedBurst) && low == appliedLow) return;
        appliedBurst = burst;
        appliedLow = low;

        staticBlock ??= new MaterialPropertyBlock();
        screen.GetPropertyBlock(staticBlock);
        staticBlock.SetFloat(StaticBurstId, burst);
        staticBlock.SetFloat(LowBatteryId, low);
        screen.SetPropertyBlock(staticBlock);
    }

    [ContextMenu("Battery/Set To Low")] void DebugBatteryLow() => BatteryPercent = Mathf.Min(BatteryPercent, lowBatteryPercent);
    [ContextMenu("Battery/Set To Dead")] void DebugBatteryDead() => BatteryPercent = 0f;
    [ContextMenu("Battery/Replace")] void DebugBatteryReplace() => ReplaceBattery();


    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    MeshRenderer flashQuad;
    Light flashLight;
    MaterialPropertyBlock flashBlock;
    Coroutine flashRoutine;

    public bool HasSilhouette => !IsFree;

    public bool Zap(Material flashMaterial, float duration, AnimationCurve alpha,
                    float lightIntensity, float lightRange)
    {
        bool hadSilhouette = HasSilhouette;
        if (hadSilhouette)
        {
            Clear();              // Resets screen & stops close/far loops
            onZombieDied?.Invoke(); // Plays zombie dying / laser scream sound
        }

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