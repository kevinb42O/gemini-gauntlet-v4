using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// BackpackSlotController - Manages the backpack equipment slot using UnifiedSlot system
/// Handles backpack equipping/upgrading and communicates with InventoryManager for slot expansion
/// </summary>
public class BackpackSlotController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The UnifiedSlot that represents the backpack slot")]
    public UnifiedSlot backpackSlot;
    
    [Header("Default Backpack Configuration")]
    [Tooltip("Default Tier 1 backpack that players start with")]
    public BackpackItem defaultTier1Backpack;
    
    [Header("Visual Feedback")]
    [Tooltip("Text to show current backpack tier and slot count")]
    public TextMeshProUGUI backpackInfoText;
    
    // Current equipped backpack
    private BackpackItem currentBackpack;
    
    // Events
    public System.Action<BackpackItem> OnBackpackChanged;
    
    void Start()
    {
        InitializeBackpackSlot();
        
        // Don't auto-equip default backpack - let the persistence system handle it
        // The InventoryManager will call LoadFromSaveData() which will equip the correct backpack
        Debug.Log("[BackpackSlotController] Start() - Waiting for persistence system to load backpack data");
        
        // FALLBACK: If after a short delay we still have no backpack, equip the default Tier1
        // This handles the case where we start directly in game scene without menu
        StartCoroutine(EnsureBackpackEquippedAfterDelay());
    }
    
    /// <summary>
    /// Coroutine to ensure a backpack is equipped after initialization
    /// This handles the case where we start testing directly in game scene
    /// </summary>
    private System.Collections.IEnumerator EnsureBackpackEquippedAfterDelay()
    {
        // Wait for persistence system to load (give it 0.5 seconds)
        yield return new WaitForSeconds(0.5f);
        
        // Check if we still have no backpack equipped
        if (currentBackpack == null)
        {
            Debug.Log("[BackpackSlotController] No backpack loaded after initialization - equipping default Tier1 backpack");
            EquipDefaultBackpack();
        }
        else
        {
            Debug.Log($"[BackpackSlotController] Backpack already equipped: {currentBackpack.GetDisplayName()}");
        }
    }
    
    /// <summary>
    /// Initialize the backpack slot and set up event handlers
    /// </summary>
    void InitializeBackpackSlot()
    {
        if (backpackSlot != null)
        {
            // Configure the slot
            backpackSlot.isStashSlot = false;
            backpackSlot.isGemSlot = false;
            backpackSlot.isForgeInputSlot = false;
            backpackSlot.isForgeOutputSlot = false;
            backpackSlot.isEquipmentSlot = true; // CRITICAL: Mark as equipment slot to prevent dragging OUT
            
            // Subscribe to events
            backpackSlot.OnItemDropped += HandleBackpackDropped;
            backpackSlot.OnDoubleClick += HandleBackpackDoubleClick;
            backpackSlot.OnRightClick += HandleBackpackRightClick;
            
            Debug.Log("[BackpackSlotController] Initialized backpack slot as equipment slot (drag-out blocked)");
        }
        else
        {
            Debug.LogError("[BackpackSlotController] BackpackSlot reference is missing!");
        }
    }
    
    /// <summary>
    /// Handle item dropped onto backpack slot - only allows backpack upgrades from chest/stash
    /// CRITICAL: Prevents dragging equipped backpack out of slot
    /// </summary>
    void HandleBackpackDropped(UnifiedSlot fromSlot, UnifiedSlot toSlot)
    {
        if (fromSlot == null || toSlot != backpackSlot) return;
        
        // CRITICAL FIX: Prevent dragging FROM the backpack slot (unequipping)
        if (fromSlot == backpackSlot)
        {
            Debug.Log($"[BackpackSlotController] ❌ BLOCKED: Cannot drag backpack out of equipment slot - backpacks cannot be unequipped, only replaced");
            return;
        }
        
        // Check if the dropped item is a backpack
        if (fromSlot.CurrentItem is BackpackItem droppedBackpack)
        {
            Debug.Log($"[BackpackSlotController] Backpack dropped: {droppedBackpack.GetDisplayName()}");
            
            // Only allow equipping if it's an upgrade or if no backpack is currently equipped
            if (currentBackpack == null || CanEquipBackpack(droppedBackpack))
            {
                // Equip the new backpack (this will replace the current one)
                bool success = EquipBackpack(droppedBackpack, true);
                
                if (success)
                {
                    // Clear the source slot since the backpack is now equipped
                    fromSlot.ClearSlot();
                    
                    // Save the source container
                    SaveSourceContainer(fromSlot);
                    
                    Debug.Log($"[BackpackSlotController] Successfully replaced backpack with {droppedBackpack.GetDisplayName()}");
                }
            }
            else
            {
                Debug.Log($"[BackpackSlotController] ❌ Cannot replace {currentBackpack.GetDisplayName()} with {droppedBackpack.GetDisplayName()} - not an upgrade (Tier {droppedBackpack.backpackTier} <= Tier {currentBackpack.backpackTier})");
            }
        }
        else
        {
            Debug.Log($"[BackpackSlotController] ❌ Cannot equip non-backpack item: {fromSlot.CurrentItem?.itemName}");
        }
    }
    
    /// <summary>
    /// Handle double-click on backpack slot - backpacks cannot be unequipped, only replaced
    /// CRITICAL: Prevents double-clicking to unequip
    /// </summary>
    void HandleBackpackDoubleClick(UnifiedSlot slot)
    {
        if (currentBackpack != null)
        {
            Debug.Log($"[BackpackSlotController] ❌ BLOCKED: Backpack {currentBackpack.GetDisplayName()} cannot be unequipped - only replaceable with higher tier backpack from chest/stash");
            
            // Note: User feedback removed for menu scene compatibility
        }
    }
    
    /// <summary>
    /// Handle right-click on backpack slot (destroy backpack)
    /// </summary>
    void HandleBackpackRightClick(UnifiedSlot slot)
    {
        if (currentBackpack != null && currentBackpack.backpackTier > 1)
        {
            Debug.Log($"[BackpackSlotController] Destroying backpack: {currentBackpack.GetDisplayName()}");
            
            // Reset to Tier 1 backpack
            ResetToTier1Backpack();
        }
        else
        {
            Debug.Log("[BackpackSlotController] Cannot destroy Tier 1 backpack - it's the default");
        }
    }
    
    /// <summary>
    /// Equip a backpack and update inventory slot count - only allows upgrades or initial equipping
    /// </summary>
    public bool EquipBackpack(BackpackItem newBackpack, bool consumeItem = true)
    {
        if (newBackpack == null) return false;
        
        // Check if this is an upgrade or if no backpack is currently equipped
        if (currentBackpack != null && newBackpack.backpackTier <= currentBackpack.backpackTier)
        {
            Debug.Log($"[BackpackSlotController] Cannot equip {newBackpack.GetDisplayName()} - not an upgrade from current {currentBackpack.GetDisplayName()}");
            
            // Note: User feedback removed for menu scene compatibility
            
            return false;
        }
        
        // Store previous backpack for comparison
        BackpackItem previousBackpack = currentBackpack;
        
        // Equip the new backpack
        currentBackpack = newBackpack;
        
        // Update the visual slot (bypass validation since this is programmatic equipment)
        if (backpackSlot != null)
        {
            backpackSlot.SetItem(newBackpack, 1, bypassValidation: true);
        }
        
        // Update info text
        UpdateBackpackInfoText();
        
        // Notify InventoryManager about slot count change
        NotifyInventoryManager();
        
        // Trigger event
        OnBackpackChanged?.Invoke(newBackpack);
        
        Debug.Log($"[BackpackSlotController] Equipped {newBackpack.GetDisplayName()} - Inventory now has {newBackpack.slotCount} slots");
        
        if (previousBackpack != null)
        {
            Debug.Log($"[BackpackSlotController] Upgraded from {previousBackpack.GetDisplayName()} (previous backpack replaced)");
            
            // Note: User feedback removed for menu scene compatibility
        }
        else
        {
            Debug.Log($"[BackpackSlotController] First backpack equipped: {newBackpack.GetDisplayName()}");
            // Note: User feedback removed for menu scene compatibility
        }
        
        return true;
    }
    
    /// <summary>
    /// Reset to default Tier 1 backpack (called on death)
    /// </summary>
    public void ResetToTier1Backpack()
    {
        if (defaultTier1Backpack != null)
        {
            Debug.Log("[BackpackSlotController] Resetting to Tier 1 backpack due to death");
            
            currentBackpack = defaultTier1Backpack;
            
            // Update visual slot (bypass validation since this is programmatic equipment)
            if (backpackSlot != null)
            {
                backpackSlot.SetItem(defaultTier1Backpack, 1, bypassValidation: true);
            }
            
            // Update info text
            UpdateBackpackInfoText();
            
            // Notify InventoryManager about slot count change
            NotifyInventoryManager();
            
            // Trigger event
            OnBackpackChanged?.Invoke(defaultTier1Backpack);
        }
    }
    
    /// <summary>
    /// Get the currently equipped backpack
    /// </summary>
    public BackpackItem GetCurrentBackpack()
    {
        return currentBackpack;
    }
    
    /// <summary>
    /// Get the current number of inventory slots provided by equipped backpack
    /// </summary>
    public int GetCurrentSlotCount()
    {
        return currentBackpack?.slotCount ?? 5; // Default to 5 if no backpack
    }
    
    /// <summary>
    /// Check if a backpack can be equipped (must be higher tier)
    /// </summary>
    public bool CanEquipBackpack(BackpackItem backpack)
    {
        if (backpack == null) return false;
        if (currentBackpack == null) return true;
        
        return backpack.backpackTier > currentBackpack.backpackTier;
    }
    
    /// <summary>
    /// Update the backpack info text display
    /// </summary>
    void UpdateBackpackInfoText()
    {
        if (backpackInfoText != null && currentBackpack != null)
        {
            backpackInfoText.text = $"Tier {currentBackpack.backpackTier}\n{currentBackpack.slotCount} Slots";
        }
    }
    
    /// <summary>
    /// Notify InventoryManager about backpack slot count change
    /// </summary>
    void NotifyInventoryManager()
    {
        InventoryManager inventoryManager = InventoryManager.Instance;
        if (inventoryManager != null)
        {
            // Tell InventoryManager to update its slot count
            inventoryManager.UpdateInventorySlotCount(GetCurrentSlotCount());
        }
        else
        {
            Debug.LogWarning("[BackpackSlotController] InventoryManager not found - cannot update slot count");
        }
    }
    
    /// <summary>
    /// Save the container that an item came from (inventory or stash)
    /// </summary>
    void SaveSourceContainer(UnifiedSlot sourceSlot)
    {
        // Check if it came from inventory
        InventoryManager inventoryManager = InventoryManager.Instance;
        if (inventoryManager != null)
        {
            var inventorySlots = inventoryManager.GetAllInventorySlots();
            if (inventorySlots != null && inventorySlots.Contains(sourceSlot))
            {
                inventoryManager.SaveInventoryData();
                Debug.Log("[BackpackSlotController] Saved inventory after backpack equip");
                return;
            }
        }
        
        // If not inventory, it might be stash (StashManager handles its own saving)
        Debug.Log("[BackpackSlotController] Item came from stash (auto-save expected)");
    }
    
    /// <summary>
    /// Get backpack data for persistence
    /// </summary>
    public BackpackSaveData GetSaveData()
    {
        return new BackpackSaveData
        {
            backpackTier = currentBackpack?.backpackTier ?? 1,
            backpackName = currentBackpack?.itemName ?? "Default Backpack",
            slotCount = GetCurrentSlotCount()
        };
    }
    
    /// <summary>
    /// Load backpack from save data
    /// </summary>
    public void LoadFromSaveData(BackpackSaveData saveData)
    {
        if (saveData == null)
        {
            Debug.Log("[BackpackSlotController] No save data - equipping default Tier 1 backpack");
            EquipDefaultBackpack();
            return;
        }
        
        Debug.Log($"[BackpackSlotController] Loading backpack from save data: Tier {saveData.backpackTier}");
        
        // Try to load the appropriate backpack based on tier
        BackpackItem backpackToLoad = null;
        
        switch (saveData.backpackTier)
        {
            case 1:
                backpackToLoad = defaultTier1Backpack;
                break;
            case 2:
                // Try to find Tier 2 backpack in Resources
                backpackToLoad = Resources.Load<BackpackItem>("Items/Tier2Backpack");
                if (backpackToLoad == null)
                {
                    Debug.LogWarning("[BackpackSlotController] Tier2Backpack not found in Resources/Items/ - trying alternative names");
                    backpackToLoad = Resources.Load<BackpackItem>("Items/AdvancedBackpack");
                }
                break;
            case 3:
                // Try to find Tier 3 backpack in Resources
                backpackToLoad = Resources.Load<BackpackItem>("Items/Tier3Backpack");
                if (backpackToLoad == null)
                {
                    Debug.LogWarning("[BackpackSlotController] Tier3Backpack not found in Resources/Items/ - trying alternative names");
                    backpackToLoad = Resources.Load<BackpackItem>("Items/MasterBackpack");
                }
                break;
            default:
                Debug.LogWarning($"[BackpackSlotController] Unknown backpack tier {saveData.backpackTier} - equipping default Tier 1");
                EquipDefaultBackpack();
                return;
        }
        
        if (backpackToLoad != null)
        {
            Debug.Log($"[BackpackSlotController] Successfully loaded {backpackToLoad.itemName} (Tier {saveData.backpackTier})");
            // Force equip regardless of current backpack (since we're loading from save)
            currentBackpack = null; // Clear current to allow "upgrade"
            EquipBackpack(backpackToLoad, false);
        }
        else
        {
            Debug.LogError($"[BackpackSlotController] Could not find Tier {saveData.backpackTier} backpack asset - equipping default Tier 1");
            EquipDefaultBackpack();
        }
    }
    
    /// <summary>
    /// Equip the default Tier 1 backpack (fallback method)
    /// </summary>
    void EquipDefaultBackpack()
    {
        if (defaultTier1Backpack != null)
        {
            currentBackpack = null; // Clear current to allow equipping
            EquipBackpack(defaultTier1Backpack, false);
        }
        else
        {
            Debug.LogError("[BackpackSlotController] No default Tier 1 backpack assigned!");
        }
    }
    
    void OnDestroy()
    {
        // Clean up event subscriptions
        if (backpackSlot != null)
        {
            backpackSlot.OnItemDropped -= HandleBackpackDropped;
            backpackSlot.OnDoubleClick -= HandleBackpackDoubleClick;
            backpackSlot.OnRightClick -= HandleBackpackRightClick;
        }
    }
}

/// <summary>
/// Data structure for saving backpack information
/// </summary>
[System.Serializable]
public class BackpackSaveData
{
    public int backpackTier = 1;
    public string backpackName = "Default Backpack";
    public int slotCount = 5;
}
