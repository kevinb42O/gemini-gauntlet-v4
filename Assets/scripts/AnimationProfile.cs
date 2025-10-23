using UnityEngine;

namespace ProceduralAnimation
{
    [CreateAssetMenu(fileName = "AnimationProfile", menuName = "Animation/Animation Profile")]
    public class AnimationProfile : ScriptableObject
    {
        [Header("=== Idle Animation ===")]
        [Range(0.1f, 5f)] public float idleBreathingSpeed = 1.5f;
        [Range(0f, 10f)] public float idleBreathingIntensity = 2f;
        [Range(0f, 5f)] public float idleSwayAmount = 1f;
        [Range(0.1f, 5f)] public float idleSwaySpeed = 0.8f;
        
        [Header("=== Recoil Animation ===")]
        [Range(0f, 30f)] public float shotgunRecoilStrength = 15f;
        [Range(0f, 20f)] public float shotgunRecoilUpward = 10f;
        [Range(0f, 10f)] public float shotgunRecoilBackward = 5f;
        [Range(0.05f, 1f)] public float recoilDuration = 0.3f;
        [Range(1f, 10f)] public float recoilRecoverySpeed = 4f;
        
        [Header("=== Beam Animation ===")]
        [Range(0f, 20f)] public float beamWristUpAngle = 15f;
        [Range(0f, 10f)] public float beamShoulderCompensation = 5f;
        [Range(0f, 5f)] public float beamJitterIntensity = 1f;
        [Range(5f, 30f)] public float beamJitterFrequency = 15f;
        [Range(0f, 10f)] public float beamChargeUpIntensity = 3f;
        
        [Header("=== Movement Response ===")]
        [Range(0f, 30f)] public float runningArmSwing = 20f;
        [Range(0.1f, 2f)] public float runningSwingSpeed = 1f;
        [Range(0f, 1f)] public float movementInertiaStrength = 0.5f;
        [Range(1f, 10f)] public float inertiaRecoverySpeed = 3f;
        [Range(0f, 20f)] public float sprintArmPullback = 15f;
        
        [Header("=== Landing & Impacts ===")]
        [Range(0f, 30f)] public float landingImpactAngle = 20f;
        [Range(0.1f, 1f)] public float landingRecoveryTime = 0.5f;
        [Range(0f, 10f)] public float landingShockwaveIntensity = 5f;
        
        [Header("=== Physics Settings ===")]
        [Range(0f, 1f)] public float armMass = 0.3f;
        [Range(0f, 50f)] public float springStiffness = 20f;
        [Range(0f, 10f)] public float springDamping = 5f;
        [Range(1f, 20f)] public float smoothingSpeed = 10f;
        
        [Header("=== Bone Distribution ===")]
        [Tooltip("How much of the animation affects the shoulder (0-1)")]
        [Range(0f, 1f)] public float shoulderInfluence = 0.3f;
        [Tooltip("How much of the animation affects the elbow (0-1)")]
        [Range(0f, 1f)] public float elbowInfluence = 0.5f;
        [Tooltip("How much of the animation affects the wrist (0-1)")]
        [Range(0f, 1f)] public float wristInfluence = 0.7f;
        
        [Header("=== Advanced Settings ===")]
        public AnimationCurve recoilCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public AnimationCurve beamPowerCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public bool enableProceduralFingers = false;
        public bool enableMuscleTension = true;
        public bool enableEnvironmentalResponse = true;
    }
}
