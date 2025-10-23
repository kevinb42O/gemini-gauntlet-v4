using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GeminiGauntlet.Progression;

namespace GeminiGauntlet.UI
{
    /// <summary>
    /// XP Summary UI that displays line-by-line XP breakdown after exfil.
    /// Shows animated counting sequence before transitioning to menu.
    /// </summary>
    public class XPSummaryUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject summaryPanel;
        [SerializeField] private TextMeshProUGUI[] categoryLineTexts; // Pre-placed text components for each category
        [SerializeField] private TextMeshProUGUI totalXPText; // Manually positioned total text
        [SerializeField] private GameObject dividerLine; // Optional divider you can position manually
        
        [Header("Animation Settings")]
        [SerializeField] private float delayBetweenLines = 0.8f;
        [SerializeField] private float countingSpeed = 50000f; // XP counted per second (10x faster than before - nearly instant)
        [SerializeField] private float finalPauseTime = 2f;
        [SerializeField] private bool useTypewriterEffect = true;
        [SerializeField] private float typewriterSpeed = 0.05f;
        
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource; // Panel's AudioSource component
        [SerializeField] private AudioClip lineCompleteSound;
        [SerializeField] private AudioClip totalCompleteSound;
        [SerializeField] private AudioClip xpCountingSound; // Short sound for each XP counted
        [SerializeField] private float audioVolume = 0.5f;
        [SerializeField] private float xpCountingSoundVolume = 0.3f;
        [SerializeField] private float basePitch = 0.8f; // Starting pitch for XP counting
        [SerializeField] private float pitchIncrement = 0.02f; // How much pitch increases per XP point
        [SerializeField] private float maxPitch = 1.5f; // Maximum pitch for XP counting
        
        [Header("Scene Transition")]
        [SerializeField] private string menuSceneName = "NieuweMainMenu";
        
        [Header("Player Control Disabling")]
        [SerializeField] private MonoBehaviour[] scriptsToDisable;
        [SerializeField] private bool pauseTimeScale = true;
        
        private XPSummaryData currentSummaryData;
        private bool isAnimating = false;
        private int currentLineIndex = 0;
        
        void Awake()
        {
            // Hide summary panel initially
            if (summaryPanel != null)
                summaryPanel.SetActive(false);
        }
        
        /// <summary>
        /// Start the XP summary sequence
        /// </summary>
        public void ShowXPSummary()
        {
            if (XPManager.Instance == null)
            {
                Debug.LogError("XPSummaryUI: XPManager.Instance is null! Cannot show XP summary.");
                GoToMenu(); // Fallback to menu
                return;
            }
            
            currentSummaryData = XPManager.Instance.GetXPSummaryData();
            
            if (currentSummaryData.sessionTotalXP <= 0)
            {
                Debug.Log("XPSummaryUI: No XP collected this session, skipping summary.");
                GoToMenu(); // Skip summary if no XP
                return;
            }
            
            Debug.Log($"XPSummaryUI: Starting summary for {currentSummaryData.sessionTotalXP} total XP");
            
            // CRITICAL FIX: Activate the GameObject so we can start coroutines
            if (!gameObject.activeInHierarchy)
            {
                Debug.Log("XPSummaryUI: Activating GameObject to enable coroutines");
                gameObject.SetActive(true);
            }
            
            // Also activate the summary panel immediately
            if (summaryPanel != null && !summaryPanel.activeSelf)
            {
                Debug.Log("XPSummaryUI: Pre-activating summary panel");
                summaryPanel.SetActive(true);
            }
            
            StartCoroutine(ShowSummarySequence());
        }
        
        private IEnumerator ShowSummarySequence()
        {
            isAnimating = true;
            
            Debug.Log("🎬 XPSummaryUI: Starting summary sequence animation");
            
            // DISABLE PLAYER CONTROLS
            DisablePlayerControls();
            
            // Show the summary panel
            if (summaryPanel != null)
            {
                summaryPanel.SetActive(true);
                Debug.Log("✅ XPSummaryUI: Summary panel activated");
            }
            else
            {
                Debug.LogError("❌ XPSummaryUI: Summary panel is null! Cannot show UI!");
                isAnimating = false;
                GoToMenu();
                yield break;
            }
            
            // Hide all category lines and total initially
            HideAllCategoryLines();
            if (dividerLine != null) dividerLine.SetActive(false);
            if (totalXPText != null) totalXPText.gameObject.SetActive(false);
            
            currentLineIndex = 0;
            
            // Show ALL categories in fixed order - never skip any!
            Debug.Log($"🔍 XPSummaryUI: Processing {currentSummaryData.categoryBreakdown.Count} categories");
            
            // Define the fixed order of categories (Missions comes last before total)
            string[] expectedCategories = { "Enemies", "Gems", "Towers", "Chests", "Platforms", "Missions" };
            
            foreach (string expectedCategory in expectedCategories)
            {
                if (currentLineIndex >= categoryLineTexts.Length) 
                {
                    Debug.LogWarning($"⚠️ Not enough category line slots for: {expectedCategory}");
                    break;
                }
                
                // Find the category data (or create empty one if not found)
                var categoryData = currentSummaryData.categoryBreakdown.FirstOrDefault(c => c.categoryName == expectedCategory);
                if (categoryData == null)
                {
                    // Create empty category data for missing categories
                    categoryData = new XPCategoryData 
                    { 
                        categoryName = expectedCategory, 
                        count = 0, 
                        totalXP = 0, 
                        xpPerItem = GetDefaultXPForCategory(expectedCategory) 
                    };
                    Debug.Log($"📝 Created empty category: {expectedCategory}");
                }
                
                Debug.Log($"🔍 Category: {categoryData.categoryName}, Count: {categoryData.count}, XP: {categoryData.totalXP}");
                Debug.Log($"✅ Showing category: {categoryData.GetDisplayName()} on line {currentLineIndex}");
                
                yield return StartCoroutine(ShowCategoryLineAtIndex(categoryData, currentLineIndex));
                currentLineIndex++;
                
                // Use unscaled time since we paused timeScale
                yield return new WaitForSecondsRealtime(delayBetweenLines);
            }
            
            // Show divider
            if (dividerLine != null) 
            {
                dividerLine.SetActive(true);
                yield return new WaitForSecondsRealtime(0.3f);
            }
            
            // Show total XP with animation
            yield return StartCoroutine(ShowTotalXP());
            
            // Pause before transitioning
            yield return new WaitForSecondsRealtime(finalPauseTime);
            
            // Clear mission session data after showing summary
            if (GeminiGauntlet.Missions.MissionManager.Instance != null)
            {
                GeminiGauntlet.Missions.MissionManager.Instance.ClearSessionData();
            }
            
            // Transition to menu
            isAnimating = false;
            GoToMenu();
        }
        
        private IEnumerator ShowCategoryLineAtIndex(XPCategoryData categoryData, int lineIndex)
        {
            if (categoryLineTexts == null || lineIndex >= categoryLineTexts.Length || categoryLineTexts[lineIndex] == null)
            {
                Debug.LogError($"XPSummaryUI: Missing category line text at index {lineIndex}!");
                yield break;
            }
            
            TextMeshProUGUI lineText = categoryLineTexts[lineIndex];
            Debug.Log($"🎭 Showing category on pre-placed line {lineIndex}: {categoryData.GetDisplayName()}");
            
            // Show the line object
            lineText.gameObject.SetActive(true);
            
            // Format the line text
            string displayText = $"{categoryData.GetDisplayName()}: {categoryData.count} x {categoryData.xpPerItem}xp = {categoryData.totalXP}xp";
            Debug.Log($"📝 Line text: {displayText}");
            
            if (useTypewriterEffect)
            {
                // Typewriter effect using unscaled time
                lineText.text = "";
                for (int i = 0; i <= displayText.Length; i++)
                {
                    lineText.text = displayText.Substring(0, i);
                    yield return new WaitForSecondsRealtime(typewriterSpeed);
                }
            }
            else
            {
                // Instant text display
                lineText.text = displayText;
                Debug.Log("✅ Text set instantly");
            }
            
            // Play completion sound
            PlaySound(lineCompleteSound);
        }
        
        private IEnumerator ShowTotalXP()
        {
            if (totalXPText == null)
            {
                Debug.LogError("XPSummaryUI: Total XP text component missing!");
                yield break;
            }
            
            totalXPText.gameObject.SetActive(true);
            
            if (useTypewriterEffect)
            {
                // Typewriter effect for "Total XP: "
                string baseText = "Total XP: ";
                totalXPText.text = "";
                
                for (int i = 0; i <= baseText.Length; i++)
                {
                    totalXPText.text = baseText.Substring(0, i);
                    yield return new WaitForSecondsRealtime(typewriterSpeed);
                }
                
                // Animated counting for the XP number
                yield return StartCoroutine(AnimateXPCounting(baseText, currentSummaryData.sessionTotalXP));
            }
            else
            {
                totalXPText.text = $"Total XP: {currentSummaryData.sessionTotalXP}xp";
            }
            
            // Play total completion sound
            PlaySound(totalCompleteSound);
        }
        
        private IEnumerator AnimateXPCounting(string baseText, int targetXP)
        {
            int currentXP = 0;
            int xpBatchSize = 50; // Count XP in batches of 50 for speed
            int totalBatches = Mathf.CeilToInt((float)targetXP / xpBatchSize);
            float delayBetweenBatches = 0.05f; // Fixed short delay between batches
            float currentPitch = basePitch; // Start at base pitch
            
            // Count XP in batches of 50 for lightning-fast counting
            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                // Calculate XP to add in this batch
                int xpInThisBatch = Mathf.Min(xpBatchSize, targetXP - currentXP);
                currentXP += xpInThisBatch;
                
                // Play sound for this batch (not for each individual XP)
                if (xpCountingSound != null)
                {
                    PlaySoundWithPitch(xpCountingSound, xpCountingSoundVolume, currentPitch);
                    currentPitch += pitchIncrement * 5f; // Increase pitch more significantly per batch
                    
                    // Cap the pitch at maxPitch
                    if (currentPitch > maxPitch)
                        currentPitch = maxPitch;
                }
                
                // Update display with new XP total
                totalXPText.text = $"{baseText}{currentXP}xp";
                
                // Wait briefly between batches for visual feedback
                if (batchIndex < totalBatches - 1)
                {
                    yield return new WaitForSecondsRealtime(delayBetweenBatches);
                }
            }
            
            // Ensure final value is correct
            totalXPText.text = $"{baseText}{targetXP}xp";
        }
        
        private void HideAllCategoryLines()
        {
            if (categoryLineTexts != null)
            {
                foreach (var lineText in categoryLineTexts)
                {
                    if (lineText != null)
                    {
                        lineText.gameObject.SetActive(false);
                        lineText.text = ""; // Clear any existing text
                    }
                }
            }
        }
        
        private void PlaySound(AudioClip clip)
        {
            if (clip != null)
            {
                if (audioSource != null)
                {
                    // Use the panel's AudioSource as requested
                    audioSource.PlayOneShot(clip, audioVolume);
                }
                else if (AudioManager.Instance != null)
                {
                    // Fallback to AudioManager if no panel AudioSource assigned
                    AudioManager.Instance.PlaySound2D(clip, audioVolume);
                    Debug.LogWarning("[XPSummaryUI] No AudioSource assigned to panel, using AudioManager fallback");
                }
            }
        }
        
        private void PlaySoundWithPitch(AudioClip clip, float volume, float pitch)
        {
            if (clip != null && audioSource != null)
            {
                // Store original pitch
                float originalPitch = audioSource.pitch;
                
                // Set new pitch and play
                audioSource.pitch = pitch;
                audioSource.PlayOneShot(clip, volume);
                
                // Reset pitch after a short delay to avoid affecting other sounds
                StartCoroutine(ResetPitchAfterDelay(originalPitch, 0.1f));
            }
            else if (clip != null)
            {
                Debug.LogWarning("[XPSummaryUI] No AudioSource assigned to panel for pitch-controlled sound");
                // Fallback without pitch control
                PlaySound(clip);
            }
        }
        
        private IEnumerator ResetPitchAfterDelay(float originalPitch, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            if (audioSource != null)
            {
                audioSource.pitch = originalPitch;
            }
        }
        
        private void GoToMenu()
        {
            // Save XP data to persistent storage before transitioning
            SaveXPDataToPersistentStorage();
            
            // Re-enable player controls before leaving (in case they stay in scene)
            EnablePlayerControls();
            
            // Reset cursor state
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            // Load menu scene
            Debug.Log("XPSummaryUI: Transitioning to menu...");
            UnityEngine.SceneManagement.SceneManager.LoadScene(menuSceneName);
        }
        
        private void SaveXPDataToPersistentStorage()
        {
            if (currentSummaryData == null) return;
            
            // FIXED: Only save session XP, let MenuXPManager handle PersistentXP
            PlayerPrefs.SetInt("LastSessionXP", currentSummaryData.sessionTotalXP);
            
            // Save category breakdown for potential use in menu
            foreach (var categoryData in currentSummaryData.categoryBreakdown)
            {
                string categoryKey = $"Session_{categoryData.categoryName}";
                PlayerPrefs.SetInt($"{categoryKey}_Count", categoryData.count);
                PlayerPrefs.SetInt($"{categoryKey}_XP", categoryData.totalXP);
            }
            
            PlayerPrefs.Save();
            Debug.Log($"XPSummaryUI: Saved {currentSummaryData.sessionTotalXP} session XP (MenuXPManager will add to persistent XP)");
        }
        
        /// <summary>
        /// Skip animation and go directly to menu (for testing or emergency)
        /// </summary>
        public void SkipToMenu()
        {
            if (isAnimating)
            {
                StopAllCoroutines();
                isAnimating = false;
            }
            GoToMenu();
        }
        
        void Update()
        {
            // Allow skip with ESC key (optional)
            if (Input.GetKeyDown(KeyCode.Escape) && isAnimating)
            {
                SkipToMenu();
            }
        }
        
        private void DisablePlayerControls()
        {
            Debug.Log("🚫 XPSummaryUI: Disabling player controls");
            
            // Disable all assigned scripts
            if (scriptsToDisable != null)
            {
                foreach (var script in scriptsToDisable)
                {
                    if (script != null)
                    {
                        script.enabled = false;
                        Debug.Log($"✅ {script.GetType().Name} disabled");
                    }
                }
            }
            
            // ⭐ FIX: Disable ALL player movement components (CORRECT ORDER!)
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                // STEP 1: Disable scripts that depend on CharacterController/Rigidbody FIRST
                var aaaMovement = player.GetComponent<AAAMovementController>();
                if (aaaMovement != null) aaaMovement.enabled = false;
                
                // CleanAAAMovementController removed - using AAAMovementController only
                
                var aaaCrouch = player.GetComponent<CleanAAACrouch>();
                if (aaaCrouch != null) aaaCrouch.enabled = false;
                
                var fallingDamage = player.GetComponent<FallingDamageSystem>();
                if (fallingDamage != null) fallingDamage.enabled = false;
                
                var celestialDrift = player.GetComponent<CelestialDriftController>();
                if (celestialDrift != null) celestialDrift.enabled = false;
                
                // STEP 2: Now disable CharacterController and Rigidbody
                var characterController = player.GetComponent<CharacterController>();
                if (characterController != null)
                {
                    characterController.enabled = false;
                    Debug.Log("🛑 CharacterController disabled");
                }
                
                var rigidbody = player.GetComponent<Rigidbody>();
                if (rigidbody != null)
                {
                    rigidbody.isKinematic = true;
                    rigidbody.linearVelocity = Vector3.zero;
                    rigidbody.angularVelocity = Vector3.zero;
                    Debug.Log("🛑 Rigidbody frozen - no floating!");
                }
            }
            
            // Pause the game time to stop all game mechanics
            if (pauseTimeScale)
            {
                Time.timeScale = 0f;
                Debug.Log("⏸️ Game time paused (timeScale = 0)");
            }
        }
        
        private int GetDefaultXPForCategory(string categoryName)
        {
            // Return default XP values for each category
            switch (categoryName.ToLower())
            {
                case "enemies": return 10;
                case "gems": return 10;
                case "collectible": return 10; // Legacy support
                case "towers": return 50;
                case "chests": return 25;
                case "platforms": return 100;
                case "missions": return 100; // Mission completion XP
                default: return 10;
            }
        }
        
        private void EnablePlayerControls()
        {
            Debug.Log("✅ XPSummaryUI: Re-enabling player controls");
            
            // Re-enable all assigned scripts
            if (scriptsToDisable != null)
            {
                foreach (var script in scriptsToDisable)
                {
                    if (script != null)
                    {
                        script.enabled = true;
                        Debug.Log($"✅ {script.GetType().Name} re-enabled");
                    }
                }
            }
            
            // ⭐ FIX: Re-enable ALL player movement components (REVERSE ORDER!)
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                // STEP 1: Re-enable CharacterController and Rigidbody FIRST
                var characterController = player.GetComponent<CharacterController>();
                if (characterController != null)
                {
                    characterController.enabled = true;
                    Debug.Log("▶️ CharacterController enabled");
                }
                
                var rigidbody = player.GetComponent<Rigidbody>();
                if (rigidbody != null)
                {
                    rigidbody.isKinematic = false;
                    Debug.Log("▶️ Rigidbody unfrozen - movement restored!");
                }
                
                // STEP 2: Then re-enable scripts that depend on them
                var aaaMovement = player.GetComponent<AAAMovementController>();
                if (aaaMovement != null) aaaMovement.enabled = true;
                
                // CleanAAAMovementController removed - using AAAMovementController only
                
                var aaaCrouch = player.GetComponent<CleanAAACrouch>();
                if (aaaCrouch != null) aaaCrouch.enabled = true;
                
                var fallingDamage = player.GetComponent<FallingDamageSystem>();
                if (fallingDamage != null) fallingDamage.enabled = true;
                
                var celestialDrift = player.GetComponent<CelestialDriftController>();
                if (celestialDrift != null) celestialDrift.enabled = true;
            }
            
            // Resume game time
            if (pauseTimeScale)
            {
                Time.timeScale = 1f;
                Debug.Log("▶️ Game time resumed (timeScale = 1)");
            }
        }
    }
}