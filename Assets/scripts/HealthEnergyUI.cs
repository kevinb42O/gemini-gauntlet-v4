using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Manages health, shield, and energy UI sliders on the main canvas
/// </summary>
public class HealthEnergyUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider shieldSlider; // NEW: Armor plate shield slider
    [SerializeField] private Slider energySlider;
    
    [Header("Optional: Slider Fill Colors")]
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Image shieldFillImage; // NEW: Shield fill image
    [SerializeField] private Image energyFillImage;
    
    [Header("Health Color Settings")]
    [SerializeField] private Color healthColorNormal = new Color(0.0f, 0.8f, 0.0f); // Green
    [SerializeField] private Color healthColorLow = new Color(1f, 0.5f, 0f); // Orange when low
    [SerializeField] private Color healthColorCritical = new Color(0.8f, 0.1f, 0.1f); // Red when critical
    [SerializeField] private float lowHealthThreshold = 0.3f; // 30% threshold
    [SerializeField] private float criticalHealthThreshold = 0.2f; // 20% threshold
    
    [Header("Shield Color Settings")]
    [SerializeField] private Color shieldColor = new Color(0.0f, 0.5f, 1.0f); // Blue
    
    [Header("Energy Color Settings")]
    [SerializeField] private Color energyColor = new Color(0.2f, 0.8f, 1f); // Cyan
    [SerializeField] private Color lowEnergyColor = new Color(1f, 0.5f, 0f); // Orange when low
    [SerializeField] private float lowEnergyThreshold = 0.3f; // 30% threshold
    
    [Header("Health Regeneration Visual Effects")]
    [SerializeField] private float regenBurstScaleMultiplier = 1.2f; // Scale during initial burst
    [SerializeField] private float regenBurstScaleDuration = 2f; // Duration of burst scale effect
    
    [Header("Critical Health Pulsation")]
    [SerializeField] private float criticalPulseScale = 1.15f; // Scale during pulse
    [SerializeField] private float criticalPulseDuration = 0.5f; // Duration of one pulse cycle
    
    private PlayerHealth playerHealth;
    private PlayerEnergySystem playerEnergy;
    private ArmorPlateSystem armorPlateSystem;
    
    // Visual effect state
    private bool isRegenerating = false;
    private bool isCriticalHealth = false;
    private Coroutine regenScaleCoroutine;
    private Coroutine criticalPulseCoroutine;
    private Vector3 healthSliderOriginalScale;
    private float lastHealthPercent = 1f;
    
    void Awake()
    {
        // Find player components
        playerHealth = FindObjectOfType<PlayerHealth>();
        playerEnergy = FindObjectOfType<PlayerEnergySystem>();
        armorPlateSystem = FindObjectOfType<ArmorPlateSystem>();
        
        if (playerHealth == null)
        {
            Debug.LogError("[HealthEnergyUI] PlayerHealth not found in scene!");
        }
        
        if (playerEnergy == null)
        {
            Debug.LogError("[HealthEnergyUI] PlayerEnergySystem not found in scene!");
        }
        
        if (armorPlateSystem == null)
        {
            Debug.LogWarning("[HealthEnergyUI] ArmorPlateSystem not found - shield UI will not function!");
        }
        
        // Set initial colors
        if (healthFillImage != null)
        {
            healthFillImage.color = healthColorNormal;
        }
        
        if (shieldFillImage != null)
        {
            shieldFillImage.color = shieldColor;
        }
        
        if (energyFillImage != null)
        {
            energyFillImage.color = energyColor;
        }
        
        // Initialize sliders
        if (healthSlider != null)
        {
            healthSlider.minValue = 0;
            healthSlider.maxValue = 1;
            healthSlider.value = 1;
            healthSliderOriginalScale = healthSlider.transform.localScale;
        }
        
        if (shieldSlider != null)
        {
            shieldSlider.minValue = 0;
            shieldSlider.maxValue = 1;
            shieldSlider.value = 0; // Start with no shield
            
            // Always keep shield slider visible (even when empty)
            shieldSlider.gameObject.SetActive(true);
        }
        
        if (energySlider != null)
        {
            energySlider.minValue = 0;
            energySlider.maxValue = 1;
            energySlider.value = 1;
        }
    }
    
    void OnEnable()
    {
        // Subscribe to events
        PlayerHealth.OnHealthChangedForHUD += UpdateHealthUI;
        PlayerEnergySystem.OnEnergyChanged += UpdateEnergyUI;
        ArmorPlateSystem.OnPlateShieldChanged += UpdateShieldUI;
    }
    
    void OnDisable()
    {
        // Unsubscribe from events
        PlayerHealth.OnHealthChangedForHUD -= UpdateHealthUI;
        PlayerEnergySystem.OnEnergyChanged -= UpdateEnergyUI;
        ArmorPlateSystem.OnPlateShieldChanged -= UpdateShieldUI;
    }
    
    /// <summary>
    /// Update health slider when health changes
    /// </summary>
    private void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        if (healthSlider == null) return;
        
        float healthPercent = maxHealth > 0 ? currentHealth / maxHealth : 0;
        healthSlider.value = healthPercent;
        
        // Detect if health is increasing (regenerating)
        bool healthIncreasing = healthPercent > lastHealthPercent;
        lastHealthPercent = healthPercent;
        
        // Update health color based on percentage
        if (healthFillImage != null)
        {
            if (healthPercent <= criticalHealthThreshold)
            {
                // Critical health - RED
                healthFillImage.color = healthColorCritical;
                
                // Start pulsation effect if not already pulsing
                if (!isCriticalHealth)
                {
                    isCriticalHealth = true;
                    if (criticalPulseCoroutine != null) StopCoroutine(criticalPulseCoroutine);
                    criticalPulseCoroutine = StartCoroutine(CriticalHealthPulse());
                }
            }
            else if (healthPercent <= lowHealthThreshold)
            {
                // Low health - ORANGE
                healthFillImage.color = healthColorLow;
                
                // Stop pulsation if it was active
                if (isCriticalHealth)
                {
                    isCriticalHealth = false;
                    if (criticalPulseCoroutine != null)
                    {
                        StopCoroutine(criticalPulseCoroutine);
                        criticalPulseCoroutine = null;
                    }
                    // Reset scale
                    if (healthSlider != null)
                    {
                        healthSlider.transform.localScale = healthSliderOriginalScale;
                    }
                }
            }
            else
            {
                // Normal health - GREEN
                healthFillImage.color = healthColorNormal;
                
                // Stop pulsation if it was active
                if (isCriticalHealth)
                {
                    isCriticalHealth = false;
                    if (criticalPulseCoroutine != null)
                    {
                        StopCoroutine(criticalPulseCoroutine);
                        criticalPulseCoroutine = null;
                    }
                    // Reset scale
                    if (healthSlider != null)
                    {
                        healthSlider.transform.localScale = healthSliderOriginalScale;
                    }
                }
            }
        }
        
        // Trigger regeneration burst visual effect when health starts increasing
        if (healthIncreasing && !isRegenerating && healthPercent < 1f)
        {
            isRegenerating = true;
            if (regenScaleCoroutine != null) StopCoroutine(regenScaleCoroutine);
            regenScaleCoroutine = StartCoroutine(RegenerationBurstEffect());
        }
        
        // Stop regeneration effect when health is full
        if (healthPercent >= 1f && isRegenerating)
        {
            isRegenerating = false;
            if (regenScaleCoroutine != null)
            {
                StopCoroutine(regenScaleCoroutine);
                regenScaleCoroutine = null;
            }
            // Reset scale
            if (healthSlider != null)
            {
                healthSlider.transform.localScale = healthSliderOriginalScale;
            }
        }
    }
    
    /// <summary>
    /// Update shield slider when plates change
    /// </summary>
    private void UpdateShieldUI(int plateCount, float currentShield, float maxShield)
    {
        if (shieldSlider == null) return;
        
        float shieldPercent = maxShield > 0 ? currentShield / maxShield : 0;
        shieldSlider.value = shieldPercent;
        
        // Always keep shield slider visible (shows empty when no plates)
        Debug.Log($"[HealthEnergyUI] Shield updated - {plateCount} plates, {currentShield}/{maxShield} shield ({shieldPercent:P0})");
    }
    
    /// <summary>
    /// Update energy slider when energy changes
    /// </summary>
    private void UpdateEnergyUI(float currentEnergy, float maxEnergy)
    {
        if (energySlider == null) return;
        
        float energyPercent = maxEnergy > 0 ? currentEnergy / maxEnergy : 0;
        energySlider.value = energyPercent;
        
        // Change color when energy is low
        if (energyFillImage != null)
        {
            if (energyPercent <= lowEnergyThreshold)
            {
                energyFillImage.color = lowEnergyColor;
            }
            else
            {
                energyFillImage.color = energyColor;
            }
        }
    }
    
    // === VISUAL EFFECT COROUTINES ===
    
    /// <summary>
    /// Regeneration burst effect - scales health slider during initial burst
    /// </summary>
    private IEnumerator RegenerationBurstEffect()
    {
        if (healthSlider == null) yield break;
        
        float elapsed = 0f;
        
        // Scale up during burst
        while (elapsed < regenBurstScaleDuration && isRegenerating)
        {
            float progress = elapsed / regenBurstScaleDuration;
            float scale = Mathf.Lerp(regenBurstScaleMultiplier, 1f, progress);
            healthSlider.transform.localScale = healthSliderOriginalScale * scale;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Ensure scale returns to normal
        healthSlider.transform.localScale = healthSliderOriginalScale;
        regenScaleCoroutine = null;
    }
    
    /// <summary>
    /// Critical health pulse effect - pulsates health slider when health is critical
    /// </summary>
    private IEnumerator CriticalHealthPulse()
    {
        if (healthSlider == null) yield break;
        
        while (isCriticalHealth)
        {
            // Pulse up
            float elapsed = 0f;
            while (elapsed < criticalPulseDuration / 2f)
            {
                float progress = elapsed / (criticalPulseDuration / 2f);
                float scale = Mathf.Lerp(1f, criticalPulseScale, progress);
                healthSlider.transform.localScale = healthSliderOriginalScale * scale;
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Pulse down
            elapsed = 0f;
            while (elapsed < criticalPulseDuration / 2f)
            {
                float progress = elapsed / (criticalPulseDuration / 2f);
                float scale = Mathf.Lerp(criticalPulseScale, 1f, progress);
                healthSlider.transform.localScale = healthSliderOriginalScale * scale;
                
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        
        // Ensure scale returns to normal when pulse stops
        healthSlider.transform.localScale = healthSliderOriginalScale;
        criticalPulseCoroutine = null;
    }
}
