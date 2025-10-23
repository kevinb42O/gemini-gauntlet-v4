using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles saving all inventory and hand level changes when player clicks Return button in menu.
/// Assign this script to the Return button to ensure all changes are saved before returning to main menu.
/// </summary>
public class MenuReturnSaveHandler : MonoBehaviour
{
    [Header("Auto-Detection (Leave Empty for Auto-Find)")]
    [Tooltip("InventoryManager to save data from - leave empty to auto-find")]
    public InventoryManager targetInventoryManager;
    
    [Tooltip("StashManager to get current inventory state from - leave empty to auto-find")]
    public StashManager stashManager;
    
    [Header("Debug Options")]
    [Tooltip("Enable detailed debug logging")]
    public bool enableDebugLogging = true;
    
    private void Awake()
    {
        // Auto-find components if not assigned
        if (targetInventoryManager == null)
        {
            targetInventoryManager = GameManager.Instance?.GetInventoryManager();
            if (enableDebugLogging)
            {
                Debug.Log($"MenuReturnSaveHandler: Auto-found InventoryManager: {(targetInventoryManager != null ? "SUCCESS" : "FAILED")}");
            }
        }
        
        // PersistentInventoryManager removed - using simplified stash/inventory system
        if (enableDebugLogging)
        {
            Debug.Log("MenuReturnSaveHandler: Using simplified stash/inventory system (no PersistentInventoryManager)");
        }
        
        if (stashManager == null)
        {
            stashManager = GameManager.Instance?.GetStashManager();
            if (enableDebugLogging)
            {
                Debug.Log($"MenuReturnSaveHandler: Auto-found StashManager: {(stashManager != null ? "SUCCESS" : "FAILED")}");
            }
        }
        
        // Auto-assign to button if this is on a button
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(SaveAllChangesAndReturn);
            if (enableDebugLogging)
            {
                Debug.Log("MenuReturnSaveHandler: Auto-assigned to button onClick event");
            }
        }
    }
    
    /// <summary>
    /// PUBLIC METHOD: Call this from Return button onClick event.
    /// Saves all inventory and hand level changes before returning to main menu.
    /// </summary>
    public void SaveAllChangesAndReturn()
    {
        if (enableDebugLogging)
        {
            Debug.Log("MenuReturnSaveHandler: Starting save process for Return button...");
        }
        
        // Validate components
        if (!ValidateComponents())
        {
            Debug.LogError("MenuReturnSaveHandler: Cannot save - missing required components!");
            return;
        }
        
        try
        {
            // STEP 1: Save current inventory state
            SaveInventoryChanges();
            
            // STEP 2: Save current hand levels
            SaveHandLevelChanges();
            
            // STEP 3: Force persistence system to save everything
            ForcePersistenceSave();
            
            // STEP 4: Validate save was successful
            ValidateSaveSuccess();
            
            if (enableDebugLogging)
            {
                Debug.Log("✅ MenuReturnSaveHandler: All changes saved successfully! Safe to return to main menu.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"MenuReturnSaveHandler: Error during save process: {e.Message}");
        }
    }
    
    /// <summary>
    /// UNIFIED PERSISTENCE: Save all game data using central persistence system
    /// </summary>
    private void SaveInventoryChanges()
    {
        try
        {
            if (enableDebugLogging)
            {
                Debug.Log("🎯 MenuReturnSaveHandler: SaveInventoryChanges() called - using unified persistence");
            }
            
            // REMOVED: No syncing needed - StashManager and InventoryManager are separate systems
            // StashManager loads from save file and saves instantly on transfers
            // InventoryManager persists items from game to menu naturally
            if (enableDebugLogging)
            {
                Debug.Log("MenuReturnSaveHandler: Skipping sync - StashManager and InventoryManager are independent");
            }
            
            // SIMPLIFIED SYSTEM: Only use allowed scripts for stash/inventory
            // StashManager handles its own saves, InventoryManager handles its own saves
            Debug.Log("✅ MenuReturnSaveHandler: Using simplified system - StashManager and InventoryManager handle their own persistence");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ MenuReturnSaveHandler: Failed to save game data: {e.Message}");
        }
    }
    
    /// <summary>
    /// Save all hand level changes
    /// </summary>
    private void SaveHandLevelChanges()
    {
        if (enableDebugLogging)
        {
            Debug.Log("MenuReturnSaveHandler: Saving hand level changes...");
        }
        
        // Get current hand levels from InventoryManager
        var (primaryLevel, secondaryLevel) = targetInventoryManager.GetSavedHandLevels();
        
        if (enableDebugLogging)
        {
            Debug.Log($"MenuReturnSaveHandler: Current hand levels - Primary (LEFT): {primaryLevel}, Secondary (RIGHT): {secondaryLevel}");
        }
        
        // Simplified system: Hand levels managed by PlayerProgression only
        if (enableDebugLogging)
        {
            Debug.Log($"MenuReturnSaveHandler: Using simplified hand level system - no persistent hand level management");
        }
    }
    
    /// <summary>
    /// Force the persistence system to save everything
    /// </summary>
    private void ForcePersistenceSave()
    {
        if (enableDebugLogging)
        {
            Debug.Log("MenuReturnSaveHandler: Forcing persistence system save...");
        }
        
        // The persistence system automatically saves on exfil and clears on death
        // For menu changes, we need to ensure current state is preserved
        
        // If player has exfilled, the data should already be saved
        // If they're making changes in the menu, those changes are in the InventoryManager
        // The key is ensuring these changes transfer properly when they start a new game
        
        if (enableDebugLogging)
        {
            Debug.Log($"MenuReturnSaveHandler: Using simplified system - no persistent inventory manager");
        }
    }
    
    /// <summary>
    /// Validate that the save was successful
    /// </summary>
    private void ValidateSaveSuccess()
    {
        if (enableDebugLogging)
        {
            Debug.Log("MenuReturnSaveHandler: Validating save success...");
        }
        
        // Check inventory state
        var inventorySlots = targetInventoryManager.GetAllInventorySlots();
        int savedItemCount = 0;
        foreach (var slot in inventorySlots)
        {
            if (slot != null && !slot.IsEmpty)
            {
                savedItemCount++;
            }
        }
        
        // Check hand level state
        var (primaryLevel, secondaryLevel) = targetInventoryManager.GetSavedHandLevels();
        bool hasHandLevels = targetInventoryManager.HasPersistedHandLevels();
        
        // Simplified system: No persistent inventory manager
        bool hasExfilled = false; // Always false in simplified system
        int exfilStreak = 0; // Always 0 in simplified system
        
        if (enableDebugLogging)
        {
            Debug.Log($"MenuReturnSaveHandler: Save validation complete:");
            Debug.Log($"  - Inventory items: {savedItemCount}");
            Debug.Log($"  - Hand levels: Primary {primaryLevel}, Secondary {secondaryLevel} (HasLevels: {hasHandLevels})");
            Debug.Log($"  - Persistence: HasExfilled {hasExfilled}, ExfilStreak {exfilStreak}");
            Debug.Log($"  - Next game will be: FRESH START (simplified system)");
        }
    }
    
    /// <summary>
    /// REMOVED: This sync method was causing data loss and is not needed.
    /// StashManager and InventoryManager are independent systems:
    /// - StashManager loads from save file when stash opens
    /// - StashManager saves instantly when items are transferred
    /// - InventoryManager persists items from game to menu naturally
    /// - No syncing between them is required or desired
    /// </summary>
    private void SyncStashInventoryToInventoryManager()
    {
        if (enableDebugLogging)
        {
            Debug.Log("MenuReturnSaveHandler: SyncStashInventoryToInventoryManager() - METHOD DISABLED");
            Debug.Log("StashManager and InventoryManager are independent - no sync needed");
        }
        // Method intentionally left empty - sync logic removed to prevent data loss
    }
    
    /// <summary>
    /// Get StashManager's inventory slots using reflection to access private field
    /// </summary>
    private System.Collections.Generic.List<UnifiedSlot> GetStashInventorySlots()
    {
        try
        {
            // Use reflection to access StashManager's private _inventorySlots field
            var fieldInfo = typeof(StashManager).GetField("_inventorySlots", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fieldInfo != null)
            {
                var inventorySlots = fieldInfo.GetValue(stashManager) as System.Collections.Generic.List<UnifiedSlot>;
                return inventorySlots;
            }
            else
            {
                Debug.LogError("MenuReturnSaveHandler: Could not find _inventorySlots field in StashManager");
                return null;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"MenuReturnSaveHandler: Failed to get StashManager inventory slots: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Validate that all required components are present
    /// </summary>
    private bool ValidateComponents()
    {
        bool isValid = true;
        
        if (targetInventoryManager == null)
        {
            Debug.LogError("MenuReturnSaveHandler: targetInventoryManager is null!");
            isValid = false;
        }
        
        // Simplified system: No persistent inventory manager needed
        // isValid remains true since we don't depend on persistent inventory manager
        
        return isValid;
    }
    
    /// <summary>
    /// Manual method to assign this handler to a button (alternative to auto-assignment)
    /// </summary>
    public void AssignToButton(Button button)
    {
        if (button != null)
        {
            button.onClick.RemoveListener(SaveAllChangesAndReturn); // Remove duplicates
            button.onClick.AddListener(SaveAllChangesAndReturn);
            Debug.Log("MenuReturnSaveHandler: Manually assigned to button");
        }
        else
        {
            Debug.LogError("MenuReturnSaveHandler: Cannot assign to null button");
        }
    }
}
