using UnityEngine;

/// <summary>Audible environmental cover. Does not lure or stun the enemy.</summary>
[RequireComponent(typeof(AudioSource))]
public class NoiseMaskingSource : MonoBehaviour
{
    [Min(0.1f)] public float radius = 10f;
    [Range(0f, 1f)] public float hearingRadiusMultiplier = 0.25f;
    private AudioSource source;

    void Awake() { source = GetComponent<AudioSource>(); }

    public bool IsMasking => isActiveAndEnabled && source != null && source.enabled &&
        source.isPlaying && source.clip != null && !source.mute && source.volume > 0f &&
        !AudioListener.pause && Time.timeScale > 0f;

    // Bind this to a clock/lever event. The clip duration defines the safe window.
    public void PlayCover()
    {
        if (source == null) source = GetComponent<AudioSource>();
        if (isActiveAndEnabled && source.clip != null) source.Play();
    }

    public static float GetMultiplier(Vector3 noisePosition, Vector3 listenerPosition)
    {
        float multiplier = 1f;
        foreach (var cover in FindObjectsByType<NoiseMaskingSource>())
        {
            if (!cover.IsMasking) continue;
            float range = Mathf.Max(0f, cover.radius);
            // Both participants must hear the cover. Never provide global protection.
            if ((noisePosition - cover.transform.position).sqrMagnitude > range * range ||
                (listenerPosition - cover.transform.position).sqrMagnitude > range * range) continue;
            // Overlapping clocks use the strongest cover, not infinitely stacked silence.
            multiplier = Mathf.Min(multiplier, Mathf.Clamp01(cover.hearingRadiusMultiplier));
        }
        return multiplier;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0f, radius));
    }
}
