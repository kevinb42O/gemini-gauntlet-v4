// ============================================================================
// AAA ESP OVERLAY SYSTEM - Professional HUD Overlays
// Distance indicators, health bars, name tags, and more!
// Complements the wallhack system with 2D UI overlays
// 
// This is the "ESP" part of wallhacks - Enhanced Sensory Perception
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// ESP (Extra Sensory Perception) overlay system
/// Draws 2D UI elements over 3D enemies for enhanced awareness
/// </summary>
public class AAAESPOverlay : MonoBehaviour
{
    #region Singleton
    public static AAAESPOverlay Instance { get; private set; }
    #endregion
    
    #region Configuration
    [Header("=== ESP TOGGLE ===")]
    public bool espEnabled = false;
    
    [Header("=== ESP FEATURES ===")]
    public bool showHealthBars = true;
    public bool showDistanceIndicators = true;
    public bool showNameTags = true;
    public bool showOffScreenIndicators = true;
    public bool showDamageNumbers = true;
    
    [Header("=== VISUAL SETTINGS ===")]
    public Color healthBarFull = Color.green;
    public Color healthBarLow = Color.red;
    public Color distanceTextColor = Color.white;
    public Color nameTagColor = Color.yellow;
    
    [Header("=== PERFORMANCE ===")]
    public float maxESPDistance = 10000f; // SCALED FOR MASSIVE WORLD!
    [Range(1, 60)]
    public int updateFrequency = 30;
    public bool useOcclusionCheck = true; // Only show ESP for visible enemies
    
    [Header("=== UI REFERENCES ===")]
    public Canvas espCanvas;
    public GameObject healthBarPrefab;
    public GameObject distanceIndicatorPrefab;
    public GameObject nameTagPrefab;
    public GameObject offScreenIndicatorPrefab;
    public GameObject damageNumberPrefab;
    
    [Header("=== FONTS ===")]
    public Font espFont;
    public TMP_FontAsset tmpFont;
    #endregion
    
    #region Private Variables
    private Dictionary<GameObject, ESPUIElements> espElements = new Dictionary<GameObject, ESPUIElements>();
    private Camera mainCamera;
    private Transform playerTransform;
    private float updateTimer = 0f;
    private float updateInterval;
    private List<GameObject> enemiesToRemove = new List<GameObject>();
    
    // Object pools for performance
    private Queue<GameObject> healthBarPool = new Queue<GameObject>();
    private Queue<GameObject> distanceIndicatorPool = new Queue<GameObject>();
    private Queue<GameObject> nameTagPool = new Queue<GameObject>();
    #endregion
    
    #region Initialization
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        
        mainCamera = Camera.main;
        playerTransform = transform;
        updateInterval = 1f / updateFrequency;
        
        // Create canvas if not assigned
        if (espCanvas == null)
        {
            CreateESPCanvas();
        }
        
        // Create default prefabs if not assigned
        CreateDefaultPrefabs();
    }
    
    void CreateESPCanvas()
    {
        GameObject canvasObj = new GameObject("ESP_Canvas_AUTO_CREATED");
        canvasObj.transform.SetParent(null); // Root object
        espCanvas = canvasObj.AddComponent<Canvas>();
        espCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        espCanvas.sortingOrder = 100; // Render on top
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        Debug.Log("<color=lime>[AAAESPOverlay] ✅ AUTO-CREATED ESP CANVAS! ESP is ready!</color>");
    }
    
    void CreateDefaultPrefabs()
    {
        // Create health bar prefab
        if (healthBarPrefab == null)
        {
            healthBarPrefab = CreateHealthBarPrefab();
        }
        
        // Create distance indicator prefab
        if (distanceIndicatorPrefab == null)
        {
            distanceIndicatorPrefab = CreateDistanceIndicatorPrefab();
        }
        
        // Create name tag prefab
        if (nameTagPrefab == null)
        {
            nameTagPrefab = CreateNameTagPrefab();
        }
    }
    
    GameObject CreateHealthBarPrefab()
    {
        GameObject prefab = new GameObject("HealthBar_Prefab");
        prefab.transform.SetParent(espCanvas.transform, false);
        
        // Background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(prefab.transform, false);
        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.5f);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(100, 10);
        
        // Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(prefab.transform, false);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = healthBarFull;
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.sizeDelta = new Vector2(100, 10);
        fillRect.anchorMin = new Vector2(0, 0.5f);
        fillRect.anchorMax = new Vector2(0, 0.5f);
        fillRect.pivot = new Vector2(0, 0.5f);
        
        // Store fill reference
        prefab.AddComponent<HealthBarUI>().fillImage = fillImage;
        
        prefab.SetActive(false);
        return prefab;
    }
    
    GameObject CreateDistanceIndicatorPrefab()
    {
        GameObject prefab = new GameObject("DistanceIndicator_Prefab");
        prefab.transform.SetParent(espCanvas.transform, false);
        
        TextMeshProUGUI text = prefab.AddComponent<TextMeshProUGUI>();
        text.fontSize = 14;
        text.color = distanceTextColor;
        text.alignment = TextAlignmentOptions.Center;
        text.font = tmpFont;
        
        RectTransform rect = prefab.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(100, 30);
        
        prefab.SetActive(false);
        return prefab;
    }
    
    GameObject CreateNameTagPrefab()
    {
        GameObject prefab = new GameObject("NameTag_Prefab");
        prefab.transform.SetParent(espCanvas.transform, false);
        
        TextMeshProUGUI text = prefab.AddComponent<TextMeshProUGUI>();
        text.fontSize = 16;
        text.color = nameTagColor;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.font = tmpFont;
        
        // Outline for readability
        text.outlineWidth = 0.2f;
        text.outlineColor = Color.black;
        
        RectTransform rect = prefab.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 30);
        
        prefab.SetActive(false);
        return prefab;
    }
    #endregion
    
    #region Update Loop
    void Update()
    {
        if (!espEnabled)
        {
            if (espElements.Count > 0)
            {
                DisableAllESP();
            }
            return;
        }
        
        updateTimer += Time.deltaTime;
        if (updateTimer >= updateInterval)
        {
            UpdateESPElements();
            updateTimer = 0f;
        }
    }
    #endregion
    
    #region ESP Management
    public void RegisterEnemy(GameObject enemy)
    {
        if (enemy == null || espElements.ContainsKey(enemy))
            return;
        
        ESPUIElements elements = new ESPUIElements
        {
            enemy = enemy,
            transform = enemy.transform
        };
        
        // Create health bar
        if (showHealthBars)
        {
            elements.healthBar = GetFromPool(healthBarPool, healthBarPrefab);
            elements.healthBarUI = elements.healthBar.GetComponent<HealthBarUI>();
        }
        
        // Create distance indicator
        if (showDistanceIndicators)
        {
            elements.distanceIndicator = GetFromPool(distanceIndicatorPool, distanceIndicatorPrefab);
            elements.distanceText = elements.distanceIndicator.GetComponent<TextMeshProUGUI>();
        }
        
        // Create name tag
        if (showNameTags)
        {
            elements.nameTag = GetFromPool(nameTagPool, nameTagPrefab);
            elements.nameText = elements.nameTag.GetComponent<TextMeshProUGUI>();
            elements.nameText.text = GetEnemyName(enemy);
        }
        
        espElements[enemy] = elements;
    }
    
    public void UnregisterEnemy(GameObject enemy)
    {
        if (!espElements.ContainsKey(enemy))
            return;
        
        ESPUIElements elements = espElements[enemy];
        
        // Return to pools
        if (elements.healthBar != null)
            ReturnToPool(healthBarPool, elements.healthBar);
        if (elements.distanceIndicator != null)
            ReturnToPool(distanceIndicatorPool, elements.distanceIndicator);
        if (elements.nameTag != null)
            ReturnToPool(nameTagPool, elements.nameTag);
        
        espElements.Remove(enemy);
    }
    
    void UpdateESPElements()
    {
        enemiesToRemove.Clear();
        
        foreach (var kvp in espElements)
        {
            GameObject enemy = kvp.Key;
            ESPUIElements elements = kvp.Value;
            
            // Check if enemy still exists
            if (enemy == null || !enemy.activeInHierarchy)
            {
                enemiesToRemove.Add(enemy);
                continue;
            }
            
            // Distance check
            float distance = Vector3.Distance(playerTransform.position, elements.transform.position);
            if (distance > maxESPDistance)
            {
                SetESPElementsActive(elements, false);
                continue;
            }
            
            // Screen position
            Vector3 screenPos = mainCamera.WorldToScreenPoint(elements.transform.position);
            bool isOnScreen = screenPos.z > 0 && screenPos.x > 0 && screenPos.x < Screen.width && 
                             screenPos.y > 0 && screenPos.y < Screen.height;
            
            // Occlusion check
            bool isVisible = true;
            if (useOcclusionCheck)
            {
                isVisible = CheckVisibility(elements.transform.position);
            }
            
            if (isOnScreen && isVisible)
            {
                UpdateESPPosition(elements, screenPos, distance);
                SetESPElementsActive(elements, true);
            }
            else if (showOffScreenIndicators && !isOnScreen)
            {
                // Show off-screen indicators
                UpdateOffScreenIndicator(elements, screenPos);
            }
            else
            {
                SetESPElementsActive(elements, false);
            }
        }
        
        // Clean up
        foreach (GameObject enemy in enemiesToRemove)
        {
            UnregisterEnemy(enemy);
        }
    }
    
    void UpdateESPPosition(ESPUIElements elements, Vector3 screenPos, float distance)
    {
        Vector2 uiPos = new Vector2(screenPos.x, screenPos.y);
        
        // Update health bar
        if (elements.healthBar != null && showHealthBars)
        {
            RectTransform rect = elements.healthBar.GetComponent<RectTransform>();
            rect.anchoredPosition = uiPos + new Vector2(0, 50);
            
            // Update health percentage
            float healthPercent = GetHealthPercent(elements.enemy);
            if (healthPercent >= 0)
            {
                elements.healthBarUI.fillImage.fillAmount = healthPercent;
                elements.healthBarUI.fillImage.color = Color.Lerp(healthBarLow, healthBarFull, healthPercent);
            }
        }
        
        // Update distance indicator
        if (elements.distanceIndicator != null && showDistanceIndicators)
        {
            RectTransform rect = elements.distanceIndicator.GetComponent<RectTransform>();
            rect.anchoredPosition = uiPos + new Vector2(0, 70);
            elements.distanceText.text = $"{Mathf.RoundToInt(distance)}m";
        }
        
        // Update name tag
        if (elements.nameTag != null && showNameTags)
        {
            RectTransform rect = elements.nameTag.GetComponent<RectTransform>();
            rect.anchoredPosition = uiPos + new Vector2(0, 30);
        }
    }
    
    void UpdateOffScreenIndicator(ESPUIElements elements, Vector3 screenPos)
    {
        // TODO: Implement off-screen indicators (arrows pointing to enemies)
    }
    
    void SetESPElementsActive(ESPUIElements elements, bool active)
    {
        if (elements.healthBar != null)
            elements.healthBar.SetActive(active);
        if (elements.distanceIndicator != null)
            elements.distanceIndicator.SetActive(active);
        if (elements.nameTag != null)
            elements.nameTag.SetActive(active);
    }
    
    void DisableAllESP()
    {
        List<GameObject> allEnemies = new List<GameObject>(espElements.Keys);
        foreach (GameObject enemy in allEnemies)
        {
            UnregisterEnemy(enemy);
        }
        espElements.Clear();
    }
    #endregion
    
    #region Helper Methods
    bool CheckVisibility(Vector3 targetPos)
    {
        Vector3 direction = targetPos - mainCamera.transform.position;
        float distance = direction.magnitude;
        
        RaycastHit hit;
        if (Physics.Raycast(mainCamera.transform.position, direction, out hit, distance))
        {
            return false; // Occluded
        }
        
        return true; // Visible
    }
    
    float GetHealthPercent(GameObject enemy)
    {
        // Same logic as AAAWallhackSystem
        var skullEnemy = enemy.GetComponent<SkullEnemy>();
        if (skullEnemy != null)
        {
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
        
        return -1;
    }
    
    string GetEnemyName(GameObject enemy)
    {
        // Try to get a proper name
        if (enemy.CompareTag("Boss"))
            return "⭐ BOSS";
        if (enemy.name.Contains("Skull"))
            return "💀 Skull";
        if (enemy.name.Contains("Guardian"))
            return "🛡️ Guardian";
        
        return "👹 Enemy";
    }
    #endregion
    
    #region Object Pooling
    GameObject GetFromPool(Queue<GameObject> pool, GameObject prefab)
    {
        if (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            obj.SetActive(true);
            return obj;
        }
        
        return Instantiate(prefab, espCanvas.transform);
    }
    
    void ReturnToPool(Queue<GameObject> pool, GameObject obj)
    {
        obj.SetActive(false);
        pool.Enqueue(obj);
    }
    #endregion
    
    #region Public API
    public void ToggleESP()
    {
        espEnabled = !espEnabled;
    }
    
    public void SetESPEnabled(bool enabled)
    {
        espEnabled = enabled;
    }
    #endregion
}

// ============================================================================
// ESP UI ELEMENT DATA
// ============================================================================
public class ESPUIElements
{
    public GameObject enemy;
    public Transform transform;
    public GameObject healthBar;
    public GameObject distanceIndicator;
    public GameObject nameTag;
    public GameObject offScreenIndicator;
    
    public HealthBarUI healthBarUI;
    public TextMeshProUGUI distanceText;
    public TextMeshProUGUI nameText;
}

// ============================================================================
// HEALTH BAR UI COMPONENT
// ============================================================================
public class HealthBarUI : MonoBehaviour
{
    public Image fillImage;
}
