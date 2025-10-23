// --- BossPlatformTrigger.cs (NEW SCRIPT - Specifically for Boss Awakening) ---
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Collider))] // Ensures a Collider is present for the trigger
public class BossPlatformTrigger : MonoBehaviour
{
    [Header("Platform Configuration")]
    [Tooltip("Informational ID for this boss platform, if needed for debugging or complex setups.")]
    public int platformID = 100; // Example: Give it a distinct ID range from tower platforms

    private bool _isPlayerCurrentlyOnPlatform = false;
    private Transform _playerTransformOnPlatform;
    private AudioSource _localAudioSource;
    private AudioSource _globallyActiveActivationSoundSource;
    private AudioClip _globallyActiveActivationClip;
    private TowerController associatedTower;
    private List<SkullEnemy> skullsManagedByThisPlatform = new List<SkullEnemy>();

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError($"BossPlatformTrigger ({name}): Collider component missing! Trigger will not work.", this);
            enabled = false;
            return;
        }
        if (!col.isTrigger)
        {
            Debug.LogWarning($"BossPlatformTrigger ({name}): Collider is not set to 'Is Trigger'. Automatically setting it now.", this);
            col.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (!_isPlayerCurrentlyOnPlatform)
            {
                Debug.Log($"BOSS PLATFORM TRIGGER (ID: {platformID}, Name: {gameObject.name}): PLAYER ENTERED.");
                _isPlayerCurrentlyOnPlatform = true;
                _playerTransformOnPlatform = other.transform;
                // No direct notification needed here; BossEnemy will poll this state.
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (_isPlayerCurrentlyOnPlatform)
            {
                _isPlayerCurrentlyOnPlatform = false;
                _playerTransformOnPlatform = null;
                TriggerLightAnimation(false);

                // Stop sound if we have an AudioSource
                if (_localAudioSource != null && _globallyActiveActivationSoundSource == _localAudioSource && 
                    _localAudioSource.isPlaying && _localAudioSource.clip == _globallyActiveActivationClip)
                {
                    _localAudioSource.Stop();
                    _globallyActiveActivationSoundSource = null;
                    _globallyActiveActivationClip = null;
                }

                // Note: TowerController and SkullEnemy now handle player detection internally
                // No need to manually notify them of player platform state changes
            }
        }
    }

    public bool IsPlayerOnThisPlatform()
    {
        return _isPlayerCurrentlyOnPlatform;
    }

    public void RegisterSkull(SkullEnemy skull, TowerController ToriginatingTower) 
    { 
        if (skull == null || skullsManagedByThisPlatform.Contains(skull)) return; 
        skullsManagedByThisPlatform.Add(skull); 
        // Note: SkullEnemy now handles player detection internally via static cached references
        // No need to manually notify skulls of player platform state 
    }

    public void UnregisterSkull(SkullEnemy skull) 
    { 
        if (skull != null) skullsManagedByThisPlatform.Remove(skull); 
    }

    private void TriggerLightAnimation(bool playerEntering)
    {
        // This is a placeholder method since we don't have light animation in the boss platform
        // You can implement actual light animation logic here if needed
    }
}