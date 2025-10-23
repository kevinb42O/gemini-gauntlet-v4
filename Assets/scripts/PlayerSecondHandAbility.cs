// --- PlayerSecondHandAbility.cs (FULL & FINAL - Corrected for API Changes) ---
using UnityEngine;
using System;

public class PlayerSecondHandAbility : MonoBehaviour
{
    public static PlayerSecondHandAbility Instance { get; private set; }

    public enum SecondHandRunStatus { Locked, Unlocked }
    public SecondHandRunStatus CurrentRunStatus { get; private set; } = SecondHandRunStatus.Unlocked;

    [Header("Unlock Requirements for Current Run")]
    [Tooltip("Gems collected by the RIGHT/SECONDARY hand needed to unlock this ability for the current run.")]
    public int gemsNeededForInitialUnlock = 2;

    private int _gemsCollectedBySecondaryHandForUnlock = 0;

    public static event Action<SecondHandRunStatus, int, int> OnSecondHandStatusChanged;

    void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        LoadGameStartState();
    }

    /// <summary>
    /// Load game start state - MODIFIED: Always start with both hands unlocked at level 1
    /// </summary>
    public void LoadGameStartState()
    {
        // GAME DESIGN CHANGE: Always start with both hands unlocked
        Debug.Log("👍 PlayerSecondHandAbility: Starting with both hands unlocked (new game design)");
        
        // Set to unlocked state
        CurrentRunStatus = SecondHandRunStatus.Unlocked;
        _gemsCollectedBySecondaryHandForUnlock = gemsNeededForInitialUnlock; // Set to required amount
        
        // Notify systems that secondary hand is unlocked
        OnSecondHandStatusChanged?.Invoke(CurrentRunStatus, _gemsCollectedBySecondaryHandForUnlock, gemsNeededForInitialUnlock);
        PlayerProgression.Instance?.NotifySecondaryHandStatusChanged();
        
        // Save unlock state to persistence manager
        HandLevelPersistenceManager.Instance?.UpdateSecondHandUnlockState(true);
        
        Debug.Log("✅ PlayerSecondHandAbility: Both hands ready for gameplay!");
    }

    public void ResetForNewGame()
    {
        // GAME DESIGN CHANGE: Always start unlocked, never lock the second hand
        CurrentRunStatus = SecondHandRunStatus.Unlocked;
        _gemsCollectedBySecondaryHandForUnlock = gemsNeededForInitialUnlock; // Set to required amount
        OnSecondHandStatusChanged?.Invoke(CurrentRunStatus, _gemsCollectedBySecondaryHandForUnlock, gemsNeededForInitialUnlock);
        PlayerProgression.Instance?.NotifySecondaryHandStatusChanged();
        
        // SAVE UNLOCKED STATE TO PERSISTENCE MANAGER
        HandLevelPersistenceManager.Instance?.UpdateSecondHandUnlockState(true);
    }

    public void RegisterSecondaryHandGemCollectionForAbility()
    {
        if (CurrentRunStatus == SecondHandRunStatus.Locked)
        {
            _gemsCollectedBySecondaryHandForUnlock++;
            OnSecondHandStatusChanged?.Invoke(CurrentRunStatus, _gemsCollectedBySecondaryHandForUnlock, gemsNeededForInitialUnlock);

            if (_gemsCollectedBySecondaryHandForUnlock >= gemsNeededForInitialUnlock)
            {
                UnlockSecondaryHand(false); // Not forced unlock
            }
        }
    }

    private void UnlockSecondaryHand(bool isForced)
    {
        CurrentRunStatus = SecondHandRunStatus.Unlocked;
        OnSecondHandStatusChanged?.Invoke(CurrentRunStatus, _gemsCollectedBySecondaryHandForUnlock, gemsNeededForInitialUnlock);
        PlayerProgression.Instance?.NotifySecondaryHandStatusChanged(); // This will trigger PSO to update hand, and PP to update HUD

        // SAVE UNLOCK STATE TO PERSISTENCE MANAGER
        HandLevelPersistenceManager.Instance?.UpdateSecondHandUnlockState(true);

        if (isForced)
        {
            Debug.Log("PlayerSecondHandAbility: Second Hand FORCE UNLOCKED (e.g., by cheat/purchase).");
            // --- CORRECTED ---
            // The purchase logic in PlayerProgression now calls ShowSecondaryHandUnlocked. This is for other forced unlocks.
            DynamicPlayerFeedManager.Instance?.ShowSecondaryHandUnlocked();
        }
        else
        {
            Debug.Log("PlayerSecondHandAbility: Second Hand UNLOCKED for this run!");
            DynamicPlayerFeedManager.Instance?.ShowSecondaryHandUnlocked();
        }
    }

    public void ForceUnlockForRun()
    {
        if (CurrentRunStatus == SecondHandRunStatus.Locked)
        {
            _gemsCollectedBySecondaryHandForUnlock = gemsNeededForInitialUnlock;
            UnlockSecondaryHand(true); // Is a forced unlock
        }
    }

    public bool IsSecondHandUnlockedForRun()
    {
        return CurrentRunStatus == SecondHandRunStatus.Unlocked;
    }
}