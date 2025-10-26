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
    
    [Header("Footstep Timing - Acceleration Aware")]
    [Tooltip("Base delay between footsteps at minimum speed (seconds)")]
    [SerializeField] private float baseStepDelay = 0.5f;
    [Tooltip("Minimum delay at maximum speed (seconds) - footsteps can't go faster than this")]
    [SerializeField] private float minStepDelay = 0.25f;
    [Tooltip("Speed at which footsteps reach minimum delay (units/sec)")]
    [SerializeField] private float maxSpeedForTiming = 1485f; // Sprint speed
    [Tooltip("Enable dynamic speed-based footstep timing (AAA+ feature)")]
    [SerializeField] private bool enableDynamicTiming = true;
    
    [Header("Audio Settings")]
    [Tooltip("Volume of footstep sounds")]
    [SerializeField] private float footstepVolume = 0.7f;
    [Tooltip("Minimum movement speed to play footsteps")]
    [SerializeField] private float minSpeedForFootsteps = 50f; // Scaled for 320-unit character
    [Tooltip("Play footsteps in random order instead of sequential")]
    [SerializeField] private bool randomizeFootsteps = true;
    [Tooltip("Pitch variation range based on speed (0 = none, 0.2 = ±20%)")]
    [SerializeField] private float speedPitchVariation = 0.15f;
    
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
        float currentSpeed = movementController.CurrentSpeed;
        bool isMoving = currentSpeed > minSpeedForFootsteps;
        
        // Check if sprinting (has sprint key held AND has energy)
        bool isSprinting = Input.GetKey(Controls.Boost) && 
                          (energySystem == null || energySystem.CanSprint);
        
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
                    Debug.Log($"[Footsteps] State changed - Moving: {isMoving}, Sprinting: {isSprinting}, Speed: {currentSpeed:F1}");
                }
            }
            
            // Play footstep at appropriate intervals
            if (Time.time >= nextStepTime)
            {
                PlayFootstep(isSprinting, currentSpeed);
                
                // === AAA+ DYNAMIC TIMING ===
                // Calculate step delay based on actual movement speed
                float stepDelay;
                if (enableDynamicTiming)
                {
                    // Interpolate delay between base and min based on speed
                    // Slower movement = longer delays, faster = shorter delays
                    float speedRatio = Mathf.Clamp01(currentSpeed / maxSpeedForTiming);
                    stepDelay = Mathf.Lerp(baseStepDelay, minStepDelay, speedRatio);
                }
                else
                {
                    // Legacy: Use base delay for all speeds
                    stepDelay = baseStepDelay;
                }
                
                nextStepTime = Time.time + stepDelay;
                
                if (enableDebugLogs)
                {
                    Debug.Log($"[Footsteps] Speed: {currentSpeed:F1}, Delay: {stepDelay:F3}s, Next: {nextStepTime:F2}");
                }
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
    /// Play a footstep sound from the array with dynamic pitch based on speed
    /// </summary>
    private void PlayFootstep(bool isSprinting, float currentSpeed)
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
            // === AAA+ DYNAMIC PITCH ===
            // Scale pitch with speed for natural footstep feel
            float basePitch = 1f;
            if (speedPitchVariation > 0f && enableDynamicTiming)
            {
                // Calculate pitch variation based on speed ratio
                float speedRatio = Mathf.Clamp01(currentSpeed / maxSpeedForTiming);
                // Range: 1.0 - variation to 1.0 + variation (e.g., 0.85 to 1.15 with 0.15 variation)
                basePitch = 1f + (speedRatio - 0.5f) * speedPitchVariation * 2f;
            }
            
            // Play using the SoundEvent system (respects all settings)
            footstepSource.pitch = basePitch;
            footstepSource.PlayOneShot(soundEvent.clip, footstepVolume * soundEvent.volume);
            
            if (enableDebugLogs)
            {
                string soundType = isSprinting && sprintFootstepSoundEvents != null && sprintFootstepSoundEvents.Length > 0 ? "SPRINT" : "WALK";
                Debug.Log($"[Footsteps] {soundType} ({soundEvent.clip.name}) - Speed: {currentSpeed:F1}, Pitch: {basePitch:F2}");
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
        float currentSpeed = movementController != null ? movementController.CurrentSpeed : 0f;
        PlayFootstep(isSprinting, currentSpeed);
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
