using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Static platform generator for tiered vertical gameplay.
/// Creates fixed platforms at different height levels for the player to traverse.
/// NO MOVEMENT - platforms are static and placed in the editor.
/// Based on UniverseGenerator but adapted for static gameplay.
/// 
/// USAGE:
/// 1. Add this component to an empty GameObject in your scene
/// 2. Configure your tiers (Normal, Ice, Fire platforms)
/// 3. Click "Generate Platforms" in the Inspector (requires Editor script)
/// 4. Platforms spawn in editor - manually place elevators between them
/// 5. Play your game!
/// </summary>
public class StaticTierPlatformGenerator : MonoBehaviour
{
    [Header("🎮 TIER SYSTEM")]
    [Tooltip("Vertical tiers of platforms (Level 1 = Normal, Level 2 = Ice, Level 3 = Fire, etc.)")]
    public List<StaticTierConfig> tiers = new List<StaticTierConfig>();
    
    [Header("📏 SPACING CONFIGURATION")]
    [Tooltip("How far apart platforms are spread horizontally within a tier")]
    public float platformSpacing = 150f;
    
    [Tooltip("Vertical distance between each tier (height difference)")]
    public float tierHeightOffset = 300f;
    
    [Tooltip("Starting Y position for the first tier")]
    public float startingHeight = 0f;
    
    [Header("📐 LAYOUT PATTERN")]
    [Tooltip("How platforms are arranged in each tier")]
    public LayoutPattern layoutPattern = LayoutPattern.Circle;
    
    [Header("🔧 DEBUG")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private Color[] tierGizmoColors = new Color[] 
    { 
        Color.cyan,     // Tier 1 (Normal/Space)
        Color.blue,     // Tier 2 (Ice)
        Color.red,      // Tier 3 (Fire)
        Color.green,    // Tier 4
        Color.yellow    // Tier 5
    };
    
    public enum LayoutPattern
    {
        Circle,         // Platforms arranged in a circle
        Grid,           // Platforms in a grid pattern
        Line,           // Platforms in a straight line
        Random          // Random placement within bounds
    }
    
    /// <summary>
    /// Generate all platforms in the editor (called by Editor script or manually)
    /// </summary>
    public void GeneratePlatforms()
    {
        if (tiers == null || tiers.Count == 0)
        {
            Debug.LogWarning("StaticTierPlatformGenerator: No tiers configured. Add at least one tier!", this);
            return;
        }
        
        // Clear existing generated platforms (only our children)
        ClearGeneratedPlatforms();
        
        // Generate each tier
        for (int tierIndex = 0; tierIndex < tiers.Count; tierIndex++)
        {
            StaticTierConfig tier = tiers[tierIndex];
            
            if (tier.platformPrefab == null)
            {
                Debug.LogWarning($"StaticTierPlatformGenerator: Tier {tierIndex + 1} ('{tier.tierName}') has no platform prefab assigned. Skipping.", this);
                continue;
            }
            
            // Calculate tier height
            float tierHeight = startingHeight + (tierIndex * tierHeightOffset);
            
            // Create parent object for this tier
            GameObject tierParent = new GameObject($"Tier_{tierIndex + 1}_{tier.tierName}");
            tierParent.transform.SetParent(this.transform);
            tierParent.transform.position = new Vector3(0, tierHeight, 0);
            
            // Generate platforms for this tier
            GeneratePlatformsForTier(tier, tierParent.transform, tierHeight, tierIndex);
            
            Debug.Log($"<color=cyan>✅ Generated Tier {tierIndex + 1}:</color> '{tier.tierName}' with {tier.platformCount} platforms at height {tierHeight}", tierParent);
        }
        
        Debug.Log($"<color=lime>🎉 GENERATION COMPLETE!</color> Created {tiers.Count} tiers. Now manually place elevators between platforms!", this);
    }
    
    /// <summary>
    /// Generate platforms for a single tier based on layout pattern
    /// </summary>
    private void GeneratePlatformsForTier(StaticTierConfig tier, Transform parent, float height, int tierIndex)
    {
        Vector3[] positions = CalculatePlatformPositions(tier.platformCount, platformSpacing, layoutPattern);
        
        for (int i = 0; i < positions.Length; i++)
        {
            // Calculate world position
            Vector3 worldPosition = parent.position + positions[i];
            worldPosition.y = height; // Ensure exact height
            
            // Instantiate platform
            GameObject platform = Instantiate(tier.platformPrefab, worldPosition, Quaternion.identity, parent);
            platform.name = $"Platform_{i + 1}_{tier.tierName}";
            
            // Make it completely static (no movement components)
            RemoveMovementComponents(platform);
            
            // Mark as static for Unity batching optimization
            platform.isStatic = true;
            
            // Add tier identifier (useful for gameplay logic)
            TierIdentifier identifier = platform.GetComponent<TierIdentifier>();
            if (identifier == null)
            {
                identifier = platform.AddComponent<TierIdentifier>();
            }
            identifier.tierIndex = tierIndex;
            identifier.tierName = tier.tierName;
            identifier.platformIndex = i;
        }
    }
    
    /// <summary>
    /// Calculate platform positions based on layout pattern
    /// </summary>
    private Vector3[] CalculatePlatformPositions(int count, float spacing, LayoutPattern pattern)
    {
        Vector3[] positions = new Vector3[count];
        
        switch (pattern)
        {
            case LayoutPattern.Circle:
                // Arrange platforms in a circle
                float radius = spacing;
                for (int i = 0; i < count; i++)
                {
                    float angle = (360f / count) * i * Mathf.Deg2Rad;
                    positions[i] = new Vector3(
                        Mathf.Cos(angle) * radius,
                        0,
                        Mathf.Sin(angle) * radius
                    );
                }
                break;
                
            case LayoutPattern.Grid:
                // Arrange platforms in a grid
                int gridSize = Mathf.CeilToInt(Mathf.Sqrt(count));
                int index = 0;
                for (int x = 0; x < gridSize && index < count; x++)
                {
                    for (int z = 0; z < gridSize && index < count; z++)
                    {
                        positions[index] = new Vector3(
                            (x - gridSize / 2f) * spacing,
                            0,
                            (z - gridSize / 2f) * spacing
                        );
                        index++;
                    }
                }
                break;
                
            case LayoutPattern.Line:
                // Arrange platforms in a straight line
                for (int i = 0; i < count; i++)
                {
                    positions[i] = new Vector3(
                        (i - count / 2f) * spacing,
                        0,
                        0
                    );
                }
                break;
                
            case LayoutPattern.Random:
                // Random placement within bounds
                float boundSize = spacing * 2f;
                for (int i = 0; i < count; i++)
                {
                    positions[i] = new Vector3(
                        Random.Range(-boundSize, boundSize),
                        0,
                        Random.Range(-boundSize, boundSize)
                    );
                }
                break;
        }
        
        return positions;
    }
    
    /// <summary>
    /// Remove any movement components to ensure platforms are completely static
    /// </summary>
    private void RemoveMovementComponents(GameObject platform)
    {
        // Remove orbital movement components
        CelestialPlatform celestial = platform.GetComponent<CelestialPlatform>();
        if (celestial != null)
        {
            DestroyImmediate(celestial);
        }
        
        // Remove rigidbody if present (we want purely static platforms)
        Rigidbody rb = platform.GetComponent<Rigidbody>();
        if (rb != null)
        {
            DestroyImmediate(rb);
        }
        
        // Ensure collider is present (required for player landing)
        if (platform.GetComponent<Collider>() == null)
        {
            BoxCollider collider = platform.AddComponent<BoxCollider>();
            Debug.LogWarning($"Platform '{platform.name}' had no collider. Added BoxCollider automatically.", platform);
        }
    }
    
    /// <summary>
    /// Clear previously generated platforms (only removes children of this generator)
    /// </summary>
    public void ClearGeneratedPlatforms()
    {
        // Destroy all child objects (our generated platforms)
        int childCount = transform.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
        
        Debug.Log($"<color=yellow>🗑️ Cleared {childCount} previously generated tiers.</color>", this);
    }
    
    /// <summary>
    /// Draw debug gizmos in scene view
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos || tiers == null || tiers.Count == 0) return;
        
        for (int tierIndex = 0; tierIndex < tiers.Count; tierIndex++)
        {
            StaticTierConfig tier = tiers[tierIndex];
            float tierHeight = startingHeight + (tierIndex * tierHeightOffset);
            
            // Choose color for this tier
            Color tierColor = tierIndex < tierGizmoColors.Length ? tierGizmoColors[tierIndex] : Color.white;
            Gizmos.color = tierColor;
            
            // Calculate platform positions
            Vector3[] positions = CalculatePlatformPositions(tier.platformCount, platformSpacing, layoutPattern);
            
            // Draw each platform position
            for (int i = 0; i < positions.Length; i++)
            {
                Vector3 worldPos = transform.position + positions[i];
                worldPos.y = tierHeight;
                
                // Draw sphere at platform position
                Gizmos.DrawWireSphere(worldPos, 20f);
                
                // Draw connecting lines in circle pattern
                if (layoutPattern == LayoutPattern.Circle && i < positions.Length - 1)
                {
                    Vector3 nextPos = transform.position + positions[i + 1];
                    nextPos.y = tierHeight;
                    Gizmos.DrawLine(worldPos, nextPos);
                }
                
                // Connect last to first in circle
                if (layoutPattern == LayoutPattern.Circle && i == positions.Length - 1)
                {
                    Vector3 firstPos = transform.position + positions[0];
                    firstPos.y = tierHeight;
                    Gizmos.DrawLine(worldPos, firstPos);
                }
            }
            
            // Draw tier label in scene view
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * tierHeight + Vector3.forward * 50f,
                $"Tier {tierIndex + 1}: {tier.tierName} ({tier.platformCount} platforms)",
                new GUIStyle() { normal = new GUIStyleState() { textColor = tierColor } }
            );
            #endif
        }
    }
}

/// <summary>
/// Configuration for a single tier of static platforms
/// </summary>
[System.Serializable]
public class StaticTierConfig
{
    [Header("🎯 TIER SETTINGS")]
    public string tierName = "Normal Platforms";
    
    [Tooltip("The platform prefab to spawn for this tier")]
    public GameObject platformPrefab;
    
    [Tooltip("How many platforms to spawn in this tier")]
    [Range(1, 20)]
    public int platformCount = 5;
    
    [Header("🎨 VISUAL THEME (Optional)")]
    [Tooltip("Theme identifier (e.g., 'Space', 'Ice', 'Fire') - used for visual reference")]
    public string themeTag = "Space";
}

/// <summary>
/// Component added to each generated platform to identify its tier
/// Useful for gameplay logic (e.g., applying ice physics on ice tier platforms)
/// </summary>
public class TierIdentifier : MonoBehaviour
{
    public int tierIndex;
    public string tierName;
    public int platformIndex;
    
    // Optional: Add gameplay properties based on tier
    public bool isIcePlatform => tierName.ToLower().Contains("ice");
    public bool isFirePlatform => tierName.ToLower().Contains("fire");
    public bool isNormalPlatform => !isIcePlatform && !isFirePlatform;
}
