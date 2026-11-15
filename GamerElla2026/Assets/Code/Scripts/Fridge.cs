using UnityEngine;

public class Fridge : MonoBehaviour, IInteractable
{
    [Header("Door Hinge Settings")]
    public Transform doorHinge;

    [Tooltip("Angle in degrees to swing open")]
    public float openAngle = 100f;
    public float swingSpeed = 6f;

    private Quaternion closedRotation;
    private Quaternion openRotation;
    private bool isOpen = false;

    void Start()
    {
        if (doorHinge == null)
        {
            Debug.LogWarning($"DoorHinge not assigned on {gameObject.name}");
            return;
        }

        closedRotation = doorHinge.localRotation;
        openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
    }

    void Update()
    {
        if (doorHinge == null) return;

        Quaternion targetRotation = isOpen ? openRotation : closedRotation;
        doorHinge.localRotation = Quaternion.Slerp(
            doorHinge.localRotation,
            targetRotation,
            Time.deltaTime * swingSpeed
        );
    }

    public void Interact()
    {
        isOpen = !isOpen;
    }

    public void OnHoverExit()
    {
        isOpen = false;
    }
}