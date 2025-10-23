using UnityEngine;

/// <summary>
/// Armor Plate item data - stackable consumable that provides armor protection
/// Plates are applied via the ArmorPlateSystem and do NOT persist between game sessions
/// </summary>
[CreateAssetMenu(fileName = "ArmorPlate", menuName = "Inventory/Armor Plate")]
public class ArmorPlateItemData : ChestItemData
{
    [Header("Armor Plate Properties")]
    [Tooltip("Amount of damage this plate can absorb")]
    public float plateShieldAmount = 1500f;
    
    [Tooltip("Armor plates are stackable in inventory")]
    public bool isStackable = true;
    
    private void OnValidate()
    {
        // Ensure armor plates are always set to correct type
        itemType = "ArmorPlate";
        
        // Set default values if not configured
        if (string.IsNullOrEmpty(itemName))
        {
            itemName = "Armor Plate";
        }
        
        if (string.IsNullOrEmpty(description))
        {
            description = "Protective armor plating that absorbs damage before health is affected. Press C to equip up to 3 plates. Each plate provides 1500 shield points.";
        }
        
        // Set rarity to uncommon (blue)
        if (itemRarity == 0)
        {
            itemRarity = 3; // Rare/Blue
            rarityColor = new Color(0.0f, 0.5f, 1.0f);
        }
    }
    
    /// <summary>
    /// Armor plates are stackable
    /// </summary>
    public override bool IsSameItem(ChestItemData other)
    {
        if (!isStackable) return false;
        
        // Check if other item is also an armor plate
        ArmorPlateItemData otherPlate = other as ArmorPlateItemData;
        if (otherPlate == null) return false;
        
        // All armor plates stack together
        return itemType == other.itemType && itemName == other.itemName;
    }
}
