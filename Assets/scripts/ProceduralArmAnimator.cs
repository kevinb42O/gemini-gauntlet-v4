using UnityEngine;
using System.Collections.Generic;

namespace GeminiGauntlet.Animation
{
    [AddComponentMenu("Gemini Gauntlet/Procedural Arm Animator")]
    public class ProceduralArmAnimator : MonoBehaviour
    {
        [Header("Profile")]
        public HandAnimationProfile profile;

        [Header("Bones (auto if not assigned)")]
        public SkinnedMeshRenderer meshRenderer;
        [Tooltip("Prefix used to find bones in SkinnedMeshRenderer.bones (e.g. L_ or R_).")]
        public string bonePrefix = "L_";
        public Transform arm;     // <prefix>Arm
        public Transform forearm; // <prefix>Forearm
        public Transform wrist;   // <prefix>Wrist
        public Transform hand;    // <prefix>Hand

        [Header("Conflict Handling")]
        [SerializeField] private bool disableAnimatorConflicts = true;
        [SerializeField] private bool disableOtherAnimationScripts = true;

        [Header("Runtime State")] 
        [SerializeField] private bool isBeaming;
        [SerializeField] private bool isRecoiling;
        [SerializeField] private float recoilTimer;
        [SerializeField] private float animTime;
        [SerializeField] private float movementSpeed;
        [SerializeField] private bool isGrounded = true;
        
        // Motion inputs (set by orchestrator)
        private Vector3 _velocity;
        private Vector3 _acceleration;

        // Base rotations
        private Quaternion _armBase, _foreBase, _wristBase, _handBase;

        // Inertia additive blending
        private Quaternion _armAdd = Quaternion.identity;
        private Quaternion _foreAdd = Quaternion.identity;
        private Quaternion _wristAdd = Quaternion.identity;
        private Quaternion _armAddPrev = Quaternion.identity;
        private Quaternion _foreAddPrev = Quaternion.identity;
        private Quaternion _wristAddPrev = Quaternion.identity;

        private void Awake()
        {
            TryAutoAssignBones();
            StoreBaseRotations();
            DisableCompetingAnimations();
        }

        private void Reset()
        {
            TryAutoAssignBones();
        }

        private void Update()
        {
            if (profile == null) return;
            animTime += Time.deltaTime * profile.animationSpeed;

            // Update timers
            if (isRecoiling)
            {
                recoilTimer -= Time.deltaTime;
                if (recoilTimer <= 0f)
                {
                    isRecoiling = false;
                }
            }

            // Apply base state
            ApplyBaseState();

            // Overlay special states
            if (isRecoiling)
                ApplyRecoil();
            if (isBeaming)
                ApplyBeamPose();

            // Inertia as additive layer (suppressed when actively beaming/recoiling)
            if (!isBeaming && !isRecoiling)
                ApplyInertia();

            // Final additive application
            ApplyAdditiveRotationsOnTop();
        }

        public void UpdateMovement(float speed, bool grounded, Vector3 velocity, Vector3 acceleration)
        {
            movementSpeed = speed;
            isGrounded = grounded;
            _velocity = velocity;
            _acceleration = acceleration;
        }

        public void PerformRecoil()
        {
            if (profile == null) return;
            isRecoiling = true;
            recoilTimer = Mathf.Max(0.01f, profile.recoilDuration);
        }

        public void SetBeamFiring(bool firing)
        {
            isBeaming = firing;
        }

        private void ApplyBaseState()
        {
            // Idle / Running / Flying base motion
            if (movementSpeed > 1f)
            {
                if (isGrounded)
                    ApplyRunning();
                else
                    ApplyFlying();
            }
            else
            {
                ApplyIdle();
            }
        }

        private void ApplyIdle()
        {
            float t = animTime * profile.idleBreathingSpeed;
            float wave = Mathf.Sin(t) * profile.idleFloatAmplitude;
            float off = Mathf.Cos(t * 0.7f) * profile.idleFloatAmplitude * 0.5f;

            if (arm) arm.localRotation = _armBase * Quaternion.Euler(wave, off * 0.5f, wave * 0.3f);
            if (forearm) forearm.localRotation = _foreBase * Quaternion.Euler(wave * 0.7f, 0f, 0f);
            // Constrain wrist to pitch (X) only to avoid inward/outward yaw/roll during idle
            if (wrist) wrist.localRotation = _wristBase * Quaternion.Euler(wave * 2f, 0f, 0f);
        }

        private void ApplyRunning()
        {
            float t = animTime * profile.runningSwingSpeed;
            float swing = Mathf.Sin(t) * profile.runningSwingAmplitude * Mathf.Clamp01(movementSpeed * 0.1f);

            if (arm) arm.localRotation = _armBase * Quaternion.Euler(swing * 10f, 0f, swing * 6f);
            if (forearm) forearm.localRotation = _foreBase * Quaternion.Euler(swing * 6f, 0f, 0f);
            // Constrain wrist to pitch (X) only while running
            if (wrist) wrist.localRotation = _wristBase * Quaternion.Euler(swing * 8f, 0f, 0f);
        }

        private void ApplyFlying()
        {
            float t = animTime * profile.flyingFloatSpeed;
            float floaty = Mathf.Sin(t) * profile.flyingFloatAmplitude;

            if (arm) arm.localRotation = _armBase * Quaternion.Euler(floaty * 3f, 0f, floaty * 2f);
            if (forearm) forearm.localRotation = _foreBase * Quaternion.Euler(floaty * 2f, 0f, 0f);
            // Constrain wrist to pitch (X) only while flying
            if (wrist) wrist.localRotation = _wristBase * Quaternion.Euler(floaty * 2f, 0f, 0f);
        }

        private void ApplyRecoil()
        {
            float progress = 1f - (recoilTimer / Mathf.Max(profile.recoilDuration, 0.0001f));
            float curve = Mathf.Sin(progress * Mathf.PI); // out and back
            float intensity = curve * profile.recoilStrength;

            // Shoulder/Arm
            if (arm)
            {
                float deg = -intensity * profile.recoilShoulderWeight;
                arm.localRotation = _armBase * Quaternion.Euler(deg, 0f, 0f);
            }
            // Forearm
            if (forearm)
            {
                float deg = -intensity * profile.recoilForearmWeight;
                forearm.localRotation = _foreBase * Quaternion.Euler(deg, 0f, 0f);
            }
            // Wrist dominant
            if (wrist)
            {
                float deg = -intensity * profile.recoilWristWeight;
                wrist.localRotation = _wristBase * Quaternion.Euler(deg, 0f, 0f);
            }
        }

        private void ApplyBeamPose()
        {
            float micro = Mathf.Sin(animTime * profile.beamVibrationFreq) * (profile.beamVibrationIntensity * 0.15f);

            if (arm)
            {
                float comp = profile.beamShoulderCompDegrees;
                arm.localRotation = _armBase * Quaternion.Euler(-comp, 0f, 0f);
            }
            if (forearm)
            {
                // Keep steady for aiming
                forearm.localRotation = _foreBase;
            }
            if (wrist)
            {
                float up = profile.beamWristUpDegrees + micro * 2f;
                wrist.localRotation = _wristBase * Quaternion.Euler(-up, 0f, 0f);
            }
        }

        private void ApplyInertia()
        {
            if (profile.inertiaAmount <= 0f) return;
            // Map acceleration to small pitch/roll offset
            float pitch = -_acceleration.z * profile.inertiaResponse; // forward accel pitches up
            float roll = _acceleration.x * profile.inertiaResponse;   // lateral accel rolls

            _armAdd = Quaternion.identity; // keep shoulder mostly stable
            _foreAdd = Quaternion.Euler(pitch * 0.5f, 0f, roll * 0.3f);
            // Constrain wrist inertia to pitch (X) only
            _wristAdd = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void ApplyAdditiveRotationsOnTop()
        {
            float t = 1f - Mathf.Exp(-profile.inertiaBlendSpeed * Time.deltaTime);

            Quaternion armTarget = _armAdd;
            Quaternion foreTarget = _foreAdd;
            Quaternion wristTarget = _wristAdd;
            if (profile.inertiaAmount < 1f)
            {
                armTarget = Quaternion.Slerp(Quaternion.identity, armTarget, profile.inertiaAmount);
                foreTarget = Quaternion.Slerp(Quaternion.identity, foreTarget, profile.inertiaAmount);
                wristTarget = Quaternion.Slerp(Quaternion.identity, wristTarget, profile.inertiaAmount);
            }

            _armAddPrev = Quaternion.Slerp(_armAddPrev, armTarget, t);
            _foreAddPrev = Quaternion.Slerp(_foreAddPrev, foreTarget, t);
            _wristAddPrev = Quaternion.Slerp(_wristAddPrev, wristTarget, t);

            if (arm) arm.localRotation = arm.localRotation * _armAddPrev;
            if (forearm) forearm.localRotation = forearm.localRotation * _foreAddPrev;
            if (wrist) wrist.localRotation = wrist.localRotation * _wristAddPrev;

            // Reset for next frame
            _armAdd = _foreAdd = _wristAdd = Quaternion.identity;
        }

        private static bool IsAncestorOrSelf(Transform potentialAncestor, Transform child)
        {
            if (potentialAncestor == null || child == null) return false;
            var t = child;
            while (t != null)
            {
                if (t == potentialAncestor) return true;
                t = t.parent;
            }
            return false;
        }

        // Disable only components that could drive the same bone chain to avoid conflicts
        private void DisableCompetingAnimations()
        {
            if (!disableAnimatorConflicts && !disableOtherAnimationScripts) return;

            // Build the controlled chain
            var chain = new List<Transform>(4);
            if (arm) chain.Add(arm);
            if (forearm) chain.Add(forearm);
            if (wrist) chain.Add(wrist);
            if (hand) chain.Add(hand);
            if (chain.Count == 0) return; // nothing to protect

            if (disableAnimatorConflicts)
            {
                // Disable Animator components on parents that encompass our chain
                var parentAnimators = GetComponentsInParent<Animator>(true);
                foreach (var anim in parentAnimators)
                {
                    if (anim == null || !anim.enabled) continue;
                    bool affects = false;
                    foreach (var b in chain)
                    {
                        if (IsAncestorOrSelf(anim.transform, b)) { affects = true; break; }
                    }
                    if (affects)
                    {
                        anim.enabled = false;
                        Debug.Log($"[ProceduralArmAnimator] Disabled Animator on '{anim.name}' to avoid conflicts for prefix '{bonePrefix}'.", this);
                    }
                }

                // Disable any rare child Animators that might sit under the bones
                foreach (var b in chain)
                {
                    var childAnimators = b.GetComponentsInChildren<Animator>(true);
                    foreach (var anim in childAnimators)
                    {
                        if (anim == null || !anim.enabled) continue;
                        anim.enabled = false;
                        Debug.Log($"[ProceduralArmAnimator] Disabled child Animator '{anim.name}' under bone '{b.name}'.", this);
                    }
                }

                // Legacy Animation components
                var parentLegacy = GetComponentsInParent<UnityEngine.Animation>(true);
                foreach (var anim in parentLegacy)
                {
                    if (anim == null || !anim.enabled) continue;
                    bool affects = false;
                    foreach (var b in chain)
                    {
                        if (IsAncestorOrSelf(anim.transform, b)) { affects = true; break; }
                    }
                    if (affects)
                    {
                        anim.enabled = false;
                        Debug.Log($"[ProceduralArmAnimator] Disabled Legacy Animation on '{anim.name}' to avoid conflicts.", this);
                    }
                }
                foreach (var b in chain)
                {
                    var childLegacy = b.GetComponentsInChildren<UnityEngine.Animation>(true);
                    foreach (var anim in childLegacy)
                    {
                        if (anim == null || !anim.enabled) continue;
                        anim.enabled = false;
                        Debug.Log($"[ProceduralArmAnimator] Disabled child Legacy Animation '{anim.name}' under bone '{b.name}'.", this);
                    }
                }
            }

            if (disableOtherAnimationScripts)
            {
                string[] keywords = { "Animation", "Recoil", "IK", "Constraint", "Rig" };
                foreach (var t in chain)
                {
                    // Parents
                    var parentScripts = t.GetComponentsInParent<MonoBehaviour>(true);
                    foreach (var mb in parentScripts)
                    {
                        if (mb == null || mb == (MonoBehaviour)this) continue;
                        string typeName = mb.GetType().Name;
                        bool match = false;
                        for (int i = 0; i < keywords.Length; i++)
                        {
                            if (typeName.IndexOf(keywords[i], System.StringComparison.OrdinalIgnoreCase) >= 0)
                            { match = true; break; }
                        }
                        if (match && mb.enabled)
                        {
                            mb.enabled = false;
                            Debug.Log($"[ProceduralArmAnimator] Disabled '{typeName}' on '{mb.name}' near bone '{t.name}'.", this);
                        }
                    }

                    // Children
                    var childScripts = t.GetComponentsInChildren<MonoBehaviour>(true);
                    foreach (var mb in childScripts)
                    {
                        if (mb == null || mb == (MonoBehaviour)this) continue;
                        string typeName = mb.GetType().Name;
                        bool match = false;
                        for (int i = 0; i < keywords.Length; i++)
                        {
                            if (typeName.IndexOf(keywords[i], System.StringComparison.OrdinalIgnoreCase) >= 0)
                            { match = true; break; }
                        }
                        if (match && mb.enabled)
                        {
                            mb.enabled = false;
                            Debug.Log($"[ProceduralArmAnimator] Disabled '{typeName}' on '{mb.name}' under bone '{t.name}'.", this);
                        }
                    }
                }
            }
        }

        private void TryAutoAssignBones()
        {
            if (meshRenderer == null)
                meshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();

            if (meshRenderer == null || meshRenderer.bones == null) return;

            // Only auto if not explicitly assigned
            if (arm && forearm && wrist && hand) return;

            foreach (var b in meshRenderer.bones)
            {
                if (b == null) continue;
                if (arm == null && b.name == bonePrefix + "Arm") arm = b;
                else if (forearm == null && b.name == bonePrefix + "Forearm") forearm = b;
                else if (wrist == null && b.name == bonePrefix + "Wrist") wrist = b;
                else if (hand == null && b.name == bonePrefix + "Hand") hand = b;
            }
        }

        private void StoreBaseRotations()
        {
            if (arm) _armBase = arm.localRotation;
            if (forearm) _foreBase = forearm.localRotation;
            if (wrist) _wristBase = wrist.localRotation;
            if (hand) _handBase = hand.localRotation;
        }

        // Utility for external reset if needed
        public void ResetToBase()
        {
            if (arm) arm.localRotation = _armBase;
            if (forearm) forearm.localRotation = _foreBase;
            if (wrist) wrist.localRotation = _wristBase;
            if (hand) hand.localRotation = _handBase;
        }
    }
}
