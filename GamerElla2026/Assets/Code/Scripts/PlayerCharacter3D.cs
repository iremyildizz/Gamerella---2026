using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerCharacter3D : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float gravity = -9.81f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 0.15f;
    public Transform playerCamera;

    private CharacterController controller;
    private InputAction moveAction;
    private InputAction lookAction;
    private float verticalRotation = 0f;
    private float verticalVelocity = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        moveAction = InputSystem.actions.FindAction("Move");
        lookAction = InputSystem.actions.FindAction("Look");

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
    }
}