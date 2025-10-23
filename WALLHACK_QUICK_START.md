# 🚀 WALLHACK QUICK START GUIDE
## Get Running in 60 Seconds!

---

## ⚡ **FASTEST SETUP EVER**

### **Option A: One-Click Setup (EASIEST)**

1. **Select your Main Camera or Player GameObject**
2. **Add Component** → Search for `AAACheatSystemIntegration`
3. **Press Play** ✅

That's it! The integration script auto-configures everything!

---

### **Option B: Manual Setup (More Control)**

1. **Main Camera/Player:**
   - Add Component → `AAAWallhackSystem`
   - Add Component → `AAAESPOverlay`
   
2. **Create Empty GameObject** named "CheatManager"
   - Add Component → `AAACheatManager`
   
3. **Press Play** ✅

---

## 🎮 **TESTING IT**

1. **Start the game**
2. **Press F10** to enable wallhack
3. **Look at enemies** - they should glow through walls!

### **Default Hotkeys:**
- **F10** = Toggle ALL cheats
- **F2** = Toggle Wallhack + ESP
- **F1** = Open cheat menu

---

## 🔧 **FOR TESTING ONLY: Give Yourself Points**

In `AAACheatManager` inspector:
- Set **Cheat Points** to `10000`
- Now you can unlock all cheats!

---

## 🎯 **Quick Settings (For Best Look)**

In **AAAWallhackSystem**:
```
Wallhack Enabled: ☑ TRUE
Occluded Color: (255, 50, 50, 153) - Red
Visible Color: (50, 255, 50, 204) - Green
Glow Intensity: 1.5
Outline Width: 0.005
```

---

## 💡 **Troubleshooting**

**"I don't see any glow!"**
→ Make sure enemies are tagged as "Enemy" or "Boss"

**"It's laggy!"**
→ Lower Update Frequency to 20 in AAAWallhackSystem

**"Shader errors!"**
→ Check that WallhackShader.shader compiled (check Console)

---

## 🎨 **Make It Look Like EngineOwning**

Copy these EXACT settings:

**AAAWallhackSystem:**
```
Occluded Color: RGB(255, 100, 0) Alpha: 150
Visible Color: RGB(0, 255, 100) Alpha: 200
Outline Color: RGB(255, 255, 255) Alpha: 255
Outline Width: 0.006
Glow Intensity: 2.0
Fresnel Power: 4.0
```

Now it looks EXACTLY like the pro cheats! 😎

---

## 📞 **Need More Help?**

Read the full guide: `AAA_WALLHACK_SYSTEM_COMPLETE.md`

---

**YOU'RE DONE! Enjoy your professional wallhack system! 🎮✨**
