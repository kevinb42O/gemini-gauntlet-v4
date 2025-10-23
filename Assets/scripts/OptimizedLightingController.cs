using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Optimized lighting controller for laptop-friendly performance with AAA visuals.
/// Handles dynamic day/night cycles, ambient lighting, and fog effects.
/// Zero runtime cost when using baked lighting mode.
/// </summary>
public class OptimizedLightingController : MonoBehaviour
{
    [Header("Lighting Mode")]
    [Tooltip("Baked = Zero runtime cost, best performance. Dynamic = Real-time day/night cycle.")]
    public LightingMode lightingMode = LightingMode.Baked;

    [Header("Main Directional Light")]
    [Tooltip("Your main sun/moon light (Directional Light)")]
    public Light mainDirectionalLight;

    [Header("Day/Night Cycle (Dynamic Mode Only)")]
    [Tooltip("Enable automatic day/night cycle")]
    public bool enableDayNightCycle = false;
    
    [Tooltip("Length of full day in real-time seconds (e.g., 300 = 5 minutes)")]
    [Range(60f, 3600f)]
    public float dayLengthInSeconds = 300f;
    
    [Tooltip("Current time of day (0 = midnight, 0.25 = sunrise, 0.5 = noon, 0.75 = sunset, 1 = midnight)")]
    [Range(0f, 1f)]
    public float timeOfDay = 0.5f;

    [Header("Lighting Colors")]
    public Gradient sunColorGradient;
    public Gradient ambientColorGradient;
    public Gradient fogColorGradient;

    [Header("Light Intensity")]
    public AnimationCurve lightIntensityCurve;
    
    [Header("Fog Settings")]
    public bool enableFog = true;
    [Range(0f, 0.1f)]
    public float fogDensity = 0.01f;

    [Header("Ambient Lighting")]
    public AmbientMode ambientMode = AmbientMode.Skybox;
    
    [Header("Performance Settings")]
    [Tooltip("Update lighting every N frames (higher = better performance, less smooth transitions)")]
    [Range(1, 10)]
    public int updateEveryNFrames = 2;

    private int frameCounter = 0;

    public enum LightingMode
    {
        Baked,      // Zero runtime cost, pre-calculated
        Dynamic     // Real-time day/night cycle
    }

    private void Awake()
    {
        // Auto-find main directional light if not assigned
        if (mainDirectionalLight == null)
        {
            Light[] lights = FindObjectsOfType<Light>();
            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    mainDirectionalLight = light;
                    break;
                }
            }
        }

        // Initialize gradients if not set
        InitializeDefaultGradients();
        
        // Initialize curve if not set
        InitializeDefaultCurve();
    }

    private void Start()
    {
        // Apply initial lighting settings
        if (lightingMode == LightingMode.Dynamic)
        {
            UpdateLighting();
        }
        else
        {
            // Baked mode - disable dynamic light if it exists
            if (mainDirectionalLight != null)
            {
                mainDirectionalLight.enabled = false;
            }
        }

        // Setup fog
        RenderSettings.fog = enableFog;
        if (enableFog)
        {
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = fogDensity;
        }
    }

    private void Update()
    {
        // Only update in dynamic mode
        if (lightingMode != LightingMode.Dynamic) return;

        // Performance optimization: Update every N frames
        frameCounter++;
        if (frameCounter < updateEveryNFrames)
        {
            return;
        }
        frameCounter = 0;

        // Update day/night cycle
        if (enableDayNightCycle)
        {
            timeOfDay += Time.deltaTime / dayLengthInSeconds;
            if (timeOfDay >= 1f)
            {
                timeOfDay = 0f;
            }
        }

        UpdateLighting();
    }

    private void UpdateLighting()
    {
        if (mainDirectionalLight == null) return;

        // Calculate sun rotation (0 = midnight, 0.5 = noon)
        float sunAngle = timeOfDay * 360f - 90f; // -90 to start at horizon
        mainDirectionalLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);

        // Update light color and intensity
        mainDirectionalLight.color = sunColorGradient.Evaluate(timeOfDay);
        mainDirectionalLight.intensity = lightIntensityCurve.Evaluate(timeOfDay);

        // Update ambient lighting
        RenderSettings.ambientMode = ambientMode;
        if (ambientMode == AmbientMode.Flat)
        {
            RenderSettings.ambientLight = ambientColorGradient.Evaluate(timeOfDay);
        }

        // Update fog color
        if (enableFog)
        {
            RenderSettings.fogColor = fogColorGradient.Evaluate(timeOfDay);
        }
    }

    private void InitializeDefaultGradients()
    {
        if (sunColorGradient == null || sunColorGradient.colorKeys.Length == 0)
        {
            sunColorGradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[5];
            colorKeys[0] = new GradientColorKey(new Color(0.2f, 0.2f, 0.4f), 0f);    // Midnight - dark blue
            colorKeys[1] = new GradientColorKey(new Color(1f, 0.6f, 0.4f), 0.25f);   // Sunrise - orange
            colorKeys[2] = new GradientColorKey(new Color(1f, 1f, 0.9f), 0.5f);      // Noon - bright white
            colorKeys[3] = new GradientColorKey(new Color(1f, 0.5f, 0.3f), 0.75f);   // Sunset - orange/red
            colorKeys[4] = new GradientColorKey(new Color(0.2f, 0.2f, 0.4f), 1f);    // Midnight - dark blue

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);

            sunColorGradient.SetKeys(colorKeys, alphaKeys);
        }

        if (ambientColorGradient == null || ambientColorGradient.colorKeys.Length == 0)
        {
            ambientColorGradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[5];
            colorKeys[0] = new GradientColorKey(new Color(0.1f, 0.1f, 0.2f), 0f);    // Midnight
            colorKeys[1] = new GradientColorKey(new Color(0.5f, 0.4f, 0.3f), 0.25f); // Sunrise
            colorKeys[2] = new GradientColorKey(new Color(0.7f, 0.7f, 0.8f), 0.5f);  // Noon
            colorKeys[3] = new GradientColorKey(new Color(0.5f, 0.3f, 0.2f), 0.75f); // Sunset
            colorKeys[4] = new GradientColorKey(new Color(0.1f, 0.1f, 0.2f), 1f);    // Midnight

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);

            ambientColorGradient.SetKeys(colorKeys, alphaKeys);
        }

        if (fogColorGradient == null || fogColorGradient.colorKeys.Length == 0)
        {
            fogColorGradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[5];
            colorKeys[0] = new GradientColorKey(new Color(0.1f, 0.1f, 0.2f), 0f);    // Midnight
            colorKeys[1] = new GradientColorKey(new Color(0.8f, 0.6f, 0.5f), 0.25f); // Sunrise
            colorKeys[2] = new GradientColorKey(new Color(0.7f, 0.8f, 0.9f), 0.5f);  // Noon
            colorKeys[3] = new GradientColorKey(new Color(0.8f, 0.5f, 0.4f), 0.75f); // Sunset
            colorKeys[4] = new GradientColorKey(new Color(0.1f, 0.1f, 0.2f), 1f);    // Midnight

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);

            fogColorGradient.SetKeys(colorKeys, alphaKeys);
        }
    }

    private void InitializeDefaultCurve()
    {
        if (lightIntensityCurve == null || lightIntensityCurve.keys.Length == 0)
        {
            lightIntensityCurve = new AnimationCurve();
            lightIntensityCurve.AddKey(0f, 0f);      // Midnight - no light
            lightIntensityCurve.AddKey(0.25f, 0.8f); // Sunrise - medium
            lightIntensityCurve.AddKey(0.5f, 1.5f);  // Noon - bright
            lightIntensityCurve.AddKey(0.75f, 0.8f); // Sunset - medium
            lightIntensityCurve.AddKey(1f, 0f);      // Midnight - no light
        }
    }

    // Public methods for external control
    public void SetTimeOfDay(float time)
    {
        timeOfDay = Mathf.Clamp01(time);
        if (lightingMode == LightingMode.Dynamic)
        {
            UpdateLighting();
        }
    }

    public void SetLightingMode(LightingMode mode)
    {
        lightingMode = mode;
        if (mode == LightingMode.Baked && mainDirectionalLight != null)
        {
            mainDirectionalLight.enabled = false;
        }
        else if (mode == LightingMode.Dynamic && mainDirectionalLight != null)
        {
            mainDirectionalLight.enabled = true;
            UpdateLighting();
        }
    }

    public void ToggleDayNightCycle()
    {
        enableDayNightCycle = !enableDayNightCycle;
    }

    // Debug visualization
    private void OnValidate()
    {
        // Update lighting in editor when values change
        if (Application.isPlaying && lightingMode == LightingMode.Dynamic)
        {
            UpdateLighting();
        }
    }
}
