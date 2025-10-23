# Chest Auto-Close Debug Guide

## Test the auto-close feature and check console logs

### Step 1: Open a chest
1. Start the game
2. Walk up to a chest
3. Press E to open it

### Step 2: Check Console for Initial Position Log
You should see:
```
📍 Stored initial player position: (X, Y, Z) for auto-close detection
```

**If you see this:** ✅ Good! Player position is being tracked.

**If you see this instead:**
```
❌ CRITICAL: PlayerController is NULL! Auto-close will NOT work!
```
→ **Problem:** Player GameObject not found or missing CharacterController component.

---

### Step 3: Try to Walk Away
1. With chest open, use WASD to walk away
2. Watch the console

### Step 4: Check Movement Detection Logs
Every second you should see:
```
📍 Movement check: Current=(X,Y,Z), Last=(X,Y,Z), Distance=X.XX, Threshold=2.5
```

**What to look for:**
- `Distance` should be **increasing** as you walk away
- When `Distance` exceeds `2.5`, you should see:
```
🚶 Player moved X.XX units away from chest (threshold: 2.5) - auto-closing chest UI
```

---

## Common Issues & Fixes

### Issue 1: "PlayerController is NULL"

**Cause:** Player GameObject not found or missing component.

**Fix:**
1. Find your Player GameObject in the hierarchy
2. Check if it has the **"Player" tag** assigned
   - Select Player GameObject
   - Look at top of Inspector
   - Tag dropdown should say "Player"
   - If not, change it to "Player"

3. Check if Player has **CharacterController** component
   - Look in Inspector for "Character Controller" component
   - If missing, add it: Add Component → Character Controller

---

### Issue 2: "Distance stays at 0.00"

**Cause:** Player movement is still disabled.

**Possible reasons:**
1. Camera controller/shooter script are disabling movement somehow
2. Movement script is being disabled elsewhere
3. CharacterController is disabled

**Fix:**
1. Check if you can actually move (WASD should work)
2. If you can't move, check what scripts are on your Player
3. Make sure movement script is NOT in the disabled list

---

### Issue 3: "Distance increases but chest doesn't close"

**Cause:** Threshold might be too high, or CloseChestInventory() has an error.

**Fix:**
1. Check what distance you're reaching (should be in the logs)
2. If distance is less than 2.5, walk further
3. If distance exceeds 2.5 but chest doesn't close, check for errors in console

---

### Issue 4: "No movement logs appearing at all"

**Cause:** CheckPlayerMovement() might not be running.

**Fix:**
1. Check if `isChestOpen` is true (should be logged when chest opens)
2. Make sure Update() method is running
3. Check for any errors that might be stopping the script

---

## Manual Testing Commands

### Test 1: Check if Player exists
In Unity Console, type:
```csharp
GameObject.FindGameObjectWithTag("Player")
```
Should return your Player GameObject.

### Test 2: Check if CharacterController exists
```csharp
GameObject.FindGameObjectWithTag("Player").GetComponent<CharacterController>()
```
Should return the CharacterController component.

---

## Expected Behavior Timeline

```
1. Press E on chest
   └─> "📍 Stored initial player position: (10, 0, 5)"

2. Wait 1 second (chest open, not moving)
   └─> "📍 Movement check: Distance=0.00, Threshold=2.5"

3. Start walking away
   └─> "📍 Movement check: Distance=0.50, Threshold=2.5"
   └─> "📍 Movement check: Distance=1.20, Threshold=2.5"
   └─> "📍 Movement check: Distance=2.10, Threshold=2.5"

4. Walk past threshold
   └─> "📍 Movement check: Distance=2.80, Threshold=2.5"
   └─> "🚶 Player moved 2.80 units away from chest - auto-closing chest UI"
   └─> Chest UI closes!
```

---

## Quick Fix Checklist

- [ ] Player GameObject has "Player" tag
- [ ] Player has CharacterController component
- [ ] Player can move with WASD when chest is open
- [ ] Console shows initial position when chest opens
- [ ] Console shows movement checks every second
- [ ] Distance increases when walking away
- [ ] Chest closes when distance exceeds 2.5

---

## Still Not Working?

If you've checked everything and it still doesn't work:

1. **Copy the console logs** and share them
2. **Check your Player GameObject setup:**
   - What components are on it?
   - What is its tag?
   - Can you move when chest is open?

3. **Try lowering the threshold:**
   - In ChestInteractionSystem.cs, line 100
   - Change `movementThreshold = 2.5f` to `movementThreshold = 0.5f`
   - This makes it close after moving just 0.5 units (very sensitive)
   - If this works, the threshold was just too high
