using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Put on the Player. Sleep goes down smoothly over time.
// - Sleeping (in a Bed) fades to black and refills sleep. The more tired the
//   player is, the more hunger and house protection are lost overnight.
// - At 0% the player passes out wherever they are.
public class PlayerSleep : MonoBehaviour
{
    [System.Serializable]
    public class SleepLevel
    {
        public string name = "Tired";
        [Tooltip("This level applies when sleep is at or above this percent")]
        [Range(0f, 100f)] public float fromPercent = 50f;
        [Tooltip("Hunger lost overnight when going to sleep at this level")]
        public float hungerCost = 20f;
        [Tooltip("House protection lost overnight when going to sleep at this level")]
        public float protectionCost = 20f;
    }

    [Header("Drain")]
    public float maxSleep = 100f;
    [Tooltip("Percent of the bar lost per minute (10 = 10% per minute, drained smoothly)")]
    public float drainPercentPerMinute = 10f;

    [Header("Sleep levels (highest first). Costs apply when you go to sleep.")]
    public List<SleepLevel> levels = new()
    {
        new SleepLevel { name = "Rested",      fromPercent = 90, hungerCost = 5,  protectionCost = 5  },
        new SleepLevel { name = "Fine",        fromPercent = 70, hungerCost = 15, protectionCost = 10 },
        new SleepLevel { name = "Tired",       fromPercent = 50, hungerCost = 25, protectionCost = 20 },
        new SleepLevel { name = "Very tired",  fromPercent = 25, hungerCost = 35, protectionCost = 30 },
        new SleepLevel { name = "Exhausted",   fromPercent = 0,  hungerCost = 50, protectionCost = 45 },
    };

    [Header("Passing out (sleep reaches 0%)")]
    [Tooltip("Costs of the lowest level are multiplied by this when passing out")]
    public float passOutCostMultiplier = 1.5f;
    [Tooltip("Seconds the screen takes to go black when passing out (faster than going to bed)")]
    public float passOutFadeTime = 0.6f;

    [Header("Fade (seconds)")]
    public float fadeOutTime = 1.5f;
    public float blackTime = 1.5f;
    public float fadeInTime = 1.2f;

    [Header("Events (sounds, messages...)")]
    public UnityEvent onFellAsleep;    // screen fully black
    public UnityEvent onPassedOut;     // fell asleep because sleep hit 0%
    public UnityEvent onWokeUp;

    float current;
    PlayerHunger hunger;
    PlayerCharacter3D movement;
    PlayerInteractor interactor;
    ScreenZoomController zoom;
    Image fade;

    public float CurrentSleep => current;
    public float Percent => maxSleep > 0f ? current / maxSleep : 0f;
    public bool IsExhausted => current <= 0f;
    public bool IsSleeping { get; private set; }
    public SleepLevel CurrentLevel => GetLevel(Percent * 100f);

    // What the last night cost (for "you slept..." messages).
    public float LastHungerCost { get; private set; }
    public float LastProtectionCost { get; private set; }
    public bool LastWasPassOut { get; private set; }

    void Awake()
    {
        current = maxSleep;
        hunger = GetComponent<PlayerHunger>();
        if (hunger == null) hunger = FindFirstObjectByType<PlayerHunger>();
        movement = GetComponent<PlayerCharacter3D>();
        interactor = GetComponent<PlayerInteractor>();
        zoom = GetComponent<ScreenZoomController>();
    }

    void Update()
    {
        if (IsSleeping) return;

        if (current > 0f)
        {
            float perSecond = maxSleep * (drainPercentPerMinute / 100f) / 60f;
            current = Mathf.Max(0f, current - perSecond * Time.deltaTime);
        }

        // Pass out wherever we are (wait if a zoom/minigame is running).
        if (current <= 0f && (zoom == null || !zoom.IsZoomed))
            StartCoroutine(SleepRoutine(passedOut: true));
    }

    // Called by a Bed. Returns false if already asleep.
    public bool GoToSleep()
    {
        if (IsSleeping) return false;
        StartCoroutine(SleepRoutine(passedOut: false));
        return true;
    }

    public void Rest() => current = maxSleep;

    public void Drain(float amount) => current = Mathf.Clamp(current - amount, 0f, maxSleep);

    public SleepLevel GetLevel(float percent)
    {
        SleepLevel best = null;
        foreach (var l in levels)
            if (percent >= l.fromPercent && (best == null || l.fromPercent > best.fromPercent))
                best = l;
        // Fallback: the lowest level if nothing matched.
        if (best == null)
            foreach (var l in levels)
                if (best == null || l.fromPercent < best.fromPercent) best = l;
        return best;
    }

    IEnumerator SleepRoutine(bool passedOut)
    {
        IsSleeping = true;
        LastWasPassOut = passedOut;
        SetPlayerControl(false);
        EnsureFade();

        // Costs are decided by how tired the player is when lying down.
        var level = GetLevel(Percent * 100f);
        float mult = passedOut ? passOutCostMultiplier : 1f;
        LastHungerCost = (level != null ? level.hungerCost : 0f) * mult;
        LastProtectionCost = (level != null ? level.protectionCost : 0f) * mult;

        yield return Fade(0f, 1f, passedOut ? passOutFadeTime : fadeOutTime);
        if (passedOut) onPassedOut.Invoke();
        onFellAsleep.Invoke();

        Rest();
        if (hunger != null) hunger.Drain(LastHungerCost);
        if (HouseProtection.Instance != null) HouseProtection.Instance.Drain(LastProtectionCost);

        yield return new WaitForSeconds(blackTime);
        yield return Fade(1f, 0f, fadeInTime);

        fade.gameObject.SetActive(false);
        SetPlayerControl(true);
        IsSleeping = false;
        onWokeUp.Invoke();
    }

    IEnumerator Fade(float from, float to, float time)
    {
        fade.gameObject.SetActive(true);
        for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, time))
        {
            fade.color = new Color(0f, 0f, 0f, Mathf.SmoothStep(from, to, t));
            yield return null;
        }
        fade.color = new Color(0f, 0f, 0f, to);
    }

    void EnsureFade()
    {
        if (fade != null) return;
        var cgo = new GameObject("SleepFadeCanvas");
        var canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;   // above all other UI

        var igo = new GameObject("Black", typeof(RectTransform));
        igo.transform.SetParent(cgo.transform, false);
        fade = igo.AddComponent<Image>();
        fade.color = new Color(0f, 0f, 0f, 0f);
        fade.raycastTarget = true;   // blocks UI clicks while asleep
        var rt = fade.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    void SetPlayerControl(bool on)
    {
        if (movement != null) movement.enabled = on;
        if (interactor != null)
        {
            interactor.enabled = on;
            if (interactor.crosshairImage != null) interactor.crosshairImage.enabled = on;
        }
    }
}
