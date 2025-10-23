// --- MaxHandUpgradePowerUp.cs (Corrected and Improved) ---
using UnityEngine;
using GeminiGauntlet.Audio;
using System.Collections;

public class MaxHandUpgradePowerUp : PowerUp
{
    [Header("Max Hand Upgrade Settings")]
    [Tooltip("Duration in seconds for which the max hand upgrade is active.")]
    public float maxHandDuration = 15f;
    
    private Coroutine _maxHandCoroutine;
    private PlayerInputHandler _inputHandler;
    private int _storedPrimaryLevel;
    private int _storedSecondaryLevel;
    private bool _isPowerupActive = false;
    private bool _primaryHandUpgraded = false;
    private bool _secondaryHandUpgraded = false;
    protected override void Awake()
    {
        base.Awake();
        powerUpType = PowerUpType.MaxHandUpgrade;
        _inputHandler = PlayerInputHandler.Instance;
    }

    protected override void ApplyPowerUpEffect(PlayerProgression progression, PlayerShooterOrchestrator pso, PlayerAOEAbility aoeAbility, PlayerHealth playerHealth)
    {
        // Add to inventory system instead of auto-activating
        if (PowerupInventoryManager.Instance != null)
        {
            PowerupInventoryManager.Instance.AddPowerup(PowerUpType.MaxHandUpgrade, maxHandDuration);
            Debug.Log($"[MaxHandUpgradePowerUp] Added to inventory with {maxHandDuration}s duration", this);
        }
        
        // Provide clear player feedback via UI
        if (DynamicPlayerFeedManager.Instance != null)
        {
            DynamicPlayerFeedManager.Instance.ShowPowerUpCollected(powerUpType);
            
            // Show instruction message
            DynamicPlayerFeedManager.Instance.ShowCustomMessage(
                $"Max Hand Upgrade Ready! Scroll to select, Middle Click to activate",
                DynamicPlayerFeedManager.Instance.powerUpColor,
                DynamicPlayerFeedManager.Instance.feedIcons.GetIconForPowerUp(PowerUpType.MaxHandUpgrade),
                true, 3.0f);
        }
    }
    protected override float GetPowerupDuration()
    {
        return maxHandDuration;
    }

    // Public method for inventory system activation
    public void ActivateMaxHandUpgrade(PlayerProgression progression, PlayerShooterOrchestrator pso, PlayerHealth playerHealth)
    {
        if (progression == null)
        {
            Debug.LogError("[MaxHandUpgradePowerUp] PlayerProgression is null! Cannot activate MaxHandUpgrade.", this);
            return;
        }
        
        // Stop existing routine if running to prevent conflicts
        if (_maxHandCoroutine != null) 
        {
            Debug.Log("[MaxHandUpgradePowerUp] Stopping existing MaxHandUpgrade coroutine", this);
            StopCoroutine(_maxHandCoroutine);
            _maxHandCoroutine = null;
        }
        
        Debug.Log($"[MaxHandUpgradePowerUp] ACTIVATING MAX HAND POWERUP - Duration: {maxHandDuration}s", this);
        
        // Store previous hand levels BEFORE applying max level
        _storedPrimaryLevel = progression.primaryHandLevel;
        _storedSecondaryLevel = progression.secondaryHandLevel;
        
        // Check if secondary hand is unlocked
        bool isSecondHandUnlocked = progression.secondaryHandLevel > 0;
        
        // Reset tracking flags
        _primaryHandUpgraded = false;
        _secondaryHandUpgraded = false;
        _isPowerupActive = true;
        
        Debug.Log($"[MaxHandUpgradePowerUp] Stored levels - Primary: {_storedPrimaryLevel}, Secondary: {_storedSecondaryLevel}, SecondUnlocked: {isSecondHandUnlocked}", this);

        // Play powerup start sound
        try
        {
            GameSounds.PlayPowerUpStart(transform.position);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[MaxHandUpgradePowerUp] Failed to play start sound: {ex.Message}", this);
        }
        
        // IMMEDIATELY upgrade hands to max level
        if (_storedPrimaryLevel < progression.maxHandLevel)
        {
            progression.DEBUG_AdminSetHandLevel(true, progression.maxHandLevel);
            _primaryHandUpgraded = true;
            Debug.Log("[MaxHandUpgradePowerUp] Primary hand upgraded to MAX level", this);
        }
        
        if (isSecondHandUnlocked && _storedSecondaryLevel < progression.maxHandLevel)
        {
            progression.DEBUG_AdminSetHandLevel(false, progression.maxHandLevel);
            _secondaryHandUpgraded = true;
            Debug.Log("[MaxHandUpgradePowerUp] Secondary hand upgraded to MAX level", this);
        }
        
        // CENTRALIZED: Use PowerupEffectManager for consistent effect management
        if (PowerupEffectManager.Instance != null)
        {
            PowerupEffectManager.Instance.ActivatePowerupEffect(PowerUpType.MaxHandUpgrade, maxHandDuration);
            Debug.Log("[MaxHandUpgradePowerUp] Activated MaxHandUpgrade effects via PowerupEffectManager", this);
        }
        else
        {
            Debug.LogWarning("[MaxHandUpgradePowerUp] PowerupEffectManager.Instance is null - effects not activated", this);
        }

        // Show powerup message
        if (DynamicPlayerFeedManager.Instance != null)
        {
            DynamicPlayerFeedManager.Instance.ShowCustomMessage(
                $"Max Hand Power-Up: Hands maxed for {maxHandDuration} seconds!",
                DynamicPlayerFeedManager.Instance.powerUpColor,
                DynamicPlayerFeedManager.Instance.feedIcons.GetIconForPowerUp(PowerUpType.MaxHandUpgrade),
                true,
                maxHandDuration
            );
        }
        
        // Start the duration countdown with CONSISTENT time system
        _maxHandCoroutine = StartCoroutine(MaxHandDurationRoutine(progression));
    }

    private IEnumerator MaxHandDurationRoutine(PlayerProgression progression)
    {
        Debug.Log($"[MaxHandUpgradePowerUp] Started duration timer for {maxHandDuration} seconds (unscaled time)", this);
        
        // CRITICAL FIX: Use unscaled time to be consistent with other powerup timers
        float elapsed = 0f;
        while (elapsed < maxHandDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        
        Debug.Log($"[MaxHandUpgradePowerUp] Duration completed after {elapsed} seconds", this);

        
        // Early exit if the powerup got cancelled/reset somehow
        if (!_isPowerupActive)
        {
            Debug.Log("[MaxHandUpgradePowerUp] Powerup already deactivated, skipping reversion", this);
            yield break;
        }
        
        // Track mouse button states before stopping shooting
        bool primaryMouseHeld = false;
        bool secondaryMouseHeld = false;
        PlayerShooterOrchestrator pso = PlayerShooterOrchestrator.Instance;
        
        // Check if mouse buttons are held
        if (_inputHandler != null)
        {
            primaryMouseHeld = _inputHandler.IsPrimaryFireHeld();
            secondaryMouseHeld = _inputHandler.IsSecondaryFireHeld();
        }
        
        // Stop shooting to prevent conflicts during hand level change
        if (pso != null)
        {
            if (primaryMouseHeld)
            {
                pso.StopPrimaryHandShooting();
                Debug.Log("[MaxHandPowerUp] Stopped primary hand shooting for reversion");
            }
            
            if (secondaryMouseHeld && progression.secondaryHandLevel > 0)
            {
                pso.StopSecondaryHandShooting();
                Debug.Log("[MaxHandPowerUp] Stopped secondary hand shooting for reversion");
            }
            
            // Wait a frame to ensure shooting has stopped
            yield return null;
        }
        
        Debug.Log($"[MaxHandUpgradePowerUp] Reverting hand levels to - Primary: {_storedPrimaryLevel}, Secondary: {_storedSecondaryLevel}", this);
        
        // Play powerup end sound
        GameSounds.PlayPowerUpEnd(transform.position);
        
        // CENTRALIZED: Use PowerupEffectManager for consistent effect management
        if (PowerupEffectManager.Instance != null)
        {
            PowerupEffectManager.Instance.DeactivatePowerupEffect(PowerUpType.MaxHandUpgrade);
            Debug.Log("[MaxHandUpgradePowerUp] Deactivated MaxHandUpgrade effects via PowerupEffectManager", this);
        }
        
        // Revert ONLY the hands that were actually upgraded
        Debug.Log($"[MaxHandUpgradePowerUp] Reverting ONLY upgraded hands - Primary upgraded: {_primaryHandUpgraded}, Secondary upgraded: {_secondaryHandUpgraded}", this);
        
        if (_primaryHandUpgraded)
        {
            progression.DEBUG_AdminSetHandLevel(true, _storedPrimaryLevel);
            Debug.Log($"[MaxHandUpgradePowerUp] Primary hand reverted to level {_storedPrimaryLevel}", this);
        }
        
        if (_secondaryHandUpgraded)
        {
            progression.DEBUG_AdminSetHandLevel(false, _storedSecondaryLevel);
            Debug.Log($"[MaxHandUpgradePowerUp] Secondary hand reverted to level {_storedSecondaryLevel}", this);
        }
        
        // Reset powerup state
        _isPowerupActive = false;
        
        // Wait another frame for hand level change to process
        yield return null;
        
        // Resume shooting if mouse buttons are still held
        if (pso != null && _inputHandler != null)
        {
            if (primaryMouseHeld && _inputHandler.IsPrimaryFireHeld())
            {
                pso.StartPrimaryHandShooting();
                Debug.Log("[MaxHandPowerUp] Resumed primary hand shooting");
            }
            
            if (secondaryMouseHeld && progression.secondaryHandLevel > 0 && _inputHandler.IsSecondaryFireHeld())
            {
                pso.StartSecondaryHandShooting();
                Debug.Log("[MaxHandPowerUp] Resumed secondary hand shooting");
            }
        }

        // Show end message
        if (DynamicPlayerFeedManager.Instance != null)
        {
            DynamicPlayerFeedManager.Instance.ShowCustomMessage(
                "Max Hand Power-Up: Effect ended!",
                Color.yellow,
                DynamicPlayerFeedManager.Instance.feedIcons.GetIconForPowerUp(PowerUpType.MaxHandUpgrade),
                true,
                2.5f
            );
        }
        
        // FIXED: Removed self-removal - PowerupInventoryManager handles all removal via UpdateActivePowerupTimers()
        // This prevents race conditions when multiple MaxHandUpgrade powerups are active
    }
    
    private void OnDisable()
    {
        // CRITICAL: Clean up when object is disabled
        CleanupPowerup("OnDisable");
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy(); // Call base class cleanup for light
        
        // CRITICAL: Clean up when object is destroyed (even if already disabled)
        CleanupPowerup("OnDestroy");
    }
    
    /// <summary>
    /// EXPERT LEVEL: Centralized cleanup method to prevent memory leaks
    /// Called from both OnDisable() and OnDestroy() to cover all edge cases
    /// </summary>
    private void CleanupPowerup(string calledFrom)
    {
        Debug.Log($"[MaxHandUpgradePowerUp] CleanupPowerup called from {calledFrom}", this);
        
        // Stop coroutine if running
        if (_maxHandCoroutine != null)
        {
            StopCoroutine(_maxHandCoroutine);
            _maxHandCoroutine = null;
            Debug.Log($"[MaxHandUpgradePowerUp] Stopped coroutine in {calledFrom}", this);
        }
        
        // Force revert if powerup was active
        if (_isPowerupActive)
        {
            ForceRevertHandLevels(calledFrom);
        }
    }
    
    /// <summary>
    /// EXPERT LEVEL: Force revert hand levels and deactivate effects
    /// Ensures clean state even if object is destroyed mid-powerup
    /// </summary>
    private void ForceRevertHandLevels(string calledFrom)
    {
        Debug.Log($"[MaxHandUpgradePowerUp] ForceRevertHandLevels called from {calledFrom}", this);
        
        // Deactivate particle effects via centralized manager
        if (PowerupEffectManager.Instance != null)
        {
            PowerupEffectManager.Instance.DeactivatePowerupEffect(PowerUpType.MaxHandUpgrade);
            Debug.Log($"[MaxHandUpgradePowerUp] Deactivated effects via PowerupEffectManager in {calledFrom}", this);
        }
        else
        {
            // Fallback to direct deactivation if manager not available
            PlayerOverheatManager overheatManager = PlayerOverheatManager.Instance;
            if (overheatManager != null)
            {
                overheatManager.DeactivateMaxHandUpgradeEffects();
                Debug.Log($"[MaxHandUpgradePowerUp] Deactivated effects via PlayerOverheatManager in {calledFrom}", this);
            }
        }
        
        // Revert hand levels
        PlayerProgression progression = PlayerProgression.Instance;
        if (progression != null)
        {
            Debug.Log($"[MaxHandUpgradePowerUp] Reverting hands in {calledFrom} - Primary: {_storedPrimaryLevel}, Secondary: {_storedSecondaryLevel}", this);
            
            // Only revert hands that were actually upgraded
            if (_primaryHandUpgraded)
            {
                progression.DEBUG_AdminSetHandLevel(true, _storedPrimaryLevel);
                Debug.Log($"[MaxHandUpgradePowerUp] Primary hand reverted to {_storedPrimaryLevel}", this);
            }
            
            if (_secondaryHandUpgraded)
            {
                progression.DEBUG_AdminSetHandLevel(false, _storedSecondaryLevel);
                Debug.Log($"[MaxHandUpgradePowerUp] Secondary hand reverted to {_storedSecondaryLevel}", this);
            }
        }
        else
        {
            Debug.LogWarning($"[MaxHandUpgradePowerUp] PlayerProgression.Instance is null in {calledFrom}, cannot revert hand levels", this);
        }
        
        // Mark as inactive
        _isPowerupActive = false;
        Debug.Log($"[MaxHandUpgradePowerUp] Powerup marked as inactive in {calledFrom}", this);
    }
}