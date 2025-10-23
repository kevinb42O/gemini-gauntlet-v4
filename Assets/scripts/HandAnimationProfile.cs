using UnityEngine;

namespace GeminiGauntlet.Animation
{
    [CreateAssetMenu(menuName = "GeminiGauntlet/Hand Animation Profile", fileName = "HandAnimationProfile")]
    public class HandAnimationProfile : ScriptableObject
    {
        [Header("General Timing")]
        [Range(0.1f, 3f)] public float animationSpeed = 1f;
        [Range(0.1f, 2f)] public float transitionSpeed = 1f;

        [Header("Idle")]
        [Range(0.5f, 3f)] public float idleBreathingSpeed = 1.5f;
        [Range(0.1f, 10f)] public float idleFloatAmplitude = 3f;
        [Range(0.0f, 1f)] public float idleFingerSway = 0.3f;

        [Header("Shotgun Recoil")]
        [Range(1f, 100f)] public float recoilStrength = 35f;
        [Range(0.05f, 0.5f)] public float recoilDuration = 0.15f;
        [Range(0.1f, 1.5f)] public float recoilRecoveryTime = 0.6f;
        [Tooltip("How much the shoulder contributes to the recoil pose (0-1).")]
        [Range(0f, 1f)] public float recoilShoulderWeight = 0.5f;
        [Tooltip("How much the forearm contributes to the recoil pose (0-1).")]
        [Range(0f, 1f)] public float recoilForearmWeight = 0.4f;
        [Tooltip("How much the wrist contributes to the recoil pose (0-1). Wrist is typically dominant.")]
        [Range(0f, 1f)] public float recoilWristWeight = 0.8f;

        [Header("Beam Firing")]
        [Range(1f, 10f)] public float beamStabilization = 5f;
        [Range(0.0f, 3f)] public float beamVibrationIntensity = 1.2f;
        [Range(1f, 40f)] public float beamVibrationFreq = 12f;
        [Tooltip("Degrees to tilt the wrist upward while beaming. This visually shows particles emitted from the hand.")]
        [Range(0f, 60f)] public float beamWristUpDegrees = 25f;
        [Tooltip("Small compensating shoulder pitch while beaming.")]
        [Range(0f, 20f)] public float beamShoulderCompDegrees = 5f;

        [Header("Movement (Running/Flying)")]
        [Range(0.0f, 5f)] public float runningSwingAmplitude = 1.5f;
        [Range(0.1f, 8f)] public float runningSwingSpeed = 2f;
        [Range(0.0f, 3f)] public float flyingStabilization = 1f;
        [Range(0.0f, 3f)] public float flyingFloatSpeed = 1.2f;
        [Range(0.0f, 12f)] public float flyingFloatAmplitude = 4f;

        [Header("Inertia Layer")]
        [Range(0f, 1f)] public float inertiaAmount = 1f;
        [Range(1f, 30f)] public float inertiaBlendSpeed = 12f;
        [Tooltip("Scales how acceleration is converted to wrist pitch/roll.")]
        [Range(0f, 2f)] public float inertiaResponse = 0.25f;
    }
}
