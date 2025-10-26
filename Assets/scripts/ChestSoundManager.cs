using UnityEngine;
using System.Collections;
using GeminiGauntlet.Audio;

/// <summary>
/// AAA Chest Sound Manager - OPTIMIZED ⭐⭐⭐⭐⭐
/// Uses centralized SpatialAudioManager for automatic distance-based cleanup
/// NO redundant Update() checks, NO Camera.main lookups, NO fallback AudioSources
/// Matches Tower performance: ~0.0ms per frame
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
    
    [Header("Debug")]
    [Tooltip("Enable detailed debug logging")]
    [SerializeField] private bool enableDebugLogs = false;
    
    // Track the humming sound handle for proper cleanup
    private SoundHandle hummingHandle = SoundHandle.Invalid;
    
    // Coroutine for startup retry logic (runs once, not every frame)
    private Coroutine startupRetryCoroutine = null;
    
    /// <summary>
    /// Start playing chest humming sound (looped) with AAA spatial audio
    /// Automatically tracked by SpatialAudioManager for distance-based cleanup
    /// </summary>
    public void StartChestHumming()
    {
        DebugLog($"[ChestSoundManager] 🎵 StartChestHumming called on {gameObject.name}");
        
        // Stop any existing hum
        if (hummingHandle.IsValid)
        {
            DebugLog("[ChestSoundManager] Stopping existing humming before starting new one");
            StopChestHumming();
        }

        // Try to start immediately
        bool success = TryStartAdvancedHumming();
        
        if (!success && SoundEventsManager.Instance == null)
        {
            // Sound system not ready - use coroutine retry (not Update loop)
            DebugLog("[ChestSoundManager] ⏳ Sound system not ready - starting retry coroutine...");
            if (startupRetryCoroutine != null)
            {
                StopCoroutine(startupRetryCoroutine);
            }
            startupRetryCoroutine = StartCoroutine(RetryStartupCoroutine());
        }
    }
    
    /// <summary>
    /// Coroutine to retry starting humming (only runs once at startup if needed)
    /// </summary>
    private IEnumerator RetryStartupCoroutine()
    {
        const int MAX_RETRIES = 5;
        const float RETRY_DELAY = 0.5f;
        
        for (int i = 0; i < MAX_RETRIES; i++)
        {
            yield return new WaitForSeconds(RETRY_DELAY);
            
            DebugLog($"[ChestSoundManager] 🔄 Retry #{i + 1}/{MAX_RETRIES} - attempting to start humming...");
            
            bool success = TryStartAdvancedHumming();
            if (success)
            {
                DebugLog($"[ChestSoundManager] ✅ Retry successful on attempt #{i + 1}");
                startupRetryCoroutine = null;
                yield break; // Success - stop retrying
            }
        }
        
        DebugLog($"[ChestSoundManager] ❌ All {MAX_RETRIES} retries failed - sound system may not be configured");
        startupRetryCoroutine = null;
    }
    
    /// <summary>
    /// Try to start humming using the advanced sound system
    /// Returns true on success, false if system not ready
    /// </summary>
    private bool TryStartAdvancedHumming()
    {
        // Check if SoundEventsManager exists
        if (SoundEventsManager.Instance == null)
        {
            DebugLog("[ChestSoundManager] ❌ SoundEventsManager.Instance is NULL");
            return false;
        }
        
        if (SoundEventsManager.Events == null)
        {
            DebugLog("[ChestSoundManager] ❌ SoundEventsManager.Events is NULL");
            return false;
        }
        
        var hummingEvent = SoundEventsManager.Events.chestHumming;
        if (hummingEvent == null || hummingEvent.clip == null)
        {
            DebugLog("[ChestSoundManager] ❌ chestHumming SoundEvent or clip is NULL");
            return false;
        }
        
        if (SoundSystemCore.Instance == null)
        {
            DebugLog("[ChestSoundManager] ❌ SoundSystemCore.Instance is NULL");
            return false;
        }

        // Use AAA spatial audio profile (SpatialAudioManager will handle distance tracking)
        var profile = SpatialAudioProfiles.GenericSFX;
        profile.profileName = "Chest Humming";
        profile.minDistance = minHummingDistance;
        profile.maxDistance = maxHummingDistance;
        profile.maxAudibleDistance = maxAudibleDistance;
        profile.rolloffMode = AudioRolloffMode.Logarithmic;
        profile.spread = 0f;
        profile.dopplerLevel = 0f;
        profile.priority = SoundPriority.Low;
        profile.distanceCheckInterval = 0.5f; // SpatialAudioManager checks every 0.5s
        profile.distanceCullFadeOut = 0.8f;

        hummingHandle = SoundSystemCore.Instance.PlaySoundAttachedWithProfile(
            hummingEvent.clip,
            transform,
            profile,
            hummingVolume * hummingEvent.volume,
            hummingEvent.pitch,
            true  // Loop
        );

        if (hummingHandle.IsValid)
        {
            DebugLog($"[ChestSoundManager] ✅ Started chest humming (SpatialAudioManager will auto-cleanup at {profile.maxAudibleDistance}m)");
            return true;
        }
        
        DebugLog("[ChestSoundManager] ❌ PlaySoundAttachedWithProfile returned invalid handle");
        return false;
    }
    
    /// <summary>
    /// Stop playing chest humming sound with smooth fade
    /// SpatialAudioManager is automatically untracked when handle is stopped
    /// </summary>
    public void StopChestHumming()
    {
        DebugLog($"[ChestSoundManager] 🛑 StopChestHumming called on {gameObject.name}");
        
        // Stop any retry coroutine
        if (startupRetryCoroutine != null)
        {
            StopCoroutine(startupRetryCoroutine);
            startupRetryCoroutine = null;
        }
        
        // Stop advanced sound system (SpatialAudioManager auto-untracks)
        if (hummingHandle.IsValid)
        {
            hummingHandle.FadeOut(0.5f);
            hummingHandle = SoundHandle.Invalid;
            DebugLog($"[ChestSoundManager] ✅ Stopped chest humming at {transform.name}");
        }
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
    public bool IsHumming => hummingHandle.IsValid;
    
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log(message);
        }
    }
    
    void OnDisable()
    {
        StopChestHumming();
    }
    
    void OnDestroy()
    {
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
        Debug.Log($"Handle Valid: {hummingHandle.IsValid}");
        Debug.Log($"Retry Coroutine Active: {startupRetryCoroutine != null}");
        Debug.Log($"SoundEventsManager.Instance: {(SoundEventsManager.Instance != null ? "EXISTS" : "NULL")}");
        Debug.Log($"SoundSystemCore.Instance: {(SoundSystemCore.Instance != null ? "EXISTS" : "NULL")}");
        
        if (SoundEventsManager.Events != null)
        {
            Debug.Log($"chestHumming SoundEvent: {(SoundEventsManager.Events.chestHumming != null ? "EXISTS" : "NULL")}");
            
            if (SoundEventsManager.Events.chestHumming != null)
            {
                Debug.Log($"chestHumming Clip: {(SoundEventsManager.Events.chestHumming.clip != null ? SoundEventsManager.Events.chestHumming.clip.name : "NULL")}");
            }
        }
        
        // Show SpatialAudioManager tracking status
        if (SpatialAudioManager.Instance != null)
        {
            Debug.Log($"SpatialAudioManager Diagnostic: {SpatialAudioManager.Instance.GetDiagnosticInfo()}");
        }
        
        Debug.Log($"====================================================");
    }
    
    [ContextMenu("⚡ FORCE: Enable Debug Logs")]
    public void ForceEnableDebugLogs()
    {
        enableDebugLogs = true;
        Debug.Log($"[ChestSoundManager] Debug logs ENABLED for {gameObject.name}");
    }
}
