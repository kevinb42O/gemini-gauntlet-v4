using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using GeminiGauntlet.Audio;

public class PowerupInventoryManager : MonoBehaviour
{
    public static PowerupInventoryManager Instance { get; private set; }

    [Header("Inventory Slots")]
    [SerializeField] private PowerupInventorySlot[] inventorySlots = new PowerupInventorySlot[8];
    
    [Header("Icon Manager")]
    [SerializeField] private PowerupIconManager iconManager;
    
    [Header("Selection Settings")]
    [SerializeField] private Color selectedSlotColor = Color.yellow;
    [SerializeField] private Color normalSlotColor = Color.white;
    [SerializeField] private float selectionScaleMultiplier = 1.1f;
    
    [Header("Input Settings")]
    [SerializeField] private float scrollSensitivity = 1f;
    [SerializeField] private bool verboseDebugging = false;
    
    
    // Current state
    private int currentSelectedSlot = 0;
    private List<PowerupData> activePowerups = new List<PowerupData>();
    
    // CRITICAL: Coroutine reference for proper cleanup
    private Coroutine _aoeSubscriptionCoroutine = null;
    
    // EXPERT LEVEL: Flag to prevent double unsubscribe
    private bool _hasUnsubscribed = false;
    
    // Visual feedback - REMOVED SCALING ANIMATIONS
    
    [Header("Testing")]
    [SerializeField] private bool grantAllPowerupsOnStart = false;
    [Space]
    [SerializeField] private bool testGrantAllPowerups = false;

    [System.Serializable]
    public class PowerupInventorySlot
    {
        public GameObject slotObject;
        public Image backgroundImage;
        public Image iconImage;
        public TextMeshProUGUI infoText;
        public Image selectionBorder; // Optional selection indicator
        public TextMeshProUGUI bonusGemsText; // For double gems bonus display
    }

    [System.Serializable]
    public class PowerupData
    {
        public PowerUpType powerupType;
        public float duration;
        public int charges;
        public bool isActive;
        
        // SIMPLE: Store activation-specific data (e.g., HomingDaggers activation duration)
        public float activationDuration; // Duration per activation for charge-based powerups
        
        public PowerupData(PowerUpType type, float dur, int chr = 1, float actDur = 0f)
        {
            powerupType = type;
            duration = dur;
            charges = chr;
            isActive = false;
            activationDuration = actDur;
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeInventory();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // REMOVED: Scaling animation setup - no more moving boxes
        
        UpdateSlotVisuals();
        SubscribeToEvents();
        
        // Grant all powerups for testing if enabled
        if (grantAllPowerupsOnStart)
        {
            GrantAllPowerupsForTesting();
        }
    }

    private void InitializeInventory()
    {
        // Initialize all slots as empty
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i].slotObject != null)
            {
                SetSlotEmpty(i);
            }
        }
        
        // Select first slot by default
        currentSelectedSlot = 0;
        
        if (verboseDebugging)
        {
            Debug.Log("[PowerupInventoryManager] Inventory initialized with 6 slots", this);
        }
    }

    private void SubscribeToEvents()
    {
        // Subscribe to powerup events
        if (PlayerProgression.Instance != null)
        {
            PlayerProgression.OnPowerUpStatusChangedForHUD += OnPowerUpStatusChanged;
        }
        
        PlayerHealth.OnPowerUpStatusChangedForHUD += OnPowerUpStatusChanged;
        PlayerAOEAbility.OnPowerUpStatusChangedForHUD += OnPowerUpStatusChanged;
        
        // CRITICAL: Subscribe to AOE charge change events for efficient sync
        // Store coroutine reference for proper cleanup
        _aoeSubscriptionCoroutine = StartCoroutine(SubscribeToAOEEventsWhenReady());
    }

    private void OnDisable()
    {
        // CRITICAL: Stop any running coroutines to prevent memory leaks
        if (_aoeSubscriptionCoroutine != null)
        {
            StopCoroutine(_aoeSubscriptionCoroutine);
            _aoeSubscriptionCoroutine = null;
        }
        
        // CRITICAL: Unsubscribe on disable to prevent memory leaks
        UnsubscribeFromAllEvents();
    }
    
    private void OnDestroy()
    {
        // CRITICAL: Unsubscribe on destroy to prevent memory leaks
        UnsubscribeFromAllEvents();
    }
    
    private void OnApplicationQuit()
    {
        // EXPERT LEVEL: Ensure cleanup on application quit
        UnsubscribeFromAllEvents();
    }

    /// <summary>
    /// EXPERT LEVEL: Centralized method to unsubscribe from all events safely
    /// Prevents double unsubscribe with flag check
    /// </summary>
    private void UnsubscribeFromAllEvents()
    {
        // EXPERT LEVEL: Prevent double unsubscribe
        if (_hasUnsubscribed)
        {
            return;
        }
        
        try
        {
            // Unsubscribe from static events (always safe to unsubscribe)
            PlayerHealth.OnPowerUpStatusChangedForHUD -= OnPowerUpStatusChanged;
            PlayerAOEAbility.OnPowerUpStatusChangedForHUD -= OnPowerUpStatusChanged;
            
            // Unsubscribe from instance events with null checks
            if (PlayerProgression.Instance != null)
            {
                PlayerProgression.OnPowerUpStatusChangedForHUD -= OnPowerUpStatusChanged;
            }
            
            if (PlayerAOEAbility.Instance != null)
            {
                PlayerAOEAbility.Instance.OnChargesChanged -= OnAOEChargesChanged;
            }
            
            // Mark as unsubscribed
            _hasUnsubscribed = true;
            
            Debug.Log("[PowerupInventoryManager] Successfully unsubscribed from all events", this);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PowerupInventoryManager] Error during event unsubscription: {ex.Message}", this);
        }
    }

    private void Update()
    {
        HandleInput();
        UpdateActivePowerupTimers();
    }
    
    /// <summary>
    /// EXPERT LEVEL: Coroutine to wait for PlayerAOEAbility initialization before subscribing to events
    /// Includes timeout, null checks, and proper cleanup
    /// </summary>
    private IEnumerator SubscribeToAOEEventsWhenReady()
    {
        float timeout = 10f; // Give up after 10 seconds
        float elapsed = 0f;
        
        // EXPERT LEVEL: Check if component is still enabled during wait
        while (PlayerAOEAbility.Instance == null && elapsed < timeout && this != null && enabled)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // EXPERT LEVEL: Verify component is still valid before subscribing
        if (this == null || !enabled)
        {
            Debug.Log("[PowerupInventoryManager] AOE subscription cancelled - component destroyed or disabled during wait");
            _aoeSubscriptionCoroutine = null;
            yield break;
        }
        
        if (PlayerAOEAbility.Instance != null)
        {
            // EXPERT LEVEL: Only subscribe if not already unsubscribed
            if (!_hasUnsubscribed)
            {
                PlayerAOEAbility.Instance.OnChargesChanged += OnAOEChargesChanged;
                Debug.Log("[PowerupInventoryManager] Successfully subscribed to AOE charge events", this);
            }
            else
            {
                Debug.Log("[PowerupInventoryManager] Skipped AOE subscription - already unsubscribed", this);
            }
        }
        else
        {
            Debug.LogError("[PowerupInventoryManager] CRITICAL: Failed to subscribe to AOE events - PlayerAOEAbility.Instance not found within timeout!", this);
        }
        
        // CRITICAL: Clear coroutine reference when complete
        _aoeSubscriptionCoroutine = null;
    }
    
    /// <summary>
    /// SINGLE SOURCE OF TRUTH: Event-based AOE charge sync from PlayerAOEAbility
    /// This is the ONLY place where inventory AOE charges are updated after initial grant
    /// </summary>
    private void OnAOEChargesChanged(int newCharges)
    {
        // Find AOE powerup in inventory
        for (int i = 0; i < activePowerups.Count; i++)
        {
            if (activePowerups[i].powerupType == PowerUpType.AOEAttack)
            {
                Debug.Log($"[PowerupInventoryManager] AOE charges synced from PlayerAOEAbility: {activePowerups[i].charges} → {newCharges}", this);
                activePowerups[i].charges = newCharges;
                
                // Remove if no charges left
                if (newCharges <= 0)
                {
                    Debug.Log("[PowerupInventoryManager] AOE depleted - removing from inventory", this);
                    activePowerups.RemoveAt(i);
                    if (currentSelectedSlot >= activePowerups.Count && activePowerups.Count > 0)
                    {
                        currentSelectedSlot = activePowerups.Count - 1;
                    }
                }
                
                // Single update after all changes
                UpdateSlotVisuals();
                break;
            }
        }
    }
    
    // REMOVED: Complex SyncAOEChargesAfterActivation coroutine - replaced with event-based system
    
    // REMOVED: Duplicate MaxHandUpgrade logic - now handled by MaxHandUpgradePowerUp.cs directly

    private void HandleInput()
    {
        // DISABLED: Powerup system inputs disabled - replaced by flip system
        // Scroll wheel and middle click are now free for other systems
        
        // ROBUST: Add null checks for input handling
        // try
        // {
        //     HandleScrollInput();
        //     HandleMiddleClickInput();
        // }
        // catch (System.Exception ex)
        // {
        //     Debug.LogError($"[PowerupInventoryManager] Error in HandleInput: {ex.Message}", this);
        // }
    }

    private void HandleScrollInput()
    {
        // DISABLED: Scroll wheel input disabled for powerup system
        // Now available for flip system or other features
        
        // float scroll = Input.GetAxis("Mouse ScrollWheel");
        // 
        // if (Mathf.Abs(scroll) > 0.01f)
        // {
        //     int direction = scroll > 0 ? 1 : -1; // Scroll up = move up list
        //     SelectSlot(currentSelectedSlot + direction);
        //     
        //     if (verboseDebugging)
        //     {
        //         Debug.Log($"[PowerupInventoryManager] Scrolled to slot {currentSelectedSlot}", this);
        //     }
        // }
    }

    private void HandleMiddleClickInput()
    {
        // DISABLED: Middle click input disabled for powerup system
        // Now available for flip system or other features
        
        // if (Input.GetMouseButtonDown(2)) // Middle mouse button
        // {
        //     ActivateSelectedPowerup();
        // }
    }

    private void SelectSlot(int slotIndex)
    {
        // ROBUST: Null check before selection
        if (activePowerups == null)
        {
            Debug.LogError("[PowerupInventoryManager] activePowerups is null in SelectSlot!", this);
            return;
        }
        
        // Wrap around the selection
        int newSlot = slotIndex;
        if (newSlot < 0)
            newSlot = GetLastOccupiedSlot();
        else if (newSlot >= inventorySlots.Length)
            newSlot = 0;
        
        // Only select slots that have powerups
        if (newSlot < activePowerups.Count)
        {
            currentSelectedSlot = newSlot;
            UpdateSlotVisuals();
        }
    }

    private int GetLastOccupiedSlot()
    {
        // ROBUST: Handle empty inventory case
        if (activePowerups == null || activePowerups.Count == 0)
        {
            return 0;
        }
        return activePowerups.Count - 1;
    }

    private void UpdateSlotVisuals()
    {
        // ROBUST: Null check for activePowerups
        if (activePowerups == null)
        {
            Debug.LogError("[PowerupInventoryManager] activePowerups is null in UpdateSlotVisuals!", this);
            return;
        }
        
        if (verboseDebugging)
        {
            Debug.Log($"[PowerupInventoryManager] UpdateSlotVisuals - Current selected slot: {currentSelectedSlot}, Active powerups: {activePowerups.Count}");
        }
        
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i].slotObject == null) continue;
            
            bool isSelected = (i == currentSelectedSlot);
            bool hasContent = i < activePowerups.Count;
            
            if (verboseDebugging)
            {
                Debug.Log($"[PowerupInventoryManager] Slot {i}: Selected={isSelected}, HasContent={hasContent}");
            }
            
            // Always show the slot object first
            inventorySlots[i].slotObject.SetActive(true);
            
            // Update selection visual
            if (inventorySlots[i].selectionBorder != null)
            {
                inventorySlots[i].selectionBorder.gameObject.SetActive(isSelected);
                if (verboseDebugging && isSelected)
                {
                    Debug.Log($"[PowerupInventoryManager] Selection border activated for slot {i}");
                }
            }
            
            // Update background color - force visibility
            if (inventorySlots[i].backgroundImage != null)
            {
                Color targetColor = isSelected ? selectedSlotColor : normalSlotColor;
                // Ensure alpha is not 0
                targetColor.a = 1f;
                inventorySlots[i].backgroundImage.color = targetColor;
                inventorySlots[i].backgroundImage.enabled = true;
                inventorySlots[i].backgroundImage.gameObject.SetActive(true);
                
                // Also check parent objects
                Transform parent = inventorySlots[i].backgroundImage.transform.parent;
                while (parent != null)
                {
                    parent.gameObject.SetActive(true);
                    parent = parent.parent;
                }
                
                if (verboseDebugging && isSelected)
                {
                    Debug.Log($"[PowerupInventoryManager] Background color set to {targetColor} for slot {i}, alpha: {targetColor.a}, enabled: {inventorySlots[i].backgroundImage.enabled}");
                }
            }
            
            // REMOVED: Scale animation - boxes don't move anymore, only highlight changes
            
            // Update slot content: either show powerup or show empty
            if (hasContent)
            {
                UpdateSlotDisplay(i);
            }
            else
            {
                SetSlotEmpty(i);
            }
        }
    }
    
    /// <summary>
    /// INDIVIDUAL: Initialize content for a specific slot only
    /// </summary>
    private void InitializeSlotContent(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < inventorySlots.Length && slotIndex < activePowerups.Count)
        {
            UpdateSlotDisplay(slotIndex);
        }
    }

    public void AddPowerup(PowerUpType powerupType, float duration, int charges = 0, float activationDuration = 0f)
    {
        // ROBUST: Validate input parameters
        if (powerupType == PowerUpType.None)
        {
            Debug.LogWarning("[PowerupInventoryManager] Cannot add powerup of type None", this);
            return;
        }
        
        if (activePowerups == null)
        {
            Debug.LogError("[PowerupInventoryManager] activePowerups list is null!", this);
            return;
        }
        
        // SINGLE SOURCE OF TRUTH: For AOE, delegate to PlayerAOEAbility completely
        if (powerupType == PowerUpType.AOEAttack)
        {
            if (PlayerAOEAbility.Instance != null)
            {
                // PlayerAOEAbility manages charges - just grant them
                if (charges > 0)
                {
                    PlayerAOEAbility.Instance.GrantAOEChargeByPowerUp(charges);
                    Debug.Log($"[PowerupInventoryManager] Granted {charges} AOE charges to PlayerAOEAbility (single source of truth)", this);
                }
                // CRITICAL: Always read from PlayerAOEAbility - it's the source of truth
                charges = PlayerAOEAbility.Instance.GetCurrentCharges();
            }
            else
            {
                Debug.LogError("[PowerupInventoryManager] PlayerAOEAbility.Instance is null! AOE powerup cannot be added.", this);
                return;
            }
        }
        
        // Check for existing powerup of same type
        int existingIndex = FindExistingPowerup(powerupType);
        
        if (existingIndex >= 0)
        {
            // Stack with existing powerup
            if (powerupType == PowerUpType.AOEAttack || powerupType == PowerUpType.HomingDaggers)
            {
                if (powerupType == PowerUpType.AOEAttack && PlayerAOEAbility.Instance != null)
                {
                    // SINGLE SOURCE OF TRUTH: Read from PlayerAOEAbility (charges already granted above)
                    activePowerups[existingIndex].charges = PlayerAOEAbility.Instance.GetCurrentCharges();
                    Debug.Log($"[PowerupInventoryManager] AOE stacked - inventory synced to {activePowerups[existingIndex].charges} charges from PlayerAOEAbility", this);
                }
                else
                {
                    // HomingDaggers managed locally
                    activePowerups[existingIndex].charges += charges;
                }
            }
            else
            {
                // SIMPLE: For duration-based powerups, just add duration
                // The actual system (PlayerHealth, PlayerProgression) handles its own duration tracking
                activePowerups[existingIndex].duration += duration;
                Debug.Log($"[PowerupInventoryManager] Stacked {powerupType} - new total duration: {activePowerups[existingIndex].duration}s", this);
            }
            
            UpdateSlotDisplay(existingIndex);
        }
        else
        {
            // Add new powerup if we have space
            if (activePowerups.Count < inventorySlots.Length)
            {
                PowerupData newPowerup = new PowerupData(powerupType, duration, charges, activationDuration);
                activePowerups.Add(newPowerup);
                
                int slotIndex = activePowerups.Count - 1;
                // INDIVIDUAL: Initialize only this new powerup's display
                InitializeSlotContent(slotIndex);
                
                // Auto-select first powerup if none selected
                if (activePowerups.Count == 1)
                {
                    currentSelectedSlot = 0;
                }
            }
            else
            {
                if (verboseDebugging)
                {
                    Debug.LogWarning("[PowerupInventoryManager] Inventory full! Cannot add more powerups.", this);
                }
            }
        }
        
        UpdateSlotVisuals();
        
        if (verboseDebugging)
        {
            Debug.Log($"[PowerupInventoryManager] Added {powerupType} - Duration: {duration}, Charges: {charges}", this);
        }
    }
    
    /// <summary>
    /// SIMPLE: Clear all powerups from inventory (for death/reset scenarios)
    /// Each powerup system handles its own deactivation through its coroutines
    /// </summary>
    public void ClearAllPowerupsAndEffects()
    {
        Debug.Log("[PowerupInventoryManager] Clearing all powerups from inventory", this);
        
        // Clear all powerups from inventory
        activePowerups.Clear();
        currentSelectedSlot = 0;
        
        // Clear all UI slots
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            SetSlotEmpty(i);
        }
        
        UpdateSlotVisuals();
        Debug.Log("[PowerupInventoryManager] All powerups cleared from inventory", this);
    }
    
    /// <summary>
    /// SIMPLE: Just mark powerup as inactive - each system handles its own deactivation
    /// </summary>
    private void DeactivatePowerup(int slotIndex)
    {
        if (slotIndex >= activePowerups.Count) return;
        
        PowerupData powerup = activePowerups[slotIndex];
        if (!powerup.isActive) return;
        
        // Just mark as inactive - the actual system (PlayerHealth, PlayerProgression, etc.) 
        // handles its own deactivation through its own coroutines
        powerup.isActive = false;
        
        Debug.Log($"[PowerupInventoryManager] Marked {powerup.powerupType} as inactive in slot {slotIndex}", this);
    }
    
    /// <summary>
    /// PUBLIC: Manually deactivate all instances of a specific powerup type
    /// </summary>
    public void DeactivateAllPowerupsOfType(PowerUpType powerupType)
    {
        Debug.Log($"[PowerupInventoryManager] Manually deactivating all {powerupType} powerups", this);
        
        bool foundAny = false;
        for (int i = 0; i < activePowerups.Count; i++)
        {
            if (activePowerups[i].powerupType == powerupType && activePowerups[i].isActive)
            {
                DeactivatePowerup(i);
                foundAny = true;
            }
        }
        
        if (!foundAny)
        {
            Debug.Log($"[PowerupInventoryManager] No active {powerupType} powerups found to deactivate", this);
        }
        
        UpdateSlotVisuals();
    }

    private int FindExistingPowerup(PowerUpType powerupType)
    {
        for (int i = 0; i < activePowerups.Count; i++)
        {
            if (activePowerups[i].powerupType == powerupType)
            {
                return i;
            }
        }
        return -1;
    }

    private void UpdateSlotDisplay(int slotIndex)
    {
        if (slotIndex >= inventorySlots.Length || slotIndex >= activePowerups.Count) return;
        
        PowerupData powerup = activePowerups[slotIndex];
        PowerupInventorySlot slot = inventorySlots[slotIndex];
        
        // CRITICAL: Add null checks for destroyed UI elements
        if (slot == null || slot.slotObject == null)
        {
            Debug.LogWarning($"[PowerupInventoryManager] Slot {slotIndex} or its GameObject is null/destroyed. Skipping update.");
            return;
        }
        
        // Set icon
        if (slot.iconImage != null && slot.iconImage.gameObject != null)
        {
            slot.iconImage.sprite = GetIconForPowerupType(powerup.powerupType);
            slot.iconImage.gameObject.SetActive(true);
        }
        
        // Set info text
        if (slot.infoText != null && slot.infoText.gameObject != null)
        {
            string displayText = GetDisplayTextForPowerup(powerup);
            slot.infoText.text = displayText;
            slot.infoText.gameObject.SetActive(!string.IsNullOrEmpty(displayText));
        }
        
        // Activate slot safely
        if (slot.slotObject != null)
        {
            slot.slotObject.SetActive(true);
        }
    }

    private void SetSlotEmpty(int slotIndex)
    {
        if (slotIndex >= inventorySlots.Length) return;
        
        PowerupInventorySlot slot = inventorySlots[slotIndex];
        
        // CRITICAL: Add null checks for destroyed UI elements
        if (slot == null || slot.slotObject == null)
        {
            Debug.LogWarning($"[PowerupInventoryManager] Slot {slotIndex} or its GameObject is null/destroyed. Skipping empty slot setup.");
            return;
        }
        
        if (slot.iconImage != null && slot.iconImage.gameObject != null)
        {
            slot.iconImage.gameObject.SetActive(false);
        }
        
        if (slot.infoText != null && slot.infoText.gameObject != null)
        {
            slot.infoText.text = "Empty";
            slot.infoText.gameObject.SetActive(true);
        }
        
        if (slot.bonusGemsText != null && slot.bonusGemsText.gameObject != null)
        {
            slot.bonusGemsText.gameObject.SetActive(false);
        }
        
        // Keep slot visible but show it's empty
        if (slot.slotObject != null)
        {
            slot.slotObject.SetActive(true);
        }
    }

    private string GetDisplayTextForPowerup(PowerupData powerup)
    {
        // DEBUG: Log what we're displaying
        if (verboseDebugging)
        {
            Debug.Log($"[GetDisplayText] Type: {powerup.powerupType}, Active: {powerup.isActive}, Duration: {powerup.duration}, Charges: {powerup.charges}");
        }
        
        // If active, prioritize showing countdown/charges over names
        if (powerup.isActive)
        {
            switch (powerup.powerupType)
            {
                case PowerUpType.AOEAttack:
                case PowerUpType.HomingDaggers:
                    return powerup.charges.ToString();
                
                case PowerUpType.DoubleGems:
                case PowerUpType.SlowTime:
                case PowerUpType.GodMode:
                case PowerUpType.MaxHandUpgrade:
                case PowerUpType.InstantCooldown:
                case PowerUpType.DoubleDamage:
                    if (powerup.duration > 0)
                    {
                        return Mathf.Ceil(powerup.duration).ToString() + "s";
                    }
                    break;
            }
        }
        
        // Special handling for MaxHandUpgrade when not active
        if (powerup.powerupType == PowerUpType.MaxHandUpgrade && !powerup.isActive)
        {
            // Show current hand levels when collected but not activated
            return GetCurrentHandLevels();
        }
        
        // Default: show powerup name when not active
        return GetPowerupName(powerup.powerupType);
    }
    
    private string GetCurrentHandLevels()
    {
        if (PlayerProgression.Instance != null)
        {
            int primaryLevel = PlayerProgression.Instance.primaryHandLevel;
            int secondaryLevel = PlayerProgression.Instance.secondaryHandLevel;
            return $"L{primaryLevel} - R{secondaryLevel}";
        }
        return "L? - R?";
    }
    
    private string GetPowerupName(PowerUpType powerupType)
    {
        switch (powerupType)
        {
            case PowerUpType.AOEAttack:
                return "AOE";
            case PowerUpType.DoubleGems:
                return "Double Gems";
            case PowerUpType.SlowTime:
                return "Slow Time";
            case PowerUpType.GodMode:
                return "God Mode";
            case PowerUpType.MaxHandUpgrade:
                return "Max Hand";
            case PowerUpType.HomingDaggers:
                return "Homing";
            case PowerUpType.InstantCooldown:
                return "Cool Down";
            case PowerUpType.DoubleDamage:
                return "2x Damage";
            default:
                return powerupType.ToString();
        }
    }

    private Sprite GetIconForPowerupType(PowerUpType powerupType)
    {
        if (iconManager != null)
        {
            return iconManager.GetIconForPowerupType(powerupType);
        }
        
        return PowerupIconManager.GetIcon(powerupType, null);
    }

    private void ActivateSelectedPowerup()
    {
        // ROBUST: Comprehensive bounds and null checking
        if (activePowerups == null || currentSelectedSlot < 0 || currentSelectedSlot >= activePowerups.Count)
        {
            Debug.LogWarning($"[PowerupInventoryManager] Invalid activation attempt - Slot: {currentSelectedSlot}, Count: {activePowerups?.Count ?? 0}", this);
            return;
        }
        
        PowerupData selectedPowerup = activePowerups[currentSelectedSlot];
        
        if (verboseDebugging)
        {
            Debug.Log($"[PowerupInventoryManager] Activating {selectedPowerup.powerupType}", this);
        }
        
        // Activate the powerup based on its type
        bool activated = false;
        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>(); // Declare once for all cases
        
        switch (selectedPowerup.powerupType)
        {
            case PowerUpType.AOEAttack:
                if (PlayerAOEAbility.Instance != null)
                {
                    // SINGLE SOURCE OF TRUTH: PlayerAOEAbility handles everything
                    // It will check charges, decrement, and fire OnChargesChanged event
                    PlayerAOEAbility.Instance.InitiateAOE();
                    
                    // OnChargesChanged event will automatically sync inventory
                    Debug.Log("[PowerupInventoryManager] AOE activation delegated to PlayerAOEAbility", this);
                    activated = true;
                }
                break;
                
            case PowerUpType.HomingDaggers:
                // FIXED: Use stored activationDuration instead of FindObjectOfType
                PlayerShooterOrchestrator pso = PlayerShooterOrchestrator.Instance;
                if (pso != null)
                {
                    // Use the activationDuration stored in PowerupData (set when powerup was collected)
                    float activationDuration = selectedPowerup.activationDuration > 0 ? selectedPowerup.activationDuration : 15f;
                    
                    pso.ActivateHomingDaggersPowerUp(activationDuration);
                    activated = true;
                    
                    Debug.Log($"[PowerupInventoryManager] Activated homing daggers for {activationDuration} seconds (stored duration)", this);
                }
                break;
                
            case PowerUpType.DoubleGems:
                if (PlayerProgression.Instance != null)
                {
                    PlayerProgression.Instance.ActivateDoubleGems(selectedPowerup.duration);
                    activated = true;
                }
                break;
                
            case PowerUpType.SlowTime:
                if (PlayerProgression.Instance != null)
                {
                    // ActivateSlowTime requires: duration, scaleFactor, startSound, endSound, soundVolume
                    PlayerProgression.Instance.ActivateSlowTime(selectedPowerup.duration, 0.3f, null, null, 0.7f);
                    activated = true;
                }
                break;
                
            case PowerUpType.GodMode:
                if (PlayerProgression.Instance != null)
                {
                    PlayerProgression.Instance.ActivateGodMode(selectedPowerup.duration);
                    activated = true;
                }
                break;
                
            case PowerUpType.MaxHandUpgrade:
                // SIMPLE: Delegate to MaxHandUpgradePowerUp script - it handles everything
                MaxHandUpgradePowerUp maxHandScript = FindObjectOfType<MaxHandUpgradePowerUp>();
                if (maxHandScript != null && PlayerProgression.Instance != null)
                {
                    Debug.Log($"[PowerupInventoryManager] Delegating MaxHandUpgrade activation to MaxHandUpgradePowerUp script", this);
                    maxHandScript.ActivateMaxHandUpgrade(
                        PlayerProgression.Instance, 
                        PlayerShooterOrchestrator.Instance, 
                        playerHealth
                    );
                    activated = true;
                }
                else
                {
                    Debug.LogError("[PowerupInventoryManager] MaxHandUpgradePowerUp script or PlayerProgression not found!", this);
                }
                break;
                
            case PowerUpType.InstantCooldown:
                if (playerHealth != null)
                {
                    // Activate the instant cooldown effect - it handles its own effects
                    playerHealth.ActivateInstantCooldown(selectedPowerup.duration);
                    activated = true;
                }
                break;
                
            case PowerUpType.DoubleDamage:
                if (playerHealth != null)
                {
                    // Activate the double damage effect - it handles its own effects
                    playerHealth.ActivateDoubleDamage(selectedPowerup.duration);
                    activated = true;
                }
                break;
        }
        
        if (activated)
        {
            // Play powerup activation sound
            try
            {
                GameSounds.PlayPowerUpStart(transform.position);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PowerupInventoryManager] Failed to play activation sound for {selectedPowerup.powerupType}. Error: {ex.Message}", this);
            }
            
            // CRITICAL: Only mark duration-based powerups as active
            // Charge-based powerups (AOE, HomingDaggers) should NOT be marked active
            bool isChargeBased = (selectedPowerup.powerupType == PowerUpType.AOEAttack || 
                                  selectedPowerup.powerupType == PowerUpType.HomingDaggers);
            
            if (!isChargeBased)
            {
                selectedPowerup.isActive = true;
            }
            
            // Brief activation visual feedback - just for this slot
            StartCoroutine(BriefActivationEffect(currentSelectedSlot));
            
            // Handle charge-based powerups
            if (selectedPowerup.powerupType == PowerUpType.HomingDaggers)
            {
                // HomingDaggers managed locally - decrement charges
                selectedPowerup.charges--;
                
                if (verboseDebugging)
                {
                    Debug.Log($"[PowerupInventoryManager] HomingDaggers charges decremented to {selectedPowerup.charges}", this);
                }
                
                if (selectedPowerup.charges <= 0)
                {
                    if (verboseDebugging)
                    {
                        Debug.Log($"[PowerupInventoryManager] HomingDaggers depleted - removing from slot {currentSelectedSlot}", this);
                    }
                    RemovePowerup(currentSelectedSlot);
                    return; // Exit early - RemovePowerup handles all visual updates
                }
            }
            // AOE charges managed by PlayerAOEAbility via OnChargesChanged event - no local handling needed
            
            // CRITICAL FIX: Only update THIS slot's display, never call UpdateSlotVisuals() here
            // UpdateSlotVisuals() updates ALL slots which causes unwanted visual changes
            UpdateSlotDisplay(currentSelectedSlot);
        }
    }

    private void RemovePowerup(int slotIndex)
    {
        // ROBUST: Comprehensive bounds checking
        if (activePowerups == null || slotIndex < 0 || slotIndex >= activePowerups.Count)
        {
            Debug.LogWarning($"[PowerupInventoryManager] Invalid RemovePowerup attempt - Index: {slotIndex}, Count: {activePowerups?.Count ?? 0}", this);
            return;
        }
        
        // FIXED: Deactivate this specific powerup's effect instance
        PowerupData expiredPowerup = activePowerups[slotIndex];
        if (expiredPowerup.isActive)
        {
            DeactivatePowerup(slotIndex);
        }
        
        activePowerups.RemoveAt(slotIndex);
        
        // FIXED: Update ALL slots from the removed index onwards (list shifted left)
        // When we remove slot 2, slot 3 becomes slot 2, slot 4 becomes slot 3, etc.
        for (int i = slotIndex; i < inventorySlots.Length; i++)
        {
            if (i < activePowerups.Count)
            {
                // Update this slot with the powerup that shifted into it
                UpdateSlotDisplay(i);
            }
            else
            {
                // Empty all slots beyond the active powerups
                SetSlotEmpty(i);
            }
        }
        
        // ROBUST: Adjust selection with comprehensive bounds checking
        if (activePowerups.Count == 0)
        {
            currentSelectedSlot = 0;
        }
        else if (currentSelectedSlot >= activePowerups.Count)
        {
            currentSelectedSlot = activePowerups.Count - 1;
        }
        else if (currentSelectedSlot < 0)
        {
            currentSelectedSlot = 0;
        }
        
        // NOTE: Don't call UpdateSlotVisuals() here - caller will handle it to avoid duplicate updates
    }

    private void UpdateActivePowerupTimers()
    {
        bool needsUpdate = false;
        
        for (int i = activePowerups.Count - 1; i >= 0; i--)
        {
            PowerupData powerup = activePowerups[i];
            
            // Update duration-based powerups
            if (powerup.isActive && powerup.duration > 0)
            {
                // STANDARDIZED: Use unscaled time for all powerup timers to prevent SlowTime interference
                powerup.duration -= Time.unscaledDeltaTime;
                
                if (powerup.duration <= 0)
                {
                    // Duration expired - deactivate and remove
                    DeactivatePowerup(i);
                    RemovePowerup(i);
                    needsUpdate = true;
                }
                else
                {
                    // INDIVIDUAL: Only update this specific powerup's timer display
                    UpdateSlotDisplay(i);
                }
            }
        }
        
        if (needsUpdate)
        {
            UpdateSlotVisuals();
        }
    }

    private void OnPowerUpStatusChanged(PowerUpType powerupType, bool isActive, float timeLeft)
    {
        // CRITICAL: Ignore AOE events - we handle AOE through OnAOEChargesChanged instead
        // AOE fires OnPowerUpStatusChangedForHUD for the HUD display, but inventory uses charges
        if (powerupType == PowerUpType.AOEAttack)
        {
            return; // AOE managed separately via OnAOEChargesChanged event
        }
        
        int existingIndex = FindExistingPowerup(powerupType);
        
        if (isActive)
        {
            if (existingIndex >= 0)
            {
                // Update existing powerup
                activePowerups[existingIndex].duration = timeLeft;
                activePowerups[existingIndex].isActive = true;
                UpdateSlotDisplay(existingIndex);
            }
            else
            {
                // Add new powerup
                AddPowerup(powerupType, timeLeft);
            }
        }
        else
        {
            // Remove powerup
            if (existingIndex >= 0)
            {
                RemovePowerup(existingIndex);
            }
        }
    }

    /// <summary>
    /// Grant all powerups for testing purposes
    /// FIXED: No longer clears existing powerups - adds to current inventory
    /// </summary>
    public void GrantAllPowerupsForTesting()
    {
        if (verboseDebugging)
        {
            Debug.Log("[PowerupInventoryManager] Granting all powerups for testing (adding to existing inventory)", this);
        }
        
        // FIXED: Don't clear existing powerups - just add new ones if space available
        
        // Add all powerup types with explicit parameters
        Debug.Log("[PowerupInventoryManager] Adding AOE Attack...", this);
        AddPowerup(PowerUpType.AOEAttack, 0f, 3); // This will handle PlayerAOEAbility sync internally
        
        Debug.Log("[PowerupInventoryManager] Adding DoubleGems...", this);
        AddPowerup(PowerUpType.DoubleGems, 15f, 0);
        
        Debug.Log("[PowerupInventoryManager] Adding SlowTime...", this);
        AddPowerup(PowerUpType.SlowTime, 10f, 0);
        
        Debug.Log("[PowerupInventoryManager] Adding GodMode...", this);
        AddPowerup(PowerUpType.GodMode, 8f, 0);
        
        Debug.Log("[PowerupInventoryManager] Adding MaxHandUpgrade...", this);
        AddPowerup(PowerUpType.MaxHandUpgrade, 20f, 0);
        
        Debug.Log("[PowerupInventoryManager] Adding HomingDaggers...", this);
        AddPowerup(PowerUpType.HomingDaggers, 0f, 5); // 5 charges, no duration (charge-based powerup)
        
        Debug.Log("[PowerupInventoryManager] Adding InstantCooldown...", this);
        AddPowerup(PowerUpType.InstantCooldown, 8f, 0);
        
        Debug.Log("[PowerupInventoryManager] Adding DoubleDamage...", this);
        AddPowerup(PowerUpType.DoubleDamage, 15f, 0);
        
        UpdateSlotVisuals();
        
        if (verboseDebugging)
        {
            Debug.Log($"[PowerupInventoryManager] Granted {activePowerups.Count} powerups for testing", this);
            for (int i = 0; i < activePowerups.Count; i++)
            {
                Debug.Log($"[PowerupInventoryManager] Slot {i}: {activePowerups[i].powerupType} - Duration: {activePowerups[i].duration}, Charges: {activePowerups[i].charges}", this);
            }
        }
    }
    
    /// <summary>
    /// Update bonus gems display for double gems powerup
    /// </summary>
    public void UpdateBonusGemsDisplay(int bonusGems, float remainingTime, bool shouldFlicker = false)
    {
        // Find the slot with DoubleGems powerup
        int doubleGemsSlotIndex = -1;
        for (int i = 0; i < activePowerups.Count && i < inventorySlots.Length; i++)
        {
            if (activePowerups[i].powerupType == PowerUpType.DoubleGems)
            {
                doubleGemsSlotIndex = i;
                break;
            }
        }
        
        if (doubleGemsSlotIndex >= 0 && doubleGemsSlotIndex < inventorySlots.Length)
        {
            var slot = inventorySlots[doubleGemsSlotIndex];
            if (slot.bonusGemsText != null)
            {
                if (bonusGems > 0)
                {
                    slot.bonusGemsText.text = $"+{bonusGems}";
                    slot.bonusGemsText.gameObject.SetActive(true);
                    
                    // Handle flickering based on remaining time
                    if (shouldFlicker)
                    {
                        float flickerSpeed = remainingTime <= 2f ? 0.15f : 0.5f;
                        StartCoroutine(FlickerBonusGemsText(slot.bonusGemsText, flickerSpeed));
                    }
                    else
                    {
                        slot.bonusGemsText.color = Color.yellow;
                    }
                }
                else
                {
                    slot.bonusGemsText.gameObject.SetActive(false);
                }
            }
        }
    }
    
    /// <summary>
    /// Flicker bonus gems text - only affects the double gems slot
    /// </summary>
    private System.Collections.IEnumerator FlickerBonusGemsText(TextMeshProUGUI text, float flickerSpeed)
    {
        bool isYellow = true;
        float flickerTime = 0f;
        
        // Find the double gems slot index to only flicker that slot
        int doubleGemsSlotIndex = -1;
        for (int i = 0; i < activePowerups.Count && i < inventorySlots.Length; i++)
        {
            if (activePowerups[i].powerupType == PowerUpType.DoubleGems)
            {
                doubleGemsSlotIndex = i;
                break;
            }
        }
        
        while (text != null && text.gameObject.activeInHierarchy && flickerTime < 10f) // Max 10 seconds of flickering
        {
            text.color = isYellow ? Color.yellow : Color.white;
            
            // REMOVED: Scale flickering - no more moving boxes
            
            isYellow = !isYellow;
            flickerTime += flickerSpeed;
            yield return new WaitForSeconds(flickerSpeed);
        }
        
        // Reset to normal state when done
        if (text != null)
        {
            text.color = Color.yellow;
        }
        
        // REMOVED: Scale reset - no more moving boxes
    }
    
    /// <summary>
    /// Brief activation effect - makes the activated slot slightly bigger for a moment
    /// </summary>
    private IEnumerator BriefActivationEffect(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= inventorySlots.Length || inventorySlots[slotIndex].slotObject == null)
            yield break;
            
        GameObject slotObject = inventorySlots[slotIndex].slotObject;
        Vector3 originalScale = slotObject.transform.localScale;
        Vector3 activationScale = originalScale * 1.2f; // 20% bigger
        
        // Scale up quickly
        float duration = 0.1f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            slotObject.transform.localScale = Vector3.Lerp(originalScale, activationScale, t);
            yield return null;
        }
        
        // Hold briefly
        yield return new WaitForSecondsRealtime(0.05f);
        
        // Scale back down
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            slotObject.transform.localScale = Vector3.Lerp(activationScale, originalScale, t);
            yield return null;
        }
        
        // Ensure it's back to original scale
        slotObject.transform.localScale = originalScale;
    }

    // Public methods for external access
    public int GetCurrentSelectedSlot() => currentSelectedSlot;
    public int GetActivePowerupCount() => activePowerups.Count;
    public PowerupData GetSelectedPowerup() => currentSelectedSlot < activePowerups.Count ? activePowerups[currentSelectedSlot] : null;
    
    // Public method to find existing powerup (needed by individual powerup scripts)
    public int FindExistingPowerupPublic(PowerUpType powerupType)
    {
        for (int i = 0; i < activePowerups.Count; i++)
        {
            if (activePowerups[i].powerupType == powerupType)
            {
                return i;
            }
        }
        return -1;
    }
    
    // Public method to remove powerup (needed by individual powerup scripts)
    public void RemovePowerupPublic(int slotIndex)
    {
        RemovePowerup(slotIndex);
    }
    
    /// <summary>
    /// Test method to verify AOE charge synchronization works correctly
    /// </summary>
    [ContextMenu("Test AOE Charge Sync")]
    public void TEST_AOEChargeSync()
    {
        if (PlayerAOEAbility.Instance == null)
        {
            Debug.LogError("[TEST] PlayerAOEAbility.Instance is null - cannot test sync");
            return;
        }
        
        Debug.Log("=== AOE CHARGE SYNC TEST START ===");
        
        // Clear existing powerups
        activePowerups.Clear();
        UpdateSlotVisuals();
        
        // Test 1: Add AOE powerup with 3 charges
        Debug.Log("[TEST] Adding AOE powerup with 3 charges...");
        AddPowerup(PowerUpType.AOEAttack, 0f, 3);
        
        int inventoryCharges = FindExistingPowerup(PowerUpType.AOEAttack) >= 0 ? 
            activePowerups[FindExistingPowerup(PowerUpType.AOEAttack)].charges : 0;
        int abilityCharges = PlayerAOEAbility.Instance.GetCurrentCharges();
        
        Debug.Log($"[TEST] After adding: Inventory={inventoryCharges}, PlayerAOEAbility={abilityCharges}");
        
        if (inventoryCharges == abilityCharges && inventoryCharges == 3)
        {
            Debug.Log("[TEST] ✅ Add sync test PASSED");
        }
        else
        {
            Debug.LogError("[TEST] ❌ Add sync test FAILED");
        }
        
        // Test 2: Activate AOE (should decrement charges)
        Debug.Log("[TEST] Activating AOE powerup...");
        if (FindExistingPowerup(PowerUpType.AOEAttack) >= 0)
        {
            currentSelectedSlot = FindExistingPowerup(PowerUpType.AOEAttack);
            ActivateSelectedPowerup();
            
            // Wait for sync coroutine to complete
            StartCoroutine(TestAOESyncAfterActivation());
        }
        else
        {
            Debug.LogError("[TEST] ❌ AOE powerup not found in inventory for activation test");
        }
    }
    
    /// <summary>
    /// Test method to verify GodMode and MaxHandUpgrade powerups work correctly
    /// </summary>
    [ContextMenu("Test GodMode and MaxHand Powerups")]
    public void TEST_GodModeAndMaxHand()
    {
        Debug.Log("=== GODMODE & MAXHAND TEST START ===");
        
        // Clear existing powerups
        activePowerups.Clear();
        UpdateSlotVisuals();
        
        // Test 1: Add GodMode powerup
        Debug.Log("[TEST] Adding GodMode powerup with 10 seconds duration...");
        AddPowerup(PowerUpType.GodMode, 10f);
        
        // Test 2: Add MaxHandUpgrade powerup
        Debug.Log("[TEST] Adding MaxHandUpgrade powerup with 15 seconds duration...");
        AddPowerup(PowerUpType.MaxHandUpgrade, 15f);
        
        Debug.Log($"[TEST] Added powerups. Total in inventory: {activePowerups.Count}");
        
        // Test 3: Try to activate GodMode
        Debug.Log("[TEST] Attempting to activate GodMode...");
        int godModeIndex = FindExistingPowerup(PowerUpType.GodMode);
        if (godModeIndex >= 0)
        {
            currentSelectedSlot = godModeIndex;
            ActivateSelectedPowerup();
            
            PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
            if (playerHealth != null && playerHealth.IsGodModeActive)
            {
                Debug.Log("[TEST] ✅ GodMode activation test PASSED");
            }
            else
            {
                Debug.LogError("[TEST] ❌ GodMode activation test FAILED - PlayerHealth not found or GodMode not active");
            }
        }
        else
        {
            Debug.LogError("[TEST] ❌ GodMode powerup not found in inventory");
        }
        
        // Test 4: Try to activate MaxHandUpgrade
        Debug.Log("[TEST] Attempting to activate MaxHandUpgrade...");
        int maxHandIndex = FindExistingPowerup(PowerUpType.MaxHandUpgrade);
        if (maxHandIndex >= 0)
        {
            currentSelectedSlot = maxHandIndex;
            ActivateSelectedPowerup();
            Debug.Log("[TEST] ✅ MaxHandUpgrade activation attempt completed (check hand levels)");
        }
        else
        {
            Debug.LogError("[TEST] ❌ MaxHandUpgrade powerup not found in inventory");
        }
        
        Debug.Log("=== GODMODE & MAXHAND TEST END ===");
    }
    
    /// <summary>
    /// Test method to verify powerup collection and stacking behavior
    /// </summary>
    [ContextMenu("Test Powerup Stacking and Particle Effects")]
    public void TEST_PowerupStackingAndParticleEffects()
    {
        Debug.Log("=== TESTING POWERUP STACKING AND PARTICLE EFFECTS ===", this);
        
        // Test 1: Add multiple different powerups (should stack independently)
        Debug.Log("[TEST] Adding multiple different powerups...");
        AddPowerup(PowerUpType.GodMode, 10f);
        AddPowerup(PowerUpType.DoubleGems, 15f);
        AddPowerup(PowerUpType.SlowTime, 12f);
        
        Debug.Log($"[TEST] Added 3 different powerups. Total in inventory: {activePowerups.Count}");
        
        // Test 2: Add same powerup types (should extend duration)
        Debug.Log("[TEST] Adding same powerup types to test stacking...");
        AddPowerup(PowerUpType.GodMode, 5f); // Should extend to 15f total
        AddPowerup(PowerUpType.DoubleGems, 10f); // Should extend to 25f total
        
        Debug.Log($"[TEST] After stacking, inventory count: {activePowerups.Count}");
        
        // Test 3: Activate all powerups
        Debug.Log("[TEST] Activating all powerups...");
        for (int i = 0; i < activePowerups.Count; i++)
        {
            currentSelectedSlot = i;
            ActivateSelectedPowerup();
        }
        
        Debug.Log("[TEST] All powerups activated. Check particle effects are running simultaneously.");
        Debug.Log("=== POWERUP STACKING TEST COMPLETE ===", this);
    }

    [ContextMenu("Test Powerup Collection and Stacking")]
    public void TEST_PowerupCollectionAndStacking()
    {
        Debug.Log("=== POWERUP COLLECTION & STACKING TEST START ===");
        
        // FIXED: Don't clear existing powerups - test with current inventory state
        Debug.Log($"[TEST] Starting with {activePowerups.Count} existing powerups in inventory");
        
        // Test 1: Collect multiple GodMode powerups (should stack duration)
        Debug.Log("[TEST] Collecting 3 GodMode powerups...");
        AddPowerup(PowerUpType.GodMode, 10f);  // First collection
        AddPowerup(PowerUpType.GodMode, 5f);   // Second collection (should stack)
        AddPowerup(PowerUpType.GodMode, 8f);   // Third collection (should stack)
        
        int godModeIndex = FindExistingPowerup(PowerUpType.GodMode);
        if (godModeIndex >= 0)
        {
            float totalDuration = activePowerups[godModeIndex].duration;
            Debug.Log($"[TEST] GodMode total duration after stacking: {totalDuration}s (expected: 23s)");
            if (Mathf.Approximately(totalDuration, 23f))
            {
                Debug.Log("[TEST] ✅ GodMode duration stacking test PASSED");
            }
            else
            {
                Debug.LogError("[TEST] ❌ GodMode duration stacking test FAILED");
            }
        }
        
        // Test 2: Collect multiple HomingDaggers (should stack charges)
        Debug.Log("[TEST] Collecting 2 HomingDagger powerups...");
        AddPowerup(PowerUpType.HomingDaggers, 0f, 5);  // First collection (5 charges)
        AddPowerup(PowerUpType.HomingDaggers, 0f, 3);  // Second collection (3 more charges)
        
        int homingIndex = FindExistingPowerup(PowerUpType.HomingDaggers);
        if (homingIndex >= 0)
        {
            int totalCharges = activePowerups[homingIndex].charges;
            Debug.Log($"[TEST] HomingDaggers total charges after stacking: {totalCharges} (expected: 8)");
            if (totalCharges == 8)
            {
                Debug.Log("[TEST] ✅ HomingDaggers charge stacking test PASSED");
            }
            else
            {
                Debug.LogError("[TEST] ❌ HomingDaggers charge stacking test FAILED");
            }
        }
        
        // Test 3: Collect different powerup types (should occupy different slots)
        Debug.Log("[TEST] Collecting different powerup types...");
        AddPowerup(PowerUpType.DoubleGems, 20f);
        AddPowerup(PowerUpType.SlowTime, 8f);
        AddPowerup(PowerUpType.MaxHandUpgrade, 15f);
        AddPowerup(PowerUpType.InstantCooldown, 10f);
        
        Debug.Log($"[TEST] Total unique powerup types in inventory: {activePowerups.Count}");
        
        // Test 4: Verify no auto-activation occurred
        bool anyAutoActivated = false;
        for (int i = 0; i < activePowerups.Count; i++)
        {
            if (activePowerups[i].isActive)
            {
                anyAutoActivated = true;
                Debug.LogError($"[TEST] ❌ Powerup {activePowerups[i].powerupType} was auto-activated!");
            }
        }
        
        if (!anyAutoActivated)
        {
            Debug.Log("[TEST] ✅ No auto-activation test PASSED - all powerups waiting for manual activation");
        }
        
        // Test 5: Check inventory limit behavior
        Debug.Log("[TEST] Testing inventory limit...");
        int slotsUsed = activePowerups.Count;
        int maxSlots = inventorySlots.Length;
        Debug.Log($"[TEST] Current slots used: {slotsUsed}/{maxSlots} (8 slots available)");
        
        if (slotsUsed <= maxSlots)
        {
            Debug.Log("[TEST] ✅ Inventory within limits - can hold up to 8 different powerup types");
        }
        else
        {
            Debug.LogError("[TEST] ❌ Inventory exceeded limits!");
        }
        
        // Test 6: Display final inventory state
        Debug.Log("[TEST] Final inventory state:");
        for (int i = 0; i < activePowerups.Count; i++)
        {
            PowerupData powerup = activePowerups[i];
            Debug.Log($"[TEST] Slot {i}: {powerup.powerupType} - Duration: {powerup.duration}s, Charges: {powerup.charges}, Active: {powerup.isActive}");
        }
        
        Debug.Log("=== POWERUP COLLECTION & STACKING TEST END ===");
    }
    
    private IEnumerator TestAOESyncAfterActivation()
    {
        // Wait for sync coroutine to complete
        yield return new WaitForSeconds(0.1f);
        
        int inventoryCharges = FindExistingPowerup(PowerUpType.AOEAttack) >= 0 ? 
            activePowerups[FindExistingPowerup(PowerUpType.AOEAttack)].charges : 0;
        int abilityCharges = PlayerAOEAbility.Instance.GetCurrentCharges();
        
        Debug.Log($"[TEST] After activation: Inventory={inventoryCharges}, PlayerAOEAbility={abilityCharges}");
        
        if (inventoryCharges == abilityCharges && inventoryCharges == 2)
        {
            Debug.Log("[TEST] ✅ Activation sync test PASSED");
        }
        else
        {
            Debug.LogError("[TEST] ❌ Activation sync test FAILED");
        }
        
        Debug.Log("=== AOE CHARGE SYNC TEST END ===");
    }
    
    // REMOVED: MaxHandUpgradeReversionCoroutine - now handled by MaxHandUpgradePowerUp.cs directly
}
