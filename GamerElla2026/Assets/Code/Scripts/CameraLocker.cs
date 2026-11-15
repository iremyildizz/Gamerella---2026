using System.Collections;
using UnityEngine;
using TMPro;

public class CameraLocker : MonoBehaviour, IInteractable
{
    [Header("Hinge & Swing Settings")]
    [SerializeField] private Transform doorHinge;
    [SerializeField] private float openAngleY = -105f;
    [SerializeField] private float swingSpeed = 4f;

    [Header("Auto-Close & Audio")]
    [SerializeField] private float autoCloseDelay = 2.5f;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] openSounds;
    [SerializeField] private AudioClip[] closeSounds;
    [SerializeField] private float closeSoundDelay = 0.25f;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    [Header("Battery Requirement & UI Prompt")]
    [SerializeField] private bool requireBattery = true;
    [SerializeField] private TextMeshProUGUI statusPromptText;
    [SerializeField] private string lockedMessage = "You need a battery to open this locker.";
    [SerializeField] private float messageDuration = 2.5f;

    private Quaternion closedRotation;
    private Quaternion openRotation;
    private bool isOpen = false;
    private Coroutine autoCloseCoroutine;
    private Coroutine promptCoroutine;
    private int lastOpenIndex = -1;
    private int lastCloseIndex = -1;

    private void Awake()
    {
        if (statusPromptText != null)
            statusPromptText.gameObject.SetActive(false);
    }

    private void Start()
    {
        if (doorHinge == null)
            doorHinge = transform;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        closedRotation = doorHinge.localRotation;
        openRotation = closedRotation * Quaternion.Euler(0f, openAngleY, 0f);
    }

    private void Update()
    {
        Quaternion targetRot = isOpen ? openRotation : closedRotation;
        doorHinge.localRotation = Quaternion.Slerp(doorHinge.localRotation, targetRot, swingSpeed * Time.deltaTime);
    }

    public void Interact()
    {
        if (isOpen) return;

        if (requireBattery)
        {
            if (SimpleInventory.Instance != null && SimpleInventory.Instance.HasBattery)
            {
                SimpleInventory.Instance.RemoveBattery();
                HidePromptImmediately();
                OpenLockerWithAutoClose();
            }
            else
            {
                ShowLockedMessage();
            }
        }
        else
        {
            HidePromptImmediately();
            OpenLockerWithAutoClose();
        }
    }

    private void ShowLockedMessage()
    {
        if (statusPromptText == null) return;

        if (promptCoroutine != null)
            StopCoroutine(promptCoroutine);

        SimpleInventory.Instance?.SetSuppressedByPrompt(true);

        statusPromptText.text = lockedMessage;
        statusPromptText.gameObject.SetActive(true);
        promptCoroutine = StartCoroutine(HidePromptAfterDelay());
    }

    public void HidePromptImmediately()
    {
        if (promptCoroutine != null)
        {
            StopCoroutine(promptCoroutine);
            promptCoroutine = null;
        }

        if (statusPromptText != null)
            statusPromptText.gameObject.SetActive(false);

        SimpleInventory.Instance?.SetSuppressedByPrompt(false);
    }

    private IEnumerator HidePromptAfterDelay()
    {
        yield return new WaitForSecondsRealtime(messageDuration);

        if (statusPromptText != null)
            statusPromptText.gameObject.SetActive(false);

        SimpleInventory.Instance?.SetSuppressedByPrompt(false);
        promptCoroutine = null;
    }

    private void OpenLockerWithAutoClose()
    {
        isOpen = true;
        PlayRandomClip(openSounds, ref lastOpenIndex);

        if (autoCloseCoroutine != null)
            StopCoroutine(autoCloseCoroutine);

        autoCloseCoroutine = StartCoroutine(AutoCloseRoutine());
    }

    private IEnumerator AutoCloseRoutine()
    {
        yield return new WaitForSeconds(autoCloseDelay);

        isOpen = false;

        if (closeSoundDelay > 0f)
            yield return new WaitForSeconds(closeSoundDelay);

        PlayRandomClip(closeSounds, ref lastCloseIndex);
        autoCloseCoroutine = null;
    }

    private void PlayRandomClip(AudioClip[] clips, ref int lastIndex)
    {
        if (audioSource == null || clips == null || clips.Length == 0)
            return;

        int index = Random.Range(0, clips.Length);

        if (clips.Length > 1 && index == lastIndex)
            index = (index + 1) % clips.Length;

        lastIndex = index;

        if (clips[index] != null)
        {
            audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
            audioSource.PlayOneShot(clips[index]);
        }
    }
}