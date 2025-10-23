// --- PlayerInputHandler.cs (COMPLETE - Tap vs Hold logic remains, subscribers decide action) ---
using UnityEngine;
using System;

public class PlayerInputHandler : MonoBehaviour
{
    public static PlayerInputHandler Instance { get; private set; }

    [Header("Shooting Input Settings")]
    [Tooltip("Duration in seconds to hold mouse button to activate beam instead of shotgun.")]
    public float beamActivationHoldDuration = 0.35f;

    [Header("Gem Collection Settings")]
    [Tooltip("Maximum time interval between clicks to register as a double-click for gem collection.")]
    public float doubleClickMaxInterval = 0.3f;

    [Header("Middle Mouse Button (MMB) Settings")]
    [Tooltip("Enable middle mouse button input processing (disable if using for trick jump system)")]
    public bool enableMiddleMouseInput = false; // DISABLED by default for trick jump system
    [Tooltip("Max duration for an MMB press to be considered a TAP (for AOE PowerUp). Longer is a HOLD (for Homing Daggers PowerUp).")]
    public float middleMouseButtonTapMaxDuration = 0.45f;

    // Primary (LMB) Actions
    public event Action OnPrimaryTapAction;
    public event Action OnPrimaryHoldStartedAction;
    public event Action OnPrimaryHoldPerformedAction;
    public event Action OnPrimaryHoldEndedAction;
    public event Action OnPrimaryDoubleClickGemCollectAction;

    // Secondary (RMB) Actions
    public event Action OnSecondaryTapAction;
    public event Action OnSecondaryHoldStartedAction;
    public event Action OnSecondaryHoldPerformedAction;
    public event Action OnSecondaryHoldEndedAction;
    public event Action OnSecondaryDoubleClickGemCollectAction;

    // Middle Mouse Button (MMB) Actions
    public event Action OnMiddleMouseTapAction;
    public event Action OnMiddleMouseHoldStartedAction;
    public event Action OnMiddleMouseHoldEndedAction;

    // LMB State
    private float _lmbDownTime = -1f;
    private bool _isLMBBeamActive = false;
    private float _lastLMBClickTime = -10f;

    // RMB State
    private float _rmbDownTime = -1f;
    private bool _isRMBBeamActive = false;
    private float _lastRMBClickTime = -10f;

    // MMB State
    private float _mmbDownTime = -1f;
    private bool _isMMBHoldActiveForHoming = false; // True if MMB is held long enough for homing (power-up context)

    // Public property to let other systems (like PSO) know if MMB is currently considered held for homing
    public bool IsMiddleMouseEffectivelyHeldForHoming { get; private set; } = false;


    void Awake()
    {
        if (Instance == null) 
        {
            Instance = this;
            Debug.Log("[INPUT] PlayerInputHandler Instance created and assigned.");
        }
        else if (Instance != this) 
        { 
            Debug.Log("[INPUT] Duplicate PlayerInputHandler destroyed.");
            Destroy(gameObject); 
            return; 
        }
    }

    void Update()
    {
        bool lmbDown = Input.GetMouseButtonDown(0);
        bool lmbHeld = Input.GetMouseButton(0);
        bool lmbUp = Input.GetMouseButtonUp(0);

        bool rmbDown = Input.GetMouseButtonDown(1);
        bool rmbHeld = Input.GetMouseButton(1);
        bool rmbUp = Input.GetMouseButtonUp(1);

        bool mmbDown = Input.GetMouseButtonDown(2);
        bool mmbHeld = Input.GetMouseButton(2);
        bool mmbUp = Input.GetMouseButtonUp(2);

        // Debug any mouse input at all
        // if (lmbDown || rmbDown || mmbDown)
        // {
        //     Debug.Log($"[INPUT] Mouse input detected - LMB: {lmbDown}, RMB: {rmbDown}, MMB: {mmbDown}");
        // }

        ProcessPrimaryInput(lmbDown, lmbHeld, lmbUp);
        ProcessSecondaryInput(rmbDown, rmbHeld, rmbUp);
        
        // Only process middle mouse if enabled (disabled by default for trick jump system)
        if (enableMiddleMouseInput)
        {
            ProcessMiddleMouseInput(mmbDown, mmbHeld, mmbUp);
        }
    }

    private void ProcessPrimaryInput(bool lmbDown, bool lmbHeld, bool lmbUp)
    {
        if (lmbDown)
        {
            if (Time.time - _lastLMBClickTime < doubleClickMaxInterval)
            {
                Debug.Log($"[INPUT] PRIMARY Double-click detected! Invoking gem collection. Subscribers: {OnPrimaryDoubleClickGemCollectAction?.GetInvocationList()?.Length ?? 0}");
                OnPrimaryDoubleClickGemCollectAction?.Invoke();
            }
            else
            {
                // Debug.Log($"[INPUT] PRIMARY Single click (time since last: {Time.time - _lastLMBClickTime:F3}s, threshold: {doubleClickMaxInterval:F3}s)");
            }
            _lastLMBClickTime = Time.time;
            _lmbDownTime = Time.time;
            _isLMBBeamActive = false;
        }

        if (lmbHeld)
        {
            if (_lmbDownTime >= 0) // Button was pressed down this session
            {
                if (!_isLMBBeamActive && (Time.time - _lmbDownTime > beamActivationHoldDuration))
                {
                    _isLMBBeamActive = true;
                    OnPrimaryHoldStartedAction?.Invoke();
                }
                if (_isLMBBeamActive) // If beam is active, continuously invoke performed action
                {
                    OnPrimaryHoldPerformedAction?.Invoke();
                }
            }
        }

        if (lmbUp)
        {
            if (_lmbDownTime >= 0) // Button was released after being pressed
            {
                if (_isLMBBeamActive)
                {
                    OnPrimaryHoldEndedAction?.Invoke();
                }
                else // Was not held long enough for beam, so it's a tap
                {
                    OnPrimaryTapAction?.Invoke();
                }
            }
            _lmbDownTime = -1f; // Reset down time
            _isLMBBeamActive = false; // Reset beam active state
        }
    }

    private void ProcessSecondaryInput(bool rmbDown, bool rmbHeld, bool rmbUp)
    {
        if (rmbDown)
        {
            if (Time.time - _lastRMBClickTime < doubleClickMaxInterval)
            {
                Debug.Log($"[INPUT] SECONDARY Double-click detected! Invoking gem collection. Subscribers: {OnSecondaryDoubleClickGemCollectAction?.GetInvocationList()?.Length ?? 0}");
                OnSecondaryDoubleClickGemCollectAction?.Invoke();
            }
            else
            {
                // Debug.Log($"[INPUT] SECONDARY Single click (time since last: {Time.time - _lastRMBClickTime:F3}s, threshold: {doubleClickMaxInterval:F3}s)");
            }
            _lastRMBClickTime = Time.time;
            _rmbDownTime = Time.time;
            _isRMBBeamActive = false;
        }

        if (rmbHeld)
        {
            if (_rmbDownTime >= 0)
            {
                if (!_isRMBBeamActive && (Time.time - _rmbDownTime > beamActivationHoldDuration))
                {
                    _isRMBBeamActive = true;
                    OnSecondaryHoldStartedAction?.Invoke();
                }
                if (_isRMBBeamActive)
                {
                    OnSecondaryHoldPerformedAction?.Invoke();
                }
            }
        }

        if (rmbUp)
        {
            if (_rmbDownTime >= 0)
            {
                if (_isRMBBeamActive)
                {
                    OnSecondaryHoldEndedAction?.Invoke();
                }
                else
                {
                    OnSecondaryTapAction?.Invoke();
                }
            }
            _rmbDownTime = -1f;
            _isRMBBeamActive = false;
        }
    }

    private void ProcessMiddleMouseInput(bool mmbDown, bool mmbHeld, bool mmbUp)
    {
        if (mmbDown)
        {
            _mmbDownTime = Time.time;
            _isMMBHoldActiveForHoming = false; // Reset on new press
            IsMiddleMouseEffectivelyHeldForHoming = false; // Reset public property
        }

        if (mmbHeld && _mmbDownTime >= 0) // Button is being held
        {
            if (!_isMMBHoldActiveForHoming && (Time.time - _mmbDownTime > middleMouseButtonTapMaxDuration))
            {
                // Hold duration for homing daggers (power-up context) has been met
                _isMMBHoldActiveForHoming = true;
                IsMiddleMouseEffectivelyHeldForHoming = true; // Update public property
                OnMiddleMouseHoldStartedAction?.Invoke(); // Signal start of MMB hold
            }
            // No continuous "HoldPerformed" for MMB in this setup, PSO handles continuous firing if needed.
        }

        if (mmbUp)
        {
            if (_mmbDownTime >= 0) // Was a valid press
            {
                if (_isMMBHoldActiveForHoming) // Was released after being held long enough
                {
                    OnMiddleMouseHoldEndedAction?.Invoke();
                }
                else if (Time.time - _mmbDownTime <= middleMouseButtonTapMaxDuration) // Was released quickly enough to be a tap
                {
                    OnMiddleMouseTapAction?.Invoke();
                }
                // If it was held longer than tap but less than hold start (unlikely with current logic but possible if tapMaxDuration is very small), nothing happens on release.
            }
            // Reset states
            _mmbDownTime = -1f;
            _isMMBHoldActiveForHoming = false;
            IsMiddleMouseEffectivelyHeldForHoming = false; // Reset public property
        }
    }
    
    // Method to check if primary fire (left mouse button) is held
    public bool IsPrimaryFireHeld()
    {
        return Input.GetMouseButton(0);
    }
    
    // Method to check if secondary fire (right mouse button) is held
    public bool IsSecondaryFireHeld()
    {
        return Input.GetMouseButton(1);
    }
}