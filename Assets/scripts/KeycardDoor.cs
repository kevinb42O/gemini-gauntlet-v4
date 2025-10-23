using UnityEngine;
using System.Collections;
using GeminiGauntlet.Audio;

/// <summary>
/// KeycardDoor.cs - Interactive door that requires a specific keycard to open
/// When the player interacts with the door and has the required keycard, the door opens and the keycard is consumed
/// </summary>
[RequireComponent(typeof(Collider))]
public class KeycardDoor : MonoBehaviour
{
    [Header("Keycard Requirement")]
    [Tooltip("The keycard required to open this door")]
    public ChestItemData requiredKeycard;
    
    [Header("Door Settings")]
    [Tooltip("How should the door open?")]
    public DoorOpenType openType = DoorOpenType.SlideUp;
    
    [Tooltip("Distance the door moves when opening")]
    public float openDistance = 3f;
    
    [Tooltip("How fast the door opens")]
    public float openSpeed = 2f;
    
    [Tooltip("Should the door close after opening? (Keycards make doors stay open permanently)")]
    public bool autoClose = false;
    
    [Tooltip("Time before door auto-closes (if autoClose is enabled and no keycard was used)")]
    public float autoCloseDelay = 5f;
    
    [Header("Interaction Settings")]
    [Tooltip("Distance at which player can interact with the door")]
    public float interactionDistance = 3f;
    
    [Header("Visual Feedback")]
    [Tooltip("Material to use when door is locked (optional)")]
    public Material lockedMaterial;
    
    [Tooltip("Material to use when door is unlocked (optional)")]
    public Material unlockedMaterial;
    
    [Tooltip("Color to tint the door when locked")]
    public Color lockedColor = Color.red;
    
    [Tooltip("Color to tint the door when unlocked")]
    public Color unlockedColor = Color.green;
    
    [Header("Interaction UI")]
    [Tooltip("UI text to show when player is in range")]
    public GameObject interactionUI;
    
    [Tooltip("Text component for interaction prompt")]
    public UnityEngine.UI.Text interactionText;
    
    [Header("Audio")]
    [Tooltip("Sound to play when door is locked")]
    public AudioClip lockedSound;
    
    [Tooltip("Sound to play when door opens")]
    public AudioClip openSound;
    
    [Tooltip("Sound to play when door closes")]
    public AudioClip closeSound;
    
    [Header("Animation")]
    [Tooltip("Hand animation controller for door interaction animations")]
    public LayeredHandAnimationController handAnimationController;
    
    // Internal state
    private bool isOpen = false;
    private bool isOpening = false;
    private bool isClosing = false;
    private bool playerInRange = false;
    private bool isUnlocked = false; // Track if door has been unlocked (for infinite use keycards)
    private Vector3 closedPosition;
    private Quaternion closedRotation;
    private Vector3 closedScale; // Store original scale for proper restoration
    private GameObject player;
    private InventoryManager inventoryManager;
    private AudioSource audioSource;
    private Renderer doorRenderer;
    private bool isUIVisible = false;
    
    public enum DoorOpenType
    {
        SlideUp,
        SlideDown,
        SlideLeft,
        SlideRight,
        SlideForward,
        SlideBackward,
        RotateLeft,
        RotateRight,
        Scale
    }
    
    private void Start()
    {
        // Store original position, rotation, and scale
        closedPosition = transform.position;
        closedRotation = transform.rotation;
        closedScale = transform.localScale;
        
        // Find player
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[KeycardDoor] Player not found!");
        }
        
        // Find inventory manager - retry if not found immediately (may be loading persistent data)
        StartCoroutine(InitializeInventoryManager());
        
        // Find hand animation controller for door interaction animations
        if (handAnimationController == null)
        {
            handAnimationController = FindObjectOfType<LayeredHandAnimationController>();
            if (handAnimationController != null)
            {
                Debug.Log($"[KeycardDoor] Found LayeredHandAnimationController: {handAnimationController.name}");
            }
            else
            {
                Debug.LogWarning("[KeycardDoor] LayeredHandAnimationController not found - door open animations will not play");
            }
        }
        
        // Validate keycard requirement
        if (requiredKeycard == null)
        {
            Debug.LogError($"[KeycardDoor] No keycard assigned to door {gameObject.name}!");
        }
        
        // Setup audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D sound
            audioSource.maxDistance = 20f;
        }
        
        // Setup collider
        SetupCollider();
        
        // Setup visual feedback
        SetupVisualFeedback();
        
        // Setup interaction UI
        SetupInteractionUI();
        
        Debug.Log($"[KeycardDoor] Initialized {gameObject.name} requiring keycard: {(requiredKeycard != null ? requiredKeycard.itemName : "NONE")}");
    }
    
    /// <summary>
    /// Initialize inventory manager with retry logic (persistent inventory may be loading)
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
                Debug.LogWarning($"[KeycardDoor] InventoryManager.Instance not found yet (attempt {attempts + 1}/{maxAttempts}), retrying...");
                attempts++;
                yield return new WaitForSeconds(0.1f);
            }
            else
            {
                Debug.Log($"[KeycardDoor] InventoryManager.Instance found successfully on attempt {attempts + 1}");
                break;
            }
        }
        
        if (inventoryManager == null)
        {
            Debug.LogError("[KeycardDoor] InventoryManager.Instance not found after all retry attempts!");
        }
    }
    
    private void Update()
    {
        // Handle player interaction
        HandlePlayerInteraction();
    }
    
    /// <summary>
    /// Sets up the collider for player detection
    /// IMPORTANT: Collider size is in LOCAL space, so we need to account for the door's scale
    /// </summary>
    private void SetupCollider()
    {
        Collider col = GetComponent<Collider>();
        
        // Calculate local space size accounting for door scale
        Vector3 localSize = CalculateLocalColliderSize();
        
        if (col == null)
        {
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            boxCollider.size = localSize;
            Debug.Log($"[KeycardDoor] Added BoxCollider to {gameObject.name} with local size {localSize} (world size ~{interactionDistance})");
        }
        else
        {
            col.isTrigger = true;
            
            // Update existing collider size based on interactionDistance
            if (col is BoxCollider boxCol)
            {
                boxCol.size = localSize;
                Debug.Log($"[KeycardDoor] Updated BoxCollider local size to {localSize} on {gameObject.name} (world size ~{interactionDistance})");
            }
            else if (col is SphereCollider sphereCol)
            {
                // For sphere, use average of the local size components
                float localRadius = (localSize.x + localSize.y + localSize.z) / 3f;
                sphereCol.radius = localRadius;
                Debug.Log($"[KeycardDoor] Updated SphereCollider local radius to {localRadius} on {gameObject.name} (world size ~{interactionDistance})");
            }
            else
            {
                Debug.LogWarning($"[KeycardDoor] Collider on {gameObject.name} is not BoxCollider or SphereCollider - cannot auto-resize. Please manually adjust collider size.");
            }
        }
    }
    
    /// <summary>
    /// Calculate the local space collider size that will result in the desired world space interaction distance
    /// </summary>
    private Vector3 CalculateLocalColliderSize()
    {
        Vector3 scale = transform.lossyScale;
        
        // Avoid division by zero
        if (scale.x == 0) scale.x = 1;
        if (scale.y == 0) scale.y = 1;
        if (scale.z == 0) scale.z = 1;
        
        // Calculate local size needed to achieve world space interaction distance
        Vector3 localSize = new Vector3(
            interactionDistance / Mathf.Abs(scale.x),
            interactionDistance / Mathf.Abs(scale.y),
            interactionDistance / Mathf.Abs(scale.z)
        );
        
        return localSize;
    }
    
    /// <summary>
    /// Sets up visual feedback for locked/unlocked state
    /// </summary>
    private void SetupVisualFeedback()
    {
        doorRenderer = GetComponent<Renderer>();
        if (doorRenderer == null)
        {
            doorRenderer = GetComponentInChildren<Renderer>();
        }
        
        if (doorRenderer != null)
        {
            // Apply locked material or color
            if (lockedMaterial != null)
            {
                doorRenderer.material = lockedMaterial;
            }
            else
            {
                doorRenderer.material.color = lockedColor;
            }
        }
        else
        {
            Debug.LogWarning($"[KeycardDoor] No Renderer found on {gameObject.name} - visual feedback disabled");
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
        
        if (isOpen)
        {
            interactionText.text = "Door is open";
        }
        else if (HasRequiredKeycard())
        {
            interactionText.text = $"Press E to open with {requiredKeycard.itemName}";
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
        // If door is unlocked (infinite use keycard used), auto-open when player enters
        if (isUnlocked && playerInRange && !isOpen && !isOpening && !isClosing)
        {
            StartCoroutine(OpenDoor());
            return;
        }
        
        if (!playerInRange || isOpen || isOpening || isClosing) return;
        
        // Check for interact key input
        if (Input.GetKeyDown(Controls.Interact))
        {
            TryOpenDoor();
        }
    }
    
    /// <summary>
    /// Checks if the player has the required keycard
    /// ENHANCED: Checks by IsSameItem() to handle keycards loaded from persistent storage
    /// </summary>
    private bool HasRequiredKeycard()
    {
        if (inventoryManager == null || requiredKeycard == null) 
        {
            Debug.LogWarning($"[KeycardDoor] HasRequiredKeycard check failed: inventoryManager={inventoryManager != null}, requiredKeycard={requiredKeycard != null}");
            return false;
        }
        
        // ENHANCED: Check all slots manually using IsSameItem() to handle persistent inventory
        var allSlots = inventoryManager.GetAllInventorySlots();
        foreach (var slot in allSlots)
        {
            if (!slot.IsEmpty && slot.CurrentItem != null)
            {
                if (slot.CurrentItem.IsSameItem(requiredKeycard))
                {
                    Debug.Log($"[KeycardDoor] ✅ Found matching keycard: {slot.CurrentItem.itemName} (ID: {slot.CurrentItem.itemID}) matches required {requiredKeycard.itemName} (ID: {requiredKeycard.itemID})");
                    return true;
                }
            }
        }
        
        Debug.Log($"[KeycardDoor] ❌ No matching keycard found for {requiredKeycard.itemName} (ID: {requiredKeycard.itemID})");
        return false;
    }
    
    /// <summary>
    /// Attempts to open the door
    /// </summary>
    public void TryOpenDoor()
    {
        if (isOpen || isOpening)
        {
            Debug.Log($"[KeycardDoor] TryOpenDoor blocked: isOpen={isOpen}, isOpening={isOpening}");
            return;
        }
        
        if (requiredKeycard == null || inventoryManager == null)
        {
            Debug.LogError($"[KeycardDoor] TryOpenDoor failed: requiredKeycard={requiredKeycard != null}, inventoryManager={inventoryManager != null}");
            return;
        }
        
        Debug.Log($"[KeycardDoor] TryOpenDoor called for door requiring {requiredKeycard.itemName}");
        
        // Check if player has the required keycard
        if (HasRequiredKeycard())
        {
            // Check if this is the Building21 keycard (infinite use)
            bool isBuilding21Keycard = requiredKeycard.itemName.Contains("Building21") || 
                                       requiredKeycard.itemName.Contains("Building 21") ||
                                       requiredKeycard.itemName.Contains("building21");
            
            if (isBuilding21Keycard)
            {
                // Building21 keycard is NOT consumed - infinite use!
                Debug.Log($"[KeycardDoor] Used {requiredKeycard.itemName} to open door (keycard retained - infinite use)");
                
                // Mark door as permanently unlocked (auto-opens when player enters)
                isUnlocked = true;
                
                // Play door interaction animation
                if (handAnimationController != null)
                {
                    handAnimationController.PlayOpenDoorAnimation();
                    Debug.Log($"[KeycardDoor] 🎬 Playing open door animation");
                }
                
                // Open the door
                StartCoroutine(OpenDoor());
                
                // Show success message
                ShowDoorOpenMessage();
            }
            else
            {
                // All other keycards are consumed (one-time use) - removes ONLY 1 from stack
                // ENHANCED: Find the actual slot with matching keycard (handles persistent inventory)
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
                    // Remove exactly 1 keycard from the stack
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
                    
                    Debug.Log($"[KeycardDoor] Consumed 1x {requiredKeycard.itemName} to unlock door permanently (one-time use)");
                    
                    // Mark door as permanently unlocked (auto-opens when player enters)
                    isUnlocked = true;
                    
                    // Play door interaction animation
                    if (handAnimationController != null)
                    {
                        handAnimationController.PlayOpenDoorAnimation();
                        Debug.Log($"[KeycardDoor] 🎬 Playing open door animation");
                    }
                    
                    // Open the door
                    StartCoroutine(OpenDoor());
                    
                    // Show success message
                    ShowDoorOpenMessage();
                }
                else
                {
                    Debug.LogError($"[KeycardDoor] Failed to find matching keycard in inventory!");
                    PlayLockedSound();
                    ShowKeycardRequiredMessage();
                }
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
    /// Opens the door with animation
    /// </summary>
    private IEnumerator OpenDoor()
    {
        isOpening = true;
        
        // Play open sound
        PlayOpenSound();
        
        // Update visual feedback
        UpdateVisualFeedback(true);
        
        // Hide interaction UI
        SetInteractionUIVisible(false);
        
        // Get the slide distance
        float slideDistance = openDistance;
        
        // Calculate target position/rotation based on open type
        Vector3 targetPosition = closedPosition;
        Quaternion targetRotation = closedRotation;
        Vector3 targetScale = transform.localScale;
        
        switch (openType)
        {
            case DoorOpenType.SlideUp:
                targetPosition = closedPosition + Vector3.up * slideDistance;
                break;
            case DoorOpenType.SlideDown:
                targetPosition = closedPosition + Vector3.down * slideDistance;
                break;
            case DoorOpenType.SlideLeft:
                targetPosition = closedPosition + transform.right * -slideDistance;
                break;
            case DoorOpenType.SlideRight:
                targetPosition = closedPosition + transform.right * slideDistance;
                break;
            case DoorOpenType.SlideForward:
                targetPosition = closedPosition + transform.forward * slideDistance;
                break;
            case DoorOpenType.SlideBackward:
                targetPosition = closedPosition + transform.forward * -slideDistance;
                break;
            case DoorOpenType.RotateLeft:
                targetRotation = closedRotation * Quaternion.Euler(0, -90, 0);
                break;
            case DoorOpenType.RotateRight:
                targetRotation = closedRotation * Quaternion.Euler(0, 90, 0);
                break;
            case DoorOpenType.Scale:
                targetScale = Vector3.zero; // Scale down to zero
                break;
        }
        
        // Animate the door
        float elapsedTime = 0f;
        float duration = 1f / openSpeed;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            // Smooth step interpolation
            t = t * t * (3f - 2f * t);
            
            if (openType == DoorOpenType.RotateLeft || openType == DoorOpenType.RotateRight)
            {
                transform.rotation = Quaternion.Lerp(closedRotation, targetRotation, t);
            }
            else if (openType == DoorOpenType.Scale)
            {
                transform.localScale = Vector3.Lerp(closedScale, targetScale, t);
            }
            else
            {
                transform.position = Vector3.Lerp(closedPosition, targetPosition, t);
            }
            
            yield return null;
        }
        
        // Ensure final position/rotation is exact
        if (openType == DoorOpenType.RotateLeft || openType == DoorOpenType.RotateRight)
        {
            transform.rotation = targetRotation;
        }
        else if (openType == DoorOpenType.Scale)
        {
            transform.localScale = targetScale;
        }
        else
        {
            transform.position = targetPosition;
        }
        
        isOpening = false;
        isOpen = true;
        
        Debug.Log($"[KeycardDoor] Door {gameObject.name} opened");
        
        // Auto-close if enabled (unlocked doors will auto-reopen when player enters)
        if (autoClose)
        {
            StartCoroutine(AutoCloseDoor());
        }
        else if (isUnlocked)
        {
            Debug.Log($"[KeycardDoor] Door {gameObject.name} is permanently unlocked - will auto-reopen when player enters");
        }
    }
    
    /// <summary>
    /// Closes the door with animation
    /// </summary>
    private IEnumerator CloseDoor()
    {
        isClosing = true;
        
        // Play close sound
        PlayCloseSound();
        
        // Store current position/rotation
        Vector3 openPosition = transform.position;
        Quaternion openRotation = transform.rotation;
        Vector3 openScale = transform.localScale;
        
        // Animate the door back to closed position
        float elapsedTime = 0f;
        float duration = 1f / openSpeed;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            // Smooth step interpolation
            t = t * t * (3f - 2f * t);
            
            if (openType == DoorOpenType.RotateLeft || openType == DoorOpenType.RotateRight)
            {
                transform.rotation = Quaternion.Lerp(openRotation, closedRotation, t);
            }
            else if (openType == DoorOpenType.Scale)
            {
                transform.localScale = Vector3.Lerp(openScale, closedScale, t);
            }
            else
            {
                transform.position = Vector3.Lerp(openPosition, closedPosition, t);
            }
            
            yield return null;
        }
        
        // Ensure final position/rotation/scale is exact
        transform.position = closedPosition;
        transform.rotation = closedRotation;
        transform.localScale = closedScale; // Restore original scale, not Vector3.one
        
        isClosing = false;
        isOpen = false;
        
        // Update visual feedback back to locked
        UpdateVisualFeedback(false);
        
        Debug.Log($"[KeycardDoor] Door {gameObject.name} closed");
    }
    
    /// <summary>
    /// Auto-closes the door after a delay
    /// </summary>
    private IEnumerator AutoCloseDoor()
    {
        yield return new WaitForSeconds(autoCloseDelay);
        
        if (isOpen && !isClosing)
        {
            StartCoroutine(CloseDoor());
        }
    }
    
    /// <summary>
    /// Updates visual feedback based on door state
    /// </summary>
    private void UpdateVisualFeedback(bool unlocked)
    {
        if (doorRenderer == null) return;
        
        if (unlocked)
        {
            if (unlockedMaterial != null)
            {
                doorRenderer.material = unlockedMaterial;
            }
            else
            {
                doorRenderer.material.color = unlockedColor;
            }
        }
        else
        {
            if (lockedMaterial != null)
            {
                doorRenderer.material = lockedMaterial;
            }
            else
            {
                doorRenderer.material.color = lockedColor;
            }
        }
    }
    
    /// <summary>
    /// Plays the locked sound
    /// </summary>
    private void PlayLockedSound()
    {
        if (audioSource != null && lockedSound != null)
        {
            audioSource.PlayOneShot(lockedSound);
        }
    }
    
    /// <summary>
    /// Plays the open sound
    /// </summary>
    private void PlayOpenSound()
    {
        if (audioSource != null && openSound != null)
        {
            audioSource.PlayOneShot(openSound);
        }
    }
    
    /// <summary>
    /// Plays the close sound
    /// </summary>
    private void PlayCloseSound()
    {
        if (audioSource != null && closeSound != null)
        {
            audioSource.PlayOneShot(closeSound);
        }
    }
    
    /// <summary>
    /// Shows a message when door opens
    /// </summary>
    private void ShowDoorOpenMessage()
    {
        if (CognitiveFeedManager.Instance != null && requiredKeycard != null)
        {
            CognitiveFeedManager.Instance.ShowInstantMessage($"Door unlocked with {requiredKeycard.itemName}! (Permanently open)", 2.5f, MessagePriority.CRITICAL);
        }
    }
    
    /// <summary>
    /// Shows a message when keycard is required
    /// </summary>
    private void ShowKeycardRequiredMessage()
    {
        if (requiredKeycard == null)
        {
            Debug.LogError("[KeycardDoor] Cannot show keycard required message - requiredKeycard is null!");
            return;
        }
        
        string message = $"Requires {requiredKeycard.itemName} to open!";
        Debug.Log($"[KeycardDoor] Showing keycard required message: {message}");
        
        if (CognitiveFeedManager.Instance != null)
        {
            CognitiveFeedManager.Instance.ShowInstantMessage(message, 2.5f, MessagePriority.CRITICAL);
        }
        else
        {
            Debug.LogWarning("[KeycardDoor] CognitiveFeedManager.Instance is null - cannot show message!");
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
            
            // Show UI only if door is not open and not unlocked (unlocked doors auto-open)
            if (!isOpen && !isUnlocked)
            {
                SetInteractionUIVisible(true);
            }
            
            Debug.Log($"[KeycardDoor] Player entered range of {gameObject.name}");
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            SetInteractionUIVisible(false);
            
            Debug.Log($"[KeycardDoor] Player left range of {gameObject.name}");
        }
    }
    
    
    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        // Draw interaction range
        Gizmos.color = isOpen ? Color.green : Color.red;
        Gizmos.DrawWireCube(transform.position, Vector3.one * interactionDistance);
        
        // Draw open direction indicator
        if (!Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Vector3 direction = Vector3.zero;
            
            switch (openType)
            {
                case DoorOpenType.SlideUp:
                    direction = Vector3.up * openDistance;
                    break;
                case DoorOpenType.SlideDown:
                    direction = Vector3.down * openDistance;
                    break;
                case DoorOpenType.SlideLeft:
                    direction = transform.right * -openDistance;
                    break;
                case DoorOpenType.SlideRight:
                    direction = transform.right * openDistance;
                    break;
                case DoorOpenType.SlideForward:
                    direction = transform.forward * openDistance;
                    break;
                case DoorOpenType.SlideBackward:
                    direction = transform.forward * -openDistance;
                    break;
            }
            
            if (direction != Vector3.zero)
            {
                Gizmos.DrawLine(transform.position, transform.position + direction);
                Gizmos.DrawSphere(transform.position + direction, 0.2f);
                
                // Draw door bounds for reference
                Renderer renderer = GetComponent<Renderer>();
                if (renderer == null) renderer = GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawWireCube(renderer.bounds.center, renderer.bounds.size);
                }
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
    [ContextMenu("Test Open Door")]
    private void TestOpenDoor()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[KeycardDoor] Test open only works in play mode!");
            return;
        }
        
        // Force stop any current animations
        StopAllCoroutines();
        
        // Reset state
        isOpening = false;
        isClosing = false;
        
        // Force open
        Debug.Log($"[KeycardDoor] TEST: Force opening door {gameObject.name}");
        StartCoroutine(OpenDoor());
    }
    
    [ContextMenu("Test Close Door")]
    private void TestCloseDoor()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[KeycardDoor] Test close only works in play mode!");
            return;
        }
        
        // Force stop any current animations
        StopAllCoroutines();
        
        // Reset state
        isOpening = false;
        isClosing = false;
        
        // Force close
        Debug.Log($"[KeycardDoor] TEST: Force closing door {gameObject.name}");
        StartCoroutine(CloseDoor());
    }
    
    [ContextMenu("Update Collider Size Now")]
    private void UpdateColliderSizeNow()
    {
        SetupCollider();
        Debug.Log($"[KeycardDoor] Collider size updated to {interactionDistance}");
    }
    
    [ContextMenu("Debug: Show Door Settings")]
    private void DebugShowDoorSettings()
    {
        Debug.Log($"[KeycardDoor] {gameObject.name} Settings:");
        Debug.Log($"  - Open Type: {openType}");
        Debug.Log($"  - Open Distance: {openDistance:F2}m");
        Debug.Log($"  - Open Speed: {openSpeed}");
        Debug.Log($"  - Auto Close: {autoClose}");
        Debug.Log($"  - Is Open: {isOpen}");
        Debug.Log($"  - Is Unlocked: {isUnlocked}");
    }
}
