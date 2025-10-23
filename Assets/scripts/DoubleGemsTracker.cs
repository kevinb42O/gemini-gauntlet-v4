using UnityEngine;
using TMPro;
using System.Collections;

public class DoubleGemsTracker : MonoBehaviour
{
    public static DoubleGemsTracker Instance { get; private set; }

    [Header("PowerupInventoryManager Integration")]
    [SerializeField] private PowerupInventoryManager powerupInventoryManager; // Reference to the PowerupInventoryManager
    
    [Header("Flickering Settings")]
    [SerializeField] private float slowFlickerThreshold = 5f; // Start slow flicker at 5 seconds
    [SerializeField] private float fastFlickerThreshold = 2f; // Start fast flicker at 2 seconds
    
    // Tracking variables
    private bool isDoubleGemsActive = false;
    private int bonusGemsCollected = 0;
    private float remainingTime = 0f;
    private float totalDuration = 0f;
    
    // Flickering coroutines
    private Coroutine flickerCoroutine;
    
    [Header("Debug")]
    [SerializeField] private bool verboseDebugging = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Find PowerupInventoryManager if not assigned
        if (powerupInventoryManager == null)
        {
            powerupInventoryManager = PowerupInventoryManager.Instance;
            if (powerupInventoryManager == null)
            {
                Debug.LogWarning("[DoubleGemsTracker] PowerupInventoryManager not found. Please assign it in the Inspector.");
            }
        }
    }

    private void Start()
    {
        // Subscribe to gem collection events
        if (InventoryManager.Instance != null)
        {
            InventoryManager.OnGemCollected += OnGemCollected;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (InventoryManager.Instance != null)
        {
            InventoryManager.OnGemCollected -= OnGemCollected;
        }
    }

    /// <summary>
    /// Start tracking double gems for the specified duration
    /// </summary>
    public void StartDoubleGemsTracking(float duration)
    {
        if (verboseDebugging)
        {
            Debug.Log($"[DoubleGemsTracker] Starting double gems tracking for {duration} seconds", this);
        }
        
        isDoubleGemsActive = true;
        bonusGemsCollected = 0;
        totalDuration = duration;
        remainingTime = duration;
        
        // Update PowerupDisplay with bonus gems
        UpdateDisplay();
        
        // Start countdown coroutine
        StartCoroutine(DoubleGemsCountdown());
    }

    /// <summary>
    /// Stop double gems tracking and grant bonus gems
    /// </summary>
    public void StopDoubleGemsTracking()
    {
        if (!isDoubleGemsActive) return;
        
        if (verboseDebugging)
        {
            Debug.Log($"[DoubleGemsTracker] Stopping double gems tracking. Granting {bonusGemsCollected} bonus gems", this);
        }
        
        isDoubleGemsActive = false;
        
        // Grant bonus gems to inventory
        if (InventoryManager.Instance != null && bonusGemsCollected > 0)
        {
            InventoryManager.Instance.AddGems(bonusGemsCollected);
            
            // Show bonus gems message
            if (DynamicPlayerFeedManager.Instance != null)
            {
                DynamicPlayerFeedManager.Instance.ShowCustomMessage(
                    $"+{bonusGemsCollected} Bonus Gems Awarded!",
                    Color.yellow,
                    null,
                    true,
                    3f
                );
            }
        }
        
        // Stop flickering
        if (flickerCoroutine != null)
        {
            StopCoroutine(flickerCoroutine);
            flickerCoroutine = null;
        }
        
        // Clear bonus gems display
        if (powerupInventoryManager != null)
        {
            powerupInventoryManager.UpdateBonusGemsDisplay(0, 0, false);
        }
        
        // Reset values
        bonusGemsCollected = 0;
        remainingTime = 0f;
    }

    /// <summary>
    /// Called when a gem is collected during double gems period
    /// </summary>
    private void OnGemCollected(int gemValue)
    {
        if (!isDoubleGemsActive) return;
        
        // Track the bonus gems (the extra amount beyond normal collection)
        bonusGemsCollected += gemValue;
        
        if (verboseDebugging)
        {
            Debug.Log($"[DoubleGemsTracker] Gem collected during double gems: +{gemValue}, Total bonus: {bonusGemsCollected}", this);
        }
        
        UpdateDisplay();
    }

    /// <summary>
    /// Update the PowerupInventoryManager with bonus gems info
    /// </summary>
    private void UpdateDisplay()
    {
        if (powerupInventoryManager != null)
        {
            bool shouldFlicker = remainingTime <= slowFlickerThreshold;
            powerupInventoryManager.UpdateBonusGemsDisplay(bonusGemsCollected, remainingTime, shouldFlicker);
        }
    }

    /// <summary>
    /// Countdown coroutine for double gems duration
    /// </summary>
    private IEnumerator DoubleGemsCountdown()
    {
        while (remainingTime > 0 && isDoubleGemsActive)
        {
            remainingTime -= Time.deltaTime;
            UpdateDisplay();
            
            // Handle flickering based on remaining time
            HandleFlickering();
            
            yield return null;
        }
        
        // Time's up - stop tracking
        if (isDoubleGemsActive)
        {
            StopDoubleGemsTracking();
        }
    }

    /// <summary>
    /// Handle flickering animation based on remaining time
    /// </summary>
    private void HandleFlickering()
    {
        // Flickering is now handled by PowerupDisplay.UpdateBonusGemsDisplay
        // This method is kept for compatibility but no longer needed
    }

    // Flickering is now handled by PowerupDisplay - this method removed

    /// <summary>
    /// Force stop double gems (for emergency cleanup)
    /// </summary>
    public void ForceStop()
    {
        if (isDoubleGemsActive)
        {
            StopDoubleGemsTracking();
        }
    }

    /// <summary>
    /// Check if double gems is currently active
    /// </summary>
    public bool IsActive()
    {
        return isDoubleGemsActive;
    }

    /// <summary>
    /// Get current bonus gems collected
    /// </summary>
    public int GetBonusGemsCollected()
    {
        return bonusGemsCollected;
    }

    /// <summary>
    /// Get remaining time
    /// </summary>
    public float GetRemainingTime()
    {
        return remainingTime;
    }
}
