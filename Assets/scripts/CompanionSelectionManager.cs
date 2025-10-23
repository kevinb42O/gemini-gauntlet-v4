using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class CompanionSelectionManager : MonoBehaviour
{
    [Header("Companion Data")]
    public CompanionData[] companions; // Assign 4 companions in inspector

    [Header("Companion Images")]
    public Image[] companionImages; // Assign 4 images for each companion
    public Image[] companionBoxHighlights; // Optional: Assign 4 highlight images/borders for hover effect

    [Header("Cooldown UI")]
    public Slider[] companionCooldownSliders; // 4 cooldown sliders, one for each companion
    public TextMeshProUGUI selectedCompanionCooldownText; // Cooldown text in stats panel

    [Header("Stats Panel")]
    public GameObject statsPanel;
    public Image companionImage;
    public TextMeshProUGUI companionNameText;
    public TextMeshProUGUI companionDescriptionText;
    public TextMeshProUGUI statPointsText; // Shows "X available / 140 total"
    public Button equipCompanionButton; // Button to equip companion

    [Header("Stat Sliders")]
    public Slider attackSlider;
    public Slider defenseSlider;
    public Slider speedSlider;
    public Slider accuracySlider;
    public Slider intelligenceSlider;
    public Slider loyaltySlider;
    public Slider courageSlider;
    public Slider companionLevelSlider;

    [Header("Stat Labels")]
    public TextMeshProUGUI attackLabel;
    public TextMeshProUGUI defenseLabel;
    public TextMeshProUGUI speedLabel;
    public TextMeshProUGUI accuracyLabel;
    public TextMeshProUGUI intelligenceLabel;
    public TextMeshProUGUI loyaltyLabel;
    public TextMeshProUGUI courageLabel;
    public TextMeshProUGUI companionLevelText;

    [Header("Individual Companion UI (Selection Area)")]
    public TextMeshProUGUI[] companionLevelTexts; // Level text for each companion (4 total)
    public TextMeshProUGUI[] companionCooldownTexts; // Detailed cooldown text for each companion (4 total)
    public Slider[] companionLevelSliders; // Level sliders for each companion (4 total, 1-1000 range)

    [Header("Cooldown Controls")]
    public Button[] companionResetButtons; // Reset cooldown buttons under each companion

    private CompanionData currentSelectedCompanion;
    private int selectedCompanionIndex = -1;
    private Coroutine[] _levelSliderAnimations;
    private float[] lastClickTimes; // Track last click time for double-click detection
    private const float DOUBLE_CLICK_TIME = 0.3f; // Time window for double-click (in seconds)

    private void UpdateStatLabel(TextMeshProUGUI label, string labelPrefix, int value)
    {
        if (label == null)
        {
            return;
        }

        label.text = $"{labelPrefix}: {value}";
    }

    private int ConfigureStatSlider(Slider slider, int currentValue, TextMeshProUGUI label, string labelPrefix)
    {
        if (slider == null)
        {
            return currentValue;
        }

        slider.minValue = 0;
        slider.maxValue = 20;
        slider.wholeNumbers = true;

        int clampedValue = Mathf.Clamp(currentValue, 0, 20);
        slider.value = clampedValue;

        UpdateStatLabel(label, labelPrefix, clampedValue);

        return clampedValue;
    }

    private void UpdateCompanionLevelUI(int index, bool animateSlider = false)
    {
        if (companions == null || index < 0 || index >= companions.Length)
        {
            return;
        }

        CompanionData companion = companions[index];
        if (companion == null)
        {
            return;
        }

        companion.EnsureProgressionInitialized();

        float levelProgressFraction = companion.GetLevelProgress01();
        int progressPercent = Mathf.RoundToInt(levelProgressFraction * 100f);

        if (index < companionLevelTexts.Length && companionLevelTexts[index] != null)
        {
            companionLevelTexts[index].text = $"Level: {companion.companionLevel}";
        }

        if (index < companionLevelSliders.Length && companionLevelSliders[index] != null)
        {
            Slider slider = companionLevelSliders[index];
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;

            if (animateSlider)
            {
                if (_levelSliderAnimations != null && index < _levelSliderAnimations.Length && _levelSliderAnimations[index] != null)
                {
                    StopCoroutine(_levelSliderAnimations[index]);
                }

                float startValue = slider.value;
                _levelSliderAnimations[index] = StartCoroutine(AnimateCompanionLevelSlider(index, slider, startValue, levelProgressFraction));
            }
            else
            {
                slider.value = levelProgressFraction;
            }
        }
    }

    void Start()
    {
        // Initialize UI
        if (statsPanel != null)
        {
            statsPanel.SetActive(false);
        }

        // Initialize cooldowns for all companions
        InitializeCompanionCooldowns();

        // Initialize double-click tracking
        lastClickTimes = new float[companionImages.Length];
        for (int i = 0; i < lastClickTimes.Length; i++)
        {
            lastClickTimes[i] = -1f;
        }

        // Set up EventTrigger listeners for each companion image
        for (int i = 0; i < companionImages.Length && i < companions.Length; i++)
        {
            int index = i; // Capture for closure

            // Add EventTrigger if it doesn't exist
            EventTrigger trigger = companionImages[i].GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = companionImages[i].gameObject.AddComponent<EventTrigger>();
            }

            // Clear existing triggers
            trigger.triggers.Clear();

            // Add PointerClick trigger
            EventTrigger.Entry clickEntry = new EventTrigger.Entry();
            clickEntry.eventID = EventTriggerType.PointerClick;
            clickEntry.callback.AddListener((data) => { OnCompanionClicked(index); });
            trigger.triggers.Add(clickEntry);

            // Add PointerEnter trigger (hover start)
            EventTrigger.Entry enterEntry = new EventTrigger.Entry();
            enterEntry.eventID = EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener((data) => { OnCompanionHoverEnter(index); });
            trigger.triggers.Add(enterEntry);

            // Add PointerExit trigger (hover end)
            EventTrigger.Entry exitEntry = new EventTrigger.Entry();
            exitEntry.eventID = EventTriggerType.PointerExit;
            exitEntry.callback.AddListener((data) => { OnCompanionHoverExit(index); });
            trigger.triggers.Add(exitEntry);
        }

        // Initialize all highlights to be hidden
        if (companionBoxHighlights != null)
        {
            for (int i = 0; i < companionBoxHighlights.Length; i++)
            {
                if (companionBoxHighlights[i] != null)
                {
                    companionBoxHighlights[i].enabled = false;
                }
            }
        }

        SetupResetButtons();

        // Start cooldown update coroutine
        StartCoroutine(UpdateCooldowns());

        if (equipCompanionButton != null)
        {
            equipCompanionButton.onClick.AddListener(EquipCurrentCompanion);
        }

        if (companionLevelSliders != null && companionLevelSliders.Length > 0)
        {
            _levelSliderAnimations = new Coroutine[companionLevelSliders.Length];
        }

        // Initial stat points text for default selection (if any)
        UpdateStatPointsDisplay();
    }

    private void InitializeCompanionCooldowns()
    {
        for (int i = 0; i < companions.Length; i++)
        {
            var companion = companions[i];
            if (companion == null)
            {
                continue;
            }

            companion.EnsureProgressionInitialized();

            bool hasActiveCooldown = companion.isOnCooldown && companion.currentCooldownTime > 0f;

            if (!hasActiveCooldown)
            {
                companion.currentCooldownTime = 0f;
                companion.isOnCooldown = false;
            }

            if (i < companionCooldownSliders.Length && companionCooldownSliders[i] != null)
            {
                companionCooldownSliders[i].maxValue = companion.cooldownHours * 3600f;
                companionCooldownSliders[i].value = hasActiveCooldown ? companion.currentCooldownTime : 0f;
            }

            UpdateCompanionLevelUI(i);

            if (i < companionCooldownTexts.Length && companionCooldownTexts[i] != null)
            {
                if (hasActiveCooldown)
                {
                    int totalSeconds = Mathf.CeilToInt(companion.currentCooldownTime);
                    int hours = totalSeconds / 3600;
                    int minutes = (totalSeconds % 3600) / 60;
                    int seconds = totalSeconds % 60;

                    companionCooldownTexts[i].text = $"{hours:D2}:{minutes:D2}:{seconds:D2}";
                    companionCooldownTexts[i].color = Color.red;
                }
                else
                {
                    companionCooldownTexts[i].text = "Ready!";
                    companionCooldownTexts[i].color = Color.green;
                }
            }

            UpdateResetButtonState(i);
        }
    }

    private void SetupResetButtons()
    {
        if (companionResetButtons == null)
        {
            return;
        }

        for (int i = 0; i < companionResetButtons.Length && i < companions.Length; i++)
        {
            Button button = companionResetButtons[i];
            if (button == null)
            {
                continue;
            }

            int companionIndex = i;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => ResetCompanionCooldown(companionIndex));
            UpdateResetButtonState(companionIndex);
        }
    }

    private void ResetCompanionCooldown(int companionIndex)
    {
        if (companionIndex < 0 || companionIndex >= companions.Length)
        {
            return;
        }

        CompanionData companion = companions[companionIndex];
        if (companion == null)
        {
            return;
        }

        companion.currentCooldownTime = 0f;
        companion.isOnCooldown = false;

        if (companionIndex < companionCooldownSliders.Length && companionCooldownSliders[companionIndex] != null)
        {
            companionCooldownSliders[companionIndex].value = 0f;
        }

        if (companionIndex < companionCooldownTexts.Length && companionCooldownTexts[companionIndex] != null)
        {
            companionCooldownTexts[companionIndex].text = "Ready!";
            companionCooldownTexts[companionIndex].color = Color.green;
        }

        UpdateResetButtonState(companionIndex);

        if (selectedCompanionIndex == companionIndex)
        {
            UpdateSelectedCompanionCooldownDisplay();
        }

        string companionKey = $"Companion_{companion.companionName}";
        PlayerPrefs.SetFloat($"{companionKey}_CooldownTime", 0f);
        PlayerPrefs.SetInt($"{companionKey}_IsOnCooldown", 0);
        PlayerPrefs.SetString("CooldownSaveTime", DateTime.Now.ToBinary().ToString());
        PlayerPrefs.Save();
    }

    private void UpdateResetButtonState(int companionIndex)
    {
        if (companionResetButtons == null || companionIndex < 0 || companionIndex >= companionResetButtons.Length)
        {
            return;
        }

        Button button = companionResetButtons[companionIndex];
        if (button == null)
        {
            return;
        }

        bool hasActiveCooldown = companionIndex < companions.Length
            && companions[companionIndex] != null
            && companions[companionIndex].isOnCooldown
            && companions[companionIndex].currentCooldownTime > 0f;

        button.interactable = hasActiveCooldown;
    }

    private System.Collections.IEnumerator UpdateCooldowns()
    {
        while (true)
        {
            // Update all companion cooldowns
            for (int i = 0; i < companions.Length; i++)
            {
                if (companions[i] != null && companions[i].isOnCooldown)
                {
                    companions[i].currentCooldownTime -= Time.deltaTime;

                    // Check if cooldown is finished
                    if (companions[i].currentCooldownTime <= 0f)
                    {
                        companions[i].currentCooldownTime = 0f;
                        companions[i].isOnCooldown = false;

                        // Update UI
                        if (i < companionCooldownSliders.Length && companionCooldownSliders[i] != null)
                        {
                            companionCooldownSliders[i].value = 0f;
                        }
                        if (i < companionCooldownTexts.Length && companionCooldownTexts[i] != null)
                        {
                            companionCooldownTexts[i].text = "Ready!";
                            companionCooldownTexts[i].color = Color.green;
                        }

                        UpdateResetButtonState(i);
                    }
                    else
                    {
                        // Update slider
                        if (i < companionCooldownSliders.Length && companionCooldownSliders[i] != null)
                        {
                            companionCooldownSliders[i].value = companions[i].currentCooldownTime;
                        }

                        // Update cooldown text
                        if (i < companionCooldownTexts.Length && companionCooldownTexts[i] != null)
                        {
                            int totalSeconds = Mathf.CeilToInt(companions[i].currentCooldownTime);
                            int hours = totalSeconds / 3600;
                            int minutes = (totalSeconds % 3600) / 60;
                            int seconds = totalSeconds % 60;

                            companionCooldownTexts[i].text = $"{hours:D2}:{minutes:D2}:{seconds:D2}";
                            companionCooldownTexts[i].color = Color.red;
                        }

                        UpdateCompanionLevelUI(i);
                        UpdateResetButtonState(i);
                    }
                }
            }

            // Update selected companion cooldown in stats panel
            if (selectedCompanionIndex >= 0 && selectedCompanionIndex < companions.Length)
            {
                UpdateSelectedCompanionCooldownDisplay();
            }

            yield return null; // Wait for next frame
        }
    }

    private void UpdateSelectedCompanionCooldownDisplay()
    {
        if (selectedCompanionIndex < 0 || selectedCompanionIndex >= companions.Length) return;
        if (selectedCompanionCooldownText == null) return;
        if (companions[selectedCompanionIndex] == null) return;

        if (companions[selectedCompanionIndex].isOnCooldown)
        {
            int totalSeconds = Mathf.CeilToInt(companions[selectedCompanionIndex].currentCooldownTime);
            int hours = totalSeconds / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            int seconds = totalSeconds % 60;

            selectedCompanionCooldownText.text = $"Cooldown: {hours:D2}:{minutes:D2}:{seconds:D2}";
            selectedCompanionCooldownText.color = Color.red;
        }
        else
        {
            selectedCompanionCooldownText.text = "Ready to use!";
            selectedCompanionCooldownText.color = Color.green;
        }
    }

    private void UpdateSelectedCompanionLevelDisplay()
    {
        if (currentSelectedCompanion == null)
        {
            return;
        }

        currentSelectedCompanion.EnsureProgressionInitialized();

        float levelProgressFraction = currentSelectedCompanion.GetLevelProgress01();
        float sliderValue = Mathf.Clamp(currentSelectedCompanion.companionLevel - 1 + levelProgressFraction, 0f, 1000f);
        int progressPercent = Mathf.RoundToInt(levelProgressFraction * 100f);

        if (companionLevelText != null)
        {
            companionLevelText.text = $"Level: {currentSelectedCompanion.companionLevel}";
        }
    }

    private void UpdateStatPointsDisplay()
    {
        if (statPointsText != null && StatPointsManager.Instance != null && currentSelectedCompanion != null)
        {
            int available = StatPointsManager.Instance.GetAvailablePoints(currentSelectedCompanion.companionName);
            int totalSpent = StatPointsManager.Instance.GetTotalSpentPoints(currentSelectedCompanion.companionName);
            int totalAvailable = StatPointsManager.Instance.GetTotalAvailablePoints(currentSelectedCompanion.companionName);

            statPointsText.text = $"{available} points available ({totalSpent}/{totalAvailable} spent)";
            Debug.Log($"Updated stat points display: {available} points available for {currentSelectedCompanion.companionName} ({totalSpent}/{totalAvailable} spent)");
        }
        else
        {
            if (statPointsText != null)
            {
                if (StatPointsManager.Instance == null)
                {
                    statPointsText.text = "Loading...";
                    Debug.LogWarning("StatPointsManager.Instance is null! Will retry later...");
                    // Retry after a short delay
                    Invoke("UpdateStatPointsDisplay", 0.1f);
                }
                else
                {
                    statPointsText.text = "StatPointsManager not found";
                    Debug.LogError("StatPointsManager.Instance is null!");
                }
            }
        }
    }

    private void OnCompanionClicked(int companionIndex)
    {
        float currentTime = Time.time;
        float timeSinceLastClick = currentTime - lastClickTimes[companionIndex];

        // Check for double-click
        if (timeSinceLastClick <= DOUBLE_CLICK_TIME && timeSinceLastClick > 0)
        {
            // Double-click detected - equip companion directly
            Debug.Log($"Double-click detected on companion {companionIndex}!");
            SelectCompanion(companionIndex);
            EquipCurrentCompanion();
        }
        else
        {
            // Single click - just select
            SelectCompanion(companionIndex);
        }

        // Update last click time
        lastClickTimes[companionIndex] = currentTime;
    }

    private void OnCompanionHoverEnter(int companionIndex)
    {
        // Show highlight for this companion
        if (companionBoxHighlights != null && companionIndex < companionBoxHighlights.Length)
        {
            if (companionBoxHighlights[companionIndex] != null)
            {
                companionBoxHighlights[companionIndex].enabled = true;
            }
        }
        else if (companionImages != null && companionIndex < companionImages.Length)
        {
            // Fallback: brighten the companion image if no highlight is assigned
            if (companionImages[companionIndex] != null)
            {
                companionImages[companionIndex].color = new Color(1.2f, 1.2f, 1.2f, 1f);
            }
        }
    }

    private void OnCompanionHoverExit(int companionIndex)
    {
        // Hide highlight for this companion
        if (companionBoxHighlights != null && companionIndex < companionBoxHighlights.Length)
        {
            if (companionBoxHighlights[companionIndex] != null)
            {
                companionBoxHighlights[companionIndex].enabled = false;
            }
        }
        else if (companionImages != null && companionIndex < companionImages.Length)
        {
            // Fallback: restore normal color if no highlight is assigned
            if (companionImages[companionIndex] != null)
            {
                companionImages[companionIndex].color = Color.white;
            }
        }
    }

    public void SelectCompanion(int companionIndex)
    {
        if (companionIndex < 0 || companionIndex >= companions.Length)
        {
            Debug.LogError("Invalid companion index: " + companionIndex);
            return;
        }

        selectedCompanionIndex = companionIndex;
        currentSelectedCompanion = companions[companionIndex];
        DisplayCompanionStats();
        ShowStatsPanel();

        // Force refresh level display as fallback
        Invoke("RefreshCompanionLevelDisplay", 0.1f);
    }

    private void DisplayCompanionStats()
    {
        if (currentSelectedCompanion == null)
        {
            Debug.LogError("DisplayCompanionStats called with null companion!");
            return;
        }

        Debug.Log($"DisplayCompanionStats called for: {currentSelectedCompanion.companionName}");

        // Update basic info
        if (companionImage != null && currentSelectedCompanion.companionImage != null)
        {
            companionImage.sprite = currentSelectedCompanion.companionImage;
        }

        if (companionNameText != null)
        {
            companionNameText.text = currentSelectedCompanion.companionName;
        }

        if (companionDescriptionText != null)
        {
            companionDescriptionText.text = currentSelectedCompanion.description;
        }

        Debug.Log($"Companion stats: Attack={currentSelectedCompanion.attackPower}, Defense={currentSelectedCompanion.defense}, Speed={currentSelectedCompanion.speed}, Accuracy={currentSelectedCompanion.accuracy}, Intelligence={currentSelectedCompanion.intelligence}, Loyalty={currentSelectedCompanion.loyalty}, Courage={currentSelectedCompanion.courage}");

        // Update sliders with current stats from companion data (already includes base + spent points)
        Debug.Log("🔄 Updating all sliders...");

        currentSelectedCompanion.attackPower = ConfigureStatSlider(
            attackSlider,
            currentSelectedCompanion.attackPower,
            attackLabel,
            "Attack");

        currentSelectedCompanion.defense = ConfigureStatSlider(
            defenseSlider,
            currentSelectedCompanion.defense,
            defenseLabel,
            "Defense");

        currentSelectedCompanion.speed = ConfigureStatSlider(
            speedSlider,
            currentSelectedCompanion.speed,
            speedLabel,
            "Speed");

        currentSelectedCompanion.accuracy = ConfigureStatSlider(
            accuracySlider,
            currentSelectedCompanion.accuracy,
            accuracyLabel,
            "Accuracy");

        currentSelectedCompanion.intelligence = ConfigureStatSlider(
            intelligenceSlider,
            currentSelectedCompanion.intelligence,
            intelligenceLabel,
            "Intelligence");

        currentSelectedCompanion.loyalty = ConfigureStatSlider(
            loyaltySlider,
            currentSelectedCompanion.loyalty,
            loyaltyLabel,
            "Loyalty");

        currentSelectedCompanion.courage = ConfigureStatSlider(
            courageSlider,
            currentSelectedCompanion.courage,
            courageLabel,
            "Courage");

        // Update level (1-1000 range, so we normalize to 0-1) with null check and debug logging
        UpdateSelectedCompanionLevelDisplay();

        // Update equip button text
        UpdateEquipButtonText();
    }

    private void ShowStatsPanel()
    {
        if (statsPanel != null)
        {
            statsPanel.SetActive(true);
        }
    }

    public void HideStatsPanel()
    {
        if (statsPanel != null)
        {
            statsPanel.SetActive(false);
        }
    }

    private void RefreshAllCompanionLevelDisplays()
    {
        for (int i = 0; i < companions.Length && i < companionLevelTexts.Length; i++)
        {
            UpdateCompanionLevelUI(i, true);
            if (companionLevelSliders != null && i < companionLevelSliders.Length && companionLevelSliders[i] != null)
            {
                Debug.Log($"[CompanionSelectionManager] Slider[{i}] value=" + companionLevelSliders[i].value);
            }
        }
    }

    public void RefreshLevelUIOnMenuOpened()
    {
        Debug.Log($"[CompanionSelectionManager] RefreshLevelUIOnMenuOpened called at {Time.time:F2}s");
        RefreshAllCompanionLevelDisplays();

        if (statsPanel != null && statsPanel.activeSelf && selectedCompanionIndex >= 0)
        {
            Debug.Log($"[CompanionSelectionManager] Updating selected companion index {selectedCompanionIndex} level label");
            UpdateSelectedCompanionLevelDisplay();
        }
    }

    public void OnCompanionMenuOpened()
    {
        RefreshLevelUIOnMenuOpened();
    }

    private IEnumerator AnimateCompanionLevelSlider(int index, Slider slider, float startValue, float targetValue)
    {
        float distance = Mathf.Abs(targetValue - startValue);
        float duration = Mathf.Clamp(distance * 0.5f, 0.2f, 1.2f);
        float elapsed = 0f;

        while (elapsed < duration && slider != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            slider.value = Mathf.Lerp(startValue, targetValue, t);
            yield return null;
        }

        if (slider != null)
        {
            slider.value = targetValue;
        }

        if (_levelSliderAnimations != null && index < _levelSliderAnimations.Length)
        {
            _levelSliderAnimations[index] = null;
        }
    }

    public CompanionData GetSelectedCompanion()
    {
        return currentSelectedCompanion;
    }

    public void ResetAllCompanionStats()
    {
        if (StatPointsManager.Instance != null)
        {
            StatPointsManager.Instance.ResetAllCompanionStats();

            // Refresh the current companion display
            if (currentSelectedCompanion != null)
            {
                DisplayCompanionStats();
            }

            Debug.Log("All companion stats reset! Fresh start with clean values.");
        }
    }

    // Public methods to manually trigger cooldowns (useful for testing)
    public void StartCooldownForCompanion(int companionIndex)
    {
        if (companionIndex >= 0 && companionIndex < companions.Length && companions[companionIndex] != null)
        {
            companions[companionIndex].currentCooldownTime = companions[companionIndex].cooldownHours * 3600f;
            companions[companionIndex].isOnCooldown = true;

            // Update UI
            if (companionIndex < companionCooldownSliders.Length && companionCooldownSliders[companionIndex] != null)
            {
                companionCooldownSliders[companionIndex].value = companions[companionIndex].currentCooldownTime;
            }
            if (companionIndex < companionCooldownTexts.Length && companionCooldownTexts[companionIndex] != null)
            {
                int totalSeconds = Mathf.CeilToInt(companions[companionIndex].currentCooldownTime);
                int hours = totalSeconds / 3600;
                int minutes = (totalSeconds % 3600) / 60;
                int seconds = totalSeconds % 60;

                companionCooldownTexts[companionIndex].text = $"{hours:D2}:{minutes:D2}:{seconds:D2}";
                companionCooldownTexts[companionIndex].color = Color.red;
            }
            UpdateCompanionLevelUI(companionIndex);

            UpdateResetButtonState(companionIndex);
        }
    }

    public void OnAttackSliderChanged()
    {
        Debug.Log("🎛️ Attack slider changed! Checking if it works...");
        if (currentSelectedCompanion == null || StatPointsManager.Instance == null)
        {
            Debug.LogError("❌ Attack slider: currentSelectedCompanion or StatPointsManager is null!");
            return;
        }

        // Calculate the exact stat value based on slider position (DIRECT 1-20 VALUES)
        int newValue = Mathf.RoundToInt(attackSlider.value);
        int currentTotal = currentSelectedCompanion.attackPower;

        Debug.Log($"🎛️ Attack slider: currentTotal={currentTotal}, newValue={newValue}, sliderValue={attackSlider.value}");

        if (newValue > currentTotal)
        {
            // Calculate exact points to spend (should be exactly 1 per slider step)
            int pointsToSpend = newValue - currentTotal;
            Debug.Log($"💰 Attack slider: Spending {pointsToSpend} points to increase Attack from {currentTotal} to {newValue}");

            bool canSpend = StatPointsManager.Instance.CanSpendPoints(currentSelectedCompanion.companionName, pointsToSpend);
            if (canSpend)
            {
                // Spend points one by one
                for (int i = 0; i < pointsToSpend; i++)
                {
                    StatPointsManager.Instance.SpendPoint(currentSelectedCompanion.companionName, "attackpower");
                }

                // Update companion data
                currentSelectedCompanion.attackPower = newValue;
                UpdateStatLabel(attackLabel, "Attack", currentSelectedCompanion.attackPower);
                Debug.Log($"✅ Attack slider: SUCCESS! Attack increased to {newValue}, spent {pointsToSpend} points");
            }
            else
            {
                // Not enough points, revert slider to exact current value
                attackSlider.value = currentTotal;
                UpdateStatLabel(attackLabel, "Attack", currentTotal);
                Debug.Log("❌ Attack slider: Not enough points, reverting slider");
            }
        }
        else if (newValue < currentTotal)
        {
            // Calculate exact points to refund (should be exactly 1 per slider step)
            int pointsToRefund = currentTotal - newValue;
            Debug.Log($"💰 Attack slider: Refunding {pointsToRefund} points to decrease Attack from {currentTotal} to {newValue}");

            // Refund points one by one
            for (int i = 0; i < pointsToRefund; i++)
            {
                StatPointsManager.Instance.RefundPoint(currentSelectedCompanion.companionName, "attackpower");
            }

            // Update companion data
            currentSelectedCompanion.attackPower = newValue;
            UpdateStatLabel(attackLabel, "Attack", currentSelectedCompanion.attackPower);
            Debug.Log($"✅ Attack slider: SUCCESS! Attack decreased to {newValue}, refunded {pointsToRefund} points");
        }
        else
        {
            Debug.Log("ℹ️ Attack slider: No change in value");
            UpdateStatLabel(attackLabel, "Attack", currentSelectedCompanion.attackPower);
        }

        UpdateStatPointsDisplay();
    }

    public void OnDefenseSliderChanged()
    {
        Debug.Log("🎛️ Defense slider changed! Checking if it works...");
        if (currentSelectedCompanion == null || StatPointsManager.Instance == null)
        {
            Debug.LogError("❌ Defense slider: currentSelectedCompanion or StatPointsManager is null!");
            return;
        }

        int newValue = Mathf.RoundToInt(defenseSlider.value);
        int currentTotal = currentSelectedCompanion.defense;

        Debug.Log($"🎛️ Defense slider: currentTotal={currentTotal}, newValue={newValue}, sliderValue={defenseSlider.value}");

        if (newValue > currentTotal)
        {
            int pointsToSpend = newValue - currentTotal;
            Debug.Log($"💰 Defense slider: Spending {pointsToSpend} points to increase Defense from {currentTotal} to {newValue}");

            bool canSpend = StatPointsManager.Instance.CanSpendPoints(currentSelectedCompanion.companionName, pointsToSpend);
            if (canSpend)
            {
                for (int i = 0; i < pointsToSpend; i++)
                {
                    StatPointsManager.Instance.SpendPoint(currentSelectedCompanion.companionName, "defense");
                }

                currentSelectedCompanion.defense = newValue;
                UpdateStatLabel(defenseLabel, "Defense", currentSelectedCompanion.defense);
                Debug.Log($"✅ Defense slider: SUCCESS! Defense increased to {newValue}, spent {pointsToSpend} points");
            }
            else
            {
                defenseSlider.value = currentTotal;
                UpdateStatLabel(defenseLabel, "Defense", currentTotal);
                Debug.Log("❌ Defense slider: Not enough points, reverting slider");
            }
        }
        else if (newValue < currentTotal)
        {
            int pointsToRefund = currentTotal - newValue;
            Debug.Log($"💰 Defense slider: Refunding {pointsToRefund} points to decrease Defense from {currentTotal} to {newValue}");

            for (int i = 0; i < pointsToRefund; i++)
            {
                StatPointsManager.Instance.RefundPoint(currentSelectedCompanion.companionName, "defense");
            }

            currentSelectedCompanion.defense = newValue;
            UpdateStatLabel(defenseLabel, "Defense", currentSelectedCompanion.defense);
            Debug.Log($"✅ Defense slider: SUCCESS! Defense decreased to {newValue}, refunded {pointsToRefund} points");
        }
        else
        {
            Debug.Log("ℹ️ Defense slider: No change in value");
            UpdateStatLabel(defenseLabel, "Defense", currentSelectedCompanion.defense);
        }

        UpdateStatPointsDisplay();
    }

    public void OnSpeedSliderChanged()
    {
        Debug.Log("🎛️ Speed slider changed! Checking if it works...");
        if (currentSelectedCompanion == null || StatPointsManager.Instance == null)
        {
            Debug.LogError("❌ Speed slider: currentSelectedCompanion or StatPointsManager is null!");
            return;
        }

        int newValue = Mathf.RoundToInt(speedSlider.value);
        int currentTotal = currentSelectedCompanion.speed;

        Debug.Log($"🎛️ Speed slider: currentTotal={currentTotal}, newValue={newValue}, sliderValue={speedSlider.value}");

        if (newValue > currentTotal)
        {
            int pointsToSpend = newValue - currentTotal;
            Debug.Log($"💰 Speed slider: Spending {pointsToSpend} points to increase Speed from {currentTotal} to {newValue}");

            bool canSpend = StatPointsManager.Instance.CanSpendPoints(currentSelectedCompanion.companionName, pointsToSpend);
            if (canSpend)
            {
                for (int i = 0; i < pointsToSpend; i++)
                {
                    StatPointsManager.Instance.SpendPoint(currentSelectedCompanion.companionName, "speed");
                }

                currentSelectedCompanion.speed = newValue;
                UpdateStatLabel(speedLabel, "Speed", currentSelectedCompanion.speed);
                Debug.Log($"✅ Speed slider: SUCCESS! Speed increased to {newValue}, spent {pointsToSpend} points");
            }
            else
            {
                speedSlider.value = currentTotal;
                UpdateStatLabel(speedLabel, "Speed", currentTotal);
                Debug.Log("❌ Speed slider: Not enough points, reverting slider");
            }
        }
        else if (newValue < currentTotal)
        {
            int pointsToRefund = currentTotal - newValue;
            Debug.Log($"💰 Speed slider: Refunding {pointsToRefund} points to decrease Speed from {currentTotal} to {newValue}");

            for (int i = 0; i < pointsToRefund; i++)
            {
                StatPointsManager.Instance.RefundPoint(currentSelectedCompanion.companionName, "speed");
            }

            currentSelectedCompanion.speed = newValue;
            UpdateStatLabel(speedLabel, "Speed", currentSelectedCompanion.speed);
            Debug.Log($"✅ Speed slider: SUCCESS! Speed decreased to {newValue}, refunded {pointsToRefund} points");
        }
        else
        {
            Debug.Log("ℹ️ Speed slider: No change in value");
            UpdateStatLabel(speedLabel, "Speed", currentSelectedCompanion.speed);
        }

        UpdateStatPointsDisplay();
    }

    public void OnAccuracySliderChanged()
    {
        Debug.Log("🎛️ Accuracy slider changed! Checking if it works...");
        if (currentSelectedCompanion == null || StatPointsManager.Instance == null)
        {
            Debug.LogError("❌ Accuracy slider: currentSelectedCompanion or StatPointsManager is null!");
            return;
        }

        int newValue = Mathf.RoundToInt(accuracySlider.value);
        int currentTotal = currentSelectedCompanion.accuracy;

        Debug.Log($"🎛️ Accuracy slider: currentTotal={currentTotal}, newValue={newValue}, sliderValue={accuracySlider.value}");

        if (newValue > currentTotal)
        {
            int pointsToSpend = newValue - currentTotal;
            Debug.Log($"💰 Accuracy slider: Spending {pointsToSpend} points to increase Accuracy from {currentTotal} to {newValue}");

            bool canSpend = StatPointsManager.Instance.CanSpendPoints(currentSelectedCompanion.companionName, pointsToSpend);
            if (canSpend)
            {
                for (int i = 0; i < pointsToSpend; i++)
                {
                    StatPointsManager.Instance.SpendPoint(currentSelectedCompanion.companionName, "accuracy");
                }

                currentSelectedCompanion.accuracy = newValue;
                UpdateStatLabel(accuracyLabel, "Accuracy", currentSelectedCompanion.accuracy);
                Debug.Log($"✅ Accuracy slider: SUCCESS! Accuracy increased to {newValue}, spent {pointsToSpend} points");
            }
            else
            {
                accuracySlider.value = currentTotal;
                UpdateStatLabel(accuracyLabel, "Accuracy", currentTotal);
                Debug.Log("❌ Accuracy slider: Not enough points, reverting slider");
            }
        }
        else if (newValue < currentTotal)
        {
            int pointsToRefund = currentTotal - newValue;
            Debug.Log($"💰 Accuracy slider: Refunding {pointsToRefund} points to decrease Accuracy from {currentTotal} to {newValue}");

            for (int i = 0; i < pointsToRefund; i++)
            {
                StatPointsManager.Instance.RefundPoint(currentSelectedCompanion.companionName, "accuracy");
            }

            currentSelectedCompanion.accuracy = newValue;
            UpdateStatLabel(accuracyLabel, "Accuracy", currentSelectedCompanion.accuracy);
            Debug.Log($"✅ Accuracy slider: SUCCESS! Accuracy decreased to {newValue}, refunded {pointsToRefund} points");
        }
        else
        {
            Debug.Log("ℹ️ Accuracy slider: No change in value");
            UpdateStatLabel(accuracyLabel, "Accuracy", currentSelectedCompanion.accuracy);
        }

        UpdateStatPointsDisplay();
    }

    public void OnIntelligenceSliderChanged()
    {
        Debug.Log("🎛️ Intelligence slider changed! Checking if it works...");
        if (currentSelectedCompanion == null || StatPointsManager.Instance == null)
        {
            Debug.LogError("❌ Intelligence slider: currentSelectedCompanion or StatPointsManager is null!");
            return;
        }

        int newValue = Mathf.RoundToInt(intelligenceSlider.value);
        int currentTotal = currentSelectedCompanion.intelligence;

        Debug.Log($"🎛️ Intelligence slider: currentTotal={currentTotal}, newValue={newValue}, sliderValue={intelligenceSlider.value}");

        if (newValue > currentTotal)
        {
            int pointsToSpend = newValue - currentTotal;
            Debug.Log($"💰 Intelligence slider: Spending {pointsToSpend} points to increase Intelligence from {currentTotal} to {newValue}");

            bool canSpend = StatPointsManager.Instance.CanSpendPoints(currentSelectedCompanion.companionName, pointsToSpend);
            if (canSpend)
            {
                for (int i = 0; i < pointsToSpend; i++)
                {
                    StatPointsManager.Instance.SpendPoint(currentSelectedCompanion.companionName, "intelligence");
                }

                currentSelectedCompanion.intelligence = newValue;
                UpdateStatLabel(intelligenceLabel, "Intelligence", currentSelectedCompanion.intelligence);
                Debug.Log($"✅ Intelligence slider: SUCCESS! Intelligence increased to {newValue}, spent {pointsToSpend} points");
            }
            else
            {
                intelligenceSlider.value = currentTotal;
                UpdateStatLabel(intelligenceLabel, "Intelligence", currentTotal);
                Debug.Log("❌ Intelligence slider: Not enough points, reverting slider");
            }
        }
        else if (newValue < currentTotal)
        {
            int pointsToRefund = currentTotal - newValue;
            Debug.Log($"💰 Intelligence slider: Refunding {pointsToRefund} points to decrease Intelligence from {currentTotal} to {newValue}");

            for (int i = 0; i < pointsToRefund; i++)
            {
                StatPointsManager.Instance.RefundPoint(currentSelectedCompanion.companionName, "intelligence");
            }

            currentSelectedCompanion.intelligence = newValue;
            UpdateStatLabel(intelligenceLabel, "Intelligence", currentSelectedCompanion.intelligence);
            Debug.Log($"✅ Intelligence slider: SUCCESS! Intelligence decreased to {newValue}, refunded {pointsToRefund} points");
        }
        else
        {
            Debug.Log("ℹ️ Intelligence slider: No change in value");
            UpdateStatLabel(intelligenceLabel, "Intelligence", currentSelectedCompanion.intelligence);
        }

        UpdateStatPointsDisplay();
    }

    public void OnLoyaltySliderChanged()
    {
        Debug.Log("🎛️ Loyalty slider changed! Checking if it works...");
        if (currentSelectedCompanion == null || StatPointsManager.Instance == null)
        {
            Debug.LogError("❌ Loyalty slider: currentSelectedCompanion or StatPointsManager is null!");
            return;
        }

        int newValue = Mathf.RoundToInt(loyaltySlider.value);
        int currentTotal = currentSelectedCompanion.loyalty;

        Debug.Log($"🎛️ Loyalty slider: currentTotal={currentTotal}, newValue={newValue}, sliderValue={loyaltySlider.value}");

        if (newValue > currentTotal)
        {
            int pointsToSpend = newValue - currentTotal;
            Debug.Log($"💰 Loyalty slider: Spending {pointsToSpend} points to increase Loyalty from {currentTotal} to {newValue}");

            bool canSpend = StatPointsManager.Instance.CanSpendPoints(currentSelectedCompanion.companionName, pointsToSpend);
            if (canSpend)
            {
                for (int i = 0; i < pointsToSpend; i++)
                {
                    StatPointsManager.Instance.SpendPoint(currentSelectedCompanion.companionName, "loyalty");
                }

                currentSelectedCompanion.loyalty = newValue;
                UpdateStatLabel(loyaltyLabel, "Loyalty", currentSelectedCompanion.loyalty);
                Debug.Log($"✅ Loyalty slider: SUCCESS! Loyalty increased to {newValue}, spent {pointsToSpend} points");
            }
            else
            {
                loyaltySlider.value = currentTotal;
                UpdateStatLabel(loyaltyLabel, "Loyalty", currentTotal);
                Debug.Log("❌ Loyalty slider: Not enough points, reverting slider");
            }
        }
        else if (newValue < currentTotal)
        {
            int pointsToRefund = currentTotal - newValue;
            Debug.Log($"💰 Loyalty slider: Refunding {pointsToRefund} points to decrease Loyalty from {currentTotal} to {newValue}");

            for (int i = 0; i < pointsToRefund; i++)
            {
                StatPointsManager.Instance.RefundPoint(currentSelectedCompanion.companionName, "loyalty");
            }

            currentSelectedCompanion.loyalty = newValue;
            UpdateStatLabel(loyaltyLabel, "Loyalty", currentSelectedCompanion.loyalty);
            Debug.Log($"✅ Loyalty slider: SUCCESS! Loyalty decreased to {newValue}, refunded {pointsToRefund} points");
        }
        else
        {
            Debug.Log("ℹ️ Loyalty slider: No change in value");
            UpdateStatLabel(loyaltyLabel, "Loyalty", currentSelectedCompanion.loyalty);
        }

        UpdateStatPointsDisplay();
    }

    public void OnCourageSliderChanged()
    {
        Debug.Log("🎛️ Courage slider changed! Checking if it works...");
        if (currentSelectedCompanion == null || StatPointsManager.Instance == null)
        {
            Debug.LogError("❌ Courage slider: currentSelectedCompanion or StatPointsManager is null!");
            return;
        }

        int newValue = Mathf.RoundToInt(courageSlider.value);
        int currentTotal = currentSelectedCompanion.courage;

        Debug.Log($"🎛️ Courage slider: currentTotal={currentTotal}, newValue={newValue}, sliderValue={courageSlider.value}");

        if (newValue > currentTotal)
        {
            int pointsToSpend = newValue - currentTotal;
            Debug.Log($"💰 Courage slider: Spending {pointsToSpend} points to increase Courage from {currentTotal} to {newValue}");

            bool canSpend = StatPointsManager.Instance.CanSpendPoints(currentSelectedCompanion.companionName, pointsToSpend);
            if (canSpend)
            {
                for (int i = 0; i < pointsToSpend; i++)
                {
                    StatPointsManager.Instance.SpendPoint(currentSelectedCompanion.companionName, "courage");
                }

                currentSelectedCompanion.courage = newValue;
                UpdateStatLabel(courageLabel, "Courage", currentSelectedCompanion.courage);
                Debug.Log($"✅ Courage slider: SUCCESS! Courage increased to {newValue}, spent {pointsToSpend} points");
            }
            else
            {
                courageSlider.value = currentTotal;
                UpdateStatLabel(courageLabel, "Courage", currentTotal);
                Debug.Log("❌ Courage slider: Not enough points, reverting slider");
            }
        }
        else if (newValue < currentTotal)
        {
            int pointsToRefund = currentTotal - newValue;
            Debug.Log($"💰 Courage slider: Refunding {pointsToRefund} points to decrease Courage from {currentTotal} to {newValue}");

            for (int i = 0; i < pointsToRefund; i++)
            {
                StatPointsManager.Instance.RefundPoint(currentSelectedCompanion.companionName, "courage");
            }

            currentSelectedCompanion.courage = newValue;
            UpdateStatLabel(courageLabel, "Courage", currentSelectedCompanion.courage);
            Debug.Log($"✅ Courage slider: SUCCESS! Courage decreased to {newValue}, refunded {pointsToRefund} points");
        }
        else
        {
            Debug.Log("ℹ️ Courage slider: No change in value");
            UpdateStatLabel(courageLabel, "Courage", currentSelectedCompanion.courage);
        }

        UpdateStatPointsDisplay();
    }

    private void RefreshCompanionLevelDisplay()
    {
        if (currentSelectedCompanion == null)
        {
            Debug.LogWarning("RefreshCompanionLevelDisplay: currentSelectedCompanion is null");
            return;
        }

        UpdateSelectedCompanionLevelDisplay();

        if (selectedCompanionIndex >= 0 && selectedCompanionIndex < companions.Length)
        {
            UpdateCompanionLevelUI(selectedCompanionIndex);
        }
    }

    private void UpdateEquipButtonText()
    {
        if (equipCompanionButton == null || currentSelectedCompanion == null) return;

        if (PersistentCompanionSelectionManager.Instance != null)
        {
            bool isEquipped = PersistentCompanionSelectionManager.Instance.IsCompanionEquipped(currentSelectedCompanion);
            bool slotsAvailable = PersistentCompanionSelectionManager.Instance.GetEquippedCount() < 4;
            bool isOnCooldown = currentSelectedCompanion.isOnCooldown;

            if (isOnCooldown)
            {
                equipCompanionButton.GetComponentInChildren<TextMeshProUGUI>().text = "On Cooldown";
                equipCompanionButton.interactable = false;
            }
            else if (isEquipped)
            {
                equipCompanionButton.GetComponentInChildren<TextMeshProUGUI>().text = "Equipped";
                equipCompanionButton.interactable = false;
            }
            else if (slotsAvailable)
            {
                equipCompanionButton.GetComponentInChildren<TextMeshProUGUI>().text = "Equip";
                equipCompanionButton.interactable = true;
            }
            else
            {
                equipCompanionButton.GetComponentInChildren<TextMeshProUGUI>().text = "Slots Full";
                equipCompanionButton.interactable = false;
            }
        }
    }

    public void EquipCurrentCompanion()
    {
        if (currentSelectedCompanion == null)
        {
            Debug.LogWarning("No companion selected to equip!");
            return;
        }

        if (PersistentCompanionSelectionManager.Instance == null)
        {
            Debug.LogError("PersistentCompanionSelectionManager not found!");
            return;
        }

        bool success = PersistentCompanionSelectionManager.Instance.EquipCompanion(currentSelectedCompanion);
        if (success)
        {
            Debug.Log($"Successfully equipped {currentSelectedCompanion.companionName}!");
            UpdateEquipButtonText(); // Update button after equipping
        }
        else
        {
            Debug.Log($"Failed to equip {currentSelectedCompanion.companionName} - may already be equipped or slots full");
        }
    }
}
