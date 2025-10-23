using System.Reflection;
using UnityEngine;

namespace VFX
{
    public static class ShotgunVFXSpawner
    {
        public static GameObject Spawn(GameObject vfxPrefab, Transform emitPoint, Vector3 forward, float speed, bool debugLogs)
        {
            if (vfxPrefab == null || emitPoint == null)
            {
                Debug.LogWarning("[ShotgunVFXSpawner] Missing prefab or emitPoint");
                return null;
            }

            Quaternion rotation = Quaternion.LookRotation(forward);
            // Slightly offset spawn forward to avoid intersecting the player's capsule/ground when grounded
            const float spawnOffset = 0.2f;
            Vector3 spawnPos = emitPoint.position + forward * spawnOffset;
            GameObject go = Object.Instantiate(vfxPrefab, spawnPos, rotation);

            try
            {
                RemoveLegacyTrackers(go);
                // Prevent immediate collision with the player/hand colliders
                IgnoreSelfCollisions(go, emitPoint);

                var magic = go.GetComponent<MagicArsenal.MagicProjectileScript>();
                var rb = go.GetComponent<Rigidbody>();
                bool usedRB = false;

                if (magic != null)
                {
                    if (rb == null)
                    {
                        Debug.LogWarning("[ShotgunVFXSpawner] MagicProjectile present but no Rigidbody. Using translate movement and disabling script.");
                        magic.enabled = false;
                        AttachTranslateMover(go.transform, forward, speed, 3f);
                    }
                    else
                    {
                        if (rb.isKinematic) rb.isKinematic = false;
                        rb.useGravity = false; // straight travel
                        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                        rb.interpolation = RigidbodyInterpolation.Interpolate;
                        SetRigidbodyVelocity(rb, forward * speed);
                        usedRB = true;
                    }
                    // Tighten collision size to avoid early ground hits when grounded
                    try
                    {
                        var sphere = go.GetComponent<SphereCollider>();
                        if (sphere != null)
                        {
                            sphere.radius = Mathf.Min(sphere.radius, 0.08f);
                        }
                        // Reduce MagicProjectile fallback radius and offset
                        magic.colliderRadius = Mathf.Min(magic.colliderRadius, 0.08f);
                        magic.collideOffset = Mathf.Min(magic.collideOffset, 0.03f);
                    }
                    catch { /* non-fatal */ }
                }
                else
                {
                    if (rb != null && !rb.isKinematic)
                    {
                        SetRigidbodyVelocity(rb, forward * speed);
                        usedRB = true;
                    }
                    else
                    {
                        AttachTranslateMover(go.transform, forward, speed, 3f);
                    }
                }

                if (debugLogs)
                {
                    Debug.Log($"[ShotgunVFXSpawner] Spawned at {emitPoint.position}, speed {speed:F1} via {(usedRB ? "Rigidbody" : "Translate")} {(magic != null ? "[MagicProjectile]" : "")}");
                }

                // Lifetime: MagicProjectile manages its own; else auto-destroy
                if (magic == null)
                {
                    Object.Destroy(go, 3f);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ShotgunVFXSpawner] Error: {e.Message}");
            }

            return go;
        }

        private static void RemoveLegacyTrackers(GameObject root)
        {
            var t = root.GetComponent<LegacyVFXTracker>();
            if (t != null) Object.Destroy(t);
            var ts = root.GetComponentsInChildren<LegacyVFXTracker>();
            foreach (var c in ts) Object.Destroy(c);
        }

        private static void AttachTranslateMover(Transform t, Vector3 dir, float speed, float life)
        {
            var mover = t.gameObject.AddComponent<TranslateMover>();
            mover.Init(dir, speed, life);
        }

        private static void SetRigidbodyVelocity(Rigidbody rb, Vector3 v)
        {
            // Prefer linearVelocity if available; otherwise use standard velocity
            var linProp = rb.GetType().GetProperty("linearVelocity", BindingFlags.Public | BindingFlags.Instance);
            if (linProp != null && linProp.CanWrite)
            {
                linProp.SetValue(rb, v, null);
                return;
            }
            rb.linearVelocity = v;
        }

        private static void IgnoreSelfCollisions(GameObject projectile, Transform emitPoint)
        {
            if (projectile == null || emitPoint == null) return;
            var playerRoot = emitPoint.root;
            if (playerRoot == null) return;

            var projCols = projectile.GetComponentsInChildren<Collider>(true);
            var playerCols = playerRoot.GetComponentsInChildren<Collider>(true);
            if (projCols == null || playerCols == null) return;

            foreach (var pc in projCols)
            {
                if (pc == null) continue;
                foreach (var hc in playerCols)
                {
                    if (hc == null) continue;
                    Physics.IgnoreCollision(pc, hc, true);
                }
            }
        }

        private class TranslateMover : MonoBehaviour
        {
            private Vector3 _dir;
            private float _speed;
            private float _endTime;

            public void Init(Vector3 dir, float speed, float life)
            {
                _dir = dir;
                _speed = speed;
                _endTime = Time.time + life;
            }

            private void Update()
            {
                transform.position += _dir * _speed * Time.deltaTime;
                if (Time.time >= _endTime)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
