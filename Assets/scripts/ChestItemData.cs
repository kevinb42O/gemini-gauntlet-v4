using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ChestItemData : ScriptableObject
{
    [Header("Basic Information")]
    public string itemName;
    [TextArea(3, 5)]
    public string description;
    public Sprite itemIcon;
    
    [Header("Item Properties")]
    public string itemType; // e.g., "Weapon", "Material", "Consumable"
    public int itemRarity; // 1-5, with 5 being the most rare
    public string craftingCategory; // For crafting system integration
    
    [Header("Visual Feedback")]
    public Color rarityColor = Color.white;
    
    // Potential crafting system hooks
    public string[] craftingComponents;
    public int craftingValue = 1;
    
    // Optional unique identifier
    public string itemID;
    
    public void GenerateID()
    {
        if (string.IsNullOrEmpty(itemID))
        {
            // Generate a DETERMINISTIC ID based on name and type (no random component)
            // This ensures all instances of the same item type can stack properly
            itemID = $"{itemName.Replace(" ", "")}_{itemType}";
        }
    }
    
    // Constructor that auto-generates an ID
    private void OnEnable()
    {
        // Only generate ID if itemName is set to prevent NullReferenceException
        if (!string.IsNullOrEmpty(itemName))
        {
            GenerateID();
        }
    }
    
    // Helper method to create color based on rarity
    public Color GetRarityColor()
    {
        // Use the defined color if it's not the default white
        if (rarityColor != Color.white)
        {
            return rarityColor;
        }
        
        // Otherwise generate based on rarity value
        switch (itemRarity)
        {
            case 1: return new Color(0.7f, 0.7f, 0.7f); // Common - Gray
            case 2: return new Color(0.0f, 0.8f, 0.0f); // Uncommon - Green
            case 3: return new Color(0.0f, 0.5f, 1.0f); // Rare - Blue
            case 4: return new Color(0.8f, 0.0f, 0.8f); // Epic - Purple
            case 5: return new Color(1.0f, 0.5f, 0.0f); // Legendary - Orange
            default: return Color.white;
        }
    }
    
    // Check if two items are the same type for stacking purposes
    // VIRTUAL: Can be overridden by derived classes (e.g., SelfReviveItemData to prevent stacking)
    public virtual bool IsSameItem(ChestItemData other)
    {
        if (other == null) return false;
        
        // First check by ID if available
        if (!string.IsNullOrEmpty(itemID) && !string.IsNullOrEmpty(other.itemID))
        {
            return itemID == other.itemID;
        }
        
        // Fall back to name comparison if IDs aren't available
        return itemName == other.itemName && itemType == other.itemType;
    }
}
