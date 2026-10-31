using System;
using UnityEngine;

/// <summary>Gameplay sound events. Radius is measured in world metres, not audio volume.</summary>
public static class NoiseSystem
{
    public enum Kind { Footstep, Interaction, Radio, Mechanism, Ambient, Enemy }
    public readonly struct Noise
    {
        public readonly Vector3 Position;
        public readonly float Radius;
        public readonly GameObject Source;
        public readonly Kind Type;
        public readonly float Intensity;

        public Noise(Vector3 position, float radius, GameObject source, Kind type=Kind.Interaction, float intensity=1f)
        {
            Position = position;
            Radius = radius;
            Source = source;
            Type=type;Intensity=Mathf.Clamp01(intensity);
        }
    }

    public static event Action<Noise> Emitted;

    // Also reset when entering Play with domain reload disabled.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { Emitted = null; }

    public static void Emit(Vector3 position, float radius, GameObject source)
        => Emit(position,radius,source,Kind.Interaction,1f);
    public static void Emit(Vector3 position,float radius,GameObject source,Kind type,float intensity=1f)
    {
        if (radius <= 0f || float.IsNaN(radius) || float.IsInfinity(radius)) return;
        Emitted?.Invoke(new Noise(position, radius, source,type,intensity));
    }
}
