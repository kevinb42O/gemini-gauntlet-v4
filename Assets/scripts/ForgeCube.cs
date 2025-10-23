using UnityEngine;

/// <summary>
/// ForgeCube - Handles player interaction to open FORGE UI in-game
/// Pattern: 100% based on ShopCube.cs with FORGE-specific adaptations
/// Scale: Adjusted for 320-unit player (3.2x scale factor)
/// </summary>
public class ForgeCube : MonoBehaviour
{
    [Header("Interaction - SCALED FOR 320-UNIT PLAYER")]
    [SerializeField] private float interactionRange = 16f; // 5f × 3.2 scale
    [SerializeField] private float autoCloseDistance = 20f; // 6.25f × 3.2 scale
    
    [Header("Visual Feedback")]
    [SerializeField] private Color glowColor = new Color(1f, 0.5f, 0f); // Orange
    [SerializeField] private bool glowWhenNearby = true;
    
    private Transform playerTransform;
    private bool isPlayerNearby = false;
    private bool isForgeUIActive = false;
    private Renderer cubeRenderer;
    private Material originalMaterial;
    private Material glowMaterial;
    
    // Component references for disabling
    private PlayerShooterOrchestrator playerShooter;
    
    void Start()
    {
        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            // Try alternative ways to find player
            player = FindObjectOfType<AAAMovementController>()?.gameObject;
            if (player == null)
            {
                player = FindObjectOfType<CelestialDriftController>()?.gameObject;
            }
        }
        
        if (player != null)
        {
            playerTransform = player.transform;
            
            // Get shooter component for disabling
            playerShooter = FindObjectOfType<PlayerShooterOrchestrator>();
            
            Debug.Log($"[ForgeCube] Found player: {player.name}");
        }
        else
        {
            Debug.LogWarning("[ForgeCube] Could not find player! FORGE interactions will not work.");
        }
        
        // Set up visuals
        SetupVisuals();
    }
    
    void SetupVisuals()
    {
        cubeRenderer = GetComponent<Renderer>();
        if (cubeRenderer != null)
        {
            originalMaterial = cubeRenderer.material;
            
            if (glowWhenNearby)
            {
                // Create a glowing material
                glowMaterial = new Material(originalMaterial);
                glowMaterial.color = glowColor;
                glowMaterial.EnableKeyword("_EMISSION");
                glowMaterial.SetColor("_EmissionColor", glowColor * 0.5f);
            }
        }
    }
    
    void Update()
    {
        if (playerTransform == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        bool wasNearby = isPlayerNearby;
        isPlayerNearby = distanceToPlayer <= interactionRange;
        
        // Handle entering/leaving interaction range
        if (isPlayerNearby && !wasNearby)
        {
            OnPlayerEnterRange();
        }
        else if (!isPlayerNearby && wasNearby)
        {
            OnPlayerExitRange();
        }
        
        // Auto-close if player walks too far while FORGE is open
        if (isForgeUIActive && distanceToPlayer > autoCloseDistance)
        {
            Debug.Log("[ForgeCube] Player walked too far - auto-closing FORGE");
            ExitForgeInteraction();
        }
        
        // Handle E key input
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.E))
        {
            if (!isForgeUIActive)
            {
                StartForgeInteraction();
            }
            else
            {
                ExitForgeInteraction();
            }
        }
    }
    
    void OnPlayerEnterRange()
    {
        Debug.Log("[ForgeCube] Player entered range - showing FORGE prompt");
        
        // Apply glow effect
        if (glowWhenNearby && cubeRenderer != null && glowMaterial != null)
        {
            cubeRenderer.material = glowMaterial;
        }
        
        // 🧠 COGNITIVE INTEGRATION: Trigger interaction event
        CognitiveEvents.OnWorldInteraction?.Invoke("forge_cube_nearby", gameObject);
    }
    
    void OnPlayerExitRange()
    {
        Debug.Log("[ForgeCube] Player left range");
        
        // Remove glow effect
        if (cubeRenderer != null && originalMaterial != null)
        {
            cubeRenderer.material = originalMaterial;
        }
        
        // Close FORGE if it's open
        if (isForgeUIActive)
        {
            ExitForgeInteraction();
        }
    }
    
    void StartForgeInteraction()
    {
        if (isForgeUIActive) return;
        
        Debug.Log("[ForgeCube] Starting FORGE interaction - opening UI");
        isForgeUIActive = true;
        
        // Show FORGE UI
        if (ForgeUIManager.Instance != null)
        {
            ForgeUIManager.Instance.ShowForgeUI();
        }
        else
        {
            Debug.LogError("[ForgeCube] ForgeUIManager not found!");
            isForgeUIActive = false;
            return;
        }
        
        // Show inventory UI
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ShowInventoryUI();
            Debug.Log("[ForgeCube] Opened inventory UI");
        }
        
        // Show mouse cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Disable shooting, keep movement enabled
        if (playerShooter != null)
        {
            playerShooter.enabled = false;
        }
    }
    
    void ExitForgeInteraction()
    {
        if (!isForgeUIActive) return;
        
        Debug.Log("[ForgeCube] Exiting FORGE interaction - closing UI");
        isForgeUIActive = false;
        
        // Hide FORGE UI
        if (ForgeUIManager.Instance != null)
        {
            ForgeUIManager.Instance.HideForgeUI();
        }
        
        // Hide inventory UI
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.HideInventoryUI();
            Debug.Log("[ForgeCube] Closed inventory UI");
        }
        
        // Hide mouse cursor and lock it back
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Re-enable shooting
        if (playerShooter != null)
        {
            playerShooter.enabled = true;
        }
    }
    
    void OnDestroy()
    {
        // Cleanup: ensure shooting is re-enabled if cube is destroyed
        if (isForgeUIActive && playerShooter != null)
        {
            playerShooter.enabled = true;
        }
        
        // Reset context in ForgeManager
        if (ForgeManager.Instance != null)
        {
            ForgeManager.Instance.SetContext(ForgeContext.Menu);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw interaction range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
        
        // Draw auto-close distance
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, autoCloseDistance);
    }
}
