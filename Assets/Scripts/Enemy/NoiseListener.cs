using UnityEngine;
using UnityEngine.Events;

/// <summary>Attach to an enemy; connect onNoiseHeard to its investigation behaviour.</summary>
public class NoiseListener : MonoBehaviour
{
    [Min(0f), Tooltip("1 = normal hearing; 2 = twice the emitted radius.")]
    public float hearingSensitivity = 1f;
    public UnityEvent<Vector3> onNoiseHeard = new UnityEvent<Vector3>();
    public bool logHeardNoise;

    public bool HasHeardNoise { get; private set; }
    public Vector3 LastHeardPosition { get; private set; }
    public float LastHeardTime { get; private set; }

    void OnEnable() { NoiseSystem.Emitted += Hear; }
    void OnDisable() { NoiseSystem.Emitted -= Hear; }

    private void Hear(NoiseSystem.Noise noise)
    {
        if (noise.Source != null &&
            (noise.Source.transform == transform || transform.IsChildOf(noise.Source.transform))) return;

        float radius = noise.Radius * Mathf.Max(0f, hearingSensitivity);
        if (radius <= 0f || (noise.Position - transform.position).sqrMagnitude > radius * radius) return;

        HasHeardNoise = true;
        LastHeardPosition = noise.Position;
        LastHeardTime = Time.time;
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
