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
    public float eatDuration = 5f;

    private Quaternion closedRotation;
    private Quaternion openRotation;
    private bool isOpen = false;
    private bool isHolding = false;
    private bool hasEatenThisOpen = false;
    private float eatProgress = 0f;

    void Start()
    {
        if (doorHinge == null)
        {
            Debug.LogWarning($"DoorHinge not assigned on {gameObject.name}");
            return;
        }

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
            eatProgress += Time.deltaTime / eatDuration;

            if (loadingRing != null)
            {
                loadingRing.enabled = true;
                loadingRing.fillAmount = Mathf.Clamp01(eatProgress);
            }

            if (eatProgress >= 1f)
            {
                hasEatenThisOpen = true;
                ResetLoadingRing();
                OnFinishedEating();
            }
        }
        else if (eatProgress > 0f)
        {
            ResetLoadingRing();
        }
    }

    private void OnFinishedEating()
    {
        Debug.Log("Finished eating");
    }

    public void Interact()
    {
        isOpen = true;
    }

    public void InteractHold(bool holding)
    {
        isHolding = holding;

        if (!holding)
        {
            hasEatenThisOpen = false;
        }
    }

    public void OnHoverExit()
    {
        isOpen = false;
        isHolding = false;
        hasEatenThisOpen = false;
        ResetLoadingRing();
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