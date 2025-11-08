using UnityEngine;

/// <summary>
/// Simple eye tracking script that looks at the player when in range.
/// Attach to the eye GameObject on a sockle.
/// ONLY ROTATES - does NOT move position!
/// </summary>
public class EyeTracker : MonoBehaviour
{
    [Header("Tracking Settings")]
    [Tooltip("Maximum distance to track the player")]
    [SerializeField] private float trackingRange = 1000f;
    
    [Header("Eye Setup - IMPORTANT!")]
    [Tooltip("Drag the actual EYE MESH/BALL here (not the parent holder)")]
    [SerializeField] private Transform eyeToRotate;
    
    [Header("Rotation Constraints")]
    [Tooltip("Enable to allow full up/down rotation")]
    [SerializeField] private bool allowVerticalRotation = true;
    [Tooltip("Enable to allow full left/right rotation")]
    [SerializeField] private bool allowHorizontalRotation = true;
    
    [Header("Smoothing")]
    [Tooltip("How smoothly the eye rotates (lower = smoother)")]
    [SerializeField] private float rotationSpeed = 5f;
    
    [Header("Debug Visualization")]
    [Tooltip("Show forward direction gizmo")]
    [SerializeField] private bool showForwardGizmo = true;
    [SerializeField] private float gizmoLength = 50f;

    private Transform playerTransform;

    private void Start()
    {
        if (eyeToRotate == null)
        {
            Debug.LogError("EyeTracker: No eye assigned! Please drag the actual eye mesh into 'Eye To Rotate' field!");
            return;
        }
        
        Debug.Log($"EyeTracker on: {gameObject.name}, will rotate: {eyeToRotate.name} at {eyeToRotate.position}");
        
        // Get player via GameManager's AAAMovementController, otherwise find by tag
        AAAMovementController movementController = GameManager.Instance?.GetAAAMovementController();
        if (movementController != null)
        {
            playerTransform = movementController.transform;
            Debug.Log($"Found player at: {playerTransform.position}");
        }
        else
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                Debug.Log($"Found player by tag at: {playerTransform.position}");
            }
            else
            {
                Debug.LogWarning("EyeTracker: No player found!");
            }
        }
    }

    private void LateUpdate()
    {
        if (playerTransform == null || eyeToRotate == null) return;

        // Check distance to player (from EYE position, not parent holder)
        float distance = Vector3.Distance(eyeToRotate.position, playerTransform.position);
        
        if (distance <= trackingRange)
        {
            // Calculate look direction (from EYE to player)
            Vector3 direction = (playerTransform.position - eyeToRotate.position).normalized;
            
            // Debug: Log what we're trying to do
            Debug.Log($"Eye at: {eyeToRotate.position}, Player at: {playerTransform.position}");
            Debug.Log($"Direction to player: {direction}");
            Debug.Log($"Eye current rotation: {eyeToRotate.rotation.eulerAngles}");
            
            // Create target rotation - look at the player
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            Debug.Log($"Target rotation: {targetRotation.eulerAngles}");
            
            // Apply rotation constraints if needed
            if (!allowVerticalRotation || !allowHorizontalRotation)
            {
                Vector3 eulerAngles = targetRotation.eulerAngles;
                Vector3 currentEuler = eyeToRotate.rotation.eulerAngles;
                
                if (!allowVerticalRotation)
                {
                    eulerAngles.x = currentEuler.x;
                }
                
                if (!allowHorizontalRotation)
                {
                    eulerAngles.y = currentEuler.y;
                }
                
                targetRotation = Quaternion.Euler(eulerAngles);
            }
            
            // Smoothly rotate the EYE towards target (ONLY ROTATION!)
            eyeToRotate.rotation = Quaternion.Slerp(
                eyeToRotate.rotation, 
                targetRotation, 
                rotationSpeed * Time.deltaTime
            );
            
            // Debug.DrawRay removed for performance
        }
    }

    // Visual helpers in editor
    private void OnDrawGizmos()
    {
        if (showForwardGizmo && eyeToRotate != null)
        {
            // Show forward direction (where eye is looking) FROM THE EYE
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(eyeToRotate.position, eyeToRotate.forward * gizmoLength);
            
            // Draw a small sphere at the end
            Gizmos.DrawWireSphere(eyeToRotate.position + eyeToRotate.forward * gizmoLength, 10f);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (eyeToRotate == null) return;
        
        // Show tracking range FROM THE EYE
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(eyeToRotate.position, trackingRange);
        
        // Show forward direction in bright green when selected
        Gizmos.color = Color.green;
        Gizmos.DrawRay(eyeToRotate.position, eyeToRotate.forward * gizmoLength);
    }
}
