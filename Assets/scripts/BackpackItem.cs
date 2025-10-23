using UnityEngine;

/// <summary>
/// BackpackItem - ScriptableObject for backpack items with tier-based slot expansion
/// Tier 1: 5 slots (default), Tier 2: 7 slots, Tier 3: 11 slots
/// Note: Gem slot always exists and is separate from backpack slot count
/// </summary>
[CreateAssetMenu(fileName = "New Backpack", menuName = "Inventory/Backpack Item")]
public class BackpackItem : ChestItemData
{
    [Header("Backpack Configuration")]
    [Tooltip("Backpack tier (1 = 5 slots, 2 = 7 slots, 3 = 11 slots)")]
    [Range(1, 3)]
    public int backpackTier = 1;
    
    [Tooltip("Number of inventory slots this backpack provides")]
    public int slotCount = 5;
    
    [Header("Visual Configuration")]
    [Tooltip("Backpack icon for UI display")]
    public Sprite backpackIcon;
    
    void OnValidate()
    {
        // Auto-set slot count based on tier
        switch (backpackTier)
        {
            case 1:
                slotCount = 5;
                break;
            case 2:
                slotCount = 7;
                break;
            case 3:
                slotCount = 11;
                break;
        }
        
        // Ensure this is marked as a backpack item type
        if (string.IsNullOrEmpty(itemType) || itemType != "Backpack")
        {
            itemType = "Backpack";
        }
        
        // Use backpack icon if available, otherwise fall back to itemIcon
        if (backpackIcon != null && itemIcon != backpackIcon)
        {
            itemIcon = backpackIcon;
        }
    }
    
    /// <summary>
    /// Get the number of inventory slots this backpack provides
    /// </summary>
    public int GetSlotCount()
    {
        return slotCount;
    }
    
    /// <summary>
    /// Get the tier of this backpack
    /// </summary>
    public int GetTier()
    {
        return backpackTier;
    }
    
    /// <summary>
    /// Check if this backpack is better than another backpack
    /// </summary>
    public bool IsBetterThan(BackpackItem otherBackpack)
    {
        if (otherBackpack == null) return true;
        return backpackTier > otherBackpack.backpackTier;
    }
    
    /// <summary>
    /// Get display name with tier information
    /// </summary>
    public string GetDisplayName()
    {
        return $"{itemName} (Tier {backpackTier})";
    }
    
    /// <summary>
    /// Get description with slot information
    /// </summary>
    public override string ToString()
    {
        return $"{GetDisplayName()} - Provides {slotCount} inventory slots";
    }
}
