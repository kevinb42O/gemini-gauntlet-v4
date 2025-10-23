using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Advanced Procedural Platform Generator for Gemini Gauntlet
/// Generates an endless field of platforms with dynamic loading/unloading
/// Large platforms: 3400x3400 with 15000 unit spacing
/// Small bridging platforms: 250x250 between large platforms
/// </summary>
public class ProceduralPlatformGenerator : MonoBehaviour
{
    [Header("Platform Configuration")]
    [SerializeField] private GameObject largePlatformPrefab;
    [SerializeField] private GameObject smallPlatformPrefab;
    [SerializeField] private Transform player;
    
    [Header("Generation Settings")]
    [SerializeField] private float largePlatformSize = 3400f;
    [SerializeField] private float smallPlatformSize = 250f;
    [SerializeField] private float platformSpacing = 15000f;
    [SerializeField] private int renderDistance = 3; // How many platform grids to render around player
    [SerializeField] private float platformHeight = 0f;
    
    [Header("Small Platform Generation")]
    [SerializeField] private int bridgePlatformsPerSegment = 8; // Number of small platforms between large ones
    [SerializeField] private float bridgePlatformHeightVariation = 200f;
    [SerializeField] private bool enableRandomBridgePatterns = true;
    
    [Header("Advanced Features")]
    [SerializeField] private bool enablePlatformVariety = true;
    [SerializeField] private bool enableDynamicLoading = true;
    [SerializeField] private float unloadDistance = 50000f;
    [SerializeField] private bool enablePerformanceOptimization = true;
    
    [Header("Visual Enhancements")]
    [SerializeField] private Material[] largePlatformMaterials;
    [SerializeField] private Material[] smallPlatformMaterials;
    [SerializeField] private bool enablePlatformRotation = true;
    [SerializeField] private bool enableHeightVariation = true;
    [SerializeField] private float heightVariationRange = 500f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool showGizmos = true;
    
    // Internal data structures
    private Dictionary<Vector2Int, PlatformCluster> activeClusters = new Dictionary<Vector2Int, PlatformCluster>();
    private Queue<PlatformCluster> clusterPool = new Queue<PlatformCluster>();
    private Vector2Int lastPlayerGridPosition = Vector2Int.zero;
    private Coroutine generationCoroutine;
    
    // Performance tracking
    private int totalPlatformsGenerated = 0;
    private int activePlatformCount = 0;
    private float lastGenerationTime = 0f;
    
    // Noise generators for procedural variety
    private System.Random randomGenerator;
    private int seed = 12345;
    
    private class PlatformCluster
    {
        public Vector2Int gridPosition;
        public GameObject largePlatform;
        public List<GameObject> smallPlatforms = new List<GameObject>();
        public bool isActive = false;
        public float lastAccessTime;
        
        public void Cleanup()
        {
            if (largePlatform != null)
            {
                DestroyImmediate(largePlatform);
                largePlatform = null;
            }
            
            foreach (var platform in smallPlatforms)
            {
                if (platform != null)
                    DestroyImmediate(platform);
            }
            smallPlatforms.Clear();
            isActive = false;
        }
    }
    
    void Start()
    {
        InitializeGenerator();
        StartGeneration();
    }
    
    void InitializeGenerator()
    {
        randomGenerator = new System.Random(seed);
        
        // Find player if not assigned
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }
        
        // Create default prefabs if not assigned
        if (largePlatformPrefab == null)
            largePlatformPrefab = CreateDefaultPlatformPrefab(largePlatformSize, "LargePlatform");
        
        if (smallPlatformPrefab == null)
            smallPlatformPrefab = CreateDefaultPlatformPrefab(smallPlatformSize, "SmallPlatform");
        
        Debug.Log($"[ProceduralPlatformGenerator] Initialized with seed: {seed}");
    }
    
    GameObject CreateDefaultPlatformPrefab(float size, string name)
    {
        GameObject prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        prefab.name = name;
        prefab.transform.localScale = new Vector3(size, 100f, size);
        
        // Add a distinctive material
        Renderer renderer = prefab.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = size > 1000f ? Color.green : Color.blue;
        renderer.material = mat;
        
        return prefab;
    }
    
    void StartGeneration()
    {
        if (generationCoroutine != null)
            StopCoroutine(generationCoroutine);
        
        generationCoroutine = StartCoroutine(GenerationLoop());
    }
    
    IEnumerator GenerationLoop()
    {
        while (true)
        {
            if (player != null)
            {
                Vector2Int currentGridPos = WorldToGridPosition(player.position);
                
                if (currentGridPos != lastPlayerGridPosition || activeClusters.Count == 0)
                {
                    float startTime = Time.realtimeSinceStartup;
                    
                    UpdatePlatformGeneration(currentGridPos);
                    
                    lastGenerationTime = Time.realtimeSinceStartup - startTime;
                    lastPlayerGridPosition = currentGridPos;
                }
                
                // Performance optimization: cleanup old platforms
                if (enablePerformanceOptimization)
                {
                    CleanupDistantPlatforms();
                }
            }
            
            yield return new WaitForSeconds(0.1f); // Check every 100ms
        }
    }
    
    void UpdatePlatformGeneration(Vector2Int centerGrid)
    {
        HashSet<Vector2Int> requiredGrids = new HashSet<Vector2Int>();
        
        // Calculate required grid positions around player
        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int z = -renderDistance; z <= renderDistance; z++)
            {
                requiredGrids.Add(centerGrid + new Vector2Int(x, z));
            }
        }
        
        // Generate missing clusters
        foreach (var gridPos in requiredGrids)
        {
            if (!activeClusters.ContainsKey(gridPos))
            {
                GeneratePlatformCluster(gridPos);
            }
        }
        
        // Mark clusters for potential cleanup
        var clustersToRemove = new List<Vector2Int>();
        foreach (var kvp in activeClusters)
        {
            if (!requiredGrids.Contains(kvp.Key))
            {
                float distance = Vector2.Distance(kvp.Key, centerGrid) * platformSpacing;
                if (distance > unloadDistance)
                {
                    clustersToRemove.Add(kvp.Key);
                }
            }
        }
        
        // Remove distant clusters
        foreach (var gridPos in clustersToRemove)
        {
            RemovePlatformCluster(gridPos);
        }
    }
    
    void GeneratePlatformCluster(Vector2Int gridPosition)
    {
        PlatformCluster cluster = GetPooledCluster();
        cluster.gridPosition = gridPosition;
        cluster.lastAccessTime = Time.time;
        
        Vector3 worldPosition = GridToWorldPosition(gridPosition);
        
        // Generate large platform
        cluster.largePlatform = GenerateLargePlatform(worldPosition, gridPosition);
        
        // Generate bridging small platforms
        GenerateSmallPlatforms(cluster, worldPosition, gridPosition);
        
        cluster.isActive = true;
        activeClusters[gridPosition] = cluster;
        
        totalPlatformsGenerated++;
        activePlatformCount = activeClusters.Count;
        
        if (showDebugInfo)
        {
            Debug.Log($"[ProceduralPlatformGenerator] Generated cluster at {gridPosition}, World: {worldPosition}");
        }
    }
    
    GameObject GenerateLargePlatform(Vector3 worldPosition, Vector2Int gridPosition)
    {
        GameObject platform = Instantiate(largePlatformPrefab, transform);
        
        // Add height variation
        float heightOffset = 0f;
        if (enableHeightVariation)
        {
            heightOffset = GetPerlinNoise(gridPosition.x * 0.1f, gridPosition.y * 0.1f) * heightVariationRange;
        }
        
        platform.transform.position = worldPosition + Vector3.up * (platformHeight + heightOffset);
        
        // Add rotation variation
        if (enablePlatformRotation)
        {
            float rotation = GetPerlinNoise(gridPosition.x * 0.05f, gridPosition.y * 0.05f) * 360f;
            platform.transform.rotation = Quaternion.Euler(0, rotation, 0);
        }
        
        // Apply random material if available
        if (enablePlatformVariety && largePlatformMaterials != null && largePlatformMaterials.Length > 0)
        {
            Renderer renderer = platform.GetComponent<Renderer>();
            if (renderer != null)
            {
                int materialIndex = Mathf.Abs(gridPosition.x + gridPosition.y * 1000) % largePlatformMaterials.Length;
                renderer.material = largePlatformMaterials[materialIndex];
            }
        }
        
        // Add platform identifier
        PlatformIdentifier identifier = platform.GetComponent<PlatformIdentifier>();
        if (identifier == null)
            identifier = platform.AddComponent<PlatformIdentifier>();
        
        identifier.gridPosition = gridPosition;
        identifier.platformType = PlatformType.Large;
        
        return platform;
    }
    
    void GenerateSmallPlatforms(PlatformCluster cluster, Vector3 centerPosition, Vector2Int gridPosition)
    {
        // Generate bridges to adjacent large platforms
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        
        foreach (var direction in directions)
        {
            GenerateBridgePlatforms(cluster, centerPosition, gridPosition, direction);
        }
        
        // Generate some random small platforms within the area for variety
        if (enableRandomBridgePatterns)
        {
            GenerateRandomSmallPlatforms(cluster, centerPosition, gridPosition);
        }
    }
    
    void GenerateBridgePlatforms(PlatformCluster cluster, Vector3 centerPosition, Vector2Int gridPosition, Vector2Int direction)
    {
        Vector3 directionVector = new Vector3(direction.x, 0, direction.y) * platformSpacing;
        Vector3 targetPosition = centerPosition + directionVector;
        
        // Create bridge platforms between center and target
        for (int i = 1; i <= bridgePlatformsPerSegment; i++)
        {
            float t = (float)i / (bridgePlatformsPerSegment + 1);
            Vector3 bridgePosition = Vector3.Lerp(centerPosition, targetPosition, t);
            
            // Add some randomness to bridge positions
            float randomOffset = GetPerlinNoise(bridgePosition.x * 0.001f, bridgePosition.z * 0.001f) * 1000f;
            bridgePosition += Vector3.right * randomOffset;
            randomOffset = GetPerlinNoise(bridgePosition.z * 0.001f, bridgePosition.x * 0.001f) * 1000f;
            bridgePosition += Vector3.forward * randomOffset;
            
            // Add height variation for bridges
            float heightOffset = GetPerlinNoise(bridgePosition.x * 0.002f, bridgePosition.z * 0.002f) * bridgePlatformHeightVariation;
            bridgePosition.y = platformHeight + heightOffset;
            
            GameObject smallPlatform = CreateSmallPlatform(bridgePosition, gridPosition, i);
            cluster.smallPlatforms.Add(smallPlatform);
        }
    }
    
    void GenerateRandomSmallPlatforms(PlatformCluster cluster, Vector3 centerPosition, Vector2Int gridPosition)
    {
        int randomPlatformCount = randomGenerator.Next(5, 15);
        
        for (int i = 0; i < randomPlatformCount; i++)
        {
            // Generate random position within the platform area
            float randomX = (float)(randomGenerator.NextDouble() - 0.5) * platformSpacing * 0.8f;
            float randomZ = (float)(randomGenerator.NextDouble() - 0.5) * platformSpacing * 0.8f;
            
            Vector3 randomPosition = centerPosition + new Vector3(randomX, 0, randomZ);
            
            // Add height variation
            float heightOffset = GetPerlinNoise(randomPosition.x * 0.003f, randomPosition.z * 0.003f) * bridgePlatformHeightVariation;
            randomPosition.y = platformHeight + heightOffset;
            
            GameObject smallPlatform = CreateSmallPlatform(randomPosition, gridPosition, 100 + i);
            cluster.smallPlatforms.Add(smallPlatform);
        }
    }
    
    GameObject CreateSmallPlatform(Vector3 position, Vector2Int gridPosition, int index)
    {
        GameObject platform = Instantiate(smallPlatformPrefab, transform);
        platform.transform.position = position;
        
        // Add slight rotation variation
        if (enablePlatformRotation)
        {
            float rotation = GetPerlinNoise(position.x * 0.01f, position.z * 0.01f) * 180f;
            platform.transform.rotation = Quaternion.Euler(0, rotation, 0);
        }
        
        // Apply random material if available
        if (enablePlatformVariety && smallPlatformMaterials != null && smallPlatformMaterials.Length > 0)
        {
            Renderer renderer = platform.GetComponent<Renderer>();
            if (renderer != null)
            {
                int materialIndex = (gridPosition.x + gridPosition.y + index) % smallPlatformMaterials.Length;
                renderer.material = smallPlatformMaterials[materialIndex];
            }
        }
        
        // Add platform identifier
        PlatformIdentifier identifier = platform.GetComponent<PlatformIdentifier>();
        if (identifier == null)
            identifier = platform.AddComponent<PlatformIdentifier>();
        
        identifier.gridPosition = gridPosition;
        identifier.platformType = PlatformType.Small;
        identifier.index = index;
        
        return platform;
    }
    
    void RemovePlatformCluster(Vector2Int gridPosition)
    {
        if (activeClusters.TryGetValue(gridPosition, out PlatformCluster cluster))
        {
            cluster.Cleanup();
            clusterPool.Enqueue(cluster);
            activeClusters.Remove(gridPosition);
            activePlatformCount = activeClusters.Count;
            
            if (showDebugInfo)
            {
                Debug.Log($"[ProceduralPlatformGenerator] Removed cluster at {gridPosition}");
            }
        }
    }
    
    void CleanupDistantPlatforms()
    {
        if (player == null) return;
        
        var clustersToRemove = new List<Vector2Int>();
        Vector2Int playerGrid = WorldToGridPosition(player.position);
        
        foreach (var kvp in activeClusters)
        {
            float distance = Vector2.Distance(kvp.Key, playerGrid);
            if (distance > renderDistance + 2) // Add buffer
            {
                clustersToRemove.Add(kvp.Key);
            }
        }
        
        foreach (var gridPos in clustersToRemove)
        {
            RemovePlatformCluster(gridPos);
        }
    }
    
    PlatformCluster GetPooledCluster()
    {
        if (clusterPool.Count > 0)
        {
            return clusterPool.Dequeue();
        }
        return new PlatformCluster();
    }
    
    Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        int gridX = Mathf.RoundToInt(worldPosition.x / platformSpacing);
        int gridZ = Mathf.RoundToInt(worldPosition.z / platformSpacing);
        return new Vector2Int(gridX, gridZ);
    }
    
    Vector3 GridToWorldPosition(Vector2Int gridPosition)
    {
        return new Vector3(gridPosition.x * platformSpacing, 0, gridPosition.y * platformSpacing);
    }
    
    float GetPerlinNoise(float x, float y)
    {
        return Mathf.PerlinNoise(x + seed * 0.1f, y + seed * 0.1f);
    }
    
    // Public API methods
    public void RegenerateAroundPlayer()
    {
        if (player != null)
        {
            lastPlayerGridPosition = Vector2Int.zero; // Force regeneration
        }
    }
    
    public void ClearAllPlatforms()
    {
        var allGrids = activeClusters.Keys.ToList();
        foreach (var grid in allGrids)
        {
            RemovePlatformCluster(grid);
        }
    }
    
    public void SetSeed(int newSeed)
    {
        seed = newSeed;
        randomGenerator = new System.Random(seed);
        Debug.Log($"[ProceduralPlatformGenerator] Seed changed to: {seed}");
    }
    
    // Debug and utility methods
    void OnDrawGizmos()
    {
        if (!showGizmos || player == null) return;
        
        Vector2Int playerGrid = WorldToGridPosition(player.position);
        
        // Draw render area
        Gizmos.color = Color.yellow;
        Vector3 center = GridToWorldPosition(playerGrid);
        float renderSize = renderDistance * platformSpacing * 2;
        Gizmos.DrawWireCube(center, new Vector3(renderSize, 100, renderSize));
        
        // Draw active platform grids
        Gizmos.color = Color.green;
        foreach (var kvp in activeClusters)
        {
            Vector3 gridCenter = GridToWorldPosition(kvp.Key);
            Gizmos.DrawWireCube(gridCenter, Vector3.one * platformSpacing * 0.1f);
        }
        
        // Draw player position
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(player.position, 100f);
    }
    
    void OnGUI()
    {
        if (!showDebugInfo) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label($"=== Procedural Platform Generator ===");
        GUILayout.Label($"Active Clusters: {activePlatformCount}");
        GUILayout.Label($"Total Generated: {totalPlatformsGenerated}");
        GUILayout.Label($"Last Gen Time: {lastGenerationTime:F4}s");
        GUILayout.Label($"Seed: {seed}");
        
        if (player != null)
        {
            Vector2Int playerGrid = WorldToGridPosition(player.position);
            GUILayout.Label($"Player Grid: {playerGrid}");
            GUILayout.Label($"Player Pos: {player.position}");
        }
        
        if (GUILayout.Button("Regenerate"))
        {
            RegenerateAroundPlayer();
        }
        
        if (GUILayout.Button("Clear All"))
        {
            ClearAllPlatforms();
        }
        
        GUILayout.EndArea();
    }
}

public enum PlatformType
{
    Large,
    Small
}

/// <summary>
/// Component attached to generated platforms for identification
/// </summary>
public class PlatformIdentifier : MonoBehaviour
{
    public Vector2Int gridPosition;
    public PlatformType platformType;
    public int index;
    
    void Start()
    {
        // Optional: Add platform-specific behavior here
        gameObject.name = $"{platformType}Platform_{gridPosition.x}_{gridPosition.y}_{index}";
    }
}
