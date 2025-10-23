using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Automatically creates visual segment dividers for the shield slider
/// Attach this to your Shield Slider GameObject
/// </summary>
[ExecuteInEditMode]
public class ShieldSliderSegments : MonoBehaviour
{
    [Header("Segment Configuration")]
    [Tooltip("Number of segments (plates) to divide the slider into")]
    [SerializeField] private int segmentCount = 3;
    
    // VEST SYSTEM: Track current segment count for dynamic updates
    private int currentSegmentCount = 1;
    
    [Header("Divider Appearance")]
    [Tooltip("Color of the divider lines")]
    [SerializeField] private Color dividerColor = new Color(0.15f, 0.15f, 0.15f, 1f); // Dark gray
    
    [Tooltip("Width of divider lines in pixels")]
    [SerializeField] private float dividerWidth = 3f;
    
    [Tooltip("Should dividers extend beyond slider height?")]
    [SerializeField] private float dividerHeightMultiplier = 1.2f;
    
    [Header("Background")]
    [Tooltip("Add a subtle background to show empty segments")]
    [SerializeField] private bool addSegmentBackground = true;
    
    [Tooltip("Color of empty segment background")]
    [SerializeField] private Color emptySegmentColor = new Color(0.1f, 0.1f, 0.15f, 0.3f); // Dark blue, semi-transparent
    
    private GameObject dividersContainer;
    private GameObject backgroundContainer;
    
    void Start()
    {
        if (Application.isPlaying)
        {
            // VEST SYSTEM: Start with 1 segment (T1 vest default)
            currentSegmentCount = 1;
            segmentCount = 1;
            CreateSegmentVisuals();
        }
    }
    
    /// <summary>
    /// VEST SYSTEM: Update segment count dynamically based on equipped vest
    /// Called by ArmorPlateSystem when vest changes
    /// </summary>
    public void UpdateSegmentCount(int newSegmentCount)
    {
        if (newSegmentCount == currentSegmentCount) return; // No change needed
        
        Debug.Log($"[ShieldSliderSegments] Updating segment count: {currentSegmentCount} -> {newSegmentCount}");
        
        currentSegmentCount = newSegmentCount;
        segmentCount = newSegmentCount;
        
        // Recreate visuals with new segment count
        CreateSegmentVisuals();
    }
    
    [ContextMenu("Create Segment Visuals")]
    public void CreateSegmentVisuals()
    {
        // Clean up existing visuals
        CleanupExistingVisuals();
        
        Slider slider = GetComponent<Slider>();
        if (slider == null)
        {
            Debug.LogError("[ShieldSliderSegments] No Slider component found on this GameObject!");
            return;
        }
        
        // Try to find Background first (most reliable)
        Transform background = transform.Find("Background");
        if (background == null)
        {
            Debug.LogError("[ShieldSliderSegments] Could not find 'Background' child! Make sure this is attached to a standard Unity Slider.");
            return;
        }
        
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        if (backgroundRect == null)
        {
            Debug.LogError("[ShieldSliderSegments] Background has no RectTransform!");
            return;
        }
        
        // Create containers as children of Background (always visible, not masked)
        dividersContainer = new GameObject("SegmentDividers");
        dividersContainer.transform.SetParent(background, false);
        
        RectTransform dividersRect = dividersContainer.AddComponent<RectTransform>();
        // Stretch to fill parent
        dividersRect.anchorMin = Vector2.zero;
        dividersRect.anchorMax = Vector2.one;
        dividersRect.sizeDelta = Vector2.zero;
        dividersRect.anchoredPosition = Vector2.zero;
        
        dividersContainer.transform.SetAsLastSibling(); // Render on top
        
        if (addSegmentBackground)
        {
            backgroundContainer = new GameObject("SegmentBackgrounds");
            backgroundContainer.transform.SetParent(background, false);
            
            RectTransform bgRect = backgroundContainer.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgRect.anchoredPosition = Vector2.zero;
            
            backgroundContainer.transform.SetAsFirstSibling(); // Render behind
        }
        
        // Use Background dimensions
        float sliderWidth = backgroundRect.rect.width;
        float sliderHeight = backgroundRect.rect.height;
        
        Debug.Log($"[ShieldSliderSegments] Background dimensions: {sliderWidth}x{sliderHeight}");
        
        // Create segment backgrounds
        if (addSegmentBackground)
        {
            CreateSegmentBackgrounds(sliderWidth, sliderHeight);
        }
        
        // Create divider lines between segments
        CreateDividerLines(sliderWidth, sliderHeight);
        
        Debug.Log($"[ShieldSliderSegments] ✅ Created {segmentCount} segments with {segmentCount - 1} dividers");
    }
    
    private void CreateSegmentBackgrounds(float sliderWidth, float sliderHeight)
    {
        float segmentWidth = sliderWidth / segmentCount;
        
        for (int i = 0; i < segmentCount; i++)
        {
            GameObject segment = new GameObject($"Segment_{i + 1}_Background");
            segment.transform.SetParent(backgroundContainer.transform, false);
            
            Image segmentImage = segment.AddComponent<Image>();
            segmentImage.color = emptySegmentColor;
            
            RectTransform segmentRect = segment.GetComponent<RectTransform>();
            segmentRect.anchorMin = new Vector2(0, 0);
            segmentRect.anchorMax = new Vector2(0, 1);
            segmentRect.pivot = new Vector2(0, 0.5f);
            
            // Position and size
            segmentRect.anchoredPosition = new Vector2(i * segmentWidth, 0);
            segmentRect.sizeDelta = new Vector2(segmentWidth, 0); // Height matches parent
        }
    }
    
    private void CreateDividerLines(float sliderWidth, float sliderHeight)
    {
        // Create divider lines between segments (segmentCount - 1 dividers)
        for (int i = 1; i < segmentCount; i++)
        {
            GameObject divider = new GameObject($"Divider_{i}");
            divider.transform.SetParent(dividersContainer.transform, false);
            
            Image dividerImage = divider.AddComponent<Image>();
            dividerImage.color = dividerColor;
            dividerImage.raycastTarget = false; // Don't block clicks
            
            RectTransform dividerRect = divider.GetComponent<RectTransform>();
            // Stretch vertically, anchor to left
            dividerRect.anchorMin = new Vector2(0, 0);
            dividerRect.anchorMax = new Vector2(0, 1);
            dividerRect.pivot = new Vector2(0.5f, 0.5f);
            
            // Calculate position as percentage of total width
            float segmentWidth = sliderWidth / segmentCount;
            float dividerPosition = segmentWidth * i;
            
            // Set size and position
            dividerRect.anchoredPosition = new Vector2(dividerPosition, 0);
            dividerRect.sizeDelta = new Vector2(dividerWidth, 0); // Height = 0 means stretch to parent
            
            Debug.Log($"[ShieldSliderSegments] Created Divider {i} at position {dividerPosition} (segment width: {segmentWidth})");
        }
    }
    
    [ContextMenu("Remove Segment Visuals")]
    public void CleanupExistingVisuals()
    {
        // Check in multiple possible locations
        Transform[] searchLocations = new Transform[] 
        { 
            transform, 
            transform.Find("Background"),
            transform.Find("Fill Area")
        };
        
        foreach (Transform location in searchLocations)
        {
            if (location == null) continue;
            
            Transform dividers = location.Find("SegmentDividers");
            if (dividers != null)
            {
                if (Application.isPlaying)
                    Destroy(dividers.gameObject);
                else
                    DestroyImmediate(dividers.gameObject);
                Debug.Log($"[ShieldSliderSegments] Removed SegmentDividers from {location.name}");
            }
            
            Transform backgrounds = location.Find("SegmentBackgrounds");
            if (backgrounds != null)
            {
                if (Application.isPlaying)
                    Destroy(backgrounds.gameObject);
                else
                    DestroyImmediate(backgrounds.gameObject);
                Debug.Log($"[ShieldSliderSegments] Removed SegmentBackgrounds from {location.name}");
            }
        }
    }
    
    void OnValidate()
    {
        // Clamp values
        segmentCount = Mathf.Max(1, segmentCount);
        dividerWidth = Mathf.Max(1f, dividerWidth);
        dividerHeightMultiplier = Mathf.Max(0.5f, dividerHeightMultiplier);
    }
}
