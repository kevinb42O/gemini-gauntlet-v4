using UnityEngine;
using System.Collections;

/// <summary>
/// Capture Lighting Controller - Spectacular lighting transitions during platform capture
/// Creates moody, evil atmosphere that gradually shifts as capture progresses
/// </summary>
public class CaptureLightingController : MonoBehaviour
{
    [Header("Light References")]
    [Tooltip("Main directional light (sun/moon)")]
    public Light directionalLight;
    
    [Tooltip("Additional lights to control (point lights, spotlights, etc.)")]
    public Light[] additionalLights;
    
    [Header("Fog Settings")]
    [Tooltip("Control Unity fog during capture")]
    public bool controlFog = true;
    
    [Header("Pre-Capture State (Evil/Tense)")]
    [Tooltip("Directional light color before capture starts")]
    public Color preCaptureDirectionalColor = new Color(0.6f, 0.2f, 0.2f, 1f); // Dark red
    
    [Tooltip("Directional light intensity before capture")]
    [Range(0f, 2f)] public float preCaptureDirectionalIntensity = 0.3f;
    
    [Tooltip("Ambient light color before capture")]
    public Color preCaptureAmbientColor = new Color(0.15f, 0.05f, 0.05f, 1f); // Very dark red
    
    [Tooltip("Fog color before capture")]
    public Color preCaptureFogColor = new Color(0.2f, 0.05f, 0.05f, 1f); // Dark red fog
    
    [Tooltip("Fog density before capture")]
    [Range(0f, 0.1f)] public float preCaptureFogDensity = 0.02f;
    
    [Header("During Capture (Gradual Transition)")]
    [Tooltip("Light color at 25% progress")]
    public Color capture25Color = new Color(0.5f, 0.2f, 0.3f, 1f); // Purple-red
    
    [Tooltip("Light color at 50% progress")]
    public Color capture50Color = new Color(0.3f, 0.3f, 0.5f, 1f); // Purple-blue
    
    [Tooltip("Light color at 75% progress")]
    public Color capture75Color = new Color(0.2f, 0.4f, 0.6f, 1f); // Blue
    
    [Tooltip("Intensity increases gradually during capture")]
    [Range(0f, 2f)] public float captureMidIntensity = 0.6f;
    
    [Header("Post-Capture State (Victory/Bright)")]
    [Tooltip("Directional light color after capture completes")]
    public Color postCaptureDirectionalColor = new Color(1f, 0.95f, 0.8f, 1f); // Warm white
    
    [Tooltip("Directional light intensity after capture")]
    [Range(0f, 2f)] public float postCaptureDirectionalIntensity = 1.2f;
    
    [Tooltip("Ambient light color after capture")]
    public Color postCaptureAmbientColor = new Color(0.4f, 0.4f, 0.45f, 1f); // Bright neutral
    
    [Tooltip("Fog color after capture")]
    public Color postCaptureFogColor = new Color(0.5f, 0.6f, 0.7f, 1f); // Light blue fog
    
    [Tooltip("Fog density after capture")]
    [Range(0f, 0.1f)] public float postCaptureFogDensity = 0.005f;
    
    [Header("Dramatic Effects")]
    [Tooltip("Enable pulsing effect during capture")]
    public bool enablePulsing = true;
    
    [Tooltip("Pulse frequency (Hz)")]
    [Range(0.1f, 5f)] public float pulseFrequency = 0.5f;
    
    [Tooltip("Pulse intensity multiplier")]
    [Range(0f, 0.5f)] public float pulseIntensity = 0.15f;
    
    [Tooltip("Enable flickering at critical moments")]
    public bool enableFlickering = true;
    
    [Header("Victory Flash")]
    [Tooltip("Bright flash when capture completes")]
    public bool enableVictoryFlash = true;
    
    [Tooltip("Victory flash color")]
    public Color victoryFlashColor = Color.white;
    
    [Tooltip("Victory flash duration")]
    [Range(0.1f, 2f)] public float victoryFlashDuration = 0.5f;
    
    // Original lighting state (to restore if needed)
    private Color originalDirectionalColor;
    private float originalDirectionalIntensity;
    private Color originalAmbientColor;
    private Color originalFogColor;
    private float originalFogDensity;
    private bool originalFogEnabled;
    
    // Current state
    private bool isCapturing = false;
    private bool isCaptured = false;
    private float currentProgress = 0f;
    
    void Start()
    {
        // Store original lighting state
        if (directionalLight != null)
        {
            originalDirectionalColor = directionalLight.color;
            originalDirectionalIntensity = directionalLight.intensity;
        }
        
        originalAmbientColor = RenderSettings.ambientLight;
        originalFogColor = RenderSettings.fogColor;
        originalFogDensity = RenderSettings.fogDensity;
        originalFogEnabled = RenderSettings.fog;
        
        // Set initial evil/tense atmosphere
        SetPreCaptureAtmosphere();
        
        Debug.Log("[CaptureLightingController] Initialized - Evil atmosphere set!");
    }
    
    /// <summary>
    /// Set the dark, evil pre-capture atmosphere
    /// </summary>
    public void SetPreCaptureAtmosphere()
    {
        if (isCaptured) return;
        
        StartCoroutine(TransitionToPreCapture());
    }
    
    private IEnumerator TransitionToPreCapture()
    {
        float duration = 2f;
        float elapsed = 0f;
        
        Color startDirColor = directionalLight != null ? directionalLight.color : Color.white;
        float startDirIntensity = directionalLight != null ? directionalLight.intensity : 1f;
        Color startAmbient = RenderSettings.ambientLight;
        Color startFog = RenderSettings.fogColor;
        float startFogDensity = RenderSettings.fogDensity;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Smooth transition
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            
            // Directional light
            if (directionalLight != null)
            {
                directionalLight.color = Color.Lerp(startDirColor, preCaptureDirectionalColor, smoothT);
                directionalLight.intensity = Mathf.Lerp(startDirIntensity, preCaptureDirectionalIntensity, smoothT);
            }
            
            // Ambient light
            RenderSettings.ambientLight = Color.Lerp(startAmbient, preCaptureAmbientColor, smoothT);
            
            // Fog
            if (controlFog)
            {
                RenderSettings.fog = true;
                RenderSettings.fogColor = Color.Lerp(startFog, preCaptureFogColor, smoothT);
                RenderSettings.fogDensity = Mathf.Lerp(startFogDensity, preCaptureFogDensity, smoothT);
            }
            
            yield return null;
        }
        
        Debug.Log("[CaptureLightingController] 🌑 Pre-capture atmosphere set - EVIL MODE ACTIVATED");
    }
    
    /// <summary>
    /// Update lighting based on capture progress (0 to 1)
    /// </summary>
    public void UpdateCaptureProgress(float progress)
    {
        if (isCaptured) return;
        
        currentProgress = Mathf.Clamp01(progress);
        
        if (!isCapturing && progress > 0f)
        {
            isCapturing = true;
            Debug.Log("[CaptureLightingController] 🎨 Capture lighting transition started!");
        }
        
        if (isCapturing)
        {
            ApplyProgressiveLighting(currentProgress);
        }
    }
    
    /// <summary>
    /// Apply lighting based on capture progress with gradual color shifts
    /// </summary>
    private void ApplyProgressiveLighting(float progress)
    {
        // Determine which color range we're in
        Color targetColor;
        Color targetAmbient;
        float targetIntensity;
        
        if (progress < 0.25f)
        {
            // 0-25%: Dark red → Purple-red
            float t = progress / 0.25f;
            targetColor = Color.Lerp(preCaptureDirectionalColor, capture25Color, t);
            targetAmbient = Color.Lerp(preCaptureAmbientColor, capture25Color * 0.3f, t);
            targetIntensity = Mathf.Lerp(preCaptureDirectionalIntensity, captureMidIntensity * 0.5f, t);
        }
        else if (progress < 0.5f)
        {
            // 25-50%: Purple-red → Purple-blue
            float t = (progress - 0.25f) / 0.25f;
            targetColor = Color.Lerp(capture25Color, capture50Color, t);
            targetAmbient = Color.Lerp(capture25Color * 0.3f, capture50Color * 0.4f, t);
            targetIntensity = Mathf.Lerp(captureMidIntensity * 0.5f, captureMidIntensity * 0.75f, t);
        }
        else if (progress < 0.75f)
        {
            // 50-75%: Purple-blue → Blue
            float t = (progress - 0.5f) / 0.25f;
            targetColor = Color.Lerp(capture50Color, capture75Color, t);
            targetAmbient = Color.Lerp(capture50Color * 0.4f, capture75Color * 0.5f, t);
            targetIntensity = Mathf.Lerp(captureMidIntensity * 0.75f, captureMidIntensity, t);
        }
        else
        {
            // 75-100%: Blue → Bright white
            float t = (progress - 0.75f) / 0.25f;
            targetColor = Color.Lerp(capture75Color, postCaptureDirectionalColor, t);
            targetAmbient = Color.Lerp(capture75Color * 0.5f, postCaptureAmbientColor, t);
            targetIntensity = Mathf.Lerp(captureMidIntensity, postCaptureDirectionalIntensity, t);
        }
        
        // Apply pulsing effect
        if (enablePulsing)
        {
            float pulse = Mathf.Sin(Time.time * pulseFrequency * Mathf.PI * 2f) * pulseIntensity;
            targetIntensity += pulse;
        }
        
        // Apply flickering at critical moments (25%, 50%, 75%)
        if (enableFlickering)
        {
            float milestone = Mathf.Floor(progress * 4f) / 4f; // 0, 0.25, 0.5, 0.75
            float distanceToMilestone = Mathf.Abs(progress - milestone);
            
            if (distanceToMilestone < 0.02f) // Within 2% of milestone
            {
                float flicker = Random.Range(0.8f, 1.2f);
                targetIntensity *= flicker;
            }
        }
        
        // Apply to lights
        if (directionalLight != null)
        {
            directionalLight.color = targetColor;
            directionalLight.intensity = targetIntensity;
        }
        
        RenderSettings.ambientLight = targetAmbient;
        
        // Fog transitions
        if (controlFog)
        {
            Color targetFog = Color.Lerp(preCaptureFogColor, postCaptureFogColor, progress);
            float targetFogDensity = Mathf.Lerp(preCaptureFogDensity, postCaptureFogDensity, progress);
            
            RenderSettings.fogColor = targetFog;
            RenderSettings.fogDensity = targetFogDensity;
        }
        
        // Additional lights
        foreach (Light light in additionalLights)
        {
            if (light != null)
            {
                light.color = targetColor;
                light.intensity = targetIntensity * 0.5f; // Half intensity for additional lights
            }
        }
    }
    
    /// <summary>
    /// Trigger the victory sequence with dramatic flash
    /// </summary>
    public void OnCaptureComplete()
    {
        if (isCaptured) return;
        
        isCaptured = true;
        isCapturing = false;
        
        Debug.Log("[CaptureLightingController] 🎉 CAPTURE COMPLETE - VICTORY LIGHTING!");
        
        StartCoroutine(VictoryLightingSequence());
    }
    
    /// <summary>
    /// Spectacular victory lighting sequence
    /// </summary>
    private IEnumerator VictoryLightingSequence()
    {
        // 1. Bright flash
        if (enableVictoryFlash)
        {
            Color preFlashColor = directionalLight != null ? directionalLight.color : Color.white;
            float preFlashIntensity = directionalLight != null ? directionalLight.intensity : 1f;
            
            // Flash to white
            if (directionalLight != null)
            {
                directionalLight.color = victoryFlashColor;
                directionalLight.intensity = postCaptureDirectionalIntensity * 2f;
            }
            
            RenderSettings.ambientLight = victoryFlashColor * 0.8f;
            
            // Hold flash
            yield return new WaitForSeconds(victoryFlashDuration * 0.3f);
            
            // Fade from flash to final state
            float flashFadeDuration = victoryFlashDuration * 0.7f;
            float elapsed = 0f;
            
            while (elapsed < flashFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / flashFadeDuration;
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                
                if (directionalLight != null)
                {
                    directionalLight.color = Color.Lerp(victoryFlashColor, postCaptureDirectionalColor, smoothT);
                    directionalLight.intensity = Mathf.Lerp(postCaptureDirectionalIntensity * 2f, postCaptureDirectionalIntensity, smoothT);
                }
                
                RenderSettings.ambientLight = Color.Lerp(victoryFlashColor * 0.8f, postCaptureAmbientColor, smoothT);
                
                yield return null;
            }
        }
        else
        {
            // No flash, just smooth transition
            yield return StartCoroutine(TransitionToVictoryLighting());
        }
        
        // 2. Set final bright, victorious atmosphere
        if (directionalLight != null)
        {
            directionalLight.color = postCaptureDirectionalColor;
            directionalLight.intensity = postCaptureDirectionalIntensity;
        }
        
        RenderSettings.ambientLight = postCaptureAmbientColor;
        
        if (controlFog)
        {
            RenderSettings.fogColor = postCaptureFogColor;
            RenderSettings.fogDensity = postCaptureFogDensity;
        }
        
        // Additional lights
        foreach (Light light in additionalLights)
        {
            if (light != null)
            {
                light.color = postCaptureDirectionalColor;
                light.intensity = postCaptureDirectionalIntensity * 0.5f;
            }
        }
        
        Debug.Log("[CaptureLightingController] ☀️ Victory lighting complete - BRIGHT AND GLORIOUS!");
    }
    
    private IEnumerator TransitionToVictoryLighting()
    {
        float duration = 2f;
        float elapsed = 0f;
        
        Color startColor = directionalLight != null ? directionalLight.color : Color.white;
        float startIntensity = directionalLight != null ? directionalLight.intensity : 1f;
        Color startAmbient = RenderSettings.ambientLight;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            
            if (directionalLight != null)
            {
                directionalLight.color = Color.Lerp(startColor, postCaptureDirectionalColor, smoothT);
                directionalLight.intensity = Mathf.Lerp(startIntensity, postCaptureDirectionalIntensity, smoothT);
            }
            
            RenderSettings.ambientLight = Color.Lerp(startAmbient, postCaptureAmbientColor, smoothT);
            
            yield return null;
        }
    }
    
    /// <summary>
    /// Reset to original lighting (for testing)
    /// </summary>
    public void ResetToOriginal()
    {
        if (directionalLight != null)
        {
            directionalLight.color = originalDirectionalColor;
            directionalLight.intensity = originalDirectionalIntensity;
        }
        
        RenderSettings.ambientLight = originalAmbientColor;
        RenderSettings.fogColor = originalFogColor;
        RenderSettings.fogDensity = originalFogDensity;
        RenderSettings.fog = originalFogEnabled;
        
        isCapturing = false;
        isCaptured = false;
        currentProgress = 0f;
        
        Debug.Log("[CaptureLightingController] Reset to original lighting");
    }
}
