using UnityEngine;
using System.Collections;

/// <summary>
/// Simple elevator doors that travel WITH the elevator.
/// No keycard needed - just open/close based on floor.
/// Front doors for entering, back doors for exiting.
/// </summary>
public class ElevatorDoorSimple : MonoBehaviour
{
    [Header("Door References")]
    [Tooltip("Left door that slides left")]
    public Transform leftDoor;
    
    [Tooltip("Right door that slides right")]
    public Transform rightDoor;
    
    [Header("Door Settings")]
    [Tooltip("How far each door slides when opening")]
    public float slideDistance = 1.5f;
    
    [Tooltip("How fast doors open/close")]
    public float doorSpeed = 2f;
    
    [Header("Audio (Optional)")]
    public AudioSource audioSource;
    public AudioClip doorOpenSound;
    public AudioClip doorCloseSound;
    
    [Header("Debug")]
    public bool showDebugGizmos = true;
    
    [Header("Runtime Testing (EDITOR ONLY)")]
    [Tooltip("Press O to OPEN doors in Play mode")]
    public KeyCode testOpenKey = KeyCode.O;
    [Tooltip("Press C to CLOSE doors in Play mode")]
    public KeyCode testCloseKey = KeyCode.C;
    public bool enableRuntimeTesting = true;
    
    // State
    private bool isOpen = false;
    private bool isAnimating = false;
    private Vector3 leftDoorClosedPosition;
    private Vector3 rightDoorClosedPosition;

    void Start()
    {
        // Store closed positions
        if (leftDoor != null)
        {
            leftDoorClosedPosition = leftDoor.localPosition;
        }
        
        if (rightDoor != null)
        {
            rightDoorClosedPosition = rightDoor.localPosition;
        }
        
        Debug.Log($"[ElevatorDoorSimple] {gameObject.name} initialized - Press O to OPEN, C to CLOSE (if testing enabled)");
    }
    
    void Update()
    {
        // Runtime testing in editor
        if (enableRuntimeTesting && Application.isEditor)
        {
            if (Input.GetKeyDown(testOpenKey))
            {
                Debug.Log($"[ElevatorDoorSimple] TEST: Opening {gameObject.name}");
                OpenDoors();
            }
            
            if (Input.GetKeyDown(testCloseKey))
            {
                Debug.Log($"[ElevatorDoorSimple] TEST: Closing {gameObject.name}");
                CloseDoors();
            }
        }
    }

    /// <summary>
    /// Open the doors
    /// </summary>
    public void OpenDoors()
    {
        if (isOpen || isAnimating) return;
        
        StartCoroutine(AnimateDoors(true));
    }

    /// <summary>
    /// Close the doors
    /// </summary>
    public void CloseDoors()
    {
        if (!isOpen || isAnimating) return;
        
        StartCoroutine(AnimateDoors(false));
    }

    /// <summary>
    /// Animate doors opening or closing
    /// </summary>
    private IEnumerator AnimateDoors(bool opening)
    {
        isAnimating = true;
        
        // Play sound
        if (audioSource != null)
        {
            AudioClip clip = opening ? doorOpenSound : doorCloseSound;
            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
        
        // Calculate target positions IN LOCAL SPACE!
        Vector3 leftTarget, rightTarget;
        
        if (opening)
        {
            // Open: slide doors apart (LOCAL Z axis)
            leftTarget = leftDoorClosedPosition + new Vector3(0, 0, slideDistance); // LEFT door slides LEFT (+Z)
            rightTarget = rightDoorClosedPosition + new Vector3(0, 0, -slideDistance); // RIGHT door slides RIGHT (-Z)
        }
        else
        {
            // Close: return to closed positions
            leftTarget = leftDoorClosedPosition;
            rightTarget = rightDoorClosedPosition;
        }
        
        // Get starting positions
        Vector3 leftStart = leftDoor != null ? leftDoor.localPosition : leftDoorClosedPosition;
        Vector3 rightStart = rightDoor != null ? rightDoor.localPosition : rightDoorClosedPosition;
        
        // Animate
        float elapsed = 0f;
        float duration = 1f / doorSpeed;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            // Smooth step
            t = t * t * (3f - 2f * t);
            
            if (leftDoor != null)
            {
                leftDoor.localPosition = Vector3.Lerp(leftStart, leftTarget, t);
            }
            
            if (rightDoor != null)
            {
                rightDoor.localPosition = Vector3.Lerp(rightStart, rightTarget, t);
            }
            
            yield return null;
        }
        
        // Ensure exact final positions
        if (leftDoor != null)
        {
            leftDoor.localPosition = leftTarget;
        }
        
        if (rightDoor != null)
        {
            rightDoor.localPosition = rightTarget;
        }
        
        isOpen = opening;
        isAnimating = false;
        
        Debug.Log($"[ElevatorDoorSimple] {gameObject.name} {(opening ? "OPENED" : "CLOSED")}");
    }

    /// <summary>
    /// Check if doors are currently open
    /// </summary>
    public bool IsOpen => isOpen;
    
    /// <summary>
    /// Check if doors are currently animating
    /// </summary>
    public bool IsAnimating => isAnimating;

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        
        // Draw door positions and slide directions
        if (leftDoor != null)
        {
            Gizmos.color = isOpen ? Color.green : Color.cyan;
            Gizmos.DrawWireCube(leftDoor.position, Vector3.one * 0.5f);
            
            // Draw LEFT slide direction (local +Z = LEFT)
            Gizmos.color = Color.red;
            Vector3 leftSlideDir = leftDoor.TransformDirection(Vector3.forward); // forward = +Z = LEFT
            Gizmos.DrawRay(leftDoor.position, leftSlideDir * slideDistance);
            Gizmos.DrawSphere(leftDoor.position + leftSlideDir * slideDistance, 0.2f);
        }
        
        if (rightDoor != null)
        {
            Gizmos.color = isOpen ? Color.green : Color.cyan;
            Gizmos.DrawWireCube(rightDoor.position, Vector3.one * 0.5f);
            
            // Draw RIGHT slide direction (local -Z = RIGHT)
            Gizmos.color = Color.blue;
            Vector3 rightSlideDir = rightDoor.TransformDirection(Vector3.back); // back = -Z = RIGHT
            Gizmos.DrawRay(rightDoor.position, rightSlideDir * slideDistance);
            Gizmos.DrawSphere(rightDoor.position + rightSlideDir * slideDistance, 0.2f);
        }
        
        // Draw labels
        #if UNITY_EDITOR
        if (leftDoor != null)
        {
            UnityEditor.Handles.Label(leftDoor.position + Vector3.up * 2, $"{gameObject.name}\n{(isOpen ? "OPEN" : "CLOSED")}");
        }
        #endif
    }
}
