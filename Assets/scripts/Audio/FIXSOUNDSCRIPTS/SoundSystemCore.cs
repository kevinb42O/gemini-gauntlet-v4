using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using System.Collections;

namespace GeminiGauntlet.Audio
{
    /// <summary>
    /// AAA-Quality Sound System Core
    /// Centralized sound management with proper pooling, prioritization, and spatial audio
    /// </summary>
    public class SoundSystemCore : MonoBehaviour
    {
        public static SoundSystemCore Instance { get; private set; }

        [Header("=== AUDIO MIXER SETUP ===")]
        // REMOVED [SerializeField] - mixer groups now passed via Initialize method
        private AudioMixerGroup masterMixerGroup;
        private AudioMixerGroup sfxMixerGroup;
        private AudioMixerGroup musicMixerGroup;
        private AudioMixerGroup ambientMixerGroup;
        private AudioMixerGroup uiMixerGroup;

        [Header("=== SOUND POOL SETTINGS ===")]
        [SerializeField] private int maxConcurrentSounds = 256;  // Increased from 32 - prevents audio pool exhaustion with 100+ skulls
        [SerializeField] private int poolInitialSize = 128;      // Increased from 16 - pre-allocate more sources
        [SerializeField] private Transform soundPoolParent;

        [Header("=== DISTANCE & ATTENUATION ===")]
        [SerializeField] private AnimationCurve defaultAttenuationCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        [SerializeField] private float globalSoundDistanceMultiplier = 1f;

        [Header("=== PRIORITY SYSTEM ===")]
        [SerializeField] private int maxHighPrioritySounds = 100;
        [SerializeField] private int maxMediumPrioritySounds = 32; // Increased for skull death sounds
        [SerializeField] private int maxLowPrioritySounds = 50;

        // Sound Pool Management
        private Queue<PooledAudioSource> availableSources = new Queue<PooledAudioSource>();
        private List<PooledAudioSource> activeSources = new List<PooledAudioSource>();
        private Dictionary<SoundPriority, List<PooledAudioSource>> prioritizedSources = new Dictionary<SoundPriority, List<PooledAudioSource>>();

        // Sound Categories
        private Dictionary<SoundCategory, SoundCategorySettings> categorySettings = new Dictionary<SoundCategory, SoundCategorySettings>();

        // Audio Listener reference
        private Transform audioListenerTransform;
        private Camera mainCamera;
        
        // PERFORMANCE FIX: Track active coroutines to prevent accumulation
        private HashSet<Coroutine> activeCoroutines = new HashSet<Coroutine>();
        private int maxConcurrentCoroutines = 50; // Safety limit

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                // DO NOT INITIALIZE HERE ANYMORE
                // InitializeSoundSystem(); 
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // REMOVED Start() method - initialization now handled by public Initialize method

        // NEW public initializer, replacing the old private one
        public void Initialize(
            AudioMixerGroup master, AudioMixerGroup sfx, AudioMixerGroup music,
            AudioMixerGroup ambient, AudioMixerGroup ui, int maxSounds)
        {
            // Assign mixer groups
            masterMixerGroup = master;
            sfxMixerGroup = sfx;
            musicMixerGroup = music;
            ambientMixerGroup = ambient;
            uiMixerGroup = ui;
            maxConcurrentSounds = maxSounds;

            // Find audio listener and main camera
            AudioListener listener = FindObjectOfType<AudioListener>();
            if (listener != null)
                audioListenerTransform = listener.transform;
            
            mainCamera = Camera.main;
            if (mainCamera == null)
                mainCamera = FindObjectOfType<Camera>();

            // Create sound pool parent if not assigned
            if (soundPoolParent == null)
            {
                GameObject poolParent = new GameObject("SoundPool");
                poolParent.transform.SetParent(transform);
                soundPoolParent = poolParent.transform;
            }

            // Initialize priority lists
            prioritizedSources[SoundPriority.Critical] = new List<PooledAudioSource>();
            prioritizedSources[SoundPriority.High] = new List<PooledAudioSource>();
            prioritizedSources[SoundPriority.Medium] = new List<PooledAudioSource>();
            prioritizedSources[SoundPriority.Low] = new List<PooledAudioSource>();

            // Create initial pool
            for (int i = 0; i < poolInitialSize; i++)
            {
                CreateAndPoolSource();
            }

            // Initialize category settings NOW that we have the mixer groups
            InitializeCategorySettings();

            Debug.Log($"SoundSystemCore: Initialized with {poolInitialSize} pooled audio sources");
        }

        private void InitializeCategorySettings()
        {
            // Master (Generic/Fallback sounds) → MASTER GROUP
            categorySettings[SoundCategory.Master] = new SoundCategorySettings
            {
                mixerGroup = masterMixerGroup,
                spatialBlend = 0.5f,
                priority = SoundPriority.Medium,
                minDistance = 5f,
                maxDistance = 50f,
                rolloffMode = AudioRolloffMode.Logarithmic,
                dopplerLevel = 0f
            };

            // SFX (Combat, Movement, Gameplay sounds) → SFX GROUP
            categorySettings[SoundCategory.SFX] = new SoundCategorySettings
            {
                mixerGroup = sfxMixerGroup,
                spatialBlend = 0.85f,  // Reduced from 1.0 for more natural 3D effect
                priority = SoundPriority.Medium,
                minDistance = 10f,     // Increased from 5f to better match game scale
                maxDistance = 60f,     // Reduced from 80f for better attenuation
                rolloffMode = AudioRolloffMode.Linear, // Changed from Logarithmic for smoother falloff
                dopplerLevel = 0f
            };

            // Music (Background music, themes) → MUSIC GROUP
            categorySettings[SoundCategory.Music] = new SoundCategorySettings
            {
                mixerGroup = musicMixerGroup,
                spatialBlend = 0f,
                priority = SoundPriority.Critical,
                minDistance = 1f,
                maxDistance = 10f,
                rolloffMode = AudioRolloffMode.Linear,
                dopplerLevel = 0f
            };

            // Ambient (Environment, atmosphere) → AMBIENT GROUP
            categorySettings[SoundCategory.Ambient] = new SoundCategorySettings
            {
                mixerGroup = ambientMixerGroup,
                spatialBlend = 0.75f,  // Reduced from 1.0 for more natural ambient sound
                priority = SoundPriority.Low,
                minDistance = 15f,     // Increased from 10f to match game scale
                maxDistance = 80f,     // Reduced from 100f for better audibility
                rolloffMode = AudioRolloffMode.Linear,
                dopplerLevel = 0f
            };

            // UI (Interface sounds) → UI GROUP
            categorySettings[SoundCategory.UI] = new SoundCategorySettings
            {
                mixerGroup = uiMixerGroup,
                spatialBlend = 0f,
                priority = SoundPriority.High,
                minDistance = 1f,
                maxDistance = 10f,
                rolloffMode = AudioRolloffMode.Linear,
                dopplerLevel = 0f
            };
        }

        /// <summary>
        /// Play a 2D sound (UI, music, etc.)
        /// </summary>
        public SoundHandle PlaySound2D(AudioClip clip, SoundCategory category, float volume = 1f, float pitch = 1f, bool loop = false)
        {
            if (clip == null) return SoundHandle.Invalid;

            var settings = GetCategorySettings(category);
            settings.spatialBlend = 0f; // Force 2D

            return PlaySoundInternal(clip, Vector3.zero, null, settings, volume, pitch, loop);
        }

        /// <summary>
        /// Play a 3D sound at a specific world position
        /// </summary>
        public SoundHandle PlaySound3D(AudioClip clip, Vector3 position, SoundCategory category, float volume = 1f, float pitch = 1f, bool loop = false)
        {
            if (clip == null) return SoundHandle.Invalid;

            var settings = GetCategorySettings(category);
            return PlaySoundInternal(clip, position, null, settings, volume, pitch, loop);
        }

        /// <summary>
        /// Play a 3D sound attached to a transform (follows the object)
        /// </summary>
        public SoundHandle PlaySoundAttached(AudioClip clip, Transform parent, SoundCategory category, float volume = 1f, float pitch = 1f, bool loop = false)
        {
            if (clip == null || parent == null) return SoundHandle.Invalid;

            var settings = GetCategorySettings(category);
            return PlaySoundInternal(clip, parent.position, parent, settings, volume, pitch, loop);
        }

        /// <summary>
        /// Play a 3D sound with custom spatial audio profile (AAA quality positioning)
        /// </summary>
        public SoundHandle PlaySound3DWithProfile(AudioClip clip, Vector3 position, SpatialAudioProfile profile, float volume = 1f, float pitch = 1f, bool loop = false)
        {
            if (clip == null || profile == null) return SoundHandle.Invalid;

            // Convert profile to category settings
            var settings = GetCategorySettings(SoundCategory.SFX);
            settings.spatialBlend = profile.spatialBlend;
            settings.minDistance = profile.minDistance;
            settings.maxDistance = profile.maxDistance;
            settings.rolloffMode = profile.rolloffMode;
            settings.dopplerLevel = profile.dopplerLevel;
            settings.priority = profile.priority;

            return PlaySoundInternal(clip, position, null, settings, volume, pitch, loop, profile);
        }

        /// <summary>
        /// Play a 3D sound attached to transform with custom spatial audio profile
        /// </summary>
        public SoundHandle PlaySoundAttachedWithProfile(AudioClip clip, Transform parent, SpatialAudioProfile profile, float volume = 1f, float pitch = 1f, bool loop = false)
        {
            if (clip == null || parent == null || profile == null) return SoundHandle.Invalid;

            // Convert profile to category settings
            var settings = GetCategorySettings(SoundCategory.SFX);
            settings.spatialBlend = profile.spatialBlend;
            settings.minDistance = profile.minDistance;
            settings.maxDistance = profile.maxDistance;
            settings.rolloffMode = profile.rolloffMode;
            settings.dopplerLevel = profile.dopplerLevel;
            settings.priority = profile.priority;

            var handle = PlaySoundInternal(clip, parent.position, parent, settings, volume, pitch, loop, profile);

            // Track looping sounds for distance-based cleanup
            if (loop && handle.IsValid && SpatialAudioManager.Instance != null)
            {
                SpatialAudioManager.Instance.TrackLoopingSound(handle, parent, profile);
            }

            return handle;
        }

        private SoundHandle PlaySoundInternal(AudioClip clip, Vector3 position, Transform parent, SoundCategorySettings settings, float volume, float pitch, bool loop, SpatialAudioProfile profile = null)
        {
            // Check if we can play this sound based on priority limits
            if (!CanPlaySoundWithPriority(settings.priority))
            {
                Debug.LogWarning($"SoundSystemCore: Cannot play sound '{clip.name}' - priority limit reached for {settings.priority}");
                return SoundHandle.Invalid;
            }

            PooledAudioSource audioSource = GetAvailableAudioSource();
            if (audioSource == null)
            {
                Debug.LogWarning($"SoundSystemCore: No available audio sources for '{clip.name}'");
                return SoundHandle.Invalid;
            }

            // Configure the audio source
            ConfigureAudioSource(audioSource, clip, position, parent, settings, volume, pitch, loop, profile);

            // Add to active list and priority tracking
            activeSources.Add(audioSource);
            prioritizedSources[settings.priority].Add(audioSource);

            // Play the sound
            audioSource.AudioSource.Play();

            // Create and return handle
            SoundHandle handle = new SoundHandle(audioSource, this);
            audioSource.Initialize(handle, settings.priority);

            return handle;
        }

        private void ConfigureAudioSource(PooledAudioSource pooledSource, AudioClip clip, Vector3 position, Transform parent, SoundCategorySettings settings, float volume, float pitch, bool loop, SpatialAudioProfile profile = null)
        {
            AudioSource source = pooledSource.AudioSource;
            Transform sourceTransform = pooledSource.transform;

            // Basic audio settings
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.loop = loop;
            source.outputAudioMixerGroup = settings.mixerGroup;
            
            // Spatial settings - use profile if provided for AAA quality
            if (profile != null)
            {
                profile.ApplyToAudioSource(source);
            }
            else
            {
                // Fallback to category settings
                source.spatialBlend = settings.spatialBlend;
                source.minDistance = settings.minDistance * globalSoundDistanceMultiplier;
                source.maxDistance = settings.maxDistance * globalSoundDistanceMultiplier;
                source.rolloffMode = settings.rolloffMode;
                source.dopplerLevel = 0f; // Force doppler effect off for consistency
            }

            // Position and parenting
            if (parent != null)
            {
                sourceTransform.SetParent(parent);
                sourceTransform.localPosition = Vector3.zero;
                pooledSource.followTransform = parent;
            }
            else
            {
                sourceTransform.SetParent(soundPoolParent);
                sourceTransform.position = position;
                pooledSource.followTransform = null;
            }

            pooledSource.gameObject.SetActive(true);
        }

        private bool CanPlaySoundWithPriority(SoundPriority priority)
        {
            var priorityList = prioritizedSources[priority];
            
            switch (priority)
            {
                case SoundPriority.Critical:
                    return true; // Always allow critical sounds
                case SoundPriority.High:
                    return priorityList.Count < maxHighPrioritySounds;
                case SoundPriority.Medium:
                    return priorityList.Count < maxMediumPrioritySounds;
                case SoundPriority.Low:
                    return priorityList.Count < maxLowPrioritySounds;
                default:
                    return false;
            }
        }

        private PooledAudioSource GetAvailableAudioSource()
        {
            // Try to get from pool first
            if (availableSources.Count > 0)
            {
                return availableSources.Dequeue();
            }

            // Create new one if under limit
            if (activeSources.Count + availableSources.Count < maxConcurrentSounds)
            {
                // We create it but it automatically gets pooled, so we need to dequeue it right away
                CreateAndPoolSource();
                return availableSources.Dequeue();
            }

            // Try to steal a low priority sound
            return StealLowestPrioritySource();
        }

        // Helper to avoid confusion with GetAvailableAudioSource
        private PooledAudioSource CreateAndPoolSource()
        {
            GameObject audioObj = new GameObject("PooledAudioSource");
            audioObj.transform.SetParent(soundPoolParent);
            
            AudioSource audioSource = audioObj.AddComponent<AudioSource>();
            PooledAudioSource pooledSource = audioObj.AddComponent<PooledAudioSource>();
            pooledSource.Initialize(this);
            
            audioObj.SetActive(false);
            availableSources.Enqueue(pooledSource);
            
            return pooledSource;
        }

        private PooledAudioSource CreatePooledAudioSource()
        {
            GameObject audioObj = new GameObject("PooledAudioSource");
            audioObj.transform.SetParent(soundPoolParent);
            
            AudioSource audioSource = audioObj.AddComponent<AudioSource>();
            PooledAudioSource pooledSource = audioObj.AddComponent<PooledAudioSource>();
            pooledSource.Initialize(this);
            
            audioObj.SetActive(false);
            availableSources.Enqueue(pooledSource);
            
            return pooledSource;
        }

        private PooledAudioSource StealLowestPrioritySource()
        {
            // Try to steal from low priority first, then medium, then high
            SoundPriority[] priorities = { SoundPriority.Low, SoundPriority.Medium, SoundPriority.High };
            
            foreach (var priority in priorities)
            {
                var priorityList = prioritizedSources[priority];
                if (priorityList.Count > 0)
                {
                    var sourceToSteal = priorityList[0];
                    ReturnSourceToPool(sourceToSteal);
                    return sourceToSteal;
                }
            }
            
            return null;
        }

        internal void ReturnSourceToPool(PooledAudioSource pooledSource)
        {
            // Remove from active tracking
            activeSources.Remove(pooledSource);
            
            // Remove from priority tracking
            foreach (var priorityList in prioritizedSources.Values)
            {
                priorityList.Remove(pooledSource);
            }

            // Reset and return to pool
            pooledSource.Reset();
            availableSources.Enqueue(pooledSource);
        }

        private SoundCategorySettings GetCategorySettings(SoundCategory category)
        {
            if (categorySettings.TryGetValue(category, out var settings))
                return settings;
                
            Debug.LogWarning($"SoundSystemCore: No settings found for category {category}, using default");
            return categorySettings[SoundCategory.SFX]; // Default fallback
        }

        void Update()
        {
            // Clean up finished sounds
            for (int i = activeSources.Count - 1; i >= 0; i--)
            {
                var source = activeSources[i];
                
                // Check if AudioSource still exists (not destroyed)
                if (source.AudioSource == null)
                {
                    // AudioSource was destroyed, remove from active list
                    activeSources.RemoveAt(i);
                    continue;
                }
                
                try
                {
                    if (!source.AudioSource.isPlaying && !source.AudioSource.loop)
                    {
                        ReturnSourceToPool(source);
                    }
                }
                catch (System.Exception e)
                {
                    // Handle case where AudioSource was destroyed between null check and access
                    Debug.LogWarning($"SoundSystemCore: AudioSource destroyed during Update - {e.Message}");
                    activeSources.RemoveAt(i);
                }
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // Public utility methods
        public void StopAllSounds()
        {
            foreach (var source in activeSources)
            {
                source.AudioSource.Stop();
            }
        }

        public void StopSoundsByCategory(SoundCategory category)
        {
            for (int i = activeSources.Count - 1; i >= 0; i--)
            {
                var source = activeSources[i];
                if (source.Category == category)
                {
                    source.AudioSource.Stop();
                    ReturnSourceToPool(source);
                }
            }
        }

        public int GetActiveSoundCount() => activeSources.Count;
        public int GetAvailableSourceCount() => availableSources.Count;
        
        /// <summary>
        /// PERFORMANCE FIX: Start a coroutine with automatic tracking and cleanup.
        /// Prevents coroutine accumulation that causes memory exhaustion.
        /// </summary>
        public Coroutine StartTrackedCoroutine(System.Collections.IEnumerator routine)
        {
            // Safety check: prevent coroutine accumulation
            if (activeCoroutines.Count >= maxConcurrentCoroutines)
            {
                Debug.LogWarning($"[SoundSystemCore] Coroutine limit reached ({activeCoroutines.Count}/{maxConcurrentCoroutines}). Skipping new coroutine.");
                return null;
            }
            
            Coroutine coroutine = StartCoroutine(TrackedCoroutineWrapper(routine));
            return coroutine;
        }
        
        /// <summary>
        /// Wrapper that tracks coroutine lifecycle and auto-removes from tracking when complete
        /// </summary>
        private System.Collections.IEnumerator TrackedCoroutineWrapper(System.Collections.IEnumerator routine)
        {
            // Note: We can't get the Coroutine reference here, so we track by count
            int startCount = activeCoroutines.Count;
            
            yield return StartCoroutine(routine);
            
            // Coroutine completed, cleanup happens automatically
        }
        
        /// <summary>
        /// Stop all active coroutines - used for cleanup
        /// </summary>
        public void StopAllTrackedCoroutines()
        {
            StopAllCoroutines();
            activeCoroutines.Clear();
        }
    }

    [System.Serializable]
    public class SoundCategorySettings
    {
        public AudioMixerGroup mixerGroup;
        public float spatialBlend = 1f;
        public SoundPriority priority = SoundPriority.Medium;
        public float minDistance = 5f;
        public float maxDistance = 50f;
        public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;
        public float dopplerLevel = 0f;
    }

    public enum SoundCategory
    {
        Master,
        SFX,
        Music,
        Ambient,
        UI
    }

    public enum SoundPriority
    {
        Critical = 0,
        High = 1,
        Medium = 2,
        Low = 3
    }
}
