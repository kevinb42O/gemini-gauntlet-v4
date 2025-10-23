using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Smooth carriage follower that eliminates jerky movement
/// </summary>
[System.Serializable]
public class SmoothCarriageFollower
{
    public Transform carriageTransform;
    public float followDistance;
    
    // Smooth movement parameters
    [SerializeField] private float smoothingSpeed = 5f;
    [SerializeField] private float rotationSmoothing = 3f;
    [SerializeField] private bool usePhysicsSmoothing = true;
    
    // Internal state
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private Vector3 velocity = Vector3.zero;
    
    public SmoothCarriageFollower(Transform carriage, float distance)
    {
        carriageTransform = carriage;
        followDistance = distance;
        
        if (carriageTransform != null)
        {
            targetPosition = carriageTransform.position;
            targetRotation = carriageTransform.rotation;
        }
    }
    
    /// <summary>
    /// Update carriage with smooth following using train history
    /// </summary>
    public void UpdateSmoothFollow(List<Vector3> positionHistory, List<Quaternion> rotationHistory, float trainSpeed)
    {
        if (carriageTransform == null || positionHistory.Count < 2) return;
        
        // Get target position and rotation from smooth history
        Quaternion historyRotation;
        Vector3 historyPosition = GetInterpolatedHistoryPosition(positionHistory, rotationHistory, out historyRotation);
        
        // Smooth approach to target
        if (usePhysicsSmoothing)
        {
            // Physics-based smooth damping
            targetPosition = Vector3.SmoothDamp(carriageTransform.position, historyPosition, ref velocity, 1f / smoothingSpeed);
            targetRotation = Quaternion.Slerp(carriageTransform.rotation, historyRotation, rotationSmoothing * Time.fixedDeltaTime);
        }
        else
        {
            // Direct interpolation
            float positionLerpSpeed = smoothingSpeed * Time.fixedDeltaTime;
            float rotationLerpSpeed = rotationSmoothing * Time.fixedDeltaTime;
            
            targetPosition = Vector3.Lerp(carriageTransform.position, historyPosition, positionLerpSpeed);
            targetRotation = Quaternion.Slerp(carriageTransform.rotation, historyRotation, rotationLerpSpeed);
        }
        
        // Apply smooth movement
        carriageTransform.position = targetPosition;
        carriageTransform.rotation = targetRotation;
    }
    
    /// <summary>
    /// Get interpolated position from history based on follow distance
    /// </summary>
    private Vector3 GetInterpolatedHistoryPosition(List<Vector3> positionHistory, List<Quaternion> rotationHistory, out Quaternion rotation)
    {
        rotation = Quaternion.identity;
        
        if (positionHistory.Count < 2)
        {
            if (positionHistory.Count == 1)
            {
                rotation = rotationHistory[0];
                return positionHistory[0];
            }
            return Vector3.zero;
        }
        
        // Calculate distance-based history lookup
        float accumulatedDistance = 0f;
        int historyIndex = positionHistory.Count - 1;
        
        // Walk backwards through history until we reach the follow distance
        for (int i = positionHistory.Count - 1; i > 0; i--)
        {
            float segmentDistance = Vector3.Distance(positionHistory[i], positionHistory[i - 1]);
            
            if (accumulatedDistance + segmentDistance >= followDistance)
            {
                // Found the segment containing our target distance
                float remainingDistance = followDistance - accumulatedDistance;
                float segmentProgress = remainingDistance / segmentDistance;
                
                // Interpolate between the two points
                Vector3 pos1 = positionHistory[i];
                Vector3 pos2 = positionHistory[i - 1];
                Quaternion rot1 = rotationHistory[i];
                Quaternion rot2 = rotationHistory[i - 1];
                
                rotation = Quaternion.Slerp(rot1, rot2, segmentProgress);
                return Vector3.Lerp(pos1, pos2, segmentProgress);
            }
            
            accumulatedDistance += segmentDistance;
        }
        
        // If we couldn't find the exact distance, use the oldest available position
        rotation = rotationHistory[0];
        return positionHistory[0];
    }
    
    /// <summary>
    /// Set smoothing parameters
    /// </summary>
    public void SetSmoothingParameters(float smoothSpeed, float rotSpeed, bool usePhysics)
    {
        smoothingSpeed = smoothSpeed;
        rotationSmoothing = rotSpeed;
        usePhysicsSmoothing = usePhysics;
    }
    
    /// <summary>
    /// Get current follow distance
    /// </summary>
    public float GetFollowDistance()
    {
        return followDistance;
    }
    
    /// <summary>
    /// Set new follow distance
    /// </summary>
    public void SetFollowDistance(float newDistance)
    {
        followDistance = newDistance;
    }
}
