using UnityEngine;
using UnityEngine.Events;

/// <summary>Hearing memory, separate from locomotion. Never follows a source transform.</summary>
[RequireComponent(typeof(NoiseListener))]
public class EnemySuspicion : MonoBehaviour
{
    public enum State { Calm, Wary, Alarmed }
    [Range(0.01f, 1f)] public float investigationThreshold = 0.6f;
    [Range(0f, 1f)] public float gainPerSound = 0.4f;
    [Range(0f, 1f)] public float loudSoundThreshold = 0.8f;
    [Min(0f)] public float calmDelay = 4f;
    [Min(0.01f)] public float decayPerSecond = 0.12f;
    [Min(0f)] public float requestCooldown = 0.35f;
    public bool orientWhenIdle = true;
    [Min(0f)] public float orientSpeed = 90f;
    public UnityEvent<State> onStateChanged = new UnityEvent<State>();
    public UnityEvent<float> onSuspicionChanged = new UnityEvent<float>();
    public UnityEvent<Vector3> onNoticedNoise = new UnityEvent<Vector3>();
    public UnityEvent<Vector3> onInvestigationRequested = new UnityEvent<Vector3>();

    public float Amount { get; private set; }
    public State CurrentState { get; private set; }
    public bool HasMemory => Amount > 0f;
    public Vector3 LastKnownPosition { get; private set; }
    private NoiseListener ears;
    private float lastNoiseAt;
    private float lastDecayAt;
    private float requestAfter;

    void Awake() { ears = GetComponent<NoiseListener>(); }
    void OnEnable() { ears.SoundPerceived += Notice; }
    void OnDisable()
    {
        ears.SoundPerceived -= Notice;
        // Do not retain an invisible priority/memory across deactivation or pooling.
        Amount = 0f;
        CurrentState = State.Calm;
        LastKnownPosition = Vector3.zero;
        requestAfter = 0f;
    }

    private void Notice(NoiseListener.Hearing hearing)
    {
        bool decisive = hearing.Noise.Category == NoiseSystem.Kind.Decoy || hearing.Audibility >= loudSoundThreshold;
        float threshold = Mathf.Clamp(investigationThreshold, 0.01f, 1f);
        LastKnownPosition = hearing.Noise.Position;
        lastNoiseAt = lastDecayAt = Time.time;
        Amount = Mathf.Clamp01(Amount + hearing.Audibility * Mathf.Clamp01(gainPerSound));
        if (decisive) Amount = Mathf.Max(Amount, threshold);
        NotifyAmount();
        if (!isActiveAndEnabled || !HasMemory) return;
        onNoticedNoise.Invoke(LastKnownPosition);
        if (!isActiveAndEnabled || Amount < threshold || (!decisive && Time.time < requestAfter)) return;
        requestAfter = Time.time + Mathf.Max(0f, requestCooldown);
        var investigator = GetComponent<EnemyInvestigator>();
        if (investigator != null && investigator.isActiveAndEnabled) investigator.Investigate(LastKnownPosition);
        onInvestigationRequested.Invoke(LastKnownPosition);
    }

    void Update()
    {
        var threat = GetComponent<EnemyThreat>();
        if (threat != null && !threat.CanHear) { if (HasMemory) ClearMemory(); return; }
        if (Echo.Menu.GameMenu.BlocksPlayerInput || Time.timeScale <= 0f || !HasMemory) return;
        float decayStarts = Mathf.Max(lastDecayAt, lastNoiseAt + Mathf.Max(0f, calmDelay));
        if (Time.time > decayStarts)
        {
            Amount = Mathf.Max(0f, Amount - Mathf.Max(0.01f, decayPerSecond) * (Time.time - decayStarts));
            lastDecayAt = Time.time;
            if (!HasMemory) LastKnownPosition = Vector3.zero;
            NotifyAmount();
        }
        if (!isActiveAndEnabled || !orientWhenIdle || CurrentState != State.Wary) return;
        var investigator = GetComponent<EnemyInvestigator>();
        if (investigator != null && investigator.CurrentState != EnemyInvestigator.State.Idle) return;
        Vector3 direction = LastKnownPosition - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction),
                Mathf.Max(0f, orientSpeed) * Time.deltaTime);
    }

    public void ClearMemory()
    {
        Amount = 0f;
        LastKnownPosition = Vector3.zero;
        requestAfter = 0f;
        ears.ClearAttention();
        NotifyAmount();
    }

    private void NotifyAmount()
    {
        State next = Amount <= 0f ? State.Calm : Amount >= Mathf.Clamp(investigationThreshold, 0.01f, 1f) ? State.Alarmed : State.Wary;
        if (CurrentState != next)
        {
            CurrentState = next;
            onStateChanged.Invoke(next);
        }
        if (isActiveAndEnabled) onSuspicionChanged.Invoke(Amount);
    }
}
