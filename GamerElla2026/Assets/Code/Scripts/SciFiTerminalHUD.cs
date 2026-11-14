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
    [SerializeField] private float correctStepGain = 0.04f;  // +4% per matching character (25 chars = 100%)
    [SerializeField] private float errorStepPenalty = 0.06f; // -6% per wrong character
    [SerializeField] private float barSmoothSpeed = 10f;

    [Header("Random Code Settings")]
    [SerializeField] private int codeLength = 12;
    [SerializeField] private bool caseSensitive = false;
    [SerializeField] private float nextCodeDelay = 0.25f;
    private const string AllowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    [Header("Highlight Colors (Hex)")]
    [SerializeField] private string untypedColorHex = "#B0B8C0"; // Light Gray
    [SerializeField] private string correctColorHex = "#7DF9FF"; // Light Cyan
    [SerializeField] private string wrongColorHex = "#FF3B3B"; // Red

    [Header("Input Prompt Settings")]
    [SerializeField] private string promptPrefix = "> ";
    [SerializeField] private bool showBlinkingCursor = true;

    private string targetCode = "";
    private string currentTypedText = "";
    private float currentProgress = 0f;
    private float displayedProgress = 0f;
    private float cursorTimer;
    private bool cursorVisible = true;
    private bool isTransitioningCode = false;
    private bool isHackComplete = false;

    private void OnEnable()
    {
        if (Keyboard.current != null)
            Keyboard.current.onTextInput += OnTextInput;
    }

    private void OnDisable()
    {
        if (Keyboard.current != null)
            Keyboard.current.onTextInput -= OnTextInput;
    }

    private void Start()
    {
        currentProgress = 0f;
        displayedProgress = 0f;
        GenerateRandomCode();
        RefreshDisplay();
        UpdateProgressUI(true);
    }

    private void Update()
    {
        // Smoothly animate the progress bar fill
        if (!Mathf.Approximately(displayedProgress, currentProgress))
        {
            displayedProgress = Mathf.MoveTowards(displayedProgress, currentProgress, barSmoothSpeed * Time.deltaTime);
            UpdateProgressUI(false);
        }

        if (!showBlinkingCursor || terminalInputText == null || isHackComplete) return;

        cursorTimer += Time.deltaTime;
        if (cursorTimer >= 0.5f)
        {
            cursorTimer = 0f;
            cursorVisible = !cursorVisible;
            RefreshInputDisplay();
        }
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
        isTransitioningCode = false;
    }

    private void OnTextInput(char character)
    {
        if (isHackComplete || isTransitioningCode) return;

        // Handle Backspace
        if (character == '\b')
        {
            if (currentTypedText.Length > 0)
            {
                currentTypedText = currentTypedText.Substring(0, currentTypedText.Length - 1);
            }
        }
        // Handle normal alphanumeric / symbol input
        else if (!char.IsControl(character) && currentTypedText.Length < targetCode.Length)
        {
            char normalizedChar = caseSensitive ? character : char.ToUpper(character);
            int charIndex = currentTypedText.Length;
            char expectedChar = targetCode[charIndex];

            bool isMatch = caseSensitive
                ? normalizedChar == expectedChar
                : char.ToUpper(normalizedChar) == char.ToUpper(expectedChar);

            currentTypedText += normalizedChar;

            // Adjust overall completion percentage based on match or error
            if (isMatch)
                currentProgress = Mathf.Clamp01(currentProgress + correctStepGain);
            else
                currentProgress = Mathf.Clamp01(currentProgress - errorStepPenalty);
        }

        cursorVisible = true;
        cursorTimer = 0f;
        RefreshDisplay();
        CheckCodeFinished();
    }

    private void CheckCodeFinished()
    {
        // Trigger when the player reaches the end of the current code line
        if (currentTypedText.Length < targetCode.Length) return;

        if (currentProgress >= 1f)
        {
            isHackComplete = true;
            RefreshInputDisplay();
            Debug.Log("100% Hack Complete!");
            // Trigger win state / close computer screen here
            return;
        }

        // Load a new random code after a brief pause so the player sees the final character highlight
        StartCoroutine(LoadNextCodeRoutine());
    }

    private IEnumerator LoadNextCodeRoutine()
    {
        isTransitioningCode = true;
        yield return new WaitForSeconds(nextCodeDelay);
        GenerateRandomCode();
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
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

        string cursor = (showBlinkingCursor && cursorVisible && !isHackComplete) ? "_" : "";
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
}