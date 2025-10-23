using UnityEngine;

/// <summary>
/// ScriptableObject configuration for crouch/slide/dive system.
/// This replaces 80+ inspector settings with a clean, data-driven approach.
/// Create via: Assets > Create > Game > Crouch Configuration
/// </summary>
[CreateAssetMenu(fileName = "CrouchConfig", menuName = "Game/Crouch Configuration", order = 1)]
public class CrouchConfig : ScriptableObject
{
    [Header("=== 🎮 BASIC SETTINGS ===")]
    [Tooltip("Normal player height when standing")]
    public float standingHeight = 320f; // SCALED for 320-unit character (was 75)
    
    [Tooltip("Player height when crouched/sliding")]
    public float crouchedHeight = 140f; // SCALED for 320-unit character (was 32)
    
    [Tooltip("Camera height when standing")]
    public float standingCameraY = 300f; // SCALED for 320-unit character (was 75)
    
    [Tooltip("Camera height when crouched")]
    public float crouchedCameraY = 130f; // SCALED for 320-unit character (was 32)
    
    [Tooltip("How fast to crouch/stand (units per second)")]
    public float transitionSpeed = 1280f; // SCALED 4x for 320-unit character (was 400)
    
        [Header("=== 🛷 SLIDE PHYSICS ===")]
    [Tooltip("Minimum speed to start sliding (units/sec)")]
    public float slideMinStartSpeed = 105f; // SCALED 3x for 320-unit character (was 35)
    
    [Tooltip("Downhill gravity acceleration (natural = 980)")]
    public float slideGravityAccel = 3240f; // SCALED 3x for 320-unit character (was 1080)
    
    [Tooltip("Friction on flat ground (higher = faster deceleration)")]
    public float slideFrictionFlat = 6f; // NEXT-LEVEL: Speed-proportional physics (was 18f)
    
    [Tooltip("Friction on slopes (higher = more control)")]
    public float slideFrictionSlope = 8f; // NEXT-LEVEL: Balanced for flow state (was 6f)
    
    [Tooltip("Steering responsiveness while sliding")]
    public float slideSteerAcceleration = 1200f; // SCALED 3x for 320-unit character (was 400)
    
    [Tooltip("Maximum slide speed")]
    public float slideMaxSpeed = 5040f; // SCALED 3x for 320-unit character (was 1680)
    
    [Tooltip("Momentum preservation (DEPRECATED - Now using physics-based friction instead)")]
    [Range(0f, 1f)]
    public float momentumPreservation = 0.85f; // Legacy value - not used in new physics system
    
    [Tooltip("Ground adhesion force (prevents bouncing)")]
    public float stickToGroundVelocity = 66f; // SCALED 3x for 320-unit character (was 22)
    
    [Tooltip("Uphill resistance multiplier")]
    public float uphillFrictionMultiplier = 4f;
    
    [Header("=== 🎯 TACTICAL DIVE ===")]
    [Tooltip("Forward launch force")]
    public float diveForwardForce = 720f;
    
    [Tooltip("Upward arc force")]
    public float diveUpwardForce = 240f;
    
    [Tooltip("How long you stay prone after dive")]
    public float diveProneDuration = 0.8f;
    
    [Tooltip("Minimum sprint speed to dive")]
    public float diveMinSprintSpeed = 320f;
    
    [Tooltip("Dive slide friction")]
    public float diveSlideFriction = 2400f;
    
    [Header("=== 🎬 VISUAL EFFECTS ===")]
    [Tooltip("Enable camera FOV kick while sliding")]
    public bool slideFOVKick = true;
    
    [Tooltip("Extra FOV added while sliding")]
    public float slideFOVAdd = 15f;
    
    [Tooltip("FOV transition speed")]
    public float slideFOVLerpSpeed = 20f;
    
    [Header("=== 🎛️ CURVE TUNING ===")]
    [Tooltip("Steering acceleration by speed (x=speed, y=multiplier)")]
    public AnimationCurve steerAccelBySpeedCurve = AnimationCurve.Linear(0f, 1f, 200f, 1.3f);
    
    [Tooltip("Friction by speed (x=speed, y=multiplier)")]
    public AnimationCurve frictionBySpeedCurve = AnimationCurve.Linear(0f, 1f, 200f, 0.6f);
    
    [Tooltip("Drift feel when steering (0=instant turn, 1=drifty)")]
    [Range(0f, 1f)]
    public float steerDriftLerp = 0.85f;
    
    [Header("=== 🔧 ADVANCED PHYSICS (Optional) ===")]
    [Tooltip("Auto-stand when speed drops below this")]
    public float slideAutoStandSpeed = 25f;
    
    [Tooltip("Minimum time before auto-stand kicks in")]
    public float slideMinimumDuration = 0.3f;
    
    [Tooltip("Coyote time while sliding (stay grounded after losing contact)")]
    public float slideGroundCoyoteTime = 0.225f; // UNIFIED: Matches AAAMovementController.coyoteTime
    
    [Tooltip("Downward acceleration while sliding (negative value)")]
    public float slidingGravity = -2250f; // SCALED 3x to match AAA gravity ratio (was -750)
    
    [Tooltip("Terminal downward velocity")]
    public float slidingTerminalDownVelocity = 540f; // SCALED 3x for consistency (was 180)
    
    [Tooltip("Minimum downward Y velocity while sliding on slopes")]
    public float minDownYWhileSliding = 25f;
    
    [Tooltip("Ground check distance (2x character height for 320-unit player = 640)")]
    public float slideGroundCheckDistance = 640f; // SCALED for 320-unit character (was 200)
    
    [Tooltip("Minimum slope angle to auto-slide on landing (degrees)")]
    public float landingSlopeAngleForAutoSlide = 12f;
    
    [Header("=== 🎯 LANDING MOMENTUM CONTROL ===")]
    [Tooltip("Speed multiplier when landing with momentum (0.5 = half speed, 1.0 = full speed)")]
    [Range(0.3f, 1.0f)]
    public float landingMomentumDamping = 0.65f; // 65% of landing speed preserved
    
    [Tooltip("Enable speed cap (prevents extreme speeds, but may break momentum chains if too low)")]
    public bool enableLandingSpeedCap = false; // DISABLED by default - let momentum flow!
    
    [Tooltip("Maximum preserved speed on landing (only if cap enabled - set VERY HIGH to avoid breaking chains)")]
    public float landingMaxPreservedSpeed = 2000f; // Very high - only catches extreme edge cases
    
    [Tooltip("Ramp launch minimum speed")]
    public float rampMinSpeed = 140f;
    
    [Tooltip("Extra jump boost from ramps")]
    public float rampExtraUpBoost = 0.15f;
    
    [Header("=== 🎯 STEP OFFSET CONTROL ===")]
    [Tooltip("Step offset during slide (0=catches on bumps, 15=smooth, 45=too forgiving)")]
    public float slideStepOffsetOverride = 15f; // FIXED: Allow small steps while sliding (was 0)
    
    [Tooltip("Enable step offset reduction during slide")]
    public bool reduceStepOffsetDuringSlide = true;
    
    [Header("=== 🧠 BEHAVIOR TOGGLES ===")]
    [Tooltip("Hold crouch key to crouch, or tap to toggle?")]
    public bool holdToCrouch = true;
    
    [Tooltip("Hold crouch key to slide, or tap to slide until stopped? (Recommended: true for manual control)")]
    public bool holdToSlide = true;
    
    [Tooltip("Enable sliding system")]
    public bool enableSlide = true;
    
    [Tooltip("Enable tactical dive system")]
    public bool enableTacticalDive = true;
    
    [Tooltip("Enable ramp launches")]
    public bool rampLaunchEnabled = true;
    
    [Tooltip("Auto-slide on landing when crouched on slopes")]
    public bool autoSlideOnLandingWhileCrouched = true;
    
    [Tooltip("Auto-resume slide from momentum on landing")]
    public bool autoResumeSlideFromMomentum = true;
    
    [Tooltip("Enable slide audio feedback")]
    public bool slideAudioEnabled = true;
    
    [Tooltip("Enable dive audio feedback")]
    public bool diveAudioEnabled = true;
    
    [Tooltip("Enable slide particle effects")]
    public bool slideParticlesEnabled = true;
    
    [Header("=== 🔍 DEBUG OPTIONS ===")]
    [Tooltip("Show debug arrows in scene view")]
    public bool showDebugVisualization = false;
    
    [Tooltip("Verbose debug logging")]
    public bool verboseDebugLogging = false;
    
    [Header("=== 🎯 SMOOTH WALL SLIDING (ENHANCEMENT) ===")]
    [Tooltip("Enable smooth wall sliding during slide (collide-and-slide algorithm). PURE ENHANCEMENT - no breaking changes.")]
    public bool enableSmoothWallSliding = true;
    
    [Tooltip("Maximum iterations for multi-surface collision resolution per frame.")]
    public int wallSlideMaxIterations = 3;
    
    [Tooltip("Speed preservation when sliding along walls (0-1). Higher = more momentum.")]
    [Range(0f, 1f)]
    public float wallSlideSpeedPreservation = 0.95f;
    
    [Tooltip("Minimum wall angle (degrees from vertical) to trigger wall sliding. Lower = more surfaces.")]
    public float wallSlideMinAngle = 45f;
    
    [Tooltip("Skin width multiplier for wall detection (prevents getting stuck in geometry).")]
    [Range(0.8f, 0.99f)]
    public float wallSlideSkinMultiplier = 0.95f;
    
    [Tooltip("Show debug visualization for wall slide raycasts.")]
    public bool showWallSlideDebug = false;
    
    // ========== INTERNAL CONSTANTS (Not exposed) ==========
    // These are optimal values that rarely need changing
    
    [HideInInspector] public float slideHardCapSeconds = 3.5f;
    [HideInInspector] public float slideBaseDuration = 1.2f;
    [HideInInspector] public float slideMaxExtraDuration = 1.0f;
    [HideInInspector] public float slideSpeedForMaxExtra = 120f;
    [HideInInspector] public float slopeBoostAngleThreshold = 15f;
    [HideInInspector] public float uphillReversalSpeed = 12f;
    [HideInInspector] public float uphillMinTime = 0.2f;
    [HideInInspector] public float uphillReversalBoost = 25f;
    [HideInInspector] public float slopeTransitionGrace = 0.35f;
    [HideInInspector] public float radiusSkin = 0.02f;
    [HideInInspector] public float groundNormalSmoothing = 15f;
    [HideInInspector] public float landingMomentumResumeWindow = 1.2f;
    [HideInInspector] public float slideLandingBuffer = 0.6f; // EXTENDED: 0.6s buffer for long jumps (was 0.3s)
    [HideInInspector] public float rampMinUpY = 8f;
    [HideInInspector] public float rampNormalDeltaDeg = 12f;
    [HideInInspector] public float rampDownhillMemory = 0.3f;
    
    /// <summary>
    /// Validates configuration values to prevent errors
    /// </summary>
#if UNITY_EDITOR
    private void OnValidate()
    {
        // PERFORMANCE FIX: Skip validation during play mode to prevent scene update freezes
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        // Ensure logical relationships
        crouchedHeight = Mathf.Min(crouchedHeight, standingHeight - 0.05f);
        crouchedCameraY = Mathf.Min(crouchedCameraY, standingCameraY - 0.01f);
        
        // Ensure positive values where needed
        standingHeight = Mathf.Max(0.1f, standingHeight);
        crouchedHeight = Mathf.Max(0.1f, crouchedHeight);
        transitionSpeed = Mathf.Max(1f, transitionSpeed);
        slideMinStartSpeed = Mathf.Max(0f, slideMinStartSpeed);
        slideMaxSpeed = Mathf.Max(slideMinStartSpeed + 10f, slideMaxSpeed);
        
        // Clamp ranges
        momentumPreservation = Mathf.Clamp01(momentumPreservation);
        steerDriftLerp = Mathf.Clamp01(steerDriftLerp);
    }
#endif
    
    /// <summary>
    /// Creates default configuration with optimal values
    /// </summary>
    public static CrouchConfig CreateDefault()
    {
        var config = CreateInstance<CrouchConfig>();
        
        // Set default curves
        config.steerAccelBySpeedCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(100f, 1f),
            new Keyframe(200f, 1.3f)
        );
        
        config.frictionBySpeedCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(50f, 1f),
            new Keyframe(200f, 0.6f)
        );
        
        return config;
    }
}
