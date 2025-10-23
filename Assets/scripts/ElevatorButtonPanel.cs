using UnityEngine;
using System.Collections;

/// <summary>
/// Smart elevator button panel that works inside the elevator.
/// - Shows all available floors for selection
/// - Press number keys (1-9) or interact with UI to select floor
/// - Press while moving → Play satisfying button sound (spam-friendly!)
/// - Integrates with ElevatorDoor system for automatic door control
/// </summary>
public class ElevatorButtonPanel : MonoBehaviour
{
    [Header("Elevator Reference")]
    [SerializeField] private ElevatorController elevatorController;
    [SerializeField] private LayeredHandAnimationController handAnimationController;
    
    [Header("Door Integration (Optional - Simple System)")]
    [Tooltip("Front doors (for entering at bottom, closed at top)")]
    [SerializeField] private ElevatorDoorSimple frontDoors;
    
    [Tooltip("Back doors (for exiting at top, closed at bottom)")]
    [SerializeField] private ElevatorDoorSimple backDoors;
    
    [SerializeField] private bool useDoors = false;
    [SerializeField] private float doorOpenDelay = 0.5f; // Delay before opening doors on arrival
    
    [Header("Interaction Settings")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private LayerMask playerLayer;
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip buttonPressSound;
    [SerializeField] private AudioClip buttonSpamSound; // Fun sound for spam clicks
    [SerializeField] private AudioClip cannotUseSound;
    
    [Header("Visual Feedback")]
    [SerializeField] private Renderer buttonRenderer;
    [SerializeField] private Material idleMaterial;
    [SerializeField] private Material pressedMaterial;
    [SerializeField] private Light buttonLight;
    [SerializeField] private Color idleColor = Color.white;
    [SerializeField] private Color pressedColor = Color.green;
    [SerializeField] private Color movingColor = Color.yellow;
    
    [Header("Interaction Settings")]
    [SerializeField] private bool showFloorPrompts = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool enableDebugLogs = false;
    
    private bool playerInRange = false;
    private Transform playerTransform = null;
    private bool isPressed = false;
    private float lastPressTime = 0f;
    private const float BUTTON_COOLDOWN = 0.3f; // Prevent super-spam
    private int[] availableFloors; // Cached available floor levels

    void Awake()
    {
        // Auto-find elevator controller if not assigned
        if (elevatorController == null)
        {
            elevatorController = FindObjectOfType<ElevatorController>();
            if (elevatorController == null)
            {
                Debug.LogError("[ElevatorButtonPanel] No ElevatorController found! Please assign one.");
            }
        }
        
        // Auto-find hand animation controller
        if (handAnimationController == null)
        {
            handAnimationController = FindObjectOfType<LayeredHandAnimationController>();
        }
        
        // No UI canvas needed - using CognitiveFeedManager
        
        // Setup audio
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D sound
        }
        
        // Cache available floors
        if (elevatorController != null)
        {
            availableFloors = elevatorController.GetAvailableFloorLevels();
        }
        
        // Subscribe to elevator arrival event
        ElevatorController.OnElevatorArrived += HandleElevatorArrival;
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        ElevatorController.OnElevatorArrived -= HandleElevatorArrival;
    }

    void Update()
    {
        CheckForPlayer();
        
        if (playerInRange)
        {
            ShowFloorSelectionPrompt();
            UpdateButtonVisual();
            
            // Check for number key presses to select floors
            CheckFloorSelectionInput();
        }
        else
        {
            HideFloorSelectionPrompt();
        }
    }
    
    /// <summary>
    /// Handle elevator arrival event - opens correct doors
    /// </summary>
    private void HandleElevatorArrival()
    {
        if (useDoors)
        {
            StartCoroutine(OpenDoorsOnArrival());
        }
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
    /// Check for floor selection input (number keys)
    /// </summary>
    private void CheckFloorSelectionInput()
    {
        if (elevatorController == null || availableFloors == null) return;
        
        // Map number keys 1-9 to floor selection
        for (int i = 0; i < availableFloors.Length && i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
            {
                SelectFloor(availableFloors[i]);
                break;
            }
        }
    }
    
    /// <summary>
    /// Select a specific floor to travel to
    /// </summary>
    public void SelectFloor(int floorLevel)
    {
        if (elevatorController == null) return;
        
        // Cooldown check (allow button spam, but with slight delay)
        if (Time.time - lastPressTime < BUTTON_COOLDOWN)
        {
            return;
        }
        lastPressTime = Time.time;
        
        // Check if elevator is moving
        if (elevatorController.IsMoving)
        {
            // FUN SPAM FEATURE! Play sound but don't do anything
            PlaySpamSound();
            StartCoroutine(ButtonPressEffect());
            
            if (enableDebugLogs)
                Debug.Log("[ElevatorButtonPanel] Button pressed while moving - SPAM MODE! 😄");
            
            return;
        }
        
        // Check if already at this floor
        if (elevatorController.CurrentFloorLevel == floorLevel)
        {
            if (enableDebugLogs)
                Debug.Log($"[ElevatorButtonPanel] Already at floor {floorLevel}");
            
            PlaySpamSound();
            return;
        }
        
        // Play right hand open door animation
        if (handAnimationController != null)
        {
            handAnimationController.PlayOpenDoorAnimation();
            if (enableDebugLogs)
                Debug.Log("[ElevatorButtonPanel] Playing right hand open door animation");
        }
        
        // Close doors if using door system
        if (useDoors)
        {
            if (frontDoors != null) frontDoors.CloseDoors();
            if (backDoors != null) backDoors.CloseDoors();
        }
        
        // Call elevator to selected floor
        elevatorController.CallElevatorToFloor(floorLevel);
        PlayButtonPressSound();
        StartCoroutine(ButtonPressEffect());
        
        if (enableDebugLogs)
            Debug.Log($"[ElevatorButtonPanel] Selected floor {floorLevel}");
    }

    /// <summary>
    /// Visual button press effect
    /// </summary>
    private IEnumerator ButtonPressEffect()
    {
        isPressed = true;
        
        // Change visual
        if (buttonRenderer != null && pressedMaterial != null)
        {
            buttonRenderer.material = pressedMaterial;
        }
        
        if (buttonLight != null)
        {
            buttonLight.color = pressedColor;
        }
        
        yield return new WaitForSeconds(0.2f);
        
        isPressed = false;
        UpdateButtonVisual();
    }

    /// <summary>
    /// Open the correct doors when elevator arrives at floor
    /// Uses per-floor door configuration from ElevatorController
    /// </summary>
    private IEnumerator OpenDoorsOnArrival()
    {
        yield return new WaitForSeconds(doorOpenDelay);
        
        if (elevatorController == null) yield break;
        
        // Get current floor and its door configuration
        int currentFloor = elevatorController.CurrentFloorLevel;
        ElevatorController.DoorConfiguration doorConfig = elevatorController.GetCurrentFloorDoorConfig();
        
        // Open doors based on configuration
        switch (doorConfig)
        {
            case ElevatorController.DoorConfiguration.FrontDoors:
                if (frontDoors != null)
                {
                    frontDoors.OpenDoors();
                    if (enableDebugLogs)
                        Debug.Log($"[ElevatorButtonPanel] Floor {currentFloor} - Opening FRONT doors");
                }
                break;
                
            case ElevatorController.DoorConfiguration.BackDoors:
                if (backDoors != null)
                {
                    backDoors.OpenDoors();
                    if (enableDebugLogs)
                        Debug.Log($"[ElevatorButtonPanel] Floor {currentFloor} - Opening BACK doors");
                }
                break;
                
            case ElevatorController.DoorConfiguration.BothDoors:
                if (frontDoors != null)
                {
                    frontDoors.OpenDoors();
                    if (enableDebugLogs)
                        Debug.Log($"[ElevatorButtonPanel] Floor {currentFloor} - Opening FRONT doors");
                }
                if (backDoors != null)
                {
                    backDoors.OpenDoors();
                    if (enableDebugLogs)
                        Debug.Log($"[ElevatorButtonPanel] Floor {currentFloor} - Opening BACK doors");
                }
                break;
        }
    }

    /// <summary>
    /// Update button visual based on elevator state
    /// </summary>
    private void UpdateButtonVisual()
    {
        if (elevatorController == null) return;
        
        if (isPressed)
        {
            // Already handled in ButtonPressEffect
            return;
        }
        
        if (elevatorController.IsMoving)
        {
            // Yellow/moving state
            if (buttonRenderer != null && idleMaterial != null)
            {
                buttonRenderer.material = idleMaterial;
                buttonRenderer.material.color = movingColor;
            }
            
            if (buttonLight != null)
            {
                buttonLight.color = movingColor;
            }
        }
        else
        {
            // Idle state
            if (buttonRenderer != null && idleMaterial != null)
            {
                buttonRenderer.material = idleMaterial;
                buttonRenderer.material.color = idleColor;
            }
            
            if (buttonLight != null)
            {
                buttonLight.color = idleColor;
            }
        }
    }

    private bool isShowingPrompt = false;
    
    /// <summary>
    /// Show floor selection prompt via CognitiveFeedManager
    /// </summary>
    private void ShowFloorSelectionPrompt()
    {
        if (!showFloorPrompts || isShowingPrompt) return;
        if (elevatorController == null || CognitiveFeedManager.Instance == null) return;
        
        int currentFloor = elevatorController.CurrentFloorLevel;
        
        if (availableFloors == null || availableFloors.Length == 0) return;
        
        // Build floor selection message
        string message = elevatorController.IsMoving ? "ELEVATOR MOVING..." : "SELECT FLOOR: ";
        
        if (!elevatorController.IsMoving)
        {
            for (int i = 0; i < availableFloors.Length; i++)
            {
                int floor = availableFloors[i];
                string floorText = floor >= 0 ? $"+{floor}" : floor.ToString();
                
                if (floor == currentFloor)
                {
                    message += $"[{i + 1}]{floorText}✓ ";
                }
                else
                {
                    message += $"[{i + 1}]{floorText} ";
                }
            }
        }
        
        CognitiveFeedManager.Instance.ShowPersistentMessage(message);
        isShowingPrompt = true;
    }
    
    /// <summary>
    /// Hide floor selection prompt
    /// </summary>
    private void HideFloorSelectionPrompt()
    {
        if (!isShowingPrompt) return;
        
        if (CognitiveFeedManager.Instance != null)
        {
            CognitiveFeedManager.Instance.HidePersistentMessage();
            isShowingPrompt = false;
        }
    }

    /// <summary>
    /// Play normal button press sound
    /// </summary>
    private void PlayButtonPressSound()
    {
        if (audioSource != null && buttonPressSound != null)
        {
            audioSource.PlayOneShot(buttonPressSound);
        }
    }

    /// <summary>
    /// Play fun spam sound (when clicking during movement)
    /// </summary>
    private void PlaySpamSound()
    {
        if (audioSource != null)
        {
            if (buttonSpamSound != null)
            {
                audioSource.PlayOneShot(buttonSpamSound);
            }
            else if (buttonPressSound != null)
            {
                // Fallback to normal sound with pitch variation
                audioSource.pitch = Random.Range(0.9f, 1.1f);
                audioSource.PlayOneShot(buttonPressSound);
                audioSource.pitch = 1f;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        
        // Draw interaction range
        Gizmos.color = playerInRange ? Color.green : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
        
        // Draw arrow pointing forward
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, transform.forward * 0.5f);
    }
}
