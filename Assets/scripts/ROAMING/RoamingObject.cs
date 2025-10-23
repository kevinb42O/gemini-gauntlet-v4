using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RoamingObject : MonoBehaviour
{
    public float speed = 2.0f;
    private Vector3 center;
    private float radius;
    private Vector3 targetPosition;
    
    // Swarming behavior variables
    private Vector3 swarmVelocity = Vector3.zero;
    private bool isAttackingCamera = false;
    private bool isAvoidingMouse = false;
    private float lastCameraAttackTime = 0f;
    private Vector3 mouseAvoidanceDirection = Vector3.zero;
    private float mouseAvoidanceTimer = 0f;
    
    // Enhanced mouse avoidance variables
    private float currentMouseAvoidanceStrength = 0f;
    private Vector3 smoothMouseAvoidanceDirection = Vector3.zero;
    
    // Mouse attack sequence variables
    private bool isAttackingMouse = false;
    private float mouseAttackStartTime = 0f;
    private float mouseAttackDuration = 15f;
    private float attackSpeed = 10f;
    private float chaseRestDuration = 2f;
    private Vector3 mouseTargetPosition;
    private float currentAttackSpeed;
    private bool isInAttackPhase = true;
    private float phaseStartTime = 0f;
    
    // Camera attack variables
    private Vector3 cameraAttackTarget;
    private bool isDodging = false;
    private Vector3 dodgeDirection;
    private Vector3 returnPosition;

    // Call this from the manager to set the roaming area
    public void Initialize(Vector3 centerPoint, float roamRadius)
    {
        center = centerPoint;
        radius = roamRadius;
        StartCoroutine(SwarmBehavior());
    }

    private IEnumerator SwarmBehavior()
    {
        while (true)
        {
            if (isAttackingMouse)
            {
                // Handle mouse attack sequence
                HandleMouseAttackSequence();
                yield return null;
                continue;
            }
            
            if (isAttackingCamera)
            {
                yield return StartCoroutine(HandleCameraAttack());
            }
            else if (isAvoidingMouse)
            {
                HandleMouseAvoidance();
            }
            else
            {
                // Normal swarming behavior
                CalculateSwarmMovement();
                ApplySwarmMovement();
                
                // Occasional camera attack (only if not attacking mouse)
                if (!isAttackingMouse)
                {
                    CheckForCameraAttack();
                }
            }
            
            yield return null;
        }
    }
    
    private void CalculateSwarmMovement()
    {
        Vector3 separation = Vector3.zero;
        Vector3 cohesion = Vector3.zero;
        Vector3 alignment = Vector3.zero;
        Vector3 orbitalMovement = Vector3.zero;
        
        var nearby = ObjectManager.GetNearbyObjects(transform.position, ObjectManager.Instance.separationRadius);
        
        if (nearby.Count > 1) // More than just itself
        {
            // Separation - avoid crowding neighbors
            foreach (var neighbor in nearby)
            {
                if (neighbor != this)
                {
                    Vector3 diff = transform.position - neighbor.transform.position;
                    float distance = diff.magnitude;
                    if (distance > 0)
                    {
                        separation += diff.normalized / distance; // Closer = stronger repulsion
                    }
                }
            }
            
            // Cohesion - move towards center of nearby objects
            Vector3 centerOfMass = Vector3.zero;
            foreach (var neighbor in nearby)
            {
                if (neighbor != this)
                {
                    centerOfMass += neighbor.transform.position;
                }
            }
            centerOfMass /= (nearby.Count - 1);
            cohesion = (centerOfMass - transform.position).normalized;
        }
        
        // ENHANCED BUBBLE-AWARE MOVEMENT
        Vector3 centerAttraction = Vector3.zero;
        float distanceFromCenter = Vector3.Distance(transform.position, center);
        float normalizedDistance = distanceFromCenter / radius;
        
        // Gentle pull toward center when getting far
        if (normalizedDistance > 0.6f)
        {
            float attractionStrength = Mathf.Pow(normalizedDistance - 0.6f, 2) * 3f;
            centerAttraction = (center - transform.position).normalized * attractionStrength;
        }
        
        // SPHERICAL ORBITAL MOVEMENT - creates beautiful bubble-aware patterns
        Vector3 toCenter = center - transform.position;
        if (toCenter.magnitude > 0.1f)
        {
            // Create tangential movement around the sphere center
            Vector3 tangent = Vector3.Cross(toCenter.normalized, Vector3.up);
            if (tangent.magnitude < 0.1f) // Handle edge case when aligned with up
            {
                tangent = Vector3.Cross(toCenter.normalized, Vector3.right);
            }
            
            // Add vertical component for 3D spherical movement
            Vector3 verticalTangent = Vector3.Cross(toCenter.normalized, tangent.normalized);
            
            // Create smooth orbital motion with some randomness
            float orbitalSpeed = 0.5f + Mathf.Sin(Time.time * 0.5f + GetInstanceID() * 0.1f) * 0.3f;
            float verticalSpeed = Mathf.Cos(Time.time * 0.3f + GetInstanceID() * 0.15f) * 0.2f;
            
            orbitalMovement = (tangent * orbitalSpeed + verticalTangent * verticalSpeed).normalized * 0.8f;
        }
        
        // GENTLE WANDERING - adds natural variation
        Vector3 wandering = Vector3.zero;
        if (Random.value < 0.1f * Time.deltaTime) // Occasional direction change
        {
            wandering = Random.insideUnitSphere * 0.3f;
        }
        
        // Combine all forces with proper weighting
        swarmVelocity = (separation * ObjectManager.Instance.separationStrength + 
                        cohesion * ObjectManager.Instance.cohesionStrength * 0.8f + 
                        centerAttraction * 1.2f +
                        orbitalMovement * 1.0f +
                        wandering * 0.5f).normalized;
    }
    
    private void ApplySwarmMovement()
    {
        if (swarmVelocity != Vector3.zero)
        {
            Vector3 targetPos = transform.position + swarmVelocity * speed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
            
            // Smooth rotation towards movement direction
            if (swarmVelocity.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(swarmVelocity);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 3f);
            }
        }
    }
    
    private void CheckForCameraAttack()
    {
        if (ObjectManager.MainCamera == null) return;
        
        // Random chance to attack camera
        if (Time.time - lastCameraAttackTime > 5f && Random.value < ObjectManager.Instance.cameraAttackChance * Time.deltaTime)
        {
            StartCameraAttack();
        }
    }
    
    private void StartCameraAttack()
    {
        isAttackingCamera = true;
        lastCameraAttackTime = Time.time;
        
        // Set target slightly behind camera to simulate dodge
        Vector3 cameraPos = ObjectManager.MainCamera.transform.position;
        Vector3 cameraForward = ObjectManager.MainCamera.transform.forward;
        cameraAttackTarget = cameraPos + cameraForward * 2f;
        
        returnPosition = center + Random.insideUnitSphere * radius * 0.5f;
        isDodging = false;
    }
    
    private IEnumerator HandleCameraAttack()
    {
        Vector3 cameraPos = ObjectManager.MainCamera.transform.position;
        
        // Phase 1: Fly toward camera
        while (!isDodging && Vector3.Distance(transform.position, cameraPos) > ObjectManager.Instance.dodgeDistance)
        {
            Vector3 attackDirection = (cameraPos - transform.position).normalized;
            transform.position += attackDirection * ObjectManager.Instance.cameraAttackSpeed * Time.deltaTime;
            
            // Look at camera
            transform.LookAt(cameraPos);
            
            yield return null;
        }
        
        // Phase 2: Dodge at last moment
        isDodging = true;
        dodgeDirection = Vector3.Cross(ObjectManager.MainCamera.transform.forward, Vector3.up).normalized;
        if (Random.value < 0.5f) dodgeDirection *= -1; // Random dodge direction
        
        float dodgeTime = 0.5f;
        float dodgeTimer = 0f;
        
        while (dodgeTimer < dodgeTime)
        {
            Vector3 dodgeMovement = dodgeDirection * ObjectManager.Instance.cameraAttackSpeed * Time.deltaTime;
            transform.position += dodgeMovement;
            
            dodgeTimer += Time.deltaTime;
            yield return null;
        }
        
        // Phase 3: Return to swarm
        while (Vector3.Distance(transform.position, returnPosition) > 1f)
        {
            Vector3 returnDirection = (returnPosition - transform.position).normalized;
            transform.position += returnDirection * speed * Time.deltaTime;
            
            transform.LookAt(returnPosition);
            yield return null;
        }
        
        isAttackingCamera = false;
    }
    
    // Enhanced mouse threat reaction with smooth strength-based avoidance
    public void ReactToMouseThreat(Vector3 mouseWorldPos, float avoidanceStrength)
    {
        if (isAttackingCamera) return; // Don't interrupt camera attacks
        
        // Calculate avoidance direction
        Vector3 rawAvoidanceDirection = (transform.position - mouseWorldPos).normalized;
        
        // Add some randomness to make it look more natural
        rawAvoidanceDirection += Random.insideUnitSphere * 0.2f;
        rawAvoidanceDirection = rawAvoidanceDirection.normalized;
        
        // Smooth the avoidance direction for natural movement
        smoothMouseAvoidanceDirection = Vector3.Slerp(smoothMouseAvoidanceDirection, rawAvoidanceDirection, Time.deltaTime * 5f);
        
        // Update avoidance strength smoothly
        currentMouseAvoidanceStrength = Mathf.Lerp(currentMouseAvoidanceStrength, avoidanceStrength, Time.deltaTime * 8f);
        
        isAvoidingMouse = true;
        mouseAvoidanceTimer = 0.1f; // Short timer, refreshed continuously
    }
    
    // Stop mouse avoidance when mouse is no longer threatening
    public void StopMouseAvoidance()
    {
        if (isAttackingCamera) return;
        
        // Smoothly reduce avoidance strength
        currentMouseAvoidanceStrength = Mathf.Lerp(currentMouseAvoidanceStrength, 0f, Time.deltaTime * 6f);
        
        // Stop avoiding when strength is very low
        if (currentMouseAvoidanceStrength < 0.05f)
        {
            isAvoidingMouse = false;
            currentMouseAvoidanceStrength = 0f;
            mouseAvoidanceTimer = 0f;
        }
    }
    
    private void HandleMouseAvoidance()
    {
        if (currentMouseAvoidanceStrength > 0.01f)
        {
            // Smooth avoidance movement based on current strength
            Vector3 avoidanceMovement = smoothMouseAvoidanceDirection * 
                                      ObjectManager.Instance.mouseAvoidanceStrength * 
                                      currentMouseAvoidanceStrength * 
                                      speed * Time.deltaTime;
            
            transform.position += avoidanceMovement;
            
            // Smooth rotation away from mouse threat
            if (smoothMouseAvoidanceDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(smoothMouseAvoidanceDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 4f);
            }
            
            mouseAvoidanceTimer -= Time.deltaTime;
        }
        else
        {
            isAvoidingMouse = false;
            mouseAvoidanceTimer = 0f;
        }
    }
    
    // Removed relocation methods - no longer needed
    
    // NEW: Start 15-second mouse attack sequence
    public void StartMouseAttackSequence(Vector3 mousePos, float attackSpd, float attackDur, float restDur)
    {
        if (isAttackingMouse) return; // Already attacking
        
        isAttackingMouse = true;
        mouseAttackStartTime = Time.time;
        mouseAttackDuration = attackDur;
        attackSpeed = attackSpd;
        chaseRestDuration = restDur;
        mouseTargetPosition = mousePos;
        
        // Start with attack phase
        isInAttackPhase = true;
        phaseStartTime = Time.time;
        currentAttackSpeed = attackSpeed;
        
        Debug.Log($"🎯 Object starting 15-second mouse attack sequence!");
    }
    
    // Handle the mouse attack sequence behavior
    private void HandleMouseAttackSequence()
    {
        // Check if 15 seconds are up
        if (Time.time - mouseAttackStartTime >= mouseAttackDuration)
        {
            // Return to bubble
            isAttackingMouse = false;
            Debug.Log($"⏰ Mouse attack sequence complete - returning to bubble");
            return;
        }
        
        // Update mouse target position (keep tracking mouse)
        if (ObjectManager.MainCamera != null)
        {
            Ray mouseRay = ObjectManager.MainCamera.ScreenPointToRay(Input.mousePosition);
            Vector3 swarmCenter = ObjectManager.Instance.transform.position;
            float distanceToCamera = Vector3.Distance(swarmCenter, ObjectManager.MainCamera.transform.position);
            mouseTargetPosition = mouseRay.origin + mouseRay.direction * distanceToCamera;
        }
        
        float phaseTime = Time.time - phaseStartTime;
        
        if (isInAttackPhase)
        {
            // ATTACK PHASE: Move at attack speed toward mouse
            currentAttackSpeed = Mathf.Lerp(currentAttackSpeed, attackSpeed, Time.deltaTime * 3f);
            
            Vector3 attackDirection = (mouseTargetPosition - transform.position).normalized;
            transform.position += attackDirection * currentAttackSpeed * Time.deltaTime;
            
            // Look at mouse target
            if (attackDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(attackDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 4f);
            }
            
            // Switch to rest phase after attacking briefly
            if (phaseTime >= 1f) // Attack for 1 second
            {
                isInAttackPhase = false;
                phaseStartTime = Time.time;
            }
        }
        else
        {
            // REST PHASE: Chase at normal speed for 2 seconds
            currentAttackSpeed = Mathf.Lerp(currentAttackSpeed, speed, Time.deltaTime * 2f);
            
            Vector3 chaseDirection = (mouseTargetPosition - transform.position).normalized;
            transform.position += chaseDirection * currentAttackSpeed * Time.deltaTime;
            
            // Look at mouse target
            if (chaseDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(chaseDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 3f);
            }
            
            // Switch back to attack phase after rest period
            if (phaseTime >= chaseRestDuration)
            {
                isInAttackPhase = true;
                phaseStartTime = Time.time;
            }
        }
    }
    
    // All migration methods removed as requested

    // This is called when another collider enters the trigger
    private void OnTriggerEnter(Collider other)
    {
        // Check if the other object is also a roaming object
        if (other.GetComponent<RoamingObject>() != null && !isAttackingCamera)
        {
            // Quick separation response - but still maintains swarming behavior
            Vector3 separationForce = (transform.position - other.transform.position).normalized;
            swarmVelocity += separationForce * 2f; // Boost separation temporarily
        }
    }
    
    void OnDestroy()
    {
        // Remove from global list when destroyed
        if (ObjectManager.AllObjects.Contains(this))
        {
            ObjectManager.AllObjects.Remove(this);
        }
    }
}