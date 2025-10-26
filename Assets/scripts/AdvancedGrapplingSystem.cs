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
    
    [Tooltip("Centripetal force multiplier - pulls toward anchor for realistic tension")]
    [SerializeField] private float centripetalForceMultiplier = 1.2f;
    
    [Tooltip("Rope constraint snap strength (higher = harder constraint, more jitter)")]
    [Range(0.5f, 1.0f)]
    [SerializeField] private float ropeConstraintStiffness = 0.85f;
    
    [Tooltip("Velocity damping when violating rope constraint (prevents jitter)")]
    [Range(0f, 0.5f)]
    [SerializeField] private float constraintViolationDamping = 0.15f;
    
    
    [Header("=== 🎣 REEL-IN SYSTEM (Hold Left Alt) ===")]
    [Tooltip("Key to hold for reeling in")]
    [SerializeField] private KeyCode reelKey = KeyCode.LeftAlt;
    
    [Tooltip("Rope shortening speed (units/sec)")]
    [SerializeField] private float reelSpeed = 800f;
    
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
        
        // Legacy platform support
        public Rigidbody attachedPlatform = null;
        public Vector3 lastPlatformPosition = Vector3.zero;
        public Vector3 platformVelocity = Vector3.zero;
        public Vector3 anchorLocalOffset = Vector3.zero;
        
        // Visuals
        public GameObject ropeLineInstance = null;
        public LineRenderer lineRenderer = null;
        
        // Audio
        public SoundHandle retractionSoundHandle;
        
        // Physics tracking
        public Vector3 lastFrameVelocity = Vector3.zero;
        public bool wasInWallJumpWhenAttached = false;
        
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
        
        // Validate
        if (movementController == null || characterController == null)
        {
            Debug.LogError("[ADVANCED GRAPPLE] Missing required components! Disabling.", this);
            enabled = false;
            return;
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
            
            // Update visuals for each rope
            if (leftRope.isActive) UpdateVisuals(leftRope, leftRopeEmitPoint);
            if (rightRope.isActive) UpdateVisuals(rightRope, rightRopeEmitPoint);
            
            // Track velocity each frame for momentum preservation
            lastFrameVelocity = movementController.Velocity;
        }
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
                
                // Block dynamic (physics-driven) objects, but allow kinematic (moving platforms)
                if (isDynamicObject)
                {
                    Debug.Log($"[GRAPPLE {(isLeftHand ? "LEFT" : "RIGHT")}] ❌ Cannot attach to dynamic object: {hit.collider.name}");
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
                AttachGrapple(hit.point, distance, isLeftHand, inWallJump, hitRb, hit.collider.transform);
            }
        }
    }
    
    void AttachGrapple(Vector3 anchor, float distance, bool isLeftHand, bool duringWallJump = false, Rigidbody platform = null, Transform hitTransform = null)
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
        
        // Spawn rope visual
        SpawnRopeVisual(rope, emitPoint);
        
        // Play sounds
        if (grappleShootSound != null) grappleShootSound.Play3D(transform.position);
        if (grappleAttachSound != null) grappleAttachSound.Play3D(anchor);
        if (retractionLoopSound != null)
        {
            rope.retractionSoundHandle = retractionLoopSound.PlayAttached(transform);
        }
        
        string modeText = rope.isGroundedTether ? "GROUNDED TETHER" : "AIRBORNE SWING";
        Debug.Log($"[GRAPPLE {(isLeftHand ? "LEFT" : "RIGHT")}] ✅ {modeText}! Rope: {rope.ropeLength:F0} units (anchor {distance:F0} away)");
    }
    
    void ReleaseGrapple(bool isLeftHand)
    {
        RopeState rope = isLeftHand ? leftRope : rightRope;
        if (!rope.isActive) return;
        
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
    }
    
    /// <summary>
    /// GROUNDED TETHER PHYSICS - Circular constraint movement
    /// Player can strafe/move forward but constrained to rope radius
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
        
        // === REEL-IN MODIFIER (works same in grounded mode) ===
        if (rope.isReeling && rope.ropeLength > minRopeLength)
        {
            float shortenAmount = reelSpeed * Time.deltaTime;
            rope.ropeLength = Mathf.Max(rope.ropeLength - shortenAmount, minRopeLength);
        }
        
        // === CIRCULAR CONSTRAINT - Prevent moving beyond rope radius ===
        if (currentDistance > rope.ropeLength)
        {
            Vector3 directionToAnchor = toAnchor.normalized;
            float overshoot = currentDistance - rope.ropeLength;
            
            Vector3 correction = directionToAnchor * (overshoot * groundTetherConstraintStiffness);
            characterController.Move(correction);
            
            Vector3 currentVelocity = movementController.Velocity;
            
            float velocityAwayFromAnchor = Vector3.Dot(currentVelocity, -directionToAnchor);
            if (velocityAwayFromAnchor > 0f)
            {
                Vector3 velocityToRemove = -directionToAnchor * velocityAwayFromAnchor;
                currentVelocity -= velocityToRemove;
                
                movementController.SetExternalVelocity(currentVelocity, Time.deltaTime * 2f, false);
            }
        }
    }
    
    void UpdateGrapplePhysics()
    {
        Vector3 currentVelocity = movementController.Velocity;
        Vector3 totalConstraintForce = Vector3.zero;
        Vector3 totalCentripetalForce = Vector3.zero;
        Vector3 totalInputForce = Vector3.zero;
        Vector3 totalOrbitalForce = Vector3.zero;
        
        // Process each active rope independently and accumulate forces
        if (leftRope.isActive)
        {
            ProcessRopePhysics(leftRope, ref currentVelocity, ref totalConstraintForce, ref totalCentripetalForce, ref totalInputForce, ref totalOrbitalForce);
        }
        
        if (rightRope.isActive)
        {
            ProcessRopePhysics(rightRope, ref currentVelocity, ref totalConstraintForce, ref totalCentripetalForce, ref totalInputForce, ref totalOrbitalForce);
        }
        
        // Apply combined forces
        currentVelocity += totalCentripetalForce + totalInputForce + totalOrbitalForce;
        movementController.SetExternalVelocity(currentVelocity, Time.deltaTime * 2f, false);
        
        // Update per-rope state
        if (leftRope.isActive) leftRope.lastFrameVelocity = currentVelocity;
        if (rightRope.isActive) rightRope.lastFrameVelocity = currentVelocity;
    }
    
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
            float shortenAmount = reelSpeed * Time.deltaTime;
            rope.ropeLength = Mathf.Max(rope.ropeLength - shortenAmount, minRopeLength);
        }
        
        // === ROPE CONSTRAINT ===
        if (currentDistance > rope.ropeLength)
        {
            float overshoot = currentDistance - rope.ropeLength;
            Vector3 correction = directionToAnchor * (overshoot * ropeConstraintStiffness);
            characterController.Move(correction);
            
            float velocityAwayFromAnchor = Vector3.Dot(currentVelocity, -directionToAnchor);
            if (velocityAwayFromAnchor > 0f)
            {
                currentVelocity -= (-directionToAnchor) * velocityAwayFromAnchor * (1f - constraintViolationDamping);
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
            
            if (tangentialSpeed > 100f)
            {
                float centripetalForce = (tangentialSpeed * tangentialSpeed) / currentDistance;
                Vector3 centripetalAcceleration = directionToAnchor * centripetalForce * centripetalForceMultiplier * Time.deltaTime;
                totalCentripetalForce += centripetalAcceleration;
            }
        }
        
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
                Debug.DrawRay(transform.position, orbitalAcceleration * 10f, Color.cyan);
            }
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
                Debug.DrawRay(rope.anchor, rope.anchorVelocity.normalized * 100f, Color.magenta);
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
            rope.ropeLineInstance = Instantiate(ropeLinePrefab, transform.position, Quaternion.identity);
            rope.lineRenderer = rope.ropeLineInstance.GetComponent<LineRenderer>();
            
            if (rope.lineRenderer != null)
            {
                rope.lineRenderer.positionCount = 2;
                rope.lineRenderer.useWorldSpace = true;
                rope.lineRenderer.startWidth = ropeWidth;
                rope.lineRenderer.endWidth = ropeWidth;
            }
        }
    }
    
    void UpdateVisuals(RopeState rope, Transform emitPoint)
    {
        if (rope.lineRenderer != null)
        {
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
