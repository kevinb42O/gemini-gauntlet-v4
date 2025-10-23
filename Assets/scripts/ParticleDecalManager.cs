using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages persistent particle decals that stick to surfaces.
/// Uses object pooling for maximum performance.
/// </summary>
public class ParticleDecalManager : MonoBehaviour
{
    public static ParticleDecalManager Instance { get; private set; }
    
    [Header("Decal Settings")]
    [Tooltip("Prefab for persistent particle decals (simple particle system)")]
    public GameObject decalParticlePrefab;
    
    [Tooltip("How long decals stay on surfaces (seconds)")]
    public float decalLifetime = 5f;
    
    [Tooltip("Maximum number of active decals (older ones removed)")]
    public int maxActiveDecals = 500;
    
    [Header("Performance")]
    [Tooltip("Pool size for decal particles")]
    public int poolSize = 100;
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    // Pool management
    private Queue<GameObject> decalPool = new Queue<GameObject>();
    private List<DecalInstance> activeDecals = new List<DecalInstance>();
    
    private class DecalInstance
    {
        public GameObject gameObject;
        public float spawnTime;
    }
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePool();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void InitializePool()
    {
        if (decalParticlePrefab == null)
        {
            Debug.LogError("[ParticleDecalManager] Decal particle prefab not assigned!");
            return;
        }
        
        // Pre-create pool
        for (int i = 0; i < poolSize; i++)
        {
            GameObject decal = Instantiate(decalParticlePrefab, transform);
            decal.SetActive(false);
            decalPool.Enqueue(decal);
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[ParticleDecalManager] Initialized pool with {poolSize} decals");
        }
    }
    
    void Update()
    {
        // Clean up expired decals
        CleanupExpiredDecals();
    }
    
    /// <summary>
    /// Spawns a persistent particle decal at collision point
    /// </summary>
    public void SpawnDecal(Vector3 position, Vector3 normal, Color color, float size = 1f)
    {
        // Get decal from pool
        GameObject decal = GetDecalFromPool();
        if (decal == null) return;
        
        // Position and orient decal
        decal.transform.position = position;
        decal.transform.rotation = Quaternion.LookRotation(normal);
        decal.transform.localScale = Vector3.one * size;
        
        // Configure particle system
        ParticleSystem ps = decal.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.startColor = color;
            
            // Emit a single particle
            ps.Clear();
            ps.Emit(1);
        }
        
        // Activate decal
        decal.SetActive(true);
        
        // Track active decal
        activeDecals.Add(new DecalInstance
        {
            gameObject = decal,
            spawnTime = Time.time
        });
        
        // Enforce max decals limit
        if (activeDecals.Count > maxActiveDecals)
        {
            ReturnOldestDecalToPool();
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[ParticleDecalManager] Spawned decal at {position}, active: {activeDecals.Count}");
        }
    }
    
    private GameObject GetDecalFromPool()
    {
        if (decalPool.Count > 0)
        {
            return decalPool.Dequeue();
        }
        
        // Pool exhausted - create new decal (expand pool dynamically)
        if (decalParticlePrefab != null)
        {
            GameObject newDecal = Instantiate(decalParticlePrefab, transform);
            newDecal.SetActive(false);
            
            if (enableDebugLogs)
            {
                Debug.LogWarning($"[ParticleDecalManager] Pool exhausted, created new decal. Total: {activeDecals.Count + 1}");
            }
            
            return newDecal;
        }
        
        return null;
    }
    
    private void CleanupExpiredDecals()
    {
        float currentTime = Time.time;
        
        // Remove expired decals (iterate backwards for safe removal)
        for (int i = activeDecals.Count - 1; i >= 0; i--)
        {
            DecalInstance decal = activeDecals[i];
            
            if (currentTime - decal.spawnTime >= decalLifetime)
            {
                // Return to pool
                if (decal.gameObject != null)
                {
                    decal.gameObject.SetActive(false);
                    decalPool.Enqueue(decal.gameObject);
                }
                
                activeDecals.RemoveAt(i);
                
                if (enableDebugLogs)
                {
                    Debug.Log($"[ParticleDecalManager] Decal expired, returned to pool. Active: {activeDecals.Count}");
                }
            }
        }
    }
    
    private void ReturnOldestDecalToPool()
    {
        if (activeDecals.Count == 0) return;
        
        DecalInstance oldest = activeDecals[0];
        
        if (oldest.gameObject != null)
        {
            oldest.gameObject.SetActive(false);
            decalPool.Enqueue(oldest.gameObject);
        }
        
        activeDecals.RemoveAt(0);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[ParticleDecalManager] Max decals reached, removed oldest. Active: {activeDecals.Count}");
        }
    }
    
    /// <summary>
    /// Clear all active decals immediately
    /// </summary>
    public void ClearAllDecals()
    {
        foreach (var decal in activeDecals)
        {
            if (decal.gameObject != null)
            {
                decal.gameObject.SetActive(false);
                decalPool.Enqueue(decal.gameObject);
            }
        }
        
        activeDecals.Clear();
        
        if (enableDebugLogs)
        {
            Debug.Log("[ParticleDecalManager] Cleared all decals");
        }
    }
}
