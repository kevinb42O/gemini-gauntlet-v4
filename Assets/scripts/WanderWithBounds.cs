// --- WanderWithinBounds.cs (Verified and Final) ---
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class WanderWithinBounds : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("How fast the object moves towards its destination.")]
    public float moveSpeed = 0.5f;
    [Tooltip("The minimum time to wait at a destination before choosing a new one.")]
    public float minWaitTime = 2.0f;
    [Tooltip("The maximum time to wait at a destination before choosing a new one.")]
    public float maxWaitTime = 5.0f;
    [Tooltip("How close the object needs to get to its destination to consider it 'arrived'.")]
    public float arrivalThreshold = 0.1f;

    [Header("Wander Area (Bounds)")]
    [Tooltip("The center of the wandering area. If null, uses the object's starting position.")]
    public Transform wanderAreaCenter;
    [Tooltip("The size (X and Z) of the rectangular wandering area.")]
    public Vector2 wanderAreaSize = new Vector2(10f, 10f);

    [Header("Charge-Up Animation")]
    [Tooltip("How many full 360-degree spins to perform during the charge-up animation.")]
    public int chargeUpSpins = 3;
    [Tooltip("How long the charge-up spin animation should take.")]
    public float chargeUpDuration = 1.0f;

    private Rigidbody _rb;
    private Vector3 _startPosition;
    private Vector3 _currentDestination;
    private Coroutine _movementCoroutine;
    private Coroutine _chargeUpCoroutine;
    private bool _isChargingUp = false;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = true;
        _rb.useGravity = false;
    }

    void OnEnable()
    {
        // When this component is enabled, start listening for the tower's broadcast.
        HybridTowerController.OnBeforeSpawnBurst += HandleBeforeSpawnBurst;
    }

    void OnDisable()
    {
        // When this component is disabled, stop listening to prevent errors.
        HybridTowerController.OnBeforeSpawnBurst -= HandleBeforeSpawnBurst;
    }

    void Start()
    {
        // If no specific center is defined, use this object's starting world position.
        _startPosition = wanderAreaCenter != null ? wanderAreaCenter.position : transform.position;
        StartWandering();
    }

    public void StartWandering()
    {
        if (_movementCoroutine != null) StopCoroutine(_movementCoroutine);
        _movementCoroutine = StartCoroutine(WanderRoutine());
    }

    public void StopWandering()
    {
        if (_movementCoroutine != null)
        {
            StopCoroutine(_movementCoroutine);
            _movementCoroutine = null;
        }
    }

    public void TriggerChargeUpAnimation()
    {
        if (_isChargingUp) return;

        if (_chargeUpCoroutine != null) StopCoroutine(_chargeUpCoroutine);
        _chargeUpCoroutine = StartCoroutine(ChargeUpSpinRoutine());
    }

    private void HandleBeforeSpawnBurst(HybridTowerController tower)
    {
        // Check if the event was broadcasted by the tower this script is attached to.
        if (tower != null && tower.gameObject == this.gameObject)
        {
            // If it's our tower, trigger the animation!
            TriggerChargeUpAnimation();
        }
    }

    private IEnumerator WanderRoutine()
    {
        while (true)
        {
            // Pause wandering if the charge-up animation is playing.
            yield return new WaitUntil(() => !_isChargingUp);

            _currentDestination = GetNewWanderDestination();

            while (Vector3.Distance(transform.position, _currentDestination) > arrivalThreshold)
            {
                // Pause movement if a charge-up is triggered mid-move.
                if (_isChargingUp)
                {
                    yield return new WaitUntil(() => !_isChargingUp);
                }

                Vector3 direction = (_currentDestination - transform.position).normalized;
                _rb.MovePosition(transform.position + direction * moveSpeed * Time.deltaTime);

                if (direction.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
                }

                yield return null;
            }

            float waitTime = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(waitTime);
        }
    }

    private Vector3 GetNewWanderDestination()
    {
        float randomX = Random.Range(-wanderAreaSize.x / 2, wanderAreaSize.x / 2);
        float randomZ = Random.Range(-wanderAreaSize.y / 2, wanderAreaSize.y / 2);
        return new Vector3(_startPosition.x + randomX, _startPosition.y, _startPosition.z + randomZ);
    }

    private IEnumerator ChargeUpSpinRoutine()
    {
        _isChargingUp = true;

        float totalRotation = 360f * chargeUpSpins;
        float rotationSpeed = totalRotation / chargeUpDuration;

        float timer = 0f;
        while (timer < chargeUpDuration)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            timer += Time.deltaTime;
            yield return null;
        }

        _isChargingUp = false;
        _chargeUpCoroutine = null;
    }

    void OnDrawGizmosSelected()
    {
        Vector3 center = (wanderAreaCenter != null) ? wanderAreaCenter.position : (Application.isPlaying ? _startPosition : transform.position);
        Gizmos.color = new Color(0, 1, 1, 0.5f);
        Gizmos.DrawWireCube(center, new Vector3(wanderAreaSize.x, 0.5f, wanderAreaSize.y));

        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, _currentDestination);
            Gizmos.DrawSphere(_currentDestination, 0.5f);
        }
    }
}