using UnityEngine;

namespace GeminiGauntlet.Audio
{
    /// <summary>
    /// Manager for centralized sound events database
    /// Provides easy access to sound events throughout the game
    /// </summary>
    public class SoundEventsManager : MonoBehaviour
    {
        public static SoundEventsManager Instance { get; private set; }
        
        [Header("=== SOUND EVENTS DATABASE ===")]
        [SerializeField] private SoundEvents soundEvents;
        
        public static SoundEvents Events => Instance?.soundEvents;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                
                // CRITICAL FIX: DontDestroyOnLoad only works on root GameObjects
                // If this GameObject has a parent, move it to root first
                if (transform.parent != null)
                {
                    Debug.LogWarning($"SoundEventsManager: Moving '{gameObject.name}' to root for DontDestroyOnLoad", this);
                    transform.SetParent(null);
                }
                
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            // Null safety check to prevent silent failures
            if (soundEvents == null)
            {
                Debug.LogError(" SoundEventsManager: No SoundEvents assigned! Sounds will not work. Please assign a SoundEvents asset in the inspector.");
            }
            else
            {
                Debug.Log($" SoundEventsManager: Initialized with {soundEvents.name} asset");
            }
        }

        /// <summary>
        /// Register a sound events database (called by bootstrap)
        /// </summary>
        public static void RegisterSoundEvents(SoundEvents events)
        {
            if (Instance == null)
            {
                // Create manager if it doesn't exist
                GameObject managerGO = new GameObject("SoundEventsManager");
                Instance = managerGO.AddComponent<SoundEventsManager>();
                DontDestroyOnLoad(managerGO);
            }
            
            Instance.soundEvents = events;
            Debug.Log("SoundEventsManager: Sound events database registered successfully.");
        }

        // Convenience methods for common sound categories
        public static class Quick
        {
            /// <summary>
            /// Play a random UI sound
            /// </summary>
            public static SoundHandle PlayUISound(SoundEvent soundEvent, float volume = 1f)
            {
                return soundEvent?.Play2D(volume) ?? SoundHandle.Invalid;
            }

            /// <summary>
            /// Play a random combat sound at position
            /// </summary>
            public static SoundHandle PlayCombatSound(SoundEvent soundEvent, Vector3 position, float volume = 1f)
            {
                return soundEvent?.Play3D(position, volume) ?? SoundHandle.Invalid;
            }

            /// <summary>
            /// Play a random enemy sound at position
            /// </summary>
            public static SoundHandle PlayEnemySound(SoundEvent soundEvent, Vector3 position, float volume = 1f)
            {
                return soundEvent?.Play3D(position, volume) ?? SoundHandle.Invalid;
            }

            /// <summary>
            /// Play a random movement sound attached to transform
            /// </summary>
            public static SoundHandle PlayMovementSound(SoundEvent soundEvent, Transform parent, float volume = 1f)
            {
                return soundEvent?.PlayAttached(parent, volume) ?? SoundHandle.Invalid;
            }

            /// <summary>
            /// Play a random sound from array at position
            /// </summary>
            public static SoundHandle PlayRandomSound3D(SoundEvent[] soundArray, Vector3 position, float volume = 1f)
            {
                return SoundEvents.PlayRandomSound3D(soundArray, position, volume);
            }

            /// <summary>
            /// Play a random sound from array attached to transform
            /// </summary>
            public static SoundHandle PlayRandomSoundAttached(SoundEvent[] soundArray, Transform parent, float volume = 1f)
            {
                return SoundEvents.PlayRandomSoundAttached(soundArray, parent, volume);
            }

            /// <summary>
            /// Play a random 2D sound from array
            /// </summary>
            public static SoundHandle PlayRandomSound2D(SoundEvent[] soundArray, float volume = 1f)
            {
                return SoundEvents.PlayRandomSound2D(soundArray, volume);
            }
        }

        // NOTE: GameSounds class moved to GameSoundsHelper.cs to avoid conflicts
        // Direct sound access is now handled by the main GameSounds compatibility bridge

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // Debug and utility methods
        [ContextMenu("List All Sound Events")]
        public void ListAllSoundEvents()
        {
            if (soundEvents == null)
            {
                Debug.LogWarning("SoundEventsManager: No sound events database assigned.");
                return;
            }

            Debug.Log("=== SOUND EVENTS DATABASE ===");
            Debug.Log($"UI Sounds: {(soundEvents.uiClick != null ? "✓" : "✗")} Click");
            Debug.Log($"Player Sounds: {soundEvents.jumpSounds?.Length ?? 0} Jump, {soundEvents.landSounds?.Length ?? 0} Land, {soundEvents.footstepSounds?.Length ?? 0} Footstep");
            Debug.Log($"Combat Sounds: {soundEvents.daggerHit?.Length ?? 0} Hit");
            Debug.Log($"Enemy Sounds: {soundEvents.skullSpawn?.Length ?? 0} Skull Spawn, {soundEvents.skullKill?.Length ?? 0} Skull Kill");
            Debug.Log($"Environment: {soundEvents.gemCollection?.Length ?? 0} Gem Collect, {soundEvents.powerUpStart?.Length ?? 0} Power Up");
        }

        [ContextMenu("Test Random Sound")]
        public void TestRandomSound()
        {
            if (soundEvents?.jumpSounds != null && soundEvents.jumpSounds.Length > 0)
            {
                Quick.PlayRandomSound3D(soundEvents.jumpSounds, transform.position, 1f);
                Debug.Log("SoundEventsManager: Played test jump sound");
            }
            else
            {
                Debug.LogWarning("SoundEventsManager: No jump sounds available for testing");
            }
        }
    }
}
