using UnityEngine;

/// <summary>
/// Dedicated sound manager for all shooting-related audio events
/// Handles weapon firing, reloading, and other shooting-specific sounds
/// </summary>
public class ShootingSoundManager : MonoBehaviour
{
    [Header("Shooting Sound Effects")]
    [SerializeField] private AudioClip[] shootSounds; // Array for weapon variety
    [SerializeField] private AudioClip reloadSound;
    [SerializeField] private AudioClip emptyClickSound;
    [SerializeField] private AudioClip weaponSwitchSound;
    [SerializeField] private AudioClip chargeUpSound;
    
    [Header("Volume Settings")]
    [SerializeField] [Range(0f, 1f)] private float shootVolume = 0.8f;
    [SerializeField] [Range(0f, 1f)] private float reloadVolume = 0.6f;
    [SerializeField] [Range(0f, 1f)] private float emptyClickVolume = 0.4f;
    [SerializeField] [Range(0f, 1f)] private float weaponSwitchVolume = 0.5f;
    [SerializeField] [Range(0f, 1f)] private float chargeVolume = 0.7f;
    
    [Header("Audio Settings")]
    [SerializeField] [Range(0.8f, 1.2f)] private float pitchVariation = 0.1f;
    
    private AudioSource audioSource;
    private AudioSource chargeAudioSource; // Separate source for charge sounds
    
    void Awake()
    {
        // Get or create main audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Create dedicated charge audio source
        GameObject chargeObject = new GameObject("ShootingChargeAudioSource");
        chargeObject.transform.SetParent(transform);
        chargeAudioSource = chargeObject.AddComponent<AudioSource>();
        
        // Configure audio sources
        ConfigureAudioSources();
    }
    
    private void ConfigureAudioSources()
    {
        // Main audio source for one-shot sounds
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 1f; // 3D sound
        
        // Charge audio source for looping charge sounds
        chargeAudioSource.playOnAwake = false;
        chargeAudioSource.loop = true;
        chargeAudioSource.spatialBlend = 1f; // 3D sound
        chargeAudioSource.clip = chargeUpSound;
    }
    
    /// <summary>
    /// Play shooting sound with optional weapon type
    /// </summary>
    public void PlayShoot(int weaponType = 0)
    {
        if (shootSounds != null && shootSounds.Length > 0 && audioSource != null)
        {
            // Select appropriate sound based on weapon type
            int soundIndex = Mathf.Clamp(weaponType, 0, shootSounds.Length - 1);
            AudioClip clipToPlay = shootSounds[soundIndex];
            
            if (clipToPlay != null)
            {
                // Add pitch variation for variety
                audioSource.pitch = Random.Range(1f - pitchVariation, 1f + pitchVariation);
                audioSource.PlayOneShot(clipToPlay, shootVolume);
                audioSource.pitch = 1f; // Reset pitch
            }
        }
    }
    
    /// <summary>
    /// Play reload sound
    /// </summary>
    public void PlayReload()
    {
        if (reloadSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(reloadSound, reloadVolume);
        }
    }
    
    /// <summary>
    /// Play empty click sound (when out of ammo)
    /// </summary>
    public void PlayEmptyClick()
    {
        if (emptyClickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(emptyClickSound, emptyClickVolume);
        }
    }
    
    /// <summary>
    /// Play weapon switch sound
    /// </summary>
    public void PlayWeaponSwitch()
    {
        if (weaponSwitchSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(weaponSwitchSound, weaponSwitchVolume);
        }
    }
    
    /// <summary>
    /// Start playing charge up sound (looped)
    /// </summary>
    public void StartChargeUp()
    {
        if (chargeUpSound != null && chargeAudioSource != null && !chargeAudioSource.isPlaying)
        {
            chargeAudioSource.volume = chargeVolume;
            chargeAudioSource.Play();
        }
    }
    
    /// <summary>
    /// Stop playing charge up sound
    /// </summary>
    public void StopChargeUp()
    {
        if (chargeAudioSource != null && chargeAudioSource.isPlaying)
        {
            chargeAudioSource.Stop();
        }
    }
    
    /// <summary>
    /// Set master shooting volume
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        shootVolume = Mathf.Clamp01(volume);
    }
    
    /// <summary>
    /// Check if charge sound is currently playing
    /// </summary>
    public bool IsCharging()
    {
        return chargeAudioSource != null && chargeAudioSource.isPlaying;
    }
}
