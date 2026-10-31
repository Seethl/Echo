using Echo.Menu;

using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    public Transform playerCamera;

    public float walkSpeed = 6f;
    public float runSpeed = 10f;
    public float mouseSensitivity = 0.1f;
    public float crouchSpeed = 1.5f;

    public float standingHeight = 1.8f;
    public float crouchingHeight = 1.0f;

    public float standingCameraY = 0.65f;
    public float crouchingCameraY = 0.2f;

    private CharacterController controller;
    private float verticalVelocity;
    private float cameraRotation;

    private const float gravity = -20f;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (!GameMenu.BlocksPlayerInput)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update()
    {
        if (GameMenu.BlocksPlayerInput || Time.timeScale == 0f)
            return;

        if (Keyboard.current == null || Mouse.current == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Огляд мишкою
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 mouse = Mouse.current.delta.ReadValue();

            transform.Rotate(Vector3.up * mouse.x * mouseSensitivity * GameSettings.SensitivityMultiplier);

            cameraRotation -= mouse.y * mouseSensitivity * GameSettings.SensitivityMultiplier;
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
        bool isCrouching = Keyboard.current.leftCtrlKey.isPressed;

        float speed = isCrouching ? crouchSpeed :
                      isRunning ? runSpeed : walkSpeed;

        float targetHeight = isCrouching
    ? crouchingHeight
    : standingHeight;

        controller.height = Mathf.Lerp(
            controller.height,
            targetHeight,
            Time.deltaTime * 8f
        );

        controller.center = new Vector3(
            0,
            (controller.height - standingHeight) / 2f,
            0
        );

        Vector3 cameraPosition = playerCamera.localPosition;

        cameraPosition.y = Mathf.Lerp(
            cameraPosition.y,
            isCrouching ? crouchingCameraY : standingCameraY,
            Time.deltaTime * 8f
        );

        playerCamera.localPosition = cameraPosition;

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
