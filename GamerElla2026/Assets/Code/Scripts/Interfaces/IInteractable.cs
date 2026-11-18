using UnityEngine;

public interface IInteractable
{
    void Interact();
    void InteractHold(bool isHolding) { }
    void OnHoverEnter() { }
    void OnHoverExit() { }

    float GetCooldown() { return 0.0f; }
    string GetTooltip() { return null; }
    Color GetTooltipColor() { return Color.white; }
}