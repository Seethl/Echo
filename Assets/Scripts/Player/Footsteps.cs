using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class Footsteps : MonoBehaviour
{
    [FormerlySerializedAs("footstepSound")]
    public AudioClip defaultFootstepSound;
    public AudioClip woodFootstepSound;
    public AudioClip carpetFootstepSound;
    public AudioClip glassFootstepSound;

    public float walkInterval = 0.5f;
    public float runInterval = 0.3f;
    public float crouchInterval = 0.8f;
    public float raycastDistance = 2f;
    public LayerMask surfaceLayers = Physics.DefaultRaycastLayers;

    private CharacterController controller;
    private FPSController movement;
    private AudioSource audioSource;
    private float timer;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        movement = GetComponent<FPSController>();
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    void OnDisable() { timer = 0f; }

    void Update()
    {
        Vector3 velocity = controller.velocity;
        velocity.y = 0f;
        if (!controller.enabled || !controller.isGrounded || velocity.sqrMagnitude <= 0.01f)
        {
            timer = 0f;
            return;
        }

        // Use the controller's actual movement mode; retain support for other controllers.
        bool isCrouching = movement != null ? movement.IsCrouching :
            Keyboard.current != null && Keyboard.current.leftCtrlKey.isPressed;
        bool isRunning = movement != null ? movement.IsRunning :
            Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed && !isCrouching;
        float interval = isCrouching ? crouchInterval : isRunning ? runInterval : walkInterval;
        timer += Time.deltaTime;
        if (timer < Mathf.Max(0.01f, interval)) return;
        timer = 0f;

        AudioClip clip = defaultFootstepSound;
        float surfaceMultiplier = 1f;
        // Start just above the feet, including while crouching or on a scaled player.
        Vector3 rayOrigin = controller.bounds.center;
        rayOrigin.y = controller.bounds.min.y + controller.skinWidth + 0.1f;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit,
            raycastDistance, surfaceLayers, QueryTriggerInteraction.Ignore))
        {
            SurfaceType surface = hit.collider.GetComponentInParent<SurfaceType>();
            if (surface != null)
            {
                AudioClip surfaceClip = surface.footstepSound;
                if (surfaceClip == null)
                {
                    switch (surface.kind)
                    {
                        case SurfaceType.Kind.Wood: surfaceClip = woodFootstepSound; break;
                        case SurfaceType.Kind.Carpet: surfaceClip = carpetFootstepSound; break;
                        case SurfaceType.Kind.Glass: surfaceClip = glassFootstepSound; break;
                    }
                }
                if (surfaceClip != null) clip = surfaceClip;
                surfaceMultiplier = surface.loudnessMultiplier;
            }
        }

        float movementVolume = isCrouching ? 0.2f : isRunning ? 1f : 0.5f;
        if (clip != null)
            audioSource.PlayOneShot(clip, Mathf.Clamp01(movementVolume * surfaceMultiplier));
    }
}
