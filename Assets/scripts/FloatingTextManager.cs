using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using TMPro;

namespace GeminiGauntlet.UI
{
    /// <summary>
    /// Manages floating text that appears when gaining XP or other notifications
    /// </summary>
    public class FloatingTextManager : MonoBehaviour
    {
    [Header("Floating Text Settings")]
    [SerializeField] private GameObject floatingTextPrefab;
    [SerializeField] private Canvas worldCanvas; // Canvas for world space UI
    [SerializeField] private float floatHeight = 2f;
    [SerializeField] private float floatDuration = 0.5f;
    [SerializeField] [Range(12, 500)] private int textSize = 200; // MUCH BIGGER for distant viewing!
    [SerializeField] [Range(1, 500)] private float worldScaleMultiplier = 10f; // Scale UP the entire canvas!
    [SerializeField] private bool debugMode = true;
    
    [Header("Smart Kill Combo System")]
    [SerializeField] private bool enableKillCombo = true;
    [SerializeField] private float comboWindow = 3.0f; // Time window to aggregate kills (longer = easier combos)
    [SerializeField] private float comboDisplayDelay = 0.25f; // Delay before showing combo (wait for more kills)
    [Tooltip("XP multipliers based on kill count: [5-9 kills], [10-19 kills], [20-39 kills], [40+ kills]")]
    [SerializeField] private float[] comboMultipliers = new float[] { 1.5f, 2.0f, 3.0f, 5.0f }; // MORE AGGRESSIVE multipliers!
    [Tooltip("Size multipliers for larger combos - kept reasonable to avoid distraction")]
    [SerializeField] private float[] comboSizeMultipliers = new float[] { 1.0f, 1.2f, 1.4f, 1.7f, 2.0f }; // Cleaner, less extreme
    
    // Kill tracking data structure
    private class KillComboData
    {
        public int killCount = 0;
        public int totalXP = 0;
        public Vector3 averagePosition = Vector3.zero;
        public float lastKillTime = 0f;
        public System.Collections.Generic.List<Vector3> killPositions = new System.Collections.Generic.List<Vector3>(128); // Pre-allocate with capacity
    }
    
    private KillComboData _currentCombo = new KillComboData();
    private Coroutine _comboDisplayCoroutine = null;
    
    // MEMORY LEAK FIX: Material caching to prevent instantiation every frame
    private Material _cachedDefaultMaterial;
    private Material _cachedWallhackMaterial;
    private Material _cachedNeonMaterial;
    private const int MAX_COMBO_POSITIONS = 200; // Prevent unbounded growth        [Header("Font Settings (Optional)")]
        [Tooltip("Custom TMP font asset for Combat text (Bold) - Leave empty for default")]
        [SerializeField] private TMP_FontAsset combatFont;
        [Tooltip("Custom TMP font asset for Movement text (Italic) - Leave empty for default")]
        [SerializeField] private TMP_FontAsset movementFont;
        [Tooltip("Custom TMP font asset for Tricks text (Bold Italic) - Leave empty for default")]
        [SerializeField] private TMP_FontAsset tricksFont;
        [Tooltip("Fallback font for legacy Text component - Leave empty for default")]
        [SerializeField] private Font legacyFont;
        
        [Header("TMP Effects (EXTRAORDINARY!)")]
        [Tooltip("Enable TextMeshPro effects controller for gradients, glows, shadows")]
        [SerializeField] private bool useTMPEffects = true;
        [Tooltip("TMP Effects Controller - Auto-created if null")]
        [SerializeField] private TMPEffectsController tmpEffectsController;
        
        [Header("Wallhack Effect (AAA+!)")]
        [Tooltip("Enable wallhack shader - text looks different when behind walls!")]
        [SerializeField] private bool useWallhackShader = true;
        [Tooltip("Occluded color (when behind walls) - desaturated, transparent")]
        [SerializeField] private Color occludedColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        [Tooltip("Occluded alpha multiplier")]
        [SerializeField] [Range(0f, 1f)] private float occludedAlpha = 0.4f;
        [SerializeField] private AnimationCurve floatCurve = new AnimationCurve(
            new Keyframe(0, 0, 0, 2),
            new Keyframe(1, 1, 0, 0)
        );
        [SerializeField] private AnimationCurve fadeCurve = new AnimationCurve(
            new Keyframe(0, 1, 0, 0),
            new Keyframe(1, 0, -2, 0)
        );
        
        private static FloatingTextManager _instance;
        private static bool _instanceSearched = false; // PERFORMANCE FIX: Prevent repeated FindObjectOfType calls
        
        public static FloatingTextManager Instance
        {
            get
            {
                if (_instance == null && !_instanceSearched)
                {
                    _instanceSearched = true;
                    _instance = FindObjectOfType<FloatingTextManager>();
                    if (_instance == null)
                    {
                        Debug.LogWarning("[FloatingTextManager] No instance found in scene. Floating text requests will be ignored.");
                    }
                }

                return _instance;
            }
        }

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            EnsureDependenciesReady();
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            
            // Flush any pending combos before disabling
            FlushCombo();
        }

        void OnDestroy()
        {
            // Flush any pending combos before destroying
            FlushCombo();
            
            // MEMORY LEAK FIX: Clean up cached materials
            if (_cachedDefaultMaterial != null)
            {
                Destroy(_cachedDefaultMaterial);
                _cachedDefaultMaterial = null;
            }
            if (_cachedWallhackMaterial != null)
            {
                Destroy(_cachedWallhackMaterial);
                _cachedWallhackMaterial = null;
            }
            if (_cachedNeonMaterial != null)
            {
                Destroy(_cachedNeonMaterial);
                _cachedNeonMaterial = null;
            }
            
            if (_instance == this)
            {
                _instance = null;
                _instanceSearched = false; // Reset search flag for scene reloads
            }
        }

        void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (debugMode)
            {
                Debug.Log($"[FloatingTextManager] Scene loaded: {scene.name}. Validating floating text dependencies.");
            }

            EnsureDependenciesReady();
        }

        void EnsureDependenciesReady()
        {
            EnsureWorldCanvasReady();
            EnsureFloatingTextPrefabReady();
        }

        void EnsureWorldCanvasReady()
        {
            if (worldCanvas != null)
            {
                if (debugMode && worldCanvas.renderMode != RenderMode.WorldSpace)
                {
                    Debug.LogWarning("[FloatingTextManager] Assigned canvas is not using World Space render mode.");
                }
                
                // CRITICAL: Ensure canvas renders on top of everything!
                worldCanvas.sortingOrder = 32767; // MAX sorting order
                
                return;
            }

            // Attempt to recover a canvas assigned at runtime (e.g., via additive scene loads)
            Canvas existing = GetComponentInChildren<Canvas>();
            if (existing != null)
            {
                worldCanvas = existing;
                return;
            }

            SetupDefaultCanvas();
        }

        void EnsureFloatingTextPrefabReady()
        {
            if (floatingTextPrefab == null)
            {
                CreateDefaultFloatingTextPrefab();
            }
        }

        void SetupDefaultCanvas()
        {
            if (worldCanvas != null)
            {
                if (debugMode)
                {
                    Debug.Log("[FloatingTextManager] World canvas already assigned. Skipping fallback canvas creation.");
                }
                return;
            }

            if (debugMode)
            {
                Debug.Log("[FloatingTextManager] Creating fallback world-space canvas for floating text.");
            }

            GameObject canvasGO = new GameObject("FloatingTextCanvas");
            canvasGO.transform.SetParent(transform, false);
            worldCanvas = canvasGO.AddComponent<Canvas>();
            worldCanvas.renderMode = RenderMode.WorldSpace;
            worldCanvas.sortingOrder = 32767; // MAX sorting order - render on top of EVERYTHING!

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;

            canvasGO.AddComponent<GraphicRaycaster>();
            canvasGO.transform.localScale = Vector3.one * worldScaleMultiplier;

            if (debugMode)
            {
                Debug.Log($"[FloatingTextManager] Fallback canvas created with scale {canvasGO.transform.localScale}");
            }
        }

        void CreateDefaultFloatingTextPrefab()
        {
            if (floatingTextPrefab != null)
            {
                Debug.Log("[FloatingTextManager] Prefab already exists, skipping creation");
                return;
            }
            
            Debug.Log("[FloatingTextManager] Creating default floating text prefab with TextMeshPro!");
            
            // Create TextMeshPro floating text prefab
            GameObject prefab = new GameObject("FloatingXPTextPrefab");
            
            // Add RectTransform for UI positioning
            RectTransform rectTransform = prefab.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(300, 100);
            
            // === USE TEXTMESHPRO FOR BETTER QUALITY! ===
            TextMeshPro tmpComponent = prefab.AddComponent<TextMeshPro>();
            
            // CRITICAL FIX: Use TMP's guaranteed built-in font to prevent weird symbols
            // TMP_Settings.defaultFontAsset is ALWAYS available (Arial SDF by default)
            if (TMP_Settings.defaultFontAsset != null)
            {
                tmpComponent.font = TMP_Settings.defaultFontAsset;
            }
            else
            {
                // Ultimate fallback: Try to load any TMP font from Resources
                TMP_FontAsset fallbackFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                if (fallbackFont == null)
                {
                    fallbackFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Roboto-Bold SDF");
                }
                if (fallbackFont != null)
                {
                    tmpComponent.font = fallbackFont;
                }
            }
            
            tmpComponent.fontSize = textSize;
            tmpComponent.color = Color.yellow;
            tmpComponent.alignment = TextAlignmentOptions.Center;
            tmpComponent.text = "+10 XP";
            tmpComponent.fontStyle = FontStyles.Bold;
            
            // CRITICAL: Disable text wrapping and enable overflow!
            tmpComponent.enableWordWrapping = false;
            tmpComponent.overflowMode = TextOverflowModes.Overflow;
            
            // Set large rect transform so text doesn't wrap
            rectTransform.sizeDelta = new Vector2(2000, 500); // HUGE to prevent wrapping!
            
            // Enable outline
            tmpComponent.outlineWidth = 0.3f;
            tmpComponent.outlineColor = Color.black;
            
            // CRITICAL: Render on top of EVERYTHING (through walls!)
            tmpComponent.renderer.sortingOrder = 32767;
            
            // CRITICAL: Disable depth testing - render regardless of what's in front!
            // MEMORY LEAK FIX: Cache material instead of creating new one
            if (tmpComponent.fontSharedMaterial != null && _cachedDefaultMaterial == null)
            {
                _cachedDefaultMaterial = new Material(tmpComponent.fontSharedMaterial);
                _cachedDefaultMaterial.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
                _cachedDefaultMaterial.renderQueue = 5000;
            }
            if (_cachedDefaultMaterial != null)
            {
                tmpComponent.fontSharedMaterial = _cachedDefaultMaterial;
            }
            
            floatingTextPrefab = prefab;
            prefab.SetActive(false);
            
            Debug.Log($"[FloatingTextManager] TextMeshPro prefab created with font: {tmpComponent.font?.name ?? "null"}");
        }
        
        /// <summary>
        /// Show floating XP text at a world position (backwards compatible for all systems)
        /// NOW WITH SMART COMBO AGGREGATION!
        /// </summary>
        public void ShowXPText(int xpAmount, Vector3 worldPosition)
        {
            if (!enableKillCombo)
            {
                // Legacy behavior - show immediately
                ShowFloatingText($"+{xpAmount} XP", worldPosition, Color.yellow, customSize: null, lockRotation: false, style: TextStyle.Combat);
                return;
            }
            
            // SMART AGGREGATION: Track this kill in the combo system
            float currentTime = Time.time;
            
            // Check if this is a new combo or continuation
            if (currentTime - _currentCombo.lastKillTime > comboWindow)
            {
                // New combo - display previous combo if exists, then start new
                if (_currentCombo.killCount > 0)
                {
                    DisplayCombo();
                }
                
                // Reset for new combo
                _currentCombo.killCount = 0;
                _currentCombo.totalXP = 0;
                _currentCombo.killPositions.Clear();
            }
            
            // Add this kill to the combo
            _currentCombo.killCount++;
            _currentCombo.totalXP += xpAmount;
            _currentCombo.lastKillTime = currentTime;
            _currentCombo.killPositions.Add(worldPosition);
            
            // Calculate average position - MEMORY LEAK FIX: Use for loop instead of foreach to avoid GC allocation
            Vector3 sum = Vector3.zero;
            int posCount = _currentCombo.killPositions.Count;
            for (int i = 0; i < posCount; i++)
            {
                sum += _currentCombo.killPositions[i];
            }
            _currentCombo.averagePosition = sum / posCount;
            
            // MEMORY LEAK FIX: Prevent unbounded list growth during heavy combat
            if (_currentCombo.killPositions.Count > MAX_COMBO_POSITIONS)
            {
                // Keep only recent positions, remove oldest
                int removeCount = _currentCombo.killPositions.Count - MAX_COMBO_POSITIONS;
                _currentCombo.killPositions.RemoveRange(0, removeCount);
            }
            
            // Cancel existing display coroutine and restart timer
            if (_comboDisplayCoroutine != null)
            {
                StopCoroutine(_comboDisplayCoroutine);
            }
            
            // Start new timer to display combo
            _comboDisplayCoroutine = StartCoroutine(ComboDisplayDelayCoroutine());
            
            if (debugMode)
            {
                Debug.Log($"[FloatingTextManager] Kill registered: {_currentCombo.killCount} kills, {_currentCombo.totalXP} XP total");
            }
        }
        
        /// <summary>
        /// Coroutine that waits before displaying combo (in case more kills happen)
        /// </summary>
        private IEnumerator ComboDisplayDelayCoroutine()
        {
            yield return new WaitForSeconds(comboDisplayDelay);
            
            // Check if combo window has passed
            if (Time.time - _currentCombo.lastKillTime >= comboDisplayDelay)
            {
                DisplayCombo();
            }
        }
        
        /// <summary>
        /// Display the accumulated combo with multipliers and style
        /// </summary>
        private void DisplayCombo()
        {
            if (_currentCombo.killCount == 0) return;
            
            int killCount = _currentCombo.killCount;
            int baseXP = _currentCombo.totalXP;
            
            // Calculate multiplier based on kill count
            float multiplier = GetComboMultiplier(killCount);
            int bonusXP = Mathf.RoundToInt(baseXP * (multiplier - 1f)); // Bonus XP from multiplier
            int totalXP = baseXP + bonusXP;
            
            // GRANT BONUS XP TO PLAYER! (Base XP was already granted by XPHooks)
            if (bonusXP > 0 && GeminiGauntlet.Progression.XPManager.Instance != null)
            {
                GeminiGauntlet.Progression.XPManager.Instance.GrantXP(bonusXP, "Combat Combos", $"{killCount}x Kill Combo Bonus");
                
                if (debugMode)
                {
                    Debug.Log($"[FloatingTextManager] 🎁 BONUS XP GRANTED: {bonusXP} XP for {killCount}x combo (x{multiplier:F1} multiplier)");
                }
            }
            
            // Get combo tier and styling
            ComboTier tier = GetComboTier(killCount);
            Color comboColor = GetComboColor(tier);
            float sizeMultiplier = GetComboSizeMultiplier(tier);
            
            // Build display text
            string displayText = BuildComboText(killCount, totalXP, bonusXP, multiplier);
            
            // Show with enhanced styling
            int customSize = Mathf.RoundToInt(textSize * sizeMultiplier);
            ShowFloatingText(displayText, _currentCombo.averagePosition, comboColor, customSize, false, TextStyle.Combat);
            
            if (debugMode)
            {
                Debug.Log($"[FloatingTextManager] 💥 COMBO DISPLAYED: {killCount} kills, {totalXP} XP (base: {baseXP}, bonus: {bonusXP}, x{multiplier:F1})");
            }
            
            // Reset combo
            _currentCombo.killCount = 0;
            _currentCombo.totalXP = 0;
            _currentCombo.killPositions.Clear();
            _comboDisplayCoroutine = null;
        }
        
        /// <summary>
        /// Combo tiers for styling
        /// </summary>
        private enum ComboTier
        {
            Single = 0,      // 1-4 kills
            Small = 1,       // 5-9 kills
            Medium = 2,      // 10-19 kills
            Large = 3,       // 20-39 kills
            Epic = 4         // 40+ kills
        }
        
        /// <summary>
        /// Get combo tier based on kill count
        /// </summary>
        private ComboTier GetComboTier(int killCount)
        {
            if (killCount >= 40) return ComboTier.Epic;
            if (killCount >= 20) return ComboTier.Large;
            if (killCount >= 10) return ComboTier.Medium;
            if (killCount >= 5) return ComboTier.Small;
            return ComboTier.Single;
        }
        
        /// <summary>
        /// Get multiplier based on kill count
        /// </summary>
        private float GetComboMultiplier(int killCount)
        {
            if (killCount >= 40 && comboMultipliers.Length > 3) return comboMultipliers[3]; // 5.0x
            if (killCount >= 20 && comboMultipliers.Length > 2) return comboMultipliers[2]; // 3.0x
            if (killCount >= 10 && comboMultipliers.Length > 1) return comboMultipliers[1]; // 2.0x
            if (killCount >= 5 && comboMultipliers.Length > 0) return comboMultipliers[0];  // 1.5x
            return 1.0f; // No multiplier for 1-4 kills
        }
        
        /// <summary>
        /// Get size multiplier based on combo tier
        /// </summary>
        private float GetComboSizeMultiplier(ComboTier tier)
        {
            int index = (int)tier;
            if (index >= 0 && index < comboSizeMultipliers.Length)
            {
                return comboSizeMultipliers[index];
            }
            return 1.0f;
        }
        
        /// <summary>
        /// Get color based on combo tier
        /// </summary>
        private Color GetComboColor(ComboTier tier)
        {
            switch (tier)
            {
                case ComboTier.Single:
                    return Color.yellow; // Standard yellow
                case ComboTier.Small:
                    return new Color(1f, 0.65f, 0f); // Orange
                case ComboTier.Medium:
                    return new Color(1f, 0.2f, 0.2f); // Red
                case ComboTier.Large:
                    return new Color(0.8f, 0.2f, 1f); // Purple
                case ComboTier.Epic:
                    return new Color(1f, 0.84f, 0f); // Gold
                default:
                    return Color.yellow;
            }
        }
        
        /// <summary>
        /// Build display text with combo information
        /// </summary>
        private string BuildComboText(int killCount, int totalXP, int bonusXP, float multiplier)
        {
            if (killCount <= 4)
            {
                // Small kills - simple display
                return $"+{totalXP} XP";
            }
            else if (killCount < 10)
            {
                // 5-9 kills - show multiplier (cleaner format)
                return $"+{totalXP} XP  x{multiplier:F1}";
            }
            else if (killCount < 20)
            {
                // 10-19 kills - emphasize
                return $"+{totalXP} XP\n{killCount}x COMBO!";
            }
            else if (killCount < 40)
            {
                // 20-39 kills - impressive!
                return $"+{totalXP} XP\n💥 {killCount}x COMBO 💥";
            }
            else
            {
                // 40+ kills - LEGENDARY (but not too flashy)
                return $"+{totalXP} XP\n🔥 {killCount}x LEGENDARY 🔥";
            }
        }
        
        /// <summary>
        /// Manually flush/display the current combo (useful for end of combat or scene transitions)
        /// </summary>
        public void FlushCombo()
        {
            if (_comboDisplayCoroutine != null)
            {
                StopCoroutine(_comboDisplayCoroutine);
                _comboDisplayCoroutine = null;
            }
            
            DisplayCombo();
        }
        
        /// <summary>
        /// BACKWARDS COMPATIBLE: Simple 3-parameter overload for legacy systems
        /// </summary>
        public void ShowFloatingText(string text, Vector3 worldPosition, Color color)
        {
            // Call the full method with default parameters
            ShowFloatingText(text, worldPosition, color, customSize: null, lockRotation: false, style: TextStyle.Combat);
        }
        
        /// <summary>
        /// Show text with duration and size multiplier (for ComboMultiplierSystem integration)
        /// Duration parameter is currently mapped to existing floatDuration field (not customizable per-call yet)
        /// </summary>
        public void ShowText(string text, Vector3 worldPosition, Color color, float duration, float sizeMultiplier)
        {
            // Calculate custom size based on multiplier
            int customSize = Mathf.RoundToInt(textSize * sizeMultiplier);
            
            // Call existing method with Combat style (combo feedback)
            // Note: Duration parameter is acknowledged but uses default floatDuration for consistency
            // Future enhancement: Could implement per-instance duration by modifying FloatTextCoroutine
            ShowFloatingText(text, worldPosition, color, customSize, lockRotation: true, style: TextStyle.Combat);
        }
        
        /// <summary>
        /// Text style for different XP systems
        /// </summary>
        public enum TextStyle
        {
            Combat,      // Killing enemies - Bold, aggressive
            Movement,    // Wall jumps - Italic, dynamic
            Tricks       // Aerial tricks - Bold Italic, extraordinary
        }
        
        /// <summary>
        /// Show custom floating text at a world position with style
        /// </summary>
        public void ShowFloatingText(string text, Vector3 worldPosition, Color color, int? customSize = null, bool lockRotation = false, TextStyle style = TextStyle.Movement, float? customDuration = null)
        {
            // Ensure we have the necessary components - create them if missing
            EnsureDependenciesReady();
            
            if (floatingTextPrefab == null || worldCanvas == null)
            {
                Debug.LogError("[FloatingTextManager] Still missing prefab or canvas after setup attempt");
                return;
            }
            
            if (debugMode)
            {
                Debug.Log($"[FloatingTextManager] Creating floating text '{text}' at world position: {worldPosition}");
                Debug.Log($"[FloatingTextManager] Canvas scale: {worldCanvas.transform.localScale}");
                Debug.Log($"[FloatingTextManager] World scale multiplier: {worldScaleMultiplier}");
            }
            
            // Instantiate the floating text
            GameObject textInstance = Instantiate(floatingTextPrefab, worldCanvas.transform);
            textInstance.SetActive(true);
            
            // CRITICAL: Scale up the text object itself for distant visibility (10-15x bigger!)
            textInstance.transform.localScale = Vector3.one * 12f; // 12x scale = HUGE text!
            
            // Try TextMeshPro first (better rendering!), fallback to Text
            TextMeshPro tmpComponent = textInstance.GetComponent<TextMeshPro>();
            Text textComponent = textInstance.GetComponent<Text>();
            
            if (tmpComponent != null)
            {
                // === TEXTMESHPRO PATH (BETTER!) ===
                tmpComponent.text = text;
                tmpComponent.color = color;
                tmpComponent.fontSize = customSize ?? textSize;
                
                // CRITICAL: Ensure no wrapping!
                tmpComponent.enableWordWrapping = false;
                tmpComponent.overflowMode = TextOverflowModes.Overflow;
                
                // Ensure rect is large enough
                RectTransform rect = tmpComponent.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.sizeDelta = new Vector2(2000, 500);
                }
                
                // Apply style and custom font based on system
                switch (style)
                {
                    case TextStyle.Combat:
                        // COMBAT: Bold, aggressive, sharp
                        tmpComponent.fontStyle = FontStyles.Bold;
                        tmpComponent.outlineWidth = 0.3f;
                        tmpComponent.outlineColor = Color.black;
                        // Apply custom font if assigned
                        if (combatFont != null)
                        {
                            tmpComponent.font = combatFont;
                        }
                        break;
                        
                    case TextStyle.Movement:
                        // MOVEMENT: Italic, dynamic, flowing
                        tmpComponent.fontStyle = FontStyles.Italic;
                        tmpComponent.outlineWidth = 0.25f;
                        tmpComponent.outlineColor = new Color(0, 0, 0, 0.8f);
                        // Apply custom font if assigned
                        if (movementFont != null)
                        {
                            tmpComponent.font = movementFont;
                        }
                        break;
                        
                    case TextStyle.Tricks:
                        // TRICKS: Bold Italic, extraordinary, flashy!
                        tmpComponent.fontStyle = FontStyles.Bold | FontStyles.Italic;
                        tmpComponent.outlineWidth = 0.35f;
                        tmpComponent.outlineColor = Color.black;
                        // Apply custom font if assigned
                        if (tricksFont != null)
                        {
                            tmpComponent.font = tricksFont;
                        }
                        break;
                }
                
                // Render on top
                tmpComponent.renderer.sortingOrder = 32767;
                
                // === WALLHACK SHADER (AAA+!) ===
                // MEMORY LEAK FIX: Cache wallhack material instead of creating new one every time
                // NOTE: Apply wallhack shader BEFORE TMPEffectsController to avoid material conflicts
                if (useWallhackShader)
                {
                    // Load wallhack shader
                    Shader wallhackShader = Shader.Find("Custom/XPTextWallhack");
                    if (wallhackShader != null)
                    {
                        // Create cached material only once
                        if (_cachedWallhackMaterial == null)
                        {
                            _cachedWallhackMaterial = new Material(wallhackShader);
                            _cachedWallhackMaterial.SetTexture("_MainTex", tmpComponent.font.atlas);
                            _cachedWallhackMaterial.SetColor("_OutlineColor", Color.black);
                            _cachedWallhackMaterial.SetFloat("_OutlineWidth", 0.3f);
                            
                            // Occluded appearance (behind walls)
                            _cachedWallhackMaterial.SetColor("_OccludedColor", occludedColor);
                            _cachedWallhackMaterial.SetColor("_OccludedOutlineColor", new Color(0.3f, 0.3f, 0.3f, 0.5f));
                            _cachedWallhackMaterial.SetFloat("_OccludedAlpha", occludedAlpha);
                            _cachedWallhackMaterial.SetFloat("_GlowPower", 2.0f);
                            _cachedWallhackMaterial.renderQueue = 5000;
                        }
                        
                        // Update per-instance color properties
                        _cachedWallhackMaterial.SetColor("_Color", color);
                        _cachedWallhackMaterial.SetColor("_FaceColor", color);
                        _cachedWallhackMaterial.SetColor("_GlowColor", color * 1.5f);
                        
                        tmpComponent.fontSharedMaterial = _cachedWallhackMaterial;
                        
                        if (debugMode)
                        {
                            Debug.Log("[FloatingTextManager] Applied cached wallhack shader!");
                        }
                    }
                    else
                    {
                        // Fallback to cached default material
                        if (_cachedDefaultMaterial != null)
                        {
                            tmpComponent.fontSharedMaterial = _cachedDefaultMaterial;
                        }
                    }
                }
                else
                {
                    // Standard always-on-top rendering using cached material
                    if (_cachedDefaultMaterial != null)
                    {
                        tmpComponent.fontSharedMaterial = _cachedDefaultMaterial;
                    }
                }
                
                // === APPLY EXTRAORDINARY TMP EFFECTS! ===
                // Apply AFTER material setup so effects enhance the cached material
                if (useTMPEffects && tmpComponent.fontSharedMaterial != null)
                {
                    // Ensure effects controller exists
                    if (tmpEffectsController == null)
                    {
                        tmpEffectsController = GetComponent<TMPEffectsController>();
                        if (tmpEffectsController == null)
                        {
                            tmpEffectsController = gameObject.AddComponent<TMPEffectsController>();
                        }
                    }
                    
                    // Apply ALL the cool effects to the cached material!
                    tmpEffectsController.ApplyEffects(tmpComponent, style);
                }
                
                if (debugMode)
                {
                    Debug.Log($"[FloatingTextManager] TextMeshPro configured: style={style}, fontSize={tmpComponent.fontSize}, customFont={tmpComponent.font != null}, effects={useTMPEffects}");
                }
            }
            else if (textComponent != null)
            {
                // === LEGACY TEXT PATH (FALLBACK) ===
                textComponent.text = text;
                textComponent.color = color;
                textComponent.fontSize = customSize ?? textSize;
                
                // Apply custom font if assigned
                if (legacyFont != null)
                {
                    textComponent.font = legacyFont;
                }
                
                // Apply style
                switch (style)
                {
                    case TextStyle.Combat:
                        textComponent.fontStyle = FontStyle.Bold;
                        break;
                    case TextStyle.Movement:
                        textComponent.fontStyle = FontStyle.Italic;
                        break;
                    case TextStyle.Tricks:
                        textComponent.fontStyle = FontStyle.BoldAndItalic;
                        break;
                }
                
                // CRITICAL: Make text render on top of EVERYTHING with NEON GLOW!
                // MEMORY LEAK FIX: Cache neon material instead of creating new one every time
                Shader neonShader = Shader.Find("Custom/XPTextNeonGlow");
                if (neonShader != null)
                {
                    // Create cached material only once
                    if (_cachedNeonMaterial == null)
                    {
                        _cachedNeonMaterial = new Material(neonShader);
                        _cachedNeonMaterial.SetFloat("_GlowIntensity", 1.5f);
                        _cachedNeonMaterial.SetFloat("_GlowSize", 0.25f);
                        _cachedNeonMaterial.SetFloat("_EmissionStrength", 2.0f);
                        _cachedNeonMaterial.SetFloat("_EmissionPulse", 1.5f);
                        _cachedNeonMaterial.SetFloat("_EmissionPulseAmount", 0.2f);
                        _cachedNeonMaterial.SetFloat("_EdgeSmoothness", 0.15f);
                        _cachedNeonMaterial.renderQueue = 5000;
                    }
                    
                    // Update per-instance properties
                    _cachedNeonMaterial.SetTexture("_MainTex", textComponent.mainTexture);
                    _cachedNeonMaterial.SetColor("_Color", color);
                    _cachedNeonMaterial.SetColor("_GlowColor", color * 1.5f);
                    
                    textComponent.material = _cachedNeonMaterial;
                }
                else
                {
                    // Fallback: Use instance material (unavoidable allocation for legacy Text)
                    textComponent.material = new Material(textComponent.material);
                    textComponent.material.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
                    textComponent.material.renderQueue = 5000;
                }
                
                if (debugMode)
                {
                    Debug.Log($"[FloatingTextManager] Text configured: style={style}, fontSize={textSize}");
                }
            }
            
            // Position it above the world position (scaled for large worlds)
            Vector3 startPosition = worldPosition + Vector3.up * (50f * worldScaleMultiplier / 50f); // Scale the height offset
            textInstance.transform.position = startPosition;
            
            if (debugMode)
            {
                Debug.Log($"[FloatingTextManager] Text positioned at: {startPosition}");
                Debug.Log($"[FloatingTextManager] Distance from camera: {Vector3.Distance(startPosition, Camera.main != null ? Camera.main.transform.position : Vector3.zero)}");
            }
            
            // Make it face the camera
            if (Camera.main != null)
            {
                textInstance.transform.LookAt(Camera.main.transform);
                textInstance.transform.Rotate(0, 180, 0); // Flip to face camera correctly
                
                if (debugMode)
                {
                    Debug.Log($"[FloatingTextManager] Text oriented to face camera at: {Camera.main.transform.position}");
                }
            }
            else
            {
                Debug.LogWarning("[FloatingTextManager] No main camera found for text orientation");
            }
            
            // Start the floating animation
            float duration = customDuration ?? floatDuration; // Use custom duration if provided
            StartCoroutine(FloatTextCoroutine(textInstance, startPosition, lockRotation, duration));
        }
        
        IEnumerator FloatTextCoroutine(GameObject textObject, Vector3 startPosition, bool lockRotation = false, float duration = -1f)
        {
            // Get both text component types (support TMP and legacy Text)
            TextMeshPro tmpComponent = textObject.GetComponent<TextMeshPro>();
            Text textComponent = textObject.GetComponent<Text>();
            
            // Use provided duration or default
            if (duration <= 0) duration = floatDuration;
            
            Vector3 endPosition = startPosition + Vector3.up * floatHeight;
            float elapsedTime = 0f;
            
            while (elapsedTime < duration)
            {
                float progress = elapsedTime / duration;
                
                // Animate position
                float heightProgress = floatCurve.Evaluate(progress);
                textObject.transform.position = Vector3.Lerp(startPosition, endPosition, heightProgress);
                
                // Animate fade (support both TMP and legacy Text!)
                float alphaValue = fadeCurve.Evaluate(progress);
                
                if (tmpComponent != null)
                {
                    Color color = tmpComponent.color;
                    color.a = alphaValue;
                    tmpComponent.color = color;
                }
                else if (textComponent != null)
                {
                    Color color = textComponent.color;
                    color.a = alphaValue;
                    textComponent.color = color;
                }
                
                // Keep facing camera (ONLY if not locked - performance!)
                if (!lockRotation && Camera.main != null)
                {
                    textObject.transform.LookAt(Camera.main.transform);
                    textObject.transform.Rotate(0, 180, 0);
                }
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // Clean up
            Destroy(textObject);
        }
        
        #if UNITY_EDITOR
        void OnValidate()
        {
            if (floatDuration <= 0) floatDuration = 0.5f;
            if (floatHeight <= 0) floatHeight = 2f;
        }
        #endif
    }
}