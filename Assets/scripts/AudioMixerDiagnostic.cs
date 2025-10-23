using UnityEngine;
using UnityEngine.Audio;
using GeminiGauntlet.Audio;

/// <summary>
/// CRITICAL: Diagnose why SoundEvents system sounds are inaudible
/// </summary>
public class AudioMixerDiagnostic : MonoBehaviour
{
    void Start()
    {
        Debug.Log("🚨 AUDIO MIXER DIAGNOSTIC - Finding why SoundEvents sounds are inaudible...");
        Invoke(nameof(RunFullDiagnostic), 1f);
    }
    
    [ContextMenu("🚨 DIAGNOSE SILENT SOUNDEVENTS")]
    public void RunFullDiagnostic()
    {
        Debug.Log("=================== AUDIO MIXER DIAGNOSTIC ===================");
        
        // 1. Check SoundSystemCore
        CheckSoundSystemCore();
        
        // 2. Check AudioMixer Groups
        CheckAudioMixerGroups();
        
        // 3. Check SoundEvents Asset
        CheckSoundEventsAsset();
        
        // 4. Test Raw Audio Source
        TestRawAudioSource();
        
        // 5. Test SoundSystemCore Directly
        TestSoundSystemCoreDirect();
        
        Debug.Log("=================== DIAGNOSTIC COMPLETE ===================");
    }
    
    private void CheckSoundSystemCore()
    {
        Debug.Log("🔍 1. CHECKING SOUNDSYSTEMCORE...");
        
        if (SoundSystemCore.Instance == null)
        {
            Debug.LogError("❌ SoundSystemCore.Instance is NULL! System not initialized!");
            return;
        }
        
        Debug.Log("✅ SoundSystemCore.Instance exists");
        
        // Check audio sources
        var audioSources = SoundSystemCore.Instance.GetComponentsInChildren<AudioSource>();
        Debug.Log($"🎧 SoundSystemCore has {audioSources.Length} AudioSources");
        
        foreach (var source in audioSources)
        {
            Debug.Log($"   AudioSource: enabled={source.enabled}, volume={source.volume}, " +
                     $"outputAudioMixerGroup={source.outputAudioMixerGroup?.name ?? "NULL"}");
        }
    }
    
    private void CheckAudioMixerGroups()
    {
        Debug.Log("🔍 2. CHECKING AUDIOMIXER GROUPS...");
        
        // Find all AudioMixerGroups in the scene
        var allMixerGroups = FindObjectsOfType<AudioMixerGroup>();
        Debug.Log($"🎚️ Found {allMixerGroups.Length} AudioMixerGroups in scene");
        
        // Check for master mixer from bootstrap
        var bootstrap = FindObjectOfType<SoundSystemBootstrap>();
        if (bootstrap != null)
        {
            Debug.Log("✅ SoundSystemBootstrap found");
            
            // Use reflection to check mixer field
            var mixerField = bootstrap.GetType().GetField("masterMixer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (mixerField != null)
            {
                var mixer = mixerField.GetValue(bootstrap) as AudioMixer;
                if (mixer != null)
                {
                    Debug.Log($"✅ Master AudioMixer found: {mixer.name}");
                    CheckMixerGroupVolumes(mixer);
                }
                else
                {
                    Debug.LogError("❌ Master AudioMixer is NULL in bootstrap!");
                }
            }
        }
        else
        {
            Debug.LogError("❌ SoundSystemBootstrap not found!");
        }
    }
    
    private void CheckMixerGroupVolumes(AudioMixer mixer)
    {
        Debug.Log("🔍 3. CHECKING MIXER GROUP VOLUMES...");
        
        string[] groupNames = { "Master", "SFX", "UI", "Music", "Ambient" };
        
        foreach (string groupName in groupNames)
        {
            if (mixer.GetFloat(groupName, out float volume))
            {
                bool isMuted = volume <= -80f;
                Debug.Log($"🎚️ {groupName}: {volume:F1}dB {(isMuted ? "❌ MUTED!" : "✅")}");
                
                if (isMuted)
                {
                    Debug.LogError($"🚨 CRITICAL: {groupName} group is MUTED! This explains the silence!");
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ Could not get volume for group: {groupName}");
            }
        }
    }
    
    private void CheckSoundEventsAsset()
    {
        Debug.Log("🔍 4. CHECKING SOUNDEVENTS ASSET...");
        
        if (SoundEventsManager.Events == null)
        {
            Debug.LogError("❌ SoundEventsManager.Events is NULL!");
            return;
        }
        
        Debug.Log("✅ SoundEventsManager.Events is assigned");
        
        // Check if sounds have clips
        var events = SoundEventsManager.Events;
        int validSounds = 0;
        int totalSounds = 0;
        
        if (events.uiClick != null)
        {
            totalSounds++;
            if (events.uiClick.clip != null)
                validSounds++;
        }
        
        if (events.jumpSounds != null)
        {
            totalSounds += events.jumpSounds.Length;
            foreach (var sound in events.jumpSounds)
            {
                if (sound != null && sound.clip != null)
                    validSounds++;
            }
        }
        
        Debug.Log($"🎵 Valid sounds: {validSounds}/{totalSounds}");
    }
    
    private void TestRawAudioSource()
    {
        Debug.Log("🔍 5. TESTING RAW AUDIOSOURCE...");
        
        // Test if we can play a direct AudioSource sound
        var testSource = gameObject.GetComponent<AudioSource>();
        if (testSource == null)
        {
            testSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Get a clip from SoundEvents
        if (SoundEventsManager.Events?.uiClick?.clip != null)
        {
            var clip = SoundEventsManager.Events.uiClick.clip;
            testSource.clip = clip;
            testSource.volume = 0.5f;
            testSource.outputAudioMixerGroup = null; // Direct output
            testSource.Play();
            
            Debug.Log("🎵 Playing test sound with RAW AudioSource (no mixer group)");
            Debug.Log($"   Clip: {clip.name}, Length: {clip.length}s");
        }
        else
        {
            Debug.LogError("❌ No audio clip available for raw test");
        }
    }
    
    private void TestSoundSystemCoreDirect()
    {
        Debug.Log("🔍 6. TESTING SOUNDSYSTEMCORE DIRECT...");
        
        if (SoundSystemCore.Instance == null)
        {
            Debug.LogError("❌ Cannot test - SoundSystemCore is NULL");
            return;
        }
        
        // Try to force play a sound through the system
        if (SoundEventsManager.Events?.uiClick != null)
        {
            Debug.Log("🎵 Forcing SoundEvent.Play2D through system...");
            
            var uiClick = SoundEventsManager.Events.uiClick;
            uiClick.ResetCooldown();
            
            var handle = uiClick.Play2D(1.0f);
            Debug.Log($"   SoundHandle.IsValid: {handle.IsValid}");
            
            if (handle.IsValid)
            {
                Debug.Log("✅ SoundSystemCore accepted the sound request");
            }
            else
            {
                Debug.LogError("❌ SoundSystemCore rejected the sound request");
            }
        }
    }
    
    [ContextMenu("🔧 EMERGENCY FIX - Unmute All Groups")]
    public void EmergencyUnmuteAllGroups()
    {
        Debug.Log("🚨 EMERGENCY: Attempting to unmute all AudioMixer groups...");
        
        var bootstrap = FindObjectOfType<SoundSystemBootstrap>();
        if (bootstrap == null)
        {
            Debug.LogError("❌ Cannot fix - SoundSystemBootstrap not found!");
            return;
        }
        
        var mixerField = bootstrap.GetType().GetField("masterMixer", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (mixerField != null)
        {
            var mixer = mixerField.GetValue(bootstrap) as AudioMixer;
            if (mixer != null)
            {
                Debug.Log("🔧 Unmuting all mixer groups...");
                
                string[] groupNames = { "Master", "SFX", "UI", "Music", "Ambient" };
                
                foreach (string groupName in groupNames)
                {
                    mixer.SetFloat(groupName, 0f); // 0dB = normal volume
                    Debug.Log($"✅ Unmuted {groupName} to 0dB");
                }
                
                Debug.Log("🎵 All groups unmuted! Try your sounds now!");
            }
        }
    }
}
