using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GeminiGauntlet.Audio;
using GeminiGauntlet.Missions.Integration;

/// <summary>
/// Manages skull death operations to prevent performance spikes when many skulls die simultaneously.
/// Batches expensive operations (VFX, audio, physics) across multiple frames.
/// </summary>
public class SkullDeathManager : MonoBehaviour
{
    [Header("Performance Settings")]
    [Tooltip("Maximum death effects to spawn per frame")]
    [Range(1, 10)] public int maxDeathEffectsPerFrame = 3;
    
    [Tooltip("Maximum physics operations per frame")]
    [Range(1, 15)] public int maxPhysicsOpsPerFrame = 5;
    
    [Tooltip("Maximum audio calls per frame")]
    [Range(1, 8)] public int maxAudioCallsPerFrame = 4;
    
    [Tooltip("Frame delay between batches")]
    [Range(1, 5)] public int frameDelayBetweenBatches = 1;

    [Header("Audio Limits")]
    [Tooltip("Maximum simultaneous death sounds per frame (NO QUEUING)")]
    [Range(1, 3)] public int maxDeathSoundsPerFrame = 3;
    
    private static SkullDeathManager _instance;
    public static SkullDeathManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Auto-create if doesn't exist
                GameObject go = new GameObject("SkullDeathManager");
                _instance = go.AddComponent<SkullDeathManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // Batching queues
    private Queue<DeathEffectRequest> deathEffectQueue = new Queue<DeathEffectRequest>();
    private Queue<PhysicsDeathRequest> physicsQueue = new Queue<PhysicsDeathRequest>();
    
    // Frame-based audio tracking (NO QUEUING)
    private List<AudioDeathRequest> currentFrameAudioRequests = new List<AudioDeathRequest>();
    private int frameAudioRequestCount = 0;
    
    // Processing flags
    private bool isProcessingDeathEffects = false;
    private bool isProcessingPhysics = false;
    private bool isProcessingAudio = false;

    private struct DeathEffectRequest
    {
        public GameObject deathEffectPrefab;
        public Vector3 position;
        public Quaternion rotation;
    }

    private struct PhysicsDeathRequest
    {
        public Rigidbody rigidbody;
        public bool enableGravity;
        public RigidbodyConstraints removeConstraints;
        public Vector3 torque;
    }

    private struct AudioDeathRequest
    {
        public Vector3 position;
        public float volume;
    }

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Start processing coroutines
        StartCoroutine(ProcessDeathEffectQueue());
        StartCoroutine(ProcessPhysicsQueue());
        StartCoroutine(ProcessFrameAudioRequests());
    }

    /// <summary>
    /// Queue a death effect for batched spawning
    /// </summary>
    public static void QueueDeathEffect(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return;
        
        Instance.deathEffectQueue.Enqueue(new DeathEffectRequest
        {
            deathEffectPrefab = prefab,
            position = position,
            rotation = rotation
        });
    }

    /// <summary>
    /// Queue physics operations for batched processing
    /// </summary>
    public static void QueuePhysicsOperations(Rigidbody rb, Vector3 torque)
    {
        if (rb == null) return;
        
        Instance.physicsQueue.Enqueue(new PhysicsDeathRequest
        {
            rigidbody = rb,
            enableGravity = true,
            removeConstraints = RigidbodyConstraints.None,
            torque = torque
        });
    }

    /// <summary>
    /// Add death audio request for immediate processing this frame (NO QUEUING)
    /// Maximum 3 sounds per frame, excess requests are ignored
    /// </summary>
    public static void QueueDeathAudio(Vector3 position, float volume)
    {
        if (Instance.frameAudioRequestCount >= Instance.maxDeathSoundsPerFrame)
        {
            // Ignore excess requests - maximum 3 per frame
            return;
        }
        
        Instance.currentFrameAudioRequests.Add(new AudioDeathRequest
        {
            position = position,
            volume = volume
        });
        Instance.frameAudioRequestCount++;
    }

    /// <summary>
    /// Process death effects with frame budgeting
    /// </summary>
    private IEnumerator ProcessDeathEffectQueue()
    {
        while (true)
        {
            if (!isProcessingDeathEffects && deathEffectQueue.Count > 0)
            {
                isProcessingDeathEffects = true;
                
                int processed = 0;
                while (deathEffectQueue.Count > 0 && processed < maxDeathEffectsPerFrame)
                {
                    var request = deathEffectQueue.Dequeue();
                    
                    if (request.deathEffectPrefab != null)
                    {
                        var fx = PoolManager.SpawnStatic(request.deathEffectPrefab, request.position, request.rotation);
                        if (fx != null)
                        {
                            PoolManager.DespawnStatic(fx, 2.5f);
                        }
                    }
                    
                    processed++;
                }
                
                // Wait for specified frames before next batch
                for (int i = 0; i < frameDelayBetweenBatches; i++)
                {
                    yield return null;
                }
                
                isProcessingDeathEffects = false;
            }
            
            yield return null;
        }
    }

    /// <summary>
    /// Process physics operations with frame budgeting
    /// </summary>
    private IEnumerator ProcessPhysicsQueue()
    {
        while (true)
        {
            if (!isProcessingPhysics && physicsQueue.Count > 0)
            {
                isProcessingPhysics = true;
                
                int processed = 0;
                while (physicsQueue.Count > 0 && processed < maxPhysicsOpsPerFrame)
                {
                    var request = physicsQueue.Dequeue();
                    
                    if (request.rigidbody != null)
                    {
                        request.rigidbody.useGravity = request.enableGravity;
                        request.rigidbody.constraints = request.removeConstraints;
                        request.rigidbody.AddTorque(request.torque);
                    }
                    
                    processed++;
                }
                
                // Wait for specified frames before next batch
                for (int i = 0; i < frameDelayBetweenBatches; i++)
                {
                    yield return null;
                }
                
                isProcessingPhysics = false;
            }
            
            yield return null;
        }
    }

    /// <summary>
    /// Process frame audio requests IMMEDIATELY (NO QUEUING)
    /// Plays up to 3 death sounds per frame, then resets for next frame
    /// </summary>
    private IEnumerator ProcessFrameAudioRequests()
    {
        while (true)
        {
            // Process all audio requests for this frame IMMEDIATELY
            if (currentFrameAudioRequests.Count > 0)
            {
                foreach (var request in currentFrameAudioRequests)
                {
                    // Play death sound immediately - no delays, no tracking
                    SkullSoundEvents.PlaySkullDeathSound(request.position, request.volume);
                }
                
                // Clear requests for next frame
                currentFrameAudioRequests.Clear();
            }
            
            // Reset frame counter for next frame
            frameAudioRequestCount = 0;
            
            // Wait one frame before processing next batch
            yield return null;
        }
    }

    /// <summary>
    /// Get debug info about queue sizes
    /// </summary>
    public static string GetQueueStatus()
    {
        if (_instance == null) return "SkullDeathManager not initialized";
        
        return $"Death Effects: {_instance.deathEffectQueue.Count}, " +
               $"Physics: {_instance.physicsQueue.Count}, " +
               $"Audio This Frame: {_instance.currentFrameAudioRequests.Count}/{_instance.maxDeathSoundsPerFrame}";
    }

    /// <summary>
    /// Clear all queues (useful for scene transitions)
    /// </summary>
    public static void ClearAllQueues()
    {
        if (_instance != null)
        {
            _instance.deathEffectQueue.Clear();
            _instance.physicsQueue.Clear();
            _instance.currentFrameAudioRequests.Clear();
            _instance.frameAudioRequestCount = 0;
        }
    }

    void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
