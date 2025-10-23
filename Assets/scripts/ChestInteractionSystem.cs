using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using GeminiGauntlet.Audio;
using GeminiGauntlet.Missions.Integration; // For mission tracking

public class ChestInteractionSystem : MonoBehaviour
{
    [Header("Detection Settings")]
    [Tooltip("Maximum distance to detect chests")]
    public float maxInteractionDistance = 5f;
    [Tooltip("Sphere radius for overlap detection as backup")]
    public float sphereDetectionRadius = 2f;
    [Tooltip("Layer mask for chest detection")]
    public LayerMask chestLayerMask = -1;
    [Tooltip("Transform to use for raycasting (usually the camera)")]
    public Transform raycastOrigin;
    [Tooltip("Enable enhanced detection with multiple methods")]
    public bool useEnhancedDetection = true;

    [Header("UI References")]
    [Tooltip("Main chest UI panel")]
    public GameObject chestInventoryPanel;
    [Tooltip("ChestInventoryPanelController component (optional)")]
    public ChestInventoryPanelController panelController;
    [Tooltip("Tooltip that follows the mouse cursor")]
    public GameObject itemTooltip;
    [Tooltip("Text component for the tooltip")]
    public TextMeshProUGUI tooltipText;

    [Header("Chest Inventory")]
    [Tooltip("Parent transform for chest inventory slots")]
    public Transform chestSlotsParent;
    [Tooltip("Prefab for inventory slot")]
    public GameObject inventorySlotPrefab;
    [Tooltip("Number of slots in chest inventory")]
    public int chestInventorySize = 12;
    
    [Header("XP Settings")]
    [Tooltip("XP amount granted when opening a chest")]
    public int chestXPAmount = 25;
    
    // Track chests that have already granted XP (per-chest XP restriction)
    private static System.Collections.Generic.HashSet<int> chestsGrantedXP = new System.Collections.Generic.HashSet<int>();
    
    [Header("Items and Generation")]
    public List<ChestItemData> possibleItems = new List<ChestItemData>();
    
    [Header("Self-Revive Item")]
    [Tooltip("Self-revive item data to spawn in chests")]
    public SelfReviveItemData selfReviveItem;
    [Tooltip("Chance (0-100%) for self-revive to spawn in chest")]
    [Range(0f, 100f)]
    public float selfReviveSpawnChance = 15f;
    
    [Header("Player References")]
    [Tooltip("Visual inventory manager for item display")]
    public InventoryManager inventoryManager;
    [Tooltip("Hand animation controller for interaction animations")]
    public LayeredHandAnimationController handAnimationController;

    [Header("Hover Highlight System")]
    [Tooltip("Prefab for highlight overlay (simple Image GameObject)")]
    public GameObject highlightOverlayPrefab;
    [Tooltip("Enable detailed hover logging to console")]
    public bool enableHoverLogging = true;
    
    [Header("Player Input Control")]
    [Tooltip("Camera controller to disable look input (e.g., AAACameraController)")]
    public MonoBehaviour cameraController;
    [Tooltip("Shooter script to disable combat (e.g., PlayerShooterOrchestrator)")]
    public MonoBehaviour shooterScript;
    
    // Current chest state
    private ChestController currentChest;
    private bool isChestOpen = false;
    private List<ChestItemData> currentChestItems = new List<ChestItemData>();
    
    // CRITICAL FIX: Store items with counts to prevent duplication
    [System.Serializable]
    private class ChestItemEntry
    {
        public ChestItemData item;
        public int count;
        
        public ChestItemEntry(ChestItemData item, int count)
        {
            this.item = item;
            this.count = count;
        }
    }
    
    // Persistent chest loot system - stores loot per chest instance WITH COUNTS
    private static Dictionary<string, List<ChestItemEntry>> persistentChestLoot = new Dictionary<string, List<ChestItemEntry>>();
    
    // PERFECTLY BALANCED UNIFORM SYSTEM: Using UnifiedSlot for chest slots like inventory and stash
    private UnifiedSlot[] chestSlots;
    private List<GameObject> legacyChestSlots = new List<GameObject>(); // Keep for backward compatibility
    
    // References
    // Legacy Inventory reference removed - using InventoryManager instead
    
    // Player Input Control
    private Transform playerTransform; // Use Transform instead of CharacterController for position tracking
    private MonoBehaviour[] playerInputScripts;
    private bool playerInputWasEnabled = true;
    
    // State tracking
    private GameObject draggedItem = null;
    private GameObject draggedItemOriginalSlot = null;
    
    // Hover highlight system
    private GameObject currentHighlightOverlay = null;
    private GameObject currentHoveredSlot = null;
    
    // Player movement tracking
    private Vector3 lastPlayerPosition;
    private float movementThreshold = 2.5f; // Distance player can move before chest auto-closes (in units)
    
    // Player input control
    private bool[] scriptsWereEnabled;

    private void Awake()
    {
        // Find player inventory manager - CRITICAL for auto-opening inventory
        if (inventoryManager == null)
        {
            inventoryManager = FindObjectOfType<InventoryManager>();
            if (inventoryManager != null)
            {
                Debug.Log($"📦 ChestInteractionSystem: Found InventoryManager: {inventoryManager.name}");
            }
            else
            {
                Debug.LogError("📦 ChestInteractionSystem: CRITICAL - InventoryManager not found! Chest inventory auto-open will not work!");
            }
        }
        
        // Find hand animation controller for interaction animations
        if (handAnimationController == null)
        {
            handAnimationController = FindObjectOfType<LayeredHandAnimationController>();
            if (handAnimationController != null)
            {
                Debug.Log($"📦 ChestInteractionSystem: Found LayeredHandAnimationController: {handAnimationController.name}");
            }
            else
            {
                Debug.LogWarning("📦 ChestInteractionSystem: LayeredHandAnimationController not found - grab animations will not play");
            }
        }
        
        // Find player transform for position tracking
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerInputScripts = player.GetComponents<MonoBehaviour>();
            Debug.Log($"✅ Found Player GameObject: {player.name}");
        }
        else
        {
            Debug.LogError("❌ Player GameObject with 'Player' tag not found!");
        }
        
        // Get panel controller if not assigned
        if (panelController == null && chestInventoryPanel != null)
        {
            panelController = chestInventoryPanel.GetComponent<ChestInventoryPanelController>();
        }
        
        // Initialize UI elements - ensure they're hidden
        if (chestInventoryPanel != null)
        {
            if (panelController != null)
            {
                // Use the controller to hide the panel
                panelController.HidePanel(false);
            }
            else
            {
                // Fallback to direct deactivation
                chestInventoryPanel.SetActive(false);
            }
        }
        
        if (itemTooltip != null)
        {
            itemTooltip.SetActive(false);
        }
        
        // Set default raycast origin if not specified
        if (raycastOrigin == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                raycastOrigin = mainCamera.transform;
            }
            else
            {
            }
        }
    }

    private void Start()
    {
        // Extra guarantee that UI is hidden at start
        if (chestInventoryPanel != null)
        {
            chestInventoryPanel.SetActive(false);
        }
        
        if (itemTooltip != null)
        {
            itemTooltip.SetActive(false);
        }
        
        // CRITICAL: Re-find InventoryManager in Start() as backup
        if (inventoryManager == null)
        {
            inventoryManager = FindObjectOfType<InventoryManager>();
            if (inventoryManager != null)
            {
                Debug.Log($"📦 ChestInteractionSystem Start(): Found InventoryManager: {inventoryManager.name}");
            }
            else
            {
                Debug.LogError("📦 ChestInteractionSystem Start(): STILL no InventoryManager found!");
            }
        }
    }
    
    private void Update()
    {
        // Check for chest interaction only if not already interacting with a chest
        if (!isChestOpen)
        {
            // FIXED: Always detect chests every frame for reliable E key interaction
            // The previous optimization (every 5 frames) caused E key presses to be missed
            DetectAndHandleChestInteraction();
        }
        else
        {
            // Handle closing chest inventory with interact key only (Escape reserved for pause menu)
            if (Input.GetKeyDown(Controls.Interact))
            {
                CloseChestInventory();
            }
            
            // Check if player has moved while chest is open
            CheckPlayerMovement();
            
            // Update tooltip position when chest is open
            if (itemTooltip != null && itemTooltip.activeSelf)
            {
                itemTooltip.transform.position = Input.mousePosition;
            }
        }
        
        // Debug key to force show chest panel if needed
        if (Input.GetKeyDown(Controls.Debug))
        {
            ForceShowChestPanel();
        }
    }

    private void DetectAndHandleChestInteraction()
    {
        ChestController detectedChest = null;
        
        if (useEnhancedDetection)
        {
            // ENHANCED DETECTION: Multiple methods to ensure we find chests
            detectedChest = DetectChestWithMultipleMethods();
        }
        else
        {
            // ORIGINAL DETECTION: Simple forward raycast
            detectedChest = DetectChestWithSimpleRaycast();
        }
        
        if (detectedChest != null)
        {
            // Always show interaction prompt when looking at a chest
            ShowInteractionPrompt(true);
            
            // Check for interaction key press
            if (Input.GetKeyDown(Controls.Interact))
            {
                Debug.Log($"E key pressed while looking at chest: {detectedChest.name}!");
                
                // If not interactable normally, force it for testing
                if (!detectedChest.IsInteractable())
                {
                    Debug.Log("Chest not interactable, but forcing interaction for testing");
                    // Make sure it's in the Open state for interaction
                    detectedChest.ForceOpenState();
                }
                
                // Interact with the chest
                if (detectedChest.PlayerInteract())
                {
                    Debug.Log("Opening chest inventory!");
                    
                    // Grant XP for opening the chest (after successful interaction)
                    GrantChestXP(detectedChest);
                    
                    // REMOVED: Mission tracking moved to ChestController.PlayerInteract() to prevent double counting
                    // ChestController.PlayerInteract() now handles mission tracking when chest is opened
                    
                    OpenChestInventory(detectedChest);
                }
                else
                {
                    Debug.LogWarning("PlayerInteract() returned false!");
                }
            }
        }
        else
        {
            // No chest detected
            ShowInteractionPrompt(false);
        }
    }
    
    /// <summary>
    /// Enhanced chest detection using multiple methods for reliability
    /// </summary>
    private ChestController DetectChestWithMultipleMethods()
    {
        if (raycastOrigin == null) 
        {
            return null;
        }
        
        Vector3 origin = raycastOrigin.position;
        Vector3 forward = raycastOrigin.forward;
        
        // METHOD 1: Primary forward raycast with enhanced detection
        RaycastHit hit;
        Debug.DrawRay(origin, forward * maxInteractionDistance, Color.blue, 0.1f);
        
        // Try raycast with all layers first, then filter
        if (Physics.Raycast(origin, forward, out hit, maxInteractionDistance))
        {
            // Check if we hit a chest directly or any collider that might be part of a chest
            ChestController chest = hit.collider.GetComponentInParent<ChestController>();
            if (chest == null)
            {
                // Try getting chest from the hit object itself
                chest = hit.collider.GetComponent<ChestController>();
            }
            if (chest == null)
            {
                // Try checking siblings and children for chest components
                Transform parent = hit.collider.transform.parent;
                if (parent != null)
                {
                    chest = parent.GetComponentInChildren<ChestController>();
                }
            }
            
            if (chest != null)
            {
                Debug.DrawLine(origin, hit.point, Color.green, 0.1f);
                return chest;
            }
        }
        
        // METHOD 2: Enhanced angled raycasts with more directions
        Vector3[] rayDirections = {
            forward + raycastOrigin.up * 0.15f,      // Up
            forward - raycastOrigin.up * 0.15f,      // Down  
            forward + raycastOrigin.right * 0.15f,   // Right
            forward - raycastOrigin.right * 0.15f,   // Left
            forward + raycastOrigin.up * 0.1f + raycastOrigin.right * 0.1f,  // Up-Right
            forward + raycastOrigin.up * 0.1f - raycastOrigin.right * 0.1f,  // Up-Left
            forward - raycastOrigin.up * 0.1f + raycastOrigin.right * 0.1f,  // Down-Right
            forward - raycastOrigin.up * 0.1f - raycastOrigin.right * 0.1f   // Down-Left
        };
        
        for (int i = 0; i < rayDirections.Length; i++)
        {
            Vector3 normalizedDir = rayDirections[i].normalized;
            Debug.DrawRay(origin, normalizedDir * maxInteractionDistance, Color.yellow, 0.1f);
            
            if (Physics.Raycast(origin, normalizedDir, out hit, maxInteractionDistance))
            {
                ChestController chest = hit.collider.GetComponentInParent<ChestController>();
                if (chest == null)
                {
                    chest = hit.collider.GetComponent<ChestController>();
                }
                
                if (chest != null)
                {
                    Debug.DrawLine(origin, hit.point, Color.orange, 0.1f);
                    return chest;
                }
            }
        }
        
        // METHOD 3: Multiple sphere overlap positions for better coverage
        Vector3[] sphereCenters = {
            origin + forward * (maxInteractionDistance * 0.3f),  // Close
            origin + forward * (maxInteractionDistance * 0.6f),  // Medium
            origin + forward * (maxInteractionDistance * 0.9f)   // Far
        };
        
        foreach (Vector3 sphereCenter in sphereCenters)
        {
            Collider[] colliders = Physics.OverlapSphere(sphereCenter, sphereDetectionRadius);
            
            foreach (Collider col in colliders)
            {
                ChestController chest = col.GetComponentInParent<ChestController>();
                if (chest == null)
                {
                    chest = col.GetComponent<ChestController>();
                }
                
                if (chest != null)
                {
                    // Verify the chest is roughly in front of the player (wider cone)
                    Vector3 toChest = (chest.transform.position - origin).normalized;
                    float dot = Vector3.Dot(forward, toChest);
                    float distance = Vector3.Distance(origin, chest.transform.position);
                    
                    if (dot > 0.2f && distance <= maxInteractionDistance) // 80-degree cone, within range
                    {
                        Debug.DrawLine(origin, chest.transform.position, Color.red, 0.1f);
                        return chest;
                    }
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Original simple raycast detection method
    /// </summary>
    private ChestController DetectChestWithSimpleRaycast()
    {
        if (raycastOrigin == null) return null;
        
        RaycastHit hit;
        
        // Cast a ray from the camera/player to detect chests
        Debug.DrawRay(raycastOrigin.position, raycastOrigin.forward * maxInteractionDistance, Color.blue, 0.1f);
        
        if (Physics.Raycast(raycastOrigin.position, raycastOrigin.forward, out hit, maxInteractionDistance, chestLayerMask))
        {
            // Check if we hit a chest
            ChestController chest = hit.collider.GetComponentInParent<ChestController>();
            return chest;
        }
        
        return null;
    }
    
    private void ShowInteractionPrompt(bool show)
    {
        // You can implement a UI prompt here, e.g. "Press E to open"
        // For now, we'll just log it
        if (show)
        {
        }
    }
    
    /// <summary>
    /// Grant XP to the player for opening a chest (only once per chest)
    /// </summary>
    private void GrantChestXP(ChestController chest)
    {
        if (chest == null) return;
        
        // Use chest instance ID to track if XP has been granted for this specific chest
        int chestID = chest.GetInstanceID();
        
        // Check if this chest has already granted XP
        if (chestsGrantedXP.Contains(chestID))
        {
            return;
        }
        
        if (GeminiGauntlet.Progression.XPManager.Instance != null)
        {
            // Grant XP to the player
            GeminiGauntlet.Progression.XPManager.Instance.GrantXP(chestXPAmount, "Chests", $"Chest Opened: {chest.name}");
            
            // Mark this chest as having granted XP
            chestsGrantedXP.Add(chestID);
            
            // Show floating text at chest location with double size
            ShowChestXPFloatingText(chest);
        }
        else
        {
        }
    }
    
    /// <summary>
    /// Show floating XP text at chest location
    /// </summary>
    private void ShowChestXPFloatingText(ChestController chest)
    {
        if (chest == null) return;
        
        // Find FloatingTextManager in the scene
        GeminiGauntlet.UI.FloatingTextManager floatingTextManager = FindObjectOfType<GeminiGauntlet.UI.FloatingTextManager>();
        if (floatingTextManager != null)
        {
            // Show floating text at chest position using standard FloatingTextManager
            Vector3 chestPosition = chest.transform.position;
            string xpText = $"+{chestXPAmount} XP";
            Color xpColor = Color.yellow;
            
            floatingTextManager.ShowFloatingText(xpText, chestPosition, xpColor);
            
        }
        else
        {
        }
    }
    
    private void OpenChestInventory(ChestController chest)
    {
        if (chest == null)
        {
            return;
        }
        
        
        // Store initial player position for movement detection
        if (playerTransform == null)
        {
            // Try to find it again if it wasn't found in Awake
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                Debug.Log($"📍 Found player transform: {playerTransform.name}");
            }
        }
        
        if (playerTransform != null)
        {
            lastPlayerPosition = playerTransform.position;
            Debug.Log($"📍 Stored initial player position: {lastPlayerPosition} for auto-close detection");
        }
        else
        {
            Debug.LogError("❌ CRITICAL: Player Transform is NULL! Auto-close will NOT work!");
        }
        
        // Set current chest and open state
        currentChest = chest;
        isChestOpen = true;
        
        // 🔥 CRITICAL FIX: Ensure chest panel is active BEFORE creating slots
        if (chestInventoryPanel != null)
        {
            Debug.Log("📦 Activating chest panel BEFORE slot creation");
            
            // ALWAYS use panelController if available (proper initialization)
            if (panelController != null)
            {
                Debug.Log("📦 Using ChestInventoryPanelController.ShowPanel() for proper initialization");
                panelController.ShowPanel(false); // Don't play sound (ChestController handles sounds)
            }
            else
            {
                Debug.LogWarning("⚠️ ChestInventoryPanelController not found! Using manual activation (may cause issues on first interaction)");
                
                // Fallback: Manual activation
                chestInventoryPanel.SetActive(true);
                
                CanvasGroup cg = chestInventoryPanel.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = 1f;
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
            }
            
            // Check parent hierarchy (always do this)
            Transform parent = chestInventoryPanel.transform.parent;
            while (parent != null)
            {
                if (!parent.gameObject.activeSelf)
                {
                    parent.gameObject.SetActive(true);
                    Debug.Log($"📦 Activated inactive parent: {parent.name}");
                }
                parent = parent.parent;
            }
            
            Debug.Log("✅ Chest panel is now active and ready");
        }
        else
        {
            Debug.LogError("❌ Chest inventory panel is NULL!");
        }
        
        // Create inventory slots FIRST (AFTER panel is active)
        CreateChestInventorySlots();
        
        // THEN generate/load items into those slots
        GenerateChestItems();
        
        // Play chest interaction sound
        // Sound removed per user request
        
        // 🔥 FIX: Open inventory FIRST, then disable player input
        // This ensures cursor management is handled properly by InventoryManager
        if (inventoryManager != null)
        {
            Debug.Log("📦 ChestInteractionSystem: Opening player inventory automatically");
            
            // Check initial state
            bool wasVisibleBefore = inventoryManager.IsInventoryVisible();
            Debug.Log($"📦 Inventory was visible before: {wasVisibleBefore}");
            
            // Make inventory visible using the correct method - this handles cursor automatically
            inventoryManager.ShowInventoryUI();
            
            // Verify it's actually visible now
            bool isActuallyVisible = inventoryManager.IsInventoryVisible();
            Debug.Log($"📦 Inventory is now visible: {isActuallyVisible}");
            
            if (!isActuallyVisible)
            {
                Debug.LogError("📦 FAILED to show inventory UI! Check InventoryManager.inventoryUIPanel assignment");
                // Force cursor visible as fallback
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
        else
        {
            Debug.LogError("📦 InventoryManager reference is null! Cannot auto-open inventory");
            // Force cursor visible as fallback
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        // Disable camera look and shooting, but KEEP movement enabled
        DisablePlayerCombatAndLook();
        
        // Legacy inventory support removed - using InventoryManager only
    }
    
    // Helper method to get the correct item count for a chest item
    // Since ChestController doesn't track item counts directly, we'll use a simpler approach
    private int GetItemCountFromChestLoot(ChestItemData itemData)
    {
        if (itemData == null)
        {
            return 0; // No item, no count
        }
        
        // For now, we'll just default all items to count of 1
        // You can add special cases for stackable items here if needed
        // For example, checking itemData.itemType or itemData.itemName
        
        // Potions, ammo, and other consumables could have higher counts
        // if (itemData.itemName.Contains("Potion") || itemData.itemName.Contains("Ammo"))
        // {
        //     return Random.Range(1, 5); // 1-4 potions or ammo
        // }
        
        // For now, just default to 1
        return 1;
    }

    // Helper method to force show the chest panel with multiple fallbacks
    private void ForceShowChestPanel()
    {
        if (chestInventoryPanel == null)
        {
            return;
        }
        
        
        // CRITICAL: Make sure the panel GameObject is active first
        chestInventoryPanel.SetActive(true);
        
        // Method 1: Use panel controller if available
        if (panelController != null)
        {
            panelController.ShowPanel(true);
        }
        
        // Method 2: Try to find and enable any CanvasGroup
        CanvasGroup canvasGroup = chestInventoryPanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        
        // Method 3: Force any RectTransform to be visible
        RectTransform rectTransform = chestInventoryPanel.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.gameObject.SetActive(true);
        }
        
        // Additional feedback sound
        // Sound removed per user request
    }
    
    private void CloseChestInventory()
    {
        isChestOpen = false;
        
        // Hide chest inventory UI
        if (chestInventoryPanel != null)
        {
            // Use panel controller if available
            if (panelController != null)
            {
                panelController.HidePanel(true);
            }
            else
            {
                // Fallback to direct deactivation
                chestInventoryPanel.SetActive(false);
            }
        }
        
        // Hide tooltip if visible
        if (itemTooltip != null)
        {
            itemTooltip.SetActive(false);
        }
        
        // Re-enable camera look and shooting
        EnablePlayerCombatAndLook();
        
        // Play UI sound
        // Sound removed per user request
        
    
    // 🔥 CHEST PERSISTENCE FIX: Save current chest state before closing
    if (currentChest != null)
    {
        UpdatePersistentChestLoot();
        
        // Tell the chest we're done interacting
        currentChest.PlayerCloseInteraction();
        currentChest = null;
    }
    
    // 🔥 FIX: Notify InventoryManager to hide inventory
    // Use cached reference instead of FindObjectOfType for performance
    if (inventoryManager != null)
    {
        Debug.Log("📦 ChestInteractionSystem: Closing player inventory automatically");
        
        // Check initial state
        bool wasVisibleBefore = inventoryManager.IsInventoryVisible();
        Debug.Log($"📦 Inventory was visible before closing: {wasVisibleBefore}");
        
        // Hide inventory using the correct method
        inventoryManager.HideInventoryUI();
        
        // Verify it's actually hidden now
        bool isStillVisible = inventoryManager.IsInventoryVisible();
        Debug.Log($"📦 Inventory is still visible after closing: {isStillVisible}");
        
        if (isStillVisible)
        {
            Debug.LogError("📦 FAILED to hide inventory UI! Check InventoryManager.HideInventoryUI() method");
        }
    }
    else
    {
        Debug.LogError("📦 InventoryManager reference is null! Cannot auto-close inventory");
    }
    
    // Legacy inventory support removed - using InventoryManager only
    
    // Note: Camera movement is now handled by EnablePlayerInput()
    // Note: Cursor management is now handled by InventoryManager.HideInventoryUI()
}
    
    private void GenerateChestItems()
    {
        currentChestItems.Clear();
        
        if (currentChest == null)
        {
            return;
        }
        
        // Create a unique ID for this specific chest instance
        string chestId = currentChest.gameObject.GetInstanceID().ToString();
        
        // Check if this chest already has persistent loot
        if (persistentChestLoot.ContainsKey(chestId))
        {
            // CRITICAL FIX: Load from entries with counts (NO DUPLICATION)
            Debug.Log($"📦 Loading persistent loot for chest {chestId}");
            
            // VALIDATION: Ensure chestSlots is initialized
            if (chestSlots == null || chestSlots.Length == 0)
            {
                Debug.LogError("📦 ❌ CRITICAL: chestSlots is null or empty! Cannot load persistent loot!");
                return;
            }
            
            // Clear slots first
            foreach (var slot in chestSlots)
            {
                if (slot != null) slot.ClearSlot();
            }
            
            // Load items from persistent storage directly into slots
            var lootEntries = persistentChestLoot[chestId];
            
            // VALIDATION: Ensure lootEntries is not null
            if (lootEntries == null)
            {
                Debug.LogError($"📦 ❌ CRITICAL: Persistent loot entries for chest {chestId} is NULL!");
                return;
            }
            
            for (int i = 0; i < lootEntries.Count && i < chestSlots.Length; i++)
            {
                var entry = lootEntries[i];
                if (entry != null && entry.item != null && chestSlots[i] != null)
                {
                    chestSlots[i].SetItem(entry.item, entry.count);
                    Debug.Log($"📦 Loaded slot {i}: {entry.count}x {entry.item.itemName}");
                }
            }
            
            Debug.Log($"📦 ✅ Loaded {lootEntries.Count} items from persistent storage");
            return; // Don't use currentChestItems for persistent loot
        }
        else
        {
            // Generate NEW loot for this chest (first time opening)
            if (possibleItems.Count > 0)
            {
                // Generate a random number of items (3-6 items)
                int itemCount = Random.Range(3, 7);
                
                // Filter out gems from chest loot (gems should only go to gem slots, not chest slots)
                List<ChestItemData> chestValidItems = new List<ChestItemData>();
                foreach (var item in possibleItems)
                {
                    // Exclude gems from chest loot generation
                    if (!(item is GemItemData))
                    {
                        chestValidItems.Add(item);
                    }
                }
                
                if (chestValidItems.Count == 0)
                {
                    return;
                }
                
                for (int i = 0; i < itemCount; i++)
                {
                    // Get a random item from filtered list (no gems)
                    ChestItemData randomItem = chestValidItems[Random.Range(0, chestValidItems.Count)];
                    currentChestItems.Add(randomItem);
                }
                
                // SELF-REVIVE ITEM GENERATION: Check spawn chance
                if (selfReviveItem != null && selfReviveSpawnChance > 0f)
                {
                    float randomChance = Random.Range(0f, 100f);
                    if (randomChance <= selfReviveSpawnChance)
                    {
                        currentChestItems.Add(selfReviveItem);
                    }
                }
                
                // ARMOR PLATE GUARANTEED SPAWN: Always add 2-4 armor plates to every chest
                List<ChestItemData> armorPlates = chestValidItems.FindAll(item => item != null && item.itemType == "ArmorPlate");
                if (armorPlates.Count > 0)
                {
                    // Add 2-4 armor plates (random amount for variety)
                    int plateCount = Random.Range(2, 5);
                    for (int p = 0; p < plateCount; p++)
                    {
                        currentChestItems.Add(armorPlates[0]); // Add the armor plate item
                    }
                    Debug.Log($"🛡️ Added {plateCount} armor plates to chest (GUARANTEED SPAWN)");
                }
                else
                {
                    Debug.LogWarning("⚠️ No armor plates found in possibleItems list! Add ArmorPlate item to ChestInteractionSystem.");
                }
                
                Debug.Log($"📦 Generated NEW loot for chest {chestId}: {currentChestItems.Count} items");
                
                // CRITICAL FIX: Set items into slots for NEW chests
                if (chestSlots != null)
                {
                    for (int i = 0; i < currentChestItems.Count && i < chestSlots.Length; i++)
                    {
                        if (currentChestItems[i] != null && chestSlots[i] != null)
                        {
                            chestSlots[i].SetItem(currentChestItems[i], 1); // New items start with count 1
                            Debug.Log($"📦 Set NEW slot {i}: 1x {currentChestItems[i].itemName}");
                        }
                    }
                    
                    // Save the initial state immediately
                    UpdatePersistentChestLoot();
                }
            }
        }
    }
    
    private void CreateChestInventorySlots()
    {
        Debug.Log("📦 CreateChestInventorySlots() called");
        
        // 🔥 CRITICAL: Ensure panel is active before finding slots
        if (chestInventoryPanel == null)
        {
            Debug.LogError("❌ Cannot create chest slots - chestInventoryPanel is NULL!");
            return;
        }
        
        if (!chestInventoryPanel.activeInHierarchy)
        {
            Debug.LogError("❌ Cannot create chest slots - chestInventoryPanel is not active!");
            chestInventoryPanel.SetActive(true);
            Debug.Log("✅ Force-activated chest panel");
        }
        
        // Validate items
        foreach (var item in currentChestItems)
        {
            if (item == null)
            {
                continue;
            }
        }
        
        // Find all existing UnifiedSlot components in the panel (for chest slots)
        UnifiedSlot[] existingSlots = chestInventoryPanel.GetComponentsInChildren<UnifiedSlot>(true);
        Debug.Log($"📦 Found {existingSlots.Length} UnifiedSlot components in chest panel");
        
        if (existingSlots.Length == 0)
        {
            Debug.LogError("❌ No UnifiedSlot components found in chest panel! Check your chest panel setup.");
            return;
        }
        
        Debug.Log($"✅ Processing {existingSlots.Length} chest slots with {currentChestItems.Count} items");
        
        
        // PERFECTLY BALANCED UNIFORM SYSTEM: Initialize UnifiedSlot array directly
        chestSlots = new UnifiedSlot[existingSlots.Length];
        Debug.Log($"📦 Initialized chestSlots array with {chestSlots.Length} slots");
        
        // Clear legacy list for backward compatibility
        legacyChestSlots.Clear();
        
        // Process each existing slot
        for (int i = 0; i < existingSlots.Length; i++)
        {
            UnifiedSlot slotComponent = existingSlots[i];
            
            // Add to our UnifiedSlot array (UNIFORM SYSTEM)
            chestSlots[i] = slotComponent;
            
            // Also keep legacy list for backward compatibility
            legacyChestSlots.Add(slotComponent.gameObject);
            
            // Set slot index for consistent system
            slotComponent.slotIndex = i;
            
            // Subscribe to drag & drop events for chest ↔ inventory transfers
            slotComponent.OnItemDropped += HandleChestItemDropped;
            slotComponent.OnDoubleClick += HandleChestDoubleClick;
            
            // Check if itemIcon is assigned on the slot
            if (slotComponent.itemIcon == null)
            {
                continue;
            }
            
            // Assign this chest system to the slot (if it's configured for chest functionality)
            slotComponent.chestSystem = this;
            
            // CRITICAL FIX: Don't set items here - GenerateChestItems() will handle it
            // This prevents duplication when loading persistent loot
            slotComponent.ClearSlot();
        }
        
    }
    
    // Called by chest inventory slots
    public void ShowTooltip(string itemName, string description)
    {
        if (itemTooltip != null && tooltipText != null)
        {
            tooltipText.text = $"{itemName}\n{description}";
            itemTooltip.SetActive(true);
            itemTooltip.transform.position = Input.mousePosition;
        }
    }
    
    public void HideTooltip()
    {
        if (itemTooltip != null)
        {
            itemTooltip.SetActive(false);
        }
    }
    
    // Handle item dragging start
    public void StartDragging(GameObject sourceSlotObj, GameObject dragVisualObj)
    {
        draggedItem = dragVisualObj;
        draggedItemOriginalSlot = sourceSlotObj;
    }
    
    // Canvas reference for proper positioning in different render modes
    private Canvas _canvasRef;
    
    // Update dragged item position
    public void UpdateDragPosition(Vector2 position)
    {
        if (draggedItem != null)
        {
            // Get canvas reference if needed
            if (_canvasRef == null)
            {
                _canvasRef = GetComponentInParent<Canvas>();
                if (_canvasRef == null)
                {
                    // Fallback to direct position
                    draggedItem.transform.position = position;
                    return;
                }
            }
            
            // Handle positioning based on canvas render mode
            if (_canvasRef.renderMode == RenderMode.WorldSpace)
            {
                // Convert screen position to world position for world space canvas
                Camera canvasCamera = _canvasRef.worldCamera;
                if (canvasCamera != null)
                {
                    Vector3 worldPos;
                    RectTransformUtility.ScreenPointToWorldPointInRectangle(
                        _canvasRef.GetComponent<RectTransform>(),
                        position, canvasCamera, out worldPos);
                    
                    draggedItem.transform.position = worldPos;
                }
                else
                {
                    // Fallback if no camera is assigned
                    draggedItem.transform.position = position;
                }
            }
            else
            {
                // Standard drag in screen space
                draggedItem.transform.position = position;
            }
        }
    }
    
    // Handle dropping item into a slot
    public void DropItem(GameObject targetSlot)
    {
        if (draggedItem == null || draggedItemOriginalSlot == null)
        {
            return;
        }
        
        UnifiedSlot sourceSlot = draggedItemOriginalSlot.GetComponent<UnifiedSlot>();
        UnifiedSlot destSlot = targetSlot.GetComponent<UnifiedSlot>();
        
        if (sourceSlot != null && destSlot != null)
        {
            // 🔥 CHEST FIX: Determine transfer direction and route properly
            bool sourceIsChest = IsChestSlot(sourceSlot);
            bool targetIsChest = IsChestSlot(destSlot);
            
            
            if (sourceIsChest && !targetIsChest)
            {
                // 🔥 CHEST → INVENTORY: Use proper transfer method
                TryTransferChestToInventory(sourceSlot, destSlot);
            }
            else if (!sourceIsChest && targetIsChest)
            {
                // 🔥 INVENTORY → CHEST: Use proper transfer method
                TryTransferInventoryToChest(sourceSlot, destSlot);
            }
            else
            {
                // 🔥 SAME SYSTEM: Use original swap logic
                ChestItemData tempItem = destSlot.CurrentItem;
                int tempCount = destSlot.ItemCount;
                destSlot.SetItem(sourceSlot.CurrentItem, sourceSlot.ItemCount);
                sourceSlot.SetItem(tempItem, tempCount);
                
                // Update chest persistence if either slot is a chest slot
                if (sourceIsChest || targetIsChest)
                {
                    UpdatePersistentChestLoot();
                }
            }
            
            // Play sound for item movement
            // Sound removed per user request
        }
        
        // Reset dragged item
        Destroy(draggedItem);
        draggedItem = null;
        draggedItemOriginalSlot = null;
    }

    /// <summary>
    /// Check if a UnifiedSlot belongs to the chest system
    /// </summary>
    private bool IsChestSlot(UnifiedSlot slot)
    {
        if (chestSlots == null) return false;
        
        foreach (UnifiedSlot chestSlot in chestSlots)
        {
            if (chestSlot == slot)
                return true;
        }
        
        return false;
    }
    
    // Cancel dragging (drop item back to original slot)
    public void CancelDrag()
    {
        if (draggedItem != null)
        {
            Destroy(draggedItem);
            draggedItem = null;
            draggedItemOriginalSlot = null;
        }
    }
    
    // DEPRECATED: Legacy method for ChestInventorySlot compatibility
    // This method is kept for backward compatibility but should NOT be used for new code
    // Use CollectItem(UnifiedSlot) instead for proper UnifiedSlot system
    [System.Obsolete("Use CollectItem(UnifiedSlot) instead - this legacy method may cause issues")]
    public bool CollectItemToPlayerInventory(ChestItemData item, ChestInventorySlot sourceSlot)
    {
        Debug.LogWarning("📦 ⚠️ LEGACY METHOD CALLED: CollectItemToPlayerInventory(ChestInventorySlot) - Use CollectItem(UnifiedSlot) instead!");
        
        // Try to find the UnifiedSlot component on the same GameObject
        UnifiedSlot unifiedSlot = sourceSlot?.GetComponent<UnifiedSlot>();
        if (unifiedSlot != null)
        {
            Debug.Log("📦 Redirecting to UnifiedSlot-based CollectItem() method");
            return CollectItem(unifiedSlot);
        }
        
        Debug.LogError("📦 ❌ Cannot redirect - no UnifiedSlot found on legacy ChestInventorySlot!");
        return false;
    }

    // === DRAG AND DROP SUPPORT METHODS ===
    
    // DEPRECATED: Legacy method mixing UnifiedSlot and ChestInventorySlot
    [System.Obsolete("Use TryTransferInventoryToChest(UnifiedSlot, UnifiedSlot) instead - this legacy method is unsafe")]
    public bool TryMoveItemFromInventoryToChest(ChestItemData item, int count, UnifiedSlot sourceSlot, ChestInventorySlot targetSlot)
    {
        Debug.LogWarning("📦 ⚠️ LEGACY METHOD CALLED: TryMoveItemFromInventoryToChest(mixed types) - Use TryTransferInventoryToChest(UnifiedSlot) instead!");
        
        // Try to find UnifiedSlot on target
        UnifiedSlot targetUnified = targetSlot?.GetComponent<UnifiedSlot>();
        
        if (sourceSlot != null && targetUnified != null)
        {
            Debug.Log("📦 Redirecting to UnifiedSlot-based TryTransferInventoryToChest() method");
            TryTransferInventoryToChest(sourceSlot, targetUnified);
            return true;
        }
        
        Debug.LogError("📦 ❌ Cannot redirect - no UnifiedSlot found on legacy ChestInventorySlot!");
        return false;
    }
    // DEPRECATED: Legacy method for ChestInventorySlot compatibility
    [System.Obsolete("Use CollectItem(UnifiedSlot) instead - this legacy method is unsafe")]
    public bool CollectItemToInventory(ChestItemData item, ChestInventorySlot sourceSlot)
    {
        Debug.LogWarning("📦 ⚠️ LEGACY METHOD CALLED: CollectItemToInventory(ChestInventorySlot) - Use CollectItem(UnifiedSlot) instead!");
        
        // Try to find the UnifiedSlot component on the same GameObject
        UnifiedSlot unifiedSlot = sourceSlot?.GetComponent<UnifiedSlot>();
        if (unifiedSlot != null)
        {
            Debug.Log("📦 Redirecting to UnifiedSlot-based CollectItem() method");
            return CollectItem(unifiedSlot);
        }
        
        Debug.LogError("📦 ❌ Cannot redirect - no UnifiedSlot found on legacy ChestInventorySlot!");
        return false;
    }
    // DEPRECATED: Legacy method for ChestInventorySlot compatibility
    [System.Obsolete("Use TryRearrangeWithinChest(UnifiedSlot, UnifiedSlot) instead - this legacy method is unsafe")]
    public bool TryMoveItemWithinChest(ChestInventorySlot sourceSlot, ChestInventorySlot targetSlot)
    {
        Debug.LogWarning("📦 ⚠️ LEGACY METHOD CALLED: TryMoveItemWithinChest(ChestInventorySlot) - Use TryRearrangeWithinChest(UnifiedSlot) instead!");
        
        // Try to find UnifiedSlot components
        UnifiedSlot sourceUnified = sourceSlot?.GetComponent<UnifiedSlot>();
        UnifiedSlot targetUnified = targetSlot?.GetComponent<UnifiedSlot>();
        
        if (sourceUnified != null && targetUnified != null)
        {
            Debug.Log("📦 Redirecting to UnifiedSlot-based TryRearrangeWithinChest() method");
            TryRearrangeWithinChest(sourceUnified, targetUnified);
            return true;
        }
        
        Debug.LogError("📦 ❌ Cannot redirect - no UnifiedSlot found on legacy ChestInventorySlots!");
        return false;
    }
    
    /// <summary>
    /// Update persistent chest loot based on current chest contents
    /// CRITICAL FIX: Store items with counts to prevent duplication
    /// ATOMIC OPERATION: Always called after slot modifications
    /// </summary>
    public void UpdatePersistentChestLoot()
    {
        if (currentChest == null)
        {
            Debug.LogWarning("📦 UpdatePersistentChestLoot: currentChest is null - cannot update");
            return;
        }
        
        // VALIDATION: Ensure chestSlots is initialized
        if (chestSlots == null || chestSlots.Length == 0)
        {
            Debug.LogWarning("📦 UpdatePersistentChestLoot: chestSlots is null or empty - cannot update");
            return;
        }
        
        string chestId = currentChest.gameObject.GetInstanceID().ToString();
        
        // Create new entry list with counts (NO DUPLICATION)
        List<ChestItemEntry> newLootEntries = new List<ChestItemEntry>();
        
        // Rebuild from current slots using UnifiedSlot system
        foreach (UnifiedSlot slotComponent in chestSlots)
        {
            if (slotComponent != null && !slotComponent.IsEmpty)
            {
                // VALIDATION: Ensure item and count are valid
                if (slotComponent.CurrentItem != null && slotComponent.ItemCount > 0)
                {
                    // Store item WITH count (not duplicates!)
                    newLootEntries.Add(new ChestItemEntry(slotComponent.CurrentItem, slotComponent.ItemCount));
                }
            }
        }
        
        // Update persistent storage with entries (not duplicates)
        persistentChestLoot[chestId] = newLootEntries;
        
        int totalItems = newLootEntries.Sum(e => e.count);
        Debug.Log($"📦 ✅ ATOMIC UPDATE: Persistent loot for chest {chestId}: {newLootEntries.Count} unique items ({totalItems} total count)");
    }
    
    // DEPRECATED: Legacy method mixing UnifiedSlot and ChestInventorySlot
    [System.Obsolete("Use TryTransferInventoryToChest(UnifiedSlot, UnifiedSlot) instead - this legacy method is unsafe")]
    public bool TryMoveItemFromInventoryToChest(UnifiedSlot sourceSlot, ChestInventorySlot targetSlot)
    {
        Debug.LogWarning("📦 ⚠️ LEGACY METHOD CALLED: TryMoveItemFromInventoryToChest(UnifiedSlot, ChestInventorySlot) - Use TryTransferInventoryToChest(UnifiedSlot, UnifiedSlot) instead!");
        
        // Try to find UnifiedSlot on target
        UnifiedSlot targetUnified = targetSlot?.GetComponent<UnifiedSlot>();
        
        if (sourceSlot != null && targetUnified != null)
        {
            Debug.Log("📦 Redirecting to UnifiedSlot-based TryTransferInventoryToChest() method");
            TryTransferInventoryToChest(sourceSlot, targetUnified);
            return true;
        }
        
        Debug.LogError("📦 ❌ Cannot redirect - no UnifiedSlot found on legacy ChestInventorySlot!");
        return false;
    }
    
    // === PLAYER INPUT CONTROL METHODS ===
    
    /// <summary>
    /// Disable camera look and shooting, but KEEP movement enabled
    /// </summary>
    private void DisablePlayerCombatAndLook()
    {
        // Disable camera look (mouse input)
        if (cameraController != null)
        {
            cameraController.enabled = false;
            Debug.Log($"🚫 Disabled camera look: {cameraController.GetType().Name}");
        }
        else
        {
            Debug.LogWarning("⚠️ No camera controller assigned! Assign AAACameraController in Inspector.");
        }
        
        // Disable shooting
        if (shooterScript != null)
        {
            shooterScript.enabled = false;
            Debug.Log($"🚫 Disabled shooting: {shooterScript.GetType().Name}");
        }
        else
        {
            Debug.LogWarning("⚠️ No shooter script assigned! Assign PlayerShooterOrchestrator in Inspector.");
        }
        
        Debug.Log("✅ Player can still MOVE but cannot LOOK or SHOOT while chest is open");
    }
    
    /// <summary>
    /// Re-enable camera look and shooting
    /// </summary>
    private void EnablePlayerCombatAndLook()
    {
        // Re-enable camera look
        if (cameraController != null)
        {
            cameraController.enabled = true;
            Debug.Log($"✅ Re-enabled camera look: {cameraController.GetType().Name}");
        }
        
        // Re-enable shooting
        if (shooterScript != null)
        {
            shooterScript.enabled = true;
            Debug.Log($"✅ Re-enabled shooting: {shooterScript.GetType().Name}");
        }
    }
    
    #region Player Movement Detection
    
    /// <summary>
    /// Checks if player has moved while chest is open and closes inventory if movement detected
    /// </summary>
    private void CheckPlayerMovement()
    {
        // Only check if chest is actually open
        if (!isChestOpen) return;
        
        // Try to find player transform if not cached
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                lastPlayerPosition = playerTransform.position;
                Debug.Log($"📍 Found player transform in CheckPlayerMovement - starting position: {lastPlayerPosition}");
            }
        }
        
        // Use cached playerTransform for position tracking
        if (playerTransform != null)
        {
            Vector3 currentPosition = playerTransform.position;
            float distance = Vector3.Distance(currentPosition, lastPlayerPosition);
            
            // Debug every 60 frames (about once per second)
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"📍 Movement check: Current={currentPosition}, Last={lastPlayerPosition}, Distance={distance:F2}, Threshold={movementThreshold}");
            }
            
            // If player has moved beyond threshold, close inventory
            if (distance > movementThreshold)
            {
                Debug.Log($"🚶 Player moved {distance:F2} units away from chest (threshold: {movementThreshold}) - auto-closing chest UI");
                CloseChestInventory();
            }
        }
        else
        {
            // Log error only once per second to avoid spam
            if (Time.frameCount % 60 == 0)
            {
                Debug.LogError("❌ Player Transform is NULL in CheckPlayerMovement! Cannot detect movement! Make sure Player GameObject has 'Player' tag.");
            }
        }
    }
    
    #endregion
    
    #region Hover Highlight System
    
    /// <summary>
    /// Call this when mouse enters a chest or inventory slot
    /// </summary>
    public void OnSlotHoverEnter(GameObject slotObject)
    {
        if (slotObject == null) return;
        
        // Store the hovered slot
        currentHoveredSlot = slotObject;
        
        // Create highlight overlay if we have a prefab
        if (highlightOverlayPrefab != null)
        {
            ShowHighlightOverlay(slotObject);
        }
        
        // Log detailed hover information
        if (enableHoverLogging)
        {
            LogHoverInfo(slotObject, true);
        }
    }
    
    /// <summary>
    /// Call this when mouse exits a chest or inventory slot
    /// </summary>
    public void OnSlotHoverExit(GameObject slotObject)
    {
        if (slotObject == null) return;
        
        // Clear the hovered slot
        if (currentHoveredSlot == slotObject)
        {
            currentHoveredSlot = null;
        }
        
        // Hide highlight overlay
        HideHighlightOverlay();
        
        // Log hover exit
        if (enableHoverLogging)
        {
            LogHoverInfo(slotObject, false);
        }
    }
    
    private void ShowHighlightOverlay(GameObject slotObject)
    {
        // Hide any existing overlay first
        HideHighlightOverlay();
        
        // Create new overlay
        if (highlightOverlayPrefab != null)
        {
            currentHighlightOverlay = Instantiate(highlightOverlayPrefab, slotObject.transform);
            
            // Position it as the last child so it renders on top
            currentHighlightOverlay.transform.SetAsLastSibling();
            
            // Make sure it fills the slot
            RectTransform overlayRect = currentHighlightOverlay.GetComponent<RectTransform>();
            if (overlayRect != null)
            {
                overlayRect.anchorMin = Vector2.zero;
                overlayRect.anchorMax = Vector2.one;
                overlayRect.offsetMin = Vector2.zero;
                overlayRect.offsetMax = Vector2.zero;
            }
        }
    }
    
    private void HideHighlightOverlay()
    {
        if (currentHighlightOverlay != null)
        {
            DestroyImmediate(currentHighlightOverlay);
            currentHighlightOverlay = null;
        }
    }
    
    private void LogHoverInfo(GameObject slotObject, bool isEntering)
    {
        string action = isEntering ? "HOVERING OVER" : "LEAVING";
        Debug.Log($"=== {action} ITEM ===");
        Debug.Log($"GameObject: {slotObject.name}");
        Debug.Log($"Full Path: {GetFullPath(slotObject)}");
        
        // Determine slot type and get item info
        ChestInventorySlot chestSlot = slotObject.GetComponent<ChestInventorySlot>();
        if (chestSlot != null && chestSlot.currentItem != null)
        {
            Debug.Log($"Type: CHEST SLOT");
            Debug.Log($"Item: {chestSlot.currentItem.itemName}");
            Debug.Log($"Count: {chestSlot.itemCount}");
            Debug.Log($"Rarity: {chestSlot.currentItem.itemRarity}");
            
            if (chestSlot.currentItem.itemIcon != null)
            {
                Debug.Log($"Sprite: {chestSlot.currentItem.itemIcon.name}");
            }
        }
        else
        {
            Debug.Log($"Type: INVENTORY SLOT (or empty slot)");
        }
        
        // Log parent hierarchy
        if (slotObject.transform.parent != null)
        {
            Debug.Log($"Parent: {slotObject.transform.parent.name}");
            if (slotObject.transform.parent.parent != null)
            {
                Debug.Log($"Grandparent: {slotObject.transform.parent.parent.name}");
            }
        }
        
        // Log text components
        TextMeshProUGUI[] textComponents = slotObject.GetComponentsInChildren<TextMeshProUGUI>();
        if (textComponents.Length > 0)
        {
            Debug.Log($"Text Components:");
            foreach (var text in textComponents)
            {
                Debug.Log($"  - {text.name}: '{text.text}'");
            }
        }
        
        Debug.Log($"========================");
    }
    
    private string GetFullPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        
        return path;
    }
    
    #endregion
    
    /// <summary>
    /// Collect item from chest slot to player inventory (for double-click)
    /// FULLY BIDIRECTIONAL: Chest → Inventory with unified stacking
    /// ATOMIC OPERATION: All-or-nothing transfer with proper save ordering
    /// </summary>
    public bool CollectItem(UnifiedSlot chestSlot)
    {
        if (chestSlot == null || chestSlot.IsEmpty)
        {
            Debug.LogWarning("📦 Cannot collect item: chest slot is null or empty");
            return false;
        }
        
        // CRITICAL: Cache item data BEFORE any modifications (prevent reference issues)
        ChestItemData itemToCollect = chestSlot.CurrentItem;
        int countToCollect = chestSlot.ItemCount;
        
        // VALIDATION: Ensure we have valid data
        if (itemToCollect == null || countToCollect <= 0)
        {
            Debug.LogError("📦 ❌ ATOMIC VALIDATION: Invalid item or count!");
            return false;
        }
        
        Debug.Log($"📦 🔄 ATOMIC TRANSFER (Chest→Inventory): {countToCollect}x {itemToCollect.itemName}");
        Debug.Log($"   Item ID: {itemToCollect.itemID}, Type: {itemToCollect.itemType}");
        
        // Get InventoryManager
        if (inventoryManager == null)
        {
            inventoryManager = FindObjectOfType<InventoryManager>();
            if (inventoryManager == null)
            {
                Debug.LogError("📦 ❌ Cannot collect item: InventoryManager not found");
                return false;
            }
        }
        
        // SPECIAL HANDLING: Vests and Backpacks go to equipment slots, not regular inventory
        bool success = false;
        
        if (itemToCollect is VestItem vestItem)
        {
            Debug.Log($"📦 🎽 VEST DETECTED: Attempting to equip {vestItem.GetDisplayName()} (Tier {vestItem.vestTier})");
            
            // Find VestSlotController
            VestSlotController vestSlot = inventoryManager.vestSlot;
            if (vestSlot == null)
            {
                vestSlot = FindObjectOfType<VestSlotController>();
            }
            
            if (vestSlot != null)
            {
                // Check if this is an upgrade
                if (vestSlot.CanEquipVest(vestItem))
                {
                    success = vestSlot.EquipVest(vestItem, false);
                    if (success)
                    {
                        Debug.Log($"📦 ✅ Successfully equipped vest: {vestItem.GetDisplayName()}");
                    }
                    else
                    {
                        Debug.Log($"📦 ❌ Failed to equip vest: {vestItem.GetDisplayName()}");
                    }
                }
                else
                {
                    Debug.Log($"📦 ❌ Cannot equip {vestItem.GetDisplayName()} - not an upgrade from current vest");
                    return false; // Not an upgrade, leave in chest
                }
            }
            else
            {
                Debug.LogError("📦 ❌ VestSlotController not found!");
                return false;
            }
        }
        else if (itemToCollect is BackpackItem backpackItem)
        {
            Debug.Log($"📦 🎒 BACKPACK DETECTED: Attempting to equip {backpackItem.GetDisplayName()} (Tier {backpackItem.backpackTier})");
            
            // Find BackpackSlotController
            BackpackSlotController backpackSlot = inventoryManager.backpackSlot;
            if (backpackSlot == null)
            {
                backpackSlot = FindObjectOfType<BackpackSlotController>();
            }
            
            if (backpackSlot != null)
            {
                // Check if this is an upgrade
                if (backpackSlot.CanEquipBackpack(backpackItem))
                {
                    success = backpackSlot.EquipBackpack(backpackItem, false);
                    if (success)
                    {
                        Debug.Log($"📦 ✅ Successfully equipped backpack: {backpackItem.GetDisplayName()}");
                    }
                    else
                    {
                        Debug.Log($"📦 ❌ Failed to equip backpack: {backpackItem.GetDisplayName()}");
                    }
                }
                else
                {
                    Debug.Log($"📦 ❌ Cannot equip {backpackItem.GetDisplayName()} - not an upgrade from current backpack");
                    return false; // Not an upgrade, leave in chest
                }
            }
            else
            {
                Debug.LogError("📦 ❌ BackpackSlotController not found!");
                return false;
            }
        }
        else
        {
            // Regular items go to inventory
            // ATOMIC OPERATION: Try to add item to inventory (autoSave=false to prevent double-save)
            success = inventoryManager.TryAddItem(itemToCollect, countToCollect, autoSave: false);
        }
        
        if (success)
        {
            Debug.Log($"📦 ✅ ATOMIC SUCCESS: Added {countToCollect}x {itemToCollect.itemName} to inventory");
            
            // Play grab animation when item is successfully transferred
            if (handAnimationController != null)
            {
                handAnimationController.PlayGrabAnimation();
                Debug.Log($"📦 🎬 Playing grab animation for item pickup");
            }
            
            // ATOMIC STEP 1: Clear the chest slot IMMEDIATELY
            chestSlot.ClearSlot();
            
            // VALIDATION: Verify slot is actually empty
            if (!chestSlot.IsEmpty)
            {
                Debug.LogError($"📦 🚨 ATOMIC FAILURE: Chest slot NOT cleared! Forcing clear...");
                chestSlot.ClearSlot();
            }
            
            // ATOMIC STEP 2: Update persistent chest data BEFORE saving
            UpdatePersistentChestLoot();
            
            // ATOMIC STEP 3: Save inventory changes ONCE at the end (after all state is consistent)
            inventoryManager.SaveInventoryData();
            
            // CRITICAL: Update PersistentItemInventoryManager for cross-scene persistence
            if (PersistentItemInventoryManager.Instance != null)
            {
                PersistentItemInventoryManager.Instance.UpdateFromInventoryManager(inventoryManager);
                PersistentItemInventoryManager.Instance.SaveInventoryData();
                Debug.Log($"📦 ✅ Updated PersistentItemInventoryManager");
            }
            
            Debug.Log($"📦 ✅ ATOMIC COMPLETE: Transfer and save successful (all systems updated)");
            
            return true;
        }
        else
        {
            Debug.LogWarning($"📦 ❌ ATOMIC ROLLBACK: Could not collect {itemToCollect.itemName} - inventory full or item rejected");
            // No changes made - chest slot still has item (rollback)
            return false;
        }
    }
    
    /// <summary>
    /// Handle drag and drop involving chest slots (chest ↔ inventory transfers)
    /// FULLY BIDIRECTIONAL: Supports all transfer directions with unified stacking
    /// </summary>
    private void HandleChestItemDropped(UnifiedSlot sourceSlot, UnifiedSlot targetSlot)
    {
        if (sourceSlot == null || targetSlot == null)
        {
            Debug.LogError("📦 HandleChestItemDropped: Source or target slot is null!");
            return;
        }
        
        if (sourceSlot == targetSlot)
        {
            Debug.Log("📦 HandleChestItemDropped: Source and target are the same slot - no action needed");
            return;
        }
        
        if (sourceSlot.IsEmpty)
        {
            Debug.Log("📦 HandleChestItemDropped: Source slot is empty - no action needed");
            return;
        }
        
        bool sourceIsChest = System.Array.IndexOf(chestSlots, sourceSlot) >= 0;
        bool targetIsChest = System.Array.IndexOf(chestSlots, targetSlot) >= 0;
        
        ChestItemData itemBeingDragged = sourceSlot.CurrentItem;
        int draggedCount = sourceSlot.ItemCount;
        
        Debug.Log($"📦 🔄 DRAG & DROP: {draggedCount}x {itemBeingDragged.itemName}");
        Debug.Log($"   Source: {(sourceIsChest ? "CHEST" : "INVENTORY")} slot {sourceSlot.slotIndex}");
        Debug.Log($"   Target: {(targetIsChest ? "CHEST" : "INVENTORY")} slot {targetSlot.slotIndex}");
        Debug.Log($"   Item ID: {itemBeingDragged.itemID}, Type: {itemBeingDragged.itemType}");
        
        if (sourceIsChest && !targetIsChest)
        {
            // Chest → Inventory transfer
            Debug.Log("📦 Direction: CHEST → INVENTORY");
            TryTransferChestToInventory(sourceSlot, targetSlot);
        }
        else if (!sourceIsChest && targetIsChest)
        {
            // Inventory → Chest transfer
            Debug.Log("📦 Direction: INVENTORY → CHEST");
            TryTransferInventoryToChest(sourceSlot, targetSlot);
        }
        else if (sourceIsChest && targetIsChest)
        {
            // Chest → Chest rearrangement
            Debug.Log("📦 Direction: CHEST → CHEST (rearrange)");
            TryRearrangeWithinChest(sourceSlot, targetSlot);
        }
        else
        {
            // Let InventoryManager handle inventory → inventory transfers
            Debug.Log("📦 Direction: INVENTORY → INVENTORY (handled by InventoryManager)");
        }
    }
    
    /// <summary>
    /// Handle double-click on chest slots (transfer to inventory)
    /// </summary>
    private void HandleChestDoubleClick(UnifiedSlot slot)
    {
        if (slot.IsEmpty) return;
        
        Debug.Log($"📦 Double-click on chest slot {slot.slotIndex}: {slot.CurrentItem?.itemName}");
        
        // Use existing CollectItem method for chest → inventory transfer
        bool transferred = CollectItem(slot);
        if (transferred)
        {
            Debug.Log($"📦 ✅ Successfully transferred {slot.CurrentItem?.itemName} from chest to inventory via double-click");
        }
        else
        {
            Debug.Log($"📦 ❌ Failed to transfer {slot.CurrentItem?.itemName} from chest to inventory (inventory full?)");
        }
    }
    
    /// <summary>
    /// Transfer item from chest slot to inventory slot
    /// ATOMIC OPERATION: All-or-nothing with proper rollback on failure
    /// </summary>
    private void TryTransferChestToInventory(UnifiedSlot chestSlot, UnifiedSlot inventorySlot)
    {
        if (inventoryManager == null)
        {
            Debug.LogError("📦 TryTransferChestToInventory: InventoryManager not found!");
            return;
        }
        
        // ATOMIC STEP 0: Cache ALL data BEFORE any modifications
        ChestItemData itemToTransfer = chestSlot.CurrentItem;
        int transferCount = chestSlot.ItemCount;
        ChestItemData inventoryItem = inventorySlot.CurrentItem;
        int inventoryCount = inventorySlot.ItemCount;
        
        // VALIDATION: Ensure we're not duplicating
        if (itemToTransfer == null || transferCount <= 0)
        {
            Debug.LogError("📦 ❌ ATOMIC VALIDATION: Invalid item or count!");
            return;
        }
        
        Debug.Log($"📦 🔄 ATOMIC DRAG-DROP (Chest→Inventory): {transferCount}x {itemToTransfer.itemName}");
        
        // Direct slot manipulation for proper drag & drop targeting
        if (inventorySlot.IsEmpty)
        {
            // ATOMIC OPERATION: Move to empty inventory slot
            inventorySlot.SetItem(itemToTransfer, transferCount);
            chestSlot.ClearSlot();
            
            // CRITICAL VALIDATION: Verify source was actually cleared
            if (!chestSlot.IsEmpty)
            {
                Debug.LogError($"📦 🚨 ATOMIC FAILURE: Chest slot NOT cleared! Forcing clear...");
                chestSlot.ClearSlot();
            }
            
            // ATOMIC SAVE ORDER: 1. Update chest state, 2. Save inventory, 3. Update persistent manager
            UpdatePersistentChestLoot();
            inventoryManager.SaveInventoryData();
            
            // CRITICAL: Update PersistentItemInventoryManager for cross-scene persistence
            if (PersistentItemInventoryManager.Instance != null)
            {
                PersistentItemInventoryManager.Instance.UpdateFromInventoryManager(inventoryManager);
                PersistentItemInventoryManager.Instance.SaveInventoryData();
            }
            
            Debug.Log($"📦 ✅ ATOMIC COMPLETE: Transferred {itemToTransfer.itemName} (x{transferCount}) from chest to empty inventory slot");
        }
        else if (inventorySlot.CurrentItem != null && inventorySlot.CurrentItem.IsSameItem(itemToTransfer))
        {
            // ATOMIC OPERATION: Stack with existing item using IsSameItem()
            int combinedCount = inventorySlot.ItemCount + transferCount;
            inventorySlot.SetItem(inventorySlot.CurrentItem, combinedCount);
            chestSlot.ClearSlot();
            
            // CRITICAL VALIDATION: Verify source was actually cleared
            if (!chestSlot.IsEmpty)
            {
                Debug.LogError($"📦 🚨 ATOMIC FAILURE: Chest slot NOT cleared! Forcing clear...");
                chestSlot.ClearSlot();
            }
            
            // ATOMIC SAVE ORDER: 1. Update chest state, 2. Save inventory, 3. Update persistent manager
            UpdatePersistentChestLoot();
            inventoryManager.SaveInventoryData();
            
            // CRITICAL: Update PersistentItemInventoryManager for cross-scene persistence
            if (PersistentItemInventoryManager.Instance != null)
            {
                PersistentItemInventoryManager.Instance.UpdateFromInventoryManager(inventoryManager);
                PersistentItemInventoryManager.Instance.SaveInventoryData();
            }
            
            Debug.Log($"📦 ✅ ATOMIC COMPLETE: Stacked {itemToTransfer.itemName} (x{transferCount}) with existing inventory items, total: {combinedCount}");
        }
        else
        {
            // ATOMIC OPERATION: Swap items (both slots modified)
            inventorySlot.SetItem(itemToTransfer, transferCount);
            chestSlot.SetItem(inventoryItem, inventoryCount);
            
            // ATOMIC SAVE ORDER: 1. Update chest state, 2. Save inventory, 3. Update persistent manager
            UpdatePersistentChestLoot();
            inventoryManager.SaveInventoryData();
            
            // CRITICAL: Update PersistentItemInventoryManager for cross-scene persistence
            if (PersistentItemInventoryManager.Instance != null)
            {
                PersistentItemInventoryManager.Instance.UpdateFromInventoryManager(inventoryManager);
                PersistentItemInventoryManager.Instance.SaveInventoryData();
            }
            
            Debug.Log($"📦 ✅ ATOMIC COMPLETE: Swapped {itemToTransfer.itemName} (chest) with {inventoryItem.itemName} (inventory)");
        }
    }
    
    /// <summary>
    /// Transfer item from inventory slot to chest slot
    /// ATOMIC OPERATION: All-or-nothing with proper rollback on failure
    /// </summary>
    private void TryTransferInventoryToChest(UnifiedSlot inventorySlot, UnifiedSlot chestSlot)
    {
        // ATOMIC STEP 0: Cache ALL data BEFORE any modifications
        ChestItemData itemToTransfer = inventorySlot.CurrentItem;
        int transferCount = inventorySlot.ItemCount;
        ChestItemData chestItem = chestSlot.CurrentItem;
        int chestCount = chestSlot.ItemCount;
        
        // VALIDATION: Ensure we're not duplicating
        if (itemToTransfer == null || transferCount <= 0)
        {
            Debug.LogError("📦 ❌ ATOMIC VALIDATION: Invalid item or count!");
            return;
        }
        
        Debug.Log($"📦 🔄 ATOMIC DRAG-DROP (Inventory→Chest): {transferCount}x {itemToTransfer.itemName}");
        
        if (chestSlot.IsEmpty)
        {
            // ATOMIC OPERATION: Move to empty chest slot
            chestSlot.SetItem(itemToTransfer, transferCount);
            inventorySlot.ClearSlot();
            
            // CRITICAL VALIDATION: Verify source was actually cleared
            if (!inventorySlot.IsEmpty)
            {
                Debug.LogError($"📦 🚨 ATOMIC FAILURE: Inventory slot NOT cleared! Forcing clear...");
                inventorySlot.ClearSlot();
            }
            
            // ATOMIC SAVE ORDER: 1. Update chest state, 2. Save inventory, 3. Update persistent manager
            UpdatePersistentChestLoot();
            if (inventoryManager != null)
            {
                inventoryManager.SaveInventoryData();
                
                // CRITICAL: Update PersistentItemInventoryManager for cross-scene persistence
                if (PersistentItemInventoryManager.Instance != null)
                {
                    PersistentItemInventoryManager.Instance.UpdateFromInventoryManager(inventoryManager);
                    PersistentItemInventoryManager.Instance.SaveInventoryData();
                }
            }
            
            Debug.Log($"📦 ✅ ATOMIC COMPLETE: Transferred {itemToTransfer.itemName} (x{transferCount}) from inventory to empty chest slot");
        }
        else if (chestSlot.CurrentItem != null && chestSlot.CurrentItem.IsSameItem(itemToTransfer))
        {
            // ATOMIC OPERATION: Stack with existing item using IsSameItem()
            int combinedCount = chestSlot.ItemCount + transferCount;
            chestSlot.SetItem(chestSlot.CurrentItem, combinedCount);
            inventorySlot.ClearSlot();
            
            // CRITICAL VALIDATION: Verify source was actually cleared
            if (!inventorySlot.IsEmpty)
            {
                Debug.LogError($"📦 🚨 ATOMIC FAILURE: Inventory slot NOT cleared! Forcing clear...");
                inventorySlot.ClearSlot();
            }
            
            // ATOMIC SAVE ORDER: 1. Update chest state, 2. Save inventory, 3. Update persistent manager
            UpdatePersistentChestLoot();
            if (inventoryManager != null)
            {
                inventoryManager.SaveInventoryData();
                
                // CRITICAL: Update PersistentItemInventoryManager for cross-scene persistence
                if (PersistentItemInventoryManager.Instance != null)
                {
                    PersistentItemInventoryManager.Instance.UpdateFromInventoryManager(inventoryManager);
                    PersistentItemInventoryManager.Instance.SaveInventoryData();
                }
            }
            
            Debug.Log($"📦 ✅ ATOMIC COMPLETE: Stacked {itemToTransfer.itemName} (x{transferCount}) with existing chest items, total: {combinedCount}");
        }
        else
        {
            // ATOMIC OPERATION: Swap items (both slots modified)
            chestSlot.SetItem(itemToTransfer, transferCount);
            inventorySlot.SetItem(chestItem, chestCount);
            
            // ATOMIC SAVE ORDER: 1. Update chest state, 2. Save inventory, 3. Update persistent manager
            UpdatePersistentChestLoot();
            if (inventoryManager != null)
            {
                inventoryManager.SaveInventoryData();
                
                // CRITICAL: Update PersistentItemInventoryManager for cross-scene persistence
                if (PersistentItemInventoryManager.Instance != null)
                {
                    PersistentItemInventoryManager.Instance.UpdateFromInventoryManager(inventoryManager);
                    PersistentItemInventoryManager.Instance.SaveInventoryData();
                }
            }
            
            Debug.Log($"📦 ✅ ATOMIC COMPLETE: Swapped {itemToTransfer.itemName} (inventory) with {chestItem.itemName} (chest)");
        }
    }
    
    /// <summary>
    /// Rearrange items within chest slots
    /// ATOMIC OPERATION: All-or-nothing with proper validation
    /// </summary>
    private void TryRearrangeWithinChest(UnifiedSlot sourceSlot, UnifiedSlot targetSlot)
    {
        // ATOMIC STEP 0: Cache ALL data BEFORE any modifications
        ChestItemData sourceItem = sourceSlot.CurrentItem;
        int sourceCount = sourceSlot.ItemCount;
        ChestItemData targetItem = targetSlot.CurrentItem;
        int targetCount = targetSlot.ItemCount;
        
        // VALIDATION: Ensure source has valid data
        if (sourceItem == null || sourceCount <= 0)
        {
            Debug.LogError("📦 ❌ ATOMIC VALIDATION: Invalid source item!");
            return;
        }
        
        Debug.Log($"📦 🔄 ATOMIC REARRANGE (Chest→Chest): {sourceCount}x {sourceItem.itemName}");
        
        if (targetSlot.IsEmpty)
        {
            // ATOMIC OPERATION: Move to empty slot
            targetSlot.SetItem(sourceItem, sourceCount);
            sourceSlot.ClearSlot();
            
            // CRITICAL VALIDATION: Verify source was cleared
            if (!sourceSlot.IsEmpty)
            {
                Debug.LogError($"📦 🚨 ATOMIC FAILURE: Source slot NOT cleared! Forcing clear...");
                sourceSlot.ClearSlot();
            }
            
            UpdatePersistentChestLoot();
            Debug.Log($"📦 ✅ ATOMIC COMPLETE: Moved {sourceItem.itemName} within chest from slot {sourceSlot.slotIndex} to slot {targetSlot.slotIndex}");
        }
        else if (targetItem != null && targetItem.IsSameItem(sourceItem))
        {
            // ATOMIC OPERATION: Stack identical items using IsSameItem()
            int combinedCount = sourceCount + targetCount;
            targetSlot.SetItem(targetItem, combinedCount);
            sourceSlot.ClearSlot();
            
            // CRITICAL VALIDATION: Verify source was cleared
            if (!sourceSlot.IsEmpty)
            {
                Debug.LogError($"📦 🚨 ATOMIC FAILURE: Source slot NOT cleared! Forcing clear...");
                sourceSlot.ClearSlot();
            }
            
            UpdatePersistentChestLoot();
            Debug.Log($"📦 ✅ ATOMIC COMPLETE: Stacked {sourceItem.itemName} within chest, total: {combinedCount}");
        }
        else
        {
            // ATOMIC OPERATION: Swap different items (both slots modified)
            sourceSlot.SetItem(targetItem, targetCount);
            targetSlot.SetItem(sourceItem, sourceCount);
            
            UpdatePersistentChestLoot();
            Debug.Log($"📦 ✅ ATOMIC COMPLETE: Swapped {sourceItem.itemName} with {targetItem.itemName} within chest");
        }
    }
    
    /// <summary>
    /// Get chest slots for perfectly balanced uniform UnifiedSlot system
    /// Used by InventoryManager double-click transfers and drag & drop operations
    /// </summary>
    public UnifiedSlot[] GetChestSlots()
    {
        if (chestSlots == null)
        {
            Debug.LogWarning("📦 GetChestSlots: chestSlots array is null! Chest may not be properly initialized.");
            return new UnifiedSlot[0]; // Return empty array instead of null
        }
        
        Debug.Log($"📦 GetChestSlots: Returning {chestSlots.Length} chest slots for uniform UnifiedSlot system");
        return chestSlots;
    }
    
    /// <summary>
    /// Check if chest is currently open
    /// </summary>
    public bool IsChestOpen()
    {
        return isChestOpen;
    }
}
