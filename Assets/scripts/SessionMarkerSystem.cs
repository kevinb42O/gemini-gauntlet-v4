using UnityEngine;
using System.Collections;

/// <summary>
/// Session Marker System - Quick Save/Load positions during gameplay
/// INSERT = Set marker (ONLY when grounded)
/// DELETE = Teleport to marker with fade effect (can be used anytime)
/// </summary>
public class SessionMarkerSystem : MonoBehaviour
{
    [Header("Session Marker Settings")]
    [Tooltip("Show debug logs for marker actions")]
    [SerializeField] private bool showDebugLogs = true;
    
    [Header("Visual Feedback")]
    [Tooltip("Color for marker set notification")]
    [SerializeField] private Color markerSetColor = new Color(0.3f, 1f, 0.3f); // Green
    
    [Tooltip("Color for marker teleport notification")]
    [SerializeField] private Color markerTeleportColor = new Color(0.2f, 0.8f, 1f); // Cyan
    
    [Header("Teleport Settings")]
    [Tooltip("Duration of fade effect for teleport")]
    [SerializeField] private float fadeDuration = 0.3f;
    
    // Marker state
    private bool hasMarker = false;
    private Vector3 markerPosition;
    private Quaternion markerRotation;
    
    // References
    private Transform playerTransform;
    private CharacterController characterController;
    private Rigidbody rb;
    private UIManager uiManager;
    private CognitiveFeedManagerEnhanced cognitiveFeedManager;
    private AAAMovementController movementController;
    
    // Teleport state
    private bool isTeleporting = false;

    void Awake()
    {
        // Get player components
        playerTransform = transform;
        characterController = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();
        movementController = GetComponent<AAAMovementController>();
        
        // Find managers
        uiManager = FindFirstObjectByType<UIManager>();
        cognitiveFeedManager = FindFirstObjectByType<CognitiveFeedManagerEnhanced>();
        
        if (uiManager == null)
        {
            Debug.LogWarning("[SessionMarker] UIManager not found. Fade effects will not work.");
        }
        
        if (cognitiveFeedManager == null)
        {
            Debug.LogWarning("[SessionMarker] CognitiveFeedManager not found. Feedback messages will not display.");
        }
        
        if (movementController == null)
        {
            Debug.LogError("[SessionMarker] AAAMovementController not found. Grounded checks will not work!");
        }
    }

    void Update()
    {
        // Don't process input during teleport
        if (isTeleporting) return;
        
        // Don't process if game is paused
        if (Time.timeScale == 0f) return;
        
        // INSERT = Set Session Marker
        if (Input.GetKeyDown(KeyCode.Insert))
        {
            SetSessionMarker();
        }
        
        // DELETE = Teleport to Session Marker
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            TeleportToMarker();
        }
    }

    /// <summary>
    /// Set the current position as session marker
    /// </summary>
    private void SetSessionMarker()
    {
        // CRITICAL: Only allow setting marker while grounded
        if (movementController != null && !movementController.IsGrounded)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning("[SessionMarker] ✗ Cannot set marker while airborne! You must be grounded.");
            }
            
            ShowMarkerFeedback("MUST BE GROUNDED TO SET MARKER", Color.red);
            return;
        }
        
        markerPosition = playerTransform.position;
        markerRotation = playerTransform.rotation;
        hasMarker = true;
        
        if (showDebugLogs)
        {
            Debug.Log($"[SessionMarker] ✓ Session marker SET at {markerPosition}");
        }
        
        // Show feedback via CognitiveFeedManager
        ShowMarkerFeedback("SESSION MARKER SET", markerSetColor);
    }

    /// <summary>
    /// Teleport player to the saved session marker
    /// </summary>
    private void TeleportToMarker()
    {
        if (!hasMarker)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning("[SessionMarker] No session marker set. Use INSERT to set a marker first.");
            }
            
            ShowMarkerFeedback("NO SESSION MARKER SET", Color.red);
            return;
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[SessionMarker] → Teleporting to marker at {markerPosition}");
        }
        
        // Start teleport sequence with fade effect
        StartCoroutine(TeleportSequence());
    }

    /// <summary>
    /// Teleport sequence with fade to black effect
    /// </summary>
    private IEnumerator TeleportSequence()
    {
        isTeleporting = true;
        
        // Fade to black
        if (uiManager != null && uiManager.fadePanelCanvasGroup != null)
        {
            yield return StartCoroutine(FadeToBlack());
        }
        else
        {
            // Fallback if no fade system available
            yield return new WaitForSeconds(0.1f);
        }
        
        // Perform teleport
        PerformTeleport();
        
        // Show feedback after teleport
        ShowMarkerFeedback("TELEPORTED TO MARKER", markerTeleportColor);
        
        // Fade from black
        if (uiManager != null && uiManager.fadePanelCanvasGroup != null)
        {
            yield return StartCoroutine(FadeFromBlack());
        }
        else
        {
            // Fallback
            yield return new WaitForSeconds(0.1f);
        }
        
        isTeleporting = false;
        
        if (showDebugLogs)
        {
            Debug.Log("[SessionMarker] ✓ Teleport complete");
        }
    }

    /// <summary>
    /// Perform the actual teleport
    /// </summary>
    private void PerformTeleport()
    {
        // Disable CharacterController temporarily
        if (characterController != null)
        {
            characterController.enabled = false;
        }
        
        // Set position and rotation
        playerTransform.position = markerPosition;
        playerTransform.rotation = markerRotation;
        
        // Reset velocities
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        // Re-enable CharacterController
        if (characterController != null)
        {
            characterController.enabled = true;
        }
        
        // Reset any movement controller velocities
        var movementController = GetComponent<AAAMovementController>();
        if (movementController != null)
        {
            // Use reflection to reset velocity if needed
            var velocityField = movementController.GetType().GetField("velocity", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (velocityField != null)
            {
                velocityField.SetValue(movementController, Vector3.zero);
            }
        }
    }

    /// <summary>
    /// Fade to black effect
    /// </summary>
    private IEnumerator FadeToBlack()
    {
        CanvasGroup fadePanel = uiManager.fadePanelCanvasGroup;
        fadePanel.gameObject.SetActive(true);
        fadePanel.alpha = 0f;
        
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadePanel.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }
        
        fadePanel.alpha = 1f;
    }

    /// <summary>
    /// Fade from black effect
    /// </summary>
    private IEnumerator FadeFromBlack()
    {
        CanvasGroup fadePanel = uiManager.fadePanelCanvasGroup;
        fadePanel.alpha = 1f;
        
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadePanel.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }
        
        fadePanel.alpha = 0f;
        fadePanel.gameObject.SetActive(false);
    }

    /// <summary>
    /// Show feedback message via CognitiveFeedManager
    /// </summary>
    private void ShowMarkerFeedback(string message, Color color)
    {
        if (cognitiveFeedManager == null) return;
        
        // Format message with color
        string formattedMessage = $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{message}</color>";
        
        // Show via cognitive feed manager
        cognitiveFeedManager.ShowQuickNotification(formattedMessage, 2f);
    }

    /// <summary>
    /// Public method to manually set marker position
    /// </summary>
    public void SetMarkerAt(Vector3 position, Quaternion rotation)
    {
        markerPosition = position;
        markerRotation = rotation;
        hasMarker = true;
        
        if (showDebugLogs)
        {
            Debug.Log($"[SessionMarker] ✓ Session marker manually set at {position}");
        }
    }

    /// <summary>
    /// Public method to check if marker is set
    /// </summary>
    public bool HasMarker()
    {
        return hasMarker;
    }

    /// <summary>
    /// Public method to clear the marker
    /// </summary>
    public void ClearMarker()
    {
        hasMarker = false;
        
        if (showDebugLogs)
        {
            Debug.Log("[SessionMarker] ✗ Session marker cleared");
        }
        
        ShowMarkerFeedback("SESSION MARKER CLEARED", Color.yellow);
    }
}
