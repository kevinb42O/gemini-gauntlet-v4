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
    
    // Hardcoded emergence settings - no inspector bullshit
    private const float EMERGENCE_DURATION = 1.5f;
    private const float STAGGER_DELAY = 0.5f;
    private const float INITIAL_DELAY = 0.3f;
    
    // Simple state tracking
    private List<TowerController> _activeTowers = new List<TowerController>();
    private HashSet<Transform> _usedSpawnPoints = new HashSet<Transform>();
    private Dictionary<TowerController, Transform> _towerToSpawnPoint = new Dictionary<TowerController, Transform>();
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
        towerCount = Mathf.Min(towerCount, towerSpawnPoints.Length);
        
        List<Transform> availablePoints = towerSpawnPoints.Where(p => p != null).ToList();
        
        yield return new WaitForSeconds(INITIAL_DELAY);
        
        for (int i = 0; i < towerCount && availablePoints.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, availablePoints.Count);
            Transform spawnPoint = availablePoints[randomIndex];
            availablePoints.RemoveAt(randomIndex);
            
            SpawnSingleTower(spawnPoint, parent);
            
            if (i < towerCount - 1)
            {
                yield return new WaitForSeconds(STAGGER_DELAY);
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
        }
        
        // If continuous spawning is enabled and chest hasn't emerged, respawn a new tower
        if (enableContinuousSpawning && !_chestHasEmerged && _playerIsOnPlatform && _hasSpawnedTowers)
        {
            // Check if we're below the max tower cap
            if (_activeTowers.Count < maxSimultaneousTowers)
            {
                // Find a free spawn point to use
                Transform freeSpawnPoint = GetFreeSpawnPoint();
                if (freeSpawnPoint != null)
                {
                    StartCoroutine(RespawnTowerDelayed(freeSpawnPoint, 2f));
                }
            }
        }
    }
    
    /// <summary>
    /// Get a spawn point that's not currently in use
    /// </summary>
    private Transform GetFreeSpawnPoint()
    {
        if (towerSpawnPoints == null || towerSpawnPoints.Length == 0)
            return null;
            
        List<Transform> freePoints = new List<Transform>();
        foreach (Transform point in towerSpawnPoints)
        {
            if (point != null && !_usedSpawnPoints.Contains(point))
            {
                freePoints.Add(point);
            }
        }
        
        if (freePoints.Count > 0)
        {
            return freePoints[Random.Range(0, freePoints.Count)];
        }
        
        return null;
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
