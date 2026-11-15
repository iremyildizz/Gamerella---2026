using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimpleInventory : MonoBehaviour
{
    public static SimpleInventory Instance { get; private set; }

    [Header("Inventory UI Elements")]
    [SerializeField] private GameObject inventoryBadgePanel; // Parent UI Panel holding icon + text (NOT Canvas!)
    [SerializeField] private Image batteryIconImage;         // UI Image using battery.png
    [SerializeField] private TextMeshProUGUI inventoryText;

    [Header("Display Options")]
    [SerializeField] private bool hideWhenEmpty = true;
    [SerializeField] private string holdingText = "1x Battery";
    [SerializeField] private string emptyText = "No Battery";

    public bool HasBattery { get; private set; } = false;
    private bool isSuppressedByPrompt = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        RefreshUI();
    }

    public void GiveBattery()
    {
        HasBattery = true;
        isSuppressedByPrompt = false;

        // Hide any active locker prompt immediately when picking up a battery
        CameraLocker activeLocker = FindFirstObjectByType<CameraLocker>();
        if (activeLocker != null)
            activeLocker.HidePromptImmediately();

        RefreshUI();
    }

    public void RemoveBattery(string reason = "")
    {
        if (!HasBattery) return;

        HasBattery = false;
        RefreshUI();
    }

    /// <summary>
    /// Temporarily hides or restores the inventory UI when another HUD prompt (like LockerPromptText) is active.
    /// </summary>
    public void SetSuppressedByPrompt(bool suppressed)
    {
        isSuppressedByPrompt = suppressed;
        RefreshUI();
    }

    public void OnPlayerInteractedWith(GameObject target)
    {
        if (!HasBattery || target == null) return;

        if (target.GetComponentInParent<BatteryPickup>() != null)
            return;

        if (IsBatteryLocker(target))
            return;

        RemoveBattery();
    }

    private bool IsBatteryLocker(GameObject target)
    {
        Transform current = target.transform;
        while (current != null)
        {
            if (current.GetComponent<CameraLocker>() != null)
                return true;

            string cleanName = current.name.Replace(" ", "").ToLowerInvariant();
            if (cleanName.Contains("batterylocker") || cleanName.Contains("cameralocker"))
                return true;

            current = current.parent;
        }

        return target.CompareTag("BatteryLocker") || target.CompareTag("CameraLocker");
    }

    private void RefreshUI()
    {
        bool shouldShowBadge = !isSuppressedByPrompt && (!hideWhenEmpty || HasBattery);

        if (inventoryBadgePanel != null)
            inventoryBadgePanel.SetActive(shouldShowBadge);

        if (batteryIconImage != null)
        {
            batteryIconImage.gameObject.SetActive(shouldShowBadge);
            batteryIconImage.color = HasBattery ? Color.white : new Color(1f, 1f, 1f, 0.25f);
        }

        if (inventoryText != null)
        {
            inventoryText.gameObject.SetActive(shouldShowBadge);
            inventoryText.text = HasBattery ? holdingText : emptyText;
        }
    }
}