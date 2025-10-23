using UnityEngine;
using GeminiGauntlet.Audio;

/// <summary>
/// AAA Forge Sound Manager - Proximity-based ambient humming for forge
/// Uses centralized spatial audio system with automatic distance-based cleanup
/// INCLUDES FALLBACK AUDIO SOURCE FOR RELIABILITY
/// Audible from 1500 units away with no Doppler effect
/// </summary>
public class ForgeSoundManager : MonoBehaviour
{
    [Header("Volume Settings")]
    [SerializeField] [Range(0f, 1f)] private float hummingVolume = 0.6f;
    
    [Header("Humming Settings")]
    [Tooltip("Distance at which forge humming becomes audible")]
    [SerializeField] private float minHummingDistance = 50f;
    [Tooltip("Distance at which forge humming is at full volume")]
    [SerializeField] private float maxHummingDistance = 200f;
    [Tooltip("Distance at which forge humming auto-stops (cleanup) - 1500 units")]
    [SerializeField] private float maxAudibleDistance = 1500f;
    
    [Header("Fallback Audio (Direct AudioSource)")]
    [Tooltip("Audio clip to use if SoundEvents system is not available")]
    [SerializeField] private AudioClip fallbackHummingClip;
    [Tooltip("Enable detailed debug logging")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // Track the humming sound handle for proper cleanup
    private SoundHandle hummingHandle = SoundHandle.Invalid;
    private bool isHumming = false;
    
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
        fallbackAudioSource.minDistance = minHummingDistance;
        fallbackAudioSource.maxDistance = maxAudibleDistance;
        fallbackAudioSource.rolloffMode = AudioRolloffMode.Linear; // Smooth linear falloff like AAA games
        fallbackAudioSource.dopplerLevel = 0f; // NO DOPPLER EFFECT (critical for ambient sounds)
        fallbackAudioSource.spread = 0f; // Directional sound (not omnidirectional)
        fallbackAudioSource.volume = hummingVolume;
        fallbackAudioSource.priority = 128; // Medium priority (0=highest, 256=lowest)
        
        DebugLog("[ForgeSoundManager] ✅ Initialized with AAA fallback AudioSource (no Doppler, smooth distance falloff, 1500 unit range)");
    }
    
    void Start()
    {
        // Auto-start humming when forge is loaded
        StartForgeHumming();
    }
    
    /// <summary>
    /// Start playing forge humming sound (looped) with AAA spatial audio
    /// Automatically tracked for distance-based cleanup
    /// INCLUDES FALLBACK MECHANISM
    /// </summary>
    public void StartForgeHumming()
    {
        DebugLog($"[ForgeSoundManager] 🎵 StartForgeHumming called on {gameObject.name}");
        
        // Stop any existing hum
        if (hummingHandle.IsValid || (fallbackAudioSource != null && fallbackAudioSource.isPlaying))
        {
            DebugLog("[ForgeSoundManager] Stopping existing humming before starting new one");
            StopForgeHumming();
        }

        // Try to use the advanced sound system first
        bool advancedSystemWorked = TryStartAdvancedHumming();
        
        if (!advancedSystemWorked)
        {
            // Fall back to direct AudioSource
            DebugLog("[ForgeSoundManager] ⚠️ Advanced sound system failed, using fallback AudioSource");
            StartFallbackHumming();
        }
    }
    
    /// <summary>
    /// Try to start humming using the advanced sound system
    /// </summary>
    private bool TryStartAdvancedHumming()
    {
        // Check if SoundEventsManager exists
        if (SoundEventsManager.Events == null)
        {
            DebugLog("[ForgeSoundManager] ❌ SoundEventsManager.Events is NULL");
            return false;
        }
        
        var hummingEvent = SoundEventsManager.Events.forgeHumming;
        if (hummingEvent == null)
        {
            DebugLog("[ForgeSoundManager] ❌ forgeHumming SoundEvent is NULL in SoundEvents");
            return false;
        }
        
        if (hummingEvent.clip == null)
        {
            DebugLog("[ForgeSoundManager] ❌ forgeHumming clip is NULL");
            return false;
        }
        
        // Check if SoundSystemCore exists
        if (SoundSystemCore.Instance == null)
        {
            DebugLog("[ForgeSoundManager] ❌ SoundSystemCore.Instance is NULL");
            return false;
        }

        // Use AAA spatial audio profile for proper 3D positioning (Fortnite/COD style)
        var profile = SpatialAudioProfiles.GenericSFX;
        profile.profileName = "Forge Humming";
        profile.minDistance = minHummingDistance;
        profile.maxDistance = maxHummingDistance;
        profile.maxAudibleDistance = maxAudibleDistance;
        profile.spread = 0f;                // Directional (not omnidirectional) - AAA standard
        profile.dopplerLevel = 0f;          // NO DOPPLER - ambient sounds don't need it
        profile.priority = SoundPriority.Low;
        profile.distanceCheckInterval = 0.3f;
        profile.distanceCullFadeOut = 0.8f; // Smooth fade out

        hummingHandle = SoundSystemCore.Instance.PlaySoundAttachedWithProfile(
            hummingEvent.clip,
            transform,
            profile,
            hummingVolume * hummingEvent.volume,  // Multiply by SoundEvent volume
            hummingEvent.pitch,
            true  // Loop
        );

        if (hummingHandle.IsValid)
        {
            isHumming = true;
            usingFallbackAudio = false;
            DebugLog($"[ForgeSoundManager] ✅ Started forge humming (ADVANCED) at {transform.name} (cleanup at {profile.maxAudibleDistance}m)");
            return true;
        }
        else
        {
            DebugLog("[ForgeSoundManager] ❌ PlaySoundAttachedWithProfile returned invalid handle");
            return false;
        }
    }
    
    /// <summary>
    /// Start humming using fallback AudioSource
    /// </summary>
    private void StartFallbackHumming()
    {
        if (fallbackAudioSource == null)
        {
            Debug.LogError("[ForgeSoundManager] ❌ Fallback AudioSource is NULL!");
            return;
        }
        
        // Try to get clip from SoundEvents first
        AudioClip clipToPlay = null;
        
        if (SoundEventsManager.Events?.forgeHumming?.clip != null)
        {
            clipToPlay = SoundEventsManager.Events.forgeHumming.clip;
            DebugLog("[ForgeSoundManager] Using clip from SoundEvents");
        }
        else if (fallbackHummingClip != null)
        {
            clipToPlay = fallbackHummingClip;
            DebugLog("[ForgeSoundManager] Using fallback clip from inspector");
        }
        else
        {
            Debug.LogError("[ForgeSoundManager] ❌ NO AUDIO CLIP AVAILABLE! Please assign fallbackHummingClip in inspector or configure SoundEvents!");
            return;
        }
        
        fallbackAudioSource.clip = clipToPlay;
        fallbackAudioSource.volume = hummingVolume;
        fallbackAudioSource.Play();
        
        isHumming = true;
        usingFallbackAudio = true;
        
        DebugLog($"[ForgeSoundManager] ✅ Started forge humming (FALLBACK) at {transform.name}");
    }
    
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log(message);
        }
    }
    
    /// <summary>
    /// Stop playing forge humming sound with smooth fade
    /// </summary>
    public void StopForgeHumming()
    {
        DebugLog($"[ForgeSoundManager] 🛑 StopForgeHumming called on {gameObject.name}");
        
        // Stop advanced sound system
        if (hummingHandle.IsValid)
        {
            hummingHandle.FadeOut(0.5f); // Slightly longer fade for forge
            SpatialAudioManager.Instance?.UntrackSound(hummingHandle);
            hummingHandle = SoundHandle.Invalid;
            DebugLog($"[ForgeSoundManager] ✅ Stopped forge humming (ADVANCED) at {transform.name}");
        }
        
        // Stop fallback audio source
        if (fallbackAudioSource != null && fallbackAudioSource.isPlaying)
        {
            StartCoroutine(FadeOutFallbackAudio(0.5f));
            DebugLog($"[ForgeSoundManager] ✅ Stopped forge humming (FALLBACK) at {transform.name}");
        }
        
        isHumming = false;
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
        fallbackAudioSource.volume = hummingVolume; // Reset volume for next play
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
    
    /// <summary>
    /// Check if forge is currently humming
    /// </summary>
    public bool IsHumming => isHumming && (hummingHandle.IsValid || (fallbackAudioSource != null && fallbackAudioSource.isPlaying));
    
    void OnDisable()
    {
        // Stop humming when forge is disabled
        StopForgeHumming();
    }
    
    void OnDestroy()
    {
        // Cleanup humming sound when forge is destroyed
        StopForgeHumming();
    }
    
    // ========== DEBUG CONTEXT MENU COMMANDS ==========
    
    [ContextMenu("🎵 TEST: Start Humming NOW")]
    public void DebugStartHumming()
    {
        Debug.Log($"[DEBUG] Manually starting humming on {gameObject.name}");
        StartForgeHumming();
    }
    
    [ContextMenu("🛑 TEST: Stop Humming NOW")]
    public void DebugStopHumming()
    {
        Debug.Log($"[DEBUG] Manually stopping humming on {gameObject.name}");
        StopForgeHumming();
    }
    
    [ContextMenu("🔍 TEST: Check Audio Status")]
    public void DebugCheckStatus()
    {
        Debug.Log($"========== FORGE AUDIO DEBUG: {gameObject.name} ==========");
        Debug.Log($"Is Humming: {isHumming}");
        Debug.Log($"Using Fallback: {usingFallbackAudio}");
        Debug.Log($"Handle Valid: {hummingHandle.IsValid}");
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
            Debug.Log($"forgeHumming SoundEvent: {(SoundEventsManager.Events.forgeHumming != null ? "EXISTS" : "NULL")}");
            
            if (SoundEventsManager.Events.forgeHumming != null)
            {
                Debug.Log($"forgeHumming Clip: {(SoundEventsManager.Events.forgeHumming.clip != null ? SoundEventsManager.Events.forgeHumming.clip.name : "NULL")}");
            }
        }
        
        Debug.Log($"SoundSystemCore.Instance: {(SoundSystemCore.Instance != null ? "EXISTS" : "NULL")}");
        Debug.Log($"Fallback Clip Assigned: {(fallbackHummingClip != null ? fallbackHummingClip.name : "NULL")}");
        Debug.Log($"====================================================");
    }
    
    [ContextMenu("⚡ FORCE: Enable Debug Logs")]
    public void ForceEnableDebugLogs()
    {
        enableDebugLogs = true;
        Debug.Log($"[ForgeSoundManager] Debug logs ENABLED for {gameObject.name}");
    }
}
