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

    [Header("Gameplay noise radius (metres)")]
    [Min(0f)] public float walkNoiseRadius = 6f;
    [Min(0f)] public float runNoiseRadius = 12f;
    [Min(0f)] public float crouchNoiseRadius = 2f;
    [Min(.1f)] public float hazardousStepDistance=.35f;

    private CharacterController controller;
    private FPSController movement;
    private AudioSource audioSource;
    private float timer;
    private Vector3 previousPosition;
    private float hazardousTravel,lastStepTime;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        movement = GetComponent<FPSController>();
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        previousPosition=transform.position;
    }

    void OnDisable() { timer = 0f; }

    void Update()
    {
        Vector3 travel=transform.position-previousPosition;previousPosition=transform.position;travel.y=0;
        if(travel.magnitude>2){hazardousTravel=0;timer=0;travel=Vector3.zero;}
        if (Echo.Menu.GameMenu.BlocksPlayerInput || Time.timeScale <= 0f)
        {
            timer = 0f;
            hazardousTravel=0;
            return;
        }
        Vector3 velocity = controller.velocity;
        velocity.y = 0f;
        bool grounded=controller.isGrounded;
        // Changing CharacterController height can clear isGrounded for a frame while
        // crouching. A short feet probe keeps real surface steps from becoming silent.
        if(controller.enabled&&!grounded)
        {
            var feet=controller.bounds.center;feet.y=controller.bounds.min.y+.12f;
            grounded=Physics.Raycast(feet,Vector3.down,.24f+controller.skinWidth,surfaceLayers,QueryTriggerInteraction.Ignore);
        }
        if (!controller.enabled || !grounded || velocity.sqrMagnitude <= 0.01f)
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
        Vector3 surfaceProbe=controller.bounds.center;surfaceProbe.y=controller.bounds.min.y+controller.skinWidth+.1f;
        bool hazardous=false;
        if(Physics.Raycast(surfaceProbe,Vector3.down,out var floorHit,raycastDistance,surfaceLayers,QueryTriggerInteraction.Ignore))
        {var current=floorHit.collider.GetComponentInParent<SurfaceType>();hazardous=current!=null&&(current.kind==SurfaceType.Kind.Glass||current.kind==SurfaceType.Kind.Creaky);}
        if(hazardous)hazardousTravel+=travel.magnitude;else hazardousTravel=0;
        timer += Time.deltaTime;
        if (timer < Mathf.Max(0.01f, interval) && (!hazardous||hazardousTravel<hazardousStepDistance||Time.time-lastStepTime<.22f)) return;
        timer = 0f;
        hazardousTravel=0;lastStepTime=Time.time;

        AudioClip clip = defaultFootstepSound;
        float surfaceMultiplier = 1f;
        float noiseMultiplier = 1f;
        float minimumRadius=0f;
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
                        case SurfaceType.Kind.Creaky: surfaceClip=Echo.Audio.PrototypeAudio.Clip(Echo.Audio.PrototypeAudio.Cue.Creak);break;
                        case SurfaceType.Kind.Stone: surfaceClip=Echo.Audio.PrototypeAudio.Clip(Echo.Audio.PrototypeAudio.Cue.Click);break;
                    }
                }
                if (surfaceClip != null) clip = surfaceClip;
                surfaceMultiplier = surface.loudnessMultiplier;
                noiseMultiplier = surface.noiseMultiplier;
                minimumRadius=surface.minimumNoiseRadius;
            }
        }

        float movementVolume = isCrouching ? 0.2f : isRunning ? 1f : 0.5f;
        if (clip != null)
            audioSource.PlayOneShot(clip, Mathf.Clamp01(movementVolume * surfaceMultiplier));

        // AI hearing still works when a clip is missing or the player mutes audio.
        float noiseRadius = isCrouching ? crouchNoiseRadius : isRunning ? runNoiseRadius : walkNoiseRadius;
        NoiseSystem.Emit(transform.position, Mathf.Max(minimumRadius,noiseRadius * Mathf.Max(0f, noiseMultiplier)), gameObject,NoiseSystem.Kind.Footstep);
    }
}
