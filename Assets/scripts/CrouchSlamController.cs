using UnityEngine;
using GeminiGauntlet.Audio;

/// <summary>
/// CROUCH SLAM - Momentum-Building Air Maneuver
/// 
/// MECHANIC: Press crouch while airborne to slam down at high speed.
/// On landing, horizontal momentum is MASSIVELY BOOSTED and carries into slide.
/// 
/// INTEGRATION: 100% modular - uses existing velocity APIs, no AAAMovementController edits.
/// - SetExternalVelocity() for slam force
/// - CleanAAACrouch.TryStartSlide() for grounded transition
/// - Respects all existing systems (wall jump, dive, slide ownership)
/// 
/// ZERO BLOAT GUARANTEE:
/// - No duplicate logic
/// - No conflicting state machines
/// - No redundant velocity systems
/// - Clean API usage only
/// </summary>
[RequireComponent(typeof(AAAMovementController))]
[RequireComponent(typeof(CleanAAACrouch))]
[DefaultExecutionOrder(-400)] // CRITICAL: Run BEFORE CleanAAACrouch (-300) to queue momentum before slide checks
public class CrouchSlamController : MonoBehaviour
{
    [Header("=== CROUCH SLAM CONFIGURATION ===")]
    [Tooltip("Downward force applied when initiating slam (units/sec)")]
    [SerializeField] private float slamDownwardForce = 5000f; // 320-unit scale
    
    [Tooltip("Horizontal speed multiplier on landing - HIGHER = FASTER CHAINS!")]
    [Range(1.5f, 10.0f)]
    [SerializeField] private float landingSpeedMultiplier = 3.5f; // Aggressive boost for speed building!
    
    [Tooltip("Minimum speed granted on slam landing (prevents weak slams from standstill)")]
    [SerializeField] private float minimumLandingSpeed = 2000f; // Guaranteed base speed boost
    
    [Tooltip("Maximum speed before slam boost is disabled (prevents infinite acceleration)")]
    [SerializeField] private float maxSpeedForBoost = 25000f; // No boost if already going this fast!
    
    [Tooltip("Minimum airborne speed before slam is allowed (prevents spam on small jumps)")]
    [SerializeField] private float minFallSpeedForSlam = 500f;
    
    [Tooltip("Slam cooldown to prevent spam (seconds)")]
    [SerializeField] private float slamCooldown = 0.5f;
    
    [Tooltip("Visual/audio feedback on slam activation")]
    [SerializeField] private bool enableFeedback = true;
    
    [Tooltip("Debug logging for development")]
    [SerializeField] private bool showDebugLogs = true;
    
    // Cached references (no GetComponent spam)
    private AAAMovementController movement;
    private CleanAAACrouch crouchController;
    private CharacterController controller;
    
    // Runtime state (minimal footprint)
    private bool isSlamming = false;
    private float lastSlamTime = -999f;
    private Vector3 velocityAtSlamStart = Vector3.zero;
    
    // Landing detection (single-frame precision)
    private bool wasAirborneLastFrame = false;
    
    void Awake()
    {
        // Cache references ONCE
        movement = GetComponent<AAAMovementController>();
        crouchController = GetComponent<CleanAAACrouch>();
        controller = GetComponent<CharacterController>();
        
        // Validate dependencies
        if (movement == null)
        {
            Debug.LogError("[CROUCH SLAM] AAAMovementController not found! Disabling.");
            enabled = false;
            return;
        }
        
        if (crouchController == null)
        {
            Debug.LogError("[CROUCH SLAM] CleanAAACrouch not found! Disabling.");
            enabled = false;
            return;
        }
    }
    
    void Update()
    {
        // === PHASE 1: SLAM ACTIVATION CHECK ===
        // Only activate in air, when falling with sufficient speed
        bool isAirborne = !movement.IsGroundedRaw; // Instant truth - no coyote time for slam
        bool isFalling = movement.Velocity.y < -minFallSpeedForSlam;
        bool canSlam = !isSlamming && (Time.time - lastSlamTime) >= slamCooldown;
        
        // Check for crouch input while airborne
        if (isAirborne && isFalling && canSlam && Input.GetKeyDown(Controls.Crouch))
        {
            // EDGE CASE: Don't slam if already diving or sliding
            if (crouchController.IsDiving || crouchController.IsDiveProne || crouchController.IsSliding)
            {
                return;
            }
            
            StartSlam();
        }
        
        // === PHASE 2: LANDING DETECTION ===
        // Detect transition from air → ground (single frame precision)
        bool justLanded = wasAirborneLastFrame && movement.IsGroundedRaw;
        
        if (isSlamming && justLanded)
        {
            OnSlamLanding();
        }
        
        // === PHASE 3: SLAM ABORT CHECK ===
        // REMOVED: The abort-on-release logic was causing bugs!
        // Players would release crouch to prepare for the next slam input, causing premature cancellation.
        // Slam now continues until landing regardless of crouch hold state.
        
        // Update state tracker
        wasAirborneLastFrame = isAirborne;
    }
    
    /// <summary>
    /// PHASE 1: Initiate slam - apply massive downward force
    /// Uses SetExternalVelocity() for clean integration with existing velocity system
    /// </summary>
    private void StartSlam()
    {
        // Capture current velocity for landing calculations
        velocityAtSlamStart = movement.Velocity;
        
        // Calculate slam force vector
        // Preserve horizontal momentum, add massive downward force
        Vector3 slamVelocity = new Vector3(
            velocityAtSlamStart.x, // Keep horizontal X
            -slamDownwardForce,    // SLAM DOWN!
            velocityAtSlamStart.z  // Keep horizontal Z
        );
        
        // Apply slam force via existing velocity API (no conflicts!)
        // Duration = 0.5s (long enough to feel impactful, short enough to not override landing)
        movement.SetExternalVelocity(slamVelocity, 0.5f, overrideGravity: true);
        
        isSlamming = true;
        lastSlamTime = Time.time;
        
        // Feedback
        if (enableFeedback)
        {
            // Screen shake would go here
            // VFX trail would spawn here
            // Audio would play here (whoosh sound)
            GameSounds.PlayPlayerJump(transform.position); // Placeholder - replace with slam audio
        }
    }
    
    /// <summary>
    /// PHASE 2: Landing impact - boost horizontal momentum and transition to slide
    /// This is where the MAGIC happens - speed EXPLODES on landing!
    /// SLOPE-AWARE: Aligns velocity to slope surface for natural physics
    /// </summary>
    private void OnSlamLanding()
    {
        // ═══════════════════════════════════════════════════════════════════
        // 🎯 SLOPE-AWARE LANDING - CRITICAL FOR PHYSICS INTEGRITY
        // ═══════════════════════════════════════════════════════════════════
        
        // Detect ground surface normal at landing point
        // CRITICAL FIX: Use larger raycast distance and start from character center
        CharacterController cc = controller;
        float raycastStart = cc != null ? cc.height * 0.5f : 5f;
        float raycastDistance = cc != null ? cc.height + 20f : 30f; // Much longer!
        
        Vector3 rayOrigin = transform.position + Vector3.up * raycastStart;
        bool hasGround = Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit groundHit, raycastDistance);
        
        Vector3 groundNormal = hasGround ? groundHit.normal : Vector3.up;
        float slopeAngle = hasGround ? Vector3.Angle(Vector3.up, groundNormal) : 0f;
        
        // ═══════════════════════════════════════════════════════════════════
        // 🎯 CAMERA-BASED DIRECTION - SLAM GOES WHERE YOU LOOK!
        // ═══════════════════════════════════════════════════════════════════
        // Get camera forward direction (horizontal plane only)
        Camera mainCamera = Camera.main;
        Vector3 cameraForward = mainCamera != null ? mainCamera.transform.forward : transform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();
        
        // Get player input to determine exact direction
        float inputX = Controls.HorizontalRaw();
        float inputY = Controls.VerticalRaw();
        
        Vector3 desiredDirection;
        if (Mathf.Abs(inputX) > 0.01f || Mathf.Abs(inputY) > 0.01f)
        {
            // Player is holding input - use input direction relative to camera
            Vector3 cameraRight = mainCamera != null ? mainCamera.transform.right : transform.right;
            cameraRight.y = 0f;
            cameraRight.Normalize();
            
            desiredDirection = (cameraForward * inputY + cameraRight * inputX).normalized;
        }
        else
        {
            // No input - use camera forward direction
            desiredDirection = cameraForward;
        }
        
        // Get current horizontal velocity for speed calculation only
        Vector3 currentVelocity = movement.Velocity;
        Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
        float currentSpeed = horizontalVelocity.magnitude;
        
        if (showDebugLogs)
        {
            Debug.Log($"[CROUCH SLAM] Landing - Initial speed: {currentSpeed:F0} u/s, Cap: {maxSpeedForBoost:F0}");
        }
        
        // 🚀 Convert fall speed on downslopes FIRST (before cap check)
        // Your 1990 fall speed should become downhill speed, not be wasted
        bool isDownslope = (slopeAngle > 5f) && (Vector3.Dot(desiredDirection, Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized) > 0.3f);
        
        if (isDownslope)
        {
            // Convert vertical fall energy into slope-aligned speed
            float fallSpeed = Mathf.Abs(currentVelocity.y);
            float slopeFactor = Mathf.Sin(slopeAngle * Mathf.Deg2Rad); // Steeper = more conversion
            float convertedSpeed = fallSpeed * slopeFactor; // Full energy conversion (no efficiency loss)
            
            currentSpeed += convertedSpeed; // Add converted fall speed to horizontal speed
        }
        
        // BOOST CALCULATION: Use current speed OR minimum, whichever is higher
        float boostedSpeed = Mathf.Max(currentSpeed * landingSpeedMultiplier, minimumLandingSpeed);
        
        // ═══════════════════════════════════════════════════════════════════
        // 🚫 SPEED CAP - CLAMP BOOSTED SPEED TO MAXIMUM!
        // This prevents infinite acceleration by hard-capping the output
        // ═══════════════════════════════════════════════════════════════════
        bool hitCap = boostedSpeed > maxSpeedForBoost;
        if (hitCap)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[CROUCH SLAM] Speed cap! {boostedSpeed:F0} → {maxSpeedForBoost:F0} u/s");
            }
            boostedSpeed = maxSpeedForBoost; // HARD CAP - can't go higher!
        }
        
        // ═══════════════════════════════════════════════════════════════════
        // 🏔️ SLOPE ALIGNMENT - PROJECT VELOCITY ONTO SLOPE SURFACE
        // ═══════════════════════════════════════════════════════════════════
        Vector3 boostedVelocity;
        
        // PROJECT desired direction onto slope surface for natural flow
        if (slopeAngle > 5f) // On a slope
        {
            // ═══════════════════════════════════════════════════════════════════
            // 🎯 CRITICAL FIX: DOWNSLOPE SLAM MOMENTUM
            // Problem: Camera direction (horizontal) projected onto slope doesn't add downhill force
            // Solution: Blend desired direction WITH pure downhill for natural gravity assist
            // ═══════════════════════════════════════════════════════════════════
            
            // Get pure downhill direction (where gravity pulls you)
            Vector3 downhillDirection = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized;
            
            // Project desired direction onto slope plane
            Vector3 slopeAlignedDesired = Vector3.ProjectOnPlane(desiredDirection, groundNormal).normalized;
            
            // Check if player is looking downhill or uphill
            float downhillAlignment = Vector3.Dot(slopeAlignedDesired, downhillDirection);
            
            Vector3 finalDirection;
            if (downhillAlignment > 0.3f) // Looking downhill (or mostly downhill)
            {
                // BOOST MODE: Blend toward pure downhill for MAXIMUM SPEED
                // Steep slopes get more downhill bias (gravity assist!)
                float slopeIntensity = Mathf.Clamp01((slopeAngle - 5f) / 40f); // 5° = 0%, 45° = 100%
                float downhillBias = Mathf.Lerp(0.3f, 0.7f, slopeIntensity); // 30-70% downhill blend
                
                finalDirection = Vector3.Slerp(slopeAlignedDesired, downhillDirection, downhillBias).normalized;
            }
            else if (downhillAlignment < -0.3f) // Looking uphill
            {
                // UPHILL MODE: Preserve player direction but apply speed reduction
                finalDirection = slopeAlignedDesired;
                boostedSpeed *= 0.7f; // 30% speed penalty for uphill slams
            }
            else // Looking sideways across slope
            {
                // SIDEWAYS MODE: Slight downhill pull for natural feel
                finalDirection = Vector3.Slerp(slopeAlignedDesired, downhillDirection, 0.2f).normalized;
            }
            
            // Apply boosted speed along final direction
            boostedVelocity = finalDirection * boostedSpeed;
        }
        else // Flat ground
        {
            // Standard horizontal boost in desired direction
            boostedVelocity = desiredDirection * boostedSpeed;
        }
        
        // CRITICAL: Hand off to slide system for grounded control
        // CleanAAACrouch owns velocity after landing (clean ownership transfer!)
        if (crouchController != null)
        {
            // Force slide start with boosted momentum
            // The slide system will take over velocity management from here
            crouchController.QueueLandingMomentum(boostedVelocity, 0.5f);
        }
        
        // Feedback
        if (enableFeedback)
        {
            // Screen shake (bigger than normal landing)
            // Impact VFX would spawn here
            // Heavy thud audio
            GameSounds.PlayPlayerLand(transform.position); // Placeholder - replace with impact audio
        }
        
        // Reset state
        isSlamming = false;
    }
    
    /// <summary>
    /// PUBLIC API: Check if currently performing slam (for external systems)
    /// </summary>
    public bool IsSlamming => isSlamming;
    
    /// <summary>
    /// PUBLIC API: Get current slam progress (0-1) for visual effects
    /// </summary>
    public float SlamProgress
    {
        get
        {
            if (!isSlamming) return 0f;
            
            // Progress based on fall speed vs max slam force
            float currentFallSpeed = -movement.Velocity.y;
            return Mathf.Clamp01(currentFallSpeed / slamDownwardForce);
        }
    }
}
