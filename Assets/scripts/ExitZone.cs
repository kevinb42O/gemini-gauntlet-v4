using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections.Generic;
using CompanionAI;
using GeminiGauntlet.Progression;

[RequireComponent(typeof(Collider))]
public class ExitZone : MonoBehaviour
{
    [Tooltip("Name of the main menu scene to load")]
    public string mainMenuSceneName = "MainMenu";
    
    [Tooltip("Visual effect to play when player enters the exit zone (optional)")]
    public GameObject exitEffect;
    
    [Tooltip("Enable debug mode to show trigger boundaries")]
    public bool debugMode = false;
    
    [Tooltip("Delay in seconds before loading the main menu scene")]
    public float loadDelay = 1.0f;
    
    [Header("Rotation Settings")]
    [Tooltip("Enable rotation of the exit zone object")]
    public bool enableRotation = true;
    
    [Tooltip("Rotation speed in degrees per second (X, Y, Z axes)")]
    public Vector3 rotationSpeed = new Vector3(0f, 45f, 0f);
    
    [Header("Inner Sphere Rotation")]
    [Tooltip("Inner sphere transform for decoration (optional)")]
    public Transform innerSphere;
    
    [Tooltip("Enable rotation of the inner sphere")]
    public bool enableInnerSphereRotation = true;
    
    [Tooltip("Inner sphere rotation speed in degrees per second (X, Y, Z axes) - typically faster and opposite direction")]
    public Vector3 innerSphereRotationSpeed = new Vector3(0f, -120f, 0f);
    
    private bool _triggered = false;
    private Collider _exitCollider;
    private bool _companionXPGranted = false;
    
    private void Awake()
    {
        _exitCollider = GetComponent<Collider>();
        if (_exitCollider == null)
        {
            Debug.LogError($"ExitZone ({name}): Collider component missing! Exit zone will not work.", this);
            enabled = false;
            return;
        }
        
        if (!_exitCollider.isTrigger)
        {
            Debug.LogWarning($"ExitZone ({name}): Collider is not set to 'Is Trigger'. Automatically setting it now.", this);
            _exitCollider.isTrigger = true;
        }
        
        // Make the collider large enough to detect the player in both walking and flying modes
        if (_exitCollider is BoxCollider boxCollider && debugMode)
        {
            Debug.Log($"ExitZone ({name}): Box collider size: {boxCollider.size}");
        }
    }
    
    private void Update()
    {
        // Rotate the exit zone object around its own center
        if (enableRotation)
        {
            transform.Rotate(rotationSpeed * Time.deltaTime, Space.Self);
        }
        
        // Rotate the inner sphere decoration (typically faster and opposite direction)
        if (enableInnerSphereRotation && innerSphere != null)
        {
            innerSphere.Rotate(innerSphereRotationSpeed * Time.deltaTime, Space.Self);
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (_triggered) return; // Prevent multiple triggers
        
        if (other.CompareTag("Player"))
        {
            _triggered = true;
            Debug.Log($"🚨 ExitZone ({name}): Player entered exit zone. Starting XP summary sequence.");
            
            // IMMEDIATE DEBUG
            Debug.Log($"🔍 Player tag check: {other.CompareTag("Player")}");
            Debug.Log($"🔍 Player name: {other.gameObject.name}");
            Debug.Log($"🔍 ExitZone triggered: {_triggered}");
            
            // Spawn effect if assigned
            if (exitEffect != null)
            {
                Instantiate(exitEffect, other.transform.position, Quaternion.identity);
            }
            
            // Handle the exit with XP summary
            StartCoroutine(HandleExitWithXPSummary());
        }
        else
        {
            Debug.LogWarning($"🚨 ExitZone: Object '{other.gameObject.name}' with tag '{other.tag}' entered, but not Player!");
        }
    }
    
    private System.Collections.IEnumerator HandleExitWithXPSummary()
    {
        // Reset time scale and cursor state
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Preserve inventory state first
        PreserveInventoryState();
        
        // Check if we have any XP to show
        bool hasXPToShow = false;
        if (XPManager.Instance != null)
        {
            var summaryData = XPManager.Instance.GetXPSummaryData();
            GrantCompanionXP(summaryData.sessionTotalXP);
            hasXPToShow = summaryData.sessionTotalXP > 0;
            Debug.Log($"ExitZone: Session has {summaryData.sessionTotalXP} XP to show");
        }
        
        if (hasXPToShow)
        {
            // Try to show XP summary UI (fancy version) - include inactive objects
            var xpSummaryUI = FindObjectOfType<GeminiGauntlet.UI.XPSummaryUI>(true);
            if (xpSummaryUI != null)
            {
                Debug.Log("ExitZone: Starting XP summary sequence...");
                xpSummaryUI.ShowXPSummary();
                // XPSummaryUI will handle the transition to menu after animation
                // DO NOT continue - let XPSummaryUI handle everything
                yield break;
            }
            else
            {
                Debug.LogWarning("ExitZone: XPSummaryUI not found, using fallback save method");
            }
        }
        
        // NEW FLOW: XP is already saved to PersistentXPManager during gameplay
        // No need to save to PlayerPrefs - PersistentXPManager handles persistence
        if (XPManager.Instance != null)
        {
            var summaryData = XPManager.Instance.GetXPSummaryData();
            GrantCompanionXP(summaryData.sessionTotalXP);
            Debug.Log($"ExitZone: Session collected {summaryData.sessionTotalXP} XP (already saved to PersistentXPManager)");
        }
        
        // If we get here, either no XP to show or no UI found
        Debug.Log("ExitZone: Going directly to menu after delay...");
        
        // Wait for effect delay only if going directly to menu
        yield return new WaitForSeconds(loadDelay);
        
        // XP is already handled by PersistentXPManager - no manual saving needed
        if (XPManager.Instance != null)
        {
            var summaryData = XPManager.Instance.GetXPSummaryData();
            GrantCompanionXP(summaryData.sessionTotalXP);
            Debug.Log($"ExitZone: Final session XP: {summaryData.sessionTotalXP} (PersistentXPManager will handle menu processing)");
        }
        
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void GrantCompanionXP(int amount)
    {
        if (_companionXPGranted || amount <= 0)
        {
            return;
        }

        HashSet<CompanionData> processedProfiles = new HashSet<CompanionData>();

        IReadOnlyCollection<CompanionData> sessionCompanions = CompanionCore.GetSessionCompanions();
        if (sessionCompanions != null)
        {
            foreach (var profile in sessionCompanions)
            {
                TryAwardXPToProfile(profile, amount, processedProfiles);
            }
        }

        if (processedProfiles.Count == 0)
        {
            CompanionCore[] companions = FindObjectsOfType<CompanionCore>(true);
            foreach (var core in companions)
            {
                if (core == null)
                {
                    continue;
                }

                TryAwardXPToProfile(core.companionProfile, amount, processedProfiles);
            }
        }

        _companionXPGranted = processedProfiles.Count > 0;
    }

    private void TryAwardXPToProfile(CompanionData profile, int amount, HashSet<CompanionData> processedProfiles)
    {
        if (profile == null || processedProfiles.Contains(profile))
        {
            return;
        }

        profile.EnsureProgressionInitialized();
        profile.AddXP(amount);
        processedProfiles.Add(profile);
    }
    
    // Keep the original method for compatibility/fallback
    private System.Collections.IEnumerator HandleExitWithDelay()
    {
        // Reset time scale and cursor state before loading new scene
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Wait for specified delay (for effects to play)
        yield return new WaitForSeconds(loadDelay);
        
        // Preserve the player's inventory state using GameStats or a similar persistent system
        PreserveInventoryState();
        
        // Load the main menu scene
        SceneManager.LoadScene(mainMenuSceneName);
    }
    
    private void PreserveInventoryState()
    {
        // Find the inventory manager
        InventoryManager inventoryManager = FindObjectOfType<InventoryManager>();
        if (inventoryManager == null)
        {
            Debug.LogWarning("ExitZone: Could not find InventoryManager in scene. Inventory will not be preserved.");
            return;
        }
        
        Debug.Log("💾🚪 ExitZone: CRITICAL - Saving complete inventory state before menu transition...");
        
        // NEW: Save inventory to PersistentItemInventoryManager for cross-scene transfer
        if (PersistentItemInventoryManager.Instance != null)
        {
            Debug.Log("🔄 ExitZone: Updating PersistentItemInventoryManager with current inventory...");
            PersistentItemInventoryManager.Instance.UpdateFromInventoryManager(inventoryManager);
            PersistentItemInventoryManager.Instance.SaveInventoryData();
            Debug.Log("✅ ExitZone: Saved inventory to PersistentItemInventoryManager");
        }
        else
        {
            Debug.LogError("❌ ExitZone: PersistentItemInventoryManager.Instance is NULL! Items will not persist to menu!");
        }
        
        // LEGACY: Keep old save system for compatibility
        inventoryManager.SaveInventoryData();
        Debug.Log("✅ ExitZone: Called InventoryManager.SaveInventoryData() - inventory and gems saved to JSON");
        
        // Get gem count using the new simple system
        int gemCount = inventoryManager.GetGemCount();
        Debug.Log($"💎 ExitZone: Current gem count from InventoryManager: {gemCount}");
        
        // Self-revive state is now automatically saved via InventoryManager.SaveInventoryData() above
        // No need for separate PlayerPrefs - the JSON system handles all persistence
        if (inventoryManager.reviveSlot != null)
        {
            bool hasRevive = inventoryManager.reviveSlot.HasRevives();
            Debug.Log($"🔄 ExitZone: Self-revive state: {(hasRevive ? "EQUIPPED" : "NONE")} (saved in JSON inventory data)");
        }
        
        // Set legacy PlayerPrefs flag for compatibility
        PlayerPrefs.SetInt("HasSavedInventory", 1);
        PlayerPrefs.Save();
        
        // Record exfil stats
        GameStats.RecordSuccessfulExfilStats(Time.timeSinceLevelLoad);
        
        Debug.Log("ExitZone: Complete inventory state preserved for main menu");
    }
    
    private void OnDrawGizmos()
    {
        if (!debugMode) return;
        
        Gizmos.color = new Color(0, 1, 0, 0.3f); // Semi-transparent green
        
        // Draw appropriate gizmo based on collider type
        if (GetComponent<BoxCollider>() is BoxCollider boxCollider)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(boxCollider.center, boxCollider.size);
        }
        else if (GetComponent<SphereCollider>() is SphereCollider sphereCollider)
        {
            Gizmos.DrawSphere(transform.position + sphereCollider.center, sphereCollider.radius);
        }
        else
        {
            // Fall back to a simple wireframe cube for other collider types
            Gizmos.DrawWireCube(transform.position, Vector3.one * 3f);
        }
    }
}
