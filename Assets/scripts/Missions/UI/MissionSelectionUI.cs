using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

namespace GeminiGauntlet.Missions.UI
{
    /// <summary>
    /// Mission selection UI for the dedicated mission canvas
    /// Shows missions organized by tiers and allows mission acceptance
    /// </summary>
    public class MissionSelectionUI : MonoBehaviour
    {
        [Header("UI Layout")]
        [Tooltip("Container for tier sections")]
        public Transform tierContainer;
        [Tooltip("Prefab for tier section UI")]
        public GameObject tierSectionPrefab;
        [Tooltip("Prefab for mission card UI")]
        public GameObject missionCardPrefab;
        
        [Header("Header UI")]
        [Tooltip("Title text for the mission selection screen")]
        public TextMeshProUGUI titleText;
        [Tooltip("Info text about equipped missions")]
        public TextMeshProUGUI equippedSlotsText;
        
        [Header("Navigation")]
        [Tooltip("Button to return to main menu")]
        public Button backButton;
        [Tooltip("Reference to camera for navigation (if needed)")]
        public Camera missionSelectionCamera;
        
        [Header("Audio")]
        [Tooltip("Sound played when mission is accepted")]
        public AudioClip missionAcceptSound;
        [Tooltip("Sound played when UI navigates")]
        public AudioClip navigationSound;
        
        // Runtime data
        private List<MissionTierSection> tierSections;
        private AudioSource audioSource;
        
        // Current filter: null = show all tiers, otherwise show only that tier
        private int? currentFilterTier = null;
        
        void Awake()
        {
            tierSections = new List<MissionTierSection>();
            
            // Get audio source
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        void Start()
        {
            // Setup navigation
            if (backButton != null)
            {
                backButton.onClick.AddListener(GoBackToMainMenu);
            }
            
            // Subscribe to mission events
            if (MissionManager.Instance != null)
            {
                MissionManager.OnMissionEquipped += OnMissionEquipped;
                MissionManager.OnMissionUnequipped += OnMissionUnequipped;
                MissionManager.OnTierUnlocked += OnTierUnlocked;
            }
            
            // Auto-unequip completed missions when opening UI
            AutoUnequipCompletedMissions();
            
            // Build mission selection UI
            BuildMissionSelectionUI();
        }
        
        void OnDestroy()
        {
            // Unsubscribe from events
            if (MissionManager.Instance != null)
            {
                MissionManager.OnMissionEquipped -= OnMissionEquipped;
                MissionManager.OnMissionUnequipped -= OnMissionUnequipped;
                MissionManager.OnTierUnlocked -= OnTierUnlocked;
            }
        }
        
        /// <summary>
        /// Build the complete mission selection UI
        /// </summary>
        void BuildMissionSelectionUI()
        {
            Debug.Log("[MissionSelectionUI] BuildMissionSelectionUI called");
            
            if (MissionManager.Instance == null)
            {
                Debug.LogError("[MissionSelectionUI] MissionManager.Instance is NULL! Cannot build UI.");
                return;
            }
            
            Debug.Log($"[MissionSelectionUI] MissionManager found. Total missions: {MissionManager.Instance.allMissions?.Length ?? 0}");
            
            // Update header info
            UpdateHeaderInfo();
            
            // Clear existing tier sections
            ClearTierSections();
            
            // Create tier sections for tiers 1-3
            for (int tier = 1; tier <= 3; tier++)
            {
                Debug.Log($"[MissionSelectionUI] Creating tier section for tier {tier}");
                CreateTierSection(tier);
            }
            
            // Re-apply current filter if any (persist selection across rebuilds)
            if (currentFilterTier.HasValue)
            {
                FilterByTier(currentFilterTier.Value);
            }
            else
            {
                FilterByTier(1);
            }
            
            Debug.Log("[MissionSelectionUI] BuildMissionSelectionUI completed");
        }
        
        /// <summary>
        /// Update header information
        /// </summary>
        void UpdateHeaderInfo()
        {
            if (titleText != null)
                titleText.text = "Mission Selection";
            
            if (equippedSlotsText != null && MissionManager.Instance != null)
            {
                int equipped = MissionManager.Instance.GetEquippedMissions().Count;
                int maxEquipped = MissionManager.Instance.maxEquippedMissions;
                equippedSlotsText.text = $"Equipped Missions: {equipped}/{maxEquipped}";
            }
        }
        
        /// <summary>
        /// Clear all tier sections
        /// </summary>
        void ClearTierSections()
        {
            foreach (var section in tierSections)
            {
                if (section != null && section.gameObject != null)
                    Destroy(section.gameObject);
            }
            tierSections.Clear();
        }
        
        /// <summary>
        /// Create a tier section with its missions
        /// </summary>
        void CreateTierSection(int tier)
        {
            if (tierSectionPrefab == null || tierContainer == null) return;
            
            // Get missions for this tier
            var tierMissions = MissionManager.Instance.GetMissionsInTier(tier);
            if (tierMissions.Count == 0) return;
            
            // Create tier section
            GameObject sectionObj = Instantiate(tierSectionPrefab, tierContainer);
            MissionTierSection section = sectionObj.GetComponent<MissionTierSection>();
            
            if (section == null)
                section = sectionObj.AddComponent<MissionTierSection>();
            
            // Initialize section
            section.InitializeTierSection(tier, tierMissions, missionCardPrefab);
            tierSections.Add(section);
        }
        
        /// <summary>
        /// Refresh the entire mission selection UI
        /// </summary>
        public void RefreshMissionSelection()
        {
            BuildMissionSelectionUI();
        }
        
        /// <summary>
        /// Go back to main menu
        /// </summary>
        void GoBackToMainMenu()
        {
            PlaySound(navigationSound);
            
            // Here you would implement camera navigation back to main menu
            // This depends on your specific camera/menu system
            Debug.Log("[MissionSelectionUI] Navigating back to main menu");
            
            // Hide this UI
            gameObject.SetActive(false);
        }
        
        #region Event Handlers
        
        void OnMissionEquipped(Mission mission)
        {
            UpdateHeaderInfo();
            // Refresh affected tier sections
            RefreshTierSections();
        }
        
        void OnMissionUnequipped(Mission mission)
        {
            UpdateHeaderInfo();
            // Refresh affected tier sections
            RefreshTierSections();
        }
        
        void OnTierUnlocked(int newTier)
        {
            // Rebuild UI to show newly unlocked tier
            BuildMissionSelectionUI();
        }
        
        #endregion
        
        /// <summary>
        /// Refresh all tier sections
        /// </summary>
        void RefreshTierSections()
        {
            foreach (var section in tierSections)
            {
                if (section != null)
                    section.RefreshMissionCards();
            }
        }
        
        /// <summary>
        /// Show only the given tier section and hide others.
        /// Hook your Tier 1/2/3 buttons to the wrapper methods below.
        /// </summary>
        public void FilterByTier(int tier)
        {
            currentFilterTier = tier;
            foreach (var section in tierSections)
            {
                if (section == null) continue;
                section.SetSectionVisible(section.TierNumber == tier);
            }
        }
        
        /// <summary>
        /// Show all tiers (clear filter).
        /// </summary>
        public void ShowAllTiers()
        {
            currentFilterTier = null;
            foreach (var section in tierSections)
            {
                if (section == null) continue;
                section.SetSectionVisible(true);
            }
        }
        
        // Inspector-friendly wrappers for three buttons
        public void OnFilterTier1() => FilterByTier(1);
        public void OnFilterTier2() => FilterByTier(2);
        public void OnFilterTier3() => FilterByTier(3);
        
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
        /// Auto-unequip completed missions when opening the UI
        /// </summary>
        void AutoUnequipCompletedMissions()
        {
            if (MissionManager.Instance == null) return;
            
            Debug.Log("[MissionSelectionUI] Checking for completed missions to auto-unequip...");
            
            var equippedMissions = MissionManager.Instance.GetEquippedMissions();
            int unequippedCount = 0;
            
            foreach (var mission in equippedMissions)
            {
                var progress = MissionManager.Instance.GetMissionProgress(mission.missionID);
                if (progress != null && progress.isCompleted)
                {
                    Debug.Log($"[MissionSelectionUI] Auto-unequipping completed mission: {mission.missionName}");
                    MissionManager.Instance.UnequipMission(mission.missionID);
                    unequippedCount++;
                }
            }
            
            if (unequippedCount > 0)
            {
                Debug.Log($"[MissionSelectionUI] Auto-unequipped {unequippedCount} completed missions");
            }
            else
            {
                Debug.Log("[MissionSelectionUI] No completed missions to auto-unequip");
            }
        }
        
        /// <summary>
        /// Manual refresh button (for testing)
        /// </summary>
        [ContextMenu("Refresh Mission Selection")]
        public void DEBUG_RefreshMissionSelection()
        {
            RefreshMissionSelection();
        }
    }
}