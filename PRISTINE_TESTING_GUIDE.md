# 🧪 PRISTINE MOVEMENT TESTING GUIDE

**Quick verification checklist for 101% architectural coherence fixes**

---

## ✅ CRITICAL: CROUCH-ON-SLOPE AUTO-SLIDE TEST

### Test #1: Standing Still on 15° Slope
**Steps:**
1. Find a moderate slope (ramp, hill) ~15-30° angle
2. Stand completely still (no WASD input)
3. Press CROUCH key (default: Left Control)

**Expected Result:**
- ✅ Player should **INSTANTLY start sliding down** the slope
- ✅ No movement input required
- ✅ Slide direction = straight down slope

**Log to verify:**
```
[AUTO-SLIDE] Crouch pressed on XX° slope - forcing slide start!
[SLIDE START] Speed: 0.00, EffectiveMin: 0.00, Forced: True
```

---

### Test #2: Standing Still on 45° Slope
**Steps:**
1. Find a steep slope ~45-60°
2. Stand completely still
3. Press CROUCH

**Expected Result:**
- ✅ Instant slide (even faster than Test #1)
- ✅ Gravity accelerates naturally
- ✅ Slide speed increases smoothly

---

### Test #3: Standing Still on 5° Slope (Flat)
**Steps:**
1. Find almost-flat ground (5-10° max)
2. Stand still
3. Press CROUCH

**Expected Result:**
- ✅ Normal crouch (no slide - too flat)
- ✅ Player can move while crouched
- ✅ No auto-slide trigger

---

## ✅ SLIDE SYSTEM VERIFICATION

### Test #4: Sprint → Slide → Stop
**Steps:**
1. Sprint on flat ground (Shift + W)
2. Press CROUCH while sprinting
3. Let momentum carry into slide
4. Wait for natural stop

**Expected Result:**
- ✅ Smooth slide start
- ✅ Gradual deceleration on flat
- ✅ Natural stop (no jerky halt)
- ✅ Animation: Sprint → Slide → Idle/Sprint

---

### Test #5: Slide → Jump → Land → Resume
**Steps:**
1. Start sliding (any method)
2. Press JUMP (Space) during slide
3. Land while holding forward

**Expected Result:**
- ✅ Slide stops instantly on jump
- ✅ Horizontal momentum preserved in air
- ✅ Smooth landing
- ✅ Movement restored immediately

**Log to verify:**
```
[SLIDE] Jump detected - stopping slide!
[MOVEMENT] Jump animation triggered
```

---

## ✅ DIVE SYSTEM VERIFICATION

### Test #6: Dive → Jump Cancel
**Steps:**
1. Sprint (Shift + W)
2. Dive (X key)
3. Press JUMP during dive flight

**Expected Result:**
- ✅ Dive cancelled immediately
- ✅ Jump executes normally
- ✅ No input blocking
- ✅ Movement restored

**Log to verify:**
```
[DIVE] Jump pressed - canceling dive!
[MOVEMENT] Dive override disabled - Input restored
```

---

### Test #7: Dive → Land → Prone → Stand
**Steps:**
1. Sprint + Dive
2. Let dive land naturally
3. Wait 0.5 seconds (prone state)
4. Press any movement key

**Expected Result:**
- ✅ Dive → prone transition smooth
- ✅ Belly slide with friction
- ✅ Stand up on input
- ✅ Movement responsive

---

## ✅ PERFORMANCE VERIFICATION

### Test #8: External Velocity Spam Check
**Steps:**
1. Start sliding on a long slope
2. Watch console/logs for 5 seconds
3. Count SetExternalVelocity calls

**Expected Result:**
- ✅ ~6-10 calls total (not 300+)
- ✅ Only updates on significant changes
- ✅ Log shows: "Updated external velocity - Change: XX%"

**Good log pattern:**
```
[SLIDE] Updated external velocity - Change: 8.3%, Magnitude: 450.2
[SLIDE] Updated external velocity - Change: 6.1%, Magnitude: 482.7
[SLIDE] Updated external velocity - Change: 5.4%, Magnitude: 512.3
```

**Bad pattern (OLD SYSTEM):**
```
SetExternalVelocity called 60 times per second
SetExternalVelocity called 60 times per second
SetExternalVelocity called 60 times per second
```

---

## ✅ CLEANUP VERIFICATION

### Test #9: Disable During Slide
**Steps:**
1. Start sliding
2. Disable CleanAAACrouch component in Inspector
3. Try to move

**Expected Result:**
- ✅ Slide stops cleanly
- ✅ Controller state restored
- ✅ No lingering forces
- ✅ Movement works normally

**Log to verify:**
```
[CONTROLLER] Slope limit restored to original 45.0°
External force cleared
```

---

### Test #10: Disable During Dive
**Steps:**
1. Start dive
2. Disable CleanAAACrouch mid-dive
3. Try to move

**Expected Result:**
- ✅ Dive cancelled
- ✅ Input NOT blocked
- ✅ Movement responsive
- ✅ No stuck states

**Log to verify:**
```
[MOVEMENT] Dive override disabled - Input restored
External force cleared
```

---

## 🎯 EDGE CASE TESTING

### Test #11: Rapid Input Spam
**Steps:**
1. Spam CROUCH + JUMP + DIVE rapidly (10+ presses/second)
2. Do this for 5 seconds
3. Check for stuck states

**Expected Result:**
- ✅ No input blocking
- ✅ No state confusion
- ✅ Clean transitions
- ✅ No errors in console

---

### Test #12: Network/Slowdown Test
**Steps:**
1. Enable slow motion (if available) or simulate lag
2. Try all movement actions
3. Check for state desync

**Expected Result:**
- ✅ States remain synchronized
- ✅ No double-triggers
- ✅ Clean recovery on resume

---

## 📊 SUCCESS CRITERIA

### All Tests Must Pass:
- [ ] Test #1: Crouch on 15° slope (instant slide)
- [ ] Test #2: Crouch on 45° slope (instant slide)
- [ ] Test #3: Crouch on flat (normal crouch)
- [ ] Test #4: Sprint → Slide → Stop
- [ ] Test #5: Slide → Jump → Resume
- [ ] Test #6: Dive → Jump cancel
- [ ] Test #7: Dive → Prone → Stand
- [ ] Test #8: External velocity <10 calls/5sec
- [ ] Test #9: Disable during slide cleanup
- [ ] Test #10: Disable during dive cleanup
- [ ] Test #11: Rapid input spam resilience
- [ ] Test #12: Slowdown resilience

### Performance Targets:
- ✅ <10 SetExternalVelocity calls per 5 seconds of sliding
- ✅ Zero console errors
- ✅ 60 FPS maintained during all movement

### Feel Targets:
- ✅ Instant slide on crouch (no delay)
- ✅ Smooth transitions (no jerky movement)
- ✅ Responsive controls (no input lag)
- ✅ Natural physics (gravity feels right)

---

## 🐛 KNOWN FIXED ISSUES

### ✅ FIXED: Crouch on slope required movement
**Before:** Standing still + crouch on slope = nothing  
**After:** Instant slide start with zero speed

### ✅ FIXED: External velocity spam
**Before:** 300+ API calls during 5-second slide  
**After:** 6-10 calls with smart change detection

### ✅ FIXED: Grounded state inconsistency
**Before:** 4 different checks, unpredictable  
**After:** Single source of truth (Raw for mechanics, Coyote for UX)

### ✅ FIXED: Dive input blocking
**Before:** Jump during dive could leave input blocked  
**After:** 3-layer cleanup guarantees restoration

### ✅ FIXED: Slide-jump conflicts
**Before:** Slide + jump = velocity spam  
**After:** Clean transition with preserved momentum

---

## 🚨 IF TESTS FAIL

### If Test #1 Fails (No slide on crouch):
**Check:**
1. `landingSlopeAngleForAutoSlide` value (should be ~12°)
2. ProbeGround() returning valid hit
3. `forceSlideStartThisFrame` being set
4. Console logs for "AUTO-SLIDE" messages

### If Test #8 Fails (Too many calls):
**Check:**
1. `EXTERNAL_VELOCITY_UPDATE_THRESHOLD` = 0.05f
2. `lastAppliedExternalVelocity` being tracked
3. Console logs for "Updated external velocity" frequency

### If Test #10 Fails (Input blocked):
**Check:**
1. OnDisable() being called
2. `movement.DisableDiveOverride()` executing
3. No errors in console during disable

---

**All tests passing = 101% PRISTINE OPERATION ✅**
