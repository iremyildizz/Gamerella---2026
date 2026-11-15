using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FirstPersonHackHUD : MonoBehaviour
{
    [Header("Progress Bar & Percentage")]
    [SerializeField] private Image progressBarFill;
    [SerializeField] private TextMeshProUGUI progressPercentText;
    [SerializeField] private float barSmoothSpeed = 10f;

    [Header("Sync Settings")]
    [SerializeField] private string terminalSaveId = "Monitor_01";
    [SerializeField] private string labelPrefix = ""; // Leave empty to show "26%", or set to "HACK: "

    private float targetProgress = 0f;
    private float displayedProgress = 0f;

    private void OnEnable()
    {
        SciFiTerminalHUD.OnHackProgressChanged += SetTargetProgress;

        // Immediately sync to saved progress without lag when returning from the monitor
        float saved = SciFiTerminalHUD.GetSavedProgress(terminalSaveId);
        targetProgress = saved;
        displayedProgress = saved;
        ApplyUI(displayedProgress);
    }

    private void OnDisable()
    {
        SciFiTerminalHUD.OnHackProgressChanged -= SetTargetProgress;
    }

    private void Update()
    {
        if (!Mathf.Approximately(displayedProgress, targetProgress))
        {
            displayedProgress = Mathf.MoveTowards(displayedProgress, targetProgress, barSmoothSpeed * Time.deltaTime);
            ApplyUI(displayedProgress);
        }
    }

    private void SetTargetProgress(float normalizedProgress)
    {
        targetProgress = Mathf.Clamp01(normalizedProgress);
    }

    private void ApplyUI(float value)
    {
        if (progressBarFill != null)
            progressBarFill.fillAmount = value;

        if (progressPercentText != null)
            progressPercentText.text = $"{labelPrefix}{Mathf.RoundToInt(value * 100f)}%";
    }
}