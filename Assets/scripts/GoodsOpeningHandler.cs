using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Handles the opening of goods items when double-clicked.
/// Manages visual effects, gem rewards, and item destruction.
/// Works in both game and menu contexts.
/// </summary>
public class GoodsOpeningHandler : MonoBehaviour
{
    public static GoodsOpeningHandler Instance { get; private set; }
    
    [Header("Audio")]
    [Tooltip("Sound to play when goods are opened")]
    public AudioClip openingSound;
    [Tooltip("Sound to play when gems are awarded")]
    public AudioClip gemRewardSound;
    
    [Header("Visual Effects")]
    [Tooltip("Particle effect to play when goods are opened (optional)")]
    public GameObject openingParticleEffect;
    
    private AudioSource _audioSource;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Get or create audio source
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Attempt to open a goods item from the specified slot
    /// </summary>
    public bool TryOpenGoods(UnifiedSlot slot)
    {
        if (slot == null || slot.IsEmpty)
        {
            Debug.Log("[GoodsOpeningHandler] Cannot open goods - slot is null or empty");
            return false;
        }
        
        // Check if the item is a goods item
        GoodsItem goodsItem = slot.CurrentItem as GoodsItem;
        if (goodsItem == null)
        {
            // Try to check if it's a regular ChestItemData with goods in the name
            if (slot.CurrentItem != null && IsGoodsItem(slot.CurrentItem))
            {
                // Handle legacy goods items
                return OpenLegacyGoods(slot, slot.CurrentItem);
            }
            
            Debug.Log($"[GoodsOpeningHandler] Item '{slot.CurrentItem?.itemName}' is not a goods item");
            return false;
        }
        
        return OpenGoods(slot, goodsItem);
    }
    
    /// <summary>
    /// Check if a ChestItemData is a goods item (for legacy support)
    /// </summary>
    private bool IsGoodsItem(ChestItemData item)
    {
        return item.itemName.ToLower().Contains("goods") || item.itemType.ToLower().Contains("goods");
    }
    
    /// <summary>
    /// Open a legacy goods item (ChestItemData with "goods" in name)
    /// </summary>
    private bool OpenLegacyGoods(UnifiedSlot slot, ChestItemData item)
    {
        Debug.Log($"[GoodsOpeningHandler] Opening legacy goods: {item.itemName}");
        
        // Calculate gem reward based on item name or rarity
        int gemReward = CalculateLegacyGemReward(item);
        
        // Award gems
        AwardGems(gemReward);
        
        // Play effects
        PlayOpeningEffects(slot.transform.position);
        
        // Start fade out and destruction
        StartCoroutine(FadeOutAndDestroy(slot, item.itemIcon, 2.0f));
        
        return true;
    }
    
    /// <summary>
    /// Calculate gem reward for legacy goods items
    /// </summary>
    private int CalculateLegacyGemReward(ChestItemData item)
    {
        // Default Tier 1 goods reward
        int minGems = 5;
        int maxGems = 15;
        
        // Try to determine tier from name
        string itemName = item.itemName.ToLower();
        if (itemName.Contains("tier 2") || itemName.Contains("t2"))
        {
            minGems = 15;
            maxGems = 30;
        }
        else if (itemName.Contains("tier 3") || itemName.Contains("t3"))
        {
            minGems = 30;
            maxGems = 50;
        }
        else if (item.itemRarity >= 3)
        {
            // Use rarity as fallback
            minGems = item.itemRarity * 10;
            maxGems = item.itemRarity * 20;
        }
        
        return Random.Range(minGems, maxGems + 1);
    }
    
    /// <summary>
    /// Open a proper GoodsItem
    /// </summary>
    private bool OpenGoods(UnifiedSlot slot, GoodsItem goodsItem)
    {
        Debug.Log($"[GoodsOpeningHandler] Opening {goodsItem.GetTierDisplayName()}");
        
        // Calculate gem reward
        int gemReward = goodsItem.CalculateGemReward();
        
        // Award gems
        AwardGems(gemReward);
        
        // Play effects
        PlayOpeningEffects(slot.transform.position);
        
        // Use opened sprite if available
        Sprite fadeSprite = goodsItem.openedSprite != null ? goodsItem.openedSprite : goodsItem.itemIcon;
        
        // Start fade out and destruction
        StartCoroutine(FadeOutAndDestroy(slot, fadeSprite, goodsItem.fadeOutDuration));
        
        return true;
    }
    
    /// <summary>
    /// Award gems to the player's inventory
    /// </summary>
    private void AwardGems(int gemCount)
    {
        Debug.Log($"[GoodsOpeningHandler] Awarding {gemCount} gems");
        
        // Find the inventory manager and add gems
        InventoryManager inventoryManager = InventoryManager.Instance;
        if (inventoryManager != null)
        {
            inventoryManager.AddGems(gemCount);
            Debug.Log($"[GoodsOpeningHandler] Successfully added {gemCount} gems to inventory");
        }
        else
        {
            Debug.LogWarning("[GoodsOpeningHandler] Could not find InventoryManager to award gems!");
        }
        
        // Show feedback message
        ShowGemRewardFeedback(gemCount);
    }
    
    /// <summary>
    /// Show feedback message for gem reward
    /// </summary>
    private void ShowGemRewardFeedback(int gemCount)
    {
        // Simple debug log feedback - can be enhanced later with proper UI
        Debug.Log($"[GoodsOpeningHandler] 🎁 OPENED GOODS! +{gemCount} gems awarded!");
    }
    
    /// <summary>
    /// Play opening sound and visual effects
    /// </summary>
    private void PlayOpeningEffects(Vector3 position)
    {
        // Play opening sound
        if (openingSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(openingSound);
        }
        
        // Play gem reward sound after a short delay
        if (gemRewardSound != null && _audioSource != null)
        {
            StartCoroutine(PlayDelayedSound(gemRewardSound, 0.3f));
        }
        
        // Spawn particle effect
        if (openingParticleEffect != null)
        {
            GameObject effect = Instantiate(openingParticleEffect, position, Quaternion.identity);
            Destroy(effect, 3.0f); // Clean up after 3 seconds
        }
    }
    
    /// <summary>
    /// Play a sound after a delay
    /// </summary>
    private IEnumerator PlayDelayedSound(AudioClip clip, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (_audioSource != null && clip != null)
        {
            _audioSource.PlayOneShot(clip);
        }
    }
    
    /// <summary>
    /// Fade out the item icon and destroy the item
    /// </summary>
    private IEnumerator FadeOutAndDestroy(UnifiedSlot slot, Sprite openedSprite, float duration)
    {
        // Change to opened sprite if available
        if (openedSprite != null && slot.itemIcon != null)
        {
            slot.itemIcon.sprite = openedSprite;
        }
        
        // Fade out the icon
        Image iconImage = slot.itemIcon;
        if (iconImage != null)
        {
            Color originalColor = iconImage.color;
            float elapsedTime = 0f;
            
            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime; // Use unscaled time to work during pause
                float alpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);
                iconImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                yield return null;
            }
            
            // Ensure fully transparent
            iconImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        }
        
        // Clear the slot
        slot.ClearSlot();
        
        Debug.Log("[GoodsOpeningHandler] Goods item opened and destroyed");
    }
    
    /// <summary>
    /// Context menu method for testing goods opening
    /// </summary>
    [ContextMenu("Test Open Goods")]
    private void TestOpenGoods()
    {
        Debug.Log("[GoodsOpeningHandler] Testing goods opening system...");
        
        // Find a slot with goods
        UnifiedSlot[] allSlots = FindObjectsOfType<UnifiedSlot>();
        foreach (var slot in allSlots)
        {
            if (!slot.IsEmpty && IsGoodsItem(slot.CurrentItem))
            {
                Debug.Log($"[GoodsOpeningHandler] Found goods in slot: {slot.CurrentItem.itemName}");
                TryOpenGoods(slot);
                return;
            }
        }
        
        Debug.Log("[GoodsOpeningHandler] No goods items found in any slots");
    }
}
