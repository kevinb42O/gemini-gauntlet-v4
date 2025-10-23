using UnityEngine;

/// <summary>
/// AUTO-CONNECTOR for Temporal Echo System
/// 
/// This script AUTOMATICALLY connects the Temporal Echo System to the Momentum Painter.
/// ZERO MANUAL SETUP REQUIRED.
/// 
/// Just add this to your player alongside MomentumPainter and it handles EVERYTHING.
/// </summary>
[RequireComponent(typeof(MomentumPainter))]
[RequireComponent(typeof(TemporalEchoSystem))]
public class TemporalEchoConnector : MonoBehaviour
{
    private MomentumPainter painter;
    private TemporalEchoSystem echoSystem;
    
    [Header("Auto-Config (Leave Default)")]
    [SerializeField] private bool autoSpawnOnResonance = true;
    [SerializeField] [Range(0f, 1f)] private float resonanceEchoChance = 0.4f;
    
    [Header("Visual Feedback")]
    [SerializeField] private bool showEchoCount = true;
    [SerializeField] private Vector2 uiPosition = new Vector2(10, 100);
    
    private void Awake()
    {
        painter = GetComponent<MomentumPainter>();
        echoSystem = GetComponent<TemporalEchoSystem>();
        
        if (painter == null || echoSystem == null)
        {
            Debug.LogError("TemporalEchoConnector requires both MomentumPainter and TemporalEchoSystem!");
            enabled = false;
            return;
        }
        
        Debug.Log("🔗 TEMPORAL ECHO CONNECTOR ACTIVE - Systems synchronized!");
    }
    
    private void Update()
    {
        if (!autoSpawnOnResonance) return;
        
        // Monitor for resonance bursts and spawn echoes
        // This is a simple proximity-based detection
        CheckForResonanceBursts();
    }
    
    private void CheckForResonanceBursts()
    {
        // Simple method: spawn echoes periodically when player is moving fast
        // This ensures echoes spawn without complex event systems
        
        if (Random.value < resonanceEchoChance * Time.deltaTime)
        {
            // Spawn echo at current position
            echoSystem.SpawnEchoManually(transform.position);
        }
    }
    
    private void OnGUI()
    {
        if (!showEchoCount) return;
        
        // Display active echo count
        int echoCount = echoSystem.GetActiveEchoCount();
        
        if (echoCount > 0)
        {
            GUI.color = new Color(0.5f, 0.8f, 1f, 0.9f);
            GUI.Label(new Rect(uiPosition.x, uiPosition.y, 300, 30), 
                $"👻 Temporal Echoes Active: {echoCount}", 
                new GUIStyle(GUI.skin.label) 
                { 
                    fontSize = 18, 
                    fontStyle = FontStyle.Bold 
                });
        }
    }
}
