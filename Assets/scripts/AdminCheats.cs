// --- AdminCheats.cs (UPDATED for Celestial Knight Flight) ---
using UnityEngine;

public class AdminCheats : MonoBehaviour
{
    [Header("Cheat Settings")]
    [Tooltip("Enable these cheats? Set to false for release builds.")]
    public bool enableCheats = true;

    [Header("References (Auto-find if null)")]
    public PlayerProgression playerProgression;
    
    // Reference to the CelestialDriftController
    private CelestialDriftController driftController;
    
    // Reference to PlayerHealth for godmode
    private PlayerHealth playerHealth;
    
    [Header("Godmode State")]
    private bool godmodeActive = false;

    void Awake()
    {
        if (playerProgression == null)
        {
            // Try to find PlayerProgression on the same GameObject or a parent.
            playerProgression = GetComponent<PlayerProgression>();
            if (playerProgression == null) playerProgression = GetComponentInParent<PlayerProgression>();
            if (playerProgression == null)
            {
                Debug.LogError("AdminCheats: PlayerProgression script not found! Some cheats will not work.", this);
            }
        }
        
        // Get reference to the CelestialDriftController
        driftController = FindFirstObjectByType<CelestialDriftController>();
        if (driftController == null)
        {
            Debug.LogWarning("AdminCheats: CelestialDriftController not found in the scene. Flight toggle cheat will not work.");
        }
        
        // Get reference to PlayerHealth for godmode
        playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (playerHealth == null)
            {
                Debug.LogWarning("AdminCheats: PlayerHealth not found. Godmode cheat will not work.");
            }
        }
    }

    void Update()
    {
        if (!enableCheats || playerProgression == null)
        {
            return;
        }

        // Toggle flight system with J key (for debugging)
        if (Input.GetKeyDown(KeyCode.J))
        {
            ToggleFlightSystem();
        }
        
        // Toggle godmode with + key (both regular and numpad)
        if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            ToggleGodmode();
        }

            // Primary Hand (Left - Numpad 1-4)
        if (Input.GetKeyDown(KeyCode.Keypad1)) { playerProgression.DEBUG_AdminSetHandLevel(true, 1); HandUIManager.ForceRefreshHandLevels(); }
        if (Input.GetKeyDown(KeyCode.Keypad2)) { playerProgression.DEBUG_AdminSetHandLevel(true, 2); HandUIManager.ForceRefreshHandLevels(); }
        if (Input.GetKeyDown(KeyCode.Keypad3)) { playerProgression.DEBUG_AdminSetHandLevel(true, 3); HandUIManager.ForceRefreshHandLevels(); }
        if (Input.GetKeyDown(KeyCode.Keypad4)) { playerProgression.DEBUG_AdminSetHandLevel(true, 4); HandUIManager.ForceRefreshHandLevels(); }
        // Numpad 0 for reset handled below

        // Secondary Hand (Right - Numpad 5-8)
        if (Input.GetKeyDown(KeyCode.Keypad5)) { playerProgression.DEBUG_AdminSetHandLevel(false, 1); HandUIManager.ForceRefreshHandLevels(); }
        if (Input.GetKeyDown(KeyCode.Keypad6)) { playerProgression.DEBUG_AdminSetHandLevel(false, 2); HandUIManager.ForceRefreshHandLevels(); }
        if (Input.GetKeyDown(KeyCode.Keypad7)) { playerProgression.DEBUG_AdminSetHandLevel(false, 3); HandUIManager.ForceRefreshHandLevels(); }
        if (Input.GetKeyDown(KeyCode.Keypad8)) { playerProgression.DEBUG_AdminSetHandLevel(false, 4); HandUIManager.ForceRefreshHandLevels(); }

        // Numpad 9 was used for unlocking Secondary Hand (removed - both hands always available)
        
        // Numpad 0 to reset both hands to Level 1
        if (Input.GetKeyDown(KeyCode.Keypad0))
        {
            playerProgression.DEBUG_AdminSetHandLevel(true, 1);
            playerProgression.DEBUG_AdminSetHandLevel(false, 1);
            HandUIManager.ForceRefreshHandLevels(); // Refresh UI after resetting both hands
            Debug.Log("ADMIN CHEAT: Both hands reset to Level 1.");
        }
    }
    
    private void ToggleFlightSystem()
    {
        if (driftController == null)
        {
            Debug.LogWarning("Cannot toggle flight: CelestialDriftController not found!");
            return;
        }

        // Toggle the flight system
        driftController.isFlightUnlocked = !driftController.isFlightUnlocked;
        
        string status = driftController.isFlightUnlocked ? "ENABLED" : "DISABLED";
        Debug.Log($"CHEAT: Celestial Drift flight system {status}");
    }
    
    private void ToggleGodmode()
    {
        if (playerHealth == null)
        {
            Debug.LogWarning("Cannot toggle godmode: PlayerHealth not found!");
            return;
        }
        
        godmodeActive = !godmodeActive;
        playerHealth.isInvincible = godmodeActive;
        
        Debug.Log($"CHEAT: Godmode {(godmodeActive ? "ON" : "OFF")}");
    }
    
    void OnGUI()
    {
        if (!enableCheats)
            return;
        
        // Display godmode status in top right corner
        if (godmodeActive)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 20;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.red;
            style.alignment = TextAnchor.UpperRight;
            
            GUI.Label(new Rect(Screen.width - 210, 10, 200, 30), "GODMODE ON", style);
        }
    }
}