using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simple stash manager - handles basic stash/inventory operations only.
/// Requirements:
/// - On open: Load stash + inventory, auto-transfer gems inventory→stash
/// - Manual transfers: Save immediately
/// - On exit: Save both stash and inventory
/// </summary>
public class StashManager : MonoBehaviour
{
    public static StashManager Instance { get; private set; }
    [Header("Stash Slots - Uniform System")]
    [Tooltip("Stash gem slot (slot 0) - mark as isGemSlot=true in Inspector")]
    public UnifiedSlot stashGemSlot;
    public UnifiedSlot stashSlot1;
    public UnifiedSlot stashSlot2;
    public UnifiedSlot stashSlot3;
    public UnifiedSlot stashSlot4;
    public UnifiedSlot stashSlot5;
    
    [Header("Purchasable Stash Slots")]
    [Tooltip("Additional stash slots that can be purchased with gems")]
    public UnifiedSlot stashSlot6;
    public UnifiedSlot stashSlot7;
    public UnifiedSlot stashSlot8;
    public UnifiedSlot stashSlot9;
    public UnifiedSlot stashSlot10;
    public UnifiedSlot stashSlot11;
    public UnifiedSlot stashSlot12;
    public UnifiedSlot stashSlot13;
    public UnifiedSlot stashSlot14;
    public UnifiedSlot stashSlot15;
    
    [Header("Locked Slot UI Elements")]
    [Tooltip("UI elements for locked slots - should match purchasable slots order")]
    public GameObject[] lockedSlotImages = new GameObject[10];
    public TMPro.TextMeshProUGUI[] slotPriceTexts = new TMPro.TextMeshProUGUI[10];
    public Button[] purchaseButtons = new Button[10];
    
    [Header("Slot Purchase Configuration")]
    [Tooltip("Base cost for first purchasable slot")]
    public int baseSlotCost = 50;
    [Tooltip("Cost multiplier for each subsequent slot")]
    public float costMultiplier = 1.5f;
    
    [Header("Inventory Slots - Uniform System")]
    [Tooltip("Inventory gem slot (slot 0) - mark as isGemSlot=true in Inspector")]
    public UnifiedSlot inventoryGemSlot;
    public UnifiedSlot inventorySlot1;
    public UnifiedSlot inventorySlot2;
    public UnifiedSlot inventorySlot3;
    public UnifiedSlot inventorySlot4;
    public UnifiedSlot inventorySlot5;
    public UnifiedSlot inventorySlot6;
    public UnifiedSlot inventorySlot7;
    public UnifiedSlot inventorySlot8;
    public UnifiedSlot inventorySlot9;
    public UnifiedSlot inventorySlot10;
    public UnifiedSlot inventorySlot11;
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip gemTransferSound;
    public AudioClip transferSound;
    public AudioClip errorSound;
    
    [Header("Flying Gem Visual Effect")]
    [Tooltip("Enable flying gem visual effect (uses existing dragImage canvas)")]
    public bool enableFlyingGemEffect = true;
    [Tooltip("Duration for gem flight animation in seconds")]
    public float gemFlightDuration = 0.3f;
    
    // Slot collections
    private List<UnifiedSlot> _stashSlots = new List<UnifiedSlot>();
    private List<UnifiedSlot> _purchasableStashSlots = new List<UnifiedSlot>();
    private List<UnifiedSlot> _inventorySlots = new List<UnifiedSlot>();
    
    // Purchasable slot state
    private bool[] _slotsPurchased = new bool[10]; // Track which slots are purchased
    private int[] _slotCosts = new int[10]; // Cache slot costs
    
    // Save paths
    private string _stashSavePath;
    private string _inventorySavePath;
    
    // REMOVED: Transfer tracking variables - no longer needed for manual-only transfers
    
    void Awake()
    {
        // CRITICAL: StashManager should ONLY exist in menu scenes
        if (!IsInMenuScene())
        {
            Debug.Log("[StashManager] Destroying StashManager - not in menu scene");
            Destroy(gameObject);
            return;
        }
        
        // SMART singleton - only persist if we're a dedicated manager GameObject
        if (Instance == null)
        {
            Instance = this;
            // DON'T persist across scenes - StashManager should only exist in menu
            if (Application.isPlaying)
            {
                Debug.Log("[StashManager] Created singleton (MENU ONLY - will not persist)");
            }
        }
        else
        {
            Debug.Log("[StashManager] Duplicate instance destroyed");
            Destroy(gameObject);
            return;
        }
        
        // Setup save paths
        string saveDir = Application.persistentDataPath;
        _stashSavePath = Path.Combine(saveDir, "stash_data.json");
        _inventorySavePath = Path.Combine(saveDir, "inventory_data.json");
        
        // Setup slot collections - EXCLUDE gem slot (stashGemSlot is slot 0)
        _stashSlots.Clear();
        _stashSlots.Add(stashSlot1);  // This becomes index 0 in _stashSlots (but is slot 1 visually)
        _stashSlots.Add(stashSlot2);  // This becomes index 1 in _stashSlots (but is slot 2 visually)
        _stashSlots.Add(stashSlot3);  // This becomes index 2 in _stashSlots (but is slot 3 visually)
        _stashSlots.Add(stashSlot4);  // This becomes index 3 in _stashSlots (but is slot 4 visually)
        _stashSlots.Add(stashSlot5);  // This becomes index 4 in _stashSlots (but is slot 5 visually)
        
        // Setup purchasable slot collections
        _purchasableStashSlots.Clear();
        _purchasableStashSlots.Add(stashSlot6);
        _purchasableStashSlots.Add(stashSlot7);
        _purchasableStashSlots.Add(stashSlot8);
        _purchasableStashSlots.Add(stashSlot9);
        _purchasableStashSlots.Add(stashSlot10);
        _purchasableStashSlots.Add(stashSlot11);
        _purchasableStashSlots.Add(stashSlot12);
        _purchasableStashSlots.Add(stashSlot13);
        _purchasableStashSlots.Add(stashSlot14);
        _purchasableStashSlots.Add(stashSlot15);
        
        // Calculate slot costs
        for (int i = 0; i < _slotCosts.Length; i++)
        {
            _slotCosts[i] = Mathf.RoundToInt(baseSlotCost * Mathf.Pow(costMultiplier, i));
        }
        
        Debug.Log($"[StashManager] Setup {_stashSlots.Count} regular stash slots + {_purchasableStashSlots.Count} purchasable slots (gem slot excluded)");
        
        _inventorySlots.Clear();
        _inventorySlots.Add(inventorySlot1);
        _inventorySlots.Add(inventorySlot2);
        _inventorySlots.Add(inventorySlot3);
        _inventorySlots.Add(inventorySlot4);
        _inventorySlots.Add(inventorySlot5);
        _inventorySlots.Add(inventorySlot6);
        _inventorySlots.Add(inventorySlot7);
        _inventorySlots.Add(inventorySlot8);
        _inventorySlots.Add(inventorySlot9);
        _inventorySlots.Add(inventorySlot10);
        _inventorySlots.Add(inventorySlot11);
        
    }
    
    void Start()
    {
        // Setup slot event listeners for transfers and deletion
        foreach (var slot in _stashSlots)
        {
            if (slot != null)
            {
                slot.OnDoubleClick += HandleDoubleClick;
                slot.OnItemDropped += HandleItemDropped;
                slot.OnRightClick += HandleRightClick; // Right-click delete
            }
        }
        
        // Setup purchasable slot event listeners (only for purchased slots)
        for (int i = 0; i < _purchasableStashSlots.Count; i++)
        {
            var slot = _purchasableStashSlots[i];
            if (slot != null)
            {
                slot.OnDoubleClick += HandleDoubleClick;
                slot.OnItemDropped += HandleItemDropped;
                slot.OnRightClick += HandleRightClick;
            }
        }
        
        // Setup purchase button listeners
        for (int i = 0; i < purchaseButtons.Length; i++)
        {
            if (purchaseButtons[i] != null)
            {
                int slotIndex = i; // Capture for closure
                purchaseButtons[i].onClick.AddListener(() => PurchaseSlot(slotIndex));
            }
        }
        
        foreach (var slot in _inventorySlots)
        {
            if (slot != null)
            {
                slot.OnDoubleClick += HandleDoubleClick;
                slot.OnItemDropped += HandleItemDropped;
                slot.OnRightClick += HandleRightClick; // Right-click delete
            }
        }
        
        // ADDED: Setup gem slot event listeners for manual transfers
        if (stashGemSlot != null)
        {
            stashGemSlot.OnDoubleClick += HandleDoubleClick;
            stashGemSlot.OnItemDropped += HandleItemDropped;
            stashGemSlot.OnRightClick += HandleRightClick; // Right-click delete
            Debug.Log("[StashManager] Added event listeners to stash gem slot");
        }
        
        if (inventoryGemSlot != null)
        {
            inventoryGemSlot.OnDoubleClick += HandleDoubleClick;
            inventoryGemSlot.OnItemDropped += HandleItemDropped;
            inventoryGemSlot.OnRightClick += HandleRightClick; // Right-click delete
            Debug.Log($"[StashManager] Added event listeners to inventory gem slot: {inventoryGemSlot.gameObject.name}");
            Debug.Log($"[StashManager] Inventory gem slot isGemSlot: {inventoryGemSlot.isGemSlot}");
        }
        else
        {
            Debug.LogError("[StashManager] inventoryGemSlot is NULL - cannot add event listeners!");
        }
        
        // Load purchased slot data
        LoadPurchasedSlotData();
        
        // REMOVED AUTO-INITIALIZE: Let UIMenuManager control when stash initializes
        // This prevents premature loading that could interfere with slot assignments
        Debug.Log("[StashManager] StashManager ready - waiting for explicit InitializeStash() call");
    }
    
    /// <summary>
    /// Check if we're currently in a menu scene
    /// </summary>
    private bool IsInMenuScene()
    {
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Menu");
    }
    
    /// <summary>
    /// REMOVED: Stash slots should NEVER be cleared automatically
    /// Stash is 100% persistent - only user actions can modify it
    /// </summary>
    
    /// <summary>
    /// Called when stash menu opens - load data and sync with InventoryManager
    /// </summary>
    public void InitializeStash()
    {
        Debug.Log("[StashManager] InitializeStash called - loading stash and inventory data");
        
        // CRITICAL: Sync inventory gem slot reference with InventoryManager
        SyncInventoryGemSlotReference();
        
        // Load stash data
        LoadStashData();
        
        // CRITICAL FIX: Don't load inventory data at all when opening stash
        // This prevents overwriting existing items that player brought from game
        Debug.Log("[StashManager] Skipping inventory data loading to preserve existing items");
        
        // Only update gem display if needed
        InventoryManager menuInventoryManager = FindMenuInventoryManager();
        if (menuInventoryManager != null)
        {
            menuInventoryManager.UpdateGemDisplay();
            Debug.Log("[StashManager] Updated gem display only");
        }
        else if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.UpdateGemDisplay();
            Debug.Log("[StashManager] Updated gem display only (fallback)");
        }
        
        // REMOVED: Auto-transfer gems - now manual only via double-click or drag-drop
        Debug.Log("[StashManager] Gems will remain in inventory until manually transferred");
        
        // Update purchasable slot UI
        UpdatePurchasableSlotUI();
    }
    
    /// <summary>
    /// Called when stash menu closes - save both stash and inventory
    /// </summary>
    public void CleanupStash()
    {
        // No automatic gem transfer cleanup needed - all transfers are now manual
        Debug.Log("[StashManager] Cleaning up stash - saving data");
        
        SaveStashData();
        
        // Find and use MENU context InventoryManager for consistency
        InventoryManager menuInventoryManager = FindMenuInventoryManager();
        if (menuInventoryManager != null)
        {
            menuInventoryManager.SaveInventoryData();
            Debug.Log("[StashManager] Menu InventoryManager saved inventory data");
        }
        else
        {
            Debug.LogWarning("[StashManager] No Menu InventoryManager found - trying fallback to Instance");
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.SaveInventoryData();
                Debug.Log("[StashManager] Fallback InventoryManager saved inventory data");
            }
            else
            {
                Debug.LogWarning("[StashManager] No InventoryManager available to save inventory data");
            }
        }
    }
    
    /// <summary>
    /// Handle double-click on slots for transfers with intelligent stacking
    /// </summary>
    private void HandleDoubleClick(UnifiedSlot slot)
    {
        Debug.Log($"[StashManager] HandleDoubleClick called on slot: {slot.gameObject.name}");
        
        if (slot.IsEmpty)
        {
            Debug.Log($"[StashManager] Slot {slot.gameObject.name} is empty - ignoring double-click");
            return;
        }
        
        Debug.Log($"[StashManager] Slot {slot.gameObject.name} contains: {slot.CurrentItem?.itemName} x{slot.ItemCount}");
        
        // Transfer item to opposite container with SMART STACKING
        if (_stashSlots.Contains(slot))
        {
            Debug.Log($"[StashManager] Transferring from stash slot to inventory");
            // Transfer from stash to inventory with stacking
            SmartTransferToInventory(slot);
        }
        else if (_inventorySlots.Contains(slot))
        {
            Debug.Log($"[StashManager] Transferring from inventory slot to stash");
            // Transfer from inventory to stash with stacking
            SmartTransferToStash(slot);
        }
        else if (_purchasableStashSlots.Contains(slot))
        {
            Debug.Log($"[StashManager] Transferring from purchasable stash slot to inventory");
            // Transfer from purchasable stash slot to inventory with stacking
            SmartTransferToInventory(slot);
        }
        else if (slot == stashGemSlot)
        {
            Debug.Log($"[StashManager] Double-clicked stash gem slot - transferring gems to inventory");
            // Transfer gems from stash to inventory
            TransferGemsToInventory();
        }
        else if (slot == inventoryGemSlot)
        {
            Debug.Log($"[StashManager] Double-clicked inventory gem slot - transferring gems to stash");
            // Transfer gems from inventory to stash
            TransferGemsToStash();
        }
        else
        {
            Debug.LogWarning($"[StashManager] Double-clicked slot {slot.gameObject.name} is not recognized");
            Debug.LogWarning($"[StashManager] Slot comparison - stashGemSlot: {(slot == stashGemSlot)}, inventoryGemSlot: {(slot == inventoryGemSlot)}");
        }
        
        // Save immediately after transfer
        SaveBothContainers();
        
        // Update PersistentItemInventoryManager if inventory was modified
        InventoryManager menuInventoryManager = FindMenuInventoryManager();
        if (PersistentItemInventoryManager.Instance != null && menuInventoryManager != null)
        {
            PersistentItemInventoryManager.Instance.UpdateFromInventoryManager(menuInventoryManager);
            PersistentItemInventoryManager.Instance.SaveInventoryData();
        }
    }
    
    /// <summary>
    /// Handle right-click on slots for deletion
    /// </summary>
    private void HandleRightClick(UnifiedSlot slot)
    {
        if (slot.IsEmpty)
        {
            return;
        }
        
        
        // Clear the slot (resets to empty state with normal background)
        slot.ClearSlot();
        
        // Save immediately after deletion
        SaveBothContainers();
        
    }
    
    /// <summary>
    /// Handle item dropped between slots
    /// </summary>
    private void HandleItemDropped(UnifiedSlot fromSlot, UnifiedSlot toSlot)
    {
        
        if (fromSlot.IsEmpty) return;
        
        // Handle item swap or transfer
        SwapOrTransferItems(fromSlot, toSlot);
        
        // Save immediately after transfer
        SaveBothContainers();
        
        // Update PersistentItemInventoryManager if inventory was modified
        InventoryManager menuInventoryManager = FindMenuInventoryManager();
        if (PersistentItemInventoryManager.Instance != null && menuInventoryManager != null)
        {
            PersistentItemInventoryManager.Instance.UpdateFromInventoryManager(menuInventoryManager);
            PersistentItemInventoryManager.Instance.SaveInventoryData();
        }
    }
    
    /// <summary>
    /// Purchase a stash slot with gems
    /// </summary>
    public void PurchaseSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _slotsPurchased.Length)
        {
            Debug.LogError($"[StashManager] Invalid slot index: {slotIndex}");
            PlaySound(errorSound);
            return;
        }
        
        if (_slotsPurchased[slotIndex])
        {
            Debug.LogWarning($"[StashManager] Slot {slotIndex + 6} is already purchased");
            PlaySound(errorSound);
            return;
        }
        
        // Check if this is the next slot to purchase (only show price for next available slot)
        int nextSlotToPurchase = GetNextSlotToPurchase();
        if (slotIndex != nextSlotToPurchase)
        {
            Debug.LogWarning($"[StashManager] Can only purchase slot {nextSlotToPurchase + 6} next");
            PlaySound(errorSound);
            return;
        }
        
        int cost = _slotCosts[slotIndex];
        
        // ENHANCED: Try to spend gems from inventory first, then from stash if needed
        bool purchaseSuccessful = false;
        int inventoryGems = PlayerProgression.Instance?.currentSpendableGems ?? 0;
        int stashGems = stashGemSlot?.ItemCount ?? 0;
        int totalGems = inventoryGems + stashGems;
        
        Debug.Log($"[StashManager] Purchase attempt: Cost={cost}, InventoryGems={inventoryGems}, StashGems={stashGems}, Total={totalGems}");
        
        if (totalGems >= cost)
        {
            // We have enough gems total, now spend them intelligently
            int remainingCost = cost;
            
            // First, try to spend from inventory
            if (PlayerProgression.Instance != null && inventoryGems > 0)
            {
                int spendFromInventory = Mathf.Min(inventoryGems, remainingCost);
                if (PlayerProgression.Instance.TrySpendGems(spendFromInventory))
                {
                    remainingCost -= spendFromInventory;
                    Debug.Log($"[StashManager] Spent {spendFromInventory} gems from inventory, remaining cost: {remainingCost}");
                }
            }
            
            // If we still need more gems, spend from stash
            if (remainingCost > 0 && stashGems >= remainingCost)
            {
                int newStashGemCount = stashGems - remainingCost;
                stashGemSlot.SetItem(stashGemSlot.CurrentItem, newStashGemCount);
                Debug.Log($"[StashManager] Spent {remainingCost} gems from stash, new stash gems: {newStashGemCount}");
                remainingCost = 0;
            }
            
            // Check if purchase was successful
            if (remainingCost == 0)
            {
                purchaseSuccessful = true;
                _slotsPurchased[slotIndex] = true;
                Debug.Log($"[StashManager] Successfully purchased slot {slotIndex + 6} for {cost} gems (from inventory + stash)");
                
                // Save both stash data and purchased slot data
                SaveBothContainers();
                SavePurchasedSlotData();
                
                // Update UI
                UpdatePurchasableSlotUI();
                
                // Play success sound
                PlaySound(transferSound);
                
                // Show feedback message
                if (DynamicPlayerFeedManager.Instance != null)
                {
                    DynamicPlayerFeedManager.Instance.ShowCustomMessage($"Stash Slot {slotIndex + 6} Purchased!", Color.green, null, false, 2.0f);
                }
            }
        }
        
        if (!purchaseSuccessful)
        {
            Debug.LogWarning($"[StashManager] Not enough gems to purchase slot {slotIndex + 6}. Cost: {cost}, Available: {totalGems}");
            PlaySound(errorSound);
            
            // Show insufficient gems message
            if (DynamicPlayerFeedManager.Instance != null)
            {
                DynamicPlayerFeedManager.Instance.ShowCustomMessage($"Need {cost} gems to purchase slot! (Have {totalGems})", Color.red, null, false, 2.0f);
            }
        }
    }
    
    /// <summary>
    /// Get the next slot index that can be purchased (slots must be purchased in order)
    /// </summary>
    private int GetNextSlotToPurchase()
    {
        for (int i = 0; i < _slotsPurchased.Length; i++)
        {
            if (!_slotsPurchased[i])
            {
                return i;
            }
        }
        return -1; // All slots purchased
    }
    
    /// <summary>
    /// Update the UI for purchasable slots (show/hide locked images, update prices, enable/disable buttons)
    /// </summary>
    private void UpdatePurchasableSlotUI()
    {
        int nextSlotToPurchase = GetNextSlotToPurchase();
        
        for (int i = 0; i < _purchasableStashSlots.Count && i < lockedSlotImages.Length; i++)
        {
            var slot = _purchasableStashSlots[i];
            var lockedImage = lockedSlotImages[i];
            var priceText = i < slotPriceTexts.Length ? slotPriceTexts[i] : null;
            var purchaseButton = i < purchaseButtons.Length ? purchaseButtons[i] : null;
            
            if (slot != null)
            {
                if (_slotsPurchased[i])
                {
                    // Slot is purchased - show as normal slot, hide locked UI
                    slot.gameObject.SetActive(true);
                    if (lockedImage != null) lockedImage.SetActive(false);
                    if (priceText != null) priceText.gameObject.SetActive(false);
                    if (purchaseButton != null) purchaseButton.gameObject.SetActive(false);
                    
                    // CRITICAL: Re-enable ALL raycast components for purchased slots
                    var slotButton = slot.GetComponent<Button>();
                    var slotImage = slot.GetComponent<Image>();
                    var graphicRaycaster = slot.GetComponent<GraphicRaycaster>();
                    
                    // Enable button interaction
                    if (slotButton != null) 
                    {
                        slotButton.interactable = true;
                        Debug.Log($"[StashManager] Enabled slot button interaction for purchased slot {i + 6}");
                        
                        // Re-enable the button's target graphic raycast
                        var buttonImage = slotButton.targetGraphic as Image;
                        if (buttonImage != null)
                        {
                            buttonImage.raycastTarget = true;
                            Debug.Log($"[StashManager] Enabled Button target graphic raycast for purchased slot {i + 6}");
                        }
                    }
                    
                    // Re-enable image raycast
                    if (slotImage != null)
                    {
                        slotImage.raycastTarget = true;
                        Debug.Log($"[StashManager] Enabled Image raycast target for purchased slot {i + 6}");
                    }
                    
                    // Re-enable graphic raycaster if present
                    if (graphicRaycaster != null)
                    {
                        graphicRaycaster.enabled = true;
                        Debug.Log($"[StashManager] Enabled GraphicRaycaster for purchased slot {i + 6}");
                    }
                }
                else
                {
                    // Slot is not purchased - show slot with locked appearance, show locked UI
                    slot.gameObject.SetActive(true); // Keep slot visible for background/border
                    if (lockedImage != null) 
                    {
                        lockedImage.SetActive(true); // Show lock overlay
                        
                        // Disable raycast blocking on lock image so buttons underneath can be clicked
                        var lockImageComponent = lockedImage.GetComponent<Image>();
                        if (lockImageComponent != null)
                        {
                            lockImageComponent.raycastTarget = false;
                        }
                    }
                    
                    // CRITICAL: Disable ALL raycast components on locked slot so clicks pass through to purchase button
                    var slotButton = slot.GetComponent<Button>();
                    var slotImage = slot.GetComponent<Image>();
                    var graphicRaycaster = slot.GetComponent<GraphicRaycaster>();
                    
                    // Disable button interaction
                    if (slotButton != null) 
                    {
                        slotButton.interactable = false;
                        Debug.Log($"[StashManager] Disabled slot button interaction for locked slot {i + 6}");
                        
                        // Also disable the button's target graphic raycast
                        var buttonImage = slotButton.targetGraphic as Image;
                        if (buttonImage != null)
                        {
                            buttonImage.raycastTarget = false;
                            Debug.Log($"[StashManager] Disabled Button target graphic raycast for locked slot {i + 6}");
                        }
                    }
                    
                    // Disable image raycast
                    if (slotImage != null)
                    {
                        slotImage.raycastTarget = false;
                        Debug.Log($"[StashManager] Disabled Image raycast target for locked slot {i + 6}");
                    }
                    
                    // Disable graphic raycaster if present
                    if (graphicRaycaster != null)
                    {
                        graphicRaycaster.enabled = false;
                        Debug.Log($"[StashManager] Disabled GraphicRaycaster for locked slot {i + 6}");
                    }
                    
                    // Also disable drag/drop functionality for unpurchased slots
                    var unifiedSlot = slot.GetComponent<UnifiedSlot>();
                    if (unifiedSlot != null)
                    {
                        // The UnifiedSlot will check CanAcceptItem() which now validates purchase status
                        Debug.Log($"[StashManager] Slot {i + 6} is LOCKED - drag/drop will be rejected by UnifiedSlot.CanAcceptItem()");
                    }
                    
                    // Only show price and button for the next slot to purchase
                    if (i == nextSlotToPurchase)
                    {
                        if (priceText != null)
                        {
                            priceText.text = $"{_slotCosts[i]} gems";
                            priceText.gameObject.SetActive(true);
                        }
                        if (purchaseButton != null)
                        {
                            purchaseButton.gameObject.SetActive(true);
                            purchaseButton.interactable = PlayerProgression.Instance != null && 
                                                         PlayerProgression.Instance.currentSpendableGems >= _slotCosts[i];
                            
                            // Ensure button can receive raycasts and is on top
                            var buttonImage = purchaseButton.GetComponent<Image>();
                            if (buttonImage != null)
                            {
                                buttonImage.raycastTarget = true;
                            }
                            
                            // CRITICAL: Ensure purchase button is on top of UI hierarchy to receive clicks
                            var buttonTransform = purchaseButton.transform as RectTransform;
                            if (buttonTransform != null)
                            {
                                buttonTransform.SetAsLastSibling(); // Move to front of UI hierarchy
                                Debug.Log($"[StashManager] Moved purchase button for slot {i + 6} to front of UI hierarchy");
                            }
                            
                            // Debug button setup
                            Debug.Log($"[StashManager] Purchase button for slot {i + 6}: Active={purchaseButton.gameObject.activeSelf}, Interactable={purchaseButton.interactable}, RaycastTarget={buttonImage?.raycastTarget}");
                        }
                    }
                    else
                    {
                        // Hide price and button for slots that aren't next
                        if (priceText != null) priceText.gameObject.SetActive(false);
                        if (purchaseButton != null) purchaseButton.gameObject.SetActive(false);
                    }
                }
            }
        }
        
        Debug.Log($"[StashManager] Updated purchasable slot UI. Next slot to purchase: {(nextSlotToPurchase >= 0 ? (nextSlotToPurchase + 6).ToString() : "All purchased")}");
        
        // Force canvas update to ensure raycast changes take effect immediately
        Canvas.ForceUpdateCanvases();
    }
    
    /// <summary>
    /// Get list of purchased stash slots that are currently available for use
    /// </summary>
    private List<UnifiedSlot> GetPurchasedSlots()
    {
        List<UnifiedSlot> purchasedSlots = new List<UnifiedSlot>();
        
        for (int i = 0; i < _purchasableStashSlots.Count && i < _slotsPurchased.Length; i++)
        {
            if (_slotsPurchased[i] && _purchasableStashSlots[i] != null)
            {
                purchasedSlots.Add(_purchasableStashSlots[i]);
            }
        }
        
        return purchasedSlots;
    }
    
    /// <summary>
    /// PUBLIC METHOD: Check if a specific purchasable stash slot is purchased and available for use
    /// This is called by UnifiedSlot to validate item placement in purchasable slots
    /// </summary>
    public bool IsSlotPurchased(UnifiedSlot slot)
    {
        if (slot == null) return false;
        
        // Check if this slot is one of our purchasable slots
        for (int i = 0; i < _purchasableStashSlots.Count; i++)
        {
            if (_purchasableStashSlots[i] == slot)
            {
                bool isPurchased = i < _slotsPurchased.Length && _slotsPurchased[i];
                Debug.Log($"[StashManager] Slot {slot.gameObject.name} (index {i+6}) purchase status: {(isPurchased ? "PURCHASED" : "LOCKED")}");
                return isPurchased;
            }
        }
        
        // If it's not a purchasable slot, it's always "available" (regular slots, gem slots, etc.)
        return true;
    }
    
    /// <summary>
    /// PUBLIC METHOD: Check if a slot is a purchasable stash slot (regardless of purchase status)
    /// </summary>
    public bool IsPurchasableSlot(UnifiedSlot slot)
    {
        if (slot == null) return false;
        return _purchasableStashSlots.Contains(slot);
    }
    
    /// <summary>
    /// PUBLIC METHOD: Trigger slot purchase when a locked slot is clicked directly
    /// This is called by UnifiedSlot when a locked purchasable slot is clicked
    /// </summary>
    public void TriggerSlotPurchaseFromClick(UnifiedSlot slot)
    {
        if (slot == null) return;
        
        // Find the slot index in our purchasable slots array
        for (int i = 0; i < _purchasableStashSlots.Count; i++)
        {
            if (_purchasableStashSlots[i] == slot)
            {
                Debug.Log($"[StashManager] 🎯 DIRECT PURCHASE TRIGGERED: Slot {i + 6} clicked directly!");
                
                // Check if this is the next slot that can be purchased (slots must be purchased in order)
                int nextSlotToPurchase = GetNextSlotToPurchase();
                if (i != nextSlotToPurchase)
                {
                    Debug.LogWarning($"[StashManager] ⚠️ Cannot purchase slot {i + 6} - must purchase slot {nextSlotToPurchase + 6} first!");
                    PlaySound(errorSound);
                    
                    // Show feedback message
                    if (DynamicPlayerFeedManager.Instance != null)
                    {
                        DynamicPlayerFeedManager.Instance.ShowCustomMessage($"Must purchase slots in order! Buy slot {nextSlotToPurchase + 6} first.", Color.red, null, false, 3.0f);
                    }
                    return;
                }
                
                // Trigger the purchase using existing logic
                PurchaseSlot(i);
                return;
            }
        }
        
        Debug.LogError($"[StashManager] Could not find slot {slot.gameObject.name} in purchasable slots array!");
    }
    
    /// <summary>
    /// Save purchased slot data to PlayerPrefs
    /// </summary>
    private void SavePurchasedSlotData()
    {
        try
        {
            for (int i = 0; i < _slotsPurchased.Length; i++)
            {
                PlayerPrefs.SetInt($"StashSlot_{i + 6}_Purchased", _slotsPurchased[i] ? 1 : 0);
            }
            PlayerPrefs.Save();
            Debug.Log("[StashManager] Saved purchased slot data to PlayerPrefs");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[StashManager] Error saving purchased slot data: {e.Message}");
        }
    }
    
    /// <summary>
    /// Load purchased slot data from PlayerPrefs
    /// </summary>
    private void LoadPurchasedSlotData()
    {
        try
        {
            for (int i = 0; i < _slotsPurchased.Length; i++)
            {
                _slotsPurchased[i] = PlayerPrefs.GetInt($"StashSlot_{i + 6}_Purchased", 0) == 1;
            }
            
            int purchasedCount = 0;
            for (int i = 0; i < _slotsPurchased.Length; i++)
            {
                if (_slotsPurchased[i]) purchasedCount++;
            }
            
            Debug.Log($"[StashManager] Loaded purchased slot data from PlayerPrefs. {purchasedCount} slots purchased.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[StashManager] Error loading purchased slot data: {e.Message}");
        }
    }
    
    // REMOVED: AutoTransferGems() method - no longer needed for manual-only transfers
    
    // REMOVED: CalculateSmoothTransferDelay() method - no longer needed for manual-only transfers
    
    /// <summary>
    /// Create a flying gem visual effect using existing dragImage canvas - PERFECTLY SYNCHRONIZED!
    /// Uses the actual inventory gem slot image for authentic visual feedback
    /// </summary>
    private void CreateFlyingGemEffect(GemItemData gemItem)
    {
        // Skip if disabled or slots not available
        if (!enableFlyingGemEffect || inventoryGemSlot == null || stashGemSlot == null || gemItem == null)
        {
            return;
        }
        
        // Find existing dragImage canvas (same system UnifiedSlot uses)
        Canvas dragCanvas = FindDragImageCanvas();
        if (dragCanvas == null)
        {
            return;
        }
        
        // Create flying gem using the actual gem sprite from inventory slot
        GameObject flyingGem = new GameObject("FlyingGem");
        flyingGem.transform.SetParent(dragCanvas.transform, false);
        
        Image flyingGemImage = flyingGem.AddComponent<Image>();
        flyingGemImage.sprite = gemItem.itemIcon;
        flyingGemImage.raycastTarget = false;
        
        RectTransform flyingGemRect = flyingGem.GetComponent<RectTransform>();
        flyingGemRect.sizeDelta = new Vector2(32, 32); // Match typical gem size
        
        // Get screen positions of source and target slots
        Vector3 startScreenPos = RectTransformUtility.WorldToScreenPoint(null, inventoryGemSlot.transform.position);
        Vector3 endScreenPos = RectTransformUtility.WorldToScreenPoint(null, stashGemSlot.transform.position);
        
        // Convert to canvas local positions
        Vector2 startLocalPos, endLocalPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            dragCanvas.GetComponent<RectTransform>(), startScreenPos, null, out startLocalPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            dragCanvas.GetComponent<RectTransform>(), endScreenPos, null, out endLocalPos);
        
        // Set initial position
        flyingGemRect.anchoredPosition = startLocalPos;
        
        // Start PERFECTLY SYNCHRONIZED flying animation
        StartCoroutine(AnimateFlyingGem(flyingGemRect, startLocalPos, endLocalPos, flyingGem));
    }
    
    /// <summary>
    /// Animate flying gem with perfect timing synchronization
    /// </summary>
    private IEnumerator AnimateFlyingGem(RectTransform flyingGemRect, Vector2 startPos, Vector2 endPos, GameObject flyingGem)
    {
        float elapsedTime = 0f;
        float duration = gemFlightDuration;
        
        while (elapsedTime < duration)
        {
            float progress = elapsedTime / duration;
            
            // Smooth easing
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            
            // Simple linear interpolation for perfect sync
            Vector2 currentPos = Vector2.Lerp(startPos, endPos, easedProgress);
            flyingGemRect.anchoredPosition = currentPos;
            
            // Subtle rotation for visual appeal
            flyingGemRect.rotation = Quaternion.Euler(0, 0, progress * 180f);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Ensure final position
        flyingGemRect.anchoredPosition = endPos;
        
        // Quick fade out and cleanup
        StartCoroutine(FadeOutAndDestroy(flyingGem));
    }
    
    /// <summary>
    /// Quick fade out effect and cleanup
    /// </summary>
    private IEnumerator FadeOutAndDestroy(GameObject flyingGem)
    {
        Image gemImage = flyingGem.GetComponent<Image>();
        Color originalColor = gemImage.color;
        
        float fadeTime = 0.1f;
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeTime)
        {
            float progress = elapsedTime / fadeTime;
            Color fadeColor = originalColor;
            fadeColor.a = Mathf.Lerp(1f, 0f, progress);
            gemImage.color = fadeColor;
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        Destroy(flyingGem);
    }
    
    /// <summary>
    /// Find the dragImage canvas (same method as UnifiedSlot uses)
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
                return canvas;
            }
        }
        
        // Method 2: Find any canvas with very high sorting order (dragImage typically has 9999)
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        Canvas highestCanvas = null;
        int highestOrder = -1;
        
        foreach (Canvas canvas in allCanvases)
        {
            if (canvas.sortingOrder > highestOrder)
            {
                highestOrder = canvas.sortingOrder;
                highestCanvas = canvas;
            }
        }
        
        return highestCanvas;
    }
    
    /// <summary>
    /// Transfer ALL gems from stash gem slot to inventory gem slot (double-click on stash gem slot)
    /// </summary>
    private void TransferGemsToInventory()
    {
        if (stashGemSlot == null || inventoryGemSlot == null)
        {
            Debug.LogError("[StashManager] Cannot transfer gems - gem slots not assigned");
            PlaySound(errorSound);
            return;
        }
        
        if (stashGemSlot.IsEmpty)
        {
            Debug.Log("[StashManager] No gems in stash to transfer");
            PlaySound(errorSound);
            return;
        }
        
        // Check if stash contains gems
        if (!(stashGemSlot.CurrentItem is GemItemData))
        {
            Debug.LogError("[StashManager] Stash gem slot does not contain gems");
            PlaySound(errorSound);
            return;
        }
        
        GemItemData gemItem = stashGemSlot.CurrentItem as GemItemData;
        int stashGemCount = stashGemSlot.ItemCount;
        
        // Check if inventory gem slot can accept gems
        if (!inventoryGemSlot.IsEmpty && !(inventoryGemSlot.CurrentItem is GemItemData))
        {
            Debug.LogError("[StashManager] Inventory gem slot is occupied by non-gem item");
            PlaySound(errorSound);
            return;
        }
        
        // Calculate new counts
        int inventoryGemCount = inventoryGemSlot.IsEmpty ? 0 : inventoryGemSlot.ItemCount;
        int newInventoryTotal = inventoryGemCount + stashGemCount;
        
        // Transfer all gems
        inventoryGemSlot.SetItem(gemItem, newInventoryTotal);
        stashGemSlot.ClearSlot();
        
        // Update InventoryManager gem count
        InventoryManager menuInventoryManager = FindMenuInventoryManager();
        if (menuInventoryManager != null)
        {
            menuInventoryManager.currentGemCount = newInventoryTotal;
            menuInventoryManager.UpdateGemDisplay();
        }
        
        PlaySound(gemTransferSound);
        Debug.Log($"[StashManager] Transferred {stashGemCount} gems from stash to inventory. New inventory total: {newInventoryTotal}");
        
        // CRITICAL: Save both containers after gem transfer
        SaveBothContainers();
        
        // Update PersistentItemInventoryManager after gem transfer
        if (PersistentItemInventoryManager.Instance != null && menuInventoryManager != null)
        {
            PersistentItemInventoryManager.Instance.UpdateFromInventoryManager(menuInventoryManager);
            PersistentItemInventoryManager.Instance.SaveInventoryData();
        }
    }
    
    /// <summary>
    /// Transfer ALL gems from inventory gem slot to stash gem slot (double-click on inventory gem slot)
    /// </summary>
    private void TransferGemsToStash()
    {
        Debug.Log("[StashManager] TransferGemsToStash() called");
        
        if (stashGemSlot == null || inventoryGemSlot == null)
        {
            Debug.LogError("[StashManager] Cannot transfer gems - gem slots not assigned");
            Debug.LogError($"[StashManager] stashGemSlot: {(stashGemSlot != null ? "OK" : "NULL")}, inventoryGemSlot: {(inventoryGemSlot != null ? "OK" : "NULL")}");
            PlaySound(errorSound);
            return;
        }
        
        Debug.Log($"[StashManager] Inventory gem slot - IsEmpty: {inventoryGemSlot.IsEmpty}, ItemCount: {inventoryGemSlot.ItemCount}");
        Debug.Log($"[StashManager] Inventory gem slot item type: {inventoryGemSlot.CurrentItem?.GetType()?.Name ?? "NULL"}");
        
        if (inventoryGemSlot.IsEmpty)
        {
            Debug.Log("[StashManager] No gems in inventory to transfer - inventory gem slot is empty");
            PlaySound(errorSound);
            return;
        }
        
        // Check if inventory contains gems
        if (!(inventoryGemSlot.CurrentItem is GemItemData))
        {
            Debug.LogError("[StashManager] Inventory gem slot does not contain gems");
            PlaySound(errorSound);
            return;
        }
        
        GemItemData gemItem = inventoryGemSlot.CurrentItem as GemItemData;
        int inventoryGemCount = inventoryGemSlot.ItemCount;
        
        // Check if stash gem slot can accept gems
        if (!stashGemSlot.IsEmpty && !(stashGemSlot.CurrentItem is GemItemData))
        {
            Debug.LogError("[StashManager] Stash gem slot is occupied by non-gem item");
            PlaySound(errorSound);
            return;
        }
        
        // Calculate new counts
        int stashGemCount = stashGemSlot.IsEmpty ? 0 : stashGemSlot.ItemCount;
        int newStashTotal = stashGemCount + inventoryGemCount;
        
        // Transfer all gems
        stashGemSlot.SetItem(gemItem, newStashTotal);
        inventoryGemSlot.ClearSlot();
        
        // Update InventoryManager gem count
        InventoryManager menuInventoryManager = FindMenuInventoryManager();
        if (menuInventoryManager != null)
        {
            menuInventoryManager.currentGemCount = 0;
            menuInventoryManager.UpdateGemDisplay();
        }
        
        PlaySound(gemTransferSound);
        Debug.Log($"[StashManager] Transferred {inventoryGemCount} gems from inventory to stash. New stash total: {newStashTotal}");
        
        // CRITICAL: Save both containers after gem transfer
        SaveBothContainers();
        
        // Update PersistentItemInventoryManager after gem transfer
        if (PersistentItemInventoryManager.Instance != null && menuInventoryManager != null)
        {
            PersistentItemInventoryManager.Instance.UpdateFromInventoryManager(menuInventoryManager);
            PersistentItemInventoryManager.Instance.SaveInventoryData();
        }
    }
    
    /// <summary>
    /// SMART Transfer item from stash to inventory with intelligent stacking
    /// </summary>
    private void SmartTransferToInventory(UnifiedSlot stashSlot)
    {
        
        // Step 1: Try to stack with existing identical items in inventory
        UnifiedSlot stackableSlot = FindStackableSlot(_inventorySlots, stashSlot.CurrentItem);
        if (stackableSlot != null)
        {
            
            // Capture item name BEFORE clearing slot
            string itemName = stashSlot.CurrentItem?.itemName ?? "Unknown Item";
            
            // Stack the items together
            int newCount = stackableSlot.ItemCount + stashSlot.ItemCount;
            stackableSlot.SetItem(stackableSlot.CurrentItem, newCount);
            stashSlot.ClearSlot();
            
            PlaySound(transferSound);
            
            // Force visual update to ensure count displays properly
            stackableSlot.UpdateVisuals();
            return;
        }
        
        // Step 2: If no stackable slot found, transfer to empty slot
        UnifiedSlot emptyInventorySlot = FindEmptySlot(_inventorySlots);
        if (emptyInventorySlot != null)
        {
            // Capture item name before clearing slot to avoid null reference
            string itemName = stashSlot.CurrentItem?.itemName ?? "Unknown Item";
            
            // Move item
            emptyInventorySlot.SetItem(stashSlot.CurrentItem, stashSlot.ItemCount);
            stashSlot.ClearSlot();
            
            PlaySound(transferSound);
        }
        else
        {
            PlaySound(errorSound);
        }
    }
    
    /// <summary>
    /// SMART Transfer item from inventory to stash with intelligent stacking
    /// </summary>
    private void SmartTransferToStash(UnifiedSlot inventorySlot)
    {
        
        // Step 1: Try to stack with existing identical items in regular stash slots
        UnifiedSlot stackableSlot = FindStackableSlot(_stashSlots, inventorySlot.CurrentItem);
        if (stackableSlot != null)
        {
            
            // Capture item name BEFORE clearing slot
            string itemName = inventorySlot.CurrentItem?.itemName ?? "Unknown Item";
            
            // Stack the items together
            int newCount = stackableSlot.ItemCount + inventorySlot.ItemCount;
            stackableSlot.SetItem(stackableSlot.CurrentItem, newCount);
            inventorySlot.ClearSlot();
            
            PlaySound(transferSound);
            
            // Force visual update to ensure count displays properly
            stackableSlot.UpdateVisuals();
            return;
        }
        
        // Step 1.5: Try to stack with existing identical items in purchased stash slots
        List<UnifiedSlot> purchasedSlots = GetPurchasedSlots();
        stackableSlot = FindStackableSlot(purchasedSlots, inventorySlot.CurrentItem);
        if (stackableSlot != null)
        {
            
            // Capture item name BEFORE clearing slot
            string itemName = inventorySlot.CurrentItem?.itemName ?? "Unknown Item";
            
            // Stack the items together
            int newCount = stackableSlot.ItemCount + inventorySlot.ItemCount;
            stackableSlot.SetItem(stackableSlot.CurrentItem, newCount);
            inventorySlot.ClearSlot();
            
            PlaySound(transferSound);
            
            // Force visual update to ensure count displays properly
            stackableSlot.UpdateVisuals();
            return;
        }
        
        // Step 2: If no stackable slot found, transfer to empty regular stash slot
        UnifiedSlot emptyStashSlot = FindEmptySlot(_stashSlots);
        if (emptyStashSlot != null)
        {
            // Capture item name before clearing slot to avoid null reference
            string itemName = inventorySlot.CurrentItem?.itemName ?? "Unknown Item";
            
            // Move item
            emptyStashSlot.SetItem(inventorySlot.CurrentItem, inventorySlot.ItemCount);
            inventorySlot.ClearSlot();
            
            PlaySound(transferSound);
            return;
        }
        
        // Step 3: If no empty regular slot, try empty purchased slots
        emptyStashSlot = FindEmptySlot(purchasedSlots);
        if (emptyStashSlot != null)
        {
            // Capture item name before clearing slot to avoid null reference
            string itemName = inventorySlot.CurrentItem?.itemName ?? "Unknown Item";
            
            // Move item
            emptyStashSlot.SetItem(inventorySlot.CurrentItem, inventorySlot.ItemCount);
            inventorySlot.ClearSlot();
            
            PlaySound(transferSound);
        }
        else
        {
            PlaySound(errorSound);
        }
    }
    
    /// <summary>
    /// Transfer item from stash to inventory (legacy method for drag-drop)
    /// </summary>
    private void TransferToInventory(UnifiedSlot stashSlot)
    {
        UnifiedSlot emptyInventorySlot = FindEmptySlot(_inventorySlots);
        if (emptyInventorySlot != null)
        {
            // Capture item name before clearing slot to avoid null reference
            string itemName = stashSlot.CurrentItem?.itemName ?? "Unknown Item";
            
            // Move item
            emptyInventorySlot.SetItem(stashSlot.CurrentItem, stashSlot.ItemCount);
            stashSlot.ClearSlot();
            
            PlaySound(transferSound);
            
            // Save immediately after transfer
            SaveBothContainers();
        }
        else
        {
            PlaySound(errorSound);
        }
    }
    
    /// <summary>
    /// Transfer item from inventory to stash
    /// </summary>
    private void TransferToStash(UnifiedSlot inventorySlot)
    {
        UnifiedSlot emptyStashSlot = FindEmptySlot(_stashSlots);
        if (emptyStashSlot != null)
        {
            // Capture item name before clearing slot to avoid null reference
            string itemName = inventorySlot.CurrentItem?.itemName ?? "Unknown Item";
            
            // Move item
            emptyStashSlot.SetItem(inventorySlot.CurrentItem, inventorySlot.ItemCount);
            inventorySlot.ClearSlot();
            
            PlaySound(transferSound);
            
            // Save immediately after transfer
            SaveBothContainers();
        }
        else
        {
            PlaySound(errorSound);
        }
    }
    
    /// <summary>
    /// Swap or transfer items between slots with UNIFIED STACKING support
    /// </summary>
    private void SwapOrTransferItems(UnifiedSlot fromSlot, UnifiedSlot toSlot)
    {
        ChestItemData fromItem = fromSlot.CurrentItem;
        int fromCount = fromSlot.ItemCount;
        
        if (toSlot.IsEmpty)
        {
            // Simple transfer to empty slot
            toSlot.SetItem(fromItem, fromCount);
            fromSlot.ClearSlot();
            Debug.Log($"[StashManager] 🔄 Transferred {fromCount}x {fromItem.itemName} to empty slot");
        }
        else if (AreItemsStackable(fromItem, toSlot.CurrentItem))
        {
            // UNIFIED STACKING: Stack identical items together
            int combinedCount = toSlot.ItemCount + fromCount;
            toSlot.SetItem(toSlot.CurrentItem, combinedCount);
            fromSlot.ClearSlot();
            Debug.Log($"[StashManager] ✅ UNIFIED STACKING: Stacked {fromCount}x {fromItem.itemName} (total: {combinedCount})");
        }
        else
        {
            // Swap different items
            var tempItem = toSlot.CurrentItem;
            var tempCount = toSlot.ItemCount;
            
            toSlot.SetItem(fromItem, fromCount);
            fromSlot.SetItem(tempItem, tempCount);
            Debug.Log($"[StashManager] 🔄 Swapped {fromItem.itemName} with {tempItem.itemName}");
        }
        
        PlaySound(transferSound);
    }
    
    /// <summary>
    /// Find first empty slot in the given collection
    /// </summary>
    private UnifiedSlot FindEmptySlot(List<UnifiedSlot> slots)
    {
        foreach (var slot in slots)
        {
            if (slot.IsEmpty) return slot;
        }
        return null;
    }
    
    /// <summary>
    /// Find slot containing stackable items (identical items that can be combined)
    /// ROBUST COMPARISON: Ignores unique IDs and focuses on core item properties
    /// </summary>
    private UnifiedSlot FindStackableSlot(List<UnifiedSlot> slots, ChestItemData targetItem)
    {
        if (targetItem == null) 
        {
            return null;
        }
        
        
        foreach (var slot in slots)
        {
            if (slot.IsEmpty) continue;
            
            if (AreItemsStackable(slot.CurrentItem, targetItem))
            {
                return slot;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// UNIFIED item comparison for stacking - uses ChestItemData.IsSameItem()
    /// This ensures consistent stacking behavior across ALL systems (chest, inventory, stash)
    /// </summary>
    private bool AreItemsStackable(ChestItemData item1, ChestItemData item2)
    {
        if (item1 == null || item2 == null)
        {
            return false;
        }
        
        // UNIFIED STACKING: Use IsSameItem() for consistency with chest/inventory system
        bool areStackable = item1.IsSameItem(item2);
        
        Debug.Log($"[StashManager] UNIFIED STACKING: {item1.itemName} + {item2.itemName} = {areStackable}");
        Debug.Log($"   Item1 ID: {item1.itemID}, Item2 ID: {item2.itemID}");
        
        return areStackable;
    }
    
    /// <summary>
    /// Save both stash and inventory data
    /// </summary>
    private void SaveBothContainers()
    {
        SaveStashData();
        
        // Find and use MENU context InventoryManager for consistency
        InventoryManager menuInventoryManager = FindMenuInventoryManager();
        if (menuInventoryManager != null)
        {
            menuInventoryManager.SaveInventoryData();
        }
        else if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.SaveInventoryData();
        }
    }
    
    /// <summary>
    /// Load stash data from file
    /// </summary>
    private void LoadStashData()
    {
        try
        {
            Debug.Log($"[StashManager] Loading stash data from: {_stashSavePath}");
            
            if (File.Exists(_stashSavePath))
            {
                string json = File.ReadAllText(_stashSavePath);
                StashSaveData data = JsonUtility.FromJson<StashSaveData>(json);
                
                Debug.Log($"[StashManager] Loaded stash data - gemCount: {data.gemCount}, slots: {data.slots?.Count ?? 0}");
                
                if (data.slots != null)
                {
                    Debug.Log($"[StashManager] Loading {data.slots.Count} regular stash slots after clearing");
                    for (int i = 0; i < _stashSlots.Count && i < data.slots.Count; i++)
                    {
                        if (_stashSlots[i] != null && data.slots[i] != null && !string.IsNullOrEmpty(data.slots[i].itemPath))
                        {
                            LoadSlotFromData(_stashSlots[i], data.slots[i]);
                            Debug.Log($"[StashManager] Loaded item into regular stash slot {i}");
                        }
                    }
                }
                
                // Load purchasable slot data (only for purchased slots)
                if (data.purchasableSlots != null)
                {
                    Debug.Log($"[StashManager] Loading {data.purchasableSlots.Count} purchasable stash slots");
                    for (int i = 0; i < _purchasableStashSlots.Count && i < data.purchasableSlots.Count; i++)
                    {
                        if (_slotsPurchased[i] && _purchasableStashSlots[i] != null && data.purchasableSlots[i] != null && !string.IsNullOrEmpty(data.purchasableSlots[i].itemPath))
                        {
                            LoadSlotFromData(_purchasableStashSlots[i], data.purchasableSlots[i]);
                            Debug.Log($"[StashManager] Loaded item into purchasable stash slot {i + 6}");
                        }
                    }
                }
                
                // Load stash gem count and apply to stashGemSlot
                if (stashGemSlot != null && data.gemCount > 0)
                {
                    // Find a gem item data to use (we need this for UnifiedSlot.SetItem)
                    ChestItemData gemItemData = FindGemItemData();
                    if (gemItemData != null)
                    {
                        stashGemSlot.SetItem(gemItemData, data.gemCount);
                        Debug.Log($"[StashManager] Set stash gem slot to {data.gemCount} gems");
                    }
                    else
                    {
                        Debug.LogError("[StashManager] Could not find GemItemData to load stash gems");
                    }
                }
                else if (stashGemSlot != null && data.gemCount == 0)
                {
                    stashGemSlot.ClearSlot();
                    Debug.Log("[StashManager] Cleared stash gem slot (0 gems saved)");
                }
                
            }
            else
            {
                Debug.Log("[StashManager] No stash save file found - starting with empty stash");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[StashManager] Error loading stash data: {e.Message}");
        }
    }
    
    /// <summary>
    /// Save stash data to file
    /// </summary>
    private void SaveStashData()
    {
        try
        {
            StashSaveData data = new StashSaveData();
            
            // Save stash gem count from stashGemSlot
            if (stashGemSlot != null && !stashGemSlot.IsEmpty)
            {
                data.gemCount = stashGemSlot.ItemCount;
            }
            else
            {
                data.gemCount = 0;
            }
            
            // Save items (gems save naturally through slot system)
            data.slots = new List<SlotData>();
            Debug.Log($"[StashManager] Checking {_stashSlots.Count} regular stash slots for items to save (gem slot handled separately)...");
            
            for (int i = 0; i < _stashSlots.Count; i++)
            {
                var slot = _stashSlots[i];
                if (slot != null)
                {
                    SlotData slotData = GetSlotData(slot);
                    data.slots.Add(slotData);
                    
                    // Debug log each slot being saved
                    if (!slot.IsEmpty)
                    {
                        Debug.Log($"[StashManager] Saving regular stash slot {i+1}: {slotData.itemName} x{slotData.itemCount} (path: {slotData.itemPath})");
                    }
                    else
                    {
                        Debug.Log($"[StashManager] Regular stash slot {i+1} is EMPTY - nothing to save");
                    }
                }
                else
                {
                    Debug.LogWarning($"[StashManager] Regular stash slot {i+1} is NULL!");
                    data.slots.Add(new SlotData()); // Empty slot
                }
            }
            
            // Save purchasable slot data
            data.purchasableSlots = new List<SlotData>();
            Debug.Log($"[StashManager] Checking {_purchasableStashSlots.Count} purchasable stash slots for items to save...");
            
            for (int i = 0; i < _purchasableStashSlots.Count; i++)
            {
                var slot = _purchasableStashSlots[i];
                if (slot != null)
                {
                    SlotData slotData = GetSlotData(slot);
                    data.purchasableSlots.Add(slotData);
                    
                    // Debug log each purchasable slot being saved
                    if (!slot.IsEmpty && _slotsPurchased[i])
                    {
                        Debug.Log($"[StashManager] Saving purchasable stash slot {i+6}: {slotData.itemName} x{slotData.itemCount} (path: {slotData.itemPath})");
                    }
                    else if (_slotsPurchased[i])
                    {
                        Debug.Log($"[StashManager] Purchasable stash slot {i+6} is EMPTY - nothing to save");
                    }
                    else
                    {
                        Debug.Log($"[StashManager] Purchasable stash slot {i+6} is NOT PURCHASED - saving empty data");
                    }
                }
                else
                {
                    Debug.LogWarning($"[StashManager] Purchasable stash slot {i+6} is NULL!");
                    data.purchasableSlots.Add(new SlotData()); // Empty slot
                }
            }
            
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(_stashSavePath, json);
            Debug.Log($"[StashManager] Saved stash data to {_stashSavePath} - {data.slots.Count} regular slots, {data.purchasableSlots.Count} purchasable slots, {data.gemCount} gems");
            
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[StashManager] Error saving stash data: {e.Message}");
        }
    }
    
    /// <summary>
    /// Load slot data into a UnifiedSlot - ENHANCED with fallback path attempts including keycards
    /// </summary>
    private void LoadSlotFromData(UnifiedSlot slot, SlotData data)
    {
        try
        {
            if (!string.IsNullOrEmpty(data.itemPath))
            {
                Debug.Log($"[StashManager] Attempting to load item '{data.itemName}' from path '{data.itemPath}'");
                
                ChestItemData item = Resources.Load<ChestItemData>(data.itemPath);
                if (item != null)
                {
                    slot.SetItem(item, data.itemCount);
                    Debug.Log($"[StashManager] Successfully loaded {data.itemName} x{data.itemCount} into slot");
                    return;
                }
                
                // Primary path failed, try alternative paths
                Debug.LogWarning($"[StashManager] Primary path '{data.itemPath}' failed, trying alternatives...");
                
                // KEYCARD FIX: Check if this might be a keycard
                bool isKeycard = data.itemName.ToLower().Contains("keycard");
                
                string[] alternativePaths;
                if (isKeycard)
                {
                    // Try keycard-specific paths first
                    alternativePaths = new string[] {
                        $"Keycards/{data.itemName}",
                        $"Keycards/{data.itemName.Replace(" ", "")}",
                        $"Items/Keycards/{data.itemName}",
                        $"Items/Keycards/{data.itemName.Replace(" ", "")}",
                        $"Items/OLDITEMS/{data.itemName}",
                        $"Items/OLDITEMS/{data.itemName.Replace(" ", "")}",
                        $"Items/{data.itemName}",
                        $"Items/{data.itemName.Replace(" ", "")}",
                        $"ChestItems/{data.itemName}",
                        $"ChestItems/{data.itemName.Replace(" ", "")}",
                        data.itemName,
                        data.itemName.Replace(" ", "")
                    };
                }
                else
                {
                    alternativePaths = new string[] {
                        $"Items/OLDITEMS/{data.itemName}",
                        $"Items/OLDITEMS/{data.itemName.Replace(" ", "")}",
                        $"Items/{data.itemName}",
                        $"Items/{data.itemName.Replace(" ", "")}",
                        $"ChestItems/{data.itemName}",
                        $"ChestItems/{data.itemName.Replace(" ", "")}",
                        data.itemName,
                        data.itemName.Replace(" ", "")
                    };
                }
                
                foreach (string altPath in alternativePaths)
                {
                    ChestItemData altItem = Resources.Load<ChestItemData>(altPath);
                    if (altItem != null)
                    {
                        slot.SetItem(altItem, data.itemCount);
                        Debug.Log($"[StashManager] Loaded {data.itemName} x{data.itemCount} using alternative path: '{altPath}'");
                        return;
                    }
                }
                
                if (isKeycard)
                {
                    Debug.LogError($"[StashManager] ⚠️ KEYCARD '{data.itemName}' could not be loaded! Keycards must be in 'Assets/Resources/Keycards/' folder to persist.");
                }
                
                Debug.LogError($"[StashManager] Could not load item '{data.itemName}' from any path. Tried: {data.itemPath}, {string.Join(", ", alternativePaths)}");
                slot.ClearSlot();
            }
            else
            {
                Debug.Log($"[StashManager] Empty item path for slot - clearing");
                slot.ClearSlot();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[StashManager] Exception loading slot data for '{data.itemName}': {e.Message}\n{e.StackTrace}");
            slot.ClearSlot();
        }
    }
    
    /// <summary>
    /// Get slot data from a UnifiedSlot
    /// </summary>
    private SlotData GetSlotData(UnifiedSlot slot)
    {
        SlotData data = new SlotData();
        
        if (slot.IsEmpty)
        {
            data.itemName = "";
            data.itemPath = "";
            data.itemCount = 0;
        }
        else
        {
            data.itemName = slot.CurrentItem.itemName;
            data.itemPath = GetItemResourcePath(slot.CurrentItem);
            data.itemCount = slot.ItemCount;
        }
        
        return data;
    }
    
    /// <summary>
    /// Get the resource path for an item - ENHANCED with multiple path attempts including keycards
    /// </summary>
    private string GetItemResourcePath(ChestItemData item)
    {
        if (item == null) return "";
        
        // Try multiple common path patterns
        string itemName = item.name;
        
        // Remove common Unity suffixes like " (Clone)"
        if (itemName.Contains("("))
        {
            itemName = itemName.Substring(0, itemName.IndexOf("(")).Trim();
        }
        
        Debug.Log($"[StashManager] Getting resource path for item: '{item.name}' -> cleaned: '{itemName}', type: '{item.itemType}'");
        
        // KEYCARD FIX: Check if this is a keycard item
        if (item.itemType != null && item.itemType.ToLower() == "keycard")
        {
            // Keycards need special handling - try keycard-specific paths first
            string[] keycardPaths = {
                $"Keycards/{itemName}",
                $"Keycards/{itemName.Replace(" ", "")}",
                $"Items/Keycards/{itemName}",
                $"Items/Keycards/{itemName.Replace(" ", "")}",
                $"prefabs_made/KEYCARDS/Keycards/{itemName}",
                $"prefabs_made/KEYCARDS/Keycards/{itemName.Replace(" ", "")}"
            };
            
            foreach (string path in keycardPaths)
            {
                ChestItemData testLoad = Resources.Load<ChestItemData>(path);
                if (testLoad != null)
                {
                    Debug.Log($"[StashManager] Found keycard at path: '{path}'");
                    return path;
                }
            }
            
            Debug.LogWarning($"[StashManager] ⚠️ KEYCARD '{item.name}' NOT FOUND in Resources folder! Keycards must be in a Resources folder to persist.");
            Debug.LogWarning($"[StashManager] Please move keycard assets from 'Assets/prefabs_made/KEYCARDS/Keycards/' to 'Assets/Resources/Keycards/'");
        }
        
        // Try common path patterns - UPDATED for your project structure
        string[] possiblePaths = {
            $"Items/OLDITEMS/{itemName}",
            $"Items/OLDITEMS/{itemName.Replace(" ", "")}", // Remove spaces
            $"Items/{itemName}",
            $"Items/{itemName.Replace(" ", "")}", 
            $"ChestItems/{itemName}",
            $"ChestItems/{itemName.Replace(" ", "")}",
            itemName, // Direct name
            itemName.Replace(" ", "") // Direct name without spaces
        };
        
        // Test which path works
        foreach (string path in possiblePaths)
        {
            ChestItemData testLoad = Resources.Load<ChestItemData>(path);
            if (testLoad != null)
            {
                Debug.Log($"[StashManager] Found working path: '{path}' for item '{item.name}'");
                return path;
            }
        }
        
        Debug.LogWarning($"[StashManager] No valid resource path found for item '{item.name}'. Tried: {string.Join(", ", possiblePaths)}");
        return $"Items/{itemName}"; // Fallback to default
    }
    
    /// <summary>
    /// Play audio clip
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    /// <summary>
    /// Find and load GemItemData from Resources for use with gem slots
    /// </summary>
    private ChestItemData FindGemItemData()
    {
        try
        {
            GemItemData gemData = Resources.Load<GemItemData>("Items/GemItemData");
            if (gemData == null)
            {
                return null;
            }
            
            return gemData;
        }
        catch (System.Exception e)
        {
            return null;
        }
    }
    
    /// <summary>
    /// DEBUG METHOD: Test gem transfer from inventory to stash
    /// Call this from Unity Inspector to test manual gem transfer
    /// </summary>
    [ContextMenu("Test Inventory to Stash Transfer")]
    public void TestInventoryToStashTransfer()
    {
        Debug.Log("[StashManager] Manual test: Attempting to transfer gems from inventory to stash");
        TransferGemsToStash();
    }
    
    /// <summary>
    /// DEBUG METHOD: Test if inventory gem slot is clickable
    /// Call this from Unity Inspector to test gem slot interaction
    /// </summary>
    [ContextMenu("Test Inventory Gem Slot Click")]
    public void TestInventoryGemSlotClick()
    {
        Debug.Log("[StashManager] Manual test: Simulating double-click on inventory gem slot");
        if (inventoryGemSlot != null)
        {
            Debug.Log($"[StashManager] Inventory gem slot: {inventoryGemSlot.gameObject.name}");
            Debug.Log($"[StashManager] Inventory gem slot active: {inventoryGemSlot.gameObject.activeInHierarchy}");
            Debug.Log($"[StashManager] Inventory gem slot interactable: {inventoryGemSlot.GetComponent<UnityEngine.UI.Selectable>()?.interactable ?? true}");
            
            // Manually trigger the HandleDoubleClick method
            HandleDoubleClick(inventoryGemSlot);
        }
        else
        {
            Debug.LogError("[StashManager] inventoryGemSlot is NULL!");
        }
    }
    
    /// <summary>
    /// DEBUG METHOD: Test gem transfer from stash to inventory
    /// Call this from Unity Inspector to test manual gem transfer
    /// </summary>
    [ContextMenu("Test Stash to Inventory Transfer")]
    public void TestStashToInventoryTransfer()
    {
        Debug.Log("[StashManager] Manual test: Attempting to transfer gems from stash to inventory");
        TransferGemsToInventory();
    }
    
    /// <summary>
    /// DEBUG METHOD: Test gem display by manually loading gems into gem slots
    /// Call this from Unity Inspector or console to test gem display
    /// </summary>
    [ContextMenu("Test Gem Display")]
    public void TestGemDisplay()
    {
        
        // Try to load the GemItemData asset
        GemItemData gemData = Resources.Load<GemItemData>("Items/GemItemData");
        if (gemData == null)
        {
            return;
        }
        
        
        // Test stash gem slot
        if (stashGemSlot != null)
        {
            stashGemSlot.SetItem(gemData, 5); // Set 5 gems
        }
        else
        {
        }
        
        // Test inventory gem slot
        if (inventoryGemSlot != null)
        {
            inventoryGemSlot.SetItem(gemData, 3); // Set 3 gems
        }
        else
        {
        }
    }
    
    // REMOVED: InstantCompleteGemTransfer() method - no longer needed for manual-only transfers
    
    /// <summary>
    /// Sync inventory gem slot reference with InventoryManager to ensure we're using the same slot
    /// </summary>
    private void SyncInventoryGemSlotReference()
    {
        Debug.Log("[StashManager] SyncInventoryGemSlotReference() called");
        
        InventoryManager menuInventoryManager = FindMenuInventoryManager();
        Debug.Log($"[StashManager] Found menuInventoryManager: {(menuInventoryManager != null ? menuInventoryManager.gameObject.name : "NULL")}");
        
        if (menuInventoryManager != null)
        {
            Debug.Log($"[StashManager] menuInventoryManager.gemSlot: {(menuInventoryManager.gemSlot != null ? menuInventoryManager.gemSlot.gameObject.name : "NULL")}");
            
            if (menuInventoryManager.gemSlot != null)
            {
                // Use the same gem slot that InventoryManager is using
                inventoryGemSlot = menuInventoryManager.gemSlot;
                Debug.Log($"[StashManager] ✅ Synced inventory gem slot reference: {inventoryGemSlot.gameObject.name}");
                Debug.Log($"[StashManager] Inventory gem slot isGemSlot: {inventoryGemSlot.isGemSlot}");
                
                // Re-add event listeners to the correct slot
                if (inventoryGemSlot != null)
                {
                    // Remove old listeners first (if any)
                    inventoryGemSlot.OnDoubleClick -= HandleDoubleClick;
                    inventoryGemSlot.OnItemDropped -= HandleItemDropped;
                    inventoryGemSlot.OnRightClick -= HandleRightClick;
                    
                    // Add new listeners
                    inventoryGemSlot.OnDoubleClick += HandleDoubleClick;
                    inventoryGemSlot.OnItemDropped += HandleItemDropped;
                    inventoryGemSlot.OnRightClick += HandleRightClick;
                    Debug.Log("[StashManager] ✅ Re-added event listeners to synced inventory gem slot");
                }
            }
            else
            {
                Debug.LogError("[StashManager] ❌ menuInventoryManager.gemSlot is NULL - cannot sync");
            }
        }
        else
        {
            Debug.LogError("[StashManager] ❌ Could not find Menu InventoryManager - cannot sync gem slot");
        }
    }
    
    /// <summary>
    /// Find InventoryManager with Menu context for proper stash integration
    /// </summary>
    private InventoryManager FindMenuInventoryManager()
    {
        InventoryManager[] allManagers = FindObjectsOfType<InventoryManager>();
        Debug.Log($"[StashManager] Found {allManagers.Length} InventoryManager(s) in scene");
        
        foreach (var manager in allManagers)
        {
            Debug.Log($"[StashManager] Checking InventoryManager on {manager.gameObject.name} - Context: {manager.inventoryContext}");
            if (manager.inventoryContext == InventoryContext.Menu)
            {
                Debug.Log($"[StashManager] ✅ Found Menu InventoryManager on {manager.gameObject.name}");
                return manager;
            }
        }
        
        Debug.LogWarning("[StashManager] ❌ No Menu context InventoryManager found in scene");
        
        // Fallback: try to use any InventoryManager if no Menu context found
        if (allManagers.Length > 0)
        {
            Debug.LogWarning($"[StashManager] Using fallback InventoryManager: {allManagers[0].gameObject.name}");
            return allManagers[0];
        }
        
        return null;
    }
    
    /// <summary>
    /// DEBUG METHOD: Test purchasable slot system
    /// </summary>
    [ContextMenu("Test Purchasable Slot System")]
    public void TestPurchasableSlotSystem()
    {
        Debug.Log("[StashManager] Testing purchasable slot system...");
        
        // Show current state
        int nextSlot = GetNextSlotToPurchase();
        Debug.Log($"[StashManager] Next slot to purchase: {(nextSlot >= 0 ? (nextSlot + 6).ToString() : "All purchased")}");
        
        if (nextSlot >= 0)
        {
            int cost = _slotCosts[nextSlot];
            int currentGems = PlayerProgression.Instance?.currentSpendableGems ?? 0;
            Debug.Log($"[StashManager] Slot {nextSlot + 6} costs {cost} gems. Player has {currentGems} gems.");
            
            if (currentGems >= cost)
            {
                Debug.Log("[StashManager] Player can afford this slot!");
            }
            else
            {
                Debug.Log($"[StashManager] Player needs {cost - currentGems} more gems.");
            }
        }
        
        // Show all slot costs
        Debug.Log("[StashManager] All slot costs:");
        for (int i = 0; i < _slotCosts.Length; i++)
        {
            string status = _slotsPurchased[i] ? "PURCHASED" : "LOCKED";
            Debug.Log($"[StashManager] Slot {i + 6}: {_slotCosts[i]} gems - {status}");
        }
    }
    
    /// <summary>
    /// DEBUG METHOD: Verify UI setup for purchasable slots
    /// </summary>
    [ContextMenu("Debug Purchasable Slot UI Setup")]
    public void DebugPurchasableSlotUISetup()
    {
        Debug.Log("[StashManager] === PURCHASABLE SLOT UI SETUP DEBUG ===");
        
        for (int i = 0; i < _purchasableStashSlots.Count; i++)
        {
            var slot = _purchasableStashSlots[i];
            var lockedImage = i < lockedSlotImages.Length ? lockedSlotImages[i] : null;
            var priceText = i < slotPriceTexts.Length ? slotPriceTexts[i] : null;
            var purchaseButton = i < purchaseButtons.Length ? purchaseButtons[i] : null;
            
            Debug.Log($"[StashManager] Slot {i + 6}:");
            Debug.Log($"  - Slot GameObject: {(slot != null ? slot.gameObject.name : "NULL")}");
            Debug.Log($"  - Locked Image: {(lockedImage != null ? lockedImage.name : "NULL")}");
            Debug.Log($"  - Price Text: {(priceText != null ? priceText.gameObject.name : "NULL")}");
            Debug.Log($"  - Purchase Button: {(purchaseButton != null ? purchaseButton.gameObject.name : "NULL")}");
            Debug.Log($"  - Status: {(_slotsPurchased[i] ? "PURCHASED" : "LOCKED")}");
            Debug.Log($"  - Cost: {_slotCosts[i]} gems");
        }
        
        // Force UI update
        UpdatePurchasableSlotUI();
        Debug.Log("[StashManager] UI update completed.");
    }
    
    /// <summary>
    /// DEBUG METHOD: Test purchase button raycast and functionality
    /// </summary>
    [ContextMenu("Test Purchase Button Raycast")]
    public void TestPurchaseButtonRaycast()
    {
        Debug.Log("[StashManager] === PURCHASE BUTTON RAYCAST TEST ===");
        
        int nextSlot = GetNextSlotToPurchase();
        if (nextSlot < 0)
        {
            Debug.Log("[StashManager] All slots purchased - no button to test");
            return;
        }
        
        var purchaseButton = nextSlot < purchaseButtons.Length ? purchaseButtons[nextSlot] : null;
        if (purchaseButton == null)
        {
            Debug.LogError($"[StashManager] Purchase button for slot {nextSlot + 6} is NULL!");
            return;
        }
        
        Debug.Log($"[StashManager] Testing purchase button for slot {nextSlot + 6}:");
        Debug.Log($"  - Button GameObject: {purchaseButton.gameObject.name}");
        Debug.Log($"  - Active: {purchaseButton.gameObject.activeSelf}");
        Debug.Log($"  - Interactable: {purchaseButton.interactable}");
        
        var buttonImage = purchaseButton.GetComponent<Image>();
        Debug.Log($"  - Has Image: {buttonImage != null}");
        if (buttonImage != null)
        {
            Debug.Log($"  - Image RaycastTarget: {buttonImage.raycastTarget}");
        }
        
        var buttonRect = purchaseButton.GetComponent<RectTransform>();
        if (buttonRect != null)
        {
            Debug.Log($"  - Position: {buttonRect.anchoredPosition}");
            Debug.Log($"  - Size: {buttonRect.sizeDelta}");
        }
        
        // Test if button has click listener
        var buttonComponent = purchaseButton.GetComponent<Button>();
        if (buttonComponent != null)
        {
            Debug.Log($"  - Button listeners count: {buttonComponent.onClick.GetPersistentEventCount()}");
        }
        
        // Check for overlapping UI elements
        var lockedImage = nextSlot < lockedSlotImages.Length ? lockedSlotImages[nextSlot] : null;
        if (lockedImage != null)
        {
            var lockImageComponent = lockedImage.GetComponent<Image>();
            Debug.Log($"  - Lock image active: {lockedImage.activeSelf}");
            Debug.Log($"  - Lock image raycast target: {lockImageComponent?.raycastTarget}");
        }
        
        // Simulate button click
        Debug.Log($"[StashManager] Attempting to simulate purchase of slot {nextSlot + 6}...");
        PurchaseSlot(nextSlot);
    }
    
    /// <summary>
    /// DEBUG METHOD: Test slot locking system - verify locked slots reject items
    /// </summary>
    [ContextMenu("Test Slot Locking System")]
    public void TestSlotLockingSystem()
    {
        Debug.Log("[StashManager] === SLOT LOCKING SYSTEM TEST ===");
        
        // Find a test item to try placing
        ChestItemData testItem = Resources.Load<ChestItemData>("Items/AsteroidDust");
        if (testItem == null)
        {
            Debug.LogError("[StashManager] Could not find test item 'AsteroidDust' for slot locking test!");
            return;
        }
        
        Debug.Log($"[StashManager] Testing with item: {testItem.itemName}");
        
        // Test all purchasable slots
        for (int i = 0; i < _purchasableStashSlots.Count; i++)
        {
            var slot = _purchasableStashSlots[i];
            if (slot == null) continue;
            
            bool isPurchased = i < _slotsPurchased.Length && _slotsPurchased[i];
            bool canAcceptItem = slot.CanAcceptItem(testItem);
            
            Debug.Log($"[StashManager] Slot {i + 6} ({slot.gameObject.name}):");
            Debug.Log($"  - Purchased: {isPurchased}");
            Debug.Log($"  - Can Accept Item: {canAcceptItem}");
            Debug.Log($"  - Expected Result: {(isPurchased ? "ACCEPT" : "REJECT")}");
            
            if (isPurchased && !canAcceptItem)
            {
                Debug.LogError($"  - ❌ ERROR: Purchased slot should accept items!");
            }
            else if (!isPurchased && canAcceptItem)
            {
                Debug.LogError($"  - ❌ ERROR: Unpurchased slot should reject items!");
            }
            else
            {
                Debug.Log($"  - ✅ CORRECT: Slot behaving as expected");
            }
        }
        
        // Test regular stash slots (should always accept)
        Debug.Log("[StashManager] Testing regular stash slots (should always accept):");
        for (int i = 0; i < _stashSlots.Count; i++)
        {
            var slot = _stashSlots[i];
            if (slot == null) continue;
            
            bool canAcceptItem = slot.CanAcceptItem(testItem);
            Debug.Log($"[StashManager] Regular slot {i + 1} ({slot.gameObject.name}): Can Accept = {canAcceptItem} (should be TRUE)");
            
            if (!canAcceptItem)
            {
                Debug.LogError($"  - ❌ ERROR: Regular stash slot should always accept items!");
            }
        }
        
        Debug.Log("[StashManager] === SLOT LOCKING TEST COMPLETE ===");
    }
    
    /// <summary>
    /// DEBUG METHOD: Test click hierarchy for locked slots - verify purchase buttons receive clicks
    /// </summary>
    [ContextMenu("Test Click Hierarchy for Locked Slots")]
    public void TestClickHierarchyForLockedSlots()
    {
        Debug.Log("[StashManager] === CLICK HIERARCHY TEST FOR LOCKED SLOTS ===");
        
        int nextSlot = GetNextSlotToPurchase();
        if (nextSlot < 0)
        {
            Debug.Log("[StashManager] All slots purchased - no locked slots to test");
            return;
        }
        
        Debug.Log($"[StashManager] Testing click hierarchy for next purchasable slot: {nextSlot + 6}");
        
        // Get the components for the next purchasable slot
        var slot = nextSlot < _purchasableStashSlots.Count ? _purchasableStashSlots[nextSlot] : null;
        var lockedImage = nextSlot < lockedSlotImages.Length ? lockedSlotImages[nextSlot] : null;
        var priceText = nextSlot < slotPriceTexts.Length ? slotPriceTexts[nextSlot] : null;
        var purchaseButton = nextSlot < purchaseButtons.Length ? purchaseButtons[nextSlot] : null;
        
        if (slot == null)
        {
            Debug.LogError($"[StashManager] Slot {nextSlot + 6} is NULL!");
            return;
        }
        
        Debug.Log($"[StashManager] === SLOT {nextSlot + 6} CLICK HIERARCHY ANALYSIS ===");
        
        // Check slot components
        var slotImage = slot.GetComponent<Image>();
        var slotButton = slot.GetComponent<Button>();
        var unifiedSlot = slot.GetComponent<UnifiedSlot>();
        
        Debug.Log($"[StashManager] SLOT ({slot.gameObject.name}):");
        Debug.Log($"  - Active: {slot.gameObject.activeSelf}");
        Debug.Log($"  - Image RaycastTarget: {slotImage?.raycastTarget}");
        Debug.Log($"  - Button Interactable: {slotButton?.interactable}");
        Debug.Log($"  - UnifiedSlot Present: {unifiedSlot != null}");
        Debug.Log($"  - Sibling Index: {slot.transform.GetSiblingIndex()}");
        
        // Check locked image
        if (lockedImage != null)
        {
            var lockImageComponent = lockedImage.GetComponent<Image>();
            Debug.Log($"[StashManager] LOCK IMAGE ({lockedImage.name}):");
            Debug.Log($"  - Active: {lockedImage.activeSelf}");
            Debug.Log($"  - RaycastTarget: {lockImageComponent?.raycastTarget}");
            Debug.Log($"  - Sibling Index: {lockedImage.transform.GetSiblingIndex()}");
        }
        
        // Check price text
        if (priceText != null)
        {
            Debug.Log($"[StashManager] PRICE TEXT ({priceText.gameObject.name}):");
            Debug.Log($"  - Active: {priceText.gameObject.activeSelf}");
            Debug.Log($"  - RaycastTarget: {priceText.raycastTarget}");
            Debug.Log($"  - Sibling Index: {priceText.transform.GetSiblingIndex()}");
        }
        
        // Check purchase button
        if (purchaseButton != null)
        {
            var buttonImage = purchaseButton.GetComponent<Image>();
            var buttonComponent = purchaseButton.GetComponent<Button>();
            Debug.Log($"[StashManager] PURCHASE BUTTON ({purchaseButton.gameObject.name}):");
            Debug.Log($"  - Active: {purchaseButton.gameObject.activeSelf}");
            Debug.Log($"  - Interactable: {purchaseButton.interactable}");
            Debug.Log($"  - Image RaycastTarget: {buttonImage?.raycastTarget}");
            Debug.Log($"  - Button Listeners: {buttonComponent?.onClick.GetPersistentEventCount()}");
            Debug.Log($"  - Sibling Index: {purchaseButton.transform.GetSiblingIndex()}");
            Debug.Log($"  - Position: {purchaseButton.transform.position}");
            Debug.Log($"  - Size: {(purchaseButton.transform as RectTransform)?.sizeDelta}");
        }
        
        Debug.Log("[StashManager] === CLICK HIERARCHY TEST COMPLETE ===");
        Debug.Log("[StashManager] EXPECTED: Purchase button should have highest sibling index and raycastTarget=true");
    }
}

/// <summary>
/// Data container for stash save file
/// </summary>
[System.Serializable]
public class StashSaveData
{
    public int gemCount = 0;
    public List<SlotData> slots = new List<SlotData>();
    public List<SlotData> purchasableSlots = new List<SlotData>();
}

/// <summary>
/// Data container for individual slot - UNIFIED with InventoryManager
/// </summary>
[System.Serializable]
public class SlotData
{
    public string itemName = "";
    public int itemCount = 0;
    public string itemPath = "";
}
