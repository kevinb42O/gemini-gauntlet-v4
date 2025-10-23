# ⚔️ SWORD MODE - ANIMATOR SETUP GUIDE

## 🎬 Complete Animator Configuration

This guide shows you exactly how to set up the sword attack animation in your Unity Animator.

---

## 📋 Prerequisites

Before starting, you need:
- ✅ Right Hand Animator Controller (already exists)
- ✅ Sword attack animation clip (import or create)
- ✅ Basic understanding of Unity Animator

---

## 🎯 Quick Setup (5 Minutes)

### Step 1: Open Animator
1. Select your **Right Hand** GameObject in hierarchy
2. Find the **Animator** component
3. Double-click the **Controller** field to open Animator window

### Step 2: Add Trigger Parameter
1. In Animator window, click **Parameters** tab
2. Click **+** button
3. Select **Trigger**
4. Name it: `SwordAttackT`
5. Press Enter

✅ **Result**: You should see "SwordAttackT" in the parameters list

### Step 3: Create Sword Attack State
1. In Animator window, find the **Shooting Layer** (Layer 1)
2. Right-click in empty space
3. Select **Create State → Empty**
4. Name it: `SwordAttack`
5. Click the new state
6. In Inspector, find **Motion** field
7. Drag your sword attack animation clip into **Motion**

✅ **Result**: State should show your animation name

### Step 4: Create Transition
1. Right-click **Any State** node
2. Select **Make Transition**
3. Click the **SwordAttack** state to connect
4. Click the transition arrow (white line)
5. In Inspector:
   - **Has Exit Time**: ❌ UNCHECK THIS
   - **Transition Duration**: 0.1
   - Under **Conditions**:
     - Click **+** button
     - Select **SwordAttackT** from dropdown

✅ **Result**: Transition has condition "SwordAttackT"

### Step 5: Setup Exit Transition
1. Right-click **SwordAttack** state
2. Select **Make Transition**
3. Click **Any State** (or Idle/default state)
4. Click the transition arrow
5. In Inspector:
   - **Has Exit Time**: ✅ CHECK THIS
   - **Exit Time**: 0.95 (exits near end of animation)
   - **Transition Duration**: 0.1
   - **Conditions**: (leave empty)

✅ **Result**: Animation will auto-return to idle

---

## 🎨 Animation Event Setup (CRITICAL!)

This is the most important part - syncing damage to the visual hit.

### Step 6: Add Animation Event
1. Select your **sword attack animation clip** in Project window
2. Open **Animation** window (Window → Animation → Animation)
3. Scrub through timeline to find the **impact frame**
   - This is the frame where the sword visually hits the target
   - Usually around 30-50% through the animation
4. Click the **Event button** (white marker icon in timeline)
5. Or right-click timeline → **Add Animation Event**
6. Click the new event marker (should appear in timeline)
7. In Inspector:
   - **Function**: Type `DealDamage` (case-sensitive!)
   - **Object**: Leave empty (Unity auto-finds SwordDamage component)
8. Save the animation (Ctrl+S)

✅ **Result**: Small white marker appears at impact frame

### Finding the Perfect Frame:
```
Frame 0 ─────────────► Frame 30 (IMPACT!) ─────────► Frame 60 (End)
        Wind-up                  ↑                      Follow-through
                            Add event here!
```

**Tips**:
- Play animation in slow motion (Animation window playback)
- Look for peak of sword swing
- Should feel "meaty" when damage triggers
- Too early = hits before visual contact
- Too late = hits after sword passes through

---

## 🎛️ Advanced Animator Settings

### Optional Improvements:

#### 1. Blend Tree (Multiple Attacks)
```
Create Blend Tree instead of single state:
- Light Attack (fast, low damage)
- Heavy Attack (slow, high damage)
- Thrust Attack (directional)
```

#### 2. Animation Speed Variation
```
SwordAttack state → Inspector:
- Speed: 1.2 (faster attack)
- Speed: 0.8 (slower, heavier attack)
```

#### 3. Root Motion (Advanced)
```
SwordAttack animation:
- Enable Root Motion for lunge attacks
- Moves character forward during swing
```

---

## 🔍 Debugging Animator

### Test Trigger Manually:
1. Enter Play Mode
2. Select Right Hand in Hierarchy
3. In Animator window, right-click **SwordAttackT** parameter
4. Select **Set Trigger**
5. Watch animation play!

### Check Transition Flow:
```
Any State ──[SwordAttackT]──► SwordAttack ──[Exit Time]──► Default
     ↑                             │
     └─────────────────────────────┘
```

### Common Issues:

| Problem | Solution |
|---------|----------|
| Animation doesn't play | Check SwordAttackT trigger exists and is spelled correctly |
| Animation loops forever | Add exit transition with Has Exit Time checked |
| Transition is choppy | Increase Transition Duration to 0.2-0.3 |
| Wrong animation plays | Check you're on Shooting Layer (Layer 1), not Base Layer |
| Event doesn't fire | Check Function name is exactly "DealDamage" |

---

## 📊 Layer Configuration

### Shooting Layer Settings:
```
Layer Index: 1
Weight: 1.0 (controlled by code)
Blending: Override
IK Pass: False
```

**Why Override?**:
- Completely replaces Base Layer animation
- Sword swing controls entire hand
- Clean, no blending artifacts

---

## 🎬 Animation Requirements

### Your Sword Attack Animation Should Have:
- ✅ Clear wind-up phase (frames 0-30%)
- ✅ Impact moment (frame ~50%)
- ✅ Follow-through (frames 50-100%)
- ✅ Total duration: 0.5-0.7 seconds
- ✅ Loops: NO (one-shot animation)

### Tips for Animation:
1. **Snappy**: Fast swing = satisfying combat
2. **Readable**: Clear visual for when it hits
3. **Recovery**: Brief pause at end before next attack
4. **Weight**: Slight anticipation before swing

---

## 🔧 Parameter List Reference

Your Right Hand Animator should have these parameters:

### Required for Sword System:
```
SwordAttackT (Trigger)    ← NEW! For sword attacks
```

### Existing Parameters (Don't Remove):
```
ShotgunT (Trigger)        ← Shotgun attacks
IsBeamAc (Bool)           ← Beam shooting
emoteIndex (Int)          ← Which emote (1-5)
PlayEmote (Trigger)       ← Trigger emote
movementState (Int)       ← Movement animations
sprintDirection (Int)     ← Sprint direction
```

---

## 🎯 Complete Layer Structure

```
Base Layer (0)
├─ Idle
├─ Walk
├─ Sprint
│  ├─ Forward
│  ├─ StrafeLeft
│  └─ StrafeRight
├─ Jump
└─ Land

Shooting Layer (1)     ← WE WORK HERE!
├─ Shotgun
├─ Beam
└─ SwordAttack        ← NEW STATE!

Emote Layer (2)
├─ Emote1
├─ Emote2
├─ Emote3
└─ Emote4

Ability Layer (3)
├─ ArmorPlate
├─ Grab
└─ OpenDoor
```

---

## 🎨 Visual Setup Diagram

```
┌─────────────────────────────────────────────────────────┐
│                    SHOOTING LAYER                       │
├─────────────────────────────────────────────────────────┤
│                                                         │
│   ┌──────────┐                                         │
│   │Any State │                                         │
│   └────┬─────┘                                         │
│        │ [SwordAttackT]                                │
│        ▼                                                │
│   ┌──────────────┐         [Exit Time]                 │
│   │ SwordAttack  │─────────────────────►               │
│   │              │                                      │
│   │ Motion: YourAnim.anim                              │
│   │ Speed: 1.0                                         │
│   │                                                     │
│   │ Events:                                            │
│   │  └─ Frame 30: DealDamage()  ◄─ CRITICAL!         │
│   └──────────────┘                                     │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

## 🧪 Testing Checklist

### Animator Testing:
1. ✅ SwordAttackT trigger exists
2. ✅ Transition from Any State works
3. ✅ Animation plays when triggered
4. ✅ Animation returns to idle after completion
5. ✅ Animation event marker visible in timeline
6. ✅ DealDamage function spelled correctly
7. ✅ Animation length is reasonable (0.5-0.7s)

### In-Game Testing:
1. ✅ Press Backspace (sword mode active)
2. ✅ Press RMB (animation plays)
3. ✅ Damage dealt at visual impact (not instantly)
4. ✅ Cooldown works (0.5s between attacks)
5. ✅ Can spam RMB after cooldown
6. ✅ Animation plays every time

---

## 💡 Pro Tips

### Animation Quality:
- **Keep it short**: 0.5-0.7 seconds total
- **Make it punchy**: Fast acceleration, slow deceleration
- **Add anticipation**: Small wind-up before swing
- **Clear impact**: Pause slightly at hit moment

### Event Timing:
- **Too early**: Weird (hits before visible contact)
- **Perfect**: Satisfying crunch when sword connects
- **Too late**: Missed feeling (sword already past)

### Performance:
- **Optimize animation**: Remove unnecessary keyframes
- **Use animation compression**: "Optimal" in import settings
- **Keep clip short**: Less data to process

---

## 🚀 Next Steps

After animator setup:
1. ✅ Test in Play Mode
2. ✅ Adjust event timing if needed
3. ✅ Add VFX at impact (particle system)
4. ✅ Add sound effect (swoosh + impact)
5. ✅ Polish animation curve
6. ✅ Add camera shake on hit

---

## 📝 Quick Reference Card

```
PARAMETER:     SwordAttackT (Trigger)
LAYER:         Shooting Layer (1)
STATE:         SwordAttack
MOTION:        Your sword animation clip
TRANSITION:    Any State → SwordAttack [SwordAttackT]
EXIT:          SwordAttack → Any State [Exit Time 0.95]
EVENT:         Frame ~30-50% → DealDamage()
DURATION:      0.5-0.7 seconds
LOOPS:         NO
```

---

## 🆘 Emergency Fallback

**If you can't get animator working:**
The system will still work! It will:
- Use shotgun animation as placeholder
- Damage will trigger immediately (no animation sync)
- Still functional, just less polished

**To fix later:**
- Import proper sword animation
- Follow this guide step-by-step
- Add animation event for timing

---

**Setup Time**: ~5 minutes  
**Difficulty**: Beginner-friendly  
**Status**: ✅ Ready to animate!  

**Happy Animating! 🎬⚔️**
