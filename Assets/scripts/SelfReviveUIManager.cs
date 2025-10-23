using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Complete Self-Revive UI Manager that creates and manages the UI at runtime
/// This ensures the self-revive system works even if no UI is set up in the scene
/// </summary>
public class SelfReviveUIManager : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private float revivePromptDuration = 10f;
    [SerializeField] private float reviveProcessDuration = 3f;
    [SerializeField] private KeyCode reviveKey = KeyCode.E;
    
    // UI Components (created at runtime)
    private Canvas uiCanvas;
    private GameObject revivePromptPanel;
    private TextMeshProUGUI revivePromptText;
    private TextMeshProUGUI countdownText;
    
    // State
    private bool isRevivePromptActive = false;
    private bool isReviveProcessActive = false;
    private Coroutine reviveCoroutine;
    
    // Events
    public System.Action OnReviveUsed;
    public System.Action OnReviveExpired;
    
    void Awake()
    {
        CreateSelfReviveUI();
    }
    
    void Update()
    {
        // Listen for E key during revive prompt
        if (isRevivePromptActive && Input.GetKeyDown(reviveKey))
        {
            Debug.Log($"SelfReviveUIManager: {reviveKey} key pressed - using revive!");
            UseRevive();
        }
    }
    
    /// <summary>
    /// Create the self-revive UI at runtime
    /// </summary>
    void CreateSelfReviveUI()
    {
        // Always use a dedicated overlay canvas to avoid being hidden by other UI toggles
        GameObject reviveCanvasGO = GameObject.Find("SelfRevive_Canvas");
        if (reviveCanvasGO != null && reviveCanvasGO.TryGetComponent<Canvas>(out uiCanvas))
        {
            // Reuse existing dedicated canvas
        }
        else
        {
            reviveCanvasGO = new GameObject("SelfRevive_Canvas");
            uiCanvas = reviveCanvasGO.AddComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            uiCanvas.overrideSorting = true;
            uiCanvas.sortingOrder = 32760; // Very high to ensure visibility

            var scaler = reviveCanvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            reviveCanvasGO.AddComponent<GraphicRaycaster>();

            Debug.Log("SelfReviveUIManager: Created dedicated overlay Canvas for self-revive UI");
        }
        
        // Create main panel
        revivePromptPanel = new GameObject("SelfRevive_Panel");
        revivePromptPanel.transform.SetParent(uiCanvas.transform, false);
        revivePromptPanel.transform.SetAsLastSibling();
        
        Image panelImage = revivePromptPanel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f); // Semi-transparent black
        
        RectTransform panelRect = revivePromptPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        
        // Create prompt text
        GameObject promptTextGO = new GameObject("PromptText");
        promptTextGO.transform.SetParent(revivePromptPanel.transform, false);
        
        revivePromptText = promptTextGO.AddComponent<TextMeshProUGUI>();
        revivePromptText.text = "Press E to use SELF REVIVE";
        revivePromptText.fontSize = 48;
        revivePromptText.color = Color.white;
        revivePromptText.alignment = TextAlignmentOptions.Center;
        
        RectTransform promptRect = promptTextGO.GetComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0.5f, 0.6f);
        promptRect.anchorMax = new Vector2(0.5f, 0.6f);
        promptRect.anchoredPosition = Vector2.zero;
        promptRect.sizeDelta = new Vector2(600, 100);
        
        // Create countdown text
        GameObject countdownTextGO = new GameObject("CountdownText");
        countdownTextGO.transform.SetParent(revivePromptPanel.transform, false);
        
        countdownText = countdownTextGO.AddComponent<TextMeshProUGUI>();
        countdownText.text = "10";
        countdownText.fontSize = 72;
        countdownText.color = Color.red;
        countdownText.alignment = TextAlignmentOptions.Center;
        
        RectTransform countdownRect = countdownTextGO.GetComponent<RectTransform>();
        countdownRect.anchorMin = new Vector2(0.5f, 0.4f);
        countdownRect.anchorMax = new Vector2(0.5f, 0.4f);
        countdownRect.anchoredPosition = Vector2.zero;
        countdownRect.sizeDelta = new Vector2(200, 100);
        
        // Start hidden
        revivePromptPanel.SetActive(false);
        
        Debug.Log("SelfReviveUIManager: Self-revive UI created successfully");
    }
    
    /// <summary>
    /// Show the revive prompt with countdown
    /// </summary>
    public void ShowRevivePrompt()
    {
        if (isRevivePromptActive || isReviveProcessActive) return;
        
        Debug.Log("SelfReviveUIManager: Showing revive prompt");
        
        isRevivePromptActive = true;
        
        if (revivePromptPanel != null)
        {
            revivePromptPanel.SetActive(true);
            revivePromptText.text = $"Press {reviveKey} to use SELF REVIVE";
            
            // Start countdown coroutine
            if (reviveCoroutine != null)
                StopCoroutine(reviveCoroutine);
            reviveCoroutine = StartCoroutine(RevivePromptCountdown());
        }
        else
        {
            Debug.LogError("SelfReviveUIManager: revivePromptPanel is NULL!");
        }
    }
    
    /// <summary>
    /// Hide the revive prompt
    /// </summary>
    public void HideRevivePrompt()
    {
        Debug.Log("SelfReviveUIManager: Hiding revive prompt");
        
        isRevivePromptActive = false;
        isReviveProcessActive = false;
        
        if (revivePromptPanel != null)
            revivePromptPanel.SetActive(false);
            
        if (reviveCoroutine != null)
        {
            StopCoroutine(reviveCoroutine);
            reviveCoroutine = null;
        }
    }
    
    /// <summary>
    /// Use the revive (called by E key)
    /// </summary>
    public void UseRevive()
    {
        if (!isRevivePromptActive || isReviveProcessActive) return;
        
        Debug.Log("SelfReviveUIManager: Using revive - starting process");
        
        isRevivePromptActive = false;
        isReviveProcessActive = true;
        
        if (revivePromptText != null)
            revivePromptText.text = "Self reviving...";
            
        // Start revive process countdown
        if (reviveCoroutine != null)
            StopCoroutine(reviveCoroutine);
        reviveCoroutine = StartCoroutine(ReviveProcessCountdown());
    }
    
    /// <summary>
    /// Countdown for the initial revive prompt
    /// </summary>
    private IEnumerator RevivePromptCountdown()
    {
        float timeLeft = revivePromptDuration;

        while (timeLeft > 0f && isRevivePromptActive)
        {
            if (countdownText != null)
                countdownText.text = Mathf.Ceil(timeLeft).ToString();

            // Use realtime so countdown is consistent regardless of Time.timeScale
            yield return new WaitForSecondsRealtime(0.1f);
            timeLeft -= 0.1f;
        }

        // Time expired without using revive
        if (isRevivePromptActive)
        {
            Debug.Log("SelfReviveUIManager: Revive prompt expired - no revive used");
            HideRevivePrompt();
            OnReviveExpired?.Invoke();
        }
    }
    
    /// <summary>
    /// Countdown for the revive process
    /// </summary>
    private IEnumerator ReviveProcessCountdown()
    {
        float timeLeft = reviveProcessDuration;

        while (timeLeft > 0f && isReviveProcessActive)
        {
            if (countdownText != null)
                countdownText.text = Mathf.Ceil(timeLeft).ToString();

            // Use realtime so countdown is consistent regardless of Time.timeScale
            yield return new WaitForSecondsRealtime(0.1f);
            timeLeft -= 0.1f;
        }

        // Revive process complete
        if (isReviveProcessActive)
        {
            Debug.Log("SelfReviveUIManager: Revive process complete - player revived");
            HideRevivePrompt();
            OnReviveUsed?.Invoke();
        }
    }
    
    /// <summary>
    /// Check if any revive UI is currently active
    /// </summary>
    public bool IsReviveUIActive()
    {
        return isRevivePromptActive || isReviveProcessActive;
    }
    
    /// <summary>
    /// Check if specifically the revive prompt is active (waiting for E key)
    /// </summary>
    public bool IsRevivePromptActive()
    {
        return isRevivePromptActive;
    }
}
