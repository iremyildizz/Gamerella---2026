using UnityEngine;
using UnityEngine.UI;

public class Fridge : MonoBehaviour, IInteractable
{
    [Header("Door Hinge Settings")]
    public Transform doorHinge;

    [Tooltip("Angle in degrees to swing open")]
    public float openAngle = 100f;
    public float swingSpeed = 6f;

    [Header("Eating Hold Settings")]
    [Tooltip("Drag the LoadingRing UI Image here")]
    public Image loadingRing;
    public float eatDuration = 3f;

    [Header("Audio Sources")]
    [Tooltip("AudioSource for door open/close sounds")]
    public AudioSource doorAudioSource;
    [Tooltip("Separate AudioSource for the looping eating sound")]
    public AudioSource eatingAudioSource;

    [Header("Audio Clips")]
    public AudioClip openSound;
    public AudioClip closeSound;
    public AudioClip eatingSound;

    [Header("Player Reference")]
    public PlayerCharacter3D player;

    private Quaternion closedRotation;
    private Quaternion openRotation;
    private bool isOpen = false;
    private bool isHolding = false;
    private bool hasEatenThisOpen = false;
    private float eatProgress = 0f;

    void Start()
    {
        if (doorAudioSource != null)
        {
            doorAudioSource.playOnAwake = false;
        }

        if (eatingAudioSource != null)
        {
            eatingAudioSource.playOnAwake = false;
            eatingAudioSource.loop = true;
        }

        if (doorHinge == null)
        {
            Debug.LogWarning($"DoorHinge not assigned on {gameObject.name}");
            return;
        }

        if (player == null)
            player = FindAnyObjectByType<PlayerCharacter3D>();

        closedRotation = doorHinge.localRotation;
        openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);

        ResetLoadingRing();
    }

    void Update()
    {
        if (doorHinge != null)
        {
            Quaternion targetRotation = isOpen ? openRotation : closedRotation;
            doorHinge.localRotation = Quaternion.Slerp(
                doorHinge.localRotation,
                targetRotation,
                Time.deltaTime * swingSpeed
            );
        }

        HandleEatingProgress();
    }

    private void HandleEatingProgress()
    {
        if (isOpen && isHolding && !hasEatenThisOpen)
        {
            if (eatingAudioSource != null && !eatingAudioSource.isPlaying && eatingSound != null)
            {
                eatingAudioSource.clip = eatingSound;
                eatingAudioSource.Play();
            }

            eatProgress += Time.deltaTime / eatDuration;

            if (loadingRing != null)
            {
                loadingRing.enabled = true;
                loadingRing.fillAmount = Mathf.Clamp01(eatProgress);
            }

            if (eatProgress >= 1f)
            {
                hasEatenThisOpen = true;
                StopEatingSound();
                ResetLoadingRing();
                OnFinishedEating();
            }
        }
        else if (eatProgress > 0f)
        {
            StopEatingSound();
            ResetLoadingRing();
        }
    }

    private void OnFinishedEating()
    {
        // Use the player's reference, or find the hunger component if it's empty.
        PlayerHunger hunger = player != null ? player.playerHunger : null;
        if (hunger == null) hunger = FindAnyObjectByType<PlayerHunger>();

        if (hunger != null)
            hunger.ReplenishHunger();
        else
            Debug.LogWarning("Fridge: no PlayerHunger found on the player", this);
    }

    public void Interact()
    {
        if (!isOpen && doorAudioSource != null && openSound != null)
        {
            doorAudioSource.PlayOneShot(openSound);
        }

        isOpen = true;
    }

    public void InteractHold(bool holding)
    {
        isHolding = holding;

        if (!holding)
        {
            hasEatenThisOpen = false;
            StopEatingSound();
        }
    }

    public void OnHoverExit()
    {
        if (isOpen && doorAudioSource != null && closeSound != null)
        {
            doorAudioSource.PlayOneShot(closeSound);
        }

        isOpen = false;
        isHolding = false;
        hasEatenThisOpen = false;
        StopEatingSound();
        ResetLoadingRing();
    }

    private void StopEatingSound()
    {
        if (eatingAudioSource != null && eatingAudioSource.isPlaying)
        {
            eatingAudioSource.Stop();
        }
    }

    private void ResetLoadingRing()
    {
        eatProgress = 0f;
        if (loadingRing != null)
        {
            loadingRing.fillAmount = 0f;
            loadingRing.enabled = false;
        }
    }
}