using UnityEngine;
using UnityEngine.Events;

// One per scene. All SecuritySwitches fire through this single "invisible
// laser", so they share one cooldown. Also holds the flash look.
public class LaserSystem : MonoBehaviour
{
    [Header("Cooldown (shared by every switch)")]
    [Tooltip("Seconds before any switch can be used again")]
    public float cooldown = 8f;
    [Tooltip("If off, firing at an empty screen does nothing and keeps the cooldown ready")]
    public bool fireOnEmptyScreen = true;

    [Header("Flash")]
    [Tooltip("URP/Unlit material, Surface Type Transparent, white")]
    [SerializeField] Material flashMaterial;
    [SerializeField] float flashDuration = 0.6f;
    [Tooltip("Flash opacity over time (0 to 1 on both axes)")]
    [SerializeField] AnimationCurve flashAlpha = new AnimationCurve(
        new Keyframe(0f, 1f), new Keyframe(0.12f, 1f), new Keyframe(0.22f, 0.25f),
        new Keyframe(0.32f, 0.9f), new Keyframe(1f, 0f));
    [Tooltip("Brightness of the light the flash casts into the room (0 = none)")]
    [SerializeField] float flashLightIntensity = 3f;
    [SerializeField] float flashLightRange = 4f;

    [Header("Events (sounds, UI...)")]
    public UnityEvent onFired;           // laser shot (hit or miss)
    public UnityEvent onSilhouetteHit;   // a silhouette was removed
    public UnityEvent onDenied;          // tried to fire during cooldown
    public UnityEvent onReady;           // cooldown finished

    float readyTime;
    bool waitingForReady;

    public bool IsReady => Time.time >= readyTime;
    public float CooldownRemaining => Mathf.Max(0f, readyTime - Time.time);
    public float CooldownProgress => cooldown <= 0f ? 1f : 1f - CooldownRemaining / cooldown;

    void Update()
    {
        if (waitingForReady && IsReady)
        {
            waitingForReady = false;
            onReady.Invoke();
        }
    }

    // Returns true if the laser actually fired.
    public bool Fire(SecurityScreen target)
    {
        if (target == null) return false;
        if (!IsReady) { onDenied.Invoke(); return false; }
        if (!fireOnEmptyScreen && !target.HasSilhouette) return false;

        readyTime = Time.time + cooldown;
        waitingForReady = true;

        bool hit = target.Zap(flashMaterial, flashDuration, flashAlpha,
                              flashLightIntensity, flashLightRange);
        onFired.Invoke();
        if (hit) onSilhouetteHit.Invoke();
        return true;
    }
}
