# Platform Capture System - Troubleshooting Guide

## 🔍 Debug Logs Added

The system now logs everything:

### On Start:
```
[PlatformCaptureSystem] ========== STARTING ON [platform name] ==========
[PlatformCaptureSystem] Auto-found Central Tower: [name or NOT FOUND]
[PlatformCaptureSystem] Auto-found Platform Trigger: [name or NOT FOUND]
[PlatformCaptureSystem] Auto-found Tower Spawner: [YES or NOT FOUND]
[PlatformCaptureSystem] Player found: [player name]
[PlatformCaptureSystem] UI initialized and hidden
[PlatformCaptureSystem] ✅ MISSION ACTIVE - Capture duration: 120s
[PlatformCaptureSystem] ========== INITIALIZATION COMPLETE ==========
```

### Every 10 Seconds:
```
[PlatformCaptureSystem] ⏱️ 10s - Progress: 15.2% | Player on platform: True | In radius: True
[PlatformCaptureSystem] ⏱️ 20s - Progress: 32.8% | Player on platform: True | In radius: True
[PlatformCaptureSystem] ⏱️ 30s - Progress: 48.1% | Player on platform: True | In radius: False
```

### When Events Happen:
```
[PlatformCaptureSystem] 📺 UI SHOWN - Player entered platform
[PlatformCaptureSystem] 🎯 CAPTURE STARTED - Player in radius!
[PlatformCaptureSystem] ⚠️ CAPTURE PAUSED - Player left radius!
[PlatformCaptureSystem] 📺 UI HIDDEN - Player left platform
```

---

## ⚠️ Common Issues from Your Screenshot

### Issue 1: Mission Active is UNCHECKED ❌
**Your Inspector shows:** `Mission Active` is unchecked

**Fix:** Check the `Mission Active` checkbox in Inspector

**Why:** The system won't run if mission isn't active

---

### Issue 2: No References Assigned ❌
**Your Inspector shows:** All reference fields are empty

**Fix:** You need to assign:
1. **Central Tower** - Drag your CentralTower GameObject
2. **Platform Trigger** - Drag your PlatformTrigger
3. **Tower Spawner** - Drag your TowerSpawner
4. **Progress UI** - Drag your PlatformCaptureUI GameObject

**Why:** System can't work without these references

---

### Issue 3: No UI GameObject Created ❌
**You need to:**
1. Create a **Slider** in your worldspace canvas
2. Create empty GameObject → Add `PlatformCaptureUI.cs`
3. Assign slider to PlatformCaptureUI
4. Assign PlatformCaptureUI to PlatformCaptureSystem

---

## ✅ Step-by-Step Fix

### 1. Create Central Tower
```
1. Create Cube GameObject on platform
2. Name it "CentralTower"
3. Add CentralTower.cs component
4. Set Capture Radius to 1500
5. Assign Gem Prefab
```

### 2. Create UI
```
1. In worldspace canvas: Right-click → UI → Slider
2. Name it "CaptureSlider"
3. Create empty GameObject "PlatformCaptureUI"
4. Add PlatformCaptureUI.cs component
5. Drag slider to "Capture Slider" field
```

### 3. Setup PlatformCaptureSystem
```
1. Select platform GameObject
2. Add PlatformCaptureSystem.cs component
3. Assign all references:
   - Central Tower: Drag CentralTower GameObject
   - Platform Trigger: Drag PlatformTrigger
   - Tower Spawner: Drag TowerSpawner
   - Progress UI: Drag PlatformCaptureUI GameObject
4. CHECK "Mission Active" ✅
5. Set Capture Duration to 120
```

---

## 🎮 Testing

### What You Should See in Console:
1. **On game start:**
   - Initialization logs with all references found
   - "MISSION ACTIVE" message

2. **When you enter platform:**
   - "UI SHOWN - Player entered platform"

3. **When you enter Central Tower radius:**
   - "CAPTURE STARTED - Player in radius!"

4. **Every 10 seconds:**
   - Progress update with percentage

### What You Should See in Game:
1. **Slider appears** when you enter platform
2. **Slider fills** when you're in Central Tower radius (cyan wireframe sphere)
3. **Slider drains** when you leave radius
4. **Slider disappears** when you leave platform

---

## 🐛 If Still Not Working

### Check Console for These Warnings:

**"⚠️ PLAYER NOT FOUND!"**
- Make sure your player GameObject has tag "Player"

**"⚠️ NO UI ASSIGNED!"**
- You didn't assign PlatformCaptureUI in Inspector

**"⚠️ MISSION INACTIVE"**
- Check the "Mission Active" checkbox

**"Central Tower: NOT FOUND"**
- Create a CentralTower GameObject as child of platform

**"Platform Trigger: NOT FOUND"**
- Make sure PlatformTrigger exists as child of platform

---

## 📋 Quick Checklist

Before asking "why isn't it working":

- [ ] Created CentralTower GameObject with CentralTower.cs
- [ ] Created Slider in worldspace canvas
- [ ] Created PlatformCaptureUI GameObject with PlatformCaptureUI.cs
- [ ] Assigned slider to PlatformCaptureUI
- [ ] Added PlatformCaptureSystem.cs to platform
- [ ] Assigned ALL references in PlatformCaptureSystem Inspector
- [ ] Checked "Mission Active" ✅
- [ ] Player has "Player" tag
- [ ] Checked Console for initialization logs

---

## 🎯 Expected Console Output

```
[PlatformCaptureSystem] ========== STARTING ON CENTRAL_TOWER_REAL (Platform Trigger) ==========
[PlatformCaptureSystem] Auto-found Central Tower: CentralTower 
[PlatformCaptureSystem] Auto-found Platform Trigger: PlatformTrigger/PlatformTrigger MISSION (Platform Trigger)
[PlatformCaptureSystem] Auto-found Tower Spawner: YES
[PlatformCaptureSystem] Player found: Fil_ARMOR_V110
[PlatformCaptureSystem] UI initialized and hidden
[PlatformCaptureSystem] ✅ MISSION ACTIVE - Capture duration: 120s
[PlatformCaptureSystem] ========== INITIALIZATION COMPLETE ==========
```

If you see this, the system is working!

---

## Summary

**The logs will tell you exactly what's wrong.** Just check the Console when you start the game!
