using UnityEngine;
using UnityEngine.Events;

/// <summary>Attach to an enemy; connect onNoiseHeard to its investigation behaviour.</summary>
public class NoiseListener : MonoBehaviour
{
    [Min(0f), Tooltip("1 = normal hearing; 2 = twice the emitted radius.")]
    public float hearingSensitivity = 1f;
    public UnityEvent<Vector3> onNoiseHeard = new UnityEvent<Vector3>();
    public bool logHeardNoise;
    [Range(0,1)] public float wallAttenuation=.25f;
    [Range(.01f,.5f)] public float detectionThreshold=.06f;
    public float LastStrength {get;private set;}
    public NoiseSystem.Kind LastKind {get;private set;}

    public bool HasHeardNoise { get; private set; }
    public Vector3 LastHeardPosition { get; private set; }
    public float LastHeardTime { get; private set; }

    void OnEnable() { NoiseSystem.Emitted += Hear; }
    void OnDisable() { NoiseSystem.Emitted -= Hear; }

    private void Hear(NoiseSystem.Noise noise)
    {
        if(Time.timeScale<=0||noise.Type==NoiseSystem.Kind.Ambient||noise.Type==NoiseSystem.Kind.Enemy)return;
        if (noise.Source != null &&
            (noise.Source.transform == transform || transform.IsChildOf(noise.Source.transform))) return;

        float strength=PerceivedStrength(noise);
        if(strength<detectionThreshold)return;

        HasHeardNoise = true;
        LastHeardPosition = noise.Position;
        LastHeardTime = Time.time;
        LastStrength=strength;LastKind=noise.Type;
        if (logHeardNoise) Debug.Log($"{name} heard noise at {noise.Position}", this);
        onNoiseHeard.Invoke(noise.Position);
    }

    public float PerceivedStrength(NoiseSystem.Noise noise)
    {
        float radius=noise.Radius*Mathf.Max(0,hearingSensitivity);
        Vector3 origin=transform.position+Vector3.up*1.4f;
        float distance=Vector3.Distance(origin,noise.Position);
        if(radius<=0||distance>=radius)return 0;
        float strength=(1-distance/radius)*noise.Intensity;
        if(Physics.Linecast(origin,noise.Position,out var hit,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
        {
            bool source= noise.Source!=null&&(hit.transform==noise.Source.transform||hit.transform.IsChildOf(noise.Source.transform));
            bool self=hit.transform==transform||hit.transform.IsChildOf(transform);
            if(!source&&!self)strength*=wallAttenuation;
        }
        // A nearby sound beats masking. A distant ambient source never masks this listener globally.
        if(distance>2.2f)strength-=Echo.Audio.NoiseMaskZone.CoverAt(origin)*.8f;
        return Mathf.Max(0,strength);
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || !HasHeardNoise) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, LastHeardPosition);
        Gizmos.DrawWireSphere(LastHeardPosition, 0.3f);
    }
}
