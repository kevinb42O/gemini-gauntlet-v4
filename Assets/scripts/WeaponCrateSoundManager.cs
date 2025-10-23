using UnityEngine;
using System.Collections.Generic;
using GeminiGauntlet.Audio;

/// <summary>
/// Dedicated sound manager for all weapon crate-related audio events
/// Handles crate opening, weapon spawning, and weapon-specific sound effects
/// Routes all audio through the Audio Mixer via GameSounds system
/// Allows assignment of multiple sounds for variety and customization
/// </summary>
public class WeaponCrateSoundManager : MonoBehaviour
{
    [Header("Crate Opening Sounds")]
    [Tooltip("Multiple sounds for crate opening - one will be chosen randomly")]
    public List<AudioClip> crateOpeningSounds = new List<AudioClip>();
    [SerializeField] [Range(0f, 1f)] private float openingVolume = 0.8f;
    
    [Header("Weapon Spawn Sounds")]
    [Tooltip("Multiple sounds for weapon spawning - one will be chosen randomly")]
    public List<AudioClip> weaponSpawnSounds = new List<AudioClip>();
    [SerializeField] [Range(0f, 1f)] private float spawnVolume = 0.7f;
    
    [Header("Crate Emergence Sounds")]
    [Tooltip("Multiple sounds for crate emerging from ground - one will be chosen randomly")]
    public List<AudioClip> crateEmergenceSounds = new List<AudioClip>();
    [SerializeField] [Range(0f, 1f)] private float emergenceVolume = 0.6f;
    
    [Header("Weapon Collection Sounds")]
    [Tooltip("Multiple sounds for weapon collection - one will be chosen randomly")]
    public List<AudioClip> weaponCollectionSounds = new List<AudioClip>();
    [SerializeField] [Range(0f, 1f)] private float collectionVolume = 0.9f;
    
    [Header("Crate Destruction Sounds")]
    [Tooltip("Multiple sounds for crate destruction/breaking - one will be chosen randomly")]
    public List<AudioClip> crateDestructionSounds = new List<AudioClip>();
    [SerializeField] [Range(0f, 1f)] private float destructionVolume = 0.8f;
    
    [Header("Weapon Activation Sounds")]
    [Tooltip("Multiple sounds for weapon activation/equipping - one will be chosen randomly")]
    public List<AudioClip> weaponActivationSounds = new List<AudioClip>();
    [SerializeField] [Range(0f, 1f)] private float activationVolume = 0.7f;
    
    [Header("Ambient Crate Sounds")]

    private SoundHandle ambientHandle = SoundHandle.Invalid;
    [Tooltip("Multiple ambient sounds for crate humming/energy - one will be chosen randomly")]
    public List<AudioClip> crateAmbientSounds = new List<AudioClip>();
    [SerializeField] [Range(0f, 1f)] private float ambientVolume = 0.4f;
    
    [Header("Special Effect Sounds")]
    [Tooltip("Multiple special effect sounds for rare/legendary weapons - one will be chosen randomly")]
    public List<AudioClip> specialEffectSounds = new List<AudioClip>();
    [SerializeField] [Range(0f, 1f)] private float specialEffectVolume = 1.0f;
    
    // We will play assigned AudioClips directly using SoundSystemCore when available
    // Fallback to AudioSource.PlayClipAtPoint if core is not ready
    
    /// <summary>
    /// Play crate opening sound - chooses randomly from assigned sounds
    /// </summary>
    public void PlayCrateOpening()
    {
        if (crateOpeningSounds.Count > 0)
        {
            AudioClip selectedSound = GetRandomSound(crateOpeningSounds);
            if (selectedSound != null)
            {
                PlayClipAtPosition(selectedSound, openingVolume);
                Debug.Log($"WeaponCrateSoundManager: Playing crate opening sound '{selectedSound.name}' at volume {openingVolume}");
            }
        }
        else
        {
            Debug.LogWarning("WeaponCrateSoundManager: No crate opening sounds assigned!");
        }
    }
    
    /// <summary>
    /// Play weapon spawn sound - chooses randomly from assigned sounds
    /// </summary>
    public void PlayWeaponSpawn()
    {
        if (weaponSpawnSounds.Count > 0)
        {
            AudioClip selectedSound = GetRandomSound(weaponSpawnSounds);
            if (selectedSound != null)
            {
                PlayClipAtPosition(selectedSound, spawnVolume);
                Debug.Log($"WeaponCrateSoundManager: Playing weapon spawn sound '{selectedSound.name}' at volume {spawnVolume}");
            }
        }
        else
        {
            Debug.LogWarning("WeaponCrateSoundManager: No weapon spawn sounds assigned!");
        }
    }
    
    /// <summary>
    /// Play crate emergence sound - chooses randomly from assigned sounds
    /// </summary>
    public void PlayCrateEmergence()
    {
        if (crateEmergenceSounds.Count > 0)
        {
            AudioClip selectedSound = GetRandomSound(crateEmergenceSounds);
            if (selectedSound != null)
            {
                PlayClipAtPosition(selectedSound, emergenceVolume);
                Debug.Log($"WeaponCrateSoundManager: Playing crate emergence sound '{selectedSound.name}' at volume {emergenceVolume}");
            }
        }
        else
        {
            Debug.LogWarning("WeaponCrateSoundManager: No crate emergence sounds assigned!");
        }
    }
    
    /// <summary>
    /// Play weapon collection sound - chooses randomly from assigned sounds
    /// </summary>
    public void PlayWeaponCollection()
    {
        if (weaponCollectionSounds.Count > 0)
        {
            AudioClip selectedSound = GetRandomSound(weaponCollectionSounds);
            if (selectedSound != null)
            {
                PlayClipAtPosition(selectedSound, collectionVolume);
                Debug.Log($"WeaponCrateSoundManager: Playing weapon collection sound '{selectedSound.name}' at volume {collectionVolume}");
            }
        }
        else
        {
            Debug.LogWarning("WeaponCrateSoundManager: No weapon collection sounds assigned!");
        }
    }
    
    /// <summary>
    /// Play crate destruction sound - chooses randomly from assigned sounds
    /// </summary>
    public void PlayCrateDestruction()
    {
        if (crateDestructionSounds.Count > 0)
        {
            AudioClip selectedSound = GetRandomSound(crateDestructionSounds);
            if (selectedSound != null)
            {
                PlayClipAtPosition(selectedSound, destructionVolume);
                Debug.Log($"WeaponCrateSoundManager: Playing crate destruction sound '{selectedSound.name}' at volume {destructionVolume}");
            }
        }
        else
        {
            Debug.LogWarning("WeaponCrateSoundManager: No crate destruction sounds assigned!");
        }
    }
    
    /// <summary>
    /// Play weapon activation sound - chooses randomly from assigned sounds
    /// </summary>
    public void PlayWeaponActivation()
    {
        if (weaponActivationSounds.Count > 0)
        {
            AudioClip selectedSound = GetRandomSound(weaponActivationSounds);
            if (selectedSound != null)
            {
                PlayClipAtPosition(selectedSound, activationVolume);
                Debug.Log($"WeaponCrateSoundManager: Playing weapon activation sound '{selectedSound.name}' at volume {activationVolume}");
            }
        }
        else
        {
            Debug.LogWarning("WeaponCrateSoundManager: No weapon activation sounds assigned!");
        }
    }
    
    /// <summary>
    /// Start playing ambient crate sound (looped) - chooses randomly from assigned sounds
    /// </summary>
    public void StartCrateAmbient()
    {
        if (crateAmbientSounds.Count > 0)
        {
            AudioClip selectedSound = GetRandomSound(crateAmbientSounds);
            if (selectedSound != null)
            {
                PlayClipAtPosition(selectedSound, ambientVolume, loop:true);
                ambientHandle = currentHandle;
                Debug.Log($"WeaponCrateSoundManager: Starting ambient crate sound '{selectedSound.name}' at volume {ambientVolume}");
            }
        }
        else
        {
            Debug.LogWarning("WeaponCrateSoundManager: No ambient crate sounds assigned!");
        }
    }
    
    /// <summary>
    /// Stop playing ambient crate sound
    /// </summary>
    public void StopCrateAmbient()
    {
        if (ambientHandle.IsValid)
        {
            ambientHandle.Stop();
            ambientHandle = SoundHandle.Invalid;
        }
        Debug.Log("WeaponCrateSoundManager: Stopped ambient crate sound");
    }
    
    /// <summary>
    /// Play special effect sound for rare/legendary weapons - chooses randomly from assigned sounds
    /// </summary>
    /// <summary>
    /// Play a random clip from list at given volume helper
    /// </summary>
    private void PlayRandomSound(List<AudioClip> soundList, float volume)
    {
        AudioClip clip = GetRandomSound(soundList);
        if (clip != null)
        {
            PlayClipAtPosition(clip, volume);
            Debug.Log($"WeaponCrateSoundManager: Playing random sound '{clip.name}' at volume {volume}");
        }
        else
        {
            Debug.LogWarning("WeaponCrateSoundManager: PlayRandomSound called with empty or null list");
        }
    }

    public void PlaySpecialEffect()
    {
        PlayRandomSound(specialEffectSounds, specialEffectVolume);
    }
    
    /// <summary>
    /// Play a specific sound by name from any category
    /// </summary>
    public void PlaySoundByName(string soundName)
    {
        AudioClip foundClip = FindSoundByName(soundName);
        if (foundClip != null)
        {
            PlayClipAtPosition(foundClip, 0.8f);
            Debug.Log($"WeaponCrateSoundManager: Playing specific sound '{soundName}'");
        }
        else
        {
            Debug.LogWarning($"WeaponCrateSoundManager: Sound '{soundName}' not found in any category!");
        }
    }
    
    /// <summary>
    /// Get a random sound from a list
    /// </summary>
    private AudioClip GetRandomSound(List<AudioClip> soundList)
    {
        if (soundList == null || soundList.Count == 0)
            return null;
            
        // Filter out null entries
        List<AudioClip> validSounds = new List<AudioClip>();
        foreach (AudioClip clip in soundList)
        {
            if (clip != null)
                validSounds.Add(clip);
        }
        
        if (validSounds.Count == 0)
            return null;
            
        int randomIndex = Random.Range(0, validSounds.Count);
        return validSounds[randomIndex];
    }
    
    /// <summary>
    /// Find a sound by name across all categories
    /// </summary>
    private AudioClip FindSoundByName(string soundName)
    {
        // Search through all sound lists
        List<List<AudioClip>> allSoundLists = new List<List<AudioClip>>
        {
            crateOpeningSounds,
            weaponSpawnSounds,
            crateEmergenceSounds,
            weaponCollectionSounds,
            crateDestructionSounds,
            weaponActivationSounds,
            crateAmbientSounds,
            specialEffectSounds
        };
        
        foreach (List<AudioClip> soundList in allSoundLists)
        {
            foreach (AudioClip clip in soundList)
            {
                if (clip != null && clip.name.Equals(soundName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return clip;
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Set volume for a specific sound category
    /// </summary>
    public void SetCategoryVolume(string category, float volume)
    {
        volume = Mathf.Clamp01(volume);
        
        switch (category.ToLower())
        {
            case "opening":
                openingVolume = volume;
                break;
            case "spawn":
                spawnVolume = volume;
                break;
            case "emergence":
                emergenceVolume = volume;
                break;
            case "collection":
                collectionVolume = volume;
                break;
            case "destruction":
                destructionVolume = volume;
                break;
            case "activation":
                activationVolume = volume;
                break;
            case "ambient":
                ambientVolume = volume;
                break;
            case "special":
                specialEffectVolume = volume;
                break;
            default:
                Debug.LogWarning($"WeaponCrateSoundManager: Unknown category '{category}'");
                break;
        }
        
        Debug.Log($"WeaponCrateSoundManager: Set {category} volume to {volume}");
    }
    
    /// <summary>
    /// Get count of sounds in a specific category
    /// </summary>
    public int GetSoundCount(string category)
    {
        switch (category.ToLower())
        {
            case "opening": return crateOpeningSounds.Count;
            case "spawn": return weaponSpawnSounds.Count;
            case "emergence": return crateEmergenceSounds.Count;
            case "collection": return weaponCollectionSounds.Count;
            case "destruction": return crateDestructionSounds.Count;
            case "activation": return weaponActivationSounds.Count;
            case "ambient": return crateAmbientSounds.Count;
            case "special": return specialEffectSounds.Count;
            default: return 0;
        }
    }
    
    /// <summary>
    /// Debug method to list all assigned sounds
    /// </summary>
    [ContextMenu("List All Assigned Sounds")]
    public void ListAllAssignedSounds()
    {
        Debug.Log("=== WeaponCrateSoundManager - All Assigned Sounds ===");
        LogSoundCategory("Crate Opening", crateOpeningSounds);
        LogSoundCategory("Weapon Spawn", weaponSpawnSounds);
        LogSoundCategory("Crate Emergence", crateEmergenceSounds);
        LogSoundCategory("Weapon Collection", weaponCollectionSounds);
        LogSoundCategory("Crate Destruction", crateDestructionSounds);
        LogSoundCategory("Weapon Activation", weaponActivationSounds);
        LogSoundCategory("Crate Ambient", crateAmbientSounds);
        LogSoundCategory("Special Effects", specialEffectSounds);
    }
    
    private void LogSoundCategory(string categoryName, List<AudioClip> sounds)
    {
        Debug.Log($"{categoryName} ({sounds.Count} sounds):");
        for (int i = 0; i < sounds.Count; i++)
        {
            if (sounds[i] != null)
            {
                Debug.Log($"  [{i}] {sounds[i].name}");
            }
            else
            {
                Debug.Log($"  [{i}] <NULL>");
            }
        }
    }    /// <summary>
    /// Helper to play clip at position through SoundSystemCore with fallback.
    /// </summary>
    private SoundHandle currentHandle = SoundHandle.Invalid;
    private void PlayClipAtPosition(AudioClip clip, float volume, bool loop=false)
    {
        if (clip == null) return;
        // Try SoundSystemCore if available
        if (SoundSystemCore.Instance != null)
        {
            currentHandle = SoundSystemCore.Instance.PlaySound3D(
                clip,
                transform.position,
                SoundCategory.SFX,
                volume,
                1f);

            if (loop && currentHandle.IsValid)
            {
                // Looping requires manual restart because pooled sources auto-stop
                // This legacy system should migrate to SoundEvent assets for proper looping
                Debug.LogWarning("WeaponCrateSoundManager: Looping via PlayClipAtPosition is deprecated. Please migrate to SoundEvent assets.");
            }
        }
        else
        {
            // Fallback to one-shot play
            AudioSource.PlayClipAtPoint(clip, transform.position, volume);
        }
    }
}

