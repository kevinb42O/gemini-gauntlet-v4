using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PowerupDisplay : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI chargesText;
    [SerializeField] private TextMeshProUGUI bonusGemsText; // For double gems bonus display
    
    [Header("Display Settings")]
    [SerializeField] private Sprite emptySlotSprite;
    [SerializeField] private Color emptySlotColor = Color.gray;
    [SerializeField] private Color activeSlotColor = Color.white;
    
    [Header("Animation Settings")]
    [SerializeField] private float collectionScaleMultiplier = 1.5f;
    [SerializeField] private float collectionAnimationDuration = 0.5f;
    [SerializeField] private AnimationCurve collectionScaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Icon Manager Reference")]
    [SerializeField] private PowerupIconManager iconManager;
    
    // Current powerup state
    private PowerUpType? currentPowerupType = null;
    private int currentCharges = 0;
    private float currentDuration = 0f;
    private bool hasActivePowerup = false;
    
    // Animation coroutine reference
    private Coroutine collectionAnimationCoroutine;
    
    // Original scale for animation
    private Vector3 originalScale;
    
    // Double gems bonus tracking
    private Coroutine bonusGemsFlickerCoroutine;

    private void Awake()
    {
        // Store original scale
        originalScale = transform.localScale;
        
        // Initialize with empty slot
        ShowEmptySlot();
    }

    private void OnEnable()
    {
        // Subscribe to powerup events from multiple sources
        SubscribeToEvents();
    }

    private void OnDisable()
    {
        // Unsubscribe from powerup events
        UnsubscribeFromEvents();
    }

    private void Start()
    {
        // Try to subscribe to events if components weren't available in OnEnable
        SubscribeToEvents();
    }

    private void SubscribeToEvents()
    {
        // Subscribe to duration-based powerup events
        if (PlayerProgression.Instance != null)
        {
            PlayerProgression.OnPowerUpStatusChangedForHUD -= OnPowerUpStatusChanged; // Prevent double subscription
            PlayerProgression.OnPowerUpStatusChangedForHUD += OnPowerUpStatusChanged;
        }
        
        // Subscribe to GodMode events from PlayerHealth
        PlayerHealth.OnPowerUpStatusChangedForHUD -= OnPowerUpStatusChanged;
        PlayerHealth.OnPowerUpStatusChangedForHUD += OnPowerUpStatusChanged;
        
        // Subscribe to AOE events from PlayerAOEAbility
        PlayerAOEAbility.OnPowerUpStatusChangedForHUD -= OnPowerUpStatusChanged;
        PlayerAOEAbility.OnPowerUpStatusChangedForHUD += OnPowerUpStatusChanged;
    }

    private void UnsubscribeFromEvents()
    {
        if (PlayerProgression.Instance != null)
        {
            PlayerProgression.OnPowerUpStatusChangedForHUD -= OnPowerUpStatusChanged;
        }
        
        PlayerHealth.OnPowerUpStatusChangedForHUD -= OnPowerUpStatusChanged;
        PlayerAOEAbility.OnPowerUpStatusChangedForHUD -= OnPowerUpStatusChanged;
    }

    /// <summary>
    /// Called when a powerup status changes (activated, deactivated, or duration updated)
    /// </summary>
    private void OnPowerUpStatusChanged(PowerUpType powerupType, bool isActive, float timeLeft)
    {
        // PERFORMANCE: Removed excessive frame-by-frame debug logging
        
        if (isActive)
        {
            // New powerup activated or existing one updated
            if (currentPowerupType != powerupType)
            {
                // New powerup collected - play collection animation
                DisplayPowerup(powerupType, timeLeft);
                PlayCollectionAnimation();
            }
            else
            {
                // Same powerup, just update duration
                UpdatePowerupDisplay(powerupType, timeLeft);
            }
        }
        else
        {
            // Powerup deactivated
            if (currentPowerupType == powerupType)
            {
                ShowEmptySlot();
            }
        }
    }

    /// <summary>
    /// Manually trigger powerup display - call this when a powerup is collected
    /// This is for powerups that don't automatically fire events
    /// </summary>
    public void OnPowerupCollected(PowerUpType powerupType, float duration = 0f)
    {
        Debug.Log($"[PowerupDisplay] OnPowerupCollected called: {powerupType}, Duration: {duration}");
        
        // For non-duration powerups, show them for a default time or indefinitely
        if (duration <= 0f)
        {
            switch (powerupType)
            {
                case PowerUpType.MaxHandUpgrade:
                    duration = 15f; // Default duration for MaxHandUpgrade
                    break;
                case PowerUpType.HomingDaggers:
                case PowerUpType.AOEAttack:
                    duration = 999f; // Show indefinitely for charge-based powerups
                    break;
                default:
                    duration = 10f; // Default fallback
                    break;
            }
        }
        
        DisplayPowerup(powerupType, duration);
        PlayCollectionAnimation();
    }

    /// <summary>
    /// Display a new powerup with its icon and charges/duration
    /// </summary>
    public void DisplayPowerup(PowerUpType powerupType, float duration)
    {
        // Only log when actually displaying a NEW powerup (not updates)
        if (currentPowerupType != powerupType)
        {
            Debug.Log($"[PowerupDisplay] DisplayPowerup called: {powerupType}, Duration: {duration}");
        }
        
        currentPowerupType = powerupType;
        currentDuration = duration;
        hasActivePowerup = true;
        
        // Set background to active state
        if (backgroundImage != null)
        {
            backgroundImage.color = activeSlotColor;
        }
        else
        {
            Debug.LogError("[PowerupDisplay] Background image is null!");
        }
        
        // Set the appropriate icon
        Sprite iconSprite = GetIconForPowerupType(powerupType);
        if (iconImage != null && iconSprite != null)
        {
            iconImage.sprite = iconSprite;
            iconImage.color = Color.white;
            iconImage.gameObject.SetActive(true);
        }
        else if (currentPowerupType != powerupType)
        {
            Debug.LogError($"[PowerupDisplay] Icon image is null or sprite not found for {powerupType}");
        }
        
        // Update charges/duration text
        UpdateChargesText(powerupType, duration);
    }

    /// <summary>
    /// Update the display for an existing powerup (usually duration changes)
    /// </summary>
    public void UpdatePowerupDisplay(PowerUpType powerupType, float duration)
    {
        if (currentPowerupType != powerupType) return;
        
        currentDuration = duration;
        UpdateChargesText(powerupType, duration);
    }

    /// <summary>
    /// Show empty slot state
    /// </summary>
    public void ShowEmptySlot()
    {
        hasActivePowerup = false;
        currentPowerupType = null;
        currentCharges = 0;
        currentDuration = 0f;
        
        if (backgroundImage != null)
        {
            backgroundImage.color = emptySlotColor;
        }
        
        if (iconImage != null)
        {
            iconImage.sprite = emptySlotSprite;
            iconImage.color = emptySlotColor;
        }
        
        if (chargesText != null)
        {
            chargesText.gameObject.SetActive(false);
        }
        
        if (bonusGemsText != null)
        {
            bonusGemsText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Update the charges/duration text based on powerup type
    /// </summary>
    private void UpdateChargesText(PowerUpType powerupType, float duration)
    {
        if (chargesText == null) 
        {
            Debug.LogError("[PowerupDisplay] Charges text is null!");
            return;
        }
        
        string displayText = "";
        
        switch (powerupType)
        {
            case PowerUpType.MaxHandUpgrade:
                // Show duration for MaxHandUpgrade
                if (duration > 0 && duration < 900) // Don't show huge numbers
                {
                    displayText = Mathf.Ceil(duration).ToString() + "s";
                }
                else
                {
                    displayText = "∞";
                }
                break;
                
            case PowerUpType.HomingDaggers:
                // Charge-based usage
                if (duration > 0 && duration < 900) // Don't show huge numbers
                {
                    displayText = Mathf.Ceil(duration).ToString();
                }
                else
                {
                    displayText = "∞";
                }
                break;
                
            case PowerUpType.AOEAttack:
                // Show countdown with "s" when active (duration <= 5), charges without "s" when ready (duration > 5)
                if (duration > 0 && duration <= 5) // Active countdown (AOE duration is 5 seconds)
                {
                    displayText = Mathf.Ceil(duration).ToString() + "s";
                }
                else if (duration > 5) // Ready with charges (charge count)
                {
                    displayText = Mathf.Ceil(duration).ToString();
                }
                else
                {
                    displayText = "∞";
                }
                break;
                
            case PowerUpType.DoubleGems:
            case PowerUpType.SlowTime:
            case PowerUpType.GodMode:
                // These are duration-based
                if (duration > 0)
                {
                    displayText = Mathf.Ceil(duration).ToString() + "s";
                }
                break;
        }
        
        if (!string.IsNullOrEmpty(displayText))
        {
            chargesText.text = displayText;
            chargesText.gameObject.SetActive(true);
        }
        else
        {
            chargesText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Update bonus gems display for double gems powerup
    /// </summary>
    public void UpdateBonusGemsDisplay(int bonusGems, float remainingTime, bool shouldFlicker = false)
    {
        if (bonusGemsText == null) return;
        
        if (currentPowerupType == PowerUpType.DoubleGems && bonusGems > 0)
        {
            bonusGemsText.text = $"+{bonusGems}";
            bonusGemsText.gameObject.SetActive(true);
            
            // Handle flickering
            if (shouldFlicker)
            {
                if (bonusGemsFlickerCoroutine != null)
                {
                    StopCoroutine(bonusGemsFlickerCoroutine);
                }
                
                float flickerSpeed = remainingTime <= 2f ? 0.15f : 0.5f;
                bonusGemsFlickerCoroutine = StartCoroutine(FlickerBonusGemsText(flickerSpeed));
            }
            else
            {
                // Stop flickering and set normal color
                if (bonusGemsFlickerCoroutine != null)
                {
                    StopCoroutine(bonusGemsFlickerCoroutine);
                    bonusGemsFlickerCoroutine = null;
                }
                bonusGemsText.color = Color.yellow;
            }
        }
        else
        {
            bonusGemsText.gameObject.SetActive(false);
            if (bonusGemsFlickerCoroutine != null)
            {
                StopCoroutine(bonusGemsFlickerCoroutine);
                bonusGemsFlickerCoroutine = null;
            }
        }
    }
    
    /// <summary>
    /// Flicker the bonus gems text
    /// </summary>
    private IEnumerator FlickerBonusGemsText(float flickerSpeed)
    {
        bool isYellow = true;
        
        while (bonusGemsText != null && bonusGemsText.gameObject.activeInHierarchy)
        {
            bonusGemsText.color = isYellow ? Color.yellow : Color.white;
            isYellow = !isYellow;
            yield return new WaitForSeconds(flickerSpeed);
        }
        
        // Reset to yellow when done
        if (bonusGemsText != null)
        {
            bonusGemsText.color = Color.yellow;
        }
        
        bonusGemsFlickerCoroutine = null;
    }

    /// <summary>
    /// Get the appropriate icon sprite for a powerup type using the centralized icon manager
    /// </summary>
    private Sprite GetIconForPowerupType(PowerUpType powerupType)
    {
        if (iconManager != null)
        {
            return iconManager.GetIconForPowerupType(powerupType);
        }
        
        // Fallback to PowerupIconManager static method if no instance assigned
        return PowerupIconManager.GetIcon(powerupType, null);
    }

    /// <summary>
    /// Play the collection animation (scale up then back to normal)
    /// </summary>
    private void PlayCollectionAnimation()
    {
        if (collectionAnimationCoroutine != null)
        {
            StopCoroutine(collectionAnimationCoroutine);
        }
        
        collectionAnimationCoroutine = StartCoroutine(CollectionAnimationCoroutine());
    }

    /// <summary>
    /// Coroutine for the collection animation
    /// </summary>
    private IEnumerator CollectionAnimationCoroutine()
    {
        float elapsedTime = 0f;
        Vector3 targetScale = originalScale * collectionScaleMultiplier;
        
        // Scale up phase
        while (elapsedTime < collectionAnimationDuration * 0.5f)
        {
            float progress = elapsedTime / (collectionAnimationDuration * 0.5f);
            float curveValue = collectionScaleCurve.Evaluate(progress);
            
            transform.localScale = Vector3.Lerp(originalScale, targetScale, curveValue);
            
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }
        
        // Scale down phase
        elapsedTime = 0f;
        while (elapsedTime < collectionAnimationDuration * 0.5f)
        {
            float progress = elapsedTime / (collectionAnimationDuration * 0.5f);
            float curveValue = collectionScaleCurve.Evaluate(1f - progress);
            
            transform.localScale = Vector3.Lerp(originalScale, targetScale, curveValue);
            
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }
        
        // Ensure we end at original scale
        transform.localScale = originalScale;
        collectionAnimationCoroutine = null;
    }

    /// <summary>
    /// Public method to manually set a powerup (for testing or special cases)
    /// </summary>
    public void SetPowerup(PowerUpType powerupType, float duration)
    {
        DisplayPowerup(powerupType, duration);
        PlayCollectionAnimation();
    }

    /// <summary>
    /// Public method to manually clear the powerup display
    /// </summary>
    public void ClearPowerup()
    {
        ShowEmptySlot();
    }

    /// <summary>
    /// Check if a powerup is currently displayed
    /// </summary>
    public bool HasActivePowerup()
    {
        return hasActivePowerup;
    }

    /// <summary>
    /// Get the currently displayed powerup type
    /// </summary>
    public PowerUpType? GetCurrentPowerupType()
    {
        return currentPowerupType;
    }

    /// <summary>
    /// Get the current duration/charges remaining
    /// </summary>
    public float GetCurrentDuration()
    {
        return currentDuration;
    }

    // TEST METHODS - Remove these after testing
    [Header("Testing (Remove after setup)")]
    [SerializeField] private bool testMode = false;
    
    [ContextMenu("Test MaxHandUpgrade")]
    public void TestMaxHandUpgrade()
    {
        OnPowerupCollected(PowerUpType.MaxHandUpgrade, 15f);
    }
    
    [ContextMenu("Test HomingDaggers")]
    public void TestHomingDaggers()
    {
        OnPowerupCollected(PowerUpType.HomingDaggers, 0f);
    }
    
    [ContextMenu("Test DoubleGems")]
    public void TestDoubleGems()
    {
        OnPowerupCollected(PowerUpType.DoubleGems, 30f);
    }
    
    [ContextMenu("Test Clear")]
    public void TestClear()
    {
        ShowEmptySlot();
    }
    
    private void Update()
    {
        // DISABLED: Keyboard testing removed - conflicts with emote system (keys 1-4)
        // Keys 1-4 are reserved for HandAnimationController emotes ONLY
        // Use scroll wheel + middle click for powerup control instead
    }
}
