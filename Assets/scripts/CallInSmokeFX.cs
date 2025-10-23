// --- CallInSmokeFX.cs ---
using UnityEngine;
using System; // Required for Action

public class CallInSmokeFX : MonoBehaviour
{
    [Tooltip("How long the smoke effect lasts before self-destructing. Set to 0 or less for no automatic self-destruct (not recommended if spawned by HelicopterCallZone).")]
    public float selfDestructTime = 15.0f;

    [Tooltip("Should the particle effects play automatically when this object is enabled/instantiated?")]
    public bool playOnAwakeOrEnable = true;

    [Tooltip("If stopping, should the particles be cleared immediately or just stop emitting?")]
    public bool clearParticlesOnStop = false;

    // Event to signal when the effect is considered finished (typically just before or during OnDestroy)
    public event Action<CallInSmokeFX> OnEffectFinished;

    private ParticleSystem[] _particleSystems;
    private bool _isEffectPlaying = false;
    private bool _isQuitting = false; // To prevent errors when OnDestroy is called during application quit

    void Awake()
    {
        _particleSystems = GetComponentsInChildren<ParticleSystem>(true); // true to include inactive
        if (_particleSystems.Length == 0)
        {
            Debug.LogWarning($"CallInSmokeFX on '{name}' found no ParticleSystem components in itself or its children. The effect will not play.", this);
        }

        if (playOnAwakeOrEnable)
        {
            PlayEffect();
        }
    }

    void OnEnable()
    {
        if (playOnAwakeOrEnable && !_isEffectPlaying)
        {
            PlayEffect();
        }
    }

    void Start()
    {
        if (selfDestructTime > 0)
        {
            Destroy(gameObject, selfDestructTime);
        }
        else
        {
            Debug.LogWarning($"CallInSmokeFX on '{name}' has selfDestructTime set to 0 or less. It will not self-destruct automatically. Ensure something else manages its lifetime.", this);
        }
    }

    public void PlayEffect()
    {
        if (_particleSystems == null || _particleSystems.Length == 0) return;
        foreach (var ps in _particleSystems)
        {
            if (ps != null)
            {
                ps.Play(true);
            }
        }
        _isEffectPlaying = true;
    }

    public void StopEffect(bool clear)
    {
        if (_particleSystems == null || _particleSystems.Length == 0) return;
        foreach (var ps in _particleSystems)
        {
            if (ps != null)
            {
                ps.Stop(true, clear ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting);
            }
        }
        _isEffectPlaying = false;
    }

    public void StopEffect()
    {
        StopEffect(clearParticlesOnStop);
    }

    public bool IsPlaying()
    {
        if (!_isEffectPlaying && (_particleSystems == null || _particleSystems.Length == 0))
        {
            return false;
        }
        if (_particleSystems != null)
        {
            foreach (var ps in _particleSystems)
            {
                if (ps != null && ps.IsAlive(true))
                {
                    _isEffectPlaying = true;
                    return true;
                }
            }
        }
        _isEffectPlaying = false;
        return false;
    }

    void OnApplicationQuit()
    {
        _isQuitting = true;
    }

    void OnDestroy()
    {
        if (_isQuitting) return;
        try
        {
            OnEffectFinished?.Invoke(this);
        }
        catch (Exception e)
        {
            Debug.LogError($"CallInSmokeFX on '{name}': Error invoking OnEffectFinished: {e.Message}", this);
        }
        _particleSystems = null;
    }
}