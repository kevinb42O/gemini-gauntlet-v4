using System.Collections.Generic;
using UnityEngine;
using System.Collections;

/// <summary>
/// Simple, safe object pool manager keyed by prefab. Auto-creates itself if missing.
/// Use PoolManager.Spawn(prefab, pos, rot[, parent]) and PoolManager.Despawn(go[, delay]).
/// </summary>
public class PoolManager : MonoBehaviour
{
    private static PoolManager _instance;
    public static PoolManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var existing = FindObjectOfType<PoolManager>();
                if (existing != null)
                {
                    _instance = existing;
                }
                else
                {
                    var go = new GameObject("PoolManager");
                    _instance = go.AddComponent<PoolManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    private class Pool
    {
        public readonly GameObject Prefab;
        public readonly Queue<GameObject> Queue = new Queue<GameObject>();
        public readonly Transform Root;
        public Pool(GameObject prefab, Transform root)
        {
            Prefab = prefab;
            Root = root;
        }
    }

    private readonly Dictionary<GameObject, Pool> _poolsByPrefab = new Dictionary<GameObject, Pool>();
    private readonly Dictionary<GameObject, Pool> _poolsByInstance = new Dictionary<GameObject, Pool>();

    public void Prewarm(GameObject prefab, int count, Transform parent = null)
    {
        if (prefab == null || count <= 0) return;
        var pool = GetOrCreatePool(prefab, parent);
        for (int i = 0; i < count; i++)
        {
            var go = CreateNew(prefab, pool);
            go.SetActive(false);
            pool.Queue.Enqueue(go);
        }
    }

    public static void PrewarmStatic(GameObject prefab, int count, Transform parent = null)
    {
        Instance.Prewarm(prefab, count, parent);
    }

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (prefab == null) return null;
        var pool = GetOrCreatePool(prefab, parent);
        GameObject go = pool.Queue.Count > 0 ? pool.Queue.Dequeue() : CreateNew(prefab, pool);
        var t = go.transform;
        t.SetParent(parent != null ? parent : pool.Root, false);
        t.SetPositionAndRotation(position, rotation);
        go.SetActive(true);
        var po = go.GetComponent<PooledObject>();
        if (po == null) po = go.AddComponent<PooledObject>();
        po.originPrefab = prefab;
        po.OnSpawned();
        _poolsByInstance[go] = pool;
        return go;
    }

    public static GameObject SpawnStatic(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        return Instance.Spawn(prefab, position, rotation, parent);
    }

    public void Despawn(GameObject instance)
    {
        if (instance == null) return;
        if (!_poolsByInstance.TryGetValue(instance, out var pool))
        {
            // Not pooled, just destroy
            Destroy(instance);
            return;
        }
        var po = instance.GetComponent<PooledObject>();
        if (po != null)
        {
            po.OnBeforeDespawn();
        }
        instance.SetActive(false);
        instance.transform.SetParent(pool.Root, false);
        var rb = instance.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        pool.Queue.Enqueue(instance);
    }

    public void Despawn(GameObject instance, float delay)
    {
        if (delay <= 0f)
        {
            Despawn(instance);
        }
        else
        {
            StartCoroutine(DespawnDelayed(instance, delay));
        }
    }

    public static void DespawnStatic(GameObject instance, float delay = 0f)
    {
        Instance.Despawn(instance, delay);
    }

    private IEnumerator DespawnDelayed(GameObject instance, float delay)
    {
        yield return new WaitForSeconds(delay);
        Despawn(instance);
    }

    private Pool GetOrCreatePool(GameObject prefab, Transform parent)
    {
        if (!_poolsByPrefab.TryGetValue(prefab, out var pool))
        {
            Transform root = new GameObject($"Pool_{prefab.name}").transform;
            root.SetParent(transform, false);
            pool = new Pool(prefab, root);
            _poolsByPrefab[prefab] = pool;
        }
        return pool;
    }

    private GameObject CreateNew(GameObject prefab, Pool pool)
    {
        var go = Instantiate(prefab, pool.Root);
        var po = go.GetComponent<PooledObject>();
        if (po == null) po = go.AddComponent<PooledObject>();
        po.originPrefab = prefab;
        go.SetActive(false);
        return go;
    }
}
