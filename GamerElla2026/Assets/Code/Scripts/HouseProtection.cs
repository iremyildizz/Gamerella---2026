using UnityEngine;

// One per scene. How well the house is boarded up (0-100). Repairs raise it,
// time and sleeping lower it.
public class HouseProtection : MonoBehaviour
{
    public static HouseProtection Instance { get; private set; }

    public float maxProtection = 100f;
    [Range(0f, 100f)] public float startProtection = 100f;
    [Tooltip("Percent lost per minute while awake (0 = only sleeping lowers it)")]
    public float drainPercentPerMinute = 3f;
    [Tooltip("Protection gained by a perfect repair (all nails hit). Scaled by accuracy.")]
    public float perfectRepairGain = 30f;

    float current;

    public float CurrentProtection => current;
    public float Percent => maxProtection > 0f ? current / maxProtection : 0f;

    void Awake()
    {
        Instance = this;
        current = maxProtection * startProtection / 100f;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Update()
    {
        if (drainPercentPerMinute <= 0f || current <= 0f) return;
        current = Mathf.Max(0f, current - maxProtection * (drainPercentPerMinute / 100f) / 60f * Time.deltaTime);
    }

    // accuracy: 0..1 (nails hit / nails shown)
    public void AddFromRepair(float accuracy) => Add(perfectRepairGain * Mathf.Clamp01(accuracy));

    public void Add(float amount) => current = Mathf.Clamp(current + amount, 0f, maxProtection);
    public void Drain(float amount) => Add(-amount);
}
