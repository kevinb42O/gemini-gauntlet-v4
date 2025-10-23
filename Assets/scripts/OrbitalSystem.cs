using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

// NOTE: The two helper classes below are included here for simplicity.
// You could also move them to their own separate files if you prefer.

[System.Serializable]
public class EnemySpawnSettings
{
    public GameObject enemyPrefab;
    [Range(0f, 1f)]
    public float spawnChance = 0.25f;
}

[System.Serializable]
public class OrbitalRing
{
    [Tooltip("Exact radius for this ring of platforms")]
    public float radius = 50f;
    
    [Tooltip("Number of platforms to spawn on this ring")]
    public int platformCount = 8;
    
    [Tooltip("Starting angle offset for this ring (0-360)")]
    [Range(0f, 360f)]
    public float angleOffset = 0f;
    
    [Tooltip("Color to display this ring in the editor")]
    public Color gizmoColor = Color.cyan;
}

[System.Serializable]
public class OrbitalTierConfig
{
    public string tierName = "Orbital Tier";
    public GameObject platformPrefab;
    
    [Header("Ring Configuration")]
    [Tooltip("Define exact rings where platforms will spawn. Each ring has its own radius and platform count.")]
    public List<OrbitalRing> rings = new List<OrbitalRing>();
    
    [Header("Fixed Speed Per Ring")]
    [Tooltip("All platforms in this orbital ring will move at this exact speed for consistency")]
    public float fixedSpeed = 10f;
    [Range(0, 45)] public float orbitalPlaneVariance = 15f;

    [Header("Enemy Spawner Settings")]
    [Tooltip("Potential enemies that can spawn in this tier.")]
    public List<EnemySpawnSettings> enemySpawnTable = new List<EnemySpawnSettings>();
}

public class OrbitalSystem : MonoBehaviour
{
    [Header("System Setup")]
    [Tooltip("The central body that all objects in this system will orbit. If null, will default to this object's transform.")]
    public Transform centralBody;

    [Header("Spawning Position")]
    [Tooltip("The vertical offset from the platform's center to spawn enemies.")]
    public float spawnHeightOffset = 1.0f;

    [Header("Orbital Plane Control")]
    [Tooltip("Tilt angle of the entire orbital system. 0° = horizontal orbit, 90° = vertical orbit")]
    [Range(0f, 90f)]
    public float orbitalTiltAngle = 0f;
    
    [Tooltip("Direction of the tilt. X-axis tilt makes platforms orbit up/down, Z-axis tilt makes them orbit left/right")]
    public SystemLayoutConfig.TiltAxis tiltAxis = SystemLayoutConfig.TiltAxis.XAxis;

    [Header("Orbital Tiers")]
    [Tooltip("Configuration for each orbital tier. Platforms will be spawned according to these settings.")]
    public List<OrbitalTierConfig> orbitalTiers = new List<OrbitalTierConfig>();
    
    private List<CelestialPlatform> allPlatforms = new List<CelestialPlatform>();
    private static int globalPlatformId = 0; // Global counter for unique platform IDs

    void Start()
    {
        // If no central body is assigned, the system will orbit around itself.
        if (centralBody == null)
        {            
            centralBody = this.transform;
        }

        SpawnInitialPlatforms();
    }

    void SpawnInitialPlatforms()
    {
        if (orbitalTiers == null || orbitalTiers.Count == 0)
        {
            Debug.LogError("OrbitalSystem: No Orbital Tiers defined. No platforms will be spawned.", this);
            return;
        }

        foreach (var tier in orbitalTiers)
        {
            if (tier.platformPrefab == null)
            {
                Debug.LogWarning($"OrbitalSystem: Tier '{tier.tierName}' is missing a Platform Prefab. Skipping this tier.", this);
                continue;
            }

            SpawnPlatformsForTier(tier);
        }
    }
    
    void SpawnPlatformsForTier(OrbitalTierConfig tier)
    {
        if (tier.rings == null || tier.rings.Count == 0)
        {
            Debug.LogWarning($"OrbitalSystem: Tier '{tier.tierName}' has no rings defined. Skipping.", this);
            return;
        }

        // Spawn platforms for each defined ring
        foreach (var ring in tier.rings)
        {
            if (ring.platformCount <= 0)
            {
                Debug.LogWarning($"OrbitalSystem: Ring at radius {ring.radius} has platformCount <= 0. Skipping.", this);
                continue;
            }

            // Calculate even angular distribution for this ring
            float angleStep = 360f / ring.platformCount;
            
            for (int i = 0; i < ring.platformCount; i++)
            {
                // Calculate evenly spaced angle with the ring's offset
                float angle = (i * angleStep) + ring.angleOffset;
                
                SpawnPlatformAtPosition(tier, ring.radius, angle);
            }
        }
    }
    
    void SpawnPlatformAtPosition(OrbitalTierConfig tier, float radius, float angle)
    {
        // Instantiate under this component's transform to keep the scene hierarchy clean.
        GameObject platformGO = Instantiate(tier.platformPrefab, Vector3.zero, Quaternion.identity, this.transform);
        
        // CRITICAL FIX: Assign unique name to prevent ChestManager tracking conflicts
        globalPlatformId++;
        string uniquePlatformName = $"Platform_{globalPlatformId:D3}_{tier.tierName.Replace(" ", "")}";
        platformGO.name = uniquePlatformName;
        
        Debug.Log($"<color=green>OrbitalSystem: Spawned platform with unique name '{uniquePlatformName}'</color>");
        
        CelestialPlatform celestialPlatform = platformGO.GetComponent<CelestialPlatform>();

        if (celestialPlatform != null)
        {
            // Calculate base orbital plane with tier variance
            Vector3 baseOrbitalPlane = Quaternion.Euler(
                Random.Range(-tier.orbitalPlaneVariance, tier.orbitalPlaneVariance),
                0f, // Don't use angle here - we'll pass it directly to Initialize
                Random.Range(-tier.orbitalPlaneVariance, tier.orbitalPlaneVariance)
            ) * Vector3.up;

            // Apply system-wide orbital tilt
            Vector3 finalOrbitalPlane = ApplyOrbitalTilt(baseOrbitalPlane);

            // Initialize with the configured central body, tier-specific speed, and EXACT angle
            celestialPlatform.Initialize(centralBody, radius, finalOrbitalPlane.normalized, tier.fixedSpeed, angle);
            allPlatforms.Add(celestialPlatform);

            TrySpawnEnemyOnPlatform(celestialPlatform.transform, tier);
        }
        else
        {
            Debug.LogError($"OrbitalSystem: Prefab '{tier.platformPrefab.name}' is MISSING the CelestialPlatform.cs script! Destroying instance.", platformGO);
            Destroy(platformGO);
        }
    }

    /// <summary>
    /// Applies the system-wide orbital tilt to the given orbital plane vector
    /// </summary>
    private Vector3 ApplyOrbitalTilt(Vector3 originalPlane)
    {
        if (Mathf.Approximately(orbitalTiltAngle, 0f))
        {
            return originalPlane; // No tilt, return original
        }

        // Create tilt rotation based on selected axis
        Quaternion tiltRotation;
        switch (tiltAxis)
        {
            case SystemLayoutConfig.TiltAxis.XAxis:
                // Tilt around X-axis (makes platforms orbit up/down)
                tiltRotation = Quaternion.AngleAxis(orbitalTiltAngle, Vector3.right);
                break;
            case SystemLayoutConfig.TiltAxis.ZAxis:
                // Tilt around Z-axis (makes platforms orbit left/right)
                tiltRotation = Quaternion.AngleAxis(orbitalTiltAngle, Vector3.forward);
                break;
            default:
                tiltRotation = Quaternion.identity;
                break;
        }

        // Apply the tilt to the orbital plane
        return tiltRotation * originalPlane;
    }

    void TrySpawnEnemyOnPlatform(Transform platformTransform, OrbitalTierConfig tier)
    {
        if (tier.enemySpawnTable == null || tier.enemySpawnTable.Count == 0) return;

        foreach (var enemySetting in tier.enemySpawnTable)
        {
            if (enemySetting.enemyPrefab == null)
            {
                Debug.LogWarning("An enemy spawn setting is missing its prefab. Skipping.");
                continue;
            }

            if (Random.value <= enemySetting.spawnChance)
            {
                Vector3 spawnPosition = platformTransform.position + (Vector3.up * spawnHeightOffset);
                Instantiate(enemySetting.enemyPrefab, spawnPosition, Quaternion.identity, platformTransform);
                break; 
            }
        }
    }

    // ============================================
    // GIZMO VISUALIZATION
    // ============================================
    void OnDrawGizmos()
    {
        DrawRingGizmos(false);
    }

    void OnDrawGizmosSelected()
    {
        DrawRingGizmos(true);
    }

    void DrawRingGizmos(bool selected)
    {
        if (orbitalTiers == null || orbitalTiers.Count == 0) return;

        Transform center = centralBody != null ? centralBody : transform;
        Vector3 centerPos = center.position;

        foreach (var tier in orbitalTiers)
        {
            if (tier.rings == null || tier.rings.Count == 0) continue;

            foreach (var ring in tier.rings)
            {
                // Set color with transparency based on selection
                Color ringColor = ring.gizmoColor;
                ringColor.a = selected ? 0.8f : 0.3f;
                Gizmos.color = ringColor;

                // Draw the ring circle
                DrawCircle(centerPos, ring.radius, 64);

                // Draw platform position markers
                if (ring.platformCount > 0)
                {
                    float angleStep = 360f / ring.platformCount;
                    for (int i = 0; i < ring.platformCount; i++)
                    {
                        float angle = (i * angleStep) + ring.angleOffset;
                        float rad = angle * Mathf.Deg2Rad;
                        
                        Vector3 platformPos = centerPos + new Vector3(
                            Mathf.Cos(rad) * ring.radius,
                            0f,
                            Mathf.Sin(rad) * ring.radius
                        );

                        // Draw sphere at platform position
                        Gizmos.DrawSphere(platformPos, selected ? 2f : 1f);
                        
                        // Draw line from center to platform
                        if (selected)
                        {
                            Gizmos.DrawLine(centerPos, platformPos);
                        }
                    }
                }

                // Draw radius label in selected mode
                if (selected)
                {
                    Vector3 labelPos = centerPos + Vector3.right * ring.radius;
                    #if UNITY_EDITOR
                    Handles.Label(labelPos, $"{ring.radius:F1}u\n{ring.platformCount} platforms");
                    #endif
                }
            }
        }
    }

    void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius
            );
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
}
