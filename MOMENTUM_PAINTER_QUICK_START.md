# 🚀 Momentum Painter - 60 Second Setup

## Instant Setup (3 Steps)

### 1. Add to Player
```
Select Player → Add Component → Momentum Painter
```

### 2. Test Immediately
- **Run** = Orange fire trails (damage enemies)
- **Crouch** = Blue ice trails (heal you)
- **Jump** = Yellow lightning trails (stun enemies)
- **Walk** = Green harmony trails (buff allies)

### 3. Cross Your Own Trail
💥 **BOOM** - Resonance Burst (massive damage + healing)

---

## Controls Summary

| Action | Trail Type | Effect | Color |
|--------|-----------|---------|-------|
| Sprint (Shift) | 🔥 Fire | Damages enemies | Orange |
| Crouch (Ctrl/C) | ❄️ Ice | Slows enemies, heals you | Blue |
| Jump/Air (Space) | ⚡ Lightning | Stuns enemies | Yellow |
| Walk (Normal) | 🌿 Harmony | Buffs companions | Green |

**Special**: Cross any trail = 💥 Resonance Burst

---

## Instant Tactics

### "Quick Kill Loop"
1. Sprint in circle around enemy (fire trail)
2. Jump through center (cross trail)
3. 💥 Everything dies

### "Emergency Heal"
1. Crouch and move
2. Walk through your ice trail
3. Instant heal

### "Stunning Beauty"  
1. Jump repeatedly
2. Enemies walk into lightning trails
3. Permanent stun lock

---

## Optional Enhancements

### Better Visuals
1. Create Material: `Right-click → Create → Material`
2. Enable Emission
3. Set Emission Intensity = 3
4. Drag to Momentum Painter's "Trail Material"

### Audio (Optional)
Add any AudioClips to:
- Trail Create Sound
- Resonance Burst Sound  
- Trail Cross Sound

---

## Settings to Tweak

**More Damage**: Increase "Fire Trail Damage" (default 15)
**More Healing**: Increase "Ice Trail Heal Amount" (default 5)
**Longer Trails**: Increase "Trail Lifetime" (default 5s)
**Bigger Trails**: Increase "Trail Width" (default 0.5m)
**Bigger Bursts**: Increase "Resonance Burst Radius" (default 3m)

---

## Debug Tips

**No trails appearing?**
- Check you're moving (speed > 0.1)
- Check Trail Spawn Interval (should be 0.05)

**Trails not damaging?**
- Ensure enemies have "EnemyHealth" component
- Check Fire Trail Damage value

**Can't heal from ice?**
- Ensure player has "PlayerHealth" component  
- Check Ice Trail Heal Amount value

---

## That's It!

You now have the most innovative movement system in gaming history.

**Go create art. Go destroy enemies. Go paint the battlefield.**

🎨💥⚔️
