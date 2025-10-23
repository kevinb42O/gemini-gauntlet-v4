using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using GeminiGauntlet.Audio;
using GeminiGauntlet.Missions.Integration;
using GeminiGauntlet.Progression;

/// <summary> 
/// Manages chest spawning when all towers on a platform are destroyed.
/// Spawns chest prefabs at the center of cleared platforms.
/// </summary>
public class ChestManager : MonoBehaviour
{
    [System.Serializable]
    public class PlatformChestMapping
    {
        [Tooltip("Platform type identifier (e.g., 'Rocky', 'Crystal', 'Metal')")]
        public string platformType;
        
        [Tooltip("Chest prefab to spawn for this platform type")]
        public GameObject chestPrefab;
        
        [Tooltip("Keywords to identify this platform type (checks platform name)")]
        public string[] platformKeywords;
    }
    
    [Header("Platform-Specific Chest Spawning")]
    [Tooltip("Define which chest spawns on which platform type")]
    public PlatformChestMapping[] platformChestMappings = new PlatformChestMapping[3]
    {
        new PlatformChestMapping { platformType = "Type 1", platformKeywords = new string[] { "platform1", "rocky", "stone" } },
        new PlatformChestMapping { platformType = "Type 2", platformKeywords = new string[] { "platform2", "crystal", "ice" } },
        new PlatformChestMapping { platformType = "Type 3", platformKeywords = new string[] { "platform3", "metal", "tech" } }
    };
    
    [Header("Fallback Chest")]
    [Tooltip("Default chest prefab if no specific mapping found")]
    public GameObject fallbackChestPrefab;
    
    [Tooltip("Height above platform surface to spawn chest")]
    public float chestSpawnHeight = 0.5f;
    
    [Header("Layer Configuration")]
    [Tooltip("Layer mask for platform detection - should include Platform layer")]
    public LayerMask platformLayerMask = -1; // Default to all layers
    
    public float spawnDelay = 1f;
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    
    // Track platforms by Transform references
    // Tracking dictionaries for the new system
    private Dictionary<Transform, int> platformTowerCount = new Dictionary<Transform, int>();
    private Dictionary<Transform, int> platformTowerDeaths = new Dictionary<Transform, int>();
    private Dictionary<Transform, GameObject> spawnedChests = new Dictionary<Transform, GameObject>();
    
    // Safety flag to prevent recursive activation attempts
    private bool isReactivating = false;
    
    void OnEnable()
    {
        // Subscribe to tower death events
        TowerController.OnTowerDeath += OnTowerDied;
        DebugLog($"ChestManager: OnTowerDeath event has {TowerController.OnTowerDeath?.GetInvocationList().Length ?? 0} subscribers");
        
        // DEFENSIVE: Ensure this GameObject stays active
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
        }
    }
    
    void OnDisable()
    {
        // Unsubscribe from tower death events
        DebugLog("ChestManager: Unsubscribed from tower death events");
        
        // Log stack trace to identify what's disabling ChestManager
        
        // EMERGENCY FIX: Try to reactivate the GameObject immediately
        if (!gameObject.activeInHierarchy && !isReactivating)
        {
            try
            {
                isReactivating = true;
                // Check if we're not already in the middle of activation/deactivation
                if (gameObject != null && !gameObject.activeSelf)
                {
                    // Direct reactivation (no coroutine needed since GameObject is inactive)
                    gameObject.SetActive(true);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ChestManager] Failed to reactivate ChestManager: {ex.Message}");
                // Last resort: Find or create a new ChestManager
                CreateBackupChestManager();
            }
            finally
            {
                isReactivating = false;
            }
        }
    }
    
    void Start()
    {
        // CRITICAL: Make ChestManager persistent and prevent destruction
        DontDestroyOnLoad(gameObject);
        
        // DEFENSIVE: Prevent multiple instances
        ChestManager[] existingManagers = FindObjectsOfType<ChestManager>();
        if (existingManagers.Length > 1)
        {
            for (int i = 1; i < existingManagers.Length; i++)
            {
                if (existingManagers[i] != this)
                {
                    Destroy(existingManagers[i].gameObject);
                }
            }
        }
        
        // DEFENSIVE: Force re-subscription in case OnEnable was missed
        TowerController.OnTowerDeath -= OnTowerDied; // Remove any duplicate subscriptions
        TowerController.OnTowerDeath += OnTowerDied; // Re-subscribe
        
        // Initialize platform tracking by scanning existing towers
        InitializePlatformTracking();
        
        // Validate chest prefabs
        ValidateChestPrefabs();
        
        // Debug: Check if we're properly subscribed
        DebugLog($"ChestManager: Start() - OnTowerDeath event has {TowerController.OnTowerDeath?.GetInvocationList().Length ?? 0} subscribers");
        DebugLog($"ChestManager: Tracking {platformTowerCount.Count} platforms with towers");
        
        // FAILSAFE: Start periodic health check to ensure we stay subscribed
        InvokeRepeating(nameof(HealthCheck), 5f, 10f); // Check every 10 seconds after 5 second delay
    }
    
    void InitializePlatformTracking()
    {
        DebugLog("ChestManager: Initializing platform tracking...");
        
        // Find all active towers and initialize the new tracking system
        foreach (TowerController tower in TowerController.ActiveTowers)
        {
            if (tower != null && tower._associatedPlatformTransform != null)
            {
                Transform platform = tower._associatedPlatformTransform;
                
                // Initialize new tracking system with platform resolution
                Transform resolvedPlatform = ResolvePlatformReference(platform);
                if (!platformTowerCount.ContainsKey(resolvedPlatform))
                {
                    platformTowerCount[resolvedPlatform] = 0;
                    platformTowerDeaths[resolvedPlatform] = 0;
                }
                
                platformTowerCount[resolvedPlatform]++;
                DebugLog($"ChestManager: Pre-existing tower '{tower.name}' registered on platform '{resolvedPlatform.name}' (resolved from '{platform.name}') - Total: {platformTowerCount[resolvedPlatform]}");
                

            }
        }
        
        DebugLog($"ChestManager: Tracking {platformTowerCount.Count} platforms with towers");
        
        // If no towers found at initialization, that's normal - towers spawn dynamically
        if (platformTowerCount.Count == 0)
        {
            DebugLog("ChestManager: No pre-existing towers found - will track towers as they spawn dynamically");
        }
    }
    
    /// <summary>
    /// Resolve platform reference consistently - ensures we always use the same Transform for tracking
    /// Normalizes both trigger and actual platform objects to the same tracking reference
    /// </summary>
    Transform ResolvePlatformReference(Transform platform)
    {
        if (platform == null) return null;
        
        // CRITICAL: Normalize platform references to handle trigger/actual platform mismatches
        Transform normalizedPlatform = platform;
        
        // If this is a trigger, try to find the actual platform
        if (platform.name.ToLower().Contains("trigger") || platform.GetComponent<PlatformTrigger>() != null)
        {
            if (platform.parent != null)
            {
                normalizedPlatform = platform.parent;
                DebugLog($"ChestManager: 🔄 Normalized trigger '{platform.name}' to actual platform '{normalizedPlatform.name}'");
            }
            else
            {
                DebugLog($"ChestManager: ⚠️ Trigger '{platform.name}' has no parent - using trigger as platform");
            }
        }
        // If this might be an actual platform, check if we have a corresponding trigger in our tracking
        else
        {
            // Check if we're already tracking a trigger version of this platform
            string platformName = platform.name;
            Transform triggerVersion = null;
            
            // Look for a trigger child that might be the tracked version
            foreach (Transform child in platform)
            {
                if (child.name.ToLower().Contains("trigger") || child.GetComponent<PlatformTrigger>() != null)
                {
                    if (platformTowerCount.ContainsKey(child) || platformTowerDeaths.ContainsKey(child))
                    {
                        triggerVersion = child;
                        break;
                    }
                }
            }
            
            if (triggerVersion != null)
            {
                normalizedPlatform = triggerVersion;
                DebugLog($"ChestManager: 🔄 Using existing trigger tracking '{triggerVersion.name}' instead of actual platform '{platform.name}'");
            }
            else
            {
                DebugLog($"ChestManager: ✅ Using actual platform '{platform.name}' (no trigger tracking found)");
            }
        }
        
        return normalizedPlatform;
    }
    
    /// <summary>
    /// Helper method to find a platform by name in the scene
    /// </summary>
    Transform FindPlatformByName(string name)
    {
        // Search all GameObjects in the scene for the platform name
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name == name)
            {
                return obj.transform;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Initialize tracking for a specific platform when we first encounter it
    /// </summary>
    void InitializePlatformTracking(Transform targetPlatform)
    {
        DebugLog($"ChestManager: Initializing tracking for specific platform '{targetPlatform.name}'...");
        
        // Find all active towers on this specific platform
        var towersOnPlatform = TowerController.ActiveTowers
            .Where(tower => tower != null && tower._associatedPlatformTransform == targetPlatform)
            .ToList();
        
        if (towersOnPlatform.Count > 0)
        {
            DebugLog($"ChestManager: Tracked {towersOnPlatform.Count} towers on platform '{targetPlatform.name}'");
            
            foreach (var tower in towersOnPlatform)
            {
                DebugLog($"  - Tower '{tower.name}' on platform '{targetPlatform.name}'");
            }
        }
        else
        {
            DebugLog($"ChestManager: No active towers found on platform '{targetPlatform.name}'");
        }
    }
    
    /// <summary>
    /// Calculate the local center position of a platform for chest spawning (relative to platform)
    /// </summary>
    Vector3 CalculateLocalPlatformCenter(Transform platform)
    {
        // Get platform collider for bounds calculation
        Collider platformCollider = platform.GetComponent<Collider>();
        
        if (platformCollider != null)
        {
            // Use collider bounds to find center and surface
            Bounds bounds = platformCollider.bounds;
            
            // FIXED APPROACH: Calculate surface position properly
            // 1. Get platform center in local space
            Vector3 localCenter = Vector3.zero; // Platform center is at (0,0,0) in local space
            
            // 2. Calculate platform height in local space
            // Get the top of the platform bounds in world space, then convert to local
            Vector3 worldSurfacePoint = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
            Vector3 localSurfacePoint = platform.InverseTransformPoint(worldSurfacePoint);
            
            // 3. Set chest position: platform center (X,Z) + surface height (Y) + spawn height
            Vector3 chestLocalPos = new Vector3(
                localCenter.x,  // Center X
                localSurfacePoint.y + chestSpawnHeight,  // Surface Y + spawn height
                localCenter.z   // Center Z
            );
            
            DebugLog($"ChestManager: Platform bounds: {bounds}");
            DebugLog($"ChestManager: World surface point: {worldSurfacePoint}");
            DebugLog($"ChestManager: Local surface point: {localSurfacePoint}");
            DebugLog($"ChestManager: Chest spawn height: {chestSpawnHeight}");
            DebugLog($"ChestManager: Final local position: {chestLocalPos}");
            
            return chestLocalPos;
        }
        
        // Fallback: spawn at platform local origin with height offset
        Vector3 fallbackPos = new Vector3(0f, chestSpawnHeight, 0f);
        DebugLog($"ChestManager: Using fallback local position: {fallbackPos}");
        return fallbackPos;
    }
    

    
    /// <summary>
    /// Register a tower when it spawns on a platform
    /// </summary>
    public void RegisterTowerSpawn(TowerController tower, Transform platform)
    {
        // Resolve platform reference consistently
        Transform resolvedPlatform = ResolvePlatformReference(platform);
        
        // Initialize platform tracking if this is the first tower on this platform
        if (!platformTowerCount.ContainsKey(resolvedPlatform))
        {
            platformTowerCount[resolvedPlatform] = 0;
            platformTowerDeaths[resolvedPlatform] = 0;
            DebugLog($"ChestManager: 🆕 First tower on platform '{resolvedPlatform.name}' (resolved from '{platform.name}') - initializing tracking");
        }
        
        // Increment the tower count for this platform
        platformTowerCount[resolvedPlatform]++;
        DebugLog($"ChestManager: 🏰 Tower registered on platform '{resolvedPlatform.name}' - Total: {platformTowerCount[resolvedPlatform]}");
    }
    
    void OnTowerDied(TowerController tower)
    {
        DebugLog($"ChestManager: 🔥 TOWER DEATH EVENT RECEIVED! Tower: {tower.name}");
        
        Transform platform = tower._associatedPlatformTransform;
        if (platform == null)
        {
            DebugLog($"ChestManager: ⚠️ Tower '{tower.name}' has no associated platform - cannot spawn chest");
            return;
        }
        
        // Resolve platform reference consistently (same as registration)
        Transform resolvedPlatform = ResolvePlatformReference(platform);
        DebugLog($"ChestManager: 💀 Tower '{tower.name}' died on platform '{resolvedPlatform.name}' (resolved from '{platform.name}')");
        
        // Increment death count for this platform
        if (!platformTowerDeaths.ContainsKey(resolvedPlatform))
        {
            platformTowerDeaths[resolvedPlatform] = 0;
        }
        platformTowerDeaths[resolvedPlatform]++;
        
        // Check if all towers on this platform are destroyed
        bool allTowersDestroyed = VerifyAllTowersDestroyedOnPlatform(resolvedPlatform);
        
        if (allTowersDestroyed)
        {
            DebugLog($"ChestManager: ✅ ROBUST VERIFICATION: ALL towers destroyed on platform '{resolvedPlatform.name}'! Starting chest emergence...");
            
            // Check if chest already spawned for this platform
            if (spawnedChests.ContainsKey(resolvedPlatform))
            {
                DebugLog($"ChestManager: Chest already spawned for platform '{resolvedPlatform.name}' - skipping");
                return;
            }
            
            // Start chest emergence with delay
            StartCoroutine(EmergeChestAfterDelay(resolvedPlatform));
        }
        else
        {
            DebugLog($"ChestManager: ⏳ Still towers alive on platform '{resolvedPlatform.name}' - chest will not spawn yet");
        }
    }
    
    /// <summary>
    /// ROBUST VERIFICATION: Check if all towers on a platform are destroyed
    /// Uses direct active tower checking instead of unreliable counters
    /// </summary>
    bool VerifyAllTowersDestroyedOnPlatform(Transform platform)
    {
        DebugLog($"ChestManager: 🔍 ROBUST VERIFICATION: Checking active towers on platform '{platform.name}'...");
        
        if (platform == null) 
        {
            DebugLog($"ChestManager: Platform is null - cannot verify towers");
            return false;
        }
        
        // Count ACTIVE (living) towers on this platform by checking TowerController.ActiveTowers
        int activeTowersOnPlatform = 0;
        int totalTowersFound = 0;
        
        foreach (TowerController tower in TowerController.ActiveTowers)
        {
            if (tower != null)
            {
                Transform towerPlatform = tower._associatedPlatformTransform;
                if (towerPlatform != null)
                {
                    Transform resolvedTowerPlatform = ResolvePlatformReference(towerPlatform);
                    if (resolvedTowerPlatform == platform)
                    {
                        totalTowersFound++;
                        if (!tower.IsDead)
                        {
                            activeTowersOnPlatform++;
                            DebugLog($"ChestManager: Found LIVING tower '{tower.name}' on platform '{platform.name}'");
                        }
                        else
                        {
                            DebugLog($"ChestManager: Found DEAD tower '{tower.name}' on platform '{platform.name}'");
                        }
                    }
                }
            }
        }
        
        bool allDestroyed = (activeTowersOnPlatform == 0) && (totalTowersFound > 0);
        
        DebugLog($"ChestManager: 📊 Platform '{platform.name}' DIRECT verification:");
        DebugLog($"  - Total towers found: {totalTowersFound}");
        DebugLog($"  - Active (living) towers: {activeTowersOnPlatform}");
        DebugLog($"  - All towers destroyed: {allDestroyed}");
        
        return allDestroyed;
    }
    
    /// <summary>
    /// Helper method to get full hierarchy path for debugging
    /// </summary>
    string GetFullPath(Transform transform)
    {
        string path = transform.name;
        Transform parent = transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }
    
    System.Collections.IEnumerator EmergeChestAfterDelay(Transform platform)
    {
        yield return new WaitForSeconds(spawnDelay);
        
        // Double-check that platform still exists
        if (platform == null)
        {
            DebugLog("ChestManager: Platform destroyed before chest could emerge");
            yield break;
        }
        
        // Check if chest already spawned for this platform
        if (spawnedChests.ContainsKey(platform))
        {
            DebugLog($"ChestManager: Chest already spawned for platform '{platform.name}' - skipping spawn");
            yield break;
        }
        
        SpawnChest(platform);
    }
    
    void SpawnChest(Transform platform)
    {
        // Get the appropriate chest prefab for this platform type
        GameObject selectedChestPrefab = GetChestPrefabForPlatform(platform);
        
        if (selectedChestPrefab == null)
        {
            DebugLog($"ChestManager: ❌ No valid chest prefab found for platform '{platform.name}' - cannot spawn chest!");
            return;
        }
        
        DebugLog($"ChestManager: 🎁 Selected chest prefab '{selectedChestPrefab.name}' for platform '{platform.name}'");
        
        // CRITICAL FIX: Find the actual platform surface for chest parenting
        Transform actualPlatformSurface = FindActualPlatformSurface(platform);
        if (actualPlatformSurface == null)
        {
            return;
        }
        
        DebugLog($"ChestManager: 🎁 Spawning chest at platform '{platform.name}' -> actual surface: '{actualPlatformSurface.name}'");
        
        // Instantiate chest prefab at origin first (we'll position it properly after parenting)
        GameObject spawnedChest = Instantiate(selectedChestPrefab, Vector3.zero, Quaternion.identity);
        
        // Ensure chest is active immediately
        spawnedChest.SetActive(true);
        
        // Store original prefab scale to preserve chest dimensions
        Vector3 originalPrefabScale = spawnedChest.transform.localScale;
        DebugLog($"ChestManager: Original prefab scale: {originalPrefabScale}");
        
        // SIMPLE FIX: Parent with worldPositionStays=true to preserve prefab scale completely
        spawnedChest.transform.SetParent(actualPlatformSurface, true);
        
        // DON'T touch the scale at all - let the prefab keep its original dimensions
        DebugLog($"ChestManager: Chest scale preserved as: {spawnedChest.transform.localScale}");
        
        // Calculate proper local position on actual platform surface
        Vector3 localSpawnPosition = CalculateLocalPlatformCenter(actualPlatformSurface);
        
        // CRITICAL: Adjust position so chest base sits on platform surface
        // Account for chest bounds - move chest up by half its height so base touches surface
        Renderer chestRenderer = spawnedChest.GetComponent<Renderer>();
        if (chestRenderer != null)
        {
            // Get chest bounds in local space
            Bounds chestBounds = chestRenderer.bounds;
            float chestHalfHeight = chestBounds.size.y * 0.5f;
            
            // Adjust Y position so bottom of chest sits on surface instead of center
            localSpawnPosition.y += chestHalfHeight;
            DebugLog($"ChestManager: Chest bounds height: {chestBounds.size.y}, adjusted Y by: {chestHalfHeight}");
        }
        else
        {
            DebugLog($"ChestManager: No renderer found on chest - using default positioning");
        }
        
        spawnedChest.transform.localPosition = localSpawnPosition;
        
        DebugLog($"ChestManager: Chest positioned - Local: {localSpawnPosition}, World: {spawnedChest.transform.position}, Scale: {spawnedChest.transform.localScale}");
        
        // Force chest to be visible and check all renderers
        EnsureChestVisibility(spawnedChest);
        
        // Register spawned chest
        spawnedChests[platform] = spawnedChest;
        
        // 🎯 MISSION TRACKING: Platform conquered (all towers destroyed -> chest emerged)
        DebugLog($"ChestManager: 🎯 MISSION TRACKING - Platform conquered! Updating mission progress...");
        MissionProgressHooks.OnPlatformConquered();
        
        // 🎯 XP TRACKING: Grant XP for platform conquest
        DebugLog($"ChestManager: 🎯 XP TRACKING - Platform conquered! Granting XP...");
        GeminiGauntlet.Progression.XPHooks.OnPlatformConquered();
        
        // Configure ChestController if present
        ChestController chestController = spawnedChest.GetComponent<ChestController>();
        if (chestController != null)
        {
            chestController.SetPlatformAssociation(platform);
            
            // CRITICAL: Start immediate emergence to make chest visible (changes state from Hidden)
            chestController.StartImmediateEmergence();
            
            DebugLog($"ChestManager: ✅ ChestController configured and emergence started for spawned chest");
        }
        else
        {
            DebugLog($"ChestManager: ⚠️ No ChestController found on spawned chest - chest may not function properly");
        }
        
        // Play spawn sound with fallback
        PlayChestSpawnSound(spawnedChest.transform.position);
        
        // CRITICAL: Notify TowerSpawner that chest has emerged - PERMANENTLY disable tower spawning
        NotifyTowerSpawnerChestEmerged(platform);
        
        DebugLog($"ChestManager: ✅ Chest spawned successfully for platform '{platform.name}' at final position: {spawnedChest.transform.position}");
        
        // Final verification
        VerifyChestSpawn(spawnedChest, platform);
    }
    
    /// <summary>
    /// Ensure chest is visible by checking all renderers and components
    /// </summary>
    void EnsureChestVisibility(GameObject chest)
    {
        // Check main renderer
        Renderer mainRenderer = chest.GetComponent<Renderer>();
        if (mainRenderer != null)
        {
            mainRenderer.enabled = true;
            DebugLog($"ChestManager: Main renderer enabled - bounds: {mainRenderer.bounds}");
        }
        else
        {
            DebugLog($"ChestManager: No main renderer found on chest");
        }
        
        // Check all child renderers
        Renderer[] childRenderers = chest.GetComponentsInChildren<Renderer>();
        DebugLog($"ChestManager: Found {childRenderers.Length} total renderers (including children)");
        
        foreach (Renderer renderer in childRenderers)
        {
            renderer.enabled = true;
            DebugLog($"ChestManager: Enabled renderer on '{renderer.gameObject.name}' - bounds: {renderer.bounds}");
        }
        
        // Check for MeshRenderer specifically
        MeshRenderer meshRenderer = chest.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.enabled = true;
            DebugLog($"ChestManager: MeshRenderer enabled - material: {(meshRenderer.material != null ? meshRenderer.material.name : "NULL")}");
        }
        
        // Check colliders
        Collider chestCollider = chest.GetComponent<Collider>();
        if (chestCollider != null)
        {
            DebugLog($"ChestManager: Chest collider found - enabled: {chestCollider.enabled}, bounds: {chestCollider.bounds}");
        }
    }
    
    /// <summary>
    /// Play chest spawn sound with fallback options
    /// </summary>
    void PlayChestSpawnSound(Vector3 position)
    {
        try
        {
            // Try main GameSounds system
            GameSounds.PlayGemSpawn(position);
            DebugLog($"ChestManager: 🔊 Played chest spawn sound via GameSounds at {position}");
        }
        catch (System.Exception ex)
        {
            
            // Fallback: Try WeaponCrateSoundManager if available
            WeaponCrateSoundManager crateSoundManager = FindFirstObjectByType<WeaponCrateSoundManager>();
            if (crateSoundManager != null)
            {
                try
                {
                    crateSoundManager.PlayCrateEmergence();
                    DebugLog($"ChestManager: 🔊 Played chest spawn sound via WeaponCrateSoundManager fallback");
                }
                catch (System.Exception fallbackEx)
                {
                }
            }
            else
            {
            }
        }
    }
    
    /// <summary>
    /// Final verification that chest spawned correctly
    /// </summary>
    void VerifyChestSpawn(GameObject chest, Transform platform)
    {
        if (chest == null)
        {
            return;
        }
        
        if (!chest.activeInHierarchy)
        {
            chest.SetActive(true);
        }
        
        Vector3 chestPosition = chest.transform.position;
        Vector3 platformPosition = platform.position;
        float distance = Vector3.Distance(chestPosition, platformPosition);
        
        DebugLog($"ChestManager: 📍 Final verification:");
        DebugLog($"  - Chest world position: {chestPosition}");
        DebugLog($"  - Chest local position: {chest.transform.localPosition}");
        DebugLog($"  - Chest scale: {chest.transform.localScale}");
        DebugLog($"  - Platform position: {platformPosition}");
        DebugLog($"  - Distance from platform: {distance}");
        DebugLog($"  - Chest active: {chest.activeInHierarchy}");
        DebugLog($"  - Chest parent: {(chest.transform.parent != null ? chest.transform.parent.name : "NULL")}");
        
        // Check if chest is too far from platform (might indicate positioning issue)
        // For local positioning, distance should be reasonable (chest should be near platform center)
        if (distance > 50f)
        {
            
            // Additional debug for local positioning
            Vector3 localPos = chest.transform.localPosition;
            float localDistance = localPos.magnitude;
            DebugLog($"ChestManager: Local position magnitude: {localDistance} (should be small for proper local positioning)");
        }
    }
    

    
    /// <summary>
    /// Get the appropriate chest prefab for a specific platform type
    /// </summary>
    GameObject GetChestPrefabForPlatform(Transform platform)
    {
        if (platform == null)
        {
            DebugLog("ChestManager: Platform is null - using fallback chest");
            return fallbackChestPrefab;
        }
        
        string platformName = platform.name.ToLower();
        DebugLog($"ChestManager: 🔍 Determining chest type for platform '{platform.name}'");
        
        // Check each mapping to find matching platform type
        foreach (var mapping in platformChestMappings)
        {
            if (mapping.chestPrefab == null) continue;
            
            // Check if any keywords match the platform name
            if (mapping.platformKeywords != null)
            {
                foreach (string keyword in mapping.platformKeywords)
                {
                    if (!string.IsNullOrEmpty(keyword) && platformName.Contains(keyword.ToLower()))
                    {
                        DebugLog($"ChestManager: ✅ Found match! Platform '{platform.name}' contains keyword '{keyword}' -> Using '{mapping.platformType}' chest: {mapping.chestPrefab.name}");
                        return mapping.chestPrefab;
                    }
                }
            }
        }
        
        // No specific match found - use fallback
        DebugLog($"ChestManager: ⚠️ No specific chest mapping found for platform '{platform.name}' - using fallback chest");
        return fallbackChestPrefab;
    }
    
    /// <summary>
    /// Validate that chest prefabs are properly assigned
    /// </summary>
    void ValidateChestPrefabs()
    {
        DebugLog("ChestManager: 🔧 Validating chest prefab assignments...");
        
        int validMappings = 0;
        for (int i = 0; i < platformChestMappings.Length; i++)
        {
            var mapping = platformChestMappings[i];
            if (mapping.chestPrefab != null)
            {
                validMappings++;
                DebugLog($"ChestManager: ✅ {mapping.platformType}: {mapping.chestPrefab.name} (Keywords: {string.Join(", ", mapping.platformKeywords ?? new string[0])})");
            }
            else
            {
                DebugLog($"ChestManager: ❌ {mapping.platformType}: NO CHEST PREFAB ASSIGNED!");
            }
        }
        
        if (fallbackChestPrefab != null)
        {
            DebugLog($"ChestManager: ✅ Fallback chest: {fallbackChestPrefab.name}");
        }
        else
        {
            DebugLog($"ChestManager: ⚠️ NO FALLBACK CHEST ASSIGNED - This could cause spawn failures!");
        }
        
        DebugLog($"ChestManager: 📊 Validation complete: {validMappings}/{platformChestMappings.Length} specific mappings + {(fallbackChestPrefab != null ? 1 : 0)} fallback");
    }
    
    /// <summary>
    /// Find the actual platform surface for chest parenting (not the parent container)
    /// </summary>
    Transform FindActualPlatformSurface(Transform platformReference)
    {
        if (platformReference == null) return null;
        
        // If the platform reference has a collider, it's likely the actual surface
        Collider platformCollider = platformReference.GetComponent<Collider>();
        if (platformCollider != null)
        {
            DebugLog($"ChestManager: ✅ Using '{platformReference.name}' as platform surface (has collider)");
            return platformReference;
        }
        
        // If no collider, search children for the actual platform surface
        DebugLog($"ChestManager: 🔍 Searching children of '{platformReference.name}' for platform surface...");
        
        // Look for children with colliders that might be the platform surface
        foreach (Transform child in platformReference)
        {
            Collider childCollider = child.GetComponent<Collider>();
            if (childCollider != null && !child.name.ToLower().Contains("trigger"))
            {
                // Found a child with collider that's not a trigger - likely the platform surface
                DebugLog($"ChestManager: ✅ Found platform surface: '{child.name}' (child with collider)");
                return child;
            }
        }
        
        // If still no luck, look for any child that looks like a platform
        foreach (Transform child in platformReference)
        {
            if (child.name.ToLower().Contains("platform") || 
                child.name.ToLower().Contains("surface") ||
                child.name.ToLower().Contains("ground"))
            {
                DebugLog($"ChestManager: ✅ Found platform surface by name: '{child.name}'");
                return child;
            }
        }
        
        // Last resort: use the first child if it exists
        if (platformReference.childCount > 0)
        {
            Transform firstChild = platformReference.GetChild(0);
            DebugLog($"ChestManager: ⚠️ Using first child as platform surface: '{firstChild.name}' (fallback)");
            return firstChild;
        }
        
        // Ultimate fallback: use the original reference
        return platformReference;
    }
    
    /// <summary>
    /// Last resort: Create a backup ChestManager if the current one can't be recovered
    /// </summary>
    static void CreateBackupChestManager()
    {
        try
        {
            
            // Create a new GameObject for ChestManager
            GameObject backupManager = new GameObject("ChestManager_Backup");
            ChestManager backup = backupManager.AddComponent<ChestManager>();
            
            // Make it persistent
            DontDestroyOnLoad(backupManager);
            
        }
        catch (System.Exception ex)
        {
        }
    }
    
    /// <summary>
    /// Periodic health check to ensure ChestManager stays active and subscribed
    /// </summary>
    void HealthCheck()
    {
        // Check if we're still subscribed to tower death events
        bool isSubscribed = TowerController.OnTowerDeath != null && 
                           System.Array.Exists(TowerController.OnTowerDeath.GetInvocationList(), 
                                              del => del.Target == this);
        
        if (!isSubscribed)
        {
            TowerController.OnTowerDeath += OnTowerDied;
        }
        
        // Check if GameObject is still active
        if (!gameObject.activeInHierarchy)
        {
        }
        
        // Check if component is enabled
        if (!enabled)
        {
        }
        
        if (isSubscribed && gameObject.activeInHierarchy && enabled)
        {
            DebugLog($"ChestManager: ✅ Health check passed - subscribed to events, GameObject active, component enabled");
        }
    }
    


    void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
        }
    }
    
    // Public utility methods
    public bool HasChestSpawned(Transform platform)
    {
        return spawnedChests.ContainsKey(platform) && spawnedChests[platform] != null;
    }
    
    public GameObject GetChestForPlatform(Transform platform)
    {
        return spawnedChests.TryGetValue(platform, out GameObject chest) ? chest : null;
    }
    
    public int GetRemainingTowerCount(Transform platform)
    {
        int towerCount = platformTowerCount.GetValueOrDefault(platform, 0);
        int deathCount = platformTowerDeaths.GetValueOrDefault(platform, 0);
        return towerCount - deathCount;
    }
    
    /// <summary>
    /// Notify TowerSpawner on the platform that chest has emerged - permanently disable tower spawning
    /// </summary>
    void NotifyTowerSpawnerChestEmerged(Transform platform)
    {
        if (platform == null)
        {
            DebugLog("ChestManager: Cannot notify TowerSpawner - platform is null");
            return;
        }
        
        // Find TowerSpawner on the platform hierarchy
        TowerSpawner towerSpawner = platform.GetComponent<TowerSpawner>();
        if (towerSpawner == null)
        {
            // Check parent hierarchy
            Transform current = platform.parent;
            while (current != null && towerSpawner == null)
            {
                towerSpawner = current.GetComponent<TowerSpawner>();
                current = current.parent;
            }
        }
        
        // Check children if not found in parents
        if (towerSpawner == null)
        {
            towerSpawner = platform.GetComponentInChildren<TowerSpawner>();
        }
        
        if (towerSpawner != null)
        {
            DebugLog($"ChestManager: 🎁 Notifying TowerSpawner on '{towerSpawner.transform.name}' that chest has emerged - disabling tower spawning PERMANENTLY");
            towerSpawner.OnChestEmerged();
        }
        else
        {
            DebugLog($"ChestManager: ⚠️ Could not find TowerSpawner for platform '{platform.name}' - tower spawning may continue (this is unusual)");
            
            // Fallback: Search all TowerSpawners in scene and find the one associated with this platform
            TowerSpawner[] allSpawners = FindObjectsByType<TowerSpawner>(FindObjectsSortMode.None);
            foreach (TowerSpawner spawner in allSpawners)
            {
                // Check if this spawner's platform matches our platform
                if (spawner.platformTrigger != null)
                {
                    Transform spawnerPlatform = spawner.platformTrigger.transform.parent;
                    if (spawnerPlatform == platform || spawnerPlatform == platform.parent)
                    {
                        DebugLog($"ChestManager: 🎯 Found matching TowerSpawner via platformTrigger association - disabling tower spawning");
                        spawner.OnChestEmerged();
                        return;
                    }
                }
            }
            
            DebugLog($"ChestManager: ❌ CRITICAL: Could not find any TowerSpawner for platform '{platform.name}' - tower spawning will NOT be disabled!");
        }
    }
}
