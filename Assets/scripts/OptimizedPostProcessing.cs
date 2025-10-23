using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Laptop-optimized post-processing controller for AAA visuals without killing performance.
/// Dynamically adjusts effects based on performance targets.
/// Requires Universal Render Pipeline (URP).
/// </summary>
[RequireComponent(typeof(Volume))]
public class OptimizedPostProcessing : MonoBehaviour
{
    [Header("Performance Profile")]
    [Tooltip("Laptop = Optimized for mobile/laptop GPUs. Desktop = Higher quality effects.")]
    public PerformanceProfile performanceProfile = PerformanceProfile.Laptop;

    [Header("Effect Toggles")]
    public bool enableBloom = true;
    public bool enableColorGrading = true;
    public bool enableVignette = true;
    public bool enableChromaticAberration = false; // Expensive, disabled by default
    public bool enableFilmGrain = true;
    public bool enableDepthOfField = false; // Very expensive, disabled by default
    public bool enableMotionBlur = false; // Expensive, disabled by default
    public bool enableAmbientOcclusion = false; // Very expensive, disabled by default

    [Header("Bloom Settings")]
    [Range(0f, 1f)]
    public float bloomIntensity = 0.3f;
    [Range(0f, 10f)]
    public float bloomThreshold = 1f;

    [Header("Color Grading")]
    [Range(-100f, 100f)]
    public float temperature = 0f;
    [Range(-100f, 100f)]
    public float tint = 0f;
    [Range(-100f, 100f)]
    public float saturation = 10f;
    [Range(-100f, 100f)]
    public float contrast = 5f;

    [Header("Vignette Settings")]
    [Range(0f, 1f)]
    public float vignetteIntensity = 0.3f;
    [Range(0f, 1f)]
    public float vignetteSmoothness = 0.4f;

    [Header("Film Grain")]
    [Range(0f, 1f)]
    public float filmGrainIntensity = 0.15f;

    [Header("Dynamic Quality Adjustment")]
    [Tooltip("Automatically reduce quality if FPS drops below target")]
    public bool enableDynamicQuality = true;
    [Tooltip("Target FPS for laptops (e.g., 60)")]
    public int targetFPS = 60;
    [Tooltip("Check FPS every N seconds")]
    public float fpsCheckInterval = 2f;

    private Volume volume;
    private Bloom bloom;
    private ColorAdjustments colorAdjustments;
    private Vignette vignette;
    private ChromaticAberration chromaticAberration;
    private FilmGrain filmGrain;
    private DepthOfField depthOfField;
    private MotionBlur motionBlur;

    private float fpsCheckTimer = 0f;
    private int frameCount = 0;
    private float fpsSum = 0f;

    public enum PerformanceProfile
    {
        Laptop,     // Optimized for integrated/mobile GPUs
        Desktop     // Higher quality for dedicated GPUs
    }

    private void Awake()
    {
        volume = GetComponent<Volume>();
        if (volume == null)
        {
            Debug.LogError("[OptimizedPostProcessing] Volume component not found!");
            return;
        }

        // Get or add volume profile
        if (volume.profile == null)
        {
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        }

        // Initialize effects
        InitializeEffects();
    }

    private void Start()
    {
        // Apply initial settings
        ApplyPerformanceProfile();
        UpdateAllEffects();
    }

    private void Update()
    {
        // Dynamic quality adjustment
        if (enableDynamicQuality)
        {
            fpsCheckTimer += Time.deltaTime;
            frameCount++;
            fpsSum += 1f / Time.unscaledDeltaTime;

            if (fpsCheckTimer >= fpsCheckInterval)
            {
                float avgFPS = fpsSum / frameCount;
                
                if (avgFPS < targetFPS * 0.8f) // 20% below target
                {
                    ReduceQuality();
                }
                else if (avgFPS > targetFPS * 1.1f) // 10% above target
                {
                    IncreaseQuality();
                }

                // Reset counters
                fpsCheckTimer = 0f;
                frameCount = 0;
                fpsSum = 0f;
            }
        }
    }

    private void InitializeEffects()
    {
        // Bloom
        if (!volume.profile.TryGet(out bloom))
        {
            bloom = volume.profile.Add<Bloom>();
        }

        // Color Adjustments
        if (!volume.profile.TryGet(out colorAdjustments))
        {
            colorAdjustments = volume.profile.Add<ColorAdjustments>();
        }

        // Vignette
        if (!volume.profile.TryGet(out vignette))
        {
            vignette = volume.profile.Add<Vignette>();
        }

        // Chromatic Aberration
        if (!volume.profile.TryGet(out chromaticAberration))
        {
            chromaticAberration = volume.profile.Add<ChromaticAberration>();
        }

        // Film Grain
        if (!volume.profile.TryGet(out filmGrain))
        {
            filmGrain = volume.profile.Add<FilmGrain>();
        }

        // Depth of Field
        if (!volume.profile.TryGet(out depthOfField))
        {
            depthOfField = volume.profile.Add<DepthOfField>();
        }

        // Motion Blur
        if (!volume.profile.TryGet(out motionBlur))
        {
            motionBlur = volume.profile.Add<MotionBlur>();
        }
    }

    private void ApplyPerformanceProfile()
    {
        switch (performanceProfile)
        {
            case PerformanceProfile.Laptop:
                // Disable expensive effects
                enableChromaticAberration = false;
                enableDepthOfField = false;
                enableMotionBlur = false;
                enableAmbientOcclusion = false;
                
                // Reduce bloom quality
                bloomIntensity = Mathf.Min(bloomIntensity, 0.3f);
                
                // Reduce film grain
                filmGrainIntensity = Mathf.Min(filmGrainIntensity, 0.15f);
                break;

            case PerformanceProfile.Desktop:
                // Allow more expensive effects if user enables them
                bloomIntensity = Mathf.Min(bloomIntensity, 0.5f);
                filmGrainIntensity = Mathf.Min(filmGrainIntensity, 0.25f);
                break;
        }
    }

    private void UpdateAllEffects()
    {
        UpdateBloom();
        UpdateColorGrading();
        UpdateVignette();
        UpdateChromaticAberration();
        UpdateFilmGrain();
        UpdateDepthOfField();
        UpdateMotionBlur();
    }

    private void UpdateBloom()
    {
        if (bloom == null) return;

        bloom.active = enableBloom;
        if (enableBloom)
        {
            bloom.intensity.value = bloomIntensity;
            bloom.threshold.value = bloomThreshold;
            bloom.scatter.value = 0.7f; // Good balance
        }
    }

    private void UpdateColorGrading()
    {
        if (colorAdjustments == null) return;

        colorAdjustments.active = enableColorGrading;
        if (enableColorGrading)
        {
            colorAdjustments.colorFilter.value = Color.white;
            colorAdjustments.postExposure.value = 0f;
            colorAdjustments.contrast.value = contrast;
            colorAdjustments.saturation.value = saturation;
            
            // Temperature and tint (hue shift approximation)
            float hueShift = temperature * 0.01f;
            colorAdjustments.hueShift.value = hueShift;
        }
    }

    private void UpdateVignette()
    {
        if (vignette == null) return;

        vignette.active = enableVignette;
        if (enableVignette)
        {
            vignette.intensity.value = vignetteIntensity;
            vignette.smoothness.value = vignetteSmoothness;
            vignette.color.value = Color.black;
        }
    }

    private void UpdateChromaticAberration()
    {
        if (chromaticAberration == null) return;

        chromaticAberration.active = enableChromaticAberration;
        if (enableChromaticAberration)
        {
            chromaticAberration.intensity.value = 0.2f; // Subtle effect
        }
    }

    private void UpdateFilmGrain()
    {
        if (filmGrain == null) return;

        filmGrain.active = enableFilmGrain;
        if (enableFilmGrain)
        {
            filmGrain.intensity.value = filmGrainIntensity;
            filmGrain.response.value = 0.8f;
        }
    }

    private void UpdateDepthOfField()
    {
        if (depthOfField == null) return;

        depthOfField.active = enableDepthOfField;
        // DOF settings would be configured here if enabled
    }

    private void UpdateMotionBlur()
    {
        if (motionBlur == null) return;

        motionBlur.active = enableMotionBlur;
        // Motion blur settings would be configured here if enabled
    }

    private void ReduceQuality()
    {
        Debug.Log("[OptimizedPostProcessing] FPS below target, reducing quality...");

        // Disable expensive effects first
        if (enableMotionBlur)
        {
            enableMotionBlur = false;
            UpdateMotionBlur();
            return;
        }

        if (enableDepthOfField)
        {
            enableDepthOfField = false;
            UpdateDepthOfField();
            return;
        }

        if (enableChromaticAberration)
        {
            enableChromaticAberration = false;
            UpdateChromaticAberration();
            return;
        }

        // Reduce bloom intensity
        if (bloomIntensity > 0.1f)
        {
            bloomIntensity -= 0.05f;
            UpdateBloom();
            return;
        }

        // Last resort: disable bloom
        if (enableBloom)
        {
            enableBloom = false;
            UpdateBloom();
        }
    }

    private void IncreaseQuality()
    {
        // Only increase if we're on Desktop profile
        if (performanceProfile != PerformanceProfile.Desktop) return;

        // Gradually re-enable effects
        if (!enableBloom)
        {
            enableBloom = true;
            bloomIntensity = 0.2f;
            UpdateBloom();
        }
        else if (bloomIntensity < 0.5f)
        {
            bloomIntensity += 0.05f;
            UpdateBloom();
        }
    }

    // Public methods for external control
    public void SetPerformanceProfile(PerformanceProfile profile)
    {
        performanceProfile = profile;
        ApplyPerformanceProfile();
        UpdateAllEffects();
    }

    public void SetBloomIntensity(float intensity)
    {
        bloomIntensity = Mathf.Clamp01(intensity);
        UpdateBloom();
    }

    public void SetVignetteIntensity(float intensity)
    {
        vignetteIntensity = Mathf.Clamp01(intensity);
        UpdateVignette();
    }

    public void ToggleEffect(string effectName, bool enabled)
    {
        switch (effectName.ToLower())
        {
            case "bloom":
                enableBloom = enabled;
                UpdateBloom();
                break;
            case "vignette":
                enableVignette = enabled;
                UpdateVignette();
                break;
            case "filmgrain":
                enableFilmGrain = enabled;
                UpdateFilmGrain();
                break;
            case "colorgrading":
                enableColorGrading = enabled;
                UpdateColorGrading();
                break;
        }
    }

    // Editor validation
    private void OnValidate()
    {
        if (Application.isPlaying && volume != null)
        {
            UpdateAllEffects();
        }
    }
}
