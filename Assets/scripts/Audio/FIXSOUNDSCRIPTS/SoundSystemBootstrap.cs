using UnityEngine;
using UnityEngine.Audio;

namespace GeminiGauntlet.Audio
{
    /// <summary>
    /// Bootstrap component that initializes the AAA Sound System
    /// Place this on a GameObject in your first scene or make it a prefab
    /// </summary>
    public class SoundSystemBootstrap : MonoBehaviour
    {
        [Header("=== SOUND SYSTEM SETUP ===")]
        [SerializeField] private bool initializeOnAwake = true;
        [SerializeField] private bool dontDestroyOnLoad = true;
        
        [Header("=== SOUND SYSTEM CORE SETTINGS ===")]
        [SerializeField] private int maxConcurrentSounds = 64; // Give control here
        
        [Header("=== AUDIO MIXER REFERENCES ===")]
        [Tooltip("Main AudioMixer asset - should contain all mixer groups")]
        [SerializeField] private AudioMixer mainAudioMixer;
        
        [Header("=== MIXER GROUP NAMES ===")]
        [Tooltip("Names of mixer groups in your AudioMixer - EXACTLY 5 GROUPS")]
        [SerializeField] private string masterGroupName = "Master";
        [SerializeField] private string sfxGroupName = "SFX";
        [SerializeField] private string musicGroupName = "Music";
        [SerializeField] private string ambientGroupName = "Ambient";
        [SerializeField] private string uiGroupName = "UI";

        [Header("=== SOUND EVENTS DATABASE ===")]
        [Tooltip("Reference to your SoundEvents ScriptableObject")]
        [SerializeField] private SoundEvents soundEventsDatabase;

        void Awake()
        {
            if (initializeOnAwake)
            {
                InitializeSoundSystem();
            }
        }

        [ContextMenu("Initialize Sound System")]
        public void InitializeSoundSystem()
        {
            // Check if already exists
            if (SoundSystemCore.Instance != null)
            {
                Debug.LogWarning("SoundSystemBootstrap: Sound system already exists!");
                return;
            }

            // Create the sound system core
            GameObject soundSystemGO = new GameObject("AAA_SoundSystem");
            
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(soundSystemGO);
            }

            // Add the core component
            SoundSystemCore soundCore = soundSystemGO.AddComponent<SoundSystemCore>();
            
            // --- NEW INITIALIZATION LOGIC ---
            if (mainAudioMixer != null)
            {
                // Get mixer groups by name
                AudioMixerGroup[] groups = mainAudioMixer.FindMatchingGroups(string.Empty);
                
                AudioMixerGroup masterGroup = FindGroupByName(groups, masterGroupName);
                AudioMixerGroup sfxGroup = FindGroupByName(groups, sfxGroupName);
                AudioMixerGroup musicGroup = FindGroupByName(groups, musicGroupName);
                AudioMixerGroup ambientGroup = FindGroupByName(groups, ambientGroupName);
                AudioMixerGroup uiGroup = FindGroupByName(groups, uiGroupName);

                // Call the new public initializer with all the data
                soundCore.Initialize(
                    masterGroup, 
                    sfxGroup ?? masterGroup, // Fallback to master if not found
                    musicGroup ?? masterGroup,
                    ambientGroup ?? masterGroup,
                    uiGroup ?? masterGroup,
                    maxConcurrentSounds
                );
                Debug.Log("SoundSystemBootstrap: AudioMixer groups configured successfully.");
            }
            else
            {
                // Initialize without mixer groups
                soundCore.Initialize(null, null, null, null, null, maxConcurrentSounds);
                Debug.LogWarning("SoundSystemBootstrap: No AudioMixer assigned. Sound system will work but without mixer group routing.");
            }
            
            // REMOVE THE OLD ConfigureMixerGroups call and the method itself, and SetPrivateField.
            // They are no longer needed.

            // Register sound events database (this part was fine)
            if (soundEventsDatabase != null)
            {
                SoundEventsManager.RegisterSoundEvents(soundEventsDatabase);
            }
            else
            {
                Debug.LogWarning("SoundSystemBootstrap: No SoundEvents database assigned.");
            }

            Debug.Log("SoundSystemBootstrap: AAA Sound System initialization complete!");
        }

        // REMOVED ConfigureMixerGroups() and SetPrivateField() methods entirely.
        // They are no longer needed with the new direct Initialize approach.

        private AudioMixerGroup FindGroupByName(AudioMixerGroup[] groups, string name)
        {
            foreach (var group in groups)
            {
                if (group.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return group;
                }
            }
            
            Debug.LogWarning($"SoundSystemBootstrap: AudioMixerGroup '{name}' not found in mixer.");
            return null;
        }

        // Public utility methods for runtime control
        // DISABLED: Volume control methods removed per user request - mixer volumes should NEVER be changed in code
        // Users should set their preferred volumes in the Unity Editor mixer settings
        
        /* 
        public void SetMasterVolume(float volume)
        {
            if (mainAudioMixer != null)
            {
                mainAudioMixer.SetFloat("MasterVolume", Mathf.Log10(Mathf.Clamp(volume, 0.001f, 1f)) * 20f);
            }
        }

        public void SetSFXVolume(float volume)
        {
            if (mainAudioMixer != null)
            {
                mainAudioMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Clamp(volume, 0.001f, 1f)) * 20f);
            }
        }

        public void SetMusicVolume(float volume)
        {
            if (mainAudioMixer != null)
            {
                mainAudioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Clamp(volume, 0.001f, 1f)) * 20f);
            }
        }
        */

        [ContextMenu("Test Sound System")]
        public void TestSoundSystem()
        {
            if (SoundSystemCore.Instance == null)
            {
                Debug.LogError("SoundSystemBootstrap: Sound system not initialized!");
                return;
            }

            Debug.Log($"Sound System Status:");
            Debug.Log($"- Active Sounds: {SoundSystemCore.Instance.GetActiveSoundCount()}");
            Debug.Log($"- Available Sources: {SoundSystemCore.Instance.GetAvailableSourceCount()}");
            
            // Test basic functionality if we have sound events
            if (soundEventsDatabase != null && SoundEventsManager.Instance != null)
            {
                Debug.Log("Sound Events Database is available - ready for game sounds!");
            }
        }
        
        // Start() intentionally left empty so that scenes do not auto-play test sounds.
        // Use the context menu actions or diagnostics methods when you explicitly need them.
    }
}
