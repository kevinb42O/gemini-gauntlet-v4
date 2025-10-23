using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using CompanionAI;

namespace GeminiGauntlet.Progression
{
    /// <summary>
    /// MenuXPManager handles persistent XP and leveling in the menu.
    /// Shows animated level progression when returning from a game session.
    /// </summary>
    public class MenuXPManager : MonoBehaviour
    {
        public static MenuXPManager Instance { get; private set; }
        
        [Header("Persistent Level Data")]
        [SerializeField] private int currentLevel = 1;
        [SerializeField] private int currentXP = 0;
        [SerializeField] private int baseXPRequired = 100;
        [SerializeField] private float xpGrowthMultiplier = 1.5f;
        [SerializeField] private int maxLevel = 50;
        
        [Header("UI References")]
        [SerializeField] private GameObject xpProgressPanel;
        [SerializeField] private Slider xpProgressBar;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI xpText;
        [SerializeField] private TextMeshProUGUI newXPText;
        [SerializeField] private GameObject levelUpEffect;
        
        [Header("Animation Settings")]
        [SerializeField] private float xpFillSpeed = 100f; // XP per second
        [SerializeField] private float levelUpPauseDuration = 1.5f;
        [SerializeField] private bool autoShowOnStart = true;
        [SerializeField] private float autoShowDelay = 0.5f;
        
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip xpGainSound;
        [SerializeField] private AudioClip levelUpSound;
        [SerializeField] private float audioVolume = 0.7f;
        [SerializeField] private float basePitch = 0.8f; // Starting pitch for XP counting
        [SerializeField] private float pitchIncrement = 0.02f; // How much pitch increases per XP point
        [SerializeField] private float maxPitch = 1.5f; // Maximum pitch for XP counting
        
        [Header("Level Rewards")]
        [SerializeField] private bool enableLevelRewards = true;
        [SerializeField] private int gemsRewardPerLevel = 10;
        [SerializeField] private int[] specialLevelRewards = { 5, 10, 15, 20, 25, 30, 35, 40, 45, 50 };
        [SerializeField] private int specialRewardGems = 25;
        
        // Events
        public static event Action<int, int> OnLevelUp; // oldLevel, newLevel
        public static event Action<int> OnXPChanged; // totalXP
        
        // Properties
        public int CurrentLevel => currentLevel;
        public int CurrentXP => currentXP;
        public int XPRequiredForCurrentLevel => GetXPRequiredForLevel(currentLevel);
        public int XPRequiredForNextLevel => GetXPRequiredForLevel(currentLevel + 1);
        public bool IsMaxLevel => currentLevel >= maxLevel;
        
        private bool isAnimating = false;
        private int sessionXPToAdd = 0;
        
        void Awake()
        {
            Debug.Log($"[MenuXPManager] Awake called on GameObject: {gameObject.name}");
            
            if (Instance == null)
            {
                Instance = this;
                // REMOVED: DontDestroyOnLoad(gameObject) - MenuXPManager should be scene-specific
                // PersistentXPManager handles the cross-scene persistence now
                Debug.Log($"[MenuXPManager] Instance created for menu scene: {gameObject.name}");
                LoadPersistentData();
            }
            else
            {
                Debug.Log($"[MenuXPManager] Duplicate instance found, destroying: {gameObject.name}");
                Destroy(gameObject);
            }
        }
        
        // Safety: Auto-save on application pause/focus lost
        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) // Game is being paused
            {
                SavePersistentData();
                Debug.Log("MenuXPManager: Auto-saved on application pause");
            }
        }
        
        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) // Game lost focus
            {
                SavePersistentData();
                Debug.Log("MenuXPManager: Auto-saved on focus lost");
            }
        }
        
        void Start()
        {
            Debug.Log($"[MenuXPManager] Start called on GameObject: {gameObject.name}");
            
            // Ensure we start with the correct persistent XP data
            SyncWithPersistentXPManager();
            
            InitializeUI();
            
            // FIXED: Always check for session XP immediately, regardless of autoShowOnStart
            ProcessSessionXPImmediately();
            
            if (autoShowOnStart)
            {
                Debug.Log($"[MenuXPManager] autoShowOnStart is true, starting delayed coroutine");
                StartCoroutine(AutoShowAfterDelay());
            }
            else
            {
                Debug.Log($"[MenuXPManager] autoShowOnStart is false, skipping delayed coroutine");
            }
        }
        
        /// <summary>
        /// Sync MenuXPManager data with PersistentXPManager on start
        /// </summary>
        private void SyncWithPersistentXPManager()
        {
            if (PersistentXPManager.Instance != null)
            {
                var progressData = PersistentXPManager.Instance.GetXPProgressData();
                currentLevel = progressData.currentLevel;
                currentXP = progressData.currentXP;
                Debug.Log($"MenuXPManager: Synced with PersistentXPManager - Starting with Level {currentLevel}, XP {currentXP}");
            }
            else
            {
                Debug.LogWarning("MenuXPManager: PersistentXPManager not found during sync!");
            }
        }
        
        /// <summary>
        /// Process any pending session XP from PersistentXPManager when menu loads
        /// </summary>
        private void ProcessSessionXPImmediately()
        {
            if (PersistentXPManager.Instance == null)
            {
                Debug.LogWarning("MenuXPManager: PersistentXPManager not found!");
                return;
            }
            
            int sessionXP = PersistentXPManager.Instance.SessionXP;
            Debug.Log($"MenuXPManager: IMMEDIATE CHECK - SessionXP from PersistentXPManager = {sessionXP}");
            
            if (sessionXP > 0)
            {
                Debug.Log($"MenuXPManager: IMMEDIATE PROCESSING {sessionXP} session XP. Current Level: {currentLevel}, Current XP: {currentXP}");

                // Get current persistent data from PersistentXPManager
                var progressData = PersistentXPManager.Instance.GetXPProgressData();
                int oldLevel = progressData.currentLevel;
                int oldXP = progressData.currentXP;

                // Process the session XP through PersistentXPManager
                int processedXP = PersistentXPManager.Instance.ProcessSessionXP();

                CompanionCore.AwardXPToSessionCompanions(processedXP);

                // Update local data after processing
                // Get updated data after processing
                progressData = PersistentXPManager.Instance.GetXPProgressData();
                
                // Temporarily revert for animation
                currentXP -= processedXP;
                currentLevel = GetLevelFromXP(currentXP);
                
                Debug.Log($"[MenuXPManager] Temporarily reverted for animation. Level: {currentLevel}, XP: {currentXP}");
                UpdateLevelUI();
                UpdateXPBar();
                
                // Store for animation
                sessionXPToAdd = processedXP;
            }
            else
            {
                Debug.Log("MenuXPManager: No session XP to process immediately");
                sessionXPToAdd = 0;
                
                // Still sync with PersistentXPManager data
                if (PersistentXPManager.Instance != null)
                {
                    var progressData = PersistentXPManager.Instance.GetXPProgressData();
                    currentLevel = progressData.currentLevel;
                    currentXP = progressData.currentXP;
                    UpdateLevelUI();
                    UpdateXPBar();
                }
            }
        }
        
        /// <summary>
        /// Add XP directly to persistent data and handle leveling
        /// </summary>
        private void AddXPToPersistentData(int xpToAdd)
        {
            if (xpToAdd <= 0) return;
            
            int oldLevel = currentLevel;
            currentXP += xpToAdd;
            
            // Check for level ups
            int newLevel = GetLevelFromXP(currentXP);
            if (newLevel > currentLevel)
            {
                currentLevel = newLevel;
                Debug.Log($"MenuXPManager: LEVEL UP! {oldLevel} → {currentLevel}");
                OnLevelUp?.Invoke(oldLevel, currentLevel);
            }
            
            // Save immediately
            SavePersistentData();
            
            Debug.Log($"MenuXPManager: Added {xpToAdd} XP. New totals - Level: {currentLevel}, XP: {currentXP}");
        }
        
        private IEnumerator AutoShowAfterDelay()
        {
            yield return new WaitForSeconds(autoShowDelay);
            
            // Check PersistentXPManager for session XP
            if (PersistentXPManager.Instance != null && sessionXPToAdd > 0)
            {
                Debug.Log($"MenuXPManager: Processing {sessionXPToAdd} session XP animation");
                ShowXPProgressWithNewXP(sessionXPToAdd);
            }
            else
            {
                Debug.Log("MenuXPManager: No session XP to animate, showing current progress");
                ShowXPProgress();
            }
        }
        
        private void LoadPersistentData()
        {
            // NEW FLOW: Get data from PersistentXPManager instead of PlayerPrefs
            if (PersistentXPManager.Instance != null)
            {
                var progressData = PersistentXPManager.Instance.GetXPProgressData();
                currentLevel = progressData.currentLevel;
                currentXP = progressData.currentXP;
                Debug.Log($"MenuXPManager: Data loaded from PersistentXPManager - Level: {currentLevel}, XP: {currentXP}");
            }
            else
            {
                // Fallback to PlayerPrefs if PersistentXPManager not available
                currentLevel = PlayerPrefs.GetInt("PlayerLevel", 1);
                currentXP = PlayerPrefs.GetInt("PersistentXP", 0);
                Debug.Log($"MenuXPManager: Fallback data from PlayerPrefs - Level: {currentLevel}, XP: {currentXP}");
            }
            
            // Ensure level matches XP (in case of data corruption)
            ValidateLevelData();
            
            Debug.Log($"MenuXPManager: FINAL DATA AFTER VALIDATION - Level: {currentLevel}, XP: {currentXP}");
        }
        
        private void SavePersistentData()
        {
            // NEW FLOW: Sync data to PersistentXPManager instead of PlayerPrefs
            if (PersistentXPManager.Instance != null)
            {
                // PersistentXPManager handles all saving automatically
                Debug.Log($"MenuXPManager: Data synced with PersistentXPManager - Level {currentLevel} with {currentXP} XP");
            }
            else
            {
                // Fallback to PlayerPrefs if PersistentXPManager not available
                if (currentLevel < 1) currentLevel = 1;
                if (currentXP < 0) currentXP = 0;
                
                PlayerPrefs.SetInt("PlayerLevel", currentLevel);
                PlayerPrefs.SetInt("PersistentXP", currentXP);
                PlayerPrefs.Save();
                
                Debug.Log($"MenuXPManager: Fallback saved to PlayerPrefs - Level {currentLevel} with {currentXP} XP");
            }
        }
        
        private void ValidateLevelData()
        {
            // Ensure current level matches current XP
            int correctLevel = GetLevelFromXP(currentXP);
            Debug.Log($"MenuXPManager: Validation check - Current Level: {currentLevel}, XP: {currentXP}, Calculated Level from XP: {correctLevel}");
            
            if (correctLevel != currentLevel)
            {
                Debug.LogWarning($"MenuXPManager: Level mismatch! XP suggests level {correctLevel} but saved level is {currentLevel}. Correcting...");
                currentLevel = correctLevel;
                // SAVE the corrected data immediately
                SavePersistentData();
            }
        }
        
        private void InitializeUI()
        {
            Debug.Log($"[MenuXPManager] InitializeUI called. GameObject: {gameObject.name}, xpProgressPanel: {(xpProgressPanel != null ? xpProgressPanel.name : "NULL")}");
            
            // FIXED: Don't disable the panel if MenuXPManager is attached to it
            if (xpProgressPanel != null && xpProgressPanel != gameObject)
            {
                Debug.Log($"[MenuXPManager] Disabling separate XP progress panel: {xpProgressPanel.name}");
                xpProgressPanel.SetActive(false);
            }
            else if (xpProgressPanel == gameObject)
            {
                Debug.LogWarning($"[MenuXPManager] MenuXPManager is attached to the XP progress panel itself! Not disabling to prevent coroutine issues.");
                // Keep the panel active since MenuXPManager is on it
            }
            
            UpdateLevelUI();
            UpdateXPBar();
        }
        
        /// <summary>
        /// Show XP progress without adding new XP
        /// </summary>
        public void ShowXPProgress()
        {
            if (xpProgressPanel != null)
                xpProgressPanel.SetActive(true);
            
            UpdateLevelUI();
            UpdateXPBar();
            
            if (newXPText != null)
                newXPText.gameObject.SetActive(false);
        }
        
        /// <summary>
        /// Show XP progress and animate adding new XP from last session
        /// </summary>
        public void ShowXPProgressWithNewXP(int newXP)
        {
            if (newXP <= 0)
            {
                ShowXPProgress();
                return;
            }
            
            sessionXPToAdd = newXP;
            
            if (xpProgressPanel != null)
                xpProgressPanel.SetActive(true);
            
            UpdateLevelUI();
            UpdateXPBar();
            
            if (newXPText != null)
            {
                newXPText.text = $"+{newXP} XP";
                newXPText.gameObject.SetActive(true);
            }
            
            StartCoroutine(AnimateXPGain(newXP));
        }
        
        private IEnumerator AnimateXPGain(int xpToAdd)
        {
            isAnimating = true;
            int startingXP = currentXP;
            int targetXP = currentXP + xpToAdd;
            int startingLevel = currentLevel;
            float currentPitch = basePitch; // Start at base pitch
            
            // Animate XP gain at original speed with frequent sound effects
            while (currentXP < targetXP)
            {
                int xpToAddThisFrame = Mathf.RoundToInt(xpFillSpeed * Time.deltaTime);
                xpToAddThisFrame = Mathf.Min(xpToAddThisFrame, targetXP - currentXP);
                
                currentXP += xpToAddThisFrame;
                
                // Play XP gain sound with incrementally rising pitch (every frame when adding XP)
                if (xpGainSound != null && xpToAddThisFrame > 0)
                {
                    // Each XP sound is slightly higher pitch than the previous one
                    for (int i = 0; i < xpToAddThisFrame; i++)
                    {
                        PlaySoundWithPitch(xpGainSound, audioVolume * 0.3f, currentPitch);
                        currentPitch += pitchIncrement;
                        
                        // Cap the pitch at maxPitch
                        if (currentPitch > maxPitch)
                            currentPitch = maxPitch;
                    }
                }
                
                // Check for level up
                int newLevel = GetLevelFromXP(currentXP);
                if (newLevel > currentLevel)
                {
                    // Level up occurred!
                    int oldLevel = currentLevel;
                    currentLevel = newLevel;
                    
                    // Play level up effects
                    yield return StartCoroutine(HandleLevelUp(oldLevel, currentLevel));
                }
                
                UpdateLevelUI();
                UpdateXPBar();
                
                yield return null;
            }
            
            // Ensure final values are correct and sync with PersistentXPManager
            if (PersistentXPManager.Instance != null)
            {
                var finalData = PersistentXPManager.Instance.GetXPProgressData();
                currentXP = finalData.currentXP;
                currentLevel = finalData.currentLevel;
            }
            else
            {
                currentXP = targetXP;
                currentLevel = GetLevelFromXP(currentXP);
            }
            
            UpdateLevelUI();
            UpdateXPBar();
            
            // Hide new XP text
            if (newXPText != null)
                newXPText.gameObject.SetActive(false);
            
            // Save progress - PersistentXPManager already saved during ProcessSessionXP()
            // But save local MenuXPManager data for consistency
            SavePersistentData();
            
            isAnimating = false;
            sessionXPToAdd = 0;
            
            OnXPChanged?.Invoke(currentXP);
        }
        
        private IEnumerator HandleLevelUp(int oldLevel, int newLevel)
        {
            // Play level up sound
            PlaySound(levelUpSound, audioVolume);
            
            // Show level up effect
            if (levelUpEffect != null)
            {
                levelUpEffect.SetActive(true);
            }
            
            // Fire level up event
            OnLevelUp?.Invoke(oldLevel, newLevel);
            
            // Handle level rewards
            if (enableLevelRewards)
            {
                GrantLevelRewards(newLevel);
            }
            
            // Pause to let player see the level up
            yield return new WaitForSeconds(levelUpPauseDuration);
            
            // Hide level up effect
            if (levelUpEffect != null)
            {
                levelUpEffect.SetActive(false);
            }
        }
        
        private void GrantLevelRewards(int level)
        {
            int gemsToGrant = gemsRewardPerLevel;
            
            // Check for special level rewards
            if (Array.IndexOf(specialLevelRewards, level) >= 0)
            {
                gemsToGrant += specialRewardGems;
                Debug.Log($"Special level reward! Extra {specialRewardGems} gems for reaching level {level}");
            }
            
            // Grant gems (you'll need to integrate with your gem system)
            // For now, just log the reward
            Debug.Log($"Level {level} reward: {gemsToGrant} gems granted!");
            
            // TODO: Integrate with PlayerProgression or InventoryManager to actually grant gems
        }
        
        private void UpdateLevelUI()
        {
            Debug.Log($"[MenuXPManager] UpdateLevelUI called. levelText is {(levelText != null ? "assigned" : "NULL")}");
            
            if (levelText != null)
            {
                string newText;
                if (IsMaxLevel)
                    newText = $"Level {currentLevel} (MAX)";
                else
                    newText = $"Level {currentLevel}";
                    
                levelText.text = newText;
                Debug.Log($"[MenuXPManager] Level text updated to: {newText}");
            }
            else
            {
                Debug.LogWarning($"[MenuXPManager] levelText is NULL! Cannot update level UI. Current level: {currentLevel}");
            }
        }
        
        private void UpdateXPBar()
        {
            Debug.Log($"[MenuXPManager] UpdateXPBar called. xpProgressBar is {(xpProgressBar != null ? "assigned" : "NULL")}, xpText is {(xpText != null ? "assigned" : "NULL")}");
            
            if (xpProgressBar != null)
            {
                if (IsMaxLevel)
                {
                    xpProgressBar.value = 1f;
                    Debug.Log($"[MenuXPManager] XP bar set to max (1.0) for max level");
                }
                else
                {
                    int xpForCurrentLevel = XPRequiredForCurrentLevel;
                    int xpForNextLevel = XPRequiredForNextLevel;
                    int xpInCurrentLevel = currentXP - xpForCurrentLevel;
                    int xpNeededForLevel = xpForNextLevel - xpForCurrentLevel;
                    
                    float barValue = (float)xpInCurrentLevel / xpNeededForLevel;
                    xpProgressBar.value = barValue;
                    Debug.Log($"[MenuXPManager] XP bar updated: {xpInCurrentLevel}/{xpNeededForLevel} = {barValue:F2}");
                }
            }
            else
            {
                Debug.LogWarning($"[MenuXPManager] xpProgressBar is NULL! Cannot update XP bar.");
            }
            
            if (xpText != null)
            {
                string newXPText;
                if (IsMaxLevel)
                {
                    newXPText = $"XP: {currentXP} (MAX LEVEL)";
                }
                else
                {
                    int xpForCurrentLevel = XPRequiredForCurrentLevel;
                    int xpForNextLevel = XPRequiredForNextLevel;
                    int xpInCurrentLevel = currentXP - xpForCurrentLevel;
                    int xpNeededForLevel = xpForNextLevel - xpForCurrentLevel;
                    
                    newXPText = $"XP: {xpInCurrentLevel}/{xpNeededForLevel}";
                }
                xpText.text = newXPText;
                Debug.Log($"[MenuXPManager] XP text updated to: {newXPText}");
            }
            else
            {
                Debug.LogWarning($"[MenuXPManager] xpText is NULL! Cannot update XP text. Current XP: {currentXP}");
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
        
        private void PlaySound(AudioClip clip, float volume)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip, volume);
            }
            else if (audioSource == null)
            {
                Debug.LogWarning("[MenuXPManager] AudioSource is null! Please assign an AudioSource component in the inspector.");
            }
        }
        
        private void PlaySoundWithPitch(AudioClip clip, float volume, float pitch)
        {
            if (audioSource != null && clip != null)
            {
                // Store original pitch
                float originalPitch = audioSource.pitch;
                
                // Set new pitch and play
                audioSource.pitch = pitch;
                audioSource.PlayOneShot(clip, volume);
                
                // Reset pitch after a short delay to avoid affecting other sounds
                StartCoroutine(ResetPitchAfterDelay(originalPitch, 0.1f));
            }
            else if (audioSource == null)
            {
                Debug.LogWarning("[MenuXPManager] AudioSource is null! Please assign an AudioSource component in the inspector.");
            }
        }
        
        private IEnumerator ResetPitchAfterDelay(float originalPitch, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (audioSource != null)
            {
                audioSource.pitch = originalPitch;
            }
        }
        
        /// <summary>
        /// Hide XP progress panel
        /// </summary>
        public void HideXPProgress()
        {
            if (xpProgressPanel != null)
                xpProgressPanel.SetActive(false);
        }
        
        /// <summary>
        /// Admin method to reset all XP data
        /// </summary>
        public void DEBUG_ResetAllXP()
        {
            currentLevel = 1;
            currentXP = 0;
            SavePersistentData();
            UpdateLevelUI();
            UpdateXPBar();
            Debug.Log("MenuXPManager: All XP data reset to level 1");
        }
        
        /// <summary>
        /// Admin method to add XP directly
        /// </summary>
        public void DEBUG_AddXP(int amount)
        {
            ShowXPProgressWithNewXP(amount);
        }
        
        void OnDestroy()
        {
            // Clear the static instance when this object is destroyed
            if (Instance == this)
            {
                Instance = null;
                Debug.Log("[MenuXPManager] Instance cleared on destroy");
            }
        }
    }
}