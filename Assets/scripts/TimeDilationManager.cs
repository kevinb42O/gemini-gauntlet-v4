using UnityEngine;

/// <summary>
/// Centralized time dilation system for cinematic slow-motion effects.
/// Manages Time.timeScale with smooth transitions and priority handling.
/// PERFORMANCE: Minimal overhead - only updates when time scale changes needed.
/// </summary>
public class TimeDilationManager : MonoBehaviour
{
    public static TimeDilationManager Instance { get; private set; }
    
    [Header("=== 🎬 TIME DILATION SETTINGS ===")]
    [Tooltip("Enable time dilation system globally")]
    [SerializeField] private bool enableTimeDilation = true;
    
    [Tooltip("Default transition speed for time scale changes (higher = faster)")]
    [SerializeField] private float defaultTransitionSpeed = 10f;
    
    [Tooltip("Show debug logs for time dilation changes")]
    [SerializeField] private bool showDebugLogs = false;
    
    // Current state
    private float targetTimeScale = 1f;
    private float currentTimeScale = 1f;
    private float transitionSpeed = 10f;
    
    // Active dilation sources (priority system)
    private struct DilationRequest
    {
        public bool active;
        public float timeScale;
        public float customTransitionSpeed;
        public string sourceName;
    }
    
    private DilationRequest trickSystemRequest;
    private DilationRequest powerupRequest;
    private DilationRequest customRequest;
    
    // Performance optimization
    private bool needsUpdate = false;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        
        // Initialize to normal time
        currentTimeScale = Time.timeScale;
        targetTimeScale = 1f;
        
        if (showDebugLogs)
        {
            Debug.Log("[TimeDilationManager] ✅ Initialized - Ready for cinematic slow-mo!");
        }
    }
    
    void Update()
    {
        if (!enableTimeDilation)
        {
            // Ensure normal time if disabled
            if (Time.timeScale != 1f)
            {
                Time.timeScale = 1f;
                currentTimeScale = 1f;
            }
            return;
        }
        
        // PERFORMANCE: Only update if there's a change needed
        if (!needsUpdate && Mathf.Approximately(currentTimeScale, targetTimeScale))
        {
            return;
        }
        
        // Calculate target based on active requests (PRIORITY: Slowest wins for most dramatic effect)
        float newTarget = 1f;
        float newTransitionSpeed = defaultTransitionSpeed;
        string activeSource = "None";
        
        // Check all sources and pick the slowest time scale
        if (powerupRequest.active)
        {
            newTarget = powerupRequest.timeScale;
            newTransitionSpeed = powerupRequest.customTransitionSpeed > 0 ? powerupRequest.customTransitionSpeed : defaultTransitionSpeed;
            activeSource = powerupRequest.sourceName;
        }
        
        if (trickSystemRequest.active && trickSystemRequest.timeScale < newTarget)
        {
            newTarget = trickSystemRequest.timeScale;
            newTransitionSpeed = trickSystemRequest.customTransitionSpeed > 0 ? trickSystemRequest.customTransitionSpeed : defaultTransitionSpeed;
            activeSource = trickSystemRequest.sourceName;
        }
        
        if (customRequest.active && customRequest.timeScale < newTarget)
        {
            newTarget = customRequest.timeScale;
            newTransitionSpeed = customRequest.customTransitionSpeed > 0 ? customRequest.customTransitionSpeed : defaultTransitionSpeed;
            activeSource = customRequest.sourceName;
        }
        
        // Update target if changed
        if (!Mathf.Approximately(targetTimeScale, newTarget))
        {
            targetTimeScale = newTarget;
            transitionSpeed = newTransitionSpeed;
            
            if (showDebugLogs)
            {
                Debug.Log($"🎬 [TimeDilation] Target changed to {targetTimeScale:F2}x (Source: {activeSource})");
            }
        }
        
        // Smooth transition to target (using unscaled time for consistent feel)
        currentTimeScale = Mathf.Lerp(currentTimeScale, targetTimeScale, Time.unscaledDeltaTime * transitionSpeed);
        Time.timeScale = currentTimeScale;
        
        // PERFORMANCE: Stop updating when we reach target
        if (Mathf.Abs(currentTimeScale - targetTimeScale) < 0.001f)
        {
            currentTimeScale = targetTimeScale;
            Time.timeScale = targetTimeScale;
            needsUpdate = false;
            
            if (showDebugLogs && targetTimeScale == 1f)
            {
                Debug.Log("🎬 [TimeDilation] ✅ Returned to normal time");
            }
        }
    }
    
    /// <summary>
    /// Request time dilation from the trick/aerial freestyle system
    /// </summary>
    public void SetTrickSystemDilation(bool active, float timeScale = 0.5f, float customTransitionSpeed = 0f)
    {
        trickSystemRequest.active = active;
        trickSystemRequest.timeScale = Mathf.Clamp(timeScale, 0.05f, 1f);
        trickSystemRequest.customTransitionSpeed = customTransitionSpeed;
        trickSystemRequest.sourceName = "Trick System";
        needsUpdate = true;
        
        if (showDebugLogs)
        {
            Debug.Log($"🎬 [TimeDilation] Trick System: {(active ? $"ACTIVE ({timeScale:F2}x)" : "INACTIVE")}");
        }
    }
    
    /// <summary>
    /// Request time dilation from powerup system (for future use)
    /// </summary>
    public void SetPowerupDilation(bool active, float timeScale = 0.2f, float customTransitionSpeed = 0f)
    {
        powerupRequest.active = active;
        powerupRequest.timeScale = Mathf.Clamp(timeScale, 0.05f, 1f);
        powerupRequest.customTransitionSpeed = customTransitionSpeed;
        powerupRequest.sourceName = "SlowTime Powerup";
        needsUpdate = true;
        
        if (showDebugLogs)
        {
            Debug.Log($"🎬 [TimeDilation] Powerup: {(active ? $"ACTIVE ({timeScale:F2}x)" : "INACTIVE")}");
        }
    }
    
    /// <summary>
    /// Request custom time dilation from any other system
    /// </summary>
    public void SetCustomDilation(bool active, float timeScale = 0.5f, string sourceName = "Custom", float customTransitionSpeed = 0f)
    {
        customRequest.active = active;
        customRequest.timeScale = Mathf.Clamp(timeScale, 0.05f, 1f);
        customRequest.customTransitionSpeed = customTransitionSpeed;
        customRequest.sourceName = sourceName;
        needsUpdate = true;
        
        if (showDebugLogs)
        {
            Debug.Log($"🎬 [TimeDilation] {sourceName}: {(active ? $"ACTIVE ({timeScale:F2}x)" : "INACTIVE")}");
        }
    }
    
    /// <summary>
    /// Force immediate return to normal time (emergency reset)
    /// </summary>
    public void ForceNormalTime()
    {
        trickSystemRequest.active = false;
        powerupRequest.active = false;
        customRequest.active = false;
        
        targetTimeScale = 1f;
        currentTimeScale = 1f;
        Time.timeScale = 1f;
        needsUpdate = false;
        
        if (showDebugLogs)
        {
            Debug.Log("🎬 [TimeDilation] ⚠️ FORCE RESET to normal time");
        }
    }
    
    /// <summary>
    /// Get current time scale value
    /// </summary>
    public float GetCurrentTimeScale()
    {
        return currentTimeScale;
    }
    
    /// <summary>
    /// Check if any time dilation is currently active
    /// </summary>
    public bool IsTimeDilationActive()
    {
        return currentTimeScale < 0.99f;
    }
    
    void OnDestroy()
    {
        // Always restore normal time when destroyed
        if (Time.timeScale != 1f)
        {
            Time.timeScale = 1f;
            Debug.Log("[TimeDilationManager] Restored normal time on destroy");
        }
        
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
