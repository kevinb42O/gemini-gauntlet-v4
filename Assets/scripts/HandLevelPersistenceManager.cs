using System.IO;
using UnityEngine;

/// <summary>
/// Simple hand level persistence manager - SCENE-INDEPENDENT numeric storage
/// Requirements:
/// - Store ONLY 2 integers (primary hand level, secondary hand level)
/// - Work in both menu and game scenes without depending on PlayerProgression
/// - Menu scene: Load/display saved hand levels in UI
/// - Game scene: PlayerProgression reads saved levels on startup, saves changes back
/// - Rock solid persistence that survives scene transitions
/// </summary>
public class HandLevelPersistenceManager : MonoBehaviour
{
    [Header("Hand Display UI References")]
    [Tooltip("Primary (left) hand display UI component - LMB")]
    public HandDisplayUI primaryHandDisplayUI;
    
    [Tooltip("Secondary (right) hand display UI component - RMB")]
    public HandDisplayUI secondaryHandDisplayUI;
    
    [Header("Hand Objects (Optional)")]
    [Tooltip("Primary hand object/transform for future hand-specific features")]
    public Transform primaryHandObject;
    
    [Tooltip("Secondary hand object/transform for future hand-specific features")]
    public Transform secondaryHandObject;
    
    [Header("Debug")]
    [Tooltip("Enable debug logging for hand level persistence")]
    public bool enableDebugLogging = true;
    
    // Singleton instance
    public static HandLevelPersistenceManager Instance { get; private set; }
    
    // SCENE-INDEPENDENT STORAGE: Just 2 integers, no dependency on PlayerProgression
    [Header("Current Hand Levels (Runtime)")]
    [Tooltip("Current primary hand level (1-4) - persisted across scenes")]
    [SerializeField] private int _currentPrimaryHandLevel = 1;
    
    [Tooltip("Current secondary hand level (1-4) - persisted across scenes")]
    [SerializeField] private int _currentSecondaryHandLevel = 1;
    
    [Tooltip("Current second hand unlock state - persisted across scenes")]
    [SerializeField] private bool _isSecondHandUnlocked = false;
    
    // Public read-only access to stored hand levels and unlock state
    public int CurrentPrimaryHandLevel => _currentPrimaryHandLevel;
    public int CurrentSecondaryHandLevel => _currentSecondaryHandLevel;
    public bool IsSecondHandUnlocked => _isSecondHandUnlocked;
    
    // Save file path
    private string _handLevelSavePath;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Set up save path following StashManager pattern
            _handLevelSavePath = Path.Combine(Application.persistentDataPath, "hand_level_data.json");
            
            DebugLog("🖐️ HandLevelPersistenceManager: Initialized and ready for persistence");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Auto-find HandDisplayUI components if not assigned
        if (primaryHandDisplayUI == null || secondaryHandDisplayUI == null)
        {
            HandDisplayUI[] allHandUIs = FindObjectsByType<HandDisplayUI>(FindObjectsSortMode.None);
            
            foreach (var handUI in allHandUIs)
            {
                if (handUI.isRightHand && primaryHandDisplayUI == null)
                {
                    primaryHandDisplayUI = handUI;
                    DebugLog("🖐️ Auto-found Primary (right) HandDisplayUI component");
                }
                else if (!handUI.isRightHand && secondaryHandDisplayUI == null)
                {
                    secondaryHandDisplayUI = handUI;
                    DebugLog("🖐️ Auto-found Secondary (left) HandDisplayUI component");
                }
            }
        }
        
        // Load hand level data on startup to restore UI state
        LoadHandLevelData();
    }
    
    /// <summary>
    /// PUBLIC METHOD: Update stored hand levels (call from PlayerProgression when levels change)
    /// </summary>
    public void UpdateStoredHandLevels(int primaryLevel, int secondaryLevel)
    {
        // Clamp values to valid range
        primaryLevel = Mathf.Clamp(primaryLevel, 1, 4);
        secondaryLevel = Mathf.Clamp(secondaryLevel, 1, 4);
        
        // Update internal storage
        _currentPrimaryHandLevel = primaryLevel;
        _currentSecondaryHandLevel = secondaryLevel;
        
        DebugLog($"🖐️ Updated stored hand levels: Primary={primaryLevel}, Secondary={secondaryLevel}");
        
        // Save immediately to file
        SaveHandLevelData();
    }
    
    /// <summary>
    /// PUBLIC METHOD: Update second hand unlock state (call from PlayerSecondHandAbility when unlock state changes)
    /// </summary>
    public void UpdateSecondHandUnlockState(bool isUnlocked)
    {
        // Update internal storage
        _isSecondHandUnlocked = isUnlocked;
        
        DebugLog($"🖐️ Updated second hand unlock state: {(isUnlocked ? "UNLOCKED" : "LOCKED")}");
        
        // Save immediately to file
        SaveHandLevelData();
    }
    
    /// <summary>
    /// Save current hand level data to persistent storage - SCENE-INDEPENDENT
    /// Uses internal storage, no dependency on PlayerProgression
    /// </summary>
    public void SaveHandLevelData()
    {
        try
        {
            // Create save data object from internal storage (no PlayerProgression dependency)
            HandLevelSaveData saveData = new HandLevelSaveData
            {
                primaryHandLevel = _currentPrimaryHandLevel,
                secondaryHandLevel = _currentSecondaryHandLevel,
                isSecondHandUnlocked = _isSecondHandUnlocked
            };
            
            // Convert to JSON and save to file (same pattern as StashManager.SaveStashData)
            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(_handLevelSavePath, json);
            
            DebugLog($"🖐️ ✅ Saved hand level data: Primary={saveData.primaryHandLevel}, Secondary={saveData.secondaryHandLevel}, UnlockState={saveData.isSecondHandUnlocked}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"🖐️ ❌ Failed to save hand level data: {e.Message}");
        }
    }
    
    /// <summary>
    /// Load hand level data from persistent storage - SCENE-INDEPENDENT
    /// Updates internal storage and optionally applies to PlayerProgression if it exists
    /// </summary>
    public void LoadHandLevelData()
    {
        try
        {
            if (!File.Exists(_handLevelSavePath))
            {
                DebugLog("🖐️ No hand level save file found - starting with default levels (1, 1) and UNLOCKED state (new game design)");
                _currentPrimaryHandLevel = 1;
                _currentSecondaryHandLevel = 1;
                _isSecondHandUnlocked = true; // GAME DESIGN CHANGE: Start with both hands unlocked
                return;
            }
            
            // Load JSON data (same pattern as StashManager.LoadStashData)
            string json = File.ReadAllText(_handLevelSavePath);
            HandLevelSaveData saveData = JsonUtility.FromJson<HandLevelSaveData>(json);
            
            if (saveData == null)
            {
                DebugLog("🖐️ ⚠️ Failed to parse hand level save data - using defaults (1, 1) and UNLOCKED state (new game design)");
                _currentPrimaryHandLevel = 1;
                _currentSecondaryHandLevel = 1;
                _isSecondHandUnlocked = true; // GAME DESIGN CHANGE: Start with both hands unlocked
                return;
            }
            
            // Update internal storage with loaded data
            _currentPrimaryHandLevel = Mathf.Clamp(saveData.primaryHandLevel, 1, 4);
            _currentSecondaryHandLevel = Mathf.Clamp(saveData.secondaryHandLevel, 1, 4);
            _isSecondHandUnlocked = saveData.isSecondHandUnlocked; // Load unlock state
            
            DebugLog($"🖐️ ✅ Loaded hand levels to internal storage: Primary={_currentPrimaryHandLevel}, Secondary={_currentSecondaryHandLevel}, UnlockState={_isSecondHandUnlocked}");
            
            // OPTIONAL: Apply to PlayerProgression if it exists (game scene)
            if (PlayerProgression.Instance != null)
            {
                PlayerProgression.Instance.SetHandLevelsFromSavedData(_currentPrimaryHandLevel, _currentSecondaryHandLevel);
                DebugLog($"🖐️ ✅ Applied hand levels to PlayerProgression: Primary={_currentPrimaryHandLevel}, Secondary={_currentSecondaryHandLevel}");
            }
            else
            {
                DebugLog("🖐️ ℹ️ PlayerProgression.Instance not found - stored in internal storage only (probably menu scene)");
            }
            
            // IMMEDIATE UI UPDATE: Update HandDisplayUI components if they exist (menu scene)
            UpdateHandDisplayUIs();
            
            // DELAYED UI REFRESH: Additional safety refresh after a short delay to handle timing issues
            StartCoroutine(DelayedUIRefresh());
        }
        catch (System.Exception e)
        {
            Debug.LogError($"🖐️ ❌ Failed to load hand level data: {e.Message}");
            // Fallback to defaults on error
            _currentPrimaryHandLevel = 1;
            _currentSecondaryHandLevel = 1;
            _isSecondHandUnlocked = true; // GAME DESIGN CHANGE: Start with both hands unlocked
        }
    }
    
    /// <summary>
    /// Delayed UI refresh coroutine to handle timing issues
    /// </summary>
    private System.Collections.IEnumerator DelayedUIRefresh()
    {
        // Wait a few frames to ensure all UI components are properly initialized
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        
        DebugLog("🖐️ DelayedUIRefresh: Performing delayed UI refresh for timing safety");
        UpdateHandDisplayUIs();
        
        // Additional refresh after a short delay
        yield return new WaitForSeconds(0.2f);
        DebugLog("🖐️ DelayedUIRefresh: Final UI refresh after delay");
        UpdateHandDisplayUIs();
    }
    
    /// <summary>
    /// PUBLIC METHOD: Force refresh all HandDisplayUI components (for external systems)
    /// </summary>
    public void ForceRefreshAllHandDisplayUIs()
    {
        DebugLog("🖐️ ForceRefreshAllHandDisplayUIs: Manual refresh triggered by external system");
        UpdateHandDisplayUIs();
    }
    
    /// <summary>
    /// Update hand display UIs with current stored hand levels (if UIs exist)
    /// </summary>
    private void UpdateHandDisplayUIs()
    {
        int updatedUIs = 0;
        
        // Auto-find HandDisplayUI components if not assigned
        if (primaryHandDisplayUI == null || secondaryHandDisplayUI == null)
        {
            HandDisplayUI[] allHandUIs = FindObjectsByType<HandDisplayUI>(FindObjectsSortMode.None);
            
            foreach (var handUI in allHandUIs)
            {
                if (handUI.isRightHand && primaryHandDisplayUI == null)
                {
                    primaryHandDisplayUI = handUI;
                    DebugLog("🖐️ Auto-found Primary (right) HandDisplayUI component during UI update");
                }
                else if (!handUI.isRightHand && secondaryHandDisplayUI == null)
                {
                    secondaryHandDisplayUI = handUI;
                    DebugLog("🖐️ Auto-found Secondary (left) HandDisplayUI component during UI update");
                }
            }
        }
        
        if (primaryHandDisplayUI != null)
        {
            primaryHandDisplayUI.ManualRefresh(); // Use ManualRefresh for better debugging
            updatedUIs++;
            DebugLog("🖐️ ✅ Updated Primary HandDisplayUI with stored hand level data");
        }
        else
        {
            DebugLog("🖐️ ⚠️ Primary HandDisplayUI not found - cannot update UI");
        }
        
        if (secondaryHandDisplayUI != null)
        {
            secondaryHandDisplayUI.ManualRefresh(); // Use ManualRefresh for better debugging
            updatedUIs++;
            DebugLog("🖐️ ✅ Updated Secondary HandDisplayUI with stored hand level data");
        }
        else
        {
            DebugLog("🖐️ ⚠️ Secondary HandDisplayUI not found - cannot update UI");
        }
        
        if (updatedUIs > 0)
        {
            DebugLog($"🖐️ ✅ Updated {updatedUIs}/2 HandDisplayUI components with stored hand level data");
        }
        else
        {
            DebugLog("🖐️ ℹ️ No HandDisplayUI components found - stored data available for when they're needed");
        }
    }
    
    /// <summary>
    /// PUBLIC METHOD: Get current stored hand levels for PlayerProgression startup
    /// Call this from PlayerProgression.Awake() or Start() to initialize with saved levels
    /// </summary>
    public void InitializePlayerProgressionWithStoredLevels()
    {
        if (PlayerProgression.Instance == null)
        {
            DebugLog("🖐️ ⚠️ InitializePlayerProgressionWithStoredLevels: PlayerProgression.Instance not found");
            return;
        }
        
        DebugLog($"🖐️ Initializing PlayerProgression with stored hand levels: Primary={_currentPrimaryHandLevel}, Secondary={_currentSecondaryHandLevel}");
        
        // Apply stored levels to PlayerProgression
        PlayerProgression.Instance.SetHandLevelsFromSavedData(_currentPrimaryHandLevel, _currentSecondaryHandLevel);
        
        DebugLog("🖐️ ✅ PlayerProgression initialized with stored hand levels");
    }
    
    /// <summary>
    /// Manual save trigger - can be called from other systems when hand levels change
    /// DEPRECATED: Use UpdateStoredHandLevels() instead
    /// </summary>
    [System.Obsolete("Use UpdateStoredHandLevels() instead")]
    public void SaveOnHandLevelChange()
    {
        DebugLog("🖐️ Hand level change detected - saving data");
        SaveHandLevelData();
    }
    
    /// <summary>
    /// Manual save trigger - can be called when exiting zones/scenes
    /// </summary>
    public void SaveOnExitZone()
    {
        DebugLog("🖐️ Exit zone detected - saving hand level data");
        SaveHandLevelData();
    }
    
    /// <summary>
    /// Emergency save on application events
    /// </summary>
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            DebugLog("🖐️ Application paused - emergency save");
            SaveHandLevelData();
        }
    }
    
    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            DebugLog("🖐️ Application lost focus - emergency save");
            SaveHandLevelData();
        }
    }
    
    void OnDestroy()
    {
        if (Instance == this)
        {
            DebugLog("🖐️ HandLevelPersistenceManager destroyed - final save");
            SaveHandLevelData();
        }
    }
    
    /// <summary>
    /// Debug logging helper
    /// </summary>
    private void DebugLog(string message)
    {
        if (enableDebugLogging)
        {
            Debug.Log(message);
        }
    }
    
    /// <summary>
    /// Get save file path for debugging
    /// </summary>
    public string GetSaveFilePath()
    {
        return _handLevelSavePath;
    }
    
    /// <summary>
    /// Context menu method for testing save/load from Inspector
    /// </summary>
    [ContextMenu("Test Save Hand Levels")]
    public void TestSaveHandLevels()
    {
        DebugLog("🖐️ === TESTING HAND LEVEL SAVE ===");
        SaveHandLevelData();
    }
    
    [ContextMenu("Test Load Hand Levels")]
    public void TestLoadHandLevels()
    {
        DebugLog("🖐️ === TESTING HAND LEVEL LOAD ===");
        LoadHandLevelData();
    }
    
    [ContextMenu("Reset Hand Level Data")]
    public void ResetHandLevelData()
    {
        try
        {
            if (File.Exists(_handLevelSavePath))
            {
                File.Delete(_handLevelSavePath);
                DebugLog("🖐️ ✅ Reset hand level save file");
            }
            else
            {
                DebugLog("🖐️ No hand level save file to reset");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"🖐️ ❌ Failed to reset hand level data: {e.Message}");
        }
    }
    
    /// <summary>
    /// EMERGENCY DEATH RESET: Force reset all hand levels to 1 and lock second hand
    /// This method provides additional safeguards for death scenarios
    /// </summary>
    public void EmergencyDeathReset()
    {
        DebugLog("🖐️ ⚠️ EMERGENCY DEATH RESET - Forcing all hand levels to 1");
        
        // Force reset internal storage to starting values
        _currentPrimaryHandLevel = 1;
        _currentSecondaryHandLevel = 1;
        _isSecondHandUnlocked = true; // GAME DESIGN CHANGE: Even on death, keep both hands unlocked
        
        // Save immediately to disk
        SaveHandLevelData();
        
        // Update PlayerProgression if it exists
        if (PlayerProgression.Instance != null)
        {
            PlayerProgression.Instance.SetHandLevelsFromSavedData(1, 1);
            DebugLog("🖐️ ✅ Emergency reset applied to PlayerProgression");
        }
        
        // Force UI refresh
        UpdateHandDisplayUIs();
        
        DebugLog("🖐️ ✅ EMERGENCY DEATH RESET COMPLETE - All systems reset to level 1");
    }
}

/// <summary>
/// Data container for hand level save file (follows StashManager pattern)
/// </summary>
[System.Serializable]
public class HandLevelSaveData
{
    public int primaryHandLevel = 1; // GAME DESIGN CHANGE: Default to level 1
    public int secondaryHandLevel = 1; // GAME DESIGN CHANGE: Default to level 1
    public bool isSecondHandUnlocked = true; // GAME DESIGN CHANGE: Default to unlocked
}
