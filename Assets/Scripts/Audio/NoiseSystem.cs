using System;
using UnityEngine;

/// <summary>Gameplay sound events. Radius is measured in world metres, not audio volume.</summary>
public static class NoiseSystem
{
    // Shared conditional scale for source power, local cover and hearing; not decibels.
    public const float ReferenceRadius=20f;
    public static float Level(float radius,float intensity=1f)=>Mathf.Clamp01(radius/ReferenceRadius)*Mathf.Clamp01(intensity);
    public static float Level(Noise noise)=>Level(noise.Radius,noise.Intensity);
    public enum Kind { Footstep, Interaction, Radio, Mechanism, Ambient, Enemy }
    public readonly struct Noise
    {
        public readonly Vector3 Position;
        public readonly float Radius;
        public readonly GameObject Source;
        public readonly Kind Type;
        public readonly float Intensity;
        public readonly GameObject Instigator;

        public Noise(Vector3 position, float radius, GameObject source, Kind type=Kind.Interaction, float intensity=1f,GameObject instigator=null)
        {
            Position = position;
            Radius = radius;
            Source = source;
            Type=type;Intensity=Mathf.Clamp01(intensity);
            Instigator=instigator;
        }
    }

    public static event Action<Noise> Emitted;

    // Also reset when entering Play with domain reload disabled.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { Emitted = null; }

    public static void Emit(Vector3 position, float radius, GameObject source)
        => Emit(position,radius,source,Kind.Interaction,1f);
    public static void Emit(Vector3 position,float radius,GameObject source,Kind type,float intensity=1f,GameObject instigator=null)
    {
        if (radius <= 0f || float.IsNaN(radius) || float.IsInfinity(radius)) return;
        Emitted?.Invoke(new Noise(position, radius, source,type,intensity,instigator));
    }
}
