using UnityEngine;
using TMPro;
using System.Collections;
using GeminiGauntlet.Progression;

/// <summary>
/// 🎯 WALL JUMP XP CHAIN SYSTEM - WORLD SPACE VERSION
/// Text appears in 3D space where you wall jump - FLY THROUGH IT!
/// ULTRA OPTIMIZED for potato PCs - zero GC allocations, object pooling
/// </summary>
public class WallJumpXPWorldSpace : MonoBehaviour
{
    public static WallJumpXPWorldSpace Instance { get; private set; }
    
    [Header("=== XP CHAIN SETTINGS ===")]
    [Tooltip("Base XP for first wall jump")]
    [SerializeField] private int baseWallJumpXP = 5;
    
    [Tooltip("XP multiplier per chain level (exponential growth!)")]
    [SerializeField] private float xpMultiplierPerChain = 1.5f;
    
    [Tooltip("Max chain level (caps XP growth)")]
    [SerializeField] private int maxChainLevel = 10;
    
    [Tooltip("Time window to continue chain (seconds)")]
    [SerializeField] private float chainTimeWindow = 2.0f;
    
    [Header("=== WORLD SPACE DISPLAY ===")]
    [Tooltip("Distance from wall jump position")]
    [SerializeField] private float spawnDistance = 100f; // SCALED for 320-unit character! (closer for testing)
    
    [Tooltip("Height offset from player")]
    [SerializeField] private float heightOffset = 50f; // SCALED for 320-unit character! (closer for testing)
    
    [Tooltip("Text floats upward speed")]
    [SerializeField] private float floatSpeed = 100f; // SCALED for 320-unit character!
    
    [Tooltip("How long the XP text stays visible (seconds)")]
    [SerializeField] private float textLifetime = 3f; // Duration XP text is visible
    
    [Tooltip("Text size in world units")]
    [SerializeField] private float textSize = 50f; // SCALED for 320-unit character!
    
    [Header("=== PERFORMANCE (POTATO MODE!) ===")]
    [Tooltip("Max active text objects (pool size)")]
    [SerializeField] private int maxPoolSize = 5;
    
    [Tooltip("Reuse text objects (CRITICAL for performance!)")]
    [SerializeField] private bool enableObjectPooling = true;
    
    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugLogs = true; // Enable to see what's happening!
    
    // Chain state
    private int currentChainLevel = 0;
    private float lastWallJumpTime = -999f;
    private int totalWallJumpsThisSession = 0;
    private int totalXPEarnedFromWallJumps = 0;
    
    // Object pooling (ZERO GC ALLOCATIONS!)
    private GameObject[] textPool;
    private int poolIndex = 0;
    
    // Chain level titles
    private readonly string[] chainTitles = new string[]
    {
        "",                    // 0
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
    
    // Chain colors
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
    
    // Cached references (avoid GetComponent calls!)
    private Transform playerTransform;
    private Camera mainCamera;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (showDebugLogs) Debug.Log("[WallJumpXP] World Space instance created");
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        // Cache references (PERFORMANCE!)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        
        mainCamera = Camera.main;
        
        // Pre-create object pool (ZERO runtime allocations!)
        if (enableObjectPooling)
        {
            CreateObjectPool();
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[WallJumpXP] Initialized - Pool size: {maxPoolSize}, Pooling: {enableObjectPooling}");
        }
    }
    
    /// <summary>
    /// Pre-create text objects for pooling (CRITICAL for performance!)
    /// </summary>
    private void CreateObjectPool()
    {
        textPool = new GameObject[maxPoolSize];
        
        for (int i = 0; i < maxPoolSize; i++)
        {
            textPool[i] = CreateTextObject();
            textPool[i].SetActive(false);
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[WallJumpXP] Created pool of {maxPoolSize} text objects (ZERO GC!)");
        }
    }
    
    /// <summary>
    /// Create a single text object (world space)
    /// </summary>
    private GameObject CreateTextObject()
    {
        GameObject textObj = new GameObject("WallJumpXP_Text");
        textObj.transform.SetParent(transform);
        textObj.layer = 0; // Default layer (ensure it's visible!)
        
        // Add TextMeshPro (world space!)
        TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
        tmp.fontSize = 400; // SCALED for 320-unit character (was 4)
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white; // Ensure visible color
        
        // CRITICAL: Set render mode
        tmp.renderer.sortingLayerID = 0;
        tmp.renderer.sortingOrder = 100;
        
        // CRITICAL: TextMeshPro needs a font! It should auto-assign, but let's check
        if (tmp.font == null)
        {
            Debug.LogWarning("[WallJumpXP] TextMeshPro has no font! It will use default font.");
        }
        
        // Add outline for readability
        tmp.outlineWidth = 0.2f;
        tmp.outlineColor = Color.black;
        
        // Add animator component (for floating)
        textObj.AddComponent<WallJumpXPTextAnimator>();
        
        if (showDebugLogs)
        {
            Debug.Log($"[WallJumpXP] Created text object - Font: {tmp.font?.name ?? "NULL"}, Material: {tmp.fontMaterial?.name ?? "NULL"}, Layer: {textObj.layer}");
        }
        
        return textObj;
    }
    
    /// <summary>
    /// Get text object from pool (ZERO allocations!)
    /// </summary>
    private GameObject GetPooledObject()
    {
        if (!enableObjectPooling)
        {
            return CreateTextObject();
        }
        
        // Round-robin through pool
        GameObject obj = textPool[poolIndex];
        poolIndex = (poolIndex + 1) % maxPoolSize;
        
        return obj;
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
        
        // Calculate XP
        int xpEarned = CalculateXP(currentChainLevel);
        totalXPEarnedFromWallJumps += xpEarned;
        
        // Grant XP through existing system
        if (XPManager.Instance != null)
        {
            XPManager.Instance.GrantXP(xpEarned, "Movement", $"Wall Jump x{currentChainLevel}");
        }
        
        // Spawn world space text
        SpawnWorldSpaceText(wallJumpPosition, currentChainLevel, xpEarned);
        
        if (showDebugLogs)
        {
            Debug.Log($"[WallJumpXP] 🎯 CHAIN x{currentChainLevel}! Earned {xpEarned} XP");
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
    /// Spawn text in world space (FLY THROUGH IT!)
    /// </summary>
    private void SpawnWorldSpaceText(Vector3 wallJumpPosition, int chainLevel, int xpEarned)
    {
        // Get pooled object (ZERO allocations!)
        GameObject textObj = GetPooledObject();
        if (textObj == null)
        {
            Debug.LogError("[WallJumpXP] Failed to get pooled object!");
            return;
        }
        
        // Position: In front of player, at wall jump height
        Vector3 spawnPos = wallJumpPosition;
        
        // FIXED: Use camera's FORWARD direction (where it's looking), not direction TO camera!
        if (mainCamera != null && playerTransform != null)
        {
            // Get camera's forward direction (horizontal only, ignore vertical look)
            Vector3 cameraForward = mainCamera.transform.forward;
            cameraForward.y = 0; // Flatten to horizontal plane
            
            if (cameraForward.sqrMagnitude < 0.01f)
            {
                // Camera pointing straight up/down, use player forward instead
                cameraForward = playerTransform.forward;
                cameraForward.y = 0;
            }
            
            cameraForward.Normalize();
            
            // Spawn text in front of where camera is looking
            spawnPos += cameraForward * spawnDistance;
            
            if (showDebugLogs)
            {
                Debug.Log($"[WallJumpXP] Camera forward (horizontal): {cameraForward}, Spawn distance: {spawnDistance}");
                Debug.Log($"[WallJumpXP] Wall jump pos: {wallJumpPosition}, Spawn pos: {spawnPos}");
            }
        }
        else
        {
            Debug.LogWarning("[WallJumpXP] Camera or player transform is null!");
        }
        
        spawnPos.y += heightOffset;
        
        textObj.transform.position = spawnPos;
        
        // Face camera (billboard effect)
        if (mainCamera != null)
        {
            textObj.transform.rotation = Quaternion.LookRotation(textObj.transform.position - mainCamera.transform.position);
        }
        
        // Setup text
        TextMeshPro tmp = textObj.GetComponent<TextMeshPro>();
        if (tmp != null)
        {
            // Get chain title and color
            string title = chainLevel < chainTitles.Length ? chainTitles[chainLevel] : chainTitles[chainTitles.Length - 1];
            Color color = chainLevel < chainColors.Length ? chainColors[chainLevel] : chainColors[chainColors.Length - 1];
            
            // Build text (optimized string format)
            tmp.text = $"{title}\nx{chainLevel} CHAIN\n<color=#FFD700>+{xpEarned} XP</color>";
            tmp.color = color;
            
            // Scale based on chain level
            float scale = textSize * (1f + chainLevel * 0.1f);
            scale = Mathf.Min(scale, textSize * 2f);
            textObj.transform.localScale = Vector3.one * scale;
            
            // Force mesh update
            tmp.ForceMeshUpdate();
            
            if (showDebugLogs)
            {
                Debug.Log($"[WallJumpXP] TEXT SPAWNED at position: {spawnPos}");
                Debug.Log($"[WallJumpXP] Text: '{tmp.text}', Color: {color}, Scale: {scale}");
                Debug.Log($"[WallJumpXP] Transform scale: {textObj.transform.localScale}, Active: {textObj.activeSelf}");
                Debug.Log($"[WallJumpXP] TMP enabled: {tmp.enabled}, Renderer: {tmp.GetComponent<Renderer>()?.enabled}, Bounds: {tmp.bounds}");
            }
        }
        else
        {
            Debug.LogError("[WallJumpXP] TextMeshPro component is null!");
        }
        
        // Activate and animate
        textObj.SetActive(true);
        
        WallJumpXPTextAnimator animator = textObj.GetComponent<WallJumpXPTextAnimator>();
        if (animator != null)
        {
            animator.StartAnimation(floatSpeed, textLifetime);
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[WallJumpXP] ✅ TEXT ACTIVATED! Active: {textObj.activeSelf}, Position: {textObj.transform.position}, Scale: {textObj.transform.localScale}");
            if (tmp != null)
            {
                Debug.Log($"[WallJumpXP] ✅ Renderer visible: {tmp.renderer.isVisible}, Enabled: {tmp.enabled}, Material: {tmp.fontMaterial?.name}");
            }
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
}

/// <summary>
/// Animates world space text (float up, fade out)
/// ULTRA OPTIMIZED - no coroutines, no allocations!
/// </summary>
public class WallJumpXPTextAnimator : MonoBehaviour
{
    private float floatSpeed;
    private float lifetime;
    private float spawnTime;
    private bool isAnimating;
    
    private TextMeshPro tmp;
    private Color originalColor;
    
    void Awake()
    {
        tmp = GetComponent<TextMeshPro>();
    }
    
    public void StartAnimation(float speed, float life)
    {
        floatSpeed = speed;
        lifetime = life;
        spawnTime = Time.time;
        isAnimating = true;
        
        if (tmp != null)
        {
            originalColor = tmp.color;
        }
    }
    
    void Update()
    {
        if (!isAnimating) return;
        
        float elapsed = Time.time - spawnTime;
        
        // Float upward
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;
        
        // Fade out (last 30% of lifetime)
        if (tmp != null && elapsed > lifetime * 0.7f)
        {
            float fadeProgress = (elapsed - lifetime * 0.7f) / (lifetime * 0.3f);
            Color c = originalColor;
            c.a = Mathf.Lerp(1f, 0f, fadeProgress);
            tmp.color = c;
        }
        
        // Deactivate when done
        if (elapsed >= lifetime)
        {
            isAnimating = false;
            gameObject.SetActive(false);
        }
    }
}
