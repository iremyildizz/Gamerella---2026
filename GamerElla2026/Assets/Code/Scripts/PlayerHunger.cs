using UnityEngine;
using UnityEngine.UI;

public class PlayerHunger : MonoBehaviour
{
    [Header("Hunger Settings")]
    public float maxHunger = 100f;
    public float drainPerSecond = 2f;

    [Header("UI")]
    public Slider hungerSlider;

    private float currentHunger;

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
        if (currentHunger > 0f)
        {
            currentHunger = Mathf.Max(0f, currentHunger - drainPerSecond * Time.deltaTime);
            UpdateUI();
        }
    }

    public float Percent => maxHunger > 0f ? currentHunger / maxHunger : 0f;

    // Lower hunger by an amount (used when sleeping).
    public void Drain(float amount)
    {
        currentHunger = Mathf.Clamp(currentHunger - amount, 0f, maxHunger);
        UpdateUI();
    }

    public void ReplenishHunger()
    {
        currentHunger = maxHunger;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (hungerSlider != null)
        {
            hungerSlider.value = currentHunger;
        }
    }
}