// --- START OF FILE MainMenuMusicPlayer.cs ---
using UnityEngine;

[RequireComponent(typeof(AudioSource))] // Ensures an AudioSource component is present
public class MainMenuMusicPlayer : MonoBehaviour
{
    [Header("Music Settings")]
    [Tooltip("The background music clip for the main menu.")]
    public AudioClip menuMusicClip;

    [Tooltip("Volume of the music.")]
    [Range(0f, 1f)]
    public float musicVolume = 0.5f;

    [Tooltip("Should the music loop?")]
    public bool loopMusic = true;

    private AudioSource audioSource;

    void Awake()
    {
        // Get the AudioSource component attached to this GameObject
        audioSource = GetComponent<AudioSource>();

        // Configure the AudioSource
        if (audioSource != null)
        {
            audioSource.playOnAwake = false; // We'll control playback manually
            audioSource.loop = loopMusic;
            audioSource.volume = musicVolume;
            audioSource.spatialBlend = 0f; // Ensure music is 2D (not positional)
        }
        else
        {
            Debug.LogError("MainMenuMusicPlayer: AudioSource component not found! Music will not play.", this);
        }
    }

    void Start()
    {
        PlayMusic();
    }

    public void PlayMusic()
    {
        if (audioSource != null && menuMusicClip != null)
        {
            if (!audioSource.isPlaying || audioSource.clip != menuMusicClip)
            {
                audioSource.clip = menuMusicClip;
                audioSource.Play();
                Debug.Log("MainMenuMusicPlayer: Playing menu music - " + menuMusicClip.name);
            }
            else if (!audioSource.isPlaying && audioSource.clip == menuMusicClip)
            {
                // If the correct clip is assigned but it somehow stopped (e.g., scene reloaded but this object persisted)
                audioSource.Play();
                Debug.Log("MainMenuMusicPlayer: Resuming menu music - " + menuMusicClip.name);
            }
        }
        else if (menuMusicClip == null)
        {
            Debug.LogWarning("MainMenuMusicPlayer: No 'Menu Music Clip' assigned. No music will play.", this);
        }
    }

    public void StopMusic()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log("MainMenuMusicPlayer: Stopping menu music.");
        }
    }

    // Optional: If you want to ensure music stops if this object is destroyed or scene changes
    // void OnDestroy()
    // {
    //     StopMusic();
    // }
}
// --- END OF FILE MainMenuMusicPlayer.cs ---