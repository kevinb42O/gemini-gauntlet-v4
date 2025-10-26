using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using TMPro;

/// <summary>
/// Unified slot controller for both stash and inventory slots.
/// Handles drag & drop, double-click transfers, and item swapping.
/// Clean, simple implementation - no legacy code.
/// </summary>
public class UnifiedSlot : MonoBehaviour, IPointerClickHandler, IDragHandler, IBeginDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    public Image itemIcon;
    public TextMeshProUGUI countText;
    public Image slotBackground;
    
    [Header("Configuration")]
    [Tooltip("Is this a stash slot? (false = inventory slot)")]
    public bool isStashSlot = true;
    [Tooltip("Is this a gem-only slot? (only accepts gem items)")]
    public bool isGemSlot = false;
    [Tooltip("Gem sprite to show when gem slot is empty (count = 0)")]
    public Sprite defaultGemSprite;
    
    [Header("FORGE System")]
    [Tooltip("Is this a FORGE input slot? (accepts items for crafting)")]
    public bool isForgeInputSlot = false;
    [Tooltip("Is this a FORGE output slot? (shows crafted items, cannot accept drops)")]
    public bool isForgeOutputSlot = false;
    [Tooltip("Is this FORGE slot in game context? (true = in-game FORGE, false = menu FORGE)")]
    public bool isInGameContext = false;
    
    [Header("Highlight System")]
    [Tooltip("Shared highlight prefab that moves between slots (assign same prefab to all slots)")]
    public GameObject hoverHighlight;
    [Tooltip("Use shared highlight system (one highlight moves between slots)")]
    public bool useSharedHighlight = true;
    
    [Header("Tooltip System")]
    [Tooltip("Tooltip prefab to show item information on hover")]
    public GameObject tooltipPrefab;
    [Tooltip("Enable tooltip display on hover (uses dragImage canvas automatically)")]
    public bool enableTooltip = true;
    
    [Header("Equipment Slot Configuration")]
    [Tooltip("Is this an equipment slot (vest/backpack)? If true, prevents dragging items OUT")]
    public bool isEquipmentSlot = false;
    
    [Tooltip("Is this a weapon equipment slot (right/left hand weapon)? If true, allows dragging OUT but triggers equipment events")]
    public bool isWeaponSlot = false;
    
    // Properties
    public ChestItemData CurrentItem { get; private set; }
    public int ItemCount { get; private set; }
    public bool IsEmpty => CurrentItem == null;
    public bool IsOccupied => CurrentItem != null;
    
    // Events for StashManager to handle
    public event System.Action<UnifiedSlot> OnDoubleClick;
    public event System.Action<UnifiedSlot, UnifiedSlot> OnItemDropped;
    public event System.Action<UnifiedSlot> OnRightClick; // New event for right-click delete
    
    // Event for equipment slot changes (used by WeaponEquipmentManager)
    public event System.Action<ChestItemData, int> OnSlotChanged;
    
    // Drag & Drop state
    private static UnifiedSlot _draggedSlot;
    private static GameObject _draggedVisual;
    private float _lastClickTime;
    private const float DOUBLE_CLICK_TIME = 0.3f;
    
    // CRITICAL: Prevent rapid double-click spam causing duplication
    private static bool _isProcessingTransfer = false;
    private static float _lastTransferTime = 0f;
    private const float TRANSFER_COOLDOWN = 0.1f; // 100ms cooldown between transfers
    
    // Shared highlight system
    private static UnifiedSlot _currentlyHighlighted;
    private static GameObject _sharedHighlightInstance;
    
    // Shared tooltip system - static so only one tooltip shows at a time
    private static GameObject _tooltipInstance;
    private static UnifiedSlot _currentTooltipSlot;
    
    // References
    private Canvas _canvas;
    private StashManager _stashManager;
    
    /// <summary>
    /// Check if an item is a goods item that can be opened
    /// </summary>
    private bool IsGoodsItem(ChestItemData item)
    {
        if (item == null) return false;
        
        // Check if it's a GoodsItem
        if (item is GoodsItem) return true;
        
        // Check legacy goods items by name or type
        return item.itemName.ToLower().Contains("goods") || item.itemType.ToLower().Contains("goods");
    }
    
    // Legacy compatibility properties and fields
    public int slotIndex = -1; // Legacy slot index for old systems
    public ChestItemData currentItem => CurrentItem; // Legacy property name
    public int currentItemCount 
    { 
        get => ItemCount; 
        set 
        {
            if (CurrentItem != null)
            {
                SetItem(CurrentItem, value);
            }
        }
    }
    
    // Chest-specific functionality (for chest slots)
    [Header("Chest Slot Configuration (Optional)")]
    [Tooltip("Reference to ChestInteractionSystem if this is a chest slot")]
    public ChestInteractionSystem chestSystem;
    [Tooltip("Quantity text component for chest slots")]
    public TextMeshProUGUI quantityText;
    
    // Chest compatibility properties
    public int itemCount 
    {
        get => ItemCount;
        set => currentItemCount = value;
    }
    
    void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        _stashManager = FindFirstObjectByType<StashManager>();
        
        // Debug log for stash manager reference
        if (_stashManager != null)
        {
            Debug.Log($"[UnifiedSlot] {gameObject.name} found StashManager: {_stashManager.gameObject.name}");
        }
        else
        {
            Debug.Log($"[UnifiedSlot] {gameObject.name} - No StashManager found (this is normal for non-stash scenes)");
        }
    }
    
    void Start()
    {
        _canvas = GetComponentInParent<Canvas>();
        
        // CRITICAL FIX: Ensure child UI elements don't block raycasts to this slot
        EnsureChildElementsDontBlockRaycasts();
        
        // Initialize visual state
        UpdateVisuals();
    }
    
    /// <summary>
    /// Ensure child UI elements (itemIcon, countText) don't block raycasts to the slot
    /// This fixes the issue where you need to click "underneath" the gem slot
    /// </summary>
    private void EnsureChildElementsDontBlockRaycasts()
    {
        // Disable raycast target on item icon so clicks pass through to the slot
        if (itemIcon != null)
        {
            itemIcon.raycastTarget = false;
            Debug.Log($"[UnifiedSlot] Disabled raycastTarget on itemIcon for {gameObject.name}");
        }
        
        // Disable raycast target on count text so clicks pass through to the slot
        if (countText != null)
        {
            countText.raycastTarget = false;
            Debug.Log($"[UnifiedSlot] Disabled raycastTarget on countText for {gameObject.name}");
        }
        
        // Disable raycast target on quantity text (for chest compatibility)
        if (quantityText != null)
        {
            quantityText.raycastTarget = false;
            Debug.Log($"[UnifiedSlot] Disabled raycastTarget on quantityText for {gameObject.name}");
        }
        
        // Ensure the slot background (if present) allows raycasts
        if (slotBackground != null)
        {
            slotBackground.raycastTarget = true; // This should receive clicks
            Debug.Log($"[UnifiedSlot] Enabled raycastTarget on slotBackground for {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Set item in this slot
    /// </summary>
    /// <param name="item">Item to set</param>
    /// <param name="count">Item count</param>
    /// <param name="bypassValidation">If true, skips CanAcceptItem validation (for programmatic equipment slot sets)</param>
    public void SetItem(ChestItemData item, int count = 1, bool bypassValidation = false)
    {
        Debug.Log($" SetItem called on slot {gameObject.name} - item: {item?.itemName}, count: {count}, isGemSlot: {isGemSlot}, bypassValidation: {bypassValidation}");
        Debug.Log($" Item type: {item?.itemType}, IsGem: {(item != null ? IsGemItem(item) : false)}");
        Debug.Log($" Item has icon: {item?.itemIcon != null}");
        
        // VALIDATION: Check if this slot can accept the item (EXCEPT for forge output slots and when bypassing validation)
        if (item != null && !isForgeOutputSlot && !bypassValidation && !CanAcceptItem(item))
        {
            Debug.LogError($"❌ REJECTED: Cannot place {item.itemName} ({item.itemType}) in {(isGemSlot ? "gem slot" : "regular slot")} {gameObject.name}");
            Debug.LogError($"   Gem slot: {isGemSlot}, Is gem item: {IsGemItem(item)}");
            return; // Reject the item placement
        }
        
        CurrentItem = item;
        ItemCount = count;
        UpdateVisuals();
        
        // Trigger OnSlotChanged event for equipment tracking
        OnSlotChanged?.Invoke(item, count);
        
        if (item != null)
        {
            Debug.Log($"✅ Successfully placed {item.itemName} in {(isGemSlot ? "gem slot" : isForgeOutputSlot ? "forge output slot" : "regular slot")} {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Check if this slot can accept the given item
    /// </summary>
    public bool CanAcceptItem(ChestItemData item)
    {
        if (item == null) return false;
        
        // FORGE OUTPUT SLOTS: Never accept items (only show crafted results)
        if (isForgeOutputSlot)
        {
            return false;
        }
        
        // FORGE INPUT SLOTS: Accept any item for crafting
        if (isForgeInputSlot)
        {
            return true;
        }
        
        // CRITICAL FIX: Check if this is a purchasable stash slot that hasn't been purchased yet
        if (isStashSlot)
        {
            // Ensure we have a StashManager reference (fallback if not found in Awake)
            if (_stashManager == null)
            {
                _stashManager = FindFirstObjectByType<StashManager>();
            }
            
            if (_stashManager != null)
            {
                // If this is a purchasable slot, check if it's been purchased
                if (_stashManager.IsPurchasableSlot(this) && !_stashManager.IsSlotPurchased(this))
                {
                    Debug.Log($"[UnifiedSlot] ❌ REJECTED: Cannot place {item.itemName} in UNPURCHASED stash slot {gameObject.name}");
                    return false; // Reject items in unpurchased slots
                }
            }
            else
            {
                Debug.LogWarning($"[UnifiedSlot] No StashManager found for stash slot validation on {gameObject.name}");
            }
        }
        
        // If this is a gem-only slot, only accept gems
        if (isGemSlot)
        {
            return IsGemItem(item);
        }
        
        // If this is a regular slot, don't accept gems (they go to gem slots only)
        if (IsGemItem(item))
        {
            return false;
        }
        
        // CRITICAL FIX: Regular inventory slots should NOT accept vests or backpacks
        // Vests and backpacks can ONLY go in their dedicated equipment slots
        // SAME FOR WEAPONS: Weapons can ONLY go in weapon equipment slots
        // EXCEPTIONS:
        // 1. Chest slots (which have chestSystem assigned) CAN hold vests/backpacks/weapons for looting
        // 2. Stash slots CAN hold vests/backpacks/weapons for storage
        // 3. Equipment slots (isEquipmentSlot=true) for vests/backpacks
        // 4. Weapon slots (isWeaponSlot=true) for weapons
        bool isThisAnEquipmentSlot = isEquipmentSlot || isWeaponSlot;
        bool isChestSlot = (chestSystem != null);
        
        // Block vests/backpacks from non-equipment slots
        if (!isStashSlot && !isForgeInputSlot && !isChestSlot && !isEquipmentSlot && (IsVestItem(item) || IsBackpackItem(item)))
        {
            Debug.Log($"[UnifiedSlot] ❌ REJECTED: Cannot place {item.itemName} ({item.itemType}) in regular inventory slot - must use equipment slot");
            return false;
        }
        
        // ✅ FIX: Allow weapons in regular inventory slots AND stash slots (for unequipping)
        // Weapons CAN be stored in inventory/stash when unequipped
        // Only block them from gem slots, forge slots (non-input), and equipment slots (vest/backpack)
        if (IsWeaponItem(item))
        {
            // Weapons can go in: weapon slots, regular inventory, stash, chest slots, forge input slots
            bool canAcceptWeapon = isWeaponSlot || (!isGemSlot && !isForgeOutputSlot && !isEquipmentSlot);
            if (!canAcceptWeapon)
            {
                Debug.Log($"[UnifiedSlot] ❌ REJECTED: Cannot place {item.itemName} ({item.itemType}) in {(isGemSlot ? "gem slot" : isEquipmentSlot ? "equipment slot" : "this slot")}");
                return false;
            }
        }
        
        return true; // Regular item in regular slot
    }
    
    /// <summary>
    /// Check if an item is a gem
    /// </summary>
    private bool IsGemItem(ChestItemData item)
    {
        if (item == null) return false;
        
        // Check if it's a GemItemData or has gem-like properties
        return item is GemItemData || 
               item.itemType == "Gem" || 
               item.itemName.ToLower().Contains("gem");
    }
    
    /// <summary>
    /// Check if an item is a backpack
    /// </summary>
    private bool IsBackpackItem(ChestItemData item)
    {
        if (item == null) return false;
        
        // Check if it's a BackpackItem or has backpack-like properties
        return item is BackpackItem || 
               item.itemType == "Backpack" || 
               item.itemName.ToLower().Contains("backpack");
    }
    
    /// <summary>
    /// Check if an item is a vest
    /// </summary>
    private bool IsVestItem(ChestItemData item)
    {
        if (item == null) return false;
        
        // Check if it's a VestItem or has vest-like properties
        return item is VestItem || 
               item.itemType == "Vest" || 
               item.itemName.ToLower().Contains("vest");
    }
    
    /// <summary>
    /// Check if an item is a weapon
    /// </summary>
    private bool IsWeaponItem(ChestItemData item)
    {
        if (item == null) return false;
        
        // Check if it's an EquippableWeaponItemData
        return item is EquippableWeaponItemData;
    }
    
    /// <summary>
    /// Clear this slot
    /// </summary>
    public void ClearSlot()
    {
        CurrentItem = null;
        ItemCount = 0;
        UpdateVisuals();
        
        // ⚔️ CRITICAL FIX: Fire OnSlotChanged event when clearing
        // This ensures WeaponEquipmentManager gets notified when weapon slot is cleared
        OnSlotChanged?.Invoke(null, 0);
    }
    
    /// <summary>
    /// Legacy compatibility method - Remove items from this slot
    /// </summary>
    public bool RemoveItem(int count = 1)
    {
        if (IsEmpty) return false;
        
        if (ItemCount <= count)
        {
            ClearSlot();
        }
        else
        {
            SetItem(CurrentItem, ItemCount - count);
        }
        return true;
    }
    
    /// <summary>
    /// Update visual elements based on current state
    /// </summary>
    public void UpdateVisuals()
    {
        // COMPREHENSIVE DEBUG LOGGING FOR GEM DISPLAY ISSUES
        Debug.Log($" UpdateVisuals called on {gameObject.name} - isGemSlot: {isGemSlot}, isEmpty: {IsEmpty}");
        
        // CRITICAL FIX: Only itemIcon is required - countText and slotBackground are optional!
        if (itemIcon == null)
        {
            Debug.LogError($"❌ UnifiedSlot on {gameObject.name}: Missing itemIcon component! Cannot display items!");
            
            // WEAPON SLOT FIX: Even if itemIcon is missing, ALWAYS show background if it exists
            if (slotBackground != null)
            {
                slotBackground.gameObject.SetActive(true);
                Debug.Log($"✅ Showing background for {gameObject.name} even though itemIcon is missing");
            }
            
            return;
        }
        
        // Warn about missing optional components but don't block
        if (countText == null && !isWeaponSlot)
        {
            Debug.LogWarning($"⚠️ UnifiedSlot on {gameObject.name}: Missing countText (optional - only needed for stackable items)");
        }
        
        if (slotBackground == null && !isWeaponSlot)
        {
            Debug.LogWarning($"⚠️ UnifiedSlot on {gameObject.name}: Missing slotBackground (optional but recommended)");
        }
        
        // CRITICAL: ALWAYS ensure slot background is visible (never hide drop zones!)
        // Show background even if countText is missing - background is ALWAYS visible!
        if (slotBackground != null)
        {
            slotBackground.gameObject.SetActive(true);
            // Ensure background stays behind icon in UI hierarchy
            if (slotBackground.transform is RectTransform bgRect)
            {
                bgRect.SetAsFirstSibling(); // Move background to back
            }
            Debug.Log($"✅ Background visible for {gameObject.name} (countText: {(countText != null ? "present" : "MISSING - OK")})");
        }
        
        if (IsEmpty)
        {
            Debug.Log($" Slot {gameObject.name} is empty - clearing visuals");
            
            //  FIX: For gem slots, always show gem icon even when count is 0
            if (isGemSlot)
            {
                // Use inspector-assigned gem sprite for empty gem slots
                if (defaultGemSprite != null)
                {
                    itemIcon.sprite = defaultGemSprite;
                    itemIcon.color = new Color(1f, 1f, 1f, 0.5f); // 50% transparency for default gem sprite
                    if (countText != null) countText.text = "0"; // Show 0 count for empty gem slots
                    Debug.Log($" Using inspector-assigned gem sprite for empty gem slot at 50% transparency: {defaultGemSprite.name}");
                }
                else
                {
                    Debug.LogWarning($" No defaultGemSprite assigned in inspector for gem slot {gameObject.name}!");
                    itemIcon.sprite = null;
                    itemIcon.color = new Color(1f, 1f, 1f, 0f);
                    if (countText != null) countText.text = "";
                }
            }
            else if (isForgeInputSlot || isForgeOutputSlot)
            {
                // FORGE slots: Hide item icon completely when empty (only show background)
                itemIcon.sprite = null;
                itemIcon.color = new Color(1f, 1f, 1f, 0f); // Completely invisible
                if (countText != null) countText.text = "";
                Debug.Log($" FORGE slot {gameObject.name} cleared - item icon hidden, background visible");
            }
            else
            {
                // Regular slots: Clear icon when empty
                itemIcon.sprite = null;
                itemIcon.color = new Color(1f, 1f, 1f, 0f); // Fully transparent when empty
                if (countText != null) countText.text = "";
            }
        }
        else
        {
            Debug.Log($" Slot {gameObject.name} has item: {CurrentItem?.itemName} (Count: {ItemCount})");
            Debug.Log($" Item icon sprite: {(CurrentItem?.itemIcon != null ? CurrentItem.itemIcon.name : "NULL")}");
            Debug.Log($" IsGem check: {(CurrentItem != null ? IsGemItem(CurrentItem) : false)}");
            
            // ✅ CRITICAL FIX: Check if item has a valid icon BEFORE setting it
            if (CurrentItem?.itemIcon == null)
            {
                Debug.LogError($" CRITICAL: Item {CurrentItem?.itemName} has NULL itemIcon! Cannot display icon - clearing slot visual!");
                // Clear the icon to prevent white square
                itemIcon.sprite = null;
                itemIcon.color = new Color(1f, 1f, 1f, 0f); // Hide icon completely
                if (countText != null) countText.text = ItemCount > 1 ? ItemCount.ToString() : "";
                return; // Exit early - can't show icon
            }
            
            //  FIX: Set item icon with FULL OPACITY on top of background
            itemIcon.sprite = CurrentItem.itemIcon;
            itemIcon.color = new Color(1f, 1f, 1f, 1f); // FULL OPACITY WHITE (no transparency)
            
            // Ensure icon is rendered on top by setting it to front
            if (itemIcon.transform is RectTransform iconRect)
            {
                iconRect.SetAsLastSibling(); // Move to front of UI hierarchy
            }
            
            if (countText != null) countText.text = ItemCount > 1 ? ItemCount.ToString() : "";
        
            //  CHEST FIX: Also update ChestInventorySlot quantityText if this is a chest slot
            ChestInventorySlot chestSlot = GetComponent<ChestInventorySlot>();
            if (chestSlot != null)
            {
                chestSlot.itemCount = ItemCount; // Sync the count
                chestSlot.UpdateQuantityDisplay(); // Update chest display
                Debug.Log($" Synced ChestInventorySlot count to {ItemCount} for {gameObject.name}");
            }
        
            Debug.Log($" Set icon sprite to: {itemIcon.sprite?.name}, countText to: '{countText?.text}', alpha: 1.0 (FULL OPACITY)");
        }
    }
    
    /// <summary>
    /// Handle pointer clicks (left-click for double-click, right-click for delete)
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"[UnifiedSlot] OnPointerClick called on {gameObject.name} - Button: {eventData.button}, IsEmpty: {IsEmpty}");
        
        // CRITICAL FIX: Check if this is a locked purchasable slot BEFORE processing any clicks
        if (isStashSlot)
        {
            // Ensure we have a StashManager reference
            if (_stashManager == null)
            {
                _stashManager = FindFirstObjectByType<StashManager>();
            }
            
            if (_stashManager != null && _stashManager.IsPurchasableSlot(this))
            {
                bool isSlotPurchased = _stashManager.IsSlotPurchased(this);
                Debug.Log($"[UnifiedSlot] Stash slot {gameObject.name} - Purchasable: true, Purchased: {isSlotPurchased}");
                
                if (!isSlotPurchased)
                {
                    Debug.Log($"[UnifiedSlot] 🎯 LOCKED SLOT CLICKED: {gameObject.name} - triggering purchase directly!");
                    // DIRECTLY TRIGGER PURCHASE instead of blocking
                    _stashManager.TriggerSlotPurchaseFromClick(this);
                    return;
                }
            }
        }
        
        if (IsEmpty) 
        {
            Debug.Log($"[UnifiedSlot] Slot {gameObject.name} is empty - ignoring click");
            return;
        }
        
        // ⚔️ SIMPLIFIED: Right-click drops item to world (inventory/weapon slots)
        // For stash/equipment slots, use legacy OnRightClick event for deletion
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            Debug.Log($"[UnifiedSlot] Right-click detected on {gameObject.name} (isWeaponSlot: {isWeaponSlot}, isStashSlot: {isStashSlot}, isEquipmentSlot: {isEquipmentSlot})");
            
            // INVENTORY/WEAPON SLOTS: Drop to world as pickup
            if (!isStashSlot && !isEquipmentSlot && !isForgeOutputSlot && !isForgeInputSlot)
            {
                Debug.Log($"[UnifiedSlot] 🌍 Right-click DROP TO WORLD from {gameObject.name} - item: {CurrentItem?.itemName}");
                HandleDropToWorld(eventData);
                return; // Item dropped, slot cleared
            }
            
            // STASH/EQUIPMENT/FORGE SLOTS: Use legacy event (for deletion)
            Debug.Log($"[UnifiedSlot] ⚠️ Right-click on special slot - firing OnRightClick event for external handler");
            OnRightClick?.Invoke(this);
            return;
        }
        
        // Handle left-click double-click detection
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            float timeSinceLastClick = Time.time - _lastClickTime;
            Debug.Log($"[UnifiedSlot] Left-click on {gameObject.name} - Time since last click: {timeSinceLastClick:F3}s (threshold: {DOUBLE_CLICK_TIME}s)");
            
            if (timeSinceLastClick <= DOUBLE_CLICK_TIME)
            {
                // CRITICAL: Prevent rapid double-click spam causing duplication
                float timeSinceLastTransfer = Time.time - _lastTransferTime;
                if (_isProcessingTransfer || timeSinceLastTransfer < TRANSFER_COOLDOWN)
                {
                    Debug.LogWarning($"[UnifiedSlot] 🚫 TRANSFER COOLDOWN: Blocked rapid double-click (cooldown: {TRANSFER_COOLDOWN}s, time since last: {timeSinceLastTransfer:F3}s)");
                    return; // Block rapid transfers
                }
                
                Debug.Log($"[UnifiedSlot] 🎯 DOUBLE-CLICK DETECTED on {gameObject.name}!");
                
                // FORGE FIX: Prevent double-clicking preview items from forge output slots
                if (isForgeOutputSlot)
                {
                    ForgeManager forgeManager = ForgeManager.Instance;
                    
                    // Check if this is a preview item (semi-transparent) by checking the alpha value
                    if (itemIcon != null && itemIcon.color.a < 1f)
                    {
                        Debug.Log($"❌ FORGE: Cannot double-click preview item from forge output slot - item not yet crafted! Alpha: {itemIcon.color.a}");
                        return; // Block double-clicking of preview items
                    }
                    
                    // Also check if ForgeManager is currently processing
                    if (forgeManager != null && forgeManager.IsProcessing)
                    {
                        Debug.Log($"❌ FORGE: Cannot double-click item while crafting is in progress!");
                        return; // Block double-clicking during processing
                    }
                    
                    // Additional check: if ForgeManager has a valid recipe, this is still a preview
                    if (forgeManager != null && forgeManager.HasValidRecipe)
                    {
                        Debug.Log($"❌ FORGE: Cannot double-click preview item - recipe still active! Item: {CurrentItem?.itemName}");
                        return; // Block double-clicking of preview items
                    }
                    
                    Debug.Log($"✅ FORGE: Allowing double-click of completed crafted item: {CurrentItem?.itemName}");
                }
                
                // GOODS OPENING: Check if this is a goods item that should be opened
                if (IsGoodsItem(CurrentItem))
                {
                    Debug.Log($"[UnifiedSlot] 🎁 GOODS OPENING: Attempting to open {CurrentItem?.itemName}");
                    
                    // Try to open the goods item
                    GoodsOpeningHandler goodsHandler = GoodsOpeningHandler.Instance;
                    if (goodsHandler != null)
                    {
                        bool opened = goodsHandler.TryOpenGoods(this);
                        if (opened)
                        {
                            Debug.Log($"[UnifiedSlot] ✅ Successfully opened goods: {CurrentItem?.itemName}");
                            return; // Exit early - goods was opened and slot will be cleared
                        }
                        else
                        {
                            Debug.Log($"[UnifiedSlot] ❌ Failed to open goods: {CurrentItem?.itemName}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[UnifiedSlot] GoodsOpeningHandler not found! Cannot open goods.");
                    }
                }
                
                // WEAPON AUTO-EQUIP: Check if this is a weapon that should be auto-equipped
                if (IsWeaponItem(CurrentItem) && !isWeaponSlot)
                {
                    Debug.Log($"[UnifiedSlot] ⚔️ WEAPON AUTO-EQUIP: Attempting to equip {CurrentItem?.itemName}");
                    
                    // Find the WeaponEquipmentManager
                    WeaponEquipmentManager weaponManager = WeaponEquipmentManager.Instance;
                    if (weaponManager != null && weaponManager.rightHandWeaponSlot != null)
                    {
                        UnifiedSlot weaponSlot = weaponManager.rightHandWeaponSlot;
                        
                        // Try to move weapon to equipment slot
                        if (weaponSlot.IsEmpty)
                        {
                            // Simple transfer - weapon slot is empty
                            weaponSlot.SetItem(CurrentItem, ItemCount, bypassValidation: true);
                            ClearSlot();
                            Debug.Log($"[UnifiedSlot] ✅ Weapon equipped to right hand slot!");
                            return; // Exit early - weapon was equipped
                        }
                        else
                        {
                            // Swap - weapon slot has a weapon, swap them
                            ChestItemData tempItem = weaponSlot.CurrentItem;
                            int tempCount = weaponSlot.ItemCount;
                            
                            weaponSlot.SetItem(CurrentItem, ItemCount, bypassValidation: true);
                            SetItem(tempItem, tempCount, bypassValidation: true);
                            Debug.Log($"[UnifiedSlot] ✅ Weapon swapped with right hand slot!");
                            return; // Exit early - weapons were swapped
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[UnifiedSlot] ⚠️ WeaponEquipmentManager or right hand slot not found! Cannot auto-equip weapon.");
                    }
                }
                
                // ✅ NEW: WEAPON AUTO-UNEQUIP: Check if this is a weapon slot being double-clicked to unequip
                if (IsWeaponItem(CurrentItem) && isWeaponSlot)
                {
                    Debug.Log($"[UnifiedSlot] ⚔️ WEAPON AUTO-UNEQUIP: Attempting to unequip {CurrentItem?.itemName}");
                    
                    // Find the InventoryManager to get first available inventory slot
                    InventoryManager inventoryManager = InventoryManager.Instance;
                    if (inventoryManager != null)
                    {
                        // Try to find first empty inventory slot
                        UnifiedSlot emptySlot = inventoryManager.GetFirstEmptySlot();
                        if (emptySlot != null)
                        {
                            // Move weapon to empty inventory slot
                            emptySlot.SetItem(CurrentItem, ItemCount, bypassValidation: true);
                            ClearSlot();
                            Debug.Log($"[UnifiedSlot] ✅ Weapon unequipped to inventory slot {emptySlot.gameObject.name}!");
                            return; // Exit early - weapon was unequipped
                        }
                        else
                        {
                            Debug.LogWarning($"[UnifiedSlot] ⚠️ No empty inventory slots available to unequip weapon!");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[UnifiedSlot] ⚠️ InventoryManager not found! Cannot auto-unequip weapon.");
                    }
                }
                
                // CRITICAL: Set transfer processing flag to prevent spam
                _isProcessingTransfer = true;
                _lastTransferTime = Time.time;
                
                try
                {
                    // Double click detected - handle chest collection if this is a chest slot
                    if (chestSystem != null)
                    {
                        Debug.Log($" Double-click collect: {CurrentItem?.itemName} from chest slot");
                        chestSystem.CollectItem(this);
                    }
                    else
                    {
                        Debug.Log($"[UnifiedSlot] Invoking OnDoubleClick event for {gameObject.name}");
                        // Regular double-click for non-chest slots
                        OnDoubleClick?.Invoke(this);
                    }
                }
                finally
                {
                    // CRITICAL: Always clear processing flag, even if transfer fails
                    _isProcessingTransfer = false;
                }
            }
            else
            {
                Debug.Log($"[UnifiedSlot] Single click on {gameObject.name} (not double-click)");
            }
            
            _lastClickTime = Time.time;
        }
    }
    
    /// <summary>
    /// Begin drag operation
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (IsEmpty) return;
        
        // EQUIPMENT SLOT FIX: Prevent dragging items OUT of equipment slots (vest/backpack only)
        // Weapon slots (isWeaponSlot) allow dragging out
        if (isEquipmentSlot && !isWeaponSlot)
        {
            Debug.Log($"❌ EQUIPMENT: Cannot drag item out of equipment slot {gameObject.name} - equipment cannot be unequipped, only replaced");
            // CRITICAL: Ensure drag state is completely clean
            _draggedSlot = null;
            _draggedVisual = null;
            return; // Block dragging from vest/backpack equipment slots
        }
        
        // FORGE FIX: Prevent dragging preview items from forge output slots
        if (isForgeOutputSlot)
        {
            ForgeManager forgeManager = ForgeManager.Instance;
            
            // Check if this is a preview item (semi-transparent) by checking the alpha value
            if (itemIcon != null && itemIcon.color.a < 1f)
            {
                Debug.Log($"❌ FORGE: Cannot drag preview item from forge output slot - item not yet crafted! Alpha: {itemIcon.color.a}");
                // CRITICAL: Ensure drag state is completely clean
                _draggedSlot = null;
                _draggedVisual = null;
                return; // Block dragging of preview items
            }
            
            // Also check if ForgeManager is currently processing
            if (forgeManager != null && forgeManager.IsProcessing)
            {
                Debug.Log($"❌ FORGE: Cannot drag item while crafting is in progress!");
                // CRITICAL: Ensure drag state is completely clean
                _draggedSlot = null;
                _draggedVisual = null;
                return; // Block dragging during processing
            }
            
            // Additional check: if ForgeManager has a valid recipe, this is still a preview
            if (forgeManager != null && forgeManager.HasValidRecipe)
            {
                Debug.Log($"❌ FORGE: Cannot drag preview item - recipe still active! Item: {CurrentItem?.itemName}");
                // CRITICAL: Ensure drag state is completely clean
                _draggedSlot = null;
                _draggedVisual = null;
                return; // Block dragging of preview items
            }
            
            Debug.Log($"✅ FORGE: Allowing drag of completed crafted item: {CurrentItem?.itemName}");
        }
        
        _draggedSlot = this;
        
        // Find dragImage canvas (present in both menu and game scenes)
        Canvas dragImageCanvas = FindDragImageCanvas();
        if (dragImageCanvas == null)
        {
            Debug.LogWarning(" DragImage canvas not found! Falling back to default canvas.");
            dragImageCanvas = _canvas;
        }
        
        // Create visual drag representation on dragImage canvas
        _draggedVisual = new GameObject("DragVisual");
        _draggedVisual.transform.SetParent(dragImageCanvas.transform, false);
        
        Image dragImage = _draggedVisual.AddComponent<Image>();
        dragImage.sprite = CurrentItem.itemIcon;
        dragImage.raycastTarget = false;
        
        // Position at cursor with slightly enlarged size (1.3x scale)
        RectTransform dragRect = _draggedVisual.GetComponent<RectTransform>();
        dragRect.sizeDelta = new Vector2(65, 65); // 1.3x larger than standard 50x50
        dragRect.position = eventData.position;
        
        // Make original slot semi-transparent during drag
        itemIcon.color = new Color(1, 1, 1, 0.5f);
        
        Debug.Log($" Started dragging {CurrentItem?.itemName} with enlarged visual (65x65)");
    }
    
    /// <summary>
    /// Update drag visual position
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (_draggedVisual != null)
        {
            _draggedVisual.transform.position = eventData.position;
        }
    }
    
    /// <summary>
    /// End drag operation
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        // ⭐ IMPROVED: Multiple checks for drag-to-world detection
        // This is called on the SOURCE slot (the one being dragged FROM)
        bool draggedOutsideUI = false;
        
        // Check 1: No raycast hit (mouse over nothing)
        if (eventData.pointerCurrentRaycast.gameObject == null)
        {
            draggedOutsideUI = true;
            Debug.Log($"[UnifiedSlot] 🌍 Drag-to-world CHECK 1: No raycast hit");
        }
        
        // Check 2: Raycast hit but not a UI element (hit world geometry)
        if (!draggedOutsideUI && eventData.pointerCurrentRaycast.gameObject != null)
        {
            // Check if the hit object has a Canvas component or is a child of Canvas
            Canvas hitCanvas = eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<Canvas>();
            if (hitCanvas == null)
            {
                draggedOutsideUI = true;
                Debug.Log($"[UnifiedSlot] 🌍 Drag-to-world CHECK 2: Hit non-UI object ({eventData.pointerCurrentRaycast.gameObject.name})");
            }
        }
        
        // Check 3: Pointer is outside screen bounds (for safety)
        if (!draggedOutsideUI)
        {
            Vector2 mousePos = eventData.position;
            if (mousePos.x < 0 || mousePos.x > Screen.width || mousePos.y < 0 || mousePos.y > Screen.height)
            {
                draggedOutsideUI = true;
                Debug.Log($"[UnifiedSlot] 🌍 Drag-to-world CHECK 3: Mouse outside screen bounds");
            }
        }
        
        // If dragged outside UI, drop to world
        if (!IsEmpty && _draggedSlot == this && draggedOutsideUI)
        {
            // Item was dragged outside UI - drop it in the world
            Debug.Log($"[UnifiedSlot] 🌍 ✅ CONFIRMED DRAG TO WORLD from {gameObject.name} - item: {CurrentItem?.itemName}");
            HandleDropToWorld(eventData);
            
            // Early return - don't restore colors since slot will be cleared
            _draggedSlot = null;
            if (_draggedVisual != null)
            {
                Destroy(_draggedVisual);
                _draggedVisual = null;
            }
            return;
        }
        
        Debug.Log($"[UnifiedSlot] Drag ended normally (not to world) - draggedOutsideUI: {draggedOutsideUI}, isEmpty: {IsEmpty}, _draggedSlot==this: {_draggedSlot == this}");
        
        // FORGE FIX: Don't restore color for forge output slots showing preview items
        if (!IsEmpty)
        {
            if (isForgeOutputSlot)
            {
                // For forge output slots, check if this should remain a preview (semi-transparent)
                ForgeManager forgeManager = ForgeManager.Instance;
                if (forgeManager != null)
                {
                    // If still processing or has valid recipe (preview state), keep semi-transparent
                    if (forgeManager.IsProcessing)
                    {
                        // During processing, keep the processing transparency
                        Debug.Log($"FORGE: Maintaining processing transparency for {CurrentItem?.itemName}");
                        // Don't change the color - let ForgeManager handle it
                    }
                    else
                    {
                        // Check if this is still a preview by checking if there's a current valid recipe
                        if (forgeManager.HasValidRecipe)
                        {
                            // Still showing a preview - keep semi-transparent
                            Debug.Log($"FORGE: Keeping preview transparency for {CurrentItem?.itemName}");
                            // Don't restore to white - this is still a preview
                            return;
                        }
                        
                        // This is a completed crafted item - restore full opacity
                        itemIcon.color = Color.white;
                        Debug.Log($"FORGE: Restored full opacity for completed item: {CurrentItem?.itemName}");
                    }
                }
            }
            else
            {
                // Regular slots - always restore to white
                itemIcon.color = Color.white;
            }
        }
        
        // Clean up drag visual
        if (_draggedVisual != null)
        {
            Destroy(_draggedVisual);
            _draggedVisual = null;
        }
        
        _draggedSlot = null;
    }
    
    /// <summary>
    /// Handle item dropped on this slot
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        if (_draggedSlot == null || _draggedSlot == this) return;
        
        // Check if this slot can accept the dragged item
        if (!CanAcceptItem(_draggedSlot.CurrentItem))
        {
            Debug.Log($" Drop rejected: {(_draggedSlot.CurrentItem?.itemName ?? "item")} cannot be placed in {(isGemSlot ? "gem slot" : "regular slot")}");
            return; // Reject the drop
        }
        
        // If target slot is not empty, check if dragged slot can accept target item (for swapping)
        if (!IsEmpty && !_draggedSlot.CanAcceptItem(CurrentItem))
        {
            Debug.Log($" Swap rejected: {(CurrentItem?.itemName ?? "item")} cannot be placed in {(_draggedSlot.isGemSlot ? "gem slot" : "regular slot")}");
            return; // Reject the swap
        }
        
        // Notify StashManager to handle the transfer/swap
        OnItemDropped?.Invoke(_draggedSlot, this);
    }
    
    /// <summary>
    /// Handle dropping item into the world (outside UI)
    /// </summary>
    private void HandleDropToWorld(PointerEventData eventData)
    {
        if (IsEmpty) return;
        
        // Get the item being dropped from THIS slot
        ChestItemData itemToDrop = CurrentItem;
        int countToDrop = ItemCount;
        bool isWeapon = IsWeaponItem(itemToDrop);
        
        Debug.Log($"[UnifiedSlot] 🌍 Dropping {countToDrop}x {itemToDrop.itemName} to world from {gameObject.name} (isWeapon: {isWeapon}, isWeaponSlot: {isWeaponSlot})");
        
        // Find WorldItemDropper
        Debug.Log($"[UnifiedSlot] 🔍 Searching for WorldItemDropper...");
        WorldItemDropper dropper = FindFirstObjectByType<WorldItemDropper>();
        if (dropper == null)
        {
            Debug.LogError("[UnifiedSlot] ❌❌❌ WorldItemDropper not found in scene! Cannot drop item to world.");
            Debug.LogError("[UnifiedSlot] Please add a GameObject with WorldItemDropper component to your scene!");
            return;
        }
        Debug.Log($"[UnifiedSlot] ✅ Found WorldItemDropper: {dropper.gameObject.name}");
        
        // Get player position for drop
        Debug.Log($"[UnifiedSlot] 🔍 Searching for Player...");
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[UnifiedSlot] ❌❌❌ Player not found! Cannot drop item to world.");
            Debug.LogError("[UnifiedSlot] Ensure your player GameObject has the 'Player' tag!");
            return;
        }
        Debug.Log($"[UnifiedSlot] ✅ Found Player: {player.name}");
        
        // Calculate drop position in front of player
        Vector3 dropPosition = player.transform.position + player.transform.forward * 100f; // 100 units in front
        Debug.Log($"[UnifiedSlot] 📍 Drop position calculated: {dropPosition}");
        
        // Drop the item using WorldItemDropper
        Debug.Log($"[UnifiedSlot] 🎯 Calling WorldItemDropper.DropItem()...");
        dropper.DropItem(itemToDrop, countToDrop, dropPosition);
        Debug.Log($"[UnifiedSlot] ✅ WorldItemDropper.DropItem() completed!");
        
        // ✅ CRITICAL FIX: Clear slot BEFORE notifying listeners
        // This ensures OnSlotChanged event fires with correct state (empty slot)
        ChestItemData droppedItem = CurrentItem; // Save reference before clearing
        ClearSlot(); // This triggers OnSlotChanged event
        
        // ✅ NEW: If this was a weapon slot, explicitly notify WeaponEquipmentManager
        // This ensures PlayerShooterOrchestrator gets updated immediately
        if (isWeaponSlot && isWeapon)
        {
            Debug.Log($"[UnifiedSlot] ⚔️ Weapon dropped from weapon slot - WeaponEquipmentManager will auto-update via OnSlotChanged event");
            // NOTE: No need to manually notify - ClearSlot() already fired OnSlotChanged event
            // WeaponEquipmentManager listens to that event and updates PlayerShooterOrchestrator
        }
        
        Debug.Log($"[UnifiedSlot] ✅ Dropped {countToDrop}x {droppedItem.itemName} to world at {dropPosition}");
    }
    
    // === CHEST INTERACTION METHODS ===
    
    /// <summary>
    /// Show hover highlight when mouse enters slot
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverHighlight != null)
        {
            if (useSharedHighlight)
            {
                // Shared highlight system: move one highlight between slots
                ShowSharedHighlight();
            }
            else
            {
                // Individual highlight system: each slot has its own
                hoverHighlight.SetActive(true);
            }
            
            Debug.Log($" Hover enter on {(isStashSlot ? "stash" : "inventory")} slot" + (IsEmpty ? " (empty)" : $" with {CurrentItem?.itemName}"));
        }
        
        // 🧠 COGNITIVE INTEGRATION: Only trigger in game contexts (not menus)
        if (!IsEmpty && CurrentItem != null && ShouldUseCognitiveSystem())
        {
            Debug.Log($"🧠 COGNITIVE: Triggering hover START for {CurrentItem.itemName} in {GetSlotContextDescription()}");
            CognitiveEvents.OnItemHoverStart?.Invoke(CurrentItem, this);
        }
        else
        {
            string reason = IsEmpty ? "slot is empty" : 
                           CurrentItem == null ? "no item" : 
                           "menu context (using existing hover overlay)";
            Debug.Log($"🧠 COGNITIVE: No cognitive event - {reason}");
        }
        
        // Show tooltip
        ShowItemTooltip();
    }
    
    /// <summary>
    /// Hide hover highlight when mouse exits slot
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoverHighlight != null)
        {
            if (useSharedHighlight)
            {
                // Shared highlight system: hide shared highlight
                HideSharedHighlight();
            }
            else
            {
                // Individual highlight system: hide this slot's highlight
                hoverHighlight.SetActive(false);
            }
            
            Debug.Log($" Hover exit on {(isStashSlot ? "stash" : "inventory")} slot");
        }
        
        // 🧠 COGNITIVE INTEGRATION: Only trigger end event if we triggered start event
        if (!IsEmpty && CurrentItem != null && ShouldUseCognitiveSystem())
        {
            Debug.Log($"🧠 COGNITIVE: Triggering hover END for {CurrentItem.itemName} in {GetSlotContextDescription()}");
            CognitiveEvents.OnItemHoverEnd?.Invoke(CurrentItem, this);
        }
        else
        {
            Debug.Log($"🧠 COGNITIVE: No hover end event - using existing menu overlay system");
        }
        
        // Hide tooltip
        HideItemTooltip();
    }
    
    /// <summary>
    /// Show shared highlight on this slot (moves highlight from other slots)
    /// </summary>
    private void ShowSharedHighlight()
    {
        // Hide highlight from currently highlighted slot
        if (_currentlyHighlighted != null && _currentlyHighlighted != this)
        {
            if (_sharedHighlightInstance != null)
            {
                _sharedHighlightInstance.SetActive(false);
            }
        }
        
        // Create shared highlight instance if needed
        if (_sharedHighlightInstance == null && hoverHighlight != null)
        {
            _sharedHighlightInstance = Instantiate(hoverHighlight, transform.parent);
            _sharedHighlightInstance.name = "SharedHighlight";
        }
        
        // Position and show highlight on this slot
        if (_sharedHighlightInstance != null)
        {
            _sharedHighlightInstance.transform.SetParent(transform, false);
            _sharedHighlightInstance.transform.localPosition = Vector3.zero;
            _sharedHighlightInstance.SetActive(true);
            _currentlyHighlighted = this;
        }
    }
    
    /// <summary>
    /// Hide shared highlight if this slot is currently highlighted
    /// </summary>
    private void HideSharedHighlight()
    {
        if (_currentlyHighlighted == this && _sharedHighlightInstance != null)
        {
            _sharedHighlightInstance.SetActive(false);
            _currentlyHighlighted = null;
        }
    }
    
    /// <summary>
    /// Show tooltip with item information at mouse position
    /// </summary>
    private void ShowItemTooltip()
    {
        // Only show tooltip if enabled and we have an item
        if (!enableTooltip || IsEmpty || tooltipPrefab == null) return;
        
        // Create tooltip instance if needed
        if (_tooltipInstance == null)
        {
            // Use the same dragImage canvas that works everywhere (priority 9999)
            Canvas dragCanvas = FindDragImageCanvas();
            if (dragCanvas == null)
            {
                Debug.LogWarning("No dragImage canvas available for tooltip!");
                return;
            }
            
            _tooltipInstance = Instantiate(tooltipPrefab, dragCanvas.transform);
            _tooltipInstance.name = "ItemTooltip";
            Debug.Log($"Created tooltip on dragImage canvas: {dragCanvas.name} (sort order: {dragCanvas.sortingOrder})");
        }
        
        // Update tooltip content
        var tooltipText = _tooltipInstance.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (tooltipText != null && CurrentItem != null)
        {
            // Build tooltip text with item information
            string tooltipContent = $"<b>{CurrentItem.itemName}</b>";
            
            if (!string.IsNullOrEmpty(CurrentItem.description))
            {
                tooltipContent += $"\n{CurrentItem.description}";
            }
            
            if (ItemCount > 1)
            {
                tooltipContent += $"\n<i>Quantity: {ItemCount}</i>";
            }
            
            // Add item type info
            tooltipContent += $"\n<color=#888888>Type: {CurrentItem.itemType}</color>";
            
            tooltipText.text = tooltipContent;
        }
        
        // Position tooltip near mouse
        PositionTooltipAtMouse();
        
        // Show tooltip and track which slot is showing it
        _tooltipInstance.SetActive(true);
        _currentTooltipSlot = this;
    }
    
    /// <summary>
    /// Hide the tooltip
    /// </summary>
    private void HideItemTooltip()
    {
        if (_tooltipInstance != null && _currentTooltipSlot == this)
        {
            _tooltipInstance.SetActive(false);
            _currentTooltipSlot = null;
        }
    }
    
    /// <summary>
    /// Position tooltip near mouse cursor, keeping it on screen
    /// </summary>
    private void PositionTooltipAtMouse()
    {
        if (_tooltipInstance == null) return;
        
        // Get mouse position in screen coordinates
        Vector3 mousePos = Input.mousePosition;
        
        // Offset tooltip slightly to avoid cursor overlap
        mousePos.x += 15f;
        mousePos.y -= 15f;
        
        // Get tooltip rect for bounds checking
        RectTransform tooltipRect = _tooltipInstance.GetComponent<RectTransform>();
        if (tooltipRect != null)
        {
            // Keep tooltip on screen (simple bounds check)
            float tooltipWidth = tooltipRect.rect.width;
            float tooltipHeight = tooltipRect.rect.height;
            
            // Clamp to screen bounds
            mousePos.x = Mathf.Clamp(mousePos.x, 0, Screen.width - tooltipWidth);
            mousePos.y = Mathf.Clamp(mousePos.y, tooltipHeight, Screen.height);
        }
        
        // Set position
        _tooltipInstance.transform.position = mousePos;
    }
    
    /// <summary>
    /// Find the dragImage canvas for drag visuals (present in both menu and game scenes)
    /// </summary>
    private Canvas FindDragImageCanvas()
    {
        // Method 1: Find by exact name "dragImage"
        GameObject dragImageObj = GameObject.Find("dragImage");
        if (dragImageObj != null)
        {
            Canvas canvas = dragImageObj.GetComponent<Canvas>();
            if (canvas != null)
            {
                Debug.Log($" Found dragImage canvas by exact name: {dragImageObj.name}");
                return canvas;
            }
        }
        
        // Method 2: Find canvas with "drag" in the name (case insensitive)
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in allCanvases)
        {
            if (canvas.name.ToLower().Contains("drag"))
            {
                Debug.Log($" Found drag canvas by name search: {canvas.name}");
                return canvas;
            }
        }
        
        // Method 3: Find highest sort order canvas (likely the UI overlay)
        Canvas highestCanvas = null;
        int highestSortOrder = int.MinValue;
        foreach (Canvas canvas in allCanvases)
        {
            if (canvas.sortingOrder > highestSortOrder)
            {
                highestSortOrder = canvas.sortingOrder;
                highestCanvas = canvas;
            }
        }
        
        if (highestCanvas != null)
        {
            Debug.Log($" Using highest sort order canvas for drag: {highestCanvas.name} (sort order: {highestSortOrder})");
            return highestCanvas;
        }
        
        Debug.LogWarning(" No suitable drag canvas found!");
        return null;
    }
    
    #region COGNITIVE SYSTEM HELPERS
    
    /// <summary>
    /// Determines if the cognitive system should be used in the current context
    /// Returns false for menu contexts, true for in-game contexts (inventory, chests)
    /// </summary>
    private bool ShouldUseCognitiveSystem()
    {
        // FORGE SLOTS: Use explicit context setting from inspector
        if (isForgeInputSlot || isForgeOutputSlot)
        {
            return isInGameContext;
        }
        
        // NON-FORGE SLOTS: Auto-detect context
        // Check if we're in a menu scene by looking for game-specific managers
        // If we find PlayerHealth, PlayerProgression, etc., we're in game
        bool hasPlayerHealth = FindFirstObjectByType<PlayerHealth>() != null;
        bool hasPlayerProgression = FindFirstObjectByType<PlayerProgression>() != null;
        bool hasInventoryManager = InventoryManager.Instance != null;
        
        // Also check if CognitiveFeedManagerEnhanced exists and is active
        var cognitiveManager = FindFirstObjectByType<CognitiveFeedManagerEnhanced>();
        bool hasCognitiveManager = cognitiveManager != null && cognitiveManager.gameObject.activeInHierarchy;
        
        // We're in a game context if we have core game systems AND cognitive manager
        bool isGameContext = (hasPlayerHealth || hasPlayerProgression || hasInventoryManager) && hasCognitiveManager;
        
        return isGameContext;
    }
    
    /// <summary>
    /// Returns a description of the current slot context for debugging
    /// </summary>
    private string GetSlotContextDescription()
    {
        if (chestSystem != null)
            return "chest";
        else if (isStashSlot)
            return "stash/inventory";
        else
            return "player inventory";
    }
    
    #endregion
}
