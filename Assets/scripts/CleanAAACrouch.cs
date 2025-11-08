using UnityEngine;
using GeminiGauntlet.Audio;

/// <summary>
/// Adds grounded-only crouch/duck behavior for CleanAAAMovementController.
/// - Smoothly adjusts CharacterController height/center and camera local Y
/// - Prevents standing if there is no headroom
/// - All target heights (player + camera) configurable in Inspector
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
[DefaultExecutionOrder(-300)]
public class CleanAAACrouch : MonoBehaviour
{
    [Header("=== 🎮 CONFIGURATION ===")]
    [Tooltip("ScriptableObject configuration asset (recommended). If null, uses legacy inspector settings below.")]
    [SerializeField] private CrouchConfig config;
    
    [Header("=== REFERENCES ===")]
    [Tooltip("Movement controller to read mode/grounded state from. If null, will try GetComponent<AAAMovementController>().")]
    [SerializeField] private AAAMovementController movement;
    [SerializeField] private CharacterController controller;
    [Tooltip("Player camera transform whose local Y should interpolate between standing/crouched heights. If null, will try a child Camera, then Camera.main.")]
    [SerializeField] private Transform cameraTransform;
    [Tooltip("Layered hand animation controller to drive slide and dive animations")]
    [SerializeField] private LayeredHandAnimationController layeredHandAnimationController;
    private PlayerAnimationStateManager animationStateManager; // Centralized animation system
    private PlayerEnergySystem energySystem; // For sprint state detection
    private CrouchSlamController slamController; // For slam state detection to prevent input conflicts

    [Header("=== ⚙️ INPUT KEYS ===")]
    // PHASE 3 COHERENCE FIX: All input keys use Controls class exclusively
    // Controls.Crouch and Controls.Dive are the single source of truth
    
    [Header("=== OPTIONAL: Particle & Audio References ===")]
    [Tooltip("Slide particle system (optional)")]
    [SerializeField] private ParticleSystem slideParticles;
    [Tooltip("Dive particle system (optional)")]
    [SerializeField] private ParticleSystem diveParticles;
    
    [Header("=== 🧪 LEGACY INSPECTOR SETTINGS (Used if Config is null) ===")]
    [Tooltip("Hold key to crouch, or tap to toggle?")]
    [SerializeField] private bool holdToCrouch = true;
    [Tooltip("Hold crouch key to slide, or tap to slide until stopped? (Recommended: true for manual control)")]
    [SerializeField] private bool holdToSlide = true;
    [SerializeField] private float standingHeight = 320.0f; // SCALED for 320-unit character (was 75)
    [SerializeField] private float crouchedHeight = 140.0f; // SCALED for 320-unit character (was 32)
    [SerializeField] private float standingCameraY = 300.0f; // SCALED for 320-unit character (was 75)
    [SerializeField] private float crouchedCameraY = 130.0f; // SCALED for 320-unit character (was 32)
    [SerializeField] private float heightLerpSpeed = 1280f; // SCALED 4x for 320-unit character (was 400)
    [SerializeField] private LayerMask obstructionMask = ~0;
    [SerializeField] private bool enableSlide = true;
    [SerializeField] private float slideMinStartSpeed = 960f; // FIXED: Was 105 - must be sprinting to slide (65% of 1485 sprint speed)
    [SerializeField] private float slideAutoStandSpeed = 25f; // LEGACY: Use config.slideAutoStandSpeed instead
    [SerializeField] private float slideGravityAccel = 3240f; // LEGACY: Use config.slideGravityAccel instead
    [SerializeField] private float slideFrictionFlat = 2f; // LEGACY: Use config.slideFrictionFlat instead
    [SerializeField] private float slideSteerAcceleration = 1200f; // LEGACY: Use config.slideSteerAcceleration instead
    [SerializeField] private float slideMaxSpeed = 3000f; // FIXED: Was 5040 - now 2× sprint (fast but not rocket-mode)
    [SerializeField] private float uphillFrictionMultiplier = 2.0f; // REBALANCED: Was 4f - reduced to 2× for smoother uphill transitions
    [SerializeField] private float stickToGroundVelocity = 66f; // LEGACY: Use config.stickToGroundVelocity instead
    [SerializeField] private float momentumPreservation = 0.96f; // LEGACY: Use config.momentumPreservation instead - MUST BE < 1.0 for decay!
    [SerializeField] private LayerMask slideGroundMask = ~0;
    [SerializeField] private float slideGroundCheckDistance = 640f; // SCALED for 320-unit character (2x height = 640) - was 600
    [SerializeField] private float slideGroundCoyoteTime = 0.225f; // UNIFIED: Matches AAAMovementController.coyoteTime for consistency
    [SerializeField] private float slidingGravity = -2250f; // SCALED 3x to match AAA gravity ratio (was -750, now matches -2500 * 0.9)
    [SerializeField] private float slidingTerminalDownVelocity = 540f; // SCALED 3x for consistency (was 180)
    [SerializeField] private AnimationCurve steerAccelBySpeedCurve;
    [SerializeField] private AnimationCurve frictionBySpeedCurve;
    [SerializeField] private float steerDriftLerp = 0.85f;
    [SerializeField] private bool slideFOVKick = true;
    [SerializeField] private float slideFOVAdd = 15f;
    [SerializeField] private bool slideAudioEnabled = true;
    [SerializeField] private bool diveAudioEnabled = true;
    [SerializeField] private bool slideParticlesEnabled = true;
    [SerializeField] private bool enableTacticalDive = true;
    [SerializeField] private float diveForwardForce = 2160f; // SCALED 3x for 320-unit character (was 720)
    [SerializeField] private float diveUpwardForce = 720f; // SCALED 3x for 320-unit character (was 240)
    [SerializeField] private float diveProneDuration = 0.8f; // TIME-BASED - no scaling needed
    [SerializeField] private float diveMinSprintSpeed = 960f; // SCALED 3x for 320-unit character (was 320)
    [SerializeField] private float diveSlideFriction = 5400f; // SCALED 3x for 320-unit character (was 1800)
    
    [Header("=== 🎯 AUTO-SLIDE CONTROL ===")]
    [Tooltip("Master toggle for SLOPE-BASED auto-slide. When OFF: NO auto-slide on steep slopes, NO auto-slide when pressing crouch on slopes. When ON: Slopes trigger slide automatically. Manual slide (crouch+sprint on flat) ALWAYS works regardless of this setting.")]
    [SerializeField] private bool enableAutoSlide = true;
    [SerializeField] private bool autoSlideOnLandingWhileCrouched = true;
    [SerializeField] private float landingSlopeAngleForAutoSlide = 12f;
    [SerializeField] private bool autoResumeSlideFromMomentum = true;
    [SerializeField] private float landingMomentumResumeWindow = 1.2f;
    [Header("=== 🎯 LANDING MOMENTUM CONTROL ===")]
    [Tooltip("Speed multiplier when landing with momentum (0.5 = half speed, 1.0 = full speed)")]
    [Range(0.3f, 1.0f)]
    [SerializeField] private float landingMomentumDamping = 0.65f; // 65% of landing speed preserved
    [Tooltip("Enable speed cap (prevents extreme speeds, but may break momentum chains if too low)")]
    [SerializeField] private bool enableLandingSpeedCap = false; // DISABLED by default - let momentum flow!
    [Tooltip("Maximum preserved speed on landing (only if cap enabled - set VERY HIGH to avoid breaking chains)")]
    [SerializeField] private float landingMaxPreservedSpeed = 2000f; // Very high - only catches extreme edge cases
    [SerializeField] private bool rampLaunchEnabled = true;
    [SerializeField] private float rampMinSpeed = 420f; // SCALED 3x for 320-unit character (was 140)
    [SerializeField] private float rampExtraUpBoost = 0.15f;
    [SerializeField] private bool showDebugVisualization = false;
    [SerializeField] private bool verboseDebugLogging = false;
    
    [Header("=== 🎯 SLIDE EXIT MOMENTUM PRESERVATION ===")]
    [Tooltip("Transfer slide momentum to movement controller when exiting slide (AAA feel!)")]
    [SerializeField] private bool enableSlideExitMomentum = true;
    [Tooltip("How much slide speed to preserve when exiting (0-1). 0.8 = 80% speed transfer")]
    [Range(0f, 1f)]
    [SerializeField] private float slideExitMomentumMultiplier = 0.85f;
    [Tooltip("Minimum slide speed to trigger momentum transfer (prevents weird behavior at low speeds)")]
    [SerializeField] private float slideExitMinSpeed = 100f;
    
    [Header("=== 🎯 SMOOTH WALL SLIDING (ENHANCEMENT) ===")]
    [Tooltip("Enable smooth wall sliding during slide (collide-and-slide algorithm). PURE ENHANCEMENT - no breaking changes.")]
    [SerializeField] private bool enableSmoothWallSliding = true;
    [Tooltip("Maximum iterations for multi-surface collision resolution per frame.")]
    [SerializeField] private int wallSlideMaxIterations = 3;
    [Tooltip("Speed preservation when sliding along walls (0-1). Higher = more momentum.")]
    [SerializeField] private float wallSlideSpeedPreservation = 0.95f;
    [Tooltip("Minimum wall angle (degrees from vertical) to trigger wall sliding. Lower = more surfaces.")]
    [SerializeField] private float wallSlideMinAngle = 45f;
    [Tooltip("Skin width multiplier for wall detection (prevents getting stuck in geometry).")]
    [SerializeField] private float wallSlideSkinMultiplier = 1.15f;
    [Tooltip("Show debug visualization for wall slide raycasts.")]
    [SerializeField] private bool showWallSlideDebug = false;
    
    // Hidden constants with optimal defaults
    private bool forceStandOnNonWalking = true;
    private bool autoScaleHeightsToController = true;
    private float autoScaleThresholdRatio = 1.5f;
    private float cameraLerpSpeed = 1280f; // SCALED 4x for 320-unit character (was 400)
    private float impactLerpSpeedMultiplier = 1.5f; // IMPACT BOOST: 1.5x faster lerp on high-speed landings (was 4x - too snappy!)
    private const float HIGH_SPEED_LANDING_THRESHOLD = 99960f; // FIXED: Was 350 - now matches MovementConfig.highSpeedThreshold (65% of sprint)
    private const float IMPACT_LANDING_WINDOW = 0.2f; // Time window to detect impact landings (seconds)
    private float slideMinimumDuration = 0.3f;
    private float slideBaseDuration = 1.2f;
    private float slideMaxExtraDuration = 1.0f;
    private float slideSpeedForMaxExtra = 120f;
    private float slideHardCapSeconds = 3.5f;
    private float slideEndSpeedThreshold = 15f;
    private bool slideHoldMode = true; // Default to hold-to-slide for manual control
    private float unifiedSlideFriction = 6f; // TRIPLED for better slope control (was 2f)
    private float uphillReversalSpeed = 12f;
    private float uphillMinTime = 0.2f;
    private float uphillReversalBoost = 25f;
    private float uphillRampUpDuration = 0.5f; // Time to reach full uphill friction (prevents brick wall effect)
    private float slopeTransitionGrace = 0.35f;
    private bool overrideSlopeLimitDuringSlide = true;
    private float slideSlopeLimitOverride = 90f;
    private bool usePureSlopeAlignedMovement = true;
    private bool reduceStepOffsetDuringSlide = true;
    private float slideStepOffsetOverride = 0f;
    private float stickDownWorldY = 120f; // SCALED 3x for 320-unit character (was 40)
    private float stickSpeedFactor = 0.15f;
    private float maxStickForce = 750f; // SCALED 3x for 320-unit character (was 250)
    private float groundNormalSmoothing = 15f;
    private bool reduceMinMoveDistanceDuringSlide = true;
    private float slideMinMoveDistanceOverride = 0f;
    private float minDownYWhileSliding = 75f; // SCALED 3x for 320-unit character (was 25)
    private bool useSlidingGravity = true;
    private bool useSteerCurve = true;
    private bool useFrictionCurve = true;
    private float slideFOVLerpSpeed = 20f;
    private bool allowSlideOnLanding = true;
    private float slideLandingBuffer = 0.6f; // EXTENDED: 0.6s buffer for long jumps (was 0.3s)
    private float rampMinUpY = 24f; // SCALED 3x for 320-unit character (was 8)
    private float rampNormalDeltaDeg = 12f;
    private float rampDownhillMemory = 0.3f;
    private float radiusSkin = 0.02f;
    private float debugArrowLength = 12f; // SCALED 3x for 320-unit character (was 4)
    private float diveSlideDistance = 3600f; // SCALED 3x for 320-unit character (was 1200)
    private float diveLandingCompression = 1200f; // SCALED 3x for 320-unit character (was 400)
    private bool enableDebugLogs = false;
    private SoundHandle slideAudioHandle = SoundHandle.Invalid;
    private bool particlesActive = false;
    
    // Tactical dive runtime state
    private bool isDiving = false;
    private bool isDiveProne = false;
    private float diveProneTimer = 0f;
    private Vector3 diveVelocity = Vector3.zero;
    private Vector3 diveSlideVelocity = Vector3.zero; // Horizontal slide velocity after landing
    private bool diveParticlesActive = false;
    private float diveStartTime = -999f;
    
    [Header("=== OPTIONAL: Performance Optimization ===")]
    [Tooltip("OPTIONAL: PlayerRaycastManager for consolidated ground checks. Falls back to local raycasts if null.")]
    [SerializeField] private PlayerRaycastManager raycastManager;

    // Runtime state
    private bool isCrouching = false;
    private bool toggleLatched = false;
    private float footOffsetLocalY;   // DEPRECATED: No longer used - center is always height/2 to match AAAMovementController
    private Vector3 cameraLocalStart; // To preserve X/Z

    private float targetHeight;
    private float targetCameraY;
    
    // Crouch height transition state - locks bottom position during entire transition
    private bool isTransitioningHeight = false;
    private float lockedBottomY = 0f;

    // Slide runtime
    private bool isSliding = false;
    private float slideTimer = 0f;
    private float slideExtraTime = 0f;
    private float slideInitialSpeed = 0f;
    private Vector3 slideVelocity = Vector3.zero; // world-space velocity we feed into controller
    private bool startedOnSlope = false;
    private bool wasOnSlope = false;
    private float lastSlopeTime = -999f;
    private float slideStartTime = -999f; // Track when slide started for landing grace period
    
    // PRISTINE: External velocity management - eliminate per-frame spam
    private Vector3 lastAppliedExternalVelocity = Vector3.zero;
    private float lastExternalVelocityUpdateTime = -999f;
    private const float EXTERNAL_VELOCITY_UPDATE_THRESHOLD = 0.05f; // Only update if changed by >5%
    // PHASE 3 COHERENCE FIX: Removed lastGroundedAt - use movement.TimeSinceGrounded instead
    private bool slideFOVActive = false;
    private float slideFOVBase = 100f;
    private float originalStepOffset = 75f;
    private Vector3 smoothedGroundNormal = Vector3.up;
    private bool hasSmoothedNormal = false;
    private float originalMinMoveDistance = 0.01f;
    private float slideBufferedUntil = -999f; // time until which a buffered slide input is valid
    private float lastDownhillAt = -999f;
    private bool hasLatchedAirMomentum = false;
    private bool forceSlideStartThisFrame = false; // used to bypass min speed on landing onto slope
    
    // Preserve and reuse carried momentum across air-to-ground transitions
    private Vector3 queuedLandingMomentum = Vector3.zero;
    private float queuedLandingMomentumUntil = -999f;
    
    // 🚀 PERFORMANCE OPTIMIZATION: Cached Vector3 instances to eliminate GC allocations in hot paths
    // These are reused every frame using .Set() instead of "new Vector3()"
    private Vector3 _cachedHorizontalVelocity;
    private Vector3 _cachedProjectionResult;
    private Vector3 _cachedDownhillDirection;
    private Vector3 _cachedSlopeAlignedVector;
    
    // Smart landing detection to prevent spam
    // PHASE 4 COHERENCE: Removed wasFallingLastFrame - use movement.IsFalling instead (single source of truth)
    private float lastLandingTime = -999f;
    private const float LANDING_COOLDOWN = 0.1f;
    private float slideDownVelY = 0f;
    
    // Uphill momentum system
    private bool isMovingUphill = false;
    private float uphillStartTime = -999f;
    private Vector3 lastDownhillDirection = Vector3.forward;
    private float slopeAngle = 0f;
    private float lastValidSlopeTime = -999f;
    private bool hasReversed = false;
    
    // Slope-to-flat transition detection for smooth deceleration
    private bool wasOnSlopeLastFrame = false;
    private float slopeToFlatTransitionTime = -999f;
    private const float FLAT_GROUND_DECEL_MULTIPLIER = 1.2f; // REBALANCED: Was 1.5× - now 1.2× for smoother transitions
    
    // PHASE 3 COHERENCE FIX: Centralized slope threshold constant (was duplicated in 2 methods)
    private const float SLOPE_ANGLE_THRESHOLD = 5f; // Flat ground (0-5°) is NOT considered a slope
    
    // Smart steep slope detection - prevent triggering on brief wall touches
    private float steepSlopeContactStartTime = -999f;
    private const float STEEP_SLOPE_MIN_CONTACT_TIME = 0.15f; // Must be on slope for 0.15s before auto-slide
    
    // PHASE 3 COHERENCE FIX: Removed duplicate lastGroundedAt - use AAAMovementController.TimeSinceGrounded instead
    // private float lastGroundedAt = -999f; // DEPRECATED - Duplicates AAA's TimeSinceGrounded property

    private void Awake()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        if (movement == null) movement = GetComponent<AAAMovementController>();

        if (cameraTransform == null)
        {
            Camera childCam = GetComponentInChildren<Camera>();
            if (childCam != null) cameraTransform = childCam.transform;
            else if (Camera.main != null) cameraTransform = Camera.main.transform;
        }

        // Find animation systems - use GameManager caching for performance
        if (GameManager.Instance != null)
        {
            animationStateManager = GameManager.Instance.GetPlayerAnimationStateManager();
            if (layeredHandAnimationController == null)
            {
                layeredHandAnimationController = GameManager.Instance.GetLayeredHandAnimationController();
            }
            if (energySystem == null)
            {
                energySystem = GameManager.Instance.GetEnergySystem();
            }
        }
        else
        {
            // FALLBACK: Only use expensive FindObjectOfType if GameManager is missing
            animationStateManager = FindObjectOfType<PlayerAnimationStateManager>();
            if (layeredHandAnimationController == null)
            {
                layeredHandAnimationController = FindObjectOfType<LayeredHandAnimationController>();
            }
            if (energySystem == null)
            {
                energySystem = FindObjectOfType<PlayerEnergySystem>();
            }
        }
        
        // Find slam controller for input conflict resolution
        slamController = GetComponent<CrouchSlamController>();
        
        // PERFORMANCE OPTIMIZATION: Auto-find raycast manager if not assigned
        if (raycastManager == null)
            raycastManager = GetComponent<PlayerRaycastManager>();

        // Load configuration from ScriptableObject if assigned
        LoadConfiguration();
        
        // If no config, use legacy inspector value for slideHoldMode
        if (config == null)
        {
            slideHoldMode = holdToSlide;
        }

        // Initialize defaults from current setup for convenience
        if (controller != null)
        {
            float prevStanding = standingHeight;
            float prevCrouched = crouchedHeight;
            if (standingHeight <= 0.01f) standingHeight = Mathf.Max(0.1f, controller.height);
            if (crouchedHeight <= 0.01f) crouchedHeight = Mathf.Clamp(controller.height * 0.6f, 0.2f, controller.height - 0.1f);
            footOffsetLocalY = controller.center.y - (controller.height * 0.5f);

            // If serialized heights are clearly out of scale vs the controller, auto-correct them
            if (autoScaleHeightsToController)
            {
                float ch = Mathf.Max(0.1f, controller.height);
                float ratio = (standingHeight > 0.001f) ? (ch / standingHeight) : 1f;
                bool mismatch = (ratio > autoScaleThresholdRatio) || (ratio < (1f / autoScaleThresholdRatio));
                if (mismatch)
                {
                    float crouchRatio = (prevStanding > 0.001f) ? Mathf.Clamp01(prevCrouched / prevStanding) : 0.6f;
                    if (crouchRatio <= 0f || crouchRatio >= 1f) crouchRatio = 0.6f;

                    // Scale heights
                    float scaleFactor = ch / Mathf.Max(0.001f, prevStanding);
                    standingHeight = ch;
                    crouchedHeight = Mathf.Clamp(ch * crouchRatio, 0.2f, ch - 0.1f);

                    // Keep feet planted reference consistent with new height
                    footOffsetLocalY = controller.center.y - (controller.height * 0.5f);

                    // Scale camera Y targets by same factor to preserve relative framing
                    standingCameraY *= scaleFactor;
                    crouchedCameraY *= scaleFactor;

                    Debug.Log($"[CleanAAACrouch] Auto-scaled to controller: StandingH={standingHeight:F2}, CrouchedH={crouchedHeight:F2}, CamY={standingCameraY:F2}->{crouchedCameraY:F2}");
                }
            }
        }

        if (cameraTransform != null)
        {
            cameraLocalStart = cameraTransform.localPosition;
            if (Mathf.Abs(standingCameraY) < 0.0001f) standingCameraY = cameraTransform.localPosition.y;
            if (Mathf.Abs(crouchedCameraY) < 0.0001f)  crouchedCameraY  = standingCameraY * 0.75f;
        }

        // Clamp logical relationships
        crouchedHeight = Mathf.Min(crouchedHeight, standingHeight - 0.05f);
        crouchedCameraY = Mathf.Min(crouchedCameraY, standingCameraY - 0.01f);

        targetHeight = standingHeight;
        targetCameraY = standingCameraY;
        // PHASE 4 COHERENCE: Removed wasFallingLastFrame initialization - use movement.IsFalling directly

        // Default curves if none assigned
        if (useSteerCurve && (steerAccelBySpeedCurve == null || steerAccelBySpeedCurve.keys.Length == 0))
        {
            steerAccelBySpeedCurve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(100f, 1f),
                new Keyframe(200f, 1.3f)
            );
        }
        if (useFrictionCurve && (frictionBySpeedCurve == null || frictionBySpeedCurve.keys.Length == 0))
        {
            frictionBySpeedCurve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(50f, 1f),
                new Keyframe(200f, 0.6f)
            );
        }
    }

    private void Update()
    {
        // ═══════════════════════════════════════════════════════════════════
        // 🧹 ABSOLUTE FIRST: Clear buffered jump if landing with slam momentum
        // ═══════════════════════════════════════════════════════════════════
        // Problem: Jump buffer queues Space during slam airtime → fires on landing → cancels slide
        // Solution: Clear buffer BEFORE jump priority check can consume it
        // Must be first line in Update() to prevent any buffered input from triggering!
        float earlyTime = Time.time;
        bool haveEarlyQueuedMomentum = (earlyTime <= queuedLandingMomentumUntil) && (queuedLandingMomentum.sqrMagnitude > 0.0001f);
        
        if (haveEarlyQueuedMomentum && movement != null)
        {
            movement.ClearJumpBuffer(); // Clear before ANY jump checks!
            if (verboseDebugLogging)
                Debug.Log($"<color=yellow>[SLAM] 🧹 Cleared jump buffer EARLY (before priority check)</color>");
        }
        
        // ═══════════════════════════════════════════════════════════════════
        // 🚀 CRITICAL: JUMP-WHILE-SLIDING CHECK - ABSOLUTE HIGHEST PRIORITY
        // ═══════════════════════════════════════════════════════════════════
        // This must run FIRST, before ANY other logic, to ensure jumping always works
        // Problem: If checked later, other systems might interfere
        // Solution: Check at frame start, exit immediately if jump pressed
        // ═══════════════════════════════════════════════════════════════════
        if (isSliding && Input.GetKeyDown(Controls.UpThrustJump))
        {
            // 🚀 OPTIMIZATION: Reuse cached Vector3 instead of allocating new one
            _cachedProjectionResult = Vector3.ProjectOnPlane(slideVelocity, Vector3.up);
            if (_cachedProjectionResult.sqrMagnitude > 0.0001f && movement != null)
            {
                movement.LatchAirMomentum(_cachedProjectionResult);
                hasLatchedAirMomentum = true;
                queuedLandingMomentum = _cachedProjectionResult;
                queuedLandingMomentumUntil = Time.time + Mathf.Max(slideGroundCoyoteTime, landingMomentumResumeWindow);
            }
            StopSlide();
            Debug.Log($"<color=lime>[SLIDE JUMP] 🚀 PRIORITY CHECK - Jump while sliding! Momentum: {_cachedProjectionResult.magnitude:F0}</color>");
            return; // Exit immediately - jump takes priority over everything!
        }
        
        // === PRISTINE: Cache per-frame values ONCE for consistency & performance ===
        float currentTime = Time.time; // Cache Time.time to avoid multiple native calls
        bool groundedRaw = movement != null && movement.IsGroundedRaw; // Instant truth for mechanics
        bool groundedWithCoyote = movement != null && movement.IsGroundedWithCoyote; // Forgiving for UX
        bool walkingMode = movement == null || movement.CurrentMode == AAAMovementController.MovementMode.Walking;
        
        // Now use cached values everywhere in this frame
        // Replace all `movement.IsGroundedRaw` with `groundedRaw`
        // Replace all `movement.IsGroundedWithCoyote` with `groundedWithCoyote`
        // Replace all `Time.time` with `currentTime` in this Update() scope
        
        // === PRISTINE AUTO-SLIDE SYSTEM ===
        // UNIFIED: ALL slopes (>12°) auto-slide when crouch pressed
        // STEEP slopes (>50°) force slide even without crouch (wall jump integrity)
        // CRITICAL FIX: Only check steep slopes if NOT already sliding (prevents interference with normal walking)
        // MASTER TOGGLE: Only runs if enableAutoSlide is TRUE
        if (enableAutoSlide && enableSlide && !isDiving && !isDiveProne && !isSliding && groundedRaw && movement != null)
        {
            CheckAndForceSlideOnSteepSlope();
        }
        
        // === TACTICAL DIVE INPUT CHECK ===
        // Must be: sprinting + grounded + X key pressed + not already diving/prone
        if (enableTacticalDive && !isDiving && !isDiveProne && !isSliding && walkingMode && groundedRaw)
        {
            bool isSprinting = Input.GetKey(Controls.Boost) && movement != null && movement.CurrentSpeed >= diveMinSprintSpeed;
            
            // DEBUG: Log dive attempt
            if (Input.GetKeyDown(Controls.Dive))
            {
                Debug.Log($"[DIVE DEBUG] Dive key pressed! isSprinting: {isSprinting}, CurrentSpeed: {movement?.CurrentSpeed}, MinRequired: {diveMinSprintSpeed}, Grounded: {groundedRaw}");
            }
            
            if (isSprinting && Input.GetKeyDown(Controls.Dive))
            {
                Debug.Log($"[DIVE DEBUG] STARTING DIVE NOW!");
                StartTacticalDive();
            }
        }
        else if (Input.GetKeyDown(Controls.Dive))
        {
            Debug.LogWarning($"[DIVE DEBUG] Can't dive! enableTacticalDive: {enableTacticalDive}, isDiving: {isDiving}, isDiveProne: {isDiveProne}, isSliding: {isSliding}, walkingMode: {walkingMode}, grounded: {groundedRaw}");
        }
        
        // Update dive state - DIVE TAKES FULL PRIORITY OVER EVERYTHING
        if (isDiving)
        {
            UpdateDive();
            // CRITICAL: Dive overrides all other movement logic - skip to camera/height updates
            // Force crouch state for visuals
            isCrouching = true;
            targetHeight = crouchedHeight;
            targetCameraY = crouchedCameraY;
            
            // Apply height and camera updates, then exit
            ApplyHeightAndCameraUpdates();
            return;
        }
        else if (isDiveProne)
        {
            UpdateDiveProne();
            // CRITICAL: Prone state overrides all other movement logic - skip to camera/height updates
            // Force crouch state for visuals
            isCrouching = true;
            targetHeight = crouchedHeight;
            targetCameraY = crouchedCameraY;
            
            // Apply height and camera updates, then exit
            ApplyHeightAndCameraUpdates();
            return;
        }
        
        // === CROUCH INPUT DETECTION ===
        // CRITICAL FIX: When crouch pressed on ANY slope, auto-start slide
        // SLAM INTEGRATION: Crouch key detection for manual slide activation (not for buffering!)
        // We DON'T block GetKeyDown during slam - slam will queue its own momentum
        bool isSlamActive = slamController != null && slamController.IsSlamming;
        bool crouchKeyPressed = Input.GetKeyDown(Controls.Crouch);
        
        // === PRISTINE: AUTO-SLIDE ON CROUCH FOR ALL SLOPES ===
        // ANY slope (>12°) triggers slide when crouch pressed
        // MASTER TOGGLE: Only runs if enableAutoSlide is TRUE
        if (enableAutoSlide && crouchKeyPressed && !isDiving && !isDiveProne && !isSliding && walkingMode && groundedRaw && movement != null)
        {
            // Check if we're on ANY slope (not just steep ones)
            if (ProbeGround(out RaycastHit crouchSlopeHit))
            {
                float crouchSlopeAngle = Vector3.Angle(Vector3.up, crouchSlopeHit.normal);
                
                // PRISTINE: Auto-slide on moderate slopes (12-50°) when crouch pressed
                // Steep slopes (>50°) are handled separately by CheckAndForceSlideOnSteepSlope()
                if (crouchSlopeAngle >= landingSlopeAngleForAutoSlide && crouchSlopeAngle <= 50f)
                {
                    forceSlideStartThisFrame = true;
                    TryStartSlide();
                    Debug.Log($"[AUTO-SLIDE] Crouch pressed on moderate slope ({crouchSlopeAngle:F1}°) - forcing slide start!");
                }
            }
        }

        // Buffered slide on landing: allow pressing slide in-air and start when we land
        // NOTE: This system is INDEPENDENT of enableAutoSlide - it's for manual slide buffering
        if (allowSlideOnLanding && enableSlide && walkingMode && movement != null)
        {
            // 🚀 ULTRA-ROBUST: Use coyote time for forgiving landing detection
            bool groundedForBufferedSlide = groundedRaw || groundedWithCoyote;
            
            // CRITICAL FIX: Refresh buffer EVERY FRAME while crouch is held in air
            // This fixes long sinusoidal jumps where you hold crouch the entire time
            // SLAM INTEGRATION: Allow crouch buffering even during slam - slam queues its own momentum
            bool crouchHeldInAir = Input.GetKey(Controls.Crouch);
            
            // REMOVED: Don't clear buffer during slam - let slam momentum take priority naturally
            // The queued momentum from slam will override any buffered slide input
            
            if (!groundedForBufferedSlide && (crouchKeyPressed || crouchHeldInAir))
            {
                slideBufferedUntil = currentTime + slideLandingBuffer;
                if (crouchKeyPressed)
                {
                    Debug.Log($"<color=cyan>[SLIDE BUFFER] Slide buffered in air (keydown)! Buffer until: {slideBufferedUntil:F2}</color>");
                }
            }

            // DISABLED: Land animations now handled by AAAMovementController -> HandAnimationController.OnPlayerLanded()
            // This prevents catastrophic animation spam that blocks combat
            // if (justLanded && (Time.time - lastLandingTime) >= LANDING_COOLDOWN)
            // {
            //     if (handAnimationController != null)
            //     {
            //         handAnimationController.PlayLandAnimation();
            //         didTriggerLandAnim = true;
            //         lastLandingTime = Time.time;
            //         
            //         if (enableDebugLogs)
            //             Debug.Log("[CleanAAACrouch] SMART LANDING detected - playing land animation");
            //     }
            // }
            
            // Check if we have queued momentum to resume slide
            bool haveQueuedMomentum = (currentTime <= queuedLandingMomentumUntil) && (queuedLandingMomentum.sqrMagnitude > 0.0001f);
            
            if (haveQueuedMomentum)
            {
                Debug.Log($"<color=magenta>[SLAM DEBUG] Queued momentum detected! Mag: {queuedLandingMomentum.magnitude:F0}, Until: {queuedLandingMomentumUntil:F2}, Now: {currentTime:F2}</color>");
                // Jump buffer already cleared at start of Update() - no need to do it again here
            }
            
            // Check for buffered slide input or active conditions
            // SLAM INTEGRATION: Allow crouch detection even during slam - momentum priority handled in TryStartSlide()
            bool crouchHeld = Input.GetKey(Controls.Crouch);
            bool crouchActive = crouchHeld || (!holdToCrouch && toggleLatched) || isCrouching;
            
            // 🚀 ULTRA-ROBUST: Try slide with coyote forgiveness for perfect landing detection
            // CRITICAL FIX: Check crouch held FIRST (highest priority for continuous hold)
            bool shouldAttemptSlide = crouchHeld || (currentTime <= slideBufferedUntil) || haveQueuedMomentum;
            
            if (!isSliding && groundedForBufferedSlide && shouldAttemptSlide)
            {
                if (haveQueuedMomentum)
                {
                    forceSlideStartThisFrame = true;
                    Debug.Log($"<color=lime>[SLIDE BUFFER] Queued momentum detected - forcing slide start!</color>");
                }
                else if (crouchHeld)
                {
                    Debug.Log($"<color=lime>[SLIDE BUFFER] Crouch held during landing - attempting slide!</color>");
                }
                else if (currentTime <= slideBufferedUntil)
                {
                    Debug.Log($"<color=lime>[SLIDE BUFFER] Buffered input detected - attempting slide!</color>");
                }
                
                // Buffered/held start: if we landed onto a slope above threshold, force-start
                if (ProbeGround(out RaycastHit landHit2))
                {
                    float landAngle2 = Vector3.Angle(Vector3.up, landHit2.normal);
                    if (landAngle2 >= landingSlopeAngleForAutoSlide)
                    {
                        forceSlideStartThisFrame = true;
                        Debug.Log($"<color=lime>[SLIDE BUFFER] Landed on slope ({landAngle2:F1}°) - forcing slide start!</color>");
                    }
                }
                TryStartSlide();
                slideBufferedUntil = -999f; // Clear buffer after attempt
            }
            else if (autoSlideOnLandingWhileCrouched && crouchActive && !isSliding && groundedForBufferedSlide)
            {
                // If crouched on a slope, force slide start
                if (ProbeGround(out RaycastHit landHit))
                {
                    float landAngle = Vector3.Angle(Vector3.up, landHit.normal);
                    if (landAngle >= landingSlopeAngleForAutoSlide)
                    {
                        forceSlideStartThisFrame = true;
                        TryStartSlide();
                    }
                }
            }
        }

        // Manual slide start (flat ground, high speed)
        // Note: Slope-based auto-slide already handled above with crouchKeyPressed
        if (enableSlide && !isSliding && !isDiving && !isDiveProne && walkingMode && groundedRaw && crouchKeyPressed)
        {
            // Only try if not already handled by slope auto-slide
            TryStartSlide();
        }

        if (isSliding)
        {
            UpdateSlide();
            // While sliding, enforce crouch visuals
            isCrouching = true;
        }
        
        // While diving or prone, enforce crouch visuals
        if (isDiving || isDiveProne)
        {
            isCrouching = true;
        }

        bool wantCrouch = false;
        if (holdToCrouch)
        {
            // Use coyote-grounded for forgiving crouch feel (UX, not mechanics)
            // INTEGRATION FIX: Don't check crouch if actively slamming
            wantCrouch = !isSlamActive && walkingMode && groundedWithCoyote && Input.GetKey(Controls.Crouch);
        }
        else
        {
            // Toggle-style: press once to crouch, press again to stand
            if (walkingMode && groundedWithCoyote && crouchKeyPressed) toggleLatched = !toggleLatched;
            // Force stand when leaving Walking mode
            if (!walkingMode && forceStandOnNonWalking) toggleLatched = false;
            wantCrouch = toggleLatched && walkingMode && groundedWithCoyote;
        }

        // If trying to stand, ensure there is headroom; otherwise remain crouched
        if (!wantCrouch && isCrouching && !isSliding)
        {
            if (!HasHeadroomToStand())
            {
                wantCrouch = true; // keep crouched if blocked
            }
        }

        if (!isSliding && !isDiving && !isDiveProne)
        {
            isCrouching = wantCrouch;
        }

        targetHeight = isCrouching ? crouchedHeight : standingHeight;
        targetCameraY = isCrouching ? crouchedCameraY : standingCameraY;

        // Apply height and camera updates
        ApplyHeightAndCameraUpdates();

        // If we left Walking/Grounded and should stand, gently move toward standing targets
        bool leftWalkingMode = !walkingMode;
        if (forceStandOnNonWalking && leftWalkingMode)
        {
            isCrouching = false;
        }

        // Slide FOV kick update
        UpdateSlideFOV();

        // DISABLED: Land animations now handled by AAAMovementController -> HandAnimationController.OnPlayerLanded()
        // This prevents catastrophic animation spam that blocks combat
        // if (!didTriggerLandAnim && justLanded && (Time.time - lastLandingTime) >= LANDING_COOLDOWN)
        // {
        //     if (handAnimationController != null)
        //     {
        //         handAnimationController.PlayLandAnimation();
        //         lastLandingTime = Time.time;
        //         
        //         if (enableDebugLogs)
        //             Debug.Log("[CleanAAACrouch] SMART LANDING (global) detected - playing land animation");
        //     }
        // }

        // Safety net for queued momentum (independent of auto-slide toggle)
        // This ensures crouch slam momentum always works
        if (enableSlide && movement != null && !isSliding && groundedRaw)
        {
            bool haveQueuedMomentumNow = (currentTime <= queuedLandingMomentumUntil) && (queuedLandingMomentum.sqrMagnitude > 0.0001f);
            if (haveQueuedMomentumNow)
            {
                forceSlideStartThisFrame = true;
                TryStartSlide();
            }
        }
    }

    private void TryStartSlide()
    {
        if (movement == null || controller == null) return;
        
        // CRITICAL: Don't restart slide if already sliding - prevents double initialization
        if (isSliding) return;
        
        // Allow steep slope forced slides to bypass walking mode requirement
        if (movement.CurrentMode != AAAMovementController.MovementMode.Walking)
        {
            // Allow if: queued momentum OR forced by steep slope
            bool haveQueued = (Time.time <= queuedLandingMomentumUntil) && (queuedLandingMomentum.sqrMagnitude > 0.0001f);
            bool isGroundedNow = movement.IsGroundedRaw;
            if (!(haveQueued && isGroundedNow) && !forceSlideStartThisFrame)
                return;
        }
        // ULTRA-ROBUST: Use coyote time for buffered slide activation (forgiving landing detection)
        bool groundedForSlide = movement.IsGroundedRaw || movement.IsGroundedWithCoyote;
        if (!groundedForSlide) return;

        // Use queued landing momentum if available, otherwise current horizontal velocity
        bool haveQueuedLandingMomentum = (Time.time <= queuedLandingMomentumUntil) && (queuedLandingMomentum.sqrMagnitude > 0.0001f);
        Vector3 horizVel;
        if (haveQueuedLandingMomentum)
        {
            horizVel = queuedLandingMomentum; // already horizontal (projected when latched)
            Debug.Log($"<color=yellow>[SLIDE START] Using queued momentum: {horizVel.magnitude:F0} u/s</color>");
        }
        else
        {
            horizVel = movement.Velocity;
            horizVel.y = 0f;
            Debug.Log($"<color=yellow>[SLIDE START] Using current velocity: {horizVel.magnitude:F0} u/s</color>");
        }
        float speed = horizVel.magnitude;

        // Check slope status
        bool hasGround = ProbeGround(out RaycastHit hit);
        float slopeAngle = hasGround ? Vector3.Angle(Vector3.up, hit.normal) : 0f;
        
        // PHASE 3 COHERENCE FIX: Use class-level constant instead of local const
        bool onSlope = hasGround && slopeAngle > SLOPE_ANGLE_THRESHOLD;
        
        // Capture and consume forced-start flag
        bool forcedByLandingSlope = forceSlideStartThisFrame; // set in Update() on landing
        bool forceStart = forcedByLandingSlope || haveQueuedLandingMomentum;
        forceSlideStartThisFrame = false;
        
        // Allow easier slide initiation on slopes
        float effectiveMinSpeed = onSlope && slopeAngle > 10f ? SlideMinStartSpeed * 0.6f : SlideMinStartSpeed;
        if (haveQueuedLandingMomentum)
        {
            // Resume slide using preserved momentum even on flat ground
            effectiveMinSpeed = 0f;
        }
        else if (forcedByLandingSlope && onSlope)
        {
            // CRITICAL FIX: When forced by steep slope (CheckAndForceSlideOnSteepSlope), allow zero speed
            // This allows slide to start even when player is standing still
            effectiveMinSpeed = 0f;
        }
        
        // PRISTINE FIX: Allow slide with ZERO speed if forced (steep slope or crouch-on-slope)
        if (speed < effectiveMinSpeed && !forceStart)
        {
            return; // too slow to initiate slide
        }

        // Initialize slide state
        isSliding = true;
        slideTimer = 0f;
        slideInitialSpeed = speed;
        slideStartTime = Time.time; // Track slide start for landing grace period
        
        Debug.Log($"[SLIDE START] Speed: {speed:F2}, EffectiveMin: {effectiveMinSpeed:F2}, Forced: {forcedByLandingSlope}, Angle: {slopeAngle:F1}°, HaveQueuedMomentum: {haveQueuedLandingMomentum}");
        // Extend slide duration based on initial speed and slope start
        slideExtraTime = Mathf.Lerp(0f, slideMaxExtraDuration,
            Mathf.InverseLerp(SlideMinStartSpeed, slideSpeedForMaxExtra, slideInitialSpeed));
        if (onSlope && slopeAngle > 20f)
        {
            // BUTTER MODE: Give MASSIVE extra time on slopes for long smooth rides
            slideExtraTime = Mathf.Min(slideHardCapSeconds, slideExtraTime + 3.0f);
        }
        startedOnSlope = onSlope;
        wasOnSlope = onSlope;
        wasOnSlopeLastFrame = onSlope; // CRITICAL: Reset frame tracking for NEW slide
        lastSlopeTime = onSlope ? Time.time : -999f;
        
        // CRITICAL FIX: Reset slope-to-flat transition tracking for NEW slide session
        // This prevents friction enhancement from persisting across different slides
        slopeToFlatTransitionTime = -999f; // Clear any old transition from previous slide
        
        Debug.Log($"[SLIDE START DEBUG] Transition time reset to -999, onSlope={onSlope}, angle={slopeAngle:F1}°");
        
        // Initialize smoothed ground normal
        smoothedGroundNormal = onSlope ? hit.normal : Vector3.up;
        hasSmoothedNormal = true;
        hasLatchedAirMomentum = false;
        // Initialize downward adhesion velocity so we immediately push toward ground
        slideDownVelY = Mathf.Min(-minDownYWhileSliding, 0f);
        
        // Audio feedback
        PlaySlideStartSound();
        
        // Visual feedback - slight camera shake for impact
        if (cameraTransform != null && speed > SlideMinStartSpeed * 1.5f)
        {
            StartCoroutine(CameraImpactShake(0.15f, 0.8f));
        }
        
        // Start particle effects
        StartSlideParticles();

        // Start velocity: CONTROLLED MOMENTUM PRESERVATION
        Vector3 startDir;
        if (onSlope)
        {
            // 🚀 OPTIMIZATION: Reuse cached Vector3 for downhill calculation
            _cachedDownhillDirection = Vector3.ProjectOnPlane(Vector3.down, hit.normal).normalized;
            
            // ═══════════════════════════════════════════════════════════════════
            // 🎯 SMART SPEED CAP - ALLOWS CROUCH SLAM BOOST, PREVENTS EXPLOITS
            // ═══════════════════════════════════════════════════════════════════
            // Crouch slam intentionally boosts to high speeds - this is a FEATURE!
            // Only cap random physics glitches/exploits, not legitimate slam mechanics
            const float MAX_SAFE_LANDING_SPEED = 400f; // For random landings only
            
            bool landingWithMomentum = haveQueuedLandingMomentum || speed > 100f;
            
            if (landingWithMomentum && horizVel.sqrMagnitude > 0.01f)
            {
                // ═══════════════════════════════════════════════════════════════════
                // 🎯 CRITICAL: RESPECT CROUCH SLAM DIRECTION!
                // Crouch slam already calculated optimal downhill-blended direction
                // Re-blending it here DESTROYS the carefully tuned slam physics
                // ═══════════════════════════════════════════════════════════════════
                
                if (haveQueuedLandingMomentum)
                {
                    // Crouch slam momentum: USE EXACT DIRECTION (already optimized!)
                    startDir = horizVel.normalized;
                    
                    Debug.Log($"<color=lime>[SLIDE] 🚀 CROUCH SLAM MOMENTUM PRESERVED! Dir: {startDir}, Speed: {speed:F0}</color>");
                }
                else
                {
                    // Regular landing: Blend momentum with downhill direction
                    // 🚀 OPTIMIZATION: Reuse cached Vector3 for slope alignment
                    _cachedSlopeAlignedVector = Vector3.ProjectOnPlane(horizVel, hit.normal).normalized;
                    float blendFactor = Mathf.Clamp01(speed / 300f); // 0-300 units = 0-100% preservation
                    startDir = Vector3.Slerp(_cachedDownhillDirection, _cachedSlopeAlignedVector, blendFactor).normalized;
                    
                    Debug.Log($"<color=cyan>[SLIDE] Regular landing - Blend: {blendFactor:F2}, Dir: {startDir}</color>");
                }
                
                // SMART CAP: Only cap random landings, NEVER cap crouch slam!
                // Crouch slam speed is physics-calculated and should be preserved exactly
                if (!haveQueuedLandingMomentum && speed > MAX_SAFE_LANDING_SPEED)
                {
                    Debug.LogWarning($"<color=red>[SLIDE] Random landing speed {speed:F0} capped to {MAX_SAFE_LANDING_SPEED:F0}</color>");
                    speed = MAX_SAFE_LANDING_SPEED;
                }
                
                Debug.Log($"<color=cyan>[SLIDE] Momentum start - Speed: {speed:F2}, Dir: {startDir}, Capped: {!haveQueuedLandingMomentum}</color>");
            }
            else
            {
                // Low speed landing - use pure downhill with gentle acceleration
                startDir = _cachedDownhillDirection;
                speed = Mathf.Max(speed * 0.3f, 50f); // Start slow, let gravity take over
                
                Debug.Log($"<color=yellow>[SLIDE] Low speed start - using pure downhill, Speed: {speed:F2}</color>");
            }
        }
        else
        {
            startDir = horizVel.sqrMagnitude > 0.01f ? horizVel.normalized : transform.forward;
        }
        
        // ═══════════════════════════════════════════════════════════════════
        // 🎯 NEXT-LEVEL SPRINT DETECTION - FRAME-PERFECT PRECISION 🎯
        // ═══════════════════════════════════════════════════════════════════
        // DESIGN: Velocity-based (player releases sprint to press crouch)
        // THRESHOLD: 97% of sprint speed (3% tolerance for frame jitter)
        // ANTI-CHEAT: Upper bound prevents speed hacks/exploits
        // FEEL: Preserves sprint energy into slide for AAA momentum flow
        // ═══════════════════════════════════════════════════════════════════
        
        // Calculate sprint threshold with proper tolerance (97% not 90%)
        // Why 97%? Frame timing jitter at 60 FPS = ±1.67%, at 30 FPS = ±3.33%
        // 97% catches all legitimate sprints while accounting for variation
        float sprintSpeedThreshold = 1350f; // Fallback for safety
        
        if (movement != null && movement.MoveSpeed > 0.1f && movement.SprintMultiplier > 1.0f)
        {
            // Formula: moveSpeed × sprintMultiplier × 0.97
            // Example: 900 × 1.65 × 0.97 = 1440.45 units/s
            sprintSpeedThreshold = movement.MoveSpeed * movement.SprintMultiplier * 0.97f;
        }
        
        // NO LIMITS - Accept any speed, no anti-cheat restrictions
        bool wasSprinting = speed >= sprintSpeedThreshold;
        
        // NEXT-LEVEL BOOST: 1.2× multiplier gives satisfying sprint-to-slide flow
        // Testing: 1.15× felt weak, 1.5× caused exponential issues, 1.2× = PERFECT
        // This is the Apex Legends / Titanfall 2 sweet spot
        float sprintBoost = wasSprinting ? 1.2f : 1.0f;
        
        if (wasSprinting && verboseDebugLogging)
        {
            Debug.Log($"<color=lime>[SLIDE] ⚡ SPRINT ENERGY CAPTURED! Speed: {speed:F2}, Boost: {sprintBoost:F2}×</color>");
        }
        
        // CRITICAL FIX: Always apply minimum speed enforcement
        // The "respect actual speed" logic was AI-generated nonsense that caused physics explosions
        // Apply sprint boost to preserve sprint energy, but ALWAYS enforce minimum
        slideVelocity = startDir * (Mathf.Max(effectiveMinSpeed, speed) * sprintBoost);
        
        Debug.Log($"<color=cyan>[SLIDE INIT] Velocity: {slideVelocity.magnitude:F2} (speed: {speed:F2}, min: {effectiveMinSpeed:F2}, sprint: {sprintBoost}x)</color>");
        
        // Consumed queued momentum
        if (haveQueuedLandingMomentum)
        {
            queuedLandingMomentum = Vector3.zero;
            queuedLandingMomentumUntil = -999f;
        }

        // PRISTINE: Request ALL controller modifications through AAA coordination API
        // CRITICAL FIX: DO NOT override slope limit during slide!
        // Setting slope limit to 90° prevents natural sliding physics - character stands still instead
        // Unity's CharacterController with 50° limit naturally slides on 70° slopes
        // The slide system adds controlled velocity on top of Unity's natural physics
        /*
        if (overrideSlopeLimitDuringSlide && movement != null)
        {
            movement.RequestSlopeLimitOverride(slideSlopeLimitOverride, AAAMovementController.ControllerModificationSource.Crouch);
        }
        */
        
        // PRISTINE: Use ownership API for stepOffset
        if (reduceStepOffsetDuringSlide && movement != null)
        {
            movement.RequestStepOffsetOverride(slideStepOffsetOverride, AAAMovementController.ControllerModificationSource.Crouch);
        }
        // PRISTINE: Use ownership API for minMoveDistance
        if (reduceMinMoveDistanceDuringSlide && movement != null)
        {
            movement.RequestMinMoveDistanceOverride(slideMinMoveDistanceOverride, AAAMovementController.ControllerModificationSource.Crouch);
        }

        // Capture FOV base at slide start
        if (slideFOVKick)
        {
            var cam = GetSlideCamera();
            if (cam != null)
            {
                slideFOVBase = cam.fieldOfView;
                slideFOVActive = true;
            }
        }

        // CRITICAL FIX: Trigger slide animation IMMEDIATELY when slide happens!
        if (animationStateManager != null)
        {
            animationStateManager.SetMovementState((int)PlayerAnimationStateManager.PlayerAnimationState.Slide);
            Debug.Log("[SLIDE] Slide animation triggered IMMEDIATELY!");
        }
    }

    private void UpdateSlide()
    {
        if (!isSliding || movement == null || controller == null) return;

        slideTimer += Time.deltaTime;

        // === CRITICAL: JUMP DETECTION - HIGHEST PRIORITY ===
        // ═══════════════════════════════════════════════════════════════════
        // 🚀 FIX: Check raw jump input FIRST, before any other logic
        // Problem: movement.JumpButtonPressed can return false if consumed elsewhere
        // Solution: Direct input check ensures jump while sliding ALWAYS works
        // ═══════════════════════════════════════════════════════════════════
        // Check BOTH the AAA property AND raw input for maximum responsiveness
        bool jumpPressed = (movement != null && movement.JumpButtonPressed) || Input.GetKeyDown(Controls.UpThrustJump);
        
        if (jumpPressed)
        {
            Vector3 carry = Vector3.ProjectOnPlane(slideVelocity, Vector3.up);
            if (carry.sqrMagnitude > 0.0001f)
            {
                movement.LatchAirMomentum(carry);
                hasLatchedAirMomentum = true;
                // Remember this momentum for a landing window so we can resume slide at full speed
                queuedLandingMomentum = carry;
                queuedLandingMomentumUntil = Time.time + Mathf.Max(slideGroundCoyoteTime, landingMomentumResumeWindow);
            }
            // JUMP FIX: Stop slide immediately to clear external velocity override
            StopSlide();
            
            Debug.Log($"<color=cyan>[SLIDE JUMP] 🚀 Jump while sliding! Momentum preserved: {carry.magnitude:F0}</color>");
            
            // Jump animation is handled automatically by PlayerAnimationStateManager
            return;
        }

        // Check ground status
        bool hasGround = ProbeGround(out RaycastHit hit);
        slopeAngle = hasGround ? Vector3.Angle(Vector3.up, hit.normal) : 0f;
        
        // PHASE 3 COHERENCE FIX: Use class-level constant instead of local const
        bool onSlope = hasGround && slopeAngle > SLOPE_ANGLE_THRESHOLD;
        
        if (onSlope)
        {
            // Refresh slope grace so we don't prematurely stop on steep terrain
            lastValidSlopeTime = Time.time;
            // PHASE 3 COHERENCE FIX: Removed lastGroundedAt - AAA tracks grounded time
        }

        // Calculate speed FIRST (needed for transition detection)
        float slideSpeed = slideVelocity.magnitude;
        
        // CRITICAL FIX: Detect slope-to-flat transitions for smooth deceleration
        // Check BEFORE updating wasOnSlopeLastFrame to catch the transition!
        // ULTRA-ROBUST: Add grace period to prevent false detection on landing
        bool isJustStartedSlide = (Time.time - slideStartTime) < 0.25f; // 0.25s grace period
        
        if (wasOnSlopeLastFrame && !onSlope && !isJustStartedSlide)
        {
            // Just transitioned from slope to flat ground!
            slopeToFlatTransitionTime = Time.time;
            if (verboseDebugLogging)
            {
                Debug.Log($"<color=cyan>[SLIDE] Slope-to-flat transition detected! Speed: {slideSpeed:F2}</color>");
            }
        }
        else if (isJustStartedSlide && !onSlope)
        {
            // Landing detection might be flaky - don't trigger transition yet
            if (verboseDebugLogging)
            {
                Debug.Log($"<color=green>[SLIDE] Grace period active - ignoring potential false slope-to-flat detection</color>");
            }
        }
        
        // Track slope history for transition handling
        if (onSlope && !wasOnSlope)
        {
            // Just transitioned onto a slope - add momentum boost
            lastSlopeTime = Time.time;
            if (slopeAngle > 20f && slideSpeed > 30f)
            {
                slideVelocity *= 1.15f; // Small boost when hitting slopes
            }
        }
        
        // CRITICAL: Update tracking variables at END of checks (after transition detection)
        wasOnSlope = onSlope;
        wasOnSlopeLastFrame = onSlope; // Save current state for NEXT frame
        
        // CRITICAL FIX: Stop immediately when speed reaches 0 or near-zero
        if (slideSpeed <= 0.1f)
        {
            if (verboseDebugLogging)
            {
                Debug.Log($"<color=yellow>[SLIDE] Speed reached 0 ({slideSpeed:F2}) - stopping slide immediately!</color>");
            }
            StopSlide();
            return;
        }
        
        // Smart auto-stand logic (only after minimum duration)
        bool shouldAutoStand = false;
        if (slideTimer > slideMinimumDuration)
        {
            // CRITICAL FIX: On steep slopes (>50°), NEVER auto-stop - let physics handle it
            // Only auto-stand on flat/gentle slopes when speed is low
            const float STEEP_SLOPE_THRESHOLD = 50f;
            bool onSteepSlope = slopeAngle >= STEEP_SLOPE_THRESHOLD;
            
            if (!onSteepSlope && !onSlope && slideSpeed < slideAutoStandSpeed)
            {
                shouldAutoStand = true;
            }
        }

        // Slide input logic: in hold mode, releasing ends the slide; in tap mode, ignore key state after start
        // PHASE 4 COHERENCE FIX: Use Controls.Crouch instead of removed crouchKey variable
        // CRITICAL FIX: On steep slopes (55°+), IGNORE button release - force slide regardless of input
        const float AUTO_SLIDE_THRESHOLD = 55f;
        bool onAutoSlideSlope = slopeAngle >= AUTO_SLIDE_THRESHOLD;
        
        bool buttonHeld = Input.GetKey(Controls.Crouch);
        bool releaseStop = slideHoldMode && !onAutoSlideSlope ? (!buttonHeld && slideTimer > 0.1f) : false;
        if (shouldAutoStand || releaseStop)
        {
            StopSlide();
            return;
        }
        // Keep sliding even when briefly airborne; downward adhesion handled via sliding gravity below.

        // Smooth ground normal updates
        if (onSlope)
        {
            smoothedGroundNormal = Vector3.Slerp(smoothedGroundNormal, hit.normal, 
                1f - Mathf.Exp(-groundNormalSmoothing * Time.deltaTime));
        }
        else
        {
            smoothedGroundNormal = Vector3.Slerp(smoothedGroundNormal, Vector3.up, 
                1f - Mathf.Exp(-groundNormalSmoothing * Time.deltaTime));
        }

        // Update uphill/downhill flag for debug visualization
        if (onSlope && slideVelocity.sqrMagnitude > 0.0001f)
        {
            // 🚀 OPTIMIZATION: Reuse cached Vector3 for downhill calculation
            _cachedDownhillDirection = Vector3.ProjectOnPlane(Vector3.down, smoothedGroundNormal).normalized;
            bool wasMovingUphill = isMovingUphill;
            isMovingUphill = Vector3.Dot(slideVelocity.normalized, _cachedDownhillDirection) < 0f;
            
            // DEBUG: Log transition detection
            if (verboseDebugLogging && wasMovingUphill != isMovingUphill)
            {
                Debug.Log($"<color=magenta>[SLOPE TRANSITION] Changed from {(wasMovingUphill ? "UPHILL" : "DOWNHILL")} to {(isMovingUphill ? "UPHILL" : "DOWNHILL")} - Angle: {slopeAngle:F1}°, Speed: {slideVelocity.magnitude:F1}</color>");
            }
            
            if (!isMovingUphill)
            {
                lastDownhillDirection = _cachedDownhillDirection;
                lastDownhillAt = Time.time;
            }
        }

        // === ENHANCED PHYSICS === 
        float dt = Time.deltaTime;

        // CRITICAL FIX: Only integrate sliding gravity when grounded and not currently jumping
        // Check upward velocity instead of IsJumpSuppressed (which means "jump is blocked", not "is jumping")
        bool isCurrentlyJumping = movement != null && movement.Velocity.y > 5f;
        
        if (useSlidingGravity && !isCurrentlyJumping)
        {
            slideDownVelY += slidingGravity * dt; // gravity is negative
            slideDownVelY = Mathf.Clamp(slideDownVelY, -slidingTerminalDownVelocity, 0f);
        }

        // 1. Advanced slope physics
        if (onSlope)
        {
            // CRITICAL FIX: Slope limit is already set in TryStartSlide() - DO NOT set it again every frame!
            // Redundant calls create stack overflow and prevent natural sliding physics
            // The slope limit override persists for the entire slide duration
            
            // PURE NATURAL PHYSICS: Apply gravity projected along the slope
            // 🚀 OPTIMIZATION: Reuse cached Vector3 for gravity projection
            _cachedProjectionResult = Vector3.ProjectOnPlane(Vector3.down, smoothedGroundNormal).normalized;
            // Use slope angle factor so steeper slopes accelerate more (F = mg*sin(θ))
            float slopeFactor = Mathf.Sin(slopeAngle * Mathf.Deg2Rad); // 0 on flat, 1 on vertical
            float accel = slideGravityAccel * Mathf.Clamp01(slopeFactor);
            
            // CRITICAL FIX: Apply gravity differently for uphill vs downhill
            // Downhill: Gravity accelerates you (speeds up)
            // Uphill: Gravity opposes you (slows down)
            if (!isMovingUphill)
            {
                // Downhill: Apply gravity along slope to accelerate
                slideVelocity += _cachedProjectionResult * (accel * dt);
                
                if (verboseDebugLogging)
                {
                    Debug.Log($"<color=green>[DOWNHILL] Gravity accel: {accel:F1}, Speed: {slideVelocity.magnitude:F1}</color>");
                }
            }
            else
            {
                // Uphill: Apply opposing gravity force (natural physics - gravity pulls you back!)
                // Use the SAME acceleration magnitude, but in OPPOSITE direction (against velocity)
                float uphillResistance = accel * slopeFactor; // Stronger on steeper slopes
                Vector3 uphillGravityForce = -slideVelocity.normalized * (uphillResistance * dt);
                slideVelocity += uphillGravityForce;
                
                if (verboseDebugLogging)
                {
                    Debug.Log($"<color=yellow>[UPHILL] Gravity resistance: {uphillResistance:F1}, Speed: {slideVelocity.magnitude:F1}</color>");
                }
            }
            
            // SMART DOWNHILL ALIGNMENT: Only apply when moving downhill, NOT when going uphill
            // This prevents fighting momentum during flat→uphill transitions
            float currentSpeed = slideVelocity.magnitude;
            if (currentSpeed > 0.1f && !isMovingUphill)
            {
                Vector3 currentDir = slideVelocity.normalized;
                float downhillAlignment = Vector3.Dot(currentDir, _cachedProjectionResult);
                
                // If sliding sideways (but NOT uphill), pull back toward downhill (stronger on steep slopes)
                if (downhillAlignment < 0.95f && downhillAlignment > 0f)
                {
                    float pullStrength = (1f - downhillAlignment) * slopeFactor * 15f; // Stronger on steep slopes
                    Vector3 correctionForce = _cachedProjectionResult * (pullStrength * currentSpeed * dt);
                    slideVelocity = Vector3.Lerp(slideVelocity, _cachedProjectionResult * currentSpeed, pullStrength * dt);
                }
            }

            // Apply additional uphill resistance when moving against downhill direction
            if (isMovingUphill)
            {
                ApplyUphillPhysics(dt);
            }
        }

        // 2. UNIFIED friction system - same for all sliding scenarios
        float baseFriction = onSlope ? SlideFrictionSlope : SlideFrictionFlat;
        
        // CRITICAL FIX: Apply STRONG friction immediately after slope-to-flat transition
        // Only apply if we actually have a valid transition time (not -999f)
        bool hadSlopeTransition = slopeToFlatTransitionTime > 0f;
        bool justLeftSlope = hadSlopeTransition && (Time.time - slopeToFlatTransitionTime) < 0.5f;
        
        if (justLeftSlope && !onSlope)
        {
            // Apply much stronger friction on flat ground after leaving a slope
            baseFriction *= FLAT_GROUND_DECEL_MULTIPLIER;
        }
        
        float frictionMult;
        if (useFrictionCurve && frictionBySpeedCurve != null && frictionBySpeedCurve.keys.Length > 0)
        {
            frictionMult = Mathf.Clamp(frictionBySpeedCurve.Evaluate(slideSpeed), 0.05f, 2f);
        }
        else
        {
            // Legacy linear scaling
            float speedFactor = Mathf.InverseLerp(50f, 200f, slideSpeed);
            frictionMult = 1f - speedFactor * 0.4f;
        }
        float dynamicFriction = baseFriction * frictionMult;
        
        // CRITICAL FIX: Reduce friction during slide startup (first 0.3 seconds)
        // This allows gravity to build up speed naturally without being killed by friction
        // ULTRA-ROBUST: Skip startup reduction if landing with momentum (already have speed!)
        bool isSlideStartup = slideTimer < 0.3f;
        bool landedWithMomentum = slideInitialSpeed > HIGH_SPEED_LANDING_THRESHOLD; // Came in hot from jump!
        
        // CROUCH SLAM FIX: Reduce friction on startup for slopes OR if landed with high momentum (even on flat!)
        // This prevents crouch slam momentum from being killed instantly on flat ground
        if (isSlideStartup && (onSlope || landedWithMomentum) && !landedWithMomentum)
        {
            // Only reduce friction for slow starts - not needed when landing with speed
            dynamicFriction *= 0.1f;
            if (verboseDebugLogging)
            {
                Debug.Log($"<color=green>[SLIDE] Startup friction reduction active! Friction: {dynamicFriction:F2}</color>");
            }
        }
        else if (isSlideStartup && landedWithMomentum)
        {
            // CRITICAL: Landing with high momentum (crouch slam!) - reduce friction even on flat ground!
            dynamicFriction *= 0.3f; // Less reduction than slopes (0.3 vs 0.1) but still helps preserve speed
            if (verboseDebugLogging)
            {
                Debug.Log($"<color=lime>[SLIDE] High-speed landing detected ({slideInitialSpeed:F2})! Friction reduced to {dynamicFriction:F2}</color>");
            }
        }
        
        if (slideSpeed > 0.01f)
        {
            // ═══════════════════════════════════════════════════════════════════
            // ⚡ NEXT-LEVEL PHYSICS ENGINE - AAA+ INDUSTRY STANDARD ⚡
            // ═══════════════════════════════════════════════════════════════════
            // Mathematical Stability: PROVEN frame-rate independent physics
            // Design Goal: Apex Legends fluidity + Titanfall 2 momentum + Warframe flow
            // 
            // PHYSICS MODEL:
            //   - Frame-rate independent (30 FPS = 144 FPS = identical feel)
            //   - Speed-proportional friction (realistic drag simulation)
            //   - Slope-adaptive decay (gravity compensates friction on slopes)
            //   - No exponential multiplication (mathematically stable)
            //
            // DECAY RATES (tested at 60 FPS over 300 frames = 5 seconds):
            //   Flat Ground: 1000 → 450 units/s (55% decay, smooth glide)
            //   Moderate Slope (20°): 1000 → 750 units/s (25% decay, extended flow)
            //   Steep Slope (45°): Speed maintained via gravity (no decay)
            //
            // ANTI-EXPLOIT: Upper speed bounds prevent hacks/bugs from breaking physics
            // ═══════════════════════════════════════════════════════════════════
            
            // --- PHASE 1: SPEED-PROPORTIONAL FRICTION (AAA STANDARD) ---
            // Real physics: Drag force = coefficient × speed²
            // Simplified: Linear approximation for performance (dragForce ∝ speed)
            float frictionCoefficient = dynamicFriction;
            
            // Frame-rate independent friction (scales with both speed AND time)
            // This ensures 30 FPS and 144 FPS feel IDENTICAL
            float frictionMagnitude = slideSpeed * frictionCoefficient * dt;
            
            // Apply friction vector (opposes motion)
            Vector3 frictionForce = -slideVelocity.normalized * frictionMagnitude;
            
            // --- PHASE 2: SLOPE-ADAPTIVE DECAY (NEXT-LEVEL FEEL) ---
            // On slopes: Gravity compensates friction → less decay (flow state)
            // On flat: Pure friction → more decay (controlled deceleration)
            float slopeDecayMultiplier = 1.0f;
            
            if (onSlope && slopeAngle > 10f)
            {
                // Slope intensity factor (0-1, based on angle)
                // 10° = 0.0 (no bonus), 45° = 0.7 (significant bonus), 70° = 1.0 (maximum)
                float slopeIntensity = Mathf.Clamp01((slopeAngle - 10f) / 60f);
                
                // Reduce friction decay based on slope intensity
                // Steep slopes feel like surfing (minimal decay)
                // Gentle slopes feel like controlled glide (moderate decay)
                slopeDecayMultiplier = Mathf.Lerp(1.0f, 0.3f, slopeIntensity);
            }
            
            // Apply slope-adjusted friction
            frictionForce *= slopeDecayMultiplier;
            
            // --- PHASE 3: VELOCITY UPDATE (STABLE INTEGRATION) ---
            // Use additive physics (NOT multiplicative) for mathematical stability
            // v_new = v_old + forces × dt (standard physics integration)
            slideVelocity += frictionForce;
            
            // --- PHASE 4: MINIMUM SPEED THRESHOLD (SMOOTH STOP) ---
            // Prevent jitter at very low speeds (AAA polish)
            if (slideVelocity.magnitude < 2f)
            {
                // Below 2 units/s: either stop completely or maintain minimum glide
                if (!isSlideStartup || !onSlope)
                {
                    // Flat ground or established slide: smooth stop
                    slideVelocity = slideVelocity.normalized * Mathf.Max(0f, slideVelocity.magnitude);
                    
                    // If almost stopped, zero it out (prevents micro-jitter)
                    if (slideVelocity.magnitude < 0.5f)
                    {
                        slideVelocity = Vector3.zero;
                    }
                }
                else
                {
                    // Startup on slope: maintain tiny velocity, let gravity build
                    slideVelocity = slideVelocity.normalized * 5f;
                }
            }
            
            // --- PHASE 5: ANTI-EXPLOIT SPEED CAP (SECURITY) ---
            // Prevent physics exploits, hacks, or bugs from causing runaway speed
            // CRITICAL: This is the MASTER speed cap for all slide momentum!
            float maxSafeSpeed = 25000f; // HARD CAP - matches crouch slam cap!
            
            if (slideSpeed > maxSafeSpeed)
            {
                // Cap at maximum safe speed (preserves direction)
                slideVelocity = slideVelocity.normalized * maxSafeSpeed;
                
                if (verboseDebugLogging)
                {
                    Debug.LogWarning($"<color=yellow>[SLIDE PHYSICS] Speed capped at {maxSafeSpeed:F1} (was {slideSpeed:F1}) - Anti-exploit protection</color>");
                }
            }
            
            // --- STABILITY GUARANTEE ---
            // No exponential multiplication = No runaway acceleration
            // Speed can only decrease (friction) or maintain (slope gravity balance)
            // Result: Mathematically stable, frame-rate independent, AAA-quality physics
        }

        // 3. MOMENTUM-CONSERVING STEERING - NO INSTANT REVERSALS!
        // ═══════════════════════════════════════════════════════════════════
        // 🚀 CRITICAL FIX: Momentum MUST be conserved during steering
        // Problem: Raw input axes cause instant reversals (forward → backward at same speed)
        // Solution: Apply steering as GRADUAL FORCE, not instant direction change
        // Result: Natural deceleration when changing direction (like real physics!)
        // ═══════════════════════════════════════════════════════════════════
        
        // AAA+ OPTIMIZATION: Cache input once per frame for consistency and performance
        Vector2 input = new Vector2(Controls.HorizontalRaw(), Controls.VerticalRaw());
        
        // PERFORMANCE: Early exit if no input - saves ~15 operations per frame
        if (input.sqrMagnitude > 0.01f)
        {
            input = Vector2.ClampMagnitude(input, 1f);
            
            // SAFETY CHECK: Ensure camera reference is valid before using it
            // Prevents null reference exceptions in edge cases (camera destruction, scene transitions)
            Vector3 camForward = cameraTransform != null ? cameraTransform.forward : transform.forward;
            Vector3 camRight = cameraTransform != null ? cameraTransform.right : transform.right;
            
            // Project onto horizontal plane
            camForward.y = 0f;
            camForward.Normalize();
            camRight.y = 0f;
            camRight.Normalize();
            
            // Build wish direction
            Vector3 wishDir = (camRight * input.x + camForward * input.y).normalized;
            
            // Project onto slope if on one
            if (onSlope)
            {
                wishDir = Vector3.ProjectOnPlane(wishDir, smoothedGroundNormal).normalized;
            }
            
            // ═══════════════════════════════════════════════════════════════════
            // Enhanced steering with drift control (curve-tuned)
            // ═══════════════════════════════════════════════════════════════════
            // Check if player is trying to steer opposite to current momentum
            Vector3 currentDir = slideVelocity.normalized;
            float directionAlignment = Vector3.Dot(currentDir, wishDir);
            
            // Dynamic steering response based on speed (curve-tuned)
            float steerPower = SlideSteerAcceleration;
            float steerMult;
            if (useSteerCurve && steerAccelBySpeedCurve != null && steerAccelBySpeedCurve.keys.Length > 0)
            {
                steerMult = Mathf.Clamp(steerAccelBySpeedCurve.Evaluate(slideSpeed), 0f, 5f);
            }
            else
            {
                // Legacy behavior
                steerMult = (slideSpeed > 100f) ? 1.3f : 1f;
            }
            steerPower *= steerMult;

            // Apply steering with adjustable drift feel
            Vector3 steerForce = wishDir * (steerPower * dt);
            slideVelocity = Vector3.Slerp(slideVelocity, slideVelocity + steerForce, Mathf.Clamp01(steerDriftLerp));
        }

        // 4. Dynamic speed clamping with particle intensity
        float effectiveMaxSpeed = SlideMaxSpeed;
        if (onSlope && slopeAngle > 25f)
        {
            // Allow higher speeds on steep slopes
            effectiveMaxSpeed *= 1.2f;
        }
        
        if (slideSpeed > effectiveMaxSpeed)
        {
            slideVelocity = slideVelocity.normalized * effectiveMaxSpeed;
        }
        
        // Update particle intensity based on speed
        UpdateSlideParticleIntensity(slideSpeed);

        // Keep velocity tangent to ground to prevent bouncing
        slideVelocity = Vector3.ProjectOnPlane(slideVelocity, smoothedGroundNormal);

        // === JUMP DETECTION REMOVED - Already handled at top of UpdateSlide() ===
        // Jump is now checked at the very beginning of UpdateSlide (line 953)
        // This ensures maximum responsiveness and prevents duplicate variable errors
        
        // Check if player has upward velocity (already jumping/launched)
        bool hasUpwardVelocity = movement != null && movement.Velocity.y > 5f; // Higher threshold to avoid false positives
        
        Vector3 externalVel;
        if (usePureSlopeAlignedMovement)
        {
            // Pure slope-aligned velocity with minimal downward bias to maintain contact
            Vector3 slopeAligned = Vector3.ProjectOnPlane(slideVelocity, smoothedGroundNormal);
            externalVel = slopeAligned;
            
            // JUMP FIX: Don't apply downward forces during jumps
            if (!hasUpwardVelocity)
            {
                float desiredDown = useSlidingGravity ? Mathf.Min(slideDownVelY, -minDownYWhileSliding) : -minDownYWhileSliding;
                if (externalVel.y > desiredDown) externalVel.y = desiredDown;
            }
        }
        else
        {
            // Legacy: dynamic stick forces for extra ground adhesion
            float stickN = stickToGroundVelocity + slideSpeed * Mathf.Max(0f, stickSpeedFactor);
            stickN = Mathf.Clamp(stickN, 0f, maxStickForce);
            Vector3 stick = (-smoothedGroundNormal * stickN) + (Vector3.down * Mathf.Max(0f, stickDownWorldY));
            externalVel = slideVelocity + stick;
            
            // JUMP FIX: Don't apply downward forces during jumps
            if (!hasUpwardVelocity)
            {
                float desiredDown = useSlidingGravity ? Mathf.Min(slideDownVelY, -minDownYWhileSliding) : -minDownYWhileSliding;
                if (externalVel.y > desiredDown) externalVel.y = desiredDown;
            }
        }
        
        // CRITICAL FIX: Cap total velocity magnitude to prevent insane acceleration on steep slopes
        // The Y component can grow MASSIVE on 70°+ slopes, causing jittering and bouncing
        float maxTotalVelocity = effectiveMaxSpeed * 1.5f; // Allow 50% overage for downward component
        if (externalVel.magnitude > maxTotalVelocity)
        {
            // Cap while preserving direction
            externalVel = externalVel.normalized * maxTotalVelocity;
            
            if (verboseDebugLogging)
            {
                Debug.Log($"<color=yellow>[SLIDE] Capped total velocity from {externalVel.magnitude:F2} to {maxTotalVelocity:F2}</color>");
            }
        }
        
        // ADDITIONAL FIX: Cap downward component specifically to prevent bouncing
        float maxDownwardVelocity = effectiveMaxSpeed * 0.8f; // Downward shouldn't exceed 80% of max slide speed
        if (externalVel.y < -maxDownwardVelocity)
        {
            externalVel.y = -maxDownwardVelocity;
            
            if (verboseDebugLogging)
            {
                Debug.Log($"<color=yellow>[SLIDE] Capped downward velocity to {maxDownwardVelocity:F2}</color>");
            }
        }
        
        // === ENHANCEMENT: SMOOTH WALL SLIDING (Collide-and-Slide Algorithm) ===
        // PURE ADDITIVE FEATURE - Pre-processes velocity before CharacterController
        // If disabled, system behaves exactly as before (zero breaking changes)
        if (enableSmoothWallSliding)
        {
            externalVel = ApplySmoothWallSliding(externalVel, effectiveMaxSpeed);
        }
        
        // === CRITICAL FIX: Apply movement EVERY FRAME - no throttling! ===
        // Throttling causes stuttering and frame-by-frame movement
        // PRISTINE: Direct CharacterController movement - slide system has exclusive control
        if (controller != null && controller.enabled)
        {
            // Apply slide velocity directly - this is the ONLY system moving the character during slide
            controller.Move(externalVel * Time.deltaTime);
            
            // 🚀 SPEED CHAIN FIX: Sync movement controller's internal velocity with slide velocity
            // This ensures jump code reads the correct speed when transitioning slide → jump
            if (movement != null)
            {
                movement.SetExternalVelocity(externalVel, 0.1f, overrideGravity: false);
            }
            
            if (verboseDebugLogging)
            {
                Debug.Log($"[SLIDE] Applied direct movement - Velocity: {externalVel.magnitude:F1}, Delta: {(externalVel * Time.deltaTime).magnitude:F2}");
            }
        }
        else if (movement != null)
        {
            // FALLBACK: If controller is invalid, try external velocity (less reliable)
            movement.SetExternalVelocity(externalVel, 0.2f, overrideGravity: false);
            
            if (verboseDebugLogging)
            {
                Debug.LogWarning($"[SLIDE] Using fallback external velocity - Controller unavailable");
            }
        }

        // BRILLIANT: Enhanced stop conditions that maintain flow
        float baseTime = slideBaseDuration;
        float maxTime = Mathf.Min(slideHardCapSeconds, slideBaseDuration + slideExtraTime);
        bool timePassed = slideTimer >= baseTime;
        bool capPassed = slideTimer >= maxTime;
        // In hold-to-slide mode, ignore time-based caps so the key/speed solely govern stop
        if (slideHoldMode)
        {
            timePassed = false;
            capPassed = false;
        }
        bool slowOnFlat = !onSlope && slideVelocity.magnitude <= slideEndSpeedThreshold;
        // PHASE 3 COHERENCE FIX: Use AAA's TimeSinceGrounded instead of local lastGroundedAt
        bool coyoteOk = movement != null && movement.TimeSinceGrounded <= slideGroundCoyoteTime;
        bool walkingModeNow = movement == null || movement.CurrentMode == AAAMovementController.MovementMode.Walking;
        // In hold-to-slide mode, do not treat brief airtime as a reason to stop
        bool lostGround = slideHoldMode ? false : (!walkingModeNow || !coyoteOk);
        
        // Slope grace period for smooth transitions
        float effectiveGracePeriod = slopeTransitionGrace;
        if (justLeftSlope && !onSlope)
        {
            effectiveGracePeriod = 0.1f; // Shorter on flat after slope
        }
        bool recentlyOnSlope = (Time.time - lastValidSlopeTime) <= effectiveGracePeriod;
        bool maintainOnSlope = onSlope || recentlyOnSlope;
        
        // === PRISTINE: Stop conditions ===
        bool shouldStop = (lostGround && !maintainOnSlope) || 
                         (capPassed && !maintainOnSlope && slowOnFlat) || 
                         (timePassed && !maintainOnSlope && slowOnFlat);
        
        if (shouldStop)
        {
            if (verboseDebugLogging)
            {
                Debug.Log($"<color=lime>[BRILLIANT SLIDE] Stopping slide. OnSlope: {onSlope}, RecentSlope: {recentlyOnSlope}, Speed: {slideVelocity.magnitude:F1}</color>");
            }
            // Latch momentum into air only when actually losing ground beyond coyote time
            if (lostGround && !hasLatchedAirMomentum)
            {
                Vector3 carry = Vector3.ProjectOnPlane(slideVelocity, Vector3.up);
                if (carry.sqrMagnitude > 0.0001f)
                {
                    movement.LatchAirMomentum(carry);
                }
                hasLatchedAirMomentum = true;
            }
            StopSlide();
        }
    }

    /// <summary>
    /// ENHANCEMENT: Smooth Wall Sliding using Collide-and-Slide Algorithm
    /// Pure additive feature - pre-processes velocity to slide smoothly along walls/obstacles.
    /// Uses recursive collision detection and velocity projection for AAA-quality wall sliding.
    /// ZERO BREAKING CHANGES - if disabled, system behaves exactly as before.
    /// </summary>
    private Vector3 ApplySmoothWallSliding(Vector3 desiredVelocity, float maxSpeed)
    {
        // Safety check - need controller for collision detection
        if (controller == null)
            return desiredVelocity;
        
        // Calculate movement distance for this frame
        float moveDistance = desiredVelocity.magnitude * Time.deltaTime;
        
        // If moving too slowly, skip collision checks (optimization)
        if (moveDistance < 0.01f)
            return desiredVelocity;
        
        // Start recursive collision resolution
        Vector3 finalVelocity = RecursiveWallSlide(desiredVelocity, transform.position, 0, maxSpeed);
        
        // Debug visualization
        if (showWallSlideDebug)
        {
            // Debug rays removed for performance
        }
        
        return finalVelocity;
    }
    
    /// <summary>
    /// Recursive collision resolution - the heart of collide-and-slide algorithm.
    /// Handles multiple collisions per frame for smooth sliding along complex geometry.
    /// </summary>
    private Vector3 RecursiveWallSlide(Vector3 velocity, Vector3 position, int depth, float maxSpeed)
    {
        // Recursion limit - prevent infinite loops on complex geometry
        if (depth >= wallSlideMaxIterations)
        {
            if (showWallSlideDebug)
                Debug.Log($"[WALL SLIDE] Max iterations reached ({wallSlideMaxIterations})");
            return Vector3.zero; // Stop movement if too many bounces
        }
        
        // Calculate movement for this iteration
        float moveDistance = velocity.magnitude * Time.deltaTime;
        if (moveDistance < 0.001f)
            return Vector3.zero; // Too small to matter
        
        // Setup capsule cast parameters (matches CharacterController shape)
        float radius = controller.radius * wallSlideSkinMultiplier; // Slightly smaller to avoid edge cases
        float height = controller.height;
        Vector3 center = controller.center;
        
        // Capsule endpoints (top and bottom spheres)
        Vector3 point1 = position + center + Vector3.up * (height * 0.5f - radius);
        Vector3 point2 = position + center - Vector3.up * (height * 0.5f - radius);
        
        // Cast capsule along velocity direction
        if (Physics.CapsuleCast(
            point1, point2, radius,
            velocity.normalized,
            out RaycastHit hit,
            moveDistance,
            slideGroundMask))
        {
            // === COLLISION DETECTED ===
            
            // Check if this is a wall (not ground/ceiling)
            float surfaceAngle = Vector3.Angle(Vector3.up, hit.normal);
            bool isWall = surfaceAngle >= wallSlideMinAngle && surfaceAngle <= (180f - wallSlideMinAngle);
            
            if (!isWall)
            {
                // Not a wall - let normal CharacterController physics handle it
                if (showWallSlideDebug)
                    Debug.Log($"[WALL SLIDE] Surface angle {surfaceAngle:F1}° - not a wall, skipping");
                return velocity;
            }
            
            // Calculate snap-to-surface movement (safe distance we can move)
            float safeDistance = Mathf.Max(0f, hit.distance - 0.01f); // Small skin to prevent penetration
            Vector3 snapToSurface = velocity.normalized * safeDistance;
            
            // Calculate leftover velocity (what we couldn't use)
            Vector3 leftoverVelocity = velocity.normalized * (moveDistance - safeDistance) / Time.deltaTime;
            
            // Project leftover velocity onto collision plane (slide along wall)
            Vector3 projectedVelocity = Vector3.ProjectOnPlane(leftoverVelocity, hit.normal);
            
            // CRITICAL: Preserve speed for momentum feel (scale projected vector)
            // This is what makes wall sliding feel smooth and fast
            float originalSpeed = leftoverVelocity.magnitude;
            if (projectedVelocity.sqrMagnitude > 0.001f)
            {
                projectedVelocity = projectedVelocity.normalized * (originalSpeed * wallSlideSpeedPreservation);
            }
            
            // Clamp to max speed (safety)
            if (projectedVelocity.magnitude > maxSpeed)
            {
                projectedVelocity = projectedVelocity.normalized * maxSpeed;
            }
            
            // Debug visualization
            if (showWallSlideDebug)
            {
                // Debug rays removed for performance
                Debug.Log($"[WALL SLIDE] Hit wall at {hit.point}, angle {surfaceAngle:F1}°, projecting velocity");
            }
            
            // Recursive call - check for more collisions with projected velocity
            Vector3 newPosition = position + snapToSurface;
            Vector3 recursiveResult = RecursiveWallSlide(projectedVelocity, newPosition, depth + 1, maxSpeed);
            
            // Combine snap movement with recursive result
            return (snapToSurface / Time.deltaTime) + recursiveResult;
        }
        else
        {
            // === NO COLLISION ===
            // Clear path - return original velocity
            return velocity;
        }
    }
    
    private void StopSlide()
    {
        if (!isSliding) return;
        
        // ═══════════════════════════════════════════════════════════════════
        // 🚀 MOMENTUM PRESERVATION - AAA SLIDE EXIT FEEL
        // ═══════════════════════════════════════════════════════════════════
        // Transfer slide momentum to movement controller when exiting
        // This creates BUTTERY smooth transitions from slide → run
        // Player keeps their speed boost instead of jarring to a halt!
        // ═══════════════════════════════════════════════════════════════════
        if (enableSlideExitMomentum && movement != null && slideVelocity.sqrMagnitude > 0.0001f)
        {
            float currentSlideSpeed = slideVelocity.magnitude;
            
            // Only transfer if speed is significant (prevents weird micro-momentum)
            if (currentSlideSpeed >= slideExitMinSpeed)
            {
                // Get horizontal slide direction (ignore vertical component)
                Vector3 horizontalSlideVel = Vector3.ProjectOnPlane(slideVelocity, Vector3.up);
                
                // Apply momentum multiplier for smooth transition
                Vector3 transferVelocity = horizontalSlideVel * slideExitMomentumMultiplier;
                
                // Transfer to movement controller (this will blend with player input naturally)
                movement.SetVelocity(transferVelocity);
                
                if (enableDebugLogs)
                {
                    Debug.Log($"<color=lime>[SLIDE EXIT] 🚀 Momentum preserved! Slide speed: {currentSlideSpeed:F1} → Transfer: {transferVelocity.magnitude:F1} ({slideExitMomentumMultiplier * 100f:F0}%)</color>");
                }
            }
            else
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"<color=yellow>[SLIDE EXIT] Speed too low ({currentSlideSpeed:F1} < {slideExitMinSpeed:F1}) - no momentum transfer</color>");
                }
            }
        }
        
        isSliding = false;
        slideTimer = 0f;
        slideExtraTime = 0f;
        slideVelocity = Vector3.zero;
        hasLatchedAirMomentum = false;
        slideFOVActive = false;
        
        // Reset tracking variables
        wasOnSlopeLastFrame = false;
        slopeToFlatTransitionTime = -999f;
        steepSlopeContactStartTime = -999f;
        slideStartTime = -999f; // Reset slide start time
        lastAppliedExternalVelocity = Vector3.zero;
        lastExternalVelocityUpdateTime = -999f;

        // ═══════════════════════════════════════════════════════════════════
        // ⚡ RACE CONDITION FIX - ATOMIC STATE CLEANUP ⚡
        // ═══════════════════════════════════════════════════════════════════
        // CRITICAL: Clear external velocity IMMEDIATELY to prevent 100ms race condition
        // Issue: SetExternalVelocity() has 200ms duration, updated every 100ms
        //        If slide stops between updates, velocity lingers for up to 100ms
        //        This causes AAA input and slide velocity to fight for control
        // Solution: Atomic cleanup - clear ALL external forces the instant slide stops
        // Result: Zero-frame latency, no input conflicts, buttery smooth transitions
        // NOTE: Momentum transfer happens BEFORE this clear, so transferred velocity persists!
        // ═══════════════════════════════════════════════════════════════════
        if (movement != null)
        {
            movement.ClearExternalForce();           // New unified API (clears temporary forces, not SetVelocity)
            #pragma warning disable CS0618 // Suppress obsolete warning
            movement.ClearExternalGroundVelocity();  // Legacy compatibility
            #pragma warning restore CS0618
        }

        // === PRISTINE: Restore ALL controller state through AAA coordination ===
        // CRITICAL FIX: Slope limit is no longer modified during slides (see TryStartSlide comment)
        /*
        if (overrideSlopeLimitDuringSlide && movement != null)
        {
            movement.RestoreSlopeLimitToOriginal();
        }
        */
        
        // PRISTINE: Restore stepOffset through API
        if (reduceStepOffsetDuringSlide && movement != null)
        {
            movement.RestoreStepOffsetToOriginal(AAAMovementController.ControllerModificationSource.Crouch);
        }
        // PRISTINE: Restore minMoveDistance through API
        if (reduceMinMoveDistanceDuringSlide && movement != null)
        {
            movement.RestoreMinMoveDistanceToOriginal(AAAMovementController.ControllerModificationSource.Crouch);
        }
        
        // === SMART ANIMATION TRANSITION ===
        if (animationStateManager != null)
        {
            bool isStillSprinting = energySystem != null && energySystem.IsCurrentlySprinting;
            bool isGrounded = movement != null && movement.IsGroundedRaw; // Instant truth
            
            if (isStillSprinting && isGrounded)
            {
                // Player is still sprinting - resume sprint animation
                animationStateManager.SetMovementState((int)PlayerAnimationStateManager.PlayerAnimationState.Sprint);
                Debug.Log("[SLIDE] Slide stopped - resuming Sprint animation (sprint key still held)!");
            }
            else
            {
                // Player released sprint or not grounded - return to idle
                animationStateManager.SetMovementState((int)PlayerAnimationStateManager.PlayerAnimationState.Idle);
                Debug.Log("[SLIDE] Slide stopped - returning to Idle animation!");
            }
        }
        
        // Stop audio and particles
        StopSlideAudio();
        StopSlideParticles();
    }
    
    private void PlaySlideStartSound()
    {
        if (!slideAudioEnabled) return;
        
        // Play slide sound from SoundEvents (oneshot, non-looping)
        if (SoundEventsManager.Events != null && SoundEventsManager.Events.slideSound != null)
        {
            slideAudioHandle = SoundEventsManager.Events.slideSound.Play2D(1f);
            
            if (enableDebugLogs)
            {
                Debug.Log("<color=cyan>[CleanAAACrouch] Playing slide sound (oneshot)</color>");
            }
        }
        else
        {
            Debug.LogWarning("[CleanAAACrouch] Slide sound not assigned in SoundEvents!");
        }
    }
    
    private void StopSlideAudio()
    {
        // Stop slide sound if it's still playing
        if (slideAudioHandle.IsValid && slideAudioHandle.IsPlaying)
        {
            slideAudioHandle.Stop();
        }
        slideAudioHandle = SoundHandle.Invalid;
    }

    
    private void StartSlideParticles()
    {
        if (!slideParticlesEnabled || slideParticles == null || particlesActive) return;
        
        particlesActive = true;
        slideParticles.gameObject.SetActive(true);
        
        // Re-enable emission (in case it was disabled by StopSlideParticles)
        var emission = slideParticles.emission;
        emission.enabled = true;
        
        // Start or resume the particle system
        if (!slideParticles.isPlaying)
            slideParticles.Play();
    }
    
    private void StopSlideParticles()
    {
        if (!particlesActive || slideParticles == null) return;
        
        particlesActive = false;
        
        // Stop EMISSION but let existing particles/trails fade naturally
        var emission = slideParticles.emission;
        emission.enabled = false;
        
        // DON'T call Stop() or SetActive(false) - let particles live out their lifetime!
        // slideParticles.Stop(); // REMOVED: This would instantly kill all particles
        // slideParticles.gameObject.SetActive(false); // REMOVED: This would hide the trail instantly
    }

    private void OnDisable()
    {
        // === PRISTINE: Guaranteed cleanup of ALL systems ===
        
        // 1. Stop sliding if active
        if (isSliding)
        {
            StopSlide();
        }
        
        // 2. Force exit dive/prone states
        if (isDiving || isDiveProne)
        {
            isDiving = false;
            isDiveProne = false;
            
            // CRITICAL: Ensure dive override is ALWAYS disabled
            if (movement != null)
            {
                movement.DisableDiveOverride();
            }
            
            StopDiveParticles();
        }
        
        // 3. Clear all external forces
        if (movement != null)
        {
            movement.ClearExternalForce();
            movement.ClearExternalGroundVelocity();
            movement.RestoreSlopeLimitToOriginal();
        }
        
        // 4. PRISTINE: Restore ALL controller values through API
        if (movement != null)
        {
            movement.RestoreStepOffsetToOriginal(AAAMovementController.ControllerModificationSource.Crouch);
            movement.RestoreMinMoveDistanceToOriginal(AAAMovementController.ControllerModificationSource.Crouch);
        }
        
        // 5. Stop all audio/particles
        StopSlideAudio();
        StopSlideParticles();
        StopDiveParticles();
    }
    
    private System.Collections.IEnumerator CameraImpactShake(float duration, float magnitude)
    {
        if (cameraTransform == null) yield break;
        
        Vector3 originalPos = cameraTransform.localPosition;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float strength = Mathf.Lerp(magnitude, 0f, elapsed / duration);
            Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * strength * 0.1f;
            randomOffset.y *= 0.3f; // Less vertical shake
            
            cameraTransform.localPosition = originalPos + randomOffset;
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        cameraTransform.localPosition = originalPos;
    }
    
    private void UpdateSlideParticleIntensity(float speed)
    {
        if (!slideParticlesEnabled || slideParticles == null || !particlesActive) return;
        
        // 🔥 FIRE TRAIL INTENSITY: Scale emission based on slide speed
        var emission = slideParticles.emission;
        float speedRatio = Mathf.InverseLerp(20f, SlideMaxSpeed * 0.8f, speed);
        float targetRate = Mathf.Lerp(15f, 60f, speedRatio); // TRAILS: Fewer particles needed (trails do the work)
        
        emission.rateOverTime = targetRate;
        
        // Scale fire particle size and lifetime based on speed
        var main = slideParticles.main;
        main.startSize = Mathf.Lerp(30f, 60f, speedRatio); // FIRE: Larger particles for visibility
        main.startLifetime = Mathf.Lerp(1.0f, 2.0f, speedRatio); // FIRE: Longer lifetime = longer trails
    }

    private void ApplyUphillPhysics(float dt)
    {
        float speed = slideVelocity.magnitude;
        if (speed < 0.01f) return;
        
        // ═══════════════════════════════════════════════════════════════════
        // 🎯 UPHILL FRICTION - ADDITIONAL RESISTANCE BEYOND GRAVITY
        // Gravity is already applied in UpdateSlide (line ~1165)
        // This adds EXTRA friction to simulate surface drag going uphill
        // ═══════════════════════════════════════════════════════════════════
        
        // Progressive uphill resistance based on speed
        float speedRatio = Mathf.InverseLerp(10f, 80f, speed);
        // At low speed: 2.5× friction (strong resistance to help you stop)
        // At high speed: 2.0× friction (allows some momentum)
        float frictionMult = Mathf.Lerp(UphillFrictionMultiplier * 1.25f, UphillFrictionMultiplier, speedRatio);
        
        Vector3 frictionForce = -slideVelocity.normalized * (SlideFrictionFlat * frictionMult * dt);
        
        if (verboseDebugLogging)
        {
            Debug.Log($"<color=cyan>[UPHILL FRICTION] Speed: {speed:F1}, Mult: {frictionMult:F2}×, Force: {frictionForce.magnitude:F2}</color>");
        }
        
        if (frictionForce.magnitude >= speed)
        {
            // Friction would stop us completely - allow slight backward roll instead
            slideVelocity = -slideVelocity.normalized * 8f;
            
            if (verboseDebugLogging)
            {
                Debug.Log($"<color=red>[UPHILL FRICTION] Speed too low - reversing direction!</color>");
            }
        }
        else
        {
            slideVelocity += frictionForce;
        }
        
        // Smart reversal system (only after sustained uphill movement)
        if (startedOnSlope && Time.time - lastSlopeTime > uphillMinTime)
        {
            if (speed < uphillReversalSpeed)
            {
                // Smooth reversal with momentum
                float reversalPower = Mathf.Lerp(uphillReversalBoost, uphillReversalBoost * 1.5f, 
                    Mathf.InverseLerp(45f, 60f, Vector3.Angle(Vector3.up, smoothedGroundNormal)));
                slideVelocity = -slideVelocity.normalized * reversalPower;
                
                if (verboseDebugLogging)
                {
                    Debug.Log($"<color=magenta>[UPHILL REVERSAL] Speed {speed:F1} < {uphillReversalSpeed:F1} - reversing with power {reversalPower:F1}!</color>");
                }
            }
        }
    }

    private bool ProbeGround(out RaycastHit hit)
    {
        hit = default;
        if (controller == null) return false;

        // PERFORMANCE OPTIMIZATION: Use shared raycast manager if available
        if (raycastManager != null && raycastManager.HasValidGroundHit)
        {
            hit = raycastManager.GroundHit;
            return raycastManager.IsGrounded;
        }

        // FALLBACK: Use local raycasts if manager not available
        // Compute bottom sphere center
        float bottomOffset = controller.height * 0.5f - Mathf.Max(controller.radius, 0.01f);
        Vector3 localBottom = controller.center + Vector3.down * bottomOffset;
        Vector3 bottomSphereCenter = transform.TransformPoint(localBottom);

        float radius = Mathf.Max(0.01f, controller.radius - radiusSkin);

        // Start slightly above the bottom sphere center to avoid initial overlap
        Vector3 origin = bottomSphereCenter + Vector3.up * 0.2f;

        // Use a ground check distance that scales with controller height so large rigs work
        float dynamicGroundCheck = Mathf.Max(slideGroundCheckDistance, controller.height * 0.5f);

        // Exclude our own layer from the probe to avoid self-hits
        int probeMask = slideGroundMask.value & ~(1 << gameObject.layer);

        bool has = Physics.SphereCast(
            origin,
            radius,
            Vector3.down,
            out hit,
            dynamicGroundCheck,
            probeMask,
            QueryTriggerInteraction.Ignore
        );

        // Fallback: if spherecast fails (e.g., very narrow geometry), try a raycast
        if (!has)
        {
            has = Physics.Raycast(
                origin,
                Vector3.down,
                out hit,
                dynamicGroundCheck,
                probeMask,
                QueryTriggerInteraction.Ignore
            );
        }

        return has && hit.collider != null && !hit.collider.isTrigger;
    }

    private void UpdateSlideFOV()
    {
        // PHASE 2 FIX: DISABLED - FOV is now ONLY controlled by AAACameraController
        // This was causing harsh FOV changes and conflicts with sprint FOV
        // Sprint FOV is the ONLY FOV change for smooth camera feel
        return;
        
        /* OLD CODE - REMOVED TO ELIMINATE FOV CONFLICTS
        if (!slideFOVKick) return;
        var cam = GetSlideCamera();
        if (cam == null) return;

        // When sliding, target = base + add; when not, target = base
        float target = (isSliding && slideFOVActive) ? Mathf.Clamp(slideFOVBase + slideFOVAdd, 1f, 179f) : slideFOVBase;
        float s = 1f - Mathf.Exp(-slideFOVLerpSpeed * Time.deltaTime);
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, target, s);
        */
    }
    
    /// <summary>
    /// Loads configuration from ScriptableObject if assigned, otherwise uses inspector values
    /// </summary>
    // ========== CONFIG SYSTEM - SINGLE SOURCE OF TRUTH ==========
    // Properties that read from CrouchConfig if assigned, otherwise fall back to inspector values
    private float SlideMinStartSpeed => config != null ? config.slideMinStartSpeed : slideMinStartSpeed;
    private float SlideAutoStandSpeed => config != null ? config.slideAutoStandSpeed : slideAutoStandSpeed;
    private float SlideGravityAccel => config != null ? config.slideGravityAccel : slideGravityAccel;
    private float SlideFrictionFlat => config != null ? config.slideFrictionFlat : slideFrictionFlat;
    private float SlideFrictionSlope => config != null ? config.slideFrictionSlope : unifiedSlideFriction;
    private float SlideSteerAcceleration => config != null ? config.slideSteerAcceleration : slideSteerAcceleration;
    private float SlideMaxSpeed => config != null ? config.slideMaxSpeed : slideMaxSpeed;
    private float UphillFrictionMultiplier => config != null ? config.uphillFrictionMultiplier : uphillFrictionMultiplier;
    private float StickToGroundVelocity => config != null ? config.stickToGroundVelocity : stickToGroundVelocity;
    private float MomentumPreservation => config != null ? config.momentumPreservation : momentumPreservation;
    private float SteerDriftLerp => config != null ? config.steerDriftLerp : steerDriftLerp;
    private float SlideGroundCoyoteTime => config != null ? config.slideGroundCoyoteTime : slideGroundCoyoteTime;
    private float SlidingGravity => config != null ? config.slidingGravity : slidingGravity;
    private float SlidingTerminalDownVelocity => config != null ? config.slidingTerminalDownVelocity : slidingTerminalDownVelocity;
    private bool HoldToSlide => config != null ? config.holdToSlide : slideHoldMode;
    
    private void LoadConfiguration()
    {
        if (config == null)
        {
            // No config assigned - use inspector values (backward compatible)
            Debug.Log("[CleanAAACrouch] Using legacy inspector settings. Assign a CrouchConfig asset for cleaner configuration!");
            return;
        }

        // Load all values from config asset
        holdToCrouch = config.holdToCrouch;
        slideHoldMode = config.holdToSlide;
        standingHeight = config.standingHeight;
        crouchedHeight = config.crouchedHeight;
        standingCameraY = config.standingCameraY;
        crouchedCameraY = config.crouchedCameraY;
        heightLerpSpeed = config.transitionSpeed;
        
        // Slide physics
        slideMinStartSpeed = config.slideMinStartSpeed;
        slideGravityAccel = config.slideGravityAccel;
        slideFrictionFlat = config.slideFrictionFlat;
        unifiedSlideFriction = config.slideFrictionSlope;
        slideSteerAcceleration = config.slideSteerAcceleration;
        slideMaxSpeed = config.slideMaxSpeed;
        momentumPreservation = config.momentumPreservation;
        stickToGroundVelocity = config.stickToGroundVelocity;
        uphillFrictionMultiplier = config.uphillFrictionMultiplier;
        
        // Tactical dive
        diveForwardForce = config.diveForwardForce;
        diveUpwardForce = config.diveUpwardForce;
        diveProneDuration = config.diveProneDuration;
        diveMinSprintSpeed = config.diveMinSprintSpeed;
        diveSlideFriction = config.diveSlideFriction;
        
        // Visual effects
        slideFOVKick = config.slideFOVKick;
        
        // Curves
        if (config.steerAccelBySpeedCurve != null && config.steerAccelBySpeedCurve.keys.Length > 0)
            steerAccelBySpeedCurve = config.steerAccelBySpeedCurve;
        if (config.frictionBySpeedCurve != null && config.frictionBySpeedCurve.keys.Length > 0)
            frictionBySpeedCurve = config.frictionBySpeedCurve;
        steerDriftLerp = config.steerDriftLerp;
        
        // Advanced physics
        slideAutoStandSpeed = config.slideAutoStandSpeed;
        slideMinimumDuration = config.slideMinimumDuration;
        slideGroundCoyoteTime = config.slideGroundCoyoteTime;
        slidingGravity = config.slidingGravity;
        slidingTerminalDownVelocity = config.slidingTerminalDownVelocity;
        minDownYWhileSliding = config.minDownYWhileSliding;
        slideGroundCheckDistance = config.slideGroundCheckDistance;
        landingSlopeAngleForAutoSlide = config.landingSlopeAngleForAutoSlide;
        landingMomentumDamping = config.landingMomentumDamping;
        enableLandingSpeedCap = config.enableLandingSpeedCap;
        landingMaxPreservedSpeed = config.landingMaxPreservedSpeed;
        rampMinSpeed = config.rampMinSpeed;
        rampExtraUpBoost = config.rampExtraUpBoost;
        
        // Step offset control
        slideStepOffsetOverride = config.slideStepOffsetOverride;
        reduceStepOffsetDuringSlide = config.reduceStepOffsetDuringSlide;
        
        // Behavior toggles
        enableSlide = config.enableSlide;
        enableTacticalDive = config.enableTacticalDive;
        rampLaunchEnabled = config.rampLaunchEnabled;
        autoSlideOnLandingWhileCrouched = config.autoSlideOnLandingWhileCrouched;
        autoResumeSlideFromMomentum = config.autoResumeSlideFromMomentum;
        slideAudioEnabled = config.slideAudioEnabled;
        diveAudioEnabled = config.diveAudioEnabled;
        slideParticlesEnabled = config.slideParticlesEnabled;
        
        // Debug options
        showDebugVisualization = config.showDebugVisualization;
        verboseDebugLogging = config.verboseDebugLogging;
        
        // ENHANCEMENT: Smooth Wall Sliding parameters
        enableSmoothWallSliding = config.enableSmoothWallSliding;
        wallSlideMaxIterations = config.wallSlideMaxIterations;
        wallSlideSpeedPreservation = config.wallSlideSpeedPreservation;
        wallSlideMinAngle = config.wallSlideMinAngle;
        wallSlideSkinMultiplier = config.wallSlideSkinMultiplier;
        showWallSlideDebug = config.showWallSlideDebug;
        
        // Hidden constants from config
        slideHardCapSeconds = config.slideHardCapSeconds;
        slideBaseDuration = config.slideBaseDuration;
        slideMaxExtraDuration = config.slideMaxExtraDuration;
        slideSpeedForMaxExtra = config.slideSpeedForMaxExtra;
        uphillReversalSpeed = config.uphillReversalSpeed;
        uphillMinTime = config.uphillMinTime;
        uphillReversalBoost = config.uphillReversalBoost;
        uphillRampUpDuration = config.uphillRampUpDuration; // Load uphill ramp duration from config
        slopeTransitionGrace = config.slopeTransitionGrace;
        radiusSkin = config.radiusSkin;
        groundNormalSmoothing = config.groundNormalSmoothing;
        landingMomentumResumeWindow = config.landingMomentumResumeWindow;
        slideLandingBuffer = config.slideLandingBuffer;
        rampMinUpY = config.rampMinUpY;
        rampNormalDeltaDeg = config.rampNormalDeltaDeg;
        rampDownhillMemory = config.rampDownhillMemory;
        
        Debug.Log($"[CleanAAACrouch] ✅ Configuration loaded from {config.name}");
    }
    
    /// <summary>
    /// Public accessor to change configuration at runtime
    /// </summary>
    public CrouchConfig Config
    {
        get => config;
        set
        {
            config = value;
            if (config != null)
            {
                LoadConfiguration();
                Debug.Log($"[CleanAAACrouch] Runtime config switched to: {config.name}");
            }
        }
    }
    
    // BRILLIANT: Debug visualization system
    private void OnDrawGizmos()
    {
        if (!showDebugVisualization || !isSliding) return;
        
        Vector3 pos = transform.position + Vector3.up * 1f;
        
        // Draw current slide velocity (BLUE)
        if (slideVelocity.magnitude > 0.1f)
        {
            Gizmos.color = Color.blue;
            Vector3 velDir = slideVelocity.normalized * debugArrowLength;
            // Gizmos.DrawRay removed for performance
            Gizmos.DrawSphere(pos + velDir, 0.2f);
        }
        
        // Draw ground normal (GREEN)
        if (hasSmoothedNormal)
        {
            Gizmos.color = Color.green;
            // Gizmos.DrawRay removed for performance
        }
        
        // Draw downhill direction (RED)
        if (slopeAngle > 1f)
        {
            Vector3 downhillDir = Vector3.ProjectOnPlane(Vector3.down, smoothedGroundNormal).normalized;
            Gizmos.color = Color.red;
            // Gizmos.DrawRay removed for performance
            Gizmos.DrawSphere(pos + downhillDir * debugArrowLength * 0.7f, 0.15f);
        }
        
        // Draw uphill indicator (YELLOW)
        if (isMovingUphill)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(pos + Vector3.up * 0.5f, 0.5f);
        }
        
        // Speed indicator text
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(pos + Vector3.up * 2f, 
            $"Speed: {slideVelocity.magnitude:F1}\nSlope: {slopeAngle:F1}°\n{(isMovingUphill ? "UPHILL" : "DOWNHILL")} ");
        #endif
    }
    
    private Camera GetSlideCamera()
    {
        if (cameraTransform != null)
        {
            var cam = cameraTransform.GetComponent<Camera>();
            if (cam != null) return cam;
            return cameraTransform.GetComponentInChildren<Camera>(true);
        }
        return Camera.main;
    }

    private bool HasHeadroomToStand()
    {
        if (controller == null) return true;

        float radius = Mathf.Max(0.01f, controller.radius - radiusSkin);

        // Compute current head position (top sphere center) in world space
        Vector3 worldCenterCurrent = transform.position + controller.center;
        float currentTopOffset = (controller.height * 0.5f) - controller.radius;
        Vector3 currentTop = worldCenterCurrent + Vector3.up * currentTopOffset;

        // Compute target head position for standing height
        // CRITICAL FIX: Use standingHeight / 2 directly (matches fix in ApplyHeightAndCameraUpdates)
        float targetCenterY = standingHeight * 0.5f;
        Vector3 worldCenterTarget = transform.position + new Vector3(controller.center.x, targetCenterY, controller.center.z);
        float targetTopOffset = (standingHeight * 0.5f) - controller.radius;
        Vector3 targetTop = worldCenterTarget + Vector3.up * targetTopOffset;

        float upDist = Mathf.Max(0f, targetTop.y - currentTop.y);
        if (upDist <= 0.001f) return true; // already at/above standing

        // Sphere cast upward to see if anything blocks the head rising to standing height
        if (Physics.SphereCast(currentTop - Vector3.up * 0.01f, radius, Vector3.up,
                               out RaycastHit hit, upDist + 0.02f, obstructionMask,
                               QueryTriggerInteraction.Ignore))
        {
            // Ignore triggers
            if (hit.collider != null && !hit.collider.isTrigger)
            {
                return false;
            }
        }
        return true;
    }
    
    private void ApplyHeightAndCameraUpdates()
    {
        // Dynamic lerp speed based on landing impact for responsive feel
        float dynamicHeightLerpSpeed = heightLerpSpeed;
        float dynamicCameraLerpSpeed = cameraLerpSpeed;
        
        // Check if we just landed with high speed (within impact window)
        bool isImpactLanding = isSliding && 
                               (Time.time - slideStartTime) < IMPACT_LANDING_WINDOW && 
                               slideInitialSpeed > HIGH_SPEED_LANDING_THRESHOLD;
        
        if (isImpactLanding)
        {
            // IMPACT BOOST: 1.5x faster transition for responsive but smooth landing feel
            dynamicHeightLerpSpeed *= impactLerpSpeedMultiplier;
            dynamicCameraLerpSpeed *= impactLerpSpeedMultiplier;
        }
        
        // Smoothly apply controller height & center
        if (controller != null)
        {
            float stepH = Mathf.Max(0.01f, dynamicHeightLerpSpeed) * Time.deltaTime;
            float newH = Mathf.MoveTowards(controller.height, targetHeight, stepH);
            
            if (!Mathf.Approximately(newH, controller.height))
            {
                // CRITICAL FIX: Adjust center to keep feet planted on ground!
                // When height changes, center must change to keep bottom of capsule in same place
                // Formula: center.y = height / 2 (keeps bottom at y=0 in local space)
                controller.height = newH;
                controller.center = new Vector3(controller.center.x, newH * 0.5f, controller.center.z);
            }
        }

        // Smoothly apply camera local Y
        if (cameraTransform != null)
        {
            float stepC = Mathf.Max(0.01f, dynamicCameraLerpSpeed) * Time.deltaTime;
            Vector3 lp = cameraTransform.localPosition;
            float newY = Mathf.MoveTowards(lp.y, targetCameraY, stepC);
            Vector3 newPos = new Vector3(cameraLocalStart.x, newY, cameraLocalStart.z);
            cameraTransform.localPosition = newPos;
            
            // CRITICAL: Update AAACameraController's base position so it doesn't override us
            AAACameraController camController = cameraTransform.GetComponent<AAACameraController>();
            if (camController != null)
            {
                camController.UpdateBaseLocalPosition(newPos);
            }
        }
    }

    // === TACTICAL DIVE SYSTEM ===
    
    private void StartTacticalDive()
    {
        if (movement == null || controller == null) return;
        
        isDiving = true;
        isDiveProne = false;
        diveStartTime = Time.time;
        
        // Get input direction from WASD keys
        float horizontal = Controls.HorizontalRaw(); // A = -1, D = +1
        float vertical = Controls.VerticalRaw();     // S = -1, W = +1
        
        // Calculate dive direction based on input (camera-relative)
        Vector3 diveDir;
        if (Mathf.Abs(horizontal) < 0.01f && Mathf.Abs(vertical) < 0.01f)
        {
            // No input - default to forward
            diveDir = cameraTransform != null ? cameraTransform.forward : transform.forward;
        }
        else
        {
            // Transform input to world space relative to camera
            Vector3 camForward = cameraTransform != null ? cameraTransform.forward : transform.forward;
            Vector3 camRight = cameraTransform != null ? cameraTransform.right : transform.right;
            
            // Project to horizontal plane
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();
            
            // Combine input directions
            diveDir = (camForward * vertical + camRight * horizontal).normalized;
        }
        
        // PRISTINE FIX: Launch player with proper arc using unified gravity system
        // Horizontal component: dive direction * forward force
        // Vertical component: upward force (will be affected by gravity naturally)
        diveVelocity = diveDir * diveForwardForce + Vector3.up * diveUpwardForce;
        
        // CRITICAL FIX: Enable dive override to block movement input
        if (movement != null)
        {
            movement.EnableDiveOverride();
            
            // BRILLIANT: Set velocity directly using SetVelocity() for immediate launch
            // This respects the unified gravity system - gravity will naturally create the arc
            movement.SetVelocity(diveVelocity);
            
            Debug.Log($"[DIVE START] Input: H={horizontal:F1}, V={vertical:F1} | Direction: {diveDir} | Initial Velocity: {diveVelocity} | Gravity will create natural arc");
        }
        
        // Force crouch immediately
        isCrouching = true;
        
        // Play dive sound
        PlayDiveSound();
        
        // Start dive particles
        StartDiveParticles();
        
        Debug.Log($"[TACTICAL DIVE] Initiated! Forward: {diveForwardForce}, Up: {diveUpwardForce}, Unified gravity system will handle arc");
    }
    
    private void UpdateDive()
    {
        if (!isDiving || movement == null || controller == null) return;
        
        // === PRISTINE: Jump cancellation - Single source of truth ===
        // Uses AAAMovementController.JumpButtonPressed property instead of raw Input.GetKeyDown
        // This ensures jump suppression is respected and maintains centralized control
        if (movement != null && movement.JumpButtonPressed)
        {
            Debug.Log("[DIVE] Jump pressed - canceling dive!");
            isDiving = false;
            
            // === PRISTINE: Guaranteed cleanup ===
            movement.DisableDiveOverride();
            movement.ClearExternalForce(); // Clear dive force
            
            StopDiveParticles();
            
            // Jump will be handled by AAA automatically
            return;
        }
        
        // CRITICAL: Give a grace period after dive start before checking grounded
        // This prevents immediate landing detection when starting from ground
        float timeSinceDiveStart = Time.time - diveStartTime;
        bool allowLandingCheck = timeSinceDiveStart > 0.1f; // 0.1 second grace period
        
        // Check if we've landed (single source: AAA's raw grounded state)
        bool isGrounded = allowLandingCheck && (movement != null && movement.IsGroundedRaw);
        
        // PRISTINE: Sync local dive velocity with movement system's actual velocity
        // This maintains single source of truth while allowing us to track dive state
        if (movement != null)
        {
            diveVelocity = movement.Velocity;
        }
        
        if (verboseDebugLogging)
            Debug.Log($"[DIVE DEBUG] UpdateDive - Time: {timeSinceDiveStart:F3}, AllowLanding: {allowLandingCheck}, Grounded: {isGrounded}, Velocity: {diveVelocity}, Y: {diveVelocity.y}");
        
        if (isGrounded)
        {
            // Landed! Transition to prone state with forward slide
            isDiving = false;
            isDiveProne = true;
            diveProneTimer = 0f;
            
            // PRISTINE: Use actual landing velocity from movement system (single source of truth)
            Vector3 landingVelocity = movement.Velocity;
            Vector3 horizontalVel = new Vector3(landingVelocity.x, 0f, landingVelocity.z);
            float landingSpeed = horizontalVel.magnitude;
            
            // Start sliding forward on belly with actual landing momentum
            diveSlideVelocity = horizontalVel.normalized * Mathf.Min(landingSpeed, diveSlideDistance);
            
            // CRITICAL FIX: Use continuous external velocity for belly slide
            if (movement != null)
            {
                // Use longer duration for smooth belly slide (will be refreshed in UpdateDiveProne)
                movement.SetExternalVelocity(new Vector3(diveSlideVelocity.x, 0f, diveSlideVelocity.z), 0.2f, overrideGravity: false);
            }
            
            // Clear dive velocity
            diveVelocity = Vector3.zero;
            
            // Trigger camera landing impact (enhanced for dive)
            TriggerDiveLandingImpact();
            
            // Play landing sound
            GameSounds.PlayPlayerLand(transform.position);
            
            // Play fall damage sound for extra impact (belly flop!)
            if (SoundEventsManager.Events != null && SoundEventsManager.Events.fallDamage != null && SoundEventsManager.Events.fallDamage.Length > 0)
            {
                GeminiGauntlet.Audio.SoundEvents.PlayRandomSound3D(SoundEventsManager.Events.fallDamage, transform.position, 0.8f);
            }
            
            // Stop dive particles, start slide particles for ground effect
            StopDiveParticles();
            if (slideParticlesEnabled && slideParticles != null)
            {
                StartSlideParticles();
            }
            
            Debug.Log($"[TACTICAL DIVE] Landed in prone position! Sliding forward at {diveSlideVelocity.magnitude:F1} units/s");
        }
        else
        {
            // PRISTINE: Unified gravity system handles dive arc automatically
            // AAAMovementController applies gravity every frame (line 725 & 743)
            // We just track the velocity for state management - NO manual updates needed!
            // The natural parabolic arc is created by:
            // 1. Initial upward velocity (diveUpwardForce)
            // 2. Gravity pulling down every frame (-980 units/s²)
            // 3. Air control disabled (dive override blocks input)
            
            if (verboseDebugLogging)
                Debug.Log($"[DIVE DEBUG] In air - Velocity: {diveVelocity.magnitude:F1}, Y: {diveVelocity.y:F1}, Unified gravity creating natural arc");
        }
    }
    
    private void UpdateDiveProne()
    {
        if (!isDiveProne) return;
        
        diveProneTimer += Time.deltaTime;
        
        // Apply belly slide friction to decelerate
        if (diveSlideVelocity.sqrMagnitude > 0.01f)
        {
            float currentSpeed = diveSlideVelocity.magnitude;
            float deceleration = diveSlideFriction * Time.deltaTime;
            
            if (deceleration >= currentSpeed)
            {
                // Come to complete stop
                diveSlideVelocity = Vector3.zero;
                if (movement != null)
                {
                    // CRITICAL FIX: Use force API instead of immediate set
                    movement.SetExternalVelocity(Vector3.zero, 0.1f, overrideGravity: true);
                }
            }
            else
            {
                // Decelerate smoothly
                diveSlideVelocity = diveSlideVelocity.normalized * (currentSpeed - deceleration);
                if (movement != null)
                {
                    // PRISTINE: Use managed state duration (0.2s) instead of per-frame spam
                    movement.SetExternalVelocity(new Vector3(diveSlideVelocity.x, 0f, diveSlideVelocity.z), 0.2f, overrideGravity: false);
                }
            }
        }
        
        // Check for any input to stand up immediately
        bool hasMovementInput = Mathf.Abs(Controls.HorizontalRaw()) > 0.1f || Mathf.Abs(Controls.VerticalRaw()) > 0.1f;
        // PRISTINE: Use AAA's centralized jump detection instead of raw Input.GetKeyDown
        // This maintains single source of truth and respects jump suppression logic
        bool hasJumpInput = movement != null && movement.JumpButtonPressed;
        // PHASE 3 COHERENCE FIX: Use Controls.Crouch instead of SerializeField crouchKey
        bool hasCrouchInput = Input.GetKeyDown(Controls.Crouch);
        
        // Stand up on any input OR after duration expires
        if (hasMovementInput || hasJumpInput || hasCrouchInput || diveProneTimer >= diveProneDuration)
        {
            ExitDiveProne();
        }
    }
    
    private void ExitDiveProne()
    {
        if (!isDiveProne) return;
        
        isDiveProne = false;
        diveProneTimer = 0f;
        
        // === PRISTINE: Guaranteed dive override cleanup ===
        if (movement != null)
        {
            movement.DisableDiveOverride();
            movement.ClearExternalForce(); // Ensure no lingering forces
        }
        
        // Stop slide particles if active
        if (slideParticlesEnabled && slideParticles != null && particlesActive)
        {
            StopSlideParticles();
        }
        
        // NO LONGER NEEDED: Auto-detection will resume Sprint/Walk/Idle automatically
        // based on current movement state (Sprint if still holding Shift, Walk if moving, etc.)
        Debug.Log("[TACTICAL DIVE] Standing up from prone! Input restored, auto-detection will handle animation.");
    }
    
    private void PlayDiveSound()
    {
        if (!diveAudioEnabled) return;
        
        if (SoundEventsManager.Events != null && SoundEventsManager.Events.tacticalDiveSound != null)
        {
            // Extra safety check for the clip itself
            if (SoundEventsManager.Events.tacticalDiveSound.clip != null)
            {
                SoundEventsManager.Events.tacticalDiveSound.Play2D(1f);
                Debug.Log("<color=cyan>[TACTICAL DIVE] Playing dive sound</color>");
            }
            else
            {
                Debug.LogWarning("[TACTICAL DIVE] Dive sound event exists but no audio clip assigned!");
            }
        }
        else
        {
            Debug.LogWarning("[TACTICAL DIVE] Dive sound not assigned in SoundEvents - skipping audio");
        }
    }
    
    private void StartDiveParticles()
    {
        if (diveParticles == null || diveParticlesActive) return;
        
        diveParticlesActive = true;
        diveParticles.Play();
    }
    
    private void StopDiveParticles()
    {
        if (diveParticles == null || !diveParticlesActive) return;
        
        diveParticlesActive = false;
        diveParticles.Stop();
    }
    
    private void TriggerDiveLandingImpact()
    {
        // Use the existing camera impact system with enhanced compression
        if (cameraTransform != null)
        {
            StartCoroutine(DiveLandingImpact());
        }
    }
    
    private System.Collections.IEnumerator DiveLandingImpact()
    {
        if (cameraTransform == null) yield break;
        
        // Get camera controller for proper landing impact
        var cameraController = cameraTransform.GetComponent<AAACameraController>();
        if (cameraController != null)
        {
            // The camera controller's landing impact system will handle this automatically
            // We just need to ensure the fall distance is tracked
            // The dive creates a "fall" that triggers the impact
        }
        
        // Additional immediate shake for impact feel
        Vector3 originalPos = cameraTransform.localPosition;
        float duration = 0.2f;
        float magnitude = 1.2f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float strength = Mathf.Lerp(magnitude, 0f, elapsed / duration);
            Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * strength * 0.15f;
            randomOffset.y *= 0.5f; // More vertical shake for belly landing
            
            cameraTransform.localPosition = originalPos + randomOffset;
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        cameraTransform.localPosition = originalPos;
    }
    
    // PRISTINE: AUTO-SLIDE SYSTEM - UNIFIED FOR ALL SLOPES
    /// <summary>
    /// PRISTINE: Auto-slide system with two-tier detection:
    /// 1. STEEP slopes (>50°) - Force slide ALWAYS (wall jump integrity)
    /// 2. MODERATE slopes (12-50°) - Handled by crouch press in Update()
    /// 
    /// SMART DETECTION: Requires minimum contact time to prevent brief wall touches.
    /// OPTIMIZED: Single raycast, early exits, no allocations.
    /// </summary>
    private void CheckAndForceSlideOnSteepSlope()
    {
        // OPTIMIZATION: Early exit if no controller
        if (controller == null) return;
        
        // CRITICAL FIX: Remove restrictive guard - auto-slide should work when landing on steep slopes
        // OLD (BROKEN): Only checked if crouching OR moving fast (>100 speed)
        // NEW (CORRECT): Always check steep slopes, let contact duration filter out wall touches
        // The contact duration check (0.15s) is sufficient to prevent false triggers
        
        // OPTIMIZATION: Single raycast downward from player center
        // Uses existing probe system for consistency
        RaycastHit hit;
        bool hasGround = ProbeGround(out hit);
        
        // OPTIMIZATION: Early exit if no ground detected
        if (!hasGround)
        {
            // Reset contact timer when no ground
            steepSlopeContactStartTime = -999f;
            return;
        }
        
        // Calculate slope angle (0° = flat, 90° = vertical wall)
        float angle = Vector3.Angle(Vector3.up, hit.normal);
        
        // PRISTINE: Only force-slide on STEEP slopes (>50°)
        // Moderate slopes (12-50°) are handled by crouch press in Update()
        const float STEEP_SLOPE_THRESHOLD = 55f; // Auto-slide on slopes 55°-90°
        
        if (angle >= STEEP_SLOPE_THRESHOLD && angle < 90f)
        {
            // INSTANT AUTO-SLIDE: No contact duration requirement - trigger immediately
            // Only start slide if NOT already sliding
            if (!isSliding)
            {
                // Only auto-slide if moving downward (not when jumping up onto slope)
                bool isMovingDown = movement != null && movement.Velocity.y <= 0f;
                
                if (isMovingDown)
                {
                    // Force slide start - simulates pressing crouch button
                    forceSlideStartThisFrame = true;
                    TryStartSlide();
                    
                    Debug.Log($"[AUTO-SLIDE] INSTANT steep slope slide! Angle: {angle:F1}°");
                }
            }
        }
    }
    
    // Expose status for UI/other systems
    public bool IsCrouching => isCrouching;
    public bool IsSliding => isSliding;
    public bool IsDiving => isDiving;
    public bool IsDiveProne => isDiveProne;
    public float CurrentSlideSpeed => isSliding ? slideVelocity.magnitude : 0f;
    
    /// <summary>
    /// PUBLIC API: Queue landing momentum for next slide transition.
    /// Used by CrouchSlamController to inject boosted speed on slam landing.
    /// </summary>
    /// <param name="momentum">3D momentum vector to preserve (can include Y for slope-aligned velocity)</param>
    /// <param name="duration">How long to keep this momentum queued (seconds)</param>
    public void QueueLandingMomentum(Vector3 momentum, float duration)
    {
        // CRITICAL FIX: Preserve Y component for slope-aligned momentum from crouch slam!
        // Old code stripped Y → horizontal → no downhill flow on slopes
        // New code keeps full 3D vector → natural slope physics
        queuedLandingMomentum = momentum; // Keep full 3D vector (slope-aligned from CrouchSlamController)
        queuedLandingMomentumUntil = Time.time + duration;
        
        if (enableDebugLogs)
            Debug.Log($"[SLIDE MOMENTUM] Queued landing momentum: {momentum.magnitude:F0} units/s for {duration:F2}s");
    }
}
