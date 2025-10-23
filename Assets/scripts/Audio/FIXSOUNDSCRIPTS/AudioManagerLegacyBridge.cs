using UnityEngine;
using UnityEngine.Audio;
using GeminiGauntlet.Audio;

/// <summary>
/// Legacy bridge to maintain compatibility with existing code that references AudioManager
/// This provides the same interface as the old AudioManager but routes to the new AAA Sound System
/// DEPRECATED: Use SoundSystemCore and SoundEventsManager directly for new code
/// </summary>
public class AudioManagerLegacyBridge : MonoBehaviour
{
    public static AudioManagerLegacyBridge Instance { get; private set; }

    [Header("=== LEGACY COMPATIBILITY ===")]
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField] private AudioMixerGroup musicMixerGroup;
    
    [Header("=== MIGRATION WARNING ===")]
    [TextArea(3, 5)]
    [SerializeField] private string migrationNote = "This is a legacy bridge for old AudioManager calls. Please migrate to use SoundSystemCore and SoundEventsManager for better performance and features.";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            Debug.LogWarning("AudioManagerLegacyBridge: Using legacy audio bridge. Please migrate to SoundSystemCore for better performance.");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Legacy method: Play 2D sound
    /// DEPRECATED: Use SoundSystemCore.Instance.PlaySound2D() instead
    /// </summary>
    public void PlaySound2D(AudioClip clip, float volume = 1f)
    {
        if (SoundSystemCore.Instance != null)
        {
            SoundSystemCore.Instance.PlaySound2D(clip, SoundCategory.UI, volume);
        }
        else
        {
            Debug.LogError("AudioManagerLegacyBridge: SoundSystemCore not initialized!");
        }
    }

    /// <summary>
    /// Legacy method: Play 3D sound at point
    /// DEPRECATED: Use SoundSystemCore.Instance.PlaySound3D() instead
    /// </summary>
    public void PlaySound3DAtPoint(AudioClip clip, Vector3 position, float volume = 1f, float pitchVariance = 0.05f)
    {
        if (SoundSystemCore.Instance != null)
        {
            float pitch = Random.Range(1f - pitchVariance, 1f + pitchVariance);
            SoundSystemCore.Instance.PlaySound3D(clip, position, SoundCategory.SFX, volume, pitch);
        }
        else
        {
            Debug.LogError("AudioManagerLegacyBridge: SoundSystemCore not initialized!");
        }
    }

    /// <summary>
    /// Legacy method: Play 3D sound attached to transform
    /// DEPRECATED: Use SoundSystemCore.Instance.PlaySoundAttached() instead
    /// </summary>
    public void PlaySound3DAttached(AudioClip clip, Transform parent, float volume = 1f, float pitchVariance = 0.05f, bool loop = false)
    {
        if (SoundSystemCore.Instance != null)
        {
            float pitch = Random.Range(1f - pitchVariance, 1f + pitchVariance);
            SoundSystemCore.Instance.PlaySoundAttached(clip, parent, SoundCategory.SFX, volume, pitch, loop);
        }
        else
        {
            Debug.LogError("AudioManagerLegacyBridge: SoundSystemCore not initialized!");
        }
    }

    /// <summary>
    /// Legacy method: Play looping 3D sound
    /// DEPRECATED: Use SoundSystemCore.Instance.PlaySoundAttached() with loop=true instead
    /// </summary>
    public GameObject PlayLooping3DSound(AudioClip clip, Transform parent, float volume = 1f)
    {
        if (SoundSystemCore.Instance != null)
        {
            var handle = SoundSystemCore.Instance.PlaySoundAttached(clip, parent, SoundCategory.Ambient, volume, 1f, true);
            
            // Create a dummy GameObject for legacy compatibility
            // In reality, the new system handles this internally
            GameObject dummyObject = new GameObject($"LegacyLoopingSound_{clip.name}");
            dummyObject.transform.SetParent(parent);
            
            return dummyObject;
        }
        else
        {
            Debug.LogError("AudioManagerLegacyBridge: SoundSystemCore not initialized!");
            return null;
        }
    }

    /// <summary>
    /// Legacy method: Audio ducking
    /// DEPRECATED: Use AudioMixer snapshots instead
    /// </summary>
    public void DuckAudio(float duckAmount, float duration)
    {
        Debug.LogWarning("AudioManagerLegacyBridge: DuckAudio is deprecated. Use AudioMixer snapshots for better control.");
        // Could implement basic volume ducking here if needed
    }

    /// <summary>
    /// Legacy method: Stop all sounds
    /// </summary>
    public void StopAllSounds()
    {
        if (SoundSystemCore.Instance != null)
        {
            SoundSystemCore.Instance.StopAllSounds();
        }
    }

    /// <summary>
    /// Migration helper: Show current sound system status
    /// </summary>
    [ContextMenu("Show Migration Status")]
    public void ShowMigrationStatus()
    {
        Debug.Log("=== AUDIO SYSTEM MIGRATION STATUS ===");
        
        if (SoundSystemCore.Instance != null)
        {
            Debug.Log("✅ New AAA Sound System is active");
            Debug.Log($"   - Active sounds: {SoundSystemCore.Instance.GetActiveSoundCount()}");
            Debug.Log($"   - Available sources: {SoundSystemCore.Instance.GetAvailableSourceCount()}");
        }
        else
        {
            Debug.LogError("❌ New AAA Sound System is NOT active");
        }

        if (SoundEventsManager.Instance != null)
        {
            Debug.Log("✅ Sound Events Manager is active");
        }
        else
        {
            Debug.LogWarning("⚠️ Sound Events Manager is not active");
        }

        Debug.Log("\n=== MIGRATION RECOMMENDATIONS ===");
        Debug.Log("1. Replace AudioManager.Instance.PlaySound2D() with SoundSystemCore.Instance.PlaySound2D()");
        Debug.Log("2. Replace AudioManager.Instance.PlaySound3DAtPoint() with SoundSystemCore.Instance.PlaySound3D()");
        Debug.Log("3. Use GameSounds.* static methods or SoundEventsManager.Quick.* for common game sounds");
        Debug.Log("4. Create SoundEvent assets for organized sound management");
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}

/// <summary>
/// Static class to help with AudioManager migration
/// </summary>
public static class AudioManagerMigrationHelper
{
    /// <summary>
    /// Quick replacement for AudioManager.Instance.PlaySound2D()
    /// </summary>
    public static void PlaySound2D(AudioClip clip, float volume = 1f)
    {
        SoundSystemCore.Instance?.PlaySound2D(clip, SoundCategory.UI, volume);
    }

    /// <summary>
    /// Quick replacement for AudioManager.Instance.PlaySound3DAtPoint()
    /// </summary>
    public static void PlaySound3DAtPoint(AudioClip clip, Vector3 position, float volume = 1f, float pitchVariance = 0.05f)
    {
        if (SoundSystemCore.Instance != null)
        {
            float pitch = Random.Range(1f - pitchVariance, 1f + pitchVariance);
            SoundSystemCore.Instance.PlaySound3D(clip, position, SoundCategory.SFX, volume, pitch);
        }
    }

    /// <summary>
    /// Quick replacement for AudioManager.Instance.PlaySound3DAttached()
    /// </summary>
    public static void PlaySound3DAttached(AudioClip clip, Transform parent, float volume = 1f, float pitchVariance = 0.05f, bool loop = false)
    {
        if (SoundSystemCore.Instance != null)
        {
            float pitch = Random.Range(1f - pitchVariance, 1f + pitchVariance);
            SoundSystemCore.Instance.PlaySoundAttached(clip, parent, SoundCategory.SFX, volume, pitch, loop);
        }
    }
}
