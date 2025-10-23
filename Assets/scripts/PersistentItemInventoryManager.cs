using UnityEngine;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Simple persistent inventory data manager that survives scene transitions
/// Only handles item data - no UI, no references, just pure data persistence
/// </summary>
public class PersistentItemInventoryManager : MonoBehaviour
{
    public static PersistentItemInventoryManager Instance { get; private set; }
    
    // Current inventory data
    private PersistentInventoryData currentInventory;
    
    // Flag to prevent saves during loading process
    private bool isLoading = false;

    [System.Serializable]
    public class PersistentItemData
    {
        public string itemName;
        public string itemPath;
        public int itemCount;
        
        public PersistentItemData() { }
        
        public PersistentItemData(string name, string path, int count)
        {
            itemName = name;
            itemPath = path;
            itemCount = count;
        }
    }
    
    [System.Serializable]
    public class PersistentInventoryData
    {
        public List<PersistentItemData> items = new List<PersistentItemData>();
        public int gemCount = 0;
        public int selfReviveCount = 0; // Added self revive persistence
        public BackpackSaveData backpackData = new BackpackSaveData(); // Added backpack persistence
        public VestSaveData vestData = new VestSaveData(); // VEST SYSTEM: Added vest persistence
    }
    
    // Save path
    private string savePath;
    
    void Awake()
    {
        // Singleton pattern with proper persistence
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Initialize inventory data
            currentInventory = new PersistentInventoryData();
            
            // Set up save path
            savePath = Path.Combine(Application.persistentDataPath, "persistent_inventory.json");
            Debug.Log($"[PersistentItemInventoryManager] Save path: {savePath}");
            
            // Load existing data
            LoadInventoryData();
            Debug.Log("[PersistentItemInventoryManager] Created persistent inventory manager (DontDestroyOnLoad)");
        }
        else
        {
            // This is a duplicate from another scene
            Debug.Log("[PersistentItemInventoryManager] Duplicate instance found - this scene will use the persistent instance");
            
            // Initialize this instance's data from the persistent one
            currentInventory = new PersistentInventoryData();
            savePath = Path.Combine(Application.persistentDataPath, "persistent_inventory.json");
            
            // Load current data from file to sync with persistent instance
            LoadInventoryData();
            
            // Don't destroy this instance - let it serve this scene, but don't make it the singleton
            Debug.Log("[PersistentItemInventoryManager] Scene-specific instance ready (not persistent)");
        }
    }
    
    /// <summary>
    /// Save current inventory data to file
    /// </summary>
    public void SaveInventoryData()
    {
        // CRITICAL: Don't save during loading process - this prevents corruption!
        if (isLoading)
        {
            Debug.LogWarning($"[PersistentItemInventoryManager] 🛑 BLOCKING SAVE during loading process to prevent data corruption!");
            return;
        }
        
        try
        {
            Debug.Log($"[PersistentItemInventoryManager] ⚠️ SAVE OPERATION TRIGGERED ⚠️");
            Debug.Log($"[PersistentItemInventoryManager] Starting JSON serialization...");
            Debug.Log($"[PersistentItemInventoryManager] Items to serialize: {currentInventory.items.Count}");
            Debug.Log($"[PersistentItemInventoryManager] Backpack data: tier {currentInventory.backpackData.backpackTier}");
            Debug.Log($"[PersistentItemInventoryManager] Vest data: tier {currentInventory.vestData.vestTier}");
            
            // Log if inventory is empty (for debugging) but allow the save
            if (currentInventory.items.Count == 0 && currentInventory.gemCount == 0)
            {
                string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                Debug.Log($"[PersistentItemInventoryManager] ℹ️ Saving empty inventory (scene: {currentScene}) - this is normal when all items are in stash");
            }
            
            string json = JsonUtility.ToJson(currentInventory, true);
            Debug.Log($"[PersistentItemInventoryManager] JSON serialization successful, writing to file: {savePath}");
            
            File.WriteAllText(savePath, json);
            Debug.Log($"[PersistentItemInventoryManager] ✅ SAVE SUCCESSFUL: {currentInventory.items.Count} items, {currentInventory.gemCount} gems");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PersistentItemInventoryManager] ❌ SAVE FAILED: {e.Message}");
            Debug.LogError($"[PersistentItemInventoryManager] Stack trace: {e.StackTrace}");
        }
    }
    
    /// <summary>
    /// Load inventory data from file
    /// </summary>
    public void LoadInventoryData()
    {
        try
        {
            Debug.Log($"[PersistentItemInventoryManager] *** LOAD OPERATION STARTING ***");
            Debug.Log($"[PersistentItemInventoryManager] Checking for save file: {savePath}");
            
            if (File.Exists(savePath))
            {
                Debug.Log($"[PersistentItemInventoryManager] Save file found, reading...");
                string json = File.ReadAllText(savePath);
                Debug.Log($"[PersistentItemInventoryManager] JSON read successful, deserializing...");
                
                currentInventory = JsonUtility.FromJson<PersistentInventoryData>(json);
                Debug.Log($"[PersistentItemInventoryManager] ✅ LOAD SUCCESSFUL: {currentInventory.items.Count} items, {currentInventory.gemCount} gems, backpack tier {currentInventory.backpackData.backpackTier}, vest tier {currentInventory.vestData.vestTier}");
                
                // Debug: List loaded items
                if (currentInventory.items.Count > 0)
                {
                    Debug.Log("[PersistentItemInventoryManager] Loaded items from save file:");
                    for (int i = 0; i < currentInventory.items.Count; i++)
                    {
                        var item = currentInventory.items[i];
                        Debug.Log($"  [{i}] {item.itemName} x{item.itemCount} from {item.itemPath}");
                    }
                }
                else
                {
                    Debug.LogWarning("[PersistentItemInventoryManager] ⚠️ Save file contains NO ITEMS!");
                }
            }
            else
            {
                Debug.Log("[PersistentItemInventoryManager] No save file found, starting with empty inventory");
                currentInventory = new PersistentInventoryData();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PersistentItemInventoryManager] ❌ LOAD FAILED: {e.Message}");
            Debug.LogError($"[PersistentItemInventoryManager] Stack trace: {e.StackTrace}");
            currentInventory = new PersistentInventoryData();
        }
    }
    
    /// <summary>
    /// Update inventory from InventoryManager slots
    /// </summary>
    public void UpdateFromInventoryManager(InventoryManager inventoryManager)
    {
        if (inventoryManager == null) return;
        
        // CRITICAL: Don't update during loading process - this prevents data corruption!
        if (isLoading)
        {
            Debug.LogWarning($"[PersistentItemInventoryManager] 🛑 BLOCKING UpdateFromInventoryManager during loading process to prevent data corruption!");
            return;
        }
        
        currentInventory.items.Clear();
        currentInventory.gemCount = inventoryManager.currentGemCount;
        
        // Update self revive count from ReviveSlotController
        if (inventoryManager.reviveSlot != null)
        {
            currentInventory.selfReviveCount = inventoryManager.reviveSlot.GetReviveCount();
        }
        else
        {
            currentInventory.selfReviveCount = 0;
        }
        
        // Update backpack data from BackpackSlotController (with error handling)
        try
        {
            if (inventoryManager.backpackSlot != null)
            {
                currentInventory.backpackData = inventoryManager.backpackSlot.GetSaveData();
                Debug.Log($"[PersistentItemInventoryManager] Saved backpack data: Tier {currentInventory.backpackData.backpackTier}, {currentInventory.backpackData.slotCount} slots");
            }
            else
            {
                currentInventory.backpackData = new BackpackSaveData(); // Default Tier 1
                Debug.LogWarning("[PersistentItemInventoryManager] BackpackSlot is null, using default Tier 1");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PersistentItemInventoryManager] Error updating backpack data: {ex.Message}");
            currentInventory.backpackData = new BackpackSaveData(); // Fallback to default
        }
        
        // VEST SYSTEM: Update vest data from VestSlotController (with error handling)
        try
        {
            if (inventoryManager.vestSlot != null)
            {
                currentInventory.vestData = inventoryManager.vestSlot.GetSaveData();
                Debug.Log($"[PersistentItemInventoryManager] Saved vest data: Tier {currentInventory.vestData.vestTier}, {currentInventory.vestData.maxPlates} plates");
            }
            else
            {
                currentInventory.vestData = new VestSaveData(); // Default T1
                Debug.LogWarning("[PersistentItemInventoryManager] VestSlot is null, using default T1");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PersistentItemInventoryManager] Error updating vest data: {ex.Message}");
            currentInventory.vestData = new VestSaveData(); // Fallback to default
        }
        
        // Get all inventory slots from the manager
        var slots = inventoryManager.GetAllInventorySlots();
        
        foreach (var slot in slots)
        {
            if (slot != null && !slot.IsEmpty && slot.CurrentItem != null)
            {
                // Skip gem slots - gems are handled separately
                if (slot.isGemSlot) continue;
                
                string resourcePath = GetItemResourcePath(slot.CurrentItem);
                PersistentItemData itemData = new PersistentItemData(
                    slot.CurrentItem.itemName,
                    resourcePath,
                    slot.ItemCount
                );
                
                currentInventory.items.Add(itemData);
                Debug.Log($"[PersistentItemInventoryManager] Saving item: {slot.CurrentItem.itemName} x{slot.ItemCount} at path: {resourcePath}");
            }
        }
        
        Debug.Log($"[PersistentItemInventoryManager] *** SAVE OPERATION COMPLETE ***");
        Debug.Log($"[PersistentItemInventoryManager] Updated from InventoryManager: {currentInventory.items.Count} items, {currentInventory.gemCount} gems, {currentInventory.selfReviveCount} self revives, backpack tier {currentInventory.backpackData.backpackTier}, vest tier {currentInventory.vestData.vestTier}");
        Debug.Log($"[PersistentItemInventoryManager] Save path: {savePath}");
    }
    
    /// <summary>
    /// Apply inventory data to InventoryManager slots
    /// </summary>
    public void ApplyToInventoryManager(InventoryManager inventoryManager)
    {
        if (inventoryManager == null) 
        {
            Debug.LogError("[PersistentItemInventoryManager] ApplyToInventoryManager: inventoryManager is null!");
            return;
        }
        
        // CRITICAL: Set loading flag to prevent saves during this process
        isLoading = true;
        Debug.Log($"[PersistentItemInventoryManager] 🔒 LOADING MODE ENABLED - Saves blocked to prevent corruption");
        
        Debug.Log($"[PersistentItemInventoryManager] *** LOAD OPERATION STARTING ***");
        Debug.Log($"[PersistentItemInventoryManager] ApplyToInventoryManager: Starting with {currentInventory.items.Count} items to apply");
        Debug.Log($"[PersistentItemInventoryManager] Gems to restore: {currentInventory.gemCount}");
        Debug.Log($"[PersistentItemInventoryManager] Self revives to restore: {currentInventory.selfReviveCount}");
        
        // Debug: List all items to be loaded
        if (currentInventory.items.Count > 0)
        {
            Debug.Log($"[PersistentItemInventoryManager] Items to load:");
            for (int i = 0; i < currentInventory.items.Count; i++)
            {
                var item = currentInventory.items[i];
                Debug.Log($"  [{i}] {item.itemName} x{item.itemCount} from {item.itemPath}");
            }
        }
        else
        {
            Debug.LogWarning("[PersistentItemInventoryManager] ⚠️ NO ITEMS TO LOAD - inventory is empty!");
        }
        
        // REMOVED: Don't clear existing items automatically - this was destroying items on successful exfil
        // Only clear items when player actually dies, not when successfully exfiltrating
        // inventoryManager.ClearNonGemSlots();
        
        // Set gem count
        inventoryManager.currentGemCount = currentInventory.gemCount;
        inventoryManager.UpdateGemDisplay();
        Debug.Log($"[PersistentItemInventoryManager] Set gem count to {currentInventory.gemCount}");
        
        // Set self revive count
        if (inventoryManager.reviveSlot != null)
        {
            inventoryManager.reviveSlot.SetReviveCount(currentInventory.selfReviveCount);
            Debug.Log($"[PersistentItemInventoryManager] Set self revive count to {currentInventory.selfReviveCount}");
        }
        else
        {
            Debug.LogWarning("[PersistentItemInventoryManager] ReviveSlot is null, cannot restore self revive count");
        }
        
        // Set backpack data (with error handling)
        try
        {
            if (inventoryManager.backpackSlot != null)
            {
                inventoryManager.backpackSlot.LoadFromSaveData(currentInventory.backpackData);
                Debug.Log($"[PersistentItemInventoryManager] ✅ RESTORED backpack to tier {currentInventory.backpackData.backpackTier} ({currentInventory.backpackData.slotCount} slots)");
            }
            else
            {
                Debug.LogWarning("[PersistentItemInventoryManager] BackpackSlot is null, cannot restore backpack data");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PersistentItemInventoryManager] Error loading backpack data: {ex.Message}");
            Debug.LogError($"[PersistentItemInventoryManager] Stack trace: {ex.StackTrace}");
            // Continue without backpack data - don't break inventory persistence
        }
        
        // VEST SYSTEM: Set vest data (with error handling)
        try
        {
            if (inventoryManager.vestSlot != null)
            {
                inventoryManager.vestSlot.LoadFromSaveData(currentInventory.vestData);
                Debug.Log($"[PersistentItemInventoryManager] ✅ RESTORED vest to tier {currentInventory.vestData.vestTier} ({currentInventory.vestData.maxPlates} plates)");
            }
            else
            {
                Debug.LogWarning("[PersistentItemInventoryManager] VestSlot is null, cannot restore vest data");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PersistentItemInventoryManager] Error loading vest data: {ex.Message}");
            Debug.LogError($"[PersistentItemInventoryManager] Stack trace: {ex.StackTrace}");
            // Continue without vest data - don't break inventory persistence
        }
        
        // Apply items to slots
        try
        {
            Debug.Log($"[PersistentItemInventoryManager] About to get inventory slots from InventoryManager...");
            var slots = inventoryManager.GetAllInventorySlots();
        Debug.Log($"[PersistentItemInventoryManager] Found {slots.Count} total slots in InventoryManager");
        
        if (slots == null)
        {
            Debug.LogError("[PersistentItemInventoryManager] GetAllInventorySlots returned null!");
            isLoading = false;
            return;
        }
        
        if (slots.Count == 0)
        {
            Debug.LogError("[PersistentItemInventoryManager] GetAllInventorySlots returned empty list!");
            isLoading = false;
            return;
        }
        
        int itemIndex = 0;
        int slotsProcessed = 0;
        int itemsPlaced = 0;
        
        Debug.Log($"[PersistentItemInventoryManager] Starting slot processing loop with {currentInventory.items.Count} items to place...");
        
        foreach (var slot in slots)
        {
            slotsProcessed++;
            Debug.Log($"[PersistentItemInventoryManager] Processing slot {slotsProcessed}: {(slot != null ? slot.name : "NULL")}");
            
            if (slot != null && !slot.isGemSlot && itemIndex < currentInventory.items.Count)
            {
                var itemData = currentInventory.items[itemIndex];
                Debug.Log($"[PersistentItemInventoryManager] Processing item {itemIndex}: {itemData.itemName} x{itemData.itemCount} from path {itemData.itemPath}");
                
                // Load the item asset with fallback paths
                ChestItemData item = Resources.Load<ChestItemData>(itemData.itemPath);
                if (item != null)
                {
                    // Check if slot is already occupied
                    if (!slot.IsEmpty)
                    {
                        Debug.LogWarning($"[PersistentItemInventoryManager] Slot {slotsProcessed} already occupied with {slot.CurrentItem?.itemName}, overwriting!");
                    }
                    
                    slot.SetItem(item, itemData.itemCount);
                    itemsPlaced++;
                    Debug.Log($"[PersistentItemInventoryManager] Applied {itemData.itemName} x{itemData.itemCount} to slot {slotsProcessed}");
                }
                else
                {
                    // Primary path failed, try alternative paths
                    Debug.LogWarning($"[PersistentItemInventoryManager] Primary path '{itemData.itemPath}' failed, trying alternatives...");
                    
                    // KEYCARD FIX: Check if this might be a keycard
                    bool isKeycard = itemData.itemName.ToLower().Contains("keycard");
                    
                    string[] alternativePaths;
                    if (isKeycard)
                    {
                        // Try keycard-specific paths first
                        alternativePaths = new string[] {
                            $"Keycards/{itemData.itemName}",
                            $"Keycards/{itemData.itemName.Replace(" ", "")}",
                            $"Items/Keycards/{itemData.itemName}",
                            $"Items/Keycards/{itemData.itemName.Replace(" ", "")}",
                            $"Items/OLDITEMS/{itemData.itemName}",
                            $"Items/OLDITEMS/{itemData.itemName.Replace(" ", "")}",
                            $"Items/{itemData.itemName}",
                            $"Items/{itemData.itemName.Replace(" ", "")}",
                            itemData.itemName,
                            itemData.itemName.Replace(" ", "")
                        };
                    }
                    else
                    {
                        alternativePaths = new string[] {
                            $"Items/OLDITEMS/{itemData.itemName}",
                            $"Items/OLDITEMS/{itemData.itemName.Replace(" ", "")}",
                            $"Items/{itemData.itemName}",
                            $"Items/{itemData.itemName.Replace(" ", "")}",
                            itemData.itemName,
                            itemData.itemName.Replace(" ", "")
                        };
                    }
                    
                    bool foundAlternative = false;
                    foreach (string altPath in alternativePaths)
                    {
                        ChestItemData altItem = Resources.Load<ChestItemData>(altPath);
                        if (altItem != null)
                        {
                            slot.SetItem(altItem, itemData.itemCount);
                            itemsPlaced++;
                            Debug.Log($"[PersistentItemInventoryManager] Loaded {itemData.itemName} x{itemData.itemCount} using alternative path: '{altPath}'");
                            foundAlternative = true;
                            break;
                        }
                    }
                    
                    if (!foundAlternative)
                    {
                        if (isKeycard)
                        {
                            Debug.LogError($"[PersistentItemInventoryManager] ⚠️ KEYCARD '{itemData.itemName}' could not be loaded! Keycards must be in 'Assets/Resources/Keycards/' folder to persist.");
                        }
                        Debug.LogError($"[PersistentItemInventoryManager] Could not load item '{itemData.itemName}' from any path. Tried: {itemData.itemPath}, {string.Join(", ", alternativePaths)}");
                    }
                }
                
                itemIndex++;
            }
            else if (slot != null && slot.isGemSlot)
            {
                Debug.Log($"[PersistentItemInventoryManager] Skipping gem slot {slotsProcessed}");
            }
            else if (slot == null)
            {
                Debug.LogWarning($"[PersistentItemInventoryManager] Slot {slotsProcessed} is null!");
            }
        }
        
        Debug.Log($"[PersistentItemInventoryManager] SUMMARY: Processed {slotsProcessed} slots, placed {itemsPlaced} items out of {currentInventory.items.Count} available, restored {currentInventory.selfReviveCount} self revives");
        
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PersistentItemInventoryManager] Error during slot application: {ex.Message}");
            Debug.LogError($"[PersistentItemInventoryManager] Stack trace: {ex.StackTrace}");
        }
        finally
        {
            // CRITICAL: Always clear loading flag
            isLoading = false;
            Debug.Log($"[PersistentItemInventoryManager] 🔓 LOADING MODE DISABLED - Saves allowed again");
            Debug.Log($"[PersistentItemInventoryManager] *** LOAD OPERATION COMPLETE ***");
        }
    }
    
    /// <summary>
    /// Get the resource path for an item - ENHANCED with keycard support
    /// </summary>
    private string GetItemResourcePath(ChestItemData item)
    {
        if (item == null) return "";
        
        string itemName = item.name;
        
        // Remove common Unity suffixes like " (Clone)"
        if (itemName.Contains("("))
        {
            itemName = itemName.Substring(0, itemName.IndexOf("(")).Trim();
        }
        
        Debug.Log($"[PersistentItemInventoryManager] Getting resource path for item: '{item.name}' -> cleaned: '{itemName}', type: '{item.itemType}'");
        
        // SELF-REVIVE FIX: Check if this is a self-revive item
        if (item is SelfReviveItemData || (item.itemType != null && item.itemType.ToLower().Contains("revive")))
        {
            // Self-revive items need special handling - try revive-specific paths first
            string[] revivePaths = {
                $"Items/{itemName}",
                $"Items/{itemName.Replace(" ", "")}",
                $"Items/SelfRevive/{itemName}",
                $"Items/SelfRevive/{itemName.Replace(" ", "")}",
                $"SelfRevive/{itemName}",
                $"SelfRevive/{itemName.Replace(" ", "")}"
            };
            
            foreach (string path in revivePaths)
            {
                ChestItemData testLoad = Resources.Load<ChestItemData>(path);
                if (testLoad != null)
                {
                    Debug.Log($"[PersistentItemInventoryManager] Found self-revive item at path: '{path}'");
                    return path;
                }
            }
            
            Debug.LogWarning($"[PersistentItemInventoryManager] ⚠️ SELF-REVIVE '{item.name}' NOT FOUND in Resources folder! Self-revive items must be in a Resources folder to persist.");
        }
        
        // KEYCARD FIX: Check if this is a keycard item
        if (item.itemType != null && item.itemType.ToLower() == "keycard")
        {
            // Keycards need special handling - try keycard-specific paths first
            string[] keycardPaths = {
                $"Keycards/{itemName}",
                $"Keycards/{itemName.Replace(" ", "")}",
                $"Items/Keycards/{itemName}",
                $"Items/Keycards/{itemName.Replace(" ", "")}"
            };
            
            foreach (string path in keycardPaths)
            {
                ChestItemData testLoad = Resources.Load<ChestItemData>(path);
                if (testLoad != null)
                {
                    Debug.Log($"[PersistentItemInventoryManager] Found keycard at path: '{path}'");
                    return path;
                }
            }
            
            Debug.LogWarning($"[PersistentItemInventoryManager] ⚠️ KEYCARD '{item.name}' NOT FOUND in Resources folder! Keycards must be in a Resources folder to persist.");
            Debug.LogWarning($"[PersistentItemInventoryManager] Please move keycard assets from 'Assets/prefabs_made/KEYCARDS/Keycards/' to 'Assets/Resources/Keycards/'");
        }
        
        // Try common paths
        if (!string.IsNullOrEmpty(itemName))
        {
            string[] possiblePaths = {
                $"Items/{itemName}",
                $"Items/{itemName.Replace(" ", "")}",
                $"Items/OLDITEMS/{itemName}",
                $"Items/OLDITEMS/{itemName.Replace(" ", "")}"
            };
            
            foreach (string path in possiblePaths)
            {
                ChestItemData testLoad = Resources.Load<ChestItemData>(path);
                if (testLoad != null)
                {
                    Debug.Log($"[PersistentItemInventoryManager] Found item at path: '{path}'");
                    return path;
                }
            }
        }
        
        // Fallback for items without proper name field
        string fileName = item.itemName.Replace(" ", "");
        Debug.Log($"[PersistentItemInventoryManager] Using fallback path for '{item.itemName}': Items/{fileName}");
        return $"Items/{fileName}";
    }
    
    /// <summary>
    /// Get current inventory data (read-only)
    /// </summary>
    public PersistentInventoryData GetCurrentInventoryData()
    {
        return currentInventory;
    }
    
    /// <summary>
    /// Clear all inventory data
    /// </summary>
    public void ClearInventoryData()
    {
        currentInventory = new PersistentInventoryData();
        SaveInventoryData();
        Debug.Log("[PersistentItemInventoryManager] Cleared all inventory data");
    }
    
    /// <summary>
    /// DEBUG: Delete the corrupted save file to start fresh
    /// </summary>
    [ContextMenu("Delete Corrupted Save File")]
    public void DeleteCorruptedSaveFile()
    {
        try
        {
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
                Debug.Log($"[PersistentItemInventoryManager] ✅ DELETED corrupted save file: {savePath}");
                Debug.Log("[PersistentItemInventoryManager] Next game session will start with fresh inventory");
            }
            else
            {
                Debug.Log("[PersistentItemInventoryManager] No save file exists to delete");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PersistentItemInventoryManager] Failed to delete save file: {e.Message}");
        }
    }
    
    /// <summary>
    /// Set self revive count directly
    /// </summary>
    public void SetSelfReviveCount(int count)
    {
        currentInventory.selfReviveCount = Mathf.Max(0, count);
        Debug.Log($"[PersistentItemInventoryManager] Set self revive count to {currentInventory.selfReviveCount}");
    }
    
    /// <summary>
    /// Get current self revive count
    /// </summary>
    public int GetSelfReviveCount()
    {
        return currentInventory.selfReviveCount;
    }
    
    /// <summary>
    /// Add self revive count
    /// </summary>
    public void AddSelfRevive(int count = 1)
    {
        currentInventory.selfReviveCount += count;
        Debug.Log($"[PersistentItemInventoryManager] Added {count} self revive(s), total: {currentInventory.selfReviveCount}");
    }
    
    /// <summary>
    /// Remove self revive count
    /// </summary>
    public bool RemoveSelfRevive(int count = 1)
    {
        if (currentInventory.selfReviveCount >= count)
        {
            currentInventory.selfReviveCount -= count;
            Debug.Log($"[PersistentItemInventoryManager] Removed {count} self revive(s), remaining: {currentInventory.selfReviveCount}");
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Set backpack data directly
    /// </summary>
    public void SetBackpackData(BackpackSaveData backpackData)
    {
        currentInventory.backpackData = backpackData ?? new BackpackSaveData();
        Debug.Log($"[PersistentItemInventoryManager] Set backpack data to tier {currentInventory.backpackData.backpackTier}");
    }
    
    /// <summary>
    /// Get current backpack data
    /// </summary>
    public BackpackSaveData GetBackpackData()
    {
        return currentInventory.backpackData;
    }
    
    /// <summary>
    /// Reset backpack to Tier 1 (called on death)
    /// </summary>
    public void ResetBackpackToTier1()
    {
        currentInventory.backpackData = new BackpackSaveData();
        Debug.Log("[PersistentItemInventoryManager] Reset backpack to Tier 1 due to death");
    }
    
    /// <summary>
    /// VEST SYSTEM: Set vest data directly
    /// </summary>
    public void SetVestData(VestSaveData vestData)
    {
        currentInventory.vestData = vestData ?? new VestSaveData();
        Debug.Log($"[PersistentItemInventoryManager] Set vest data to tier {currentInventory.vestData.vestTier}");
    }
    
    /// <summary>
    /// VEST SYSTEM: Get current vest data
    /// </summary>
    public VestSaveData GetVestData()
    {
        return currentInventory.vestData;
    }
    
    /// <summary>
    /// VEST SYSTEM: Reset vest to T1 (called on death)
    /// </summary>
    public void ResetVestToT1()
    {
        currentInventory.vestData = new VestSaveData();
        Debug.Log("[PersistentItemInventoryManager] Reset vest to T1 due to death");
    }
    
    /// <summary>
    /// Debug method to show current persistent inventory contents
    /// </summary>
    [ContextMenu("Debug Current Inventory")]
    public void DebugCurrentInventory()
    {
        Debug.Log($"[PersistentItemInventoryManager] === CURRENT PERSISTENT INVENTORY ===");
        Debug.Log($"[PersistentItemInventoryManager] Gems: {currentInventory.gemCount}");
        Debug.Log($"[PersistentItemInventoryManager] Self Revives: {currentInventory.selfReviveCount}");
        Debug.Log($"[PersistentItemInventoryManager] Backpack: Tier {currentInventory.backpackData.backpackTier} ({currentInventory.backpackData.slotCount} slots)");
        Debug.Log($"[PersistentItemInventoryManager] Vest: Tier {currentInventory.vestData.vestTier} ({currentInventory.vestData.maxPlates} plates)");
        Debug.Log($"[PersistentItemInventoryManager] Items: {currentInventory.items.Count}");
        
        for (int i = 0; i < currentInventory.items.Count; i++)
        {
            var item = currentInventory.items[i];
            Debug.Log($"[PersistentItemInventoryManager] Item {i}: {item.itemName} x{item.itemCount} (path: {item.itemPath})");
        }
        
        if (currentInventory.items.Count == 0)
        {
            Debug.LogWarning("[PersistentItemInventoryManager] No items found in persistent inventory!");
        }
    }
    
    /// <summary>
    /// Test method to verify scrap metal can be loaded from resources
    /// </summary>
    [ContextMenu("Test Scrap Metal Loading")]
    public void TestScrapMetalLoading()
    {
        // Test both possible paths
        string[] testPaths = { "Items/ScrapMetal", "Items/Scrap Metal" };
        
        foreach (string path in testPaths)
        {
            ChestItemData scrapItem = Resources.Load<ChestItemData>(path);
            if (scrapItem != null)
            {
                Debug.Log($"[PersistentItemInventoryManager] ✅ Successfully loaded ScrapMetal from path: {path}");
                Debug.Log($"[PersistentItemInventoryManager] Item details: name='{scrapItem.name}', itemName='{scrapItem.itemName}', type='{scrapItem.itemType}'");
            }
            else
            {
                Debug.LogWarning($"[PersistentItemInventoryManager] ❌ Failed to load ScrapMetal from path: {path}");
            }
        }
        
        // Also test loading all items in Items folder
        ChestItemData[] allItems = Resources.LoadAll<ChestItemData>("Items");
        Debug.Log($"[PersistentItemInventoryManager] Found {allItems.Length} total items in Resources/Items/");
        
        foreach (var item in allItems)
        {
            Debug.Log($"[PersistentItemInventoryManager] Available item: '{item.name}' (itemName: '{item.itemName}')");
        }
    }
    
    /// <summary>
    /// Emergency save on application pause (mobile/focus loss)
    /// </summary>
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            Debug.Log("[PersistentItemInventoryManager] Application paused - emergency save");
            SaveInventoryData();
        }
    }
    
    /// <summary>
    /// Emergency save on application focus loss
    /// </summary>
    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            Debug.Log("[PersistentItemInventoryManager] Application lost focus - emergency save");
            SaveInventoryData();
        }
    }
    
    /// <summary>
    /// Emergency save on application quit
    /// </summary>
    void OnApplicationQuit()
    {
        Debug.Log("[PersistentItemInventoryManager] Application quitting - final save");
        SaveInventoryData();
    }
    
    /// <summary>
    /// Emergency save on destroy (scene transition safety)
    /// </summary>
    void OnDestroy()
    {
        if (Instance == this)
        {
            Debug.Log("[PersistentItemInventoryManager] Instance destroyed - final save");
            SaveInventoryData();
        }
    }
}
