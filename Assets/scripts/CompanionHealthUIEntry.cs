using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CompanionAI;

/// <summary>
/// Manual UI entry that binds a slider + text labels to a specific companion.
/// </summary>
public class CompanionHealthUIEntry : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI companionLabel;
    [SerializeField] private TextMeshProUGUI stateLabel;

    [Header("Display Settings")]
    [SerializeField] private bool hideWhenCompanionMissing = true;

    [Header("Companion Binding")]
    [Tooltip("Drag a specific CompanionCore from the scene or leave empty to match via CompanionData.")]
    [SerializeField] private CompanionCore companionReference;
    [Tooltip("Optional: assign a CompanionData profile to automatically match any spawned companion using it.")]
    [SerializeField] private CompanionData companionProfile;

    private CompanionCore _boundCompanion;
    private bool _eventsSubscribed;

    private const string DeadSuffix = " -> DEAD";

    /// <summary>
    /// Allows external systems (e.g., CompanionHealthUIManager) to assign the companion at runtime.
    /// </summary>
    public void Initialize(CompanionCore companion)
    {
        companionReference = companion;
        TryBindCompanion();
        UpdateVisibility();
        UpdateDisplay();
    }

    private void Awake()
    {
        TryBindCompanion();
        UpdateVisibility();
        UpdateDisplay();
    }

    private void OnEnable()
    {
        TryBindCompanion();
        UpdateVisibility();
        UpdateDisplay();
    }

    private void OnDisable()
    {
        DetachFromCompanion();
    }

    private void Update()
    {
        TryBindCompanion();
        UpdateVisibility();
        UpdateDisplay();
    }

    private void TryBindCompanion()
    {
        CompanionCore target = FindTargetCompanion();

        if (target == null)
        {
            DetachFromCompanion();
            return;
        }

        if (_boundCompanion == target)
        {
            return;
        }

        DetachFromCompanion();
        _boundCompanion = target;

        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
        }

        SubscribeToEvents();
    }

    private void DetachFromCompanion()
    {
        if (_boundCompanion == null)
        {
            return;
        }

        UnsubscribeFromEvents();
        _boundCompanion = null;
    }

    private void SubscribeToEvents()
    {
        if (_eventsSubscribed || _boundCompanion == null)
        {
            return;
        }

        CompanionCore.OnCompanionHealthChanged += OnCompanionHealthChanged;
        CompanionCore.OnCompanionDied += OnCompanionLifecycleChanged;
        CompanionCore.OnCompanionRemoved += OnCompanionLifecycleChanged;
        _eventsSubscribed = true;
    }

    private void UnsubscribeFromEvents()
    {
        if (!_eventsSubscribed)
        {
            return;
        }

        CompanionCore.OnCompanionHealthChanged -= OnCompanionHealthChanged;
        CompanionCore.OnCompanionDied -= OnCompanionLifecycleChanged;
        CompanionCore.OnCompanionRemoved -= OnCompanionLifecycleChanged;
        _eventsSubscribed = false;
    }

    private void OnCompanionHealthChanged(CompanionCore companion)
    {
        if (companion != _boundCompanion)
        {
            return;
        }

        UpdateDisplay();
    }

    private void OnCompanionLifecycleChanged(CompanionCore companion)
    {
        if (companionProfile != null && companion.companionProfile != companionProfile && companion != companionReference)
        {
            return;
        }

        TryBindCompanion();
        UpdateVisibility();
        UpdateDisplay();
    }

    private void UpdateVisibility()
    {
        bool shouldShow = _boundCompanion != null;

        if (!shouldShow && !hideWhenCompanionMissing && (companionReference != null || companionProfile != null))
        {
            shouldShow = true;
        }

        SetElementActive(healthSlider?.gameObject, shouldShow);
        SetElementActive(companionLabel?.gameObject, shouldShow);
        SetElementActive(stateLabel?.gameObject, shouldShow);

        if (!shouldShow)
        {
            if (healthSlider != null)
            {
                healthSlider.value = 0f;
            }

            if (companionLabel != null)
            {
                companionLabel.text = string.Empty;
            }

            if (stateLabel != null)
            {
                stateLabel.text = string.Empty;
            }
        }
    }

    private void SetElementActive(GameObject element, bool active)
    {
        if (element == null)
        {
            return;
        }

        if (element.activeSelf != active)
        {
            element.SetActive(active);
        }
    }

    private void UpdateDisplay()
    {
        if (_boundCompanion == null)
        {
            return;
        }

        if (healthSlider != null)
        {
            healthSlider.value = _boundCompanion.HealthNormalized;
        }

        if (companionLabel != null)
        {
            if (_boundCompanion.IsDead)
            {
                companionLabel.text = $"{_boundCompanion.DisplayName}{DeadSuffix}";
            }
            else
            {
                float currentHealth = Mathf.Max(0f, _boundCompanion.Health);
                float maxHealth = Mathf.Max(0.0001f, _boundCompanion.MaxHealth);
                companionLabel.text = $"{_boundCompanion.DisplayName}: {currentHealth:0}/{maxHealth:0}";
            }
        }

        if (stateLabel != null)
        {
            stateLabel.text = FormatStateText();
        }
    }

    private string FormatStateText()
    {
        if (_boundCompanion == null)
        {
            return string.Empty;
        }

        if (_boundCompanion.IsDead)
        {
            return "DEAD";
        }

        switch (_boundCompanion.CurrentState)
        {
            case CompanionCore.CompanionState.Attacking:
            case CompanionCore.CompanionState.Engaging:
                return "FIGHTING";
            case CompanionCore.CompanionState.Following:
                return "FOLLOWING";
            default:
                return _boundCompanion.CurrentState.ToString().ToUpperInvariant();
        }
    }

    private CompanionCore FindTargetCompanion()
    {
        // Direct reference has priority (works with manual setup and manager instantiation)
        if (companionReference != null && companionReference.gameObject.activeInHierarchy)
        {
            return companionReference;
        }

        if (companionProfile == null)
        {
            return null;
        }

        foreach (var activeCompanion in CompanionCore.GetActiveCompanions())
        {
            if (activeCompanion == null)
            {
                continue;
            }

            if (!activeCompanion.isActiveAndEnabled)
            {
                continue;
            }

            if (activeCompanion.companionProfile == companionProfile)
            {
                return activeCompanion;
            }
        }

        return null;
    }
}
