# 🎬 Alternating Jump Animations Setup Guide

## ✅ What This Does

The system will **automatically alternate** between two jump animations every time you jump!

- **Jump 1** → **Jump 2** → **Jump 1** → **Jump 2** (in sequence, not random)
- **Pure Animator logic** - no code changes needed!
- The Animator state machine handles everything automatically

---

## 🎮 How It Works

### The Magic:
Unity's Animator will **remember which jump you just did** and play the OTHER one next time!

- After **Jump** finishes → goes to **"WasJump1"** state (invisible, instant)
- After **Jump2** finishes → goes to **"WasJump2"** state (invisible, instant)
- When you jump again:
  - If in "WasJump1" state → plays Jump2
  - If in "WasJump2" state → plays Jump
- The Animator naturally alternates between them!

### No Code Required:
- Code just sets `movementState = 3` (Jump) every time
- Animator decides whether to play Jump or Jump2 based on which "memory" state it's in
- Simple, elegant, and uses Unity's built-in state machine logic

---

## 🔧 Unity Animator Setup (SUPER SIMPLE!)

### For Each Hand Animator (Left & Right, Levels 1-4):

#### Step 1: Add New Jump2 Animation State
1. Open your hand's **Animator Controller** (e.g., `RobotArmII_R_Animator`)
2. Go to **Base Layer** (Layer 0)
3. **Right-click** in empty space → **Create State** → **Empty**
4. Name it: `Jump2` (or `R_jump2` for right hand, `L_jump2` for left hand)

#### Step 2: Assign Your Second Jump Animation Clip
1. Select the new `Jump2` state
2. In the **Inspector**, find the **Motion** field
3. Drag your **second jump animation clip** into the Motion field
   - Example: `R_jump_variation2.anim` or whatever your second jump animation is called

#### Step 3: Add "Memory" States (Invisible States)
1. **Right-click** in empty space → **Create State** → **Empty**
2. Name it: `WasJump1` (this is just a memory marker, no animation needed)
3. **Right-click** again → **Create State** → **Empty**  
4. Name it: `WasJump2` (another memory marker)
5. Leave both states **empty** (no Motion assigned) - they're instant!

#### Step 4: Setup Jump Exit Transitions
1. **Jump → WasJump1** transition:
   - **Has Exit Time**: ✅ **Checked**
   - **Exit Time**: `0.95` (when jump animation finishes)
   - **Transition Duration**: `0` (instant!)
   - **No conditions**

2. **Jump2 → WasJump2** transition:
   - **Has Exit Time**: ✅ **Checked**
   - **Exit Time**: `0.95` (when jump2 animation finishes)
   - **Transition Duration**: `0` (instant!)
   - **No conditions**

#### Step 5: Setup Alternating Logic
1. **WasJump1 → Jump2** transition:
   - **Conditions**: `movementState` **Equals** `3`
   - **Has Exit Time**: ❌ **Unchecked**
   - **Transition Duration**: `0.1`
   - (If in WasJump1 and player jumps, play Jump2!)

2. **WasJump2 → Jump** transition:
   - **Conditions**: `movementState` **Equals** `3`
   - **Has Exit Time**: ❌ **Unchecked**
   - **Transition Duration**: `0.1`
   - (If in WasJump2 and player jumps, play Jump!)

#### Step 6: Setup Initial State
1. **Right-click** on `WasJump2` state → **Set as Layer Default State**
   - This makes the first jump play Jump (since WasJump2 → Jump)

---

## 🎯 Quick Setup Checklist

For **EACH** of your 8 hand animators:

- [ ] **RobotArmII_R (Level 1)** - Add Jump2 state with second animation
- [ ] **RobotArmII_R (Level 2)** - Add Jump2 state with second animation
- [ ] **RobotArmII_R (Level 3)** - Add Jump2 state with second animation
- [ ] **RobotArmII_R (Level 4)** - Add Jump2 state with second animation
- [ ] **RobotArmII_L (Level 1)** - Add Jump2 state with second animation
- [ ] **RobotArmII_L (Level 2)** - Add Jump2 state with second animation
- [ ] **RobotArmII_L (Level 3)** - Add Jump2 state with second animation
- [ ] **RobotArmII_L (Level 4)** - Add Jump2 state with second animation

---

## 🎨 Animation Recommendations

### Good Jump Animation Pairs:
- **Jump1**: Standard two-handed jump
- **Jump2**: One-handed jump or different arm pose

### Tips:
- Keep both animations **similar duration** for consistency
- Make sure **exit times match** so landing feels smooth
- Test both animations to ensure they look good in sequence

---

## 🎨 Visual Diagram

```
START: [WasJump2] (default state)
          ↓ (player presses jump, movementState = 3)
       [Jump] ← plays FULL Jump animation
          ↓ (exit time, instant)
      [WasJump1] (memory state, instant)
          ↓ (player presses jump again, movementState = 3)
       [Jump2] ← plays FULL Jump2 animation
          ↓ (exit time, instant)
      [WasJump2] (memory state, instant)
          ↓ (player presses jump again, movementState = 3)
       [Jump] ← plays FULL Jump animation again
          ↓ (repeats forever...)
```

**The cycle:**
1. **First jump**: WasJump2 → Jump (plays ONLY Jump animation)
2. Jump finishes → instantly goes to WasJump1 (invisible)
3. **Second jump**: WasJump1 → Jump2 (plays ONLY Jump2 animation)
4. Jump2 finishes → instantly goes to WasJump2 (invisible)
5. **Third jump**: WasJump2 → Jump (plays ONLY Jump animation)
6. Repeats forever!

---

## 🧪 Testing

1. **Play the game**
2. **Jump once** - Should play Jump1 animation
3. **Jump again** - Should play Jump2 animation
4. **Jump again** - Should play Jump1 animation
5. **Repeat** - Should keep alternating!

---

## 🔥 Benefits

✅ **Zero code changes** - Pure Animator logic!  
✅ **Super simple** - Just add one state and a few transitions  
✅ **Automatic alternation** - Animator handles everything  
✅ **No random** - Predictable sequence (Jump1 → Jump2 → Jump1...)  
✅ **Elegant solution** - Uses Unity's built-in state machine  

---

## 🚨 Troubleshooting

### "Only Jump1 plays, never Jump2"
- Check that **Jump → Jump2** exit transition exists (with Exit Time checked)
- Verify **Any State → Jump2** transition exists with `movementState = 3` condition
- Make sure Jump2 state has an animation clip assigned

### "Jump2 plays but never goes back to Jump1"
- Check that **Jump2 → Jump** exit transition exists (with Exit Time checked)
- Verify the exit time is set correctly (around 0.9)

### "Animation gets stuck or doesn't transition"
- Make sure **both** Any State transitions have `movementState = 3` condition
- Verify exit times are set correctly (0.9 works well for most jump animations)
- Check that transition durations are reasonable (0.1 is a good default)

---

## 🎉 That's It!

You now have **alternating jump animations** with minimal setup! Just add the second animation to your Animators and you're done! 🚀
