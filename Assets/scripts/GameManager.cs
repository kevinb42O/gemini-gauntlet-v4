using UnityEngine;
using System.Collections.Generic;
using GeminiGauntlet.UI; // FloatingTextManager
using GeminiGauntlet.Progression; // XPManager

/// <summary>
/// Central GameManager that holds references to all key game systems.
/// Eliminates the need for inefficient FindObjectOfType calls throughout the codebase.
/// Provides clean dependency injection and system communication.
/// </summary>
public class GameManager : MonoBehaviour
{
    // Singleton instance
    public static GameManager Instance { get; private set; }
    
    [Header("Core Systems")]
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private StashManager stashManager;
    [SerializeField] private PlayerHealth playerHealth;
    
    [Header("UI Systems")]
    [SerializeField] private MenuReturnSaveHandler menuReturnSaveHandler;
    [SerializeField] private ReviveSlotController reviveSlotController;
    [SerializeField] private SelfReviveUI selfReviveUI;
    
    [Header("Interaction Systems")]
    [SerializeField] private ChestInteractionSystem chestInteractionSystem;
    [SerializeField] private ExitZone exitZone;
    
    [Header("Audio Systems (DEPRECATED - Using Modern SoundEvents System)")]
    // DEPRECATED: Legacy audio systems - replaced by Assets/scripts/Audio/FIXSOUNDSCRIPTS/
    // Modern system uses: SoundEvents.cs, GameSoundsHelper.cs, SoundSystemBootstrap.cs
    // [SerializeField] private PlayerSoundManager playerSoundManager; // DEPRECATED
    // [SerializeField] private PlatformRelativeAudioManager audioManager; // DEPRECATED
    
    [Header("Player Systems")]
    [SerializeField] private PlayerMovementManager playerMovementManager;
    [SerializeField] private PlayerProgression playerProgression;
    [SerializeField] private PlayerOverheatManager playerOverheatManager;
    
    [Header("🎯 PERFORMANCE-CRITICAL SYSTEMS (Coherence Upgrade)")]
    [Tooltip("COHERENCE: Cached references eliminate 180+ FindObjectOfType calls across codebase")]
    [SerializeField] private AAACameraController aaaCameraController;
    [SerializeField] private AAAMovementController aaaMovementController;
    [SerializeField] private CognitiveFeedManagerEnhanced cognitiveFeedbackManager;
    [SerializeField] private WallJumpXPSimple wallJumpXPSystem;
    [SerializeField] private AerialTrickXPSystem aerialTrickSystem;
    [SerializeField] private ComboMultiplierSystem comboSystem;
    [SerializeField] private PlayerEnergySystem energySystem;
    [SerializeField] private CleanAAACrouch crouchController;
    [SerializeField] private FloatingTextManager floatingTextManager;
    [SerializeField] private XPManager xpManager;
    
    [Header("Auto-Discovery")]
    [SerializeField] private bool autoDiscoverSystems = true;
    [SerializeField] private bool logDiscoveryProcess = true;
    
    // System availability flags
    public bool IsInMenuContext { get; private set; }
    public bool IsInGameContext { get; private set; }
    
    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("🎮 GameManager: Singleton instance created and made persistent");
            
            // Auto-discover systems if enabled
            if (autoDiscoverSystems)
            {
                AutoDiscoverSystems();
            }
            
            // Detect context
            DetectContext();
        }
        else
        {
            Debug.LogWarning("🎮 GameManager: Duplicate instance destroyed");
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Initialize system references after all Awake calls
        InitializeSystemReferences();
    }
    
    /// <summary>
    /// Auto-discover systems in the scene if not manually assigned
    /// </summary>
    private void AutoDiscoverSystems()
    {
        if (logDiscoveryProcess)
        {
            Debug.Log("🔍 GameManager: Auto-discovering systems...");
        }
        
        // Core Systems
        // PersistentInventoryManager removed - using simplified stash/inventory system
        
        if (inventoryManager == null)
            inventoryManager = FindObjectOfType<InventoryManager>();
        
        if (stashManager == null)
            stashManager = FindObjectOfType<StashManager>();
        
        if (playerHealth == null)
            playerHealth = FindObjectOfType<PlayerHealth>();
        
        // UI Systems
        if (menuReturnSaveHandler == null)
            menuReturnSaveHandler = FindObjectOfType<MenuReturnSaveHandler>();
        
        if (reviveSlotController == null)
            reviveSlotController = FindObjectOfType<ReviveSlotController>();
        
        if (selfReviveUI == null)
            selfReviveUI = FindObjectOfType<SelfReviveUI>();
        
        // Interaction Systems
        if (chestInteractionSystem == null)
            chestInteractionSystem = FindObjectOfType<ChestInteractionSystem>();
        
        if (exitZone == null)
            exitZone = FindObjectOfType<ExitZone>();
        
        // Audio Systems - DEPRECATED (Using modern SoundEvents system)
        // Legacy systems disabled - modern audio via Assets/scripts/Audio/FIXSOUNDSCRIPTS/
        
        // Player Systems
        if (playerMovementManager == null)
            playerMovementManager = FindObjectOfType<PlayerMovementManager>();
        
        if (playerProgression == null)
            playerProgression = FindObjectOfType<PlayerProgression>();
        
        if (playerOverheatManager == null)
            playerOverheatManager = FindObjectOfType<PlayerOverheatManager>();
        
        // 🎯 PERFORMANCE-CRITICAL SYSTEMS (Coherence Upgrade)
        if (aaaCameraController == null)
            aaaCameraController = FindObjectOfType<AAACameraController>();
        
        if (aaaMovementController == null)
            aaaMovementController = FindObjectOfType<AAAMovementController>();
        
        if (cognitiveFeedbackManager == null)
            cognitiveFeedbackManager = FindObjectOfType<CognitiveFeedManagerEnhanced>();
        
        if (wallJumpXPSystem == null)
            wallJumpXPSystem = FindObjectOfType<WallJumpXPSimple>();
        
        if (aerialTrickSystem == null)
            aerialTrickSystem = FindObjectOfType<AerialTrickXPSystem>();
        
        if (comboSystem == null)
            comboSystem = FindObjectOfType<ComboMultiplierSystem>();
        
        if (energySystem == null)
            energySystem = FindObjectOfType<PlayerEnergySystem>();
        
        if (crouchController == null)
            crouchController = FindObjectOfType<CleanAAACrouch>();
        
        if (floatingTextManager == null)
            floatingTextManager = FindObjectOfType<FloatingTextManager>();
        
        if (xpManager == null)
            xpManager = FindObjectOfType<XPManager>();
        
        if (logDiscoveryProcess)
        {
            LogDiscoveredSystems();
        }
    }
    
    /// <summary>
    /// Detect whether we're in menu or game context
    /// </summary>
    private void DetectContext()
    {
        IsInMenuContext = stashManager != null;
        IsInGameContext = playerHealth != null && !IsInMenuContext;
        
        string context = IsInMenuContext ? "MENU" : (IsInGameContext ? "GAME" : "UNKNOWN");
        Debug.Log($"🎮 GameManager: Detected context: {context}");
    }
    
    /// <summary>
    /// Initialize system references after discovery
    /// </summary>
    private void InitializeSystemReferences()
    {
        // Provide GameManager reference to systems that need it
        // PersistentInventoryManager removed - using simplified stash/inventory system
        
        if (stashManager != null)
        {
            // StashManager can access InventoryManager through GameManager
        }
        
        if (menuReturnSaveHandler != null)
        {
            // MenuReturnSaveHandler can access all systems through GameManager
        }
        
        Debug.Log("🎮 GameManager: System references initialized");
    }
    
    /// <summary>
    /// Log all discovered systems for debugging
    /// </summary>
    private void LogDiscoveredSystems()
    {
        Debug.Log("🎮 GameManager: System Discovery Results:");
        Debug.Log($"  📦 PersistentInventoryManager: ❌ (Removed - using simplified system)");
        Debug.Log($"  🎒 InventoryManager: {(inventoryManager != null ? "✅" : "❌")}");
        Debug.Log($"  🏪 StashManager: {(stashManager != null ? "✅" : "❌")}");
        Debug.Log($"  ❤️ PlayerHealth: {(playerHealth != null ? "✅" : "❌")}");
        Debug.Log($"  💾 MenuReturnSaveHandler: {(menuReturnSaveHandler != null ? "✅" : "❌")}");
        Debug.Log($"  💊 ReviveSlotController: {(reviveSlotController != null ? "✅" : "❌")}");
        Debug.Log($"  🆘 SelfReviveUI: {(selfReviveUI != null ? "✅" : "❌")}");
        Debug.Log($"  📦 ChestInteractionSystem: {(chestInteractionSystem != null ? "✅" : "❌")}");
        Debug.Log($"  🚪 ExitZone: {(exitZone != null ? "✅" : "❌")}");
        Debug.Log($"  🏃 PlayerMovementManager: {(playerMovementManager != null ? "✅" : "❌")}");
        Debug.Log($"  📈 PlayerProgression: {(playerProgression != null ? "✅" : "❌")}");
        Debug.Log($"  🔥 PlayerOverheatManager: {(playerOverheatManager != null ? "✅" : "❌")}");
        Debug.Log("  🎯 === PERFORMANCE-CRITICAL SYSTEMS ===");
        Debug.Log($"  📷 AAACameraController: {(aaaCameraController != null ? "✅" : "❌")}");
        Debug.Log($"  🏃 AAAMovementController: {(aaaMovementController != null ? "✅" : "❌")}");
        Debug.Log($"  🧠 CognitiveFeedbackManager: {(cognitiveFeedbackManager != null ? "✅" : "❌")}");
        Debug.Log($"  🧗 WallJumpXPSystem: {(wallJumpXPSystem != null ? "✅" : "❌")}");
        Debug.Log($"  🎪 AerialTrickSystem: {(aerialTrickSystem != null ? "✅" : "❌")}");
        Debug.Log($"  🔥 ComboSystem: {(comboSystem != null ? "✅" : "❌")}");
        Debug.Log($"  ⚡ EnergySystem: {(energySystem != null ? "✅" : "❌")}");
        Debug.Log($"  🦆 CrouchController: {(crouchController != null ? "✅" : "❌")}");
        Debug.Log($"  💬 FloatingTextManager: {(floatingTextManager != null ? "✅" : "❌")}");
        Debug.Log($"  ⭐ XPManager: {(xpManager != null ? "✅" : "❌")}");
    }
    
    // ==========================================
    // PUBLIC ACCESSORS - Replace FindObjectOfType calls
    // ==========================================
    
    // PersistentInventoryManager removed - using simplified stash/inventory system
    
    /// <summary>
    /// Get InventoryManager instance
    /// </summary>
    public InventoryManager GetInventoryManager()
    {
        if (inventoryManager == null)
        {
            inventoryManager = FindObjectOfType<InventoryManager>();
            if (inventoryManager == null)
            {
                Debug.LogWarning("🎮 GameManager: InventoryManager not found!");
            }
        }
        return inventoryManager;
    }
    
    /// <summary>
    /// Get StashManager instance
    /// </summary>
    public StashManager GetStashManager()
    {
        if (stashManager == null)
        {
            stashManager = FindObjectOfType<StashManager>();
            if (stashManager == null && IsInMenuContext)
            {
                Debug.LogWarning("🎮 GameManager: StashManager not found in menu context!");
            }
        }
        return stashManager;
    }
    
    /// <summary>
    /// Get PlayerHealth instance
    /// </summary>
    public PlayerHealth GetPlayerHealth()
    {
        if (playerHealth == null)
        {
            playerHealth = FindObjectOfType<PlayerHealth>();
            if (playerHealth == null && IsInGameContext)
            {
                Debug.LogWarning("🎮 GameManager: PlayerHealth not found in game context!");
            }
        }
        return playerHealth;
    }
    
    /// <summary>
    /// Get MenuReturnSaveHandler instance
    /// </summary>
    public MenuReturnSaveHandler GetMenuReturnSaveHandler()
    {
        if (menuReturnSaveHandler == null)
        {
            menuReturnSaveHandler = FindObjectOfType<MenuReturnSaveHandler>();
        }
        return menuReturnSaveHandler;
    }
    
    /// <summary>
    /// Get ReviveSlotController instance
    /// </summary>
    public ReviveSlotController GetReviveSlotController()
    {
        if (reviveSlotController == null)
        {
            reviveSlotController = FindObjectOfType<ReviveSlotController>();
        }
        return reviveSlotController;
    }
    
    /// <summary>
    /// Get SelfReviveUI instance
    /// </summary>
    public SelfReviveUI GetSelfReviveUI()
    {
        if (selfReviveUI == null)
        {
            selfReviveUI = FindObjectOfType<SelfReviveUI>();
        }
        return selfReviveUI;
    }
    
    /// <summary>
    /// Get ChestInteractionSystem instance
    /// </summary>
    public ChestInteractionSystem GetChestInteractionSystem()
    {
        if (chestInteractionSystem == null)
        {
            chestInteractionSystem = FindObjectOfType<ChestInteractionSystem>();
        }
        return chestInteractionSystem;
    }
    
    /// <summary>
    /// Get ExitZone instance
    /// </summary>
    public ExitZone GetExitZone()
    {
        if (exitZone == null)
        {
            exitZone = FindObjectOfType<ExitZone>();
        }
        return exitZone;
    }
    
    // ========================================
    // AUDIO SYSTEMS - DEPRECATED
    // Using modern system: Assets/scripts/Audio/FIXSOUNDSCRIPTS/
    // SoundEvents.cs, GameSoundsHelper.cs, SoundSystemBootstrap.cs
    // ========================================
    
    /// <summary>
    /// Get PlayerMovementManager instance
    /// </summary>
    public PlayerMovementManager GetPlayerMovementManager()
    {
        if (playerMovementManager == null)
        {
            playerMovementManager = FindObjectOfType<PlayerMovementManager>();
        }
        return playerMovementManager;
    }
    
    /// <summary>
    /// Get PlayerProgression instance
    /// </summary>
    public PlayerProgression GetPlayerProgression()
    {
        if (playerProgression == null)
        {
            playerProgression = FindObjectOfType<PlayerProgression>();
        }
        return playerProgression;
    }
    
    /// <summary>
    /// Get PlayerOverheatManager instance
    /// </summary>
    public PlayerOverheatManager GetPlayerOverheatManager()
    {
        if (playerOverheatManager == null)
        {
            playerOverheatManager = FindObjectOfType<PlayerOverheatManager>();
        }
        return playerOverheatManager;
    }
    
    // ========================================
    // 🎯 PERFORMANCE-CRITICAL SYSTEM ACCESSORS
    // COHERENCE: Replaces 180+ FindObjectOfType calls
    // ========================================
    
    public AAACameraController GetAAACameraController()
    {
        if (aaaCameraController == null)
            aaaCameraController = FindObjectOfType<AAACameraController>();
        return aaaCameraController;
    }
    
    public AAAMovementController GetAAAMovementController()
    {
        if (aaaMovementController == null)
            aaaMovementController = FindObjectOfType<AAAMovementController>();
        return aaaMovementController;
    }
    
    public CognitiveFeedManagerEnhanced GetCognitiveFeedbackManager()
    {
        if (cognitiveFeedbackManager == null)
            cognitiveFeedbackManager = FindObjectOfType<CognitiveFeedManagerEnhanced>();
        return cognitiveFeedbackManager;
    }
    
    public WallJumpXPSimple GetWallJumpXPSystem()
    {
        if (wallJumpXPSystem == null)
            wallJumpXPSystem = FindObjectOfType<WallJumpXPSimple>();
        return wallJumpXPSystem;
    }
    
    public AerialTrickXPSystem GetAerialTrickSystem()
    {
        if (aerialTrickSystem == null)
            aerialTrickSystem = FindObjectOfType<AerialTrickXPSystem>();
        return aerialTrickSystem;
    }
    
    public ComboMultiplierSystem GetComboSystem()
    {
        if (comboSystem == null)
            comboSystem = FindObjectOfType<ComboMultiplierSystem>();
        return comboSystem;
    }
    
    public PlayerEnergySystem GetEnergySystem()
    {
        if (energySystem == null)
            energySystem = FindObjectOfType<PlayerEnergySystem>();
        return energySystem;
    }
    
    public CleanAAACrouch GetCrouchController()
    {
        if (crouchController == null)
            crouchController = FindObjectOfType<CleanAAACrouch>();
        return crouchController;
    }
    
    public FloatingTextManager GetFloatingTextManager()
    {
        if (floatingTextManager == null)
            floatingTextManager = FindObjectOfType<FloatingTextManager>();
        return floatingTextManager;
    }
    
    public XPManager GetXPManager()
    {
        if (xpManager == null)
            xpManager = FindObjectOfType<XPManager>();
        return xpManager;
    }
    
    /// <summary>
    /// Refresh system references (useful after scene changes)
    /// </summary>
    public void RefreshSystemReferences()
    {
        Debug.Log("🎮 GameManager: Refreshing system references...");
        
        // Clear current references
        inventoryManager = null;
        stashManager = null;
        playerHealth = null;
        menuReturnSaveHandler = null;
        reviveSlotController = null;
        selfReviveUI = null;
        chestInteractionSystem = null;
        exitZone = null;
        
        // Clear performance-critical references
        aaaCameraController = null;
        aaaMovementController = null;
        cognitiveFeedbackManager = null;
        wallJumpXPSystem = null;
        aerialTrickSystem = null;
        comboSystem = null;
        energySystem = null;
        crouchController = null;
        floatingTextManager = null;
        xpManager = null;
        
        // Re-discover systems
        AutoDiscoverSystems();
        DetectContext();
        InitializeSystemReferences();
        
        Debug.Log("🎮 GameManager: System references refreshed");
    }
}
