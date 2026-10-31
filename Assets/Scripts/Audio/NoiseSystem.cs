using System;
using UnityEngine;

/// <summary>Gameplay sound events. Radius is measured in world metres, not audio volume.</summary>
public static class NoiseSystem
{
    public enum Kind { General, Footstep, Decoy }
    public readonly struct Noise
    {
        public readonly Vector3 Position;
        public readonly float Radius;
        public readonly GameObject Source;
        public readonly Kind Category;

        public Noise(Vector3 position, float radius, GameObject source, Kind category = Kind.General)
        {
            Position = position;
            Radius = radius;
            Source = source;
            Category = category;
        }
    }

    public static event Action<Noise> Emitted;

    // Also reset when entering Play with domain reload disabled.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { Emitted = null; }

    public static void Emit(Vector3 position, float radius, GameObject source, Kind category = Kind.General)
    {
        if (radius <= 0f || float.IsNaN(radius) || float.IsInfinity(radius)) return;
        if (float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z) ||
            float.IsInfinity(position.x) || float.IsInfinity(position.y) || float.IsInfinity(position.z)) return;
        Emitted?.Invoke(new Noise(position, radius, source, category));
    }
}
