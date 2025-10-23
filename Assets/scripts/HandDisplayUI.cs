using UnityEngine;
using UnityEngine.UI;

public class HandDisplayUI : MonoBehaviour
{
    [Header("Hand Type")]
    [Tooltip("Set to true for right hand box, false for left hand box")]
    public bool isRightHand = true;
    
    [Header("Hand Images")]
    [Tooltip("Assign all 4 hand images: Index 0 = Level 1, Index 1 = Level 2, Index 2 = Level 3, Index 3 = Level 4")]
    public Sprite[] handImages = new Sprite[4];
    
    [Header("UI References")]
    [Tooltip("The Image component that will display the hand sprite")]
    public Image handImageDisplay;
    
    [Tooltip("Array of 4 Image components for level indicator boxes (left to right: level 1, 2, 3, 4)")]
    public Image[] levelIndicatorBoxes = new Image[4];
    
    [Header("Level Indicator Colors")]
    [Tooltip("Color for active/unlocked level boxes")]
    public Color activeColor = Color.yellow;
    
    [Tooltip("Color for inactive/locked level boxes")]
    public Color inactiveColor = Color.grey;
    
    // Retry mechanism fields
    private Coroutine _retryCoroutine;
    private bool _hasValidDisplay = false;
    private int _retryAttempts = 0;
    private const int MAX_QUICK_RETRIES = 30;  // 3 seconds of quick retries
    private const float QUICK_RETRY_INTERVAL = 0.1f;
    private const float SLOW_RETRY_INTERVAL = 1.0f;
    
    private void Start()
    {
        // IMMEDIATE FIX: Clear any white box by setting sprite to null and hiding
        if (handImageDisplay == null)
        {
            handImageDisplay = GetComponent<Image>();
        }
        
        // Immediately hide/clear to prevent white box
        if (handImageDisplay != null)
        {
            handImageDisplay.sprite = null;
            handImageDisplay.color = new Color(1, 1, 1, 0); // Make transparent initially
            Debug.Log($"🖐️ HandDisplayUI ({gameObject.name}): Cleared initial display to prevent white box");
        }
        
        // Validate setup
        if (handImageDisplay == null)
        {
            Debug.LogError($"HandDisplayUI ({gameObject.name}): No Image component found! Please assign handImageDisplay or add Image component.", this);
            return;
        }
        
        if (handImages.Length != 4)
        {
            Debug.LogError($"HandDisplayUI ({gameObject.name}): handImages array must have exactly 4 elements (one for each hand level)!", this);
            return;
        }
        
        if (levelIndicatorBoxes.Length != 4)
        {
            Debug.LogError($"HandDisplayUI ({gameObject.name}): levelIndicatorBoxes array must have exactly 4 elements (one for each level)!", this);
            return;
        }
        
        // Subscribe to hand level change events
        // Right hand = Secondary (RMB), Left hand = Primary (LMB)
        if (isRightHand)
        {
            PlayerProgression.OnSecondaryHandLevelChangedForHUD += OnSecondaryHandLevelChanged;
        }
        else
        {
            PlayerProgression.OnPrimaryHandLevelChangedForHUD += OnPrimaryHandLevelChanged;
        }
        
        // Start persistent retry mechanism immediately
        _hasValidDisplay = false;
        _retryAttempts = 0;
        if (_retryCoroutine != null)
        {
            StopCoroutine(_retryCoroutine);
        }
        _retryCoroutine = StartCoroutine(PersistentRetryUpdate());
    }
    
    /// <summary>
    /// Persistent retry mechanism that keeps attempting to display correct sprites until successful
    /// </summary>
    private System.Collections.IEnumerator PersistentRetryUpdate()
    {
        Debug.Log($"🖐️ HandDisplayUI ({gameObject.name}): Starting persistent retry mechanism for {(isRightHand ? "RIGHT" : "LEFT")} hand");
        
        // Keep trying until we have a valid display
        while (!_hasValidDisplay)
        {
            _retryAttempts++;
            
            // Try to update the display
            UpdateHandDisplay();
            
            // Check if we now have a valid display (will be set by UpdateHandDisplay)
            if (_hasValidDisplay)
            {
                Debug.Log($"✅ HandDisplayUI ({gameObject.name}): Successfully displayed hand after {_retryAttempts} attempts!");
                break;
            }
            
            // Determine retry interval based on attempt count
            float retryInterval;
            if (_retryAttempts <= MAX_QUICK_RETRIES)
            {
                // Quick retries for first 3 seconds
                retryInterval = QUICK_RETRY_INTERVAL;
            }
            else
            {
                // Slower retries after that to avoid performance impact
                retryInterval = SLOW_RETRY_INTERVAL;
                
                // Log every 10 slow retries
                if (_retryAttempts % 10 == 0)
                {
                    Debug.LogWarning($"⚠️ HandDisplayUI ({gameObject.name}): Still trying to display hand (attempt {_retryAttempts})...");
                }
            }
            
            yield return new WaitForSeconds(retryInterval);
        }
        
        _retryCoroutine = null;
    }
    
    private void OnEnable()
    {
        // When UI is re-enabled (like opening stash), clear white box and restart retry if needed
        if (handImageDisplay != null)
        {
            // Immediately clear any potential white box
            handImageDisplay.sprite = null;
            handImageDisplay.color = new Color(1, 1, 1, 0);
            
            // If we don't have a valid display, restart the retry mechanism
            if (!_hasValidDisplay)
            {
                Debug.Log($"🖐️ HandDisplayUI ({gameObject.name}): OnEnable - No valid display, restarting retry mechanism");
                
                if (_retryCoroutine != null)
                {
                    StopCoroutine(_retryCoroutine);
                }
                _retryAttempts = 0;
                _retryCoroutine = StartCoroutine(PersistentRetryUpdate());
            }
            else
            {
                // Even if we had valid display, refresh it when re-enabled
                UpdateHandDisplay();
            }
        }
    }
    
    private void OnDisable()
    {
        // Stop retry coroutine when disabled to save performance
        if (_retryCoroutine != null)
        {
            StopCoroutine(_retryCoroutine);
            _retryCoroutine = null;
        }
    }
    
    private void OnDestroy()
    {
        // Stop any running coroutines
        if (_retryCoroutine != null)
        {
            StopCoroutine(_retryCoroutine);
            _retryCoroutine = null;
        }
        
        // Unsubscribe from events
        // Right hand = Secondary (RMB), Left hand = Primary (LMB)
        if (isRightHand)
        {
            PlayerProgression.OnSecondaryHandLevelChangedForHUD -= OnSecondaryHandLevelChanged;
        }
        else
        {
            PlayerProgression.OnPrimaryHandLevelChangedForHUD -= OnPrimaryHandLevelChanged;
        }
    }
    
    private void OnPrimaryHandLevelChanged(int newLevel)
    {
        // Primary = Left hand (LMB)
        if (!isRightHand)
        {
            UpdateHandDisplay();
        }
    }
    
    private void OnSecondaryHandLevelChanged(int newLevel)
    {
        // Secondary = Right hand (RMB)
        if (isRightHand)
        {
            UpdateHandDisplay();
        }
    }
    
    /// <summary>
    /// PUBLIC METHOD: Manual refresh for external systems to trigger UI update
    /// </summary>
    public void ManualRefresh()
    {
        Debug.Log($"🖐️ HandDisplayUI ({gameObject.name}): Manual refresh triggered for {(isRightHand ? "RIGHT" : "LEFT")} hand");
        UpdateHandDisplay();
    }
    
    public void UpdateHandDisplay()
    {
        if (handImageDisplay == null)
        {
            Debug.LogWarning($"🖐️ HandDisplayUI ({gameObject.name}): handImageDisplay is null, cannot update display");
            _hasValidDisplay = false;
            return;
        }
        
        // Get hand level data from the appropriate source
        int primaryLevel, secondaryLevel;
        bool isSecondaryHandUnlocked;
        string dataSource = "UNKNOWN";
        bool hasDataSource = false;
        
        if (PlayerProgression.Instance != null)
        {
            // Game scene - get data from PlayerProgression
            primaryLevel = PlayerProgression.Instance.primaryHandLevel;
            secondaryLevel = PlayerProgression.Instance.secondaryHandLevel;
            isSecondaryHandUnlocked = true; // Both hands always unlocked
            dataSource = "PlayerProgression";
            hasDataSource = true;
            
            if (_retryAttempts <= 5) // Only log first few attempts to avoid spam
            {
                Debug.Log($"🖐️ HandDisplayUI ({gameObject.name}): Using {dataSource} data - Primary={primaryLevel}, Secondary={secondaryLevel}, Unlocked={isSecondaryHandUnlocked}");
            }
        }
        else if (HandLevelPersistenceManager.Instance != null)
        {
            // Menu scene - get data from HandLevelPersistenceManager
            primaryLevel = HandLevelPersistenceManager.Instance.CurrentPrimaryHandLevel;
            secondaryLevel = HandLevelPersistenceManager.Instance.CurrentSecondaryHandLevel;
            isSecondaryHandUnlocked = HandLevelPersistenceManager.Instance.IsSecondHandUnlocked;
            dataSource = "HandLevelPersistenceManager";
            hasDataSource = true;
            
            if (_retryAttempts <= 5) // Only log first few attempts to avoid spam
            {
                Debug.Log($"🖐️ HandDisplayUI ({gameObject.name}): Using {dataSource} data - Primary={primaryLevel}, Secondary={secondaryLevel}, Unlocked={isSecondaryHandUnlocked}");
            }
        }
        else
        {
            // No data source available yet - keep retrying
            if (_retryAttempts == 1 || _retryAttempts % 10 == 0)
            {
                Debug.LogWarning($"🖐️ HandDisplayUI ({gameObject.name}): No data source available yet (attempt {_retryAttempts})! Will keep retrying...");
            }
            _hasValidDisplay = false;
            return; // Don't use fallback, just keep retrying
        }
        
        if (isRightHand)
        {
            // Right hand = Secondary (RMB) - always show current level
            bool success = SetHandImage(secondaryLevel);
            UpdateLevelIndicators(secondaryLevel);
            _hasValidDisplay = success && hasDataSource;
        }
        else
        {
            // Left hand = Primary (LMB) - always show current level
            bool success = SetHandImage(primaryLevel);
            handImageDisplay.enabled = true;
            UpdateLevelIndicators(primaryLevel);
            _hasValidDisplay = success && hasDataSource;
        }
    }
    
    private bool SetHandImage(int handLevel)
    {
        // Clamp level to valid range (1-4)
        handLevel = Mathf.Clamp(handLevel, 1, 4);
        
        // Convert to array index (level 1 = index 0, level 2 = index 1, etc.)
        int imageIndex = handLevel - 1;
        
        // Enhanced validation to catch missing/broken sprite references
        bool hasValidSprite = false;
        if (imageIndex >= 0 && imageIndex < handImages.Length && handImages[imageIndex] != null)
        {
            // Additional check for broken references that aren't null but are missing
            try 
            {
                // This will fail if the sprite reference is broken
                if (handImages[imageIndex].texture != null)
                {
                    handImageDisplay.sprite = handImages[imageIndex];
                    handImageDisplay.color = new Color(1, 1, 1, 1); // Full opacity
                    handImageDisplay.enabled = true;
                    hasValidSprite = true;
                    
                    if (_retryAttempts <= 5 || _retryAttempts % 10 == 0)
                    {
                        Debug.Log($"✅ HandDisplayUI ({gameObject.name}): Successfully set sprite for hand level {handLevel} on attempt {_retryAttempts}");
                    }
                }
            }
            catch (System.Exception e)
            {
                if (_retryAttempts == 1 || _retryAttempts % 10 == 0)
                {
                    Debug.LogError($"HandDisplayUI ({gameObject.name}): Sprite reference is broken for level {handLevel}! Error: {e.Message}", this);
                }
            }
        }
        
        if (!hasValidSprite)
        {
            if (_retryAttempts == 1 || _retryAttempts % 20 == 0)
            {
                Debug.LogWarning($"HandDisplayUI ({gameObject.name}): No valid sprite for hand level {handLevel} (index {imageIndex}) on attempt {_retryAttempts}! Clearing display.", this);
            }
            
            // Clear the sprite to prevent white image
            handImageDisplay.sprite = null;
            handImageDisplay.color = new Color(1, 1, 1, 0); // Fully transparent
            handImageDisplay.enabled = false;
        }
        
        return hasValidSprite;
    }
    
    private void UpdateLevelIndicators(int currentLevel)
    {
        // If level is 0, hide all indicators (for locked secondary hand)
        if (currentLevel <= 0)
        {
            for (int i = 0; i < levelIndicatorBoxes.Length; i++)
            {
                if (levelIndicatorBoxes[i] != null)
                {
                    levelIndicatorBoxes[i].enabled = false;
                }
            }
            return;
        }
        
        // Update each level indicator box
        for (int i = 0; i < levelIndicatorBoxes.Length; i++)
        {
            if (levelIndicatorBoxes[i] != null)
            {
                levelIndicatorBoxes[i].enabled = true;
                
                // Level indicator logic: if current level is greater than box index + 1, make it yellow
                // Box 0 = Level 1, Box 1 = Level 2, Box 2 = Level 3, Box 3 = Level 4
                int boxLevel = i + 1;
                
                if (currentLevel >= boxLevel)
                {
                    // This level is unlocked/active - make it yellow
                    levelIndicatorBoxes[i].color = activeColor;
                }
                else
                {
                    // This level is locked/inactive - make it grey
                    levelIndicatorBoxes[i].color = inactiveColor;
                }
            }
        }
    }
}
