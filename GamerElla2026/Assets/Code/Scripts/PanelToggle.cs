using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PanelToggle : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] GameObject panel;
    [Tooltip("The X button inside the panel")]
    [SerializeField] Button closeButton;
    [Tooltip("The tab at the bottom that reopens the panel")]
    [SerializeField] Button openTab;
    [SerializeField] bool startOpen = true;

    [Header("Cursor")]
    [Tooltip("Hold this key to show the mouse and click UI")]
    [SerializeField] Key uiKey = Key.LeftAlt;
    [Tooltip("Optional: also toggle the panel with this key (None = off)")]
    [SerializeField] Key toggleKey = Key.None;

    [Header("Player (found automatically)")]
    [SerializeField] PlayerCharacter3D movement;
    [SerializeField] PlayerInteractor interactor;
    [SerializeField] ScreenZoomController zoom;

    bool uiMode;

    void Awake()
    {
        if (movement == null) movement = FindFirstObjectByType<PlayerCharacter3D>();
        if (interactor == null) interactor = FindFirstObjectByType<PlayerInteractor>();
        if (zoom == null) zoom = FindFirstObjectByType<ScreenZoomController>();

        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (openTab != null) openTab.onClick.AddListener(Open);
        SetOpen(startOpen);
    }

    public void Open() => SetOpen(true);
    public void Close() => SetOpen(false);
    public void Toggle() => SetOpen(!panel.activeSelf);

    void SetOpen(bool open)
    {
        if (panel != null) panel.SetActive(open);
        if (openTab != null) openTab.gameObject.SetActive(!open);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (toggleKey != Key.None && kb[toggleKey].wasPressedThisFrame) Toggle();

        bool zoomed = zoom != null && zoom.IsZoomed;
        bool wantUi = !zoomed && kb[uiKey].isPressed;
        if (wantUi != uiMode) SetUiMode(wantUi);
    }

    void SetUiMode(bool on)
    {
        uiMode = on;
        Cursor.lockState = on ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = on;
        if (movement != null) movement.enabled = !on;
        if (interactor != null) interactor.enabled = !on;
    }
}
