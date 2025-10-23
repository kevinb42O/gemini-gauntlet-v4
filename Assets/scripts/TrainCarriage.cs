using UnityEngine;

public class TrainCarriage : MonoBehaviour
{
    [Header("Carriage Configuration")]
    [SerializeField] private float followSmoothness = 5f;
    [SerializeField] private float rotationSmoothness = 3f;
    [SerializeField] private float maxFollowDistance = 15f;
    [SerializeField] private float minFollowDistance = 5f;
    
    [Header("Physics Settings")]
    [SerializeField] private bool usePhysicsConstraints = true;
    [SerializeField] private float springForce = 50f;
    [SerializeField] private float dampingForce = 10f;
    
    [Header("Visual Settings")]
    [SerializeField] private bool showConnectionLine = true;
    [SerializeField] private LineRenderer connectionLine;
    [SerializeField] private Transform connectionPoint;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    // Private variables
    private SpaceTrainController trainController;
    private float followDistance;
    private Vector3 targetPosition;
    private Vector3 currentVelocity;
    private bool isInitialized = false;
    
    // Connection tracking
    private TrainCarriage previousCarriage; // The carriage in front of this one
    private Vector3 lastValidPosition;
    
    void Start()
    {
        // Store initial position as fallback
        lastValidPosition = transform.position;
        
        // Setup connection line if provided
        SetupConnectionLine();
    }
    
    void Update()
    {
        if (!isInitialized) return;
        
        UpdateCarriage();
    }
    
    /// <summary>
    /// Initialize this carriage with train controller and follow distance
    /// </summary>
    public void Initialize(SpaceTrainController controller, float distance)
    {
        trainController = controller;
        followDistance = Mathf.Clamp(distance, minFollowDistance, maxFollowDistance);
        isInitialized = true;
        
        Debug.Log($"[TrainCarriage] {name} initialized with follow distance: {followDistance}");
    }
    
    /// <summary>
    /// Set the carriage that this one should follow (for carriage-to-carriage following)
    /// </summary>
    public void SetPreviousCarriage(TrainCarriage previous)
    {
        previousCarriage = previous;
    }
    
    /// <summary>
    /// Main carriage update logic
    /// </summary>
    public void UpdateCarriage()
    {
        if (!isInitialized) return;
        
        // Calculate target position
        CalculateTargetPosition();
        
        // Move towards target
        MoveToTarget();
        
        // Update rotation to face movement direction
        UpdateRotation();
        
        // Update visual connection
        UpdateConnectionLine();
        
        // Store valid position
        lastValidPosition = transform.position;
    }
    
    /// <summary>
    /// Calculate where this carriage should be positioned
    /// </summary>
    private void CalculateTargetPosition()
    {
        if (previousCarriage != null)
        {
            // Follow the previous carriage
            Vector3 carriageDirection = (previousCarriage.transform.position - transform.position).normalized;
            targetPosition = previousCarriage.transform.position - carriageDirection * followDistance;
        }
        else if (trainController != null)
        {
            // Follow the main train using new smooth method
            Quaternion targetRotation;
            targetPosition = trainController.GetSmoothHistoricalPosition(followDistance, out targetRotation);
        }
        else
        {
            // No valid target, stay in place
            targetPosition = transform.position;
            return;
        }
        
        // Validate target position
        if (IsValidPosition(targetPosition))
        {
            // Apply physics constraints if enabled
            if (usePhysicsConstraints)
            {
                ApplyPhysicsConstraints();
            }
        }
        else
        {
            // Use last valid position as fallback
            targetPosition = lastValidPosition;
            Debug.LogWarning($"[TrainCarriage] Invalid target position calculated for {name}, using fallback");
        }
    }
    
    /// <summary>
    /// Move the carriage towards its target position
    /// </summary>
    private void MoveToTarget()
    {
        // Calculate movement
        Vector3 direction = (targetPosition - transform.position);
        float distance = direction.magnitude;
        
        // Only move if there's a meaningful distance
        if (distance > 0.1f)
        {
            // Smooth movement using various methods based on distance
            Vector3 newPosition;
            
            if (distance > followDistance * 1.5f)
            {
                // Large distance: immediate snap to prevent stretching
                newPosition = targetPosition;
                Debug.Log($"[TrainCarriage] {name} snapping to position (distance: {distance:F2})");
            }
            else if (distance > 2f)
            {
                // Medium distance: fast lerp
                newPosition = Vector3.Lerp(transform.position, targetPosition, followSmoothness * 2f * Time.deltaTime);
            }
            else
            {
                // Small distance: smooth lerp
                newPosition = Vector3.Lerp(transform.position, targetPosition, followSmoothness * Time.deltaTime);
            }
            
            // Calculate velocity for physics effects
            currentVelocity = (newPosition - transform.position) / Time.deltaTime;
            
            // Apply the movement
            transform.position = newPosition;
        }
        else
        {
            currentVelocity = Vector3.zero;
        }
    }
    
    /// <summary>
    /// Update carriage rotation to face movement direction
    /// </summary>
    private void UpdateRotation()
    {
        if (currentVelocity.magnitude > 0.1f)
        {
            // Face the movement direction
            Quaternion targetRotation = Quaternion.LookRotation(currentVelocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothness * Time.deltaTime);
        }
        else if (previousCarriage != null)
        {
            // If not moving, try to align with previous carriage
            Vector3 directionToPrevious = (previousCarriage.transform.position - transform.position).normalized;
            if (directionToPrevious.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPrevious);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothness * 0.5f * Time.deltaTime);
            }
        }
    }
    
    /// <summary>
    /// Apply physics-based constraints for more realistic movement
    /// </summary>
    private void ApplyPhysicsConstraints()
    {
        Vector3 followTarget = previousCarriage != null ? previousCarriage.transform.position : trainController.transform.position;
        
        // Calculate spring force to maintain proper distance
        Vector3 connectionVector = followTarget - transform.position;
        float currentDistance = connectionVector.magnitude;
        float targetDistance = followDistance;
        
        // Spring force proportional to distance difference
        float distanceDifference = currentDistance - targetDistance;
        Vector3 springForceVector = connectionVector.normalized * distanceDifference * springForce;
        
        // Damping force to reduce oscillation
        Vector3 dampingForceVector = -currentVelocity * dampingForce;
        
        // Combine forces
        Vector3 totalForce = springForceVector + dampingForceVector;
        
        // Apply force to modify target position
        targetPosition += totalForce * Time.deltaTime * Time.deltaTime;
    }
    
    /// <summary>
    /// Check if a position is valid (not NaN, not infinite, etc.)
    /// </summary>
    private bool IsValidPosition(Vector3 position)
    {
        return !float.IsNaN(position.x) && !float.IsNaN(position.y) && !float.IsNaN(position.z) &&
               !float.IsInfinity(position.x) && !float.IsInfinity(position.y) && !float.IsInfinity(position.z);
    }
    
    /// <summary>
    /// Setup the visual connection line
    /// </summary>
    private void SetupConnectionLine()
    {
        if (connectionLine == null)
        {
            // Try to find LineRenderer component
            connectionLine = GetComponent<LineRenderer>();
        }
        
        if (connectionLine != null && showConnectionLine)
        {
            connectionLine.positionCount = 2;
            connectionLine.startWidth = 0.1f;
            connectionLine.endWidth = 0.1f;
            connectionLine.material = CreateConnectionMaterial();
            connectionLine.useWorldSpace = true;
        }
    }
    
    /// <summary>
    /// Update the visual connection line
    /// </summary>
    private void UpdateConnectionLine()
    {
        if (connectionLine == null || !showConnectionLine || !isInitialized) return;
        
        Vector3 startPoint = connectionPoint != null ? connectionPoint.position : transform.position;
        Vector3 endPoint;
        
        if (previousCarriage != null)
        {
            Transform previousConnectionPoint = previousCarriage.connectionPoint;
            endPoint = previousConnectionPoint != null ? previousConnectionPoint.position : previousCarriage.transform.position;
        }
        else if (trainController != null)
        {
            endPoint = trainController.transform.position;
        }
        else
        {
            // No connection target
            connectionLine.enabled = false;
            return;
        }
        
        connectionLine.enabled = true;
        connectionLine.SetPosition(0, startPoint);
        connectionLine.SetPosition(1, endPoint);
        
        // Color based on connection strength
        float distance = Vector3.Distance(startPoint, endPoint);
        float normalizedDistance = Mathf.Clamp01(distance / (followDistance * 2f));
        Color connectionColor = Color.Lerp(Color.green, Color.red, normalizedDistance);
        connectionLine.startColor = connectionColor;
        connectionLine.endColor = connectionColor;
    }
    
    /// <summary>
    /// Create a material for the connection line
    /// </summary>
    private Material CreateConnectionMaterial()
    {
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = Color.cyan;
        return mat;
    }
    
    /// <summary>
    /// Get current carriage velocity
    /// </summary>
    public Vector3 GetCarriageVelocity()
    {
        return currentVelocity;
    }
    
    /// <summary>
    /// Get the connection point position
    /// </summary>
    public Vector3 GetConnectionPoint()
    {
        return connectionPoint != null ? connectionPoint.position : transform.position;
    }
    
    /// <summary>
    /// Emergency reset to a safe position
    /// </summary>
    public void EmergencyReset()
    {
        if (trainController != null)
        {
            Quaternion resetRotation;
            transform.position = trainController.GetSmoothHistoricalPosition(followDistance, out resetRotation);
            currentVelocity = Vector3.zero;
            Debug.Log($"[TrainCarriage] Emergency reset performed for {name}");
        }
    }
    
    // Debug visualization
    void OnDrawGizmos()
    {
        if (!showDebugInfo) return;
        
        // Draw carriage bounds
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 2f);
        
        // Draw follow distance
        if (isInitialized)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, followDistance);
            
            // Draw target position
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(targetPosition, 0.5f);
            
            // Draw velocity vector
            if (currentVelocity.magnitude > 0.1f)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(transform.position, currentVelocity.normalized * 2f);
            }
        }
        
        // Draw connection point
        if (connectionPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(connectionPoint.position, 0.3f);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Additional debug info when selected
        if (isInitialized && showDebugInfo)
        {
            // Draw connection to follow target
            Gizmos.color = Color.magenta;
            if (previousCarriage != null)
            {
                Gizmos.DrawLine(transform.position, previousCarriage.transform.position);
            }
            else if (trainController != null)
            {
                Gizmos.DrawLine(transform.position, trainController.transform.position);
            }
        }
    }
}
