// --- AdvancedGrapplingSystem.cs ---
// Professional grappling/rope system based on proven GitHub implementations:
// - MRTK ManipulationMoveLogic patterns (distance-based physics)
// - Unity Community spring physics (realistic tension)
// - Attack on Titan / Just Cause style dual-mode system
//
// This extends the existing RopeSwingController with advanced features
using UnityEngine;
using GeminiGauntlet.Audio;

/// <summary>
/// Advanced Grappling System - UNIFIED ROPE PHYSICS
/// 
/// SINGLE SYSTEM WITH REEL MODIFIER:
/// - Base: Pure pendulum physics (Worms ninja rope, Spider-Man 2, Attack on Titan)
/// - Gravity creates natural swing arc
/// - Hold Left Alt: Reel in (shortens rope while maintaining swing physics)
/// - Release timing = skill expression
/// 
/// CORE PHYSICS:
/// - Rope is a RIGID CONSTRAINT (not elastic)
/// - Gravity always applies (creates pendulum arc)
/// - Momentum fully preserved on release
/// - Reeling modifies constraint length, not physics behavior
/// 
/// TWO-WAY PHYSICS (Dynamic Rigidbodies):
/// - Grapple to dynamic objects (rigidbodies with isKinematic = false)
/// - Forces applied to BOTH player AND object (Newton's 3rd law)
/// - Lighter objects get dragged/swung around by player momentum
/// - Heavier objects resist but still move
/// - Object naturally "throws" when released (rigidbody keeps momentum from forces)
/// - CharacterController movement → Force application to rigidbody
/// - NO player rigidbody required (force calculated from rope tension)
/// </summary>
[RequireComponent(typeof(AAAMovementController))]
public class AdvancedGrapplingSystem : MonoBehaviour
{
    [Header("=== 🎯 GRAPPLE TARGETING ===")]
    [Tooltip("Maximum grapple range")]
    [SerializeField] private float maxGrappleRange = 10000f;
    
    [Tooltip("Minimum grapple range (0 = can shoot ground/close surfaces)")]
    [SerializeField] private float minGrappleRange = 0f;
    
    [Tooltip("Layers that can be grappled")]
    [SerializeField] private LayerMask grappleLayers = -1;
    
    [Tooltip("Aim assist radius")]
    [SerializeField] private float aimAssistRadius = 300f;
    
    
    [Header("=== 🪝 SWING PHYSICS ===")]
    [Tooltip("Air control while swinging (0 = no control, 1 = full control)")]
    [Range(0f, 1f)]
    [SerializeField] private float swingAirControl = 0.3f;
    
    [Tooltip("Add velocity in input direction while swinging (adds energy to swing)")]
    [SerializeField] private float swingInputForce = 8000f;
    
    [Tooltip("Simple air drag applied to player while swinging (0 = PERFECT CONSERVATION, >0 = energy loss)")]
    [Range(0f, 1f)]
    [SerializeField] private float swingAirDrag = 0f; // 🔥 SET TO 0 FOR PERFECT PENDULUM
    
    [Tooltip("Additional gravity multiplier while grappling (1.0 = normal gravity, >1.0 = stronger downward force)")]
    [Range(0.5f, 3.0f)]
    [SerializeField] private float grapplingGravityMultiplier = 1.0f;
    
    [Tooltip("Only apply extra gravity when swinging downward (preserves upward momentum)")]
    [SerializeField] private bool onlyApplyGravityWhenDescending = true;
    
    [Tooltip("Enable perfect pendulum energy conservation (maintains exact height on swings)")]
    [SerializeField] private bool enablePerfectEnergyConservation = false;
    
    [Tooltip("Momentum preservation factor during upward swings (1.0 = perfect conservation, <1.0 = energy loss)")]
    [Range(0.8f, 1.2f)]
    [SerializeField] private float upwardMomentumPreservation = 1.0f;
    
    [Tooltip("Additional force applied along the swing arc to maintain realistic pendulum motion")]
    [SerializeField] private float pendulumForceMultiplier = 1.0f;
    
    [Tooltip("Centripetal force multiplier - pulls toward anchor for realistic tension")]
    [SerializeField] private float centripetalForceMultiplier = 1.2f;
    
    [Tooltip("Rope constraint snap strength (higher = harder constraint, more jitter)")]
    [Range(0.5f, 1.0f)]
    [SerializeField] private float ropeConstraintStiffness = 1.0f; // 🔥 MAX for rigid constraint
    
    [Tooltip("Velocity damping when violating rope constraint (0 = PERFECT CONSERVATION, >0 = energy loss)")]
    [Range(0f, 0.5f)]
    [SerializeField] private float constraintViolationDamping = 0f; // 🔥 SET TO 0 FOR PERFECT PENDULUM - MOMENTUM KILLER!
    
    
    [Header("=== 🎣 REEL-IN SYSTEM (Hold Left Alt) ===")]
    [Tooltip("Key to hold for reeling in")]
    [SerializeField] private KeyCode reelKey = KeyCode.LeftAlt;
    
    [Tooltip("Rope shortening speed (units/sec)")]
    [SerializeField] private float reelSpeed = 800f;
    
    [Tooltip("Reel efficiency when shortening rope. Values >1 amplify the angular momentum gain, <1 dampen it")]
    [Range(0.1f, 2f)]
    [SerializeField] private float reelEfficiency = 1.0f;
    
    [Tooltip("Minimum rope length during swing (enforced even if anchor is closer)")]
    [SerializeField] private float minSwingRopeLength = 750f;
    
    [Tooltip("Minimum rope length when reeling (can't reel closer than this)")]
    [SerializeField] private float minRopeLength = 200f;
    
    
    [Header("=== 🏃 GROUNDED TETHER MODE ===")]
    [Tooltip("Allow shooting rope while grounded for tethered movement")]
    [SerializeField] private bool enableGroundedTether = true;
    
    [Tooltip("Movement speed multiplier when tethered on ground (feels more controlled)")]
    [Range(0.5f, 1.5f)]
    [SerializeField] private float groundTetherSpeedMultiplier = 0.85f;
    
    [Tooltip("How tightly to constrain to rope radius (1 = hard stop, 0.9 = slight overshoot allowed)")]
    [Range(0.8f, 1.0f)]
    [SerializeField] private float groundTetherConstraintStiffness = 0.95f;
    
    
    [Header("=== 🚀 MOMENTUM & RELEASE (CONSERVATION STYLE) ===")]
    [Tooltip("Preserve velocity on release (Conservation style - momentum chaining)")]
    [SerializeField] private bool preserveVelocityOnRelease = true;
    
    [Tooltip("ADDITIVE bonus velocity for perfect release timing (units/s, not multiplier!)")]
    [Range(0f, 1000f)]
    [SerializeField] private float optimalReleaseBonus = 500f;
    
    [Tooltip("Optimal release angle (degrees from horizontal for max boost)")]
    [SerializeField] private float optimalReleaseAngle = 45f;
    
    [Header("=== 🧗 WALL JUMP INTEGRATION ===")]
    [Tooltip("Enable wall jump while grappling (rope auto-releases on wall jump)")]
    [SerializeField] private bool enableWallJumpWhileGrappling = true;
    
    [Tooltip("Enable rope shooting during wall jump (preserves wall jump momentum)")]
    [SerializeField] private bool enableRopeShootDuringWallJump = true;
    
    [Tooltip("Momentum blend when shooting rope during wall jump (0=rope only, 1=keep wall jump)")]
    [Range(0f, 1f)]
    [SerializeField] private float wallJumpToRopeMomentumBlend = 0.7f;
    
    [Header("=== 🎢 DYNAMIC ANCHOR TRACKING ===")]
    [Tooltip("Track anchor on ANY moving/rotating object (not just rigidbodies)")]
    [SerializeField] private bool enableDynamicAnchorTracking = true;
    
    [Tooltip("Inherit object velocity on release (creates slingshot effect)")]
    [SerializeField] private bool inheritObjectVelocityOnRelease = true;
    
    [Tooltip("Object velocity multiplier on release (1.0 = 100% of object's speed)")]
    [Range(0f, 2f)]
    [SerializeField] private float objectVelocityMultiplier = 1.0f;
    
    [Tooltip("Apply continuous tangential force while attached (orbital following)")]
    [SerializeField] private bool enableOrbitalFollowing = true;
    
    [Tooltip("Orbital force strength (higher = stronger pull to follow rotation)")]
    [Range(0f, 2f)]
    [SerializeField] private float orbitalFollowStrength = 1.0f;
    
    [Header("=== 💪 TWO-WAY PHYSICS (Dynamic Rigidbodies) ===")]
    [Tooltip("Enable two-way physics with dynamic rigidbodies (drag/swing objects)")]
    [SerializeField] private bool enableTwoWayPhysics = true;
    
    [Tooltip("Force multiplier applied to dynamic rigidbodies (based on rope tension)")]
    [Range(0.1f, 10f)]
    [SerializeField] private float rigidbodyForceMultiplier = 1.5f;
    
    [Tooltip("Max force that can be applied to rigidbodies (prevents excessive forces)")]
    [SerializeField] private float maxRigidbodyForce = 50000f;
    
    [Header("=== 🎨 VISUALS ===")]
    [Tooltip("Emit point for LEFT hand rope")]
    [SerializeField] private Transform leftRopeEmitPoint;
    
    [Tooltip("Emit point for RIGHT hand rope")]
    [SerializeField] private Transform rightRopeEmitPoint;
    
    [Tooltip("Line Renderer prefab for rope visual")]
    [SerializeField] private GameObject ropeLinePrefab;
    
    [Tooltip("Rope width")]
    [SerializeField] private float ropeWidth = 20f;
    
    [Tooltip("Rope color gradient")]
    [SerializeField] private Gradient ropeColorGradient;
    
    [Header("=== 🔊 AUDIO ===")]
    [SerializeField] private SoundEvent grappleShootSound;
    [SerializeField] private SoundEvent grappleAttachSound;
    [SerializeField] private SoundEvent grappleReleaseSound;
    [SerializeField] private SoundEvent retractionLoopSound;
    
    // === CORE REFERENCES ===
    private AAAMovementController movementController;
    private CharacterController characterController;
    private Transform cameraTransform;
    private LayeredHandAnimationController handAnimationController;
    
    // === ROPE STATE ENCAPSULATION (Clean dual-rope architecture) ===
    [System.Serializable]
    private class RopeState
    {
        public bool isActive = false;
        public Vector3 anchor = Vector3.zero;
        public float ropeLength = 0f;
        public float initialDistance = 0f;
        public bool isReeling = false;
        public bool isGroundedTether = false;
        
        // Dynamic anchor tracking
        public Transform anchorTransform = null;
        public Vector3 anchorLocalPosition = Vector3.zero;
        public Vector3 lastAnchorWorldPosition = Vector3.zero;
        public Vector3 anchorVelocity = Vector3.zero;
        
        // Tethered SkullEnemy reference (for ally system)
        public SkullEnemy tetheredSkull = null;
        
        // Legacy platform support
        public Rigidbody attachedPlatform = null;
        public Vector3 lastPlatformPosition = Vector3.zero;
        public Vector3 platformVelocity = Vector3.zero;
        public Vector3 anchorLocalOffset = Vector3.zero;
        
        // Two-way physics for dynamic rigidbodies
        public Rigidbody dynamicRigidbody = null;
        public bool isDynamicObject = false;
        public float dynamicObjectMass = 0f;
        
        // Visuals
        public GameObject ropeLineInstance = null;
        public LineRenderer lineRenderer = null;
        
        // Audio
        public SoundHandle retractionSoundHandle;
        
        // Physics tracking
        public Vector3 lastFrameVelocity = Vector3.zero;
        public bool wasInWallJumpWhenAttached = false;
        
        // Energy conservation tracking
        public float initialHeight = 0f;
        public float initialKineticEnergy = 0f;
        public float totalEnergy = 0f;
        
        public void Reset()
        {
            isActive = false;
            anchor = Vector3.zero;
            ropeLength = 0f;
            initialDistance = 0f;
            isReeling = false;
            isGroundedTether = false;
            
            anchorTransform = null;
            anchorVelocity = Vector3.zero;
            attachedPlatform = null;
            platformVelocity = Vector3.zero;
            
            dynamicRigidbody = null;
            isDynamicObject = false;
            dynamicObjectMass = 0f;
            
            // Reset energy tracking
            initialHeight = 0f;
            initialKineticEnergy = 0f;
            totalEnergy = 0f;
            
            // Release tethered skull (if any)
            if (tetheredSkull != null)
            {
                tetheredSkull.OnRopeReleased();
                tetheredSkull = null;
            }
            
            if (ropeLineInstance != null)
            {
                Destroy(ropeLineInstance);
                ropeLineInstance = null;
                lineRenderer = null;
            }
            
            if (retractionSoundHandle.IsValid)
            {
                retractionSoundHandle.Stop();
            }
        }
    }
    
    // === DUAL ROPE INSTANCES ===
    private RopeState leftRope = new RopeState();
    private RopeState rightRope = new RopeState();
    
    // === PUBLIC STATE ACCESS ===
    public bool IsGrappling => leftRope.isActive || rightRope.isActive;
    public bool IsLeftRopeActive => leftRope.isActive;
    public bool IsRightRopeActive => rightRope.isActive;
    public bool IsReeling => leftRope.isReeling || rightRope.isReeling;
    public bool IsGroundedTether => (leftRope.isActive && leftRope.isGroundedTether) || (rightRope.isActive && rightRope.isGroundedTether);
    public Vector3 GrappleAnchor => leftRope.isActive ? leftRope.anchor : (rightRope.isActive ? rightRope.anchor : Vector3.zero);
    public float RopeLength => leftRope.isActive ? leftRope.ropeLength : (rightRope.isActive ? rightRope.ropeLength : 0f);
    
    // Rope anchor positions for procedural IK (RopeArmIK.cs)
    public Vector3 LeftRopeAnchor => leftRope.anchor;
    public Vector3 RightRopeAnchor => rightRope.anchor;
    
    // === SHARED PHYSICS STATE ===
    private Vector3 lastFrameVelocity = Vector3.zero;
    
    void Awake()
    {
        // Get required components
        movementController = GetComponent<AAAMovementController>();
        characterController = GetComponent<CharacterController>();
        cameraTransform = Camera.main?.transform;
        handAnimationController = GetComponent<LayeredHandAnimationController>();
        
        // Validate
        if (movementController == null || characterController == null)
        {
            Debug.LogError("[ADVANCED GRAPPLE] Missing required components! Disabling.", this);
            enabled = false;
            return;
        }
        
        if (handAnimationController == null)
        {
            Debug.LogWarning("[ADVANCED GRAPPLE] LayeredHandAnimationController not found - rope animations will not play.");
        }
        
        // 🔥 AUTO-DETECT HAND EMIT POINTS (if not assigned in inspector)
        if (leftRopeEmitPoint == null || rightRopeEmitPoint == null)
        {
            // Find PlayerShooterOrchestrator to get emit points
            PlayerShooterOrchestrator orchestrator = FindObjectOfType<PlayerShooterOrchestrator>();
            if (orchestrator != null)
            {
                if (leftRopeEmitPoint == null)
                {
                    leftRopeEmitPoint = orchestrator.leftHandEmitPoint;
                    if (leftRopeEmitPoint != null)
                        Debug.Log($"[ADVANCED GRAPPLE] ✅ Auto-detected LEFT hand emit point: {leftRopeEmitPoint.name}");
                    else
                        Debug.LogWarning("[ADVANCED GRAPPLE] ⚠️ Could not find LEFT hand emit point!");
                }
                
                if (rightRopeEmitPoint == null)
                {
                    rightRopeEmitPoint = orchestrator.rightHandEmitPoint;
                    if (rightRopeEmitPoint != null)
                        Debug.Log($"[ADVANCED GRAPPLE] ✅ Auto-detected RIGHT hand emit point: {rightRopeEmitPoint.name}");
                    else
                        Debug.LogWarning("[ADVANCED GRAPPLE] ⚠️ Could not find RIGHT hand emit point!");
                }
            }
            else
            {
                Debug.LogWarning("[ADVANCED GRAPPLE] ⚠️ PlayerShooterOrchestrator not found! Rope will spawn from body center.");
            }
        }
        
        // Initialize default gradient
        if (ropeColorGradient == null || ropeColorGradient.colorKeys.Length == 0)
        {
            ropeColorGradient = new Gradient();
            ropeColorGradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0.2f, 0.8f, 1f), 0f),
                    new GradientColorKey(new Color(1f, 0.3f, 0.5f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );
        }
    }
    
    void Update()
    {
        HandleInput();
        
        // Update physics for each active rope
        if (leftRope.isActive || rightRope.isActive)
        {
            // Update anchor positions BEFORE physics
            if (leftRope.isActive) UpdateMovingPlatformAnchor(leftRope);
            if (rightRope.isActive) UpdateMovingPlatformAnchor(rightRope);
            
            // Check grounded state transitions for each rope
            bool isGroundedNow = movementController.IsGrounded;
            
            if (leftRope.isActive)
            {
                bool wasGroundedLastFrame = leftRope.isGroundedTether;
                if (wasGroundedLastFrame && !isGroundedNow)
                {
                    leftRope.isGroundedTether = false;
                    Debug.Log("[GRAPPLE LEFT] 🚀 TETHER → SWING! Ledge jump detected!");
                }
                else if (!wasGroundedLastFrame && isGroundedNow)
                {
                    leftRope.isGroundedTether = true;
                    Debug.Log("[GRAPPLE LEFT] 🏃 SWING → TETHER! Landed while grappled!");
                }
            }
            
            if (rightRope.isActive)
            {
                bool wasGroundedLastFrame = rightRope.isGroundedTether;
                if (wasGroundedLastFrame && !isGroundedNow)
                {
                    rightRope.isGroundedTether = false;
                    Debug.Log("[GRAPPLE RIGHT] 🚀 TETHER → SWING! Ledge jump detected!");
                }
                else if (!wasGroundedLastFrame && isGroundedNow)
                {
                    rightRope.isGroundedTether = true;
                    Debug.Log("[GRAPPLE RIGHT] 🏃 SWING → TETHER! Landed while grappled!");
                }
            }
            
            // Apply physics based on mode
            bool anyGroundedTether = (leftRope.isActive && leftRope.isGroundedTether) || (rightRope.isActive && rightRope.isGroundedTether);
            
            if (anyGroundedTether)
            {
                if (leftRope.isActive && leftRope.isGroundedTether) UpdateGroundedTetherPhysics(leftRope);
                if (rightRope.isActive && rightRope.isGroundedTether) UpdateGroundedTetherPhysics(rightRope);
            }
            else
            {
                UpdateGrapplePhysics();
            }
            
            // Track velocity each frame for momentum preservation
            lastFrameVelocity = movementController.Velocity;
        }
    }
    
    /// <summary>
    /// 🔥 VISUAL FIX: Update rope visuals in LateUpdate for perfect hand tracking
    /// LateUpdate runs AFTER all animations, so hand positions are final
    /// This eliminates rope lag when moving fast (no more visual disconnect!)
    /// </summary>
    void LateUpdate()
    {
        // Update visuals AFTER all animations/movement finalized
        if (leftRope.isActive) UpdateVisuals(leftRope, leftRopeEmitPoint);
        if (rightRope.isActive) UpdateVisuals(rightRope, rightRopeEmitPoint);
    }
    
    void HandleInput()
    {
        // NEW INPUT SCHEME: Mouse3 as modifier + LMB/RMB for hand selection
        bool mouse3Held = Input.GetMouseButton(Controls.RopeSwingButton); // Mouse3/Mouse5
        
        bool lmbPressed = Input.GetMouseButtonDown(0); // Left mouse button
        bool lmbReleased = Input.GetMouseButtonUp(0);
        bool lmbHeld = Input.GetMouseButton(0);
        
        bool rmbPressed = Input.GetMouseButtonDown(1); // Right mouse button
        bool rmbReleased = Input.GetMouseButtonUp(1);
        bool rmbHeld = Input.GetMouseButton(1);
        
        // === SHOOT ROPE: Mouse3 + LMB/RMB ===
        if (mouse3Held && lmbPressed && !leftRope.isActive)
        {
            TryShootGrapple(true); // Left hand
        }
        
        if (mouse3Held && rmbPressed && !rightRope.isActive)
        {
            TryShootGrapple(false); // Right hand
        }
        
        // === RELEASE ROPE: Release LMB/RMB (Mouse3 doesn't need to be held) ===
        if (lmbReleased && leftRope.isActive)
        {
            ReleaseGrapple(true); // Left hand
        }
        
        if (rmbReleased && rightRope.isActive)
        {
            ReleaseGrapple(false); // Right hand
        }
        
        // === REEL INPUT: Alt reels BOTH active ropes ===
        bool reelPressed = Input.GetKey(reelKey);
        leftRope.isReeling = leftRope.isActive && reelPressed;
        rightRope.isReeling = rightRope.isActive && reelPressed;
        
        // Jumping NEVER releases ropes - player must manually release with LMB/RMB
    }
    
    void TryShootGrapple(bool isLeftHand)
    {
        RopeState rope = isLeftHand ? leftRope : rightRope;
        if (rope.isActive) return; // Already has rope active
        
        // Get emit point for this hand
        Transform emitPoint = isLeftHand ? leftRopeEmitPoint : rightRopeEmitPoint;
        
        // Shoot from hand emit point if assigned, otherwise camera
        Vector3 origin = emitPoint != null ? emitPoint.position : 
                        (cameraTransform != null ? cameraTransform.position : transform.position);
        Vector3 direction = cameraTransform != null ? cameraTransform.forward : transform.forward;
        
        RaycastHit hit;
        bool hitSomething = false;
        
        // Raycast first, then sphere cast for aim assist
        if (Physics.Raycast(origin, direction, out hit, maxGrappleRange, grappleLayers))
        {
            hitSomething = true;
        }
        else if (aimAssistRadius > 0f && Physics.SphereCast(origin, aimAssistRadius, direction, out hit, maxGrappleRange, grappleLayers))
        {
            hitSomething = true;
        }
        
        if (hitSomething)
        {
            float distance = Vector3.Distance(transform.position, hit.point);
            
            // Check range (minGrappleRange now supports 0 for ground shots)
            if (distance >= minGrappleRange && distance <= maxGrappleRange)
            {
                // Check for moving platforms (kinematic rigidbodies)
                Rigidbody hitRigidbody = hit.collider.attachedRigidbody;
                bool isMovingPlatform = hitRigidbody != null && hitRigidbody.isKinematic;
                bool isDynamicObject = hitRigidbody != null && !hitRigidbody.isKinematic;
                
                // Block dynamic objects ONLY if two-way physics is disabled
                if (isDynamicObject && !enableTwoWayPhysics)
                {
                    Debug.Log($"[GRAPPLE {(isLeftHand ? "LEFT" : "RIGHT")}] ❌ Cannot attach to dynamic object (two-way physics disabled): {hit.collider.name}");
                    return;
                }
                
                // Block moving platforms if disabled
                if (isMovingPlatform && !enableDynamicAnchorTracking)
                {
                    Debug.Log($"[GRAPPLE {(isLeftHand ? "LEFT" : "RIGHT")}] ❌ Moving platform grapple disabled: {hit.collider.name}");
                    return;
                }
                
                // Check if we're in a wall jump (preserve that momentum)
                bool inWallJump = movementController != null && movementController.IsInWallJumpChain;
                
                // Pass the transform and rigidbody (if exists) for tracking
                Rigidbody hitRb = isMovingPlatform ? hitRigidbody : null;
                
                // Pass dynamic rigidbody for two-way physics
                Rigidbody dynamicRb = (isDynamicObject && enableTwoWayPhysics) ? hitRigidbody : null;
                
                AttachGrapple(hit.point, distance, isLeftHand, inWallJump, hitRb, hit.collider.transform, dynamicRb);
            }
        }
    }
    
    void AttachGrapple(Vector3 anchor, float distance, bool isLeftHand, bool duringWallJump = false, Rigidbody platform = null, Transform hitTransform = null, Rigidbody dynamicRb = null)
    {
        RopeState rope = isLeftHand ? leftRope : rightRope;
        Transform emitPoint = isLeftHand ? leftRopeEmitPoint : rightRopeEmitPoint;
        
        rope.isActive = true;
        rope.anchor = anchor;
        rope.wasInWallJumpWhenAttached = duringWallJump;
        
        // === UNIVERSAL ANCHOR TRACKING (Works on EVERYTHING) ===
        rope.anchorTransform = hitTransform;
        if (rope.anchorTransform != null && enableDynamicAnchorTracking)
        {
            // Store anchor position in LOCAL SPACE of the object we hit
            rope.anchorLocalPosition = rope.anchorTransform.InverseTransformPoint(anchor);
            rope.lastAnchorWorldPosition = anchor;
            rope.anchorVelocity = Vector3.zero;
            
            Debug.Log($"[GRAPPLE {(isLeftHand ? "LEFT" : "RIGHT")}] 🎯 Attached to: {rope.anchorTransform.name} at local position {rope.anchorLocalPosition}");
            
            // === SKULL TETHER SYSTEM ===
            // Check if we hit a SkullEnemy and tether it as an ally
            SkullEnemy skull = rope.anchorTransform.GetComponent<SkullEnemy>();
            if (skull != null && !skull.IsDead())
            {
                rope.tetheredSkull = skull;
                skull.OnRopeTethered(transform, rope); // Tell skull it's now friendly and give it rope reference
                
                // 🎯 SPECIAL SKULL TETHERING PHYSICS:
                // - NO two-way physics (player wins tug-of-war, not skull!)
                // - Rope is EXTENDABLE up to 5000 units (doesn't constrain player movement)
                // - Skull gets pulled by rope constraint in SkullEnemy.cs
                rope.isDynamicObject = false; // CRITICAL: Disable two-way physics so skull doesn't drag player
                rope.dynamicRigidbody = null;
                
                Debug.Log($"[GRAPPLE {(isLeftHand ? "LEFT" : "RIGHT")}] 💀 Tethered SkullEnemy - PLAYER WINS TUG-OF-WAR! Extendable rope mode.");
            }
        }
        else
        {
            rope.anchorTransform = null;
            rope.anchorVelocity = Vector3.zero;
        }
        
        // === LEGACY MOVING PLATFORM SETUP (Keep for backward compatibility) ===
        rope.attachedPlatform = platform;
        if (rope.attachedPlatform != null && hitTransform != null)
        {
            rope.anchorLocalOffset = hitTransform.InverseTransformPoint(anchor);
            rope.lastPlatformPosition = hitTransform.position;
            rope.platformVelocity = Vector3.zero;
            
            Debug.Log($"[GRAPPLE {(isLeftHand ? "LEFT" : "RIGHT")}] 🎢 Rigidbody platform detected: {hitTransform.name}");
        }
        else
        {
            rope.attachedPlatform = null;
            rope.platformVelocity = Vector3.zero;
        }
        
        // === TWO-WAY PHYSICS SETUP (Dynamic rigidbodies) ===
        rope.dynamicRigidbody = dynamicRb;
        rope.isDynamicObject = dynamicRb != null;
        if (rope.isDynamicObject)
        {
            rope.dynamicObjectMass = dynamicRb.mass;
            Debug.Log($"[GRAPPLE {(isLeftHand ? "LEFT" : "RIGHT")}] 💪 TWO-WAY PHYSICS! Dynamic object: {hitTransform.name} (Mass: {rope.dynamicObjectMass:F1})");
        }
        else
        {
            rope.dynamicObjectMass = 0f;
        }
        
        // Initial retraction: Shorten rope by 200 units to prevent ground scraping
        float retractionAmount = distance < 300f ? 0f : 200f;
        rope.ropeLength = Mathf.Max(distance - retractionAmount, 50f);
        
        // ENFORCE MINIMUM SWING LENGTH
        if (rope.ropeLength < minSwingRopeLength)
        {
            rope.ropeLength = minSwingRopeLength;
            Debug.Log($"[GRAPPLE {(isLeftHand ? "LEFT" : "RIGHT")}] 📏 Enforced minimum swing length: {minSwingRopeLength:F0} units (anchor was {distance:F0} away)");
        }
        
        rope.initialDistance = rope.ropeLength;
        
        // Check if we're grounded when attaching
        rope.isGroundedTether = movementController.IsGrounded;
        
        // Initialize physics tracking
        if (duringWallJump && enableRopeShootDuringWallJump)
        {
            rope.lastFrameVelocity = movementController.Velocity;
            Debug.Log($"[GRAPPLE {(isLeftHand ? "LEFT" : "RIGHT")}] 🧗 Attached during wall jump! Preserving momentum: {rope.lastFrameVelocity.magnitude:F0} units/s");
        }
        else
        {
            rope.lastFrameVelocity = movementController.Velocity;
        }
        
        // === ENERGY CONSERVATION TRACKING ===
        if (enablePerfectEnergyConservation)
        {
            rope.initialHeight = transform.position.y;
            Vector3 velocity = movementController.Velocity;
            rope.initialKineticEnergy = 0.5f * velocity.sqrMagnitude; // KE = 1/2 * m * v^2 (mass = 1 for simplicity)
            float potentialEnergy = Physics.gravity.magnitude * rope.initialHeight; // PE = mgh
            rope.totalEnergy = rope.initialKineticEnergy + potentialEnergy;
            
            Debug.Log($"[GRAPPLE {(isLeftHand ? "LEFT" : "RIGHT")}] ⚡ Energy Conservation Started - Height: {rope.initialHeight:F1}, KE: {rope.initialKineticEnergy:F1}, Total: {rope.totalEnergy:F1}");
        }
        
        // Spawn rope visual
        SpawnRopeVisual(rope, emitPoint);
        
        // Play sounds
        if (grappleShootSound != null) grappleShootSound.Play3D(transform.position);
        if (grappleAttachSound != null) grappleAttachSound.Play3D(anchor);
        if (retractionLoopSound != null)
        {
            rope.retractionSoundHandle = retractionLoopSound.PlayAttached(transform);
        }
        
        // === PLAY ROPE ANIMATION ===
        if (handAnimationController != null)
        {
            handAnimationController.PlayRopeShoot(isLeftHand);
        }
        
        string modeText = rope.isGroundedTether ? "GROUNDED TETHER" : "AIRBORNE SWING";
        Debug.Log($"[GRAPPLE {(isLeftHand ? "LEFT" : "RIGHT")}] ✅ {modeText}! Rope: {rope.ropeLength:F0} units (anchor {distance:F0} away)");
    }
    
    void ReleaseGrapple(bool isLeftHand)
    {
        RopeState rope = isLeftHand ? leftRope : rightRope;
        if (!rope.isActive) return;
        
        // Release tethered skull BEFORE resetting rope state
        if (rope.tetheredSkull != null)
        {
            rope.tetheredSkull.OnRopeReleased();
            rope.tetheredSkull = null;
            Debug.Log($"[GRAPPLE {(isLeftHand ? "LEFT" : "RIGHT")}] 💀 Untethered skull - Back to HOSTILE!");
        }
        
        rope.isActive = false;
        
        // CONSERVATION STYLE: Preserve momentum + add skill bonuses
        if (preserveVelocityOnRelease)
        {
            Vector3 releaseVelocity = rope.lastFrameVelocity;
            float baseSpeed = releaseVelocity.magnitude;
            
            // Platform/object velocity bonus
            if (rope.anchorTransform != null && inheritObjectVelocityOnRelease)
            {
                Vector3 objectBonus = rope.anchorVelocity * objectVelocityMultiplier;
                releaseVelocity += objectBonus;
                Debug.Log($"[GRAPPLE {(isLeftHand ? "LEFT" : "RIGHT")}] 🌀 Object velocity added! Object: {rope.anchorVelocity.magnitude:F0} → Total: {releaseVelocity.magnitude:F0}");
            }
            else if (rope.attachedPlatform != null && inheritObjectVelocityOnRelease)
            {
                Vector3 platformBonus = rope.platformVelocity * objectVelocityMultiplier;
                releaseVelocity += platformBonus;
                Debug.Log($"[GRAPPLE {(isLeftHand ? "LEFT" : "RIGHT")}] 🎢 Platform velocity added! Platform: {rope.platformVelocity.magnitude:F0} → Total: {releaseVelocity.magnitude:F0}");
            }
            
            // Skill bonus for optimal release angle
            Vector3 horizontalVel = new Vector3(releaseVelocity.x, 0, releaseVelocity.z);
            float velocityAngle = Vector3.Angle(horizontalVel, releaseVelocity);
            bool releasingUpward = releaseVelocity.y > 0;
            
            if (releasingUpward)
            {
                float angleDifference = Mathf.Abs(velocityAngle - optimalReleaseAngle);
                float angleQuality = Mathf.Clamp01(1f - angleDifference / 45f);
                
                float bonusSpeed = optimalReleaseBonus * angleQuality;
                Vector3 bonusVelocity = releaseVelocity.normalized * bonusSpeed;
                releaseVelocity += bonusVelocity;
                
                if (angleQuality > 0.8f)
                {
                    Debug.Log($"[GRAPPLE {(isLeftHand ? "LEFT" : "RIGHT")}] 🎯 PERFECT RELEASE! Angle: {velocityAngle:F1}° Bonus: +{bonusSpeed:F0} units/s");
                }
            }
            
            float duration = Mathf.Lerp(0.2f, 0.4f, releaseVelocity.magnitude / 5000f);
            movementController.SetExternalVelocity(releaseVelocity, duration, false);
            
            if (MomentumVisualization.Instance != null)
            {
                MomentumVisualization.Instance.OnSpeedGain(baseSpeed, releaseVelocity.magnitude, transform.position);
            }
            
            float speedGain = ((releaseVelocity.magnitude - baseSpeed) / baseSpeed) * 100f;
            Debug.Log($"[GRAPPLE {(isLeftHand ? "LEFT" : "RIGHT")}] 🚀 Released! Speed: {releaseVelocity.magnitude:F0} units/s (+{speedGain:F1}% gain)");
        }
        
        // Cleanup
        rope.Reset();
        if (grappleReleaseSound != null) grappleReleaseSound.Play3D(transform.position);
        
        // === STOP ROPE ANIMATION ===
        if (handAnimationController != null)
        {
            handAnimationController.StopRopeAnimation(isLeftHand);
        }
    }
    
    /// <summary>
    /// GROUNDED TETHER PHYSICS - Circular constraint movement
    /// Player can strafe/move forward but constrained to rope radius
    /// 
    /// 🔥 FIX: Separated reel mechanics from constraint enforcement to prevent
    /// violent snapping when reeling in while grounded
    /// </summary>
    void UpdateGroundedTetherPhysics(RopeState rope)
    {
        Vector3 toAnchor = rope.anchor - transform.position;
        float currentDistance = toAnchor.magnitude;
        
        if (currentDistance < 0.1f)
        {
            ReleaseGrapple(rope == leftRope);
            return;
        }
        
        // === REEL-IN SYSTEM (Grounded Mode) ===
        if (rope.isReeling && rope.ropeLength > minRopeLength)
        {
            float oldLength = rope.ropeLength;
            
            // 🔥 FIX: Slower reel speed when grounded (prevents violent snapping)
            // Ground friction + fast reel = accumulated constraint violations = sudden yank
            float groundedReelSpeed = reelSpeed * 0.4f; // 40% speed when grounded for smooth feel
            float shortenAmount = groundedReelSpeed * Time.deltaTime;
            float newLength = Mathf.Max(oldLength - shortenAmount, minRopeLength);
            rope.ropeLength = newLength;

            // 🔥 FIX: Apply gentle inward force during grounded reel instead of relying on constraint
            // This gives smooth continuous movement instead of sudden snaps
            if (newLength < oldLength - 0.001f)
            {
                Vector3 dirToAnchor = toAnchor.normalized;
                float reelPullForce = shortenAmount * 2f; // Gentle continuous pull
                characterController.Move(dirToAnchor * reelPullForce * Time.deltaTime);
                
                // 🔥 FIX: DO NOT apply angular momentum amplification when grounded
                // The issue: Sprint forward + shoot rope forward + reel = tangential velocity gets multiplied
                // Angular momentum conservation is for ORBITAL motion (spinning around anchor)
                // NOT for linear motion (running forward)
                // 
                // When grounded, player is walking/running, not swinging in pendulum arc
                // Applying ratio amplification to linear movement = speed multiplication bug
                //
                // Solution: Only preserve direction, don't amplify magnitude when grounded
                Vector3 currentVelocity = movementController.Velocity;
                Vector3 tangential = currentVelocity - dirToAnchor * Vector3.Dot(currentVelocity, dirToAnchor);
                if (tangential.magnitude > 0.001f)
                {
                    // Keep tangential direction but don't amplify speed (grounded = no pendulum physics)
                    // Only apply gentle redirection toward anchor, preserving sprint speed
                    Vector3 adjusted = currentVelocity.normalized * currentVelocity.magnitude;
                    movementController.SetExternalVelocity(adjusted, Time.deltaTime * 2f, false);
                }
            }
        }
        
        // === CIRCULAR CONSTRAINT - Only enforce when NOT actively reeling ===
        // 🔥 FIX: Disable harsh constraint correction during reel to prevent force accumulation
        if (!rope.isReeling && currentDistance > rope.ropeLength)
        {
            Vector3 directionToAnchor = toAnchor.normalized;
            float overshoot = currentDistance - rope.ropeLength;

            // Soft positional correction to avoid snapping
            Vector3 correction = directionToAnchor * (overshoot * groundTetherConstraintStiffness);
            characterController.Move(correction * 0.9f);

            Vector3 currentVelocity = movementController.Velocity;

            float velocityAwayFromAnchor = Vector3.Dot(currentVelocity, -directionToAnchor);
            if (velocityAwayFromAnchor > 0f)
            {
                // Remove outward velocity component without damping (perfect conservation)
                Vector3 outwardComp = -directionToAnchor * velocityAwayFromAnchor;
                currentVelocity -= outwardComp;

                // Apply a small spring impulse to pull player back toward anchor
                Vector3 springImpulse = -directionToAnchor * (overshoot * groundTetherConstraintStiffness * 0.5f);
                currentVelocity += springImpulse * Time.deltaTime;

                movementController.SetExternalVelocity(currentVelocity, Time.deltaTime * 2f, false);
            }
        }
    }
    
    void UpdateGrapplePhysics()
    {
        // 🔥 SIMPLE APPROACH: Let AAAMovementController handle ALL gravity and physics
        // Rope system ONLY applies constraint forces (like a real rope would)
        
        Vector3 currentVelocity = movementController.Velocity;
        
        // Process each active rope independently - ONLY apply constraint forces
        if (leftRope.isActive)
        {
            ApplyRopeConstraint(leftRope, ref currentVelocity);
        }
        
        if (rightRope.isActive)
        {
            ApplyRopeConstraint(rightRope, ref currentVelocity);
        }
        
        // Set the constrained velocity - let AAAMovementController handle everything else
        movementController.SetExternalVelocity(currentVelocity, Time.deltaTime * 2f, false);
        
        // Update per-rope state
        if (leftRope.isActive) leftRope.lastFrameVelocity = currentVelocity;
        if (rightRope.isActive) rightRope.lastFrameVelocity = currentVelocity;
    }
    
    /// <summary>
    /// SIMPLE ROPE CONSTRAINT - Enforces rope length + centripetal force for tight swings
    /// Based on proven implementations that work WITH CharacterController physics
    /// SPECIAL: For skull tethering, rope extends dynamically up to 5000 units
    /// 
    /// 🔥 RESTORED: Centripetal force for responsive arc control (Option A)
    /// </summary>
    void ApplyRopeConstraint(RopeState rope, ref Vector3 currentVelocity)
    {
        Vector3 toAnchor = rope.anchor - transform.position;
        float currentDistance = toAnchor.magnitude;
        
        if (currentDistance < 0.1f)
        {
            ReleaseGrapple(rope == leftRope);
            return;
        }
        
        Vector3 directionToAnchor = toAnchor.normalized;
        
        // === SKULL TETHER SPECIAL BEHAVIOR ===
        // Rope extends dynamically as player moves away (up to 5000 units max)
        // Skull gets pulled by its own rope constraint system (in SkullEnemy.cs)
        if (rope.tetheredSkull != null && !rope.tetheredSkull.IsDead())
        {
            float maxSkullRopeLength = 5000f; // Max tether distance
            
            // Allow rope to extend as player moves away
            if (currentDistance > rope.ropeLength)
            {
                rope.ropeLength = Mathf.Min(currentDistance, maxSkullRopeLength);
            }
            
            // Only constrain if beyond maximum
            if (currentDistance > maxSkullRopeLength)
            {
                float overshoot = currentDistance - maxSkullRopeLength;
                Vector3 correction = directionToAnchor * overshoot;
                characterController.Move(correction);
                
                // Remove outward velocity
                float velocityAwayFromAnchor = Vector3.Dot(currentVelocity, -directionToAnchor);
                if (velocityAwayFromAnchor > 0f)
                {
                    Vector3 outwardComponent = -directionToAnchor * velocityAwayFromAnchor;
                    currentVelocity -= outwardComponent;
                }
            }
            
            // Skull tethering doesn't constrain player movement - player is free!
            return;
        }
        
        // === NORMAL ROPE SWING BEHAVIOR ===
        // === REEL-IN MODIFIER (with angular momentum conservation) ===
        if (rope.isReeling && rope.ropeLength > minRopeLength)
        {
            float oldLength = rope.ropeLength;
            float shortenAmount = reelSpeed * Time.deltaTime;
            float newLength = Mathf.Max(oldLength - shortenAmount, minRopeLength);
            rope.ropeLength = newLength;
            
            // 🔥 RESTORED: Angular momentum conservation during reel (figure skater effect)
            if (newLength < oldLength - 0.001f)
            {
                float radialVelocity = Vector3.Dot(currentVelocity, directionToAnchor);
                Vector3 tangentialVelocity = currentVelocity - directionToAnchor * radialVelocity;
                
                if (tangentialVelocity.magnitude > 0.001f)
                {
                    // L = mvr conservation: when r decreases, v increases
                    float ratio = Mathf.Clamp((oldLength / newLength) * reelEfficiency, 0.5f, 4f);
                    Vector3 amplifiedTangential = tangentialVelocity * ratio;
                    currentVelocity = directionToAnchor * radialVelocity + amplifiedTangential;
                }
            }
        }
        
        // === ROPE LENGTH CONSTRAINT ===
        if (currentDistance > rope.ropeLength)
        {
            // Snap position to rope length
            float overshoot = currentDistance - rope.ropeLength;
            Vector3 correction = directionToAnchor * (overshoot * ropeConstraintStiffness);
            characterController.Move(correction);
            
            // Remove velocity component that would take you away from anchor
            float velocityAwayFromAnchor = Vector3.Dot(currentVelocity, -directionToAnchor);
            if (velocityAwayFromAnchor > 0f)
            {
                Vector3 outwardComponent = -directionToAnchor * velocityAwayFromAnchor;
                
                // Apply constraint violation damping (0 = perfect conservation)
                currentVelocity -= outwardComponent * (1f - constraintViolationDamping);
            }
            
            // Update position after constraint
            toAnchor = rope.anchor - transform.position;
            currentDistance = toAnchor.magnitude;
            directionToAnchor = toAnchor.normalized;
        }
        
        // === 🔥 CENTRIPETAL FORCE - RESTORED (This is what your multiplier controls!) ===
        // This pulls you toward the anchor during circular motion (tighter arc control)
        if (currentDistance > 0.1f && centripetalForceMultiplier > 0f)
        {
            // Calculate tangential velocity (perpendicular to rope)
            Vector3 tangentialVelocity = currentVelocity - directionToAnchor * Vector3.Dot(currentVelocity, directionToAnchor);
            float tangentialSpeed = tangentialVelocity.magnitude;
            
            if (tangentialSpeed > 0.01f)
            {
                // Centripetal acceleration: a = v² / r
                float centripetalAccel = (tangentialSpeed * tangentialSpeed) / Mathf.Max(currentDistance, 0.0001f);
                
                // Apply your multiplier here!
                Vector3 centripetalForce = directionToAnchor * centripetalAccel * centripetalForceMultiplier * Time.deltaTime;
                
                // Safety clamp to prevent extreme values
                centripetalForce = Vector3.ClampMagnitude(centripetalForce, 2000f * Time.deltaTime);
                
                currentVelocity += centripetalForce;
            }
        }
        
        // === 🔥 PENDULUM FORCE - RESTORED (Gravity component along swing arc) ===
        // This creates natural pendulum acceleration (faster at bottom, slower at top)
        if (currentDistance > 0.1f && pendulumForceMultiplier > 0f)
        {
            // Calculate tangential gravity component (perpendicular to rope)
            Vector3 gravityDirection = Vector3.down;
            Vector3 tangentialGravity = gravityDirection - directionToAnchor * Vector3.Dot(gravityDirection, directionToAnchor);
            
            // Apply gravity along the swing arc (creates natural pendulum motion)
            Vector3 pendulumForce = tangentialGravity * Physics.gravity.magnitude * pendulumForceMultiplier * Time.deltaTime;
            
            currentVelocity += pendulumForce;
        }
        
        // === UPWARD MOMENTUM PRESERVATION ===
        // Adjust energy gain/loss during upward swings
        if (currentVelocity.y > 0f && upwardMomentumPreservation != 1.0f)
        {
            Vector3 upwardComponent = Vector3.up * Mathf.Max(0f, currentVelocity.y);
            Vector3 adjustedUpward = upwardComponent * upwardMomentumPreservation;
            Vector3 horizontalComponent = currentVelocity - Vector3.up * currentVelocity.y;
            currentVelocity = horizontalComponent + adjustedUpward;
        }
        
        // === 🔥 PLAYER INPUT - RESTORED (Swing steering with your air control settings!) ===
        float inputX = Controls.HorizontalRaw();
        float inputY = Controls.VerticalRaw();
        
        if (Mathf.Abs(inputX) > 0.01f || Mathf.Abs(inputY) > 0.01f)
        {
            // Get camera-relative input direction
            Vector3 cameraForward = cameraTransform != null ? cameraTransform.forward : transform.forward;
            Vector3 cameraRight = cameraTransform != null ? cameraTransform.right : transform.right;
            cameraForward.y = 0;
            cameraRight.y = 0;
            cameraForward.Normalize();
            cameraRight.Normalize();
            
            Vector3 inputDirection = (cameraForward * inputY + cameraRight * inputX).normalized;
            
            // Apply swing input force (now your swingInputForce slider works!)
            Vector3 swingForce = inputDirection * swingInputForce * Time.deltaTime;
            
            // Project force to be tangential to rope (can't pull toward/away from anchor)
            Vector3 tangentialForce = swingForce - directionToAnchor * Vector3.Dot(swingForce, directionToAnchor);
            
            // Dynamic air control - reduces at high speeds for more momentum-based feel
            float speedFactor = Mathf.Clamp01(currentVelocity.magnitude / 3000f);
            float dynamicAirControl = Mathf.Lerp(swingAirControl, swingAirControl * 0.6f, speedFactor);
            
            // Apply controlled input force (now your swingAirControl slider works!)
            currentVelocity += tangentialForce * dynamicAirControl;
        }
        
        // === 🔥 AIR DRAG - RESTORED (Your swing air drag slider is now active!) ===
        if (swingAirDrag > 0f)
        {
            // Simple velocity damping (0 = no drag, 1 = instant stop)
            currentVelocity *= (1f - swingAirDrag * Time.deltaTime);
        }
    }
    
    /// <summary>
    /// DEPRECATED - Old complex system that fought against AAAMovementController
    /// </summary>
    void ProcessRopePhysics(RopeState rope, ref Vector3 currentVelocity, ref Vector3 totalConstraintForce, ref Vector3 totalCentripetalForce, ref Vector3 totalInputForce, ref Vector3 totalOrbitalForce)
    {
        Vector3 toAnchor = rope.anchor - transform.position;
        float currentDistance = toAnchor.magnitude;
        
        if (currentDistance < 0.1f)
        {
            ReleaseGrapple(rope == leftRope);
            return;
        }
        
        Vector3 directionToAnchor = toAnchor / currentDistance;
        
        // === REEL-IN MODIFIER ===
        if (rope.isReeling && rope.ropeLength > minRopeLength)
        {
            float oldLength = rope.ropeLength;
            float shortenAmount = reelSpeed * Time.deltaTime;
            float newLength = Mathf.Max(oldLength - shortenAmount, minRopeLength);
            rope.ropeLength = newLength;

            // Preserve angular momentum: scale tangential velocity when radius shortens
            if (newLength < oldLength - 0.001f)
            {
                Vector3 dir = directionToAnchor = toAnchor.normalized; // ensure direction is set
                float along = Vector3.Dot(currentVelocity, dir);
                Vector3 tangential = currentVelocity - dir * along;

                if (tangential.magnitude > 0.001f)
                {
                    float ratio = Mathf.Clamp((oldLength / newLength) * reelEfficiency, 0.5f, 4f);
                    Vector3 newTangential = tangential * ratio;
                    currentVelocity = dir * along + newTangential;
                }
            }
        }
        
        // === ROPE CONSTRAINT ===
        if (currentDistance > rope.ropeLength)
        {
            float overshoot = currentDistance - rope.ropeLength;
            
            // 🔥 PERFECT CONSERVATION FIX: Only redirect velocity, don't dampen it
            Vector3 correction = directionToAnchor * overshoot;
            characterController.Move(correction); // Snap to rope length (no partial correction)

            // Remove outward velocity component but preserve TOTAL SPEED (conservation)
            float velocityAwayFromAnchor = Vector3.Dot(currentVelocity, -directionToAnchor);
            if (velocityAwayFromAnchor > 0f)
            {
                // 🔥 CRITICAL FIX: Perfect energy conservation - redirect velocity tangentially
                Vector3 outwardComponent = -directionToAnchor * velocityAwayFromAnchor;
                
                // Remove outward component completely (redirect to tangential)
                currentVelocity -= outwardComponent;
                
                // 🚫 REMOVED DOUBLE DAMPING - this was killing momentum!
                // The constraint violation damping was being applied AFTER already removing the component
                // This was causing 60-70% energy loss instead of perfect conservation
            }

            toAnchor = rope.anchor - transform.position;
            currentDistance = toAnchor.magnitude;
            directionToAnchor = toAnchor.normalized;
        }
        
        // === CENTRIPETAL FORCE ===
        if (currentDistance > 0.1f && currentDistance >= rope.ropeLength * 0.5f)
        {
            Vector3 tangentialVelocity = currentVelocity - directionToAnchor * Vector3.Dot(currentVelocity, directionToAnchor);
            float tangentialSpeed = tangentialVelocity.magnitude;

            if (tangentialSpeed > 0.01f)
            {
                // realistic centripetal acceleration: a = v^2 / r
                float centripetal = (tangentialSpeed * tangentialSpeed) / Mathf.Max(currentDistance, 0.0001f);
                Vector3 centripetalAcceleration = directionToAnchor * centripetal * centripetalForceMultiplier * Time.deltaTime;
                // clamp to avoid extreme numbers
                centripetalAcceleration = Vector3.ClampMagnitude(centripetalAcceleration, 2000f * Time.deltaTime);
                totalCentripetalForce += centripetalAcceleration;
            }
        }
        
        // === IMPROVED PENDULUM PHYSICS ===
        // 🚫 DISABLED - This was adding complexity that interfered with natural momentum
        // The rope constraint + centripetal force is sufficient for realistic pendulum motion
        /*
        // Add realistic pendulum forces based on gravity and rope angle
        if (pendulumForceMultiplier > 0f && currentDistance > 0.1f)
        {
            Vector3 toAnchorNormalized = directionToAnchor;
            Vector3 gravityDirection = Vector3.down;
            
            // Calculate the component of gravity perpendicular to the rope (tangential force)
            Vector3 tangentialGravity = gravityDirection - toAnchorNormalized * Vector3.Dot(gravityDirection, toAnchorNormalized);
            
            // This creates the natural pendulum restoring force
            Vector3 pendulumForce = tangentialGravity * Physics.gravity.magnitude * pendulumForceMultiplier * Time.deltaTime;
            
            // Apply momentum preservation for upward swings
            if (currentVelocity.y > 0f && upwardMomentumPreservation != 1.0f)
            {
                // Adjust upward momentum based on preservation factor
                Vector3 upwardComponent = Vector3.up * Mathf.Max(0f, currentVelocity.y);
                Vector3 adjustedUpward = upwardComponent * upwardMomentumPreservation;
                Vector3 horizontalComponent = currentVelocity - upwardComponent;
                currentVelocity = horizontalComponent + adjustedUpward;
            }
            
            totalCentripetalForce += pendulumForce;
        }
        */
        
        // === PLAYER INPUT (Swing steering) ===
        float inputX = Controls.HorizontalRaw();
        float inputY = Controls.VerticalRaw();
        
        if (Mathf.Abs(inputX) > 0.01f || Mathf.Abs(inputY) > 0.01f)
        {
            Vector3 cameraForward = cameraTransform != null ? cameraTransform.forward : transform.forward;
            Vector3 cameraRight = cameraTransform != null ? cameraTransform.right : transform.right;
            cameraForward.y = 0;
            cameraRight.y = 0;
            cameraForward.Normalize();
            cameraRight.Normalize();
            
            Vector3 inputDirection = (cameraForward * inputY + cameraRight * inputX).normalized;
            Vector3 swingForce = inputDirection * swingInputForce * Time.deltaTime;
            
            Vector3 tangentialForce = swingForce - directionToAnchor * Vector3.Dot(swingForce, directionToAnchor);
            
            float speedFactor = Mathf.Clamp01(currentVelocity.magnitude / 3000f);
            float dynamicAirControl = Mathf.Lerp(swingAirControl, swingAirControl * 0.6f, speedFactor);
            
            totalInputForce += tangentialForce * dynamicAirControl;
        }
        
        // === ORBITAL FOLLOWING ===
        if (enableOrbitalFollowing && rope.anchorVelocity.magnitude > 0.1f)
        {
            Vector3 anchorMotionDir = rope.anchorVelocity.normalized;
            Vector3 ropeDir = directionToAnchor;
            
            Vector3 tangentialAnchorMotion = anchorMotionDir - ropeDir * Vector3.Dot(anchorMotionDir, ropeDir);
            
            if (tangentialAnchorMotion.magnitude > 0.01f)
            {
                float orbitalForce = rope.anchorVelocity.magnitude * orbitalFollowStrength * Time.deltaTime;
                Vector3 orbitalAcceleration = tangentialAnchorMotion.normalized * orbitalForce;
                
                totalOrbitalForce += orbitalAcceleration;
                // Debug.DrawRay removed for performance
            }
        }
        
        // === TWO-WAY PHYSICS (Apply opposite forces to dynamic rigidbodies) ===
        if (rope.isDynamicObject && rope.dynamicRigidbody != null && enableTwoWayPhysics)
        {
            // Calculate total force being applied to player
            Vector3 totalPlayerForce = totalCentripetalForce + totalInputForce + totalOrbitalForce;
            
            // Calculate constraint force from rope tension
            Vector3 constraintForce = Vector3.zero;
            if (currentDistance > rope.ropeLength)
            {
                float overshoot = currentDistance - rope.ropeLength;
                constraintForce = -directionToAnchor * (overshoot * ropeConstraintStiffness * 15000f);
            }
            
            // Total force = constraint + swing forces
            Vector3 totalForce = constraintForce + (totalPlayerForce * 10000f);
            
            // Apply opposite force to rigidbody (Newton's 3rd law)
            Vector3 rigidbodyForce = -totalForce * rigidbodyForceMultiplier;
            
            // Clamp to prevent excessive forces
            rigidbodyForce = Vector3.ClampMagnitude(rigidbodyForce, maxRigidbodyForce);
            
            // Apply force at the anchor point for realistic torque
            rope.dynamicRigidbody.AddForceAtPosition(rigidbodyForce, rope.anchor, ForceMode.Force);
            
            // Visual debug removed for performance
        }
    }
    
    /// <summary>
    /// UNIVERSAL ANCHOR TRACKING - Updates anchor position for ANY moving/rotating object
    /// </summary>
    void UpdateMovingPlatformAnchor(RopeState rope)
    {
        // === NEW UNIVERSAL SYSTEM ===
        if (rope.anchorTransform != null && enableDynamicAnchorTracking)
        {
            Vector3 newAnchorWorld = rope.anchorTransform.TransformPoint(rope.anchorLocalPosition);
            rope.anchorVelocity = (newAnchorWorld - rope.lastAnchorWorldPosition) / Time.deltaTime;
            rope.lastAnchorWorldPosition = newAnchorWorld;
            rope.anchor = newAnchorWorld;
            
            if (rope.anchorVelocity.magnitude > 1f)
            {
                // Debug.DrawRay removed for performance
            }
            
            return;
        }
        
        // === LEGACY RIGIDBODY PLATFORM SYSTEM (Fallback) ===
        if (rope.attachedPlatform == null) return;
        
        Transform platformTransform = rope.attachedPlatform.transform;
        if (platformTransform == null)
        {
            Debug.LogWarning("[GRAPPLE] Platform destroyed! Releasing grapple.");
            ReleaseGrapple(rope == leftRope);
            return;
        }
        
        Vector3 newAnchor = platformTransform.TransformPoint(rope.anchorLocalOffset);
        rope.platformVelocity = (platformTransform.position - rope.lastPlatformPosition) / Time.deltaTime;
        rope.lastPlatformPosition = platformTransform.position;
        rope.anchor = newAnchor;
        
        float newDistance = Vector3.Distance(transform.position, rope.anchor);
        if (Mathf.Abs(newDistance - rope.ropeLength) > 10f)
        {
            rope.ropeLength = newDistance;
        }
    }
    
    void SpawnRopeVisual(RopeState rope, Transform emitPoint)
    {
        if (ropeLinePrefab != null)
        {
            // 🔥 FIXED: Parent to emit point for PERFECT hand tracking (just like beam particles!)
            // This ensures the LineRenderer GameObject follows the hand with ZERO lag
            Transform parent = emitPoint != null ? emitPoint : transform;
            rope.ropeLineInstance = Instantiate(ropeLinePrefab, parent.position, Quaternion.identity, parent);
            rope.lineRenderer = rope.ropeLineInstance.GetComponent<LineRenderer>();
            
            if (rope.lineRenderer != null)
            {
                rope.lineRenderer.positionCount = 2;
                rope.lineRenderer.useWorldSpace = true; // CRITICAL: World space so positions work correctly
                rope.lineRenderer.startWidth = ropeWidth;
                rope.lineRenderer.endWidth = ropeWidth;
            }
        }
    }
    
    void UpdateVisuals(RopeState rope, Transform emitPoint)
    {
        if (rope.lineRenderer != null)
        {
            // Use emit point if assigned, otherwise fallback to body center
            Vector3 ropeStart = emitPoint != null ? emitPoint.position : 
                               (transform.position + Vector3.up * (characterController.height * 0.5f));
            
            rope.lineRenderer.SetPosition(0, ropeStart);
            rope.lineRenderer.SetPosition(1, rope.anchor);
            
            float speed = movementController.Velocity.magnitude;
            float speedNormalized = Mathf.Clamp01(speed / 3000f);
            
            float currentDistance = Vector3.Distance(transform.position, rope.anchor);
            float tensionRatio = Mathf.Clamp01((currentDistance - rope.ropeLength * 0.8f) / (rope.ropeLength * 0.2f));
            
            Color ropeColor;
            
            if (rope.isGroundedTether)
            {
                Color safeColor = new Color(0.2f, 1f, 0.4f);
                Color warningColor = new Color(1f, 0.9f, 0.2f);
                Color constraintColor = new Color(1f, 0.3f, 0.2f);
                
                ropeColor = Color.Lerp(safeColor, warningColor, tensionRatio);
                if (tensionRatio > 0.8f)
                {
                    ropeColor = Color.Lerp(warningColor, constraintColor, (tensionRatio - 0.8f) / 0.2f);
                }
                
                if (rope.isReeling)
                {
                    ropeColor = Color.Lerp(ropeColor, new Color(1f, 1f, 0.2f), 0.6f);
                }
            }
            else
            {
                Color relaxedColor = new Color(0.2f, 0.6f, 1f);
                Color fastColor = new Color(1f, 0.5f, 0f);
                Color tensionColor = new Color(1f, 0.2f, 0.2f);
                Color reelingColor = new Color(1f, 1f, 0.2f);
                
                Color speedColor = Color.Lerp(relaxedColor, fastColor, speedNormalized);
                ropeColor = Color.Lerp(speedColor, tensionColor, tensionRatio * 0.5f);
                
                if (rope.isReeling)
                {
                    ropeColor = Color.Lerp(ropeColor, reelingColor, 0.7f);
                }
            }
            
            float tensionPulse = 1f + tensionRatio * 0.3f;
            rope.lineRenderer.startWidth = ropeWidth * tensionPulse;
            rope.lineRenderer.endWidth = ropeWidth * tensionPulse;
            
            rope.lineRenderer.startColor = ropeColor;
            rope.lineRenderer.endColor = ropeColor;
        }
    }
    
    void OnDisable()
    {
        if (leftRope.isActive) ReleaseGrapple(true);
        if (rightRope.isActive) ReleaseGrapple(false);
    }
    
    void OnDrawGizmos()
    {
        if (leftRope.isActive)
        {
            DrawRopeGizmo(leftRope, Color.cyan, Color.green);
        }
        
        if (rightRope.isActive)
        {
            DrawRopeGizmo(rightRope, Color.magenta, Color.yellow);
        }
        
        // Draw velocity vector
        Vector3 vel = movementController != null ? movementController.Velocity : Vector3.zero;
        Gizmos.color = Color.white;
        Gizmos.DrawRay(transform.position, vel.normalized * (vel.magnitude * 0.1f));
    }
    
    void DrawRopeGizmo(RopeState rope, Color airborneColor, Color groundedColor)
    {
        Gizmos.color = rope.isGroundedTether ? groundedColor : (rope.isReeling ? Color.yellow : airborneColor);
        Gizmos.DrawLine(transform.position, rope.anchor);
        
        Gizmos.color = rope.isGroundedTether ? groundedColor : Color.yellow;
        Gizmos.DrawWireSphere(rope.anchor, 100f);
        
        Gizmos.color = rope.isGroundedTether ? 
            new Color(0f, 1f, 0f, 0.4f) : 
            new Color(airborneColor.r, airborneColor.g, airborneColor.b, 0.3f);
        Gizmos.DrawWireSphere(rope.anchor, rope.ropeLength);
        
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(rope.anchor, minRopeLength);
        
        if (rope.isGroundedTether)
        {
            Vector3 anchorGroundPos = new Vector3(rope.anchor.x, transform.position.y, rope.anchor.z);
            Gizmos.color = new Color(groundedColor.r, groundedColor.g, groundedColor.b, 0.2f);
            DrawCircleGizmo(anchorGroundPos, rope.ropeLength, 32);
        }
        
        if (rope.anchorVelocity.magnitude > 0.1f)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(rope.anchor, rope.anchorVelocity.normalized * 200f);
            
            if (enableOrbitalFollowing)
            {
                Gizmos.color = airborneColor;
                Vector3 toAnchor = rope.anchor - transform.position;
                Vector3 dirToAnchor = toAnchor.normalized;
                Vector3 tangentialMotion = rope.anchorVelocity - dirToAnchor * Vector3.Dot(rope.anchorVelocity, dirToAnchor);
                if (tangentialMotion.magnitude > 0.01f)
                {
                    Gizmos.DrawRay(transform.position, tangentialMotion.normalized * 150f);
                }
            }
        }
    }
    
    /// <summary>
    /// Helper to draw horizontal circle gizmo for grounded tether visualization
    /// </summary>
    void DrawCircleGizmo(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
}
