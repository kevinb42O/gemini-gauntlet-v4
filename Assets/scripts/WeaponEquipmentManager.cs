using UnityEngine;
using GeminiGauntlet.UI;

/// <summary>
/// Manages weapon equipment slots for both hands
/// Integrates with PlayerShooterOrchestrator to enable/disable sword mode
/// Singleton pattern for easy access across systems
/// </summary>
public class WeaponEquipmentManager : MonoBehaviour
{
    public static WeaponEquipmentManager Instance { get; private set; }
    
    [Header("Equipment Slot References")]
    [Tooltip("Right hand weapon slot (UnifiedSlot with isWeaponSlot = true, NOT isEquipmentSlot)")]
    public UnifiedSlot rightHandWeaponSlot;
    
    [Tooltip("Left hand weapon slot (UnifiedSlot with isWeaponSlot = true, NOT isEquipmentSlot) - FUTURE")]
    public UnifiedSlot leftHandWeaponSlot;
    
    [Header("Integration References")]
    [Tooltip("Reference to PlayerShooterOrchestrator (auto-found if null)")]
    public PlayerShooterOrchestrator playerShooter;
    
    // Current equipped weapons
    private EquippableWeaponItemData _rightHandWeapon;
    private EquippableWeaponItemData _leftHandWeapon;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("[WeaponEquipmentManager] Duplicate instance destroyed");
            Destroy(gameObject);
            return;
        }
        
        // Find PlayerShooterOrchestrator if not assigned
        if (playerShooter == null)
        {
            playerShooter = FindObjectOfType<PlayerShooterOrchestrator>();
            if (playerShooter == null)
            {
                Debug.LogError("[WeaponEquipmentManager] ❌ PlayerShooterOrchestrator not found! Weapon system will not function.");
            }
        }
    }
    
    void Start()
    {
        // Subscribe to slot change events
        if (rightHandWeaponSlot != null)
        {
            rightHandWeaponSlot.OnSlotChanged += OnRightHandSlotChanged;
            
            // Check initial equipment state
            CheckRightHandEquipment();
        }
        else
        {
            Debug.LogWarning("[WeaponEquipmentManager] ⚠️ Right hand weapon slot not assigned!");
        }
        
        if (leftHandWeaponSlot != null)
        {
            leftHandWeaponSlot.OnSlotChanged += OnLeftHandSlotChanged;
            
            // Check initial equipment state
            CheckLeftHandEquipment();
        }
        else
        {
            Debug.LogWarning("[WeaponEquipmentManager] ⚠️ Left hand weapon slot not assigned!");
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (rightHandWeaponSlot != null)
        {
            rightHandWeaponSlot.OnSlotChanged -= OnRightHandSlotChanged;
        }
        
        if (leftHandWeaponSlot != null)
        {
            leftHandWeaponSlot.OnSlotChanged -= OnLeftHandSlotChanged;
        }
    }
    
    /// <summary>
    /// Called when right hand weapon slot changes
    /// </summary>
    private void OnRightHandSlotChanged(ChestItemData item, int count)
    {
        CheckRightHandEquipment();
    }
    
    /// <summary>
    /// Called when left hand weapon slot changes
    /// </summary>
    private void OnLeftHandSlotChanged(ChestItemData item, int count)
    {
        CheckLeftHandEquipment();
    }
    
    /// <summary>
    /// Check what weapon is equipped in left hand and update PlayerShooterOrchestrator
    /// </summary>
    private void CheckLeftHandEquipment()
    {
        if (leftHandWeaponSlot == null || playerShooter == null) return;
        
        // Get current item in left hand slot
        ChestItemData currentItem = leftHandWeaponSlot.CurrentItem;
        
        if (currentItem != null && currentItem is EquippableWeaponItemData weaponData)
        {
            // Weapon equipped
            _leftHandWeapon = weaponData;
            
            // Tell PlayerShooterOrchestrator that left sword is available
            if (weaponData.weaponTypeID == "sword")
            {
                playerShooter.SetLeftSwordAvailable(true);
                Debug.Log($"[WeaponEquipmentManager] ✅ LEFT HAND Sword equipped - left sword mode now available!");
                
                // ⚔️ AUTO-ACTIVATE left sword mode when equipped via inventory
                if (!playerShooter.IsLeftSwordModeActive)
                {
                    playerShooter.ToggleLeftSwordMode();
                    Debug.Log("[WeaponEquipmentManager] ⚔️ AUTO-ACTIVATED LEFT sword mode after equipping from inventory!");
                    
                    // Show equipment notification
                    ShowEquipmentNotification($"⚔️ {weaponData.itemName} Equipped (LEFT HAND)!", Color.cyan);
                }
                else
                {
                    Debug.Log("[WeaponEquipmentManager] ℹ️ Left sword already active - no need to toggle");
                    ShowEquipmentNotification($"⚔️ {weaponData.itemName} Equipped (LEFT)!", Color.cyan);
                }
            }
        }
        else
        {
            // No weapon equipped
            _leftHandWeapon = null;
            
            // Tell PlayerShooterOrchestrator to disable left sword mode
            playerShooter.SetLeftSwordAvailable(false);
            Debug.Log($"[WeaponEquipmentManager] ❌ No LEFT weapon equipped - left sword mode disabled");
            
            // Show notification when unequipping
            if (currentItem == null)
            {
                ShowEquipmentNotification("⚔️ LEFT Weapon Unequipped", new Color(1f, 0.5f, 0f));
            }
        }
    }
    
    /// <summary>
    /// Check what weapon is equipped in right hand and update PlayerShooterOrchestrator
    /// </summary>
    private void CheckRightHandEquipment()
    {
        if (rightHandWeaponSlot == null || playerShooter == null) return;
        
        // Get current item in right hand slot
        ChestItemData currentItem = rightHandWeaponSlot.CurrentItem;
        
        if (currentItem != null && currentItem is EquippableWeaponItemData weaponData)
        {
            // Weapon equipped
            _rightHandWeapon = weaponData;
            
            // Tell PlayerShooterOrchestrator that sword is available
            if (weaponData.weaponTypeID == "sword")
            {
                playerShooter.SetSwordAvailable(true);
                Debug.Log($"[WeaponEquipmentManager] ✅ Sword equipped - sword mode now available!");
                
                // ⚔️ UNIFIED SYSTEM FIX: Auto-activate sword mode when equipped via inventory
                // This makes inventory equip work EXACTLY like manual Mouse4 toggle
                // If sword mode is not already active, activate it now
                if (!playerShooter.IsSwordModeActive)
                {
                    playerShooter.ToggleSwordMode();
                    Debug.Log("[WeaponEquipmentManager] ⚔️ AUTO-ACTIVATED sword mode after equipping from inventory!");
                    
                    // Show floating text notification
                    ShowEquipmentNotification($"⚔️ {weaponData.itemName} Equipped & Activated!", Color.cyan);
                }
                else
                {
                    Debug.Log("[WeaponEquipmentManager] ℹ️ Sword already active - no need to toggle");
                    
                    // Show floating text notification
                    ShowEquipmentNotification($"⚔️ {weaponData.itemName} Equipped!", Color.cyan);
                }
            }
        }
        else
        {
            // No weapon equipped
            _rightHandWeapon = null;
            
            // Tell PlayerShooterOrchestrator to disable sword mode
            // This will auto-deactivate sword mode if it was active (handled in SetSwordAvailable)
            playerShooter.SetSwordAvailable(false);
            Debug.Log($"[WeaponEquipmentManager] ❌ No weapon equipped - sword mode disabled");
            
            // Show floating text notification when unequipping
            if (currentItem == null)
            {
                ShowEquipmentNotification("⚔️ Weapon Unequipped", new Color(1f, 0.5f, 0f)); // Orange
            }
        }
    }
    
    /// <summary>
    /// Check if player has a specific weapon type equipped in right hand
    /// </summary>
    public bool HasRightHandWeapon(string weaponTypeID)
    {
        return _rightHandWeapon != null && _rightHandWeapon.weaponTypeID == weaponTypeID;
    }
    
    /// <summary>
    /// Check if player has a specific weapon type equipped in left hand
    /// </summary>
    public bool HasLeftHandWeapon(string weaponTypeID)
    {
        return _leftHandWeapon != null && _leftHandWeapon.weaponTypeID == weaponTypeID;
    }
    
    /// <summary>
    /// Get currently equipped right hand weapon
    /// </summary>
    public EquippableWeaponItemData GetRightHandWeapon()
    {
        return _rightHandWeapon;
    }
    
    /// <summary>
    /// Get currently equipped left hand weapon
    /// </summary>
    public EquippableWeaponItemData GetLeftHandWeapon()
    {
        return _leftHandWeapon;
    }
    
    /// <summary>
    /// Show equipment notification using FloatingTextManager
    /// </summary>
    private void ShowEquipmentNotification(string message, Color color)
    {
        if (FloatingTextManager.Instance != null && playerShooter != null)
        {
            Vector3 displayPosition = playerShooter.transform.position + Vector3.up * 50f; // 50 units above player
            FloatingTextManager.Instance.ShowFloatingText(message, displayPosition, color, customSize: 24);
        }
    }
    
    /// <summary>
    /// Force refresh equipment state (call after loading save game)
    /// </summary>
    public void RefreshEquipmentState()
    {
        CheckRightHandEquipment();
        CheckLeftHandEquipment();
    }
}
