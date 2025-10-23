# Tower Dancing - Quick Reference

## The One Rule
**Towers MUST stay within their platform trigger boundaries at all times.**

## Implementation Summary

### First Move (Special)
- **Direction**: Towards platform center
- **Distance**: 60-80% of distance to center
- **Purpose**: Move inward if spawned near edges

### All Other Moves
- **Validation**: Must be within platform bounds
- **Safety Margin**: 2 units from edges
- **Collision**: 5+ units from other towers
- **Attempts**: Up to 10 tries to find valid position
- **Fallback**: Stay put if no valid position found

## Key Code Locations

### TowerController.cs

**Line 88**: First move flag
```csharp
protected bool _isFirstDanceMove = true;
```

**Line 616**: Reset flag on dance start
```csharp
_isFirstDanceMove = true; // Reset first move flag
```

**Lines 625-726**: Complete boundary-aware target selection
- Platform trigger detection
- Bounds checking with safety margin
- First move towards center logic
- Collision avoidance

**Line 1081**: Reset flag on collapse
```csharp
_isFirstDanceMove = true; // Reset for potential respawn
```

## Parameters

| Parameter | Value | Purpose |
|-----------|-------|---------|
| Safety Margin | 2 units | Distance from platform edges |
| Min Tower Distance | 5 units | Collision avoidance |
| Max Attempts | 10 | Position search limit |
| First Move % | 60-80% | Distance towards center |

## Testing Checklist

- [ ] Tower spawns near edge → moves inward first
- [ ] Tower stays within platform bounds
- [ ] Multiple towers don't overlap
- [ ] Small platforms work correctly
- [ ] Large platforms allow free movement
- [ ] Fallback works when no valid position

## Common Issues

**Tower leaves platform?**
- Check platform has PlatformTrigger component
- Verify PlatformTrigger has Collider component
- Check tower's `_associatedPlatformTriggerInternal` is set

**Tower not moving?**
- Check `_isDancing` flag is true
- Verify `_hasAppeared` is true
- Look for "couldn't find valid dancing position" warning

**Tower stuck at spawn?**
- Platform might be too small
- Other towers might be blocking all valid positions
- Check safety margin isn't too large for platform size

## Debug Commands

Watch Unity Console for:
```
[TowerController] {name} FIRST DANCE MOVE towards center
[TowerController] {name} picked valid dancing target within bounds
[TowerController] {name} couldn't find valid dancing position - staying put
```
