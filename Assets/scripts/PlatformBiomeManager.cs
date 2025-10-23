using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Platform Biome Manager for Gemini Gauntlet
/// Creates different biomes and special platform types based on world position
/// Adds environmental variety to the procedural platform generation
/// </summary>
public class PlatformBiomeManager : MonoBehaviour
{
    [Header("Biome Configuration")]
    [SerializeField] private BiomeData[] availableBiomes;
    [SerializeField] private float biomeSize = 50000f; // Size of each biome area
    [SerializeField] private bool enableBiomeTransitions = true;
    [SerializeField] private float transitionZoneSize = 10000f;
    
    [Header("Special Platforms")]
    [SerializeField] private SpecialPlatformData[] specialPlatforms;
    [SerializeField] private float specialPlatformChance = 0.1f; // 10% chance
    [SerializeField] private bool enableSpecialPlatforms = true;
    
    [Header("Environmental Effects")]
    [SerializeField] private bool enableWeatherEffects = true;
    [SerializeField] private bool enableAmbientSounds = true;
    [SerializeField] private bool enableBiomeParticles = true;
    
    [Header("Debug")]
    [SerializeField] private bool showBiomeDebug = true;
    [SerializeField] private bool showBiomeGizmos = true;
    
    // Biome system
    private Dictionary<Vector2Int, BiomeType> biomeCache = new Dictionary<Vector2Int, BiomeType>();
    private System.Random biomeRandom;
    private int biomeSeed = 54321;
    
    // Special platform tracking
    private Dictionary<Vector2Int, SpecialPlatformType> specialPlatformCache = new Dictionary<Vector2Int, SpecialPlatformType>();
    
    public static PlatformBiomeManager Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeBiomeSystem();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void InitializeBiomeSystem()
    {
        biomeRandom = new System.Random(biomeSeed);
        
        // Create default biomes if none assigned
        if (availableBiomes == null || availableBiomes.Length == 0)
        {
            CreateDefaultBiomes();
        }
        
        // Create default special platforms if none assigned
        if (specialPlatforms == null || specialPlatforms.Length == 0)
        {
            CreateDefaultSpecialPlatforms();
        }
        
        Debug.Log($"[PlatformBiomeManager] Initialized with {availableBiomes.Length} biomes and {specialPlatforms.Length} special platforms");
    }
    
    void CreateDefaultBiomes()
    {
        availableBiomes = new BiomeData[]
        {
            new BiomeData
            {
                biomeType = BiomeType.Crystal,
                name = "Crystal Fields",
                primaryColor = Color.cyan,
                secondaryColor = Color.blue,
                ambientLightColor = Color.cyan,
                fogColor = Color.blue,
                particleColor = Color.white,
                temperatureRange = new Vector2(-10f, 10f)
            },
            new BiomeData
            {
                biomeType = BiomeType.Volcanic,
                name = "Volcanic Wastes",
                primaryColor = Color.red,
                secondaryColor = Color.yellow,
                ambientLightColor = Color.red,
                fogColor = Color.red,
                particleColor = Color.orange,
                temperatureRange = new Vector2(30f, 60f)
            },
            new BiomeData
            {
                biomeType = BiomeType.Forest,
                name = "Floating Forest",
                primaryColor = Color.green,
                secondaryColor = Color.yellow,
                ambientLightColor = Color.green,
                fogColor = Color.green,
                particleColor = Color.green,
                temperatureRange = new Vector2(15f, 25f)
            },
            new BiomeData
            {
                biomeType = BiomeType.Desert,
                name = "Sky Desert",
                primaryColor = Color.yellow,
                secondaryColor = Color.orange,
                ambientLightColor = Color.yellow,
                fogColor = Color.yellow,
                particleColor = Color.yellow,
                temperatureRange = new Vector2(25f, 45f)
            },
            new BiomeData
            {
                biomeType = BiomeType.Ice,
                name = "Frozen Peaks",
                primaryColor = Color.white,
                secondaryColor = Color.cyan,
                ambientLightColor = Color.white,
                fogColor = Color.white,
                particleColor = Color.white,
                temperatureRange = new Vector2(-30f, -10f)
            },
            new BiomeData
            {
                biomeType = BiomeType.Void,
                name = "Void Realm",
                primaryColor = Color.black,
                secondaryColor = Color.magenta,
                ambientLightColor = Color.magenta,
                fogColor = Color.black,
                particleColor = Color.magenta,
                temperatureRange = new Vector2(-50f, 50f)
            }
        };
    }
    
    void CreateDefaultSpecialPlatforms()
    {
        specialPlatforms = new SpecialPlatformData[]
        {
            new SpecialPlatformData
            {
                platformType = SpecialPlatformType.Treasure,
                name = "Treasure Platform",
                scaleFactor = 1.2f,
                glowColor = Color.gold,
                particleColor = Color.yellow,
                spawnChance = 0.05f,
                description = "Contains valuable loot"
            },
            new SpecialPlatformData
            {
                platformType = SpecialPlatformType.Danger,
                name = "Danger Platform",
                scaleFactor = 0.8f,
                glowColor = Color.red,
                particleColor = Color.red,
                spawnChance = 0.08f,
                description = "Hazardous but rewarding"
            },
            new SpecialPlatformData
            {
                platformType = SpecialPlatformType.Healing,
                name = "Healing Platform",
                scaleFactor = 1.0f,
                glowColor = Color.green,
                particleColor = Color.green,
                spawnChance = 0.03f,
                description = "Restores player health"
            },
            new SpecialPlatformData
            {
                platformType = SpecialPlatformType.Teleporter,
                name = "Teleporter Platform",
                scaleFactor = 1.5f,
                glowColor = Color.magenta,
                particleColor = Color.magenta,
                spawnChance = 0.02f,
                description = "Teleports to distant platforms"
            },
            new SpecialPlatformData
            {
                platformType = SpecialPlatformType.Boss,
                name = "Boss Platform",
                scaleFactor = 2.0f,
                glowColor = Color.black,
                particleColor = Color.red,
                spawnChance = 0.01f,
                description = "Boss encounter area"
            }
        };
    }
    
    public BiomeType GetBiomeAtPosition(Vector3 worldPosition)
    {
        Vector2Int biomeGrid = WorldToBiomeGrid(worldPosition);
        
        if (biomeCache.TryGetValue(biomeGrid, out BiomeType cachedBiome))
        {
            return cachedBiome;
        }
        
        // Generate new biome for this grid position
        BiomeType newBiome = GenerateBiomeForGrid(biomeGrid);
        biomeCache[biomeGrid] = newBiome;
        
        return newBiome;
    }
    
    public BiomeData GetBiomeData(BiomeType biomeType)
    {
        foreach (var biome in availableBiomes)
        {
            if (biome.biomeType == biomeType)
                return biome;
        }
        
        return availableBiomes[0]; // Fallback to first biome
    }
    
    public SpecialPlatformType GetSpecialPlatformType(Vector2Int gridPosition)
    {
        if (!enableSpecialPlatforms)
            return SpecialPlatformType.None;
        
        if (specialPlatformCache.TryGetValue(gridPosition, out SpecialPlatformType cachedType))
        {
            return cachedType;
        }
        
        // Generate special platform type for this position
        SpecialPlatformType newType = GenerateSpecialPlatformType(gridPosition);
        specialPlatformCache[gridPosition] = newType;
        
        return newType;
    }
    
    public SpecialPlatformData GetSpecialPlatformData(SpecialPlatformType platformType)
    {
        foreach (var platform in specialPlatforms)
        {
            if (platform.platformType == platformType)
                return platform;
        }
        
        return new SpecialPlatformData(); // Return empty data
    }
    
    Vector2Int WorldToBiomeGrid(Vector3 worldPosition)
    {
        int gridX = Mathf.FloorToInt(worldPosition.x / biomeSize);
        int gridZ = Mathf.FloorToInt(worldPosition.z / biomeSize);
        return new Vector2Int(gridX, gridZ);
    }
    
    BiomeType GenerateBiomeForGrid(Vector2Int biomeGrid)
    {
        // Use grid position as seed for consistent biome generation
        int seed = biomeGrid.x * 1000 + biomeGrid.y;
        System.Random gridRandom = new System.Random(seed + biomeSeed);
        
        // Simple biome selection based on distance from origin
        float distanceFromOrigin = Vector2.Distance(Vector2.zero, biomeGrid);
        
        // Closer to origin = more common biomes, further = rarer biomes
        if (distanceFromOrigin < 2f)
        {
            // Starting area - friendly biomes
            BiomeType[] startingBiomes = { BiomeType.Forest, BiomeType.Crystal };
            return startingBiomes[gridRandom.Next(startingBiomes.Length)];
        }
        else if (distanceFromOrigin < 5f)
        {
            // Mid-range - mixed biomes
            BiomeType[] midBiomes = { BiomeType.Forest, BiomeType.Crystal, BiomeType.Desert, BiomeType.Ice };
            return midBiomes[gridRandom.Next(midBiomes.Length)];
        }
        else if (distanceFromOrigin < 10f)
        {
            // Far range - challenging biomes
            BiomeType[] farBiomes = { BiomeType.Volcanic, BiomeType.Ice, BiomeType.Desert };
            return farBiomes[gridRandom.Next(farBiomes.Length)];
        }
        else
        {
            // Very far - rare and dangerous biomes
            BiomeType[] rareBiomes = { BiomeType.Void, BiomeType.Volcanic };
            return rareBiomes[gridRandom.Next(rareBiomes.Length)];
        }
    }
    
    SpecialPlatformType GenerateSpecialPlatformType(Vector2Int gridPosition)
    {
        // Use grid position as seed
        int seed = gridPosition.x * 2000 + gridPosition.y * 3000;
        System.Random gridRandom = new System.Random(seed + biomeSeed);
        
        float roll = (float)gridRandom.NextDouble();
        
        if (roll > specialPlatformChance)
            return SpecialPlatformType.None;
        
        // Select special platform type based on weighted chances
        float totalWeight = 0f;
        foreach (var platform in specialPlatforms)
        {
            totalWeight += platform.spawnChance;
        }
        
        float weightedRoll = (float)gridRandom.NextDouble() * totalWeight;
        float currentWeight = 0f;
        
        foreach (var platform in specialPlatforms)
        {
            currentWeight += platform.spawnChance;
            if (weightedRoll <= currentWeight)
            {
                return platform.platformType;
            }
        }
        
        return SpecialPlatformType.None;
    }
    
    public void ApplyBiomeEffectsToPlatform(GameObject platform, Vector3 worldPosition)
    {
        BiomeType biome = GetBiomeAtPosition(worldPosition);
        BiomeData biomeData = GetBiomeData(biome);
        
        // Apply biome-specific effects
        EnhancedPlatformController controller = platform.GetComponent<EnhancedPlatformController>();
        if (controller != null)
        {
            controller.SetGlowColor(biomeData.primaryColor);
        }
        
        // Apply biome material if available
        Renderer renderer = platform.GetComponent<Renderer>();
        if (renderer != null && biomeData.biomeMaterial != null)
        {
            renderer.material = biomeData.biomeMaterial;
        }
        
        // Add biome-specific components
        AddBiomeSpecificComponents(platform, biome, biomeData);
    }
    
    public void ApplySpecialPlatformEffects(GameObject platform, Vector2Int gridPosition)
    {
        SpecialPlatformType specialType = GetSpecialPlatformType(gridPosition);
        if (specialType == SpecialPlatformType.None) return;
        
        SpecialPlatformData specialData = GetSpecialPlatformData(specialType);
        
        // Apply special platform effects
        EnhancedPlatformController controller = platform.GetComponent<EnhancedPlatformController>();
        if (controller != null)
        {
            controller.SetGlowColor(specialData.glowColor);
        }
        
        // Scale platform
        platform.transform.localScale *= specialData.scaleFactor;
        
        // Add special platform behavior
        AddSpecialPlatformBehavior(platform, specialType, specialData);
        
        if (showBiomeDebug)
        {
            Debug.Log($"[PlatformBiomeManager] Applied special platform: {specialData.name} at {gridPosition}");
        }
    }
    
    void AddBiomeSpecificComponents(GameObject platform, BiomeType biome, BiomeData biomeData)
    {
        switch (biome)
        {
            case BiomeType.Volcanic:
                AddVolcanicEffects(platform, biomeData);
                break;
            case BiomeType.Ice:
                AddIceEffects(platform, biomeData);
                break;
            case BiomeType.Crystal:
                AddCrystalEffects(platform, biomeData);
                break;
            case BiomeType.Void:
                AddVoidEffects(platform, biomeData);
                break;
        }
    }
    
    void AddVolcanicEffects(GameObject platform, BiomeData biomeData)
    {
        // Add heat damage component
        VolcanicPlatform volcanic = platform.GetComponent<VolcanicPlatform>();
        if (volcanic == null)
            volcanic = platform.AddComponent<VolcanicPlatform>();
        
        volcanic.heatDamage = 5f;
        volcanic.damageInterval = 2f;
    }
    
    void AddIceEffects(GameObject platform, BiomeData biomeData)
    {
        // Add slippery surface
        IcePlatform ice = platform.GetComponent<IcePlatform>();
        if (ice == null)
            ice = platform.AddComponent<IcePlatform>();
        
        ice.slipperiness = 0.8f;
        ice.freezeChance = 0.1f;
    }
    
    void AddCrystalEffects(GameObject platform, BiomeData biomeData)
    {
        // Add mana regeneration
        CrystalPlatform crystal = platform.GetComponent<CrystalPlatform>();
        if (crystal == null)
            crystal = platform.AddComponent<CrystalPlatform>();
        
        crystal.manaRegenRate = 2f;
        crystal.crystalGrowthRate = 1f;
    }
    
    void AddVoidEffects(GameObject platform, BiomeData biomeData)
    {
        // Add void corruption
        VoidPlatform voidPlatform = platform.GetComponent<VoidPlatform>();
        if (voidPlatform == null)
            voidPlatform = platform.AddComponent<VoidPlatform>();
        
        voidPlatform.corruptionRate = 0.5f;
        voidPlatform.voidDamage = 10f;
    }
    
    void AddSpecialPlatformBehavior(GameObject platform, SpecialPlatformType specialType, SpecialPlatformData specialData)
    {
        switch (specialType)
        {
            case SpecialPlatformType.Treasure:
                TreasurePlatform treasure = platform.AddComponent<TreasurePlatform>();
                treasure.lootValue = 100;
                break;
                
            case SpecialPlatformType.Healing:
                HealingPlatform healing = platform.AddComponent<HealingPlatform>();
                healing.healingRate = 10f;
                break;
                
            case SpecialPlatformType.Teleporter:
                TeleporterPlatform teleporter = platform.AddComponent<TeleporterPlatform>();
                teleporter.teleportRange = 50000f;
                break;
                
            case SpecialPlatformType.Boss:
                BossPlatform boss = platform.AddComponent<BossPlatform>();
                boss.bossLevel = 5;
                break;
        }
    }
    
    // Public utility methods
    public string GetBiomeNameAtPosition(Vector3 worldPosition)
    {
        BiomeType biome = GetBiomeAtPosition(worldPosition);
        BiomeData biomeData = GetBiomeData(biome);
        return biomeData.name;
    }
    
    public Color GetBiomeColorAtPosition(Vector3 worldPosition)
    {
        BiomeType biome = GetBiomeAtPosition(worldPosition);
        BiomeData biomeData = GetBiomeData(biome);
        return biomeData.primaryColor;
    }
    
    void OnDrawGizmos()
    {
        if (!showBiomeGizmos) return;
        
        // Draw biome boundaries around camera/player
        Camera cam = Camera.main;
        if (cam == null) return;
        
        Vector3 cameraPos = cam.transform.position;
        Vector2Int centerBiome = WorldToBiomeGrid(cameraPos);
        
        for (int x = -2; x <= 2; x++)
        {
            for (int z = -2; z <= 2; z++)
            {
                Vector2Int biomeGrid = centerBiome + new Vector2Int(x, z);
                BiomeType biome = GetBiomeAtPosition(new Vector3(biomeGrid.x * biomeSize, 0, biomeGrid.y * biomeSize));
                BiomeData biomeData = GetBiomeData(biome);
                
                Gizmos.color = biomeData.primaryColor;
                Vector3 biomeCenter = new Vector3(biomeGrid.x * biomeSize + biomeSize * 0.5f, 0, biomeGrid.y * biomeSize + biomeSize * 0.5f);
                Gizmos.DrawWireCube(biomeCenter, Vector3.one * biomeSize);
            }
        }
    }
}

// Data structures
[System.Serializable]
public class BiomeData
{
    public BiomeType biomeType;
    public string name;
    public Color primaryColor;
    public Color secondaryColor;
    public Color ambientLightColor;
    public Color fogColor;
    public Color particleColor;
    public Material biomeMaterial;
    public Vector2 temperatureRange;
    public AudioClip ambientSound;
    public GameObject[] decorativeObjects;
}

[System.Serializable]
public class SpecialPlatformData
{
    public SpecialPlatformType platformType;
    public string name;
    public string description;
    public float scaleFactor = 1f;
    public Color glowColor = Color.white;
    public Color particleColor = Color.white;
    public float spawnChance = 0.05f;
    public GameObject specialPrefab;
    public AudioClip specialSound;
}

public enum BiomeType
{
    Forest,
    Desert,
    Ice,
    Volcanic,
    Crystal,
    Void
}

public enum SpecialPlatformType
{
    None,
    Treasure,
    Danger,
    Healing,
    Teleporter,
    Boss
}

// Special platform behavior components (basic implementations)
public class VolcanicPlatform : MonoBehaviour
{
    public float heatDamage = 5f;
    public float damageInterval = 2f;
    // Implementation would go here
}

public class IcePlatform : MonoBehaviour
{
    public float slipperiness = 0.8f;
    public float freezeChance = 0.1f;
    // Implementation would go here
}

public class CrystalPlatform : MonoBehaviour
{
    public float manaRegenRate = 2f;
    public float crystalGrowthRate = 1f;
    // Implementation would go here
}

public class VoidPlatform : MonoBehaviour
{
    public float corruptionRate = 0.5f;
    public float voidDamage = 10f;
    // Implementation would go here
}

public class TreasurePlatform : MonoBehaviour
{
    public int lootValue = 100;
    // Implementation would go here
}

public class HealingPlatform : MonoBehaviour
{
    public float healingRate = 10f;
    // Implementation would go here
}

public class TeleporterPlatform : MonoBehaviour
{
    public float teleportRange = 50000f;
    // Implementation would go here
}

public class BossPlatform : MonoBehaviour
{
    public int bossLevel = 5;
    // Implementation would go here
}
