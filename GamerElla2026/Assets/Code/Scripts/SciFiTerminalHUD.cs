using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class SciFiTerminalHUD : MonoBehaviour
{
    [Header("UI Text References")]
    [SerializeField] private TextMeshProUGUI targetCodeText;
    [SerializeField] private TextMeshProUGUI terminalInputText;

    [Header("Progress Bar & Percentage")]
    [SerializeField] private Image progressBarFill;
    [SerializeField] private TextMeshProUGUI progressPercentText;
    [SerializeField] private float correctStepGain = 0.04f;  // +4% per matching character
    [SerializeField] private float errorStepPenalty = 0.06f; // -6% per wrong character
    [SerializeField] private float barSmoothSpeed = 10f;

    [Header("Random Code & Loading Animation")]
    [SerializeField] private int codeLength = 12;
    [SerializeField] private bool caseSensitive = false;
    [SerializeField] private float loadingDuration = 1.2f;
    [SerializeField] private float dotStepInterval = 0.25f;
    [SerializeField] private string loadingPrefix = "LOADING NEXT SEQUENCE";
    private const string AllowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    [Header("Save Settings")]
    [SerializeField] private string terminalSaveId = "Monitor_01";

    [Header("Highlight Colors (Hex)")]
    [SerializeField] private string untypedColorHex = "#B0B8C0"; // Light Gray
    [SerializeField] private string correctColorHex = "#7DF9FF"; // Light Cyan
    [SerializeField] private string wrongColorHex = "#FF3B3B"; // Red

    [Header("Input Prompt & Feedback")]
    [SerializeField] private string promptPrefix = "> ";
    [SerializeField] private bool showBlinkingCursor = true;
    [SerializeField] private float errorShakeDuration = 0.12f;
    [SerializeField] private float errorShakeMagnitude = 6f;

    private string targetCode = "";
    private string currentTypedText = "";
    private float currentProgress = 0f;
    private float displayedProgress = 0f;
    private float cursorTimer;
    private bool cursorVisible = true;
    private bool isLoadingNextCode = false;
    private bool isHackComplete = false;
    private Vector2 originalTargetPos;

    private string KeyProgress => $"{terminalSaveId}_Progress";
    private string KeyTargetCode => $"{terminalSaveId}_TargetCode";
    private string KeyTypedText => $"{terminalSaveId}_TypedText";
    private string KeySolved => $"{terminalSaveId}_IsSolved";

    private void OnEnable()
    {
        if (Keyboard.current != null)
            Keyboard.current.onTextInput += OnTextInput;
    }

    private void OnDisable()
    {
        if (Keyboard.current != null)
            Keyboard.current.onTextInput -= OnTextInput;

        // Save state immediately whenever the pop-up is closed or unloaded
        SaveTerminalState();
    }

    private void OnApplicationQuit()
    {
        SaveTerminalState();
    }

    private void Start()
    {
        if (targetCodeText != null)
            originalTargetPos = targetCodeText.rectTransform.anchoredPosition;

        LoadOrInitializeState();
        RefreshDisplay();
        UpdateProgressUI(true);
    }

    private void Update()
    {
        if (!Mathf.Approximately(displayedProgress, currentProgress))
        {
            displayedProgress = Mathf.MoveTowards(displayedProgress, currentProgress, barSmoothSpeed * Time.deltaTime);
            UpdateProgressUI(false);
        }

        if (!showBlinkingCursor || terminalInputText == null || isHackComplete || isLoadingNextCode) return;

        cursorTimer += Time.deltaTime;
        if (cursorTimer >= 0.5f)
        {
            cursorTimer = 0f;
            cursorVisible = !cursorVisible;
            RefreshInputDisplay();
        }
    }

    private void LoadOrInitializeState()
    {
        currentProgress = Mathf.Clamp01(PlayerPrefs.GetFloat(KeyProgress, 0f));
        displayedProgress = currentProgress;
        isHackComplete = PlayerPrefs.GetInt(KeySolved, 0) == 1 || currentProgress >= 1f;

        string savedCode = PlayerPrefs.GetString(KeyTargetCode, "");
        string savedTyped = PlayerPrefs.GetString(KeyTypedText, "");

        // If we closed the screen mid-loading or before a code existed, generate a fresh code while keeping currentProgress
        if (string.IsNullOrEmpty(savedCode) || savedCode.Length != codeLength || savedTyped.Length >= savedCode.Length)
        {
            GenerateRandomCode();
        }
        else
        {
            targetCode = savedCode;
            currentTypedText = savedTyped;
            isLoadingNextCode = false;
        }
    }

    private void SaveTerminalState()
    {
        PlayerPrefs.SetFloat(KeyProgress, currentProgress);
        PlayerPrefs.SetString(KeyTargetCode, targetCode);
        PlayerPrefs.SetString(KeyTypedText, currentTypedText);
        PlayerPrefs.SetInt(KeySolved, isHackComplete ? 1 : 0);
        PlayerPrefs.Save();

        OnHackProgressChanged?.Invoke(currentProgress);
    }

    private void GenerateRandomCode()
    {
        StringBuilder sb = new StringBuilder(codeLength);
        for (int i = 0; i < codeLength; i++)
        {
            int index = Random.Range(0, AllowedChars.Length);
            sb.Append(AllowedChars[index]);
        }

        targetCode = sb.ToString();
        currentTypedText = "";
        isLoadingNextCode = false;
        SaveTerminalState();
    }

    private void OnTextInput(char character)
    {
        if (isHackComplete || isLoadingNextCode) return;

        if (character == '\b')
        {
            if (currentTypedText.Length > 0)
            {
                currentTypedText = currentTypedText.Substring(0, currentTypedText.Length - 1);
            }
        }
        else if (!char.IsControl(character) && currentTypedText.Length < targetCode.Length)
        {
            char normalizedChar = caseSensitive ? character : char.ToUpper(character);
            int charIndex = currentTypedText.Length;
            char expectedChar = targetCode[charIndex];

            bool isMatch = caseSensitive
                ? normalizedChar == expectedChar
                : char.ToUpper(normalizedChar) == char.ToUpper(expectedChar);

            currentTypedText += normalizedChar;

            if (isMatch)
            {
                currentProgress = Mathf.Clamp01(currentProgress + correctStepGain);
            }
            else
            {
                currentProgress = Mathf.Clamp01(currentProgress - errorStepPenalty);
                StopCoroutine(nameof(ShakeTargetRoutine));
                StartCoroutine(nameof(ShakeTargetRoutine));
            }
        }

        cursorVisible = true;
        cursorTimer = 0f;
        SaveTerminalState();
        RefreshDisplay();
        CheckCodeFinished();
    }

    private void CheckCodeFinished()
    {
        if (currentProgress >= 1f)
        {
            isHackComplete = true;
            SaveTerminalState();
            RefreshDisplay();
            return;
        }

        if (currentTypedText.Length >= targetCode.Length)
        {
            // Clear typed text before starting loading transition so closing mid-load doesn't restore a finished line
            currentTypedText = "";
            targetCode = "";
            SaveTerminalState();
            StartCoroutine(LoadNextCodeWithDotsRoutine());
        }
    }

    private IEnumerator LoadNextCodeWithDotsRoutine()
    {
        isLoadingNextCode = true;

        yield return new WaitForSeconds(0.15f);

        if (terminalInputText != null)
            terminalInputText.text = promptPrefix;

        float elapsed = 0f;
        int dotCount = 1;

        while (elapsed < loadingDuration)
        {
            if (targetCodeText != null)
            {
                string dots = new string('.', dotCount);
                targetCodeText.text = $"<color={correctColorHex}>{loadingPrefix}{dots}</color>";
            }

            dotCount = (dotCount % 3) + 1;
            yield return new WaitForSeconds(dotStepInterval);
            elapsed += dotStepInterval;
        }

        GenerateRandomCode();
        RefreshDisplay();
    }

    private IEnumerator ShakeTargetRoutine()
    {
        if (targetCodeText == null) yield break;

        float elapsed = 0f;
        while (elapsed < errorShakeDuration)
        {
            float offsetX = Random.Range(-errorShakeMagnitude, errorShakeMagnitude);
            targetCodeText.rectTransform.anchoredPosition = originalTargetPos + new Vector2(offsetX, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        targetCodeText.rectTransform.anchoredPosition = originalTargetPos;
    }

    private void RefreshDisplay()
    {
        if (isHackComplete)
        {
            if (targetCodeText != null)
                targetCodeText.text = $"<color={correctColorHex}>ACCESS GRANTED</color>";
            RefreshInputDisplay();
            return;
        }

        RefreshTargetDisplay();
        RefreshInputDisplay();
    }

    private void RefreshTargetDisplay()
    {
        if (targetCodeText == null) return;

        StringBuilder formatted = new StringBuilder();

        for (int i = 0; i < targetCode.Length; i++)
        {
            char targetChar = targetCode[i];

            if (i < currentTypedText.Length)
            {
                char typedChar = currentTypedText[i];
                bool isMatch = caseSensitive
                    ? typedChar == targetChar
                    : char.ToUpper(typedChar) == char.ToUpper(targetChar);

                string color = isMatch ? correctColorHex : wrongColorHex;
                formatted.Append($"<color={color}>{targetChar}</color>");
            }
            else
            {
                formatted.Append($"<color={untypedColorHex}>{targetChar}</color>");
            }
        }

        targetCodeText.text = formatted.ToString();
    }

    private void RefreshInputDisplay()
    {
        if (terminalInputText == null) return;

        string cursor = (showBlinkingCursor && cursorVisible && !isHackComplete && !isLoadingNextCode) ? "_" : "";
        terminalInputText.text = promptPrefix + currentTypedText + cursor;
    }

    private void UpdateProgressUI(bool instant)
    {
        float value = instant ? currentProgress : displayedProgress;

        if (progressBarFill != null)
            progressBarFill.fillAmount = value;

        if (progressPercentText != null)
            progressPercentText.text = $"{Mathf.RoundToInt(value * 100f)}%";
    }
    public static event System.Action<float> OnHackProgressChanged;

    public static float GetSavedProgress(string saveId = "Monitor_01")
    {
        return Mathf.Clamp01(PlayerPrefs.GetFloat($"{saveId}_Progress", 0f));
    }


    // Call this from a "New Game" button or debug menu when you want to wipe saved hack progress
    public void ResetSavedTerminalData()
    {
        PlayerPrefs.DeleteKey(KeyProgress);
        PlayerPrefs.DeleteKey(KeyTargetCode);
        PlayerPrefs.DeleteKey(KeyTypedText);
        PlayerPrefs.DeleteKey(KeySolved);
        PlayerPrefs.Save();

        currentProgress = 0f;
        displayedProgress = 0f;
        isHackComplete = false;
        GenerateRandomCode();
        RefreshDisplay();
        UpdateProgressUI(true);
    }


}