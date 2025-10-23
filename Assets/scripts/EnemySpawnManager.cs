using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

namespace CompanionAI
{
    /// <summary>
    /// Floor-specific spawn settings for multi-level spawning
    /// </summary>
    [System.Serializable]
    public class FloorSpawnSettings
    {
        [Tooltip("Name of this floor (for debugging)")]
        public string floorName = "Ground Floor";
        
        [Tooltip("Center point for this floor (required!)")]
        public Transform floorCenter;
        
        [Tooltip("How many enemies to spawn on this floor")]
        [Range(0, 50)] public int enemyCount = 5;
        
        [Tooltip("Spawn radius for this floor")]
        [Range(5000f, 100000f)] public float spawnRadius = 25000f;
        
        [Tooltip("Y-axis height offset (optional, for fine-tuning)")]
        public float heightOffset = 0f;
    }
    
    /// <summary>
    /// 🎯 ENEMY SPAWN MANAGER - Multi-level support!
    /// 
    /// PERFORMANCE OPTIMIZED:
    /// - Spawns at Start() only (no runtime spawning)
    /// - Uses NavMesh.SamplePosition (fast)
    /// - Ensures proper spacing between enemies
    /// - Validates spawn positions
    /// - Supports multiple floors/levels
    /// </summary>
    public class EnemySpawnManager : MonoBehaviour
    {
        [Header("🎯 Spawn Settings")]
        [Tooltip("Enemy prefab to spawn (must have EnemyCompanionBehavior)")]
        public GameObject enemyPrefab;
        
        [Header("🏢 MULTI-LEVEL SPAWNING")]
        [Tooltip("Define spawn settings for each floor/level")]
        public FloorSpawnSettings[] floors = new FloorSpawnSettings[3];
        
        [Header("🚫 Spacing")]
        [Tooltip("Minimum distance between spawned enemies")]
        [Range(1000f, 20000f)] public float minSpacing = 5000f;
        
        [Header("🔍 NavMesh Settings")]
        [Tooltip("Max distance to search for valid NavMesh position")]
        [Range(100f, 5000f)] public float navMeshSearchDistance = 1000f;
        
        [Tooltip("Ground layer mask (optional - for extra validation)")]
        public LayerMask groundLayers = -1;
        
        [Header("🎨 Visualization")]
        [Tooltip("Show spawn area in editor")]
        public bool showGizmos = true;
        
        [Tooltip("Show debug logs during spawning")]
        public bool enableDebugLogs = true;
        
        [Header("📊 Runtime Info (Read-Only)")]
        [SerializeField] private int spawnedCount = 0;
        [SerializeField] private int failedAttempts = 0;
        
        private List<Vector3> spawnedPositions = new List<Vector3>();
        private List<GameObject> spawnedEnemies = new List<GameObject>();
        
        void Start()
        {
            Debug.Log("[EnemySpawnManager] 🚀 Start() called - Beginning spawn process...");
            
            if (enemyPrefab == null)
            {
                Debug.LogError("[EnemySpawnManager] ❌ CRITICAL: No enemy prefab assigned! Assign prefab in inspector!");
                return;
            }
            
            Debug.Log($"[EnemySpawnManager] ✅ Enemy prefab assigned: {enemyPrefab.name}");
            
            // Validate prefab has required components
            if (enemyPrefab.GetComponent<EnemyCompanionBehavior>() == null)
            {
                Debug.LogError($"[EnemySpawnManager] ❌ CRITICAL: Enemy prefab '{enemyPrefab.name}' is missing EnemyCompanionBehavior component!");
                Debug.LogError("[EnemySpawnManager] Add EnemyCompanionBehavior to your prefab and set isEnemy = TRUE");
                return;
            }
            
            Debug.Log("[EnemySpawnManager] ✅ Prefab validation passed - Starting spawn...");
            SpawnEnemies();
        }
        
        private void SpawnEnemies()
        {
            if (floors == null || floors.Length == 0)
            {
                Debug.LogError("[EnemySpawnManager] ❌ No floors defined! Add floor settings in inspector.");
                return;
            }
            
            spawnedCount = 0;
            failedAttempts = 0;
            
            // Spawn on each floor
            for (int floorIndex = 0; floorIndex < floors.Length; floorIndex++)
            {
                FloorSpawnSettings floor = floors[floorIndex];
                
                if (floor.floorCenter == null)
                {
                    Debug.LogWarning($"[EnemySpawnManager] ⚠️ Floor {floorIndex} ({floor.floorName}) has no center assigned - skipping!");
                    continue;
                }
                
                if (floor.enemyCount <= 0)
                {
                    if (enableDebugLogs)
                        Debug.Log($"[EnemySpawnManager] ⏭️ Floor {floorIndex} ({floor.floorName}) has 0 enemies - skipping");
                    continue;
                }
                
                SpawnOnFloor(floor, floorIndex);
            }
            
            // Final report
            Debug.Log($"[EnemySpawnManager] 🎯 SPAWN COMPLETE: {spawnedCount} total enemies spawned across {floors.Length} floors");
            Debug.Log($"[EnemySpawnManager] Failed attempts: {failedAttempts}");
        }
        
        private void SpawnOnFloor(FloorSpawnSettings floor, int floorIndex)
        {
            Vector3 center = floor.floorCenter.position + Vector3.up * floor.heightOffset;
            float floorHeight = center.y; // Remember the floor's Y position
            
            if (enableDebugLogs)
            {
                Debug.Log($"[EnemySpawnManager] 🏢 Floor {floorIndex} ({floor.floorName}): Spawning {floor.enemyCount} enemies");
                Debug.Log($"[EnemySpawnManager]    Center: {center}, Radius: {floor.spawnRadius}, Height: {floorHeight}");
            }
            
            int floorSpawnedCount = 0;
            int maxAttempts = floor.enemyCount * 10;
            int attempts = 0;
            
            while (floorSpawnedCount < floor.enemyCount && attempts < maxAttempts)
            {
                attempts++;
                
                // Generate random position in circle (use floor's radius!)
                Vector2 randomCircle = Random.insideUnitCircle * floor.spawnRadius;
                Vector3 randomPosition = center + new Vector3(randomCircle.x, 0, randomCircle.y);
                
                // Try to find valid NavMesh position ON THIS FLOOR
                if (TryGetValidSpawnPosition(randomPosition, floorHeight, out Vector3 validPosition))
                {
                    // Check spacing from other enemies
                    if (IsValidSpacing(validPosition))
                    {
                        // Spawn enemy
                        GameObject enemy = Instantiate(enemyPrefab, validPosition, Quaternion.identity, transform);
                        enemy.name = $"{floor.floorName}_Enemy_{floorSpawnedCount + 1}";
                        
                        spawnedPositions.Add(validPosition);
                        spawnedEnemies.Add(enemy);
                        spawnedCount++;
                        floorSpawnedCount++;
                        
                        if (enableDebugLogs)
                        {
                            Debug.Log($"[EnemySpawnManager] ✅ {floor.floorName}: Spawned enemy {floorSpawnedCount}/{floor.enemyCount} at {validPosition}");
                        }
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
            
            // Floor report
            if (enableDebugLogs)
            {
                Debug.Log($"[EnemySpawnManager] 🏢 {floor.floorName} COMPLETE: {floorSpawnedCount}/{floor.enemyCount} enemies spawned");
            }
            
            if (floorSpawnedCount < floor.enemyCount)
            {
                Debug.LogWarning($"[EnemySpawnManager] ⚠️ {floor.floorName}: Only spawned {floorSpawnedCount}/{floor.enemyCount}. Try increasing radius or decreasing spacing.");
            }
        }
        
        private bool TryGetValidSpawnPosition(Vector3 randomPosition, float expectedHeight, out Vector3 validPosition)
        {
            // Try to find NavMesh position
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPosition, out hit, navMeshSearchDistance, NavMesh.AllAreas))
            {
                validPosition = hit.position;
                
                // CRITICAL: Check if we're on the correct floor!
                // Allow 500 units tolerance for height difference (floors are ~1500 apart)
                float heightDifference = Mathf.Abs(validPosition.y - expectedHeight);
                
                if (heightDifference > 500f)
                {
                    // Wrong floor! NavMesh found a different level
                    if (enableDebugLogs && Random.value < 0.1f) // Log 10% of failures to avoid spam
                    {
                        Debug.Log($"[EnemySpawnManager] ⚠️ Wrong floor detected! Expected Y={expectedHeight:F0}, Got Y={validPosition.y:F0}, Diff={heightDifference:F0}");
                    }
                    validPosition = Vector3.zero;
                    return false;
                }
                
                // Correct floor! Optional: Extra validation with ground raycast
                if (groundLayers != -1)
                {
                    RaycastHit groundHit;
                    if (Physics.Raycast(validPosition + Vector3.up * 100f, Vector3.down, out groundHit, 200f, groundLayers))
                    {
                        // Ground found - use this position (but keep the NavMesh Y to stay on correct floor)
                        validPosition = new Vector3(groundHit.point.x, validPosition.y, groundHit.point.z);
                        return true;
                    }
                }
                
                return true;
            }
            
            validPosition = Vector3.zero;
            return false;
        }
        
        private bool IsValidSpacing(Vector3 position)
        {
            foreach (Vector3 existingPosition in spawnedPositions)
            {
                float distance = Vector3.Distance(position, existingPosition);
                if (distance < minSpacing)
                {
                    return false; // Too close to another enemy
                }
            }
            
            return true; // Good spacing
        }
        
        /// <summary>
        /// Get all spawned enemies (useful for other scripts)
        /// </summary>
        public List<GameObject> GetSpawnedEnemies()
        {
            return new List<GameObject>(spawnedEnemies);
        }
        
        /// <summary>
        /// Get count of alive enemies
        /// </summary>
        public int GetAliveEnemyCount()
        {
            int aliveCount = 0;
            foreach (GameObject enemy in spawnedEnemies)
            {
                if (enemy != null)
                {
                    aliveCount++;
                }
            }
            return aliveCount;
        }
        
        void OnDrawGizmos()
        {
            if (!showGizmos) return;
            
            if (floors == null || floors.Length == 0) return;
            
            // Draw each floor
            Color[] floorColors = new Color[] 
            { 
                new Color(1f, 0f, 0f, 0.3f),    // Red (Floor -1)
                new Color(0f, 1f, 0f, 0.3f),    // Green (Floor 0)
                new Color(0f, 0.5f, 1f, 0.3f)   // Blue (Floor 1)
            };
            
            for (int i = 0; i < floors.Length; i++)
            {
                FloorSpawnSettings floor = floors[i];
                if (floor.floorCenter == null) continue;
                
                Vector3 center = floor.floorCenter.position + Vector3.up * floor.heightOffset;
                Color floorColor = i < floorColors.Length ? floorColors[i] : Color.white;
                
                // Draw spawn radius
                Gizmos.color = floorColor;
                DrawCircle(center, floor.spawnRadius, 64);
                
                // Draw center point
                Gizmos.color = floorColor;
                Gizmos.DrawSphere(center, 500f);
            }
            
            // Draw spawned positions in play mode
            if (Application.isPlaying && spawnedPositions.Count > 0)
            {
                Gizmos.color = Color.yellow;
                foreach (Vector3 pos in spawnedPositions)
                {
                    Gizmos.DrawSphere(pos, 300f);
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
        
        #if UNITY_EDITOR
        [ContextMenu("Preview Spawn Positions")]
        private void PreviewSpawnPositions()
        {
            if (!Application.isPlaying)
            {
                Debug.Log("[EnemySpawnManager] ⚠️ Preview only works in Play Mode");
                return;
            }
            
            // Calculate total enemy count from all floors
            int totalEnemyCount = 0;
            foreach (FloorSpawnSettings floor in floors)
            {
                if (floor != null) totalEnemyCount += floor.enemyCount;
            }
            
            Debug.Log($"[EnemySpawnManager] 📊 SPAWN REPORT:");
            Debug.Log($"  - Spawned: {spawnedCount}/{totalEnemyCount}");
            Debug.Log($"  - Failed Attempts: {failedAttempts}");
            Debug.Log($"  - Alive Enemies: {GetAliveEnemyCount()}");
        }
        
        [ContextMenu("Despawn All Enemies")]
        private void DespawnAllEnemies()
        {
            if (!Application.isPlaying)
            {
                Debug.Log("[EnemySpawnManager] ⚠️ Can only despawn in Play Mode");
                return;
            }
            
            foreach (GameObject enemy in spawnedEnemies)
            {
                if (enemy != null)
                {
                    Destroy(enemy);
                }
            }
            
            spawnedEnemies.Clear();
            spawnedPositions.Clear();
            spawnedCount = 0;
            
            Debug.Log("[EnemySpawnManager] 🗑️ All enemies despawned");
        }
        #endif
    }
}
