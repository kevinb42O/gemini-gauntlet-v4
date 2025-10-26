using UnityEngine;

/// <summary>
/// Equippable weapon item that can be placed in weapon equipment slots
/// Supports both right and left hand (future expansion)
/// Extends ChestItemData to work with ALL existing systems (chests, inventory, forge, world items)
/// </summary>
[CreateAssetMenu(fileName = "New Weapon", menuName = "Inventory/Equippable Weapon")]
public class EquippableWeaponItemData : ChestItemData
{
    [Header("Weapon Equipment Settings")]
    [Tooltip("Which hand(s) this weapon can be equipped to")]
    public WeaponHandType allowedHands = WeaponHandType.RightHand;
    
    [Header("Weapon Visual")]
    [Tooltip("Path to weapon prefab in Assets folder (for world item spawning)")]
    public string weaponPrefabPath = "Assets/prefabs_made/SWORD/sword-of-arturias";
    
    [Tooltip("World item model prefab (the physical pickup in the world)")]
    public GameObject worldItemModel;
    
    [Header("Weapon Properties")]
    [Tooltip("Weapon identifier for PlayerShooterOrchestrator (e.g., 'sword', 'dagger')")]
    public string weaponTypeID = "sword";
    
    [Tooltip("Is this weapon currently unique/legendary? (prevents stacking)")]
    public bool isUniqueWeapon = true;
    
    /// <summary>
    /// Unique weapons don't stack - each is its own slot
    /// </summary>
    public override bool IsSameItem(ChestItemData other)
    {
        if (isUniqueWeapon) return false; // Unique weapons never stack
        return base.IsSameItem(other);
    }
}

/// <summary>
/// Defines which hand(s) a weapon can be equipped to
/// Uses flags for future dual-wielding support
/// </summary>
[System.Flags]
public enum WeaponHandType
{
    RightHand = 1 << 0,  // Binary: 01
    LeftHand = 1 << 1,   // Binary: 10
    BothHands = RightHand | LeftHand // Binary: 11
}
