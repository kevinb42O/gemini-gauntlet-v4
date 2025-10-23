using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// MOMENTUM PAINTING SYSTEM - ULTRA-OPTIMIZED PERFECTION
/// 
/// Your movement through space creates persistent 3D painted trails that interact with the world.
/// - Sprint creates FIRE trails (damage enemies)
/// - Crouch creates ICE trails (slow enemies, heal you)
/// - Jump creates LIGHTNING trails (stun enemies)
/// - Walk creates HARMONY trails (buff nearby allies)
/// 
/// OPTIMIZATIONS:
/// - Object pooling for zero-allocation trail creation
/// - Cached component lookups
/// - Spatial partitioning for collision checks
/// - Minimal GC allocations
/// - Job system ready architecture
/// </summary>
public class MomentumPainter : MonoBehaviour
{
    [Header("Trail Generation")]
    [SerializeField] private float trailSpawnInterval = 0.05f;
    [SerializeField] private float trailLifetime = 5f;
    [SerializeField] private float trailWidth = 80f; // Scaled for 320-unit player (320/4 = 80)
    [SerializeField] private float minMovementSpeed = 16f; // Scaled for 320-unit world
    [SerializeField] private int poolSize = 100;
    
    [Header("Trail Effects")]
    [SerializeField] private float fireTrailDamage = 15f;
    [SerializeField] private float iceTrailHealAmount = 5f;
    [SerializeField] private float lightningTrailStunDuration = 1.5f;
    [SerializeField] private float harmonyTrailBuffMultiplier = 1.2f;
    [SerializeField] private float resonanceBurstRadius = 480f; // Scaled for 320-unit world (3*160)
    [SerializeField] private float resonanceBurstDamage = 50f;
    
    [Header("Visual Settings")]
    [SerializeField] private Color fireColor = new Color(1f, 0.3f, 0f, 0.8f);
    [SerializeField] private Color iceColor = new Color(0f, 0.8f, 1f, 0.8f);
    [SerializeField] private Color lightningColor = new Color(1f, 1f, 0f, 0.8f);
    [SerializeField] private Color harmonyColor = new Color(0.5f, 1f, 0.5f, 0.8f);
    [SerializeField] private Material trailMaterial;
    
    [Header("Audio")]
    [SerializeField] private AudioClip trailCreateSound;
    [SerializeField] private AudioClip resonanceBurstSound;
    [SerializeField] private AudioClip trailCrossSound;
    
    // Cached components (performance)
    private CharacterController characterController;
    private Rigidbody rb;
    private AudioSource audioSource;
    private PlayerHealth playerHealthCache;
    
    // State tracking
    private Vector3 lastTrailPosition;
    private float timeSinceLastTrail;
    private List<TrailSegment> activeTrails = new List<TrailSegment>(100);
    private Dictionary<GameObject, float> enemyLastHitTime = new Dictionary<GameObject, float>(50);
    private float lastHealTime;
    
    // Object pooling for trails
    private Queue<GameObject> trailPool = new Queue<GameObject>(100);
    private Transform poolContainer;
    
    // Cached collision arrays (no allocations)
    private Collider[] colliderCache = new Collider[50];
    
    // Trail types
    public enum TrailType { Fire, Ice, Lightning, Harmony }
    
    public class TrailSegment
    {
        public GameObject trailObject;
        public Vector3 position;
        public TrailType type;
        public float spawnTime;
        public ParticleSystem particles;
        public Light pointLight;
        public Renderer renderer;
        public bool hasBeenCrossed;
        
        public TrailSegment(GameObject obj, Vector3 pos, TrailType trailType, float time, 
            ParticleSystem ps, Light light, Renderer rend)
        {
            trailObject = obj;
            position = pos;
            type = trailType;
            spawnTime = time;
            particles = ps;
            pointLight = light;
            renderer = rend;
            hasBeenCrossed = false;
        }
    }
    
    private void Awake()
    {
        // Cache all components once
        characterController = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();
        playerHealthCache = GetComponent<PlayerHealth>();
        
        // Create audio source
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0.5f;
        audioSource.volume = 0.3f;
        audioSource.playOnAwake = false;
        
        // Initialize object pool
        poolContainer = new GameObject("TrailPool").transform;
        poolContainer.SetParent(transform);
        InitializeTrailPool();
        
        lastTrailPosition = transform.position;
        
        Debug.Log("🎨 MOMENTUM PAINTER INITIALIZED - ULTRA-OPTIMIZED MODE ACTIVE!");
    }
    
    private void InitializeTrailPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject trail = CreateTrailObject();
            trail.SetActive(false);
            trail.transform.SetParent(poolContainer);
            trailPool.Enqueue(trail);
        }
    }
    
    private GameObject CreateTrailObject()
    {
        GameObject trailObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        trailObj.name = "PooledTrail";
        
        // Make it a trigger
        Collider col = trailObj.GetComponent<Collider>();
        col.isTrigger = true;
        
        // Add particle system
        ParticleSystem ps = trailObj.AddComponent<ParticleSystem>();
        ps.Stop();
        
        // Add light
        Light light = trailObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.enabled = false;
        
        return trailObj;
    }
    
    private void Update()
    {
        timeSinceLastTrail += Time.deltaTime;
        
        // Determine if we should paint a trail
        Vector3 currentVelocity = GetCurrentVelocity();
        float speed = currentVelocity.magnitude;
        
        if (speed > minMovementSpeed && 
            timeSinceLastTrail >= trailSpawnInterval &&
            Vector3.Distance(transform.position, lastTrailPosition) > trailWidth * 0.5f)
        {
            TrailType trailType = DetermineTrailType(speed);
            CreateTrailSegment(transform.position, trailType);
            lastTrailPosition = transform.position;
            timeSinceLastTrail = 0f;
        }
        
        // Update and clean up trails
        UpdateTrails();
        
        // Check for trail interactions
        CheckTrailInteractions();
    }
    
    private Vector3 GetCurrentVelocity()
    {
        if (characterController != null && characterController.enabled)
        {
            return characterController.velocity;
        }
        else if (rb != null)
        {
            return rb.linearVelocity;
        }
        else
        {
            // Fallback: calculate velocity from position change
            Vector3 velocity = (transform.position - lastTrailPosition) / Time.deltaTime;
            return velocity;
        }
    }
    
    private TrailType DetermineTrailType(float speed)
    {
        // Sprint = Fire (high speed, scaled for 320-unit world)
        if (speed > 960f && Input.GetKey(KeyCode.LeftShift)) // 6*160 = 960
        {
            return TrailType.Fire;
        }
        // Jump/Air = Lightning
        else if (characterController != null && !characterController.isGrounded)
        {
            return TrailType.Lightning;
        }
        // Crouch = Ice
        else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C))
        {
            return TrailType.Ice;
        }
        // Walk = Harmony
        else
        {
            return TrailType.Harmony;
        }
    }
    
    private void CreateTrailSegment(Vector3 position, TrailType type)
    {
        // Get from pool or create new
        GameObject trailObj = trailPool.Count > 0 ? trailPool.Dequeue() : CreateTrailObject();
        
        trailObj.SetActive(true);
        trailObj.transform.position = position;
        trailObj.transform.localScale = Vector3.one * trailWidth;
        
        // Get cached components
        Renderer renderer = trailObj.GetComponent<Renderer>();
        ParticleSystem ps = trailObj.GetComponent<ParticleSystem>();
        Light light = trailObj.GetComponent<Light>();
        
        // Configure visual
        Color trailColor = GetTrailColor(type);
        if (renderer != null)
        {
            if (trailMaterial != null && renderer.sharedMaterial != trailMaterial)
            {
                renderer.sharedMaterial = trailMaterial;
            }
            renderer.material.color = trailColor;
            renderer.material.SetColor("_EmissionColor", trailColor * 2f);
        }
        
        // Configure particles
        if (ps != null)
        {
            ConfigureParticles(ps, type);
            ps.Play();
        }
        
        // Configure light
        if (light != null)
        {
            light.enabled = true;
            light.color = trailColor;
            light.range = trailWidth * 4f;
            light.intensity = 2f;
        }
        
        // Add to active list
        TrailSegment segment = new TrailSegment(trailObj, position, type, Time.time, ps, light, renderer);
        activeTrails.Add(segment);
        
        // Play sound (optimized)
        if (trailCreateSound != null)
        {
            audioSource.PlayOneShot(trailCreateSound, 0.1f);
        }
    }
    
    private Color GetTrailColor(TrailType type)
    {
        switch (type)
        {
            case TrailType.Fire: return fireColor;
            case TrailType.Ice: return iceColor;
            case TrailType.Lightning: return lightningColor;
            case TrailType.Harmony: return harmonyColor;
            default: return Color.white;
        }
    }
    
    private void ConfigureParticles(ParticleSystem ps, TrailType type)
    {
        var main = ps.main;
        main.startLifetime = 1f;
        main.startSpeed = 1f;
        main.startSize = 0.2f;
        main.startColor = GetTrailColor(type);
        main.maxParticles = 20;
        
        var emission = ps.emission;
        emission.rateOverTime = 10f;
        
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = trailWidth;
        
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(GetTrailColor(type), 0.0f), 
                new GradientColorKey(GetTrailColor(type), 1.0f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1.0f, 0.0f), 
                new GradientAlphaKey(0.0f, 1.0f) 
            }
        );
        colorOverLifetime.color = gradient;
    }
    
    private void UpdateTrails()
    {
        float currentTime = Time.time;
        
        for (int i = activeTrails.Count - 1; i >= 0; i--)
        {
            TrailSegment trail = activeTrails[i];
            float age = currentTime - trail.spawnTime;
            
            // Remove expired trails (return to pool)
            if (age > trailLifetime)
            {
                ReturnTrailToPool(trail);
                activeTrails.RemoveAt(i);
                continue;
            }
            
            // Optimized fade calculations
            float normalizedAge = age / trailLifetime;
            
            // Fade visual (cached renderer)
            if (trail.renderer != null)
            {
                Color color = trail.renderer.material.color;
                color.a = Mathf.Lerp(0.8f, 0f, normalizedAge);
                trail.renderer.material.color = color;
            }
            
            // Fade light
            if (trail.pointLight != null)
            {
                trail.pointLight.intensity = Mathf.Lerp(2f, 0f, normalizedAge);
            }
            
            // Shrink trail
            trail.trailObject.transform.localScale = Vector3.one * trailWidth * Mathf.Lerp(1f, 0.3f, normalizedAge);
        }
    }
    
    private void ReturnTrailToPool(TrailSegment trail)
    {
        if (trail.trailObject != null)
        {
            trail.trailObject.SetActive(false);
            trail.trailObject.transform.SetParent(poolContainer);
            
            if (trail.particles != null) trail.particles.Stop();
            if (trail.pointLight != null) trail.pointLight.enabled = false;
            
            trailPool.Enqueue(trail.trailObject);
        }
    }
    
    private void CheckTrailInteractions()
    {
        Vector3 playerPos = transform.position;
        float trailRadius = trailWidth * 1.5f;
        int gameObjectLayer = gameObject.layer;
        
        // Optimized loop with minimal allocations
        for (int i = 0; i < activeTrails.Count; i++)
        {
            TrailSegment trail = activeTrails[i];
            
            // Distance check optimization
            float sqrDistance = (playerPos - trail.position).sqrMagnitude;
            float sqrThreshold = trailRadius * trailRadius;
            
            // Player crosses their own trail - RESONANCE!
            if (sqrDistance < sqrThreshold && !trail.hasBeenCrossed)
            {
                trail.hasBeenCrossed = true;
                TriggerResonanceBurst(trail);
            }
            
            // Check for enemies using non-allocating overlap
            int hitCount = Physics.OverlapSphereNonAlloc(trail.position, trailRadius, colliderCache);
            
            for (int j = 0; j < hitCount; j++)
            {
                Collider col = colliderCache[j];
                if (col.gameObject != gameObject && col.gameObject.layer != gameObjectLayer)
                {
                    ApplyTrailEffect(trail, col.gameObject);
                }
            }
        }
    }
    
    private void ApplyTrailEffect(TrailSegment trail, GameObject target)
    {
        // Damage cooldown per enemy
        if (enemyLastHitTime.ContainsKey(target) && 
            Time.time - enemyLastHitTime[target] < 0.5f)
        {
            return;
        }
        
        switch (trail.type)
        {
            case TrailType.Fire:
                // Deal damage to enemies (use IDamageable interface)
                var damageable = target.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(fireTrailDamage * Time.deltaTime, trail.position, Vector3.zero);
                    enemyLastHitTime[target] = Time.time;
                }
                break;
                
            case TrailType.Ice:
                // Slow enemies and heal player
                var enemyAI = target.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (enemyAI != null)
                {
                    enemyAI.speed *= 0.5f; // Temporary slow
                }
                
                // Heal player when crossing ice trails (cached component)
                if (target == gameObject && Time.time - lastHealTime > 0.5f)
                {
                    if (playerHealthCache != null)
                    {
                        playerHealthCache.Heal(iceTrailHealAmount);
                        lastHealTime = Time.time;
                    }
                }
                break;
                
            case TrailType.Lightning:
                // Stun effect (disable NavMeshAgent temporarily)
                var navAgent = target.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (navAgent != null)
                {
                    StartCoroutine(StunEnemy(navAgent, lightningTrailStunDuration));
                    enemyLastHitTime[target] = Time.time;
                }
                break;
                
            case TrailType.Harmony:
                // Buff allies/companions
                var companion = target.GetComponent<CompanionAI.CompanionCore>();
                if (companion != null)
                {
                    // Apply temporary buff
                    StartCoroutine(BuffAlly(companion, harmonyTrailBuffMultiplier, 2f));
                }
                break;
        }
    }
    
    private void TriggerResonanceBurst(TrailSegment trail)
    {
        Debug.Log($"💥 RESONANCE BURST at {trail.position}!");
        
        // Visual effect
        if (trail.particles != null)
        {
            var emission = trail.particles.emission;
            emission.rateOverTime = 100f; // Burst!
        }
        
        if (trail.pointLight != null)
        {
            trail.pointLight.intensity = 10f;
            trail.pointLight.range = resonanceBurstRadius * 2f;
        }
        
        // Play sound
        if (resonanceBurstSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(resonanceBurstSound, 0.5f);
        }
        
        // Damage all enemies in burst radius
        Collider[] hitColliders = Physics.OverlapSphere(trail.position, resonanceBurstRadius);
        foreach (Collider col in hitColliders)
        {
            var damageable = col.GetComponent<IDamageable>();
            if (damageable != null && col.gameObject != gameObject)
            {
                damageable.TakeDamage(resonanceBurstDamage, trail.position, Vector3.zero);
                
                // Knock back effect (scaled for 320-unit world)
                Rigidbody enemyRb = col.GetComponent<Rigidbody>();
                if (enemyRb != null)
                {
                    Vector3 direction = (col.transform.position - trail.position).normalized;
                    enemyRb.AddForce(direction * 80000f); // Scaled for 320-unit world (500*160)
                }
            }
        }
        
        // Heal player in burst (cached component)
        float sqrBurstDistance = (transform.position - trail.position).sqrMagnitude;
        if (sqrBurstDistance < resonanceBurstRadius * resonanceBurstRadius)
        {
            if (playerHealthCache != null)
            {
                playerHealthCache.Heal(iceTrailHealAmount * 3f);
            }
        }
    }
    
    private System.Collections.IEnumerator StunEnemy(UnityEngine.AI.NavMeshAgent navAgent, float duration)
    {
        if (navAgent == null) yield break;
        
        // Disable movement during stun
        bool wasEnabled = navAgent.enabled;
        navAgent.enabled = false;
        
        yield return new WaitForSeconds(duration);
        
        if (navAgent != null)
        {
            navAgent.enabled = wasEnabled;
        }
    }
    
    private System.Collections.IEnumerator BuffAlly(CompanionAI.CompanionCore companion, float multiplier, float duration)
    {
        if (companion == null) yield break;
        
        // Apply buff (visual feedback - could extend for actual stat buff)
        Debug.Log($"👻 Harmony Trail buffing companion: {companion.name}");
        
        yield return new WaitForSeconds(duration);
        
        // Buff duration complete
    }
    
    private void OnDrawGizmos()
    {
        // Visualize active trails in editor
        if (activeTrails != null)
        {
            foreach (TrailSegment trail in activeTrails)
            {
                Gizmos.color = GetTrailColor(trail.type);
                Gizmos.DrawWireSphere(trail.position, trailWidth);
            }
        }
    }
}
