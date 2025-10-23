using UnityEngine;

public class ShopCube : MonoBehaviour
{
    [Header("Shop Configuration")]
    [SerializeField] private float interactionRange = 50f;
    [SerializeField] private Transform worldUIPosition;
    [SerializeField] private GameObject worldShopUIPrefab;
    
    [Header("Visual Feedback")]
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private Color shopCubeColor = Color.cyan;
    [SerializeField] private bool glowWhenNearby = true;
    
    private Transform playerTransform;
    private WorldShopUI currentShopUI;
    private bool isPlayerNearby = false;
    private bool isShopUIVisible = false;
    private bool isMenuInteractionActive = false;
    private Renderer cubeRenderer;
    private Material originalMaterial;
    private Material glowMaterial;
    
    // Component references for disabling
    private AAAMovementController aaaMovement;
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
            
            // Get movement and shooter components for disabling
            aaaMovement = player.GetComponent<AAAMovementController>();
            playerShooter = FindObjectOfType<PlayerShooterOrchestrator>();
            
            Debug.Log($"[ShopCube] Found player: {player.name}");
        }
        else
        {
            Debug.LogWarning("[ShopCube] Could not find player! Shop interactions will not work.");
        }
        
        // Set up visuals
        SetupVisuals();
        
        // Default world UI position to above the cube if not set
        if (worldUIPosition == null)
        {
            GameObject uiPos = new GameObject("WorldUIPosition");
            uiPos.transform.SetParent(transform);
            uiPos.transform.localPosition = Vector3.up * 2f;
            worldUIPosition = uiPos.transform;
        }
        
        // Hide interaction prompt initially
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
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
                glowMaterial.color = shopCubeColor;
                glowMaterial.EnableKeyword("_EMISSION");
                glowMaterial.SetColor("_EmissionColor", shopCubeColor * 0.5f);
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
        
        // Handle interaction input
        if (isPlayerNearby && isShopUIVisible && !isMenuInteractionActive && Input.GetKeyDown(KeyCode.E))
        {
            StartMenuInteraction();
        }
        else if (isMenuInteractionActive && Input.GetKeyDown(KeyCode.E))
        {
            ExitMenuInteraction();
        }
    }
    
    void OnPlayerEnterRange()
    {
        Debug.Log("[ShopCube] Player entered interaction range - showing static shop UI");
        
        // Apply glow effect
        if (glowWhenNearby && cubeRenderer != null && glowMaterial != null)
        {
            cubeRenderer.material = glowMaterial;
        }
        
        // Automatically show shop UI
        ShowShopUI();
        
        // Show interaction hint for menu interaction
        if (DynamicPlayerFeedManager.Instance != null)
        {
            DynamicPlayerFeedManager.Instance?.ShowCustomMessage("Press E to Interact with Menu", Color.white, null, false, 2.0f);
        }
    }
    
    void OnPlayerExitRange()
    {
        Debug.Log("[ShopCube] Player left interaction range");
        
        // Remove glow effect
        if (cubeRenderer != null && originalMaterial != null)
        {
            cubeRenderer.material = originalMaterial;
        }
        
        // Exit menu interaction if active (this will hide mouse and re-enable shooting)
        if (isMenuInteractionActive)
        {
            ExitMenuInteraction();
        }
        
        // Hide shop UI
        HideShopUI();
    }
    
    void ShowShopUI()
    {
        if (isShopUIVisible) return;
        
        Debug.Log("[ShopCube] Showing static shop UI");
        isShopUIVisible = true;
        
        // Create world shop UI
        if (worldShopUIPrefab != null && worldUIPosition != null)
        {
            GameObject shopUIObject = Instantiate(worldShopUIPrefab, worldUIPosition.position, worldUIPosition.rotation);
            currentShopUI = shopUIObject.GetComponent<WorldShopUI>();
            
            if (currentShopUI != null)
            {
                currentShopUI.Initialize(this);
            }
            else
            {
                Debug.LogError("[ShopCube] WorldShopUI component not found on shop UI prefab!");
            }
        }
        else
        {
            Debug.LogError("[ShopCube] worldShopUIPrefab or worldUIPosition not assigned!");
        }
    }
    
    void HideShopUI()
    {
        if (!isShopUIVisible) return;
        
        Debug.Log("[ShopCube] Hiding shop UI");
        isShopUIVisible = false;
        
        // Destroy world shop UI
        if (currentShopUI != null)
        {
            Destroy(currentShopUI.gameObject);
            currentShopUI = null;
        }
    }
    
    void StartMenuInteraction()
    {
        if (isMenuInteractionActive) return;
        
        Debug.Log("[ShopCube] Starting menu interaction - disabling player controls");
        isMenuInteractionActive = true;
        
        // Show mouse cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Disable only shooting, keep movement enabled
        if (playerShooter != null)
        {
            playerShooter.enabled = false;
        }
        
        // Notify WorldShopUI that interaction is active
        if (currentShopUI != null)
        {
            currentShopUI.SetInteractionActive(true);
        }
    }
    
    void ExitMenuInteraction()
    {
        if (!isMenuInteractionActive) return;
        
        Debug.Log("[ShopCube] Exiting menu interaction - re-enabling player controls");
        isMenuInteractionActive = false;
        
        // Hide mouse cursor and lock it back
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Re-enable only shooting, movement was never disabled
        if (playerShooter != null)
        {
            playerShooter.enabled = true;
        }
        
        // Notify WorldShopUI that interaction is inactive
        if (currentShopUI != null)
        {
            currentShopUI.SetInteractionActive(false);
        }
    }
    
    public void CloseShop()
    {
        // Legacy method for compatibility - calls the new exit method
        ExitMenuInteraction();
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw interaction range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
        
        // Draw UI position
        if (worldUIPosition != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(worldUIPosition.position, Vector3.one * 0.5f);
            Gizmos.DrawLine(transform.position, worldUIPosition.position);
        }
    }
}
