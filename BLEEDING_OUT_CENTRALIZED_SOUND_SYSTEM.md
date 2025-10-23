# 🔊 BLEEDING OUT - CENTRALIZED SOUND SYSTEM!

## ✨ PERFECTLY INTEGRATED WITH YOUR SOUNDEVENTS!

**All sounds are centralized in your SoundEvents system - just like you wanted!** 🎵

---

## 🎯 WHAT I ADDED

### **1. New Sounds in SoundEvents.cs** 📋

Added to `PLAYER: Bleeding Out` section:

```csharp
[Header("► PLAYER: Bleeding Out")]
// Labored breathing loop (can reuse outOfBreathLoop!)
public SoundEvent bleedingOutBreathingLoop;

// Heartbeat that intensifies as timer runs out
public SoundEvent bleedingOutHeartbeatLoop;

// Sound when you start bleeding out
public SoundEvent[] bleedingOutStart;

// Optional struggle sounds when crawling
public SoundEvent[] bleedingOutCrawl;
```

---

### **2. New Methods in GameSounds.cs** 🎮

#### **StartBleedingOutSounds(Transform player)**
- Plays bleeding out start sound (one-shot)
- Starts breathing loop (reuses `outOfBreathLoop` if `bleedingOutBreathingLoop` not assigned!)
- Starts heartbeat loop
- Attached to player transform

#### **StopBleedingOutSounds()**
- Stops breathing loop
- Stops heartbeat loop
- Clean stop when revived or dead

#### **PlayBleedingOutCrawlSound(Vector3 position)**
- Optional struggle sounds when crawling
- Plays randomly from array

#### **UpdateBleedingOutHeartbeatIntensity(float progress)**
- **Automatically intensifies heartbeat as time runs out!**
- Volume: 0.5 → 1.5 (gets louder)
- Pitch: 1.0 → 1.2 (gets faster)
- Called every frame by BleedOutUIManager

---

## 🔄 HOW IT WORKS

### **When You Die:**
```
PlayerHealth.Die()
  ↓
GameSounds.PlayPlayerDeath() ← Death sound
  ↓
GameSounds.StartBleedingOutSounds(transform)
  ↓
  - Play bleedingOutStart sound (one-shot)
  - Start bleedingOutBreathingLoop (or outOfBreathLoop)
  - Start bleedingOutHeartbeatLoop
```

### **While Bleeding Out:**
```
BleedOutUIManager (every frame)
  ↓
GameSounds.UpdateBleedingOutHeartbeatIntensity(progress)
  ↓
  - Volume increases (0.5 → 1.5)
  - Pitch increases (1.0 → 1.2)
  - Faster/louder as timer runs out!
```

### **When Revived OR Dead:**
```
GameSounds.StopBleedingOutSounds()
  ↓
  - Stop breathing loop
  - Stop heartbeat loop
  - Clean exit
```

---

## 🎵 SOUND ASSIGNMENT GUIDE

### **In Unity Inspector (SoundEvents ScriptableObject):**

#### **Option 1: Reuse Existing Sound**
```
PLAYER: Bleeding Out
├─ bleedingOutBreathingLoop: [LEAVE EMPTY]
│  └─ System will automatically use outOfBreathLoop!
├─ bleedingOutHeartbeatLoop: [Assign heartbeat clip]
├─ bleedingOutStart: [Assign critical hit sound]
└─ bleedingOutCrawl: [Optional struggle sounds]
```

#### **Option 2: Use Dedicated Sounds**
```
PLAYER: Bleeding Out
├─ bleedingOutBreathingLoop: [Assign heavy breathing clip]
│  └─ Separate from energy breathing if you want
├─ bleedingOutHeartbeatLoop: [Assign heartbeat clip]
├─ bleedingOutStart: [Assign critical hit sound]
└─ bleedingOutCrawl: [Optional struggle sounds]
```

---

## 🌟 SMART REUSE SYSTEM

### **Breathing Sound:**
```csharp
// Smart fallback - uses existing sound if no dedicated one!
SoundEvent breathingSound = SafeEvents.bleedingOutBreathingLoop ?? SafeEvents.outOfBreathLoop;
```

**This means:**
- If `bleedingOutBreathingLoop` is assigned → Use it
- If empty → Automatically uses `outOfBreathLoop`
- **You can reuse your PlayerEnergy breathing sound!**

---

## 💎 FEATURES

### **1. Labored Breathing** 💨
- Loops while bleeding out
- Attached to player (follows you)
- Can reuse energy system breathing!

### **2. Dynamic Heartbeat** 💓
- Starts slow and quiet
- Gets faster and louder as timer runs out
- **Auto-intensity adjustment every frame!**
- Creates tension and urgency

### **3. Struggle Sounds** 😰
- Optional crawl sounds
- Can be triggered when moving
- Attached to position (3D audio)

### **4. Start Sound** 🩸
- Plays when bleeding out begins
- One-shot, not looping
- Dramatic critical hit feel

---

## 🔧 TECHNICAL DETAILS

### **Sound Handles (Tracking Active Sounds):**
```csharp
private static SoundHandle currentBleedingOutBreathingHandle;
private static SoundHandle currentBleedingOutHeartbeatHandle;
```

**Why This Matters:**
- Prevents duplicate breathing sounds
- Allows stopping sounds when revived
- Enables dynamic volume/pitch control
- Clean sound management

### **Intensity System:**
```csharp
// Volume increases as time decreases
float volume = Mathf.Lerp(1.5f, 0.5f, normalizedTimeRemaining);

// Pitch increases (faster heartbeat)
float pitch = Mathf.Lerp(1.2f, 1.0f, normalizedTimeRemaining);
```

**Result:**
- 100% time remaining → Quiet (0.5), slow (1.0)
- 0% time remaining → LOUD (1.5), fast (1.2)
- Smooth transition using Lerp

---

## 📊 FILES MODIFIED

### **1. SoundEvents.cs**
- Added bleeding out sounds section
- 4 new sound fields

### **2. GameSoundsHelper.cs**
- Added `StartBleedingOutSounds()`
- Added `StopBleedingOutSounds()`
- Added `PlayBleedingOutCrawlSound()`
- Added `UpdateBleedingOutHeartbeatIntensity()`
- Added sound handle tracking

### **3. PlayerHealth.cs**
- Calls `StartBleedingOutSounds()` when bleeding out starts
- Calls `StopBleedingOutSounds()` when timer expires
- Calls `StopBleedingOutSounds()` when revived

### **4. BleedOutUIManager.cs**
- Calls `UpdateBleedingOutHeartbeatIntensity()` every frame
- Passes progress value (1.0 → 0.0)

---

## ✅ SETUP CHECKLIST

### **In Unity:**

1. **Open SoundEvents ScriptableObject**
2. **Navigate to "PLAYER: Bleeding Out"**
3. **Assign sounds:**
   - `bleedingOutBreathingLoop` - Leave empty to reuse outOfBreathLoop OR assign dedicated sound
   - `bleedingOutHeartbeatLoop` - Assign heartbeat loop
   - `bleedingOutStart` - Assign critical hit sounds (array)
   - `bleedingOutCrawl` - Optional struggle sounds (array)

4. **Configure sound settings:**
   - Set volume levels
   - Set loop to TRUE for breathing/heartbeat
   - Set loop to FALSE for start sounds
   - Adjust pitch variation

5. **Test in game!**

---

## 🎮 TESTING GUIDE

### **What To Listen For:**

**When You Die:**
- ✅ Death sound plays
- ✅ Critical hit sound (bleedingOutStart)
- ✅ Breathing loop starts
- ✅ Heartbeat loop starts (quiet)

**While Bleeding Out:**
- ✅ Breathing continues
- ✅ Heartbeat gets LOUDER
- ✅ Heartbeat gets FASTER
- ✅ Tension builds!

**When Timer Expires:**
- ✅ All sounds stop
- ✅ Clean silence

**When Revived:**
- ✅ All sounds stop
- ✅ Back to normal

---

## 🔊 SOUND RECOMMENDATIONS

### **Breathing (bleedingOutBreathingLoop):**
- Heavy, labored breathing
- Loop seamlessly
- 0.8-1.5 seconds per breath
- Slight variation in pitch

### **Heartbeat (bleedingOutHeartbeatLoop):**
- Clear heartbeat rhythm
- Loop seamlessly
- 0.6-0.8 seconds per beat
- Low frequency (bass)

### **Start Sounds (bleedingOutStart):**
- Heavy impact/grunt
- Short duration (0.5-1.5s)
- Multiple variations
- Dramatic

### **Crawl Sounds (bleedingOutCrawl):**
- Struggle grunts
- Scraping/dragging
- Short bursts
- Optional/subtle

---

## 🌟 ADVANCED FEATURES (FUTURE)

### **Ready To Add:**

**1. Distance-Based Volume:**
```csharp
// Make heartbeat quieter when far from camera
float distance = Vector3.Distance(player.position, camera.position);
float distanceVolume = 1f - Mathf.Clamp01(distance / maxDistance);
```

**2. Crawl Sound Triggers:**
```csharp
// Trigger struggle sounds when moving
if (player.hasChanged)
{
    GameSounds.PlayBleedingOutCrawlSound(player.position);
}
```

**3. Breathing Sync:**
```csharp
// Sync breathing sound with camera breathing effect
float breathingPhase = Mathf.Sin(breathingTimer);
breathingHandle.SetPitch(1.0f + breathingPhase * 0.1f);
```

---

## 🎯 RESULT

**You now have a COMPLETE, CENTRALIZED bleeding out sound system!**

### **Benefits:**
- ✅ **All sounds in SoundEvents** (centralized!)
- ✅ **Reuses existing breathing sound** (if you want!)
- ✅ **Dynamic heartbeat** (intensifies over time!)
- ✅ **Clean start/stop** (no sound leaks!)
- ✅ **Easy to configure** (Unity Inspector!)
- ✅ **Extensible** (ready for more sounds!)

---

## 💡 TIPS

### **Reusing Energy Breathing:**
- Leave `bleedingOutBreathingLoop` empty
- System automatically uses `outOfBreathLoop`
- One sound, two purposes!

### **Testing Without Sounds:**
- System gracefully handles missing sounds
- No errors if sounds not assigned
- Debug logs show what's playing

### **Volume Balancing:**
- Heartbeat: Start quiet (0.5), end loud (1.5)
- Breathing: Constant volume (1.0)
- Crawl: Subtle (0.3)

---

## 🔥 FINAL WORDS

**Everything is in SoundEvents, just like you wanted!**

- Centralized configuration ✅
- Professional sound management ✅
- Dynamic intensity ✅
- Clean integration ✅

**Your bleeding out system now sounds as good as it feels!** 🔊🩸🎵
