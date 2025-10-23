using UnityEngine;

[CreateAssetMenu(fileName = "Self-Revive Item", menuName = "Inventory/Self-Revive Item")]
public class SelfReviveItemData : ChestItemData
{
    [Header("Self-Revive Properties")]
    [Tooltip("How long the revive effect lasts (for future implementation)")]
    public float reviveDuration = 5f;
    
    [Tooltip("Health percentage restored on revive (0-1)")]
    [Range(0f, 1f)]
    public float healthRestorePercentage = 1f;
    
    [Tooltip("Whether this revive provides temporary invincibility")]
    public bool providesInvincibility = true;
    
    [Tooltip("Duration of invincibility after revive")]
    public float invincibilityDuration = 3f;
    
    private void Awake()
    {
        // Set default values for self-revive items
        if (string.IsNullOrEmpty(itemName))
            itemName = "Self-Revive";
            
        if (string.IsNullOrEmpty(itemType))
            itemType = "Self-Revive";
            
        if (string.IsNullOrEmpty(description))
            description = "A mysterious device that can bring you back from the brink of death. Use wisely.";
            
        if (itemRarity == 0)
            itemRarity = 4; // Epic rarity by default
            
        if (rarityColor == Color.white)
            rarityColor = new Color(0.8f, 0.0f, 0.8f); // Purple for epic
            
        if (string.IsNullOrEmpty(craftingCategory))
            craftingCategory = "Consumable";
    }
    
    /// <summary>
    /// Self-revive items are NOT the same for stacking purposes (each is unique)
    /// Override IsSameItem to prevent stacking - each self-revive occupies its own slot
    /// </summary>
    public override bool IsSameItem(ChestItemData other)
    {
        return false; // Self-revives are always treated as different items - NEVER stack
    }
    
    /// <summary>
    /// Check if this item should go to the revive slot
    /// </summary>
    public bool IsReviveItem()
    {
        return itemType.ToLower().Contains("revive") || 
               itemName.ToLower().Contains("revive") ||
               itemType == "Self-Revive";
    }
    
    /// <summary>
    /// Get the revive effectiveness (for future implementation)
    /// </summary>
    public float GetReviveEffectiveness()
    {
        // Base effectiveness on rarity
        return 0.5f + (itemRarity * 0.1f); // 60% to 100% effectiveness
    }
}
