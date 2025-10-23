using UnityEngine;

/// <summary>
/// Helper script for testing the goods opening system.
/// Provides context menu methods to add goods items to inventory for testing.
/// </summary>
public class GoodsTestHelper : MonoBehaviour
{
    [Header("Test Items")]
    [Tooltip("Tier 1 Goods item to add for testing")]
    public GoodsItem tier1Goods;
    [Tooltip("Tier 2 Goods item to add for testing")]
    public GoodsItem tier2Goods;
    [Tooltip("Tier 3 Goods item to add for testing")]
    public GoodsItem tier3Goods;
    
    /// <summary>
    /// Add a Tier 1 goods item to the first empty inventory slot
    /// </summary>
    [ContextMenu("Add Tier 1 Goods to Inventory")]
    private void AddTier1Goods()
    {
        if (tier1Goods != null)
        {
            AddGoodsToInventory(tier1Goods, "Tier 1");
        }
        else
        {
            Debug.LogWarning("[GoodsTestHelper] Tier 1 Goods item not assigned!");
        }
    }
    
    /// <summary>
    /// Add a Tier 2 goods item to the first empty inventory slot
    /// </summary>
    [ContextMenu("Add Tier 2 Goods to Inventory")]
    private void AddTier2Goods()
    {
        if (tier2Goods != null)
        {
            AddGoodsToInventory(tier2Goods, "Tier 2");
        }
        else
        {
            Debug.LogWarning("[GoodsTestHelper] Tier 2 Goods item not assigned!");
        }
    }
    
    /// <summary>
    /// Add a Tier 3 goods item to the first empty inventory slot
    /// </summary>
    [ContextMenu("Add Tier 3 Goods to Inventory")]
    private void AddTier3Goods()
    {
        if (tier3Goods != null)
        {
            AddGoodsToInventory(tier3Goods, "Tier 3");
        }
        else
        {
            Debug.LogWarning("[GoodsTestHelper] Tier 3 Goods item not assigned!");
        }
    }
    
    /// <summary>
    /// Add all three tiers of goods to inventory
    /// </summary>
    [ContextMenu("Add All Goods Tiers to Inventory")]
    private void AddAllGoodsTiers()
    {
        AddTier1Goods();
        AddTier2Goods();
        AddTier3Goods();
    }
    
    /// <summary>
    /// Helper method to add goods to inventory
    /// </summary>
    private void AddGoodsToInventory(GoodsItem goodsItem, string tierName)
    {
        InventoryManager inventoryManager = InventoryManager.Instance;
        if (inventoryManager != null)
        {
            bool added = inventoryManager.TryAddItem(goodsItem, 1);
            if (added)
            {
                Debug.Log($"[GoodsTestHelper] ✅ Successfully added {tierName} Goods to inventory! Double-click to open and get {goodsItem.minGems}-{goodsItem.maxGems} gems.");
            }
            else
            {
                Debug.LogWarning($"[GoodsTestHelper] ❌ Failed to add {tierName} Goods - inventory might be full!");
            }
        }
        else
        {
            Debug.LogError("[GoodsTestHelper] InventoryManager not found!");
        }
    }
    
    /// <summary>
    /// Show current gem count
    /// </summary>
    [ContextMenu("Show Current Gem Count")]
    private void ShowGemCount()
    {
        InventoryManager inventoryManager = InventoryManager.Instance;
        if (inventoryManager != null)
        {
            Debug.Log($"[GoodsTestHelper] 💎 Current gem count: {inventoryManager.currentGemCount}");
        }
        else
        {
            Debug.LogError("[GoodsTestHelper] InventoryManager not found!");
        }
    }
    
    /// <summary>
    /// Test the goods opening system status
    /// </summary>
    [ContextMenu("Test Goods System Status")]
    private void TestGoodsSystemStatus()
    {
        if (GoodsOpeningHandler.Instance != null)
        {
            Debug.Log("[GoodsTestHelper] ✅ GoodsOpeningHandler is active and ready!");
            
            // Count goods items in inventory
            int goodsCount = 0;
            UnifiedSlot[] allSlots = FindObjectsOfType<UnifiedSlot>();
            foreach (var slot in allSlots)
            {
                if (!slot.IsEmpty && slot.CurrentItem != null)
                {
                    bool isGoods = slot.CurrentItem.itemName.ToLower().Contains("goods") || 
                                  slot.CurrentItem.itemType.ToLower().Contains("goods");
                    if (isGoods)
                    {
                        goodsCount++;
                        Debug.Log($"[GoodsTestHelper] Found goods: {slot.CurrentItem.itemName} in slot {slot.name}");
                    }
                }
            }
            
            if (goodsCount > 0)
            {
                Debug.Log($"[GoodsTestHelper] 🎁 Found {goodsCount} goods items ready to open!");
            }
            else
            {
                Debug.Log("[GoodsTestHelper] No goods items found. Use 'Add Goods' context menu options to add some for testing.");
            }
        }
        else
        {
            Debug.LogError("[GoodsTestHelper] ❌ GoodsOpeningHandler not found! Make sure GoodsSystemInitializer is in the scene.");
        }
    }
}
