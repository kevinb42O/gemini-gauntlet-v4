using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// VestSlotController - Manages the vest equipment slot using UnifiedSlot system
/// Handles vest equipping/upgrading and communicates with ArmorPlateSystem for plate capacity
/// Similar to BackpackSlotController but for armor plates
/// </summary>
public class VestSlotController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The UnifiedSlot that represents the vest slot")]
    public UnifiedSlot vestSlot;
    
    [Header("Default Vest Configuration")]
    [Tooltip("Default T1 vest that players start with")]
    public VestItem defaultT1Vest;
    
    [Header("Visual Feedback")]
    [Tooltip("Text to show current vest tier and plate capacity")]
    public TextMeshProUGUI vestInfoText;
    
    // Current equipped vest
    private VestItem currentVest;
    
    // Events
    public System.Action<VestItem> OnVestChanged;
    
    void Start()
    {
        InitializeVestSlot();
        
        // Equip default T1 vest on start (players always have a vest)
        if (defaultT1Vest != null)
        {
            Debug.Log("[VestSlotController] Equipping default T1 vest on start");
            EquipVest(defaultT1Vest, false);
        }
        else
        {
            Debug.LogError("[VestSlotController] No default T1 vest assigned! Players need a default vest.");
        }
    }
    
    /// <summary>
    /// Initialize the vest slot and set up event handlers
    /// </summary>
    void InitializeVestSlot()
    {
        if (vestSlot != null)
        {
            // Configure the slot
            vestSlot.isStashSlot = false;
            vestSlot.isGemSlot = false;
            vestSlot.isForgeInputSlot = false;
            vestSlot.isForgeOutputSlot = false;
            vestSlot.isEquipmentSlot = true; // CRITICAL: Mark as equipment slot to prevent dragging OUT
            
            // Subscribe to events
            vestSlot.OnItemDropped += HandleVestDropped;
            vestSlot.OnDoubleClick += HandleVestDoubleClick;
            vestSlot.OnRightClick += HandleVestRightClick;
            
            Debug.Log("[VestSlotController] Initialized vest slot as equipment slot (drag-out blocked)");
        }
        else
        {
            Debug.LogError("[VestSlotController] VestSlot reference is missing!");
        }
    }
    
    /// <summary>
    /// Handle item dropped onto vest slot - only allows vest upgrades from chest/stash
    /// CRITICAL: Prevents dragging equipped vest out of slot
    /// </summary>
    void HandleVestDropped(UnifiedSlot fromSlot, UnifiedSlot toSlot)
    {
        if (fromSlot == null || toSlot != vestSlot) return;
        
        // CRITICAL FIX: Prevent dragging FROM the vest slot (unequipping)
        if (fromSlot == vestSlot)
        {
            Debug.Log($"[VestSlotController] ❌ BLOCKED: Cannot drag vest out of equipment slot - vests cannot be unequipped, only replaced");
            return;
        }
        
        // Check if the dropped item is a vest
        if (fromSlot.CurrentItem is VestItem droppedVest)
        {
            Debug.Log($"[VestSlotController] Vest dropped: {droppedVest.GetDisplayName()}");
            
            // Only allow equipping if it's an upgrade or if no vest is currently equipped
            if (currentVest == null || CanEquipVest(droppedVest))
            {
                // Equip the new vest (this will replace the current one)
                bool success = EquipVest(droppedVest, true);
                
                if (success)
                {
                    // Clear the source slot since the vest is now equipped
                    fromSlot.ClearSlot();
                    
                    // Save the source container
                    SaveSourceContainer(fromSlot);
                    
                    Debug.Log($"[VestSlotController] Successfully replaced vest with {droppedVest.GetDisplayName()}");
                }
            }
            else
            {
                Debug.Log($"[VestSlotController] ❌ Cannot replace {currentVest.GetDisplayName()} with {droppedVest.GetDisplayName()} - not an upgrade (Tier {droppedVest.vestTier} <= Tier {currentVest.vestTier})");
            }
        }
        else
        {
            Debug.Log($"[VestSlotController] ❌ Cannot equip non-vest item: {fromSlot.CurrentItem?.itemName}");
        }
    }
    
    /// <summary>
    /// Handle double-click on vest slot - vests cannot be unequipped, only replaced
    /// CRITICAL: Prevents double-clicking to unequip
    /// </summary>
    void HandleVestDoubleClick(UnifiedSlot slot)
    {
        if (currentVest != null)
        {
            Debug.Log($"[VestSlotController] ❌ BLOCKED: Vest {currentVest.GetDisplayName()} cannot be unequipped - only replaceable with higher tier vest from chest/stash");
        }
    }
    
    /// <summary>
    /// Handle right-click on vest slot (destroy vest and reset to T1)
    /// </summary>
    void HandleVestRightClick(UnifiedSlot slot)
    {
        if (currentVest != null && currentVest.vestTier > 1)
        {
            Debug.Log($"[VestSlotController] Destroying vest: {currentVest.GetDisplayName()}");
            
            // Reset to T1 vest
            ResetToT1Vest();
        }
        else
        {
            Debug.Log("[VestSlotController] Cannot destroy T1 vest - it's the default");
        }
    }
    
    /// <summary>
    /// Equip a vest and update armor plate capacity - only allows upgrades or initial equipping
    /// </summary>
    public bool EquipVest(VestItem newVest, bool consumeItem = true)
    {
        if (newVest == null) return false;
        
        // Check if this is an upgrade or if no vest is currently equipped
        if (currentVest != null && newVest.vestTier <= currentVest.vestTier)
        {
            Debug.Log($"[VestSlotController] Cannot equip {newVest.GetDisplayName()} - not an upgrade from current {currentVest.GetDisplayName()}");
            return false;
        }
        
        // Store previous vest for comparison
        VestItem previousVest = currentVest;
        
        // Equip the new vest
        currentVest = newVest;
        
        // Update the visual slot (bypass validation since this is programmatic equipment)
        if (vestSlot != null)
        {
            vestSlot.SetItem(newVest, 1, bypassValidation: true);
        }
        
        // Update info text
        UpdateVestInfoText();
        
        // Notify ArmorPlateSystem about plate capacity change
        NotifyArmorPlateSystem();
        
        // Trigger event
        OnVestChanged?.Invoke(newVest);
        
        Debug.Log($"[VestSlotController] Equipped {newVest.GetDisplayName()} - Armor capacity now {newVest.maxPlates} plates");
        
        if (previousVest != null)
        {
            Debug.Log($"[VestSlotController] Upgraded from {previousVest.GetDisplayName()} (previous vest replaced)");
        }
        else
        {
            Debug.Log($"[VestSlotController] First vest equipped: {newVest.GetDisplayName()}");
        }
        
        return true;
    }
    
    /// <summary>
    /// Reset to default T1 vest (called on death)
    /// </summary>
    public void ResetToT1Vest()
    {
        if (defaultT1Vest != null)
        {
            Debug.Log("[VestSlotController] Resetting to T1 vest due to death");
            
            currentVest = defaultT1Vest;
            
            // Update visual slot (bypass validation since this is programmatic equipment)
            if (vestSlot != null)
            {
                vestSlot.SetItem(defaultT1Vest, 1, bypassValidation: true);
            }
            
            // Update info text
            UpdateVestInfoText();
            
            // Notify ArmorPlateSystem about plate capacity change
            NotifyArmorPlateSystem();
            
            // Trigger event
            OnVestChanged?.Invoke(defaultT1Vest);
        }
    }
    
    /// <summary>
    /// Get the currently equipped vest
    /// </summary>
    public VestItem GetCurrentVest()
    {
        return currentVest;
    }
    
    /// <summary>
    /// Get the current maximum plate capacity provided by equipped vest
    /// </summary>
    public int GetCurrentMaxPlates()
    {
        return currentVest?.maxPlates ?? 1; // Default to 1 if no vest
    }
    
    /// <summary>
    /// Check if a vest can be equipped (must be higher tier)
    /// </summary>
    public bool CanEquipVest(VestItem vest)
    {
        if (vest == null) return false;
        if (currentVest == null) return true;
        
        return vest.vestTier > currentVest.vestTier;
    }
    
    /// <summary>
    /// Update the vest info text display
    /// </summary>
    void UpdateVestInfoText()
    {
        if (vestInfoText != null && currentVest != null)
        {
            vestInfoText.text = $"T{currentVest.vestTier} Vest\n{currentVest.maxPlates} Plates";
        }
    }
    
    /// <summary>
    /// Notify ArmorPlateSystem about vest plate capacity change
    /// </summary>
    void NotifyArmorPlateSystem()
    {
        ArmorPlateSystem armorSystem = FindObjectOfType<ArmorPlateSystem>();
        if (armorSystem != null)
        {
            // Tell ArmorPlateSystem to update its max plate count
            armorSystem.UpdateMaxPlates(GetCurrentMaxPlates());
        }
        else
        {
            Debug.LogWarning("[VestSlotController] ArmorPlateSystem not found - cannot update plate capacity");
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
                Debug.Log("[VestSlotController] Saved inventory after vest equip");
                return;
            }
        }
        
        // If not inventory, it might be stash (StashManager handles its own saving)
        Debug.Log("[VestSlotController] Item came from stash (auto-save expected)");
    }
    
    /// <summary>
    /// Get vest data for persistence
    /// </summary>
    public VestSaveData GetSaveData()
    {
        return new VestSaveData
        {
            vestTier = currentVest?.vestTier ?? 1,
            vestName = currentVest?.itemName ?? "Default Vest",
            maxPlates = GetCurrentMaxPlates()
        };
    }
    
    /// <summary>
    /// Load vest from save data
    /// </summary>
    public void LoadFromSaveData(VestSaveData saveData)
    {
        if (saveData == null)
        {
            Debug.Log("[VestSlotController] No save data - equipping default T1 vest");
            EquipDefaultVest();
            return;
        }
        
        Debug.Log($"[VestSlotController] Loading vest from save data: Tier {saveData.vestTier}");
        
        // Try to load the appropriate vest based on tier
        VestItem vestToLoad = null;
        
        switch (saveData.vestTier)
        {
            case 1:
                vestToLoad = defaultT1Vest;
                break;
            case 2:
                // Try to find Tier 2 vest in Resources
                vestToLoad = Resources.Load<VestItem>("Items/T2_Vest");
                if (vestToLoad == null)
                {
                    Debug.LogWarning("[VestSlotController] T2_Vest not found in Resources/Items/ - trying alternative names");
                    vestToLoad = Resources.Load<VestItem>("Items/Tier2Vest");
                }
                break;
            case 3:
                // Try to find Tier 3 vest in Resources
                vestToLoad = Resources.Load<VestItem>("Items/T3_Vest");
                if (vestToLoad == null)
                {
                    Debug.LogWarning("[VestSlotController] T3_Vest not found in Resources/Items/ - trying alternative names");
                    vestToLoad = Resources.Load<VestItem>("Items/Tier3Vest");
                }
                break;
            default:
                Debug.LogWarning($"[VestSlotController] Unknown vest tier {saveData.vestTier} - equipping default T1");
                EquipDefaultVest();
                return;
        }
        
        if (vestToLoad != null)
        {
            Debug.Log($"[VestSlotController] Successfully loaded {vestToLoad.itemName} (Tier {saveData.vestTier})");
            // Force equip regardless of current vest (since we're loading from save)
            currentVest = null; // Clear current to allow "upgrade"
            EquipVest(vestToLoad, false);
        }
        else
        {
            Debug.LogError($"[VestSlotController] Could not find Tier {saveData.vestTier} vest asset - equipping default T1");
            EquipDefaultVest();
        }
    }
    
    /// <summary>
    /// Equip the default T1 vest (fallback method)
    /// </summary>
    void EquipDefaultVest()
    {
        if (defaultT1Vest != null)
        {
            currentVest = null; // Clear current to allow equipping
            EquipVest(defaultT1Vest, false);
        }
        else
        {
            Debug.LogError("[VestSlotController] No default T1 vest assigned!");
        }
    }
    
    void OnDestroy()
    {
        // Clean up event subscriptions
        if (vestSlot != null)
        {
            vestSlot.OnItemDropped -= HandleVestDropped;
            vestSlot.OnDoubleClick -= HandleVestDoubleClick;
            vestSlot.OnRightClick -= HandleVestRightClick;
        }
    }
}

/// <summary>
/// Data structure for saving vest information
/// </summary>
[System.Serializable]
public class VestSaveData
{
    public int vestTier = 1;
    public string vestName = "Default Vest";
    public int maxPlates = 1;
}
