using System;
using UnityEngine;

/// <summary>Gameplay sound events. Radius is measured in world metres, not audio volume.</summary>
public static class NoiseSystem
{
    public enum Kind { General = 0, Interaction = General, Footstep = 1, Decoy = 2, Radio = 3, Mechanism = 4, Ambient = 5, Enemy = 6 }
    public readonly struct Noise
    {
        public readonly Vector3 Position;
        public readonly float Radius;
        public readonly GameObject Source;
        public readonly Kind Category;
        public Kind Type => Category;
        public readonly float Intensity;

        public Noise(Vector3 position, float radius, GameObject source, Kind category = Kind.General, float intensity = 1f)
        {
            Position = position;
            Radius = radius;
            Source = source;
            Category = category;
            Intensity = Mathf.Clamp01(intensity);
        }
    }

    public static event Action<Noise> Emitted;

    // Also reset when entering Play with domain reload disabled.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { Emitted = null; }

    public static void Emit(Vector3 position, float radius, GameObject source, Kind category = Kind.General, float intensity = 1f)
    {
        if (radius <= 0f || float.IsNaN(radius) || float.IsInfinity(radius)) return;
        if (intensity <= 0f || float.IsNaN(intensity) || float.IsInfinity(intensity)) return;
        if (float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z) ||
            float.IsInfinity(position.x) || float.IsInfinity(position.y) || float.IsInfinity(position.z)) return;
        Emitted?.Invoke(new Noise(position, radius, source, category, intensity));
    }
}
