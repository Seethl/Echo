using UnityEngine;
using UnityEngine.Events;

/// <summary>Attach to an enemy; connect onNoiseHeard to its investigation behaviour.</summary>
public class NoiseListener : MonoBehaviour
{
    [Min(0f), Tooltip("1 = normal hearing; 2 = twice the emitted radius.")]
    public float hearingSensitivity = 1f;
    public UnityEvent<Vector3> onNoiseHeard = new UnityEvent<Vector3>();
    public bool logHeardNoise;
    [Min(0f)] public float decoyAttentionSeconds = 2f;

    public bool HasHeardNoise { get; private set; }
    public Vector3 LastHeardPosition { get; private set; }
    public float LastHeardTime { get; private set; }
    public NoiseSystem.Kind LastHeardCategory { get; private set; }
    private float attentionUntil;
    private float attentionStrength;

    void OnEnable() { NoiseSystem.Emitted += Hear; }
    void OnDisable() { NoiseSystem.Emitted -= Hear; }

    private void Hear(NoiseSystem.Noise noise)
    {
        if (Echo.Menu.GameMenu.BlocksPlayerInput || Time.timeScale <= 0f) return;
        var threat = GetComponent<EnemyThreat>();
        if (threat != null && !threat.CanHear) return;
        if (noise.Source != null &&
            (noise.Source.transform == transform || transform.IsChildOf(noise.Source.transform))) return;

        float radius = noise.Radius * Mathf.Max(0f, hearingSensitivity);
        radius *= NoiseMaskingSource.GetMultiplier(noise.Position, transform.position);
        if (radius <= 0f || (noise.Position - transform.position).sqrMagnitude > radius * radius) return;

        float strength = radius / Mathf.Max(0.5f, Vector3.Distance(noise.Position, transform.position));
        // A quiet step must not instantly cancel a ringing distraction. Nearby loud
        // actions still break attention, so the clock never makes the player invulnerable.
        if (Time.time < attentionUntil && noise.Category != NoiseSystem.Kind.Decoy &&
            strength < attentionStrength * 1.25f) return;

        HasHeardNoise = true;
        LastHeardPosition = noise.Position;
        LastHeardTime = Time.time;
        LastHeardCategory = noise.Category;
        attentionUntil = noise.Category == NoiseSystem.Kind.Decoy ? Time.time + decoyAttentionSeconds : 0f;
        attentionStrength = strength;
        if (logHeardNoise) Debug.Log($"{name} heard noise at {noise.Position}", this);
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
