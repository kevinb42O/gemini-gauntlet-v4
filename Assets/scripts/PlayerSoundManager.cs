using UnityEngine;
using GeminiGauntlet.Audio;

[RequireComponent(typeof(AudioSource))]
public class PlayerSoundManager : MonoBehaviour
{
    [Header("Sound Settings")]
    [Tooltip("Volume for death and respawn sounds")]
    [Range(0f, 1f)]
    public float volume = 0.8f;
    [Tooltip("Make sounds 2D (not affected by distance/position)")]
    public bool playAs2DSound = true;

    private AudioSource _audioSource;
    private static PlayerSoundManager _instance;

    public static PlayerSoundManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PlayerSoundManager>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("PlayerSoundManager");
                    _instance = obj.AddComponent<PlayerSoundManager>();
                    obj.AddComponent<AudioSource>();
                    DontDestroyOnLoad(obj);
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        // Ensure only one instance exists
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource != null)
        {
            _audioSource.playOnAwake = false;
            _audioSource.loop = false;
            _audioSource.spatialBlend = playAs2DSound ? 0f : 1f;
        }
    }

    public void PlayDeathSound()
    {
        if (_audioSource == null) return;
        
        // Use new AAA sound system through GameSounds
        GameSounds.PlayPlayerDeath(transform.position, volume);
    }

    public void PlayRespawnSound()
    {
        if (_audioSource == null) return;
        
        // Use new AAA sound system through GameSounds
        GameSounds.PlayPlayerSpawn(transform.position, volume);
    }
}
