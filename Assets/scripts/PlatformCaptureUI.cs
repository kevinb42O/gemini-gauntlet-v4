using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Platform Capture UI - Displays capture progress and Tower Protector Cube health
/// Assign sliders in the inspector - this script controls both
/// </summary>
public class PlatformCaptureUI : MonoBehaviour
{
    [Header("UI References - ASSIGN THESE")]
    [Tooltip("The slider GameObject that shows capture progress")]
    public Slider captureSlider;
    
    [Tooltip("The slider GameObject that shows Tower Protector Cube health")]
    public Slider cubeHealthSlider;
    
    [Tooltip("Optional: Parent container for organization (NOT used for show/hide)")]
    public GameObject uiContainer;
    
    [Header("Visual Settings")]
    [Tooltip("Color when capturing")]
    public Color capturingColor = Color.cyan;
    
    [Tooltip("Color when complete")]
    public Color completeColor = Color.green;
    
    [Tooltip("Color for cube health (high health)")]
    public Color cubeHealthHighColor = Color.green;
    
    [Tooltip("Color for cube health (low health)")]
    public Color cubeHealthLowColor = Color.red;
    
    [Tooltip("Color for cube health when friendly")]
    public Color cubeHealthFriendlyColor = new Color(0.2f, 1f, 0.8f, 1f); // Cyan-green
    
    private Image sliderFillImage;
    private Image cubeHealthFillImage;
    private bool isCubeHealthVisible = false;
    
    void Awake()
    {
        // Cache fill image reference for capture slider
        if (captureSlider != null && captureSlider.fillRect != null)
        {
            sliderFillImage = captureSlider.fillRect.GetComponent<Image>();
        }
        
        // Cache fill image reference for cube health slider
        if (cubeHealthSlider != null && cubeHealthSlider.fillRect != null)
        {
            cubeHealthFillImage = cubeHealthSlider.fillRect.GetComponent<Image>();
        }
        
        // Start hidden
        Hide();
        HideCubeHealth();
    }
    
    /// <summary>
    /// Show the capture progress slider
    /// </summary>
    public void Show()
    {
        if (captureSlider != null)
        {
            captureSlider.gameObject.SetActive(true);
        }
    }
    
    /// <summary>
    /// Hide the capture progress slider
    /// </summary>
    public void Hide()
    {
        if (captureSlider != null)
        {
            captureSlider.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Check if capture slider is visible
    /// </summary>
    public bool IsVisible()
    {
        return captureSlider != null && captureSlider.gameObject.activeSelf;
    }
    
    /// <summary>
    /// Update capture progress (0 to 1)
    /// </summary>
    public void UpdateProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);
        
        if (captureSlider != null)
        {
            captureSlider.value = progress;
        }
        
        // Update fill color (gradient from capturing to complete)
        if (sliderFillImage != null)
        {
            sliderFillImage.color = Color.Lerp(capturingColor, completeColor, progress);
        }
    }
    
    /// <summary>
    /// Initialize the UI (optional - for compatibility)
    /// </summary>
    public void Initialize(string missionName, float duration)
    {
        // This method exists for compatibility with PlatformCaptureSystem
        // You can add text labels here if you want to display mission name/duration
        Debug.Log($"[PlatformCaptureUI] Initialized: {missionName} - {duration}s");
    }
    
    /// <summary>
    /// Show the Tower Protector Cube health slider
    /// </summary>
    public void ShowCubeHealth()
    {
        if (cubeHealthSlider != null)
        {
            cubeHealthSlider.gameObject.SetActive(true);
            isCubeHealthVisible = true;
        }
    }
    
    /// <summary>
    /// Hide the Tower Protector Cube health slider
    /// </summary>
    public void HideCubeHealth()
    {
        if (cubeHealthSlider != null)
        {
            cubeHealthSlider.gameObject.SetActive(false);
            isCubeHealthVisible = false;
        }
    }
    
    /// <summary>
    /// Update Tower Protector Cube health (0 to 1)
    /// </summary>
    public void UpdateCubeHealth(float healthPercent, bool isFriendly = false)
    {
        healthPercent = Mathf.Clamp01(healthPercent);
        
        if (cubeHealthSlider != null)
        {
            cubeHealthSlider.value = healthPercent;
        }
        
        // Update fill color based on health and friendly state
        if (cubeHealthFillImage != null)
        {
            if (isFriendly)
            {
                // Friendly cube - use special color
                cubeHealthFillImage.color = cubeHealthFriendlyColor;
            }
            else
            {
                // Hostile cube - gradient from red (low) to green (high)
                cubeHealthFillImage.color = Color.Lerp(cubeHealthLowColor, cubeHealthHighColor, healthPercent);
            }
        }
    }
    
    /// <summary>
    /// Check if cube health UI is visible
    /// </summary>
    public bool IsCubeHealthVisible()
    {
        return isCubeHealthVisible;
    }
}
