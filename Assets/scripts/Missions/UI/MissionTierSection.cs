using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace GeminiGauntlet.Missions.UI
{
    /// <summary>
    /// UI component for a tier section containing multiple missions
    /// </summary>
    public class MissionTierSection : MonoBehaviour
    {
        [Header("Tier Section UI")]
        public TextMeshProUGUI tierTitleText;
        public TextMeshProUGUI tierStatusText;
        public Transform missionCardsContainer;
        public GameObject lockedOverlay;
        public GameObject missionCardPrefab;
        
        [Header("Visual States")]
        public Color unlockedTierColor = Color.white;
        public Color lockedTierColor = Color.gray;
        
        private int tierNumber;
        private List<Mission> tierMissions;
        private List<MissionCard> missionCards;
        
        /// <summary>
        /// Public accessor for this section's tier number.
        /// </summary>
        public int TierNumber => tierNumber;

        /// <summary>
        /// Show or hide the entire tier section GameObject.
        /// </summary>
        public void SetSectionVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
        
        /// <summary>
        /// Initialize this tier section
        /// </summary>
        public void InitializeTierSection(int tier, List<Mission> missions, GameObject cardPrefab)
        {
            Debug.Log($"[TierSection] Initializing Tier {tier} with {missions.Count} missions.");

            tierNumber = tier;
            tierMissions = missions;
            missionCardPrefab = cardPrefab;
            missionCards = new List<MissionCard>();

            if (missionCardPrefab == null)
            {
                Debug.LogError($"[TierSection] Mission Card Prefab is NOT ASSIGNED for Tier {tier}! Cannot create mission cards.");
                return;
            }
            
            // Setup tier info
            UpdateTierInfo();
            
            // Create mission cards
            CreateMissionCards();
        }
        
        /// <summary>
        /// Update tier title and status with proper layout
        /// </summary>
        void UpdateTierInfo()
        {
            bool isUnlocked = MissionManager.Instance.GetCurrentUnlockedTier() >= tierNumber;
            
            // Set tier title (main header)
            if (tierTitleText != null)
            {
                tierTitleText.text = $"TIER {tierNumber}";
                tierTitleText.color = isUnlocked ? unlockedTierColor : lockedTierColor;
                
                // Ensure tier title is positioned as a header
                var titleRect = tierTitleText.GetComponent<RectTransform>();
                if (titleRect != null)
                {
                    // Position at top of section
                    titleRect.anchorMin = new Vector2(0, 1);
                    titleRect.anchorMax = new Vector2(1, 1);
                    titleRect.anchoredPosition = new Vector2(0, -10); // Small offset from top
                    titleRect.sizeDelta = new Vector2(0, 40); // Fixed height for title
                }
            }
            
            // Set tier status (below title)
            if (tierStatusText != null)
            {
                if (isUnlocked)
                {
                    int completed = GetCompletedMissionsCount();
                    int total = tierMissions.Count;
                    tierStatusText.text = $"Progress: {completed}/{total} missions completed";
                }
                else
                {
                    tierStatusText.text = "LOCKED - Complete previous tier to unlock";
                }
                
                // Position tier status below the title
                var statusRect = tierStatusText.GetComponent<RectTransform>();
                if (statusRect != null)
                {
                    // Position below title
                    statusRect.anchorMin = new Vector2(0, 1);
                    statusRect.anchorMax = new Vector2(1, 1);
                    statusRect.anchoredPosition = new Vector2(0, -55); // Below title (title height + spacing)
                    statusRect.sizeDelta = new Vector2(0, 25); // Fixed height for status
                }
            }
            
            // Adjust mission cards container to be below header section
            if (missionCardsContainer != null)
            {
                var containerRect = missionCardsContainer.GetComponent<RectTransform>();
                if (containerRect != null)
                {
                    // Position mission cards below header section
                    containerRect.anchorMin = new Vector2(0, 0);
                    containerRect.anchorMax = new Vector2(1, 1);
                    containerRect.anchoredPosition = new Vector2(0, 0);
                    containerRect.offsetMax = new Vector2(0, -90); // Leave space for header (title + status + spacing)
                }
            }
            
            // Show/hide locked overlay
            if (lockedOverlay != null)
                lockedOverlay.SetActive(!isUnlocked);
        }
        
        /// <summary>
        /// Get number of completed missions in this tier
        /// </summary>
        int GetCompletedMissionsCount()
        {
            int completed = 0;
            foreach (var mission in tierMissions)
            {
                var progress = MissionManager.Instance.GetMissionProgress(mission.missionID);
                if (progress.isCompleted)
                    completed++;
            }
            return completed;
        }
        
        /// <summary>
        /// Create mission cards for all missions in this tier with MANUAL POSITIONING
        /// </summary>
        void CreateMissionCards()
        {
            if (missionCardPrefab == null || missionCardsContainer == null) return;
            
            // Clear existing cards
            ClearMissionCards();
            
            // Create new cards with manual positioning
            for (int i = 0; i < tierMissions.Count; i++)
            {
                var mission = tierMissions[i];
                Debug.Log($"[TierSection] Creating card {i} for mission: {mission.missionName}");
                
                GameObject cardObj = Instantiate(missionCardPrefab, missionCardsContainer);
                MissionCard card = cardObj.GetComponent<MissionCard>();
                
                // MANUAL POSITIONING - Force consistent height and spacing!
                RectTransform rectTransform = cardObj.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    // Set anchors for full width
                    rectTransform.anchorMin = new Vector2(0, 1);
                    rectTransform.anchorMax = new Vector2(1, 1);
                    rectTransform.pivot = new Vector2(0.5f, 1);
                    
                    // FORCE EXACT SIZE - no flexibility!
                    rectTransform.sizeDelta = new Vector2(0, 120); // 0 width = stretch, 120 height FIXED
                    
                    // OPTIMAL SPACING - each card 250 pixels below the previous
                    float yPosition = -10 - (i * 250); // Start at -10, then -260, -510, etc.
                    rectTransform.anchoredPosition = new Vector2(0, yPosition);
                    
                    // FORCE the height again to make sure it sticks
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 120);
                    
                    Debug.Log($"[TierSection] Positioned card {i} to height 120 at Y: {yPosition}");
                }
                
                card.InitializeMissionCard(mission);
                missionCards.Add(card);
            }

            Debug.Log($"[TierSection] Finished creating {missionCards.Count} manually positioned cards for Tier {tierNumber}.");
        }
        
        /// <summary>
        /// Clear all mission cards
        /// </summary>
        void ClearMissionCards()
        {
            foreach (var card in missionCards)
            {
                if (card != null && card.gameObject != null)
                    Destroy(card.gameObject);
            }
            missionCards.Clear();
        }
        
        /// <summary>
        /// Refresh all mission cards in this section
        /// </summary>
        public void RefreshMissionCards()
        {
            UpdateTierInfo();
            
            foreach (var card in missionCards)
            {
                if (card != null)
                    card.RefreshMissionCard();
            }
        }
    }
}
