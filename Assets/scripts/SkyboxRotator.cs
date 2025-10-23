using UnityEngine;

public class SkyboxRotator : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("Speed of rotation in degrees per second")]
    public float rotationSpeed = 5f;
    
    [Header("Rotation Axes")]
    [Tooltip("Enable rotation around the X-axis")]
    public bool rotateX = false;
    
    [Tooltip("Enable rotation around the Y-axis (most common for skyboxes)")]
    public bool rotateY = true;
    
    [Tooltip("Enable rotation around the Z-axis")]
    public bool rotateZ = false;
    
    [Header("Direction Controls")]
    [Tooltip("Reverse the X-axis rotation direction")]
    public bool reverseX = false;
    
    [Tooltip("Reverse the Y-axis rotation direction")]
    public bool reverseY = false;
    
    [Tooltip("Reverse the Z-axis rotation direction")]
    public bool reverseZ = false;
    
    [Header("Advanced Settings")]
    [Tooltip("Use unscaled time (continues during time scale changes)")]
    public bool useUnscaledTime = true;
    
    [Tooltip("Pause rotation when this is enabled")]
    public bool pauseRotation = false;
    
    // Current rotation values
    private float currentRotationX = 0f;
    private float currentRotationY = 0f;
    private float currentRotationZ = 0f;
    
    // Reference to the current skybox material
    private Material currentSkyboxMaterial;
    
    // Track if we've shown warnings
    private bool hasShownXZWarning = false;
    
    void Start()
    {
        // Get the current skybox material
        UpdateSkyboxMaterial();
    }
    
    void Update()
    {
        // Skip rotation if paused
        if (pauseRotation)
            return;
            
        // Update skybox material reference in case it changed
        UpdateSkyboxMaterial();
        
        // Skip if no skybox material is available
        if (currentSkyboxMaterial == null)
            return;
            
        // Calculate delta time based on settings
        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        
        // Calculate rotation increments
        float rotationIncrement = rotationSpeed * deltaTime;
        
        // Update rotation values for each enabled axis
        if (rotateX)
        {
            currentRotationX += rotationIncrement * (reverseX ? -1f : 1f);
            currentRotationX = currentRotationX % 360f; // Keep within 0-360 range
            
            if (!hasShownXZWarning)
            {
                Debug.LogWarning("X-axis rotation is not supported by most skybox shaders. Only Y-axis rotation is commonly used.");
                hasShownXZWarning = true;
            }
        }
        
        if (rotateY)
        {
            currentRotationY += rotationIncrement * (reverseY ? -1f : 1f);
            currentRotationY = currentRotationY % 360f; // Keep within 0-360 range
        }
        
        if (rotateZ)
        {
            currentRotationZ += rotationIncrement * (reverseZ ? -1f : 1f);
            currentRotationZ = currentRotationZ % 360f; // Keep within 0-360 range
            
            if (!hasShownXZWarning)
            {
                Debug.LogWarning("Z-axis rotation is not supported by most skybox shaders. Only Y-axis rotation is commonly used.");
                hasShownXZWarning = true;
            }
        }
        
        // Apply rotation to the skybox material
        ApplySkyboxRotation();
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
            
        // Create rotation matrix from Euler angles
        Quaternion rotation = Quaternion.Euler(currentRotationX, currentRotationY, currentRotationZ);
        Matrix4x4 rotationMatrix = Matrix4x4.Rotate(rotation);
        
        // Apply rotation to skybox material
        // Different skybox shaders use different property names
        if (currentSkyboxMaterial.HasProperty("_Rotation"))
        {
            // For Skybox/Cubemap shader
            currentSkyboxMaterial.SetFloat("_Rotation", currentRotationY);
        }
        else if (currentSkyboxMaterial.HasProperty("_RotationMatrix"))
        {
            // For custom skybox shaders that support full 3D rotation
            currentSkyboxMaterial.SetMatrix("_RotationMatrix", rotationMatrix);
        }
        else
        {
            // Fallback: try to set common rotation properties
            if (currentSkyboxMaterial.HasProperty("_SunSize"))
            {
                // This is likely a procedural skybox, apply Y rotation
                currentSkyboxMaterial.SetFloat("_Rotation", currentRotationY);
            }
        }
    }
    
    [Header("Runtime Controls")]
    [Space(10)]
    [Tooltip("Click to reset all rotations to zero")]
    public bool resetRotation = false;
    
    void OnValidate()
    {
        // Handle reset button in inspector
        if (resetRotation)
        {
            ResetRotation();
            resetRotation = false;
        }
    }
    
    /// <summary>
    /// Reset all rotation values to zero
    /// </summary>
    public void ResetRotation()
    {
        currentRotationX = 0f;
        currentRotationY = 0f;
        currentRotationZ = 0f;
        
        if (Application.isPlaying)
        {
            ApplySkyboxRotation();
        }
    }
    
    /// <summary>
    /// Set rotation speed at runtime
    /// </summary>
    /// <param name="newSpeed">New rotation speed in degrees per second</param>
    public void SetRotationSpeed(float newSpeed)
    {
        rotationSpeed = newSpeed;
    }
    
    /// <summary>
    /// Toggle pause state
    /// </summary>
    public void TogglePause()
    {
        pauseRotation = !pauseRotation;
    }
    
    /// <summary>
    /// Set pause state
    /// </summary>
    /// <param name="pause">True to pause, false to resume</param>
    public void SetPause(bool pause)
    {
        pauseRotation = pause;
    }
    
    /// <summary>
    /// Set which axes should rotate
    /// </summary>
    /// <param name="x">Rotate around X axis</param>
    /// <param name="y">Rotate around Y axis</param>
    /// <param name="z">Rotate around Z axis</param>
    public void SetRotationAxes(bool x, bool y, bool z)
    {
        rotateX = x;
        rotateY = y;
        rotateZ = z;
    }
}
