using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Centralized particle system manager with object pooling and batch optimization
/// Reduces draw calls by reusing particle systems and enforcing particle limits
/// </summary>
public class OptimizedParticleManager : MonoBehaviour
{
    public static OptimizedParticleManager Instance { get; private set; }
    
    [Header("Performance Settings")]
    [Tooltip("Maximum particles per system (lower = better performance)")]
    [SerializeField] private int maxParticlesPerSystem = 300;
    
    [Tooltip("Maximum active particle systems at once")]
    [SerializeField] private int maxActiveParticleSystems = 50;
    
    [Tooltip("Pool size for each VFX prefab")]
    [SerializeField] private int poolSizePerPrefab = 5;
    
    [Tooltip("Enable GPU instancing on all particle materials")]
    [SerializeField] private bool enableGPUInstancing = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    // Pooling system
    private Dictionary<GameObject, Queue<GameObject>> _particlePools = new Dictionary<GameObject, Queue<GameObject>>();
    private List<ParticleSystemInfo> _activeParticleSystems = new List<ParticleSystemInfo>();
    
    // Statistics
    private int _totalParticlesSpawned = 0;
    private int _totalParticlesPooled = 0;
    
    private class ParticleSystemInfo
    {
        public GameObject gameObject;
        public ParticleSystem[] particleSystems;
        public float spawnTime;
        public float maxLifetime;
        public bool isPooled;
        public GameObject prefabSource;
    }
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    void Update()
    {
        CleanupFinishedParticles();
        
        if (showDebugInfo)
        {
            Debug.Log($"[ParticleManager] Active: {_activeParticleSystems.Count}, Pooled: {_totalParticlesPooled}, Spawned: {_totalParticlesSpawned}");
        }
    }
    
    /// <summary>
    /// Spawn a particle effect with automatic pooling and optimization
    /// </summary>
    public GameObject SpawnParticleEffect(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[ParticleManager] Attempted to spawn null prefab!");
            return null;
        }
        
        // Check if we've hit the max active limit
        if (_activeParticleSystems.Count >= maxActiveParticleSystems)
        {
            // Force cleanup oldest particle
            ForceCleanupOldest();
        }
        
        GameObject instance = GetFromPool(prefab);
        bool isFromPool = instance != null;
        
        if (instance == null)
        {
            // Create new instance
            instance = Instantiate(prefab, position, rotation, parent);
            _totalParticlesSpawned++;
        }
        else
        {
            // Reuse pooled instance
            instance.transform.position = position;
            instance.transform.rotation = rotation;
            instance.transform.SetParent(parent);
            instance.SetActive(true);
        }
        
        // Optimize particle systems
        ParticleSystem[] particleSystems = instance.GetComponentsInChildren<ParticleSystem>(true);
        float maxLifetime = 0f;
        
        foreach (ParticleSystem ps in particleSystems)
        {
            if (ps != null)
            {
                OptimizeParticleSystem(ps);
                
                // Calculate max lifetime
                var main = ps.main;
                float lifetime = main.startLifetime.constantMax + main.duration;
                if (lifetime > maxLifetime)
                {
                    maxLifetime = lifetime;
                }
                
                // Play the particle system
                if (!ps.isPlaying)
                {
                    ps.Play(true);
                }
            }
        }
        
        // Track active particle
        ParticleSystemInfo info = new ParticleSystemInfo
        {
            gameObject = instance,
            particleSystems = particleSystems,
            spawnTime = Time.time,
            maxLifetime = maxLifetime + 0.5f, // Add buffer
            isPooled = isFromPool,
            prefabSource = prefab
        };
        
        _activeParticleSystems.Add(info);
        
        return instance;
    }
    
    /// <summary>
    /// Optimize a single particle system for batching
    /// </summary>
    private void OptimizeParticleSystem(ParticleSystem ps)
    {
        if (ps == null) return;
        
        var main = ps.main;
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        
        // Limit max particles
        main.maxParticles = Mathf.Min(main.maxParticles, maxParticlesPerSystem);
        
        // Disable prewarm (causes spawn lag)
        main.prewarm = false;
        
        // Optimize renderer
        if (renderer != null)
        {
            // Enable GPU instancing if available
            if (enableGPUInstancing && renderer.sharedMaterial != null)
            {
                renderer.sharedMaterial.enableInstancing = true;
            }
            
            // Disable shadows for particles (huge performance gain)
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            
            // Use mesh rendering for better batching
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }
        
        // Optimize collision if enabled
        var collision = ps.collision;
        if (collision.enabled)
        {
            collision.maxCollisionShapes = 16; // Limit collision checks
            collision.collidesWith = LayerMask.GetMask("Default", "Enemy"); // Only collide with necessary layers
        }
    }
    
    /// <summary>
    /// Get instance from pool or return null
    /// </summary>
    private GameObject GetFromPool(GameObject prefab)
    {
        if (!_particlePools.ContainsKey(prefab))
        {
            _particlePools[prefab] = new Queue<GameObject>();
            return null;
        }
        
        Queue<GameObject> pool = _particlePools[prefab];
        
        while (pool.Count > 0)
        {
            GameObject instance = pool.Dequeue();
            if (instance != null)
            {
                _totalParticlesPooled--;
                return instance;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Return instance to pool
    /// </summary>
    private void ReturnToPool(ParticleSystemInfo info)
    {
        if (info.gameObject == null || info.prefabSource == null) return;
        
        // Stop all particle systems
        foreach (ParticleSystem ps in info.particleSystems)
        {
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
        
        // Deactivate and pool
        info.gameObject.SetActive(false);
        info.gameObject.transform.SetParent(transform); // Parent to manager
        
        if (!_particlePools.ContainsKey(info.prefabSource))
        {
            _particlePools[info.prefabSource] = new Queue<GameObject>();
        }
        
        // Only pool if we haven't exceeded pool size
        if (_particlePools[info.prefabSource].Count < poolSizePerPrefab)
        {
            _particlePools[info.prefabSource].Enqueue(info.gameObject);
            _totalParticlesPooled++;
        }
        else
        {
            // Destroy excess pooled objects
            Destroy(info.gameObject);
        }
    }
    
    /// <summary>
    /// Cleanup finished particle systems
    /// </summary>
    private void CleanupFinishedParticles()
    {
        for (int i = _activeParticleSystems.Count - 1; i >= 0; i--)
        {
            ParticleSystemInfo info = _activeParticleSystems[i];
            
            // Check if particle system is finished
            bool isFinished = Time.time - info.spawnTime > info.maxLifetime;
            
            if (isFinished || info.gameObject == null)
            {
                if (info.gameObject != null)
                {
                    ReturnToPool(info);
                }
                _activeParticleSystems.RemoveAt(i);
            }
        }
    }
    
    /// <summary>
    /// Force cleanup of oldest particle system
    /// </summary>
    private void ForceCleanupOldest()
    {
        if (_activeParticleSystems.Count == 0) return;
        
        ParticleSystemInfo oldest = _activeParticleSystems[0];
        
        if (oldest.gameObject != null)
        {
            ReturnToPool(oldest);
        }
        
        _activeParticleSystems.RemoveAt(0);
        
        if (showDebugInfo)
        {
            Debug.Log("[ParticleManager] Forced cleanup of oldest particle system");
        }
    }
    
    /// <summary>
    /// Clear all active particles immediately
    /// </summary>
    public void ClearAllParticles()
    {
        for (int i = _activeParticleSystems.Count - 1; i >= 0; i--)
        {
            ParticleSystemInfo info = _activeParticleSystems[i];
            if (info.gameObject != null)
            {
                ReturnToPool(info);
            }
        }
        _activeParticleSystems.Clear();
    }
    
    /// <summary>
    /// Get current statistics
    /// </summary>
    public void GetStatistics(out int active, out int pooled, out int totalSpawned)
    {
        active = _activeParticleSystems.Count;
        pooled = _totalParticlesPooled;
        totalSpawned = _totalParticlesSpawned;
    }
    
    void OnDestroy()
    {
        ClearAllParticles();
        
        // Cleanup pools
        foreach (var pool in _particlePools.Values)
        {
            while (pool.Count > 0)
            {
                GameObject instance = pool.Dequeue();
                if (instance != null)
                {
                    Destroy(instance);
                }
            }
        }
        _particlePools.Clear();
    }
}
