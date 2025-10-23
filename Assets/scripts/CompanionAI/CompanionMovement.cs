using UnityEngine;
using UnityEngine.AI;
using System.Collections;

namespace CompanionAI
{
    /// <summary>
    /// Handles all companion movement behavior - following, combat movement, repositioning
    /// </summary>
    public class CompanionMovement : MonoBehaviour
    {
        [Header("Following Behavior")]
        [Range(100f, 2000f)] public float followingDistance = 400f;
        [Range(50f, 1000f)] public float minFollowDistance = 200f;
        [Range(2000f, 20000f)] public float maxFollowDistance = 10000f;
        
        [Header("Movement Speeds")]
        [Range(100f, 5000f)] public float walkingSpeed = 400f;
        [Range(200f, 5000)] public float runningSpeed = 750f;
        
        [Header("Dynamic Movement")]
        public bool moveWhileShooting = true;
        [Range(1f, 500f)] public float combatSpeedMultiplier = 300f;
        [Range(0.2f, 2f)] public float repositionInterval = 0.5f;
        [Range(0f, 1f)] public float jumpChance = 0.8f; // High jump chance for monkey behavior
        [Range(100f, 8000f)] public float jumpForce = 4000f; // Increased for -800 gravity
        
        [Header("Ninja Combat Settings")]
        [Range(0.5f, 2f)] public float tacticalMovementInterval = 0.3f; // How often to change tactics
        [Range(100f, 750f)] public float minMovementDistance = 250f; // Minimum movement per reposition
        [Range(200f, 2500f)] public float maxMovementDistance = 1000f; // Maximum movement per reposition
        
        [Header("Ground Detection")]
        public LayerMask groundLayers = -1;
        
        private CompanionCore _core;
        private NavMeshAgent _navAgent;
        private Transform _transform;
        private Rigidbody _rigidbody;
        
        // Movement state
        private Vector3 _lastPlayerPosition;
        private float _lastDistanceToPlayer;
        private float _lastRepositionTime;
        private bool _isRepositioning;
        private float _lastJumpTime;
        private Vector3 _strafeDirection;
        private float _lastStrafeChange;
        
        // Coroutines
        private Coroutine _movementUpdateCoroutine;
        
        public void Initialize(CompanionCore core)
        {
            _core = core;
            _transform = transform;
            _navAgent = core.NavAgent;
            _rigidbody = GetComponent<Rigidbody>();
            
            ConfigureNavAgent();
            StartMovementSystem();
        }
        
        private void ConfigureNavAgent()
        {
            _navAgent.stoppingDistance = followingDistance * 0.8f;
            _navAgent.acceleration = 500f;
            _navAgent.angularSpeed = 2000f;
            _navAgent.speed = walkingSpeed;
            
            // 🎯 CRITICAL: Disable automatic rotation for enemies
            // Enemies should always face their target, not their movement direction
            // This allows strafing and backpedaling while maintaining aim
            EnemyCompanionBehavior enemyBehavior = GetComponent<EnemyCompanionBehavior>();
            if (enemyBehavior != null && enemyBehavior.isEnemy)
            {
                _navAgent.updateRotation = false;
                Debug.Log("[CompanionMovement] 🎯 Enemy NavAgent rotation disabled - will face target while moving");
            }
            
            // 🦘 CONFIGURE RIGIDBODY FOR SMOOTH PARKOUR JUMPING
            if (_rigidbody != null)
            {
                _rigidbody.freezeRotation = true;
                _rigidbody.useGravity = true;
                
                // CRITICAL: Check if this is an enemy - enemies should be kinematic to prevent pushback!
                if (enemyBehavior != null && enemyBehavior.isEnemy)
                {
                    // ENEMY: Make kinematic to prevent physics pushback
                    _rigidbody.isKinematic = true;
                    Debug.Log($"[CompanionMovement] 🛡️ Enemy Rigidbody set to KINEMATIC - no physics pushback!");
                }
                else
                {
                    // FRIENDLY: Normal physics for parkour
                    _rigidbody.mass = 1f; // Light for agile movement
                    _rigidbody.linearDamping = 0.5f; // Air resistance for smoother landing
                    _rigidbody.angularDamping = 5f; // Prevent spinning
                    
                    Debug.Log($"[CompanionMovement] 🦘 Rigidbody configured for parkour! Mass: {_rigidbody.mass}, Drag: {_rigidbody.linearDamping}");
                }
            }
        }
        
        private void StartMovementSystem()
        {
            _movementUpdateCoroutine = StartCoroutine(MovementUpdateLoop());
        }
        
        private IEnumerator MovementUpdateLoop()
        {
            while (enabled && !_core.IsDead)
            {
                UpdateMovement();
                yield return new WaitForSeconds(0.05f); // 20 FPS update rate
            }
            
            // 💀 PERFORMANCE: Stop coroutine when companion dies
            Debug.Log("[CompanionMovement] 💀 Movement coroutine stopped - companion is dead");
        }
        
        private void UpdateMovement()
        {
            if (_core.PlayerTransform == null) return;
            
            float distanceToPlayer = Vector3.Distance(_transform.position, _core.PlayerTransform.position);
            
            switch (_core.CurrentState)
            {
                case CompanionCore.CompanionState.Following:
                    HandleFollowingMovement(distanceToPlayer);
                    break;
                    
                case CompanionCore.CompanionState.Engaging:
                    HandleEngagingMovement(distanceToPlayer);
                    break;
                    
                case CompanionCore.CompanionState.Attacking:
                    HandleCombatMovement(distanceToPlayer);
                    break;
            }
            
            _lastPlayerPosition = _core.PlayerTransform.position;
            _lastDistanceToPlayer = distanceToPlayer;
        }
        
        private void HandleFollowingMovement(float distanceToPlayer)
        {
            _navAgent.speed = walkingSpeed;
            
            // CHECK: Enemies NEVER teleport!
            EnemyCompanionBehavior enemyBehavior = GetComponent<EnemyCompanionBehavior>();
            bool isEnemy = enemyBehavior != null && enemyBehavior.isEnemy;
            
            if (distanceToPlayer > maxFollowDistance && !isEnemy)
            {
                // Teleport to player if too far (FRIENDLY COMPANIONS ONLY)
                TeleportToPlayer();
            }
            else if (distanceToPlayer > followingDistance)
            {
                // Move toward player
                _navAgent.SetDestination(_core.PlayerTransform.position);
            }
            else if (distanceToPlayer < minFollowDistance && !isEnemy)
            {
                // Move away from player (FRIENDLY COMPANIONS ONLY)
                Vector3 awayDirection = (_transform.position - _core.PlayerTransform.position).normalized;
                Vector3 targetPosition = _core.PlayerTransform.position + awayDirection * followingDistance;
                _navAgent.SetDestination(targetPosition);
            }
            
            // DEBUG: Log if enemy would have teleported
            if (distanceToPlayer > maxFollowDistance && isEnemy)
            {
                Debug.LogWarning($"[CompanionMovement] ⚠️ ENEMY {gameObject.name} would have teleported! Distance: {distanceToPlayer:F0} > {maxFollowDistance:F0} - BLOCKED!");
            }
        }
        
        private void HandleEngagingMovement(float distanceToPlayer)
        {
            // Keep it simple - just move toward combat while maintaining follow distance
            _navAgent.speed = runningSpeed;
            HandleFollowingMovement(distanceToPlayer);
        }
        
        private void HandleCombatMovement(float distanceToPlayer)
        {
            if (!moveWhileShooting) return;

            // CHECK: If enemy, use different speed calculation (no combatSpeedMultiplier stacking)
            EnemyCompanionBehavior enemyBehavior = GetComponent<EnemyCompanionBehavior>();
            if (enemyBehavior != null && enemyBehavior.isEnemy)
            {
                // ENEMY: Use runningSpeed directly (EnemyCompanionBehavior already configured it)
                _navAgent.speed = runningSpeed;
            }
            else
            {
                // FRIENDLY COMPANION: Apply combat speed multiplier
                _navAgent.speed = runningSpeed * combatSpeedMultiplier;
            }
            
            // Get target - works for both friendly companions and enemies
            // For enemies, EnemyCompanionBehavior injects fake target via reflection
            Transform target = _core.TargetingSystem?.GetCurrentTarget();
            
            // For enemies without targeting system, use player as target
            if (target == null && enemyBehavior != null && enemyBehavior.isEnemy)
            {
                // Enemy mode: Use player transform as target for movement
                target = _core.PlayerTransform;
            }
            
            if (target == null) return;
            
            // 🎯 TACTICAL MOVEMENT PATTERNS
            PerformTacticalMovement(target, distanceToPlayer);
            
            // 🦘 AGGRESSIVE JUMPING - Jump frequently like a crazy monkey!
            if (Time.time - _lastJumpTime > 0.8f)
            {
                float jumpRoll = Random.value;
                Debug.Log($"[CompanionMovement] 🎲 JUMP ROLL: {jumpRoll:F2} vs {jumpChance:F2}");
                
                if (jumpRoll < jumpChance)
                {
                    Jump();
                    _lastJumpTime = Time.time;
                }
                else
                {
                    Debug.Log("[CompanionMovement] 🎲 Jump roll failed - no jump this time");
                }
            }
            
            // 🌪️ CONSTANT REPOSITIONING - Never stay still!
            if (Time.time - _lastRepositionTime > repositionInterval)
            {
                RepositionInCombat();
                _lastRepositionTime = Time.time;
            }
        }
        
        private void PerformTacticalMovement(Transform target, float distanceToPlayer)
        {
            Vector3 playerPos = _core.PlayerTransform.position;
            Vector3 targetPos = target.position;
            Vector3 myPos = _transform.position;
            
            // Calculate distances for tactical decisions
            float distanceToTarget = Vector3.Distance(myPos, targetPos);
            
            // 🎯 MOVEMENT STRATEGY SELECTION
            if (distanceToTarget < 300f)
            {
                // CLOSE RANGE: Backpedal while shooting (kiting)
                PerformBackpedalMovement(targetPos, playerPos);
            }
            else if (distanceToTarget > 800f)
            {
                // LONG RANGE: Aggressive advance with strafing
                PerformAdvanceMovement(targetPos, playerPos);
            }
            else
            {
                // MID RANGE: Circle strafe like a pro
                PerformCircleStrafe(targetPos, playerPos);
            }
        }
        
        private void PerformBackpedalMovement(Vector3 targetPos, Vector3 playerPos)
        {
            // 🏃‍♂️ BACKPEDAL: Move away from target while staying near player
            Vector3 awayFromTarget = (_transform.position - targetPos).normalized;
            Vector3 towardPlayer = (playerPos - _transform.position).normalized;
            
            // Blend away from target + toward player
            Vector3 backpedalDirection = (awayFromTarget * 0.7f + towardPlayer * 0.3f).normalized;
            Vector3 destination = _transform.position + backpedalDirection * 200f;
            
            _navAgent.SetDestination(destination);
        }
        
        private void PerformAdvanceMovement(Vector3 targetPos, Vector3 playerPos)
        {
            // ⚡ ADVANCE: Move toward target with tactical offset
            Vector3 toTarget = (targetPos - _transform.position).normalized;
            Vector3 sideOffset = Vector3.Cross(toTarget, Vector3.up) * Random.Range(-150f, 150f);
            
            Vector3 destination = _transform.position + toTarget * 300f + sideOffset;
            _navAgent.SetDestination(destination);
        }
        
        private void PerformCircleStrafe(Vector3 targetPos, Vector3 playerPos)
        {
            // 🌪️ CIRCLE STRAFE: Move in circles around target
            Vector3 toTarget = (targetPos - _transform.position).normalized;
            Vector3 strafeDirection = Vector3.Cross(toTarget, Vector3.up);
            
            // Randomly switch strafe direction
            if (Time.time - _lastStrafeChange > 1.5f)
            {
                strafeDirection *= Random.value > 0.5f ? 1f : -1f;
                _lastStrafeChange = Time.time;
            }
            else
            {
                strafeDirection *= _strafeDirection.x > 0 ? 1f : -1f; // Keep current direction
            }
            
            Vector3 destination = _transform.position + strafeDirection * 250f;
            _navAgent.SetDestination(destination);
            
            _strafeDirection = strafeDirection; // Remember strafe direction
        }
        
        private Vector3 CalculateOptimalCombatPosition(Transform target)
        {
            // SIMPLIFIED: Just stay near player with slight tactical offset
            // No complex positioning requirements - shoot from anywhere
            Vector3 playerPos = _core.PlayerTransform.position;
            Vector3 sideOffset = Vector3.Cross(Vector3.forward, Vector3.up) * Random.Range(-150f, 150f);
            
            return playerPos + sideOffset;
        }
        
        private void RepositionInCombat()
        {
            // Get target - works for both friendly companions and enemies
            Transform target = _core.TargetingSystem?.GetCurrentTarget();
            
            // For enemies without targeting system, use player as target
            if (target == null)
            {
                EnemyCompanionBehavior enemy = GetComponent<EnemyCompanionBehavior>();
                if (enemy != null && enemy.isEnemy)
                {
                    target = _core.PlayerTransform;
                }
            }
            
            if (target == null) return;
            
            // 🎯 ADVANCED TACTICAL REPOSITIONING
            Vector3 targetPos = target.position;
            Vector3 playerPos = _core.PlayerTransform.position;
            Vector3 myPos = _transform.position;
            
            // Choose repositioning strategy based on situation
            float randomChoice = Random.value;
            Vector3 newPosition;
            
            if (randomChoice < 0.4f)
            {
                // 🌪️ AGGRESSIVE FLANK: Move to target's side
                Vector3 toTarget = (targetPos - myPos).normalized;
                Vector3 flankDirection = Vector3.Cross(toTarget, Vector3.up) * (Random.value > 0.5f ? 1f : -1f);
                newPosition = myPos + flankDirection * Random.Range(200f, 400f);
            }
            else if (randomChoice < 0.7f)
            {
                // 🏃‍♂️ TACTICAL RETREAT: Fall back while maintaining angle
                Vector3 retreatDirection = (myPos - targetPos).normalized;
                Vector3 sideStep = Vector3.Cross(retreatDirection, Vector3.up) * Random.Range(-100f, 100f);
                newPosition = myPos + retreatDirection * 250f + sideStep;
            }
            else
            {
                // ⚡ SURPRISE ADVANCE: Quick dash toward better position
                Vector3 advanceDirection = (targetPos - myPos).normalized;
                Vector3 tacticalOffset = Vector3.Cross(advanceDirection, Vector3.up) * Random.Range(-200f, 200f);
                newPosition = myPos + advanceDirection * 150f + tacticalOffset;
            }
            
            // Ensure we don't move too far from player
            float distanceFromPlayer = Vector3.Distance(newPosition, playerPos);
            if (distanceFromPlayer > maxFollowDistance * 0.8f)
            {
                Vector3 toPlayer = (playerPos - newPosition).normalized;
                newPosition = playerPos + toPlayer * followingDistance;
            }
            
            _navAgent.SetDestination(newPosition);
            _isRepositioning = true;
        }
        
        private void Jump()
        {
            if (_rigidbody == null)
            {
                Debug.LogError("[CompanionMovement] ❌ NO RIGIDBODY - Cannot jump without Rigidbody component!");
                return;
            }
            
            if (!IsGrounded())
            {
                return; // Quietly skip if not grounded
            }
            
            // 🦘 PARKOUR JUMP - Keep moving while jumping!
            Vector3 jumpDirection = Vector3.up;
            Vector3 movementDirection = Vector3.zero;
            
            // GET CURRENT MOVEMENT DIRECTION from NavMesh
            if (_navAgent.enabled && _navAgent.hasPath)
            {
                Vector3 navDirection = (_navAgent.destination - _transform.position).normalized;
                movementDirection = new Vector3(navDirection.x, 0, navDirection.z);
            }
            
            // 🚀 DYNAMIC JUMP TYPES based on movement
            if (movementDirection.magnitude > 0.1f)
            {
                // MOVING JUMP - Jump in movement direction
                jumpDirection = (Vector3.up + movementDirection * 0.6f).normalized;
                Debug.Log("[CompanionMovement] 🏃‍♂️🦘 PARKOUR JUMP while moving!");
            }
            else
            {
                // STATIONARY JUMP - Add random direction for variety
                Vector3 randomDirection = new Vector3(
                    Random.Range(-0.4f, 0.4f), 
                    0, 
                    Random.Range(-0.4f, 0.4f)
                );
                jumpDirection = (Vector3.up + randomDirection).normalized;
                Debug.Log("[CompanionMovement] 🦘 NINJA JUMP in place!");
            }
            
            // 🎯 CALCULATE JUMP FORCE for -800 gravity
            float adjustedJumpForce = jumpForce;
            
            // Boost force if moving for better arc
            if (movementDirection.magnitude > 0.1f)
            {
                adjustedJumpForce *= 1.2f; // 20% more force for moving jumps
            }
            
            // 🚀 EXECUTE SMOOTH JUMP
            _rigidbody.AddForce(jumpDirection * adjustedJumpForce, ForceMode.Impulse);
            
            // 🌊 ADD SMOOTH LANDING - Reduce drag temporarily for better arc
            StartCoroutine(SmoothJumpPhysics());
            
            Debug.Log($"[CompanionMovement] 🦘 JUMP! Force: {adjustedJumpForce:F0}, Direction: {jumpDirection}");
        }
        
        private IEnumerator SmoothJumpPhysics()
        {
            if (_rigidbody == null) yield break;
            
            // 🚀 JUMP PHASE: Reduce drag for smooth arc
            float originalDrag = _rigidbody.linearDamping;
            _rigidbody.linearDamping = 0.1f; // Very low drag during jump
            
            // Wait for peak of jump
            yield return new WaitForSeconds(0.3f);
            
            // 🪂 LANDING PHASE: Increase drag for smoother landing
            if (_rigidbody != null)
            {
                _rigidbody.linearDamping = 1.5f; // Higher drag for soft landing
            }
            
            // Wait for landing
            yield return new WaitForSeconds(0.4f);
            
            // 🏃‍♂️ RESTORE NORMAL PHYSICS
            if (_rigidbody != null)
            {
                _rigidbody.linearDamping = originalDrag; // Back to normal
            }
        }
        

        
        private bool IsGrounded()
        {
            // 🔍 SMART GROUND DETECTION for smooth jumping
            Vector3 rayStart = _transform.position + Vector3.up * 0.2f;
            
            // Primary ground check
            bool isGrounded = Physics.Raycast(rayStart, Vector3.down, 1.5f, groundLayers);
            
            // Secondary check: Velocity-based (if falling fast, probably not grounded)
            if (isGrounded && _rigidbody != null && _rigidbody.linearVelocity.y < -5f)
            {
                isGrounded = false; // Still falling fast, don't allow jump
            }
            
            return isGrounded;
        }
        
        private void TeleportToPlayer()
        {
            if (_core.PlayerTransform == null) return;
            
            // CRITICAL: Double-check enemy status before teleporting
            EnemyCompanionBehavior enemyBehavior = GetComponent<EnemyCompanionBehavior>();
            if (enemyBehavior != null && enemyBehavior.isEnemy)
            {
                Debug.LogError($"[CompanionMovement] ❌ BLOCKED TELEPORT for ENEMY {gameObject.name}! Enemies should NEVER teleport!");
                return;
            }
            
            Vector3 teleportPosition = _core.PlayerTransform.position + Vector3.back * followingDistance;
            _transform.position = teleportPosition;
            
            Debug.Log("[CompanionMovement] Teleported to player - was too far away!");
        }
        
        public void SetFollowingMode()
        {
            _navAgent.speed = walkingSpeed;
            _isRepositioning = false;
        }
        
        public void SetEngagingMode()
        {
            _navAgent.speed = runningSpeed;
        }
        
        public void Stop()
        {
            if (_navAgent != null && _navAgent.enabled)
            {
                _navAgent.isStopped = true;
            }
        }
        
        /// <summary>
        /// 💀 INSTANT DEATH: Completely stop all movement and disable NavMesh
        /// </summary>
        public void InstantDeathStop()
        {
            // Stop all coroutines
            if (_movementUpdateCoroutine != null)
            {
                StopCoroutine(_movementUpdateCoroutine);
                _movementUpdateCoroutine = null;
            }
            
            // Disable NavMesh completely
            if (_navAgent != null && _navAgent.enabled && _navAgent.isOnNavMesh)
            {
                _navAgent.isStopped = true;
                _navAgent.enabled = false;
            }
            else if (_navAgent != null)
            {
                _navAgent.enabled = false;
            }
            
            // Disable this component
            this.enabled = false;
            
            Debug.Log("[CompanionMovement] 💀 INSTANT DEATH: All movement systems shut down");
        }
        
        public void Resume()
        {
            _navAgent.isStopped = false;
        }
        
        [ContextMenu("Force Jump Test")]
        public void ForceJumpTest()
        {
            Debug.Log("[CompanionMovement] 🧪 MANUAL JUMP TEST TRIGGERED!");
            Jump();
        }
        
        [ContextMenu("Jump System Diagnostics")]
        public void JumpSystemDiagnostics()
        {
            Debug.Log("=== 🦘 JUMP SYSTEM DIAGNOSTICS ===");
            Debug.Log($"Rigidbody: {_rigidbody != null} ({_rigidbody})");
            Debug.Log($"NavMeshAgent: {_navAgent != null} ({_navAgent})");
            Debug.Log($"Ground Layers: {groundLayers}");
            Debug.Log($"Jump Force: {jumpForce}");
            Debug.Log($"Jump Chance: {jumpChance}");
            Debug.Log($"Is Grounded: {IsGrounded()}");
            Debug.Log($"Position: {_transform.position}");
            Debug.Log($"Last Jump Time: {_lastJumpTime}");
            Debug.Log($"Time Since Last Jump: {Time.time - _lastJumpTime}");
            
            if (_rigidbody != null)
            {
                Debug.Log($"Rigidbody Mass: {_rigidbody.mass}");
                Debug.Log($"Rigidbody Drag: {_rigidbody.linearDamping}");
                Debug.Log($"Rigidbody Use Gravity: {_rigidbody.useGravity}");
                Debug.Log($"Rigidbody Freeze Rotation: {_rigidbody.freezeRotation}");
                Debug.Log($"Rigidbody Velocity: {_rigidbody.linearVelocity}");
            }
        }
        
        public void Cleanup()
        {
            if (_movementUpdateCoroutine != null)
            {
                StopCoroutine(_movementUpdateCoroutine);
                _movementUpdateCoroutine = null;
            }
        }
    }
}