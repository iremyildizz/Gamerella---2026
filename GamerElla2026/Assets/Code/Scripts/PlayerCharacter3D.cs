using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour {

    public float moveSpeed = 5f;
    private InputAction moveAction;

    void Start() {
        moveAction = InputSystem.actions.FindAction("Move");
    }

    void Update() {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        transform.Translate(new Vector3(moveInput.x, 0, moveInput.y) * Time.deltaTime * moveSpeed);
    }
}
