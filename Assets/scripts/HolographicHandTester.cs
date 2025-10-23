// ============================================================================
// HOLOGRAPHIC HAND TESTER
// Quick testing script for holographic shader effects
// Press keys to test different effects in Play mode
// ============================================================================

using UnityEngine;

public class HolographicHandTester : MonoBehaviour
{
    [Header("Test Configuration")]
    [Tooltip("Auto-find all HolographicHandControllers in scene")]
    public bool autoFindControllers = true;
    
    [Tooltip("Manually assigned controllers (if not auto-finding)")]
    public HolographicHandController[] handControllers;
    
    [Header("Test Keybinds")]
    public KeyCode damageGlitchKey = KeyCode.G;
    public KeyCode powerupBoostKey = KeyCode.B;
    public KeyCode cycleLevelKey = KeyCode.L;
    public KeyCode toggleEffectsKey = KeyCode.T;
    public KeyCode rainbowModeKey = KeyCode.R;
    
    [Header("Status")]
    public bool effectsEnabled = true;
    public bool rainbowMode = false;
    
    private int currentTestLevel = 1;
    
    void Start()
    {
        if (autoFindControllers)
        {
            handControllers = FindObjectsOfType<HolographicHandController>();
            Debug.Log($"[HolographicHandTester] Found {handControllers.Length} hand controllers");
        }
        
        if (handControllers == null || handControllers.Length == 0)
        {
            Debug.LogWarning("[HolographicHandTester] No hand controllers found! Add HolographicHandController to your hands first.");
            enabled = false;
            return;
        }
        
        PrintInstructions();
    }
    
    void Update()
    {
        if (handControllers == null || handControllers.Length == 0) return;
        
        // Test damage glitch
        if (Input.GetKeyDown(damageGlitchKey))
        {
            TestDamageGlitch();
        }
        
        // Test powerup boost
        if (Input.GetKeyDown(powerupBoostKey))
        {
            TestPowerupBoost();
        }
        
        // Cycle hand levels
        if (Input.GetKeyDown(cycleLevelKey))
        {
            CycleHandLevels();
        }
        
        // Toggle effects
        if (Input.GetKeyDown(toggleEffectsKey))
        {
            ToggleEffects();
        }
        
        // Rainbow mode
        if (Input.GetKeyDown(rainbowModeKey))
        {
            ToggleRainbowMode();
        }
        
        // Rainbow mode update
        if (rainbowMode)
        {
            UpdateRainbowMode();
        }
    }
    
    private void TestDamageGlitch()
    {
        Debug.Log("[HolographicHandTester] Testing DAMAGE GLITCH effect...");
        foreach (var controller in handControllers)
        {
            if (controller != null)
            {
                controller.TriggerDamageGlitch(0.5f);
            }
        }
    }
    
    private void TestPowerupBoost()
    {
        Debug.Log("[HolographicHandTester] Testing POWERUP BOOST effect...");
        foreach (var controller in handControllers)
        {
            if (controller != null)
            {
                controller.TriggerPowerupBoost(5f);
            }
        }
    }
    
    private void CycleHandLevels()
    {
        currentTestLevel++;
        if (currentTestLevel > 4) currentTestLevel = 1;
        
        Debug.Log($"[HolographicHandTester] Cycling to LEVEL {currentTestLevel} colors...");
        foreach (var controller in handControllers)
        {
            if (controller != null)
            {
                controller.SetHandLevelColors(currentTestLevel);
            }
        }
    }
    
    private void ToggleEffects()
    {
        effectsEnabled = !effectsEnabled;
        
        Debug.Log($"[HolographicHandTester] Effects {(effectsEnabled ? "ENABLED" : "DISABLED")}");
        foreach (var controller in handControllers)
        {
            if (controller != null)
            {
                controller.scanLineSpeed = effectsEnabled ? 1.5f : 0f;
                controller.pulseSpeed = effectsEnabled ? 2.0f : 0f;
                controller.emissionIntensity = effectsEnabled ? 2.0f : 0.5f;
            }
        }
    }
    
    private void ToggleRainbowMode()
    {
        rainbowMode = !rainbowMode;
        Debug.Log($"[HolographicHandTester] Rainbow Mode {(rainbowMode ? "ENABLED" : "DISABLED")}");
        
        if (!rainbowMode)
        {
            // Restore original colors
            foreach (var controller in handControllers)
            {
                if (controller != null)
                {
                    controller.SetHandLevelColors(controller.handLevel);
                }
            }
        }
    }
    
    private void UpdateRainbowMode()
    {
        float hue = Mathf.PingPong(Time.time * 0.2f, 1f);
        Color rainbowColor = Color.HSVToRGB(hue, 0.8f, 1f);
        Color rainbowEmission = Color.HSVToRGB(hue, 0.6f, 1.5f);
        
        foreach (var controller in handControllers)
        {
            if (controller != null && controller.levelColors != null && controller.levelColors.Length > 0)
            {
                // Update all level colors to rainbow
                for (int i = 0; i < controller.levelColors.Length; i++)
                {
                    controller.levelColors[i].baseColor = rainbowColor;
                    controller.levelColors[i].emissionColor = rainbowEmission;
                }
                controller.SetHandLevelColors(controller.handLevel);
            }
        }
    }
    
    private void PrintInstructions()
    {
        Debug.Log("╔════════════════════════════════════════════════════════════╗");
        Debug.Log("║     HOLOGRAPHIC HAND SHADER - TEST CONTROLS                ║");
        Debug.Log("╠════════════════════════════════════════════════════════════╣");
        Debug.Log($"║  [{damageGlitchKey}] - Trigger Damage Glitch Effect                  ║");
        Debug.Log($"║  [{powerupBoostKey}] - Trigger Powerup Boost Effect                  ║");
        Debug.Log($"║  [{cycleLevelKey}] - Cycle Through Hand Level Colors (1-4)          ║");
        Debug.Log($"║  [{toggleEffectsKey}] - Toggle All Effects On/Off                    ║");
        Debug.Log($"║  [{rainbowModeKey}] - Toggle Rainbow Mode (Psychedelic!)             ║");
        Debug.Log("╠════════════════════════════════════════════════════════════╣");
        Debug.Log($"║  Found {handControllers.Length} hand controllers ready for testing    ║");
        Debug.Log("╚════════════════════════════════════════════════════════════╝");
    }
    
    void OnGUI()
    {
        // On-screen instructions
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 14;
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.UpperLeft;
        
        string instructions = $"HOLOGRAPHIC SHADER TEST CONTROLS:\n" +
                             $"[{damageGlitchKey}] Damage Glitch  |  [{powerupBoostKey}] Powerup Boost  |  [{cycleLevelKey}] Cycle Levels (Current: L{currentTestLevel})\n" +
                             $"[{toggleEffectsKey}] Toggle Effects ({(effectsEnabled ? "ON" : "OFF")})  |  [{rainbowModeKey}] Rainbow Mode ({(rainbowMode ? "ON" : "OFF")})";
        
        GUI.Label(new Rect(10, 10, 800, 60), instructions, style);
    }
}
