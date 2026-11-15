using UnityEngine;

public class BatteryPickup : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        if (SimpleInventory.Instance != null)
        {
            SimpleInventory.Instance.GiveBattery();
        }
        else
        {
            Debug.LogWarning("No SimpleInventory found on Player!");
        }
    }
}