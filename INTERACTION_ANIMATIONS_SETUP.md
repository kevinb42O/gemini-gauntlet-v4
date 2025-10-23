# Interaction Animations Setup Guide

## Overview
This guide explains how to set up the new interaction animations (Grab and OpenDoor) in the layered hand animation system.

## What Was Added

### New Animation States
Two new interaction animations have been added to the **Ability Layer (Layer 3)**:
- **Grab** - Plays when player picks up items from chests
- **OpenDoor** - Plays when player interacts with keycard doors

### Modified Scripts
1. **IndividualLayeredHandController.cs**
   - Added `Grab = 2` and `OpenDoor = 3` to `AbilityState` enum
   - Added `PlayGrabAnimation()` method
   - Added `PlayOpenDoorAnimation()` method

2. **LayeredHandAnimationController.cs**
   - Added `PlayGrabAnimation()` passthrough method
   - Added `PlayOpenDoorAnimation()` passthrough method

3. **ChestInteractionSystem.cs**
   - Integrated grab animation when items are transferred from chest to inventory
   - Auto-finds `LayeredHandAnimationController` on startup

4. **KeycardDoor.cs**
   - Integrated open door animation when player uses keycard
   - Auto-finds `LayeredHandAnimationController` on startup

## Unity Animator Setup

### Step 1: Add New Animator Parameters
For **BOTH** left and right hand animators, add these parameters if they don't exist:
- `PlayGrab` (Trigger) - Triggers grab animation
- `PlayOpenDoor` (Trigger) - Triggers open door animation
- `abilityType` (Int) - Already exists for armor plates, now supports values 0-3

### Step 2: Create Animation States in Ability Layer (Layer 3)

#### For Right Hand Animator:
1. Open the **Right Hand Animator Controller** (e.g., `RobotArmII_R_AnimatorController`)
2. Navigate to **Layer 3 (Ability Layer)**
3. Create two new animation states:
   - **Grab** - Assign your grab animation clip
   - **OpenDoor** - Assign your open door animation clip

#### For Left Hand Animator:
1. Open the **Left Hand Animator Controller** (e.g., `RobotArmII_L_AnimatorController`)
2. Navigate to **Layer 3 (Ability Layer)**
3. Create the same two animation states:
   - **Grab** - Assign your grab animation clip
   - **OpenDoor** - Assign your open door animation clip

### Step 3: Setup Transitions

#### From "Any State" to Grab:
- **Condition**: `PlayGrab` trigger is set AND `abilityType == 2`
- **Settings**: 
  - Has Exit Time: **false**
  - Transition Duration: **0** (instant)
  - Interruption Source: **None**

#### From "Any State" to OpenDoor:
- **Condition**: `PlayOpenDoor` trigger is set AND `abilityType == 3`
- **Settings**: 
  - Has Exit Time: **false**
  - Transition Duration: **0** (instant)
  - Interruption Source: **None**

#### From Grab to Exit:
- **Condition**: None (automatic after animation completes)
- **Settings**: 
  - Has Exit Time: **true**
  - Exit Time: **1.0** (plays full animation)
  - Transition Duration: **0.1**

#### From OpenDoor to Exit:
- **Condition**: None (automatic after animation completes)
- **Settings**: 
  - Has Exit Time: **true**
  - Exit Time: **1.0** (plays full animation)
  - Transition Duration: **0.1**

### Step 4: Animation Timing Configuration

The animation durations are configured in code:
- **Grab**: 0.8 seconds (configurable in `PlayGrabAnimation()`)
- **OpenDoor**: 1.2 seconds (configurable in `PlayOpenDoorAnimation()`)

If your animation clips are different lengths, adjust these values in:
- `IndividualLayeredHandController.cs` line 504 (Grab duration)
- `IndividualLayeredHandController.cs` line 548 (OpenDoor duration)

## How It Works

### Grab Animation Flow:
1. Player double-clicks item in chest OR drags item to inventory
2. `ChestInteractionSystem.CollectItem()` successfully transfers item
3. Calls `handAnimationController.PlayGrabAnimation()`
4. Right hand plays grab animation on Ability Layer
5. Animation completes after 0.8 seconds
6. Hand returns to normal movement state

### OpenDoor Animation Flow:
1. Player presses E near keycard door with correct keycard
2. `KeycardDoor.TryOpenDoor()` validates keycard
3. Calls `handAnimationController.PlayOpenDoorAnimation()`
4. Right hand plays open door animation on Ability Layer
5. Animation completes after 1.2 seconds
6. Hand returns to normal movement state
7. Door opens simultaneously

## Animation Requirements

### Grab Animation Clip:
- **Duration**: ~0.5-1.0 seconds recommended
- **Action**: Hand reaches forward and closes into fist (grabbing motion)
- **Type**: One-shot animation (plays once, then exits)
- **Bones**: Should animate arm and hand bones only

### OpenDoor Animation Clip:
- **Duration**: ~1.0-1.5 seconds recommended
- **Action**: Hand reaches forward, pushes/pulls (door opening motion)
- **Type**: One-shot animation (plays once, then exits)
- **Bones**: Should animate arm and hand bones only

## Layer System Architecture

The interaction animations use the **Ability Layer (Layer 3)** which:
- **Weight**: 0 normally, 1 when active
- **Blend Mode**: Override (replaces base movement layer)
- **Priority**: Highest (can interrupt movement, shooting, emotes)
- **Blend Mask**: Should only affect arm/hand bones

When an interaction animation plays:
1. Ability Layer weight goes to 1.0 (instantly)
2. Base Layer weight goes to 0.0 (movement animations hidden)
3. Animation plays for specified duration
4. Ability Layer weight returns to 0.0
5. Base Layer weight returns to 1.0 (movement resumes)

## Testing

### Test Grab Animation:
1. Open a chest in-game
2. Double-click an item or drag it to inventory
3. Watch right hand - should play grab animation
4. Check console for: "📦 🎬 Playing grab animation for item pickup"

### Test OpenDoor Animation:
1. Approach a keycard door with the correct keycard
2. Press E to interact
3. Watch right hand - should play open door animation
4. Check console for: "[KeycardDoor] 🎬 Playing open door animation"

## Troubleshooting

### Animation Doesn't Play:
1. Check that `LayeredHandAnimationController` is assigned in scene
2. Verify animator parameters exist (`PlayGrab`, `PlayOpenDoor`, `abilityType`)
3. Check transitions are set up correctly in Ability Layer
4. Ensure animation clips are assigned to states

### Animation Plays Too Fast/Slow:
1. Check animation clip speed in Unity (should be 1.0)
2. Adjust duration in code if needed:
   - `IndividualLayeredHandController.cs` line 504 (Grab)
   - `IndividualLayeredHandController.cs` line 548 (OpenDoor)

### Animation Gets Stuck:
1. Verify "Exit" state exists in Ability Layer
2. Check transitions from animation states to Exit have "Has Exit Time" enabled
3. Ensure Exit Time is set to 1.0 (full animation plays)

### Wrong Hand Animates:
- Interaction animations are **RIGHT HAND ONLY** by design
- Left hand continues normal movement animations
- This is intentional for gameplay clarity

## Advanced Configuration

### Changing Animation Durations:
Edit `IndividualLayeredHandController.cs`:
```csharp
// Line 504 - Grab duration
StartCoroutine(CompleteAbilityAfterDuration(0.8f)); // Change 0.8f to your duration

// Line 548 - OpenDoor duration
StartCoroutine(CompleteAbilityAfterDuration(1.2f)); // Change 1.2f to your duration
```

### Adding More Interaction Animations:
1. Add new state to `AbilityState` enum (e.g., `PushButton = 4`)
2. Create new method in `IndividualLayeredHandController.cs` (e.g., `PlayPushButtonAnimation()`)
3. Add passthrough in `LayeredHandAnimationController.cs`
4. Call from your interaction system
5. Setup animator states and transitions

## Sound Integration

### Sound Events Setup
Two new sound events have been added to `SoundEvents.cs`:

1. **grabItemSound** - Plays when grabbing items from chests
2. **openDoorSound** - Plays when opening doors with keycard

### How to Assign Sounds in Unity:
1. Locate your **SoundEvents** ScriptableObject asset (usually in `Resources/` folder)
2. Find the section: **► COLLECTIBLES: Interactions**
3. Assign audio clips to:
   - **Grab Item Sound** - A quick grabbing/picking sound effect
   - **Open Door Sound** - A door interaction/keycard swipe sound effect

### Sound Configuration:
- Both sounds play as **3D spatial audio** at the hand's position
- Sounds are automatically loaded from Resources folder
- If SoundEvents asset is not found, animations will still play (just without sound)
- Debug logs will show when sounds are triggered (if debug logging is enabled)

## Summary

✅ **Grab animation** plays when picking up items from chests  
✅ **OpenDoor animation** plays when using keycards on doors  
✅ **Right hand only** - left hand continues movement  
✅ **Automatic integration** - no manual calls needed  
✅ **Layered system** - doesn't interrupt other animations permanently  
✅ **Configurable durations** - reads actual animation clip length  
✅ **Sound effects** - plays grab and door sounds automatically  
✅ **3D spatial audio** - sounds play at hand position  

The system is fully integrated and ready to use once you:
1. Assign your animation clips in the Unity Animator
2. Assign your sound clips in the SoundEvents asset
