using UnityEngine;
using UnityEngine.Events;

// Put on a bed (with a collider on the Interactable layer).
// Clicking it puts the player to sleep. The fade, sleep levels and overnight
// costs are all set on the Player's PlayerSleep component.
public class Bed : MonoBehaviour, IInteractable
{
    [Header("Rules")]
    [Tooltip("Can only sleep when sleep is at or below this percent (100 = always)")]
    [SerializeField, Range(0f, 100f)] float sleepAllowedBelowPercent = 100f;

    [Header("Events (sounds, messages...)")]
    public UnityEvent onWentToBed;
    public UnityEvent onNotTired;     // tried to sleep while too rested

    PlayerSleep sleep;

    void Awake()
    {
        sleep = FindFirstObjectByType<PlayerSleep>();
        if (sleep == null) Debug.LogWarning("Bed: no PlayerSleep on the player", this);
    }

    public void Interact()
    {
        if (sleep == null || sleep.IsSleeping) return;
        if (sleep.Percent * 100f > sleepAllowedBelowPercent)
        {
            onNotTired.Invoke();
            return;
        }
        if (sleep.GoToSleep()) onWentToBed.Invoke();
    }
}
