using UnityEngine;
using GeminiGauntlet.Audio;

/// <summary>
/// Temporary debug script to identify what sounds are playing
/// </summary>
public class AudioDebugger : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F9))
        {
            LogActiveSounds();
        }
    }

    void LogActiveSounds()
    {
        Debug.Log("═══════════════════════════════════");
        Debug.Log("    ACTIVE SOUNDS DIAGNOSTIC");
        Debug.Log("═══════════════════════════════════");

        if (SoundSystemCore.Instance == null)
        {
            Debug.LogError("SoundSystemCore not initialized!");
            return;
        }

        // Find all PooledAudioSource components in scene
        var allPooledSources = FindObjectsOfType<PooledAudioSource>();
        int activeCount = 0;

        foreach (var source in allPooledSources)
        {
            if (source.IsPlaying())
            {
                activeCount++;
                string clipName = source.AudioSource.clip != null ? source.AudioSource.clip.name : "No Clip";
                bool isLooping = source.AudioSource.loop;
                float volume = source.AudioSource.volume;
                Vector3 position = source.transform.position;

                Debug.Log($"[{activeCount}] {clipName} | Loop: {isLooping} | Vol: {volume:F2} | Pos: {position}");
            }
        }

        Debug.Log($"Total Active: {activeCount}");
        Debug.Log("═══════════════════════════════════");
    }
}
