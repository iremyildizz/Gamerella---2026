using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Interaction Settings")]
    public Transform playerCamera;
    public float interactRange = 3f;
    public LayerMask interactableLayer = ~0;

    [Header("Crosshair UI")]
    public Image crosshairImage;
    public Sprite defaultDotSprite;
    public Sprite interactIconSprite;
    public Vector2 defaultDotSize = new Vector2(12f, 12f);
    public Vector2 interactIconSize = new Vector2(36f, 36f);

    private InputAction interactAction;
    private IInteractable currentInteractable;
    private InteractableOutline currentOutline;

    public bool HasInteractable => currentInteractable != null;
    private bool showCooldown = false;

    void Start()
    {
        interactAction = InputSystem.actions.FindAction("Click");
        UpdateCrosshair(false);
    }

    void Update()
    {
        DetectInteractable();

        if (interactAction != null && currentInteractable != null)
        {
            ShowInteractableCooldowm(currentInteractable.GetCooldown());

            if (interactAction.WasPressedThisFrame())
            {
                if (currentInteractable is MonoBehaviour targetBehaviour)
                {
                    SimpleInventory.Instance?.OnPlayerInteractedWith(targetBehaviour.gameObject);
                }

                currentInteractable.Interact();
            }

            currentInteractable.InteractHold(interactAction.IsPressed());
        }
    }

    void DetectInteractable()
    {
        IInteractable previousInteractable = currentInteractable;
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayer))
        {
            currentInteractable = hit.collider.GetComponentInParent<IInteractable>();
        }
        else
        {
            currentInteractable = null;
        }

        if (currentInteractable != previousInteractable)
        {
            previousInteractable?.InteractHold(false);
            previousInteractable?.OnHoverExit();

            currentInteractable?.OnHoverEnter();

            if (currentOutline != null)
            {
                currentOutline.SetOutline(false);
                currentOutline = null;
            }

            if (currentInteractable is MonoBehaviour targetBehaviour)
            {
                currentOutline = targetBehaviour.GetComponentInParent<InteractableOutline>();
                if (currentOutline != null)
                {
                    currentOutline.SetOutline(true);
                }
            }

            UpdateCrosshair(currentInteractable != null);
        }
    }

    void UpdateCrosshair(bool isHoveringInteractable)
    {
        if (crosshairImage == null) return;

        if (isHoveringInteractable && interactIconSprite != null)
        {
            crosshairImage.sprite = interactIconSprite;
            crosshairImage.rectTransform.sizeDelta = interactIconSize;
        }
        else
        {
            crosshairImage.sprite = defaultDotSprite;
            crosshairImage.rectTransform.sizeDelta = defaultDotSize;
        }
    }

    void ShowInteractableCooldowm(float cooldown) {
        if (cooldown != 0.0)
        {
            showCooldown = true;
            print(cooldown);
        }

        showCooldown = false;
    }

    void OnDrawGizmosSelected()
    {
        if (playerCamera == null) return;
        Gizmos.color = currentInteractable != null ? Color.green : Color.yellow;
        Gizmos.DrawRay(playerCamera.position, playerCamera.forward * interactRange);
    }
}