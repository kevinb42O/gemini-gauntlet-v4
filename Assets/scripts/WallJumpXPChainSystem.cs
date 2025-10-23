using UnityEngine;
using TMPro;
using System.Collections;
using GeminiGauntlet.Progression;

/// <summary>
/// 🎯 WALL JUMP XP CHAIN SYSTEM - AAA Quality
/// Rewards players for consecutive wall jumps with escalating XP and visual feedback
/// Designed by a UX/UI expert to feel AMAZING!
/// </summary>
public class WallJumpXPChainSystem : MonoBehaviour
{
    public static WallJumpXPChainSystem Instance { get; private set; }
    
    [Header("=== XP CHAIN SETTINGS ===")]
    [Tooltip("Base XP for first wall jump")]
    [SerializeField] private int baseWallJumpXP = 5;
    
    [Tooltip("XP multiplier per chain level (exponential growth!)")]
    [SerializeField] private float xpMultiplierPerChain = 1.5f;
    
    [Tooltip("Max chain level (caps XP growth)")]
    [SerializeField] private int maxChainLevel = 10;
    
    [Tooltip("Time window to continue chain (seconds)")]
    [SerializeField] private float chainTimeWindow = 2.0f;
    
    [Header("=== VISUAL FEEDBACK ===")]
    [Tooltip("UI Text for displaying chain info (will be created if null)")]
    [SerializeField] private TextMeshProUGUI chainDisplayText;
    
    [Tooltip("Position offset from screen center")]
    [SerializeField] private Vector2 displayOffset = new Vector2(0, 200);
    
    [Tooltip("Enable particle effects on wall jump")]
    [SerializeField] private bool enableParticles = true;
    
    [Header("=== AUDIO ===")]
    [Tooltip("Play audio feedback on wall jump")]
    [SerializeField] private bool enableAudio = true;
    
    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugLogs = true;
    
    // Chain state
    private int currentChainLevel = 0;
    private float lastWallJumpTime = -999f;
    private int totalWallJumpsThisSession = 0;
    private int totalXPEarnedFromWallJumps = 0;
    
    // UI state
    private GameObject chainUIContainer;
    private CanvasGroup chainCanvasGroup;
    private Coroutine fadeCoroutine;
    
    // Chain level titles (for that AAA feel!)
    private readonly string[] chainTitles = new string[]
    {
        "",                    // 0 - no chain
        "WALL JUMP!",         // 1
        "DOUBLE!",            // 2
        "TRIPLE!",            // 3
        "QUAD!",              // 4
        "MEGA!",              // 5
        "ULTRA!",             // 6
        "MONSTER!",           // 7
        "LEGENDARY!",         // 8
        "GODLIKE!",           // 9
        "UNSTOPPABLE!!!"      // 10+
    };
    
    // Chain colors (visual progression!)
    private readonly Color[] chainColors = new Color[]
    {
        Color.white,                           // 0
        new Color(1f, 1f, 1f),                // 1 - White
        new Color(0.5f, 1f, 0.5f),            // 2 - Light Green
        new Color(0f, 1f, 0f),                // 3 - Green
        new Color(0f, 1f, 1f),                // 4 - Cyan
        new Color(0.5f, 0.5f, 1f),            // 5 - Light Blue
        new Color(1f, 0.5f, 0f),              // 6 - Orange
        new Color(1f, 0f, 0f),                // 7 - Red
        new Color(1f, 0f, 1f),                // 8 - Magenta
        new Color(1f, 1f, 0f),                // 9 - Yellow
        new Color(1f, 0.8f, 0f)               // 10+ - Gold
    };
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[WallJumpXP] Instance created");
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        SetupUI();
        
        if (showDebugLogs)
        {
            Debug.Log($"[WallJumpXP] System initialized - Base XP: {baseWallJumpXP}, Multiplier: {xpMultiplierPerChain}x, Max Chain: {maxChainLevel}");
        }
    }
    
    /// <summary>
    /// Setup the UI dynamically (no prefab needed!)
    /// </summary>
    private void SetupUI()
    {
        // Find or create canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[WallJumpXP] No Canvas found in scene! UI will not display.");
            return;
        }
        
        // Create UI container
        chainUIContainer = new GameObject("WallJumpChainUI");
        chainUIContainer.transform.SetParent(canvas.transform, false);
        
        // Add RectTransform
        RectTransform rectTransform = chainUIContainer.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = displayOffset;
        rectTransform.sizeDelta = new Vector2(600, 200);
        
        // Add CanvasGroup for fading
        chainCanvasGroup = chainUIContainer.AddComponent<CanvasGroup>();
        chainCanvasGroup.alpha = 0f;
        
        // Create text element
        GameObject textObj = new GameObject("ChainText");
        textObj.transform.SetParent(chainUIContainer.transform, false);
        
        chainDisplayText = textObj.AddComponent<TextMeshProUGUI>();
        chainDisplayText.fontSize = 48;
        chainDisplayText.alignment = TextAlignmentOptions.Center;
        chainDisplayText.fontStyle = FontStyles.Bold;
        chainDisplayText.color = Color.white;
        chainDisplayText.text = "";
        
        // Setup text rect
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        
        // Add outline for readability
        var outline = textObj.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(3, -3);
        
        if (showDebugLogs)
        {
            Debug.Log("[WallJumpXP] UI created dynamically!");
        }
    }
    
    /// <summary>
    /// Call this from AAAMovementController when a wall jump is performed
    /// </summary>
    public void OnWallJumpPerformed()
    {
        float timeSinceLastJump = Time.time - lastWallJumpTime;
        
        // Check if chain continues or resets
        if (timeSinceLastJump <= chainTimeWindow && currentChainLevel > 0)
        {
            // CHAIN CONTINUES!
            currentChainLevel = Mathf.Min(currentChainLevel + 1, maxChainLevel);
        }
        else
        {
            // NEW CHAIN STARTS
            currentChainLevel = 1;
        }
        
        lastWallJumpTime = Time.time;
        totalWallJumpsThisSession++;
        
        // Calculate XP for this wall jump
        int xpEarned = CalculateXP(currentChainLevel);
        totalXPEarnedFromWallJumps += xpEarned;
        
        // Grant XP through existing system
        if (XPManager.Instance != null)
        {
            XPManager.Instance.GrantXP(xpEarned, "Movement", $"Wall Jump x{currentChainLevel}");
        }
        
        // Visual feedback
        ShowChainFeedback(currentChainLevel, xpEarned);
        
        // Audio feedback
        if (enableAudio)
        {
            PlayChainAudio(currentChainLevel);
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[WallJumpXP] 🎯 CHAIN x{currentChainLevel}! Earned {xpEarned} XP | Total Session: {totalWallJumpsThisSession} jumps, {totalXPEarnedFromWallJumps} XP");
        }
    }
    
    /// <summary>
    /// Calculate XP based on chain level (exponential growth!)
    /// </summary>
    private int CalculateXP(int chainLevel)
    {
        if (chainLevel <= 0) return 0;
        
        // Exponential formula: baseXP * (multiplier ^ (level - 1))
        float xp = baseWallJumpXP * Mathf.Pow(xpMultiplierPerChain, chainLevel - 1);
        return Mathf.RoundToInt(xp);
    }
    
    /// <summary>
    /// Show AMAZING visual feedback!
    /// </summary>
    private void ShowChainFeedback(int chainLevel, int xpEarned)
    {
        if (chainDisplayText == null) return;
        
        // Get chain title
        string title = chainLevel < chainTitles.Length ? chainTitles[chainLevel] : chainTitles[chainTitles.Length - 1];
        
        // Get chain color
        Color color = chainLevel < chainColors.Length ? chainColors[chainLevel] : chainColors[chainColors.Length - 1];
        
        // Build display text
        string displayText = $"<size=72>{title}</size>\n";
        displayText += $"<size=48>x{chainLevel} CHAIN</size>\n";
        displayText += $"<size=36><color=#FFD700>+{xpEarned} XP</color></size>";
        
        chainDisplayText.text = displayText;
        chainDisplayText.color = color;
        
        // Animate!
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(AnimateChainDisplay(chainLevel));
    }
    
    /// <summary>
    /// Animate the chain display (fade in, scale, fade out)
    /// </summary>
    private IEnumerator AnimateChainDisplay(int chainLevel)
    {
        if (chainCanvasGroup == null) yield break;
        
        // Scale based on chain level (bigger = more impressive!)
        float targetScale = 1f + (chainLevel * 0.1f);
        targetScale = Mathf.Min(targetScale, 2f); // Cap at 2x
        
        chainUIContainer.transform.localScale = Vector3.one * targetScale * 0.5f;
        
        // PHASE 1: Fade in + Scale up (0.2s)
        float elapsed = 0f;
        float fadeInDuration = 0.2f;
        
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeInDuration;
            
            chainCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            chainUIContainer.transform.localScale = Vector3.Lerp(Vector3.one * targetScale * 0.5f, Vector3.one * targetScale, t);
            
            yield return null;
        }
        
        chainCanvasGroup.alpha = 1f;
        chainUIContainer.transform.localScale = Vector3.one * targetScale;
        
        // PHASE 2: Hold (1.0s for chain > 3, 0.5s otherwise)
        float holdDuration = chainLevel > 3 ? 1.0f : 0.5f;
        yield return new WaitForSeconds(holdDuration);
        
        // PHASE 3: Fade out (0.3s)
        elapsed = 0f;
        float fadeOutDuration = 0.3f;
        
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;
            
            chainCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            
            yield return null;
        }
        
        chainCanvasGroup.alpha = 0f;
    }
    
    /// <summary>
    /// Play SATISFYING audio feedback (pitch increases with chain level!)
    /// This is what makes wall jumping feel AMAZING!
    /// </summary>
    private void PlayChainAudio(int chainLevel)
    {
        // Play the satisfying XP notification sound with pitch scaling
        // The GameSounds helper automatically handles pitch based on chain level
        GeminiGauntlet.Audio.GameSounds.PlayWallJumpXPNotification(transform.position, chainLevel, 1.0f);
        
        if (showDebugLogs)
        {
            Debug.Log($"[WallJumpXP] 🎵 Satisfying XP notification sound played for chain x{chainLevel}!");
        }
    }
    
    /// <summary>
    /// Reset chain (called when player lands or after timeout)
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
    /// Check if chain should timeout
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
    /// Get session stats (for end-game summary)
    /// </summary>
    public (int jumps, int xp, int maxChain) GetSessionStats()
    {
        return (totalWallJumpsThisSession, totalXPEarnedFromWallJumps, currentChainLevel);
    }
    
    /// <summary>
    /// Reset session stats (called on new game)
    /// </summary>
    public void ResetSession()
    {
        currentChainLevel = 0;
        totalWallJumpsThisSession = 0;
        totalXPEarnedFromWallJumps = 0;
        lastWallJumpTime = -999f;
        
        if (showDebugLogs)
        {
            Debug.Log("[WallJumpXP] Session reset");
        }
    }
}
