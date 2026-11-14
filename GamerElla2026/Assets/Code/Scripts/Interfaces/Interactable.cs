using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour {
    [Header("Events")]
    public UnityEvent onHoverEnter;
    public UnityEvent onHoverExit;
    public UnityEvent onInteract;

    public void HoverEnter() => onHoverEnter?.Invoke();
    public void HoverExit() => onHoverExit?.Invoke();
    public void Interact() => onInteract?.Invoke();
}
