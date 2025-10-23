using UnityEngine;

/// <summary>
/// Specialized item data for goods that can be opened by double-clicking.
/// Extends ChestItemData with tier-based gem rewards and opening functionality.
/// </summary>
[CreateAssetMenu(fileName = "New Goods Item", menuName = "Inventory/Goods Item")]
public class GoodsItem : ChestItemData
{
    [Header("Goods Properties")]
    [Tooltip("Tier level of the goods (1, 2, 3, etc.)")]
    public int goodsTier = 1;
    
    [Header("Gem Rewards")]
    [Tooltip("Minimum gems awarded when opened")]
    public int minGems = 5;
    [Tooltip("Maximum gems awarded when opened")]
    public int maxGems = 15;
    
    [Header("Visual Effects")]
    [Tooltip("Sprite to show when the goods is opened (before fade out)")]
    public Sprite openedSprite;
    [Tooltip("Duration of fade out animation after opening")]
    public float fadeOutDuration = 2.0f;
    
    /// <summary>
    /// Calculate the gem reward for this goods item
    /// </summary>
    public int CalculateGemReward()
    {
        return Random.Range(minGems, maxGems + 1);
    }
    
    /// <summary>
    /// Check if this item is a goods item that can be opened
    /// </summary>
    public bool IsOpenableGoods()
    {
        return itemName.ToLower().Contains("goods") || itemType.ToLower().Contains("goods");
    }
    
    /// <summary>
    /// Get display name with tier information
    /// </summary>
    public string GetTierDisplayName()
    {
        return $"Tier {goodsTier} {itemName}";
    }
}
