// --- BloodSplashEffect.cs (Ultra Lightweight - Pool Compatible) ---
using UnityEngine;

namespace GeminiGauntlet.Effects
{
    /// <summary>
    /// Lightweight auto-cleanup blood splash effect for enemy deaths.
    /// Spawns, plays once, then destroys itself automatically.
    /// PERFORMANCE: Single particle system, no Update loops, auto-cleanup.
    /// </summary>
    public class BloodSplashEffect : MonoBehaviour
    {
        [Header("Auto-Cleanup Settings")]
        [Tooltip("Destroy this GameObject after particles finish (default 2 seconds)")]
        [SerializeField] private float lifetime = 2f;
        
        [Header("Optional Components")]
        [Tooltip("If assigned, will play this sound on spawn")]
        [SerializeField] private AudioClip splashSound;
        [SerializeField] [Range(0f, 1f)] private float soundVolume = 0.3f;
        
        private ParticleSystem _particleSystem;
        private float _spawnTime;
        
        void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
            if (_particleSystem == null)
            {
                Debug.LogWarning($"[BloodSplashEffect] No ParticleSystem found on {gameObject.name}! Effect will not display.", this);
            }
        }
        
        void OnEnable()
        {
            _spawnTime = Time.time;
            
            // Play particle effect
            if (_particleSystem != null)
            {
                _particleSystem.Play();
            }
            
            // Play optional sound
            if (splashSound != null)
            {
                AudioSource.PlayClipAtPoint(splashSound, transform.position, soundVolume);
            }
            
            // Schedule auto-cleanup
            Destroy(gameObject, lifetime);
        }
        
        /// <summary>
        /// Public method to spawn blood splash at a specific position
        /// </summary>
        public static GameObject SpawnBloodSplash(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
            {
                Debug.LogWarning("[BloodSplashEffect] Cannot spawn blood splash - prefab is null!");
                return null;
            }
            
            GameObject instance = Instantiate(prefab, position, rotation);
            return instance;
        }
    }
}
