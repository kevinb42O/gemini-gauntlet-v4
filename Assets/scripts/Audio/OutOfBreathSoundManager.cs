using UnityEngine;
using GeminiGauntlet.Audio;

/// <summary>
/// Manages the out-of-breath sound effect with smooth volume fading based on energy levels.
/// Fades in starting at 20% energy and continues until energy is regained to 20%.
/// </summary>
public class OutOfBreathSoundManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the SoundEvents asset")]
    public SoundEvents soundEvents;
    
    [Tooltip("Reference to PlayerEnergySystem (auto-found if null)")]
    public PlayerEnergySystem playerEnergySystem;
    
    [Header("Fade Settings")]
    [Tooltip("Energy percentage where sound starts fading in (0-1)")]
    [Range(0f, 0.5f)]
    public float fadeStartThreshold = 0.2f; // 20%
    
    [Tooltip("Energy percentage where sound is at full volume (0-1)")]
    [Range(0f, 0.2f)]
    public float fullVolumeThreshold = 0.05f; // 5%
    
    [Tooltip("Maximum volume for the out-of-breath sound")]
    [Range(0f, 1f)]
    public float maxVolume = 0.8f;
    
    [Tooltip("Minimum volume (when fading in/out)")]
    [Range(0f, 0.1f)]
    public float minVolume = 0.0f;
    
    [Tooltip("How quickly volume changes (higher = faster)")]
    [Range(0.1f, 10f)]
    public float fadeSpeed = 2f;
    
    [Header("Debug")]
    [Tooltip("Show debug logs")]
    public bool enableDebugLogs = false;
    
    // State tracking
    private SoundHandle currentSoundHandle;
    private bool isPlaying = false;
    private float currentVolume = 0f;
    private float targetVolume = 0f;
    
    void Start()
    {
        // Auto-find PlayerEnergySystem if not assigned
        if (playerEnergySystem == null)
        {
            playerEnergySystem = FindObjectOfType<PlayerEnergySystem>();
            if (playerEnergySystem == null)
            {
                Debug.LogError("[OutOfBreathSoundManager] PlayerEnergySystem not found!");
                enabled = false;
                return;
            }
        }
        
        // Validate sound events
        if (soundEvents == null)
        {
            Debug.LogError("[OutOfBreathSoundManager] SoundEvents asset not assigned!");
            enabled = false;
            return;
        }
        
        if (soundEvents.outOfBreathLoop == null || soundEvents.outOfBreathLoop.clip == null)
        {
            Debug.LogWarning("[OutOfBreathSoundManager] Out of breath sound not assigned in SoundEvents!");
            enabled = false;
            return;
        }
        
        // Ensure the sound event is set to loop
        if (!soundEvents.outOfBreathLoop.loop)
        {
            Debug.LogWarning("[OutOfBreathSoundManager] Out of breath sound should be set to loop! Auto-enabling loop.");
            soundEvents.outOfBreathLoop.loop = true;
        }
        
        if (enableDebugLogs)
            Debug.Log("[OutOfBreathSoundManager] Initialized successfully");
    }
    
    void Update()
    {
        if (playerEnergySystem == null || soundEvents == null) return;
        
        // Calculate energy percentage
        float energyPercent = playerEnergySystem.CurrentEnergy / playerEnergySystem.MaxEnergy;
        
        // Determine target volume based on energy level
        CalculateTargetVolume(energyPercent);
        
        // Handle sound playback state
        UpdateSoundPlayback();
        
        // Smoothly interpolate current volume towards target
        if (currentVolume != targetVolume)
        {
            currentVolume = Mathf.Lerp(currentVolume, targetVolume, fadeSpeed * Time.deltaTime);
            
            // Snap to target if very close
            if (Mathf.Abs(currentVolume - targetVolume) < 0.01f)
            {
                currentVolume = targetVolume;
            }
            
            // Apply volume to the sound handle
            if (isPlaying && currentSoundHandle.IsValid)
            {
                currentSoundHandle.SetVolume(currentVolume);
                
                if (enableDebugLogs && Time.frameCount % 30 == 0) // Log every 30 frames to reduce spam
                {
                    Debug.Log($"[OutOfBreathSoundManager] Energy: {energyPercent:P0}, Volume: {currentVolume:F2}/{targetVolume:F2}");
                }
            }
        }
    }
    
    /// <summary>
    /// Calculate the target volume based on energy percentage
    /// </summary>
    private void CalculateTargetVolume(float energyPercent)
    {
        if (energyPercent <= fullVolumeThreshold)
        {
            // Below 5% energy - full volume
            targetVolume = maxVolume;
        }
        else if (energyPercent <= fadeStartThreshold)
        {
            // Between 5% and 20% - smooth fade
            // Map energy from [fullVolumeThreshold, fadeStartThreshold] to [maxVolume, minVolume]
            float fadeRange = fadeStartThreshold - fullVolumeThreshold;
            float energyInRange = energyPercent - fullVolumeThreshold;
            float fadeProgress = energyInRange / fadeRange;
            
            // Use smooth curve for natural fade (inverse because lower energy = higher volume)
            float smoothProgress = Mathf.SmoothStep(0f, 1f, fadeProgress);
            targetVolume = Mathf.Lerp(maxVolume, minVolume, smoothProgress);
        }
        else
        {
            // Above 20% energy - silent
            targetVolume = minVolume;
        }
    }
    
    /// <summary>
    /// Handle starting/stopping the sound based on target volume
    /// </summary>
    private void UpdateSoundPlayback()
    {
        // Should we be playing?
        bool shouldPlay = targetVolume > minVolume;
        
        if (shouldPlay && !isPlaying)
        {
            // Start playing
            StartSound();
        }
        else if (!shouldPlay && isPlaying)
        {
            // Stop playing (with fade out)
            if (currentVolume <= minVolume + 0.01f)
            {
                StopSound();
            }
        }
        
        // Validate sound handle is still valid
        if (isPlaying && !currentSoundHandle.IsValid)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[OutOfBreathSoundManager] Sound handle became invalid, restarting...");
            isPlaying = false;
            StartSound();
        }
    }
    
    /// <summary>
    /// Start playing the out-of-breath sound
    /// </summary>
    private void StartSound()
    {
        if (soundEvents.outOfBreathLoop == null) return;
        
        // Start at minimum volume for smooth fade in
        currentVolume = minVolume;
        
        // Play the looping sound as 2D (not positional)
        currentSoundHandle = soundEvents.outOfBreathLoop.Play2D(1f);
        
        if (currentSoundHandle.IsValid)
        {
            currentSoundHandle.SetVolume(currentVolume);
            isPlaying = true;
            
            if (enableDebugLogs)
                Debug.Log("[OutOfBreathSoundManager] Started out-of-breath sound");
        }
        else
        {
            Debug.LogError("[OutOfBreathSoundManager] Failed to start out-of-breath sound!");
        }
    }
    
    /// <summary>
    /// Stop playing the out-of-breath sound
    /// </summary>
    private void StopSound()
    {
        if (currentSoundHandle.IsValid)
        {
            currentSoundHandle.Stop();
            
            if (enableDebugLogs)
                Debug.Log("[OutOfBreathSoundManager] Stopped out-of-breath sound");
        }
        
        isPlaying = false;
        currentVolume = minVolume;
    }
    
    void OnDestroy()
    {
        // Clean up sound on destroy
        if (currentSoundHandle.IsValid)
        {
            currentSoundHandle.Stop();
        }
    }
    
    void OnDisable()
    {
        // Clean up sound when disabled
        if (currentSoundHandle.IsValid)
        {
            currentSoundHandle.Stop();
        }
        isPlaying = false;
        currentVolume = minVolume;
    }
}
