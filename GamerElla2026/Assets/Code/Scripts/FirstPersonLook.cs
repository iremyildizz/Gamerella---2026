using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonLook : MonoBehaviour {
    [Header("Input")]
    public InputActionReference lookAction;

    [Header("Settings")]
    public float mouseSensitivity = 0.15f;
    public Transform playerBody;

    private float xRotation = 0f;

    void OnEnable() {
        lookAction.action.Enable();
    }

    void OnDisable() {
        lookAction.action.Disable();
    }

    void Start() {
      Cursor.lockState = CursorLockMode.Locked;
      Cursor.visible = false;
    }

    void Update() {
        Vector2 lookInput = lookAction.action.ReadValue<Vector2>();

        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }
}
