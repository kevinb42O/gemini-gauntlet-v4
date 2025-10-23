using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Interactive button to call the elevator.
/// Uses the centralized Controls.Interact key for interaction.
/// </summary>
public class ElevatorCallButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ElevatorController elevatorController;
    [SerializeField] private LayeredHandAnimationController handAnimationController;
    
    [Header("Floor Configuration")]
    [Tooltip("The floor level this button calls the elevator to (e.g., -1, 0, 1, 2)")]
    [SerializeField] private int floorLevel = 0;
    
    [Header("Interaction Settings")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private LayerMask playerLayer;
    
    [Header("Interaction Prompt")]
    [SerializeField] private string interactionPrompt = "Press [E] to call elevator to floor {0}";
    
    [Header("Visual Feedback")]
    [SerializeField] private Renderer buttonRenderer;
    [SerializeField] private Material idleMaterial;
    [SerializeField] private Material activeMaterial;
    [SerializeField] private Material pressedMaterial;
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip buttonPressSound;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool enableDebugLogs = true;
    
    private bool playerInRange = false;
    private Transform playerTransform = null;
    private bool isPressed = false;

    void Awake()
    {
        // No UI canvas needed - using CognitiveFeedManager
        
        // Validate elevator reference
        if (elevatorController == null)
        {
            elevatorController = FindObjectOfType<ElevatorController>();
            if (elevatorController == null)
            {
                Debug.LogError("[ElevatorCallButton] No ElevatorController found! Please assign one.");
            }
        }
        
        // Auto-find hand animation controller
        if (handAnimationController == null)
        {
            handAnimationController = FindObjectOfType<LayeredHandAnimationController>();
        }
    }

    void Update()
    {
        CheckForPlayer();
        
        if (playerInRange && !isPressed)
        {
            // Show interaction prompt via CognitiveFeedManager
            ShowInteractionPrompt();
            UpdateButtonVisual();
            
            // Check for interact key
            if (Input.GetKeyDown(Controls.Interact))
            {
                CallElevator();
            }
        }
        else if (!playerInRange)
        {
            HideInteractionPrompt();
        }
        
        // Update button visual based on elevator status
        UpdateButtonVisual();
    }

    /// <summary>
    /// Check if player is in interaction range
    /// </summary>
    private void CheckForPlayer()
    {
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, interactionRange, playerLayer);
        
        bool foundPlayer = false;
        foreach (Collider col in nearbyColliders)
        {
            if (col.CompareTag("Player") || col.GetComponent<CharacterController>() != null)
            {
                playerInRange = true;
                playerTransform = col.transform;
                foundPlayer = true;
                break;
            }
        }
        
        if (!foundPlayer)
        {
            playerInRange = false;
            playerTransform = null;
        }
    }

    /// <summary>
    /// Call the elevator
    /// </summary>
    private void CallElevator()
    {
        if (elevatorController == null)
        {
            Debug.LogError("[ElevatorCallButton] Cannot call elevator - no controller assigned!");
            return;
        }
        
        // Removed the IsMoving check - ElevatorController handles queueing if it's moving
        // This allows players to call the elevator even when it's in transit
        
        // Play right hand open door animation
        if (handAnimationController != null)
        {
            handAnimationController.PlayOpenDoorAnimation();
            if (enableDebugLogs)
                Debug.Log("[ElevatorCallButton] Playing right hand open door animation");
        }
        
        // Play button press sound
        if (audioSource != null && buttonPressSound != null)
        {
            audioSource.PlayOneShot(buttonPressSound);
        }
        
        // Visual feedback
        isPressed = true;
        StartCoroutine(ResetButtonAfterDelay(0.5f));
        
        // Call elevator to this floor
        elevatorController.CallElevatorToFloor(floorLevel);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[ElevatorCallButton] Called elevator to floor {floorLevel}.");
        }
    }

    /// <summary>
    /// Reset button pressed state after delay
    /// </summary>
    private System.Collections.IEnumerator ResetButtonAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isPressed = false;
    }

    private bool isShowingPrompt = false;
    
    /// <summary>
    /// Show interaction prompt via CognitiveFeedManager
    /// </summary>
    private void ShowInteractionPrompt()
    {
        if (isShowingPrompt) return;
        
        if (CognitiveFeedManager.Instance != null)
        {
            string keyName = Controls.Interact.ToString();
            string floorText = floorLevel >= 0 ? $"+{floorLevel}" : floorLevel.ToString();
            string message = $"Press [{keyName}] to call elevator to floor {floorText}";
            CognitiveFeedManager.Instance.ShowPersistentMessage(message);
            isShowingPrompt = true;
        }
    }
    
    /// <summary>
    /// Hide interaction prompt
    /// </summary>
    private void HideInteractionPrompt()
    {
        if (!isShowingPrompt) return;
        
        if (CognitiveFeedManager.Instance != null)
        {
            CognitiveFeedManager.Instance.HidePersistentMessage();
            isShowingPrompt = false;
        }
    }

    /// <summary>
    /// Update button visual based on state
    /// </summary>
    private void UpdateButtonVisual()
    {
        if (buttonRenderer == null) return;
        
        if (isPressed && pressedMaterial != null)
        {
            buttonRenderer.material = pressedMaterial;
        }
        else if (playerInRange && activeMaterial != null)
        {
            buttonRenderer.material = activeMaterial;
        }
        else if (idleMaterial != null)
        {
            buttonRenderer.material = idleMaterial;
        }
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        
        // Draw interaction range
        Gizmos.color = playerInRange ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
        
        // Draw button floor level indicator
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2f);
        
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2.5f, $"Call Floor {floorLevel}");
        #endif
    }
}
