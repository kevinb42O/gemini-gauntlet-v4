using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

/// <summary>
/// Animation types for recipe info panel
/// </summary>
public enum RecipePanelAnimationType
{
    Fade,
    SlideFromLeft,
    SlideFromRight,
    SlideFromTop,
    SlideFromBottom
}

/// <summary>
/// FORGE context - determines where crafted items are routed
/// </summary>
public enum ForgeContext
{
    Menu,  // Crafted items go to stash/inventory (menu scene)
    Game   // Crafted items go to game inventory (in-game scene)
}

[System.Serializable]
public class ForgeRecipe
{
    [Header("Recipe Configuration")]
    [Tooltip("Drag up to 4 ingredient items here (order doesn't matter)")]
    public ChestItemData[] requiredIngredients = new ChestItemData[4];
    [Tooltip("The item that will be created when crafting")]
    public ChestItemData outputItem;
    [Tooltip("How many output items to create")]
    public int outputCount = 1;
    
    /// <summary>
    /// Check if this recipe matches the given ingredients (order doesn't matter, counts duplicates)
    /// </summary>
    public bool MatchesIngredients(List<ChestItemData> ingredients)
    {
        if (ingredients == null || outputItem == null) return false;
        
        // Get non-null required ingredients
        var requiredList = requiredIngredients.Where(item => item != null).ToList();
        
        // Must have exact same number of ingredients
        if (ingredients.Count != requiredList.Count) return false;
        
        // Count occurrences of each required ingredient type
        var requiredCounts = new Dictionary<ChestItemData, int>();
        foreach (var required in requiredList)
        {
            if (requiredCounts.ContainsKey(required))
                requiredCounts[required]++;
            else
                requiredCounts[required] = 1;
        }
        
        // Count occurrences of each provided ingredient type
        var providedCounts = new Dictionary<ChestItemData, int>();
        foreach (var provided in ingredients)
        {
            if (providedCounts.ContainsKey(provided))
                providedCounts[provided]++;
            else
                providedCounts[provided] = 1;
        }
        
        // Check if provided ingredients match required ingredients exactly (including counts)
        foreach (var requiredPair in requiredCounts)
        {
            if (!providedCounts.ContainsKey(requiredPair.Key) || 
                providedCounts[requiredPair.Key] != requiredPair.Value)
            {
                return false;
            }
        }
        
        // Also check that we don't have extra ingredient types
        foreach (var providedPair in providedCounts)
        {
            if (!requiredCounts.ContainsKey(providedPair.Key))
            {
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Get the number of required ingredients for this recipe
    /// </summary>
    public int GetRequiredIngredientCount()
    {
        return requiredIngredients.Count(item => item != null);
    }
}

/// <summary>
/// FORGE Manager - Handles crafting system with 4 input slots and 1 output slot
/// Integrates seamlessly with existing UnifiedSlot system
/// </summary>
public class ForgeManager : MonoBehaviour
{
    public static ForgeManager Instance { get; private set; }
    
    [Header("FORGE UI References")]
    [Tooltip("The 4 input slots for crafting ingredients")]
    public UnifiedSlot[] inputSlots = new UnifiedSlot[4];
    [Tooltip("The output slot that shows the crafted item")]
    public UnifiedSlot outputSlot;
    
    [Header("FORGE Visual Feedback")]
    [Tooltip("Image shown when no valid recipe is detected (default state)")]
    public Image invalidRecipeImage;
    [Tooltip("Button shown when valid recipe is detected - click to craft")]
    public Button craftButton;
    [Tooltip("Image shown during processing (pulsating effect)")]
    public Image processingImage;
    [Tooltip("Text field for countdown display during processing (5 seconds)")]
    public TextMeshProUGUI countdownText;
    [Tooltip("Progress slider shown during crafting process")]
    public Slider progressSlider;
    
    [Header("Progress Slider Settings")]
    [Tooltip("Time in seconds for slider fade out animation after crafting")]
    [Range(0.5f, 3f)]
    public float sliderFadeOutTime = 1.5f;
    [Tooltip("Smooth progress animation speed multiplier")]
    [Range(1f, 10f)]
    public float progressSmoothness = 3f;
    
    [Header("Recipe Info Panel")]
    [Tooltip("Button to toggle recipe info panel visibility")]
    public Button recipeInfoButton;
    [Tooltip("Panel containing all recipe information")]
    public GameObject recipeInfoPanel;
    [Tooltip("Content area where recipe entries will be dynamically created")]
    public Transform recipeContentArea;
    [Tooltip("Prefab for individual recipe entries (should have ingredient slots and output slot)")]
    public GameObject recipeEntryPrefab;
    
    [Header("Recipe Panel Animation")]
    [Tooltip("Time for panel slide/fade animation")]
    [Range(0.2f, 1f)]
    public float panelAnimationTime = 0.3f;
    [Tooltip("Panel animation type")]
    public RecipePanelAnimationType animationType = RecipePanelAnimationType.Fade;
    
    [Header("Recipe System")]
    [Tooltip("All available crafting recipes")]
    public List<ForgeRecipe> availableRecipes = new List<ForgeRecipe>();
    
    [Header("UI Transparency Settings")]
    [Tooltip("Alpha when no valid recipe is found")]
    [Range(0f, 1f)]
    public float inactiveAlpha = 0.5f;
    [Tooltip("Alpha when valid recipe is detected")]
    [Range(0f, 1f)]
    public float activeAlpha = 1f;
    
    [Header("Processing Settings")]
    [Tooltip("Time in seconds for crafting process")]
    public float craftingTime = 5f;
    
    [Header("Audio System")]
    [Tooltip("Audio source for playing FORGE sounds")]
    public AudioSource audioSource;
    [Tooltip("Sound played when valid recipe is detected")]
    public AudioClip recipeDetectedSound;
    [Tooltip("Sound played when processing starts")]
    public AudioClip processingSound;
    [Tooltip("Sound played when processing is complete")]
    public AudioClip processingCompleteSound;
    
    // Current state
    private ForgeRecipe currentValidRecipe = null;
    private bool isProcessing = false;
    private InventoryManager inventoryManager;
    private StashManager stashManager;
    
    // Context awareness for game vs menu routing
    private ForgeContext currentContext = ForgeContext.Menu;
    
    // Source tracking system - remembers where each forge input slot's item came from
    private Dictionary<UnifiedSlot, UnifiedSlot> forgeInputSourceMap = new Dictionary<UnifiedSlot, UnifiedSlot>();
    
    // Progress slider animation
    private Coroutine sliderFadeCoroutine;
    
    // Recipe info panel state
    private bool isRecipePanelVisible = false;
    private Coroutine panelAnimationCoroutine;
    private List<GameObject> createdRecipeEntries = new List<GameObject>();
    
    // Public properties for external access
    public bool IsProcessing => isProcessing;
    public bool HasValidRecipe => currentValidRecipe != null;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Get references to existing managers
        inventoryManager = InventoryManager.Instance;
        stashManager = FindFirstObjectByType<StashManager>();
    }
    
    void Start()
    {
        InitializeForgeSlots();
        SetupCraftButton();
        SetupRecipeInfoPanel();
        UpdateForgeState();
    }
    
    /// <summary>
    /// Initialize all FORGE slots and connect events
    /// </summary>
    void InitializeForgeSlots()
    {
        // Setup input slots
        for (int i = 0; i < inputSlots.Length; i++)
        {
            if (inputSlots[i] != null)
            {
                // Ensure input slots are marked correctly
                inputSlots[i].isForgeInputSlot = true;
                inputSlots[i].isForgeOutputSlot = false;
                
                // Subscribe to events
                inputSlots[i].OnDoubleClick += HandleInputSlotDoubleClick;
                inputSlots[i].OnItemDropped += HandleItemDropped;
            }
        }
        
        // Setup output slot
        if (outputSlot != null)
        {
            outputSlot.isForgeInputSlot = false;
            outputSlot.isForgeOutputSlot = true;
            
            // Subscribe to events
            outputSlot.OnDoubleClick += HandleOutputSlotDoubleClick;
        }
        
        Debug.Log("FORGE: Initialized all slots");
    }
    
    /// <summary>
    /// Setup craft button and invalid recipe image
    /// </summary>
    void SetupCraftButton()
    {
        // Setup craft button click listener
        if (craftButton != null)
        {
            craftButton.onClick.RemoveAllListeners();
            craftButton.onClick.AddListener(CraftItem);
        }
        
        // Initialize UI state - show invalid image, hide button
        SetForgeUIState(false);
    }
    
    /// <summary>
    /// Setup recipe info panel and button
    /// </summary>
    void SetupRecipeInfoPanel()
    {
        // Setup recipe info button click listener
        if (recipeInfoButton != null)
        {
            recipeInfoButton.onClick.RemoveAllListeners();
            recipeInfoButton.onClick.AddListener(ToggleRecipeInfoPanel);
            Debug.Log("FORGE: Recipe info button initialized");
        }
        
        // Initialize panel as hidden
        if (recipeInfoPanel != null)
        {
            recipeInfoPanel.SetActive(false);
            isRecipePanelVisible = false;
            Debug.Log("FORGE: Recipe info panel initialized as hidden");
        }
        
        // Generate recipe entries
        GenerateRecipeEntries();
    }
    
    /// <summary>
    /// Handle double-click on input slots - return item to original source location
    /// </summary>
    void HandleInputSlotDoubleClick(UnifiedSlot slot)
    {
        if (slot.IsEmpty) return;
        
        Debug.Log($"FORGE: Double-click on input slot with {slot.CurrentItem.itemName}");
        
        ChestItemData itemToReturn = slot.CurrentItem;
        int countToReturn = slot.ItemCount;
        bool success = false;
        
        // Check if we have a tracked source for this slot
        if (forgeInputSourceMap.ContainsKey(slot))
        {
            UnifiedSlot originalSource = forgeInputSourceMap[slot];
            
            // Verify the original source still exists and is valid
            if (originalSource != null && originalSource.gameObject != null)
            {
                // Try to return to original source if it's empty or can stack
                if (originalSource.IsEmpty)
                {
                    originalSource.SetItem(itemToReturn, countToReturn);
                    success = true;
                    Debug.Log($"FORGE: Returned {itemToReturn.itemName} x{countToReturn} to original source: {originalSource.name}");
                    
                    // Trigger save for the original source container
                    TriggerSaveForSlot(originalSource);
                }
                else if (originalSource.CurrentItem == itemToReturn)
                {
                    // Same item type - try to stack
                    originalSource.SetItem(itemToReturn, originalSource.ItemCount + countToReturn);
                    success = true;
                    Debug.Log($"FORGE: Stacked {itemToReturn.itemName} x{countToReturn} back to original source: {originalSource.name} (total: {originalSource.ItemCount})");
                    
                    // Trigger save for the original source container
                    TriggerSaveForSlot(originalSource);
                }
                else
                {
                    Debug.Log($"FORGE: Original source {originalSource.name} is occupied with different item - falling back to inventory/stash");
                }
            }
            else
            {
                Debug.Log($"FORGE: Original source no longer exists - falling back to inventory/stash");
            }
            
            // Clear the source mapping regardless of success
            forgeInputSourceMap.Remove(slot);
        }
        else
        {
            Debug.Log($"FORGE: No source tracking found for slot {slot.name} - falling back to inventory/stash");
        }
        
        // If returning to original source failed, fall back to inventory/stash system
        if (!success)
        {
            success = TryReturnItemToInventoryOrStash(itemToReturn, countToReturn);
        }
        
        if (success)
        {
            slot.ClearSlot();
            UpdateForgeState();
        }
        else
        {
            Debug.LogWarning($"FORGE: Failed to return {itemToReturn.itemName} - no available storage space!");
        }
    }
    
    /// <summary>
    /// Handle double-click on output slot - context-aware routing (menu vs game)
    /// </summary>
    void HandleOutputSlotDoubleClick(UnifiedSlot slot)
    {
        if (slot.IsEmpty) return;
        
        Debug.Log($"FORGE: Double-click on output - Context: {currentContext}");
        
        // Route based on context
        if (currentContext == ForgeContext.Game)
        {
            // IN-GAME: Route to persistent inventory
            bool success = TryAddToGameInventory(slot.CurrentItem, slot.ItemCount);
            if (success)
            {
                slot.ClearSlot();
                UpdateForgeState();
            }
        }
        else
        {
            // MENU: Use existing logic (stash/inventory)
            if (slot.CurrentItem is BackpackItem backpackItem)
            {
                bool success = TryEquipOrStoreBackpack(backpackItem, slot.ItemCount);
                if (success)
                {
                    slot.ClearSlot();
                    UpdateForgeState();
                    Debug.Log("FORGE: Backpack successfully equipped or stored");
                }
            }
            else
            {
                bool success = TryReturnItemToInventoryOrStash(slot.CurrentItem, slot.ItemCount);
                if (success)
                {
                    slot.ClearSlot();
                    UpdateForgeState();
                    Debug.Log("FORGE: Output item successfully returned and saved");
                }
            }
        }
    }
    
    /// <summary>
    /// Handle item dropped into forge slots
    /// </summary>
    void HandleItemDropped(UnifiedSlot fromSlot, UnifiedSlot toSlot)
    {
        Debug.Log($"FORGE: Item dropped from {fromSlot.name} to {toSlot.name}");
        
        // Perform the actual item transfer/swap with stack splitting for forge input slots
        if (fromSlot != null && toSlot != null)
        {
            ChestItemData fromItem = fromSlot.CurrentItem;
            int fromCount = fromSlot.ItemCount;
            ChestItemData toItem = toSlot.CurrentItem;
            int toCount = toSlot.ItemCount;
            
            // If target slot is empty
            if (toSlot.IsEmpty)
            {
                // FORGE INPUT SLOTS: Only accept 1 item from stacks, return the rest
                if (toSlot.isForgeInputSlot && fromCount > 1)
                {
                    // Place only 1 item in forge slot
                    toSlot.SetItem(fromItem, 1);
                    // Return the remaining items to the original slot
                    fromSlot.SetItem(fromItem, fromCount - 1);
                    Debug.Log($"FORGE: Took 1 {fromItem.itemName} for forge, returned {fromCount - 1} to {fromSlot.name}");
                    
                    // Record source tracking for the forge input slot
                    forgeInputSourceMap[toSlot] = fromSlot;
                    Debug.Log($"FORGE: Recorded source tracking: {toSlot.name} came from {fromSlot.name}");
                    
                    // Trigger save for the original slot's container
                    TriggerSaveForSlot(fromSlot);
                }
                else
                {
                    // Normal transfer for non-forge slots or single items
                    toSlot.SetItem(fromItem, fromCount);
                    fromSlot.ClearSlot();
                    Debug.Log($"FORGE: Moved {fromItem.itemName} x{fromCount} to {toSlot.name}");
                    
                    // Record source tracking if this is a forge input slot
                    if (toSlot.isForgeInputSlot)
                    {
                        forgeInputSourceMap[toSlot] = fromSlot;
                        Debug.Log($"FORGE: Recorded source tracking: {toSlot.name} came from {fromSlot.name}");
                    }
                }
            }
            // If both slots have items, handle swapping
            else
            {
                // Clear any existing source tracking for the target slot before swapping
                if (toSlot.isForgeInputSlot && forgeInputSourceMap.ContainsKey(toSlot))
                {
                    forgeInputSourceMap.Remove(toSlot);
                    Debug.Log($"FORGE: Cleared old source tracking for {toSlot.name} due to swap");
                }
                
                // For forge input slots, still only accept 1 item when swapping
                if (toSlot.isForgeInputSlot && fromCount > 1)
                {
                    // Put the existing forge item back to source
                    fromSlot.SetItem(toItem, toCount + fromCount - 1); // Combine counts
                    // Place only 1 new item in forge slot
                    toSlot.SetItem(fromItem, 1);
                    Debug.Log($"FORGE: Swapped 1 {fromItem.itemName} into forge, combined {toItem.itemName} with remaining stack");
                    
                    // Record new source tracking for the forge input slot
                    forgeInputSourceMap[toSlot] = fromSlot;
                    Debug.Log($"FORGE: Recorded source tracking: {toSlot.name} came from {fromSlot.name}");
                    
                    // Trigger save for the original slot's container
                    TriggerSaveForSlot(fromSlot);
                }
                else
                {
                    // Normal swap
                    fromSlot.SetItem(toItem, toCount);
                    toSlot.SetItem(fromItem, fromCount);
                    Debug.Log($"FORGE: Swapped {fromItem.itemName} with {toItem.itemName}");
                    
                    // Record source tracking if this is a forge input slot
                    if (toSlot.isForgeInputSlot)
                    {
                        forgeInputSourceMap[toSlot] = fromSlot;
                        Debug.Log($"FORGE: Recorded source tracking: {toSlot.name} came from {fromSlot.name}");
                    }
                }
            }
        }
        
        UpdateForgeState();
    }
    
    /// <summary>
    /// Trigger save for the container that owns the given slot
    /// </summary>
    void TriggerSaveForSlot(UnifiedSlot slot)
    {
        if (slot == null) return;
        
        // Check if it's an inventory slot
        if (inventoryManager != null)
        {
            var inventorySlots = inventoryManager.GetAllInventorySlots();
            if (inventorySlots != null && inventorySlots.Contains(slot))
            {
                // Trigger inventory save using existing JSON system
                inventoryManager.SaveInventoryData();
                Debug.Log("FORGE: Triggered inventory save");
                return;
            }
        }
        
        // Check if it's a stash slot
        if (stashManager != null)
        {
            // Check if it's one of the stash slots (stashSlot1 to stashSlot5)
            if (slot == stashManager.stashSlot1 || slot == stashManager.stashSlot2 || 
                slot == stashManager.stashSlot3 || slot == stashManager.stashSlot4 || 
                slot == stashManager.stashSlot5)
            {
                // Note: StashManager likely auto-saves when items are modified
                Debug.Log("FORGE: Stack split with stash slot (auto-save expected)");
                return;
            }
        }
        
        Debug.Log($"FORGE: Could not determine save system for slot {slot.name}");
    }
    
    /// <summary>
    /// Try to return an item to inventory first, then stash if inventory is full
    /// Uses direct slot manipulation like the existing systems
    /// </summary>
    bool TryReturnItemToInventoryOrStash(ChestItemData item, int count)
    {
        // Try inventory first - find first empty inventory slot
        if (inventoryManager != null)
        {
            var inventorySlots = inventoryManager.GetAllInventorySlots();
            foreach (var slot in inventorySlots)
            {
                if (slot != null && slot.IsEmpty)
                {
                    slot.SetItem(item, count);
                    Debug.Log($"FORGE: Returned {item.itemName} x{count} to inventory");
                    
                    // Trigger inventory save using existing JSON system
                    inventoryManager.SaveInventoryData();
                    Debug.Log("FORGE: Triggered inventory save after item return");
                    
                    return true;
                }
            }
        }
        
        // Try stash if inventory failed - find first empty stash slot
        if (stashManager != null)
        {
            // Access stash slots directly (they should be public or have a getter)
            var stashSlots = GetStashSlots();
            foreach (var slot in stashSlots)
            {
                if (slot != null && slot.IsEmpty)
                {
                    slot.SetItem(item, count);
                    Debug.Log($"FORGE: Returned {item.itemName} x{count} to stash");
                    
                    // Note: StashManager likely auto-saves when items are modified
                    Debug.Log("FORGE: Returned item to stash (auto-save expected)");
                    
                    return true;
                }
            }
        }
        
        Debug.LogWarning($"FORGE: Could not return {item.itemName} - both inventory and stash are full!");
        return false;
    }
    
    /// <summary>
    /// Get stash slots from StashManager (helper method)
    /// </summary>
    List<UnifiedSlot> GetStashSlots()
    {
        var stashSlots = new List<UnifiedSlot>();
        if (stashManager != null)
        {
            // Access the public stash slot fields
            if (stashManager.stashSlot1 != null) stashSlots.Add(stashManager.stashSlot1);
            if (stashManager.stashSlot2 != null) stashSlots.Add(stashManager.stashSlot2);
            if (stashManager.stashSlot3 != null) stashSlots.Add(stashManager.stashSlot3);
            if (stashManager.stashSlot4 != null) stashSlots.Add(stashManager.stashSlot4);
            if (stashManager.stashSlot5 != null) stashSlots.Add(stashManager.stashSlot5);
        }
        return stashSlots;
    }
    
    /// <summary>
    /// Set the FORGE context (Menu or Game)
    /// </summary>
    public void SetContext(ForgeContext context)
    {
        currentContext = context;
        Debug.Log($"[ForgeManager] Context set to: {context}");
    }
    
    /// <summary>
    /// Add crafted item to game inventory (in-game context only)
    /// </summary>
    private bool TryAddToGameInventory(ChestItemData item, int count)
    {
        // Get the game InventoryManager instance
        InventoryManager gameInventory = InventoryManager.Instance;
        if (gameInventory == null)
        {
            Debug.LogError("[ForgeManager] Cannot find InventoryManager in game!");
            return false;
        }
        
        // Try to add item to inventory
        bool success = gameInventory.TryAddItem(item, count, autoSave: false);
        
        if (success)
        {
            Debug.Log($"[ForgeManager] Added {item.itemName} x{count} to game inventory");
            
            // Save inventory data
            gameInventory.SaveInventoryData();
            
            // CRITICAL: Update PersistentItemInventoryManager
            if (PersistentItemInventoryManager.Instance != null)
            {
                PersistentItemInventoryManager.Instance.UpdateFromInventoryManager(gameInventory);
                PersistentItemInventoryManager.Instance.SaveInventoryData();
                Debug.Log("[ForgeManager] ✅ Updated PersistentInventoryManager");
            }
            else
            {
                Debug.LogWarning("[ForgeManager] ⚠️ PersistentInventoryManager not found!");
            }
            
            // 🧠 COGNITIVE INTEGRATION: User feedback through Cognitive system
            CognitiveEvents.OnWorldInteraction?.Invoke($"forge_crafted_{item.itemName}", null);
        }
        else
        {
            Debug.LogWarning($"[ForgeManager] Inventory full! Cannot add {item.itemName}");
            
            // 🧠 COGNITIVE INTEGRATION: User feedback through Cognitive system
            CognitiveEvents.OnWorldInteraction?.Invoke("forge_inventory_full", null);
        }
        
        return success;
    }
    
    /// <summary>
    /// Smart backpack routing: Try to equip if upgrade, otherwise store in stash/inventory
    /// </summary>
    bool TryEquipOrStoreBackpack(BackpackItem backpack, int count)
    {
        if (backpack == null) return false;
        
        Debug.Log($"FORGE: Smart routing for {backpack.GetDisplayName()}");
        
        // Try to equip if it's an upgrade
        if (inventoryManager != null && inventoryManager.backpackSlot != null)
        {
            BackpackSlotController backpackController = inventoryManager.backpackSlot;
            
            if (backpackController.CanEquipBackpack(backpack))
            {
                Debug.Log($"FORGE: Equipping {backpack.GetDisplayName()} as upgrade");
                bool equipped = backpackController.EquipBackpack(backpack, false); // Don't consume since we're transferring
                
                if (equipped)
                {
                    Debug.Log($"FORGE: Auto-equipped {backpack.GetDisplayName()} upgrade from forge!");
                    
                    // Note: User feedback removed for menu scene compatibility
                    
                    // Save the inventory since backpack was equipped
                    inventoryManager.SaveInventoryData();
                    return true;
                }
            }
            else
            {
                Debug.Log($"FORGE: {backpack.GetDisplayName()} is not an upgrade - storing instead");
            }
        }
        
        // If can't equip or not an upgrade, try to store in stash first
        var stashSlots = GetStashSlots();
        foreach (var slot in stashSlots)
        {
            if (slot != null && slot.IsEmpty)
            {
                slot.SetItem(backpack, count);
                Debug.Log($"FORGE: Stored {backpack.GetDisplayName()} in stash");
                
                // Note: User feedback removed for menu scene compatibility
                
                return true;
            }
        }
        
        // If stash is full, try inventory
        if (inventoryManager != null)
        {
            var inventorySlots = inventoryManager.GetAllInventorySlots();
            foreach (var slot in inventorySlots)
            {
                if (slot != null && slot.IsEmpty && !slot.isGemSlot)
                {
                    slot.SetItem(backpack, count);
                    Debug.Log($"FORGE: Stored {backpack.GetDisplayName()} in inventory (stash was full)");
                    
                    // Note: User feedback removed for menu scene compatibility
                    
                    // Save inventory
                    inventoryManager.SaveInventoryData();
                    return true;
                }
            }
        }
        
        Debug.LogWarning($"FORGE: Could not equip or store {backpack.GetDisplayName()} - all storage full!");
        
        // Note: User feedback removed for menu scene compatibility
        
        return false;
    }
    
    /// <summary>
    /// Update the forge state - check for valid recipes and update UI
    /// </summary>
    public void UpdateForgeState()
    {
        // Don't check for recipes if output slot already has an item (waiting to be taken)
        if (outputSlot != null && !outputSlot.IsEmpty)
        {
            Debug.Log("FORGE: Output slot occupied - waiting for player to take the item");
            return;
        }
        
        // Get current ingredients from input slots
        List<ChestItemData> currentIngredients = new List<ChestItemData>();
        
        foreach (var slot in inputSlots)
        {
            if (slot != null && !slot.IsEmpty)
            {
                currentIngredients.Add(slot.CurrentItem);
            }
        }
        
        // Find matching recipe - prioritize recipes with more ingredients (more specific matches)
        var matchingRecipes = availableRecipes.Where(recipe => recipe.MatchesIngredients(currentIngredients)).ToList();
        
        if (matchingRecipes.Count > 1)
        {
            Debug.Log($"FORGE: Found {matchingRecipes.Count} matching recipes for {currentIngredients.Count} ingredients:");
            foreach (var recipe in matchingRecipes)
            {
                Debug.Log($"  - {recipe.outputItem?.itemName} (requires {recipe.GetRequiredIngredientCount()} ingredients)");
            }
        }
        
        ForgeRecipe newValidRecipe = matchingRecipes
            .OrderByDescending(recipe => recipe.GetRequiredIngredientCount())
            .FirstOrDefault();
            
        if (newValidRecipe != null)
        {
            Debug.Log($"FORGE: Selected recipe: {newValidRecipe.outputItem?.itemName} (requires {newValidRecipe.GetRequiredIngredientCount()} ingredients)");
        }
        
        // Check if recipe state changed (for audio feedback)
        bool hadValidRecipe = currentValidRecipe != null;
        bool hasValidRecipe = newValidRecipe != null;
        
        // Play recipe detected sound if we just found a valid recipe
        if (!hadValidRecipe && hasValidRecipe)
        {
            PlayForgeSound(recipeDetectedSound);
        }
        
        currentValidRecipe = newValidRecipe;
        
        // Update UI based on whether we have a valid recipe
        SetForgeUIState(hasValidRecipe);
        
        // Show preview of output (but don't consume ingredients)
        UpdateOutputSlot();
        
        Debug.Log($"FORGE: Updated state - Valid recipe: {hasValidRecipe}");
    }
    
    /// <summary>
    /// Update FORGE UI state: invalid, valid, or processing
    /// </summary>
    void SetForgeUIState(bool hasValidRecipe)
    {
        if (isProcessing)
        {
            // PROCESSING STATE: Show processing image, countdown, and progress slider
            SetUIElement(invalidRecipeImage, false);
            SetUIElement(craftButton, false);
            SetUIElement(processingImage, true);
            SetUIElement(countdownText, true);
            SetUIElement(progressSlider, true);
            
            // Initialize progress slider
            if (progressSlider != null)
            {
                progressSlider.value = 0f;
                SetSliderAlpha(progressSlider, 1f);
                Debug.Log("FORGE: Progress slider initialized and shown");
            }
        }
        else if (hasValidRecipe)
        {
            // VALID STATE: Show craft button, hide others
            SetUIElement(invalidRecipeImage, false);
            SetUIElement(craftButton, true); // Show craft button when valid recipe
            SetUIElement(processingImage, false);
            SetUIElement(countdownText, false);
            SetUIElement(progressSlider, false);
        }
        else
        {
            // INVALID STATE: Show invalid image, hide others
            SetUIElement(invalidRecipeImage, true);
            SetUIElement(craftButton, false);
            SetUIElement(processingImage, false);
            SetUIElement(countdownText, false);
            SetUIElement(progressSlider, false);
        }
        
        // Update input slot transparency (not transparent during processing)
        float targetAlpha = (hasValidRecipe || isProcessing) ? activeAlpha : inactiveAlpha;
        foreach (var slot in inputSlots)
        {
            if (slot != null && slot.itemIcon != null)
            {
                // Keep forge input icons hidden when empty so only background shows
                if (slot.IsEmpty)
                {
                    SetImageAlpha(slot.itemIcon, 0f);
                }
                else
                {
                    SetImageAlpha(slot.itemIcon, targetAlpha);
                }
            }
        }
    }
    
    /// <summary>
    /// Helper method to safely set UI element active state
    /// </summary>
    void SetUIElement(Component element, bool active)
    {
        if (element != null && element.gameObject != null)
        {
            element.gameObject.SetActive(active);
        }
    }
    
    /// <summary>
    /// Play audio clip safely
    /// </summary>
    void PlayForgeSound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
            Debug.Log($"FORGE: Playing sound {clip.name}");
        }
    }
    
    /// <summary>
    /// Update the output slot to show preview of what will be crafted (no consumption)
    /// </summary>
    void UpdateOutputSlot()
    {
        if (outputSlot == null) return;
        
        if (currentValidRecipe != null && !isProcessing)
        {
            // Show preview of what will be crafted (no consumption yet)
            outputSlot.SetItem(currentValidRecipe.outputItem, currentValidRecipe.outputCount);
            
            // Make output slot semi-transparent to show it's a preview
            if (outputSlot.itemIcon != null)
            {
                SetImageAlpha(outputSlot.itemIcon, inactiveAlpha);
            }
            
            Debug.Log($"FORGE: Showing preview of {currentValidRecipe.outputItem.itemName} x{currentValidRecipe.outputCount}");
        }
        else if (!isProcessing)
        {
            // Clear output slot when no valid recipe and not processing
            outputSlot.ClearSlot();
        }
        // Don't clear output slot during processing - let the coroutine handle it
    }
    
    /// <summary>
    /// Start the crafting process when button is clicked
    /// </summary>
    public void CraftItem()
    {
        if (currentValidRecipe == null || isProcessing)
        {
            Debug.LogWarning("FORGE: Cannot craft - no valid recipe or already processing");
            return;
        }
        
        Debug.Log($"FORGE: Starting crafting process for {currentValidRecipe.outputItem.itemName}");
        
        // Start processing
        isProcessing = true;
        
        // Update UI to show processing state
        SetForgeUIState(true); // This will show processing image and countdown
        
        // Play processing sound
        PlayForgeSound(processingSound);
        
        // Start the crafting coroutine
        StartCoroutine(ProcessCraftingCoroutine());
    }
    
    /// <summary>
    /// Processing coroutine with countdown, progress slider, and visual feedback
    /// </summary>
    System.Collections.IEnumerator ProcessCraftingCoroutine()
    {
        float timeRemaining = craftingTime;
        float totalTime = craftingTime;
        
        Debug.Log($"FORGE: Processing started - {craftingTime} seconds remaining");
        
        // Countdown loop with progress slider animation
        while (timeRemaining > 0)
        {
            // Update countdown text
            if (countdownText != null)
            {
                countdownText.text = Mathf.Ceil(timeRemaining).ToString();
            }
            
            // Update progress slider with smooth animation
            if (progressSlider != null)
            {
                float targetProgress = 1f - (timeRemaining / totalTime);
                float currentProgress = progressSlider.value;
                
                // Smooth progress animation
                float smoothedProgress = Mathf.Lerp(currentProgress, targetProgress, Time.deltaTime * progressSmoothness);
                progressSlider.value = smoothedProgress;
                
                // Debug progress every 0.5 seconds
                if (Mathf.FloorToInt(timeRemaining * 2) != Mathf.FloorToInt((timeRemaining - Time.deltaTime) * 2))
                {
                    Debug.Log($"FORGE: Progress {(smoothedProgress * 100):F1}% - {timeRemaining:F1}s remaining");
                }
            }
            
            // Wait for next frame
            yield return null;
            timeRemaining -= Time.deltaTime;
        }
        
        // Ensure slider reaches 100% at completion
        if (progressSlider != null)
        {
            progressSlider.value = 1f;
            Debug.Log("FORGE: Progress slider completed at 100%");
        }
        
        // Processing complete
        Debug.Log("FORGE: Processing complete!");
        
        // Clear countdown text
        if (countdownText != null)
        {
            countdownText.text = "";
        }
        
        // Complete the crafting
        CompleteCrafting();
    }
    
    /// <summary>
    /// Complete the crafting process - consume ingredients and create output
    /// </summary>
    void CompleteCrafting()
    {
        if (currentValidRecipe == null)
        {
            Debug.LogError("FORGE: CompleteCrafting called but no valid recipe!");
            isProcessing = false;
            UpdateForgeState();
            return;
        }
        
        Debug.Log($"FORGE: Completing crafting of {currentValidRecipe.outputItem.itemName}");
        
        // Consume ingredients from input slots
        foreach (var slot in inputSlots)
        {
            if (slot != null && !slot.IsEmpty)
            {
                Debug.Log($"FORGE: Consuming ingredient: {slot.CurrentItem.itemName}");
                slot.ClearSlot();
                
                // Clear source tracking for consumed ingredients
                if (forgeInputSourceMap.ContainsKey(slot))
                {
                    forgeInputSourceMap.Remove(slot);
                    Debug.Log($"FORGE: Cleared source tracking for consumed ingredient slot: {slot.name}");
                }
            }
        }
        
        // Create the output item
        if (outputSlot != null)
        {
            outputSlot.SetItem(currentValidRecipe.outputItem, currentValidRecipe.outputCount);
            
            // Make output slot fully opaque now that it's real
            if (outputSlot.itemIcon != null)
            {
                SetImageAlpha(outputSlot.itemIcon, activeAlpha);
            }
        }
        
        // Play completion sound
        PlayForgeSound(processingCompleteSound);
        
        // Start progress slider fade out animation
        if (progressSlider != null)
        {
            // Stop any existing fade coroutine
            if (sliderFadeCoroutine != null)
            {
                StopCoroutine(sliderFadeCoroutine);
            }
            
            // Start fade out animation
            sliderFadeCoroutine = StartCoroutine(FadeOutProgressSlider());
            Debug.Log("FORGE: Started progress slider fade out animation");
        }
        
        // Reset processing state
        isProcessing = false;
        currentValidRecipe = null; // Clear recipe since ingredients are consumed
        
        // Update forge state
        UpdateForgeState();
        
        Debug.Log("FORGE: Crafting completed successfully!");
    }
    
    /// <summary>
    /// Fade out progress slider animation after crafting completion
    /// </summary>
    System.Collections.IEnumerator FadeOutProgressSlider()
    {
        if (progressSlider == null) yield break;
        
        Debug.Log($"FORGE: Starting progress slider fade out over {sliderFadeOutTime} seconds");
        
        float elapsedTime = 0f;
        float startAlpha = 1f;
        
        // Fade out animation
        while (elapsedTime < sliderFadeOutTime)
        {
            float currentAlpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / sliderFadeOutTime);
            SetSliderAlpha(progressSlider, currentAlpha);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Ensure completely transparent and hidden at the end
        SetSliderAlpha(progressSlider, 0f);
        SetUIElement(progressSlider, false);
        
        Debug.Log("FORGE: Progress slider fade out completed and hidden");
        
        // Clear the coroutine reference
        sliderFadeCoroutine = null;
    }
    
    /// <summary>
    /// Set alpha transparency for an Image component
    /// </summary>
    void SetImageAlpha(Image image, float alpha)
    {
        if (image != null)
        {
            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }
    }
    
    /// <summary>
    /// Set alpha transparency for a Button component
    /// </summary>
    void SetButtonAlpha(Button button, float alpha)
    {
        if (button != null && button.image != null)
        {
            Color color = button.image.color;
            color.a = alpha;
            button.image.color = color;
        }
    }
    
    /// <summary>
    /// Set alpha transparency for a Slider component (affects fill area and handle)
    /// </summary>
    void SetSliderAlpha(Slider slider, float alpha)
    {
        if (slider == null) return;
        
        // Set alpha for fill area
        if (slider.fillRect != null)
        {
            Image fillImage = slider.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                Color fillColor = fillImage.color;
                fillColor.a = alpha;
                fillImage.color = fillColor;
            }
        }
        
        // Set alpha for handle
        if (slider.handleRect != null)
        {
            Image handleImage = slider.handleRect.GetComponent<Image>();
            if (handleImage != null)
            {
                Color handleColor = handleImage.color;
                handleColor.a = alpha;
                handleImage.color = handleColor;
            }
        }
        
        // Set alpha for background
        Image backgroundImage = slider.GetComponent<Image>();
        if (backgroundImage != null)
        {
            Color bgColor = backgroundImage.color;
            bgColor.a = alpha * 0.5f; // Background slightly more transparent
            backgroundImage.color = bgColor;
        }
    }
    
    /// <summary>
    /// Toggle recipe info panel visibility with animation
    /// </summary>
    public void ToggleRecipeInfoPanel()
    {
        if (isRecipePanelVisible)
        {
            HideRecipeInfoPanel();
        }
        else
        {
            ShowRecipeInfoPanel();
        }
    }
    
    /// <summary>
    /// Public method to refresh recipe entries (useful for debugging)
    /// </summary>
    [ContextMenu("Refresh Recipe Entries")]
    public void RefreshRecipeEntries()
    {
        Debug.Log("FORGE: Manually refreshing recipe entries");
        GenerateRecipeEntries();
    }
    
    /// <summary>
    /// Debug method to check ScrollRect setup
    /// </summary>
    [ContextMenu("Debug ScrollRect Setup")]
    public void DebugScrollRectSetup()
    {
        if (recipeContentArea == null)
        {
            Debug.LogError("FORGE: Recipe content area is null!");
            return;
        }
        
        ScrollRect scrollRect = recipeContentArea.GetComponentInParent<ScrollRect>();
        if (scrollRect == null)
        {
            Debug.LogError("FORGE: No ScrollRect found in parents!");
            return;
        }
        
        RectTransform contentRect = recipeContentArea.GetComponent<RectTransform>();
        RectTransform viewportRect = scrollRect.viewport;
        
        Debug.Log($"FORGE ScrollRect Debug:");
        Debug.Log($"- ScrollRect vertical: {scrollRect.vertical}");
        Debug.Log($"- ScrollRect horizontal: {scrollRect.horizontal}");
        Debug.Log($"- Content assigned: {scrollRect.content != null}");
        Debug.Log($"- Viewport size: {(viewportRect != null ? viewportRect.rect.size.ToString() : "null")}");
        Debug.Log($"- Content size: {(contentRect != null ? contentRect.rect.size.ToString() : "null")}");
        Debug.Log($"- Content anchors: min={contentRect?.anchorMin}, max={contentRect?.anchorMax}");
        Debug.Log($"- Recipe entries count: {createdRecipeEntries.Count}");
        
        // Check layout components
        VerticalLayoutGroup layoutGroup = recipeContentArea.GetComponent<VerticalLayoutGroup>();
        ContentSizeFitter sizeFitter = recipeContentArea.GetComponent<ContentSizeFitter>();
        
        Debug.Log($"- VerticalLayoutGroup: {layoutGroup != null}");
        Debug.Log($"- ContentSizeFitter: {sizeFitter != null}");
        if (sizeFitter != null)
        {
            Debug.Log($"- Vertical fit: {sizeFitter.verticalFit}");
            Debug.Log($"- Horizontal fit: {sizeFitter.horizontalFit}");
        }
    }
    
    /// <summary>
    /// Show recipe info panel with animation
    /// </summary>
    void ShowRecipeInfoPanel()
    {
        if (recipeInfoPanel == null) return;
        
        Debug.Log("FORGE: Showing recipe info panel");
        
        // Stop any existing animation
        if (panelAnimationCoroutine != null)
        {
            StopCoroutine(panelAnimationCoroutine);
        }
        
        // Ensure panel is active
        recipeInfoPanel.SetActive(true);
        isRecipePanelVisible = true;
        
        // Start show animation
        panelAnimationCoroutine = StartCoroutine(AnimatePanel(true));
    }
    
    /// <summary>
    /// Hide recipe info panel with animation
    /// </summary>
    void HideRecipeInfoPanel()
    {
        if (recipeInfoPanel == null) return;
        
        Debug.Log("FORGE: Hiding recipe info panel");
        
        // Stop any existing animation
        if (panelAnimationCoroutine != null)
        {
            StopCoroutine(panelAnimationCoroutine);
        }
        
        isRecipePanelVisible = false;
        
        // Start hide animation
        panelAnimationCoroutine = StartCoroutine(AnimatePanel(false));
    }
    
    /// <summary>
    /// Generate recipe entries dynamically from availableRecipes
    /// </summary>
    void GenerateRecipeEntries()
    {
        if (recipeContentArea == null || recipeEntryPrefab == null)
        {
            Debug.LogWarning("FORGE: Recipe content area or prefab not assigned - cannot generate recipe entries");
            return;
        }
        
        // Setup layout components for proper arrangement
        SetupRecipeContentLayout();
        
        // Clear existing entries
        ClearRecipeEntries();
        
        Debug.Log($"FORGE: Generating {availableRecipes.Count} recipe entries");
        
        // Create entry for each recipe
        foreach (var recipe in availableRecipes)
        {
            if (recipe.outputItem != null)
            {
                GameObject entryObject = Instantiate(recipeEntryPrefab, recipeContentArea);
                createdRecipeEntries.Add(entryObject);
                
                // Setup the recipe entry (this will be handled by a separate component)
                RecipeEntryDisplay entryDisplay = entryObject.GetComponent<RecipeEntryDisplay>();
                if (entryDisplay != null)
                {
                    entryDisplay.SetupRecipe(recipe);
                }
                else
                {
                    Debug.LogWarning($"FORGE: Recipe entry prefab missing RecipeEntryDisplay component!");
                }
                
                Debug.Log($"FORGE: Created recipe entry for {recipe.outputItem.itemName}");
            }
        }
        
        Debug.Log($"FORGE: Created {createdRecipeEntries.Count} recipe entries");
        
        // Force layout rebuild
        ForceLayoutRebuild();
    }
    
    /// <summary>
    /// Clear all dynamically created recipe entries
    /// </summary>
    void ClearRecipeEntries()
    {
        foreach (var entry in createdRecipeEntries)
        {
            if (entry != null)
            {
                DestroyImmediate(entry);
            }
        }
        createdRecipeEntries.Clear();
        Debug.Log("FORGE: Cleared all recipe entries");
    }
    
    /// <summary>
    /// Setup layout components for recipe content area
    /// </summary>
    void SetupRecipeContentLayout()
    {
        if (recipeContentArea == null) return;
        
        // Add VerticalLayoutGroup if it doesn't exist
        VerticalLayoutGroup layoutGroup = recipeContentArea.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = recipeContentArea.gameObject.AddComponent<VerticalLayoutGroup>();
            Debug.Log("FORGE: Added VerticalLayoutGroup to recipe content area");
        }
        
        // Configure layout group settings
        layoutGroup.childAlignment = TextAnchor.UpperLeft;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.spacing = 10f; // Space between recipe entries
        
        // Set padding
        layoutGroup.padding = new RectOffset(10, 10, 10, 10); // left, right, top, bottom
        
        // Add ContentSizeFitter if it doesn't exist
        ContentSizeFitter sizeFitter = recipeContentArea.GetComponent<ContentSizeFitter>();
        if (sizeFitter == null)
        {
            sizeFitter = recipeContentArea.gameObject.AddComponent<ContentSizeFitter>();
            Debug.Log("FORGE: Added ContentSizeFitter to recipe content area");
        }
        
        // Configure size fitter for ScrollRect compatibility
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        // Setup ScrollRect compatibility
        SetupScrollRectCompatibility();
        
        Debug.Log("FORGE: Recipe content layout configured for ScrollRect");
    }
    
    /// <summary>
    /// Setup ScrollRect compatibility for proper scrolling behavior
    /// </summary>
    void SetupScrollRectCompatibility()
    {
        if (recipeContentArea == null) return;
        
        // Find the ScrollRect component in parents
        ScrollRect scrollRect = recipeContentArea.GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
        {
            // Ensure ScrollRect is configured for vertical scrolling
            scrollRect.vertical = true;
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 20f;
            
            // Ensure content is assigned
            if (scrollRect.content == null)
            {
                scrollRect.content = recipeContentArea.GetComponent<RectTransform>();
                Debug.Log("FORGE: Assigned content to ScrollRect");
            }
            
            // Set content anchoring for proper scrolling
            RectTransform contentRect = recipeContentArea.GetComponent<RectTransform>();
            if (contentRect != null)
            {
                // Anchor to top-left and stretch horizontally
                contentRect.anchorMin = new Vector2(0, 1);
                contentRect.anchorMax = new Vector2(1, 1);
                contentRect.pivot = new Vector2(0.5f, 1);
                contentRect.anchoredPosition = new Vector2(0, 0);
                
                Debug.Log("FORGE: Configured content RectTransform for scrolling");
            }
            
            Debug.Log("FORGE: ScrollRect configured for vertical scrolling");
        }
        else
        {
            Debug.LogWarning("FORGE: No ScrollRect found in parents - scrolling may not work properly");
        }
    }
    
    /// <summary>
    /// Force layout rebuild for immediate visual update
    /// </summary>
    void ForceLayoutRebuild()
    {
        if (recipeContentArea == null) return;
        
        // Force immediate layout rebuild
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(recipeContentArea.GetComponent<RectTransform>());
        
        // Also rebuild parent if it's a ScrollRect
        Transform parent = recipeContentArea.parent;
        if (parent != null)
        {
            ScrollRect scrollRect = parent.GetComponent<ScrollRect>();
            if (scrollRect != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(parent.GetComponent<RectTransform>());
                Debug.Log("FORGE: Forced layout rebuild for ScrollRect");
            }
        }
        
        Debug.Log("FORGE: Forced layout rebuild completed");
    }
    
    /// <summary>
    /// Animate panel show/hide based on animation type
    /// </summary>
    System.Collections.IEnumerator AnimatePanel(bool show)
    {
        if (recipeInfoPanel == null) yield break;
        
        RectTransform panelRect = recipeInfoPanel.GetComponent<RectTransform>();
        CanvasGroup canvasGroup = recipeInfoPanel.GetComponent<CanvasGroup>();
        
        // Ensure CanvasGroup exists for fade animations
        if (canvasGroup == null && (animationType == RecipePanelAnimationType.Fade))
        {
            canvasGroup = recipeInfoPanel.AddComponent<CanvasGroup>();
        }
        
        float elapsedTime = 0f;
        Vector3 startPos = Vector3.zero;
        Vector3 targetPos = Vector3.zero;
        float startAlpha = show ? 0f : 1f;
        float targetAlpha = show ? 1f : 0f;
        
        // Setup animation based on type
        if (panelRect != null)
        {
            switch (animationType)
            {
                case RecipePanelAnimationType.SlideFromLeft:
                    startPos = show ? new Vector3(-panelRect.rect.width, 0, 0) : Vector3.zero;
                    targetPos = show ? Vector3.zero : new Vector3(-panelRect.rect.width, 0, 0);
                    break;
                case RecipePanelAnimationType.SlideFromRight:
                    startPos = show ? new Vector3(panelRect.rect.width, 0, 0) : Vector3.zero;
                    targetPos = show ? Vector3.zero : new Vector3(panelRect.rect.width, 0, 0);
                    break;
                case RecipePanelAnimationType.SlideFromTop:
                    startPos = show ? new Vector3(0, panelRect.rect.height, 0) : Vector3.zero;
                    targetPos = show ? Vector3.zero : new Vector3(0, panelRect.rect.height, 0);
                    break;
                case RecipePanelAnimationType.SlideFromBottom:
                    startPos = show ? new Vector3(0, -panelRect.rect.height, 0) : Vector3.zero;
                    targetPos = show ? Vector3.zero : new Vector3(0, -panelRect.rect.height, 0);
                    break;
            }
            
            // Set initial position for slide animations
            if (animationType != RecipePanelAnimationType.Fade)
            {
                panelRect.anchoredPosition = startPos;
            }
        }
        
        // Set initial alpha for fade animation
        if (canvasGroup != null && animationType == RecipePanelAnimationType.Fade)
        {
            canvasGroup.alpha = startAlpha;
        }
        
        // Animate
        while (elapsedTime < panelAnimationTime)
        {
            float progress = elapsedTime / panelAnimationTime;
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            
            // Apply position animation
            if (panelRect != null && animationType != RecipePanelAnimationType.Fade)
            {
                panelRect.anchoredPosition = Vector3.Lerp(startPos, targetPos, easedProgress);
            }
            
            // Apply alpha animation
            if (canvasGroup != null && animationType == RecipePanelAnimationType.Fade)
            {
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, easedProgress);
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Ensure final values
        if (panelRect != null && animationType != RecipePanelAnimationType.Fade)
        {
            panelRect.anchoredPosition = targetPos;
        }
        
        if (canvasGroup != null && animationType == RecipePanelAnimationType.Fade)
        {
            canvasGroup.alpha = targetAlpha;
        }
        
        // Hide panel if animation was hide
        if (!show)
        {
            recipeInfoPanel.SetActive(false);
        }
        
        panelAnimationCoroutine = null;
        Debug.Log($"FORGE: Panel animation completed ({(show ? "show" : "hide")})");
    }
    
    /// <summary>
    /// Public method to manually trigger forge state update
    /// (useful for external systems)
    /// </summary>
    public void RefreshForgeState()
    {
        UpdateForgeState();
    }
    
    /// <summary>
    /// Clear all source tracking data (useful for reset/cleanup)
    /// </summary>
    public void ClearSourceTracking()
    {
        forgeInputSourceMap.Clear();
        Debug.Log("FORGE: Cleared all source tracking data");
    }
    
    void OnDestroy()
    {
        // Clean up event subscriptions
        foreach (var slot in inputSlots)
        {
            if (slot != null)
            {
                slot.OnDoubleClick -= HandleInputSlotDoubleClick;
                slot.OnItemDropped -= HandleItemDropped;
            }
        }
        
        if (outputSlot != null)
        {
            outputSlot.OnDoubleClick -= HandleOutputSlotDoubleClick;
        }
        
        // Clean up craft button listener
        if (craftButton != null)
        {
            craftButton.onClick.RemoveAllListeners();
        }
        
        // Clean up recipe info button listener
        if (recipeInfoButton != null)
        {
            recipeInfoButton.onClick.RemoveAllListeners();
        }
        
        // Clear source tracking data
        ClearSourceTracking();
        
        // Clear recipe entries
        ClearRecipeEntries();
        
        // Stop any running coroutines
        if (sliderFadeCoroutine != null)
        {
            StopCoroutine(sliderFadeCoroutine);
            sliderFadeCoroutine = null;
        }
        
        if (panelAnimationCoroutine != null)
        {
            StopCoroutine(panelAnimationCoroutine);
            panelAnimationCoroutine = null;
        }
    }
}
