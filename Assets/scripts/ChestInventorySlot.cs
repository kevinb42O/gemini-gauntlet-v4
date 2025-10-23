using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ChestInventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("UI References")]
    public Image itemIcon;
    public Image slotBackground;
    public TextMeshProUGUI quantityText;
    
    // NEW: A simple GameObject to enable/disable for a hover effect.
    // Create a simple UI Image as a child of your slot prefab and assign it here.
    [Tooltip("A UI element to show when the slot is hovered over.")]
    public GameObject hoverHighlight;

    [HideInInspector]
    public ChestItemData currentItem;
    [HideInInspector]
    public int itemCount = 0;
    [HideInInspector]
    public ChestInteractionSystem chestSystem;

    // NEW: Double-click detection variables
    private float lastClickTime = 0f;
    private const float DOUBLE_CLICK_TIME = 0.3f; // Time window for a double-click

    private GameObject dragVisual; // For drag and drop
    private bool isDragging = false;
    // Removed DragAndDropItem reference - using new UnifiedSlot system

    void Awake()
    {
        // Initialize hover highlight system
        if (hoverHighlight != null)
        {
            hoverHighlight.SetActive(false); // Always start with highlight off
        }
        else
        {
            // Try to find a child object with "highlight" in the name as fallback
            Transform highlightChild = transform.Find("highlight");
            if (highlightChild == null) highlightChild = transform.Find("Highlight");
            
            if (highlightChild != null)
            {
                hoverHighlight = highlightChild.gameObject;
                Debug.Log($"Auto-assigned existing highlight object to {gameObject.name}");
            }
            else
            {
                // No highlight found - create one automatically
                GameObject newHighlight = new GameObject("Highlight");
                newHighlight.transform.SetParent(transform);
                newHighlight.transform.localPosition = Vector3.zero;
                
                // Add a UI Image component
                Image highlightImage = newHighlight.AddComponent<Image>();
                highlightImage.color = new Color(1f, 1f, 0.5f, 0.3f); // Semi-transparent yellow
                
                // Match size with this slot
                RectTransform rt = newHighlight.GetComponent<RectTransform>();
                RectTransform myRT = GetComponent<RectTransform>();
                if (rt != null && myRT != null)
                {
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }
                
                // Assign it as our highlight
                hoverHighlight = newHighlight;
                Debug.Log($"Created new highlight object for {gameObject.name}");
            }
            
            // Make sure it starts inactive
            if (hoverHighlight != null)
            {
                hoverHighlight.SetActive(false);
            }
        }
    }

    public void SetItem(ChestItemData item)
    {
        currentItem = item;
        if (item != null)
        {
            Debug.Log($"Setting item {item.itemName} in slot {gameObject.name}");
            
            if (itemIcon == null)
            {
                Debug.LogError($"itemIcon is NULL on slot {gameObject.name}! Assign Image component in Inspector");
                // Try to find the Image component
                itemIcon = GetComponentInChildren<UnityEngine.UI.Image>();
                if (itemIcon == null)
                {
                    Debug.LogError($"CRITICAL: Could not find any Image component on {gameObject.name} or its children!");
                    return;
                }
                Debug.Log($"Auto-assigned Image component from {itemIcon.gameObject.name}");
            }
            
            if (item.itemIcon == null)
            {
                Debug.LogError($"Item {item.itemName} has NULL sprite! Fix in ScriptableObject");
                // Use a fallback sprite to at least show something
                itemIcon.color = Color.magenta; // Visual indicator that sprite is missing
            }
            else
            {
                itemIcon.sprite = item.itemIcon;
                itemIcon.color = Color.white; // Reset color
                Debug.Log($"Set sprite {item.itemIcon.name} on slot {gameObject.name}");
            }
            
            // CRITICAL FIX: Force icon to be visible
            itemIcon.enabled = true;
            
            // Force layout refresh
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
            // CRITICAL FIX: Ensure we have a valid item count (at least 1)
            if (itemCount <= 0)
            {
                Debug.LogWarning($"Item {item.itemName} has invalid count {itemCount} - forcing to 1");
                itemCount = 1; // Default to 1 item if count is invalid
            }
            
            // Drag and drop is now handled by UnifiedSlot system - no separate component needed
            Debug.Log($"ChestInventorySlot: Set item {item.itemName} with count {itemCount}");
        }
        else
        {
            ClearSlot();
        }
    }

    public void ClearSlot()
    {
        currentItem = null;
        itemIcon.sprite = null;
        itemIcon.enabled = false;
        itemCount = 0;
        UpdateQuantityDisplay();
        
        // Drag and drop is now handled by UnifiedSlot system - no separate component needed
        Debug.Log($"ChestInventorySlot: Cleared slot");
    }

    // Helper method to show highlight overlay
    public void ShowHighlight()
    {
        if (hoverHighlight != null)
        {
            // Ensure the highlight is positioned correctly
            hoverHighlight.transform.position = transform.position;
            hoverHighlight.SetActive(true);
        }
        else
        {
            Debug.LogError($"No highlight object assigned to slot {gameObject.name}!");
        }
    }
    
    // Helper method to hide highlight overlay
    public void HideHighlight()
    {
        if (hoverHighlight != null)
        {
            hoverHighlight.SetActive(false);
        }
    }

    // Reliable hover detection system
    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log($"Mouse entered slot {gameObject.name}");
        
        // Show highlight using our helper method
        ShowHighlight();
        
        // Only show tooltip if we have an item
        if (currentItem != null && chestSystem != null)
        {
            chestSystem.ShowTooltip(currentItem.itemName, currentItem.description);
        }
    }

    // Reliable hover exit detection
    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log($"Mouse exited slot {gameObject.name}");
        
        // Hide highlight using our helper method
        HideHighlight();
        
        // Always hide the tooltip when mouse exits
        if (chestSystem != null)
        {
            chestSystem.HideTooltip();
        }
    }

    // NEW: This method handles all clicks and detects double-clicks.
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if ((Time.time - lastClickTime) < DOUBLE_CLICK_TIME)
            {
                // Double-click detected!
                HandleDoubleClick();
            }
            lastClickTime = Time.time;
        }
    }

    // 🔥 FIXED: Proper double-click handling with stacking support
    private void HandleDoubleClick()
    {
        if (currentItem != null && chestSystem != null)
        {
            ChestItemData itemToCollect = currentItem;
            int countToCollect = itemCount;
            
            Debug.Log($"🎯 Double-clicked on {itemToCollect.itemName} (count: {countToCollect})");
            
            // 🔥 CRITICAL FIX: DO NOT clear slot before validation
            // Keep item in slot until we confirm successful transfer
            
            bool collected = chestSystem.CollectItemToPlayerInventory(itemToCollect, this);
            
            if (collected)
            {
                Debug.Log($"✅ Successfully collected {itemToCollect.itemName} (count: {countToCollect})");
                
                // 🔥 ENHANCED STACKING FIX: Only clear slot after successful transfer
                // The CollectItemToPlayerInventory method should handle slot clearing
                // But let's ensure it's cleared properly here as backup
                if (currentItem == itemToCollect && itemCount == countToCollect)
                {
                    Debug.Log($"🧹 Backup slot clearing after successful collection");
                    ClearSlot();
                }
            }
            else
            {
                Debug.LogWarning($"❌ Failed to collect {itemToCollect.itemName} - item remains in chest slot");
                // Item stays in slot since transfer failed - no restoration needed
            }
        }
        else
        {
            if (currentItem == null)
                Debug.LogWarning("⚠️ Double-click on empty slot - nothing to collect");
            if (chestSystem == null)
                Debug.LogError("❌ Double-click failed - chestSystem is null! Check slot initialization.");
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentItem == null)
            return;
            
        isDragging = true;
        
        // Create a visual representation of the dragged item
        dragVisual = new GameObject("DraggedItem");
        Image dragImage = dragVisual.AddComponent<Image>();
        dragImage.sprite = itemIcon.sprite;
        dragImage.raycastTarget = false; // Don't block raycasts during drag
        
        // Match the size of the original icon
        RectTransform dragRect = dragVisual.GetComponent<RectTransform>();
        RectTransform originalRect = itemIcon.GetComponent<RectTransform>();
        dragRect.sizeDelta = originalRect.sizeDelta * 1.2f; // Slightly larger than original
        
        // Start dragging in the chest system
        chestSystem.StartDragging(gameObject, dragVisual);
        
        // Play sound
        // Sound removed per user request
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;
            
        // Update position via chest system
        chestSystem.UpdateDragPosition(eventData.position);
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;
            
        isDragging = false;
        
        // Check if we're dropping onto a valid slot
        // This will be handled by the OnDrop method of the target slot
        
        // If we didn't drop on a valid target, return item to original position
        if (eventData.pointerCurrentRaycast.gameObject == null || 
            !eventData.pointerCurrentRaycast.gameObject.GetComponent<ChestInventorySlot>())
        {
            chestSystem.CancelDrag();
            
            // Clean up drag visual if it exists
            if (dragVisual != null)
            {
                Destroy(dragVisual);
                dragVisual = null;
            }
        }
    }
    
    public void OnDrop(PointerEventData eventData)
    {
        // Something was dropped on this slot
        chestSystem.DropItem(gameObject);
        
        // Clean up the drag visual
        if (dragVisual != null)
        {
            Destroy(dragVisual);
            dragVisual = null;
        }
    }
    
    // === ADDITIONAL METHODS FOR DRAG-AND-DROP COMPATIBILITY ===
    
    /// <summary>
    /// Set item with count
    /// </summary>
    public void SetItem(ChestItemData item, int count)
    {
        itemCount = count;
        SetItem(item);
        
        // Update quantity display
        UpdateQuantityDisplay();
        
        // Drag and drop is now handled by UnifiedSlot system - no separate component needed
        Debug.Log($"ChestInventorySlot: Updated item {item?.itemName} with count {count}");
    }
    
    /// <summary>
    /// Add items to this slot (for stacking)
    /// </summary>
    public bool AddItem(ChestItemData item, int count)
    {
        if (currentItem == null)
        {
            SetItem(item, count);
            return true;
        }
        else if (currentItem == item)
        {
            itemCount += count;
            UpdateQuantityDisplay();
            return true;
        }
        
        return false; // Can't add different item
    }
    
    /// <summary>
    /// Update quantity text display
    /// </summary>
    public void UpdateQuantityDisplay()
    {
        if (quantityText != null)
        {
            if (currentItem != null && itemCount > 1)
            {
                quantityText.text = itemCount.ToString();
                quantityText.gameObject.SetActive(true);
            }
            else
            {
                quantityText.gameObject.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// Check if slot is empty
    /// </summary>
    public bool IsEmpty()
    {
        return currentItem == null || itemCount <= 0;
    }
    
    /// <summary>
    /// Check if slot can accept item
    /// </summary>
    public bool CanAcceptItem(ChestItemData item)
    {
        return IsEmpty() || currentItem == item;
    }
    
    /// <summary>
    /// Remove items from this slot
    /// </summary>
    public bool RemoveItem(int count)
    {
        // ENHANCED SAFETY CHECKS
        // Case 1: If no count to remove, consider it a success (nothing to do)
        if (count <= 0) 
        {
            Debug.Log($"RemoveItem called with count {count} - nothing to remove");
            return true;
        }
        
        // Case 2: If item is null or count too low, fail gracefully
        if (currentItem == null || itemCount < count) 
        {
            Debug.LogWarning($"Cannot remove {count} items from slot - current item: {(currentItem != null ? currentItem.itemName : "NULL")}, count: {itemCount}");
            // For double-click handling to work properly with UI refreshing,
            // we need to return true even if the item was already removed
            // This prevents errors when double-clicking multiple times rapidly
            return currentItem == null; // Return true if item is already gone (already removed)
        }
        
        string itemName = currentItem.itemName;
        ChestItemData itemBeingRemoved = currentItem; // Store reference for later
        itemCount -= count;
        
        Debug.Log($"CHEST SLOT: Removed {count}x {itemName}, {itemCount} remaining");
        
        if (itemCount <= 0)
        {
            Debug.Log("CHEST SLOT: Clearing slot completely");
            
            // Clear the slot
            ClearSlot();
        }
        else
        {
            UpdateQuantityDisplay();
            
            // Drag and drop is now handled by UnifiedSlot system - no separate component needed
            Debug.Log($"CHEST SLOT: Updated item count to {itemCount}");
        }
        
        // Make sure icon is properly toggled (critical fix for visibility issues)
        if (itemIcon != null)
        {
            itemIcon.gameObject.SetActive(currentItem != null);
        }
        
        return true;
    }
}
