using UnityEngine;

namespace GeminiGauntlet.Animation
{
    [AddComponentMenu("Gemini Gauntlet/Player Animation Controller")]
    public class PlayerAnimationController : MonoBehaviour
    {
        [Header("Arm Animators")]
        public ProceduralArmAnimator leftArm;   // Left arm component
        public ProceduralArmAnimator rightArm;  // Right arm component

        [Header("Hand Mapping")] 
        [Tooltip("If true, PRIMARY hand maps to Right arm; otherwise to Left arm.")]
        public bool primaryIsRightHand = true;

        [Header("Movement Sampling")] 
        [Tooltip("If assigned, movement/grounded will be sampled from this CharacterController if present in parents.")]
        public CharacterController characterController;
        [Tooltip("If assigned, movement will be sampled from this Rigidbody if present in parents (used when no CharacterController).")]
        public Rigidbody rigidbodySource;
        [Tooltip("Optional explicit Grounded check ray length. For 320-unit character: 50. Standard 2m character: 0.3")]
        public float groundedRayLength = 50f;
        [Tooltip("LayerMask used for manual grounded raycast if needed.")]
        public LayerMask groundedLayers = ~0;
        
        [Header("Performance Optimization")]
        [Tooltip("OPTIONAL: PlayerRaycastManager for consolidated ground checks. Falls back to local raycasts if null.")]
        public PlayerRaycastManager raycastManager;

        [Header("Diagnostics")] 
        public bool logDebug = false;

        // Singleton convenience (optional)
        public static PlayerAnimationController Instance { get; private set; }

        // Cached kinematics
        private Vector3 _prevPosition;
        private Vector3 _prevVelocity;
        private float _prevTime = -1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (logDebug) Debug.LogWarning("[PlayerAnimationController] Multiple instances found. Using the latest.", this);
            }
            Instance = this;

            if (characterController == null)
                characterController = GetComponentInParent<CharacterController>();
            if (rigidbodySource == null)
                rigidbodySource = GetComponentInParent<Rigidbody>();
            
            // PERFORMANCE OPTIMIZATION: Auto-find raycast manager if not assigned
            if (raycastManager == null)
                raycastManager = GetComponentInParent<PlayerRaycastManager>();

            _prevPosition = transform.position;
            _prevTime = Time.time;
        }

        private void Update()
        {
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);

            // Sample movement state
            Vector3 velocity = SampleVelocity();
            float speed = velocity.magnitude;
            bool grounded = SampleGrounded();
            Vector3 acceleration = (velocity - _prevVelocity) / dt;

            // Forward to arms
            if (leftArm != null)
                leftArm.UpdateMovement(speed, grounded, velocity, acceleration);
            if (rightArm != null)
                rightArm.UpdateMovement(speed, grounded, velocity, acceleration);

            // Cache
            _prevVelocity = velocity;
            _prevPosition = transform.position;
        }

        // Compatibility API for existing callers (e.g., HandFiringMechanics, PlayerShooterOrchestrator)
        public void TriggerShotgunRecoil(bool isPrimaryHand)
        {
            var arm = GetArmForHand(isPrimaryHand);
            if (arm == null)
            {
                if (logDebug) Debug.LogWarning("[PlayerAnimationController] TriggerShotgunRecoil called but mapped arm is missing.", this);
                return;
            }
            arm.PerformRecoil();
        }

        public void SetBeamFiring(bool isPrimaryHand, bool firing)
        {
            var arm = GetArmForHand(isPrimaryHand);
            if (arm == null)
            {
                if (logDebug) Debug.LogWarning("[PlayerAnimationController] SetBeamFiring called but mapped arm is missing.", this);
                return;
            }
            arm.SetBeamFiring(firing);
        }

        // Direct per-arm helpers (optional convenience)
        public void TriggerLeftRecoil()  { if (leftArm) leftArm.PerformRecoil(); }
        public void TriggerRightRecoil() { if (rightArm) rightArm.PerformRecoil(); }
        public void SetLeftBeam(bool firing)  { if (leftArm) leftArm.SetBeamFiring(firing); }
        public void SetRightBeam(bool firing) { if (rightArm) rightArm.SetBeamFiring(firing); }

        private ProceduralArmAnimator GetArmForHand(bool isPrimary)
        {
            bool routeToRight = isPrimary ? primaryIsRightHand : !primaryIsRightHand;
            return routeToRight ? rightArm : leftArm;
        }

        private Vector3 SampleVelocity()
        {
            // Prefer CharacterController if available (works for kinematic controllers)
            if (characterController != null)
                return characterController.velocity;

            // Next, use Rigidbody if available
            if (rigidbodySource != null)
                return rigidbodySource.linearVelocity;

            // Fallback: approximate from transform displacement
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            Vector3 displacement = transform.position - _prevPosition;
            return displacement / dt;
        }

        private bool SampleGrounded()
        {
            if (characterController != null)
                return characterController.isGrounded;

            // PERFORMANCE OPTIMIZATION: Use shared raycast manager if available
            if (raycastManager != null && raycastManager.HasValidGroundHit)
                return raycastManager.IsGrounded;

            // FALLBACK: Simple manual ground probe if no CharacterController or manager
            Vector3 origin = transform.position + Vector3.up * 0.05f;
            return Physics.Raycast(origin, Vector3.down, groundedRayLength, groundedLayers, QueryTriggerInteraction.Ignore);
        }
    }
}
