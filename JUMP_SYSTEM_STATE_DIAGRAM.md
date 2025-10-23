# 🎯 JUMP SYSTEM STATE TRANSITION DIAGRAM

## 🔴 CURRENT SYSTEM (BROKEN LOGIC)

```
┌─────────────────────────────────────────────────────────────────┐
│                         GROUNDED STATE                          │
│                                                                 │
│  Resources: airJumpRemaining = maxAirJumps (0 or 1)           │
│            consecutiveWallJumps = 0                            │
│            lastWallJumpedFrom = null                           │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ [JUMP INPUT]
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                        AIRBORNE STATE                           │
│                     (After Ground Jump)                         │
│                                                                 │
│  Resources: airJumpRemaining = 0 (if maxAirJumps = 0)         │
│            airJumpRemaining = 1 (if maxAirJumps = 1)           │
│            consecutiveWallJumps = 0                            │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ [Multiple possible transitions]
                              │
        ┌─────────────────────┼─────────────────────┐
        │                     │                     │
        ↓                     ↓                     ↓
   [COYOTE JUMP]        [WALL JUMP]          [DOUBLE JUMP]
   (0.15s window)       (Near wall)          (If charges > 0)
        │                     │                     │
        │                     │                     │
        ↓                     ↓                     ↓
┌──────────────┐    ┌──────────────────┐    ┌──────────────┐
│ Same as      │    │ ⚠️ PROBLEM:      │    │ Consumes 1   │
│ ground jump  │    │ RESETS charges!  │    │ charge       │
│              │    │                  │    │              │
│ Resources:   │    │ Resources:       │    │ Resources:   │
│ Unchanged    │    │ airJump = MAX ⚠️ │    │ airJump--    │
│              │    │ wallJumps++      │    │              │
└──────────────┘    └──────────────────┘    └──────────────┘
                              │
                              │ [EXPLOIT CHAIN]
                              ↓
                    ┌──────────────────┐
                    │ Wall Jump again  │
                    │ (Different wall) │
                    │                  │
                    │ Resources:       │
                    │ airJump = MAX ⚠️ │ ← INFINITE REFILLS!
                    │ wallJumps++      │
                    └──────────────────┘
                              │
                              ↓
                    [Can double jump AGAIN]
                              │
                              ↓
                    [Can wall jump AGAIN]
                              │
                              ↓
                    [NEVER NEED TO LAND] ⚠️
```

---

## 🟢 PROPOSED SYSTEM (LOGICAL & SKILL-BASED)

```
┌─────────────────────────────────────────────────────────────────┐
│                         GROUNDED STATE                          │
│                                                                 │
│  Resources: airJumpRemaining = maxAirJumps (0 or 1)           │
│            consecutiveWallJumps = 0                            │
│            lastWallJumpedFrom = null                           │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ [JUMP INPUT]
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                        AIRBORNE STATE                           │
│                     (After Ground Jump)                         │
│                                                                 │
│  Resources: airJumpRemaining = 0 (if maxAirJumps = 0)         │
│            airJumpRemaining = 1 (if maxAirJumps = 1)           │
│            consecutiveWallJumps = 0                            │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ [Multiple possible transitions]
                              │
        ┌─────────────────────┼─────────────────────┐
        │                     │                     │
        ↓                     ↓                     ↓
   [COYOTE JUMP]        [WALL JUMP]          [DOUBLE JUMP]
   (0.15s window)       (Near wall)          (If charges > 0)
        │                     │                     │
        │                     │                     │
        ↓                     ↓                     ↓
┌──────────────┐    ┌──────────────────┐    ┌──────────────┐
│ Same as      │    │ ✅ FIXED:        │    │ Consumes 1   │
│ ground jump  │    │ NO RESET!        │    │ charge       │
│              │    │                  │    │              │
│ Resources:   │    │ Resources:       │    │ Resources:   │
│ Unchanged    │    │ airJump = SAME ✅│    │ airJump--    │
│              │    │ wallJumps++      │    │              │
│              │    │ lastWall = THIS  │    │              │
└──────────────┘    └──────────────────┘    └──────────────┘
                              │
                              │ [STRATEGIC CHOICES]
                              │
        ┌─────────────────────┼─────────────────────┐
        │                     │                     │
        ↓                     ↓                     ↓
   [WALL JUMP]          [DOUBLE JUMP]          [LAND]
   (Different wall)     (If had charges)       (Reset all)
        │                     │                     │
        │                     │                     │
        ↓                     ↓                     ↓
┌──────────────┐    ┌──────────────────┐    ┌──────────────┐
│ Resources:   │    │ Resources:       │    │ Resources:   │
│ airJump=SAME │    │ airJump--        │    │ airJump=MAX  │
│ wallJumps++  │    │ (Now 0)          │    │ wallJumps=0  │
│ lastWall=NEW │    │                  │    │ lastWall=null│
└──────────────┘    └──────────────────┘    └──────────────┘
        │                     │                     │
        │                     │                     │
        ↓                     ↓                     ↓
   [CAN WALL JUMP]      [OUT OF JUMPS]       [FULL RESET]
   [AGAIN]              [MUST LAND]          [READY TO GO]
        │                     │
        │                     │
        ↓                     ↓
   [MUST LAND          [MUST LAND
    EVENTUALLY]         NOW]
```

---

## 📊 RESOURCE FLOW COMPARISON

### **CURRENT SYSTEM (maxAirJumps = 1):**

```
START: Ground (airJump=1, wallJump=0)
  ↓
ACTION: Jump
  ↓
STATE: Airborne (airJump=1, wallJump=0)
  ↓
ACTION: Wall Jump A
  ↓
STATE: Airborne (airJump=1 ⚠️ RESET!, wallJump=1)
  ↓
ACTION: Double Jump
  ↓
STATE: Airborne (airJump=0, wallJump=1)
  ↓
ACTION: Wall Jump B
  ↓
STATE: Airborne (airJump=1 ⚠️ RESET!, wallJump=2)
  ↓
ACTION: Double Jump
  ↓
STATE: Airborne (airJump=0, wallJump=2)
  ↓
ACTION: Wall Jump C
  ↓
STATE: Airborne (airJump=1 ⚠️ RESET!, wallJump=3)
  ↓
[INFINITE LOOP - NEVER LAND]
```

### **PROPOSED SYSTEM (maxAirJumps = 1):**

```
START: Ground (airJump=1, wallJump=0)
  ↓
ACTION: Jump
  ↓
STATE: Airborne (airJump=1, wallJump=0)
  ↓
ACTION: Wall Jump A
  ↓
STATE: Airborne (airJump=1 ✅ NO CHANGE, wallJump=1)
  ↓
ACTION: Double Jump
  ↓
STATE: Airborne (airJump=0, wallJump=1)
  ↓
ACTION: Wall Jump B
  ↓
STATE: Airborne (airJump=0 ✅ NO CHANGE, wallJump=2)
  ↓
[OUT OF DOUBLE JUMPS - CAN ONLY WALL JUMP]
  ↓
ACTION: Wall Jump C
  ↓
STATE: Airborne (airJump=0, wallJump=3)
  ↓
[MUST LAND TO RESET - SKILL-BASED RESOURCE MANAGEMENT]
```

---

## 🎮 PLAYER DECISION TREE

### **CURRENT SYSTEM:**
```
Near Wall + Have Double Jump?
  ↓
  ├─ Wall Jump → Get double jump back → Use it → Wall Jump → [REPEAT]
  │  (No decision needed - always optimal)
  │
  └─ Double Jump → Waste it (wall jump would refill anyway)
     (Never optimal)

RESULT: No meaningful choices. Wall jump is always correct.
```

### **PROPOSED SYSTEM:**
```
Near Wall + Have Double Jump?
  ↓
  ├─ Wall Jump → Keep double jump → Save for later
  │  (Good if more walls ahead)
  │
  └─ Double Jump → Lose it → Must land soon
     (Good if no more walls ahead)

RESULT: Strategic decision based on environment!
```

---

## 🔬 EDGE CASE MATRIX

| Scenario | Current Behavior | Proposed Behavior | Impact |
|----------|-----------------|-------------------|--------|
| **Ground → Wall Jump** | airJump reset to MAX | airJump unchanged | ✅ Same (both start with MAX) |
| **Wall Jump → Wall Jump** | airJump reset to MAX | airJump unchanged | ⚠️ DIFFERENT (no refill) |
| **Wall Jump → Double Jump** | Can use (just refilled) | Can use (if had charges) | ⚠️ DIFFERENT (strategic) |
| **Double Jump → Wall Jump** | airJump reset to MAX | airJump unchanged (0) | ⚠️ DIFFERENT (no refill) |
| **Coyote → Wall Jump** | airJump reset to MAX | airJump unchanged | ✅ Same result |
| **Wall Jump → Land** | Reset all | Reset all | ✅ Same |
| **3x Wall Jump chain** | Infinite (with DJ between) | Limited by DJ charges | ⚠️ DIFFERENT (skill-based) |

---

## 💡 SKILL CEILING COMPARISON

### **CURRENT SYSTEM:**
```
Skill Floor:  ████████░░ (80%) - Easy to stay airborne forever
Skill Ceiling: ████░░░░░░ (40%) - No resource management needed
Mastery Depth: ██░░░░░░░░ (20%) - Just chain wall jumps

Player Skill Expression: LOW
Strategic Depth: MINIMAL
Exploit Potential: HIGH
```

### **PROPOSED SYSTEM:**
```
Skill Floor:  ██████░░░░ (60%) - Must learn resource management
Skill Ceiling: ████████░░ (80%) - Optimize resource usage
Mastery Depth: ████████░░ (80%) - Strategic wall jump chains

Player Skill Expression: HIGH
Strategic Depth: SIGNIFICANT
Exploit Potential: NONE
```

---

## 🎯 IMPLEMENTATION CHECKLIST

### **Step 1: Remove Resource Reset (1 line change)**
```csharp
// In PerformWallJump() - Line 2983
// REMOVE THIS LINE:
airJumpRemaining = maxAirJumps; // ← DELETE THIS

// That's it. Single line fix.
```

### **Step 2: Test All Transitions**
- [ ] Ground → Jump → Wall Jump → Can't double jump (if maxAirJumps=0) ✅
- [ ] Ground → Jump → Wall Jump → Can double jump once (if maxAirJumps=1) ✅
- [ ] Ground → Jump → Double Jump → Wall Jump → Can't double jump again ✅
- [ ] Ground → Jump → Wall Jump → Wall Jump → Wall Jump → Still works ✅
- [ ] Coyote time → Wall Jump → Behaves correctly ✅

### **Step 3: Update Debug Logging**
```csharp
if (showWallJumpDebug)
{
    Debug.Log($"[JUMP] Wall jump executed - " +
              $"Air jumps remaining: {airJumpRemaining}, " + // ADD THIS
              $"Consecutive wall jumps: {consecutiveWallJumps}");
}
```

### **Step 4: Document in Inspector**
```csharp
[Header("=== JUMPING ===")]
[Tooltip("Base jump force when on ground")]
[SerializeField] private float jumpForce = 155f;

[Tooltip("Double jump force (weaker than ground jump)")]
[SerializeField] private float doubleJumpForce = 30f;

[Tooltip("Number of air jumps allowed. 0=disabled, 1=one double jump, etc. " +
         "NOTE: Wall jumps do NOT refill this resource!")]
[SerializeField] private int maxAirJumps = 0;
```

---

## 🏆 FINAL RECOMMENDATION

**Remove the resource reset. It's a single line change that:**
- ✅ Fixes logical inconsistency
- ✅ Adds skill ceiling
- ✅ Enables strategic gameplay
- ✅ Removes exploit potential
- ✅ Makes double jump system meaningful
- ✅ Maintains current feel (since maxAirJumps=0 anyway)

**The current system works "by accident" because double jumps are disabled.** But the moment you enable them, the whole system breaks down into infinite air time.

Fix it now before you forget, even if double jumps stay disabled. Future you will thank present you.
