using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GeminiGauntlet.Missions.UI
{
    /// <summary>
    /// Individual mission card UI component
    /// </summary>
    [System.Serializable]
    public class MissionCard : MonoBehaviour
    {
        [Header("Mission Card UI")]
        public Image missionIcon;
        public TextMeshProUGUI missionNameText;
        public TextMeshProUGUI missionDescriptionText;
        public TextMeshProUGUI rewardsText;
        public Button acceptButton;
        public GameObject equippedIndicator;
        public GameObject completedIndicator;
        
        [Header("Visual States")]
        public Color normalCardColor = Color.white;
        public Color equippedCardColor = Color.cyan;
        public Color completedCardColor = Color.green;
        
        private Mission currentMission;
        private Image cardBackground;
        
        void Awake()
        {
            cardBackground = GetComponent<Image>();
        }
        
        /// <summary>
        /// Initialize this mission card
        /// </summary>
        public void InitializeMissionCard(Mission mission)
        {
            currentMission = mission;
            
            if (currentMission == null) return;
            
            // Set mission info
            if (missionNameText != null)
                missionNameText.text = mission.missionName;
            
            if (missionDescriptionText != null)
                missionDescriptionText.text = mission.missionDescription;
            
            if (missionIcon != null && mission.missionIcon != null)
                missionIcon.sprite = mission.missionIcon;
            
            // Set rewards text
            if (rewardsText != null)
            {
                string rewards = $"Rewards: {mission.xpReward} XP";
                if (mission.gemReward > 0)
                    rewards += $", {mission.gemReward} Gems";
                rewardsText.text = rewards;
            }
            
            // Setup accept button
            if (acceptButton != null)
            {
                acceptButton.onClick.RemoveAllListeners();
                acceptButton.onClick.AddListener(AcceptMission);
            }
            
            // Refresh card state
            RefreshMissionCard();
        }
        
        /// <summary>
        /// Refresh this mission card's visual state
        /// </summary>
        public void RefreshMissionCard()
        {
            if (currentMission == null || MissionManager.Instance == null) return;
            
            var progress = MissionManager.Instance.GetMissionProgress(currentMission.missionID);
            bool isEquipped = MissionManager.Instance.IsMissionEquipped(currentMission.missionID);
            bool isCompleted = progress.isCompleted;
            bool isUnlocked = MissionManager.Instance.IsMissionUnlocked(currentMission);
            
            // Update visual state
            if (equippedIndicator != null)
                equippedIndicator.SetActive(isEquipped);
            
            if (completedIndicator != null)
                completedIndicator.SetActive(isCompleted);
            
            // Update card color
            if (cardBackground != null)
            {
                if (isCompleted)
                    cardBackground.color = completedCardColor;
                else if (isEquipped)
                    cardBackground.color = equippedCardColor;
                else
                    cardBackground.color = normalCardColor;
            }
            
            // Update accept button
            if (acceptButton != null)
            {
                var buttonText = acceptButton.GetComponentInChildren<TextMeshProUGUI>();
                
                if (!isUnlocked)
                {
                    acceptButton.interactable = false;
                    if (buttonText != null) buttonText.text = "LOCKED";
                }
                else if (isCompleted)
                {
                    acceptButton.interactable = false;
                    if (buttonText != null) buttonText.text = "COMPLETED";
                }
                else if (isEquipped)
                {
                    acceptButton.interactable = true;
                    if (buttonText != null) buttonText.text = "UNEQUIP";
                }
                else
                {
                    acceptButton.interactable = true;
                    if (buttonText != null) buttonText.text = "ACCEPT";
                }
            }
        }
        
        /// <summary>
        /// Accept/unequip this mission
        /// </summary>
        void AcceptMission()
        {
            if (currentMission == null || MissionManager.Instance == null) return;
            
            bool isEquipped = MissionManager.Instance.IsMissionEquipped(currentMission.missionID);
            
            if (isEquipped)
            {
                MissionManager.Instance.UnequipMission(currentMission.missionID);
            }
            else
            {
                if (MissionManager.Instance.GetAvailableEquippedSlots() > 0)
                {
                    MissionManager.Instance.EquipMission(currentMission.missionID);
                }
                else
                {
                    Debug.Log("[MissionCard] No available equipped slots!");
                    // Could show a popup here about max missions equipped
                }
            }
        }
    }
}
