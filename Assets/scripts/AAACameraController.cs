using UnityEngine;

/// <summary>
/// AAA-Quality FPS Camera Controller with smooth movement, dynamic FOV, and immersive effects
/// Features: Camera bob, smooth look, FOV changes, recoil system, and more!
/// 
///  BFFL INTEGRATION: Aerial Trick System â†” Wall Jump System
/// These systems are now best friends forever! Key behaviors:
/// - Aerial trick â†’ wall jump: Instantly cancels reconciliation for seamless combo flow
/// - Wall jump â†’ aerial trick: Clears reconciliation states for clean trick restart
/// - Both systems respect each other's camera control and transition smoothly
/// </summary>
public class AAACameraController : MonoBehaviour
{
    [Header("=== LOOK SETTINGS ===")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float verticalLookLimit = 90f;
    [SerializeField] private bool invertY = false;
    [SerializeField] private AnimationCurve sensitivityCurve = AnimationCurve.Linear(0, 1, 1, 1); // Flat curve for consistent feel
    [Tooltip("Horizontal rotation speed multiplier during aerial tricks (0.2 = 20% speed)")]
    [SerializeField] [Range(0.1f, 1f)] private float trickYawSensitivity = 0.3f; // Much slower horizontal during tricks
    
    [Header("=== SMOOTHING ===")]
    [Tooltip("Frame-based smoothing (0 = raw input, 0.1-0.3 = AAA feel, 0.5+ = sluggish)")]
    [SerializeField] [Range(0f, 0.5f)] private float lookSmoothing = 0.15f; // AAA-quality frame interpolation
    [SerializeField] private float positionSmoothing = 12f; // Increased for smoother position changes
    [SerializeField] private bool enableSmoothing = true;
    [SerializeField] private bool enableMotionPrediction = true; // NEW: Predict fast movements
    
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
    [Tooltip("Maximum tilt angle during wall jump (degrees) - AAA standard: 8-12Â°")]
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
    
    [Header("===  AERIAL FREESTYLE TRICK SYSTEM (REVOLUTIONARY) ===")]
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
    
    [Header("ðŸŽª MOMENTUM PHYSICS SYSTEM (SKATE GAME FEEL)")]
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
    [Tooltip("Camera return to neutral duration after landing (smooth pitch/roll reset)")]
    [SerializeField] private float cameraReturnDuration = 0.3f;
    [Tooltip("Easing curve for camera return (reused from old reconciliation system)")]
    [SerializeField] private AnimationCurve reconciliationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    // ðŸŽ¯ REMOVED: Grace period, mouse deadzone, cancel reconciliation - no longer needed!
    
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
    
    [Header("ðŸŽ¬ TIME DILATION (CINEMATIC SLOW-MO)")]
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
    
    [Header("ðŸ›¡ï¸ EMERGENCY RECOVERY SYSTEM (PHASE 1)")]
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
    
    //  CRITICAL FIX: Store base local position to prevent drift
    private Vector3 baseLocalPosition = Vector3.zero;
    
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
    
    //  AERIAL FREESTYLE TRICK SYSTEM - STATE MACHINE (SIMPLIFIED)
    /// <summary>
    /// Trick system states - SIMPLIFIED (reconciliation states removed!)
    ///  Player body now rotates with yaw, so no reconciliation needed!
    /// </summary>
    public enum TrickSystemState
    {
        Grounded,           // On ground, ready to jump
        JumpInitiated,      // Jump triggered, waiting for airborne confirmation
        Airborne,           // In air, tricks not yet active
        FreestyleActive,    // Performing tricks (camera independent for pitch/roll only!)
        LandingApproach     // Approaching ground (time dilation ramp out)
        //  REMOVED: Reconciling, TransitionCleanup - no longer needed!
    }
    
    private TrickSystemState _trickState = TrickSystemState.Grounded;
    private TrickSystemState _previousTrickState = TrickSystemState.Grounded;
    private float _stateEnterTime = 0f;
    
    // Legacy boolean flags (kept for backward compatibility, but state machine is source of truth)
    private bool isFreestyleModeActive = false;
    private bool wasAirborneLastFrame = false;
    //  RECONCILIATION REMOVED: wasReconciling variable deleted - no longer needed!
    private float airborneStartTime = 0f;
    private Quaternion freestyleRotation = Quaternion.identity; // Camera's independent rotation during tricks (pitch/roll only now!)
    
    //  PRO JUMP MECHANICS FOR TRICKS (hold = higher jump, tap = small jump)
    private bool isHoldingScrollWheelButton = false;
    private float scrollWheelButtonPressTime = 0f;
    private bool trickJumpTriggered = false; // Track if we triggered a trick jump this frame
    
    //  TIME DILATION STATE (Now managed by TimeDilationManager)
    private TimeDilationManager timeDilationManager;
    private bool wasTimeDilationRequested = false;
    private Quaternion freestyleRotationVelocity = Quaternion.identity; // For smooth damping
    private Vector2 freestyleLookInput = Vector2.zero; // Accumulated rotation input
    private Vector2 freestyleLookVelocity = Vector2.zero; // For smoothing
    private float freestyleFOV = 0f; // Current trick FOV
    private float totalRotationX = 0f; // Track total pitch rotation (backflips/frontflips)
    private float totalRotationY = 0f; // Track total yaw rotation (spins)
    private float totalRotationZ = 0f; // Track total roll rotation (barrel rolls)
    //  RECONCILIATION REMOVED: No longer needed - player body rotates with yaw!
    private float lastRotationSpeed = 0f; // For motion blur intensity
    
    //  SKATE-STYLE ENHANCEMENTS
    private float freestyleModeStartTime = 0f; // When freestyle mode was activated
    private float currentRotationSpeedMultiplier = 1f; // Analog speed control
    private Vector2 lastRawInput = Vector2.zero; // Track input magnitude for analog control
    private bool isInInitialBurst = false; // Are we in the flick-it burst phase?
    
    //  MOMENTUM PHYSICS SYSTEM (THE GEM - SKATE GAME FEEL)
    private Vector2 angularVelocity = Vector2.zero; // Persistent rotation velocity (PITCH ONLY - Y component)
    private bool isFlickBurstActive = false; // Flick burst for initial impact
    private float flickBurstStartTime = 0f; // When the flick burst started
    private Vector2 lastFlickDirection = Vector2.zero; // Track flick direction for burst
    private Vector2 smoothedInput = Vector2.zero; // Smoothed input for momentum system
    private Vector2 inputVelocity = Vector2.zero; // Velocity for input smoothing
    
    //  EMERGENCY RECOVERY SYSTEM (PHASE 1)
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
    
    //  AAA PAUSE DETECTION SYSTEM (Prevents camera floating during pause)
    private bool _isPaused = false;
    private const float PAUSE_DETECTION_THRESHOLD = 0.01f; // Time.timeScale below this = paused
    
    void Start()
    {
        playerCamera = GetComponent<Camera>();
        movementController = GetComponentInParent<AAAMovementController>();
        playerTransform = transform.parent;
        
        //  CRITICAL FIX: Store base local position for consistent offset application
        baseLocalPosition = transform.localPosition;
        Debug.Log($"[AAACameraController] Base local position stored: {baseLocalPosition}");
        
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
        
        //  UNIFIED IMPACT SYSTEM: Subscribe to impact events
        // This makes the camera listen to FallingDamageSystem instead of tracking falls independently
        ImpactEventBroadcaster.OnImpact += OnImpactReceived;
        Debug.Log("[AAACameraController] âœ… Subscribed to unified impact events");
    }
    
    void OnEnable()
    {
        // Ensure references
        if (playerCamera == null) playerCamera = GetComponent<Camera>();
        if (playerTransform == null) playerTransform = transform.parent;
        
        //  CRITICAL FIX: Store base local position when enabled
        baseLocalPosition = transform.localPosition;
        
        //  CRITICAL FIX: Restore FOV when re-enabled (prevents FOV reset when chest opens/closes)
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
        //  CRITICAL FIX: Store current FOV when disabled so we can restore it
        storedFOVBeforeDisable = currentFOV;
        Debug.Log($"[AAACameraController] OnDisable: Stored FOV {storedFOVBeforeDisable} for restoration");
        
        //  PHASE 1: Safety cleanup - ensure Time.timeScale is reset
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
        //  AAA PAUSE DETECTION: Early exit if game is paused (prevents camera floating)
        _isPaused = Time.timeScale < PAUSE_DETECTION_THRESHOLD;
        if (_isPaused)
        {
            // Game is paused - freeze all camera updates to prevent floating
            return;
        }
        
        //  PHASE 1: Emergency recovery FIRST (safety checks before everything)
        UpdateEmergencyRecovery();
        
        //  SMART FOV: Only update when transitioning, not every frame
        UpdateFOVTransition();
        UpdateMotionPrediction();
        
        //  TIME DILATION: Update slow-mo effect
        UpdateTimeDilation();
        
        //  AERIAL FREESTYLE TRICK SYSTEM
        UpdateAerialFreestyleSystem();
    }
    
    void LateUpdate()
    {
        //  AAA PAUSE DETECTION: Early exit if game is paused (prevents camera floating)
        // This is CRITICAL - without this check, camera effects continue running during pause
        if (_isPaused)
        {
            // Game is paused - freeze all camera updates including look input
            return;
        }
        
        // CRITICAL: Mouse look in LateUpdate for frame-perfect timing (AAA standard)
        // Handle normal mouse look (ALWAYS for yaw rotation)
        HandleLookInput();
        
        // During tricks, ALSO handle freestyle input (for pitch/backflips)
        if (isFreestyleModeActive)
        {
            HandleFreestyleLookInput(); // Adds pitch rotation during tricks
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
        // Get raw mouse input (Unity Input Manager now at 1.0 sensitivity)
        rawLookInput.x = Input.GetAxis("Mouse X");
        rawLookInput.y = Input.GetAxis("Mouse Y");
        
        // Apply base sensitivity
        lookInput = rawLookInput * mouseSensitivity;
        
        // Reduce horizontal sensitivity during tricks for better control
        if (isFreestyleModeActive)
        {
            lookInput.x *= trickYawSensitivity;
        }
        
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
        // ALWAYS ACTIVE: Normal mouse look handles yaw even during tricks
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
    /// UNIFIED IMPACT SYSTEM - Handle impact events from FallingDamageSystem
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
        Debug.Log($"[CAMERA IMPACT] ðŸŽ¯ {impact.severity} | " +
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
                    Debug.Log($"[CAMERA] Wall screen pos: {horizontalPos:F2}, side: {wallSide}, tilt: {dynamicWallTiltTarget:F1}Â°");
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
    ///  BFFL FIX: Now aware of trick state - instantly cancels reconciliation for seamless combo flow!
    /// </summary>
    public void TriggerWallJumpTilt(Vector3 wallNormal)
    {
        if (!enableWallJumpTilt) return;
        
        //  SIMPLIFIED: No reconciliation to cancel - just clean up trick mode!
        bool wasInTrickMode = isFreestyleModeActive;
        
        if (wasInTrickMode)
        {
            // Simply exit trick mode - camera is already upright (player body rotated during tricks)
            isFreestyleModeActive = false;
            
            // Reset momentum physics
            angularVelocity = Vector2.zero;
            smoothedInput = Vector2.zero;
            inputVelocity = Vector2.zero;
            isFlickBurstActive = false;
            
            // Reset camera pitch/roll to neutral (yaw is on player body now)
            freestyleRotation = Quaternion.identity;
            
            Debug.Log($"ðŸ¤ [TRICKâ†’WALLJUMP] Trick mode ended, camera reset to neutral");
        }
        
        //  COMBO SYSTEM INTEGRATION: Register wall jump in combo tracker
        if (ComboMultiplierSystem.Instance != null)
        {
            bool isAirborne = movementController != null && !movementController.IsGrounded;
            ComboMultiplierSystem.Instance.AddWallJump(isAirborne);
            
            // Extra feedback for seamless transitions
            if (wasInTrickMode)
            {
                Debug.Log(" [COMBO] SEAMLESS Trickâ†’WallJump transition detected!");
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
        
        Debug.Log($"ðŸŽ¥ [WALL JUMP TILT] Triggered! Tilt: {wallJumpTiltTarget:F1}Â°, Wall Normal: {wallNormal}, Dot: {dotRight:F2}");
    }
    
    /// <summary>
    ///  SMART FOV SYSTEM: Only transitions when needed, not every frame
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
    ///  PUBLIC METHOD: Call this when sprint STARTS
    /// </summary>
    public void SetSprintFOV()
    {
        targetFOV = baseFOV + sprintFOVIncrease;
        Debug.Log($"[AAACameraController] Sprint started - FOV target: {targetFOV}");
    }
    
    /// <summary>
    ///  PUBLIC METHOD: Call this when sprint STOPS
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
        //  UNIFIED IMPACT SYSTEM: Unsubscribe from impact events
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
        
        //  FREESTYLE MODE: Camera rotation is pitch/roll only (yaw on player body!)
        if (isFreestyleModeActive)
        {
            // During tricks, freestyleRotation only contains pitch/roll
            // Yaw comes from player body rotation
            transform.localRotation = freestyleRotation;
        }
        else
        {
            // Normal camera behavior
            // Combine all tilt sources (strafe + wall jump + dynamic wall-relative)
            float totalRollTilt = currentTilt + wallJumpTiltAmount + dynamicWallTilt;
            
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
        
        // Apply motion prediction for ultra-smooth movement
        if (enableMotionPrediction && predictedVelocity.sqrMagnitude > 0.01f)
        {
            totalOffset += predictedVelocity * Time.deltaTime * 0.1f; // Subtle prediction
        }
        
        //  CRITICAL FIX: Always apply offset relative to BASE position, not current position
        // This prevents drift/accumulation - camera height stays EXACTLY at base + offset
        transform.localPosition = baseLocalPosition + totalOffset;
    }
    
    // Public methods for camera shake system
    
    /// <summary>
    /// Updates the base local position for the camera (used by crouch system)
    /// </summary>
    public void UpdateBaseLocalPosition(Vector3 newBasePosition)
    {
        baseLocalPosition = newBasePosition;
    }
    
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
    // ðŸ›¡ï¸ STATE MACHINE & EMERGENCY RECOVERY (PHASE 1)
    // ========================================
    
    /// <summary>
    /// SIMPLIFIED: Validate state transition - prevents invalid state changes
    /// ðŸŽ¯ Reconciling and TransitionCleanup states removed!
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
                       newState == TrickSystemState.Grounded || // Direct to grounded on landing
                       newState == TrickSystemState.Airborne; // Allow cancel
                
            case TrickSystemState.LandingApproach:
                return newState == TrickSystemState.Grounded; // Direct to grounded (no reconciliation!)
                
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
                Debug.LogWarning($"[TRICK STATE] Invalid transition: {_trickState} â†’ {newState}");
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
            Debug.Log($"[TRICK STATE] {_previousTrickState} â†’ {_trickState}");
        }
    }
    
    /// <summary>
    /// SIMPLIFIED: State enter callback - setup for new state
    /// ðŸŽ¯ Reconciliation states removed!
    /// </summary>
    private void OnTrickStateEnter(TrickSystemState state)
    {
        switch (state)
        {
            case TrickSystemState.Grounded:
                // Ensure all systems are reset
                isFreestyleModeActive = false;
                // isReconciling removed - no longer needed
                break;
                
            case TrickSystemState.FreestyleActive:
                // Sync legacy flag
                isFreestyleModeActive = true;
                break;
                
            // ðŸŽ¯ Reconciling and TransitionCleanup cases REMOVED - no longer needed!
        }
    }
    
    /// <summary>
    /// SIMPLIFIED: State exit callback - cleanup for old state
    /// </summary>
    private void OnTrickStateExit(TrickSystemState state)
    {
        switch (state)
        {
            case TrickSystemState.FreestyleActive:
                // Exiting freestyle - log stats
                if (showEmergencyDebug)
                {
                    Debug.Log($"[TRICK STATE] Freestyle ended - X:{totalRotationX:F0}Â° Y:{totalRotationY:F0}Â° Z:{totalRotationZ:F0}Â°");
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
        if (_trickState == TrickSystemState.Grounded && !isFreestyleModeActive)
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
        
        // 2. CHECK FOR TIME.TIMESCALE STUCK (only when NOT in trick mode)
        if (!isFreestyleModeActive && timeDilationManager != null && timeDilationManager.IsTimeDilationActive())
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
        
        // ðŸŽ¯ CHECK 4 REMOVED: No infinite reconciliation possible - system simplified!
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
        
        // Reset all trick systems
        isFreestyleModeActive = false;
        // ðŸŽ¯ RECONCILIATION REMOVED: No longer tracking these variables!
        
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
        // wasReconciling removed - no longer needed
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
        
        // Reset all trick mode flags
        isFreestyleModeActive = false;
        // ðŸŽ¯ RECONCILIATION REMOVED: No longer tracking reconciliation state!
        wasAirborneLastFrame = false; // CRITICAL: Prevent false airborne detection
        
        // Reset rotation states
        // ðŸŽ¯ PRESERVE YAW during revive
        float targetYaw = currentLook.x;
        freestyleRotation = Quaternion.Euler(currentLook.y, targetYaw, 0f);
        totalRotationX = 0f;
        totalRotationY = 0f;
        totalRotationZ = 0f;
        freestyleLookInput = Vector2.zero;
        freestyleLookVelocity = Vector2.zero;
        
        // 🎪 RESET MOMENTUM PHYSICS SYSTEM
        angularVelocity = Vector2.zero;
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
        
        Debug.Log("[AAACameraController] âœ… Trick system reset complete - ready for normal gameplay");
    }
    
    // ========================================
    // ðŸŽª AERIAL FREESTYLE TRICK SYSTEM
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
        
        // ðŸŽ® PRO JUMP MECHANICS FOR TRICK SYSTEM (hold = higher jump, tap = small jump)
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
                    Debug.Log("ðŸŽ® [TRICK JUMP] Jump triggered on PRESS! Hold to go higher, release early for small jump.");
                }
                // If already airborne, ONLY activate freestyle (no jump)
                else if (!isFreestyleModeActive)
                {
                    EnterFreestyleMode();
                    Debug.Log("ðŸŽ® [TRICK JUMP] Already airborne - Freestyle activated (no jump)!");
                }
                else
                {
                    Debug.Log("ðŸŽ® [TRICK JUMP] Already in freestyle mode - ignoring input");
                }
            }
            
            // Button released - cut jump if released early (like spacebar!) AND ramp out of slow-mo
            if (Input.GetMouseButtonUp(2) && isHoldingScrollWheelButton)
            {
                float holdDuration = Time.time - scrollWheelButtonPressTime;
                isHoldingScrollWheelButton = false;
                
                // ðŸŽ® VARIABLE JUMP HEIGHT: If released early while still going up, cut the jump!
                // This is EXACTLY how spacebar works - release early = small jump
                if (movementController != null && movementController.Velocity.y > 0f)
                {
                    // Still going up - apply jump cut!
                    StartCoroutine(ApplyTrickJumpCut());
                    Debug.Log($"ðŸŽ® [TRICK JUMP] Released early ({holdDuration:F2}s) while rising - Jump cut applied!");
                }
                else
                {
                    Debug.Log($"ðŸŽ® [TRICK JUMP] Released after {holdDuration:F2}s (already falling or at peak)");
                }
                
                // ðŸŽ¬ SLOW-MO: Releasing button starts ramping out of slow-mo
                Debug.Log("ðŸŽ¬ [TIME DILATION] Scroll button released - Ramping out of slow-mo");
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
        
        //  ULTRA COOL: Handle trick FOV boost - synced with time dilation
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
        
        // Track airborne state for edge case handling
        wasAirborneLastFrame = isAirborne;
    }
    
    /// <summary>
    /// Trigger a trick jump using the existing system
    ///  Jump height control happens via ApplyTrickJumpCut() coroutine
    /// </summary>
    private void TriggerTrickJump()
    {
        if (movementController != null)
        {
            movementController.TriggerTrickJumpFromExternalSystem();
            Debug.Log("ðŸŽ® [TRICK JUMP] Trick jump triggered!");
        }
        else
        {
            Debug.LogWarning("ðŸŽ® [TRICK JUMP] AAAMovementController not found! Cannot trigger jump.");
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
                
                Debug.Log($"ðŸŽ® [TRICK JUMP] Jump cut applied! New Y velocity: {currentVelocity.y:F1}");
            }
            else
            {
                Debug.LogWarning("ðŸŽ® [TRICK JUMP] Could not access velocity field for jump cut!");
            }
        }
    }
    
    /// <summary>
    /// Enter freestyle mode - camera becomes independent from body
    /// SKATE-STYLE: Instant burst on activation for flick-it feel
    ///  BFFL: Clears reconciliation states for clean trick restart
    /// </summary>
    private void EnterFreestyleMode()
    {
        // Track if we're transitioning from wall jump
        bool isTransitioningFromWallJump = (Time.time - wallJumpTiltStartTime) < 0.5f;
        
        isFreestyleModeActive = true;
        freestyleModeStartTime = Time.time;
        isInInitialBurst = true;
        
        //  RECONCILIATION REMOVED: No cleanup needed - player body rotates with yaw!
        
        //  COMBO SYSTEM INTEGRATION: Register trick start
        if (ComboMultiplierSystem.Instance != null)
        {
            bool isAirborne = movementController != null && !movementController.IsGrounded;
            // Use trickAwesomeness = 1.0 for starting a trick (will get more points on landing)
            ComboMultiplierSystem.Instance.AddTrick(1.0f, isAirborne);
            
            // Extra feedback for seamless transitions
            if (isTransitioningFromWallJump)
            {
                Debug.Log(" [COMBO] SEAMLESS WallJumpâ†’Trick transition detected!");
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
        smoothedInput = Vector2.zero;
        inputVelocity = Vector2.zero;
        isFlickBurstActive = false;
        lastFlickDirection = Vector2.zero;
        
        //  PLAY EPIC TRICK START SOUND (bass thump slide down with slow-motion!)
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
        
        Debug.Log($" [FREESTYLE] Exited - Rotations: X={totalRotationX:F0}Â° Y={totalRotationY:F0}Â° Z={totalRotationZ:F0}Â°");
    }
    
    /// <summary>
    /// Handle landing while in freestyle mode - SIMPLIFIED (No reconciliation needed!)
    /// Player body already faces correct direction from yaw rotation during tricks
    /// Camera just needs to return pitch/roll to neutral
    /// </summary>
    private void LandDuringFreestyle()
    {
        isFreestyleModeActive = false;
        
        // ï¿½ CRITICAL FIX: Recapture baseline rotation so mouse look continues from new player orientation!
        // Without this, HandleLookInput() will snap player back to pre-trick rotation
        if (playerTransform != null)
        {
            baseYawRotation = playerTransform.rotation;
            yawStart = currentLook.x; // Reset yaw tracking to current mouse position
            referenceUp = playerTransform.up;
            
            Debug.Log($" [LANDING FIX] Recaptured baseline - Player now facing: {playerTransform.eulerAngles.y:F1}Â° yaw");
        }
        
        //  FREEZE MOMENTUM ON LANDING (stop the spin)
        angularVelocity = Vector2.zero;
        smoothedInput = Vector2.zero;
        inputVelocity = Vector2.zero;
        isFlickBurstActive = false;
        
        // Reset button hold state on landing
        isHoldingScrollWheelButton = false;
        
        //  CALCULATE LANDING QUALITY
        // Only check if upside down - no roll since we only do pitch
        Vector3 freestyleEuler = freestyleRotation.eulerAngles;
        
        // Check if upside down (pitch > 90° means inverted)
        float pitch = NormalizeAngle(freestyleEuler.x);
        bool isUpsideDown = Mathf.Abs(pitch) > 90f;
        
        // Clean landing = not upside down (no roll to check since we don't roll)
        bool isCleanLanding = !isUpsideDown;
        
        if (isCleanLanding)
        {
            // CLEAN LANDING - No reconciliation needed!
            Debug.Log($"✨ [FREESTYLE] CLEAN LANDING! Pitch: {pitch:F1}° - No camera adjustment needed");
            AddTrauma(0.1f); // Tiny trauma for impact feel
            
            // Sync currentLook.y to match freestyle pitch so HandleLookInput continues from here
            float finalPitch = freestyleEuler.x;
            if (finalPitch > 180f) finalPitch -= 360f; // Normalize to -180 to 180
            
            currentLook.y = finalPitch;
            targetLook.y = finalPitch;
            
            // Zero out roll for normal mode (should already be 0, but just in case)
            freestyleRotation = Quaternion.Euler(finalPitch, 0f, 0f);
        }
        else
        {
            // FAILED LANDING - Camera crashes to reality!
            Debug.Log($"💥 [FREESTYLE] CRASH LANDING! Upside down: {isUpsideDown} - Reconciling camera!");
            
            // Scale trauma based on how inverted we were
            float traumaAmount = isUpsideDown ? failedLandingTrauma : failedLandingTrauma * 0.5f;
            AddTrauma(traumaAmount);
            
            //  ONLY RECONCILE ON BAD LANDINGS (upside down)
            StartCoroutine(SmoothCameraReturnToNeutral(freestyleRotation, 0.3f));
        }
        
        Debug.Log($"🎪 [FREESTYLE] LANDED - Total flips: X={totalRotationX/360f:F1} Y={totalRotationY/360f:F1} Z={totalRotationZ/360f:F1}");
        
        //  AERIAL TRICK XP SYSTEM - Reward the player for sick tricks!
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
    /// Smoothly return camera to neutral orientation after landing
    /// ONLY resets roll - pitch is controlled by normal mouse look (currentLook.y)
    /// </summary>
    private System.Collections.IEnumerator SmoothCameraReturnToNeutral(Quaternion startRotation, float duration)
    {
        float elapsed = 0f;
        
        // Extract current pitch from normal mouse look system
        float targetPitch = currentLook.y;
        
        // Target is current pitch (from mouse look) + 0 roll
        Quaternion targetRotation = Quaternion.Euler(targetPitch, 0f, 0f);
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = reconciliationCurve.Evaluate(elapsed / duration);
            
            freestyleRotation = Quaternion.Slerp(startRotation, targetRotation, t);
            
            yield return null;
        }
        
        freestyleRotation = targetRotation;
        Debug.Log($" [CAMERA RETURN] Camera returned to neutral - Pitch: {targetPitch:F1}° (from mouse), Roll: 0°");
    }
    
    /// <summary>
    /// Handle mouse input during freestyle mode - ðŸŽª MOMENTUM PHYSICS SYSTEM (THE GEM)
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
            0f, // IGNORE X - yaw handled by normal mouse look!
            Input.GetAxis("Mouse Y") // ONLY process Y for pitch (backflips/frontflips)
        );
        
        // TIME DILATION COMPENSATION: Scale input to maintain consistent feel
        float timeCompensation = Mathf.Max(0.1f, Time.timeScale);
        
        // Apply sensitivity with time compensation
        Vector2 trickInput = rawInput * trickInputSensitivity * mouseSensitivity * timeCompensation;
        
        // Invert Y if needed
        if (invertY)
            trickInput.y = -trickInput.y;
        
        // ========================================
        //  MOMENTUM PHYSICS SYSTEM
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
                Debug.Log($" [FLICK] Burst activated! Magnitude: {inputMagnitude:F2}");
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
            // Yaw handled by normal mouse look system - trick system only does PITCH!
            
            // === TRACK TOTAL ROTATIONS FOR STATS ===
            totalRotationX += pitchDelta;
            // totalRotationY already tracked when rotating player body
            
            // === APPLY ROTATIONS TO CAMERA ===
            Quaternion pitchRotation = Quaternion.AngleAxis(pitchDelta, Vector3.right);
            
            // Camera only gets pitch - yaw is 100% on player body, NO ROLL
            freestyleRotation = freestyleRotation * pitchRotation;
            
            // === TRACK ROTATION SPEED (for motion blur) ===
            lastRotationSpeed = Mathf.Abs(pitchDelta) / Time.unscaledDeltaTime;
            
            // Debug visualization
            if (inputMagnitude > 0.1f || angularVelocity.magnitude > 10f)
            {
                Debug.Log($"🎪 [MOMENTUM] Input: {inputMagnitude:F2} | Velocity: {angularVelocity.magnitude:F1}°/s | Speed: {lastRotationSpeed:F1}°/s");
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
            // Yaw handled by normal mouse look - trick system only does PITCH!
            
            float maxDelta = maxTrickRotationSpeed * Time.unscaledDeltaTime;
            pitchDelta = Mathf.Clamp(pitchDelta, -maxDelta, maxDelta);
            
            totalRotationX += pitchDelta;
            
            // Apply pitch to camera only (yaw handled by normal mouse look)
            Quaternion pitchRotation = Quaternion.AngleAxis(pitchDelta, Vector3.right);
            freestyleRotation = freestyleRotation * pitchRotation;
            
            lastRotationSpeed = Mathf.Abs(pitchDelta) / Time.unscaledDeltaTime;
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
    //  TIME DILATION SYSTEM
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
                Debug.Log(" [AAACameraController] Created TimeDilationManager automatically");
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
                Debug.Log($"ðŸŽ¬ [TIME DILATION] Trick slow-mo ACTIVATED ({trickTimeScale:F2}x speed)");
            }
            else
            {
                Debug.Log("ðŸŽ¬ [TIME DILATION] Trick slow-mo DEACTIVATED");
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
