// --- TowerSpawner.cs (CLEAN & SIMPLE - 200 LINES INSTEAD OF 1440!) ---
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TowerSpawner : MonoBehaviour
{
    [Header("Tower Configuration")]
    [Tooltip("The tower prefab to spawn")]
    public GameObject towerPrefab;
    
    [Header("Spawn Points")]
    [Tooltip("Assign empty GameObjects positioned where towers should spawn.")]
    public Transform[] towerSpawnPoints = new Transform[0];
    
    [Header("Platform Setup - JUST ASSIGN THIS!")]
    [Tooltip("The actual platform transform that towers should be parented to (the moving platform object)")]
    public Transform platformParent;
    
    [Header("Platform Trigger")]
    [Tooltip("Assign the PlatformTrigger that should activate this TowerSpawner when player enters.")]
    public PlatformTrigger platformTrigger;
    
    [Header("Spawn Configuration")]
    [SerializeField] private int minTowersToSpawn = 1;
    [SerializeField] private int maxTowersToSpawn = 3;
    
    [Header("Continuous Spawning")]
    [Tooltip("Enable continuous tower spawning - when a tower dies, a new one spawns at a free spawn point")]
    public bool enableContinuousSpawning = false;
    
    [Tooltip("Maximum number of towers that can exist simultaneously on this platform")]
    [SerializeField] private int maxSimultaneousTowers = 15;
    
    [Header("🎯 PRIORITY 2: Spawn Point Cooldowns")]
    [Tooltip("Time (seconds) a spawn point must wait before being reused after a tower death")]
    [Range(10f, 120f)]
    [SerializeField] private float spawnPointCooldownDuration = 45f;
    
    [Tooltip("Show debug logs for spawn point cooldown system")]
    [SerializeField] private bool logSpawnPointCooldowns = true;
    
    [Header("🎯 SMART SPAWN ROTATION")]
    [Tooltip("If enabled, uses spawn points in rotation (saves one for respawning) instead of random selection")]
    [SerializeField] private bool useSmartSpawnRotation = true;
    
    [Tooltip("Minimum distance between towers to prevent spawning too close (in Unity units)")]
    [Range(500f, 5000f)]
    [SerializeField] private float minTowerSeparationDistance = 2000f;
    
    [Tooltip("Show debug logs for smart spawn rotation")]
    [SerializeField] private bool logSmartSpawning = true;
    
    [Header("🎯 PRIORITY 4: Respawn Delay Randomization")]
    [Tooltip("Minimum delay (seconds) before respawning a tower after death")]
    [Range(2f, 15f)]
    [SerializeField] private float minRespawnDelay = 5f;
    
    [Tooltip("Maximum delay (seconds) before respawning a tower after death")]
    [Range(2f, 20f)]
    [SerializeField] private float maxRespawnDelay = 10f;
    
    [Tooltip("Show debug logs for respawn delay randomization")]
    [SerializeField] private bool logRespawnDelays = true;
    
    // Hardcoded emergence settings - no inspector bullshit
    private const float EMERGENCE_DURATION = 1.5f;
    private const float STAGGER_DELAY = 0.5f;
    private const float INITIAL_DELAY = 0.3f;
    
    // Simple state tracking
    private List<TowerController> _activeTowers = new List<TowerController>();
    private HashSet<Transform> _usedSpawnPoints = new HashSet<Transform>();
    private Dictionary<TowerController, Transform> _towerToSpawnPoint = new Dictionary<TowerController, Transform>();
    
    // 🎯 PRIORITY 2: Spawn point cooldown tracking
    private Dictionary<Transform, float> _spawnPointCooldowns = new Dictionary<Transform, float>();
    
    // 🎯 SMART SPAWN ROTATION: Track last used spawn point index for sequential spawning
    private int _lastUsedSpawnPointIndex = -1;
    private List<Transform> _reservedRespawnPoints = new List<Transform>(); // Points reserved for respawning
    
    private bool _hasSpawnedTowers = false;
    private bool _chestHasEmerged = false;
    private bool _playerIsOnPlatform = false;

    void Start()
    {
        // Auto-find platform parent if not assigned
        if (platformParent == null)
        {
            platformParent = FindPlatformParent();
        }
        
        // Subscribe to tower death events
        TowerController.OnTowerDeath += OnTowerDestroyed;
        
        // Register with platform trigger
        if (platformTrigger != null)
        {
            platformTrigger.associatedTowerSpawner = this;
        }
    }
    
    void OnDestroy()
    {
        TowerController.OnTowerDeath -= OnTowerDestroyed;
    }

    void Update()
    {
        // Clean up dead towers from tracking list
        _activeTowers.RemoveAll(tower => tower == null || tower.IsDead);
        
        // 🎯 PRIORITY 2: Tick down spawn point cooldowns
        if (_spawnPointCooldowns.Count > 0)
        {
            // Create list of cooldowns that expired this frame
            List<Transform> expiredCooldowns = new List<Transform>();
            
            // Update all cooldowns
            List<Transform> pointsToUpdate = new List<Transform>(_spawnPointCooldowns.Keys);
            foreach (Transform spawnPoint in pointsToUpdate)
            {
                if (spawnPoint == null)
                {
                    expiredCooldowns.Add(spawnPoint);
                    continue;
                }
                
                _spawnPointCooldowns[spawnPoint] -= Time.deltaTime;
                
                if (_spawnPointCooldowns[spawnPoint] <= 0f)
                {
                    expiredCooldowns.Add(spawnPoint);
                    if (logSpawnPointCooldowns)
                    {
                        Debug.Log($"[TowerSpawner] ✅ Spawn point '{spawnPoint.name}' cooldown expired - available for respawn");
                    }
                }
            }
            
            // Remove expired cooldowns
            foreach (Transform expiredPoint in expiredCooldowns)
            {
                _spawnPointCooldowns.Remove(expiredPoint);
            }
        }
    }

    /// <summary>
    /// Called by PlatformTrigger when player enters platform - spawn towers!
    /// </summary>
    public void OnPlayerEnteredPlatform()
    {
        _playerIsOnPlatform = true;
        
        // Never spawn if chest has emerged or towers already spawned
        if (_chestHasEmerged || _hasSpawnedTowers)
        {
            return;
        }
        
        StartCoroutine(SpawnTowersStaggered());
        _hasSpawnedTowers = true;
    }
    
    public void OnPlayerLeftPlatform()
    {
        _playerIsOnPlatform = false;
    }

    /// <summary>
    /// Staggered tower spawning for smooth, cinematic feel
    /// 🎯 SMART SPAWNING: Uses sequential spawn points, reserves last one(s) for respawning
    /// </summary>
    private System.Collections.IEnumerator SpawnTowersStaggered()
    {
        if (towerPrefab == null || towerSpawnPoints == null || towerSpawnPoints.Length == 0)
        {
            Debug.LogWarning("[TowerSpawner] Missing towerPrefab or spawn points!");
            yield break;
        }
        
        Transform parent = platformParent != null ? platformParent : transform;
        int towerCount = Random.Range(minTowersToSpawn, maxTowersToSpawn + 1);
        
        // 🎯 SMART SPAWNING: If we have more spawn points than towers, reserve some for respawning
        List<Transform> availablePoints = towerSpawnPoints.Where(p => p != null).ToList();
        int totalSpawnPoints = availablePoints.Count;
        
        if (useSmartSpawnRotation && totalSpawnPoints > towerCount)
        {
            // Reserve 1 spawn point for respawning (or 2 if we have 6+ points)
            int pointsToReserve = totalSpawnPoints >= 6 ? 2 : 1;
            towerCount = Mathf.Min(towerCount, totalSpawnPoints - pointsToReserve);
            
            if (logSmartSpawning)
            {
                Debug.Log($"[TowerSpawner] 🎯 SMART SPAWN: {towerCount} initial towers, reserving {pointsToReserve} spawn points for respawning (Total points: {totalSpawnPoints})");
            }
        }
        else
        {
            towerCount = Mathf.Min(towerCount, totalSpawnPoints);
        }
        
        yield return new WaitForSeconds(INITIAL_DELAY);
        
        // 🎯 SMART SPAWNING: Use sequential rotation instead of random
        for (int i = 0; i < towerCount && availablePoints.Count > 0; i++)
        {
            Transform spawnPoint;
            
            if (useSmartSpawnRotation)
            {
                // Sequential spawning - use next spawn point in order
                _lastUsedSpawnPointIndex = (_lastUsedSpawnPointIndex + 1) % availablePoints.Count;
                spawnPoint = availablePoints[_lastUsedSpawnPointIndex];
                availablePoints.RemoveAt(_lastUsedSpawnPointIndex);
                
                // Adjust index after removal
                if (_lastUsedSpawnPointIndex >= availablePoints.Count && availablePoints.Count > 0)
                {
                    _lastUsedSpawnPointIndex = 0;
                }
                
                if (logSmartSpawning)
                {
                    Debug.Log($"[TowerSpawner] 🎯 Tower {i + 1}/{towerCount} spawning at sequential point: '{spawnPoint.name}'");
                }
            }
            else
            {
                // Random spawning (old behavior)
                int randomIndex = Random.Range(0, availablePoints.Count);
                spawnPoint = availablePoints[randomIndex];
                availablePoints.RemoveAt(randomIndex);
            }
            
            // Check if this spawn point is too close to existing towers
            if (!IsSpawnPointSafe(spawnPoint))
            {
                if (logSmartSpawning)
                {
                    Debug.LogWarning($"[TowerSpawner] ⚠️ Spawn point '{spawnPoint.name}' too close to existing towers - skipping");
                }
                continue; // Skip this spawn point
            }
            
            SpawnSingleTower(spawnPoint, parent);
            
            if (i < towerCount - 1)
            {
                yield return new WaitForSeconds(STAGGER_DELAY);
            }
        }
        
        // Mark remaining points as reserved for respawning
        if (useSmartSpawnRotation)
        {
            _reservedRespawnPoints = availablePoints;
            if (logSmartSpawning && _reservedRespawnPoints.Count > 0)
            {
                Debug.Log($"[TowerSpawner] 🎯 Reserved {_reservedRespawnPoints.Count} spawn points for respawning: {string.Join(", ", _reservedRespawnPoints.Select(p => p.name))}");
            }
        }
    }
    
    /// <summary>
    /// Spawn a single tower - PARENT TO PLATFORM for moving platforms!
    /// </summary>
    private void SpawnSingleTower(Transform spawnPoint, Transform parent)
    {
        Vector3 targetPosition = spawnPoint.position;
        
        // Instantiate at spawn point first to get tower height
        GameObject towerObj = Instantiate(towerPrefab, targetPosition, spawnPoint.rotation, parent);
        towerObj.name = $"Tower_{spawnPoint.name}";
        
        TowerController tower = towerObj.GetComponent<TowerController>();
        if (tower == null)
        {
            Debug.LogWarning($"[TowerSpawner] Tower prefab missing TowerController component!");
            Destroy(towerObj);
            return;
        }
        
        // Calculate tower height from renderer bounds
        float towerHeight = 5f; // Default fallback
        Renderer[] renderers = towerObj.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds combinedBounds = renderers[0].bounds;
            foreach (Renderer r in renderers)
            {
                combinedBounds.Encapsulate(r.bounds);
            }
            towerHeight = combinedBounds.size.y;
        }
        
        // Move tower underground by its own height
        Vector3 targetLocalPosition = parent.InverseTransformPoint(targetPosition);
        Vector3 undergroundLocalPosition = targetLocalPosition - new Vector3(0f, towerHeight, 0f);
        towerObj.transform.localPosition = undergroundLocalPosition;
        
        _activeTowers.Add(tower);
        _usedSpawnPoints.Add(spawnPoint);
        _towerToSpawnPoint[tower] = spawnPoint;
        tower._associatedPlatformTransform = parent;
        
        ChestManager chestManager = FindFirstObjectByType<ChestManager>();
        if (chestManager != null)
        {
            chestManager.RegisterTowerSpawn(tower, parent);
        }
        
        StartCoroutine(EmergeTower(tower, targetPosition));
    }
    
    /// <summary>
    /// Tower rises from underground to surface
    /// </summary>
    private System.Collections.IEnumerator EmergeTower(TowerController tower, Vector3 targetWorldPosition)
    {
        if (tower == null) yield break;
        
        Vector3 targetLocalPosition = tower.transform.parent.InverseTransformPoint(targetWorldPosition);
        Vector3 startLocalPosition = tower.transform.localPosition;
        float elapsed = 0f;
        
        while (elapsed < EMERGENCE_DURATION)
        {
            if (tower == null || tower.gameObject == null) yield break;
            
            elapsed += Time.deltaTime;
            float t = elapsed / EMERGENCE_DURATION;
            float smoothT = t * t * (3f - 2f * t); // Smoothstep
            
            Vector3 newLocalPos = Vector3.Lerp(startLocalPosition, targetLocalPosition, smoothT);
            tower.transform.localPosition = newLocalPos;
            
            yield return null;
        }
        
        if (tower != null && tower.gameObject != null)
        {
            tower.transform.localPosition = targetLocalPosition;
            
            Rigidbody rb = tower.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.constraints = RigidbodyConstraints.FreezeAll;
            }
            
            tower.OnEmergenceComplete();
        }
    }
    
    /// <summary>
    /// Called when a tower is destroyed
    /// </summary>
    private void OnTowerDestroyed(TowerController tower)
    {
        if (tower == null) return;
        
        // Remove from active towers list
        _activeTowers.Remove(tower);
        
        // Get the spawn point this tower was using
        Transform spawnPoint = null;
        if (_towerToSpawnPoint.TryGetValue(tower, out spawnPoint))
        {
            _towerToSpawnPoint.Remove(tower);
            _usedSpawnPoints.Remove(spawnPoint);
            
            // 🎯 PRIORITY 2: Add cooldown to this spawn point
            if (spawnPoint != null)
            {
                _spawnPointCooldowns[spawnPoint] = spawnPointCooldownDuration;
                if (logSpawnPointCooldowns)
                {
                    Debug.Log($"[TowerSpawner] ⏱️ Spawn point '{spawnPoint.name}' on cooldown for {spawnPointCooldownDuration}s after tower death");
                }
            }
        }
        
        // If continuous spawning is enabled and chest hasn't emerged, respawn a new tower
        if (enableContinuousSpawning && !_chestHasEmerged && _playerIsOnPlatform && _hasSpawnedTowers)
        {
            // Check if we're below the max tower cap
            if (_activeTowers.Count < maxSimultaneousTowers)
            {
                // Find a free spawn point to use (respects cooldowns)
                Transform freeSpawnPoint = GetFreeSpawnPoint();
                if (freeSpawnPoint != null)
                {
                    // 🎯 PRIORITY 4: Randomize respawn delay
                    float randomDelay = Random.Range(minRespawnDelay, maxRespawnDelay);
                    if (logRespawnDelays)
                    {
                        Debug.Log($"[TowerSpawner] 🎲 Tower respawn delay randomized: {randomDelay:F1}s (range: {minRespawnDelay}-{maxRespawnDelay}s)");
                    }
                    StartCoroutine(RespawnTowerDelayed(freeSpawnPoint, randomDelay));
                }
                else if (logSpawnPointCooldowns)
                {
                    Debug.LogWarning($"[TowerSpawner] ⏸️ No free spawn points available (all on cooldown or in use) - respawn delayed");
                }
            }
        }
    }
    
    /// <summary>
    /// Get a spawn point that's not currently in use AND not on cooldown
    /// 🎯 SMART SPAWNING: Prefers reserved respawn points if available
    /// </summary>
    private Transform GetFreeSpawnPoint()
    {
        if (towerSpawnPoints == null || towerSpawnPoints.Length == 0)
            return null;
        
        // 🎯 SMART SPAWNING: Prioritize reserved respawn points first
        if (useSmartSpawnRotation && _reservedRespawnPoints.Count > 0)
        {
            List<Transform> availableReservedPoints = new List<Transform>();
            
            foreach (Transform point in _reservedRespawnPoints)
            {
                if (point != null && !_usedSpawnPoints.Contains(point) && !_spawnPointCooldowns.ContainsKey(point))
                {
                    // Check if spawn point is safe (not too close to existing towers)
                    if (IsSpawnPointSafe(point))
                    {
                        availableReservedPoints.Add(point);
                    }
                }
            }
            
            if (availableReservedPoints.Count > 0)
            {
                Transform selectedPoint = availableReservedPoints[Random.Range(0, availableReservedPoints.Count)];
                if (logSmartSpawning)
                {
                    Debug.Log($"[TowerSpawner] 🎯 SMART RESPAWN: Using reserved spawn point '{selectedPoint.name}' ({availableReservedPoints.Count} reserved points available)");
                }
                return selectedPoint;
            }
            else if (logSmartSpawning)
            {
                Debug.Log($"[TowerSpawner] 🎯 SMART RESPAWN: No reserved points available, checking all spawn points...");
            }
        }
        
        // Fallback: Check all spawn points (old behavior)
        List<Transform> freePoints = new List<Transform>();
        foreach (Transform point in towerSpawnPoints)
        {
            if (point != null && !_usedSpawnPoints.Contains(point))
            {
                // 🎯 PRIORITY 2: Check if spawn point is on cooldown
                if (_spawnPointCooldowns.ContainsKey(point))
                {
                    // Skip this point - it's on cooldown
                    if (logSpawnPointCooldowns)
                    {
                        float remainingCooldown = _spawnPointCooldowns[point];
                        Debug.Log($"[TowerSpawner] ⏱️ Spawn point '{point.name}' skipped - {remainingCooldown:F1}s cooldown remaining");
                    }
                    continue;
                }
                
                // 🎯 SMART SPAWNING: Check if spawn point is safe
                if (!IsSpawnPointSafe(point))
                {
                    if (logSmartSpawning)
                    {
                        Debug.LogWarning($"[TowerSpawner] ⚠️ Spawn point '{point.name}' too close to existing towers - skipped");
                    }
                    continue;
                }
                
                freePoints.Add(point);
            }
        }
        
        if (freePoints.Count > 0)
        {
            Transform selectedPoint = freePoints[Random.Range(0, freePoints.Count)];
            if (logSpawnPointCooldowns)
            {
                Debug.Log($"[TowerSpawner] ✅ Selected free spawn point: '{selectedPoint.name}' ({freePoints.Count} available)");
            }
            return selectedPoint;
        }
        
        return null;
    }
    
    /// <summary>
    /// 🎯 SMART SPAWNING: Check if a spawn point is safe (not too close to existing towers)
    /// Prevents towers from spawning into each other or too close together
    /// </summary>
    private bool IsSpawnPointSafe(Transform spawnPoint)
    {
        if (spawnPoint == null) return false;
        
        // No need to check if no towers exist yet
        if (_activeTowers.Count == 0) return true;
        
        Vector3 spawnPosition = spawnPoint.position;
        
        // Check distance to all active towers
        foreach (TowerController tower in _activeTowers)
        {
            if (tower == null || tower.IsDead) continue;
            
            float distance = Vector3.Distance(spawnPosition, tower.transform.position);
            
            if (distance < minTowerSeparationDistance)
            {
                if (logSmartSpawning)
                {
                    Debug.LogWarning($"[TowerSpawner] ⚠️ Spawn point '{spawnPoint.name}' too close to tower '{tower.name}' (distance: {distance:F0} < min: {minTowerSeparationDistance:F0})");
                }
                return false; // Too close!
            }
        }
        
        return true; // Safe to spawn
    }
    
    /// <summary>
    /// Respawn a tower at a specific spawn point after a delay
    /// </summary>
    private System.Collections.IEnumerator RespawnTowerDelayed(Transform spawnPoint, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Double-check conditions before spawning
        if (_chestHasEmerged || !_playerIsOnPlatform || !enableContinuousSpawning)
        {
            yield break;
        }
        
        Transform parent = platformParent != null ? platformParent : transform;
        SpawnSingleTower(spawnPoint, parent);
    }
    
    /// <summary>
    /// Called by ChestManager when chest emerges - permanently disable tower spawning
    /// </summary>
    public void OnChestEmerged()
    {
        _chestHasEmerged = true;
    }
    
    /// <summary>
    /// Simple platform parent finder - tries to find CelestialPlatform or uses self
    /// </summary>
    private Transform FindPlatformParent()
    {
        Transform current = transform;
        
        // Walk up hierarchy looking for CelestialPlatform
        while (current != null)
        {
            if (current.GetComponent<CelestialPlatform>() != null)
            {
                return current;
            }
            current = current.parent;
        }
        
        // Fallback to self
        return transform;
    }
}
