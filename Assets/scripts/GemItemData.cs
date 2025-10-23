using UnityEngine;

/// <summary>
/// Gem item data for the uniform item system.
/// Gems are now first-class items that work with the standard slot system.
/// </summary>
[CreateAssetMenu(fileName = "New Gem", menuName = "Inventory/Gem")]
public class GemItemData : ChestItemData
{
    [Header("Gem Properties")]
    public int gemValue = 1; // How many gems this item represents
    
    void OnEnable()
    {
        // Set default gem properties
        if (string.IsNullOrEmpty(itemName))
            itemName = "Gem";
            
        if (string.IsNullOrEmpty(itemType))
            itemType = "Gem";
            
        if (itemRarity == 0)
            itemRarity = 3; // Rare by default
            
        if (rarityColor == Color.white)
            rarityColor = new Color(0.0f, 1.0f, 0.8f); // Cyan for gems
            
        // Generate ID if not set (can't call base.OnEnable since it's private)
        if (string.IsNullOrEmpty(itemID))
        {
            GenerateID();
        }
    }
    
    /// <summary>
    /// Check if this is a gem item
    /// </summary>
    public bool IsGem()
    {
        return itemType == "Gem" || itemName.ToLower().Contains("gem");
    }
}
