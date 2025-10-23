using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GeminiGauntlet.Audio;

/// <summary>
/// ElevatorDoor.cs - Dual sliding elevator doors that require an elevator keycard
/// Features a 20-second "arrival" delay after using the keycard before doors open
/// One-time use keycard that is consumed on use
/// </summary>
[RequireComponent(typeof(Collider))]
public class ElevatorDoor : MonoBehaviour
{
    [Header("Elevator Keycard Requirement")]
    [Tooltip("The elevator keycard required to call the elevator")]
    public ElevatorKeycardData requiredKeycard;
    
    [Header("Door References")]
    [Tooltip("Left door that slides to the left")]
    public Transform leftDoor;
    
    [Tooltip("Right door that slides to the right")]
    public Transform rightDoor;
    
    [Header("Door Settings")]
    [Tooltip("AUTO-DETECT each door's size and slide exactly that distance? (Recommended)")]
    public bool useAutoSlideDistance = true;
    
    [Tooltip("Manual override: Distance each door slides when opening (only used if useAutoSlideDistance is false)")]
    public float manualDoorSlideDistance = 2f;
    
    [Tooltip("How fast the doors open")]
    public float doorOpenSpeed = 2f;
    
    [Tooltip("How fast the doors close (very slow for dramatic effect)")]
    public float doorCloseSpeed = 0.1f;
    
    [Tooltip("Elevator arrival delay after using keycard (seconds)")]
    public float elevatorArrivalDelay = 20f;
    
    [Tooltip("Time doors stay open before closing (seconds)")]
    public float doorOpenDuration = 15f;
    
    [Header("Interaction Settings")]
    [Tooltip("Distance at which player can interact with the elevator")]
    public float interactionDistance = 3f;
    
    [Header("Visual Feedback")]
    [Tooltip("Material to use when elevator is waiting/locked")]
    public Material lockedMaterial;
    
    [Tooltip("Material to use when elevator has arrived")]
    public Material unlockedMaterial;
    
    [Tooltip("Color to tint the doors when locked")]
    public Color lockedColor = Color.red;
    
    [Tooltip("Color to tint the doors when unlocked")]
    public Color unlockedColor = Color.green;
    
    [Tooltip("Color during elevator arrival countdown")]
    public Color arrivingColor = Color.yellow;
    
    [Header("Interaction UI")]
    [Tooltip("UI text to show when player is in range")]
    public GameObject interactionUI;
    
    [Tooltip("Text component for interaction prompt")]
    public UnityEngine.UI.Text interactionText;
    
    [Header("Exit Zone Integration")]
    [Tooltip("ExitZone that becomes active when doors fully close")]
    public ExitZone exitZone;
    
    [Header("Audio (Centralized System)")]
    [Tooltip("Audio is now handled by GameSounds system - no clips needed here!")]
    public bool useCentralizedAudio = true;
    
    // Internal state
    private bool isOpen = false;
    private bool isOpening = false;
    private bool isClosing = false;
    private bool elevatorArriving = false;
    private bool elevatorArrived = false;
    private bool playerInRange = false;
    private Vector3 leftDoorClosedPosition;
    private Vector3 rightDoorClosedPosition;
    private GameObject player;
    private InventoryManager inventoryManager;
    private Renderer[] doorRenderers;
    private bool isUIVisible = false;
    private float arrivalTimer = 0f;
    private float doorOpenTimer = 0f;
    
    private void Start()
    {
        // Store original door positions
        if (leftDoor != null)
        {
            leftDoorClosedPosition = leftDoor.localPosition;
        }
        else
        {
            Debug.LogError($"[ElevatorDoor] Left door not assigned on {gameObject.name}!");
        }
        
        if (rightDoor != null)
        {
            rightDoorClosedPosition = rightDoor.localPosition;
        }
        else
        {
            Debug.LogError($"[ElevatorDoor] Right door not assigned on {gameObject.name}!");
        }
        
        // Calculate auto slide distance if enabled (both doors are identical)
        if (useAutoSlideDistance)
        {
            float autoDistance = CalculateAutoSlideDistance(leftDoor); // Both doors same size
            
            if (autoDistance > 0f)
            {
                Debug.Log($"[ElevatorDoor] {gameObject.name} auto-detected door width: {autoDistance:F2}m (both doors identical)");
            }
            else
            {
                Debug.LogWarning($"[ElevatorDoor] {gameObject.name} failed to auto-detect door size, using manual distance: {manualDoorSlideDistance:F2}m");
            }
        }
        
        // Find player
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[ElevatorDoor] Player not found!");
        }
        
        // Find inventory manager
        StartCoroutine(InitializeInventoryManager());
        
        // Validate keycard requirement
        if (requiredKeycard == null)
        {
            Debug.LogError($"[ElevatorDoor] No elevator keycard assigned to {gameObject.name}!");
        }
        
        // Audio is now handled by centralized GameSounds system
        
        // Setup collider
        SetupCollider();
        
        // Setup visual feedback
        SetupVisualFeedback();
        
        // Setup interaction UI
        SetupInteractionUI();
        
        // Disable ExitZone initially (will be enabled when doors fully close)
        if (exitZone != null)
        {
            exitZone.gameObject.SetActive(false);
            Debug.Log($"[ElevatorDoor] ExitZone GameObject disabled - will activate when doors close");
        }
        else
        {
            Debug.LogWarning($"[ElevatorDoor] No ExitZone assigned! Player won't be able to exit after doors close.");
        }
        
        Debug.Log($"[ElevatorDoor] Initialized {gameObject.name} requiring elevator keycard");
    }
    
    /// <summary>
    /// Initialize inventory manager with retry logic
    /// </summary>
    private IEnumerator InitializeInventoryManager()
    {
        int attempts = 0;
        int maxAttempts = 10;
        
        while (inventoryManager == null && attempts < maxAttempts)
        {
            inventoryManager = InventoryManager.Instance;
            
            if (inventoryManager == null)
            {
                Debug.LogWarning($"[ElevatorDoor] InventoryManager.Instance not found yet (attempt {attempts + 1}/{maxAttempts}), retrying...");
                attempts++;
                yield return new WaitForSeconds(0.1f);
            }
            else
            {
                Debug.Log($"[ElevatorDoor] InventoryManager.Instance found successfully");
                break;
            }
        }
        
        if (inventoryManager == null)
        {
            Debug.LogError("[ElevatorDoor] InventoryManager.Instance not found after all retry attempts!");
        }
    }
    
    private void Update()
    {
        // Handle elevator arrival countdown
        if (elevatorArriving)
        {
            UpdateArrivalCountdown();
        }
        
        // Handle door open timer (countdown before closing)
        if (isOpen && !isClosing)
        {
            UpdateDoorOpenTimer();
        }
        
        // Handle player interaction
        HandlePlayerInteraction();
    }
    
    /// <summary>
    /// Updates the arrival countdown timer
    /// </summary>
    private void UpdateArrivalCountdown()
    {
        arrivalTimer -= Time.deltaTime;
        
        // Update UI with countdown
        UpdateInteractionUI();
        
        // Check if elevator has arrived
        if (arrivalTimer <= 0f && !elevatorArrived)
        {
            ElevatorArrived();
        }
    }
    
    /// <summary>
    /// Updates the door open timer (countdown before doors start closing)
    /// </summary>
    private void UpdateDoorOpenTimer()
    {
        doorOpenTimer -= Time.deltaTime;
        
        // Update UI with countdown
        UpdateInteractionUI();
        
        // Check if it's time to close the doors
        if (doorOpenTimer <= 0f)
        {
            StartCoroutine(CloseDoors());
        }
    }
    
    /// <summary>
    /// Called when elevator arrives after the delay
    /// </summary>
    private void ElevatorArrived()
    {
        elevatorArriving = false;
        elevatorArrived = true;
        
        // Play arrival sound
        PlayElevatorArrivalSound();
        
        // Update visual feedback
        UpdateVisualFeedback(true);
        
        // Show arrival message
        ShowElevatorArrivedMessage();
        
        // Open doors automatically
        StartCoroutine(OpenDoors());
        
        Debug.Log($"[ElevatorDoor] Elevator arrived at {gameObject.name}!");
    }
    
    /// <summary>
    /// Sets up the collider for player detection
    /// FIXED: Properly scales collider size based on transform scale
    /// </summary>
    private void SetupCollider()
    {
        Collider col = GetComponent<Collider>();
        
        // Calculate local space size (accounting for transform scale)
        Vector3 scale = transform.lossyScale;
        float avgScale = (Mathf.Abs(scale.x) + Mathf.Abs(scale.y) + Mathf.Abs(scale.z)) / 3f;
        
        // Avoid division by zero
        if (avgScale < 0.001f) avgScale = 1f;
        
        // Calculate local size needed for desired world space interaction distance
        float localSize = interactionDistance / avgScale;
        
        if (col == null)
        {
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            boxCollider.size = Vector3.one * localSize;
            Debug.Log($"[ElevatorDoor] Added BoxCollider - World Distance: {interactionDistance}, Local Size: {localSize}, Scale: {avgScale}");
        }
        else
        {
            col.isTrigger = true;
            
            // Update existing collider size
            if (col is BoxCollider boxCol)
            {
                boxCol.size = Vector3.one * localSize;
                Debug.Log($"[ElevatorDoor] Updated BoxCollider - World Distance: {interactionDistance}, Local Size: {localSize}, Scale: {avgScale}");
            }
            else if (col is SphereCollider sphereCol)
            {
                sphereCol.radius = localSize / 2f;
                Debug.Log($"[ElevatorDoor] Updated SphereCollider - World Distance: {interactionDistance}, Local Radius: {localSize / 2f}, Scale: {avgScale}");
            }
        }
    }
    
    /// <summary>
    /// Sets up visual feedback for locked/unlocked state
    /// </summary>
    private void SetupVisualFeedback()
    {
        // Get renderers from both doors
        List<Renderer> rendererList = new List<Renderer>();
        
        if (leftDoor != null)
        {
            Renderer leftRenderer = leftDoor.GetComponent<Renderer>();
            if (leftRenderer != null) rendererList.Add(leftRenderer);
        }
        
        if (rightDoor != null)
        {
            Renderer rightRenderer = rightDoor.GetComponent<Renderer>();
            if (rightRenderer != null) rendererList.Add(rightRenderer);
        }
        
        doorRenderers = rendererList.ToArray();
        
        if (doorRenderers.Length > 0)
        {
            // Apply locked material or color
            foreach (var renderer in doorRenderers)
            {
                if (lockedMaterial != null)
                {
                    renderer.material = lockedMaterial;
                }
                else
                {
                    renderer.material.color = lockedColor;
                }
            }
        }
        else
        {
            Debug.LogWarning($"[ElevatorDoor] No Renderers found on doors - visual feedback disabled");
        }
    }
    
    /// <summary>
    /// Sets up the interaction UI
    /// </summary>
    private void SetupInteractionUI()
    {
        if (interactionUI != null)
        {
            interactionUI.SetActive(false);
        }
    }
    
    /// <summary>
    /// Updates the interaction UI text based on current state
    /// </summary>
    private void UpdateInteractionUI()
    {
        if (interactionText == null || requiredKeycard == null) return;
        
        if (isClosing)
        {
            interactionText.text = "⚠️ DOORS CLOSING! GET IN NOW!";
        }
        else if (isOpen && doorOpenTimer > 0f)
        {
            int secondsRemaining = Mathf.CeilToInt(doorOpenTimer);
            interactionText.text = $"🚪 Doors closing in {secondsRemaining}s - HURRY!";
        }
        else if (isOpen)
        {
            interactionText.text = "Elevator doors are open";
        }
        else if (elevatorArriving)
        {
            int secondsRemaining = Mathf.CeilToInt(arrivalTimer);
            interactionText.text = $"Elevator arriving in {secondsRemaining}s...";
        }
        else if (elevatorArrived)
        {
            interactionText.text = "Elevator has arrived!";
        }
        else if (HasRequiredKeycard())
        {
            interactionText.text = $"Press E to call elevator (uses {requiredKeycard.itemName})";
        }
        else
        {
            interactionText.text = $"Requires {requiredKeycard.itemName}";
        }
    }
    
    /// <summary>
    /// Handles player interaction and input
    /// </summary>
    private void HandlePlayerInteraction()
    {
        if (!playerInRange || isOpen || isOpening || elevatorArriving) return;
        
        // Check for interact key input
        if (Input.GetKeyDown(Controls.Interact))
        {
            TryCallElevator();
        }
    }
    
    /// <summary>
    /// Checks if the player has the required elevator keycard
    /// </summary>
    private bool HasRequiredKeycard()
    {
        if (inventoryManager == null || requiredKeycard == null) 
        {
            return false;
        }
        
        // Check all slots for matching keycard
        var allSlots = inventoryManager.GetAllInventorySlots();
        foreach (var slot in allSlots)
        {
            if (!slot.IsEmpty && slot.CurrentItem != null)
            {
                if (slot.CurrentItem.IsSameItem(requiredKeycard))
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Attempts to call the elevator using the keycard
    /// </summary>
    public void TryCallElevator()
    {
        if (elevatorArriving || elevatorArrived || isOpen)
        {
            return;
        }
        
        if (requiredKeycard == null || inventoryManager == null)
        {
            Debug.LogError($"[ElevatorDoor] TryCallElevator failed: requiredKeycard or inventoryManager is null");
            return;
        }
        
        // Check if player has the required keycard
        if (HasRequiredKeycard())
        {
            // Find and consume the keycard (ONE-TIME USE)
            var allSlots = inventoryManager.GetAllInventorySlots();
            UnifiedSlot matchingSlot = null;
            
            foreach (var slot in allSlots)
            {
                if (!slot.IsEmpty && slot.CurrentItem != null && slot.CurrentItem.IsSameItem(requiredKeycard))
                {
                    matchingSlot = slot;
                    break;
                }
            }
            
            if (matchingSlot != null)
            {
                // Remove the keycard (consume it)
                if (matchingSlot.ItemCount > 1)
                {
                    matchingSlot.SetItem(matchingSlot.CurrentItem, matchingSlot.ItemCount - 1);
                }
                else
                {
                    matchingSlot.ClearSlot();
                }
                
                inventoryManager.OnInventoryChanged?.Invoke();
                inventoryManager.SaveInventoryData();
                
                Debug.Log($"[ElevatorDoor] Consumed {requiredKeycard.itemName} to call elevator");
                
                // Start elevator arrival sequence
                StartElevatorArrival();
                
                // Show success message
                ShowElevatorCalledMessage();
            }
            else
            {
                Debug.LogError($"[ElevatorDoor] Failed to find matching keycard in inventory!");
                PlayLockedSound();
                ShowKeycardRequiredMessage();
            }
        }
        else
        {
            // Player doesn't have the keycard
            PlayLockedSound();
            ShowKeycardRequiredMessage();
        }
    }
    
    /// <summary>
    /// Starts the elevator arrival countdown
    /// </summary>
    private void StartElevatorArrival()
    {
        elevatorArriving = true;
        arrivalTimer = elevatorArrivalDelay;
        
        // Play elevator called sound
        PlayElevatorCalledSound();
        
        // Update visual feedback to "arriving" state
        UpdateVisualFeedback(false, true);
        
        Debug.Log($"[ElevatorDoor] Elevator called! Arriving in {elevatorArrivalDelay} seconds...");
    }
    
    /// <summary>
    /// Opens the elevator doors with animation
    /// </summary>
    private IEnumerator OpenDoors()
    {
        isOpening = true;
        
        // Play door open sound
        PlayDoorOpenSound();
        
        // Get slide distance (both doors use same distance since they're identical)
        float slideDistance = GetEffectiveSlideDistance(leftDoor);
        
        // UNIVERSAL SOLUTION: Works with any rotation!
        // Use the ELEVATOR's right direction (not the doors') and convert to local space
        Vector3 elevatorRight = transform.right; // Elevator's world-space right direction
        
        // Calculate world-space movement - both doors slide same distance
        Vector3 leftWorldMovement = elevatorRight * -slideDistance; // Left door slides opposite to elevator's right
        Vector3 rightWorldMovement = elevatorRight * slideDistance; // Right door slides along elevator's right
        
        // Convert world movement to parent's local space (for localPosition)
        Vector3 leftLocalMovement = leftDoor.parent.InverseTransformDirection(leftWorldMovement);
        Vector3 rightLocalMovement = rightDoor.parent.InverseTransformDirection(rightWorldMovement);
        
        // Calculate target local positions
        Vector3 leftDoorTargetPosition = leftDoorClosedPosition + leftLocalMovement;
        Vector3 rightDoorTargetPosition = rightDoorClosedPosition + rightLocalMovement;
        
        Debug.Log($"[ElevatorDoor] Elevator right: {elevatorRight}");
        Debug.Log($"[ElevatorDoor] Slide distance: {slideDistance:F2}m (both doors identical)");
        Debug.Log($"[ElevatorDoor] Local movements - Left: {leftLocalMovement}, Right: {rightLocalMovement}");
        Debug.Log($"[ElevatorDoor] Target positions - Left: {leftDoorTargetPosition}, Right: {rightDoorTargetPosition}");
        
        // Animate both doors simultaneously
        float elapsedTime = 0f;
        float duration = 1f / doorOpenSpeed;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            // Smooth step interpolation
            t = t * t * (3f - 2f * t);
            
            // Move both doors
            if (leftDoor != null)
            {
                leftDoor.localPosition = Vector3.Lerp(leftDoorClosedPosition, leftDoorTargetPosition, t);
            }
            
            if (rightDoor != null)
            {
                rightDoor.localPosition = Vector3.Lerp(rightDoorClosedPosition, rightDoorTargetPosition, t);
            }
            
            yield return null;
        }
        
        // Ensure final positions are exact
        if (leftDoor != null)
        {
            leftDoor.localPosition = leftDoorTargetPosition;
        }
        
        if (rightDoor != null)
        {
            rightDoor.localPosition = rightDoorTargetPosition;
        }
        
        isOpening = false;
        isOpen = true;
        
        // Start the door open timer (15 seconds before closing)
        doorOpenTimer = doorOpenDuration;
        
        // Show interaction UI with countdown
        SetInteractionUIVisible(true);
        
        // Show message about doors staying open
        ShowDoorsOpenMessage();
        
        Debug.Log($"[ElevatorDoor] Doors opened on {gameObject.name} - will close in {doorOpenDuration} seconds");
    }
    
    /// <summary>
    /// Closes the elevator doors with SLOW dramatic animation
    /// </summary>
    private IEnumerator CloseDoors()
    {
        isClosing = true;
        isOpen = false;
        
        // Play door close sound
        PlayDoorCloseSound();
        
        // Show dramatic closing message
        ShowDoorsClosingMessage();
        
        // Get current door positions (should be open)
        Vector3 leftDoorOpenPosition = leftDoor != null ? leftDoor.localPosition : leftDoorClosedPosition;
        Vector3 rightDoorOpenPosition = rightDoor != null ? rightDoor.localPosition : rightDoorClosedPosition;
        
        Debug.Log($"[ElevatorDoor] Closing doors from Open positions - Left: {leftDoorOpenPosition}, Right: {rightDoorOpenPosition}");
        
        // Animate both doors closing VERY SLOWLY (0.1 speed = 10x slower)
        float elapsedTime = 0f;
        float duration = 1f / doorCloseSpeed; // With 0.1 speed, this will be 10 seconds
        
        Debug.Log($"[ElevatorDoor] 🚨 DOORS CLOSING SLOWLY! Duration: {duration} seconds");
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            // Smooth step interpolation for dramatic effect
            t = t * t * (3f - 2f * t);
            
            // Move both doors back to closed position
            if (leftDoor != null)
            {
                leftDoor.localPosition = Vector3.Lerp(leftDoorOpenPosition, leftDoorClosedPosition, t);
            }
            
            if (rightDoor != null)
            {
                rightDoor.localPosition = Vector3.Lerp(rightDoorOpenPosition, rightDoorClosedPosition, t);
            }
            
            yield return null;
        }
        
        // Ensure final positions are exact
        if (leftDoor != null)
        {
            leftDoor.localPosition = leftDoorClosedPosition;
        }
        
        if (rightDoor != null)
        {
            rightDoor.localPosition = rightDoorClosedPosition;
        }
        
        isClosing = false;
        
        // ACTIVATE EXIT ZONE - Player can now exfil!
        if (exitZone != null)
        {
            exitZone.gameObject.SetActive(true);
            Debug.Log($"[ElevatorDoor] ✅ EXIT ZONE GAMEOBJECT ACTIVATED! Player can now exfil!");
            ShowExitActiveMessage();
        }
        else
        {
            Debug.LogError("[ElevatorDoor] ❌ ExitZone is null! Cannot activate exit!");
        }
        
        // Hide interaction UI
        SetInteractionUIVisible(false);
        
        Debug.Log($"[ElevatorDoor] Doors fully closed on {gameObject.name} - Exit zone active!");
    }
    
    /// <summary>
    /// Updates visual feedback based on door state
    /// </summary>
    private void UpdateVisualFeedback(bool unlocked, bool arriving = false)
    {
        if (doorRenderers == null || doorRenderers.Length == 0) return;
        
        foreach (var renderer in doorRenderers)
        {
            if (renderer == null) continue;
            
            if (arriving)
            {
                // Yellow during arrival countdown
                if (unlockedMaterial != null)
                {
                    renderer.material = unlockedMaterial;
                }
                renderer.material.color = arrivingColor;
            }
            else if (unlocked)
            {
                // Green when arrived
                if (unlockedMaterial != null)
                {
                    renderer.material = unlockedMaterial;
                }
                else
                {
                    renderer.material.color = unlockedColor;
                }
            }
            else
            {
                // Red when locked
                if (lockedMaterial != null)
                {
                    renderer.material = lockedMaterial;
                }
                else
                {
                    renderer.material.color = lockedColor;
                }
            }
        }
    }
    
    /// <summary>
    /// Plays the locked sound
    /// </summary>
    private void PlayLockedSound()
    {
        if (useCentralizedAudio)
        {
            GameSounds.PlayElevatorLocked(transform.position, 1f);
        }
    }
    
    /// <summary>
    /// Plays the elevator called sound
    /// </summary>
    private void PlayElevatorCalledSound()
    {
        if (useCentralizedAudio)
        {
            GameSounds.PlayElevatorCalled(transform.position, 1f);
        }
    }
    
    /// <summary>
    /// Plays the elevator arrival sound
    /// </summary>
    private void PlayElevatorArrivalSound()
    {
        if (useCentralizedAudio)
        {
            GameSounds.PlayElevatorArrival(transform.position, 1f);
        }
    }
    
    /// <summary>
    /// Plays the door open sound
    /// </summary>
    private void PlayDoorOpenSound()
    {
        if (useCentralizedAudio)
        {
            GameSounds.PlayElevatorDoorsOpen(transform.position, 1f);
        }
    }
    
    /// <summary>
    /// Plays the door close sound
    /// </summary>
    private void PlayDoorCloseSound()
    {
        if (useCentralizedAudio)
        {
            GameSounds.PlayElevatorDoorsClose(transform.position, 1f);
        }
    }
    
    /// <summary>
    /// Shows a message when elevator is called
    /// </summary>
    private void ShowElevatorCalledMessage()
    {
        if (CognitiveFeedManager.Instance != null && requiredKeycard != null)
        {
            CognitiveFeedManager.Instance.ShowInstantMessage($"Elevator called! Arriving in {elevatorArrivalDelay} seconds...", 3f);
        }
    }
    
    /// <summary>
    /// Shows a message when elevator arrives
    /// </summary>
    private void ShowElevatorArrivedMessage()
    {
        if (CognitiveFeedManager.Instance != null)
        {
            CognitiveFeedManager.Instance.ShowInstantMessage("Elevator has arrived! Doors opening...", 2.5f);
        }
    }
    
    /// <summary>
    /// Shows a message when doors open
    /// </summary>
    private void ShowDoorsOpenMessage()
    {
        if (CognitiveFeedManager.Instance != null)
        {
            CognitiveFeedManager.Instance.ShowInstantMessage($"⚠️ Doors will close in {doorOpenDuration} seconds! GET IN!", 3f);
        }
    }
    
    /// <summary>
    /// Shows a message when doors start closing
    /// </summary>
    private void ShowDoorsClosingMessage()
    {
        if (CognitiveFeedManager.Instance != null)
        {
            CognitiveFeedManager.Instance.ShowInstantMessage("🚨 DOORS CLOSING! HURRY!", 3f);
        }
    }
    
    /// <summary>
    /// Shows a message when doors fully close and exit activates
    /// </summary>
    private void ShowExitActiveMessage()
    {
        if (CognitiveFeedManager.Instance != null)
        {
            CognitiveFeedManager.Instance.ShowInstantMessage("✅ Exit zone activated! You can now exfil!", 3f);
        }
    }
    
    /// <summary>
    /// Shows a message when keycard is required
    /// </summary>
    private void ShowKeycardRequiredMessage()
    {
        if (requiredKeycard == null)
        {
            Debug.LogError("[ElevatorDoor] Cannot show keycard required message - requiredKeycard is null!");
            return;
        }
        
        string message = $"Requires {requiredKeycard.itemName} to call elevator!";
        
        if (CognitiveFeedManager.Instance != null)
        {
            CognitiveFeedManager.Instance.ShowInstantMessage(message, 2.5f, MessagePriority.CRITICAL);
        }
    }
    
    /// <summary>
    /// Shows or hides the interaction UI
    /// </summary>
    private void SetInteractionUIVisible(bool visible)
    {
        if (interactionUI != null && isUIVisible != visible)
        {
            interactionUI.SetActive(visible);
            isUIVisible = visible;
            
            if (visible)
            {
                UpdateInteractionUI();
            }
        }
    }
    
    // Trigger events for player detection
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            
            Debug.Log($"[ElevatorDoor] Player entered range of {gameObject.name}");
            
            // Show UI only if doors are not open
            if (!isOpen)
            {
                SetInteractionUIVisible(true);
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            SetInteractionUIVisible(false);
            
            Debug.Log($"[ElevatorDoor] Player left range of {gameObject.name}");
        }
    }
    
    /// <summary>
    /// AUTO-DETECTS door size and calculates the exact slide distance needed
    /// CRITICAL: Gets the WIDTH (perpendicular to slide direction), not depth/thickness
    /// Both elevator doors are identical, so we only need to check one
    /// </summary>
    private float CalculateAutoSlideDistance(Transform door)
    {
        if (door == null) return 0f;
        
        // Try to get bounds from Renderer first (most accurate for visual size)
        Renderer renderer = door.GetComponent<Renderer>();
        if (renderer == null)
        {
            renderer = door.GetComponentInChildren<Renderer>();
        }
        
        Bounds bounds;
        bool hasBounds = false;
        
        if (renderer != null)
        {
            bounds = renderer.bounds;
            hasBounds = true;
        }
        else
        {
            // Fallback to Collider bounds
            Collider col = door.GetComponent<Collider>();
            if (col == null)
            {
                col = door.GetComponentInChildren<Collider>();
            }
            
            if (col != null)
            {
                bounds = col.bounds;
                hasBounds = true;
            }
            else
            {
                Debug.LogError($"[ElevatorDoor] {door.name} has no Renderer or Collider - cannot auto-detect size!");
                return 0f;
            }
        }
        
        if (!hasBounds)
        {
            return 0f;
        }
        
        // CRITICAL: Elevator doors slide horizontally (left/right)
        // We need the door's WIDTH (Z-axis in world space for typical elevator doors)
        // This ensures door slides exactly its width to clear itself
        float doorWidth = bounds.size.z;
        
        return doorWidth;
    }
    
    /// <summary>
    /// Gets the effective slide distance for a specific door (auto-detected or manual override)
    /// </summary>
    private float GetEffectiveSlideDistance(Transform door)
    {
        if (useAutoSlideDistance)
        {
            float autoDistance = CalculateAutoSlideDistance(door);
            if (autoDistance > 0f)
            {
                return autoDistance;
            }
            else
            {
                // Auto-detection failed, fallback to manual
                Debug.LogWarning($"[ElevatorDoor] {door.name} auto-detection failed, using manual distance: {manualDoorSlideDistance}");
                return manualDoorSlideDistance;
            }
        }
        else
        {
            return manualDoorSlideDistance;
        }
    }
    
    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        // Draw interaction range
        Gizmos.color = isOpen ? Color.green : (elevatorArriving ? Color.yellow : Color.red);
        Gizmos.DrawWireCube(transform.position, Vector3.one * interactionDistance);
        
        // Draw door slide direction indicators
        if (!Application.isPlaying && leftDoor != null && rightDoor != null)
        {
            // Get the slide distance (both doors identical)
            float slideDistance = useAutoSlideDistance ? CalculateAutoSlideDistance(leftDoor) : manualDoorSlideDistance;
            
            if (slideDistance <= 0f) slideDistance = manualDoorSlideDistance; // Fallback
            
            Gizmos.color = useAutoSlideDistance ? Color.cyan : Color.yellow; // Cyan = auto, Yellow = manual
            
            // Left door indicator
            Vector3 leftStart = leftDoor.position;
            Vector3 leftEnd = leftStart + leftDoor.TransformDirection(Vector3.left * slideDistance);
            Gizmos.DrawLine(leftStart, leftEnd);
            Gizmos.DrawSphere(leftEnd, 0.1f);
            
            // Right door indicator
            Vector3 rightStart = rightDoor.position;
            Vector3 rightEnd = rightStart + rightDoor.TransformDirection(Vector3.right * slideDistance);
            Gizmos.DrawLine(rightStart, rightEnd);
            Gizmos.DrawSphere(rightEnd, 0.1f);
            
            // Draw door bounds for reference
            Renderer leftRenderer = leftDoor.GetComponent<Renderer>();
            if (leftRenderer == null) leftRenderer = leftDoor.GetComponentInChildren<Renderer>();
            if (leftRenderer != null)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(leftRenderer.bounds.center, leftRenderer.bounds.size);
            }
            
            Renderer rightRenderer = rightDoor.GetComponent<Renderer>();
            if (rightRenderer == null) rightRenderer = rightDoor.GetComponentInChildren<Renderer>();
            if (rightRenderer != null)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(rightRenderer.bounds.center, rightRenderer.bounds.size);
            }
        }
    }
    
    /// <summary>
    /// Editor-time validation to update gizmos when settings change
    /// </summary>
    private void OnValidate()
    {
        // This makes the gizmos update in the editor when you change settings
        // No runtime logic needed here
    }
    
    // Context menu methods for testing
    [ContextMenu("Test Call Elevator")]
    private void TestCallElevator()
    {
        if (Application.isPlaying && !elevatorArriving && !elevatorArrived)
        {
            StartElevatorArrival();
        }
        else
        {
            Debug.Log("[ElevatorDoor] Test call can only be used in play mode when elevator is not already called");
        }
    }
    
    [ContextMenu("Test Open Doors Immediately")]
    private void TestOpenDoorsImmediate()
    {
        if (Application.isPlaying && !isOpen)
        {
            elevatorArrived = true;
            elevatorArriving = false;
            StartCoroutine(OpenDoors());
        }
        else
        {
            Debug.Log("[ElevatorDoor] Test open can only be used in play mode when doors are closed");
        }
    }
    
    [ContextMenu("Debug: Show Auto-Detected Size")]
    private void DebugShowAutoDetectedSize()
    {
        if (leftDoor != null)
        {
            float autoDistance = CalculateAutoSlideDistance(leftDoor);
            if (autoDistance > 0f)
            {
                Debug.Log($"[ElevatorDoor] AUTO-DETECTED door width: {autoDistance:F2}m (both doors identical)");
            }
            else
            {
                Debug.LogWarning($"[ElevatorDoor] Could not auto-detect door size (no Renderer/Collider found)");
            }
        }
        else
        {
            Debug.LogWarning($"[ElevatorDoor] Left door not assigned - cannot auto-detect size");
        }
        
        Debug.Log($"[ElevatorDoor] Current mode: {(useAutoSlideDistance ? "AUTO" : "MANUAL")} | Manual override distance: {manualDoorSlideDistance:F2}m");
    }
}
