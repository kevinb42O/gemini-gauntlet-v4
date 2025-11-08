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
    public float wallDetectionDistance = 400f; // INCREASED: 1.25× body length for more forgiving wall jump detection
    
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
    
    [Header("=== ⛰️ SLOPE & COLLISION ===")]
    [Tooltip("Force applied on steep slopes")]
    public float slopeForce = 14000f; // BALANCED: 2× gravity strength for reliable slope sliding
    
    [Tooltip("Maximum walkable slope angle")]
    public float maxSlopeAngle = 50f;
    
    [Tooltip("Minimum slope angle to apply slope physics - CRITICAL: Must be low (1-2°) to catch gentle slopes!")]
    public float minimumSlopeAngle = 1f; // FIXED: Was 8f - now catches ALL slopes for smooth walking
    
    [Header("=== SLOPE MOMENTUM SYSTEM ===")]
    [Tooltip("Enable natural slope physics during walking (gravity acceleration downhill, friction uphill)")]
    /// <summary>
    /// Enable/disable realistic slope physics. When enabled, slopes affect movement speed naturally:
    /// - Downhill: Slight speed boost from gravity (feels satisfying!)
    /// - Uphill: Movement slows down realistically (harder to climb)
    /// Disable this if you want arcade-style movement that ignores terrain angles.
    /// </summary>
    public bool enableSlopeMomentum = true;
    
    [Tooltip("Acceleration multiplier on downhill slopes (gravity component)")]
    /// <summary>
    /// How much faster you move when running downhill (gravity assist).
    /// - 0.4 = 40% speed boost on steep downhills (default, subtle but noticeable)
    /// - 0.0 = No downhill boost (flat speed everywhere)
    /// - 1.0 = 100% speed boost (very slippery, fast downhills)
    /// Higher values = more "gravity pull" feeling when going downhill.
    /// </summary>
    public float slopeAccelerationMultiplier = 0.4f; // Subtle but noticeable
    
    [Tooltip("Friction multiplier on uphill slopes (resists movement) - INCREASE to make slopes harder")]
    /// <summary>
    /// How much SLOWER you move when climbing uphill (friction/resistance).
    /// This is THE MAIN SETTING for slope difficulty!
    /// - 0.5 = Easy mode (barely notice uphill slopes)
    /// - 1.0 = Realistic (noticeable slowdown, still manageable)
    /// - 1.8 = Default (challenging but fair)
    /// - 3.0 = Hard mode (steep slopes are a real challenge!)
    /// - 5.0+ = Dark Souls mode (brutal uphill climbs)
    /// Higher values = uphill movement feels heavier and more exhausting.
    /// </summary>
    public float uphillFrictionMultiplier = 1.8f; // Harder to run uphill
    
    [Tooltip("Speed bonus percentage when jumping off downhill slopes (ramp jumps)")]
    /// <summary>
    /// Extra horizontal speed boost when jumping off downhill slopes (ramp jump mechanic).
    /// Creates satisfying "launch" feeling when jumping downhill at speed.
    /// - 0.0 = No ramp jump bonus (normal jumps everywhere)
    /// - 0.25 = 25% speed boost (default, rewarding but not overpowered)
    /// - 0.5 = 50% speed boost (big air, Tony Hawk vibes!)
    /// - 1.0 = 100% speed boost (extreme ramp jumps, doubles your speed!)
    /// Perfect for games with vertical terrain and skillful movement.
    /// </summary>
    [Range(0f, 1f)]
    public float rampJumpBonus = 0.25f; // 25% speed boost
    
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
    
    [Tooltip("Smooth stair climbing (gradual vs instant step-up)")]
    public bool smoothStepClimbing = true;
    
    [Tooltip("Step offset as percentage of player height (0.125 = 12.5% = 40 units for 320-height character)")]
    [Range(0.05f, 0.25f)]
    public float stepOffsetHeightPercent = 0.125f;
    
    [Tooltip("Descend force on slopes")]
    public float descendForce = 50000f;
    
    [Header("=== 🌍 GROUNDING ===")]
    [Tooltip("Ground check distance - CRITICAL: Must be large enough to detect slopes ahead!")]
    public float groundCheckDistance = 20f; // FIXED: Was 0.7f - too small for 320-unit character on slopes!
    
    [Tooltip("Grounded state hysteresis (smoothing) - CRITICAL: Must be low (0.02s) for smooth slope walking!")]
    public float groundedHysteresisSeconds = 0.02f; // FIXED: Was 0.1f - now 20ms for instant slope stick response
    
    [Tooltip("Grounded debounce frames - CRITICAL: Must be 1 for instant slope response!")]
    public int groundedDebounceFrames = 1; // FIXED: Was 2 - now instant confirmation
    
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
