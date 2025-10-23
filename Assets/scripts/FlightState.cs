// --- FlightState.cs (CORRECTED) ---
using UnityEngine;

public class FlightState : IPlayerState
{
    private PlayerMovementManager _manager;
    private CelestialDriftController _flightController;


    public void OnEnter(PlayerMovementManager manager)
    {
        _manager = manager;
        _flightController = manager.FlightController;

        // Flight mode is now permanent in CelestialDriftController 
        _manager.transform.SetParent(null, true);

        if (_manager.PendingLaunchVelocity.sqrMagnitude > 0.1f)
        {
            _manager.Rb.linearVelocity = _manager.PendingLaunchVelocity;
            _manager.ClearPendingLaunchVelocity();
        }
        
        Debug.Log("<color=cyan>STATE: Entered Flight State</color>");
    }

    public void OnExit()
    {
        Debug.Log("<color=cyan>STATE: Exiting Flight State</color>");
    }

    public void HandleInput()
    {
        // Bubble system removed – no special flight input required here.
    }
    
    public void Update()
    {
        // CRITICAL: Block flight input processing when in AAA mode
        AAAMovementIntegrator integrator = _manager.GetComponent<AAAMovementIntegrator>();
        bool isInAAAMode = (integrator != null && integrator.IsAAASystemActive());
        
        if (isInAAAMode)
        {
            // Don't process flight input when in AAA mode, but allow lock-on to continue working
            // Send zero input to flight controller to prevent conflicts
            _flightController.ManualUpdate(Vector3.zero, Vector3.zero, 0f, false);
            return;
        }
        
        // --- Input Gathering (Only when in TRUE flight mode) ---
        float h = Controls.HorizontalRaw();
        float v = Controls.VerticalRaw();
        float u = 0;
        if (Input.GetKey(Controls.UpThrustJump)) u = 1f;
        if (Input.GetKey(Controls.DownThrust)) u = -1f;

        float roll = 0;
        if (Input.GetKey(Controls.RollLeft)) roll = -1f;
        if (Input.GetKey(Controls.RollRight)) roll = 1f;
    
        bool boost = Input.GetKey(Controls.Boost);
    
        // DEBUG: Log inputs at source to identify corruption point
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        
        // Log flight inputs when in true flight mode
        if (Mathf.Abs(roll) > 0.1f || mouseX != 0 || mouseY != 0)
        {
            Debug.Log($"<color=yellow>[FLIGHT INPUT] MouseRaw: ({mouseX:F2}, {mouseY:F2}), RollRaw: {roll:F2}, Q: {Input.GetKey(Controls.RollLeft)}, E: {Input.GetKey(Controls.RollRight)}</color>");
        }
    
        // --- Send Input to the Engine ---
        // The CelestialDriftController receives the input (only when in flight mode)
        _flightController.ManualUpdate(
            new Vector3(h, u, v), 
            new Vector3(mouseX, mouseY, 0), 
            roll, 
            boost
        );
    }

    // HEROIC FIX: This is now the one and only FixedUpdate method.
    public void FixedUpdate()
    {
        // If AAA is active, skip flight physics entirely
        AAAMovementIntegrator integrator = _manager.GetComponent<AAAMovementIntegrator>();
        bool isInAAAMode = (integrator != null && integrator.IsAAASystemActive());
        if (isInAAAMode)
        {
            return;
        }

        // First, check if we need to follow a locked-on platform.
        // This synchronizes our reference frame BEFORE we apply our own thrust.
        if (_manager.IsLockedOn)
        {
            _manager.FollowPlatformPhysics();
        }

        // Second, tell our "engine" (CelestialDriftController) to apply thrust and other physics.
        // This thrust will now be relative to our (potentially moving) reference frame.
        _flightController.ManualFixedUpdate();
    }
}