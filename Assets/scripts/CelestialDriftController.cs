// --- CelestialDriftController.cs (HERO REFACTOR - FINAL ENGINE VERSION - CORRECTED) ---
using UnityEngine;
using GeminiGauntlet.Audio;

[RequireComponent(typeof(Rigidbody))]
public class CelestialDriftController : MonoBehaviour
{
    // ... (All of your existing [Header] variables at the top remain exactly the same)
    #region PublicVariables
    [Header("Flight Dynamics & Speed")]
    [SerializeField] private float mainThrustForce = 50f;
    [SerializeField] private float maneuveringThrustForce = 35f;
    [SerializeField, Tooltip("Force applied to lift off from a platform.")]
    public float liftoffBoost = 8f;
    [SerializeField] private float maxSpeed = 75f;

    [Header("Boost Mechanic")]
    [SerializeField] private float boostMultiplier = 2.5f;
    [SerializeField, Tooltip("How quickly boost power reaches full (units per second)")]
    private float boostRampUpRate = 2f;
    [SerializeField, Tooltip("How quickly boost power fades once released (units per second)")]
    private float boostRampDownRate = 4;

    private float boostBlend = 0f;
    [SerializeField] private float maxBoostEnergy = 100f;
    [SerializeField] private float boostDrainRate = 30f;
    [SerializeField] private float boostRegenRate = 20f;
    [SerializeField] private float boostRegenDelay = 1.0f;
    public float currentBoostEnergy { get; private set; }
    private float timeSinceLastBoost = 0f;

    [Header("Handling & Control")]
    // Removed unused linearDrag field
    [SerializeField, Tooltip("How quickly player rotates to face input direction.")]
    private float lookRotationSpeed = 2f;
    [SerializeField] private float rollSpeed = 45f;
    
    [Header("Roll Lock (X to toggle)")]
    [SerializeField, Tooltip("Keyboard key to toggle roll on/off")] private KeyCode rollToggleKey = KeyCode.X;
    [SerializeField, Tooltip("If false, roll input is ignored and the player is smoothly uprighted.")] private bool isRollEnabled = true;
    [SerializeField, Tooltip("How quickly to align to world up when roll is disabled")] private float rollLockUprightSpeed = 6f;
    [SerializeField, Tooltip("Angle threshold (degrees) to finish uprighting")] private float uprightAngleThreshold = 0.5f;
    
    private bool isUprightingToWorldUp = false;
    private Quaternion targetUprightRotation;
    
    [Header("Camera Reference")]
    [SerializeField] private Camera playerCamera;
    // FOV is now controlled exclusively by AAACameraController
    
    [Header("Core Flight State")]
    public bool isFlightUnlocked = true;
    
    [Header("Flight System")]
    private bool isInFlightMode = true;
    
    [Header("Gravity Settings")]
    [SerializeField, Tooltip("Only applies in flight mode - platforms use Unity gravity")]
    public float flightGravityMultiplier = 0.5f;
    [SerializeField, Tooltip("Heavy gravity multiplier when falling to platforms")]
    public float fallingGravityMultiplier = 25f;
    [SerializeField, Tooltip("How fast player uprights during falling")]
    public float fallingUprightSpeed = 5f;
    
    [Header("Falling State")]
    public bool isFallingToPlatform = false; // Public so AAAMovementIntegrator can set it
    
    [Header("Flight Sound System")]
    [Tooltip("Enable flight sound effects")]
    public bool enableFlightSounds = true;
    [Tooltip("Volume for flight loop sound (constant)")]
    [Range(0f, 1f)] public float flightSoundVolume = 0.8f;
    [Tooltip("Time to reach maximum pitch when boosting (seconds)")]
    [Range(1f, 5f)] public float boostPitchRampTime = 2.5f;
    [Tooltip("Maximum pitch when boost is held for full duration")]
    [Range(1f, 20f)] public float maxBoostPitch = 10f;
    [Tooltip("Time for pitch to roll off back to 1.0 when boost is released (seconds)")]
    [Range(0.5f, 3f)] public float pitchRollOffTime = 1f;
    [Tooltip("Volume for vertical movement sound (constant)")]
    [Range(0f, 1f)] public float verticalMovementSoundVolume = 0.6f;
    #endregion

    // Flight sound state
    private SoundHandle currentFlightLoopHandle;
    private bool isFlightLoopPlaying = false;
    private bool wasInFlightMode = false;
    private float lastFlightSoundUpdate = 0f;
    private float lastSetVolume = 0f;
    private const float FLIGHT_SOUND_UPDATE_INTERVAL = 0.1f; // More frequent updates for smooth pitch changes
    
    // Boost sound tracking
    private float boostStartTime = 0f;
    private bool wasBoostingForSound = false;
    
    // Pitch roll-off tracking
    private bool isPitchRollingOff = false;
    private float rollOffStartTime = 0f;
    private float rollOffStartPitch = 1f;
    
    // Vertical movement sound variables
    private SoundHandle currentVerticalSoundHandle;
    private bool isVerticalSoundPlaying = false;
    private bool wasVerticalMovementActive = false;
    
    // Volume fade variables for vertical movement sound
    private bool isVerticalVolumeFadingIn = false;
    private bool isVerticalVolumeFadingOut = false;
    private float verticalVolumeFadeStartTime;
    private float verticalVolumeFadeStartVolume;
    [SerializeField] private float verticalVolumeFadeTime = 0.2f; // Quick fade to prevent clicking
    
    private Rigidbody rb;
    private Vector3 moveInput;
    private Vector3 lookInput;
    private float rollInput;
    
    // Thread-safe input copies for FixedUpdate
    private Vector3 safeMoveCopy;
    private Vector3 safeLookCopy;
    private float safeRollCopy;
    
    private bool isBoosting;
    private bool wasBoostingLastFrame;
    private bool isApplyingThrust;
    private bool wasApplyingThrustLastFrame;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Simple FPS setup - Unity handles gravity normally
        rb.useGravity = true;
        rb.linearDamping = 2f; // Basic air resistance
        currentBoostEnergy = maxBoostEnergy;

        if (playerCamera == null) playerCamera = Camera.main;
        // FOV is now controlled exclusively by AAACameraController
        
        // Start in flight mode
        isInFlightMode = true;
    }

    // This method is called by the active state in its Update() loop.
    public void ManualUpdate(Vector3 move, Vector3 look, float roll, bool boost)
    {
        // Store inputs safely to prevent race conditions
        moveInput = move;
        lookInput = look;
        rollInput = roll;
        
        // Create thread-safe copies for FixedUpdate to prevent input swapping
        safeMoveCopy = move;
        safeLookCopy = look;
        safeRollCopy = roll;
        
        // Input validation to prevent corruption
        if (float.IsNaN(safeLookCopy.x) || float.IsNaN(safeLookCopy.y) || float.IsNaN(safeRollCopy))
        {
            Debug.LogWarning("[INPUT FIX] Detected NaN in input values, resetting to zero");
            safeLookCopy = Vector3.zero;
            safeRollCopy = 0f;
        }
        
        isBoosting = boost && currentBoostEnergy > 0;
        isApplyingThrust = safeMoveCopy.magnitude > 0.1f;

        UpdateBoostBlend();
        HandleBoostEnergy();
        HandleCameraFOV();
        HandleFlightSounds();
        
        // Handle vertical movement sound (Space/LeftControl during CelestialDrift)
        bool isCelestialDriftActive = isInFlightMode && isFlightUnlocked;
        AAAMovementIntegrator aaaIntegrator = GetComponent<AAAMovementIntegrator>();
        if (aaaIntegrator != null && aaaIntegrator.useAAAMovement)
        {
            isCelestialDriftActive = false;
        }
        HandleVerticalMovementSound(isCelestialDriftActive);

        // Roll toggle (X): disable/enable roll and upright smoothly when disabling
        // Only process when Celestial Drift is active (not AAA ground mode)
        if (Input.GetKeyDown(rollToggleKey) && isFlightUnlocked)
        {
            bool isInAAAMode = aaaIntegrator != null && aaaIntegrator.useAAAMovement;
            if (!isInAAAMode)
            {
                ToggleRollEnabled();
                if (!isRollEnabled)
                {
                    StartUprightToWorldUp();
                }
            }
        }
    }
    
    // This method is called by the active state in its FixedUpdate() loop.
    public void ManualFixedUpdate()
    {
        ApplyThrust();
        ApplyBubbleGravity();
        ApplyRotation();
        
        // Handle upright alignment ONLY during controlled falling transitions
        // NOT during normal flight - player needs full manual control!
        bool shouldUprightWhileFalling = isFallingToPlatform;
        
        if (shouldUprightWhileFalling && isRollEnabled)
        {
            ApplyFallingUpright();
        }
        
        // One-time upright when requested
        if (isUprightingToWorldUp)
        {
            ApplyRollLockUpright();
        }

        // While roll is disabled (but not uprighting), suppress only roll angular velocity
        if (!isRollEnabled && !isUprightingToWorldUp)
        {
            SuppressRollAngularVelocity();
        }
        
        ClampVelocity();
    }

    private void ApplyThrust()
    {
        AAAMovementIntegrator aaaIntegrator = GetComponent<AAAMovementIntegrator>();
        if (aaaIntegrator != null && aaaIntegrator.useAAAMovement)
        {
            // EXPERT FIX: Block flight during AAA mode OR falling state!
            // Debug.Log("<color=red>🛑 FLIGHT BLOCKED: AAA/Falling mode active, no thrust applied</color>");
            return; // Exit early - no flight forces when using AAA movement OR falling!
        }
        
        float boostFactor = Mathf.Lerp(1f, boostMultiplier, boostBlend);
        float currentThrustForce = mainThrustForce * boostFactor;
        float currentManeuveringForce = maneuveringThrustForce * boostFactor;
        
        if (Mathf.Abs(moveInput.z) > 0.1f) rb.AddForce(transform.forward * moveInput.z * currentThrustForce, ForceMode.Acceleration);
        if (Mathf.Abs(moveInput.x) > 0.1f) rb.AddForce(transform.right * moveInput.x * currentManeuveringForce, ForceMode.Acceleration);
        if (Mathf.Abs(moveInput.y) > 0.1f) rb.AddForce(transform.up * moveInput.y * currentManeuveringForce, ForceMode.Acceleration);
    }
    

    
    private void ApplyBubbleGravity()
    {
        // Check if we're locked onto a platform - if so, disable gravity to allow perfect vertical following
        PlayerMovementManager movementManager = GetComponent<PlayerMovementManager>();
        if (movementManager != null && movementManager.IsLockedOn)
        {
            // When locked onto a platform, disable gravity to prevent interference with vertical platform following
            // The platform following system will handle all vertical movement
            return;
        }
        
        // Apply custom gravity in flight mode - heavy gravity when falling to platform
        float gravityMultiplier = isFallingToPlatform ? fallingGravityMultiplier : flightGravityMultiplier;
        rb.AddForce(Physics.gravity * gravityMultiplier, ForceMode.Acceleration);
        
        if (isFallingToPlatform && rb.linearVelocity.y < -1f)
        {
            Debug.Log($"<color=orange>🍂 CELESTIAL FALLING: Velocity Y = {rb.linearVelocity.y:F1}, Gravity = {gravityMultiplier}x</color>");
        }
    }
    
    private void ApplyFallingUpright()
    {
        // Determine target up: locked platform up if locked, otherwise WORLD up
        PlayerMovementManager pmm = FindObjectOfType<PlayerMovementManager>();
        Vector3 targetUp = Vector3.up;
        string targetName = "WORLD";

        if (pmm != null && pmm.IsLockedOn && pmm.LockedPlatform != null)
        {
            targetUp = pmm.LockedPlatform.transform.up;
            targetName = pmm.LockedPlatform.name;
        }

        // Current up direction
        Vector3 currentUp = transform.up;

        // Calculate rotation needed to align up vectors
        Quaternion targetRotation = Quaternion.FromToRotation(currentUp, targetUp) * transform.rotation;

        // Apply smooth rotation towards upright
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, fallingUprightSpeed * Time.fixedDeltaTime);

        // Debug visualization
        if (Time.frameCount % 30 == 0) // Every 30 physics frames
        {
            if (targetName == "WORLD")
                Debug.Log("<color=cyan>⬆️ UPRIGHT FALLING: Aligning to WORLD up</color>");
            else
                Debug.Log($"<color=cyan>⬆️ UPRIGHT FALLING: Aligning to {targetName} up vector</color>");
        }
    }

    private void ApplyRotation()
    {
        // 🛑 CRITICAL FIX: Don't apply flight rotation when in AAA ground movement mode!
        // BUT ALLOW free camera look during falling states!
        AAAMovementIntegrator aaaIntegrator = GetComponent<AAAMovementIntegrator>();
        if (aaaIntegrator != null && aaaIntegrator.useAAAMovement)
        {
            // EXPERT FIX: Block flight rotation ONLY during AAA ground mode - NOT during falling!
            // Debug.Log("<color=red>🛑 FLIGHT ROTATION BLOCKED: AAA ground mode active</color>");
            return; // Exit early - no flight rotation when using AAA ground movement!
        }
        
        // ✅ FALLING STATE FIX: Allow free camera look while falling!
        // When falling, player should maintain full camera control for better experience
        
        float currentLookSpeed = lookRotationSpeed;
        float currentRollSpeed = rollSpeed;

        // BULLETPROOF INPUT HANDLING - Read fresh input directly to prevent corruption
        // Get fresh mouse input directly from Input system to avoid variable corruption
        float freshMouseX = Input.GetAxis("Mouse X");
        float freshMouseY = Input.GetAxis("Mouse Y");
        
        // Get fresh roll input directly from Input system
        float freshRollInput = 0f;
        if (Input.GetKey(KeyCode.Q)) freshRollInput = -1f;
        if (Input.GetKey(KeyCode.E)) freshRollInput = 1f;
        
        // PERMANENT FIX: Use fresh input values instead of potentially corrupted stored values
        // This prevents the mouse/roll swapping bug that occurs after playing for a while
        
        // Apply rotation with GUARANTEED correct axis assignments
        float pitchTorque = -freshMouseY * currentLookSpeed;     // Mouse Y -> Pitch (X-axis) - ALWAYS
        float yawTorque = freshMouseX * currentLookSpeed;        // Mouse X -> Yaw (Y-axis) - ALWAYS  
        float rollTorque = isRollEnabled ? (-freshRollInput * currentRollSpeed) : 0f; // Q/E -> Roll (Z-axis) - ALWAYS
        
        // DEBUG: Log when significant input detected to verify correct assignments
        if (Mathf.Abs(freshRollInput) > 0.1f || Mathf.Abs(freshMouseX) > 0.1f || Mathf.Abs(freshMouseY) > 0.1f)
        {
            Debug.Log($"<color=lime>[INPUT FIXED] Mouse: ({freshMouseX:F2}, {freshMouseY:F2}) -> Yaw/Pitch, Roll: {freshRollInput:F2} -> Roll</color>");
        }
        
        Vector3 rotTorque = new Vector3(pitchTorque, yawTorque, rollTorque);
        rb.AddRelativeTorque(rotTorque, ForceMode.VelocityChange);
    }

    // === ROLL LOCK/Upright Helpers ===
    /// <summary>
    /// Toggle roll enabled/disabled.
    /// </summary>
    public void ToggleRollEnabled()
    {
        SetRollEnabled(!isRollEnabled);
    }

    /// <summary>
    /// Enable or disable roll. When disabling, does not automatically start uprighting;
    /// call StartUprightToWorldUp() if you want to upright immediately.
    /// </summary>
    public void SetRollEnabled(bool enabled)
    {
        isRollEnabled = enabled;
        if (enabled)
        {
            // Cancel any in-progress uprighting when roll is re-enabled
            isUprightingToWorldUp = false;
        }
        Debug.Log($"[CelestialDrift] Roll {(isRollEnabled ? "ENABLED" : "DISABLED")} (key {rollToggleKey})");
    }

    /// <summary>
    /// Compute and start a smooth upright alignment toward world up while preserving yaw.
    /// </summary>
    private void StartUprightToWorldUp()
    {
        // Instant upright: preserve current forward (yaw & pitch), zero only roll
        targetUprightRotation = ComputeUprightRotationPreserveYaw();
        transform.rotation = targetUprightRotation;
        isUprightingToWorldUp = false;

        // Clear roll spin only; do not touch yaw/pitch momentum
        if (rb != null)
        {
            Vector3 localAngVel = transform.InverseTransformDirection(rb.angularVelocity);
            localAngVel.z = 0f;
            rb.angularVelocity = transform.TransformDirection(localAngVel);
        }
    }

    /// <summary>
    /// One-time smooth upright toward a precomputed target, then stop.
    /// Preserves full yaw/pitch control after completion.
    /// </summary>
    private void ApplyRollLockUpright()
    {
        // Rotate smoothly toward the precomputed upright target
        Quaternion current = transform.rotation;
        Quaternion next = Quaternion.Slerp(current, targetUprightRotation, rollLockUprightSpeed * Time.fixedDeltaTime);
        transform.rotation = next;

        // Stop when close enough
        float angle = Quaternion.Angle(next, targetUprightRotation);
        if (angle <= uprightAngleThreshold)
        {
            transform.rotation = targetUprightRotation;
            isUprightingToWorldUp = false;

            // Remove only the roll component of angular velocity, preserve yaw/pitch momentum
            Vector3 localAngVel = transform.InverseTransformDirection(rb.angularVelocity);
            localAngVel.z = 0f; // Z is roll in local space
            rb.angularVelocity = transform.TransformDirection(localAngVel);
        }
    }

    /// <summary>
    /// While roll is disabled and not uprighting, kill roll spin without affecting yaw/pitch.
    /// </summary>
    private void SuppressRollAngularVelocity()
    {
        if (rb == null) return;
        Vector3 localAngVel = transform.InverseTransformDirection(rb.angularVelocity);
        if (Mathf.Abs(localAngVel.z) > 0.0001f)
        {
            localAngVel.z = 0f;
            rb.angularVelocity = transform.TransformDirection(localAngVel);
        }
    }

    /// <summary>
    /// Build a rotation that preserves current forward (yaw & pitch) and zeroes only roll relative to world up.
    /// </summary>
    private Quaternion ComputeUprightRotationPreserveYaw()
    {
        Vector3 forward = transform.forward;
        Vector3 worldUp = Vector3.up;
        Vector3 desiredUp = Vector3.ProjectOnPlane(worldUp, forward);
        if (desiredUp.sqrMagnitude < 1e-6f)
        {
            // Forward nearly parallel to world up: roll undefined, keep current rotation
            return transform.rotation;
        }
        desiredUp.Normalize();
        return Quaternion.LookRotation(forward, desiredUp);
    }

    // Build a rotation that zeroes pitch and roll, preserving only yaw relative to world up
    private Quaternion ComputeYawOnlyWorldAlignedRotation()
    {
        Vector3 forwardHoriz = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (forwardHoriz.sqrMagnitude < 1e-6f)
        {
            forwardHoriz = Vector3.ProjectOnPlane(transform.right, Vector3.up);
            if (forwardHoriz.sqrMagnitude < 1e-6f)
            {
                forwardHoriz = Vector3.forward;
            }
        }
        forwardHoriz.Normalize();
        return Quaternion.LookRotation(forwardHoriz, Vector3.up);
    }

    /// <summary>
    /// Fully resets Celestial Drift state to a clean, known-good baseline.
    /// Call this whenever (re)activating flight to prevent axis/roll corruption.
    /// </summary>
    public void ResetFlightState(bool hardOrientationReset = true)
    {
        // Zero input buffers
        moveInput = Vector3.zero;
        lookInput = Vector3.zero;
        rollInput = 0f;
        safeMoveCopy = Vector3.zero;
        safeLookCopy = Vector3.zero;
        safeRollCopy = 0f;

        // Reset flags and boost state
        isBoosting = false;
        wasBoostingLastFrame = false;
        isApplyingThrust = false;
        wasApplyingThrustLastFrame = false;
        boostBlend = 0f;
        timeSinceLastBoost = 0f;

        // Ensure roll control is enabled and no pending upright action
        isRollEnabled = true;
        isUprightingToWorldUp = false;

        // FOV is now controlled exclusively by AAACameraController

        // Exit any special falling mode
        isFallingToPlatform = false;

        // Reset rigidbody velocities
        if (rb != null)
        {
            rb.angularVelocity = Vector3.zero;
            rb.linearVelocity = Vector3.zero;
        }

        // Reset orientation to world-aligned yaw only (no pitch/roll)
        if (hardOrientationReset)
        {
            transform.rotation = ComputeYawOnlyWorldAlignedRotation();
        }

        // Mark as in flight mode
        isInFlightMode = true;

        Debug.Log("<color=lime>♻️ CelestialDrift: Flight state RESET (fresh instance)</color>");
    }

    private void UpdateBoostBlend()
    {
        float target = (isBoosting && currentBoostEnergy > 0f) ? 1f : 0f;
        float rate = target > boostBlend ? boostRampUpRate : boostRampDownRate;
        boostBlend = Mathf.MoveTowards(boostBlend, target, rate * Time.deltaTime);
    }
    
    private void HandleBoostEnergy()
    {
        if (isBoosting)
        {
            currentBoostEnergy -= boostDrainRate * boostBlend * Time.deltaTime;
            timeSinceLastBoost = 0f;
        }
        else
        {
            timeSinceLastBoost += Time.deltaTime;
            if (timeSinceLastBoost >= boostRegenDelay)
            {
                currentBoostEnergy += boostRegenRate * Time.deltaTime;
            }
        }
        currentBoostEnergy = Mathf.Clamp(currentBoostEnergy, 0, maxBoostEnergy);
    }

    // --- Public Utility Methods for States ---

    // FIX 2: Add this method back so other scripts can access the player's velocity.
    /// <summary>
    /// Gets the current velocity of the player's Rigidbody.
    /// </summary>
    /// <returns>The world-space velocity vector.</returns>
    public Vector3 GetVelocity()
    {
        if (rb == null) return Vector3.zero;
        return rb.linearVelocity;
    }

    private void ClampVelocity()
    {
        float currentMaxSpeed = isBoosting ? maxSpeed * boostMultiplier * 0.8f : maxSpeed;
        if (rb.linearVelocity.sqrMagnitude > currentMaxSpeed * currentMaxSpeed)
        {
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, rb.linearVelocity.normalized * currentMaxSpeed, Time.fixedDeltaTime * 2f);
        }
    }

    private void HandleCameraFOV()
    {
        // FOV is now controlled exclusively by AAACameraController
        // This method is kept for backward compatibility but does nothing
        // Flight boost FOV changes should be handled by AAACameraController detecting flight mode
    }
    
    // === FALLING STATE CONTROL METHODS ===
    /// <summary>
    /// Enable heavy gravity falling - called by AAAMovementIntegrator when transitioning from flight
    /// </summary>
    public void StartFallingToPlatform()
    {
        isFallingToPlatform = true;
        Debug.Log("<color=orange>🍂 CELESTIAL FALLING STARTED - Heavy gravity enabled</color>");
    }
    
    /// <summary>
    /// Disable falling mode and return to normal flight gravity
    /// </summary>
    public void StopFallingToPlatform()
    {
        isFallingToPlatform = false;
        Debug.Log("<color=cyan>✈️ CELESTIAL FALLING ENDED - Normal flight gravity restored</color>");
    }
    
    /// <summary>
    /// Check if we've landed on a platform during falling - called by AAAMovementIntegrator
    /// </summary>
    public bool HasLandedOnPlatform()
    {
        if (!isFallingToPlatform) return false;
        
        // Check if we're close to a surface and moving slowly downward
        bool isNearGround = Physics.Raycast(transform.position, Vector3.down, 2f, LayerMask.GetMask("Default"));
        bool hasSlowDownwardVelocity = rb.linearVelocity.y > -5f && rb.linearVelocity.y < 1f;
        
        if (isNearGround && hasSlowDownwardVelocity)
        {
            Debug.Log($"<color=green>🎯 CELESTIAL LANDING DETECTED: Ground={isNearGround}, VelY={rb.linearVelocity.y:F1}</color>");
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Handle flight sound effects based on current flight state and boost duration
    /// Only plays when CelestialDrift is specifically active (not AAA movement mode)
    /// </summary>
    private void HandleFlightSounds()
    {
        if (!enableFlightSounds) return;
        
        // Check if CelestialDrift is specifically active (flight mode AND not AAA movement)
        bool isCelestialDriftActive = isInFlightMode && isFlightUnlocked;
        
        // Block flight sounds when in AAA movement mode
        AAAMovementIntegrator aaaIntegrator = GetComponent<AAAMovementIntegrator>();
        if (aaaIntegrator != null && aaaIntegrator.useAAAMovement)
        {
            isCelestialDriftActive = false;
        }
        
        bool isCurrentlyInFlightMode = isCelestialDriftActive;
        
        // Handle flight activation sound (play once when entering flight mode)
        if (isCurrentlyInFlightMode && !wasInFlightMode)
        {
            OnFlightActivated();
        }
        
        // Handle flight deactivation
        if (!isCurrentlyInFlightMode && wasInFlightMode)
        {
            OnFlightDeactivated();
        }
        
        // Handle flight loop sound based on boost state (only when in flight mode)
        if (isCurrentlyInFlightMode)
        {
            // Start flight sound when entering flight mode (if not already playing)
            if (!isFlightLoopPlaying)
            {
                StartFlightLoop();
            }
            
            // Track boost state changes
            if (isBoosting && !wasBoostingForSound)
            {
                // Boost just started - handle smooth transition from roll-off
                float currentPitch = 1f;
                if (isPitchRollingOff && currentFlightLoopHandle.IsValid)
                {
                    // Get current pitch during roll-off for smooth transition
                    currentPitch = currentFlightLoopHandle.GetPitch();
                    Debug.Log($"🔄 Boost reactivated during roll-off - continuing from pitch {currentPitch:F2}");
                }
                
                // Calculate equivalent boost duration for current pitch
                float normalizedPitch = Mathf.InverseLerp(1f, maxBoostPitch, currentPitch);
                float equivalentBoostDuration = normalizedPitch * boostPitchRampTime;
                
                // Set boost start time to create seamless transition
                boostStartTime = Time.time - equivalentBoostDuration;
                wasBoostingForSound = true;
                isPitchRollingOff = false; // Cancel any ongoing roll-off
                
                Debug.Log($"🚀 Boost started - Current pitch: {currentPitch:F2}, Equivalent duration: {equivalentBoostDuration:F1}s");
            }
            else if (!isBoosting && wasBoostingForSound)
            {
                // Boost just ended - start pitch roll-off instead of stopping immediately
                wasBoostingForSound = false;
                if (isFlightLoopPlaying)
                {
                    StartPitchRollOff();
                }
            }
            
            // Update pitch while boosting
            if (isBoosting && isFlightLoopPlaying)
            {
                if (Time.time - lastFlightSoundUpdate >= FLIGHT_SOUND_UPDATE_INTERVAL)
                {
                    UpdateFlightLoopPitch();
                }
            }
            
            // Update pitch roll-off when not boosting but rolling off
            if (!isBoosting && isPitchRollingOff && isFlightLoopPlaying)
            {
                if (Time.time - lastFlightSoundUpdate >= FLIGHT_SOUND_UPDATE_INTERVAL)
                {
                    UpdatePitchRollOff();
                }
            }
            
            // Handle vertical movement sound (Space/LeftControl)
            HandleVerticalMovementSound(isCelestialDriftActive);
        }
        else if (isFlightLoopPlaying)
        {
            // Stop flight sound when exiting flight mode
            StopFlightLoop();
        }
        
        // Stop vertical movement sound when not in CelestialDrift
        if (!isCelestialDriftActive && isVerticalSoundPlaying)
        {
            StopVerticalMovementSound();
        }
        
        wasInFlightMode = isCurrentlyInFlightMode;
    }
    
    /// <summary>
    /// Called when flight mode is activated
    /// </summary>
    private void OnFlightActivated()
    {
        GameSounds.PlayFlightActivation(transform.position);
        Debug.Log("🚀 Flight activated - playing activation sound");
    }
    
    /// <summary>
    /// Called when flight mode is deactivated
    /// </summary>
    private void OnFlightDeactivated()
    {
        if (isFlightLoopPlaying)
        {
            StopFlightLoop();
        }
        Debug.Log("🛬 Flight deactivated");
    }
    
    /// <summary>
    /// Start the flight loop sound with constant volume and base pitch
    /// </summary>
    private void StartFlightLoop()
    {
        if (isFlightLoopPlaying) return;
        
        try
        {
            currentFlightLoopHandle = GameSounds.StartFlightLoop(transform, flightSoundVolume);
            
            if (currentFlightLoopHandle.IsValid)
            {
                isFlightLoopPlaying = true;
                lastFlightSoundUpdate = Time.time;
                lastSetVolume = flightSoundVolume;
                Debug.Log($"🌪️ Flight loop started - Boost Duration: 0s, Volume: {flightSoundVolume:F2}, Pitch: 1.0");
            }
            else
            {
                Debug.LogWarning("Failed to start flight loop sound - invalid handle returned");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception in StartFlightLoop: {e.Message}");
        }
    }

    /// <summary>
    /// Update the pitch of the flight loop based on boost duration
    /// </summary>
    private void UpdateFlightLoopPitch()
    {
        if (!isFlightLoopPlaying || !isBoosting) return;
        
        try
        {
            float boostDuration = Time.time - boostStartTime;
            float newPitch = CalculateBoostPitch(boostDuration);
            
            // Directly set pitch on existing sound handle (no restart needed!)
            if (currentFlightLoopHandle.IsValid)
            {
                currentFlightLoopHandle.SetPitch(newPitch);
                lastFlightSoundUpdate = Time.time;
                Debug.Log($"🔄 Flight pitch updated - Boost Duration: {boostDuration:F1}s, Pitch: {newPitch:F2}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception in UpdateFlightLoopPitch: {e.Message}");
        }
    }
    
    /// <summary>
    /// Calculate pitch based on boost duration (1.0 to maxBoostPitch over boostPitchRampTime)
    /// </summary>
    private float CalculateBoostPitch(float boostDuration)
    {
        float normalizedDuration = Mathf.Clamp01(boostDuration / boostPitchRampTime);
        return Mathf.Lerp(1f, maxBoostPitch, normalizedDuration);
    }
    
    /// <summary>
    /// Start the pitch roll-off process when boost is released
    /// </summary>
    private void StartPitchRollOff()
    {
        if (!isFlightLoopPlaying) return;
        
        try
        {
            // Get current pitch and start roll-off
            rollOffStartPitch = currentFlightLoopHandle.IsValid ? currentFlightLoopHandle.GetPitch() : 1f;
            rollOffStartTime = Time.time;
            isPitchRollingOff = true;
            
            Debug.Log($"🔽 Starting pitch roll-off from {rollOffStartPitch:F2} to 1.0 over {pitchRollOffTime}s");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception in StartPitchRollOff: {e.Message}");
        }
    }
    
    /// <summary>
    /// Update pitch during roll-off phase
    /// </summary>
    private void UpdatePitchRollOff()
    {
        if (!isPitchRollingOff || !isFlightLoopPlaying) return;
        
        try
        {
            float rollOffDuration = Time.time - rollOffStartTime;
            float normalizedRollOff = Mathf.Clamp01(rollOffDuration / pitchRollOffTime);
            float currentPitch = Mathf.Lerp(rollOffStartPitch, 1f, normalizedRollOff);
            
            if (currentFlightLoopHandle.IsValid)
            {
                currentFlightLoopHandle.SetPitch(currentPitch);
                lastFlightSoundUpdate = Time.time;
                Debug.Log($"🔽 Pitch rolling off - Duration: {rollOffDuration:F1}s, Pitch: {currentPitch:F2}");
            }
            
            // Check if roll-off is complete
            if (normalizedRollOff >= 1f)
            {
                isPitchRollingOff = false;
                // Set final pitch to exactly 1.0 and continue playing
                if (currentFlightLoopHandle.IsValid)
                {
                    currentFlightLoopHandle.SetPitch(1f);
                }
                Debug.Log($"✅ Pitch roll-off complete - continuing at pitch 1.0");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception in UpdatePitchRollOff: {e.Message}");
        }
    }
    
    /// <summary>
    /// Update volume fade for vertical movement sound to prevent clicking
    /// </summary>
    private void UpdateVerticalVolumeFade()
    {
        if (!isVerticalSoundPlaying || !currentVerticalSoundHandle.IsValid) return;
        
        try
        {
            if (isVerticalVolumeFadingIn)
            {
                float fadeProgress = (Time.time - verticalVolumeFadeStartTime) / verticalVolumeFadeTime;
                if (fadeProgress >= 1f)
                {
                    // Fade in complete
                    currentVerticalSoundHandle.SetVolume(verticalMovementSoundVolume);
                    isVerticalVolumeFadingIn = false;
                    Debug.Log($"🔊 Vertical sound fade-in complete - Volume: {verticalMovementSoundVolume:F2}");
                }
                else
                {
                    // Continue fading in
                    float currentVolume = Mathf.Lerp(0f, verticalMovementSoundVolume, fadeProgress);
                    currentVerticalSoundHandle.SetVolume(currentVolume);
                }
            }
            else if (isVerticalVolumeFadingOut)
            {
                float fadeProgress = (Time.time - verticalVolumeFadeStartTime) / verticalVolumeFadeTime;
                if (fadeProgress >= 1f)
                {
                    // Fade out complete - stop the sound
                    currentVerticalSoundHandle.Stop();
                    isVerticalVolumeFadingOut = false;
                    isVerticalSoundPlaying = false;
                    Debug.Log("🔇 Vertical sound fade-out complete - Sound stopped");
                }
                else
                {
                    // Continue fading out
                    float currentVolume = Mathf.Lerp(verticalVolumeFadeStartVolume, 0f, fadeProgress);
                    currentVerticalSoundHandle.SetVolume(currentVolume);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception in UpdateVerticalVolumeFade: {e.Message}");
        }
    }
    
    /// <summary>
    /// Handle vertical movement sound (Space/LeftControl keys) during CelestialDrift
    /// </summary>
    private void HandleVerticalMovementSound(bool isCelestialDriftActive)
    {
        if (!isCelestialDriftActive) return;
        
        // Update volume fade transitions
        UpdateVerticalVolumeFade();
        
        // Check if vertical movement is active (Space or LeftControl pressed)
        bool isVerticalMovementActive = Mathf.Abs(moveInput.y) > 0.1f;
        
        // Track vertical movement state changes
        if (isVerticalMovementActive && !wasVerticalMovementActive)
        {
            // Vertical movement just started
            StartVerticalMovementSound();
        }
        else if (!isVerticalMovementActive && wasVerticalMovementActive)
        {
            // Vertical movement just ended
            StopVerticalMovementSound();
        }
        
        wasVerticalMovementActive = isVerticalMovementActive;
    }
    
    /// <summary>
    /// Start the vertical movement sound loop with volume fade-in
    /// </summary>
    private void StartVerticalMovementSound()
    {
        if (isVerticalSoundPlaying) return;
        
        try
        {
            // Start sound at zero volume to prevent clicking
            currentVerticalSoundHandle = GameSounds.StartVerticalMovementLoop(transform, 0f);
            
            if (currentVerticalSoundHandle.IsValid)
            {
                isVerticalSoundPlaying = true;
                
                // Start volume fade-in
                isVerticalVolumeFadingIn = true;
                isVerticalVolumeFadingOut = false;
                verticalVolumeFadeStartTime = Time.time;
                verticalVolumeFadeStartVolume = 0f;
                
                Debug.Log($"⬆️⬇️ Vertical movement sound started with fade-in - Target Volume: {verticalMovementSoundVolume:F2}");
            }
            else
            {
                Debug.LogWarning("Failed to start vertical movement sound - invalid handle returned");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception in StartVerticalMovementSound: {e.Message}");
        }
    }
    
    /// <summary>
    /// Stop the vertical movement sound loop with volume fade-out
    /// </summary>
    private void StopVerticalMovementSound()
    {
        if (isVerticalSoundPlaying && currentVerticalSoundHandle.IsValid)
        {
            try
            {
                // If currently fading in, cancel it and start fade-out from current volume
                if (isVerticalVolumeFadingIn)
                {
                    float currentVolume = currentVerticalSoundHandle.GetVolume();
                    verticalVolumeFadeStartVolume = currentVolume;
                    isVerticalVolumeFadingIn = false;
                }
                else
                {
                    // Start fade-out from current volume
                    verticalVolumeFadeStartVolume = currentVerticalSoundHandle.GetVolume();
                }
                
                // Start volume fade-out
                isVerticalVolumeFadingOut = true;
                verticalVolumeFadeStartTime = Time.time;
                
                Debug.Log($"🔇 Vertical movement sound starting fade-out from volume: {verticalVolumeFadeStartVolume:F2}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Exception in StopVerticalMovementSound: {e.Message}");
                // Fallback to immediate stop if fade-out fails
                if (currentVerticalSoundHandle.IsValid)
                {
                    currentVerticalSoundHandle.Stop();
                }
                isVerticalSoundPlaying = false;
            }
        }
    }

    /// <summary>
    /// Stop the flight loop sound
    /// </summary>
    private void StopFlightLoop()
    {
        if (isFlightLoopPlaying)
        {
            try
            {
                if (currentFlightLoopHandle.IsValid)
                {
                    currentFlightLoopHandle.Stop();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Exception in StopFlightLoop: {e.Message}");
            }
            finally
            {
                isFlightLoopPlaying = false;
                Debug.Log("🔇 Flight loop stopped");
            }
        }
    }
    
    /// <summary>
    /// Public method to manually trigger flight activation sound (for integration)
    /// </summary>
    public void TriggerFlightActivation()
    {
        if (enableFlightSounds)
        {
            GameSounds.PlayFlightActivation(transform.position);
            Debug.Log("🚀 Flight activation triggered manually");
        }
    }
    
    /// <summary>
    /// Public method to check if flight loop is currently playing
    /// </summary>
    public bool IsFlightLoopPlaying => isFlightLoopPlaying;
    
    void OnDestroy()
    {
        if (isFlightLoopPlaying)
        {
            StopFlightLoop();
        }
    }
    
    void OnDisable()
    {
        if (isFlightLoopPlaying)
        {
            StopFlightLoop();
        }
    }
}