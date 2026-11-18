using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MonitorInteraction : MonoBehaviour, IInteractable
{
    [Header("Mini-Game Scene")]
    [SerializeField] private string hackingSceneName = "HackingMiniGame";

    [Header("Player Controls & HUD to Pause")]
    [Tooltip("Drag your player movement, mouse-look, and interaction scripts here so the player can't walk/look around while hacking.")]
    [SerializeField] private MonoBehaviour[] scriptsToDisable;
    [SerializeField] private GameObject playerHudCanvas;

    [Header("Tooltip")]
    public string tooltipText = "+Objective";
    public Color tooltipColor = Color.blue;

    private bool isHackingOpen = false;
    private bool isTransitioning = false;

    private void Update()
    {
        if (!isHackingOpen || isTransitioning) return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            StartCoroutine(CloseMiniGameRoutine());
        }
    }

    public void Interact()
    {
        if (isHackingOpen || isTransitioning) return;
        StartCoroutine(OpenMiniGameRoutine());
    }

    private IEnumerator OpenMiniGameRoutine()
    {
        isTransitioning = true;

        // 1. Pause first-person controls and hide crosshair HUD
        SetPlayerControlsEnabled(false);
        if (playerHudCanvas != null)
            playerHudCanvas.SetActive(false);

        // 2. Unlock and show cursor for the pop-up UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 3. Load HackingMiniGame additively over MainLevel
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(hackingSceneName, LoadSceneMode.Additive);
        while (loadOp != null && !loadOp.isDone)
        {
            yield return null;
        }

        // Disable duplicate Main Camera / AudioListener / EventSystem inside HackingMiniGame so they don't conflict with MainLevel
        CleanUpAdditiveSceneDuplicates();

        isHackingOpen = true;
        isTransitioning = false;
    }

    private IEnumerator CloseMiniGameRoutine()
    {
        isTransitioning = true;

        // 1. Unload HackingMiniGame pop-up
        AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(hackingSceneName);
        while (unloadOp != null && !unloadOp.isDone)
        {
            yield return null;
        }

        // 2. Re-lock cursor and restore first-person controls + crosshair HUD
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerHudCanvas != null)
            playerHudCanvas.SetActive(true);

        SetPlayerControlsEnabled(true);

        isHackingOpen = false;
        isTransitioning = false;
    }

    private void SetPlayerControlsEnabled(bool enabledState)
    {
        if (scriptsToDisable == null) return;

        foreach (MonoBehaviour script in scriptsToDisable)
        {
            if (script != null)
                script.enabled = enabledState;
        }
    }

    private void CleanUpAdditiveSceneDuplicates()
    {
        Scene additiveScene = SceneManager.GetSceneByName(hackingSceneName);
        if (!additiveScene.IsValid()) return;

        foreach (GameObject rootObj in additiveScene.GetRootGameObjects())
        {
            // Disable the additive scene's camera & audio listener so MainLevel's camera stays active
            Camera cam = rootObj.GetComponentInChildren<Camera>(true);
            if (cam != null) cam.gameObject.SetActive(false);

            // Disable duplicate EventSystem if MainLevel already has one
            if (rootObj.name.Contains("EventSystem") && FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length > 1)
            {
                rootObj.SetActive(false);
            }
        }
    }

    public string GetTooltip()
    {
        return tooltipText;
    }

    public Color GetTooltipColor()
    {
        return tooltipColor;
    }
}