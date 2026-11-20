using UnityEngine;
using UnityEngine.UI;

public class PlayerHunger : MonoBehaviour
{
    [Header("Hunger Settings")]
    public float maxHunger = 100f;
    [Tooltip("Percent of the bar lost per minute (10 = 10% per minute, drained smoothly)")]
    public float drainPercentPerMinute = 10f;

    [Header("UI")]
    public Slider hungerSlider;

    private float currentHunger;
    private bool hasDied = false;

    public float CurrentHunger => currentHunger;
    public bool IsStarving => currentHunger <= 0f;

    void Start()
    {
        currentHunger = maxHunger;

        if (hungerSlider != null)
        {
            hungerSlider.minValue = 0f;
            hungerSlider.maxValue = maxHunger;
            hungerSlider.value = currentHunger;
        }
    }

    void Update()
    {
        if (hasDied) return;

        if (currentHunger > 0f)
        {
            float perSecond = maxHunger * (drainPercentPerMinute / 100f) / 60f;
            currentHunger = Mathf.Max(0f, currentHunger - perSecond * Time.deltaTime);
            UpdateUI();
            CheckStarvation();
        }
    }

    public float Percent => maxHunger > 0f ? currentHunger / maxHunger : 0f;

    // Lower hunger by an amount (used when sleeping).
    public void Drain(float amount)
    {
        if (hasDied) return;
        currentHunger = Mathf.Clamp(currentHunger - amount, 0f, maxHunger);
        UpdateUI();
        CheckStarvation();

        if (GameOverManager.Instance != null && GameOverManager.Instance.IsGameOver)
            return;
    }

    public void ReplenishHunger()
    {
        currentHunger = maxHunger;
        UpdateUI();
    }

    private void CheckStarvation()
    {
        if (!hasDied && currentHunger <= 0f)
        {
            hasDied = true;
            if (GameOverManager.Instance != null)
                GameOverManager.Instance.TriggerGameOver(GameOverManager.LoseReason.Starvation);
        }
    }

    private void UpdateUI()
    {
        if (hungerSlider != null)
        {
            hungerSlider.value = currentHunger;
        }
    }
}