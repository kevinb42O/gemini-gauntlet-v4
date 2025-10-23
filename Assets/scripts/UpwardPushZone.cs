using UnityEngine;

/// <summary>
/// Creates a zone that pushes the player upward when they enter it.
/// Continues pushing until the player reaches the maximum height.
/// Perfect for jump pads, updrafts, or vertical boost zones!
/// </summary>
public class UpwardPushZone : MonoBehaviour
{
    [Header("Zone Settings")]
    [Tooltip("Radius of the push zone")]
    [SerializeField] private float zoneRadius = 50f;
    
    [Tooltip("Upward velocity applied to the player (MUST BE HIGHER THAN GRAVITY! For -300 gravity, use 400+)")]
    [SerializeField] private float pushForce = 500f;
    
    [Tooltip("Maximum height above zone center where push effect still works")]
    [SerializeField] private float maxPushHeight = 500f;
    
    [Tooltip("Use impulse mode for instant boost instead of continuous force")]
    [SerializeField] private bool useImpulseMode = false;
    
    [Tooltip("Cooldown between impulse boosts (only for impulse mode)")]
    [SerializeField] private float impulseCooldown = 0.5f;
    
    [Tooltip("Cancel horizontal velocity when pushing (makes you go straight up instead of preserving momentum)")]
    [SerializeField] private bool cancelHorizontalVelocity = false;
    
    [Header("Visual Settings")]
    [Tooltip("Show the zone radius in the editor")]
    [SerializeField] private bool showGizmos = true;
    
    [Tooltip("Color of the zone gizmo")]
    [SerializeField] private Color gizmoColor = new Color(0f, 1f, 0f, 0.3f);
    
    [Header("Optional Effects")]
    [Tooltip("Particle system to play when player enters zone (optional)")]
    [SerializeField] private ParticleSystem entryEffect;
    
    [Tooltip("Audio source for sound effects (optional)")]
    [SerializeField] private AudioSource audioSource;
    
    private CharacterController playerController;
    private AAAMovementController playerMovement;
    private Transform playerTransform;
    private bool playerInZone = false;
    private float zoneBaseHeight;
    private float lastImpulseTime = -999f;
    private float currentUpwardVelocity = 0f;
    
    private void Awake()
    {
        // Store the base height of the zone
        zoneBaseHeight = transform.position.y;
    }
    
    private void Update()
    {
        if (playerInZone && playerController != null && playerTransform != null)
        {
            // Check if player is still within horizontal radius
            float horizontalDistance = Vector3.Distance(
                new Vector3(transform.position.x, 0, transform.position.z),
                new Vector3(playerTransform.position.x, 0, playerTransform.position.z)
            );
            
            // Check if player is below max height
            float playerHeightAboveZone = playerTransform.position.y - zoneBaseHeight;
            
            // Apply push force if within radius and below max height
            if (horizontalDistance <= zoneRadius && playerHeightAboveZone < maxPushHeight)
            {
                ApplyUpwardPush();
            }
            else if (horizontalDistance > zoneRadius || playerHeightAboveZone >= maxPushHeight)
            {
                // Player left the zone
                playerInZone = false;
                playerController = null;
                playerMovement = null;
                playerTransform = null;
                currentUpwardVelocity = 0f;
            }
        }
    }
    
    private void ApplyUpwardPush()
    {
        if (playerController != null && playerMovement != null)
        {
            if (useImpulseMode)
            {
                // Impulse mode: Set upward velocity instantly with cooldown
                if (Time.time - lastImpulseTime >= impulseCooldown)
                {
                    // Debug.Log($"[UpwardPushZone] 🚀 IMPULSE BOOST! Setting velocity to: {pushForce}");
                    // Debug.Log($"[UpwardPushZone] Before: IsGrounded={playerMovement.IsGrounded}, Velocity={playerMovement.Velocity}");
                    
                    // Cancel horizontal velocity if requested (straight up launch)
                    if (cancelHorizontalVelocity)
                    {
                        Vector3 newVelocity = new Vector3(0, pushForce, 0);
                        playerMovement.SetVelocity(newVelocity);
                        // Debug.Log($"[UpwardPushZone] ⬆️ STRAIGHT UP! Zeroed horizontal velocity");
                    }
                    else
                    {
                        playerMovement.SetUpwardVelocity(pushForce);
                        // Debug.Log($"[UpwardPushZone] ↗️ Preserved horizontal momentum");
                    }
                    
                    lastImpulseTime = Time.time;
                    
                    // Debug.Log($"[UpwardPushZone] After: IsGrounded={playerMovement.IsGrounded}, Velocity={playerMovement.Velocity}");
                }
            }
            else
            {
                // Continuous mode: Add upward velocity every frame
                float velocityToAdd = pushForce * Time.deltaTime;
                
                // Cancel horizontal velocity if requested
                if (cancelHorizontalVelocity)
                {
                    Vector3 currentVel = playerMovement.Velocity;
                    // Gradually reduce horizontal velocity while adding upward
                    Vector3 newVelocity = new Vector3(
                        currentVel.x * 0.9f, // Dampen horizontal
                        currentVel.y + velocityToAdd,
                        currentVel.z * 0.9f  // Dampen horizontal
                    );
                    playerMovement.SetVelocity(newVelocity);
                }
                else
                {
                    playerMovement.AddUpwardVelocity(velocityToAdd);
                }
                
                // PERFORMANCE: Disabled to prevent FPS drops
                // if (Time.frameCount % 30 == 0)
                // {
                //     Debug.Log($"[UpwardPushZone] Continuous push: Adding {velocityToAdd}, Current velocity: {playerMovement.Velocity.y}");
                // }
            }
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // CRITICAL: Try to find player components (don't rely on tag)
        CharacterController cc = other.GetComponent<CharacterController>();
        AAAMovementController mc = other.GetComponent<AAAMovementController>();
        
        // If we found both components, this is the player!
        if (cc != null && mc != null)
        {
            playerController = cc;
            playerMovement = mc;
            playerTransform = other.transform;
            
            // Debug.Log($"[UpwardPushZone] ✅ PLAYER DETECTED! GameObject: {other.gameObject.name}, Tag: {other.tag}");
            
            playerInZone = true;
            lastImpulseTime = -999f; // Reset impulse timer on entry
            currentUpwardVelocity = 0f;
            
            // Play entry effect if assigned
            if (entryEffect != null && !entryEffect.isPlaying)
            {
                entryEffect.Play();
            }
            
            // Play sound effect if assigned
            if (audioSource != null && !audioSource.isPlaying)
            {
                audioSource.Play();
            }
            
            // Debug.Log($"[UpwardPushZone] 🚀 PLAYER ENTERED PUSH ZONE!");
            // Debug.Log($"[UpwardPushZone] Push Force: {pushForce}, Max Height: {maxPushHeight}, Mode: {(useImpulseMode ? "IMPULSE" : "CONTINUOUS")}");
            // Debug.Log($"[UpwardPushZone] Player Gravity: {playerMovement.Gravity}, IsGrounded: {playerMovement.IsGrounded}");
            // Debug.Log($"[UpwardPushZone] ⚠️ IMPORTANT: Push force ({pushForce}) must be MUCH higher than gravity ({Mathf.Abs(playerMovement.Gravity)}) to work!");
        }
        else
        {
            // Debug.LogWarning($"[UpwardPushZone] ❌ NOT PLAYER: {other.gameObject.name} (CharacterController: {cc != null}, AAAMovementController: {mc != null})");
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        // Player left the trigger zone
        if (other.CompareTag("Player"))
        {
            playerInZone = false;
            playerController = null;
            playerMovement = null;
            playerTransform = null;
            currentUpwardVelocity = 0f;
            
            // Debug.Log("[UpwardPushZone] Player left push zone");
        }
    }
    
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        // Draw the horizontal radius at base height
        Gizmos.color = gizmoColor;
        DrawCircle(transform.position, zoneRadius, 32);
        
        // Draw the max height cylinder
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, gizmoColor.a * 0.5f);
        DrawCircle(transform.position + Vector3.up * maxPushHeight, zoneRadius, 32);
        
        // Draw vertical lines to show the cylinder
        Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle) * zoneRadius, 0, Mathf.Sin(angle) * zoneRadius);
            Gizmos.DrawLine(transform.position + offset, transform.position + offset + Vector3.up * maxPushHeight);
        }
        
        // Draw upward arrow to show push direction
        Gizmos.color = Color.yellow;
        Vector3 arrowStart = transform.position;
        Vector3 arrowEnd = transform.position + Vector3.up * (maxPushHeight * 0.5f);
        Gizmos.DrawLine(arrowStart, arrowEnd);
        
        // Draw arrow head
        Vector3 arrowTip = arrowEnd;
        float arrowSize = zoneRadius * 0.3f;
        Gizmos.DrawLine(arrowTip, arrowTip + (Vector3.down + Vector3.right) * arrowSize);
        Gizmos.DrawLine(arrowTip, arrowTip + (Vector3.down + Vector3.left) * arrowSize);
        Gizmos.DrawLine(arrowTip, arrowTip + (Vector3.down + Vector3.forward) * arrowSize);
        Gizmos.DrawLine(arrowTip, arrowTip + (Vector3.down + Vector3.back) * arrowSize);
    }
    
    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
    
    private void OnValidate()
    {
        // Ensure values stay positive
        zoneRadius = Mathf.Max(0.1f, zoneRadius);
        pushForce = Mathf.Max(0f, pushForce);
        maxPushHeight = Mathf.Max(0.1f, maxPushHeight);
    }
}
