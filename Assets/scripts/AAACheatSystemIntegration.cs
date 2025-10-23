// ============================================================================
// AAA CHEAT SYSTEM INTEGRATION
// Easy integration helper - automatically connects all cheat systems
// Just attach this ONE script and everything works!
// ============================================================================

using UnityEngine;

/// <summary>
/// One-click cheat system integration
/// Attach this to your Player or Main Camera and everything auto-configures
/// </summary>
[RequireComponent(typeof(AAAWallhackSystem))]
[RequireComponent(typeof(AAAESPOverlay))]
[RequireComponent(typeof(AAASmartAimbot))]
public class AAACheatSystemIntegration : MonoBehaviour
{
    [Header("=== AUTO-SETUP ===")]
    [Tooltip("Automatically find and configure all systems on Start")]
    public bool autoSetup = true;
    
    [Header("=== CHEAT INTEGRATION WITH GAMEPLAY ===")]
    [Tooltip("Automatically award points for kills")]
    public bool autoAwardKillPoints = true;
    
    [Tooltip("Track enemy kills for cheat point awards")]
    public bool trackEnemyKills = true;
    
    [Header("=== QUICK TOGGLE HOTKEYS ===")]
    [Tooltip("Master toggle for ALL cheats")]
    public KeyCode masterToggleKey = KeyCode.F10;
    
    [Tooltip("Quick toggle wallhack + ESP together")]
    public KeyCode wallhackESPToggleKey = KeyCode.F2;
    
    [Header("=== SYSTEM REFERENCES (Auto-filled) ===")]
    public AAAWallhackSystem wallhackSystem;
    public AAAESPOverlay espOverlay;
    public AAASmartAimbot aimbotSystem;
    public AAACheatManager cheatManager;
    
    [Header("=== STATUS (Read-Only) ===")]
    [SerializeField] private bool systemsInitialized = false;
    [SerializeField] private int activeEnemies = 0;
    
    void Start()
    {
        if (autoSetup)
        {
            AutoSetupSystems();
        }
        
        Debug.Log("[AAACheatSystemIntegration] ✅ Cheat systems ready!");
    }
    
    void AutoSetupSystems()
    {
        Debug.Log("<color=yellow>[AAACheatSystemIntegration] 🔧 AUTO-SETUP STARTING...</color>");
        
        // Get or create wallhack system
        wallhackSystem = GetComponent<AAAWallhackSystem>();
        if (wallhackSystem == null)
        {
            wallhackSystem = gameObject.AddComponent<AAAWallhackSystem>();
            Debug.Log("<color=lime>[Integration] ✅ Added Wallhack System</color>");
        }
        
        // Get or create ESP overlay
        espOverlay = GetComponent<AAAESPOverlay>();
        if (espOverlay == null)
        {
            espOverlay = gameObject.AddComponent<AAAESPOverlay>();
            Debug.Log("<color=lime>[Integration] ✅ Added ESP Overlay (Canvas auto-creates!)</color>");
        }
        
        // Get or create aimbot system
        aimbotSystem = GetComponent<AAASmartAimbot>();
        if (aimbotSystem == null)
        {
            aimbotSystem = gameObject.AddComponent<AAASmartAimbot>();
            Debug.Log("<color=lime>[Integration] ✅ Added Smart Aimbot System</color>");
        }
        
        // Find or create cheat manager
        cheatManager = FindObjectOfType<AAACheatManager>();
        if (cheatManager == null)
        {
            GameObject managerObj = new GameObject("AAA_CheatManager_AUTO_CREATED");
            cheatManager = managerObj.AddComponent<AAACheatManager>();
            Debug.Log("<color=lime>[Integration] ✅ Created Cheat Manager</color>");
        }
        
        // Link systems together
        if (cheatManager != null)
        {
            cheatManager.wallhackSystem = wallhackSystem;
            cheatManager.aimbotSystem = aimbotSystem;
        }
        
        systemsInitialized = true;
        
        Debug.Log("<color=lime>[AAACheatSystemIntegration] ✅✅✅ ALL SYSTEMS READY!</color>");
        Debug.Log("<color=cyan>Press F10 = Master Toggle | F2 = Wallhack+ESP | F11 = Aimbot</color>");
        
        // Hook into enemy death events if enabled
        if (autoAwardKillPoints)
        {
            HookEnemyDeathEvents();
        }
    }
    
    void HookEnemyDeathEvents()
    {
        // This will be called whenever enemies die
        // You'll need to modify your enemy scripts to call this
        Debug.Log("[Integration] Enemy death tracking enabled");
    }
    
    void Update()
    {
        if (!systemsInitialized)
            return;
        
        // Master toggle - turns ALL cheats on/off
        if (Input.GetKeyDown(masterToggleKey))
        {
            ToggleAllCheats();
        }
        
        // Wallhack + ESP quick toggle
        if (Input.GetKeyDown(wallhackESPToggleKey))
        {
            ToggleWallhackAndESP();
        }
        
        // Update status
        if (wallhackSystem != null)
        {
            activeEnemies = wallhackSystem.GetActiveWallhackCount();
        }
    }
    
    #region Public API
    
    /// <summary>
    /// Toggle ALL active cheats on/off
    /// </summary>
    public void ToggleAllCheats()
    {
        bool newState = !wallhackSystem.wallhackEnabled;
        
        if (wallhackSystem != null)
            wallhackSystem.wallhackEnabled = newState;
        
        if (espOverlay != null)
            espOverlay.espEnabled = newState;
        
        Debug.Log($"[Integration] ALL CHEATS: {(newState ? "ON" : "OFF")}");
    }
    
    /// <summary>
    /// Toggle wallhack and ESP together (they work best in tandem)
    /// </summary>
    public void ToggleWallhackAndESP()
    {
        if (cheatManager != null && cheatManager.IsCheatUnlocked("wallhack"))
        {
            cheatManager.ToggleCheat("wallhack");
            espOverlay.ToggleESP();
        }
        else
        {
            // Fallback for testing
            wallhackSystem.ToggleWallhack();
            espOverlay.ToggleESP();
        }
    }
    
    /// <summary>
    /// Call this from your enemy death code
    /// Example: AAACheatSystemIntegration.NotifyEnemyKilled(this.gameObject);
    /// </summary>
    public static void NotifyEnemyKilled(GameObject enemy)
    {
        // Award cheat points
        if (AAACheatManager.Instance != null)
        {
            AAACheatManager.Instance.OnEnemyKilled();
        }
        
        // Unregister from wallhack/ESP
        if (AAAWallhackSystem.Instance != null)
        {
            // The wallhack system will auto-cleanup, but we can force it
        }
        
        if (AAAESPOverlay.Instance != null)
        {
            AAAESPOverlay.Instance.UnregisterEnemy(enemy);
        }
    }
    
    /// <summary>
    /// Give player bonus cheat points
    /// </summary>
    public void AwardBonusPoints(int amount, string reason)
    {
        if (cheatManager != null)
        {
            cheatManager.AwardPoints(amount, reason);
        }
    }
    
    #endregion
    
    #region Debug UI
    void OnGUI()
    {
        if (!systemsInitialized)
            return;
        
        GUIStyle headerStyle = new GUIStyle();
        headerStyle.fontSize = 18;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = Color.cyan;
        
        GUIStyle style = new GUIStyle();
        style.fontSize = 14;
        style.normal.textColor = Color.white;
        
        int y = 10;
        
        GUI.Label(new Rect(Screen.width - 310, y, 300, 25), "🎮 AAA CHEAT SYSTEM", headerStyle);
        y += 30;
        
        if (wallhackSystem != null)
        {
            Color statusColor = wallhackSystem.wallhackEnabled ? Color.green : Color.red;
            style.normal.textColor = statusColor;
            GUI.Label(new Rect(Screen.width - 310, y, 300, 20), 
                $"Wallhack: {(wallhackSystem.wallhackEnabled ? "ON" : "OFF")}", style);
            y += 20;
        }
        
        if (espOverlay != null)
        {
            Color statusColor = espOverlay.espEnabled ? Color.green : Color.red;
            style.normal.textColor = statusColor;
            GUI.Label(new Rect(Screen.width - 310, y, 300, 20), 
                $"ESP: {(espOverlay.espEnabled ? "ON" : "OFF")}", style);
            y += 20;
        }
        
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(Screen.width - 310, y, 300, 20), 
            $"Active Enemies: {activeEnemies}", style);
        y += 20;
        
        if (cheatManager != null)
        {
            GUI.Label(new Rect(Screen.width - 310, y, 300, 20), 
                $"Cheat Points: {cheatManager.cheatPoints}", style);
            y += 25;
        }
        
        // Hotkey hints
        style.fontSize = 12;
        style.normal.textColor = Color.yellow;
        GUI.Label(new Rect(Screen.width - 310, y, 300, 20), 
            $"[{masterToggleKey}] Toggle All Cheats", style);
        y += 18;
        GUI.Label(new Rect(Screen.width - 310, y, 300, 20), 
            $"[{wallhackESPToggleKey}] Toggle Wallhack+ESP", style);
        y += 18;
        GUI.Label(new Rect(Screen.width - 310, y, 300, 20), 
            "[F1] Cheat Menu", style);
    }
    #endregion
}
