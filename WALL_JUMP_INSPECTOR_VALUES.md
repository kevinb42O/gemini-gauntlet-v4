# 🎮 WALL JUMP - INSPECTOR VALUES REFERENCE

## 📋 COPY-PASTE READY VALUES

If you need to manually set these in the Unity Inspector, here are the exact values:

---

## 🧗 WALL JUMP SYSTEM

```
Enable Wall Jump:                    ✓ TRUE

Wall Jump Up Force:                  1650
Wall Jump Out Force:                 1800
Wall Jump Forward Boost:             1200
Wall Jump Fall Speed Bonus:          1.2
Wall Jump Input Influence:           1.8
Wall Jump Input Boost Multiplier:    2.2
Wall Jump Input Boost Threshold:     0.15
Wall Jump Momentum Preservation:     1.0
Wall Detection Distance:             400
Wall Jump Cooldown:                  0.12
Wall Jump Grace Period:              0.08
Max Consecutive Wall Jumps:          99
Min Fall Speed For Wall Jump:        0.01
Wall Jump Air Control Lockout Time:  0
Show Wall Jump Debug:                ☐ FALSE (or TRUE for testing)
```

---

## 📊 SIDE-BY-SIDE COMPARISON

```
PARAMETER                          OLD      NEW      CHANGE
─────────────────────────────────────────────────────────────
Wall Jump Up Force                 1200  →  1650    +37.5%
Wall Jump Out Force                1350  →  1800    +33.0%
Wall Jump Forward Boost            650   →  1200    +85.0%  🔥
Wall Jump Fall Speed Bonus         0.8   →  1.2     +50.0%
Wall Jump Input Influence          1.25  →  1.8     +44.0%
Wall Jump Input Boost Multiplier   1.25  →  2.2     +76.0%  🔥
Wall Jump Input Boost Threshold    0.25  →  0.15    -40.0%
Wall Jump Momentum Preservation    1.0   →  1.0     (same)
Wall Detection Distance            350   →  400     +14.0%
Wall Jump Cooldown                 0.5   →  0.12    -76.0%  🔥
Wall Jump Grace Period             0.15  →  0.08    -47.0%
Max Consecutive Wall Jumps         99    →  99      (same)
Min Fall Speed For Wall Jump       0.01  →  0.01    (same)
Wall Jump Air Control Lockout      0     →  0       (same)
```

🔥 = **CRITICAL CHANGES** (these make the biggest difference)

---

## 🎯 QUICK SETUP CHECKLIST

### **Step 1: Locate MovementConfig**
- Path: `Assets/prefabs_made/MOVEMENT_CONFIG/MovementConfig.asset`
- Or: Create new via `Assets > Create > Game > Movement Configuration`

### **Step 2: Open Inspector**
- Select the MovementConfig asset
- Scroll to "Wall Jump System" section

### **Step 3: Update Values**
- Copy values from the table above
- Paste into corresponding fields
- Save the asset (Ctrl+S)

### **Step 4: Test**
- Enter Play Mode
- Find a wall
- Press Space while falling
- Hold movement direction
- Experience the difference!

---

## 🔧 ALTERNATIVE: MANUAL INSPECTOR SETUP

If you prefer to set values directly in the Inspector:

### **WALL JUMP SYSTEM Section:**

1. **Wall Jump Up Force:** `1650`
   - Tooltip: "Upward force when wall jumping - OPTIMIZED for big, satisfying jumps"

2. **Wall Jump Out Force:** `1800`
   - Tooltip: "Outward force from wall - OPTIMIZED for aggressive angle"

3. **Wall Jump Forward Boost:** `1200`
   - Tooltip: "Forward boost in movement direction - MOMENTUM CASCADE SYSTEM"

4. **Wall Jump Fall Speed Bonus:** `1.2`
   - Tooltip: "Fall speed bonus - Converts fall energy into jump power (realistic physics)"

5. **Wall Jump Input Influence:** `1.8`
   - Tooltip: "Input influence on wall jump direction - SKILL EXPRESSION"

6. **Wall Jump Input Boost Multiplier:** `2.2`
   - Tooltip: "Input boost multiplier - EXPONENTIAL REWARD for perfect timing"

7. **Wall Jump Input Boost Threshold:** `0.15`
   - Tooltip: "Input threshold for boost - Lower = more forgiving"

8. **Wall Jump Momentum Preservation:** `1.0`
   - Tooltip: "Momentum preservation from previous velocity - FULL PRESERVATION for speed building"

9. **Wall Detection Distance:** `400`
   - Tooltip: "Wall detection distance - Generous for flow state"

10. **Wall Jump Cooldown:** `0.12`
    - Tooltip: "Cooldown between wall jumps - ULTRA RESPONSIVE for infinite chains"

11. **Wall Jump Grace Period:** `0.08`
    - Tooltip: "Grace period after leaving wall - Prevents immediate re-detection"

---

## 📝 VALIDATION CHECKLIST

After setting values, verify:

- [ ] Wall Jump Up Force = 1650 (not 1200)
- [ ] Wall Jump Out Force = 1800 (not 1350)
- [ ] Wall Jump Forward Boost = 1200 (not 650) ⭐ CRITICAL
- [ ] Wall Jump Input Boost Multiplier = 2.2 (not 1.25) ⭐ CRITICAL
- [ ] Wall Jump Cooldown = 0.12 (not 0.5) ⭐ CRITICAL
- [ ] Wall Jump Momentum Preservation = 1.0 (full preservation)
- [ ] All other values match the table above

---

## 🎮 TESTING PROCEDURE

### **Test 1: Single Wall Jump**
1. Sprint toward wall
2. Jump and hit wall while falling
3. Press Space
4. **Expected:** BIG vertical pop, strong push away from wall
5. **Feel:** "WHOA! POWERFUL!"

### **Test 2: Input Reward**
1. Approach wall at speed
2. Wall jump while holding W (forward)
3. **Expected:** MUCH faster than without input
4. **Feel:** "I'm FLYING forward!"

### **Test 3: Chain Sequence**
1. Find area with multiple walls
2. Chain 5+ wall jumps rapidly
3. Hold movement direction throughout
4. **Expected:** Exponential speed building
5. **Feel:** "I AM SPEED INCARNATE!"

### **Test 4: Cooldown**
1. Wall jump
2. Immediately try to wall jump again
3. **Expected:** 0.12s delay (almost instant)
4. **Feel:** "So responsive! No flow break!"

---

## 🔍 TROUBLESHOOTING

### **Problem: Wall jumps feel weak**
- **Check:** Wall Jump Up Force = 1650 (not 1200)
- **Check:** Wall Jump Out Force = 1800 (not 1350)

### **Problem: Not building speed**
- **Check:** Wall Jump Forward Boost = 1200 (not 650)
- **Check:** Wall Jump Input Boost Multiplier = 2.2 (not 1.25)
- **Check:** Are you holding movement direction during jump?

### **Problem: Cooldown feels slow**
- **Check:** Wall Jump Cooldown = 0.12 (not 0.5)
- **Check:** Wall Jump Grace Period = 0.08 (not 0.15)

### **Problem: Can't chain jumps**
- **Check:** Max Consecutive Wall Jumps = 99
- **Check:** Min Fall Speed For Wall Jump = 0.01 (very low)
- **Check:** Wall Jump Air Control Lockout = 0 (disabled)

### **Problem: Hard to trigger input boost**
- **Check:** Wall Jump Input Boost Threshold = 0.15 (not 0.25)
- **Try:** Hold W/A/S/D during wall jump

---

## 💾 BACKUP YOUR OLD VALUES

Before changing, write down your current values here:

```
OLD VALUES (for rollback if needed):
─────────────────────────────────────
Wall Jump Up Force:              _______
Wall Jump Out Force:             _______
Wall Jump Forward Boost:         _______
Wall Jump Fall Speed Bonus:      _______
Wall Jump Input Influence:       _______
Wall Jump Input Boost Multi:     _______
Wall Jump Input Boost Threshold: _______
Wall Jump Cooldown:              _______
Wall Jump Grace Period:          _______
```

---

## 🎯 RECOMMENDED WORKFLOW

1. **Backup** current values (write them down)
2. **Update** to new values (copy from table)
3. **Test** in Play Mode (try all 4 tests)
4. **Adjust** if needed (use tuning guide)
5. **Enjoy** your legendary wall jump system!

---

## 📞 QUICK REFERENCE

**Most Important Values:**
- Forward Boost: **1200** (momentum cascade)
- Input Multiplier: **2.2** (skill reward)
- Cooldown: **0.12** (flow state)

**Get these 3 right and you're 90% there!**

---

*Inspector-ready values for copy-paste convenience.* 📋
