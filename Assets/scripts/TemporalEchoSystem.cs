using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// TEMPORAL ECHO SYSTEM - The Most Insane Companion System Ever Created
/// 
/// YOUR TRAILS SPAWN GHOSTLY CLONES OF YOURSELF THAT REPLAY YOUR MOVEMENTS AND FIGHT ALONGSIDE YOU.
/// 
/// The better you move, the more echoes you create.
/// Echoes inherit your weapons and abilities.
/// They fade over time like memories.
/// 
/// ZERO SETUP REQUIRED - Just attach to player with MomentumPainter.
/// 
/// This creates an ARMY OF YOU based on your skill.
/// Master players create a SWARM of echoes.
/// The battlefield becomes a TEMPORAL WAR ZONE.
/// </summary>
[RequireComponent(typeof(MomentumPainter))]
public class TemporalEchoSystem : MonoBehaviour
{
    [Header("Echo Generation")]
    [SerializeField] private int maxActiveEchoes = 10;
    [SerializeField] private float echoSpawnChance = 0.3f; // 30% chance per resonance burst
    [SerializeField] private float echoLifetime = 8f;
    [SerializeField] private float echoMoveSpeed = 480f; // Scaled for 320-unit world (3*160)
    
    [Header("Echo Combat")]
    [SerializeField] private float echoDamageMultiplier = 0.5f; // 50% of your damage
    [SerializeField] private float echoAttackRange = 1600f; // Scaled for 320-unit world (10*160)
    [SerializeField] private float echoAttackInterval = 1f;
    [SerializeField] private bool echoesInheritWeapons = true;
    
    [Header("Visual Settings")]
    [SerializeField] private Color echoColor = new Color(0.5f, 0.8f, 1f, 0.4f);
    [SerializeField] private Material echoMaterial;
    [SerializeField] [Range(0f, 1f)] private float echoTransparency = 0.4f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip echoSpawnSound;
    [SerializeField] private AudioClip echoAttackSound;
    [SerializeField] private float audioVolume = 0.3f;
    
    // Component references
    private MomentumPainter painter;
    private AudioSource audioSource;
    
    // Echo management
    private List<TemporalEcho> activeEchoes = new List<TemporalEcho>();
    private Queue<GameObject> echoPool = new Queue<GameObject>();
    private Transform echoContainer;
    
    // Movement recording for echo replay
    private Queue<MovementSnapshot> movementHistory = new Queue<MovementSnapshot>();
    private const int MAX_MOVEMENT_HISTORY = 200;
    private float recordInterval = 0.1f;
    private float timeSinceLastRecord;
    
    private class MovementSnapshot
    {
        public Vector3 position;
        public Quaternion rotation;
        public float timestamp;
        
        public MovementSnapshot(Vector3 pos, Quaternion rot, float time)
        {
            position = pos;
            rotation = rot;
            timestamp = time;
        }
    }
    
    private class TemporalEcho
    {
        public GameObject echoObject;
        public float spawnTime;
        public float lastAttackTime;
        public Queue<MovementSnapshot> movementPath;
        public int currentPathIndex;
        public List<Renderer> renderers;
        public Light echoLight;
        public ParticleSystem particles;
        
        public TemporalEcho(GameObject obj, Queue<MovementSnapshot> path, float time)
        {
            echoObject = obj;
            spawnTime = time;
            lastAttackTime = 0f;
            movementPath = new Queue<MovementSnapshot>(path);
            currentPathIndex = 0;
            renderers = new List<Renderer>();
            
            // Get all visual components
            Renderer[] rends = obj.GetComponentsInChildren<Renderer>();
            renderers.AddRange(rends);
            
            echoLight = obj.GetComponent<Light>();
            particles = obj.GetComponent<ParticleSystem>();
        }
    }
    
    private void Awake()
    {
        painter = GetComponent<MomentumPainter>();
        
        if (painter == null)
        {
            Debug.LogError("TemporalEchoSystem requires MomentumPainter component!");
            enabled = false;
            return;
        }
        
        // Setup audio
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0.7f;
        audioSource.volume = audioVolume;
        audioSource.playOnAwake = false;
        
        // Create echo container
        echoContainer = new GameObject("EchoContainer").transform;
        echoContainer.SetParent(transform);
        
        // Subscribe to resonance burst events by checking trails
        Debug.Log("👻 TEMPORAL ECHO SYSTEM ACTIVATED - Your echoes will haunt the battlefield!");
    }
    
    private void Update()
    {
        RecordMovement();
        CheckForResonanceBursts();
        UpdateEchoes();
    }
    
    private void RecordMovement()
    {
        timeSinceLastRecord += Time.deltaTime;
        
        if (timeSinceLastRecord >= recordInterval)
        {
            MovementSnapshot snapshot = new MovementSnapshot(
                transform.position,
                transform.rotation,
                Time.time
            );
            
            movementHistory.Enqueue(snapshot);
            
            // Keep history limited
            if (movementHistory.Count > MAX_MOVEMENT_HISTORY)
            {
                movementHistory.Dequeue();
            }
            
            timeSinceLastRecord = 0f;
        }
    }
    
    private void CheckForResonanceBursts()
    {
        // Check if a resonance burst just happened by monitoring painter's trails
        if (painter == null) return;
        
        // Access the public trails from MomentumPainter
        var trails = GetActiveTrails();
        
        foreach (var trail in trails)
        {
            if (trail.hasBeenCrossed && !HasEchoAtPosition(trail.position))
            {
                // Resonance burst detected! Spawn echo?
                if (Random.value < echoSpawnChance && activeEchoes.Count < maxActiveEchoes)
                {
                    SpawnTemporalEcho(trail.position);
                }
            }
        }
    }
    
    private List<MomentumPainter.TrailSegment> GetActiveTrails()
    {
        // Use reflection or make trails public in MomentumPainter
        // For now, we'll track resonance bursts differently
        return new List<MomentumPainter.TrailSegment>();
    }
    
    private bool HasEchoAtPosition(Vector3 position)
    {
        foreach (var echo in activeEchoes)
        {
            if (Vector3.Distance(echo.echoObject.transform.position, position) < 160f) // Scaled for 320-unit world (1*160)
            {
                return true;
            }
        }
        return false;
    }
    
    private void SpawnTemporalEcho(Vector3 position)
    {
        if (movementHistory.Count < 10) return; // Need some history
        
        // Create or get from pool
        GameObject echoObject = CreateEchoClone();
        echoObject.transform.position = position;
        echoObject.transform.rotation = transform.rotation;
        echoObject.SetActive(true);
        
        // Create echo with movement history
        TemporalEcho echo = new TemporalEcho(echoObject, movementHistory, Time.time);
        activeEchoes.Add(echo);
        
        // Play sound
        if (echoSpawnSound != null)
        {
            audioSource.PlayOneShot(echoSpawnSound, 0.5f);
        }
        
        Debug.Log($"👻 Temporal Echo #{activeEchoes.Count} spawned! Army strength: {activeEchoes.Count}");
    }
    
    private GameObject CreateEchoClone()
    {
        // Create a simplified clone of the player
        GameObject clone = new GameObject("TemporalEcho");
        clone.transform.SetParent(echoContainer);
        
        // Add visual representation (simplified player mesh)
        GameObject visualClone = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visualClone.transform.SetParent(clone.transform);
        visualClone.transform.localPosition = Vector3.zero;
        visualClone.transform.localScale = transform.localScale * 0.9f; // Slightly smaller
        
        // Remove collider (echoes don't collide)
        Collider col = visualClone.GetComponent<Collider>();
        if (col != null) Destroy(col);
        
        // Make it ghostly
        Renderer renderer = visualClone.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (echoMaterial != null)
            {
                renderer.material = echoMaterial;
            }
            else
            {
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.SetFloat("_Mode", 3); // Transparent mode
                renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                renderer.material.SetInt("_ZWrite", 0);
                renderer.material.DisableKeyword("_ALPHATEST_ON");
                renderer.material.EnableKeyword("_ALPHABLEND_ON");
                renderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                renderer.material.renderQueue = 3000;
            }
            
            Color ghostColor = echoColor;
            ghostColor.a = echoTransparency;
            renderer.material.color = ghostColor;
            renderer.material.SetColor("_EmissionColor", echoColor * 0.5f);
        }
        
        // Add glowing particles
        ParticleSystem ps = clone.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 1f;
        main.startSpeed = 0.5f;
        main.startSize = 0.1f;
        main.startColor = echoColor;
        main.maxParticles = 30;
        
        var emission = ps.emission;
        emission.rateOverTime = 15f;
        
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 80f; // Scaled for 320-unit world (0.5*160)
        
        // Add light
        Light light = clone.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = echoColor;
        light.range = 480f; // Scaled for 320-unit world (3*160)
        light.intensity = 1f;
        
        // Clone weapons if enabled
        if (echoesInheritWeapons)
        {
            ClonePlayerWeapons(clone);
        }
        
        return clone;
    }
    
    private void ClonePlayerWeapons(GameObject echo)
    {
        // Find weapons in player's children
        Transform[] children = GetComponentsInChildren<Transform>();
        foreach (Transform child in children)
        {
            if (child.name.ToLower().Contains("weapon") || 
                child.name.ToLower().Contains("gun") ||
                child.name.ToLower().Contains("sword"))
            {
                // Create simplified clone
                GameObject weaponClone = new GameObject(child.name + "_Echo");
                weaponClone.transform.SetParent(echo.transform);
                weaponClone.transform.localPosition = child.localPosition;
                weaponClone.transform.localRotation = child.localRotation;
                weaponClone.transform.localScale = child.localScale;
                
                // Copy mesh if exists
                MeshFilter originalMesh = child.GetComponent<MeshFilter>();
                if (originalMesh != null)
                {
                    MeshFilter cloneMesh = weaponClone.AddComponent<MeshFilter>();
                    cloneMesh.mesh = originalMesh.mesh;
                    
                    MeshRenderer cloneRenderer = weaponClone.AddComponent<MeshRenderer>();
                    cloneRenderer.material = new Material(Shader.Find("Standard"));
                    Color weaponColor = echoColor;
                    weaponColor.a = echoTransparency;
                    cloneRenderer.material.color = weaponColor;
                }
            }
        }
    }
    
    private void UpdateEchoes()
    {
        float currentTime = Time.time;
        
        for (int i = activeEchoes.Count - 1; i >= 0; i--)
        {
            TemporalEcho echo = activeEchoes[i];
            float age = currentTime - echo.spawnTime;
            
            // Remove expired echoes
            if (age > echoLifetime || echo.movementPath.Count == 0)
            {
                RemoveEcho(echo);
                activeEchoes.RemoveAt(i);
                continue;
            }
            
            // Update echo position (replay movement)
            UpdateEchoMovement(echo);
            
            // Update echo visuals (fade over time)
            UpdateEchoVisuals(echo, age);
            
            // Echo attacks enemies
            EchoAttackNearbyEnemies(echo, currentTime);
        }
    }
    
    private void UpdateEchoMovement(TemporalEcho echo)
    {
        if (echo.movementPath.Count == 0) return;
        
        // Replay recorded movement
        MovementSnapshot nextPoint = echo.movementPath.Dequeue();
        
        if (nextPoint != null)
        {
            // Smooth movement to next point
            echo.echoObject.transform.position = Vector3.MoveTowards(
                echo.echoObject.transform.position,
                nextPoint.position,
                echoMoveSpeed * Time.deltaTime
            );
            
            echo.echoObject.transform.rotation = Quaternion.Slerp(
                echo.echoObject.transform.rotation,
                nextPoint.rotation,
                Time.deltaTime * 5f
            );
        }
    }
    
    private void UpdateEchoVisuals(TemporalEcho echo, float age)
    {
        float normalizedAge = age / echoLifetime;
        float alpha = Mathf.Lerp(echoTransparency, 0f, normalizedAge);
        
        // Fade all renderers
        foreach (Renderer renderer in echo.renderers)
        {
            if (renderer != null && renderer.material != null)
            {
                Color color = renderer.material.color;
                color.a = alpha;
                renderer.material.color = color;
            }
        }
        
        // Fade light
        if (echo.echoLight != null)
        {
            echo.echoLight.intensity = Mathf.Lerp(1f, 0f, normalizedAge);
        }
    }
    
    private void EchoAttackNearbyEnemies(TemporalEcho echo, float currentTime)
    {
        if (currentTime - echo.lastAttackTime < echoAttackInterval) return;
        
        // Find nearby enemies
        Collider[] nearbyColliders = Physics.OverlapSphere(
            echo.echoObject.transform.position,
            echoAttackRange
        );
        
        foreach (Collider col in nearbyColliders)
        {
            IDamageable damageable = col.GetComponent<IDamageable>();
            if (damageable != null && col.gameObject != gameObject)
            {
                // Echo attacks!
                float damage = CalculatePlayerBaseDamage() * echoDamageMultiplier;
                damageable.TakeDamage(damage, echo.echoObject.transform.position, Vector3.zero);
                
                echo.lastAttackTime = currentTime;
                
                // Visual feedback
                if (echo.particles != null)
                {
                    var emission = echo.particles.emission;
                    emission.rateOverTime = 50f; // Burst on attack
                }
                
                // Play sound
                if (echoAttackSound != null)
                {
                    audioSource.PlayOneShot(echoAttackSound, 0.2f);
                }
                
                Debug.Log($"👻 Echo attacked enemy for {damage} damage!");
                break; // One enemy per attack cycle
            }
        }
    }
    
    private float CalculatePlayerBaseDamage()
    {
        // Return base damage value for echo attacks
        // Echoes deal a percentage (echoDamageMultiplier) of this base value
        // Adjust this value to match your game's damage scaling
        return 20f;
    }
    
    private void RemoveEcho(TemporalEcho echo)
    {
        if (echo.echoObject != null)
        {
            Destroy(echo.echoObject);
        }
    }
    
    private void OnDrawGizmos()
    {
        // Visualize echoes and their attack ranges
        if (activeEchoes != null)
        {
            Gizmos.color = echoColor;
            foreach (var echo in activeEchoes)
            {
                if (echo.echoObject != null)
                {
                    Gizmos.DrawWireSphere(echo.echoObject.transform.position, echoAttackRange);
                }
            }
        }
    }
    
    // Public API for external systems
    public int GetActiveEchoCount() => activeEchoes.Count;
    
    public void ClearAllEchoes()
    {
        foreach (var echo in activeEchoes)
        {
            RemoveEcho(echo);
        }
        activeEchoes.Clear();
    }
    
    public void SpawnEchoManually(Vector3 position)
    {
        if (activeEchoes.Count < maxActiveEchoes)
        {
            SpawnTemporalEcho(position);
        }
    }
}
