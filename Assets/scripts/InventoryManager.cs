using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using TMPro;
using System.IO;
using GeminiGauntlet.Audio;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }
    
    [Header("Context Settings")]
    [Tooltip("Mark this InventoryManager's context - affects singleton behavior and data handling")]
    public InventoryContext inventoryContext = InventoryContext.Game;
    
    [Header("Essential References")]
    public ReviveSlotController reviveSlot;
    public UnifiedSlot gemSlot;
    public BackpackSlotController backpackSlot;
    public VestSlotController vestSlot; // VEST SYSTEM: Reference to vest slot
    
    [Header("Weapon Equipment Slots")]
    [Tooltip("Right hand weapon slot (UnifiedSlot with isWeaponSlot = true) - Used for auto-equip")]
    public UnifiedSlot rightHandWeaponSlot;
    
    [Tooltip("Left hand weapon slot (UnifiedSlot with isWeaponSlot = true) - FUTURE")]
    public UnifiedSlot leftHandWeaponSlot;
    
    public Transform inventorySlotsParent;
    
    [Header("Inventory UI")]
    [Tooltip("Main inventory UI panel - will be hidden at start and toggled with TAB key")]
    public GameObject inventoryUIPanel;
    
    [Header("Audio")]
    [Tooltip("Reference to SoundEvents ScriptableObject for inventory sounds")]
    public SoundEvents soundEvents;
    
    [Header("Gem System - Uniform Slot Based")]
    [Tooltip("Inventory slot 0 should be marked as gem slot in Unity Inspector")]
    public bool useUniformGemSystem = true;
    
    [Header("Inventory Settings")]
    public int inventoryCapacity = 24;
    
    [Header("Backpack System")]
    [Tooltip("Current number of active inventory slots (controlled by equipped backpack)")]
    public int currentActiveSlots = 5; // Default Tier 1 backpack slots
    
    // Internal inventory slots
    private List<UnifiedSlot> inventorySlots = new List<UnifiedSlot>();
    
    // Save/Load functionality
    private string _inventorySavePath;
    
    // UI state management
    private bool isInventoryVisible = false;
    
    // SIMPLE GEM COUNT SYSTEM
    public int currentGemCount = 0;
    
    // Events for other systems
    public System.Action<ChestItemData, int> OnItemAdded;
    public System.Action OnInventoryChanged;
    public static System.Action<int> OnGemCollected;
    
    void Awake()
    {
        // Check current scene to determine persistence behavior
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool isMenuScene = currentScene.Contains("Menu");
        
        if (Instance == null)
        {
            Instance = this;
            
            // Only persist if we're in menu scene
            if (isMenuScene)
            {
                DontDestroyOnLoad(gameObject);
                Debug.Log($"[InventoryManager] Created persistent {inventoryContext} singleton in menu scene");
            }
            else
            {
                Debug.Log($"[InventoryManager] Created non-persistent {inventoryContext} singleton in game scene");
            }
        }
        else
        {
            // If existing instance is from menu and we're in game, replace it
            if (!isMenuScene && Instance.inventoryContext == InventoryContext.Menu)
            {
                Debug.Log($"[InventoryManager] Replacing menu instance with game instance");
                Destroy(Instance.gameObject);
                Instance = this;
            }
            else
            {
                // Otherwise destroy duplicate
                Debug.Log($"[InventoryManager] Duplicate {inventoryContext} instance destroyed - keeping existing {Instance.inventoryContext} instance");
                Destroy(gameObject);
                return;
            }
        }
        
        
        // Initialize save path
        _inventorySavePath = Path.Combine(Application.persistentDataPath, "inventory_data.json");
        
        // Initialize inventory slots
        InitializeInventorySlots();
    }
    
    void Start()
    {
        Debug.Log($"[InventoryManager] Start() called - Context: {inventoryContext} on {gameObject.name} in scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        
        // Validate and auto-assign missing references in game scene
        ValidateAndAssignReferences();
        
        // Context-specific initialization
        if (inventoryContext == InventoryContext.Game)
        {
            // Game context: Subscribe to PlayerProgression, hide UI initially
            SubscribeToGemEvents();
            UpdateGemSlotFromProgression();
            HideInventoryUI();
        }
        else if (inventoryContext == InventoryContext.Menu)
        {
            // Menu context: Don't subscribe to PlayerProgression (doesn't exist in menu)
            // Don't hide UI (menu manages its own UI state)
            Debug.Log("[InventoryManager] Menu context - skipping PlayerProgression subscription");
        }
        
        // NEW: Load from PersistentItemInventoryManager if available
        if (PersistentItemInventoryManager.Instance != null)
        {
            Debug.Log($"[InventoryManager] Found PersistentItemInventoryManager - loading inventory data for {inventoryContext} context");
            var persistentData = PersistentItemInventoryManager.Instance.GetCurrentInventoryData();
            Debug.Log($"[InventoryManager] PersistentItemInventoryManager has {persistentData.items.Count} items and {persistentData.gemCount} gems");
            
            PersistentItemInventoryManager.Instance.ApplyToInventoryManager(this);
            Debug.Log("[InventoryManager] Loaded inventory from PersistentItemInventoryManager");
            
            // CRITICAL FIX: Ensure slot visibility is updated after loading (especially important for menu scene)
            if (inventoryContext == InventoryContext.Menu)
            {
                StartCoroutine(EnsureSlotVisibilityAfterLoad());
            }
        }
        else
        {
            Debug.Log("[InventoryManager] No PersistentItemInventoryManager found - using fallback LoadInventoryData()");
            // FALLBACK: Load inventory data using legacy system
            LoadInventoryData();
        }
        
        Debug.Log($"[InventoryManager] {inventoryContext} context initialization complete on {gameObject.name}");
    }
    
    /// <summary>
    /// Coroutine to ensure slot visibility is properly updated after loading from persistence
    /// This is critical for menu scenes where the UI needs to show the correct number of slots
    /// </summary>
    private System.Collections.IEnumerator EnsureSlotVisibilityAfterLoad()
    {
        // Wait one frame to ensure all initialization is complete
        yield return null;
        
        Debug.Log("[InventoryManager] EnsureSlotVisibilityAfterLoad: Checking slot visibility...");
        
        // Re-validate references in case they weren't ready during Start()
        if (inventorySlots == null || inventorySlots.Count == 0)
        {
            Debug.Log("[InventoryManager] EnsureSlotVisibilityAfterLoad: Slots not initialized, validating references...");
            ValidateAndAssignReferences();
        }
        
        // Update slot visibility based on current backpack
        if (inventorySlots != null && inventorySlots.Count > 0)
        {
            Debug.Log($"[InventoryManager] EnsureSlotVisibilityAfterLoad: Updating visibility for {currentActiveSlots} active slots");
            UpdateSlotVisibility();
        }
        else
        {
            Debug.LogWarning("[InventoryManager] EnsureSlotVisibilityAfterLoad: Still no slots found after validation!");
        }
    }
    
    /// <summary>
    /// Validate and auto-assign missing references when InventoryManager enters a new scene
    /// </summary>
    private void ValidateAndAssignReferences()
    {
        bool needsReassignment = false;
        
        // Check if critical references are missing
        if (reviveSlot == null)
        {
            Debug.LogWarning("[InventoryManager] ReviveSlot reference missing - attempting to find in scene");
            reviveSlot = FindObjectOfType<ReviveSlotController>();
            needsReassignment = true;
        }
        
        if (backpackSlot == null)
        {
            Debug.LogWarning("[InventoryManager] BackpackSlot reference missing - attempting to find in scene");
            backpackSlot = FindObjectOfType<BackpackSlotController>();
            needsReassignment = true;
        }
        
        if (vestSlot == null)
        {
            Debug.LogWarning("[InventoryManager] VestSlot reference missing - attempting to find in scene");
            vestSlot = FindObjectOfType<VestSlotController>();
            needsReassignment = true;
        }
        
        if (gemSlot == null)
        {
            Debug.LogWarning("[InventoryManager] GemSlot reference missing - attempting to find in scene");
            // Look for UnifiedSlot with isGemSlot = true
            UnifiedSlot[] allSlots = FindObjectsOfType<UnifiedSlot>();
            foreach (var slot in allSlots)
            {
                if (slot.isGemSlot)
                {
                    gemSlot = slot;
                    break;
                }
            }
            needsReassignment = true;
        }
        
        if (inventorySlotsParent == null)
        {
            Debug.LogWarning("[InventoryManager] InventorySlotsParent reference missing - attempting to find in scene");
            GameObject inventoryParent = GameObject.Find("inventorySLOTS");
            if (inventoryParent != null)
            {
                inventorySlotsParent = inventoryParent.transform;
            }
            needsReassignment = true;
        }
        
        if (inventoryUIPanel == null)
        {
            Debug.LogWarning("[InventoryManager] InventoryUIPanel reference missing - attempting to find in scene");
            GameObject inventoryPanel = GameObject.Find("inventoryPanel");
            if (inventoryPanel != null)
            {
                inventoryUIPanel = inventoryPanel;
            }
            needsReassignment = true;
        }
        
        if (needsReassignment)
        {
            Debug.Log("[InventoryManager] Re-initializing inventory slots after reference assignment");
            InitializeInventorySlots();
        }
    }
    
    /// <summary>
    /// Check if we're currently in the menu scene
    /// </summary>
    private bool IsInMenuScene()
    {
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Menu");
    }
    
    /// <summary>
    /// Handle TAB key input for inventory toggle (only in game context)
    /// </summary>
    void Update()
    {
        // Only handle TAB key in game context
        if (inventoryContext == InventoryContext.Game && Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleInventoryUI();
        }
    }
    
    /// <summary>
    /// Toggle inventory UI visibility
    /// </summary>
    public void ToggleInventoryUI()
    {
        Debug.Log($"[InventoryManager] ⚡ ToggleInventoryUI() CALLED - Current state: {isInventoryVisible}");
        if (isInventoryVisible)
        {
            HideInventoryUI();
        }
        else
        {
            ShowInventoryUI();
        }
    }
    
    /// <summary>
    /// Show inventory UI
    /// </summary>
    public void ShowInventoryUI()
    {
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("[InventoryManager] 🔵 ShowInventoryUI() CALLED");
        Debug.Log($"[InventoryManager] inventoryUIPanel null? {inventoryUIPanel == null}");
        Debug.Log($"[InventoryManager] isInventoryVisible BEFORE: {isInventoryVisible}");
        
        if (inventoryUIPanel != null)
        {
            inventoryUIPanel.SetActive(true);
            isInventoryVisible = true;
            
            Debug.Log($"[InventoryManager] ✅ isInventoryVisible set to TRUE");
            Debug.Log($"[InventoryManager] IsInventoryVisible() returns: {IsInventoryVisible()}");
            
            // 🧠 COGNITIVE: Notify system about inventory being opened
            Debug.Log("[InventoryManager] 🧠 Invoking CognitiveEvents.OnInventoryOpened");
            CognitiveEvents.OnInventoryOpened?.Invoke();
            
            // 🔊 AUDIO: Play inventory open sound
            if (soundEvents != null && soundEvents.inventoryOpen != null)
            {
                soundEvents.inventoryOpen.Play2D();
                Debug.Log("[InventoryManager] 🔊 Playing inventory open sound");
            }
            
            // CRITICAL FIX: Ensure slots are properly visible when UI is shown
            // This handles the case where the menu loads and needs to show the correct number of slots
            if (inventorySlots != null && inventorySlots.Count > 0)
            {
                UpdateSlotVisibility();
            }
            else
            {
                Debug.LogWarning("[InventoryManager] ShowInventoryUI: Inventory slots not initialized yet");
            }
            
            // Only manage cursor in game scenes, NOT in menu scenes
            if (!IsInMenuScene())
            {
                // Unlock cursor for inventory interaction
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            
        }
        else
        {
            Debug.LogError("[InventoryManager] ❌ inventoryUIPanel is NULL! Cannot show inventory.");
        }
        Debug.Log("═══════════════════════════════════════════════════════");
    }
    
    /// <summary>
    /// Hide inventory UI
    /// </summary>
    public void HideInventoryUI()
    {
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("[InventoryManager] 🔴 HideInventoryUI() CALLED");
        Debug.Log($"[InventoryManager] inventoryUIPanel null? {inventoryUIPanel == null}");
        Debug.Log($"[InventoryManager] isInventoryVisible BEFORE: {isInventoryVisible}");
        
        if (inventoryUIPanel != null)
        {
            inventoryUIPanel.SetActive(false);
            isInventoryVisible = false;
            
            Debug.Log($"[InventoryManager] ✅ isInventoryVisible set to FALSE");
            Debug.Log($"[InventoryManager] IsInventoryVisible() returns: {IsInventoryVisible()}");
            
            // 🧠 COGNITIVE: Notify system about inventory being closed
            Debug.Log("[InventoryManager] 🧠 Invoking CognitiveEvents.OnInventoryClosed");
            CognitiveEvents.OnInventoryClosed?.Invoke();
            
            // 🔊 AUDIO: Play inventory close sound
            if (soundEvents != null && soundEvents.inventoryClose != null)
            {
                soundEvents.inventoryClose.Play2D();
                Debug.Log("[InventoryManager] 🔊 Playing inventory close sound");
            }
            
            // Only manage cursor in game scenes, NOT in menu scenes
            if (!IsInMenuScene())
            {
                // Lock cursor for gameplay
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            
        }
        else
        {
            Debug.LogError("[InventoryManager] ❌ inventoryUIPanel is NULL! Cannot hide inventory.");
        }
        Debug.Log("═══════════════════════════════════════════════════════");
    }
    
    /// <summary>
    /// Check if inventory UI is currently visible
    /// </summary>
    public bool IsInventoryVisible()
    {
        Debug.Log($"[InventoryManager] 🔍 IsInventoryVisible() queried - returning: {isInventoryVisible}");
        return isInventoryVisible;
    }
    
    /// <summary>
    /// Check if an item is a gem
    /// </summary>
    private bool IsGemItem(ChestItemData item)
    {
        if (item == null) return false;
        
        // Check if it's a GemItemData or has gem-like properties
        return item is GemItemData || 
               item.itemType.ToLower() == "gem" || 
               item.itemName.ToLower().Contains("gem");
    }
    
    /// <summary>
    /// Check if an item is a self-revive item
    /// </summary>
    private bool IsSelfReviveItem(ChestItemData item)
    {
        if (item == null) return false;
        
        // Check if it's a SelfReviveItemData or has revive-like properties
        return item is SelfReviveItemData || 
               item.itemType.ToLower().Contains("revive") || 
               item.itemName.ToLower().Contains("revive");
    }
    
    /// <summary>
    /// Clear all non-gem slots for PersistentItemInventoryManager
    /// </summary>
    public void ClearNonGemSlots()
    {
        foreach (var slot in inventorySlots)
        {
            if (slot != null && !slot.isGemSlot)
            {
                slot.ClearSlot();
            }
        }
    }
    
    private void InitializeInventorySlots()
    {
        // Clear existing slots to prevent duplicates
        inventorySlots.Clear();
        
        // Find all inventory slots in the parent
        if (inventorySlotsParent != null)
        {
            var foundSlots = inventorySlotsParent.GetComponentsInChildren<UnifiedSlot>();
            inventorySlots.AddRange(foundSlots);
            
            // Set slot indices and subscribe to drag & drop events
            for (int i = 0; i < inventorySlots.Count; i++)
            {
                inventorySlots[i].slotIndex = i;
                
                // Subscribe to drag & drop events for inventory rearrangement
                inventorySlots[i].OnItemDropped += HandleInventoryItemDropped;
                inventorySlots[i].OnDoubleClick += HandleInventoryDoubleClick;
            }
            
            Debug.Log($"[InventoryManager] Initialized {inventorySlots.Count} inventory slots");
        }
        else
        {
            Debug.LogWarning("[InventoryManager] inventorySlotsParent is null - cannot initialize slots");
        }
        
        // ⚔️ NEW: Subscribe weapon slots to drag/drop handlers for weapon slot ↔ inventory transfers
        if (rightHandWeaponSlot != null)
        {
            rightHandWeaponSlot.OnItemDropped += HandleInventoryItemDropped;
            Debug.Log($"[InventoryManager] ✅ Subscribed rightHandWeaponSlot to drag/drop handler");
        }
        
        if (leftHandWeaponSlot != null)
        {
            leftHandWeaponSlot.OnItemDropped += HandleInventoryItemDropped;
            Debug.Log($"[InventoryManager] ✅ Subscribed leftHandWeaponSlot to drag/drop handler");
        }
    }
    
    /// <summary>
    /// Add item to inventory - core functionality
    /// ATOMIC OPERATION: All-or-nothing with proper validation
    /// ⚔️ WEAPON AUTO-EQUIP: If item is an equippable weapon and weapon slot is empty, auto-equip there
    /// </summary>
    /// <param name="item">Item to add</param>
    /// <param name="count">Count to add</param>
    /// <param name="autoSave">Auto-save after adding (default true, set false if caller will save)</param>
    public bool TryAddItem(ChestItemData item, int count = 1, bool autoSave = true)
    {
        // ATOMIC VALIDATION: Ensure valid input
        if (item == null || count <= 0)
        {
            Debug.LogError($"[InventoryManager] ❌ ATOMIC VALIDATION: Invalid item or count (item={item}, count={count})");
            return false;
        }
        
        Debug.Log($"[InventoryManager] 🔄 ATOMIC ADD: {count}x {item.itemName} (ID: {item.itemID}, autoSave: {autoSave})");
        
        // Handle gems - they can ONLY go to gem slots!
        if (item is GemItemData || item.itemType.ToLower() == "gem" || item.itemName.ToLower().Contains("gem"))
        {
            return TryAddGemToGemSlot(item, count);
        }
        
        // ⚔️ WEAPON AUTO-EQUIP: Check if this is an equippable weapon
        if (item is EquippableWeaponItemData weaponData)
        {
            // UNIFIED: Use InventoryManager's own weapon slot reference (same slot WeaponEquipmentManager uses)
            if (rightHandWeaponSlot != null && rightHandWeaponSlot.IsEmpty)
            {
                // AUTO-EQUIP: Directly equip to weapon slot instead of inventory
                Debug.Log($"[InventoryManager] ⚔️ AUTO-EQUIP: Weapon slot empty - equipping {weaponData.itemName} directly!");
                
                rightHandWeaponSlot.SetItem(weaponData, count, bypassValidation: true);
                
                // ⚔️ AUTO-ACTIVATE: After equipping, activate sword mode if not already active
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    PlayerShooterOrchestrator playerShooter = player.GetComponent<PlayerShooterOrchestrator>();
                    if (playerShooter != null && !playerShooter.IsSwordModeActive)
                    {
                        // Run coroutine on player (persistent object)
                        playerShooter.StartCoroutine(ActivateSwordModeNextFrame(playerShooter));
                        Debug.Log("[InventoryManager] ⚔️ Started auto-activation coroutine on player");
                    }
                }
                
                OnItemAdded?.Invoke(item, count);
                OnInventoryChanged?.Invoke();
                
                if (autoSave)
                {
                    SaveInventoryData();
                }
                
                return true;
            }
            else
            {
                Debug.Log($"[InventoryManager] Weapon slot occupied - adding {weaponData.itemName} to regular inventory");
                // Fall through to normal inventory logic
            }
        }
        
        // REMOVED: Self-revive items are no longer forced to revive slot
        // They can now be stored in regular inventory slots like any other item
        // Players can manually equip them to the revive slot via drag-drop or double-click
        
        // ATOMIC OPERATION: First try to stack with existing items of the same type
        var existingSlot = GetSlotWithItem(item);
        if (existingSlot != null)
        {
            int oldCount = existingSlot.ItemCount;
            int newCount = oldCount + count;
            existingSlot.SetItem(item, newCount);
            
            Debug.Log($"[InventoryManager] ✅ ATOMIC STACKING: {oldCount} + {count} = {newCount}x {item.itemName}");
            
            OnItemAdded?.Invoke(item, count);
            OnInventoryChanged?.Invoke();
            
            if (autoSave)
            {
                SaveInventoryData(); // Auto-save on item stacking
            }
            return true;
        }
        
        // ATOMIC OPERATION: If no existing stack, find first empty slot for regular items
        var emptySlot = GetFirstEmptySlot();
        if (emptySlot != null)
        {
            emptySlot.SetItem(item, count);
            
            Debug.Log($"[InventoryManager] ✅ ATOMIC NEW SLOT: Added {count}x {item.itemName} to empty slot");
            
            OnItemAdded?.Invoke(item, count);
            OnInventoryChanged?.Invoke();
            
            if (autoSave)
            {
                SaveInventoryData(); // Auto-save on item addition
            }
            return true;
        }
        
        Debug.LogWarning($"[InventoryManager] ❌ ATOMIC FAILED: No space for {count}x {item.itemName}");
        return false;
    }
    
    /// <summary>
    /// Add gems specifically to gem slots only - SIMPLE!
    /// </summary>
    private bool TryAddGemToGemSlot(ChestItemData gemItem, int count)
    {
        
        // Find the gem slot (should be slot with isGemSlot = true)
        UnifiedSlot gemSlot = GetGemSlot();
        if (gemSlot == null)
        {
            return false;
        }
        
        
        // BUGFIX: If gem slot is occupied by non-gem item, clear it first
        if (!gemSlot.IsEmpty && !(gemSlot.CurrentItem is GemItemData) && !IsGemItem(gemSlot.CurrentItem))
        {
            
            // Try to move the non-gem item to a regular slot
            var regularSlot = GetFirstEmptySlot();
            if (regularSlot != null)
            {
                regularSlot.SetItem(gemSlot.CurrentItem, gemSlot.ItemCount);
                gemSlot.ClearSlot();
            }
            else
            {
                gemSlot.ClearSlot();
            }
        }
        
        // Add gems to the gem slot (stack if possible)
        if (gemSlot.IsEmpty)
        {
            gemSlot.SetItem(gemItem, count);
        }
        else if (gemSlot.CurrentItem is GemItemData || IsGemItem(gemSlot.CurrentItem))
        {
            gemSlot.SetItem(gemSlot.CurrentItem, gemSlot.ItemCount + count);
        }
        else
        {
            return false;
        }
        
        
        // Trigger events
        OnItemAdded?.Invoke(gemItem, count);
        OnInventoryChanged?.Invoke();
        SaveInventoryData(); // Auto-save gem additions
        
        return true;
    }
    
    // SyncSpendableGemsToInventory method removed - PlayerProgression doesn't exist in menu scene!
    
    /// <summary>
    /// Get the gem slot (directly assigned in inspector)
    /// </summary>
    private UnifiedSlot GetGemSlot()
    {
        if (gemSlot == null)
        {
            return null;
        }
        
        return gemSlot;
    }
    
    /// <summary>
    /// Get first empty slot in inventory slot (EXCLUDES gem slots and special slots)
    /// BACKPACK SYSTEM: Only considers active slots based on equipped backpack
    /// </summary>
    public UnifiedSlot GetFirstEmptySlot()
    {
        // CRITICAL FIX: Regular items should NEVER go to gem slots!
        // BACKPACK SYSTEM: Only check slots within the active slot count
        int regularSlotCount = 0; // Count regular slots separately from array index
        
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            var slot = inventorySlots[i];
            if (slot != null && !slot.isGemSlot)
            {
                // This is a regular slot - check if it's within active slot limit
                if (regularSlotCount < currentActiveSlots && slot.IsEmpty)
                {
                    return slot;
                }
                regularSlotCount++; // Only increment for regular slots
            }
        }
        return null;
    }
    
    /// <summary>
    /// Get first slot that contains the specified item (for stacking)
    /// UNIFIED STACKING: Uses IsSameItem() for proper item matching
    /// </summary>
    public UnifiedSlot GetSlotWithItem(ChestItemData item)
    {
        if (item == null) return null;
        
        return inventorySlots.FirstOrDefault(slot => 
            !slot.IsEmpty && 
            slot.CurrentItem != null && 
            slot.CurrentItem.IsSameItem(item));
    }
    
    /// <summary>
    /// Get all inventory slots for external systems (EXCLUDES gem slot - gem slot is separate)
    /// </summary>
    public List<UnifiedSlot> GetAllInventorySlots()
    {
        // Return only regular inventory slots, exclude gem slot
        var regularSlots = new List<UnifiedSlot>();
        
        // Safety check
        if (inventorySlots == null)
        {
            Debug.LogWarning("[InventoryManager] GetAllInventorySlots: inventorySlots is null, returning empty list");
            return regularSlots;
        }
        
        foreach (var slot in inventorySlots)
        {
            if (slot != null && slot != gemSlot && !slot.isGemSlot)
            {
                regularSlots.Add(slot);
            }
        }
        
        Debug.Log($"[InventoryManager] GetAllInventorySlots: Returning {regularSlots.Count} regular slots (excluding gem slot)");
        return regularSlots;
    }
    
    /// <summary>
    /// BACKPACK SYSTEM: Update the number of active inventory slots based on equipped backpack
    /// </summary>
    public void UpdateInventorySlotCount(int newSlotCount)
    {
        // CRITICAL FIX: Ensure slots are initialized before updating visibility
        if (inventorySlots == null || inventorySlots.Count == 0)
        {
            Debug.LogWarning($"[InventoryManager] UpdateInventorySlotCount called but inventorySlots is empty - re-initializing slots");
            ValidateAndAssignReferences();
            
            // If still empty after validation, just store the slot count and return
            if (inventorySlots == null || inventorySlots.Count == 0)
            {
                Debug.LogWarning($"[InventoryManager] Cannot update slot visibility - no slots found. Storing slot count {newSlotCount} for later.");
                currentActiveSlots = newSlotCount;
                return;
            }
        }
        
        int previousSlotCount = currentActiveSlots;
        
        // Count only regular inventory slots (exclude gem slot)
        int maxRegularSlots = 0;
        foreach (var slot in inventorySlots)
        {
            if (slot != null && slot != gemSlot && !slot.isGemSlot)
            {
                maxRegularSlots++;
            }
        }
        
        currentActiveSlots = Mathf.Clamp(newSlotCount, 5, maxRegularSlots); // Min 5, max available regular slots
        
        Debug.Log($"[InventoryManager] Backpack system: Updated active slots from {previousSlotCount} to {currentActiveSlots} (max regular slots: {maxRegularSlots})");
        
        // If reducing slots, handle items in deactivated slots
        if (currentActiveSlots < previousSlotCount)
        {
            HandleSlotReduction(previousSlotCount, currentActiveSlots);
        }
        
        // Update UI visibility of slots
        UpdateSlotVisibility();
        
        // Save the change
        SaveInventoryData();
    }
    
    /// <summary>
    /// BACKPACK SYSTEM: Handle items when slots are reduced (due to death or backpack downgrade)
    /// </summary>
    private void HandleSlotReduction(int oldSlotCount, int newSlotCount)
    {
        Debug.Log($"[InventoryManager] Handling slot reduction from {oldSlotCount} to {newSlotCount}");
        
        // Check slots that are being deactivated - need to count regular slots properly
        int regularSlotIndex = 0;
        
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            var slot = inventorySlots[i];
            if (slot != null && !slot.isGemSlot)
            {
                // This is a regular slot - check if it should be deactivated
                if (regularSlotIndex >= newSlotCount && regularSlotIndex < oldSlotCount && !slot.IsEmpty)
                {
                    Debug.LogWarning($"[InventoryManager] Item {slot.CurrentItem?.itemName} lost due to backpack slot reduction! (regular slot {regularSlotIndex})");
                    slot.ClearSlot(); // Items are lost when backpack is downgraded/lost
                }
                regularSlotIndex++; // Only increment for regular slots
            }
        }
    }
    
    /// <summary>
    /// BACKPACK SYSTEM: Update visibility of inventory slots based on active slot count
    /// </summary>
    private void UpdateSlotVisibility()
    {
        // CRITICAL FIX: Safety check - don't try to update visibility if slots aren't initialized
        if (inventorySlots == null || inventorySlots.Count == 0)
        {
            Debug.LogWarning("[InventoryManager] UpdateSlotVisibility called but inventorySlots is empty - skipping");
            return;
        }
        
        int regularSlotIndex = 0; // Counter for regular slots only
        
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            var slot = inventorySlots[i];
            if (slot != null)
            {
                if (slot.isGemSlot || slot == gemSlot)
                {
                    // Gem slot is always visible
                    slot.gameObject.SetActive(true);
                }
                else
                {
                    // Regular inventory slot - check against currentActiveSlots
                    bool shouldBeVisible = regularSlotIndex < currentActiveSlots;
                    slot.gameObject.SetActive(shouldBeVisible);
                    
                    if (!shouldBeVisible && !slot.IsEmpty)
                    {
                        // Clear items from hidden slots
                        Debug.Log($"[InventoryManager] Clearing item from hidden slot {regularSlotIndex}: {slot.CurrentItem?.itemName}");
                        slot.ClearSlot();
                    }
                    
                    regularSlotIndex++; // Only increment for regular slots
                }
            }
        }
        
        Debug.Log($"[InventoryManager] Updated slot visibility - {currentActiveSlots} regular slots active (gem slot always visible)");
    }
    
    /// <summary>
    /// BACKPACK SYSTEM: Get current active slot count
    /// </summary>
    public int GetCurrentActiveSlotCount()
    {
        return currentActiveSlots;
    }
    
    /// <summary>
    /// BACKPACK SYSTEM: Reset to Tier 1 backpack (5 slots) - called on death
    /// </summary>
    public void ResetToTier1Backpack()
    {
        Debug.Log("[InventoryManager] Resetting to Tier 1 backpack due to death");
        
        // Reset backpack slot controller
        if (backpackSlot != null)
        {
            backpackSlot.ResetToTier1Backpack();
        }
        
        // Update slot count (this will handle item loss automatically)
        UpdateInventorySlotCount(5); // Tier 1 = 5 slots
    }
    
    /// <summary>
    /// VEST SYSTEM: Reset to T1 vest (1 plate) - called on death
    /// </summary>
    public void ResetToT1Vest()
    {
        Debug.Log("[InventoryManager] Resetting to T1 vest due to death");
        
        // Reset vest slot controller
        if (vestSlot != null)
        {
            vestSlot.ResetToT1Vest();
        }
    }
    
    /// <summary>
    /// Add item to specific slot (for drag-drop)
    /// </summary>
    public bool TryAddItemToSpecificSlot(ChestItemData item, int count, UnifiedSlot targetSlot)
    {
        if (item == null || count <= 0 || targetSlot == null) return false;
        
        if (!targetSlot.IsEmpty)
        {
            return false;
        }
        
        targetSlot.SetItem(item, count);
        OnItemAdded?.Invoke(item, count);
        OnInventoryChanged?.Invoke();
        SaveInventoryData(); // Auto-save on specific slot addition
        return true;
    }
    
    /// <summary>
    /// Move item between inventory slots
    /// </summary>
    public bool TryMoveItemWithinInventory(UnifiedSlot fromSlot, UnifiedSlot toSlot)
    {
        if (fromSlot == null || toSlot == null || fromSlot.IsEmpty) return false;
        
        if (toSlot.IsEmpty)
        {
            // Simple move
            toSlot.SetItem(fromSlot.CurrentItem, fromSlot.ItemCount);
            fromSlot.ClearSlot();
            OnInventoryChanged?.Invoke();
            SaveInventoryData(); // Auto-save on item move
            
            // Save to PersistentItemInventoryManager
            if (PersistentItemInventoryManager.Instance != null)
            {
                PersistentItemInventoryManager.Instance.UpdateFromInventoryManager(this);
                PersistentItemInventoryManager.Instance.SaveInventoryData();
            }
            
            return true;
        }
        else
        {
            // Swap items
            var tempItem = toSlot.CurrentItem;
            var tempCount = toSlot.ItemCount;
            
            toSlot.SetItem(fromSlot.CurrentItem, fromSlot.ItemCount);
            fromSlot.SetItem(tempItem, tempCount);
            OnInventoryChanged?.Invoke();
            SaveInventoryData(); // Auto-save on item swap
            
            // Save to PersistentItemInventoryManager
            if (PersistentItemInventoryManager.Instance != null)
            {
                PersistentItemInventoryManager.Instance.UpdateFromInventoryManager(this);
                PersistentItemInventoryManager.Instance.SaveInventoryData();
            }
            
            return true;
        }
    }
    
    /// <summary>
    /// Clear all inventory slots (but preserve gem slot if gems are present)
    /// ONLY called on player death - NEVER called automatically
    /// BACKPACK SYSTEM: Also resets backpack to Tier 1
    /// VEST SYSTEM: Also resets vest to T1
    /// </summary>
    public void ClearInventoryOnDeath()
    {
        Debug.Log("[InventoryManager] CLEARING INVENTORY ON DEATH - items will be lost, backpack reset to Tier 1, vest reset to T1");
        
        // BACKPACK SYSTEM: Reset to Tier 1 backpack first (this handles slot reduction)
        ResetToTier1Backpack();
        
        // VEST SYSTEM: Reset to T1 vest (1 plate capacity)
        ResetToT1Vest();
        
        foreach (var slot in inventorySlots)
        {
            // BUGFIX: Don't clear gem slot if we have gems loaded
            if (slot == gemSlot && currentGemCount > 0)
            {
                continue; // Skip clearing gem slot
            }
            
            slot.ClearSlot();
        }
        OnInventoryChanged?.Invoke();
        SaveInventoryData(); // Auto-save on inventory clear
    }
    
    /// <summary>
    /// Legacy method - redirects to death-specific clearing
    /// </summary>
    public void ClearInventory()
    {
        Debug.LogWarning("[InventoryManager] ClearInventory() called - redirecting to ClearInventoryOnDeath()");
        ClearInventoryOnDeath();
    }
    
    /// <summary>
    /// Remove item from inventory (for compatibility)
    /// </summary>
    public bool TryRemoveItem(ChestItemData item, int count = 1)
    {
        if (item == null || count <= 0) return false;
        
        // Find slot with this item
        var slot = inventorySlots.FirstOrDefault(s => !s.IsEmpty && s.CurrentItem == item);
        if (slot != null)
        {
            if (slot.ItemCount <= count)
            {
                slot.ClearSlot();
            }
            else
            {
                slot.SetItem(slot.CurrentItem, slot.ItemCount - count);
            }
            OnInventoryChanged?.Invoke();
            SaveInventoryData(); // Auto-save on item removal
            return true;
        }
        
        // Item not found in inventory
        return false;
    }

    // Legacy compatibility methods (simplified stubs)
    public bool IsInventoryOpen => false; // Simplified system doesn't track this
    public bool HasPersistedHandLevels() => false; // Simplified system doesn't persist hand levels
    public void SetVisibility(bool visible) { } // Simplified system doesn't manage UI visibility
    public (int, int) GetSavedHandLevels() => (1, 1); // Default hand levels

    /// <summary>
    /// Subscribe to PlayerProgression gem events (only in game context)
    /// </summary>
    private void SubscribeToGemEvents()
    {
        if (inventoryContext != InventoryContext.Game)
        {
            Debug.Log($"[InventoryManager] Skipping gem event subscription - Context: {inventoryContext}");
            return;
        }
        
        if (PlayerProgression.Instance != null)
        {
            PlayerProgression.OnSpendableGemsChanged += OnSpendableGemsChanged;
            Debug.Log("[InventoryManager] Subscribed to PlayerProgression gem events");
        }
        else
        {
            Debug.LogWarning("[InventoryManager] PlayerProgression.Instance is null - cannot subscribe to gem events");
        }
    }

    /// <summary>
    /// Unsubscribe from PlayerProgression gem events
    /// </summary>
    private void UnsubscribeFromGemEvents()
    {
        if (PlayerProgression.Instance != null)
        {
            PlayerProgression.OnSpendableGemsChanged -= OnSpendableGemsChanged;
        }
    }

    /// <summary>
    /// Called when PlayerProgression.spendablegems changes
    /// </summary>
    private void OnSpendableGemsChanged(int newGemCount)
    {
        
        // SIMPLE: Just store the count and update display
        currentGemCount = newGemCount;
        UpdateGemDisplay();
        
        // Save to inventory data immediately
        SaveGemCountToInventoryData(newGemCount);
    }

    /// <summary>
    /// Save inventory data to file - SIMPLE VERSION
    /// </summary>
    public void SaveInventoryData()
    {
        
        try
        {
            string dataPath = Path.Combine(Application.persistentDataPath, "inventory_data.json");
            
            InventorySaveData data;
            if (File.Exists(dataPath))
            {
                string loadedJson = File.ReadAllText(dataPath);
                data = JsonUtility.FromJson<InventorySaveData>(loadedJson);
                if (data == null)
                {
                    data = new InventorySaveData();
                }
            }
            else
            {
                data = new InventorySaveData();
            }
            
            // Simple: just update the gem count
            data.gemCount = currentGemCount;
            
            // SMART FIX: Save self-revive slot state (max 1 allowed)
            if (reviveSlot != null)
            {
                data.reviveCount = reviveSlot.HasRevives() ? 1 : 0;
            }
            else
            {
                data.reviveCount = 0;
            }
            
            if (data.inventorySlots == null)
            {
                data.inventorySlots = new List<SlotSaveData>();
            }
            
            // Save all inventory slots
            data.inventorySlots.Clear();
            foreach (var slot in inventorySlots)
            {
                if (slot != null)
                {
                    SlotSaveData slotData = GetSlotSaveData(slot);
                    data.inventorySlots.Add(slotData);
                }
            }
            
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(dataPath, json);
            
            // Also update PersistentItemInventoryManager for cross-scene persistence
            if (PersistentItemInventoryManager.Instance != null)
            {
                PersistentItemInventoryManager.Instance.UpdateFromInventoryManager(this);
                PersistentItemInventoryManager.Instance.SaveInventoryData();
            }
            
        }
        catch (System.Exception e)
        {
        }
    }

    /// <summary>
    /// Load inventory data from file - ENHANCED VERSION with detailed logging
    /// </summary>
    public void LoadInventoryData()
    {
        Debug.Log($"[InventoryManager] LoadInventoryData() called on {gameObject.name}");
        
        try
        {
            string dataPath = Path.Combine(Application.persistentDataPath, "inventory_data.json");
            
            if (!File.Exists(dataPath))
            {
                Debug.Log("[InventoryManager] No inventory save file found - starting fresh");
                currentGemCount = 0;
                UpdateGemDisplay();
                return;
            }
            
            string json = File.ReadAllText(dataPath);
            Debug.Log($"[InventoryManager] Loaded JSON: {json.Substring(0, Mathf.Min(200, json.Length))}...");
            
            InventorySaveData data = JsonUtility.FromJson<InventorySaveData>(json);
            
            if (data == null)
            {
                Debug.LogError("[InventoryManager] Failed to parse inventory JSON data");
                currentGemCount = 0;
                UpdateGemDisplay();
                return;
            }
            
            Debug.Log($"[InventoryManager] Parsed data - gemCount: {data.gemCount}, slots: {data.inventorySlots?.Count ?? 0}, reviveCount: {data.reviveCount}");
            
            // Load gem count and update gem slot with proper GemItemData
            currentGemCount = data.gemCount;
            UpdateGemDisplay();
            
            // CRITICAL: Load proper GemItemData into gem slot if we have gems
            if (gemSlot != null && currentGemCount > 0)
            {
                ChestItemData gemItemData = FindGemItemData();
                if (gemItemData != null)
                {
                    gemSlot.SetItem(gemItemData, currentGemCount);
                    Debug.Log($"[InventoryManager] Set inventory gem slot to {currentGemCount} gems using GemItemData");
                }
                else
                {
                    Debug.LogError("[InventoryManager] Could not find GemItemData to load inventory gems");
                }
            }
            else if (gemSlot != null && currentGemCount == 0)
            {
                gemSlot.ClearSlot();
                Debug.Log("[InventoryManager] Cleared inventory gem slot (0 gems saved)");
            }
            
            // CRITICAL: Only clear and load if we have valid slot references
            if (inventorySlots != null && inventorySlots.Count > 0)
            {
                // SAFE CLEARING: Only clear slots that belong to THIS InventoryManager context
                foreach (var slot in inventorySlots)
                {
                    if (slot != null)
                    {
                        // Don't clear gem slot if we have gems loaded
                        if (slot == gemSlot && currentGemCount > 0)
                        {
                            continue; // Skip clearing gem slot
                        }
                        
                        // CRITICAL FIX: Clear slots before loading to prevent duplicate items
                        // Only skip gem slot if it has gems loaded
                        if (slot != gemSlot || currentGemCount == 0)
                        {
                            slot.ClearSlot();
                        }
                    }
                }
                
                // Load slots if available
                if (data.inventorySlots != null)
                {
                    int slotsToLoad = Mathf.Min(data.inventorySlots.Count, inventorySlots.Count);
                    Debug.Log($"[InventoryManager] Loading {slotsToLoad} inventory slots");
                    
                    for (int i = 0; i < slotsToLoad; i++)
                    {
                        if (data.inventorySlots[i] != null && !string.IsNullOrEmpty(data.inventorySlots[i].itemPath))
                        {
                            if (i < inventorySlots.Count && inventorySlots[i] != null)
                            {
                                LoadSlotFromData(inventorySlots[i], data.inventorySlots[i]);
                            }
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[InventoryManager] No inventory slots found on {gameObject.name} - skipping item loading");
            }
            
            // SMART FIX: Restore self-revive slot state (preventing stacking abuse)
            if (reviveSlot != null)
            {
                // Clear first, then set if we have saved revive data
                reviveSlot.SetReviveCount(0);
                
                if (data.reviveCount > 0)
                {
                    // Enforce max 1 revive rule - even if save data is corrupted
                    reviveSlot.SetReviveCount(1);
                    Debug.Log("[InventoryManager] Restored 1 self-revive from save data");
                }
            }
            
            Debug.Log($"[InventoryManager] Successfully loaded inventory data on {gameObject.name}");
            
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[InventoryManager] Error loading inventory data: {e.Message}\n{e.StackTrace}");
            currentGemCount = 0;
            UpdateGemDisplay();
        }
    }

    /// <summary>
    /// Load ONLY gem count from save file (preserves current inventory items)
    /// </summary>
    public void LoadGemCountOnly()
    {
        
        try
        {
            string dataPath = Path.Combine(Application.persistentDataPath, "inventory_data.json");
            
            if (!File.Exists(dataPath))
            {
                return;
            }
            
            string json = File.ReadAllText(dataPath);
            InventorySaveData data = JsonUtility.FromJson<InventorySaveData>(json);
            
            if (data == null)
            {
                return;
            }
            
            // ONLY load gem count, don't touch inventory items
            currentGemCount = data.gemCount;
            UpdateGemDisplay();
            
        }
        catch (System.Exception e)
        {
        }
    }

    /// <summary>
    /// Load slot data into a UnifiedSlot
    /// </summary>
    private void LoadSlotFromData(UnifiedSlot slot, SlotData data)
    {
        if (string.IsNullOrEmpty(data.itemName))
        {
            slot.ClearSlot();
            return;
        }
        
        ChestItemData itemData = Resources.Load<ChestItemData>(data.itemPath);
        if (itemData != null)
        {
            slot.SetItem(itemData, data.itemCount);
        }
        else
        {
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
            data.itemCount = 0;
            data.itemPath = "";
            return data;
        }
        
        data.itemName = slot.CurrentItem?.name ?? "";
        data.itemCount = slot.ItemCount;
        data.itemPath = GetItemResourcePath(slot.CurrentItem);
        
        return data;
    }

    /// <summary>
    /// Update gem display and gem slot with proper GemItemData - PUBLIC for external access
    /// </summary>
    public void UpdateGemDisplay()
    {
        Debug.Log($"[InventoryManager] UpdateGemDisplay called - currentGemCount: {currentGemCount}, gemSlot: {(gemSlot != null ? gemSlot.name : "NULL")}");
        
        // Update gem slot with proper GemItemData
        if (gemSlot != null)
        {
            if (currentGemCount > 0)
            {
                ChestItemData gemItemData = FindGemItemData();
                if (gemItemData != null)
                {
                    gemSlot.SetItem(gemItemData, currentGemCount);
                    Debug.Log($"[InventoryManager] Updated gem slot with {currentGemCount} gems using GemItemData");
                }
                else
                {
                    Debug.LogError("[InventoryManager] Could not find GemItemData for gem slot update");
                }
            }
            else
            {
                gemSlot.ClearSlot();
                Debug.Log("[InventoryManager] Cleared gem slot (0 gems)");
            }
        }
        else
        {
            Debug.LogWarning("[InventoryManager] gemSlot is NULL - cannot update gem display");
        }
        
        // Update any gem UI text elements if they exist
        // if (gemCountText != null) gemCountText.text = currentGemCount.ToString();
    }

    /// <summary>
    /// SIMPLE: Save gem count directly to inventory data
    /// </summary>
    private void SaveGemCountToInventoryData(int gemCount)
    {
        
        try
        {
            string dataPath = Path.Combine(Application.persistentDataPath, "inventory_data.json");
            
            // Load or create inventory data
            InventorySaveData data;
            if (File.Exists(dataPath))
            {
                string loadedJson = File.ReadAllText(dataPath);
                data = JsonUtility.FromJson<InventorySaveData>(loadedJson);
                if (data == null)
                {
                    data = new InventorySaveData();
                }
            }
            else
            {
                data = new InventorySaveData();
            }
            
            // Simple: just update the gem count
            data.gemCount = gemCount;
            
            if (data.inventorySlots == null)
            {
                data.inventorySlots = new List<SlotSaveData>();
            }
            
            // Save updated data
            string updatedJson = JsonUtility.ToJson(data, true);
            File.WriteAllText(dataPath, updatedJson);
            
        }
        catch (System.Exception e)
        {
        }
    }

    /// <summary>
    /// Update gem slot from current PlayerProgression value or fallback based on context
    /// </summary>
    private void UpdateGemSlotFromProgression()
    {
        if (inventoryContext == InventoryContext.Game && PlayerProgression.Instance != null)
        {
            // GAME CONTEXT: Use actual gem count from PlayerProgression
            int currentGems = PlayerProgression.Instance.currentSpendableGems;
            currentGemCount = currentGems;
            UpdateGemDisplay();
            Debug.Log($"[InventoryManager] Game context - loaded {currentGems} gems from PlayerProgression");
        }
        else
        {
            // MENU CONTEXT or no PlayerProgression: Use saved gem count
            int fallbackGems = GetFallbackGemCount();
            currentGemCount = fallbackGems;
            UpdateGemDisplay();
            Debug.Log($"[InventoryManager] {inventoryContext} context - using fallback gems: {fallbackGems}");
        }
    }

    /// <summary>
    /// Get fallback gem count for menu scenes (where PlayerProgression doesn't exist)
    /// </summary>
    private int GetFallbackGemCount()
    {
        // For now, return the stored gem count or 0
        return currentGemCount;
    }
    
    /// <summary>
    /// SIMPLE: Add gems to the current gem count (called when player collects gems)
    /// </summary>
    public void AddGems(int count)
    {
        currentGemCount += count;
        
        // Fire event for DoubleGemsTracker and other systems
        OnGemCollected?.Invoke(count);
        
        UpdateGemDisplay();
        SaveGemCountToInventoryData(currentGemCount);
    }
    
    /// <summary>
    /// SIMPLE: Get current gem count
    /// </summary>
    public int GetGemCount()
    {
        return currentGemCount;
    }
    
    /// <summary>
    /// SIMPLE: Set gem count (for loading saved data)
    /// </summary>
    public void SetGemCount(int count)
    {
        currentGemCount = count;
        UpdateGemDisplay();
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
        
        Debug.Log($"[InventoryManager] Getting resource path for item: '{item.name}' -> cleaned: '{itemName}', type: '{item.itemType}'");
        
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
                    Debug.Log($"[InventoryManager] Found keycard at path: '{path}'");
                    return path;
                }
            }
            
            Debug.LogWarning($"[InventoryManager] ⚠️ KEYCARD '{item.name}' NOT FOUND in Resources folder! Keycards must be in a Resources folder to persist.");
            Debug.LogWarning($"[InventoryManager] Please move keycard assets from 'Assets/prefabs_made/KEYCARDS/Keycards/' to 'Assets/Resources/Keycards/'");
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
                Debug.Log($"[InventoryManager] Found working path: '{path}' for item '{item.name}'");
                return path;
            }
        }
        
        Debug.LogWarning($"[InventoryManager] No valid resource path found for item '{item.name}'. Tried: {string.Join(", ", possiblePaths)}");
        return $"Items/{itemName}"; // Fallback to default
    }
    
    /// <summary>
    /// Get save data for a specific slot (using SlotSaveData format)
    /// </summary>
    private SlotSaveData GetSlotSaveData(UnifiedSlot slot)
    {
        var slotData = new SlotSaveData();
        
        if (!slot.IsEmpty)
        {
            slotData.itemName = slot.CurrentItem?.itemName ?? "";
            slotData.itemPath = GetItemResourcePath(slot.CurrentItem);
            slotData.itemCount = slot.ItemCount;
        }
        else
        {
            slotData.itemName = "";
            slotData.itemPath = "";
            slotData.itemCount = 0;
        }
        
        return slotData;
    }
    
    /// <summary>
    /// Load data into a specific slot - ENHANCED with fallback path attempts including keycards
    /// </summary>
    private void LoadSlotFromData(UnifiedSlot slot, SlotSaveData data)
    {
        try
        {
            if (!string.IsNullOrEmpty(data.itemPath))
            {
                Debug.Log($"[InventoryManager] Attempting to load item '{data.itemName}' from path '{data.itemPath}'");
                
                ChestItemData item = Resources.Load<ChestItemData>(data.itemPath);
                if (item != null)
                {
                    slot.SetItem(item, data.itemCount);
                    Debug.Log($"[InventoryManager] Successfully loaded {data.itemName} x{data.itemCount} into slot");
                    return;
                }
                
                // Primary path failed, try alternative paths
                Debug.LogWarning($"[InventoryManager] Primary path '{data.itemPath}' failed, trying alternatives...");
                
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
                        Debug.Log($"[InventoryManager] Loaded {data.itemName} x{data.itemCount} using alternative path: '{altPath}'");
                        return;
                    }
                }
                
                if (isKeycard)
                {
                    Debug.LogError($"[InventoryManager] ⚠️ KEYCARD '{data.itemName}' could not be loaded! Keycards must be in 'Assets/Resources/Keycards/' folder to persist.");
                }
                
                Debug.LogError($"[InventoryManager] Could not load item '{data.itemName}' from any path. Tried: {data.itemPath}, {string.Join(", ", alternativePaths)}");
                slot.ClearSlot();
            }
            else
            {
                Debug.Log($"[InventoryManager] Empty item path for slot - clearing");
                slot.ClearSlot();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[InventoryManager] Exception loading slot data for '{data.itemName}': {e.Message}\n{e.StackTrace}");
            slot.ClearSlot();
        }
    }
    
    /// <summary>
    /// Handle drag and drop operations within inventory slots for rearrangement
    /// ⚔️ WEAPON SLOT SUPPORT: Also handles weapon slot ↔ inventory slot transfers
    /// </summary>
    private void HandleInventoryItemDropped(UnifiedSlot sourceSlot, UnifiedSlot targetSlot)
    {
        if (sourceSlot == null || targetSlot == null)
        {
            return;
        }
        
        if (sourceSlot == targetSlot)
        {
  
            return;
        }
        
        if (sourceSlot.IsEmpty)
        {
            return;
        }
        
        // ⚔️ NEW: Allow drag/drop between weapon slots and inventory slots
        // Valid combinations:
        // 1. Inventory → Inventory (original functionality)
        // 2. Inventory ↔ Weapon Slot (new functionality)
        bool sourceIsInventory = inventorySlots.Contains(sourceSlot);
        bool targetIsInventory = inventorySlots.Contains(targetSlot);
        bool sourceIsWeaponSlot = (sourceSlot == rightHandWeaponSlot || sourceSlot == leftHandWeaponSlot);
        bool targetIsWeaponSlot = (targetSlot == rightHandWeaponSlot || targetSlot == leftHandWeaponSlot);
        
        // Reject if neither source nor target is an inventory/weapon slot
        if (!sourceIsInventory && !sourceIsWeaponSlot)
        {
            Debug.Log($"[InventoryManager] ❌ Drag rejected: Source slot {sourceSlot.gameObject.name} is not inventory or weapon slot");
            return;
        }
        
        if (!targetIsInventory && !targetIsWeaponSlot)
        {
            Debug.Log($"[InventoryManager] ❌ Drag rejected: Target slot {targetSlot.gameObject.name} is not inventory or weapon slot");
            return;
        }
        
        Debug.Log($"[InventoryManager] 🔄 DRAG/DROP: {sourceSlot.gameObject.name} (weapon:{sourceIsWeaponSlot}) → {targetSlot.gameObject.name} (weapon:{targetIsWeaponSlot}) | Item: {sourceSlot.CurrentItem?.itemName}");
        
        
        // Handle the operation based on target slot state
        if (targetSlot.IsEmpty)
        {
            // Simple move to empty slot
            MoveInventoryItemToEmptySlot(sourceSlot, targetSlot);
        }
        else
        {
            // Try to stack or swap items
            if (CanStackInventoryItems(sourceSlot.CurrentItem, targetSlot.CurrentItem))
            {
                // Stack items together
                StackInventoryItems(sourceSlot, targetSlot);
            }
            else
            {
                // Swap items between slots
                SwapInventoryItems(sourceSlot, targetSlot);
            }
        }
        
        // Save immediately after any inventory rearrangement
        SaveInventoryData();
    }
    
    /// <summary>
    /// Handle double-click on inventory slots - transfer to chest if chest is open, or equip self-revive
    /// FULLY BIDIRECTIONAL: Inventory → Chest with unified stacking
    /// </summary>
    private void HandleInventoryDoubleClick(UnifiedSlot slot)
    {
        if (slot.IsEmpty) return;
        
        ChestItemData currentItem = slot.CurrentItem; // Cache before clearing
        int currentCount = slot.ItemCount;
        
        Debug.Log($"[InventoryManager] 🔄 DOUBLE-CLICK on inventory slot: {currentCount}x {currentItem.itemName}");
        Debug.Log($"   Item ID: {currentItem.itemID}, Type: {currentItem.itemType}");
        
        // Check if this is a self-revive item - auto-equip to revive slot if empty
        if (IsSelfReviveItem(currentItem))
        {
            if (reviveSlot != null)
            {
                if (reviveSlot.IsFull())
                {
                    Debug.Log($"[InventoryManager] ❌ Cannot equip self-revive - revive slot already has a self-revive equipped!");
                    return;
                }
                
                // Equip to revive slot
                if (reviveSlot.AddRevives(1))
                {
                    // Remove from inventory slot
                    slot.ClearSlot();
                    SaveInventoryData();
                    Debug.Log($"[InventoryManager] ✅ Auto-equipped {currentItem?.itemName} to revive slot via double-click");
                    return;
                }
            }
            else
            {
                Debug.LogError($"[InventoryManager] ❌ Cannot equip self-revive - revive slot reference is null!");
                return;
            }
        }
        
        // Try to transfer to chest if chest is open
        ChestInteractionSystem chestSystem = FindActiveChestSystem();
        if (chestSystem != null && chestSystem.IsChestOpen())
        {
            Debug.Log($"[InventoryManager] 🔄 DOUBLE-CLICK TRANSFER (Inventory→Chest): {currentCount}x {currentItem.itemName}");
            
            bool transferred = TryTransferToChest(slot, chestSystem);
            if (transferred)
            {
                // Save inventory immediately after transfer
                SaveInventoryData();
                Debug.Log($"[InventoryManager] ✅ DOUBLE-CLICK SUCCESS: Transferred {currentCount}x {currentItem.itemName} to chest");
            }
            else
            {
                Debug.LogWarning($"[InventoryManager] ❌ DOUBLE-CLICK FAILED: Could not transfer {currentItem.itemName} to chest (chest full?)");
            }
        }
        else
        {
            Debug.Log($"[InventoryManager] No chest open - double-click had no effect");
        }
    }
    
    /// <summary>
    /// Find active ChestInteractionSystem in the scene
    /// </summary>
    private ChestInteractionSystem FindActiveChestSystem()
    {
        ChestInteractionSystem[] chestSystems = FindObjectsByType<ChestInteractionSystem>(FindObjectsSortMode.None);
        foreach (var chestSystem in chestSystems)
        {
            if (chestSystem != null && chestSystem.gameObject.activeInHierarchy)
            {
                return chestSystem;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Try to transfer item from inventory slot to chest using UnifiedSlot system
    /// ATOMIC OPERATION: All-or-nothing with proper chest state update
    /// </summary>
    private bool TryTransferToChest(UnifiedSlot inventorySlot, ChestInteractionSystem chestSystem)
    {
        if (inventorySlot.IsEmpty)
        {
            Debug.LogWarning("[InventoryManager] TryTransferToChest: Source slot is empty");
            return false;
        }
        
        // Get chest slots from ChestInteractionSystem
        UnifiedSlot[] chestSlots = chestSystem.GetChestSlots();
        if (chestSlots == null || chestSlots.Length == 0)
        {
            Debug.LogWarning("[InventoryManager] TryTransferToChest: No chest slots available");
            return false;
        }
        
        // ATOMIC: Cache data before modifications
        ChestItemData itemToTransfer = inventorySlot.CurrentItem;
        int transferCount = inventorySlot.ItemCount;
        
        // VALIDATION
        if (itemToTransfer == null || transferCount <= 0)
        {
            Debug.LogError("[InventoryManager] ❌ ATOMIC VALIDATION: Invalid item or count!");
            return false;
        }
        
        Debug.Log($"[InventoryManager] 🔄 ATOMIC TRANSFER (Inventory→Chest): {transferCount}x {itemToTransfer.itemName}");
        
        // UNIFIED STACKING: First try to stack with existing identical items in chest
        foreach (var chestSlot in chestSlots)
        {
            if (!chestSlot.IsEmpty && 
                chestSlot.CurrentItem != null &&
                chestSlot.CurrentItem.IsSameItem(itemToTransfer))
            {
                // ATOMIC: Stack items together
                int combinedCount = chestSlot.ItemCount + transferCount;
                chestSlot.SetItem(chestSlot.CurrentItem, combinedCount);
                inventorySlot.ClearSlot();
                
                // CRITICAL: Verify source cleared
                if (!inventorySlot.IsEmpty)
                {
                    Debug.LogError("[InventoryManager] 🚨 ATOMIC FAILURE: Inventory slot NOT cleared! Forcing clear...");
                    inventorySlot.ClearSlot();
                }
                
                // CRITICAL: Update chest persistent loot!
                chestSystem.UpdatePersistentChestLoot();
                
                Debug.Log($"[InventoryManager] ✅ ATOMIC COMPLETE: Stacked {transferCount}x {itemToTransfer.itemName} in chest (total: {combinedCount})");
                return true;
            }
        }
        
        // If no stacking possible, find empty slot
        foreach (var chestSlot in chestSlots)
        {
            if (chestSlot.IsEmpty)
            {
                // ATOMIC: Move to empty chest slot
                chestSlot.SetItem(itemToTransfer, transferCount);
                inventorySlot.ClearSlot();
                
                // CRITICAL: Verify source cleared
                if (!inventorySlot.IsEmpty)
                {
                    Debug.LogError("[InventoryManager] 🚨 ATOMIC FAILURE: Inventory slot NOT cleared! Forcing clear...");
                    inventorySlot.ClearSlot();
                }
                
                // CRITICAL: Update chest persistent loot!
                chestSystem.UpdatePersistentChestLoot();
                
                Debug.Log($"[InventoryManager] ✅ ATOMIC COMPLETE: Transferred {transferCount}x {itemToTransfer.itemName} to empty chest slot");
                return true;
            }
        }
        
        // If we get here, chest is full
        Debug.LogWarning($"[InventoryManager] ❌ ATOMIC FAILED: Chest is full, cannot transfer {itemToTransfer.itemName}");
        return false;
    }
    
    /// <summary>
    /// Move item from source to empty target slot within inventory
    /// ATOMIC OPERATION: Validated move with verification
    /// </summary>
    private void MoveInventoryItemToEmptySlot(UnifiedSlot sourceSlot, UnifiedSlot targetSlot)
    {
        // ATOMIC: Capture item data before clearing source
        ChestItemData item = sourceSlot.CurrentItem;
        int count = sourceSlot.ItemCount;
        
        // VALIDATION
        if (item == null || count <= 0)
        {
            Debug.LogError("[InventoryManager] ❌ ATOMIC VALIDATION: Invalid item or count in MoveInventoryItemToEmptySlot!");
            return;
        }
        
        // ATOMIC: Move item to target slot
        targetSlot.SetItem(item, count);
        sourceSlot.ClearSlot();
        
        // VALIDATION: Verify source cleared
        if (!sourceSlot.IsEmpty)
        {
            Debug.LogError("[InventoryManager] 🚨 ATOMIC FAILURE: Source slot NOT cleared! Forcing clear...");
            sourceSlot.ClearSlot();
        }
        
        Debug.Log($"[InventoryManager] ✅ ATOMIC COMPLETE: Moved {count}x {item.itemName} within inventory");
    }
    
    /// <summary>
    /// Stack compatible items together within inventory
    /// ATOMIC OPERATION: Validated stacking with verification
    /// </summary>
    private void StackInventoryItems(UnifiedSlot sourceSlot, UnifiedSlot targetSlot)
    {
        // ATOMIC: Capture data before modifications
        ChestItemData sourceItem = sourceSlot.CurrentItem;
        int sourceCount = sourceSlot.ItemCount;
        int targetCount = targetSlot.ItemCount;
        
        // VALIDATION
        if (sourceItem == null || sourceCount <= 0)
        {
            Debug.LogError("[InventoryManager] ❌ ATOMIC VALIDATION: Invalid source in StackInventoryItems!");
            return;
        }
        
        // ATOMIC: Combine counts
        int totalCount = sourceCount + targetCount;
        
        // Update target slot with combined count
        targetSlot.SetItem(targetSlot.CurrentItem, totalCount);
        sourceSlot.ClearSlot();
        
        // VALIDATION: Verify source cleared
        if (!sourceSlot.IsEmpty)
        {
            Debug.LogError("[InventoryManager] 🚨 ATOMIC FAILURE: Source slot NOT cleared after stacking! Forcing clear...");
            sourceSlot.ClearSlot();
        }
        
        Debug.Log($"[InventoryManager] ✅ ATOMIC COMPLETE: Stacked {sourceCount}x {sourceItem.itemName} (total: {totalCount})");
    }
    
    /// <summary>
    /// Swap items between two occupied inventory slots
    /// ATOMIC OPERATION: Validated swap
    /// </summary>
    private void SwapInventoryItems(UnifiedSlot sourceSlot, UnifiedSlot targetSlot)
    {
        // ATOMIC: Capture both items before swapping
        ChestItemData sourceItem = sourceSlot.CurrentItem;
        int sourceCount = sourceSlot.ItemCount;
        
        ChestItemData targetItem = targetSlot.CurrentItem;
        int targetCount = targetSlot.ItemCount;
        
        // VALIDATION
        if (sourceItem == null || sourceCount <= 0 || targetItem == null || targetCount <= 0)
        {
            Debug.LogError("[InventoryManager] ❌ ATOMIC VALIDATION: Invalid items in SwapInventoryItems!");
            return;
        }
        
        // ATOMIC: Perform the swap
        sourceSlot.SetItem(targetItem, targetCount);
        targetSlot.SetItem(sourceItem, sourceCount);
        
        Debug.Log($"[InventoryManager] ✅ ATOMIC COMPLETE: Swapped {sourceItem.itemName} with {targetItem.itemName}");
    }
    
    /// <summary>
    /// Check if two items can be stacked together within inventory
    /// UNIFIED STACKING: Uses IsSameItem() for consistent behavior
    /// </summary>
    private bool CanStackInventoryItems(ChestItemData item1, ChestItemData item2)
    {
        if (item1 == null || item2 == null) return false;
        
        // UNIFIED: Use IsSameItem() for all stacking checks
        bool canStack = item1.IsSameItem(item2);
        
        Debug.Log($"[InventoryManager] CanStackInventoryItems: {item1.itemName} + {item2.itemName} = {canStack}");
        
        return canStack;
    }
    
    /// <summary>
    /// Data structure for saving inventory to JSON
    /// </summary>
    [System.Serializable]
    public class InventorySaveData
    {
        public int gemCount;
        public List<SlotSaveData> inventorySlots;
        public int reviveCount = 0; // Self-revive slot: 0 = empty, 1 = has revive (max 1 allowed)
    }
    
    /// <summary>
    /// Data structure for saving individual slot data - UNIFIED with StashManager
    /// </summary>
    [System.Serializable]
    public class SlotSaveData
    {
        public string itemName = "";
        public string itemPath = "";
        public int itemCount = 0;
    }
    
    /// <summary>
    /// Check if a slot is owned by THIS InventoryManager (using Inspector references)
    /// </summary>
    private bool IsOwnedByThisInventoryManager(UnifiedSlot slot)
    {
        // Check if this slot is in our inventorySlots list (assigned via Inspector)
        return inventorySlots.Contains(slot) || slot == gemSlot;
    }
    
    /// <summary>
    /// SAFE load that never clears stash slots - only loads inventory slots
    /// </summary>
    public void LoadInventoryDataSafe()
    {
        Debug.Log($"[InventoryManager] LoadInventoryDataSafe() called - will not clear any slots");
        
        try
        {
            string dataPath = Path.Combine(Application.persistentDataPath, "inventory_data.json");
            
            if (!File.Exists(dataPath))
            {
                Debug.Log("[InventoryManager] No inventory save file found - starting fresh");
                currentGemCount = 0;
                UpdateGemDisplay();
                return;
            }
            
            string json = File.ReadAllText(dataPath);
            InventorySaveData data = JsonUtility.FromJson<InventorySaveData>(json);
            
            if (data == null)
            {
                Debug.LogError("[InventoryManager] Failed to parse inventory JSON data");
                return;
            }
            
            // Load gem count and update gem slot
            currentGemCount = data.gemCount;
            UpdateGemDisplay();
            
            // CRITICAL FIX: NEVER load over existing items - only load into truly empty slots
            if (data.inventorySlots != null && inventorySlots != null)
            {
                int slotsToLoad = Mathf.Min(data.inventorySlots.Count, inventorySlots.Count);
                Debug.Log($"[InventoryManager] Loading {slotsToLoad} inventory slots (SAFE - only into empty slots)");
                
                for (int i = 0; i < slotsToLoad; i++)
                {
                    if (data.inventorySlots[i] != null && !string.IsNullOrEmpty(data.inventorySlots[i].itemPath))
                    {
                        if (i < inventorySlots.Count && inventorySlots[i] != null)
                        {
                            // CRITICAL: Only load into completely empty slots - NEVER overwrite existing items
                            if (inventorySlots[i].IsEmpty && IsOwnedByThisInventoryManager(inventorySlots[i]))
                            {
                                LoadSlotFromData(inventorySlots[i], data.inventorySlots[i]);
                                Debug.Log($"[InventoryManager] Loaded item into empty slot {i}");
                            }
                            else if (!inventorySlots[i].IsEmpty)
                            {
                                Debug.Log($"[InventoryManager] Slot {i} has existing item - preserving it, not loading save data");
                            }
                        }
                    }
                }
            }
            
            Debug.Log($"[InventoryManager] Successfully loaded inventory data safely");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[InventoryManager] Error loading inventory data safely: {e.Message}");
        }
    }
    
    /// <summary>
    /// Coroutine: Activate sword mode on the next frame after equipping
    /// This ensures WeaponEquipmentManager processes the slot change first via OnSlotChanged event
    /// Then we activate sword mode to complete the seamless pickup flow
    /// ⭐ STATIC: Can run on any MonoBehaviour (runs on player since this is singleton)
    /// </summary>
    private static System.Collections.IEnumerator ActivateSwordModeNextFrame(PlayerShooterOrchestrator playerShooter)
    {
        // Wait for next frame to ensure WeaponEquipmentManager.CheckRightHandEquipment() has executed
        yield return null;
        
        // Now activate sword mode (if sword is available)
        if (playerShooter.CanUseSwordMode() && !playerShooter.IsSwordModeActive)
        {
            playerShooter.ToggleSwordMode();
            Debug.Log("[InventoryManager] ⚔️ AUTO-ACTIVATED sword mode after pickup!");
        }
        else
        {
            Debug.LogWarning($"[InventoryManager] ⚠️ Could not auto-activate sword mode - CanUse: {playerShooter.CanUseSwordMode()}, IsActive: {playerShooter.IsSwordModeActive}");
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
                Debug.LogError("[InventoryManager] Could not load GemItemData from Resources/Items/GemItemData");
                return null;
            }
            
            Debug.Log($"[InventoryManager] Successfully loaded GemItemData: {gemData.itemName} with icon: {gemData.itemIcon?.name}");
            return gemData;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[InventoryManager] Exception loading GemItemData: {e.Message}");
            return null;
        }
    }
}

/// <summary>
/// Enum to define InventoryManager context for proper dual-scene handling
/// </summary>
[System.Serializable]
public enum InventoryContext
{
    [Tooltip("Game context - attached to player, handles PlayerProgression, TAB key toggle")]
    Game,
    
    [Tooltip("Menu context - attached to menu panel, no PlayerProgression, UI managed by menu")]
    Menu
}
