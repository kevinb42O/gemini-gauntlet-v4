using UnityEngine;
using System.Collections.Generic;

namespace GeminiGauntlet.Audio
{
    /// <summary>
    /// DEPRECATED - NO LONGER NEEDED
    /// Skulls now manage their own chatter sounds directly.
    /// This script kept only for backward compatibility - does nothing.
    /// TODO: Remove all references to SkullChatterManager and delete this file.
    /// </summary>
    public class SkullChatterManager : MonoBehaviour
    {
        public static SkullChatterManager Instance { get; private set; }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// DEPRECATED - Skulls handle their own audio now
        /// </summary>
        public void RegisterSkull(Transform skullTransform)
        {
            // Does nothing - skulls handle their own audio
        }

        /// <summary>
        /// DEPRECATED - Skulls handle their own audio now
        /// </summary>
        public void UnregisterSkull(Transform skullTransform)
        {
            // Does nothing - skulls handle their own audio
        }

        /// <summary>
        /// DEPRECATED
        /// </summary>
        public string GetDebugInfo()
        {
            return "SkullChatterManager is deprecated - skulls handle their own audio";
        }
    }
}
