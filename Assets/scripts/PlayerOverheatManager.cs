// --- PlayerOverheatManager.cs (FULL & FINAL - Corrected for API Changes) ---
using UnityEngine;
using System;
using GeminiGauntlet.Audio;

public class PlayerOverheatManager : MonoBehaviour
{
    public static PlayerOverheatManager Instance { get; private set; }

    [Header("Sound Events")]
    [Tooltip("Reference to SoundEvents asset for overheat audio feedback")]
    public SoundEvents soundEvents;
    
    [Header("Global Heat Settings")]
    public float maxHeat = 100f;
    public float streamHeatAccumulationRate = 25f;
    public float shotgunHeatCost = 15f;
    public float heatCooldownRate = 33f;
    public float heatCooldownDelay = 0.5f;
    public float forcedCooldownDurationAfterOverheat = 2.5f;
    [Tooltip("At what percentage of maxHeat is the 'warning' state triggered for UI feedback?")]
    [Range(0f, 1f)] public float heatWarningThresholdPercent = 0.70f;

    [Header("Ability & Collection Heat Modifiers")]
    public float heatCostForAOE = 30f;
    public float heatReductionPerGemCollected = 2f;

    [Header("Manual Hand Visual Assignment")]
    [Tooltip("MANUAL ASSIGNMENT: Drag your LEFT hand's HandOverheatVisuals here (isPrimary=true)")]
    public HandOverheatVisuals manualPrimaryHandVisuals;
    [Tooltip("MANUAL ASSIGNMENT: Drag your RIGHT hand's HandOverheatVisuals here (isPrimary=false)")]
    public HandOverheatVisuals manualSecondaryHandVisuals;

    public HandOverheatVisuals ActivePrimaryHandVisuals { get; private set; }
    public HandOverheatVisuals ActiveSecondaryHandVisuals { get; private set; }

    [Header("LIVE HEAT (For Inspector Testing)")]
    [Range(0f, 1000f)] public float _currentHeatPrimary_Inspector;
    [Range(0f, 1000f)] public float _currentHeatSecondary_Inspector;

    public float CurrentHeatPrimary { get; private set; }
    public float CurrentHeatSecondary { get; private set; }

    public event Action<bool, float, float> OnHeatChangedForHUD;
    public event Action<bool> OnHandFullyOverheated;
    public event Action<bool> OnHandDegradedDueToOverheat;
    public event Action<bool> OnHandRecoveredFromForcedCooldown;

    private float _timeSinceLastShotPrimary;
    private float _timeSinceLastShotSecondary;
    private bool _isPrimaryHandFiringStreamInternal;
    private bool _isSecondaryHandFiringStreamInternal;
    private bool _isPrimaryInForcedCooldown;
    private float _primaryForcedCooldownEndTime;
    private bool _isSecondaryInForcedCooldown;
    private float _secondaryForcedCooldownEndTime;

    private float _prevInspectorHeatPrimary;
    private float _prevInspectorHeatSecondary;

    private bool _primaryWarningSent = false;
    private bool _secondaryWarningSent = false;
    
    // Sound alert tracking - prevent spam
    private bool _primary50PercentSoundPlayed = false;
    private bool _primary70PercentSoundPlayed = false;
    private bool _secondary50PercentSoundPlayed = false;
    private bool _secondary70PercentSoundPlayed = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }

        CurrentHeatPrimary = Mathf.Clamp(_currentHeatPrimary_Inspector, 0, maxHeat);
        CurrentHeatSecondary = Mathf.Clamp(_currentHeatSecondary_Inspector, 0, maxHeat);
        _prevInspectorHeatPrimary = CurrentHeatPrimary;
        _prevInspectorHeatSecondary = CurrentHeatSecondary;
    }

    void Start()
    {
        _timeSinceLastShotPrimary = heatCooldownDelay;
        _timeSinceLastShotSecondary = heatCooldownDelay;

        // Apply manual assignments if provided (overrides auto-registration)
        if (manualPrimaryHandVisuals != null)
        {
            Debug.Log($"PlayerOverheatManager: Using MANUAL assignment for Primary hand: {manualPrimaryHandVisuals.gameObject.name}", this);
            ActivePrimaryHandVisuals = manualPrimaryHandVisuals;
        }
        if (manualSecondaryHandVisuals != null)
        {
            Debug.Log($"PlayerOverheatManager: Using MANUAL assignment for Secondary hand: {manualSecondaryHandVisuals.gameObject.name}", this);
            ActiveSecondaryHandVisuals = manualSecondaryHandVisuals;
        }

        OnHeatChangedForHUD?.Invoke(true, CurrentHeatPrimary, maxHeat);
        OnHeatChangedForHUD?.Invoke(false, CurrentHeatSecondary, maxHeat);
        UpdateHandVisuals(true); UpdateHandVisuals(false);

        if (maxHeat <= 0) Debug.LogError("PlayerOverheatManager DEBUG LOG: maxHeat is 0 or less! This will likely prevent firing.");
        if (shotgunHeatCost <= 0 && streamHeatAccumulationRate <= 0) Debug.LogWarning("PlayerOverheatManager DEBUG LOG: All heat costs/rates are zero. Heat will not change from firing.");
    }

    void Update()
    {
        bool primaryChangedByInspector = false;
        if (!Mathf.Approximately(_currentHeatPrimary_Inspector, _prevInspectorHeatPrimary))
        {
            CurrentHeatPrimary = Mathf.Clamp(_currentHeatPrimary_Inspector, 0, maxHeat);
            _prevInspectorHeatPrimary = CurrentHeatPrimary; _currentHeatPrimary_Inspector = CurrentHeatPrimary;
            OnHeatChangedForHUD?.Invoke(true, CurrentHeatPrimary, maxHeat); UpdateHandVisuals(true);
            CheckForOverheatFromInspectorChange(true); primaryChangedByInspector = true; _timeSinceLastShotPrimary = 0f;
            HandleHeatWarning(true);
        }
        bool secondaryChangedByInspector = false;
        if (!Mathf.Approximately(_currentHeatSecondary_Inspector, _prevInspectorHeatSecondary))
        {
            CurrentHeatSecondary = Mathf.Clamp(_currentHeatSecondary_Inspector, 0, maxHeat);
            _prevInspectorHeatSecondary = CurrentHeatSecondary; _currentHeatSecondary_Inspector = CurrentHeatSecondary;
            OnHeatChangedForHUD?.Invoke(false, CurrentHeatSecondary, maxHeat); UpdateHandVisuals(false);
            CheckForOverheatFromInspectorChange(false); secondaryChangedByInspector = true; _timeSinceLastShotSecondary = 0f;
            HandleHeatWarning(false);
        }

        if (Time.deltaTime == 0f && !(primaryChangedByInspector || secondaryChangedByInspector)) return;

        if (_isPrimaryInForcedCooldown)
        {
            if (Time.time >= _primaryForcedCooldownEndTime)
            {
                _isPrimaryInForcedCooldown = false;
                OnHandRecoveredFromForcedCooldown?.Invoke(true);
                DynamicPlayerFeedManager.Instance?.ShowOverheatRecovered(true);
                UpdateHandVisuals(true); SyncInspectorValue(true);
                Debug.Log("DEBUG LOG: Primary Recovered from Forced Cooldown.");
            }
            else
            {
                DynamicPlayerFeedManager.Instance?.UpdateOverheatCriticalCountdown(true, _primaryForcedCooldownEndTime - Time.time);
            }
        }
        if (_isSecondaryInForcedCooldown)
        {
            if (Time.time >= _secondaryForcedCooldownEndTime)
            {
                _isSecondaryInForcedCooldown = false;
                OnHandRecoveredFromForcedCooldown?.Invoke(false);
                DynamicPlayerFeedManager.Instance?.ShowOverheatRecovered(false);
                UpdateHandVisuals(false); SyncInspectorValue(false);
                Debug.Log("DEBUG LOG: Secondary Recovered from Forced Cooldown.");
            }
            else
            {
                DynamicPlayerFeedManager.Instance?.UpdateOverheatCriticalCountdown(false, _secondaryForcedCooldownEndTime - Time.time);
            }
        }


        if (!primaryChangedByInspector)
        {
            float oldHeat = CurrentHeatPrimary;
            CurrentHeatPrimary = ProcessHandHeat(true, _isPrimaryHandFiringStreamInternal, CurrentHeatPrimary, ref _timeSinceLastShotPrimary, _isPrimaryInForcedCooldown);
            if (!Mathf.Approximately(oldHeat, CurrentHeatPrimary)) SyncInspectorValue(true);
        }
        if (!secondaryChangedByInspector)
        {
            float oldHeat = CurrentHeatSecondary;
            CurrentHeatSecondary = ProcessHandHeat(false, _isSecondaryHandFiringStreamInternal, CurrentHeatSecondary, ref _timeSinceLastShotSecondary, _isSecondaryInForcedCooldown);
            if (!Mathf.Approximately(oldHeat, CurrentHeatSecondary)) SyncInspectorValue(false);
        }
    }

    private void SyncInspectorValue(bool isPrimary)
    {
        if (isPrimary) { _currentHeatPrimary_Inspector = CurrentHeatPrimary; _prevInspectorHeatPrimary = CurrentHeatPrimary; }
        else { _currentHeatSecondary_Inspector = CurrentHeatSecondary; _prevInspectorHeatSecondary = CurrentHeatSecondary; }
    }

    private void CheckForOverheatFromInspectorChange(bool isPrimary)
    {
        if (isPrimary) { if (CurrentHeatPrimary >= maxHeat && !_isPrimaryInForcedCooldown) TriggerFullOverheatConsequences(true); }
        else { if (CurrentHeatSecondary >= maxHeat && !_isSecondaryInForcedCooldown) TriggerFullOverheatConsequences(false); }
    }

    public void SetActiveHandOverheatVisuals(bool isPrimary, HandOverheatVisuals visuals)
    {
        Debug.Log($"PlayerOverheatManager: Registering {(isPrimary ? "Primary" : "Secondary")} hand visuals: {visuals?.gameObject.name ?? "NULL"}", this);
        if (isPrimary) ActivePrimaryHandVisuals = visuals; else ActiveSecondaryHandVisuals = visuals;
        UpdateHandVisuals(isPrimary);
    }

    private float ProcessHandHeat(bool isPrimary, bool isFiringStreamIntent, float currentHandHeatValue, ref float timeSinceLastShotField, bool isInForcedCooldown)
    {
        float previousHeat = currentHandHeatValue;

        if (isFiringStreamIntent && CanFire(isPrimary, false) && !isInForcedCooldown)
        {
            if (streamHeatAccumulationRate > 0) currentHandHeatValue += streamHeatAccumulationRate * Time.deltaTime;
            timeSinceLastShotField = 0f;
        }
        else
        {
            timeSinceLastShotField += Time.deltaTime;
            if (timeSinceLastShotField >= heatCooldownDelay && currentHandHeatValue > 0 && !isInForcedCooldown)
            {
                if (heatCooldownRate > 0) currentHandHeatValue -= heatCooldownRate * Time.deltaTime;
            }
        }
        currentHandHeatValue = Mathf.Clamp(currentHandHeatValue, 0, maxHeat);

        HandleHeatWarning(isPrimary);

        if (!Mathf.Approximately(previousHeat, currentHandHeatValue))
        {
            OnHeatChangedForHUD?.Invoke(isPrimary, currentHandHeatValue, maxHeat);
            UpdateHandVisuals(isPrimary);
        }
        if (currentHandHeatValue >= maxHeat && !isInForcedCooldown && previousHeat < maxHeat)
        {
            TriggerFullOverheatConsequences(isPrimary);
        }
        return currentHandHeatValue;
    }

    // --- CORRECTED ---
    private void HandleHeatWarning(bool isPrimary)
    {
        float currentHeatValue = isPrimary ? CurrentHeatPrimary : CurrentHeatSecondary;
        float heatPercent = (maxHeat > 0) ? currentHeatValue / maxHeat : 0;

        ref bool warningSentFlag = ref (isPrimary ? ref _primaryWarningSent : ref _secondaryWarningSent);
        ref bool sound50Flag = ref (isPrimary ? ref _primary50PercentSoundPlayed : ref _secondary50PercentSoundPlayed);
        ref bool sound70Flag = ref (isPrimary ? ref _primary70PercentSoundPlayed : ref _secondary70PercentSoundPlayed);

        // 50% heat warning sound
        if (heatPercent >= 0.5f && !sound50Flag)
        {
            PlayOverheatSound(soundEvents?.handHeat50Warning, isPrimary);
            sound50Flag = true;
        }
        else if (heatPercent < 0.5f && sound50Flag)
        {
            sound50Flag = false; // Reset when heat drops below 50%
        }
        
        // 70%+ high heat warning sound and UI
        if (heatPercent >= heatWarningThresholdPercent && heatPercent < 1.0f)
        {
            if (!warningSentFlag)
            {
                DynamicPlayerFeedManager.Instance?.ShowOverheatWarning(isPrimary);
                warningSentFlag = true;
            }
            
            if (!sound70Flag)
            {
                PlayOverheatSound(soundEvents?.handHeatHighWarning, isPrimary);
                sound70Flag = true;
            }
        }
        else if (heatPercent < heatWarningThresholdPercent)
        {
            warningSentFlag = false;
            sound70Flag = false; // Reset when heat drops below threshold
        }
    }

    private void TriggerFullOverheatConsequences(bool isPrimary)
    {
        string handId = isPrimary ? "Primary" : "Secondary";
        Debug.LogWarning($"DEBUG LOG: {handId} Hand TRIGGERING FULL OVERHEAT. Current Heat: {(isPrimary ? CurrentHeatPrimary : CurrentHeatSecondary)}, Max Heat: {maxHeat}");
        
        // Play 100% overheat sound from the specific hand
        PlayOverheatSound(soundEvents?.handOverheated, isPrimary);
        
        if (isPrimary)
        {
            if (_isPrimaryInForcedCooldown) return;
            _isPrimaryInForcedCooldown = true;
            _primaryForcedCooldownEndTime = Time.time + forcedCooldownDurationAfterOverheat;
            DynamicPlayerFeedManager.Instance?.UpdateOverheatCriticalCountdown(true, forcedCooldownDurationAfterOverheat);
            _primaryWarningSent = false;
            if (PlayerProgression.Instance != null && PlayerProgression.Instance.DegradeHandLevelDueToOverheat()) OnHandDegradedDueToOverheat?.Invoke(true);
        }
        else
        {
            if (_isSecondaryInForcedCooldown) return;
            _isSecondaryInForcedCooldown = true;
            _secondaryForcedCooldownEndTime = Time.time + forcedCooldownDurationAfterOverheat;
            DynamicPlayerFeedManager.Instance?.UpdateOverheatCriticalCountdown(false, forcedCooldownDurationAfterOverheat);
            _secondaryWarningSent = false;
        }
        OnHandFullyOverheated?.Invoke(isPrimary); UpdateHandVisuals(isPrimary);
    }

    public void SetHandFiringState(bool isPrimary, bool isFiring)
    {
        if (isPrimary) { _isPrimaryHandFiringStreamInternal = isFiring; if (isFiring) _timeSinceLastShotPrimary = 0f; }
        else { _isSecondaryHandFiringStreamInternal = isFiring; if (isFiring) _timeSinceLastShotSecondary = 0f; }
        SyncInspectorValue(true); SyncInspectorValue(false);
    }

    public void AddHeatToHand(bool isPrimary, float heatAmount)
    {
        string handId = isPrimary ? "Primary" : "Secondary";
        float previousHeat;
        if (isPrimary)
        {
            if (_isPrimaryInForcedCooldown && heatAmount > 0) { Debug.Log($"DEBUG LOG: AddHeatToHand ({handId}) rejected: In forced cooldown."); return; }
            previousHeat = CurrentHeatPrimary; CurrentHeatPrimary += heatAmount; CurrentHeatPrimary = Mathf.Clamp(CurrentHeatPrimary, 0, maxHeat);
            if (heatAmount > 0) _timeSinceLastShotPrimary = 0f;
            HandleHeatWarning(true);
            OnHeatChangedForHUD?.Invoke(true, CurrentHeatPrimary, maxHeat); UpdateHandVisuals(true);
            if (CurrentHeatPrimary >= maxHeat && previousHeat < maxHeat && !_isPrimaryInForcedCooldown) TriggerFullOverheatConsequences(true);
        }
        else
        {
            if (_isSecondaryInForcedCooldown && heatAmount > 0) { Debug.Log($"DEBUG LOG: AddHeatToHand ({handId}) rejected: In forced cooldown."); return; }
            previousHeat = CurrentHeatSecondary; CurrentHeatSecondary += heatAmount; CurrentHeatSecondary = Mathf.Clamp(CurrentHeatSecondary, 0, maxHeat);
            if (heatAmount > 0) _timeSinceLastShotSecondary = 0f;
            HandleHeatWarning(false);
            OnHeatChangedForHUD?.Invoke(false, CurrentHeatSecondary, maxHeat); UpdateHandVisuals(false);
            if (CurrentHeatSecondary >= maxHeat && previousHeat < maxHeat && !_isSecondaryInForcedCooldown) TriggerFullOverheatConsequences(false);
        }
        SyncInspectorValue(isPrimary);
    }

    public bool CanFire(bool isPrimary, bool forShotgun = false)
    {
        float currentActualHeat = isPrimary ? CurrentHeatPrimary : CurrentHeatSecondary;
        bool isInForcedCD = isPrimary ? _isPrimaryInForcedCooldown : _isSecondaryInForcedCooldown;

        if (maxHeat <= 0) return false;
        
        // Play blocked sound from the specific hand when trying to fire while overheated
        if (isInForcedCD)
        {
            PlayOverheatSound(soundEvents?.handOverheatedBlocked, isPrimary);
            return false;
        }

        bool can;
        if (forShotgun)
        {
            can = (currentActualHeat + shotgunHeatCost <= maxHeat);
        }
        else
        {
            can = (currentActualHeat < maxHeat);
        }
        return can;
    }

    public bool IsHandOverheated(bool isPrimary)
    {
        return isPrimary ? _isPrimaryInForcedCooldown : _isSecondaryInForcedCooldown;
    }

    public void ResetHandHeat(bool isPrimary, bool notifyHudAndVisuals = true)
    {
        bool wasInCooldown;
        DynamicPlayerFeedManager.Instance?.ClearOverheatCriticalMessage(isPrimary);

        if (isPrimary)
        {
            wasInCooldown = _isPrimaryInForcedCooldown; CurrentHeatPrimary = 0f; _timeSinceLastShotPrimary = heatCooldownDelay; _isPrimaryInForcedCooldown = false; _primaryForcedCooldownEndTime = Time.time;
            _primaryWarningSent = false;
            _primary50PercentSoundPlayed = false; // Reset sound flags
            _primary70PercentSoundPlayed = false;
            if (notifyHudAndVisuals) { if (wasInCooldown) OnHandRecoveredFromForcedCooldown?.Invoke(true); OnHeatChangedForHUD?.Invoke(true, CurrentHeatPrimary, maxHeat); UpdateHandVisuals(true); }
        }
        else
        {
            wasInCooldown = _isSecondaryInForcedCooldown; CurrentHeatSecondary = 0f; _timeSinceLastShotSecondary = heatCooldownDelay; _isSecondaryInForcedCooldown = false; _secondaryForcedCooldownEndTime = Time.time;
            _secondaryWarningSent = false;
            _secondary50PercentSoundPlayed = false; // Reset sound flags
            _secondary70PercentSoundPlayed = false;
            if (notifyHudAndVisuals) { if (wasInCooldown) OnHandRecoveredFromForcedCooldown?.Invoke(false); OnHeatChangedForHUD?.Invoke(false, CurrentHeatSecondary, maxHeat); UpdateHandVisuals(false); }
        }
        SyncInspectorValue(isPrimary);
    }

    private void UpdateHandVisuals(bool isPrimary)
    {
        HandOverheatVisuals visuals = isPrimary ? ActivePrimaryHandVisuals : ActiveSecondaryHandVisuals;
        float currentHeat = isPrimary ? CurrentHeatPrimary : CurrentHeatSecondary;
        bool isInForcedCooldown = isPrimary ? _isPrimaryInForcedCooldown : _isSecondaryInForcedCooldown;
        float normalizedHeat = (maxHeat > 0) ? currentHeat / maxHeat : 0;
        bool isOverheated = isInForcedCooldown || (currentHeat >= maxHeat);
        
        // Debug.Log($"PlayerOverheatManager: UpdateHandVisuals({(isPrimary ? "Primary" : "Secondary")}) - Heat: {currentHeat:F1}/{maxHeat:F1} ({normalizedHeat:F3}), Overheated: {isOverheated}, Visuals: {visuals?.gameObject.name ?? "NULL"}", this);
        
        if (visuals != null) { visuals.UpdateVisuals(normalizedHeat, isOverheated); }
        else { Debug.LogWarning($"PlayerOverheatManager: No visuals assigned for {(isPrimary ? "Primary" : "Secondary")} hand!", this); }
    }

    public bool CanAffordAOE() { return CanFire(true, false) && (CurrentHeatPrimary + heatCostForAOE <= maxHeat); }
    public void ApplyHeatForAOE() { AddHeatToHand(true, heatCostForAOE); }
    public void ApplyHeatReductionFromGemCollection(bool collectedByPrimaryHand)
    {
        if (heatReductionPerGemCollected > 0) AddHeatToHand(collectedByPrimaryHand, -heatReductionPerGemCollected);
    }
    
    /// <summary>
    /// Directly set heat level for a hand (bypasses normal heat system checks)
    /// Used by InstantCooldown powerup for smooth heat draining effect
    /// </summary>
    public void SetHeatDirectly(bool isPrimary, float heatValue)
    {
        heatValue = Mathf.Clamp(heatValue, 0, maxHeat);
        
        if (isPrimary)
        {
            CurrentHeatPrimary = heatValue;
            _currentHeatPrimary_Inspector = heatValue;
            _prevInspectorHeatPrimary = heatValue;
        }
        else
        {
            CurrentHeatSecondary = heatValue;
            _currentHeatSecondary_Inspector = heatValue;
            _prevInspectorHeatSecondary = heatValue;
        }
        
        // Update HUD and visuals
        OnHeatChangedForHUD?.Invoke(isPrimary, heatValue, maxHeat);
        UpdateHandVisuals(isPrimary);
        
        // Handle heat warnings
        HandleHeatWarning(isPrimary);
    }
    
    /// <summary>
    /// Activate instant cooldown particle effects on both hands with duration
    /// </summary>
    public void ActivateInstantCooldownEffects(float duration = 0f)
    {
        if (ActivePrimaryHandVisuals != null)
        {
            ActivePrimaryHandVisuals.ActivateInstantCooldownEffect();
        }
        else
        {
            Debug.LogWarning("PlayerOverheatManager: No primary hand visuals found for instant cooldown effect!", this);
        }
        
        if (ActiveSecondaryHandVisuals != null)
        {
            ActiveSecondaryHandVisuals.ActivateInstantCooldownEffect();
        }
        else
        {
            Debug.LogWarning("PlayerOverheatManager: No secondary hand visuals found for instant cooldown effect!", this);
        }
        
        Debug.Log("PlayerOverheatManager: Instant cooldown effects activated on both hands", this);
        
        // Auto-deactivate after duration if specified
        if (duration > 0f)
        {
            StartCoroutine(DeactivateInstantCooldownAfterDuration(duration));
        }
    }
    
    private System.Collections.IEnumerator DeactivateInstantCooldownAfterDuration(float duration)
    {
        yield return new WaitForSeconds(duration);
        DeactivateInstantCooldownEffects();
    }
    
    /// <summary>
    /// Deactivate instant cooldown particle effects on both hands
    /// </summary>
    public void DeactivateInstantCooldownEffects()
    {
        if (ActivePrimaryHandVisuals != null)
        {
            ActivePrimaryHandVisuals.DeactivateInstantCooldownEffect();
        }
        
        if (ActiveSecondaryHandVisuals != null)
        {
            ActiveSecondaryHandVisuals.DeactivateInstantCooldownEffect();
        }
        
        Debug.Log("PlayerOverheatManager: Instant cooldown effects deactivated on both hands", this);
    }
    
    /// <summary>
    /// Activate double damage particle effects on both hands with duration
    /// </summary>
    public void ActivateDoubleDamageEffects(float duration = 0f)
    {
        if (ActivePrimaryHandVisuals != null)
        {
            ActivePrimaryHandVisuals.ActivateDoubleDamageEffect();
        }
        else
        {
            Debug.LogWarning("PlayerOverheatManager: No primary hand visuals found for double damage effect!", this);
        }
        
        if (ActiveSecondaryHandVisuals != null)
        {
            ActiveSecondaryHandVisuals.ActivateDoubleDamageEffect();
        }
        else
        {
            Debug.LogWarning("PlayerOverheatManager: No secondary hand visuals found for double damage effect!", this);
        }
        
        Debug.Log("PlayerOverheatManager: Double damage effects activated on both hands", this);
        
        // Auto-deactivate after duration if specified
        if (duration > 0f)
        {
            StartCoroutine(DeactivateDoubleDamageAfterDuration(duration));
        }
    }
    
    private System.Collections.IEnumerator DeactivateDoubleDamageAfterDuration(float duration)
    {
        yield return new WaitForSeconds(duration);
        DeactivateDoubleDamageEffects();
    }
    
    /// <summary>
    /// Deactivate double damage particle effects on both hands
    /// </summary>
    public void DeactivateDoubleDamageEffects()
    {
        if (ActivePrimaryHandVisuals != null)
        {
            ActivePrimaryHandVisuals.DeactivateDoubleDamageEffect();
        }
        
        if (ActiveSecondaryHandVisuals != null)
        {
            ActiveSecondaryHandVisuals.DeactivateDoubleDamageEffect();
        }
        
        Debug.Log("PlayerOverheatManager: Double damage effects deactivated on both hands", this);
    }
    
    /// <summary>
    /// Activate max hand upgrade particle effects on both hands
    /// </summary>
    public void ActivateMaxHandUpgradeEffects()
    {
        if (ActivePrimaryHandVisuals != null)
        {
            ActivePrimaryHandVisuals.ActivateMaxHandUpgradeEffect();
        }
        else
        {
            Debug.LogWarning("PlayerOverheatManager: No primary hand visuals found for max hand upgrade effect!", this);
        }
        
        if (ActiveSecondaryHandVisuals != null)
        {
            ActiveSecondaryHandVisuals.ActivateMaxHandUpgradeEffect();
        }
        else
        {
            Debug.LogWarning("PlayerOverheatManager: No secondary hand visuals found for max hand upgrade effect!", this);
        }
        
        Debug.Log("PlayerOverheatManager: Max hand upgrade effects activated on both hands", this);
    }
    
    /// <summary>
    /// Deactivate max hand upgrade particle effects on both hands
    /// </summary>
    public void DeactivateMaxHandUpgradeEffects()
    {
        if (ActivePrimaryHandVisuals != null)
        {
            ActivePrimaryHandVisuals.DeactivateMaxHandUpgradeEffect();
        }
        
        if (ActiveSecondaryHandVisuals != null)
        {
            ActiveSecondaryHandVisuals.DeactivateMaxHandUpgradeEffect();
        }
        
        Debug.Log("PlayerOverheatManager: Max hand upgrade effects deactivated on both hands", this);
    }
    
    /// <summary>
    /// Play an overheat sound through the hand's Transform so it follows the hand
    /// Uses PlayAttached() to match stream/shotgun sound system
    /// </summary>
    private void PlayOverheatSound(SoundEvent soundEvent, bool isPrimary)
    {
        if (soundEvent == null || soundEvent.clip == null) return;
        
        // Get the hand's transform from the visuals (which we already have!)
        HandOverheatVisuals handVisuals = isPrimary ? ActivePrimaryHandVisuals : ActiveSecondaryHandVisuals;
        
        if (handVisuals != null)
        {
            // Play attached to the hand's transform so sound follows the hand
            soundEvent.PlayAttached(handVisuals.transform, 1f);
        }
        else
        {
            // Fallback to 2D if no hand visuals found
            soundEvent.Play2D();
        }
    }
}