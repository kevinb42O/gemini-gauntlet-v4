using UnityEngine;
using System.Collections.Generic;

// MODERN PLATFORM SYSTEM: Direct passenger movement for perfect synchronization
// NO Rigidbody needed - pure mathematical movement with zero frame delay
public class CelestialPlatform : MonoBehaviour
{
    private Transform orbitalCenterTarget;
    private float radius;
    private float speed;
    private float currentAngle;
    private Vector3 orbitalPlane;

    [Header("Movement Parameters")]
    [SerializeField] private float minSpeed = 5f;
    [SerializeField] private float maxSpeed = 20f;

    // MODERN SYSTEM: Direct passenger tracking for perfect sync
    private List<AAAMovementController> _passengers = new List<AAAMovementController>();
    private Vector3 _previousPosition;
    private Vector3 _currentPosition;
    private Vector3 _movementDelta;
    
    // Legacy fields for backward compatibility (will be removed eventually)
    private Vector3 _currentFramePredictedPosition;
    private Quaternion _currentFramePredictedRotation;
    private Vector3 _currentFrameVelocity;

    // AAA Freeze System - Reference Frame Shifting
    [Header("Freeze System (AAA Reference Frame Shift)")]
    [SerializeField] private bool isFrozen = false;
    private Vector3 frozenPosition;
    private Quaternion frozenRotation;
    private float frozenAngle;
    private Vector3 cachedVelocity;

    // Public property to check freeze state
    public bool IsFrozen => isFrozen;

    // This component is needed only for UniverseManager to call Initialize.
    private void Awake()
    {
        // MODERN: No Rigidbody needed - pure mathematical movement!
        // Remove any existing Rigidbody (backward compatibility cleanup)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            Debug.Log($"[PLATFORM] Removing unnecessary Rigidbody from {name} - using modern direct movement system");
            Destroy(rb);
        }
        
        enabled = false; // will be enabled by Initialize if needed
    }

    public void Initialize(Transform center, float r, Vector3 plane, float newSpeed = -1f, float startAngle = -1f)
    {
        orbitalCenterTarget = center;
        radius = r;
        orbitalPlane = plane;
        // Use provided start angle if valid, otherwise randomize
        currentAngle = startAngle >= 0f ? startAngle : Random.Range(0f, 360f);

        // Determine orbital speed. If a valid speed is passed in, use it; otherwise pick a random speed within range.
        speed = newSpeed >= 0f ? newSpeed : Random.Range(minSpeed, maxSpeed);

        // Calculate and set initial position so UniverseManager sees them spawn correctly.
        Vector3 localOrbitalPosition = new Vector3(Mathf.Cos(currentAngle * Mathf.Deg2Rad), 0, Mathf.Sin(currentAngle * Mathf.Deg2Rad)) * radius;
        Quaternion worldPlaneRotation = Quaternion.FromToRotation(Vector3.up, orbitalPlane);
        Vector3 worldFinalOrbitalPosition = worldPlaneRotation * localOrbitalPosition;
        transform.position = orbitalCenterTarget.position + worldFinalOrbitalPosition;
        
        // MODERN: Initialize position tracking
        _currentPosition = transform.position;
        _previousPosition = transform.position;
        _movementDelta = Vector3.zero;

        // Enable update loop only if the platform actually moves.
        enabled = !Mathf.Approximately(speed, 0f);
    }

    // UpdateTargetCenter and SetAsCenter are no longer needed with a static sun.

    // ZERO-JITTER SYSTEM: Calculate movement in FixedUpdate for physics consistency
    void FixedUpdate()
    {
        if (orbitalCenterTarget == null)
        {
            // Platform was placed manually in scene and not initialized by UniverseManager.
            // Still update cached values so other scripts don't get default zeros.
            _currentFramePredictedPosition = transform.position;
            _currentFramePredictedRotation = transform.rotation;
            _currentFrameVelocity = Vector3.zero;
            return;
        }

        // For frozen platforms, ensure cached values are updated
        if (isFrozen)
        {
            _currentFramePredictedPosition = frozenPosition;
            _currentFramePredictedRotation = frozenRotation;
            _currentFrameVelocity = cachedVelocity;
            return;
        }
        
        // ZERO-JITTER: Calculate platform movement in FixedUpdate for physics consistency
        if (orbitalCenterTarget != null && !Mathf.Approximately(speed, 0f))
        {
            // Store previous position
            _previousPosition = _currentPosition;
            
            // Advance orbital angle using FixedUpdate's consistent timing
            currentAngle += speed * Time.fixedDeltaTime;

            // Calculate new position
            Vector3 localOrbitalPosition = new Vector3(Mathf.Cos(currentAngle * Mathf.Deg2Rad), 0, Mathf.Sin(currentAngle * Mathf.Deg2Rad)) * radius;
            Quaternion worldPlaneRotation = Quaternion.FromToRotation(Vector3.up, orbitalPlane);
            Vector3 worldFinalOrbitalPosition = worldPlaneRotation * localOrbitalPosition;
            _currentPosition = orbitalCenterTarget.position + worldFinalOrbitalPosition;

            // Calculate movement delta for this frame
            _movementDelta = _currentPosition - _previousPosition;
            
            // Move platform
            transform.position = _currentPosition;
            
            // Keep platform level for walking
            transform.rotation = Quaternion.identity;
            
            // CRITICAL: Move passengers IMMEDIATELY in same physics step
            if (_movementDelta.sqrMagnitude > 0.0001f)
            {
                MovePassengers(_movementDelta);
            }
            
            // Reset delta after application
            _movementDelta = Vector3.zero;
            
            // Legacy: Calculate velocity for backward compatibility
            float angleInRadians = currentAngle * Mathf.Deg2Rad;
            Vector3 localTangentDirection = new Vector3(-Mathf.Sin(angleInRadians), 0, Mathf.Cos(angleInRadians));
            Vector3 worldTangentDirection = worldPlaneRotation * localTangentDirection.normalized;
            float linearSpeedMagnitude = speed * Mathf.Deg2Rad * radius;
            _currentFrameVelocity = worldTangentDirection * linearSpeedMagnitude;
            
            // Update cached values
            _currentFramePredictedPosition = transform.position;
            _currentFramePredictedRotation = transform.rotation;
        }
    }

    // ZERO-JITTER: Direct passenger movement in FixedUpdate for perfect sync
    private void MovePassengers(Vector3 movementDelta)
    {
        if (_passengers.Count == 0 || movementDelta.sqrMagnitude < 0.0001f) return;
        
        // Move ALL passengers unconditionally - grounded check happens AFTER movement in LateUpdate
        for (int i = _passengers.Count - 1; i >= 0; i--)
        {
            var passenger = _passengers[i];
            
            // Clean up null references (passenger destroyed)
            if (passenger == null)
            {
                _passengers.RemoveAt(i);
                continue;
            }
            
            // Move passenger directly - they'll check grounded state in their LateUpdate
            passenger.MovePlatformPassenger(movementDelta);
        }
    }
    
    // MODERN: Public API for passenger registration
    public void RegisterPassenger(AAAMovementController passenger)
    {
        if (passenger == null || _passengers.Contains(passenger)) return;
        
        _passengers.Add(passenger);
        Debug.Log($"[PLATFORM] {name} registered passenger: {passenger.name} (Total: {_passengers.Count})");
    }
    
    public void UnregisterPassenger(AAAMovementController passenger)
    {
        if (passenger == null) return;
        
        if (_passengers.Remove(passenger))
        {
            Debug.Log($"[PLATFORM] {name} unregistered passenger: {passenger.name} (Remaining: {_passengers.Count})");
        }
    }
    
    // MODERN: Get last movement delta for jump momentum
    public Vector3 GetMovementDelta()
    {
        return _movementDelta;
    }
    
    // Legacy: Backward compatibility methods
    public Vector3 GetPredictedPositionThisFrame()
    {
        return transform.position;
    }

    public Quaternion GetPredictedRotationThisFrame()
    {
        return transform.rotation;
    }

    public Vector3 GetCurrentVelocity()
    {
        // Legacy method - returns velocity calculated from movement delta
        if (Time.fixedDeltaTime > 0)
            return _movementDelta / Time.fixedDeltaTime;
        return _currentFrameVelocity;
    }

    public void Freeze()
    {
        if (isFrozen)
        {
            Debug.Log($" Platform {name} is already frozen!");
            return;
        }
        
        frozenPosition = transform.position;
        frozenRotation = transform.rotation;
        frozenAngle = currentAngle;
        cachedVelocity = _currentFrameVelocity;
        isFrozen = true;
        
        Debug.Log($" PLATFORM FROZEN: {name} at position {frozenPosition}");
    }

    public void Unfreeze()
    {
        if (!isFrozen)
        {
            Debug.Log($" Platform {name} is not frozen!");
            return;
        }
        
        currentAngle = frozenAngle; // resume from stored angle
        isFrozen = false;
        Debug.Log($" PLATFORM UNFROZEN: {name} - resuming orbital motion");
    }
}