using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Scenes")]
    [Tooltip("Scene name or project path (e.g. Assets/Level/Scenes/MyLevel.unity). Must be in Build Settings.")]
    [SerializeField] private string gameScene = "Assets/Level/Scenes/MaínLevel.unity";

    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject controlsPanel;

    [Header("Selection (keyboard / gamepad)")]
    [SerializeField] private GameObject firstSelectedMain;
    [SerializeField] private GameObject firstSelectedControls;

    [Header("Hacking save data to wipe on new game")]
    [SerializeField] private string terminalSaveId = "Monitor_01";

    private void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ShowMain();
    }

    private void Update()
    {
        if (controlsPanel != null && controlsPanel.activeSelf &&
            Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ShowMain();
        }
    }

    public void Play()
    {
        ClearSavedHackData();
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameScene);
    }

    public void ShowControls()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(true);
        Select(firstSelectedControls);
    }

    public void ShowMain()
    {
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
        Select(firstSelectedMain);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void Select(GameObject target)
    {
        if (EventSystem.current != null && target != null)
            EventSystem.current.SetSelectedGameObject(target);
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
