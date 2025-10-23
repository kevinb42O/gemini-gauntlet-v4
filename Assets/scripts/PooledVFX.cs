using UnityEngine;

/// <summary>
/// Attach to VFX prefabs to auto-return them to pool after lifetime.
/// This prevents memory leaks and eliminates Instantiate/Destroy overhead.
/// </summary>
public class PooledVFX : MonoBehaviour
{
    [Header("Lifetime Settings")]
    [Tooltip("How long before this VFX returns to pool (seconds)")]
    public float lifetime = 2f;
    
    [Tooltip("If true, uses particle system duration instead of fixed lifetime")]
    public bool useParticleSystemDuration = true;
    
    [Header("Debug")]
    [Tooltip("Show debug logs for pooling events")]
    public bool enableDebugLogs = false;
    
    private float spawnTime;
    private float calculatedLifetime;
    private ParticleSystem[] particleSystems;
    
    void Awake()
    {
        // Cache particle systems once
        particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        
        if (useParticleSystemDuration && particleSystems.Length > 0)
        {
            // Calculate maximum lifetime from all particle systems
            calculatedLifetime = 0f;
            foreach (var ps in particleSystems)
            {
                if (ps != null)
                {
                    float psLifetime = ps.main.duration + ps.main.startLifetime.constantMax;
                    if (psLifetime > calculatedLifetime)
                    {
                        calculatedLifetime = psLifetime;
                    }
                }
            }
            
            // Add small buffer to ensure particles finish
            calculatedLifetime += 0.5f;
            
            if (enableDebugLogs)
            {
                Debug.Log($"[PooledVFX] {name} calculated lifetime: {calculatedLifetime:F2}s from particle systems");
            }
        }
        else
        {
            calculatedLifetime = lifetime;
        }
    }
    
    void OnEnable()
    {
        // Reset timer when spawned from pool
        spawnTime = Time.time;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[PooledVFX] {name} spawned from pool at {spawnTime:F2}");
        }
        
        // Ensure all particle systems are playing
        foreach (var ps in particleSystems)
        {
            if (ps != null && !ps.isPlaying)
            {
                ps.Play();
            }
        }
    }
    
    void Update()
    {
        // Check if lifetime expired
        if (Time.time - spawnTime >= calculatedLifetime)
        {
            ReturnToPool();
        }
    }
    
    /// <summary>
    /// Returns this VFX to the pool
    /// </summary>
    public void ReturnToPool()
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[PooledVFX] {name} returning to pool after {Time.time - spawnTime:F2}s");
        }
        
        // Stop all particle systems before returning to pool
        foreach (var ps in particleSystems)
        {
            if (ps != null && ps.isPlaying)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
        
        // Return to pool by deactivating
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Force immediate return to pool (useful for cleanup)
    /// </summary>
    public void ForceReturnToPool()
    {
        ReturnToPool();
    }
    
    void OnDisable()
    {
        // Clean up when disabled
        if (enableDebugLogs)
        {
            Debug.Log($"[PooledVFX] {name} disabled");
        }
    }
}
