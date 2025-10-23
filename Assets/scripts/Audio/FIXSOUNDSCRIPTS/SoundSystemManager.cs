using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

namespace GeminiGauntlet.Audio
{
    /// <summary>
    /// Centralized Sound System Manager - Handles initialization order and dependencies
    /// Replaces fragile circular dependencies with proper initialization flow
    /// </summary>
    public class SoundSystemManager : MonoBehaviour
    {
        public static SoundSystemManager Instance { get; private set; }
        
        [Header("=== INITIALIZATION ORDER ===")]
        [SerializeField] private bool initializeOnAwake = true;
        [SerializeField] private bool debugInitialization = true;
        
        [Header("=== CORE SYSTEMS ===")]
        [SerializeField] private SoundEvents soundEventsAsset;
        [SerializeField] private AudioMixerGroup masterMixerGroup;
        [SerializeField] private AudioMixerGroup sfxMixerGroup;
        [SerializeField] private AudioMixerGroup musicMixerGroup;
        [SerializeField] private AudioMixerGroup ambientMixerGroup;
        [SerializeField] private AudioMixerGroup uiMixerGroup;
        
        [Header("=== POOL SETTINGS ===")]
        [SerializeField] private int maxConcurrentSounds = 256;  // Increased from 32 - handles 100+ skulls + rapid shotgun fire + other sounds
        [SerializeField] private int poolInitialSize = 128;      // Increased from 16 - pre-allocate more sources
        
        // System State
        private bool isInitialized = false;
        private SoundSystemCore soundCore;
        private SoundEventsManager eventsManager;
        
        // Initialization validation
        private readonly List<string> initializationErrors = new List<string>();
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                
                if (initializeOnAwake)
                {
                    InitializeSoundSystem();
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// Initialize sound system with proper dependency order
        /// </summary>
        public bool InitializeSoundSystem()
        {
            if (isInitialized)
            {
                if (debugInitialization) Debug.Log("🔊 SoundSystemManager: Already initialized");
                return true;
            }
            
            initializationErrors.Clear();
            
            if (debugInitialization) Debug.Log("🔊 SoundSystemManager: Starting initialization...");
            
            // Step 1: Validate required assets
            if (!ValidateRequiredAssets())
            {
                LogInitializationErrors();
                return false;
            }
            
            // Step 2: Initialize SoundSystemCore
            if (!InitializeSoundCore())
            {
                LogInitializationErrors();
                return false;
            }
            
            // Step 3: Initialize SoundEventsManager
            if (!InitializeEventsManager())
            {
                LogInitializationErrors();
                return false;
            }
            
            // Step 4: Validate system integration
            if (!ValidateSystemIntegration())
            {
                LogInitializationErrors();
                return false;
            }
            
            isInitialized = true;
            if (debugInitialization) Debug.Log("✅ SoundSystemManager: Initialization complete!");
            return true;
        }
        
        private bool ValidateRequiredAssets()
        {
            bool valid = true;
            
            if (soundEventsAsset == null)
            {
                initializationErrors.Add("SoundEvents asset is not assigned");
                valid = false;
            }
            
            if (masterMixerGroup == null)
            {
                initializationErrors.Add("Master AudioMixerGroup is not assigned");
                valid = false;
            }
            
            if (sfxMixerGroup == null)
            {
                initializationErrors.Add("SFX AudioMixerGroup is not assigned");
                valid = false;
            }
            
            return valid;
        }
        
        private bool InitializeSoundCore()
        {
            // Find or create SoundSystemCore
            soundCore = FindObjectOfType<SoundSystemCore>();
            if (soundCore == null)
            {
                GameObject coreGO = new GameObject("SoundSystemCore");
                coreGO.transform.SetParent(transform);
                soundCore = coreGO.AddComponent<SoundSystemCore>();
            }
            
            // Initialize with mixer groups
            try
            {
                soundCore.Initialize(
                    masterMixerGroup, sfxMixerGroup, musicMixerGroup,
                    ambientMixerGroup, uiMixerGroup, maxConcurrentSounds
                );
                
                if (debugInitialization) Debug.Log("✅ SoundSystemCore initialized");
                return true;
            }
            catch (System.Exception e)
            {
                initializationErrors.Add($"SoundSystemCore initialization failed: {e.Message}");
                return false;
            }
        }
        
        private bool InitializeEventsManager()
        {
            // Find or create SoundEventsManager
            eventsManager = FindObjectOfType<SoundEventsManager>();
            if (eventsManager == null)
            {
                GameObject managerGO = new GameObject("SoundEventsManager");
                managerGO.transform.SetParent(transform);
                eventsManager = managerGO.AddComponent<SoundEventsManager>();
            }
            
            // Register sound events asset
            try
            {
                SoundEventsManager.RegisterSoundEvents(soundEventsAsset);
                
                if (debugInitialization) Debug.Log("✅ SoundEventsManager initialized");
                return true;
            }
            catch (System.Exception e)
            {
                initializationErrors.Add($"SoundEventsManager initialization failed: {e.Message}");
                return false;
            }
        }
        
        private bool ValidateSystemIntegration()
        {
            // Validate SoundSystemCore
            if (SoundSystemCore.Instance == null)
            {
                initializationErrors.Add("SoundSystemCore.Instance is null after initialization");
                return false;
            }
            
            // Validate SoundEventsManager
            if (SoundEventsManager.Instance == null)
            {
                initializationErrors.Add("SoundEventsManager.Instance is null after initialization");
                return false;
            }
            
            // Validate Events asset
            if (SoundEventsManager.Events == null)
            {
                initializationErrors.Add("SoundEventsManager.Events is null after initialization");
                return false;
            }
            
            return true;
        }
        
        private void LogInitializationErrors()
        {
            Debug.LogError("❌ SoundSystemManager: Initialization failed!");
            foreach (string error in initializationErrors)
            {
                Debug.LogError($"   • {error}");
            }
        }
        
        /// <summary>
        /// Check if sound system is properly initialized
        /// </summary>
        public bool IsInitialized => isInitialized;
        
        /// <summary>
        /// Get initialization errors (for debugging)
        /// </summary>
        public List<string> GetInitializationErrors() => new List<string>(initializationErrors);
        
        /// <summary>
        /// Force re-initialization (for debugging/recovery)
        /// </summary>
        [ContextMenu("Force Re-Initialize")]
        public void ForceReInitialize()
        {
            isInitialized = false;
            InitializeSoundSystem();
        }
        
        /// <summary>
        /// Test sound system functionality
        /// </summary>
        [ContextMenu("Test Sound System")]
        public void TestSoundSystem()
        {
            if (!isInitialized)
            {
                Debug.LogWarning("🔊 SoundSystemManager: Cannot test - system not initialized");
                return;
            }
            
            Debug.Log("🔊 Testing sound system...");
            
            // Test through GameSounds (should work without null reference issues)
            GameSounds.PlayUIFeedback(transform.position, 0.5f);
            
            Debug.Log("✅ Sound system test completed");
        }
        
        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
