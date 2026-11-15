using UnityEngine;

// Put on the Player. Sleep goes down smoothly over time; sleeping in a Bed
// fills it back up.
public class PlayerSleep : MonoBehaviour
{
    public float maxSleep = 100f;
    [Tooltip("Percent of the bar lost per minute (10 = 10% per minute, drained smoothly)")]
    public float drainPercentPerMinute = 10f;

    float current;

    public float CurrentSleep => current;
    public float Percent => maxSleep > 0f ? current / maxSleep : 0f;
    public bool IsExhausted => current <= 0f;

    void Awake() => current = maxSleep;

    void Update()
    {
        if (current <= 0f) return;
        float perSecond = maxSleep * (drainPercentPerMinute / 100f) / 60f;
        current = Mathf.Max(0f, current - perSecond * Time.deltaTime);
    }

    public void Rest() => current = maxSleep;

    public void Drain(float amount) => current = Mathf.Clamp(current - amount, 0f, maxSleep);
}
