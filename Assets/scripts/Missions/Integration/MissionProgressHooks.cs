using UnityEngine;

namespace GeminiGauntlet.Missions.Integration
{
    /// <summary>
    /// Static utility class for mission progress hooks
    /// Call these methods from existing gameplay systems to update mission progress
    /// </summary>
    public static class MissionProgressHooks
    {
        /// <summary>
        /// Call when player kills any enemy
        /// </summary>
        public static void OnEnemyKilled(string enemyType = "")
        {
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.OnEnemyKilled(enemyType);
            }
        }
        
        /// <summary>
        /// Call when player conquers a platform
        /// </summary>
        public static void OnPlatformConquered()
        {
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.OnPlatformConquered();
            }
        }
        
        /// <summary>
        /// Call when player loots a chest
        /// </summary>
        public static void OnChestLooted()
        {
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.OnChestLooted();
            }
        }
        
        /// <summary>
        /// Call when player collects an item (gems, collectibles, etc.)
        /// </summary>
        public static void OnItemCollected(string itemName = "")
        {
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.OnItemCollected(itemName);
            }
        }
        
        /// <summary>
        /// Call when player crafts an item via FORGE
        /// </summary>
        public static void OnItemCrafted(string itemName = "")
        {
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.OnItemCrafted(itemName);
            }
        }
        
        /// <summary>
        /// Call when player dies/fails (resets non-persistent mission progress)
        /// </summary>
        public static void OnPlayerDeath()
        {
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.OnPlayerDeath();
            }
        }
    }
    
    /// <summary>
    /// Component that can be added to enemy objects to auto-report kills
    /// </summary>
    public class MissionEnemyTracker : MonoBehaviour
    {
        [Header("Enemy Configuration")]
        [Tooltip("Type of enemy for mission tracking (e.g., 'skull', 'goblin', 'boss')")]
        public string enemyType = "enemy";
        
        [Header("Auto-tracking")]
        [Tooltip("Automatically track when this enemy is destroyed")]
        public bool trackOnDestroy = true;
        
        void OnDestroy()
        {
            if (trackOnDestroy)
            {
                MissionProgressHooks.OnEnemyKilled(enemyType);
            }
        }
        
        /// <summary>
        /// Manually trigger enemy kill tracking
        /// </summary>
        public void TrackEnemyKill()
        {
            MissionProgressHooks.OnEnemyKilled(enemyType);
        }
    }
    
    /// <summary>
    /// Component that can be added to chest objects to auto-report looting
    /// </summary>
    public class MissionChestTracker : MonoBehaviour
    {
        [Header("Chest Configuration")]
        [Tooltip("Automatically track when this chest is opened/looted")]
        public bool trackOnLoot = true;
        
        private bool hasBeenLooted = false;
        
        /// <summary>
        /// Call this when the chest is looted
        /// </summary>
        public void OnChestLooted()
        {
            if (trackOnLoot && !hasBeenLooted)
            {
                hasBeenLooted = true;
                MissionProgressHooks.OnChestLooted();
                Debug.Log("[MissionChestTracker] Chest looted - mission progress updated");
            }
        }
        
        /// <summary>
        /// Reset loot state (for testing)
        /// </summary>
        public void ResetLootState()
        {
            hasBeenLooted = false;
        }
    }
    
    /// <summary>
    /// Component that can be added to platform objects to auto-report conquering
    /// </summary>
    public class MissionPlatformTracker : MonoBehaviour
    {
        [Header("Platform Configuration")]
        [Tooltip("Automatically track when this platform is conquered")]
        public bool trackOnConquer = true;
        
        private bool hasBeenConquered = false;
        
        /// <summary>
        /// Call this when the platform is conquered
        /// </summary>
        public void OnPlatformConquered()
        {
            if (trackOnConquer && !hasBeenConquered)
            {
                hasBeenConquered = true;
                MissionProgressHooks.OnPlatformConquered();
                Debug.Log("[MissionPlatformTracker] Platform conquered - mission progress updated");
            }
        }
        
        /// <summary>
        /// Reset conquest state (for testing)
        /// </summary>
        public void ResetConquestState()
        {
            hasBeenConquered = false;
        }
    }
    
    /// <summary>
    /// Component that can be added to collectible items to auto-report collection
    /// </summary>
    public class MissionCollectibleTracker : MonoBehaviour
    {
        [Header("Collectible Configuration")]
        [Tooltip("Name/type of collectible for mission tracking")]
        public string collectibleType = "gem";
        [Tooltip("Automatically track when this collectible is collected")]
        public bool trackOnCollect = true;
        
        /// <summary>
        /// Call this when the collectible is picked up
        /// </summary>
        public void OnCollectibleCollected()
        {
            if (trackOnCollect)
            {
                MissionProgressHooks.OnItemCollected(collectibleType);
                Debug.Log($"[MissionCollectibleTracker] {collectibleType} collected - mission progress updated");
            }
        }
        
        /// <summary>
        /// Trigger collection via collision (optional)
        /// </summary>
        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                OnCollectibleCollected();
            }
        }
    }
    
    /// <summary>
    /// Integration component for FORGE system to track crafting
    /// Add this to ForgeManager or create a separate script that monitors FORGE output
    /// </summary>
    public class MissionForgeIntegration : MonoBehaviour
    {
        [Header("FORGE Integration")]
        [Tooltip("Reference to the FORGE output slot to monitor")]
        public UnifiedSlot forgeOutputSlot;
        [Tooltip("Check output slot every frame for new items")]
        public bool monitorForgeOutput = true;
        
        private ChestItemData lastOutputItem;
        private int lastOutputCount;
        
        void Start()
        {
            // Try to find FORGE output slot if not assigned
            if (forgeOutputSlot == null && ForgeManager.Instance != null)
            {
                // Access the output slot from ForgeManager
                forgeOutputSlot = ForgeManager.Instance.outputSlot;
                if (forgeOutputSlot != null)
                {
                    Debug.Log("[MissionForgeIntegration] Successfully connected to FORGE output slot");
                }
                else
                {
                    Debug.LogWarning("[MissionForgeIntegration] FORGE output slot is null - crafting missions won't track automatically");
                }
            }
        }
        
        void Update()
        {
            if (monitorForgeOutput && forgeOutputSlot != null)
            {
                CheckForgeOutput();
            }
        }
        
        /// <summary>
        /// Check if a new item has been crafted in the FORGE
        /// </summary>
        void CheckForgeOutput()
        {
            if (forgeOutputSlot.IsEmpty)
            {
                // Reset tracking when slot is empty
                lastOutputItem = null;
                lastOutputCount = 0;
                return;
            }
            
            var currentItem = forgeOutputSlot.CurrentItem;
            var currentCount = forgeOutputSlot.ItemCount;
            
            // Check if this is a new item (different from last check)
            if (currentItem != lastOutputItem || currentCount > lastOutputCount)
            {
                // Item was crafted!
                if (currentItem != null)
                {
                    MissionProgressHooks.OnItemCrafted(currentItem.itemName);
                    Debug.Log($"[MissionForgeIntegration] Item crafted: {currentItem.itemName} - mission progress updated");
                }
                
                lastOutputItem = currentItem;
                lastOutputCount = currentCount;
            }
        }
        
        /// <summary>
        /// Manually trigger crafting tracking for specific item
        /// </summary>
        public void OnItemCrafted(string itemName)
        {
            MissionProgressHooks.OnItemCrafted(itemName);
        }
    }
}