using Echo.Interactions;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AlarmDecoy : Interactable
{
    public enum State { Unarmed, Armed, Ringing, Spent }
    [Min(0f)] public float noiseRadius = 16f;
    [Min(0.1f)] public float ringDuration = 5f;
    [Min(0.1f)] public float pulseInterval = 1f;
    public State CurrentState { get; private set; }
    public override string Prompt => CurrentState == State.Armed ? "E — забрати й вимкнути будильник" : "E — забрати будильник";

    private PlayerSoundTool owner;
    private AudioSource audioSource;
    private float ringAt;
    private float stopAt;
    private float nextPulse;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    public void Arm(PlayerSoundTool tool, float delay)
    {
        if (CurrentState != State.Unarmed || tool == null) return;
        owner = tool;
        ringAt = Time.time + Mathf.Max(0.1f, delay);
        CurrentState = State.Armed;
    }

    void Update()
    {
        if (Echo.Menu.GameMenu.BlocksPlayerInput || Time.timeScale <= 0f) return;
        if (CurrentState == State.Armed && Time.time >= ringAt)
        {
            CurrentState = State.Ringing;
            stopAt = Time.time + Mathf.Max(0.1f, ringDuration);
            nextPulse = Time.time;
            if (audioSource.clip != null) { audioSource.loop = true; audioSource.Play(); }
        }
        if (CurrentState != State.Ringing) return;
        if (Time.time >= stopAt)
        {
            audioSource.Stop();
            CurrentState = State.Spent;
            return;
        }
        if (Time.time < nextPulse) return;
        NoiseSystem.Emit(transform.position, noiseRadius, gameObject, NoiseSystem.Kind.Decoy);
        nextPulse = Time.time + Mathf.Max(0.1f, pulseInterval);
    }

    public override bool CanInteract(PlayerInteractor player) => base.CanInteract(player) && owner != null &&
        owner.CanUse && player.gameObject == owner.gameObject && CurrentState != State.Ringing;

    public override void Interact(PlayerInteractor player)
    {
        if (!CanInteract(player)) return;
        owner.Recover(this);
        Destroy(gameObject);
    }

    void OnDisable()
    {
        if (audioSource != null) audioSource.Stop();
        if (CurrentState == State.Armed || CurrentState == State.Ringing) CurrentState = State.Spent;
    }
}
