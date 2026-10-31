
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class Footsteps : MonoBehaviour
{
    public AudioClip defaultFootstepSound;

    public float walkInterval = 0.5f;
    public float runInterval = 0.3f;
    public float crouchInterval = 0.8f;

    public float raycastDistance = 2f;

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
            timer = 0f;
            return;
        }

        bool isCrouching = Keyboard.current.leftCtrlKey.isPressed;
        bool isRunning = Keyboard.current.leftShiftKey.isPressed && !isCrouching;

        float interval = isCrouching ? crouchInterval :
                         isRunning ? runInterval : walkInterval;

        timer += Time.deltaTime;

        if (timer < interval)
            return;

        timer = 0f;

        AudioClip clip = defaultFootstepSound;
        float surfaceMultiplier = 1f;

        Vector3 rayOrigin = transform.position + Vector3.up * 0.3f;

        if (Physics.Raycast(rayOrigin, Vector3.down,
            out RaycastHit hit, raycastDistance))
        {
            SurfaceType surface =
                hit.collider.GetComponentInParent<SurfaceType>();

            if (surface != null)
            {
                if (surface.footstepSound != null)
                    clip = surface.footstepSound;

                surfaceMultiplier = surface.loudnessMultiplier;
            }
        }

        float movementVolume = isCrouching ? 0.2f :
                               isRunning ? 1f : 0.5f;

        float finalVolume =
            Mathf.Clamp01(movementVolume * surfaceMultiplier);

        if (clip != null)
            audioSource.PlayOneShot(clip, finalVolume);
    }
}
