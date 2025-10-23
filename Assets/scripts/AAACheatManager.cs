// ============================================================================
// AAA CHEAT MANAGEMENT SYSTEM
// Handles unlocking, activating, and managing all cheat codes
// Integrates with progression system for earnable cheats
// 
// This is the "cheat economy" - players earn cheats through gameplay!
// ============================================================================

using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Central cheat management system
/// Handles unlocking, progression, and activation of all cheat codes
/// </summary>
public class AAACheatManager : MonoBehaviour
{
    #region Singleton
    public static AAACheatManager Instance { get; private set; }
    #endregion
    
    #region Cheat Definitions
    [System.Serializable]
    public class CheatDefinition
    {
        public string cheatID;
        public string displayName;
        public string description;
        public CheatCategory category;
        public int unlockCost; // Currency or points needed
        public KeyCode toggleKey = KeyCode.None;
        public bool isUnlocked = false;
        public bool isActive = false;
        public Sprite icon;
    }
    
    public enum CheatCategory
    {
        Vision,      // Wallhacks, ESP, etc.
        Combat,      // Aimbot, infinite ammo, etc.
        Movement,    // Speed hacks, fly, etc.
        Player,      // God mode, infinite health, etc.
        World,       // Time manipulation, etc.
        Fun          // Silly cheats like big head mode, etc.
    }
    #endregion
    
    #region Configuration
    [Header("=== CHEAT SYSTEM SETTINGS ===")]
    [Tooltip("Enable cheat system globally")]
    public bool cheatSystemEnabled = true;
    
    [Tooltip("Allow cheats in ranked/competitive modes")]
    public bool allowCheatsInCompetitive = false;
    
    [Tooltip("Show cheat UI notifications")]
    public bool showCheatNotifications = true;
    
    [Tooltip("Save cheat unlocks between sessions")]
    public bool persistCheats = true;
    
    [Header("=== AVAILABLE CHEATS ===")]
    public List<CheatDefinition> availableCheats = new List<CheatDefinition>();
    
    [Header("=== CHEAT CURRENCY ===")]
    [Tooltip("Player's current cheat points")]
    public int cheatPoints = 0;
    
    [Tooltip("Points earned per enemy kill")]
    public int pointsPerKill = 10;
    
    [Tooltip("Points earned per mission complete")]
    public int pointsPerMission = 100;
    
    [Tooltip("Points earned per secret found")]
    public int pointsPerSecret = 50;
    
    [Header("=== SYSTEM REFERENCES ===")]
    public AAAWallhackSystem wallhackSystem;
    public AAASmartAimbot aimbotSystem;
    public Canvas cheatMenuCanvas;
    public KeyCode cheatMenuKey = KeyCode.F1;
    #endregion
    
    #region Private Variables
    private Dictionary<string, CheatDefinition> cheatRegistry = new Dictionary<string, CheatDefinition>();
    private bool cheatMenuOpen = false;
    private const string SAVE_KEY = "AAA_CheatData";
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
        DontDestroyOnLoad(gameObject);
        
        // Initialize cheat registry
        InitializeCheatRegistry();
        
        // Load saved cheats
        if (persistCheats)
        {
            LoadCheatData();
        }
        
        // Find wallhack system if not assigned
        if (wallhackSystem == null)
        {
            wallhackSystem = FindObjectOfType<AAAWallhackSystem>();
            if (wallhackSystem == null)
            {
                Debug.LogWarning("[AAACheatManager] No wallhack system found! Wallhack cheat will not work.");
            }
        }
        
        // Find aimbot system if not assigned
        if (aimbotSystem == null)
        {
            aimbotSystem = FindObjectOfType<AAASmartAimbot>();
            if (aimbotSystem == null)
            {
                Debug.LogWarning("[AAACheatManager] No aimbot system found! Aimbot cheat will not work.");
            }
        }
    }
    
    void Start()
    {
        // Initialize default cheats if none exist
        if (availableCheats.Count == 0)
        {
            CreateDefaultCheats();
        }
        
        // Hide cheat menu initially
        if (cheatMenuCanvas != null)
        {
            cheatMenuCanvas.gameObject.SetActive(false);
        }
        
        Debug.Log($"[AAACheatManager] Initialized with {availableCheats.Count} cheats. Points: {cheatPoints}");
    }
    
    /// <summary>
    /// Initialize the cheat registry for quick lookups
    /// </summary>
    private void InitializeCheatRegistry()
    {
        cheatRegistry.Clear();
        foreach (CheatDefinition cheat in availableCheats)
        {
            if (!string.IsNullOrEmpty(cheat.cheatID))
            {
                cheatRegistry[cheat.cheatID] = cheat;
            }
        }
    }
    
    /// <summary>
    /// Create default cheat definitions
    /// </summary>
    private void CreateDefaultCheats()
    {
        availableCheats.Add(new CheatDefinition
        {
            cheatID = "wallhack",
            displayName = "Wallhack Vision",
            description = "See enemies through walls with glowing outlines. EngineOwning style!",
            category = CheatCategory.Vision,
            unlockCost = 500,
            toggleKey = KeyCode.F2,
            isUnlocked = false,
            isActive = false
        });
        
        availableCheats.Add(new CheatDefinition
        {
            cheatID = "godmode",
            displayName = "God Mode",
            description = "Become invincible - take no damage from any source.",
            category = CheatCategory.Player,
            unlockCost = 1000,
            toggleKey = KeyCode.F3,
            isUnlocked = false,
            isActive = false
        });
        
        availableCheats.Add(new CheatDefinition
        {
            cheatID = "infinite_ammo",
            displayName = "Infinite Ammo",
            description = "Never run out of ammunition. Reload instantly!",
            category = CheatCategory.Combat,
            unlockCost = 750,
            toggleKey = KeyCode.F4,
            isUnlocked = false,
            isActive = false
        });
        
        availableCheats.Add(new CheatDefinition
        {
            cheatID = "aimbot",
            displayName = "🎯 Smart Aimbot",
            description = "Intelligent auto-aim with human-like smoothness. BETTER than EngineOwning!",
            category = CheatCategory.Combat,
            unlockCost = 1200,
            toggleKey = KeyCode.F11,
            isUnlocked = false,
            isActive = false
        });
        
        availableCheats.Add(new CheatDefinition
        {
            cheatID = "superspeed",
            displayName = "Super Speed",
            description = "Move at 2x normal speed. Gotta go fast!",
            category = CheatCategory.Movement,
            unlockCost = 600,
            toggleKey = KeyCode.F5,
            isUnlocked = false,
            isActive = false
        });
        
        availableCheats.Add(new CheatDefinition
        {
            cheatID = "noclip",
            displayName = "No Clip / Fly Mode",
            description = "Fly through walls and explore freely.",
            category = CheatCategory.Movement,
            unlockCost = 1500,
            toggleKey = KeyCode.F6,
            isUnlocked = false,
            isActive = false
        });
        
        availableCheats.Add(new CheatDefinition
        {
            cheatID = "one_hit_kill",
            displayName = "One Hit Kill",
            description = "Every attack instantly kills enemies.",
            category = CheatCategory.Combat,
            unlockCost = 2000,
            toggleKey = KeyCode.F7,
            isUnlocked = false,
            isActive = false
        });
        
        availableCheats.Add(new CheatDefinition
        {
            cheatID = "slow_motion",
            displayName = "Slow Motion",
            description = "Activate Matrix-style bullet time.",
            category = CheatCategory.World,
            unlockCost = 800,
            toggleKey = KeyCode.F8,
            isUnlocked = false,
            isActive = false
        });
        
        availableCheats.Add(new CheatDefinition
        {
            cheatID = "big_head",
            displayName = "Big Head Mode",
            description = "Everyone gets a HUGE head! Classic cheat from the 90s.",
            category = CheatCategory.Fun,
            unlockCost = 300,
            toggleKey = KeyCode.F9,
            isUnlocked = false,
            isActive = false
        });
        
        InitializeCheatRegistry();
    }
    #endregion
    
    #region Update Loop
    void Update()
    {
        if (!cheatSystemEnabled)
            return;
        
        // Toggle cheat menu
        if (Input.GetKeyDown(cheatMenuKey))
        {
            ToggleCheatMenu();
        }
        
        // Check for cheat hotkeys
        foreach (CheatDefinition cheat in availableCheats)
        {
            if (cheat.isUnlocked && cheat.toggleKey != KeyCode.None && Input.GetKeyDown(cheat.toggleKey))
            {
                ToggleCheat(cheat.cheatID);
            }
        }
    }
    #endregion
    
    #region Cheat Management
    /// <summary>
    /// Unlock a cheat by ID
    /// </summary>
    public bool UnlockCheat(string cheatID)
    {
        if (!cheatRegistry.ContainsKey(cheatID))
        {
            Debug.LogError($"[AAACheatManager] Cheat '{cheatID}' not found!");
            return false;
        }
        
        CheatDefinition cheat = cheatRegistry[cheatID];
        
        // Check if already unlocked
        if (cheat.isUnlocked)
        {
            Debug.Log($"[AAACheatManager] Cheat '{cheat.displayName}' already unlocked!");
            return false;
        }
        
        // Check if player has enough points
        if (cheatPoints < cheat.unlockCost)
        {
            Debug.Log($"[AAACheatManager] Not enough points! Need {cheat.unlockCost}, have {cheatPoints}");
            if (showCheatNotifications)
            {
                ShowNotification($"Need {cheat.unlockCost - cheatPoints} more points to unlock {cheat.displayName}!");
            }
            return false;
        }
        
        // Unlock the cheat
        cheatPoints -= cheat.unlockCost;
        cheat.isUnlocked = true;
        
        Debug.Log($"[AAACheatManager] Unlocked '{cheat.displayName}'! Remaining points: {cheatPoints}");
        
        if (showCheatNotifications)
        {
            ShowNotification($"🎮 CHEAT UNLOCKED: {cheat.displayName}!");
        }
        
        // Save progress
        if (persistCheats)
        {
            SaveCheatData();
        }
        
        return true;
    }
    
    /// <summary>
    /// Toggle a cheat on/off
    /// </summary>
    public bool ToggleCheat(string cheatID)
    {
        if (!cheatRegistry.ContainsKey(cheatID))
        {
            Debug.LogError($"[AAACheatManager] Cheat '{cheatID}' not found!");
            return false;
        }
        
        CheatDefinition cheat = cheatRegistry[cheatID];
        
        // Check if unlocked
        if (!cheat.isUnlocked)
        {
            Debug.Log($"[AAACheatManager] Cheat '{cheat.displayName}' is locked!");
            if (showCheatNotifications)
            {
                ShowNotification($"🔒 {cheat.displayName} is locked! Cost: {cheat.unlockCost} points");
            }
            return false;
        }
        
        // Toggle the cheat
        cheat.isActive = !cheat.isActive;
        
        // Apply cheat effect
        ApplyCheatEffect(cheatID, cheat.isActive);
        
        Debug.Log($"[AAACheatManager] Cheat '{cheat.displayName}' {(cheat.isActive ? "ACTIVATED" : "DEACTIVATED")}");
        
        if (showCheatNotifications)
        {
            ShowNotification($"{cheat.displayName}: {(cheat.isActive ? "ON" : "OFF")}");
        }
        
        return true;
    }
    
    /// <summary>
    /// Apply the actual cheat effect
    /// </summary>
    private void ApplyCheatEffect(string cheatID, bool active)
    {
        switch (cheatID)
        {
            case "wallhack":
                if (wallhackSystem != null)
                {
                    wallhackSystem.SetWallhackEnabled(active);
                }
                break;
            
            case "aimbot":
                if (aimbotSystem != null)
                {
                    aimbotSystem.SetAimbotEnabled(active);
                    Debug.Log($"<color=cyan>[AAACheatManager] 🎯 Aimbot {(active ? "ENABLED" : "DISABLED")}!</color>");
                }
                break;
                
            case "godmode":
                ApplyGodMode(active);
                break;
                
            case "infinite_ammo":
                ApplyInfiniteAmmo(active);
                break;
                
            case "superspeed":
                ApplySuperSpeed(active);
                break;
                
            case "noclip":
                ApplyNoClip(active);
                break;
                
            case "one_hit_kill":
                ApplyOneHitKill(active);
                break;
                
            case "slow_motion":
                ApplySlowMotion(active);
                break;
                
            case "big_head":
                ApplyBigHeadMode(active);
                break;
                
            default:
                Debug.LogWarning($"[AAACheatManager] No implementation for cheat: {cheatID}");
                break;
        }
    }
    
    /// <summary>
    /// Check if a specific cheat is active
    /// </summary>
    public bool IsCheatActive(string cheatID)
    {
        if (!cheatRegistry.ContainsKey(cheatID))
            return false;
        
        return cheatRegistry[cheatID].isActive;
    }
    
    /// <summary>
    /// Check if a specific cheat is unlocked
    /// </summary>
    public bool IsCheatUnlocked(string cheatID)
    {
        if (!cheatRegistry.ContainsKey(cheatID))
            return false;
        
        return cheatRegistry[cheatID].isUnlocked;
    }
    #endregion
    
    #region Cheat Point System
    /// <summary>
    /// Award cheat points to the player
    /// </summary>
    public void AwardPoints(int amount, string reason = "")
    {
        cheatPoints += amount;
        Debug.Log($"[AAACheatManager] Awarded {amount} points! Total: {cheatPoints} ({reason})");
        
        if (showCheatNotifications && amount > 0)
        {
            ShowNotification($"+{amount} Cheat Points! ({reason})");
        }
        
        if (persistCheats)
        {
            SaveCheatData();
        }
    }
    
    /// <summary>
    /// Call this when player kills an enemy
    /// </summary>
    public void OnEnemyKilled()
    {
        AwardPoints(pointsPerKill, "Enemy Kill");
    }
    
    /// <summary>
    /// Call this when player completes a mission
    /// </summary>
    public void OnMissionComplete()
    {
        AwardPoints(pointsPerMission, "Mission Complete");
    }
    
    /// <summary>
    /// Call this when player finds a secret
    /// </summary>
    public void OnSecretFound()
    {
        AwardPoints(pointsPerSecret, "Secret Found");
    }
    #endregion
    
    #region Cheat Effect Implementations
    // These are placeholder implementations - you'll need to integrate with your actual systems
    
    private void ApplyGodMode(bool active)
    {
        // TODO: Integrate with your player health system
        var playerHealth = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            // You'll need to add a "isInvincible" flag to your PlayerHealth component
            // playerHealth.isInvincible = active;
            Debug.Log($"[CHEAT] God Mode: {active}");
        }
    }
    
    private void ApplyInfiniteAmmo(bool active)
    {
        // TODO: Integrate with your weapon system
        Debug.Log($"[CHEAT] Infinite Ammo: {active}");
    }
    
    private void ApplySuperSpeed(bool active)
    {
        var movement = GameObject.FindGameObjectWithTag("Player")?.GetComponent<AAAMovementController>();
        if (movement != null)
        {
            // You could add a speed multiplier field to your movement controller
            // movement.speedMultiplier = active ? 2f : 1f;
            Debug.Log($"[CHEAT] Super Speed: {active}");
        }
    }
    
    private void ApplyNoClip(bool active)
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // Disable collisions and enable flight
            var collider = player.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = !active;
            }
            Debug.Log($"[CHEAT] No Clip: {active}");
        }
    }
    
    private void ApplyOneHitKill(bool active)
    {
        // TODO: Set a global damage multiplier
        Debug.Log($"[CHEAT] One Hit Kill: {active}");
    }
    
    private void ApplySlowMotion(bool active)
    {
        Time.timeScale = active ? 0.3f : 1f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        Debug.Log($"[CHEAT] Slow Motion: {active}");
    }
    
    private void ApplyBigHeadMode(bool active)
    {
        // Find all character heads and scale them
        GameObject[] characters = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject character in characters)
        {
            Transform head = character.transform.Find("Head"); // Adjust to your hierarchy
            if (head != null)
            {
                head.localScale = active ? Vector3.one * 2f : Vector3.one;
            }
        }
        Debug.Log($"[CHEAT] Big Head Mode: {active}");
    }
    #endregion
    
    #region UI Management
    private void ToggleCheatMenu()
    {
        cheatMenuOpen = !cheatMenuOpen;
        
        if (cheatMenuCanvas != null)
        {
            cheatMenuCanvas.gameObject.SetActive(cheatMenuOpen);
        }
        
        // Pause game when menu is open
        Time.timeScale = cheatMenuOpen ? 0f : 1f;
    }
    
    private void ShowNotification(string message)
    {
        // TODO: Implement proper UI notification system
        Debug.Log($"[NOTIFICATION] {message}");
    }
    #endregion
    
    #region Save/Load System
    [System.Serializable]
    private class CheatSaveData
    {
        public int cheatPoints;
        public List<string> unlockedCheats = new List<string>();
    }
    
    private void SaveCheatData()
    {
        CheatSaveData saveData = new CheatSaveData
        {
            cheatPoints = cheatPoints
        };
        
        foreach (CheatDefinition cheat in availableCheats)
        {
            if (cheat.isUnlocked)
            {
                saveData.unlockedCheats.Add(cheat.cheatID);
            }
        }
        
        string json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
        
        Debug.Log("[AAACheatManager] Cheat data saved!");
    }
    
    private void LoadCheatData()
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY))
        {
            Debug.Log("[AAACheatManager] No saved cheat data found.");
            return;
        }
        
        string json = PlayerPrefs.GetString(SAVE_KEY);
        CheatSaveData saveData = JsonUtility.FromJson<CheatSaveData>(json);
        
        cheatPoints = saveData.cheatPoints;
        
        foreach (string cheatID in saveData.unlockedCheats)
        {
            if (cheatRegistry.ContainsKey(cheatID))
            {
                cheatRegistry[cheatID].isUnlocked = true;
            }
        }
        
        Debug.Log($"[AAACheatManager] Loaded cheat data: {cheatPoints} points, {saveData.unlockedCheats.Count} unlocked cheats");
    }
    #endregion
    
    #region Debug UI
    void OnGUI()
    {
        if (!cheatSystemEnabled)
            return;
        
        // Draw cheat points in corner
        GUIStyle style = new GUIStyle();
        style.fontSize = 16;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.yellow;
        
        GUI.Label(new Rect(10, Screen.height - 30, 300, 25), $"💰 Cheat Points: {cheatPoints}", style);
        
        // Show active cheats
        int yOffset = Screen.height - 60;
        foreach (CheatDefinition cheat in availableCheats)
        {
            if (cheat.isActive)
            {
                style.normal.textColor = Color.green;
                GUI.Label(new Rect(10, yOffset, 300, 25), $"[ACTIVE] {cheat.displayName}", style);
                yOffset -= 25;
            }
        }
    }
    #endregion
}
