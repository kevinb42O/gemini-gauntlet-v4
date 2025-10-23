// --- DoubleDamagePowerUp.cs ---
using UnityEngine;

public class DoubleDamagePowerUp : PowerUp
{
    [Header("Double Damage Specifics")]
    [Tooltip("Duration for which double damage is active, in seconds.")]
    public float activeDuration = 15f;
    
    [Tooltip("Damage multiplier applied to all damage sources.")]
    public float damageMultiplier = 2f;
    
    [Header("Visual Effects")]
    [Tooltip("Parent GameObject containing all particle effects to enable/disable during Double Damage.")]
    public GameObject doubleDamageEffectsParent;

    protected override void Awake()
    {
        base.Awake();
        powerUpType = PowerUpType.DoubleDamage;
    }

    protected override void ApplyPowerUpEffect(PlayerProgression progression, PlayerShooterOrchestrator pso, PlayerAOEAbility aoeAbility, PlayerHealth playerHealth)
    {
        // Add to inventory system instead of auto-activating
        if (PowerupInventoryManager.Instance != null)
        {
            PowerupInventoryManager.Instance.AddPowerup(PowerUpType.DoubleDamage, activeDuration);
            Debug.Log($"[DoubleDamagePowerUp] Added to inventory with {activeDuration}s duration", this);
        }
        
        // Provide clear player feedback via UI
        if (DynamicPlayerFeedManager.Instance != null)
        {
            DynamicPlayerFeedManager.Instance.ShowPowerUpCollected(powerUpType);
            
            // Show instruction message
            DynamicPlayerFeedManager.Instance.ShowCustomMessage(
                $"Double Damage Ready! Scroll to select, Middle Click to activate",
                DynamicPlayerFeedManager.Instance.powerUpColor,
                DynamicPlayerFeedManager.Instance.feedIcons.GetIconForPowerUp(PowerUpType.DoubleDamage),
                true, 3.0f);
        }
    }

    protected override float GetPowerupDuration()
    {
        return activeDuration;
    }
}
