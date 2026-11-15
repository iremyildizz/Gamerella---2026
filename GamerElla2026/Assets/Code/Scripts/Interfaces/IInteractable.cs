public interface IInteractable
{
    void Interact();
    void InteractHold(bool isHolding) { }
    void OnHoverEnter() { }
    void OnHoverExit() { }

    float GetCooldown() { return 0.0f; }
}