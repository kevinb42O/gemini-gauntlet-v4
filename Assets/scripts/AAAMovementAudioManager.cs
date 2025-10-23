using UnityEngine;
using GeminiGauntlet.Audio;
using System.Collections;

/// <summary>
/// Professional Audio Manager for AAA Movement System
/// Handles footsteps, movement sounds, and environmental audio with dynamic mixing
/// </summary>
public class AAAMovementAudioManager : MonoBehaviour
{
    [Header("=== AUDIO SOURCES ===")]
    [SerializeField] private AudioSource footstepSource;
    [SerializeField] private AudioSource movementSource;
    [SerializeField] private AudioSource environmentSource;
    
    [Header("=== FOOTSTEP SOUNDS ===")]
    [SerializeField] private FootstepSoundSet[] surfaceSounds;
    [SerializeField] private float footstepVolumeMultiplier = 1f;
    [SerializeField] private float footstepPitchVariation = 0.1f;
    
    [Header("=== MOVEMENT SOUNDS ===")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip doubleJumpSound;
    [SerializeField] private AudioClip landSound;
    [SerializeField] private AudioClip slideStartSound;
    [SerializeField] private AudioClip slidingLoopSound;
    [SerializeField] private AudioClip crouchSound;
    [SerializeField] private AudioClip sprintBreathingSound;
    
    [Header("=== ENVIRONMENTAL AUDIO ===")]
    [SerializeField] private float windIntensityMultiplier = 1f;
    [SerializeField] private AudioClip windSound;
    [SerializeField] private AudioClip ambientSound;
    
    [Header("=== DYNAMIC MIXING ===")]
    [SerializeField] private AnimationCurve speedToVolumeMultiplier = AnimationCurve.Linear(0, 0.5f, 1, 1.5f);
    [SerializeField] private AnimationCurve speedToPitchMultiplier = AnimationCurve.Linear(0, 0.9f, 1, 1.1f);
    
    // Private variables
    private AAAMovementController movementController;
    private string currentSurface = "default";
    private bool isPlayingSlideLoop = false;
    private bool isPlayingSprintBreathing = false;
    private Coroutine slideLoopCoroutine;
    private Coroutine sprintBreathingCoroutine;
    
    // Surface detection
    private LayerMask surfaceDetectionMask = -1;
    private float surfaceCheckDistance = 0.5f;
    
    [System.Serializable]
    public class FootstepSoundSet
    {
        public string surfaceName;
        public AudioClip[] walkSounds;
        public AudioClip[] runSounds;
        public AudioClip[] sprintSounds;
        public float volumeMultiplier = 1f;
        public float pitchMultiplier = 1f;
    }
    
    void Start()
    {
        movementController = GetComponent<AAAMovementController>();
        
        // Initialize audio sources if not assigned
        if (footstepSource == null)
        {
            footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.spatialBlend = 0f; // 2D sound
        }
        
        if (movementSource == null)
        {
            movementSource = gameObject.AddComponent<AudioSource>();
            movementSource.spatialBlend = 0f;
        }
        
        if (environmentSource == null)
        {
            environmentSource = gameObject.AddComponent<AudioSource>();
            environmentSource.spatialBlend = 0f;
            environmentSource.loop = true;
        }
        
        // Start ambient sounds
        if (ambientSound != null)
        {
            environmentSource.clip = ambientSound;
            environmentSource.Play();
        }
    }
    
    void Update()
    {
        // DISABLED: UpdateSurfaceDetection();
        // DISABLED: UpdateEnvironmentalAudio();
        // DISABLED: UpdateSprintBreathing();
        
        // No update functionality needed - only direct Jump/Land sound triggers used
    }
    
    private void UpdateSurfaceDetection()
    {
        // DISABLED: User requested no surface-specific sounds
        return;
    }
    
    private void UpdateEnvironmentalAudio()
    {
        if (movementController == null) return;
        
        // Adjust wind sound based on movement speed
        float speedNormalized = movementController.CurrentSpeed / 20f; // Normalize to 0-1 range
        float windVolume = speedNormalized * windIntensityMultiplier;
        
        if (windSound != null && !environmentSource.isPlaying)
        {
            environmentSource.clip = windSound;
            environmentSource.Play();
        }
        
        if (environmentSource.isPlaying)
        {
            environmentSource.volume = windVolume;
        }
    }
    
    private void UpdateSprintBreathing()
    {
        bool shouldPlayBreathing = Input.GetKey(Controls.Sprint) && 
                                  movementController.CurrentSpeed > 12f && 
                                  movementController.IsGrounded;
        
        if (shouldPlayBreathing && !isPlayingSprintBreathing)
        {
            StartSprintBreathing();
        }
        else if (!shouldPlayBreathing && isPlayingSprintBreathing)
        {
            StopSprintBreathing();
        }
    }
    
    public void PlayFootstep()
    {
        // DISABLED: User requested no footstep sounds
        // GameSounds.PlayPlayerFootstep(currentSurface, finalVolume);
        return;
    }
    
    public void PlayJumpSound()
    {
        GameSounds.PlayPlayerJump(transform.position);
    }
    
    public void PlayDoubleJumpSound()
    {
        if (doubleJumpSound != null)
        {
            // Use the assigned double jump sound if available
            movementSource.PlayOneShot(doubleJumpSound);
            Debug.Log("<color=magenta>🔊 DOUBLE JUMP SOUND PLAYED (Custom)</color>");
        }
        else
        {
            // Fall back to GameSounds system with a different pitch for distinction
            GameSounds.PlayPlayerDoubleJump(transform.position, 1.2f); // Higher pitch fallback
            Debug.Log("<color=magenta>🔊 DOUBLE JUMP SOUND PLAYED (Fallback)</color>");
        }
    }
    
    public void PlayLandSound()
    {
        float landingIntensity = Mathf.Clamp01(movementController.Velocity.y / -10f);
        GameSounds.PlayPlayerLand(transform.position, landingIntensity);
    }
    
    public void PlaySlideStart()
    {
        // DISABLED: User requested no slide sounds
        // GameSounds.PlayPlayerSlideStart();
        // StartSlideLoop();
        return;
    }
    
    public void StopSlide()
    {
        // DISABLED: User requested no slide sounds
        // StopSlideLoop();
        return;
    }
    
    public void PlayCrouchSound()
    {
        // DISABLED: User requested no crouch sounds
        // GameSounds.PlayPlayerCrouch();
        return;
    }
    
    private void StartSlideLoop()
    {
        if (slidingLoopSound != null && !isPlayingSlideLoop)
        {
            isPlayingSlideLoop = true;
            slideLoopCoroutine = StartCoroutine(SlideLoopCoroutine());
        }
    }
    
    private void StopSlideLoop()
    {
        if (isPlayingSlideLoop)
        {
            isPlayingSlideLoop = false;
            if (slideLoopCoroutine != null)
            {
                StopCoroutine(slideLoopCoroutine);
            }
        }
    }
    
    private void StartSprintBreathing()
    {
        if (sprintBreathingSound != null && !isPlayingSprintBreathing)
        {
            isPlayingSprintBreathing = true;
            sprintBreathingCoroutine = StartCoroutine(SprintBreathingCoroutine());
        }
    }
    
    private void StopSprintBreathing()
    {
        if (isPlayingSprintBreathing)
        {
            isPlayingSprintBreathing = false;
            if (sprintBreathingCoroutine != null)
            {
                StopCoroutine(sprintBreathingCoroutine);
            }
        }
    }
    
    private IEnumerator SlideLoopCoroutine()
    {
        while (isPlayingSlideLoop && movementController.IsSliding)
        {
            movementSource.PlayOneShot(slidingLoopSound);
            yield return new WaitForSeconds(slidingLoopSound.length * 0.8f);
        }
        isPlayingSlideLoop = false;
    }
    
    private IEnumerator SprintBreathingCoroutine()
    {
        while (isPlayingSprintBreathing)
        {
            movementSource.PlayOneShot(sprintBreathingSound);
            yield return new WaitForSeconds(sprintBreathingSound.length + Random.Range(0.5f, 1.5f));
        }
        isPlayingSprintBreathing = false;
    }
    
    private FootstepSoundSet GetSoundSetForSurface(string surface)
    {
        foreach (FootstepSoundSet soundSet in surfaceSounds)
        {
            if (soundSet.surfaceName.Equals(surface, System.StringComparison.OrdinalIgnoreCase))
            {
                return soundSet;
            }
        }
        
        // Return default if available
        foreach (FootstepSoundSet soundSet in surfaceSounds)
        {
            if (soundSet.surfaceName.Equals("default", System.StringComparison.OrdinalIgnoreCase))
            {
                return soundSet;
            }
        }
        
        return surfaceSounds.Length > 0 ? surfaceSounds[0] : null;
    }
    
    private AudioClip[] GetFootstepClipsForSpeed(FootstepSoundSet soundSet)
    {
        float speed = movementController.CurrentSpeed;
        
        if (speed > 15f) // Sprint
        {
            return soundSet.sprintSounds;
        }
        else if (speed > 8f) // Run
        {
            return soundSet.runSounds;
        }
        else // Walk
        {
            return soundSet.walkSounds;
        }
    }
    
    // Public methods for external systems
    public void SetMasterVolume(float volume)
    {
        footstepSource.volume *= volume;
        movementSource.volume *= volume;
        environmentSource.volume *= volume;
    }
    
    public void SetFootstepVolume(float volume)
    {
        footstepVolumeMultiplier = volume;
    }
}
