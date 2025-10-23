// --- PlayerMovementManager.cs (HERO REFACTOR - CORRECTED & SIMPLIFIED) ---
using UnityEngine;
using System;

[RequireComponent(typeof(CelestialDriftController))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerMovementManager : MonoBehaviour
{
    [Header("Core References")]
    public CelestialDriftController FlightController;
    public Rigidbody Rb;
    // --- ADD THIS ---
    public AAAMovementIntegrator AaaIntegrator; // Assign this in the Inspector


    [Header("Lock-On System")]
    [SerializeField] private float lockOnRange = 500f;
    [SerializeField] private float lockOnSphereRadius = 3f;
    [SerializeField, Tooltip("Layers that can be locked onto.")]
    private LayerMask lockableLayers = -1; // -1 means everything

    // --- Events for Other Systems ---
    public static event Action<Transform> OnPlayerLandedOnNewPlatform;
    
    // --- State Machine ---
    private IPlayerState _currentState;
    public FlightState FlightState { get; private set; }
    // --- REMOVE THIS ---
    // public GroundedState GroundedState { get; private set; } 
    
    // --- State Data ---
    public bool IsLockedOn => _lockedPlatform != null;
    // --- REMOVE THIS ---
    // public bool IsOnPlatform => _currentState == GroundedState && TargetPlatform != null;
    public CelestialPlatform TargetPlatform { get; private set; }
    public CelestialPlatform LockedPlatform => _lockedPlatform;
    public Vector3 PendingLaunchVelocity { get; private set; }
    
    // --- Internal Lock-On State ---
    private CelestialPlatform _lockedPlatform;
    private Vector3 _lastPlatformPosition;
    private Quaternion _lastPlatformRotation;

    void Awake()
    {
        FlightController = GetComponent<CelestialDriftController>();
        Rb = GetComponent<Rigidbody>();
        // --- ADD THIS ---
        if (AaaIntegrator == null) AaaIntegrator = GetComponent<AAAMovementIntegrator>();


        if (FlightController == null || Rb == null || AaaIntegrator == null)
        {
            Debug.LogError("PlayerMovementManager is missing required components (FlightController, Rigidbody, or AAAMovementIntegrator)!", this);
            enabled = false;
            return;
        }

        // Create instances of our states
        FlightState = new FlightState();
        // --- REMOVE THIS ---
        // GroundedState = new GroundedState();
    }

    void Start()
    {
        TransitionToState(FlightState);
    }

    void Update()
    {
        HandleUniversalInput();
        _currentState?.HandleInput();
        _currentState?.Update();
    }

    // --- ADD THIS NEW METHOD ---
    void FixedUpdate()
    {
        _currentState?.FixedUpdate();

        // New landing check, happens every physics frame while in flight
        if (_currentState == FlightState)
        {
            CheckForLanding();
        }
    }

    // --- IMPROVED LANDING DETECTION - Preserves Lock-On System ---
    private void CheckForLanding()
    {
        // CRITICAL: Only check landing if we're NOT in AAA mode already
        if (AaaIntegrator != null && AaaIntegrator.useAAAMovement)
        {
            return; // Already in AAA mode, no need to check
        }
        
        // Use more conservative distance to prevent premature landing
        float landingDistance = IsLockedOn ? 1.5f : 2.0f; // Shorter distance when locked on
        
        if (Physics.Raycast(transform.position, -transform.up, out RaycastHit hit, landingDistance, lockableLayers))
        {
            var platform = hit.transform.GetComponentInParent<CelestialPlatform>();
            if (platform != null)
            {
                // PRESERVE LOCK-ON: If we're locked to a different platform, only land on the locked one
                if (IsLockedOn && _lockedPlatform != platform)
                {
                    Debug.Log($"<color=yellow>🔒 IGNORING LANDING: Locked to {_lockedPlatform.name}, but detected {platform.name}</color>");
                    return; // Don't land on platforms we're not locked to
                }
                
                Debug.Log($"<color=lime>LANDING DETECTED on {platform.name}. Switching to AAA Ground Control.</color>");
                
                // Fire event BEFORE switching modes to ensure proper order
                OnPlayerLandedOnNewPlatform?.Invoke(platform.transform);
                
                // PRESERVE LOCK-ON: Pass current lock-on state to AAA mode
                bool wasLockedOn = IsLockedOn;
                CelestialPlatform currentLockOn = _lockedPlatform;
                
                // Switch to AAA mode (this will handle component management)
                AaaIntegrator.SwitchToAAAMode(platform);
                
                // CRITICAL: Don't disable ourselves - let AAAIntegrator manage our state
                // this.enabled = false; // REMOVED - causes component lifecycle conflicts
                
                Debug.Log($"<color=green>✅ LANDING COMPLETE: Lock-on preserved = {wasLockedOn}</color>");
            }
        }
    }
    
    private void HandleUniversalInput()
    {
        if (Input.GetKeyDown(Controls.LockOn))
        {
            TryToggleLockOn();
        }
    }
    
    // --- MODIFY THIS METHOD ---
    private void TransitionToState(IPlayerState newState)
    {
        _currentState?.OnExit();
        _currentState = newState;
        _currentState.OnEnter(this);
        
        // This is no longer needed here, the landing check handles it
        // if (newState == GroundedState && TargetPlatform != null)
        // {
        //     OnPlayerLandedOnNewPlatform?.Invoke(TargetPlatform.transform);
        // }
    }
    
    public void TransitionToFlight()
    {
        if (_currentState == FlightState) return;
        TransitionToState(FlightState);
    }

    // --- REMOVE THIS METHOD ---
    // public void TransitionToGrounded(CelestialPlatform platform)
    // {
    //     if (_currentState == GroundedState) return;
    //     TargetPlatform = platform;
    //     TransitionToState(GroundedState);
    // }

    public void SetPendingLaunchVelocity(Vector3 velocity)
    {
        PendingLaunchVelocity = velocity;
    }

    public void ClearPendingLaunchVelocity()
    {
        PendingLaunchVelocity = Vector3.zero;
    }
    
    // --- Universal Lock-On Logic ---
    private void TryToggleLockOn()
    {
        if (_lockedPlatform != null)
        {
            _lockedPlatform = null;
            // Display manual platform unlock message instantly
            if (FlavorTextManager.Instance != null && CognitiveFeedManager.Instance != null)
            {
                string message = FlavorTextManager.Instance.GetPlatformLockOffMessage(true); // true = manual unlock
                CognitiveFeedManager.Instance.ShowInstantMessage(message, 1.5f);
            }
            Debug.Log("<color=orange>Lock-On Released.</color>");
            return;
        }

        Camera cam = Camera.main;
        if (cam == null) {
            Debug.LogError("Lock-On failed: No main camera found!");
            return;
        }

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.SphereCast(ray, lockOnSphereRadius, out RaycastHit hit, lockOnRange, lockableLayers, QueryTriggerInteraction.Ignore))
        {
            var platform = hit.transform.GetComponentInParent<CelestialPlatform>();
            if (platform != null)
            {
                _lockedPlatform = platform;
                _lastPlatformPosition = platform.transform.position;
                _lastPlatformRotation = platform.transform.rotation;
                
                // Display manual platform lock message instantly
                if (FlavorTextManager.Instance != null && CognitiveFeedManager.Instance != null)
                {
                    string message = FlavorTextManager.Instance.GetPlatformLockOnMessage(true); // true = manual lock
                    CognitiveFeedManager.Instance.ShowInstantMessage(message, 1.5f);
                }
                Debug.Log($"<color=lime>Lock-On Acquired: {platform.name}</color>");
            }
        }
        else
        {
            Debug.Log("Lock-On: No valid platforms found in view.");
        }
    }
    
    // Restore a previously saved lock-on state
    // Used when toggling between movement systems to maintain platform lock-on
    public void RestoreLockOnState(CelestialPlatform platform)
    {
        if (platform == null)
        {
            Debug.LogWarning("Attempted to restore lock-on with null platform.");
            return;
        }
        
        // Directly set the locked platform
        _lockedPlatform = platform;
        
        // Update position tracking
        _lastPlatformPosition = platform.transform.position;
        _lastPlatformRotation = platform.transform.rotation;
        
        Debug.Log($"<color=lime>🔒 Lock-on restored to {platform.name}</color>");
    }
    
    // Force an immediate update of the lock-on target
    // This is used by other systems to ensure they have the most current lock-on information
    public void ForceUpdateLockOn()
    {
        // Check if we have a locked platform but it's null (was destroyed)
        if (_lockedPlatform == null && IsLockedOn)
        {
            Debug.LogWarning("Locked platform was destroyed or became invalid. Clearing lock-on.");
            _lockedPlatform = null;
            return;
        }
        
        // If we have a valid lock-on, refresh its position data
        if (IsLockedOn && _lockedPlatform != null)
        {
            _lastPlatformPosition = _lockedPlatform.transform.position;
            _lastPlatformRotation = _lockedPlatform.transform.rotation;
            Debug.Log($"<color=cyan>Lock-on refreshed: {_lockedPlatform.name}</color>");
        }
        else
        {
            // If we're close to a platform but not locked on, try an automatic lock-on
            TryAutoLockOnNearbyPlatform();
        }
    }
    
    // Try to automatically lock on to a platform that's very close to the player
    private void TryAutoLockOnNearbyPlatform()
    {
        // Only auto-lock if we don't already have a locked platform
        if (_lockedPlatform != null) return;
        
        // Search for nearby platforms
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, lockOnSphereRadius * 2f, lockableLayers);
        
        // Find the closest valid platform
        float closestDistance = float.MaxValue;
        CelestialPlatform closestPlatform = null;
        
        foreach (var hitCollider in hitColliders)
        {
            CelestialPlatform platform = hitCollider.GetComponentInParent<CelestialPlatform>();
            if (platform != null)
            {
                float distance = Vector3.Distance(transform.position, platform.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlatform = platform;
                }
            }
        }
        
        // If we found a close platform, lock onto it
        if (closestPlatform != null)
        {
            _lockedPlatform = closestPlatform;
            _lastPlatformPosition = closestPlatform.transform.position;
            _lastPlatformRotation = closestPlatform.transform.rotation;
            Debug.Log($"<color=lime>Auto Lock-On Acquired: {closestPlatform.name}</color>");
        }
    }
    
    // HEROIC CORRECTION: This is the new, correct way to follow a platform.
    // It directly manipulates the player's transform to match the platform's movement delta.
    // This achieves perfect synchronization without fighting the player's controls.
    public void FollowPlatformPhysics()
    {
        // Avoid double-move when in AAA: parenting/CharacterController handles platform following
        AAAMovementIntegrator integrator = GetComponent<AAAMovementIntegrator>();
        if (integrator != null && integrator.IsAAASystemActive())
        {
            return;
        }

        if (!IsLockedOn) return;
        
        // Ensure the platform still exists.
        if (_lockedPlatform == null) {
            IsLockedOn.Equals(false); // A safety check in case the platform was destroyed.
            return;
        }

        // Calculate the platform's movement since the last physics frame.
        Quaternion rotationDelta = _lockedPlatform.transform.rotation * Quaternion.Inverse(_lastPlatformRotation);
        Vector3 positionDelta = _lockedPlatform.transform.position - _lastPlatformPosition;
        
        // --- Apply the deltas to the player's Rigidbody ---

        // 1. Apply the position change. MovePosition is the correct, physics-safe way to do this.
        Rb.MovePosition(Rb.position + positionDelta);

        // 2. Apply the rotation change. This rotates the player's velocity and orientation.
        // IMPORTANT: Velocity tracking on kinematic rigidbody is REQUIRED for:
        // - Particle systems to get correct velocity info for projectiles when moving fast
        // - Dynamic hand movement animation systems
        Rb.linearVelocity = rotationDelta * Rb.linearVelocity;
        
        // 🎯 CELESTIAL FLIGHT FIX: Only apply platform rotation in AAA mode, not in celestial flight
        // In celestial flight, player should have full manual control over look/rotation direction
        AAAMovementIntegrator aaaIntegrator = GetComponent<AAAMovementIntegrator>();
        bool isInAAAMode = aaaIntegrator != null && aaaIntegrator.useAAAMovement;
        
        if (isInAAAMode)
        {
            // AAA Mode: Apply platform rotation (preserves existing behavior)
            Rb.MoveRotation(rotationDelta * Rb.rotation);
        }
        else
        {
            // Celestial Flight Mode: Skip rotation sync to preserve manual look control
            // Player maintains full control over their look/fly direction while following platform position
            Debug.Log("<color=cyan>✈️ Celestial Flight: Preserving manual look control while following platform</color>");
        }

        // --- Update state for the next frame ---
        _lastPlatformPosition = _lockedPlatform.transform.position;
        _lastPlatformRotation = _lockedPlatform.transform.rotation;
    }
    
    /// <summary>
    /// CRITICAL: Clear lock-on state to prevent conflicts during mode transitions
    /// This fixes both gravity toggle and camera roll corruption bugs
    /// </summary>
    public void ClearLockOn()
    {
        if (_lockedPlatform != null)
        {
            Debug.Log($"<color=orange>🔓 CLEARING LOCK-ON: Was locked to {_lockedPlatform.name}</color>");
            _lockedPlatform = null;
            _lastPlatformPosition = Vector3.zero;
            _lastPlatformRotation = Quaternion.identity;
            
            // Clear target platform as well
            TargetPlatform = null;
            
            Debug.Log("<color=orange>🔓 LOCK-ON CLEARED: Ready for clean mode transition</color>");
        }
        else
        {
            Debug.Log("<color=gray>🔓 No lock-on to clear</color>");
        }
    }
}