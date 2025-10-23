using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ReviveSlotController : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler

{
    [Header("UI References")]
    public Image reviveIconImage;        // Assign in Inspector
    // [REMOVED] public TextMeshProUGUI reviveCountText;  // No text for revive slot
    public Sprite reviveSprite;          // Assign in Inspector
    
    [Header("Revive Item Settings")]
    public string[] reviveItemTags = { "Self-Revive", "self-revive", "Revive", "revive", "Revival", "revival", "ReviveItem" };
    
    private int reviveCount = 0;
    private InventoryManager inventoryManager;
    
    void Awake()
    {
        // CRITICAL: Ensure revive tags are correct (in case Inspector overrode them)
        reviveItemTags = new string[] { "Self-Revive", "self-revive", "Revive", "revive", "Revival", "revival", "ReviveItem" };
        
        // Use the InventoryManager singleton instance
        inventoryManager = InventoryManager.Instance;
        
        // Ensure we have references to UI elements
        if (reviveIconImage == null)
        {
            reviveIconImage = GetComponentInChildren<Image>();
            if (reviveIconImage == null)
            {
                Debug.LogWarning("Revive Icon Image is missing! Please assign it in the Inspector.");
            }
        }
        
        // [REMOVED] No reviveCountText for revive slot
        
        Debug.Log($"[REVIVE DEBUG] Initialized with tags: {string.Join(", ", reviveItemTags)}");
    }
    
    void Start()
    {
        // PERSISTENCE FIX: Don't force clear revive count - let InventoryManager restore saved state
        // reviveCount will be set by InventoryManager.LoadInventoryData() after this Start() method
        
        // Apply revive sprite
        if (reviveIconImage != null && reviveSprite != null)
        {
            reviveIconImage.sprite = reviveSprite;
        }
        
        // Initialize UI to show current state (will be updated when InventoryManager loads data)
        UpdateReviveDisplay();
        
        Debug.Log($"ReviveSlotController initialized - reviveCount: {reviveCount} (will be updated by persistence system)");
    }
    
    /// <summary>
    /// Update the revive slot UI display
    /// </summary>
    private void UpdateReviveDisplay() 
    {
        Debug.Log($"ReviveSlotController.UpdateReviveDisplay called with reviveCount={reviveCount}");
        
        // Only show revive icon when we actually have revives
        bool hasRevives = (reviveCount > 0);
        
        // Update image - ONLY VISIBLE WHEN HAS REVIVES
        if (reviveIconImage != null) 
        {
            if (hasRevives)
            {
                // Show the revive icon when we have revives
                reviveIconImage.enabled = true;
                reviveIconImage.gameObject.SetActive(true);
                reviveIconImage.color = Color.white; // Full brightness
                Debug.Log($"Revive icon SHOWN - has {reviveCount} revives");
            }
            else
            {
                // Hide the revive icon when slot is empty
                reviveIconImage.enabled = false;
                reviveIconImage.gameObject.SetActive(false);
                Debug.Log($"Revive icon HIDDEN - slot is empty");
            }
        }
        
        // [REMOVED] No reviveCountText for revive slot
    }
    
    /// <summary>
    /// Set the revive count directly
    /// </summary>
    public void SetReviveCount(int count) 
    {
        if (count < 0) count = 0;
        
        reviveCount = count;
        UpdateReviveDisplay();
        
        // Sync with persistent inventory system
        SyncWithPersistentInventory();
        
        Debug.Log($"Revive count set to: {reviveCount}");
    }
    
    /// <summary>
    /// Property to check if the revive slot is empty
    /// </summary>
    public bool IsEmpty => reviveCount <= 0;

    /// <summary>
    /// SetItem method for compatibility with InventorySlot logic
    /// </summary>
    public void SetItem(object item, int count = 1)
    {
        // Only allow setting if empty
        if (IsEmpty)
        {
            reviveCount = 1;
            UpdateReviveDisplay();
        }
    }

    /// <summary>
    /// Add revive items - ONLY ONE REVIVE ALLOWED AT A TIME
    /// </summary>
    public bool AddRevives(int amount) 
    {
        if (amount <= 0) return false;
        
        // CRITICAL: Only allow one revive at a time
        if (reviveCount >= 1)
        {
            Debug.Log($"Cannot add revive - slot already full (has {reviveCount})");
            return false; // Slot is full
        }
        
        // Set to 1 (only one revive allowed)
        reviveCount = 1;
        UpdateReviveDisplay();
        
        // Sync with persistent inventory system
        SyncWithPersistentInventory();
        
        // Play UI click sound on equip
        PlayUIClick();
        Debug.Log($"[REVIVE] UI_click sound played on equip.");
        Debug.Log($"Added revive to slot. Total: {reviveCount}");
        return true; // Successfully added
    }
    
    /// <summary>
    /// Remove revive items if we have enough
    /// </summary>
    public bool RemoveRevives(int amount)
    {
        if (amount <= 0) return false;
        
        if (reviveCount >= amount)
        {
            reviveCount -= amount;
            UpdateReviveDisplay();
            
            // Sync with persistent inventory system
            SyncWithPersistentInventory();
            
            // Play UI click sound on unequip
            PlayUIClick();
            Debug.Log($"[REVIVE] UI_click sound played on unequip.");
            Debug.Log($"Removed {amount} revives. Remaining: {reviveCount}");
            return true;
        }
        
        Debug.Log($"Cannot remove {amount} revives - only have {reviveCount}");
        return false;
    }
    
    /// <summary>
    /// Check if we have any revives available
    /// </summary>
    public bool HasRevives()
    {
        return reviveCount > 0;
    }
    
    /// <summary>
    /// Check if the revive slot is full (can only hold 1 revive)
    /// </summary>
    public bool IsFull()
    {
        return reviveCount >= 1;
    }
    
    /// <summary>
    /// Get current revive count
    /// </summary>
    public int GetReviveCount()
    {
        return reviveCount;
    }
    
    /// <summary>
    /// Check if an item is a revive item - needed by InventoryManager
    /// </summary>
    public bool IsReviveItem(ChestItemData item)
    {
        if (item == null) 
        {
            Debug.Log("[REVIVE DEBUG] Item is NULL - not a revive item");
            return false;
        }
        
        Debug.Log($"[REVIVE DEBUG] Checking item: Name='{item.itemName}', Type='{item.itemType}'");
        Debug.Log($"[REVIVE DEBUG] Available tags: {string.Join(", ", reviveItemTags)}");
        
        // Check if the item has a "revive" tag or name contains "revive"
        foreach (string tag in reviveItemTags)
        {
            bool nameMatch = item.itemName.ToLower().Contains(tag.ToLower());
            bool typeMatch = item.itemType.ToLower().Contains(tag.ToLower());
            
            Debug.Log($"[REVIVE DEBUG] Tag '{tag}' - Name match: {nameMatch}, Type match: {typeMatch}");
            
            if (nameMatch || typeMatch)
            {
                Debug.Log($"[REVIVE DEBUG] MATCH FOUND! Item '{item.itemName}' is a revive item");
                return true;
            }
        }
        
        Debug.Log($"[REVIVE DEBUG] NO MATCH - Item '{item.itemName}' is NOT a revive item");
        return false;
    }

    /// <summary>
    /// Sync revive count with persistent inventory system
    /// </summary>
    private void SyncWithPersistentInventory()
    {
        if (PersistentItemInventoryManager.Instance != null)
        {
            PersistentItemInventoryManager.Instance.SetSelfReviveCount(reviveCount);
            Debug.Log($"[ReviveSlotController] Synced revive count ({reviveCount}) with persistent inventory");
        }
        else
        {
            Debug.LogWarning("[ReviveSlotController] PersistentItemInventoryManager.Instance is null - cannot sync revive count");
        }
    }
    
    /// <summary>
    /// Play UI click sound for equip/unequip
    /// </summary>
    private void PlayUIClick()
    {
        // No sound needed for revive slot.
    }
    
    /// <summary>
    /// Handle double-click on revive slot to unequip back to inventory
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // Only handle left double-click when slot has a revive
        if (eventData.button != PointerEventData.InputButton.Left || !HasRevives()) return;
        
        // Check for double-click
        if (eventData.clickCount == 2)
        {
            // Try to move revive back to inventory
            if (inventoryManager != null)
            {
                // Find first empty inventory slot
                var emptySlot = inventoryManager.GetFirstEmptySlot();
                if (emptySlot != null)
                {
                    // Create a self-revive item instance to place in inventory
                    // We need to find the self-revive item asset
                    SelfReviveItemData reviveItemAsset = FindSelfReviveItemAsset();
                    if (reviveItemAsset != null)
                    {
                        // Place in inventory slot
                        emptySlot.SetItem(reviveItemAsset, 1);
                        
                        // Remove from revive slot
                        RemoveRevives(1);
                        
                        // Save the change
                        inventoryManager.SaveInventoryData();
                        
                        Debug.Log($"[ReviveSlotController] Unequipped self-revive to inventory via double-click");
                    }
                    else
                    {
                        Debug.LogError("[ReviveSlotController] Could not find SelfReviveItemData asset to unequip!");
                    }
                }
                else
                {
                    Debug.Log("[ReviveSlotController] Cannot unequip self-revive - inventory is full");
                }
            }
        }
    }
    
    /// <summary>
    /// Find the SelfReviveItemData asset in Resources
    /// </summary>
    private SelfReviveItemData FindSelfReviveItemAsset()
    {
        // Try common paths for self-revive items
        string[] possiblePaths = {
            "Items/Self-Revive",
            "Items/SelfRevive",
            "Items/Self Revive",
            "SelfRevive/Self-Revive",
            "SelfRevive/SelfRevive"
        };
        
        foreach (string path in possiblePaths)
        {
            SelfReviveItemData asset = Resources.Load<SelfReviveItemData>(path);
            if (asset != null)
            {
                Debug.Log($"[ReviveSlotController] Found self-revive asset at: {path}");
                return asset;
            }
        }
        
        // Fallback: try to find any SelfReviveItemData in Resources
        SelfReviveItemData[] allReviveItems = Resources.LoadAll<SelfReviveItemData>("");
        if (allReviveItems.Length > 0)
        {
            Debug.Log($"[ReviveSlotController] Found self-revive asset via LoadAll: {allReviveItems[0].name}");
            return allReviveItems[0];
        }
        
        return null;
    }
    
    /// <summary>
    /// Handle pointer entering the revive slot (add visual feedback)
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Add visual feedback if needed (optional, can be left empty)
    }

    /// <summary>
    /// Handle item dropped on the revive slot (required by IDropHandler)
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        // Get the dragged item from UnifiedSlot drag system
        GameObject draggedObject = eventData.pointerDrag;
        if (draggedObject == null) return;
        
        UnifiedSlot draggedSlot = draggedObject.GetComponent<UnifiedSlot>();
        if (draggedSlot == null || draggedSlot.IsEmpty) return;
        
        ChestItemData draggedItem = draggedSlot.CurrentItem;
        
        // Check if the dragged item is a self-revive item
        if (!IsReviveItem(draggedItem))
        {
            Debug.Log($"[ReviveSlotController] ❌ Cannot drop {draggedItem.itemName} - not a revive item");
            return;
        }
        
        // Check if revive slot is already full
        if (IsFull())
        {
            Debug.Log($"[ReviveSlotController] ❌ Cannot drop revive - slot already has a self-revive equipped!");
            return;
        }
        
        Debug.Log($"[ReviveSlotController] 🔄 Attempting to equip {draggedItem.itemName} via drag-drop...");
        
        // Add the revive to this slot
        if (AddRevives(1))
        {
            // Remove the item from the source slot
            draggedSlot.ClearSlot();
            Debug.Log($"[ReviveSlotController] Successfully equipped {draggedItem.itemName} to revive slot");
            
            // Save the change
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.SaveInventoryData();
            }
        }
    }

    /// <summary>
    /// Handle pointer exiting the revive slot (remove visual feedback)
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        // Remove visual feedback
        if (reviveIconImage != null)
        {
            // Restore normal color based on revive count
            bool hasRevives = (reviveCount > 0);
            reviveIconImage.color = hasRevives ? Color.white : new Color(1f, 1f, 1f, 0.3f);
        }
    }
}
