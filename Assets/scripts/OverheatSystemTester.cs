using UnityEngine;
using UnityEngine.UI; // If you want to quickly link a UI slider for visual feedback

public class OverheatSystemTester : MonoBehaviour
{
    public PlayerOverheatManager overheatManager; // Assign this in the Inspector

    [Header("Quick Test UI (Optional)")]
    public Slider primaryHeatSlider;
    public Slider secondaryHeatSlider;
    public Text primaryHeatText;
    public Text secondaryHeatText;

    private bool _isPrimaryStreaming = false;
    private bool _isSecondaryStreaming = false;

    void Start()
    {
        if (overheatManager == null)
        {
            overheatManager = PlayerOverheatManager.Instance;
            if (overheatManager == null)
            {
                Debug.LogError("OverheatSystemTester: PlayerOverheatManager instance not found and not assigned! Disabling tester.", this);
                enabled = false;
                return;
            }
        }
        Debug.Log("OverheatSystemTester: Initialized. Subscribing to events.");
        SubscribeToEvents();
        UpdateUITestElements(); // Initial UI update
    }

    void OnEnable()
    {
        if (overheatManager != null)
        {
            SubscribeToEvents();
        }
    }

    void OnDisable()
    {
        if (overheatManager != null)
        {
            UnsubscribeFromEvents();
        }
        // Ensure stream states are turned off if the tester is disabled
        if (_isPrimaryStreaming && overheatManager != null) overheatManager.SetHandFiringState(true, false);
        if (_isSecondaryStreaming && overheatManager != null) overheatManager.SetHandFiringState(false, false);
        _isPrimaryStreaming = false;
        _isSecondaryStreaming = false;
    }

    void SubscribeToEvents()
    {
        overheatManager.OnHeatChangedForHUD += HandleHeatChanged;
        overheatManager.OnHandFullyOverheated += HandleHandFullyOverheated;
        overheatManager.OnHandDegradedDueToOverheat += HandleHandDegraded;
        overheatManager.OnHandRecoveredFromForcedCooldown += HandleHandRecovered;
    }

    void UnsubscribeFromEvents()
    {
        overheatManager.OnHeatChangedForHUD -= HandleHeatChanged;
        overheatManager.OnHandFullyOverheated -= HandleHandFullyOverheated;
        overheatManager.OnHandDegradedDueToOverheat -= HandleHandDegraded;
        overheatManager.OnHandRecoveredFromForcedCooldown -= HandleHandRecovered;
    }

    void Update()
    {
        if (overheatManager == null) return;

        // --- Primary Hand Controls ---
        // Toggle Stream Fire (Primary)
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            _isPrimaryStreaming = !_isPrimaryStreaming;
            overheatManager.SetHandFiringState(true, _isPrimaryStreaming);
            Debug.Log($"Tester: Primary Stream toggled to: {_isPrimaryStreaming}");
        }
        // Shotgun Fire (Primary)
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            overheatManager.AddHeatToHand(true, overheatManager.shotgunHeatCost);
            Debug.Log($"Tester: Primary Shotgun fired (Heat Cost: {overheatManager.shotgunHeatCost})");
        }
        // Reduce Heat (Primary - like gem collect)
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            overheatManager.ApplyHeatReductionFromGemCollection(true);
            Debug.Log($"Tester: Primary Heat Reduced (Amount: {overheatManager.heatReductionPerGemCollected})");
        }
        // Reset Heat (Primary)
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            overheatManager.ResetHandHeat(true);
            Debug.Log("Tester: Primary Heat Reset");
        }

        // --- Secondary Hand Controls ---
        // Toggle Stream Fire (Secondary)
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            _isSecondaryStreaming = !_isSecondaryStreaming;
            overheatManager.SetHandFiringState(false, _isSecondaryStreaming);
            Debug.Log($"Tester: Secondary Stream toggled to: {_isSecondaryStreaming}");
        }
        // Shotgun Fire (Secondary)
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            overheatManager.AddHeatToHand(false, overheatManager.shotgunHeatCost);
            Debug.Log($"Tester: Secondary Shotgun fired (Heat Cost: {overheatManager.shotgunHeatCost})");
        }
        // Reduce Heat (Secondary - like gem collect)
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            overheatManager.ApplyHeatReductionFromGemCollection(false);
            Debug.Log($"Tester: Secondary Heat Reduced (Amount: {overheatManager.heatReductionPerGemCollected})");
        }
        // Reset Heat (Secondary)
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            overheatManager.ResetHandHeat(false);
            Debug.Log("Tester: Secondary Heat Reset");
        }

        // --- AOE (Uses Primary Hand Heat) ---
        if (Input.GetKeyDown(KeyCode.Q)) // Same as your AOE key
        {
            if (overheatManager.CanAffordAOE())
            {
                overheatManager.ApplyHeatForAOE();
                Debug.Log($"Tester: AOE Fired (Primary Heat Cost: {overheatManager.heatCostForAOE})");
            }
            else
            {
                Debug.Log("Tester: Tried to fire AOE, but cannot afford heat or primary hand in cooldown.");
            }
        }

        // Log current heat values
        if (Input.GetKeyDown(KeyCode.L))
        {
            Debug.Log($"Current Heat - Primary: {overheatManager.CurrentHeatPrimary:F2}/{overheatManager.maxHeat}, Secondary: {overheatManager.CurrentHeatSecondary:F2}/{overheatManager.maxHeat}");
        }
    }

    // Event Handlers
    private void HandleHeatChanged(bool isPrimary, float currentHeat, float maxHeat)
    {
        string hand = isPrimary ? "Primary" : "Secondary";
        // Debug.Log($"EVENT: OnHeatChangedForHUD - {hand} Hand: {currentHeat:F2} / {maxHeat:F2}");
        UpdateUITestElements();
    }

    private void HandleHandFullyOverheated(bool isPrimary)
    {
        string hand = isPrimary ? "Primary" : "Secondary";
        Debug.LogWarning($"EVENT: OnHandFullyOverheated - {hand} Hand is now fully overheated!");
        if (isPrimary) _isPrimaryStreaming = false; // Stream should have been stopped by manager/orchestrator
        else _isSecondaryStreaming = false;
    }

    private void HandleHandDegraded(bool isPrimary) // Only primary degrades in your current setup
    {
        if (isPrimary)
        {
            Debug.LogError("EVENT: OnHandDegradedDueToOverheat - Primary Hand has degraded!");
        }
    }

    private void HandleHandRecovered(bool isPrimary)
    {
        string hand = isPrimary ? "Primary" : "Secondary";
        Debug.Log($"EVENT: OnHandRecoveredFromForcedCooldown - {hand} Hand has recovered from forced cooldown.");
    }

    private void UpdateUITestElements()
    {
        if (overheatManager == null) return;

        if (primaryHeatSlider != null)
        {
            primaryHeatSlider.minValue = 0;
            primaryHeatSlider.maxValue = overheatManager.maxHeat;
            primaryHeatSlider.value = overheatManager.CurrentHeatPrimary;
        }
        if (primaryHeatText != null)
        {
            primaryHeatText.text = $"P: {overheatManager.CurrentHeatPrimary:F0}/{overheatManager.maxHeat:F0}";
        }

        if (secondaryHeatSlider != null)
        {
            secondaryHeatSlider.minValue = 0;
            secondaryHeatSlider.maxValue = overheatManager.maxHeat;
            secondaryHeatSlider.value = overheatManager.CurrentHeatSecondary;
        }
        if (secondaryHeatText != null)
        {
            secondaryHeatText.text = $"S: {overheatManager.CurrentHeatSecondary:F0}/{overheatManager.maxHeat:F0}";
        }
    }
}