// --- CognitiveFeedIntegration.cs - Deep Game Systems Integration ---
using UnityEngine;
using System.Collections;

/// <summary>
/// 🔗 COGNITIVE FEED INTEGRATION HELPER
/// 
/// This helper class monitors specific game systems that don't have
/// events and feeds information to the cognitive system.
/// It acts as a bridge between the cognitive feedback and existing systems.
/// </summary>
public class CognitiveFeedIntegration : MonoBehaviour
{
    [Header("🔍 Monitoring Settings")]
    [SerializeField] private float healthCheckInterval = 2f;
    [SerializeField] private float inventoryCheckInterval = 5f;
    [SerializeField] private float progressionCheckInterval = 10f;
    
    [Header("🎯 Threshold Settings")]
    [SerializeField] private float lowHealthThreshold = 0.3f;
    [SerializeField] private float criticalHealthThreshold = 0.15f;
    [SerializeField] private float inventoryFullThreshold = 0.9f;
    
    // State tracking
    private PlayerHealth cachedPlayerHealth;
    private PlayerProgression cachedPlayerProgression;
    private InventoryManager cachedInventoryManager;
    
    private float lastHealthPercentage = 1f;
    private int lastSpendableGems = 0;
    private int lastPrimaryHandLevel = 1;
    private int lastSecondaryHandLevel = 1;
    private bool wasInventoryFull = false;
    
    // Performance tracking
    private int consecutiveLowHealthWarnings = 0;
    private float lastGemCollectionTime;
    private int gemsCollectedInSession = 0;
    
    void Start()
    {
        StartCoroutine(InitializeReferences());
    }
    
    void OnEnable()
    {
        // Subscribe to PlayerProgression events
        PlayerProgression.OnSpendableGemsChanged += OnSpendableGemsChanged;
        PlayerProgression.OnPrimaryHandLevelChangedForHUD += OnPrimaryHandLevelChanged;
        PlayerProgression.OnSecondaryHandLevelChangedForHUD += OnSecondaryHandLevelChanged;
    }
    
    void OnDisable()
    {
        PlayerProgression.OnSpendableGemsChanged -= OnSpendableGemsChanged;
        PlayerProgression.OnPrimaryHandLevelChangedForHUD -= OnPrimaryHandLevelChanged;
        PlayerProgression.OnSecondaryHandLevelChangedForHUD -= OnSecondaryHandLevelChanged;
    }
    
    private IEnumerator InitializeReferences()
    {
        // Wait a bit for all systems to initialize
        yield return new WaitForSeconds(1f);
        
        cachedPlayerHealth = FindFirstObjectByType<PlayerHealth>();
        cachedPlayerProgression = PlayerProgression.Instance;
        cachedInventoryManager = InventoryManager.Instance;
        
        if (cachedPlayerHealth != null)
        {
            lastHealthPercentage = cachedPlayerHealth.GetHealthPercentage();
        }
        
        if (cachedPlayerProgression != null)
        {
            lastSpendableGems = cachedPlayerProgression.currentSpendableGems;
            lastPrimaryHandLevel = cachedPlayerProgression.primaryHandLevel;
            lastSecondaryHandLevel = cachedPlayerProgression.secondaryHandLevel;
        }
        
        // Start monitoring coroutines
        StartCoroutine(MonitorHealthContinuously());
        StartCoroutine(MonitorInventoryStatus());
        StartCoroutine(MonitorPlayerProgression());
    }
    
    #region CONTINUOUS MONITORING
    
    private IEnumerator MonitorHealthContinuously()
    {
        while (true)
        {
            yield return new WaitForSeconds(healthCheckInterval);
            
            if (cachedPlayerHealth != null && !cachedPlayerHealth.isDead)
            {
                float currentHealthPercent = cachedPlayerHealth.GetHealthPercentage();
                
                // Check for significant health changes
                if (Mathf.Abs(currentHealthPercent - lastHealthPercentage) > 0.1f)
                {
                    AnalyzeHealthChange(lastHealthPercentage, currentHealthPercent);
                    lastHealthPercentage = currentHealthPercent;
                }
                
                // Check for concerning health patterns
                if (currentHealthPercent <= criticalHealthThreshold)
                {
                    consecutiveLowHealthWarnings++;
                    if (consecutiveLowHealthWarnings >= 3)
                    {
                        CognitiveEvents.TriggerHealthEvent("critical_pattern", currentHealthPercent);
                        consecutiveLowHealthWarnings = 0; // Reset to avoid spam
                    }
                }
                else if (currentHealthPercent > lowHealthThreshold)
                {
                    consecutiveLowHealthWarnings = 0;
                }
            }
        }
    }
    
    private IEnumerator MonitorInventoryStatus()
    {
        while (true)
        {
            yield return new WaitForSeconds(inventoryCheckInterval);
            
            if (cachedInventoryManager != null)
            {
                // Check inventory fullness
                float fullnessPercentage = CalculateInventoryFullness();
                bool isNowFull = fullnessPercentage >= inventoryFullThreshold;
                
                if (isNowFull && !wasInventoryFull)
                {
                    CognitiveEvents.OnSystemAlert?.Invoke("Inventory", "near_capacity");
                }
                else if (!isNowFull && wasInventoryFull)
                {
                    CognitiveEvents.OnSystemAlert?.Invoke("Inventory", "space_available");
                }
                
                wasInventoryFull = isNowFull;
            }
        }
    }
    
    private IEnumerator MonitorPlayerProgression()
    {
        while (true)
        {
            yield return new WaitForSeconds(progressionCheckInterval);
            
            if (cachedPlayerProgression != null)
            {
                // Analyze gem spending patterns
                AnalyzeGemSpendingPattern();
                
                // Check for progression stagnation
                CheckProgressionStagnation();
            }
        }
    }
    
    #endregion
    
    #region EVENT HANDLERS
    
    private void OnSpendableGemsChanged(int newGemCount)
    {
        int gemDifference = newGemCount - lastSpendableGems;
        
        if (gemDifference > 0)
        {
            // Gems increased
            gemsCollectedInSession += gemDifference;
            lastGemCollectionTime = Time.time;
            
            // Trigger gem collection analysis
            AnalyzeGemCollection(gemDifference, newGemCount);
        }
        else if (gemDifference < 0)
        {
            // Gems spent
            AnalyzeGemSpending(-gemDifference, newGemCount);
        }
        
        lastSpendableGems = newGemCount;
    }
    
    private void OnPrimaryHandLevelChanged(int newLevel)
    {
        if (newLevel > lastPrimaryHandLevel)
        {
            CognitiveEvents.OnProgressionMilestone?.Invoke(newLevel, 0);
            TriggerHandUpgradeComment(true, newLevel);
        }
        lastPrimaryHandLevel = newLevel;
    }
    
    private void OnSecondaryHandLevelChanged(int newLevel)
    {
        if (newLevel > lastSecondaryHandLevel)
        {
            CognitiveEvents.OnProgressionMilestone?.Invoke(newLevel, 0);
            TriggerHandUpgradeComment(false, newLevel);
        }
        lastSecondaryHandLevel = newLevel;
    }
    
    #endregion
    
    #region ANALYSIS METHODS
    
    private void AnalyzeHealthChange(float fromPercent, float toPercent)
    {
        if (toPercent < fromPercent)
        {
            // Health decreased (damage taken)
            float damageSeverity = fromPercent - toPercent;
            
            if (damageSeverity > 0.3f)
            {
                CognitiveEvents.OnDamageTaken?.Invoke(damageSeverity * 100f, "high_impact");
            }
            else if (damageSeverity > 0.1f)
            {
                CognitiveEvents.OnDamageTaken?.Invoke(damageSeverity * 100f, "moderate");
            }
        }
        else
        {
            // Health increased (healing/regeneration)
            CognitiveEvents.TriggerHealthEvent("health_restored", toPercent);
        }
    }
    
    private void AnalyzeGemCollection(int gemsGained, int totalGems)
    {
        // Calculate collection efficiency
        float timeSinceLastCollection = Time.time - lastGemCollectionTime;
        
        if (gemsGained >= 10)
        {
            CognitiveEvents.TriggerPerformanceAnalysis("gem_burst", 0.9f);
        }
        else if (timeSinceLastCollection < 2f && gemsGained > 1)
        {
            CognitiveEvents.TriggerPerformanceAnalysis("efficient_collection", 0.8f);
        }
        
        // Milestone analysis
        if (totalGems % 50 == 0 && totalGems > 0)
        {
            CognitiveEvents.OnAchievementUnlocked?.Invoke($"gem_milestone_{totalGems}");
        }
    }
    
    private void AnalyzeGemSpending(int gemsSpent, int remainingGems)
    {
        float spendingRatio = (float)gemsSpent / (gemsSpent + remainingGems);
        
        if (spendingRatio > 0.8f)
        {
            CognitiveEvents.TriggerCustomEvent("high_spending", spendingRatio);
        }
        else if (spendingRatio < 0.1f)
        {
            CognitiveEvents.TriggerCustomEvent("conservative_spending", spendingRatio);
        }
    }
    
    private void TriggerHandUpgradeComment(bool isPrimaryHand, int newLevel)
    {
        string handType = isPrimaryHand ? "primary" : "secondary";
        CognitiveEvents.OnStrategicDecision?.Invoke("hand_upgrade", 
            new object[] { handType, newLevel });
    }
    
    private void AnalyzeGemSpendingPattern()
    {
        // This could analyze spending frequency, amounts, etc.
        // For now, just monitor large expenditures
        if (cachedPlayerProgression != null && lastSpendableGems > 100)
        {
            CognitiveEvents.TriggerCustomEvent("wealth_accumulation", lastSpendableGems);
        }
    }
    
    private void CheckProgressionStagnation()
    {
        // Check if player has been at the same hand levels for too long
        // This could suggest they need guidance
        if (Time.time - lastGemCollectionTime > 120f && gemsCollectedInSession < 10)
        {
            CognitiveEvents.OnSystemAlert?.Invoke("Progression", "stagnation_detected");
        }
    }
    
    private float CalculateInventoryFullness()
    {
        if (cachedInventoryManager == null) return 0f;
        
        // This would need access to the actual inventory slots
        // For now, return a placeholder calculation
        return 0.5f; // Placeholder
    }
    
    #endregion
    
    #region UTILITY METHODS
    
    /// <summary>
    /// Manually trigger a performance analysis from external systems
    /// </summary>
    public void TriggerPerformanceEvent(string actionType, float efficiency)
    {
        CognitiveEvents.TriggerPerformanceAnalysis(actionType, efficiency);
    }
    
    /// <summary>
    /// Manually trigger a combat event from external systems
    /// </summary>
    public void TriggerCombatEvent(string combatType, Vector3 position, object additionalData = null)
    {
        CognitiveEvents.TriggerCombatAnalysis(combatType, position, additionalData);
    }
    
    /// <summary>
    /// Notify cognitive system about environmental changes
    /// </summary>
    public void NotifyEnvironmentalChange(string changeType, float intensity)
    {
        CognitiveEvents.OnEnvironmentalChange?.Invoke(changeType, intensity);
    }
    
    #endregion
}