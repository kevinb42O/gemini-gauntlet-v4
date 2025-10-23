using UnityEngine;

/// <summary>
/// Helper class to integrate gem collection with the uniform slot system.
/// Handles adding collected gems as GemItemData items to the appropriate gem slots.
/// </summary>
public class UniformGemCollectionHelper : MonoBehaviour
{
    public static UniformGemCollectionHelper Instance { get; private set; }
    
    [Header("Gem Item Configuration")]
    [Tooltip("The GemItemData asset to use for collected gems")]
    public GemItemData standardGemItem;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Add a collected gem to the inventory system as a GemItemData item
    /// </summary>
    public void AddCollectedGem(int gemCount = 1)
    {
        Debug.Log($"💎 UniformGemCollectionHelper: AddCollectedGem called for {gemCount} gem(s)");
        
        if (standardGemItem == null)
        {
            Debug.LogError("💎 UniformGemCollectionHelper: standardGemItem not assigned! Cannot add gems to inventory.");
            return;
        }
        
        Debug.Log($"💎 standardGemItem is assigned: {standardGemItem.name}");
        
        // Try to add gem to inventory in game scene
        Debug.Log("💎 Attempting to add gem to game inventory...");
        if (TryAddGemToGameInventory(gemCount))
        {
            Debug.Log($"💎 SUCCESS: Added {gemCount} gem(s) to game inventory");
            return;
        }
        
        // Try to add gem to menu stash system
        Debug.Log("💎 Attempting to add gem to menu stash...");
        if (TryAddGemToMenuStash(gemCount))
        {
            Debug.Log($"💎 SUCCESS: Added {gemCount} gem(s) to menu stash");
            return;
        }
        
        Debug.LogError($"💎 FAILURE: Could not add {gemCount} gem(s) - no available gem collection system found");
    }
    
    /// <summary>
    /// Try to add gems to game scene inventory (when playing)
    /// </summary>
    private bool TryAddGemToGameInventory(int gemCount)
    {
        Debug.Log($"💎 TryAddGemToGameInventory: Searching for InventoryManager...");
        
        // Find InventoryManager in game scene
        InventoryManager inventoryManager = FindFirstObjectByType<InventoryManager>();
        if (inventoryManager == null)
        {
            Debug.LogWarning("💎 TryAddGemToGameInventory: InventoryManager not found in scene!");
            return false;
        }
        
        Debug.Log($"💎 TryAddGemToGameInventory: Found InventoryManager: {inventoryManager.name}");
        Debug.Log($"💎 TryAddGemToGameInventory: Calling AddGemToInventory with standardGemItem='{standardGemItem.name}' count={gemCount}");
        
        // Try to add gems using the new simplified TryAddItem method (gems auto-route to gem slots)
        bool result = inventoryManager.TryAddItem(standardGemItem, gemCount);
        
        Debug.Log($"💎 TryAddGemToGameInventory: AddGemToInventory returned: {result}");
        return result;
    }
    
    /// <summary>
    /// Try to add gems to menu stash system (when in menu)
    /// </summary>
    private bool TryAddGemToMenuStash(int gemCount)
    {
        // Find StashManager in menu scene
        StashManager stashManager = FindFirstObjectByType<StashManager>();
        if (stashManager == null) return false;
        
        // Try to add to inventory gem slot first (player's current gems)
        UnifiedSlot inventoryGemSlot = stashManager.inventoryGemSlot;
        if (inventoryGemSlot != null && TryAddGemToSlot(inventoryGemSlot, gemCount))
        {
            Debug.Log($"💎 Added {gemCount} gems to inventory gem slot");
            return true;
        }
        
        // If inventory gem slot full, try stash gem slot
        UnifiedSlot stashGemSlot = stashManager.stashGemSlot;
        if (stashGemSlot != null && TryAddGemToSlot(stashGemSlot, gemCount))
        {
            Debug.Log($"💎 Added {gemCount} gems to stash gem slot");
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Try to add gems to a specific UnifiedSlot
    /// </summary>
    private bool TryAddGemToSlot(UnifiedSlot slot, int gemCount)
    {
        if (slot == null || !slot.isGemSlot) return false;
        
        if (slot.IsEmpty)
        {
            // Add new gem item to empty slot
            slot.SetItem(standardGemItem, gemCount);
        }
        else if (slot.CurrentItem is GemItemData)
        {
            // Stack with existing gems
            slot.SetItem(slot.CurrentItem, slot.ItemCount + gemCount);
        }
        else
        {
            // Slot occupied by non-gem item
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Get total gem count from all gem slots
    /// </summary>
    public int GetTotalGemCount()
    {
        int totalGems = 0;
        
        // Check game inventory
        InventoryManager inventoryManager = FindFirstObjectByType<InventoryManager>();
        if (inventoryManager != null)
        {
            // Would need to implement gem counting in InventoryManager
        }
        
        // Check menu stash
        StashManager stashManager = FindFirstObjectByType<StashManager>();
        if (stashManager != null)
        {
            if (stashManager.inventoryGemSlot != null && stashManager.inventoryGemSlot.CurrentItem is GemItemData)
            {
                totalGems += stashManager.inventoryGemSlot.ItemCount;
            }
            
            if (stashManager.stashGemSlot != null && stashManager.stashGemSlot.CurrentItem is GemItemData)
            {
                totalGems += stashManager.stashGemSlot.ItemCount;
            }
        }
        
        return totalGems;
    }
    
    /// <summary>
    /// Test method to diagnose gem collection setup - call this manually to test
    /// </summary>
    [ContextMenu("Test Gem Collection Setup")]
    public void TestGemCollectionSetup()
    {
        Debug.Log("🔧 === GEM COLLECTION SETUP TEST ===");
        
        // Test 1: Check if UniformGemCollectionHelper instance exists
        Debug.Log($"🔧 UniformGemCollectionHelper.Instance = {(Instance != null ? "EXISTS" : "NULL")}");
        
        // Test 2: Check standardGemItem assignment
        Debug.Log($"🔧 standardGemItem = {(standardGemItem != null ? standardGemItem.name : "NULL - NOT ASSIGNED!")}");
        
        // Test 3: Check for InventoryManager in scene
        InventoryManager inventoryManager = FindFirstObjectByType<InventoryManager>();
        Debug.Log($"🔧 InventoryManager found = {(inventoryManager != null ? inventoryManager.name : "NULL - NOT FOUND!")}");
        
        if (inventoryManager != null)
        {
            Debug.Log($"🔧 InventoryManager.useUniformGemSystem = {inventoryManager.useUniformGemSystem}");
        }
        
        // Test 4: Check for UnifiedSlots in scene
        UnifiedSlot[] allSlots = FindObjectsOfType<UnifiedSlot>();
        Debug.Log($"🔧 Total UnifiedSlots in scene = {allSlots.Length}");
        
        int gemSlotCount = 0;
        foreach (var slot in allSlots)
        {
            if (slot.isGemSlot)
            {
                gemSlotCount++;
                Debug.Log($"🔧 Found gem slot: {slot.name} (isStashSlot={slot.isStashSlot})");
            }
        }
        
        Debug.Log($"🔧 Total gem slots found = {gemSlotCount}");
        
        // Test 5: Try a manual gem collection
        if (standardGemItem != null)
        {
            Debug.Log("🔧 Attempting manual gem collection test...");
            AddCollectedGem(1);
        }
        else
        {
            Debug.LogError("🔧 Cannot test gem collection - standardGemItem not assigned!");
        }
        
        Debug.Log("🔧 === GEM COLLECTION SETUP TEST COMPLETE ===");
    }
    
    /// <summary>
    /// Update method to test gem collection with F1 key
    /// </summary>
    void Update()
    {
        // Press F1 to test gem collection setup
        if (Input.GetKeyDown(KeyCode.F1))
        {
            TestGemCollectionSetup();
        }
        
        // Press F2 to test adding a gem manually
        if (Input.GetKeyDown(KeyCode.F2))
        {
            Debug.Log("🔧 F2 pressed - Manual gem add test");
            AddCollectedGem(1);
        }
    }
}
