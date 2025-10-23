using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GeminiGauntlet.Missions.UI
{
    /// <summary>
    /// Individual mission slot UI component
    /// </summary>
    public class MissionSlotUI : MonoBehaviour
    {
        [Header("Mission Slot UI Elements")]
        public Image missionIcon;
        public TextMeshProUGUI missionNameText;
        public TextMeshProUGUI missionDescriptionText;
        public Slider progressBar;
        public TextMeshProUGUI progressText;
        public GameObject completedIndicator;
        public Button unequipButton;
        
        [Header("Visual States")]
        public Color normalColor = Color.white;
        public Color completedColor = Color.green;
        
        private Mission currentMission;
        private MissionProgress currentProgress;
        
        /// <summary>
        /// Initialize this slot with mission data
        /// </summary>
        public void InitializeSlot(Mission mission)
        {
            Debug.Log($"[MissionSlotUI] Initializing slot with mission: {mission?.missionName}");
            
            currentMission = mission;
            currentProgress = MissionManager.Instance?.GetMissionProgress(mission.missionID);
            
            if (currentMission == null)
            {
                Debug.LogError("[MissionSlotUI] Mission is NULL! Cannot initialize slot.");
                return;
            }
            
            // Set mission info
            if (missionNameText != null)
                missionNameText.text = mission.missionName;
            else
                Debug.LogWarning("[MissionSlotUI] missionNameText is NULL!");
            
            if (missionDescriptionText != null)
                missionDescriptionText.text = mission.GetMissionTypeDisplayName();
            else
                Debug.LogWarning("[MissionSlotUI] missionDescriptionText is NULL!");
            
            if (missionIcon != null && mission.missionIcon != null)
                missionIcon.sprite = mission.missionIcon;
            
            // Setup unequip button
            if (unequipButton != null)
            {
                Debug.Log($"[MissionSlotUI] Setting up unequip button for mission: {mission.missionName}");
                unequipButton.onClick.RemoveAllListeners();
                unequipButton.onClick.AddListener(() => UnequipMission());
                
                // Set button text
                var buttonText = unequipButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                    buttonText.text = "UNEQUIP";
            }
            else
            {
                Debug.LogError($"[MissionSlotUI] Unequip button is NULL for mission: {mission.missionName}!");
            }
            
            // Update progress display
            UpdateProgress(currentProgress?.currentProgress ?? 0);
        }
        
        /// <summary>
        /// Update the progress display
        /// </summary>
        public void UpdateProgress(int newProgress)
        {
            if (currentMission == null) return;
            
            // Update progress bar
            if (progressBar != null)
            {
                progressBar.maxValue = currentMission.targetCount;
                progressBar.value = newProgress;
            }
            
            // Update progress text
            if (progressText != null)
            {
                progressText.text = currentMission.GetProgressText(newProgress);
            }
            
            // Check if completed
            if (currentMission.IsComplete(newProgress))
            {
                MarkAsCompleted();
            }
        }
        
        /// <summary>
        /// Mark this mission as completed visually
        /// </summary>
        public void MarkAsCompleted()
        {
            // Show completed indicator
            if (completedIndicator != null)
                completedIndicator.SetActive(true);
            
            // Change colors to indicate completion
            if (missionNameText != null)
                missionNameText.color = completedColor;
            
            if (progressText != null)
                progressText.text = "COMPLETED!";
            
            // Change unequip button to "Claim Rewards"
            if (unequipButton != null)
            {
                var buttonText = unequipButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                    buttonText.text = "Claim Rewards";
            }
        }
        
        /// <summary>
        /// Unequip this mission
        /// </summary>
        void UnequipMission()
        {
            Debug.Log($"[MissionSlotUI] UnequipMission called for: {currentMission?.missionName}");
            
            if (currentMission == null)
            {
                Debug.LogError("[MissionSlotUI] Cannot unequip - currentMission is NULL!");
                return;
            }
            
            if (MissionManager.Instance == null)
            {
                Debug.LogError("[MissionSlotUI] Cannot unequip - MissionManager.Instance is NULL!");
                return;
            }
            
            // If completed, claim rewards instead of unequipping
            if (currentProgress != null && currentProgress.isCompleted && !currentProgress.hasClaimedReward)
            {
                Debug.Log($"[MissionSlotUI] Claiming rewards for completed mission: {currentMission.missionName}");
                MissionManager.Instance.ClaimMissionRewards(currentMission.missionID);
            }
            else
            {
                Debug.Log($"[MissionSlotUI] Unequipping mission: {currentMission.missionName}");
                MissionManager.Instance.UnequipMission(currentMission.missionID);
            }
        }
        
        /// <summary>
        /// Get the mission ID for this slot
        /// </summary>
        public string GetMissionID()
        {
            return currentMission?.missionID ?? "";
        }
    }
}
