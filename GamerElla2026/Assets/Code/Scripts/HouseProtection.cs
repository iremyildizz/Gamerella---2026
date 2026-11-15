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
    bool hasBreached = false;

    public float CurrentProtection => current;
    public bool IsBreached => current <= 0f;
    public float Percent => maxProtection > 0f ? current / maxProtection : 0f;

    void Awake()
    {
        Instance = this;
        current = maxProtection * startProtection / 100f;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Update()
    {
        if (hasBreached || drainPercentPerMinute <= 0f) return;

        if (current > 0f)
        {
            current = Mathf.Max(0f, current - maxProtection * (drainPercentPerMinute / 100f) / 60f * Time.deltaTime);
            CheckBreach();
        }
    }

    // accuracy: 0..1 (nails hit / nails shown)
    public void AddFromRepair(float accuracy) => Add(perfectRepairGain * Mathf.Clamp01(accuracy));

    public void Add(float amount)
    {
        if (hasBreached) return;
        current = Mathf.Clamp(current + amount, 0f, maxProtection);
        CheckBreach();
    }

    public void Drain(float amount) => Add(-amount);

    private void CheckBreach()
    {
        if (!hasBreached && current <= 0f)
        {
            hasBreached = true;
            if (GameOverManager.Instance != null)
                GameOverManager.Instance.TriggerGameOver(GameOverManager.LoseReason.ZombiesBreached);
        }
    }
}