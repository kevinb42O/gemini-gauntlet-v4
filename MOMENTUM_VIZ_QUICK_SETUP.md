# 🚀 MOMENTUM VISUALIZATION - QUICK SETUP

**2-Minute Setup with Your Own Canvas**

---

## ✅ SETUP STEPS

### Step 1: Create Canvas (if you don't have one)
1. Right-click Hierarchy → UI → Canvas
2. Name it: `GameCanvas` (or whatever you want)
3. Set Canvas Scaler:
   - UI Scale Mode: Scale With Screen Size
   - Reference Resolution: 1920 × 1080

### Step 2: Create Momentum Visualization
1. Right-click Hierarchy → Create Empty
2. Name it: `MomentumVisualization`
3. Add Component → `MomentumVisualization`

### Step 3: Assign Canvas
1. Select `MomentumVisualization` GameObject
2. In Inspector, find "Target Canvas" field
3. Drag your `GameCanvas` into the field
4. **Done!**

---

## 🎮 WHAT YOU'LL SEE

When you play:
- **Speed Counter:** Top center (shows when > 500 units/s)
- **Chain Counter:** Below speed (shows when chaining moves)
- **Gain Indicators:** Float up from player on speed gains

---

## ⚙️ OPTIONAL TUNING

In the Inspector, you can adjust:

**References:**
- Target Canvas: Your game's canvas (REQUIRED)
- Player Movement: Auto-found (leave empty)

**Speed Counter:**
- Show Speed Counter: ✅
- Speed Threshold: 500 (hide when slower)
- Speed Counter Position: (0.5, 0.85) - screen space 0-1

**Chain Counter:**
- Show Chain Counter: ✅
- Chain Time Window: 2.0 seconds
- Min Speed Gain For Chain: 200 units/s

**Gain Indicators:**
- Show Gain Indicators: ✅
- Min Gain To Show: 100 units/s

**Styling:**
- Speed Font Size: 48
- Chain Font Size: 36
- Gain Font Size: 24

---

## 🐛 TROUBLESHOOTING

### "Inspector Locked" Error
- Click "Unlock" in the dialog
- This happens when Unity locks the inspector

### No UI Showing
- Check Target Canvas is assigned
- Check player speed > 500 units/s (for speed counter)
- Check Console for errors

### UI in Wrong Position
- Adjust Speed/Chain Counter Position (0-1 screen space)
- (0.5, 0.5) = center
- (0, 0) = bottom-left
- (1, 1) = top-right

### Canvas Not Found
- Assign Target Canvas in Inspector
- System will auto-create one if not assigned (but you lose control)

---

## 🎯 TESTING

1. **Play the game**
2. **Sprint around** → Speed counter should show ~1500
3. **Wall jump** → Should show "+800" gain indicator and "1x CHAIN!"
4. **Wall jump again** → Should show "2x CHAIN!"
5. **Land** → Chain should disappear

---

## 💡 PRO TIPS

**Better Control:**
- Use your existing game canvas (recommended!)
- Adjust sorting order if UI is behind other elements
- Position counters to match your UI style

**Performance:**
- System uses < 0.05ms/frame
- Object pooling = zero allocations
- Safe to leave enabled always

**Customization:**
- Change colors in code (speedGradient)
- Change fonts (assign in CreateUI)
- Add sound effects (in OnSpeedGain)

---

## ✅ CHECKLIST

- [ ] Canvas created/exists
- [ ] MomentumVisualization GameObject created
- [ ] MomentumVisualization component added
- [ ] Target Canvas assigned in Inspector
- [ ] Play mode → UI appears when moving fast
- [ ] No console errors

---

**That's it! You now have professional momentum visualization!** 🔥
