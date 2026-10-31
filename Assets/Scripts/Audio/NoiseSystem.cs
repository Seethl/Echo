using System;
using UnityEngine;

/// <summary>Gameplay sound events. Radius is measured in world metres, not audio volume.</summary>
public static class NoiseSystem
{
    public readonly struct Noise
    {
        public readonly Vector3 Position;
        public readonly float Radius;
        public readonly GameObject Source;

        public Noise(Vector3 position, float radius, GameObject source)
        {
            Position = position;
            Radius = radius;
            Source = source;
        }
    }

    public static event Action<Noise> Emitted;

    // Also reset when entering Play with domain reload disabled.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { Emitted = null; }

    public static void Emit(Vector3 position, float radius, GameObject source)
    {
        if (radius <= 0f || float.IsNaN(radius) || float.IsInfinity(radius)) return;
        Emitted?.Invoke(new Noise(position, radius, source));
    }
}
