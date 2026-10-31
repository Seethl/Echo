using UnityEngine;

public class SurfaceType : MonoBehaviour
{
    public enum Kind { Default, Wood, Carpet, Glass }

    public Kind kind;
    [Tooltip("Optional override of the player's sound for this surface kind.")]
    public AudioClip footstepSound;

    [Range(0f, 2f)]
    public float loudnessMultiplier = 1f;
}
