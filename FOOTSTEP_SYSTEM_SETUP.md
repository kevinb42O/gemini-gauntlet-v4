# Footstep Sound System Setup Guide

## Overview
Professional footstep system with **5 individual footstep sounds** that play sequentially. Automatically adjusts timing based on walk/sprint state and energy availability.

## What Was Created
- **PlayerFootstepController.cs** - New footstep system with 5 individual sound slots

## Features
✅ **5 Individual Footstep Sounds** - Assign each sound separately  
✅ **Sequential Playback** - Cycles through all 5 sounds in order  
✅ **Walk/Sprint Timing** - Different delays for walking vs sprinting  
✅ **Energy Integration** - Respects energy system (no sprint sounds when out of energy)  
✅ **Pitch Variation** - Random pitch for natural sound  
✅ **Grounded Detection** - Only plays when player is on ground and moving  

## Setup Instructions

### Step 1: Add Component to Player
1. Select your **Player** GameObject in the hierarchy
2. Click **Add Component**
3. Search for "**PlayerFootstepController**"
4. Add it

### Step 2: Footstep Sounds (Assign in SoundEvents)
The system automatically loads footsteps from your **SoundEvents** asset.

**Setup in SoundEvents:**
1. Open your **SoundEvents** asset (in Project window)
2. Find **"PLAYER: Movement"** section
3. Assign your sounds:
   - **footstepSounds** array: 5 walk footstep sounds
   - **sprintFootstepSounds** array: 5 sprint footstep sounds (separate!)

**Why separate sounds?**
- Walk sounds can be heavier/slower
- Sprint sounds can be lighter/faster
- More realistic audio feedback
- If no sprint sounds assigned, walk sounds are used for both

### Step 3: Configure Timing (Optional)
Adjust these settings to your preference:

#### Footstep Timing
- **Walk Step Delay**: `0.5` seconds (default) - Time between footsteps when walking
- **Sprint Step Delay**: `0.3` seconds (default) - Time between footsteps when sprinting (faster!)

#### Audio Settings
- **Footstep Volume**: `0.7` (default) - Overall volume of footsteps
- **Min Speed For Footsteps**: `1.0` (default) - Minimum movement speed to trigger footsteps
- **Randomize Footsteps**: `ON` (default) - Plays footsteps in random order

### Step 3: Test It!
1. Enter Play Mode
2. Move around (WASD keys)
3. You should hear footsteps playing randomly from your SoundEvents
4. Hold **Shift** to sprint - footsteps play faster!
5. Run out of energy - footsteps return to walk speed

## How It Works

### Random Sound Playback (Default)
The system randomly picks from your footstep sounds:
```
Step 1 → Random sound (e.g., Sound 3)
Step 2 → Random sound (e.g., Sound 1)
Step 3 → Random sound (e.g., Sound 5)
Step 4 → Random sound (e.g., Sound 2)
...completely random each time
```

**No pitch variation** - sounds play exactly as recorded

### Walk vs Sprint Timing
- **Walking**: Plays footstep every `0.5 seconds` (default)
- **Sprinting**: Plays footstep every `0.3 seconds` (default) - **40% faster!**
- **Out of Energy**: Automatically returns to walk timing when energy depletes

### Energy Integration
The system checks:
1. Is sprint key held? (Shift)
2. Does player have energy? (via PlayerEnergySystem)
3. Is player moving fast enough? (speed > 8)

If all conditions are met → **Sprint footsteps** (faster timing)  
Otherwise → **Walk footsteps** (normal timing)

### Grounded Detection
Footsteps **only play** when:
- Player is grounded (touching the ground)
- Player is moving (speed > 1.0)

No footsteps in the air or when standing still!

## Customization Options

### Make Footsteps Faster/Slower

#### Faster Footsteps
- **Walk Step Delay**: `0.4` (from 0.5)
- **Sprint Step Delay**: `0.25` (from 0.3)

#### Slower Footsteps
- **Walk Step Delay**: `0.6` (from 0.5)
- **Sprint Step Delay**: `0.4` (from 0.3)

### Adjust Volume
- **Louder**: Increase "Footstep Volume" to `1.0`
- **Quieter**: Decrease "Footstep Volume" to `0.5`

### Sequential vs Random
- **Random** (default): Check "Randomize Footsteps"
- **Sequential**: Uncheck "Randomize Footsteps" for 1→2→3→4→5 order

## Troubleshooting

### No Footstep Sounds Playing

**Check 1**: Are footstep sounds assigned in SoundEvents?
- Open your **SoundEvents** asset in Project window
- Find **"PLAYER: Movement"** section
- Check **footstepSounds** array has sounds assigned

**Check 2**: Is the component enabled?
- Make sure the checkbox next to "PlayerFootstepController" is checked

**Check 3**: Is player grounded and moving?
- Footsteps only play when on ground and moving
- Try walking around on flat ground

**Check 4**: Is volume too low?
- Check "Footstep Volume" setting (should be 0.5-1.0)
- Check Unity's Audio Mixer volume

### Footsteps Playing Too Fast/Slow

**Too Fast**:
- Increase "Walk Step Delay" and "Sprint Step Delay"

**Too Slow**:
- Decrease "Walk Step Delay" and "Sprint Step Delay"

### Sprint Footsteps Not Working

**Check 1**: Energy system
- Make sure PlayerEnergySystem is on the Player
- Verify you have energy (check energy bar)

**Check 2**: Sprint key
- Default sprint key is Shift
- Check Controls.cs for your sprint key binding

**Check 3**: Movement speed
- Sprint footsteps only trigger when speed > 8
- Make sure you're actually sprinting (moving fast)

### Same Sound Playing Repeatedly

**Issue**: Only one footstep sound in SoundEvents
- Open your **SoundEvents** asset
- Add more sounds to the **footstepSounds** array
- System will randomly pick from all assigned sounds

## Debug Mode

Enable debug logs to see what's happening:
1. Select Player GameObject
2. Find PlayerFootstepController component
3. Check "**Enable Debug Logs**"
4. Enter Play Mode
5. Watch Console for footstep debug messages

Debug messages show:
- When footsteps play
- Current footstep index (1-5)
- Sprint state
- Pitch variation

## What You Should Hear Now

### Walking
- Footsteps play every **0.5 seconds**
- **Random** sound from your SoundEvents each time
- No pitch variation - sounds exactly as recorded

### Sprinting
- Footsteps play every **0.3 seconds** (faster!)
- Same random selection, just faster timing
- No pitch changes

### Energy Depletes During Sprint
- Footsteps **automatically slow down** to walk timing
- Seamless transition - no interruption
- Returns to sprint timing when energy regenerates

## Perfect!
Your footstep system now automatically uses the sounds you already assigned in SoundEvents, plays them randomly with no pitch variation, and respects the energy system!
