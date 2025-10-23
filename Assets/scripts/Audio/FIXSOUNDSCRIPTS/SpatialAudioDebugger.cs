using UnityEngine;

namespace GeminiGauntlet.Audio
{
    /// <summary>
    /// Visual debugging tool for spatial audio system
    /// Shows 3D audio ranges in Scene view and provides runtime diagnostics
    /// </summary>
    public class SpatialAudioDebugger : MonoBehaviour
    {
        [Header("=== VISUALIZATION ===")]
        [SerializeField] private bool enableGizmos = true;
        [SerializeField] private bool showMinDistance = true;
        [SerializeField] private bool showMaxDistance = true;
        [SerializeField] private bool showAudibleRange = true;
        [SerializeField] private bool showListenerPosition = true;
        
        [Header("=== COLORS ===")]
        [SerializeField] private Color minDistanceColor = new Color(0f, 1f, 0f, 0.3f);
        [SerializeField] private Color maxDistanceColor = new Color(1f, 1f, 0f, 0.2f);
        [SerializeField] private Color audibleRangeColor = new Color(1f, 0f, 0f, 0.1f);
        [SerializeField] private Color listenerColor = new Color(0f, 0.5f, 1f, 0.8f);
        
        [Header("=== PROFILE TESTING ===")]
        [SerializeField] private SpatialAudioType profileToVisualize = SpatialAudioType.Enemy;
        [SerializeField] private bool testAtThisPosition = false;
        
        private AudioListener audioListener;
        
        void Start()
        {
            audioListener = FindObjectOfType<AudioListener>();
            if (audioListener == null && Camera.main != null)
            {
                audioListener = Camera.main.GetComponent<AudioListener>();
            }
        }
        
        /// <summary>
        /// Test playing a sound at this debugger's position
        /// </summary>
        [ContextMenu("Test Play Sound Here")]
        public void TestPlaySoundHere()
        {
            SpatialAudioProfile profile = GetProfileForType(profileToVisualize);
            
            Debug.Log($"🎵 Testing {profile.profileName} at position {transform.position}");
            Debug.Log($"   • Min Distance: {profile.minDistance}m");
            Debug.Log($"   • Max Distance: {profile.maxDistance}m");
            Debug.Log($"   • Max Audible: {profile.maxAudibleDistance}m");
            Debug.Log($"   • Rolloff Mode: {profile.rolloffMode}");
            Debug.Log($"   • Spatial Blend: {profile.spatialBlend}");
            
            if (audioListener != null)
            {
                float distance = Vector3.Distance(transform.position, audioListener.transform.position);
                float volumeMultiplier = profile.GetVolumeMultiplierAtDistance(transform.position);
                Debug.Log($"   • Distance to listener: {distance:F1}m");
                Debug.Log($"   • Volume multiplier: {volumeMultiplier:F2}");
                Debug.Log($"   • Is audible: {profile.IsAudibleFromListener(transform.position)}");
            }
        }
        
        /// <summary>
        /// Show diagnostic info about spatial audio system
        /// </summary>
        [ContextMenu("Show Spatial Audio Diagnostics")]
        public void ShowDiagnostics()
        {
            Debug.Log("=== SPATIAL AUDIO DIAGNOSTICS ===");
            
            if (SoundSystemCore.Instance != null)
            {
                Debug.Log($"✅ SoundSystemCore: Active");
                Debug.Log($"   • Active Sounds: {SoundSystemCore.Instance.GetActiveSoundCount()}");
                Debug.Log($"   • Available Sources: {SoundSystemCore.Instance.GetAvailableSourceCount()}");
            }
            else
            {
                Debug.LogError("❌ SoundSystemCore: NOT INITIALIZED!");
            }
            
            if (SpatialAudioManager.Instance != null)
            {
                Debug.Log($"✅ SpatialAudioManager: Active");
                Debug.Log($"   • {SpatialAudioManager.Instance.GetDiagnosticInfo()}");
            }
            else
            {
                Debug.LogError("❌ SpatialAudioManager: NOT FOUND!");
            }
            
            if (audioListener != null)
            {
                Debug.Log($"✅ AudioListener: Found at {audioListener.transform.position}");
            }
            else
            {
                Debug.LogError("❌ AudioListener: NOT FOUND!");
            }
            
            Debug.Log("\n=== SPATIAL AUDIO PROFILES ===");
            LogProfileInfo("Skull Chatter", SpatialAudioProfiles.SkullChatter);
            LogProfileInfo("Skull Death", SpatialAudioProfiles.SkullDeath);
            LogProfileInfo("Tower Shoot", SpatialAudioProfiles.TowerShoot);
            LogProfileInfo("Tower Idle", SpatialAudioProfiles.TowerIdle);
            LogProfileInfo("Tower Awaken", SpatialAudioProfiles.TowerAwaken);
        }
        
        private void LogProfileInfo(string name, SpatialAudioProfile profile)
        {
            Debug.Log($"📊 {name}:");
            Debug.Log($"   • Min: {profile.minDistance}m | Max: {profile.maxDistance}m | Audible: {profile.maxAudibleDistance}m");
            Debug.Log($"   • Rolloff: {profile.rolloffMode} | Spatial: {profile.spatialBlend} | Priority: {profile.priority}");
        }
        
        private SpatialAudioProfile GetProfileForType(SpatialAudioType type)
        {
            switch (type)
            {
                case SpatialAudioType.Enemy:
                    return SpatialAudioProfiles.SkullChatter;
                case SpatialAudioType.Boss:
                    return SpatialAudioProfiles.SkullDeath;
                case SpatialAudioType.Tower:
                    return SpatialAudioProfiles.TowerShoot;
                default:
                    return SpatialAudioProfiles.GenericSFX;
            }
        }
        
        void OnDrawGizmos()
        {
            if (!enableGizmos) return;
            
            SpatialAudioProfile profile = GetProfileForType(profileToVisualize);
            Vector3 position = transform.position;
            
            // Draw min distance sphere (full volume)
            if (showMinDistance)
            {
                Gizmos.color = minDistanceColor;
                Gizmos.DrawWireSphere(position, profile.minDistance);
                Gizmos.DrawSphere(position, 0.5f);
            }
            
            // Draw max distance sphere (zero volume)
            if (showMaxDistance)
            {
                Gizmos.color = maxDistanceColor;
                Gizmos.DrawWireSphere(position, profile.maxDistance);
            }
            
            // Draw max audible distance (auto-cull distance)
            if (showAudibleRange)
            {
                Gizmos.color = audibleRangeColor;
                Gizmos.DrawWireSphere(position, profile.maxAudibleDistance);
            }
            
            // Draw listener position and connection
            if (showListenerPosition && audioListener != null)
            {
                Vector3 listenerPos = audioListener.transform.position;
                Gizmos.color = listenerColor;
                Gizmos.DrawSphere(listenerPos, 1f);
                
                // Draw line to listener
                float distance = Vector3.Distance(position, listenerPos);
                Color lineColor = distance <= profile.maxAudibleDistance ? Color.green : Color.red;
                lineColor.a = 0.5f;
                Gizmos.color = lineColor;
                Gizmos.DrawLine(position, listenerPos);
                
                // Draw distance label in editor
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(
                    (position + listenerPos) * 0.5f,
                    $"{distance:F1}m",
                    new GUIStyle { normal = new GUIStyleState { textColor = lineColor } }
                );
                #endif
            }
            
            // Draw profile name
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(
                position + Vector3.up * 2f,
                $"{profile.profileName}\nMin: {profile.minDistance}m | Max: {profile.maxDistance}m | Audible: {profile.maxAudibleDistance}m",
                new GUIStyle { 
                    normal = new GUIStyleState { textColor = Color.white },
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 10
                }
            );
            #endif
        }
        
        void OnDrawGizmosSelected()
        {
            // Draw more detailed info when selected
            if (!enableGizmos) return;
            
            SpatialAudioProfile profile = GetProfileForType(profileToVisualize);
            
            // Draw custom rolloff curve visualization
            if (profile.rolloffMode == AudioRolloffMode.Custom && profile.customRolloffCurve != null)
            {
                Gizmos.color = Color.cyan;
                Vector3 lastPoint = transform.position + Vector3.right * profile.minDistance;
                
                for (int i = 1; i <= 20; i++)
                {
                    float t = i / 20f;
                    float distance = Mathf.Lerp(profile.minDistance, profile.maxDistance, t);
                    float volume = profile.customRolloffCurve.Evaluate(t);
                    
                    Vector3 point = transform.position + Vector3.right * distance + Vector3.up * volume * 5f;
                    Gizmos.DrawLine(lastPoint, point);
                    lastPoint = point;
                }
            }
        }
    }
}
