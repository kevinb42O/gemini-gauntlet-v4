using UnityEngine;
using CompanionAI;

public class SimpleHomingDagger : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 1500f;
    public float damage = 25f;
    public float lifetime = 10f;
    public float homingStrength = 5f;
    
    [Header("Target Tags")]
    public string[] targetTags = {"Skull", "Gem", "Enemy"};
    
    private Transform target;
    private Rigidbody rb;
    private float timer;
    private bool isHoming;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        rb.useGravity = false;
        rb.isKinematic = false;
        
        // Find nearest enemy
        FindTarget();
        
        // Start moving forward
        rb.linearVelocity = transform.forward * speed;
        
        timer = lifetime;
        
        Debug.Log($"[SimpleHomingDagger] Created with target: {(target ? target.name : "None")}", this);
    }
    
    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            Destroy(gameObject);
            return;
        }
        
        // Re-search for targets every 0.3 seconds (more frequent)
        if (Time.time % 0.3f < Time.deltaTime)
        {
            // Always search for better targets (skulls have priority)
            FindTarget();
        }
        
        // Check if current target is still valid every frame
        if (target != null)
        {
            if (!target.gameObject.activeInHierarchy)
            {
                Debug.Log($"[SimpleHomingDagger] Target {target.name} is inactive - searching for new target", this);
                target = null;
                isHoming = false;
                FindTarget();
            }
        }
        
        // Home toward target if we have one
        if (target != null && isHoming)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            Vector3 currentVelocity = rb.linearVelocity.normalized;
            Vector3 newDirection = Vector3.Slerp(currentVelocity, direction, homingStrength * Time.deltaTime);
            rb.linearVelocity = newDirection * speed;
            
            // Rotate to face movement direction
            transform.rotation = Quaternion.LookRotation(rb.linearVelocity);
            
            Debug.Log($"[SimpleHomingDagger] Homing toward {target.name}", this);
        }
    }
    void FindTarget()
    {
        // Get all colliders in range (5000 units radius)
        Collider[] allColliders = Physics.OverlapSphere(transform.position, 5000f);
        
        // Separate targets by priority: Skulls > Enemy Companions > Gems
        Transform bestSkull = null;
        Transform bestEnemyCompanion = null;
        Transform bestGem = null;
        float closestSkullDistance = float.MaxValue;
        float closestEnemyCompanionDistance = float.MaxValue;
        float closestGemDistance = float.MaxValue;
        
        Debug.Log($"[SimpleHomingDagger] Scanning {allColliders.Length} colliders for targets", this);
        
        foreach (Collider col in allColliders)
        {
            if (col == null || !col.gameObject.activeInHierarchy) continue;
            
            // TARGET VALIDATION: Check tags AND enemy companion component
            bool isValidTarget = false;
            string matchedTag = "";
            
            // Check for enemy companion first (component-based detection)
            EnemyCompanionBehavior enemyComp = col.GetComponent<EnemyCompanionBehavior>();
            if (enemyComp != null && enemyComp.isEnemy)
            {
                isValidTarget = true;
                matchedTag = "Enemy";
            }
            else
            {
                // Check normal tags for other enemies
                foreach (string tag in targetTags)
                {
                    if (col.CompareTag(tag))
                    {
                        isValidTarget = true;
                        matchedTag = tag;
                        break;
                    }
                }
            }
            
            if (!isValidTarget) continue; // Skip everything that doesn't match our criteria
            
            float distance = Vector3.Distance(transform.position, col.transform.position);
            string name = col.name.ToLower();
            
            // PRIORITY 1: SKULLS (highest priority)
            if (matchedTag == "Skull")
            {
                if (distance < closestSkullDistance)
                {
                    closestSkullDistance = distance;
                    bestSkull = col.transform;
                    Debug.Log($"[SimpleHomingDagger] Found SKULL target: {col.name} (tag: {matchedTag})", this);
                }
            }
            // PRIORITY 2: ENEMY COMPANIONS (medium priority)
            else if (matchedTag == "Enemy")
            {
                if (distance < closestEnemyCompanionDistance)
                {
                    closestEnemyCompanionDistance = distance;
                    bestEnemyCompanion = col.transform;
                    Debug.Log($"[SimpleHomingDagger] Found ENEMY COMPANION target: {col.name} (component detected)", this);
                }
            }
            // PRIORITY 3: GEMS (lower priority)
            else if (matchedTag == "Gem")
            {
                if (distance < closestGemDistance)
                {
                    closestGemDistance = distance;
                    bestGem = col.transform;
                    Debug.Log($"[SimpleHomingDagger] Found GEM target: {col.name} (tag: {matchedTag})", this);
                }
            }
        }
        
        // Select target by priority: Skull > Enemy Companion > Gem
        Transform newTarget = null;
        string targetType = "";
        
        if (bestSkull != null)
        {
            newTarget = bestSkull;
            targetType = "SKULL";
        }
        else if (bestEnemyCompanion != null)
        {
            newTarget = bestEnemyCompanion;
            targetType = "ENEMY COMPANION";
        }
        else if (bestGem != null)
        {
            newTarget = bestGem;
            targetType = "GEM";
        }
        
        // Only switch targets if we found a better one or current target is inactive
        if (newTarget != null && (target == null || !target.gameObject.activeInHierarchy || newTarget != target))
        {
            target = newTarget;
            isHoming = true;
            Debug.Log($"[SimpleHomingDagger] NEW TARGET ACQUIRED: {targetType} - {target.name}", this);
            
            // Start homing immediately for new targets
            if (!isHoming)
            {
                Invoke(nameof(EnableHoming), 0.2f);
            }
        }
        else if (target == null)
        {
            Debug.Log("[SimpleHomingDagger] No valid targets found - will fly straight", this);
        }
    }
    
    
    void EnableHoming()
    {
        isHoming = true;
    }
    
    void OnTriggerEnter(Collider other)
    {
        // ONLY HIT OBJECTS WITH TARGET TAGS
        bool isValidTarget = false;
        string matchedTag = "";
        
        foreach (string tag in targetTags)
        {
            if (other.CompareTag(tag))
            {
                isValidTarget = true;
                matchedTag = tag;
                break;
            }
        }
        
        if (isValidTarget)
        {
            // Try to damage the target
            var damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage, transform.position, Vector3.zero);
                Debug.Log($"[SimpleHomingDagger] Hit {other.name} (tag: {matchedTag}) for {damage} damage", this);
            }
            else
            {
                Debug.Log($"[SimpleHomingDagger] Hit {other.name} (tag: {matchedTag}) - no damage component", this);
            }
            
            // Destroy the dagger
            Destroy(gameObject);
        }
        // IGNORE EVERYTHING ELSE - no more hitting random obstacles!
    }
}
