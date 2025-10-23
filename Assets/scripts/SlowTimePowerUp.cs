// --- SlowTimePowerUp.cs (Corrected and Improved) ---
using UnityEngine;

public class SlowTimePowerUp : PowerUp
{
    [Header("Slow Time Specifics")]
    [Tooltip("Duration for which time is slowed, in seconds (real-time).")]
    public float activeDuration = 8f;
    [Tooltip("Time scale factor (e.g., 0.2 for 20% speed, much slower than before).")]
    [Range(0.05f, 0.9f)] public float timeScaleFactor = 0.2f;

    [Header("One-Shot Sounds (Optional)")]
    [Tooltip("Sound played when time slow STARTS.")]
    public AudioClip timeSlowStartSound;
    [Tooltip("Sound played when time slow ENDS.")]
    public AudioClip timeSlowEndSound;
    [Range(0f, 1f)] public float timeSlowSoundVolume = 0.7f;

    public static bool isTimeSlowActive = false;
    public static float originalFixedDeltaTime;

    protected override void Awake()
    {
        base.Awake();
        powerUpType = PowerUpType.SlowTime;
    }

    protected override void ApplyPowerUpEffect(PlayerProgression progression, PlayerShooterOrchestrator pso, PlayerAOEAbility aoeAbility, PlayerHealth playerHealth)
    {
        // Add to inventory system instead of auto-activating
        if (PowerupInventoryManager.Instance != null)
        {
            PowerupInventoryManager.Instance.AddPowerup(PowerUpType.SlowTime, activeDuration);
            Debug.Log($"[SlowTimePowerUp] Added to inventory with {activeDuration}s duration", this);
        }
        
        // Provide clear player feedback via UI
        if (DynamicPlayerFeedManager.Instance != null)
        {
            DynamicPlayerFeedManager.Instance.ShowPowerUpCollected(powerUpType);
            
            // Show instruction message
            DynamicPlayerFeedManager.Instance.ShowCustomMessage(
                $"Slow Time Ready! Scroll to select, Middle Click to activate",
                DynamicPlayerFeedManager.Instance.powerUpColor,
                DynamicPlayerFeedManager.Instance.feedIcons.GetIconForPowerUp(PowerUpType.SlowTime),
                true, 3.0f);
        }
    }

    protected override float GetPowerupDuration()
    {
        return activeDuration;
    }

    // Static methods for managing time scale should remain for global access.
    public static void SetTimeSlowActive(bool active)
    {
        isTimeSlowActive = active;
        if (active)
        {
            originalFixedDeltaTime = Time.fixedDeltaTime;
        }
    }

    public static void RestoreOriginalFixedDeltaTime()
    {
        if (originalFixedDeltaTime > 0 && Time.fixedDeltaTime != originalFixedDeltaTime)
        {
            Time.fixedDeltaTime = originalFixedDeltaTime;
        }
    }
}