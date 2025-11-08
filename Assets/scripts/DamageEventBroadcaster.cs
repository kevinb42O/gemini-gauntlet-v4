// ============================================================================
// DAMAGE EVENT BROADCASTER - Lightweight Damage Detection System
// Hooks into PlayerHealth.TakeDamage to provide precise damage event notifications
// Zero performance cost - only broadcasts when damage actually occurs
// ============================================================================

using UnityEngine;
using System;

/// <summary>
/// Lightweight component that hooks into PlayerHealth.TakeDamage method
/// to provide precise damage event notifications for other systems.
/// Automatically patches PlayerHealth to broadcast damage events.
/// </summary>
public class DamageEventBroadcaster : MonoBehaviour
{
    /// <summary>
    /// Event fired when player takes damage (after armor processing)
    /// float parameter is the actual damage amount applied to health
    /// </summary>
    public static event Action<float> OnDamageTaken;
    
    /// <summary>
    /// Event fired when player takes any damage (before armor processing)
    /// float parameter is the original damage amount before armor reduction
    /// </summary>
    public static event Action<float> OnDamageReceived;
    
    // Instance event for specific subscribers
    public event Action<float> OnDamageTaken_Instance;
    public event Action<float> OnDamageReceived_Instance;
    
    private PlayerHealth playerHealth;
    private bool isPatched = false;
    
    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("[DamageEventBroadcaster] No PlayerHealth component found! This component must be on the same GameObject as PlayerHealth.");
            enabled = false;
            return;
        }
        
        Debug.Log("[DamageEventBroadcaster] ✅ Initialized and ready to broadcast damage events", this);
    }
    
    void Start()
    {
        // The actual patching happens via the modified PlayerHealth script
        // This component serves as the event broadcaster
        isPatched = true;
    }
    
    /// <summary>
    /// Called by the patched PlayerHealth.TakeDamage method when damage is received (before armor)
    /// </summary>
    /// <param name="originalDamage">The original damage amount before armor processing</param>
    public void BroadcastDamageReceived(float originalDamage)
    {
        if (!enabled || !isPatched) return;
        
        // Broadcast to static listeners
        OnDamageReceived?.Invoke(originalDamage);
        
        // Broadcast to instance listeners
        OnDamageReceived_Instance?.Invoke(originalDamage);
        
        Debug.Log($"[DamageEventBroadcaster] Damage received: {originalDamage}", this);
    }
    
    /// <summary>
    /// Called by the patched PlayerHealth.TakeDamage method when health damage is applied (after armor)
    /// </summary>
    /// <param name="healthDamage">The actual damage amount applied to health after armor processing</param>
    public void BroadcastDamageTaken(float healthDamage)
    {
        if (!enabled || !isPatched) return;
        
        // Only broadcast if actual health damage occurred
        if (healthDamage > 0)
        {
            // Broadcast to static listeners
            OnDamageTaken?.Invoke(healthDamage);
            
            // Broadcast to instance listeners
            OnDamageTaken_Instance?.Invoke(healthDamage);
            
            Debug.Log($"[DamageEventBroadcaster] Health damage taken: {healthDamage}", this);
        }
    }
    
    void OnDestroy()
    {
        // Clear all instance event subscriptions
        OnDamageTaken_Instance = null;
        OnDamageReceived_Instance = null;
    }
    
    // ============================================================================
    // TESTING & DEBUG HELPERS
    // ============================================================================
    
    [ContextMenu("Test Damage Event (10 damage)")]
    private void TestDamageEvent()
    {
        if (Application.isPlaying && playerHealth != null)
        {
            playerHealth.TakeDamage(10f);
        }
    }
    
    [ContextMenu("Test Damage Event (50 damage)")]
    private void TestBigDamageEvent()
    {
        if (Application.isPlaying && playerHealth != null)
        {
            playerHealth.TakeDamage(50f);
        }
    }
}