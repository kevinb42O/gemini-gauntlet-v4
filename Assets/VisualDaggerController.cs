// VisualDaggerController.cs
using UnityEngine;

public class VisualDaggerController : MonoBehaviour
{
    private float speed;
    private float lifetime;
    private Vector3 targetPoint;
    private string poolTag;
    private float timeToLive;
    private bool hasTargetPoint;

    // Call this after spawning from the pool
    public void Initialize(string tag, Vector3 startPos, Vector3 endPoint, float travelSpeed, float maxLifetime)
    {
        transform.position = startPos;
        this.poolTag = tag;
        this.targetPoint = endPoint;
        this.speed = travelSpeed;
        this.lifetime = maxLifetime;
        this.timeToLive = 0f;
        this.hasTargetPoint = true;
        
        // Point the dagger towards its destination
        transform.LookAt(targetPoint);
    }

    void Update()
    {
        timeToLive += Time.deltaTime;
        if (timeToLive > lifetime)
        {
            ObjectPooler.Instance.ReturnToPool(poolTag, gameObject);
            return;
        }

        if (hasTargetPoint)
        {
            // Move towards the target, and if we get very close, just keep going straight.
            // This prevents slowing down at the end.
            float distanceToTarget = Vector3.Distance(transform.position, targetPoint);
            if (distanceToTarget < 1.0f) {
                hasTargetPoint = false; // Switch to flying straight
            }
        }
        
        transform.position += transform.forward * speed * Time.deltaTime;
    }
}