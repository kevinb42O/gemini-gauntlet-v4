using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MenuSkyboxChanger : MonoBehaviour
{
    [Header("Skybox Collection")]
    [Tooltip("Array of skybox materials to cycle through")]
    public Material[] skyboxMaterials;
    
    [Tooltip("Array of skybox textures (will auto-generate materials if materials array is empty)")]
    public Texture[] skyboxTextures;
    
    [Header("Button Controls")]
    [Tooltip("Button to go to next skybox")]
    public Button nextSkyboxButton;
    
    [Tooltip("Button to go to previous skybox")]
    public Button previousSkyboxButton;
    
    [Tooltip("Button to reset to first skybox")]
    public Button resetSkyboxButton;
    
    [Header("Skybox Selection Buttons")]
    [Tooltip("Array of buttons for direct skybox selection (optional)")]
    public Button[] directSelectionButtons;
    
    [Header("Animation Settings")]
    [Tooltip("Enable smooth transition between skyboxes")]
    public bool enableSmoothTransition = true;
    
    [Tooltip("Duration of the transition in seconds")]
    public float transitionDuration = 1.0f;
    
    [Tooltip("Transition curve for smooth skybox changes")]
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Audio Settings")]
    [Tooltip("Audio source for button click sounds (optional)")]
    public AudioSource audioSource;
    
    [Tooltip("Sound to play when changing skybox")]
    public AudioClip skyboxChangeSound;
    
    [Header("UI Feedback")]
    [Tooltip("TextMeshPro component to display current skybox name (optional)")]
    public TextMeshProUGUI skyboxNameText;
    
    [Tooltip("Names for each skybox (for UI display)")]
    public string[] skyboxNames;
    
    [Tooltip("Image component to show skybox preview (optional)")]
    public Image skyboxPreviewImage;
    
    [Tooltip("Preview sprites for each skybox (optional)")]
    public Sprite[] skyboxPreviewSprites;
    
    // Private variables
    private int currentSkyboxIndex = 0;
    private Material[] generatedMaterials;
    private bool isTransitioning = false;
    private Material originalSkybox;
    
    void Start()
    {
        // Store original skybox
        originalSkybox = RenderSettings.skybox;
        
        // Initialize skybox materials
        InitializeSkyboxMaterials();
        
        // Set up button listeners
        SetupButtonListeners();
        
        // Sync with SkyboxManager if available
        SyncWithSkyboxManager();
        
        // Set initial skybox if no previous selection
        if (GetSkyboxMaterialsArray().Length > 0 && currentSkyboxIndex == 0)
        {
            SetSkybox(0, false);
        }
        
        // Update UI
        UpdateUI();
    }
    
    private void InitializeSkyboxMaterials()
    {
        // If no materials are assigned but textures are, generate materials
        if ((skyboxMaterials == null || skyboxMaterials.Length == 0) && 
            skyboxTextures != null && skyboxTextures.Length > 0)
        {
            generatedMaterials = new Material[skyboxTextures.Length];
            
            for (int i = 0; i < skyboxTextures.Length; i++)
            {
                if (skyboxTextures[i] != null)
                {
                    Material skyboxMat = new Material(Shader.Find("Skybox/Cubemap"));
                    skyboxMat.SetTexture("_Tex", skyboxTextures[i]);
                    skyboxMat.name = $"Generated Skybox Material {i}";
                    generatedMaterials[i] = skyboxMat;
                }
                else
                {
                    Debug.LogWarning($"Skybox texture at index {i} is null!");
                }
            }
        }
        
        // Validate materials
        Material[] materials = GetSkyboxMaterialsArray();
        if (materials.Length == 0)
        {
            Debug.LogError("No skybox materials or textures assigned to MenuSkyboxChanger!");
        }
    }
    
    private void SetupButtonListeners()
    {
        // Next button
        if (nextSkyboxButton != null)
        {
            nextSkyboxButton.onClick.AddListener(NextSkybox);
        }
        
        // Previous button
        if (previousSkyboxButton != null)
        {
            previousSkyboxButton.onClick.AddListener(PreviousSkybox);
        }
        
        // Reset button
        if (resetSkyboxButton != null)
        {
            resetSkyboxButton.onClick.AddListener(ResetToFirstSkybox);
        }
        
        // Direct selection buttons
        if (directSelectionButtons != null)
        {
            for (int i = 0; i < directSelectionButtons.Length; i++)
            {
                if (directSelectionButtons[i] != null)
                {
                    int index = i; // Capture for closure
                    directSelectionButtons[i].onClick.AddListener(() => SetSkybox(index));
                }
            }
        }
    }
    
    private void SyncWithSkyboxManager()
    {
        if (SkyboxManager.Instance != null && SkyboxManager.Instance.HasSelectedSkybox())
        {
            Material selectedSkybox = SkyboxManager.Instance.GetSelectedSkybox();
            Material[] materials = GetSkyboxMaterialsArray();
            
            // Find the index of the selected skybox in our materials array
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == selectedSkybox)
                {
                    currentSkyboxIndex = i;
                    RenderSettings.skybox = selectedSkybox;
                    Debug.Log($"Synced with SkyboxManager: Set to index {i}");
                    return;
                }
            }
        }
    }
    
    public Material[] GetSkyboxMaterialsArray()
    {
        if (skyboxMaterials != null && skyboxMaterials.Length > 0)
        {
            return skyboxMaterials;
        }
        else if (generatedMaterials != null && generatedMaterials.Length > 0)
        {
            return generatedMaterials;
        }
        return new Material[0];
    }
    
    /// <summary>
    /// Change to the next skybox in the array
    /// </summary>
    public void NextSkybox()
    {
        Material[] materials = GetSkyboxMaterialsArray();
        if (materials.Length > 0)
        {
            int nextIndex = (currentSkyboxIndex + 1) % materials.Length;
            SetSkybox(nextIndex);
        }
    }
    
    /// <summary>
    /// Change to the previous skybox in the array
    /// </summary>
    public void PreviousSkybox()
    {
        Material[] materials = GetSkyboxMaterialsArray();
        if (materials.Length > 0)
        {
            int prevIndex = (currentSkyboxIndex - 1 + materials.Length) % materials.Length;
            SetSkybox(prevIndex);
        }
    }
    
    /// <summary>
    /// Reset to the first skybox
    /// </summary>
    public void ResetToFirstSkybox()
    {
        SetSkybox(0);
    }
    
    /// <summary>
    /// Set skybox to a specific index
    /// </summary>
    /// <param name="index">Index of the skybox to set</param>
    /// <param name="useTransition">Whether to use smooth transition</param>
    public void SetSkybox(int index, bool useTransition = true)
    {
        Material[] materials = GetSkyboxMaterialsArray();
        
        if (materials.Length == 0)
        {
            Debug.LogWarning("No skybox materials available!");
            return;
        }
        
        // Clamp index to valid range
        index = Mathf.Clamp(index, 0, materials.Length - 1);
        
        if (materials[index] == null)
        {
            Debug.LogWarning($"Skybox material at index {index} is null!");
            return;
        }
        
        // Don't change if already on this skybox
        if (index == currentSkyboxIndex && RenderSettings.skybox == materials[index])
        {
            return;
        }
        
        currentSkyboxIndex = index;
        
        // Update UI immediately (especially preview image)
        UpdateUI();
        
        // Save selection to SkyboxManager for scene persistence (using material directly)
        if (SkyboxManager.Instance != null)
        {
            SkyboxManager.Instance.SetSelectedSkybox(materials[index]);
            Debug.Log($"MenuSkyboxChanger: Set SkyboxManager material to {materials[index].name}");
        }
        
        // Play sound effect
        PlaySkyboxChangeSound();
        
        // Apply skybox change
        if (enableSmoothTransition && useTransition && Application.isPlaying)
        {
            StartCoroutine(SmoothTransitionToSkybox(materials[index]));
        }
        else
        {
            RenderSettings.skybox = materials[index];
        }
        
        Debug.Log($"Skybox changed to index {index}: {materials[index].name}");
    }
    
    private IEnumerator SmoothTransitionToSkybox(Material targetSkybox)
    {
        if (isTransitioning) yield break;
        
        isTransitioning = true;
        Material startSkybox = RenderSettings.skybox;
        
        // Create a temporary material for blending
        Material blendMaterial = new Material(Shader.Find("Skybox/Cubemap"));
        
        float elapsedTime = 0f;
        
        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = elapsedTime / transitionDuration;
            
            // Apply easing curve
            if (transitionCurve != null)
            {
                progress = transitionCurve.Evaluate(progress);
            }
            
            // Simple transition by switching at halfway point
            // (True blending would require a custom shader)
            if (progress >= 0.5f)
            {
                RenderSettings.skybox = targetSkybox;
            }
            
            yield return null;
        }
        
        // Ensure final skybox is set
        RenderSettings.skybox = targetSkybox;
        
        // Clean up
        if (blendMaterial != null)
        {
            DestroyImmediate(blendMaterial);
        }
        
        isTransitioning = false;
    }
    
    private void PlaySkyboxChangeSound()
    {
        if (audioSource != null && skyboxChangeSound != null)
        {
            audioSource.PlayOneShot(skyboxChangeSound);
        }
    }
    
    private void UpdateUI()
    {
        // Update skybox name text
        if (skyboxNameText != null)
        {
            if (skyboxNames != null && currentSkyboxIndex < skyboxNames.Length && 
                !string.IsNullOrEmpty(skyboxNames[currentSkyboxIndex]))
            {
                skyboxNameText.text = skyboxNames[currentSkyboxIndex];
            }
            else
            {
                Material[] materials = GetSkyboxMaterialsArray();
                if (materials.Length > 0 && materials[currentSkyboxIndex] != null)
                {
                    skyboxNameText.text = materials[currentSkyboxIndex].name;
                }
                else
                {
                    skyboxNameText.text = $"Skybox {currentSkyboxIndex + 1}";
                }
            }
        }
        
        // Update preview image
        if (skyboxPreviewImage != null && skyboxPreviewSprites != null && 
            currentSkyboxIndex < skyboxPreviewSprites.Length && 
            skyboxPreviewSprites[currentSkyboxIndex] != null)
        {
            skyboxPreviewImage.sprite = skyboxPreviewSprites[currentSkyboxIndex];
        }
        
        // Update direct selection button states
        if (directSelectionButtons != null)
        {
            for (int i = 0; i < directSelectionButtons.Length; i++)
            {
                if (directSelectionButtons[i] != null)
                {
                    // You can add visual feedback here (e.g., different colors for selected button)
                    ColorBlock colors = directSelectionButtons[i].colors;
                    if (i == currentSkyboxIndex)
                    {
                        colors.normalColor = Color.yellow; // Highlight selected
                    }
                    else
                    {
                        colors.normalColor = Color.white; // Default color
                    }
                    directSelectionButtons[i].colors = colors;
                }
            }
        }
    }
    
    /// <summary>
    /// Get the current skybox index
    /// </summary>
    /// <returns>Current skybox index</returns>
    public int GetCurrentSkyboxIndex()
    {
        return currentSkyboxIndex;
    }
    
    /// <summary>
    /// Get all skybox names
    /// </summary>
    /// <returns>Array of skybox names</returns>
    public string[] GetAllSkyboxNames()
    {
        return skyboxNames;
    }
    
    /// <summary>
    /// Get the skybox names array (for SkyboxManager integration)
    /// </summary>
    /// <returns>Array of skybox names</returns>
    public string[] GetSkyboxNames()
    {
        return skyboxNames;
    }
    
    /// <summary>
    /// Get the total number of available skyboxes
    /// </summary>
    /// <returns>Total skybox count</returns>
    public int GetSkyboxCount()
    {
        return GetSkyboxMaterialsArray().Length;
    }
    
    /// <summary>
    /// Restore the original skybox
    /// </summary>
    public void RestoreOriginalSkybox()
    {
        if (originalSkybox != null)
        {
            RenderSettings.skybox = originalSkybox;
        }
    }
    
    void OnDestroy()
    {
        // Clean up generated materials
        if (generatedMaterials != null)
        {
            for (int i = 0; i < generatedMaterials.Length; i++)
            {
                if (generatedMaterials[i] != null)
                {
                    DestroyImmediate(generatedMaterials[i]);
                }
            }
        }
    }
}
