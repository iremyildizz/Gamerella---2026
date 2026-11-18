using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerInteractor : MonoBehaviour {
    [Header("Interaction Settings")]
    public Transform playerCamera;
    public float interactRange = 3f;
    public LayerMask interactableLayer = ~0;

    [Header("Crosshair UI")]
    public Image crosshairImage;
    public Sprite defaultDotSprite;
    public Sprite interactIconSprite;
    public TMP_Text cooldownTimer;
    public TMP_Text tooltipText;
    public Vector2 defaultDotSize = new Vector2(12f, 12f);
    public Vector2 interactIconSize = new Vector2(36f, 36f);
    public Color toolTipDefaultColor = Color.white;

    private InputAction interactAction;
    private IInteractable currentInteractable;
    private InteractableOutline currentOutline;

    public bool HasInteractable => currentInteractable != null;

    void Start() {
        interactAction = InputSystem.actions.FindAction("Click");
        UpdateCrosshair(false);
        UpdateCooldownUI(0f);
        UpdateTooltip(null, toolTipDefaultColor);
    }

    void Update() {
        DetectInteractable();

        if (currentInteractable != null) {
            UpdateCooldownUI(currentInteractable.GetCooldown());
            UpdateTooltip(currentInteractable.GetTooltip(), currentInteractable.GetTooltipColor());

            if (interactAction != null) {
                if (interactAction.WasPressedThisFrame()) {
                    if (currentInteractable is MonoBehaviour targetBehaviour) {
                        SimpleInventory.Instance?.OnPlayerInteractedWith(targetBehaviour.gameObject);
                    }

                    currentInteractable.Interact();
                }

                currentInteractable.InteractHold(interactAction.IsPressed());
            }
        } else {
            UpdateCooldownUI(0f);
            UpdateTooltip(null, toolTipDefaultColor);
        }
    }

    void DetectInteractable() {
        IInteractable previousInteractable = currentInteractable;
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayer)) {
            currentInteractable = hit.collider.GetComponentInParent<IInteractable>();
        } else {
            currentInteractable = null;
        }

        if (currentInteractable != previousInteractable) {
            previousInteractable?.InteractHold(false);
            previousInteractable?.OnHoverExit();

            currentInteractable?.OnHoverEnter();

            if (currentOutline != null) {
                currentOutline.SetOutline(false);
                currentOutline = null;
            }

            if (currentInteractable is MonoBehaviour targetBehaviour) {
                currentOutline = targetBehaviour.GetComponentInParent<InteractableOutline>();
                if (currentOutline != null) {
                    currentOutline.SetOutline(true);
                }
            }

            UpdateCrosshair(currentInteractable != null);
        }
    }

    void UpdateCrosshair(bool isHoveringInteractable) {
        if (crosshairImage == null) return;

        if (isHoveringInteractable && interactIconSprite != null) {
            crosshairImage.sprite = interactIconSprite;
            crosshairImage.rectTransform.sizeDelta = interactIconSize;
        } else {
            crosshairImage.sprite = defaultDotSprite;
            crosshairImage.rectTransform.sizeDelta = defaultDotSize;
        }
    }

    void UpdateCooldownUI(float cooldown) {
        if (cooldownTimer == null) return;

        if (cooldown > 0.01f) {
            cooldownTimer.gameObject.SetActive(true);
            cooldownTimer.text = cooldown.ToString("F1");
        } else {
            cooldownTimer.gameObject.SetActive(false);
        }
    }

    void UpdateTooltip(string tooltip, Color? color = null) {
        if (string.IsNullOrEmpty(tooltip)) {
            tooltipText.gameObject.SetActive(false);
        } else {
            tooltipText.gameObject.SetActive(true);
            tooltipText.text = tooltip;
            if (color.HasValue) {
                tooltipText.color = color.Value;
            } else {
                tooltipText.color = toolTipDefaultColor;
            }
        }
    }

    void OnDrawGizmosSelected() {
        if (playerCamera == null) return;
        Gizmos.color = currentInteractable != null ? Color.green : Color.yellow;
        Gizmos.DrawRay(playerCamera.position, playerCamera.forward * interactRange);
    }
}