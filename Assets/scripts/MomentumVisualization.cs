// ============================================================================
// MOMENTUM VISUALIZATION - Zero Bloat, Maximum Impact
// ============================================================================
// Shows real-time speed, chain combos, and momentum gains
// Optimized for performance - single Update(), minimal allocations
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Lightweight momentum visualization system
/// Shows speed counter, chain combos, and gain indicators
/// Zero bloat - uses object pooling and efficient updates
/// </summary>
public class MomentumVisualization : MonoBehaviour
{
    #region Singleton
    public static MomentumVisualization Instance { get; private set; }
    #endregion
    
    #region Configuration
    [Header("=== REFERENCES ===")]
    [Tooltip("Canvas to use for UI (REQUIRED - assign your game's canvas)")]
    [SerializeField] private Canvas targetCanvas;
    
    [Tooltip("Player's movement controller (auto-found if null)")]
    [SerializeField] private AAAMovementController playerMovement;
    
    [Header("=== SPEED COUNTER ===")]
    [Tooltip("Show real-time speed in units/s")]
    [SerializeField] private bool showSpeedCounter = true;
    
    [Tooltip("Speed threshold to show counter (hide when slow)")]
    [SerializeField] private float speedThreshold = 500f;
    
    [Tooltip("Color gradient for speed (slow → fast)")]
    [SerializeField] private Gradient speedGradient;
    
    [Header("=== CHAIN COUNTER ===")]
    [Tooltip("Show chain combo multiplier (2x, 3x, etc.)")]
    [SerializeField] private bool showChainCounter = true;
    
    [Tooltip("Time window to maintain chain (seconds)")]
    [SerializeField] private float chainTimeWindow = 2.0f;
    
    [Tooltip("Minimum speed gain to count as chain link")]
    [SerializeField] private float minSpeedGainForChain = 200f;
    
    [Header("=== GAIN INDICATORS ===")]
    [Tooltip("Show floating +XXX speed gain text")]
    [SerializeField] private bool showGainIndicators = true;
    
    [Tooltip("Minimum gain to show indicator")]
    [SerializeField] private float minGainToShow = 100f;
    
    [Tooltip("Gain indicator lifetime (seconds)")]
    [SerializeField] private float gainIndicatorLifetime = 1.5f;
    
    [Header("=== UI POSITIONING ===")]
    [Tooltip("Speed counter position (0-1 screen space)")]
    [SerializeField] private Vector2 speedCounterPosition = new Vector2(0.5f, 0.85f);
    
    [Tooltip("Chain counter position (0-1 screen space)")]
    [SerializeField] private Vector2 chainCounterPosition = new Vector2(0.5f, 0.75f);
    
    [Header("=== STYLING ===")]
    [Tooltip("Font size for speed counter")]
    [SerializeField] private int speedFontSize = 48;
    
    [Tooltip("Font size for chain counter")]
    [SerializeField] private int chainFontSize = 36;
    
    [Tooltip("Font size for gain indicators")]
    [SerializeField] private int gainFontSize = 24;
    #endregion
    
    #region Private State
    // Chain tracking
    private int currentChainCount = 0;
    private float lastChainTime = -999f;
    private float lastSpeed = 0f;
    
    // Gain indicator pool
    private class GainIndicator
    {
        public GameObject gameObject;
        public Text text;
        public float spawnTime;
        public Vector3 worldPosition;
        public bool active;
    }
    private List<GainIndicator> gainIndicatorPool = new List<GainIndicator>();
    private const int GAIN_INDICATOR_POOL_SIZE = 10;
    
    // UI elements
    private Canvas canvas;
    private Text speedText;
    private Text chainText;
    
    // Performance optimization
    private float lastUpdateTime = 0f;
    private const float UPDATE_INTERVAL = 0.016f; // ~60fps
    #endregion
    
    #region Initialization
    void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Auto-find player if not assigned
        if (playerMovement == null)
        {
            playerMovement = FindObjectOfType<AAAMovementController>();
        }
        
        if (playerMovement == null)
        {
            Debug.LogError("[MOMENTUM VIZ] AAAMovementController not found! Disabling.");
            enabled = false;
            return;
        }
        
        // Initialize gradient if null
        if (speedGradient == null)
        {
            speedGradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[3];
            colorKeys[0] = new GradientColorKey(Color.white, 0f);      // Slow
            colorKeys[1] = new GradientColorKey(Color.yellow, 0.5f);   // Medium
            colorKeys[2] = new GradientColorKey(Color.red, 1f);        // Fast
            
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);
            
            speedGradient.SetKeys(colorKeys, alphaKeys);
        }
        
        // Create UI
        CreateUI();
        
        Debug.Log("[MOMENTUM VIZ] Initialized - Zero bloat, maximum impact!");
    }
    
    void CreateUI()
    {
        // Use assigned canvas or create one
        if (targetCanvas == null)
        {
            Debug.LogWarning("[MOMENTUM VIZ] No canvas assigned! Creating default canvas. Assign your game's canvas for better control.");
            
            GameObject canvasObj = new GameObject("MomentumVisualizationCanvas");
            canvasObj.transform.SetParent(transform);
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // On top of everything
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObj.AddComponent<GraphicRaycaster>(); // For UI interaction
        }
        else
        {
            canvas = targetCanvas;
            Debug.Log($"[MOMENTUM VIZ] Using assigned canvas: {canvas.name}");
        }
        
        // Speed counter
        if (showSpeedCounter)
        {
            GameObject speedObj = new GameObject("SpeedCounter");
            speedObj.transform.SetParent(canvas.transform);
            speedText = speedObj.AddComponent<Text>();
            speedText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            speedText.fontSize = speedFontSize;
            speedText.alignment = TextAnchor.MiddleCenter;
            speedText.color = Color.white;
            
            RectTransform speedRect = speedObj.GetComponent<RectTransform>();
            speedRect.anchorMin = speedCounterPosition;
            speedRect.anchorMax = speedCounterPosition;
            speedRect.pivot = new Vector2(0.5f, 0.5f);
            speedRect.sizeDelta = new Vector2(400, 100);
            
            // Outline for readability
            Outline outline = speedObj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, -2);
            
            Debug.Log($"[MOMENTUM VIZ] Speed counter created! Position: {speedCounterPosition} | Canvas: {canvas.renderMode} | Active: {speedObj.activeSelf}");
        }
        
        // Chain counter
        if (showChainCounter)
        {
            GameObject chainObj = new GameObject("ChainCounter");
            chainObj.transform.SetParent(canvas.transform);
            chainText = chainObj.AddComponent<Text>();
            chainText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            chainText.fontSize = chainFontSize;
            chainText.alignment = TextAnchor.MiddleCenter;
            chainText.color = new Color(1f, 0.8f, 0.2f); // Gold
            
            RectTransform chainRect = chainObj.GetComponent<RectTransform>();
            chainRect.anchorMin = chainCounterPosition;
            chainRect.anchorMax = chainCounterPosition;
            chainRect.pivot = new Vector2(0.5f, 0.5f);
            chainRect.sizeDelta = new Vector2(300, 80);
            
            // Outline
            Outline outline = chainObj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, -2);
            
            chainText.gameObject.SetActive(false); // Hidden by default
        }
        
        // Initialize gain indicator pool
        if (showGainIndicators)
        {
            for (int i = 0; i < GAIN_INDICATOR_POOL_SIZE; i++)
            {
                CreateGainIndicator();
            }
        }
    }
    
    void CreateGainIndicator()
    {
        GameObject gainObj = new GameObject($"GainIndicator_{gainIndicatorPool.Count}");
        gainObj.transform.SetParent(canvas.transform);
        
        Text gainText = gainObj.AddComponent<Text>();
        gainText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        gainText.fontSize = gainFontSize;
        gainText.alignment = TextAnchor.MiddleCenter;
        gainText.color = Color.green;
        
        RectTransform gainRect = gainObj.GetComponent<RectTransform>();
        gainRect.sizeDelta = new Vector2(200, 50);
        
        // Outline
        Outline outline = gainObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1, -1);
        
        gainObj.SetActive(false);
        
        GainIndicator indicator = new GainIndicator
        {
            gameObject = gainObj,
            text = gainText,
            active = false
        };
        
        gainIndicatorPool.Add(indicator);
    }
    #endregion
    
    #region Update Loop
    void Update()
    {
        // Throttle updates for performance
        if (Time.time - lastUpdateTime < UPDATE_INTERVAL)
            return;
        lastUpdateTime = Time.time;
        
        if (playerMovement == null) return;
        
        // Get current speed
        float currentSpeed = playerMovement.Velocity.magnitude;
        
        // Update speed counter
        if (showSpeedCounter && speedText != null)
        {
            UpdateSpeedCounter(currentSpeed);
        }
        
        // Update chain counter
        if (showChainCounter && chainText != null)
        {
            UpdateChainCounter(currentSpeed);
        }
        
        // Update gain indicators
        if (showGainIndicators)
        {
            UpdateGainIndicators();
        }
        
        // Track speed for next frame
        lastSpeed = currentSpeed;
    }
    
    void UpdateSpeedCounter(float currentSpeed)
    {
        // Hide if below threshold
        if (currentSpeed < speedThreshold)
        {
            if (speedText.gameObject.activeSelf)
            {
                speedText.gameObject.SetActive(false);
                Debug.Log($"[MOMENTUM VIZ] Speed counter hidden (speed: {currentSpeed:F0} < threshold: {speedThreshold})");
            }
            return;
        }
        
        if (!speedText.gameObject.activeSelf)
        {
            speedText.gameObject.SetActive(true);
            Debug.Log($"[MOMENTUM VIZ] Speed counter shown! Speed: {currentSpeed:F0}");
        }
        
        // Format speed
        speedText.text = $"{currentSpeed:F0}";
        
        // Color based on speed (0-6000 range)
        float speedNormalized = Mathf.Clamp01(currentSpeed / 6000f);
        speedText.color = speedGradient.Evaluate(speedNormalized);
        
        // Scale based on speed (subtle pulse)
        float scale = 1f + (speedNormalized * 0.2f);
        speedText.transform.localScale = Vector3.one * scale;
        
        // DEBUG: Log position
        RectTransform rect = speedText.GetComponent<RectTransform>();
        if (Time.frameCount % 60 == 0) // Every 60 frames
        {
            Debug.Log($"[MOMENTUM VIZ] Speed: {currentSpeed:F0} | Text: '{speedText.text}' | Active: {speedText.gameObject.activeSelf} | Position: {rect.position} | Canvas: {canvas.name}");
        }
    }
    
    void UpdateChainCounter(float currentSpeed)
    {
        // Check if chain is still active
        float timeSinceLastChain = Time.time - lastChainTime;
        if (timeSinceLastChain > chainTimeWindow)
        {
            // Chain expired
            if (currentChainCount > 0)
            {
                currentChainCount = 0;
                chainText.gameObject.SetActive(false);
            }
        }
        
        // Show chain if active
        if (currentChainCount > 1)
        {
            chainText.gameObject.SetActive(true);
            chainText.text = $"{currentChainCount}x CHAIN!";
            
            // Pulse animation
            float pulse = 1f + Mathf.Sin(Time.time * 5f) * 0.1f;
            chainText.transform.localScale = Vector3.one * pulse;
        }
        else
        {
            chainText.gameObject.SetActive(false);
        }
    }
    
    void UpdateGainIndicators()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) return;
        
        foreach (var indicator in gainIndicatorPool)
        {
            if (!indicator.active) continue;
            
            float lifetime = Time.time - indicator.spawnTime;
            
            // Deactivate if expired
            if (lifetime > gainIndicatorLifetime)
            {
                indicator.active = false;
                indicator.gameObject.SetActive(false);
                continue;
            }
            
            // Animate position (float upward)
            Vector3 worldPos = indicator.worldPosition + Vector3.up * (lifetime * 200f);
            Vector3 screenPos = mainCam.WorldToScreenPoint(worldPos);
            
            // Check if on screen
            if (screenPos.z > 0)
            {
                RectTransform rect = indicator.gameObject.GetComponent<RectTransform>();
                rect.position = screenPos;
                
                // Fade out
                float alpha = 1f - (lifetime / gainIndicatorLifetime);
                Color color = indicator.text.color;
                color.a = alpha;
                indicator.text.color = color;
            }
            else
            {
                // Off screen - deactivate
                indicator.active = false;
                indicator.gameObject.SetActive(false);
            }
        }
    }
    #endregion
    
    #region Public API
    /// <summary>
    /// Call this when player gains speed (wall jump, rope release, etc.)
    /// </summary>
    public void OnSpeedGain(float speedBefore, float speedAfter, Vector3 worldPosition)
    {
        float gain = speedAfter - speedBefore;
        
        // Ignore small gains
        if (gain < minGainToShow) return;
        
        // Update chain
        if (gain >= minSpeedGainForChain)
        {
            currentChainCount++;
            lastChainTime = Time.time;
        }
        
        // Show gain indicator
        if (showGainIndicators)
        {
            ShowGainIndicator(gain, worldPosition);
        }
    }
    
    /// <summary>
    /// Call this when chain is broken (landing, collision, etc.)
    /// </summary>
    public void BreakChain()
    {
        if (currentChainCount > 1)
        {
            Debug.Log($"[MOMENTUM VIZ] Chain broken! Final: {currentChainCount}x");
        }
        
        currentChainCount = 0;
        lastChainTime = -999f;
        
        if (chainText != null)
        {
            chainText.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Manual chain increment (for special moves)
    /// </summary>
    public void IncrementChain()
    {
        currentChainCount++;
        lastChainTime = Time.time;
    }
    
    /// <summary>
    /// Get current chain count
    /// </summary>
    public int GetChainCount() => currentChainCount;
    #endregion
    
    #region Private Methods
    void ShowGainIndicator(float gain, Vector3 worldPosition)
    {
        // Find inactive indicator
        GainIndicator indicator = null;
        foreach (var ind in gainIndicatorPool)
        {
            if (!ind.active)
            {
                indicator = ind;
                break;
            }
        }
        
        // All in use - reuse oldest
        if (indicator == null)
        {
            float oldestTime = float.MaxValue;
            foreach (var ind in gainIndicatorPool)
            {
                if (ind.spawnTime < oldestTime)
                {
                    oldestTime = ind.spawnTime;
                    indicator = ind;
                }
            }
        }
        
        if (indicator == null) return;
        
        // Activate indicator
        indicator.active = true;
        indicator.spawnTime = Time.time;
        indicator.worldPosition = worldPosition;
        indicator.gameObject.SetActive(true);
        
        // Set text
        indicator.text.text = $"+{gain:F0}";
        
        // Color based on gain size
        if (gain >= 1000f)
            indicator.text.color = new Color(1f, 0.2f, 0.2f); // Red (huge!)
        else if (gain >= 500f)
            indicator.text.color = new Color(1f, 0.8f, 0.2f); // Gold (big)
        else
            indicator.text.color = new Color(0.2f, 1f, 0.2f); // Green (normal)
    }
    #endregion
    
    #region Cleanup
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    #endregion
}
