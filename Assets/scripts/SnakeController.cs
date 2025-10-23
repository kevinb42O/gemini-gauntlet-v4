// --- SnakeController.cs ---
using UnityEngine;
using System.Collections.Generic;

public class SnakeController : MonoBehaviour
{
    public enum SnakeState { Idle, Chasing, Enraged, Fleeing }

    [Header("State & Target")]
    public SnakeState currentState = SnakeState.Idle;
    private Transform _playerTarget;

    [Header("Movement Stats")]
    public float idleSpeed = 10f;
    public float chaseSpeed = 25f;
    public float enragedSpeed = 40f;
    public float turnSpeed = 2f;

    [Header("Attack Behavior")]
    [Tooltip("How far ahead of the player the snake should aim.")]
    public float predictionAmount = 1.5f;
    [Tooltip("How strongly the snake wiggles.")]
    public float oscillationStrength = 10;
    [Tooltip("How fast the snake wiggles.")]
    public float oscillationSpeed = 1f;
    private float _timeToSwitchOscillation = 5f;
    private Vector3 _oscillationAxis;

    [Header("Obstacle Avoidance")]
    public LayerMask platformLayer;
    public float avoidanceRayDistance = 50f;
    public float avoidanceStrength = 100f;

    [Header("Internal References")]
    public List<SnakeSegment> segments = new List<SnakeSegment>();
    private List<Gem> _gems = new List<Gem>();
    private Rigidbody _headRb;
    private int _totalGems;
    private bool _isEnraged = false;

    void Awake()
    {
        _headRb = GetComponent<Rigidbody>();
        FindAndRegisterSegmentsAndGems();
        SwitchOscillationAxis();
    }

    void Start()
    {
        // Tell all gems that this snake is their controller
        foreach (Gem gem in _gems)
        {
            gem.SetSnakeController(this);
        }
    }

    private void FindAndRegisterSegmentsAndGems()
    {
        // The head is the first segment
        if (GetComponent<SnakeSegment>() != null)
        {
            segments.Add(GetComponent<SnakeSegment>());
        }

        // Find all other segments by searching for SnakeSegment components
        // Assumes snake is pre-assembled in the hierarchy under a single parent
        if (transform.parent != null)
        {
            segments.AddRange(transform.parent.GetComponentsInChildren<SnakeSegment>());
        } else {
             // Fallback if not parented
            segments.AddRange(FindObjectsByType<SnakeSegment>(FindObjectsSortMode.None));
        }


        foreach (var segment in segments)
        {
            Gem gem = segment.GetComponentInChildren<Gem>();
            if (gem != null)
            {
                _gems.Add(gem);
            }
        }
        _totalGems = _gems.Count;
        Debug.Log($"Snake found {_totalGems} gems.");
    }

    void Update()
    {
        switch (currentState)
        {
            case SnakeState.Idle:
                // Idle logic would go here (e.g., gentle wandering)
                // For now, it does nothing until engaged.
                break;
            case SnakeState.Chasing:
            case SnakeState.Enraged:
                HandleChaseMovement();
                break;
            case SnakeState.Fleeing:
                HandleFleeMovement();
                break;
        }

        // Randomly switch between horizontal and vertical swimming
        _timeToSwitchOscillation -= Time.deltaTime;
        if (_timeToSwitchOscillation <= 0)
        {
            SwitchOscillationAxis();
        }
    }

    void FixedUpdate()
    {
        // Move physics logic to FixedUpdate
        if (currentState == SnakeState.Chasing || currentState == SnakeState.Enraged)
        {
            ApplyMovementForces();
        }
    }
    
    private Vector3 _targetDirection; // Store target direction for FixedUpdate

    void HandleChaseMovement()
    {
        if (_playerTarget == null) return;

        // 1. Predict Player's Future Position
        Rigidbody playerRb = _playerTarget.GetComponent<Rigidbody>();
        Vector3 predictedPosition = _playerTarget.position;
        if (playerRb != null)
        {
            predictedPosition += playerRb.linearVelocity * predictionAmount;
        }

        // 2. Calculate Direction to Target
        _targetDirection = (predictedPosition - _headRb.position).normalized;

        // 3. Obstacle Avoidance
        RaycastHit hit;
        if (Physics.SphereCast(_headRb.position, 10f, _headRb.linearVelocity.normalized, out hit, avoidanceRayDistance, platformLayer))
        {
            // If a platform is ahead, add a force pushing away from it
            Vector3 avoidanceDir = Vector3.Reflect(transform.forward, hit.normal);
            _headRb.AddForce(avoidanceDir * avoidanceStrength);
        }
    }

    void ApplyMovementForces()
    {
        if (_playerTarget == null) return;

        // Determine current speed based on state
        float currentSpeed = (_isEnraged) ? enragedSpeed : chaseSpeed;

        // Rotate towards the target direction
        Quaternion targetRotation = Quaternion.LookRotation(_targetDirection);
        _headRb.rotation = Quaternion.Slerp(_headRb.rotation, targetRotation, Time.fixedDeltaTime * turnSpeed);

        // Apply forward force
        _headRb.AddForce(transform.forward * currentSpeed, ForceMode.Acceleration);

        // Apply oscillating "swimming" force
        float sineWave = Mathf.Sin(Time.time * oscillationSpeed) * oscillationStrength;
        _headRb.AddForce(_oscillationAxis * sineWave, ForceMode.Acceleration);
    }


    void HandleFleeMovement()
    {
        // Fly towards a point far, far away
        Vector3 fleePoint = transform.position + (transform.forward * 10000f);
        Vector3 fleeDirection = (fleePoint - _headRb.position).normalized;
        _headRb.AddForce(fleeDirection * 150f, ForceMode.Acceleration); // Massive speed boost
    }

    private void SwitchOscillationAxis()
    {
        // Randomly pick between horizontal (transform.right) and vertical (transform.up)
        _oscillationAxis = (Random.value > 0.5f) ? transform.right : transform.up;
        _timeToSwitchOscillation = Random.Range(4f, 8f); // How long to swim in one style
    }

    // Called by a gem when it is destroyed
    public void OnGemDestroyed()
    {
        _totalGems--;
        if (_totalGems <= 0)
        {
            currentState = SnakeState.Fleeing;
            Debug.Log("SNAKE DEFEATED! Fleeing!");
            // Make all segments non-lethal
            foreach (var seg in segments) { seg.isLethal = false; }
            Destroy(transform.parent.gameObject, 15f); // Destroy snake after 15s
            return;
        }

        // Check for enraged state
        if (!_isEnraged && _totalGems <= (_gems.Count / 2))
        {
            _isEnraged = true;
            currentState = SnakeState.Enraged;
            Debug.Log("SNAKE ENRAGED!");
        }
    }

    // Called by the UniverseTrigger to start the fight
    public void EngagePlayer(Transform player)
    {
        if (currentState == SnakeState.Idle)
        {
            _playerTarget = player;
            currentState = SnakeState.Chasing;
            Debug.Log("SNAKE ENGAGED!");
        }
    }
}