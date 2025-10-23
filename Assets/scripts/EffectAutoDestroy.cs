// --- EffectAutoDestroy.cs (Improved & Corrected Obsolete API & Scope) ---
using UnityEngine;
using System.Linq; // For Max and All

public class EffectAutoDestroy : MonoBehaviour
{
    [Tooltip("Set a manual time in seconds after which this effect GameObject will be destroyed. If 0 or less, it will try to calculate based on Particle Systems and Audio Sources.")]
    public float manualDestroyTime = 0f;

    [Tooltip("Additional safety delay in seconds, added to calculated or manual time. Useful if particles have trails that need to fade, or audio needs to complete.")]
    public float additionalSafetyDelay = 0.5f;

    [Tooltip("If true, will also consider the duration of AudioSources on this object or its children when calculating auto-destroy time (if manualDestroyTime is not set).")]
    public bool considerAudioDuration = true;

    void Start()
    {
        float destroyTime = 0f;

        if (manualDestroyTime > 0f)
        {
            destroyTime = manualDestroyTime;
        }
        else
        {
            float maxParticleSystemDuration = 0f;
            ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>(true); // Get all, including inactive

            if (particleSystems.Length > 0)
            {
                foreach (ParticleSystem ps in particleSystems)
                {
                    if (ps == null) continue;
                    var mainModule = ps.main;
                    if (mainModule.loop)
                    {
                        Debug.LogWarning($"EffectAutoDestroy ({name}): Particle system '{ps.name}' is looping. Using fallback duration (5s) for this PS if it's the longest and no manualDestroyTime is set.", ps.gameObject);
                        maxParticleSystemDuration = Mathf.Max(maxParticleSystemDuration, 5f);
                        continue;
                    }
                    float psStartDelay = mainModule.startDelay.curveMultiplier * mainModule.startDelay.constantMax;
                    float psDuration = mainModule.duration;
                    float psStartLifetime = mainModule.startLifetime.curveMultiplier * mainModule.startLifetime.constantMax;
                    float currentPsTime = psStartDelay + psDuration + psStartLifetime;
                    if (currentPsTime > maxParticleSystemDuration)
                    {
                        maxParticleSystemDuration = currentPsTime;
                    }
                }
            }

            float maxAudioDuration = 0f;
            AudioSource[] audioSources = null; // Declare here to widen scope

            if (considerAudioDuration)
            {
                audioSources = GetComponentsInChildren<AudioSource>(true); // Assign here
                if (audioSources.Length > 0)
                {
                    foreach (AudioSource aus in audioSources)
                    {
                        if (aus == null || aus.clip == null) continue;
                        if (aus.loop)
                        {
                            Debug.LogWarning($"EffectAutoDestroy ({name}): AudioSource '{aus.name}' with clip '{aus.clip.name}' is looping. Using fallback (5s) if longest and no manualDestroyTime.", aus.gameObject);
                            maxAudioDuration = Mathf.Max(maxAudioDuration, 5f);
                            continue;
                        }
                        float audioClipLength = aus.clip.length / Mathf.Max(0.01f, Mathf.Abs(aus.pitch));
                        if (audioClipLength > maxAudioDuration)
                        {
                            maxAudioDuration = audioClipLength;
                        }
                    }
                }
            }

            destroyTime = Mathf.Max(maxParticleSystemDuration, maxAudioDuration);

            // Check if any relevant components were found for automatic calculation
            bool noParticles = particleSystems.Length == 0;
            bool noRelevantAudio = !considerAudioDuration || audioSources == null || audioSources.All(a => a == null || a.clip == null || a.loop);
            // If audioSources is null (because considerAudioDuration was false), noRelevantAudio should be true for this check.
            if (audioSources == null && considerAudioDuration) noRelevantAudio = true;


            if (destroyTime <= 0.01f && noParticles && noRelevantAudio)
            {
                Debug.LogWarning($"EffectAutoDestroy ({name}): No ParticleSystems or relevant (non-looping) AudioSources found, and no manual time set. Using default destroy time (1s + safety). Set manualDestroyTime if this is not desired.", gameObject);
                destroyTime = 1.0f;
            }
        }

        if (destroyTime <= 0 && additionalSafetyDelay <= 0)
        {
            Debug.LogWarning($"EffectAutoDestroy ({name}): Calculated destroy time ({destroyTime:F2}s) and safety delay ({additionalSafetyDelay:F2}s) result in no self-destruction. Ensure configuration is correct or set manualDestroyTime for '{this.gameObject.name}'.", gameObject);
        }
        else
        {
            float finalDestroyTime = Mathf.Max(0.01f, destroyTime + additionalSafetyDelay);
            Destroy(gameObject, finalDestroyTime);
        }
    }
}