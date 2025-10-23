using UnityEngine;

/// <summary>
/// Continuously rotates the emit point to always look towards the center of the screen
/// with complete free rotation in all axes.
/// </summary>
public class EmitPointScreenCenter : MonoBehaviour
{
    [Header("Screen Center Targeting")]
    [SerializeField] private bool enableRotation = true;
    [SerializeField] private float rotationSpeed = 10f;
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
    
    void Update()
    {
        if (!enableRotation || _mainCamera == null) return;
        
        RotateTowardsScreenCenter();
    }
    
    void RotateTowardsScreenCenter()
    {
        // Calculate screen center world position
        Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        Ray screenCenterRay = _mainCamera.ScreenPointToRay(screenCenter);
        _screenCenterWorldPos = screenCenterRay.origin + (screenCenterRay.direction * targetDistance);
        
        // Calculate direction from emit point to screen center
        Vector3 directionToCenter = (_screenCenterWorldPos - transform.position).normalized;
        
        // Create target rotation
        Quaternion targetRotation = Quaternion.LookRotation(directionToCenter);
        
        // Apply rotation (instant or smooth)
        if (rotationSpeed <= 0f)
        {
            // Instant rotation
            transform.rotation = targetRotation;
        }
        else
        {
            // Smooth rotation
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 
                rotationSpeed * Time.deltaTime);
        }
        
        // Debug visualization
        if (showDebugRay)
        {
            Debug.DrawRay(transform.position, directionToCenter * targetDistance, debugRayColor);
            Debug.DrawLine(transform.position, _screenCenterWorldPos, debugRayColor);
        }
    }
    
    /// <summary>
    /// Instantly snap to screen center direction (useful for initialization)
    /// </summary>
    public void SnapToScreenCenter()
    {
        if (_mainCamera == null) return;
        
        Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        Ray screenCenterRay = _mainCamera.ScreenPointToRay(screenCenter);
        _screenCenterWorldPos = screenCenterRay.origin + (screenCenterRay.direction * targetDistance);
        
        Vector3 directionToCenter = (_screenCenterWorldPos - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(directionToCenter);
        
        Debug.Log("[EmitPointScreenCenter] Snapped to screen center direction");
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
    /// Get the current direction to screen center
    /// </summary>
    public Vector3 GetScreenCenterDirection()
    {
        if (_mainCamera == null) return Vector3.forward;
        
        Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        Ray screenCenterRay = _mainCamera.ScreenPointToRay(screenCenter);
        Vector3 screenCenterWorldPos = screenCenterRay.origin + (screenCenterRay.direction * targetDistance);
        
        return (screenCenterWorldPos - transform.position).normalized;
    }
}
