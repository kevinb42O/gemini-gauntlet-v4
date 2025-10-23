using UnityEngine;

namespace GeminiGauntlet.Audio
{
    /// <summary>
    /// AAA Spatial Audio System for Skull Enemies
    /// Proper 3D distance handling, automatic cleanup, crystal-clear positioning
    /// Each skull plays its own looping chatter sound attached to its transform
    /// 
    /// PERFORMANCE: Global chatter limit prevents audio pool exhaustion with 100+ skulls
    /// Only the closest skulls to the player can chatter (max 20 concurrent chatters)
    /// </summary>
    public static class SkullSoundEvents
    {
        private static SoundEvents Events => SoundEventsManager.Events;
        
        // PERFORMANCE: Track active chatter sounds to prevent pool exhaustion
        private static System.Collections.Generic.Dictionary<Transform, SoundHandle> activeChatterSounds = new System.Collections.Generic.Dictionary<Transform, SoundHandle>();
        private const int MAX_CONCURRENT_CHATTERS = 20; // Only 20 skulls can chatter at once
        
        /// <summary>
        /// Start skull chatter sound attached to the skull transform
        /// Uses AAA spatial audio profile with automatic distance-based volume
        /// Each skull gets its own looping chatter sound for directional awareness
        /// 
        /// PERFORMANCE: Global limit of 20 concurrent chatters - only closest skulls chatter
        /// </summary>
        public static SoundHandle StartSkullChatter(Transform skullTransform, float volume = 1f)
        {
            if (skullTransform == null)
            {
                return SoundHandle.Invalid;
            }

            if (Events?.skullChatter == null || Events.skullChatter.Length == 0)
            {
                return SoundHandle.Invalid;
            }
            
            // PERFORMANCE: Check if we've hit the global chatter limit
            if (activeChatterSounds.Count >= MAX_CONCURRENT_CHATTERS)
            {
                // Find the farthest skull from the player and stop its chatter
                Transform farthestSkull = FindFarthestChatteringSkull();
                if (farthestSkull != null)
                {
                    // Stop the farthest skull's chatter to make room for this closer one
                    StopSkullChatter(farthestSkull);
                }
                else
                {
                    // Can't find a skull to replace, don't start new chatter
                    return SoundHandle.Invalid;
                }
            }

            // Get random chatter clip
            var chatterEvent = SoundEvents.GetRandomSound(Events.skullChatter);
            if (chatterEvent?.clip == null)
            {
                return SoundHandle.Invalid;
            }

            // Use AAA spatial audio profile for proper 3D positioning
            var profile = SpatialAudioProfiles.SkullChatter;
            var handle = SoundSystemCore.Instance?.PlaySoundAttachedWithProfile(
                chatterEvent.clip,
                skullTransform,
                profile,
                volume * chatterEvent.volume,  // Multiply by SoundEvent volume
                chatterEvent.pitch,
                true  // Loop
            );
            
            // Track this chatter sound
            if (handle != null && handle.IsValid)
            {
                activeChatterSounds[skullTransform] = handle;
            }

            return handle ?? SoundHandle.Invalid;
        }
        
        /// <summary>
        /// Stop skull chatter sound INSTANTLY - no fade, no delay
        /// When a skull dies, the sound must stop immediately for tight audio feedback
        /// </summary>
        public static void StopSkullChatter(SoundHandle chatterHandle)
        {
            if (chatterHandle != null && chatterHandle.IsValid)
            {
                // INSTANT stop - no fade (skull is dead, silence is immediate)
                chatterHandle.Stop();
                
                // Untrack from spatial audio manager
                SpatialAudioManager.Instance?.UntrackSound(chatterHandle);
                
                // Remove from active chatters tracking
                RemoveChatterHandle(chatterHandle);
            }
        }
        
        /// <summary>
        /// Stop skull chatter by skull transform (overload for convenience)
        /// </summary>
        public static void StopSkullChatter(Transform skullTransform)
        {
            if (skullTransform != null && activeChatterSounds.TryGetValue(skullTransform, out SoundHandle handle))
            {
                StopSkullChatter(handle);
            }
        }
        
        /// <summary>
        /// Find the skull that's farthest from the player (to stop its chatter)
        /// </summary>
        private static Transform FindFarthestChatteringSkull()
        {
            Transform player = Camera.main?.transform;
            if (player == null) return null;
            
            Transform farthestSkull = null;
            float maxDistance = 0f;
            
            // Find the skull farthest from the player
            foreach (var kvp in activeChatterSounds)
            {
                if (kvp.Key == null) continue; // Skip destroyed skulls
                
                float distance = Vector3.Distance(player.position, kvp.Key.position);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    farthestSkull = kvp.Key;
                }
            }
            
            return farthestSkull;
        }
        
        /// <summary>
        /// Remove a chatter handle from tracking (cleanup)
        /// </summary>
        private static void RemoveChatterHandle(SoundHandle handle)
        {
            // Find and remove the handle from the dictionary
            Transform keyToRemove = null;
            foreach (var kvp in activeChatterSounds)
            {
                if (kvp.Value == handle)
                {
                    keyToRemove = kvp.Key;
                    break;
                }
            }
            
            if (keyToRemove != null)
            {
                activeChatterSounds.Remove(keyToRemove);
            }
        }
        
        /// <summary>
        /// Get debug info about active chatters
        /// </summary>
        public static string GetDebugInfo()
        {
            // Clean up null entries first
            var keysToRemove = new System.Collections.Generic.List<Transform>();
            foreach (var kvp in activeChatterSounds)
            {
                if (kvp.Key == null || !kvp.Value.IsValid)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            foreach (var key in keysToRemove)
            {
                activeChatterSounds.Remove(key);
            }
            
            return $"{activeChatterSounds.Count}/{MAX_CONCURRENT_CHATTERS} skulls chattering";
        }
        
        /// <summary>
        /// Play skull death sound at the specified position
        /// Uses AAA spatial audio profile for proper 3D distance falloff
        /// </summary>
        public static void PlaySkullDeathSound(Vector3 position, float volume = 1f)
        {
            var candidates = Events?.skullKill;
            if ((candidates == null || candidates.Length == 0) && Events?.skullMovement != null)
            {
                candidates = Events.skullMovement; // fallback to general skull movement sounds
            }

            if (candidates == null || candidates.Length == 0)
            {
                Debug.LogWarning("[SkullSoundEvents] ⚠️ Skull death sound event not found");
                return;
            }

            // Get random death sound
            var deathEvent = SoundEvents.GetRandomSound(candidates);
            if (deathEvent?.clip == null)
            {
                Debug.LogWarning("[SkullSoundEvents] ⚠️ Skull death clip is null");
                return;
            }

            // Use AAA spatial audio profile
            var profile = SpatialAudioProfiles.SkullDeath;
            SoundSystemCore.Instance?.PlaySound3DWithProfile(
                deathEvent.clip,
                position,
                profile,
                volume * deathEvent.volume,  // Multiply by SoundEvent volume
                deathEvent.pitch,
                false  // One-shot
            );

            Debug.Log($"[SkullSoundEvents] 💀💥 Played skull death sound at {position} (audible up to {profile.maxAudibleDistance}m)");
        }
        
        /// <summary>
        /// Play skull attack sound at the specified position
        /// Uses AAA spatial audio profile for proper 3D distance falloff
        /// </summary>
        public static void PlaySkullAttackSound(Vector3 position, float volume = 1f)
        {
            var attackCandidates = Events?.skullMovement;

            if (attackCandidates == null || attackCandidates.Length == 0)
            {
                Debug.LogWarning("[SkullSoundEvents] ⚠️ Skull attack sound event not found");
                return;
            }

            // Get random attack sound
            var attackEvent = SoundEvents.GetRandomSound(attackCandidates);
            if (attackEvent?.clip == null)
            {
                Debug.LogWarning("[SkullSoundEvents] ⚠️ Skull attack clip is null");
                return;
            }

            // Use skull death profile for attack (similar audible range)
            var profile = SpatialAudioProfiles.SkullDeath;
            SoundSystemCore.Instance?.PlaySound3DWithProfile(
                attackEvent.clip,
                position,
                profile,
                volume * attackEvent.volume,  // Multiply by SoundEvent volume
                attackEvent.pitch,
                false  // One-shot
            );

            Debug.Log($"[SkullSoundEvents] 💀⚔️ Played skull attack sound at {position}");
        }
    }
}