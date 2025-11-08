// ============================================================================
// FALLING DAMAGE RED LIGHT FIX - Integration Test
// Tests that TakeDamageBypassArmor properly triggers DamageEventBroadcaster events
// which should now trigger the red light effect in DynamicPlayerLightController
// ============================================================================

using UnityEngine;

/// <summary>
/// Test component to verify that falling damage triggers the red light effect.
/// Add this to the Player GameObject to test the fix.
/// </summary>
public class FallingDamageRedLightFix : MonoBehaviour
{
    [Header("Test Configuration")]
    [SerializeField] private KeyCode testKey = KeyCode.F9;
    [SerializeField] private float testDamageAmount = 25f;
    
    private PlayerHealth playerHealth;
    private DamageEventBroadcaster damageEventBroadcaster;
    private DynamicPlayerLightController lightController;
    
    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        damageEventBroadcaster = GetComponent<DamageEventBroadcaster>();
        lightController = FindFirstObjectByType<DynamicPlayerLightController>();
    }
    
    void Start()
    {
        Debug.Log("[FallingDamageRedLightFix] ✅ Test component ready. Press F9 to test falling damage red light effect.");
        
        // Subscribe to damage events to verify they're being triggered
        if (damageEventBroadcaster != null)
        {
            DamageEventBroadcaster.OnDamageTaken += OnDamageTakenTest;
            DamageEventBroadcaster.OnDamageReceived += OnDamageReceivedTest;
            Debug.Log("[FallingDamageRedLightFix] ✅ Subscribed to damage events for testing.");
        }
        else
        {
            Debug.LogError("[FallingDamageRedLightFix] ❌ No DamageEventBroadcaster found! Add DamageEventBroadcaster component to Player.");
        }
        
        if (lightController == null)
        {
            Debug.LogError("[FallingDamageRedLightFix] ❌ No DynamicPlayerLightController found! Check that component exists in scene.");
        }
        else
        {
            Debug.Log("[FallingDamageRedLightFix] ✅ Found DynamicPlayerLightController - red light effects should work.");
        }
    }
    
    void Update()
    {
        if (Input.GetKeyDown(testKey))
        {
            TestFallingDamageRedLight();
        }
    }
    
    /// <summary>
    /// Test the falling damage red light effect by simulating fall damage
    /// </summary>
    [ContextMenu("Test Falling Damage Red Light")]
    public void TestFallingDamageRedLight()
    {
        if (playerHealth == null)
        {
            Debug.LogError("[FallingDamageRedLightFix] ❌ No PlayerHealth component found!");
            return;
        }
        
        Debug.Log($"[FallingDamageRedLightFix] 🧪 TESTING: Applying {testDamageAmount} bypass armor damage (simulating fall damage)...");
        
        float healthBefore = playerHealth.CurrentHealth;
        
        // This simulates what FallingDamageSystem does
        playerHealth.TakeDamageBypassArmor(testDamageAmount);
        
        float healthAfter = playerHealth.CurrentHealth;
        Debug.Log($"[FallingDamageRedLightFix] 📊 Health: {healthBefore} → {healthAfter} (damage: {healthBefore - healthAfter})");
        
        if (lightController != null)
        {
            Debug.Log("[FallingDamageRedLightFix] 🔴 If the red light flashed, the fix is working! If not, check DamageEventBroadcaster integration.");
        }
    }
    
    /// <summary>
    /// Event handler to verify damage events are being triggered
    /// </summary>
    private void OnDamageTakenTest(float damage)
    {
        Debug.Log($"[FallingDamageRedLightFix] ✅ DAMAGE EVENT TRIGGERED: OnDamageTaken({damage}) - Red light should flash!");
    }
    
    /// <summary>
    /// Event handler to verify damage events are being triggered
    /// </summary>
    private void OnDamageReceivedTest(float damage)
    {
        Debug.Log($"[FallingDamageRedLightFix] ✅ DAMAGE EVENT TRIGGERED: OnDamageReceived({damage}) - Events are working!");
    }
    
    void OnDestroy()
    {
        // Clean up event subscriptions
        if (damageEventBroadcaster != null)
        {
            DamageEventBroadcaster.OnDamageTaken -= OnDamageTakenTest;
            DamageEventBroadcaster.OnDamageReceived -= OnDamageReceivedTest;
        }
    }
    
    // ============================================================================
    // DIAGNOSTIC HELPERS
    // ============================================================================
    
    [ContextMenu("Check Component Dependencies")]
    public void CheckDependencies()
    {
        Debug.Log("=== FALLING DAMAGE RED LIGHT FIX DIAGNOSTICS ===");
        
        Debug.Log($"PlayerHealth: {(playerHealth != null ? "✅ Found" : "❌ Missing")}");
        Debug.Log($"DamageEventBroadcaster: {(damageEventBroadcaster != null ? "✅ Found" : "❌ Missing")}");
        Debug.Log($"DynamicPlayerLightController: {(lightController != null ? "✅ Found" : "❌ Missing")}");
        
        Debug.Log("=== END DIAGNOSTICS ===");
    }
}