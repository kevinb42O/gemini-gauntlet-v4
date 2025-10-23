using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawn level configuration for flying skulls
/// </summary>
[System.Serializable]
public class SkullSpawnLevel
{
    [Tooltip("Name for this level (for debugging)")]
    public string levelName = "Level 1";
    
    [Tooltip("Center point for this spawn level")]
    public Transform centerPoint;
    
    [Tooltip("Number of flying skulls to spawn at this level")]
    [Range(1, 30)] public int skullCount = 5;
    
    [Tooltip("Spawn radius for this level")]
    [Range(10f, 200f)] public float spawnRadius = 50f;
    
    [Tooltip("Y-axis height offset (optional)")]
    public float heightOffset = 0f;
    
    [Tooltip("Randomize spawn height within this range (creates vertical variation)")]
    [Range(0f, 20f)] public float verticalSpread = 5f;
}

/// <summary>
/// 💀 FLYING SKULL SPAWN MANAGER
/// 
/// Spawns persistent flying skull enemies across multiple levels in your world.
/// Perfect for indoor environments with floors/levels.
/// 
/// FEATURES:
/// - Multi-level spawning (3 levels by default, expandable)
/// - Configurable spawn radii per level
/// - Automatic spacing to prevent clustering
/// - Height variation for natural placement
/// - Performance-optimized (spawns at Start only)
/// - Visual gizmos for easy setup
/// 
/// SETUP:
/// 1. Add this script to an empty GameObject
/// 2. Assign your flying skull prefab
/// 3. Create 3 empty GameObjects as center points for each level
/// 4. Configure spawn settings for each level
/// 5. Adjust radii and counts as needed
/// </summary>
public class FlyingSkullSpawnManager : MonoBehaviour
{
    [Header("🎯 Flying Skull Prefab")]
    [Tooltip("The FlyingSkullEnemy prefab to spawn")]
    public GameObject flyingSkullPrefab;
    
    [Header("🏢 MULTI-LEVEL SPAWN CONFIGURATION")]
    [Tooltip("Spawn levels - configure 3 for your indoor area")]
    public SkullSpawnLevel[] spawnLevels = new SkullSpawnLevel[3];
    
    [Header("🚫 Spacing & Validation")]
    [Tooltip("Minimum distance between spawned skulls")]
    [Range(5f, 50f)] public float minSpacing = 10f;
    
    [Tooltip("Check for wall collisions before spawning")]
    public bool validateSpawnPositions = true;
    
    [Tooltip("Layers that block spawn positions (walls, obstacles)")]
    public LayerMask obstacleLayers = -1;
    
    [Tooltip("Sphere check radius for spawn validation")]
    [Range(1f, 5f)] public float spawnCheckRadius = 2f;
    
    [Header("⚡ Performance")]
    [Tooltip("Parent spawned skulls to this manager for hierarchy organization")]
    public bool parentSkullsToManager = true;
    
    [Tooltip("Maximum spawn attempts per skull (prevents infinite loops)")]
    [Range(10, 100)] public int maxAttemptsPerSkull = 50;
    
    [Header("🎨 Visualization")]
    [Tooltip("Show spawn areas in editor")]
    public bool showGizmos = true;
    
    [Tooltip("Show detailed debug logs during spawning")]
    public bool enableDebugLogs = true;
    
    [Header("📊 Runtime Info (Read-Only)")]
    [SerializeField] private int totalSpawned = 0;
    [SerializeField] private int failedAttempts = 0;
    
    private List<Vector3> spawnedPositions = new List<Vector3>();
    private List<GameObject> spawnedSkulls = new List<GameObject>();
    
    void Start()
    {
        LogInfo("🚀 FlyingSkullSpawnManager starting...");
        
        if (flyingSkullPrefab == null)
        {
            LogError("❌ No flying skull prefab assigned! Assign FlyingSkullEnemy prefab in inspector.");
            return;
        }
        
        // Validate prefab has FlyingSkullEnemy component
        if (flyingSkullPrefab.GetComponent<FlyingSkullEnemy>() == null)
        {
            LogError($"❌ Prefab '{flyingSkullPrefab.name}' is missing FlyingSkullEnemy component!");
            return;
        }
        
        LogInfo($"✅ Prefab validated: {flyingSkullPrefab.name}");
        SpawnAllLevels();
    }
    
    private void SpawnAllLevels()
    {
        if (spawnLevels == null || spawnLevels.Length == 0)
        {
            LogError("❌ No spawn levels configured! Add spawn levels in inspector.");
            return;
        }
        
        totalSpawned = 0;
        failedAttempts = 0;
        
        // Spawn each level
        for (int i = 0; i < spawnLevels.Length; i++)
        {
            SkullSpawnLevel level = spawnLevels[i];
            
            if (level.centerPoint == null)
            {
                LogWarning($"⚠️ Level {i} ({level.levelName}) has no center point - skipping!");
                continue;
            }
            
            if (level.skullCount <= 0)
            {
                LogInfo($"⏭️ Level {i} ({level.levelName}) has 0 skulls - skipping");
                continue;
            }
            
            SpawnLevel(level, i);
        }
        
        // Final report
        LogInfo($"🎯 SPAWN COMPLETE: {totalSpawned} flying skulls spawned across {spawnLevels.Length} levels");
        LogInfo($"Failed attempts: {failedAttempts}");
    }
    
    private void SpawnLevel(SkullSpawnLevel level, int levelIndex)
    {
        Vector3 centerPos = level.centerPoint.position + Vector3.up * level.heightOffset;
        
        LogInfo($"🏢 Level {levelIndex} ({level.levelName}): Spawning {level.skullCount} skulls");
        LogInfo($"   Center: {centerPos}, Radius: {level.spawnRadius}");
        
        int levelSpawnCount = 0;
        int attempts = 0;
        
        while (levelSpawnCount < level.skullCount && attempts < maxAttemptsPerSkull * level.skullCount)
        {
            attempts++;
            
            // Generate random position in circle
            Vector2 randomCircle = Random.insideUnitCircle * level.spawnRadius;
            float randomHeight = Random.Range(-level.verticalSpread, level.verticalSpread);
            Vector3 randomPos = centerPos + new Vector3(randomCircle.x, randomHeight, randomCircle.y);
            
            // Validate position
            if (IsValidSpawnPosition(randomPos))
            {
                // Check spacing
                if (IsValidSpacing(randomPos))
                {
                    // Spawn skull
                    GameObject skull = Instantiate(
                        flyingSkullPrefab, 
                        randomPos, 
                        Quaternion.identity, 
                        parentSkullsToManager ? transform : null
                    );
                    
                    skull.name = $"{level.levelName}_FlyingSkull_{levelSpawnCount + 1}";
                    
                    spawnedPositions.Add(randomPos);
                    spawnedSkulls.Add(skull);
                    totalSpawned++;
                    levelSpawnCount++;
                    
                    LogInfo($"✅ {level.levelName}: Spawned skull {levelSpawnCount}/{level.skullCount} at {randomPos}");
                }
                else
                {
                    failedAttempts++;
                }
            }
            else
            {
                failedAttempts++;
            }
        }
        
        // Level report
        LogInfo($"🏢 {level.levelName} COMPLETE: {levelSpawnCount}/{level.skullCount} skulls spawned");
        
        if (levelSpawnCount < level.skullCount)
        {
            LogWarning($"⚠️ {level.levelName}: Only spawned {levelSpawnCount}/{level.skullCount}. Try increasing radius or decreasing spacing.");
        }
    }
    
    private bool IsValidSpawnPosition(Vector3 position)
    {
        if (!validateSpawnPositions)
            return true;
        
        // Check for obstacles using sphere cast
        Collider[] overlaps = Physics.OverlapSphere(position, spawnCheckRadius, obstacleLayers);
        
        if (overlaps.Length > 0)
        {
            // Position is inside or too close to an obstacle
            return false;
        }
        
        return true;
    }
    
    private bool IsValidSpacing(Vector3 position)
    {
        foreach (Vector3 existingPos in spawnedPositions)
        {
            float distance = Vector3.Distance(position, existingPos);
            if (distance < minSpacing)
            {
                return false; // Too close to another skull
            }
        }
        
        return true; // Good spacing
    }
    
    /// <summary>
    /// Get all spawned flying skulls (useful for other scripts)
    /// </summary>
    public List<GameObject> GetSpawnedSkulls()
    {
        return new List<GameObject>(spawnedSkulls);
    }
    
    /// <summary>
    /// Get count of alive flying skulls
    /// </summary>
    public int GetAliveSkullCount()
    {
        int aliveCount = 0;
        foreach (GameObject skull in spawnedSkulls)
        {
            if (skull != null)
            {
                FlyingSkullEnemy skullEnemy = skull.GetComponent<FlyingSkullEnemy>();
                if (skullEnemy != null && !skullEnemy.IsDead())
                {
                    aliveCount++;
                }
            }
        }
        return aliveCount;
    }
    
    #region Logging Helpers
    
    private void LogInfo(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[FlyingSkullSpawnManager] {message}");
    }
    
    private void LogWarning(string message)
    {
        Debug.LogWarning($"[FlyingSkullSpawnManager] {message}");
    }
    
    private void LogError(string message)
    {
        Debug.LogError($"[FlyingSkullSpawnManager] {message}");
    }
    
    #endregion
    
    #region Gizmos
    
    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        if (spawnLevels == null || spawnLevels.Length == 0) return;
        
        // Level colors
        Color[] levelColors = new Color[] 
        { 
            new Color(1f, 0f, 0f, 0.3f),    // Red - Level 1
            new Color(0f, 1f, 0f, 0.3f),    // Green - Level 2
            new Color(0f, 0.5f, 1f, 0.3f),  // Blue - Level 3
            new Color(1f, 1f, 0f, 0.3f),    // Yellow - Level 4
            new Color(1f, 0f, 1f, 0.3f)     // Magenta - Level 5
        };
        
        for (int i = 0; i < spawnLevels.Length; i++)
        {
            SkullSpawnLevel level = spawnLevels[i];
            if (level.centerPoint == null) continue;
            
            Vector3 center = level.centerPoint.position + Vector3.up * level.heightOffset;
            Color levelColor = i < levelColors.Length ? levelColors[i] : Color.white;
            
            // Draw spawn radius circle
            Gizmos.color = levelColor;
            DrawCircle(center, level.spawnRadius, 64);
            
            // Draw vertical spread
            if (level.verticalSpread > 0)
            {
                Color spreadColor = levelColor;
                spreadColor.a = 0.1f;
                Gizmos.color = spreadColor;
                DrawCircle(center + Vector3.up * level.verticalSpread, level.spawnRadius, 32);
                DrawCircle(center - Vector3.up * level.verticalSpread, level.spawnRadius, 32);
            }
            
            // Draw center point
            Gizmos.color = levelColor;
            Gizmos.DrawSphere(center, 1f);
            
            // Draw level label in scene view
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(center + Vector3.up * 2f, $"{level.levelName}\n{level.skullCount} skulls");
            #endif
        }
        
        // Draw spawned positions in play mode
        if (Application.isPlaying && spawnedPositions.Count > 0)
        {
            Gizmos.color = Color.yellow;
            foreach (Vector3 pos in spawnedPositions)
            {
                Gizmos.DrawWireSphere(pos, 0.5f);
            }
        }
    }
    
    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
    
    #endregion
    
    #region Editor Utilities
    
    #if UNITY_EDITOR
    [ContextMenu("Preview Spawn Report")]
    private void PreviewSpawnReport()
    {
        if (!Application.isPlaying)
        {
            Debug.Log("[FlyingSkullSpawnManager] ⚠️ Preview only works in Play Mode");
            return;
        }
        
        int totalConfigured = 0;
        foreach (SkullSpawnLevel level in spawnLevels)
        {
            if (level != null) totalConfigured += level.skullCount;
        }
        
        Debug.Log($"[FlyingSkullSpawnManager] 📊 SPAWN REPORT:");
        Debug.Log($"  - Configured: {totalConfigured} skulls");
        Debug.Log($"  - Spawned: {totalSpawned}");
        Debug.Log($"  - Alive: {GetAliveSkullCount()}");
        Debug.Log($"  - Failed Attempts: {failedAttempts}");
    }
    
    [ContextMenu("Despawn All Skulls")]
    private void DespawnAllSkulls()
    {
        if (!Application.isPlaying)
        {
            Debug.Log("[FlyingSkullSpawnManager] ⚠️ Can only despawn in Play Mode");
            return;
        }
        
        foreach (GameObject skull in spawnedSkulls)
        {
            if (skull != null)
            {
                Destroy(skull);
            }
        }
        
        spawnedSkulls.Clear();
        spawnedPositions.Clear();
        totalSpawned = 0;
        
        Debug.Log("[FlyingSkullSpawnManager] 🗑️ All flying skulls despawned");
    }
    #endif
    
    #endregion
}

// Extension method for FlyingSkullEnemy to check if dead
public static class FlyingSkullEnemyExtensions
{
    public static bool IsDead(this FlyingSkullEnemy skull)
    {
        // Uses public IsDead property from FlyingSkullEnemy
        return skull == null || skull.IsDead;
    }
}
