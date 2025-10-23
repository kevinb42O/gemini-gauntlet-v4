using UnityEngine;

namespace GeminiGauntlet.Audio
{
    /// <summary>
    /// Diagnostic tool to help identify 3D sound issues
    /// </summary>
    public class SoundDiagnostic : MonoBehaviour
    {
        [Header("=== SOUND DIAGNOSTIC TOOL ===")]
        [SerializeField] private bool enableDebugLogs = true;
        
        [ContextMenu("Test Gem Hit 3D Sound")]
        public void TestGemHit3D()
        {
            Vector3 testPosition = transform.position + Vector3.right * 5f; // 5 units to the right
            Debug.Log($"🔊 Testing Gem Hit 3D at position: {testPosition}");
            
            if (enableDebugLogs)
            {
                DiagnoseGemHitSounds();
            }
            
            GameSounds.PlayGemHit(testPosition, 1f);
        }
        
        [ContextMenu("Test Skull Kill 3D Sound")]
        public void TestSkullKill3D()
        {
            Vector3 testPosition = transform.position + Vector3.left * 5f; // 5 units to the left
            Debug.Log($"🔊 Testing Skull Kill 3D at position: {testPosition}");
            
            if (enableDebugLogs)
            {
                DiagnoseSkullKillSounds();
            }
            
            GameSounds.PlayEnemyDeath(testPosition, 1f);
        }
        
        [ContextMenu("Diagnose All Sound Categories")]
        public void DiagnoseAllSoundCategories()
        {
            if (SoundSystemCore.Instance == null)
            {
                Debug.LogError("❌ SoundSystemCore.Instance is null!");
                return;
            }
            
            Debug.Log("🔍 === SOUND SYSTEM DIAGNOSTIC ===");
            
            // Check if SoundEventsManager is working
            if (SoundEventsManager.Instance == null)
            {
                Debug.LogError("❌ SoundEventsManager.Instance is null!");
                return;
            }
            
            if (SoundEventsManager.Events == null)
            {
                Debug.LogError("❌ SoundEventsManager.Events is null!");
                return;
            }
            
            Debug.Log("✅ Sound system components are initialized");
            
            // Diagnose specific sound arrays
            DiagnoseGemHitSounds();
            DiagnoseSkullKillSounds();
        }
        
        private void DiagnoseGemHitSounds()
        {
            var events = SoundEventsManager.Events;
            if (events?.gemHit == null || events.gemHit.Length == 0)
            {
                Debug.LogError("❌ gemHit sound array is null or empty!");
                return;
            }
            
            Debug.Log($"🔍 Gem Hit Sounds ({events.gemHit.Length} sounds):");
            for (int i = 0; i < events.gemHit.Length; i++)
            {
                var sound = events.gemHit[i];
                if (sound?.clip == null)
                {
                    Debug.LogWarning($"   [{i}] ❌ No audio clip assigned");
                    continue;
                }
                
                Debug.Log($"   [{i}] {sound.clip.name} - Category: {sound.category}, Volume: {sound.volume}, 3D Override: {sound.use3DOverride}");
                
                if (sound.use3DOverride)
                {
                    Debug.Log($"        3D Settings: Min={sound.minDistance3D}, Max={sound.maxDistance3D}");
                }
            }
        }
        
        private void DiagnoseSkullKillSounds()
        {
            var events = SoundEventsManager.Events;
            if (events?.skullKill == null || events.skullKill.Length == 0)
            {
                Debug.LogError("❌ skullKill sound array is null or empty!");
                return;
            }
            
            Debug.Log($"🔍 Skull Kill Sounds ({events.skullKill.Length} sounds):");
            for (int i = 0; i < events.skullKill.Length; i++)
            {
                var sound = events.skullKill[i];
                if (sound?.clip == null)
                {
                    Debug.LogWarning($"   [{i}] ❌ No audio clip assigned");
                    continue;
                }
                
                Debug.Log($"   [{i}] {sound.clip.name} - Category: {sound.category}, Volume: {sound.volume}, 3D Override: {sound.use3DOverride}");
                
                if (sound.use3DOverride)
                {
                    Debug.Log($"        3D Settings: Min={sound.minDistance3D}, Max={sound.maxDistance3D}");
                }
            }
        }
        
        [ContextMenu("Test 3D Positioning")]
        public void Test3DPositioning()
        {
            Debug.Log("🔊 Testing 3D positioning with different sounds...");
            
            // Test at different positions
            Vector3[] testPositions = {
                transform.position + Vector3.forward * 10f,  // Front
                transform.position + Vector3.back * 10f,     // Back
                transform.position + Vector3.right * 10f,    // Right
                transform.position + Vector3.left * 10f,     // Left
                transform.position + Vector3.up * 10f        // Above
            };
            
            string[] directions = { "Front", "Back", "Right", "Left", "Above" };
            
            for (int i = 0; i < testPositions.Length; i++)
            {
                Debug.Log($"🎯 Playing gem hit sound {directions[i]} at {testPositions[i]}");
                GameSounds.PlayGemHit(testPositions[i], 0.5f);
                
                // Small delay between sounds
                System.Threading.Thread.Sleep(500);
            }
        }
    }
}
