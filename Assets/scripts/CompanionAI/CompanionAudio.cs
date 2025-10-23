using UnityEngine;
using CompanionAI;
/// <summary>
/// Handles all companion audio - weapon sounds, movement sounds, voice lines
/// </summary>
[DefaultExecutionOrder(-50)]
public class CompanionAudio : MonoBehaviour
{
    [Header("🔊 WEAPON AUDIO (Sound Events First, then fallback clips)")]
    [Tooltip("Primary shotgun sound reference fetched from SoundEvents asset if available")]
    public GeminiGauntlet.Audio.SoundEvent companionShotgunEvent;

    [Tooltip("Primary stream loop sound reference fetched from SoundEvents asset if available")]
    public GeminiGauntlet.Audio.SoundEvent companionStreamEvent;
    
    [Header("🎲 RANDOM SOUND VARIATION (Enemy Companions)")]
    [Tooltip("Array of stream sounds - enemy companions will pick ONE random sound at spawn (zero performance cost)")]
    public GeminiGauntlet.Audio.SoundEvent[] companionStreamVariations;
    
    // Runtime-assigned random stream sound (set once at initialization)
    private GeminiGauntlet.Audio.SoundEvent _assignedStreamSound;

    [Tooltip("Fallback shotgun clip when sound events are not configured")]
    public AudioClip shotgunSFX;

    [Tooltip("Fallback stream loop clip when sound events are not configured")]
    public AudioClip streamLoopSFX;

    [Header("🎚️ VOLUME SETTINGS")]
    [Range(0f, 3f)] public float shotgunVolume = 0.9f;
    [Range(0f, 3f)] public float streamVolume = 0.7f;

    [Header("🎯 3D AUDIO SETTINGS")]
    [Range(1f, 50f)] public float minDistance = 5f;
    [Range(50f, 1000f)] public float maxDistance = 500f;

    private CompanionCore _core;
    private AudioSource _audioSource;
    private AudioSource _streamAudioSource;

    public void Initialize(CompanionCore core)
    {
        _core = core;
        SetupAudioSources();
        
        // 🎲 RANDOM SOUND ASSIGNMENT: Pick ONE random stream sound at spawn (zero runtime cost!)
        AssignRandomStreamSound();
    }
    
    /// <summary>
    /// Assigns a random stream sound from the variations array at spawn time.
    /// This happens ONCE per companion, so there's ZERO performance cost during gameplay.
    /// Each enemy companion will have a unique sound, making combat less repetitive.
    /// </summary>
    private void AssignRandomStreamSound()
    {
        // If variations array is configured, pick a random one
        if (companionStreamVariations != null && companionStreamVariations.Length > 0)
        {
            _assignedStreamSound = companionStreamVariations[Random.Range(0, companionStreamVariations.Length)];
            Debug.Log($"[CompanionAudio] 🎲 Assigned random stream sound variation (index {System.Array.IndexOf(companionStreamVariations, _assignedStreamSound)}/{companionStreamVariations.Length})");
        }
        else
        {
            // No variations configured - use default
            _assignedStreamSound = null;
            Debug.Log("[CompanionAudio] ℹ️ No stream variations configured - using default sound");
        }
    }

    private void SetupAudioSources()
    {
        // Setup main audio source
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            Debug.Log("[CompanionAudio] Created new main AudioSource");
        }

        ConfigureAudioSource(_audioSource);

        // Setup stream audio source - check if it already exists first
        Transform existingStreamAudio = transform.Find("StreamAudio");
        if (existingStreamAudio != null)
        {
            _streamAudioSource = existingStreamAudio.GetComponent<AudioSource>();
            if (_streamAudioSource != null)
            {
                Debug.Log("[CompanionAudio] Found existing StreamAudio - reusing it");
                ConfigureAudioSource(_streamAudioSource);
                return;
            }
        }

        // Create new stream audio object if it doesn't exist
        var streamAudioObj = new GameObject("StreamAudio");
        streamAudioObj.transform.SetParent(transform);
        streamAudioObj.transform.localPosition = Vector3.zero;

        _streamAudioSource = streamAudioObj.AddComponent<AudioSource>();
        ConfigureAudioSource(_streamAudioSource);

        Debug.Log("[CompanionAudio] Audio system initialized successfully (created new StreamAudio)");
    }

    private void ConfigureAudioSource(AudioSource source)
    {
        source.spatialBlend = 1f;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.dopplerLevel = 0f;
        source.priority = 128;
        source.pitch = 1f;
        source.panStereo = 0f;

        Debug.Log($"[CompanionAudio] 🔊 AudioSource configured - 3D: {source.spatialBlend}, Range: {source.minDistance}-{source.maxDistance}");
    }

    public void PlayShotgunSound()
    {
        // CRITICAL FIX: Re-initialize audio sources if they were destroyed (happens when enemy deactivates/reactivates)
        if (_audioSource == null)
        {
            Debug.LogWarning("[CompanionAudio] ⚠️ Main audio source is null - attempting to reinitialize...");
            SetupAudioSources();
            
            if (_audioSource == null)
            {
                Debug.LogError("[CompanionAudio] ❌ Failed to reinitialize main audio source!");
                return;
            }
        }

        // Prefer SoundEvents integration when configured
        if (TryPlayShotgunViaSoundEvents())
        {
            Debug.Log("[CompanionAudio] ✅ Shotgun sound played via SoundEvents!");
            return;
        }

        if (shotgunSFX != null)
        {
            _audioSource.PlayOneShot(shotgunSFX, shotgunVolume);
            Debug.Log("[CompanionAudio] 💥 SHOTGUN BLAST played via fallback AudioClip!");
            return;
        }

        Debug.LogWarning("[CompanionAudio] ⚠️ Cannot play shotgun sound - no sound event or clip configured!");
    }

    public void PlayStreamSound()
    {
        // CRITICAL FIX: Re-initialize audio sources if they were destroyed (happens when enemy deactivates/reactivates)
        if (_streamAudioSource == null)
        {
            Debug.LogWarning("[CompanionAudio] ⚠️ Stream audio source is null - attempting to reinitialize...");
            SetupAudioSources();
            
            if (_streamAudioSource == null)
            {
                Debug.LogError("[CompanionAudio] ❌ Failed to reinitialize stream audio source!");
                return;
            }
        }

        if (_streamAudioSource.isPlaying)
        {
            Debug.Log("[CompanionAudio] 🌊 Stream sound already playing");
            return;
        }

        // Try SoundEvents first
        if (TryPlayStreamViaSoundEvents())
        {
            Debug.Log("[CompanionAudio] ✅ Stream sound started via SoundEvents!");
            return;
        }

        if (streamLoopSFX != null)
        {
            _streamAudioSource.clip = streamLoopSFX;
            _streamAudioSource.volume = streamVolume;
            _streamAudioSource.loop = true;
            _streamAudioSource.Play();
            Debug.Log("[CompanionAudio] 🌊 STREAM SOUND started via fallback AudioClip!");
            return;
        }

        Debug.LogWarning("[CompanionAudio] ⚠️ Cannot play stream sound - no sound event or clip configured!");
    }

    public void StopStreamSound()
    {
        if (_streamAudioSource != null && _streamAudioSource.isPlaying)
        {
            _streamAudioSource.Stop();
            _streamAudioSource.clip = null;
            Debug.Log("[CompanionAudio] 🔇 Stream sound stopped");
        }
        _activeStreamHandle?.Stop();
        _activeStreamHandle = GeminiGauntlet.Audio.SoundHandle.Invalid;
    }

    public void StopAllSounds()
    {
        if (_audioSource != null)
        {
            _audioSource.Stop();
        }

        StopStreamSound();
    }

    public void SetMasterVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);

        if (_audioSource != null)
        {
            _audioSource.volume = volume;
        }

        if (_streamAudioSource != null)
        {
            _streamAudioSource.volume = volume * streamVolume;
        }
        if (_activeStreamHandle.IsValid)
        {
            _activeStreamHandle.SetVolume(volume * streamVolume);
        }
    }

    [ContextMenu("🧪 Test Shotgun Sound")]
    public void TestShotgunSound()
    {
        Debug.Log("[CompanionAudio] 🧪 TESTING SHOTGUN SOUND...");
        PlayShotgunSound();
    }

    [ContextMenu("🧪 Test Stream Sound")]
    public void TestStreamSound()
    {
        Debug.Log("[CompanionAudio] 🧪 TESTING STREAM SOUND...");
        if (_streamAudioSource != null && _streamAudioSource.isPlaying)
        {
            StopStreamSound();
        }
        else
        {
            PlayStreamSound();
        }
    }

    [ContextMenu("🔧 Audio System Diagnostics")]
    public void AudioSystemDiagnostics()
    {
        Debug.Log("=== 🔊 AUDIO SYSTEM DIAGNOSTICS ===");
        Debug.Log($"Shotgun Clip: {shotgunSFX != null} ({shotgunSFX})");
        Debug.Log($"Stream Clip: {streamLoopSFX != null} ({streamLoopSFX})");
        Debug.Log($"Shotgun Event: {companionShotgunEvent != null}");
        Debug.Log($"Stream Event: {companionStreamEvent != null}");
        Debug.Log($"Main AudioSource: {_audioSource != null}");
        Debug.Log($"Stream AudioSource: {_streamAudioSource != null}");

        if (_audioSource != null)
        {
            Debug.Log($"Main Audio - 3D: {_audioSource.spatialBlend}, Volume: {_audioSource.volume}");
            Debug.Log($"Main Audio - Distance: {_audioSource.minDistance}-{_audioSource.maxDistance}");
        }

        if (_streamAudioSource != null)
        {
            Debug.Log($"Stream Audio - 3D: {_streamAudioSource.spatialBlend}, Volume: {_streamAudioSource.volume}");
            Debug.Log($"Stream Audio - Playing: {_streamAudioSource.isPlaying}");
        }
    }

    public void Cleanup()
    {
        StopAllSounds();

        if (_streamAudioSource != null)
        {
            Destroy(_streamAudioSource.gameObject);
        }
        _activeStreamHandle = GeminiGauntlet.Audio.SoundHandle.Invalid;
    }

    private GeminiGauntlet.Audio.SoundHandle _activeStreamHandle = GeminiGauntlet.Audio.SoundHandle.Invalid;

    private bool TryPlayShotgunViaSoundEvents()
    {
        var events = GeminiGauntlet.Audio.SoundEventsManager.Events;
        GeminiGauntlet.Audio.SoundEvent soundEvent = companionShotgunEvent ?? events?.companionShotgun;

        if (soundEvent == null)
        {
            if (events == null)
            {
                Debug.LogWarning("[CompanionAudio] ⚠️ SoundEventsManager has no Events asset registered – cannot play shotgun via sound events");
            }
            return false;
        }

        var handle = GeminiGauntlet.Audio.SoundEventsManager.Quick.PlayCombatSound(soundEvent, transform.position, shotgunVolume);
        if (!handle.IsValid)
        {
            Debug.LogWarning("[CompanionAudio] ⚠️ Shotgun sound event failed to play (invalid handle)");
            return false;
        }

        Debug.Log("[CompanionAudio] 💥 Shotgun sound played via SoundEvents!");
        return true;
    }

    private bool TryPlayStreamViaSoundEvents()
    {
        var events = GeminiGauntlet.Audio.SoundEventsManager.Events;
        
        // 🎲 PRIORITY 1: Use the randomly assigned stream sound (if available)
        // This was picked at spawn time, so zero performance cost!
        GeminiGauntlet.Audio.SoundEvent soundEvent = _assignedStreamSound ?? companionStreamEvent ?? events?.companionStreamLoop;

        if (soundEvent == null)
        {
            if (events == null)
            {
                Debug.LogWarning("[CompanionAudio] ⚠️ SoundEventsManager has no Events asset registered – cannot play stream via sound events");
            }
            return false;
        }

        _activeStreamHandle = soundEvent.PlayAttached(transform, streamVolume);
        if (!_activeStreamHandle.IsValid)
        {
            Debug.LogWarning("[CompanionAudio] ⚠️ Stream sound event failed to play (invalid handle)");
            return false;
        }

        Debug.Log($"[CompanionAudio] 🌊 Stream loop started via SoundEvents! (Using {(_assignedStreamSound != null ? "random variation" : "default sound")})");
        return true;
    }
}