using UnityEngine;

public class SurfaceType : MonoBehaviour
{
    public AudioClip footstepSound;

    [Range(0f, 2f)]
    public float loudnessMultiplier = 1f;
}
