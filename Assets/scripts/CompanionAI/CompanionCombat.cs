using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CompanionAI
{
    /// <summary>
    /// Controls all combat behaviour for the modular companion system.
    /// Handles weapon selection, damage application, audio and animation hooks.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public class CompanionCombat : MonoBehaviour
    {
        [Header("Weapon Emit Points")]
        public Transform leftHandEmitPoint;
        public Transform rightHandEmitPoint;

        [Header("Weapon Visuals")]
        public GameObject shotgunParticlePrefab;
        public GameObject streamParticlePrefab;
        
        [Header("Direct Particle Systems (Recommended)")]
        [Tooltip("Particle system already on left hand emit point (shotgun)")]
        public ParticleSystem shotgunParticleSystem;
        [Tooltip("Particle system already on right hand emit point (beam)")]
        public ParticleSystem streamParticleSystem;
        

        [Header("Damage Settings")]
        [Range(100f, 5000f)] public float streamThreshold = 1200f;
        [Range(0.05f, 1.5f)] public float shotgunCooldown = 0.25f;
        [Range(5f, 500f)] public float streamDamage = 40f;
        [Range(10f, 1000f)] public float shotgunDamage = 120f;
        [Range(0.2f, 2f)] public float beamCooldown = 0.5f;
        [Range(0.2f, 5f)] public float beamDuration = 2.5f;
        [Range(25f, 500f)] public float damageRadius = 150f;
        public bool useAreaDamage = true;

        [Header("Target Tracking")]
        [Range(1f, 50f)] public float targetTrackingSpeedMultiplier = 10f;

        [Header("Debug")]
        public bool enableDebugLogs = false;


        private CompanionCore _core;
        private CompanionTargeting _targeting;
        private CompanionHandAnimator _handAnimator;

        private Coroutine _combatLoop;
        
        // ⚡ PERFORMANCE: Cache player transform to avoid FindObjectOfType every frame!
        private Transform _cachedPlayerTransform;
        private float _lastPlayerCacheTime = -999f;
        private const float PLAYER_CACHE_DURATION = 2f; // Re-cache every 2 seconds
        private Coroutine _streamDamageLoop;
        private Coroutine _losMonitorLoop;

        private GameObject _activeStreamEffect;
        private ParticleSystem _streamParticleSystem;

        private readonly Collider[] _areaBuffer = new Collider[12];

        private bool _isAttacking;
        private float _lastShotgunTime;
        private float _lastBeamToggle;

        public void Initialize(CompanionCore core)
        {
            _core = core;
            _targeting = core.TargetingSystem ?? GetComponent<CompanionTargeting>();

            _handAnimator = GetComponent<CompanionHandAnimator>();
            if (_handAnimator == null)
            {
                _handAnimator = GetComponentInChildren<CompanionHandAnimator>();
            }

            if (enableDebugLogs)
            {
                Debug.Log("[CompanionCombat] Initialized combat system");
            }
        }
        
        public void StartAttacking()
        {
            if (_isAttacking) return;

            _isAttacking = true;
            if (_combatLoop == null)
            {
                _combatLoop = StartCoroutine(CombatLoop());
            }
        }

        public void StopAttacking()
        {
            if (!_isAttacking) return;

            _isAttacking = false;

            if (_combatLoop != null)
            {
                StopCoroutine(_combatLoop);
                _combatLoop = null;
            }

            StopStreamAttack();
            _handAnimator?.PlayIdleBoth();
        }

        public void Cleanup()
        {
            StopAttacking();
        }

        private IEnumerator CombatLoop()
        {
            var wait = new WaitForSeconds(0.05f);

            while (_isAttacking && enabled && _core != null && !_core.IsDead)
            {
                Transform target = _targeting?.GetCurrentTarget();

                if (target == null)
                {
                    StopStreamAttack();
                    yield return wait;
                    continue;
                }

                // 🔥 CRITICAL FIX: Check line of sight BEFORE shooting!
                // If we can't see the target, DON'T SHOOT!
                if (!HasLineOfSightToTarget(target))
                {
                    // No LOS - stop shooting and wait
                    StopStreamAttack();
                    
                    if (enableDebugLogs)
                    {
                        Debug.Log($"[CompanionCombat] 🚫 NO LINE OF SIGHT to {target.name} - STOPPED SHOOTING!");
                    }
                    
                    yield return wait;
                    continue;
                }

                FaceTarget(target);

                float distance = Vector3.Distance(transform.position, target.position);

                // DUAL WIELDING: Always keep beam active, add shotgun at close range
                StartStreamAttack(target);
                
                if (distance <= streamThreshold * 0.8f)
                {
                    // Close range: BOTH weapons firing!
                    TryShotgunAttack(target);
                }

                yield return wait;
            }

            // Ensure we fully stop when loop exits
            StopStreamAttack();
            _handAnimator?.PlayIdleBoth();
        }
        
        /// <summary>
        /// 🔥 CRITICAL: Check if we have line of sight to target
        /// Shoots a raycast to see if anything blocks our view
        /// 
        /// SPECIAL CASE: For enemy companions targeting player with fake offset targets,
        /// we need to check LOS to the REAL player, not the fake offset position!
        /// 
        /// ⚡ OPTIMIZED: Caches player transform to avoid expensive FindObjectOfType calls!
        /// </summary>
        private bool HasLineOfSightToTarget(Transform target)
        {
            if (target == null) return false;
            
            // 🎯 SPECIAL CASE: If target name contains "FakeTarget", find the real player!
            // Enemy companions use fake targets for aim inaccuracy, but we need to check LOS to REAL player
            Transform realTarget = target;
            if (target.name.Contains("FakeTarget"))
            {
                // ⚡ PERFORMANCE: Use cached player transform instead of FindObjectOfType!
                if (_cachedPlayerTransform == null || Time.time - _lastPlayerCacheTime > PLAYER_CACHE_DURATION)
                {
                    // Cache expired or doesn't exist - find player
                    AAAMovementController playerController = FindObjectOfType<AAAMovementController>();
                    if (playerController != null)
                    {
                        _cachedPlayerTransform = playerController.transform;
                        _lastPlayerCacheTime = Time.time;
                        
                        if (enableDebugLogs)
                        {
                            Debug.Log($"[CompanionCombat] 🎯 Cached player transform for performance!");
                        }
                    }
                }
                
                // Use cached player transform
                if (_cachedPlayerTransform != null)
                {
                    realTarget = _cachedPlayerTransform;
                }
            }
            
            // Shoot raycast from our position to REAL target
            Vector3 startPos = transform.position + Vector3.up * 160f; // Eye height
            Vector3 targetPos = realTarget.position + Vector3.up * 160f; // Target center
            Vector3 direction = (targetPos - startPos).normalized;
            float distance = Vector3.Distance(startPos, targetPos);
            
            RaycastHit hit;
            if (Physics.Raycast(startPos, direction, out hit, distance))
            {
                // Check if we hit the target or something else
                if (hit.transform == realTarget || hit.transform.IsChildOf(realTarget))
                {
                    // Hit the target - clear LOS!
                    if (enableDebugLogs)
                    {
                        Debug.Log($"[CompanionCombat] ✅ LOS CLEAR to {realTarget.name} at {hit.distance:F0} units");
                    }
                    return true;
                }
                else
                {
                    // Hit something else (wall) - blocked!
                    if (enableDebugLogs)
                    {
                        Debug.Log($"[CompanionCombat] 🚫 LOS BLOCKED by {hit.collider.name} at {hit.distance:F0} units");
                    }
                    return false;
                }
            }
            
            // Raycast missed - no LOS
            if (enableDebugLogs)
            {
                Debug.Log($"[CompanionCombat] ⚠️ LOS RAYCAST MISSED (target out of range?)");
            }
            return false;
        }

        private void TryShotgunAttack(Transform target)
        {
            if (Time.time - _lastShotgunTime < shotgunCooldown)
            {
                return;
            }

            _lastShotgunTime = Time.time;

            if (leftHandEmitPoint == null)
            {
                if (enableDebugLogs)
                {
                    Debug.LogWarning("[CompanionCombat] Left hand emit point missing - cannot fire shotgun");
                }
                return;
            }

            // Check what's assigned
            if (enableDebugLogs)
            {
                Debug.Log($"[CompanionCombat] 🔍 Shotgun setup - Direct PS: {(shotgunParticleSystem != null ? "ASSIGNED" : "NULL")}, Prefab: {(shotgunParticlePrefab != null ? "ASSIGNED" : "NULL")}");
            }
            
            // PRIORITY 1: Use direct particle system reference (if assigned)
            if (shotgunParticleSystem != null)
            {
                shotgunParticleSystem.gameObject.SetActive(true);
                var emission = shotgunParticleSystem.emission;
                emission.enabled = true;
                shotgunParticleSystem.Clear();
                shotgunParticleSystem.Play(true);
                
                if (enableDebugLogs)
                {
                    Debug.Log($"[CompanionCombat] ✅ Playing direct shotgun PS");
                }
            }
            // PRIORITY 2: Instantiate prefab
            else if (shotgunParticlePrefab != null)
            {
                // Calculate direction to target for proper aiming
                Vector3 shootDirection = leftHandEmitPoint.forward;
                if (target != null)
                {
                    // Aim at center mass/head (160 units up for player height 320)
                    Vector3 targetCenter = target.position + Vector3.up * 160f;
                    shootDirection = (targetCenter - leftHandEmitPoint.position).normalized;
                }
                
                GameObject shotgunFx = Instantiate(
                    shotgunParticlePrefab,
                    leftHandEmitPoint.position,
                    Quaternion.LookRotation(shootDirection));
                
                if (enableDebugLogs)
                {
                    Debug.Log($"[CompanionCombat] 🔫 Instantiated shotgun prefab at {leftHandEmitPoint.position}");
                }
                
                // Force all particle systems to play
                ParticleSystem[] particles = shotgunFx.GetComponentsInChildren<ParticleSystem>(true);
                
                if (enableDebugLogs)
                {
                    Debug.Log($"[CompanionCombat] 🔍 Found {particles.Length} particle systems in shotgun prefab");
                }
                
                foreach (ParticleSystem ps in particles)
                {
                    if (ps != null)
                    {
                        ps.gameObject.SetActive(true);
                        var emission = ps.emission;
                        emission.enabled = true;
                        ps.Clear();
                        ps.Play(true);
                        
                        if (enableDebugLogs)
                        {
                            Debug.Log($"[CompanionCombat] 🎆 Shotgun particle '{ps.name}' - Active: {ps.gameObject.activeSelf}, Playing: {ps.isPlaying}, Emission: {ps.emission.enabled}, StartColor: {ps.main.startColor.color}");
                        }
                    }
                }
                
                // Destroy after particles finish
                float maxDuration = 2f;
                foreach (ParticleSystem ps in particles)
                {
                    if (ps != null && ps.main.duration > maxDuration)
                    {
                        maxDuration = ps.main.duration + ps.main.startLifetime.constantMax;
                    }
                }
                
                Destroy(shotgunFx, maxDuration);
                
                if (enableDebugLogs)
                {
                    Debug.Log($"[CompanionCombat] 🔫 Shotgun prefab instantiated with {particles.Length} particle systems");
                }
            }
            else if (enableDebugLogs)
            {
                Debug.LogWarning("[CompanionCombat] ❌ No shotgun particle system or prefab assigned!");
            }

            _handAnimator?.PlayShotgunLeft();
            _core?.PlayShotgunSound();

            ApplyDamage(target, shotgunDamage, useAreaDamage);
        }

        private void StartStreamAttack(Transform target)
        {
            if (rightHandEmitPoint == null)
            {
                if (enableDebugLogs)
                {
                    Debug.LogWarning("[CompanionCombat] Right hand emit point missing - cannot fire beam");
                }
                return;
            }

            // Use direct particle system reference
            if (streamParticleSystem != null)
            {
                // Make sure it's enabled and active
                if (!streamParticleSystem.gameObject.activeSelf)
                {
                    streamParticleSystem.gameObject.SetActive(true);
                }
                
                // Force emission on
                var emission = streamParticleSystem.emission;
                emission.enabled = true;
                
                if (!streamParticleSystem.isPlaying)
                {
                    streamParticleSystem.Clear();
                    streamParticleSystem.Play(true);
                    
                    if (enableDebugLogs)
                    {
                        Debug.Log($"[CompanionCombat] ✅ Playing stream - Active: {streamParticleSystem.gameObject.activeSelf}, Playing: {streamParticleSystem.isPlaying}, Count: {streamParticleSystem.particleCount}");
                    }
                }
                
                // Point at target
                Vector3 direction = (target.position - rightHandEmitPoint.position).normalized;
                if (direction.sqrMagnitude > 0.0001f)
                {
                    streamParticleSystem.transform.rotation = Quaternion.LookRotation(direction);
                }
            }
            // Instantiate prefab if no direct reference
            else
            {
                if (_activeStreamEffect == null && streamParticlePrefab != null)
                {
                    _activeStreamEffect = Instantiate(
                        streamParticlePrefab,
                        rightHandEmitPoint.position,
                        rightHandEmitPoint.rotation,
                        rightHandEmitPoint);

                    // Get ALL particle systems in the beam
                    ParticleSystem[] beamParticles = _activeStreamEffect.GetComponentsInChildren<ParticleSystem>(true);
                    
                    if (enableDebugLogs)
                    {
                        Debug.Log($"[CompanionCombat] 🌊 Instantiated beam prefab with {beamParticles.Length} particle systems");
                    }
                    
                    // Force all to play and loop
                    foreach (ParticleSystem ps in beamParticles)
                    {
                        if (ps != null)
                        {
                            ps.gameObject.SetActive(true);
                            
                            // Force looping so it doesn't stop
                            var main = ps.main;
                            main.loop = true;
                            
                            var emission = ps.emission;
                            emission.enabled = true;
                            
                            ps.Clear();
                            ps.Play(true);
                            
                            if (enableDebugLogs)
                            {
                                Debug.Log($"[CompanionCombat] 🎆 Beam particle '{ps.name}' - Loop: {ps.main.loop}, Playing: {ps.isPlaying}");
                            }
                        }
                    }
                    
                    _streamParticleSystem = _activeStreamEffect.GetComponentInChildren<ParticleSystem>();
                }

                if (_activeStreamEffect != null)
                {
                    _activeStreamEffect.transform.position = rightHandEmitPoint.position;
                    Vector3 direction = (target.position - rightHandEmitPoint.position).normalized;
                    if (direction.sqrMagnitude > 0.0001f)
                    {
                        _activeStreamEffect.transform.rotation = Quaternion.LookRotation(direction);
                    }

                    // CRITICAL: Keep ALL particle systems playing
                    ParticleSystem[] allBeamParticles = _activeStreamEffect.GetComponentsInChildren<ParticleSystem>(true);
                    foreach (ParticleSystem ps in allBeamParticles)
                    {
                        if (ps != null && !ps.isPlaying)
                        {
                            // Force emission on and restart
                            var emission = ps.emission;
                            emission.enabled = true;
                            ps.Play(true);
                            
                            if (enableDebugLogs)
                            {
                                Debug.LogWarning($"[CompanionCombat] ⚠️ Beam particle '{ps.name}' stopped! Restarting...");
                            }
                        }
                    }
                }
                else if (enableDebugLogs)
                {
                    Debug.LogWarning("[CompanionCombat] ❌ No stream particle system or prefab assigned!");
                }
            }

            if (_streamDamageLoop == null)
            {
                _streamDamageLoop = StartCoroutine(StreamDamageLoop());
                _core?.PlayStreamSound();
                _handAnimator?.StartBeamRight();
            }

            _lastBeamToggle = Time.time;
        }

        private void StopStreamAttack()
        {
            if (_streamDamageLoop != null)
            {
                StopCoroutine(_streamDamageLoop);
                _streamDamageLoop = null;
            }

            // PRIORITY 1: Stop direct particle system reference
            if (streamParticleSystem != null && streamParticleSystem.isPlaying)
            {
                streamParticleSystem.Stop();
                
                if (enableDebugLogs)
                {
                    Debug.Log("[CompanionCombat] 🛑 Stopped stream particle system");
                }
            }
            // FALLBACK: Stop instantiated prefab
            else if (_streamParticleSystem != null && _streamParticleSystem.isPlaying)
            {
                _streamParticleSystem.Stop();
            }

            if (_activeStreamEffect != null)
            {
                _activeStreamEffect.transform.localPosition = Vector3.zero;
                _activeStreamEffect.transform.localRotation = Quaternion.identity;
            }

            _core?.StopStreamSound();
            _handAnimator?.StopBeamRight();
        }

        private IEnumerator StreamDamageLoop()
        {
            var wait = new WaitForSeconds(0.15f);

            while (true)
            {
                Transform target = _targeting?.GetCurrentTarget();

                if (target == null || !_isAttacking)
                {
                    StopStreamAttack();
                    yield break;
                }

                // CRITICAL FIX: Use area damage for stream weapon to hit player through fake target
                // Enemy companions use fake targets for aiming, so single-target damage misses the player
                ApplyDamage(target, streamDamage * 0.15f, true);

                yield return wait;
            }
        }

        private void ApplyDamage(Transform target, float damageAmount, bool area)
        {
            if (target == null)
            {
                return;
            }

            Vector3 hitPoint = target.position;
            Vector3 hitDirection = (target.position - transform.position).normalized;

            if (!area)
            {
                IDamageable singleTarget = target.GetComponent<IDamageable>() ?? target.GetComponentInParent<IDamageable>();
                singleTarget?.TakeDamage(damageAmount, hitPoint, hitDirection);
                return;
            }

            int count = Physics.OverlapSphereNonAlloc(hitPoint, damageRadius, _areaBuffer);
            for (int i = 0; i < count; i++)
            {
                Collider col = _areaBuffer[i];
                if (col == null) continue;

                IDamageable damageable = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
                damageable?.TakeDamage(damageAmount, col.ClosestPoint(hitPoint), hitDirection);
            }
        }

        private void FaceTarget(Transform target)
        {
            if (target == null) return;

            Vector3 toTarget = (target.position - transform.position);
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude < 0.0001f) return;

            Quaternion desiredRotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                desiredRotation,
                Time.deltaTime * targetTrackingSpeedMultiplier);
        }
    }
}

