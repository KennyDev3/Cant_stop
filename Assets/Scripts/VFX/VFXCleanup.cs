using UnityEngine;

public class VFXCleanup : MonoBehaviour
{
    private ParticleSystem[] particleSystems;

    void Start()
    {
        // Get all particle systems, including children
        particleSystems = GetComponentsInChildren<ParticleSystem>();

        // Find the duration of the longest particle system
        float maxDuration = 0f;
        foreach (var ps in particleSystems)
        {
            float duration = ps.main.duration + ps.main.startLifetime.constantMax;
            if (duration > maxDuration)
            {
                maxDuration = duration;
            }
        }

        // Schedule the destruction of this entire GameObject after the longest duration
        // Adding a small buffer (e.g., 0.1f) is often wise.
        Destroy(gameObject, maxDuration + 0.1f);
    }
}