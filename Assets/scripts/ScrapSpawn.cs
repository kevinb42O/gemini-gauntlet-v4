using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// ScrapSpawn.cs - Manages scrap spawning on platforms with configurable spawn rates and amounts
/// Attach this script to platforms where you want scrap to spawn
/// </summary>
public class ScrapSpawn : MonoBehaviour
{
    [Header("Scrap Spawning Configuration")]
    [Tooltip("Percentage chance (0-100) that scrap will spawn on this platform")]
    [Range(0f, 100f)]
    public float spawnChancePercentage = 30f;
    
    [Tooltip("Minimum number of scrap items to spawn")]
    [Range(1, 20)]
    public int minScrapAmount = 1;
    
    [Tooltip("Maximum number of scrap items to spawn")]
    [Range(1, 20)]
    public int maxScrapAmount = 5;
    
    [Header("Scrap Item Configuration")]
    [Tooltip("The scrap item data to spawn (should be a ChestItemData asset)")]
    public ChestItemData scrapItemData;
    
    [Tooltip("Prefab for the scrap world item (should have ScrapItem component)")]
    public GameObject scrapItemPrefab;
    
    [Header("Spawn Area Configuration")]
    [Tooltip("Spawn scrap within this area from platform center (platforms are 3200x3200)")]
    public float spawnAreaSize = 1500f; // Smaller than platform to avoid edges
    
    [Tooltip("Height offset above platform surface to place scrap (for flat platforms)")]
    public float spawnHeightOffset = 5f;
    
    [Tooltip("Minimum distance between spawned scrap items")]
    public float minDistanceBetweenItems = 50f; // Larger for big platforms
    
    [Header("Spawn Timing")]
    [Tooltip("Delay before spawning scrap after platform becomes active")]
    public float spawnDelay = 0.5f;
    
    [Tooltip("Should scrap respawn when platform is revisited?")]
    public bool allowRespawn = false;
    
    [Header("Debug")]
    [Tooltip("Show spawn area gizmos in scene view")]
    public bool showSpawnAreaGizmos = true;
    
    [Tooltip("Enable verbose debug logging for spawn operations")]
    public bool enableDebugLogging = false;
    
    // Internal state
    private List<GameObject> spawnedScrapItems = new List<GameObject>();
    private bool hasSpawned = false;
    private CelestialPlatform celestialPlatform;
    private PlatformTrigger platformTrigger;
    private Collider triggerCollider;
    
    // Cached values for performance
    private Bounds cachedSpawnBounds;
    private bool hasCachedBounds = false;
    private float minDistanceSquared; // Cached squared distance for faster checks
    
    private void Start()
    {
        // Get reference to the platform component
        celestialPlatform = GetComponent<CelestialPlatform>();
        
        // Find the platform trigger (usually on a child object)
        platformTrigger = GetComponentInChildren<PlatformTrigger>();
        if (platformTrigger == null)
        {
            platformTrigger = GetComponent<PlatformTrigger>();
        }
        
        // Get the trigger collider for spawn bounds
        if (platformTrigger != null)
        {
            triggerCollider = platformTrigger.GetComponent<Collider>();
            if (triggerCollider != null && triggerCollider.isTrigger)
            {
                if (enableDebugLogging)
                    Debug.Log($"[ScrapSpawn] Found platform trigger collider on {gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"[ScrapSpawn] PlatformTrigger found but no trigger collider on {gameObject.name}");
            }
        }
        else
        {
            if (enableDebugLogging)
                Debug.LogWarning($"[ScrapSpawn] No PlatformTrigger found on {gameObject.name}. Will use default spawn area.");
        }
        
        // Cache spawn bounds and squared distance for performance
        cachedSpawnBounds = CalculateSpawnBounds();
        hasCachedBounds = true;
        minDistanceSquared = minDistanceBetweenItems * minDistanceBetweenItems;
        
        // Validate configuration
        if (!ValidateConfiguration())
        {
            Debug.LogError($"[ScrapSpawn] Invalid configuration on {gameObject.name}. Disabling component.", this);
            enabled = false;
            return;
        }
        
        // Subscribe to platform events if available
        SubscribeToPlatformEvents();
        
        // Spawn scrap with delay if platform is already active
        if (ShouldSpawnScrap())
        {
            Invoke(nameof(TrySpawnScrap), spawnDelay);
        }
    }
    
    private void OnDestroy()
    {
        // Clean up spawned items
        CleanupSpawnedItems();
        
        // Unsubscribe from events
        UnsubscribeFromPlatformEvents();
    }
    
    /// <summary>
    /// Validates the spawn configuration
    /// </summary>
    private bool ValidateConfiguration()
    {
        if (scrapItemData == null)
        {
            Debug.LogError($"[ScrapSpawn] No scrap item data assigned on {gameObject.name}!");
            return false;
        }
        
        if (scrapItemPrefab == null)
        {
            Debug.LogError($"[ScrapSpawn] No scrap item prefab assigned on {gameObject.name}!");
            return false;
        }
        
        // Check if prefab has ScrapItem component
        ScrapItem scrapComponent = scrapItemPrefab.GetComponent<ScrapItem>();
        if (scrapComponent == null)
        {
            Debug.LogError($"[ScrapSpawn] Scrap item prefab on {gameObject.name} must have ScrapItem component!");
            return false;
        }
        
        if (minScrapAmount > maxScrapAmount)
        {
            Debug.LogWarning($"[ScrapSpawn] Min scrap amount ({minScrapAmount}) is greater than max ({maxScrapAmount}) on {gameObject.name}. Swapping values.");
            int temp = minScrapAmount;
            minScrapAmount = maxScrapAmount;
            maxScrapAmount = temp;
        }
        
        return true;
    }
    
    /// <summary>
    /// Subscribe to platform-related events
    /// </summary>
    private void SubscribeToPlatformEvents()
    {
        // Subscribe to player landing events
        PlayerMovementManager.OnPlayerLandedOnNewPlatform += OnPlayerLandedOnPlatform;
    }
    
    /// <summary>
    /// Unsubscribe from platform events
    /// </summary>
    private void UnsubscribeFromPlatformEvents()
    {
        PlayerMovementManager.OnPlayerLandedOnNewPlatform -= OnPlayerLandedOnPlatform;
    }
    
    /// <summary>
    /// Called when player lands on a platform
    /// </summary>
    private void OnPlayerLandedOnPlatform(Transform platformTransform)
    {
        // Check if this is our platform
        if (platformTransform == transform)
        {
            if (enableDebugLogging)
                Debug.Log($"[ScrapSpawn] Player landed on platform {gameObject.name}");
            
            if (ShouldSpawnScrap())
            {
                Invoke(nameof(TrySpawnScrap), spawnDelay);
            }
        }
    }
    
    /// <summary>
    /// Determines if scrap should spawn based on current conditions
    /// </summary>
    private bool ShouldSpawnScrap()
    {
        // Don't spawn if already spawned and respawn is disabled
        if (hasSpawned && !allowRespawn)
        {
            return false;
        }
        
        // Don't spawn if there are still active scrap items
        if (HasActiveScrapItems())
        {
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Attempts to spawn scrap based on spawn chance
    /// </summary>
    public void TrySpawnScrap()
    {
        if (!ShouldSpawnScrap())
        {
            return;
        }
        
        // Roll for spawn chance
        float roll = Random.Range(0f, 100f);
        if (roll > spawnChancePercentage)
        {
            if (enableDebugLogging)
                Debug.Log($"[ScrapSpawn] Spawn chance failed on {gameObject.name}. Rolled {roll:F1}%, needed {spawnChancePercentage}%");
            return;
        }
        
        if (enableDebugLogging)
            Debug.Log($"[ScrapSpawn] Spawn chance succeeded on {gameObject.name}. Rolled {roll:F1}%, needed {spawnChancePercentage}%");
        
        // Determine how many scrap items to spawn
        int scrapCount = Random.Range(minScrapAmount, maxScrapAmount + 1);
        
        // Spawn the scrap items
        SpawnScrapItems(scrapCount);
        
        hasSpawned = true;
    }
    
    /// <summary>
    /// Spawns the specified number of scrap items as static objects on platform surface
    /// </summary>
    private void SpawnScrapItems(int count)
    {
        if (enableDebugLogging)
            Debug.Log($"[ScrapSpawn] Spawning {count} static scrap items on platform {gameObject.name}");
        
        List<Vector3> spawnPositions = GenerateSpawnPositions(count);
        
        for (int i = 0; i < spawnPositions.Count; i++)
        {
            Vector3 spawnPos = spawnPositions[i];
            
            // Create random rotation for natural placement
            Quaternion randomRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            
            // Instantiate scrap item at natural prefab size
            GameObject scrapObj = Instantiate(scrapItemPrefab, spawnPos, randomRotation);
            
            // CRITICAL: Parent scrap to the platform so it moves with the platform
            scrapObj.transform.SetParent(transform, true);
            
            // Configure the scrap item
            ScrapItem scrapComponent = scrapObj.GetComponent<ScrapItem>();
            if (scrapComponent != null)
            {
                scrapComponent.SetScrapData(scrapItemData, 1);
                scrapComponent.SetParentSpawner(this);
                
                // Disable visual effects for static placement
                scrapComponent.enableBobbing = false;
                scrapComponent.enableRotation = false;
            }
            
            // Add to our tracking list
            spawnedScrapItems.Add(scrapObj);
            
            if (enableDebugLogging)
                Debug.Log($"[ScrapSpawn] Spawned static scrap {i + 1}/{count} at {spawnPos} with rotation {randomRotation.eulerAngles.y:F1}°");
        }
    }
    
    /// <summary>
    /// Generates valid spawn positions for scrap items on flat platform surface (optimized - no raycasting!)
    /// </summary>
    private List<Vector3> GenerateSpawnPositions(int count)
    {
        List<Vector3> positions = new List<Vector3>();
        int maxAttempts = count * 5; // Reduced from 15 to 5 - still plenty for flat platforms
        int attempts = 0;
        
        // Use cached spawn bounds for performance
        Bounds spawnBounds = hasCachedBounds ? cachedSpawnBounds : CalculateSpawnBounds();
        
        if (enableDebugLogging)
            Debug.Log($"[ScrapSpawn] Generating {count} spawn positions on platform {gameObject.name} within bounds: {spawnBounds.size}");
        
        // For cylindrical platforms, we can use radius-based spawning
        float platformRadius = Mathf.Min(spawnBounds.extents.x, spawnBounds.extents.z);
        float spawnRadius = platformRadius * 0.85f; // Stay 15% away from edges
        
        while (positions.Count < count && attempts < maxAttempts)
        {
            attempts++;
            
            // Generate random position within circular area (better for cylindrical platforms)
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(0f, spawnRadius);
            
            float randomX = Mathf.Cos(angle) * distance;
            float randomZ = Mathf.Sin(angle) * distance;
            
            // For flat platforms, just use the platform height + small offset (NO RAYCASTING!)
            Vector3 spawnPos = spawnBounds.center + new Vector3(randomX, spawnHeightOffset, randomZ);
            
            // Check if position is valid (not too close to other items)
            if (IsValidSpawnPosition(spawnPos, positions))
            {
                positions.Add(spawnPos);
                if (enableDebugLogging)
                    Debug.Log($"[ScrapSpawn] Generated spawn position {positions.Count}/{count} at {spawnPos}");
            }
        }
        
        if (enableDebugLogging)
            Debug.Log($"[ScrapSpawn] Generated {positions.Count}/{count} spawn positions after {attempts} attempts");
        
        if (positions.Count < count)
        {
            Debug.LogWarning($"[ScrapSpawn] Could only find {positions.Count} valid spawn positions out of {count} requested on {gameObject.name}");
        }
        
        return positions;
    }
    
    /// <summary>
    /// Calculates spawn bounds from trigger collider or uses default area (called once and cached)
    /// </summary>
    private Bounds CalculateSpawnBounds()
    {
        if (triggerCollider != null)
        {
            // Use the trigger collider bounds
            Bounds bounds = triggerCollider.bounds;
            if (enableDebugLogging)
                Debug.Log($"[ScrapSpawn] Using trigger collider bounds: center={bounds.center}, size={bounds.size}");
            return bounds;
        }
        else
        {
            // Fallback to default spawn area
            Bounds bounds = new Bounds(transform.position, new Vector3(spawnAreaSize * 2f, 100f, spawnAreaSize * 2f));
            if (enableDebugLogging)
                Debug.Log($"[ScrapSpawn] Using default spawn area bounds: center={bounds.center}, size={bounds.size}");
            return bounds;
        }
    }
    
    /// <summary>
    /// Checks if a spawn position is valid (not too close to existing items)
    /// Uses squared distance for better performance (avoids sqrt calculation)
    /// </summary>
    private bool IsValidSpawnPosition(Vector3 position, List<Vector3> existingPositions)
    {
        // Early exit if no existing positions
        if (existingPositions.Count == 0)
            return true;
        
        // Use squared distance to avoid expensive Vector3.Distance (which uses sqrt)
        foreach (Vector3 existingPos in existingPositions)
        {
            float sqrDistance = (position - existingPos).sqrMagnitude;
            if (sqrDistance < minDistanceSquared)
            {
                return false;
            }
        }
        return true;
    }
    
    /// <summary>
    /// Checks if there are still active scrap items from this spawner
    /// </summary>
    private bool HasActiveScrapItems()
    {
        // Clean up null references (destroyed items)
        spawnedScrapItems.RemoveAll(item => item == null);
        
        return spawnedScrapItems.Count > 0;
    }
    
    /// <summary>
    /// Called by ScrapItem when it's collected
    /// </summary>
    public void OnScrapItemCollected(GameObject scrapItem)
    {
        spawnedScrapItems.Remove(scrapItem);
        Debug.Log($"[ScrapSpawn] Scrap item collected. Remaining items: {spawnedScrapItems.Count}");
    }
    
    /// <summary>
    /// Forces scrap to spawn regardless of chance (for testing)
    /// </summary>
    [ContextMenu("Force Spawn Scrap")]
    public void ForceSpawnScrap()
    {
        if (!ValidateConfiguration())
        {
            Debug.LogError("[ScrapSpawn] Cannot force spawn - invalid configuration!");
            return;
        }
        
        CleanupSpawnedItems();
        hasSpawned = false;
        
        int scrapCount = Random.Range(minScrapAmount, maxScrapAmount + 1);
        SpawnScrapItems(scrapCount);
        hasSpawned = true;
    }
    
    /// <summary>
    /// Clears all spawned scrap items
    /// </summary>
    [ContextMenu("Clear All Scrap")]
    public void ClearAllScrap()
    {
        CleanupSpawnedItems();
        hasSpawned = false;
    }
    
    /// <summary>
    /// Cleans up all spawned scrap items
    /// </summary>
    private void CleanupSpawnedItems()
    {
        foreach (GameObject item in spawnedScrapItems)
        {
            if (item != null)
            {
                DestroyImmediate(item);
            }
        }
        spawnedScrapItems.Clear();
    }
    
    /// <summary>
    /// Resets the spawn state to allow respawning
    /// </summary>
    public void ResetSpawnState()
    {
        hasSpawned = false;
        Debug.Log($"[ScrapSpawn] Spawn state reset on {gameObject.name}");
    }
    
    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        if (!showSpawnAreaGizmos) return;
        
        // Get spawn bounds (use cached if available)
        Bounds spawnBounds = hasCachedBounds ? cachedSpawnBounds : CalculateSpawnBounds();
        
        // Draw spawn area from trigger bounds or default
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(spawnBounds.center, new Vector3(spawnBounds.size.x, 1f, spawnBounds.size.z));
        
        // Draw trigger collider bounds if available
        if (triggerCollider != null)
        {
            Gizmos.color = Color.cyan;
            Bounds triggerBounds = triggerCollider.bounds;
            Gizmos.DrawWireCube(triggerBounds.center, triggerBounds.size);
        }
        
        // Draw spawn height offset
        Gizmos.color = Color.green;
        Vector3 spawnHeightPos = spawnBounds.center + Vector3.up * spawnHeightOffset;
        Gizmos.DrawWireSphere(spawnHeightPos, 10f); // Small sphere to show spawn height
        
        // Draw minimum distance circles for existing items
        if (Application.isPlaying && spawnedScrapItems != null)
        {
            Gizmos.color = Color.red;
            foreach (GameObject item in spawnedScrapItems)
            {
                if (item != null)
                {
                    Gizmos.DrawWireSphere(item.transform.position, minDistanceBetweenItems);
                }
            }
        }
        
        // Draw labels
        #if UNITY_EDITOR
        string boundsSource = triggerCollider != null ? "Trigger Bounds" : "Default Area";
        Handles.Label(spawnBounds.center + Vector3.up * 10f, $"Spawn Area ({boundsSource}): {spawnBounds.size.x:F0}x{spawnBounds.size.z:F0}");
        if (triggerCollider != null)
        {
            Handles.Label(spawnBounds.center + Vector3.up * 20f, $"Using Platform Trigger Collider");
        }
        #endif
    }
}
