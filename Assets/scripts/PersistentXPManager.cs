using UnityEngine;
using System;

namespace GeminiGauntlet.Progression
{
    /// <summary>
    /// PersistentXPManager handles XP data persistence across all scenes.
    /// This is the single source of truth for player level and XP data.
    /// Both XPManager (gameplay) and MenuXPManager (UI) communicate with this.
    /// </summary>
    public class PersistentXPManager : MonoBehaviour
    {
        public static PersistentXPManager Instance { get; private set; }
        
        [Header("Persistent Level Data")]
        [SerializeField] private int currentLevel = 1;
        [SerializeField] private int currentXP = 0;
        [SerializeField] private int baseXPRequired = 100;
        [SerializeField] private float xpGrowthMultiplier = 1.5f;
        [SerializeField] private int maxLevel = 50;
        
        [Header("Session Tracking")]
        [SerializeField] private int sessionXP = 0; // XP gained this session (not yet processed by menu)
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        
        // Events for other managers to listen to
        public static event Action<int, int> OnLevelChanged; // oldLevel, newLevel
        public static event Action<int> OnXPChanged; // totalXP
        public static event Action<int> OnSessionXPChanged; // sessionXP
        
        // Properties
        public int CurrentLevel => currentLevel;
        public int CurrentXP => currentXP;
        public int SessionXP => sessionXP;
        public int XPRequiredForCurrentLevel => GetXPRequiredForLevel(currentLevel);
        public int XPRequiredForNextLevel => GetXPRequiredForLevel(currentLevel + 1);
        public bool IsMaxLevel => currentLevel >= maxLevel;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadPersistentData();
                DebugLog("PersistentXPManager: Singleton created and made persistent across scenes");
            }
            else
            {
                DebugLog("PersistentXPManager: Duplicate instance destroyed");
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// Add XP to the session total (called by XPManager during gameplay)
        /// </summary>
        public void AddSessionXP(int amount)
        {
            if (amount <= 0) return;
            
            sessionXP += amount;
            DebugLog($"Added {amount} session XP. Session total: {sessionXP}");
            OnSessionXPChanged?.Invoke(sessionXP);
            
            // Save session XP to PlayerPrefs for safety
            PlayerPrefs.SetInt("LastSessionXP", sessionXP);
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// Process session XP and add it to persistent totals (called by MenuXPManager)
        /// </summary>
        public int ProcessSessionXP()
        {
            if (sessionXP <= 0) return 0;
            
            int xpToProcess = sessionXP;
            int oldLevel = currentLevel;
            
            // Add session XP to persistent totals
            currentXP += sessionXP;
            
            // Check for level ups
            int newLevel = GetLevelFromXP(currentXP);
            if (newLevel > currentLevel)
            {
                currentLevel = newLevel;
                DebugLog($"LEVEL UP! {oldLevel} → {currentLevel}");
                OnLevelChanged?.Invoke(oldLevel, currentLevel);
            }
            
            // Clear session XP
            sessionXP = 0;
            
            // Save all data
            SavePersistentData();
            
            // Clear PlayerPrefs session XP
            PlayerPrefs.DeleteKey("LastSessionXP");
            PlayerPrefs.Save();
            
            DebugLog($"Processed {xpToProcess} session XP. New totals - Level: {currentLevel}, XP: {currentXP}");
            
            OnXPChanged?.Invoke(currentXP);
            OnSessionXPChanged?.Invoke(sessionXP);
            
            return xpToProcess;
        }
        
        /// <summary>
        /// Reset session XP without processing it (for new game start)
        /// </summary>
        public void ResetSessionXP()
        {
            sessionXP = 0;
            PlayerPrefs.DeleteKey("LastSessionXP");
            PlayerPrefs.Save();
            OnSessionXPChanged?.Invoke(sessionXP);
            DebugLog("Session XP reset");
        }
        
        /// <summary>
        /// Get current XP progress for UI display
        /// </summary>
        public XPProgressData GetXPProgressData()
        {
            var data = new XPProgressData();
            data.currentLevel = currentLevel;
            data.currentXP = currentXP;
            data.sessionXP = sessionXP;
            data.xpRequiredForCurrentLevel = XPRequiredForCurrentLevel;
            data.xpRequiredForNextLevel = XPRequiredForNextLevel;
            data.isMaxLevel = IsMaxLevel;
            
            if (!IsMaxLevel)
            {
                int xpInCurrentLevel = currentXP - data.xpRequiredForCurrentLevel;
                int xpNeededForLevel = data.xpRequiredForNextLevel - data.xpRequiredForCurrentLevel;
                data.progressPercent = (float)xpInCurrentLevel / xpNeededForLevel;
            }
            else
            {
                data.progressPercent = 1f;
            }
            
            return data;
        }
        
        private void LoadPersistentData()
        {
            currentLevel = PlayerPrefs.GetInt("PlayerLevel", 1);
            currentXP = PlayerPrefs.GetInt("PersistentXP", 0);
            sessionXP = PlayerPrefs.GetInt("LastSessionXP", 0);
            
            // Validate data consistency
            ValidateLevelData();
            
            DebugLog($"Loaded persistent data - Level: {currentLevel}, XP: {currentXP}, Session XP: {sessionXP}");
        }
        
        private void SavePersistentData()
        {
            // Safety validation
            if (currentLevel < 1) currentLevel = 1;
            if (currentXP < 0) currentXP = 0;
            
            PlayerPrefs.SetInt("PlayerLevel", currentLevel);
            PlayerPrefs.SetInt("PersistentXP", currentXP);
            PlayerPrefs.Save();
            
            DebugLog($"Saved persistent data - Level: {currentLevel}, XP: {currentXP}");
        }
        
        private void ValidateLevelData()
        {
            int correctLevel = GetLevelFromXP(currentXP);
            if (correctLevel != currentLevel)
            {
                DebugLog($"Level validation: XP suggests level {correctLevel} but saved level is {currentLevel}. Correcting...");
                currentLevel = correctLevel;
                SavePersistentData();
            }
        }
        
        private int GetXPRequiredForLevel(int level)
        {
            if (level <= 1) return 0;
            
            int totalXP = 0;
            for (int i = 2; i <= level; i++)
            {
                totalXP += Mathf.RoundToInt(baseXPRequired * Mathf.Pow(xpGrowthMultiplier, i - 2));
            }
            return totalXP;
        }
        
        private int GetLevelFromXP(int xp)
        {
            for (int level = 1; level <= maxLevel; level++)
            {
                if (xp < GetXPRequiredForLevel(level + 1))
                    return level;
            }
            return maxLevel;
        }
        
        private void DebugLog(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[PersistentXPManager] {message}");
            }
        }
        
        // Auto-save on application events
        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) SavePersistentData();
        }
        
        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) SavePersistentData();
        }
        
        /// <summary>
        /// Admin method to reset all XP data
        /// </summary>
        public void DEBUG_ResetAllXP()
        {
            currentLevel = 1;
            currentXP = 0;
            sessionXP = 0;
            SavePersistentData();
            PlayerPrefs.DeleteKey("LastSessionXP");
            PlayerPrefs.Save();
            
            OnLevelChanged?.Invoke(currentLevel, 1);
            OnXPChanged?.Invoke(currentXP);
            OnSessionXPChanged?.Invoke(sessionXP);
            
            DebugLog("All XP data reset to level 1");
        }
        
        /// <summary>
        /// Admin method to add XP directly
        /// </summary>
        public void DEBUG_AddXP(int amount)
        {
            AddSessionXP(amount);
        }
    }
    
    /// <summary>
    /// Data structure for XP progress information
    /// </summary>
    [System.Serializable]
    public class XPProgressData
    {
        public int currentLevel;
        public int currentXP;
        public int sessionXP;
        public int xpRequiredForCurrentLevel;
        public int xpRequiredForNextLevel;
        public float progressPercent;
        public bool isMaxLevel;
    }
}
