using UnityEngine;
using System.Collections;
using System.Reflection;
using GeminiGauntlet.Audio;

/// <summary>
/// AAA Movement System Integrator - Automatically sets up and configures all AAA movement components
/// This script handles the integration between your existing systems and the new AAA movement system
/// </summary>
public class AAAMovementIntegrator : MonoBehaviour
{
    // Coroutine to hide persistent UI message after delay
    private IEnumerator ClearPersistentMessageDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (CognitiveFeedManager.Instance != null)
        {
            CognitiveFeedManager.Instance.HidePersistentMessage();
        }

    }

    // Try to find a valid landing surface directly beneath the player's feet (generic colliders)
    private bool TryFindLandingSurface(out RaycastHit hit)
    {
        CharacterController cc = GetComponent<CharacterController>();
        float radius = Mathf.Max(0.1f, autoLandSphereRadius);
        Vector3 origin;
        if (cc != null)
        {
            float bottomOffset = cc.height * 0.5f - Mathf.Max(cc.radius, 0.01f);
            Vector3 bottomLocal = cc.center + Vector3.down * bottomOffset;
            Vector3 bottomWorld = transform.TransformPoint(bottomLocal);
            origin = bottomWorld + transform.up * (radius + 0.02f);
            radius = Mathf.Max(radius, cc.radius * 0.9f);
        }
        else
        {
            origin = transform.position + transform.up * (radius + 0.02f);
        }
        float maxDist = Mathf.Max(0.2f, autoLandCheckDistance);
        if (Physics.SphereCast(origin, radius, -transform.up, out hit, maxDist, autoLandLayerMask, QueryTriggerInteraction.Ignore))
        {
            float angle = Vector3.Angle(hit.normal, transform.up);
            if (angle <= maxAutoLandSlopeAngle)
                return true;
        }
        return false;
    }

    private void AutoDetectAndLand()
    {
        if (TryFindLandingSurface(out RaycastHit hit))
        {
            LandOnSurface(hit);
        }
    }
    
    // Note: IsAAASystemActive() is defined later in this file (returns useAAAMovement && isInitialized)

    private void LandOnSurface(RaycastHit hit)
    {
        // Align upright relative to surface normal while preserving current camera yaw
        Vector3 refForward = (playerCamera != null) ? playerCamera.transform.forward : transform.forward;
        AlignUprightToNormalPreserveYaw(hit.normal, refForward);
        // Snap bottom of controller to contact
        SnapBottomToContact(hit);

        // Enter AAA ground mode
        useAAAMovement = true;
        if (aaaMovement != null) aaaMovement.enabled = true;
        if (aaaCamera != null) aaaCamera.enabled = true;
        if (aaaAudio != null) aaaAudio.enabled = true;

        // Physics/state
        // Capture RB velocity before swapping physics to drive momentum bridge
        Vector3 rbVelocity = Vector3.zero;
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rbVelocity = rb.linearVelocity;
        }
        EnsureAAAModePhysics();
        
        // Apply landing momentum using the hit surface normal
        if (preserveLandingMomentum && aaaMovement != null)
        {
            Vector3 preservedHorizontal = Vector3.ProjectOnPlane(rbVelocity, hit.normal);
            if (preservedHorizontal.magnitude >= landingMomentumMinSpeed)
            {
                StartLandingMomentum(preservedHorizontal);
            }
        }

        // Ensure AAAMovementController is in Walking mode and grounded
        if (aaaMovement != null)
        {
            aaaMovement.SetMovementMode(AAAMovementController.MovementMode.Walking);
            aaaMovement.SetGroundedImmediate(true);
        }

        // CRITICAL FIX: Check if we landed on a moving platform and initialize tracking
        CelestialPlatform landedPlatform = hit.collider.GetComponent<CelestialPlatform>();
        if (landedPlatform == null)
        {
            landedPlatform = hit.collider.GetComponentInParent<CelestialPlatform>();
        }
        
        if (landedPlatform != null)
        {
            // Initialize platform movement tracking for first-time landing
            lastPlatform = landedPlatform;
            lastPlatformPosition = landedPlatform.transform.position;
            lastPlatformRotation = landedPlatform.transform.rotation;
            isTrackingPlatformMovement = true; // Enable platform movement tracking
            
            // Preserve lock-on state for platform interaction
            var pmm = GetComponent<PlayerMovementManager>();
            if (pmm != null)
            {
                pmm.RestoreLockOnState(landedPlatform);
            }
        }

        // Disable flight
        if (existingMovementManager != null) existingMovementManager.enabled = false;
        var drift = GetComponent<CelestialDriftController>();
        if (drift != null) drift.enabled = false;

        // Feedback
        if (CognitiveFeedManager.Instance != null && FlavorTextManager.Instance != null)
        {
            CognitiveFeedManager.Instance.ShowPersistentMessage(FlavorTextManager.Instance.GetWalkingActivateMessage());
            StartCoroutine(ClearPersistentMessageDelay(3f));
        }

        LogSystemStatus();
    }

    private void AlignUprightToNormal(Vector3 groundNormal)
    {
        Vector3 forwardProjected = Vector3.ProjectOnPlane(transform.forward, groundNormal);
        if (forwardProjected.sqrMagnitude < 1e-4f)
        {
            forwardProjected = Vector3.ProjectOnPlane(transform.up, groundNormal);
        }
        transform.rotation = Quaternion.LookRotation(forwardProjected.normalized, groundNormal);
    }

    // Preserve yaw by aligning using a reference world forward (typically camera forward)
    private void AlignUprightToNormalPreserveYaw(Vector3 groundNormal, Vector3 referenceWorldForward)
    {
        Vector3 forwardProjected = Vector3.ProjectOnPlane(referenceWorldForward, groundNormal);
        if (forwardProjected.sqrMagnitude < 1e-4f)
        {
            forwardProjected = Vector3.ProjectOnPlane(transform.forward, groundNormal);
        }
        transform.rotation = Quaternion.LookRotation(forwardProjected.normalized, groundNormal);
    }

    private void SnapBottomToContact(RaycastHit hit)
    {
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null)
        {
            float bottomOffset = cc.height * 0.5f - Mathf.Max(cc.radius, 0.01f);
            Vector3 bottomLocal = cc.center + Vector3.down * bottomOffset;
            Vector3 currentBottom = transform.TransformPoint(bottomLocal);
            Vector3 desiredBottom = hit.point + hit.normal * (Mathf.Max(cc.radius, 0.05f) + 0.02f);
            Vector3 delta = desiredBottom - currentBottom;
            transform.position += delta;
        }
        else
        {
            transform.position = hit.point + hit.normal * 0.1f;
        }
    }
    


    [Header("=== INTEGRATION SETTINGS ===")]
    [SerializeField] public bool useAAAMovement = false; // Start in FLIGHT mode by default
    [SerializeField] private bool replaceExistingMovement = true;
    [Header("Flight Collision Detection")]
    [Tooltip("Separate collider for platform detection during flight mode (when CharacterController is disabled)")]
    public SphereCollider flightTriggerCollider;
    [Tooltip("Radius for flight mode trigger collider")]
    public float flightTriggerRadius = 1.5f;
    
    [Header("Debug Settings")]
    [Tooltip("Enable debug logging for troubleshooting")]
    public bool enableDebugMode = false; // Disabled to remove on-screen debug info
    
    [Header("=== AUTO-LANDING SETTINGS ===")]
    [Tooltip("Automatically land on any collider when feet touch a valid surface during flight.")]
    public bool enableAutoLanding = true;
    [Tooltip("Max distance below feet to check for landing surfaces while in flight.")]
    public float autoLandCheckDistance = 2.0f;
    [Tooltip("Sphere radius used for landing detection casts.")]
    public float autoLandSphereRadius = 0.45f;
    [Tooltip("Maximum slope angle (degrees) considered valid for auto-landing.")]
    [Range(0f, 89f)] public float maxAutoLandSlopeAngle = 75f;
    [Tooltip("LayerMask for auto-landing surface detection.")]
    public LayerMask autoLandLayerMask = ~0;
    
    [Header("=== COMPONENT REFERENCES ===\n")]
    [SerializeField] private AAAMovementController aaaMovement;
    [SerializeField] private AAACameraController aaaCamera;
    [SerializeField] private AAAMovementAudioManager aaaAudio;
    
    [Header("=== EXISTING SYSTEM REFERENCES ===")]
    [SerializeField] private PlayerMovementManager existingMovementManager; // CRITICAL: This calls FlightState.Update()!
    [SerializeField] private SimpleCharacterMotor existingCharacterMotor;

    
    [Header("=== CAMERA SETUP ===")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform cameraParent;
    
    // State tracking
    private bool isInitialized = false;
    private bool wasGroundedLastFrame = false;
    // SIMPLIFIED: Only Flying (false) or AAA (true) modes - no falling state
    
    // Freefall integration
    [Header("=== FREEFALL INTEGRATION ===")]
    [Tooltip("Set by FreefallController - prevents F-key conflicts during freefall")]
    public bool isInFreefall = false;
    
    // BULLETPROOF COLLIDER MANAGEMENT
    private CharacterController pillCollider;
    private Rigidbody playerRigidbody;
    private bool lastColliderState = false;
    
    // Landing momentum preservation (reintroduced)
    [Header("=== LANDING MOMENTUM ===")]
    [Tooltip("Preserve horizontal momentum when switching from flight to walking.")]
    [SerializeField] private bool preserveLandingMomentum = true;
    [Tooltip("How long to keep the preserved horizontal momentum active (seconds).")]
    [SerializeField] private float landingMomentumDuration = 0.45f;
    [Tooltip("Minimum horizontal speed required to trigger momentum preservation.")]
    [SerializeField] private float landingMomentumMinSpeed = 0.2f;
    private Coroutine landingMomentumRoutine;
    
    // Platform movement tracking for CharacterController
    private Rigidbody existingRigidbody;
    private CelestialPlatform lastPlatform;
    private bool preserveLockOn = true; // Set to false if we don't want to preserve lock-on state
    private Vector3 lastPlatformPosition;
    private Quaternion lastPlatformRotation;
    private bool isTrackingPlatformMovement = false;
    
    // Simple falling physics
    private Vector3 fallingVelocity;
    private float gravity = -50f; // Heavy gravity for fast falling
    
    void Start()
    {
        // FORCE START IN CELESTIAL FLIGHT MODE - Override any Inspector settings
        useAAAMovement = false;
        
        // Initialize flight trigger collider for platform detection
        InitializeFlightTriggerCollider();
        
        InitializeAAASystem();
    }
    
    // (legacy inline InitializeFlightTriggerCollider removed; using robust version defined later in file)
    
    void Update()
    {
        if (isInitialized)
        {
            // Press F to toggle between AAA ground movement and Celestial Flight systems
            // But NOT during freefall - FreefallController handles F-key during freefall
            if (Input.GetKeyDown(KeyCode.F) && !isInFreefall)
            {
                ToggleAAASystem();
            }

            if (useAAAMovement)
            {
                HandleSystemIntegration();
            }
            else
            {
                // FLYING MODE: auto-landing on ANY collider
                if (enableAutoLanding)
                {
                    AutoDetectAndLand();
                }
            }
        }
    }
    
    void FixedUpdate()
    {
        if (isInitialized && useAAAMovement)
        {
            // Handle platform movement for CharacterController when in AAA mode
            // FIXED: Use FixedUpdate for consistent timing regardless of framerate
            if (isTrackingPlatformMovement)
            {
                HandlePlatformMovement();
            }
        }
    }
    
    private void InitializeAAASystem()
    {
        // Auto-find components if not assigned
        AutoFindComponents();
        
        // Setup AAA Movement Controller
        SetupMovementController();
        
        // Setup Audio Manager
        SetupAudioManager();
        
        // VFX Manager has been completely removed
        
        // Disable existing systems if requested
        if (replaceExistingMovement)
        {
            DisableExistingSystems();
        }
        
        isInitialized = true;

        // Apply starting state (Celestial flight by default)
        if (!useAAAMovement)
        {
            // Disable AAA components (but keep camera controller for shake effects)
            if (aaaMovement != null) aaaMovement.enabled = false;
            // Keep AAACameraController enabled for camera shake effects in all modes
            // if (aaaCamera != null) aaaCamera.enabled = false; // DISABLED: Camera shake should work in flight too
            if (aaaAudio != null) aaaAudio.enabled = false;
            // Ensure legacy systems are enabled
            if (existingMovementManager != null) existingMovementManager.enabled = true;
            if (existingCharacterMotor != null) existingCharacterMotor.enabled = true;

            // BULLETPROOF: Ensure flight mode physics setup
            EnsureFlightModePhysics();

            // Ensure CelestialDrift starts from a clean, fresh state (like game start)
            var driftStartup = GetComponent<CelestialDriftController>();
            if (driftStartup != null)
            {
                driftStartup.ResetFlightState(false);
            }
        }
        else
        {
            // If starting with AAA movement, disable legacy
            DisableExistingSystems();
        }

        LogSystemStatus();
    }
    
    private void AutoFindComponents()
    {
        // Find AAA components
        if (aaaMovement == null)
            aaaMovement = GetComponent<AAAMovementController>();
        
        if (aaaCamera == null)
            aaaCamera = GetComponentInChildren<AAACameraController>();
        
        if (aaaAudio == null)
            aaaAudio = GetComponent<AAAMovementAudioManager>();
        
        // Find existing components
        if (existingMovementManager == null)
            existingMovementManager = GetComponent<PlayerMovementManager>();
        
        // GroundedState is a plain class, not a Component, so we cannot GetComponent it.
        // It's handled indirectly via PlayerMovementManager, so no need to fetch it here.
        
        if (existingCharacterMotor == null)
            existingCharacterMotor = GetComponent<SimpleCharacterMotor>();
        
        // Find camera
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();
        
        if (cameraParent == null && playerCamera != null)
            cameraParent = playerCamera.transform.parent;
    }
    
    private void SetupMovementController()
    {
        if (aaaMovement == null)
        {
            aaaMovement = gameObject.AddComponent<AAAMovementController>();
        }
        
        // Ensure CharacterController exists
        CharacterController cc = GetComponent<CharacterController>();
        if (cc == null)
        {
            cc = gameObject.AddComponent<CharacterController>();
            cc.height = 2.0f;
            cc.radius = 0.5f;
            cc.center = new Vector3(0, 1.0f, 0);
        }
    }
    
    private void SetupAudioManager()
    {
        if (aaaAudio == null)
        {
            aaaAudio = gameObject.AddComponent<AAAMovementAudioManager>();
        }
        
        // Ensure AudioSource exists
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f; // 2D audio
        }
    }
    
    private void DisableExistingSystems()
    {
        if (existingCharacterMotor != null)
        {
            existingCharacterMotor.enabled = false;
        }
        
        // Disable PlayerMovementManager when switching to AAA mode
        if (existingMovementManager != null)
        {
            existingMovementManager.enabled = false;
        }
        
        // Note: GroundedState is not a MonoBehaviour, so we can't disable it directly
        // It will be bypassed when the PlayerMovementManager is disabled
        // if (existingGroundedState != null && enableDebugMode)
        // {
        // }
    }
    
    /// <summary>
    /// Check for platform landing while in flying mode and automatically switch to AAA mode
    /// </summary>
    private void CheckForPlatformLanding()
    {
        // Raycast down to detect platforms while flying
        if (Physics.Raycast(transform.position, -transform.up, out RaycastHit hit, 2.5f))
        {
            var platform = hit.transform.GetComponentInParent<CelestialPlatform>();
            if (platform != null)
            {
        
                SwitchToAAAMode(platform);
            }
        }
    }

    private void HandleSystemIntegration()
    {
        // Handle landing detection
        if (aaaMovement != null)
        {
            bool isGroundedNow = aaaMovement.IsGrounded;
            
            if (isGroundedNow && !wasGroundedLastFrame)
            {
                // Landing detected - effects/VFX removed per user request
            }
            
            wasGroundedLastFrame = isGroundedNow;
        }
        
        // Jump input handling is done by AAAMovementController, not here
    }
    
    private void HandleDebugInput()
    {
        // Debug: Press F1 to toggle AAA system
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ToggleAAASystem();
        }
        
        // Debug: Press F2 to reset player position
        if (Input.GetKeyDown(KeyCode.F2))
        {
            ResetPlayerPosition();
        }
        
        // Debug: Press F3 to log system status
        if (Input.GetKeyDown(KeyCode.F3))
        {
            LogSystemStatus();
        }
    }
    
    private void ToggleAAASystem()
  {
      if (useAAAMovement)
      {
          // Switching FROM AAA TO FLIGHT - Check if we're on a platform for launch
          bool isOnPlatform = IsPlayerOnPlatform();
          if (isOnPlatform)
          {
      
              SwitchToFlightMode(true); // Pass true for launch
          }
          else
          {
              SwitchToFlightMode(false); // Normal flight switch
          }
      }
      else
      {
          // Switching FROM FLIGHT TO AAA - Check lock-on vs nearby platform
          if (existingMovementManager != null && existingMovementManager.IsLockedOn)
          {
              // Smart fall to locked platform
      
              SwitchToLockedPlatformFall();
          }
          else
          {
              // Not locked: engage falling with world-up uprighting; auto-landing will switch to AAA on contact
              var drift = GetComponent<CelestialDriftController>();
              if (drift != null)
              {
                  drift.StartFallingToPlatform();
              }
          }
      }
  }
    
    
    
 
    
    /// <summary>
    /// NEW METHOD: This will be called by flight system when it's time to land.
    /// Switches to AAA ground movement mode on the specified platform.
    /// </summary>
    public void SwitchToAAAMode(CelestialPlatform landingPlatform)
    {
        if (useAAAMovement) return; // Already in this mode

        useAAAMovement = true;
        // SIMPLIFIED: Direct flight → AAA transition
        
        // Show flavor text for walking mode activation
        if (CognitiveFeedManager.Instance != null && FlavorTextManager.Instance != null)
        {
            CognitiveFeedManager.Instance.ShowPersistentMessage(FlavorTextManager.Instance.GetWalkingActivateMessage());
            StartCoroutine(ClearPersistentMessageDelay(4f));
        }
        
        // Play sound effect for walking mode activation
        GameSounds.PlayUIFeedback(transform.position, 0.6f);
        
        // Capture current rigidbody velocity BEFORE physics swap (for momentum preservation)
        Rigidbody rb = GetComponent<Rigidbody>();
        Vector3 rbVelocity = Vector3.zero;
        if (rb != null)
        {
            rbVelocity = rb.linearVelocity;
        }

        // STEP 2: KEEP PlayerMovementManager ENABLED for lock-on in AAA
        // Flight inputs are gated inside FlightState when AAA mode is active
        if (existingMovementManager != null)
        {
            if (!existingMovementManager.enabled)
            {
                existingMovementManager.enabled = true;
            }
        }

        // STEP 3: Do NOT parent the player to the platform.
        // Parenting a CharacterController under a moving parent causes sliding/jitter.
        // Instead, we keep world-space parenting and apply platform deltas manually.
        transform.SetParent(null, true);

        // Snap player perfectly to the platform surface using the controller's bottom reference
        Vector3 platformUp = landingPlatform.transform.up;
        if (Physics.Raycast(transform.position + platformUp * 2f, -platformUp, out RaycastHit hit, 10f, ~0, QueryTriggerInteraction.Ignore))
        {
            // Use precise bottom snap to contact to avoid micro-gaps that cause sliding
            SnapBottomToContact(hit);
        }
        // --- End Snap ---

        // Align upright relative to platform's up while preserving current camera yaw
        Vector3 refForward = (playerCamera != null) ? playerCamera.transform.forward : transform.forward;
        AlignUprightToNormalPreserveYaw(platformUp, refForward);

        // STEP 4: Enable AAA ground movement components
        if (aaaMovement != null) aaaMovement.enabled = true;
        if (aaaCamera != null) aaaCamera.enabled = true;
        if (aaaAudio != null) aaaAudio.enabled = true;
        
        // STEP 4.5: PRESERVE camera orientation for smooth transitions (no reset)
        // ResetCameraInputState(); // DISABLED: User wants smooth camera transitions
        
        // CRITICAL FIX: Switch AAAMovementController to Walking mode for proper jumping
        if (aaaMovement != null)
        {
            aaaMovement.SetMovementMode(AAAMovementController.MovementMode.Walking);
            aaaMovement.SetGroundedImmediate(true);
        }
        
        // STEP 5: BULLETPROOF: Configure physics for AAA mode
        EnsureAAAModePhysics();
        
        // STEP 5.5: Apply preserved horizontal momentum as an external ground velocity with decay
        if (preserveLandingMomentum && aaaMovement != null)
        {
            // Project onto the platform plane (horizontal relative to platform)
            Vector3 preservedHorizontal = Vector3.ProjectOnPlane(rbVelocity, platformUp);
            if (preservedHorizontal.magnitude >= landingMomentumMinSpeed)
            {
                StartLandingMomentum(preservedHorizontal);
            }
        }

        // STEP 6: Store platform reference and ENABLE delta-tracking (no parenting)
        lastPlatform = landingPlatform;
        lastPlatformPosition = landingPlatform.transform.position;
        lastPlatformRotation = landingPlatform.transform.rotation;
        isTrackingPlatformMovement = true; // Apply platform deltas via CharacterController

        // STEP 6.5: Preserve lock-on state explicitly for consistency
        var pmm = GetComponent<PlayerMovementManager>();
        if (pmm != null && landingPlatform != null)
        {
            pmm.RestoreLockOnState(landingPlatform);
        }
        
        // STEP 7: Disable existing flight systems (already done in STEP 2, but kept for clarity)
        // Flight systems already disabled in STEP 2
        if (existingCharacterMotor != null) existingCharacterMotor.enabled = false;
        
        // TODO: Need to prevent FlightState input when in AAA mode
        // FlightState is not a MonoBehaviour - need different approach

        LogSystemStatus();
    }
    
    /// <summary>
    /// NEW METHOD: Switch to flight mode with optional platform launch
    /// </summary>
    private void SwitchToFlightMode(bool launchFromPlatform = false)
    {
        if (!useAAAMovement) return; // Already in flight mode

        useAAAMovement = false;
        
        // Stop any landing momentum bridging when leaving AAA
        StopLandingMomentum();

        // Show flavor text for flight mode activation
        if (CognitiveFeedManager.Instance != null && FlavorTextManager.Instance != null)
        {
            CognitiveFeedManager.Instance.ShowPersistentMessage(FlavorTextManager.Instance.GetFlightActivateMessage());
            StartCoroutine(ClearPersistentMessageDelay(4f));
        }
        
        // Play sound effect for flying mode activation
        GameSounds.PlayPowerUpStart(transform.position, 0.7f);
        
        
        // 1. Detach from any parented platform (SetParent with true preserves world position automatically)
        transform.SetParent(null, true); // This already preserves world position/rotation - no manual restoration needed!
        // Stop applying platform deltas while in flight
        isTrackingPlatformMovement = false;
        
        // 1.1 Ensure any falling-to-platform state is cleared to prevent platform-up alignment in flight
        var drift = GetComponent<CelestialDriftController>();
        if (drift != null)
        {
            drift.StopFallingToPlatform();
        }

        // 2. Disable AAA components (but keep camera controller for shake effects)
        if (aaaMovement != null) 
        {
            aaaMovement.enabled = false;
        }
        // Keep AAACameraController enabled for camera shake effects in all modes
        // if (aaaCamera != null) aaaCamera.enabled = false; // DISABLED: Camera shake should work in flight too
        if (aaaAudio != null) aaaAudio.enabled = false;
        
        // 3. BULLETPROOF: Ensure flight mode physics setup
        EnsureFlightModePhysics();
        
        // 3.5 Ensure CelestialDrift is reset to a fresh, clean state (axes/orientation/velocities)
        if (drift != null)
        {
            drift.ResetFlightState(false);
        }
        
        // 4.5. Apply sustained upward boost if launching from platform
        if (launchFromPlatform)
        {
            StartCoroutine(ApplySustainedLaunchBoost());
        }
        
        // 5. Enable flight systems first
        if (existingMovementManager != null) 
        {
            existingMovementManager.enabled = true;
        }

        // 5.1 Clear lock-on when leaving walking (AAA) back to flight (per user request)
        var pmm2 = GetComponent<PlayerMovementManager>();
        if (pmm2 != null)
        {
            pmm2.ClearLockOn();
        }
        
        // 6. PRESERVE camera orientation for smooth transitions (no reset)
        // Camera reset completely disabled for smooth mode transitions
        // ResetCameraInputState(); // DISABLED: User wants smooth camera transitions
        
        LogSystemStatus();
    }

    // (duplicate EnsureAAAModePhysics/EnsureFlightModePhysics removed; robust versions are defined later in file)
    
    /// <summary>
    /// Coroutine to apply sustained upward boost - ENHANCED VERSION
    /// Provides 1.5 seconds of upward boost while maintaining full player control
    /// </summary>
    private System.Collections.IEnumerator ApplySustainedLaunchBoost()
    {
        // Wait 1 frame to ensure flight systems are ready
        yield return new WaitForFixedUpdate();
        
        
        float boostDuration = 1.5f; // Extended to 1.5 seconds for better launch feel
        float elapsed = 0f;
        
        // Get rigidbody for force application
        Rigidbody rb = GetComponent<Rigidbody>();
        
        // Get flight controller to check thrust settings
        CelestialDriftController flightController = GetComponent<CelestialDriftController>();
        
        // Enhanced boost force - much stronger and uses proper direction
        float boostForce = 45f; // Increased from 12f for better effect
        
        while (elapsed < boostDuration && rb != null)
        {
            // Use transform.up (player's up direction) instead of Vector3.up (world up)
            Vector3 thrustDirection = transform.up;
            
            // Apply stronger boost force
            rb.AddForce(thrustDirection * boostForce, ForceMode.Force);
            
            // Debug every 0.3 seconds
            if (Mathf.FloorToInt(elapsed * 3.33f) != Mathf.FloorToInt((elapsed - Time.fixedDeltaTime) * 3.33f))
            {
            }
            
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        
    }
    
    /// <summary>
    /// Smart fall to locked platform - uses existing lock-on to guide fall
    /// </summary>
    private void SwitchToLockedPlatformFall()
  {
      if (existingMovementManager == null || !existingMovementManager.IsLockedOn)
      {
          // No lock-on: stay in FLIGHT mode and notify the player
          if (CognitiveFeedManager.Instance != null)
          {
              CognitiveFeedManager.Instance.ShowPersistentMessage("[No lock-on active. Flight remains enabled.]");
              StartCoroutine(ClearPersistentMessageDelay(4f));
          }
          return;
      }
      
      CelestialPlatform lockedPlatform = existingMovementManager.LockedPlatform;
      
      // Don't clear lock-on - use it to guide the fall!
      // Engage falling within flight; landing will switch to AAA automatically
      var drift = GetComponent<CelestialDriftController>();
      if (drift != null)
      {
          drift.StartFallingToPlatform();
      }
  }
    
    
    /// <summary>
    /// Check if player is currently standing on a platform (for launch detection)
    /// </summary>
    private bool IsPlayerOnPlatform()
  {
      // Method 1: Check if AAA movement controller reports grounded
      if (aaaMovement != null && aaaMovement.enabled)
      {
            // Use AAA controller's ground detection via public API
            bool isGrounded = aaaMovement.IsGrounded;
            if (isGrounded)
            {
                // Double-check with raycast for platform specifically
                RaycastHit hit;
                if (Physics.Raycast(transform.position, Vector3.down, out hit, 3f))
                {
                    CelestialPlatform platform = hit.collider.GetComponent<CelestialPlatform>();
                    if (platform != null)
                    {
                        return true;
                    }
                }
                return true; // Grounded but maybe not on platform
            }
      }
      
      // Method 2: Direct raycast check (fallback)
      RaycastHit hit2;
      float checkDistance = 2f;
        
        if (Physics.Raycast(transform.position, Vector3.down, out hit2, checkDistance))
        {
            CelestialPlatform platform = hit2.collider.GetComponent<CelestialPlatform>();
            if (platform != null)
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// IMPROVED: Use the preserved lock-on system for platform tracking
    /// PRESERVES LOCK-ON: Works with existing lock-on instead of fighting it
    /// </summary>
    private void TrackPlatformMovement()
    {
        if (lastPlatform == null || existingMovementManager == null) return;
        
        // PRESERVE LOCK-ON: Don't force re-enable if user manually disabled flight manager
        // Just ensure the lock-on state is consistent with our platform
        if (existingMovementManager.IsLockedOn)
        {
            CelestialPlatform currentLockOn = existingMovementManager.LockedPlatform;
            
            if (currentLockOn == lastPlatform)
            {
                // Perfect! Lock-on matches our platform - let it work
                // Checking parent for platform
            }
            else if (currentLockOn != null)
            {
                // User locked onto a different platform - respect their choice
                lastPlatform = currentLockOn; // Update our reference
                lastPlatformPosition = currentLockOn.transform.position;
                lastPlatformRotation = currentLockOn.transform.rotation;
            }
        }
        else
        {
            // No lock-on active - offer to restore it for the current platform
        }
    }

    /// <summary>
    /// Fixes camera positioning to ensure it's at head level (not at feet)
    /// </summary>
    private void FixCameraPosition()
    {
        if (playerCamera == null) return;
        
        // Ensure camera parent exists and is positioned correctly
        if (cameraParent == null && playerCamera != null)
        {
            // Create camera parent if missing
            GameObject cameraParentObj = new GameObject("CameraParent");
            cameraParentObj.transform.SetParent(transform);
            cameraParentObj.transform.localPosition = new Vector3(0, 1.6f, 0);
            cameraParentObj.transform.localRotation = Quaternion.identity;
            
            // Move camera under parent
            playerCamera.transform.SetParent(cameraParentObj.transform);
            playerCamera.transform.localPosition = Vector3.zero;
            playerCamera.transform.localRotation = Quaternion.identity;
            
            cameraParent = cameraParentObj.transform;
        }
        else
        {
            // Make sure camera parent is at correct height (head level)
            cameraParent.localPosition = new Vector3(0, 1.6f, 0);
            
            // Make sure camera is centered within the parent
            playerCamera.transform.localPosition = Vector3.zero;
            playerCamera.transform.localRotation = Quaternion.identity;
        }
        
        // REMOVED: This was causing deeper sinking issues
        // if (aaaCamera != null)
        // {
        //     aaaCamera.UpdateOriginalCameraPosition();
        // }
        
        if (enableDebugMode)
        {
        }
    }
    
    private void ResetPlayerPosition()
    {
        transform.position = Vector3.up * 2f;
        if (aaaMovement != null)
        {
            // Reset velocity if possible through reflection or public method
        }
    }
    
    private void LogSystemStatus()
    {
        
        if (aaaMovement != null)
        {
        }
        
    }
    
    void OnGUI()
    {
        // Debug overlay disabled
        /*
        if (!enableDebugMode || !useAAAMovement) return;
        
        // Debug overlay
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("<b>AAA Movement System</b>");
        
        if (aaaMovement != null)
        {
            GUILayout.Label($"Speed: {aaaMovement.CurrentSpeed:F1} m/s");
            GUILayout.Label($"Grounded: {aaaMovement.IsGrounded}");
            GUILayout.Label($"Crouching: {aaaMovement.IsCrouching}");
            GUILayout.Label($"Sliding: {aaaMovement.IsSliding}");
        }
        
        GUILayout.Space(10);
        GUILayout.Label("<b>Debug Controls:</b>");
        GUILayout.Label("F1: Toggle System");
        GUILayout.Label("F2: Reset Position");
        GUILayout.Label("F3: Log Status");
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
        */
    }
    
    // Helper method to detect a platform below the player
    private CelestialPlatform FindPlatformBelow()
    {
        float rayDistance = 20.0f; // Check far below player to catch distant platforms
        
        // Add extensive debugging
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, rayDistance))
        {
            
            // First check direct hit for CelestialPlatform
            CelestialPlatform platform = hit.collider.GetComponent<CelestialPlatform>();
            
            // If not found directly on hit collider, try parent
            if (platform == null)
            {
                platform = hit.collider.GetComponentInParent<CelestialPlatform>();
                // Checking parent for platform
            }
                
            if (platform != null)
            {
                // Checking parent for platform
                return platform;
            }
            else
            {
                // Checking parent for platform
            }
        }
        else
        {
            // No raycast hit detected
        }
        
        // If standard raycast failed, try a slightly broader search with SphereCast
        float sphereRadius = 3.0f;
        if (Physics.SphereCast(transform.position, sphereRadius, Vector3.down, out RaycastHit sphereHit, rayDistance))
        {
            CelestialPlatform platform = sphereHit.collider.GetComponent<CelestialPlatform>();
            if (platform == null)
                platform = sphereHit.collider.GetComponentInParent<CelestialPlatform>();
                
            if (platform != null)
            {
                // Checking parent for platform
                return platform;
            }
            else
            {
                // Checking parent for platform
            }
        }
        else
        {
            // No raycast hit detected
        }
        
        return null;
    }
    
    // HandleMomentumPreservation function removed - momentum system no longer used
    
    private void HandlePlatformMovement()
    {
        // CRITICAL FIX: Check if platform reference is valid
        if (lastPlatform == null)
        {
            isTrackingPlatformMovement = false; // Stop tracking if platform is gone
            return;
        }

        // If we are parented to the platform, do NOT also apply deltas via CharacterController
        // This avoids double movement that pushes the player off upon landing
        if (transform.parent == lastPlatform.transform)
        {
            return;
        }

        // Get current platform transform
        Transform platformTransform = lastPlatform.transform;
        
        // Calculate how much the platform has moved/rotated
        Vector3 platformDelta = platformTransform.position - lastPlatformPosition;
        Quaternion platformRotationDelta = platformTransform.rotation * Quaternion.Inverse(lastPlatformRotation);
        
        // Apply platform movement to CharacterController
        // FIXED: Remove restrictive threshold - handle ANY platform movement speed
        if (platformDelta.magnitude > 0.0001f) // Minimal threshold to avoid floating-point noise
        {
            CharacterController cc = GetComponent<CharacterController>();
            if (cc != null && cc.enabled && cc.gameObject.activeInHierarchy)
            {
                // CRITICAL FIX: Handle high-speed platform movement
                // For very fast platforms, break movement into smaller steps to prevent tunneling
                if (platformDelta.magnitude > 2.0f) // High-speed platform detected
                {
                    int steps = Mathf.CeilToInt(platformDelta.magnitude / 1.5f); // Max 1.5 units per step
                    Vector3 stepDelta = platformDelta / steps;
                    
                    for (int i = 0; i < steps; i++)
                    {
                        cc.Move(stepDelta);
                    }
                }
                else
                {
                    // Normal speed - single move operation
                    cc.Move(platformDelta);
                }
            }
        }
        
        // Handle platform rotation (rotate player around platform center)
        if (Quaternion.Angle(lastPlatformRotation, platformTransform.rotation) > 0.1f)
        {
            // Calculate rotation around platform center
            Vector3 directionFromPlatform = transform.position - platformTransform.position;
            Vector3 newDirection = platformRotationDelta * directionFromPlatform;
            Vector3 rotationDelta = newDirection - directionFromPlatform;
            
            CharacterController cc = GetComponent<CharacterController>();
            if (cc != null && cc.enabled && cc.gameObject.activeInHierarchy)
            {
                cc.Move(rotationDelta);
                
                // Checking parent for platform
            }
        }
        
        // Update stored platform transform for next frame
        lastPlatformPosition = platformTransform.position;
        lastPlatformRotation = platformTransform.rotation;
    }
    
    // Public methods for external integration
    public void EnableAAASystem()
    {
        useAAAMovement = true;
        // SIMPLIFIED: Direct transition to AAA mode
    }
    
    public void DisableAAASystem()
    {
        useAAAMovement = false;
        ToggleAAASystem();
    }
    
    public bool IsAAASystemActive()
    {
        return useAAAMovement && isInitialized;
    }
    
    // Expose flight switch as a public API for triggers and other systems
    public void SwitchToFlightModeExternal(bool launchFromPlatform = false)
    {
        SwitchToFlightMode(launchFromPlatform);
    }
    
    public AAAMovementController GetMovementController()
    {
        return aaaMovement;
    }
    
    /// <summary>
    /// BULLETPROOF: Validates that CharacterController and Rigidbody are never both active simultaneously
    /// </summary>
    private void ValidateColliderState()
    {
        // Cache components for performance
        if (pillCollider == null) pillCollider = GetComponent<CharacterController>();
        if (playerRigidbody == null) playerRigidbody = GetComponent<Rigidbody>();
        
        if (pillCollider == null || playerRigidbody == null) return;
        
        bool shouldHaveColliderEnabled = useAAAMovement;
        
        // CRITICAL: Ensure CharacterController is NEVER active during flight mode
        if (!shouldHaveColliderEnabled && pillCollider.enabled)
        {
            pillCollider.enabled = false;
        }
        
        // Enable CharacterController only when in AAA mode and grounded
        if (shouldHaveColliderEnabled && !pillCollider.enabled)
        {
            pillCollider.enabled = true;
        }
        
        // Ensure PlayerMovementManager is ENABLED in AAA mode so lock-on input still works
        if (shouldHaveColliderEnabled && existingMovementManager != null && !existingMovementManager.enabled)
        {
            existingMovementManager.enabled = true;
        }
        
        // Track state changes for debugging
        if (pillCollider.enabled != lastColliderState)
        {
            lastColliderState = pillCollider.enabled;
            // Collider state changed
        }
        
        // SAFETY CHECK: Never allow both CharacterController and free Rigidbody
        if (pillCollider.enabled && !playerRigidbody.isKinematic)
        {
            if (!useAAAMovement)
            {
                // In flight mode - disable CharacterController
                pillCollider.enabled = false;
            }
            else
            {
                // In AAA mode - make Rigidbody kinematic
                playerRigidbody.isKinematic = true;
            }
        }
    }
    
    /// <summary>
    /// BULLETPROOF: Ensures proper physics setup for flight mode (Rigidbody active, CharacterController disabled)
    /// PRESERVES LOCK-ON: Maintains platform tracking capabilities
    /// FIXED: Proper synchronization prevents physics conflicts
    /// </summary>
    private void EnsureFlightModePhysics()
    {
        if (pillCollider == null) pillCollider = GetComponent<CharacterController>();
        if (playerRigidbody == null) playerRigidbody = GetComponent<Rigidbody>();
        
        // CRITICAL FIX: Disable CharacterController FIRST to prevent dual-physics conflicts
        if (pillCollider != null && pillCollider.enabled)
        {
            // Capture current velocity from CharacterController before disabling
            Vector3 currentVelocity = Vector3.zero;
            if (aaaMovement != null)
            {
                currentVelocity = aaaMovement.Velocity;
            }
            
            // Immediately disable CharacterController to prevent conflicts
            pillCollider.enabled = false;
            
            // Transfer velocity to Rigidbody for smooth transition
            if (playerRigidbody != null && currentVelocity.magnitude > 0.1f)
            {
                playerRigidbody.linearVelocity = currentVelocity;
            }
        }
        
        // Enable Rigidbody for flight physics AFTER CharacterController is disabled
        if (playerRigidbody != null)
        {
            if (playerRigidbody.isKinematic)
            {
                playerRigidbody.isKinematic = false;
            }
            
            // Ensure proper gravity settings for flight
            playerRigidbody.useGravity = true;
            playerRigidbody.linearDamping = 2f; // Air resistance
        }
        
        // CRITICAL: Enable flight trigger collider for platform detection during flight
        EnableFlightTriggerCollider();
        
        if (enableDebugMode)
        {
        }
    }
    
    /// <summary>
    /// REMOVED: No longer needed - CharacterController disabled immediately for better synchronization
    /// </summary>
    private System.Collections.IEnumerator DisableCharacterControllerNextFrame()
    {
        yield return null; // Placeholder - method no longer used
    }
    
    /// <summary>
    /// CRITICAL: Reset camera input state to prevent roll corruption when switching modes
    /// This fixes the bug where horizontal mouse movement causes rolling instead of looking
    /// </summary>
    private void ResetCameraInputState()
    {
        // Intentionally left blank: camera input reset disabled for smooth transitions
    }
    
    /// <summary>
    /// Brief pause to let Unity's input system stabilize after mode switching
    /// </summary>
    private System.Collections.IEnumerator InputStabilizationPause()
    {
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
    }
    
    /// <summary>
    /// BULLETPROOF: Ensures proper physics setup for AAA mode (CharacterController active, Rigidbody kinematic)
    /// PRESERVES LOCK-ON: Maintains platform tracking for moving platforms
    /// FIXED: Proper synchronization prevents physics conflicts
    /// </summary>
    private void EnsureAAAModePhysics()
    {
        if (pillCollider == null) pillCollider = GetComponent<CharacterController>();
        if (playerRigidbody == null) playerRigidbody = GetComponent<Rigidbody>();
        
        // CRITICAL FIX: Make Rigidbody kinematic FIRST and capture velocity
        Vector3 transferVelocity = Vector3.zero;
        if (playerRigidbody != null && !playerRigidbody.isKinematic)
        {
            // Capture current velocity before making kinematic
            transferVelocity = playerRigidbody.linearVelocity;
            
            // Zero out velocity before making kinematic to prevent conflicts
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            
            // Make kinematic to disable Rigidbody physics
            playerRigidbody.isKinematic = true;
        }
        
        // Enable CharacterController immediately after Rigidbody is kinematic
        if (pillCollider != null && !pillCollider.enabled)
        {
            pillCollider.enabled = true;
            
            // Transfer velocity to AAAMovementController for smooth transition
            if (aaaMovement != null && transferVelocity.magnitude > 0.1f)
            {
                aaaMovement.SetVelocityImmediate(transferVelocity);
            }
        }
        
        // CRITICAL: Disable flight trigger collider when CharacterController is active
        DisableFlightTriggerCollider();
        
        if (enableDebugMode)
        {
        }
    }
    
    /// <summary>
    /// REMOVED: No longer needed - CharacterController enabled immediately for better synchronization
    /// </summary>
    private System.Collections.IEnumerator EnableCharacterControllerNextFrame()
    {
        yield return null; // Placeholder - method no longer used
    }
    
    /// <summary>
    /// Helper method to get current mode as string for debugging
    /// </summary>
    private string GetCurrentModeString()
    {
        if (useAAAMovement) return "AAA";
        return "FLIGHT";
    }
    
    /// <summary>
    /// Enable flight trigger collider for platform detection during flight mode
    /// </summary>
    private void EnableFlightTriggerCollider()
    {
        if (flightTriggerCollider == null)
        {
            // Try to find existing SphereCollider or create one
            flightTriggerCollider = GetComponent<SphereCollider>();
            if (flightTriggerCollider == null)
            {
                return;
            }
        }
        
        if (!flightTriggerCollider.enabled)
        {
            flightTriggerCollider.enabled = true;
            flightTriggerCollider.isTrigger = true; // Ensure it's a trigger
            flightTriggerCollider.radius = flightTriggerRadius;
        }
    }
    
    /// <summary>
    /// Disable flight trigger collider when CharacterController is active
    /// </summary>
    private void DisableFlightTriggerCollider()
    {
        if (flightTriggerCollider != null && flightTriggerCollider.enabled)
        {
            flightTriggerCollider.enabled = false;
        }
    }
    
    /// <summary>
    /// Initialize flight trigger collider if not assigned
    /// </summary>
    private void InitializeFlightTriggerCollider()
    {
        if (flightTriggerCollider == null)
        {
            // Look for existing SphereCollider that could be used as flight trigger
            SphereCollider[] sphereColliders = GetComponents<SphereCollider>();
            foreach (var sphere in sphereColliders)
            {
                if (sphere.isTrigger)
                {
                    flightTriggerCollider = sphere;
                    break;
                }
            }
            
            if (flightTriggerCollider == null)
            {
            }
        }
    }

    // === Landing momentum helpers ===
    private void StartLandingMomentum(Vector3 horizontalVelocity)
    {
        if (!preserveLandingMomentum) return;
        if (horizontalVelocity.sqrMagnitude < (landingMomentumMinSpeed * landingMomentumMinSpeed)) return;
        if (landingMomentumRoutine != null) StopCoroutine(landingMomentumRoutine);
        landingMomentumRoutine = StartCoroutine(ApplyLandingMomentum(horizontalVelocity));
    }

    private void StopLandingMomentum()
    {
        if (landingMomentumRoutine != null)
        {
            StopCoroutine(landingMomentumRoutine);
            landingMomentumRoutine = null;
        }
        if (aaaMovement != null)
        {
            // Clear external override if any
            aaaMovement.ClearExternalGroundVelocity();
        }
    }

    private System.Collections.IEnumerator ApplyLandingMomentum(Vector3 initialHorizontal)
    {
        float elapsed = 0f;
        Vector3 start = initialHorizontal;
        while (elapsed < landingMomentumDuration && aaaMovement != null)
        {
            float t = Mathf.Clamp01(elapsed / Mathf.Max(landingMomentumDuration, 0.0001f));
            // Smooth decay (ease-out)
            Vector3 current = Vector3.Lerp(start, Vector3.zero, t);
            // Provide mild ground stick to prevent micro-bounces while preserving horizontal
            Vector3 ext = new Vector3(current.x, -2f, current.z);
            aaaMovement.SetExternalGroundVelocity(ext);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (aaaMovement != null)
        {
            aaaMovement.ClearExternalGroundVelocity();
        }
        landingMomentumRoutine = null;
    }
}
