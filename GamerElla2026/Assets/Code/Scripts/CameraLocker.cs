using UnityEngine;

public class CameraLocker : MonoBehaviour, IInteractable
{
    [Header("Hinge & Swing Settings")]
    [SerializeField] private Transform doorHinge;
    [SerializeField] private float openAngleY = -105f; // Use 105 or -105 depending on which side the hinge is on
    [SerializeField] private float swingSpeed = 4f;

    [Header("Battery Requirement")]
    [SerializeField] private bool requireBattery = true;

    private Quaternion closedRotation;
    private Quaternion openRotation;
    private bool isOpen = false;
    private bool isUnlocked = false;

    private void Start()
    {
        if (doorHinge == null)
            doorHinge = transform;

        closedRotation = doorHinge.localRotation;
        openRotation = closedRotation * Quaternion.Euler(0f, openAngleY, 0f);
    }

    private void Update()
    {
        Quaternion targetRot = isOpen ? openRotation : closedRotation;
        doorHinge.localRotation = Quaternion.Slerp(doorHinge.localRotation, targetRot, swingSpeed * Time.deltaTime);
    }

    public void Interact()
    {
        if (requireBattery && !isUnlocked)
        {
            if (SimpleInventory.Instance != null && SimpleInventory.Instance.HasBattery)
            {
                SimpleInventory.Instance.RemoveBattery("Inserted battery into Camera Locker.");
                isUnlocked = true;
                isOpen = true;
            }
            else
            {
                Debug.Log("Locked! You need a battery to open this locker.");
                return;
            }
        }
        else
        {
            isOpen = !isOpen;
        }
    }
}