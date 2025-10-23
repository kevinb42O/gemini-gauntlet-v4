# ⚡ TOWER PROTECTOR CUBE - QUICK SETUP GUIDE

## 🚀 5-Minute Setup

### Step 1: Place the Cube
1. Drag **SkullSpawnerCube** prefab into scene
2. Position on top of Central Tower
3. Scale to desired size (recommend 10-20 units)

### Step 2: Assign Beam Prefab
1. Select SkullSpawnerCube in Hierarchy
2. In Inspector, find **"Beam Prefab"** field
3. Navigate to: `Assets/MagicArsenal/Effects/Prefabs/Beams/Setup/Beam/`
4. Drag **Arcane Beam.prefab** into the field

**Alternative Beams:**
- 🔥 **Fire Beam** - Red flames
- ⚡ **Lightning Beam** - Electric bolts
- ❄️ **Frost Beam** - Ice beam
- ✨ **Life Beam** - Green energy
- 🌍 **Earth Beam** - Brown/green

### Step 3: Assign Sound Events
1. In Inspector, find **"Sound Events"** field
2. Drag your **SoundEvents** ScriptableObject
3. Open SoundEvents asset
4. Find **"► ENVIRONMENT: Towers"** section
5. Assign audio clip to **"Tower Laser Shoot"**

### Step 4: Setup UI Sliders
1. In Canvas, create **two UI Sliders**:
   - **CaptureProgressSlider** (bottom, larger)
   - **CubeHealthSlider** (above capture, smaller)
2. Select GameObject with **PlatformCaptureUI** component
3. Assign both sliders in Inspector
4. See **AAA_TOWER_PROTECTOR_UI_SETUP.md** for detailed styling

### Step 5: Link to Platform System
1. Select **PlatformCaptureSystem** in Hierarchy
2. In Inspector, find **"Tower Protector Cube"** field
3. Drag SkullSpawnerCube from Hierarchy into this field

### Step 6: Test!
1. Enter Play Mode
2. Wait 5 seconds for first attack
3. Watch cube glow orange (tracking)
4. See the epic beam fire!
5. Try capturing platform while cube is alive

---

## ⚙️ Quick Settings Reference

### Health & Damage
```
Max Health: 1000 HP (default)
Laser Damage: 50 DPS (250 total per attack)
```

### Timing
```
Attack Interval: 15 seconds
Warning Phase: 2 seconds (orange glow)
Laser Duration: 5 seconds (yellow glow + beam)
```

### Tracking
```
Tracking Speed: 45°/second
Aim Prediction: 0.3 (30% lead)
Laser Range: 2000 units
```

---

## 🎨 Visual States at a Glance

| Glow Color | State | What's Happening |
|------------|-------|------------------|
| 🔴 **Red** | Hostile Idle | Waiting to attack |
| 🟠 **Orange** | Tracking | 2-second warning! |
| 🟡 **Yellow** | Firing | LASER ACTIVE! |
| 🟢 **Green** | Friendly | Platform captured |
| ⚪ **White Flash** | Hit | Taking damage |

---

## 🎯 Gameplay Tips

### For Players
- **Orange glow = GET READY!** You have 2 seconds
- **Sprint sideways** to dodge the beam
- **Use cover** if available
- **Kill it early** for easy capture
- **Keep it alive** for friendly ally (bragging rights!)

### For Level Designers
- Place on **high ground** for visibility
- Add **cover structures** for fairness
- Use **multiple cubes** for extreme difficulty
- Try **different beam types** for variety

---

## 🔧 Common Tweaks

### Make it Easier
```
Max Health: 500 HP
Laser Damage: 25 DPS
Tracking Speed: 30°/s
Aim Prediction: 0.1
```

### Make it BRUTAL
```
Max Health: 2000 HP
Laser Damage: 100 DPS
Tracking Speed: 90°/s
Aim Prediction: 0.5
Attack Interval: 10 seconds
```

### Boss Fight Mode
```
Max Health: 5000 HP
Laser Damage: 75 DPS
Laser Duration: 8 seconds
Attack Interval: 8 seconds
Place 3 cubes around platform
```

---

## ✅ Verification Checklist

Quick test to ensure everything works:

- [ ] Cube glows red when idle
- [ ] Cube glows orange before attack
- [ ] Magic beam spawns with particles
- [ ] Beam tracks player movement
- [ ] Player takes damage from beam
- [ ] Cube takes damage from weapons
- [ ] **Health slider appears** when on platform
- [ ] **Health slider updates** when cube takes damage
- [ ] **Health slider color changes** (green → red)
- [ ] Cube turns green when platform captured
- [ ] **Health slider turns cyan** when friendly
- [ ] **Health slider hides** when cube dies
- [ ] Sound plays during laser
- [ ] Death sequence works

---

## 🎮 Expected Player Experience

### First Encounter
```
1. Player lands on platform
2. Cube starts glowing orange
3. "What's happening?!"
4. LASER FIRES
5. "OH SHIT!"
6. Player learns to dodge
```

### Mastery
```
1. Player sees orange glow
2. Immediately sprints sideways
3. Dodges beam like a pro
4. Shoots cube while dodging
5. "I'm a badass!"
```

---

## 🚨 If Something's Wrong

### No Beam Visual
→ Check Magic Beam Prefab is assigned

### No Damage
→ Verify Player has "Player" tag

### No Sound
→ Assign SoundEvents + audio clip

### Cube Won't Turn Friendly
→ Link to PlatformCaptureSystem

---

## 🎉 You're Done!

The Tower Protector Cube is now ready to defend your platforms with epic laser beams!

**Pro Tip:** Try different beam types for different platforms - Fire for lava areas, Frost for ice areas, Lightning for tech areas, etc.

---

**This will be LEGENDARY! 🔥**
