using UnityEngine;

/// <summary>Optional material rule for a wall/door. Opening works through collider geometry.</summary>
public class AcousticObstacle : MonoBehaviour
{
    [Range(0f, 1f), Tooltip("Remaining gameplay hearing radius: 0 blocks sound, 1 is transparent.")]
    public float transmission = 0.45f;
}
