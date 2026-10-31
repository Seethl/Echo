using UnityEngine;

public class SurfaceType : MonoBehaviour
{
    public enum Kind { Default, Wood, Carpet, Glass, Stone, Creaky }

    public Kind kind;
    [Tooltip("Optional override of the player's sound for this surface kind.")]
    public AudioClip footstepSound;

    [Range(0f, 2f)]
    public float loudnessMultiplier = 1f;

    [Min(0f), Tooltip("Gameplay hearing radius multiplier, independent of audio volume.")]
    public float noiseMultiplier = 1f;
    [Min(0f), Tooltip("Keeps hazardous glass audible even while crouching.")]
    public float minimumNoiseRadius;
}
