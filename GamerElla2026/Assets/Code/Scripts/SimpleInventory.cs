using UnityEngine;
using TMPro;

public class SimpleInventory : MonoBehaviour
{
    public static SimpleInventory Instance { get; private set; }

    [Header("HUD Display")]
    [SerializeField] private TextMeshProUGUI inventoryStatusText;
    [SerializeField] private string holdingBatteryText = "Inventory: Battery";
    [SerializeField] private string emptyInventoryText = "Inventory: Empty";

    public bool HasBattery { get; private set; } = false;

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
        RefreshUI();
        Debug.Log("Picked up a battery.");
    }

    public void RemoveBattery(string reason = "")
    {
        if (!HasBattery) return;

        HasBattery = false;
        RefreshUI();

        if (!string.IsNullOrEmpty(reason))
            Debug.Log($"Battery removed: {reason}");
    }

    /// <summary>
    /// Call this from your Player interaction raycast script right before or after calling target.Interact().
    /// If the player interacts with anything other than a BatteryPickup or CameraLocker, the battery is removed.
    /// </summary>
    public void HandleObjectInteraction(GameObject interactedObject)
    {
        if (!HasBattery || interactedObject == null) return;

        bool isBatterySource = interactedObject.GetComponentInParent<BatteryPickup>() != null;
        bool isCameraLocker = interactedObject.CompareTag("CameraLocker"); // Ready for when you add your locker

        if (!isBatterySource && !isCameraLocker)
        {
            RemoveBattery($"Interacted with {interactedObject.name} instead of Camera Locker.");
        }
    }

    private void RefreshUI()
    {
        if (inventoryStatusText == null) return;
        inventoryStatusText.text = HasBattery ? holdingBatteryText : emptyInventoryText;
    }
}