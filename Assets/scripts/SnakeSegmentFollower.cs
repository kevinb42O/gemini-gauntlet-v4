using UnityEngine;

/// <summary>
/// Makes a snake body segment smoothly follow the segment in front of it
/// Much simpler and more reliable than physics joints!
/// </summary>
public class SnakeSegmentFollower : MonoBehaviour
{
    [Header("Follow Target")]
    [Tooltip("The segment this one should follow (usually the previous segment).")]
    public Transform targetSegment;

    [Header("Follow Settings")]
    [Tooltip("How far behind the target this segment stays.")]
    public float followDistance = 150f;
    [Tooltip("How fast this segment moves to catch up (higher = tighter following).")]
    public float moveSpeed = 10f;
    [Tooltip("How fast this segment rotates to match target (higher = snappier turns).")]
    public float rotationSpeed = 5f;
    [Tooltip("Use Rigidbody physics (smoother) or direct transform movement (simpler).")]
    public bool usePhysics = true;

    private Rigidbody _rb;
    private Vector3 _desiredPosition;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        
        if (usePhysics && _rb != null)
        {
            _rb.useGravity = false;
            _rb.linearDamping = 2f;
            _rb.angularDamping = 2f;
        }
    }

    void FixedUpdate()
    {
        if (targetSegment == null) return;

        // Calculate where this segment should be (behind the target)
        Vector3 directionToTarget = transform.position - targetSegment.position;
        _desiredPosition = targetSegment.position + directionToTarget.normalized * followDistance;

        if (usePhysics && _rb != null)
        {
            // Physics-based following (smoother, more natural)
            Vector3 moveDirection = (_desiredPosition - transform.position);
            _rb.AddForce(moveDirection * moveSpeed, ForceMode.Acceleration);

            // Rotate to face movement direction
            if (_rb.linearVelocity.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(_rb.linearVelocity.normalized);
                _rb.rotation = Quaternion.Slerp(_rb.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed);
            }
        }
        else
        {
            // Direct transform following (simpler, more predictable)
            transform.position = Vector3.Lerp(transform.position, _desiredPosition, Time.fixedDeltaTime * moveSpeed);

            // Rotate to face the target
            Vector3 lookDirection = (targetSegment.position - transform.position).normalized;
            if (lookDirection.magnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (targetSegment != null)
        {
            // Draw line to target
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, targetSegment.position);

            // Draw desired position
            if (Application.isPlaying)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_desiredPosition, 20f);
            }
        }
    }
}
