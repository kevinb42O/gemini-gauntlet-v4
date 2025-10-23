using UnityEngine;

namespace GeminiGauntlet.Audio
{
    /// <summary>
    /// Backwards-compatibility shim for legacy code paths that still reference <see cref="AudioManager"/>.
    /// Routes new calls through the refactored AAA sound system while retaining the old API surface.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;
        public static AudioManager Instance => _instance;

        [Header("VOLUME CONTROLS")]
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float sfxVolume = 1f;
        [Range(0f, 1f)] public float musicVolume = 1f;
        [Range(0f, 1f)] public float uiVolume = 1f;

        [Header("LEGACY COMPATIBILITY")]
        [Tooltip("Use new sound system if available, fallback to legacy if not")]
        public bool useNewSoundSystem = true;

        private AudioSource _legacyAudioSource;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAudioManager();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void InitializeAudioManager()
        {
            _legacyAudioSource = GetComponent<AudioSource>();
            if (_legacyAudioSource == null)
            {
                _legacyAudioSource = gameObject.AddComponent<AudioSource>();
            }

            ConfigureLegacyAudioSource();
        }

        private void ConfigureLegacyAudioSource()
        {
            _legacyAudioSource.spatialBlend = 1f;
            _legacyAudioSource.minDistance = 5f;
            _legacyAudioSource.maxDistance = 500f;
            _legacyAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            _legacyAudioSource.dopplerLevel = 0f;
            _legacyAudioSource.priority = 128;
            _legacyAudioSource.playOnAwake = false;
        }

        public SoundHandle PlaySound3DAtPoint(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (useNewSoundSystem && SoundSystemCore.Instance != null)
            {
                return SoundSystemCore.Instance.PlaySound3D(clip, position, SoundCategory.SFX, volume * sfxVolume);
            }

            return PlaySoundLegacy(clip, position, volume * sfxVolume);
        }

        public SoundHandle PlaySoundAttached(AudioClip clip, Transform parent, float volume = 1f)
        {
            if (useNewSoundSystem && SoundSystemCore.Instance != null)
            {
                return SoundSystemCore.Instance.PlaySoundAttached(clip, parent, SoundCategory.SFX, volume * sfxVolume);
            }

            return PlaySoundLegacy(clip, parent != null ? parent.position : Vector3.zero, volume * sfxVolume);
        }

        public SoundHandle PlaySoundLooping(AudioClip clip, Transform parent, float volume = 1f)
        {
            if (useNewSoundSystem && SoundSystemCore.Instance != null)
            {
                return SoundSystemCore.Instance.PlaySoundAttached(clip, parent, SoundCategory.SFX, volume * sfxVolume, 1f, true);
            }

            return PlaySoundLegacy(clip, parent != null ? parent.position : Vector3.zero, volume * sfxVolume, true);
        }

        public SoundHandle PlaySound2D(AudioClip clip, float volume = 1f)
        {
            if (useNewSoundSystem && SoundSystemCore.Instance != null)
            {
                return SoundSystemCore.Instance.PlaySound2D(clip, SoundCategory.UI, volume * uiVolume);
            }

            if (clip != null)
            {
                _legacyAudioSource.spatialBlend = 0f;
                _legacyAudioSource.PlayOneShot(clip, volume * uiVolume * masterVolume);
            }

            return SoundHandle.Invalid;
        }

        private SoundHandle PlaySoundLegacy(AudioClip clip, Vector3 position, float volume, bool loop = false)
        {
            if (clip == null)
            {
                return SoundHandle.Invalid;
            }

            GameObject tempAudioObj = new GameObject($"TempAudio_{clip.name}");
            tempAudioObj.transform.position = position;

            AudioSource tempAudioSource = tempAudioObj.AddComponent<AudioSource>();
            ConfigureLegacyAudioSource3D(tempAudioSource);

            tempAudioSource.clip = clip;
            tempAudioSource.volume = volume * masterVolume;
            tempAudioSource.loop = loop;
            tempAudioSource.Play();

            if (!loop)
            {
                Destroy(tempAudioObj, clip.length + 0.1f);
            }

            return SoundHandle.Invalid;
        }

        private void ConfigureLegacyAudioSource3D(AudioSource source)
        {
            source.spatialBlend = 1f;
            source.minDistance = 5f;
            source.maxDistance = 500f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.dopplerLevel = 0f;
            source.priority = 128;
            source.playOnAwake = false;
        }

        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
        }

        public void SetUIVolume(float volume)
        {
            uiVolume = Mathf.Clamp01(volume);
        }

        public void StopAllSounds()
        {
            SoundSystemCore.Instance?.StopAllSounds();
            _legacyAudioSource?.Stop();
        }

        [ContextMenu("Test 3D Audio")]
        public void Test3DAudio()
        {
            Debug.Log("[AudioManager] Test 3D Audio - assign a test clip and call PlaySound3DAtPoint");
        }

        [ContextMenu("Audio Manager Diagnostics")]
        public void AudioDiagnostics()
        {
            Debug.Log("=== Audio Manager Diagnostics ===");
            Debug.Log($"Use New Sound System: {useNewSoundSystem}");
            Debug.Log($"SoundSystemCore Available: {SoundSystemCore.Instance != null}");
            Debug.Log($"Master Volume: {masterVolume:F2}");
            Debug.Log($"SFX Volume: {sfxVolume:F2}");
            Debug.Log($"UI Volume: {uiVolume:F2}");
            Debug.Log($"Legacy AudioSource Present: {_legacyAudioSource != null}");
        }
    }
}