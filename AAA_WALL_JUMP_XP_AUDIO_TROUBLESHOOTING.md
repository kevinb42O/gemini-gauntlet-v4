# 🔧 WALL JUMP XP AUDIO - TROUBLESHOOTING GUIDE

## 🎯 MOST LIKELY ISSUE (90% of cases)

**You haven't assigned an audio clip yet!**

1. Open Unity Editor
2. Go to: `Assets/audio/AudioMixer/SoundEvents.asset`
3. Find: **"Wall Jump XP Notification"** (under PLAYER: Movement)
4. **Drag an audio clip into this field**
5. Done! Test again.

If that doesn't work, continue below...

---

## ❌ Sound Not Playing? Follow These Steps:

### ✅ Step 1: Verify Audio Clip is Assigned
1. Open Unity Editor
2. Find: `Assets/audio/AudioMixer/SoundEvents.asset`
3. Select it in Project window
4. In Inspector, scroll to **"PLAYER: Movement"** section
5. Check if **"Wall Jump XP Notification"** has an audio clip assigned
   - ❌ **If empty**: Drag an audio clip into this field
   - ✅ **If assigned**: Continue to Step 2

### ✅ Step 2: Check WallJumpXPSimple Component
1. In Unity Hierarchy, find your **Player** object
2. Look for **WallJumpXPSimple** component
3. Check the **"AUDIO"** section:
   - ✅ **Enable Audio** should be **checked/true**
   - If unchecked, check it!

### ✅ Step 3: Enable Debug Logs
1. Still in **WallJumpXPSimple** component
2. In **"DEBUG"** section:
   - Check **"Show Debug Logs"** to **true**
3. Enter Play Mode
4. Perform a wall jump
5. Check Unity Console for these messages:
   - `[WallJumpXP] 🎯 CHAIN x1! Earned X XP`
   - `[WallJumpXP] 🎵 Playing XP notification sound for chain x1`
   - `🎵 WALL JUMP XP NOTIFICATION PLAYED - Chain x1, Pitch: 1.00`

### ✅ Step 4: Check SoundEventsManager
1. In Unity Hierarchy, look for **SoundEventsManager** object
2. If it doesn't exist:
   - Create an empty GameObject
   - Add **SoundEventsManager** component
   - Assign your **SoundEvents** asset to it
3. Make sure it's active in the scene

### ✅ Step 5: Check Audio Listener
1. Make sure your **Camera** has an **Audio Listener** component
2. Only ONE Audio Listener should exist in the scene

### ✅ Step 6: Verify Sound System is Ready
1. Enable debug logs (Step 3)
2. Look for warning: `"🔊 Wall Jump XP Notification sound not configured in SoundEvents"`
3. If you see this, the audio clip is NOT assigned (go back to Step 1)

### ✅ Step 7: Test with Console Logs
When you wall jump, you should see these logs in order:
```
[WallJumpXP] 🎯 CHAIN x1! Earned 5 XP
[WallJumpXP] 🎵 Playing XP notification sound for chain x1
🎵 WALL JUMP XP NOTIFICATION PLAYED - Chain x1, Pitch: 1.00
```

**If you DON'T see the last line**, the GameSounds method isn't being called properly.

## 🐛 Common Issues

### Issue 1: "Sound system not ready"
**Cause**: SoundEventsManager not initialized
**Fix**: 
- Add SoundEventsManager to scene
- Assign SoundEvents asset to it
- Make sure it runs on Awake()

### Issue 2: Sound plays but you can't hear it
**Cause**: Volume too low or 3D audio out of range
**Fix**:
- In SoundEvents asset, increase volume to 1.0 or higher
- Check Audio Listener is on camera
- Try changing to 2D sound temporarily (in GameSoundsHelper.cs line 263, change `Play3D` to `Play2D`)

### Issue 3: "wallJumpXPNotification is null"
**Cause**: Field not assigned in SoundEvents
**Fix**: 
- Open SoundEvents asset
- Find "Wall Jump XP Notification" field
- Assign an audio clip

### Issue 4: No debug logs appear
**Cause**: Debug logs disabled
**Fix**:
- Select WallJumpXPSimple component
- Enable "Show Debug Logs"

### Issue 5: Sound plays but pitch doesn't change
**Cause**: Base pitch not set to 1.0
**Fix**:
- In SoundEvents asset
- Find your assigned audio clip settings
- Set **Pitch** to **1.0**
- Set **Pitch Variation** to **0.0**

## 🔍 Advanced Debugging

### Check if GameSounds is being called
Add this temporary debug line to `WallJumpXPSimple.cs` line 112:
```csharp
Debug.Log($"🔊 CALLING GameSounds.PlayWallJumpXPNotification at {wallJumpPosition} with chain {currentChainLevel}");
GeminiGauntlet.Audio.GameSounds.PlayWallJumpXPNotification(wallJumpPosition, currentChainLevel, 1.0f);
```

### Check if sound clip exists
Add this to `GameSoundsHelper.cs` line 241:
```csharp
Debug.Log($"🔊 SafeEvents null? {SafeEvents == null}");
Debug.Log($"🔊 wallJumpXPNotification null? {SafeEvents?.wallJumpXPNotification == null}");
Debug.Log($"🔊 Audio clip null? {SafeEvents?.wallJumpXPNotification?.clip == null}");
```

### Force play the sound (bypass all checks)
Temporarily replace line 263 in `GameSoundsHelper.cs`:
```csharp
// Original:
SafeEvents.wallJumpXPNotification.Play3D(position, volume);

// Test version (force play):
AudioSource.PlayClipAtPoint(SafeEvents.wallJumpXPNotification.clip, position, volume);
```

## 📋 Checklist

Before asking for help, verify:
- [ ] Audio clip is assigned in SoundEvents asset
- [ ] WallJumpXPSimple has "Enable Audio" checked
- [ ] SoundEventsManager exists in scene
- [ ] Audio Listener is on camera
- [ ] Debug logs are enabled
- [ ] You see XP text when wall jumping (proves system is working)
- [ ] Console shows the debug logs listed above
- [ ] No errors in Console

## 🎯 Quick Test

1. Enable debug logs in WallJumpXPSimple
2. Enter Play Mode
3. Wall jump once
4. Check Console - you should see:
   ```
   [WallJumpXP] 🎯 CHAIN x1! Earned 5 XP
   [WallJumpXP] 🎵 Playing XP notification sound for chain x1
   🎵 WALL JUMP XP NOTIFICATION PLAYED - Chain x1, Pitch: 1.00
   ```

If you see all three lines but hear nothing:
- Volume is too low
- Audio Listener issue
- Audio clip is silent/corrupted

If you DON'T see the third line:
- SoundEvents not assigned
- Audio clip not assigned
- Sound system not initialized

## 💡 Still Not Working?

Share these details:
1. Screenshot of SoundEvents asset (showing Wall Jump XP Notification field)
2. Screenshot of WallJumpXPSimple component settings
3. Console logs when performing a wall jump
4. Any errors in Console

---

**Most Common Fix**: Just assign an audio clip to the "Wall Jump XP Notification" field in SoundEvents! 🎵
