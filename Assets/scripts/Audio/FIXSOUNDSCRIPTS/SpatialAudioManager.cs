using UnityEngine;
using System.Collections.Generic;

namespace GeminiGauntlet.Audio
{
    /// <summary>
    /// AAA Spatial Audio Manager - Handles distance-based cleanup and 3D audio optimization
    /// Solves the "skull chatter chaos" and ensures sounds respect 3D distance properly
    /// </summary>
    public class SpatialAudioManager : MonoBehaviour
    {
        public static SpatialAudioManager Instance { get; private set; }
        
        [Header("=== DISTANCE CULLING ===")]
        [SerializeField] private bool enableDistanceCulling = true;
        [SerializeField] private float globalMaxAudibleDistance = 20000f; // Scaled for large game world (3200-unit platforms)
        [SerializeField] private float cullCheckInterval = 0.5f;
        
        [Header("=== DEBUGGING ===")]
        [SerializeField] private bool showDebugLogs = false;
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private Color audibleRangeColor = new Color(0f, 1f, 0f, 0.2f);
        [SerializeField] private Color culledRangeColor = new Color(1f, 0f, 0f, 0.2f);
        
        // Tracked looping sounds for distance-based cleanup
        private class TrackedSound
        {
            public SoundHandle handle;
            public Transform attachedTransform;
            public Vector3 lastPosition;
            public SpatialAudioProfile profile;
            public float lastCheckTime;
            public bool isLooping;
            
            public Vector3 GetCurrentPosition()
            {
                if (attachedTransform != null)
                    return attachedTransform.position;
                return lastPosition;
            }
        }
        
        private List<TrackedSound> trackedSounds = new List<TrackedSound>();
        private AudioListener audioListener;
        private float lastGlobalCheckTime;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void Start()
        {
            // Find audio listener
            audioListener = FindObjectOfType<AudioListener>();
            if (audioListener == null && Camera.main != null)
            {
                audioListener = Camera.main.GetComponent<AudioListener>();
            }
            
            if (audioListener == null)
            {
                Debug.LogWarning("⚠️ SpatialAudioManager: No AudioListener found!");
            }
        }
        
        void Update()
        {
            if (!enableDistanceCulling) return;
            
            // Periodic distance check for all tracked sounds
            if (Time.time - lastGlobalCheckTime >= cullCheckInterval)
            {
                CheckAndCullDistantSounds();
                lastGlobalCheckTime = Time.time;
            }
        }
        
        /// <summary>
        /// Track a looping sound for distance-based cleanup
        /// </summary>
        public void TrackLoopingSound(SoundHandle handle, Transform attachedTransform, SpatialAudioProfile profile)
        {
            if (handle == null || !handle.IsValid) return;
            
            var tracked = new TrackedSound
            {
                handle = handle,
                attachedTransform = attachedTransform,
                lastPosition = attachedTransform != null ? attachedTransform.position : Vector3.zero,
                profile = profile ?? SpatialAudioProfiles.GenericSFX,
                lastCheckTime = Time.time,
                isLooping = true
            };
            
            trackedSounds.Add(tracked);
            
            if (showDebugLogs)
            {
                Debug.Log($"🎵 Tracking looping sound: {profile?.profileName ?? "Unknown"} at {tracked.GetCurrentPosition()}");
            }
        }
        
        /// <summary>
        /// Untrack a sound (when manually stopped)
        /// </summary>
        public void UntrackSound(SoundHandle handle)
        {
            if (handle == null) return;
            
            trackedSounds.RemoveAll(t => t.handle == handle);
        }
        
        /// <summary>
        /// Check all tracked sounds and cull those beyond audible range
        /// </summary>
        private void CheckAndCullDistantSounds()
        {
            if (audioListener == null || trackedSounds.Count == 0) return;
            
            Vector3 listenerPos = audioListener.transform.position;
            int culledCount = 0;
            
            // Check in reverse to safely remove while iterating
            for (int i = trackedSounds.Count - 1; i >= 0; i--)
            {
                var tracked = trackedSounds[i];
                
                // Remove invalid handles
                if (tracked.handle == null || !tracked.handle.IsValid)
                {
                    trackedSounds.RemoveAt(i);
                    continue;
                }
                
                // Remove if attached transform was destroyed
                if (tracked.attachedTransform == null && tracked.isLooping)
                {
                    if (showDebugLogs)
                    {
                        Debug.Log($"🧹 Stopping orphaned sound (transform destroyed)");
                    }
                    tracked.handle.Stop();
                    trackedSounds.RemoveAt(i);
                    culledCount++;
                    continue;
                }
                
                // Check distance
                Vector3 soundPos = tracked.GetCurrentPosition();
                float distance = Vector3.Distance(listenerPos, soundPos);
                float maxDistance = tracked.profile != null ? tracked.profile.maxAudibleDistance : globalMaxAudibleDistance;
                
                // Cull if beyond audible range
                if (distance > maxDistance)
                {
                    if (showDebugLogs)
                    {
                        Debug.Log($"🧹 Culling distant sound: {tracked.profile?.profileName} at {distance:F1}m (max: {maxDistance:F1}m)");
                    }
                    
                    // Fade out before stopping
                    float fadeTime = tracked.profile?.distanceCullFadeOut ?? 0.2f;
                    tracked.handle.FadeOut(fadeTime);
                    
                    trackedSounds.RemoveAt(i);
                    culledCount++;
                }
            }
            
            if (culledCount > 0 && showDebugLogs)
            {
                Debug.Log($"🧹 Distance culling: Stopped {culledCount} sounds beyond audible range");
            }
        }
        
        /// <summary>
        /// Get distance from audio listener to position
        /// </summary>
        public float GetDistanceFromListener(Vector3 position)
        {
            if (audioListener == null) return 0f;
            return Vector3.Distance(audioListener.transform.position, position);
        }
        
        /// <summary>
        /// Check if a position is within audible range
        /// </summary>
        public bool IsAudible(Vector3 position, float maxDistance)
        {
            return GetDistanceFromListener(position) <= maxDistance;
        }
        
        /// <summary>
        /// Force cleanup all tracked sounds (for scene transitions)
        /// </summary>
        public void CleanupAllTrackedSounds()
        {
            if (showDebugLogs)
            {
                Debug.Log($"🧹 Force cleanup: Stopping {trackedSounds.Count} tracked sounds");
            }
            
            foreach (var tracked in trackedSounds)
            {
                if (tracked.handle != null && tracked.handle.IsValid)
                {
                    tracked.handle.Stop();
                }
            }
            
            trackedSounds.Clear();
        }
        
        /// <summary>
        /// Get diagnostic info about tracked sounds
        /// </summary>
        public string GetDiagnosticInfo()
        {
            if (audioListener == null) return "No AudioListener";
            
            Vector3 listenerPos = audioListener.transform.position;
            int totalTracked = trackedSounds.Count;
            int audible = 0;
            int distant = 0;
            
            foreach (var tracked in trackedSounds)
            {
                if (tracked.handle == null || !tracked.handle.IsValid) continue;
                
                float distance = Vector3.Distance(listenerPos, tracked.GetCurrentPosition());
                float maxDistance = tracked.profile?.maxAudibleDistance ?? globalMaxAudibleDistance;
                
                if (distance <= maxDistance)
                    audible++;
                else
                    distant++;
            }
            
            return $"Tracked: {totalTracked} | Audible: {audible} | Distant: {distant}";
        }
        
        void OnDrawGizmos()
        {
            if (!showGizmos || !Application.isPlaying || audioListener == null) return;
            
            Vector3 listenerPos = audioListener.transform.position;
            
            // Draw each tracked sound's position and audible range
            foreach (var tracked in trackedSounds)
            {
                if (tracked.handle == null || !tracked.handle.IsValid) continue;
                
                Vector3 soundPos = tracked.GetCurrentPosition();
                float distance = Vector3.Distance(listenerPos, soundPos);
                float maxDistance = tracked.profile?.maxAudibleDistance ?? globalMaxAudibleDistance;
                
                // Color based on audibility
                Color color = distance <= maxDistance ? audibleRangeColor : culledRangeColor;
                Gizmos.color = color;
                
                // Draw sphere at sound position
                Gizmos.DrawSphere(soundPos, 1f);
                
                // Draw line to listener
                Gizmos.DrawLine(soundPos, listenerPos);
                
                // Draw audible range
                Gizmos.color = new Color(color.r, color.g, color.b, 0.1f);
                Gizmos.DrawWireSphere(soundPos, maxDistance);
            }
        }
        
        void OnDestroy()
        {
            if (Instance == this)
            {
                CleanupAllTrackedSounds();
                Instance = null;
            }
        }
    }
}
