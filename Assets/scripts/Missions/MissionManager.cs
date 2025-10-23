using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;
using GeminiGauntlet.Progression;

namespace GeminiGauntlet.Missions
{
    /// <summary>
    /// Singleton Mission Manager - handles mission progress tracking across scenes
    /// Integrates with existing XP and inventory systems
    /// </summary>
    public class MissionManager : MonoBehaviour
    {
        public static MissionManager Instance { get; private set; }
        
        [Header("Mission Configuration")]
        [Tooltip("All available missions in the game")]
        public Mission[] allMissions;
        [Tooltip("Maximum number of missions that can be equipped at once")]
        public int maxEquippedMissions = 3;
        
        [Header("Mission Completion")]
        [Tooltip("Audio played when mission progress updates")]
        public AudioClip missionProgressSound;
        [Tooltip("Audio played when mission completes")]
        public AudioClip missionCompleteSound;
        [Tooltip("Volume for mission audio")]
        public float audioVolume = 0.7f;
        
        [Header("Debug")]
        [Tooltip("Enable debug logging for mission system")]
        public bool enableDebugLogs = true;
        
        // Runtime data
        private MissionSaveData saveData;
        private Dictionary<string, MissionProgress> missionProgressLookup;
        private Dictionary<string, Mission> missionLookup;
        private List<int> sessionCompletedMissions; // For XP summary integration
        private string savePath;
        
        // Events
        public static event Action<Mission, int> OnMissionProgressUpdated; // mission, newProgress
        public static event Action<Mission> OnMissionCompleted;
        public static event Action<Mission> OnMissionEquipped;
        public static event Action<Mission> OnMissionUnequipped;
        public static event Action<int> OnTierUnlocked; // newTier
        
        void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeMissionSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// Initialize the mission system
        /// </summary>
        void InitializeMissionSystem()
        {
            // Setup save path
            savePath = Path.Combine(Application.persistentDataPath, "mission_save.json");
            
            // Initialize collections
            missionProgressLookup = new Dictionary<string, MissionProgress>();
            missionLookup = new Dictionary<string, Mission>();
            sessionCompletedMissions = new List<int>();
            
            // Build mission lookup
            foreach (var mission in allMissions)
            {
                if (mission != null && !string.IsNullOrEmpty(mission.missionID))
                {
                    missionLookup[mission.missionID] = mission;
                }
            }
            
            // Load save data
            LoadMissionData();
            
            DebugLog("MissionManager initialized successfully");
        }
        
        #region Mission Progress Tracking
        
        /// <summary>
        /// Update mission progress for a specific type and target
        /// </summary>
        public void UpdateMissionProgress(MissionType type, string target = "", int amount = 1)
        {
            // Find all equipped missions that match this progress type
            var equippedMissions = GetEquippedMissions();
            
            foreach (var mission in equippedMissions)
            {
                if (mission.missionType == type)
                {
                    // Check if target matches (if specified)
                    bool targetMatches = string.IsNullOrEmpty(mission.targetSpecifier) || 
                                       string.IsNullOrEmpty(target) || 
                                       mission.targetSpecifier.Equals(target, StringComparison.OrdinalIgnoreCase);
                    
                    if (targetMatches)
                    {
                        var progress = GetMissionProgress(mission.missionID);
                        if (!progress.isCompleted)
                        {
                            progress.currentProgress += amount;
                            
                            // Clamp to target count
                            progress.currentProgress = Mathf.Min(progress.currentProgress, mission.targetCount);
                            
                            DebugLog($"Mission progress updated: {mission.missionName} - {progress.currentProgress}/{mission.targetCount}");
                            
                            // Check for completion
                            if (mission.IsComplete(progress.currentProgress))
                            {
                                CompleteMission(mission);
                            }
                            else
                            {
                                // Play progress sound
                                PlayMissionAudio(missionProgressSound);
                                OnMissionProgressUpdated?.Invoke(mission, progress.currentProgress);
                            }
                        }
                    }
                }
            }
            
            // Auto-save after progress update
            SaveMissionData();
        }
        
        /// <summary>
        /// Complete a mission and handle rewards
        /// </summary>
        void CompleteMission(Mission mission)
        {
            var progress = GetMissionProgress(mission.missionID);
            if (progress.isCompleted) return; // Already completed
            
            progress.isCompleted = true;
            progress.completionTime = DateTime.Now;
            
            // Track for session summary
            sessionCompletedMissions.Add(mission.xpReward);
            
            DebugLog($"Mission completed: {mission.missionName} - Rewards: {mission.xpReward} XP, {mission.gemReward} gems");
            
            // Play completion sound
            PlayMissionAudio(missionCompleteSound);
            
            // Fire events
            OnMissionCompleted?.Invoke(mission);
            OnMissionProgressUpdated?.Invoke(mission, progress.currentProgress);
            
            // Check tier unlocking
            CheckTierUnlocking();
            
            // Auto-save
            SaveMissionData();
        }
        
        #endregion
        
        #region Mission Management
        
        /// <summary>
        /// Equip a mission (add to equipped slots)
        /// </summary>
        public bool EquipMission(string missionID)
        {
            if (string.IsNullOrEmpty(missionID)) return false;
            
            var mission = GetMission(missionID);
            if (mission == null) return false;
            
            // Check if mission is unlocked
            if (!IsMissionUnlocked(mission)) return false;
            
            // Check if already equipped
            if (IsMissionEquipped(missionID)) return false;
            
            // Find empty slot
            Debug.Log($"[MissionManager] EquipMission: Looking for empty slot. Current slots: [{string.Join(", ", saveData.equippedMissionIDs)}]");
            for (int i = 0; i < maxEquippedMissions; i++)
            {
                Debug.Log($"[MissionManager] EquipMission: Checking slot {i}: '{saveData.equippedMissionIDs[i]}' (IsNullOrEmpty: {string.IsNullOrEmpty(saveData.equippedMissionIDs[i])})");
                if (string.IsNullOrEmpty(saveData.equippedMissionIDs[i]))
                {
                    Debug.Log($"[MissionManager] EquipMission: Found empty slot {i}! Equipping '{mission.missionName}' (ID: '{missionID}')");
                    saveData.equippedMissionIDs[i] = missionID;
                    GetMissionProgress(missionID).isEquipped = true;
                    
                    Debug.Log($"[MissionManager] EquipMission: After equipping - slots: [{string.Join(", ", saveData.equippedMissionIDs)}]");
                    DebugLog($"Mission equipped: {mission.missionName}");
                    OnMissionEquipped?.Invoke(mission);
                    SaveMissionData();
                    return true;
                }
            }
            
            DebugLog($"Failed to equip mission {mission.missionName} - no empty slots");
            return false;
        }
        
        /// <summary>
        /// Unequip a mission
        /// </summary>
        public bool UnequipMission(string missionID)
        {
            if (string.IsNullOrEmpty(missionID)) return false;
            
            var mission = GetMission(missionID);
            if (mission == null) return false;
            
            // Find and remove from equipped slots
            for (int i = 0; i < maxEquippedMissions; i++)
            {
                if (saveData.equippedMissionIDs[i] == missionID)
                {
                    saveData.equippedMissionIDs[i] = null;
                    GetMissionProgress(missionID).isEquipped = false;
                    
                    DebugLog($"Mission unequipped: {mission.missionName}");
                    OnMissionUnequipped?.Invoke(mission);
                    SaveMissionData();
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Claim mission rewards (XP and gems)
        /// </summary>
        public bool ClaimMissionRewards(string missionID)
        {
            var mission = GetMission(missionID);
            var progress = GetMissionProgress(missionID);
            
            if (mission == null || progress == null) return false;
            if (!progress.isCompleted || progress.hasClaimedReward) return false;
            
            // Mark as claimed
            progress.hasClaimedReward = true;
            
            // Add gems through StashManager
            if (mission.gemReward > 0)
            {
                AddGemReward(mission.gemReward);
            }
            
            // Add items to inventory (if any)
            if (mission.itemRewards != null && mission.itemRewards.Length > 0)
            {
                AddItemRewards(mission.itemRewards);
            }
            
            DebugLog($"Mission rewards claimed: {mission.missionName} - {mission.xpReward} XP, {mission.gemReward} gems");
            
            // Remove from equipped missions after claiming
            UnequipMission(missionID);
            
            SaveMissionData();
            return true;
        }
        
        #endregion
        
        #region Reward System Integration
        
        /// <summary>
        /// Add gem rewards through StashManager
        /// </summary>
        void AddGemReward(int gemAmount)
        {
            var stashManager = FindObjectOfType<StashManager>();
            if (stashManager != null)
            {
                // Load gem item data
                var gemData = Resources.Load<GemItemData>("Items/GemItemData");
                if (gemData != null)
                {
                    // Try inventory first, then stash
                    if (stashManager.inventoryGemSlot != null)
                    {
                        AddGemsToSlot(stashManager.inventoryGemSlot, gemData, gemAmount);
                    }
                    else if (stashManager.stashGemSlot != null)
                    {
                        AddGemsToSlot(stashManager.stashGemSlot, gemData, gemAmount);
                    }
                    
                    DebugLog($"Added {gemAmount} gems to player inventory via StashManager");
                }
                else
                {
                    Debug.LogError("MissionManager: Could not load GemItemData from Resources/Items/GemItemData");
                }
            }
            else
            {
                Debug.LogWarning("MissionManager: StashManager not found - cannot add gem rewards");
            }
        }
        
        void AddGemsToSlot(UnifiedSlot slot, GemItemData gemData, int amount)
        {
            if (slot.IsEmpty)
            {
                slot.SetItem(gemData, amount);
            }
            else if (slot.CurrentItem is GemItemData)
            {
                slot.SetItem(slot.CurrentItem, slot.ItemCount + amount);
            }
        }
        
        /// <summary>
        /// Add item rewards to inventory
        /// </summary>
        void AddItemRewards(ChestItemData[] items)
        {
            var inventoryManager = InventoryManager.Instance;
            if (inventoryManager != null)
            {
                foreach (var item in items)
                {
                    if (item != null)
                    {
                        // Try to add item to inventory
                        // This would need the specific method from InventoryManager
                        DebugLog($"Added item reward: {item.itemName}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Get total session mission XP for XP summary integration
        /// </summary>
        public int GetSessionMissionXP()
        {
            return sessionCompletedMissions.Sum();
        }
        
        /// <summary>
        /// Clear session data (called after XP summary is shown)
        /// </summary>
        public void ClearSessionData()
        {
            sessionCompletedMissions.Clear();
        }
        
        #endregion
        
        #region Tier System
        
        /// <summary>
        /// Check if all missions in current tier are completed to unlock next tier
        /// </summary>
        void CheckTierUnlocking()
        {
            for (int tier = 1; tier <= 3; tier++)
            {
                if (tier <= saveData.currentUnlockedTier) continue; // Already unlocked
                
                var tierMissions = GetMissionsInTier(tier - 1); // Check previous tier
                bool allCompleted = tierMissions.All(m => GetMissionProgress(m.missionID).isCompleted);
                
                if (allCompleted && tier > saveData.currentUnlockedTier)
                {
                    saveData.currentUnlockedTier = tier;
                    DebugLog($"Tier {tier} unlocked!");
                    OnTierUnlocked?.Invoke(tier);
                    break;
                }
            }
        }
        
        /// <summary>
        /// Check if a mission is unlocked based on tier progression
        /// </summary>
        public bool IsMissionUnlocked(Mission mission)
        {
            // Use GetCurrentUnlockedTier() for consistency with tier section logic
            return (int)mission.tier <= GetCurrentUnlockedTier();
        }
        
        /// <summary>
        /// Get all missions in a specific tier
        /// </summary>
        public List<Mission> GetMissionsInTier(int tier)
        {
            var tierMissions = allMissions.Where(m => m != null && (int)m.tier == tier).ToList();
            DebugLog($"Found {tierMissions.Count} missions in tier {tier}");
            return tierMissions;
        }
        
        #endregion
        
        #region Data Accessors
        
        /// <summary>
        /// Get mission by ID
        /// </summary>
        public Mission GetMission(string missionID)
        {
            Debug.Log($"[MissionManager] GetMission called with ID: '{missionID}'");
            
            if (missionLookup.TryGetValue(missionID, out var mission))
            {
                Debug.Log($"[MissionManager] Found mission: '{mission.missionName}' for ID: '{missionID}'");
                return mission;
            }
            else
            {
                Debug.LogError($"[MissionManager] Mission with ID '{missionID}' NOT FOUND in lookup table!");
                Debug.Log($"[MissionManager] Available mission IDs: [{string.Join(", ", missionLookup.Keys)}]");
                return null;
            }
        }
        
        /// <summary>
        /// Get mission progress, creating if needed
        /// </summary>
        public MissionProgress GetMissionProgress(string missionID)
        {
            if (!missionProgressLookup.TryGetValue(missionID, out var progress))
            {
                progress = new MissionProgress(missionID);
                missionProgressLookup[missionID] = progress;
            }
            return progress;
        }
        
        /// <summary>
        /// Get all currently equipped missions
        /// </summary>
        public List<Mission> GetEquippedMissions()
        {
            var equipped = new List<Mission>();
            
            Debug.Log($"[MissionManager] GetEquippedMissions called. Equipped slots: [{string.Join(", ", saveData.equippedMissionIDs)}]");
            
            for (int i = 0; i < saveData.equippedMissionIDs.Length; i++)
            {
                var missionID = saveData.equippedMissionIDs[i];
                Debug.Log($"[MissionManager] Slot {i}: '{missionID}' (IsNullOrEmpty: {string.IsNullOrEmpty(missionID)})");
                
                if (!string.IsNullOrEmpty(missionID))
                {
                    var mission = GetMission(missionID);
                    if (mission != null)
                    {
                        Debug.Log($"[MissionManager] Adding mission to equipped list: {mission.missionName}");
                        equipped.Add(mission);
                    }
                    else
                    {
                        Debug.LogError($"[MissionManager] Mission with ID '{missionID}' not found in allMissions!");
                    }
                }
            }
            
            Debug.Log($"[MissionManager] Returning {equipped.Count} equipped missions");
            return equipped;
        }
        
        /// <summary>
        /// Check if mission is currently equipped
        /// </summary>
        public bool IsMissionEquipped(string missionID)
        {
            return saveData.equippedMissionIDs.Contains(missionID);
        }
        
        /// <summary>
        /// Get the equipped mission IDs array for slot-based UI assignment
        /// </summary>
        public string[] GetEquippedMissionIDs()
        {
            Debug.Log($"[MissionManager] GetEquippedMissionIDs called. Returning: [{string.Join(", ", saveData.equippedMissionIDs)}]");
            return saveData.equippedMissionIDs;
        }
        
        /// <summary>
        /// Get number of available equipped slots
        /// </summary>
        public int GetAvailableEquippedSlots()
        {
            return maxEquippedMissions - saveData.equippedMissionIDs.Count(id => !string.IsNullOrEmpty(id));
        }
        
        /// <summary>
        /// Get current unlocked tier
        /// </summary>
        public int GetCurrentUnlockedTier()
        {
            // For now, return 3 to unlock all tiers for testing
            // Later you can implement tier progression logic
            return 3;
        }
        
        #endregion
        
        #region Save/Load System
        
        /// <summary>
        /// Save mission data to persistent storage
        /// </summary>
        public void SaveMissionData()
        {
            try
            {
                // Convert dictionary to array for serialization
                saveData.missionProgresses = missionProgressLookup.Values.ToArray();
                
                string jsonData = JsonUtility.ToJson(saveData, true);
                File.WriteAllText(savePath, jsonData);
                DebugLog("Mission data saved successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save mission data: {e.Message}");
            }
        }
        
        /// <summary>
        /// Load mission data from persistent storage
        /// </summary>
        void LoadMissionData()
        {
            try
            {
                if (File.Exists(savePath))
                {
                    string jsonData = File.ReadAllText(savePath);
                    saveData = JsonUtility.FromJson<MissionSaveData>(jsonData);
                    
                    // Rebuild lookup dictionary
                    missionProgressLookup.Clear();
                    if (saveData.missionProgresses != null)
                    {
                        foreach (var progress in saveData.missionProgresses)
                        {
                            missionProgressLookup[progress.missionID] = progress;
                        }
                    }
                    
                    DebugLog($"Mission data loaded - {missionProgressLookup.Count} missions tracked");
                }
                else
                {
                    // Create new save data
                    saveData = new MissionSaveData();
                    DebugLog("Created new mission save data");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load mission data: {e.Message}");
                saveData = new MissionSaveData();
            }
        }
        
        #endregion
        
        #region Gameplay Integration Hooks
        
        /// <summary>
        /// Called when player kills an enemy
        /// </summary>
        public void OnEnemyKilled(string enemyType)
        {
            UpdateMissionProgress(MissionType.Kill, enemyType, 1);
        }
        
        /// <summary>
        /// Called when player conquers a platform
        /// </summary>
        public void OnPlatformConquered()
        {
            UpdateMissionProgress(MissionType.Conquer, "", 1);
        }
        
        /// <summary>
        /// Called when player loots a chest
        /// </summary>
        public void OnChestLooted()
        {
            UpdateMissionProgress(MissionType.Loot, "", 1);
        }
        
        /// <summary>
        /// Called when player collects an item
        /// </summary>
        public void OnItemCollected(string itemName)
        {
            UpdateMissionProgress(MissionType.Collect, itemName, 1);
        }
        
        /// <summary>
        /// Called when player crafts an item via FORGE
        /// </summary>
        public void OnItemCrafted(string itemName)
        {
            UpdateMissionProgress(MissionType.Craft, itemName, 1);
        }
        
        /// <summary>
        /// Reset mission progress if player dies (for non-persistent missions)
        /// </summary>
        public void OnPlayerDeath()
        {
            var equippedMissions = GetEquippedMissions();
            bool anyReset = false;
            
            foreach (var mission in equippedMissions)
            {
                if (!mission.persistProgressOnDeath)
                {
                    var progress = GetMissionProgress(mission.missionID);
                    if (progress.currentProgress > 0 && !progress.isCompleted)
                    {
                        progress.currentProgress = 0;
                        DebugLog($"Reset mission progress due to death: {mission.missionName}");
                        anyReset = true;
                    }
                }
            }
            
            if (anyReset)
            {
                SaveMissionData();
            }
        }
        
        #endregion
        
        #region Utility
        
        /// <summary>
        /// Play mission audio safely
        /// </summary>
        void PlayMissionAudio(AudioClip clip)
        {
            if (clip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound3DAtPoint(clip, transform.position, audioVolume);
            }
        }
        
        /// <summary>
        /// Debug logging with toggle
        /// </summary>
        void DebugLog(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[MissionManager] {message}");
            }
        }
        
        /// <summary>
        /// Reset all mission data (for testing)
        /// </summary>
        [ContextMenu("Reset All Mission Data")]
        public void DEBUG_ResetAllMissionData()
        {
            missionProgressLookup.Clear();
            saveData = new MissionSaveData();
            sessionCompletedMissions.Clear();
            SaveMissionData();
            DebugLog("All mission data reset!");
        }
        
        #endregion
    }
}