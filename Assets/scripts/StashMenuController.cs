using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple stash menu controller - replaces the deleted StashUIManager
/// Handles opening/closing the stash menu and initializing the StashManager
/// </summary>
public class StashMenuController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject stashPanelGameObject; // The stash panel GameObject to activate/deactivate
    public Button stashButton;
    public Button stashBackButton;
    
    [Header("System References")]
    public StashManager stashManager;
    
    [Header("Camera Control")]
    public Camera menuCamera;
    public Transform stashPanel; // The stash canvas panel to look at
    public Transform mainMenuPanel; // The main menu panel to look at
    public float cameraRotationSpeed = 3f;
    
    // Camera rotation state
    private bool isRotatingCamera = false;
    private Quaternion targetCameraRotation;
    
    private void Awake()
    {
        // Auto-find StashManager if not assigned
        if (stashManager == null)
        {
            stashManager = FindFirstObjectByType<StashManager>();
        }
        
        // Auto-find camera if not assigned
        if (menuCamera == null)
        {
            menuCamera = Camera.main;
            if (menuCamera == null)
            {
                menuCamera = FindFirstObjectByType<Camera>();
            }
        }
        
        // Make sure stash panel is inactive at start
        if (stashPanelGameObject != null)
        {
            stashPanelGameObject.SetActive(false);
        }
    }
    
    private void Update()
    {
        // Handle smooth camera rotation
        if (isRotatingCamera && menuCamera != null)
        {
            // Smoothly rotate camera to target rotation
            menuCamera.transform.rotation = Quaternion.Lerp(menuCamera.transform.rotation, targetCameraRotation, Time.deltaTime * cameraRotationSpeed);
            
            // Check if rotation is complete
            float rotationDistance = Quaternion.Angle(menuCamera.transform.rotation, targetCameraRotation);
            
            if (rotationDistance < 0.1f)
            {
                // Snap to final rotation and stop rotating
                menuCamera.transform.rotation = targetCameraRotation;
                isRotatingCamera = false;
                Debug.Log("StashMenuController: Camera rotation completed");
            }
        }
    }
    
    private void Start()
    {
        // Add button listeners
        if (stashButton != null)
        {
            stashButton.onClick.AddListener(OpenStashMenu);
            Debug.Log("StashMenuController: Stash button listener added");
        }
        else
        {
            Debug.LogError("StashMenuController: stashButton is not assigned!");
        }
        
        if (stashBackButton != null)
        {
            stashBackButton.onClick.AddListener(CloseStashMenu);
            Debug.Log("StashMenuController: Back button listener added");
        }
        else
        {
            Debug.LogWarning("StashMenuController: stashBackButton is not assigned (optional)");
        }
    }
    
    /// <summary>
    /// Open the stash menu and initialize the stash system
    /// </summary>
    public void OpenStashMenu()
    {
        Debug.Log("StashMenuController: Opening stash menu");
        
        if (stashPanelGameObject != null)
        {
            stashPanelGameObject.SetActive(true);
            Debug.Log("StashMenuController: Stash panel activated");
        }
        else
        {
            Debug.LogError("StashMenuController: stashPanelGameObject is not assigned!");
            return;
        }
        
        // Rotate camera to look at stash panel
        if (stashPanel != null && menuCamera != null)
        {
            RotateCameraToLookAt(stashPanel);
            Debug.Log("StashMenuController: Camera rotating to look at stash panel");
        }
        else if (stashPanel == null)
        {
            Debug.LogWarning("StashMenuController: stashPanel is not assigned - camera will not rotate");
        }
        
        if (stashManager != null)
        {
            stashManager.InitializeStash();
            Debug.Log("StashMenuController: StashManager initialized");
        }
        else
        {
            Debug.LogError("StashMenuController: stashManager is not assigned!");
        }
    }
    
    /// <summary>
    /// Close the stash menu
    /// </summary>
    public void CloseStashMenu()
    {
        Debug.Log("StashMenuController: Closing stash menu");
        
        // CRITICAL: Save stash data before closing
        if (stashManager != null)
        {
            stashManager.CleanupStash();
        }
        else
        {
            Debug.LogError("StashMenuController: stashManager is null - cannot save stash data!");
        }
        
        if (stashPanelGameObject != null)
        {
            stashPanelGameObject.SetActive(false);
            Debug.Log("StashMenuController: Stash panel deactivated");
        }
        else
        {
            Debug.LogError("StashMenuController: stashPanelGameObject is not assigned!");
        }
        
        // Rotate camera back to look at main menu panel
        if (mainMenuPanel != null && menuCamera != null)
        {
            RotateCameraToLookAt(mainMenuPanel);
            Debug.Log("StashMenuController: Camera rotating back to look at main menu panel");
        }
        else if (mainMenuPanel == null)
        {
            Debug.LogWarning("StashMenuController: mainMenuPanel is not assigned - camera will not rotate");
        }
    }
    
    /// <summary>
    /// Toggle the stash menu (for keyboard shortcuts)
    /// </summary>
    public void ToggleStashMenu()
    {
        if (stashPanelGameObject != null)
        {
            if (stashPanelGameObject.activeInHierarchy)
            {
                CloseStashMenu();
            }
            else
            {
                OpenStashMenu();
            }
        }
    }
    
    /// <summary>
    /// Rotate camera to look at the specified target panel
    /// </summary>
    private void RotateCameraToLookAt(Transform target)
    {
        if (menuCamera == null)
        {
            Debug.LogError("StashMenuController: Cannot rotate camera - menuCamera is null!");
            return;
        }
        
        if (target == null)
        {
            Debug.LogError("StashMenuController: Cannot rotate camera - target is null!");
            return;
        }
        
        // Calculate rotation to look at target
        Vector3 directionToTarget = target.position - menuCamera.transform.position;
        targetCameraRotation = Quaternion.LookRotation(directionToTarget);
        isRotatingCamera = true;
        
        Debug.Log($"StashMenuController: Starting camera rotation to look at {target.name}");
    }
}
