// --- PowerupEffectManager.cs (Centralized Particle Effect Management) ---
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Centralized manager for all powerup particle effects and visual feedback
/// Prevents conflicts and ensures consistent effect management across the system
/// </summary>
public class PowerupEffectManager : MonoBehaviour
{
    public static PowerupEffectManager Instance { get; private set; }
    
    [Header("Debug")]
    [SerializeField] private bool verboseDebugging = false;
    
    // FIXED: Support multiple instances per powerup type
    // Each effect instance gets a unique ID
    private int nextEffectInstanceID = 1;
    
    // Track active effect instances
    private class EffectInstance
    {
        public int instanceID;
        public PowerUpType powerupType;
        public Coroutine durationCoroutine;
        public bool isActive;
        
        public EffectInstance(int id, PowerUpType type)
        {
            instanceID = id;
            powerupType = type;
            isActive = true;
        }
    }
    
    private Dictionary<int, EffectInstance> activeEffectInstances = new Dictionary<int, EffectInstance>();
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[PowerupEffectManager] Initialized as singleton", this);
        }
        else
        {
            Debug.LogWarning("[PowerupEffectManager] Duplicate instance destroyed", this);
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// FIXED: Activate a powerup effect with automatic duration management
    /// Returns unique instance ID for tracking multiple instances of same type
    /// </summary>
    public int ActivatePowerupEffect(PowerUpType powerupType, float duration)
    {
        if (verboseDebugging)
        {
            Debug.Log($"[PowerupEffectManager] ActivatePowerupEffect called - Type: {powerupType}, Duration: {duration}s", this);
        }
        
        // Create new effect instance with unique ID
        int instanceID = nextEffectInstanceID++;
        EffectInstance newInstance = new EffectInstance(instanceID, powerupType);
        activeEffectInstances[instanceID] = newInstance;
        
        if (verboseDebugging)
        {
            Debug.Log($"[PowerupEffectManager] Created effect instance #{instanceID} for {powerupType}", this);
        }
        
        // Check if this is the FIRST instance of this powerup type
        bool isFirstInstance = GetActiveInstanceCount(powerupType) == 1;
        
        // Only activate visual effects if this is the first instance
        // (prevents duplicate particle systems)
        if (isFirstInstance)
        {
            if (verboseDebugging)
            {
                Debug.Log($"[PowerupEffectManager] First instance of {powerupType} - activating visual effects", this);
            }
            
            // Delegate to appropriate system based on powerup type
            switch (powerupType)
            {
                case PowerUpType.GodMode:
                    ActivateGodModeEffect(duration);
                    break;
                    
                case PowerUpType.DoubleGems:
                    ActivateDoubleGemsEffect(duration);
                    break;
                    
                case PowerUpType.SlowTime:
                    ActivateSlowTimeEffect(duration);
                    break;
                    
                case PowerUpType.MaxHandUpgrade:
                    ActivateMaxHandUpgradeEffect(duration);
                    break;
                    
                case PowerUpType.InstantCooldown:
                    ActivateInstantCooldownEffect(duration);
                    break;
                    
                case PowerUpType.DoubleDamage:
                    ActivateDoubleDamageEffect(duration);
                    break;
                    
                default:
                    Debug.LogWarning($"[PowerupEffectManager] No effect handler for powerup type: {powerupType}", this);
                    break;
            }
        }
        else
        {
            if (verboseDebugging)
            {
                Debug.Log($"[PowerupEffectManager] Additional instance of {powerupType} - visual effects already active", this);
            }
        }
        
        // Start duration tracking coroutine for THIS instance
        if (duration > 0)
        {
            newInstance.durationCoroutine = StartCoroutine(EffectDurationCoroutine(instanceID, duration));
        }
        
        return instanceID;
    }
    
    /// <summary>
    /// FIXED: Deactivate a specific effect instance by ID
    /// </summary>
    public void DeactivatePowerupEffectByID(int instanceID)
    {
        if (!activeEffectInstances.ContainsKey(instanceID))
        {
            if (verboseDebugging)
            {
                Debug.LogWarning($"[PowerupEffectManager] Cannot deactivate instance #{instanceID} - not found", this);
            }
            return;
        }
        
        EffectInstance instance = activeEffectInstances[instanceID];
        PowerUpType powerupType = instance.powerupType;
        
        if (verboseDebugging)
        {
            Debug.Log($"[PowerupEffectManager] Deactivating instance #{instanceID} of {powerupType}", this);
        }
        
        // Stop duration tracking coroutine if running
        if (instance.durationCoroutine != null)
        {
            StopCoroutine(instance.durationCoroutine);
        }
        
        // Remove this instance
        activeEffectInstances.Remove(instanceID);
        
        // Check if this was the LAST instance of this powerup type
        int remainingInstances = GetActiveInstanceCount(powerupType);
        
        if (remainingInstances == 0)
        {
            if (verboseDebugging)
            {
                Debug.Log($"[PowerupEffectManager] Last instance of {powerupType} - deactivating visual effects", this);
            }
            
            // Delegate to appropriate system for cleanup
            switch (powerupType)
            {
                case PowerUpType.GodMode:
                    DeactivateGodModeEffect();
                    break;
                    
                case PowerUpType.DoubleGems:
                    DeactivateDoubleGemsEffect();
                    break;
                    
                case PowerUpType.SlowTime:
                    DeactivateSlowTimeEffect();
                    break;
                    
                case PowerUpType.MaxHandUpgrade:
                    DeactivateMaxHandUpgradeEffect();
                    break;
                    
                case PowerUpType.InstantCooldown:
                    DeactivateInstantCooldownEffect();
                    break;
                    
                case PowerUpType.DoubleDamage:
                    DeactivateDoubleDamageEffect();
                    break;
            }
        }
        else
        {
            if (verboseDebugging)
            {
                Debug.Log($"[PowerupEffectManager] {remainingInstances} instance(s) of {powerupType} still active - keeping visual effects", this);
            }
        }
    }
    
    /// <summary>
    /// DEPRECATED: Deactivate all instances of a powerup type (for backward compatibility)
    /// </summary>
    public void DeactivatePowerupEffect(PowerUpType powerupType)
    {
        if (verboseDebugging)
        {
            Debug.Log($"[PowerupEffectManager] DeactivatePowerupEffect (all instances) called - Type: {powerupType}", this);
        }
        
        // Find all instances of this type and deactivate them
        var instancesToRemove = new List<int>();
        foreach (var kvp in activeEffectInstances)
        {
            if (kvp.Value.powerupType == powerupType)
            {
                instancesToRemove.Add(kvp.Key);
            }
        }
        
        foreach (int instanceID in instancesToRemove)
        {
            DeactivatePowerupEffectByID(instanceID);
        }
    }
    
    /// <summary>
    /// FIXED: Get count of active instances for a powerup type
    /// </summary>
    private int GetActiveInstanceCount(PowerUpType powerupType)
    {
        int count = 0;
        foreach (var instance in activeEffectInstances.Values)
        {
            if (instance.powerupType == powerupType && instance.isActive)
            {
                count++;
            }
        }
        return count;
    }
    
    /// <summary>
    /// Check if a specific powerup effect is currently active
    /// </summary>
    public bool IsEffectActive(PowerUpType powerupType)
    {
        return GetActiveInstanceCount(powerupType) > 0;
    }
    
    /// <summary>
    /// FIXED: Force stop all active effects (for death/reset scenarios)
    /// </summary>
    public void StopAllEffects()
    {
        Debug.Log("[PowerupEffectManager] Stopping all active powerup effects", this);
        
        // Create a copy of the instance IDs to avoid modification during iteration
        var instanceIDs = new List<int>(activeEffectInstances.Keys);
        
        foreach (int instanceID in instanceIDs)
        {
            DeactivatePowerupEffectByID(instanceID);
        }
        
        // Clear all tracking
        activeEffectInstances.Clear();
        nextEffectInstanceID = 1; // Reset ID counter
    }
    
    /// <summary>
    /// FIXED: Coroutine to track effect instance duration and auto-deactivate
    /// </summary>
    private IEnumerator EffectDurationCoroutine(int instanceID, float duration)
    {
        if (verboseDebugging)
        {
            Debug.Log($"[PowerupEffectManager] Starting duration tracking for instance #{instanceID} - {duration}s", this);
        }
        
        // Use unscaled time for consistency with other powerup timers
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        
        if (verboseDebugging)
        {
            Debug.Log($"[PowerupEffectManager] Duration expired for instance #{instanceID} - auto-deactivating", this);
        }
        
        // Auto-deactivate this specific instance when duration expires
        DeactivatePowerupEffectByID(instanceID);
    }
    
    // Individual effect activation methods
    private void ActivateGodModeEffect(float duration)
    {
        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.ActivateGodMode(duration);
            Debug.Log($"[PowerupEffectManager] Activated GodMode effect via PlayerHealth", this);
        }
    }
    
    private void DeactivateGodModeEffect()
    {
        // GodMode deactivation is handled by PlayerProgression when duration expires
        // No direct deactivation method needed in PlayerHealth
        Debug.Log($"[PowerupEffectManager] GodMode effect will auto-deactivate when duration expires", this);
    }
    
    private void ActivateDoubleGemsEffect(float duration)
    {
        if (PlayerProgression.Instance != null)
        {
            PlayerProgression.Instance.ActivateDoubleGems(duration);
            Debug.Log($"[PowerupEffectManager] Activated DoubleGems effect via PlayerProgression", this);
        }
    }
    
    private void DeactivateDoubleGemsEffect()
    {
        // DoubleGems deactivation is handled by PlayerProgression when duration expires
        // No direct deactivation method needed
        Debug.Log($"[PowerupEffectManager] DoubleGems effect will auto-deactivate when duration expires", this);
    }
    
    private void ActivateSlowTimeEffect(float duration)
    {
        if (PlayerProgression.Instance != null)
        {
            PlayerProgression.Instance.ActivateSlowTime(duration, 0.3f, null, null, 0.7f);
            Debug.Log($"[PowerupEffectManager] Activated SlowTime effect via PlayerProgression", this);
        }
    }
    
    private void DeactivateSlowTimeEffect()
    {
        // SlowTime deactivation is handled by PlayerProgression when duration expires
        // Reset time scale as safety measure
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        Debug.Log($"[PowerupEffectManager] Reset time scale to normal", this);
    }
    
    private void ActivateMaxHandUpgradeEffect(float duration)
    {
        PlayerOverheatManager overheatManager = PlayerOverheatManager.Instance;
        if (overheatManager != null)
        {
            overheatManager.ActivateMaxHandUpgradeEffects();
            Debug.Log($"[PowerupEffectManager] Activated MaxHandUpgrade effects via PlayerOverheatManager", this);
        }
    }
    
    private void DeactivateMaxHandUpgradeEffect()
    {
        PlayerOverheatManager overheatManager = PlayerOverheatManager.Instance;
        if (overheatManager != null)
        {
            overheatManager.DeactivateMaxHandUpgradeEffects();
            Debug.Log($"[PowerupEffectManager] Deactivated MaxHandUpgrade effects via PlayerOverheatManager", this);
        }
    }
    
    private void ActivateInstantCooldownEffect(float duration)
    {
        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.ActivateInstantCooldown(duration);
        }
        
        PlayerOverheatManager overheatManager = PlayerOverheatManager.Instance;
        if (overheatManager != null)
        {
            overheatManager.ActivateInstantCooldownEffects(duration);
            Debug.Log($"[PowerupEffectManager] Activated InstantCooldown effects", this);
        }
    }
    
    private void DeactivateInstantCooldownEffect()
    {
        // InstantCooldown deactivation is handled automatically when duration expires
        PlayerOverheatManager overheatManager = PlayerOverheatManager.Instance;
        if (overheatManager != null)
        {
            overheatManager.DeactivateInstantCooldownEffects();
            Debug.Log($"[PowerupEffectManager] Deactivated InstantCooldown effects", this);
        }
    }
    
    private void ActivateDoubleDamageEffect(float duration)
    {
        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.ActivateDoubleDamage(duration);
        }
        
        PlayerOverheatManager overheatManager = PlayerOverheatManager.Instance;
        if (overheatManager != null)
        {
            overheatManager.ActivateDoubleDamageEffects(duration);
            Debug.Log($"[PowerupEffectManager] Activated DoubleDamage effects", this);
        }
    }
    
    private void DeactivateDoubleDamageEffect()
    {
        // DoubleDamage deactivation is handled automatically when duration expires
        PlayerOverheatManager overheatManager = PlayerOverheatManager.Instance;
        if (overheatManager != null)
        {
            overheatManager.DeactivateDoubleDamageEffects();
            Debug.Log($"[PowerupEffectManager] Deactivated DoubleDamage effects", this);
        }
    }
}
