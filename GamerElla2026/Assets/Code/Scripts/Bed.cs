using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Put on a bed (with a collider on the Interactable layer).
// Clicking it fades the screen to black; the player wakes up rested, but
// hungrier and with the house less protected (time passed).
public class Bed : MonoBehaviour, IInteractable
{
    [Header("Timing (seconds)")]
    [SerializeField] float fadeOutTime = 1.5f;
    [SerializeField] float blackTime = 1.5f;
    [SerializeField] float fadeInTime = 1.2f;

    [Header("Cost of sleeping")]
    [Tooltip("Hunger lost while sleeping (out of 100)")]
    [SerializeField] float hungerCost = 30f;
    [Tooltip("House protection lost while sleeping (out of 100)")]
    [SerializeField] float protectionCost = 25f;

    [Header("Rules")]
    [Tooltip("Can only sleep when sleep is at or below this percent (100 = always)")]
    [SerializeField, Range(0f, 100f)] float sleepAllowedBelowPercent = 100f;

    [Header("Events (sounds, messages...)")]
    public UnityEvent onFellAsleep;   // screen fully black
    public UnityEvent onWokeUp;
    public UnityEvent onNotTired;     // tried to sleep while too rested

    public bool IsSleeping { get; private set; }

    PlayerSleep sleep;
    PlayerHunger hunger;
    PlayerCharacter3D movement;
    PlayerInteractor interactor;
    Image fade;

    void Awake()
    {
        sleep = FindFirstObjectByType<PlayerSleep>();
        hunger = FindFirstObjectByType<PlayerHunger>();
        movement = FindFirstObjectByType<PlayerCharacter3D>();
        interactor = FindFirstObjectByType<PlayerInteractor>();
        if (sleep == null) Debug.LogWarning("Bed: no PlayerSleep on the player", this);
    }

    public void Interact()
    {
        if (IsSleeping) return;
        if (sleep != null && sleep.Percent * 100f > sleepAllowedBelowPercent)
        {
            onNotTired.Invoke();
            return;
        }
        StartCoroutine(SleepRoutine());
    }

    IEnumerator SleepRoutine()
    {
        IsSleeping = true;
        SetPlayerControl(false);
        EnsureFade();

        yield return Fade(0f, 1f, fadeOutTime);
        onFellAsleep.Invoke();

        if (sleep != null) sleep.Rest();
        if (hunger != null) hunger.Drain(hungerCost);
        if (HouseProtection.Instance != null) HouseProtection.Instance.Drain(protectionCost);

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
