using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Rotating platform that carries passengers - DEDICATED SYSTEM
/// Combines rotation + passenger movement in one clean script
/// Attach to any platform that should rotate and carry the player
/// </summary>
[RequireComponent(typeof(Collider))]
public class RotatingPlatform : MonoBehaviour
{
    [Header("=== 🌀 ROTATION ===")]
    [Tooltip("Maximum rotation speed (degrees/sec)")]
    [SerializeField] private float maxRotationSpeed = 180f;
    
    [Tooltip("Time to reach max speed from stop (seconds)")]
    [SerializeField] private float accelerationTime = 2f;
    
    [Tooltip("Time to slow down from max to stop (seconds)")]
    [SerializeField] private float decelerationTime = 2f;
    
    [Tooltip("Pause duration at full stop (seconds)")]
    [SerializeField] private float pauseAtStopDuration = 0.5f;
    
    [Tooltip("Rotation axis (default: up/Y axis)")]
    [SerializeField] private Vector3 rotationAxis = Vector3.up;
    
    [Header("=== 🎨 DEBUG ===")]
    [Tooltip("Show rotation and passenger info")]
    [SerializeField] private bool debug = true;
    
    // Rotation state
    private enum RotationState { Accelerating, Decelerating, Paused }
    private RotationState currentState = RotationState.Accelerating;
    private float currentSpeed = 0f;
    private float stateTimer = 0f;
    
    // Passenger tracking
    private List<AAAMovementController> passengers = new List<AAAMovementController>();
    
    // Rotation tracking for passenger movement
    private Quaternion previousRotation;
    private Quaternion currentRotation;
    
    void Awake()
    {
        currentRotation = transform.rotation;
        previousRotation = transform.rotation;
    }
    
    void FixedUpdate()
    {
        // Update rotation state
        stateTimer += Time.fixedDeltaTime;
        
        switch (currentState)
        {
            case RotationState.Accelerating:
                float accelProgress = Mathf.Clamp01(stateTimer / accelerationTime);
                currentSpeed = Mathf.Lerp(0f, maxRotationSpeed, EaseInOutCubic(accelProgress));
                
                if (stateTimer >= accelerationTime)
                {
                    currentSpeed = maxRotationSpeed;
                    currentState = RotationState.Decelerating;
                    stateTimer = 0f;
                }
                break;
                
            case RotationState.Decelerating:
                float decelProgress = Mathf.Clamp01(stateTimer / decelerationTime);
                currentSpeed = Mathf.Lerp(maxRotationSpeed, 0f, EaseInOutCubic(decelProgress));
                
                if (stateTimer >= decelerationTime)
                {
                    currentSpeed = 0f;
                    currentState = RotationState.Paused;
                    stateTimer = 0f;
                }
                break;
                
            case RotationState.Paused:
                currentSpeed = 0f;
                
                if (stateTimer >= pauseAtStopDuration)
                {
                    currentState = RotationState.Accelerating;
                    stateTimer = 0f;
                }
                break;
        }
        
        // Store rotation before applying
        previousRotation = currentRotation;
        
        // Apply rotation
        if (currentSpeed > 0.01f)
        {
            transform.Rotate(rotationAxis.normalized, currentSpeed * Time.fixedDeltaTime, Space.World);
        }
        
        // Store new rotation
        currentRotation = transform.rotation;
        
        // Calculate rotation delta
        Quaternion rotationDelta = currentRotation * Quaternion.Inverse(previousRotation);
        float rotationAngle = Quaternion.Angle(rotationDelta, Quaternion.identity);
        
        // Move passengers if platform rotated
        if (rotationAngle > 0.01f && passengers.Count > 0)
        {
            RotatePassengers(rotationDelta);
            
            if (debug)
            {
                Debug.Log($"[ROTATING PLATFORM] {name} rotated {rotationAngle:F2}° with {passengers.Count} passengers");
            }
        }
    }
    
    void LateUpdate()
    {
        // Check for passengers on platform (runs after movement controller's ground check)
        DetectPassengers();
    }
    
    /// <summary>
    /// Detect which movement controllers are standing on this platform
    /// </summary>
    void DetectPassengers()
    {
        // Simple detection: Check all movement controllers in scene
        AAAMovementController[] allControllers = FindObjectsOfType<AAAMovementController>();
        
        // Clear passenger list
        List<AAAMovementController> currentPassengers = new List<AAAMovementController>();
        
        foreach (var controller in allControllers)
        {
            if (IsStandingOnPlatform(controller))
            {
                currentPassengers.Add(controller);
            }
        }
        
        // Update passenger list
        if (currentPassengers.Count != passengers.Count)
        {
            passengers = currentPassengers;
            if (debug)
            {
                Debug.Log($"[ROTATING PLATFORM] {name} now has {passengers.Count} passengers");
            }
        }
        else
        {
            passengers = currentPassengers;
        }
    }
    
    /// <summary>
    /// Check if a movement controller is standing on this platform
    /// </summary>
    bool IsStandingOnPlatform(AAAMovementController controller)
    {
        if (controller == null || !controller.IsGrounded) return false;
        
        // Raycast down from player to check if they're on this platform
        Vector3 origin = controller.transform.position + Vector3.up * 10f;
        RaycastHit hit;
        
        if (Physics.Raycast(origin, Vector3.down, out hit, 200f, -1, QueryTriggerInteraction.Ignore))
        {
            // Check if hit this platform's collider
            return hit.collider != null && hit.collider.transform == transform;
        }
        
        return false;
    }
    
    /// <summary>
    /// Rotate all passengers around platform center
    /// </summary>
    void RotatePassengers(Quaternion rotationDelta)
    {
        Vector3 platformCenter = transform.position;
        
        foreach (var passenger in passengers)
        {
            if (passenger == null) continue;
            
            // Get passenger position relative to platform center
            Vector3 relativePos = passenger.transform.position - platformCenter;
            
            // Rotate relative position
            Vector3 rotatedRelativePos = rotationDelta * relativePos;
            
            // Calculate movement needed
            Vector3 movement = rotatedRelativePos - relativePos;
            
            // Apply movement via CharacterController
            if (passenger.TryGetComponent<CharacterController>(out var cc))
            {
                cc.Move(movement);
            }
        }
    }
    
    /// <summary>
    /// Smooth easing function
    /// </summary>
    float EaseInOutCubic(float t)
    {
        return t < 0.5f 
            ? 4f * t * t * t 
            : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }
    
    void OnDrawGizmos()
    {
        // Draw rotation axis
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, rotationAxis.normalized * 200f);
        
        // Draw speed indicator
        if (Application.isPlaying)
        {
            float speedRatio = currentSpeed / (maxRotationSpeed + 0.01f);
            Gizmos.color = Color.Lerp(Color.green, Color.red, speedRatio);
            Gizmos.DrawWireSphere(transform.position, 50f + speedRatio * 50f);
            
            // Draw passenger count
            if (passengers.Count > 0)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, 100f);
            }
        }
    }
}
