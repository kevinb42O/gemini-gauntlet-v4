using UnityEngine;
using GeminiGauntlet.Audio;

/// <summary>
/// Professional footstep sound controller with 5 individual footstep sounds
/// Plays sounds based on movement state (walk/sprint) with proper timing
/// </summary>
public class PlayerFootstepController : MonoBehaviour
{
    [Header("Footstep Sounds (Auto-loaded from SoundEvents)")]
    [Tooltip("Footstep sounds are automatically loaded from your SoundEvents asset")]
    [SerializeField] private bool useSoundEventsSystem = true;
    
    [Header("Footstep Timing")]
    [Tooltip("Delay between footsteps when walking (seconds)")]
    [SerializeField] private float walkStepDelay = 0.5f;
    [Tooltip("Delay between footsteps when sprinting (seconds)")]
    [SerializeField] private float sprintStepDelay = 0.3f;
    
    [Header("Audio Settings")]
    [Tooltip("Volume of footstep sounds")]
    [SerializeField] private float footstepVolume = 0.7f;
    [Tooltip("Minimum movement speed to play footsteps")]
    [SerializeField] private float minSpeedForFootsteps = 1f;
    [Tooltip("Play footsteps in random order instead of sequential")]
    [SerializeField] private bool randomizeFootsteps = true;
    
    [Header("References")]
    [Tooltip("Audio source for footsteps (auto-created if null)")]
    [SerializeField] private AudioSource footstepSource;
    [Tooltip("Movement controller reference (auto-found if null)")]
    [SerializeField] private AAAMovementController movementController;
    [Tooltip("Energy system reference (auto-found if null)")]
    [SerializeField] private PlayerEnergySystem energySystem;
    [Tooltip("Crouch system reference (auto-found if null)")]
    [SerializeField] private CleanAAACrouch crouchSystem;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    // Private state
    private SoundEvent[] footstepSoundEvents;
    private SoundEvent[] sprintFootstepSoundEvents;
    private int currentFootstepIndex = 0;
    private float nextStepTime = 0f;
    private bool wasMovingLastFrame = false;
    private bool wasSprintingLastFrame = false;
    
    void Start()
    {
        // Auto-find movement controller
        if (movementController == null)
        {
            movementController = GetComponent<AAAMovementController>();
            if (movementController == null)
            {
                Debug.LogError("[PlayerFootstepController] AAAMovementController not found!");
            }
        }
        
        // Auto-find energy system
        if (energySystem == null)
        {
            energySystem = GetComponent<PlayerEnergySystem>();
        }
        
        // Auto-find crouch system
        if (crouchSystem == null)
        {
            crouchSystem = GetComponent<CleanAAACrouch>();
        }
        
        // Create audio source if not assigned
        if (footstepSource == null)
        {
            footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.spatialBlend = 0f; // 2D sound
            footstepSource.playOnAwake = false;
        }
        
        // Load footstep sounds from SoundEvents
        LoadFootstepSounds();
        
        if (enableDebugLogs)
        {
            Debug.Log($"[PlayerFootstepController] Initialized with {(footstepSoundEvents != null ? footstepSoundEvents.Length : 0)} footstep sounds");
        }
    }
    
    /// <summary>
    /// Load footstep sounds from the SoundEvents system
    /// </summary>
    private void LoadFootstepSounds()
    {
        if (!useSoundEventsSystem)
        {
            Debug.LogWarning("[PlayerFootstepController] SoundEvents system disabled. No footsteps will play.");
            return;
        }
        
        // Get walk footstep sounds from SoundEvents
        if (SoundEventsManager.Events != null && SoundEventsManager.Events.footstepSounds != null)
        {
            footstepSoundEvents = SoundEventsManager.Events.footstepSounds;
            
            if (footstepSoundEvents.Length == 0)
            {
                Debug.LogWarning("[PlayerFootstepController] No walk footstep sounds assigned in SoundEvents asset!");
            }
            else
            {
                Debug.Log($"[PlayerFootstepController] Loaded {footstepSoundEvents.Length} walk footstep sounds from SoundEvents");
            }
        }
        else
        {
            Debug.LogError("[PlayerFootstepController] SoundEvents not found or footstepSounds array is null!");
        }
        
        // Get sprint footstep sounds from SoundEvents
        if (SoundEventsManager.Events != null && SoundEventsManager.Events.sprintFootstepSounds != null)
        {
            sprintFootstepSoundEvents = SoundEventsManager.Events.sprintFootstepSounds;
            
            if (sprintFootstepSoundEvents.Length == 0)
            {
                Debug.LogWarning("[PlayerFootstepController] No sprint footstep sounds assigned - will use walk sounds for sprinting");
            }
            else
            {
                Debug.Log($"[PlayerFootstepController] Loaded {sprintFootstepSoundEvents.Length} sprint footstep sounds from SoundEvents");
            }
        }
        else
        {
            Debug.LogWarning("[PlayerFootstepController] Sprint footstep sounds not found - will use walk sounds for sprinting");
        }
    }
    
    void Update()
    {
        if (movementController == null || footstepSoundEvents == null || footstepSoundEvents.Length == 0)
            return;
        
        // Check if player is sliding OR diving - if so, skip all footstep logic
        if (crouchSystem != null && (crouchSystem.IsSliding || crouchSystem.IsDiving || crouchSystem.IsDiveProne))
        {
            // Reset state when sliding/diving to avoid footstep on slide/dive end
            wasMovingLastFrame = false;
            wasSprintingLastFrame = false;
            nextStepTime = 0f;
            return;
        }
        
        // Check if player is grounded and moving
        bool isGrounded = movementController.IsGrounded;
        bool isMoving = movementController.CurrentSpeed > minSpeedForFootsteps;
        
        // Check if sprinting (has sprint key held AND has energy)
        bool isSprinting = Input.GetKey(Controls.Boost) && 
                          (energySystem == null || energySystem.CanSprint) &&
                          movementController.CurrentSpeed > 8f;
        
        // Only play footsteps when grounded and moving
        if (isGrounded && isMoving)
        {
            // Detect state changes
            bool justStartedMoving = isMoving && !wasMovingLastFrame;
            bool sprintStateChanged = isSprinting != wasSprintingLastFrame;
            
            // Reset timing when starting to move or changing sprint state
            if (justStartedMoving || sprintStateChanged)
            {
                nextStepTime = Time.time;
                
                if (enableDebugLogs)
                {
                    Debug.Log($"[Footsteps] State changed - Moving: {isMoving}, Sprinting: {isSprinting}");
                }
            }
            
            // Play footstep at appropriate intervals
            if (Time.time >= nextStepTime)
            {
                PlayFootstep(isSprinting);
                
                // Set next step time based on movement state
                float stepDelay = isSprinting ? sprintStepDelay : walkStepDelay;
                nextStepTime = Time.time + stepDelay;
            }
        }
        else
        {
            // Reset when not moving
            if (wasMovingLastFrame)
            {
                nextStepTime = 0f;
            }
        }
        
        // Update state tracking
        wasMovingLastFrame = isMoving;
        wasSprintingLastFrame = isSprinting;
    }
    
    /// <summary>
    /// Play a footstep sound from the array
    /// </summary>
    private void PlayFootstep(bool isSprinting)
    {
        if (footstepSource == null)
            return;
        
        // Choose which sound array to use based on sprint state
        SoundEvent[] soundArray;
        
        if (isSprinting && sprintFootstepSoundEvents != null && sprintFootstepSoundEvents.Length > 0)
        {
            // Use sprint sounds
            soundArray = sprintFootstepSoundEvents;
        }
        else if (footstepSoundEvents != null && footstepSoundEvents.Length > 0)
        {
            // Use walk sounds (fallback for sprint if no sprint sounds)
            soundArray = footstepSoundEvents;
        }
        else
        {
            // No sounds available
            return;
        }
        
        // Select footstep sound event (random or sequential)
        SoundEvent soundEvent;
        if (randomizeFootsteps)
        {
            // Pick a random footstep
            int randomIndex = Random.Range(0, soundArray.Length);
            soundEvent = soundArray[randomIndex];
        }
        else
        {
            // Sequential playback
            currentFootstepIndex = currentFootstepIndex % soundArray.Length;
            soundEvent = soundArray[currentFootstepIndex];
            currentFootstepIndex = (currentFootstepIndex + 1) % soundArray.Length;
        }
        
        if (soundEvent != null && soundEvent.clip != null)
        {
            // Play using the SoundEvent system (respects all settings)
            footstepSource.pitch = 1f; // No pitch variation
            footstepSource.PlayOneShot(soundEvent.clip, footstepVolume * soundEvent.volume);
            
            if (enableDebugLogs)
            {
                string soundType = isSprinting && sprintFootstepSoundEvents != null && sprintFootstepSoundEvents.Length > 0 ? "SPRINT" : "WALK";
                Debug.Log($"[Footsteps] Playing {soundType} footstep ({soundEvent.clip.name}) - Sprint: {isSprinting}");
            }
        }
    }
    
    
    /// <summary>
    /// Manually trigger a footstep (useful for animation events)
    /// </summary>
    public void TriggerFootstep()
    {
        bool isSprinting = Input.GetKey(Controls.Boost) && 
                          (energySystem == null || energySystem.CanSprint);
        PlayFootstep(isSprinting);
    }
    
    /// <summary>
    /// Set footstep volume at runtime
    /// </summary>
    public void SetFootstepVolume(float volume)
    {
        footstepVolume = Mathf.Clamp01(volume);
    }
    
    /// <summary>
    /// Enable or disable footstep sounds
    /// </summary>
    public void SetFootstepsEnabled(bool enabled)
    {
        this.enabled = enabled;
    }
}
