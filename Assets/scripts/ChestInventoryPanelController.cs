using UnityEngine;
using System.Collections;
using GeminiGauntlet.Audio;

/// <summary>
/// Attach this script to the ChestInventoryPanel GameObject to ensure it becomes visible when requested.
/// This script provides a public interface for other scripts to show/hide the panel.
/// </summary>
public class ChestInventoryPanelController : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Should this panel be hidden at start?")]
    public bool hideAtStart = true;
    
    [Tooltip("Play sound when showing/hiding panel")]
    public bool playSoundEffects = false; // DISABLED: ChestController already plays sounds
    
    [Tooltip("Debug mode")]
    public bool enableDebugLogs = true;
    
    [Header("Optional Animation")]
    [Tooltip("Optional animator component")]
    public Animator panelAnimator;
    
    [Tooltip("Animation trigger parameter for showing panel")]
    public string showTriggerName = "Show";
    
    [Tooltip("Animation trigger parameter for hiding panel")]
    public string hideTriggerName = "Hide";

    // Internal state tracking
    private bool isVisible = false;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        // Try to get canvas group if available
        canvasGroup = GetComponent<CanvasGroup>();
        
        // Log initialization
        if (enableDebugLogs)
        {
            Debug.Log($"ChestInventoryPanelController: Initialized on {gameObject.name}");
        }
    }

    private void Start()
    {
        // 🔥 CRITICAL FIX: Initialize CanvasGroup state BEFORE hiding
        // This ensures the panel is properly set up for the first interaction
        if (canvasGroup != null)
        {
            // Set initial state based on hideAtStart
            if (hideAtStart)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                isVisible = false;
                
                if (enableDebugLogs)
                {
                    Debug.Log($"ChestInventoryPanelController: Panel {gameObject.name} initialized as HIDDEN (CanvasGroup alpha=0)");
                }
            }
            else
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                isVisible = true;
                
                if (enableDebugLogs)
                {
                    Debug.Log($"ChestInventoryPanelController: Panel {gameObject.name} initialized as VISIBLE (CanvasGroup alpha=1)");
                }
            }
        }
        else
        {
            // No CanvasGroup - use GameObject activation
            if (hideAtStart)
            {
                gameObject.SetActive(false);
                isVisible = false;
                
                if (enableDebugLogs)
                {
                    Debug.Log($"ChestInventoryPanelController: Panel {gameObject.name} initialized as HIDDEN (GameObject deactivated)");
                }
            }
            else
            {
                gameObject.SetActive(true);
                isVisible = true;
                
                if (enableDebugLogs)
                {
                    Debug.Log($"ChestInventoryPanelController: Panel {gameObject.name} initialized as VISIBLE (GameObject activated)");
                }
            }
        }
    }

    /// <summary>
    /// Shows the panel with optional animation and sound
    /// </summary>
    /// <param name="playSound">Whether to play sound effect</param>
    public void ShowPanel(bool playSound = true)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"ChestInventoryPanelController: Showing panel {gameObject.name}");
        }
        
        // 🔥 CRITICAL FIX: Ensure CanvasGroup reference exists (in case ShowPanel called before Start)
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (enableDebugLogs && canvasGroup != null)
            {
                Debug.Log($"ChestInventoryPanelController: Late-initialized CanvasGroup reference");
            }
        }
        
        // 🔥 CRITICAL FIX: If no CanvasGroup exists, add one!
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            if (enableDebugLogs)
            {
                Debug.Log($"ChestInventoryPanelController: Created missing CanvasGroup component");
            }
        }
        
        // Make sure GameObject is active FIRST
        gameObject.SetActive(true);
        
        // 🔥 CRITICAL FIX: Force CanvasGroup to visible state immediately
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        
        if (enableDebugLogs)
        {
            Debug.Log($"ChestInventoryPanelController: CanvasGroup set to alpha={canvasGroup.alpha}, interactable={canvasGroup.interactable}, blocksRaycasts={canvasGroup.blocksRaycasts}");
        }
        
        // Trigger animation if available
        if (panelAnimator != null && !string.IsNullOrEmpty(showTriggerName))
        {
            panelAnimator.SetTrigger(showTriggerName);
        }
        
        // Play sound if enabled
        if (playSound && playSoundEffects)
        {
            GameSounds.PlayUIFeedback(transform.position);
        }
        
        isVisible = true;
    }

    /// <summary>
    /// Hides the panel with optional animation and sound
    /// </summary>
    /// <param name="playSound">Whether to play sound effect</param>
    public void HidePanel(bool playSound = true)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"ChestInventoryPanelController: Hiding panel {gameObject.name}");
        }
        
        // Handle CanvasGroup if available
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            
            // Don't deactivate GameObject when using CanvasGroup
        }
        else
        {
            // If no CanvasGroup, deactivate the GameObject
            gameObject.SetActive(false);
        }
        
        // Trigger animation if available
        if (panelAnimator != null && !string.IsNullOrEmpty(hideTriggerName))
        {
            panelAnimator.SetTrigger(hideTriggerName);
        }
        
        // Play sound if enabled
        if (playSound && playSoundEffects)
        {
            GameSounds.PlayUIFeedback(transform.position);
        }
        
        isVisible = false;
    }

    /// <summary>
    /// Toggles the panel visibility
    /// </summary>
    public void TogglePanel()
    {
        if (isVisible)
        {
            HidePanel();
        }
        else
        {
            ShowPanel();
        }
    }

    /// <summary>
    /// Returns whether the panel is currently visible
    /// </summary>
    public bool IsVisible()
    {
        return isVisible;
    }
}
