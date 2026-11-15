using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class PlayerCharacter3D : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float gravity = -9.81f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 0.15f;
    public Transform playerCamera;

    [Header("Head Bob & Footsteps")]
    public float bobFrequency = 10f;
    public float bobAmplitude = 0.05f;
    public AudioClip[] footstepClips;
    [Range(0f, 0.2f)] public float pitchVariation = 0.1f;

    [Header("Hunger")]
    public PlayerHunger playerHunger;

    private CharacterController controller;
    private AudioSource audioSource;
    private InputAction moveAction;
    private InputAction lookAction;

    private float verticalRotation = 0f;
    private float verticalVelocity = 0f;
    private float defaultCameraY;
    private float bobTimer = 0f;
    private bool stepTriggered = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        moveAction = InputSystem.actions.FindAction("Move");
        lookAction = InputSystem.actions.FindAction("Look");

        if (playerCamera != null)
        {
            defaultCameraY = playerCamera.localPosition.y;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovement();
    }

    void HandleMouseLook()
    {
        Vector2 lookInput = lookAction.ReadValue<Vector2>() * mouseSensitivity;

        verticalRotation -= lookInput.y;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);
        playerCamera.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

        transform.Rotate(Vector3.up * lookInput.x);
    }

    void HandleMovement()
    {
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        Vector3 horizontalMove = (transform.right * moveInput.x + transform.forward * moveInput.y) * moveSpeed;

        Vector3 finalVelocity = horizontalMove + (Vector3.up * verticalVelocity);
        controller.Move(finalVelocity * Time.deltaTime);

        HandleHeadBobAndFootsteps(moveInput);
    }

    void HandleHeadBobAndFootsteps(Vector2 moveInput)
    {
        if (playerCamera == null) return;

        bool isWalking = controller.isGrounded && moveInput.sqrMagnitude > 0.01f;

        if (isWalking)
        {
            bobTimer += Time.deltaTime * bobFrequency;
            float sinWave = Mathf.Sin(bobTimer);

            Vector3 camPos = playerCamera.localPosition;
            camPos.y = defaultCameraY + sinWave * bobAmplitude;
            playerCamera.localPosition = camPos;

            if (sinWave < -0.9f && !stepTriggered)
            {
                PlayFootstep();
                stepTriggered = true;
            }
            else if (sinWave > 0f)
            {
                stepTriggered = false;
            }
        }
        else
        {
            bobTimer = 0f;
            stepTriggered = false;

            Vector3 camPos = playerCamera.localPosition;
            camPos.y = Mathf.Lerp(camPos.y, defaultCameraY, Time.deltaTime * 10f);
            playerCamera.localPosition = camPos;
        }
    }

    void PlayFootstep()
    {
        if (footstepClips == null || footstepClips.Length == 0) return;

        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
        audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        audioSource.PlayOneShot(clip);
    }
}