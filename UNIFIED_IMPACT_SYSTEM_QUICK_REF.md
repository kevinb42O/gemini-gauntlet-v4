# 🎯 UNIFIED IMPACT SYSTEM - QUICK REFERENCE

## 🚀 QUICK START (30 seconds)

### What Changed?
Superhero landing now triggers based on **ACTUAL FALL HEIGHT**, not just tricks!

### Do I Need To Do Anything?
**NO!** ✅ The system is backward compatible and works automatically.

### Optional: Remove Deprecated System
```
GameObject → Player → Remove Component → SuperheroLandingSystem
```

---

## 📊 HOW IT WORKS (60 seconds)

```
Player Falls
    ↓
FallingDamageSystem calculates impact
    ↓
Broadcasts ImpactData event
    ↓
AAACameraController receives event
    ↓
Triggers trauma + spring + superhero landing (if epic enough)
```

---

## 🦸 SUPERHERO LANDING TRIGGERS

### ✅ WILL Trigger
- Fall 2000+ units (always)
- Hang in air 2+ seconds + fall 640+ units
- Do aerial tricks + fall 640+ units

### ❌ WON'T Trigger
- Small jumps (<640 units)
- Falls without tricks/airtime/height combo
- Platform/elevator movement

---

## 🔧 TUNING

### Make Superhero Landing Easier
**File:** `FallingDamageSystem.cs`  
**Method:** `CalculateImpactData()`  
**Lines:** ~495-500

```csharp
// Change thresholds (lower = easier to trigger)
impact.shouldTriggerSuperheroLanding = 
    (fallDistance >= 1500f) ||  // Was 2000
    (airTime >= 1.5f && fallDistance >= 500f) || // Was 2.0s, 640u
    (impact.wasInTrick && fallDistance >= 500f); // Was 640u
```

### Change Impact Damage
**File:** `FallingDamageSystem.cs`  
**Inspector:** Scaled Fall Damage section

Adjust:
- Min Damage Fall Height (default: 320)
- Moderate Damage Fall Height (default: 640)
- Severe Damage Fall Height (default: 960)
- Lethal Fall Height (default: 1280)

### Disable Damage (Keep Visual Effects)
**File:** `FallingDamageSystem.cs`  
**Inspector:** Set all damage values to 0

---

## 🐛 DEBUGGING

### Enable Debug Logging
```csharp
// In any Start() method
ImpactEventBroadcaster.EnableDebugLogging = true;
```

### Console Output
```
[IMPACT SYSTEM] 📢 BROADCAST: 🟡 Moderate Impact | Fall: 1500u | Superhero: YES 🦸
🦸 [SUPERHERO LANDING] TRIGGERED by IMPACT! Fall: 1500u, Air: 2.5s
```

### Check Listeners
```csharp
int count = ImpactEventBroadcaster.GetListenerCount();
string names = ImpactEventBroadcaster.GetListenerNames();
Debug.Log($"Listeners: {count} - {names}");
```

---

## 🎓 FOR ADVANCED USERS

### Add Custom Impact Listener
```csharp
public class MyImpactHandler : MonoBehaviour
{
    void Start() { ImpactEventBroadcaster.OnImpact += HandleImpact; }
    void OnDestroy() { ImpactEventBroadcaster.OnImpact -= HandleImpact; }
    
    void HandleImpact(ImpactData impact)
    {
        Debug.Log($"Impact detected: {impact.severity}");
        // Your custom logic here
    }
}
```

### Access Impact Data
```csharp
void HandleImpact(ImpactData impact)
{
    float fallHeight = impact.fallDistance;
    float airTime = impact.airTime;
    ImpactSeverity tier = impact.severity;
    bool superhero = impact.shouldTriggerSuperheroLanding;
    bool onSlope = impact.wasOnSlope;
    bool wasSprinting = impact.wasSprinting;
    bool doingTricks = impact.wasInTrick;
}
```

---

## 📁 KEY FILES

- `ImpactData.cs` - Data structure
- `ImpactEventBroadcaster.cs` - Event system
- `FallingDamageSystem.cs` - Authority (calculates impacts)
- `AAACameraController.cs` - Listener (handles camera effects)
- `SuperheroLandingSystem.cs` - **DEPRECATED** (don't use)

---

## ❓ FAQ

**Q: Will superhero landing still work with tricks?**  
A: Yes! Better than before. Tricks + decent fall = superhero landing.

**Q: Can I keep using SuperheroLandingSystem?**  
A: Yes, but it's deprecated. Will be removed in future. Migrate ASAP.

**Q: Does this break my existing gameplay?**  
A: No! Backward compatible. Everything works better now.

**Q: How do I disable superhero landing?**  
A: AAACameraController inspector → uncheck "Enable Superhero Landing"

**Q: Can I adjust trauma/compression per impact?**  
A: Yes! Modify `CalculateImpactData()` in FallingDamageSystem.

---

## 🆘 HELP

**Problem:** Superhero landing not triggering  
**Solution:** Check fall distance (needs 640+ units) OR airtime (2+ sec)

**Problem:** Too many superhero landings  
**Solution:** Increase thresholds in `CalculateImpactData()`

**Problem:** Compilation errors  
**Solution:** Ensure all 3 new files exist (ImpactData.cs, ImpactEventBroadcaster.cs, implementation doc)

**Problem:** Deprecation warnings  
**Solution:** Remove SuperheroLandingSystem component from player

---

## 📚 FULL DOCUMENTATION

See: `UNIFIED_IMPACT_SYSTEM_IMPLEMENTATION.md`

---

**Version:** 1.0  
**Last Updated:** October 16, 2025  
**Status:** Production Ready ✅
