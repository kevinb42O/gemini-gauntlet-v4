// ============================================================================
// AAA WALLHACK SYSTEM - EngineOwning Style
// Ultra-performant wallhack system with dynamic enemy tracking
// Features: Color-coded enemies, distance indicators, health bars, glow effects
// 
// Senior AAA Quality - Optimized for maximum performance
// Uses object pooling, batching, and smart LOD system
// ============================================================================

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Main wallhack system controller - manages all wallhack rendering and effects
/// Attach this to your Player or Camera
/// </summary>
public class AAAWallhackSystem : MonoBehaviour
{
    #region Singleton
    public static AAAWallhackSystem Instance { get; private set; }
    #endregion
    
    #region Configuration
    [Header("=== WALLHACK TOGGLE ===")]
    [Tooltip("Enable/Disable wallhack globally (can be toggled at runtime)")]
    public bool wallhackEnabled = false;
    
    [Header("=== VISUAL SETTINGS ===")]
    [Tooltip("Color for enemies behind walls (occluded)")]
    public Color occludedColor = new Color(1f, 0.2f, 0.2f, 0.6f); // Red/Orange
    
    [Tooltip("Color for visible enemies (not behind walls)")]
    public Color visibleColor = new Color(0.2f, 1f, 0.2f, 0.8f); // Green
    
    [Tooltip("Outline color for enhanced visibility")]
    public Color outlineColor = new Color(1f, 1f, 1f, 1f); // White
    
    [Tooltip("Outline width (0 = no outline, 0.01 = thick outline)")]
    [Range(0f, 0.02f)]
    public float outlineWidth = 0.005f;
    
    [Tooltip("Glow intensity for enemies")]
    [Range(0f, 5f)]
    public float glowIntensity = 1.5f;
    
    [Tooltip("Fresnel power (edge glow sharpness)")]
    [Range(0.1f, 10f)]
    public float fresnelPower = 3f;
    
    [Tooltip("Overall transparency")]
    [Range(0f, 1f)]
    public float alphaTransparency = 0.6f;
    
    [Header("=== PERFORMANCE SETTINGS ===")]
    [Tooltip("Maximum distance to render wallhacks (units) - SCALED FOR MASSIVE WORLD!")]
    public float maxRenderDistance = 10000f; // Your world is HUGE!
    
    [Tooltip("Update frequency (times per second). Lower = better performance")]
    [Range(1, 60)]
    public int updateFrequency = 30;
    
    [Tooltip("Use LOD system - reduces quality at distance")]
    public bool useLODSystem = true;
    
    [Tooltip("Distance at which LOD starts reducing quality")]
    public float lodStartDistance = 5000f; // SCALED!
    
    [Tooltip("Batch material updates for better performance")]
    public bool useBatching = true;
    
    [Header("=== ENEMY DETECTION ===")]
    [Tooltip("Layers to detect as enemies")]
    public LayerMask enemyLayers = -1;
    
    [Tooltip("Tags to detect as enemies (comma-separated)")]
    public string[] enemyTags = new string[] { "Enemy", "Boss", "SkullEnemy" };
    
    [Tooltip("Auto-detect enemies by component type")]
    public bool autoDetectByComponent = true;
    
    [Tooltip("Scan radius for enemy detection (units) - SCALED FOR MASSIVE WORLD!")]
    public float enemyScanRadius = 15000f; // HUGE scan range!
    
    [Header("=== ADVANCED FEATURES ===")]
    [Tooltip("Color enemies by health (green = full, red = low)")]
    public bool colorByHealth = true;
    
    [Tooltip("Show distance indicators")]
    public bool showDistanceIndicators = false;
    
    [Tooltip("Brighten enemies that are targeting player")]
    public bool highlightAggressive = true;
    
    [Tooltip("Different color for boss enemies")]
    public bool useBossColor = true;
    public Color bossColor = new Color(1f, 0f, 1f, 0.8f); // Purple
    
    [Header("=== SHADER REFERENCE ===")]
    [Tooltip("Reference to the wallhack shader")]
    public Shader wallhackShader;
    #endregion
    
    #region Private Variables
    private Dictionary<GameObject, WallhackTarget> trackedEnemies = new Dictionary<GameObject, WallhackTarget>();
    private List<GameObject> enemiesToRemove = new List<GameObject>();
    private Material wallhackMaterial;
    private float updateTimer = 0f;
    private float updateInterval;
    private Transform playerTransform;
    private Camera mainCamera;
    
    // Performance tracking
    private int activeWallhacks = 0;
    private float lastScanTime = 0f;
    private const float SCAN_INTERVAL = 0.5f; // Scan for new enemies every 0.5s
    #endregion
    
    #region Initialization
    void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        
        // Initialize
        playerTransform = transform;
        mainCamera = Camera.main;
        updateInterval = 1f / updateFrequency;
        
        // Find or load shader - Auto-detect URP or Built-in pipeline
        if (wallhackShader == null)
        {
            // Try URP version first
            wallhackShader = Shader.Find("Custom/AAA_WallhackShader_URP");
            
            // If not found, try built-in version
            if (wallhackShader == null)
            {
                wallhackShader = Shader.Find("Custom/AAA_WallhackShader");
            }
            
            if (wallhackShader == null)
            {
                Debug.LogError("[AAAWallhackSystem] Wallhack shader not found! Make sure 'WallhackShader_URP.shader' (for URP) or 'WallhackShader.shader' (for Built-in) is in Assets/shaders/");
                enabled = false;
                return;
            }
            else
            {
                Debug.Log($"[AAAWallhackSystem] Using shader: {wallhackShader.name}");
            }
        }
        
        // Create shared material (batching optimization)
        wallhackMaterial = new Material(wallhackShader);
        wallhackMaterial.name = "Wallhack_SharedMaterial";
        
        Debug.Log("[AAAWallhackSystem] Initialized successfully!");
    }
    
    void Start()
    {
        // Initial enemy scan
        ScanForEnemies();
    }
    #endregion
    
    #region Update Loop
    void Update()
    {
        if (!wallhackEnabled)
        {
            // Clean up if disabled
            if (trackedEnemies.Count > 0)
            {
                DisableAllWallhacks();
            }
            return;
        }
        
        // Periodic enemy scanning
        if (Time.time - lastScanTime > SCAN_INTERVAL)
        {
            ScanForEnemies();
            lastScanTime = Time.time;
        }
        
        // Update wallhacks at specified frequency
        updateTimer += Time.deltaTime;
        if (updateTimer >= updateInterval)
        {
            UpdateWallhacks();
            updateTimer = 0f;
        }
    }
    #endregion
    
    #region Enemy Detection
    /// <summary>
    /// Scan for enemies in the scene
    /// </summary>
    private void ScanForEnemies()
    {
        // Method 1: Find by tags
        foreach (string tag in enemyTags)
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject enemy in enemies)
            {
                if (!trackedEnemies.ContainsKey(enemy))
                {
                    RegisterEnemy(enemy);
                }
            }
        }
        
        // Method 2: Find by component (for scripts like SkullEnemy, BossEnemy, etc.)
        if (autoDetectByComponent)
        {
            // Find all IDamageable enemies
            IDamageable[] damageables = FindObjectsOfType<MonoBehaviour>().OfType<IDamageable>().ToArray();
            foreach (IDamageable damageable in damageables)
            {
                MonoBehaviour mb = damageable as MonoBehaviour;
                if (mb != null && !trackedEnemies.ContainsKey(mb.gameObject))
                {
                    // Check if it's actually an enemy (not the player)
                    if (!mb.CompareTag("Player"))
                    {
                        RegisterEnemy(mb.gameObject);
                    }
                }
            }
        }
        
        // Method 3: Sphere overlap for nearby enemies (most performant)
        Collider[] nearbyColliders = Physics.OverlapSphere(playerTransform.position, enemyScanRadius, enemyLayers);
        foreach (Collider col in nearbyColliders)
        {
            if (!trackedEnemies.ContainsKey(col.gameObject) && !col.CompareTag("Player"))
            {
                RegisterEnemy(col.gameObject);
            }
        }
    }
    
    /// <summary>
    /// Register a new enemy for wallhack tracking
    /// </summary>
    private void RegisterEnemy(GameObject enemy)
    {
        if (enemy == null || trackedEnemies.ContainsKey(enemy))
            return;
        
        // Get all renderers (including child objects)
        Renderer[] renderers = enemy.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return;
        
        // Create wallhack target
        WallhackTarget target = new WallhackTarget
        {
            gameObject = enemy,
            transform = enemy.transform,
            renderers = renderers,
            originalMaterials = new Dictionary<Renderer, Material[]>(),
            wallhackMaterials = new List<Material>(),
            isAlive = true,
            isBoss = enemy.CompareTag("Boss") || enemy.name.ToLower().Contains("boss")
        };
        
        // Store original materials and create wallhack materials
        foreach (Renderer renderer in renderers)
        {
            // Skip particle systems and other non-mesh renderers
            if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
            {
                Material[] originalMats = renderer.sharedMaterials;
                target.originalMaterials[renderer] = originalMats;
                
                // Create wallhack material instance
                Material wallhackMat = new Material(wallhackShader);
                target.wallhackMaterials.Add(wallhackMat);
                
                // Apply wallhack material
                Material[] newMats = new Material[originalMats.Length + 1];
                for (int i = 0; i < originalMats.Length; i++)
                {
                    newMats[i] = originalMats[i];
                }
                newMats[originalMats.Length] = wallhackMat;
                renderer.materials = newMats;
            }
        }
        
        trackedEnemies[enemy] = target;
        activeWallhacks++;
        
        Debug.Log($"[AAAWallhackSystem] Registered enemy: {enemy.name} (Total: {activeWallhacks})");
    }
    
    /// <summary>
    /// Unregister an enemy (when destroyed or too far away)
    /// </summary>
    private void UnregisterEnemy(GameObject enemy)
    {
        if (!trackedEnemies.ContainsKey(enemy))
            return;
        
        WallhackTarget target = trackedEnemies[enemy];
        
        // Restore original materials
        foreach (var kvp in target.originalMaterials)
        {
            Renderer renderer = kvp.Key;
            if (renderer != null)
            {
                renderer.materials = kvp.Value;
            }
        }
        
        // Clean up wallhack materials
        foreach (Material mat in target.wallhackMaterials)
        {
            if (mat != null)
                Destroy(mat);
        }
        
        trackedEnemies.Remove(enemy);
        activeWallhacks--;
    }
    #endregion
    
    #region Wallhack Updates
    /// <summary>
    /// Update all active wallhacks
    /// </summary>
    private void UpdateWallhacks()
    {
        enemiesToRemove.Clear();
        
        foreach (var kvp in trackedEnemies)
        {
            GameObject enemy = kvp.Key;
            WallhackTarget target = kvp.Value;
            
            // Check if enemy still exists
            if (enemy == null || !enemy.activeInHierarchy)
            {
                enemiesToRemove.Add(enemy);
                continue;
            }
            
            // Distance culling for performance
            float distance = Vector3.Distance(playerTransform.position, target.transform.position);
            if (distance > maxRenderDistance)
            {
                // Disable wallhack for distant enemies
                SetWallhackActive(target, false);
                continue;
            }
            
            // Enable wallhack
            SetWallhackActive(target, true);
            
            // Update material properties
            UpdateWallhackMaterial(target, distance);
        }
        
        // Remove dead/destroyed enemies
        foreach (GameObject enemy in enemiesToRemove)
        {
            UnregisterEnemy(enemy);
        }
    }
    
    /// <summary>
    /// Update material properties for a wallhack target
    /// </summary>
    private void UpdateWallhackMaterial(WallhackTarget target, float distance)
    {
        foreach (Material mat in target.wallhackMaterials)
        {
            if (mat == null)
                continue;
            
            // Determine colors
            Color occluded = occludedColor;
            Color visible = visibleColor;
            
            // Boss color override
            if (useBossColor && target.isBoss)
            {
                occluded = bossColor;
                visible = bossColor;
            }
            
            // Health-based coloring
            if (colorByHealth)
            {
                float healthPercent = GetHealthPercent(target.gameObject);
                if (healthPercent >= 0)
                {
                    // Lerp from green (full health) to red (low health)
                    visible = Color.Lerp(Color.red, Color.green, healthPercent);
                    occluded = Color.Lerp(new Color(1f, 0f, 0f, 0.6f), new Color(1f, 0.5f, 0f, 0.6f), healthPercent);
                }
            }
            
            // LOD system - reduce quality at distance
            float alpha = alphaTransparency;
            float glow = glowIntensity;
            float outline = outlineWidth;
            
            if (useLODSystem && distance > lodStartDistance)
            {
                float lodFactor = 1f - Mathf.Clamp01((distance - lodStartDistance) / (maxRenderDistance - lodStartDistance));
                glow *= lodFactor;
                outline *= lodFactor;
                alpha *= lodFactor;
            }
            
            // Apply material properties
            mat.SetColor("_WallhackColor", occluded);
            mat.SetColor("_VisibleColor", visible);
            mat.SetColor("_OutlineColor", outlineColor);
            mat.SetFloat("_OutlineWidth", outline);
            mat.SetFloat("_GlowIntensity", glow);
            mat.SetFloat("_FresnelPower", fresnelPower);
            mat.SetFloat("_Alpha", alpha);
            mat.SetFloat("_UseOutline", outlineWidth > 0 ? 1f : 0f);
            mat.SetFloat("_UseFresnel", glowIntensity > 0 ? 1f : 0f);
        }
    }
    
    /// <summary>
    /// Enable or disable wallhack rendering for a target
    /// </summary>
    private void SetWallhackActive(WallhackTarget target, bool active)
    {
        foreach (Renderer renderer in target.renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = active;
            }
        }
    }
    #endregion
    
    #region Helper Methods
    /// <summary>
    /// Get health percentage of an enemy (0-1)
    /// Returns -1 if health component not found
    /// </summary>
    private float GetHealthPercent(GameObject enemy)
    {
        // Try common health component patterns
        
        // Pattern 1: SkullEnemy style (maxHealth + currentHealth)
        var skullEnemy = enemy.GetComponent<SkullEnemy>();
        if (skullEnemy != null)
        {
            // Use reflection to access private health fields if needed
            var healthField = skullEnemy.GetType().GetField("currentHealth", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var maxHealthField = skullEnemy.GetType().GetField("maxHealth", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            if (healthField != null && maxHealthField != null)
            {
                float current = (float)healthField.GetValue(skullEnemy);
                float max = (float)maxHealthField.GetValue(skullEnemy);
                return max > 0 ? current / max : -1;
            }
        }
        
        // Pattern 2: Generic Health component
        var health = enemy.GetComponent<Health>();
        if (health != null)
        {
            return health.currentHealth / health.maxHealth;
        }
        
        // Pattern 3: IDamageable interface (custom implementation needed)
        // This is a placeholder - you'd need to implement your own health tracking
        
        return -1; // Health unknown
    }
    
    /// <summary>
    /// Disable all active wallhacks
    /// </summary>
    private void DisableAllWallhacks()
    {
        List<GameObject> allEnemies = new List<GameObject>(trackedEnemies.Keys);
        foreach (GameObject enemy in allEnemies)
        {
            UnregisterEnemy(enemy);
        }
        trackedEnemies.Clear();
        activeWallhacks = 0;
    }
    #endregion
    
    #region Public API
    /// <summary>
    /// Toggle wallhack on/off
    /// </summary>
    public void ToggleWallhack()
    {
        wallhackEnabled = !wallhackEnabled;
        Debug.Log($"[AAAWallhackSystem] Wallhack {(wallhackEnabled ? "ENABLED" : "DISABLED")}");
        
        if (!wallhackEnabled)
        {
            DisableAllWallhacks();
        }
    }
    
    /// <summary>
    /// Set wallhack state
    /// </summary>
    public void SetWallhackEnabled(bool enabled)
    {
        wallhackEnabled = enabled;
        if (!enabled)
        {
            DisableAllWallhacks();
        }
    }
    
    /// <summary>
    /// Force rescan for enemies
    /// </summary>
    public void ForceRescan()
    {
        ScanForEnemies();
    }
    
    /// <summary>
    /// Get number of active wallhacks
    /// </summary>
    public int GetActiveWallhackCount()
    {
        return activeWallhacks;
    }
    #endregion
    
    #region Cleanup
    void OnDestroy()
    {
        DisableAllWallhacks();
        
        if (wallhackMaterial != null)
        {
            Destroy(wallhackMaterial);
        }
        
        if (Instance == this)
        {
            Instance = null;
        }
    }
    #endregion
    
    #region Debug
    void OnGUI()
    {
        if (!wallhackEnabled)
            return;
        
        // Draw debug info
        GUIStyle style = new GUIStyle();
        style.fontSize = 14;
        style.normal.textColor = Color.white;
        
        GUI.Label(new Rect(10, 10, 300, 25), $"Wallhack Active: {activeWallhacks} enemies", style);
        GUI.Label(new Rect(10, 30, 300, 25), $"Update Rate: {updateFrequency} Hz", style);
    }
    #endregion
}

// ============================================================================
// WALLHACK TARGET DATA STRUCTURE
// Stores information about each tracked enemy
// ============================================================================
public class WallhackTarget
{
    public GameObject gameObject;
    public Transform transform;
    public Renderer[] renderers;
    public Dictionary<Renderer, Material[]> originalMaterials;
    public List<Material> wallhackMaterials;
    public bool isAlive;
    public bool isBoss;
}

// ============================================================================
// HELPER: Generic Health Component (if you don't have one already)
// ============================================================================
public class Health : MonoBehaviour
{
    public float currentHealth = 100f;
    public float maxHealth = 100f;
}
