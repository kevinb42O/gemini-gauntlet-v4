using UnityEngine;
using System.Collections.Generic;

namespace GeminiGauntlet.Audio
{
    /// <summary>
    /// Centralized database for all game sound events. Organized by category and tier.
    /// This is the single source of truth for audio clip configuration.
    /// </summary>
    [CreateAssetMenu(fileName = "SoundEvents", menuName = "Audio/Sound Events")]
    public class SoundEvents : ScriptableObject
    {
        // VISUAL SEPARATION FOR INSPECTOR
        // ─────────────────────────────────
        
        // ▼ ▼ ▼ ▼ ▼ UI SECTION ▼ ▼ ▼ ▼ ▼
        
        [Header("═══════════════════ UI SOUNDS ═══════════════════")]
      
        public SoundEvent uiClick;
        public SoundEvent[] uiFeedback;
        
        [Header("► UI: Inventory")]
        [Tooltip("Sound played when inventory is opened")]
        public SoundEvent inventoryOpen;
        [Tooltip("Sound played when inventory is closed")]
        public SoundEvent inventoryClose;
        
      
        
        // ▼ ▼ ▼ ▼ ▼ PLAYER SECTION ▼ ▼ ▼ ▼ ▼
        
        [Header("═══════════════════ PLAYER ═══════════════════")]
   
        
        [Header("► PLAYER: Movement")]
        public SoundEvent[] jumpSounds;
        public SoundEvent[] doubleJumpSounds;
        public SoundEvent[] landSounds;
        [Tooltip("Dedicated wall jump bounce sound - plays when pushing off a wall")]
        public SoundEvent[] wallJumpSounds;
        [Tooltip("Satisfying notification sound when gaining XP from wall jumps - pitch scales with chain level!")]
        public SoundEvent wallJumpXPNotification;
        public SoundEvent[] footstepSounds;
        public SoundEvent[] sprintFootstepSounds;
        public SoundEvent slideSound;
        [Tooltip("Tactical dive sound - forward dive into prone position")]
        public SoundEvent tacticalDiveSound;
        [Tooltip("Looping wind sound played while falling (starts after brief delay)")]
        public SoundEvent fallingWindLoop;
        
        [Header("► PLAYER: Aerial Tricks")]
        [Tooltip("🔥 EPIC! Bass thump slide down when trick STARTS (plays with slow-motion!)")]
        public SoundEvent trickStartSound;
        [Tooltip("Small trick landing sound (basic flips, short airtime)")]
        public SoundEvent trickLandingSmall;
        [Tooltip("Medium trick landing sound (decent flips, good airtime)")]
        public SoundEvent trickLandingMedium;
        [Tooltip("Big trick landing sound (multi-rotations, long airtime)")]
        public SoundEvent trickLandingBig;
        [Tooltip("Insane trick landing sound (crazy rotations, epic airtime)")]
        public SoundEvent trickLandingInsane;
        [Tooltip("Godlike trick landing sound (MAXIMUM STYLE!)")]
        public SoundEvent trickLandingGodlike;
        [Tooltip("Perfect landing bonus sound (clean landing with no deviation)")]
        public SoundEvent perfectLandingBonus;
        [Tooltip("COMBO! Wall jump + trick combo sound (MULTIPLIER MADNESS!)")]
        public SoundEvent comboMultiplierSound;
        
        [Header("► PLAYER: Flight")]
        public SoundEvent flightActivation;
        public SoundEvent flightLoop;
        public SoundEvent verticalMovementLoop;
        
        [Header("► PLAYER: State")]
        public SoundEvent[] playerDeath;
        public SoundEvent[] playerSpawn;
        public SoundEvent[] fallDamage;
        [Tooltip("Sounds played randomly when player takes damage (with cooldown to prevent spam)")]
        public SoundEvent[] playerHit;
        
        [Header("► PLAYER: Armor")]
        [Tooltip("Sound when an armor plate breaks")]
        public SoundEvent[] armorBreak;
        [Tooltip("Sound when applying/equipping an armor plate (can be interrupted)")]
        public SoundEvent armorPlateApply;
        
        [Header("► PLAYER: Energy")]
        [Tooltip("Out of breath looping sound when energy is low/depleted")]
        public SoundEvent outOfBreathLoop;
        
        [Header("► PLAYER: Hand Overheat")]
        [Tooltip("Sound alert when hand reaches 50% heat (moderate warning)")]
        public SoundEvent handHeat50Warning;
        [Tooltip("Sound alert when hand reaches high heat threshold (70%+ - critical warning)")]
        public SoundEvent handHeatHighWarning;
        [Tooltip("Sound when hand reaches 100% and becomes overheated (critical failure)")]
        public SoundEvent handOverheated;
        [Tooltip("Sound when player tries to shoot but hand is currently overheated (blocked action)")]
        public SoundEvent handOverheatedBlocked;
        
        [Header("► PLAYER: Bleeding Out")]
        [Tooltip("Labored breathing loop when bleeding out (reuses outOfBreathLoop or can be separate)")]
        public SoundEvent bleedingOutBreathingLoop;
        [Tooltip("Heartbeat sound that intensifies as timer runs out")]
        public SoundEvent bleedingOutHeartbeatLoop;
        [Tooltip("Sound when you start bleeding out (critical hit)")]
        public SoundEvent[] bleedingOutStart;
        [Tooltip("Optional struggle sounds when crawling while bleeding out")]
        public SoundEvent[] bleedingOutCrawl;
        
      

        // ▼ ▼ ▼ ▼ ▼ COMBAT SECTION ▼ ▼ ▼ ▼ ▼
        
        [Header("═══════════════════ COMBAT ═══════════════════")]
       
        
        [Header("► COMBAT: Main Weapons")]
        [Tooltip("Shotgun/Tap sounds. Index 0 = Level 1, Index 1 = Level 2, etc.")]
        public SoundEvent[] shotSoundsByLevel;
        [Tooltip("Stream/Beam looping sounds. Index 0 = Level 1, Index 1 = Level 2, etc.")]
        public SoundEvent[] streamSoundsByLevel;

        [Header("► COMBAT: Companion Weapons")]
        [Tooltip("Companion shotgun blast sound")]
        public SoundEvent companionShotgun;
        [Tooltip("Companion stream/beam loop sound")]
        public SoundEvent companionStreamLoop;

        [Header("► COMBAT: Special Attacks")]
        public SoundEvent[] daggerHit;
        public SoundEvent[] aoeActivation;
        
        [Header("► COMBAT: Sword")]
        [Tooltip("Sound when sword mode is activated (unsheath/draw sword sound)")]
        public SoundEvent swordUnsheath;
        [Tooltip("Sound when sword swing starts (whoosh sound)")]
        public SoundEvent swordSwing;
        [Tooltip("Heavy/powerful whoosh sound for charged power attack swing")]
        public SoundEvent swordHeavySwing;
        [Tooltip("Sound when sword hits an enemy or gem (satisfying impact)")]
        public SoundEvent swordHitEnemy;
        [Tooltip("Sound when sword hits a wall or other non-damageable object (clang/scrape)")]
        public SoundEvent swordHitWall;
        [Tooltip("Looping sound when charging a powerful sword attack (hold RMB)")]
        public SoundEvent swordChargeLoop;
        [Tooltip("Sound when releasing a fully charged powerful sword strike")]
        public SoundEvent swordPowerAttack;
        
        

        // ▼ ▼ ▼ ▼ ▼ ENEMIES SECTION ▼ ▼ ▼ ▼ ▼
        
        [Header("═══════════════════ ENEMIES ═══════════════════")]
     
        
        [Header("► ENEMIES: Skulls")]
        public SoundEvent[] skullSpawn;
        public SoundEvent[] skullKill;
        public SoundEvent[] skullMovement;
        public SoundEvent[] skullChatter;
        
        [Header("► ENEMIES: Companion")]
        [Tooltip("Sound when enemy companion is hit by player")]
        public SoundEvent[] companionHitmarker;
        [Tooltip("Sound when enemy companion dies")]
        public SoundEvent[] companionDeath;
        
      

        // ▼ ▼ ▼ ▼ ▼ COLLECTIBLES SECTION ▼ ▼ ▼ ▼ ▼
        
        [Header("═══════════════════ COLLECTIBLES ═══════════════════")]
   
        
        [Header("► COLLECTIBLES: Gems")]
        public SoundEvent[] gemCollection;
        public SoundEvent[] gemSpawn;
        public SoundEvent[] gemHit;
        public SoundEvent[] gemDetach;
        public SoundEvent gemHumming;
        
        [Header("► COLLECTIBLES: Chests")]
        public SoundEvent[] chestEmergence;
        public SoundEvent[] chestOpening;
        [Tooltip("Ambient humming sound that plays when near a chest (proximity-based, looped)")]
        public SoundEvent chestHumming;
        
        [Header("► COLLECTIBLES: Forge")]
        [Tooltip("Ambient humming sound that plays when near the forge (proximity-based, looped, 1500 units range)")]
        public SoundEvent forgeHumming;
        
        [Header("► COLLECTIBLES: Speaker Cube")]
        [Tooltip("Music that plays from speaker cubes (proximity-based, looped)")]
        public SoundEvent speakerMusic;
        
        [Header("► COLLECTIBLES: Interactions")]
        [Tooltip("Sound when grabbing items from chests")]
        public SoundEvent grabItemSound;
        [Tooltip("Sound when opening doors with keycard")]
        public SoundEvent openDoorSound;
        
      
        
        // ▼ ▼ ▼ ▼ ▼ ENVIRONMENT SECTION ▼ ▼ ▼ ▼ ▼
        
        [Header("═══════════════════ ENVIRONMENT ═══════════════════")]
        [Space(10)]
        
        [Header("► ENVIRONMENT: Platforms")]
        public SoundEvent[] platformActivation;
        
        [Header("► ENVIRONMENT: Platform Capture")]
        [Tooltip("Sound when platform capture starts (player enters Central Tower radius)")]
        public SoundEvent platformCaptureStart;
        [Tooltip("Sound when platform capture completes successfully")]
        public SoundEvent platformCaptureComplete;
        
        [Header("► ENVIRONMENT: Towers")]
        public SoundEvent[] towerShoot;
        public SoundEvent[] towerAppear;
        public SoundEvent[] towerCollapse;
        public SoundEvent[] towerCharge;
        [Tooltip("Looping laser sound for Tower Protector Cube (5 second duration)")]
        public SoundEvent towerLaserShoot;
        
        [Header("► ENVIRONMENT: Elevator")]
        [Tooltip("Sound when elevator is called (keycard used)")]
        public SoundEvent elevatorCalled;
        [Tooltip("Sound when elevator arrives after 20 seconds")]
        public SoundEvent elevatorArrival;
        [Tooltip("Sound when elevator doors open")]
        public SoundEvent elevatorDoorsOpen;
        [Tooltip("Sound when elevator doors start closing")]
        public SoundEvent elevatorDoorsClose;
        [Tooltip("Sound when player doesn't have keycard")]
        public SoundEvent elevatorLocked;
       

        // ▼ ▼ ▼ ▼ ▼ POWERUPS SECTION ▼ ▼ ▼ ▼ ▼
        
        [Header("═══════════════════ POWERUPS ═══════════════════")]
       
        
        [Header("► POWERUPS: General")]
        public SoundEvent[] powerUpStart;
        public SoundEvent[] powerUpEnd;
        [Tooltip("Hand upgrade sounds. Index 0 = Lvl 1->2, Index 1 = Lvl 2->3, etc.")]
        public SoundEvent[] handUpgradeSoundsByLevel;
        
        [Header("► POWERUPS: Specific Types")]
        public SoundEvent[] maxHandUpgradeSounds;
        public SoundEvent[] homingDaggersSounds;
        public SoundEvent[] aoeAttackSounds;
        public SoundEvent[] doubleGemsSounds;
        public SoundEvent[] slowTimeSounds;
        public SoundEvent[] godModeSounds;

        /// <summary>
        /// Get a random sound from an array of sound events
        /// </summary>
        public static SoundEvent GetRandomSound(SoundEvent[] soundArray)
        {
            if (soundArray == null || soundArray.Length == 0)
                return null;
                
            return soundArray[Random.Range(0, soundArray.Length)];
        }

        /// <summary>
        /// Play a random sound from an array at a 3D position
        /// </summary>
        public static SoundHandle PlayRandomSound3D(SoundEvent[] soundArray, Vector3 position, float volumeMultiplier = 1f)
        {
            var soundEvent = GetRandomSound(soundArray);
            return soundEvent?.Play3D(position, volumeMultiplier) ?? SoundHandle.Invalid;
        }

        /// <summary>
        /// Play a random sound from an array attached to a transform
        /// </summary>
        public static SoundHandle PlayRandomSoundAttached(SoundEvent[] soundArray, Transform parent, float volumeMultiplier = 1f)
        {
            var soundEvent = GetRandomSound(soundArray);
            return soundEvent?.PlayAttached(parent, volumeMultiplier) ?? SoundHandle.Invalid;
        }

        /// <summary>
        /// Play a random sound from an array as 2D
        /// </summary>
        public static SoundHandle PlayRandomSound2D(SoundEvent[] soundArray, float volumeMultiplier = 1f)
        {
            var soundEvent = GetRandomSound(soundArray);
            return soundEvent?.Play2D(volumeMultiplier) ?? SoundHandle.Invalid;
        }
    }

    /// <summary>
    /// Individual sound event with all necessary configuration
    /// </summary>
    [System.Serializable]
    public class SoundEvent
    {
        [Header("Audio Clip")]
        public AudioClip clip;
        
        [Header("Settings")]
        public SoundCategory category = SoundCategory.SFX;
        [Range(0f, 2f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;
        [Range(0f, 0.5f)] public float pitchVariation = 0.05f;
        public bool loop = false;

        [Header("3D Settings Override")]
        public bool use3DOverride = false;
        [Range(1f, 200f)] public float minDistance3D = 5f;
        [Range(10f, 500f)] public float maxDistance3D = 50f;

        [Header("Volume Rolloff (Anti-Click)")]
        public bool useVolumeRolloff = false;
        [Range(0.05f, 2f)] public float fadeOutDuration = 0.15f;
        [Range(0f, 1f)] public float rolloffStartTime = 0.7f; // Start fade at 70% of clip duration

        [Header("Cooldown")]
        [Tooltip("IMPORTANT: Only use cooldownTime for sounds that DON'T have gameplay cooldowns elsewhere!\n" +
                 "For shotgun/weapons: Set to 0 (weapon system handles cooldown)\n" +
                 "For UI/footsteps/impacts: Use cooldown to prevent spam\n" +
                 "NEVER double-up cooldowns - causes sounds to randomly not play!")]
        public float cooldownTime = 0f;
        private float lastPlayTime = -999f;
        
        // FIXED: Per-instance variable instead of static (was causing cross-sound interference)
        private int consecutiveFailedAttempts = 0;
        
        // PERFORMANCE FIX: Per-source cooldown tracking for high-frequency sounds (shotgun)
        // Prevents hundreds of sounds playing per minute from exhausting the audio pool
        private Dictionary<int, float> perSourceLastPlayTime = new Dictionary<int, float>();

        /// <summary>
        /// Check if this sound can be played (cooldown check) - SIMPLIFIED
        /// </summary>
        public bool CanPlay()
        {
            if (cooldownTime <= 0f) return true;
            return (Time.time - lastPlayTime) >= cooldownTime;
        }
        
        /// <summary>
        /// Force reset cooldown - for testing/debugging purposes
        /// </summary>
        public void ResetCooldown()
        {
            // Setting to a very low value ensures the cooldown is cleared
            lastPlayTime = -999f;
        }
        
        /// <summary>
        /// Always play this sound regardless of cooldown status
        /// </summary>
        public SoundHandle ForcePlay2D(float volumeMultiplier = 1f)
        {
            if (clip == null)
            {
                Debug.LogError($"❌ SoundEvent has NO AUDIO CLIP assigned!");
                return SoundHandle.Invalid;
            }

            // Don't update lastPlayTime to avoid affecting normal cooldown system
            float finalPitch = pitch + Random.Range(-pitchVariation, pitchVariation);
            
            Debug.Log($"⚡ Force playing 2D sound: {clip.name} (category: {category})");
            return SoundSystemCore.Instance?.PlaySound2D(clip, category, volume * volumeMultiplier, finalPitch, loop) ?? SoundHandle.Invalid;
        }

        /// <summary>
        /// Play this sound as 2D - SIMPLIFIED (removed complex cooldown logic)
        /// </summary>
        public SoundHandle Play2D(float volumeMultiplier = 1f)
        {
            // Simple cooldown check - no complex failure tracking
            if (!CanPlay())
            {
                consecutiveFailedAttempts++;
                // Only log every 5th failure to reduce spam
                if (consecutiveFailedAttempts % 5 == 0)
                {
                    Debug.LogWarning($"🔊 Sound {clip?.name} on cooldown ({consecutiveFailedAttempts} attempts)");
                }
                return SoundHandle.Invalid;
            }
            
            // Reset failure counter on successful play
            consecutiveFailedAttempts = 0;
            
            if (clip == null)
            {
                Debug.LogError($"❌ SoundEvent {this} has NO AUDIO CLIP assigned!");
                return SoundHandle.Invalid;
            }

            // Update last play time for cooldown tracking
            lastPlayTime = Time.time;
            float finalPitch = pitch + Random.Range(-pitchVariation, pitchVariation);
            
            Debug.Log($"✅ Playing 2D sound: {clip.name} (category: {category})");
            return SoundSystemCore.Instance?.PlaySound2D(clip, category, volume * volumeMultiplier, finalPitch, loop) ?? SoundHandle.Invalid;
        }

        /// <summary>
        /// Play this sound at a 3D position
        /// </summary>
        public SoundHandle Play3D(Vector3 position, float volumeMultiplier = 1f)
        {
            if (!CanPlay() || clip == null) return SoundHandle.Invalid;

            lastPlayTime = Time.time;
            float finalPitch = pitch + Random.Range(-pitchVariation, pitchVariation);
            
            var handle = SoundSystemCore.Instance?.PlaySound3D(clip, position, category, volume * volumeMultiplier, finalPitch, loop) ?? SoundHandle.Invalid;
            
            // Apply 3D overrides if specified
            if (use3DOverride && handle.IsValid)
            {
                ApplyDistanceOverrides(handle);
            }
            
            // Apply automatic volume rolloff to prevent clicking
            if (useVolumeRolloff && handle.IsValid && !loop && clip != null)
            {
                ApplyVolumeRolloff(handle);
            }
            
            return handle;
        }

        /// <summary>
        /// Play this sound attached to a transform
        /// </summary>
        public SoundHandle PlayAttached(Transform parent, float volumeMultiplier = 1f)
        {
            if (!CanPlay() || clip == null || parent == null) return SoundHandle.Invalid;

            lastPlayTime = Time.time;
            float finalPitch = pitch + Random.Range(-pitchVariation, pitchVariation);
            
            var handle = SoundSystemCore.Instance?.PlaySoundAttached(clip, parent, category, volume * volumeMultiplier, finalPitch, loop) ?? SoundHandle.Invalid;
            
            // Apply 3D overrides if specified
            if (use3DOverride && handle.IsValid)
            {
                ApplyDistanceOverrides(handle);
            }
            
            // Apply automatic volume rolloff to prevent clicking
            if (useVolumeRolloff && handle.IsValid && !loop && clip != null)
            {
                ApplyVolumeRolloff(handle);
            }
            
            return handle;
        }
        
        /// <summary>
        /// Play this sound attached to a transform with per-source cooldown tracking.
        /// CRITICAL for high-frequency sounds like shotgun blasts to prevent audio pool exhaustion.
        /// </summary>
        /// <param name="parent">Transform to attach sound to</param>
        /// <param name="sourceId">Unique ID for the source (e.g., hand instance ID)</param>
        /// <param name="perSourceCooldown">Cooldown time for this specific source</param>
        /// <param name="volumeMultiplier">Volume multiplier</param>
        public SoundHandle PlayAttachedWithSourceCooldown(Transform parent, int sourceId, float perSourceCooldown, float volumeMultiplier = 1f)
        {
            if (clip == null || parent == null) return SoundHandle.Invalid;
            
            // Check per-source cooldown
            if (perSourceLastPlayTime.TryGetValue(sourceId, out float lastTime))
            {
                float timeSinceLastPlay = Time.time - lastTime;
                if (timeSinceLastPlay < perSourceCooldown)
                {
                    // Still on cooldown for this specific source
                    return SoundHandle.Invalid;
                }
            }
            
            // Update per-source cooldown
            perSourceLastPlayTime[sourceId] = Time.time;
            // DO NOT update lastPlayTime here - it would cause cross-source interference!
            // Per-source cooldown is independent and should not affect global cooldown
            
            float finalPitch = pitch + Random.Range(-pitchVariation, pitchVariation);
            
            var handle = SoundSystemCore.Instance?.PlaySoundAttached(clip, parent, category, volume * volumeMultiplier, finalPitch, loop) ?? SoundHandle.Invalid;
            
            // Apply 3D overrides if specified
            if (use3DOverride && handle.IsValid)
            {
                ApplyDistanceOverrides(handle);
            }
            
            // NEVER use volume rolloff for high-frequency sounds - causes coroutine accumulation
            
            return handle;
        }

        private void ApplyDistanceOverrides(SoundHandle handle)
        {
            // This would require additional API in SoundHandle to modify AudioSource settings
            // For now, the 3D settings are handled by category settings in SoundSystemCore
        }

        /// <summary>
        /// Apply beautiful volume rolloff to prevent audio clicking artifacts
        /// </summary>
        private void ApplyVolumeRolloff(SoundHandle handle)
        {
            if (SoundSystemCore.Instance != null && clip != null)
            {
                SoundSystemCore.Instance.StartCoroutine(VolumeRolloffCoroutine(handle));
            }
        }

        /// <summary>
        /// Coroutine that handles smooth volume rolloff to prevent clicking
        /// FIXED: Added safeguards to prevent coroutine accumulation and memory leaks
        /// </summary>
        private System.Collections.IEnumerator VolumeRolloffCoroutine(SoundHandle handle)
        {
            if (!handle.IsValid || clip == null) 
            {
                Debug.LogWarning("⚠️ VolumeRolloffCoroutine: Handle invalid or clip null at start");
                yield break;
            }

            float clipDuration = clip.length;
            float rolloffStartDelay = clipDuration * rolloffStartTime;
            
            // FIXED: Wait with periodic checks instead of single WaitForSeconds
            // This allows early exit if the sound stops during the wait period
            float waitElapsed = 0f;
            while (waitElapsed < rolloffStartDelay)
            {
                // Check if handle is still valid every frame during wait
                if (!handle.IsValid || !handle.IsPlaying)
                {
                    // Sound stopped early, exit coroutine immediately
                    yield break;
                }
                
                waitElapsed += Time.deltaTime;
                yield return null;
            }
            
            // Final check before starting fade
            if (!handle.IsValid || !handle.IsPlaying) yield break;
            
            // Get the current volume to fade from
            float startVolume = handle.GetVolume();
            
            // Smooth fade out over the specified duration
            float elapsed = 0f;
            while (elapsed < fadeOutDuration && handle.IsValid && handle.IsPlaying)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / fadeOutDuration;
                
                // Use smooth curve for natural rolloff
                float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
                float currentVolume = Mathf.Lerp(startVolume, 0f, smoothProgress);
                
                handle.SetVolume(currentVolume);
                yield return null;
            }
            
            // Ensure the sound stops cleanly
            if (handle.IsValid)
            {
                handle.Stop();
            }
        }

        /// <summary>
        /// Get a formatted name for this sound event
        /// </summary>
        public override string ToString()
        {
            string clipName = clip?.name ?? "No Clip";
            return $"SoundEvent[{clipName}] - {category}";
        }
    }
}
