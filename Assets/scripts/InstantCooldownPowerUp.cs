// --- InstantCooldownPowerUp.cs ---
using UnityEngine;

public class InstantCooldownPowerUp : PowerUp
{
    [Header("Instant Cooldown Specifics")]
    [Tooltip("Duration for which heat immunity is active, in seconds.")]
    public float activeDuration = 8f;
    
    [Tooltip("Speed at which heat drains when powerup is activated (units per second).")]
    public float heatDrainSpeed = 150f;
    
    [Header("Visual Effects")]
    [Tooltip("Parent GameObject containing all particle effects to enable/disable during Instant Cooldown.")]
    public GameObject instantCooldownEffectsParent;

    protected override void Awake()
    {
        base.Awake();
        powerUpType = PowerUpType.InstantCooldown;
    }

    protected override void ApplyPowerUpEffect(PlayerProgression progression, PlayerShooterOrchestrator pso, PlayerAOEAbility aoeAbility, PlayerHealth playerHealth)
    {
        // Add to inventory system instead of auto-activating
        if (PowerupInventoryManager.Instance != null)
        {
            PowerupInventoryManager.Instance.AddPowerup(PowerUpType.InstantCooldown, activeDuration);
            Debug.Log($"[InstantCooldownPowerUp] Added to inventory with {activeDuration}s duration", this);
        }
        
        // Provide clear player feedback via UI
        if (DynamicPlayerFeedManager.Instance != null)
        {
            DynamicPlayerFeedManager.Instance.ShowPowerUpCollected(powerUpType);
            
            // Show instruction message
            DynamicPlayerFeedManager.Instance.ShowCustomMessage(
                $"Instant Cooldown Ready! Scroll to select, Middle Click to activate",
                DynamicPlayerFeedManager.Instance.powerUpColor,
                DynamicPlayerFeedManager.Instance.feedIcons.GetIconForPowerUp(PowerUpType.InstantCooldown),
                true, 3.0f);
        }
    }

    protected override float GetPowerupDuration()
    {
        return activeDuration;
    }
}
