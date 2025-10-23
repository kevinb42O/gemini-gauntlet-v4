# 🎯 TACTICAL DIVE SYSTEM - Call of Duty Style

## Overview
A high-octane tactical dive mechanic inspired by Call of Duty's dolphin dive. Launch yourself forward into a prone position while sprinting for aggressive movement plays and tactical repositioning.

**⚡ OPTIMIZED FOR LARGE WORLD**: All values scaled 4x for 320-unit player (vs standard 80-unit player)

## 🎮 How to Use

### Controls
- **Sprint** (Hold Shift) + **X Key** = Tactical Dive
- Must be moving at sprint speed (320+ units/sec for your large world)
- Must be grounded to initiate

### Dive Behavior
1. **Launch Phase**: Player launches forward with an upward arc
2. **Flight Phase**: Gravity pulls you down in a realistic arc
3. **Landing Phase**: Slam into prone position with camera impact
4. **Prone Phase**: Stay prone for 0.8 seconds (configurable)
5. **Recovery**: Press any movement key, jump, or crouch to stand up instantly

## ⚙️ Configuration (Inspector)

### Core Settings
- **Enable Tactical Dive**: Toggle the entire system on/off
- **Dive Key**: Key to trigger dive (default: X)
- **Dive Forward Force**: Horizontal launch speed (default: 720 - scaled for 320-unit player)
- **Dive Upward Force**: Vertical arc height (default: 240 - scaled for 320-unit player)
- **Dive Min Sprint Speed**: Minimum speed required (default: 320 - scaled for 320-unit player)

### Feel & Timing
- **Dive Prone Duration**: How long you stay prone (default: 0.8s)
- **Dive Landing Compression**: Camera impact strength (default: 400 - scaled for 320-unit player)

### Audio & VFX
- **Dive Audio Enabled**: Play sound on dive
- **Dive Particles**: Optional particle system for dive trail

## 🎨 Visual Feedback

### Camera Effects
- **Launch**: Slight upward camera movement
- **Landing**: Heavy camera shake and compression (belly flop feel)
- **Prone**: Low camera position (crouched height)

### Audio
- **Dive Sound**: Assign in SoundEvents asset (`tacticalDiveSound`)
- **Landing Sound**: Uses existing player land sound

### Particles
- **Dive Trail**: Optional particles during flight
- **Ground Slide**: Slide particles activate on landing for dust effect

## 🔧 Technical Details

### Integration
- **Full Priority Override**: Dive takes complete control, blocking ALL other movement input
- Works seamlessly with existing crouch/slide system
- Uses same external velocity system as sliding
- Hand animations use slide pose for prone look
- Sprint/movement keys are completely ignored during dive and prone states

### Physics
- Forward velocity: Camera-relative direction
- Gravity: Uses sliding gravity system (-750 units/s²)
- Terminal velocity: 180 units/s
- Landing detection: CharacterController grounded check

### State Management
- **isDiving**: In-air dive phase
- **isDiveProne**: Grounded prone phase
- Both states enforce crouch visuals
- Cannot slide or crouch while diving/prone

## 🎯 Tactical Uses

### Aggressive Plays
- Dive around corners for surprise attacks
- Close distance quickly while staying low
- Dodge incoming fire with unpredictable movement

### Positioning
- Dive into cover
- Quick prone for stealth
- Momentum-based repositioning

### Movement Flow
- Sprint → Dive → Prone → Stand → Continue
- Can chain into slide after standing
- Maintains movement momentum

## 🎬 Animation Integration

### Hand Animations
- **Dive Start**: Triggers slide pose (hands forward)
- **Prone**: Maintains slide pose
- **Stand Up**: Returns to normal pose

### Future Enhancements
- Custom dive animation (arms forward, legs back)
- Prone-specific hand pose
- Transition animations

## 🔊 Sound Setup

1. Open `SoundEvents` asset in Unity
2. Find **PLAYER: Movement** section
3. Assign audio clip to `Tactical Dive Sound`
4. Recommended: Whoosh/wind sound with impact

## 🎨 Particle Setup (Optional)

1. Create particle system for dive trail
2. Assign to `Dive Particles` in CleanAAACrouch
3. Particles play during flight phase
4. Auto-stop on landing

## 🐛 Troubleshooting

### Dive Not Triggering
- Check sprint speed is above `diveMinSprintSpeed`
- Ensure player is grounded
- Verify X key is not bound elsewhere
- Check `enableTacticalDive` is true

### Weird Landing Behavior
- Adjust `diveUpwardForce` for better arc
- Increase `diveForwardForce` for more distance
- Check ground collision layers

### Camera Issues
- Verify camera transform is assigned
- Check AAACameraController is present
- Adjust `diveLandingCompression` for impact feel

## 🎮 Tuning Guide

### For Longer Dives
- Increase `diveForwardForce` (720 → 900)
- Increase `diveUpwardForce` (240 → 320)

### For Quicker Recovery
- Decrease `diveProneDuration` (0.8 → 0.5)
- Or just press any key to stand immediately

### For More Impact
- Increase `diveLandingCompression` (400 → 600)
- Add heavier landing sound
- Increase camera shake duration

### For Tactical Stealth
- Decrease `diveForwardForce` (720 → 480)
- Increase `diveProneDuration` (0.8 → 1.5)
- Disable dive particles

## 🚀 Advanced Tips

### Dive Canceling
- Press jump during flight to cancel into normal jump
- (Not implemented yet - future feature)

### Dive Chaining
- Land → Stand immediately → Sprint → Dive again
- Creates aggressive movement flow

### Dive Angles
- Look down while diving for steeper angle
- Look up for longer distance (forward vector is horizontal)

## 📊 Default Values Summary (Scaled for 320-Unit Player)

```
Dive Forward Force: 720 units/s (4x scale)
Dive Upward Force: 240 units/s (4x scale)
Dive Min Sprint Speed: 320 units/s (4x scale)
Dive Prone Duration: 0.8 seconds
Dive Landing Compression: 400 units (4x scale)
Gravity: -750 units/s² (sliding gravity)
Terminal Velocity: 180 units/s
Player Height: 320 units
Player Radius: 50 units
```

## 🎯 Call of Duty Comparison

This system replicates the iconic CoD dolphin dive:
- ✅ Fast forward launch
- ✅ Upward arc trajectory
- ✅ Prone landing position
- ✅ Instant recovery on input
- ✅ Sprint requirement
- ✅ Camera impact on landing

## 🔮 Future Enhancements

- [ ] Dive damage to enemies (belly flop attack)
- [ ] Dive through windows/obstacles
- [ ] Dive cancel into jump
- [ ] Dive momentum carry into slide
- [ ] Custom dive animation
- [ ] Dive distance/height HUD indicator
- [ ] Dive cooldown system
- [ ] Dive achievements/stats

---

**Created**: 2025-10-04  
**System**: Tactical Dive  
**Inspired By**: Call of Duty Series  
**Status**: ✅ Fully Implemented & Ready to Use
