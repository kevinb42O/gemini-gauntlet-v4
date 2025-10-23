// --- PlatformFillManager.cs ---
using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class PlatformFillManager : MonoBehaviour
{
    [System.Serializable]
    public class TierPrefabSet
    {
        public GameObject innerTierTowerPrefab;
        public GameObject middleTierTowerPrefab;
        public GameObject outerTierTowerPrefab;
        public GameObject emptyPrefab; // Optional - can be null
    }
    
    [Header("Tower Prefabs")]
    [SerializeField] private TierPrefabSet prefabSet;
    
    [Header("Fill Ratios")]
    [Range(0f, 1f)] [SerializeField] private float innerTierFillRatio = 0.8f;
    [Range(0f, 1f)] [SerializeField] private float middleTierFillRatio = 0.6f;
    [Range(0f, 1f)] [SerializeField] private float outerTierFillRatio = 0.4f;
    
    [Header("Platform References")]
    [SerializeField] private List<Transform> innerTierPlatforms = new List<Transform>();
    [SerializeField] private List<Transform> middleTierPlatforms = new List<Transform>();
    [SerializeField] private List<Transform> outerTierPlatforms = new List<Transform>();
    
    [Header("Tower Placement")]
    [SerializeField] private string towerAttachPointName = "TowerAttachPoint";
    [SerializeField] private Vector3 towerOffset = new Vector3(0, 0.5f, 0);
    
    [Header("Debug")]
#pragma warning disable 0414
    [SerializeField] private bool showDebugInfo = true;
#pragma warning restore 0414
    [SerializeField] private bool autoAssignPlatforms = false;
    [SerializeField] private bool clearAllTowers = false;
    [SerializeField] private bool placeTowers = false;
    
    // Dictionary to track which platforms have towers
    private Dictionary<Transform, GameObject> platformTowerMap = new Dictionary<Transform, GameObject>();
    
    private void OnEnable()
    {
        // Auto-assign platforms if needed
        if (autoAssignPlatforms)
        {
            AutoAssignPlatforms();
            autoAssignPlatforms = false;
        }
    }
    
    private void Update()
    {
        // Editor-only operations
        if (!Application.isPlaying)
        {
            if (clearAllTowers)
            {
                ClearAllTowers();
                clearAllTowers = false;
            }
            
            if (placeTowers)
            {
                PlaceTowersOnPlatforms();
                placeTowers = false;
            }
        }
    }
    
    public void AutoAssignPlatforms()
    {
        // Clear existing lists
        innerTierPlatforms.Clear();
        middleTierPlatforms.Clear();
        outerTierPlatforms.Clear();
        
        // Find all platforms
        CelestialPlatform[] allPlatforms = FindObjectsByType<CelestialPlatform>(FindObjectsSortMode.None);
        
        if (allPlatforms.Length == 0)
        {
            Debug.LogWarning("No CelestialPlatform components found in scene.");
            return;
        }
        
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
        
        Debug.Log($"Auto-assigned platforms: {innerTierPlatforms.Count} inner, {middleTierPlatforms.Count} middle, {outerTierPlatforms.Count} outer");
    }
    
    public void ClearAllTowers()
    {
        // Clear towers in editor mode
        if (!Application.isPlaying)
        {
            // Find all tower controllers and destroy their gameobjects
            TowerController[] towers = FindObjectsByType<TowerController>(FindObjectsSortMode.None);
            foreach (var tower in towers)
            {
                if (tower != null && tower.gameObject != null)
                {
                    DestroyImmediate(tower.gameObject);
                }
            }
            
            // Clear the dictionary
            platformTowerMap.Clear();
            
            Debug.Log("Cleared all towers from the scene.");
        }
        else
        {
            Debug.LogWarning("ClearAllTowers can only be used in edit mode.");
        }
    }
    
    public void PlaceTowersOnPlatforms()
    {
        if (!Application.isPlaying)
        {
            // Clear existing towers first
            ClearAllTowers();
            
            // Place towers on inner tier platforms
            PlaceTowersForTier(innerTierPlatforms, innerTierFillRatio, prefabSet.innerTierTowerPrefab);
            
            // Place towers on middle tier platforms
            PlaceTowersForTier(middleTierPlatforms, middleTierFillRatio, prefabSet.middleTierTowerPrefab);
            
            // Place towers on outer tier platforms
            PlaceTowersForTier(outerTierPlatforms, outerTierFillRatio, prefabSet.outerTierTowerPrefab);
            
            Debug.Log("Placed towers on platforms according to fill ratios.");
        }
        else
        {
            Debug.LogWarning("PlaceTowersOnPlatforms can only be used in edit mode.");
        }
    }
    
    private void PlaceTowersForTier(List<Transform> platforms, float fillRatio, GameObject towerPrefab)
    {
        if (towerPrefab == null)
        {
            Debug.LogWarning("Tower prefab is null. Cannot place towers for this tier.");
            return;
        }
        
        // Shuffle the platforms list to randomize which ones get towers
        List<Transform> shuffledPlatforms = new List<Transform>(platforms);
        ShuffleList(shuffledPlatforms);
        
        // Calculate how many platforms should have towers
        int towerCount = Mathf.RoundToInt(platforms.Count * fillRatio);
        
        // Place towers on the first 'towerCount' platforms in the shuffled list
        for (int i = 0; i < towerCount && i < shuffledPlatforms.Count; i++)
        {
            Transform platform = shuffledPlatforms[i];
            if (platform == null) continue;
            
            // Find tower attach point or use platform transform
            Transform attachPoint = FindAttachPoint(platform);
            Vector3 spawnPosition = attachPoint.position + towerOffset;
            
            // Instantiate the tower
            GameObject tower = Instantiate(towerPrefab, spawnPosition, Quaternion.identity);
            tower.name = $"Tower_{platform.name}";
            
            // Set the tower's parent to the platform
            tower.transform.SetParent(platform);
            
            // Rotate the tower to align with the platform's up direction
            tower.transform.up = platform.up;
            
            // Store in dictionary
            platformTowerMap[platform] = tower;
            
            // Set up tower controller references
            TowerController towerController = tower.GetComponent<TowerController>();
            if (towerController != null)
            {
                towerController._associatedPlatformTransform = platform;
                
                // Platform trigger setup removed as part of gravity zone system deprecation
                // Towers will need to use the new PlatformDualBubbleSystem for triggering
            }
        }
        
        // Place empty prefabs on remaining platforms if emptyPrefab is provided
        if (prefabSet.emptyPrefab != null)
        {
            for (int i = towerCount; i < shuffledPlatforms.Count; i++)
            {
                Transform platform = shuffledPlatforms[i];
                if (platform == null) continue;
                
                // Find tower attach point or use platform transform
                Transform attachPoint = FindAttachPoint(platform);
                Vector3 spawnPosition = attachPoint.position + towerOffset;
                
                // Instantiate the empty prefab
                GameObject emptyObject = Instantiate(prefabSet.emptyPrefab, spawnPosition, Quaternion.identity);
                emptyObject.name = $"Empty_{platform.name}";
                
                // Set the empty object's parent to the platform
                emptyObject.transform.SetParent(platform);
                
                // Rotate the empty object to align with the platform's up direction
                emptyObject.transform.up = platform.up;
                
                // Store in dictionary
                platformTowerMap[platform] = emptyObject;
            }
        }
    }
    
    private Transform FindAttachPoint(Transform platform)
    {
        // Try to find a child transform with the attach point name
        Transform attachPoint = platform.Find(towerAttachPointName);
        
        // If no specific attach point is found, use the platform transform itself
        return attachPoint != null ? attachPoint : platform;
    }
    
    private void ShuffleList<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
    
    // Public accessors for platform lists (for debugging and visualization)
    public List<Transform> InnerTierPlatforms => innerTierPlatforms;
    public List<Transform> MiddleTierPlatforms => middleTierPlatforms;
    public List<Transform> OuterTierPlatforms => outerTierPlatforms;
}
