using UnityEngine;
using GeminiGauntlet.Audio;

/// <summary>
/// ScrapItem.cs - Individual scrap item that can be collected by pressing E when walking (not flying)
/// Extends the existing WorldItem system with movement mode restrictions
/// </summary>
public class ScrapItem : MonoBehaviour
{
    [Header("Scrap Item Configuration")]
    [Tooltip("The scrap item data")]
    public ChestItemData scrapItemData;
    
    [Tooltip("Number of scrap items this represents")]
    public int scrapCount = 1;
    
    [Header("Collection Settings")]
    [Tooltip("Distance at which player can collect this scrap")]
    public float collectionDistance = 3f;
    
    [Tooltip("Time before scrap can be collected (prevents immediate pickup after spawn)")]
    public float collectionCooldown = 1f;
    
    [Header("Visual Effects")]
    [Tooltip("Should the scrap bob up and down?")]
    public bool enableBobbing = true;
    
    [Tooltip("Bobbing speed")]
    public float bobbingSpeed = 2f;
    
    [Tooltip("Bobbing height")]
    public float bobbingHeight = 0.3f;
    
    [Tooltip("Should the scrap rotate?")]
    public bool enableRotation = true;
    
    [Tooltip("Rotation speed")]
    public float rotationSpeed = 90f;
    
    [Header("UI Interaction")]
    [Tooltip("UI text to show when player is in range (E to collect)")]
    public GameObject interactionUI;
    
    [Tooltip("Text component for interaction prompt")]
    public UnityEngine.UI.Text interactionText;
    
    [Header("Audio")]
    [Tooltip("Uses gem collection sound for scrap pickup")]
    public bool playCollectionSound = true;
    
    [Header("Movement Restriction")]
    [Tooltip("Show debug messages for movement mode restrictions")]
    public bool showMovementDebug = true;
    
    [Header("Animation")]
    [Tooltip("Right hand controller for pickup animation (assign R_Hand from hierarchy)")]
    public IndividualLayeredHandController rightHandController;
    
    // Internal state
    private Vector3 originalPosition;
    private bool canBeCollected = false;
    private bool playerInRange = false;
    private GameObject player;
    private InventoryManager inventoryManager;
    private ScrapSpawn parentSpawner;
    
    // Movement system references
    private PlayerMovementManager playerMovementManager;
    private AAAMovementIntegrator aaaMovementIntegrator;
    
    // UI state
    private bool isUIVisible = false;
    
    private void Start()
    {
        originalPosition = transform.position;
        
        // Find player and required components
        FindPlayerComponents();
        
        // Find inventory manager
        inventoryManager = InventoryManager.Instance;
        if (inventoryManager == null)
        {
            Debug.LogError("[ScrapItem] InventoryManager.Instance not found!");
        }
        
        // Start collection cooldown
        Invoke(nameof(EnableCollection), collectionCooldown);
        
        // Ensure we have a collider for detection
        SetupCollider();
        
        // Setup interaction UI
        SetupInteractionUI();
        
        // Validate scrap data
        if (scrapItemData == null)
        {
            Debug.LogError($"[ScrapItem] No scrap data assigned to {gameObject.name}!");
        }
        
        // Validate visual components
        ValidateVisualComponents();
    }
    
    private void Update()
    {
        // Visual effects
        ApplyVisualEffects();
        
        // Handle player interaction
        HandlePlayerInteraction();
    }
    
    /// <summary>
    /// Finds and caches player components needed for movement mode detection
    /// </summary>
    private void FindPlayerComponents()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[ScrapItem] Player not found!");
            return;
        }
        
        // Get movement system components - try both player and children
        playerMovementManager = player.GetComponent<PlayerMovementManager>();
        if (playerMovementManager == null)
        {
            playerMovementManager = player.GetComponentInChildren<PlayerMovementManager>();
        }
        
        aaaMovementIntegrator = player.GetComponent<AAAMovementIntegrator>();
        if (aaaMovementIntegrator == null)
        {
            aaaMovementIntegrator = player.GetComponentInChildren<AAAMovementIntegrator>();
        }
        
        if (playerMovementManager == null)
        {
            Debug.LogWarning("[ScrapItem] PlayerMovementManager not found on player - movement mode detection disabled");
        }
        
        if (aaaMovementIntegrator == null)
        {
            Debug.LogWarning("[ScrapItem] AAAMovementIntegrator not found on player - movement mode detection disabled");
        }
        
        // Auto-find right hand controller if not manually assigned
        if (rightHandController == null)
        {
            // Search for R_Hand in player hierarchy
            IndividualLayeredHandController[] handControllers = player.GetComponentsInChildren<IndividualLayeredHandController>();
            foreach (var controller in handControllers)
            {
                // Find the right hand (not left hand)
                if (!controller.IsLeftHand)
                {
                    rightHandController = controller;
                    Debug.Log($"[ScrapItem] Auto-found right hand controller: {controller.name}");
                    break;
                }
            }
            
            if (rightHandController == null)
            {
                Debug.LogWarning("[ScrapItem] Right hand controller not found - pickup animation will not play. Assign R_Hand manually in Inspector!");
            }
        }
        else
        {
            Debug.Log($"[ScrapItem] Using manually assigned right hand controller: {rightHandController.name}");
        }
        
        Debug.Log($"[ScrapItem] Movement components found: PMM={playerMovementManager != null}, AAA={aaaMovementIntegrator != null}, RightHand={rightHandController != null}");
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
        }
        else
        {
            col.isTrigger = true;
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
                string itemName = scrapItemData != null ? scrapItemData.itemName : "Scrap";
                interactionText.text = $"Press E to collect {itemName}";
            }
        }
    }
    
    /// <summary>
    /// Validates that the scrap item has proper visual components
    /// </summary>
    private void ValidateVisualComponents()
    {
        // Check for MeshRenderer
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        MeshRenderer[] childRenderers = GetComponentsInChildren<MeshRenderer>();
        
        if (meshRenderer == null && childRenderers.Length == 0)
        {
            Debug.LogError($"[ScrapItem] No MeshRenderer found on {gameObject.name} or its children! Scrap will be invisible.");
            
            // Try to add a basic cube as fallback
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
                meshFilter.mesh = CreateCubeMesh();
            }
            
            if (meshRenderer == null)
            {
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
                // Use a basic material
                meshRenderer.material = new Material(Shader.Find("Standard"));
                meshRenderer.material.color = new Color(0.6f, 0.4f, 0.2f); // Brown color for scrap
            }
            
            Debug.LogWarning($"[ScrapItem] Added fallback cube mesh and material to {gameObject.name}");
        }
        else
        {
            int rendererCount = (meshRenderer != null ? 1 : 0) + childRenderers.Length;
            Debug.Log($"[ScrapItem] Visual validation passed - found {rendererCount} renderer(s) on {gameObject.name}");
        }
        
        // Check for MeshFilter
        MeshFilter filter = GetComponent<MeshFilter>();
        MeshFilter[] childFilters = GetComponentsInChildren<MeshFilter>();
        
        if (filter == null && childFilters.Length == 0)
        {
            Debug.LogWarning($"[ScrapItem] No MeshFilter found on {gameObject.name} - may cause rendering issues");
        }
    }
    
    /// <summary>
    /// Creates a simple cube mesh as fallback
    /// </summary>
    private Mesh CreateCubeMesh()
    {
        GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Mesh cubeMesh = tempCube.GetComponent<MeshFilter>().mesh;
        DestroyImmediate(tempCube);
        return cubeMesh;
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
            TryCollectScrap();
        }
    }
    
    /// <summary>
    /// Checks if the player is in the correct movement mode for collection
    /// </summary>
    private bool IsPlayerInValidMovementMode()
    {
        if (aaaMovementIntegrator == null)
        {
            if (showMovementDebug)
            {
                Debug.LogWarning("[ScrapItem] AAAMovementIntegrator not available - allowing collection (fallback mode)");
            }
            return true; // Allow collection if we can't detect movement mode
        }
        
        // Check if player is in AAA movement mode (walking)
        bool isInAAAMode = aaaMovementIntegrator.IsAAASystemActive();
        
        if (showMovementDebug)
        {
            string mode = isInAAAMode ? "Walking (AAA)" : "Flying (Celestial)";
            Debug.Log($"[ScrapItem] Player movement mode: {mode}. Collection allowed: {isInAAAMode}");
        }
        
        return isInAAAMode;
    }
    
    /// <summary>
    /// Attempts to collect the scrap item using unified slot system (NOT gem slots!)
    /// </summary>
    public bool TryCollectScrap()
    {
        if (!canBeCollected || scrapItemData == null || inventoryManager == null)
        {
            return false;
        }
        
        // Check movement mode restriction
        if (!IsPlayerInValidMovementMode())
        {
            ShowMovementModeMessage();
            return false;
        }
        
        // CRITICAL: Ensure scrap doesn't go to gem slots by using unified slot system directly
        bool addedSuccessfully = false;
        
        // First try to stack with existing scrap items
        var existingSlot = inventoryManager.GetSlotWithItem(scrapItemData);
        if (existingSlot != null)
        {
            int newCount = existingSlot.ItemCount + scrapCount;
            existingSlot.SetItem(scrapItemData, newCount);
            addedSuccessfully = true;
            Debug.Log($"[ScrapItem] Stacked {scrapCount}x {scrapItemData.itemName} with existing items (total: {newCount})");
        }
        else
        {
            // Find first empty unified slot (NOT gem slot)
            var emptySlot = inventoryManager.GetFirstEmptySlot();
            if (emptySlot != null)
            {
                emptySlot.SetItem(scrapItemData, scrapCount);
                addedSuccessfully = true;
                Debug.Log($"[ScrapItem] Added {scrapCount}x {scrapItemData.itemName} to empty slot");
            }
        }
        
        if (addedSuccessfully)
        {
            // Play right hand pickup animation
            PlayPickupAnimation();
            
            // Trigger inventory events
            inventoryManager.OnItemAdded?.Invoke(scrapItemData, scrapCount);
            inventoryManager.OnInventoryChanged?.Invoke();
            
            // Notify parent spawner
            if (parentSpawner != null)
            {
                parentSpawner.OnScrapItemCollected(gameObject);
            }
            
            // Play collection sound
            PlayCollectionSound();
            
            // Hide UI
            SetInteractionUIVisible(false);
            
            // Destroy this scrap item
            Destroy(gameObject);
            
            Debug.Log($"[ScrapItem] Successfully collected {scrapCount}x {scrapItemData.itemName} to unified inventory slot");
            return true;
        }
        else
        {
            Debug.Log($"[ScrapItem] Cannot collect {scrapItemData.itemName} - no available inventory slots!");
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
            CognitiveFeedManager.Instance.ShowInstantMessage("You must be walking to collect scrap! Land on the platform first.", 2f, MessagePriority.CRITICAL);
        }
        
        Debug.Log("[ScrapItem] Collection blocked - player must be in walking mode (AAA movement)");
    }
    
    /// <summary>
    /// Shows a message when inventory is full
    /// </summary>
    private void ShowInventoryFullMessage()
    {
        if (CognitiveFeedManager.Instance != null)
        {
            CognitiveFeedManager.Instance.ShowInstantMessage("Inventory full! Cannot collect scrap.", 2f, MessagePriority.CRITICAL);
        }
    }
    
    /// <summary>
    /// Plays the right hand pickup animation
    /// </summary>
    private void PlayPickupAnimation()
    {
        if (rightHandController != null)
        {
            rightHandController.PlayGrabAnimation();
            Debug.Log("[ScrapItem] ✅ Triggered right hand GRAB animation via PlayGrabAnimation()");
        }
        else
        {
            Debug.LogError("[ScrapItem] ❌ Cannot play pickup animation - rightHandController is NULL! Assign R_Hand manually in Inspector!");
        }
    }
    
    /// <summary>
    /// Plays the collection sound
    /// </summary>
    private void PlayCollectionSound()
    {
        if (playCollectionSound)
        {
            // Use existing gem collection sound - simple and effective
            GameSounds.PlayGemCollection(transform.position, 0.8f);
            Debug.Log("[ScrapItem] Played scrap collection sound");
        }
    }
    
    /// <summary>
    /// Sets the scrap data and count
    /// </summary>
    public void SetScrapData(ChestItemData itemData, int count)
    {
        scrapItemData = itemData;
        scrapCount = count;
        
        // Update game object name for easier identification
        if (itemData != null)
        {
            gameObject.name = $"ScrapItem_{itemData.itemName}_{count}";
        }
        
        // Update interaction UI text
        SetupInteractionUI();
    }
    
    /// <summary>
    /// Sets the parent spawner reference
    /// </summary>
    public void SetParentSpawner(ScrapSpawn spawner)
    {
        parentSpawner = spawner;
    }
    
    /// <summary>
    /// Enables collection after cooldown
    /// </summary>
    private void EnableCollection()
    {
        canBeCollected = true;
        Debug.Log($"[ScrapItem] Collection enabled for {gameObject.name}");
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
        if (scrapItemData == null) return "Unknown Scrap";
        
        if (scrapCount > 1)
        {
            return $"{scrapItemData.itemName} x{scrapCount}";
        }
        else
        {
            return scrapItemData.itemName;
        }
    }
    
    // Trigger events for player detection
    private void OnTriggerEnter(Collider other)
    {
        if (!canBeCollected) return;
        
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            
            // Show interaction UI only if in valid movement mode
            if (IsPlayerInValidMovementMode())
            {
                SetInteractionUIVisible(true);
            }
            
            Debug.Log($"[ScrapItem] Player entered range of {gameObject.name}");
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            SetInteractionUIVisible(false);
            
            Debug.Log($"[ScrapItem] Player left range of {gameObject.name}");
        }
    }
    
    private void OnTriggerStay(Collider other)
    {
        if (!canBeCollected || !playerInRange) return;
        
        if (other.CompareTag("Player"))
        {
            // Update UI visibility based on current movement mode
            bool shouldShowUI = IsPlayerInValidMovementMode();
            SetInteractionUIVisible(shouldShowUI);
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
            TryCollectScrap();
        }
        else
        {
            Debug.Log("[ScrapItem] Test collection can only be used in play mode");
        }
    }
    
    [ContextMenu("Force Enable Collection")]
    private void ForceEnableCollection()
    {
        canBeCollected = true;
        Debug.Log("[ScrapItem] Collection force enabled");
    }
}
