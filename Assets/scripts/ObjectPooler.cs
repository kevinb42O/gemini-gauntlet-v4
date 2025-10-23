// --- ObjectPooler.cs ---
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For Find

public class ObjectPooler : MonoBehaviour
{
    [System.Serializable]
    public class Pool
    {
        public string tag;          // A string to identify this pool (e.g., "Dagger", "EnemyBullet", "HitSpark")
        public GameObject prefab;   // The prefab to pool
        [Min(0)] public int size;   // Initial size of this pool
        public bool canGrow = true; // Can the pool grow if it runs out of objects?
    }

    public static ObjectPooler Instance { get; private set; }

    public List<Pool> pools; // List of different object pools you want to create (configure in Inspector)
    private Dictionary<string, Queue<GameObject>> _poolDictionary; // Renamed for convention
    private Dictionary<string, Pool> _poolSettingsDictionary; // For quick lookup of pool settings like prefab

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Optional: if your pooler needs to persist across scenes
        }
        else
        {
            Debug.LogWarning($"ObjectPooler: Instance already exists on '{Instance.gameObject.name}'. Destroying duplicate on '{gameObject.name}'.", this);
            Destroy(gameObject);
            return;
        }

        InitializePools();
    }

    void InitializePools()
    {
        _poolDictionary = new Dictionary<string, Queue<GameObject>>();
        _poolSettingsDictionary = new Dictionary<string, Pool>();

        foreach (Pool pool in pools)
        {
            if (pool.prefab == null)
            {
                Debug.LogError($"ObjectPooler: Pool with tag '{pool.tag}' has no Prefab assigned. Skipping this pool.", this);
                continue;
            }
            if (string.IsNullOrEmpty(pool.tag))
            {
                Debug.LogError($"ObjectPooler: A pool has an empty tag (Prefab: {pool.prefab.name}). Skipping this pool.", this);
                continue;
            }
            if (_poolDictionary.ContainsKey(pool.tag))
            {
                Debug.LogWarning($"ObjectPooler: Duplicate pool tag '{pool.tag}'. The first one encountered will be used. Please ensure unique tags.", this);
                continue;
            }


            Queue<GameObject> objectQueue = new Queue<GameObject>();
            GameObject poolHolder = new GameObject(pool.tag + " Pool");
            poolHolder.transform.SetParent(this.transform);

            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab, poolHolder.transform);
                if (obj == null) // Should not happen if prefab is valid
                {
                    Debug.LogError($"ObjectPooler: Failed to instantiate prefab for pool '{pool.tag}'. Check prefab integrity.", pool.prefab);
                    continue;
                }
                obj.name = $"{pool.prefab.name}_Pooled_{i}"; // Make names more descriptive
                obj.SetActive(false);
                IPooledObject pooledObjScript = obj.GetComponent<IPooledObject>();
                pooledObjScript?.OnObjectSpawnedFromPool(false); // False: it's for pool setup, not active use
                objectQueue.Enqueue(obj);
            }
            _poolDictionary.Add(pool.tag, objectQueue);
            _poolSettingsDictionary.Add(pool.tag, pool); // Store settings for growth
            // Debug.Log($"ObjectPooler: Initialized pool '{pool.tag}' with {objectQueue.Count} objects.");
        }
    }

    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (string.IsNullOrEmpty(tag))
        {
            Debug.LogError("ObjectPooler: SpawnFromPool called with a null or empty tag.");
            return null;
        }
        if (!_poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"ObjectPooler: Pool with tag '{tag}' doesn't exist. Cannot spawn.");
            return null;
        }

        Queue<GameObject> objectQueue = _poolDictionary[tag];
        GameObject objectToSpawn = null;

        if (objectQueue.Count > 0)
        {
            objectToSpawn = objectQueue.Dequeue();
            if (objectToSpawn == null) // Object might have been destroyed externally
            {
                Debug.LogError($"ObjectPooler: Dequeued a null object from pool '{tag}'. This indicates an external issue or a bug in return logic. Trying to grow if possible.");
                // Attempt to recover by treating as if queue was empty
                objectQueue.Clear(); // Clear potentially more nulls if this is a systemic issue
                // Fall through to growth logic
            }
        }

        if (objectToSpawn == null) // If still null (was empty or dequeued null)
        {
            Pool currentPoolSettings = _poolSettingsDictionary.ContainsKey(tag) ? _poolSettingsDictionary[tag] : null;
            if (currentPoolSettings != null && currentPoolSettings.canGrow)
            {
                Transform poolHolderTransform = this.transform.Find(tag + " Pool");
                if (poolHolderTransform == null) poolHolderTransform = this.transform;

                if (currentPoolSettings.prefab == null) // Safety check
                {
                    Debug.LogError($"ObjectPooler: Prefab for pool '{tag}' is null in settings, cannot grow pool.", this);
                    return null;
                }
                objectToSpawn = Instantiate(currentPoolSettings.prefab, poolHolderTransform);
                if (objectToSpawn == null)
                {
                    Debug.LogError($"ObjectPooler: Failed to instantiate new object for growing pool '{tag}'.", currentPoolSettings.prefab);
                    return null;
                }
                objectToSpawn.name = $"{currentPoolSettings.prefab.name}_Pooled_Grown";
                Debug.LogWarning($"ObjectPooler: Pool '{tag}' was empty or contained null. Grown by 1. Consider increasing initial size or checking for external object destruction.");
            }
            else
            {
                Debug.LogWarning($"ObjectPooler: Pool '{tag}' is empty or cannot provide a valid object, and cannot grow. Returning null.");
                return null;
            }
        }

        // Configure the spawned object
        objectToSpawn.transform.SetPositionAndRotation(position, rotation);

        if (parent != null)
        {
            objectToSpawn.transform.SetParent(parent, true); // True to keep world position
        }
        else
        {
            Transform poolHolderTransform = this.transform.Find(tag + " Pool");
            if (poolHolderTransform != null && objectToSpawn.transform.parent != poolHolderTransform)
            {
                objectToSpawn.transform.SetParent(poolHolderTransform, true);
            }
        }

        objectToSpawn.SetActive(true);

        IPooledObject pooledObjScript = objectToSpawn.GetComponent<IPooledObject>();
        pooledObjScript?.OnObjectSpawnedFromPool(true); // True: now active for use

        return objectToSpawn;
    }

    public void ReturnToPool(string tag, GameObject objectToReturn)
    {
        if (objectToReturn == null)
        {
            Debug.LogWarning($"ObjectPooler: Attempted to return a null object to pool '{tag}'. Ignoring.");
            return;
        }
        if (string.IsNullOrEmpty(tag))
        {
            Debug.LogError($"ObjectPooler: ReturnToPool called with a null or empty tag for object '{objectToReturn.name}'. Destroying object.", objectToReturn);
            Destroy(objectToReturn);
            return;
        }

        if (!_poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"ObjectPooler: Pool with tag '{tag}' doesn't exist. Cannot return object '{objectToReturn.name}'. Destroying it instead.", objectToReturn);
            Destroy(objectToReturn);
            return;
        }

        // Check if object is already inactive AND in the queue, which would be a double return
        if (!objectToReturn.activeSelf && _poolDictionary[tag].Contains(objectToReturn))
        {
            Debug.LogWarning($"ObjectPooler: Object '{objectToReturn.name}' is already inactive and in pool '{tag}'. Probable double return. Ignoring.", objectToReturn);
            return;
        }


        IPooledObject pooledObjScript = objectToReturn.GetComponent<IPooledObject>();
        pooledObjScript?.OnObjectReturnedToPool();

        objectToReturn.SetActive(false);

        Transform poolHolderTransform = this.transform.Find(tag + " Pool");
        if (poolHolderTransform != null && objectToReturn.transform.parent != poolHolderTransform)
        {
            objectToReturn.transform.SetParent(poolHolderTransform, true);
        }

        // Final check to prevent adding if somehow it got there while active (shouldn't happen)
        if (!_poolDictionary[tag].Contains(objectToReturn))
        {
            _poolDictionary[tag].Enqueue(objectToReturn);
        }
        else if (objectToReturn.activeSelf) // Should have been caught by earlier check if inactive
        {
            Debug.LogError($"ObjectPooler: Object '{objectToReturn.name}' was returned to pool '{tag}' while still active AND already in queue. This is a critical error state.", objectToReturn);
        }
    }

    /// <summary>
    /// Optionally pre-warms a pool or adds more objects to an existing pool.
    /// </summary>
    public void PrewarmPool(string tag, int additionalCount)
    {
        if (string.IsNullOrEmpty(tag) || additionalCount <= 0) return;
        if (!_poolDictionary.ContainsKey(tag) || !_poolSettingsDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"ObjectPooler: Cannot prewarm pool '{tag}' as it does not exist or settings are missing.");
            return;
        }

        Pool poolSettings = _poolSettingsDictionary[tag];
        if (poolSettings.prefab == null)
        {
            Debug.LogError($"ObjectPooler: Prefab for pool '{tag}' is null. Cannot prewarm.", this);
            return;
        }

        Queue<GameObject> objectQueue = _poolDictionary[tag];
        Transform poolHolder = this.transform.Find(tag + " Pool");
        if (poolHolder == null) poolHolder = this.transform; // Fallback

        int currentPoolSize = objectQueue.Count; // This counts available objects, not total ever created.
                                                 // A better count would be poolHolder.childCount if all objects are parented there.

        for (int i = 0; i < additionalCount; i++)
        {
            GameObject obj = Instantiate(poolSettings.prefab, poolHolder);
            if (obj == null) continue;
            obj.name = $"{poolSettings.prefab.name}_Pooled_Prewarm_{currentPoolSize + i}";
            obj.SetActive(false);
            IPooledObject pooledObjScript = obj.GetComponent<IPooledObject>();
            pooledObjScript?.OnObjectSpawnedFromPool(false);
            objectQueue.Enqueue(obj);
        }
        // Debug.Log($"ObjectPooler: Prewarmed pool '{tag}' with {additionalCount} additional objects. New available count: {objectQueue.Count}");
    }
}

/// <summary>
/// Interface for objects that can be pooled.
/// Implementing this allows pooled objects to reset their state when spawned or returned.
/// </summary>
public interface IPooledObject
{
    /// <summary>
    /// Called when the object is retrieved from the pool.
    /// If isActiveAndReady is true, the object is being set active for use.
    /// If false, it's likely during initial pool creation.
    /// </summary>
    void OnObjectSpawnedFromPool(bool isActiveAndReady);

    /// <summary>
    /// Called when the object is returned to the pool and about to be deactivated.
    /// Good for resetting timers, clearing trails, detaching from parents, etc.
    /// </summary>
    void OnObjectReturnedToPool();
}