# HEAD COLLISION SYSTEM - QUICK SETUP (2 MINUTES)

## 🚀 INSTANT SETUP

### 1. Create Config Asset (30 seconds)
```
Right-click in Project → Create → Game → Head Collision Configuration
Name: HeadCollisionConfig_Default
```

### 2. Add to Player (30 seconds)
```
Select Player → Add Component → "Head Collision System"
Drag config asset to Config field
```

### 3. Done! ✅

All references auto-find. System ready to use.

---

## 🎯 DEFAULT VALUES (PRE-TUNED FOR 320-UNIT PLAYER)

**You don't need to change anything!** But if you want:

### Quick Damage Tuning
- **Light:** 150 HP @ 500 units/s (weak jump)
- **Moderate:** 500 HP @ 1200 units/s (full jump)
- **Severe:** 1200 HP @ 2500+ units/s (grapple launch)

### Quick Physics Tuning
- **Bounce:** 0.5 coefficient (50% energy loss)
- **Dampening:** 0.7 (keep 70% horizontal speed)

---

## 🧪 TEST IT

1. **Jump into ceiling** → Light damage + small bounce
2. **Grapple into overhang** → Moderate/Severe damage + big bounce
3. **Aerial trick into ceiling** → Severe damage + blood splat

---

## 🐛 TROUBLESHOOTING (1 SECOND EACH)

**No damage?**
→ Config assigned? ✅

**No bounce?**
→ AAAMovementController exists? ✅

**No camera shake?**
→ Camera has AAACameraController? ✅

---

## 📊 AT A GLANCE

| Feature | Status |
|---------|--------|
| Damage | ✅ Velocity-based |
| Bounce | ✅ Realistic physics |
| Audio | ✅ Impact sound |
| Camera | ✅ Trauma shake |
| Blood | ✅ Severe hits only |
| Config | ✅ ScriptableObject |
| Performance | ✅ Zero GC |

**System Status:** 🟢 PRODUCTION READY

See `AAA_HEAD_COLLISION_SYSTEM_COMPLETE.md` for full documentation.
