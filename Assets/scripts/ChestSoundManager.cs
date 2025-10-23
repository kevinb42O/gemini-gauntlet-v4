using UnityEngine;
using GeminiGauntlet.Audio;

/// <summary>
/// AAA Chest Sound Manager - Proximity-based ambient humming for chests
/// Uses centralized spatial audio system with automatic distance-based cleanup
/// INCLUDES FALLBACK AUDIO SOURCE FOR RELIABILITY
/// </summary>
public class ChestSoundManager : MonoBehaviour
{
    [Header("Volume Settings")]
    [SerializeField] [Range(0f, 1f)] private float hummingVolume = 0.6f;
    [SerializeField] [Range(0f, 1f)] private float emergenceVolume = 0.8f;
    [SerializeField] [Range(0f, 1f)] private float openingVolume = 0.7f;
    
    [Header("Humming Settings")]
    [Tooltip("Distance at which chest humming becomes audible")]
    [SerializeField] private float minHummingDistance = 50f;
    [Tooltip("Distance at which chest humming is at full volume")]
    [SerializeField] private float maxHummingDistance = 500f;
    [Tooltip("Distance at which chest humming auto-stops (cleanup)")]
    [SerializeField] private float maxAudibleDistance = 1000f;
    
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
    
    // Retry mechanism for delayed sound system initialization
    private bool needsRetry = false;
    private float retryTimer = 0f;
    private const float RETRY_DELAY = 0.5f;
    private const int MAX_RETRIES = 5;
    private int retryCount = 0;
    
    void Awake()
    {
        // Create fallback audio source with AAA game settings (Fortnite/COD style)
        fallbackAudioSource = gameObject.AddComponent<AudioSource>();
        fallbackAudioSource.playOnAwake = false;
        fallbackAudioSource.loop = true;
        fallbackAudioSource.spatialBlend = 1f; // Full 3D
        fallbackAudioSource.minDistance = minHummingDistance;
        fallbackAudioSource.maxDistance = maxAudibleDistance;
        fallbackAudioSource.rolloffMode = AudioRolloffMode.Logarithmic; // Better for long distances
        fallbackAudioSource.dopplerLevel = 0f; // NO DOPPLER EFFECT (critical for ambient sounds)
        fallbackAudioSource.spread = 0f; // Directional sound (not omnidirectional)
        fallbackAudioSource.volume = hummingVolume;
        fallbackAudioSource.priority = 128; // Medium priority (0=highest, 256=lowest)
        
        DebugLog("[ChestSoundManager] ✅ Initialized with AAA fallback AudioSource (no Doppler, smooth distance falloff)");
    }
    
    void Update()
    {
        // Retry mechanism for delayed sound system initialization - ONLY during startup
        if (needsRetry && !isHumming && !usingFallbackAudio)
        {
            retryTimer += Time.deltaTime;
            if (retryTimer >= RETRY_DELAY)
            {
                retryTimer = 0f;
                retryCount++;
                
                if (retryCount <= MAX_RETRIES)
                {
                    DebugLog($"[ChestSoundManager] 🔄 Retry #{retryCount} - attempting to start humming...");
                    
                    // Try advanced system again
                    bool success = TryStartAdvancedHumming();
                    if (success)
                    {
                        needsRetry = false; // Stop retrying on success
                        retryCount = 0;
                    }
                }
                else
                {
                    DebugLog($"[ChestSoundManager] ❌ Max retries ({MAX_RETRIES}) reached - staying with fallback");
                    needsRetry = false;
                }
            }
        }
        
        // CRITICAL: Distance check for ALL audio (not just fallback)
        if (isHumming)
        {
            CheckDistanceAndStop();
        }
    }
    
    /// <summary>
    /// CRITICAL: Check distance to player and stop audio if too far
    /// This applies to BOTH advanced and fallback audio systems
    /// </summary>
    private void CheckDistanceAndStop()
    {
        Transform player = Camera.main?.transform;
        if (player == null) return;
        
        float distance = Vector3.Distance(transform.position, player.position);
        
        // Stop if player is beyond max audible distance
        if (distance > maxAudibleDistance)
        {
            DebugLog($"[ChestSoundManager] 🔇 Player too far ({distance:F1}m > {maxAudibleDistance}m) - stopping chest audio");
            StopChestHumming();
        }
    }
    
    /// <summary>
    /// Start playing chest humming sound (looped) with AAA spatial audio
    /// Automatically tracked for distance-based cleanup
    /// INCLUDES FALLBACK MECHANISM
    /// </summary>
    public void StartChestHumming()
    {
        DebugLog($"[ChestSoundManager] 🎵 StartChestHumming called on {gameObject.name}");
        
        // CRITICAL: Check distance before starting - don't start if player is too far
        Transform player = Camera.main?.transform;
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            if (distance > maxAudibleDistance)
            {
                DebugLog($"[ChestSoundManager] ⏭️ Player too far ({distance:F1}m > {maxAudibleDistance}m) - not starting chest audio");
                return; // Don't start if too far
            }
        }
        
        // Stop any existing hum
        if (hummingHandle.IsValid || (fallbackAudioSource != null && fallbackAudioSource.isPlaying))
        {
            DebugLog("[ChestSoundManager] Stopping existing humming before starting new one");
            StopChestHumming();
        }

        // Try to use the advanced sound system first
        bool advancedSystemWorked = TryStartAdvancedHumming();
        
        if (!advancedSystemWorked)
        {
            // Check if sound system is just not ready yet (vs. permanently unavailable)
            if (SoundEventsManager.Instance == null && retryCount < MAX_RETRIES)
            {
                DebugLog("[ChestSoundManager] ⏳ Sound system not ready yet - will retry...");
                needsRetry = true;
                retryTimer = 0f;
            }
            
            // Fall back to direct AudioSource
            DebugLog("[ChestSoundManager] ⚠️ Advanced sound system failed, using fallback AudioSource");
            StartFallbackHumming();
        }
        else
        {
            // Success! Disable retry mechanism
            needsRetry = false;
            retryCount = 0;
        }
    }
    
    /// <summary>
    /// Try to start humming using the advanced sound system
    /// </summary>
    private bool TryStartAdvancedHumming()
    {
        // Check if SoundEventsManager exists - WAIT for it if needed
        if (SoundEventsManager.Instance == null)
        {
            DebugLog("[ChestSoundManager] ❌ SoundEventsManager.Instance is NULL - system not initialized yet");
            return false;
        }
        
        if (SoundEventsManager.Events == null)
        {
            DebugLog("[ChestSoundManager] ❌ SoundEventsManager.Events is NULL - database not assigned");
            return false;
        }
        
        var hummingEvent = SoundEventsManager.Events.chestHumming;
        if (hummingEvent == null)
        {
            DebugLog("[ChestSoundManager] ❌ chestHumming SoundEvent is NULL in SoundEvents");
            return false;
        }
        
        if (hummingEvent.clip == null)
        {
            DebugLog("[ChestSoundManager] ❌ chestHumming clip is NULL");
            return false;
        }
        
        // Check if SoundSystemCore exists
        if (SoundSystemCore.Instance == null)
        {
            DebugLog("[ChestSoundManager] ❌ SoundSystemCore.Instance is NULL");
            return false;
        }

        // Use AAA spatial audio profile for proper 3D positioning (Fortnite/COD style)
        var profile = SpatialAudioProfiles.GenericSFX;
        profile.profileName = "Chest Humming";
        profile.minDistance = minHummingDistance;
        profile.maxDistance = maxHummingDistance;
        profile.maxAudibleDistance = maxAudibleDistance;
        profile.rolloffMode = AudioRolloffMode.Logarithmic; // Better for long distances
        profile.spread = 0f;                // Directional (not omnidirectional) - AAA standard
        profile.dopplerLevel = 0f;          // NO DOPPLER - ambient sounds don't need it
        profile.priority = SoundPriority.Low;
        profile.distanceCheckInterval = 0.3f;
        profile.distanceCullFadeOut = 0.8f; // Smooth fade out
        
        DebugLog($"[ChestSoundManager] 🔧 Profile: min={profile.minDistance}, max={profile.maxDistance}, audible={profile.maxAudibleDistance}, rolloff={profile.rolloffMode}");

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
            DebugLog($"[ChestSoundManager] ✅ Started chest humming (ADVANCED) at {transform.name} (cleanup at {profile.maxAudibleDistance}m)");
            return true;
        }
        else
        {
            DebugLog("[ChestSoundManager] ❌ PlaySoundAttachedWithProfile returned invalid handle");
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
            Debug.LogError("[ChestSoundManager] ❌ Fallback AudioSource is NULL!");
            return;
        }
        
        // CRITICAL: Update AudioSource distance settings (they may have changed since Awake)
        fallbackAudioSource.minDistance = minHummingDistance;
        fallbackAudioSource.maxDistance = maxAudibleDistance;
        DebugLog($"[ChestSoundManager] 🔧 Updated fallback AudioSource distances: min={minHummingDistance}, max={maxAudibleDistance}");
        
        // Try to get clip from SoundEvents first
        AudioClip clipToPlay = null;
        
        if (SoundEventsManager.Events?.chestHumming?.clip != null)
        {
            clipToPlay = SoundEventsManager.Events.chestHumming.clip;
            DebugLog("[ChestSoundManager] Using clip from SoundEvents");
        }
        else if (fallbackHummingClip != null)
        {
            clipToPlay = fallbackHummingClip;
            DebugLog("[ChestSoundManager] Using fallback clip from inspector");
        }
        else
        {
            Debug.LogError("[ChestSoundManager] ❌ NO AUDIO CLIP AVAILABLE! Please assign fallbackHummingClip in inspector or configure SoundEvents!");
            return;
        }
        
        fallbackAudioSource.clip = clipToPlay;
        fallbackAudioSource.volume = hummingVolume;
        fallbackAudioSource.Play();
        
        isHumming = true;
        usingFallbackAudio = true;
        
        DebugLog($"[ChestSoundManager] ✅ Started chest humming (FALLBACK) at {transform.name}");
    }
    
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log(message);
        }
    }
    
    /// <summary>
    /// Stop playing chest humming sound with smooth fade
    /// </summary>
    public void StopChestHumming()
    {
        DebugLog($"[ChestSoundManager] 🛑 StopChestHumming called on {gameObject.name}");
        
        // CRITICAL: Disable retry mechanism to prevent restart
        needsRetry = false;
        retryCount = 0;
        retryTimer = 0f;
        
        // Stop advanced sound system
        if (hummingHandle.IsValid)
        {
            hummingHandle.FadeOut(0.5f); // Slightly longer fade for chest
            SpatialAudioManager.Instance?.UntrackSound(hummingHandle);
            hummingHandle = SoundHandle.Invalid;
            DebugLog($"[ChestSoundManager] ✅ Stopped chest humming (ADVANCED) at {transform.name}");
        }
        
        // Stop fallback audio source
        if (fallbackAudioSource != null && fallbackAudioSource.isPlaying)
        {
            StartCoroutine(FadeOutFallbackAudio(0.5f));
            DebugLog($"[ChestSoundManager] ✅ Stopped chest humming (FALLBACK) at {transform.name}");
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
    /// Play chest emergence sound
    /// </summary>
    public void PlayChestEmergence()
    {
        GameSounds.PlayChestEmergence(transform.position, emergenceVolume);
    }
    
    /// <summary>
    /// Play chest opening sound
    /// </summary>
    public void PlayChestOpening()
    {
        GameSounds.PlayChestOpening(transform.position, openingVolume);
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
    /// Check if chest is currently humming
    /// </summary>
    public bool IsHumming => isHumming && (hummingHandle.IsValid || (fallbackAudioSource != null && fallbackAudioSource.isPlaying));
    
    void OnDisable()
    {
        // Stop humming when chest is disabled
        StopChestHumming();
    }
    
    void OnDestroy()
    {
        // Cleanup humming sound when chest is destroyed
        StopChestHumming();
    }
    
    // ========== DEBUG CONTEXT MENU COMMANDS ==========
    
    [ContextMenu("🎵 TEST: Start Humming NOW")]
    public void DebugStartHumming()
    {
        Debug.Log($"[DEBUG] Manually starting humming on {gameObject.name}");
        StartChestHumming();
    }
    
    [ContextMenu("🛑 TEST: Stop Humming NOW")]
    public void DebugStopHumming()
    {
        Debug.Log($"[DEBUG] Manually stopping humming on {gameObject.name}");
        StopChestHumming();
    }
    
    [ContextMenu("🔍 TEST: Check Audio Status")]
    public void DebugCheckStatus()
    {
        Debug.Log($"========== CHEST AUDIO DEBUG: {gameObject.name} ==========");
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
        }
        
        Debug.Log($"SoundEventsManager.Events: {(SoundEventsManager.Events != null ? "EXISTS" : "NULL")}");
        
        if (SoundEventsManager.Events != null)
        {
            Debug.Log($"chestHumming SoundEvent: {(SoundEventsManager.Events.chestHumming != null ? "EXISTS" : "NULL")}");
            
            if (SoundEventsManager.Events.chestHumming != null)
            {
                Debug.Log($"chestHumming Clip: {(SoundEventsManager.Events.chestHumming.clip != null ? SoundEventsManager.Events.chestHumming.clip.name : "NULL")}");
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
        Debug.Log($"[ChestSoundManager] Debug logs ENABLED for {gameObject.name}");
    }
}
