using UnityEngine;

/// <summary>
/// ✅ SINGLE SOURCE OF TRUTH for emit point rotation.
/// Continuously rotates the emit point to EXACTLY match camera forward direction.
/// Uses INSTANT rotation (no lag) to ensure perfect synchronization with particle spawn direction.
/// 
/// CRITICAL FIX: Removed smooth rotation (Slerp) that caused dual-hit bug where particles
/// spawned in camera direction but emit point was still rotating, creating mismatch.
/// 
/// This script is the ONLY system that should control emit point rotation!
/// </summary>
public class EmitPointScreenCenter : MonoBehaviour
{
    [Header("Screen Center Targeting")]
    [SerializeField] private bool enableRotation = true;
    [SerializeField] private float rotationSpeed = 0f; // 0 = INSTANT rotation (no lag) - CRITICAL for shooting accuracy
    [SerializeField] private float targetDistance = 100f; // Distance to project screen center
    
    [Header("Debug")]
    [SerializeField] private bool showDebugRay = false;
    [SerializeField] private Color debugRayColor = Color.red;
    
    private Camera _mainCamera;
    private Vector3 _screenCenterWorldPos;
    
    void Start()
    {
        // Find main camera
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            _mainCamera = FindFirstObjectByType<Camera>();
        }
        
        if (_mainCamera == null)
        {
            Debug.LogError("[EmitPointScreenCenter] No camera found! Script will not function.");
            enabled = false;
            return;
        }
        
        Debug.Log($"[EmitPointScreenCenter] Successfully found camera: {_mainCamera.name}");
    }
    
    void LateUpdate()
    {
        if (!enableRotation || _mainCamera == null) return;
        
        // ✅ CRITICAL: LateUpdate runs AFTER all animations and Update() calls
        // This ensures emit point ALWAYS points at camera forward, overriding hand animations
        RotateTowardsScreenCenter();
    }
    
    void RotateTowardsScreenCenter()
    {
        // ✅ CRITICAL FIX: Use CAMERA FORWARD directly for perfect synchronization
        // This OVERRIDES all hand animations that try to rotate emit points
        // LateUpdate() ensures this runs AFTER animator, so animations can't interfere
        // No more dual-hit bug from animation/rotation conflicts!
        Vector3 cameraForward = _mainCamera.transform.forward;
        
        // Create target rotation directly from camera forward
        Quaternion targetRotation = Quaternion.LookRotation(cameraForward);
        
        // ✅ ALWAYS USE INSTANT ROTATION - no lag, no dual hits
        // Smooth rotation (Slerp) causes desync between emit point and actual fire direction
        transform.rotation = targetRotation;
        
        // Debug visualization
        if (showDebugRay)
        {
            // Debug.DrawRay removed for performance
        }
    }
    
    /// <summary>
    /// Instantly snap to camera forward direction (useful for initialization)
    /// </summary>
    public void SnapToScreenCenter()
    {
        if (_mainCamera == null) return;
        
        // ✅ Use camera forward directly
        Vector3 cameraForward = _mainCamera.transform.forward;
        transform.rotation = Quaternion.LookRotation(cameraForward);
        
        Debug.Log("[EmitPointScreenCenter] Snapped to camera forward direction");
    }
    
    /// <summary>
    /// Enable/disable the rotation behavior
    /// </summary>
    public void SetRotationEnabled(bool enabled)
    {
        enableRotation = enabled;
        Debug.Log($"[EmitPointScreenCenter] Rotation enabled: {enabled}");
    }
    
    /// <summary>
    /// Get the current camera forward direction (single source of truth)
    /// </summary>
    public Vector3 GetScreenCenterDirection()
    {
        if (_mainCamera == null) return Vector3.forward;
        
        // ✅ Return camera forward directly - matches emit point rotation exactly
        return _mainCamera.transform.forward;
    }
}
