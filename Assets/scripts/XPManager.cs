using UnityEngine;
using System;
using System.Collections.Generic;

namespace GeminiGauntlet.Progression
{
    /// <summary>
    /// XPManager handles XP collection during gameplay only (no leveling).
    /// Leveling happens in the menu after exfil via MenuXPManager.
    /// </summary>
    public class XPManager : MonoBehaviour
    {
        public static XPManager Instance { get; private set; }
        
        [Header("XP Collection (Gameplay Only)")]
        [SerializeField] private int sessionTotalXP = 0;
        
        [Header("XP Sources Tracking")]
        [SerializeField] private Dictionary<string, int> xpByCategory = new Dictionary<string, int>();
        [SerializeField] private Dictionary<string, int> countByCategory = new Dictionary<string, int>();
        
        [Header("XP Values")]
        [SerializeField] private int gemXPValue = 10;
        [SerializeField] private int enemyXPValue = 10;
        [SerializeField] private int towerXPValue = 50;
        [SerializeField] private int bossXPValue = 100;
        [SerializeField] private int bossMinionXPValue = 25;
        
        [Header("Audio")]
        [SerializeField] private AudioClip xpGainSound;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool showXPGainMessages = false; // Keep quiet during gameplay
        
        // Events for tracking
        public static event Action<string, int, int> OnCategoryXPGained; // category, amount, count
        
        // Properties for XP summary
        public int SessionTotalXP => sessionTotalXP;
        public Dictionary<string, int> GetXPByCategory() => new Dictionary<string, int>(xpByCategory);
        public Dictionary<string, int> GetCountByCategory() => new Dictionary<string, int>(countByCategory);
        
        void Awake()
        {
            Debug.Log("[XPManager] Awake called");
            
            if (Instance == null)
            {
                Instance = this;
                // REMOVED: DontDestroyOnLoad(gameObject) - XPManager should NOT persist across scenes
                // This was causing conflicts with MenuXPManager when returning from game to menu
                Debug.Log($"[XPManager] Instance set to: {gameObject.name} (scene-specific, not persistent)");
                DebugLog("XPManager: Singleton instance created for current scene only");
            }
            else
            {
                Debug.Log($"[XPManager] Duplicate instance found, destroying: {gameObject.name}");
                DebugLog("XPManager: Duplicate instance destroyed");
                Destroy(gameObject);
            }
        }
        
        void Start()
        {
            // Initialize XP tracking dictionaries
            if (xpByCategory == null) xpByCategory = new Dictionary<string, int>();
            if (countByCategory == null) countByCategory = new Dictionary<string, int>();
            
            // Reset session XP to 0 when starting new game (as per user's flow)
            ResetSession();
            
            // Reset PersistentXPManager session XP for new game
            if (PersistentXPManager.Instance != null)
            {
                PersistentXPManager.Instance.ResetSessionXP();
            }
            
            DebugLog("XPManager: Started - session XP reset to 0, ready to collect XP during gameplay");
        }
        
        /// <summary>
        /// Grant XP from any source (collection only, no leveling)
        /// </summary>
        public void GrantXP(int amount, string category = "General", string sourceName = "Unknown")
        {
            if (amount <= 0) return;
            
            // Track session XP
            sessionTotalXP += amount;
            
            // Track by category
            if (!xpByCategory.ContainsKey(category)) xpByCategory[category] = 0;
            if (!countByCategory.ContainsKey(category)) countByCategory[category] = 0;
            
            xpByCategory[category] += amount;
            countByCategory[category]++;
            
            Debug.Log($"[XPManager] XP GRANTED: {amount} XP for '{category}' from '{sourceName}'. Session Total: {sessionTotalXP}");
            DebugLog($"Collected {amount} XP from {sourceName} ({category}) - Session Total: {sessionTotalXP}");
            
            // Send XP to PersistentXPManager for cross-scene tracking
            if (PersistentXPManager.Instance != null)
            {
                PersistentXPManager.Instance.AddSessionXP(amount);
            }
            
            // Show minimal XP gain message during gameplay (optional)
            if (showXPGainMessages && DynamicPlayerFeedManager.Instance != null)
            {
                string message = $"+{amount} XP";
                DynamicPlayerFeedManager.Instance.ShowCustomMessage(message, Color.yellow, null, false, 1f);
            }
            
            // Play subtle XP collection sound
            if (xpGainSound != null)
            {
                AudioManager.Instance?.PlaySound3DAtPoint(xpGainSound, transform.position, 0.2f);
            }
            
            // Fire event for tracking
            OnCategoryXPGained?.Invoke(category, amount, countByCategory[category]);
        }
        
        /// <summary>
        /// Get XP data for the summary UI
        /// </summary>
        public XPSummaryData GetXPSummaryData()
        {
            Debug.Log($" [XPManager] GetXPSummaryData() called - sessionTotalXP: {sessionTotalXP}");
            Debug.Log($" [XPManager] xpByCategory.Count: {xpByCategory.Count}");
            Debug.Log($" [XPManager] countByCategory.Count: {countByCategory.Count}");
            
            // Log all categories and their values
            foreach (var kvp in xpByCategory)
            {
                int count = countByCategory.ContainsKey(kvp.Key) ? countByCategory[kvp.Key] : 0;
                Debug.Log($" [XPManager] Category '{kvp.Key}': {kvp.Value} XP, {count} count");
            }
            
            var summaryData = new XPSummaryData();
            summaryData.categoryBreakdown = new List<XPCategoryData>();
            
            // Add regular gameplay XP categories
            foreach (var category in xpByCategory.Keys)
            {
                var categoryData = new XPCategoryData
                {
                    categoryName = category,
                    count = countByCategory.ContainsKey(category) ? countByCategory[category] : 0,
                    xpPerItem = GetXPValueForCategory(category),
                    totalXP = xpByCategory[category]
                };
                
                Debug.Log($" [XPManager] Adding category data: {categoryData.categoryName} - {categoryData.totalXP} XP, {categoryData.count} count");
                Debug.Log($" [XPManager] Category '{category}' in xpByCategory: {xpByCategory[category]} XP");
                Debug.Log($" [XPManager] Category '{category}' in countByCategory: {(countByCategory.ContainsKey(category) ? countByCategory[category] : 0)} count");
                summaryData.categoryBreakdown.Add(categoryData);
            }
            
            // Add mission completion XP if mission system is available
            if (GeminiGauntlet.Missions.MissionManager.Instance != null)
            {
                int missionXP = GeminiGauntlet.Missions.MissionManager.Instance.GetSessionMissionXP();
                if (missionXP > 0)
                {
                    var missionCategoryData = new XPCategoryData
                    {
                        categoryName = "Missions",
                        count = 1, // Number of missions completed this session
                        xpPerItem = missionXP,
                        totalXP = missionXP
                    };
                    
                    summaryData.categoryBreakdown.Add(missionCategoryData);
                }
            }
            
            // Calculate total session XP (regular XP + mission XP)
            summaryData.sessionTotalXP = sessionTotalXP;
            if (GeminiGauntlet.Missions.MissionManager.Instance != null)
            {
                summaryData.sessionTotalXP += GeminiGauntlet.Missions.MissionManager.Instance.GetSessionMissionXP();
            }
            
            return summaryData;
        }
        
        private int GetXPValueForCategory(string category)
        {
            switch (category.ToLower())
            {
                case "collectible": return gemXPValue; // Legacy support
                case "gems": return gemXPValue;        // New category name
                case "enemy": return enemyXPValue;     // Legacy support
                case "enemies": return enemyXPValue;   // New category name
                case "tower": return towerXPValue;     // Legacy support
                case "towers": return towerXPValue;    // New category name
                case "boss": return bossXPValue;       // Legacy support (now counts as enemy)
                case "bossminion": return bossMinionXPValue; // Legacy support (now counts as enemy)
                case "platform": return 100;          // Legacy support
                case "platforms": return 100;         // New category name
                case "chest": return 25;              // Legacy support
                case "chests": return 25;             // New category name
                case "missions": return 100;          // Mission completion XP
                default: return 10;
            }
        }
        
        /// <summary>
        /// Reset session data (called when starting new game)
        /// </summary>
        public void ResetSession()
        {
            sessionTotalXP = 0;
            xpByCategory.Clear();
            countByCategory.Clear();
            DebugLog("XPManager: Session reset - ready for new game");
        }
        
        void OnDestroy()
        {
            // Clear the static instance when this object is destroyed
            // This ensures MenuXPManager can work properly when returning to menu
            if (Instance == this)
            {
                Instance = null;
                Debug.Log("[XPManager] Instance cleared on destroy - MenuXPManager can now take over");
            }
        }
        
        private void DebugLog(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[XPManager] {message}");
            }
        }
        
        #region Public API
        
        /// <summary>
        /// Admin method to grant XP directly (for testing)
        /// </summary>
        public void DEBUG_GrantXP(int amount, string category = "Debug")
        {
            GrantXP(amount, category, "Admin Command");
        }
        
        #endregion
    }
    
    /// <summary>
    /// Data structure for XP summary
    /// </summary>
    [System.Serializable]
    public class XPSummaryData
    {
        public int sessionTotalXP;
        public List<XPCategoryData> categoryBreakdown;
    }
    
    [System.Serializable]
    public class XPCategoryData
    {
        public string categoryName;
        public int count;
        public int xpPerItem;
        public int totalXP;
        
        public string GetDisplayName()
        {
            switch (categoryName.ToLower())
            {
                case "collectible": return "Gems Collected"; // Legacy support
                case "gems": return "Gems Collected";        // New category name
                case "enemy": return "Enemies Killed";      // Legacy support
                case "enemies": return "Enemies Killed";    // New category name
                case "tower": return "Towers Destroyed";    // Legacy support
                case "towers": return "Towers Destroyed";   // New category name
                case "boss": return "Enemies Killed";       // Legacy - now counts as enemies
                case "bossminion": return "Enemies Killed"; // Legacy - now counts as enemies
                case "platform": return "Platforms Conquered"; // Legacy support
                case "platforms": return "Platforms Conquered"; // New category name
                case "chest": return "Chests Opened";       // Legacy support
                case "chests": return "Chests Opened";      // New category name
                case "missions": return "Missions Completed";
                default: return categoryName;
            }
        }
    }
}