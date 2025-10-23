# 🎭 EMOTE ANIMATOR SETUP GUIDE

## ⚠️ PROBLEM: All Arrow Keys Playing Same Emote

If all 4 arrow keys are playing the same emote animation, the issue is in the **Unity Animator setup**, not the code.

## ✅ CODE IS WORKING CORRECTLY

The code is now setting:
- **PlayEmote** trigger
- **emoteIndex** integer parameter (1, 2, 3, or 4)

You should see these logs when you press arrow keys:
```
🎭 [EMOTE START] RobotArmII_R Emote Emote1 (Index=1) - Trigger set, Index parameter set to 1
🎭 [EMOTE START] RobotArmII_R Emote Emote2 (Index=2) - Trigger set, Index parameter set to 2
🎭 [EMOTE START] RobotArmII_R Emote Emote3 (Index=3) - Trigger set, Index parameter set to 3
🎭 [EMOTE START] RobotArmII_R Emote Emote4 (Index=4) - Trigger set, Index parameter set to 4
```

## 🔧 REQUIRED ANIMATOR SETUP

Your **RIGHT HAND** animator needs to be set up like this:

### **Animator Parameters:**
```
PlayEmote (Trigger) - triggers emote transition
emoteIndex (Int) - which emote to play (1, 2, 3, or 4)
```

### **Emote Layer (Layer 2) - Override Blending**

You have **TWO OPTIONS** for setting up emote selection:

---

## **OPTION 1: Blend Tree (RECOMMENDED)**

This is the best approach for 4 separate emotes.

### **State Machine Structure:**
```
Any State → Emote Blend Tree (Condition: PlayEmote trigger)
Emote Blend Tree → Exit (Automatic when animation completes)
```

### **Emote Blend Tree Setup:**
1. **Create a 1D Blend Tree** called "Emote Blend Tree"
2. **Set Parameter**: `emoteIndex`
3. **Add 4 motion fields**:
   - **Position 1**: Your Emote 1 animation clip
   - **Position 2**: Your Emote 2 animation clip
   - **Position 3**: Your Emote 3 animation clip
   - **Position 4**: Your Emote 4 animation clip

### **Blend Tree Thresholds:**
```
Motion 1 (Emote1): Threshold = 1
Motion 2 (Emote2): Threshold = 2
Motion 3 (Emote3): Threshold = 3
Motion 4 (Emote4): Threshold = 4
```

When `emoteIndex = 1`, it plays Emote1.
When `emoteIndex = 2`, it plays Emote2.
Etc.

---

## **OPTION 2: Separate States with Conditions**

This is more manual but gives you more control.

### **State Machine Structure:**
```
Any State → Emote1 (Conditions: PlayEmote trigger, emoteIndex == 1)
Any State → Emote2 (Conditions: PlayEmote trigger, emoteIndex == 2)
Any State → Emote3 (Conditions: PlayEmote trigger, emoteIndex == 3)
Any State → Emote4 (Conditions: PlayEmote trigger, emoteIndex == 4)

Emote1 → Exit (Automatic)
Emote2 → Exit (Automatic)
Emote3 → Exit (Automatic)
Emote4 → Exit (Automatic)
```

### **Transition Setup for Each Emote:**

**Any State → Emote1:**
- Conditions: `PlayEmote` (trigger), `emoteIndex` `Equals` `1`
- Has Exit Time: `false`
- Fixed Duration: `0.1s`
- Transition Duration: `0.1s`

**Any State → Emote2:**
- Conditions: `PlayEmote` (trigger), `emoteIndex` `Equals` `2`
- Has Exit Time: `false`
- Fixed Duration: `0.1s`
- Transition Duration: `0.1s`

**Any State → Emote3:**
- Conditions: `PlayEmote` (trigger), `emoteIndex` `Equals` `3`
- Has Exit Time: `false`
- Fixed Duration: `0.1s`
- Transition Duration: `0.1s`

**Any State → Emote4:**
- Conditions: `PlayEmote` (trigger), `emoteIndex` `Equals` `4`
- Has Exit Time: `false`
- Fixed Duration: `0.1s`
- Transition Duration: `0.1s`

---

## 🧪 **DEBUGGING STEPS**

1. **Check Console Logs** when you press arrow keys:
   - You should see different `Index=` values (1, 2, 3, 4)
   - If all show the same index, the problem is in PlayerAnimationStateManager
   - If they show different indices but play same animation, problem is in Animator

2. **Open Unity Animator Window**:
   - Select your **RIGHT hand** (e.g., RobotArmII_R)
   - Go to Animator window
   - Look at **Layer 2 (Emote Layer)**
   - Verify you have transitions set up correctly

3. **Test in Animator Window**:
   - In Play mode, watch the Animator window
   - Press different arrow keys
   - Watch which state it transitions to
   - Check if `emoteIndex` parameter changes in the Animator window

4. **Verify Animation Clips**:
   - Make sure you have 4 different animation clips assigned
   - Make sure they're not all pointing to the same clip

---

## 🎯 **COMMON MISTAKES**

❌ **All 4 blend tree positions point to same clip**
- Fix: Assign different clips to each threshold

❌ **Blend tree is set to 2D instead of 1D**
- Fix: Use 1D blend tree with single parameter

❌ **Transitions missing `emoteIndex` condition**
- Fix: Add `emoteIndex Equals X` condition to each transition

❌ **Only one "Any State → Emote" transition exists**
- Fix: Create separate transitions for each emote or use blend tree

❌ **emoteIndex parameter doesn't exist in Animator**
- Fix: Add it in the Parameters panel (type: Int)

---

## ✅ **WHAT TO CHECK RIGHT NOW**

1. Open your **RobotArmII_R** animator
2. Go to **Layer 2 (Emote Layer)**
3. Check if you have:
   - ✅ A blend tree using `emoteIndex` parameter
   - OR ✅ 4 separate emote states with conditions
4. Verify each emote state has a **different animation clip** assigned
5. Check that `PlayEmote` trigger and `emoteIndex` Int parameter exist

---

## 📋 **QUICK TEST**

Run the game and press arrow keys while watching the console:

**Expected logs:**
```
🎭 [EMOTE START] RobotArmII_R Emote Emote1 (Index=1)
🎭 [EMOTE DURATION] RobotArmII_R playing emote index 1, clip length: 2.50s

🎭 [EMOTE START] RobotArmII_R Emote Emote2 (Index=2)
🎭 [EMOTE DURATION] RobotArmII_R playing emote index 2, clip length: 3.00s
```

If you see **different index values** (1, 2, 3, 4) but the **same clip length** for all, then your animator is not using the `emoteIndex` parameter correctly.

---

## 🎉 **ONCE FIXED**

After fixing your Animator setup, you should see:
- Up Arrow → Plays Emote 1 animation
- Down Arrow → Plays Emote 2 animation
- Left Arrow → Plays Emote 3 animation
- Right Arrow → Plays Emote 4 animation

**The code is already correct - you just need to fix the Unity Animator setup!**
