using UnityEngine;
using UnityEngine.Events;

/// <summary>Attach to an enemy; connect onNoiseHeard to its investigation behaviour.</summary>
public class NoiseListener : MonoBehaviour
{
    public readonly struct Hearing
    {
        public readonly NoiseSystem.Noise Noise;
        public readonly float Audibility;
        public readonly float Strength;
        public Hearing(NoiseSystem.Noise noise, float audibility, float strength)
        { Noise = noise; Audibility = audibility; Strength = strength; }
    }

    public event System.Action<Hearing> SoundPerceived;
    [Min(0f), Tooltip("1 = normal hearing; 2 = twice the emitted radius.")]
    public float hearingSensitivity = 1f;
    public UnityEvent<Vector3> onNoiseHeard = new UnityEvent<Vector3>();
    public bool logHeardNoise;
    [Min(0f)] public float decoyAttentionSeconds = 2f;
    public bool useOcclusion = true;
    public LayerMask obstacleLayers = Physics.DefaultRaycastLayers;
    [Range(0f, 1f)] public float defaultObstacleTransmission = 0.45f;
    public Vector3 earOffset = new Vector3(0f, 0.9f, 0f);

    public bool HasHeardNoise { get; private set; }
    public Vector3 LastHeardPosition { get; private set; }
    public float LastHeardTime { get; private set; }
    public NoiseSystem.Kind LastHeardCategory { get; private set; }
    public float LastAudibility { get; private set; }
    public float LastStrength => LastAudibility;
    public NoiseSystem.Kind LastKind => LastHeardCategory;
    private float attentionUntil;
    private float attentionStrength;

    void OnEnable() { NoiseSystem.Emitted += Hear; }
    void OnDisable()
    {
        NoiseSystem.Emitted -= Hear;
        ClearAttention();
    }

    public void ClearAttention()
    {
        attentionUntil = 0f;
        attentionStrength = 0f;
    }

    private void Hear(NoiseSystem.Noise noise)
    {
        if (Time.timeScale <= 0f || noise.Category == NoiseSystem.Kind.Ambient || noise.Category == NoiseSystem.Kind.Enemy) return;
        var manor = Echo.Manor.ManorProgress.Active;
        if (manor != null && (!manor.Has("hall.open") || manor.IsTutorialPosition(noise.Position))) return;
        var threat = GetComponent<EnemyThreat>();
        if (threat != null && !threat.CanHear) return;
        if (noise.Source != null &&
            (noise.Source.transform == transform || transform.IsChildOf(noise.Source.transform))) return;

        Vector3 earPosition = transform.TransformPoint(earOffset);
        float radius = noise.Radius * Mathf.Max(0f, hearingSensitivity) * noise.Intensity;
        radius *= NoiseMaskingSource.GetMultiplier(noise.Position, earPosition, transform,
            useOcclusion ? (int)obstacleLayers : 0, defaultObstacleTransmission);
        if (Vector3.Distance(noise.Position, earPosition) > 2.2f)
            radius *= 1f - Echo.Audio.NoiseMaskZone.CoverAt(earPosition) * 0.8f;
        if (useOcclusion)
            radius *= SoundPropagation.Transmission(noise.Position, earPosition,
                noise.Source != null ? noise.Source.transform : null, transform, obstacleLayers, defaultObstacleTransmission);
        float distance = Vector3.Distance(noise.Position, earPosition);
        if (radius <= 0f || distance > radius) return;

        float strength = radius / Mathf.Max(0.5f, distance);
        // A quiet step must not instantly cancel a ringing distraction. Nearby loud
        // actions still break attention, so the clock never makes the player invulnerable.
        if (Time.time < attentionUntil && noise.Category != NoiseSystem.Kind.Decoy &&
            strength < attentionStrength * 1.25f) return;

        HasHeardNoise = true;
        LastHeardPosition = noise.Position;
        LastHeardTime = Time.time;
        LastHeardCategory = noise.Category;
        LastAudibility = Mathf.Clamp(1f - distance / radius, 0.05f, 1f);
        attentionUntil = noise.Category == NoiseSystem.Kind.Decoy ? Time.time + decoyAttentionSeconds : 0f;
        attentionStrength = strength;
        if (logHeardNoise) Debug.Log($"{name} heard noise at {noise.Position}", this);
        SoundPerceived?.Invoke(new Hearing(noise, LastAudibility, strength));
        onNoiseHeard.Invoke(noise.Position);
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || !HasHeardNoise) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, LastHeardPosition);
        Gizmos.DrawWireSphere(LastHeardPosition, 0.3f);
    }
}
