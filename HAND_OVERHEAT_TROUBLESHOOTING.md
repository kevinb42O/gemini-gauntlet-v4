# 🔍 Hand Overheat Troubleshooting - Left Hand Not Working

## The Problem
**Right hand overheat works perfectly ✅**  
**Left hand overheat doesn't show particles ❌**

---

## Root Cause Analysis

The code mapping is **CORRECT**:
- `isPrimary = TRUE` = **LEFT hand** (LMB)
- `isPrimary = FALSE` = **RIGHT hand** (RMB)

If only the right hand works, the issue is in your **Unity scene setup**, not the code.

---

## 🔧 Quick Fix Steps

### Step 1: Use the Diagnostic Tool

1. **Add the diagnostic script** to your player GameObject:
   - Select your player in Hierarchy
   - Add Component → `HandOverheatDiagnostic`

2. **Run the diagnostic** (in Play mode):
   - Right-click the component → "Run Hand Overheat Diagnostic"
   - OR check the "Run Diagnostic" checkbox in Inspector
   - Check Console for detailed report

3. **Look for these issues:**
   ```
   ❌ Only ONE HandOverheatVisuals found (should be 2)
   ❌ Both hands have isPrimary = false
   ❌ Both hands have isPrimary = true
   ❌ Left hand component is disabled
   ❌ "Primary Hand Visuals: NULL" in report
   ```

---

### Step 2: Check Your Scene Setup

#### Find Your Hand GameObjects

In Hierarchy, expand your player and look for hand objects. They might be named:
- `LeftHand` / `RightHand`
- `Hand_L` / `Hand_R`
- `Primary` / `Secondary`
- Or inside an armature/rig structure

#### Verify HandOverheatVisuals Components

For **LEFT hand** GameObject:
```
✅ Has HandOverheatVisuals component
✅ Component is ENABLED (checkbox checked)
✅ GameObject is ACTIVE (not grayed out)
✅ isPrimary = TRUE ← CRITICAL!
✅ Wildfire prefabs assigned
✅ Path points assigned (at least 2)
```

For **RIGHT hand** GameObject:
```
✅ Has HandOverheatVisuals component
✅ Component is ENABLED
✅ GameObject is ACTIVE
✅ isPrimary = FALSE ← CRITICAL!
✅ Wildfire prefabs assigned
✅ Path points assigned (at least 2)
```

---

## 🎯 Most Likely Issues

### Issue #1: Both Hands Set to Same Value
**Symptom:** Only one hand works

**Fix:**
1. Select LEFT hand GameObject
2. Find `HandOverheatVisuals` component
3. Set `isPrimary` = **TRUE** ✅
4. Select RIGHT hand GameObject
5. Find `HandOverheatVisuals` component
6. Set `isPrimary` = **FALSE** ✅

---

### Issue #2: Left Hand Component Disabled
**Symptom:** Right hand works, left hand silent

**Fix:**
1. Select LEFT hand GameObject
2. Find `HandOverheatVisuals` component
3. Check the **checkbox** next to component name (should be checked ✅)

---

### Issue #3: Missing Path Points
**Symptom:** Console shows error about path points

**Fix:**
1. Select LEFT hand GameObject
2. Find `HandOverheatVisuals` component
3. Expand "Path Points" array
4. Assign at least 2 transforms:
   - Element 0: Hand bone/transform (start)
   - Element 1: Shoulder/upper arm (end)

---

### Issue #4: Component on Wrong GameObject
**Symptom:** Diagnostic shows unexpected GameObject names

**Fix:**
1. Remove `HandOverheatVisuals` from incorrect GameObjects
2. Add it to the correct hand GameObjects
3. Configure `isPrimary` correctly for each

---

## 🔍 Debug Console Output

When you fire your weapons, you should see these logs:

### LEFT Hand (LMB) - Should show:
```
[HandOverheatVisuals] LeftHand: Correctly configured as isPrimary=true (LEFT hand)
[HAND MAPPING] LeftHand: isPrimary=true, will respond to LEFT-CLICK input
PlayerOverheatManager: Registering Primary hand visuals: LeftHand
```

### RIGHT Hand (RMB) - Should show:
```
[HandOverheatVisuals] RightHand: Correctly configured as isPrimary=false (RIGHT hand)
[HAND MAPPING] RightHand: isPrimary=false, will respond to RIGHT-CLICK input
PlayerOverheatManager: Registering Secondary hand visuals: RightHand
```

---

## 🎮 Testing After Fix

1. **Enter Play Mode**
2. **Fire LEFT hand (LMB)** continuously
   - Watch heat bar fill up
   - At 70% → particles should appear on LEFT hand
   - At 100% → full overheat effect
3. **Fire RIGHT hand (RMB)** continuously
   - Same behavior on RIGHT hand
4. **Both should work independently!**

---

## 📊 Verification Checklist

Run through this checklist:

```
Scene Setup:
☐ Found LEFT hand GameObject
☐ Found RIGHT hand GameObject
☐ Both have HandOverheatVisuals component
☐ Both components are ENABLED
☐ Both GameObjects are ACTIVE

Configuration:
☐ LEFT hand: isPrimary = TRUE
☐ RIGHT hand: isPrimary = FALSE
☐ Both have wildfire prefabs assigned
☐ Both have path points assigned (min 2)

Runtime:
☐ Diagnostic shows 2 HandOverheatVisuals
☐ Diagnostic shows Primary visuals assigned
☐ Diagnostic shows Secondary visuals assigned
☐ LEFT hand particles appear when firing LMB
☐ RIGHT hand particles appear when firing RMB
```

---

## 🆘 Still Not Working?

### Enable Debug Logging

In `HandOverheatVisuals.cs` line 280, uncomment this line:
```csharp
Debug.Log($"HandOverheatVisuals ({gameObject.name}): UpdateVisuals called - Heat={normalizedHeat:F3}, Overheated={isEffectivelyOverheated}, ShowEffect={shouldShowAnyEffect}, Threshold={visibilityThreshold}, ActiveAtZero={effectActiveAtZeroHeat}", this);
```

This will spam the console but show you exactly what's happening.

### Check PlayerOverheatManager

In `PlayerOverheatManager.cs` line 375, uncomment this line:
```csharp
Debug.Log($"PlayerOverheatManager: UpdateHandVisuals({(isPrimary ? "Primary" : "Secondary")}) - Heat: {currentHeat:F1}/{maxHeat:F1} ({normalizedHeat:F3}), Overheated: {isOverheated}, Visuals: {visuals?.gameObject.name ?? "NULL"}", this);
```

This shows which hand is being updated and with what values.

---

## 💡 Quick Summary

**The mapping is correct in code:**
- Primary = LEFT hand (LMB)
- Secondary = RIGHT hand (RMB)

**Your issue is likely:**
1. LEFT hand's `isPrimary` is set to `FALSE` (should be `TRUE`)
2. LEFT hand's component is disabled
3. LEFT hand's GameObject is inactive
4. Component is on wrong GameObject

**Use the diagnostic tool to find the exact issue!**

---

*The code is solid. This is a Unity Inspector configuration issue.* 🎮
