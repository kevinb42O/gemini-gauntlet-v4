// ============================================================================
// HOLOGRAPHIC HAND CONTROLLER
// Automatically applies holographic shader to robot hands with level-based colors
// Supports dynamic effects: energy pulse, damage glitch, powerup boost
// ============================================================================

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HolographicHandController : MonoBehaviour
{
    [Header("Hand Configuration")]
    [Tooltip("Which hand level this is (1-4)")]
    [Range(1, 4)]
    public int handLevel = 1;
    
    [Tooltip("Is this a left hand?")]
    public bool isLeftHand = false;
    
    [Header("Shader Material")]
    [Tooltip("Assign the HolographicEnergyScan shader material here")]
    public Material holographicMaterial;
    
    [Header("Level-Based Colors")]
    [Tooltip("Color scheme for each hand level")]
    public HandLevelColors[] levelColors = new HandLevelColors[]
    {
        new HandLevelColors { level = 1, baseColor = new Color(0.2f, 0.6f, 1.0f), emissionColor = new Color(0.5f, 1.0f, 1.5f), name = "Level 1 - Blue" },
        new HandLevelColors { level = 2, baseColor = new Color(0.2f, 1.0f, 0.4f), emissionColor = new Color(0.5f, 1.5f, 0.8f), name = "Level 2 - Green" },
        new HandLevelColors { level = 3, baseColor = new Color(0.8f, 0.2f, 1.0f), emissionColor = new Color(1.2f, 0.5f, 1.5f), name = "Level 3 - Purple" },
        new HandLevelColors { level = 4, baseColor = new Color(1.0f, 0.8f, 0.2f), emissionColor = new Color(1.5f, 1.2f, 0.5f), name = "Level 4 - Gold" }
    };
    
    [Header("Effect Settings")]
    [Range(0f, 10f)]
    public float scanLineSpeed = 1.5f;
    
    [Range(0f, 10f)]
    public float pulseSpeed = 2.0f;
    
    [Range(0f, 10f)]
    public float emissionIntensity = 2.0f;
    
    [Range(0f, 10f)]
    public float fresnelIntensity = 2.5f;
    
    [Header("Dynamic Effects")]
    [Tooltip("Enable glitch effect when taking damage")]
    public bool enableDamageGlitch = true;
    
    [Tooltip("Enable boost effect when powerup active")]
    public bool enablePowerupBoost = true;
    
    [Header("Auto-Find Renderers")]
    [Tooltip("Automatically find all renderers in children")]
    public bool autoFindRenderers = true;
    
    [Tooltip("Manually assigned renderers (if not auto-finding)")]
    public Renderer[] targetRenderers;
    
    // Private variables
    private Material[] instanceMaterials;
    private float baseEmissionIntensity;
    private float baseScanLineSpeed;
    private bool isGlitching = false;
    private bool isBoosted = false;
    
    // Material property IDs (cached for performance)
    private static readonly int ColorID = Shader.PropertyToID("_Color");
    private static readonly int HoloColorID = Shader.PropertyToID("_HoloColor");
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");
    private static readonly int EmissionIntensityID = Shader.PropertyToID("_EmissionIntensity");
    private static readonly int ScanLineSpeedID = Shader.PropertyToID("_ScanLineSpeed");
    private static readonly int PulseSpeedID = Shader.PropertyToID("_PulseSpeed");
    private static readonly int FresnelIntensityID = Shader.PropertyToID("_FresnelIntensity");
    private static readonly int GlitchIntensityID = Shader.PropertyToID("_GlitchIntensity");
    private static readonly int UseGlitchID = Shader.PropertyToID("_UseGlitch");
    
    void Awake()
    {
        InitializeHolographicEffect();
    }
    
    void Start()
    {
        // Store base values
        baseEmissionIntensity = emissionIntensity;
        baseScanLineSpeed = scanLineSpeed;
        
        // Apply initial hand level colors
        SetHandLevelColors(handLevel);
    }
    
    /// <summary>
    /// Initialize holographic effect on all renderers
    /// </summary>
    private void InitializeHolographicEffect()
    {
        // Find renderers
        if (autoFindRenderers)
        {
            targetRenderers = GetComponentsInChildren<Renderer>();
            Debug.Log($"[HolographicHandController] Auto-found {targetRenderers.Length} renderers on {gameObject.name}");
        }
        
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            Debug.LogWarning($"[HolographicHandController] No renderers found on {gameObject.name}!");
            return;
        }
        
        // Create material instances
        List<Material> materials = new List<Material>();
        
        foreach (Renderer renderer in targetRenderers)
        {
            if (renderer == null) continue;
            
            // Create material instance
            Material[] mats = renderer.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (holographicMaterial != null)
                {
                    // Replace with holographic material
                    Material instanceMat = new Material(holographicMaterial);
                    mats[i] = instanceMat;
                    materials.Add(instanceMat);
                }
                else
                {
                    // Use existing material but make instance
                    Material instanceMat = new Material(mats[i]);
                    mats[i] = instanceMat;
                    materials.Add(instanceMat);
                }
            }
            renderer.materials = mats;
        }
        
        instanceMaterials = materials.ToArray();
        Debug.Log($"[HolographicHandController] Created {instanceMaterials.Length} material instances for {gameObject.name}");
    }
    
    /// <summary>
    /// Set colors based on hand level
    /// </summary>
    public void SetHandLevelColors(int level)
    {
        handLevel = Mathf.Clamp(level, 1, 4);
        
        if (instanceMaterials == null || instanceMaterials.Length == 0)
        {
            Debug.LogWarning($"[HolographicHandController] No materials to update on {gameObject.name}");
            return;
        }
        
        // Get colors for this level
        HandLevelColors colors = levelColors[handLevel - 1];
        
        // Apply to all materials
        foreach (Material mat in instanceMaterials)
        {
            if (mat == null) continue;
            
            mat.SetColor(HoloColorID, colors.baseColor);
            mat.SetColor(EmissionColorID, colors.emissionColor);
            mat.SetColor(ColorID, colors.baseColor * 0.8f);
        }
        
        Debug.Log($"[HolographicHandController] Applied {colors.name} to {gameObject.name}");
    }
    
    /// <summary>
    /// Update shader properties in real-time
    /// </summary>
    void Update()
    {
        if (instanceMaterials == null || instanceMaterials.Length == 0) return;
        
        foreach (Material mat in instanceMaterials)
        {
            if (mat == null) continue;
            
            mat.SetFloat(EmissionIntensityID, emissionIntensity);
            mat.SetFloat(ScanLineSpeedID, scanLineSpeed);
            mat.SetFloat(PulseSpeedID, pulseSpeed);
            mat.SetFloat(FresnelIntensityID, fresnelIntensity);
        }
    }
    
    // ============================================================================
    // PUBLIC API - Dynamic Effects
    // ============================================================================
    
    /// <summary>
    /// Trigger damage glitch effect
    /// </summary>
    public void TriggerDamageGlitch(float duration = 0.5f)
    {
        if (!enableDamageGlitch || isGlitching) return;
        StartCoroutine(DamageGlitchCoroutine(duration));
    }
    
    /// <summary>
    /// Trigger powerup boost effect
    /// </summary>
    public void TriggerPowerupBoost(float duration = 5f)
    {
        if (!enablePowerupBoost || isBoosted) return;
        StartCoroutine(PowerupBoostCoroutine(duration));
    }
    
    /// <summary>
    /// Set glitch intensity directly (for continuous effects)
    /// </summary>
    public void SetGlitchIntensity(float intensity)
    {
        if (instanceMaterials == null) return;
        
        foreach (Material mat in instanceMaterials)
        {
            if (mat == null) continue;
            mat.SetFloat(GlitchIntensityID, intensity);
            mat.SetFloat(UseGlitchID, intensity > 0.01f ? 1f : 0f);
        }
    }
    
    /// <summary>
    /// Boost emission and scan speed (for powerups)
    /// </summary>
    public void SetBoostMultiplier(float multiplier)
    {
        emissionIntensity = baseEmissionIntensity * multiplier;
        scanLineSpeed = baseScanLineSpeed * multiplier;
    }
    
    // ============================================================================
    // COROUTINES - Effect Animations
    // ============================================================================
    
    private IEnumerator DamageGlitchCoroutine(float duration)
    {
        isGlitching = true;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float intensity = Mathf.Lerp(1f, 0f, elapsed / duration);
            SetGlitchIntensity(intensity);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        SetGlitchIntensity(0f);
        isGlitching = false;
    }
    
    private IEnumerator PowerupBoostCoroutine(float duration)
    {
        isBoosted = true;
        
        // Ramp up
        float rampTime = 0.3f;
        float elapsed = 0f;
        while (elapsed < rampTime)
        {
            float t = elapsed / rampTime;
            SetBoostMultiplier(Mathf.Lerp(1f, 2f, t));
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Hold boost
        SetBoostMultiplier(2f);
        yield return new WaitForSeconds(duration - rampTime * 2f);
        
        // Ramp down
        elapsed = 0f;
        while (elapsed < rampTime)
        {
            float t = elapsed / rampTime;
            SetBoostMultiplier(Mathf.Lerp(2f, 1f, t));
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        SetBoostMultiplier(1f);
        isBoosted = false;
    }
    
    // ============================================================================
    // EDITOR HELPERS
    // ============================================================================
    
    void OnValidate()
    {
        // Clamp hand level
        handLevel = Mathf.Clamp(handLevel, 1, 4);
        
        // Update colors in editor
        if (Application.isPlaying && instanceMaterials != null && instanceMaterials.Length > 0)
        {
            SetHandLevelColors(handLevel);
        }
    }
    
    void OnDestroy()
    {
        // Clean up material instances
        if (instanceMaterials != null)
        {
            foreach (Material mat in instanceMaterials)
            {
                if (mat != null)
                {
                    Destroy(mat);
                }
            }
        }
    }
}

// ============================================================================
// DATA STRUCTURES
// ============================================================================

[System.Serializable]
public class HandLevelColors
{
    public int level;
    public string name;
    public Color baseColor;
    public Color emissionColor;
}
