using UnityEngine;

/// <summary>
/// Simple component that makes a beam VFX follow an emit point's POSITION only (not rotation)
/// This prevents beams from spinning when hands animate while keeping them at the correct spawn location
/// </summary>
public class BeamPositionFollower : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform targetTransform;
    
    [Header("Update Settings")]
    [SerializeField] private bool updatePosition = true;
    [Tooltip("DISABLE smooth follow for zero-lag beam tracking! Instant position updates prevent visible delay.")]
    [SerializeField] private bool smoothFollow = false;
    [SerializeField] private float smoothSpeed = 10f;
    
    [Header("Offset Settings")]
    [SerializeField] private Vector3 positionOffset = Vector3.zero;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    
    private bool _isInitialized = false;
    
    /// <summary>
    /// Set the target transform to follow
    /// </summary>
    public void SetTarget(Transform target)
    {
        targetTransform = target;
        _isInitialized = true;
        
        if (showDebugLogs)
        {
            Debug.Log($"[BeamPositionFollower] Target set to: {(target != null ? target.name : "NULL")} on {name}");
        }
    }
    
    // ✅ ZERO-LAG FIX: LateUpdate ensures position tracking happens AFTER all animations
    // This eliminates visible delay when hands move during animations
    void LateUpdate()
    {
        if (!_isInitialized || targetTransform == null || !updatePosition)
            return;
        
        // Calculate target position with offset
        Vector3 targetPosition = targetTransform.position + positionOffset;
        
        if (smoothFollow)
        {
            // Smooth lerp to target position (WARNING: adds lag, not recommended for beams!)
            transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
        }
        else
        {
            // ✅ INSTANT position update - zero lag, perfect tracking even at high speeds!
            transform.position = targetPosition;
        }
        
        // NOTE: We deliberately DON'T update rotation - that stays locked to camera forward direction
        // Rotation is handled by HandFiringMechanics.UpdateBeamRotation() in LateUpdate()
        // This prevents the beam from spinning when the emit point rotates due to hand animations
        
        if (showDebugLogs && Time.frameCount % 60 == 0) // Log every 60 frames
        {
            Debug.Log($"[BeamPositionFollower] Following {targetTransform.name}: Pos={transform.position}, Rot={transform.rotation.eulerAngles}");
        }
    }
    
    /// <summary>
    /// Enable/disable position following
    /// </summary>
    public void SetFollowingEnabled(bool enabled)
    {
        updatePosition = enabled;
    }
    
    /// <summary>
    /// Set position offset from target
    /// </summary>
    public void SetPositionOffset(Vector3 offset)
    {
        positionOffset = offset;
    }
}
