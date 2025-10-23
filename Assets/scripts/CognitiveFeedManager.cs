// --- CognitiveFeedManager.cs (CORRECTED) ---
using UnityEngine;
using GeminiGauntlet.Audio;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Message priority categories for cognitive feed
/// CRITICAL = Item info, inventory, urgent player-affecting events (cannot be interrupted)
/// HIGH = Time-sensitive system messages (elevator, doors, mechanics)
/// NORMAL = Standard feedback, tutorials, commentary
/// </summary>
public enum MessagePriority
{
    NORMAL = 0,      // Can be interrupted by anything
    HIGH = 1,        // Can only be interrupted by CRITICAL
    CRITICAL = 2     // Cannot be interrupted (item info, inventory)
}

public class CognitiveFeedManager : MonoBehaviour
{
    public static CognitiveFeedManager Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI cognitiveText;
    public CanvasGroup cognitivePanelCanvasGroup;
    
    [Header("Persistent Message UI (For Landing Prompts)")]
    public TextMeshProUGUI persistentMessageText;
    public CanvasGroup persistentMessagePanelCanvasGroup;
    private Coroutine _persistentMessageCoroutine;

    [Header("Typing Effect Settings")]
    public float wordsPerSecond = 5f;
    public AudioClip wordSound;
    [Range(0, 1)] public float wordSoundVolume = 0.5f;

    [Header("Special Pauses")]
    public float ellipsisPause = 0.5f;
    public float thinkingPause = 1.0f;

    [Header("Display Timings")]
    public float holdDuration = 4.0f;
    public float fadeDuration = 0.3f;

    private AudioSource _audioSource;
    private Queue<string> messageQueue = new Queue<string>();
    private Coroutine _displayCoroutine;
    private MessagePriority _currentMessagePriority = MessagePriority.NORMAL;

    private bool hasWelcomedPlayer = false;
    private bool hasExplainedGems = false;
    private bool hasExplainedGemAttraction = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            _audioSource = gameObject.AddComponent<AudioSource>();
            hasWelcomedPlayer = PlayerPrefs.GetInt("HasWelcomedPlayer", 0) == 1;
        }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        if (cognitiveText == null || cognitivePanelCanvasGroup == null)
        {
            Debug.LogError("CognitiveFeedManager is missing UI references! Disabling.", this);
            enabled = false;
            return;
        }

        if (persistentMessageText == null || persistentMessagePanelCanvasGroup == null)
        {
            Debug.LogWarning("CognitiveFeedManager is missing Persistent Message UI references. Landing prompts will not be shown.", this);
        }
        else
        {
            persistentMessagePanelCanvasGroup.alpha = 0;
        }

        cognitivePanelCanvasGroup.alpha = 0;
        
        StartCoroutine(WelcomeRoutine());
    }

    // --- FIX: Event subscriptions are now in OnEnable for robustness ---
    void OnEnable()
    {
        // Subscribe to the event from the NEW manager
        PlayerMovementManager.OnPlayerLandedOnNewPlatform += HandleNewPlatformLanded;
        
        // These other subscriptions are also safer here
        PlayerProgression.OnSpendableGemsChanged += HandleGemCollection;
        if (PlayerInputHandler.Instance != null)
        {
            PlayerInputHandler.Instance.OnPrimaryDoubleClickGemCollectAction += HandleGemAttractionTutorial;
            PlayerInputHandler.Instance.OnSecondaryDoubleClickGemCollectAction += HandleGemAttractionTutorial;
        }
        
        // SOURCE OF TRUTH: Subscribe to inventory events to hide messages when inventory opens
        CognitiveEvents.OnInventoryOpened += HandleInventoryOpened;
        CognitiveEvents.OnInventoryClosed += HandleInventoryClosed;
    }

    // --- FIX: Unsubscribe in OnDisable to match OnEnable ---
    void OnDisable()
    {
        // Unsubscribe from the event on the NEW manager.
        // We no longer need to check for a singleton instance.
        PlayerMovementManager.OnPlayerLandedOnNewPlatform -= HandleNewPlatformLanded;
        
        PlayerProgression.OnSpendableGemsChanged -= HandleGemCollection;
        if (PlayerInputHandler.Instance != null)
        {
            PlayerInputHandler.Instance.OnPrimaryDoubleClickGemCollectAction -= HandleGemAttractionTutorial;
            PlayerInputHandler.Instance.OnSecondaryDoubleClickGemCollectAction -= HandleGemAttractionTutorial;
        }
        
        // SOURCE OF TRUTH: Unsubscribe from inventory events
        CognitiveEvents.OnInventoryOpened -= HandleInventoryOpened;
        CognitiveEvents.OnInventoryClosed -= HandleInventoryClosed;
    }
    private IEnumerator WelcomeRoutine()
    {
        yield return new WaitForSeconds(3.0f);
        
        if (!hasWelcomedPlayer && FlavorTextManager.Instance?.idleMessages.Length > 0)
        {
            QueueMessage(FlavorTextManager.Instance.idleMessages[0]);
            hasWelcomedPlayer = true;
            
            PlayerPrefs.SetInt("HasWelcomedPlayer", 1);
            PlayerPrefs.Save();
        }
    }

    public void QueueMessage(string message)
    {
        if (string.IsNullOrEmpty(message)) return;
        
        // SOURCE OF TRUTH: Block queued messages when inventory is open
        if (InventoryManager.Instance != null && InventoryManager.Instance.IsInventoryVisible())
        {
            Debug.Log($"[CognitiveFeed] Queued message blocked - inventory is open");
            return;
        }
        
        messageQueue.Enqueue(message);
        
        if (_displayCoroutine == null)
        {
            _displayCoroutine = StartCoroutine(DisplayMessageRoutine());
        }
    }
    
    /// <summary>
    /// Displays an instant message with priority protection
    /// </summary>
    /// <param name="message">Message to display</param>
    /// <param name="displayTime">How long to show the message (seconds)</param>
    /// <param name="priority">Message priority level (default: HIGH)</param>
    public void ShowInstantMessage(string message, float displayTime = 2.0f, MessagePriority priority = MessagePriority.HIGH)
    {
        Debug.Log($"[CognitiveFeed] 📨 ShowInstantMessage() called: '{message.Substring(0, Mathf.Min(50, message.Length))}...'");
        
        if (string.IsNullOrEmpty(message)) return;
        
        // SOURCE OF TRUTH: Block ALL messages when inventory is open
        bool hasInventoryManager = InventoryManager.Instance != null;
        bool isInventoryOpen = hasInventoryManager && InventoryManager.Instance.IsInventoryVisible();
        Debug.Log($"[CognitiveFeed] 🔍 Inventory check: InventoryManager exists={hasInventoryManager}, isOpen={isInventoryOpen}");
        
        if (isInventoryOpen)
        {
            Debug.LogWarning($"[CognitiveFeed] ❌ Message BLOCKED - inventory is open: '{message.Substring(0, Mathf.Min(30, message.Length))}...'");
            return;
        }
        
        // Priority protection: don't interrupt higher priority messages
        if (_currentMessagePriority > priority)
        {
            Debug.Log($"[CognitiveFeed] Message blocked: current priority={_currentMessagePriority}, incoming priority={priority}");
            return;
        }
        
        // Stop any existing display that can be interrupted
        if (_displayCoroutine != null)
        {
            StopCoroutine(_displayCoroutine);
            _displayCoroutine = null;
        }
        
        // Start instant display with priority
        _displayCoroutine = StartCoroutine(DisplayInstantMessage(message, displayTime, priority));
    }

    private IEnumerator DisplayInstantMessage(string message, float displayTime, MessagePriority priority)
    {
        // Set current priority
        _currentMessagePriority = priority;
        
        // Instantly show panel and message
        cognitivePanelCanvasGroup.alpha = 1;
        cognitiveText.text = message;
        
        // Hold for specified time
        yield return new WaitForSeconds(displayTime);
        
        // Fade out
        yield return StartCoroutine(FadeCanvasGroup(cognitivePanelCanvasGroup, 1, 0, fadeDuration));
        
        // Reset priority after message completes
        _currentMessagePriority = MessagePriority.NORMAL;
        
        // Check if we should resume the message queue
        if (messageQueue.Count > 0)
        {
            _displayCoroutine = StartCoroutine(DisplayMessageRoutine());
        }
        else
        {
            _displayCoroutine = null;
        }
    }

    private IEnumerator DisplayMessageRoutine()
    {
        while (messageQueue.Count > 0)
        {
            // Queued messages are always NORMAL priority
            _currentMessagePriority = MessagePriority.NORMAL;
            
            string fullMessage = messageQueue.Dequeue();
            string[] words = fullMessage.Split(' ');

            cognitiveText.text = "";
            yield return StartCoroutine(FadeCanvasGroup(cognitivePanelCanvasGroup, 0, 1, fadeDuration));

            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                string currentText = cognitiveText.text;

                if (word.Contains("..."))
                {
                    cognitiveText.text += "... ";
                    yield return new WaitForSeconds(ellipsisPause);
                    continue;
                }
                if (word.StartsWith("*") && word.EndsWith("*"))
                {
                    cognitiveText.text = "";
                    string thinkingText = word.Trim('*');
                    yield return StartCoroutine(TypeOutThinkingText(thinkingText));
                    yield return new WaitForSeconds(thinkingPause);
                    cognitiveText.text = currentText;
                    continue;
                }

                cognitiveText.text += word + " ";
                if (wordSound != null)
                {
                    GameSounds.PlayUIFeedback(transform.position, wordSoundVolume);
                }

                yield return new WaitForSeconds(1f / wordsPerSecond);
            }

            yield return new WaitForSeconds(holdDuration);
            yield return StartCoroutine(FadeCanvasGroup(cognitivePanelCanvasGroup, 1, 0, fadeDuration));
            
            // Reset priority after each message
            _currentMessagePriority = MessagePriority.NORMAL;
        }
        _displayCoroutine = null;
    }

    private IEnumerator TypeOutThinkingText(string text)
    {
        cognitiveText.fontStyle = FontStyles.Italic;
        float charsPerSecond = 10f;
        foreach (char c in text)
        {
            cognitiveText.text += c;
            yield return new WaitForSeconds(1f / charsPerSecond);
        }
        cognitiveText.fontStyle = FontStyles.Normal;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(start, end, timer / duration);
            yield return null;
        }
        cg.alpha = end;
    }

    private void HandleNewPlatformLanded(Transform platformTransform)
    {
        // Use platform lock messages instead of the removed newPlatformMessages
        if (FlavorTextManager.Instance != null)
        {
            string message = FlavorTextManager.Instance.GetPlatformLockOnMessage();
            QueueMessage(message);
        }
    }

    private void HandleGemCollection(int newTotal)
    {
        if (!hasExplainedGems && newTotal > 0)
        {
            QueueMessage("[Flight Systems]ONLINE,[Hand Firing Mechanisms]ONLINE");
            hasExplainedGems = true;
        }
    }

    private void HandleGemAttractionTutorial()
    {
        if (!hasExplainedGemAttraction)
        {
            QueueMessage("Try Left or Right DOUBLE click to COLLECT some Gems.");
            hasExplainedGemAttraction = true;
        }
    }
    
    /// <summary>
    /// SOURCE OF TRUTH: Hide all cognitive messages when inventory opens
    /// </summary>
    private void HandleInventoryOpened()
    {
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("[CognitiveFeed] 🔵 HandleInventoryOpened() EVENT RECEIVED");
        
        // Stop any active display coroutines
        if (_displayCoroutine != null)
        {
            Debug.Log("[CognitiveFeed] ⏸️ Stopping active display coroutine");
            StopCoroutine(_displayCoroutine);
            _displayCoroutine = null;
        }
        
        // Hide main cognitive panel immediately
        if (cognitivePanelCanvasGroup != null)
        {
            Debug.Log($"[CognitiveFeed] 🙈 Hiding cognitive panel (alpha {cognitivePanelCanvasGroup.alpha} -> 0)");
            cognitivePanelCanvasGroup.alpha = 0;
        }
        
        // Hide persistent message panel
        if (persistentMessagePanelCanvasGroup != null)
        {
            Debug.Log($"[CognitiveFeed] 🙈 Hiding persistent panel (alpha {persistentMessagePanelCanvasGroup.alpha} -> 0)");
            persistentMessagePanelCanvasGroup.alpha = 0;
        }
        
        // Reset priority
        _currentMessagePriority = MessagePriority.NORMAL;
        
        Debug.Log("[CognitiveFeed] ✅ Inventory opened - all messages hidden");
        Debug.Log("═══════════════════════════════════════════════════════");
    }
    
    /// <summary>
    /// SOURCE OF TRUTH: Resume message queue when inventory closes
    /// </summary>
    private void HandleInventoryClosed()
    {
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("[CognitiveFeed] 🔴 HandleInventoryClosed() EVENT RECEIVED");
        Debug.Log($"[CognitiveFeed] Message queue count: {messageQueue.Count}");
        
        // Resume message queue if there are pending messages
        if (messageQueue.Count > 0 && _displayCoroutine == null)
        {
            Debug.Log("[CognitiveFeed] ▶️ Resuming message queue");
            _displayCoroutine = StartCoroutine(DisplayMessageRoutine());
        }
        else
        {
            Debug.Log("[CognitiveFeed] No messages to resume");
        }
        
        Debug.Log("[CognitiveFeed] ✅ Inventory closed - message system resumed");
        Debug.Log("═══════════════════════════════════════════════════════");
    }

    public void ShowPersistentMessage(string message)
    {
        if (persistentMessagePanelCanvasGroup == null) return;
        
        // SOURCE OF TRUTH: Block persistent messages when inventory is open
        if (InventoryManager.Instance != null && InventoryManager.Instance.IsInventoryVisible())
        {
            Debug.Log($"[CognitiveFeed] Persistent message blocked - inventory is open");
            return;
        }

        if (_persistentMessageCoroutine != null)
        {
            StopCoroutine(_persistentMessageCoroutine);
        }

        persistentMessageText.text = message;
        _persistentMessageCoroutine = StartCoroutine(FadeCanvasGroup(persistentMessagePanelCanvasGroup, persistentMessagePanelCanvasGroup.alpha, 1, fadeDuration));
    }

    public void HidePersistentMessage()
    {
        if (persistentMessagePanelCanvasGroup == null || persistentMessagePanelCanvasGroup.alpha == 0) return;
        
        if (_persistentMessageCoroutine != null)
        {
            StopCoroutine(_persistentMessageCoroutine);
        }
        
        _persistentMessageCoroutine = StartCoroutine(FadeCanvasGroup(persistentMessagePanelCanvasGroup, persistentMessagePanelCanvasGroup.alpha, 0, fadeDuration));
    }
}