// ============================================================================
// SHOTGUN VFX POOL SETUP - Automatic Pool Integration
// Ensures the object pool is available in every scene that needs it
// ============================================================================

using UnityEngine;

/// <summary>
/// Automatic setup component that ensures ShotgunVFXPool is available.
/// Add this to any GameObject in scenes that use shotgun VFX.
/// It will automatically create and configure the pool if it doesn't exist.
/// </summary>
public class ShotgunVFXPoolSetup : MonoBehaviour
{
    [Header("Pool Auto-Setup")]
    [Tooltip("Automatically create pool if it doesn't exist")]
    [SerializeField] private bool autoCreatePool = true;
    
    [Tooltip("Pool configuration to use when auto-creating")]
    [SerializeField] private int initialPoolSize = 10;
    [SerializeField] private int maxPoolSize = 20;
    [SerializeField] private float vfxLifetime = 3f;
    [SerializeField] private bool enableDebugMode = false;
    
    private void Awake()
    {
        if (autoCreatePool && ShotgunVFXPool.Instance == null)
        {
            CreateShotgunVFXPool();
        }
    }
    
    /// <summary>
    /// Create and configure the ShotgunVFXPool
    /// </summary>
    private void CreateShotgunVFXPool()
    {
        GameObject poolObject = new GameObject("ShotgunVFXPool");
        DontDestroyOnLoad(poolObject);
        
        ShotgunVFXPool pool = poolObject.AddComponent<ShotgunVFXPool>();
        
        // Use reflection to set the private fields (since they're SerializeField)
        var poolType = typeof(ShotgunVFXPool);
        
        var initialSizeField = poolType.GetField("initialPoolSize", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        initialSizeField?.SetValue(pool, initialPoolSize);
        
        var maxSizeField = poolType.GetField("maxPoolSize", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        maxSizeField?.SetValue(pool, maxPoolSize);
        
        var lifetimeField = poolType.GetField("vfxLifetime", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        lifetimeField?.SetValue(pool, vfxLifetime);
        
        var debugField = poolType.GetField("debugMode", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        debugField?.SetValue(pool, enableDebugMode);
        
        Debug.Log($"[ShotgunVFXPoolSetup] ✅ Auto-created ShotgunVFXPool with {initialPoolSize} initial VFX objects");
    }
    
    /// <summary>
    /// Validate that the pool is working correctly
    /// </summary>
    [ContextMenu("Test Pool")]
    private void TestPool()
    {
        if (ShotgunVFXPool.Instance == null)
        {
            Debug.LogError("[ShotgunVFXPoolSetup] ❌ No ShotgunVFXPool found!");
            return;
        }
        
        Debug.Log($"[ShotgunVFXPoolSetup] ✅ Pool Status: {ShotgunVFXPool.Instance.GetPoolStats()}");
        
        // Test getting and returning a VFX
        var testVFX = ShotgunVFXPool.Instance.GetVFX();
        if (testVFX != null)
        {
            Debug.Log("[ShotgunVFXPoolSetup] ✅ Successfully retrieved VFX from pool");
            
            // Return it immediately
            ShotgunVFXPool.Instance.ReturnVFX(testVFX);
            Debug.Log("[ShotgunVFXPoolSetup] ✅ Successfully returned VFX to pool");
        }
        else
        {
            Debug.LogError("[ShotgunVFXPoolSetup] ❌ Failed to retrieve VFX from pool");
        }
        
        Debug.Log($"[ShotgunVFXPoolSetup] Final Pool Status: {ShotgunVFXPool.Instance.GetPoolStats()}");
    }
    
    /// <summary>
    /// Force create pool (useful for testing)
    /// </summary>
    [ContextMenu("Force Create Pool")]
    private void ForceCreatePool()
    {
        if (ShotgunVFXPool.Instance != null)
        {
            Debug.LogWarning("[ShotgunVFXPoolSetup] Pool already exists, destroying old one first");
            DestroyImmediate(ShotgunVFXPool.Instance.gameObject);
        }
        
        CreateShotgunVFXPool();
    }
    
    private void OnValidate()
    {
        // Ensure reasonable values
        initialPoolSize = Mathf.Clamp(initialPoolSize, 1, 50);
        maxPoolSize = Mathf.Max(initialPoolSize, maxPoolSize);
        vfxLifetime = Mathf.Clamp(vfxLifetime, 0.5f, 10f);
    }
}