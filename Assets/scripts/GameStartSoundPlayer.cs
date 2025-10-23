// --- GameStartSoundPlayer.cs ---
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GameStartSoundPlayer : MonoBehaviour
{
    [Header("Sound Settings")]
    [Tooltip("The audio clip to play when the game starts.")]
    public AudioClip gameStartClip;

    [Tooltip("Volume of the game start sound.")]
    [Range(0f, 1f)]
    public float volume = 0.8f;

    [Tooltip("Make sound 2D (not affected by distance/position). Generally true for UI/Announcements.")]
    public bool playAs2DSound = true;

    private AudioSource _audioSource;

    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        // Configure AudioSource settings here as they are persistent
        if (_audioSource != null)
        {
            _audioSource.playOnAwake = false; // We control playback
            _audioSource.loop = false;        // Game start sound is typically a one-shot
            if (playAs2DSound)
            {
                _audioSource.spatialBlend = 0f; // 2D sound
            }
            else
            {
                _audioSource.spatialBlend = 1f; // 3D sound (uses AudioListener position)
            }
        }
        else
        {
            // Should not happen due to RequireComponent, but as a fallback:
            Debug.LogError("GameStartSoundPlayer: AudioSource component is missing despite RequireComponent. This is unexpected.", this);
        }
    }

    void Start()
    {
        // Automatic sound play disabled as requested
        // PlaySound();
    }

    void PlaySound()
    {
        if (gameStartClip == null)
        {
            Debug.LogWarning("GameStartSoundPlayer: No 'Game Start Clip' assigned. No sound will play.", this);
            return;
        }

        if (_audioSource != null)
        {
            // Check if it's already playing this specific clip (e.g., if Start() gets called multiple times somehow)
            if (_audioSource.isPlaying && _audioSource.clip == gameStartClip)
            {
                // Already playing, do nothing or restart if desired
                // Debug.Log("GameStartSoundPlayer: Game start sound is already playing.");
            }
            else
            {
                _audioSource.clip = gameStartClip; // Assign the clip
                _audioSource.volume = volume;      // Set volume (can be dynamic if needed)
                _audioSource.Play();
                // Debug.Log("GameStartSoundPlayer: Played game start sound '" + gameStartClip.name + "'");
            }
        }
        else
        {
            // Fallback if _audioSource was somehow nullified after Awake
            Debug.LogError("GameStartSoundPlayer: AudioSource is null in PlaySound. Cannot play sound.", this);
        }
    }

    // Optional: Method to stop the sound if needed from elsewhere
    public void StopSound()
    {
        if (_audioSource != null && _audioSource.isPlaying)
        {
            _audioSource.Stop();
        }
    }
}