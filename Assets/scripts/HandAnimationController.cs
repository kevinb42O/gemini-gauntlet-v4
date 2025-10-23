using UnityEngine;
using System.Collections;

/// <summary>
/// Robust hand animation controller with clean state management.
/// Features:
/// - Lock mechanism prevents unwanted interruptions
/// - Clear priority hierarchy (Emotes > ArmorPlate > Slide > Sprint > Shotgun > Movement)
/// - Natural animation flow without forced idle fallbacks
/// - One-shot animations complete fully before transitioning
/// - Movement system integrates seamlessly
/// - Both hands work together for most actions (simple and reliable)
/// </summary>
public class HandAnimationController : MonoBehaviour
{
    // === EMOTE EVENT SYSTEM FOR COMPANION MIRRORING ===
    public static event System.Action<int> OnPlayerEmote; // Emote number (1-4)

    [Header("Hand Setup - All Levels")]
    [Tooltip("Left hand animators - Level 1 to 4")]
    public Animator[] leftHandAnimators = new Animator[4];  // L1, L2, L3, L4
    [Tooltip("Right hand animators - Level 1 to 4")]
    public Animator[] rightHandAnimators = new Animator[4]; // R1, R2, R3, R4
    
    [Header("Player Progression Integration")]
    [Tooltip("Reference to PlayerProgression (auto-found if null)")]
    public PlayerProgression playerProgression;
    
    [Header("Movement Integration")]
    [Tooltip("Reference to AAAMovementController (auto-found if null)")]
    public AAAMovementController aaaMovementController;
    [Tooltip("Reference to CleanAAACrouch for sliding detection (auto-found if null)")]
    public CleanAAACrouch cleanAAACrouch;
    [Tooltip("Reference to CelestialDriftController (auto-found if null)")]
    public CelestialDriftController celestialDriftController;
    [Tooltip("Reference to AAAMovementIntegrator (auto-found if null)")]
    public AAAMovementIntegrator aaaMovementIntegrator;
    
    [Header("Energy System Integration")]
    [Tooltip("Reference to PlayerEnergySystem (auto-found if null)")]
    public PlayerEnergySystem playerEnergySystem;
    
    [Header("Shooting System Integration")]
    [Tooltip("Reference to PlayerShooterOrchestrator (auto-found if null)")]
    public PlayerShooterOrchestrator playerShooterOrchestrator;
    
    [Header("Animation Clips - Assign in Inspector")]
    [Tooltip("Left hand idle animation clip")]
    public AnimationClip leftIdleClip;
    [Tooltip("Right hand idle animation clip")]
    public AnimationClip rightIdleClip;
    [Tooltip("Left hand walk animation clip")]
    public AnimationClip leftWalkClip;
    [Tooltip("Right hand walk animation clip")]
    public AnimationClip rightWalkClip;
    [Tooltip("Left hand sprint animation clip")]
    public AnimationClip leftSprintClip;
    [Tooltip("Right hand sprint animation clip")]
    public AnimationClip rightSprintClip;
    [Tooltip("Left hand jump animation clip")]
    public AnimationClip leftJumpClip;
    [Tooltip("Right hand jump animation clip")]
    public AnimationClip rightJumpClip;
    [Tooltip("Left hand beam loop animation clip")]
    public AnimationClip leftBeamLoopClip;
    [Tooltip("Right hand beam loop animation clip")]
    public AnimationClip rightBeamLoopClip;
    [Tooltip("Left hand shotgun animation clip")]
    public AnimationClip leftShotgunClip;
    [Tooltip("Right hand shotgun animation clip")]
    public AnimationClip rightShotgunClip;
    [Tooltip("Left hand slide animation clip")]
    public AnimationClip leftSlideClip;
    [Tooltip("Right hand slide animation clip")]
    public AnimationClip rightSlideClip;
    [Tooltip("Left hand dive animation clip (arms forward, dolphin dive)")]
    public AnimationClip leftDiveClip;
    [Tooltip("Right hand dive animation clip (arms forward, dolphin dive)")]
    public AnimationClip rightDiveClip;
    [Tooltip("Left hand landing animation clip (impact pose after significant jumps/dives)")]
    public AnimationClip leftLandClip;
    [Tooltip("Right hand landing animation clip (impact pose after significant jumps/dives)")]
    public AnimationClip rightLandClip;
    
    [Header("Flight Animation Clips")]
    [Tooltip("Left hand fly forward animation clip")]
    public AnimationClip leftFlyForwardClip;
    [Tooltip("Right hand fly forward animation clip")]
    public AnimationClip rightFlyForwardClip;
    [Tooltip("Left hand fly up animation clip")]
    public AnimationClip leftFlyUpClip;
    [Tooltip("Right hand fly up animation clip")]
    public AnimationClip rightFlyUpClip;
    [Tooltip("Left hand fly down animation clip")]
    public AnimationClip leftFlyDownClip;
    [Tooltip("Right hand fly down animation clip")]
    public AnimationClip rightFlyDownClip;
    [Tooltip("Left hand fly strafe left animation clip")]
    public AnimationClip leftFlyStrafeLeftClip;
    [Tooltip("Right hand fly strafe left animation clip")]
    public AnimationClip rightFlyStrafeLeftClip;
    [Tooltip("Left hand fly strafe right animation clip")]
    public AnimationClip leftFlyStrafeRightClip;
    [Tooltip("Right hand fly strafe right animation clip")]
    public AnimationClip rightFlyStrafeRightClip;
    [Tooltip("Left hand fly boost animation clip")]
    public AnimationClip leftFlyBoostClip;
    [Tooltip("Right hand fly boost animation clip")]
    public AnimationClip rightFlyBoostClip;
    [Tooltip("Left hand take off animation clip")]
    public AnimationClip leftTakeOffClip;
    [Tooltip("Right hand take off animation clip")]
    public AnimationClip rightTakeOffClip;
    
    [Header("Armor Plate Animation Clips")]
    [Tooltip("Right hand apply plate animation clip (one-shot)")]
    public AnimationClip rightApplyPlateClip;
    
    [Header("Emote Animation Clips")]
    [Tooltip("Left hand emote 1 animation clip")]
    public AnimationClip leftEmote1Clip;
    [Tooltip("Right hand emote 1 animation clip")]
    public AnimationClip rightEmote1Clip;
    [Tooltip("Left hand emote 2 animation clip")]
    public AnimationClip leftEmote2Clip;
    [Tooltip("Right hand emote 2 animation clip")]
    public AnimationClip rightEmote2Clip;
    [Tooltip("Left hand emote 3 animation clip")]
    public AnimationClip leftEmote3Clip;
    [Tooltip("Right hand emote 3 animation clip")]
    public AnimationClip rightEmote3Clip;
    [Tooltip("Left hand emote 4 animation clip")]
    public AnimationClip leftEmote4Clip;
    [Tooltip("Right hand emote 4 animation clip")]
    public AnimationClip rightEmote4Clip;
    
    [Header("=== INSPECTOR CONFIGURATION SYSTEM ===")]
    
    [Header("Animation Priorities (Higher = More Important)")]
    [Tooltip("IDLE priority - should always be 0 (lowest)")]
    [Range(0, 20)] public int idlePriority = 0;
    [Tooltip("Emote priority - higher than IDLE but overridable")]
    [Range(0, 20)] public int emotePriority = 2;
    [Tooltip("Flight animations priority - DISABLED (not needed for now)")]
    [Range(0, 20)] public int flightPriority = 0;
    [Tooltip("Tactical actions (Dive/Slide) priority")]
    [Range(0, 20)] public int tacticalPriority = 4;
    [Tooltip("Walk animation priority - DISABLED (arms invisible, not needed)")]
    [Range(0, 20)] public int walkPriority = 0;
    [Tooltip("Tactical override (active Slide/Dive) priority - LOWERED so jump can interrupt")]
    [Range(0, 20)] public int tacticalOverridePriority = 6;
    [Tooltip("Sprint animation priority")]
    [Range(0, 20)] public int sprintPriority = 7;
    [Tooltip("Brief combat (Shotgun/Beam) priority - combat actions")]
    [Range(0, 20)] public int briefCombatPriority = 8;
    [Tooltip("One-shot animations (Jump/Land/TakeOff) priority - HIGHEST RESPONSIVE MOVEMENT")]
    [Range(0, 20)] public int oneShotPriority = 10;
    [Tooltip("Ability (ArmorPlate) priority - HIGHEST, uninterruptible")]
    [Range(0, 20)] public int abilityPriority = 10;
    
    [System.Serializable]
    public class EmoteConfig
    {
        [Tooltip("Key to press for this emote")]
        public KeyCode triggerKey = KeyCode.Alpha1;
        [Tooltip("Which hands should play this emote")]
        public HandSelection handSelection = HandSelection.Both;
        [Tooltip("Left hand animation clip")]
        public AnimationClip leftClip;
        [Tooltip("Right hand animation clip")]
        public AnimationClip rightClip;
        [Tooltip("Custom name for this emote")]
        public string emoteName = "Emote";
    }
    
    public enum HandSelection
    {
        LeftOnly,
        RightOnly,
        Both
    }
    
    [Header("Emote Configuration")]
    [Tooltip("Configure up to 8 emotes with custom keys and hand selection")]
    public EmoteConfig[] emoteConfigs = new EmoteConfig[4]
    {
        new EmoteConfig { triggerKey = KeyCode.Alpha1, emoteName = "Emote 1" },
        new EmoteConfig { triggerKey = KeyCode.Alpha2, emoteName = "Emote 2" },
        new EmoteConfig { triggerKey = KeyCode.Alpha3, emoteName = "Emote 3" },
        new EmoteConfig { triggerKey = KeyCode.Alpha4, emoteName = "Emote 4" }
    };
    
    [Header("Animation Timing & Blending")]
    [Tooltip("Default crossfade duration for smooth transitions")]
    [Range(0.0f, 2.0f)] public float crossFadeDuration = 0.25f;
    [Tooltip("Animation playback speed multiplier")]
    [Range(0.5f, 3.0f)] public float animationSpeed = 1.0f;
    [Tooltip("Instant blend time for combat actions")]
    [Range(0.0f, 0.2f)] public float instantBlendTime = 0.0f;
    [Tooltip("Fast blend time for critical transitions")]
    [Range(0.0f, 0.3f)] public float fastBlendTime = 0.15f;
    [Tooltip("Smooth blend time for movement transitions")]
    [Range(0.1f, 0.5f)] public float smoothBlendTime = 0.3f;
    [Tooltip("Slow blend time for emotes and cinematic transitions")]
    [Range(0.2f, 1.0f)] public float slowBlendTime = 0.8f;
    
    [Header("Combat Timing")]
    [Tooltip("How long shotgun/beam animations lock hands (seconds)")]
    [Range(0.5f, 5.0f)] public float combatLockDuration = 1.2f;
    
    [Header("Debug & Behavior Settings")]
    [Tooltip("Enable detailed debug logging in console")]
    public bool enableDebugLogs = true;
    [Tooltip("Show emote key presses in debug logs")]
    public bool debugEmoteInput = true;
    [Tooltip("Show state transitions in debug logs")]
    public bool debugStateTransitions = true;
    [Tooltip("Show priority conflicts in debug logs")]
    public bool debugPriorityConflicts = true;
    
    // CLEANED: Removed all deprecated animation trigger hashes - using clips only
    
    // === AAA ANIMATION ORCHESTRATION SYSTEM ===
    // Priority-based state hierarchy inspired by industry leaders:
    // - God of War: Committed action system
    // - Doom Eternal: Instant combat responsiveness
    // - Apex Legends: Movement fluidity with combat priority
    // - Destiny 2: Ability lock system
    
    /// <summary>
    /// Animation states with numerical priority values
    /// Higher number = Higher priority
    /// CRITICAL: SPRINT ALWAYS PLAYS! Only Shotgun/Beam can briefly interrupt
    /// Sprint animation must be enjoyed - it's amazing! 🏃
    /// Shotgun/Beam play briefly then auto-return to sprint
    /// </summary>
    private enum HandAnimationState
    {
        Idle = 0,           // Priority 0 - Default, always interruptible
        FlyForward = 3,     // Priority 3 - Flight locomotion
        FlyUp = 4,          // Priority 3 - Flight locomotion
        FlyDown = 5,        // Priority 3 - Flight locomotion
        FlyStrafeLeft = 6,  // Priority 3 - Flight locomotion
        FlyStrafeRight = 7, // Priority 3 - Flight locomotion
        FlyBoost = 8,       // Priority 3 - Flight locomotion
        Dive = 12,          // Priority 4 - Tactical action (soft locked)
        Slide = 13,         // Priority 4 - Tactical action (soft locked)
        Walk = 14,          // Priority 5 - Basic locomotion
        Jump = 9,           // Priority 6 - One-shot movement
        Land = 10,          // Priority 6 - One-shot movement
        TakeOff = 11,       // Priority 6 - Critical transition
        Shotgun = 1,        // Priority 7 - Brief combat interrupt (auto-returns to sprint)
        Beam = 2,           // Priority 7 - Brief combat interrupt (auto-returns to sprint)
        Sprint = 15,        // Priority 8 - SPRINT IS KING! Amazing animation must play!
        ArmorPlate = 16,    // Priority 9 - Critical ability (HARD LOCKED)
        Emote = 17          // Priority 10 - Player expression (HARD LOCKED, highest)
    }
    
    /// <summary>
    /// Per-hand state tracking with advanced lock mechanisms
    /// Supports both hard locks (emotes, armor) and soft locks (combat actions)
    /// </summary>
    private class HandState
    {
        public HandAnimationState currentState = HandAnimationState.Idle;
        public HandAnimationState previousState = HandAnimationState.Idle;
        public bool beamWasActiveBeforeInterruption = false;
        public Coroutine animationCompletionCoroutine = null;
        public float stateStartTime = 0f;
        public bool isEmotePlaying = false;
        public bool isLocked = false; // Hard lock - nothing can interrupt
        public bool isSoftLocked = false; // Soft lock - only higher priority can interrupt
        public float lockDuration = 0f; // How long is this state locked for
        
        public void Reset()
        {
            previousState = currentState;
            currentState = HandAnimationState.Idle;
            beamWasActiveBeforeInterruption = false;
            animationCompletionCoroutine = null;
            stateStartTime = Time.time;
            isEmotePlaying = false;
            isLocked = false;
            isSoftLocked = false;
            lockDuration = 0f;
        }
    }
    
    private HandState _leftHandState = new HandState();
    private HandState _rightHandState = new HandState();
    
    // SUPER ROBUST: Track current movement state for efficient change detection
    private HandAnimationState _currentMovementState = HandAnimationState.Idle;
    
    // Backward compatibility flags (read-only, derived from state machine)
    private bool _leftBeamActive => _leftHandState.currentState == HandAnimationState.Beam;
    private bool _rightBeamActive => _rightHandState.currentState == HandAnimationState.Beam;
    
    // Dive override - pauses automatic animation updates
    private bool _isDiveOverrideActive = false;
    private bool _leftEmoteActive => _leftHandState.isEmotePlaying;
    private bool _rightEmoteActive => _rightHandState.isEmotePlaying;
    
    // Flight state tracking
    private bool _isInFlightMode = false;
    private bool _isCurrentlyFlying = false;
    private bool _isCurrentlyBoosting = false;
    private Vector3 _lastFlightInput = Vector3.zero;
    
    // Movement animation state tracking
    private bool _isCurrentlyMoving = false;
    private bool _isCurrentlySprinting = false;
    private int _currentEmoteNumber = 1; // Legacy: Current emote being played (1-4)
    private int _currentEmoteIndex = 0; // New: Current emote config index (0-based)
    
    
    // Smart landing system tracking
    private float _jumpStartTime = -999f;
    private bool _wasInAir = false;
    private bool _justCompletedDive = false;
    private const float MIN_AIR_TIME_FOR_LAND_ANIM = 1.0f; // 1 second minimum air time
    
    // Simple jump cooldown
    private float _lastJumpTime = -999f;
    private const float JUMP_COOLDOWN = 0.5f;
    
    // Input thresholds
    private const float FLIGHT_INPUT_THRESHOLD = 0.1f;
    private const float FLIGHT_DIRECTION_THRESHOLD = 0.6f;
    private const float FLIGHT_CHANGE_THRESHOLD = 0.2f;
    
    // === AAA ANIMATION PRIORITY & BLEND CONFIGURATION ===
    // Based on industry research from God of War, Doom Eternal, Apex Legends
    
    /// <summary>
    /// Animation priority levels - defines interruption hierarchy
    /// CRITICAL: SPRINT IS KING! Only brief combat interrupts allowed
    /// Sprint animation must be enjoyed - it's the star of the show! 🏃
    /// </summary>
    private static class AnimationPriority
    {
        public const int IDLE = 0;          // Always interruptible
        // EMOTE = 2                       // Higher than IDLE, can be overridden by most things
        public const int FLIGHT = 3;        // All flight animations
        public const int TACTICAL = 4;      // Dive, Slide - soft locked
        public const int WALK = 5;          // Walk - basic locomotion
        public const int ONE_SHOT = 6;      // Jump, Land, TakeOff
        public const int BRIEF_COMBAT = 7;  // Shotgun/Beam - brief interrupt, auto-return to sprint
        public const int SPRINT = 8;        // SPRINT IS KING! Amazing animation must play!
        public const int TACTICAL_OVERRIDE = 9; // Slide/Dive - MUST override sprint! Active tactical actions
        public const int ABILITY = 10;      // ArmorPlate - hard locked
        public const int EMOTE = 11;        // DEPRECATED - now using priority 2 for easy override
    }
    
    /// <summary>
    /// Blend time configuration based on animation type
    /// Instant for combat, smooth for movement - AAA standard
    /// </summary>
    private static class BlendTime
    {
        public const float INSTANT = 0.0f;      // Combat actions (shotgun, hit reaction)
        public const float VERY_FAST = 0.05f;   // Critical transitions
        public const float FAST = 0.1f;         // Combat-to-movement
        public const float NORMAL = 0.2f;       // Default
        public const float SMOOTH = 0.3f;       // Movement-to-movement
        public const float SLOW = 0.4f;         // Emotes, cinematic
    }
    
    /// <summary>
    /// Combat timing configuration
    /// </summary>
    private static class CombatTiming
    {
        public const float SHOTGUN_LOCK_DURATION = 1.5f;  // Shotgun/Beam lock time (1.5 seconds)
    }
    
    // === LIFECYCLE METHODS ===
    
    private void Awake()
    {
        // Cache component references in Awake for better performance
        CacheComponentReferences();
        
        // Initialize emote keys from Controls (after InputManager has set them)
        InitializeEmoteKeysFromControls();
        
        // Initialize state machine
        _leftHandState.Reset();
        _rightHandState.Reset();
    }
    
    void Start()
    {
        // Subscribe to events
        if (playerEnergySystem != null)
            PlayerEnergySystem.OnSprintInterrupted += OnSprintInterrupted;
        
        // Auto-find animators if not assigned
        AutoFindMissingAnimators();
        
        // Set animation speeds for all hand levels
        ApplyAnimationSpeedToAllHands();
        
        // CRITICAL: Ensure hands are completely unlocked before starting
        _leftHandState.isLocked = false;
        _leftHandState.isSoftLocked = false;
        _leftHandState.isEmotePlaying = false;
        _rightHandState.isLocked = false;
        _rightHandState.isSoftLocked = false;
        _rightHandState.isEmotePlaying = false;
        
        // Start both hands in IDLE - everything else will overwrite as needed
        TransitionToState(_leftHandState, HandAnimationState.Idle, true);
        TransitionToState(_rightHandState, HandAnimationState.Idle, false);
        
        if (enableDebugLogs)
        {
            string leftInfo = GetCurrentLeftAnimator() ? GetCurrentLeftAnimator().name : "NULL";
            string rightInfo = GetCurrentRightAnimator() ? GetCurrentRightAnimator().name : "NULL";
            Debug.Log($"[HandAnimationController] Initialized - L{GetCurrentLeftHandLevel()}: {leftInfo}, R{GetCurrentRightHandLevel()}: {rightInfo}");
            Debug.Log($"[HandAnimationController] Initial state - Left: {_leftHandState.currentState} (Locked: {_leftHandState.isLocked}), Right: {_rightHandState.currentState} (Locked: {_rightHandState.isLocked})");
        }
    }
    
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (playerEnergySystem != null)
            PlayerEnergySystem.OnSprintInterrupted -= OnSprintInterrupted;
        
        // Clean up coroutines
        if (_leftHandState.animationCompletionCoroutine != null) 
            StopCoroutine(_leftHandState.animationCompletionCoroutine);
        if (_rightHandState.animationCompletionCoroutine != null) 
            StopCoroutine(_rightHandState.animationCompletionCoroutine);
        
        // Reset states
        _leftHandState.Reset();
        _rightHandState.Reset();
    }
    
    void Update()
    {
        // Skip all automatic updates if dive override is active
        if (_isDiveOverrideActive)
        {
            return;
        }
        
        // Update flight mode state
        UpdateFlightModeState();
        
        // Track air state for smart landing system
        UpdateAirStateTracking();
        
        // Jump detection handled by AAAMovementController via OnPlayerJumped()
        // CheckJumpInput(); // REMOVED: Redundant - AAAMovementController handles this
        
        // Monitor movement and update movement animations (only if not locked)
        if (!_leftHandState.isLocked && !_rightHandState.isLocked)
        {
            UpdateMovementAnimations();
        }
        
        // Monitor flight and update flight animations (only if not locked)
        if (!_leftHandState.isLocked && !_rightHandState.isLocked)
        {
            UpdateFlightAnimations();
        }
        
        // Check for emote input
        CheckEmoteInput();
    }
    
    // === INITIALIZATION METHODS ===
    
    /// <summary>
    /// Cache all component references to avoid repeated FindObjectOfType calls
    /// Called in Awake for optimal performance
    /// </summary>
    private void CacheComponentReferences()
    {
        if (playerProgression == null)
            playerProgression = FindObjectOfType<PlayerProgression>();
        
        if (aaaMovementController == null)
            aaaMovementController = FindObjectOfType<AAAMovementController>();
        
        if (cleanAAACrouch == null)
            cleanAAACrouch = FindObjectOfType<CleanAAACrouch>();
        
        if (celestialDriftController == null)
            celestialDriftController = FindObjectOfType<CelestialDriftController>();
            
        if (aaaMovementIntegrator == null)
            aaaMovementIntegrator = FindObjectOfType<AAAMovementIntegrator>();
        
        if (playerEnergySystem == null)
            playerEnergySystem = FindObjectOfType<PlayerEnergySystem>();
        
        if (playerShooterOrchestrator == null)
            playerShooterOrchestrator = FindObjectOfType<PlayerShooterOrchestrator>();
    }
    
    // === INPUT HANDLING ===
    
    
    /// <summary>
    /// Check for emote input using configurable key mapping from Inspector
    /// </summary>
    private void CheckEmoteInput()
    {
        if (_leftHandState.isEmotePlaying || _rightHandState.isEmotePlaying) 
        {
            // Check if any configured emote key was pressed while blocked
            if (debugEmoteInput && enableDebugLogs)
            {
                for (int i = 0; i < emoteConfigs.Length; i++)
                {
                    if (Input.GetKeyDown(emoteConfigs[i].triggerKey))
                    {
                        Debug.Log($"[HandAnimationController] Emote input blocked - {emoteConfigs[i].emoteName} ({emoteConfigs[i].triggerKey}) pressed but emote already playing");
                        break;
                    }
                }
            }
            return;
        }
        
        // Check all configured emote keys
        for (int i = 0; i < emoteConfigs.Length; i++)
        {
            if (Input.GetKeyDown(emoteConfigs[i].triggerKey))
            {
                if (debugEmoteInput && enableDebugLogs) 
                    Debug.Log($"[HandAnimationController] {emoteConfigs[i].triggerKey} pressed - playing {emoteConfigs[i].emoteName}");
                PlayEmoteByIndex(i);
                break; // Only process one emote per frame
            }
        }
    }
    
    
    // === STATE UPDATE METHODS ===
    
    /// <summary>
    /// Update flight mode state from AAAMovementIntegrator
    /// </summary>
    private void UpdateFlightModeState()
    {
        bool wasInFlightMode = _isInFlightMode;
        _isInFlightMode = false;
        
        // Check if we're in flight mode via AAAMovementIntegrator
        if (aaaMovementIntegrator != null)
        {
            _isInFlightMode = !aaaMovementIntegrator.useAAAMovement;
        }
        else if (celestialDriftController != null)
        {
            _isInFlightMode = celestialDriftController.isFlightUnlocked;
        }
        
        // Detect takeoff transition (AAA -> Flight)
        if (!wasInFlightMode && _isInFlightMode)
        {
            OnTakeOffStarted();
        }
    }
    
    // Monitor flight input and update flight animations
    private void UpdateFlightAnimations()
    {
        // Only update flight animations when in flight mode
        if (!_isInFlightMode) return;
        
        // Skip if hands are in high-priority states
        if (IsInHighPriorityState(_leftHandState) || IsInHighPriorityState(_rightHandState)) return;
        
        // Get flight input from CelestialDriftController
        Vector3 currentFlightInput = Vector3.zero;
        bool isBoosting = false;
        
        if (celestialDriftController != null)
        {
            // Access flight input via reflection since moveInput might be private
            var moveInputField = typeof(CelestialDriftController).GetField("moveInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (moveInputField != null)
            {
                currentFlightInput = (Vector3)moveInputField.GetValue(celestialDriftController);
            }
            
            var isBoostingField = typeof(CelestialDriftController).GetField("isBoosting", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (isBoostingField != null)
            {
                isBoosting = (bool)isBoostingField.GetValue(celestialDriftController);
            }
        }
        
        // Check if flight state has changed
        bool wasFlying = _isCurrentlyFlying;
        bool wasBoosting = _isCurrentlyBoosting;
        
        _isCurrentlyFlying = currentFlightInput.magnitude > FLIGHT_INPUT_THRESHOLD;
        _isCurrentlyBoosting = isBoosting;
        
        bool flightStateChanged = (wasFlying != _isCurrentlyFlying) || 
                                  (wasBoosting != _isCurrentlyBoosting) ||
                                  Vector3.Distance(_lastFlightInput, currentFlightInput) > FLIGHT_CHANGE_THRESHOLD;
        
        if (flightStateChanged)
        {
            _lastFlightInput = currentFlightInput;
            
            // Determine dominant flight animation (priority: boost > vertical > forward > strafe)
            HandAnimationState targetState = HandAnimationState.Idle;
            
            if (_isCurrentlyBoosting)
            {
                targetState = HandAnimationState.FlyBoost;
            }
            else if (Mathf.Abs(currentFlightInput.y) > FLIGHT_DIRECTION_THRESHOLD)
            {
                targetState = currentFlightInput.y > 0 ? HandAnimationState.FlyUp : HandAnimationState.FlyDown;
            }
            else if (Mathf.Abs(currentFlightInput.z) > FLIGHT_DIRECTION_THRESHOLD)
            {
                targetState = currentFlightInput.z > 0 ? HandAnimationState.FlyForward : HandAnimationState.Idle;
            }
            else if (Mathf.Abs(currentFlightInput.x) > FLIGHT_DIRECTION_THRESHOLD)
            {
                targetState = currentFlightInput.x > 0 ? HandAnimationState.FlyStrafeRight : HandAnimationState.FlyStrafeLeft;
            }
            else if (!_isCurrentlyFlying)
            {
                targetState = HandAnimationState.Idle;
            }
            
            // Transition both hands to the new flight state
            RequestStateTransition(_leftHandState, targetState, true);
            RequestStateTransition(_rightHandState, targetState, false);
            
            if (enableDebugLogs)
                Debug.Log($"[HandAnimationController] Flight state changed: Flying={_isCurrentlyFlying}, Boosting={_isCurrentlyBoosting}, Input={currentFlightInput}");
        }
    }
    
    // Simple movement detection - transitions hands back to movement states after combat
    private void UpdateMovementAnimations()
    {
        // Skip if in flight mode
        if (_isInFlightMode) return;
        
        // Get current movement state from EnergySystem
        bool isCurrentlySprinting = playerEnergySystem != null ? playerEnergySystem.IsCurrentlySprinting : false;
        
        // Determine target state
        HandAnimationState targetState = isCurrentlySprinting ? HandAnimationState.Sprint : HandAnimationState.Idle;
        
        // Only transition if hands are in combat states (Shotgun/Beam) and not locked
        if (_leftHandState.currentState == HandAnimationState.Shotgun || _leftHandState.currentState == HandAnimationState.Beam)
        {
            if (!_leftHandState.isLocked && !_leftHandState.isSoftLocked)
            {
                RequestStateTransition(_leftHandState, targetState, true);
            }
        }
        
        if (_rightHandState.currentState == HandAnimationState.Shotgun || _rightHandState.currentState == HandAnimationState.Beam)
        {
            if (!_rightHandState.isLocked && !_rightHandState.isSoftLocked)
            {
                RequestStateTransition(_rightHandState, targetState, false);
            }
        }
    }
    
    
    /// <summary>
    /// Check if hand is in a high-priority state that shouldn't be interrupted by movement
    /// FIXED: Includes Slide and Dive to prevent sprint from overriding while sliding
    /// </summary>
    private bool IsInHighPriorityState(HandState handState)
    {
        return handState.currentState == HandAnimationState.Beam ||
               handState.currentState == HandAnimationState.Shotgun ||
               handState.currentState == HandAnimationState.Slide ||
               handState.currentState == HandAnimationState.Dive ||
               handState.currentState == HandAnimationState.ArmorPlate ||
               handState.isEmotePlaying ||
               handState.isLocked;
    }
    
    
    /// <summary>
    /// SMART air state tracking using AAAMovementController's IsFalling state
    /// No more frame-by-frame checks - uses proper state transitions
    /// </summary>
    private void UpdateAirStateTracking()
    {
        if (aaaMovementController == null) return;
        
        bool isCurrentlyFalling = aaaMovementController.IsFalling;
        bool isGrounded = aaaMovementController.IsGrounded;
        
        // Start tracking air time when falling begins (much more reliable)
        if (isCurrentlyFalling && !_wasInAir)
        {
            _wasInAir = true;
            // If we don't have a jump start time (fell off ledge), use current time
            if (_jumpStartTime <= -999f)
            {
                _jumpStartTime = Time.time;
                if (enableDebugLogs)
                    Debug.Log("[HandAnimationController] SMART: Started tracking air time (falling detected)");
            }
        }
        
        // Reset air tracking when grounded (but let OnPlayerLanded handle the animation)
        if (isGrounded && _wasInAir)
        {
            // Don't reset here - let OnPlayerLanded handle it properly
            // This prevents race conditions between systems
        }
    }
    
    /// <summary>
    /// Called when sprint is interrupted due to energy depletion
    /// </summary>
    private void OnSprintInterrupted()
    {
        // Force switch from sprint to walk animation when energy runs out
        if (_isCurrentlySprinting)
        {
            _isCurrentlySprinting = false;
            
            // If still moving, switch to walk animation
            if (_isCurrentlyMoving)
            {
                RequestStateTransition(_leftHandState, HandAnimationState.Walk, true);
                RequestStateTransition(_rightHandState, HandAnimationState.Walk, false);
                if (enableDebugLogs)
                    Debug.Log("[HandAnimationController] Sprint interrupted - switching to WALK animation");
            }
        }
    }
    
    // === HELPER METHODS ===
    
    /// <summary>
    /// Get current hand levels from PlayerProgression
    /// </summary>
    private int GetCurrentLeftHandLevel()
    {
        // Primary hand = Left hand (LMB)
        return playerProgression != null ? playerProgression.primaryHandLevel : 1;
    }
    
    private int GetCurrentRightHandLevel()
    {
        // Secondary hand = Right hand (RMB)
        return playerProgression != null ? playerProgression.secondaryHandLevel : 1;
    }
    
    // Get the current active left hand animator based on level
    private Animator GetCurrentLeftAnimator()
    {
        int index = Mathf.Clamp(GetCurrentLeftHandLevel() - 1, 0, 3);
        return leftHandAnimators[index];
    }
    
    // Get the current active right hand animator based on level  
    private Animator GetCurrentRightAnimator()
    {
        int index = Mathf.Clamp(GetCurrentRightHandLevel() - 1, 0, 3);
        return rightHandAnimators[index];
    }
    
    // Auto-find missing animators for all hand levels
    private void AutoFindMissingAnimators()
    {
        for (int i = 0; i < 4; i++)
        {
            if (leftHandAnimators[i] == null)
                leftHandAnimators[i] = FindAnimatorWithName($"left{i+1}", $"l{i+1}", $"primary{i+1}", "left", "l_", "primary");
            if (rightHandAnimators[i] == null)
                rightHandAnimators[i] = FindAnimatorWithName($"right{i+1}", $"r{i+1}", $"secondary{i+1}", "right", "r_", "secondary");
        }
    }
    
    // Apply animation speed to all hand animators
    private void ApplyAnimationSpeedToAllHands()
    {
        // Safety check for array bounds
        if (leftHandAnimators != null)
        {
            for (int i = 0; i < leftHandAnimators.Length && i < 4; i++)
            {
                if (leftHandAnimators[i] != null) leftHandAnimators[i].speed = animationSpeed;
            }
        }
        
        if (rightHandAnimators != null)
        {
            for (int i = 0; i < rightHandAnimators.Length && i < 4; i++)
            {
                if (rightHandAnimators[i] != null) rightHandAnimators[i].speed = animationSpeed;
            }
        }
    }
    
    /// Simple auto-find for hand animators
    private Animator FindAnimatorWithName(params string[] searchTerms)
    {
        Animator[] animators = FindObjectsOfType<Animator>();
        foreach (Animator anim in animators)
        {
            if (anim == null || !anim.gameObject.activeInHierarchy) continue;
            string name = anim.gameObject.name.ToLower();
            foreach (string term in searchTerms)
            {
                if (name.Contains(term.ToLower()))
                    return anim;
            }
        }
        return null;
    }
    
    void OnValidate()
    {
        // Keep speeds in sync when editing in Inspector - with safety check
        if (Application.isPlaying)
        {
            ApplyAnimationSpeedToAllHands();
        }
    }
    
    // Helper method to play animation safely with clip preference
    private void PlayAnimationClip(Animator anim, AnimationClip clip, string debugName = "", bool forceInstant = false)
    {
        if (anim == null || !anim.gameObject.activeInHierarchy)
        {
            return;
        }
        
        if (clip != null)
        {
            // INSTANT play for shotgun/combat actions - NO CROSSFADE
            if (forceInstant || crossFadeDuration <= 0f)
            {
                anim.Play(clip.name, 0, 0f);
            }
            else
            {
                // Use crossfading for smooth transitions (movement, etc.)
                anim.CrossFade(clip.name, crossFadeDuration, 0, 0f);
            }
            
            if (enableDebugLogs && !string.IsNullOrEmpty(debugName))
                Debug.Log($"[HandAnimationController] Playing clip {debugName} ({clip.name}) on {anim.name}");
        }
        else
        {
            if (enableDebugLogs)
                Debug.LogError($"[HandAnimationController] No clip assigned for {debugName} - animation will not play!");
        }
    }
    
    // CLEANED: Removed deprecated PlayAnimation() trigger method
    
    // === CORE STATE MACHINE LOGIC ===
    // AAA-level animation orchestration inspired by industry leaders
    
    /// <summary>
    /// Get the priority level for an animation state using Inspector configuration
    /// Higher priority can interrupt lower priority
    /// ALL PRIORITIES NOW CONFIGURABLE IN INSPECTOR!
    /// </summary>
    private int GetAnimationPriority(HandAnimationState state)
    {
        switch (state)
        {
            case HandAnimationState.Idle:
                return idlePriority;
            
            case HandAnimationState.FlyForward:
            case HandAnimationState.FlyUp:
            case HandAnimationState.FlyDown:
            case HandAnimationState.FlyStrafeLeft:
            case HandAnimationState.FlyStrafeRight:
            case HandAnimationState.FlyBoost:
                return flightPriority;
            
            case HandAnimationState.Dive:
            case HandAnimationState.Slide:
                return tacticalOverridePriority; // Active tactical actions
            
            case HandAnimationState.Walk:
                return walkPriority;
            
            case HandAnimationState.Jump:
            case HandAnimationState.Land:
            case HandAnimationState.TakeOff:
                return oneShotPriority;
            
            case HandAnimationState.Shotgun:
            case HandAnimationState.Beam:
                return briefCombatPriority;
            
            case HandAnimationState.Sprint:
                return sprintPriority;
            
            case HandAnimationState.ArmorPlate:
                return abilityPriority;
            
            case HandAnimationState.Emote:
                return emotePriority;
            
            default:
                return idlePriority;
        }
    }
    
    /// <summary>
    /// Get the blend time for transitioning TO a specific animation state using Inspector configuration
    /// ALL BLEND TIMES NOW CONFIGURABLE IN INSPECTOR!
    /// </summary>
    private float GetBlendTimeForState(HandAnimationState state, HandAnimationState previousState)
    {
        // Shotgun instant for snappy feel
        if (state == HandAnimationState.Shotgun) return instantBlendTime;
        
        // Beam instant for responsive combat
        if (state == HandAnimationState.Beam) return instantBlendTime;
        
        // Emotes blend slowly for dramatic effect
        if (state == HandAnimationState.Emote) return slowBlendTime;
        
        // Jump/Land/TakeOff very fast for responsiveness
        if (state == HandAnimationState.Jump || state == HandAnimationState.Land || state == HandAnimationState.TakeOff)
            return fastBlendTime;
        
        // Dive/Slide fast for tactical feel
        if (state == HandAnimationState.Dive || state == HandAnimationState.Slide)
            return fastBlendTime;
        
        // Combat to movement - fast transition
        if (IsCombatState(previousState) && IsMovementState(state))
            return fastBlendTime;
        
        // Movement to movement - smooth
        if (IsMovementState(previousState) && IsMovementState(state))
            return smoothBlendTime;
        
        // Beam/Shotgun to movement - smooth transition
        if ((state == HandAnimationState.Walk || state == HandAnimationState.Sprint) &&
            (previousState == HandAnimationState.Beam || previousState == HandAnimationState.Shotgun))
            return smoothBlendTime;
        
        // Movement to combat pose FAST (sprint → beam/shotgun)
        if ((state == HandAnimationState.Beam || state == HandAnimationState.Shotgun) &&
            (previousState == HandAnimationState.Walk || previousState == HandAnimationState.Sprint || previousState == HandAnimationState.Idle))
            return fastBlendTime;
        
        // Flight transitions smooth
        int newPriority = GetAnimationPriority(state);
        if (newPriority == flightPriority)
            return smoothBlendTime;
        
        // Default crossfade duration from Inspector
        return crossFadeDuration;
    }
    
    /// <summary>
    /// Check if a state is a combat state
    /// </summary>
    private bool IsCombatState(HandAnimationState state)
    {
        return state == HandAnimationState.Shotgun || 
               state == HandAnimationState.Beam ||
               state == HandAnimationState.ArmorPlate;
    }
    
    /// <summary>
    /// Check if a state is a movement state
    /// </summary>
    private bool IsMovementState(HandAnimationState state)
    {
        return state == HandAnimationState.Idle ||
               state == HandAnimationState.Walk ||
               state == HandAnimationState.Sprint ||
               state == HandAnimationState.FlyForward ||
               state == HandAnimationState.FlyUp ||
               state == HandAnimationState.FlyDown ||
               state == HandAnimationState.FlyStrafeLeft ||
               state == HandAnimationState.FlyStrafeRight ||
               state == HandAnimationState.FlyBoost;
    }
    
    /// <summary>
    /// Check if an animation state requires hard lock (cannot be interrupted by anything)
    /// </summary>
    private bool RequiresHardLock(HandAnimationState state)
    {
        bool requiresLock = state == HandAnimationState.Emote || state == HandAnimationState.ArmorPlate;
        if (enableDebugLogs && requiresLock)
            Debug.Log($"[HandAnimationController] {state} requires HARD LOCK");
        return requiresLock;
    }
    
    /// <summary>
    /// Check if an animation state requires soft lock (only higher priority can interrupt)
    /// Soft lock is ONLY for tactical actions (Dive/Slide)
    /// Shotgun/Beam are brief interrupts that auto-return to sprint!
    /// </summary>
    private bool RequiresSoftLock(HandAnimationState state)
    {
        return state == HandAnimationState.Dive || 
               state == HandAnimationState.Slide;
    }
    
    // DELETED: IsBriefCombatInterrupt() - No more auto-return bullshit
    
    /// <summary>
    /// Request a state transition with AAA-level priority checking
    /// This is the SINGLE SOURCE OF TRUTH for all animation transitions
    /// Uses numerical priority system inspired by God of War's committed action system
    /// </summary>
    private void RequestStateTransition(HandState handState, HandAnimationState newState, bool isLeftHand)
    {
        HandAnimationState currentState = handState.currentState;
        
        if (debugStateTransitions && enableDebugLogs)
            Debug.Log($"[HandAnimationController] {(isLeftHand ? "Left" : "Right")} RequestStateTransition: {currentState} → {newState} (Locked: {handState.isLocked}, SoftLocked: {handState.isSoftLocked})");
        
        // === HARD LOCK CHECK ===
        // Hard locked states cannot be interrupted by ANYTHING (except themselves)
        if (handState.isLocked)
        {
            // Allow re-triggering the same state (e.g., rapid shotgun fire)
            if (currentState == newState)
            {
                TransitionToState(handState, newState, isLeftHand);
                return;
            }
            
            if (enableDebugLogs)
                Debug.Log($"[HandAnimationController] {(isLeftHand ? "Left" : "Right")} HARD LOCKED in {currentState} - rejecting {newState}");
            return;
        }
        
        // Get priorities for both states (used throughout this method)
        int currentPriority = GetAnimationPriority(currentState);
        int newPriority = GetAnimationPriority(newState);
        
        // === REDUNDANT TRANSITION CHECK ===
        // Don't transition if already in this state (EXCEPT for combat actions that need to retrigger)
        if (currentState == newState && newState != HandAnimationState.Shotgun && newState != HandAnimationState.Beam)
        {
            return;
        }
        
        // === SOFT LOCK CHECK ===
        // Soft locked states can only be interrupted by higher priority
        // EXCEPTION: Same state can re-trigger (rapid shotgun fire)
        if (handState.isSoftLocked)
        {
            // Allow same state re-triggering (Shotgun rapid fire)
            if (currentState == newState)
            {
                TransitionToState(handState, newState, isLeftHand);
                return;
            }
            
            // Block lower priority
            if (newPriority < currentPriority)
            {
                if (enableDebugLogs)
                    Debug.Log($"[HandAnimationController] {(isLeftHand ? "Left" : "Right")} SOFT LOCKED in {currentState} (P{currentPriority}) - rejecting lower priority {newState} (P{newPriority})");
                return;
            }
        }
        
        // === PRIORITY-BASED INTERRUPTION SYSTEM ===
        
        // Higher priority ALWAYS interrupts lower priority
        if (newPriority > currentPriority)
        {
            TransitionToState(handState, newState, isLeftHand);
            return;
        }
        
        // DELETED: Brief combat interrupt special case - no more special cases
        
        // Equal priority - allow transitions within same tier
        // This enables smooth movement blending (flight directions, etc.)
        if (newPriority == currentPriority)
        {
            // Within flight tier - always allow (smooth movement)
            if (newPriority == AnimationPriority.FLIGHT)
            {
                TransitionToState(handState, newState, isLeftHand);
                return;
            }
            
            // Same priority - allow transitions within same tier
            TransitionToState(handState, newState, isLeftHand);
            return;
        }
        
        // Movement states can ALWAYS interrupt lower priority (even downwards)
        // This allows: Sprint → Walk, Walk → Idle, Sprint → Idle
        // BUT: Active combat (shotgun/beam) can interrupt sprint!
        if (newPriority >= AnimationPriority.WALK || currentPriority >= AnimationPriority.WALK)
        {
            // Allow movement-to-movement transitions in any direction
            if (newState == HandAnimationState.Walk || newState == HandAnimationState.Sprint || newState == HandAnimationState.Idle)
            {
                if (currentState == HandAnimationState.Walk || currentState == HandAnimationState.Sprint || currentState == HandAnimationState.Idle)
                {
                    TransitionToState(handState, newState, isLeftHand);
                    return;
                }
            }
        }
        
        // Lower priority cannot interrupt higher priority (combat protection)
        // This prevents movement from interrupting combat actions
        if (enableDebugLogs && newPriority < currentPriority)
        {
            Debug.Log($"[HandAnimationController] {(isLeftHand ? "Left" : "Right")}: {newState} (P{newPriority}) cannot interrupt {currentState} (P{currentPriority})");
        }
    }
    
    /// <summary>
    /// Actually perform the state transition and play the animation
    /// Uses AAA-level blend time system and lock mechanisms
    /// </summary>
    private void TransitionToState(HandState handState, HandAnimationState newState, bool isLeftHand)
    {
        HandAnimationState previousState = handState.currentState;
        int newPriority = GetAnimationPriority(newState);
        
        if (enableDebugLogs)
            Debug.Log($"[HandAnimationController] {(isLeftHand ? "LEFT" : "RIGHT")}: {previousState} → {newState} (P{newPriority})");
        
        // Store beam interruption state BEFORE changing state
        if (previousState == HandAnimationState.Beam && newState != HandAnimationState.Beam)
        {
            handState.beamWasActiveBeforeInterruption = true;
        }
        
        // Update state
        handState.previousState = previousState;
        handState.currentState = newState;
        handState.stateStartTime = Time.time;
        
        // Apply appropriate lock mechanism based on new state
        bool wasLocked = handState.isLocked;
        bool wasSoftLocked = handState.isSoftLocked;
        
        handState.isLocked = RequiresHardLock(newState);
        handState.isSoftLocked = RequiresSoftLock(newState);
        
        if (enableDebugLogs && (wasLocked != handState.isLocked || wasSoftLocked != handState.isSoftLocked))
        {
            Debug.Log($"[HandAnimationController] {(isLeftHand ? "Left" : "Right")} lock state changed: HardLock {wasLocked}→{handState.isLocked}, SoftLock {wasSoftLocked}→{handState.isSoftLocked} for state {newState}");
        }
        
        // Cancel any existing completion coroutine
        if (handState.animationCompletionCoroutine != null)
        {
            StopCoroutine(handState.animationCompletionCoroutine);
            handState.animationCompletionCoroutine = null;
        }
        
        // Play the appropriate animation with smart blend time
        Animator animator = isLeftHand ? GetCurrentLeftAnimator() : GetCurrentRightAnimator();
        AnimationClip clip = GetClipForState(newState, isLeftHand);
        
        if (clip != null)
        {
            // Get context-aware blend time (combat is instant, movement is smooth)
            float blendTime = GetBlendTimeForState(newState, previousState);
            bool instantPlay = (blendTime <= 0.001f); // Effectively instant
            
            // Override crossFade duration temporarily for this transition
            float originalCrossFade = crossFadeDuration;
            crossFadeDuration = blendTime;
            
            PlayAnimationClip(animator, clip, $"{(isLeftHand ? "L" : "R")} {newState}", instantPlay);
            
            // Restore original crossFade duration
            crossFadeDuration = originalCrossFade;
            
            // DISABLED: No more one-shot scheduling, brief combat interrupts, or locking bullshit
            // Animations play and that's it - no timers, no locks, no complexity
            handState.lockDuration = 0f;
        }
    }
    
    /// <summary>
    /// Force transition to idle, bypassing all priority checks
    /// Used for error recovery and resets
    /// </summary>
    private void ForceTransitionToIdle(HandState handState, bool isLeftHand)
    {
        handState.currentState = HandAnimationState.Idle;
        handState.beamWasActiveBeforeInterruption = false;
        handState.isEmotePlaying = false;
        handState.isLocked = false;
        handState.stateStartTime = Time.time;
        
        if (handState.animationCompletionCoroutine != null)
        {
            StopCoroutine(handState.animationCompletionCoroutine);
            handState.animationCompletionCoroutine = null;
        }
        
        Animator animator = isLeftHand ? GetCurrentLeftAnimator() : GetCurrentRightAnimator();
        AnimationClip clip = isLeftHand ? leftIdleClip : rightIdleClip;
        PlayAnimationClip(animator, clip, $"{(isLeftHand ? "L" : "R")} Idle (FORCED)");
        
        if (enableDebugLogs)
            Debug.Log($"[HandAnimationController] {(isLeftHand ? "Left" : "Right")} FORCED to Idle");
    }
    
    /// <summary>
    /// Get the animation clip for a given state
    /// </summary>
    private AnimationClip GetClipForState(HandAnimationState state, bool isLeftHand)
    {
        switch (state)
        {
            case HandAnimationState.Idle:
                return isLeftHand ? leftIdleClip : rightIdleClip;
            case HandAnimationState.Walk:
                return isLeftHand ? leftWalkClip : rightWalkClip;
            case HandAnimationState.Sprint:
                return isLeftHand ? leftSprintClip : rightSprintClip;
            case HandAnimationState.Jump:
                return isLeftHand ? leftJumpClip : rightJumpClip;
            case HandAnimationState.Land:
                if (enableDebugLogs)
                {
                    AnimationClip clip = isLeftHand ? leftLandClip : rightLandClip;
                    Debug.Log($"[HandAnimationController] GetClipForState(Land) returning: {(isLeftHand ? "Left" : "Right")} - {(clip != null ? clip.name : "NULL - CLIP NOT ASSIGNED!")}");
                }
                return isLeftHand ? leftLandClip : rightLandClip;
            case HandAnimationState.Slide:
                if (enableDebugLogs)
                {
                    AnimationClip clip = isLeftHand ? leftSlideClip : rightSlideClip;
                    Debug.Log($"[HandAnimationController] GetClipForState(Slide) returning: {(isLeftHand ? "Left" : "Right")} - {(clip != null ? clip.name : "NULL - CLIP NOT ASSIGNED!")}");
                }
                return isLeftHand ? leftSlideClip : rightSlideClip;
            case HandAnimationState.Dive:
                return isLeftHand ? leftDiveClip : rightDiveClip;
            case HandAnimationState.Shotgun:
                return isLeftHand ? leftShotgunClip : rightShotgunClip;
            case HandAnimationState.Beam:
                return isLeftHand ? leftBeamLoopClip : rightBeamLoopClip;
            case HandAnimationState.FlyForward:
                return isLeftHand ? leftFlyForwardClip : rightFlyForwardClip;
            case HandAnimationState.FlyUp:
                return isLeftHand ? leftFlyUpClip : rightFlyUpClip;
            case HandAnimationState.FlyDown:
                return isLeftHand ? leftFlyDownClip : rightFlyDownClip;
            case HandAnimationState.FlyStrafeLeft:
                return isLeftHand ? leftFlyStrafeLeftClip : rightFlyStrafeLeftClip;
            case HandAnimationState.FlyStrafeRight:
                return isLeftHand ? leftFlyStrafeRightClip : rightFlyStrafeRightClip;
            case HandAnimationState.FlyBoost:
                return isLeftHand ? leftFlyBoostClip : rightFlyBoostClip;
            case HandAnimationState.TakeOff:
                return isLeftHand ? leftTakeOffClip : rightTakeOffClip;
            case HandAnimationState.ArmorPlate:
                if (enableDebugLogs)
                    Debug.Log($"[HandAnimationController] GetClipForState(ArmorPlate) returning: {(isLeftHand ? "null (left hand)" : (rightApplyPlateClip != null ? rightApplyPlateClip.name : "NULL - CLIP NOT ASSIGNED!"))}");
                return isLeftHand ? null : rightApplyPlateClip; // Only right hand has armor plate
            case HandAnimationState.Emote:
                return GetEmoteClip(_currentEmoteNumber, isLeftHand);
            default:
                return null;
        }
    }
    
    // DELETED: IsOneShotAnimation() - No more one-shot complexity, all animations just play
    
    // DELETED: OneShotAnimationComplete() - No more bullshit timers and waiting
    // Animations play instantly and that's it - no locks, no waiting, no complexity
    
    // DELETED: BriefCombatComplete() - This was causing thrashing and automatic state changes
    // Hands now stay in whatever state they're put in - no more automatic returns to sprint
    
    
    
    // CLEANED: Removed 60+ redundant wrapper methods
    // External systems should call RequestStateTransition() directly
    // Example: RequestStateTransition(_leftHandState, HandAnimationState.Idle, true)
    
    // === COMBAT ANIMATIONS (STATE MACHINE WRAPPERS) =  =  =
    public void PlayShootShotgun(bool isPrimaryHand)
    {
        if (isPrimaryHand)
        {
            RequestStateTransition(_rightHandState, HandAnimationState.Shotgun, false);
            if (enableDebugLogs)
                Debug.Log($"[HandAnimationController] Right shotgun fired");
        }
        else
        {
            RequestStateTransition(_leftHandState, HandAnimationState.Shotgun, true);
            if (enableDebugLogs)
                Debug.Log($"[HandAnimationController] Left shotgun fired");
        }
    }
    
    // === BEAM ANIMATIONS (STATE MACHINE WRAPPERS) ===
    public void StartBeamLeft()
    {
        if (enableDebugLogs)
            Debug.Log($"[HandAnimationController] StartBeamLeft");
        
        RequestStateTransition(_leftHandState, HandAnimationState.Beam, true);
    }
    
    public void StartBeamRight()
    {
        if (enableDebugLogs)
            Debug.Log($"[HandAnimationController] StartBeamRight");
        
        RequestStateTransition(_rightHandState, HandAnimationState.Beam, false);
    }
    
    public void StopBeamLeft()
    {
        if (enableDebugLogs)
            Debug.Log($"[HandAnimationController] StopBeamLeft - IMMEDIATE UNLOCK");
        
        // CRITICAL: Stop any running completion coroutines (brief combat timers)
        if (_leftHandState.animationCompletionCoroutine != null)
        {
            StopCoroutine(_leftHandState.animationCompletionCoroutine);
            _leftHandState.animationCompletionCoroutine = null;
            if (enableDebugLogs)
                Debug.Log("[HandAnimationController] Left beam completion timer CANCELLED");
        }
        
        // IMMEDIATE UNLOCK: Clear all locks and reset state
        _leftHandState.beamWasActiveBeforeInterruption = false;
        _leftHandState.currentState = HandAnimationState.Idle;
        _leftHandState.isLocked = false;
        _leftHandState.isSoftLocked = false;
        _leftHandState.lockDuration = 0f;
        _leftHandState.stateStartTime = Time.time;
        
        if (enableDebugLogs)
            Debug.Log("[HandAnimationController] Left beam IMMEDIATELY UNLOCKED - all animations available");
    }
    
    public void StopBeamRight()
    {
        if (enableDebugLogs)
            Debug.Log($"[HandAnimationController] StopBeamRight - IMMEDIATE UNLOCK");
        
        // CRITICAL: Stop any running completion coroutines (brief combat timers)
        if (_rightHandState.animationCompletionCoroutine != null)
        {
            StopCoroutine(_rightHandState.animationCompletionCoroutine);
            _rightHandState.animationCompletionCoroutine = null;
            if (enableDebugLogs)
                Debug.Log("[HandAnimationController] Right beam completion timer CANCELLED");
        }
        
        // IMMEDIATE UNLOCK: Clear all locks and reset state
        _rightHandState.beamWasActiveBeforeInterruption = false;
        _rightHandState.currentState = HandAnimationState.Idle;
        _rightHandState.isLocked = false;
        _rightHandState.isSoftLocked = false;
        _rightHandState.lockDuration = 0f;
        _rightHandState.stateStartTime = Time.time;
        
        if (enableDebugLogs)
            Debug.Log("[HandAnimationController] Right beam IMMEDIATELY UNLOCKED - all animations available");
    }
    
    public void StopAllBeams()
    {
        StopBeamLeft();
        StopBeamRight();
    }
    
    // === ESSENTIAL MOVEMENT METHODS (KEEP FOR EXTERNAL SYSTEMS) ===
    public void PlayLandBoth()
    {
        RequestStateTransition(_leftHandState, HandAnimationState.Land, true);
        RequestStateTransition(_rightHandState, HandAnimationState.Land, false);
    }
    
    public void PlaySlideBoth()
    {
        RequestStateTransition(_leftHandState, HandAnimationState.Slide, true);
        RequestStateTransition(_rightHandState, HandAnimationState.Slide, false);
    }
    
    public void PlayDiveBoth()
    {
        RequestStateTransition(_leftHandState, HandAnimationState.Dive, true);
        RequestStateTransition(_rightHandState, HandAnimationState.Dive, false);
    }
    
    public void PlayJumpBoth()
    {
        RequestStateTransition(_leftHandState, HandAnimationState.Jump, true);
        RequestStateTransition(_rightHandState, HandAnimationState.Jump, false);
    }
    
    public void PlayTakeOffBoth()
    {
        RequestStateTransition(_leftHandState, HandAnimationState.TakeOff, true);
        RequestStateTransition(_rightHandState, HandAnimationState.TakeOff, false);
    }
    
    public void PlayIdleBoth()
    {
        RequestStateTransition(_leftHandState, HandAnimationState.Idle, true);
        RequestStateTransition(_rightHandState, HandAnimationState.Idle, false);
    }
    
    // CLEANED: Removed 30+ flight wrapper methods - use RequestStateTransition() directly
    
    // === EMOTE ANIMATIONS (REFACTORED - NO MORE DUPLICATION) ===
    
    /// <summary>
    /// Play emote by configuration index (used by new configurable system)
    /// </summary>
    public void PlayEmoteByIndex(int configIndex)
    {
        // Validate config index
        if (configIndex < 0 || configIndex >= emoteConfigs.Length)
        {
            if (enableDebugLogs) Debug.LogWarning($"[HandAnimationController] Invalid emote config index: {configIndex}");
            return;
        }
        
        // Don't allow emotes to interrupt each other
        if (_leftHandState.isEmotePlaying || _rightHandState.isEmotePlaying) return;
        
        EmoteConfig config = emoteConfigs[configIndex];
        
        // Check if clips are assigned based on hand selection
        bool hasValidClips = false;
        switch (config.handSelection)
        {
            case HandSelection.LeftOnly:
                hasValidClips = config.leftClip != null;
                break;
            case HandSelection.RightOnly:
                hasValidClips = config.rightClip != null;
                break;
            case HandSelection.Both:
                hasValidClips = config.leftClip != null || config.rightClip != null;
                break;
        }
        
        if (!hasValidClips)
        {
            if (enableDebugLogs) Debug.LogWarning($"[HandAnimationController] {config.emoteName} has no valid clips assigned for {config.handSelection} selection");
            return;
        }

        // Store current emote config for GetEmoteClip to use
        _currentEmoteIndex = configIndex;

        // Store beam states before interrupting
        _leftHandState.beamWasActiveBeforeInterruption = (_leftHandState.currentState == HandAnimationState.Beam);
        _rightHandState.beamWasActiveBeforeInterruption = (_rightHandState.currentState == HandAnimationState.Beam);

        // Mark emotes as playing based on hand selection
        switch (config.handSelection)
        {
            case HandSelection.LeftOnly:
                _leftHandState.isEmotePlaying = true;
                break;
            case HandSelection.RightOnly:
                _rightHandState.isEmotePlaying = true;
                break;
            case HandSelection.Both:
                _leftHandState.isEmotePlaying = true;
                _rightHandState.isEmotePlaying = true;
                break;
        }

        // Cancel any pending completion coroutines for selected hands
        if (config.handSelection == HandSelection.LeftOnly || config.handSelection == HandSelection.Both)
        {
            if (_leftHandState.animationCompletionCoroutine != null)
            {
                StopCoroutine(_leftHandState.animationCompletionCoroutine);
                _leftHandState.animationCompletionCoroutine = null;
            }
        }
        if (config.handSelection == HandSelection.RightOnly || config.handSelection == HandSelection.Both)
        {
            if (_rightHandState.animationCompletionCoroutine != null)
            {
                StopCoroutine(_rightHandState.animationCompletionCoroutine);
                _rightHandState.animationCompletionCoroutine = null;
            }
        }

        // Transition selected hands to emote state
        switch (config.handSelection)
        {
            case HandSelection.LeftOnly:
                RequestStateTransition(_leftHandState, HandAnimationState.Emote, true);
                break;
            case HandSelection.RightOnly:
                RequestStateTransition(_rightHandState, HandAnimationState.Emote, false);
                break;
            case HandSelection.Both:
                RequestStateTransition(_leftHandState, HandAnimationState.Emote, true);
                RequestStateTransition(_rightHandState, HandAnimationState.Emote, false);
                break;
        }

        if (enableDebugLogs)
            Debug.Log($"[HandAnimationController] {config.emoteName} playing on {config.handSelection} - one-shot system will handle completion");

        // Notify companion to mirror this emote (use config index + 1 for backward compatibility)
        if (OnPlayerEmote != null)
            OnPlayerEmote(configIndex + 1);
    }
    
    /// <summary>
    /// Legacy emote method for backward compatibility (1-4 numbering)
    /// </summary>
    public void PlayEmote(int emoteNumber)
    {
        // Convert 1-based emote number to 0-based config index
        int configIndex = emoteNumber - 1;
        PlayEmoteByIndex(configIndex);
    }
    
    // Helper to get emote clip using new configuration system
    private AnimationClip GetEmoteClip(int emoteNumber, bool isLeftHand)
    {
        // Use new configuration system if available
        if (_currentEmoteIndex >= 0 && _currentEmoteIndex < emoteConfigs.Length)
        {
            EmoteConfig config = emoteConfigs[_currentEmoteIndex];
            AnimationClip clip = isLeftHand ? config.leftClip : config.rightClip;
            
            if (debugEmoteInput && enableDebugLogs)
            {
                string clipName = clip != null ? clip.name : "NULL";
                Debug.Log($"[HandAnimationController] GetEmoteClip({config.emoteName}, {(isLeftHand ? "Left" : "Right")}) returning: {clipName}");
            }
            
            return clip;
        }
        
        // Fallback to legacy system for backward compatibility
        AnimationClip legacyClip = null;
        switch (emoteNumber)
        {
            case 1: legacyClip = isLeftHand ? leftEmote1Clip : rightEmote1Clip; break;
            case 2: legacyClip = isLeftHand ? leftEmote2Clip : rightEmote2Clip; break;
            case 3: legacyClip = isLeftHand ? leftEmote3Clip : rightEmote3Clip; break;
            case 4: legacyClip = isLeftHand ? leftEmote4Clip : rightEmote4Clip; break;
            default: legacyClip = null; break;
        }
        
        if (debugEmoteInput && enableDebugLogs)
        {
            string clipName = legacyClip != null ? legacyClip.name : "NULL";
            Debug.Log($"[HandAnimationController] GetEmoteClip(Legacy {emoteNumber}, {(isLeftHand ? "Left" : "Right")}) returning: {clipName}");
        }
        
        return legacyClip;
    }
    
    // Backward compatibility wrappers
    public void PlayEmote1() => PlayEmote(1);
    public void PlayEmote2() => PlayEmote(2);
    public void PlayEmote3() => PlayEmote(3);
    public void PlayEmote4() => PlayEmote(4);
    
    // DEBUG METHOD: Force unlock hands if they get stuck
    [System.Obsolete("DEBUG ONLY - Remove in production")]
    public void DEBUG_ForceUnlockHands()
    {
        Debug.Log("[HandAnimationController] DEBUG: Force unlocking both hands");
        
        // Stop any running coroutines
        if (_leftHandState.animationCompletionCoroutine != null)
        {
            StopCoroutine(_leftHandState.animationCompletionCoroutine);
            _leftHandState.animationCompletionCoroutine = null;
        }
        if (_rightHandState.animationCompletionCoroutine != null)
        {
            StopCoroutine(_rightHandState.animationCompletionCoroutine);
            _rightHandState.animationCompletionCoroutine = null;
        }
        
        // Force unlock everything
        _leftHandState.isLocked = false;
        _leftHandState.isSoftLocked = false;
        _leftHandState.isEmotePlaying = false;
        _leftHandState.lockDuration = 0f;
        
        _rightHandState.isLocked = false;
        _rightHandState.isSoftLocked = false;
        _rightHandState.isEmotePlaying = false;
        _rightHandState.lockDuration = 0f;
        
        // Force to idle
        _leftHandState.currentState = HandAnimationState.Idle;
        _rightHandState.currentState = HandAnimationState.Idle;
        
        Debug.Log("[HandAnimationController] DEBUG: Both hands force unlocked and set to IDLE");
    }
    
    // === INTEGRATED HOOKS FOR GAME SYSTEMS ===
    
    // Called by AAAMovementController when player jumps
    public void OnPlayerJumped()
    {
        if (enableDebugLogs)
            Debug.Log("[HandAnimationController] OnPlayerJumped called by AAAMovementController");
        
        // Track jump start time for smart landing system
        _jumpStartTime = Time.time;
        _wasInAir = true;
        
        PlayJumpBoth();
    }
    
    // Called by movement systems  
    public void PlayLandAnimation()
    {
        PlayLandBoth();
    }
    
    // Called by slide systems
    public void OnSlideStarted()
    {
        if (enableDebugLogs)
            Debug.Log("[HandAnimationController] OnSlideStarted called by CleanAAACrouch");
        PlaySlideBoth();
    }
    
    // Called by tactical dive system
    public void PlayDiveAnimation()
    {
        // Enable dive override to prevent automatic animation updates
        _isDiveOverrideActive = true;
        PlayDiveBoth();
        
        if (enableDebugLogs)
            Debug.Log("[HandAnimationController] Dive animation started - auto-updates paused");
    }
    
    public void StopDiveAnimation()
    {
        // Disable dive override to resume automatic animation updates
        _isDiveOverrideActive = false;
        
        if (enableDebugLogs)
            Debug.Log("[HandAnimationController] Dive animation stopped - auto-updates resumed");
    }
    
    public void OnSlideStopped()
    {
        if (enableDebugLogs)
            Debug.Log("[HandAnimationController] OnSlideStopped called by CleanAAACrouch - FORCE unlocking slide");
        
        // CRITICAL: Force unlock AND change state - slide should end when CleanAAACrouch says so
        if (_leftHandState.currentState == HandAnimationState.Slide)
        {
            _leftHandState.isSoftLocked = false;
            _leftHandState.isLocked = false;
            _leftHandState.currentState = HandAnimationState.Idle; // FORCE state change
            _leftHandState.stateStartTime = Time.time;
            if (enableDebugLogs)
                Debug.Log("[HandAnimationController] Left hand FORCE unlocked and changed to Idle");
        }
        
        if (_rightHandState.currentState == HandAnimationState.Slide)
        {
            _rightHandState.isSoftLocked = false;
            _rightHandState.isLocked = false;
            _rightHandState.currentState = HandAnimationState.Idle; // FORCE state change
            _rightHandState.stateStartTime = Time.time;
            if (enableDebugLogs)
                Debug.Log("[HandAnimationController] Right hand FORCE unlocked and changed to Idle");
        }
        
        // Now play idle animations directly (bypass RequestStateTransition)
        Animator leftAnim = GetCurrentLeftAnimator();
        Animator rightAnim = GetCurrentRightAnimator();
        
        if (leftAnim != null && leftIdleClip != null)
        {
            leftAnim.Play(leftIdleClip.name, 0, 0f);
            if (enableDebugLogs)
                Debug.Log("[HandAnimationController] Left animator FORCED to play idle");
        }
        
        if (rightAnim != null && rightIdleClip != null)
        {
            rightAnim.Play(rightIdleClip.name, 0, 0f);
            if (enableDebugLogs)
                Debug.Log("[HandAnimationController] Right animator FORCED to play idle");
        }
    }
    
    // Called by flight systems
    public void OnTakeOffStarted()
    {
        PlayTakeOffBoth();
        
        if (enableDebugLogs)
            Debug.Log("[HandAnimationController] Take off animation started - transitioning to flight mode");
    }
    
    // === BACKWARD-COMPATIBILITY WRAPPERS ===
    
    // Called by AAAMovementController when player lands
    public void OnPlayerLanded()
    {
        if (enableDebugLogs)
            Debug.Log("[HandAnimationController] OnPlayerLanded called by AAAMovementController");
        
        // Smart landing system: only play land animation for significant jumps or after dives
        bool shouldPlayLandAnimation = false;
        
        // SIMPLE: ANY jump should trigger land animation when touching ground
        if (_wasInAir)
        {
            PlayLandBoth();
            if (enableDebugLogs)
                Debug.Log($"[HandAnimationController] Landing detected - playing land animation");
        }
        
        // Reset air tracking
        _wasInAir = false;
        _jumpStartTime = -999f;
    }
    
    // Beam events
    public void OnBeamStarted(bool isPrimaryHand)
    {
        Debug.Log($"[HandAnimationController] 🔥 OnBeamStarted called for {(isPrimaryHand ? "LEFT" : "RIGHT")} hand");
        if (isPrimaryHand) StartBeamLeft(); else StartBeamRight();
    }
    
    public void OnBeamStopped(bool isPrimaryHand)
    {
        Debug.Log($"[HandAnimationController] 🛑 OnBeamStopped called for {(isPrimaryHand ? "LEFT" : "RIGHT")} hand");
        if (isPrimaryHand) StopBeamLeft(); else StopBeamRight();
    }
    
    // STUB: OnHandModelsChanged() - Does NOTHING to prevent compilation errors
    // External systems can call this but it won't do any nuclear resets or bullshit
    public void OnHandModelsChanged()
    {
        if (enableDebugLogs) Debug.Log("[HandAnimationController] OnHandModelsChanged called - IGNORING (no more nuclear resets)");
        // INTENTIONALLY EMPTY - no more automatic resets
    }
    
    // Capture flow (mapped to neutral idles in simplified controller)
    public void OnCaptureStarted()
    {
        if (enableDebugLogs) Debug.Log("[HandAnimationController] OnCaptureStarted");
    }
    
    public void OnCaptureCompleted()
    {
        if (enableDebugLogs) Debug.Log("[HandAnimationController] OnCaptureCompleted");
        PlayIdleBoth();
    }
    
    public void OnCaptureInterrupted()
    {
        if (enableDebugLogs) Debug.Log("[HandAnimationController] OnCaptureInterrupted");
        PlayIdleBoth();
    }
    
    // Interact prompt
    public void OnInteractPromptShown()
    {
        if (enableDebugLogs) Debug.Log("[HandAnimationController] OnInteractPromptShown");
    }
    
    public void OnInteractPromptHidden()
    {
        if (enableDebugLogs) Debug.Log("[HandAnimationController] OnInteractPromptHidden");
    }
    
    // Optional compatibility helper
    public void OnShotgunFired(bool isPrimaryHand)
    {
        PlayShootShotgun(isPrimaryHand);
    }
    
    // === ARMOR PLATE ANIMATION ===
    
    /// <summary>
    /// Play armor plate application animation on right hand (one-shot)
    /// Called by ArmorPlateSystem when a plate is applied
    /// CRITICAL: This animation MUST NOT be interrupted by any other animation
    /// </summary>
    public void PlayApplyPlateAnimation()
    {
        if (enableDebugLogs)
            Debug.Log($"[HandAnimationController] PlayApplyPlateAnimation called - rightApplyPlateClip: {(rightApplyPlateClip != null ? rightApplyPlateClip.name : "NULL!")}");
        
        // CRITICAL FIX: If no clip is assigned, don't start the animation at all
        if (rightApplyPlateClip == null)
        {
            if (enableDebugLogs)
                Debug.LogError("[HandAnimationController] rightApplyPlateClip is NULL! Skipping armor plate animation to prevent infinite lock!");
            return; // Exit early to prevent the stuck state
        }
        
        if (enableDebugLogs)
            Debug.Log($"[HandAnimationController] Right hand current state: {_rightHandState.currentState}, requesting ArmorPlate transition");
        
        // FIXED: Don't lock BEFORE transition - let the transition handle locking
        // ArmorPlate is now a one-shot animation, so completion is handled automatically
        RequestStateTransition(_rightHandState, HandAnimationState.ArmorPlate, false);
    }

    /// <summary>
    /// Emergency method to force unlock armor plate animation if it gets stuck
    /// Called by ArmorPlateSystem if animation doesn't complete properly
    /// </summary>
    public void ForceUnlockArmorPlateAnimation()
    {
        if (_rightHandState.currentState == HandAnimationState.ArmorPlate)
        {
            if (enableDebugLogs)
                Debug.Log("[HandAnimationController] EMERGENCY: Force unlocking stuck armor plate animation");
            
            // Stop any running completion coroutine
            if (_rightHandState.animationCompletionCoroutine != null)
            {
                StopCoroutine(_rightHandState.animationCompletionCoroutine);
                _rightHandState.animationCompletionCoroutine = null;
            }
            
            // Force unlock and return to idle
            ForceTransitionToIdle(_rightHandState, false);
        }
    }

    /// <summary>
    /// Check if right hand is currently in ArmorPlate animation state
    /// Used by ArmorPlateSystem to detect stuck animations
    /// </summary>
    public bool IsRightHandInArmorPlateState()
    {
        return _rightHandState.currentState == HandAnimationState.ArmorPlate;
    }

    /// <summary>
    /// NUCLEAR RESET - Completely clears all animation states and locks
    /// Used by death system to ensure clean state
    /// </summary>
    public void NuclearResetAllAnimations()
    {
        if (enableDebugLogs) Debug.Log("[HandAnimationController] NUCLEAR RESET - Clearing all animations and locks");

        // Stop all coroutines
        StopAllCoroutines();

        // Clear all locks and states for both hands
        ClearHandState(_leftHandState);
        ClearHandState(_rightHandState);

        // Force both hands to IDLE immediately
        _leftHandState.currentState = HandAnimationState.Idle;
        _rightHandState.currentState = HandAnimationState.Idle;


        // Force play IDLE animations
        Animator leftAnim = GetCurrentLeftAnimator();
        Animator rightAnim = GetCurrentRightAnimator();
        
        if (leftAnim != null && leftIdleClip != null)
        {
            PlayAnimationClip(leftAnim, leftIdleClip, "NUCLEAR RESET", true);
        }
        if (rightAnim != null && rightIdleClip != null)
        {
            PlayAnimationClip(rightAnim, rightIdleClip, "NUCLEAR RESET", true);
        }

        if (enableDebugLogs) Debug.Log("[HandAnimationController] NUCLEAR RESET COMPLETE");
    }

    /// <summary>
    /// Clear all state and locks for a hand
    /// </summary>
    private void ClearHandState(HandState handState)
    {
        handState.isLocked = false;
        handState.isSoftLocked = false;
        handState.lockDuration = 0f;
        handState.isEmotePlaying = false;

        // Stop any running coroutines
        if (handState.animationCompletionCoroutine != null)
        {
            StopCoroutine(handState.animationCompletionCoroutine);
            handState.animationCompletionCoroutine = null;
        }
    }
    
    /// <summary>
    /// Initialize emote keys from the centralized Controls system
    /// Called during Awake to sync emote configs with Controls
    /// </summary>
    private void InitializeEmoteKeysFromControls()
    {
        // Update emote configs with keys from Controls
        if (emoteConfigs.Length >= 1) emoteConfigs[0].triggerKey = Controls.Emote1;
        if (emoteConfigs.Length >= 2) emoteConfigs[1].triggerKey = Controls.Emote2;
        if (emoteConfigs.Length >= 3) emoteConfigs[2].triggerKey = Controls.Emote3;
        if (emoteConfigs.Length >= 4) emoteConfigs[3].triggerKey = Controls.Emote4;
    }
}