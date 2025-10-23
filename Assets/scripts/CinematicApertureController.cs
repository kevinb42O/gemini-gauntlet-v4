using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// Place this script on any GameObject. It will automatically create and manage
// the required UI elements for the cinematic aperture effect.
[AddComponentMenu("UI/Cinematic Aperture Controller")]
public class CinematicApertureController : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Total duration for the aperture to close and then open.")]
    public float animationDuration = 2.0f;
    [Tooltip("How long the aperture stays fully closed.")]
    public float holdDuration = 0.25f;
    [Tooltip("Delay before the animation starts.")]
    public float startDelay = 0.5f;
    [Tooltip("A smooth ease-in-out curve is highly recommended.")]
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Play the intro animation automatically when the scene loads.")]
    public bool playOnStart = true;

    // References to the graphics we will animate.
    private RectTransform topEyelidGraphic;
    private RectTransform bottomEyelidGraphic;

    private Canvas canvas;
    private GameObject topEyelid;
    private GameObject bottomEyelid;

    void Awake()
    {
        // Try to find existing canvas
        canvas = GetComponentInChildren<Canvas>();
        
        // Create canvas if it doesn't exist
        if (canvas == null)
        {
            CreateCanvas();
        }
        else
        {
            // Find existing UI elements
            topEyelid = canvas.transform.Find("Eyelid_Top")?.gameObject;
            bottomEyelid = canvas.transform.Find("Eyelid_Bottom")?.gameObject;
            
            if (topEyelid != null) topEyelidGraphic = topEyelid.transform.Find("Masked_Graphic_Top") as RectTransform;
            if (bottomEyelid != null) bottomEyelidGraphic = bottomEyelid.transform.Find("Masked_Graphic_Bottom") as RectTransform;
        }

        // Create missing elements
        if (topEyelidGraphic == null || bottomEyelidGraphic == null)
        {
            CreateApertureUI();
        }

        // Start with the eyelids closed, instantly.
        if (topEyelidGraphic != null) topEyelidGraphic.localScale = new Vector3(1, 1, 1);
        if (bottomEyelidGraphic != null) bottomEyelidGraphic.localScale = new Vector3(1, 1, 1);
    }
    
    private void CreateCanvas()
    {
        // Create Canvas
        GameObject canvasGO = new GameObject("ApertureCanvas");
        canvasGO.transform.SetParent(transform, false);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // Ensure it's on top of other UI
        canvas.gameObject.AddComponent<GraphicRaycaster>();
        
        // Add Canvas Scaler for proper scaling
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        
        CreateApertureUI();
    }
    
    private void CreateApertureUI()
    {
        if (canvas == null)
        {
            Debug.LogError("CinematicApertureController: Canvas is null. Cannot create aperture UI.");
            return;
        }
        
        try
        {
            // Create Top Eyelid if it doesn't exist
            if (topEyelid == null)
            {
                topEyelid = CreateEyelid("Eyelid_Top", canvas.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
                if (topEyelid != null)
                {
                    var graphic = topEyelid.transform.Find("Masked_Graphic_Top");
                    if (graphic != null) topEyelidGraphic = graphic.GetComponent<RectTransform>();
                }
            }
            
            // Create Bottom Eyelid if it doesn't exist
            if (bottomEyelid == null)
            {
                bottomEyelid = CreateEyelid("Eyelid_Bottom", canvas.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
                if (bottomEyelid != null)
                {
                    var graphic = bottomEyelid.transform.Find("Masked_Graphic_Bottom");
                    if (graphic != null)
                    {
                        bottomEyelidGraphic = graphic.GetComponent<RectTransform>();
                        // Flip the bottom graphic
                        bottomEyelidGraphic.localRotation = Quaternion.Euler(0, 0, 180);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"CinematicApertureController: Error creating aperture UI: {e.Message}");
            Debug.LogException(e);
        }
    }
    
    private GameObject CreateEyelid(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        // Create eyelid container with mask
        GameObject eyelid = new GameObject(name, typeof(RectTransform));
        eyelid.transform.SetParent(parent, false);
        
        RectTransform rt = eyelid.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0, 0);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        
        // Add mask
        Mask mask = eyelid.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        
        // Create a child object for the black overlay
        GameObject overlay = new GameObject("BlackOverlay", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(eyelid.transform, false);
        
        // Configure the overlay to cover the entire screen
        RectTransform overlayRt = overlay.GetComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.sizeDelta = Vector2.zero;
        overlayRt.anchoredPosition = Vector2.zero;
        overlay.GetComponent<Image>().color = Color.black;
        
        // Create a child object for the circle mask
        GameObject circleMask = new GameObject("CircleMask", typeof(RectTransform), typeof(Image));
        circleMask.transform.SetParent(eyelid.transform, false);
        
        // Configure the circle mask
        RectTransform circleRt = circleMask.GetComponent<RectTransform>();
        circleRt.anchorMin = new Vector2(0.5f, 0.5f);
        circleRt.anchorMax = new Vector2(0.5f, 0.5f);
        circleRt.pivot = new Vector2(0.5f, 0.5f);
        
        // Make it large enough to cover the screen
        float screenHeight = Screen.height;
        float screenWidth = Screen.width;
        float size = Mathf.Sqrt(screenWidth * screenWidth + screenHeight * screenHeight) * 0.6f;
        circleRt.sizeDelta = new Vector2(size, size);
        
        // Set up the circle mask
        Image circleImg = circleMask.GetComponent<Image>();
        circleImg.color = Color.white;
        
        // Use a simple white texture for the circle
        Texture2D circleTexture = new Texture2D(1, 1);
        circleTexture.SetPixel(0, 0, Color.white);
        circleTexture.Apply();
        
        // Create a sprite from the texture
        Sprite circleSprite = Sprite.Create(circleTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        circleImg.sprite = circleSprite;
        
        // Set up the mask to show only where the circle is
        RectMask2D rectMask = eyelid.AddComponent<RectMask2D>();
        
        // Set up the mask to invert (show everything except the circle)
        // We'll use the stencil buffer for this
        var maskableGraphic = overlay.GetComponent<MaskableGraphic>();
        if (maskableGraphic != null)
        {
            maskableGraphic.maskable = true;
        }
        
        // Add a component to handle the masking effect
        var maskEffect = eyelid.AddComponent<MaskEffect>();
        maskEffect.Initialize(overlay.GetComponent<RectTransform>(), circleRt);
        
        return eyelid;
    }

    void Start()
    {
        if (playOnStart && topEyelidGraphic != null && bottomEyelidGraphic != null)
        {
            // We now play the OPENING animation first.
            StartCoroutine(PlayOpenAnimation());
        }
        else if (playOnStart)
        {
            Debug.LogWarning("CinematicApertureController: Eyelid graphics not properly initialized. Animation not started.");
        }
    }

    // A public method to trigger the full close-then-open effect,
    // useful for scene transitions.
    public void PlayFullTransition()
    {
        StartCoroutine(PlayCloseAndOpenAnimation());
    }

    private IEnumerator PlayOpenAnimation()
    {
        yield return new WaitForSeconds(startDelay);

        float openDuration = animationDuration / 2f;
        float timer = 0f;

        while (timer < openDuration)
        {
            // We evaluate the curve and then INVERT the result (1 - t).
            // This makes our ease-in-out curve work for an "opening" motion.
            float t = 1f - easeCurve.Evaluate(timer / openDuration);
            float scaleValue = Mathf.Lerp(0f, 1f, t);
            
            Vector3 newScale = new Vector3(1, scaleValue, 1);
            topEyelidGraphic.localScale = newScale;
            bottomEyelidGraphic.localScale = newScale;

            timer += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        // Snap to final open position (fully retracted).
        Vector3 finalScale = new Vector3(1, 0, 1);
        topEyelidGraphic.localScale = finalScale;
        bottomEyelidGraphic.localScale = finalScale;
    }
    
    // This coroutine can be called from other scripts to trigger the effect.
    // e.g., before loading a new level.
    private IEnumerator PlayCloseAndOpenAnimation()
    {
        // --- Animate In (Closing) ---
        float closeDuration = animationDuration / 2f;
        float timer = 0f;

        while (timer < closeDuration)
        {
            float t = easeCurve.Evaluate(timer / closeDuration);
            float scaleValue = Mathf.Lerp(0f, 1f, t);

            Vector3 newScale = new Vector3(1, scaleValue, 1);
            topEyelidGraphic.localScale = newScale;
            bottomEyelidGraphic.localScale = newScale;

            timer += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        
        // Snap to final closed position.
        Vector3 closedScale = new Vector3(1, 1, 1);
        topEyelidGraphic.localScale = closedScale;
        bottomEyelidGraphic.localScale = closedScale;

        // --- Hold ---
        yield return new WaitForSeconds(holdDuration);
        // --- Animate Out (Opening) ---
        // We can just call the other coroutine to keep code clean.
        yield return StartCoroutine(PlayOpenAnimation());
    }
}

// Helper component to handle the mask effect
[RequireComponent(typeof(RectTransform))]
public class MaskEffect : MonoBehaviour
{
    private RectTransform overlay;
    private RectTransform circleMask;
    private Material material;

    public void Initialize(RectTransform overlay, RectTransform circleMask)
    {
        this.overlay = overlay;
        this.circleMask = circleMask;
        
        // Create a material with a shader that supports masking
        var shader = Shader.Find("UI/Default");
        if (shader == null)
        {
            Debug.LogError("Could not find UI/Default shader. Make sure you have the UI shaders in your project.");
            return;
        }
        
        material = new Material(shader);
        
        // Set the material to the overlay
        var image = overlay.GetComponent<Image>();
        if (image != null)
        {
            image.material = material;
        }
        
        // Update the mask position/size
        UpdateMask();
    }
    
    private void Update()
    {
        if (overlay != null && circleMask != null)
        {
            UpdateMask();
        }
    }
    
    private void UpdateMask()
    {
        if (material == null || overlay == null || circleMask == null) return;
        
        // Convert mask position to overlay's local space
        Vector2 maskPos = overlay.InverseTransformPoint(circleMask.position);
        
        // Calculate mask size in overlay's local space
        Vector2 maskSize = new Vector2(
            circleMask.rect.width * circleMask.lossyScale.x / overlay.lossyScale.x,
            circleMask.rect.height * circleMask.lossyScale.y / overlay.lossyScale.y
        );
        
        // Set material properties
        material.SetVector("_MaskCenter", maskPos);
        material.SetVector("_MaskSize", maskSize);
    }
    
    private void OnDestroy()
    {
        if (material != null)
        {
            if (Application.isPlaying)
            {
                Destroy(material);
            }
            else
            {
                DestroyImmediate(material);
            }
        }
    }
}
