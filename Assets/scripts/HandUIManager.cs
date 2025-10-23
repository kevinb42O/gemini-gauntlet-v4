// --- HandUIManager.cs (Dynamic Hand Level UI System) ---
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Amazing Hand UI Manager that displays both hands' levels, progress, and gem collection
/// Features dynamic text fields, progress bars, and visual effects
/// </summary>
public class HandUIManager : MonoBehaviour
{
    [Header("UI References - Left Hand (Primary - LMB)")]
    [Tooltip("LEFT SIDE UI PANEL - Level display text")]
    public TextMeshProUGUI leftHandLevelText;
    [Tooltip("LEFT SIDE UI PANEL - Gems collected text")]
    public TextMeshProUGUI leftHandGemsText;
    [Tooltip("LEFT SIDE UI PANEL - Progress text")]
    public TextMeshProUGUI leftHandProgressText;
    [Tooltip("LEFT SIDE UI PANEL - Progress bar")]
    public Slider leftHandProgressBar;
    [Tooltip("LEFT SIDE UI PANEL - Heat percentage text")]
    public TextMeshProUGUI leftHandHeatText;
    [Tooltip("LEFT SIDE UI PANEL - Heat bar")]
    public Slider leftHandHeatBar;
    [Tooltip("LEFT SIDE UI PANEL - Container for animations")]
    public RectTransform leftHandContainer;
    
    [Header("UI References - Right Hand (Secondary - RMB)")]
    [Tooltip("RIGHT SIDE UI PANEL - Level display text")]
    public TextMeshProUGUI rightHandLevelText;
    [Tooltip("RIGHT SIDE UI PANEL - Gems collected text")]
    public TextMeshProUGUI rightHandGemsText;
    [Tooltip("RIGHT SIDE UI PANEL - Progress text")]
    public TextMeshProUGUI rightHandProgressText;
    [Tooltip("RIGHT SIDE UI PANEL - Progress bar")]
    public Slider rightHandProgressBar;
    [Tooltip("RIGHT SIDE UI PANEL - Heat percentage text")]
    public TextMeshProUGUI rightHandHeatText;
    [Tooltip("RIGHT SIDE UI PANEL - Heat bar")]
    public Slider rightHandHeatBar;
    [Tooltip("RIGHT SIDE UI PANEL - Container for animations")]
    public RectTransform rightHandContainer;
    
    [Header("Visual Effects")]
    [Tooltip("Level up effect prefab")]
    public GameObject levelUpEffectPrefab;
    [Tooltip("Gem collection effect prefab")]
    public GameObject gemCollectionEffectPrefab;
    [Tooltip("Overheat warning effect prefab")]
    public GameObject overheatWarningEffectPrefab;
    [Tooltip("Progress bar fill colors")]
    public Gradient progressBarGradient;
    [Tooltip("Heat bar fill colors (cool to hot)")]
    public Gradient heatBarGradient;
    [Tooltip("Level up glow duration")]
    public float levelUpGlowDuration = 2f;
    [Tooltip("Gem collection pulse duration")]
    public float gemPulseDuration = 0.5f;
    [Tooltip("Heat warning pulse duration")]
    public float heatWarningPulseDuration = 1f;
    
    [Header("Player Heat Particle Effects")]
    [Tooltip("Left hand heat warning particle effect (child of player/camera)")]
    public ParticleSystem leftHandHeatWarningParticles;
    [Tooltip("Left hand overheat particle effect (child of player/camera)")]
    public ParticleSystem leftHandOverheatParticles;
    [Tooltip("Right hand heat warning particle effect (child of player/camera)")]
    public ParticleSystem rightHandHeatWarningParticles;
    [Tooltip("Right hand overheat particle effect (child of player/camera)")]
    public ParticleSystem rightHandOverheatParticles;
    
    [Header("Animation Settings")]
    [Tooltip("Scale animation intensity")]
    public float scaleAnimationIntensity = 1.2f;
    [Tooltip("Animation duration")]
    public float animationDuration = 0.3f;
    [Tooltip("Text color flash on level up")]
    public Color levelUpFlashColor = Color.yellow;
    [Tooltip("Text color flash on gem collection")]
    public Color gemCollectionFlashColor = Color.cyan;
    [Tooltip("Text color flash on heat warning")]
    public Color heatWarningFlashColor = Color.red;
    [Tooltip("Text color for overheated state")]
    public Color overheatedTextColor = Color.red;
    
    // Singleton instance
    public static HandUIManager Instance { get; private set; }
    
    // Current hand data
    private int _leftHandLevel = 1;
    private int _leftHandGems = 0;
    private int _leftHandGemsNeeded = 5;
    private float _leftHandHeat = 0f;
    private float _leftHandMaxHeat = 100f;
    private bool _leftHandOverheated = false;
    private int _rightHandLevel = 1;
    private int _rightHandGems = 0;
    private int _rightHandGemsNeeded = 5;
    private float _rightHandHeat = 0f;
    private float _rightHandMaxHeat = 100f;
    private bool _rightHandOverheated = false;
    
    // Original colors for restoration
    private Color _originalLeftLevelColor;
    private Color _originalRightLevelColor;
    private Color _originalLeftGemsColor;
    private Color _originalRightGemsColor;
    private Color _originalLeftHeatColor;
    private Color _originalRightHeatColor;
    
    // Original scales for restoration
    private Vector3 _originalLeftHeatTextScale;
    private Vector3 _originalRightHeatTextScale;
    
    // Animation coroutines
    private Coroutine _leftHandAnimCoroutine;
    private Coroutine _rightHandAnimCoroutine;
    private Coroutine _heatSyncCoroutine;
    
    // Particle effect state tracking
    private bool _leftHandWarningParticlesActive = false;
    private bool _leftHandOverheatParticlesActive = false;
    private bool _rightHandWarningParticlesActive = false;
    private bool _rightHandOverheatParticlesActive = false;
    
    // Heat sync settings
    private const float HEAT_SYNC_INTERVAL = 0.1f; // Sync every 100ms for responsiveness
    
    void Awake()
    {
        // Singleton pattern - BUT DON'T USE DontDestroyOnLoad for UI components
        // UI components should be recreated with each scene to avoid null reference issues
        if (Instance == null)
        {
            Instance = this;
            // REMOVED: DontDestroyOnLoad(gameObject); - This causes issues with UI elements being destroyed
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Store original colors
        StoreOriginalColors();
    }
    
    void Start()
    {
        // Subscribe to PlayerProgression events
        SubscribeToEvents();
        
        // Initial UI update
        RefreshAllHandUI();
        
        // Start heat sync coroutine for robust state management
        StartHeatSyncCoroutine();
    }
    
    void OnDestroy()
    {
        // Stop heat sync coroutine
        StopHeatSyncCoroutine();
        
        // Unsubscribe from events
        UnsubscribeFromEvents();
        
        // Clear singleton reference when this instance is destroyed
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    private void StoreOriginalColors()
    {
        if (leftHandLevelText != null) _originalLeftLevelColor = leftHandLevelText.color;
        if (rightHandLevelText != null) _originalRightLevelColor = rightHandLevelText.color;
        if (leftHandGemsText != null) _originalLeftGemsColor = leftHandGemsText.color;
        if (rightHandGemsText != null) _originalRightGemsColor = rightHandGemsText.color;
        if (leftHandHeatText != null) 
        {
            _originalLeftHeatColor = leftHandHeatText.color;
            _originalLeftHeatTextScale = leftHandHeatText.transform.localScale;
        }
        if (rightHandHeatText != null) 
        {
            _originalRightHeatColor = rightHandHeatText.color;
            _originalRightHeatTextScale = rightHandHeatText.transform.localScale;
        }
    }
    
    private void SubscribeToEvents()
    {
        if (PlayerProgression.Instance != null)
        {
            // LMB=Primary=LEFT physical hand → LEFT side UI
            PlayerProgression.OnPrimaryHandLevelChangedForHUD += OnLeftHandLevelChanged;
            PlayerProgression.OnPrimaryHandGemsChangedForHUD += OnLeftHandGemsChanged;
            PlayerProgression.OnSecondaryHandLevelChangedForHUD += OnRightHandLevelChanged;
            PlayerProgression.OnSecondaryHandGemsChangedForHUD += OnRightHandGemsChanged;
        }
        
        // Subscribe to heat management events
        if (PlayerOverheatManager.Instance != null)
        {
            PlayerOverheatManager.Instance.OnHeatChangedForHUD += OnHandHeatChanged;
            PlayerOverheatManager.Instance.OnHandFullyOverheated += OnHandOverheated;
            PlayerOverheatManager.Instance.OnHandRecoveredFromForcedCooldown += OnHandRecoveredFromOverheat;
        }
    }
    
    private void UnsubscribeFromEvents()
    {
        if (PlayerProgression.Instance != null)
        {
            // LMB=Primary=LEFT physical hand → LEFT side UI
            PlayerProgression.OnPrimaryHandLevelChangedForHUD -= OnLeftHandLevelChanged;
            PlayerProgression.OnPrimaryHandGemsChangedForHUD -= OnLeftHandGemsChanged;
            PlayerProgression.OnSecondaryHandLevelChangedForHUD -= OnRightHandLevelChanged;
            PlayerProgression.OnSecondaryHandGemsChangedForHUD -= OnRightHandGemsChanged;
        }
        
        // Unsubscribe from heat management events
        if (PlayerOverheatManager.Instance != null)
        {
            PlayerOverheatManager.Instance.OnHeatChangedForHUD -= OnHandHeatChanged;
            PlayerOverheatManager.Instance.OnHandFullyOverheated -= OnHandOverheated;
            PlayerOverheatManager.Instance.OnHandRecoveredFromForcedCooldown -= OnHandRecoveredFromOverheat;
        }
    }
    
    private void SetupProgressBarGradients()
    {
        // Setup gem progress bar gradients
        if (leftHandProgressBar != null && leftHandProgressBar.fillRect != null)
        {
            Image fillImage = leftHandProgressBar.fillRect.GetComponent<Image>();
            if (fillImage != null && progressBarGradient != null)
            {
                fillImage.color = progressBarGradient.Evaluate(0f);
            }
        }
        
        if (rightHandProgressBar != null && rightHandProgressBar.fillRect != null)
        {
            Image fillImage = rightHandProgressBar.fillRect.GetComponent<Image>();
            if (fillImage != null && progressBarGradient != null)
            {
                fillImage.color = progressBarGradient.Evaluate(0f);
            }
        }
        
        // Setup heat bar gradients
        if (leftHandHeatBar != null && leftHandHeatBar.fillRect != null)
        {
            Image fillImage = leftHandHeatBar.fillRect.GetComponent<Image>();
            if (fillImage != null && heatBarGradient != null)
            {
                fillImage.color = heatBarGradient.Evaluate(0f);
            }
        }
        
        if (rightHandHeatBar != null && rightHandHeatBar.fillRect != null)
        {
            Image fillImage = rightHandHeatBar.fillRect.GetComponent<Image>();
            if (fillImage != null && heatBarGradient != null)
            {
                fillImage.color = heatBarGradient.Evaluate(0f);
            }
        }
    }
    
    // Event handlers
    private void OnLeftHandLevelChanged(int newLevel)
    {
        bool leveledUp = newLevel > _leftHandLevel;
        _leftHandLevel = newLevel;
        
        UpdateLeftHandUI();
        
        if (leveledUp)
        {
            TriggerLevelUpEffect(false); // false = left hand
        }
    }
    
    private void OnLeftHandGemsChanged(int currentGems, int gemsNeeded)
    {
        bool gemsIncreased = currentGems > _leftHandGems;
        _leftHandGems = currentGems;
        _leftHandGemsNeeded = gemsNeeded;
        
        UpdateLeftHandUI();
        
        if (gemsIncreased)
        {
            TriggerGemCollectionEffect(false); // false = left hand
        }
    }
    
    private void OnRightHandLevelChanged(int newLevel)
    {
        bool leveledUp = newLevel > _rightHandLevel;
        _rightHandLevel = newLevel;
        
        UpdateRightHandUI();
        
        if (leveledUp)
        {
            TriggerLevelUpEffect(true); // true = right hand
        }
    }
    
    private void OnRightHandGemsChanged(int currentGems, int gemsNeeded)
    {
        bool gemsIncreased = currentGems > _rightHandGems;
        _rightHandGems = currentGems;
        _rightHandGemsNeeded = gemsNeeded;
        
        UpdateRightHandUI();
        
        if (gemsIncreased)
        {
            TriggerGemCollectionEffect(true); // true = right hand
        }
    }
    
    // Heat event handlers
    private void OnHandHeatChanged(bool isPrimaryHand, float currentHeat, float maxHeat)
    {
        if (isPrimaryHand) // Primary = LMB = LEFT physical hand → LEFT side UI
        {
            _leftHandHeat = currentHeat;
            _leftHandMaxHeat = maxHeat;
            UpdateLeftHandHeatUI();
        }
        else // Secondary = RMB = RIGHT physical hand → RIGHT side UI
        {
            _rightHandHeat = currentHeat;
            _rightHandMaxHeat = maxHeat;
            UpdateRightHandHeatUI();
        }
        
        // Check for heat warning threshold (70%)
        float heatPercentage = maxHeat > 0 ? currentHeat / maxHeat : 0f;
        if (heatPercentage >= 0.7f && heatPercentage < 1f)
        {
            TriggerHeatWarningEffect(!isPrimaryHand); // Invert for trigger methods
            ActivateHeatWarningParticles(isPrimaryHand, true);
        }
        else if (heatPercentage < 0.7f)
        {
            // Deactivate warning particles when heat drops below threshold
            ActivateHeatWarningParticles(isPrimaryHand, false);
        }
    }
    
    private void OnHandOverheated(bool isPrimaryHand)
    {
        if (isPrimaryHand) // Primary = LMB = LEFT physical hand → LEFT side UI
        {
            _leftHandOverheated = true;
            UpdateLeftHandHeatUI();
        }
        else // Secondary = RMB = RIGHT physical hand → RIGHT side UI
        {
            _rightHandOverheated = true;
            UpdateRightHandHeatUI();
        }
        
        // Deactivate warning particles and activate overheat particles
        ActivateHeatWarningParticles(isPrimaryHand, false);
        ActivateOverheatParticles(isPrimaryHand, true);
        
        TriggerOverheatEffect(!isPrimaryHand); // Invert for trigger methods
    }
    
    private void OnHandRecoveredFromOverheat(bool isPrimaryHand)
    {
        if (isPrimaryHand) // Primary = LMB = LEFT physical hand → LEFT side UI
        {
            _leftHandOverheated = false;
            UpdateLeftHandHeatUI();
        }
        else // Secondary = RMB = RIGHT physical hand → RIGHT side UI
        {
            _rightHandOverheated = false;
            UpdateRightHandHeatUI();
        }
        
        // Deactivate overheat particles when recovering
        ActivateOverheatParticles(isPrimaryHand, false);
        
        TriggerRecoveryEffect(!isPrimaryHand); // Invert for trigger methods
    }
    
    // UI Update Methods
    private void UpdateLeftHandUI()
    {
        // Update level text
        if (leftHandLevelText != null)
        {
            leftHandLevelText.text = $"LVL {_leftHandLevel}";
        }
        
        // Update gems text
        if (leftHandGemsText != null)
        {
            leftHandGemsText.text = $"{_leftHandGems} Gems";
        }
        
        // Update progress text and bar
        if (_leftHandLevel >= 4) // Max level
        {
            if (leftHandProgressText != null)
                leftHandProgressText.text = "MAX LEVEL";
            if (leftHandProgressBar != null)
                leftHandProgressBar.value = 1f;
        }
        else
        {
            if (leftHandProgressText != null)
                leftHandProgressText.text = $"{_leftHandGems}/{_leftHandGemsNeeded}";
            if (leftHandProgressBar != null)
                leftHandProgressBar.value = _leftHandGemsNeeded > 0 ? (float)_leftHandGems / _leftHandGemsNeeded : 0f;
        }
        
        // Update progress bar color
        UpdateProgressBarColor(leftHandProgressBar, _leftHandGems, _leftHandGemsNeeded);
        
        // Update heat UI
        UpdateLeftHandHeatUI();
    }
    
    private void UpdateRightHandUI()
    {
        // Update level text
        if (rightHandLevelText != null)
        {
            rightHandLevelText.text = $"LVL {_rightHandLevel}";
        }
        
        // Update gems text
        if (rightHandGemsText != null)
        {
            rightHandGemsText.text = $"{_rightHandGems} Gems";
        }
        
        // Update progress text and bar
        if (_rightHandLevel >= 4) // Max level
        {
            if (rightHandProgressText != null)
                rightHandProgressText.text = "MAX LEVEL";
            if (rightHandProgressBar != null)
                rightHandProgressBar.value = 1f;
        }
        else
        {
            if (rightHandProgressText != null)
                rightHandProgressText.text = $"{_rightHandGems}/{_rightHandGemsNeeded}";
            if (rightHandProgressBar != null)
                rightHandProgressBar.value = _rightHandGemsNeeded > 0 ? (float)_rightHandGems / _rightHandGemsNeeded : 0f;
        }
        
        // Update progress bar color
        UpdateProgressBarColor(rightHandProgressBar, _rightHandGems, _rightHandGemsNeeded);
        
        // Update heat UI
        UpdateRightHandHeatUI();
    }
    
    private void UpdateProgressBarColor(Slider progressBar, int currentGems, int gemsNeeded)
    {
        if (progressBar == null || progressBar.fillRect == null || progressBarGradient == null) return;
        
        Image fillImage = progressBar.fillRect.GetComponent<Image>();
        if (fillImage != null)
        {
            float progress = gemsNeeded > 0 ? (float)currentGems / gemsNeeded : 1f;
            fillImage.color = progressBarGradient.Evaluate(progress);
        }
    }
    
    // Heat UI Update Methods
    private void UpdateLeftHandHeatUI()
    {
        float heatPercentage = _leftHandMaxHeat > 0 ? _leftHandHeat / _leftHandMaxHeat : 0f;
        
        // Robust overheated state check - use actual heat manager state, not cached value
        bool isActuallyOverheated = PlayerOverheatManager.Instance != null ? 
            PlayerOverheatManager.Instance.IsHandOverheated(true) : false;
        
        // Sync our cached state with the actual state
        if (_leftHandOverheated != isActuallyOverheated)
        {
            _leftHandOverheated = isActuallyOverheated;
            
            // CRITICAL: Reset text scale when overheat state changes
            if (!isActuallyOverheated && leftHandHeatText != null)
            {
                leftHandHeatText.transform.localScale = _originalLeftHeatTextScale;
            }
        }
        
        // Update heat text
        if (leftHandHeatText != null)
        {
            if (isActuallyOverheated)
            {
                leftHandHeatText.text = "OVERHEATED!";
                leftHandHeatText.color = overheatedTextColor;
            }
            else
            {
                leftHandHeatText.text = $"{Mathf.RoundToInt(heatPercentage * 100)}% Heat";
                leftHandHeatText.color = _originalLeftHeatColor;
                // Ensure scale is reset when not overheated
                leftHandHeatText.transform.localScale = _originalLeftHeatTextScale;
            }
        }
        
        // Update heat bar
        if (leftHandHeatBar != null)
        {
            leftHandHeatBar.value = heatPercentage;
            UpdateHeatBarColor(leftHandHeatBar, heatPercentage, isActuallyOverheated);
        }
    }
    
    private void UpdateRightHandHeatUI()
    {
        float heatPercentage = _rightHandMaxHeat > 0 ? _rightHandHeat / _rightHandMaxHeat : 0f;
        
        // Robust overheated state check - use actual heat manager state, not cached value
        bool isActuallyOverheated = PlayerOverheatManager.Instance != null ? 
            PlayerOverheatManager.Instance.IsHandOverheated(false) : false;
        
        // Sync our cached state with the actual state
        if (_rightHandOverheated != isActuallyOverheated)
        {
            _rightHandOverheated = isActuallyOverheated;
            
            // CRITICAL: Reset text scale when overheat state changes
            if (!isActuallyOverheated && rightHandHeatText != null)
            {
                rightHandHeatText.transform.localScale = _originalRightHeatTextScale;
            }
        }
        
        // Update heat text
        if (rightHandHeatText != null)
        {
            if (isActuallyOverheated)
            {
                rightHandHeatText.text = "OVERHEATED!";
                rightHandHeatText.color = overheatedTextColor;
            }
            else
            {
                rightHandHeatText.text = $"{Mathf.RoundToInt(heatPercentage * 100)}% Heat";
                rightHandHeatText.color = _originalRightHeatColor;
                // Ensure scale is reset when not overheated
                rightHandHeatText.transform.localScale = _originalRightHeatTextScale;
            }
        }
        
        // Update heat bar
        if (rightHandHeatBar != null)
        {
            rightHandHeatBar.value = heatPercentage;
            UpdateHeatBarColor(rightHandHeatBar, heatPercentage, isActuallyOverheated);
        }
    }
    
    private void UpdateHeatBarColor(Slider heatBar, float heatPercentage, bool isOverheated)
    {
        if (heatBar == null || heatBar.fillRect == null) return;
        
        Image fillImage = heatBar.fillRect.GetComponent<Image>();
        if (fillImage != null)
        {
            if (isOverheated)
            {
                // Flash red when overheated
                fillImage.color = Color.red;
            }
            else if (heatBarGradient != null)
            {
                // Use gradient for normal heat levels
                fillImage.color = heatBarGradient.Evaluate(heatPercentage);
            }
        }
    }
    
    // Particle Effect Control Methods
    private void ActivateHeatWarningParticles(bool isPrimaryHand, bool activate)
    {
        ParticleSystem targetParticles = isPrimaryHand ? leftHandHeatWarningParticles : rightHandHeatWarningParticles;
        bool currentlyActive = isPrimaryHand ? _leftHandWarningParticlesActive : _rightHandWarningParticlesActive;
        
        if (targetParticles != null && currentlyActive != activate)
        {
            if (activate)
            {
                if (!targetParticles.isPlaying)
                {
                    targetParticles.Play();
                }
            }
            else
            {
                if (targetParticles.isPlaying)
                {
                    targetParticles.Stop();
                }
            }
            
            // Update state tracking
            if (isPrimaryHand)
                _leftHandWarningParticlesActive = activate;
            else
                _rightHandWarningParticlesActive = activate;
        }
    }
    
    private void ActivateOverheatParticles(bool isPrimaryHand, bool activate)
    {
        ParticleSystem targetParticles = isPrimaryHand ? leftHandOverheatParticles : rightHandOverheatParticles;
        bool currentlyActive = isPrimaryHand ? _leftHandOverheatParticlesActive : _rightHandOverheatParticlesActive;
        
        if (targetParticles != null && currentlyActive != activate)
        {
            if (activate)
            {
                if (!targetParticles.isPlaying)
                {
                    targetParticles.Play();
                }
            }
            else
            {
                if (targetParticles.isPlaying)
                {
                    targetParticles.Stop();
                }
            }
            
            // Update state tracking
            if (isPrimaryHand)
                _leftHandOverheatParticlesActive = activate;
            else
                _rightHandOverheatParticlesActive = activate;
        }
    }
    
    /// <summary>
    /// Ensure all particles are properly stopped when the component is disabled
    /// </summary>
    void OnDisable()
    {
        // Stop heat sync coroutine
        StopHeatSyncCoroutine();
        
        // Stop all particle effects when UI is disabled
        ActivateHeatWarningParticles(true, false);   // Right hand warning
        ActivateHeatWarningParticles(false, false);  // Left hand warning
        ActivateOverheatParticles(true, false);      // Right hand overheat
        ActivateOverheatParticles(false, false);     // Left hand overheat
    }
    
    /// <summary>
    /// Start the heat sync coroutine for robust state management
    /// </summary>
    private void StartHeatSyncCoroutine()
    {
        StopHeatSyncCoroutine(); // Stop any existing coroutine
        _heatSyncCoroutine = StartCoroutine(HeatSyncCoroutine());
    }
    
    /// <summary>
    /// Stop the heat sync coroutine
    /// </summary>
    private void StopHeatSyncCoroutine()
    {
        if (_heatSyncCoroutine != null)
        {
            StopCoroutine(_heatSyncCoroutine);
            _heatSyncCoroutine = null;
        }
    }
    
    /// <summary>
    /// Coroutine that periodically syncs heat states to ensure UI accuracy
    /// </summary>
    private IEnumerator HeatSyncCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(HEAT_SYNC_INTERVAL);
            
            // Only sync if PlayerOverheatManager exists
            if (PlayerOverheatManager.Instance != null)
            {
                // Get current heat values directly from heat manager
                // CORRECTED: Primary = LEFT hand (LMB), Secondary = RIGHT hand (RMB)
                float leftHeat = PlayerOverheatManager.Instance.CurrentHeatPrimary;
                float rightHeat = PlayerOverheatManager.Instance.CurrentHeatSecondary;
                float maxHeat = PlayerOverheatManager.Instance.maxHeat;
                
                // Check if our cached values are out of sync (with small tolerance for floating point)
                bool leftHeatChanged = Mathf.Abs(_leftHandHeat - leftHeat) > 0.1f;
                bool rightHeatChanged = Mathf.Abs(_rightHandHeat - rightHeat) > 0.1f;
                bool maxHeatChanged = Mathf.Abs(_leftHandMaxHeat - maxHeat) > 0.1f || Mathf.Abs(_rightHandMaxHeat - maxHeat) > 0.1f;
                
                // Update cached values if they changed
                if (leftHeatChanged || maxHeatChanged)
                {
                    _leftHandHeat = leftHeat;
                    _leftHandMaxHeat = maxHeat;
                    UpdateLeftHandHeatUI();
                }
                
                if (rightHeatChanged || maxHeatChanged)
                {
                    _rightHandHeat = rightHeat;
                    _rightHandMaxHeat = maxHeat;
                    UpdateRightHandHeatUI();
                }
                
                // Update particle effects if needed
                if (leftHeatChanged || rightHeatChanged || maxHeatChanged)
                {
                    RefreshParticleEffects();
                }
            }
        }
    }
    
    // Visual Effects
    private void TriggerLevelUpEffect(bool isRightHand)
    {
        RectTransform container = isRightHand ? rightHandContainer : leftHandContainer;
        TextMeshProUGUI levelText = isRightHand ? rightHandLevelText : leftHandLevelText;
        
        // CRITICAL: Add null checks for destroyed UI elements
        if (container == null || levelText == null)
        {
            Debug.LogWarning($"[HandUIManager] Cannot trigger level up effect for {(isRightHand ? "right" : "left")} hand - UI elements are null/destroyed.");
            return;
        }
        
        // Stop any existing animation
        if (isRightHand && _rightHandAnimCoroutine != null)
        {
            StopCoroutine(_rightHandAnimCoroutine);
        }
        else if (!isRightHand && _leftHandAnimCoroutine != null)
        {
            StopCoroutine(_leftHandAnimCoroutine);
        }
        
        // Start level up animation
        Coroutine animCoroutine = StartCoroutine(LevelUpAnimation(container, levelText, isRightHand));
        
        if (isRightHand)
            _rightHandAnimCoroutine = animCoroutine;
        else
            _leftHandAnimCoroutine = animCoroutine;
        
        // Spawn level up effect
        if (levelUpEffectPrefab != null && container != null)
        {
            GameObject effect = Instantiate(levelUpEffectPrefab, container.position, Quaternion.identity, container);
            Destroy(effect, levelUpGlowDuration);
        }
    }
    
    private void TriggerGemCollectionEffect(bool isRightHand)
    {
        TextMeshProUGUI gemsText = isRightHand ? rightHandGemsText : leftHandGemsText;
        RectTransform container = isRightHand ? rightHandContainer : leftHandContainer;
        
        // CRITICAL: Add null checks for destroyed UI elements
        if (gemsText == null || container == null)
        {
            Debug.LogWarning($"[HandUIManager] Cannot trigger gem collection effect for {(isRightHand ? "right" : "left")} hand - UI elements are null/destroyed.");
            return;
        }
        
        // Start gem collection pulse
        StartCoroutine(GemCollectionPulse(gemsText, isRightHand));
        
        // Spawn gem collection effect
        if (gemCollectionEffectPrefab != null && container != null)
        {
            GameObject effect = Instantiate(gemCollectionEffectPrefab, container.position, Quaternion.identity, container);
            Destroy(effect, gemPulseDuration);
        }
    }
    
    private void TriggerHeatWarningEffect(bool isRightHand)
    {
        TextMeshProUGUI heatText = isRightHand ? rightHandHeatText : leftHandHeatText;
        RectTransform container = isRightHand ? rightHandContainer : leftHandContainer;
        
        // Start heat warning pulse
        StartCoroutine(HeatWarningPulse(heatText, isRightHand));
        
        // Spawn heat warning effect
        if (overheatWarningEffectPrefab != null && container != null)
        {
            GameObject effect = Instantiate(overheatWarningEffectPrefab, container.position, Quaternion.identity, container);
            Destroy(effect, heatWarningPulseDuration);
        }
    }
    
    private void TriggerOverheatEffect(bool isRightHand)
    {
        RectTransform container = isRightHand ? rightHandContainer : leftHandContainer;
        TextMeshProUGUI heatText = isRightHand ? rightHandHeatText : leftHandHeatText;
        
        // Start overheat animation
        StartCoroutine(OverheatAnimation(container, heatText, isRightHand));
        
        // Spawn overheat effect
        if (overheatWarningEffectPrefab != null && container != null)
        {
            GameObject effect = Instantiate(overheatWarningEffectPrefab, container.position, Quaternion.identity, container);
            Destroy(effect, levelUpGlowDuration);
        }
    }
    
    private void TriggerRecoveryEffect(bool isRightHand)
    {
        TextMeshProUGUI heatText = isRightHand ? rightHandHeatText : leftHandHeatText;
        Color originalColor = isRightHand ? _originalRightHeatColor : _originalLeftHeatColor;
        
        // Start recovery animation
        StartCoroutine(RecoveryAnimation(heatText, originalColor));
    }
    
    // Animation Coroutines
    private IEnumerator LevelUpAnimation(RectTransform container, TextMeshProUGUI levelText, bool isRightHand)
    {
        if (container == null || levelText == null) yield break;
        
        Vector3 originalScale = container.localScale;
        Color originalColor = isRightHand ? _originalRightLevelColor : _originalLeftLevelColor;
        
        // Scale up and flash color
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / animationDuration;
            
            // Scale animation
            float scale = Mathf.Lerp(1f, scaleAnimationIntensity, Mathf.Sin(progress * Mathf.PI));
            container.localScale = originalScale * scale;
            
            // Color flash
            levelText.color = Color.Lerp(originalColor, levelUpFlashColor, Mathf.Sin(progress * Mathf.PI));
            
            yield return null;
        }
        
        // Restore original state
        container.localScale = originalScale;
        levelText.color = originalColor;
        
        // Glow effect
        yield return StartCoroutine(GlowEffect(levelText, levelUpFlashColor, originalColor, levelUpGlowDuration));
    }
    
    private IEnumerator GemCollectionPulse(TextMeshProUGUI gemsText, bool isRightHand)
    {
        if (gemsText == null) yield break;
        
        Color originalColor = isRightHand ? _originalRightGemsColor : _originalLeftGemsColor;
        Vector3 originalScale = gemsText.transform.localScale;
        
        // Quick pulse animation
        float elapsed = 0f;
        while (elapsed < gemPulseDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / gemPulseDuration;
            
            // Scale pulse
            float scale = 1f + (scaleAnimationIntensity - 1f) * 0.5f * Mathf.Sin(progress * Mathf.PI * 2f);
            gemsText.transform.localScale = originalScale * scale;
            
            // Color flash
            gemsText.color = Color.Lerp(originalColor, gemCollectionFlashColor, Mathf.Sin(progress * Mathf.PI * 2f));
            
            yield return null;
        }
        
        // Restore original state
        gemsText.transform.localScale = originalScale;
        gemsText.color = originalColor;
    }
    
    private IEnumerator GlowEffect(TextMeshProUGUI text, Color glowColor, Color originalColor, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // Fade glow out
            text.color = Color.Lerp(glowColor, originalColor, progress);
            
            yield return null;
        }
        
        text.color = originalColor;
    }
    
    private IEnumerator HeatWarningPulse(TextMeshProUGUI heatText, bool isRightHand)
    {
        if (heatText == null) yield break;
        
        Color originalColor = isRightHand ? _originalRightHeatColor : _originalLeftHeatColor;
        Vector3 originalScale = heatText.transform.localScale;
        
        // Warning pulse animation
        float elapsed = 0f;
        while (elapsed < heatWarningPulseDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / heatWarningPulseDuration;
            
            // Scale pulse
            float scale = 1f + (scaleAnimationIntensity - 1f) * 0.3f * Mathf.Sin(progress * Mathf.PI * 3f);
            heatText.transform.localScale = originalScale * scale;
            
            // Color flash
            heatText.color = Color.Lerp(originalColor, heatWarningFlashColor, Mathf.Sin(progress * Mathf.PI * 3f));
            
            yield return null;
        }
        
        // Restore original state
        heatText.transform.localScale = originalScale;
        heatText.color = originalColor;
    }
    
    private IEnumerator OverheatAnimation(RectTransform container, TextMeshProUGUI heatText, bool isRightHand)
    {
        if (container == null || heatText == null) yield break;
        
        Vector3 originalContainerScale = container.localScale;
        Vector3 originalTextScale = isRightHand ? _originalRightHeatTextScale : _originalLeftHeatTextScale;
        
        // Intense shake and scale animation
        float elapsed = 0f;
        while (elapsed < animationDuration * 2f) // Longer animation for overheat
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (animationDuration * 2f);
            
            // Container shake animation
            Vector3 shake = Random.insideUnitCircle * 0.1f * (1f - progress);
            container.localScale = originalContainerScale + (Vector3)shake;
            
            // Text scale pulse animation (grows and shrinks dramatically)
            float scalePulse = 1f + (scaleAnimationIntensity * 0.8f) * Mathf.Sin(progress * Mathf.PI * 6f);
            heatText.transform.localScale = originalTextScale * scalePulse;
            
            // Flash red and white
            heatText.color = Color.Lerp(overheatedTextColor, Color.white, Mathf.Sin(progress * Mathf.PI * 8f));
            
            yield return null;
        }
        
        // CRITICAL: Restore both container and text to original scales
        container.localScale = originalContainerScale;
        heatText.transform.localScale = originalTextScale;
        heatText.color = overheatedTextColor;
    }
    
    private IEnumerator RecoveryAnimation(TextMeshProUGUI heatText, Color originalColor)
    {
        if (heatText == null) yield break;
        
        // Use the stored original scale, not the current scale which might be wrong
        bool isRightHand = (heatText == rightHandHeatText);
        Vector3 originalScale = isRightHand ? _originalRightHeatTextScale : _originalLeftHeatTextScale;
        
        // Recovery glow animation
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / animationDuration;
            
            // Scale pulse (gentle recovery animation)
            float scale = 1f + (scaleAnimationIntensity - 1f) * 0.3f * Mathf.Sin(progress * Mathf.PI);
            heatText.transform.localScale = originalScale * scale;
            
            // Color transition from red to original
            heatText.color = Color.Lerp(overheatedTextColor, originalColor, progress);
            
            yield return null;
        }
        
        // CRITICAL: Restore to the stored original scale and color
        heatText.transform.localScale = originalScale;
        heatText.color = originalColor;
    }
    
    // Public Methods
    public void RefreshAllHandUI()
    {
        // Get current data from PlayerProgression if available
        if (PlayerProgression.Instance != null)
        {
            _leftHandLevel = PlayerProgression.Instance.secondaryHandLevel;
            _leftHandGems = PlayerProgression.Instance.gemsCollectedForSecondaryHand;
            _leftHandGemsNeeded = PlayerProgression.Instance.GetGemsNeededForNext_AutoLevel_Threshold(false);
            
            _rightHandLevel = PlayerProgression.Instance.primaryHandLevel;
            _rightHandGems = PlayerProgression.Instance.gemsCollectedForPrimaryHand;
            _rightHandGemsNeeded = PlayerProgression.Instance.GetGemsNeededForNext_AutoLevel_Threshold(true);
        }
        
        // Get current heat data from PlayerOverheatManager if available
        // CORRECTED: Primary = LEFT hand (LMB), Secondary = RIGHT hand (RMB)
        if (PlayerOverheatManager.Instance != null)
        {
            _leftHandHeat = PlayerOverheatManager.Instance.CurrentHeatPrimary;
            _leftHandMaxHeat = PlayerOverheatManager.Instance.maxHeat;
            _leftHandOverheated = PlayerOverheatManager.Instance.IsHandOverheated(true);
            
            _rightHandHeat = PlayerOverheatManager.Instance.CurrentHeatSecondary;
            _rightHandMaxHeat = PlayerOverheatManager.Instance.maxHeat;
            _rightHandOverheated = PlayerOverheatManager.Instance.IsHandOverheated(false);
        }
        
        // Update UI
        UpdateLeftHandUI();
        UpdateRightHandUI();
        
        // Update particle effects based on current heat levels
        RefreshParticleEffects();
    }
    
    /// <summary>
    /// Force update UI with specific values (for testing or external systems)
    /// </summary>
    public void ForceUpdateHandUI(bool isRightHand, int level, int gems, int gemsNeeded)
    {
        if (isRightHand)
        {
            _rightHandLevel = level;
            _rightHandGems = gems;
            _rightHandGemsNeeded = gemsNeeded;
            UpdateRightHandUI();
        }
        else
        {
            _leftHandLevel = level;
            _leftHandGems = gems;
            _leftHandGemsNeeded = gemsNeeded;
            UpdateLeftHandUI();
        }
    }
    
    /// <summary>
    /// Force refresh UI when admin cheats or external systems change hand levels
    /// This ensures the UI always shows current hand levels
    /// </summary>
    public static void ForceRefreshHandLevels()
    {
        if (Instance != null)
        {
            Instance.RefreshAllHandUI();
        }
    }
    
    /// <summary>
    /// Refresh particle effects based on current heat levels
    /// </summary>
    private void RefreshParticleEffects()
    {
        if (PlayerOverheatManager.Instance == null) return;
        
        // Left hand particle effects - use actual heat manager state
        // CORRECTED: Primary = LEFT hand (LMB), Secondary = RIGHT hand (RMB)
        float leftHeatPercentage = PlayerOverheatManager.Instance.maxHeat > 0 ? 
            PlayerOverheatManager.Instance.CurrentHeatPrimary / PlayerOverheatManager.Instance.maxHeat : 0f;
        bool leftOverheated = PlayerOverheatManager.Instance.IsHandOverheated(true);
        
        if (leftOverheated)
        {
            ActivateHeatWarningParticles(true, false);  // FIXED: Left hand = isPrimaryHand=true
            ActivateOverheatParticles(true, true);      // FIXED: Left hand = isPrimaryHand=true
        }
        else if (leftHeatPercentage >= 0.7f)
        {
            ActivateOverheatParticles(true, false);     // FIXED: Left hand = isPrimaryHand=true
            ActivateHeatWarningParticles(true, true);   // FIXED: Left hand = isPrimaryHand=true
        }
        else
        {
            ActivateHeatWarningParticles(true, false);  // FIXED: Left hand = isPrimaryHand=true
            ActivateOverheatParticles(true, false);     // FIXED: Left hand = isPrimaryHand=true
        }
        
        // Right hand particle effects - use actual heat manager state
        // CORRECTED: Primary = LEFT hand (LMB), Secondary = RIGHT hand (RMB)
        float rightHeatPercentage = PlayerOverheatManager.Instance.maxHeat > 0 ? 
            PlayerOverheatManager.Instance.CurrentHeatSecondary / PlayerOverheatManager.Instance.maxHeat : 0f;
        bool rightOverheated = PlayerOverheatManager.Instance.IsHandOverheated(false);
        
        if (rightOverheated)
        {
            ActivateHeatWarningParticles(false, false); // FIXED: Right hand = isPrimaryHand=false
            ActivateOverheatParticles(false, true);     // FIXED: Right hand = isPrimaryHand=false
        }
        else if (rightHeatPercentage >= 0.7f)
        {
            ActivateOverheatParticles(false, false);    // FIXED: Right hand = isPrimaryHand=false
            ActivateHeatWarningParticles(false, true);  // FIXED: Right hand = isPrimaryHand=false
        }
        else
        {
            ActivateHeatWarningParticles(false, false); // FIXED: Right hand = isPrimaryHand=false
            ActivateOverheatParticles(false, false);    // FIXED: Right hand = isPrimaryHand=false
        }
    }
    
    /// <summary>
    /// Test method to trigger level up effect
    /// </summary>
    [ContextMenu("Test Left Hand Level Up")]
    public void TestLeftHandLevelUp()
    {
        TriggerLevelUpEffect(false);
    }
    
    /// <summary>
    /// Test method to trigger level up effect
    /// </summary>
    [ContextMenu("Test Right Hand Level Up")]
    public void TestRightHandLevelUp()
    {
        TriggerLevelUpEffect(true);
    }
    
    /// <summary>
    /// Test method to trigger gem collection effect
    /// </summary>
    [ContextMenu("Test Left Hand Gem Collection")]
    public void TestLeftHandGemCollection()
    {
        TriggerGemCollectionEffect(false);
    }
    
    /// <summary>
    /// Test method to trigger gem collection effect
    /// </summary>
    [ContextMenu("Test Right Hand Gem Collection")]
    public void TestRightHandGemCollection()
    {
        TriggerGemCollectionEffect(true);
    }
    
    /// <summary>
    /// Test method to trigger heat warning effect
    /// </summary>
    [ContextMenu("Test Left Hand Heat Warning")]
    public void TestLeftHandHeatWarning()
    {
        TriggerHeatWarningEffect(false);
    }
    
    /// <summary>
    /// Test method to trigger heat warning effect
    /// </summary>
    [ContextMenu("Test Right Hand Heat Warning")]
    public void TestRightHandHeatWarning()
    {
        TriggerHeatWarningEffect(true);
    }
    
    /// <summary>
    /// Test method to trigger overheat effect
    /// </summary>
    [ContextMenu("Test Left Hand Overheat")]
    public void TestLeftHandOverheat()
    {
        TriggerOverheatEffect(false);
    }
    
    /// <summary>
    /// Test method to trigger overheat effect
    /// </summary>
    [ContextMenu("Test Right Hand Overheat")]
    public void TestRightHandOverheat()
    {
        TriggerOverheatEffect(true);
    }
    
    /// <summary>
    /// Test method to trigger recovery effect
    /// </summary>
    [ContextMenu("Test Left Hand Recovery")]
    public void TestLeftHandRecovery()
    {
        TriggerRecoveryEffect(false);
    }
    
    /// <summary>
    /// Test method to trigger recovery effect
    /// </summary>
    [ContextMenu("Test Right Hand Recovery")]
    public void TestRightHandRecovery()
    {
        TriggerRecoveryEffect(true);
    }
    
    /// <summary>
    /// Force update heat UI with specific values (for testing)
    /// </summary>
    public void ForceUpdateHeatUI(bool isRightHand, float heat, float maxHeat, bool isOverheated)
    {
        if (isRightHand)
        {
            _rightHandHeat = heat;
            _rightHandMaxHeat = maxHeat;
            _rightHandOverheated = isOverheated;
            UpdateRightHandHeatUI();
        }
        else
        {
            _leftHandHeat = heat;
            _leftHandMaxHeat = maxHeat;
            _leftHandOverheated = isOverheated;
            UpdateLeftHandHeatUI();
        }
        
        // Update particle effects based on new heat values
        RefreshParticleEffects();
    }
    
    /// <summary>
    /// Test method to activate left hand heat warning particles
    /// </summary>
    [ContextMenu("Test Left Hand Heat Warning Particles")]
    public void TestLeftHandHeatWarningParticles()
    {
        ActivateHeatWarningParticles(false, true);
    }
    
    /// <summary>
    /// Test method to activate right hand heat warning particles
    /// </summary>
    [ContextMenu("Test Right Hand Heat Warning Particles")]
    public void TestRightHandHeatWarningParticles()
    {
        ActivateHeatWarningParticles(true, true);
    }
    
    /// <summary>
    /// Test method to activate left hand overheat particles
    /// </summary>
    [ContextMenu("Test Left Hand Overheat Particles")]
    public void TestLeftHandOverheatParticles()
    {
        ActivateOverheatParticles(false, true);
    }
    
    /// <summary>
    /// Test method to activate right hand overheat particles
    /// </summary>
    [ContextMenu("Test Right Hand Overheat Particles")]
    public void TestRightHandOverheatParticles()
    {
        ActivateOverheatParticles(true, true);
    }
    
    /// <summary>
    /// Test method to stop all particle effects
    /// </summary>
    [ContextMenu("Stop All Heat Particles")]
    public void StopAllHeatParticles()
    {
        ActivateHeatWarningParticles(true, false);   // Right hand warning
        ActivateHeatWarningParticles(false, false);  // Left hand warning
        ActivateOverheatParticles(true, false);      // Right hand overheat
        ActivateOverheatParticles(false, false);     // Left hand overheat
    }
}
