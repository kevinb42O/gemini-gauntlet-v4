using UnityEngine;
using GeminiGauntlet.Audio;

/// <summary>
/// AAA Speaker Music Manager - Proximity-based music playback for speaker cubes
/// Uses centralized spatial audio system with automatic distance-based cleanup
/// INCLUDES FALLBACK AUDIO SOURCE FOR RELIABILITY
/// </summary>
public class SpeakerMusicManager : MonoBehaviour
{
    [Header("Volume Settings")]
    [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.7f;
    
    [Header("Music Settings")]
    [Tooltip("Distance at which speaker music becomes audible")]
    [SerializeField] private float minMusicDistance = 5f;
    [Tooltip("Distance at which speaker music is at full volume")]
    [SerializeField] private float maxMusicDistance = 20f;
    [Tooltip("Distance at which speaker music auto-stops (cleanup)")]
    [SerializeField] private float maxAudibleDistance = 50f;
    
    [Header("Fallback Audio (Direct AudioSource)")]
    [Tooltip("Audio clip to use if SoundEvents system is not available")]
    [SerializeField] private AudioClip fallbackMusicClip;
    [Tooltip("Enable detailed debug logging")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // Track the music sound handle for proper cleanup
    private SoundHandle musicHandle = SoundHandle.Invalid;
    private bool isPlaying = false;
    
    // Fallback AudioSource for direct playback
    private AudioSource fallbackAudioSource;
    private bool usingFallbackAudio = false;
    
    void Awake()
    {
        // Create fallback audio source with AAA game settings (Fortnite/COD style)
        fallbackAudioSource = gameObject.AddComponent<AudioSource>();
        fallbackAudioSource.playOnAwake = false;
        fallbackAudioSource.loop = true;
        fallbackAudioSource.spatialBlend = 1f; // Full 3D
        fallbackAudioSource.minDistance = minMusicDistance;
        fallbackAudioSource.maxDistance = maxAudibleDistance;
        fallbackAudioSource.rolloffMode = AudioRolloffMode.Linear; // Smooth linear falloff like AAA games
        fallbackAudioSource.dopplerLevel = 0f; // NO DOPPLER EFFECT (critical for music)
        fallbackAudioSource.spread = 0f; // Directional sound (not omnidirectional)
        fallbackAudioSource.volume = musicVolume;
        fallbackAudioSource.priority = 128; // Medium priority (0=highest, 256=lowest)
        
        DebugLog("[SpeakerMusicManager] ✅ Initialized with AAA fallback AudioSource (no Doppler, smooth distance falloff)");
    }
    
    void Start()
    {
        // Auto-start music when speaker cube is loaded
        StartSpeakerMusic();
    }
    
    /// <summary>
    /// Start playing speaker music (looped) with AAA spatial audio
    /// Automatically tracked for distance-based cleanup
    /// INCLUDES FALLBACK MECHANISM
    /// </summary>
    public void StartSpeakerMusic()
    {
        DebugLog($"[SpeakerMusicManager] 🎵 StartSpeakerMusic called on {gameObject.name}");
        
        // Stop any existing music
        if (musicHandle.IsValid || (fallbackAudioSource != null && fallbackAudioSource.isPlaying))
        {
            DebugLog("[SpeakerMusicManager] Stopping existing music before starting new one");
            StopSpeakerMusic();
        }

        // Try to use the advanced sound system first
        bool advancedSystemWorked = TryStartAdvancedMusic();
        
        if (!advancedSystemWorked)
        {
            // Fall back to direct AudioSource
            DebugLog("[SpeakerMusicManager] ⚠️ Advanced sound system failed, using fallback AudioSource");
            StartFallbackMusic();
        }
    }
    
    /// <summary>
    /// Try to start music using the advanced sound system
    /// </summary>
    private bool TryStartAdvancedMusic()
    {
        // Check if SoundEventsManager exists
        if (SoundEventsManager.Events == null)
        {
            DebugLog("[SpeakerMusicManager] ❌ SoundEventsManager.Events is NULL");
            return false;
        }
        
        var musicEvent = SoundEventsManager.Events.speakerMusic;
        if (musicEvent == null)
        {
            DebugLog("[SpeakerMusicManager] ❌ speakerMusic SoundEvent is NULL in SoundEvents");
            return false;
        }
        
        if (musicEvent.clip == null)
        {
            DebugLog("[SpeakerMusicManager] ❌ speakerMusic clip is NULL");
            return false;
        }
        
        // Check if SoundSystemCore exists
        if (SoundSystemCore.Instance == null)
        {
            DebugLog("[SpeakerMusicManager] ❌ SoundSystemCore.Instance is NULL");
            return false;
        }

        // Use AAA spatial audio profile for proper 3D positioning (Fortnite/COD style)
        var profile = SpatialAudioProfiles.GenericSFX;
        profile.profileName = "Speaker Music";
        profile.minDistance = minMusicDistance;
        profile.maxDistance = maxMusicDistance;
        profile.maxAudibleDistance = maxAudibleDistance;
        profile.spread = 0f;                // Directional (not omnidirectional) - AAA standard
        profile.dopplerLevel = 0f;          // NO DOPPLER - music doesn't need it
        profile.priority = SoundPriority.Low;
        profile.distanceCheckInterval = 0.3f;
        profile.distanceCullFadeOut = 0.8f; // Smooth fade out

        musicHandle = SoundSystemCore.Instance.PlaySoundAttachedWithProfile(
            musicEvent.clip,
            transform,
            profile,
            musicVolume * musicEvent.volume,  // Multiply by SoundEvent volume
            musicEvent.pitch,
            true  // Loop
        );

        if (musicHandle.IsValid)
        {
            isPlaying = true;
            usingFallbackAudio = false;
            DebugLog($"[SpeakerMusicManager] ✅ Started speaker music (ADVANCED) at {transform.name} (cleanup at {profile.maxAudibleDistance}m)");
            return true;
        }
        else
        {
            DebugLog("[SpeakerMusicManager] ❌ PlaySoundAttachedWithProfile returned invalid handle");
            return false;
        }
    }
    
    /// <summary>
    /// Start music using fallback AudioSource
    /// </summary>
    private void StartFallbackMusic()
    {
        if (fallbackAudioSource == null)
        {
            Debug.LogError("[SpeakerMusicManager] ❌ Fallback AudioSource is NULL!");
            return;
        }
        
        // Try to get clip from SoundEvents first
        AudioClip clipToPlay = null;
        
        if (SoundEventsManager.Events?.speakerMusic?.clip != null)
        {
            clipToPlay = SoundEventsManager.Events.speakerMusic.clip;
            DebugLog("[SpeakerMusicManager] Using clip from SoundEvents");
        }
        else if (fallbackMusicClip != null)
        {
            clipToPlay = fallbackMusicClip;
            DebugLog("[SpeakerMusicManager] Using fallback clip from inspector");
        }
        else
        {
            Debug.LogError("[SpeakerMusicManager] ❌ NO AUDIO CLIP AVAILABLE! Please assign fallbackMusicClip in inspector or configure SoundEvents!");
            return;
        }
        
        fallbackAudioSource.clip = clipToPlay;
        fallbackAudioSource.volume = musicVolume;
        fallbackAudioSource.Play();
        
        isPlaying = true;
        usingFallbackAudio = true;
        
        DebugLog($"[SpeakerMusicManager] ✅ Started speaker music (FALLBACK) at {transform.name}");
    }
    
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log(message);
        }
    }
    
    /// <summary>
    /// Stop playing speaker music with smooth fade
    /// </summary>
    public void StopSpeakerMusic()
    {
        DebugLog($"[SpeakerMusicManager] 🛑 StopSpeakerMusic called on {gameObject.name}");
        
        // Stop advanced sound system
        if (musicHandle.IsValid)
        {
            musicHandle.FadeOut(1f); // Longer fade for music
            SpatialAudioManager.Instance?.UntrackSound(musicHandle);
            musicHandle = SoundHandle.Invalid;
            DebugLog($"[SpeakerMusicManager] ✅ Stopped speaker music (ADVANCED) at {transform.name}");
        }
        
        // Stop fallback audio source
        if (fallbackAudioSource != null && fallbackAudioSource.isPlaying)
        {
            StartCoroutine(FadeOutFallbackAudio(1f));
            DebugLog($"[SpeakerMusicManager] ✅ Stopped speaker music (FALLBACK) at {transform.name}");
        }
        
        isPlaying = false;
        usingFallbackAudio = false;
    }
    
    /// <summary>
    /// Fade out the fallback audio source
    /// </summary>
    private System.Collections.IEnumerator FadeOutFallbackAudio(float duration)
    {
        if (fallbackAudioSource == null) yield break;
        
        float startVolume = fallbackAudioSource.volume;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fallbackAudioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }
        
        fallbackAudioSource.Stop();
        fallbackAudioSource.volume = musicVolume; // Reset volume for next play
    }
    
    /// <summary>
    /// Set music volume dynamically (adjusts the looping sound)
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        
        if (musicHandle.IsValid)
        {
            musicHandle.SetVolume(musicVolume);
        }
        
        if (fallbackAudioSource != null && fallbackAudioSource.isPlaying)
        {
            fallbackAudioSource.volume = musicVolume;
        }
    }
    
    /// <summary>
    /// Check if speaker is currently playing music
    /// </summary>
    public bool IsPlaying => isPlaying && (musicHandle.IsValid || (fallbackAudioSource != null && fallbackAudioSource.isPlaying));
    
    void OnDisable()
    {
        // Stop music when speaker is disabled
        StopSpeakerMusic();
    }
    
    void OnDestroy()
    {
        // Cleanup music when speaker is destroyed
        StopSpeakerMusic();
    }
    
    // ========== DEBUG CONTEXT MENU COMMANDS ==========
    
    [ContextMenu("🎵 TEST: Start Music NOW")]
    public void DebugStartMusic()
    {
        Debug.Log($"[DEBUG] Manually starting music on {gameObject.name}");
        StartSpeakerMusic();
    }
    
    [ContextMenu("🛑 TEST: Stop Music NOW")]
    public void DebugStopMusic()
    {
        Debug.Log($"[DEBUG] Manually stopping music on {gameObject.name}");
        StopSpeakerMusic();
    }
    
    [ContextMenu("🔍 TEST: Check Audio Status")]
    public void DebugCheckStatus()
    {
        Debug.Log($"========== SPEAKER AUDIO DEBUG: {gameObject.name} ==========");
        Debug.Log($"Is Playing: {isPlaying}");
        Debug.Log($"Using Fallback: {usingFallbackAudio}");
        Debug.Log($"Handle Valid: {musicHandle.IsValid}");
        Debug.Log($"Fallback AudioSource: {(fallbackAudioSource != null ? "EXISTS" : "NULL")}");
        
        if (fallbackAudioSource != null)
        {
            Debug.Log($"Fallback Playing: {fallbackAudioSource.isPlaying}");
            Debug.Log($"Fallback Clip: {(fallbackAudioSource.clip != null ? fallbackAudioSource.clip.name : "NULL")}");
            Debug.Log($"Fallback Volume: {fallbackAudioSource.volume}");
            Debug.Log($"Fallback Spatial Blend: {fallbackAudioSource.spatialBlend}");
            Debug.Log($"Fallback Max Distance: {fallbackAudioSource.maxDistance}");
        }
        
        Debug.Log($"SoundEventsManager.Events: {(SoundEventsManager.Events != null ? "EXISTS" : "NULL")}");
        
        if (SoundEventsManager.Events != null)
        {
            Debug.Log($"speakerMusic SoundEvent: {(SoundEventsManager.Events.speakerMusic != null ? "EXISTS" : "NULL")}");
            
            if (SoundEventsManager.Events.speakerMusic != null)
            {
                Debug.Log($"speakerMusic Clip: {(SoundEventsManager.Events.speakerMusic.clip != null ? SoundEventsManager.Events.speakerMusic.clip.name : "NULL")}");
            }
        }
        
        Debug.Log($"SoundSystemCore.Instance: {(SoundSystemCore.Instance != null ? "EXISTS" : "NULL")}");
        Debug.Log($"Fallback Clip Assigned: {(fallbackMusicClip != null ? fallbackMusicClip.name : "NULL")}");
        Debug.Log($"====================================================");
    }
    
    [ContextMenu("⚡ FORCE: Enable Debug Logs")]
    public void ForceEnableDebugLogs()
    {
        enableDebugLogs = true;
        Debug.Log($"[SpeakerMusicManager] Debug logs ENABLED for {gameObject.name}");
    }
}
