using UnityEngine;
using GeminiGauntlet.UI;

/// <summary>
/// 🔥 COMBO MULTIPLIER SYSTEM - ULTIMATE EDITION
/// Tracks wall jumps + tricks and creates INSANE multipliers!
/// The more you chain, the MORE XP YOU GET!
/// 🤝 BFFL INTEGRATION: Fully aware of trick↔walljump transitions for EPIC bonus multipliers!
/// </summary>
public class ComboMultiplierSystem : MonoBehaviour
{
    public static ComboMultiplierSystem Instance { get; private set; }
    
    [Header("=== COMBO SETTINGS ===")]
    [SerializeField] private float comboTimeWindow = 3f; // Time to continue combo
    [SerializeField] private float wallJumpComboValue = 1f; // Each wall jump adds 1.0 to combo
    [SerializeField] private float trickComboValue = 2f; // Each trick adds 2.0 to combo
    [SerializeField] private float comboDecayRate = 0.5f; // How fast combo decays per second
    
    [Header("=== 🤝 TRANSITION BONUSES (BFFL REWARDS!) ===")]
    [Tooltip("Bonus multiplier when transitioning trick→walljump")]
    [SerializeField] private float trickToWallJumpBonus = 0.5f; // +50% bonus!
    [Tooltip("Bonus multiplier when transitioning walljump→trick")]
    [SerializeField] private float wallJumpToTrickBonus = 0.5f; // +50% bonus!
    [Tooltip("Massive bonus for perfect BFFL transitions (no ground touch)")]
    [SerializeField] private float seamlessTransitionBonus = 1f; // +100% EPIC bonus!
    [Tooltip("Time window to detect seamless transitions (seconds)")]
    [SerializeField] private float seamlessTransitionWindow = 0.3f; // Must be quick!
    
    [Header("=== MULTIPLIER CALCULATION ===")]
    [SerializeField] private float baseMultiplier = 1f; // Starting multiplier
    [SerializeField] private float multiplierPerComboPoint = 0.25f; // +0.25x per combo point
    [SerializeField] private float maxMultiplier = 10f; // Cap at 10x
    
    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool showTransitionLogs = true; // Extra logs for BFFL transitions
    
    // Combo state
    private float currentComboPoints = 0f;
    private float lastComboTime = -999f;
    private int wallJumpsInCombo = 0;
    private int tricksInCombo = 0;
    private float currentMultiplier = 1f;
    
    // 🤝 BFFL TRANSITION TRACKING
    private enum ComboAction { None, WallJump, Trick }
    private ComboAction lastAction = ComboAction.None;
    private float lastActionTime = -999f;
    private int seamlessTransitions = 0; // Track perfect transitions
    private bool wasAirborneLastAction = false; // Track if last action was airborne
    
    // Session stats
    private float highestComboPoints = 0f;
    private float highestMultiplier = 1f;
    private int totalCombos = 0;
    private int totalSeamlessTransitions = 0; // Track all-time perfect transitions
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (showDebugLogs) Debug.Log("[ComboSystem] Instance created");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Update()
    {
        // Decay combo over time
        if (currentComboPoints > 0)
        {
            float timeSinceLastCombo = Time.time - lastComboTime;
            
            if (timeSinceLastCombo > comboTimeWindow)
            {
                // Combo expired!
                if (showDebugLogs && currentComboPoints > 0)
                {
                    Debug.Log($"[COMBO] 💔 COMBO ENDED! Final: {currentComboPoints:F1} points, {currentMultiplier:F2}x multiplier");
                    
                    // Show final combo as floating text
                    ShowComboEndText();
                }
                
                ResetCombo();
            }
        }
    }
    
    /// <summary>
    /// Show combo end summary using floating text system
    /// </summary>
    private void ShowComboEndText()
    {
        if (FloatingTextManager.Instance == null) return;
        
        string comboText = $"{currentMultiplier:F1}x COMBO!";
        if (seamlessTransitions > 0)
        {
            comboText = $"{currentMultiplier:F1}x ✨×{seamlessTransitions} COMBO!";
        }
        
        // Determine color based on multiplier
        Color comboColor = Color.white;
        if (currentMultiplier >= 5f) comboColor = new Color(1f, 0.5f, 0f); // Epic orange
        else if (currentMultiplier >= 3f) comboColor = Color.yellow; // Good yellow
        
        // Show at screen center (will be visible!)
        Vector3 screenCenter = Camera.main != null 
            ? Camera.main.ScreenToWorldPoint(new Vector3(Screen.width * 0.5f, Screen.height * 0.6f, 5f))
            : Vector3.up * 2f;
        
        FloatingTextManager.Instance.ShowText(
            comboText, 
            screenCenter, 
            comboColor, 
            2f, // Duration
            1.5f // Size multiplier
        );
    }
    
    /// <summary>
    /// Add wall jump to combo
    /// 🤝 BFFL: Detects transitions and awards bonuses!
    /// </summary>
    public void AddWallJump(bool isAirborne = true)
    {
        float bonusMultiplier = 1f;
        float timeSinceLastAction = Time.time - lastActionTime;
        bool isSeamless = false;
        
        // 🤝 BFFL: Detect trick→walljump transition
        if (lastAction == ComboAction.Trick && timeSinceLastAction <= seamlessTransitionWindow)
        {
            // SEAMLESS TRANSITION DETECTED!
            bonusMultiplier += seamlessTransitionBonus;
            seamlessTransitions++;
            totalSeamlessTransitions++;
            isSeamless = true;
            
            if (showTransitionLogs)
            {
                Debug.Log($"🤝✨ [COMBO] SEAMLESS TRANSITION! Trick→WallJump in {timeSinceLastAction:F2}s! Bonus: +{seamlessTransitionBonus * 100}%");
            }
            
            // Show instant visual feedback!
            ShowSeamlessTransitionFeedback("TRICK→WALLJUMP!");
        }
        else if (lastAction == ComboAction.Trick)
        {
            // Standard transition bonus
            bonusMultiplier += trickToWallJumpBonus;
            
            if (showTransitionLogs)
            {
                Debug.Log($"🤝 [COMBO] Trick→WallJump transition! Bonus: +{trickToWallJumpBonus * 100}%");
            }
        }
        
        // Apply combo value with bonus
        float points = wallJumpComboValue * bonusMultiplier;
        currentComboPoints += points;
        wallJumpsInCombo++;
        lastComboTime = Time.time;
        lastActionTime = Time.time;
        lastAction = ComboAction.WallJump;
        wasAirborneLastAction = isAirborne;
        
        UpdateMultiplier();
        
        if (showDebugLogs)
        {
            string bonusText = bonusMultiplier > 1f ? $" (+{(bonusMultiplier - 1f) * 100:F0}% BONUS!)" : "";
            Debug.Log($"[COMBO] 🧗 Wall Jump! +{points:F1} pts{bonusText} | Total: {currentComboPoints:F1} pts, {currentMultiplier:F2}x");
        }
    }
    
    /// <summary>
    /// Add trick to combo
    /// 🤝 BFFL: Detects transitions and awards bonuses!
    /// </summary>
    public void AddTrick(float trickAwesomeness = 1f, bool isAirborne = true)
    {
        float bonusMultiplier = 1f;
        float timeSinceLastAction = Time.time - lastActionTime;
        bool isSeamless = false;
        
        // 🤝 BFFL: Detect walljump→trick transition
        if (lastAction == ComboAction.WallJump && timeSinceLastAction <= seamlessTransitionWindow)
        {
            // SEAMLESS TRANSITION DETECTED!
            bonusMultiplier += seamlessTransitionBonus;
            seamlessTransitions++;
            totalSeamlessTransitions++;
            isSeamless = true;
            
            if (showTransitionLogs)
            {
                Debug.Log($"🤝✨ [COMBO] SEAMLESS TRANSITION! WallJump→Trick in {timeSinceLastAction:F2}s! Bonus: +{seamlessTransitionBonus * 100}%");
            }
            
            // Show instant visual feedback!
            ShowSeamlessTransitionFeedback("WALLJUMP→TRICK!");
        }
        else if (lastAction == ComboAction.WallJump)
        {
            // Standard transition bonus
            bonusMultiplier += wallJumpToTrickBonus;
            
            if (showTransitionLogs)
            {
                Debug.Log($"🤝 [COMBO] WallJump→Trick transition! Bonus: +{wallJumpToTrickBonus * 100}%");
            }
        }
        
        // Apply combo value with bonus
        float points = trickComboValue * trickAwesomeness * bonusMultiplier;
        currentComboPoints += points;
        tricksInCombo++;
        lastComboTime = Time.time;
        lastActionTime = Time.time;
        lastAction = ComboAction.Trick;
        wasAirborneLastAction = isAirborne;
        
        UpdateMultiplier();
        
        if (showDebugLogs)
        {
            string bonusText = bonusMultiplier > 1f ? $" (+{(bonusMultiplier - 1f) * 100:F0}% BONUS!)" : "";
            Debug.Log($"[COMBO] 🎪 Trick! +{points:F1} pts{bonusText} | Total: {currentComboPoints:F1} pts, {currentMultiplier:F2}x");
        }
    }
    
    /// <summary>
    /// Show visual feedback for seamless transitions
    /// </summary>
    private void ShowSeamlessTransitionFeedback(string transitionText)
    {
        if (FloatingTextManager.Instance == null) return;
        
        // Show epic sparkle text at screen center
        Vector3 screenCenter = Camera.main != null 
            ? Camera.main.ScreenToWorldPoint(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 5f))
            : Vector3.up * 2f;
        
        FloatingTextManager.Instance.ShowText(
            $"✨ {transitionText} ✨", 
            screenCenter, 
            new Color(1f, 0.8f, 0f), // Gold color
            1.5f, // Duration
            2f // Larger size!
        );
    }
    
    /// <summary>
    /// Calculate current multiplier based on combo points
    /// </summary>
    private void UpdateMultiplier()
    {
        currentMultiplier = baseMultiplier + (currentComboPoints * multiplierPerComboPoint);
        currentMultiplier = Mathf.Min(currentMultiplier, maxMultiplier);
        
        // Update session stats
        if (currentComboPoints > highestComboPoints)
        {
            highestComboPoints = currentComboPoints;
        }
        if (currentMultiplier > highestMultiplier)
        {
            highestMultiplier = currentMultiplier;
        }
    }
    
    /// <summary>
    /// Get current multiplier
    /// </summary>
    public float GetMultiplier()
    {
        return currentMultiplier;
    }
    
    /// <summary>
    /// Check if combo is active
    /// </summary>
    public bool IsComboActive()
    {
        return currentComboPoints > 0 && (Time.time - lastComboTime) <= comboTimeWindow;
    }
    
    /// <summary>
    /// Get combo info for display
    /// 🤝 BFFL: Now includes seamless transition count!
    /// </summary>
    public (int wallJumps, int tricks, float points, float multiplier, int seamlessTransitions) GetComboInfo()
    {
        return (wallJumpsInCombo, tricksInCombo, currentComboPoints, currentMultiplier, seamlessTransitions);
    }
    
    /// <summary>
    /// Get detailed combo string for UI display
    /// </summary>
    public string GetComboDisplayString()
    {
        if (!IsComboActive()) return "";
        
        string result = $"{currentMultiplier:F1}x";
        
        if (seamlessTransitions > 0)
        {
            result += $" ✨×{seamlessTransitions}"; // Show seamless transition count
        }
        
        return result;
    }
    
    /// <summary>
    /// Get last action type (for UI/feedback)
    /// </summary>
    public string GetLastActionName()
    {
        return lastAction switch
        {
            ComboAction.WallJump => "Wall Jump",
            ComboAction.Trick => "Trick",
            _ => "None"
        };
    }
    
    /// <summary>
    /// Reset combo
    /// </summary>
    public void ResetCombo()
    {
        if (currentComboPoints > 0)
        {
            totalCombos++;
            
            // Log epic combos
            if (seamlessTransitions >= 3 && showDebugLogs)
            {
                Debug.Log($"🔥 [COMBO] LEGENDARY COMBO ENDED! {seamlessTransitions} seamless transitions! {currentMultiplier:F2}x multiplier!");
            }
        }
        
        currentComboPoints = 0f;
        wallJumpsInCombo = 0;
        tricksInCombo = 0;
        currentMultiplier = 1f;
        lastComboTime = -999f;
        lastAction = ComboAction.None;
        lastActionTime = -999f;
        seamlessTransitions = 0;
        wasAirborneLastAction = false;
    }
    
    /// <summary>
    /// Get session stats
    /// 🤝 BFFL: Now includes seamless transition tracking!
    /// </summary>
    public (float highestPoints, float highestMult, int totalCombos, int seamlessTransitions) GetSessionStats()
    {
        return (highestComboPoints, highestMultiplier, totalCombos, totalSeamlessTransitions);
    }
    
    /// <summary>
    /// Reset session stats
    /// </summary>
    public void ResetSession()
    {
        ResetCombo();
        highestComboPoints = 0f;
        highestMultiplier = 1f;
        totalCombos = 0;
        totalSeamlessTransitions = 0;
        
        if (showDebugLogs) Debug.Log("[COMBO] Session reset - ready for EPIC combos!");
    }
}
