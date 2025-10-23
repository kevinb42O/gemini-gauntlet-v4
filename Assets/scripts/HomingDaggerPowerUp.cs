// --- HomingDaggerPowerUp.cs (Corrected and Improved) ---
using UnityEngine;

public class HomingDaggersPowerUp : PowerUp
{
    [Header("Homing Dagger Specifics")]
    [Tooltip("Number of homing dagger charges to grant.")]
    public int charges = 5;
    [Tooltip("Duration each homing dagger activation lasts, in seconds.")]
    public float activeDuration = 15f;

    protected override void Awake()
    {
        base.Awake();
        powerUpType = PowerUpType.HomingDaggers;
    }

    // --- THIS IS THE FIX ---
    // The method signature now perfectly matches the one in the base PowerUp class.
    // We accept 'playerHealth' as a parameter but we don't need to use it here.
    protected override void ApplyPowerUpEffect(PlayerProgression progression, PlayerShooterOrchestrator pso, PlayerAOEAbility aoeAbility, PlayerHealth playerHealth)
    {
        // Add to inventory system as charge-based powerup with activationDuration
        if (PowerupInventoryManager.Instance != null)
        {
            PowerupInventoryManager.Instance.AddPowerup(PowerUpType.HomingDaggers, 0f, charges, activeDuration);
            Debug.Log($"[HomingDaggersPowerUp] Added to inventory with {charges} charges, {activeDuration}s per activation", this);
        }
        
        // Provide clear player feedback via UI
        if (DynamicPlayerFeedManager.Instance != null)
        {
            DynamicPlayerFeedManager.Instance.ShowPowerUpCollected(powerUpType);
            
            // Show instruction message
            DynamicPlayerFeedManager.Instance.ShowCustomMessage(
                $"Homing Daggers Ready! Scroll to select, Middle Click to activate",
                DynamicPlayerFeedManager.Instance.powerUpColor,
                DynamicPlayerFeedManager.Instance.feedIcons.GetIconForPowerUp(PowerUpType.HomingDaggers),
                true, 3.0f);
        }
    }

    protected override float GetPowerupDuration()
    {
        return activeDuration; // Duration per activation, not total duration
    }
}