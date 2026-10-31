using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class Footsteps : MonoBehaviour
{
    public AudioClip footstepSound;

    public float walkInterval = 0.5f;
    public float runInterval = 0.3f;
    public float crouchInterval = 0.8f;

    private CharacterController controller;
    private AudioSource audioSource;
    private float timer;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    void Update()
    {
        if (Keyboard.current == null)
            return;

        Vector3 velocity = controller.velocity;
        velocity.y = 0;

        bool isMoving = velocity.magnitude > 0.1f;

        if (!isMoving || !controller.isGrounded)
        {
            timer = 0;
            return;
        }

        bool isCrouching = Keyboard.current.leftCtrlKey.isPressed;
        bool isRunning = Keyboard.current.leftShiftKey.isPressed && !isCrouching;

        float interval = isCrouching ? crouchInterval :
                         isRunning ? runInterval : walkInterval;

        audioSource.volume = isCrouching ? 0.2f :
                             isRunning ? 1f : 0.5f;

        timer += Time.deltaTime;

        if (timer >= interval)
        {
            timer = 0;

            if (footstepSound != null)
                audioSource.PlayOneShot(footstepSound);
        }
    }
}