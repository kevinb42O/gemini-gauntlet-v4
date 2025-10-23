using UnityEngine;

/// <summary>
/// ScriptableObject configuration for AAAMovementController.
/// This replaces 60+ inspector settings with a clean, data-driven approach.
/// Create via: Assets > Create > Game > Movement Configuration
/// </summary>
[CreateAssetMenu(fileName = "MovementConfig", menuName = "Game/Movement Configuration", order = 2)]
public class MovementConfig : ScriptableObject
{
    [Header("=== ⚡ CORE PHYSICS ===")]
    [Tooltip("Gravity force (negative = down, natural = -980) - SCALED: Snappier jumps at 320-unit scale")]
    public float gravity = -7000f; // BALANCED: Strong gravity for responsive, grounded feel (0.24s to apex)
    
    [Tooltip("Maximum fall speed - SCALED: Realistic terminal velocity (3× sprint speed)")]
    public float terminalVelocity = 6000f; // BALANCED: Achievable terminal velocity, proportional to movement
    
    [Tooltip("Jump force (higher = jump higher) - SCALED: ~0.65× character height jump")]
    public float jumpForce = 1700f; // BALANCED: Tight 206-unit jump (0.65× height), 0.49s total air time
    
    [Tooltip("Double jump force (if enabled) - SCALED: Noticeable second jump")]
    public float doubleJumpForce = 1150f; // BALANCED: 68% of main jump power, 135-unit boost
    
    [Header("=== 🏃 MOVEMENT ===")]
    [Tooltip("Base movement speed")]
    public float moveSpeed = 1300f; // BALANCED: Responsive base speed, covers body length in 0.25s
    
    [Tooltip("Sprint speed multiplier")]
    public float sprintMultiplier = 1.55f; // BALANCED: 2015 sprint speed, matches movement-to-jump ratio
    
    [Tooltip("Maximum speed while in air")]
    public float maxAirSpeed = 15000f; // BALANCED: High enough for wall jump chains and momentum tricks
    
    [Header("=== ✈️ AIR CONTROL ===")]
    [Tooltip("Air control strength (0-1)")]
    [Range(0f, 1f)]
    public float airControlStrength = 0.25f;
    
    [Tooltip("Air acceleration force")]
    public float airAcceleration = 2000f; // BALANCED: Stronger air control for responsive mid-air adjustments
    
    [Tooltip("Speed threshold for high-speed air physics (UNIFIED with CleanAAACrouch.HIGH_SPEED_LANDING_THRESHOLD)")]
    public float highSpeedThreshold = 1300f; // BALANCED: 65% of sprint speed (1300÷2015) for high-speed detection
    
    [Header("=== 🦘 JUMP MECHANICS ===")]
    [Tooltip("Coyote time - grace period after leaving ground (UNIFIED with CleanAAACrouch.slideGroundCoyoteTime)")]
    public float coyoteTime = 0.225f; // BBFL: Matches slide coyote time for perfect sync
    
    [Tooltip("Number of air jumps allowed (0 = disabled)")]
    public int maxAirJumps = 1;
    
    [Tooltip("Jump cut multiplier when releasing jump key early")]
    [Range(0f, 1f)]
    public float jumpCutMultiplier = 0.5f;
    
    [Header("=== 🧗 WALL JUMP SYSTEM ===")]
    [Tooltip("Enable/disable wall jumping entirely")]
    public bool enableWallJump = true;
    
    [Header("Core Forces - MOMENTUM BASED")]
    [Tooltip("Upward force when wall jumping (reduced - rely on momentum scaling)")]
    public float wallJumpUpForce = 1300f; // BALANCED: 76% of main jump, responsive wall climb chains
    
    [Tooltip("Outward push from wall (minimal base - momentum does the work)")]
    public float wallJumpOutForce = 1800f; // BALANCED: Strong launch away from wall, feels punchy
    
    [Header("Momentum Scaling - THE SECRET SAUCE")]
    [Tooltip("Forward boost in current movement direction (subtle speed gain)")]
    public float wallJumpForwardBoost = 0f; // REMOVED: Momentum preservation handles this
    
    [Tooltip("Camera direction boost - WHERE YOU LOOK = WHERE YOU GO (MASSIVE)")]
    public float wallJumpCameraDirectionBoost = 900f; // BALANCED: Strong camera influence for intuitive control
    
    [Tooltip("Require input for camera boost? FALSE = camera always controls (recommended)")]
    public bool wallJumpCameraBoostRequiresInput = false;
    
    [Tooltip("Fall speed → horizontal speed conversion (CRITICAL - your momentum multiplier!)")]
    [Range(0f, 1f)]
    public float wallJumpFallSpeedBonus = 1f; // MAX: 100% fall energy = horizontal speed! (Titanfall physics)
    
    [Tooltip("How much WASD input affects jump direction (full control for precision)")]
    [Range(0f, 1f)]
    public float wallJumpInputInfluence = 1f; // MAX: Full directional control
    
    [Tooltip("Speed multiplier when pushing in jump direction (reward player intent)")]
    public float wallJumpInputBoostMultiplier = 1.5f; // INCREASED: Bigger reward for good input
    
    [Tooltip("Input threshold for boost (very forgiving for flow state)")]
    [Range(0f, 1f)]
    public float wallJumpInputBoostThreshold = 0.15f; // MORE FORGIVING: Easier to maintain momentum
    
    [Tooltip("Preserve previous velocity? MOMENTUM SCALING KEY (35% = natural feel)")]
    [Range(0f, 1f)]
    public float wallJumpMomentumPreservation = 0.35f; // CRITICAL: Keep 35% horizontal velocity = momentum chains!
    
    [Header("Detection & Timing")]
    [Tooltip("Wall detection distance (how far to scan for walls)")]
    public float wallDetectionDistance = 250f; // BALANCED: 78% of body length, tight but forgiving detection
    
    [Tooltip("Cooldown between wall jumps (0.12s = ultra-responsive)")]
    public float wallJumpCooldown = 0.12f;
    
    [Tooltip("Grace period after leaving wall (prevents instant re-detection)")]
    public float wallJumpGracePeriod = 0.08f;
    
    [Tooltip("Maximum consecutive wall jumps before touching ground (99 = unlimited)")]
    public int maxConsecutiveWallJumps = 99;
    
    [Tooltip("Minimum falling speed required to wall jump (-Y velocity)")]
    public float minFallSpeedForWallJump = 0.01f;
    
    [Tooltip("Air control lockout after wall jump (0 = full control, 0.25 = quarter second lock)")]
    public float wallJumpAirControlLockoutTime = 0f;
    
    [Header("Debug")]
    [Tooltip("Show wall detection rays and jump info in Scene view")]
    public bool showWallJumpDebug = false;
    
    [Header("=== 🌊 WALL JUMP IMPACT VFX ===")]
    [Tooltip("Enable wall jump impact ripple effects")]
    public bool enableWallJumpVFX = true;
    
    [Tooltip("Base effect duration in seconds")]
    [Range(0.5f, 5f)]
    public float vfxBaseDuration = 2f;
    
    [Tooltip("Effect scale multiplier (adjusts ripple size)")]
    [Range(0.1f, 3f)]
    public float vfxEffectScale = 1f;
    
    [Tooltip("Minimum speed for effect (below this = no effect)")]
    [Range(100f, 1000f)]
    public float vfxMinSpeedThreshold = 300f;
    
    [Tooltip("Speed at which effect reaches maximum intensity")]
    [Range(500f, 3000f)]
    public float vfxMaxIntensitySpeed = 1500f;
    
    [Tooltip("Minimum effect intensity (at threshold speed)")]
    [Range(0f, 1f)]
    public float vfxMinIntensity = 0.3f;
    
    [Tooltip("Maximum effect intensity (at max speed)")]
    [Range(0f, 3f)]
    public float vfxMaxIntensity = 1.5f;
    
    [Header("=== ⛰️ SLOPE & COLLISION ===")]
    [Tooltip("Force applied on steep slopes")]
    public float slopeForce = 14000f; // BALANCED: 2× gravity strength for reliable slope sliding
    
    [Tooltip("Maximum walkable slope angle")]
    public float maxSlopeAngle = 50f;
    
    [Tooltip("Maximum step height for stairs")]
    public float maxStepHeight = 40f;
    
    [Tooltip("Player character height")]
    public float playerHeight = 320f;
    
    [Tooltip("Player character radius")]
    public float playerRadius = 50f;
    
    [Tooltip("Stair detection distance")]
    public float stairCheckDistance = 150f;
    
    [Tooltip("Speed multiplier when climbing stairs")]
    [Range(0f, 1f)]
    public float stairClimbSpeedMultiplier = 1f;
    
    [Tooltip("Descend force on slopes")]
    public float descendForce = 50000f;
    
    [Header("=== 🌍 GROUNDING ===")]
    [Tooltip("Ground check distance - CRITICAL: Must be large enough to detect slopes ahead!")]
    public float groundCheckDistance = 20f; // FIXED: Was 0.7f - too small for 320-unit character on slopes!
    
    [Tooltip("Grounded state hysteresis (smoothing)")]
    public float groundedHysteresisSeconds = 0f;
    
    [Tooltip("Jump ground suppression time")]
    public float jumpGroundSuppressSeconds = 0f;
    
    [Tooltip("Enable anti-sink prediction")]
    public bool enableAntiSinkPrediction = false;
    
    [Tooltip("Ground prediction distance")]
    public float groundPredictionDistance = 400f;
    
    [Tooltip("Ground clearance")]
    public float groundClearance = 0.5f;
    
    [Header("=== 🎭 LAYER MASKS ===")]
    [Tooltip("Ground layer mask")]
    public LayerMask groundMask = ~0;
    
    [Header("=== 🪢 ROPE SWING SYSTEM (AAA+ Physics) ===")]
    [Tooltip("Enable rope swing mechanic")]
    public bool enableRopeSwing = true;
    
    [Header("Rope Targeting")]
    [Tooltip("Maximum rope attachment distance - SCALED for 320-unit character")]
    public float maxRopeDistance = 10000f; // Increased for giant world
    
    [Tooltip("Minimum rope attachment distance - SCALED for 320-unit character")]
    public float minRopeDistance = 500f; // Increased to prevent weird physics at large scale
    
    [Tooltip("Aim assist radius for rope targeting - SCALED for 320-unit character")]
    public float aimAssistRadius = 300f; // Larger assist for high-speed gameplay
    
    [Header("Physics - Verlet Integration (SCALED for 320-unit character)")]
    [Tooltip("Rope stiffness - higher = more rigid rope (0.8-1.0 recommended)")]
    [Range(0.5f, 1f)]
    public float ropeStiffness = 0.97f; // Slightly higher for large scale to prevent excessive stretch
    
    [Tooltip("Rope elasticity - allows slight stretch under high tension")]
    [Range(0f, 0.2f)]
    public float ropeElasticity = 0.03f; // Reduced for large scale (3% vs 5%)
    
    [Tooltip("Gravity multiplier while swinging (>1 = faster, <1 = floaty)")]
    public float swingGravityMultiplier = 1.0f; // CRITICAL: Reduced to 1.0 - gravity is already very strong (-7000)!
    
    [Tooltip("Air drag coefficient (realistic rope air resistance)")]
    [Range(0f, 0.5f)]
    public float swingAirDrag = 0.01f; // REDUCED: Less drag for high-speed movement
    
    [Header("Player Control (SCALED for high-speed movement)")]
    [Tooltip("Air control strength while swinging (0-1)")]
    [Range(0f, 1f)]
    public float swingAirControl = 0.10f; // REDUCED: Less air control for momentum-based swinging at high speeds
    
    [Tooltip("Allow pumping the swing by pressing forward at bottom of arc")]
    public bool enableSwingPumping = true;
    
    [Tooltip("Pumping force multiplier - SCALED for 320-unit character")]
    public float pumpingForce = 2000f; // INCREASED: 2.5x for high-speed gameplay
    
    [Tooltip("Allow boost pumping (press shift at bottom for speed burst)")]
    public bool enableBoostPumping = true;
    
    [Tooltip("Boost pump multiplier (stronger than normal pump)")]
    public float boostPumpMultiplier = 1.5f;
    
    [Header("Momentum & Release (Tuned for high-speed gameplay)")]
    [Tooltip("Momentum preservation on release (1.0 = perfect conservation)")]
    [Range(0.8f, 1.2f)]
    public float ropeMomentumMultiplier = 1.05f; // REDUCED: Less bonus - speeds are already very high!
    
    [Tooltip("Release timing bonus window (seconds from bottom of swing)")]
    public float releaseTimingWindow = 0.25f; // TIGHTER: Slightly shorter window for high-speed gameplay
    
    [Tooltip("Perfect release bonus multiplier")]
    [Range(1f, 2f)]
    public float perfectReleaseBonus = 1.2f; // REDUCED: Smaller bonus to prevent excessive speed stacking
    
    [Header("Integration")]
    [Tooltip("Rope-to-wall-jump velocity bonus")]
    [Range(0f, 2f)]
    public float ropeWallJumpBonus = 1.5f;
    
    [Tooltip("Can aerial trick while swinging")]
    public bool allowAerialTricksOnRope = true;
    
    [Tooltip("Trick jump force multiplier from rope")]
    [Range(1f, 2f)]
    public float ropeTrickBonus = 1.2f;
    
    [Header("=== 🔗 INTEGRATION FLAGS ===")]
    [Tooltip("Allow internal mode toggle")]
    public bool allowInternalModeToggle = false;
    
    [Tooltip("Prefer integrator for mode toggle")]
    public bool preferIntegratorForModeToggle = false;
    
    [Tooltip("Enable celestial flight integration")]
    public bool enableCelestialFlightIntegration = false;
    
    [Tooltip("Play land animation on grounded here")]
    public bool playLandAnimationOnGroundedHere = false;
    
    /// <summary>
    /// Validates configuration values to prevent errors
    /// </summary>
#if UNITY_EDITOR
    private void OnValidate()
    {
        // PERFORMANCE FIX: Skip validation during play mode to prevent scene update freezes
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        // Ensure positive values where needed
        terminalVelocity = Mathf.Max(1f, terminalVelocity);
        jumpForce = Mathf.Max(0f, jumpForce);
        moveSpeed = Mathf.Max(0.1f, moveSpeed);
        sprintMultiplier = Mathf.Max(1f, sprintMultiplier);
        maxAirSpeed = Mathf.Max(moveSpeed, maxAirSpeed);
        
        // Clamp ranges
        airControlStrength = Mathf.Clamp01(airControlStrength);
        jumpCutMultiplier = Mathf.Clamp01(jumpCutMultiplier);
        wallJumpMomentumPreservation = Mathf.Clamp01(wallJumpMomentumPreservation);
        stairClimbSpeedMultiplier = Mathf.Clamp01(stairClimbSpeedMultiplier);
        
        // Ensure logical relationships
        maxAirJumps = Mathf.Max(0, maxAirJumps);
        maxConsecutiveWallJumps = Mathf.Max(1, maxConsecutiveWallJumps);
        playerHeight = Mathf.Max(0.1f, playerHeight);
        playerRadius = Mathf.Max(0.1f, playerRadius);
    }
#endif
    
    /// <summary>
    /// Creates default configuration with optimal values for 320-unit character
    /// </summary>
    public static MovementConfig CreateDefault()
    {
        var config = CreateInstance<MovementConfig>();
        
        // Already has correct defaults from field initializers
        
        return config;
    }
}
