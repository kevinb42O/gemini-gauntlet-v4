using UnityEngine;
using System.Collections;
using GeminiGauntlet.UI;
using GeminiGauntlet.Audio;

/// <summary>
/// World pickup for equippable sword item
/// Press E within 250 units to collect
/// Includes bobbing/rotation effects for visibility
/// </summary>
public class WorldSwordPickup : MonoBehaviour
{
    [Header("Item Configuration")]
    [Tooltip("The sword weapon item data")]
    public EquippableWeaponItemData swordItemData;
    
    [Header("Pickup Settings")]
    [Tooltip("Distance at which player can pickup sword (scaled for 300+ unit game)")]
    public float pickupRange = 250f;
    
    [Tooltip("Should sword bob up and down?")]
    public bool enableBobbing = true;
    public float bobbingSpeed = 1.5f;
    public float bobbingHeight = 10f; // Scaled for 300+ unit game
    
    [Tooltip("Should sword rotate?")]
    public bool enableRotation = true;
    public float rotationSpeed = 30f;
    
    [Header("Visual Feedback")]
    [Tooltip("Glow/highlight effect when in range (optional)")]
    public GameObject highlightEffect;
    
    [Tooltip("Pickup prompt UI (optional)")]
    public GameObject pickupPromptUI;
    
    // State
    private Transform _playerTransform;
    private InventoryManager _inventoryManager;
    private Vector3 _originalPosition;
    private bool _isInRange = false;
    private bool _canPickup = true;
    
    void Start()
    {
        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerTransform = player.transform;
        }
        else
        {
            Debug.LogError("[WorldSwordPickup] ❌ Player not found! Cannot function.");
        }
        
        // Find inventory manager
        _inventoryManager = InventoryManager.Instance;
        if (_inventoryManager == null)
        {
            Debug.LogError("[WorldSwordPickup] ❌ InventoryManager not found!");
        }
        
        // Store original position for bobbing
        _originalPosition = transform.position;
        
        // ⚠️ CRITICAL VALIDATION: Ensure item data is assigned
        if (swordItemData == null)
        {
            Debug.LogError($"[WorldSwordPickup] ❌❌❌ CRITICAL: No swordItemData assigned to {gameObject.name}!");
            Debug.LogError($"[WorldSwordPickup] Please assign an EquippableWeaponItemData asset in the Inspector!");
            enabled = false; // Disable this component
            return;
        }
        
        // Validate icon
        if (swordItemData.itemIcon == null)
        {
            Debug.LogError($"[WorldSwordPickup] ❌ CRITICAL: {swordItemData.itemName} has NO ICON assigned!");
            Debug.LogError($"[WorldSwordPickup] Open the {swordItemData.name} asset and assign an Item Icon sprite!");
        }
        else
        {
            Debug.Log($"[WorldSwordPickup] ✅ Sword data loaded: {swordItemData.itemName} with icon: {swordItemData.itemIcon.name}");
        }
        
        // Hide effects initially
        if (highlightEffect != null) highlightEffect.SetActive(false);
        if (pickupPromptUI != null) pickupPromptUI.SetActive(false);
    }
    
    void Update()
    {
        if (!_canPickup || _playerTransform == null) return;
        
        // Apply visual effects
        ApplyVisualEffects();
        
        // Check distance to player
        float distance = Vector3.Distance(transform.position, _playerTransform.position);
        bool wasInRange = _isInRange;
        _isInRange = distance <= pickupRange;
        
        // Toggle UI effects based on range
        if (_isInRange != wasInRange)
        {
            OnRangeChanged(_isInRange);
        }
        
        // Handle E key pickup
        if (_isInRange && Input.GetKeyDown(KeyCode.E))
        {
            TryPickupSword();
        }
    }
    
    /// <summary>
    /// Apply bobbing and rotation visual effects
    /// </summary>
    private void ApplyVisualEffects()
    {
        if (enableBobbing)
        {
            float bobOffset = Mathf.Sin(Time.time * bobbingSpeed) * bobbingHeight;
            transform.position = _originalPosition + Vector3.up * bobOffset;
        }
        
        if (enableRotation)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }
    
    /// <summary>
    /// Called when player enters/exits pickup range
    /// </summary>
    private void OnRangeChanged(bool inRange)
    {
        if (highlightEffect != null)
        {
            highlightEffect.SetActive(inRange);
        }
        
        if (pickupPromptUI != null)
        {
            pickupPromptUI.SetActive(inRange);
        }
        
        if (inRange)
        {
            Debug.Log($"[WorldSwordPickup] Player in range - press E to pickup {swordItemData?.itemName}");
        }
    }
    
    /// <summary>
    /// Attempt to pickup the sword
    /// AUTO-EQUIP: If appropriate hand weapon slot is empty, equip directly based on handType
    /// - RIGHT hand swords → rightHandWeaponSlot
    /// - LEFT hand swords → leftHandWeaponSlot
    /// Otherwise, go to inventory
    /// </summary>
    private void TryPickupSword()
    {
        if (swordItemData == null || _inventoryManager == null)
        {
            Debug.LogError("[WorldSwordPickup] Cannot pickup - missing references!");
            return;
        }
        
        // Determine which hand this sword is for
        bool isRightHand = ((int)swordItemData.allowedHands & 1) != 0; // RightHand = 1 << 0 = 1
        bool isLeftHand = ((int)swordItemData.allowedHands & 2) != 0;  // LeftHand = 1 << 1 = 2
        
        // Check if weapon equipment manager exists and appropriate hand slot is empty
        WeaponEquipmentManager weaponManager = WeaponEquipmentManager.Instance;
        bool canAutoEquip = false;
        UnifiedSlot targetSlot = null;
        
        if (weaponManager != null)
        {
            if (isRightHand && weaponManager.rightHandWeaponSlot != null && weaponManager.rightHandWeaponSlot.IsEmpty)
            {
                canAutoEquip = true;
                targetSlot = weaponManager.rightHandWeaponSlot;
            }
            else if (isLeftHand && weaponManager.leftHandWeaponSlot != null && weaponManager.leftHandWeaponSlot.IsEmpty)
            {
                canAutoEquip = true;
                targetSlot = weaponManager.leftHandWeaponSlot;
            }
        }
        
        if (canAutoEquip && targetSlot != null)
        {
            // AUTO-EQUIP: Directly equip to appropriate hand slot
            string handName = isRightHand ? "RIGHT" : "LEFT";
            Debug.Log($"[WorldSwordPickup] ⚔️ AUTO-EQUIP: {handName} hand empty - equipping {swordItemData.itemName} directly!");
            
            targetSlot.SetItem(swordItemData, 1, bypassValidation: true);
            
            // ⚔️ AUTO-ACTIVATE: After equipping, activate sword mode if not already active
            PlayerShooterOrchestrator playerShooter = _playerTransform?.GetComponent<PlayerShooterOrchestrator>();
            if (playerShooter != null)
            {
                // Check appropriate hand's sword mode state
                bool isAlreadyActive = isRightHand ? playerShooter.IsSwordModeActive : playerShooter.IsLeftSwordModeActive;
                
                if (!isAlreadyActive)
                {
                    // ⭐ CRITICAL FIX: Run coroutine on PLAYER (persistent object), not this pickup (about to be destroyed)
                    playerShooter.StartCoroutine(ActivateSwordModeNextFrame(playerShooter, isRightHand));
                    Debug.Log($"[WorldSwordPickup] ⚔️ Started {handName} auto-activation coroutine on player");
                }
            }
            
            // Show floating text notification
            ShowEquipNotification();
            
            // Play pickup sound
            GameSounds.PlayGemCollection(_playerTransform.position);
            
            // Trigger EQUIP animation on player (not just grab)
            TriggerEquipAnimation();
            
            // Destroy this world item
            Destroy(gameObject);
        }
        else
        {
            // Normal flow: Try to add to inventory
            if (_inventoryManager.TryAddItem(swordItemData, 1))
            {
                Debug.Log($"[WorldSwordPickup] ✅ Picked up {swordItemData.itemName} (added to inventory)");
                
                // Show floating text notification using FloatingTextManager
                ShowPickupNotification();
                
                // Play pickup sound (using existing gem collection sound for now)
                GameSounds.PlayGemCollection(_playerTransform.position);
                
                // Trigger "grab" animation on player (same as chest interaction)
                TriggerGrabAnimation();
                
                // Destroy this world item
                Destroy(gameObject);
            }
            else
            {
                Debug.Log($"[WorldSwordPickup] ❌ Inventory full - cannot pickup {swordItemData.itemName}");
                
                // Show error message using CognitiveFeedManagerEnhanced
                CognitiveFeedManagerEnhanced cognitive = CognitiveFeedManagerEnhanced.Instance;
                if (cognitive != null)
                {
                    cognitive.ShowQuickNotification("⚠️ Inventory Full!", duration: 2f);
                }
            }
        }
    }
    
    /// <summary>
    /// Show pickup notification using CognitiveFeedManagerEnhanced (proper inventory feedback)
    /// </summary>
    private void ShowPickupNotification()
    {
        CognitiveFeedManagerEnhanced cognitive = CognitiveFeedManagerEnhanced.Instance;
        if (cognitive != null)
        {
            string message = $"⚔️ {swordItemData.itemName} Acquired!";
            cognitive.ShowQuickNotification(message, duration: 2.5f);
            Debug.Log($"[WorldSwordPickup] 🧠 Cognitive notification: {message}");
        }
        else
        {
            Debug.LogWarning("[WorldSwordPickup] ⚠️ CognitiveFeedManagerEnhanced not found - notification skipped");
        }
    }
    
    /// <summary>
    /// Show equip notification using CognitiveFeedManagerEnhanced (when auto-equipped)
    /// </summary>
    private void ShowEquipNotification()
    {
        CognitiveFeedManagerEnhanced cognitive = CognitiveFeedManagerEnhanced.Instance;
        if (cognitive != null)
        {
            string message = $"⚔️ {swordItemData.itemName} Equipped!";
            cognitive.ShowQuickNotification(message, duration: 2.5f);
            Debug.Log($"[WorldSwordPickup] 🧠 Cognitive notification: {message}");
        }
        else
        {
            Debug.LogWarning("[WorldSwordPickup] ⚠️ CognitiveFeedManagerEnhanced not found - notification skipped");
        }
    }
    
    /// <summary>
    /// Trigger "grab" animation on player (same as chest interaction)
    /// </summary>
    private void TriggerGrabAnimation()
    {
        if (_playerTransform == null) return;
        
        // Find PlayerAnimationStateManager on player
        PlayerAnimationStateManager animManager = _playerTransform.GetComponent<PlayerAnimationStateManager>();
        if (animManager != null)
        {
            // Trigger grab animation (same as taking items from chest)
            Animator playerAnimator = _playerTransform.GetComponent<Animator>();
            if (playerAnimator != null)
            {
                playerAnimator.SetTrigger("Grab"); // Adjust trigger name if different
                Debug.Log("[WorldSwordPickup] ✅ Triggered grab animation");
            }
        }
    }
    
    /// <summary>
    /// Trigger EQUIP animation on player (when weapon auto-equipped from world)
    /// This should play the sword draw/equip animation
    /// </summary>
    private void TriggerEquipAnimation()
    {
        if (_playerTransform == null) return;
        
        // ⭐ REMOVED: Direct ToggleSwordMode() call
        // WeaponEquipmentManager will handle sword mode activation via OnSlotChanged event
        // This prevents duplicate animation triggers and mode conflicts
        Debug.Log("[WorldSwordPickup] ⚔️ Sword equipped - WeaponEquipmentManager will handle animation");
    }
    
    /// <summary>
    /// Coroutine: Activate sword mode on the next frame after equipping
    /// This ensures WeaponEquipmentManager processes the slot change first via OnSlotChanged event
    /// Then we activate sword mode to complete the seamless pickup flow
    /// ⭐ STATIC: Can run on any MonoBehaviour (runs on player since pickup gets destroyed)
    /// ⚔️ DUAL-HAND: Supports both RIGHT hand (Mouse4) and LEFT hand (Mouse5) sword activation
    /// </summary>
    private static System.Collections.IEnumerator ActivateSwordModeNextFrame(PlayerShooterOrchestrator playerShooter, bool isRightHand)
    {
        // Wait for next frame to ensure WeaponEquipmentManager.CheckXXXHandEquipment() has executed
        yield return null;
        
        if (isRightHand)
        {
            // RIGHT HAND sword activation
            if (playerShooter.CanUseSwordMode() && !playerShooter.IsSwordModeActive)
            {
                playerShooter.ToggleSwordMode();
                Debug.Log("[WorldSwordPickup] ⚔️ AUTO-ACTIVATED RIGHT sword mode after pickup!");
            }
            else
            {
                Debug.LogWarning($"[WorldSwordPickup] ⚠️ Could not auto-activate RIGHT sword mode - CanUse: {playerShooter.CanUseSwordMode()}, IsActive: {playerShooter.IsSwordModeActive}");
            }
        }
        else
        {
            // LEFT HAND sword activation
            if (playerShooter.CanUseLeftSwordMode() && !playerShooter.IsLeftSwordModeActive)
            {
                playerShooter.ToggleLeftSwordMode();
                Debug.Log("[WorldSwordPickup] ⚔️ AUTO-ACTIVATED LEFT sword mode after pickup!");
            }
            else
            {
                Debug.LogWarning($"[WorldSwordPickup] ⚠️ Could not auto-activate LEFT sword mode - CanUse: {playerShooter.CanUseLeftSwordMode()}, IsActive: {playerShooter.IsLeftSwordModeActive}");
            }
        }
    }
    
    /// <summary>
    /// Visualize pickup range in editor
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
        
        // Draw a vertical line to show pickup height
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 100f);
    }
}
