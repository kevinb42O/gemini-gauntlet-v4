# Interaction Animations - Quick Reference

## Unity Animator Setup Checklist

### Required Animator Parameters (Both Hands):
- ✅ `PlayGrab` (Trigger)
- ✅ `PlayOpenDoor` (Trigger)  
- ✅ `abilityType` (Int) - values 0-3

### Ability Layer (Layer 3) States:
- ✅ **Grab** state with your grab animation clip
- ✅ **OpenDoor** state with your open door animation clip
- ✅ **Exit** state (should already exist)

### Transitions to Add:

#### Any State → Grab
- Conditions: `PlayGrab` + `abilityType == 2`
- Has Exit Time: **false**
- Duration: **0**

#### Any State → OpenDoor
- Conditions: `PlayOpenDoor` + `abilityType == 3`
- Has Exit Time: **false**
- Duration: **0**

#### Grab → Exit
- Has Exit Time: **true** (Exit Time: 1.0)
- Duration: **0.1**

#### OpenDoor → Exit
- Has Exit Time: **true** (Exit Time: 1.0)
- Duration: **0.1**

## When Animations Play

### Grab Animation:
- **Trigger**: Player picks up item from chest (double-click or drag)
- **Duration**: 0.8 seconds
- **Hand**: Right hand only
- **System**: ChestInteractionSystem

### OpenDoor Animation:
- **Trigger**: Player uses keycard on door (press E)
- **Duration**: 1.2 seconds
- **Hand**: Right hand only
- **System**: KeycardDoor

## Animation Clip Requirements

### Grab Clip:
- Duration: ~0.5-1.0 seconds
- Motion: Reach forward → close fist
- Bones: Arm + hand only

### OpenDoor Clip:
- Duration: ~1.0-1.5 seconds
- Motion: Reach forward → push/pull
- Bones: Arm + hand only

## Testing Commands

```
1. Test Grab: Open chest → double-click item
   Console: "📦 🎬 Playing grab animation for item pickup"

2. Test OpenDoor: Approach door with keycard → press E
   Console: "[KeycardDoor] 🎬 Playing open door animation"
```

## File Locations

**Scripts Modified:**
- `Assets/scripts/IndividualLayeredHandController.cs`
- `Assets/scripts/LayeredHandAnimationController.cs`
- `Assets/scripts/ChestInteractionSystem.cs`
- `Assets/scripts/KeycardDoor.cs`

**Documentation:**
- `INTERACTION_ANIMATIONS_SETUP.md` (full guide)
- `INTERACTION_ANIMATIONS_QUICK_REFERENCE.md` (this file)

## AbilityState Enum Values

```csharp
None = 0       // No ability active
ArmorPlate = 1 // Armor plate animation
Grab = 2       // Grab item animation (NEW)
OpenDoor = 3   // Open door animation (NEW)
```

## Common Issues

| Problem | Solution |
|---------|----------|
| Animation doesn't play | Check animator parameters exist |
| Animation too fast/slow | Adjust duration in code (lines 504, 548) |
| Wrong hand animates | Interactions are right hand only (by design) |
| Animation gets stuck | Check Exit transitions have "Has Exit Time" enabled |

---

**Ready to use!** Just assign your animation clips in Unity Animator and you're done! 🎬
