# Control Remapping Summary

## Changes Made - October 23, 2025

### Input System Architecture
Your project uses **Unity's legacy Input system** with:
- **Controls.cs**: Static class centralizing all keybindings
- **InputSettings.cs**: ScriptableObject for Inspector-configurable controls
- **Mouse button indices**: 0=LMB, 1=RMB, 2=MMB, 3=Side Button 1, 4=Side Button 2

---

## 1. Sword Toggle Control

### Previous Configuration
- **Key**: `Backspace` (KeyCode.Backspace)
- **Location**: `Controls.cs` line 37, `InputSettings.cs` line 50
- **Input Check**: `PlayerShooterOrchestrator.cs` line 270

### New Configuration
- **Key**: `Mouse3` (Side Button 1)
- **Implementation**: Direct `Input.GetMouseButtonDown(3)` check
- **Reason**: Bypassed Controls.cs system since mouse buttons aren't KeyCodes

### Files Modified
- `Assets\scripts\PlayerShooterOrchestrator.cs` (line 270)
  - Changed from `Input.GetKeyDown(Controls.SwordModeToggle)`
  - Changed to `Input.GetMouseButtonDown(3)`
  - Updated comment to reflect Mouse3 usage

---

## 2. Session Marker System Controls

### Previous Configuration
- **Set Marker**: `Mouse3` (Side Button 1) - line 79
- **Teleport to Marker**: `Mouse4` (Side Button 2) - line 85
- **Location**: `SessionMarkerSystem.cs`

### New Configuration
- **Set Marker**: `INSERT` key (KeyCode.Insert)
- **Teleport to Marker**: `DELETE` key (KeyCode.Delete)

### Files Modified
- `Assets\scripts\SessionMarkerSystem.cs`
  - Line 6-7: Updated header documentation
  - Line 79: Changed `Input.GetMouseButtonDown(3)` → `Input.GetKeyDown(KeyCode.Insert)`
  - Line 85: Changed `Input.GetMouseButtonDown(4)` → `Input.GetKeyDown(KeyCode.Delete)`
  - Line 130: Updated debug message to reference INSERT key

---

## Control Summary Table

| Action | Old Control | New Control | Script Location |
|--------|-------------|-------------|-----------------|
| **Sword Toggle** | Backspace | Mouse3 (Side Button) | PlayerShooterOrchestrator.cs:270 |
| **Set Session Marker** | Mouse3 | INSERT | SessionMarkerSystem.cs:79 |
| **Teleport to Marker** | Mouse4 | DELETE | SessionMarkerSystem.cs:85 |

---

## Testing Checklist

- [ ] Sword toggle activates/deactivates with Mouse3 (side button)
- [ ] Session marker sets correctly with INSERT key (only when grounded)
- [ ] Session marker teleport works with DELETE key
- [ ] No conflicts with other mouse button inputs
- [ ] Visual feedback displays correctly for all actions

---

## Notes

### Why Mouse3 Instead of KeyCode?
Unity's `Input.GetMouseButtonDown()` uses integer indices (0-6), not KeyCode enum values. Mouse buttons cannot be assigned to KeyCode variables in InputSettings.cs, so the sword toggle now uses a direct mouse button check.

### Session Marker Behavior
- **SET (INSERT)**: Only works when player is grounded (safety feature)
- **TELEPORT (DELETE)**: Can be used anytime, includes fade-to-black effect

### Future Improvements
If you want to make mouse buttons configurable via InputSettings:
1. Add integer fields to InputSettings.cs (e.g., `public int swordToggleMouseButton = 3`)
2. Create a Controls.cs property to store the mouse button index
3. Update PlayerShooterOrchestrator to use the configurable value

---

## Rollback Instructions

If you need to revert these changes:

### Sword Toggle → Back to Backspace
In `PlayerShooterOrchestrator.cs` line 270:
```csharp
// Change this:
if (Input.GetMouseButtonDown(3))

// Back to this:
if (Input.GetKeyDown(Controls.SwordModeToggle))
```

### Session Markers → Back to Mouse Buttons
In `SessionMarkerSystem.cs` lines 79 and 85:
```csharp
// Change INSERT back to Mouse3:
if (Input.GetKeyDown(KeyCode.Insert)) → if (Input.GetMouseButtonDown(3))

// Change DELETE back to Mouse4:
if (Input.GetKeyDown(KeyCode.Delete)) → if (Input.GetMouseButtonDown(4))
```
