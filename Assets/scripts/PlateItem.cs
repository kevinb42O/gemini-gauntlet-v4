using UnityEngine;
using GeminiGauntlet.Audio;

/// <summary>
/// PlateItem.cs - Individual armor plate item that can be collected by pressing E when walking (not flying)
/// Extends the ScrapItem system for armor plate pickups
/// </summary>
public class PlateItem : MonoBehaviour
{
    [Header("Plate Item Configuration")]
    [Tooltip("The armor plate item data")]
    public ArmorPlateItemData plateItemData;
    
    [Tooltip("Number of plates this represents")]
    public int plateCount = 1;
    
    [Header("Collection Settings")]
    [Tooltip("Distance at which player can collect this plate")]
    public float collectionDistance = 3f;
    
    [Tooltip("Time before plate can be collected (prevents immediate pickup after spawn)")]
    public float collectionCooldown = 0.1f;
    
    [Header("Visual Effects")]
    [Tooltip("Should the plate bob up and down?")]
    public bool enableBobbing = true;
    
    [Tooltip("Bobbing speed")]
    public float bobbingSpeed = 2f;
    
    [Tooltip("Bobbing height")]
    public float bobbingHeight = 0.3f;
    
    [Tooltip("Should the plate rotate?")]
    public bool enableRotation = true;
    
    [Tooltip("Rotation speed")]
    public float rotationSpeed = 90f;
    
    [Header("UI Interaction")]
    [Tooltip("UI text to show when player is in range (E to collect)")]
    public GameObject interactionUI;
    
    [Tooltip("Text component for interaction prompt")]
    public UnityEngine.UI.Text interactionText;
    
    [Header("Audio")]
    [Tooltip("Play collection sound on pickup")]
    public bool playCollectionSound = true;
    
    [Header("Movement Restriction")]
    [Tooltip("Show debug messages for movement mode restrictions")]
    public bool showMovementDebug = false;
    
    // Internal state
    private Vector3 originalPosition;
    private bool canBeCollected = false;
    private bool playerInRange = false;
    private GameObject player;
    private InventoryManager inventoryManager;
    
    // Movement system references
    private AAAMovementIntegrator aaaMovementIntegrator;
    
    // UI state
    private bool isUIVisible = false;
    
    private void Start()
    {
        Debug.Log($"[PlateItem] ===== START CALLED FOR {gameObject.name} =====");
        originalPosition = transform.position;
        Debug.Log($"[PlateItem] Position: {transform.position}");
        
        // Find player and required components
        FindPlayerComponents();
        
        // Find inventory manager
        inventoryManager = InventoryManager.Instance;
        if (inventoryManager == null)
        {
            Debug.LogError("[PlateItem] InventoryManager.Instance not found!");
        }
        else
        {
            Debug.Log("[PlateItem] InventoryManager found!");
        }
        
        // Start collection cooldown
        Invoke(nameof(EnableCollection), collectionCooldown);
        Debug.Log($"[PlateItem] Collection will be enabled in {collectionCooldown} seconds");
        
        // Ensure we have a collider for detection
        SetupCollider();
        
        // Setup interaction UI
        SetupInteractionUI();
        
        // Validate plate data
        if (plateItemData == null)
        {
            Debug.LogError($"[PlateItem] No plate data assigned to {gameObject.name}!");
        }
        else
        {
            Debug.Log($"[PlateItem] Plate data: {plateItemData.itemName}");
        }
        
        Debug.Log($"[PlateItem] ===== INITIALIZED {gameObject.name} with {plateCount}x armor plates =====");
    }
    
    private void Update()
    {
        // Visual effects
        ApplyVisualEffects();
        
        // Handle player interaction
        HandlePlayerInteraction();
        
        // DEBUG: Show status every 2 seconds
        if (Time.frameCount % 120 == 0)
        {
            Debug.Log($"[PlateItem] Status: canCollect={canBeCollected}, playerInRange={playerInRange}, player={player != null}");
        }
    }
    
    /// <summary>
    /// Finds and caches player components needed for movement mode detection
    /// </summary>
    private void FindPlayerComponents()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[PlateItem] Player not found!");
            return;
        }
        
        // Get movement system component
        aaaMovementIntegrator = player.GetComponent<AAAMovementIntegrator>();
        if (aaaMovementIntegrator == null)
        {
            aaaMovementIntegrator = player.GetComponentInChildren<AAAMovementIntegrator>();
        }
        
        if (aaaMovementIntegrator == null)
        {
            Debug.LogWarning("[PlateItem] AAAMovementIntegrator not found on player - movement mode detection disabled");
        }
    }
    
    /// <summary>
    /// Sets up the collider for player detection
    /// </summary>
    private void SetupCollider()
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
            sphereCollider.isTrigger = true;
            sphereCollider.radius = collectionDistance;
            Debug.Log($"[PlateItem] Created new SphereCollider with radius {collectionDistance}");
        }
        else
        {
            col.isTrigger = true;
            // CRITICAL: Update existing collider radius to match collectionDistance
            if (col is SphereCollider sphereCol)
            {
                sphereCol.radius = collectionDistance;
                Debug.Log($"[PlateItem] Updated existing SphereCollider radius to {collectionDistance}");
            }
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
            
            // Update interaction text if available
            if (interactionText != null)
            {
                string itemName = plateItemData != null ? plateItemData.itemName : "Armor Plate";
                interactionText.text = $"Press E to collect {itemName}";
            }
        }
    }
    
    /// <summary>
    /// Applies visual effects (bobbing and rotation)
    /// </summary>
    private void ApplyVisualEffects()
    {
        if (enableBobbing)
        {
            float bobOffset = Mathf.Sin(Time.time * bobbingSpeed) * bobbingHeight;
            transform.position = originalPosition + Vector3.up * bobOffset;
        }
        
        if (enableRotation)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }
    
    /// <summary>
    /// Handles player interaction and input
    /// </summary>
    private void HandlePlayerInteraction()
    {
        if (!canBeCollected || !playerInRange) return;
        
        // Check for E key input
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("[PlateItem] *** E KEY PRESSED - Attempting to collect plate! ***");
            TryCollectPlate();
        }
    }
    
    /// <summary>
    /// Checks if the player is in the correct movement mode for collection
    /// DISABLED: Allow collection in both walking AND flying modes
    /// </summary>
    private bool IsPlayerInValidMovementMode()
    {
        // ALWAYS ALLOW COLLECTION - No movement mode restriction
        return true;
        
        /* ORIGINAL CODE - Movement mode restriction disabled
        if (aaaMovementIntegrator == null)
        {
            if (showMovementDebug)
            {
                Debug.LogWarning("[PlateItem] AAAMovementIntegrator not available - allowing collection (fallback mode)");
            }
            return true; // Allow collection if we can't detect movement mode
        }
        
        // Check if player is in AAA movement mode (walking)
        bool isInAAAMode = aaaMovementIntegrator.IsAAASystemActive();
        
        if (showMovementDebug)
        {
            string mode = isInAAAMode ? "Walking (AAA)" : "Flying (Celestial)";
            Debug.Log($"[PlateItem] Player movement mode: {mode}. Collection allowed: {isInAAAMode}");
        }
        
        return isInAAAMode;
        */
    }
    
    /// <summary>
    /// Attempts to collect the plate item
    /// </summary>
    public bool TryCollectPlate()
    {
        if (!canBeCollected || plateItemData == null || inventoryManager == null)
        {
            return false;
        }
        
        // Check movement mode restriction
        if (!IsPlayerInValidMovementMode())
        {
            ShowMovementModeMessage();
            return false;
        }
        
        // Try to add to inventory
        bool addedSuccessfully = false;
        
        // First try to stack with existing plates
        var existingSlot = inventoryManager.GetSlotWithItem(plateItemData);
        if (existingSlot != null)
        {
            int newCount = existingSlot.ItemCount + plateCount;
            existingSlot.SetItem(plateItemData, newCount);
            addedSuccessfully = true;
            Debug.Log($"[PlateItem] Stacked {plateCount}x {plateItemData.itemName} with existing items (total: {newCount})");
        }
        else
        {
            // Find first empty unified slot
            var emptySlot = inventoryManager.GetFirstEmptySlot();
            if (emptySlot != null)
            {
                emptySlot.SetItem(plateItemData, plateCount);
                addedSuccessfully = true;
                Debug.Log($"[PlateItem] Added {plateCount}x {plateItemData.itemName} to empty slot");
            }
        }
        
        if (addedSuccessfully)
        {
            // Trigger inventory events
            inventoryManager.OnItemAdded?.Invoke(plateItemData, plateCount);
            inventoryManager.OnInventoryChanged?.Invoke();
            
            // Play collection sound
            PlayCollectionSound();
            
            // Hide UI
            SetInteractionUIVisible(false);
            
            // Destroy this plate item
            Destroy(gameObject);
            
            Debug.Log($"[PlateItem] Successfully collected {plateCount}x {plateItemData.itemName}");
            return true;
        }
        else
        {
            Debug.Log($"[PlateItem] Cannot collect {plateItemData.itemName} - no available inventory slots!");
            ShowInventoryFullMessage();
            return false;
        }
    }
    
    /// <summary>
    /// Shows a message when player tries to collect while in wrong movement mode
    /// </summary>
    private void ShowMovementModeMessage()
    {
        if (CognitiveFeedManager.Instance != null)
        {
            CognitiveFeedManager.Instance.ShowInstantMessage("You must be walking to collect armor plates! Land on the platform first.", 2f, MessagePriority.CRITICAL);
        }
        
        Debug.Log("[PlateItem] Collection blocked - player must be in walking mode (AAA movement)");
    }
    
    /// <summary>
    /// Shows a message when inventory is full
    /// </summary>
    private void ShowInventoryFullMessage()
    {
        if (CognitiveFeedManager.Instance != null)
        {
            CognitiveFeedManager.Instance.ShowInstantMessage("Inventory full! Cannot collect armor plate.", 2f, MessagePriority.CRITICAL);
        }
    }
    
    /// <summary>
    /// Plays the collection sound
    /// </summary>
    private void PlayCollectionSound()
    {
        if (playCollectionSound)
        {
            // Use armor plate apply sound for pickup
            GameSounds.PlayArmorPlateApply(transform.position);
            Debug.Log("[PlateItem] Played plate collection sound");
        }
    }
    
    /// <summary>
    /// Sets the plate data and count
    /// </summary>
    public void SetPlateData(ArmorPlateItemData itemData, int count)
    {
        plateItemData = itemData;
        plateCount = count;
        
        // Update game object name for easier identification
        if (itemData != null)
        {
            gameObject.name = $"PlateItem_{itemData.itemName}_{count}";
        }
        
        // Update interaction UI text
        SetupInteractionUI();
    }
    
    /// <summary>
    /// Enables collection after cooldown
    /// </summary>
    private void EnableCollection()
    {
        canBeCollected = true;
        Debug.Log($"[PlateItem] *** COLLECTION ENABLED FOR {gameObject.name} ***");
        Debug.Log($"[PlateItem] Collider info: {GetComponent<Collider>()?.GetType().Name}, isTrigger={GetComponent<Collider>()?.isTrigger}");
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
        }
    }
    
    /// <summary>
    /// Get display name for UI
    /// </summary>
    public string GetDisplayName()
    {
        if (plateItemData == null) return "Unknown Plate";
        
        if (plateCount > 1)
        {
            return $"{plateItemData.itemName} x{plateCount}";
        }
        else
        {
            return plateItemData.itemName;
        }
    }
    
    // Trigger events for player detection
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[PlateItem] *** TRIGGER ENTER: {other.gameObject.name} (Tag: {other.tag}) ***");
        
        if (other.CompareTag("Player"))
        {
            Debug.Log($"[PlateItem] *** PLAYER DETECTED! ***");
            
            if (!canBeCollected)
            {
                Debug.Log($"[PlateItem] Player detected but cooldown active - will enable when ready");
            }
            else
            {
                playerInRange = true;
                SetInteractionUIVisible(true);
                Debug.Log($"[PlateItem] UI shown - Press E to collect!");
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            SetInteractionUIVisible(false);
            
            Debug.Log($"[PlateItem] Player left range of {gameObject.name}");
        }
    }
    
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // CRITICAL: If cooldown just finished and player is still in range, enable interaction
            if (canBeCollected && !playerInRange)
            {
                Debug.Log($"[PlateItem] *** COOLDOWN FINISHED - Player still in range! Enabling interaction! ***");
                playerInRange = true;
                SetInteractionUIVisible(true);
            }
            
            // Update UI visibility based on current movement mode
            if (canBeCollected && playerInRange)
            {
                bool shouldShowUI = IsPlayerInValidMovementMode();
                SetInteractionUIVisible(shouldShowUI);
            }
        }
    }
    
    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        // Draw collection range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, collectionDistance);
        
        // Draw movement mode status
        if (Application.isPlaying && playerInRange)
        {
            bool validMode = IsPlayerInValidMovementMode();
            Gizmos.color = validMode ? Color.green : Color.red;
            Gizmos.DrawSphere(transform.position + Vector3.up * 2f, 0.3f);
        }
    }
    
    // Context menu methods for testing
    [ContextMenu("Test Collection")]
    private void TestCollection()
    {
        if (Application.isPlaying)
        {
            TryCollectPlate();
        }
        else
        {
            Debug.Log("[PlateItem] Test collection can only be used in play mode");
        }
    }
    
    [ContextMenu("Force Enable Collection")]
    private void ForceEnableCollection()
    {
        canBeCollected = true;
        Debug.Log("[PlateItem] Collection force enabled");
    }
}
