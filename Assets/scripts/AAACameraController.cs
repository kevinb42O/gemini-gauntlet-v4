using UnityEngine;

/// <summary>
/// AAA-Quality FPS Camera Controller with smooth movement, dynamic FOV, and immersive effects
/// Features: Camera bob, smooth look, FOV changes, recoil system, and more!
/// 
/// 🤝 BFFL INTEGRATION: Aerial Trick System ↔ Wall Jump System
/// These systems are now best friends forever! Key behaviors:
/// - Aerial trick → wall jump: Instantly cancels reconciliation for seamless combo flow
/// - Wall jump → aerial trick: Clears reconciliation states for clean trick restart
/// - Both systems respect each other's camera control and transition smoothly
/// </summary>
public class AAACameraController : MonoBehaviour
{
    [Header("=== LOOK SETTINGS ===")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float verticalLookLimit = 90f;
    [SerializeField] private bool invertY = false;
    [SerializeField] private AnimationCurve sensitivityCurve = AnimationCurve.Linear(0, 1, 1, 1); // Flat curve for consistent feel
    
    [Header("=== SMOOTHING ===")]
    [Tooltip("Frame-based smoothing (0 = raw input, 0.1-0.3 = AAA feel, 0.5+ = sluggish)")]
    [SerializeField] [Range(0f, 0.5f)] private float lookSmoothing = 0.15f; // AAA-quality frame interpolation
    [SerializeField] private float positionSmoothing = 12f; // Increased for smoother position changes
    [SerializeField] private bool enableSmoothing = true;
    [SerializeField] private bool enableMotionPrediction = true; // NEW: Predict fast movements
    
    [Header("=== HEAD BOB (Call of Duty Style - Realistic) ===")]
    [SerializeField] private bool enableHeadBob = true;
    [Tooltip("Vertical bob intensity (scaled for 320-unit character)")]
    [SerializeField] private float headBobVerticalIntensity = 12f; // Scaled up 6.4x for large character
    [Tooltip("Horizontal sway intensity (scaled for 320-unit character)")]
    [SerializeField] private float headBobHorizontalIntensity = 6f; // Scaled up 6.4x for large character
    [Tooltip("Forward lean intensity (scaled for 320-unit character)")]
    [SerializeField] private float headBobForwardIntensity = 4f; // Scaled up 6.4x for large character
    [Tooltip("Walk frequency (steps per second)")]
    [SerializeField] private float walkBobFrequency = 1.4f; // Slower for large character mass
    [Tooltip("Sprint frequency (steps per second)")]
    [SerializeField] private float sprintBobFrequency = 2.0f; // Slower sprint for large character
    [Tooltip("Velocity influence on bob intensity (0-1)")]
    [SerializeField] [Range(0f, 1f)] private float velocityInfluence = 0.4f; // Lower for smoother feel
    [Tooltip("Smoothness of bob transitions")]
    [SerializeField] private float headBobSmoothness = 5f; // Very smooth for large character
    [Tooltip("Footstep impact sharpness (higher = more pronounced steps)")]
    [SerializeField] private float footstepSharpness = 1.3f; // Softer for heavy character feel
    [Tooltip("Enable subtle head tilt on steps (realistic weight distribution)")]
    [SerializeField] private bool enableStepTilt = true;
    [Tooltip("Maximum tilt angle per step (degrees)")]
    [SerializeField] private float maxStepTiltAngle = 1.2f; // Subtle tilt for large character
    
    [Header("=== DYNAMIC FOV ===")]
    [SerializeField] private float baseFOV = 100f; // Walk/Normal FOV
    [SerializeField] private float sprintFOVIncrease = 10f; // Sprint FOV = 110 (ONLY sprint changes FOV!)
    [SerializeField] private float fovTransitionSpeed = 8f;
    
    [Header("=== CAMERA SHAKE SYSTEM ===")]
    [SerializeField] private bool enableCameraShake = true;
    [SerializeField] private float beamShakeIntensity = 0.08f;
    [SerializeField] private float beamShakeSpeed = 15f;
    [SerializeField] private float shotgunShakeIntensity = 0.15f;
    [SerializeField] private float shotgunShakeDuration = 0.12f;
    [SerializeField] private float dualHandMultiplier = 1.6f;
    [SerializeField] private float shakeDecay = 8f;
    [SerializeField] private AnimationCurve shakeIntensityCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    
    [Header("=== TRAUMA SHAKE SYSTEM (AAA Impact Effects) ===")]
    [SerializeField] private bool enableTraumaShake = true;
    [SerializeField] private float maxTrauma = 1.0f;
    [SerializeField] private float traumaDecayRate = 1.5f; // How fast trauma fades
    [SerializeField] private float traumaShakeIntensity = 2.5f; // Shake multiplier
    [SerializeField] private float traumaShakeSpeed = 25f; // Shake frequency
    [SerializeField] private AnimationCurve traumaIntensityCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Trauma to shake scaling
    
    [Header("=== STRAFE TILT SYSTEM ===")]
    [SerializeField] private bool enableStrafeTilt = true;
    [SerializeField] private float maxTiltAngle = 5f; // Maximum tilt angle in degrees
    [SerializeField] private float tiltSpeed = 18f; // Increased for snappier response
    [SerializeField] private float tiltReturnSpeed = 12f; // Increased for snappier return
    [SerializeField] private float tiltSpringDamping = 0.7f; // NEW: Spring damping for natural feel
    [SerializeField] private AnimationCurve tiltResponseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Allows fine-tuning response
    
    [Header("=== WALL JUMP CAMERA TILT (AAA QUALITY) ===")]
    [Tooltip("Enable dynamic camera tilt during wall jumps")]
    [SerializeField] private bool enableWallJumpTilt = true;
    [Tooltip("Maximum tilt angle during wall jump (degrees) - AAA standard: 8-12°")]
    [SerializeField] private float wallJumpMaxTiltAngle = 10f;
    [Tooltip("How quickly camera tilts into wall jump (higher = snappier)")]
    [SerializeField] private float wallJumpTiltSpeed = 25f;
    [Tooltip("How quickly camera returns to normal after wall jump")]
    [SerializeField] private float wallJumpTiltReturnSpeed = 8f;
    [Tooltip("Tilt curve over time - allows fine control of tilt feel")]
    [SerializeField] private AnimationCurve wallJumpTiltCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    [Tooltip("Duration of wall jump tilt effect (seconds)")]
    [SerializeField] private float wallJumpTiltDuration = 0.4f;
    [Tooltip("Add subtle forward pitch during wall jump for extra dynamism")]
    [SerializeField] private bool enableWallJumpPitch = true;
    [Tooltip("Maximum forward pitch angle during wall jump (degrees)")]
    [SerializeField] private float wallJumpMaxPitchAngle = 3f;
    
    [Header("=== DYNAMIC WALL-RELATIVE TILT ===")]
    [Tooltip("Enable dynamic wall-relative camera tilt (tilts AWAY from walls during chains)")]
    [SerializeField] private bool enableDynamicWallTilt = true;
    [Tooltip("Maximum tilt angle away from wall (degrees)")]
    [SerializeField] private float dynamicTiltMaxAngle = 12f;
    [Tooltip("Speed of tilt application")]
    [SerializeField] private float dynamicTiltSpeed = 20f;
    [Tooltip("Speed of return to neutral")]
    [SerializeField] private float dynamicTiltReturnSpeed = 15f;
    [Tooltip("Screen center deadzone (0-0.5, ignore walls near center)")]
    [SerializeField] private float screenCenterDeadzone = 0.2f;
    [Tooltip("Show debug logs for dynamic tilt")]
    [SerializeField] private bool showDynamicTiltDebug = false;
    
    [Header("=== 🎪 AERIAL FREESTYLE TRICK SYSTEM (REVOLUTIONARY) ===")]
    [Tooltip("Enable the mind-bending aerial camera trick system")]
    [SerializeField] private bool enableAerialFreestyle = true;
    [Tooltip("Middle mouse click acts as trick jump (auto-engages freestyle)")]
    [SerializeField] private bool middleClickTrickJump = true;
    [Tooltip("Maximum rotation speed during tricks (degrees/second)")]
    [SerializeField] private float maxTrickRotationSpeed = 360f;
    [Tooltip("How responsive the camera is to mouse input during tricks")]
    [SerializeField] private float trickInputSensitivity = 3.5f;
    [Tooltip("Smoothing for trick rotations (0 = instant, 1 = very smooth)")]
    [SerializeField] [Range(0f, 0.95f)] private float trickRotationSmoothing = 0.25f;
    [Tooltip("Initial flip burst multiplier (Skate-style flick-it feel)")]
    [SerializeField] private float initialFlipBurstMultiplier = 2.5f;
    [Tooltip("How long the initial burst lasts (seconds)")]
    [SerializeField] private float initialBurstDuration = 0.15f;
    [Tooltip("Enable analog speed control (move mouse faster = rotate faster)")]
    [SerializeField] private bool enableAnalogSpeedControl = true;
    [Tooltip("Speed control responsiveness (how quickly rotation speed changes)")]
    [SerializeField] private float speedControlResponsiveness = 8f;
    [Tooltip("Minimum mouse movement to maintain rotation (prevents drift)")]
    [SerializeField] private float minInputThreshold = 0.01f;
    [Tooltip("Enable automatic roll on diagonal movement (varial flips)")]
    [SerializeField] private bool enableDiagonalRoll = true;
    [Tooltip("Roll strength multiplier (0 = no roll, 1 = full roll)")]
    [SerializeField] [Range(0f, 1f)] private float rollStrength = 0.35f;
    
    [Header("🎪 MOMENTUM PHYSICS SYSTEM (SKATE GAME FEEL)")]
    [Tooltip("Enable momentum-based rotation (flick and let it spin like Tony Hawk/Skate)")]
    [SerializeField] private bool enableMomentumPhysics = true;
    [Tooltip("How quickly input builds velocity (higher = more responsive to flicks)")]
    [SerializeField] private float angularAcceleration = 12f;
    [Tooltip("How quickly velocity decays when no input (higher = stops faster)")]
    [SerializeField] private float angularDrag = 4f;
    [Tooltip("Maximum angular velocity (degrees/second)")]
    [SerializeField] private float maxAngularVelocity = 720f;
    [Tooltip("Initial flick multiplier (skate-style burst on first input)")]
    [SerializeField] private float flickBurstMultiplier = 2.8f;
    [Tooltip("How long flick burst lasts (seconds)")]
    [SerializeField] private float flickBurstDuration = 0.12f;
    [Tooltip("Input smoothing for flicks (lower = snappier, 0.02-0.05 recommended)")]
    [SerializeField] [Range(0f, 0.1f)] private float flickInputSmoothing = 0.03f;
    [Tooltip("Counter-rotation strength when fighting momentum (0-1)")]
    [SerializeField] [Range(0f, 1f)] private float counterRotationStrength = 0.85f;
    
    [Tooltip("FOV boost during tricks for extra intensity")]
    [SerializeField] private float trickFOVBoost = 15f;
    [Tooltip("Speed of FOV transition during tricks")]
    [SerializeField] private float trickFOVSpeed = 12f;
    [Tooltip("Landing reconciliation duration (industry standard: 0.5-0.8 seconds)")]
    [SerializeField] private float landingReconciliationDuration = 0.6f;
    [Tooltip("Reconciliation easing curve for cinematic feel")]
    [SerializeField] private AnimationCurve reconciliationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Tooltip("Grace period after landing before reconciliation starts (seconds)")]
    [SerializeField] private float landingGracePeriod = 0.12f;
    [Tooltip("Mouse input deadzone to prevent sensor drift during reconciliation")]
    [SerializeField] private float mouseInputDeadzone = 0.01f;
    [Tooltip("Allow player to cancel reconciliation with mouse input (player-first)")]
    [SerializeField] private bool allowPlayerCancelReconciliation = true;
    
    [Header("🎮 KEYBOARD ROLL CONTROLS")]
    [Tooltip("Enable Q/E keyboard roll controls during tricks")]
    [SerializeField] private bool enableKeyboardRoll = true;
    [Tooltip("Maximum roll speed when fully accelerated (degrees/second)")]
    [SerializeField] private float keyboardRollMaxSpeed = 360f;
    [Tooltip("Time to reach max speed when holding button (seconds)")]
    [SerializeField] private float keyboardRollAccelTime = 1.2f;
    [Tooltip("Momentum fade-out rate when released (0-1, higher = faster fade)")]
    [Range(0f, 1f)]
    [SerializeField] private float keyboardRollDecayRate = 0.05f;
    [Tooltip("Key for rolling left")]
    [SerializeField] private KeyCode rollLeftKey = KeyCode.Q;
    [Tooltip("Key for rolling right")]
    [SerializeField] private KeyCode rollRightKey = KeyCode.E;
    
    [Tooltip("Minimum air time before tricks are allowed (seconds)")]
    [SerializeField] private float minAirTimeForTricks = 0.15f;
    [Tooltip("Trauma intensity when landing mid-flip (0-1)")]
    [SerializeField] private float failedLandingTrauma = 0.6f;
    [Tooltip("Enable motion blur effect during tricks (visual feedback)")]
    [SerializeField] private bool enableTrickMotionBlur = true;
    [Tooltip("Show trick UI indicators and rotation display")]
    [SerializeField] private bool showTrickUI = true;
    [Tooltip("Rotation threshold for 'clean landing' (degrees from upright)")]
    [SerializeField] private float cleanLandingThreshold = 25f;
    
    [Header("🎬 TIME DILATION (CINEMATIC SLOW-MO)")]
    [Tooltip("Enable time dilation during tricks (slow-motion effect)")]
    [SerializeField] private bool enableTimeDilation = true;
    [Tooltip("Time scale during tricks (0.5 = half speed, 1.0 = normal)")]
    [SerializeField] [Range(0.1f, 1f)] private float trickTimeScale = 0.5f;
    [Tooltip("How long to ramp INTO slow-mo (seconds, unscaled)")]
    [SerializeField] private float timeDilationRampIn = 0.4f;
    [Tooltip("How long to ramp OUT of slow-mo (seconds, unscaled)")]
    [SerializeField] private float timeDilationRampOut = 0.15f;
    [Tooltip("Distance from ground to start ramping out (units)")]
    [SerializeField] private float landingAnticipationDistance = 3f;
    
    [Header("🛡️ EMERGENCY RECOVERY SYSTEM (PHASE 1)")]
    [Tooltip("Enable emergency recovery and safety systems")]
    [SerializeField] private bool enableEmergencyRecovery = true;
    [Tooltip("Key to force upright camera (emergency reset)")]
    [SerializeField] private KeyCode emergencyUprightKey = KeyCode.R;
    [Tooltip("Maximum time in any state before auto-recovery (seconds)")]
    [SerializeField] private float maxStateTimeout = 10f;
    [Tooltip("Auto-fix quaternion drift every frame")]
    [SerializeField] private bool autoFixQuaternionDrift = true;
    [Tooltip("Show emergency recovery debug logs")]
    [SerializeField] private bool showEmergencyDebug = false;
    
    [Header("=== LANDING IMPACT (Smooth Spring Compression) ===")]
    [SerializeField] private bool enableLandingImpact = true;
    [Tooltip("How much the camera compresses down on landing (knee bend simulation).")]
    [SerializeField] private float landingCompressionAmount = 80f; // Scaled for 320-unit player (~25% compression)
    [Tooltip("Spring stiffness - how quickly it bounces back (LOWER = slower recovery).")]
    [SerializeField] private float landingSpringStiffness = 100f; // SLOW & SMOOTH: Realistic heavy feel
    [Tooltip("Spring damping - controls overshoot/oscillation (1.0+ = no bounce, <1.0 = bouncy).")]
    [SerializeField] private float landingSpringDamping = 1.5f; // ZERO BOUNCE GUARANTEED: Over-damped for smooth slow recovery
    [Tooltip("Fall distance threshold for triggering impact (units).")]
    [SerializeField] private float minFallDistanceForImpact = 320f; // 1x player height (scaled for 320-unit player)
    [Tooltip("Fall distance for maximum impact scaling (units).")]
    [SerializeField] private float maxFallDistanceForImpact = 1600f; // 5x player height (big fall)
    [Tooltip("Curve for scaling compression by fall distance.")]
    [SerializeField] private AnimationCurve fallDistanceScaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Tooltip("Add subtle forward tilt on landing for extra realism.")]
    [SerializeField] private bool enableLandingTilt = true;
    [Tooltip("Maximum forward tilt angle on landing.")]
    [SerializeField] private float maxLandingTiltAngle = 3f;
    
    [Header("=== IDLE SWAY ===")]
    [SerializeField] private bool enableIdleSway = true; // NEW: Subtle breathing motion
    [SerializeField] private float idleSwayAmount = 0.02f; // NEW: Subtle sway intensity
    [SerializeField] private float idleSwaySpeed = 1.5f; // NEW: Breathing speed
    
    [Header("=== DECOUPLED HANDS SYSTEM ===")]
    [Tooltip("Enable independent hand motion system (hands as siblings of camera, not children)")]
    [SerializeField] private bool enableDecoupledHands = false; // DISABLED - Causing jitter issues!
    [Tooltip("Primary hand transform (left/right hand)")]
    [SerializeField] private Transform primaryHandTransform;
    [Tooltip("Secondary hand transform (left/right hand)")]
    [SerializeField] private Transform secondaryHandTransform;
    [Tooltip("Enable procedural hand sway independent of camera")]
    [SerializeField] private bool enableProceduralHandSway = true;
    [Tooltip("Intensity of procedural hand sway")]
    [SerializeField] private float handSwayIntensity = 0.5f;
    [Tooltip("Speed of procedural hand sway")]
    [SerializeField] private float handSwaySpeed = 2f;
    
    // Private variables
    private Camera playerCamera;
    private AAAMovementController movementController;
    private Transform playerTransform;
    // Preserve-yaw state to avoid snapping on enable
    private Quaternion baseYawRotation;
    private float yawStart;
    private Vector3 referenceUp;
    
    // Look rotation
    private Vector2 currentLook;
    private Vector2 targetLook;
    private Vector2 lookInput;
    private Vector2 rawLookInput; // Store raw input for frame-based smoothing
    
    // FOV management
    private float currentFOV;
    private float targetFOV;
    private float storedFOVBeforeDisable; // Store FOV when disabled to restore it
    
    // Camera shake system
    private Vector3 shakeOffset = Vector3.zero;
    private bool isPrimaryBeamActive = false;
    private bool isSecondaryBeamActive = false;
    private float primaryShotgunShakeTimer = 0f;
    private float secondaryShotgunShakeTimer = 0f;
    private Vector3 currentBeamShake = Vector3.zero;
    private Vector3 currentShotgunShake = Vector3.zero;
    private float beamShakeTime = 0f;
    private Vector3 basePosition = Vector3.zero;
    
    // Trauma shake system (AAA impact effects)
    private float currentTrauma = 0f;
    private Vector3 traumaShakeOffset = Vector3.zero;
    private float traumaShakeTime = 0f;
    
    // Strafe tilt system
    private float currentTilt = 0f;
    private float targetTilt = 0f;
    private float strafeInput = 0f;
    private float tiltVelocity = 0f;
    
    // Wall jump tilt system
    private float wallJumpTiltAmount = 0f; // Current wall jump tilt
    private float wallJumpTiltTarget = 0f; // Target wall jump tilt
    private float wallJumpTiltVelocity = 0f; // Smooth damp velocity
    private float wallJumpTiltStartTime = -999f; // When wall jump tilt started
    private float wallJumpPitchAmount = 0f; // Current forward pitch
    private float wallJumpPitchTarget = 0f; // Target forward pitch
    private float wallJumpPitchVelocity = 0f; // Smooth damp velocity
    
    // Dynamic wall-relative tilt system
    private float dynamicWallTilt = 0f;
    private float dynamicWallTiltTarget = 0f;
    private float dynamicWallTiltVelocity = 0f;
    
    // 🎪 AERIAL FREESTYLE TRICK SYSTEM - STATE MACHINE (PHASE 1)
    /// <summary>
    /// Trick system states - guarantees valid transitions only
    /// </summary>
    public enum TrickSystemState
    {
        Grounded,           // On ground, ready to jump
        JumpInitiated,      // Jump triggered, waiting for airborne confirmation
        Airborne,           // In air, tricks not yet active
        FreestyleActive,    // Performing tricks (camera independent)
        LandingApproach,    // Approaching ground (time dilation ramp out)
        Reconciling,        // Post-landing camera snap to reality
        TransitionCleanup   // Final cleanup before returning to Grounded
    }
    
    private TrickSystemState _trickState = TrickSystemState.Grounded;
    private TrickSystemState _previousTrickState = TrickSystemState.Grounded;
    private float _stateEnterTime = 0f;
    
    // Legacy boolean flags (kept for backward compatibility, but state machine is source of truth)
    private bool isFreestyleModeActive = false;
    private bool wasAirborneLastFrame = false;
    private bool wasReconciling = false; // Track reconciliation state for smooth handoff
    private float airborneStartTime = 0f;
    private Quaternion freestyleRotation = Quaternion.identity; // Camera's independent rotation during tricks
    
    // 🎮 PRO JUMP MECHANICS FOR TRICKS (hold = higher jump, tap = small jump)
    private bool isHoldingScrollWheelButton = false;
    private float scrollWheelButtonPressTime = 0f;
    private bool trickJumpTriggered = false; // Track if we triggered a trick jump this frame
    
    // 🎬 TIME DILATION STATE (Now managed by TimeDilationManager)
    private TimeDilationManager timeDilationManager;
    private bool wasTimeDilationRequested = false;
    private Quaternion freestyleRotationVelocity = Quaternion.identity; // For smooth damping
    private Vector2 freestyleLookInput = Vector2.zero; // Accumulated rotation input
    private Vector2 freestyleLookVelocity = Vector2.zero; // For smoothing
    private float freestyleFOV = 0f; // Current trick FOV
    private float totalRotationX = 0f; // Track total pitch rotation (backflips/frontflips)
    private float totalRotationY = 0f; // Track total yaw rotation (spins)
    private float totalRotationZ = 0f; // Track total roll rotation (barrel rolls)
    private float keyboardRollVelocity = 0f; // Smooth keyboard roll velocity (momentum-based)
    private bool isReconciling = false; // Are we snapping back to reality after landing?
    private float reconciliationStartTime = 0f;
    private float reconciliationProgress = 0f; // 0 to 1 for time-normalized blend
    private Quaternion reconciliationStartRotation = Quaternion.identity;
    private Quaternion reconciliationTargetRotation = Quaternion.identity;
    private float landingTime = 0f; // When we landed (for grace period)
    private bool isInLandingGrace = false; // Are we in grace period?
    private float lastRotationSpeed = 0f; // For motion blur intensity
    
    // 🔥 SKATE-STYLE ENHANCEMENTS
    private float freestyleModeStartTime = 0f; // When freestyle mode was activated
    private float currentRotationSpeedMultiplier = 1f; // Analog speed control
    private Vector2 lastRawInput = Vector2.zero; // Track input magnitude for analog control
    private bool isInInitialBurst = false; // Are we in the flick-it burst phase?
    
    // 🎪 MOMENTUM PHYSICS SYSTEM (THE GEM - SKATE GAME FEEL)
    private Vector2 angularVelocity = Vector2.zero; // Persistent rotation velocity (pitch, yaw)
    private float rollVelocity = 0f; // Separate roll velocity for varial flips
    private bool isFlickBurstActive = false; // Flick burst for initial impact
    private float flickBurstStartTime = 0f; // When the flick burst started
    private Vector2 lastFlickDirection = Vector2.zero; // Track flick direction for burst
    private Vector2 smoothedInput = Vector2.zero; // Smoothed input for momentum system
    private Vector2 inputVelocity = Vector2.zero; // Velocity for input smoothing
    
    // 🛡️ EMERGENCY RECOVERY SYSTEM (PHASE 1)
    private int _emergencyResetCount = 0;
    private float _lastEmergencyResetTime = -999f;
    private const float EMERGENCY_RESET_COOLDOWN = 5f;
    private float _lastQuaternionNormalizeTime = -999f;
    private const float QUATERNION_NORMALIZE_INTERVAL = 1f; // Normalize every second
    
    // Landing impact system - Spring-based compression
    private bool wasGrounded = true;
    private float fallStartHeight = 0f;
    private bool isTrackingFall = false;
    
    // Spring physics for smooth landing
    private float landingCompressionOffset = 0f; // Current compression amount
    private float landingCompressionVelocity = 0f; // Spring velocity
    private float landingTiltOffset = 0f; // Forward tilt on landing
    private float landingTiltVelocity = 0f; // Tilt spring velocity
    
    // Idle sway system
    private float idleSwayTime = 0f;
    private Vector3 idleSwayOffset = Vector3.zero;
    
    // Head bob system - COD style
    private float headBobTimer = 0f;
    private Vector3 headBobOffset = Vector3.zero;
    private float currentBobFrequency = 1.8f;
    private float currentBobIntensity = 0f;
    private float lastStepPhase = 0f;
    private float stepTiltAngle = 0f;
    private float stepTiltVelocity = 0f;
    private PlayerEnergySystem energySystem;
    
    // Motion prediction
    private Vector3 lastPlayerPosition = Vector3.zero;
    private Vector3 predictedVelocity = Vector3.zero;
    
    // Performance optimization - reusable vectors
    private Vector3 reusableShakeVector = Vector3.zero;
    private Vector3 reusableSwayVector = Vector3.zero;
    
    // Decoupled hands system
    private Quaternion basePrimaryHandRotation = Quaternion.identity; // Store YOUR rotation
    private Quaternion baseSecondaryHandRotation = Quaternion.identity; // Store YOUR rotation
    private Vector3 basePrimaryHandPosition = Vector3.zero; // Store YOUR position
    private Vector3 baseSecondaryHandPosition = Vector3.zero; // Store YOUR position
    private Vector3 currentPrimaryHandOffset = Vector3.zero; // Only offset, not full position
    private Vector3 currentSecondaryHandOffset = Vector3.zero; // Only offset, not full position
    private Quaternion primaryRotationOffset = Quaternion.identity; // Only rotation offset
    private Quaternion secondaryRotationOffset = Quaternion.identity; // Only rotation offset
    private float handSwayTime = 0f;
    private Vector3 proceduralHandOffset = Vector3.zero;
    private Vector3 lastCameraRotationEuler = Vector3.zero;
    private float rotationDelta = 0f;
    
    // 🛡️ AAA PAUSE DETECTION SYSTEM (Prevents camera floating during pause)
    private bool _isPaused = false;
    private const float PAUSE_DETECTION_THRESHOLD = 0.01f; // Time.timeScale below this = paused
    
    void Start()
    {
        playerCamera = GetComponent<Camera>();
        movementController = GetComponentInParent<AAAMovementController>();
        playerTransform = transform.parent;
        
        // Initialize values
        currentFOV = baseFOV;
        targetFOV = baseFOV;
        playerCamera.fieldOfView = currentFOV;
        
        // Get energy system reference for sprint detection
        if (movementController != null)
        {
            energySystem = movementController.GetComponent<PlayerEnergySystem>();
        }
        
        // Subscribe to sprint interruption event
        PlayerEnergySystem.OnSprintInterrupted += OnSprintInterrupted;
        
        // Initialize camera shake
        // basePosition no longer needed - we add shake as offset to current position
        
        // Initialize motion prediction
        if (playerTransform != null)
        {
            lastPlayerPosition = playerTransform.position;
        }
        
        // Initialize landing impact
        if (movementController != null)
        {
            wasGrounded = movementController.IsGrounded;
        }
        
        // Initialize decoupled hands system
        if (enableDecoupledHands)
        {
            InitializeDecoupledHands();
        }
        
        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // 🎯 UNIFIED IMPACT SYSTEM: Subscribe to impact events
        // This makes the camera listen to FallingDamageSystem instead of tracking falls independently
        ImpactEventBroadcaster.OnImpact += OnImpactReceived;
        Debug.Log("[AAACameraController] ✅ Subscribed to unified impact events");
    }
    
    void OnEnable()
    {
        // Ensure references
        if (playerCamera == null) playerCamera = GetComponent<Camera>();
        if (playerTransform == null) playerTransform = transform.parent;
        
        // 🔥 CRITICAL FIX: Restore FOV when re-enabled (prevents FOV reset when chest opens/closes)
        if (playerCamera != null && storedFOVBeforeDisable > 0)
        {
            // Restore the FOV that was active when we were disabled
            currentFOV = storedFOVBeforeDisable;
            targetFOV = storedFOVBeforeDisable;
            playerCamera.fieldOfView = storedFOVBeforeDisable;
            Debug.Log($"[AAACameraController] OnEnable: Restored FOV to {storedFOVBeforeDisable}");
        }
        
        // Capture base yaw state relative to current up to prevent world-Y snapping
        if (playerTransform != null)
        {
            referenceUp = playerTransform.up;
            baseYawRotation = playerTransform.rotation;
            // Capture current absolute yaw value at enable so our yaw is applied as a delta from this baseline
            // This keeps the initial orientation unchanged on the first Update
            yawStart = currentLook.x;
        }
    }
    
    void OnDisable()
    {
        // 🔥 CRITICAL FIX: Store current FOV when disabled so we can restore it
        storedFOVBeforeDisable = currentFOV;
        Debug.Log($"[AAACameraController] OnDisable: Stored FOV {storedFOVBeforeDisable} for restoration");
        
        // 🛡️ PHASE 1: Safety cleanup - ensure Time.timeScale is reset
        if (enableEmergencyRecovery && timeDilationManager != null)
        {
            Debug.LogWarning("[EMERGENCY] OnDisable: Forcing normal time");
            timeDilationManager.ForceNormalTime();
        }
    }
    
    /// <summary>
    /// PHASE 1: Safety cleanup on application quit
    /// </summary>
    void OnApplicationQuit()
    {
        // Ensure Time.timeScale is reset
        if (Time.timeScale != 1f)
        {
            Time.timeScale = 1f;
            Debug.Log("[TRICK SYSTEM] Application quit - Time.timeScale reset to 1.0");
        }
    }
    
    void Update()
    {
        // 🛡️ AAA PAUSE DETECTION: Early exit if game is paused (prevents camera floating)
        _isPaused = Time.timeScale < PAUSE_DETECTION_THRESHOLD;
        if (_isPaused)
        {
            // Game is paused - freeze all camera updates to prevent floating
            return;
        }
        
        // 🛡️ PHASE 1: Emergency recovery FIRST (safety checks before everything)
        UpdateEmergencyRecovery();
        
        // 🔥 SMART FOV: Only update when transitioning, not every frame
        UpdateFOVTransition();
        UpdateMotionPrediction();
        UpdateHeadBob();
        
        // 🎬 TIME DILATION: Update slow-mo effect
        UpdateTimeDilation();
        
        // 🎪 AERIAL FREESTYLE TRICK SYSTEM
        UpdateAerialFreestyleSystem();
    }
    
    void LateUpdate()
    {
        // 🛡️ AAA PAUSE DETECTION: Early exit if game is paused (prevents camera floating)
        // This is CRITICAL - without this check, camera effects continue running during pause
        if (_isPaused)
        {
            // Game is paused - freeze all camera updates including look input
            return;
        }
        
        // CRITICAL: Mouse look in LateUpdate for frame-perfect timing (AAA standard)
        // BUT: Skip normal look input if freestyle mode is active OR reconciling
        if (!isFreestyleModeActive && !isReconciling)
        {
            HandleLookInput();
        }
        else
        {
            // 🎪 FREESTYLE MODE OR RECONCILIATION: Handle trick rotation
            if (isFreestyleModeActive)
            {
                HandleFreestyleLookInput(); // Special input handling during tricks
            }
            
            // DON'T apply rotation here - let ApplyCameraTransform() handle it (prevents double application)
        }
        
        // Camera effects in LateUpdate for smoothness after all movement
        UpdateStrafeTilt();
        UpdateWallJumpTilt(); // NEW: AAA wall jump camera tilt
        UpdateDynamicWallTilt(); // NEW: Dynamic wall-relative tilt system
        UpdateCameraShake();
        UpdateTraumaShake(); // AAA trauma-based shake
        UpdateLandingImpact();
        UpdateIdleSway();
        ApplyCameraTransform(); // This applies BOTH freestyle AND normal rotation
        
        // Update decoupled hands after camera transform - NO SMOOTHING, direct follow!
        if (enableDecoupledHands)
        {
            UpdateDecoupledHands();
        }
    }
    
    private void HandleLookInput()
    {
        // 🎪 CRITICAL FIX: Don't accumulate yaw during freestyle mode!
        // Freestyle mode uses its own rotation system (freestyleRotation)
        // Accumulating yaw here causes massive drift when landing (-215° bug)
        if (isFreestyleModeActive || isReconciling || isInLandingGrace)
        {
            // Sync currentLook with freestyle rotation to prevent drift
            // Extract yaw from freestyleRotation to keep them aligned
            Vector3 freestyleEuler = freestyleRotation.eulerAngles;
            targetLook.x = freestyleEuler.y;
            currentLook.x = freestyleEuler.y;
            
            // Still track pitch changes for when we exit freestyle
            // (but don't apply them during freestyle)
            rawLookInput.y = Input.GetAxis("Mouse Y");
            float pitchInput = rawLookInput.y * mouseSensitivity;
            if (invertY) pitchInput = -pitchInput;
            
            targetLook.y -= pitchInput;
            targetLook.y = Mathf.Clamp(targetLook.y, -verticalLookLimit, verticalLookLimit);
            currentLook.y = targetLook.y;
            
            return; // Don't process normal look input during freestyle
        }
        
        // Get raw mouse input (Unity Input Manager now at 1.0 sensitivity)
        rawLookInput.x = Input.GetAxis("Mouse X");
        rawLookInput.y = Input.GetAxis("Mouse Y");
        
        // Apply base sensitivity
        lookInput = rawLookInput * mouseSensitivity;
        
        // Apply sensitivity curve (now enhances instead of crushing)
        float curveMultiplier = sensitivityCurve.Evaluate(Mathf.Clamp01(rawLookInput.magnitude));
        lookInput *= curveMultiplier;
        
        // Invert Y if needed
        if (invertY)
            lookInput.y = -lookInput.y;
        
        // Calculate target look rotation
        targetLook.x += lookInput.x;
        targetLook.y -= lookInput.y;
        targetLook.y = Mathf.Clamp(targetLook.y, -verticalLookLimit, verticalLookLimit);
        
        // AAA-quality frame-based smoothing (like COD/Apex)
        // This is MUCH more responsive than SmoothDamp
        if (enableSmoothing && lookSmoothing > 0f)
        {
            // Frame-rate independent smoothing
            // Higher lookSmoothing = MORE smoothing (0.15 = light, 0.3 = heavy)
            float smoothFactor = Mathf.Exp(-lookSmoothing * 60f * Time.deltaTime);
            currentLook = Vector2.Lerp(targetLook, currentLook, smoothFactor);
        }
        else
        {
            // Raw input - instant response (like CS:GO)
            currentLook = targetLook;
        }
        
        // Apply rotation to player and camera without snapping to world axes.
        // Yaw: rotate around the current up vector by delta from the captured baseline.
        if (playerTransform != null)
        {
            if (referenceUp == Vector3.zero) referenceUp = playerTransform.up;
            float yawDelta = currentLook.x - yawStart;
            Quaternion yawRotation = Quaternion.AngleAxis(yawDelta, referenceUp);
            playerTransform.rotation = yawRotation * baseYawRotation;
        }
    }
    
    // UpdateCameraBob method REMOVED
    
    private void UpdateMotionPrediction()
    {
        if (!enableMotionPrediction || playerTransform == null)
        {
            predictedVelocity = Vector3.zero;
            return;
        }
        
        // Calculate player velocity for motion prediction
        Vector3 currentPosition = playerTransform.position;
        Vector3 deltaPosition = currentPosition - lastPlayerPosition;
        predictedVelocity = Vector3.Lerp(predictedVelocity, deltaPosition / Time.deltaTime, 10f * Time.deltaTime);
        lastPlayerPosition = currentPosition;
    }
    
    private void UpdateLandingImpact()
    {
        if (!enableLandingImpact || movementController == null)
        {
            landingCompressionOffset = 0f;
            landingCompressionVelocity = 0f;
            landingTiltOffset = 0f;
            landingTiltVelocity = 0f;
            return;
        }
        
        // Detect landing
        bool isGrounded = movementController.IsGrounded;
        
        // Track fall height
        if (!isGrounded && movementController.Velocity.y < 0 && !isTrackingFall)
        {
            fallStartHeight = transform.position.y;
            isTrackingFall = true;
        }
        
        // Landing detected - Apply instant compression!
        if (isGrounded && !wasGrounded && isTrackingFall)
        {
            // Calculate fall distance
            float fallDistance = fallStartHeight - transform.position.y;
            
            // Reset dynamic wall tilt on landing
            dynamicWallTiltTarget = 0f;
            dynamicWallTilt = 0f;
            
            // Only trigger impact if fall was significant
            if (fallDistance >= minFallDistanceForImpact)
            {
                // Calculate impact strength based on fall distance
                float normalizedFallDistance = Mathf.Clamp01((fallDistance - minFallDistanceForImpact) / (maxFallDistanceForImpact - minFallDistanceForImpact));
                float impactScale = fallDistanceScaleCurve.Evaluate(normalizedFallDistance);
                
                // Apply instant compression (knee bend) - this is the "impact"
                float compressionAmount = -landingCompressionAmount * (0.4f + impactScale * 0.6f); // 40-100% compression
                landingCompressionOffset = compressionAmount;
                
                // Set initial downward velocity for spring (makes it feel more impactful)
                landingCompressionVelocity = compressionAmount * 2f;
                
                // Apply forward tilt for extra realism
                if (enableLandingTilt)
                {
                    landingTiltOffset = maxLandingTiltAngle * (0.3f + impactScale * 0.7f);
                    landingTiltVelocity = landingTiltOffset * 1.5f;
                }
                
                Debug.Log($"[LANDING SPRING] Fall: {fallDistance:F1} units, Compression: {compressionAmount:F2}");
            }
            
            isTrackingFall = false;
        }
        
        // Reset fall tracking when grounded
        if (isGrounded)
        {
            isTrackingFall = false;
        }
        
        // SMOOTH SPRING PHYSICS - ONE BOUNCE ONLY (not a clown!)
        // Spring equation: F = -k * x - c * v
        // k = stiffness, x = displacement, c = damping, v = velocity
        
        // Calculate spring force for compression
        float springForce = -landingSpringStiffness * landingCompressionOffset;
        float dampingForce = -landingSpringDamping * landingCompressionVelocity;
        float totalForce = springForce + dampingForce;
        
        // Update velocity and position
        landingCompressionVelocity += totalForce * Time.deltaTime;
        landingCompressionOffset += landingCompressionVelocity * Time.deltaTime;
        
        // CRITICAL: Stop spring when very close to rest (INCREASED thresholds to prevent infinite bouncing)
        // Human knees don't bounce infinitely - they compress once and return smoothly
        if (Mathf.Abs(landingCompressionOffset) < 0.01f && Mathf.Abs(landingCompressionVelocity) < 0.1f)
        {
            landingCompressionOffset = 0f;
            landingCompressionVelocity = 0f;
        }
        
        // Same spring physics for tilt
        if (enableLandingTilt)
        {
            float tiltSpringForce = -landingSpringStiffness * landingTiltOffset;
            float tiltDampingForce = -landingSpringDamping * landingTiltVelocity;
            float tiltTotalForce = tiltSpringForce + tiltDampingForce;
            
            landingTiltVelocity += tiltTotalForce * Time.deltaTime;
            landingTiltOffset += landingTiltVelocity * Time.deltaTime;
            
            // Stop tilt spring when at rest (INCREASED thresholds)
            if (Mathf.Abs(landingTiltOffset) < 0.05f && Mathf.Abs(landingTiltVelocity) < 0.5f)
            {
                landingTiltOffset = 0f;
                landingTiltVelocity = 0f;
            }
        }
        
        wasGrounded = isGrounded;
    }
    
    /// <summary>
    /// 🎯 UNIFIED IMPACT SYSTEM - Handle impact events from FallingDamageSystem
    /// 
    /// This method is called automatically when the player lands.
    /// It receives comprehensive impact data from the unified system and applies
    /// camera compression based on the calculated severity.
    /// 
    /// Benefits:
    /// - No duplicate fall tracking (FallingDamageSystem handles it)
    /// - Consistent thresholds across all systems
    /// - Works for ALL jump types (tiny, light, moderate, severe, lethal)
    /// - Integrated with aerial tricks, sprint, and other context
    /// 
    /// Called by: ImpactEventBroadcaster when FallingDamageSystem detects landing
    /// </summary>
    /// <param name="impact">Comprehensive impact data from unified system</param>
    private void OnImpactReceived(ImpactData impact)
    {
        // Early exit if landing impact is disabled
        if (!enableLandingImpact) return;
        
        // Skip if no compression needed (impact too small)
        // This happens for falls < 50 units (like stepping off a curb)
        if (impact.compressionAmount <= 0f) return;
        
        // Use compression amount from unified system
        // This value is already scaled by severity tier (Light/Moderate/Severe/Lethal)
        // and calculated using the same thresholds as damage system
        float compressionAmount = -impact.compressionAmount;
        
        // Apply instant compression (knee bend) - this is the "impact" moment
        // The spring physics in UpdateLandingImpact() will smoothly recover from this
        landingCompressionOffset = compressionAmount;
        
        // Set initial downward velocity for spring
        // This makes the impact feel more "punchy" - camera compresses fast, recovers slow
        // Multiplier of 2.0 gives good impact feel without being too jarring
        landingCompressionVelocity = compressionAmount * 2f;
        
        // Apply forward tilt for extra realism (optional)
        // This simulates the player's body leaning forward on impact
        // Scaled by severity: tiny jumps = subtle tilt, big falls = dramatic tilt
        if (enableLandingTilt)
        {
            landingTiltOffset = maxLandingTiltAngle * impact.severityNormalized;
            landingTiltVelocity = landingTiltOffset * 1.5f;
        }
        
        // Reset dynamic wall tilt on landing
        // Prevents weird tilt carryover from wall jumps/tricks
        dynamicWallTiltTarget = 0f;
        dynamicWallTilt = 0f;
        
        // Debug log for tuning (disable in production)
        // Shows: severity tier, fall distance, compression amount, trauma intensity
        Debug.Log($"[CAMERA IMPACT] 🎯 {impact.severity} | " +
                  $"Fall: {impact.fallDistance:F0}u | " +
                  $"Compression: {compressionAmount:F1} | " +
                  $"Trauma: {impact.traumaIntensity:F2} | " +
                  $"Trick: {(impact.wasInTrick ? "YES" : "NO")}");
    }
    
    private void UpdateIdleSway()
    {
        if (!enableIdleSway)
        {
            idleSwayOffset = Vector3.zero;
            return;
        }
        
        // Check if player is idle (low movement speed)
        bool isIdle = movementController == null || movementController.CurrentSpeed < 50f;
        
        if (isIdle)
        {
            idleSwayTime += Time.deltaTime * idleSwaySpeed;
            
            // Create subtle breathing motion using sine waves
            float swayX = Mathf.Sin(idleSwayTime * 0.8f) * idleSwayAmount;
            float swayY = Mathf.Sin(idleSwayTime * 1.2f) * idleSwayAmount * 0.5f; // Vertical breathing
            float swayZ = Mathf.Sin(idleSwayTime * 0.6f) * idleSwayAmount * 0.3f;
            
            reusableSwayVector.x = swayX;
            reusableSwayVector.y = swayY;
            reusableSwayVector.z = swayZ;
            
            // Smoothly blend in the sway
            idleSwayOffset = Vector3.Lerp(idleSwayOffset, reusableSwayVector, 5f * Time.deltaTime);
        }
        else
        {
            // Smoothly fade out when moving
            idleSwayOffset = Vector3.Lerp(idleSwayOffset, Vector3.zero, 10f * Time.deltaTime);
        }
    }
    
    /// <summary>
    /// Call of Duty style headbob - realistic, subtle, weighted
    /// Features:
    /// - Velocity-based intensity scaling
    /// - Sharp footstep impacts (not floaty sine waves)
    /// - Subtle forward lean for momentum feel
    /// - Minimal horizontal sway (grounded, not drunk)
    /// - Step-based tilt for weight distribution
    /// - Disabled during slides (slide has its own camera feel)
    /// </summary>
    private void UpdateHeadBob()
    {
        if (!enableHeadBob || movementController == null)
        {
            headBobOffset = Vector3.Lerp(headBobOffset, Vector3.zero, headBobSmoothness * Time.deltaTime);
            stepTiltAngle = Mathf.SmoothDamp(stepTiltAngle, 0f, ref stepTiltVelocity, 0.15f);
            currentBobIntensity = Mathf.Lerp(currentBobIntensity, 0f, headBobSmoothness * Time.deltaTime);
            return;
        }
        
        // Check if player is grounded and moving
        bool isGrounded = movementController.IsGrounded;
        bool isSliding = movementController.IsSliding; // Don't bob during slides!
        Vector3 velocity = movementController.GetVelocity();
        float horizontalSpeed = new Vector2(velocity.x, velocity.z).magnitude;
        bool isMoving = horizontalSpeed > 50f; // Unity units
        
        // CRITICAL: Disable headbob during slides (slide has its own camera feel)
        if (isSliding)
        {
            headBobOffset = Vector3.Lerp(headBobOffset, Vector3.zero, headBobSmoothness * 2f * Time.deltaTime);
            stepTiltAngle = Mathf.SmoothDamp(stepTiltAngle, 0f, ref stepTiltVelocity, 0.1f);
            currentBobIntensity = Mathf.Lerp(currentBobIntensity, 0f, headBobSmoothness * 2f * Time.deltaTime);
            headBobTimer = 0f;
            lastStepPhase = 0f;
            return;
        }
        
        // Check if sprinting
        bool isSprinting = Input.GetKey(Controls.Boost) && 
                          (energySystem == null || energySystem.CanSprint) &&
                          horizontalSpeed > 400f;
        
        if (isGrounded && isMoving)
        {
            // === FREQUENCY CALCULATION (COD-style) ===
            // Smoothly transition between walk and sprint frequencies
            float targetFrequency = isSprinting ? sprintBobFrequency : walkBobFrequency;
            currentBobFrequency = Mathf.Lerp(currentBobFrequency, targetFrequency, 8f * Time.deltaTime);
            
            // === INTENSITY SCALING (Velocity-based) ===
            // Scale bob intensity based on actual movement speed (feels more responsive)
            float speedRatio = Mathf.Clamp01(horizontalSpeed / (isSprinting ? 800f : 500f));
            float targetIntensity = Mathf.Lerp(0.3f, 1f, speedRatio); // Never fully zero when moving
            currentBobIntensity = Mathf.Lerp(currentBobIntensity, targetIntensity, headBobSmoothness * Time.deltaTime);
            
            // Velocity influence: faster movement = more bob
            float velocityMultiplier = 1f + (velocityInfluence * speedRatio);
            
            // === TIMER INCREMENT ===
            headBobTimer += Time.deltaTime * currentBobFrequency;
            
            // === FOOTSTEP PHASE CALCULATION ===
            // Use a sharper curve for realistic footstep impacts (not floaty sine)
            float stepPhase = headBobTimer % 1f; // 0 to 1 per step
            
            // Detect footstep impact (phase wraps around)
            if (stepPhase < lastStepPhase)
            {
                // Footstep just occurred - apply subtle tilt
                if (enableStepTilt)
                {
                    // Alternate tilt direction per step (left foot, right foot)
                    float tiltDirection = (Mathf.FloorToInt(headBobTimer) % 2 == 0) ? 1f : -1f;
                    stepTiltAngle = maxStepTiltAngle * tiltDirection * currentBobIntensity;
                }
            }
            lastStepPhase = stepPhase;
            
            // === VERTICAL BOB (Compression on footstep) ===
            // Sharp downward compression on footstep, smooth recovery
            // Using power curve for realistic weight transfer
            float verticalCurve = Mathf.Pow(Mathf.Sin(stepPhase * Mathf.PI), footstepSharpness);
            float verticalBob = -verticalCurve * headBobVerticalIntensity * velocityMultiplier * currentBobIntensity;
            
            // === HORIZONTAL SWAY (Minimal, realistic weight shift) ===
            // Very subtle side-to-side, synchronized with steps
            // COD uses minimal horizontal movement compared to vertical
            float horizontalCurve = Mathf.Sin(stepPhase * Mathf.PI * 2f); // Full sine for smooth sway
            float horizontalBob = horizontalCurve * headBobHorizontalIntensity * velocityMultiplier * currentBobIntensity;
            
            // === FORWARD LEAN (Momentum feel) ===
            // Subtle forward push during movement for weight/momentum
            // More pronounced during sprint
            float forwardLean = headBobForwardIntensity * velocityMultiplier * currentBobIntensity;
            if (isSprinting)
            {
                forwardLean *= 1.5f; // Extra lean during sprint
            }
            
            // === COMBINE ALL COMPONENTS ===
            Vector3 targetBob = new Vector3(
                horizontalBob,
                verticalBob,
                forwardLean
            );
            
            // Smooth interpolation for natural feel
            headBobOffset = Vector3.Lerp(headBobOffset, targetBob, headBobSmoothness * Time.deltaTime);
            
            // === STEP TILT (Weight distribution) ===
            if (enableStepTilt)
            {
                // Smooth damp for natural spring-like return
                stepTiltAngle = Mathf.SmoothDamp(stepTiltAngle, 0f, ref stepTiltVelocity, 0.12f);
            }
        }
        else
        {
            // === IDLE STATE (Smooth return to center) ===
            headBobOffset = Vector3.Lerp(headBobOffset, Vector3.zero, headBobSmoothness * Time.deltaTime);
            stepTiltAngle = Mathf.SmoothDamp(stepTiltAngle, 0f, ref stepTiltVelocity, 0.15f);
            currentBobIntensity = Mathf.Lerp(currentBobIntensity, 0f, headBobSmoothness * Time.deltaTime);
            
            // Reset timer when not moving
            if (!isMoving)
            {
                headBobTimer = 0f;
                lastStepPhase = 0f;
            }
        }
    }
    
    private void UpdateStrafeTilt()
    {
        if (!enableStrafeTilt)
        {
            targetTilt = 0f;
            currentTilt = Mathf.SmoothDamp(currentTilt, targetTilt, ref tiltVelocity, 1f / tiltReturnSpeed);
            return;
        }
        
        // Get strafe input using the Controls system
        strafeInput = Controls.HorizontalRaw();
        
        // Apply response curve for more control over the feel
        float curvedInput = tiltResponseCurve.Evaluate(Mathf.Abs(strafeInput)) * Mathf.Sign(strafeInput);
        
        // Calculate target tilt (negative for left strafe, positive for right strafe)
        // We negate it so tilting feels natural (tilt into the direction of movement)
        targetTilt = -curvedInput * maxTiltAngle;
        
        // Use spring damping for more natural, bouncy feel (AAA quality)
        float smoothTime = strafeInput != 0 ? 1f / tiltSpeed : 1f / tiltReturnSpeed;
        currentTilt = Mathf.SmoothDamp(currentTilt, targetTilt, ref tiltVelocity, smoothTime, Mathf.Infinity, Time.deltaTime);
        
        // Apply spring damping for overshoot effect
        if (Mathf.Abs(strafeInput) < 0.1f && Mathf.Abs(tiltVelocity) > 0.1f)
        {
            currentTilt += tiltVelocity * tiltSpringDamping * Time.deltaTime;
        }
    }
    
    /// <summary>
    /// AAA WALL JUMP CAMERA TILT SYSTEM
    /// Creates dynamic, cinematic camera tilt when wall jumping
    /// Based on Titanfall 2, Mirror's Edge, and Dying Light wall jump feel
    /// </summary>
    private void UpdateWallJumpTilt()
    {
        if (!enableWallJumpTilt)
        {
            wallJumpTiltTarget = 0f;
            wallJumpPitchTarget = 0f;
            wallJumpTiltAmount = Mathf.SmoothDamp(wallJumpTiltAmount, 0f, ref wallJumpTiltVelocity, 1f / wallJumpTiltReturnSpeed);
            wallJumpPitchAmount = Mathf.SmoothDamp(wallJumpPitchAmount, 0f, ref wallJumpPitchVelocity, 1f / wallJumpTiltReturnSpeed);
            return;
        }
        
        // Check if wall jump tilt is active
        float timeSinceWallJump = Time.time - wallJumpTiltStartTime;
        bool isWallJumpTiltActive = timeSinceWallJump < wallJumpTiltDuration;
        
        if (isWallJumpTiltActive)
        {
            // Calculate tilt progress (0 to 1 over duration)
            float tiltProgress = Mathf.Clamp01(timeSinceWallJump / wallJumpTiltDuration);
            
            // Apply animation curve for dynamic feel (starts strong, eases out)
            float curveValue = wallJumpTiltCurve.Evaluate(tiltProgress);
            
            // Apply curve to both tilt and pitch
            wallJumpTiltTarget = wallJumpTiltTarget * curveValue;
            
            if (enableWallJumpPitch)
            {
                wallJumpPitchTarget = wallJumpMaxPitchAngle * curveValue;
            }
            
            // Fast interpolation during active phase
            float smoothTime = 1f / wallJumpTiltSpeed;
            wallJumpTiltAmount = Mathf.SmoothDamp(wallJumpTiltAmount, wallJumpTiltTarget, ref wallJumpTiltVelocity, smoothTime);
            wallJumpPitchAmount = Mathf.SmoothDamp(wallJumpPitchAmount, wallJumpPitchTarget, ref wallJumpPitchVelocity, smoothTime);
        }
        else
        {
            // Return to neutral smoothly
            wallJumpTiltTarget = 0f;
            wallJumpPitchTarget = 0f;
            
            float returnTime = 1f / wallJumpTiltReturnSpeed;
            wallJumpTiltAmount = Mathf.SmoothDamp(wallJumpTiltAmount, 0f, ref wallJumpTiltVelocity, returnTime);
            wallJumpPitchAmount = Mathf.SmoothDamp(wallJumpPitchAmount, 0f, ref wallJumpPitchVelocity, returnTime);
        }
    }
    
    /// <summary>
    /// DYNAMIC WALL-RELATIVE CAMERA TILT SYSTEM
    /// Tilts camera AWAY from walls during airborne wall jump chains
    /// Zero new raycasts - reuses existing wall detection from movement controller
    /// </summary>
    private void UpdateDynamicWallTilt()
    {
        if (!enableDynamicWallTilt || movementController == null)
        {
            dynamicWallTiltTarget = 0f;
            return;
        }

        // Only active during wall jump chains
        if (!movementController.IsInWallJumpChain)
        {
            dynamicWallTiltTarget = 0f;
        }
        else
        {
            Vector3 wallPoint = movementController.LastWallHitPoint;
            if (wallPoint != Vector3.zero)
            {
                // Calculate screen position of wall
                Vector3 screenPos = playerCamera.WorldToViewportPoint(wallPoint);
                float horizontalPos = (screenPos.x - 0.5f) * 2f; // -1 to +1

                // Determine side (with deadzone)
                int wallSide = 0;
                if (horizontalPos < -screenCenterDeadzone) wallSide = -1; // Left
                else if (horizontalPos > screenCenterDeadzone) wallSide = 1; // Right

                // Tilt AWAY from wall (negate to invert)
                dynamicWallTiltTarget = -wallSide * dynamicTiltMaxAngle;

                if (showDynamicTiltDebug)
                    Debug.Log($"[CAMERA] Wall screen pos: {horizontalPos:F2}, side: {wallSide}, tilt: {dynamicWallTiltTarget:F1}°");
            }
            else
            {
                dynamicWallTiltTarget = 0f;
            }
        }

        // Smooth transition
        float smoothTime = (Mathf.Abs(dynamicWallTiltTarget) > 0.01f) 
            ? (1f / dynamicTiltSpeed) 
            : (1f / dynamicTiltReturnSpeed);
        dynamicWallTilt = Mathf.SmoothDamp(dynamicWallTilt, dynamicWallTiltTarget, 
                                           ref dynamicWallTiltVelocity, smoothTime);
    }
    
    /// <summary>
    /// PUBLIC API: Call this when player performs a wall jump
    /// wallNormal: The normal vector of the wall (pointing away from wall)
    /// 🤝 BFFL FIX: Now aware of trick state - instantly cancels reconciliation for seamless combo flow!
    /// </summary>
    public void TriggerWallJumpTilt(Vector3 wallNormal)
    {
        if (!enableWallJumpTilt) return;
        
        // 🤝 BFFL: Instantly reconcile camera to UPRIGHT when wall jumping from trick
        // This is CRITICAL - wall jump should feel like landing (camera resets to normal)
        bool wasInTrickMode = isReconciling || isInLandingGrace || isFreestyleModeActive;
        
        if (wasInTrickMode)
        {
            // FORCE IMMEDIATE RECONCILIATION TO UPRIGHT (like landing)
            // Extract current yaw to preserve look direction
            Vector3 freestyleEuler = freestyleRotation.eulerAngles;
            float normalizedYaw = NormalizeAngle(freestyleEuler.y);
            
            // Snap camera to UPRIGHT orientation (pitch=0, roll=0, preserve yaw)
            // This is the same target as landing reconciliation
            Quaternion uprightTarget = Quaternion.Euler(0f, normalizedYaw, 0f);
            
            // INSTANT snap - no blending, wall jump demands immediate response
            freestyleRotation = uprightTarget;
            
            // Clear ALL trick states
            isReconciling = false;
            isInLandingGrace = false;
            isFreestyleModeActive = false;
            
            // Reset momentum physics (but PRESERVE keyboard roll momentum for smooth feel)
            angularVelocity = Vector2.zero;
            rollVelocity = 0f;
            // keyboardRollVelocity is NOT reset - let it naturally decay
            smoothedInput = Vector2.zero;
            inputVelocity = Vector2.zero;
            isFlickBurstActive = false;
            
            Debug.Log($"🤝 [TRICK→WALLJUMP] INSTANT RECONCILIATION! Camera snapped to upright (yaw: {normalizedYaw:F1}°)");
        }
        
        // 🔥 COMBO SYSTEM INTEGRATION: Register wall jump in combo tracker
        if (ComboMultiplierSystem.Instance != null)
        {
            bool isAirborne = movementController != null && !movementController.IsGrounded;
            ComboMultiplierSystem.Instance.AddWallJump(isAirborne);
            
            // Extra feedback for seamless transitions
            if (wasInTrickMode)
            {
                Debug.Log("🤝✨ [COMBO] SEAMLESS Trick→WallJump transition detected!");
            }
        }
        
        // Calculate tilt direction based on wall normal
        // We want to tilt AWAY from the wall (into the jump direction)
        
        // Project wall normal onto camera's right vector to determine tilt direction
        Vector3 cameraRight = transform.right;
        float dotRight = Vector3.Dot(wallNormal, cameraRight);
        
        // Positive dot = wall is to the right, tilt left (negative angle)
        // Negative dot = wall is to the left, tilt right (positive angle)
        // We negate because we want to tilt AWAY from wall
        wallJumpTiltTarget = -Mathf.Sign(dotRight) * wallJumpMaxTiltAngle;
        
        // Start the tilt timer
        wallJumpTiltStartTime = Time.time;
        
        Debug.Log($"🎥 [WALL JUMP TILT] Triggered! Tilt: {wallJumpTiltTarget:F1}°, Wall Normal: {wallNormal}, Dot: {dotRight:F2}");
    }
    
    /// <summary>
    /// 🔥 SMART FOV SYSTEM: Only transitions when needed, not every frame
    /// </summary>
    private void UpdateFOVTransition()
    {
        // Only update if we're transitioning (currentFOV != targetFOV)
        if (Mathf.Abs(currentFOV - targetFOV) > 0.01f)
        {
            // Smooth FOV transition
            currentFOV = Mathf.Lerp(currentFOV, targetFOV, fovTransitionSpeed * Time.deltaTime);
            playerCamera.fieldOfView = currentFOV;
        }
    }
    
    /// <summary>
    /// 🔥 PUBLIC METHOD: Call this when sprint STARTS
    /// </summary>
    public void SetSprintFOV()
    {
        targetFOV = baseFOV + sprintFOVIncrease;
        Debug.Log($"[AAACameraController] Sprint started - FOV target: {targetFOV}");
    }
    
    /// <summary>
    /// 🔥 PUBLIC METHOD: Call this when sprint STOPS
    /// </summary>
    public void SetNormalFOV()
    {
        targetFOV = baseFOV;
        Debug.Log($"[AAACameraController] Sprint stopped - FOV target: {targetFOV}");
    }
    
    /// <summary>
    /// Called when sprint is interrupted due to energy depletion
    /// </summary>
    private void OnSprintInterrupted()
    {
        // Force FOV back to base immediately when energy runs out
        targetFOV = baseFOV;
        Debug.Log("[AAACameraController] Sprint interrupted - resetting FOV to base");
    }
    
    void OnDestroy()
    {
        // 🎯 UNIFIED IMPACT SYSTEM: Unsubscribe from impact events
        // CRITICAL: Prevents memory leaks and null reference errors
        ImpactEventBroadcaster.OnImpact -= OnImpactReceived;
        
        // Unsubscribe from sprint interruption event
        PlayerEnergySystem.OnSprintInterrupted -= OnSprintInterrupted;
        
        Debug.Log("[AAACameraController] Unsubscribed from all events");
    }
    
    private void UpdateCameraShake()
    {
        if (!enableCameraShake)
        {
            shakeOffset = Vector3.zero;
            return;
        }
        
        // Update beam shake (continuous while active)
        UpdateBeamShake();
        
        // Update shotgun shake (timed bursts)
        UpdateShotgunShake();
        
        // Combine all shake effects
        shakeOffset = currentBeamShake + currentShotgunShake;
        
        // Apply decay to smooth out transitions
        shakeOffset = Vector3.Lerp(shakeOffset, Vector3.zero, shakeDecay * Time.deltaTime);
    }
    
    private void UpdateBeamShake()
    {
        bool bothBeamsActive = isPrimaryBeamActive && isSecondaryBeamActive;
        float intensityMultiplier = bothBeamsActive ? dualHandMultiplier : 1f;
        
        if (isPrimaryBeamActive || isSecondaryBeamActive)
        {
            beamShakeTime += Time.deltaTime * beamShakeSpeed;
            
            // Generate smooth trembling motion - reuse vector to avoid garbage
            float shakeX = Mathf.PerlinNoise(beamShakeTime * 1.5f, 0f) - 0.5f;
            float shakeY = Mathf.PerlinNoise(0f, beamShakeTime * 1.3f) - 0.5f;
            float shakeZ = Mathf.PerlinNoise(beamShakeTime * 0.8f, beamShakeTime * 0.7f) - 0.5f;
            
            reusableShakeVector.x = shakeX;
            reusableShakeVector.y = shakeY;
            reusableShakeVector.z = shakeZ;
            currentBeamShake = reusableShakeVector * beamShakeIntensity * intensityMultiplier;
        }
        else
        {
            // Gradually reduce beam shake when not active
            currentBeamShake = Vector3.Lerp(currentBeamShake, Vector3.zero, shakeDecay * Time.deltaTime);
        }
    }
    
    private void UpdateShotgunShake()
    {
        // Update primary hand shotgun shake
        if (primaryShotgunShakeTimer > 0f)
        {
            primaryShotgunShakeTimer -= Time.deltaTime;
            float normalizedTime = 1f - (primaryShotgunShakeTimer / shotgunShakeDuration);
            float intensity = shakeIntensityCurve.Evaluate(normalizedTime);
            
            // Generate random direction for shotgun kick - reuse vector
            reusableShakeVector.x = UnityEngine.Random.Range(-1f, 1f);
            reusableShakeVector.y = UnityEngine.Random.Range(-1f, 1f); // Balanced vertical shake
            reusableShakeVector.z = UnityEngine.Random.Range(-0.3f, 0.3f);
            
            currentShotgunShake += reusableShakeVector * (shotgunShakeIntensity * intensity * 0.5f); // Primary hand contribution
        }
        
        // Update secondary hand shotgun shake
        if (secondaryShotgunShakeTimer > 0f)
        {
            secondaryShotgunShakeTimer -= Time.deltaTime;
            float normalizedTime = 1f - (secondaryShotgunShakeTimer / shotgunShakeDuration);
            float intensity = shakeIntensityCurve.Evaluate(normalizedTime);
            
            reusableShakeVector.x = UnityEngine.Random.Range(-1f, 1f);
            reusableShakeVector.y = UnityEngine.Random.Range(-1f, 1f); // Balanced vertical shake
            reusableShakeVector.z = UnityEngine.Random.Range(-0.3f, 0.3f);
            
            currentShotgunShake += reusableShakeVector * (shotgunShakeIntensity * intensity * 0.5f); // Secondary hand contribution
        }
        
        // Apply dual hand multiplier if both hands are firing shotgun
        if (primaryShotgunShakeTimer > 0f && secondaryShotgunShakeTimer > 0f)
        {
            currentShotgunShake *= dualHandMultiplier;
        }
        
        // Decay shotgun shake
        currentShotgunShake = Vector3.Lerp(currentShotgunShake, Vector3.zero, shakeDecay * 2f * Time.deltaTime);
    }
    
    private void ApplyCameraTransform()
    {
        // Safety checks to prevent NaN values
        if (float.IsNaN(currentLook.y)) currentLook.y = 0f;
        if (float.IsNaN(landingTiltOffset)) landingTiltOffset = 0f;
        if (float.IsNaN(currentTilt)) currentTilt = 0f;
        if (float.IsNaN(wallJumpTiltAmount)) wallJumpTiltAmount = 0f;
        if (float.IsNaN(wallJumpPitchAmount)) wallJumpPitchAmount = 0f;
        
        // === AAA CAMERA ROTATION SYSTEM ===
        // Combines multiple tilt sources for cinematic feel
        
        // 🎪 FREESTYLE MODE: Camera rotation is COMPLETELY INDEPENDENT
        if (isFreestyleModeActive || isReconciling)
        {
            // During tricks or landing reconciliation, use freestyle rotation
            transform.localRotation = freestyleRotation;
        }
        else
        {
            // Normal camera behavior
            // Combine all tilt sources (strafe + wall jump + dynamic wall-relative + step tilt)
            float totalRollTilt = currentTilt + wallJumpTiltAmount + dynamicWallTilt + stepTiltAngle;
            
            // Combine all pitch sources (landing + wall jump)
            float totalPitch = currentLook.y + landingTiltOffset + wallJumpPitchAmount;
            
            // Apply combined rotation
            Vector3 eulerAngles = new Vector3(
                totalPitch,      // Pitch: Look up/down + landing tilt + wall jump pitch
                0,               // Yaw: Handled by player body rotation
                totalRollTilt    // Roll: Strafe tilt + wall jump tilt + step tilt
            );
            
            // Additional safety check for the final euler angles
            if (float.IsNaN(eulerAngles.x)) eulerAngles.x = 0f;
            if (float.IsNaN(eulerAngles.y)) eulerAngles.y = 0f;
            if (float.IsNaN(eulerAngles.z)) eulerAngles.z = 0f;
            
            transform.localRotation = Quaternion.Euler(eulerAngles);
        }
        
        // Combine all position offsets (shake, landing compression, idle sway)
        Vector3 totalOffset = Vector3.zero;
        
        if (enableCameraShake && shakeOffset.sqrMagnitude > 0.001f)
        {
            totalOffset += shakeOffset;
        }
        
        // Apply trauma shake (AAA impact effects)
        if (enableTraumaShake && traumaShakeOffset.sqrMagnitude > 0.001f)
        {
            totalOffset += traumaShakeOffset;
        }
        
        // Apply smooth spring-based landing compression (knee bend)
        if (enableLandingImpact && Mathf.Abs(landingCompressionOffset) > 0.001f)
        {
            totalOffset.y += landingCompressionOffset;
        }
        
        if (enableIdleSway)
        {
            totalOffset += idleSwayOffset;
        }
        
        if (enableHeadBob)
        {
            totalOffset += headBobOffset;
        }
        
        // Apply motion prediction for ultra-smooth movement
        if (enableMotionPrediction && predictedVelocity.sqrMagnitude > 0.01f)
        {
            totalOffset += predictedVelocity * Time.deltaTime * 0.1f; // Subtle prediction
        }
        
        // Apply all offsets to current position
        if (totalOffset.sqrMagnitude > 0.0001f)
        {
            Vector3 currentPos = transform.localPosition;
            transform.localPosition = currentPos + totalOffset;
        }
    }
    
    // Public methods for camera shake system
    
    /// <summary>
    /// Start beam shake effect for primary hand (continuous until stopped)
    /// </summary>
    public void StartPrimaryBeamShake()
    {
        isPrimaryBeamActive = true;
        Debug.Log("[AAACameraController] Primary beam shake started");
    }
    
    /// <summary>
    /// Stop beam shake effect for primary hand
    /// </summary>
    public void StopPrimaryBeamShake()
    {
        isPrimaryBeamActive = false;
        Debug.Log("[AAACameraController] Primary beam shake stopped");
    }
    
    /// <summary>
    /// Start beam shake effect for secondary hand (continuous until stopped)
    /// </summary>
    public void StartSecondaryBeamShake()
    {
        isSecondaryBeamActive = true;
        Debug.Log("[AAACameraController] Secondary beam shake started");
    }
    
    /// <summary>
    /// Stop beam shake effect for secondary hand
    /// </summary>
    public void StopSecondaryBeamShake()
    {
        isSecondaryBeamActive = false;
        Debug.Log("[AAACameraController] Secondary beam shake stopped");
    }
    
    /// <summary>
    /// Trigger shotgun shake burst for primary hand
    /// </summary>
    public void TriggerPrimaryShotgunShake()
    {
        primaryShotgunShakeTimer = shotgunShakeDuration;
        Debug.Log("[AAACameraController] Primary shotgun shake triggered");
    }
    
    /// <summary>
    /// Trigger shotgun shake burst for secondary hand
    /// </summary>
    public void TriggerSecondaryShotgunShake()
    {
        secondaryShotgunShakeTimer = shotgunShakeDuration;
        Debug.Log("[AAACameraController] Secondary shotgun shake triggered");
    }
    
    /// <summary>
    /// Stop all camera shake effects immediately
    /// </summary>
    public void StopAllShake()
    {
        isPrimaryBeamActive = false;
        isSecondaryBeamActive = false;
        primaryShotgunShakeTimer = 0f;
        secondaryShotgunShakeTimer = 0f;
        currentBeamShake = Vector3.zero;
        currentShotgunShake = Vector3.zero;
        shakeOffset = Vector3.zero;
        currentTrauma = 0f;
        traumaShakeOffset = Vector3.zero;
        Debug.Log("[AAACameraController] All camera shake stopped");
    }
    
    /// <summary>
    /// AAA Trauma System - Add impact shake with intensity 0-1
    /// Perfect for falls, collisions, explosions
    /// </summary>
    public void AddTrauma(float trauma)
    {
        if (!enableTraumaShake) return;
        
        currentTrauma = Mathf.Clamp01(currentTrauma + trauma);
        Debug.Log($"[AAACameraController] Trauma added: {trauma:F2}, Current: {currentTrauma:F2}");
    }
    
    /// <summary>
    /// Update trauma-based shake system (called in LateUpdate)
    /// </summary>
    private void UpdateTraumaShake()
    {
        if (!enableTraumaShake || currentTrauma <= 0.001f)
        {
            traumaShakeOffset = Vector3.zero;
            return;
        }
        
        // Decay trauma over time
        currentTrauma = Mathf.Max(0f, currentTrauma - traumaDecayRate * Time.deltaTime);
        
        // Calculate shake intensity from trauma using curve
        float shakeAmount = traumaIntensityCurve.Evaluate(currentTrauma) * traumaShakeIntensity;
        
        // Generate Perlin noise-based shake (more organic than random)
        traumaShakeTime += Time.deltaTime * traumaShakeSpeed;
        float noiseX = (Mathf.PerlinNoise(traumaShakeTime, 0f) - 0.5f) * 2f;
        float noiseY = (Mathf.PerlinNoise(0f, traumaShakeTime) - 0.5f) * 2f;
        float noiseZ = (Mathf.PerlinNoise(traumaShakeTime, traumaShakeTime) - 0.5f) * 2f;
        
        traumaShakeOffset = new Vector3(
            noiseX * shakeAmount,
            noiseY * shakeAmount,
            noiseZ * shakeAmount * 0.5f // Less Z shake for comfort
        );
    }
    
    // Legacy public methods
    public void SetSensitivity(float sensitivity)
    {
        mouseSensitivity = sensitivity;
    }
    
    public void SetFOV(float fov)
    {
        baseFOV = fov;
    }
    
    // UpdateOriginalCameraPosition method REMOVED - no longer needed
    
    // Properties
    public float CurrentFOV => currentFOV;
    public Vector2 LookRotation => currentLook;
    
    // ===== DECOUPLED HANDS SYSTEM =====
    
    /// <summary>
    /// Initialize the decoupled hands system - PRESERVES your hand positions AND rotations!
    /// </summary>
    private void InitializeDecoupledHands()
    {
        if (primaryHandTransform != null)
        {
            basePrimaryHandRotation = primaryHandTransform.localRotation; // Store YOUR rotation
            basePrimaryHandPosition = primaryHandTransform.localPosition; // Store YOUR position
            currentPrimaryHandOffset = Vector3.zero; // Start with no offset
            primaryRotationOffset = Quaternion.identity; // Start with no rotation offset
            Debug.Log($"[AAACameraController] Primary hand stored - Pos: {basePrimaryHandPosition}, Rot: {basePrimaryHandRotation.eulerAngles}");
        }
        
        if (secondaryHandTransform != null)
        {
            baseSecondaryHandRotation = secondaryHandTransform.localRotation; // Store YOUR rotation
            baseSecondaryHandPosition = secondaryHandTransform.localPosition; // Store YOUR position
            currentSecondaryHandOffset = Vector3.zero; // Start with no offset
            secondaryRotationOffset = Quaternion.identity; // Start with no rotation offset
            Debug.Log($"[AAACameraController] Secondary hand stored - Pos: {baseSecondaryHandPosition}, Rot: {baseSecondaryHandRotation.eulerAngles}");
        }
        
        lastCameraRotationEuler = transform.localEulerAngles;
        
        Debug.Log("[AAACameraController] Decoupled hands initialized - YOUR positions and rotations preserved!");
    }
    
    /// <summary>
    /// Update decoupled hands with lag, sway, and procedural motion
    /// </summary>
    private void UpdateDecoupledHands()
    {
        if (primaryHandTransform == null && secondaryHandTransform == null)
            return;
        
        // Calculate camera rotation delta for reactive motion
        Vector3 currentCameraEuler = transform.localEulerAngles;
        rotationDelta = Mathf.Abs(Mathf.DeltaAngle(lastCameraRotationEuler.x, currentCameraEuler.x)) + 
                        Mathf.Abs(Mathf.DeltaAngle(lastCameraRotationEuler.y, currentCameraEuler.y));
        lastCameraRotationEuler = currentCameraEuler;
        
        // Calculate procedural hand sway
        if (enableProceduralHandSway)
        {
            UpdateProceduralHandSway();
        }
        
        // Update primary hand - DIRECT, no smoothing!
        if (primaryHandTransform != null)
        {
            UpdateSingleHand(primaryHandTransform, basePrimaryHandRotation, basePrimaryHandPosition, 1f);
        }
        
        // Update secondary hand (with slight offset for asymmetry) - DIRECT, no smoothing!
        if (secondaryHandTransform != null)
        {
            UpdateSingleHand(secondaryHandTransform, baseSecondaryHandRotation, baseSecondaryHandPosition, -1f);
        }
    }
    
    /// <summary>
    /// Update a single hand transform - DIRECT FOLLOW with only procedural sway, NO SMOOTHING!
    /// </summary>
    private void UpdateSingleHand(Transform handTransform, Quaternion baseRotation, Vector3 basePosition, float swayMultiplier)
    {
        // Calculate rotation offset from procedural sway ONLY
        Quaternion rotationOffset = Quaternion.identity;
        
        if (enableProceduralHandSway)
        {
            Vector3 swayRotation = new Vector3(
                proceduralHandOffset.y * handSwayIntensity * swayMultiplier * 10f, // Pitch from vertical sway
                proceduralHandOffset.x * handSwayIntensity * swayMultiplier * 15f, // Yaw from horizontal sway
                proceduralHandOffset.x * handSwayIntensity * swayMultiplier * -8f  // Roll for natural feel
            );
            rotationOffset = Quaternion.Euler(swayRotation);
        }
        
        // Apply: YOUR base rotation * OUR sway offset - DIRECT, no smoothing!
        handTransform.localRotation = baseRotation * rotationOffset;
        
        // Position offset - ONLY the sway offset
        Vector3 positionOffset = Vector3.zero;
        
        if (enableProceduralHandSway)
        {
            positionOffset = proceduralHandOffset * handSwayIntensity * swayMultiplier;
        }
        
        // Apply: YOUR base position + OUR sway offset - DIRECT, no smoothing!
        handTransform.localPosition = basePosition + positionOffset;
    }
    
    /// <summary>
    /// Generate procedural hand sway based on movement and camera rotation
    /// </summary>
    private void UpdateProceduralHandSway()
    {
        handSwayTime += Time.deltaTime * handSwaySpeed;
        
        // Get movement input for reactive sway
        float horizontalInput = Controls.HorizontalRaw();
        float verticalInput = Controls.VerticalRaw();
        
        // Base sway (subtle breathing/idle motion)
        float baseSwayX = Mathf.Sin(handSwayTime * 0.8f) * 0.01f;
        float baseSwayY = Mathf.Sin(handSwayTime * 1.2f) * 0.008f;
        float baseSwayZ = Mathf.Sin(handSwayTime * 0.6f) * 0.005f;
        
        // Movement-reactive sway
        float movementSwayX = horizontalInput * 0.02f;
        float movementSwayY = verticalInput * 0.015f;
        
        // Rotation-reactive sway (hands lag behind fast camera turns)
        float rotationSwayX = -lookInput.x * 0.03f;
        float rotationSwayY = lookInput.y * 0.02f;
        
        // Combine all sway sources - DIRECT, no smoothing!
        proceduralHandOffset.x = baseSwayX + movementSwayX + rotationSwayX;
        proceduralHandOffset.y = baseSwayY + movementSwayY + rotationSwayY;
        proceduralHandOffset.z = baseSwayZ;
    }
    
    /// <summary>
    /// Set hand transforms at runtime (useful for dynamic hand switching)
    /// </summary>
    public void SetHandTransforms(Transform primary, Transform secondary)
    {
        primaryHandTransform = primary;
        secondaryHandTransform = secondary;
        
        if (enableDecoupledHands)
        {
            InitializeDecoupledHands();
        }
    }
    
    /// <summary>
    /// Enable/disable decoupled hands system at runtime
    /// </summary>
    public void SetDecoupledHandsEnabled(bool enabled)
    {
        enableDecoupledHands = enabled;
        
        if (enabled)
        {
            InitializeDecoupledHands();
        }
    }
    
    /// <summary>
    /// Get current procedural hand offset (useful for external systems)
    /// </summary>
    public Vector3 GetProceduralHandOffset()
    {
        return proceduralHandOffset;
    }
    
    // ========================================
    // 🛡️ STATE MACHINE & EMERGENCY RECOVERY (PHASE 1)
    // ========================================
    
    /// <summary>
    /// PHASE 1: Validate state transition - prevents invalid state changes
    /// </summary>
    private bool CanTransitionTo(TrickSystemState newState)
    {
        switch (_trickState)
        {
            case TrickSystemState.Grounded:
                return newState == TrickSystemState.JumpInitiated;
                
            case TrickSystemState.JumpInitiated:
                return newState == TrickSystemState.Airborne || 
                       newState == TrickSystemState.Grounded || // Allow cancel if jump fails
                       newState == TrickSystemState.FreestyleActive; // Direct to freestyle if middle-click
                
            case TrickSystemState.Airborne:
                return newState == TrickSystemState.FreestyleActive || 
                       newState == TrickSystemState.Grounded; // Allow landing without tricks
                
            case TrickSystemState.FreestyleActive:
                return newState == TrickSystemState.LandingApproach || 
                       newState == TrickSystemState.Reconciling ||
                       newState == TrickSystemState.Airborne; // Allow cancel
                
            case TrickSystemState.LandingApproach:
                return newState == TrickSystemState.Reconciling;
                
            case TrickSystemState.Reconciling:
                return newState == TrickSystemState.TransitionCleanup;
                
            case TrickSystemState.TransitionCleanup:
                return newState == TrickSystemState.Grounded;
                
            default:
                return false;
        }
    }
    
    /// <summary>
    /// PHASE 1: Safe state transition with validation and callbacks
    /// </summary>
    private void TransitionTrickState(TrickSystemState newState)
    {
        // Validate transition
        if (!CanTransitionTo(newState))
        {
            if (showEmergencyDebug)
            {
                Debug.LogWarning($"[TRICK STATE] Invalid transition: {_trickState} → {newState}");
            }
            return;
        }
        
        // Exit current state
        OnTrickStateExit(_trickState);
        
        // Update state
        _previousTrickState = _trickState;
        _trickState = newState;
        _stateEnterTime = Time.time;
        
        // Enter new state
        OnTrickStateEnter(newState);
        
        if (showEmergencyDebug)
        {
            Debug.Log($"[TRICK STATE] {_previousTrickState} → {_trickState}");
        }
    }
    
    /// <summary>
    /// PHASE 1: State enter callback - setup for new state
    /// </summary>
    private void OnTrickStateEnter(TrickSystemState state)
    {
        switch (state)
        {
            case TrickSystemState.Grounded:
                // Ensure all systems are reset
                isFreestyleModeActive = false;
                isReconciling = false;
                break;
                
            case TrickSystemState.FreestyleActive:
                // Sync legacy flag
                isFreestyleModeActive = true;
                break;
                
            case TrickSystemState.Reconciling:
                // 🔧 DISABLED: State machine reconciliation (using manual system instead)
                // The manual UpdateLandingReconciliation() handles everything now
                // DO NOT set isReconciling = true here - it's already set by LandDuringFreestyle()
                
                // Just do XP award, nothing else
                // 🎪 AERIAL TRICK XP SYSTEM - Award XP when entering reconciliation (landing!)
                if (AerialTrickXPSystem.Instance != null)
                {
                    // Calculate airtime
                    float airtime = Time.time - airborneStartTime;
                    
                    // Get rotations
                    Vector3 rotations = new Vector3(totalRotationX, totalRotationY, totalRotationZ);
                    
                    // Check if landing is clean
                    Vector3 currentEuler = freestyleRotation.eulerAngles;
                    float pitchFromUpright = Mathf.Abs(Mathf.DeltaAngle(currentEuler.x, 0f));
                    float rollFromUpright = Mathf.Abs(Mathf.DeltaAngle(currentEuler.z, 0f));
                    float totalDeviation = pitchFromUpright + rollFromUpright;
                    bool isCleanLanding = totalDeviation < cleanLandingThreshold;
                    
                    // Get landing position
                    Vector3 landingPosition = transform.position;
                    
                    // Award XP!
                    AerialTrickXPSystem.Instance.OnTrickLanded(airtime, rotations, landingPosition, isCleanLanding);
                }
                
                // 🔧 CRITICAL: Do NOT set any reconciliation flags here!
                // LandDuringFreestyle() already handles everything
                break;
                
            case TrickSystemState.TransitionCleanup:
                // Final cleanup before grounded
                isReconciling = false;
                isFreestyleModeActive = false;
                break;
        }
    }
    
    /// <summary>
    /// PHASE 1: State exit callback - cleanup for old state
    /// </summary>
    private void OnTrickStateExit(TrickSystemState state)
    {
        switch (state)
        {
            case TrickSystemState.FreestyleActive:
                // Exiting freestyle - log stats
                if (showEmergencyDebug)
                {
                    Debug.Log($"[TRICK STATE] Freestyle ended - X:{totalRotationX:F0}° Y:{totalRotationY:F0}° Z:{totalRotationZ:F0}°");
                }
                break;
        }
    }
    
    /// <summary>
    /// PHASE 1: Emergency recovery - OPTIMIZED to only run checks when needed
    /// </summary>
    private void UpdateEmergencyRecovery()
    {
        if (!enableEmergencyRecovery) return;
        
        // PERFORMANCE: Only check manual upright key every frame (cheap)
        if (Input.GetKeyDown(emergencyUprightKey))
        {
            EmergencyUpright();
            return;
        }
        
        // PERFORMANCE: Skip expensive checks if grounded and idle (most common state)
        if (_trickState == TrickSystemState.Grounded && !isFreestyleModeActive && !isReconciling)
        {
            return; // Nothing to check - system is stable
        }
        
        // 1. CHECK FOR STATE TIMEOUT (only when in active states)
        if (_trickState != TrickSystemState.Grounded)
        {
            float timeInState = Time.time - _stateEnterTime;
            if (timeInState > maxStateTimeout)
            {
                Debug.LogError($"[EMERGENCY] State timeout! Stuck in {_trickState} for {timeInState:F1}s - FORCE RESET");
                EmergencyReset();
                return;
            }
        }
        
        // 2. CHECK FOR TIME.TIMESCALE STUCK (only when NOT in trick/reconcile mode)
        if (!isFreestyleModeActive && !isReconciling && timeDilationManager != null && timeDilationManager.IsTimeDilationActive())
        {
            Debug.LogWarning($"[EMERGENCY] Time.timeScale stuck at {timeDilationManager.GetCurrentTimeScale()}! Resetting to 1.0");
            timeDilationManager.ForceNormalTime();
        }
        
        // 3. AUTO-FIX QUATERNION DRIFT (only once per second, only during tricks)
        if (autoFixQuaternionDrift && isFreestyleModeActive && 
            Time.time - _lastQuaternionNormalizeTime > QUATERNION_NORMALIZE_INTERVAL)
        {
            // Calculate magnitude manually (Quaternion doesn't have sqrMagnitude)
            float sqrMag = freestyleRotation.x * freestyleRotation.x +
                          freestyleRotation.y * freestyleRotation.y +
                          freestyleRotation.z * freestyleRotation.z +
                          freestyleRotation.w * freestyleRotation.w;
            
            if (Mathf.Abs(sqrMag - 1f) > 0.02f)
            {
                if (showEmergencyDebug)
                {
                    Debug.LogWarning($"[EMERGENCY] Quaternion drift detected! Normalizing");
                }
                freestyleRotation = Quaternion.Normalize(freestyleRotation);
            }
            
            _lastQuaternionNormalizeTime = Time.time;
        }
        
        // 4. CHECK FOR INFINITE RECONCILIATION (only when reconciling)
        if (isReconciling && (Time.time - reconciliationStartTime) > 5f)
        {
            Debug.LogWarning("[EMERGENCY] Reconciliation stuck! Force completing.");
            isReconciling = false;
            // 🎯 PRESERVE YAW even in emergency
            float targetYaw = currentLook.x;
            freestyleRotation = Quaternion.Euler(currentLook.y, targetYaw, 0f);
            
            if (_trickState == TrickSystemState.Reconciling)
            {
                TransitionTrickState(TrickSystemState.TransitionCleanup);
                TransitionTrickState(TrickSystemState.Grounded);
            }
        }
    }
    
    /// <summary>
    /// PHASE 1: Emergency upright - force camera to upright position
    /// </summary>
    private void EmergencyUpright()
    {
        // Cooldown check
        if (Time.time - _lastEmergencyResetTime < EMERGENCY_RESET_COOLDOWN)
        {
            Debug.LogWarning("[EMERGENCY] Upright on cooldown!");
            return;
        }
        
        Debug.LogError("[EMERGENCY] FORCE UPRIGHT TRIGGERED!");
        
        // Reset camera to upright
        freestyleRotation = Quaternion.Euler(currentLook.y, 0f, 0f);
        transform.localRotation = freestyleRotation;
        
        // Reset all trick systems (including new state variables)
        isFreestyleModeActive = false;
        isReconciling = false;
        isInLandingGrace = false;
        reconciliationProgress = 0f;
        
        // Force normal time via manager
        if (timeDilationManager != null)
        {
            timeDilationManager.ForceNormalTime();
        }
        wasTimeDilationRequested = false;
        
        // Reset rotation tracking
        totalRotationX = 0f;
        totalRotationY = 0f;
        totalRotationZ = 0f;
        
        // 🎪 RESET MOMENTUM PHYSICS SYSTEM
        angularVelocity = Vector2.zero;
        rollVelocity = 0f;
        keyboardRollVelocity = 0f; // Reset keyboard roll momentum
        smoothedInput = Vector2.zero;
        inputVelocity = Vector2.zero;
        isFlickBurstActive = false;
        lastFlickDirection = Vector2.zero;
        
        // Force grounded state
        _trickState = TrickSystemState.Grounded;
        _stateEnterTime = Time.time;
        
        _emergencyResetCount++;
        _lastEmergencyResetTime = Time.time;
        
        Debug.Log($"[EMERGENCY] Upright complete! Total emergency resets: {_emergencyResetCount}");
    }
    
    /// <summary>
    /// PHASE 1: Emergency reset - complete system reset
    /// </summary>
    private void EmergencyReset()
    {
        Debug.LogError("[EMERGENCY] FULL SYSTEM RESET TRIGGERED!");
        
        // Force upright
        EmergencyUpright();
        
        // Additional cleanup
        wasAirborneLastFrame = false;
        wasReconciling = false;
        airborneStartTime = 0f;
        
        Debug.LogError($"[EMERGENCY] Full reset complete! State: {_trickState}");
    }
    
    /// <summary>
    /// PUBLIC API: Force reset trick system for self-revive (prevents trick mode activation bug)
    /// Called by PlayerHealth before re-enabling camera
    /// </summary>
    public void ForceResetTrickSystemForRevive()
    {
        Debug.Log("[AAACameraController] ForceResetTrickSystemForRevive - Resetting trick system for self-revive");
        
        // Force grounded state to prevent trick mode activation
        _trickState = TrickSystemState.Grounded;
        _previousTrickState = TrickSystemState.Grounded;
        _stateEnterTime = Time.time;
        
        // Reset all trick mode flags (including new state variables)
        isFreestyleModeActive = false;
        isReconciling = false;
        isInLandingGrace = false;
        reconciliationProgress = 0f;
        wasReconciling = false;
        wasAirborneLastFrame = false; // CRITICAL: Prevent false airborne detection
        
        // Reset rotation states
        // 🎯 PRESERVE YAW during revive
        float targetYaw = currentLook.x;
        freestyleRotation = Quaternion.Euler(currentLook.y, targetYaw, 0f);
        totalRotationX = 0f;
        totalRotationY = 0f;
        totalRotationZ = 0f;
        freestyleLookInput = Vector2.zero;
        freestyleLookVelocity = Vector2.zero;
        
        // 🎪 RESET MOMENTUM PHYSICS SYSTEM
        angularVelocity = Vector2.zero;
        rollVelocity = 0f;
        keyboardRollVelocity = 0f; // Reset keyboard roll momentum
        smoothedInput = Vector2.zero;
        inputVelocity = Vector2.zero;
        isFlickBurstActive = false;
        lastFlickDirection = Vector2.zero;
        
        // Reset button states
        isHoldingScrollWheelButton = false;
        trickJumpTriggered = false;
        
        // Reset FOV to normal
        freestyleFOV = baseFOV;
        
        // Force normal time if time dilation was active
        if (timeDilationManager != null && wasTimeDilationRequested)
        {
            timeDilationManager.ForceNormalTime();
            wasTimeDilationRequested = false;
        }
        
        Debug.Log("[AAACameraController] ✅ Trick system reset complete - ready for normal gameplay");
    }
    
    // ========================================
    // 🎪 AERIAL FREESTYLE TRICK SYSTEM
    // THE MOST REVOLUTIONARY CAMERA MECHANIC EVER CREATED
    // ========================================
    
    /// <summary>
    /// Main update loop for aerial freestyle trick system
    /// </summary>
    private void UpdateAerialFreestyleSystem()
    {
        if (!enableAerialFreestyle) return;
        
        bool isAirborne = !movementController.IsGrounded;
        float airTime = isAirborne ? Time.time - airborneStartTime : 0f;
        
        // Track when we become airborne
        if (isAirborne && !wasAirborneLastFrame)
        {
            airborneStartTime = Time.time;
        }
        
        // 🎮 PRO JUMP MECHANICS FOR TRICK SYSTEM (hold = higher jump, tap = small jump)
        // Jump IMMEDIATELY on press, cut if released early (exactly like spacebar!)
        if (middleClickTrickJump)
        {
            // Button pressed - JUMP IMMEDIATELY!
            if (Input.GetMouseButtonDown(2))
            {
                // CRITICAL FIX: Only trigger jump if grounded (prevents mid-air jump spam)
                if (!isAirborne)
                {
                    TriggerTrickJump();
                    isHoldingScrollWheelButton = true;
                    scrollWheelButtonPressTime = Time.time;
                    
                    // Immediately activate freestyle (don't wait for min air time)
                    EnterFreestyleMode();
                    Debug.Log("🎮 [TRICK JUMP] Jump triggered on PRESS! Hold to go higher, release early for small jump.");
                }
                // If already airborne, ONLY activate freestyle (no jump)
                else if (!isFreestyleModeActive)
                {
                    EnterFreestyleMode();
                    Debug.Log("🎮 [TRICK JUMP] Already airborne - Freestyle activated (no jump)!");
                }
                else
                {
                    Debug.Log("🎮 [TRICK JUMP] Already in freestyle mode - ignoring input");
                }
            }
            
            // Button released - cut jump if released early (like spacebar!) AND ramp out of slow-mo
            if (Input.GetMouseButtonUp(2) && isHoldingScrollWheelButton)
            {
                float holdDuration = Time.time - scrollWheelButtonPressTime;
                isHoldingScrollWheelButton = false;
                
                // 🎮 VARIABLE JUMP HEIGHT: If released early while still going up, cut the jump!
                // This is EXACTLY how spacebar works - release early = small jump
                if (movementController != null && movementController.Velocity.y > 0f)
                {
                    // Still going up - apply jump cut!
                    StartCoroutine(ApplyTrickJumpCut());
                    Debug.Log($"🎮 [TRICK JUMP] Released early ({holdDuration:F2}s) while rising - Jump cut applied!");
                }
                else
                {
                    Debug.Log($"🎮 [TRICK JUMP] Released after {holdDuration:F2}s (already falling or at peak)");
                }
                
                // 🎬 SLOW-MO: Releasing button starts ramping out of slow-mo
                Debug.Log("🎬 [TIME DILATION] Scroll button released - Ramping out of slow-mo");
            }
        }
        
        // DISABLED: LEFT ALT legacy system - middle click is the new way!
        // The middle click system keeps freestyle active until landing
        // No need for hold-to-activate anymore
        
        // Optional: LEFT ALT can still activate if you want (but won't deactivate)
        // bool freestyleInput = Input.GetKey(KeyCode.LeftAlt);
        // if (isAirborne && airTime >= minAirTimeForTricks && freestyleInput && !isFreestyleModeActive)
        // {
        //     EnterFreestyleMode();
        // }
        
        // Handle landing
        if (!isAirborne && wasAirborneLastFrame && isFreestyleModeActive)
        {
            LandDuringFreestyle();
        }
        
        // CRITICAL: Update reconciliation if active (continues AFTER landing for smooth transition)
        if (isReconciling)
        {
            UpdateLandingReconciliation();
        }
        // EDGE CASE: If we're grounded and reconciling just finished, ensure smooth handoff
        else if (!isAirborne && !isFreestyleModeActive && wasReconciling)
        {
            // Just finished reconciling - ensure camera is synced with normal rotation
            // 🎯 PRESERVE YAW - use currentLook.x (player's horizontal look direction)
            float totalPitch = currentLook.y + landingTiltOffset + wallJumpPitchAmount;
            float targetYaw = currentLook.x; // PRESERVE PLAYER'S HORIZONTAL LOOK DIRECTION
            float totalRollTilt = currentTilt + wallJumpTiltAmount + dynamicWallTilt;
            freestyleRotation = Quaternion.Euler(totalPitch, targetYaw, totalRollTilt);
            wasReconciling = false;
        }
        
        // 🎬 ULTRA COOL: Handle trick FOV boost - synced with time dilation
        if (isFreestyleModeActive)
        {
            // Boost FOV during tricks - synced with time dilation using unscaledDeltaTime
            freestyleFOV = Mathf.Lerp(freestyleFOV, baseFOV + trickFOVBoost, trickFOVSpeed * Time.unscaledDeltaTime);
            targetFOV = freestyleFOV; // Override targetFOV while tricking
        }
        else
        {
            // Reset freestyle FOV when not tricking (but don't override targetFOV - let sprint system control it)
            if (freestyleFOV > baseFOV + 0.1f)
            {
                // Smoothly return freestyle FOV to base
                freestyleFOV = Mathf.Lerp(freestyleFOV, baseFOV, trickFOVSpeed * 0.5f * Time.unscaledDeltaTime);
            }
            else
            {
                // Fully reset when close enough
                freestyleFOV = baseFOV;
            }
            // Don't set targetFOV here - let sprint system control it
        }
        
        // Track state changes for edge case handling
        wasReconciling = isReconciling;
        wasAirborneLastFrame = isAirborne;
    }
    
    /// <summary>
    /// Trigger a trick jump using the existing system
    /// 🎪 Jump height control happens via ApplyTrickJumpCut() coroutine
    /// </summary>
    private void TriggerTrickJump()
    {
        if (movementController != null)
        {
            movementController.TriggerTrickJumpFromExternalSystem();
            Debug.Log("🎮 [TRICK JUMP] Trick jump triggered!");
        }
        else
        {
            Debug.LogWarning("🎮 [TRICK JUMP] AAAMovementController not found! Cannot trigger jump.");
        }
    }
    
    /// <summary>
    /// Apply jump cut for variable height (mimics releasing spacebar early)
    /// Waits one frame for jump velocity to be applied, then cuts it
    /// </summary>
    private System.Collections.IEnumerator ApplyTrickJumpCut()
    {
        // Wait one frame for jump to be applied
        yield return null;
        
        // Cut the jump velocity (same as releasing spacebar early)
        if (movementController != null)
        {
            // Access velocity through reflection or public property
            var velocityField = typeof(AAAMovementController).GetField("velocity", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (velocityField != null)
            {
                Vector3 currentVelocity = (Vector3)velocityField.GetValue(movementController);
                
                // Apply jump cut (0.5x multiplier - same as spacebar release)
                currentVelocity.y *= 0.5f;
                velocityField.SetValue(movementController, currentVelocity);
                
                Debug.Log($"🎮 [TRICK JUMP] Jump cut applied! New Y velocity: {currentVelocity.y:F1}");
            }
            else
            {
                Debug.LogWarning("🎮 [TRICK JUMP] Could not access velocity field for jump cut!");
            }
        }
    }
    
    /// <summary>
    /// Enter freestyle mode - camera becomes independent from body
    /// SKATE-STYLE: Instant burst on activation for flick-it feel
    /// 🤝 BFFL: Clears reconciliation states for clean trick restart
    /// </summary>
    private void EnterFreestyleMode()
    {
        // Track if we're transitioning from wall jump
        bool isTransitioningFromWallJump = (Time.time - wallJumpTiltStartTime) < 0.5f;
        
        isFreestyleModeActive = true;
        freestyleModeStartTime = Time.time;
        isInInitialBurst = true;
        
        // 🤝 BFFL: Clear reconciliation states when entering freestyle
        // This ensures clean transitions from wall jump → new trick
        isReconciling = false;
        isInLandingGrace = false;
        
        // 🔥 COMBO SYSTEM INTEGRATION: Register trick start
        if (ComboMultiplierSystem.Instance != null)
        {
            bool isAirborne = movementController != null && !movementController.IsGrounded;
            // Use trickAwesomeness = 1.0 for starting a trick (will get more points on landing)
            ComboMultiplierSystem.Instance.AddTrick(1.0f, isAirborne);
            
            // Extra feedback for seamless transitions
            if (isTransitioningFromWallJump)
            {
                Debug.Log("🤝✨ [COMBO] SEAMLESS WallJump→Trick transition detected!");
            }
        }
        
        // Initialize freestyle rotation to current camera orientation
        freestyleRotation = transform.localRotation;
        
        // Reset rotation tracking
        totalRotationX = 0f;
        totalRotationY = 0f;
        totalRotationZ = 0f;
        freestyleLookInput = Vector2.zero;
        freestyleLookVelocity = Vector2.zero;
        lastRawInput = Vector2.zero;
        currentRotationSpeedMultiplier = initialFlipBurstMultiplier; // START FAST!
        
        // 🎪 RESET MOMENTUM PHYSICS SYSTEM
        angularVelocity = Vector2.zero;
        rollVelocity = 0f;
        keyboardRollVelocity = 0f; // Reset keyboard roll momentum
        smoothedInput = Vector2.zero;
        inputVelocity = Vector2.zero;
        isFlickBurstActive = false;
        lastFlickDirection = Vector2.zero;
        
        // 🔥 PLAY EPIC TRICK START SOUND (bass thump slide down with slow-motion!)
        GeminiGauntlet.Audio.GameSounds.PlayTrickStartSound(transform.position, 1.0f);
        
        Debug.Log($"🎪 [FREESTYLE] TRICK MODE ACTIVATED! Initial burst: {initialFlipBurstMultiplier}x speed!");
    }
    
    /// <summary>
    /// Exit freestyle mode smoothly (released alt in air)
    /// </summary>
    private void ExitFreestyleMode()
    {
        isFreestyleModeActive = false;
        
        // Smoothly blend back to normal camera orientation
        // We'll let the normal camera system take over gradually
        
        Debug.Log($"🎪 [FREESTYLE] Exited - Rotations: X={totalRotationX:F0}° Y={totalRotationY:F0}° Z={totalRotationZ:F0}°");
    }
    
    /// <summary>
    /// Handle landing while in freestyle mode - THE DRAMATIC MOMENT
    /// SEQUENTIAL PHASES: Grace period → Reconciliation → Normal control
    /// </summary>
    private void LandDuringFreestyle()
    {
        isFreestyleModeActive = false;
        isReconciling = true;
        isInLandingGrace = true; // Start grace period
        landingTime = Time.time;
        reconciliationStartTime = Time.time; // Will be updated after grace period
        reconciliationStartRotation = freestyleRotation;
        reconciliationProgress = 0f;
        
        // 🎪 FREEZE MOMENTUM ON LANDING (stop the spin)
        angularVelocity = Vector2.zero;
        rollVelocity = 0f;
        smoothedInput = Vector2.zero;
        inputVelocity = Vector2.zero;
        isFlickBurstActive = false;
        
        // Reset button hold state on landing
        isHoldingScrollWheelButton = false;
        
        // 🎯 CORRECT DEVIATION CALCULATION
        // Calculate the ACTUAL angle between current freestyle rotation and UPRIGHT target
        // Target = UPRIGHT orientation (player standing, looking forward/wherever they're facing)
        Vector3 freestyleEuler = freestyleRotation.eulerAngles;
        float normalizedYaw = NormalizeAngle(freestyleEuler.y);
        
        // Target is UPRIGHT - pitch=0 (head up), yaw=preserved (direction), roll=0 (no tilt)
        // If you land upside down (pitch=180°), deviation will be ~180° = FAIL
        Quaternion targetRotation = Quaternion.Euler(0f, normalizedYaw, 0f);
        
        // This is the REAL deviation - the actual angle we'll reconcile through
        float totalDeviation = Quaternion.Angle(freestyleRotation, targetRotation);
        
        bool isCleanLanding = totalDeviation < cleanLandingThreshold;
        
        if (isCleanLanding)
        {
            // CLEAN LANDING - Smooth transition
            Debug.Log($"✨ [FREESTYLE] CLEAN LANDING! Deviation: {totalDeviation:F1}° - Smooth recovery");
            AddTrauma(0.1f); // Tiny trauma for impact feel
        }
        else
        {
            // FAILED LANDING - Camera crashes to reality!
            Debug.Log($"💥 [FREESTYLE] CRASH LANDING! Deviation: {totalDeviation:F1}° - Reality check!");
            
            // Scale trauma based on how inverted we were
            float traumaAmount = Mathf.Lerp(failedLandingTrauma * 0.5f, failedLandingTrauma, totalDeviation / 180f);
            AddTrauma(traumaAmount);
        }
        
        Debug.Log($"🎪 [FREESTYLE] LANDED - Total flips: X={totalRotationX/360f:F1} Y={totalRotationY/360f:F1} Z={totalRotationZ/360f:F1} - Grace period: {landingGracePeriod:F2}s");
        
        // 🎪 AERIAL TRICK XP SYSTEM - Reward the player for sick tricks!
        if (AerialTrickXPSystem.Instance != null)
        {
            // Calculate airtime
            float airtime = Time.time - airborneStartTime;
            
            // Get rotations
            Vector3 rotations = new Vector3(totalRotationX, totalRotationY, totalRotationZ);
            
            // Get landing position
            Vector3 landingPosition = transform.position;
            
            // Award XP!
            AerialTrickXPSystem.Instance.OnTrickLanded(airtime, rotations, landingPosition, isCleanLanding);
        }
    }
    
    /// <summary>
    /// Update the landing reconciliation - INDUSTRY STANDARD TIME-NORMALIZED BLEND
    /// BULLETPROOF IMPLEMENTATION:
    /// - Fixed duration (frame-rate independent)
    /// - Player can interrupt (player-first)
    /// - Animation curve easing (cinematic)
    /// - Grace period (reaction time)
    /// - Sequential phases (cognitive load reduction)
    /// 🤝 BFFL: Wall jump can instantly cancel reconciliation for seamless combo flow!
    /// </summary>
    private void UpdateLandingReconciliation()
    {
        // 🤝 BFFL CHECK: Wall jump might have cancelled us - early exit if so
        if (!isReconciling && !isInLandingGrace)
        {
            return; // Wall jump rescued us! Let it take over.
        }
        
        // CRITICAL: Only reconcile if we're grounded (not while approaching!)
        bool isGrounded = movementController != null && movementController.IsGrounded;
        
        if (!isGrounded)
        {
            // Still airborne - maintain freestyle rotation, don't reset yet!
            return;
        }
        
        // === PHASE 1: LANDING GRACE PERIOD ===
        // Give player a moment to register landing before camera starts moving
        if (isInLandingGrace)
        {
            float graceDuration = Time.time - landingTime;
            
            if (graceDuration < landingGracePeriod)
            {
                // Still in grace period - freeze camera, let player register landing
                return;
            }
            else
            {
                // Grace period over - start reconciliation
                isInLandingGrace = false;
                reconciliationStartTime = Time.time;
                reconciliationProgress = 0f;
                
                // Capture starting and target rotations
                reconciliationStartRotation = freestyleRotation;
                
                // Calculate target rotation (normal camera orientation)
                // 🎯 CRITICAL FIX: Target is CLEAN body-relative orientation
                // Extract pitch/yaw from current freestyle rotation, ZERO out roll
                // This prevents accumulated wall jump tilts from polluting the reconciliation
                Vector3 freestyleEuler = freestyleRotation.eulerAngles;
                float normalizedPitch = NormalizeAngle(freestyleEuler.x);
                float normalizedYaw = NormalizeAngle(freestyleEuler.y);
                
                reconciliationTargetRotation = Quaternion.Euler(normalizedPitch, normalizedYaw, 0f);
                
                Debug.Log($"🎯 [RECONCILIATION] Starting - Duration: {landingReconciliationDuration:F2}s, Angle: {Quaternion.Angle(reconciliationStartRotation, reconciliationTargetRotation):F1}°, Target Yaw: {normalizedYaw:F1}°");
            }
        }
        
        // === PHASE 2: CHECK FOR PLAYER INTERRUPT ===
        // Player can cancel reconciliation by moving mouse (player-first philosophy)
        if (allowPlayerCancelReconciliation)
        {
            Vector2 rawInput = new Vector2(
                Input.GetAxis("Mouse X"),
                Input.GetAxis("Mouse Y")
            );
            
            // Check if input exceeds deadzone (prevents sensor noise from canceling)
            if (rawInput.magnitude > mouseInputDeadzone)
            {
                // Player is trying to look - CANCEL reconciliation, return control
                isReconciling = false;
                isInLandingGrace = false;
                
                // Sync freestyle rotation to current normal rotation for smooth handoff
                // 🎯 CRITICAL: Preserve current view direction, zero roll
                Vector3 currentEuler = freestyleRotation.eulerAngles;
                float normalizedPitch = NormalizeAngle(currentEuler.x);
                float normalizedYaw = NormalizeAngle(currentEuler.y);
                
                freestyleRotation = Quaternion.Euler(normalizedPitch, normalizedYaw, 0f);
                
                Debug.Log("✋ [RECONCILIATION] Cancelled by player input - control restored");
                return;
            }
        }
        
        // === PHASE 3: TIME-NORMALIZED RECONCILIATION ===
        // Progress from 0 to 1 over fixed duration (frame-rate independent)
        reconciliationProgress += Time.deltaTime / landingReconciliationDuration;
        reconciliationProgress = Mathf.Clamp01(reconciliationProgress);
        
        // Apply animation curve for cinematic easing
        float curvedProgress = reconciliationCurve.Evaluate(reconciliationProgress);
        
        // Blend using time-normalized progress (NOT frame-rate dependent)
        freestyleRotation = Quaternion.Slerp(
            reconciliationStartRotation,
            reconciliationTargetRotation,
            curvedProgress
        );
        
        // === PHASE 4: COMPLETION CHECK ===
        if (reconciliationProgress >= 1.0f)
        {
            // Reconciliation complete - snap to final rotation and exit
            freestyleRotation = reconciliationTargetRotation;
            isReconciling = false;
            
            float totalDuration = Time.time - (landingTime + landingGracePeriod);
            Debug.Log($"✅ [RECONCILIATION] Complete - Total time: {totalDuration:F2}s (grace: {landingGracePeriod:F2}s + blend: {landingReconciliationDuration:F2}s)");
        }
    }
    
    /// <summary>
    /// Handle mouse input during freestyle mode - 🎪 MOMENTUM PHYSICS SYSTEM (THE GEM)
    /// Skate game feel: Flick and let it spin, with realistic physics
    /// Features:
    /// - Velocity-based rotation (not direct control)
    /// - Flick burst for initial impact (Skate-style)
    /// - Angular drag for realistic slowdown
    /// - Counter-rotation support (fight momentum)
    /// - 3-axis control (pitch, yaw, roll for varial flips)
    /// - Time dilation compensation
    /// </summary>
    private void HandleFreestyleLookInput()
    {
        if (!isFreestyleModeActive) return;
        
        // Get raw mouse input
        Vector2 rawInput = new Vector2(
            Input.GetAxis("Mouse X"),
            Input.GetAxis("Mouse Y")
        );
        
        // TIME DILATION COMPENSATION: Scale input to maintain consistent feel
        float timeCompensation = Mathf.Max(0.1f, Time.timeScale);
        
        // Apply sensitivity with time compensation
        Vector2 trickInput = rawInput * trickInputSensitivity * mouseSensitivity * timeCompensation;
        
        // Invert Y if needed
        if (invertY)
            trickInput.y = -trickInput.y;
        
        // ========================================
        // 🎪 MOMENTUM PHYSICS SYSTEM
        // ========================================
        
        if (enableMomentumPhysics)
        {
            // Smooth input with minimal delay (just noise filtering)
            smoothedInput = Vector2.SmoothDamp(
                smoothedInput,
                trickInput,
                ref inputVelocity,
                flickInputSmoothing
            );
            
            // === FLICK BURST SYSTEM (Skate-style initial impact) ===
            float inputMagnitude = smoothedInput.magnitude;
            bool hasSignificantInput = inputMagnitude > minInputThreshold;
            
            // Detect new flick (input after stillness)
            if (hasSignificantInput && lastRawInput.magnitude <= minInputThreshold)
            {
                // NEW FLICK DETECTED!
                isFlickBurstActive = true;
                flickBurstStartTime = Time.time;
                lastFlickDirection = smoothedInput.normalized;
                Debug.Log($"🎪 [FLICK] Burst activated! Magnitude: {inputMagnitude:F2}");
            }
            
            // Calculate flick multiplier (burst phase)
            float flickMultiplier = 1f;
            if (isFlickBurstActive)
            {
                float burstElapsed = Time.time - flickBurstStartTime;
                if (burstElapsed < flickBurstDuration)
                {
                    // Burst active - extra power!
                    float burstProgress = burstElapsed / flickBurstDuration;
                    flickMultiplier = Mathf.Lerp(flickBurstMultiplier, 1f, burstProgress);
                }
                else
                {
                    // Burst expired
                    isFlickBurstActive = false;
                }
            }
            
            // === FORCE-BASED INPUT (Add to velocity, not direct control) ===
            Vector2 inputForce = smoothedInput * angularAcceleration * flickMultiplier;
            
            // Apply force to velocity
            angularVelocity += inputForce * Time.unscaledDeltaTime;
            
            // === ANGULAR DRAG (Realistic slowdown when no input) ===
            if (!hasSignificantInput || inputMagnitude < 0.1f)
            {
                // No significant input - apply drag
                angularVelocity -= angularVelocity * angularDrag * Time.unscaledDeltaTime;
            }
            else
            {
                // Has input - check if counter-rotating (fighting momentum)
                Vector2 velocityDir = angularVelocity.normalized;
                Vector2 inputDir = smoothedInput.normalized;
                float alignment = Vector2.Dot(velocityDir, inputDir);
                
                if (alignment < -0.3f) // Counter-rotating
                {
                    // Apply extra drag for responsive counter-rotation
                    float counterDrag = angularDrag * (1f + counterRotationStrength * 2f);
                    angularVelocity -= angularVelocity * counterDrag * Time.unscaledDeltaTime;
                }
            }
            
            // === CLAMP MAX VELOCITY ===
            angularVelocity = Vector2.ClampMagnitude(angularVelocity, maxAngularVelocity);
            
            // === CALCULATE ROTATION DELTAS FROM VELOCITY ===
            float pitchDelta = -angularVelocity.y * Time.unscaledDeltaTime;
            float yawDelta = angularVelocity.x * Time.unscaledDeltaTime;
            
            // === 🌪️ DIAGONAL ROLL (VARIAL FLIP SYSTEM) ===
            float rollDelta = 0f;
            if (enableDiagonalRoll && hasSignificantInput)
            {
                // Calculate diagonal strength (both X and Y input = diagonal)
                float diagonalStrength = Mathf.Abs(smoothedInput.x * smoothedInput.y);
                
                // Roll direction based on diagonal quadrant
                float rollDirection = Mathf.Sign(smoothedInput.x * smoothedInput.y);
                
                // Calculate roll velocity from diagonal input
                float rollAccel = diagonalStrength * angularAcceleration * rollStrength * rollDirection;
                rollVelocity += rollAccel * Time.unscaledDeltaTime;
                
                // Apply drag to roll velocity
                rollVelocity -= rollVelocity * angularDrag * Time.unscaledDeltaTime;
                
                // Clamp roll velocity
                rollVelocity = Mathf.Clamp(rollVelocity, -maxAngularVelocity * 0.5f, maxAngularVelocity * 0.5f);
                
                // Calculate roll delta
                rollDelta = rollVelocity * Time.unscaledDeltaTime;
                
                // Track for stats
                totalRotationZ += rollDelta;
            }
            else
            {
                // No diagonal input - decay roll velocity
                rollVelocity -= rollVelocity * angularDrag * 2f * Time.unscaledDeltaTime;
                rollDelta = rollVelocity * Time.unscaledDeltaTime;
            }
            
            // === 🎮 KEYBOARD ROLL CONTROLS (Q/E for corrections - SMOOTH MOMENTUM) ===
            if (enableKeyboardRoll)
            {
                float keyboardRollInput = 0f;
                if (Input.GetKey(rollLeftKey))
                {
                    keyboardRollInput = -1f; // Q = Roll left (negative)
                }
                else if (Input.GetKey(rollRightKey))
                {
                    keyboardRollInput = 1f; // E = Roll right (positive)
                }
                
                if (Mathf.Abs(keyboardRollInput) > 0.01f)
                {
                    // Smooth acceleration curve - starts slow, builds momentum
                    // Acceleration is calculated to reach max speed over keyboardRollAccelTime
                    float acceleration = keyboardRollMaxSpeed / keyboardRollAccelTime;
                    float targetDirection = Mathf.Sign(keyboardRollInput);
                    
                    // Accelerate toward target direction
                    keyboardRollVelocity += targetDirection * acceleration * Time.unscaledDeltaTime;
                    
                    // Clamp to max velocity
                    keyboardRollVelocity = Mathf.Clamp(keyboardRollVelocity, -keyboardRollMaxSpeed, keyboardRollMaxSpeed);
                }
                else
                {
                    // Natural momentum fade-out when released (preserves spin but gradually decays)
                    // Exponential decay - feels organic and never abruptly stops
                    float decayAmount = Mathf.Abs(keyboardRollVelocity) * keyboardRollDecayRate;
                    keyboardRollVelocity -= Mathf.Sign(keyboardRollVelocity) * decayAmount * Time.unscaledDeltaTime * 60f;
                    
                    // Only snap to zero when EXTREMELY slow (imperceptible)
                    if (Mathf.Abs(keyboardRollVelocity) < 0.1f)
                    {
                        keyboardRollVelocity = 0f;
                    }
                }
                
                // Apply velocity to rotation (smooth momentum)
                if (Mathf.Abs(keyboardRollVelocity) > 0.1f)
                {
                    float keyboardRollDelta = keyboardRollVelocity * Time.unscaledDeltaTime;
                    rollDelta += keyboardRollDelta; // Add to existing roll from diagonal input
                    totalRotationZ += keyboardRollDelta;
                    
                    if (Mathf.Abs(keyboardRollInput) > 0.01f) // Only log when actively pressing
                    {
                        Debug.Log($"🎮 [KEYBOARD ROLL] {(keyboardRollInput < 0 ? "LEFT (Q)" : "RIGHT (E)")} - Velocity: {keyboardRollVelocity:F1}°/s");
                    }
                }
            }
            
            // === TRACK TOTAL ROTATIONS FOR STATS ===
            totalRotationX += pitchDelta;
            totalRotationY += yawDelta;
            
            // === APPLY ROTATIONS IN LOCAL SPACE (3-AXIS CONTROL) ===
            Quaternion pitchRotation = Quaternion.AngleAxis(pitchDelta, Vector3.right);
            Quaternion yawRotation = Quaternion.AngleAxis(yawDelta, Vector3.up);
            Quaternion rollRotation = Quaternion.AngleAxis(rollDelta, Vector3.forward);
            
            // Combine all three axes
            freestyleRotation = freestyleRotation * pitchRotation * yawRotation * rollRotation;
            
            // === TRACK ROTATION SPEED (for motion blur) ===
            lastRotationSpeed = (Mathf.Abs(pitchDelta) + Mathf.Abs(yawDelta) + Mathf.Abs(rollDelta)) / Time.unscaledDeltaTime;
            
            // Debug visualization
            if (inputMagnitude > 0.1f || angularVelocity.magnitude > 10f)
            {
                Debug.Log($"🎪 [MOMENTUM] Input: {inputMagnitude:F2} | Velocity: {angularVelocity.magnitude:F1}°/s | Roll: {rollVelocity:F1}°/s | Speed: {lastRotationSpeed:F1}°/s");
            }
        }
        else
        {
            // === LEGACY DIRECT CONTROL (Fallback if momentum disabled) ===
            float responsiveSmoothing = Mathf.Min(trickRotationSmoothing, 0.1f);
            
            freestyleLookInput = Vector2.SmoothDamp(
                freestyleLookInput,
                trickInput,
                ref freestyleLookVelocity,
                responsiveSmoothing
            );
            
            float pitchDelta = -freestyleLookInput.y;
            float yawDelta = freestyleLookInput.x;
            
            float maxDelta = maxTrickRotationSpeed * Time.unscaledDeltaTime;
            pitchDelta = Mathf.Clamp(pitchDelta, -maxDelta, maxDelta);
            yawDelta = Mathf.Clamp(yawDelta, -maxDelta, maxDelta);
            
            totalRotationX += pitchDelta;
            totalRotationY += yawDelta;
            
            Quaternion pitchRotation = Quaternion.AngleAxis(pitchDelta, Vector3.right);
            Quaternion yawRotation = Quaternion.AngleAxis(yawDelta, Vector3.up);
            
            freestyleRotation = freestyleRotation * pitchRotation * yawRotation;
            
            lastRotationSpeed = (Mathf.Abs(pitchDelta) + Mathf.Abs(yawDelta)) / Time.unscaledDeltaTime;
        }
        
        // CRITICAL: Normalize every frame to prevent quaternion drift
        freestyleRotation = Quaternion.Normalize(freestyleRotation);
        
        // Store raw input for next frame
        lastRawInput = rawInput;
    }
    
    /// <summary>
    /// PUBLIC API: Check if player is currently performing aerial tricks
    /// </summary>
    public bool IsPerformingAerialTricks => isFreestyleModeActive;
    
    /// <summary>
    /// PUBLIC API: Get current rotation stats for UI display
    /// </summary>
    public Vector3 GetTrickRotations()
    {
        return new Vector3(totalRotationX, totalRotationY, totalRotationZ);
    }
    
    /// <summary>
    /// PUBLIC API: Get current wall jump tilt amount (for CognitiveFeedbackManager)
    /// COHERENCE: Eliminates reflection anti-pattern
    /// </summary>
    public float WallJumpTiltAmount => wallJumpTiltAmount;
    
    /// <summary>
    /// PUBLIC API: Get rotation speed for motion blur intensity
    /// </summary>
    public float GetTrickRotationSpeed => lastRotationSpeed;
    
    /// <summary>
    /// PUBLIC API: Get current analog speed multiplier (for UI feedback)
    /// </summary>
    public float GetCurrentSpeedMultiplier => currentRotationSpeedMultiplier;
    
    /// <summary>
    /// PUBLIC API: Check if in initial burst phase (for visual effects)
    /// </summary>
    public bool IsInInitialBurst => isInInitialBurst;
    
    // ========================================
    // 🎬 TIME DILATION SYSTEM
    // ========================================
    
    /// <summary>
    /// Update time dilation effect - smooth ramp in/out of slow-mo
    /// NOW USES CENTRALIZED TimeDilationManager for performance and compatibility
    /// </summary>
    private void UpdateTimeDilation()
    {
        if (!enableTimeDilation) 
        {
            // Ensure time dilation is disabled
            if (wasTimeDilationRequested && timeDilationManager != null)
            {
                timeDilationManager.SetTrickSystemDilation(false);
                wasTimeDilationRequested = false;
            }
            return;
        }
        
        // Initialize manager reference if needed
        if (timeDilationManager == null)
        {
            timeDilationManager = TimeDilationManager.Instance;
            
            // If no manager exists, create one
            if (timeDilationManager == null)
            {
                GameObject managerObj = new GameObject("TimeDilationManager");
                timeDilationManager = managerObj.AddComponent<TimeDilationManager>();
                Debug.Log("🎬 [AAACameraController] Created TimeDilationManager automatically");
            }
        }
        
        // Check if we should be in slow-mo (ONLY while holding scroll button AND airborne)
        bool isAirborne = movementController != null && !movementController.IsGrounded;
        bool shouldBeDilated = isFreestyleModeActive && isHoldingScrollWheelButton && isAirborne;
        
        // Check distance to ground for landing anticipation (faster ramp out)
        float distanceToGround = GetDistanceToGround();
        bool isApproachingLanding = distanceToGround > 0 && distanceToGround < landingAnticipationDistance;
        
        // Calculate transition speed based on state
        float transitionSpeed = 0f;
        if (shouldBeDilated && !isApproachingLanding)
        {
            // Ramping IN - use slow ramp in speed
            transitionSpeed = 1f / timeDilationRampIn;
        }
        else if (isApproachingLanding || !shouldBeDilated)
        {
            // Ramping OUT - use fast ramp out speed
            transitionSpeed = 1f / timeDilationRampOut;
        }
        
        // Request time dilation from manager
        if (shouldBeDilated != wasTimeDilationRequested)
        {
            timeDilationManager.SetTrickSystemDilation(shouldBeDilated, trickTimeScale, transitionSpeed);
            wasTimeDilationRequested = shouldBeDilated;
            
            if (shouldBeDilated)
            {
                Debug.Log($"🎬 [TIME DILATION] Trick slow-mo ACTIVATED ({trickTimeScale:F2}x speed)");
            }
            else
            {
                Debug.Log("🎬 [TIME DILATION] Trick slow-mo DEACTIVATED");
            }
        }
        // Update transition speed if approaching landing (dynamic speed change)
        else if (shouldBeDilated && isApproachingLanding)
        {
            // Switch to faster ramp out when approaching ground
            timeDilationManager.SetTrickSystemDilation(false, trickTimeScale, transitionSpeed);
            wasTimeDilationRequested = false;
        }
    }
    
    /// <summary>
    /// Get distance to ground using raycast
    /// </summary>
    private float GetDistanceToGround()
    {
        if (movementController == null) return -1f;
        
        RaycastHit hit;
        Vector3 origin = transform.position;
        
        if (Physics.Raycast(origin, Vector3.down, out hit, 100f))
        {
            return hit.distance;
        }
        
        return -1f; // No ground detected
    }
    
    // ========================================
    // PUBLIC API FOR EXTERNAL SYSTEMS
    // ========================================
    
    /// <summary>
    /// Check if player is currently performing aerial tricks
    /// </summary>
    public bool IsTrickActive => isFreestyleModeActive;
    
    /// <summary>
    /// Normalize euler angle to -180 to 180 range
    /// </summary>
    private float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
}
