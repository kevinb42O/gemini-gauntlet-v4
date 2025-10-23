// --- GodModePowerUp.cs (Corrected and Improved) ---
using UnityEngine;

public class GodModePowerUp : PowerUp
{
    [Header("God Mode Specifics")]
    [Tooltip("Duration for which god mode is active, in seconds.")]
    public float activeDuration = 10f;
    
    [Header("Visual Effects")]
    [Tooltip("Parent GameObject containing all particle effects to enable/disable during God Mode.")]
    public GameObject godModeEffectsParent;

    protected override void Awake()
    {
        base.Awake();
        powerUpType = PowerUpType.GodMode;
    }

    protected override void ApplyPowerUpEffect(PlayerProgression progression, PlayerShooterOrchestrator pso, PlayerAOEAbility aoeAbility, PlayerHealth playerHealth)
    {
        // Add to inventory system instead of auto-activating
        if (PowerupInventoryManager.Instance != null)
        {
            PowerupInventoryManager.Instance.AddPowerup(PowerUpType.GodMode, activeDuration);
            Debug.Log($"[GodModePowerUp] Added to inventory with {activeDuration}s duration", this);
            
            // Pass parent GameObject to PlayerProgression for activation
            if (PlayerProgression.Instance != null && godModeEffectsParent != null)
            {
                PlayerProgression.Instance.SetGodModeEffectsParent(godModeEffectsParent);
                Debug.Log($"[GodModePowerUp] Assigned effects parent: {godModeEffectsParent.name}", this);
            }
        }
        
        // Provide clear player feedback via UI
        if (DynamicPlayerFeedManager.Instance != null)
        {
            DynamicPlayerFeedManager.Instance.ShowPowerUpCollected(powerUpType);
            
            // Show instruction message
            DynamicPlayerFeedManager.Instance.ShowCustomMessage(
                $"God Mode Ready! Scroll to select, Middle Click to activate",
                DynamicPlayerFeedManager.Instance.powerUpColor,
                DynamicPlayerFeedManager.Instance.feedIcons.GetIconForPowerUp(PowerUpType.GodMode),
                true, 3.0f);
        }
    }

    protected override float GetPowerupDuration()
    {
        return activeDuration;
    }
}