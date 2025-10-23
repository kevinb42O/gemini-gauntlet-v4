using UnityEngine;

namespace GeminiGauntlet.Audio
{
    /// <summary>
    /// Represents a handle to a playing sound event that can be used to stop or modify the sound
    /// </summary>
    [System.Serializable]
    public struct SoundEventHandle
    {
        /// <summary>
        /// Unique ID of the sound event
        /// </summary>
        public int id;
        
        /// <summary>
        /// Whether the handle is valid (references an actual sound)
        /// </summary>
        public bool isValid => id > 0;
        
        /// <summary>
        /// A handle that doesn't reference any sound
        /// </summary>
        public static SoundEventHandle Invalid => new SoundEventHandle { id = 0 };
        
        /// <summary>
        /// Creates a new sound event handle with the given ID
        /// </summary>
        public static SoundEventHandle Create(int id)
        {
            return new SoundEventHandle { id = id };
        }
        
        public override string ToString()
        {
            return isValid ? $"SoundEventHandle({id})" : "SoundEventHandle(Invalid)";
        }
    }
}
