using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
public class GameWinManager : MonoBehaviour
{
    [Header("Win UI")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private float delayBeforeWinScreen = 0.8f;

    [Header("Additive Mini-Game & Player Controls")]
    [SerializeField] private string hackingSceneName = "HackingMiniGame";
    [SerializeField] private string terminalSaveId = "Monitor_01";
    [SerializeField] private MonoBehaviour[] playerScriptsToDisable;
    [SerializeField] private GameObject playerHudCanvas;

    private bool hasWon = false;

    private void Awake()
    {
        ClearSavedHackData();
    }

    private void OnEnable()
    {
        SciFiTerminalHUD.OnHackCompleted += HandleHackCompleted;
    }

    private void OnDisable()
    {
        SciFiTerminalHUD.OnHackCompleted -= HandleHackCompleted;
    }

    private void Start()
    {
        if (winPanel != null)
            winPanel.SetActive(false);
    }

    private void HandleHackCompleted()
    {
        if (hasWon) return;
        hasWon = true;
        StartCoroutine(WinSequenceRoutine());
    }

    private IEnumerator WinSequenceRoutine()
    {
        // Brief pause so the player sees "ACCESS GRANTED" and 100% on the monitor
        yield return new WaitForSeconds(delayBeforeWinScreen);

        // Close HackingMiniGame overlay if it is currently loaded
        Scene miniGameScene = SceneManager.GetSceneByName(hackingSceneName);
        if (miniGameScene.IsValid() && miniGameScene.isLoaded)
        {
            AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(hackingSceneName);
            while (unloadOp != null && !unloadOp.isDone)
                yield return null;
        }

        // Disable first-person movement/look and hide the regular HUD
        if (playerScriptsToDisable != null)
        {
            foreach (MonoBehaviour script in playerScriptsToDisable)
            {
                if (script != null)
                    script.enabled = false;
            }
        }

        if (playerHudCanvas != null)
            playerHudCanvas.SetActive(false);

        // Unlock cursor and show the Win UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (winPanel != null)
            winPanel.SetActive(true);
    }

    // Hook this up to a "Play Again" / "Restart" UI Button on your WinPanel
    public void RestartGame()
    {
        ClearSavedHackData();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadMainMenu()
    {
        ClearSavedHackData();
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    private void ClearSavedHackData()
    {
        PlayerPrefs.DeleteKey($"{terminalSaveId}_Progress");
        PlayerPrefs.DeleteKey($"{terminalSaveId}_TargetCode");
        PlayerPrefs.DeleteKey($"{terminalSaveId}_TypedText");
        PlayerPrefs.DeleteKey($"{terminalSaveId}_IsSolved");
        PlayerPrefs.Save();
    }
}