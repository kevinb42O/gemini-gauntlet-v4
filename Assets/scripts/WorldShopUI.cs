using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WorldShopUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private Transform shopPanel;
    
    [Header("Flight Unlock UI")]
    [SerializeField] private Button flightUnlockButton;
    [SerializeField] private TextMeshProUGUI flightPriceText;
    [SerializeField] private TextMeshProUGUI flightStatusText;
    [SerializeField] private Image flightIcon;
    
    [Header("Secondary Hand Unlock UI")]
    [SerializeField] private Button secondHandUnlockButton;
    [SerializeField] private TextMeshProUGUI secondHandPriceText;
    [SerializeField] private TextMeshProUGUI secondHandStatusText;
    [SerializeField] private Image secondHandIcon;
    
    [Header("Player Info")]
    [SerializeField] private TextMeshProUGUI playerGemsText;
    [SerializeField] private Button closeButton;
    
    [Header("Shop Configuration")]
    [SerializeField] private int flightUnlockCost = 500;
    [SerializeField] private int secondHandUnlockCost = 500;
    
    [Header("Flight Unlock Item")]
    [SerializeField] private FlightUnlockItemData flightUnlockItem;
    [SerializeField] private Sprite defaultFlightIcon;
    
    private ShopCube parentShopCube;
    private PlayerProgression playerProgression;
    private CelestialDriftController celestialDrift;
    private bool isInteractionActive = false;
    
    void Start()
    {
        SetupReferences();
        SetupUI();
        SetupButtons();
        UpdateShopDisplay();
    }
    
    void SetupReferences()
    {
        // Find required components
        playerProgression = FindObjectOfType<PlayerProgression>();
        celestialDrift = FindObjectOfType<CelestialDriftController>();
        
        // Setup world canvas as static (does not follow camera)
        if (worldCanvas == null)
            worldCanvas = GetComponent<Canvas>();
            
        if (worldCanvas != null)
        {
            worldCanvas.renderMode = RenderMode.WorldSpace;
            worldCanvas.worldCamera = Camera.main;
            
            // Keep static rotation - do not follow camera
            // Player must walk around to face the shop UI
        }
    }
    
    void SetupUI()
    {
        // Set price texts
        if (flightPriceText != null)
            flightPriceText.text = $"{flightUnlockCost} Gems";
            
        if (secondHandPriceText != null)
            secondHandPriceText.text = $"{secondHandUnlockCost} Gems";
    }
    
    void SetupButtons()
    {
        // Flight unlock button
        if (flightUnlockButton != null)
        {
            flightUnlockButton.onClick.AddListener(PurchaseFlightUnlock);
        }
        
        // Second hand unlock button
        if (secondHandUnlockButton != null)
        {
            secondHandUnlockButton.onClick.AddListener(PurchaseSecondHandUnlock);
        }
        
        // Close button
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseShop);
        }
    }
    
    void Update()
    {
        // Update display every frame for real-time gem count
        UpdatePlayerGemsDisplay();
        
        // Update button interactability based on interaction state
        UpdateButtonInteractability();
    }
    
    void UpdateShopDisplay()
    {
        UpdateFlightUnlockUI();
        UpdateSecondHandUnlockUI();
        UpdatePlayerGemsDisplay();
    }
    
    void UpdateFlightUnlockUI()
    {
        bool isFlightUnlocked = IsFlightUnlocked();
        int playerGems = GetPlayerGemCount();
        bool canAfford = playerGems >= flightUnlockCost;
        
        if (flightStatusText != null)
        {
            if (isFlightUnlocked)
            {
                flightStatusText.text = "✅ UNLOCKED";
                flightStatusText.color = Color.green;
            }
            else if (canAfford)
            {
                flightStatusText.text = "Available for Purchase";
                flightStatusText.color = Color.white;
            }
            else
            {
                flightStatusText.text = "Not Enough Gems";
                flightStatusText.color = Color.red;
            }
        }
    }
    
    void UpdateSecondHandUnlockUI()
    {
        bool isSecondHandUnlocked = IsSecondHandUnlocked();
        int playerGems = GetPlayerGemCount();
        bool canAfford = playerGems >= secondHandUnlockCost;
        
        if (secondHandStatusText != null)
        {
            if (isSecondHandUnlocked)
            {
                secondHandStatusText.text = "✅ UNLOCKED";
                secondHandStatusText.color = Color.green;
            }
            else if (canAfford)
            {
                secondHandStatusText.text = "Available for Purchase";
                secondHandStatusText.color = Color.white;
            }
            else
            {
                secondHandStatusText.text = "Not Enough Gems";
                secondHandStatusText.color = Color.red;
            }
        }
    }
    
    void UpdateButtonInteractability()
    {
        bool isFlightUnlocked = IsFlightUnlocked();
        bool isSecondHandUnlocked = IsSecondHandUnlocked();
        int playerGems = GetPlayerGemCount();
        
        // Buttons only work when interaction is active (E key pressed)
        if (flightUnlockButton != null)
        {
            bool canAffordFlight = playerGems >= flightUnlockCost;
            flightUnlockButton.interactable = isInteractionActive && !isFlightUnlocked && canAffordFlight;
        }
        
        if (secondHandUnlockButton != null)
        {
            bool canAffordSecondHand = playerGems >= secondHandUnlockCost;
            secondHandUnlockButton.interactable = isInteractionActive && !isSecondHandUnlocked && canAffordSecondHand;
        }
        
        if (closeButton != null)
        {
            closeButton.interactable = isInteractionActive;
        }
    }
    
    void UpdatePlayerGemsDisplay()
    {
        if (playerGemsText != null)
        {
            int gemCount = GetPlayerGemCount();
            playerGemsText.text = $"Your Gems: {gemCount}";
        }
    }
    
    void PurchaseFlightUnlock()
    {
        if (IsFlightUnlocked())
        {
            Debug.Log("[WorldShopUI] Flight is already unlocked!");
            return;
        }
        
        int playerGems = GetPlayerGemCount();
        if (playerGems < flightUnlockCost)
        {
            Debug.Log("[WorldShopUI] Not enough gems for flight unlock!");
            ShowInsufficientGemsMessage();
            return;
        }
        
        // Deduct gems
        if (SpendGems(flightUnlockCost))
        {
            // Unlock flight
            UnlockFlight();
            
            Debug.Log("[WorldShopUI] Flight purchased and unlocked!");
            ShowPurchaseMessage("Flight Unlocked!");
            
            // Update UI
            UpdateShopDisplay();
        }
        else
        {
            Debug.LogError("[WorldShopUI] Failed to spend gems for flight unlock!");
        }
    }
    
    void PurchaseSecondHandUnlock()
    {
        if (IsSecondHandUnlocked())
        {
            Debug.Log("[WorldShopUI] Second hand is already unlocked!");
            return;
        }
        
        int playerGems = GetPlayerGemCount();
        if (playerGems < secondHandUnlockCost)
        {
            Debug.Log("[WorldShopUI] Not enough gems for second hand unlock!");
            ShowInsufficientGemsMessage();
            return;
        }
        
        // Deduct gems
        if (SpendGems(secondHandUnlockCost))
        {
            // Unlock second hand
            UnlockSecondHand();
            
            Debug.Log("[WorldShopUI] Second hand purchased and unlocked!");
            ShowPurchaseMessage("Second Hand Unlocked!");
            
            // Update UI
            UpdateShopDisplay();
        }
        else
        {
            Debug.LogError("[WorldShopUI] Failed to spend gems for second hand unlock!");
        }
    }
    
    bool IsFlightUnlocked()
    {
        if (celestialDrift != null)
        {
            return celestialDrift.isFlightUnlocked;
        }
        return false;
    }
    
    bool IsSecondHandUnlocked()
    {
        return true; // Both hands always available
    }
    
    int GetPlayerGemCount()
    {
        if (playerProgression != null)
        {
            int currentGems = playerProgression.currentSpendableGems;
        }
        return 0;
    }
    
    bool SpendGems(int amount)
    {
        if (playerProgression != null)
        {
            return playerProgression.TrySpendGems(amount);
        }
        return false;
    }
    
    void UnlockFlight()
    {
        if (celestialDrift != null)
        {
            celestialDrift.isFlightUnlocked = true;
            Debug.Log("[WorldShopUI] Flight has been unlocked!");
        }
        
        // Add flight unlock item to inventory
        AddFlightItemToInventory();
    }
    
    void AddFlightItemToInventory()
    {
        // Create flight unlock item if not assigned in inspector
        if (flightUnlockItem == null)
        {
            flightUnlockItem = ScriptableObject.CreateInstance<FlightUnlockItemData>();
            flightUnlockItem.itemName = "Flight Unlock";
            flightUnlockItem.description = "Grants the ability to fly using CelestialDrift controls";
            flightUnlockItem.itemType = "Ability";
            
            // Use default flight icon if available
            if (defaultFlightIcon != null)
            {
                flightUnlockItem.itemIcon = defaultFlightIcon;
            }
        }
        
        // Add to inventory using correct method name
        InventoryManager inventoryManager = FindObjectOfType<InventoryManager>();
        if (inventoryManager != null)
        {
            bool success = inventoryManager.TryAddItem(flightUnlockItem, 1);
            if (success)
            {
                Debug.Log("[WorldShopUI] Added Flight Unlock item to inventory");
            }
            else
            {
                Debug.LogError("[WorldShopUI] Failed to add Flight Unlock item to inventory - inventory may be full");
            }
        }
        else
        {
            Debug.LogError("[WorldShopUI] InventoryManager not found! Cannot add flight item to inventory");
        }
    }
    
    void UnlockSecondHand()
    {
        // Both hands always available - no unlock needed
        Debug.Log("[WorldShopUI] Second hand already available (no unlock required).");
    }
    
    void ShowPurchaseMessage(string message)
    {
        if (DynamicPlayerFeedManager.Instance != null)
        {
            DynamicPlayerFeedManager.Instance.ShowCustomMessage(message, Color.green, null, true, 3.0f);
        }
    }
    
    void ShowInsufficientGemsMessage()
    {
        if (DynamicPlayerFeedManager.Instance != null)
        {
            DynamicPlayerFeedManager.Instance.ShowCustomMessage("Insufficient gems for purchase!", Color.red, null, false, 2.0f);
        }
    }
    
    void CloseShop()
    {
        if (parentShopCube != null)
        {
            parentShopCube.CloseShop();
        }
    }
    
    public void Initialize(ShopCube shopCube)
    {
        parentShopCube = shopCube;
    }
    
    public void SetInteractionActive(bool active)
    {
        isInteractionActive = active;
        Debug.Log($"[WorldShopUI] Interaction set to: {(active ? "ACTIVE" : "INACTIVE")} - buttons {(active ? "enabled" : "disabled")}");
    }
    
    void OnDestroy()
    {
        // Clean up button listeners
        if (flightUnlockButton != null)
            flightUnlockButton.onClick.RemoveAllListeners();
            
        if (secondHandUnlockButton != null)
            secondHandUnlockButton.onClick.RemoveAllListeners();
            
        if (closeButton != null)
            closeButton.onClick.RemoveAllListeners();
    }
}
