using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GeminiGauntlet.Audio;

/// <summary>
/// Manages armor plate system - plates absorb damage before health is affected
/// Each plate provides 1500 HP shield, max 3 plates can be equipped
/// </summary>
public class ArmorPlateSystem : MonoBehaviour
{
    [Header("Plate Configuration")]
    [SerializeField] private int maxPlates = 3; // Default max plates (overridden by vest system)
    [SerializeField] private float plateHealth = 1500f;
    
    [Header("Current Plate State")]
    private int currentPlateCount = 0;
    private float currentPlateShield = 0f; // Current shield HP across all plates
    
    // VEST SYSTEM: Dynamic max plates based on equipped vest
    private int dynamicMaxPlates = 1; // Default to 1 plate (T1 vest)
    
    [Header("Animation System")]
    [SerializeField] private Animator rightHandAnimator;
    [SerializeField] private string plateApplyAnimationTrigger = "ApplyPlate";
    [SerializeField] private float plateApplicationDelay = 0.5f; // Delay between each plate application
    
    private bool isApplyingPlates = false;
    
    // Events
    public static System.Action<int, float, float> OnPlateShieldChanged; // (plateCount, currentShield, maxShield)
    public static System.Action OnPlateBroken; // Fired when a plate breaks
    public static System.Action OnPlateApplied; // Fired when a plate is applied
    
    // References
    private InventoryManager inventoryManager;
    private PlayerHealth playerHealth;
    private LayeredHandAnimationController layeredHandAnimationController; // Fallback animation system
    private PlayerAnimationStateManager animationStateManager; // Centralized animation system
    
    void Awake()
    {
        inventoryManager = InventoryManager.Instance;
        playerHealth = GetComponent<PlayerHealth>();
        
        if (playerHealth == null)
        {
            
            Debug.LogError("[ArmorPlateSystem] PlayerHealth component not found!");
        }
        
        // Find animation systems
        animationStateManager = FindObjectOfType<PlayerAnimationStateManager>();
        if (animationStateManager != null)
        {
            Debug.Log("[ArmorPlateSystem] ✅ Found PlayerAnimationStateManager - using centralized animation system");
        }
        else
        {
            // Fallback to LayeredHandAnimationController
            layeredHandAnimationController = FindObjectOfType<LayeredHandAnimationController>();
            if (layeredHandAnimationController != null)
            {
                Debug.Log("[ArmorPlateSystem] ✅ Found LayeredHandAnimationController - using fallback animation system");
            }
            else
            {
                Debug.LogError("[ArmorPlateSystem] ❌ No animation system found! Armor plate animations will not work!");
            }
        }
        
        // Initialize plate state
        currentPlateCount = 0;
        currentPlateShield = 0f;
        
        // VEST SYSTEM: Initialize with default 1 plate (T1 vest)
        dynamicMaxPlates = 1;
        
        Debug.Log("[ArmorPlateSystem] Initialized - Dynamic Max Plates: " + dynamicMaxPlates + ", Plate Health: " + plateHealth);
    }
    
    void Update()
    {
        // Listen for armor plate key to apply plates
        if (Input.GetKeyDown(Controls.ArmorPlate) && !isApplyingPlates)
        {
            Debug.Log("[ArmorPlateSystem] 🔑 ARMOR PLATE KEY PRESSED - attempting to apply plates");
            TryApplyPlatesFromInventory();
        }
    }
    
    /// <summary>
    /// Attempt to apply plates from inventory - fills all available slots
    /// VEST SYSTEM: Uses dynamic max plates based on equipped vest
    /// NEW LOGIC: Replaces damaged plates instead of adding to them
    /// </summary>
    public void TryApplyPlatesFromInventory()
    {
        Debug.Log("[ArmorPlateSystem] 🔍 TryApplyPlatesFromInventory() called");
        
        if (isApplyingPlates)
        {
            Debug.Log("[ArmorPlateSystem] ⏳ Already applying plates - ignoring request");
            return;
        }
        
        // Debug current state
        Debug.Log($"[ArmorPlateSystem] 📊 CURRENT STATE: Plates={currentPlateCount}/{dynamicMaxPlates}, Shield={currentPlateShield}/{dynamicMaxPlates * plateHealth}");
        
        // NEW LOGIC: Check if all plates are at 100% health
        float maxShield = dynamicMaxPlates * plateHealth;
        bool allPlatesFullyIntact = (currentPlateCount == dynamicMaxPlates && currentPlateShield >= maxShield);
        
        if (allPlatesFullyIntact)
        {
            Debug.Log($"[ArmorPlateSystem] ✅ All plates are fully intact! ({currentPlateCount}/{dynamicMaxPlates} at 100%)");
            CognitiveFeedManager.Instance?.QueueMessage("*Already fully plated*");
            return;
        }
        
        // Check how many plates player has in inventory
        int platesInInventory = GetPlateCountFromInventory();
        Debug.Log($"[ArmorPlateSystem] 🎒 Plates in inventory: {platesInInventory}");
        
        if (platesInInventory <= 0)
        {
            Debug.Log("[ArmorPlateSystem] ❌ No plates in inventory!");
            CognitiveFeedManager.Instance?.QueueMessage("*No armor plates available*");
            return;
        }
        
        // NEW LOGIC: Calculate how many damaged/missing plates need replacement
        int damagedOrMissingPlates = CalculateDamagedOrMissingPlates();
        int platesToApply = Mathf.Min(damagedOrMissingPlates, platesInInventory);
        
        Debug.Log($"[ArmorPlateSystem] 🔧 CALCULATION: Damaged/Missing={damagedOrMissingPlates}, In inventory={platesInInventory}, Will apply={platesToApply}");
        Debug.Log($"[ArmorPlateSystem] 🎯 Animation Controller Status: {(layeredHandAnimationController != null ? "✅ FOUND" : "❌ MISSING")}");
        
        // Start plate application sequence
        StartCoroutine(ApplyPlatesSequence(platesToApply));
    }
    
    /// <summary>
    /// Apply plates one by one with animation
    /// </summary>
    private IEnumerator ApplyPlatesSequence(int platesToApply)
    {
        Debug.Log($"[ArmorPlateSystem] 🚀 ApplyPlatesSequence() started - applying {platesToApply} plates");
        isApplyingPlates = true;
        
        // Check if health is low (below 20%) - if so, trigger instant regen
        bool wasHealthLow = playerHealth != null && (playerHealth.CurrentHealth / playerHealth.maxHealth) < 0.2f;
        
        for (int i = 0; i < platesToApply; i++)
        {
            Debug.Log($"[ArmorPlateSystem] 🔄 Applying plate {i + 1}/{platesToApply}");
            
            // Remove one plate from inventory
            if (!RemovePlateFromInventory())
            {
                Debug.LogWarning("[ArmorPlateSystem] ❌ Failed to remove plate from inventory - stopping sequence");
            }
            
            // Apply the plate
            ApplySinglePlate();
            
            // Play animation using centralized system
            if (animationStateManager != null)
            {
                Debug.Log("[ArmorPlateSystem] 🎬 Triggering plate animation via PlayerAnimationStateManager");
                bool success = animationStateManager.RequestArmorPlate();
                if (success)
                {
                    Debug.Log("[ArmorPlateSystem] ✅ Armor plate animation request accepted");
                }
                else
                {
                    Debug.LogWarning("[ArmorPlateSystem] ⚠️ Armor plate animation request rejected (hand may be locked)");
                }
            }
            else if (layeredHandAnimationController != null)
            {
                Debug.Log("[ArmorPlateSystem] 🎬 Using fallback LayeredHandAnimationController");
                layeredHandAnimationController.PlayApplyPlateAnimation();
            }
            else if (rightHandAnimator != null)
            {
                // Fallback: Use direct animator trigger if LayeredHandAnimationController not found
                Debug.Log("[ArmorPlateSystem] ⚠️ Using fallback animator - LayeredHandAnimationController not found!");
                rightHandAnimator.SetTrigger(plateApplyAnimationTrigger);
                Debug.Log("[ArmorPlateSystem] 🎬 Triggered fallback animation on right hand animator");
            }
            else
            {
                Debug.LogError("[ArmorPlateSystem] ❌ No animation system found - plate applied without animation");
            }
            
            // Play plate application sound (interruptible for rapid applications)
            GameSounds.PlayArmorPlateApply(transform.position);
            
            // Fire event
            OnPlateApplied?.Invoke();
            
            // Wait before applying next plate
            if (i < platesToApply - 1)
            {
                yield return new WaitForSeconds(plateApplicationDelay);
            }
        }
        
        isApplyingPlates = false;
        
        // FEATURE: If health was low when plates were applied, instantly start health regen
        if (wasHealthLow && playerHealth != null)
        {
            Debug.Log("[ArmorPlateSystem] 💊 Health was low - triggering instant health regeneration!");
            playerHealth.TriggerInstantHealthRegeneration();
            CognitiveFeedManager.Instance?.QueueMessage("*Emergency regeneration protocols activated*");
        }
        
        Debug.Log($"[ArmorPlateSystem] 🎉 Plate application complete - Current plates: {currentPlateCount}, Shield: {currentPlateShield}");
    }

    /// <summary>
    /// Safety check to detect and fix stuck armor plate animations
    /// </summary>
    private IEnumerator CheckForStuckAnimation()
    {
        // Wait for the expected animation duration (1-2 seconds)
        yield return new WaitForSeconds(2.0f);
        
        // Check if LayeredHandAnimationController is still stuck in ArmorPlate state
        if (layeredHandAnimationController != null && layeredHandAnimationController.IsRightHandInArmorPlateState())
        {
            Debug.LogWarning("[ArmorPlateSystem] DETECTED: Armor plate animation stuck! Force unlocking...");
            layeredHandAnimationController.ForceUnlockArmorPlateAnimation();
            Debug.Log("[ArmorPlateSystem] SAFETY: Forced unlock of stuck armor plate animation");
        }
    }
    
    /// <summary>
    /// Apply a single plate to the system
    /// VEST SYSTEM: Uses dynamic max plates
    /// NEW LOGIC: Replaces damaged plates instead of adding to them
    /// </summary>
    private void ApplySinglePlate()
    {
        // NEW LOGIC: Each plate application adds exactly 1500 HP (one full plate)
        // This replaces the most damaged plate slot
        
        if (currentPlateCount < dynamicMaxPlates)
        {
            // Missing plates - add a new plate slot
            currentPlateCount++;
            currentPlateShield += plateHealth;
            Debug.Log($"[ArmorPlateSystem] Added new plate! Count: {currentPlateCount}/{dynamicMaxPlates}, Shield: {currentPlateShield}");
        }
        else
        {
            // All plate slots are filled - replace the most damaged one
            // Simply add one full plate worth of health (1500 HP)
            currentPlateShield += plateHealth;
            
            // Clamp to max capacity
            float maxPossibleShield = dynamicMaxPlates * plateHealth;
            if (currentPlateShield > maxPossibleShield)
            {
                currentPlateShield = maxPossibleShield;
            }
            
            Debug.Log($"[ArmorPlateSystem] Replaced damaged plate! Added {plateHealth} HP, Total shield: {currentPlateShield}");
        }
        
        // VEST SYSTEM: Notify UI with dynamic max shield based on vest capacity
        float maxShield = dynamicMaxPlates * plateHealth;
        OnPlateShieldChanged?.Invoke(currentPlateCount, currentPlateShield, maxShield);
    }
    
    /// <summary>
    /// Process incoming damage - plates absorb damage first before health
    /// Returns remaining damage that should be applied to health
    /// </summary>
    public float ProcessDamage(float incomingDamage)
    {
        if (currentPlateShield <= 0)
        {
            // No shield - all damage goes to health
            return incomingDamage;
        }
        
        float damageToShield = Mathf.Min(incomingDamage, currentPlateShield);
        float remainingDamage = incomingDamage - damageToShield;
        
        // Apply damage to shield
        currentPlateShield -= damageToShield;
        
        // Check if a plate broke
        int newPlateCount = Mathf.CeilToInt(currentPlateShield / plateHealth);
        if (newPlateCount < currentPlateCount)
        {
            // Plate(s) broke!
            int platesBroken = currentPlateCount - newPlateCount;
            currentPlateCount = newPlateCount;
            
            Debug.Log($"[ArmorPlateSystem] {platesBroken} plate(s) broken! Remaining plates: {currentPlateCount}");
            
            // Play plate break sound
            GameSounds.PlayArmorBreak(transform.position);
            
            // Fire event
            OnPlateBroken?.Invoke();
            
            // Notify player
            if (currentPlateCount > 0)
            {
                CognitiveFeedManager.Instance?.QueueMessage($"*WARNING: Armor plate destroyed - {currentPlateCount} remaining*");
            }
            else
            {
                CognitiveFeedManager.Instance?.QueueMessage("*CRITICAL: All armor plates destroyed!*");
            }
        }
        
        // Clamp shield to zero
        if (currentPlateShield < 0) currentPlateShield = 0;
        
        // VEST SYSTEM: Notify UI with dynamic max shield
        float maxShield = dynamicMaxPlates * plateHealth;
        OnPlateShieldChanged?.Invoke(currentPlateCount, currentPlateShield, maxShield);
        
        Debug.Log($"[ArmorPlateSystem] Damage processed - Incoming: {incomingDamage}, To Shield: {damageToShield}, To Health: {remainingDamage}, Shield remaining: {currentPlateShield}");
        
        return remainingDamage;
    }
    
    /// <summary>
    /// Get current plate count
    /// </summary>
    public int GetCurrentPlateCount()
    {
        return currentPlateCount;
    }
    
    /// <summary>
    /// Get current shield amount
    /// </summary>
    public float GetCurrentShield()
    {
        return currentPlateShield;
    }
    
    /// <summary>
    /// Get max shield amount
    /// VEST SYSTEM: Uses dynamic max plates
    /// </summary>
    public float GetMaxShield()
    {
        return dynamicMaxPlates * plateHealth;
    }
    
    /// <summary>
    /// Get current max plates (based on equipped vest)
    /// </summary>
    public int GetMaxPlates()
    {
        return dynamicMaxPlates;
    }
    
    /// <summary>
    /// Check if player has any plates equipped
    /// </summary>
    public bool HasPlates()
    {
        return currentPlateCount > 0;
    }
    
    /// <summary>
    /// Clear all plates (called on death)
    /// VEST SYSTEM: Uses dynamic max plates
    /// </summary>
    public void ClearAllPlates()
    {
        currentPlateCount = 0;
        currentPlateShield = 0f;
        
        Debug.Log("[ArmorPlateSystem] All plates cleared");
        
        // VEST SYSTEM: Notify UI with dynamic max shield
        float maxShield = dynamicMaxPlates * plateHealth;
        OnPlateShieldChanged?.Invoke(currentPlateCount, currentPlateShield, maxShield);
    }
    
    /// <summary>
    /// VEST SYSTEM: Update max plates based on equipped vest
    /// Called by VestSlotController when vest is equipped/changed
    /// </summary>
    public void UpdateMaxPlates(int newMaxPlates)
    {
        int previousMaxPlates = dynamicMaxPlates;
        dynamicMaxPlates = Mathf.Max(1, newMaxPlates); // Minimum 1 plate
        
        Debug.Log($"[ArmorPlateSystem] Max plates updated: {previousMaxPlates} -> {dynamicMaxPlates}");
        
        // If reducing max plates, remove excess plates
        if (currentPlateCount > dynamicMaxPlates)
        {
            int platesToRemove = currentPlateCount - dynamicMaxPlates;
            currentPlateCount = dynamicMaxPlates;
            currentPlateShield = dynamicMaxPlates * plateHealth; // Full shield for remaining plates
            
            Debug.Log($"[ArmorPlateSystem] Removed {platesToRemove} excess plates due to vest downgrade");
            CognitiveFeedManager.Instance?.QueueMessage($"*Vest downgrade: {platesToRemove} plate(s) lost*");
        }
        
        // Notify UI of the change
        float maxShield = dynamicMaxPlates * plateHealth;
        OnPlateShieldChanged?.Invoke(currentPlateCount, currentPlateShield, maxShield);
        
        // Notify ShieldSliderSegments to update dividers
        ShieldSliderSegments shieldSegments = FindObjectOfType<ShieldSliderSegments>();
        if (shieldSegments != null)
        {
            shieldSegments.UpdateSegmentCount(dynamicMaxPlates);
        }
    }
    
    // ===== INVENTORY INTEGRATION =====
    
    /// <summary>
    /// Get plate count from inventory
    /// </summary>
    private int GetPlateCountFromInventory()
    {
        Debug.Log("[ArmorPlateSystem] 🔍 GetPlateCountFromInventory() called");
        
        if (inventoryManager == null)
        {
            inventoryManager = InventoryManager.Instance;
            if (inventoryManager == null)
            {
                Debug.LogError("[ArmorPlateSystem] ❌ InventoryManager not found!");
                return 0;
            }
        }
        
        // Search for plate items in inventory
        int plateCount = 0;
        var slots = inventoryManager.GetAllInventorySlots();
        Debug.Log($"[ArmorPlateSystem] 📦 Checking {slots.Count} inventory slots for armor plates");
        
        foreach (var slot in slots)
        {
            if (!slot.IsEmpty)
            {
                var itemData = slot.CurrentItem;
                Debug.Log($"[ArmorPlateSystem] 🔍 Slot contains: {itemData?.itemName} (Type: {itemData?.itemType}, Count: {slot.ItemCount})");
                
                if (itemData != null && itemData.itemType == "ArmorPlate")
                {
                    plateCount += slot.ItemCount;
                    Debug.Log($"[ArmorPlateSystem] ✅ Found {slot.ItemCount} armor plates in slot! Total so far: {plateCount}");
                }
            }
        }
        
        Debug.Log($"[ArmorPlateSystem] 📊 Total armor plates in inventory: {plateCount}");
        return plateCount;
    }
    
    /// <summary>
    /// Remove one plate from inventory
    /// </summary>
    private bool RemovePlateFromInventory()
    {
        Debug.Log("[ArmorPlateSystem] 🗑️ RemovePlateFromInventory() called");
        
        if (inventoryManager == null)
        {
            inventoryManager = InventoryManager.Instance;
            if (inventoryManager == null)
            {
                Debug.LogError("[ArmorPlateSystem] ❌ InventoryManager not found!");
                return false;
            }
        }
        
        // Find and remove one plate from inventory
        var slots = inventoryManager.GetAllInventorySlots();
        Debug.Log($"[ArmorPlateSystem] 🔍 Searching {slots.Count} slots for armor plate to remove");
        
        foreach (var slot in slots)
        {
            if (!slot.IsEmpty)
            {
                var itemData = slot.CurrentItem;
                Debug.Log($"[ArmorPlateSystem] 🔍 Checking slot: {itemData?.itemName} (Type: {itemData?.itemType})");
                
                if (itemData != null && itemData.itemType == "ArmorPlate")
                {
                    Debug.Log($"[ArmorPlateSystem] 🎯 Found armor plate! Attempting to remove 1 from {slot.ItemCount}");
                    
                    // Remove one plate using TryRemoveItem
                    bool removed = inventoryManager.TryRemoveItem(itemData, 1);
                    if (removed)
                    {
                        Debug.Log("[ArmorPlateSystem] ✅ Successfully removed 1 plate from inventory");
                        return true;
                    }
                    else
                    {
                        Debug.LogWarning("[ArmorPlateSystem] ⚠️ TryRemoveItem returned false - removal failed");
                    }
                }
            }
        }
        
        Debug.LogWarning("[ArmorPlateSystem] ❌ Failed to find plate item in inventory");
        return false;
    }
    
    /// <summary>
    /// Calculate how many plates are damaged or missing
    /// Returns the number of plates needed to reach full capacity
    /// </summary>
    private int CalculateDamagedOrMissingPlates()
    {
        // Calculate current plate health percentage
        float maxShield = dynamicMaxPlates * plateHealth;
        float currentHealthPercentage = currentPlateShield / maxShield;
        
        // Each plate represents 33.33% (for 3 plates), 50% (for 2 plates), or 100% (for 1 plate)
        float platePercentage = 1f / dynamicMaxPlates;
        
        // Calculate how many "plate slots" worth of damage we have
        float missingPlateSlots = (maxShield - currentPlateShield) / plateHealth;
        
        // Round up - if we have ANY damage, we need at least 1 plate to fix it
        int platesNeeded = Mathf.CeilToInt(missingPlateSlots);
        
        Debug.Log($"[ArmorPlateSystem] Damage calculation: Current shield: {currentPlateShield}/{maxShield} ({currentHealthPercentage:P0}), Plates needed: {platesNeeded}");
        
        return platesNeeded;
    }
}
