using UnityEngine;
using System;

namespace GeminiGauntlet.Missions
{
    /// <summary>
    /// Mission types supported by the system
    /// </summary>
    public enum MissionType
    {
        Kill,           // Kill X enemies of specific type
        Conquer,        // Conquer X platforms
        Loot,           // Loot X chests
        Collect,        // Collect X items (gems, specific items)
        Craft           // Craft specific items using FORGE
    }

    /// <summary>
    /// Mission difficulty tiers - determines unlock progression
    /// </summary>
    public enum MissionTier
    {
        Tier1 = 1,
        Tier2 = 2,
        Tier3 = 3
    }

    /// <summary>
    /// ScriptableObject defining a mission with all its parameters
    /// Easy to create new missions in Unity Inspector
    /// </summary>
    [CreateAssetMenu(fileName = "New Mission", menuName = "Gemini Gauntlet/Mission")]
    public class Mission : ScriptableObject
    {
        [Header("Mission Identity")]
        [Tooltip("Unique identifier for this mission")]
        public string missionID;
        [Tooltip("Display name shown to players")]
        public string missionName;
        [Tooltip("Short description of the mission")]
        [TextArea(2, 4)]
        public string missionDescription;
        [Tooltip("Mission icon for UI display")]
        public Sprite missionIcon;
        
        [Header("Mission Configuration")]
        [Tooltip("Type of mission (Kill, Conquer, Loot, etc.)")]
        public MissionType missionType;
        [Tooltip("Which tier this mission belongs to")]
        public MissionTier tier = MissionTier.Tier1;
        
        [Header("Mission Requirements")]
        [Tooltip("Target count to complete (e.g., kill 5 skulls = 5)")]
        public int targetCount = 1;
        [Tooltip("Specific target for mission (enemy type, item name, etc.)")]
        public string targetSpecifier = "";
        
        [Header("Progress Persistence")]
        [Tooltip("If true, progress persists if player dies/fails to exfil")]
        public bool persistProgressOnDeath = true;
        
        [Header("Rewards")]
        [Tooltip("XP rewarded upon mission completion")]
        public int xpReward = 100;
        [Tooltip("Gems rewarded upon mission completion")]
        public int gemReward = 10;
        [Tooltip("Optional item rewards (can be empty)")]
        public ChestItemData[] itemRewards;
        
        [Header("Mission Text Templates")]
        [Tooltip("Progress text template (use {current} and {target} placeholders)")]
        public string progressTemplate = "{current}/{target}";
        [Tooltip("Completion text shown when mission is done")]
        public string completionText = "Mission Complete!";
        
        /// <summary>
        /// Get formatted progress text
        /// </summary>
        public string GetProgressText(int currentProgress)
        {
            return progressTemplate
                .Replace("{current}", currentProgress.ToString())
                .Replace("{target}", targetCount.ToString());
        }
        
        /// <summary>
        /// Check if mission is complete based on current progress
        /// </summary>
        public bool IsComplete(int currentProgress)
        {
            return currentProgress >= targetCount;
        }
        
        /// <summary>
        /// Get mission type display name for UI
        /// </summary>
        public string GetMissionTypeDisplayName()
        {
            switch (missionType)
            {
                case MissionType.Kill: return $"Kill {targetCount} {targetSpecifier}";
                case MissionType.Conquer: return $"Conquer {targetCount} Platform{(targetCount > 1 ? "s" : "")}";
                case MissionType.Loot: return $"Loot {targetCount} Chest{(targetCount > 1 ? "s" : "")}";
                case MissionType.Collect: return $"Collect {targetCount} {targetSpecifier}";
                case MissionType.Craft: return $"Craft {targetSpecifier}";
                default: return missionName;
            }
        }
        
        /// <summary>
        /// Validate mission configuration
        /// </summary>
        void OnValidate()
        {
            // Auto-generate mission ID if empty
            if (string.IsNullOrEmpty(missionID))
            {
                missionID = name.Replace(" ", "_").ToLower();
            }
            
            // Ensure target count is at least 1
            if (targetCount < 1)
                targetCount = 1;
                
            // Ensure rewards are non-negative
            if (xpReward < 0) xpReward = 0;
            if (gemReward < 0) gemReward = 0;
        }
    }
    
    /// <summary>
    /// Runtime mission progress data
    /// </summary>
    [System.Serializable]
    public class MissionProgress
    {
        public string missionID;
        public int currentProgress;
        public bool isCompleted;
        public bool isEquipped;
        public bool hasClaimedReward;
        public DateTime startTime;
        public DateTime completionTime;
        
        public MissionProgress()
        {
            startTime = DateTime.Now;
        }
        
        public MissionProgress(string id)
        {
            missionID = id;
            currentProgress = 0;
            isCompleted = false;
            isEquipped = false;
            hasClaimedReward = false;
            startTime = DateTime.Now;
        }
    }
    
    /// <summary>
    /// Save data for mission system
    /// </summary>
    [System.Serializable]
    public class MissionSaveData
    {
        public MissionProgress[] missionProgresses;
        public int currentUnlockedTier = 1;
        public string[] equippedMissionIDs = new string[3]; // Max 3 equipped missions
        
        public MissionSaveData()
        {
            missionProgresses = new MissionProgress[0];
            equippedMissionIDs = new string[3];
        }
    }
}