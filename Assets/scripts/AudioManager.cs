using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Mixer Groups")]
    public AudioMixerGroup masterMixerGroup;
    public AudioMixerGroup sfxMixerGroup;
    public AudioMixerGroup musicMixerGroup;

    [Header("Sound Ducking Settings")]
    [Range(0f, 1f)]
    public float duckedVolume = 0.3f;
    public float duckDuration = 3f;
    public float duckFadeTime = 0.2f;

    private AudioMixer _audioMixer;
    private float _originalMasterVolume;
    private Coroutine _duckCoroutine;
    private string _volumeParameterName; // Will be determined at runtime
    private bool _hasValidVolumeParameter = false;
    
    // Common AudioMixer parameter names to try
    private readonly string[] COMMON_VOLUME_PARAMETERS = { "Volume", "MasterVolume", "Master", "MainVolume" };

    // Play a sound by name (stub for compatibility)
    public void Play(string soundName)
    {
        Debug.Log($"AudioManager.Play called with soundName: {soundName}");
        // TODO: Implement actual sound playback
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (masterMixerGroup != null && masterMixerGroup.audioMixer != null)
            {
                _audioMixer = masterMixerGroup.audioMixer;
                
                // Try to find a valid volume parameter
                _hasValidVolumeParameter = TryFindVolumeParameter();
                
                if (!_hasValidVolumeParameter)
                {
                    _originalMasterVolume = 0f;
                    Debug.LogWarning($"AudioManager: No valid volume parameter found. Tried: {string.Join(", ", COMMON_VOLUME_PARAMETERS)}. " +
                                   "Audio ducking will be disabled. To fix: expose a volume parameter in your AudioMixer.");
                }
            }
            else
            {
                Debug.LogWarning("AudioManager: No AudioMixerGroup assigned or AudioMixer not found.");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Tries to find a valid volume parameter in the AudioMixer by testing common parameter names
    /// </summary>
    /// <returns>True if a valid parameter was found and configured</returns>
    private bool TryFindVolumeParameter()
    {
        if (_audioMixer == null) return false;

        foreach (string paramName in COMMON_VOLUME_PARAMETERS)
        {
            try
            {
                if (_audioMixer.GetFloat(paramName, out _originalMasterVolume))
                {
                    _volumeParameterName = paramName;
                    Debug.Log($"AudioManager: Found valid volume parameter '{paramName}' with value {_originalMasterVolume:F2} dB");
                    return true;
                }
            }
            catch (System.Exception)
            {
                // Parameter doesn't exist, continue trying
                continue;
            }
        }
        
        return false;
    }

    public void DuckAudioForHandUpgrade()
    {
        if (_duckCoroutine != null)
        {
            StopCoroutine(_duckCoroutine);
        }
        _duckCoroutine = StartCoroutine(DuckAudioRoutine());
    }

    private IEnumerator DuckAudioRoutine()
    {
        if (_audioMixer == null || !_hasValidVolumeParameter) yield break;

        // Convert linear duckedVolume (0.0-1.0) to decibels
        // Use 20*log10(volume) formula with a small minimum to avoid -Infinity
        float duckedVolumeDb = 20f * Mathf.Log10(Mathf.Max(duckedVolume, 0.0001f));
        
        // Fade out to ducked volume
        float startTime = Time.time;
        float endTime = startTime + duckFadeTime;
        
        while (Time.time < endTime)
        {
            float t = (Time.time - startTime) / duckFadeTime;
            // Lerp between decibel values (both are now in dB scale)
            float currentVolumeDb = Mathf.Lerp(_originalMasterVolume, duckedVolumeDb, t);
            _audioMixer.SetFloat(_volumeParameterName, currentVolumeDb);
            yield return new WaitForEndOfFrame();
        }

        // Hold at ducked volume
        yield return new WaitForSeconds(duckDuration - (2 * duckFadeTime));

        // Fade back to original volume
        startTime = Time.time;
        endTime = startTime + duckFadeTime;
        
        while (Time.time < endTime)
        {
            float t = (Time.time - startTime) / duckFadeTime;
            // Lerp between decibel values (both are now in dB scale)
            float currentVolumeDb = Mathf.Lerp(duckedVolumeDb, _originalMasterVolume, t);
            _audioMixer.SetFloat(_volumeParameterName, currentVolumeDb);
            yield return new WaitForEndOfFrame();
        }

        // Ensure we're back at the original volume
        _audioMixer.SetFloat(_volumeParameterName, _originalMasterVolume);
    }

    // This method is for UI sounds, background announcements, etc.
    public void PlaySound2D(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        GameObject soundObj = new GameObject($"TempAudio_2D_{clip.name}");
        AudioSource audioSource = soundObj.AddComponent<AudioSource>();
        audioSource.outputAudioMixerGroup = sfxMixerGroup;
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.spatialBlend = 0f; // 2D sound
        audioSource.Play();
        Destroy(soundObj, clip.length);
    }

    // --- NEW: Method for playing 3D positional sounds robustly ---
    public void PlaySound3DAtPoint(AudioClip clip, Vector3 position, float volume = 1f, float pitchVariance = 0.05f)
    {
        if (clip == null) return;

        GameObject soundObj = new GameObject($"TempAudio_3D_{clip.name}");
        soundObj.transform.position = position;

        AudioSource audioSource = soundObj.AddComponent<AudioSource>();
        audioSource.outputAudioMixerGroup = sfxMixerGroup;
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.pitch = Random.Range(1f - pitchVariance, 1f + pitchVariance);
        
        // 3D Sound Settings
        audioSource.spatialBlend = 1.0f; 
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic; // More realistic falloff
        audioSource.minDistance = 3f;
        audioSource.maxDistance = 50f;

        audioSource.Play();
        
        // Destroy after the clip finishes playing (accounting for pitch changes)
        float clipDuration = clip.length / Mathf.Max(0.01f, audioSource.pitch);
        Destroy(soundObj, clipDuration);
    }

    // --- NEW: Method for playing a one-shot 3D sound attached to a moving object ---
    public void PlaySoundAttached(AudioClip clip, Transform parent, float volume = 1f, float pitchVariance = 0.05f)
    {
        if (clip == null || parent == null) return;

        GameObject soundObj = new GameObject($"TempAudio_Attached_{clip.name}");
        soundObj.transform.SetParent(parent);
        soundObj.transform.localPosition = Vector3.zero;
        soundObj.transform.localRotation = Quaternion.identity;

        AudioSource audioSource = soundObj.AddComponent<AudioSource>();
        audioSource.outputAudioMixerGroup = sfxMixerGroup;
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.pitch = Random.Range(1f - pitchVariance, 1f + pitchVariance);
        
        // 3D Sound Settings
        audioSource.spatialBlend = 1.0f; 
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.minDistance = 3f;
        audioSource.maxDistance = 50f;
        audioSource.loop = false;

        audioSource.Play();
        
        // Destroy after the clip finishes playing
        float destroyDelay = clip.length / Mathf.Max(0.01f, audioSource.pitch);
        Destroy(soundObj, destroyDelay);
    }

    // --- NEW: Method for creating a looping 3D sound source ---
    public AudioSource PlaySoundLooping(AudioClip clip, Transform parent, float volume = 1f)
    {
        if (clip == null || parent == null) return null;

        GameObject soundObj = new GameObject($"TempAudio_Looping_{clip.name}");
        soundObj.transform.SetParent(parent);
        soundObj.transform.localPosition = Vector3.zero;
        soundObj.transform.localRotation = Quaternion.identity;

        AudioSource audioSource = soundObj.AddComponent<AudioSource>();
        audioSource.outputAudioMixerGroup = sfxMixerGroup;
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.spatialBlend = 1.0f; // 3D sound
        audioSource.loop = true;
        audioSource.Play();

        return audioSource;
    }
}