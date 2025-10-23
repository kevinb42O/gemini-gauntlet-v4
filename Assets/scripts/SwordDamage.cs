using UnityEngine;
using System.Collections;
using GeminiGauntlet.Audio;

/// <summary>
/// Simple sword damage component - activated by animation events.
/// Deals damage in a sphere radius when enabled.
/// Ultra simple implementation - no bloat code.
/// </summary>
public class SwordDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("Damage dealt per hit")]
    public float damage = 50f;
    
    [Tooltip("Radius of damage sphere")]
    public float damageRadius = 2f;
    
    [Tooltip("Layers that can be damaged")]
    public LayerMask damageLayerMask = ~0; // All layers by default
    
    [Header("Attack Settings")]
    [Tooltip("Cooldown between attacks in seconds")]
    public float attackCooldown = 0.5f;
    
    [Header("Charged Attack Settings")]
    [Tooltip("Damage dealt by charged power attack (inspector configurable)")]
    public float chargedAttackDamage = 150f;
    
    [Tooltip("Radius of charged attack damage sphere (larger than normal)")]
    public float chargedAttackRadius = 4f;
    
    [Tooltip("Time required to fully charge the attack (seconds)")]
    public float chargeTime = 1.5f;
    
    [Header("Debug")]
    [Tooltip("Show damage sphere in editor")]
    public bool showDebugSphere = true;
    
    private float _lastAttackTime = 0f;
    private Transform _playerRoot; // Cache player root for efficient filtering
    
    void Start()
    {
        // Cache player root transform for efficient filtering
        // Find the root player GameObject (usually has PlayerShooterOrchestrator or similar)
        Transform current = transform;
        while (current.parent != null)
        {
            current = current.parent;
        }
        _playerRoot = current;
        Debug.Log($"[SwordDamage] Cached player root: {_playerRoot.name}");
    }
    
    /// <summary>
    /// Check if a collider belongs to the player (sword, hands, body, etc.)
    /// </summary>
    private bool IsPlayerCollider(Collider col)
    {
        if (col == null) return true; // Safety check
        
        // Check if it's the sword itself
        if (col.transform == transform || col.transform.IsChildOf(transform))
            return true;
        
        // Check if it's part of the player hierarchy
        if (_playerRoot != null && (col.transform == _playerRoot || col.transform.IsChildOf(_playerRoot)))
            return true;
        
        // Check for common player tags/names
        if (col.CompareTag("Player") || col.name.Contains("Hand") || col.name.Contains("Player"))
            return true;
        
        return false;
    }
    
    /// <summary>
    /// Called by animation event to deal damage
    /// This is the entry point - animation triggers this method
    /// </summary>
    public void DealDamage()
    {
        // Check cooldown
        if (Time.time < _lastAttackTime + attackCooldown)
        {
            Debug.Log($"[SwordDamage] On cooldown - {(attackCooldown - (Time.time - _lastAttackTime)):F2}s remaining");
            return;
        }
        
        _lastAttackTime = Time.time;
        
        // STEP 1: Check for damageable targets using damageLayerMask
        Collider[] damageableColliders = Physics.OverlapSphere(transform.position, damageRadius, damageLayerMask);
        
        // STEP 2: Check for ANY colliders (including walls) using all layers
        Collider[] allColliders = Physics.OverlapSphere(transform.position, damageRadius);
        
        Debug.Log($"[SwordDamage] ⚔️ SWORD ATTACK! Position: {transform.position}, Radius: {damageRadius}");
        Debug.Log($"[SwordDamage] Found {damageableColliders.Length} damageable colliders, {allColliders.Length} total colliders");
        
        int damageCount = 0;
        bool hitDamageableObject = false;
        bool hitNonDamageableObject = false;
        
        // Apply damage to all valid targets
        foreach (Collider hit in damageableColliders)
        {
            // Skip if it's the sword itself or player (ROBUST CHECK)
            if (IsPlayerCollider(hit))
            {
                Debug.Log($"[SwordDamage] Skipping player collider: {hit.name}");
                continue;
            }
            
            // Debug what we hit
            Debug.Log($"[SwordDamage] Hit collider: {hit.name} (Layer: {LayerMask.LayerToName(hit.gameObject.layer)})");
            
            // Try to get IDamageable component - check SkullEnemy specifically
            SkullEnemy skull = hit.GetComponent<SkullEnemy>();
            if (skull != null)
            {
                Vector3 damageDirection = (hit.transform.position - transform.position).normalized;
                skull.TakeDamage(damage, hit.transform.position, damageDirection);
                damageCount++;
                hitDamageableObject = true;
                Debug.Log($"[SwordDamage] ✅ DAMAGED SKULL: {hit.name} for {damage} damage!");
                continue;
            }
            
            // Try to get Gem component
            Gem gem = hit.GetComponent<Gem>();
            if (gem != null)
            {
                Vector3 damageDirection = (hit.transform.position - transform.position).normalized;
                gem.TakeDamage(damage, hit.transform.position, damageDirection);
                damageCount++;
                hitDamageableObject = true;
                Debug.Log($"[SwordDamage] ✅ DAMAGED GEM: {hit.name} for {damage} damage!");
                continue;
            }
            
            // Fallback to IDamageable interface for other targets
            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
            {
                Vector3 damageDirection = (hit.transform.position - transform.position).normalized;
                damageable.TakeDamage(damage, hit.transform.position, damageDirection);
                damageCount++;
                hitDamageableObject = true;
                Debug.Log($"[SwordDamage] ✅ DAMAGED (IDamageable): {hit.name} for {damage} damage!");
            }
        }
        
        // Check if we hit any non-damageable objects (walls, props, etc.)
        // CRITICAL: Only check if we didn't hit any damageable objects
        if (!hitDamageableObject)
        {
            foreach (Collider hit in allColliders)
            {
                // Skip if it's the sword itself or player (ROBUST CHECK)
                if (IsPlayerCollider(hit))
                    continue;
                
                // Check if this collider is NOT damageable
                bool isDamageable = hit.GetComponent<SkullEnemy>() != null || 
                                    hit.GetComponent<Gem>() != null || 
                                    hit.GetComponent<IDamageable>() != null;
                
                if (!isDamageable)
                {
                    hitNonDamageableObject = true;
                    Debug.Log($"[SwordDamage] 🔨 Hit non-damageable object: {hit.name} (Layer: {LayerMask.LayerToName(hit.gameObject.layer)})");
                    break; // We only need to know we hit something non-damageable
                }
            }
        }
        
        // SOUND EFFECTS - Play appropriate sound based on what was hit
        if (SoundEventsManager.Events != null)
        {
            if (hitDamageableObject)
            {
                // Hit an enemy or gem - play satisfying impact sound
                Debug.Log($"[SwordDamage] 🎯 Successfully damaged {damageCount} targets! Playing enemy hit sound");
                SoundEventsManager.Events.swordHitEnemy?.Play3D(transform.position);
            }
            else if (hitNonDamageableObject)
            {
                // Hit something but it wasn't damageable (wall, prop, etc.) - play clang sound
                Debug.Log($"[SwordDamage] 🔨 Hit non-damageable object! Playing wall hit sound");
                SoundEventsManager.Events.swordHitWall?.Play3D(transform.position);
            }
            // If we hit nothing at all, no sound plays (sword swings through air)
        }
        
        if (damageCount == 0 && allColliders.Length > 0)
        {
            Debug.LogWarning($"[SwordDamage] ⚠️ No targets damaged (found {allColliders.Length} colliders but none were damageable)");
        }
    }
    
    /// <summary>
    /// Try to attack - returns true if attack was triggered
    /// Called from input handling
    /// </summary>
    public bool TryAttack()
    {
        if (Time.time < _lastAttackTime + attackCooldown)
        {
            return false;
        }
        
        // Animation will call DealDamage() at the right frame
        return true;
    }
    
    /// <summary>
    /// Check if sword is ready to attack (not on cooldown)
    /// </summary>
    public bool IsReady()
    {
        return Time.time >= _lastAttackTime + attackCooldown;
    }
    
    /// <summary>
    /// Deal charged attack damage - called by animation event for power attack
    /// Uses larger radius and higher damage than normal attack
    /// </summary>
    public void DealChargedDamage()
    {
        // Check cooldown
        if (Time.time < _lastAttackTime + attackCooldown)
        {
            Debug.Log($"[SwordDamage] Charged attack on cooldown - {(attackCooldown - (Time.time - _lastAttackTime)):F2}s remaining");
            return;
        }
        
        _lastAttackTime = Time.time;
        
        // STEP 1: Check for damageable targets using damageLayerMask
        Collider[] damageableColliders = Physics.OverlapSphere(transform.position, chargedAttackRadius, damageLayerMask);
        
        // STEP 2: Check for ANY colliders (including walls) using all layers
        Collider[] allColliders = Physics.OverlapSphere(transform.position, chargedAttackRadius);
        
        Debug.Log($"[SwordDamage] ⚔️💥 CHARGED SWORD ATTACK! Position: {transform.position}, Radius: {chargedAttackRadius}, Damage: {chargedAttackDamage}");
        Debug.Log($"[SwordDamage] Found {damageableColliders.Length} damageable colliders, {allColliders.Length} total colliders");
        
        int damageCount = 0;
        bool hitDamageableObject = false;
        bool hitNonDamageableObject = false;
        
        // Apply damage to all valid targets
        foreach (Collider hit in damageableColliders)
        {
            // Skip if it's the sword itself or player (ROBUST CHECK)
            if (IsPlayerCollider(hit))
            {
                Debug.Log($"[SwordDamage] Skipping player collider: {hit.name}");
                continue;
            }
            
            // Debug what we hit
            Debug.Log($"[SwordDamage] Hit collider: {hit.name} (Layer: {LayerMask.LayerToName(hit.gameObject.layer)})");
            
            // Try to get IDamageable component - check SkullEnemy specifically
            SkullEnemy skull = hit.GetComponent<SkullEnemy>();
            if (skull != null)
            {
                Vector3 damageDirection = (hit.transform.position - transform.position).normalized;
                skull.TakeDamage(chargedAttackDamage, hit.transform.position, damageDirection);
                damageCount++;
                hitDamageableObject = true;
                Debug.Log($"[SwordDamage] ✅ CHARGED DAMAGED SKULL: {hit.name} for {chargedAttackDamage} damage!");
                continue;
            }
            
            // Try to get Gem component
            Gem gem = hit.GetComponent<Gem>();
            if (gem != null)
            {
                Vector3 damageDirection = (hit.transform.position - transform.position).normalized;
                gem.TakeDamage(chargedAttackDamage, hit.transform.position, damageDirection);
                damageCount++;
                hitDamageableObject = true;
                Debug.Log($"[SwordDamage] ✅ CHARGED DAMAGED GEM: {hit.name} for {chargedAttackDamage} damage!");
                continue;
            }
            
            // Fallback to IDamageable interface for other targets
            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
            {
                Vector3 damageDirection = (hit.transform.position - transform.position).normalized;
                damageable.TakeDamage(chargedAttackDamage, hit.transform.position, damageDirection);
                damageCount++;
                hitDamageableObject = true;
                Debug.Log($"[SwordDamage] ✅ CHARGED DAMAGED (IDamageable): {hit.name} for {chargedAttackDamage} damage!");
            }
        }
        
        // Check if we hit any non-damageable objects (walls, props, etc.)
        // CRITICAL: Only check if we didn't hit any damageable objects
        if (!hitDamageableObject)
        {
            foreach (Collider hit in allColliders)
            {
                // Skip if it's the sword itself or player (ROBUST CHECK)
                if (IsPlayerCollider(hit))
                    continue;
                
                // Check if this collider is NOT damageable
                bool isDamageable = hit.GetComponent<SkullEnemy>() != null || 
                                    hit.GetComponent<Gem>() != null || 
                                    hit.GetComponent<IDamageable>() != null;
                
                if (!isDamageable)
                {
                    hitNonDamageableObject = true;
                    Debug.Log($"[SwordDamage] 🔨 Charged attack hit non-damageable object: {hit.name} (Layer: {LayerMask.LayerToName(hit.gameObject.layer)})");
                    break; // We only need to know we hit something non-damageable
                }
            }
        }
        
        // SOUND EFFECTS - Play appropriate sound based on what was hit
        if (SoundEventsManager.Events != null)
        {
            if (hitDamageableObject)
            {
                // Hit an enemy or gem - play power attack sound
                Debug.Log($"[SwordDamage] 🎯💥 Successfully damaged {damageCount} targets with CHARGED ATTACK! Playing power attack sound");
                SoundEventsManager.Events.swordPowerAttack?.Play3D(transform.position);
            }
            else if (hitNonDamageableObject)
            {
                // Hit something but it wasn't damageable (wall, prop, etc.) - play wall hit sound
                Debug.Log($"[SwordDamage] 🔨 Charged attack hit non-damageable object! Playing wall hit sound");
                SoundEventsManager.Events.swordHitWall?.Play3D(transform.position);
            }
        }
        
        if (damageCount == 0 && allColliders.Length > 0)
        {
            Debug.LogWarning($"[SwordDamage] ⚠️ No targets damaged by charged attack (found {allColliders.Length} colliders but none were damageable)");
        }
    }
    
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (showDebugSphere)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, damageRadius);
        }
    }
#endif
}
