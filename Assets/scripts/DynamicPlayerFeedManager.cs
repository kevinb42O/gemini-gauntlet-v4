// --- DynamicPlayerFeedManager.cs (FULL & FINAL - With Dynamic Padding) ---
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;

public class DynamicPlayerFeedManager : MonoBehaviour
{
    public static DynamicPlayerFeedManager Instance { get; private set; }

    [Header("Core UI Setup")]
    [Tooltip("The UI Prefab for a single feed message. Must have FeedItemController.cs on it.")]
    public GameObject feedItemPrefab;
    [Tooltip("The SINGLE parent RectTransform for ALL messages. MUST have a VerticalLayoutGroup with SPACING set to 0.")]
    public RectTransform feedContainer;

    [Tooltip("Maximum number of messages visible at once. Items are removed from the top.")]
    public int maxVisibleFeedItems = 8;

    [Header("Visual Style & Configuration")]
    [Tooltip("Holds all the icon sprites for the feed system.")]
    public FeedIconsSO feedIcons;
    [Tooltip("Default duration for messages that don't specify one.")]
    public float defaultMessageDuration = 3.0f;

    [Header("MAJOR Event Style (Gems, Power-Up Pickups/Warnings)")]
    public float majorEventFontSize = 26f;
    public float majorEventIconSize = 30f;

    [Header("MINOR Event Style (Kills, Level Ups, etc.)")]
    public float minorEventFontSize = 18f;
    public float minorEventIconSize = 22f;

    [Header("Event-Specific Colors")]
    public Color gemCollectColor = new Color(1f, 0.92f, 0.016f);
    public Color powerUpColor = new Color(0.2f, 1f, 1f);
    public Color powerUpWarningColor = new Color(1f, 0.75f, 0f);
    public Color statusUpdateColor = new Color(0.8f, 0.8f, 0.8f);
    public Color killFeedColor = new Color(0.7f, 0.7f, 0.7f);
    public Color overheatWarningColor = new Color(1f, 0.64f, 0f);
    public Color overheatCriticalColor = Color.red;
    public Color overheatRecoveredColor = new Color(0.6f, 1f, 0.6f);

    [Header("XP Feed Settings")]
    [Tooltip("Color for XP gain feed messages (subtle green by default).")]
    public Color xpCollectColor = new Color(0.7f, 1f, 0.7f);
    [Tooltip("Optional override sprite for XP messages. If null, falls back to FeedIconsSO.infoIcon.")]
    public Sprite xpIconOverride = null;

    private List<FeedItemController> _activeFeedItems = new List<FeedItemController>();
    private FeedItemController _criticalOverheatFeedItemPrimary = null;
    private FeedItemController _criticalOverheatFeedItemSecondary = null;
    private Dictionary<PowerUpType, bool> _powerUpWarningShown = new Dictionary<PowerUpType, bool>();

    private int _currentGemCollectionCount = 0;
    private float _lastGemCollectionTime = 0f;
    private const float GEM_COLLECTION_WINDOW = 1.5f; // Time window to combine gem collections
    private FeedItemController _currentGemFeedItem = null;

    // XP aggregation state
    private int _currentXPGainTotal = 0;
    private float _lastXPGainTime = 0f;
    private const float XP_COLLECTION_WINDOW = 2.0f; // Time window to combine XP gains
    private FeedItemController _currentXPFeedItem = null;

    void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); return; }

        if (feedItemPrefab == null) Debug.LogError("FeedManager: FeedItemPrefab not assigned!", this);
        if (feedContainer == null) Debug.LogError("FeedManager: FeedContainer not assigned!", this);
        if (feedIcons == null) Debug.LogError("FeedManager: FeedIcons ScriptableObject not assigned!", this);
    }

    // --- MODIFIED: Added isMajor parameter ---
    private void AddFeedItem(string message, Color color, Sprite icon, float duration, float fontSize, float iconSize, bool isMajor, bool isCriticalUpdate = false, bool isPrimaryCritical = true)
    {
        if (feedItemPrefab == null || feedContainer == null || !gameObject.activeInHierarchy) return;

        FeedItemController existingCriticalItem = isCriticalUpdate ? (isPrimaryCritical ? _criticalOverheatFeedItemPrimary : _criticalOverheatFeedItemSecondary) : null;

        if (existingCriticalItem != null)
        {
            // Critical items are always major
            existingCriticalItem.Initialize(message, color, icon, iconSize, duration > 0 ? duration : defaultMessageDuration, true);
            if (existingCriticalItem.notificationText != null) { existingCriticalItem.notificationText.fontSize = fontSize; }
        }
        else
        {
            CullToMaxVisible();

            GameObject newItemGO = Instantiate(feedItemPrefab, feedContainer);
            FeedItemController itemController = newItemGO.GetComponent<FeedItemController>();

            if (itemController != null)
            {
                if (itemController.notificationText != null) { itemController.notificationText.fontSize = fontSize; }
                // --- MODIFIED: Pass the isMajor flag to the controller ---
                itemController.Initialize(message, color, icon, iconSize, duration > 0 ? duration : defaultMessageDuration, isMajor);

                _activeFeedItems.Add(itemController);
                StartCoroutine(RemoveItemWhenDestroyed(itemController));

                if (isCriticalUpdate)
                {
                    if (isPrimaryCritical) _criticalOverheatFeedItemPrimary = itemController;
                    else _criticalOverheatFeedItemSecondary = itemController;
                }
            }
            else { Destroy(newItemGO); }
        }
    }

    private void CullToMaxVisible()
    {
        while (_activeFeedItems.Count >= maxVisibleFeedItems)
        {
            int removeIndex = 0;
            if (_currentXPFeedItem != null)
            {
                // Prefer removing a non-XP item if possible
                int idx = _activeFeedItems.FindIndex(i => i != _currentXPFeedItem);
                if (idx >= 0) removeIndex = idx;
            }

            FeedItemController item = _activeFeedItems[removeIndex];
            if (item != null) { item.ForceFadeOutAndDestroy(); }
            _activeFeedItems.RemoveAt(removeIndex);
        }
    }

    private IEnumerator RemoveItemWhenDestroyed(FeedItemController item)
    {
        yield return new WaitUntil(() => item == null);
        if (_activeFeedItems.Contains(item)) { _activeFeedItems.Remove(item); }
        if (_criticalOverheatFeedItemPrimary == item) _criticalOverheatFeedItemPrimary = null;
        if (_criticalOverheatFeedItemSecondary == item) _criticalOverheatFeedItemSecondary = null;
        if (_currentXPFeedItem == item) _currentXPFeedItem = null;
    }

    public void ShowCustomMessage(string message, Color color, Sprite icon, bool isMajorEvent, float duration = -1f)
    {
        // Intercept XP messages to coalesce into a single incremental feed item
        if (TryExtractXPGain(message, out int xpAmount) && xpAmount > 0)
        {
            ShowXPGained(xpAmount);
            return;
        }

        float fontSize = isMajorEvent ? majorEventFontSize : minorEventFontSize;
        float iconSize = isMajorEvent ? majorEventIconSize : minorEventIconSize;
        float messageDuration = (duration > 0) ? duration : defaultMessageDuration;
        AddFeedItem(message, color, icon, messageDuration, fontSize, iconSize, isMajorEvent);
    }

    // --- NEW: Coalesced XP feed (single incremental message within a 2s inactivity window) ---
    public void ShowXPGained(int amount)
    {
        if (feedItemPrefab == null || feedContainer == null || !gameObject.activeInHierarchy) return;
        float currentTime = Time.time;

        // Continue updating the same item if within the time window and we still have a valid item
        if (currentTime - _lastXPGainTime < XP_COLLECTION_WINDOW && _currentXPFeedItem != null)
        {
            _currentXPGainTotal += amount;
            string message = $"+{_currentXPGainTotal} XP";
            _currentXPFeedItem.UpdateMessage(message);
            _currentXPFeedItem.ResetDuration(2.0f); // keep it visible while XP keeps coming
        }
        else
        {
            // Start a new XP accumulation window
            _currentXPGainTotal = amount;
            string message = $"+{amount} XP";

            // Respect max visible items for XP as well, but try not to cull the active XP item
            CullToMaxVisible();

            GameObject newItemGO = Instantiate(feedItemPrefab, feedContainer);
            FeedItemController itemController = newItemGO.GetComponent<FeedItemController>();
            if (itemController != null)
            {
                Sprite icon = xpIconOverride != null ? xpIconOverride : (feedIcons != null ? feedIcons.infoIcon : null);
                // XP is a minor-style, subtle event by default
                if (itemController.notificationText != null) { itemController.notificationText.fontSize = minorEventFontSize; }
                itemController.Initialize(message, xpCollectColor, icon, minorEventIconSize, 2.0f, false);
                _currentXPFeedItem = itemController;
                _activeFeedItems.Add(itemController);
                StartCoroutine(RemoveItemWhenDestroyed(itemController));
            }
            else
            {
                Destroy(newItemGO);
            }
        }

        _lastXPGainTime = currentTime;
    }

    public void ShowGemCollected(int amount)
    {
        float currentTime = Time.time;

        // If we're within the collection window and have an active gem feed item
        if (currentTime - _lastGemCollectionTime < GEM_COLLECTION_WINDOW && _currentGemFeedItem != null)
        {
            // Update the existing gem count
            _currentGemCollectionCount += amount;
            string message = $"+{_currentGemCollectionCount}";
            _currentGemFeedItem.UpdateMessage(message);
            _currentGemFeedItem.ResetDuration(2.0f); // Reset the display duration
        }
        else
        {
            // Start a new gem collection display
            _currentGemCollectionCount = amount;
            string message = $"+{amount}";

            // Enforce max items before creating new gem item
            CullToMaxVisible();

            // Create new feed item
            GameObject newItemGO = Instantiate(feedItemPrefab, feedContainer);
            FeedItemController itemController = newItemGO.GetComponent<FeedItemController>();

            if (itemController != null)
            {
                itemController.Initialize(message, gemCollectColor, feedIcons.gemCollected, majorEventIconSize, 2.0f, true);
                _currentGemFeedItem = itemController;
                _activeFeedItems.Add(itemController);
                StartCoroutine(RemoveItemWhenDestroyed(itemController));
            }
        }

        _lastGemCollectionTime = currentTime;
    }

    public void ShowPowerUpCollected(PowerUpType type, int charges = -1) { _powerUpWarningShown[type] = false; string message = $"Power-Up: {FormatPowerUpName(type)}"; if (charges > 0) message += $" (x{charges})"; AddFeedItem(message, powerUpColor, feedIcons.GetIconForPowerUp(type), defaultMessageDuration, majorEventFontSize, majorEventIconSize, true); }
    public void ShowPowerUpEndingSoon(PowerUpType type) { if (_powerUpWarningShown.TryGetValue(type, out bool hasBeenShown) && hasBeenShown) return; _powerUpWarningShown[type] = true; string message = $"{FormatPowerUpName(type)} ending soon!"; AddFeedItem(message, powerUpWarningColor, feedIcons.GetIconForPowerUp(type), 4.5f, majorEventFontSize, majorEventIconSize, true); }
    public void UpdateOverheatCriticalCountdown(bool isPrimaryHand, float remainingTime) { string hand = isPrimaryHand ? "Primary" : "Secondary"; string message = $"{hand} HAND OVERHEAT: {remainingTime:F1}s"; AddFeedItem(message, overheatCriticalColor, feedIcons.overheated, remainingTime + 0.2f, majorEventFontSize, majorEventIconSize, true, true, isPrimaryHand); }
    public void ShowKillFeed(string enemyName, FeedIconsSO.KillFeedType killType) { string message = $"Defeated {enemyName}"; AddFeedItem(message, killFeedColor, feedIcons.GetIconForKillFeed(killType), 2.5f, minorEventFontSize, minorEventIconSize, false); }
    public void ShowPowerUpEnded(PowerUpType type) { string message = $"{FormatPowerUpName(type)} Ended"; AddFeedItem(message, statusUpdateColor, feedIcons.GetIconForPowerUp(type), 1.5f, minorEventFontSize, minorEventIconSize, false); }
    public void ShowHandLevelUp(bool isPrimaryHand, int newLevel) { string hand = isPrimaryHand ? "Primary" : "Secondary"; AddFeedItem($"{hand} Hand Level {newLevel}", statusUpdateColor, feedIcons.levelUp, 3.0f, minorEventFontSize, minorEventIconSize, false); }
    public void ShowSecondaryHandUnlocked() { AddFeedItem("Secondary Hand Unlocked", statusUpdateColor, feedIcons.handUnlocked, 3.0f, minorEventFontSize, minorEventIconSize, false); }
    public void ShowHandDegraded(bool isPrimaryHand, int newLevel) { string hand = isPrimaryHand ? "Primary" : "Secondary"; AddFeedItem($"{hand} Hand Level Degraded to {newLevel}", overheatWarningColor, feedIcons.levelDown, 3.0f, minorEventFontSize, minorEventIconSize, false); }
    public void ShowOverheatWarning(bool isPrimaryHand) { string hand = isPrimaryHand ? "Primary" : "Secondary"; AddFeedItem($"{hand} Hand High Heat", overheatWarningColor, feedIcons.warningIcon, 1.5f, minorEventFontSize, minorEventIconSize, false); }
    public void ShowOverheatRecovered(bool isPrimaryHand) { ClearOverheatCriticalMessage(isPrimaryHand); string hand = isPrimaryHand ? "Primary" : "Secondary"; AddFeedItem($"{hand} Hand Cooled", overheatRecoveredColor, feedIcons.recovered, 2.0f, minorEventFontSize, minorEventIconSize, false); }
    public void ClearOverheatCriticalMessage(bool isPrimaryHand) { FeedItemController itemToClear = isPrimaryHand ? _criticalOverheatFeedItemPrimary : _criticalOverheatFeedItemSecondary; itemToClear?.ForceFadeOutAndDestroy(); }
    private string FormatPowerUpName(PowerUpType type) { switch (type) { case PowerUpType.MaxHandUpgrade: return "Max Hand"; case PowerUpType.HomingDaggers: return "Homing Daggers"; case PowerUpType.AOEAttack: return "AOE Attack"; case PowerUpType.DoubleGems: return "Double Gems"; case PowerUpType.SlowTime: return "Slow Time"; case PowerUpType.GodMode: return "God Mode"; default: return "Power-Up"; } }

    // Attempts to extract an XP gain amount from a free-form message.
    // Matches messages like "+25 XP", "25 XP", "XP +25", etc. (case-insensitive) by requiring the presence of "XP" and any integer number.
    private bool TryExtractXPGain(string message, out int xpAmount)
    {
        xpAmount = 0;
        if (string.IsNullOrEmpty(message)) return false;

        string upper = message.ToUpperInvariant();
        if (!upper.Contains("XP")) return false;

        // Find the first integer in the string
        int len = message.Length;
        for (int i = 0; i < len; i++)
        {
            if (char.IsDigit(message[i]))
            {
                int start = i;
                while (i < len && char.IsDigit(message[i])) i++;
                string numberStr = message.Substring(start, i - start);
                if (int.TryParse(numberStr, out int parsed) && parsed > 0)
                {
                    xpAmount = parsed;
                    return true;
                }
            }
        }
        return false;
    }
}