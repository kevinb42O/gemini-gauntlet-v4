using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

namespace GeminiGauntlet.Missions.UI
{
    /// <summary>
    /// UI component for displaying equipped missions in the main menu
    /// Shows max 3 missions with progress bars and details
    /// </summary>
    public class EquippedMissionsUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Text shown when no missions are equipped")]
        public TextMeshProUGUI noMissionsText;
        
        [Header("Manual Mission Slots")]
        [Tooltip("Mission slot 1 - assign manually in inspector")]
        public MissionSlotUI missionSlot1;
        [Tooltip("Mission slot 2 - assign manually in inspector")]
        public MissionSlotUI missionSlot2;
        [Tooltip("Mission slot 3 - assign manually in inspector")]
        public MissionSlotUI missionSlot3;
        
        [Header("Audio")]
        [Tooltip("Sound played when mission progress updates")]
        public AudioClip progressUpdateSound;
        [Tooltip("Sound played when mission completes")]
        public AudioClip missionCompleteSound;
        
        // Runtime data
        private MissionSlotUI[] missionSlots;
        private AudioSource audioSource;
        
        void Awake()
        {
            // Initialize mission slots array
            missionSlots = new MissionSlotUI[] { missionSlot1, missionSlot2, missionSlot3 };
            
            // Get audio source
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        void Start()
        {
            // Subscribe to mission events
            if (MissionManager.Instance != null)
            {
                MissionManager.OnMissionProgressUpdated += OnMissionProgressUpdated;
                MissionManager.OnMissionCompleted += OnMissionCompleted;
                MissionManager.OnMissionEquipped += OnMissionEquipped;
                MissionManager.OnMissionUnequipped += OnMissionUnequipped;
            }
            
            // Initial UI refresh
            RefreshEquippedMissions();
        }
        
        void OnDestroy()
        {
            // Unsubscribe from events
            if (MissionManager.Instance != null)
            {
                MissionManager.OnMissionProgressUpdated -= OnMissionProgressUpdated;
                MissionManager.OnMissionCompleted -= OnMissionCompleted;
                MissionManager.OnMissionEquipped -= OnMissionEquipped;
                MissionManager.OnMissionUnequipped -= OnMissionUnequipped;
            }
        }
        
        /// <summary>
        /// Refresh the entire equipped missions display
        /// </summary>
        public void RefreshEquippedMissions()
        {
            Debug.Log("[EquippedMissionsUI] RefreshEquippedMissions called");
            
            if (MissionManager.Instance == null)
            {
                Debug.LogError("[EquippedMissionsUI] MissionManager.Instance is NULL!");
                return;
            }
            
            // Clear existing slots
            ClearMissionSlots();
            
            // Get equipped missions
            var equippedMissions = MissionManager.Instance.GetEquippedMissions();
            Debug.Log($"[EquippedMissionsUI] Retrieved {equippedMissions.Count} equipped missions from MissionManager");
            
            // Debug: Print all equipped mission names with detailed info
            Debug.Log($"[EquippedMissionsUI] DETAILED LIST DEBUG - Count: {equippedMissions.Count}");
            for (int i = 0; i < equippedMissions.Count; i++)
            {
                var mission = equippedMissions[i];
                if (mission == null)
                {
                    Debug.LogError($"[EquippedMissionsUI] Equipped mission {i}: NULL MISSION!");
                }
                else
                {
                    Debug.Log($"[EquippedMissionsUI] Equipped mission {i}: '{mission.missionName}' (ID: '{mission.missionID}')");
                }
            }
            
            if (equippedMissions.Count == 0)
            {
                // Show "no missions" message
                ShowNoMissionsState();
            }
            else
            {
                // Show equipped missions
                ShowEquippedMissions(equippedMissions);
            }
        }
        
        /// <summary>
        /// Clear all mission slot UI elements
        /// </summary>
        void ClearMissionSlots()
        {
            // CRITICAL: Add null check for missionSlots array
            if (missionSlots == null)
            {
                Debug.LogWarning("[EquippedMissionsUI] missionSlots array is null. Cannot clear mission slots.");
                return;
            }
            
            // Clear each slot but don't destroy them (they're manually assigned)
            foreach (var slot in missionSlots)
            {
                if (slot != null && slot.gameObject != null)
                {
                    slot.gameObject.SetActive(false);
                }
            }
        }
        
        /// <summary>
        /// Show the "no missions equipped" state
        /// </summary>
        void ShowNoMissionsState()
        {
            if (noMissionsText != null)
            {
                noMissionsText.gameObject.SetActive(true);
                noMissionsText.text = "No missions equipped\nClick MISSIONS to select missions";
            }
            
            // Hide all mission slots
            foreach (var slot in missionSlots)
            {
                if (slot != null)
                    slot.gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// Show equipped missions with their progress
        /// </summary>
        void ShowEquippedMissions(List<Mission> missions)
        {
            Debug.Log($"[EquippedMissionsUI] Showing {missions.Count} equipped missions");
            
            if (noMissionsText != null)
                noMissionsText.gameObject.SetActive(false);
            
            // First, hide all slots
            for (int i = 0; i < missionSlots.Length; i++)
            {
                if (missionSlots[i] != null)
                {
                    missionSlots[i].gameObject.SetActive(false);
                }
            }
            
            // NEW: Display missions sequentially in first available UI slots
            if (MissionManager.Instance != null)
            {
                var equippedIDs = MissionManager.Instance.GetEquippedMissionIDs();
                Debug.Log($"[EquippedMissionsUI] Backend equipped slots: [{string.Join(", ", equippedIDs)}]");
                
                // Collect all non-empty equipped missions
                var equippedMissions = new List<Mission>();
                for (int i = 0; i < equippedIDs.Length; i++)
                {
                    var missionID = equippedIDs[i];
                    if (!string.IsNullOrEmpty(missionID))
                    {
                        var mission = missions.FirstOrDefault(m => m.missionID == missionID);
                        if (mission != null)
                        {
                            equippedMissions.Add(mission);
                            Debug.Log($"[EquippedMissionsUI] Found equipped mission: '{mission.missionName}' from backend slot {i}");
                        }
                        else
                        {
                            Debug.LogError($"[EquippedMissionsUI] Could not find mission with ID '{missionID}' from backend slot {i}!");
                        }
                    }
                }
                
                // Display missions sequentially in first available UI slots
                for (int uiSlot = 0; uiSlot < equippedMissions.Count && uiSlot < missionSlots.Length; uiSlot++)
                {
                    if (missionSlots[uiSlot] != null)
                    {
                        Debug.Log($"[EquippedMissionsUI] Displaying mission '{equippedMissions[uiSlot].missionName}' in UI slot {uiSlot}");
                        missionSlots[uiSlot].gameObject.SetActive(true);
                        missionSlots[uiSlot].InitializeSlot(equippedMissions[uiSlot]);
                    }
                }
            }
            else
            {
                Debug.LogError("[EquippedMissionsUI] MissionManager.Instance is null! Cannot get equipped slot positions.");
            }
            
            Debug.Log($"[EquippedMissionsUI] Showing {missions.Count} equipped missions in correct slot positions");
        }
        
        /// <summary>
        /// Find mission slot by mission ID
        /// </summary>
        MissionSlotUI FindMissionSlot(string missionID)
        {
            foreach (var slot in missionSlots)
            {
                if (slot != null && slot.GetMissionID() == missionID)
                    return slot;
            }
            return null;
        }
        
        #region Event Handlers
        
        void OnMissionProgressUpdated(Mission mission, int newProgress)
        {
            var slot = FindMissionSlot(mission.missionID);
            if (slot != null)
            {
                slot.UpdateProgress(newProgress);
                PlaySound(progressUpdateSound);
            }
        }
        
        void OnMissionCompleted(Mission mission)
        {
            var slot = FindMissionSlot(mission.missionID);
            if (slot != null)
            {
                slot.MarkAsCompleted();
                PlaySound(missionCompleteSound);
            }
        }
        
        void OnMissionEquipped(Mission mission)
        {
            // Refresh the entire display when a new mission is equipped
            RefreshEquippedMissions();
        }
        
        void OnMissionUnequipped(Mission mission)
        {
            // Refresh the entire display when a mission is unequipped
            RefreshEquippedMissions();
        }
        
        #endregion
        
        #region Utility
        
        /// <summary>
        /// Play audio clip safely
        /// </summary>
        void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
        
        /// <summary>
        /// Manual refresh button (for testing)
        /// </summary>
        [ContextMenu("Refresh Equipped Missions")]
        public void DEBUG_RefreshEquippedMissions()
        {
            RefreshEquippedMissions();
        }
        
        #endregion
    }
}