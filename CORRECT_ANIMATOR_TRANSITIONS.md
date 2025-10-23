# ✅ **CORRECT UNITY ANIMATOR TRANSITIONS**

## 🎯 **Movement State Values:**
```
0 = Idle
1 = Walk
2 = Sprint
3 = Jump
4 = Land
5 = Slide
6 = Dive
```

---

## 🔄 **CORRECT Transition Flow (Matches Game Mechanics):**

### **From IDLE (0):**
- Idle → Walk (movementState == 1)
- Idle → Jump (movementState == 3)
- **NO direct transition to Sprint** (must walk first)
- **NO direct transition to Slide** (can only slide from sprint!)
- **NO direct transition to Dive** (can only dive from sprint!)

### **From WALK (1):**
- Walk → Idle (movementState == 0)
- Walk → Sprint (movementState == 2)
- Walk → Jump (movementState == 3)

### **From SPRINT (2):**
- Sprint → Idle (movementState == 0)
- Sprint → Walk (movementState == 1)
- Sprint → Jump (movementState == 3)
- Sprint → **Slide** (movementState == 5) ⭐
- Sprint → **Dive** (movementState == 6) ⭐

### **From JUMP (3):**
- Jump → Land (movementState == 4)
- Jump → Idle (movementState == 0) (if land is skipped)

### **From LAND (4):**
- Land → Idle (movementState == 0)
- Land → Walk (movementState == 1)
- Land → Sprint (movementState == 2)

### **From SLIDE (5):**
- Slide → Idle (movementState == 0)
- Slide → Walk (movementState == 1)
- Slide → Sprint (movementState == 2)
- Slide → Jump (movementState == 3) (jump cancels slide)

### **From DIVE (6):**
- Dive → Idle (movementState == 0)
- Dive → Walk (movementState == 1)
- Dive → Sprint (movementState == 2)

---

## 🚫 **INVALID Transitions (Don't Create These!):**

❌ Idle → Sprint (must walk first to build up speed)
❌ Idle → Slide (can only slide from sprint!)
❌ Idle → Dive (can only dive from sprint!)
❌ Walk → Slide (need sprint speed first!)
❌ Walk → Dive (need sprint speed first!)

---

## ⚙️ **Transition Settings:**

### **Fast Actions (Instant Response):**
- Any → Jump: Duration **0.05**, Has Exit Time **UNCHECKED**
- Any → Dive: Duration **0.05**, Has Exit Time **UNCHECKED**
- Sprint → Slide: Duration **0.1**, Has Exit Time **UNCHECKED**

### **Smooth Movements:**
- Idle ↔ Walk: Duration **0.2**, Has Exit Time **UNCHECKED**
- Walk ↔ Sprint: Duration **0.25**, Has Exit Time **UNCHECKED**
- Sprint → Idle: Duration **0.2**, Has Exit Time **UNCHECKED**

### **Landing:**
- Jump → Land: Duration **0.1**, Has Exit Time **CHECKED**, Exit Time **0.9**
- Land → Idle/Walk/Sprint: Duration **0.15**, Has Exit Time **UNCHECKED**

### **Recovery:**
- Slide → Idle: Duration **0.2**, Has Exit Time **UNCHECKED**
- Dive → Idle: Duration **0.2**, Has Exit Time **UNCHECKED**

---

## 🎮 **Game Mechanics Logic:**

**Why Slide/Dive Only From Sprint:**
1. Player must be **sprinting** to have enough speed
2. Pressing **Ctrl** while sprinting = **Slide**
3. Pressing **X** while sprinting = **Dive**
4. You **cannot** slide or dive from idle/walk (not enough momentum!)

**Sprint Buildup:**
1. Start from **Idle**
2. Press movement keys → **Walk**
3. Hold **Shift** → **Sprint**
4. Now you can **Slide** or **Dive**!

---

## ✅ **Quick Setup Checklist:**

For each hand animator (Left & Right):

**Base Layer:**
- [ ] Has all 7 states (Idle, Walk, Sprint, Jump, Land, Slide, Dive)
- [ ] Has movementState parameter (Int)
- [ ] Idle is default state (orange)
- [ ] NO "Any State" transitions
- [ ] All transitions use movementState conditions
- [ ] Sprint → Slide transition exists
- [ ] Sprint → Dive transition exists
- [ ] NO Idle → Slide transition
- [ ] NO Idle → Dive transition

---

**This matches your actual game mechanics perfectly!** 🎯
