// 🎯 PHYSICS-PERFECT ARENA - QUICK REFERENCE
// ============================================

// HOW TO BUILD IN UNITY:
// 1. Tools > Physics Perfect Arena > BUILD PERFECTION
// 2. Wait ~10 seconds
// 3. Press Play
// 4. Done.

// ZONE LOCATIONS (for teleporting/testing):
// Zone 1 (GREEN):   (0, 150, -500)       - Basic Wall Jump
// Zone 2 (CYAN):    (8000, 1350, 0)      - Drop Launch  
// Zone 3 (YELLOW):  (0, 150, 14500)      - Zigzag Climb
// Zone 4 (ORANGE):  (0, 150, -10500)     - Speed Gauntlet
// Zone 5 (PURPLE):  (15000, 150, 0)      - Spiral Tower
// Zone 6 (RED):     (0, 150, 24500)      - Canyon Flow

// PHYSICS VALUES USED:
// Character Height:     320 units
// Gravity:             -3500 u/s²
// Jump Force:           2200 u/s
// Sprint Speed:         1485 u/s
// Wall Jump Up:         1500 u/s
// Fall Speed Bonus:     100%
// Momentum Preserved:   35%

// CALCULATED DISTANCES:
// Sprint Jump:          1522 units
// Walk Jump:            1075 units
// Wall Jump Height:     321 units
// Drop (600u) Launch:   3500+ units

// ZONE PROGRESSION:
// 1. Tutorial (2 min)   → Learn wall jump
// 2. Drop (30 sec)      → "OH SHIT!" moment
// 3. Climb (3 min)      → Chain momentum
// 4. Gauntlet (5 min)   → Maintain speed
// 5. Spiral (10 min)    → Master camera
// 6. Flow (2 min)       → Victory lap

// EVERY DISTANCE IS CALCULATED.
// EVERY JUMP IS ACHIEVABLE.
// THIS IS PERFECTION.

using UnityEngine;

/// <summary>
/// Helper component to add to player for quick zone teleportation during testing
/// Add this to your player character and press number keys to teleport
/// </summary>
public class ArenaZoneTeleporter : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            transform.position = new Vector3(0, 150, -500);
            Debug.Log("🟢 Teleported to Zone 1: Basic Wall Jump");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            transform.position = new Vector3(8000, 1350, 0);
            Debug.Log("🔵 Teleported to Zone 2: Drop Launch");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            transform.position = new Vector3(0, 150, 14500);
            Debug.Log("🟡 Teleported to Zone 3: Zigzag Climb");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            transform.position = new Vector3(0, 150, -10500);
            Debug.Log("🟠 Teleported to Zone 4: Speed Gauntlet");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            transform.position = new Vector3(15000, 150, 0);
            Debug.Log("🟣 Teleported to Zone 5: Spiral Tower");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            transform.position = new Vector3(0, 150, 24500);
            Debug.Log("🔴 Teleported to Zone 6: Canyon Flow");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            transform.position = new Vector3(0, 150, -500);
            Debug.Log("🏠 Returned to start");
        }
    }
    
    void OnGUI()
    {
        GUI.Box(new Rect(10, 10, 300, 160), "ARENA ZONE TELEPORTER");
        GUI.Label(new Rect(20, 35, 280, 20), "1 - Zone 1: Basic (GREEN)");
        GUI.Label(new Rect(20, 55, 280, 20), "2 - Zone 2: Drop (CYAN)");
        GUI.Label(new Rect(20, 75, 280, 20), "3 - Zone 3: Climb (YELLOW)");
        GUI.Label(new Rect(20, 95, 280, 20), "4 - Zone 4: Gauntlet (ORANGE)");
        GUI.Label(new Rect(20, 115, 280, 20), "5 - Zone 5: Spiral (PURPLE)");
        GUI.Label(new Rect(20, 135, 280, 20), "6 - Zone 6: Flow (RED)");
        GUI.Label(new Rect(20, 155, 280, 20), "0 - Return to Start");
    }
}
