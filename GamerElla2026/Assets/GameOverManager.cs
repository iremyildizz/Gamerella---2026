using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance { get; private set; }

    public enum LoseReason
    {
        Starvation,
        ZombiesBreached
    }

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverCanvasOrPanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text reasonText;

    [Header("Messages")]
    [SerializeField] private string starvationTitle = "YOU DIED";
    [SerializeField, TextArea] private string starvationMessage = "You died from starvation.";
    [SerializeField] private string zombieTitle = "BREACHED";
    [SerializeField, TextArea] private string zombieMessage = "The defenses failed and the zombies got inside.";

    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip starvationSound;
    [SerializeField] private AudioClip zombieBreachSound;

    [Header("Controls to Disable on Death")]
    [SerializeField] private MonoBehaviour[] scriptsToDisable;

    [Header("Events")]
    public UnityEvent onStarvationGameOver;
    public UnityEvent onZombieBreachGameOver;

    public bool IsGameOver { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (gameOverCanvasOrPanel != null)
            gameOverCanvasOrPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void TriggerGameOver(LoseReason reason)
    {
        if (IsGameOver) return;
        IsGameOver = true;

        // Disable player movement / interaction scripts
        if (scriptsToDisable != null)
        {
            foreach (var script in scriptsToDisable)
            {
                if (script != null) script.enabled = false;
            }
        }

        // Update UI and play matching audio
        if (reason == LoseReason.Starvation)
        {
            if (titleText != null) titleText.text = starvationTitle;
            if (reasonText != null) reasonText.text = starvationMessage;
            if (sfxSource != null && starvationSound != null)
                sfxSource.PlayOneShot(starvationSound);

            onStarvationGameOver?.Invoke();
        }
        else if (reason == LoseReason.ZombiesBreached)
        {
            if (titleText != null) titleText.text = zombieTitle;
            if (reasonText != null) reasonText.text = zombieMessage;
            if (sfxSource != null && zombieBreachSound != null)
                sfxSource.PlayOneShot(zombieBreachSound);

            onZombieBreachGameOver?.Invoke();
        }

        if (gameOverCanvasOrPanel != null)
            gameOverCanvasOrPanel.SetActive(true);

        // Unlock cursor so the player can click Restart / Main Menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    // Hook this up to a "Retry" UI button
    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}