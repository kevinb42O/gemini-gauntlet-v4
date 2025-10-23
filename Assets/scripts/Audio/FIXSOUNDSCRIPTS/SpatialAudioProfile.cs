using UnityEngine;

namespace GeminiGauntlet.Audio
{
    /// <summary>
    /// AAA-Quality Spatial Audio Profile System
    /// Defines crystal-clear 3D audio parameters for different entity types
    /// </summary>
    [System.Serializable]
    public class SpatialAudioProfile
    {
        [Header("=== IDENTITY ===")]
        public string profileName = "Default";
        public SpatialAudioType audioType = SpatialAudioType.Enemy;
        
        [Header("=== 3D SPATIAL SETTINGS ===")]
        [Tooltip("0 = 2D (no spatial), 1 = Full 3D spatial")]
        [Range(0f, 1f)]
        public float spatialBlend = 1f;
        
        [Tooltip("Distance where audio starts attenuating (full volume)")]
        public float minDistance = 5f;
        
        [Tooltip("Distance where audio becomes inaudible (zero volume)")]
        public float maxDistance = 50f;
        
        [Tooltip("How audio fades with distance")]
        public AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;
        
        [Tooltip("Custom rolloff curve (if rolloffMode is Custom)")]
        public AnimationCurve customRolloffCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        
        [Header("=== DOPPLER & EFFECTS ===")]
        [Tooltip("Doppler effect intensity (0 = off, 1 = max)")]
        [Range(0f, 1f)]
        public float dopplerLevel = 0f;
        
        [Tooltip("How much audio spreads in 3D space (0-360 degrees)")]
        [Range(0f, 360f)]
        public float spread = 0f;
        
        [Header("=== DISTANCE MANAGEMENT ===")]
        [Tooltip("Auto-stop looping sounds beyond this distance from listener")]
        public float maxAudibleDistance = 100f;
        
        [Tooltip("Check distance every N seconds for cleanup")]
        public float distanceCheckInterval = 0.5f;
        
        [Tooltip("Fade out duration when distance-culling")]
        public float distanceCullFadeOut = 0.2f;
        
        [Header("=== PRIORITY ===")]
        public SoundPriority priority = SoundPriority.Medium;
        
        [Header("=== OCCLUSION (Future) ===")]
        [Tooltip("Enable occlusion by geometry (walls, obstacles)")]
        public bool enableOcclusion = false;
        
        [Tooltip("LayerMask for occlusion raycasts")]
        public LayerMask occlusionLayers = -1;
        
        /// <summary>
        /// Apply this profile to an AudioSource
        /// </summary>
        public void ApplyToAudioSource(AudioSource source)
        {
            if (source == null) return;
            
            source.spatialBlend = spatialBlend;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.rolloffMode = rolloffMode;
            source.dopplerLevel = dopplerLevel;
            source.spread = spread;
            
            if (rolloffMode == AudioRolloffMode.Custom && customRolloffCurve != null)
            {
                source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, customRolloffCurve);
            }
        }
        
        /// <summary>
        /// Get distance from listener to position
        /// </summary>
        public float GetDistanceFromListener(Vector3 position)
        {
            var listener = Camera.main?.GetComponent<AudioListener>();
            if (listener == null) return 0f;
            
            return Vector3.Distance(listener.transform.position, position);
        }
        
        /// <summary>
        /// Check if position is within audible range
        /// </summary>
        public bool IsAudibleFromListener(Vector3 position)
        {
            return GetDistanceFromListener(position) <= maxAudibleDistance;
        }
        
        /// <summary>
        /// Calculate volume multiplier based on distance
        /// </summary>
        public float GetVolumeMultiplierAtDistance(Vector3 position)
        {
            float distance = GetDistanceFromListener(position);
            
            if (distance <= minDistance) return 1f;
            if (distance >= maxDistance) return 0f;
            
            // Linear interpolation between min and max distance
            float normalizedDistance = (distance - minDistance) / (maxDistance - minDistance);
            return Mathf.Clamp01(1f - normalizedDistance);
        }
    }
    
    public enum SpatialAudioType
    {
        Player,
        Enemy,
        Boss,
        Tower,
        Environment,
        UI,
        Music,
        Ambient
    }
    
    /// <summary>
    /// Predefined AAA spatial audio profiles for game entities
    /// </summary>
    public static class SpatialAudioProfiles
    {
        /// <summary>
        /// Skull enemy chatter - medium range, clear spatial positioning
        /// Scaled for large game world (3200-unit platforms, 320-unit character)
        /// </summary>
        public static SpatialAudioProfile SkullChatter => new SpatialAudioProfile
        {
            profileName = "Skull Chatter",
            audioType = SpatialAudioType.Enemy,
            spatialBlend = 1f,
            minDistance = 1000f,        // ~3 character heights (full volume)
            maxDistance = 5000f,        // ~1.5 platforms (zero volume)
            maxAudibleDistance = 7000f, // Auto-stop beyond this
            rolloffMode = AudioRolloffMode.Linear,
            dopplerLevel = 0f,
            spread = 30f,
            priority = SoundPriority.Low,
            distanceCheckInterval = 0.5f,
            distanceCullFadeOut = 0.3f
        };
        
        /// <summary>
        /// Skull death sound - medium range, high impact
        /// Scaled for large game world (3200-unit platforms, 320-unit character)
        /// </summary>
        public static SpatialAudioProfile SkullDeath => new SpatialAudioProfile
        {
            profileName = "Skull Death",
            audioType = SpatialAudioType.Enemy,
            spatialBlend = 1f,
            minDistance = 1500f,         // ~4-5 character heights (full volume)
            maxDistance = 8000f,         // ~2.5 platforms (zero volume)
            maxAudibleDistance = 10000f, // Audible across multiple platforms
            rolloffMode = AudioRolloffMode.Linear,
            dopplerLevel = 0f,
            spread = 45f,
            priority = SoundPriority.Medium,
            distanceCheckInterval = 0f  // One-shot, no distance check
        };
        
        /// <summary>
        /// Tower shooting - long range, directional
        /// Scaled for large game world (towers should be heard from far)
        /// </summary>
        public static SpatialAudioProfile TowerShoot => new SpatialAudioProfile
        {
            profileName = "Tower Shoot",
            audioType = SpatialAudioType.Tower,
            spatialBlend = 1f,
            minDistance = 2000f,         // ~6 character heights (full volume)
            maxDistance = 12000f,        // ~3.75 platforms (zero volume)
            maxAudibleDistance = 15000f, // Audible across large distances
            rolloffMode = AudioRolloffMode.Linear,
            dopplerLevel = 0f,
            spread = 20f,  // More directional
            priority = SoundPriority.Medium,
            distanceCheckInterval = 0f
        };
        
        /// <summary>
        /// Tower idle/ambient - close range looping
        /// Scaled for large game world (should fade when leaving area)
        /// </summary>
        public static SpatialAudioProfile TowerIdle => new SpatialAudioProfile
        {
            profileName = "Tower Idle",
            audioType = SpatialAudioType.Tower,
            spatialBlend = 1f,
            minDistance = 1500f,        // ~4-5 character heights (full volume)
            maxDistance = 6000f,        // ~2 platforms (zero volume)
            maxAudibleDistance = 8000f, // Auto-stop when far (2.5 platforms)
            rolloffMode = AudioRolloffMode.Linear,
            dopplerLevel = 0f,
            spread = 60f,  // More ambient spread
            priority = SoundPriority.Low,
            distanceCheckInterval = 0.3f,
            distanceCullFadeOut = 0.5f
        };
        
        /// <summary>
        /// Tower awaken - medium range impact
        /// Scaled for large game world (big dramatic moment)
        /// </summary>
        public static SpatialAudioProfile TowerAwaken => new SpatialAudioProfile
        {
            profileName = "Tower Awaken",
            audioType = SpatialAudioType.Tower,
            spatialBlend = 1f,
            minDistance = 2000f,         // ~6 character heights (full volume)
            maxDistance = 10000f,        // ~3 platforms (zero volume)
            maxAudibleDistance = 12000f, // Audible across large area
            rolloffMode = AudioRolloffMode.Linear,
            dopplerLevel = 0f,
            spread = 40f,
            priority = SoundPriority.High,
            distanceCheckInterval = 0f
        };
        
        /// <summary>
        /// Player movement - close range
        /// Scaled for large game world (player-centric sounds)
        /// </summary>
        public static SpatialAudioProfile PlayerMovement => new SpatialAudioProfile
        {
            profileName = "Player Movement",
            audioType = SpatialAudioType.Player,
            spatialBlend = 0.7f,  // Slightly less spatial for player
            minDistance = 800f,         // ~2.5 character heights (full volume)
            maxDistance = 4000f,        // ~1.25 platforms (zero volume)
            maxAudibleDistance = 6000f, // Should be heard nearby
            rolloffMode = AudioRolloffMode.Linear,
            dopplerLevel = 0f,
            spread = 90f,
            priority = SoundPriority.High,
            distanceCheckInterval = 0f
        };
        
        /// <summary>
        /// Generic 3D sound effect - medium range
        /// Scaled for large game world (default fallback)
        /// </summary>
        public static SpatialAudioProfile GenericSFX => new SpatialAudioProfile
        {
            profileName = "Generic SFX",
            audioType = SpatialAudioType.Environment,
            spatialBlend = 1f,
            minDistance = 1200f,        // ~3.75 character heights (full volume)
            maxDistance = 6000f,        // ~2 platforms (zero volume)
            maxAudibleDistance = 8000f, // Reasonable fallback range
            rolloffMode = AudioRolloffMode.Linear,
            dopplerLevel = 0f,
            spread = 45f,
            priority = SoundPriority.Medium,
            distanceCheckInterval = 0f
        };
    }
}
