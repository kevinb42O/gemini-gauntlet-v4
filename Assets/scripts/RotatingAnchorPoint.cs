using UnityEngine;

/// <summary>
/// Simple rotating object - accelerates, decelerates, reverses, repeat
/// Perfect for testing grappling hook on moving/rotating anchors
/// Attach to any GameObject to make it spin perpetually
/// </summary>
public class RotatingAnchorPoint : MonoBehaviour
{
    [Header("=== 🌀 ROTATION SETTINGS ===")]
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
    
    [Header("=== 🎨 VISUAL FEEDBACK ===")]
    [Tooltip("Show rotation speed in console")]
    [SerializeField] private bool debugRotation = false;
    
    // Runtime state
    private enum RotationState { Accelerating, Decelerating, Paused }
    private RotationState currentState = RotationState.Accelerating;
    private float currentSpeed = 0f;
    private float stateTimer = 0f;
    
    // CRITICAL: Use FixedUpdate for rotation to sync with CelestialPlatform passenger movement
    void FixedUpdate()
    {
        stateTimer += Time.fixedDeltaTime;
        
        switch (currentState)
        {
            case RotationState.Accelerating:
                // Smooth acceleration
                float accelProgress = Mathf.Clamp01(stateTimer / accelerationTime);
                currentSpeed = Mathf.Lerp(0f, maxRotationSpeed, EaseInOutCubic(accelProgress));
                
                if (stateTimer >= accelerationTime)
                {
                    currentSpeed = maxRotationSpeed;
                    currentState = RotationState.Decelerating;
                    stateTimer = 0f;
                    if (debugRotation) Debug.Log($"[ROTATE] Reached max speed: {maxRotationSpeed}°/s");
                }
                break;
                
            case RotationState.Decelerating:
                // Smooth deceleration
                float decelProgress = Mathf.Clamp01(stateTimer / decelerationTime);
                currentSpeed = Mathf.Lerp(maxRotationSpeed, 0f, EaseInOutCubic(decelProgress));
                
                if (stateTimer >= decelerationTime)
                {
                    currentSpeed = 0f;
                    currentState = RotationState.Paused;
                    stateTimer = 0f;
                    if (debugRotation) Debug.Log("[ROTATE] Stopped");
                }
                break;
                
            case RotationState.Paused:
                currentSpeed = 0f;
                
                if (stateTimer >= pauseAtStopDuration)
                {
                    currentState = RotationState.Accelerating;
                    stateTimer = 0f;
                    if (debugRotation) Debug.Log("[ROTATE] Starting acceleration");
                }
                break;
        }
        
        // Apply rotation
        if (currentSpeed > 0.01f)
        {
            transform.Rotate(rotationAxis.normalized, currentSpeed * Time.fixedDeltaTime, Space.World);
        }
    }
    
    /// <summary>
    /// Smooth easing function for natural acceleration/deceleration
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
        
        // Draw speed indicator (longer line = faster rotation)
        float speedRatio = currentSpeed / (maxRotationSpeed + 0.01f);
        Gizmos.color = Color.Lerp(Color.green, Color.red, speedRatio);
        Gizmos.DrawWireSphere(transform.position, 50f + speedRatio * 50f);
    }
}
