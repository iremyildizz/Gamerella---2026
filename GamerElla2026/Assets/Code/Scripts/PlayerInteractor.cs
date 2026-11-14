using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Interaction Settings")]
    public Transform playerCamera;
    public float interactRange = 3f;
    public LayerMask interactableLayer = 0; 

    private InputAction interactAction;
    private IInteractable currentInteractable;

    public bool HasInteractable => currentInteractable != null;

    void Start()
    {
        interactAction = InputSystem.actions.FindAction("Click");
    }

    void Update()
    {
        DetectInteractable();

        if (interactAction != null && interactAction.WasPressedThisFrame() && currentInteractable != null)
        {
            currentInteractable.Interact();
        }
    }

    void DetectInteractable()
    {
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayer))
        {
            currentInteractable = hit.collider.GetComponentInParent<IInteractable>();
        }
        else
        {
            currentInteractable = null;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (playerCamera == null) return;
        Gizmos.color = currentInteractable != null ? Color.green : Color.yellow;
        Gizmos.DrawRay(playerCamera.position, playerCamera.forward * interactRange);
    }
}