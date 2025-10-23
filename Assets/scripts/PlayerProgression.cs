// --- PlayerProgression.cs (CORRECTED) ---
using UnityEngine;
using GeminiGauntlet.Audio;
using System;
using System.Collections;

public class PlayerProgression : MonoBehaviour
{
    public static PlayerProgression Instance { get; private set; }

    public enum LevelUpEffectSpawnLocation
    {
        AtHandEmitPoint,
        AtPlayerCenter_GroundAOE
    }

    [Header("Hand Leveling (Automatic via Gem Collection)")]
    public int[] gemsNeededForLevel = { 5, 15, 30 };
    [Min(1)] public int maxHandLevel = 4;
    public int primaryHandLevel { get; private set; } = 1;
    public int gemsCollectedForPrimaryHand { get; private set; } = 0;
    public int secondaryHandLevel { get; private set; } = 1;
    public int gemsCollectedForSecondaryHand { get; private set; } = 0;

    [Header("Spendable Gems (Currency)")]
    public int initialSpendableGemsForTesting = 25;
    private int _currentSpendableGems = -1; // -1 means not initialized yet
    public int currentSpendableGems 
    { 
        get 
        {
            // Initialize on first access if not already done
            if (_currentSpendableGems == -1)
            {
                _currentSpendableGems = initialSpendableGemsForTesting;
                if (_currentSpendableGems > 0)
                {
                }
            }
            return _currentSpendableGems;
        }
        private set 
        {
            _currentSpendableGems = value;
        }
    }
    public static event Action<int> OnSpendableGemsChanged;

    /// <summary>
    /// Decrease spendable gems by specified amount (for stash transfer to prevent duplication)
    /// </summary>
    public void DecreaseSpendableGems(int amount)
    {
        if (amount <= 0) return;
        
        int oldAmount = currentSpendableGems;
        currentSpendableGems = Mathf.Max(0, currentSpendableGems - amount);
        
        
        // Fire event for UI updates
        OnSpendableGemsChanged?.Invoke(currentSpendableGems);
    }

    [Header("Upgrade Costs (Configurable in Inspector)")]
    public int costToUnlockSecondaryHand = 1;
    public int[] primaryHandUpgradeCosts = { 10, 20, 40 };
    public int[] secondaryHandUpgradeCosts = { 10, 20, 40 };

    [Header("Player Stat Upgrade Costs & Amounts")]
    public int costForMoveSpeedUpgrade = 5;
    public float moveSpeedUpgradeAmount = 1f;
    public int costForJumpHeightUpgrade = 5;
    public float jumpHeightUpgradeAmount = 0.2f;

    [Header("Level Up Visuals")]
    public GameObject levelUpEffectPrefab;
    public LevelUpEffectSpawnLocation levelUpEffectSpawnType = LevelUpEffectSpawnLocation.AtPlayerCenter_GroundAOE;
    public Transform levelUpEffectSpawnPointFallback;
    [Min(0.1f)] public float levelUpEffectDuration = 3.0f;
    public Vector3 levelUpEffectScale = Vector3.one;
    public float groundRaycastDistance = 5f;
    public LayerMask groundLayerMask = ~0;

    [Header("Gem AOE Collection Settings")]
    public float gemCollectionRadius = 150f;
    [Tooltip("Layer mask for gems. Set to 'Everything' (-1) if gems don't have specific layer, or configure gem layer.")] 
    public LayerMask gemLayerMaskForCollection = -1; // -1 means all layers (default fix)
    
    [Header("Hand Visual System")]
    [Tooltip("Reference to LayeredHandAnimationController for visual updates")]
    private LayeredHandAnimationController handAnimationController;
    
    [Header("PowerUp States (Runtime)")]
    public bool IsDoubleGemsActive { get; private set; } = false;

    public static event Action<int, int> OnPrimaryHandGemsChangedForHUD;
    public static event Action<int> OnPrimaryHandLevelChangedForHUD;
    public static event Action<int, int> OnSecondaryHandGemsChangedForHUD;
    public static event Action<int> OnSecondaryHandLevelChangedForHUD;
    public static event Action OnProgressionResetForHUD;
    public static event Action<PowerUpType, bool, float> OnPowerUpStatusChangedForHUD;

    private CelestialDriftController _celestialDriftController;
    
    // Coroutine references for power-ups
    private Coroutine _doubleGemsCoroutine;
    private float _doubleGemsEndTime;
    private Coroutine _slowTimeCoroutine;
    
    // Audio handling moved to centralized GameSounds system

    void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); return; }

        _celestialDriftController = GetComponent<CelestialDriftController>();
        
        // Auto-find hand animation controller for visual updates
        handAnimationController = GetComponent<LayeredHandAnimationController>();
        if (handAnimationController == null)
            handAnimationController = FindObjectOfType<LayeredHandAnimationController>();
        
        // Cache PlayerHealth reference for reliable godmode functionality
        CachePlayerHealthReference();
        
        // Initialize starting gems for testing
        if (initialSpendableGemsForTesting > 0)
        {
            currentSpendableGems = initialSpendableGemsForTesting;
            // Fire the event so UI updates immediately
            OnSpendableGemsChanged?.Invoke(currentSpendableGems);
        }
    }

    private bool _eventsSubscribed = false;

    void OnEnable()
    {
        // Note: Input event subscription moved to Start() to fix initialization timing
    }

    void OnDisable()
    {
        // Unsubscribe from double-click gem collection events
        if (_eventsSubscribed && PlayerInputHandler.Instance != null)
        {
            PlayerInputHandler.Instance.OnPrimaryDoubleClickGemCollectAction -= OnPrimaryDoubleClickGemCollection;
            PlayerInputHandler.Instance.OnSecondaryDoubleClickGemCollectAction -= OnSecondaryDoubleClickGemCollection;
            _eventsSubscribed = false;
        }
    }

    // Helper method to safely subscribe to input events
    private void TrySubscribeToInputEvents()
    {
        if (PlayerInputHandler.Instance != null && !_eventsSubscribed)
        {
            PlayerInputHandler.Instance.OnPrimaryDoubleClickGemCollectAction += OnPrimaryDoubleClickGemCollection;
            PlayerInputHandler.Instance.OnSecondaryDoubleClickGemCollectAction += OnSecondaryDoubleClickGemCollection;
            _eventsSubscribed = true;
        }
        else if (PlayerInputHandler.Instance == null)
        {
        }
        else if (_eventsSubscribed)
        {
        }
    }

    // Separate methods for proper event subscription/unsubscription
    private void OnPrimaryDoubleClickGemCollection()
    {
        AttemptDoubleClickCollection(true);
    }

    private void OnSecondaryDoubleClickGemCollection()
    {
        AttemptDoubleClickCollection(false);
    }

    void Start()
    {
        // Initialize immediately with defaults - hand levels will update when persistence manager loads
        InitializeOrResetForNewRun();
        
        // Start coroutine to check for persistence manager and update hand levels when ready
        StartCoroutine(CheckForPersistenceManagerAndUpdate());
        
        // Start coroutine to retry input event subscription until successful
        StartCoroutine(RetryInputEventSubscription());
    }
    
    /// <summary>
    /// Check for HandLevelPersistenceManager and update hand levels when available
    /// This allows hands to start at (1,1) and visibly update to correct saved levels
    /// </summary>
    private IEnumerator CheckForPersistenceManagerAndUpdate()
    {
        float checkInterval = 0.2f; // Check every 0.2 seconds
        float maxWaitTime = 10f; // Keep trying for 10 seconds
        float elapsedTime = 0f;
        
        
        // Keep checking until persistence manager is available or timeout
        while (HandLevelPersistenceManager.Instance == null && elapsedTime < maxWaitTime)
        {
            yield return new WaitForSeconds(checkInterval);
            elapsedTime += checkInterval;
        }
        
        if (HandLevelPersistenceManager.Instance != null)
        {
            int savedPrimary = HandLevelPersistenceManager.Instance.CurrentPrimaryHandLevel;
            int savedSecondary = HandLevelPersistenceManager.Instance.CurrentSecondaryHandLevel;
            
            
            // ENHANCED DEBUGGING: Track what's happening with each hand
            
            // Force upgrade PRIMARY hand to saved level
            if (savedPrimary != primaryHandLevel)
            {
                DEBUG_AdminSetHandLevel(true, savedPrimary); // isPrimary = true
            }
            else
            {
            }
            
            // Force upgrade SECONDARY hand to saved level
            if (savedSecondary != secondaryHandLevel)
            {
                DEBUG_AdminSetHandLevel(false, savedSecondary); // isPrimary = false
            }
            else
            {
            }
            
        }
        else
        {
        }
    }
    
    private IEnumerator RetryInputEventSubscription()
    {
        float retryInterval = 0.1f; // Check every 0.1 seconds
        float maxRetryTime = 5f; // Give up after 5 seconds
        float elapsedTime = 0f;
        
        while (!_eventsSubscribed && elapsedTime < maxRetryTime)
        {
            TrySubscribeToInputEvents();
            if (!_eventsSubscribed)
            {
                yield return new WaitForSeconds(retryInterval);
                elapsedTime += retryInterval;
            }
        }
        
        if (!_eventsSubscribed)
        {
        }
        else
        {
        }
    }

    public void InitializeOrResetForNewRun()
    {
        currentSpendableGems = initialSpendableGemsForTesting;
        OnSpendableGemsChanged?.Invoke(currentSpendableGems);
        
        // HAND LEVEL PERSISTENCE: Load saved hand levels instead of hardcoding to 1
        if (HandLevelPersistenceManager.Instance != null)
        {
            primaryHandLevel = HandLevelPersistenceManager.Instance.CurrentPrimaryHandLevel;
            secondaryHandLevel = HandLevelPersistenceManager.Instance.CurrentSecondaryHandLevel;
        }
        else
        {
            // Fallback to defaults if persistence manager not available
            primaryHandLevel = 1;
            secondaryHandLevel = 1;
        }
        
        gemsCollectedForPrimaryHand = 0;
        gemsCollectedForSecondaryHand = 0;
        
        // ... (rest of your reset logic)
        
        OnProgressionResetForHUD?.Invoke();
        OnPrimaryHandLevelChangedForHUD?.Invoke(primaryHandLevel);
        OnPrimaryHandGemsChangedForHUD?.Invoke(gemsCollectedForPrimaryHand, GetGemsNeededForNext_AutoLevel_Threshold(true));
        OnSecondaryHandLevelChangedForHUD?.Invoke(secondaryHandLevel);
        OnSecondaryHandGemsChangedForHUD?.Invoke(gemsCollectedForSecondaryHand, GetGemsNeededForNext_AutoLevel_Threshold(false));
    }

    public void RegisterGemCollection(bool collectedByPrimaryHandInitially)
    {
        int gemsToAdd = IsDoubleGemsActive ? 2 : 1;
        currentSpendableGems += gemsToAdd;
        OnSpendableGemsChanged?.Invoke(currentSpendableGems);
        
        // Use the improved FlavorTextManager gem collection system instead of direct messages
        // This is intentionally left empty as the gem collection messages are now handled by the
        // hand system that calls RegisterGemCollection, using FlavorTextManager.RecordGemCollection()

        if (collectedByPrimaryHandInitially)
        {
            if (primaryHandLevel < maxHandLevel)
            {
                gemsCollectedForPrimaryHand += gemsToAdd;
                CheckForAutoLevelUp(true);
            }
        }
        else
        {
            if (secondaryHandLevel < maxHandLevel)
            {
                gemsCollectedForSecondaryHand += gemsToAdd;
                CheckForAutoLevelUp(false);
            }
        }
        OnPrimaryHandGemsChangedForHUD?.Invoke(gemsCollectedForPrimaryHand, GetGemsNeededForNext_AutoLevel_Threshold(true));
        OnSecondaryHandGemsChangedForHUD?.Invoke(gemsCollectedForSecondaryHand, GetGemsNeededForNext_AutoLevel_Threshold(false));
    }

    public bool TrySpendGems(int amount)
    {
        if (currentSpendableGems >= amount)
        {
            currentSpendableGems -= amount;
            OnSpendableGemsChanged?.Invoke(currentSpendableGems);
            return true;
        }
        return false;
    }

    // --- Purchase Methods ---
    
    public bool PurchaseMoveSpeedUpgrade()
    {
        if (TrySpendGems(costForMoveSpeedUpgrade))
        {
            if (_celestialDriftController != null)
            {
                // Note: moveSpeed is now private in the refactored CelestialDriftController
                // This upgrade functionality needs to be reimplemented
                CognitiveFeedManager.Instance?.QueueMessage("Mobility enhanced.");
                return true;
            }
        }
        return false;
    }

    public bool PurchaseJumpHeightUpgrade()
    {
        if (TrySpendGems(costForJumpHeightUpgrade))
        {
            if (_celestialDriftController != null)
            {
                // Note: jumpHeight and SetJumpParameters are now removed in the refactored CelestialDriftController
                // This upgrade functionality needs to be reimplemented
                CognitiveFeedManager.Instance?.QueueMessage("Vertical thrust augmented.");
                return true;
            }
        }
        return false;
    }
    
    public int GetGemsNeededForNext_AutoLevel_Threshold(bool isPrimaryHand)
    {
        int currentLevel = isPrimaryHand ? primaryHandLevel : secondaryHandLevel;
        if (currentLevel >= maxHandLevel) return 0;
        int thresholdIndex = currentLevel - 1;
        if (gemsNeededForLevel != null && thresholdIndex >= 0 && thresholdIndex < gemsNeededForLevel.Length)
        {
            return gemsNeededForLevel[thresholdIndex];
        }
        return int.MaxValue;
    }

    private void CheckForAutoLevelUp(bool isPrimaryHand)
    {
        int currentLevel = isPrimaryHand ? primaryHandLevel : secondaryHandLevel;
        int gemsCollected = isPrimaryHand ? gemsCollectedForPrimaryHand : gemsCollectedForSecondaryHand;

        if (currentLevel >= maxHandLevel) return;

        int gemsNeeded = GetGemsNeededForNext_AutoLevel_Threshold(isPrimaryHand);
        if (gemsNeeded == int.MaxValue || gemsNeeded == 0 || gemsCollected < gemsNeeded) return;

        PerformAutoLevelUp(isPrimaryHand, gemsCollected - gemsNeeded);
    }

    private void PerformAutoLevelUp(bool isPrimaryHand, int overflowGems)
    {
        int newLevelToShow = 0;
        if (isPrimaryHand)
        {
            primaryHandLevel++;
            gemsCollectedForPrimaryHand = overflowGems;
            newLevelToShow = primaryHandLevel;
            PlayerShooterOrchestrator.Instance?.HandlePrimaryHandLevelChanged(primaryHandLevel);
            OnPrimaryHandLevelChangedForHUD?.Invoke(primaryHandLevel);
            
            // UPDATE HAND VISUALS: Change holographic appearance
            handAnimationController?.OnHandLevelChanged(true, primaryHandLevel);
        }
        else
        {
            secondaryHandLevel++;
            gemsCollectedForSecondaryHand = overflowGems;
            newLevelToShow = secondaryHandLevel;
            PlayerShooterOrchestrator.Instance?.HandleSecondaryHandLevelChanged(secondaryHandLevel);
            OnSecondaryHandLevelChangedForHUD?.Invoke(secondaryHandLevel);
            
            // UPDATE HAND VISUALS: Change holographic appearance
            handAnimationController?.OnHandLevelChanged(false, secondaryHandLevel);
        }
        
        // HAND LEVEL PERSISTENCE: Update stored hand levels immediately after level up
        if (HandLevelPersistenceManager.Instance != null)
        {
            HandLevelPersistenceManager.Instance.UpdateStoredHandLevels(primaryHandLevel, secondaryHandLevel);
        }
        PlayLevelUpEffect(isPrimaryHand);

        // Play sound
        PlayHandUpgradeSound(isPrimaryHand, newLevelToShow);

        PlayerOverheatManager.Instance?.ResetHandHeat(isPrimaryHand, true);
        DynamicPlayerFeedManager.Instance?.ShowHandLevelUp(isPrimaryHand, newLevelToShow);

        // --- FLY UNLOCK LOGIC REMOVED ---

        if ((isPrimaryHand && primaryHandLevel < maxHandLevel) ||
            (!isPrimaryHand && secondaryHandLevel < maxHandLevel))
        {
            CheckForAutoLevelUp(isPrimaryHand);
        }
        else
        {
            if (isPrimaryHand) OnPrimaryHandGemsChangedForHUD?.Invoke(gemsCollectedForPrimaryHand, GetGemsNeededForNext_AutoLevel_Threshold(true));
            else OnSecondaryHandGemsChangedForHUD?.Invoke(gemsCollectedForSecondaryHand, GetGemsNeededForNext_AutoLevel_Threshold(false));
        }
    }

    private void PlayHandUpgradeSound(bool isPrimaryHand, int newLevel)
    {
        // Using centralized GameSounds system with the unified HandUpgrade method
        // that handles both primary and secondary hand upgrades
        Vector3 playerPosition = transform.position;
        string handType = isPrimaryHand ? "primary" : "secondary";
        
        // Log which hand is being upgraded
        
        // Use the handUpgradeSoundsByLevel array in SoundEvents
        // The array is 0-indexed, but levels start at 1, so newLevel-2 gives the correct index
        // Level 2 = index 0, Level 3 = index 1, Level 4 = index 2
        GameSounds.PlayHandUpgrade(newLevel, 1.0f);
        
        // Play power-up sound for additional feedback
        GameSounds.PlayPowerUpStart(playerPosition, 0.7f);
    }

    public bool DegradeHandLevelDueToOverheat()
    {
        if (primaryHandLevel <= 1) return false;
        primaryHandLevel--;
        gemsCollectedForPrimaryHand = 0;
        PlayerShooterOrchestrator.Instance?.HandlePrimaryHandLevelChanged(primaryHandLevel);
        OnPrimaryHandLevelChangedForHUD?.Invoke(primaryHandLevel);
        OnPrimaryHandGemsChangedForHUD?.Invoke(gemsCollectedForPrimaryHand, GetGemsNeededForNext_AutoLevel_Threshold(true));
        PlayerOverheatManager.Instance?.ResetHandHeat(true, true);
        DynamicPlayerFeedManager.Instance?.ShowHandDegraded(true, primaryHandLevel);
        
        // UPDATE HAND VISUALS: Change holographic appearance after degradation
        handAnimationController?.OnHandLevelChanged(true, primaryHandLevel);
        
        // HAND LEVEL PERSISTENCE: Update stored hand levels immediately after degradation
        if (HandLevelPersistenceManager.Instance != null)
        {
            HandLevelPersistenceManager.Instance.UpdateStoredHandLevels(primaryHandLevel, secondaryHandLevel);
        }
        
        return true;
    }
    
    /// <summary>
    /// PUBLIC METHOD FOR PERSISTENCE: Set hand levels from saved data
    /// This method is specifically for loading saved hand level data on startup
    /// </summary>
    public void SetHandLevelsFromSavedData(int savedPrimaryLevel, int savedSecondaryLevel)
    {
        
        // Clamp values to valid range
        savedPrimaryLevel = Mathf.Clamp(savedPrimaryLevel, 1, maxHandLevel);
        savedSecondaryLevel = Mathf.Clamp(savedSecondaryLevel, 1, maxHandLevel);
        
        // Set the private properties (this is the only place we should do this outside of normal gameplay)
        primaryHandLevel = savedPrimaryLevel;
        secondaryHandLevel = savedSecondaryLevel;
        
        // Reset gem counts when loading (gems don't persist across sessions by design)
        gemsCollectedForPrimaryHand = 0;
        gemsCollectedForSecondaryHand = 0;
        
        // UPDATE HAND VISUALS: Set holographic appearance to loaded levels
        handAnimationController?.OnHandLevelChanged(true, primaryHandLevel);
        handAnimationController?.OnHandLevelChanged(false, secondaryHandLevel);
        
        // Notify all systems of the loaded hand levels
        PlayerShooterOrchestrator.Instance?.HandlePrimaryHandLevelChanged(primaryHandLevel);
        PlayerShooterOrchestrator.Instance?.HandleSecondaryHandLevelChanged(secondaryHandLevel);
        
        // Update HUD elements
        OnPrimaryHandLevelChangedForHUD?.Invoke(primaryHandLevel);
        OnSecondaryHandLevelChangedForHUD?.Invoke(secondaryHandLevel);
        OnPrimaryHandGemsChangedForHUD?.Invoke(gemsCollectedForPrimaryHand, GetGemsNeededForNext_AutoLevel_Threshold(true));
        OnSecondaryHandGemsChangedForHUD?.Invoke(gemsCollectedForSecondaryHand, GetGemsNeededForNext_AutoLevel_Threshold(false));
        
    }
    
    /// <summary>
    /// PUBLIC METHOD FOR DEATH RESET: Reset all hand levels and gems to starting values on player death
    /// This method is specifically called when the player dies to reset progression
    /// </summary>
    public void ResetHandLevelsOnDeath(bool isActualDeath = true)
    {
        if (!isActualDeath)
        {
            return;
        }
        
        
        // Reset hand levels to starting values ONLY on actual death
        primaryHandLevel = 1;
        secondaryHandLevel = 1;
        
        // Reset gem counts to zero
        gemsCollectedForPrimaryHand = 0;
        gemsCollectedForSecondaryHand = 0;
        
        // Notify all systems of the reset hand levels
        PlayerShooterOrchestrator.Instance?.HandlePrimaryHandLevelChanged(primaryHandLevel);
        PlayerShooterOrchestrator.Instance?.HandleSecondaryHandLevelChanged(secondaryHandLevel);
        
        // Update HUD elements
        OnPrimaryHandLevelChangedForHUD?.Invoke(primaryHandLevel);
        OnSecondaryHandLevelChangedForHUD?.Invoke(secondaryHandLevel);
        OnPrimaryHandGemsChangedForHUD?.Invoke(gemsCollectedForPrimaryHand, GetGemsNeededForNext_AutoLevel_Threshold(true));
        OnSecondaryHandGemsChangedForHUD?.Invoke(gemsCollectedForSecondaryHand, GetGemsNeededForNext_AutoLevel_Threshold(false));
        
    }

    public void NotifySecondaryHandStatusChanged()
    {
        PlayerShooterOrchestrator.Instance?.HandleSecondaryHandLevelChanged(secondaryHandLevel);
        OnSecondaryHandLevelChangedForHUD?.Invoke(secondaryHandLevel);
        OnSecondaryHandGemsChangedForHUD?.Invoke(gemsCollectedForSecondaryHand, GetGemsNeededForNext_AutoLevel_Threshold(false));
        PlayerOverheatManager.Instance?.ResetHandHeat(false, true);
    }

    public bool TryAttractClosestGem(bool forPrimaryHandProgression)
    {
        if (gemLayerMaskForCollection == 0) return false;
        Transform centerForOverlap = _celestialDriftController != null ? _celestialDriftController.transform : transform;

        Collider[] hitColliders = Physics.OverlapSphere(centerForOverlap.position, gemCollectionRadius, gemLayerMaskForCollection);
        if (hitColliders.Length == 0) return false;

        Gem closestGem = null;
        float closestDistanceSqr = Mathf.Infinity;
        foreach (Collider hitCollider in hitColliders)
        {
            if (hitCollider.GetComponent<BossGem>() != null) continue;
            Gem gem = hitCollider.GetComponent<Gem>();
            if (gem != null && gem.IsDetached() && !gem.IsCollected() && !gem.IsBeingAttracted())
            {
                float distanceSqr = (centerForOverlap.position - gem.transform.position).sqrMagnitude;
                if (distanceSqr < closestDistanceSqr)
                {
                    closestDistanceSqr = distanceSqr;
                    closestGem = gem;
                }
            }
        }
        if (closestGem != null)
        {
            closestGem.StartAttractionToPlayer(centerForOverlap, forPrimaryHandProgression);
            return true;
        }
        return false;
    }

    private void PlayLevelUpEffect(bool isPrimaryHand)
    {
        if (levelUpEffectPrefab == null) return;

        Vector3 spawnPosition = Vector3.zero;
        Quaternion spawnRotation = Quaternion.identity;

        // 1. Attempt to get the hand's emit point if that's the desired spawn location
        if (levelUpEffectSpawnType == LevelUpEffectSpawnLocation.AtHandEmitPoint)
        {
            if (PlayerShooterOrchestrator.Instance != null)
            {
                HandFiringMechanics handMechanics = isPrimaryHand
                    ? PlayerShooterOrchestrator.Instance.primaryHandMechanics
                    : PlayerShooterOrchestrator.Instance.secondaryHandMechanics;

                if (handMechanics != null && handMechanics.emitPoint != null)
                {
                    spawnPosition = handMechanics.emitPoint.position;
                    spawnRotation = handMechanics.emitPoint.rotation;
                    // Spawn and exit early if we found the hand
                    GameObject effect = Instantiate(levelUpEffectPrefab, spawnPosition, spawnRotation);
                    effect.transform.localScale = levelUpEffectScale;
                    Destroy(effect, levelUpEffectDuration);
                    return;
                }
            }
        }

        // 2. If we're here, we're either spawning on the ground or the hand emit point was not found (fallback).
        Transform playerTransform = (_celestialDriftController != null) ? _celestialDriftController.transform : transform;
        if (Physics.Raycast(playerTransform.position, Vector3.down, out RaycastHit hit, groundRaycastDistance, groundLayerMask))
        {
            spawnPosition = hit.point;
            spawnRotation = Quaternion.LookRotation(Vector3.up, playerTransform.forward);
        }
        else
        { 
            // Ultimate fallback if raycast fails
            Transform fallback = levelUpEffectSpawnPointFallback ?? playerTransform;
            spawnPosition = fallback.position;
            spawnRotation = fallback.rotation;
        }

        // 3. Instantiate and Destroy the effect
        GameObject finalEffect = Instantiate(levelUpEffectPrefab, spawnPosition, spawnRotation);
        finalEffect.transform.localScale = levelUpEffectScale;
        Destroy(finalEffect, levelUpEffectDuration);
    }

    public float GetCurrentGemAttractionSpeedMultiplier()
    {
        if (PlayerShooterOrchestrator.Instance != null)
        {
            HandLevelSO primaryHandConfig = PlayerShooterOrchestrator.Instance.GetCurrentHandConfig(true);
            if (primaryHandConfig != null) return primaryHandConfig.gemAttractionSpeedMultiplier;
        }
        return 1.0f;
    }

    public void UpgradeHandsToMaxByPowerUp()
    {
        bool primaryUpgraded = false; bool secondaryUpgraded = false; int targetMaxLevel = maxHandLevel;
        if (primaryHandLevel < targetMaxLevel)
        {
            primaryHandLevel = targetMaxLevel; gemsCollectedForPrimaryHand = 0;
            PlayerShooterOrchestrator.Instance?.HandlePrimaryHandLevelChanged(primaryHandLevel);
            OnPrimaryHandLevelChangedForHUD?.Invoke(primaryHandLevel);
            OnPrimaryHandGemsChangedForHUD?.Invoke(gemsCollectedForPrimaryHand, 0);
            PlayerOverheatManager.Instance?.ResetHandHeat(true, true); PlayLevelUpEffect(true); primaryUpgraded = true;
            DynamicPlayerFeedManager.Instance?.ShowHandLevelUp(true, primaryHandLevel);
        }
        if (secondaryHandLevel < targetMaxLevel)
        {
            secondaryHandLevel = targetMaxLevel; gemsCollectedForSecondaryHand = 0;
            PlayerShooterOrchestrator.Instance?.HandleSecondaryHandLevelChanged(secondaryHandLevel);
            OnSecondaryHandLevelChangedForHUD?.Invoke(secondaryHandLevel);
            OnSecondaryHandGemsChangedForHUD?.Invoke(gemsCollectedForSecondaryHand, 0);
            PlayerOverheatManager.Instance?.ResetHandHeat(false, true); PlayLevelUpEffect(false); secondaryUpgraded = true;
            DynamicPlayerFeedManager.Instance?.ShowHandLevelUp(false, secondaryHandLevel);
        }
        if (primaryUpgraded || secondaryUpgraded)
        {
        }
        else
        {
        }
    }

    public void DEBUG_AdminSetHandLevel(bool isPrimary, int newLevel)
    {
        newLevel = Mathf.Clamp(newLevel, 1, maxHandLevel);

        if (isPrimary)
        {
            if (primaryHandLevel == newLevel) return; // No change
            int oldLevel = primaryHandLevel;

            primaryHandLevel = newLevel;
            gemsCollectedForPrimaryHand = 0; // Reset gem count for the new level

            // Notify HUD and other systems
            OnPrimaryHandLevelChangedForHUD?.Invoke(primaryHandLevel);
            OnPrimaryHandGemsChangedForHUD?.Invoke(gemsCollectedForPrimaryHand, GetGemsNeededForNext_AutoLevel_Threshold(true));

            // Update Hand Firing Mechanics
            if (PlayerShooterOrchestrator.Instance != null)
            {
                PlayerShooterOrchestrator.Instance.HandlePrimaryHandLevelChanged(primaryHandLevel);
            }
            
            // UPDATE HAND VISUALS: Change holographic appearance
            handAnimationController?.OnHandLevelChanged(true, primaryHandLevel);

            // Play effects if it's an upgrade
            if (newLevel > oldLevel)
            {
                PlayLevelUpEffect(true);
                PlayHandUpgradeSound(true, newLevel);
            }
        }
        else // Secondary Hand
        {
            if (secondaryHandLevel == newLevel) return; // No change
            int oldLevel = secondaryHandLevel;

            secondaryHandLevel = newLevel;
            gemsCollectedForSecondaryHand = 0; // Reset gem count for the new level

            // Notify HUD and other systems
            OnSecondaryHandLevelChangedForHUD?.Invoke(secondaryHandLevel);
            OnSecondaryHandGemsChangedForHUD?.Invoke(gemsCollectedForSecondaryHand, GetGemsNeededForNext_AutoLevel_Threshold(false));

            // Update Hand Firing Mechanics
            if (PlayerShooterOrchestrator.Instance != null)
            {
                PlayerShooterOrchestrator.Instance.HandleSecondaryHandLevelChanged(secondaryHandLevel);
            }
            
            // UPDATE HAND VISUALS: Change holographic appearance
            handAnimationController?.OnHandLevelChanged(false, secondaryHandLevel);

            // Play effects if it's an upgrade
            if (newLevel > oldLevel)
            {
                PlayLevelUpEffect(false);
                // Don't auto-activate double gems - let inventory system handle it
                // ActivateDoubleGems(10f);
                PlayHandUpgradeSound(false, newLevel);
            }
        }
    }

    public void ActivateDoubleGems(float duration)
    {
        if (IsDoubleGemsActive)
        {
            // Extend duration if already active
            _doubleGemsEndTime = Time.time + duration;
            return;
        }

        IsDoubleGemsActive = true;
        if (_doubleGemsCoroutine != null) StopCoroutine(_doubleGemsCoroutine);
        
        // Start DoubleGemsTracker
        if (DoubleGemsTracker.Instance != null)
        {
            DoubleGemsTracker.Instance.StartDoubleGemsTracking(duration);
        }
        
        // Notify PlayerHealth to activate particle effects
        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.ActivatePowerupEffect(PowerUpType.DoubleGems, duration);
        }
        
        // Set end time and start coroutine
        _doubleGemsEndTime = Time.time + duration;
        if (_doubleGemsCoroutine != null) { StopCoroutine(_doubleGemsCoroutine); _doubleGemsCoroutine = null; }
        _doubleGemsCoroutine = StartCoroutine(DoubleGemsDurationCoroutine(duration));
        
        // Show activation message
        DynamicPlayerFeedManager.Instance?.ShowPowerUpCollected(PowerUpType.DoubleGems);
        
        // Check if we need to restart the coroutine
        if (_doubleGemsCoroutine == null)
        {
            _doubleGemsEndTime = Time.time + duration;
            _doubleGemsCoroutine = StartCoroutine(DoubleGemsDurationCoroutine(duration));
        }
    }

    private IEnumerator DoubleGemsDurationCoroutine(float activeDuration)
    {
        float timer = 0f;
        float lastUpdateTime = 0f;
        const float UPDATE_INTERVAL = 1.0f; // Update HUD every 1 second instead of every frame
        
        while (timer < activeDuration)
        {
            float timeLeft = activeDuration - timer;
            
            // PERFORMANCE FIX: Only update HUD every 1 second, not every frame
            if (timer - lastUpdateTime >= UPDATE_INTERVAL || timer == 0f)
            {
                OnPowerUpStatusChangedForHUD?.Invoke(PowerUpType.DoubleGems, true, timeLeft);
                lastUpdateTime = timer;
            }

            if (timeLeft <= 5.0f)
            {
                DynamicPlayerFeedManager.Instance?.ShowPowerUpEndingSoon(PowerUpType.DoubleGems);
            }

            timer += Time.unscaledDeltaTime;
            yield return null;
        }
        DeactivateDoubleGems();
    }

    private void DeactivateDoubleGems()
    {
        if (!IsDoubleGemsActive) return;
        IsDoubleGemsActive = false;

        // Stop DoubleGemsTracker (this will grant bonus gems)
        if (DoubleGemsTracker.Instance != null)
        {
            DoubleGemsTracker.Instance.StopDoubleGemsTracking();
        }

        // Notify PlayerHealth to deactivate particle effects
        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.DeactivatePowerupEffect(PowerUpType.DoubleGems);
        }

        OnPowerUpStatusChangedForHUD?.Invoke(PowerUpType.DoubleGems, false, 0);
        DynamicPlayerFeedManager.Instance?.ShowPowerUpEnded(PowerUpType.DoubleGems);
        if (_doubleGemsCoroutine != null) { StopCoroutine(_doubleGemsCoroutine); _doubleGemsCoroutine = null; }
    }

    // God Mode functionality
    public bool IsGodModeActive { get; private set; } = false;
    private Coroutine _godModeCoroutine;
    private GameObject _godModeEffectsParent;
    
    [Header("God Mode References")]
    [SerializeField] private PlayerHealth playerHealthRef;
    
    // Cache PlayerHealth reference for reliable access
    private PlayerHealth _cachedPlayerHealth;

    /// <summary>
    /// Cache PlayerHealth reference for reliable access during godmode operations
    /// </summary>
    private void CachePlayerHealthReference()
    {
        // Try assigned reference first
        if (playerHealthRef != null)
        {
            _cachedPlayerHealth = playerHealthRef;
            Debug.Log("[PlayerProgression] Using assigned PlayerHealth reference for godmode", this);
            return;
        }
        
        // Fallback to finding PlayerHealth component
        _cachedPlayerHealth = FindObjectOfType<PlayerHealth>();
        if (_cachedPlayerHealth != null)
        {
            Debug.Log("[PlayerProgression] Found PlayerHealth component for godmode", this);
        }
        else
        {
            Debug.LogError("[PlayerProgression] Could not find PlayerHealth component! Godmode will not work properly.", this);
        }
    }
    
    public void SetGodModeEffectsParent(GameObject effectsParent)
    {
        _godModeEffectsParent = effectsParent;
        
        // Ensure effects parent is disabled by default
        if (_godModeEffectsParent != null)
        {
            _godModeEffectsParent.SetActive(false);
        }
    }

    public void ActivateGodMode(float duration)
    {
        Debug.Log($"[PlayerProgression] ActivateGodMode called with duration: {duration}s", this);
        
        // Ensure we have a valid PlayerHealth reference
        if (_cachedPlayerHealth == null)
        {
            CachePlayerHealthReference();
        }
        
        if (IsGodModeActive)
        {
            // Extend duration if already active
            Debug.Log("[PlayerProgression] Extending existing godmode duration", this);
            if (_godModeCoroutine != null)
            {
                StopCoroutine(_godModeCoroutine);
            }
            
            // CRITICAL FIX: Also extend PlayerHealth godmode duration when extending
            if (_cachedPlayerHealth != null)
            {
                _cachedPlayerHealth.ActivateGodMode(duration);
                Debug.Log($"[PlayerProgression] Extended PlayerHealth godmode duration. PlayerHealth.IsGodModeActive = {_cachedPlayerHealth.IsGodModeActive}", this);
            }
        }
        else
        {
            IsGodModeActive = true;
            Debug.Log("[PlayerProgression] Activating godmode - setting IsGodModeActive = true", this);
            
            // Enable the effects parent (activates all child effects)
            if (_godModeEffectsParent != null)
            {
                _godModeEffectsParent.SetActive(true);
                Debug.Log("[PlayerProgression] Enabled godmode effects parent", this);
            }
            
            // CRITICAL: Notify PlayerHealth to activate its invincibility and particle effects
            if (_cachedPlayerHealth != null)
            {
                _cachedPlayerHealth.ActivateGodMode(duration);
                Debug.Log($"[PlayerProgression] Successfully activated PlayerHealth godmode. PlayerHealth.IsGodModeActive = {_cachedPlayerHealth.IsGodModeActive}", this);
                
                // ADDITIONAL DEBUG: Check if particle effect is assigned
                if (_cachedPlayerHealth.HasGodModeParticleEffect)
                {
                    Debug.Log("[PlayerProgression] PlayerHealth has godmode particle effect assigned", this);
                }
                else
                {
                    Debug.LogError("[PlayerProgression] PlayerHealth godmode particle effect is NOT assigned! Please assign it in the inspector.", this);
                }
            }
            else
            {
                Debug.LogError("[PlayerProgression] Failed to activate godmode - PlayerHealth reference is null!", this);
                
                // FALLBACK: Try to find PlayerHealth one more time
                _cachedPlayerHealth = FindObjectOfType<PlayerHealth>();
                if (_cachedPlayerHealth != null)
                {
                    Debug.Log("[PlayerProgression] Found PlayerHealth via fallback search - activating godmode", this);
                    _cachedPlayerHealth.ActivateGodMode(duration);
                }
                else
                {
                    Debug.LogError("[PlayerProgression] CRITICAL: Could not find PlayerHealth component anywhere! Godmode particle effects will not work.", this);
                }
            }
        }

        _godModeCoroutine = StartCoroutine(GodModeDurationCoroutine(duration));
        
        // Show activation message
        DynamicPlayerFeedManager.Instance?.ShowPowerUpCollected(PowerUpType.GodMode);
    }


    private IEnumerator GodModeDurationCoroutine(float activeDuration)
    {
        float timer = 0f;
        float lastUpdateTime = 0f;
        const float UPDATE_INTERVAL = 1.0f; // Update HUD every 1 second instead of every frame
        
        while (timer < activeDuration)
        {
            float timeLeft = activeDuration - timer;
            
            // PERFORMANCE FIX: Only update HUD every 1 second, not every frame
            if (timer - lastUpdateTime >= UPDATE_INTERVAL || timer == 0f)
            {
                OnPowerUpStatusChangedForHUD?.Invoke(PowerUpType.GodMode, true, timeLeft);
                lastUpdateTime = timer;
            }

            if (timeLeft <= 5.0f)
            {
                DynamicPlayerFeedManager.Instance?.ShowPowerUpEndingSoon(PowerUpType.GodMode);
            }

            timer += Time.unscaledDeltaTime;
            yield return null;
        }
        DeactivateGodMode();
    }

    private void DeactivateGodMode()
    {
        if (!IsGodModeActive) return;
        
        Debug.Log("[PlayerProgression] Deactivating godmode", this);
        IsGodModeActive = false;

        // Disable the effects parent (deactivates all child effects)
        if (_godModeEffectsParent != null)
        {
            _godModeEffectsParent.SetActive(false);
            Debug.Log("[PlayerProgression] Disabled godmode effects parent", this);
        }

        // Notify PlayerHealth to deactivate its invincibility and particle effects
        if (_cachedPlayerHealth != null)
        {
            // PlayerHealth will handle its own deactivation through its coroutine
            // No need for complex reflection - just let it finish naturally
            Debug.Log($"[PlayerProgression] PlayerHealth godmode will deactivate naturally. Current state: {_cachedPlayerHealth.IsGodModeActive}", this);
        }
        else
        {
            Debug.LogWarning("[PlayerProgression] PlayerHealth reference is null during godmode deactivation", this);
        }

        OnPowerUpStatusChangedForHUD?.Invoke(PowerUpType.GodMode, false, 0);
        DynamicPlayerFeedManager.Instance?.ShowPowerUpEnded(PowerUpType.GodMode);
        if (_godModeCoroutine != null) { StopCoroutine(_godModeCoroutine); _godModeCoroutine = null; }
    }

    // Max Hand Upgrade functionality
    public bool IsMaxHandUpgradeActive { get; private set; } = false;
    private Coroutine _maxHandUpgradeCoroutine;
    private int _originalPrimaryHandLevel;
    private int _originalSecondaryHandLevel;

    public void ActivateMaxHandUpgrade(float duration)
    {
        if (IsMaxHandUpgradeActive)
        {
            // Extend duration if already active
            if (_maxHandUpgradeCoroutine != null)
            {
                StopCoroutine(_maxHandUpgradeCoroutine);
            }
        }
        else
        {
            IsMaxHandUpgradeActive = true;
            
            // Store original hand levels
            _originalPrimaryHandLevel = primaryHandLevel;
            _originalSecondaryHandLevel = secondaryHandLevel;
            
            // Set both hands to level 4
            DEBUG_AdminSetHandLevel(true, 4);  // Primary hand to level 4
            DEBUG_AdminSetHandLevel(false, 4); // Secondary hand to level 4
            
            // Notify PlayerHealth to activate particle effects
            PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.ActivatePowerupEffect(PowerUpType.MaxHandUpgrade, duration);
            }
        }

        _maxHandUpgradeCoroutine = StartCoroutine(MaxHandUpgradeDurationCoroutine(duration));
        
        // Show activation message
        DynamicPlayerFeedManager.Instance?.ShowPowerUpCollected(PowerUpType.MaxHandUpgrade);
    }

    private IEnumerator MaxHandUpgradeDurationCoroutine(float activeDuration)
    {
        float timer = 0f;
        float lastUpdateTime = 0f;
        const float UPDATE_INTERVAL = 1.0f; // Update HUD every 1 second instead of every frame
        
        while (timer < activeDuration)
        {
            float timeLeft = activeDuration - timer;
            
            // PERFORMANCE FIX: Only update HUD every 1 second, not every frame
            if (timer - lastUpdateTime >= UPDATE_INTERVAL || timer == 0f)
            {
                OnPowerUpStatusChangedForHUD?.Invoke(PowerUpType.MaxHandUpgrade, true, timeLeft);
                lastUpdateTime = timer;
            }

            if (timeLeft <= 5.0f)
            {
                DynamicPlayerFeedManager.Instance?.ShowPowerUpEndingSoon(PowerUpType.MaxHandUpgrade);
            }

            timer += Time.unscaledDeltaTime;
            yield return null;
        }
        DeactivateMaxHandUpgrade();
    }

    private void DeactivateMaxHandUpgrade()
    {
        if (!IsMaxHandUpgradeActive) return;
        IsMaxHandUpgradeActive = false;

        // Restore original hand levels
        DEBUG_AdminSetHandLevel(true, _originalPrimaryHandLevel);  // Restore primary hand
        DEBUG_AdminSetHandLevel(false, _originalSecondaryHandLevel); // Restore secondary hand

        // Deactivate particle effects
        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.DeactivatePowerupEffect(PowerUpType.MaxHandUpgrade);
        }

        OnPowerUpStatusChangedForHUD?.Invoke(PowerUpType.MaxHandUpgrade, false, 0);
        DynamicPlayerFeedManager.Instance?.ShowPowerUpEnded(PowerUpType.MaxHandUpgrade);
        if (_maxHandUpgradeCoroutine != null) { StopCoroutine(_maxHandUpgradeCoroutine); _maxHandUpgradeCoroutine = null; }
    }

    public void ActivateSlowTime(float duration, float scaleFactor, AudioClip startSound_oneshot, AudioClip endSound_oneshot, float soundVolume_oneshot)
    {
        if (SlowTimePowerUp.isTimeSlowActive) return;
        if (_slowTimeCoroutine != null) StopCoroutine(_slowTimeCoroutine);
        
        // Notify PlayerHealth to activate particle effects
        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.ActivatePowerupEffect(PowerUpType.SlowTime, duration);
        }
        
        _slowTimeCoroutine = StartCoroutine(SlowTimeRoutine(duration, scaleFactor, startSound_oneshot, endSound_oneshot, soundVolume_oneshot));
    }

    private IEnumerator SlowTimeRoutine(float duration, float scaleFactor, AudioClip startSound_oneshot, AudioClip endSound_oneshot, float soundVolume_oneshot)
    {
        Debug.Log($"[SlowTime] ========== COROUTINE STARTED ==========", this);
        Debug.Log($"[SlowTime] Duration: {duration}s, Scale: {scaleFactor}", this);
        Debug.Log($"[SlowTime] Time.timeScale BEFORE: {Time.timeScale}", this);
        Debug.Log($"[SlowTime] GameObject active: {gameObject.activeInHierarchy}, enabled: {enabled}", this);
        
        SlowTimePowerUp.SetTimeSlowActive(true);
        Time.timeScale = scaleFactor;
        if (SlowTimePowerUp.originalFixedDeltaTime > 0) Time.fixedDeltaTime = SlowTimePowerUp.originalFixedDeltaTime * scaleFactor;
        else { SlowTimePowerUp.originalFixedDeltaTime = Time.fixedDeltaTime; Time.fixedDeltaTime *= scaleFactor; }

        Debug.Log($"[SlowTime] Time.timeScale AFTER setting: {Time.timeScale}", this);

        if (startSound_oneshot != null) GameSounds.PlayPowerUpStart(transform.position, soundVolume_oneshot);

        // CRITICAL FIX: Use unscaled time for duration tracking to prevent interference with Time.timeScale
        float realTimeElapsed = 0f;
        float lastUpdateTime = 0f;
        const float UPDATE_INTERVAL = 1.0f; // Update HUD every 1 second instead of every frame
        int frameCount = 0;
        
        Debug.Log($"[SlowTime] Starting while loop - will run until {duration} seconds elapsed", this);
        
        while (realTimeElapsed < duration)
        {
            float deltaTime = Time.unscaledDeltaTime;
            realTimeElapsed += deltaTime;
            float timeLeft = duration - realTimeElapsed;
            
            frameCount++;
            
            // Log every 60 frames to avoid spam
            if (frameCount % 60 == 0)
            {
                Debug.Log($"[SlowTime] Frame {frameCount}: Elapsed={realTimeElapsed:F2}s, TimeLeft={timeLeft:F2}s, DeltaTime={deltaTime:F4}s, TimeScale={Time.timeScale}", this);
            }
            
            // PERFORMANCE FIX: Only update HUD every 1 second, not every frame
            if (realTimeElapsed - lastUpdateTime >= UPDATE_INTERVAL || realTimeElapsed == 0f)
            {
                OnPowerUpStatusChangedForHUD?.Invoke(PowerUpType.SlowTime, true, timeLeft);
                lastUpdateTime = realTimeElapsed;
            }

            if (timeLeft <= 5.0f && timeLeft > 4.9f)
            {
                Debug.Log($"[SlowTime] Showing ending soon warning at {timeLeft:F2}s remaining", this);
                DynamicPlayerFeedManager.Instance?.ShowPowerUpEndingSoon(PowerUpType.SlowTime);
            }
            
            yield return null; // CHANGED FROM WaitForEndOfFrame to null for better reliability
        }

        Debug.Log($"[SlowTime] ========== WHILE LOOP COMPLETED ==========", this);
        Debug.Log($"[SlowTime] Total frames: {frameCount}, Total time: {realTimeElapsed:F2}s", this);
        Debug.Log($"[SlowTime] NOW RESTORING Time.timeScale from {Time.timeScale} to 1.0f", this);
        
        // CRITICAL FIX: Always restore Time.timeScale to 1.0f regardless of what happened during the powerup
        Time.timeScale = 1.0f;
        SlowTimePowerUp.RestoreOriginalFixedDeltaTime();
        SlowTimePowerUp.SetTimeSlowActive(false);

        Debug.Log($"[SlowTime] Time.timeScale restored to: {Time.timeScale}", this);

        // Notify PlayerHealth to deactivate particle effects
        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.DeactivatePowerupEffect(PowerUpType.SlowTime);
            Debug.Log($"[SlowTime] Deactivated particle effects", this);
        }

        if (endSound_oneshot != null) GameSounds.PlayPowerUpEnd(transform.position, soundVolume_oneshot);
        OnPowerUpStatusChangedForHUD?.Invoke(PowerUpType.SlowTime, false, 0);
        DynamicPlayerFeedManager.Instance?.ShowPowerUpEnded(PowerUpType.SlowTime);
        _slowTimeCoroutine = null;
        
        Debug.Log($"[SlowTime] ========== COROUTINE FULLY COMPLETED ==========", this);
    }

    void OnDestroy()
    {
        Debug.Log("[PlayerProgression] OnDestroy called - Cleaning up powerup states", this);
        
        if (IsDoubleGemsActive) DeactivateDoubleGems();
        
        // CRITICAL FIX: Always ensure Time.timeScale is reset to 1.0f on scene transitions
        if (SlowTimePowerUp.isTimeSlowActive || Time.timeScale != 1.0f)
        {
            Debug.Log($"[PlayerProgression] OnDestroy - Resetting Time.timeScale from {Time.timeScale} to 1.0f", this);
            Time.timeScale = 1.0f;
            SlowTimePowerUp.RestoreOriginalFixedDeltaTime();
            SlowTimePowerUp.SetTimeSlowActive(false);
        }
        
        // Stop any active SlowTime coroutine
        if (_slowTimeCoroutine != null)
        {
            StopCoroutine(_slowTimeCoroutine);
            _slowTimeCoroutine = null;
            Debug.Log("[PlayerProgression] OnDestroy - Stopped active SlowTime coroutine", this);
        }
        
        // HAND LEVEL PERSISTENCE: Save current hand levels on scene transition/destroy
        if (HandLevelPersistenceManager.Instance != null)
        {
            HandLevelPersistenceManager.Instance.UpdateStoredHandLevels(primaryHandLevel, secondaryHandLevel);
        }
    }

    public void AttemptDoubleClickCollection(bool isPrimaryHand)
{
    Debug.Log($"[PlayerProgression] AttemptDoubleClickCollection called for {(isPrimaryHand ? "PRIMARY" : "SECONDARY")} hand");
    
    // =================== GEM COLLECTION ONLY ===================
    // NOTE: Powerup collection now uses collision/E key interaction system
    // See PowerUp.cs OnTriggerEnter/Stay for new pickup system
    
    // Find all gems in the scene and check their individual attraction ranges
    Gem[] allGems = FindObjectsByType<Gem>(FindObjectsSortMode.None);
    Debug.Log($"[PlayerProgression] Found {allGems.Length} gems in scene");
    
    int gemsInRange = 0;
    int gemsAttempted = 0;
    
    foreach (Gem gem in allGems)
    {
        if (gem != null && gem.IsDetached() && !gem.IsCollected() && !gem.IsBeingAttracted())
        {
            // Check if player is within this gem's individual attraction range
            float distanceToGem = Vector3.Distance(transform.position, gem.transform.position);
            Debug.Log($"[PlayerProgression] Gem at distance {distanceToGem:F2}, attraction range: {gem.attractionRange}");
            
            if (distanceToGem <= gem.attractionRange)
            {
                gemsInRange++;
                gemsAttempted++;
                Debug.Log($"[PlayerProgression] Starting attraction for gem at {gem.transform.position}");
                gem.StartAttractionToPlayer(transform, isPrimaryHand);
            }
            else
            {
                Debug.Log($"[PlayerProgression] Gem too far away: {distanceToGem:F2} > {gem.attractionRange}");
            }
        }
        else if (gem != null)
        {
            Debug.Log($"[PlayerProgression] Gem skipped - Detached: {gem.IsDetached()}, Collected: {gem.IsCollected()}, BeingAttracted: {gem.IsBeingAttracted()}");
        }
    }
    
    Debug.Log($"[PlayerProgression] Gem collection attempt complete - {gemsInRange} gems in range, {gemsAttempted} attempted");
}

    private IEnumerator UnduckAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (PlayerShooterOrchestrator.Instance != null)
        {
            PlayerShooterOrchestrator.Instance.DuckHandStreamVolumes(false);
        }
    }
}