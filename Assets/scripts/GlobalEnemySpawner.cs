// --- REFACTORED: GlobalEnemySpawner.cs ---
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GlobalEnemySpawner : MonoBehaviour
{
    // ... (Your header and variables are fine) ...
    [Header("Spawning Configuration")]
    public GameObject enemyPrefab;
    public int enemiesPerWave = 3;
    public float spawnRadiusAroundPlatform = 15f;
    public float spawnHeightOffset = 5f;

    [Header("Performance")]
    [Tooltip("Use pooling for enemies")] public bool usePooling = true;
    [Tooltip("Prewarm pooled enemies on Start")] public bool prewarmOnStart = true;
    [Tooltip("How many enemies to prewarm")] public int prewarmCount = 30;
    [Tooltip("Max enemies spawned per frame")] public int spawnPerFrame = 3;
    [Tooltip("Small random delay between spawns to de-sync AI")] public Vector2 spawnJitterRange = new Vector2(0.0f, 0.03f);

    private Transform playerTransform;

    void OnEnable()
    {
        // --- MODIFIED: Subscribe to the correct event owner ---
        PlayerMovementManager.OnPlayerLandedOnNewPlatform += SpawnWave;
    }

    void OnDisable()
    {
        // --- MODIFIED: Unsubscribe from the correct event owner ---
        PlayerMovementManager.OnPlayerLandedOnNewPlatform -= SpawnWave;
    }

    void Start()
    {
        // FindFirstObjectByType is a bit slow, let's find the player via its tag.
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if(playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogError("GlobalEnemySpawner: Cannot find the player (tagged 'Player') in the scene!", this);
            enabled = false;
        }

        if (prewarmOnStart && usePooling && enemyPrefab != null)
        {
            PoolManager.PrewarmStatic(enemyPrefab, Mathf.Max(prewarmCount, enemiesPerWave));

            // Also prewarm dependent pooled objects referenced by the enemy prefab (if any)
            var skullPrefabComp = enemyPrefab.GetComponent<SkullEnemy>();
            if (skullPrefabComp != null)
            {
                if (skullPrefabComp.deathEffectPrefab != null)
                {
                    PoolManager.PrewarmStatic(skullPrefabComp.deathEffectPrefab, Mathf.Max(8, enemiesPerWave));
                }

                if (skullPrefabComp.powerUpPrefabs != null)
                {
                    // Small pool for each powerup to eliminate first-use hitches
                    foreach (var p in skullPrefabComp.powerUpPrefabs)
                    {
                        if (p != null)
                            PoolManager.PrewarmStatic(p, 2);
                    }
                }
            }
        }
    }

    // The rest of the script is logically sound and does not need changes.
    private void SpawnWave(Transform platformLandedOn)
    {
        if (enemyPrefab == null || playerTransform == null) return;
        StartCoroutine(SpawnWaveRoutine(platformLandedOn));
    }

    private IEnumerator SpawnWaveRoutine(Transform platformLandedOn)
    {
        Debug.Log($"Spawning wave of {enemiesPerWave} enemies around platform {platformLandedOn.name}");
        int spawnedThisFrame = 0;
        for (int i = 0; i < enemiesPerWave; i++)
        {
            Vector2 randomCirclePos = Random.insideUnitCircle.normalized * spawnRadiusAroundPlatform;
            Vector3 spawnPos = platformLandedOn.position + new Vector3(randomCirclePos.x, spawnHeightOffset, randomCirclePos.y);
            Quaternion rot = Quaternion.LookRotation(playerTransform.position - spawnPos);

            GameObject enemyGO = usePooling
                ? PoolManager.SpawnStatic(enemyPrefab, spawnPos, rot)
                : Instantiate(enemyPrefab, spawnPos, rot);

            SkullEnemy skull = enemyGO != null ? enemyGO.GetComponent<SkullEnemy>() : null;
            if (skull != null)
            {
                skull.InitializeSkull(null, null, true);
            }

            spawnedThisFrame++;
            if (spawnedThisFrame >= Mathf.Max(1, spawnPerFrame))
            {
                spawnedThisFrame = 0;
                float jitter = Random.Range(spawnJitterRange.x, spawnJitterRange.y);
                if (jitter > 0f) yield return new WaitForSeconds(jitter);
                else yield return null; // spread over frames
            }
        }
    }

    // --- NEW: Methods called by the state machine ---
    public void OnPlayerEnteredFlight()
    {
        Debug.Log("GlobalEnemySpawner: Player entered flight mode");
        // Future: Could despawn ground enemies, spawn flying enemies, etc.
    }

    public void OnPlayerLanded(CelestialPlatform platform)
    {
        Debug.Log($"GlobalEnemySpawner: Player landed on {platform.name}");
        // Spawn wave using existing logic
        SpawnWave(platform.transform);
    }

    public void OnPlayerTookOff(CelestialPlatform platform)
    {
        Debug.Log($"GlobalEnemySpawner: Player took off from {platform.name}");
        // Future: Could despawn platform-specific enemies, etc.
    }
}