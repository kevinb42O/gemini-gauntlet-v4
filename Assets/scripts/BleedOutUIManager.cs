using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Enhanced Bleeding Out System - Shows circular progress indicator and bleeding out mechanics
/// Handles both scenarios: with self-revive available and without
/// Future-proofed for multiplayer teammate revives
/// </summary>
public class BleedOutUIManager : MonoBehaviour
{
    [Header("Bleed Out Settings")]
    [SerializeField] private float bleedOutDuration = 30f; // Time until death
    [SerializeField] private float holdESpeedMultiplier = 2f; // Speed multiplier when holding E
    [SerializeField] private KeyCode skipKey = KeyCode.E; // Hold to speed up death
    [SerializeField] private float selfReviveHoldDuration = 2.5f; // Time to hold E to complete self-revive
    
    [Header("UI Components (Created at Runtime)")]
    private Canvas uiCanvas;
    private GameObject bleedOutPanel;
    private Image circularProgressImage; // Circular fill around item icon
    private Image itemIconImage; // Self-revive item icon (or skull icon if no revive)
    private TextMeshProUGUI bleedingOutText;
    private TextMeshProUGUI instructionText; // "Hold E to skip" or "Press E to use Self-Revive"
    private TextMeshProUGUI connectionLostText; // Shows "CONNECTION LOST" at the end
    
    [Header("Icon Sprites")]
    [SerializeField] private Sprite selfReviveIconSprite; // Assign in inspector or find at runtime
    [SerializeField] private Sprite skullIconSprite; // Default icon when no self-revive
    
    [Header("Visual Settings")]
    [SerializeField] private Color circularProgressColor = new Color(1f, 0f, 0f, 0.9f); // Red progress bar
    [SerializeField] private float circularProgressSize = 280f; // Size of circular progress indicator (BIGGER for impact!)
    [SerializeField] private bool enableRotationAnimation = true; // Rotate progress ring for extra polish
    [SerializeField] private float rotationSpeed = 30f; // Degrees per second
    
    // State tracking
    private bool isBleedingOut = false;
    private bool hasSelfRevive = false;
    private float currentBleedOutTime = 0f;
    private Coroutine bleedOutCoroutine;
    private Coroutine pulseAnimationCoroutine;
    
    // Self-revive hold tracking
    private bool isHoldingForRevive = false;
    private float reviveHoldProgress = 0f;
    private Coroutine selfReviveHoldCoroutine;
    
    // CRITICAL: Self-revive debounce to prevent double-consumption
    private float _lastSelfReviveRequestTime = -999f;
    private const float SELF_REVIVE_COOLDOWN = 0.5f;
    
    // Visual effect references
    private CanvasGroup bleedingOutTextCanvasGroup;
    private RectTransform progressContainerRect;
    private RectTransform progressRingRect; // For rotation animation
    private TextMeshProUGUI timerText; // Shows remaining time in center
    private Coroutine rotationAnimationCoroutine;
    
    // Events
    public System.Action OnBleedOutComplete; // Fired when bleed out timer expires (player dies)
    public System.Action OnSelfReviveRequested; // Fired when player COMPLETES holding E for self-revive
    public System.Action<float> OnBleedOutProgress; // Fired each frame with progress (0-1)
    public System.Action<float> OnSelfReviveProgress; // Fired each frame during self-revive hold (0-1)
    
    void Awake()
    {
        CreateBleedOutUI();
    }
    
    /// <summary>
    /// Create the bleed out UI at runtime
    /// </summary>
    void CreateBleedOutUI()
    {
        // Create or find dedicated overlay canvas
        GameObject canvasGO = GameObject.Find("BleedOut_Canvas");
        if (canvasGO != null && canvasGO.TryGetComponent<Canvas>(out uiCanvas))
        {
            Debug.Log("[BleedOutUIManager] Reusing existing BleedOut_Canvas");
        }
        else
        {
            canvasGO = new GameObject("BleedOut_Canvas");
            uiCanvas = canvasGO.AddComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            uiCanvas.overrideSorting = true;
            uiCanvas.sortingOrder = 32765; // Very high to ensure visibility above everything
            
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            
            canvasGO.AddComponent<GraphicRaycaster>();
            
            Debug.Log("[BleedOutUIManager] Created dedicated BleedOut_Canvas");
        }
        
        // Create main panel (semi-transparent background)
        bleedOutPanel = new GameObject("BleedOut_Panel");
        bleedOutPanel.transform.SetParent(uiCanvas.transform, false);
        bleedOutPanel.transform.SetAsLastSibling();
        
        // Panel doesn't need a background - blood overlay handles that
        RectTransform panelRect = bleedOutPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        
        // Create circular progress container (center of screen)
        GameObject progressContainer = new GameObject("ProgressContainer");
        progressContainer.transform.SetParent(bleedOutPanel.transform, false);
        
        progressContainerRect = progressContainer.AddComponent<RectTransform>();
        progressContainerRect.anchorMin = new Vector2(0.5f, 0.5f);
        progressContainerRect.anchorMax = new Vector2(0.5f, 0.5f);
        progressContainerRect.anchoredPosition = new Vector2(0f, -200f); // MOVED DOWN 200 pixels to not block view
        progressContainerRect.sizeDelta = new Vector2(circularProgressSize, circularProgressSize);
        
        // Create circular progress ring (outer ring)
        GameObject outerRingGO = new GameObject("OuterRing");
        outerRingGO.transform.SetParent(progressContainer.transform, false);
        
        Image outerRing = outerRingGO.AddComponent<Image>();
        outerRing.sprite = CreateCircleSprite();
        outerRing.color = new Color(0.1f, 0.1f, 0.1f, 0.8f); // Dark background ring
        
        RectTransform outerRect = outerRingGO.GetComponent<RectTransform>();
        outerRect.anchorMin = Vector2.zero;
        outerRect.anchorMax = Vector2.one;
        outerRect.offsetMin = Vector2.zero;
        outerRect.offsetMax = Vector2.zero;
        
        // Add glow outline to outer ring
        Outline outline = outerRingGO.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.5f);
        outline.effectDistance = new Vector2(3f, 3f);
        
        // Create inner background (darker circle)
        GameObject innerBgGO = new GameObject("InnerBackground");
        innerBgGO.transform.SetParent(progressContainer.transform, false);
        
        Image innerBg = innerBgGO.AddComponent<Image>();
        innerBg.sprite = CreateCircleSprite();
        innerBg.color = new Color(0.05f, 0.05f, 0.05f, 0.9f);
        
        RectTransform innerBgRect = innerBgGO.GetComponent<RectTransform>();
        innerBgRect.anchorMin = new Vector2(0.1f, 0.1f);
        innerBgRect.anchorMax = new Vector2(0.9f, 0.9f);
        innerBgRect.offsetMin = Vector2.zero;
        innerBgRect.offsetMax = Vector2.zero;
        
        // Create circular progress foreground (radial fill) - THICKER RING!
        GameObject progressFgGO = new GameObject("ProgressForeground");
        progressFgGO.transform.SetParent(progressContainer.transform, false);
        
        circularProgressImage = progressFgGO.AddComponent<Image>();
        circularProgressImage.sprite = CreateCircleSprite();
        circularProgressImage.type = Image.Type.Filled;
        circularProgressImage.fillMethod = Image.FillMethod.Radial360;
        circularProgressImage.fillOrigin = (int)Image.Origin360.Top;
        circularProgressImage.fillAmount = 1f;
        circularProgressImage.fillClockwise = false; // Counter-clockwise drain
        circularProgressImage.color = circularProgressColor;
        
        progressRingRect = progressFgGO.GetComponent<RectTransform>();
        progressRingRect.anchorMin = new Vector2(0.12f, 0.12f); // THICKER ring (was 0.15)
        progressRingRect.anchorMax = new Vector2(0.88f, 0.88f); // THICKER ring (was 0.85)
        progressRingRect.offsetMin = Vector2.zero;
        progressRingRect.offsetMax = Vector2.zero;
        
        // Add ENHANCED glow effect to progress ring
        Shadow glowEffect1 = progressFgGO.AddComponent<Shadow>();
        glowEffect1.effectColor = new Color(circularProgressColor.r, circularProgressColor.g, circularProgressColor.b, 0.8f);
        glowEffect1.effectDistance = new Vector2(0f, 0f);
        glowEffect1.useGraphicAlpha = true;
        
        // Add second glow layer for MORE INTENSITY
        Shadow glowEffect2 = progressFgGO.AddComponent<Shadow>();
        glowEffect2.effectColor = new Color(circularProgressColor.r, circularProgressColor.g, circularProgressColor.b, 0.4f);
        glowEffect2.effectDistance = new Vector2(2f, 2f);
        glowEffect2.useGraphicAlpha = true;
        
        // Create item icon in center (self-revive or skull) - BIGGER!
        GameObject iconGO = new GameObject("ItemIcon");
        iconGO.transform.SetParent(progressContainer.transform, false);
        
        itemIconImage = iconGO.AddComponent<Image>();
        itemIconImage.color = Color.white;
        itemIconImage.preserveAspect = true;
        
        RectTransform iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(circularProgressSize * 0.6f, circularProgressSize * 0.6f); // BIGGER: 60% of progress circle (was 50%)
        
        // Add ENHANCED drop shadow to icon
        Shadow iconShadow = iconGO.AddComponent<Shadow>();
        iconShadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
        iconShadow.effectDistance = new Vector2(5f, -5f);
        
        // Add subtle glow around icon
        Shadow iconGlow = iconGO.AddComponent<Shadow>();
        iconGlow.effectColor = new Color(1f, 0.3f, 0.3f, 0.3f); // Subtle red glow
        iconGlow.effectDistance = new Vector2(0f, 0f);
        
        // Create "BLEEDING OUT" text above the circle
        GameObject bleedingTextGO = new GameObject("BleedingOutText");
        bleedingTextGO.transform.SetParent(bleedOutPanel.transform, false);
        
        bleedingOutText = bleedingTextGO.AddComponent<TextMeshProUGUI>();
        bleedingOutText.text = "BLEEDING OUT";
        bleedingOutText.fontSize = 64;
        bleedingOutText.color = new Color(1f, 0.15f, 0.15f, 1f); // Bright red
        bleedingOutText.alignment = TextAlignmentOptions.Center;
        bleedingOutText.fontStyle = FontStyles.Bold;
        bleedingOutText.outlineWidth = 0.3f;
        bleedingOutText.outlineColor = new Color(0f, 0f, 0f, 0.8f);
        
        // Add canvas group for pulsing animation
        bleedingOutTextCanvasGroup = bleedingTextGO.AddComponent<CanvasGroup>();
        bleedingOutTextCanvasGroup.alpha = 1f;
        
        RectTransform bleedingTextRect = bleedingTextGO.GetComponent<RectTransform>();
        bleedingTextRect.anchorMin = new Vector2(0.5f, 0.5f);
        bleedingTextRect.anchorMax = new Vector2(0.5f, 0.5f);
        bleedingTextRect.anchoredPosition = new Vector2(0f, circularProgressSize * 0.75f); // Closer to circle
        bleedingTextRect.sizeDelta = new Vector2(1000, 120);
        
        // Create instruction text below the circle
        GameObject instructionTextGO = new GameObject("InstructionText");
        instructionTextGO.transform.SetParent(bleedOutPanel.transform, false);
        
        instructionText = instructionTextGO.AddComponent<TextMeshProUGUI>();
        instructionText.text = "Hold E to skip";
        instructionText.fontSize = 36;
        instructionText.color = new Color(0.9f, 0.9f, 0.9f, 0.9f); // Light gray
        instructionText.alignment = TextAlignmentOptions.Center;
        instructionText.fontStyle = FontStyles.Italic;
        instructionText.outlineWidth = 0.2f;
        instructionText.outlineColor = new Color(0f, 0f, 0f, 0.6f);
        
        RectTransform instructionRect = instructionTextGO.GetComponent<RectTransform>();
        instructionRect.anchorMin = new Vector2(0.5f, 0.5f);
        instructionRect.anchorMax = new Vector2(0.5f, 0.5f);
        instructionRect.anchoredPosition = new Vector2(0f, -circularProgressSize * 0.75f); // Closer to circle
        instructionRect.sizeDelta = new Vector2(800, 90);
        
        // Create timer text in center (shows remaining seconds)
        GameObject timerTextGO = new GameObject("TimerText");
        timerTextGO.transform.SetParent(progressContainer.transform, false);
        
        timerText = timerTextGO.AddComponent<TextMeshProUGUI>();
        timerText.text = "30";
        timerText.fontSize = 48;
        timerText.color = new Color(1f, 1f, 1f, 0.8f); // White
        timerText.alignment = TextAlignmentOptions.Center;
        timerText.fontStyle = FontStyles.Bold;
        timerText.outlineWidth = 0.3f;
        timerText.outlineColor = new Color(0f, 0f, 0f, 0.9f);
        
        RectTransform timerRect = timerTextGO.GetComponent<RectTransform>();
        timerRect.anchorMin = new Vector2(0.5f, 0.5f);
        timerRect.anchorMax = new Vector2(0.5f, 0.5f);
        timerRect.anchoredPosition = new Vector2(0f, -circularProgressSize * 0.15f); // Below icon
        timerRect.sizeDelta = new Vector2(200, 80);
        
        // Create "CONNECTION LOST" text (hidden initially, shown at death)
        GameObject connectionLostGO = new GameObject("ConnectionLostText");
        connectionLostGO.transform.SetParent(bleedOutPanel.transform, false);
        
        connectionLostText = connectionLostGO.AddComponent<TextMeshProUGUI>();
        connectionLostText.text = "CONNECTION LOST";
        connectionLostText.fontSize = 84;
        connectionLostText.color = new Color(1f, 0.1f, 0.1f, 1f); // Bright red
        connectionLostText.alignment = TextAlignmentOptions.Center;
        connectionLostText.fontStyle = FontStyles.Bold;
        connectionLostText.outlineWidth = 0.4f;
        connectionLostText.outlineColor = new Color(0f, 0f, 0f, 1f);
        connectionLostText.enabled = false; // Hidden initially
        
        RectTransform connectionRect = connectionLostGO.GetComponent<RectTransform>();
        connectionRect.anchorMin = new Vector2(0.5f, 0.5f);
        connectionRect.anchorMax = new Vector2(0.5f, 0.5f);
        connectionRect.anchoredPosition = Vector2.zero;
        connectionRect.sizeDelta = new Vector2(1200, 180);
        
        // Start hidden
        bleedOutPanel.SetActive(false);
        
        Debug.Log("[BleedOutUIManager] Bleed out UI created successfully");
    }
    
    /// <summary>
    /// Create a proper circle sprite for the progress indicator
    /// Uses procedural generation to create a smooth anti-aliased circle
    /// </summary>
    private Sprite CreateCircleSprite()
    {
        int resolution = 512; // High resolution for smooth edges
        Texture2D circleTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        circleTexture.filterMode = FilterMode.Bilinear;
        circleTexture.wrapMode = TextureWrapMode.Clamp;
        
        Color[] pixels = new Color[resolution * resolution];
        Vector2 center = new Vector2(resolution / 2f, resolution / 2f);
        float radius = resolution / 2f - 2f; // Slight padding for anti-aliasing
        
        // Generate circle with anti-aliasing
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float distance = Vector2.Distance(pos, center);
                
                // Anti-aliased edge
                float alpha = 1f - Mathf.Clamp01((distance - radius + 1f) / 2f);
                pixels[y * resolution + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        
        circleTexture.SetPixels(pixels);
        circleTexture.Apply();
        
        return Sprite.Create(
            circleTexture,
            new Rect(0, 0, resolution, resolution),
            new Vector2(0.5f, 0.5f),
            resolution / 2f
        );
    }
    
    /// <summary>
    /// Start the bleeding out sequence
    /// </summary>
    public void StartBleedOut(bool hasSelfReviveAvailable)
    {
        if (isBleedingOut) return;
        
        Debug.Log($"[BleedOutUIManager] Starting bleed out - Has self-revive: {hasSelfReviveAvailable}");
        
        isBleedingOut = true;
        hasSelfRevive = hasSelfReviveAvailable;
        currentBleedOutTime = 0f;
        
        // Show the UI
        if (bleedOutPanel != null)
        {
            bleedOutPanel.SetActive(true);
        }
        
        // Set appropriate icon
        if (itemIconImage != null)
        {
            if (hasSelfReviveAvailable && selfReviveIconSprite != null)
            {
                itemIconImage.sprite = selfReviveIconSprite;
            }
            else if (skullIconSprite != null)
            {
                itemIconImage.sprite = skullIconSprite;
            }
            else
            {
                // Create a default icon color if no sprites assigned
                itemIconImage.color = hasSelfReviveAvailable ? Color.green : Color.red;
            }
        }
        
        // Set appropriate instruction text
        if (instructionText != null)
        {
            if (hasSelfReviveAvailable)
            {
                instructionText.text = $"Hold {skipKey} to use Self-Revive";
            }
            else
            {
                instructionText.text = $"Hold {skipKey} to skip";
            }
        }
        
        // Reset circular progress
        if (circularProgressImage != null)
        {
            circularProgressImage.fillAmount = 1f; // Start full, drains to 0
        }
        
        // Hide connection lost text
        if (connectionLostText != null)
        {
            connectionLostText.enabled = false;
        }
        
        // Start the bleed out coroutine
        if (bleedOutCoroutine != null)
        {
            StopCoroutine(bleedOutCoroutine);
        }
        bleedOutCoroutine = StartCoroutine(BleedOutCoroutine());
        
        // Start pulse animation
        if (pulseAnimationCoroutine != null)
        {
            StopCoroutine(pulseAnimationCoroutine);
        }
        pulseAnimationCoroutine = StartCoroutine(PulseAnimationCoroutine());
        
        // Start rotation animation for progress ring
        if (enableRotationAnimation && rotationAnimationCoroutine != null)
        {
            StopCoroutine(rotationAnimationCoroutine);
        }
        if (enableRotationAnimation)
        {
            rotationAnimationCoroutine = StartCoroutine(RotationAnimationCoroutine());
        }
    }
    
    /// <summary>
    /// Stop the bleeding out sequence
    /// </summary>
    public void StopBleedOut()
    {
        Debug.Log("[BleedOutUIManager] Stopping bleed out");
        
        isBleedingOut = false;
        isHoldingForRevive = false;
        reviveHoldProgress = 0f;
        
        if (bleedOutPanel != null)
        {
            bleedOutPanel.SetActive(false);
        }
        
        if (bleedOutCoroutine != null)
        {
            StopCoroutine(bleedOutCoroutine);
            bleedOutCoroutine = null;
        }
        
        if (pulseAnimationCoroutine != null)
        {
            StopCoroutine(pulseAnimationCoroutine);
            pulseAnimationCoroutine = null;
        }
        
        if (rotationAnimationCoroutine != null)
        {
            StopCoroutine(rotationAnimationCoroutine);
            rotationAnimationCoroutine = null;
        }
        
        if (selfReviveHoldCoroutine != null)
        {
            StopCoroutine(selfReviveHoldCoroutine);
            selfReviveHoldCoroutine = null;
        }
    }
    
    /// <summary>
    /// Main bleeding out coroutine
    /// </summary>
    private IEnumerator BleedOutCoroutine()
    {
        bool hasRequestedSelfRevive = false;
        
        while (currentBleedOutTime < bleedOutDuration && isBleedingOut)
        {
            // CRITICAL: Check for E key HOLD (self-revive) - must hold to complete
            if (hasSelfRevive && !hasRequestedSelfRevive)
            {
                if (Input.GetKey(skipKey))
                {
                    // Start or continue holding for self-revive
                    if (!isHoldingForRevive)
                    {
                        isHoldingForRevive = true;
                        reviveHoldProgress = 0f;
                        Debug.Log("[BleedOutUIManager] Started holding E for self-revive");
                        
                        // Update instruction text
                        if (instructionText != null)
                        {
                            instructionText.text = "Keep holding E...";
                        }
                    }
                    
                    // Increase progress
                    reviveHoldProgress += Time.unscaledDeltaTime / selfReviveHoldDuration;
                    
                    // Update circular progress to show revive progress (fill up from 0 to 1)
                    if (circularProgressImage != null)
                    {
                        // Show revive progress as filling up (green color for revive)
                        circularProgressImage.fillAmount = reviveHoldProgress;
                        circularProgressImage.color = Color.Lerp(Color.yellow, Color.green, reviveHoldProgress);
                    }
                    
                    // Fire progress event
                    OnSelfReviveProgress?.Invoke(reviveHoldProgress);
                    
                    // Check if hold is complete
                    if (reviveHoldProgress >= 1f)
                    {
                        Debug.Log("[BleedOutUIManager] Self-revive hold completed!");
                        hasRequestedSelfRevive = true;
                        isHoldingForRevive = false;
                        OnSelfReviveRequested?.Invoke();
                        // Don't stop bleeding out - PlayerHealth will handle the revive process
                    }
                }
                else if (isHoldingForRevive)
                {
                    // Released E before completing - reset progress
                    Debug.Log("[BleedOutUIManager] Released E - resetting self-revive progress");
                    isHoldingForRevive = false;
                    reviveHoldProgress = 0f;
                    
                    // Restore instruction text
                    if (instructionText != null)
                    {
                        instructionText.text = $"Hold {skipKey} to use Self-Revive";
                    }
                    
                    // Restore bleed out progress display
                    float bleedOutProgress = 1f - (currentBleedOutTime / bleedOutDuration);
                    if (circularProgressImage != null)
                    {
                        circularProgressImage.fillAmount = bleedOutProgress;
                        circularProgressImage.color = circularProgressColor;
                    }
                }
            }
            
            // Check for E key held (speed up death) - only if NOT using self-revive
            float speedMultiplier = 1f;
            if (!hasSelfRevive && Input.GetKey(skipKey))
            {
                speedMultiplier = holdESpeedMultiplier;
            }
            
            // Update bleed out timer (use unscaled time to work during pause)
            currentBleedOutTime += Time.unscaledDeltaTime * speedMultiplier;
            
            // Calculate progress (1 = full health, 0 = dead)
            float progress = 1f - (currentBleedOutTime / bleedOutDuration);
            progress = Mathf.Clamp01(progress);
            
            // Update UI (only if not holding for self-revive)
            if (circularProgressImage != null && !isHoldingForRevive)
            {
                circularProgressImage.fillAmount = progress;
                circularProgressImage.color = circularProgressColor; // Ensure color is correct
            }
            
            // Update timer text
            if (timerText != null)
            {
                float remainingTime = bleedOutDuration - currentBleedOutTime;
                timerText.text = Mathf.CeilToInt(remainingTime).ToString();
                
                // Color change based on urgency
                if (progress < 0.2f)
                {
                    timerText.color = new Color(1f, 0.1f, 0.1f, 1f); // Bright red when critical
                }
                else if (progress < 0.5f)
                {
                    timerText.color = new Color(1f, 0.6f, 0.1f, 0.9f); // Orange when low
                }
                else
                {
                    timerText.color = new Color(1f, 1f, 1f, 0.8f); // White when safe
                }
            }
            
            // Fire progress event for blood overlay pulsation
            OnBleedOutProgress?.Invoke(progress);
            
            // Update bleeding out heartbeat intensity (gets faster/louder as time runs out)
            GeminiGauntlet.Audio.GameSounds.UpdateBleedingOutHeartbeatIntensity(progress);
            
            yield return null;
        }
        
        // Bleed out complete - player dies
        if (isBleedingOut && !hasRequestedSelfRevive)
        {
            Debug.Log("[BleedOutUIManager] Bleed out complete - triggering death");
            
            // Hide bleeding out text and show connection lost
            if (bleedingOutText != null)
            {
                bleedingOutText.enabled = false;
            }
            if (instructionText != null)
            {
                instructionText.enabled = false;
            }
            if (circularProgressImage != null)
            {
                circularProgressImage.enabled = false;
            }
            if (itemIconImage != null)
            {
                itemIconImage.enabled = false;
            }
            if (connectionLostText != null)
            {
                connectionLostText.enabled = true;
            }
            
            // Fire death event
            OnBleedOutComplete?.Invoke();
        }
    }
    
    /// <summary>
    /// Get current bleed out progress (0 = dead, 1 = full health)
    /// </summary>
    public float GetBleedOutProgress()
    {
        if (!isBleedingOut) return 1f;
        
        float progress = 1f - (currentBleedOutTime / bleedOutDuration);
        return Mathf.Clamp01(progress);
    }
    
    /// <summary>
    /// Check if currently bleeding out
    /// </summary>
    public bool IsBleedingOut()
    {
        return isBleedingOut;
    }
    
    /// <summary>
    /// Pulse animation for text and progress ring - gets faster as death approaches
    /// </summary>
    private IEnumerator PulseAnimationCoroutine()
    {
        float minPulseSpeed = 0.5f; // Slowest pulse (when healthy)
        float maxPulseSpeed = 3.0f; // Fastest pulse (near death)
        
        while (isBleedingOut)
        {
            // Get current progress
            float progress = GetBleedOutProgress();
            
            // Calculate pulse speed based on health (faster as death approaches)
            float pulseSpeed = Mathf.Lerp(maxPulseSpeed, minPulseSpeed, progress);
            float pulseDuration = 1f / pulseSpeed;
            
            // Pulse text opacity
            float t = 0f;
            while (t < pulseDuration / 2f && isBleedingOut)
            {
                t += Time.unscaledDeltaTime;
                float normalizedTime = t / (pulseDuration / 2f);
                
                // Sine wave for smooth pulsing
                float alpha = Mathf.Lerp(0.7f, 1f, Mathf.Sin(normalizedTime * Mathf.PI));
                
                if (bleedingOutTextCanvasGroup != null)
                {
                    bleedingOutTextCanvasGroup.alpha = alpha;
                }
                
                // Slight scale pulse on progress ring for urgency
                if (progressContainerRect != null && progress < 0.3f)
                {
                    float scale = Mathf.Lerp(1f, 1.05f, Mathf.Sin(normalizedTime * Mathf.PI * 2f));
                    progressContainerRect.localScale = Vector3.one * scale;
                }
                
                yield return null;
            }
            
            // Pulse back
            t = 0f;
            while (t < pulseDuration / 2f && isBleedingOut)
            {
                t += Time.unscaledDeltaTime;
                float normalizedTime = t / (pulseDuration / 2f);
                
                float alpha = Mathf.Lerp(1f, 0.7f, Mathf.Sin(normalizedTime * Mathf.PI));
                
                if (bleedingOutTextCanvasGroup != null)
                {
                    bleedingOutTextCanvasGroup.alpha = alpha;
                }
                
                if (progressContainerRect != null && progress < 0.3f)
                {
                    float scale = Mathf.Lerp(1.05f, 1f, Mathf.Sin(normalizedTime * Mathf.PI * 2f));
                    progressContainerRect.localScale = Vector3.one * scale;
                }
                
                yield return null;
            }
        }
        
        // Reset to normal
        if (bleedingOutTextCanvasGroup != null)
        {
            bleedingOutTextCanvasGroup.alpha = 1f;
        }
        if (progressContainerRect != null)
        {
            progressContainerRect.localScale = Vector3.one;
        }
    }
    
    /// <summary>
    /// Rotation animation for progress ring - adds dynamic movement
    /// </summary>
    private IEnumerator RotationAnimationCoroutine()
    {
        if (progressRingRect == null) yield break;
        
        float currentRotation = 0f;
        
        while (isBleedingOut)
        {
            // Rotate the progress ring for dynamic effect
            currentRotation -= rotationSpeed * Time.unscaledDeltaTime;
            if (currentRotation <= -360f) currentRotation += 360f;
            
            progressRingRect.localRotation = Quaternion.Euler(0f, 0f, currentRotation);
            
            yield return null;
        }
        
        // Reset rotation
        if (progressRingRect != null)
        {
            progressRingRect.localRotation = Quaternion.identity;
        }
    }
}
