// --- LevelUpEffect.cs ---
using UnityEngine;

public class LevelUpEffect : MonoBehaviour
{
    private float _lifetime = 5f; // Default lifetime

    // Call this immediately after instantiating the effect
    // Ensure this is PUBLIC
    public void Initialize(float duration)
    {
        _lifetime = duration;
        if (_lifetime > 0)
        {
            Destroy(gameObject, _lifetime);
        }
        else
        {
            // If duration is 0 or less, destroy immediately or rely on Particle System's own duration
            ParticleSystem ps = GetComponent<ParticleSystem>();
            if (ps == null || (ps.main.duration < _lifetime && ps.main.stopAction != ParticleSystemStopAction.Destroy))
            {
                // If not a PS, or its own duration is shorter than our override, and it doesn't self-destroy.
                // This logic might need refinement based on how you want to handle ParticleSystems.
                // For simplicity, if duration is 0, we'll let ParticleSystems with StopAction.Destroy handle themselves,
                // otherwise, we destroy immediately.
                if (ps != null && ps.main.stopAction == ParticleSystemStopAction.Destroy && _lifetime <= 0)
                {
                    // Let it self-destroy if configured and lifetime is effectively "use default"
                }
                else
                {
                    Debug.LogWarning($"LevelUpEffect: Invalid duration ({_lifetime}) or not a self-terminating Particle System. Destroying immediately or after its own short cycle.", this);
                    // If it's not a self-destroying particle system, or lifetime is explicitly > 0, Destroy is already scheduled.
                    // If lifetime is <= 0 and not a self-destroying PS, destroy now.
                    if (_lifetime <= 0 && (ps == null || ps.main.stopAction != ParticleSystemStopAction.Destroy))
                    {
                        Destroy(gameObject);
                    }
                }
            }
        }
    }
}