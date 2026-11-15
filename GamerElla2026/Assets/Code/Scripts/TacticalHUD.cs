using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;


public class TacticalHUD : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] GameObject tacticalPanel;
    [SerializeField] GameObject warningPopup;
    [Tooltip("Optional: the popup's message text, so needs can set their own message")]
    [SerializeField] TMP_Text warningMessage;
    [SerializeField] bool panelStartsOpen = false;

    [Header("Input")]
    [SerializeField] Key toggleKey = Key.Q;

    [Header("Test warnings (until needs are implemented)")]
    [Tooltip("Show the warning every N seconds. 0 = off")]
    [SerializeField] float testWarningInterval = 5f;
    [Tooltip("Don't pop the test warning while the panel is already open")]
    [SerializeField] bool skipWhilePanelOpen = true;

    [Header("Events (sounds...)")]
    public UnityEvent onWarningShown;
    public UnityEvent onPanelOpened;
    public UnityEvent onPanelClosed;

    public bool PanelOpen => tacticalPanel != null && tacticalPanel.activeSelf;
    public bool WarningVisible => warningPopup != null && warningPopup.activeSelf;

    void Awake()
    {
        if (tacticalPanel != null) tacticalPanel.SetActive(panelStartsOpen);
        if (warningPopup != null) warningPopup.SetActive(false);
    }

    void Start()
    {
        if (testWarningInterval > 0f) StartCoroutine(TestWarnings());
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
        {
            HideWarning();
            SetPanel(!PanelOpen);
        }
    }

    public void ShowWarning(string message = null)
    {
        if (warningPopup == null) return;
        if (warningMessage != null && !string.IsNullOrEmpty(message)) warningMessage.text = message;
        if (!warningPopup.activeSelf)
        {
            warningPopup.SetActive(true);
            warningPopup.transform.SetAsLastSibling();   // draw on top
            onWarningShown.Invoke();
        }
    }

    public void HideWarning()
    {
        if (warningPopup != null) warningPopup.SetActive(false);
    }

    public void SetPanel(bool open)
    {
        if (tacticalPanel == null || tacticalPanel.activeSelf == open) return;
        tacticalPanel.SetActive(open);
        if (open) onPanelOpened.Invoke(); else onPanelClosed.Invoke();
    }

    IEnumerator TestWarnings()
    {
        while (true)
        {
            yield return new WaitForSeconds(testWarningInterval);
            if (testWarningInterval <= 0f) yield break;
            if (skipWhilePanelOpen && PanelOpen) continue;
            ShowWarning();
        }
    }
}
