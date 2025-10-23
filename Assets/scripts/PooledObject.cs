using UnityEngine;

/// <summary>
/// Marker component for pooled instances. Stores the origin prefab and allows custom reset hooks.
/// </summary>
public class PooledObject : MonoBehaviour
{
    [HideInInspector]
    public GameObject originPrefab;

    /// <summary>
    /// Called by PoolManager when the object is spawned from the pool.
    /// </summary>
    public virtual void OnSpawned() {}

    /// <summary>
    /// Called by user scripts before returning to the pool if they need to reset internal state immediately.
    /// </summary>
    public virtual void OnBeforeDespawn() {}
}
