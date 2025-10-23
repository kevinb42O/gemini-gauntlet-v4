// --- DoubleGemsPowerUp.cs (Corrected and Improved) ---
using UnityEngine;

public class DoubleGemsPowerUp : PowerUp
{
    [Header("Double Gems Specifics")]
    [Tooltip("Duration for which gems count as double, in seconds.")]
    public float activeDuration = 20f;

    protected override void Awake()
    {
        base.Awake();
        powerUpType = PowerUpType.DoubleGems;
    }

    // --- THIS IS THE FIX ---
    // The method signature now perfectly matches the one in the base PowerUp class.
    // We accept pso, aoeAbility, and playerHealth as parameters but we don't need to use them.
    protected override void ApplyPowerUpEffect(PlayerProgression progression, PlayerShooterOrchestrator pso, PlayerAOEAbility aoeAbility, PlayerHealth playerHealth)
    {
        // Add to inventory system instead of auto-activating
        if (PowerupInventoryManager.Instance != null)
        {
            PowerupInventoryManager.Instance.AddPowerup(PowerUpType.DoubleGems, activeDuration);
            Debug.Log($"[DoubleGemsPowerUp] Added to inventory with {activeDuration}s duration", this);
        }
        
        // Provide clear player feedback via UI
        if (DynamicPlayerFeedManager.Instance != null)
        {
            DynamicPlayerFeedManager.Instance.ShowPowerUpCollected(powerUpType);
            
            // Show instruction message
            DynamicPlayerFeedManager.Instance.ShowCustomMessage(
                $"Double Gems Ready! Scroll to select, Middle Click to activate",
                DynamicPlayerFeedManager.Instance.powerUpColor,
                DynamicPlayerFeedManager.Instance.feedIcons.GetIconForPowerUp(PowerUpType.DoubleGems),
                true, 3.0f);
        }
    }

    protected override float GetPowerupDuration()
    {
        return activeDuration;
    }
}