// --- PlatformEncounterManager.cs (TIER-BASED SPAWNING) ---
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum PlatformTier
{
    Inner,
    Middle,
    Outer
}

public class PlatformEncounterManager : MonoBehaviour
{
    public static PlatformEncounterManager Instance { get; private set; }

    [Header("Encounter Settings")]
    [Tooltip("The Tower prefab to spawn. Must have TowerController.cs on it.")]
    public GameObject towerPrefab;

    [Header("Tier-Based Tower Spawning")]
    [Tooltip("Chance to spawn a tower on Inner tier platforms")]
    [Range(0f, 1f)] public float innerTierTowerSpawnChance = 0.9f;
    [Tooltip("Chance to spawn a tower on Middle tier platforms")]
    [Range(0f, 1f)] public float middleTierTowerSpawnChance = 0.75f;
    [Tooltip("Chance to spawn a tower on Outer tier platforms")]
    [Range(0f, 1f)] public float outerTierTowerSpawnChance = 0.5f;
    
    [Header("Spawning Mechanics")]
    [Tooltip("How far below the platform the tower should start its emergence from.")]
    public float emergenceStartYOffset = 10f;
    [Tooltip("A small delay before the tower starts emerging after being spawned.")]
    public float emergenceInitialDelay = 0.5f;
    [Tooltip("Initialize towers at game start without waiting for player")]
    public bool spawnTowersAtGameStart = true;
    
    private HashSet<Transform> _platformsWithEncounters = new HashSet<Transform>();
    private Dictionary<Transform, PlatformTier> _platformTiers = new Dictionary<Transform, PlatformTier>();
    
    // Lists of platforms by tier - accessible for debugging and visualization
    public List<Transform> InnerTierPlatforms { get; private set; } = new List<Transform>();
    public List<Transform> MiddleTierPlatforms { get; private set; } = new List<Transform>();
    public List<Transform> OuterTierPlatforms { get; private set; } = new List<Transform>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void OnEnable()
    {
        // --- FIX: Subscribe to the event on PlayerMovementManager ---
        PlayerMovementManager.OnPlayerLandedOnNewPlatform += HandlePlayerLanded;
    }

    void OnDisable()
    {
        // --- FIX: Unsubscribe from the event on PlayerMovementManager ---
        PlayerMovementManager.OnPlayerLandedOnNewPlatform -= HandlePlayerLanded;
    }
    
    [Header("Tower Prefab System")]
    [SerializeField] private bool useManualPrefabPlacement = true;
#pragma warning disable 0414
    [SerializeField] private string prefabPlacementManagerName = "PlatformFillManager";
#pragma warning restore 0414
    private PlatformFillManager _platformFillManager;
    
    // Public lists for visualization and debugging
    public List<Transform> innerTierPlatforms = new List<Transform>();
    public List<Transform> middleTierPlatforms = new List<Transform>();
    public List<Transform> outerTierPlatforms = new List<Transform>();
    
    void Start()
    {
        if (spawnTowersAtGameStart)
        {
            InitializeTowers();
        }
    }
    
    private void InitializeTowers()
    {
        // Find all platforms in the scene
        CelestialPlatform[] allPlatforms = FindObjectsByType<CelestialPlatform>(FindObjectsSortMode.None);
        
        if (allPlatforms.Length == 0)
        {
            Debug.LogWarning("No CelestialPlatform components found in scene.");
            return;
        }
        
        // Clear existing lists
        innerTierPlatforms.Clear();
        middleTierPlatforms.Clear();
        outerTierPlatforms.Clear();
        
        // Get the sun position (assumed to be at origin)
        Vector3 sunPosition = Vector3.zero;
        
        // Calculate distance thresholds based on platform distribution
        List<float> distances = new List<float>();
        foreach (var platform in allPlatforms)
        {
            if (platform.transform != null)
            {
                distances.Add(Vector3.Distance(platform.transform.position, sunPosition));
            }
        }
        
        distances.Sort();
        
        // Define thresholds at 1/3 and 2/3 of the sorted distances
        float innerThreshold = distances[Mathf.FloorToInt(distances.Count * 0.33f)];
        float outerThreshold = distances[Mathf.FloorToInt(distances.Count * 0.66f)];
        
        // Assign platforms to tiers based on distance
        foreach (var platform in allPlatforms)
        {
            if (platform.transform == null) continue;
            
            float distance = Vector3.Distance(platform.transform.position, sunPosition);
            
            if (distance <= innerThreshold)
            {
                innerTierPlatforms.Add(platform.transform);
            }
            else if (distance <= outerThreshold)
            {
                middleTierPlatforms.Add(platform.transform);
            }
            else
            {
                outerTierPlatforms.Add(platform.transform);
            }
        }
        
        // If using manual prefab placement, don't spawn towers at runtime
        if (!useManualPrefabPlacement)
        {
            // Spawn towers based on tier probabilities using legacy method
            SpawnTowersForTier(innerTierPlatforms, innerTierTowerSpawnChance);
            SpawnTowersForTier(middleTierPlatforms, middleTierTowerSpawnChance);
            SpawnTowersForTier(outerTierPlatforms, outerTierTowerSpawnChance);
        }
    }

    /// <summary>
    /// This method is called by the PlayerMovementManager event whenever the player lands on a new platform.
    /// </summary>
    /// <param name="platformLandedOn">The Transform of the platform the player just landed on.</param>
    private void HandlePlayerLanded(Transform platformLandedOn)
    {
        if (towerPrefab == null)
        {
            Debug.LogError("PlatformEncounterManager: Tower Prefab is not assigned! Cannot spawn towers.", this);
            return;
        }

        if (_platformsWithEncounters.Contains(platformLandedOn))
        {
            // Tower already exists, just notify it that player has entered
            TowerController existingTower = FindTowerOnPlatform(platformLandedOn);
            if (existingTower != null)
            {
                // Note: TowerController now handles player detection internally
            }
            return;
        }
        
        // Determine platform tier if not already categorized
        if (!_platformTiers.ContainsKey(platformLandedOn))
        {
            // Default to middle tier if not categorized
            _platformTiers[platformLandedOn] = PlatformTier.Middle;
        }
        
        PlatformTier tier = _platformTiers[platformLandedOn];
        float spawnChance = GetTowerSpawnChanceForTier(tier);
        
        _platformsWithEncounters.Add(platformLandedOn);

        if (Random.value <= spawnChance)
        {
            Debug.Log($"Encounter triggered on {tier} tier platform '{platformLandedOn.name}'. Spawning tower.", platformLandedOn);
            SpawnTowerOnPlatform(platformLandedOn);
        }
        else
        {
            Debug.Log($"Encounter check on {tier} tier platform '{platformLandedOn.name}'. Spawn chance failed. No tower will spawn.", platformLandedOn);
        }
    }
    
    /// <summary>
    /// Returns the tower spawn chance for the given platform tier.
    /// </summary>
    private float GetTowerSpawnChanceForTier(PlatformTier tier)
    {
        switch (tier)
        {
            case PlatformTier.Inner:
                return innerTierTowerSpawnChance;
            case PlatformTier.Middle:
                return middleTierTowerSpawnChance;
            case PlatformTier.Outer:
                return outerTierTowerSpawnChance;
            default:
                return 0.5f; // Default fallback
        }
    }
    
    private TowerController FindTowerOnPlatform(Transform platform)
    {
        foreach (TowerController tower in TowerController.ActiveTowers)
        {
            if (tower._associatedPlatformTransform == platform)
            {
                return tower;
            }
        }
        return null;
    }

    private void SpawnTowerOnPlatform(Transform platform)
    {
        // Calculate the starting position for the tower's emergence animation.
        // It starts below the platform and moves up to the platform's center.
        Vector3 emergenceStartPosition = platform.position - (platform.up * emergenceStartYOffset); // Changed to use platform.up for orientation

        // Instantiate the tower prefab at this starting position, matching the platform's orientation.
        GameObject towerInstance = Instantiate(towerPrefab, emergenceStartPosition, platform.rotation);

        TowerController towerController = towerInstance.GetComponent<TowerController>();
        if (towerController != null)
        {
            // REMOVED: StartEmergence() - emergence is now handled by TowerSpawner
            // Tower emergence will be handled automatically by TowerSpawner when player enters platform
        }
        else
        {
            Debug.LogError($"PlatformEncounterManager: The assigned 'towerPrefab' is missing a TowerController script!", towerPrefab);
            Destroy(towerInstance);
        }
    }
    
    /// <summary>
    /// Spawns towers on platforms in the given tier based on the specified spawn chance.
    /// </summary>
    /// <param name="platformsInTier">List of platforms in this tier</param>
    /// <param name="spawnChance">Chance (0-1) to spawn a tower on each platform</param>
    private void SpawnTowersForTier(List<Transform> platformsInTier, float spawnChance)
    {
        if (towerPrefab == null)
        {
            Debug.LogError("PlatformEncounterManager: Tower Prefab is not assigned! Cannot spawn towers.", this);
            return;
        }
        
        foreach (Transform platform in platformsInTier)
        {
            // Skip if this platform already has a tower
            if (_platformsWithEncounters.Contains(platform))
            {
                continue;
            }
            
            // Determine if we should spawn a tower based on chance
            if (Random.value <= spawnChance)
            {
                _platformsWithEncounters.Add(platform);
                SpawnTowerOnPlatform(platform);
            }
        }
    }
}