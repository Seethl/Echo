
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    public Transform playerCamera;

    public float walkSpeed = 6f;
    public float runSpeed = 10f;
    public float mouseSensitivity = 0.1f;

    private CharacterController controller;
    private float verticalVelocity;
    private float cameraRotation;

    private const float gravity = -20f;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (Keyboard.current == null || Mouse.current == null)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Огляд мишкою
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 mouse = Mouse.current.delta.ReadValue();

            transform.Rotate(Vector3.up * mouse.x * mouseSensitivity);

            cameraRotation -= mouse.y * mouseSensitivity;
            cameraRotation = Mathf.Clamp(cameraRotation, -85f, 85f);

            playerCamera.localRotation = Quaternion.Euler(cameraRotation, 0, 0);
        }

        // Рух
        Vector2 input = Vector2.zero;

        if (Keyboard.current.wKey.isPressed) input.y += 1;
        if (Keyboard.current.sKey.isPressed) input.y -= 1;
        if (Keyboard.current.dKey.isPressed) input.x += 1;
        if (Keyboard.current.aKey.isPressed) input.x -= 1;

        input = Vector2.ClampMagnitude(input, 1f);

        bool isRunning = Keyboard.current.leftShiftKey.isPressed;
        float speed = isRunning ? runSpeed : walkSpeed;

        Vector3 movement =
            transform.right * input.x +
            transform.forward * input.y;

        // Гравітація
        if (controller.isGrounded && verticalVelocity < 0)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;

        movement *= speed;
        movement.y = verticalVelocity;

        controller.Move(movement * Time.deltaTime);
    }
}
