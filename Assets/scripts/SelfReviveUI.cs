using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SelfReviveUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject revivePromptPanel;
    public TextMeshProUGUI revivePromptText;
    public TextMeshProUGUI countdownText;
    public Button useReviveButton; // Optional - for mouse users
    
    [Header("Settings")]
    public float revivePromptDuration = 10f;
    public float reviveProcessDuration = 5f;
    
    private bool isRevivePromptActive = false;
    private bool isReviveProcessActive = false;
    private Coroutine reviveCoroutine;
    
    // Events
    public System.Action OnReviveUsed;
    public System.Action OnReviveExpired;
    
    void Awake()
    {
        // Ensure UI starts hidden
        if (revivePromptPanel != null)
            revivePromptPanel.SetActive(false);
            
        // Set up button if available
        if (useReviveButton != null)
        {
            useReviveButton.onClick.AddListener(OnUseReviveButtonClicked);
        }
    }
    
    void Update()
    {
        // Listen for E key during revive prompt
        if (isRevivePromptActive && Input.GetKeyDown(KeyCode.E))
        {
            UseRevive();
        }
    }
    
    /// <summary>
    /// Show the revive prompt with countdown
    /// </summary>
    public void ShowRevivePrompt()
    {
        if (isRevivePromptActive || isReviveProcessActive) return;
        
        Debug.Log("SelfReviveUI: Showing revive prompt");
        
        isRevivePromptActive = true;
        
        // Debug UI component assignments
        Debug.Log($"SelfReviveUI: revivePromptPanel = {(revivePromptPanel != null ? "ASSIGNED" : "NULL")}");
        Debug.Log($"SelfReviveUI: revivePromptText = {(revivePromptText != null ? "ASSIGNED" : "NULL")}");
        Debug.Log($"SelfReviveUI: countdownText = {(countdownText != null ? "ASSIGNED" : "NULL")}");
        
        if (revivePromptPanel != null)
        {
            revivePromptPanel.SetActive(true);
            Debug.Log("SelfReviveUI: Panel activated");
        }
        else
        {
            Debug.LogError("SelfReviveUI: revivePromptPanel is NULL! Please assign it in the Inspector.");
        }
            
        if (revivePromptText != null)
        {
            revivePromptText.text = "Press E to use SELF REVIVE";
            Debug.Log("SelfReviveUI: Prompt text set");
        }
        else
        {
            Debug.LogError("SelfReviveUI: revivePromptText is NULL! Please assign it in the Inspector.");
        }
            
        // Start countdown coroutine
        if (reviveCoroutine != null)
            StopCoroutine(reviveCoroutine);
        reviveCoroutine = StartCoroutine(RevivePromptCountdown());
    }
    
    /// <summary>
    /// Hide the revive prompt
    /// </summary>
    public void HideRevivePrompt()
    {
        Debug.Log("SelfReviveUI: Hiding revive prompt");
        
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
    /// Use the revive (called by E key or button)
    /// </summary>
    public void UseRevive()
    {
        if (!isRevivePromptActive || isReviveProcessActive) return;
        
        Debug.Log("SelfReviveUI: Using revive");
        
        isRevivePromptActive = false;
        isReviveProcessActive = true;
        
        if (revivePromptText != null)
            revivePromptText.text = "self reviving...";
            
        // Start revive process countdown
        if (reviveCoroutine != null)
            StopCoroutine(reviveCoroutine);
        reviveCoroutine = StartCoroutine(ReviveProcessCountdown());
    }
    
    /// <summary>
    /// Button click handler
    /// </summary>
    private void OnUseReviveButtonClicked()
    {
        UseRevive();
    }
    
    /// <summary>
    /// Countdown for the initial revive prompt (10 seconds)
    /// </summary>
    private IEnumerator RevivePromptCountdown()
    {
        float timeLeft = revivePromptDuration;
        
        while (timeLeft > 0 && isRevivePromptActive)
        {
            if (countdownText != null)
                countdownText.text = Mathf.Ceil(timeLeft).ToString();
                
            yield return new WaitForSeconds(1f);
            timeLeft -= 1f;
        }
        
        // Time expired without using revive
        if (isRevivePromptActive)
        {
            Debug.Log("SelfReviveUI: Revive prompt expired");
            HideRevivePrompt();
            OnReviveExpired?.Invoke();
        }
    }
    
    /// <summary>
    /// Countdown for the revive process (5 seconds)
    /// </summary>
    private IEnumerator ReviveProcessCountdown()
    {
        float timeLeft = reviveProcessDuration;
        
        while (timeLeft > 0 && isReviveProcessActive)
        {
            if (countdownText != null)
                countdownText.text = Mathf.Ceil(timeLeft).ToString();
                
            yield return new WaitForSeconds(1f);
            timeLeft -= 1f;
        }
        
        // Revive process complete
        if (isReviveProcessActive)
        {
            Debug.Log("SelfReviveUI: Revive process complete");
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
    
    /// <summary>
    /// Check if the revive process is active (5 second countdown)
    /// </summary>
    public bool IsReviveProcessActive()
    {
        return isReviveProcessActive;
    }
}
