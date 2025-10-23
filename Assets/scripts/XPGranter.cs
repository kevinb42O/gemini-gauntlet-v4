using UnityEngine;

namespace GeminiGauntlet.Progression
{
    /// <summary>
    /// XPGranter component that can be attached to any object to grant XP when specific conditions are met.
    /// This provides a flexible way to reward player progression throughout the game.
    /// </summary>
    public class XPGranter : MonoBehaviour
    {
        [Header("XP Settings")]
        [SerializeField] private int xpAmount = 10;
        [SerializeField] private XPCategory xpCategory = XPCategory.Enemy;
        [SerializeField] private string customCategoryName = "";
        
        [Header("Grant Conditions")]
        [SerializeField] private bool grantOnDestroy = true;
        [SerializeField] private bool grantOnDisable = false;
        [SerializeField] private bool requiresPlayerAction = true;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        
        private bool hasGrantedXP = false;
        private bool wasSpawned = false;
        
        /// <summary>
        /// Categories for organizing different types of XP sources
        /// </summary>
        public enum XPCategory
        {
            Enemy,
            Tower, 
            Collectible,
            Boss,
            BossMinion,
            Achievement,
            Custom
        }
        
        void Start()
        {
            wasSpawned = true;
            DebugLog($"XPGranter initialized on '{gameObject.name}' - Amount: {xpAmount}, Category: {GetCategoryName()}");
        }
        
        void OnDisable()
        {
            if (grantOnDisable && !hasGrantedXP && wasSpawned)
            {
                GrantXP("OnDisable");
            }
        }
        
        void OnDestroy()
        {
            if (grantOnDestroy && !hasGrantedXP && wasSpawned)
            {
                GrantXP("OnDestroy");
            }
        }
        
        /// <summary>
        /// Manually grant XP (call this from external scripts when specific conditions are met)
        /// </summary>
        public void GrantXPManually(string reason = "Manual")
        {
            if (!hasGrantedXP)
            {
                GrantXP(reason);
            }
        }
        
        /// <summary>
        /// Force grant XP even if already granted (for special cases)
        /// </summary>
        public void ForceGrantXP(string reason = "Forced")
        {
            GrantXP(reason, true);
        }
        
        private void GrantXP(string reason, bool forceGrant = false)
        {
            if (hasGrantedXP && !forceGrant)
            {
                DebugLog($"XP already granted for '{gameObject.name}', skipping.");
                return;
            }
            
            if (XPManager.Instance == null)
            {
                Debug.LogWarning($"XPGranter on '{gameObject.name}': XPManager.Instance is null! Cannot grant XP.");
                return;
            }
            
            string categoryName = GetCategoryName();
            hasGrantedXP = true;
            
            DebugLog($"Granting {xpAmount} XP from '{gameObject.name}' (Category: {categoryName}, Reason: {reason})");
            
            // Grant XP through the manager
            switch (xpCategory.ToString().ToLower())
            {
                case "collectible":
                    XPManager.Instance.GrantXP(xpAmount, "Gems", gameObject.name);
                    break;
                case "enemy":
                    XPManager.Instance.GrantXP(xpAmount, "Enemies", gameObject.name);
                    break;
                case "tower":
                    XPManager.Instance.GrantXP(xpAmount, "Towers", gameObject.name);
                    break;
                case "boss":
                    XPManager.Instance.GrantXP(xpAmount, "Enemies", gameObject.name);
                    break;
                case "bossminion":
                    XPManager.Instance.GrantXP(xpAmount, "Enemies", gameObject.name);
                    break;
                case "achievement":
                    XPManager.Instance.GrantXP(xpAmount, "Achievements", gameObject.name);
                    break;
                default:
                    if (!string.IsNullOrEmpty(customCategoryName))
                    {
                        XPManager.Instance.GrantXP(xpAmount, customCategoryName, gameObject.name);
                    }
                    else
                    {
                        XPManager.Instance.GrantXP(xpAmount, "General", gameObject.name);
                    }
                    break;
            }
        }
        
        private string GetCategoryName()
        {
            if (xpCategory == XPCategory.Custom && !string.IsNullOrEmpty(customCategoryName))
            {
                return customCategoryName;
            }
            return xpCategory.ToString();
        }
        
        private void DebugLog(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[XPGranter] {message}");
            }
        }
        
        #region Editor Helpers
        
        /// <summary>
        /// Quick setup for enemy XP
        /// </summary>
        public void SetupForEnemy(int amount = 15)
        {
            xpAmount = amount;
            xpCategory = XPCategory.Enemy;
            grantOnDestroy = true;
            grantOnDisable = false;
            requiresPlayerAction = true;
        }
        
        /// <summary>
        /// Quick setup for tower XP
        /// </summary>
        public void SetupForTower(int amount = 50)
        {
            xpAmount = amount;
            xpCategory = XPCategory.Tower;
            grantOnDestroy = true;
            grantOnDisable = false;
            requiresPlayerAction = true;
        }
        
        /// <summary>
        /// Quick setup for collectible XP
        /// </summary>
        public void SetupForCollectible(int amount = 5)
        {
            xpAmount = amount;
            xpCategory = XPCategory.Collectible;
            grantOnDestroy = true;
            grantOnDisable = true;
            requiresPlayerAction = true;
        }
        
        #endregion
        
        #region Public Properties
        
        public int XPAmount => xpAmount;
        public XPCategory Category => xpCategory;
        public string CategoryName => GetCategoryName();
        public bool HasGrantedXP => hasGrantedXP;
        
        #endregion
    }
}