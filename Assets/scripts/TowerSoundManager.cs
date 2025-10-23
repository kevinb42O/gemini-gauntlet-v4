using UnityEngine;
using GeminiGauntlet.Audio;

/// <summary>
/// AAA Tower Sound Manager - Uses centralized sound system with proper spatial audio profiles
/// NO MORE ROGUE AUDIOSOURCE CREATION!
/// </summary>
public class TowerSoundManager : MonoBehaviour
{
    [Header("Volume Settings - ONLY 3 SOUNDS NEEDED")]
    [SerializeField] [Range(0f, 1f)] private float shootVolume = 0.8f;   // Tower spawns skulls
    [SerializeField] [Range(0f, 1f)] private float wakeupVolume = 0.7f;  // Tower emerges
    [SerializeField] [Range(0f, 1f)] private float deathVolume = 0.9f;   // Tower dies
    [SerializeField] [Range(0f, 1f)] private float idleVolume = 0.5f;    // Legacy - for idle loop (not used for main 3 sounds)
    
    [Header("Debugging")]
    [SerializeField] private bool showDebugLogs = false;
    
    // Track looping idle sound for proper cleanup
    private SoundHandle idleSoundHandle = SoundHandle.Invalid;
    
    // CRITICAL: Prevent sound spam by tracking last play time for each of the 3 sounds
    // GLOBAL tracking prevents multiple towers from spamming the same sound simultaneously
    private static float _lastTowerAppearTimeGlobal = -999f;
    private float _lastTowerShootTime = -999f;
    private float _lastTowerDeathTime = -999f;
    private const float MIN_SOUND_INTERVAL = 0.5f; // Minimum time between same sound plays
    
    /// <summary>
    /// Play tower shooting sound with AAA spatial audio profile
    /// SIMPLE: Plays when tower spawns skull burst (ONE sound per burst, not per skull)
    /// </summary>
    public void PlayTowerShoot()
    {
        // CRITICAL: Prevent sound spam
        float timeSinceLastPlay = Time.time - _lastTowerShootTime;
        if (timeSinceLastPlay < MIN_SOUND_INTERVAL)
        {
            if (showDebugLogs) Debug.LogWarning($"[TowerSoundManager] PlayTowerShoot called too soon! Only {timeSinceLastPlay:F2}s since last play. Ignoring.");
            return;
        }
        _lastTowerShootTime = Time.time;
        
        if (SoundEventsManager.Events?.towerShoot == null || SoundEventsManager.Events.towerShoot.Length == 0)
        {
            if (showDebugLogs) Debug.LogWarning("[TowerSoundManager] Tower shoot sound not configured");
            return;
        }

        // Get random shoot sound
        var shootEvent = SoundEvents.GetRandomSound(SoundEventsManager.Events.towerShoot);
        if (shootEvent?.clip == null) return;

        // Use AAA spatial audio profile
        var profile = SpatialAudioProfiles.TowerShoot;
        SoundSystemCore.Instance?.PlaySound3DWithProfile(
            shootEvent.clip,
            transform.position,
            profile,
            shootVolume * shootEvent.volume,  // Multiply by SoundEvent volume
            shootEvent.pitch,
            false
        );

        Debug.Log($"[TowerSoundManager] 🗼💥 PLAYED tower shoot at {transform.position} (Time: {Time.time:F2})");
    }
    
    /// <summary>
    /// Play tower wakeup sound with AAA spatial audio profile
    /// </summary>
    public void PlayTowerWakeup()
    {
        if (SoundEventsManager.Events?.towerAppear == null || SoundEventsManager.Events.towerAppear.Length == 0)
        {
            if (showDebugLogs) Debug.LogWarning("[TowerSoundManager] Tower wakeup sound not configured");
            return;
        }

        // Get random wakeup sound
        var wakeupEvent = SoundEvents.GetRandomSound(SoundEventsManager.Events.towerAppear);
        if (wakeupEvent?.clip == null) return;

        // Use AAA spatial audio profile
        var profile = SpatialAudioProfiles.TowerAwaken;
        SoundSystemCore.Instance?.PlaySound3DWithProfile(
            wakeupEvent.clip,
            transform.position,
            profile,
            wakeupVolume * wakeupEvent.volume,  // Multiply by SoundEvent volume
            wakeupEvent.pitch,
            false
        );

        if (showDebugLogs) Debug.Log($"[TowerSoundManager] 🗼 Played tower wakeup at {transform.position}");
    }
    
    /// <summary>
    /// Start playing tower idle sound (looped) with AAA spatial audio profile
    /// Automatically tracked for distance-based cleanup
    /// </summary>
    public void StartTowerIdle()
    {
        // Stop any existing idle sound
        if (idleSoundHandle.IsValid)
        {
            StopTowerIdle();
        }

        if (SoundEventsManager.Events?.towerAppear == null || SoundEventsManager.Events.towerAppear.Length == 0)
        {
            if (showDebugLogs) Debug.LogWarning("[TowerSoundManager] Tower idle sound not configured");
            return;
        }

        // Get first idle sound (or random)
        var idleEvent = SoundEventsManager.Events.towerAppear[0];
        if (idleEvent?.clip == null) return;

        // Use AAA spatial audio profile with looping and distance tracking
        var profile = SpatialAudioProfiles.TowerIdle;
        idleSoundHandle = SoundSystemCore.Instance?.PlaySoundAttachedWithProfile(
            idleEvent.clip,
            transform,
            profile,
            idleVolume * idleEvent.volume,  // Multiply by SoundEvent volume
            idleEvent.pitch,
            true  // Loop
        ) ?? SoundHandle.Invalid;

        if (showDebugLogs && idleSoundHandle.IsValid)
        {
            Debug.Log($"[TowerSoundManager] 🗼 Started tower idle loop (will auto-cleanup at {profile.maxAudibleDistance}m)");
        }
    }
    
    /// <summary>
    /// Stop playing tower idle sound with smooth fade
    /// </summary>
    public void StopTowerIdle()
    {
        if (idleSoundHandle.IsValid)
        {
            idleSoundHandle.FadeOut(0.5f);
            SpatialAudioManager.Instance?.UntrackSound(idleSoundHandle);
            idleSoundHandle = SoundHandle.Invalid;
            
            if (showDebugLogs) Debug.Log("[TowerSoundManager] 🗼 Stopped tower idle loop");
        }
    }
    
    /// <summary>
    /// Play tower death sound with AAA spatial audio profile
    /// SIMPLE: Plays when tower is destroyed (ONE sound per tower death)
    /// </summary>
    public void PlayTowerDeath()
    {
        // CRITICAL: Prevent sound spam
        float timeSinceLastPlay = Time.time - _lastTowerDeathTime;
        if (timeSinceLastPlay < MIN_SOUND_INTERVAL)
        {
            if (showDebugLogs) Debug.LogWarning($"[TowerSoundManager] PlayTowerDeath called too soon! Only {timeSinceLastPlay:F2}s since last play. Ignoring.");
            return;
        }
        _lastTowerDeathTime = Time.time;
        
        // Stop idle sound immediately
        StopTowerIdle();

        if (SoundEventsManager.Events?.towerCollapse == null || SoundEventsManager.Events.towerCollapse.Length == 0)
        {
            if (showDebugLogs) Debug.LogWarning("[TowerSoundManager] Tower death sound not configured");
            return;
        }

        // Get random death sound
        var deathEvent = SoundEvents.GetRandomSound(SoundEventsManager.Events.towerCollapse);
        if (deathEvent?.clip == null) return;

        // Use AAA spatial audio profile (use TowerAwaken profile for impact)
        var profile = SpatialAudioProfiles.TowerAwaken;
        SoundSystemCore.Instance?.PlaySound3DWithProfile(
            deathEvent.clip,
            transform.position,
            profile,
            deathVolume * deathEvent.volume,  // Multiply by SoundEvent volume
            deathEvent.pitch,
            false
        );

        Debug.Log($"[TowerSoundManager] 🗼� PLAYED tower death at {transform.position} (Time: {Time.time:F2})");
    }
    
    /// <summary>
    /// DEPRECATED: Use PlayTowerShoot() instead
    /// </summary>
    public void PlayTowerCharge()
    {
        // Redirect to PlayTowerShoot for consistency
        PlayTowerShoot();
    }
    
    /// <summary>
    /// Play tower appear sound with AAA spatial audio profile
    /// </summary>
    public void PlayTowerAppear(AudioClip appearSound = null, float volume = 1.0f)
    {
        // CRITICAL: Prevent sound spam - don't play if ANY tower played this sound recently (GLOBAL check)
        float timeSinceLastPlay = Time.time - _lastTowerAppearTimeGlobal;
        if (timeSinceLastPlay < MIN_SOUND_INTERVAL)
        {
            Debug.LogWarning($"[TowerSoundManager] ⚠️ PlayTowerAppear called TOO SOON! Only {timeSinceLastPlay:F2}s since last play. BLOCKING SOUND SPAM (tower: {gameObject.name}).");
            return;
        }
        _lastTowerAppearTimeGlobal = Time.time;
        
        // Use the provided clip or fallback to towerAppear sounds
        AudioClip clipToPlay = appearSound;
        float pitch = 1f;
        float soundEventVolume = 1f;

        if (clipToPlay == null && SoundEventsManager.Events?.towerAppear != null && SoundEventsManager.Events.towerAppear.Length > 0)
        {
            var appearEvent = SoundEvents.GetRandomSound(SoundEventsManager.Events.towerAppear);
            clipToPlay = appearEvent?.clip;
            pitch = appearEvent?.pitch ?? 1f;
            soundEventVolume = appearEvent?.volume ?? 1f;
        }

        if (clipToPlay == null)
        {
            if (showDebugLogs) Debug.LogWarning("[TowerSoundManager] Tower appear sound not configured");
            return;
        }

        // Use AAA spatial audio profile
        var profile = SpatialAudioProfiles.TowerAwaken;
        SoundSystemCore.Instance?.PlaySound3DWithProfile(
            clipToPlay,
            transform.position,
            profile,
            volume * wakeupVolume * soundEventVolume,  // Multiply by SoundEvent volume
            pitch,
            false
        );

        Debug.Log($"[TowerSoundManager] 🗼✅ PLAYED tower appear at {transform.position} (Time: {Time.time:F2})");
    }
    
    /// <summary>
    /// Set idle volume dynamically (adjusts the looping sound)
    /// NOTE: Not used for main 3 tower sounds (Appear, Shoot, Die)
    /// </summary>
    public void SetIdleVolume(float volume)
    {
        if (idleSoundHandle.IsValid)
        {
            idleSoundHandle.SetVolume(Mathf.Clamp01(volume));
        }
    }
    
    /// <summary>
    /// Check if idle sound is currently playing
    /// </summary>
    public bool IsIdlePlaying()
    {
        return idleSoundHandle.IsValid;
    }

    void OnDestroy()
    {
        // Cleanup idle sound when tower is destroyed
        StopTowerIdle();
    }
}
