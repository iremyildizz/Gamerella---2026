using UnityEngine;

public class TestInteractable : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        Debug.Log($"Interacted with {gameObject.name}");
    }

    public string GetTooltip()
    {
        return "This is a test interactable object.";
    }

    public Color GetTooltipColor()
    {
        return Color.green;
    }
}