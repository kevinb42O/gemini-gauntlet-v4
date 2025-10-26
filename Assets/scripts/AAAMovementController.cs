// --- REFACTORED AAAMovementController.cs ---
using UnityEngine;
using System.Collections; // Needed for coroutine
using System; // For reflection
using System.Reflection; // For safe integration with external flight controller
using GeminiGauntlet.Audio; // For audio system

/// <summary>
/// AAA Ground Movement Controller.
/// Refactored for stable, responsive, and predictable physics.
/// Features: Fast movement, reliable jumping/double-jumping, and dashing.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class AAAMovementController : MonoBehaviour
{
    [Header("=== 🎮 CONFIGURATION ===")]
    [Tooltip("ScriptableObject configuration asset (recommended). If null, uses legacy inspector settings below.")]
    [SerializeField] private MovementConfig config;
    
    [Header("=== CORE REFERENCES ===")]
    [Tooltip("The CharacterController component on this player.")]
    [SerializeField] private CharacterController controller;
    [Tooltip("The transform of the main camera.")]
    [SerializeField] private Transform cameraTransform;
    
    // Animation system references
    private LayeredHandAnimationController _layeredHandAnimationController; // For backward compatibility
    private PlayerAnimationStateManager _animationStateManager; // Centralized system
    
    // Camera system reference for wall jump tilt
    private AAACameraController cameraController;
    
    // Bleeding out system references
    private PlayerHealth playerHealth;
    private DeathCameraController deathCameraController;
    
    [Header("=== BLEEDING OUT MODE ===")]
    [Tooltip("Movement speed multiplier when bleeding out (slow crawl)")]
    [SerializeField] private float bleedOutSpeedMultiplier = 0.1f; // 10x slower!
    [Tooltip("Input smoothing for bleeding out movement (AAA feel)")]
    [SerializeField] private float bleedOutInputSmoothing = 8f; // Smooth, heavy crawl
    
    [Header("=== INPUT SMOOTHING (AAA FEEL) ===")]
    [Tooltip("Input direction smoothing time - HIGHER = less jerky direction changes (0.08-0.15 recommended)")]
    [SerializeField] private float inputSmoothTime = 0.10f; // AAA standard for smooth transitions (start conservative)
    
    // Bleeding out input smoothing
    private Vector2 currentBleedOutInput = Vector2.zero;
    private Vector2 bleedOutInputVelocity = Vector2.zero;
    
    // AAA Input smoothing for normal movement
    private Vector2 currentSmoothedInput = Vector2.zero;
    private Vector2 inputSmoothVelocity = Vector2.zero;
    private Vector3 velocitySmoothRef = Vector3.zero;

    [Header("=== 🧪 LEGACY INSPECTOR SETTINGS (Used if Config is null) ===")]
    [Header("=== MOVEMENT ===")]
    [SerializeField] private float moveSpeed = 900f; // SCALED 3x for 320-unit character (was 105)
    [SerializeField] private float sprintMultiplier = 1.65f; // SCALED for 320-unit character (was 1.85)
    
    [Header("=== AAA+ ACCELERATION SYSTEM ===")]
    [Tooltip("Ground acceleration rate (higher = faster response). Source Engine uses 600-1200.")]
    [SerializeField] private float groundAcceleration = 2400f; // TUNED: Increased for snappier response (was 1800f)
    [Tooltip("Friction when no input (higher = faster stop). Source Engine uses 400-800.")]
    [SerializeField] private float groundFriction = 1800f; // TUNED: Increased to reduce ice-skating feel (was 1200f)
    [Tooltip("Speed below which friction is stronger (for snappy stops).")]
    [SerializeField] private float stopSpeed = 200f; // TUNED: Increased for more aggressive stops (was 150f)
    [Tooltip("Enable acceleration-based movement (AAA industry standard).")]
    [SerializeField] private bool enableAccelerationSystem = true;
    
    [Header("=== AIR CONTROL ===")]
    [Tooltip("How much control you have in the air (0 = none, 1 = full). Lower = more momentum-based.")]
    [SerializeField] private float airControlStrength = 0.25f; // SCALED for 320-unit character (was 0.28)
    [Tooltip("How quickly you can change direction in the air. Lower = more momentum preservation.")]
    [SerializeField] private float airAcceleration = 1500f; // SCALED for 320-unit character (was 18)
    [Tooltip("Maximum speed you can reach in the air from input alone.")]
    [SerializeField] private float maxAirSpeed = 12500f; // SCALED for 320-unit character (was 90)
    [Tooltip("Preserve more speed when going fast (makes sprint jumps feel amazing).")]
    [SerializeField] private bool preserveHighSpeedMomentum = true;
    [Tooltip("Speed threshold for high-speed air physics (UNIFIED with CleanAAACrouch.HIGH_SPEED_LANDING_THRESHOLD)")]
    [SerializeField] private float highSpeedThreshold = 960f; // FIXED: Was 350 - now 65% of sprint (960÷1485) for consistent high-speed detection
    // Energy system reference
    private PlayerEnergySystem energySystem;
    
    // Crouch system reference
    private CleanAAACrouch crouchController;
    
    // PHASE 4 COHERENCE: Controller ownership tracking (FIXED - removed duplicate enum)
    private ControllerModificationSource _currentOwner = ControllerModificationSource.None;
    
    // PRISTINE: Slope limit restoration stack for nested overrides
    private struct SlopeLimitOverride
    {
        public float slopeLimit;
        public ControllerModificationSource source;
        public float timestamp;
    }
    private System.Collections.Generic.Stack<SlopeLimitOverride> _slopeLimitStack = new System.Collections.Generic.Stack<SlopeLimitOverride>();
    
    // PRISTINE: Controller modification tracking for stepOffset and minMoveDistance
    private float _originalStepOffset = 0f;
    private float _originalMinMoveDistance = 0f;
    private bool _stepOffsetOverridden = false;
    private bool _minMoveDistanceOverridden = false;
    private ControllerModificationSource _stepOffsetOwner;
    private ControllerModificationSource _minMoveDistanceOwner;

    [Header("=== JUMPING ===")]
    [Tooltip("Jump force (higher = jump higher) - SCALED: ~2× character height jump")]
    [SerializeField] private float jumpForce = 2200f; // TUNED: Reduced from 2800 - more grounded feel (~690 unit jump = 2.2× height)
    [Tooltip("Double jump force (if enabled) - SCALED: Noticeable second jump")]
    [SerializeField] private float doubleJumpForce = 1400f; // TUNED: Reduced from 1800 - proportional to main jump (~280 unit boost)
    [SerializeField] private int maxAirJumps = 1; // ENABLED for 320-unit character (was 0)
    
    // === WALL JUMP SYSTEM - FALLBACK VALUES ONLY ===
    // ⚠️ EDIT IN MovementConfig SCRIPTABLEOBJECT - These are defaults if no config is assigned
    private bool enableWallJump = true;
    private float wallJumpUpForce = 1900f;
    private float wallJumpOutForce = 1200f;
    private float wallJumpFallSpeedBonus = 0.6f;
    private float wallJumpInputInfluence = 0.8f;
    private float wallJumpMomentumPreservation = 0f;
    private float wallJumpForwardBoost = 400f;
    private float wallJumpCameraDirectionBoost = 1800f;
    private bool wallJumpCameraBoostRequiresInput = false;
    private float wallJumpInputBoostMultiplier = 1.3f;
    private float wallJumpInputBoostThreshold = 0.2f;
    private float wallDetectionDistance = 400f;
    private float wallJumpCooldown = 0.12f;
    private float wallJumpGracePeriod = 0.08f;
    private int maxConsecutiveWallJumps = 98;
    private float minFallSpeedForWallJump = 0.01f;
    private float wallJumpAirControlLockoutTime = 0f;
    private bool showWallJumpDebug = false;
    // Wall jump runtime state
    private float lastWallJumpTime = -999f;
    private int consecutiveWallJumps = 0;
    private Vector3 lastWallNormal = Vector3.zero;
    private float wallJumpVelocityProtectionUntil = -999f; // Protect wall jump velocity from air control
    
    // 🔒 ANTI-EXPLOIT: Track last wall jumped from to prevent same-wall spam
    private Collider lastWallJumpedFrom = null; // The specific wall object we just jumped from
    private int lastWallJumpedInstanceID = 0; // Backup ID in case collider gets destroyed
    
    // ✨ BUILT-IN COLLISION DETECTION: Use CharacterController's collision data (WAY simpler!)
    private CollisionFlags lastCollisionFlags; // Tracks what we hit (sides, below, above)
    private Vector3 lastWallCollisionNormal = Vector3.zero; // Wall normal from actual collision
    private Vector3 lastWallCollisionPoint = Vector3.zero; // Wall hit point from actual collision
    private Collider lastWallCollider = null; // Collider we're touching (for same-wall detection)
    private bool isCurrentlyTouchingWall = false; // True when actively colliding with wall this frame
    private float lastWallContactTime = -999f; // When we last touched a wall
    
    // 🎮 FORGIVENESS BUFFER: Multiple recent wall contacts for consistent detection
    private const int MAX_WALL_HISTORY = 5; // Remember last 5 wall contacts
    private struct WallContact
    {
        public Vector3 normal;
        public Vector3 point;
        public Collider collider;
        public float time;
    }
    private WallContact[] recentWallContacts = new WallContact[MAX_WALL_HISTORY];
    private int wallContactIndex = 0;
    
    [Header("=== 🎪 TRICK JUMP MOMENTUM BOOST SYSTEM ===")]
    [Tooltip("Enable momentum boost for trick jumps (middle-click aerial tricks)")]
    [SerializeField] private bool enableTrickJumpBoost = false; // DISABLED: Jump same height as normal jumps
    [Tooltip("Extra upward velocity added to trick jumps - SET TO 0 for same jump height as normal")]
    [SerializeField] private float trickJumpVerticalBoost = 0f; // Was 31f - now 0 for identical jump height
    [Tooltip("Extra forward velocity in movement direction - SCALED for 320-unit character")]
    [SerializeField] private float trickJumpForwardBoost = 135f; // Scaled 3x from base (45 → 135)
    [Tooltip("Speed multiplier - scales boost with current speed (0.2 = 20% of speed added)")]
    [SerializeField] private float trickJumpSpeedScaling = 0.2f; // Percentage stays same
    [Tooltip("Maximum total boost from speed scaling - SCALED for 320-unit character")]
    [SerializeField] private float trickJumpMaxSpeedBonus = 90f; // Scaled 3x from base (30 → 90)
    [Tooltip("Minimum speed required for speed scaling to activate - SCALED for 320-unit character")]
    [SerializeField] private float trickJumpSpeedThreshold = 150f; // Scaled 3x from base (50 → 150)
    [Tooltip("Show trick jump boost debug logs")]
    [SerializeField] private bool showTrickJumpDebug = true;
    // Trick jump runtime state
    private bool isTrickJump = false; // Flag to identify trick jumps
    
    [Header("=== PRO JUMP MECHANICS ===")]
    [Tooltip("Coyote time: grace period to jump after leaving ground (seconds).")]
    [SerializeField] private float coyoteTime = 0.225f; // SCALED for 320-unit character (was 0.15)
    [Tooltip("Jump buffer: remember jump input this long before landing (seconds).")]
    [SerializeField] private float jumpBufferTime = 0.12f; // Queue jumps early
    [Tooltip("Variable jump height: release jump key early for shorter jumps.")]
    [SerializeField] private bool enableVariableJumpHeight = true;
    [Tooltip("How quickly upward velocity decays when jump is released early.")]
    [SerializeField] private float jumpCutMultiplier = 0.5f; // Cut jump height by 50%
    
    private float lastGroundedTime = -999f;
    private float jumpBufferedTime = -999f;

    [Header("=== GRAVITY ===")]
    [Tooltip("Gravity force (negative = down, natural = -980) - SCALED: Snappier jumps at 320-unit scale")]
    [SerializeField] private float gravity = -3500f; // FIXED: Was -2500 - increased for snappier jump arcs
    [Tooltip("Maximum fall speed - SCALED: Realistic terminal velocity (5.4× sprint speed)")]
    [SerializeField] private float terminalVelocity = 8000f; // FIXED: Was 20000 - now proportional to movement speeds
    [Tooltip("Extra downward force when holding crouch in air")]
    [SerializeField] private float descendForce = 50000f; // SCALED for 320-unit character (was 105)


    [Header("=== SLOPE HANDLING ===")]
    [Tooltip("How strongly the character is pushed onto slopes to prevent bouncing.")]
    [SerializeField] private float slopeForce = 10000f; // SCALED for 320-unit character (was 50)
    [Tooltip("Maximum angle of a slope the character can walk on.")]
    [SerializeField] private float maxSlopeAngle = 50f; // SCALED for 320-unit character (was 45)
    
    [Header("=== SLOPE MOMENTUM SYSTEM ===")]
    [Tooltip("Enable natural slope physics during walking (gravity acceleration downhill, friction uphill).")]
    [SerializeField] private bool enableSlopeMomentum = true;
    [Tooltip("Acceleration multiplier on downhill slopes (gravity component).")]
    [SerializeField] private float slopeAccelerationMultiplier = 0.4f; // Subtle but noticeable
    [Tooltip("Friction multiplier on uphill slopes (resists movement).")]
    [SerializeField] private float uphillFrictionMultiplier = 1.8f; // Harder to run uphill
    [Tooltip("Minimum slope angle (degrees) to apply slope physics.")]
    [SerializeField] private float minimumSlopeAngle = 8f;
    [Tooltip("Speed bonus percentage when jumping off downhill slopes (ramp jumps).")]
    [SerializeField] private float rampJumpBonus = 0.25f; // 25% speed boost
    
    // PHASE 4 COHERENCE: Slope limit coordination with CleanAAACrouch
    private float _originalSlopeLimitFromAwake = 45f; // Store original for restoration

    [Header("=== PHYSICS ===")]
    [SerializeField] private LayerMask groundMask = 4161; // Custom layer mask
    [SerializeField] private float groundCheckDistance = 100f; // CRITICAL: Must be large enough to detect slopes on 320-unit character (was 20f - too small!)

    [Header("=== DIRECTIONAL SPRINT SYSTEM ===")]
    [Tooltip("Current sprint direction for hand animations (Forward/Left/Right/Backward)")]
    private Vector2 currentSprintInput = Vector2.zero; // Track X/Y input during sprint
    
    // Public accessors for animation system
    public Vector2 CurrentSprintInput => currentSprintInput;
    public bool IsSprintingForward => currentSprintInput.y > 0.3f && Mathf.Abs(currentSprintInput.x) < 0.5f;
    public bool IsSprintingBackward => currentSprintInput.y < -0.3f;
    public bool IsSprintingStrafeLeft => currentSprintInput.x < -0.3f;
    public bool IsSprintingStrafeRight => currentSprintInput.x > 0.3f;
    
    /// <summary>
    /// Returns normalized sprint animation speed (0.0 = walk speed, 1.0 = full sprint speed)
    /// This syncs hand animations with the acceleration-based movement system
    /// </summary>
    public float NormalizedSprintSpeed
    {
        get
        {
            // Calculate target sprint speed (base move speed * sprint multiplier)
            float maxSprintSpeed = MoveSpeed * SprintMultiplier;
            
            // Get current horizontal speed
            float currentHorizontalSpeed = new Vector3(velocity.x, 0, velocity.z).magnitude;
            
            // Normalize: 0.0 at walk speed (MoveSpeed), 1.0 at full sprint (maxSprintSpeed)
            // This creates smooth acceleration from walk → sprint animation speeds
            float normalizedSpeed = Mathf.Clamp01((currentHorizontalSpeed - MoveSpeed) / (maxSprintSpeed - MoveSpeed));
            
            return normalizedSpeed;
        }
    }

    [Header("=== COLLISION ===")]
    [Tooltip("Height of the character's collision capsule.")]
    [SerializeField] private float playerHeight = 320f; // SCALED for 320-unit character (was 300)
    [Tooltip("Radius of the character's collision capsule. LARGER = earlier wall detection for wall jumps!")]
    [SerializeField] private float playerRadius = 75f; // Increased from 50 for more forgiving wall jump timing
    
    [Header("=== MOVING PLATFORM SUPPORT ===")]
    [Tooltip("Disable movement logic when parented to moving platforms (elevators, etc.)")]
    [SerializeField] private bool enableMovingPlatformSupport = true;
    
    // Runtime tracking for moving platforms
    private Transform _previousParent = null;
    private bool _isOnMovingPlatform = false;
    private Vector3 _platformVelocity = Vector3.zero;
    private Vector3 _lastPlatformPosition = Vector3.zero;
    
    // NEW: Non-parenting platform support (for CharacterController.Move() based platforms)
    private ElevatorController _currentElevator = null;
    private bool _isOnNonParentingPlatform = false;
    
    // UNIVERSAL PLATFORM SYSTEM: Track any moving platform we're standing on
    private CelestialPlatform _currentCelestialPlatform = null;
    private Vector3 _lastPlatformVelocity = Vector3.zero;
    private bool _wasOnPlatformLastFrame = false;
    
    [Header("=== STAIR CLIMBING ===")]
    [Tooltip("Maximum height of stairs/steps the character can climb. Adjust this for your stair size.")]
    [SerializeField] private float maxStepHeight = 40f; // SCALED for 320-unit character (was 20)
    [Tooltip("Distance to check ahead for stairs.")]
    [SerializeField] private float stairCheckDistance = 150f; // SCALED for 320-unit character (was 25)
    [Tooltip("Enable advanced stair detection and climbing assistance.")]
    [SerializeField] private bool enableStairClimbingAssist = true;
    [Tooltip("Smooth step climbing to prevent jarring transitions.")]
    [SerializeField] private bool smoothStepClimbing = true;
    [Tooltip("Speed multiplier when climbing stairs (1.0 = no slowdown).")]
    [SerializeField] private float stairClimbSpeedMultiplier = 1f; // SCALED for 320-unit character (was 0.85)

    [Header("=== GROUNDING STABILITY ===")]
    [Tooltip("Time to keep grounded after a brief miss to prevent flicker (seconds)")]
    [SerializeField] private float groundedHysteresisSeconds = 0.1f; // ENABLED: Prevents rapid grounded/airborne flickering
    [Tooltip("Frames required to confirm grounded state change (prevents single-frame glitches)")]
    [SerializeField] private int groundedDebounceFrames = 2; // Require 2 consecutive frames to change state
    [Tooltip("Show detailed grounding debug logs (raw state, debounce counters, etc.)")]
    [SerializeField] private bool showGroundingDebug = false;
    [Tooltip("Enable pre-move anti-sink prediction to prevent ground penetration. RECOMMENDED for large worlds.")]
    [SerializeField] private bool enableAntiSinkPrediction = false; // DISABLED for 320-unit character (was true)
    [Header("=== JUMP LIFT-OFF PROTECTION ===")]
    [Tooltip("Time after issuing a jump during which grounded checks are ignored to avoid canceling lift-off.")]
    [SerializeField] private float jumpGroundSuppressSeconds = 0f; // DISABLED for 320-unit character (was 0.08)

    // Private State Variables
    private Vector3 velocity;
    private Vector3 moveDirection;
    private int airJumpRemaining;
    private float _suppressGroundedUntil = -999f;
    
    // ========== CONFIG SYSTEM - SINGLE SOURCE OF TRUTH ==========
    // These properties read from MovementConfig if assigned, otherwise fall back to inspector values
    // This allows data-driven configuration while maintaining backward compatibility
    // PUBLIC: Exposed for external systems (e.g., CleanAAACrouch sprint detection)
    
    public float MoveSpeed => config != null ? config.moveSpeed : moveSpeed;
    public float SprintMultiplier => config != null ? config.sprintMultiplier : sprintMultiplier;
    private float MaxAirSpeed => config != null ? config.maxAirSpeed : maxAirSpeed;
    private float AirControlStrength => config != null ? config.airControlStrength : airControlStrength;
    private float AirAcceleration => config != null ? config.airAcceleration : airAcceleration;
    private float HighSpeedThreshold => config != null ? config.highSpeedThreshold : highSpeedThreshold;
    private float JumpForce => config != null ? config.jumpForce : jumpForce;
    private float DoubleJumpForce => config != null ? config.doubleJumpForce : doubleJumpForce;
    private int MaxAirJumps => config != null ? config.maxAirJumps : maxAirJumps;
    private float JumpCutMultiplier => config != null ? config.jumpCutMultiplier : jumpCutMultiplier;
    private float CoyoteTime => config != null ? config.coyoteTime : coyoteTime;
    public float Gravity => config != null ? config.gravity : gravity; // PUBLIC - external scripts need access
    private float TerminalVelocity => config != null ? config.terminalVelocity : terminalVelocity;
    private float DescendForce => config != null ? config.descendForce : descendForce;
    private float SlopeForce => config != null ? config.slopeForce : slopeForce;
    private float MaxSlopeAngle => config != null ? config.maxSlopeAngle : maxSlopeAngle;
    private float MaxStepHeight => config != null ? config.maxStepHeight : maxStepHeight;
    private float PlayerHeight => config != null ? config.playerHeight : playerHeight;
    private float PlayerRadius => config != null ? config.playerRadius : playerRadius;
    private float StairCheckDistance => config != null ? config.stairCheckDistance : stairCheckDistance;
    private float StairClimbSpeedMultiplier => config != null ? config.stairClimbSpeedMultiplier : stairClimbSpeedMultiplier;
    private float GroundCheckDistance => config != null ? config.groundCheckDistance : groundCheckDistance;
    private float GroundedHysteresisSeconds => config != null ? config.groundedHysteresisSeconds : groundedHysteresisSeconds;
    private float JumpGroundSuppressSeconds => config != null ? config.jumpGroundSuppressSeconds : jumpGroundSuppressSeconds;
    private bool EnableAntiSinkPrediction => config != null ? config.enableAntiSinkPrediction : enableAntiSinkPrediction;
    private float GroundPredictionDistance => config != null ? config.groundPredictionDistance : groundPredictionDistance;
    private float GroundClearance => config != null ? config.groundClearance : groundClearance;
    private LayerMask GroundMask => config != null ? config.groundMask : groundMask;
    private bool AllowInternalModeToggle => config != null ? config.allowInternalModeToggle : allowInternalModeToggle;
    private bool PreferIntegratorForModeToggle => config != null ? config.preferIntegratorForModeToggle : preferIntegratorForModeToggle;
    private bool EnableCelestialFlightIntegration => config != null ? config.enableCelestialFlightIntegration : enableCelestialFlightIntegration;
    private bool PlayLandAnimationOnGroundedHere => config != null ? config.playLandAnimationOnGroundedHere : playLandAnimationOnGroundedHere;
    
    // Wall Jump Config Properties
    private bool EnableWallJump => config != null ? config.enableWallJump : enableWallJump;
    private float WallJumpUpForce => config != null ? config.wallJumpUpForce : wallJumpUpForce;
    private float WallJumpOutForce => config != null ? config.wallJumpOutForce : wallJumpOutForce;
    private float WallJumpForwardBoost => config != null ? config.wallJumpForwardBoost : wallJumpForwardBoost;
    private float WallJumpCameraDirectionBoost => config != null ? config.wallJumpCameraDirectionBoost : wallJumpCameraDirectionBoost;
    private bool WallJumpCameraBoostRequiresInput => config != null ? config.wallJumpCameraBoostRequiresInput : wallJumpCameraBoostRequiresInput;
    private float WallJumpFallSpeedBonus => config != null ? config.wallJumpFallSpeedBonus : wallJumpFallSpeedBonus;
    private float WallJumpInputInfluence => config != null ? config.wallJumpInputInfluence : wallJumpInputInfluence;
    private float WallJumpInputBoostMultiplier => config != null ? config.wallJumpInputBoostMultiplier : wallJumpInputBoostMultiplier;
    private float WallJumpInputBoostThreshold => config != null ? config.wallJumpInputBoostThreshold : wallJumpInputBoostThreshold;
    private float WallJumpMomentumPreservation => config != null ? config.wallJumpMomentumPreservation : wallJumpMomentumPreservation;
    private float WallDetectionDistance => config != null ? config.wallDetectionDistance : wallDetectionDistance;
    private float WallJumpCooldown => config != null ? config.wallJumpCooldown : wallJumpCooldown;
    private float WallJumpGracePeriod => config != null ? config.wallJumpGracePeriod : wallJumpGracePeriod;
    private int MaxConsecutiveWallJumps => config != null ? config.maxConsecutiveWallJumps : maxConsecutiveWallJumps;
    private float MinFallSpeedForWallJump => config != null ? config.minFallSpeedForWallJump : minFallSpeedForWallJump;
    private float WallJumpAirControlLockoutTime => config != null ? config.wallJumpAirControlLockoutTime : wallJumpAirControlLockoutTime;
    private bool ShowWallJumpDebug => config != null ? config.showWallJumpDebug : showWallJumpDebug;
    
    // Wall Jump VFX Config Properties
    private bool EnableWallJumpVFX => config != null ? config.enableWallJumpVFX : true;
    private float VFXBaseDuration => config != null ? config.vfxBaseDuration : 2f;
    private float VFXEffectScale => config != null ? config.vfxEffectScale : 1f;
    private float VFXMinSpeedThreshold => config != null ? config.vfxMinSpeedThreshold : 300f;
    private float VFXMaxIntensitySpeed => config != null ? config.vfxMaxIntensitySpeed : 1500f;
    private float VFXMinIntensity => config != null ? config.vfxMinIntensity : 0.3f;
    private float VFXMaxIntensity => config != null ? config.vfxMaxIntensity : 1.5f;
    
    // Helper properties for time-based checks
    public bool IsJumpSuppressed => Time.time <= _suppressGroundedUntil;
    private bool IsWallJumpProtected => Time.time <= wallJumpVelocityProtectionUntil;
    private bool HasActiveExternalForce => Time.time <= (_externalForceStartTime + _externalForceDuration);
    
    /// <summary>
    /// PRISTINE: Single source of truth for jump button press detection.
    /// Returns true if jump button was pressed this frame AND jump is NOT suppressed.
    /// External systems (CleanAAACrouch, etc.) should use THIS instead of reading raw input.
    /// Maintains centralized control over jump detection and respects all suppression logic.
    /// </summary>
    public bool JumpButtonPressed => !IsJumpSuppressed && Input.GetKeyDown(Controls.UpThrustJump);
    
    /// <summary>
    /// Returns true if in wall jump chain (airborne + recent wall jump)
    /// Used by camera system for dynamic tilt effects
    /// </summary>
    public bool IsInWallJumpChain => consecutiveWallJumps > 0 && !IsGrounded && (Time.time - lastWallJumpTime < 0.6f);

    /// <summary>
    /// Last detected wall hit point (for camera tilt calculations)
    /// Zero vector when no wall is detected
    /// </summary>
    public Vector3 LastWallHitPoint { get; private set; }
    
    private Vector3 groundNormal; // To store the normal of the ground surface
    private float currentSlopeAngle = 0f; // Current slope angle in degrees
    
    // Stair climbing state
    private bool isClimbingStairs = false;
    private float stairClimbProgress = 0f;
    private Vector3 stairClimbStartPos;
    private Vector3 stairClimbTargetPos;
    
    // Movement Mode System
    public enum MovementMode { Walking, Flying }
    private MovementMode currentMode = MovementMode.Walking; // Start in WALKING mode for crouch/slide to work
    private bool modeJustChanged = false;
    private float modeChangeUpwardPush = 15f; // Upward velocity when switching to flying on platform
    private float lastJumpTime = 0f;
    private float jumpCooldown = 0.2f; // Minimum time between jumps
    private float emergencyJumpForce = 200f; // Stronger jump when stuck
    private int consecutiveJumpAttempts = 0;
    private float lastJumpAttemptTime = 0f;
    private bool wasStuckInGround = false;
    
    // Jump state management (simplified for walking mode only)
    private bool canJump = true;
    private float jumpCooldownTimer = 0f;
    private const float MIN_JUMP_COOLDOWN = 0.15f; // Slightly longer cooldown
    private bool hasJumped = false; // Track if player actually jumped (not just fell)
    private float timeOfJump = -999f; // Track when the jump occurred
    private const float MIN_TIME_IN_AIR_FOR_LAND_SOUND = 0.2f; // Minimum airtime before land sound plays
    
    // Movement system conflict resolution
    private CelestialDriftController celestialDriftController;
    private Rigidbody rb;
    private bool celestialDriftWasEnabled = false;
    
    [Header("=== MODE TOGGLE SETTINGS ===")]
    [Tooltip("If true, AAAMovementController handles F key mode toggle. If false, external integrator owns it.")]
    [SerializeField] private bool allowInternalModeToggle = false; // DISABLED for 320-unit character (was true)
    [Tooltip("Auto-disable internal toggle when AAAMovementIntegrator is present.")]
    [SerializeField] private bool preferIntegratorForModeToggle = false; // DISABLED for 320-unit character (was true)
    
    [Header("=== CELESTIAL FLIGHT INTEGRATION ===")]
    [Tooltip("If enabled, pressing F to enter Flying will enable CelestialDriftController in flight-only mode (falling disabled via reflection if available). When walking, the controller is disabled.")]
    [SerializeField] private bool enableCelestialFlightIntegration = false; // DISABLED for 320-unit character (was true)
    
    // Lock-on system integration
    private PlayerMovementManager playerMovementManager;
    
    // Sound system integration
    private AAAMovementAudioManager audioManager;
    
    [Header("=== ANIMATION TRIGGERS ===")]
    [Tooltip("If true, this controller will directly trigger the hand land animation on grounded transitions. Recommended OFF when CleanAAACrouch handles land animations.")]
    [SerializeField] private bool playLandAnimationOnGroundedHere = false; // DISABLED for 320-unit character (was false)

    // Public Properties for other systems to read
    public bool IsGrounded { get; private set; }
    private float lastGroundDistance = -1f; // PHASE 1 UNIFICATION: Cached ground distance
    
    /// <summary>
    /// Grounded state WITH coyote time - use this for gameplay logic that needs forgiveness.
    /// </summary>
    public bool IsGroundedWithCoyote => IsGrounded || (Time.time - lastGroundedTime) <= CoyoteTime;
    
    /// <summary>
    /// Raw grounded state WITHOUT coyote time - use this when you need instant truth.
    /// </summary>
    public bool IsGroundedRaw => controller != null && controller.isGrounded;
    
    /// <summary>
    /// Time since player was last grounded (for external systems to implement custom coyote).
    /// </summary>
    public float TimeSinceGrounded => Time.time - lastGroundedTime;
    
    /// <summary>
    /// Whether the player is currently falling.
    /// </summary>
    public bool IsFalling => isFalling;
    
    /// <summary>
    /// Current horizontal movement speed.
    /// </summary>
    public float CurrentSpeed => new Vector3(velocity.x, 0, velocity.z).magnitude;
    
    /// <summary>
    /// Current velocity vector.
    /// </summary>
    public Vector3 Velocity => velocity;
    
    /// <summary>
    /// Current movement mode (Walking or Flying).
    /// </summary>
    public MovementMode CurrentMode => currentMode;
    
    /// <summary>
    /// Current movement input direction.
    /// </summary>
    public Vector3 MoveDirection => moveDirection;
    
    /// <summary>
    /// Whether the player is currently climbing stairs.
    /// </summary>
    public bool IsClimbingStairs => isClimbingStairs;
    
    // Constants for magic number elimination
    private const float MIN_GROUND_PENETRATION_THRESHOLD = 2.0f;
    private const float STAIR_CLIMB_UPWARD_BOOST_MIN = 5f;
    private const float STAIR_CLIMB_UPWARD_BOOST_MAX = 50f;
    private const float STAIR_CLIMB_UPWARD_BOOST_MULTIPLIER = 0.3f;
    private const float GROUND_STICK_VELOCITY = -5f;
    private const float WALL_JUMP_BLEND_FACTOR = 0.3f;
    private const float WALL_BOUNCE_FORCE = 30f;
    private const float MAX_HORIZONTAL_BOUNCE_SPEED = 200f;
    private const float EMERGENCY_UNSTICK_OFFSET = 10f;
    private const float MIN_SPEED_FOR_MOVEMENT_BOOST = 5f;
    private const float MIN_HORIZONTAL_VELOCITY_FOR_BOUNCE = 5f;
    private const float WALL_BOUNCE_DOT_THRESHOLD = 0.3f;
    private const float HIGH_UPWARD_VELOCITY_THRESHOLD = 100f;
    
    // Grounding debounce timers
    private float _lastTimeRawGrounded = -999f;
    private float _lastTimeRawAirborne = -999f;
    private int _consecutiveGroundedFrames = 0;
    private int _consecutiveAirborneFrames = 0;
    private bool _rawGroundedThisFrame = false;
    
    /// <summary>
    /// Whether the player is currently sliding (bridges to CleanAAACrouch).
    /// </summary>
    public bool IsSliding => crouchController != null ? crouchController.IsSliding : false;
    
    /// <summary>
    /// Whether the player is currently crouching (bridges to CleanAAACrouch).
    /// </summary>
    public bool IsCrouching => crouchController != null ? crouchController.IsCrouching : false;
    
    // ============================================================
    // VELOCITY SYSTEM - SINGLE SOURCE OF TRUTH
    // ============================================================
    // Only AAAMovementController modifies velocity.
    // External systems request velocity changes through controlled APIs.
    
    // External force system (replaces old external velocity)
    private Vector3 _externalForce = Vector3.zero;
    private float _externalForceDuration = 0f;
    private float _externalForceStartTime = -999f;
    private bool _externalForceOverridesGravity = false;
    
    // Legacy bridge (deprecated but maintained for compatibility)
    private Vector3 externalGroundVelocity = Vector3.zero;
    private bool useExternalGroundVelocity = false;

    // Air momentum preservation (for slide -> jump carry)
    private bool airMomentumLatched = false;
    private float airMomentumPreserveUntil = -999f;
    
    // Air control state - locks momentum direction when leaving ground
    private Vector3 airborneVelocitySnapshot = Vector3.zero;
    private bool wasGroundedLastFrame = true;
    private bool justPerformedWallJump = false; // Flag to prevent air control interference
    
    // Landing impact tracking for juice
    private float fallStartHeight = 0f;
    private bool isFalling = false;
    
    // Landing animation control - prevent spam on small bumps
    private float timeLeftGround = -999f; // When player became airborne
    private const float MIN_AIR_TIME_FOR_LAND_ANIM = 1.0f; // Minimum airtime before land animation plays
    private float lastLandingProcessedTime = -999f; // Last time we processed a landing
    private const float LANDING_COOLDOWN = 0.5f; // Cooldown between landing detections (prevents jitter spam)
    private float lastLandAnimationTime = -999f; // Last time land animation was played
    private const float LAND_ANIMATION_COOLDOWN = 5.0f; // Minimum time between land animations (increased to reduce spam)
    
    // Tactical dive override - when true, movement input is COMPLETELY IGNORED
    private bool isDiveOverrideActive = false;
    private float diveOverrideStartTime = -999f;

    void Awake()
    {
        // Auto-find components
        if (controller == null) controller = GetComponent<CharacterController>();
        if (cameraTransform == null) cameraTransform = Camera.main?.transform;

        // Cache animation system references
        _animationStateManager = FindObjectOfType<PlayerAnimationStateManager>();
        _layeredHandAnimationController = FindObjectOfType<LayeredHandAnimationController>();
        
        // Cache camera controller reference for wall jump tilt
        if (cameraTransform != null)
        {
            cameraController = cameraTransform.GetComponent<AAACameraController>();
            if (cameraController == null)
            {
                Debug.LogWarning("[AAA MOVEMENT] AAACameraController not found on camera! Wall jump tilt will not work.");
            }
        }
        
        // CRITICAL FIX: Initialize timeLeftGround to prevent 1000+ second air time on first landing
        // If player starts on ground, this prevents calculating from -999f
        timeLeftGround = Time.time;
        lastLandingProcessedTime = Time.time - LANDING_COOLDOWN; // Allow first landing immediately
        lastLandAnimationTime = Time.time - LAND_ANIMATION_COOLDOWN; // Allow first land animation immediately

        // Get reference to PlayerMovementManager for lock-on system
        playerMovementManager = FindObjectOfType<PlayerMovementManager>();
        if (playerMovementManager == null)
        {
            Debug.LogWarning("[AAA MOVEMENT] PlayerMovementManager not found! Lock-on platform following will not work.");
        }

        // External movement API methods moved below Awake()

        // Get reference to AAAMovementAudioManager for sound effects
        audioManager = GetComponent<AAAMovementAudioManager>();
        if (audioManager == null)
        {
            Debug.LogWarning("[AAA MOVEMENT] AAAMovementAudioManager not found! Movement sounds will not play.");
        }

        // Get reference to Rigidbody for movement system conflicts
        rb = GetComponent<Rigidbody>();
        
        // Get reference to PlayerEnergySystem for sprint energy management
        energySystem = GetComponent<PlayerEnergySystem>();
        if (energySystem == null)
        {
            Debug.LogWarning("[AAA MOVEMENT] PlayerEnergySystem not found! Sprint will work without energy cost.");
        }
        
        // Get reference to CleanAAACrouch for crouch speed reduction
        crouchController = GetComponent<CleanAAACrouch>();
        if (crouchController == null)
        {
            Debug.LogWarning("[AAA MOVEMENT] CleanAAACrouch not found! Crouch speed reduction will not work.");
        }
        
        // Get bleeding out system references
        playerHealth = GetComponent<PlayerHealth>();
        deathCameraController = FindObjectOfType<DeathCameraController>();
        
        if (playerHealth != null)
        {
            Debug.Log("[AAAMovementController] Found PlayerHealth - bleeding out mode ready");
        }
        if (deathCameraController != null)
        {
            Debug.Log("[AAAMovementController] Found DeathCameraController - camera-relative movement ready");
        }

        // If an AAAMovementIntegrator exists and preferred, defer F toggle to it
        if (preferIntegratorForModeToggle && FindObjectOfType<AAAMovementIntegrator>() != null)
        {
            allowInternalModeToggle = false;
        }
    }
    
    // Attempt to configure CelestialDriftController to use flight only and disable any falling state.
    // Uses reflection to avoid hard dependency on specific field/method names.
    private void TryConfigureCelestialFlightNoFalling(Component controllerComp)
    {
        if (controllerComp == null) return;
        try
        {
            Type t = controllerComp.GetType();
            BindingFlags BF = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            
            // 1) Try boolean toggles that look like falling flags
            string[] fallFlags = {
                "EnableFalling", "FallingEnabled", "AllowFalling", "UseFalling",
                "CelestialFalling", "Falling", "enableFalling", "fallingEnabled", "allowFalling"
            };
            foreach (string name in fallFlags)
            {
                var p = t.GetProperty(name, BF);
                if (p != null && p.CanWrite && p.PropertyType == typeof(bool))
                {
                    p.SetValue(controllerComp, false, null);
                }
                var f = t.GetField(name, BF);
                if (f != null && f.FieldType == typeof(bool))
                {
                    f.SetValue(controllerComp, false);
                }
            }
            
            // 2) Try methods to disable falling
            (string method, object[] args)[] fallMethods = new (string, object[])[]
            {
                ("DisableFalling", null),
                ("AbortFalling", null),
                ("StopFalling", null),
                ("SetFallingEnabled", new object[]{ false }),
                ("SetEnableFalling", new object[]{ false }),
                ("SetAllowFalling", new object[]{ false }),
                ("EnableFalling", new object[]{ false })
            };
            foreach (var cand in fallMethods)
            {
                var m = t.GetMethod(cand.method, BF);
                if (m != null)
                {
                    var pars = m.GetParameters();
                    if ((cand.args == null && pars.Length == 0) || (cand.args != null && pars.Length == cand.args.Length))
                    {
                        m.Invoke(controllerComp, cand.args);
                    }
                }
            }
            
            // 3) Try to force state/mode to Flight/Flying
            string[] stateProps = { "State", "Mode", "CurrentState", "CurrentMode" };
            foreach (var spName in stateProps)
            {
                var sp = t.GetProperty(spName, BF);
                if (sp != null && sp.CanWrite && sp.PropertyType.IsEnum)
                {
                    var names = Enum.GetNames(sp.PropertyType);
                    object val = null;
                    if (Array.Exists(names, n => n == "Flight")) val = Enum.Parse(sp.PropertyType, "Flight");
                    else if (Array.Exists(names, n => n == "Flying")) val = Enum.Parse(sp.PropertyType, "Flying");
                    else if (Array.Exists(names, n => n == "FlightMode")) val = Enum.Parse(sp.PropertyType, "FlightMode");
                    if (val != null)
                    {
                        sp.SetValue(controllerComp, val, null);
                    }
                }
            }
            
            // 4) Try to call an activation for flight
            string[] flightActivate = { "EnterFlight", "ActivateFlight", "EnableFlight", "StartFlight" };
            foreach (var mName in flightActivate)
            {
                var m = t.GetMethod(mName, BF);
                if (m != null && m.GetParameters().Length == 0)
                {
                    m.Invoke(controllerComp, null);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AAA MOVEMENT] Celestial flight integration failed: {ex.Message}");
        }
    }

    void OnEnable()
    {
        // This is the ONLY place we should set up the controller's dimensions.
        // It's called when the script is enabled, ensuring it's always correct.
        SetupControllerDimensions();
        _lastPosition = transform.position; // Init world-pos tracker for velocity proxy
        
        // Find movement system conflicts
        celestialDriftController = GetComponent<CelestialDriftController>();
        audioManager = GetComponent<AAAMovementAudioManager>();
        rb = GetComponent<Rigidbody>();
        
        // Crouch/slide initialization removed - handled by CleanAAACrouch system
        
        // Disable conflicting systems
        HandleMovementSystemConflicts(true);
        
        // Start in Walking mode
        currentMode = MovementMode.Walking;
        Debug.Log("[MOVEMENT] Starting in WALKING mode");
        
        
        // Lock-on is controlled by user pressing "R" - no automatic lock-on when enabling AAA mode
    }

    void OnDisable()
    {
        // === PRISTINE: Bidirectional cleanup contract ===
        // If dive override is active when AAA is disabled, clean it up
        // This ensures CleanAAACrouch doesn't get stuck thinking dive is still active
        if (isDiveOverrideActive)
        {
            isDiveOverrideActive = false;
            diveOverrideStartTime = -999f;
            _currentOwner = ControllerModificationSource.None;
            Debug.Log("[MOVEMENT] OnDisable: Dive override was active - cleaned up to prevent orphaned state");
        }
        
        // Clear any active external forces to prevent lingering velocity
        if (useExternalGroundVelocity || HasActiveExternalForce)
        {
            useExternalGroundVelocity = false;
            _externalForceStartTime = -999f;
            _externalForceDuration = 0f;
            externalGroundVelocity = Vector3.zero;
            _currentOwner = ControllerModificationSource.None;
            Debug.Log("[MOVEMENT] OnDisable: External forces cleared");
        }
        
        // Re-enable conflicting systems when this controller is turned off
        HandleMovementSystemConflicts(false);
    }

    private void SetupControllerDimensions()
    {
        if (controller == null) return;
        
        // Use player height for controller setup
        float targetHeight = playerHeight;
        controller.height = targetHeight;
        controller.radius = playerRadius;
        // Center should be at half height from the bottom of the capsule
        // This ensures the transform position is at the base of the character
        controller.center = new Vector3(0, targetHeight / 2f, 0);
        
        // Minimum skin width needed for collision detection, scaled for large world
        controller.skinWidth = 5.0f; // 10% of radius (50 * 0.1) - Unity recommended ratio
        
        // Step offset scaled for large world - allows stepping over small obstacles
        // CRITICAL FIX: Reduced to 0.025f (2.5% of height) to prevent slope bouncing
        // Even 5% (16 units) causes bouncing on slopes - logs show 60-unit falls every 0.2s
        // For buttery smooth slope walking like sliding, step offset must be MINIMAL
        // 2.5% = 8 units for 320-height character (same as standard 0.25 units for 30-height)
        controller.stepOffset = Mathf.Clamp(maxStepHeight, 0.1f, playerHeight * 0.025f);
        
        // Respect designer slope setting and process even tiny moves for accuracy
        controller.slopeLimit = maxSlopeAngle;
        controller.minMoveDistance = 0.0f;
        
        // Store original slope limit for bla bla restoration
        _originalSlopeLimitFromAwake = controller.slopeLimit;
        
        // PRISTINE: Store original controller values
        _originalStepOffset = controller.stepOffset;
        _originalMinMoveDistance = controller.minMoveDistance;
        
        Debug.Log($"[MOVEMENT] Controller dimensions set - Height: {targetHeight:F1}, Radius: {playerRadius:F1}, SlopeLimit: {_originalSlopeLimitFromAwake:F1}°");
        Debug.Log($"[MOVEMENT] Config Status - Using Config: {(config != null ? "YES" : "NO")}, MoveSpeed: {MoveSpeed:F1}, StepOffset: {controller.stepOffset:F1}");
        // FORCE RECOMPILE - Dynamic slope step offset system active
    }

    void Update()
    {
        // === CRITICAL: BLEEDING OUT MUTEX CHECK (MUST BE ABSOLUTE FIRST!) ===
        // If player is bleeding out, BleedOutMovementController has exclusive control
        // This prevents ANY movement processing that could conflict
        if (playerHealth != null && playerHealth.isBleedingOut)
        {
            return; // Completely skip all movement logic
        }
        
        // === PRIORITY 1: MOVING PLATFORM CHECK (MUST BE FIRST!) ===
        // If player is parented to elevator/moving platform, let parent handle movement
        if (enableMovingPlatformSupport)
        {
            DetectMovingPlatform();
            DetectNonParentingPlatform(); // NEW: Check for CharacterController.Move() platforms
            
            if (_isOnMovingPlatform)
            {
                // Platform handles ALL movement - do NOTHING else
                // Don't even check grounded state - let platform physics handle it
                return; // ← CRITICAL: Skip ALL logic including grounded checks!
            }
            
            // NEW: If on non-parenting platform, movement still works but platform applies additional movement
            // This is handled automatically in LateUpdate by the elevator, so we just continue normally
        }
        
        if (Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log($"[MOVEMENT] Switching from {currentMode} mode");
        }

        // Handle Mode Toggle Input (F key)
        HandleModeToggle();
        
        // The core logic loop, organized by movement mode
        if (currentMode == MovementMode.Walking)
        {
            // Walking mode: standard platformer physics with gravity
            // ZERO-JITTER: DON'T check grounded here - wait for FixedUpdate to finish platform movement
            // CheckGrounded() moved to LateUpdate() for perfect sync with platform physics
            
            // CRITICAL: Skip ALL input processing if dive override is active
            if (!isDiveOverrideActive)
            {
                // Check for stairs before processing movement (uses cached grounded state)
                if (enableStairClimbingAssist && IsGrounded)
                {
                    DetectAndHandleStairs();
                }
                
                HandleInputAndHorizontalMovement();
                HandleWalkingVerticalMovement();
            }
        }
        else // Flying mode
        {
            // Flying mode: free 3D movement with no gravity
            // Make sure we're not grounded in flying mode
            IsGrounded = false;
            HandleFlyingMovement();
        }
        
        // PHASE 4 COHERENCE: Unified external force system with ownership tracking
        bool hasActiveExternalForce = HasActiveExternalForce;
        
        if (hasActiveExternalForce)
        {
            // External force is active - apply it directly
            velocity = _externalForce;
            
            // Don't apply gravity if force overrides it
            if (!_externalForceOverridesGravity && currentMode == MovementMode.Walking && !IsGrounded)
            {
                velocity.y += Gravity * Time.deltaTime; // SCALED: Affected by slow-mo for extended air time
                if (velocity.y < -TerminalVelocity)
                {
                    velocity.y = -TerminalVelocity;
                }
            }
        }
        else if (!useExternalGroundVelocity)
        {
            // PHASE 4 COHERENCE: Reset ownership when no external forces active
            if (!isDiveOverrideActive && (_currentOwner == ControllerModificationSource.Crouch || _currentOwner == ControllerModificationSource.Dive))
            {
                _currentOwner = ControllerModificationSource.Movement;
            }
            
            // CRITICAL FIX: Gravity ALWAYS applies in walking mode when airborne
            // Dive override blocks INPUT, NOT GRAVITY!
            if (currentMode == MovementMode.Walking && !IsGrounded)
            {
                velocity.y += Gravity * Time.deltaTime; // SCALED: Affected by slow-mo for extended air time
                
                // Clamp to terminal velocity
                if (velocity.y < -TerminalVelocity)
                {
                    velocity.y = -TerminalVelocity;
                }
            }
            else if (IsGrounded && velocity.y > 0 && Time.time >= _suppressGroundedUntil)
            {
                // JUMP FIX: Don't zero upward velocity if we're in jump suppression window
                // Only apply ground stick if we're not jumping
                if (Time.time >= _suppressGroundedUntil)
                {
                    velocity.y = 0;
                }
            }
        }
        
        // Final step: move the controller.
        if (controller.enabled) {
            Vector3 velocityBeforeMove = velocity;
            
            // ✨ CAPTURE COLLISION FLAGS - This tells us what we hit (ground, walls, ceiling)
            // This is FREE data from the CharacterController - no raycasts needed!
            isCurrentlyTouchingWall = false; // Reset each frame - OnControllerColliderHit will set to true if we hit wall
            
            // ZERO-JITTER: Platform movement handled in FixedUpdate via MovePlatformPassenger
            // Just apply character's own velocity here
            lastCollisionFlags = controller.Move(velocity * Time.deltaTime);
            
            // Check if we lost wall contact (haven't touched wall in a while)
            if (Time.time - lastWallContactTime > 0.1f)
            {
                lastWallCollider = null; // Clear wall reference if not touching anymore
                LastWallHitPoint = Vector3.zero;
            }
        }
        
        // POST-MOVEMENT GROUND CORRECTION (Lightweight fallback for any remaining issues)
        if (currentMode == MovementMode.Walking && velocity.y <= 0)
        {
            CheckAndCorrectGroundPosition();
        }
        
        // STUCK DETECTION SYSTEM
        // Detect if player appears stuck in ground geometry and flag for emergency jump
        DetectStuckInGround();
    }
    
    // ZERO-JITTER: LateUpdate runs AFTER FixedUpdate (where platforms move passengers)
    // This ensures grounded checks happen AFTER all platform movement is complete
    void LateUpdate()
    {
        // Check grounded state AFTER all physics/platform movement
        if (currentMode == MovementMode.Walking)
        {
            CheckGrounded();
        }
        
        // ANTI-SINK PRE-MOVE GROUND PREDICTION (DISABLED - CAUSING FLOATING ISSUES)
        // The anti-sink prediction system was causing players to float above ground after jumps
        // and then slowly sink. This has been disabled for more reliable grounding.
        /* DISABLED ANTI-SINK SYSTEM
        if (enableAntiSinkPrediction)
        {
            // Check if we're about to move into the ground
            Vector3 predictedPosition = transform.position + (velocity * Time.deltaTime); // SCALED: Respects slow-mo

            if (!useExternalGroundVelocity && currentMode == MovementMode.Walking && velocity.y <= 0)
            {
                // Check if there's ground directly beneath where we're about to move
                RaycastHit predictHit;
                if (Physics.Raycast(new Vector3(predictedPosition.x, transform.position.y + 0.05f, predictedPosition.z),
                                    Vector3.down, out predictHit,
                                    groundPredictionDistance, groundMask, QueryTriggerInteraction.Ignore))
                {
                    // If we'll hit ground, adjust position to exactly match ground height + clearance
                    float targetPosY = predictHit.point.y + groundClearance;

                    // Only adjust if we would sink into ground
                    if (predictedPosition.y < targetPosY)
                    {
                        // Set position directly to ground height + clearance
                        predictedPosition.y = targetPosY;
                        transform.position = predictedPosition;

                        // Zero out vertical velocity to prevent further sinking
                        velocity.y = 0;

                        // Skip the controller.Move call since we've already positioned correctly
                        return;
                    }
                }
            }
        }
        */
    }

    [Header("=== GROUND PENETRATION PREVENTION ===")]
    [Tooltip("Distance to look ahead for ground before applying movement")]
    [SerializeField] private float groundPredictionDistance = 20.0f; // Scaled for 300-unit character
    [Tooltip("Extra height to keep character above ground")]
    [SerializeField] private float groundClearance = 1.0f; // Scaled for 300-unit character
    
    /// <summary>
    /// NEW: Detect if player is on a non-parenting platform (uses CharacterController.Move())
    /// OPTIMIZED: Caches elevator reference, only searches once
    /// </summary>
    private void DetectNonParentingPlatform()
    {
        // Fast path: If we have a current elevator, just verify we're still in it
        if (_currentElevator != null)
        {
            if (!_currentElevator.IsPlayerInElevator(controller))
            {
                Debug.Log("[MOVEMENT] Left non-parenting platform (elevator)");
                _currentElevator = null;
                _isOnNonParentingPlatform = false;
            }
            return; // Early exit - we're done
        }
        
        // Slow path: Only runs when we don't have an elevator cached
        // This only happens once when entering a new elevator
        ElevatorController[] elevators = FindObjectsOfType<ElevatorController>();
        foreach (var elevator in elevators)
        {
            if (elevator.IsPlayerInElevator(controller))
            {
                _currentElevator = elevator;
                _isOnNonParentingPlatform = true;
                Debug.Log("[MOVEMENT] ✅ Entered non-parenting platform (elevator) - Full movement control maintained!");
                break;
            }
        }
    }
    
    /// <summary>
    /// Detect if player has been parented to a moving platform (elevator, train, etc.)
    /// When parented, we disable controller.Move() to prevent fighting with parent transform.
    /// This ensures butter-smooth movement even at extreme speeds.
    /// </summary>
    private void DetectMovingPlatform()
    {
        // Check if parent transform changed
        if (transform.parent != _previousParent)
        {
            bool wasOnPlatform = _isOnMovingPlatform;
            _isOnMovingPlatform = (transform.parent != null);
            _previousParent = transform.parent;
            
            if (_isOnMovingPlatform && !wasOnPlatform)
            {
                // JUST PARENTED TO MOVING PLATFORM
                Debug.Log($"[MOVEMENT] ✅ Parented to moving platform: {transform.parent.name}");
                Debug.Log("[MOVEMENT] DISABLING controller.Move() to prevent jerkiness");
                
                // Reset velocity to prevent conflicts with platform motion
                velocity = Vector3.zero;
                _platformVelocity = Vector3.zero;
                _lastPlatformPosition = transform.parent.position;
                
                // Clear any external forces that might conflict
                ClearExternalForce();
                
                // CRITICAL: Disable CharacterController to prevent collision push-up
                if (controller != null)
                {
                    controller.enabled = false;
                    Debug.Log("[MOVEMENT] CharacterController DISABLED (preventing collision interference)");
                }
            }
            else if (!_isOnMovingPlatform && wasOnPlatform)
            {
                // JUST UNPARENTED FROM MOVING PLATFORM
                Debug.Log("[MOVEMENT] ✅ Unparented from moving platform");
                Debug.Log("[MOVEMENT] RESUMING controller.Move() control");
                
                // CRITICAL: Re-enable CharacterController
                if (controller != null)
                {
                    controller.enabled = true;
                    Debug.Log("[MOVEMENT] CharacterController RE-ENABLED");
                }
                
                // Inherit platform's velocity for smooth transition
                // This prevents sudden stops when leaving elevator
                velocity = _platformVelocity;
                Debug.Log($"[MOVEMENT] Inherited platform velocity: {_platformVelocity}");
                
                // Reset tracking
                _platformVelocity = Vector3.zero;
            }
        }
        
        // Track platform velocity each frame (for smooth unparenting)
        if (_isOnMovingPlatform && transform.parent != null)
        {
            Vector3 currentPlatformPos = transform.parent.position;
            _platformVelocity = (currentPlatformPos - _lastPlatformPosition) / Time.deltaTime; // SCALED: Respects slow-mo
            _lastPlatformPosition = currentPlatformPos;
        }
    }
    
    private void CheckGrounded()
    {
        // Use CharacterController.isGrounded - accurate at any scale
        if (controller == null)
        {
            IsGrounded = false;
            groundNormal = Vector3.up;
            lastGroundDistance = -1f;
            currentSlopeAngle = 0f;
            return;
        }

        // CharacterController's built-in grounded detection is INSTANT and ACCURATE
        // It uses the capsule's actual collision data - perfect for any scale!
        bool wasGrounded = IsGrounded;
        _rawGroundedThisFrame = controller.isGrounded;
        
        // STABILITY FIX: Debounce grounded state changes to prevent flickering
        // Require multiple consecutive frames in the same state before changing
        if (_rawGroundedThisFrame)
        {
            _consecutiveGroundedFrames++;
            _consecutiveAirborneFrames = 0;
            _lastTimeRawGrounded = Time.time;
        }
        else
        {
            _consecutiveAirborneFrames++;
            _consecutiveGroundedFrames = 0;
            _lastTimeRawAirborne = Time.time;
        }
        
        // Debug logging for troubleshooting
        if (showGroundingDebug && (_consecutiveGroundedFrames == 1 || _consecutiveAirborneFrames == 1))
        {
            Debug.Log($"[GROUNDING DEBUG] Raw: {_rawGroundedThisFrame}, Current: {IsGrounded}, " +
                     $"GroundedFrames: {_consecutiveGroundedFrames}, AirborneFrames: {_consecutiveAirborneFrames}");
        }
        
        // Apply hysteresis: Only change state if we have enough consecutive frames OR hysteresis time expired
        if (!IsGrounded && _consecutiveGroundedFrames >= groundedDebounceFrames)
        {
            // Transition to grounded
            IsGrounded = true;
            if (showGroundingDebug)
            {
                Debug.Log($"[GROUNDING DEBUG] ✅ STATE CHANGE: Airborne → Grounded (after {_consecutiveGroundedFrames} frames)");
            }
        }
        else if (IsGrounded && _consecutiveAirborneFrames >= groundedDebounceFrames)
        {
            // Check hysteresis timer before transitioning to airborne
            float timeSinceLastGrounded = Time.time - _lastTimeRawGrounded;
            if (timeSinceLastGrounded > groundedHysteresisSeconds)
            {
                // Hysteresis expired - transition to airborne
                IsGrounded = false;
                if (showGroundingDebug)
                {
                    Debug.Log($"[GROUNDING DEBUG] ✈️ STATE CHANGE: Grounded → Airborne (after {_consecutiveAirborneFrames} frames, {timeSinceLastGrounded:F3}s)");
                }
            }
            else if (showGroundingDebug && _consecutiveAirborneFrames == groundedDebounceFrames)
            {
                Debug.Log($"[GROUNDING DEBUG] ⏳ HYSTERESIS ACTIVE: Staying grounded ({timeSinceLastGrounded:F3}s / {groundedHysteresisSeconds:F3}s)");
            }
        }
        
        // PHASE 1 UNIFICATION: Cache ground distance AND slope angle for camera system and slope handling
        // PERFORMANCE: Only do SphereCast when grounded (no need when airborne)
        if (IsGrounded)
        {
            float radius = controller.radius * 0.9f;
            Vector3 origin = transform.position + Vector3.up * (radius + 0.1f);
            RaycastHit hit;
            
            // SphereCast to detect ground normal and slope angle
            if (Physics.SphereCast(origin, radius, Vector3.down, out hit, 
                                  GroundCheckDistance + 0.1f, groundMask, QueryTriggerInteraction.Ignore))
        {
            lastGroundDistance = hit.distance;
            groundNormal = hit.normal;
            
            // ZERO-JITTER PLATFORM SYSTEM: Register with platform for direct FixedUpdate movement
            CelestialPlatform platform = hit.collider.GetComponent<CelestialPlatform>();
            if (platform == null)
            {
                // Try parent in case the collider is on a child object
                platform = hit.collider.GetComponentInParent<CelestialPlatform>();
            }
            
            // CRITICAL FIX: If platform not found via groundMask, do SECOND check without layer mask!
            // This handles platforms on different layers (e.g., tower vs platform layer mismatch)
            if (platform == null)
            {
                RaycastHit platformHit;
                if (Physics.SphereCast(origin, radius, Vector3.down, out platformHit, 
                                      GroundCheckDistance + 0.1f, ~0, QueryTriggerInteraction.Ignore))
                {
                    platform = platformHit.collider.GetComponent<CelestialPlatform>();
                    if (platform == null)
                    {
                        platform = platformHit.collider.GetComponentInParent<CelestialPlatform>();
                    }
                    
                    if (platform != null)
                    {
                        Debug.Log($"[PLATFORM] Found CelestialPlatform via layer-agnostic check: {platform.name} (Layer: {platform.gameObject.layer})");
                    }
                }
            }
            
            if (platform != null)
            {
                // We're on a moving platform!
                if (_currentCelestialPlatform != platform)
                {
                    // Unregister from old platform
                    if (_currentCelestialPlatform != null)
                    {
                        _currentCelestialPlatform.UnregisterPassenger(this);
                    }
                    
                    // Register with new platform for FixedUpdate movement
                    platform.RegisterPassenger(this);
                    _currentCelestialPlatform = platform;
                    Debug.Log($"[PLATFORM] ✅ ZERO-JITTER: Registered with platform {platform.name} (FixedUpdate sync)");
                }
            }
            else if (_currentCelestialPlatform != null)
            {
                // We just left a platform - unregister
                _currentCelestialPlatform.UnregisterPassenger(this);
                Debug.Log($"[PLATFORM] Unregistered from platform: {_currentCelestialPlatform.name}");
                
                // Store momentum for jump inheritance - ONLY HORIZONTAL!
                // CRITICAL FIX: Zero out Y component to prevent inconsistent jump heights
                Vector3 platformVel = _currentCelestialPlatform.GetCurrentVelocity();
                _lastPlatformVelocity = new Vector3(platformVel.x, 0f, platformVel.z);
                _currentCelestialPlatform = null;
            }
            
            // CRITICAL FIX: Calculate slope angle correctly
            // Angle from vertical (0° = flat, 90° = wall)
            float angleFromUp = Vector3.Angle(Vector3.up, hit.normal);
            
            // If normal is pointing mostly upward (dot product > 0), it's a valid ground surface
            float dotUp = Vector3.Dot(hit.normal, Vector3.up);
            
            if (dotUp > 0.1f) // Normal pointing generally upward
            {
                currentSlopeAngle = angleFromUp;
            }
            else // Normal pointing down or sideways - treat as flat or invalid
            {
                currentSlopeAngle = 0f;
            }
            
            // Slope detected and angle calculated
            }
            else
            {
                // SphereCast didn't hit - treat as flat ground
                lastGroundDistance = 0f;
                groundNormal = Vector3.up;
                currentSlopeAngle = 0f;
            }
        }
        else
        {
            // Not grounded - reset slope data
            lastGroundDistance = -1f;
            groundNormal = Vector3.up;
            currentSlopeAngle = 0f;
            
            // MODERN: Unregister from platform when airborne
            if (_currentCelestialPlatform != null)
            {
                _currentCelestialPlatform.UnregisterPassenger(this);
                Debug.Log($"[PLATFORM] Left platform (airborne): {_currentCelestialPlatform.name}");
                
                // Store momentum for potential re-landing - ONLY HORIZONTAL!
                // CRITICAL FIX: Zero out Y component to prevent inconsistent jump heights
                Vector3 platformVel = _currentCelestialPlatform.GetCurrentVelocity();
                _lastPlatformVelocity = new Vector3(platformVel.x, 0f, platformVel.z);
                _currentCelestialPlatform = null;
            }
        }
        
        // CRITICAL: If on moving platform, force grounded state to prevent falling animation
        if (_isOnNonParentingPlatform)
        {
            IsGrounded = true; // Treat elevator floor as ground
        }
        
        // Track last grounded time for coyote time system
        if (IsGrounded)
        {
            lastGroundedTime = Time.time;
        }
        
        // Track when player leaves ground (for minimum air time check)
        if (wasGrounded && !IsGrounded)
        {
            timeLeftGround = Time.time;
            Debug.Log($"[GROUNDING] ✈️ LEFT GROUND (debounced) - Raw: {_rawGroundedThisFrame}, Frames airborne: {_consecutiveAirborneFrames}");
        }
        
        // Log instant grounded detection
        if (!wasGrounded && IsGrounded)
        {
            float airTime = Time.time - timeLeftGround;
            Debug.Log($"[GROUNDING] ✅ LANDED (debounced) - Air time: {airTime:F2}s, Raw: {_rawGroundedThisFrame}, Frames grounded: {_consecutiveGroundedFrames}");
        }
        
        // CRITICAL FIX: DO NOT override groundNormal to Vector3.up!
        // The correct slope normal was already calculated in the SphereCast above (line 1064)
        // Overwriting it to Vector3.up breaks slope descent physics!
        // groundNormal = Vector3.up; // ← REMOVED: This was causing slope walking to use flat ground physics

        // Reset jump capability when we land solidly
        if (IsGrounded && !canJump)
        {
            canJump = true;
            
            // Calculate how long player was in air
            float airTime = Time.time - timeLeftGround;
            
            // Check landing cooldown to prevent rapid re-triggers from jittery ground detection
            float timeSinceLastLanding = Time.time - lastLandingProcessedTime;
            if (timeSinceLastLanding < LANDING_COOLDOWN)
            {
                Debug.Log($"[GROUNDING] Landing ignored - Cooldown active ({timeSinceLastLanding:F2}s < {LANDING_COOLDOWN:F2}s)");
                return;
            }
            
            // Mark that we're processing this landing
            lastLandingProcessedTime = Time.time;
            
            // SMART LANDING SYSTEM: Only play Land animation if:
            // 1. Player was in air for minimum time (prevents spam on small bumps)
            // 2. Player is NOT sprinting (skip Land if still sprinting for instant resume)
            bool wasInAirLongEnough = airTime >= MIN_AIR_TIME_FOR_LAND_ANIM;
            bool isSprinting = energySystem != null && energySystem.IsCurrentlySprinting;
            
            if (_animationStateManager != null)
            {
                if (!wasInAirLongEnough)
                {
                    // Player was barely off ground - skip Land animation
                    Debug.Log($"[MOVEMENT] Air time too short ({airTime:F2}s < {MIN_AIR_TIME_FOR_LAND_ANIM:F2}s) - Skipping land animation");
                }
                else if (isSprinting)
                {
                    // Player is sprinting - skip Land animation, resume Sprint
                    Debug.Log($"[MOVEMENT] Sprinting detected - Skipping land animation, resuming sprint");
                }
                else
                {
                    // CHECK: Don't spam land animation if player landed recently
                    float timeSinceLastLandAnim = Time.time - lastLandAnimationTime;
                    if (timeSinceLastLandAnim < LAND_ANIMATION_COOLDOWN)
                    {
                        Debug.Log($"[MOVEMENT] Land animation on cooldown - Last played {timeSinceLastLandAnim:F2}s ago");
                    }
                    else
                    {
                        // Player was in air long enough - play Land animation
                        _animationStateManager.SetMovementState((int)PlayerAnimationStateManager.PlayerAnimationState.Land);
                        lastLandAnimationTime = Time.time;
                        Debug.Log($"[MOVEMENT] Playing land animation - Air time: {airTime:F2}s");
                    }
                }
            }
            
            // Play landing sound ONLY if player actually jumped AND was in air long enough
            if (hasJumped && (Time.time - timeOfJump) >= MIN_TIME_IN_AIR_FOR_LAND_SOUND)
            {
                GameSounds.PlayPlayerLand(transform.position);
                hasJumped = false; // Reset flag after landing
            }
            else if (hasJumped)
            {
                // Jump was too short (bunny hop), just reset flag without sound
                hasJumped = false;
            }
        }

        // Simple debug visualization - green when grounded, red when in air
        Vector3 debugOrigin = transform.position;
        Color debugColor = IsGrounded ? Color.green : Color.red;
        Debug.DrawRay(debugOrigin, Vector3.down * 5f, debugColor);
    }
    
    // POST-MOVEMENT GROUND CORRECTION
    // Lightweight fallback to handle any remaining ground penetration issues
    private void CheckAndCorrectGroundPosition()
    {
        if (controller == null || IsGrounded) return;
        
        float radius = controller.radius * 0.95f; // Slightly smaller to avoid edge issues
        Vector3 origin = transform.position + Vector3.up * (radius + radius * 0.05f);
        
        RaycastHit hit;
        if (Physics.SphereCast(origin, radius, Vector3.down, out hit, 
                              groundCheckDistance + 0.1f, groundMask, QueryTriggerInteraction.Ignore))
        {
            // If we're penetrating the ground, correct position
            float groundY = hit.point.y;
            float playerBottomY = transform.position.y - controller.height / 2f;
            
            if (playerBottomY < groundY)
            {
                // Calculate how much we're penetrating
                float penetration = groundY - playerBottomY;
                if (penetration > MIN_GROUND_PENETRATION_THRESHOLD)
                {
                    transform.position += Vector3.up * penetration;
                    velocity.y = 0; // Stop downward movement
                    
                    // Update grounded state
                    bool wasAirborne = !IsGrounded;
                    IsGrounded = true;
                    groundNormal = hit.normal;
                    
                    // Reset wall jump chain when landing
                    if (wasAirborne && WallJumpXPSimple.Instance != null)
                    {
                        WallJumpXPSimple.Instance.ResetChain();
                    }
                    
                    if (!canJump)
                        canJump = true;
                }
            }
        }
    }
    
    // STUCK DETECTION SYSTEM
    // Detect if player appears stuck in ground geometry for emergency jump system
    private void DetectStuckInGround()
    {
        if (controller == null) return;
        
        // Check if controller reports being grounded but we're not actually moving
        bool currentlyStuck = controller.isGrounded && 
                             velocity.magnitude > 1f && 
                             controller.velocity.magnitude < 1f;
        
        if (currentlyStuck && !wasStuckInGround)
        {
            Debug.LogWarning("[COLLISION] Player appears stuck in ground geometry");
            wasStuckInGround = true;
        }
        else if (!currentlyStuck)
        {
            wasStuckInGround = false;
        }
    }
    
    // ULTRA-ROBUST STAIR CLIMBING SYSTEM
    // Detects stairs ahead and assists with climbing to ensure smooth, reliable stair navigation
    private void DetectAndHandleStairs()
    {
        if (controller == null || moveDirection.magnitude < 0.1f)
        {
            isClimbingStairs = false;
            return;
        }
        
        // Get horizontal movement direction
        Vector3 horizontalDir = new Vector3(moveDirection.x, 0, moveDirection.z).normalized;
        if (horizontalDir.magnitude < 0.1f)
        {
            isClimbingStairs = false;
            return;
        }
        
        // Calculate check positions
        float checkHeight = controller.radius + controller.radius * 0.05f; // Start check slightly above ground (scaled)
        float maxCheckHeight = maxStepHeight + controller.radius;
        Vector3 basePosition = transform.position + Vector3.up * checkHeight;
        Vector3 forwardCheckPos = basePosition + horizontalDir * stairCheckDistance;
        
        // STEP 1: Check if there's an obstacle ahead at foot level
        RaycastHit lowHit;
        bool hasLowObstacle = Physics.Raycast(basePosition, horizontalDir, out lowHit, 
                                              stairCheckDistance, groundMask, QueryTriggerInteraction.Ignore);
        
        // Debug visualization
        Debug.DrawRay(basePosition, horizontalDir * stairCheckDistance, hasLowObstacle ? Color.yellow : Color.green);
        
        if (!hasLowObstacle)
        {
            isClimbingStairs = false;
            return;
        }
        
        // STEP 2: Check if there's clearance above the obstacle (can we step up?)
        Vector3 highCheckPos = basePosition + Vector3.up * maxStepHeight;
        RaycastHit highHit;
        bool hasClearanceAbove = !Physics.Raycast(highCheckPos, horizontalDir, out highHit,
                                                   stairCheckDistance, groundMask, QueryTriggerInteraction.Ignore);
        
        Debug.DrawRay(highCheckPos, horizontalDir * stairCheckDistance, hasClearanceAbove ? Color.green : Color.red);
        
        if (!hasClearanceAbove)
        {
            isClimbingStairs = false;
            return; // Can't climb - obstacle is too tall or there's a ceiling
        }
        
        // STEP 3: Check if there's a valid landing spot on top of the step
        Vector3 topCheckStart = forwardCheckPos + Vector3.up * maxStepHeight;
        RaycastHit topHit;
        bool hasValidLanding = Physics.Raycast(topCheckStart, Vector3.down, out topHit,
                                               maxStepHeight + 1f, groundMask, QueryTriggerInteraction.Ignore);
        
        Debug.DrawRay(topCheckStart, Vector3.down * (maxStepHeight + 1f), hasValidLanding ? Color.cyan : Color.magenta);
        
        if (!hasValidLanding)
        {
            isClimbingStairs = false;
            return; // No valid landing spot
        }
        
        // STEP 4: Validate step height is within acceptable range
        float stepHeight = topHit.point.y - transform.position.y;
        if (stepHeight < 0.1f || stepHeight > maxStepHeight)
        {
            isClimbingStairs = false;
            return; // Step is too small or too large
        }
        
        // STEP 5: Check slope angle - stairs should be relatively flat on top
        float slopeAngle = Vector3.Angle(topHit.normal, Vector3.up);
        if (slopeAngle > maxSlopeAngle)
        {
            isClimbingStairs = false;
            return; // Too steep to be a stair
        }
        
        // WE FOUND VALID STAIRS! Assist with climbing
        isClimbingStairs = true;
        
        // Apply stair climbing assistance
        if (smoothStepClimbing)
        {
            // Smooth upward velocity to climb the step naturally
            float climbSpeed = MoveSpeed * stairClimbSpeedMultiplier * Time.deltaTime; // SCALED: Respects slow-mo
            float upwardBoost = Mathf.Clamp(stepHeight / Time.deltaTime * STAIR_CLIMB_UPWARD_BOOST_MULTIPLIER, 
                                            STAIR_CLIMB_UPWARD_BOOST_MIN, 
                                            STAIR_CLIMB_UPWARD_BOOST_MAX);
            
            // Apply gentle upward force to help climb
            if (velocity.y < upwardBoost)
            {
                velocity.y = Mathf.Lerp(velocity.y, upwardBoost, Time.deltaTime * 10f); // SCALED: Respects slow-mo
            }
        }
        else
        {
            // Instant step-up (less smooth but more reliable for very tall steps)
            if (stepHeight > controller.stepOffset)
            {
                velocity.y = Mathf.Max(velocity.y, 20f);
            }
        }
        
        // Maintain horizontal speed while climbing (with optional slowdown)
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
        if (horizontalVelocity.magnitude > 0.1f)
        {
            float targetSpeed = MoveSpeed * stairClimbSpeedMultiplier;
            if (horizontalVelocity.magnitude > targetSpeed)
            {
                horizontalVelocity = horizontalVelocity.normalized * targetSpeed;
                velocity.x = horizontalVelocity.x;
                velocity.z = horizontalVelocity.z;
            }
        }
        
        Debug.DrawLine(transform.position, topHit.point, Color.blue, 0.1f);
    }
    
    private void HandleModeToggle()
    {
        if (!allowInternalModeToggle) return;
        // Toggle movement mode with F key
        if (Input.GetKeyDown(KeyCode.F))
        {
            MovementMode previousMode = currentMode;
            currentMode = (currentMode == MovementMode.Walking) ? MovementMode.Flying : MovementMode.Walking;
            modeJustChanged = true;
            
            // Integrate CelestialFlight only (no falling)
            if (celestialDriftController != null)
            {
                celestialDriftController.enabled = (currentMode == MovementMode.Flying) && enableCelestialFlightIntegration;
                if (celestialDriftController.enabled)
                {
                    TryConfigureCelestialFlightNoFalling(celestialDriftController);
                }
            }
            
            if (currentMode == MovementMode.Flying)
            {
                // Switching to flying mode - disable gravity

                CognitiveFeedManager.Instance?.QueueMessage("[FLIGHT SYSTEMS ONLINE - Gravitational constraints disengaged.]");
                
                // Reset vertical velocity when entering flying mode
                velocity.y = 0f;
            }
            else
            {
                // Switching to walking mode - enable gravity

                CognitiveFeedManager.Instance?.QueueMessage("[GROUND MOBILITY MODE - Gravitational physics restored.]");
                
                // If we're in the air when switching to walking mode, start falling
                if (!IsGrounded)
                {
                    velocity.y = -1f; // Small downward velocity to start falling
                }
                
                // Ensure celestial controller is disabled while in walking
                if (celestialDriftController != null)
                {
                    celestialDriftController.enabled = false;
                }
                
                // Reset jump capability
                canJump = true;
            }
        }
    }
    
    private void HandleFlyingMovement()
    {
        // Get raw input
        float inputX = Controls.HorizontalRaw();
        float inputY = Controls.VerticalRaw();
        
        // Calculate movement direction relative to camera
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        Vector3 up = Vector3.up; // Always use world up for consistency
        
        // Normalize directions (don't zero out Y for full 3D movement)
        forward.Normalize();
        right.Normalize();
        
        // Calculate movement direction (horizontal only, no vertical component from camera)
        Vector3 horizontalMovement = (forward * inputY + right * inputX).normalized;
        
        // Apply horizontal movement
        Vector3 targetVelocity = horizontalMovement * MoveSpeed;
        velocity.x = targetVelocity.x;
        velocity.z = targetVelocity.z;
        
        // Handle vertical movement (only jump key for upward thrust)
        if (Input.GetKey(Controls.UpThrustJump))
        {
            // Thrust upward with jump key - clamp time scale to prevent excessive thrust during slow motion
            velocity.y = JumpForce * 0.5f * Mathf.Clamp(Time.timeScale, 0.2f, 1.0f); // Reduced upward force for better control, scaled for time
        }
        else
        {
            // No vertical input - stop vertical movement (hover)
            velocity.y = 0f;
        }
        
        // No gravity in flying mode!
        IsGrounded = false;
    }

    private void HandleInputAndHorizontalMovement()
    {
        // === ISSUE #1 FIX: INPUT SUPPRESSION DURING SLIDE ===
        // AAA+ IMPLEMENTATION: Prevent input conflict between slide steering and movement system
        // When slide is active, CleanAAACrouch handles ALL input processing via its steering system
        // This prevents dual ownership conflicts and ensures buttery-smooth slide control
        if (crouchController != null && crouchController.IsSliding)
        {
            // Slide system has full control - skip ALL input processing here
            // External velocity is already set by CleanAAACrouch.UpdateSlide()
            // This eliminates jitter, stuttering, and input fighting
            return;
        }
        
        // Get Input using Controls script (no AZERTY/QWERTY confusion, immediate response)
        float rawInputX = Controls.HorizontalRaw();
        float rawInputY = Controls.VerticalRaw();
        
        // BLEEDING OUT MODE: Smooth input for heavy, labored crawl feel
        bool isBleedingOut = playerHealth != null && playerHealth.isBleedingOut;
        float inputX = rawInputX;
        float inputY = rawInputY;
        
        if (isBleedingOut)
        {
            // AAA SMOOTHING: Smooth damp for buttery crawl input
            Vector2 targetInput = new Vector2(rawInputX, rawInputY);
            currentBleedOutInput = Vector2.SmoothDamp(currentBleedOutInput, targetInput, ref bleedOutInputVelocity, 1f / bleedOutInputSmoothing, Mathf.Infinity, Time.unscaledDeltaTime); // UNSCALED
            inputX = currentBleedOutInput.x;
            inputY = currentBleedOutInput.y;
        }
        else
        {
            // AAA INPUT SMOOTHING: Smooth the raw binary input for buttery direction changes
            // This eliminates jerky strafe/forward/backward transitions
            Vector2 targetInput = new Vector2(rawInputX, rawInputY);
            currentSmoothedInput = Vector2.SmoothDamp(currentSmoothedInput, targetInput, ref inputSmoothVelocity, inputSmoothTime, Mathf.Infinity, Time.deltaTime);
            inputX = currentSmoothedInput.x;
            inputY = currentSmoothedInput.y;
            
            // Reset bleed out smoothing
            currentBleedOutInput = Vector2.zero;
            bleedOutInputVelocity = Vector2.zero;
        }
        
        Transform activeCameraTransform = cameraTransform; // Default to main camera
        
        if (isBleedingOut && deathCameraController != null)
        {
            // Get BleedOutCamera transform for camera-relative movement
            Camera bleedOutCam = GameObject.Find("BleedOutCamera")?.GetComponent<Camera>();
            if (bleedOutCam != null)
            {
                activeCameraTransform = bleedOutCam.transform;
            }
        }

        // 🎪 AERIAL TRICK MODE: Input relative to VELOCITY direction, not camera
        // This preserves momentum and makes tricks feel natural
        bool isInTrickMode = cameraController != null && cameraController.IsPerformingAerialTricks;
        
        Vector3 forward;
        Vector3 right;
        
        if (isInTrickMode && !IsGrounded)
        {
            // TRICK MODE: Use velocity direction as reference frame
            Vector3 currentHorizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
            float currentSpeed = currentHorizontalVelocity.magnitude;
            
            // If moving with significant speed, use velocity direction
            if (currentSpeed > 5f)
            {
                forward = currentHorizontalVelocity.normalized;
                right = Vector3.Cross(Vector3.up, forward).normalized;
            }
            else
            {
                // Low speed: use camera's horizontal rotation only (ignore pitch/roll from tricks)
                Vector3 cameraForwardFlat = new Vector3(activeCameraTransform.forward.x, 0, activeCameraTransform.forward.z);
                Vector3 cameraRightFlat = new Vector3(activeCameraTransform.right.x, 0, activeCameraTransform.right.z);
                
                if (cameraForwardFlat.magnitude > 0.1f)
                {
                    forward = cameraForwardFlat.normalized;
                    right = cameraRightFlat.normalized;
                }
                else
                {
                    // Fallback: world forward
                    forward = Vector3.forward;
                    right = Vector3.right;
                }
            }
        }
        else
        {
            // NORMAL MODE: Camera-relative movement (standard FPS controls)
            forward = activeCameraTransform.forward;
            right = activeCameraTransform.right;
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();
        }

        moveDirection = (forward * inputY + right * inputX).normalized;

        // Calculate target horizontal velocity with sprint and crouch support
        // CRITICAL FIX: Use MoveSpeed property (checks config) instead of moveSpeed field
        float currentMoveSpeed = MoveSpeed;
        bool isSprinting = false;
        bool isCrouching = false;
        
        // BLEEDING OUT MODE: Slow crawl speed
        if (isBleedingOut)
        {
            currentMoveSpeed *= bleedOutSpeedMultiplier; // 10x slower when bleeding out!
            // Can't sprint or crouch when bleeding out - already dying!
        }
        // Check if player is crouching (but NOT sliding/diving - those have their own velocity systems)
        else if (crouchController != null && crouchController.IsCrouching && !crouchController.IsSliding && !crouchController.IsDiving && !crouchController.IsDiveProne)
        {
            currentMoveSpeed *= 0.5f; // Half speed while crouching
            isCrouching = true;
        }
        
        // Check if player wants to sprint AND has energy available AND is actually moving
        bool hasMovementInput = Mathf.Abs(inputX) > 0.01f || Mathf.Abs(inputY) > 0.01f;
        
        // CRITICAL FIX: Disable sprint effect on air control during wall jump trajectory preservation
        // Sprint input while airborne creates targetHorizontalVelocity that fights wall jump momentum
        // Only allow sprint to affect movement when GROUNDED or when wall jump trajectory is fully released
        bool allowSprintInAir = IsGrounded || (Time.time > lastWallJumpTime + 0.5f);
        
        // Debug: Log when sprint is blocked during wall jump
        if (ShowWallJumpDebug && Input.GetKey(Controls.Boost) && !allowSprintInAir && !IsGrounded)
        {
            float timeSinceWallJump = Time.time - lastWallJumpTime;
            Debug.Log($"[WALL JUMP] Sprint blocked during trajectory preservation ({timeSinceWallJump:F2}s since wall jump)");
        }
        
        // Can't sprint while crouching OR bleeding out OR during wall jump trajectory preservation
        if (Input.GetKey(Controls.Boost) && hasMovementInput && !isCrouching && !isBleedingOut && allowSprintInAir)
        {
            if (energySystem != null)
            {
                // Only sprint if energy system allows it
                if (energySystem.CanSprint)
                {
                    currentMoveSpeed *= SprintMultiplier;
                    isSprinting = true;
                    // Consume energy while sprinting AND moving
                    energySystem.ConsumeSprint(Time.unscaledDeltaTime); // UNSCALED: Energy drain not affected by slow-mo
                    
                    // 🎯 DIRECTIONAL SPRINT: Track input direction for hand animations
                    currentSprintInput = new Vector2(inputX, inputY);
                }
                // If out of energy, can't sprint
            }
            else
            {
                // No energy system - sprint without restriction (legacy behavior)
                currentMoveSpeed *= SprintMultiplier;
                isSprinting = true;
                
                // 🎯 DIRECTIONAL SPRINT: Track input direction for hand animations
                currentSprintInput = new Vector2(inputX, inputY);
            }
        }
        else
        {
            // Not sprinting - clear sprint input
            currentSprintInput = Vector2.zero;
        }
        
        Vector3 targetHorizontalVelocity = moveDirection * currentMoveSpeed;
        
        // SNAPSHOT VELOCITY WHEN LEAVING GROUND - Lock momentum direction!
        // CRITICAL: Skip snapshot if we just wall jumped (wall jump sets its own velocity)
        if (wasGroundedLastFrame && !IsGrounded && !justPerformedWallJump)
        {
            // Just became airborne - snapshot current velocity
            airborneVelocitySnapshot = new Vector3(velocity.x, 0, velocity.z);
        }
        wasGroundedLastFrame = IsGrounded;
        
        // Clear wall jump flag after snapshot logic
        if (justPerformedWallJump && Time.time > wallJumpVelocityProtectionUntil)
        {
            justPerformedWallJump = false;
        }

        // === CRITICAL FIX: SLIDE INPUT SUPPRESSION ===
        // ISSUE #1 SOLUTION: When slide is active, it owns velocity via SetExternalVelocity
        // AAA must NOT process input to avoid fighting slide's steering system
        // This prevents the catastrophic race condition where both systems compete for velocity control
        bool isSlideActive = crouchController != null && crouchController.IsSliding;
        
        // PHASE 4 COHERENCE: Legacy external velocity system (deprecated but still supported)
        // New code should use SetExternalVelocity() instead
        if (useExternalGroundVelocity || isSlideActive)
        {
            // SLIDE PRIORITY: If slide is active, it controls velocity - skip AAA input processing
            if (isSlideActive)
            {
                // Slide owns velocity - AAA hands off control completely
                // Slide's steering system (CleanAAACrouch lines 1124-1165) handles WASD input
                // No velocity modification here to prevent conflict
            }
            else
            {
                // Legacy external velocity path (non-slide systems)
                velocity.x = externalGroundVelocity.x;
                velocity.z = externalGroundVelocity.z;
                // JUMP FIX: If an external system provides vertical bias, only apply downward forces
                // Don't override positive (upward) velocity during jumps
                if (Mathf.Abs(externalGroundVelocity.y) > 0f)
                {
                    // Only apply external downward velocity, preserve upward jump velocity
                    if (externalGroundVelocity.y < 0f && (velocity.y <= 0f || Time.time >= _suppressGroundedUntil))
                    {
                        velocity.y = externalGroundVelocity.y;
                    }
                }
            }
        }
        else
        {
            // Apply horizontal velocity from input unless we are preserving carried air momentum.
            bool preserveAir = airMomentumLatched && currentMode == MovementMode.Walking && (!IsGrounded || Time.time <= airMomentumPreserveUntil);
            if (!preserveAir)
            {
                // MOMENTUM-BASED AIR CONTROL - Feels AAA!
                if (IsGrounded)
                {
                    // === AAA+ ACCELERATION SYSTEM (SOURCE ENGINE STYLE) ===
                    // Industry standard: acceleration-based movement for responsive feel
                    // Benefits: Frame-rate independent, predictable physics, skill-based
                    
                    if (enableAccelerationSystem)
                    {
                        // Get current horizontal velocity
                        Vector3 currentHorizontalVel = new Vector3(velocity.x, 0, velocity.z);
                        float currentSpeed = currentHorizontalVel.magnitude;
                        
                        // Check if player has movement input
                        bool hasInput = Mathf.Abs(inputX) > 0.01f || Mathf.Abs(inputY) > 0.01f;
                        
                        if (hasInput)
                        {
                            // === ACCELERATION PHASE ===
                            // Calculate desired velocity from input
                            Vector3 desiredVelocity = targetHorizontalVelocity;
                            float desiredSpeed = desiredVelocity.magnitude;
                            
                            // Calculate velocity delta needed
                            Vector3 velocityDelta = desiredVelocity - currentHorizontalVel;
                            
                            // Apply slope momentum if enabled
                            float effectiveAcceleration = groundAcceleration;
                            if (enableSlopeMomentum && currentSlopeAngle > minimumSlopeAngle)
                            {
                                // Calculate slope factor (-1 = downhill, +1 = uphill)
                                Vector3 downhillDir = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized;
                                float slopeDirection = Vector3.Dot(moveDirection.normalized, downhillDir);
                                
                                if (slopeDirection > 0.1f)
                                {
                                    // Moving downhill - boost acceleration
                                    float slopeFactor = Mathf.Sin(currentSlopeAngle * Mathf.Deg2Rad);
                                    effectiveAcceleration *= (1f + (slopeAccelerationMultiplier * slopeFactor * slopeDirection));
                                }
                                else if (slopeDirection < -0.1f)
                                {
                                    // Moving uphill - reduce acceleration (more resistance)
                                    float slopeFactor = Mathf.Sin(currentSlopeAngle * Mathf.Deg2Rad);
                                    effectiveAcceleration /= (1f + (uphillFrictionMultiplier * slopeFactor * Mathf.Abs(slopeDirection)));
                                }
                            }
                            
                            // Frame-rate independent acceleration
                            float maxSpeedChange = effectiveAcceleration * Time.deltaTime;
                            
                            // Clamp velocity change to max acceleration
                            if (velocityDelta.magnitude > maxSpeedChange)
                            {
                                velocityDelta = velocityDelta.normalized * maxSpeedChange;
                            }
                            
                            // Apply acceleration
                            velocity.x += velocityDelta.x;
                            velocity.z += velocityDelta.z;
                        }
                        else
                        {
                            // === FRICTION PHASE (NO INPUT) ===
                            // Apply ground friction to slow down smoothly
                            if (currentSpeed > 0.01f)
                            {
                                // Speed-proportional friction (Source Engine model)
                                float frictionAmount = groundFriction;
                                
                                // Stronger friction at low speeds for snappy stops
                                if (currentSpeed < stopSpeed)
                                {
                                    frictionAmount *= (currentSpeed / stopSpeed) + 1f;
                                }
                                
                                // Apply friction force (frame-rate independent)
                                float frictionMagnitude = frictionAmount * Time.deltaTime;
                                
                                // Don't overshoot zero
                                if (frictionMagnitude >= currentSpeed)
                                {
                                    velocity.x = 0f;
                                    velocity.z = 0f;
                                }
                                else
                                {
                                    Vector3 frictionForce = -currentHorizontalVel.normalized * frictionMagnitude;
                                    velocity.x += frictionForce.x;
                                    velocity.z += frictionForce.z;
                                }
                            }
                        }
                    }
                    else
                    {
                        // LEGACY: Instant velocity (old system)
                        velocity.x = targetHorizontalVelocity.x;
                        velocity.z = targetHorizontalVelocity.z;
                    }
                }
                else
                {
                    // In air: momentum-based control with limited steering
                    // CRITICAL: Skip air control if wall jump velocity is protected
                    if (Time.time > wallJumpVelocityProtectionUntil)
                    {
                        // ENHANCED: Reduce air control strength after wall jump for smoother trajectory
                        // Check if we recently wall jumped (within 0.5s) for gradual control restoration
                        float timeSinceWallJump = Time.time - lastWallJumpTime;
                        bool recentWallJump = timeSinceWallJump < 0.5f;
                        
                        if (recentWallJump)
                        {
                            // Gradual air control restoration: 50% → 100% over 0.25s
                            float controlRestoration = Mathf.Clamp01((timeSinceWallJump - wallJumpAirControlLockoutTime) / 0.25f);
                            float reducedControl = Mathf.Lerp(0.5f, 1.0f, controlRestoration);
                            
                            // Temporarily reduce air control for smoother trajectory
                            float originalControl = airControlStrength;
                            airControlStrength *= reducedControl;
                            ApplyAirControl(targetHorizontalVelocity, inputX, inputY);
                            airControlStrength = originalControl; // Restore original value
                            
                            if (ShowWallJumpDebug && controlRestoration < 1.0f)
                            {
                                Debug.Log($"[MOVEMENT] Air control at {reducedControl * 100:F0}% (Trajectory preservation active)");
                            }
                        }
                        else
                        {
                            // Normal air control - wall jump trajectory fully released
                            ApplyAirControl(targetHorizontalVelocity, inputX, inputY);
                        }
                    }
                    // else: Wall jump velocity is protected - no air control modifications
                }
            }
        }

        // Auto-clear the air momentum latch once we've had a chance to restart slide or resume walking
        if (airMomentumLatched && (useExternalGroundVelocity || (IsGrounded && Time.time > airMomentumPreserveUntil)))
        {
            airMomentumLatched = false;
        }

    }

    private void HandleWalkingVerticalMovement()
    {
        // Update jump cooldown timer
        if (jumpCooldownTimer > 0)
        {
            jumpCooldownTimer -= Time.unscaledDeltaTime; // UNSCALED: Cooldown not affected by slow-mo
        }

        // If we are on the ground...
        if (IsGrounded)
        {
            airJumpRemaining = MaxAirJumps; // Reset double jump charges
            lastGroundedTime = Time.time; // Track for coyote time
            consecutiveWallJumps = 0; // Reset wall jump counter when landing
            
            // 🔒 ANTI-EXPLOIT: Clear last wall when touching ground
            lastWallJumpedFrom = null;
            lastWallJumpedInstanceID = 0;
            
            // MOMENTUM VISUALIZATION: Break chain on landing
            if (MomentumVisualization.Instance != null)
            {
                MomentumVisualization.Instance.BreakChain();
            }
            
            // Landing impact detection
            if (isFalling)
            {
                float fallDistance = fallStartHeight - transform.position.y;
                HandleLandingImpact(fallDistance);
                isFalling = false;
            }

            // Grounded state tracked
            
            // Only apply ground stick forces when not driven by an external ground velocity (e.g., slide)
            if (!useExternalGroundVelocity)
            {
                // Check if on slope for step offset adjustment
                
                // Apply ground stick force when not climbing stairs
                if (velocity.y <= 0 && !isClimbingStairs)
                {
                    // Slope angle detected - check if step offset needs adjustment
                    
                    // 🎯 BRILLIANT FIX: Disable step offset on slopes (like sliding system does!)
                    // This prevents bouncing caused by step offset trying to climb the slope
                    if (currentSlopeAngle > 5f && currentSlopeAngle <= MaxSlopeAngle)
                    {
                        // On slope: Set step offset to 0 for smooth descent
                        if (controller.stepOffset > 0.1f)
                        {
                            controller.stepOffset = 0f;
                        }
                    }
                    else
                    {
                        // On flat ground: Restore step offset for stairs
                        if (controller.stepOffset < _originalStepOffset - 0.1f)
                        {
                            controller.stepOffset = _originalStepOffset;
                        }
                    }
                    
                    // SLOPE DESCENT SYSTEM: Apply downward force based on slope angle
                    // This makes walking down slopes feel natural and smooth
                    // CRITICAL FIX: Reduced force to be proportional to movement speed, not arbitrary constant
                    if (currentSlopeAngle > 5f && currentSlopeAngle <= MaxSlopeAngle)
                    {
                        // Calculate slope descent force proportional to slope angle
                        // Steeper slopes = stronger downward pull (5° = 0%, 50° = 100%)
                        float slopeNormalized = Mathf.Clamp01((currentSlopeAngle - 5f) / (MaxSlopeAngle - 5f));
                        
                        // FIXED: Use movement speed as base force instead of SlopeForce constant
                        // This makes descent feel natural and proportional to walk speed
                        // Old: SlopeForce (10,000) * slopeNormalized = 5,580 units/s (WAY too fast!)
                        // New: MoveSpeed (900) * 0.5 * slopeNormalized = 252 units/s (smooth and controlled)
                        float descentPull = MoveSpeed * 0.5f * slopeNormalized * Time.deltaTime; // SCALED: Respects slow-mo
                        
                        // Apply descent force along the slope surface
                        Vector3 slopeDirection = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized;
                        velocity += slopeDirection * descentPull;
                        
                        // GENTLE SLOPE STICK: Keep player grounded on slopes without excessive downward velocity
                        // Clamped to reasonable range to prevent jump height contamination
                        velocity.y = Mathf.Clamp(velocity.y, -5f, -1f); // Constrained range for clean jumps
                    }
                    else
                    {
                        // Flat ground or very gentle slope - minimal stick force
                        velocity.y = Mathf.Clamp(velocity.y, -2f, 0f); // Allow slight upward for step climbing
                    }
                }
            }

            // JUMP BUFFERING - Remember jump input before landing
            if (Input.GetKeyDown(Controls.UpThrustJump))
            {
                jumpBufferedTime = Time.time;
            }
            
            // BULLETPROOF JUMPING - ALWAYS WORKS (with buffer support)
            bool jumpedThisFrame = false;
            if (Input.GetKeyDown(Controls.UpThrustJump) || (Time.time - jumpBufferedTime <= jumpBufferTime))
            {
                jumpedThisFrame = HandleBulletproofJump();
                if (jumpedThisFrame)
                {
                    jumpBufferedTime = -999f; // Clear buffer on successful jump
                }
            }

            // CONTROL KEY DOES NOTHING when grounded (as requested)
            // Controls.DownThrust is ignored in walking mode when grounded
        }
        else // If we are in the air...
        {
            // Gravity is applied centrally in Update() when not grounded for consistency
            
            // Track fall height for landing impact
            // CRITICAL: Don't set falling state if on moving platform (elevator)!
            if (!isFalling && velocity.y < 0 && !_isOnNonParentingPlatform)
            {
                fallStartHeight = transform.position.y;
                isFalling = true;
            }
            
            // CRITICAL: Clear falling state if on platform (prevents falling animation in elevator)
            if (isFalling && _isOnNonParentingPlatform)
            {
                isFalling = false;
            }
            
            // VARIABLE JUMP HEIGHT - Release jump early for shorter jumps
            if (enableVariableJumpHeight && Input.GetKeyUp(Controls.UpThrustJump) && velocity.y > 0)
            {
                velocity.y *= JumpCutMultiplier; // Cut jump short
            }
            
            // COYOTE TIME - Can still jump briefly after leaving ground
            if (Input.GetKeyDown(Controls.UpThrustJump))
            {
                if (Time.time - lastGroundedTime <= CoyoteTime)
                {
                    // Coyote jump!
                    HandleBulletproofJump();
                }
                else
                {
                    // Buffer the jump for landing
                    jumpBufferedTime = Time.time;
                }
            }

            // WALL JUMP DETECTION & EXECUTION (takes priority over double jump)
            bool performedWallJump = false;
            if (enableWallJump && Input.GetKeyDown(Controls.UpThrustJump))
            {
                // Check if we can wall jump
                Collider detectedWallCollider = null;
                if (CanWallJump() && DetectWall(out Vector3 wallNormal, out Vector3 hitPoint, out detectedWallCollider))
                {
                    // 🔒 ANTI-EXPLOIT: Check if this is a NEW wall (not the one we just jumped from)
                    if (IsNewWall(detectedWallCollider))
                    {
                        PerformWallJump(wallNormal, detectedWallCollider);
                        performedWallJump = true; // Flag to prevent double jump on same frame
                    }
                    else if (ShowWallJumpDebug) // CRITICAL: Use property!
                    {
                        Debug.Log("[JUMP] 🔒 Wall jump BLOCKED - Cannot jump off same wall twice in a row!");
                    }
                }
            }
            
            // Handle Double Jump Input (ONLY if we didn't just wall jump)
            if (!performedWallJump && Input.GetKeyDown(Controls.UpThrustJump) && airJumpRemaining > 0)
            {
                // CONSERVATION STYLE: Preserve upward momentum + add boost
                // If rising: Adds to momentum (chain bonus!)
                // If falling: Starts fresh (safety net)
                velocity.y = Mathf.Max(velocity.y, 0) + DoubleJumpForce;
                airJumpRemaining--;

                // Play double jump sound using the centralized sound system
                GameSounds.PlayPlayerDoubleJump(transform.position);
                
                // Trigger jump animation for double jump
                if (_animationStateManager != null)
                {
                    _animationStateManager.SetMovementState((int)PlayerAnimationStateManager.PlayerAnimationState.Jump);
                    Debug.Log("[JUMP] Double jump animation triggered - Conservation style (preserved upward momentum)");
                }
            }
        }
    }

    private bool HandleBulletproofJump()
    {
        float timeSinceLastJump = Time.time - lastJumpTime;
        float timeSinceLastAttempt = Time.time - lastJumpAttemptTime;

        // Track consecutive attempts for emergency mode
        if (timeSinceLastAttempt > 1f)
        {
            consecutiveJumpAttempts = 0;
        }
        consecutiveJumpAttempts++;
        lastJumpAttemptTime = Time.time;

        // Respect minimum cooldown to prevent spam
        if (timeSinceLastJump < jumpCooldown)
        {
            return false;
        }

        // EMERGENCY MODE: If multiple jump attempts in short time, we're probably stuck
        bool emergencyMode = consecutiveJumpAttempts >= 3;
        float jumpPower = emergencyMode ? emergencyJumpForce : JumpForce;

            // Jump conditions - at least ONE must be true:
        bool normalJump = IsGrounded && canJump;
        bool stuckJump = !IsGrounded && velocity.y < 1f;
        bool emergencyJump = emergencyMode;
        bool groundedButStuck = IsGrounded && Mathf.Abs(velocity.y) < 0.1f;

        if (normalJump || stuckJump || emergencyJump || groundedButStuck)
        {
            // Clear external forces when jumping
            if (useExternalGroundVelocity)
            {
                ClearExternalGroundVelocity();
                Debug.Log("[JUMP] Cleared external ground velocity");
            }
            if (HasActiveExternalForce)
            {
                ClearExternalForce();
                Debug.Log("[JUMP] Cleared external force");
            }

            // ARCHITECTURAL FIX: Clean velocity state for consistent jumps
            // PROBLEM: Slope descent adds accumulative velocity over multiple frames
            // SOLUTION: Zero Y-axis completely, then rebuild ONLY what we want
            
            // Step 1: Preserve horizontal velocity (player movement + any existing momentum)
            float currentHorizontalSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
            Vector3 horizontalDirection = new Vector3(velocity.x, 0f, velocity.z);
            if (currentHorizontalSpeed > 0.01f)
            {
                horizontalDirection = horizontalDirection.normalized;
            }
            
            // Step 2: Zero Y to remove ALL slope contamination (descent pulls downward over time)
            velocity.y = 0f;

            // Step 3: Apply pure jump force to clean Y-axis
            velocity.y = jumpPower;
            
            // Step 4: Add platform momentum (HORIZONTAL ONLY, pre-cleaned in CheckGrounded)
            // This preserves platform movement feel while keeping jump HEIGHT consistent
            if (_currentCelestialPlatform != null)
            {
                Vector3 platformDelta = _currentCelestialPlatform.GetMovementDelta();
                Vector3 platformVelocity = platformDelta / Time.fixedDeltaTime;
                
                // Add ONLY horizontal platform velocity (Y already zero from CheckGrounded line 1239)
                velocity.x += platformVelocity.x;
                velocity.z += platformVelocity.z;
                
                Debug.Log($"[JUMP] Platform momentum: ({platformVelocity.x:F1}, {platformVelocity.z:F1}) | Player speed: {currentHorizontalSpeed:F0} | Jump: {jumpPower:F1}");
            }
            else if (_lastPlatformVelocity.sqrMagnitude > 0.001f)
            {
                // Inherit 50% of last platform velocity for smooth transition off platform
                velocity.x += _lastPlatformVelocity.x * 0.5f;
                velocity.z += _lastPlatformVelocity.z * 0.5f;
                
                Debug.Log($"[JUMP] Last platform momentum (50%): ({_lastPlatformVelocity.x * 0.5f:F1}, {_lastPlatformVelocity.z * 0.5f:F1}) | Jump: {jumpPower:F1}");
            }
            else if (currentSlopeAngle > 5f)
            {
                // === RAMP JUMP SYSTEM ===
                // Give speed boost when jumping off downhill slopes (like real ramp jumps!)
                if (enableSlopeMomentum && currentSlopeAngle > minimumSlopeAngle)
                {
                    // Check if moving downhill
                    Vector3 downhillDir = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized;
                    Vector3 currentHorizontalVel = new Vector3(velocity.x, 0, velocity.z);
                    float slopeDirection = Vector3.Dot(currentHorizontalVel.normalized, downhillDir);
                    
                    if (slopeDirection > 0.3f && currentHorizontalSpeed > 100f)
                    {
                        // Moving downhill with speed - RAMP JUMP!
                        // Apply speed bonus (scales with slope angle)
                        float slopeFactor = Mathf.Sin(currentSlopeAngle * Mathf.Deg2Rad);
                        float speedBonus = currentHorizontalSpeed * rampJumpBonus * slopeFactor;
                        
                        // Add bonus in current movement direction
                        velocity.x += horizontalDirection.x * speedBonus;
                        velocity.z += horizontalDirection.z * speedBonus;
                        
                        Debug.Log($"[RAMP JUMP] Speed boost! Angle: {currentSlopeAngle:F1}°, Bonus: {speedBonus:F1}, New Speed: {new Vector3(velocity.x, 0, velocity.z).magnitude:F1}");
                    }
                    else
                    {
                        Debug.Log($"[JUMP] Clean slope jump: {currentSlopeAngle:F1}° slope, horizontal speed: {currentHorizontalSpeed:F0}, jump: {jumpPower:F1}");
                    }
                }
                else
                {
                    Debug.Log($"[JUMP] Clean slope jump: {currentSlopeAngle:F1}° slope, horizontal speed: {currentHorizontalSpeed:F0}, jump: {jumpPower:F1}");
                }
            }
            
            // Suppress grounded detection for jump duration
            float suppressionTime = Mathf.Max(0.25f, Mathf.Abs(jumpPower / Gravity) * 0.5f);
            _suppressGroundedUntil = Time.time + suppressionTime;
            Debug.Log($"[JUMP] Applied {jumpPower:F1} jump force, suppressing grounded for {suppressionTime:F2}s");

            // Emergency unstick: Move slightly upward if stuck in geometry
            if (emergencyMode || stuckJump)
            {
                Vector3 unstickOffset = Vector3.up * EMERGENCY_UNSTICK_OFFSET;
                if (controller.enabled)
                {
                    controller.enabled = false;
                    transform.position += unstickOffset;
                    controller.enabled = true;
                }
                Debug.Log($"[JUMP] Emergency unstick applied with {jumpPower:F1} jump force");
            }

            lastJumpTime = Time.time;
            timeOfJump = Time.time; // Record exact jump time
            canJump = false;
            hasJumped = true; // Mark that player actually jumped
            IsGrounded = false; // Force ungrounded state
            
            // CRITICAL FIX: Clear cached platform velocity after jump to prevent reuse
            // This ensures future jumps don't inherit stale platform data
            _lastPlatformVelocity = Vector3.zero;
            
            // Update last grounded time to prevent immediate coyote time
            lastGroundedTime = Time.time - CoyoteTime - 0.01f;

            // Reset attempt counter on successful jump
            consecutiveJumpAttempts = 0;

            // Play jump sound
            if (audioManager != null)
            {
                audioManager.PlayJumpSound();
            }
            else
            {
                GameSounds.PlayPlayerJump(transform.position);
            }

            // Trigger jump animation
            if (_animationStateManager != null)
            {
                _animationStateManager.SetMovementState((int)PlayerAnimationStateManager.PlayerAnimationState.Jump);
                Debug.Log("[JUMP] Jump animation triggered");
            }

            Debug.Log($"[JUMP] Success - Mode: {(emergencyMode ? "EMERGENCY" : "NORMAL")}, Power: {jumpPower:F1}");
            return true;
        }

        Debug.Log($"[JUMP] Blocked - Attempts: {consecutiveJumpAttempts}, Cooldown: {timeSinceLastJump:F2}s");
        return false;
    }

    private void HandleMovementSystemConflicts(bool disableOthers)
    {
        if (celestialDriftController == null)
            celestialDriftController = GetComponent<CelestialDriftController>();


        if (disableOthers)
        {
            // Disable flight controller
            if (celestialDriftController != null)
            {
                celestialDriftWasEnabled = celestialDriftController.enabled;
                celestialDriftController.enabled = false;
            }
            // Disable Rigidbody physics
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.detectCollisions = false; // Prevent any physics interaction while CC drives movement
            }
        }
        else // Re-enable others
        {
            if (celestialDriftController != null && celestialDriftWasEnabled)
            {
                celestialDriftController.enabled = true;
            }
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.detectCollisions = true; // Restore collisions for flight mode
            }
        }
    }
    


    
    // ----------------------------------------------------------------------
    // Rigidbody Velocity Proxy
    // ----------------------------------------------------------------------
    /// <summary>
    /// While the CharacterController governs grounded movement (Walking mode) we
    /// still want particle systems to inherit the true *world* velocity of the
    /// player, including any motion imparted by a moving platform.  The
    /// CharacterController.velocity property does NOT include platform motion,
    /// so we compute the frame-to-frame world displacement instead.
    /// </summary>
    private Vector3 _lastPosition; // Tracks previous position for velocity calc

    

    private void FixedUpdate()
    {
        // Skip physics updates when on moving platform
        if (_isOnMovingPlatform)
        {
            _lastPosition = transform.position;
            return;
        }
        
        if (currentMode != MovementMode.Walking) { _lastPosition = transform.position; return; }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) return;

        // World-space velocity: distance moved since last physics step
        Vector3 worldVelocity = (transform.position - _lastPosition) / Time.fixedDeltaTime;
        
        // IMPORTANT: Velocity tracking on kinematic rigidbody is REQUIRED for:
        // - Particle systems to get correct velocity info for projectiles when moving fast
        // - Dynamic hand movement animation systems
        rb.linearVelocity = worldVelocity; // supply to Inherit Velocity module

        _lastPosition = transform.position;
    }
    

    // ============================================================
    // PUBLIC API - VELOCITY CONTROL
    // ============================================================
    // All velocity modifications go through these methods
    
    /// <summary>
    /// ZERO-JITTER: Called by platform in FixedUpdate to apply movement immediately.
    /// Direct transform manipulation ensures perfect sync with platform physics.
    /// </summary>
    /// <param name="movementDelta">The movement vector the platform moved this frame</param>
    public void MovePlatformPassenger(Vector3 movementDelta)
    {
        // Direct transform move for zero-jitter synchronization
        if (controller != null && controller.enabled)
        {
            controller.Move(movementDelta);
        }
    }
    
    /// <summary>
    /// Sets external velocity override with duration-based expiration.
    /// This is the correct way to override player velocity from external systems.
    /// Respects wall jump velocity protection window.
    /// PHASE 4: Now tracks ownership to prevent system conflicts.
    /// </summary>
    /// <param name="force">The velocity vector to SET (world space) - replaces current velocity</param>
    /// <param name="duration">How long to apply this velocity (seconds)</param>
    /// <param name="overrideGravity">If true, gravity is NOT applied during this velocity override</param>
    public void SetExternalVelocity(Vector3 force, float duration, bool overrideGravity = false)
    {
        // Protect wall jump velocity from external override
        if (Time.time <= wallJumpVelocityProtectionUntil)
        {
            if (ShowWallJumpDebug)
            {
                Debug.Log($"[VELOCITY] Wall jump protection active - Blending external velocity (Protected for {wallJumpVelocityProtectionUntil - Time.time:F2}s)");
            }
            // IMPROVED: Blend instead of rejecting completely
            force = Vector3.Lerp(this.velocity, force, WALL_JUMP_BLEND_FACTOR);
        }
        
        _externalForce = force;
        _externalForceDuration = duration;
        _externalForceOverridesGravity = overrideGravity;
        _externalForceStartTime = Time.time;
        
        // PHASE 4 COHERENCE: Track ownership (Crouch/Dive systems use this)
        if (!isDiveOverrideActive && _currentOwner != ControllerModificationSource.Dive)
        {
            _currentOwner = ControllerModificationSource.Crouch; // Assume crouch/slide if not dive
        }
        
        if (ShowWallJumpDebug)
            Debug.Log($"[VELOCITY] SetExternalVelocity - Magnitude: {force.magnitude:F1}, Duration: {duration:F2}s, OverrideGravity: {overrideGravity}, Owner: {_currentOwner}");
    }
    
    /// <summary>
    /// Add velocity to current velocity (additive, not replacement).
    /// Use this for impulses like explosions or boosts.
    /// </summary>
    public void AddVelocity(Vector3 additiveVelocity)
    {
        // Protect wall jump velocity
        if (Time.time <= wallJumpVelocityProtectionUntil)
        {
            if (ShowWallJumpDebug)
            {
                Debug.Log($"[VELOCITY] Wall jump protection active - Scaling additive velocity (Protected for {wallJumpVelocityProtectionUntil - Time.time:F2}s)");
            }
            // Scale down additive velocity during wall jump protection
            additiveVelocity *= WALL_JUMP_BLEND_FACTOR;
        }
        
        velocity += additiveVelocity;
        Debug.Log($"[VELOCITY] AddVelocity - Added: {additiveVelocity.magnitude:F1}, New total: {velocity.magnitude:F1}");
    }
    
    /// <summary>
    /// Get current velocity (READ ONLY - external systems should not modify directly)
    /// </summary>
    public Vector3 GetVelocity() => velocity;
    
    /// <summary>
    /// Clear any active external forces
    /// </summary>
    public void ClearExternalForce()
    {
        _externalForce = Vector3.zero;
        _externalForceDuration = 0f;
        _externalForceStartTime = -999f;
        _externalForceOverridesGravity = false;
        
        // PHASE 4 COHERENCE: Reset ownership when external force cleared
        if (_currentOwner == ControllerModificationSource.Crouch || _currentOwner == ControllerModificationSource.Dive)
        {
            _currentOwner = ControllerModificationSource.Movement;
        }
        
        Debug.Log("[VELOCITY] External force cleared");
    }
    
    // ============================================================
    // LEGACY COMPATIBILITY BRIDGE
    // ============================================================
    
    /// <summary>
    /// LEGACY: Sets external ground velocity. Use SetExternalVelocity() instead.
    /// </summary>
    [System.Obsolete("Use SetExternalVelocity() for new code. This method is maintained for backward compatibility.")]
    public void SetExternalGroundVelocity(Vector3 v)
    {
        // Protect wall jump velocity from external override
        if (Time.time <= wallJumpVelocityProtectionUntil)
        {
            if (ShowWallJumpDebug)
            {
                Debug.Log($"[VELOCITY] Wall jump protection active - External velocity blocked (Protected for {wallJumpVelocityProtectionUntil - Time.time:F2}s)");
            }
            return; // Don't override wall jump velocity
        }
        
        externalGroundVelocity = v;
        useExternalGroundVelocity = true;
    }

    /// <summary>
    /// LEGACY: Clears external ground velocity. Use ClearExternalForce() instead.
    /// </summary>
    [System.Obsolete("Use ClearExternalForce() for new code. This method is maintained for backward compatibility.")]
    public void ClearExternalGroundVelocity()
    {
        useExternalGroundVelocity = false;
        externalGroundVelocity = Vector3.zero;
        
        // PHASE 4 COHERENCE: Reset ownership when external velocity cleared
        if (_currentOwner == ControllerModificationSource.Crouch || _currentOwner == ControllerModificationSource.Dive)
        {
            _currentOwner = ControllerModificationSource.Movement;
        }
    }

    /// <summary>
    /// Preserves horizontal momentum when transitioning to air (e.g., slide jump).
    /// </summary>
    /// <param name="v">Momentum vector to latch</param>
    public void LatchAirMomentum(Vector3 v)
    {
        velocity.x = v.x;
        velocity.z = v.z;
        // Preserve horizontal momentum while airborne and for a brief, gravity-scaled landing window
        // so CleanAAACrouch can restart sliding with full carried speed.
        airMomentumLatched = true;
        float g = Mathf.Abs(Gravity);
        float window = Mathf.Clamp(0.08f + 40f / Mathf.Max(200f, g), 0.08f, 0.18f);
        airMomentumPreserveUntil = Time.time + window;
    }

    /// <summary>
    /// Immediately launches player into air with specified velocity.
    /// </summary>
    /// <param name="v">Launch velocity vector</param>
    public void LaunchAir(Vector3 v)
    {
        velocity = v;
        IsGrounded = false;
    }
    
    /// <summary>
    /// Adds upward velocity (for push zones, updrafts, etc.).
    /// </summary>
    /// <param name="upwardSpeed">Upward velocity to add</param>
    public void AddUpwardVelocity(float upwardSpeed)
    {
        velocity.y += upwardSpeed;
        
        // Force player into air state if velocity is upward
        if (velocity.y > 0)
        {
            IsGrounded = false;
            // Suppress grounded checks based on velocity
            float suppressionTime = Mathf.Max(0.2f, Mathf.Abs(velocity.y / Gravity) * 0.5f);
            _suppressGroundedUntil = Time.time + suppressionTime;
            
            Debug.Log($"[VELOCITY] AddUpwardVelocity: {upwardSpeed:F1}, Total Y: {velocity.y:F1}, Suppression: {suppressionTime:F2}s");
        }
    }
    
    /// <summary>
    /// Sets upward velocity directly (overrides current Y velocity).
    /// </summary>
    /// <param name="upwardSpeed">Upward velocity to set</param>
    public void SetUpwardVelocity(float upwardSpeed)
    {
        velocity.y = upwardSpeed;
        
        // Force player into air state
        IsGrounded = false;
        // Suppress grounded checks
        float suppressionTime = Mathf.Max(0.25f, Mathf.Abs(upwardSpeed / Gravity) * 0.5f);
        _suppressGroundedUntil = Time.time + suppressionTime;
        
        Debug.Log($"[VELOCITY] SetUpwardVelocity: {upwardSpeed:F1}, Suppression: {suppressionTime:F2}s");
    }
    
    /// <summary>
    /// Sets complete velocity vector (for jump pads that need to control all axes).
    /// </summary>
    /// <param name="newVelocity">New velocity vector</param>
    public void SetVelocity(Vector3 newVelocity)
    {
        velocity = newVelocity;
        
        // Force player into air state if moving upward
        if (newVelocity.y > 0)
        {
            IsGrounded = false;
            float suppressionTime = Mathf.Max(0.25f, Mathf.Abs(newVelocity.y / Gravity) * 0.5f);
            _suppressGroundedUntil = Time.time + suppressionTime;
            
            Debug.Log($"[VELOCITY] SetVelocity - Magnitude: {newVelocity.magnitude:F1}, Suppression: {suppressionTime:F2}s");
        }
    }
    
    /// <summary>
    /// Public method for external systems (like trick jump) to trigger a jump
    /// Uses the same bulletproof jump system as spacebar
    /// </summary>
    public void TriggerJumpFromExternalSystem()
    {
        HandleBulletproofJump();
    }
    
    /// <summary>
    /// 🎪 TRICK JUMP: Public method for camera controller to trigger trick jump with momentum boost
    /// EDGE CASE SAFE: Only applies boost if jump succeeds, respects all momentum systems
    /// </summary>
    public void TriggerTrickJumpFromExternalSystem()
    {
        // Set flag to identify this as a trick jump
        isTrickJump = true;
        
        // Attempt the jump using bulletproof system
        bool jumpSucceeded = HandleBulletproofJump();
        
        // Only apply boost if jump actually succeeded
        if (jumpSucceeded && enableTrickJumpBoost)
        {
            ApplyTrickJumpMomentumBoost();
        }
        
        // Clear flag immediately after
        isTrickJump = false;
    }
    
    /// <summary>
    /// 🎪 APPLY TRICK JUMP MOMENTUM BOOST
    /// EDGE CASE SAFE: Respects existing momentum, doesn't break wall jumps, scales with speed
    /// Similar to wall jump boost system but tuned for aerial tricks
    /// </summary>
    private void ApplyTrickJumpMomentumBoost()
    {
        // EDGE CASE #1: Safety check - should never happen but prevents crashes
        if (!enableTrickJumpBoost)
        {
            if (showTrickJumpDebug)
                Debug.LogWarning("[TRICK JUMP] Boost called but disabled in settings!");
            return;
        }
        
        // EDGE CASE #2: Don't apply boost if we're in an invalid state
        if (currentMode != MovementMode.Walking)
        {
            if (showTrickJumpDebug)
                Debug.Log("[TRICK JUMP] Boost skipped - not in walking mode");
            return;
        }
        
        // EDGE CASE #3: Don't interfere with wall jump momentum (wall jump takes priority)
        if (Time.time < wallJumpVelocityProtectionUntil)
        {
            if (showTrickJumpDebug)
                Debug.Log("[TRICK JUMP] Boost skipped - wall jump velocity protected");
            return;
        }
        
        // EDGE CASE #4: Don't apply if external forces are active (dive, knockback, etc.)
        if (HasActiveExternalForce)
        {
            if (showTrickJumpDebug)
                Debug.Log("[TRICK JUMP] Boost skipped - external force active");
            return;
        }
        
        // Calculate current horizontal velocity and speed
        Vector3 currentHorizontal = new Vector3(velocity.x, 0, velocity.z);
        float currentSpeed = currentHorizontal.magnitude;
        
        // EDGE CASE #5: If stationary, apply boost in camera forward direction
        Vector3 boostDirection;
        if (currentSpeed < 0.1f)
        {
            // No movement - use camera forward direction (projected to horizontal plane)
            if (cameraTransform != null)
            {
                Vector3 cameraForward = cameraTransform.forward;
                boostDirection = new Vector3(cameraForward.x, 0, cameraForward.z).normalized;
            }
            else
            {
                // Fallback to player forward if no camera
                boostDirection = transform.forward;
            }
            
            if (showTrickJumpDebug)
                Debug.Log("[TRICK JUMP] Stationary - using camera forward for boost");
        }
        else
        {
            // Moving - boost in current movement direction (preserves momentum!)
            boostDirection = currentHorizontal.normalized;
        }
        
        // Calculate vertical boost (simple additive)
        float verticalBoost = trickJumpVerticalBoost;
        
        // Calculate forward boost with speed scaling
        float forwardBoost = trickJumpForwardBoost;
        
        // EDGE CASE #6: Speed scaling only applies above threshold (prevents weird low-speed behavior)
        if (currentSpeed > trickJumpSpeedThreshold)
        {
            // Scale boost with speed, but cap it to prevent infinite scaling
            float speedBonus = currentSpeed * trickJumpSpeedScaling;
            speedBonus = Mathf.Min(speedBonus, trickJumpMaxSpeedBonus);
            forwardBoost += speedBonus;
            
            if (showTrickJumpDebug)
                Debug.Log($"[TRICK JUMP] Speed scaling active - Speed: {currentSpeed:F1}, Bonus: {speedBonus:F1}");
        }
        
        // Apply vertical boost (additive to existing upward velocity)
        velocity.y += verticalBoost;
        
        // Apply horizontal boost (additive to existing horizontal velocity)
        Vector3 horizontalBoost = boostDirection * forwardBoost;
        velocity.x += horizontalBoost.x;
        velocity.z += horizontalBoost.z;
        
        // EDGE CASE #7: Update airborne velocity snapshot for air control system
        // This ensures air control uses the boosted velocity as baseline
        airborneVelocitySnapshot = new Vector3(velocity.x, 0, velocity.z);
        
        // EDGE CASE #8: Clear air momentum latch if active (trick jump creates new momentum)
        if (airMomentumLatched)
        {
            airMomentumLatched = false;
            if (showTrickJumpDebug)
                Debug.Log("[TRICK JUMP] Cleared air momentum latch - trick jump momentum takes priority");
        }
        
        // Calculate final stats for debug
        float finalSpeed = new Vector3(velocity.x, 0, velocity.z).magnitude;
        float finalHeight = velocity.y;
        
        if (showTrickJumpDebug)
        {
            Debug.Log($"🎪 [TRICK JUMP] BOOST APPLIED!\n" +
                     $"  Vertical: +{verticalBoost:F1} (Final Y velocity: {finalHeight:F1})\n" +
                     $"  Forward: +{forwardBoost:F1} (Final horizontal speed: {finalSpeed:F1})\n" +
                     $"  Direction: {boostDirection}\n" +
                     $"  Initial Speed: {currentSpeed:F1}");
        }
    }
    
    // ===== Integrator Bridge APIs (explicit, reflection-free) =====
    // Allow external systems (e.g., AAAMovementIntegrator) to set state safely
    public void SetMovementMode(MovementMode mode)
    {
        currentMode = mode;
        modeJustChanged = true;
    }

    public void SetGroundedImmediate(bool grounded)
    {
        IsGrounded = grounded;
        if (grounded && velocity.y < 0f)
        {
            velocity.y = 0f;
        }
    }

    /// <summary>
    /// DANGEROUS: Immediately set velocity with priority system.
    /// ONLY use this for dive system or when you absolutely need direct control.
    /// Prefer SetExternalVelocity() for most use cases.
    /// </summary>
    /// <param name="v">Velocity to set</param>
    /// <param name="priority">Priority level (0=normal, 1=high, 2+=critical). Wall jump protection=1.</param>
    public void SetVelocityImmediate(Vector3 v, int priority = 0)
    {
        // Priority-based wall jump protection
        if (Time.time <= wallJumpVelocityProtectionUntil && priority < 1)
        {
            if (ShowWallJumpDebug)
            {
                Debug.LogWarning($"[VELOCITY] SetVelocityImmediate blocked - Priority too low ({priority} < 1). Wall jump protection active.");
            }
            return;
        }
        
        velocity = v;
        Debug.Log($"[VELOCITY] SetVelocityImmediate - Magnitude: {v.magnitude:F1}, Priority: {priority}");
    }
    
    /// <summary>
    /// LEGACY: Backward compatibility wrapper for old AddExternalForce calls.
    /// DEPRECATED: Use SetExternalVelocity() instead.
    /// </summary>
    [System.Obsolete("Use SetExternalVelocity() instead - name is more accurate")]
    public void AddExternalForce(Vector3 force, float duration, bool overrideGravity = false)
    {
        SetExternalVelocity(force, duration, overrideGravity);
    }
    
    /// <summary>
    /// Enables dive override mode - blocks all movement input.
    /// </summary>
    public void EnableDiveOverride()
    {
        isDiveOverrideActive = true;
        diveOverrideStartTime = Time.time;
        _currentOwner = ControllerModificationSource.Dive;
        Debug.Log("[MOVEMENT] Dive override enabled - Input blocked, Ownership: Dive");
    }
    
    /// <summary>
    /// Disables dive override mode - restores movement input.
    /// </summary>
    public void DisableDiveOverride()
    {
        isDiveOverrideActive = false;
        diveOverrideStartTime = -999f;
        
        // PHASE 4 COHERENCE: Restore ownership to Movement
        if (_currentOwner == ControllerModificationSource.Dive)
        {
            _currentOwner = ControllerModificationSource.Movement;
        }
        
        Debug.Log("[MOVEMENT] Dive override disabled - Input restored, Ownership: Movement");
    }
    
    // ============================================================
    // PHASE 4 COHERENCE: CHARACTERCONTROLLER COORDINATION API
    // ============================================================
    // CleanAAACrouch calls these to modify CharacterController safely
    
    /// <summary>
    /// PRISTINE: Request slope limit override with automatic stack management.
    /// Supports nested overrides - restoring pops stack to previous value.
    /// </summary>
    public bool RequestSlopeLimitOverride(float newSlopeLimit, ControllerModificationSource source)
    {
        if (controller == null) return false;
        
        // Check if we can grant permission
        bool canModify = _currentOwner == ControllerModificationSource.None || 
                        _currentOwner == ControllerModificationSource.Movement ||
                        (source == ControllerModificationSource.Crouch && _currentOwner == ControllerModificationSource.Crouch) ||
                        (source == ControllerModificationSource.Dive && _currentOwner == ControllerModificationSource.Dive);
        
        if (canModify)
        {
            // Push current value onto stack before changing
            _slopeLimitStack.Push(new SlopeLimitOverride 
            { 
                slopeLimit = controller.slopeLimit, 
                source = source, 
                timestamp = Time.time 
            });
            
            controller.slopeLimit = newSlopeLimit;
            Debug.Log($"[CONTROLLER] Slope limit set to {newSlopeLimit:F1}° by {source} (Stack depth: {_slopeLimitStack.Count})");
            return true;
        }
        
        Debug.LogWarning($"[CONTROLLER] Slope limit modification denied - Owner: {_currentOwner}, Requester: {source}");
        return false;
    }
    
    /// <summary>
    /// PRISTINE: Restore slope limit - pops stack to previous override or original.
    /// CRITICAL FIX: Always ensures slope limit is restored properly to prevent 90° getting stuck.
    /// </summary>
    public void RestoreSlopeLimitToOriginal()
    {
        if (controller == null) return;
        
        // Pop the most recent override from stack
        if (_slopeLimitStack.Count > 0)
        {
            _slopeLimitStack.Pop(); // Remove current
            
            // Restore to previous in stack, or original if stack empty
            if (_slopeLimitStack.Count > 0)
            {
                var previous = _slopeLimitStack.Peek();
                controller.slopeLimit = previous.slopeLimit;
                Debug.Log($"[CONTROLLER] ✅ Slope limit restored to previous {previous.slopeLimit:F1}° by {previous.source} (Stack depth: {_slopeLimitStack.Count})");
            }
            else
            {
                controller.slopeLimit = _originalSlopeLimitFromAwake;
                Debug.Log($"[CONTROLLER] ✅ Slope limit restored to original {_originalSlopeLimitFromAwake:F1}° (Stack empty)");
            }
        }
        else
        {
            // Stack empty, just restore to original - CRITICAL for slide recovery
            controller.slopeLimit = _originalSlopeLimitFromAwake;
            Debug.Log($"[CONTROLLER] ✅ Slope limit FORCE RESTORED to original {_originalSlopeLimitFromAwake:F1}° (Emergency recovery)");
        }
        
        // PARANOID CHECK: Verify restoration actually worked
        if (Mathf.Abs(controller.slopeLimit - _originalSlopeLimitFromAwake) > 0.1f && _slopeLimitStack.Count == 0)
        {
            Debug.LogWarning($"[CONTROLLER] ⚠️ Slope limit restoration failed! Expected {_originalSlopeLimitFromAwake:F1}°, got {controller.slopeLimit:F1}°. Force setting...");
            controller.slopeLimit = _originalSlopeLimitFromAwake;
        }
    }
    
    /// <summary>
    /// PRISTINE: Request permission to override stepOffset with ownership tracking.
    /// </summary>
    public bool RequestStepOffsetOverride(float newStepOffset, ControllerModificationSource source)
    {
        if (controller == null) return false;
        
        // Check if we can modify (same ownership rules as slope limit)
        bool canModify = !_stepOffsetOverridden ||
                        (source == ControllerModificationSource.Crouch && _stepOffsetOwner == ControllerModificationSource.Crouch) ||
                        (source == ControllerModificationSource.Dive && _stepOffsetOwner == ControllerModificationSource.Dive);
        
        if (canModify)
        {
            if (!_stepOffsetOverridden)
            {
                _originalStepOffset = controller.stepOffset; // Store current value first time
            }
            
            controller.stepOffset = newStepOffset;
            _stepOffsetOverridden = true;
            _stepOffsetOwner = source;
            Debug.Log($"[CONTROLLER] Step offset set to {newStepOffset:F2} by {source}");
            return true;
        }
        
        Debug.LogWarning($"[CONTROLLER] Step offset modification denied - Owner: {_stepOffsetOwner}, Requester: {source}");
        return false;
    }
    
    /// <summary>
    /// PRISTINE: Restore stepOffset to original value.
    /// CRITICAL FIX: Ensures stepOffset is always restored to prevent getting stuck on slopes.
    /// </summary>
    public void RestoreStepOffsetToOriginal(ControllerModificationSource source)
    {
        if (controller == null) return;
        
        // Only allow the owner to restore
        if (_stepOffsetOverridden && _stepOffsetOwner == source)
        {
            controller.stepOffset = _originalStepOffset;
            _stepOffsetOverridden = false;
            Debug.Log($"[CONTROLLER] ✅ Step offset restored to original {_originalStepOffset:F2} by {source}");
            
            // PARANOID CHECK: Verify restoration worked
            if (Mathf.Abs(controller.stepOffset - _originalStepOffset) > 0.01f)
            {
                Debug.LogWarning($"[CONTROLLER] ⚠️ Step offset restoration failed! Expected {_originalStepOffset:F2}, got {controller.stepOffset:F2}. Force setting...");
                controller.stepOffset = _originalStepOffset;
            }
        }
    }
    
    /// <summary>
    /// PRISTINE: Request permission to override minMoveDistance with ownership tracking.
    /// </summary>
    public bool RequestMinMoveDistanceOverride(float newMinMoveDistance, ControllerModificationSource source)
    {
        if (controller == null) return false;
        
        bool canModify = !_minMoveDistanceOverridden ||
                        (source == ControllerModificationSource.Crouch && _minMoveDistanceOwner == ControllerModificationSource.Crouch) ||
                        (source == ControllerModificationSource.Dive && _minMoveDistanceOwner == ControllerModificationSource.Dive);
        
        if (canModify)
        {
            if (!_minMoveDistanceOverridden)
            {
                _originalMinMoveDistance = controller.minMoveDistance;
            }
            
            controller.minMoveDistance = newMinMoveDistance;
            _minMoveDistanceOverridden = true;
            _minMoveDistanceOwner = source;
            Debug.Log($"[CONTROLLER] MinMoveDistance set to {newMinMoveDistance:F4} by {source}");
            return true;
        }
        
        Debug.LogWarning($"[CONTROLLER] MinMoveDistance modification denied - Owner: {_minMoveDistanceOwner}, Requester: {source}");
        return false;
    }
    
    /// <summary>
    /// PRISTINE: Restore minMoveDistance to original value.
    /// CRITICAL FIX: Ensures minMoveDistance is always restored properly.
    /// </summary>
    public void RestoreMinMoveDistanceToOriginal(ControllerModificationSource source)
    {
        if (controller == null) return;
        
        if (_minMoveDistanceOverridden && _minMoveDistanceOwner == source)
        {
            controller.minMoveDistance = _originalMinMoveDistance;
            _minMoveDistanceOverridden = false;
            Debug.Log($"[CONTROLLER] ✅ MinMoveDistance restored to original {_originalMinMoveDistance:F4} by {source}");
            
            // PARANOID CHECK: Verify restoration worked
            if (Mathf.Abs(controller.minMoveDistance - _originalMinMoveDistance) > 0.0001f)
            {
                Debug.LogWarning($"[CONTROLLER] ⚠️ MinMoveDistance restoration failed! Expected {_originalMinMoveDistance:F4}, got {controller.minMoveDistance:F4}. Force setting...");
                controller.minMoveDistance = _originalMinMoveDistance;
            }
        }
    }
    
    /// <summary>
    /// Get the original slope limit set in Awake (for restoration).
    /// </summary>
    public float GetOriginalSlopeLimit() => _originalSlopeLimitFromAwake;
    
    /// <summary>
    /// Enum for identifying which system is requesting controller modifications.
    /// </summary>
    public enum ControllerModificationSource
    {
        None,      // ADDED: No system owns the controller
        Movement,
        Crouch,
        Dive,
        External
    }
    
    // ============================================================
    // PRIVATE - AIR CONTROL SYSTEM
    // ============================================================
    private void ApplyAirControl(Vector3 targetHorizontalVelocity, float inputX, float inputY)
    {
        // Get current horizontal velocity
        Vector3 currentHorizontal = new Vector3(velocity.x, 0, velocity.z);
        float currentSpeed = currentHorizontal.magnitude;
        
        // If no input, maintain current momentum perfectly (camera look doesn't affect movement!)
        if (Mathf.Abs(inputX) < 0.01f && Mathf.Abs(inputY) < 0.01f)
        {
            // No input = pure momentum preservation, camera rotation has ZERO effect
            return;
        }
        
        // HIGH SPEED MOMENTUM PRESERVATION - Sprint jumps feel AMAZING
        float effectiveAirControl = airControlStrength;
        if (preserveHighSpeedMomentum && currentSpeed > highSpeedThreshold)
        {
            // Reduce air control at high speeds for better momentum feel
            float speedRatio = Mathf.Clamp01((currentSpeed - highSpeedThreshold) / highSpeedThreshold);
            effectiveAirControl = Mathf.Lerp(airControlStrength, airControlStrength * 0.5f, speedRatio);
        }
        
        // Player is giving input - calculate steering adjustment
        // This is relative to camera but only affects velocity by airControlStrength amount
        Vector3 desiredChange = targetHorizontalVelocity - currentHorizontal;
        
        // Apply air control strength - this limits how much you can change direction
        Vector3 airControlChange = desiredChange * effectiveAirControl;
        
        // Apply acceleration smoothing for natural feel
        float maxChangeThisFrame = airAcceleration * Time.deltaTime; // SCALED: Respects slow-mo for extended air time
        if (airControlChange.magnitude > maxChangeThisFrame)
        {
            airControlChange = airControlChange.normalized * maxChangeThisFrame;
        }
        
        // Apply the change
        Vector3 newHorizontal = currentHorizontal + airControlChange;
        
        // Clamp to max air speed ONLY if we're not already going faster (preserve sprint jumps!)
        if (currentSpeed <= maxAirSpeed && newHorizontal.magnitude > maxAirSpeed)
        {
            newHorizontal = newHorizontal.normalized * maxAirSpeed;
        }
        
        // Apply to velocity
        velocity.x = newHorizontal.x;
        velocity.z = newHorizontal.z;
        
        // Preserve vertical velocity (jumping/falling)
    }
    
    // ============================================================
    // PRIVATE - LANDING IMPACT SYSTEM
    // ============================================================
    private float _lastLandingSoundTime = -999f;
    private const float LANDING_SOUND_COOLDOWN = 0.15f;
    private const float MIN_FALL_DISTANCE_FOR_IMPACT = 10f;
    
    private void HandleLandingImpact(float fallDistance)
    {
        // Only trigger on significant falls
        if (fallDistance < 10f) return;
        
        // Prevent sound spam on bumpy terrain (e.g., steep slopes causing rapid bouncing)
        if (Time.time - _lastLandingSoundTime < LANDING_SOUND_COOLDOWN)
        {
            return; // Too soon after last landing sound
        }
        
        // Scale impact by fall distance
        float impactStrength = Mathf.Clamp01(fallDistance / 100f);
        
        // CameraShake.Instance?.Shake(impactStrength * 0.3f, 0.2f);
        
        // FIXED: Play landing sound for all significant falls (not just 50+ units)
        // This ensures wall jumps trigger landing sounds
        GameSounds.PlayPlayerLand(transform.position);
        _lastLandingSoundTime = Time.time;
        
        Debug.Log($"[MOVEMENT] Landing impact - Fall distance: {fallDistance:F1} units");
    }
    
    // ============================================================
    // PRIVATE - WALL JUMP SYSTEM
    // ============================================================
    
    /// <summary>
    /// Check if player can perform a wall jump right now
    /// </summary>
    private bool CanWallJump()
    {
        // Must not be grounded
        if (IsGrounded)
        {
            if (ShowWallJumpDebug) Debug.Log("[JUMP] Wall jump blocked - Player is grounded"); // CRITICAL: Use property!
            return false;
        }
        
        // Check cooldown
        float timeSinceLastWallJump = Time.time - lastWallJumpTime;
        if (timeSinceLastWallJump < WallJumpCooldown) // CRITICAL: Use property!
        {
            if (ShowWallJumpDebug) Debug.Log($"[JUMP] Wall jump blocked - Cooldown active ({timeSinceLastWallJump:F2}s < {WallJumpCooldown:F2}s)");
            return false;
        }
        
        // Grace period - don't detect walls immediately after wall jump
        if (timeSinceLastWallJump < WallJumpGracePeriod) // CRITICAL: Use property!
        {
            if (ShowWallJumpDebug) Debug.Log($"[JUMP] Wall jump blocked - Grace period ({timeSinceLastWallJump:F2}s < {WallJumpGracePeriod:F2}s)");
            return false;
        }
        
        // Check consecutive wall jump limit
        if (consecutiveWallJumps >= MaxConsecutiveWallJumps) // CRITICAL: Use property!
        {
            if (ShowWallJumpDebug) Debug.Log($"[JUMP] Wall jump blocked - Max consecutive reached ({consecutiveWallJumps}/{MaxConsecutiveWallJumps})");
            return false;
        }
        
        // Must be falling at minimum speed (prevents wall jump spam at apex)
        if (velocity.y > -MinFallSpeedForWallJump) // CRITICAL: Use property!
        {
            if (ShowWallJumpDebug) Debug.Log($"[JUMP] Wall jump blocked - Not falling fast enough (velocity.y: {velocity.y:F2} > -{MinFallSpeedForWallJump:F2})");
            return false;
        }
        
        if (ShowWallJumpDebug) Debug.Log("[JUMP] Wall jump conditions met"); // CRITICAL: Use property!
        return true;
    }
    
    /// <summary>
    /// 🔒 ANTI-EXPLOIT: Check if the detected wall is the same one we just jumped from
    /// Returns true if this is a NEW wall (safe to jump), false if it's the SAME wall (blocked)
    /// </summary>
    private bool IsNewWall(Collider wallCollider)
    {
        if (wallCollider == null) return true; // No collider = allow (shouldn't happen but safe)
        
        // Check if this is the exact same collider we just jumped from
        if (lastWallJumpedFrom != null && lastWallJumpedFrom == wallCollider)
        {
            if (ShowWallJumpDebug) // CRITICAL: Use property!
            {
                Debug.Log($"[JUMP] 🔒 BLOCKED - Same wall detected: {wallCollider.gameObject.name}");
            }
            return false;
        }
        
        // Backup check using instance ID (in case collider reference changed)
        int currentID = wallCollider.GetInstanceID();
        if (lastWallJumpedInstanceID != 0 && lastWallJumpedInstanceID == currentID)
        {
            if (ShowWallJumpDebug) // CRITICAL: Use property!
            {
                Debug.Log($"[JUMP] 🔒 BLOCKED - Same wall detected (by ID): {wallCollider.gameObject.name}");
            }
            return false;
        }
        
        // This is a different wall - allow wall jump!
        if (ShowWallJumpDebug) // CRITICAL: Use property!
        {
            string lastWallName = lastWallJumpedFrom != null ? lastWallJumpedFrom.gameObject.name : "None";
            Debug.Log($"[JUMP] ✅ NEW WALL - Last: {lastWallName}, Current: {wallCollider.gameObject.name}");
        }
        return true;
    }
    
    /// <summary>
    /// ✨ SIMPLIFIED WALL DETECTION - Uses CharacterController's built-in collision data!
    /// Replaces 8-directional raycasts with actual collision information from OnControllerColliderHit.
    /// This is more reliable, more performant, and actually reflects what the player is touching.
    /// 🎮 FORGIVENESS: Uses buffered contacts for consistent detection even during frame gaps.
    /// </summary>
    private bool DetectWall(out Vector3 wallNormal, out Vector3 hitPoint)
    {
        Collider detectedWallCollider = null;
        return DetectWall(out wallNormal, out hitPoint, out detectedWallCollider);
    }
    
    /// <summary>
    /// ✨ SIMPLIFIED WALL DETECTION - Uses CharacterController's built-in collision data!
    /// Returns TRUE if we're currently touching a wall (from actual collision, not raycasts).
    /// Overload with collider output for same-wall spam prevention.
    /// 🎮 FORGIVENESS: Checks buffered contacts within grace period for responsive wall jumps.
    /// </summary>
    private bool DetectWall(out Vector3 wallNormal, out Vector3 hitPoint, out Collider wallCollider)
    {
        // 🎮 FORGIVENESS SYSTEM: Check recent wall contacts within extended grace period
        // This makes wall jumps feel consistent even if collision detection has tiny gaps
        float extendedGracePeriod = WallJumpGracePeriod * 2f; // Double the grace period for detection
        
        // Find the MOST RECENT valid wall contact within grace period
        WallContact? bestContact = null;
        float newestTime = -999f;
        
        for (int i = 0; i < MAX_WALL_HISTORY; i++)
        {
            WallContact contact = recentWallContacts[i];
            if (contact.collider == null) continue; // Skip empty slots
            
            float age = Time.time - contact.time;
            if (age <= extendedGracePeriod && contact.time > newestTime)
            {
                bestContact = contact;
                newestTime = contact.time;
            }
        }
        
        // Use the best recent contact if found
        if (bestContact.HasValue)
        {
            wallNormal = bestContact.Value.normal;
            hitPoint = bestContact.Value.point;
            wallCollider = bestContact.Value.collider;
            
            if (ShowWallJumpDebug)
            {
                float age = Time.time - bestContact.Value.time;
                Debug.Log($"[WALL DETECT] ✅ Recent wall contact: {wallCollider.name} (age: {age:F3}s)");
                Debug.DrawRay(hitPoint, wallNormal * 50f, Color.green, 0.1f);
            }
            
            return true;
        }
        
        // No valid wall contacts within grace period
        wallNormal = Vector3.zero;
        hitPoint = Vector3.zero;
        wallCollider = null;
        return false;
    }
    
    /// <summary>
    /// Execute wall jump - AAA implementation with predictable, skill-based control
    /// Based on research from Mario 64, Celeste, and Titanfall movement systems
    /// 🔒 ENHANCED: Now tracks which wall was jumped from to prevent same-wall spam
    /// </summary>
    private void PerformWallJump(Vector3 wallNormal, Collider wallCollider = null)
    {
        // ============================================================
        // AAA WALL JUMP - CLEAN & SIMPLE
        // ============================================================
        // Direction priority: Camera > Wall Normal
        // Force: Constant up + Horizontal (camera or wall direction)
        // Momentum: Preserved by percentage, not added
        // ============================================================
        
        Vector3 playerUp = groundNormal;
        Vector3 awayFromWall = wallNormal.normalized;
        
        // Get camera direction (horizontal only)
        Vector3 cameraDirection = Vector3.zero;
        if (cameraTransform != null)
        {
            cameraDirection = cameraTransform.forward;
            cameraDirection.y = 0;
            if (cameraDirection.sqrMagnitude > 0.01f)
            {
                cameraDirection.Normalize();
            }
            else
            {
                cameraDirection = Vector3.zero;
            }
        }
        
        // ============================================================
        // DETERMINE HORIZONTAL DIRECTION
        // ============================================================
        // SMART DETECTION: Face-first vs. Normal wall jump
        Vector3 horizontalDirection;
        if (cameraDirection != Vector3.zero && WallJumpCameraDirectionBoost > 0)
        {
            // Check if player is looking AT the wall (face-first) or AWAY from it (normal)
            // Dot product: camera direction · wall normal
            // > 0 = looking at wall (face-first) → push BACKWARD
            // < 0 = looking away from wall (normal) → use camera direction
            float dotCameraToWall = Vector3.Dot(cameraDirection, awayFromWall);
            
            if (dotCameraToWall < -0.3f) // Looking AT the wall (face-first scenario)
            {
                // 🚀 FACE-FIRST MAGIC: Push BACKWARD from where you're looking!
                // You're looking at the wall → Get pushed away dramatically
                horizontalDirection = -cameraDirection;
                
                if (ShowWallJumpDebug)
                {
                    Debug.Log($"[JUMP] 🚀 FACE-FIRST detected! Dot: {dotCameraToWall:F2} - Reversing camera direction");
                }
            }
            else // Normal wall jump (looking away or parallel to wall)
            {
                // Normal wall jump: Use camera direction as-is
                horizontalDirection = cameraDirection;
                
                if (ShowWallJumpDebug)
                {
                    Debug.Log($"[JUMP] ➡️ NORMAL wall jump. Dot: {dotCameraToWall:F2} - Using camera direction");
                }
            }
        }
        else
        {
            // Fallback: Use wall normal (away from wall)
            horizontalDirection = awayFromWall;
            horizontalDirection.y = 0;
            horizontalDirection.Normalize();
        }
        
        // ============================================================
        // CALCULATE FORCES - CONSERVATION STYLE (ADDITIVE)
        // ============================================================
        
        // UPWARD FORCE: Constant (predictable arc)
        float upForce = WallJumpUpForce;
        
        // BASE BONUS: Flat velocity added for wall jump
        float baseBonus = WallJumpOutForce;
        
        // CAMERA BOOST: Extra force if using camera direction (additive)
        float cameraBonus = 0f;
        if (cameraDirection != Vector3.zero && WallJumpCameraDirectionBoost > 0)
        {
            cameraBonus = WallJumpCameraDirectionBoost;
        }
        
        // FALL ENERGY: Convert fall speed to horizontal boost (additive)
        float fallSpeed = Mathf.Abs(velocity.y);
        float fallEnergyBonus = fallSpeed * WallJumpFallSpeedBonus;
        
        // ============================================================
        // BUILD FINAL VELOCITY - CONSERVATION STYLE
        // ============================================================
        // Formula: Preserved Velocity + Skill Bonuses + Situational Bonuses
        
        // STEP 1: Preserve percentage of current horizontal momentum
        Vector3 currentHorizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
        Vector3 preservedVelocity = currentHorizontalVelocity * WallJumpMomentumPreservation;
        
        // STEP 2: Add wall jump bonuses (all additive!)
        Vector3 wallJumpBonus = horizontalDirection * (baseBonus + cameraBonus + fallEnergyBonus);
        
        // STEP 3: Combine preserved + bonuses
        Vector3 finalHorizontalVelocity = preservedVelocity + wallJumpBonus;
        
        // STEP 4: Set final velocity (horizontal + vertical)
        float speedBefore = currentHorizontalVelocity.magnitude;
        velocity = finalHorizontalVelocity + (playerUp * upForce);
        float speedAfter = velocity.magnitude;
        
        if (ShowWallJumpDebug)
        {
            Debug.Log($"[JUMP] === CONSERVATION WALL JUMP ===");
            Debug.Log($"[JUMP] Direction: {horizontalDirection}");
            Debug.Log($"[JUMP] Preserved Momentum: {preservedVelocity.magnitude:F1} ({WallJumpMomentumPreservation:F2}%)");
            Debug.Log($"[JUMP] Bonuses: Base={baseBonus:F1}, Camera={cameraBonus:F1}, Fall={fallEnergyBonus:F1}");
            Debug.Log($"[JUMP] Total Horizontal: {finalHorizontalVelocity.magnitude:F1}");
            Debug.Log($"[JUMP] Upward Force: {upForce:F1}");
            Debug.Log($"[JUMP] Final Velocity: {velocity.magnitude:F1}");
        }
        
        // MOMENTUM VISUALIZATION: Report speed gain
        if (MomentumVisualization.Instance != null)
        {
            MomentumVisualization.Instance.OnSpeedGain(speedBefore, speedAfter, transform.position);
        }
        
        // Protect wall jump velocity from air control interference
        wallJumpVelocityProtectionUntil = Time.time + WallJumpAirControlLockoutTime; // CRITICAL: Use property!
        lastWallNormal = wallNormal;
        IsGrounded = false;
        
        // 🔒 ANTI-EXPLOIT: Store which wall we just jumped from
        lastWallJumpedFrom = wallCollider;
        lastWallJumpedInstanceID = wallCollider != null ? wallCollider.GetInstanceID() : 0;
        
        if (ShowWallJumpDebug && wallCollider != null)
        {
            Debug.Log($"[JUMP] 🔒 Locked wall: {wallCollider.gameObject.name} (ID: {lastWallJumpedInstanceID})");
        }
        
        // Trigger camera tilt effect
        if (cameraController != null)
        {
            cameraController.TriggerWallJumpTilt(wallNormal);
        }
        
        // 🎯 WALL JUMP IMPACT VFX SYSTEM - Creates ripple effect on wall surface!
        if (EnableWallJumpVFX && WallJumpImpactVFX.Instance != null)
        {
            // Calculate player speed at impact for effect intensity
            float playerSpeed = velocity.magnitude;
            
            // Get precise impact point from last wall detection
            Vector3 impactPoint = LastWallHitPoint != Vector3.zero ? LastWallHitPoint : transform.position;
            
            // Configure VFX system with current config settings
            WallJumpImpactVFX.Instance.SetBaseDuration(VFXBaseDuration);
            WallJumpImpactVFX.Instance.SetEffectScale(VFXEffectScale);
            WallJumpImpactVFX.Instance.SetSpeedThresholds(VFXMinSpeedThreshold, VFXMaxIntensitySpeed);
            WallJumpImpactVFX.Instance.SetIntensityRange(VFXMinIntensity, VFXMaxIntensity);
            
            // Trigger the ripple effect with speed-based intensity
            WallJumpImpactVFX.Instance.TriggerImpactEffect(impactPoint, wallNormal, playerSpeed, wallCollider);
            
            if (ShowWallJumpDebug)
            {
                Debug.Log($"[JUMP] 🌊 Wall jump VFX triggered at {impactPoint} with speed {playerSpeed:F1}", this);
            }
        }
        
        // 🎯 WALL JUMP XP CHAIN SYSTEM - Uses existing FloatingTextManager!
        if (WallJumpXPSimple.Instance != null)
        {
            WallJumpXPSimple.Instance.OnWallJumpPerformed(transform.position);
        }
        
        // Old debug logs removed - see AAA debug output above
        
        // Reset double jump charges
        airJumpRemaining = MaxAirJumps;
        
        // Set falling state for landing impact detection
        isFalling = true;
        fallStartHeight = transform.position.y;
        
        // Play dedicated wall jump sound (with fallback to regular jump sound)
        GameSounds.PlayPlayerWallJump(transform.position, 1.0f);
        
        // Trigger jump animation for wall jump
        if (_animationStateManager != null)
        {
            _animationStateManager.SetMovementState((int)PlayerAnimationStateManager.PlayerAnimationState.Jump);
            Debug.Log("[JUMP] Wall jump animation triggered");
        }
        
        // Wall jump executed - see AAA debug output above
    }
    
    /// <summary>
    /// <summary>
    /// ✨ BUILT-IN COLLISION CALLBACK - Unity calls this automatically when CharacterController hits something!
    /// Handles: Wall jump detection, wall bounces, anti-exploit wall locking
    /// This replaces the complex 8-directional raycast system with actual collision data.
    /// </summary>
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Only process wall collisions (not ground or ceiling)
        if (hit == null || controller == null) return;
        
        // Don't bounce if moving strongly upward (jump pads, updraft zones)
        if (velocity.y > HIGH_UPWARD_VELOCITY_THRESHOLD)
        {
            if (ShowWallJumpDebug)
            {
                Debug.Log($"[COLLISION] Wall bounce skipped - Strong upward velocity ({velocity.y:F1})");
            }
            return;
        }
        
        // Check if this is a wall collision (mostly vertical surface)
        float angleFromUp = Vector3.Angle(hit.normal, Vector3.up);
        bool isWall = angleFromUp > 60f && angleFromUp < 120f;
        
        if (!isWall) return;
        
        // ✨ NEW: Store in circular buffer for consistency (remember multiple contacts)
        recentWallContacts[wallContactIndex] = new WallContact
        {
            normal = hit.normal,
            point = hit.point,
            collider = hit.collider,
            time = Time.time
        };
        wallContactIndex = (wallContactIndex + 1) % MAX_WALL_HISTORY;
        
        // Update current contact data
        lastWallCollisionNormal = hit.normal;
        lastWallCollisionPoint = hit.point;
        lastWallCollider = hit.collider;
        isCurrentlyTouchingWall = true;
        lastWallContactTime = Time.time;
        
        // Update LastWallHitPoint for camera system
        LastWallHitPoint = hit.point;
        
        if (ShowWallJumpDebug)
        {
            Debug.Log($"[WALL COLLISION] Hit wall: {hit.collider.name}, Normal: {hit.normal}, Angle: {angleFromUp:F1}°");
            Debug.DrawRay(hit.point, hit.normal * 50f, Color.cyan, 0.5f);
        }
        
        // 🔒 ANTI-EXPLOIT: Clear wall lock when touching ANY other object
        // This allows wall jumping again after touching a different surface
        if (hit.collider != null && lastWallJumpedFrom != null)
        {
            // Check if this collision is with a DIFFERENT object than the last wall jumped from
            if (hit.collider != lastWallJumpedFrom && hit.collider.GetInstanceID() != lastWallJumpedInstanceID)
            {
                // Touching a different object - clear the wall lock!
                if (ShowWallJumpDebug)
                {
                    Debug.Log($"[COLLISION] 🔒 Wall lock cleared - Touched different object: {hit.collider.gameObject.name}");
                }
                lastWallJumpedFrom = null;
                lastWallJumpedInstanceID = 0;
            }
        }
        
        // Apply bounce-back if airborne and moving toward wall
        if (!IsGrounded && !justPerformedWallJump)
        {
            // Check if we're moving toward the wall
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
            float dotToWall = Vector3.Dot(horizontalVelocity.normalized, -hit.normal);
            
            // Apply bounce if moving toward wall
            if (dotToWall > WALL_BOUNCE_DOT_THRESHOLD && horizontalVelocity.magnitude > MIN_HORIZONTAL_VELOCITY_FOR_BOUNCE)
            {
                // Apply gentle push away from wall
                Vector3 bounceVelocity = hit.normal * WALL_BOUNCE_FORCE;
                
                // Only apply horizontal bounce (don't affect vertical velocity/gravity)
                velocity.x += bounceVelocity.x;
                velocity.z += bounceVelocity.z;
                
                // Clamp to prevent excessive bounce
                Vector3 horizontalVel = new Vector3(velocity.x, 0, velocity.z);
                if (horizontalVel.magnitude > MAX_HORIZONTAL_BOUNCE_SPEED)
                {
                    horizontalVel = horizontalVel.normalized * MAX_HORIZONTAL_BOUNCE_SPEED;
                    velocity.x = horizontalVel.x;
                    velocity.z = horizontalVel.z;
                }
                
                if (ShowWallJumpDebug)
                {
                    Debug.Log($"[COLLISION] Wall bounce applied - New velocity: {velocity.magnitude:F1}");
                }
            }
        }
        
        // Timer-based flag clearing prevents multi-wall bounce issues
    }
    
    /// <summary>
    /// PHASE 1 UNIFICATION: Get cached ground distance from last check
    /// Returns -1 if not grounded or no valid data, 0 if grounded
    /// Eliminates duplicate raycasts in camera system
    /// </summary>
    public float GetLastGroundDistance() => lastGroundDistance;
}