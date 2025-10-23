// --- ENHANCED CognitiveFeedManager.cs - THE GEM OF THE GAME ---
using UnityEngine;
using GeminiGauntlet.Audio;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// 🧠 ENHANCED COGNITIVE FEEDBACK MANAGER
/// 
/// This is the GEM of the game - an incredibly intelligent inner voice that:
/// • Observes EVERYTHING happening in the game
/// • Provides contextual, personality-driven commentary  
/// • Has memory and learns from player behavior
/// • Uses advanced typewriter effects with realistic typing patterns
/// • Gives item analysis and strategic advice
/// • Adapts personality based on player actions
/// 
/// Makes players think: "WTF how does he even do this?! So accurate!"
/// </summary>
public class CognitiveFeedManagerEnhanced : MonoBehaviour
{
    public static CognitiveFeedManagerEnhanced Instance { get; private set; }

    [Header("🎭 Core UI References")]
    public TextMeshProUGUI cognitiveText;
    public CanvasGroup cognitivePanelCanvasGroup;
    public TextMeshProUGUI persistentMessageText;
    public CanvasGroup persistentMessagePanelCanvasGroup;
    
    [Header("📊 LIVE PERFORMANCE INTEGRATION")]
    [SerializeField] private bool enableLiveDataIntegration = true;
    [SerializeField] private float liveDataUpdateRate = 0.1f; // Update 10 times per second
    [SerializeField] private bool prioritizeLiveData = true; // Live data overrides regular messages

    [Header("🎪 Advanced Typewriter System")]
    [SerializeField] private float baseTypingSpeed = 120f; // Characters per second - MUCH FASTER TYPING
    [SerializeField] private float fastTypingSpeed = 150f; // When excited/urgent - BLAZING FAST
    [SerializeField] private float slowTypingSpeed = 80f;  // When thinking/serious - STILL FAST
    
    [SerializeField] private float punctuationPause = 0.05f; // Minimal pauses
    [SerializeField] private float commaPause = 0.03f;
    [SerializeField] private float periodPause = 0.08f;
    [SerializeField] private float exclamationPause = 0.06f;
    [SerializeField] private float ellipsisPause = 0.12f;
    [SerializeField] private float dashPause = 0.04f;
    
    [Header("🎨 Visual Effects")]
    [SerializeField] private float glitchChance = 0.02f; // 2% chance per character
    [SerializeField] private Color[] emotionColors = {
        Color.white,    // Normal
        Color.green,    // Happy/Success
        Color.yellow,   // Warning/Caution
        Color.red,      // Danger/Angry
        Color.cyan,     // Analytical/Curious
        Color.magenta   // Excited/Surprised
    };
    
    [Header("🧠 Personality System")]
    [SerializeField] private float curiosityLevel = 0.5f;
    [SerializeField] private float analyticalLevel = 0.7f;
    [SerializeField] private float sarcasmLevel = 0.3f;
    [SerializeField] private float supportLevel = 0.8f;
    [SerializeField] private float urgencyLevel = 0.4f;

    [Header("📊 Learning & Memory")]
    [SerializeField] private int maxMemoryEntries = 100;
    [SerializeField] private float playerAnalysisInterval = 30f; // Analyze player every 30 seconds

    // === INTERNAL STATE ===
    private AudioSource audioSource;
    private Queue<CognitiveMessage> messageQueue = new Queue<CognitiveMessage>();
    private Coroutine displayCoroutine;
    private Coroutine persistentMessageCoroutine;
    
    // Memory & Learning System
    private Dictionary<string, float> playerBehaviorScores = new Dictionary<string, float>();
    private List<CognitiveMemory> memories = new List<CognitiveMemory>();
    private CognitivePersonality personality;
    private PlayerAnalytics analytics;
    
    // Game State Tracking
    private float lastHealthCheckTime;
    private float lastInventoryCheckTime;
    private int consecutiveDeaths = 0;
    private int gemsCollectedThisSession = 0;
    private bool isInCombat = false;
    private bool isObservingItem = false;
    private ChestItemData lastObservedItem;
    
    // Performance tracking
    private Dictionary<string, int> actionCounts = new Dictionary<string, int>();
    
    // Wall Jump & Aerial Trick monitoring
    private int lastWallJumpChainLevel = 0;
    private int lastTrickCount = 0;
    private float lastPerformanceCheckTime = 0f;
    private float lastWallJumpChainStartTime = 0f; // Track chain timing for real numeric data
    private const float PERFORMANCE_CHECK_COOLDOWN = 0.5f; // Check every 0.5s
    
    // ========================================
    // 🎯 CLEAN STATE MACHINE - NO CONFLICTS!
    // ========================================
    private enum CognitiveState
    {
        Idle,                    // Nothing happening
        ShowingItemInfo,         // Inventory hover
        ShowingWallJumpLive,     // Wall jump in progress
        ShowingWallJumpSummary,  // Wall jump just ended
        ShowingTrickLive,        // Trick in progress  
        ShowingTrickSummary      // Trick just ended
    }
    
    private CognitiveState currentState = CognitiveState.Idle;
    private string currentDisplayText = "";
    private float stateStartTime = 0f;
    private const float SUMMARY_DISPLAY_DURATION = 3f; // Show summaries for 3 seconds
    
    // State-specific data
    private string lastWallJumpSummary = "";
    private string lastTrickSummary = "";
    private int lastWallJumpChain = 0;
    
    // PERFORMANCE CACHES (potato-friendly!) - CHARACTERCONTROLLER EDITION
    private CharacterController playerCharacterController;
    private Transform playerTransform;
    private WallJumpXPSimple wallJumpSystem;
    private AerialTrickXPSystem aerialTrickSystem;
    private ComboMultiplierSystem comboSystem;
    private InventoryManager inventoryManager;
    private AAACameraController aaaCameraController; // REAL camera effects data (tilt, visual tricks)
    private AAAMovementController aaaMovementController; // REAL movement data source (wall jump chains, speed)
    private bool hasCachedReferences = false;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            audioSource = gameObject.AddComponent<AudioSource>();
            InitializePersonalitySystem();
            InitializeAnalytics();
            
            // Fix: Only apply DontDestroyOnLoad to root GameObjects
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        else 
        { 
            Destroy(gameObject); 
        }
    }

    void Start()
    {
        ValidateReferences();
        InitializeUI();
        // 🔥 ESSENTIALS ONLY: No welcome messages, no continuous analysis
        // Only inventory hover + wall jump/tricks live data!
    }

    void OnEnable()
    {
        SubscribeToGameEvents();
    }

    void OnDisable()
    {
        UnsubscribeFromGameEvents();
    }
    
    void Update()
    {
        // Cache references once
        if (!hasCachedReferences)
        {
            CachePerformanceReferences();
        }
        
        // 🎯 CLEAN STATE MACHINE UPDATE
        UpdateStateMachine();
    }
    
    /// <summary>
    /// 🎯 CLEAN STATE MACHINE - Handles exactly 4 core functionalities without conflicts
    /// </summary>
    private void UpdateStateMachine()
    {
        // Check if inventory is open - BLOCKS everything
        if (inventoryManager != null && inventoryManager.IsInventoryVisible())
        {
            // Only allow inventory hover in this mode
            if (currentState != CognitiveState.ShowingItemInfo)
            {
                TransitionToState(CognitiveState.Idle);
            }
            return;
        }
        
        // Priority 1: Check for wall jump (highest priority)
        if (IsInWallJump())
        {
            if (currentState != CognitiveState.ShowingWallJumpLive)
            {
                TransitionToState(CognitiveState.ShowingWallJumpLive);
            }
            UpdateWallJumpDisplay();
            return;
        }
        
        // Wall jump just ended - show summary
        if (currentState == CognitiveState.ShowingWallJumpLive)
        {
            TransitionToState(CognitiveState.ShowingWallJumpSummary);
            return;
        }
        
        // Priority 2: Check for aerial trick
        if (IsInAerialTrick())
        {
            if (currentState != CognitiveState.ShowingTrickLive)
            {
                TransitionToState(CognitiveState.ShowingTrickLive);
            }
            UpdateTrickDisplay();
            return;
        }
        
        // Trick just ended - show summary
        if (currentState == CognitiveState.ShowingTrickLive)
        {
            TransitionToState(CognitiveState.ShowingTrickSummary);
            return;
        }
        
        // Check if summaries have expired
        if (currentState == CognitiveState.ShowingWallJumpSummary || currentState == CognitiveState.ShowingTrickSummary)
        {
            if (Time.time - stateStartTime > SUMMARY_DISPLAY_DURATION)
            {
                TransitionToState(CognitiveState.Idle);
            }
            return;
        }
        
        // If not showing anything and not in any performance state, go idle
        if (currentState != CognitiveState.ShowingItemInfo && currentState != CognitiveState.Idle)
        {
            TransitionToState(CognitiveState.Idle);
        }
    }
    
    /// <summary>
    /// Clean state transition handler
    /// </summary>
    private void TransitionToState(CognitiveState newState)
    {
        if (currentState == newState) return;
        
        Debug.Log($"[CognitiveFeed] State transition: {currentState} -> {newState}");
        
        currentState = newState;
        stateStartTime = Time.time;
        
        // Handle state-specific transitions
        switch (newState)
        {
            case CognitiveState.Idle:
                HideDisplay();
                break;
                
            case CognitiveState.ShowingItemInfo:
                // Handled by OnItemHoverStart
                break;
                
            case CognitiveState.ShowingWallJumpLive:
                // Will update in UpdateWallJumpDisplay()
                ShowDisplay();
                break;
                
            case CognitiveState.ShowingWallJumpSummary:
                ShowDisplay();
                DisplayText(lastWallJumpSummary);
                break;
                
            case CognitiveState.ShowingTrickLive:
                // Will update in UpdateTrickDisplay()
                ShowDisplay();
                break;
                
            case CognitiveState.ShowingTrickSummary:
                ShowDisplay();
                DisplayText(lastTrickSummary);
                break;
        }
    }
    
    /// <summary>
    /// Check if player is currently in wall jump
    /// 🎯 COHERENCE: Uses proper public accessors instead of reflection
    /// </summary>
    private bool IsInWallJump()
    {
        if (wallJumpSystem == null || aaaMovementController == null) return false;
        
        // ✅ COHERENCE: Use public accessors (no reflection!)
        int chainLevel = wallJumpSystem.CurrentChainLevel;
        float lastJumpTime = wallJumpSystem.LastWallJumpTime;
        
        // Consider in wall jump if chain active and recent (within 2 seconds)
        return chainLevel > 0 && (Time.time - lastJumpTime) < 2f;
    }
    
    /// <summary>
    /// Check if player is performing aerial trick
    /// </summary>
    private bool IsInAerialTrick()
    {
        return aaaCameraController != null && aaaCameraController.IsPerformingAerialTricks;
    }
    
    /// <summary>
    /// Update wall jump live display
    /// 🎯 COHERENCE: Uses proper public accessors instead of reflection
    /// </summary>
    private void UpdateWallJumpDisplay()
    {
        if (wallJumpSystem == null || aaaMovementController == null) return;
        
        // ✅ COHERENCE: Use public accessors (no reflection!)
        int chainLevel = wallJumpSystem.CurrentChainLevel;
        float lastJumpTime = wallJumpSystem.LastWallJumpTime;
        float timeSinceLastJump = Time.time - lastJumpTime;
        float speed = aaaMovementController.CurrentSpeed;
        
        // Build live display
        string display = $"<color=#FF6600>WALL JUMP CHAIN x{chainLevel}</color>\n";
        display += $"<color=#4ECDC4>SPEED: {speed:F1} m/s</color> | <color=#00FFFF>TIME: {timeSinceLastJump:F1}s</color>";
        
        // Add camera tilt if available
        if (aaaCameraController != null)
        {
            // ✅ COHERENCE: Use public accessor (no reflection!)
            float tilt = aaaCameraController.WallJumpTiltAmount;
            if (Mathf.Abs(tilt) > 0.1f)
            {
                string dir = tilt > 0 ? "R" : "L";
                display += $" | <color=#FFD700>TILT {dir}{Mathf.Abs(tilt):F1}°</color>";
            }
        }
        
        // Save as summary for when chain ends
        lastWallJumpChain = chainLevel;
        lastWallJumpSummary = $"<color=#FF6600>WALL JUMP COMPLETE!</color>\n";
        lastWallJumpSummary += $"<color=#FFD700>Chain: x{chainLevel}</color> | <color=#4ECDC4>Speed: {speed:F1} m/s</color>";
        
        DisplayText(display);
    }
    
    /// <summary>
    /// Update aerial trick live display
    /// </summary>
    private void UpdateTrickDisplay()
    {
        if (aaaCameraController == null || aaaMovementController == null) return;
        
        Vector3 trickRotations = aaaCameraController.GetTrickRotations();
        float rotationSpeed = aaaCameraController.GetTrickRotationSpeed;
        Vector3 velocity = aaaMovementController.Velocity;
        float speed = velocity.magnitude;
        
        // Calculate total rotation
        float totalRotation = Mathf.Sqrt(trickRotations.x * trickRotations.x + 
                                        trickRotations.y * trickRotations.y + 
                                        trickRotations.z * trickRotations.z);
        
        // Build live display
        string display = $"<color=#FF00FF>AERIAL TRICK IN PROGRESS</color>\n";
        display += $"<color=#FFD700>ROTATION: {totalRotation:F0}°</color> | <color=#4ECDC4>SPEED: {speed:F1} m/s</color>";
        
        string verticalState = velocity.y > 0.5f ? "UP" : velocity.y < -0.5f ? "DOWN" : "FLAT";
        display += $" | <color=#AA96DA>{verticalState}</color>";
        
        // Save as summary for when trick ends
        lastTrickSummary = $"<color=#FF00FF>TRICK COMPLETE!</color>\n";
        lastTrickSummary += $"<color=#FFD700>ROTATION: {totalRotation:F0}°</color> | <color=#4ECDC4>SPEED: {speed:F1} m/s</color> | <color=#AA96DA>{verticalState}</color>";
        
        DisplayText(display);
    }
    
    /// <summary>
    /// Show the display panel
    /// </summary>
    private void ShowDisplay()
    {
        if (cognitivePanelCanvasGroup != null)
        {
            cognitivePanelCanvasGroup.alpha = 1f;
        }
    }
    
    /// <summary>
    /// Hide the display panel
    /// </summary>
    private void HideDisplay()
    {
        if (cognitivePanelCanvasGroup != null)
        {
            cognitivePanelCanvasGroup.alpha = 0f;
        }
        if (cognitiveText != null)
        {
            cognitiveText.text = "";
        }
    }
    
    /// <summary>
    /// Display text in the panel
    /// </summary>
    private void DisplayText(string text)
    {
        if (cognitiveText != null)
        {
            cognitiveText.text = text;
        }
    }
    
    /// <summary>
    /// PUBLIC API: Show a quick notification that auto-dismisses
    /// Used by external systems like SessionMarkerSystem
    /// </summary>
    public void ShowQuickNotification(string message, float duration = 2f)
    {
        // Stop any existing notification
        if (displayCoroutine != null)
        {
            StopCoroutine(displayCoroutine);
        }
        
        // Start new notification
        displayCoroutine = StartCoroutine(ShowQuickNotificationCoroutine(message, duration));
    }
    
    /// <summary>
    /// Coroutine to display and auto-hide notification
    /// </summary>
    private IEnumerator ShowQuickNotificationCoroutine(string message, float duration)
    {
        // Show the panel
        if (cognitivePanelCanvasGroup != null)
        {
            cognitivePanelCanvasGroup.alpha = 1f;
        }
        
        // Display the message
        if (cognitiveText != null)
        {
            cognitiveText.text = message;
        }
        
        // Wait for duration
        yield return new WaitForSeconds(duration);
        
        // Hide the panel
        if (cognitivePanelCanvasGroup != null)
        {
            cognitivePanelCanvasGroup.alpha = 0f;
        }
        
        if (cognitiveText != null)
        {
            cognitiveText.text = "";
        }
        
        displayCoroutine = null;
    }

    #region INITIALIZATION

    private void InitializePersonalitySystem()
    {
        personality = new CognitivePersonality
        {
            curiosity = curiosityLevel,
            analytical = analyticalLevel,
            sarcasm = sarcasmLevel,
            support = supportLevel,
            urgency = urgencyLevel,
            adaptability = 0.6f
        };
        
        // Initialize behavior tracking
        playerBehaviorScores["exploration"] = 0f;
        playerBehaviorScores["aggression"] = 0f;
        playerBehaviorScores["caution"] = 0f;
        playerBehaviorScores["efficiency"] = 0f;
        playerBehaviorScores["experimentation"] = 0f;
    }

    private void InitializeAnalytics()
    {
        analytics = new PlayerAnalytics();
    }

    private void ValidateReferences()
    {
        if (cognitiveText == null || cognitivePanelCanvasGroup == null)
        {
            Debug.LogError("❌ CognitiveFeedManagerEnhanced: Missing critical UI references!");
            enabled = false;
            return;
        }
        
        // Live data integration uses existing cognitiveText - no extra components needed!
    }
    
    /// <summary>
    /// 🎯 COHERENCE: Cache all references through GameManager - ZERO FindObjectOfType calls!
    /// This eliminates the reflection anti-pattern and provides type-safe, performant access.
    /// </summary>
    private void CachePerformanceReferences()
    {
        // 🎯 COHERENCE: Use GameManager for ALL system references
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[🧠 CognitiveFeed] GameManager not available - falling back to singleton pattern");
            
            // Fallback to singleton pattern if GameManager not available
            wallJumpSystem = WallJumpXPSimple.Instance;
            aerialTrickSystem = AerialTrickXPSystem.Instance;
            comboSystem = ComboMultiplierSystem.Instance;
            inventoryManager = InventoryManager.Instance;
            aaaCameraController = FindFirstObjectByType<AAACameraController>();
            aaaMovementController = FindFirstObjectByType<AAAMovementController>();
        }
        else
        {
            // ✅ COHERENCE: Single source of truth - GameManager
            wallJumpSystem = GameManager.Instance.GetWallJumpXPSystem();
            aerialTrickSystem = GameManager.Instance.GetAerialTrickSystem();
            comboSystem = GameManager.Instance.GetComboSystem();
            inventoryManager = GameManager.Instance.GetInventoryManager();
            aaaCameraController = GameManager.Instance.GetAAACameraController();
            aaaMovementController = GameManager.Instance.GetAAAMovementController();
        }
        
        // Cache player references
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerCharacterController = playerObj.GetComponent<CharacterController>();
        }
        
        hasCachedReferences = true;
        Debug.Log($"[🧠 CognitiveFeed] ✅ COHERENCE: References cached via {(GameManager.Instance != null ? "GameManager" : "Fallback")}");
    }

    private void InitializeUI()
    {
        cognitivePanelCanvasGroup.alpha = 0;
        if (persistentMessagePanelCanvasGroup != null)
            persistentMessagePanelCanvasGroup.alpha = 0;
    }

    #endregion

    #region EVENT SUBSCRIPTIONS

    private void SubscribeToGameEvents()
    {
        // Movement & Platform Events
        PlayerMovementManager.OnPlayerLandedOnNewPlatform += OnPlatformLanded;
        
        // Inventory & Items
        if (InventoryManager.Instance != null)
        {
            InventoryManager.OnGemCollected += OnGemCollected;
            InventoryManager.Instance.OnItemAdded += OnItemAddedToInventory;
            InventoryManager.Instance.OnInventoryChanged += OnInventoryChanged;
        }
        
        // Health & Survival Events
        PlayerHealth.OnHealthChangedForHUD += OnHealthChanged;
        PlayerHealth.OnPowerUpStatusChangedForHUD += OnPowerUpStatusChanged;
        
        // Cognitive Events System
        Debug.Log("🧠 COGNITIVE: Subscribing to OnItemHoverStart and OnItemHoverEnd events");
        CognitiveEvents.OnItemHoverStart += OnItemHoverStart;
        CognitiveEvents.OnItemHoverEnd += OnItemHoverEnd;
        CognitiveEvents.OnPlayerHealthChanged += OnPlayerHealthEvent;
        CognitiveEvents.OnPlayerStateChanged += OnPlayerStateChanged;
        CognitiveEvents.OnCombatEvent += OnCombatEvent;
        CognitiveEvents.OnDamageTaken += OnDamageTaken;
        CognitiveEvents.OnPowerUpActivated += OnPowerUpActivated;
        
        // Inventory UI Events
        CognitiveEvents.OnInventoryOpened += OnInventoryOpened;
        CognitiveEvents.OnInventoryClosed += OnInventoryClosed;
        
        // World Interaction Events
        CognitiveEvents.OnWorldInteraction += OnWorldInteraction;
        
        // Movement Performance Events
        Debug.Log("🧠 COGNITIVE: Subscribing to Wall Jump and Aerial Trick systems");
        SubscribeToPerformanceSystems();
    }

    private void UnsubscribeFromGameEvents()
    {
        // 🔥 ONLY ESSENTIALS: Unsubscribe from inventory events only
        
        CognitiveEvents.OnItemHoverStart -= OnItemHoverStart;
        CognitiveEvents.OnItemHoverEnd -= OnItemHoverEnd;
        
        // Inventory UI Events
        CognitiveEvents.OnInventoryOpened -= OnInventoryOpened;
        CognitiveEvents.OnInventoryClosed -= OnInventoryClosed;
        
        // World Interaction Events
        CognitiveEvents.OnWorldInteraction -= OnWorldInteraction;
    }

    #endregion

    #region GAME EVENT HANDLERS

    private void OnPlatformLanded(Transform platform)
    {
        // DISABLED - Not essential
    }

    private void OnGemCollected(int newTotal)
    {
        // DISABLED - Not essential
    }

    private void OnItemAddedToInventory(ChestItemData item, int count)
    {
        // DISABLED - Not essential
    }

    private void OnInventoryChanged()
    {
        lastInventoryCheckTime = Time.time;
        // Trigger inventory analysis after a short delay
        StartCoroutine(DelayedInventoryAnalysis());
    }

    /// <summary>
    /// Subscribe to performance systems (Wall Jump, Aerial Tricks)
    /// </summary>
    private void SubscribeToPerformanceSystems()
    {
        // Check every frame for system availability
        StartCoroutine(WaitForPerformanceSystems());
    }
    
    private System.Collections.IEnumerator WaitForPerformanceSystems()
    {
        // Wait for systems to initialize
        yield return new WaitForSeconds(1f);
        
        // Note: These systems don't have events, so we'll poll them when relevant events happen
        // We'll check them during Update() when performance feats occur
    }
    
    private void OnItemHoverStart(ChestItemData item, UnifiedSlot slot)
    {
        Debug.Log($"🧠 COGNITIVE: OnItemHoverStart called for item: {item?.itemName ?? "NULL"}");
        
        if (item == null) return;
        
        // Generate analysis
        string analysis = GenerateItemAnalysis(item, slot);
        Debug.Log($"🧠 COGNITIVE: Generated analysis: {analysis}");
        
        // Transition to ShowingItemInfo state
        TransitionToState(CognitiveState.ShowingItemInfo);
        ShowDisplay();
        DisplayText(analysis);
        
        lastObservedItem = item;
        isObservingItem = true;
    }

    private void OnItemHoverEnd(ChestItemData item, UnifiedSlot slot)
    {
        Debug.Log($"🧠 COGNITIVE: OnItemHoverEnd called for item: {item?.itemName ?? "NULL"}");
        
        if (isObservingItem)
        {
            isObservingItem = false;
            lastObservedItem = null;
            
            // Only transition to Idle if we're currently showing item info
            if (currentState == CognitiveState.ShowingItemInfo)
            {
                TransitionToState(CognitiveState.Idle);
            }
            
            Debug.Log($"🧠 COGNITIVE: Stopped observing item");
        }
    }

    private void OnHealthChanged(float currentHealth, float maxHealth)
    {
        // DISABLED - Not essential
    }

    private void OnPowerUpStatusChanged(PowerUpType powerUpType, bool isActive, float duration)
    {
        // DISABLED - Not essential
    }

    private void OnPlayerHealthEvent(string eventType, float healthPercentage)
    {
        // DISABLED - Not essential
    }

    private void OnPlayerStateChanged(string newState)
    {
        // DISABLED - Not essential
    }
    
    private void OnWorldInteraction(string interactionType, GameObject interactedObject)
    {
        // Handle FORGE cube and other world interactions
        if (interactionType == "forge_cube_nearby")
        {
            // Only show if not in another state
            if (currentState == CognitiveState.Idle)
            {
                string message = "Press E to use FORGE";
                ShowQuickNotification(message, 2.0f);
                Debug.Log($"🧠 COGNITIVE: FORGE interaction message displayed");
            }
        }
        else if (interactionType == "forge_inventory_full")
        {
            string message = "Inventory Full!";
            ShowQuickNotification(message, 2.0f);
            Debug.Log($"🧠 COGNITIVE: FORGE inventory full message displayed");
        }
        else if (interactionType.StartsWith("forge_crafted_"))
        {
            // Extract item name from interaction type
            string itemName = interactionType.Replace("forge_crafted_", "");
            string message = $"Crafted: {itemName}";
            ShowQuickNotification(message, 2.0f);
            Debug.Log($"🧠 COGNITIVE: FORGE crafted message displayed for {itemName}");
        }
        // Add more interaction types here as needed
    }

    private void OnCombatEvent(string combatType, Vector3 position)
    {
        // DISABLED - Not essential
    }

    private void OnDamageTaken(float damageAmount, string damageType)
    {
        // DISABLED - Not essential
    }

    private void OnPowerUpActivated(string powerUpType)
    {
        // DISABLED - Not essential (no message queue in state machine)
    }
    
    private void OnInventoryOpened()
    {
        RecordAction("inventory_opened");
        Debug.Log("[CognitiveFeed] Inventory opened - state machine will handle display");
        // State machine handles everything via UpdateStateMachine()
    }
    
    private void OnInventoryClosed()
    {
        RecordAction("inventory_closed");
        Debug.Log("[CognitiveFeed] Inventory closed - state machine will resume");
        // State machine handles everything via UpdateStateMachine()
    }

    #endregion

    #region MESSAGE GENERATION SYSTEM

    private string GenerateContextualMessage(string eventType, Dictionary<string, object> context)
    {
        switch (eventType)
        {
            case "platform_landing":
                return GeneratePlatformMessage(context);
            case "combat_start":
                return GenerateCombatMessage(context);
            case "low_health":
                return GenerateHealthWarning(context);
            default:
                return "Interesting...";
        }
    }

    private string GenerateCombatMessage(Dictionary<string, object> context)
    {
        var messages = new[]
        {
            "[Combat protocols engaged. Analyzing threat assessment...]",
            "[Hostile entities detected. Recommend tactical approach.]",
            "[Battle systems online. Monitoring engagement parameters...]"
        };
        return messages[UnityEngine.Random.Range(0, messages.Length)];
    }

    private string GenerateHealthWarning(Dictionary<string, object> context)
    {
        var messages = new[]
        {
            "[WARNING: Health levels critical. Immediate medical attention required.]",
            "[ALERT: Vitals approaching dangerous thresholds. Seek cover.]",
            "[CAUTION: Structural integrity compromised. Emergency protocols advised.]"
        };
        return messages[UnityEngine.Random.Range(0, messages.Length)];
    }

    private string GeneratePlatformMessage(Dictionary<string, object> context)
    {
        int landingCount = (int)context["landing_count"];
        
        var messages = new[]
        {
            "[Platform sync established. Analyzing structural integrity...]",
            "[Gravity anchor deployed. Reading environmental data...]",
            "[Surface contact confirmed. Calculating next trajectory...]",
            "[Platform locked. Cross-referencing with navigation charts...]"
        };

        if (landingCount > 20)
        {
            var expertMessages = new[]
            {
                "[Your platform navigation is becoming quite... elegant.]",
                "[I'm detecting a pattern in your movement choices. Impressive.]",
                "[Platform mastery achieved. You're making this look effortless.]"
            };
            return expertMessages[UnityEngine.Random.Range(0, expertMessages.Length)];
        }

        return messages[UnityEngine.Random.Range(0, messages.Length)];
    }

    private string GenerateGemAnalysis(int totalGems)
    {
        float efficiency = analytics.CalculateGemEfficiency();
        
        if (efficiency > 0.8f)
        {
            return $"[Outstanding gem acquisition rate. Current total: {totalGems}. Your collection efficiency is remarkable.]";
        }
        else if (efficiency < 0.3f)
        {
            return $"[Gem count: {totalGems}. I'm detecting missed opportunities. Consider optimizing your collection routes.]";
        }
        else
        {
            return $"[Gem reserves: {totalGems}. Steady progress. Your methodical approach is... prudent.]";
        }
    }

    private string GenerateItemAcquisitionMessage(ChestItemData item, int count)
    {
        var itemMessages = new Dictionary<string, string[]>
        {
            ["Keycard"] = new[]
            {
                "[Keycard acquired. Access protocols updated.]",
                "[Security clearance obtained. New areas should be accessible.]",
                "[Authorization token secured. Proceed with caution.]"
            },
            ["Backpack"] = new[]
            {
                "[Storage capacity enhanced. Your organizational skills are... improving.]",
                "[Backpack systems integrated. Inventory management just became more efficient.]",
                "[Carrying capacity upgraded. More room for... acquisitions.]"
            },
            ["Vest"] = new[]
            {
                "[Protective systems upgraded. Your survival chances just improved significantly.]",
                "[Armor plating detected. Smart choice given your... tendency to take risks.]",
                "[Defensive protocols enhanced. I recommend testing its limits carefully.]"
            }
        };

        if (itemMessages.ContainsKey(item.itemType))
        {
            var messages = itemMessages[item.itemType];
            return messages[UnityEngine.Random.Range(0, messages.Length)];
        }

        return $"[{item.itemName} acquired. Analyzing potential applications...]";
    }

    private string GenerateItemAnalysis(ChestItemData item, UnifiedSlot slot)
    {
        // Determine context for more accurate analysis
        string context = "unknown";
        if (slot.chestSystem != null)
            context = "chest discovery";
        else if (slot.isStashSlot)
            context = "stash management";
        else
            context = "inventory review";
            
        // Start with colorful header
        var analysis = $"<color=#00FFFF>[{context.ToUpper()}]</color> <color=#FFD700>{item.itemName}</color>";
        
        // Add contextual information based on item type with rich text formatting
        switch (item.itemType?.ToLower())
        {
            case "keycard":
                analysis += context == "chest discovery" ? 
                    ". <color=#00FF00>Excellent find!</color> This keycard <color=#FF69B4>unlocks new areas</color> for exploration" :
                    ". <color=#4ECDC4>Security access tool</color>. Essential for <color=#95E1D3>area progression</color>";
                break;
            case "backpack":
                analysis += context == "chest discovery" ? 
                    ". <color=#FFB347>Storage expansion discovered!</color> Your <color=#98D8C8>carrying capacity</color> will increase significantly" :
                    ". <color=#F38181>Storage enhancement</color>. Current <color=#AA96DA>inventory efficiency</color> analysis in progress";
                break;
            case "vest":
            case "armorplate":
                var playerHealth = FindFirstObjectByType<PlayerHealth>();
                float healthPercentage = playerHealth != null ? 
                    playerHealth.CurrentHealth / playerHealth.maxHealth : 1f;
                    
                if (healthPercentage < 0.7f && context == "inventory review")
                    analysis += ". <color=#4ECDC4>Protective gear</color>. <color=#FF6B6B>RECOMMENDATION:</color> <color=#FFD93D>Equip immediately</color> - health below optimal";
                else if (context == "chest discovery")
                    analysis += ". <color=#00FF00>Defensive equipment acquired!</color> This will <color=#95E1D3>enhance your survival</color> capabilities";
                else
                    analysis += ". <color=#4ECDC4>Protective gear</color>. <color=#98D8C8>Damage mitigation system</color> operational";
                break;
            case "weapon":
                analysis += context == "chest discovery" ? 
                    ". <color=#FF6B6B>Combat enhancement discovered!</color> <color=#FFB347>Offensive capabilities</color> expanded" :
                    ". <color=#FF6B6B>Weapon system</color>. Combat effectiveness analysis: <color=#00FF00>Ready for deployment</color>";
                break;
            case "consumable":
                analysis += context == "chest discovery" ? 
                    ". <color=#98D8C8>Consumable resource</color> located. <color=#FFD93D>Immediate tactical advantage</color> available" :
                    ". <color=#AA96DA>Single-use tactical item</color>. <color=#95E1D3>Strategic deployment</color> recommended";
                break;
            default:
                analysis += context == "chest discovery" ?
                    ". <color=#FF69B4>Unknown artifact discovered</color>. Recommend <color=#FFD93D>immediate acquisition</color> for analysis" :
                    ". Classification: <color=#F38181>Unidentified</color>. Analysis protocols suggest <color=#95E1D3>retention</color>";
                break;
        }

        // Add strategic advice based on current inventory state
        if (InventoryManager.Instance != null)
        {
            float inventoryFullness = analytics.GetInventoryFullness();
            if (inventoryFullness > 0.8f && context == "chest discovery")
            {
                analysis += ". <color=#FF0000>ALERT:</color> <color=#FFD93D>Inventory space critical</color>. Consider item prioritization";
            }
            else if (inventoryFullness > 0.8f)
            {
                analysis += ". <color=#FF6B6B>WARNING:</color> Storage approaching <color=#FFD93D>maximum capacity</color>";
            }
        }

        analysis += ".";
        return analysis;
    }

    private string GeneratePowerUpActivationMessage(PowerUpType powerUpType, float duration)
    {
        var powerUpMessages = new Dictionary<PowerUpType, string[]>
        {
            [PowerUpType.GodMode] = new[]
            {
                $"[Invincibility matrix activated. Duration: {duration:F1}s. You are untouchable.]",
                $"[God mode engaged. {duration:F1} seconds of absolute protection online.]",
                $"[Damage immunity protocols active for {duration:F1}s. Use this time wisely.]"
            },
            [PowerUpType.MaxHandUpgrade] = new[]
            {
                $"[Hand systems at maximum efficiency. Duration: {duration:F1}s.]",
                $"[Weapon output amplified to peak performance for {duration:F1}s.]",
                $"[Hand upgrade matrix: OPTIMAL. {duration:F1}s of enhanced capability.]"
            },
            [PowerUpType.SlowTime] = new[]
            {
                $"[Temporal distortion field active. {duration:F1}s of enhanced reaction time.]",
                $"[Time dilation engaged. Reality moves slower for {duration:F1}s.]",
                $"[Chronometer manipulation online. {duration:F1}s advantage window.]"
            }
        };

        if (powerUpMessages.ContainsKey(powerUpType))
        {
            var messages = powerUpMessages[powerUpType];
            return messages[UnityEngine.Random.Range(0, messages.Length)];
        }

        return $"[{powerUpType} enhancement active. Duration: {duration:F1}s.]";
    }

    private string GeneratePowerUpDeactivationMessage(PowerUpType powerUpType)
    {
        var deactivationMessages = new Dictionary<PowerUpType, string[]>
        {
            [PowerUpType.GodMode] = new[]
            {
                "[Invincibility matrix offline. You are mortal again.]",
                "[God mode disengaged. Normal damage vulnerability restored.]",
                "[Protection field collapsed. Exercise caution.]"
            },
            [PowerUpType.MaxHandUpgrade] = new[]
            {
                "[Hand systems returning to baseline efficiency.]",
                "[Weapon enhancement cycle complete. Normal output restored.]",
                "[Hand upgrade matrix offline. Standard performance resumed.]"
            },
            [PowerUpType.SlowTime] = new[]
            {
                "[Temporal field dissipating. Normal time flow resumed.]",
                "[Time dilation offline. Reality returns to standard speed.]",
                "[Chronometer manipulation complete. Time synchronized.]"
            }
        };

        if (deactivationMessages.ContainsKey(powerUpType))
        {
            var messages = deactivationMessages[powerUpType];
            return messages[UnityEngine.Random.Range(0, messages.Length)];
        }

        return $"[{powerUpType} enhancement cycle complete.]";
    }

    #endregion

    // ========================================
    // REMOVED: Old typewriter/message queue system
    // State machine handles direct display
    // ========================================

    #region ITEM OBSERVATION SYSTEM

    // REMOVED: ShowItemObservation and HideItemObservation - now handled by state machine

    #endregion

    #region CONTINUOUS ANALYSIS SYSTEM

    private IEnumerator ContinuousAnalysis()
    {
        while (true)
        {
            yield return new WaitForSeconds(playerAnalysisInterval);
            
            PerformPlayerAnalysis();
            UpdatePersonalityBasedOnBehavior();
        }
    }

    private void PerformPlayerAnalysis()
    {
        // Analyze recent player behavior and potentially comment
        analytics.UpdateStats();
        
        // DISABLED - State machine only handles 4 core functionalities
    }

    private bool ShouldProvideAnalysisComment()
    {
        // Logic to determine if the AI should comment on player behavior
        return UnityEngine.Random.value < (personality.analytical * 0.3f);
    }

    private string GeneratePlayerAnalysis()
    {
        var patterns = analytics.GetRecentBehaviorPatterns();
        
        if (patterns.Contains("efficient_movement"))
        {
            return "[Your movement patterns show remarkable efficiency. Calculated routes, minimal wasted motion.]";
        }
        else if (patterns.Contains("exploration_focused"))
        {
            return "[I'm detecting a thorough exploration pattern. You're not missing much, are you?]";
        }
        else if (patterns.Contains("risk_taking"))
        {
            return "[Your risk assessment algorithms seem... aggressive. Fascinating approach.]";
        }
        
        return "[Behavioral patterns within normal parameters. Continuing observation...]";
    }

    #endregion

    #region PERSONALITY & LEARNING

    private void RecordAction(string actionType)
    {
        if (!actionCounts.ContainsKey(actionType))
            actionCounts[actionType] = 0;
            
        actionCounts[actionType]++;
        
        // Update behavi or scores based on action type
        UpdateBehaviorScores(actionType);
    }

    private void UpdateBehaviorScores(string actionType)
    {
        switch (actionType)
        {
            case "platform_landing":
                playerBehaviorScores["exploration"] += 0.1f;
                break;
            case "gem_collection":
                playerBehaviorScores["efficiency"] += 0.15f;
                break;
            case "item_acquired":
                playerBehaviorScores["exploration"] += 0.2f;
                break;
        }
    }

    private void UpdatePersonalityBasedOnBehavior()
    {
        // Adapt personality traits based on observed player behavior
        float adaptationRate = personality.adaptability * 0.01f;
        
        if (playerBehaviorScores["efficiency"] > 10f)
        {
            personality.analytical = Mathf.Min(1f, personality.analytical + adaptationRate);
        }
        
        if (playerBehaviorScores["exploration"] > 15f)
        {
            personality.curiosity = Mathf.Min(1f, personality.curiosity + adaptationRate);
        }
    }

    #endregion

    #region PERFORMANCE MONITORING (WALL JUMP & AERIAL TRICKS)
    
    /// <summary>
    /// ULTRA PERFORMANT: Monitor using cached references (POTATO-FRIENDLY!)
    /// </summary>
    private void MonitorPerformanceSystems()
    {
        lastPerformanceCheckTime = Time.time;
        
        // 🔥 COMPLETELY DISABLED - LIVE DATA SYSTEM HANDLES EVERYTHING!
        // No more trick monitoring, no more wall jump monitoring
        // ONLY inventory hover + live data display (wall jumps/tricks shown in real-time)
    }
    
    /// <summary>
    /// Generate REAL NUMERIC DATA analysis for wall jump chains - NO VAGUE INTERPRETATIONS
    /// </summary>
    private void AnalyzeWallJumpPerformance(int chainLevel, int totalJumps, int totalXP)
    {
        if (WallJumpXPSimple.Instance == null) return;
        
        // Get REAL performance data
        var stats = WallJumpXPSimple.Instance.GetSessionStats();
        float currentTime = Time.time;
        float chainDuration = currentTime - lastWallJumpChainStartTime;
        
        // Get player velocity if available
        GameObject playerObj = GameObject.FindWithTag("Player");
        float currentSpeed = 0f;
        if (playerObj != null)
        {
            Rigidbody rb = playerObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                currentSpeed = rb.linearVelocity.magnitude;
            }
        }
        
        // DISABLED - State machine handles wall jump display directly in UpdateWallJumpDisplay()
    }
    
    /// <summary>
    /// REAL NUMERIC DATA - Show actual performance metrics, no vague interpretations
    /// </summary>
    private string GenerateNumericWallJumpAnalysis(int chainLevel, int totalJumps, int totalXP, float chainDuration, float currentSpeed, (int jumps, int xp, int maxChain) sessionStats)
    {
        // Calculate real performance metrics
        float jumpsPerSecond = chainDuration > 0 ? chainLevel / chainDuration : 0f;
        float xpPerJump = chainLevel > 0 ? (float)totalXP / chainLevel : 0f;
        float sessionAverageXP = sessionStats.jumps > 0 ? (float)sessionStats.xp / sessionStats.jumps : 0f;
        
        // Get combo multiplier info if available
        string comboInfo = "";
        if (ComboMultiplierSystem.Instance != null)
        {
            float multiplier = ComboMultiplierSystem.Instance.GetMultiplier();
            if (multiplier > 1f)
            {
                comboInfo = $" | <color=#FF6600>COMBO: x{multiplier:F1}</color>";
            }
        }
        
        // Build message based on chain intensity
        if (chainLevel >= 7)
        {
            return $"<color=#FF0000>🔥 WALL JUMP PERFORMANCE DATA 🔥</color>\n" +
                   $"<color=#FFD700>Chain: {chainLevel}x</color> | <color=#00FFFF>Duration: {chainDuration:F2}s</color> | <color=#FF69B4>Rate: {jumpsPerSecond:F1}/s</color>\n" +
                   $"<color=#00FF00>XP Earned: {totalXP}</color> | <color=#FFAA00>Per Jump: {xpPerJump:F1}</color>{comboInfo}\n" +
                   $"<color=#95E1D3>Speed: {currentSpeed:F1} m/s</color> | <color=#98D8C8>Session Best: {sessionStats.maxChain}x</color>";
        }
        else if (chainLevel >= 4)
        {
            return $"<color=#00FFFF>📊 WALL JUMP METRICS</color>\n" +
                   $"<color=#FFD700>Chain Level: {chainLevel}x</color> | <color=#4ECDC4>Time: {chainDuration:F2}s</color> | <color=#FFB347>XP: {totalXP}</color>{comboInfo}\n" +
                   $"<color=#AA96DA>Current Speed: {currentSpeed:F1} m/s</color> | <color=#F38181>Jumps/Sec: {jumpsPerSecond:F1}</color>";
        }
        else if (chainLevel >= 2)
        {
            return $"<color=#00FFFF>WALL JUMP:</color> <color=#FFD700>x{chainLevel}</color> | <color=#4ECDC4>{chainDuration:F2}s</color> | <color=#98D8C8>{totalXP} XP</color>{comboInfo}";
        }
        
        return "";
    }
    
    /// <summary>
    /// Chain ended - show final numeric performance summary
    /// </summary>
    private void AnalyzeWallJumpChainEnd(int finalChainLevel)
    {
        // Only comment on impressive chains that ended
        if (finalChainLevel >= 4)
        {
            float totalChainTime = Time.time - lastWallJumpChainStartTime;
            float averageJumpInterval = totalChainTime / finalChainLevel;
            
            // Get session stats for comparison
            var sessionStats = WallJumpXPSimple.Instance?.GetSessionStats() ?? (0, 0, 0);
            
            // DISABLED - State machine handles chain end via ShowingWallJumpSummary state
        }
    }
    
    /// <summary>
    /// Analyze aerial trick performance with REAL numeric data - no estimations or vague interpretations
    /// </summary>
    private void AnalyzeAerialTrickPerformance(int biggestTrickXP, int totalTricks, float velocity)
    {
        // 🔥 DISABLED - Live data system shows tricks in real-time!
        // No need for separate messages that conflict with live display
    }
    
    /// <summary>
    /// REAL NUMERIC TRICK ANALYSIS - Show actual performance metrics, no estimations
    /// </summary>
    private string GenerateNumericTrickAnalysis(int xp, int totalTricks, float velocity, (int tricks, int xp, int biggest) sessionStats)
    {
        // Get combo multiplier info if available
        string comboInfo = "";
        if (ComboMultiplierSystem.Instance != null)
        {
            float multiplier = ComboMultiplierSystem.Instance.GetMultiplier();
            if (multiplier > 1f)
            {
                var comboData = ComboMultiplierSystem.Instance.GetComboInfo();
                comboInfo = $" | <color=#FF6600>COMBO: x{multiplier:F1} ({comboData.wallJumps}W+{comboData.tricks}T)</color>";
            }
        }
        
        // Calculate session performance metrics
        float avgXPPerTrick = sessionStats.tricks > 0 ? (float)sessionStats.xp / sessionStats.tricks : 0f;
        float sessionEfficiency = sessionStats.biggest > 0 ? (float)xp / sessionStats.biggest : 1f;
        
        // Build message based on trick intensity
        if (xp >= 150) // Godlike tricks
        {
            return $"<color=#FF00FF>🎪 AERIAL PERFORMANCE DATA 🎪</color>\n" +
                   $"<color=#FFD700>Trick XP: {xp}</color> | <color=#00FFFF>Landing Speed: {velocity:F1} m/s</color>{comboInfo}\n" +
                   $"<color=#FF6B6B>Session Total: {sessionStats.tricks} tricks</color> | <color=#98D8C8>Avg XP: {avgXPPerTrick:F1}</color>\n" +
                   $"<color=#00FF00>Session Best: {sessionStats.biggest} XP</color> | <color=#FFB347>Current/Best: {sessionEfficiency:F1}x</color>";
        }
        else if (xp >= 80) // Big tricks
        {
            return $"<color=#00FFFF>📊 TRICK METRICS</color>\n" +
                   $"<color=#FFD700>XP Earned: {xp}</color> | <color=#4ECDC4>Velocity: {velocity:F1} m/s</color>{comboInfo}\n" +
                   $"<color=#95E1D3>Session Avg: {avgXPPerTrick:F1} XP</color> | <color=#AA96DA>Total Tricks: {sessionStats.tricks}</color>";
        }
        else if (xp >= 30) // Medium tricks
        {
            return $"<color=#00FFFF>AERIAL:</color> <color=#FFD700>{xp} XP</color> | <color=#4ECDC4>{velocity:F1} m/s</color>{comboInfo}";
        }
        
        return "";
    }
    
    #endregion

    #region UTILITY METHODS

    private IEnumerator DelayedInventoryAnalysis()
    {
        yield return new WaitForSeconds(1f);
        // Perform inventory analysis here if needed
    }

    private IEnumerator WelcomeSequence()
    {
        // DISABLED - No welcome messages in clean state machine
        yield break;
    }

    private void PlayTypingSound(char character, EmotionType emotion)
    {
        // Use the existing GameSounds system
        GameSounds.PlayCognitiveFeedWord(transform.position, 0.3f);
    }

    private void StopAllAudio()
    {
        // Stop the audio source if it's playing
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        
        // Stop all AudioSources on this GameObject (including one-shot sounds)
        AudioSource[] allAudioSources = GetComponents<AudioSource>();
        foreach (var source in allAudioSources)
        {
            if (source != null && source.isPlaying)
            {
                source.Stop();
            }
        }
        
        Debug.Log("[CognitiveFeedEnhanced] 🔇 All audio stopped");
    }

    private Color GetEmotionColor(EmotionType emotion)
    {
        // Map emotion types to color indices
        int colorIndex = 0;
        switch (emotion)
        {
            case EmotionType.Normal: colorIndex = 0; break;
            case EmotionType.Happy: colorIndex = 1; break;
            case EmotionType.Warning: colorIndex = 2; break;
            case EmotionType.Danger: colorIndex = 3; break;
            case EmotionType.Analytical:
            case EmotionType.Curious:
            case EmotionType.Thoughtful: colorIndex = 4; break;
            case EmotionType.Excited: colorIndex = 5; break;
            case EmotionType.Urgent: colorIndex = 2; break; // Use warning color for urgency
        }
        
        return emotionColors[Mathf.Min(colorIndex, emotionColors.Length - 1)];
    }

    private int GetMessagePriority(MessageType type)
    {
        switch (type)
        {
            case MessageType.Urgent: return 4;
            case MessageType.Warning: return 3;
            case MessageType.Analysis: return 2;
            case MessageType.Discovery: return 2;
            case MessageType.Normal: return 1;
            case MessageType.System: return 1;
            default: return 1;
        }
    }

    private float CalculateHoldTime(string text, MessageType messageType)
    {
        float baseTime = 3f + (text.Length * 0.05f);
        
        switch (messageType)
        {
            case MessageType.Urgent:
                return baseTime * 1.5f;
            case MessageType.Warning:
                return baseTime * 1.3f;
            case MessageType.Analysis:
                return baseTime * 1.2f;
            default:
                return baseTime;
        }
    }

    private int GetActionCount(string actionType)
    {
        return actionCounts.ContainsKey(actionType) ? actionCounts[actionType] : 0;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float from, float to, float duration)
    {
        Debug.Log($"🧠 COGNITIVE: FadeCanvasGroup starting - from {from} to {to} over {duration}s");
        Debug.Log($"🧠 COGNITIVE: CanvasGroup object: {canvasGroup.name}, active: {canvasGroup.gameObject.activeInHierarchy}");
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float newAlpha = Mathf.Lerp(from, to, elapsed / duration);
            canvasGroup.alpha = newAlpha;
            yield return null;
        }
        canvasGroup.alpha = to;
        Debug.Log($"🧠 COGNITIVE: FadeCanvasGroup complete - final alpha: {canvasGroup.alpha}");
    }

    #endregion

    #region HELPER METHODS FOR STATE MACHINE
    
    // 🎯 COHERENCE: GetPrivateField REMOVED - reflection anti-pattern eliminated!
    // All systems now expose proper public accessors.
    // This is a major architectural improvement for performance and maintainability.
    
    #endregion

    #region DATA STRUCTURES

    [System.Serializable]
    public class CognitiveMessage
    {
        public string text;
        public MessageType messageType;
        public EmotionType emotion;
        public float timestamp;
        public int priority;
    }

    [System.Serializable]
    public class CognitivePersonality
    {
        public float curiosity;      // How much the AI comments on discoveries
        public float analytical;     // How much the AI analyzes player behavior
        public float sarcasm;        // How sarcastic responses can be
        public float support;        // How encouraging the AI is
        public float urgency;        // How quickly the AI responds to danger
        public float adaptability;   // How much the personality changes over time
    }

    [System.Serializable]
    public class CognitiveMemory
    {
        public string eventType;
        public float timestamp;
        public Dictionary<string, object> context;
        public float importance;
    }

    [System.Serializable]
    public class PlayerAnalytics
    {
        public float totalPlayTime;
        public int totalActions;
        public Dictionary<string, float> actionRates = new Dictionary<string, float>();
        
        // Health tracking
        public float lastHealthPercentage = 1f;
        public bool hasWarnedLowHealth = false;
        public int damageEventCount = 0;
        public float totalDamageTaken = 0f;
        
        // Performance metrics
        public float averageHealthMaintenance = 1f;
        public Dictionary<string, int> behaviorCounts = new Dictionary<string, int>();
        
        public void UpdateStats()
        {
            totalPlayTime += Time.deltaTime;
            
            // Update average health maintenance
            if (InventoryManager.Instance != null)
            {
                // Add real health tracking here when PlayerHealth is accessible
            }
        }
        
        public float CalculateGemEfficiency()
        {
            // Calculate how efficiently the player collects gems
            float baseEfficiency = UnityEngine.Random.Range(0.3f, 0.9f);
            
            // Adjust based on recent behavior
            if (behaviorCounts.ContainsKey("gem_collection"))
            {
                int gemCollections = behaviorCounts["gem_collection"];
                if (gemCollections > 50) baseEfficiency += 0.1f; // Bonus for high collection
            }
            
            return Mathf.Clamp01(baseEfficiency);
        }
        
        public float GetInventoryFullness()
        {
            if (InventoryManager.Instance != null)
            {
                // Calculate actual inventory fullness
                int usedSlots = 0;
                int totalSlots = InventoryManager.Instance.currentActiveSlots;
                
                // This would need access to actual slot data
                return Mathf.Clamp01((float)usedSlots / totalSlots);
            }
            return 0f;
        }
        
        public List<string> GetRecentBehaviorPatterns()
        {
            var patterns = new List<string>();
            
            // Analyze behavior counts to determine patterns
            if (behaviorCounts.ContainsKey("platform_landing") && behaviorCounts["platform_landing"] > 20)
            {
                patterns.Add("efficient_movement");
            }
            
            if (behaviorCounts.ContainsKey("item_acquired") && behaviorCounts["item_acquired"] > 10)
            {
                patterns.Add("exploration_focused");
            }
            
            if (damageEventCount > 10 && averageHealthMaintenance < 0.5f)
            {
                patterns.Add("risk_taking");
            }
            else if (damageEventCount < 5 && averageHealthMaintenance > 0.8f)
            {
                patterns.Add("cautious_play");
            }
            
            if (behaviorCounts.ContainsKey("powerup_activated") && behaviorCounts["powerup_activated"] > 5)
            {
                patterns.Add("strategic_enhancement");
            }
            
            return patterns;
        }
        
        public void RecordBehavior(string behaviorType)
        {
            if (!behaviorCounts.ContainsKey(behaviorType))
                behaviorCounts[behaviorType] = 0;
                
            behaviorCounts[behaviorType]++;
        }
        
        public void RecordDamage(float amount)
        {
            damageEventCount++;
            totalDamageTaken += amount;
            
            // Update average health maintenance (simplified)
            averageHealthMaintenance = Mathf.Lerp(averageHealthMaintenance, lastHealthPercentage, 0.1f);
        }
    }

    public enum MessageType
    {
        Normal,
        System,
        Warning,
        Urgent,
        Analysis,
        Discovery,
        Observation
    }

    public enum EmotionType
    {
        Normal = 0,
        Happy = 1,
        Warning = 2,
        Danger = 3,
        Analytical = 4,
        Curious = 5,
        Excited = 6,
        Thoughtful = 7,
        Urgent = 8
    }

    #endregion
}