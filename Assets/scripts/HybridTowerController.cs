// --- HybridTowerController.cs (FINAL, Corrected for Inheritance) ---
using UnityEngine;
using System.Collections;
using System;

public class HybridTowerController : TowerController
{
    // This event will be broadcasted before spawning skulls.
    public static event Action<HybridTowerController> OnBeforeSpawnBurst;

    [Header("Hybrid: Fireball Settings")]
    [Tooltip("The chasing fireball prefab. MUST have the ChasingFireball.cs script.")]
    public GameObject fireballPrefab;
    [Tooltip("An empty GameObject where fireballs are spawned from.")]
    public Transform fireballSpawnPoint;
    [Tooltip("Time in seconds between each fireball spawn.")]
    public float fireballSpawnInterval = 6f;
    [Tooltip("Initial force to shoot the fireball out of the tower.")]
    public float fireballLaunchForce = 5f;

    private float _nextFireballSpawnTimer;
    private WanderWithinBounds _wanderScript;
    private Transform _cachedPlayerTransform; // Cache player reference for performance

    // This script doesn't need its own Awake since TowerController's is public.
    // However, if you add one, it must not use 'override' unless the base is 'virtual'.
    // Let's get the reference in Start instead, which is safer.

    protected override void Start()
    {
        // It's crucial to call the base method first to set up its variables.
        base.Start();

        // Now we can do our HybridTower-specific setup.
        _wanderScript = GetComponent<WanderWithinBounds>();
        _nextFireballSpawnTimer = fireballSpawnInterval;
        
        // Cache player reference once for performance
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _cachedPlayerTransform = playerObj.transform;
        }
        
        Debug.Log($"[HybridTowerController] Start() completed. Initial state:");
        Debug.Log($"  skullTypes: {(HasValidSkullTypes() ? "ASSIGNED" : "NULL")}");
        Debug.Log($"  skullSpawnPoints: {(skullSpawnPoints != null ? skullSpawnPoints.Length.ToString() : "NULL")} points");
        Debug.Log($"  _canSpawnSkullsBasedOnPlayerPresence: {_canSpawnSkullsBasedOnPlayerPresence}");
        Debug.Log($"  _skullSpawningEnabled: {_skullSpawningEnabled}");
        Debug.Log($"  _hasAppeared: {_hasAppeared}");
    }

    // Override OnEmergenceComplete to ensure HybridTowerController gets proper skull spawning activation
    public override void OnEmergenceComplete()
    {
        Debug.Log($"[HybridTowerController] OnEmergenceComplete() called!");
        
        // Call base implementation first
        base.OnEmergenceComplete();
        
        // CRITICAL FIX: Ensure HybridTowerController can spawn skulls immediately after emergence
        // Since the tower emerged, the player is obviously on the platform
        _canSpawnSkullsBasedOnPlayerPresence = true;
        
        Debug.Log($"[HybridTowerController] After OnEmergenceComplete:");
        Debug.Log($"  _hasAppeared: {_hasAppeared}");
        Debug.Log($"  _skullSpawningEnabled: {_skullSpawningEnabled}");
        Debug.Log($"  _canSpawnSkullsBasedOnPlayerPresence: {_canSpawnSkullsBasedOnPlayerPresence}");
        Debug.Log($"  Next skull spawn in: {_nextSkullSpawnTimer:F2}s");
    }

    protected override void Update()
    {
        if (IsDead || !_hasAppeared || _isCollapsing) 
        {
            Debug.Log($"[HybridTowerController] Update early return: IsDead={IsDead}, _hasAppeared={_hasAppeared}, _isCollapsing={_isCollapsing}");
            return;
        }

        // The Wander script now handles rotation.
        // We only do the gentle spin if the wander script isn't attached.
        if (_wanderScript == null && gentleSpinSpeed != 0)
        {
            transform.Rotate(Vector3.up, gentleSpinSpeed * Time.deltaTime, Space.World);
        }

        // DEBUG: Log spawning conditions every few seconds
        if (Time.time % 3f < Time.deltaTime)
        {
            Debug.Log($"[HybridTowerController] Spawn Status Check:");
            Debug.Log($"  _canSpawnSkullsBasedOnPlayerPresence: {_canSpawnSkullsBasedOnPlayerPresence}");
            Debug.Log($"  _skullSpawningEnabled: {_skullSpawningEnabled}");
            Debug.Log($"  skullTypes: {(HasValidSkullTypes() ? "ASSIGNED" : "NULL")}");
            Debug.Log($"  skullSpawnPoints: {(skullSpawnPoints != null ? skullSpawnPoints.Length.ToString() : "NULL")} points");
            Debug.Log($"  _activeSkulls.Count: {_activeSkulls.Count}/{maxActiveSkullsPerTower}");
            Debug.Log($"  _nextSkullSpawnTimer: {_nextSkullSpawnTimer:F2}s");
            
            // Check platform detection
            PlatformTrigger platformTrigger = GetAssociatedPlatformTrigger();
            bool playerOnPlatform = platformTrigger != null && platformTrigger.IsPlayerOnThisPlatform();
            Debug.Log($"  Platform Detection: trigger={platformTrigger != null}, playerOnPlatform={playerOnPlatform}");
        }

        if (_canSpawnSkullsBasedOnPlayerPresence)
        {
            // --- 1. Skull Spawning Cycle ---
            if (HasValidSkullTypes())
            {
                _nextSkullSpawnTimer -= Time.deltaTime;
                if (_nextSkullSpawnTimer <= 0)
                {
                    Debug.Log($"[HybridTowerController] Skull spawn timer reached! Checking conditions...");
                    
                    _activeSkulls.RemoveAll(s => s == null || s.IsDead());
                    if (_activeSkulls.Count < maxActiveSkullsPerTower)
                    {
                        Debug.Log($"[HybridTowerController] Starting skull burst! Active skulls: {_activeSkulls.Count}/{maxActiveSkullsPerTower}");
                        
                        // Broadcast the event BEFORE starting the spawn coroutine.
                        OnBeforeSpawnBurst?.Invoke(this);

                        StartCoroutine(SpawnSkullBurstCoroutine());
                    }
                    else
                    {
                        Debug.Log($"[HybridTowerController] Cannot spawn skulls - at max capacity: {_activeSkulls.Count}/{maxActiveSkullsPerTower}");
                    }
                    _nextSkullSpawnTimer = skullSpawnInterval;
                }
            }
            else
            {
                Debug.LogWarning($"[HybridTowerController] No valid skull types configured! Cannot spawn skulls.");
            }

            // --- 2. Fireball Spawning Cycle ---
            if (fireballPrefab != null)
            {
                _nextFireballSpawnTimer -= Time.deltaTime;
                if (_nextFireballSpawnTimer <= 0)
                {
                    // Use cached player reference instead of FindGameObjectWithTag for performance
                    if (_cachedPlayerTransform != null)
                    {
                        Debug.Log($"[HybridTowerController] Spawning chasing fireball!");
                        StartCoroutine(SpawnChasingFireballCoroutine());
                    }
                    else
                    {
                        Debug.LogWarning($"[HybridTowerController] Cannot spawn fireball - player reference is NULL!");
                    }
                    _nextFireballSpawnTimer = fireballSpawnInterval;
                }
            }
        }
        else
        {
            if (Time.time % 5f < Time.deltaTime) // Log this less frequently
            {
                Debug.LogWarning($"[HybridTowerController] Not spawning because _canSpawnSkullsBasedOnPlayerPresence is FALSE");
            }
        }
    }

    private IEnumerator SpawnChasingFireballCoroutine()
    {
        // Use cached player reference instead of FindGameObjectWithTag for performance
        if (fireballPrefab == null || fireballSpawnPoint == null || IsDead || _cachedPlayerTransform == null)
            yield break;

        GameObject fireballGO = Instantiate(fireballPrefab, fireballSpawnPoint.position, Quaternion.identity);

        ChasingFireball newFireball = fireballGO.GetComponent<ChasingFireball>();
        if (newFireball != null)
        {
            newFireball.Initialize(_cachedPlayerTransform);
        }

        Rigidbody fireballRb = fireballGO.GetComponent<Rigidbody>();
        if (fireballRb != null)
        {
            Vector3 launchDirection = (fireballSpawnPoint.forward + Vector3.up * 0.5f).normalized;
            fireballRb.AddForce(launchDirection * fireballLaunchForce, ForceMode.VelocityChange);
        }
    }
}