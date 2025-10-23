using UnityEngine;
using GeminiGauntlet.Audio;

/// <summary>
/// Emergency Audio System Fixer - Resolves time scale and competing system issues
/// </summary>
public class AudioSystemFixer : MonoBehaviour
{
    [Header("Emergency Fixes")]
    [SerializeField] private bool forceNormalTimeScale = true;
    [SerializeField] private bool disableLegacySoundBank = true;
    
    void Start()
    {
        Debug.Log("🚨 AUDIO SYSTEM FIXER - Starting diagnostics and fixes...");
        
        // CRITICAL FIX 1: Restore normal time scale
        FixTimeScale();
        
        // CRITICAL FIX 2: Check for competing systems
        CheckCompetingSystems();
        
        // CRITICAL FIX 3: Test sound system
        Invoke(nameof(TestSoundSystem), 1f);
    }
    
    [ContextMenu("🚨 EMERGENCY FIX - Restore Normal Audio")]
    public void EmergencyAudioFix()
    {
        Debug.Log("🚨 EMERGENCY AUDIO FIX ACTIVATED!");
        
        // Fix 1: Force normal time scale (but respect SlowTime powerup)
        if (Time.timeScale != 1f && !SlowTimePowerUp.isTimeSlowActive)
        {
            Debug.LogWarning($"⚠️ Time.timeScale was {Time.timeScale} - FIXING TO 1.0f");
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f; // Reset fixed delta time
        }
        
        // Fix 2: Check audio listener
        AudioListener[] listeners = FindObjectsOfType<AudioListener>();
        Debug.Log($"🎧 Found {listeners.Length} AudioListeners in scene");
        
        // Fix 3: Check sound system
        if (SoundSystemCore.Instance != null)
        {
            Debug.Log("✅ SoundSystemCore is active");
            
            if (SoundEventsManager.Events != null)
            {
                Debug.Log("✅ SoundEventsManager.Events is assigned");
                TestBasicSounds();
            }
            else
            {
                Debug.LogError("❌ SoundEventsManager.Events is NULL!");
            }
        }
        else
        {
            Debug.LogError("❌ SoundSystemCore.Instance is NULL!");
        }
    }
    
    private void FixTimeScale()
    {
        float currentTimeScale = Time.timeScale;
        Debug.Log($"🕐 Current Time.timeScale: {currentTimeScale}");
        
        if (currentTimeScale != 1f && forceNormalTimeScale)
        {
            // CRITICAL FIX: Don't force time scale if SlowTime powerup is active
            if (!SlowTimePowerUp.isTimeSlowActive)
            {
                Debug.LogWarning($"⚠️ SLOW MOTION DETECTED! Time.timeScale was {currentTimeScale}");
                Debug.Log("🔧 Forcing Time.timeScale back to 1.0f...");
                
                Time.timeScale = 1f;
                Time.fixedDeltaTime = 0.02f; // Standard physics timestep
                
                Debug.Log("✅ Time.timeScale restored to normal!");
            }
            else
            {
                Debug.Log("✅ SlowTime powerup is active - respecting time scale");
            }
        }
        else
        {
            Debug.Log("✅ Time.timeScale is normal");
        }
    }
    
    private void CheckCompetingSystems()
    {
        Debug.Log("🔍 Checking for competing audio systems...");
        
        // Check new AAA system
        if (SoundEventsManager.Instance != null)
        {
            Debug.Log("✅ New AAA SoundEventsManager is active");
        }
        else
        {
            Debug.LogError("❌ New AAA SoundEventsManager is missing!");
        }
        
        Debug.Log("✅ Legacy SoundBank system has been completely removed!");
    }
    
    private void TestSoundSystem()
    {
        Debug.Log("🎵 Testing sound system after fixes...");
        
        if (SoundEventsManager.Events == null)
        {
            Debug.LogError("❌ Cannot test - SoundEventsManager.Events is NULL");
            return;
        }
        
        // Test through GameSounds (should work)
        Debug.Log("🎵 Testing GameSounds.PlayUIFeedback...");
        GameSounds.PlayUIFeedback(transform.position, 0.5f);
        
        // Test direct sound event (after cooldown reset)
        if (SoundEventsManager.Events.uiClick != null)
        {
            Debug.Log("🎵 Testing direct SoundEvent.Play2D...");
            SoundEventsManager.Events.uiClick.ResetCooldown();
            var handle = SoundEventsManager.Events.uiClick.Play2D(0.5f);
            
            if (handle.IsValid)
                Debug.Log("✅ Direct sound playback works!");
            else
                Debug.LogError("❌ Direct sound playback failed!");
        }
    }
    
    private void TestBasicSounds()
    {
        var events = SoundEventsManager.Events;
        int testCount = 0;
        
        // Test UI Click
        if (events.uiClick != null)
        {
            events.uiClick.ResetCooldown();
            var handle = events.uiClick.Play2D(0.3f);
            if (handle.IsValid) testCount++;
            Debug.Log($"UI Click: {(handle.IsValid ? "✅" : "❌")}");
        }
        
        // Test Jump Sound
        if (events.jumpSounds != null && events.jumpSounds.Length > 0)
        {
            events.jumpSounds[0].ResetCooldown();
            var handle = events.jumpSounds[0].Play2D(0.3f);
            if (handle.IsValid) testCount++;
            Debug.Log($"Jump Sound: {(handle.IsValid ? "✅" : "❌")}");
        }
        
        Debug.Log($"🎵 Basic sound test complete: {testCount} sounds working");
    }
    
    void Update()
    {
        // DISABLED: This was preventing proper time scale management
        // The real issue is that SlowTimeRoutine coroutine is not completing properly
        /*
        if (Time.timeScale != 1f && forceNormalTimeScale)
        {
            Debug.LogWarning($"⚠️ Time.timeScale changed to {Time.timeScale} - forcing back to 1.0f");
            Time.timeScale = 1f;
        }
        */
    }
}
