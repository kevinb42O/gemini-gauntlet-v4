using UnityEngine;
using GeminiGauntlet.Audio;

/// <summary>
/// KeycardItem.cs - World item for keycards that can be picked up by the player
/// Keycards are used to open specific doors in the game
/// </summary>
public class KeycardItem : MonoBehaviour
{
    [Header("Keycard Data")]
    [Tooltip("The keycard item data (must be assigned)")]
    public ChestItemData keycardData;
    
    [Header("Collection Settings")]
    [Tooltip("Distance at which player can collect this keycard")]
    public float collectionDistance = 2.5f;
    
    [Tooltip("Should this keycard be collected automatically when player gets close?")]
    public bool autoCollect = true;
    
    [Tooltip("Time before keycard can be collected (prevents immediate pickup after drop)")]
    public float collectionCooldown = 0.5f;
    
    [Header("Visual Effects")]
    [Tooltip("Should the keycard bob up and down?")]
    public bool enableBobbing = true;
    
    [Tooltip("Bobbing speed")]
    public float bobbingSpeed = 2f;
    
    [Tooltip("Bobbing height")]
    public float bobbingHeight = 0.3f;
    
    [Tooltip("Should the keycard rotate?")]
    public bool enableRotation = true;
    
    [Tooltip("Rotation speed")]
    public float rotationSpeed = 60f;
    
    [Header("Interaction UI")]
    [Tooltip("UI text to show when player is in range")]
    public GameObject interactionUI;
    
    [Tooltip("Text component for interaction prompt")]
    public UnityEngine.UI.Text interactionText;
    
    [Header("Audio")]
    [Tooltip("Play sound when collected")]
    public bool playCollectionSound = true;
    
    // Internal state
    private Vector3 originalPosition;
    private bool canBeCollected = false;
    private bool playerInRange = false;
    private GameObject player;
    private InventoryManager inventoryManager;
    private bool isUIVisible = false;
    
    private void Start()
    {
        originalPosition = transform.position;
        
        // Find player
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[KeycardItem] Player not found!");
        }
        
        // Find inventory manager
        inventoryManager = InventoryManager.Instance;
        if (inventoryManager == null)
        {
            Debug.LogError("[KeycardItem] InventoryManager.Instance not found!");
        }
        
        // Validate keycard data
        if (keycardData == null)
        {
            Debug.LogError($"[KeycardItem] No keycard data assigned to {gameObject.name}!");
        }
        
        // Start collection cooldown
        Invoke(nameof(EnableCollection), collectionCooldown);
        
        // Setup collider
        SetupCollider();
        
        // Setup interaction UI
        SetupInteractionUI();
        
        Debug.Log($"[KeycardItem] Initialized {gameObject.name} with keycard: {(keycardData != null ? keycardData.itemName : "NONE")}");
    }
    
    private void Update()
    {
        // Visual effects
        ApplyVisualEffects();
        
        // Handle player interaction
        HandlePlayerInteraction();
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
            Debug.Log($"[KeycardItem] Added SphereCollider to {gameObject.name}");
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
            if (interactionText != null && keycardData != null)
            {
                interactionText.text = $"Press E to collect {keycardData.itemName}";
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
            TryCollectKeycard();
        }
    }
    
    /// <summary>
    /// Attempts to collect the keycard
    /// </summary>
    public bool TryCollectKeycard()
    {
        if (!canBeCollected || keycardData == null || inventoryManager == null)
        {
            Debug.LogWarning("[KeycardItem] Cannot collect - conditions not met");
            return false;
        }
        
        // Try to add to inventory
        if (inventoryManager.TryAddItem(keycardData, 1))
        {
            // Play collection sound
            PlayCollectionSound();
            
            // Show collection message
            ShowCollectionMessage();
            
            // Hide UI
            SetInteractionUIVisible(false);
            
            // Destroy this keycard item
            Destroy(gameObject);
            
            Debug.Log($"[KeycardItem] Successfully collected {keycardData.itemName}");
            return true;
        }
        else
        {
            Debug.Log($"[KeycardItem] Cannot collect {keycardData.itemName} - inventory full!");
            ShowInventoryFullMessage();
            return false;
        }
    }
    
    /// <summary>
    /// Plays the collection sound
    /// </summary>
    private void PlayCollectionSound()
    {
        if (playCollectionSound)
        {
            // Use gem collection sound for keycards
            GameSounds.PlayGemCollection(transform.position, 1.0f);
        }
    }
    
    /// <summary>
    /// Shows a message when keycard is collected
    /// </summary>
    private void ShowCollectionMessage()
    {
        if (CognitiveFeedManager.Instance != null && keycardData != null)
        {
            CognitiveFeedManager.Instance.ShowInstantMessage($"Collected {keycardData.itemName}!", 2f, MessagePriority.CRITICAL);
        }
    }
    
    /// <summary>
    /// Shows a message when inventory is full
    /// </summary>
    private void ShowInventoryFullMessage()
    {
        if (CognitiveFeedManager.Instance != null)
        {
            CognitiveFeedManager.Instance.ShowInstantMessage("Inventory full! Cannot collect keycard.", 2f, MessagePriority.CRITICAL);
        }
    }
    
    /// <summary>
    /// Enables collection after cooldown
    /// </summary>
    private void EnableCollection()
    {
        canBeCollected = true;
        Debug.Log($"[KeycardItem] Collection enabled for {gameObject.name}");
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
        if (keycardData == null) return "Unknown Keycard";
        return keycardData.itemName;
    }
    
    // Trigger events for player detection
    private void OnTriggerEnter(Collider other)
    {
        if (!canBeCollected) return;
        
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            SetInteractionUIVisible(true);
            
            Debug.Log($"[KeycardItem] Player entered range of {gameObject.name}");
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            SetInteractionUIVisible(false);
            
            Debug.Log($"[KeycardItem] Player left range of {gameObject.name}");
        }
    }
    
    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        // Draw collection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, collectionDistance);
        
        // Draw keycard indicator
        if (Application.isPlaying && playerInRange)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(transform.position + Vector3.up * 2f, 0.2f);
        }
    }
    
    // Context menu methods for testing
    [ContextMenu("Test Collection")]
    private void TestCollection()
    {
        if (Application.isPlaying)
        {
            TryCollectKeycard();
        }
        else
        {
            Debug.Log("[KeycardItem] Test collection can only be used in play mode");
        }
    }
    
    [ContextMenu("Force Enable Collection")]
    private void ForceEnableCollection()
    {
        canBeCollected = true;
        Debug.Log("[KeycardItem] Collection force enabled");
    }
}
