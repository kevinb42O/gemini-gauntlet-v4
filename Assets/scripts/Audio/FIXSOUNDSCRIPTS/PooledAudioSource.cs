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
            // OPTIMIZED: Only update position if transform following is active
            if (followTransform != null)
                transform.position = followTransform.position;

            // FIXED: Add null safety and prevent multiple ReturnToPool calls
            if (isInitialized && AudioSource != null && !isReturningToPool && 
                !AudioSource.isPlaying && !AudioSource.loop)
            {
                ReturnToPool();
            }
        }

        public void Stop()
        {
            if (AudioSource != null)
            {
                // CRITICAL: Force stop everything
                AudioSource.loop = false; // Disable loop FIRST
                AudioSource.Stop(); // Stop playback
                AudioSource.clip = null; // Clear clip
                Debug.Log($"[PooledAudioSource] FORCE STOPPED {gameObject.name} (was playing: {AudioSource.isPlaying})");
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
            // FIXED: Prevent multiple ReturnToPool calls
            if (isReturningToPool) return;
            isReturningToPool = true;
            
            // FIXED: Add null safety check for soundSystem
            if (soundSystem != null)
            {
                soundSystem.ReturnSourceToPool(this);
            }
            else
            {
                Debug.LogWarning($" PooledAudioSource: soundSystem is null when trying to return {gameObject.name} to pool");
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
