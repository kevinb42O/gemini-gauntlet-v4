// --- AOEPowerUp.cs (Enhanced with Debugging & Sound) ---
using UnityEngine;
using GeminiGauntlet.Audio;

public class AOEPowerUp : PowerUp
{
    [Header("AOE Specifics")]
    [Tooltip("Number of AOE charges this power-up grants.")]
    public int chargesGranted = 1;
    
    [Header("Debug")]
    [Tooltip("Enable verbose logging to help diagnose issues")]
    public bool verboseDebugging = true;

    protected override void Awake()
    {
        base.Awake();
        powerUpType = PowerUpType.AOEAttack;
        
        if (verboseDebugging)
        {
            Debug.Log($"[AOEPowerUp {gameObject.name}] Initialized with {chargesGranted} charge(s) to grant", this);
        }
    }

    protected override void ApplyPowerUpEffect(PlayerProgression progression, PlayerShooterOrchestrator pso, PlayerAOEAbility aoeAbility, PlayerHealth playerHealth)
    {
        Debug.Log($"[AOEPowerUp {gameObject.name}] ApplyPowerUpEffect called", this);
        
        // Failsafe to find AOE ability if not provided
        if (aoeAbility == null)
        {
            Debug.LogWarning("[AOEPowerUp] aoeAbility parameter is null, attempting to find it directly", this);
            aoeAbility = GameObject.FindObjectOfType<PlayerAOEAbility>();
            
            if (aoeAbility == null)
            {
                Debug.LogError("[AOEPowerUp] CRITICAL: PlayerAOEAbility component not found anywhere in scene!", this);
                return;
            }
        }
        
        Debug.Log($"[AOEPowerUp] Using AOEAbility instance: {aoeAbility.name}, current charges: {aoeAbility.GetCurrentCharges()}", this);
        
        // Play power-up collection sound
        GameSounds.PlayPowerUpSound(PowerUpType.AOEAttack, transform.position);
        
        // Log before granting charges
        Debug.Log($"[AOEPowerUp] Granting {chargesGranted} AOE charge(s) to player", this);
        
        // 1. Activate the core game mechanic - with explicit force to Ready state
        aoeAbility.GrantAOEChargeByPowerUp(chargesGranted);

        // Extra debug logs to confirm charges were granted
        Debug.Log($"[AOEPowerUp] After granting: AOE charges should be {aoeAbility.GetCurrentCharges()}, status: {aoeAbility.CurrentAOEStatus}", this);

        // 2. Add to inventory system
        if (PowerupInventoryManager.Instance != null)
        {
            PowerupInventoryManager.Instance.AddPowerup(PowerUpType.AOEAttack, 0f, chargesGranted);
            Debug.Log($"[AOEPowerUp] Added to inventory with {chargesGranted} charges", this);
        }
        
        // 3. Provide clear player feedback via UI, including the number of charges.
        if (DynamicPlayerFeedManager.Instance != null)
        {
            DynamicPlayerFeedManager.Instance.ShowPowerUpCollected(powerUpType, chargesGranted);
            Debug.Log($"[AOEPowerUp] Showing UI feedback for {chargesGranted} charges", this);
            
            // Also show a custom message for better feedback
            DynamicPlayerFeedManager.Instance.ShowCustomMessage(
                $"AOE Attack Ready! Scroll to select, Middle Click to activate",
                DynamicPlayerFeedManager.Instance.powerUpColor,
                DynamicPlayerFeedManager.Instance.feedIcons.GetIconForPowerUp(PowerUpType.AOEAttack),
                true, 3.0f);
        }
        else
        {
            Debug.LogWarning("[AOEPowerUp] DynamicPlayerFeedManager.Instance is null, cannot show UI feedback", this);
        }
            
        // Extra debug code removed to fix bracket mismatch
    }

    protected override float GetPowerupDuration()
    {
        return 0f; // AOE is charge-based, not duration-based
    }
}