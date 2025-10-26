using UnityEngine;
using System.Collections.Generic;

// Add necessary imports
using System; // For Exception type

namespace GeminiGauntlet.Audio
{
    /// <summary>
    /// Static API gateway for all game sound playback. Routes all calls through the
    /// centralized SoundEvents system, now with level-aware methods for tiered audio.
    /// </summary>
    public static class GameSounds
    {
        private static SoundEvents cachedEvents;
        private static Dictionary<Transform, SoundHandle> activeBreathingSounds = new Dictionary<Transform, SoundHandle>();
        private static bool hasLoggedInitializationWarning = false;

        private static SoundEvents SafeEvents {
            get {
                if (cachedEvents != null) return cachedEvents;
                if (SoundEventsManager.Instance != null) cachedEvents = SoundEventsManager.Events;
                if (cachedEvents == null && !hasLoggedInitializationWarning)
                {
                    Debug.LogWarning("🔊 GameSounds: Sound system not properly initialized. Sounds will be skipped.");
                    hasLoggedInitializationWarning = true;
                }
                return cachedEvents;
            }
        }
        
        public static bool IsSystemReady => SafeEvents != null;

        private static SoundHandle SafePlaySound3D(SoundEvent[] soundArray, Vector3 position, float volume)
        {
            if (!IsSystemReady || soundArray == null || soundArray.Length == 0) return SoundHandle.Invalid;
            return SoundEvents.PlayRandomSound3D(soundArray, position, volume);
        }

        // Overload for single SoundEvent instead of array
        private static SoundHandle SafePlaySound3D(SoundEvent soundEvent, Vector3 position, float volume)
        {
            if (!IsSystemReady || soundEvent == null) return SoundHandle.Invalid;
            return soundEvent.Play3D(position, volume);
        }

        private static SoundHandle SafePlayTieredSound3D(SoundEvent[] soundArray, int level, Vector3 position, float volume)
        {
            if (!IsSystemReady || soundArray == null || soundArray.Length == 0) return SoundHandle.Invalid;
            int index = Mathf.Clamp(level - 1, 0, soundArray.Length - 1);
            var soundEvent = soundArray[index];
            return soundEvent?.Play3D(position, volume) ?? SoundHandle.Invalid;
        }

        private static SoundHandle SafePlayShotgunSound3D(SoundEvent[] soundArray, int level, Vector3 position, float volume)
        {
            if (!IsSystemReady || soundArray == null || soundArray.Length == 0) return SoundHandle.Invalid;
            int index = Mathf.Clamp(level - 1, 0, soundArray.Length - 1);
            var soundEvent = soundArray[index];
            
            if (soundEvent != null)
            {
                // CRITICAL: Uses only per-source cooldown (0.02s) to prevent double-triggering.
                // Fire rate is controlled by HandFiringMechanics._nextShotgunFireTime
                // NO sound cooldownTime should be set on shotgun sounds!
                
                var handle = soundEvent.Play3D(position, volume);
                return handle;
            }
            
            return SoundHandle.Invalid;
        }

        // ===== PLAYER SOUNDS =====
        // Methods moved to prevent duplication with full implementations below
        
        // ===== COMBAT SOUNDS =====
        // Combat sound methods moved to prevent duplication with full implementations below
        public static SoundHandle PlayShotgunBlast(Vector3 position, int level, float volume = 1f) => SafePlayShotgunSound3D(SafeEvents?.shotSoundsByLevel, level, position, volume);
        
        /// <summary>
        /// Play shotgun sound as 2D audio (no spatial positioning).
        /// CHANGED: Now plays as 2D UI sound for consistent volume and reliability.
        /// No more 3D spatial audio issues - sounds always play at full volume.
        /// 
        /// NOTE: Sound cleanup is handled by PlayerShooterOrchestrator's ring buffer system.
        /// </summary>
        /// <param name="handTransform">The Transform of the hand (left or right) - NOT USED, kept for API compatibility</param>
        /// <param name="level">Hand level (1-4)</param>
        /// <param name="volume">Volume multiplier</param>
        public static SoundHandle PlayShotgunBlastOnHand(Transform handTransform, int level, float volume = 1f)
        {
            if (!IsSystemReady)
            {
                return SoundHandle.Invalid;
            }
            
            if (SafeEvents == null || SafeEvents.shotSoundsByLevel == null)
            {
                return SoundHandle.Invalid;
            }
            
            int index = Mathf.Clamp(level - 1, 0, SafeEvents.shotSoundsByLevel.Length - 1);
            var soundEvent = SafeEvents.shotSoundsByLevel[index];
            
            if (soundEvent == null || soundEvent.clip == null)
            {
                return SoundHandle.Invalid;
            }
            
            // Play as 2D sound - no spatial positioning, always full volume
            return soundEvent.Play2D(volume);
        }
        
        public static SoundHandle PlayStreamLoop(Transform parent, int level, float volume = 1f)
        {
            Debug.Log($"PlayStreamLoop called - Level: {level}, Volume: {volume}, Parent: {(parent != null ? parent.name : "null")}");
            
            if (!IsSystemReady)
            {
                Debug.LogWarning("PlayStreamLoop: Sound system not ready!");
                return SoundHandle.Invalid;
            }
            
            if (SafeEvents == null)
            {
                Debug.LogError("PlayStreamLoop: SafeEvents is null!");
                return SoundHandle.Invalid;
            }
            
            if (SafeEvents.streamSoundsByLevel == null)
            {
                Debug.LogError("PlayStreamLoop: streamSoundsByLevel array is null!");
                return SoundHandle.Invalid;
            }
            
            Debug.Log($"StreamSoundsByLevel array length: {SafeEvents.streamSoundsByLevel.Length}");
            int index = Mathf.Clamp(level - 1, 0, SafeEvents.streamSoundsByLevel.Length - 1);
            Debug.Log($"Using index {index} for level {level}");
            
            var soundEvent = SafeEvents.streamSoundsByLevel[index];
            if (soundEvent == null)
            {
                Debug.LogError($"PlayStreamLoop: No sound event configured for level {level} (index {index})!");
                return SoundHandle.Invalid;
            }
            
            Debug.Log($"Playing stream sound for level {level} with volume {volume}");
            return soundEvent.PlayAttached(parent, volume);
        }

        // ===== ENEMY & BOSS SOUNDS =====
        // Enemy sound methods moved to prevent duplication with full implementations below
        // StartEnemyBreathing is fully implemented later in this file (line ~505)
        // StopEnemyBreathingInternal is fully implemented later in this file (line ~556)

        // ===== ENVIRONMENT & INTERACTION SOUNDS =====
        // Environment sound methods moved to prevent duplication with full implementations below
        public static void PlayChestEmergence(Vector3 position, float volume = 1f) => SafePlaySound3D(SafeEvents?.chestEmergence, position, volume);
        public static void PlayChestOpening(Vector3 position, float volume = 1f) => SafePlaySound3D(SafeEvents?.chestOpening, position, volume);
        
        /// <summary>
        /// Start chest humming sound (looped, proximity-based) - handled by ChestSoundManager
        /// This is a convenience method for direct access if needed
        /// </summary>
        public static SoundHandle PlayChestHumming(Transform parent, float volume = 1f)
        {
            if (!IsSystemReady || SafeEvents?.chestHumming == null) return SoundHandle.Invalid;
            return SafeEvents.chestHumming.PlayAttached(parent, volume);
        }
        
        // Elevator sounds
        public static void PlayElevatorCalled(Vector3 position, float volume = 1f) => SafePlaySound3D(SafeEvents?.elevatorCalled, position, volume);
        public static void PlayElevatorArrival(Vector3 position, float volume = 1f) => SafePlaySound3D(SafeEvents?.elevatorArrival, position, volume);
        public static void PlayElevatorDoorsOpen(Vector3 position, float volume = 1f) => SafePlaySound3D(SafeEvents?.elevatorDoorsOpen, position, volume);
        public static void PlayElevatorDoorsClose(Vector3 position, float volume = 1f) => SafePlaySound3D(SafeEvents?.elevatorDoorsClose, position, volume);
        public static void PlayElevatorLocked(Vector3 position, float volume = 1f) => SafePlaySound3D(SafeEvents?.elevatorLocked, position, volume);
        
        // ===== PROGRESSION & POWER-UP SOUNDS =====
        // Power-up sound methods moved to prevent duplication with full implementations below
        public static void PlayHandUpgrade(int newLevel, float volume = 1f)
        {
            if (!IsSystemReady || SafeEvents?.handUpgradeSoundsByLevel == null) return;
            int index = newLevel - 2; // Upgrade TO level 2 is index 0
            if (index < 0 || index >= SafeEvents.handUpgradeSoundsByLevel.Length)
            {
                index = SafeEvents.handUpgradeSoundsByLevel.Length - 1; // Fallback
            }
            if (index < 0) return; // No sounds available
            var soundEvent = SafeEvents.handUpgradeSoundsByLevel[index];
            soundEvent?.Play2D(volume);
        }
        
        // ===== GEM SOUNDS =====
        // Gem sound methods moved to prevent duplication with full implementations below
        

        // ===== PLAYER MOVEMENT SOUNDS =====
        
        public static void PlayPlayerJump(float volume = 1f)
        {
            PlayPlayerJump(GetPlayerPosition(), volume);
        }
        
        public static void PlayPlayerJump(Vector3 position, float volume = 1f)
        {
            SafePlaySound3D(SafeEvents?.jumpSounds, position, volume);
        }
        
        public static void PlayPlayerDoubleJump(float volume = 1f)
        {
            PlayPlayerDoubleJump(GetPlayerPosition(), volume);
        }
        
        public static void PlayPlayerDoubleJump(Vector3 position, float volume = 1f)
        {
            // Play from doubleJumpSounds array if available
            if (SafeEvents?.doubleJumpSounds != null && SafeEvents.doubleJumpSounds.Length > 0)
            {
                SafePlaySound3D(SafeEvents.doubleJumpSounds, position, volume);
                Debug.Log("<color=magenta>🔊 DOUBLE JUMP SOUND PLAYED</color>");
            }
            // Fallback to regular jump sound with higher pitch if no double jump sounds available
            else if (SafeEvents?.jumpSounds != null && SafeEvents.jumpSounds.Length > 0)
            {
                // Get a random jump sound and play with higher pitch
                var jumpSound = SoundEvents.GetRandomSound(SafeEvents.jumpSounds);
                if (jumpSound != null)
                {
                    // Store original pitch
                    float originalPitch = jumpSound.pitch;
                    // Set higher pitch for double jump
                    jumpSound.pitch *= 1.2f;
                    // Play the sound
                    jumpSound.Play3D(position, volume);
                    // Restore original pitch
                    jumpSound.pitch = originalPitch;
                    Debug.Log("<color=magenta>🔊 DOUBLE JUMP SOUND PLAYED (Fallback with higher pitch)</color>");
                }
            }
        }
        
        public static void PlayPlayerWallJump(float volume = 1f)
        {
            PlayPlayerWallJump(GetPlayerPosition(), volume);
        }
        
        public static void PlayPlayerWallJump(Vector3 position, float volume = 1f)
        {
            // Play dedicated wall jump sound if available
            if (SafeEvents?.wallJumpSounds != null && SafeEvents.wallJumpSounds.Length > 0)
            {
                SafePlaySound3D(SafeEvents.wallJumpSounds, position, volume);
                Debug.Log("<color=cyan>🔊 WALL JUMP SOUND PLAYED</color>");
            }
            // Fallback to regular jump sound if no wall jump sounds configured
            else if (SafeEvents?.jumpSounds != null && SafeEvents.jumpSounds.Length > 0)
            {
                SafePlaySound3D(SafeEvents.jumpSounds, position, volume);
                Debug.Log("<color=cyan>🔊 WALL JUMP SOUND PLAYED (Jump Sound Fallback)</color>");
            }
        }
        
        /// <summary>
        /// Play satisfying XP notification sound for wall jumps with pitch scaling based on chain level.
        /// Higher chains = higher pitch = more satisfying feedback!
        /// </summary>
        /// <param name="position">3D position to play the sound</param>
        /// <param name="chainLevel">Current chain level (1-10+) - affects pitch</param>
        /// <param name="volume">Volume multiplier (default 1.0)</param>
        public static void PlayWallJumpXPNotification(Vector3 position, int chainLevel, float volume = 1f)
        {
            if (!IsSystemReady || SafeEvents?.wallJumpXPNotification == null) 
            {
                Debug.LogWarning("🔊 Wall Jump XP Notification sound not configured in SoundEvents");
                return;
            }
            
            // Calculate pitch based on chain level
            // Chain 1: pitch 1.0 (base)
            // Chain 2: pitch 1.1
            // Chain 3: pitch 1.2
            // Chain 10+: pitch 1.9 (capped)
            float basePitch = SafeEvents.wallJumpXPNotification.pitch;
            float pitchIncrease = (chainLevel - 1) * 0.1f; // +0.1 per chain level
            float finalPitch = Mathf.Clamp(basePitch + pitchIncrease, basePitch, basePitch + 0.9f);
            
            // Store original pitch
            float originalPitch = SafeEvents.wallJumpXPNotification.pitch;
            
            // Temporarily set the pitch
            SafeEvents.wallJumpXPNotification.pitch = finalPitch;
            
            // Play the sound
            SafeEvents.wallJumpXPNotification.Play3D(position, volume);
            
            // Restore original pitch
            SafeEvents.wallJumpXPNotification.pitch = originalPitch;
            
            Debug.Log($"<color=yellow>🎵 WALL JUMP XP NOTIFICATION PLAYED - Chain x{chainLevel}, Pitch: {finalPitch:F2}</color>");
        }
        
        /// <summary>
        /// 🔥 Play EPIC trick start sound (bass thump slide down with slow-motion!)
        /// This is the moment that makes players feel like a GOD!
        /// </summary>
        /// <param name="position">3D position to play the sound</param>
        /// <param name="volume">Volume multiplier</param>
        public static void PlayTrickStartSound(Vector3 position, float volume = 1f)
        {
            if (!IsSystemReady || SafeEvents?.trickStartSound == null)
            {
                Debug.LogWarning("🔊 Trick Start sound not configured in SoundEvents");
                return;
            }
            
            // Play the EPIC bass thump!
            SafeEvents.trickStartSound.Play3D(position, volume);
            
            Debug.Log($"<color=cyan>🔥 TRICK START SOUND! BASS THUMP WITH SLOW-MO!</color>");
        }
        
        /// <summary>
        /// Play trick landing sound based on trick size/awesomeness
        /// </summary>
        /// <param name="position">3D position to play the sound</param>
        /// <param name="airtime">How long in the air</param>
        /// <param name="rotations">Number of full rotations</param>
        /// <param name="isPerfect">Was it a perfect landing?</param>
        /// <param name="volume">Volume multiplier</param>
        public static void PlayTrickLandingSound(Vector3 position, float airtime, int rotations, bool isPerfect, float volume = 1f)
        {
            if (!IsSystemReady) return;
            
            // Determine trick tier
            SoundEvent trickSound = null;
            string tierName = "";
            
            if (airtime > 3f || rotations >= 4)
            {
                trickSound = SafeEvents?.trickLandingGodlike;
                tierName = "GODLIKE";
            }
            else if (airtime > 2f || rotations >= 3)
            {
                trickSound = SafeEvents?.trickLandingInsane;
                tierName = "INSANE";
            }
            else if (airtime > 1.5f || rotations >= 2)
            {
                trickSound = SafeEvents?.trickLandingBig;
                tierName = "BIG";
            }
            else if (airtime > 1f || rotations >= 1)
            {
                trickSound = SafeEvents?.trickLandingMedium;
                tierName = "MEDIUM";
            }
            else
            {
                trickSound = SafeEvents?.trickLandingSmall;
                tierName = "SMALL";
            }
            
            // Play trick sound
            if (trickSound != null)
            {
                trickSound.Play3D(position, volume);
                Debug.Log($"<color=magenta>🎪 {tierName} TRICK LANDING SOUND PLAYED!</color>");
            }
            
            // Play perfect landing bonus sound if applicable
            if (isPerfect && SafeEvents?.perfectLandingBonus != null)
            {
                SafeEvents.perfectLandingBonus.Play3D(position, volume * 0.8f);
                Debug.Log($"<color=gold>⭐ PERFECT LANDING BONUS SOUND!</color>");
            }
        }
        
        /// <summary>
        /// Play EPIC combo multiplier sound (wall jump + trick combo!)
        /// </summary>
        /// <param name="position">3D position to play the sound</param>
        /// <param name="multiplier">Combo multiplier value</param>
        /// <param name="volume">Volume multiplier</param>
        public static void PlayComboMultiplierSound(Vector3 position, float multiplier, float volume = 1f)
        {
            if (!IsSystemReady || SafeEvents?.comboMultiplierSound == null) return;
            
            // Pitch scales with multiplier (higher multiplier = higher pitch!)
            float basePitch = SafeEvents.comboMultiplierSound.pitch;
            float pitchBoost = (multiplier - 1f) * 0.2f; // +0.2 per multiplier level
            float finalPitch = Mathf.Clamp(basePitch + pitchBoost, basePitch, basePitch + 1.0f);
            
            // Store original
            float originalPitch = SafeEvents.comboMultiplierSound.pitch;
            
            // Set pitch
            SafeEvents.comboMultiplierSound.pitch = finalPitch;
            
            // Play
            SafeEvents.comboMultiplierSound.Play3D(position, volume);
            
            // Restore
            SafeEvents.comboMultiplierSound.pitch = originalPitch;
            
            Debug.Log($"<color=orange>🔥 COMBO MULTIPLIER x{multiplier:F1} SOUND! Pitch: {finalPitch:F2}</color>");
        }
        
        public static void PlayPlayerLand(float volume = 1f)
        {
            PlayPlayerLand(GetPlayerPosition(), volume);
        }
        
        public static void PlayPlayerLand(Vector3 position, float volume = 1f)
        {
            SafePlaySound3D(SafeEvents?.landSounds, position, volume);
        }
        
        public static void PlayPlayerFootstep(string surfaceType = "default", float volume = 1f)
        {
            SafePlaySound3D(SafeEvents?.footstepSounds, GetPlayerPosition(), volume);
        }
        
        public static void PlayPlayerSlideStart(float volume = 1f)
        {
            // Use footstep as fallback for slide sounds
            PlayPlayerFootstep("slide", volume);
        }
        
        public static void PlayPlayerCrouch(float volume = 1f)
        {
            // Use footstep as fallback for crouch sounds
            PlayPlayerFootstep("crouch", volume);
        }
        
        public static void PlayPlayerDeath(Vector3 position, float volume = 1f)
        {
            // Use the dedicated player death sounds
            if (SafeEvents?.playerDeath != null && SafeEvents.playerDeath.Length > 0)
            {
                SafePlaySound3D(SafeEvents.playerDeath, position, volume);
                Debug.Log("<color=red>🔊 PLAYER DEATH SOUND PLAYED</color>");
            }
            else
            {
                // Fallback to skull spawn sounds only if player death sounds aren't configured
                SafePlaySound3D(SafeEvents?.skullSpawn, position, volume);
                Debug.Log("<color=red>🔊 PLAYER DEATH SOUND PLAYED (Skull Spawn Fallback)</color>");
            }
        }
        
        // ===== BLEEDING OUT SOUNDS =====
        
        private static SoundHandle currentBleedingOutBreathingHandle = SoundHandle.Invalid;
        private static SoundHandle currentBleedingOutHeartbeatHandle = SoundHandle.Invalid;
        
        public static void StartBleedingOutSounds(Transform player)
        {
            if (!IsSystemReady || player == null) return;
            
            // Play bleeding out start sound (one-shot)
            if (SafeEvents.bleedingOutStart != null && SafeEvents.bleedingOutStart.Length > 0)
            {
                SafePlaySound3D(SafeEvents.bleedingOutStart, player.position, 1f);
                Debug.Log("<color=red>🩸 BLEEDING OUT START SOUND PLAYED</color>");
            }
            
            // Start labored breathing loop (reuse outOfBreathLoop or use dedicated)
            SoundEvent breathingSound = SafeEvents.bleedingOutBreathingLoop ?? SafeEvents.outOfBreathLoop;
            if (breathingSound != null)
            {
                // Stop any existing breathing sound first
                if (currentBleedingOutBreathingHandle.IsValid)
                {
                    currentBleedingOutBreathingHandle.Stop();
                }
                
                currentBleedingOutBreathingHandle = breathingSound.PlayAttached(player, 1f);
                Debug.Log("<color=yellow>💨 BLEEDING OUT BREATHING LOOP STARTED</color>");
            }
            
            // Start heartbeat loop
            if (SafeEvents.bleedingOutHeartbeatLoop != null)
            {
                // Stop any existing heartbeat sound first
                if (currentBleedingOutHeartbeatHandle.IsValid)
                {
                    currentBleedingOutHeartbeatHandle.Stop();
                }
                
                currentBleedingOutHeartbeatHandle = SafeEvents.bleedingOutHeartbeatLoop.PlayAttached(player, 0.5f);
                Debug.Log("<color=red>💓 BLEEDING OUT HEARTBEAT LOOP STARTED</color>");
            }
        }
        
        public static void StopBleedingOutSounds()
        {
            // Stop breathing loop
            if (currentBleedingOutBreathingHandle.IsValid)
            {
                currentBleedingOutBreathingHandle.Stop();
                currentBleedingOutBreathingHandle = SoundHandle.Invalid;
                Debug.Log("<color=green>✅ BLEEDING OUT BREATHING STOPPED</color>");
            }
            
            // Stop heartbeat loop
            if (currentBleedingOutHeartbeatHandle.IsValid)
            {
                currentBleedingOutHeartbeatHandle.Stop();
                currentBleedingOutHeartbeatHandle = SoundHandle.Invalid;
                Debug.Log("<color=green>✅ BLEEDING OUT HEARTBEAT STOPPED</color>");
            }
        }
        
        public static void PlayBleedingOutCrawlSound(Vector3 position, float volume = 0.3f)
        {
            // Optional struggle sounds when crawling
            if (SafeEvents?.bleedingOutCrawl != null && SafeEvents.bleedingOutCrawl.Length > 0)
            {
                SafePlaySound3D(SafeEvents.bleedingOutCrawl, position, volume);
            }
        }
        
        public static void UpdateBleedingOutHeartbeatIntensity(float normalizedTimeRemaining)
        {
            // Intensify heartbeat as time runs out (1.0 = full time, 0.0 = time up)
            if (currentBleedingOutHeartbeatHandle.IsValid)
            {
                // Volume increases as time decreases: 0.5 → 1.5
                float volume = Mathf.Lerp(1.5f, 0.5f, normalizedTimeRemaining);
                currentBleedingOutHeartbeatHandle.SetVolume(volume);
                
                // Pitch increases slightly as time decreases: 1.0 → 1.2 (faster heartbeat)
                float pitch = Mathf.Lerp(1.2f, 1.0f, normalizedTimeRemaining);
                currentBleedingOutHeartbeatHandle.SetPitch(pitch);
            }
        }
        
        public static void PlayArmorBreak(Vector3 position, float volume = 1f)
        {
            // Play armor break sound
            if (SafeEvents?.armorBreak != null && SafeEvents.armorBreak.Length > 0)
            {
                SafePlaySound3D(SafeEvents.armorBreak, position, volume);
                Debug.Log("<color=cyan>🔊 ARMOR BREAK SOUND PLAYED</color>");
            }
            else
            {
                Debug.LogWarning("🔊 Armor break sound not configured in SoundEvents");
            }
        }
        
        // Track the current armor plate apply sound handle for interruption
        private static SoundHandle currentArmorApplyHandle = SoundHandle.Invalid;
        
        public static void PlayArmorPlateApply(Vector3 position, float volume = 1f)
        {
            // Stop any currently playing armor apply sound (for rapid applications)
            if (currentArmorApplyHandle.IsValid)
            {
                currentArmorApplyHandle.Stop();
                Debug.Log("<color=cyan>🔊 INTERRUPTED previous armor plate apply sound</color>");
            }
            
            // Play new armor plate apply sound
            if (SafeEvents?.armorPlateApply != null)
            {
                currentArmorApplyHandle = SafePlaySound3D(SafeEvents.armorPlateApply, position, volume);
                Debug.Log("<color=cyan>🔊 ARMOR PLATE APPLY SOUND PLAYED</color>");
            }
            else
            {
                Debug.LogWarning("🔊 Armor plate apply sound not configured in SoundEvents");
            }
        }
        
        public static void PlayPlayerSpawn(Vector3 position, float volume = 1f)
        {
            // Map to power-up start sounds for player spawn (positive effect)
            if (SafeEvents?.powerUpStart != null && SafeEvents.powerUpStart.Length > 0)
            {
                SafePlaySound3D(SafeEvents.powerUpStart, position, volume);
            }
            else
            {
                // Fallback to UI click for spawn notification
                SafePlaySound3D(SafeEvents?.uiClick, position, volume);
            }
        }
        
        public static void PlayFallDamage(Vector3 position, float volume = 1f)
        {
            // Play fall damage sound
            if (SafeEvents?.fallDamage != null && SafeEvents.fallDamage.Length > 0)
            {
                SafePlaySound3D(SafeEvents.fallDamage, position, volume);
                Debug.Log("<color=red>🔊 FALL DAMAGE SOUND PLAYED</color>");
            }
            else
            {
                Debug.LogWarning("🔊 Fall damage sound not configured in SoundEvents");
            }
        }
        
        public static void PlayPlayerHit(Vector3 position, float volume = 1f)
        {
            // Play random player hit sound (with built-in cooldown to prevent spam from rapid hits)
            if (SafeEvents?.playerHit != null && SafeEvents.playerHit.Length > 0)
            {
                SafePlaySound3D(SafeEvents.playerHit, position, volume);
                Debug.Log("<color=yellow>🔊 PLAYER HIT SOUND PLAYED (Random)</color>");
            }
            else
            {
                Debug.LogWarning("🔊 Player hit sounds not configured in SoundEvents");
            }
        }
        
        // Track the current falling wind sound handle
        private static SoundHandle currentFallingWindHandle = SoundHandle.Invalid;
        
        public static SoundHandle StartFallingWindLoop(Transform parent, float volume = 1f)
        {
            // Stop any existing falling wind sound first
            if (currentFallingWindHandle.IsValid)
            {
                currentFallingWindHandle.Stop();
            }
            
            // Start new falling wind loop
            if (SafeEvents?.fallingWindLoop != null)
            {
                currentFallingWindHandle = SafeEvents.fallingWindLoop.PlayAttached(parent, volume);
                Debug.Log("<color=cyan>🔊 FALLING WIND LOOP STARTED</color>");
                return currentFallingWindHandle;
            }
            else
            {
                Debug.LogWarning("🔊 Falling wind loop sound not configured in SoundEvents");
                return SoundHandle.Invalid;
            }
        }
        
        public static void StopFallingWindLoop()
        {
            if (currentFallingWindHandle.IsValid)
            {
                currentFallingWindHandle.Stop();
                currentFallingWindHandle = SoundHandle.Invalid;
                Debug.Log("<color=cyan>🔊 FALLING WIND LOOP STOPPED</color>");
            }
        }
        
        // ===== COMBAT SOUNDS =====
        
        public static void PlayProjectileHit(Vector3 position, float volume = 1f)
        {
            SafePlaySound3D(SafeEvents?.daggerHit, position, volume);
        }
        
        public static void PlaySoundAtPoint(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip != null)
            {
                AudioSource.PlayClipAtPoint(clip, position, volume);
            }
        }
        
        // ===== BOSS SOUNDS =====
        
        public static void PlayBossAwaken(Vector3 position, float volume = 1f)
        {
            // Use consistent SoundEvent.Play pattern - map to enemy spawn sounds
            if (SoundEventsManager.Events?.skullSpawn != null && SoundEventsManager.Events.skullSpawn.Length > 0)
            {
                SoundEventsManager.Quick.PlayRandomSound3D(SoundEventsManager.Events.skullSpawn, position, volume);
            }
        }
        
        public static void PlayBossDeath(Vector3 position, float volume = 1f)
        {
            // Use consistent SoundEvent.Play pattern - map to enemy death sounds
            if (SoundEventsManager.Events?.skullKill != null && SoundEventsManager.Events.skullKill.Length > 0)
            {
                SoundEventsManager.Quick.PlayRandomSound3D(SoundEventsManager.Events.skullKill, position, volume);
            }
        }
        
        // ===== UI SOUNDS =====
        
        public static SoundHandle PlayUIClick(float volume = 1f)
        {
            if (SafeEvents?.uiClick != null)
            {
                return SafeEvents.uiClick.Play2D(volume);
            }

            return SoundHandle.Invalid;
        }

        public static void PlayUIFeedback(Vector3 position, float volume = 1f)
        {
            if (SoundEventsManager.Events?.uiFeedback != null && SoundEventsManager.Events.uiFeedback.Length > 0)
            {
                SoundEventsManager.Quick.PlayRandomSound2D(SoundEventsManager.Events.uiFeedback, volume);
            }
            else if (SoundEventsManager.Events?.uiClick != null)
            {
                SoundEventsManager.Events.uiClick.Play2D(volume);
            }
        }
        
        // ===== PLATFORM SOUNDS =====
        
        public static void PlayPlatformActivate(Vector3 position, float volume = 1f)
        {
            if (SoundEventsManager.Events?.platformActivation != null && SoundEventsManager.Events.platformActivation.Length > 0)
            {
                SoundEventsManager.Quick.PlayRandomSound3D(SoundEventsManager.Events.platformActivation, position, volume);
            }
            else if (SoundEventsManager.Events?.uiClick != null)
            {
                SoundEventsManager.Events.uiClick.Play3D(position, volume);
            }
        }
        
        // ===== POWER-UP SOUNDS =====
        
        public static void PlayPowerUpStart(Vector3 position, float volume = 1f)
        {
            if (SoundEventsManager.Events?.powerUpStart != null && SoundEventsManager.Events.powerUpStart.Length > 0)
            {
                SoundEventsManager.Quick.PlayRandomSound3D(SoundEventsManager.Events.powerUpStart, position, volume);
            }
            else if (SoundEventsManager.Events?.uiClick != null)
            {
                SoundEventsManager.Events.uiClick.Play3D(position, volume);
            }
        }
        
        public static void PlayPowerUpEnd(Vector3 position, float volume = 1f)
        {
            if (SoundEventsManager.Events?.powerUpEnd != null && SoundEventsManager.Events.powerUpEnd.Length > 0)
            {
                SoundEventsManager.Quick.PlayRandomSound3D(SoundEventsManager.Events.powerUpEnd, position, volume);
            }
            else if (SoundEventsManager.Events?.uiClick != null)
            {
                SoundEventsManager.Events.uiClick.Play3D(position, volume);
            }
        }
        
        // Specific Power-Up Sound Methods
        
        public static void PlayMaxHandUpgradeSound(Vector3 position, float volume = 1f)
        {
            if (SafeEvents?.maxHandUpgradeSounds != null && SafeEvents.maxHandUpgradeSounds.Length > 0)
            {
                SafePlaySound3D(SafeEvents.maxHandUpgradeSounds, position, volume);
            }
            else
            {
                // Fallback to general power-up start sound
                PlayPowerUpStart(position, volume);
            }
        }
        
        public static void PlayHomingDaggersSound(Vector3 position, float volume = 1f)
        {
            if (SafeEvents?.homingDaggersSounds != null && SafeEvents.homingDaggersSounds.Length > 0)
            {
                SafePlaySound3D(SafeEvents.homingDaggersSounds, position, volume);
            }
            else
            {
                // Fallback to general power-up start sound
                PlayPowerUpStart(position, volume);
            }
        }
        
        public static void PlayAOEAttackSound(Vector3 position, float volume = 1f)
        {
            if (SafeEvents?.aoeAttackSounds != null && SafeEvents.aoeAttackSounds.Length > 0)
            {
                SafePlaySound3D(SafeEvents.aoeAttackSounds, position, volume);
            }
            else
            {
                // Fallback to general power-up start sound
                PlayPowerUpStart(position, volume);
            }
        }
        
        public static void PlayDoubleGemsSound(Vector3 position, float volume = 1f)
        {
            if (SafeEvents?.doubleGemsSounds != null && SafeEvents.doubleGemsSounds.Length > 0)
            {
                SafePlaySound3D(SafeEvents.doubleGemsSounds, position, volume);
            }
            else
            {
                // Fallback to general power-up start sound
                PlayPowerUpStart(position, volume);
            }
        }
        
        public static void PlaySlowTimeSound(Vector3 position, float volume = 1f)
        {
            if (SafeEvents?.slowTimeSounds != null && SafeEvents.slowTimeSounds.Length > 0)
            {
                SafePlaySound3D(SafeEvents.slowTimeSounds, position, volume);
            }
            else
            {
                // Fallback to general power-up start sound
                PlayPowerUpStart(position, volume);
            }
        }
        
        public static void PlayGodModeSound(Vector3 position, float volume = 1f)
        {
            if (SafeEvents?.godModeSounds != null && SafeEvents.godModeSounds.Length > 0)
            {
                SafePlaySound3D(SafeEvents.godModeSounds, position, volume);
            }
            else
            {
                // Fallback to general power-up start sound
                PlayPowerUpStart(position, volume);
            }
        }
        
        // PowerUp Type-based sound selection
        public static void PlayPowerUpSound(PowerUpType powerUpType, Vector3 position, float volume = 1f)
        {
            Debug.Log($"<color=cyan>🔊 GameSounds: Playing PowerUp sound for {powerUpType} at position {position}</color>");
            
            // Debug info - check if SoundEvents is properly loaded
            if (SafeEvents == null)
            {
                Debug.LogError($"<color=red>🔊 GameSounds: ERROR - SafeEvents is null! Sound system not initialized properly.</color>");
                return;
            }
            
            // Print detailed diagnostic info
            Debug.Log($"<color=cyan>SoundEvents arrays status:</color>\n" +
                      $"- maxHandUpgradeSounds: {(SafeEvents.maxHandUpgradeSounds != null ? SafeEvents.maxHandUpgradeSounds.Length + " items" : "null")}\n" +
                      $"- homingDaggersSounds: {(SafeEvents.homingDaggersSounds != null ? SafeEvents.homingDaggersSounds.Length + " items" : "null")}\n" +
                      $"- aoeAttackSounds: {(SafeEvents.aoeAttackSounds != null ? SafeEvents.aoeAttackSounds.Length + " items" : "null")}\n" +
                      $"- doubleGemsSounds: {(SafeEvents.doubleGemsSounds != null ? SafeEvents.doubleGemsSounds.Length + " items" : "null")}\n" +
                      $"- slowTimeSounds: {(SafeEvents.slowTimeSounds != null ? SafeEvents.slowTimeSounds.Length + " items" : "null")}\n" +
                      $"- godModeSounds: {(SafeEvents.godModeSounds != null ? SafeEvents.godModeSounds.Length + " items" : "null")}");

            switch (powerUpType)
            {
                case PowerUpType.MaxHandUpgrade:
                    Debug.Log($"<color=cyan>🔊 Playing MaxHandUpgrade sound effect</color>");
                    PlayMaxHandUpgradeSound(position, volume);
                    break;
                case PowerUpType.HomingDaggers:
                    Debug.Log($"<color=cyan>🔊 Playing HomingDaggers sound effect</color>");
                    PlayHomingDaggersSound(position, volume);
                    break;
                case PowerUpType.AOEAttack:
                    Debug.Log($"<color=cyan>🔊 Playing AOEAttack sound effect</color>");
                    PlayAOEAttackSound(position, volume);
                    break;
                case PowerUpType.DoubleGems:
                    Debug.Log($"<color=cyan>🔊 Playing DoubleGems sound effect</color>");
                    PlayDoubleGemsSound(position, volume);
                    break;
                case PowerUpType.SlowTime:
                    Debug.Log($"<color=cyan>🔊 Playing SlowTime sound effect</color>");
                    PlaySlowTimeSound(position, volume);
                    break;
                case PowerUpType.GodMode:
                    Debug.Log($"<color=cyan>🔊 Playing GodMode sound effect</color>");
                    PlayGodModeSound(position, volume);
                    break;
                default:
                    // Default to general power-up sound
                    Debug.Log($"<color=cyan>🔊 Playing default power-up sound (type not recognized)</color>");
                    PlayPowerUpStart(position, volume);
                    break;
            }
        }
        
        // ===== AOE SOUNDS =====
        
        public static void PlayAOEActivation(Vector3 position, float volume = 1f)
        {
            // Use dagger hit sounds for AOE activation
            if (SoundEventsManager.Events?.daggerHit != null && SoundEventsManager.Events.daggerHit.Length > 0)
            {
                SoundEventsManager.Quick.PlayRandomSound3D(SoundEventsManager.Events.daggerHit, position, volume);
            }
        }
        
        // ===== GEM SOUNDS =====
        
        public static void PlayGemCollection(Vector3 position, float volume = 1f)
        {
            // Use consistent SoundEvent.Play pattern
            if (SoundEventsManager.Events?.gemCollection != null && SoundEventsManager.Events.gemCollection.Length > 0)
            {
                SoundEventsManager.Quick.PlayRandomSound3D(SoundEventsManager.Events.gemCollection, position, volume);
            }
        }
        
        public static void PlayGemHit(Vector3 position, float volume = 1f)
        {
            SafePlaySound3D(SafeEvents?.gemHit, position, volume);
        }
        
        public static void PlayGemDetach(Vector3 position, float volume = 1f)
        {
            // Use consistent SoundEvent.Play pattern
            if (SoundEventsManager.Events?.gemDetach != null && SoundEventsManager.Events.gemDetach.Length > 0)
            {
                SoundEventsManager.Quick.PlayRandomSound3D(SoundEventsManager.Events.gemDetach, position, volume);
            }
        }
        
        public static void PlayGemSpawn(Vector3 position, float volume = 1f)
        {
            // Use consistent SoundEvent.Play pattern
            if (SoundEventsManager.Events?.gemSpawn != null && SoundEventsManager.Events.gemSpawn.Length > 0)
            {
                SoundEventsManager.Quick.PlayRandomSound3D(SoundEventsManager.Events.gemSpawn, position, volume);
            }
        }
        
        public static void StartGemHumming(Transform parent, float volume = 1f)
        {
            if (SoundEventsManager.Events?.gemHumming != null)
            {
                SoundEventsManager.Events.gemHumming.PlayAttached(parent, volume);
                Debug.Log($"🎵 Playing gem humming sound on {parent.name}");
            }
        }
        
        // ===== TOWER SOUNDS =====
        
        public static void PlayTowerShoot(Vector3 position, float volume = 1f)
        {
            if (SoundEventsManager.Events?.towerShoot != null && SoundEventsManager.Events.towerShoot.Length > 0)
            {
                var randomSound = SoundEventsManager.Events.towerShoot[UnityEngine.Random.Range(0, SoundEventsManager.Events.towerShoot.Length)];
                randomSound?.Play3D(position, volume);
                Debug.Log($"🎵 Playing tower shoot sound at {position}");
            }
        }
        
        public static void PlayTowerWakeup(Vector3 position, float volume = 1f)
        {
            // Map to towerAppear sounds
            if (SoundEventsManager.Events?.towerAppear != null && SoundEventsManager.Events.towerAppear.Length > 0)
            {
                var randomSound = SoundEventsManager.Events.towerAppear[UnityEngine.Random.Range(0, SoundEventsManager.Events.towerAppear.Length)];
                if (randomSound?.clip != null)
                {
                    SoundSystemCore.Instance?.PlaySound3D(randomSound.clip, position, SoundCategory.SFX, volume);
                    Debug.Log($"🎵 [DIRECT] Playing tower wake up: {randomSound.clip.name}");
                }
            }
        }
        
        public static void PlayTowerDeath(Vector3 position, float volume = 1f)
        {
            // Map to towerCollapse sounds
            if (SoundEventsManager.Events?.towerCollapse != null && SoundEventsManager.Events.towerCollapse.Length > 0)
            {
                var randomSound = SoundEventsManager.Events.towerCollapse[UnityEngine.Random.Range(0, SoundEventsManager.Events.towerCollapse.Length)];
                if (randomSound?.clip != null)
                {
                    SoundSystemCore.Instance?.PlaySound3D(randomSound.clip, position, SoundCategory.SFX, volume);
                    Debug.Log($"🎵 [DIRECT] Playing tower death: {randomSound.clip.name}");
                }
            }
        }
        
        public static void PlayTowerAppear(Vector3 position, float volume = 1f)
        {
            // Use consistent SoundEvent.Play pattern
            if (SoundEventsManager.Events?.towerAppear != null && SoundEventsManager.Events.towerAppear.Length > 0)
            {
                SoundEventsManager.Quick.PlayRandomSound3D(SoundEventsManager.Events.towerAppear, position, volume);
            }
        }
        
        public static void PlayTowerCollapse(Vector3 position, float volume = 1f)
        {
            // Use consistent SoundEvent.Play pattern
            if (SoundEventsManager.Events?.towerCollapse != null && SoundEventsManager.Events.towerCollapse.Length > 0)
            {
                SoundEventsManager.Quick.PlayRandomSound3D(SoundEventsManager.Events.towerCollapse, position, volume);
            }
        }
        
        public static void StartTowerIdle(Transform parent, float volume = 1f)
        {
            // Map to tower appear sounds for idle
            if (SoundEventsManager.Events?.towerAppear != null && SoundEventsManager.Events.towerAppear.Length > 0)
            {
                SoundEventsManager.Events.towerAppear[0]?.PlayAttached(parent, volume);
                Debug.Log($"🎵 Playing tower idle sound on {parent.name}");
            }
        }
        
        // ===== ENEMY SOUNDS =====
        
        public static void PlayEnemySpawn(Vector3 position, float volume = 1f)
        {
            // Use consistent SoundEvent.Play pattern - map to skull spawn sounds
            if (SoundEventsManager.Events?.skullSpawn != null && SoundEventsManager.Events.skullSpawn.Length > 0)
            {
                SoundEventsManager.Quick.PlayRandomSound3D(SoundEventsManager.Events.skullSpawn, position, volume);
            }
        }
        
        public static void PlayEnemyDeath(Vector3 position, float volume = 1f)
        {
            // Map to skull kill sounds for enemy death
            SafePlaySound3D(SafeEvents?.skullKill, position, volume);
        }
        
        // ===== PROGRESSION SOUNDS =====
        
        public static void PlayLevelUp(Vector3 position, float volume = 1f)
        {
            // Use consistent SoundEvent.Play pattern - map to power up start sounds
            if (SoundEventsManager.Events?.powerUpStart != null && SoundEventsManager.Events.powerUpStart.Length > 0)
            {
                SoundEventsManager.Quick.PlayRandomSound3D(SoundEventsManager.Events.powerUpStart, position, volume);
            }
        }
        
        public static void PlayExperienceGain(Vector3 position, float volume = 1f)
        {
            // Map to gem collection sounds
            if (SoundEventsManager.Events?.gemCollection != null && SoundEventsManager.Events.gemCollection.Length > 0)
            {
                SoundEventsManager.Quick.PlayRandomSound3D(SoundEventsManager.Events.gemCollection, position, volume);
            }
        }
        
        // ===== EQUIPMENT SOUNDS =====
        
        public static void PlayHelmetEnergyCritical(Vector3 position, float volume = 1f)
        {
            // Map to UI click sound for critical alerts
            if (SoundEventsManager.Events?.uiClick != null)
            {
                SoundEventsManager.Events.uiClick.Play3D(position, volume);
            }
        }
        
        public static void PlayHelmetEnergyRestored(Vector3 position, float volume = 1f)
        {
            // Map to power up end sounds for restoration
            if (SoundEventsManager.Events?.powerUpEnd != null && SoundEventsManager.Events.powerUpEnd.Length > 0)
            {
                SoundEventsManager.Quick.PlayRandomSound3D(SoundEventsManager.Events.powerUpEnd, position, volume);
            }
        }
        
        // ===== COGNITIVE FEED SOUNDS =====
        
        public static void PlayCognitiveFeedWord(Vector3 position, float volume = 1f)
        {
            // Map to UI feedback sounds
            if (SoundEventsManager.Events?.uiFeedback != null && SoundEventsManager.Events.uiFeedback.Length > 0)
            {
                SoundEventsManager.Quick.PlayRandomSound3D(SoundEventsManager.Events.uiFeedback, position, volume);
            }
            else if (SoundEventsManager.Events?.uiClick != null)
            {
                SoundEventsManager.Events.uiClick.Play3D(position, volume);
            }
        }
        
        // ===== SLIDING SOUNDS =====
        
        public static void StartSlidingLoop(Transform parent, float volume = 1f)
        {
            // Map to footstep sounds for sliding loop
            if (SoundEventsManager.Events?.footstepSounds != null && SoundEventsManager.Events.footstepSounds.Length > 0)
            {
                SoundEventsManager.Quick.PlayMovementSound(SoundEventsManager.Events.footstepSounds[0], parent, volume);
            }
        }
        
        public static void PlaySlidingStart(Vector3 position, float volume = 1f)
        {
            // Map to footstep sounds for sliding start
            if (SoundEventsManager.Events?.footstepSounds != null && SoundEventsManager.Events.footstepSounds.Length > 0)
            {
                SoundEventsManager.Quick.PlayRandomSound3D(SoundEventsManager.Events.footstepSounds, position, volume);
            }
        }
        
        public static void PlaySlidingEnd(Vector3 position, float volume = 1f)
        {
            // Map to land sounds for sliding end
            if (SoundEventsManager.Events?.landSounds != null && SoundEventsManager.Events.landSounds.Length > 0)
            {
                SoundEventsManager.Quick.PlayRandomSound3D(SoundEventsManager.Events.landSounds, position, volume);
            }
        }
        
        // ===== SKULL-SPECIFIC SOUNDS =====
        
        public static void PlayEnemyAttack(Vector3 position, float volume = 1f)
        {
            if (SoundEventsManager.Events?.skullChatter != null && SoundEventsManager.Events.skullChatter.Length > 0)
            {
                SoundEventsManager.Quick.PlayRandomSound3D(SoundEventsManager.Events.skullChatter, position, volume);
            }
        }
        
        public static void PlayEnemyMovement(Vector3 position, float volume = 1f)
        {
            if (SoundEventsManager.Events?.skullMovement != null && SoundEventsManager.Events.skullMovement.Length > 0)
            {
                SoundEventsManager.Quick.PlayRandomSound3D(SoundEventsManager.Events.skullMovement, position, volume);
            }
        }
        
        public static void PlayEnemyBreathing(Vector3 position, float volume = 1f)
        {
            SafePlaySound3D(SafeEvents?.skullChatter, position, volume);
        }
        
        /// <summary>
        /// Start looping enemy breathing sound for a specific transform
        /// </summary>
        public static void StartEnemyBreathing(Transform enemyTransform, float volume = 1f)
        {
            if (enemyTransform == null || SafeEvents?.skullChatter == null || SafeEvents.skullChatter.Length == 0)
                return;
                
            // Stop any existing breathing for this enemy
            StopEnemyBreathing(enemyTransform);
            
            // Get a random breathing sound and play it as looping
            var breathingSound = SoundEvents.GetRandomSound(SafeEvents.skullChatter);
            if (breathingSound?.clip != null)
            {
                var handle = SoundSystemCore.Instance?.PlaySound3D(breathingSound.clip, enemyTransform.position, 
                    breathingSound.category, volume * breathingSound.volume, 1f, true); // loop = true
                    
                if (handle != null && handle.IsValid)
                {
                    activeBreathingSounds[enemyTransform] = handle;
                    
                    // Make the sound follow the enemy
                    if (handle.AudioSource?.followTransform == null)
                    {
                        handle.AudioSource.followTransform = enemyTransform;
                    }
                }
            }
        }
        
        public static void StopEnemyBreathing()
        {
            // Stop all active breathing sounds
            var keysToRemove = new List<Transform>();
            
            foreach (var kvp in activeBreathingSounds)
            {
                if (kvp.Value.IsValid)
                {
                    kvp.Value.Stop();
                }
                keysToRemove.Add(kvp.Key);
            }
            
            foreach (var key in keysToRemove)
            {
                activeBreathingSounds.Remove(key);
            }
        }
        
        /// <summary>
        /// Stop breathing sound for a specific enemy transform (implementation)
        /// </summary>
        private static void StopEnemyBreathingInternal(Transform enemyTransform)
        {
            if (enemyTransform == null) return;
            
            if (activeBreathingSounds.TryGetValue(enemyTransform, out var handle))
            {
                if (handle.IsValid) handle.Stop();
                activeBreathingSounds.Remove(enemyTransform);
            }
        }
        
        public static void StopEnemyBreathing(Transform enemyTransform)
        {
            if (enemyTransform == null) return;
            
            StopEnemyBreathingInternal(enemyTransform);
            
            // Add log for this public version
            Debug.Log($"Stopping enemy breathing for {enemyTransform.name}");
        }
        
        // ===== ADDITIONAL SOUNDS =====
        
        public static void PlaySkullSpawn(Vector3 position, float volume = 1f)
        {
            if (SoundEventsManager.Events?.skullSpawn != null && SoundEventsManager.Events.skullSpawn.Length > 0)
            {
                SoundEventsManager.Quick.PlayRandomSound3D(SoundEventsManager.Events.skullSpawn, position, volume);
            }
        }
        
        public static void PlayTowerCharge(Vector3 position, float volume = 1f)
        {
            // Map to tower appear sounds for charging
            if (SoundEventsManager.Events?.towerAppear != null && SoundEventsManager.Events.towerAppear.Length > 0)
            {
                SoundEventsManager.Quick.PlayRandomSound3D(SoundEventsManager.Events.towerAppear, position, volume);
            }
        }
        
        // PlayPowerUpStart method is already defined earlier in this file
        
        // ===== UTILITY METHODS =====
        
        private static Vector3 GetPlayerPosition()
        {
            GameObject player = GameObject.FindWithTag("Player");
            return player != null ? player.transform.position : Vector3.zero;
        }
        
        // ═══════════════════ FLIGHT SOUNDS ═══════════════════
        
        /// <summary>
        /// Play flight activation sound when player starts flying
        /// </summary>
        public static void PlayFlightActivation(Vector3 position, float volume = 1f)
        {
            if (SafeEvents?.flightActivation != null)
            {
                SafePlaySound3D(SafeEvents.flightActivation, position, volume);
            }
        }
        
        /// <summary>
        /// Start flight loop sound attached to transform
        /// </summary>
        public static SoundHandle StartFlightLoop(Transform parent, float volume = 1f)
        {
            if (SafeEvents?.flightLoop != null && parent != null)
            {
                return SafeEvents.flightLoop.PlayAttached(parent, volume);
            }
            return SoundHandle.Invalid;
        }
        
        /// <summary>
        /// Start flight loop sound at position
        /// </summary>
        public static SoundHandle StartFlightLoop(Vector3 position, float volume = 1f)
        {
            if (SafeEvents?.flightLoop != null)
            {
                return SafeEvents.flightLoop.Play3D(position, volume);
            }
            return SoundHandle.Invalid;
        }
        
        /// <summary>
        /// Start vertical movement loop sound attached to transform (Space/LeftControl during CelestialDrift)
        /// </summary>
        public static SoundHandle StartVerticalMovementLoop(Transform parent, float volume = 1f)
        {
            if (SafeEvents?.verticalMovementLoop != null && parent != null)
            {
                return SafeEvents.verticalMovementLoop.PlayAttached(parent, volume);
            }
            return SoundHandle.Invalid;
        }
        
        /// <summary>
        /// Start vertical movement loop sound at position
        /// </summary>
        public static SoundHandle StartVerticalMovementLoop(Vector3 position, float volume = 1f)
        {
            if (SafeEvents?.verticalMovementLoop != null)
            {
                return SafeEvents.verticalMovementLoop.Play3D(position, volume);
            }
            return SoundHandle.Invalid;
        }
    }
}
