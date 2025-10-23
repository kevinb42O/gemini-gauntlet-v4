using UnityEngine;
using GeminiGauntlet.UI;

namespace GeminiGauntlet.Progression
{
    /// <summary>
    /// Clean XP tracking hooks - much simpler than XPGranter components!
    /// Call these methods from gameplay systems to grant XP automatically
    /// </summary>
    public static class XPHooks
    {
        /// <summary>
        /// Grant XP when player kills an enemy
        /// </summary>
        public static void OnEnemyKilled(string enemyType = "enemy")
        {
            Debug.Log($"[XPHooks] OnEnemyKilled called with enemyType: {enemyType}");
            
            if (XPManager.Instance == null)
            {
                Debug.LogError("[XPHooks] OnEnemyKilled: XPManager.Instance is NULL! Finding in scene...");
                var xpManager = UnityEngine.Object.FindObjectOfType<XPManager>();
                if (xpManager != null)
                {
                    Debug.Log($"[XPHooks] Found XPManager in scene: {xpManager.name}");
                }
                else
                {
                    Debug.LogError("[XPHooks] NO XPManager found in scene at all!");
                    return;
                }
            }
            
            // Get XP amount based on enemy type
            int xpAmount = GetXPForEnemyType(enemyType);
            string category = GetCategoryForEnemyType(enemyType);
            string sourceName = GetDisplayNameForEnemyType(enemyType);
            
            Debug.Log($"[XPHooks] OnEnemyKilled: {enemyType} → {xpAmount} XP, Category: {category}, Source: {sourceName}");
            XPManager.Instance.GrantXP(xpAmount, category, sourceName);
            Debug.Log($"[XPHooks] GrantXP call completed for {enemyType}");
        }
        
        /// <summary>
        /// Grant XP when player destroys a tower
        /// </summary>
        public static void OnTowerDestroyed()
        {
            Debug.Log("[XPHooks] OnTowerDestroyed called");
            
            if (XPManager.Instance == null)
            {
                Debug.LogError("[XPHooks] OnTowerDestroyed: XPManager.Instance is NULL!");
                return;
            }
            
            Debug.Log("[XPHooks] OnTowerDestroyed: Granting 50 XP for tower");
            XPManager.Instance.GrantXP(50, "Towers", "Tower Destroyed");
            Debug.Log("[XPHooks] OnTowerDestroyed: GrantXP call completed");
        }
        
        /// <summary>
        /// Grant XP when player collects a gem
        /// </summary>
        public static void OnGemCollected()
        {
            Debug.Log("[XPHooks] OnGemCollected called");
            
            if (XPManager.Instance == null)
            {
                Debug.LogError("[XPHooks] OnGemCollected: XPManager.Instance is NULL!");
                return;
            }
            
            Debug.Log("[XPHooks] OnGemCollected: Granting 10 XP for Gems");
            XPManager.Instance.GrantXP(10, "Gems", "Gem Collected");
            Debug.Log("[XPHooks] OnGemCollected: GrantXP call completed");
        }
        
        /// <summary>
        /// Grant XP when player opens/interacts with a chest
        /// </summary>
        public static void OnChestOpened()
        {
            Debug.Log("[XPHooks] OnChestOpened called");
            
            if (XPManager.Instance == null)
            {
                Debug.LogError("[XPHooks] OnChestOpened: XPManager.Instance is NULL!");
                return;
            }
            
            Debug.Log("[XPHooks] OnChestOpened: Granting 25 XP for chest");
            XPManager.Instance.GrantXP(25, "Chests", "Chest Opened");
            Debug.Log("[XPHooks] OnChestOpened: GrantXP call completed");
        }
        
        /// <summary>
        /// Grant XP when player conquers a platform (all towers destroyed)
        /// </summary>
        public static void OnPlatformConquered()
        {
            if (XPManager.Instance != null)
            {
                Debug.Log("[XPHooks] OnPlatformConquered: 100 XP");
                XPManager.Instance.GrantXP(100, "Platforms", "Platform Conquered");  // Fixed category name
            }
            else
            {
                Debug.LogError("[XPHooks] OnPlatformConquered: XPManager.Instance is NULL!");
            }
        }
        
        /// <summary>
        /// Grant XP for any custom source
        /// </summary>
        public static void OnCustomXP(int amount, string category, string sourceName)
        {
            if (XPManager.Instance != null)
            {
                XPManager.Instance.GrantXP(amount, category, sourceName);
            }
        }
        
        #region Floating Text Overloads
        
        /// <summary>
        /// Grant XP when player kills an enemy - WITH FLOATING TEXT
        /// </summary>
        public static void OnEnemyKilled(string enemyType, Vector3 position)
        {
            Debug.Log($"[XPHooks] OnEnemyKilled called with enemyType: {enemyType} at position: {position}");
            
            if (XPManager.Instance == null)
            {
                Debug.LogError("[XPHooks] OnEnemyKilled: XPManager.Instance is NULL! Finding in scene...");
                var xpManager = UnityEngine.Object.FindObjectOfType<XPManager>();
                if (xpManager != null)
                {
                    Debug.Log($"[XPHooks] Found XPManager in scene: {xpManager.name}");
                }
                else
                {
                    Debug.LogError("[XPHooks] NO XPManager found in scene at all!");
                    return;
                }
            }
            
            // Get XP amount based on enemy type
            int xpAmount = GetXPForEnemyType(enemyType);
            string category = GetCategoryForEnemyType(enemyType);
            string sourceName = GetDisplayNameForEnemyType(enemyType);
            
            Debug.Log($"[XPHooks] OnEnemyKilled: {enemyType} → {xpAmount} XP, Category: {category}, Source: {sourceName}");
            XPManager.Instance.GrantXP(xpAmount, category, sourceName);
            
            // Show floating text
            if (FloatingTextManager.Instance != null)
            {
                FloatingTextManager.Instance.ShowXPText(xpAmount, position);
                Debug.Log($"[XPHooks] Floating text shown for {xpAmount} XP at {position}");
            }
            else
            {
                Debug.LogWarning("[XPHooks] FloatingTextManager.Instance is null, cannot show floating text");
            }
            
            Debug.Log($"[XPHooks] GrantXP call completed for {enemyType}");
        }
        
        /// <summary>
        /// Grant XP when player destroys a tower - WITH FLOATING TEXT
        /// </summary>
        public static void OnTowerDestroyed(Vector3 position)
        {
            Debug.Log($"[XPHooks] OnTowerDestroyed called at position: {position}");
            
            if (XPManager.Instance == null)
            {
                Debug.LogError("[XPHooks] OnTowerDestroyed: XPManager.Instance is NULL!");
                return;
            }
            
            int xpAmount = 50;
            Debug.Log($"[XPHooks] OnTowerDestroyed: Granting {xpAmount} XP for tower");
            XPManager.Instance.GrantXP(xpAmount, "Towers", "Tower Destroyed");
            
            // Show floating text in ORANGE for towers
            if (FloatingTextManager.Instance != null)
            {
                FloatingTextManager.Instance.ShowFloatingText($"+{xpAmount} XP", position, Color.cyan); // Cyan for towers
                Debug.Log($"[XPHooks] Tower floating text shown for {xpAmount} XP at {position}");
            }
            else
            {
                Debug.LogWarning("[XPHooks] FloatingTextManager.Instance is null, cannot show floating text");
            }
            
            Debug.Log("[XPHooks] OnTowerDestroyed: GrantXP call completed");
        }
        
        /// <summary>
        /// Grant XP when player destroys a gem - WITH FLOATING TEXT
        /// </summary>
        public static void OnGemDestroyed(Vector3 position)
        {
            Debug.Log($"[XPHooks] OnGemDestroyed called at position: {position}");
            
            if (XPManager.Instance == null)
            {
                Debug.LogError("[XPHooks] OnGemDestroyed: XPManager.Instance is NULL!");
                return;
            }
            
            int xpAmount = 5; // Gems give less XP than skulls or towers
            Debug.Log($"[XPHooks] OnGemDestroyed: Granting {xpAmount} XP for gem");
            XPManager.Instance.GrantXP(xpAmount, "Gems", "Gem Destroyed");
            
            // Show floating text in MAGENTA for gems
            if (FloatingTextManager.Instance != null)
            {
                FloatingTextManager.Instance.ShowFloatingText($"+{xpAmount} XP", position, Color.magenta); // Magenta for gems
                Debug.Log($"[XPHooks] Gem floating text shown for {xpAmount} XP at {position}");
            }
            else
            {
                Debug.LogWarning("[XPHooks] FloatingTextManager.Instance is null, cannot show floating text");
            }
            
            Debug.Log("[XPHooks] OnGemDestroyed: GrantXP call completed");
        }
        
        #endregion
        
        #region Private Helpers
        
        static int GetXPForEnemyType(string enemyType)
        {
            switch (enemyType.ToLower())
            {
                case "skull": return 10;
                case "bossminion": return 25;
                case "boss": return 100;
                default: return 10;
            }
        }
        
        static string GetCategoryForEnemyType(string enemyType)
        {
            switch (enemyType.ToLower())
            {
                case "skull": return "Enemies";         // Updated to match UI categories
                case "bossminion": return "Enemies";    // Boss minions count as enemies for UI
                case "boss": return "Enemies";          // Bosses count as enemies for UI
                default: return "Enemies";
            }
        }
        
        static string GetDisplayNameForEnemyType(string enemyType)
        {
            switch (enemyType.ToLower())
            {
                case "skull": return "Skull Enemy Killed";
                case "bossminion": return "Boss Minion Killed";
                case "boss": return "Boss Defeated";
                default: return "Enemy Killed";
            }
        }
        
        #endregion
    }
}