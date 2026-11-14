using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class FirstPersonInteractor : MonoBehaviour {
    [Header("Input")]
    public InputActionReference interactAction;

    [Header("Raycast Settings")]
    public float interactRange = 3f;
    public LayerMask interactableLayer;

    [Header("Reticle UI")]
    public Image reticleImage;
    public Color defaultColor = Color.white;
    public Color hoverColor = Color.green;

    public Camera camera;

    private Interactable currentInteractable;

    void OnEnable() {
        interactAction.action.Enable();
        interactAction.action.performed += OnInteractPerformed;
    }

    void OnDisable() {
        interactAction.action.performed -= OnInteractPerformed;
        interactAction.action.Disable();
    }

    void Update() {
        CheckForInteractable();
        Test();
    }

    private void CheckForInteractable() {
        Ray ray = new Ray(camera.transform.position, camera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayer)) {
            print("hit");
            if (hit.collider.TryGetComponent(out Interactable interactable)) {
                if (currentInteractable != interactable) {
                    ClearCurrentInteractable();
                    currentInteractable = interactable;
                    currentInteractable.HoverEnter();

                    if (reticleImage != null)
                        reticleImage.color = hoverColor;
                }
                return;
            }
        }

        ClearCurrentInteractable();
    }

    private void ClearCurrentInteractable() {
        if (currentInteractable != null) {
            currentInteractable.HoverExit();
            currentInteractable = null;

            if (reticleImage != null)
                reticleImage.color = defaultColor;
        }
    }

    private void OnInteractPerformed(InputAction.CallbackContext context) {
        if (currentInteractable != null) {
            currentInteractable.Interact();
        }
    }

    private void Test() {
        if (currentInteractable != null) {
            print("found interactable");
        } else {
            print("no interactable");
        }
    }
}
