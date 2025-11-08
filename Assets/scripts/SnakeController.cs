// --- SnakeController.cs ---
using UnityEngine;
using System.Collections.Generic;

public class SnakeController : MonoBehaviour, IDamageable
{
    public enum SnakeState { Idle, Chasing, Enraged, Fleeing }

    [Header("State & Target")]
    public SnakeState currentState = SnakeState.Idle;
    private Transform _playerTarget;

    [Header("Health")]
    public float maxHealth = 10000f;
    private float _currentHealth;

    [Header("Movement Stats")]
    public float idleSpeed = 300f;
    public float chaseSpeed = 5000f;
    public float enragedSpeed = 8000f;
    public float turnSpeed = 8f;
    public float maxVelocity = 10000f;
    public float drag = 0.5f;

    [Header("Attack Behavior")]
    [Tooltip("How far ahead of the player the snake should aim.")]
    public float predictionAmount = 1.5f;
    [Tooltip("How strongly the snake wiggles.")]
    public float oscillationStrength = 100f;
    [Tooltip("How fast the snake wiggles.")]
    public float oscillationSpeed = 1f;
    [Tooltip("Use world space oscillation (true) or local space (false).")]
    public bool useWorldSpaceOscillation = true;
    private float _timeToSwitchOscillation = 5f;
    private Vector3 _oscillationAxis;

    [Header("Obstacle Avoidance")]
    public LayerMask platformLayer;
    public float avoidanceRayDistance = 500f;
    public float avoidanceStrength = 1000f;

    [Header("Auto-Engage Settings")]
    [Tooltip("Auto-engage player on start (for testing).")]
    public bool autoEngageOnStart = true;
    public float playerDetectionRange = 50000f;

    [Header("Joint Configuration")]
    [Tooltip("Auto-create follow scripts on segments (BETTER than joints!).")]
    public bool autoCreateFollowers = true;
    [Tooltip("Distance between segments.")]
    public float segmentSpacing = 150f;
    [Tooltip("How fast segments follow (higher = tighter).")]
    public float followSpeed = 10f;
    [Tooltip("How fast segments rotate (higher = snappier).")]
    public float followRotationSpeed = 5f;
    [Tooltip("Mass of each body segment.")]
    public float segmentMass = 80f;

    [Header("Internal References")]
    public List<SnakeSegment> segments = new List<SnakeSegment>();
    private List<Gem> _gems = new List<Gem>();
    private Rigidbody _headRb;
    private int _totalGems;
    private bool _isEnraged = false;
    private Vector3 _targetDirection;

    void Awake()
    {
        _headRb = GetComponent<Rigidbody>();
        if (_headRb == null)
        {
            Debug.LogWarning("SnakeController: No Rigidbody found, adding one automatically!");
            _headRb = gameObject.AddComponent<Rigidbody>();
        }

        // Configure rigidbody for smooth movement
        _headRb.linearDamping = drag;
        _headRb.angularDamping = 0.5f;
        _headRb.useGravity = false; // Flying snake
        _headRb.constraints = RigidbodyConstraints.None;

        // Initialize health
        _currentHealth = maxHealth;

        FindAndRegisterSegmentsAndGems();
        SwitchOscillationAxis();
    }

    void Start()
    {
        // Tell all gems that this snake is their controller
        foreach (Gem gem in _gems)
        {
            if (gem != null)
            {
                gem.SetSnakeController(this);
            }
        }

        Debug.Log($"🐍 Snake Start - State: {currentState}, AutoEngage: {autoEngageOnStart}");

        // Auto-engage for testing
        if (autoEngageOnStart)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                EngagePlayer(player.transform);
                Debug.Log($"✅ Snake auto-engaged player '{player.name}'!");
            }
            else
            {
                Debug.LogWarning("⚠️ Snake couldn't find player with 'Player' tag!");
                Debug.LogWarning("Make sure your player GameObject is tagged as 'Player'!");
            }
        }
        else
        {
            Debug.LogWarning("🔴 Auto-engage is OFF - snake will stay idle until EngagePlayer() is called!");
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
        // Update oscillation timer
        _timeToSwitchOscillation -= Time.deltaTime;
        if (_timeToSwitchOscillation <= 0)
        {
            SwitchOscillationAxis();
        }

        // FORCE CHASE IF WE HAVE A PLAYER TARGET
        if (_playerTarget != null && currentState == SnakeState.Idle)
        {
            currentState = SnakeState.Chasing;
            Debug.Log("🐍 FORCING CHASE STATE!");
        }

        // Debug current state
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"🐍 State: {currentState}, HasPlayer: {_playerTarget != null}, Distance: {(_playerTarget != null ? Vector3.Distance(transform.position, _playerTarget.position) : 0f):F0}");
        }

        // Calculate target direction in Update (read-only operations)
        switch (currentState)
        {
            case SnakeState.Idle:
                HandleIdleMovement();
                break;
            case SnakeState.Chasing:
            case SnakeState.Enraged:
                CalculateChaseDirection();
                break;
            case SnakeState.Fleeing:
                CalculateFleeDirection();
                break;
        }
    }

    void FixedUpdate()
    {
        if (_headRb == null) return;

        // Apply movement forces based on state
        switch (currentState)
        {
            case SnakeState.Idle:
                ApplyIdleForces();
                break;
            case SnakeState.Chasing:
            case SnakeState.Enraged:
                ApplyChaseForces();
                break;
            case SnakeState.Fleeing:
                ApplyFleeForces();
                break;
        }

        // Clamp velocity to prevent infinite acceleration
        if (_headRb.linearVelocity.magnitude > maxVelocity)
        {
            _headRb.linearVelocity = _headRb.linearVelocity.normalized * maxVelocity;
        }
    }
    
    // ===== IDLE MOVEMENT =====
    void HandleIdleMovement()
    {
        // Gentle circular/wandering pattern
        _targetDirection = (_headRb.rotation * Vector3.forward).normalized;
        // Add some random drift
        _targetDirection += new Vector3(
            Mathf.Sin(Time.time * 0.3f) * 0.5f,
            Mathf.Cos(Time.time * 0.4f) * 0.5f,
            0
        );
        _targetDirection.Normalize();
    }

    void ApplyIdleForces()
    {
        if (_headRb == null) return;

        // Slow wandering
        Quaternion targetRotation = Quaternion.LookRotation(_targetDirection);
        _headRb.rotation = Quaternion.Slerp(_headRb.rotation, targetRotation, Time.fixedDeltaTime * turnSpeed * 0.5f);
        _headRb.AddForce(transform.forward * idleSpeed, ForceMode.Acceleration);

        // Add gentle oscillation
        float sineWave = Mathf.Sin(Time.time * oscillationSpeed * 0.5f) * (oscillationStrength * 0.3f);
        Vector3 oscillationDir = useWorldSpaceOscillation ? _oscillationAxis : (transform.rotation * _oscillationAxis);
        _headRb.AddForce(oscillationDir * sineWave, ForceMode.Acceleration);
    }

    // ===== CHASE MOVEMENT =====
    void CalculateChaseDirection()
    {
        if (_playerTarget == null)
        {
            _targetDirection = transform.forward;
            return;
        }

        // 1. Predict Player's Future Position
        Rigidbody playerRb = _playerTarget.GetComponent<Rigidbody>();
        Vector3 predictedPosition = _playerTarget.position;
        if (playerRb != null)
        {
            predictedPosition += playerRb.linearVelocity * predictionAmount;
        }

        // 2. Calculate Direction to Target
        _targetDirection = (predictedPosition - _headRb.position).normalized;

            // 3. Obstacle Avoidance - only if we're actually moving
            if (_headRb.linearVelocity.magnitude > 0.1f)
            {
                RaycastHit hit;
                Vector3 checkDirection = _headRb.linearVelocity.normalized;
                
                if (Physics.SphereCast(_headRb.position, 100f, checkDirection, out hit, avoidanceRayDistance, platformLayer))
                {
                    // Blend avoidance direction with target direction
                    Vector3 avoidanceDir = Vector3.Reflect(checkDirection, hit.normal).normalized;
                    _targetDirection = Vector3.Lerp(_targetDirection, avoidanceDir, 0.7f).normalized;
                    
                    Debug.DrawRay(hit.point, hit.normal * 200f, Color.red, 0.1f);
                }
            }
        }    void ApplyChaseForces()
    {
        if (_headRb == null || _playerTarget == null) return;

        // Calculate direction to player EVERY frame
        Vector3 directionToPlayer = (_playerTarget.position - _headRb.position).normalized;

        // Determine current speed based on state
        float currentSpeed = _isEnraged ? enragedSpeed : chaseSpeed;

        // FORCE rotation to look at player - MUCH FASTER TURNING
        if (directionToPlayer.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            _headRb.MoveRotation(Quaternion.Slerp(_headRb.rotation, targetRotation, Time.fixedDeltaTime * 20f));
        }

        // MASSIVE FORCE directly toward player
        _headRb.AddForce(directionToPlayer * currentSpeed, ForceMode.Acceleration);

        // Clamp velocity
        if (_headRb.linearVelocity.magnitude > maxVelocity)
        {
            _headRb.linearVelocity = _headRb.linearVelocity.normalized * maxVelocity;
        }

        // Debug visualization
        Debug.DrawRay(_headRb.position, directionToPlayer * 1000f, Color.green, 0.1f);
        Debug.DrawRay(_headRb.position, _headRb.linearVelocity.normalized * 800f, Color.red, 0.1f);
    }

    // ===== FLEE MOVEMENT =====
    void CalculateFleeDirection()
    {
        // Fly away from current position
        _targetDirection = transform.forward;
    }

    void ApplyFleeForces()
    {
        if (_headRb == null) return;

        // Massive speed boost, straight ahead
        _headRb.AddForce(transform.forward * 150f, ForceMode.Acceleration);
    }

    private void SwitchOscillationAxis()
    {
        if (useWorldSpaceOscillation)
        {
            // World space - use absolute directions
            _oscillationAxis = (Random.value > 0.5f) ? Vector3.right : Vector3.up;
        }
        else
        {
            // Local space - relative to snake
            _oscillationAxis = (Random.value > 0.5f) ? Vector3.right : Vector3.up;
        }
        
        _timeToSwitchOscillation = Random.Range(4f, 8f);
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

    // IDamageable implementation
    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
    {
        _currentHealth -= amount;
        Debug.Log($"🐍 Snake took {amount} damage! Health: {_currentHealth}/{maxHealth}");

        // Visual feedback - flash or something
        // TODO: Add hit effect

        if (_currentHealth <= 0)
        {
            Die();
        }
        else if (_currentHealth <= maxHealth * 0.5f && !_isEnraged)
        {
            _isEnraged = true;
            currentState = SnakeState.Enraged;
            Debug.Log("🐍 SNAKE ENRAGED!");
        }
    }

    private void Die()
    {
        currentState = SnakeState.Fleeing;
        Debug.Log("🐍 SNAKE DEFEATED! Fleeing!");
        
        // Make all segments non-lethal
        foreach (var seg in segments) 
        { 
            if (seg != null) seg.isLethal = false; 
        }
        
        // Destroy after fleeing
        Destroy(transform.parent != null ? transform.parent.gameObject : gameObject, 10f);
    }

    // ===== EDITOR TESTING HELPERS =====
    [ContextMenu("Test: Engage Player")]
    private void TestEngagePlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            EngagePlayer(player.transform);
        }
        else
        {
            Debug.LogError("No player found with 'Player' tag!");
        }
    }

    [ContextMenu("Test: Enrage Snake")]
    private void TestEnrage()
    {
        _isEnraged = true;
        currentState = SnakeState.Enraged;
        Debug.Log("Snake manually enraged!");
    }

    [ContextMenu("Test: Make Snake Flee")]
    private void TestFlee()
    {
        currentState = SnakeState.Fleeing;
        foreach (var seg in segments) { seg.isLethal = false; }
        Debug.Log("Snake fleeing!");
    }

    [ContextMenu("Setup Snake (Auto-Configure)")]
    private void AutoSetupSnake()
    {
        // Ensure we have a rigidbody
        if (_headRb == null)
        {
            _headRb = GetComponent<Rigidbody>();
            if (_headRb == null)
            {
                _headRb = gameObject.AddComponent<Rigidbody>();
            }
        }

        // Configure rigidbody
        _headRb.linearDamping = drag;
        _headRb.angularDamping = 2f;
        _headRb.useGravity = false;
        _headRb.mass = 100f;

        // Ensure this head has a SnakeSegment
        if (GetComponent<SnakeSegment>() == null)
        {
            gameObject.AddComponent<SnakeSegment>();
            Debug.Log("Added SnakeSegment to head!");
        }

        // Find or create parent
        if (transform.parent == null)
        {
            GameObject snakeParent = new GameObject("SnakeParent");
            transform.SetParent(snakeParent.transform);
            Debug.Log("Created SnakeParent for organization!");
        }

        // Re-scan for segments
        FindAndRegisterSegmentsAndGems();

        // Setup followers if enabled
        if (autoCreateFollowers)
        {
            SetupSnakeFollowers();
        }

        Debug.Log($"✅ Snake Setup Complete!\n" +
                  $"- Segments: {segments.Count}\n" +
                  $"- Gems: {_gems.Count}\n" +
                  $"- Auto-engage: {autoEngageOnStart}");
    }

    [ContextMenu("Create Test Snake Body")]
    private void CreateTestSnakeBody()
    {
        if (segments.Count == 0)
        {
            FindAndRegisterSegmentsAndGems();
        }

        // Create body segments if only head exists
        if (segments.Count <= 1)
        {
            int bodySegmentCount = 5; // Create 5 body segments
            
            // MASSIVE HEAD - 10X PLAYER SIZE! Make it horizontal (lying down)
            if (transform.localScale.magnitude < 500f)
            {
                // Create a sphere for the head instead of capsule
                MeshFilter mf = GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh.name.Contains("Capsule"))
                {
                    GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    mf.sharedMesh = sphere.GetComponent<MeshFilter>().sharedMesh;
                    Destroy(sphere);
                }
                transform.localScale = new Vector3(900f, 900f, 900f); // HUGE SPHERE HEAD!
            }

            for (int i = 0; i < bodySegmentCount; i++)
            {
                GameObject bodySegment = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bodySegment.name = $"SnakeBody_{i + 1}";
                bodySegment.transform.SetParent(transform.parent);
                
                // Spawn behind the head in a straight line
                float offset = segmentSpacing * (i + 1);
                bodySegment.transform.position = transform.position - (transform.forward * offset);
                bodySegment.transform.rotation = transform.rotation;
                
                // MASSIVE SPHERES - Get smaller toward tail
                float scaleMultiplier = 1.0f - (i * 0.1f);
                float size = 850f * scaleMultiplier;
                bodySegment.transform.localScale = new Vector3(size, size, size);

                // Add components
                Rigidbody rb = bodySegment.GetComponent<Rigidbody>();
                if (rb == null) rb = bodySegment.AddComponent<Rigidbody>();
                rb.mass = segmentMass * scaleMultiplier;
                rb.linearDamping = drag;
                rb.angularDamping = 2f;
                rb.useGravity = false;

                SnakeSegment segment = bodySegment.AddComponent<SnakeSegment>();
                segments.Add(segment);
            }

            Debug.Log($"Created {bodySegmentCount} SPHERICAL BOSS SNAKE segments!");
        }

        // Setup followers
        if (autoCreateFollowers)
        {
            SetupSnakeFollowers();
        }
    }

    private void SetupSnakeFollowers()
    {
        if (segments.Count <= 1)
        {
            Debug.LogWarning("Need at least 2 segments to create followers!");
            return;
        }

        int followersCreated = 0;

        // Start from index 1 (skip head)
        for (int i = 1; i < segments.Count; i++)
        {
            SnakeSegment currentSegment = segments[i];
            SnakeSegment previousSegment = segments[i - 1];

            if (currentSegment == null || previousSegment == null) continue;

            // Remove old follower to avoid duplicates
            SnakeSegmentFollower oldFollower = currentSegment.GetComponent<SnakeSegmentFollower>();
            if (oldFollower != null)
            {
#if UNITY_EDITOR
                DestroyImmediate(oldFollower);
#else
                Destroy(oldFollower);
#endif
            }

            // Add follower script
            SnakeSegmentFollower follower = currentSegment.gameObject.AddComponent<SnakeSegmentFollower>();
            follower.targetSegment = previousSegment.transform;
            follower.followDistance = segmentSpacing;
            follower.moveSpeed = followSpeed;
            follower.rotationSpeed = followRotationSpeed;
            follower.usePhysics = true; // Use physics for smooth movement

            followersCreated++;
        }

        Debug.Log($"✅ Created {followersCreated} SnakeSegmentFollowers - NO JOINTS NEEDED!");
    }

    void OnDrawGizmosSelected()
    {
        if (_headRb != null && _playerTarget != null)
        {
            // Draw line to player
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, _playerTarget.position);

            // Draw detection range
            Gizmos.color = new Color(1, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, playerDetectionRange);

            // Draw avoidance range
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Gizmos.DrawRay(transform.position, transform.forward * avoidanceRayDistance);
        }

        // Draw current state
        if (_headRb != null)
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * 200f, 
                $"State: {currentState}\n" +
                $"Speed: {_headRb.linearVelocity.magnitude:F1}\n" +
                $"Gems: {_totalGems}");
        }
    }
}