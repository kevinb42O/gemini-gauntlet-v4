# Blood Splash - Copy/Paste Settings Card

## 🎯 Ultra-Fast Setup (30 seconds)

### Particle System Main Module
```
Duration: 0.5
Looping: OFF
Start Lifetime: Random Between Two Constants (0.8 - 1.2)
Start Speed: Random Between Two Constants (15 - 25)
Start Size: Random Between Two Constants (0.5 - 1.5)
Start Color: #8B0000 (Dark Red)
Gravity Modifier: 1.5
Simulation Space: World
Max Particles: 30
```

### Emission Module
```
Rate over Time: 0
Bursts:
  Time: 0.0
  Count: 25
  Cycles: 1
  Interval: 0
```

### Shape Module
```
Shape: Sphere
Radius: 1.0
Radius Thickness: 0
Randomize Direction: 0.3
```

### Renderer Module
```
Render Mode: Billboard
Material: Default-Particle
Min Particle Size: 0
Max Particle Size: 0.5
```

### BloodSplashEffect Component
```
Lifetime: 2.0 seconds
Splash Sound: (optional)
Sound Volume: 0.3
```

---

## 🔥 Performance Budget
- **Max Particles:** 30 (can reduce to 15 if needed)
- **Lifetime:** 2 seconds total
- **Cost per death:** ~0.01ms (negligible)

---

## ✅ That's it!
Create prefab → Assign to skull's Death Effect Prefab field → Done!
