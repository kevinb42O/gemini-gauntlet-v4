using UnityEngine;
using System.Text;

namespace GeminiGauntlet.Audio
{
    /// <summary>
    /// Real-time health monitoring for the audio system.
    /// Displays warnings when system is under stress and helps identify performance issues.
    /// </summary>
    public class AudioSystemHealthMonitor : MonoBehaviour
    {
        [Header("Display Settings")]
        [SerializeField] private bool showOnScreenDisplay = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.F8;
        [SerializeField] private int fontSize = 14;
        
        [Header("Warning Thresholds")]
        [SerializeField] private int warningActiveSounds = 25;
        [SerializeField] private int criticalActiveSounds = 30;
        
        private StringBuilder displayText = new StringBuilder();
        private GUIStyle textStyle;
        private bool initialized = false;

        void Start()
        {
            InitializeStyle();
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                showOnScreenDisplay = !showOnScreenDisplay;
            }
        }

        void OnGUI()
        {
            if (!showOnScreenDisplay) return;
            if (!initialized) InitializeStyle();

            displayText.Clear();
            
            // Header
            displayText.AppendLine("═══ AUDIO SYSTEM HEALTH ═══");
            displayText.AppendLine();

            // Sound System Core Stats
            if (SoundSystemCore.Instance != null)
            {
                int activeSounds = SoundSystemCore.Instance.GetActiveSoundCount();
                int availableSources = SoundSystemCore.Instance.GetAvailableSourceCount();
                int totalPool = activeSounds + availableSources;

                displayText.AppendLine("▼ Sound Pool:");
                displayText.AppendLine($"  Active: {activeSounds} / {totalPool}");
                displayText.AppendLine($"  Available: {availableSources}");
                
                // Warning indicators
                if (activeSounds >= criticalActiveSounds)
                {
                    displayText.AppendLine("  ⚠️ CRITICAL: Pool near exhaustion!");
                }
                else if (activeSounds >= warningActiveSounds)
                {
                    displayText.AppendLine("  ⚠️ WARNING: High pool usage");
                }
                else
                {
                    displayText.AppendLine("  ✅ Pool healthy");
                }
            }
            else
            {
                displayText.AppendLine("▼ Sound Pool: NOT INITIALIZED");
            }

            displayText.AppendLine();

            // Skull Chatter Stats
            if (SkullChatterManager.Instance != null)
            {
                displayText.AppendLine("▼ Skull Chatter:");
                displayText.AppendLine($"  {SkullChatterManager.Instance.GetDebugInfo()}");
            }

            displayText.AppendLine();

            // Performance Tips
            displayText.AppendLine("▼ Controls:");
            displayText.AppendLine($"  {toggleKey} = Toggle Display");

            // Draw the text
            GUI.Label(new Rect(10, 10, 400, 400), displayText.ToString(), textStyle);
        }

        private void InitializeStyle()
        {
            textStyle = new GUIStyle();
            textStyle.fontSize = fontSize;
            textStyle.normal.textColor = Color.white;
            textStyle.alignment = TextAnchor.UpperLeft;
            textStyle.wordWrap = false;
            
            // Add shadow for better readability
            textStyle.normal.background = MakeTex(2, 2, new Color(0, 0, 0, 0.7f));
            textStyle.padding = new RectOffset(10, 10, 10, 10);
            
            initialized = true;
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        /// <summary>
        /// Log detailed audio system stats to console
        /// </summary>
        public void LogDetailedStats()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("    AUDIO SYSTEM DETAILED STATS");
            Debug.Log("═══════════════════════════════════════");
            
            if (SoundSystemCore.Instance != null)
            {
                Debug.Log($"Active Sounds: {SoundSystemCore.Instance.GetActiveSoundCount()}");
                Debug.Log($"Available Sources: {SoundSystemCore.Instance.GetAvailableSourceCount()}");
            }
            
            if (SkullChatterManager.Instance != null)
            {
                Debug.Log($"Skull Chatter: {SkullChatterManager.Instance.GetDebugInfo()}");
            }
            
            Debug.Log("═══════════════════════════════════════");
        }
    }
}
