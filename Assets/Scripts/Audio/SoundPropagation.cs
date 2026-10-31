using System.Collections.Generic;
using UnityEngine;

/// <summary>Direct-path gameplay attenuation, not a full room/portal acoustic simulation.</summary>
public static class SoundPropagation
{
    public static float Transmission(Vector3 sourcePosition, Vector3 listenerPosition,
        Transform sourceRoot, Transform listenerRoot, int obstacleLayers, float defaultTransmission = 0.45f)
    {
        Vector3 direction = listenerPosition - sourcePosition;
        float distance = direction.magnitude;
        if (distance <= 0.001f || obstacleLayers == 0) return 1f;
        var hits = Physics.RaycastAll(sourcePosition, direction / distance, distance,
            obstacleLayers, QueryTriggerInteraction.Ignore);
        if (hits.Length == 0) return 1f;
        var counted = new HashSet<Object>();
        float result = 1f;
        foreach (var hit in hits)
        {
            var collider = hit.collider;
            if ((sourceRoot != null && collider.transform.IsChildOf(sourceRoot)) ||
                (listenerRoot != null && collider.transform.IsChildOf(listenerRoot))) continue;
            var profile = collider.GetComponentInParent<AcousticObstacle>();
            if (profile != null && !profile.isActiveAndEnabled) profile = null;
            // A composite door/profile is one barrier even when it has several colliders.
            Object barrier = profile != null ? (Object)profile :
                collider.attachedRigidbody != null ? collider.attachedRigidbody : (Object)collider;
            if (!counted.Add(barrier)) continue;
            float factor = profile != null ? profile.transmission : defaultTransmission;
            if (float.IsNaN(factor) || float.IsInfinity(factor)) factor = 0.45f;
            result *= Mathf.Clamp01(factor);
            if (result <= 0f) return 0f;
        }
        return result;
    }
}
