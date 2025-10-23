using UnityEngine;

/// <summary>
/// Initializes the goods opening system by creating the GoodsOpeningHandler singleton.
/// Add this script to any GameObject in your scene to enable goods opening functionality.
/// </summary>
public class GoodsSystemInitializer : MonoBehaviour
{
    [Header("Audio Clips (Optional)")]
    [Tooltip("Sound to play when goods are opened")]
    public AudioClip openingSound;
    [Tooltip("Sound to play when gems are awarded")]
    public AudioClip gemRewardSound;
    
    [Header("Visual Effects (Optional)")]
    [Tooltip("Particle effect to play when goods are opened")]
    public GameObject openingParticleEffect;
    
    void Awake()
    {
        // Check if GoodsOpeningHandler already exists
        if (GoodsOpeningHandler.Instance == null)
        {
            // Create a new GameObject for the GoodsOpeningHandler
            GameObject handlerObject = new GameObject("GoodsOpeningHandler");
            GoodsOpeningHandler handler = handlerObject.AddComponent<GoodsOpeningHandler>();
            
            // Set up audio source
            AudioSource audioSource = handlerObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = 0.7f;
            
            // Apply settings from this initializer (fields are public)
            if (openingSound != null)
            {
                handler.openingSound = openingSound;
            }
            
            if (gemRewardSound != null)
            {
                handler.gemRewardSound = gemRewardSound;
            }
            
            if (openingParticleEffect != null)
            {
                handler.openingParticleEffect = openingParticleEffect;
            }
            
            Debug.Log("[GoodsSystemInitializer] ✅ GoodsOpeningHandler created and configured!");
        }
        else
        {
            Debug.Log("[GoodsSystemInitializer] GoodsOpeningHandler already exists - skipping creation");
        }
    }
    
    /// <summary>
    /// Context menu method to test the goods opening system
    /// </summary>
    [ContextMenu("Test Goods Opening System")]
    private void TestGoodsSystem()
    {
        if (GoodsOpeningHandler.Instance != null)
        {
            Debug.Log("[GoodsSystemInitializer] ✅ GoodsOpeningHandler is active and ready!");
            
            // Try to find a goods item to test with
            UnifiedSlot[] allSlots = FindObjectsOfType<UnifiedSlot>();
            foreach (var slot in allSlots)
            {
                if (!slot.IsEmpty && slot.CurrentItem != null)
                {
                    bool isGoods = slot.CurrentItem.itemName.ToLower().Contains("goods") || 
                                  slot.CurrentItem.itemType.ToLower().Contains("goods");
                    if (isGoods)
                    {
                        Debug.Log($"[GoodsSystemInitializer] Found goods item for testing: {slot.CurrentItem.itemName}");
                        return;
                    }
                }
            }
            
            Debug.Log("[GoodsSystemInitializer] No goods items found in inventory for testing");
        }
        else
        {
            Debug.LogError("[GoodsSystemInitializer] ❌ GoodsOpeningHandler not found!");
        }
    }
}
