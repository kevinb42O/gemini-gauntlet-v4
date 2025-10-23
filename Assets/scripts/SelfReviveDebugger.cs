using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Debug script to help diagnose self-revive UI and death restart issues
/// Attach this to any GameObject in the scene to get detailed debug info
/// </summary>
public class SelfReviveDebugger : MonoBehaviour
{
    [Header("Debug Controls")]
    [SerializeField] private KeyCode debugKey = KeyCode.F1;
    [SerializeField] private KeyCode killPlayerKey = KeyCode.F2;
    [SerializeField] private bool autoDebugOnStart = true;
    
    private PlayerHealth playerHealth;
    private SelfReviveUI selfReviveUI;
    private ReviveSlotController reviveSlotController;
    
    void Start()
    {
        if (autoDebugOnStart)
        {
            Invoke(nameof(PerformDebugCheck), 1f); // Delay to let everything initialize
        }
    }
    
    void Update()
    {
        if (Input.GetKeyDown(debugKey))
        {
            PerformDebugCheck();
        }
        
        if (Input.GetKeyDown(killPlayerKey))
        {
            KillPlayerForTesting();
        }
    }
    
    void PerformDebugCheck()
    {
        Debug.Log("=== SELF-REVIVE DEBUG CHECK ===");
        
        // Find components
        playerHealth = FindObjectOfType<PlayerHealth>();
        selfReviveUI = FindObjectOfType<SelfReviveUI>();
        reviveSlotController = FindObjectOfType<ReviveSlotController>();
        
        // Check PlayerHealth
        if (playerHealth != null)
        {
            Debug.Log($"✅ PlayerHealth found: {playerHealth.name}");
            Debug.Log($"   - Current Health: {playerHealth.CurrentHealth}/{playerHealth.maxHealth}");
            Debug.Log($"   - Is Dead: {playerHealth.isDead}");
        }
        else
        {
            Debug.LogError("❌ PlayerHealth NOT FOUND in scene!");
        }
        
        // Check SelfReviveUI
        if (selfReviveUI != null)
        {
            Debug.Log($"✅ SelfReviveUI found: {selfReviveUI.name}");
            CheckSelfReviveUIComponents();
        }
        else
        {
            Debug.LogError("❌ SelfReviveUI NOT FOUND in scene!");
            Debug.LogError("   You need to create a GameObject with SelfReviveUI component!");
            CreateSelfReviveUIInstructions();
        }
        
        // Check ReviveSlotController
        if (reviveSlotController != null)
        {
            Debug.Log($"✅ ReviveSlotController found: {reviveSlotController.name}");
            Debug.Log($"   - Has Revives: {reviveSlotController.HasRevives()}");
        }
        else
        {
            Debug.LogError("❌ ReviveSlotController NOT FOUND in scene!");
        }
        
        Debug.Log("=== END DEBUG CHECK ===");
    }
    
    void CheckSelfReviveUIComponents()
    {
        // Check if UI components are assigned
        var revivePromptPanel = selfReviveUI.revivePromptPanel;
        var revivePromptText = selfReviveUI.revivePromptText;
        var countdownText = selfReviveUI.countdownText;
        
        Debug.Log($"   - Revive Prompt Panel: {(revivePromptPanel != null ? "✅ ASSIGNED" : "❌ NULL")}");
        Debug.Log($"   - Revive Prompt Text: {(revivePromptText != null ? "✅ ASSIGNED" : "❌ NULL")}");
        Debug.Log($"   - Countdown Text: {(countdownText != null ? "✅ ASSIGNED" : "❌ NULL")}");
        
        if (revivePromptPanel != null)
        {
            Debug.Log($"   - Panel Active: {revivePromptPanel.activeSelf}");
            Debug.Log($"   - Panel GameObject: {revivePromptPanel.name}");
        }
    }
    
    void CreateSelfReviveUIInstructions()
    {
        Debug.Log("=== HOW TO CREATE SELF-REVIVE UI ===");
        Debug.Log("1. Create a new GameObject in the scene");
        Debug.Log("2. Add SelfReviveUI component to it");
        Debug.Log("3. Create UI Canvas if not exists");
        Debug.Log("4. Create Panel for revive prompt");
        Debug.Log("5. Create Text elements for prompt and countdown");
        Debug.Log("6. Assign all UI references in SelfReviveUI inspector");
        Debug.Log("=====================================");
    }
    
    void KillPlayerForTesting()
    {
        if (playerHealth != null)
        {
            Debug.Log("🔥 KILLING PLAYER FOR TESTING...");
            playerHealth.TakeDamage(playerHealth.CurrentHealth);
        }
        else
        {
            Debug.LogError("Cannot kill player - PlayerHealth not found!");
        }
    }
    
    void OnGUI()
    {
        GUI.color = Color.yellow;
        GUI.Label(new Rect(10, 10, 300, 20), $"Press {debugKey} for Self-Revive Debug");
        GUI.Label(new Rect(10, 30, 300, 20), $"Press {killPlayerKey} to Kill Player (Testing)");
        GUI.color = Color.white;
    }
}
