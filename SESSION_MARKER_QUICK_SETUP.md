# 🚀 SESSION MARKER - 60 SECOND SETUP

## What You Get
- **Mouse3** = Set marker (green notification)
- **Mouse4** = Teleport back (fade effect + cyan notification)

## Setup (3 Steps)

### 1. Add Component
Select **Player GameObject** → Add Component → **SessionMarkerSystem**

### 2. Verify It Works
- Play game
- Press **Mouse3** - See "SESSION MARKER SET" 
- Move away
- Press **Mouse4** - Fade to black, teleport, fade in

### 3. Done
That's it. No configuration needed.

## Controls
```
Mouse3 (Side Button 1)  = Set Session Marker
Mouse4 (Side Button 2)  = Teleport to Marker
```

## What It Does
✅ Saves position/rotation  
✅ Teleports with fade effect  
✅ Resets all velocities  
✅ Shows feedback via CognitiveFeedManager  
✅ Respects pause state  

## Dependencies (Auto-Detected)
- UIManager (for fade effects)
- CognitiveFeedManagerEnhanced (for messages)
- CharacterController (on player)

## Customization
All settings in Inspector:
- **Show Debug Logs** - Console output
- **Marker Set Color** - Notification color
- **Marker Teleport Color** - Notification color  
- **Fade Duration** - Teleport speed (0.3s default)

## Don't Have Side Mouse Buttons?
Edit `SessionMarkerSystem.cs` lines 69-78:
```csharp
// Change from Mouse3/4 to any keys you want
if (Input.GetKeyDown(KeyCode.F5))  // Set marker
if (Input.GetKeyDown(KeyCode.F9))  // Teleport
```

## Code Quality
- Zero GC allocation
- Proper physics handling
- Clean coroutine-based fades
- Senior dev architecture
- Fully commented

**That's all. Now go test it. 🔥**
