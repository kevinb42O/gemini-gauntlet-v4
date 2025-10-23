using UnityEngine;
using UnityEngine.Audio;
using GeminiGauntlet.Audio;
using System.Collections.Generic;

/// <summary>
/// Solves the "warped audio" problem on fast-moving platforms by:
/// 1. Creating a local audio listener space that moves WITH the platform
/// 2. Canceling out Doppler effects for audio sources near the platform
/// 3. Automatically detecting platform-relative sounds vs world-space sounds
/// </summary>
[DefaultExecutionOrder(-100)] // Execute early to ensure audio is fixed before other systems
public class PlatformRelativeAudioManager : MonoBehaviour
{
    [Header("Platform Reference")]
    [Tooltip("The platform the player is currently on - set automatically or can be assigned manually")]
    public Transform currentPlatform;

    [Header("Audio References")]
    [Tooltip("Main audio listener (usually on camera)")]
    public AudioListener mainAudioListener;

    [Header("Audio Processing")]
    [Tooltip("Platform-relative sounds within this radius will have Doppler disabled (note: enforced globally)")]
    [Range(10f, 200f)] 
    public float platformAudioRadius = 50f;
    
    [Tooltip("Whether to create a local audio space for platform sounds")]
    public bool createLocalAudioSpace = true;
    
    // Internal tracking
    private Vector3 _platformVelocity;
    private Vector3 _previousPlatformPosition;
    private float _platformSpeed;
    
    // Tracked audio sources on platform
    private HashSet<AudioSource> _platformRelativeSources = new HashSet<AudioSource>();
    
    // Optional - local audio space
    private Transform _localAudioSpace;
    
    private void Awake()
    {
        if (mainAudioListener == null)
            mainAudioListener = FindObjectOfType<AudioListener>();
            
        if (createLocalAudioSpace)
        {
            CreateLocalAudioSpace();
        }
        
        // Debug log
        Debug.Log("[PlatformRelativeAudioManager] Initialized - Warped Audio Fix Active");
    }
    
    private void CreateLocalAudioSpace()
    {
        GameObject localSpace = new GameObject("PlatformRelativeAudioSpace");
        localSpace.transform.parent = this.transform;
        localSpace.transform.localPosition = Vector3.zero;
        _localAudioSpace = localSpace.transform;
        
        Debug.Log("[PlatformRelativeAudioManager] Created local audio space");
    }
    
    private void Start()
    {
        if (currentPlatform != null)
        {
            _previousPlatformPosition = currentPlatform.position;
        }
    }
    
    private void LateUpdate()
    {
        if (currentPlatform == null)
            return;
            
        // Calculate platform velocity and speed
        _platformVelocity = (currentPlatform.position - _previousPlatformPosition) / Time.deltaTime;
        _previousPlatformPosition = currentPlatform.position;
        _platformSpeed = _platformVelocity.magnitude;
        
        // If we're on a fast-moving platform
        if (_platformSpeed > 5f)
        {
            // Process all audio sources
            ProcessPlatformAudioSources();
            
            // Update local audio space if enabled
            if (createLocalAudioSpace && _localAudioSpace != null)
            {
                // Position local audio space at listener position 
                _localAudioSpace.position = mainAudioListener.transform.position;
                _localAudioSpace.rotation = mainAudioListener.transform.rotation;
            }
        }
    }
    
    private void ProcessPlatformAudioSources()
    {
        // Find all active pooled audio sources
        PooledAudioSource[] activeSources = FindObjectsOfType<PooledAudioSource>();
        
        foreach (var pooledSource in activeSources)
        {
            // Skip null sources
            if (pooledSource == null || pooledSource.AudioSource == null)
                continue;
                
            AudioSource source = pooledSource.AudioSource;
            
            // Check if it's within our platform radius
            float distanceToListener = Vector3.Distance(source.transform.position, mainAudioListener.transform.position);
            
            if (distanceToListener < platformAudioRadius)
            {
                // Add to our tracked set
                if (!_platformRelativeSources.Contains(source))
                {
                    _platformRelativeSources.Add(source);
                    
                    // Doppler is globally disabled; ensure this source stays at zero
                    source.dopplerLevel = 0f;
                    
                    // Place in local audio space if enabled
                    if (createLocalAudioSpace && _localAudioSpace != null)
                    {
                        if (!pooledSource.followTransform)
                        {
                            // For non-attached sounds, parent to local audio space
                            Vector3 worldPos = source.transform.position;
                            source.transform.SetParent(_localAudioSpace);
                            source.transform.position = worldPos; // Keep world position
                        }
                    }
                    
                    Debug.Log($"[PlatformRelativeAudioManager] Fixed warped audio: {source.clip?.name} (Distance: {distanceToListener:F1})");
                }
            }
            else
            {
                // Remove from tracked sources if it moved outside radius
                if (_platformRelativeSources.Contains(source))
                {
                    _platformRelativeSources.Remove(source);
                    // Doppler stays disabled globally
                    source.dopplerLevel = 0f;
                }
            }
        }
        
        // Clean up any null references
        _platformRelativeSources.RemoveWhere(source => source == null);
    }
    
    /// <summary>
    /// Call this when the player changes platforms
    /// </summary>
    public void SetCurrentPlatform(Transform platform)
    {
        if (platform == currentPlatform)
            return;
            
        currentPlatform = platform;
        _previousPlatformPosition = platform != null ? platform.position : Vector3.zero;
        _platformVelocity = Vector3.zero;
        
        // Clear tracked sources when changing platforms
        _platformRelativeSources.Clear();
        
        Debug.Log($"[PlatformRelativeAudioManager] Platform changed: {(platform != null ? platform.name : "none")}");
    }
}
