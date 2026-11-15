using Project.UI;
using UnityEngine;

// Put on the Needs Panel root (next to TacticalHUD).
// Pushes Hunger / Sleep / Protection into the tactical panel's bars and
// shows the warning popup when a need gets low.
public class NeedsDisplay : MonoBehaviour
{
    [Header("Tactical panel bars")]
    [Tooltip("The ProfileScrollViewSpawner inside TacticalPanel")]
    [SerializeField] ProfileScrollViewSpawner spawner;
    [Tooltip("Index of each need's profile (row) in the spawner's Profiles list. Each profile has one progress bar.")]
    [SerializeField] int hungerProfile = 0, sleepProfile = 1, protectionProfile = 2;
    [Tooltip("Seconds between bar refreshes")]
    [SerializeField] float refreshInterval = 0.25f;

    [Header("Warnings")]
    [SerializeField] TacticalHUD hud;
    [Tooltip("Warn when a need drops below this percent")]
    [SerializeField, Range(0f, 100f)] float warnBelowPercent = 30f;
    [SerializeField] string hungerMessage = "Food running low";
    [SerializeField] string sleepMessage = "You need to sleep";
    [SerializeField] string protectionMessage = "The house needs repairs";

    PlayerHunger hunger;
    PlayerSleep sleep;
    float timer;
    bool warnedHunger, warnedSleep, warnedProtection;

    void Awake()
    {
        hunger = FindFirstObjectByType<PlayerHunger>();
        sleep = FindFirstObjectByType<PlayerSleep>();
        if (hud == null) hud = GetComponent<TacticalHUD>();
        if (spawner == null) spawner = GetComponentInChildren<ProfileScrollViewSpawner>(true);
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < refreshInterval) return;
        timer = 0f;

        float h = hunger != null ? hunger.Percent : 1f;
        float s = sleep != null ? sleep.Percent : 1f;
        float p = HouseProtection.Instance != null ? HouseProtection.Instance.Percent : 1f;

        if (spawner != null)
        {
            spawner.UpdateProgressValue(hungerProfile, 0, h * 100f);
            spawner.UpdateProgressValue(sleepProfile, 0, s * 100f);
            spawner.UpdateProgressValue(protectionProfile, 0, p * 100f);
        }

        CheckWarning(h, ref warnedHunger, hungerMessage);
        CheckWarning(s, ref warnedSleep, sleepMessage);
        CheckWarning(p, ref warnedProtection, protectionMessage);
    }

    // Warn once when crossing below the threshold; re-arm after recovering a bit.
    void CheckWarning(float percent, ref bool warned, string message)
    {
        float limit = warnBelowPercent / 100f;
        if (!warned && percent < limit)
        {
            warned = true;
            if (hud != null) hud.ShowWarning(message);
        }
        else if (warned && percent > limit + 0.05f)
        {
            warned = false;
        }
    }
}
