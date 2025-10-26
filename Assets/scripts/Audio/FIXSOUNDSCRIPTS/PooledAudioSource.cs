using UnityEngine;

namespace GeminiGauntlet.Audio
{
    /// <summary>
    /// Pooled AudioSource component that can be reused to avoid memory allocation
    /// </summary>
    public class PooledAudioSource : MonoBehaviour
    {
        public AudioSource AudioSource { get; private set; }
        public SoundHandle Handle { get; private set; }
        public SoundPriority Priority { get; private set; }
        public SoundCategory Category { get; private set; }
        public Transform followTransform;

        private SoundSystemCore soundSystem;
        private float startTime;
        private bool isInitialized = false;
        private bool isReturningToPool = false; // ADDED: Prevent multiple ReturnToPool calls

        void Awake()
        {
            AudioSource = GetComponent<AudioSource>();
        }

        public void Initialize(SoundSystemCore system)
        {
            soundSystem = system;
            isInitialized = true;
        }

        public void Initialize(SoundHandle handle, SoundPriority priority)
        {
            Handle = handle;
            Priority = priority;
            startTime = Time.time;
        }

        void Update()
        {
            // CRITICAL: Early exit if already returning to pool (prevents race condition)
            if (isReturningToPool) return;
            
            // OPTIMIZED: Only update position if transform following is active
            if (followTransform != null)
                transform.position = followTransform.position;

            // FIXED: Add null safety and prevent multiple ReturnToPool calls
            // Only return non-looping sounds that have finished playing
            if (isInitialized && AudioSource != null && 
                !AudioSource.isPlaying && !AudioSource.loop)
            {
                ReturnToPool();
            }
        }

        public void Stop()
        {
            // CRITICAL FIX: Prevent race condition - set flag BEFORE doing anything
            if (isReturningToPool) return;
            isReturningToPool = true;
            
            if (AudioSource != null)
            {
                // CRITICAL: Force stop everything
                AudioSource.loop = false; // Disable loop FIRST
                AudioSource.Stop(); // Stop playback
                AudioSource.clip = null; // Clear clip
            }
            
            ReturnToPool();
        }

        public void SetVolume(float volume)
        {
            if (AudioSource != null)
            {
                AudioSource.volume = volume;
            }
        }

        public void SetPitch(float pitch)
        {
            if (AudioSource != null)
            {
                AudioSource.pitch = pitch;
            }
        }

        public bool IsPlaying()
        {
            return AudioSource != null && AudioSource.isPlaying;
        }

        public float GetPlaybackTime()
        {
            return Time.time - startTime;
        }

        public void Reset()
        {
            if (AudioSource != null)
            {
                AudioSource.Stop();
                AudioSource.clip = null;
                AudioSource.loop = false;
                AudioSource.volume = 1f;
                AudioSource.pitch = 1f;
            }

            Handle = SoundHandle.Invalid;
            Priority = SoundPriority.Medium;
            followTransform = null;
            startTime = 0f;
            isInitialized = false;
            isReturningToPool = false; // FIXED: Reset the flag for clean state

            transform.SetParent(soundSystem?.transform);
            transform.localPosition = Vector3.zero;
            gameObject.SetActive(false);
        }

        public void ReturnToPool()
        {
            // CRITICAL FIX: Prevent multiple ReturnToPool calls (thread-safe pattern)
            if (isReturningToPool)
            {
                // Already returning to pool - this is the race condition we're protecting against
                return;
            }
            isReturningToPool = true;
            
            // FIXED: Add null safety check for soundSystem
            if (soundSystem != null)
            {
                soundSystem.ReturnSourceToPool(this);
            }
            else
            {
                Debug.LogWarning($"[PooledAudioSource] soundSystem is null when trying to return {gameObject.name} to pool");
            }
        }

        void OnDestroy()
        {
            // Mark handle as invalid when this object is destroyed
            if (Handle != null && Handle.IsValid)
            {
                Handle.MarkAsInvalid();
            }
        }
    }
}
