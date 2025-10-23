using UnityEngine;
using GeminiGauntlet.Progression;
using GeminiGauntlet.UI;

/// <summary>
/// 🎯 WALL JUMP XP CHAIN SYSTEM - SIMPLE VERSION
/// Uses your existing FloatingTextManager - ZERO setup needed!
/// </summary>
public class WallJumpXPSimple : MonoBehaviour
{
    public static WallJumpXPSimple Instance { get; private set; }
    
    [Header("=== XP CHAIN SETTINGS ===")]
    [SerializeField] private int baseWallJumpXP = 5;
    [SerializeField] private float xpMultiplierPerChain = 1.5f;
    [SerializeField] private int maxChainLevel = 10;
    [SerializeField] private float chainTimeWindow = 2.0f;
    
    [Header("=== VISUAL SETTINGS ===")]
    [SerializeField] private float spawnDistanceInFront = 3000f; // Distance in front of camera
    [SerializeField] private float textSizeMultiplier = 5f; // Text size multiplier (1 = normal, 5 = 5x larger)
    [Tooltip("Delay before showing text (lets camera stabilize)")]
    [SerializeField] private float textDisplayDelay = 0.1f; // Small delay for camera stabilization
    
    [Header("=== AAA SCI-FI COLORS ===")]
    [SerializeField] private Color singleJumpColor = new Color(0.4f, 0.8f, 1f); // Cyan
    [SerializeField] private Color chain2Color = new Color(0f, 1f, 0.5f); // Neon Green
    [SerializeField] private Color chain3Color = new Color(0f, 1f, 1f); // Bright Cyan
    [SerializeField] private Color chain4Color = new Color(0.5f, 0.5f, 1f); // Electric Blue
    [SerializeField] private Color chain5Color = new Color(1f, 0.5f, 1f); // Neon Pink
    [SerializeField] private Color chain6Color = new Color(1f, 0.3f, 0f); // Hot Orange
    [SerializeField] private Color chain7Color = new Color(1f, 0f, 0.5f); // Hot Pink
    [SerializeField] private Color chain8Color = new Color(1f, 0f, 0f); // Danger Red
    [SerializeField] private Color chain9Color = new Color(1f, 1f, 0f); // Warning Yellow
    [SerializeField] private Color chain10PlusColor = new Color(1f, 0.8f, 0f); // Legendary Gold
    
    [Header("=== AUDIO ===")]
    [SerializeField] private bool enableAudio = true; // Enable XP notification sound
    
    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugLogs = false; // DISABLED - logs kill FPS!
    
    // Chain state
    private int currentChainLevel = 0;
    private float lastWallJumpTime = -999f;
    private int totalWallJumpsThisSession = 0;
    private int totalXPEarnedFromWallJumps = 0;
    
    // Chain titles (NEW LOGIC: 1st jump = no chain, 2nd jump = chain starts!)
    private readonly string[] chainTitles = new string[]
    {
        "",                    // 0 - unused
        "WALL JUMP!",         // 1 - Single jump (no chain yet)
        "CHAIN x2!",          // 2 - Chain starts!
        "CHAIN x3!",          // 3
        "CHAIN x4!",          // 4
        "MEGA CHAIN x5!",     // 5
        "ULTRA CHAIN x6!",    // 6
        "MONSTER CHAIN x7!",  // 7
        "LEGENDARY x8!",      // 8
        "GODLIKE x9!",        // 9
        "UNSTOPPABLE x10!!!"  // 10+
    };
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (showDebugLogs) Debug.Log("[WallJumpXP] Simple instance created");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Call this from AAAMovementController when a wall jump is performed
    /// </summary>
    public void OnWallJumpPerformed(Vector3 wallJumpPosition)
    {
        float timeSinceLastJump = Time.time - lastWallJumpTime;
        
        // Check if chain continues or resets
        if (timeSinceLastJump <= chainTimeWindow && currentChainLevel > 0)
        {
            currentChainLevel = Mathf.Min(currentChainLevel + 1, maxChainLevel);
        }
        else
        {
            currentChainLevel = 1;
        }
        
        lastWallJumpTime = Time.time;
        totalWallJumpsThisSession++;
        
        // Calculate base XP
        int baseXP = CalculateXP(currentChainLevel);
        
        // 🔥 COMBO MULTIPLIER SYSTEM!
        float xpComboMultiplier = 1f;
        bool hasCombo = false;
        if (ComboMultiplierSystem.Instance != null)
        {
            // Add this wall jump to the combo
            ComboMultiplierSystem.Instance.AddWallJump();
            
            // Get the multiplier
            xpComboMultiplier = ComboMultiplierSystem.Instance.GetMultiplier();
            hasCombo = xpComboMultiplier > 1f;
        }
        
        // Apply combo multiplier to XP!
        int xpEarned = Mathf.RoundToInt(baseXP * xpComboMultiplier);
        totalXPEarnedFromWallJumps += xpEarned;
        
        // Grant XP through existing system
        if (XPManager.Instance != null)
        {
            string wallJumpName = $"Wall Jump x{currentChainLevel}";
            if (hasCombo)
            {
                wallJumpName += $" [COMBO x{xpComboMultiplier:F1}!]";
            }
            XPManager.Instance.GrantXP(xpEarned, "Movement", wallJumpName);
        }
        
        // Show floating text with delay (lets camera stabilize)
        // NOTE: Combo multiplier is NOT shown here - it's shown on TRICK landing for maximum impact!
        StartCoroutine(ShowFloatingTextDelayed(wallJumpPosition, currentChainLevel, xpEarned, textDisplayDelay));
        
        // 🎵 PLAY SATISFYING XP NOTIFICATION SOUND (pitch scales with chain!)
        if (enableAudio)
        {
            GeminiGauntlet.Audio.GameSounds.PlayWallJumpXPNotification(wallJumpPosition, currentChainLevel, 1.0f);
            
            // NOTE: Combo sound is NOT played here - it's played on TRICK landing for epic payoff!
            
            if (showDebugLogs)
            {
                Debug.Log($"[WallJumpXP] 🎵 Playing XP notification sound for chain x{currentChainLevel}");
            }
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[WallJumpXP] 🎯 CHAIN x{currentChainLevel}! Base XP: {baseXP}, Combo: x{xpComboMultiplier:F1}, Total XP: {xpEarned}");
        }
    }
    
    /// <summary>
    /// Calculate XP based on chain level
    /// </summary>
    private int CalculateXP(int chainLevel)
    {
        if (chainLevel <= 0) return 0;
        float xp = baseWallJumpXP * Mathf.Pow(xpMultiplierPerChain, chainLevel - 1);
        return Mathf.RoundToInt(xp);
    }
    
    /// <summary>
    /// Get AAA sci-fi color based on chain level
    /// </summary>
    private Color GetChainColor(int chainLevel)
    {
        switch (chainLevel)
        {
            case 1: return singleJumpColor;
            case 2: return chain2Color;
            case 3: return chain3Color;
            case 4: return chain4Color;
            case 5: return chain5Color;
            case 6: return chain6Color;
            case 7: return chain7Color;
            case 8: return chain8Color;
            case 9: return chain9Color;
            default: return chain10PlusColor; // 10+
        }
    }
    
    /// <summary>
    /// Coroutine to show text with delay (camera stabilization)
    /// </summary>
    private System.Collections.IEnumerator ShowFloatingTextDelayed(Vector3 position, int chainLevel, int xpEarned, float delay)
    {
        // Wait for camera to stabilize
        yield return new WaitForSeconds(delay);
        
        // Now show the text with stable camera position
        ShowFloatingText(position, chainLevel, xpEarned);
    }
    
    /// <summary>
    /// Show floating text using existing FloatingTextManager
    /// NOTE: Combo info is NOT shown here - it's shown on trick landing for maximum impact!
    /// </summary>
    private void ShowFloatingText(Vector3 position, int chainLevel, int xpEarned)
    {
        if (FloatingTextManager.Instance == null)
        {
            if (showDebugLogs) Debug.LogWarning("[WallJumpXP] FloatingTextManager not found!");
            return;
        }
        
        // SPAWN 350 UNITS IN FRONT OF CAMERA (where you're looking!)
        Vector3 spawnPosition = position;
        if (Camera.main != null)
        {
            // Get camera forward direction (horizontal only)
            Vector3 cameraForward = Camera.main.transform.forward;
            cameraForward.y = 0; // Flatten to horizontal
            
            if (cameraForward.sqrMagnitude > 0.01f)
            {
                cameraForward.Normalize();
                spawnPosition += cameraForward * spawnDistanceInFront; // Configurable distance!
            }
            
            if (showDebugLogs)
            {
                Debug.Log($"[WallJumpXP] Offset text {spawnDistanceInFront} units forward: {position} → {spawnPosition}");
            }
        }
        
        // Get chain title
        string title = chainLevel < chainTitles.Length ? chainTitles[chainLevel] : chainTitles[chainTitles.Length - 1];
        
        // Get AAA sci-fi color based on chain level
        Color color = GetChainColor(chainLevel);
        
        // Build text (clean and simple - combo info shown on trick landing!)
        string text;
        if (chainLevel == 1)
        {
            text = $"{title}\n+{xpEarned} XP"; // Single jump: no chain number
        }
        else
        {
            text = $"{title}\n+{xpEarned} XP"; // Chain: title already includes "x2", "x3", etc.
        }
        
        // Calculate font size based on chain level (configurable multiplier!)
        int baseFontSize = 48 + (chainLevel * 8); // Base: 48 → 128
        int fontSize = Mathf.RoundToInt(baseFontSize * textSizeMultiplier);
        fontSize = Mathf.Min(fontSize, 1000); // Cap at 1000 to prevent issues
        
        // Show using existing system with locked rotation and MOVEMENT style (italic, dynamic!)
        FloatingTextManager.Instance.ShowFloatingText(text, spawnPosition, color, fontSize, lockRotation: true, style: FloatingTextManager.TextStyle.Movement);
        
        if (showDebugLogs)
        {
            Debug.Log($"[WallJumpXP] Showed floating text at {spawnPosition}");
        }
    }
    
    /// <summary>
    /// Reset chain
    /// </summary>
    public void ResetChain()
    {
        if (currentChainLevel > 0 && showDebugLogs)
        {
            Debug.Log($"[WallJumpXP] Chain ended at x{currentChainLevel}");
        }
        currentChainLevel = 0;
    }
    
    /// <summary>
    /// Check chain timeout
    /// </summary>
    void Update()
    {
        if (currentChainLevel > 0)
        {
            float timeSinceLastJump = Time.time - lastWallJumpTime;
            if (timeSinceLastJump > chainTimeWindow)
            {
                ResetChain();
            }
        }
    }
    
    /// <summary>
    /// Get session stats
    /// </summary>
    public (int jumps, int xp, int maxChain) GetSessionStats()
    {
        return (totalWallJumpsThisSession, totalXPEarnedFromWallJumps, currentChainLevel);
    }
    
    /// <summary>
    /// Reset session
    /// </summary>
    public void ResetSession()
    {
        currentChainLevel = 0;
        totalWallJumpsThisSession = 0;
        totalXPEarnedFromWallJumps = 0;
        lastWallJumpTime = -999f;
        if (showDebugLogs) Debug.Log("[WallJumpXP] Session reset");
    }
    
    // ========================================
    // PUBLIC ACCESSORS - For CognitiveFeedbackManager & Other Systems
    // COHERENCE: Eliminates reflection anti-pattern
    // ========================================
    
    /// <summary>
    /// Get current wall jump chain level (0 = no active chain)
    /// </summary>
    public int CurrentChainLevel => currentChainLevel;
    
    /// <summary>
    /// Get time of last wall jump (for chain tracking)
    /// </summary>
    public float LastWallJumpTime => lastWallJumpTime;
    
    /// <summary>
    /// Check if chain is currently active
    /// </summary>
    public bool IsChainActive => currentChainLevel > 0 && (Time.time - lastWallJumpTime) <= chainTimeWindow;
    
    /// <summary>
    /// Get chain time window (for external calculations)
    /// </summary>
    public float ChainTimeWindow => chainTimeWindow;
}
