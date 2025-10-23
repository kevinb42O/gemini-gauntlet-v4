# Keycard Persistence - Quick Fix Guide

## The Problem
**Keycards don't save when you close the game!**

## The Solution (2 minutes)

### Step 1: Open Unity Editor Tool
In Unity menu bar, click:
```
Tools → Keycard Setup → Move Keycards to Resources Folder
```

### Step 2: Click the Button
Click the big button that says:
```
Copy Keycards to Resources Folder
```

### Step 3: Done!
You'll see a success message. Keycards now persist! 🎉

---

## What This Does
- Copies your 5 keycard files to `Assets/Resources/Keycards/`
- Keycards can now be saved and loaded properly
- Original files stay where they are (nothing breaks)

## Test It
1. Play game
2. Pick up a keycard
3. Exit game
4. Play again
5. Keycard should still be in your inventory ✅

---

## Still Having Issues?
Check the full documentation: `KEYCARD_PERSISTENCE_FIX.md`
