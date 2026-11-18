using UnityEngine;

public class BatteryPickup : MonoBehaviour, IInteractable
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] grabSounds;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    [Header("Tooltip")]
    public string tooltipText = "+Battery";
    public Color tooltipColor = Color.green;

    private void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public void Interact()
    {
        if (SimpleInventory.Instance != null)
        {
            SimpleInventory.Instance.GiveBattery();
            PlayGrabSound();
        }
        else
        {
            Debug.LogWarning("No SimpleInventory found on Player!");
        }
    }

    private void PlayGrabSound()
    {
        if (audioSource == null || grabSounds == null || grabSounds.Length == 0)
            return;

        AudioClip clip = grabSounds[Random.Range(0, grabSounds.Length)];
        if (clip != null)
        {
            audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
            audioSource.PlayOneShot(clip);
        }
    }

    public string GetTooltip()
    {
        return tooltipText;
    }

    public Color GetTooltipColor()
    {
        return tooltipColor;
    }
}