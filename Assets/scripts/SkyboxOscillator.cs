using UnityEngine;

public class SkyboxOscillator : MonoBehaviour
{
    [Header("Oscillation Settings")]
    [Tooltip("Speed of oscillation in degrees per second")]
    public float oscillationSpeed = 30f;
    
    [Tooltip("Maximum rotation angle (will oscillate between +maxAngle and -maxAngle)")]
    public float maxAngle = 90f;
    
    [Header("Smoothing Settings")]
    [Tooltip("Use smooth sine wave oscillation instead of linear")]
    public bool useSmoothOscillation = true;
    
    [Tooltip("Easing curve for smooth oscillation (only used if useSmoothOscillation is true)")]
    public AnimationCurve easingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Advanced Settings")]
    [Tooltip("Use unscaled time (continues during time scale changes)")]
    public bool useUnscaledTime = true;
    
    [Tooltip("Pause oscillation when this is enabled")]
    public bool pauseOscillation = false;
    
    [Tooltip("Start from maximum angle instead of center")]
    public bool startFromMax = true;
    
    // Internal variables
    private float currentRotation = 0f;
    private float oscillationTime = 0f;
    private Material currentSkyboxMaterial;
    private bool movingTowardsNegative = false;
    
    void Start()
    {
        // Get the current skybox material
        UpdateSkyboxMaterial();
        
        // Initialize starting position
        if (startFromMax)
        {
            currentRotation = maxAngle;
            movingTowardsNegative = true;
        }
        else
        {
            currentRotation = 0f;
            movingTowardsNegative = true;
        }
        
        // Apply initial rotation
        ApplySkyboxRotation();
    }
    
    void Update()
    {
        // Skip oscillation if paused
        if (pauseOscillation)
            return;
            
        // Update skybox material reference in case it changed
        UpdateSkyboxMaterial();
        
        // Skip if no skybox material is available
        if (currentSkyboxMaterial == null)
            return;
            
        // Calculate delta time based on settings
        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        
        // Update oscillation
        UpdateOscillation(deltaTime);
        
        // Apply rotation to the skybox material
        ApplySkyboxRotation();
    }
    
    private void UpdateOscillation(float deltaTime)
    {
        if (useSmoothOscillation)
        {
            // Smooth sine wave oscillation
            oscillationTime += deltaTime * (oscillationSpeed / 180f); // Convert to appropriate time scale
            
            // Use sine wave to create smooth oscillation between -1 and 1
            float normalizedValue = Mathf.Sin(oscillationTime * Mathf.PI);
            
            // Apply easing curve if provided
            if (easingCurve != null && easingCurve.keys.Length > 0)
            {
                // Convert sine value from [-1,1] to [0,1] for curve evaluation
                float curveInput = (normalizedValue + 1f) * 0.5f;
                float curveOutput = easingCurve.Evaluate(curveInput);
                // Convert back to [-1,1] range
                normalizedValue = (curveOutput * 2f) - 1f;
            }
            
            // Apply to rotation
            currentRotation = normalizedValue * maxAngle;
        }
        else
        {
            // Linear oscillation
            float rotationIncrement = oscillationSpeed * deltaTime;
            
            if (movingTowardsNegative)
            {
                currentRotation -= rotationIncrement;
                if (currentRotation <= -maxAngle)
                {
                    currentRotation = -maxAngle;
                    movingTowardsNegative = false;
                }
            }
            else
            {
                currentRotation += rotationIncrement;
                if (currentRotation >= maxAngle)
                {
                    currentRotation = maxAngle;
                    movingTowardsNegative = true;
                }
            }
        }
    }
    
    private void UpdateSkyboxMaterial()
    {
        // Get the current skybox material from RenderSettings
        if (RenderSettings.skybox != currentSkyboxMaterial)
        {
            currentSkyboxMaterial = RenderSettings.skybox;
        }
    }
    
    private void ApplySkyboxRotation()
    {
        if (currentSkyboxMaterial == null)
            return;
            
        // Apply Y-axis rotation to skybox material
        // Different skybox shaders use different property names
        if (currentSkyboxMaterial.HasProperty("_Rotation"))
        {
            // For Skybox/Cubemap shader
            currentSkyboxMaterial.SetFloat("_Rotation", currentRotation);
        }
        else if (currentSkyboxMaterial.HasProperty("_RotationMatrix"))
        {
            // For custom skybox shaders that support full 3D rotation
            Quaternion rotation = Quaternion.Euler(0f, currentRotation, 0f);
            Matrix4x4 rotationMatrix = Matrix4x4.Rotate(rotation);
            currentSkyboxMaterial.SetMatrix("_RotationMatrix", rotationMatrix);
        }
        else
        {
            // Fallback: try to set common rotation properties
            if (currentSkyboxMaterial.HasProperty("_SunSize"))
            {
                // This is likely a procedural skybox, apply Y rotation
                currentSkyboxMaterial.SetFloat("_Rotation", currentRotation);
            }
        }
    }
    
    [Header("Runtime Controls")]
    [Space(10)]
    [Tooltip("Click to reset oscillation to starting position")]
    public bool resetOscillation = false;
    
    void OnValidate()
    {
        // Handle reset button in inspector
        if (resetOscillation)
        {
            ResetOscillation();
            resetOscillation = false;
        }
        
        // Ensure maxAngle is positive
        if (maxAngle < 0f)
            maxAngle = 0f;
            
        // Ensure oscillationSpeed is positive
        if (oscillationSpeed < 0f)
            oscillationSpeed = 0f;
    }
    
    /// <summary>
    /// Reset oscillation to starting position
    /// </summary>
    public void ResetOscillation()
    {
        oscillationTime = 0f;
        
        if (startFromMax)
        {
            currentRotation = maxAngle;
            movingTowardsNegative = true;
        }
        else
        {
            currentRotation = 0f;
            movingTowardsNegative = true;
        }
        
        if (Application.isPlaying)
        {
            ApplySkyboxRotation();
        }
    }
    
    /// <summary>
    /// Set oscillation speed at runtime
    /// </summary>
    /// <param name="newSpeed">New oscillation speed in degrees per second</param>
    public void SetOscillationSpeed(float newSpeed)
    {
        oscillationSpeed = Mathf.Max(0f, newSpeed);
    }
    
    /// <summary>
    /// Set maximum oscillation angle at runtime
    /// </summary>
    /// <param name="newMaxAngle">New maximum angle in degrees</param>
    public void SetMaxAngle(float newMaxAngle)
    {
        maxAngle = Mathf.Max(0f, newMaxAngle);
    }
    
    /// <summary>
    /// Toggle pause state
    /// </summary>
    public void TogglePause()
    {
        pauseOscillation = !pauseOscillation;
    }
    
    /// <summary>
    /// Set pause state
    /// </summary>
    /// <param name="pause">True to pause, false to resume</param>
    public void SetPause(bool pause)
    {
        pauseOscillation = pause;
    }
    
    /// <summary>
    /// Get current rotation value
    /// </summary>
    /// <returns>Current rotation in degrees</returns>
    public float GetCurrentRotation()
    {
        return currentRotation;
    }
    
    /// <summary>
    /// Get oscillation progress (0 to 1, where 0.5 is center)
    /// </summary>
    /// <returns>Oscillation progress from 0 to 1</returns>
    public float GetOscillationProgress()
    {
        if (maxAngle == 0f) return 0.5f;
        return (currentRotation + maxAngle) / (2f * maxAngle);
    }
}
