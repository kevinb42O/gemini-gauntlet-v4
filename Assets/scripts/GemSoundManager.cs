using UnityEngine;
using GeminiGauntlet.Audio;

/// <summary>
/// AAA Gem Sound Manager - Uses centralized spatial audio system
/// Proper 3D distance handling, automatic cleanup, no more infinite hums!
/// </summary>
public class GemSoundManager : MonoBehaviour
{
    [Header("Volume Settings")]
    [SerializeField] [Range(0f, 1f)] private float collectionVolume = 0.8f;
    [SerializeField] [Range(0f, 1f)] private float hummingVolume = 0.5f;
    [SerializeField] [Range(0f, 1f)] private float spawnVolume = 0.6f;
    [SerializeField] [Range(0f, 1f)] private float hitVolume = 0.7f;
    [SerializeField] [Range(0f, 1f)] private float detachVolume = 0.9f;
    
    // Track the humming sound handle for proper cleanup
    private SoundHandle hummingHandle = SoundHandle.Invalid;
    
    /// <summary>
    /// Play gem collection sound
    /// </summary>
    public void PlayGemCollection()
    {
        // Use GameSounds system to route through mixer
        GameSounds.PlayGemCollection(transform.position, collectionVolume);
    }
    
    /// <summary>
    /// Start playing gem humming sound (looped) with AAA spatial audio
    /// Automatically tracked for distance-based cleanup
    /// </summary>
    public void StartGemHumming()
    {
        // Stop any existing hum
        if (hummingHandle.IsValid)
        {
            StopGemHumming();
        }

        if (SoundEventsManager.Events?.gemHumming == null)
        {
            Debug.LogWarning("[GemSoundManager] Gem humming sound not configured");
            return;
        }

        var hummingEvent = SoundEventsManager.Events.gemHumming;
        if (hummingEvent?.clip == null)
        {
            Debug.LogWarning("[GemSoundManager] Gem humming clip is null");
            return;
        }

        // Use AAA spatial audio profile for proper 3D positioning
        var profile = SpatialAudioProfiles.GenericSFX;
        profile.profileName = "Gem Humming";
        profile.minDistance = 800f;         // ~2.5 character heights
        profile.maxDistance = 4000f;        // ~1.25 platforms
        profile.maxAudibleDistance = 6000f; // Auto-cleanup at ~2 platforms
        profile.spread = 45f;               // Ambient spread
        profile.priority = SoundPriority.Low;
        profile.distanceCheckInterval = 0.3f;
        profile.distanceCullFadeOut = 0.5f;

        hummingHandle = SoundSystemCore.Instance?.PlaySoundAttachedWithProfile(
            hummingEvent.clip,
            transform,
            profile,
            hummingVolume * hummingEvent.volume,  // Multiply by SoundEvent volume
            hummingEvent.pitch,
            true  // Loop
        ) ?? SoundHandle.Invalid;

        if (hummingHandle.IsValid)
        {
            Debug.Log($"[GemSoundManager] 💎 Started gem humming (will auto-cleanup at {profile.maxAudibleDistance}m)");
        }
    }
    
    /// <summary>
    /// Stop playing gem humming sound with smooth fade
    /// FIXED: Actually stops the sound now!
    /// </summary>
    public void StopGemHumming()
    {
        if (hummingHandle.IsValid)
        {
            hummingHandle.FadeOut(0.3f);
            SpatialAudioManager.Instance?.UntrackSound(hummingHandle);
            hummingHandle = SoundHandle.Invalid;
            
            Debug.Log("[GemSoundManager] 💎 Stopped gem humming (faded out)");
        }
    }
    
    /// <summary>
    /// Play gem spawn sound
    /// </summary>
    public void PlayGemSpawn()
    {
        // Use GameSounds system to route through mixer
        GameSounds.PlayGemSpawn(transform.position, spawnVolume);
    }
    
    /// <summary>
    /// Play gem hit sound
    /// </summary>
    public void PlayGemHit()
    {
        // Use GameSounds system to route through mixer
        GameSounds.PlayGemHit(transform.position, hitVolume);
    }
    
    /// <summary>
    /// Play gem detach sound
    /// </summary>
    public void PlayGemDetach()
    {
        // Use GameSounds system to route through mixer
        GameSounds.PlayGemDetach(transform.position, detachVolume);
    }
    
    /// <summary>
    /// Set humming volume dynamically (adjusts the looping sound)
    /// </summary>
    public void SetHummingVolume(float volume)
    {
        hummingVolume = Mathf.Clamp01(volume);
        
        if (hummingHandle.IsValid)
        {
            hummingHandle.SetVolume(hummingVolume);
        }
    }
    
    void OnDestroy()
    {
        // Cleanup humming sound when gem is destroyed
        StopGemHumming();
    }
}
