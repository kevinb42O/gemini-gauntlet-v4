# 🎮 MOVEMENT SYSTEM QUICK REFERENCE

## 🚀 **QUICK ACCESS - WHAT WORKS NOW**

**✅ ALL SYSTEMS 101% FUNCTIONAL**

---

## 🎯 **SINGLE SOURCE OF TRUTH**

| **What You Need** | **Where To Get It** | **DON'T Use** |
|-------------------|---------------------|---------------|
| Is player grounded? | `movement.IsGrounded` | ~~`controller.isGrounded`~~ |
| Is player falling? | `movement.IsFalling` | ~~Local `wasFalling` checks~~ |
| Time since grounded? | `movement.TimeSinceGrounded` | ~~Local time tracking~~ |
| Is player sliding? | `crouchController.IsSliding` | ~~Local state vars~~ |
| Is player diving? | `crouchController.IsDiving` | ~~Local state vars~~ |
| Input keys? | `Controls.Crouch`, `Controls.Dive` | ~~`Input.GetKey(KeyCode.X)`~~ |

---

## 🔧 **API CHEAT SHEET**

### **Need to modify player velocity?**
```csharp
// ✅ CORRECT
movement.SetExternalVelocity(myVelocity, duration, overrideGravity: false);

// ❌ WRONG
velocity = myVelocity; // Don't touch velocity directly!
controller.Move(myVelocity * Time.deltaTime); // Don't bypass AAA!
```

### **Need to change slope limit?**
```csharp
// ✅ CORRECT
bool granted = movement.RequestSlopeLimitOverride(
    90f, 
    AAAMovementController.ControllerModificationSource.Crouch
);

if (granted) {
    // Your code here
    movement.RestoreSlopeLimitToOriginal(); // Restore when done
}

// ❌ WRONG
controller.slopeLimit = 90f; // Don't modify directly!
```

### **Need to block player input?**
```csharp
// ✅ CORRECT (Dive system)
movement.EnableDiveOverride();  // Blocks input
// ... do dive stuff ...
movement.DisableDiveOverride(); // Restores input

// ❌ WRONG
// Setting flags without telling AAA - input will still process!
```

### **Need to preserve momentum through air?**
```csharp
// ✅ CORRECT
Vector3 horizontalMomentum = new Vector3(velocity.x, 0, velocity.z);
movement.LatchAirMomentum(horizontalMomentum);

// ❌ WRONG
// Creating your own momentum system - conflicts with AAA's system!
```

---

## 📋 **COMMON PATTERNS**

### **Pattern 1: Slide System**
```csharp
void StartSlide()
{
    // Request slope override
    movement.RequestSlopeLimitOverride(90f, ControllerModificationSource.Crouch);
    
    // Set velocity (2-frame duration to avoid spam)
    movement.SetExternalVelocity(slideVelocity, Time.deltaTime * 2f, false);
}

void StopSlide()
{
    // Restore slope limit
    movement.RestoreSlopeLimitToOriginal();
    
    // Clear velocity
    movement.ClearExternalForce();
}
```

### **Pattern 2: Dive System**
```csharp
void StartDive()
{
    // Block input + set ownership
    movement.EnableDiveOverride();
    
    // Set velocity for entire dive duration (not per-frame!)
    movement.SetExternalVelocity(diveVelocity, diveDuration, false);
}

void EndDive()
{
    // Restore input + ownership
    movement.DisableDiveOverride();
    
    // Velocity clears automatically after duration
}
```

### **Pattern 3: Check State Before Action**
```csharp
void TryDoSomething()
{
    // ✅ Use AAA's state
    if (movement.IsGrounded && !movement.IsFalling)
    {
        // Safe to do ground-only action
    }
    
    // ✅ Check ownership before modifying
    bool granted = movement.RequestSlopeLimitOverride(value, source);
    if (!granted)
    {
        Debug.LogWarning("Another system owns the controller!");
        return;
    }
}
```

---

## ⚠️ **COMMON MISTAKES TO AVOID**

### **❌ DON'T: Frame-by-Frame Velocity Spam**
```csharp
// BAD - Called every frame!
void Update()
{
    movement.SetExternalVelocity(myVel, Time.deltaTime, false);
}
```

### **✅ DO: Set Once with Duration**
```csharp
// GOOD - Set once, lasts multiple frames
void StartAction()
{
    float duration = 2.0f; // Or Time.deltaTime * 2f for continuous
    movement.SetExternalVelocity(myVel, duration, false);
}
```

### **❌ DON'T: Duplicate State Tracking**
```csharp
// BAD - Redundant with AAA
private bool wasGrounded;
private bool wasFalling;
private float timeGrounded;

void Update()
{
    wasGrounded = IsGrounded; // Already in AAA!
}
```

### **✅ DO: Use AAA's Properties**
```csharp
// GOOD - Single source of truth
void Update()
{
    if (movement.IsGrounded) { }
    if (movement.IsFalling) { }
    float timeGrounded = movement.TimeSinceGrounded;
}
```

### **❌ DON'T: Direct Controller Modification**
```csharp
// BAD - Causes conflicts
controller.slopeLimit = 90f;
controller.stepOffset = 0f;
```

### **✅ DO: Use Coordination API**
```csharp
// GOOD - Coordinated access
movement.RequestSlopeLimitOverride(90f, source);
```

---

## 🎮 **INPUT SYSTEM**

### **All Input Through Controls Class**
```csharp
// Movement
if (Input.GetKey(Controls.Boost)) { }        // Shift (sprint)
if (Input.GetKey(Controls.Crouch)) { }       // Ctrl (crouch/slide)
if (Input.GetKeyDown(Controls.Dive)) { }     // X (dive)
if (Input.GetKeyDown(Controls.UpThrustJump)) { } // Space (jump)

// Camera-relative input
float h = Controls.HorizontalRaw(); // A/D
float v = Controls.VerticalRaw();   // W/S
```

### **Never Hardcode Keys**
```csharp
// ❌ BAD
if (Input.GetKeyDown(KeyCode.X)) { }

// ✅ GOOD  
if (Input.GetKeyDown(Controls.Dive)) { }
```

---

## 🔍 **DEBUGGING TIPS**

### **Check Current Owner**
```csharp
// AAA logs ownership on every modification
// Look for: "[CONTROLLER] ... Owner: Crouch/Dive/Movement"
```

### **Check External Force Status**
```csharp
// AAA logs velocity changes
// Look for: "[VELOCITY] SetExternalVelocity - Magnitude: X, Duration: Y"
```

### **Check Permission Denials**
```csharp
// AAA warns when requests denied
// Look for: "[CONTROLLER] ... modification denied - Owner: X, Requester: Y"
```

---

## 📊 **SYSTEM PRIORITIES**

### **Ownership Hierarchy** (Higher = More Control)
1. **Dive** - Blocks all input, full control
2. **Crouch/Slide** - Modifies controller, sets velocity
3. **Movement** - Normal player control

### **When Systems Conflict**
- Dive **always wins** (blocks movement input)
- Crouch **requests permission** from AAA
- AAA **grants/denies** based on current owner

---

## 🎓 **BEST PRACTICES**

### **1. Always Check Grounded State**
```csharp
// Before ground-only actions
if (!movement.IsGrounded) return;
```

### **2. Use Duration-Based Forces**
```csharp
// Not per-frame spam
movement.SetExternalVelocity(vel, duration, false);
```

### **3. Clean Up After Yourself**
```csharp
void OnDisable()
{
    // Restore controller state
    movement.RestoreSlopeLimitToOriginal();
    
    // Clear forces
    movement.ClearExternalForce();
    
    // Restore input
    movement.DisableDiveOverride();
}
```

### **4. Request, Don't Take**
```csharp
// Always use coordination API
bool granted = movement.RequestSlopeLimitOverride(value, source);
if (!granted) return; // Respect denial
```

### **5. Single Source of Truth**
```csharp
// Use AAA's state, not local tracking
if (movement.IsFalling) { }
if (movement.IsGrounded) { }
```

---

## 🚨 **EMERGENCY FIXES**

### **Player stuck in slide?**
```csharp
// Force restore
movement.RestoreSlopeLimitToOriginal();
movement.ClearExternalForce();
```

### **Velocity not applying?**
```csharp
// Check ownership
Debug.Log($"Owner: {movement._currentOwner}"); // Check logs
```

### **Input not working?**
```csharp
// Check dive override
movement.DisableDiveOverride(); // Force restore
```

---

## 📞 **QUICK CONTACTS**

| Component | File | Responsibility |
|-----------|------|----------------|
| **AAA Movement** | `AAAMovementController.cs` | Velocity, grounded state, controller ownership |
| **Crouch/Slide** | `CleanAAACrouch.cs` | Slide physics, dive system, crouch |
| **Input** | `Controls.cs` | All input key bindings |
| **Energy** | `PlayerEnergySystem.cs` | Sprint authorization |

---

**Remember: Request, don't take. Coordinate, don't conflict. Single source of truth!** ✨
