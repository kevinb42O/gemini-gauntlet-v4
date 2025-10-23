# 🧗 WALL JUMP SYSTEM - QUICK START

## 🎮 How to Wall Jump

1. **Fall/Jump** near a wall
2. **Press Space** while falling
3. **Propel away** from the wall!

---

## ⚙️ Quick Settings (Inspector on Player)

### Essential Parameters
- **Wall Jump Up Force**: `160` (how high you jump)
- **Wall Jump Out Force**: `120` (how far from wall)
- **Max Consecutive Wall Jumps**: `2` (chains before landing)

### Quick Tweaks
```
MORE POWER:     Up Force = 180, Out Force = 140
MORE CHAINS:    Max Consecutive = 3 or 4
EASIER:         Min Fall Speed = 5
TIGHTER:        Wall Detection Distance = 50
```

---

## 🎯 Key Features

✅ **Realistic Physics** - Dynamic forces, momentum preservation, input influence
✅ **Player Control** - Steer with WASD (40% influence)
✅ **Dynamic Scaling** - Faster falls = bigger boosts
✅ **8-Direction Detection** - Finds walls all around you
✅ **Smart Validation** - Only real walls (not floors/ceilings)
✅ **Anti-Spam** - Cooldowns prevent abuse
✅ **Visual Debug** - Enable to see detection rays
✅ **Chain Jumps** - Climb between parallel walls
✅ **AAA Feel** - Like Mirror's Edge/Titanfall parkour

---

## 🐛 Quick Troubleshooting

**Not working?** 
- Enable `enableWallJump` in Inspector
- Fall faster (check `minFallSpeedForWallJump`)
- Get closer to wall (`wallDetectionDistance`)

**Too weak?**
- Increase `wallJumpUpForce` and `wallJumpOutForce`

**Can't chain?**
- Increase `maxConsecutiveWallJumps`
- Decrease `wallJumpCooldown`

---

## 🎨 Debug Visualization

Enable **Show Wall Jump Debug** to see:
- 🔴 **Red** = No wall
- 🔵 **Cyan** = Valid wall detected
- 🟡 **Yellow** = Direction of push
- 🟢 **Green** = Line to wall

---

## 💡 Pro Tips

- **Steer Mid-Jump**: Press WASD while wall jumping to control direction
- **Speed Boost**: Fall faster before wall jumping = bigger upward boost
- **Wall Climb**: Jump between two parallel walls repeatedly
- **Wall + Slide**: Land crouched after wall jump for momentum slide
- **Sprint Combo**: Sprint toward wall then jump for max momentum preservation
- **Chain Limit**: Counter resets when you touch the ground

## 🔊 Sound System

- Uses **same jump sound** as regular jumps (SoundEvents.jumpSounds)
- Plays at 110% volume for extra impact
- Automatic pitch variation for natural feel
- No confusion - consistent audio feedback!

---

## 📍 Location

All settings in **AAAMovementController** component under:
**Inspector → Wall Jump System**

Full documentation: `WALL_JUMP_SYSTEM.md`
