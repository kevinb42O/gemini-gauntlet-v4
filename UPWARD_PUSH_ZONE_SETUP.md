# 🚀 Upward Push Zone - Quick Setup Guide

## What It Does
Creates a fun zone that pushes the player upward when they step into it! Perfect for jump pads, updrafts, or vertical boost areas.

## Setup Steps

### 1. Create the Zone GameObject
1. In Unity Hierarchy, create an empty GameObject (Right-click → Create Empty)
2. Name it "UpwardPushZone" or "JumpPad" or whatever you like
3. Position it where you want the push zone to be

### 2. Add the Script
1. Select the GameObject
2. In Inspector, click "Add Component"
3. Search for "Upward Push Zone" and add it

### 3. Add a Trigger Collider
1. With the GameObject selected, click "Add Component"
2. Add a **Sphere Collider** or **Box Collider**
3. **IMPORTANT**: Check the "Is Trigger" checkbox
4. Adjust the collider size to match your zone radius

### 4. Configure the Zone Settings

**Zone Settings:**
- **Zone Radius**: How wide the push zone is (default: 5)
- **Push Force**: How strong the upward push is (default: 20)
  - Try 15-25 for gentle lift
  - Try 30-50 for strong boost
  - Try 50+ for launch pad effect!
- **Max Push Height**: How high above the zone the push still works (default: 10)
  - Player stops being pushed once they reach this height

**Visual Settings:**
- **Show Gizmos**: See the zone in the editor (green cylinder with yellow arrow)
- **Gizmo Color**: Change the visualization color

**Optional Effects:**
- **Entry Effect**: Drag a Particle System here for visual effects when entering
- **Audio Source**: Drag an Audio Source for sound effects

## Example Configurations

### For CharacterController (Most Unity Games)

**Gentle Updraft**
- Zone Radius: 50
- Push Force: 50
- Max Push Height: 400
- Use Impulse Mode: No
- Great for floating platforms or wind tunnels

**Jump Pad (Continuous)**
- Zone Radius: 30
- Push Force: 100
- Max Push Height: 300
- Use Impulse Mode: No
- Continuous upward push

**Launch Pad (Impulse)**
- Zone Radius: 50
- Push Force: 80
- Max Push Height: 500
- Use Impulse Mode: Yes
- Impulse Cooldown: 0.5
- Instant powerful boost!

**Super Launch (Impulse)**
- Zone Radius: 40
- Push Force: 150
- Max Push Height: 800
- Use Impulse Mode: Yes
- Impulse Cooldown: 1.0
- MEGA BOOST!

**ROCKET LAUNCH (Impulse)**
- Zone Radius: 60
- Push Force: 250
- Max Push Height: 1000
- Use Impulse Mode: Yes
- Impulse Cooldown: 1.5
- TO THE MOON! 🚀

### For Small Characters (10-50 units tall)

**Gentle Updraft**
- Zone Radius: 8
- Push Force: 15
- Max Push Height: 15

**Jump Pad**
- Zone Radius: 3
- Push Force: 40
- Max Push Height: 8

**Launch Pad**
- Zone Radius: 5
- Push Force: 60
- Max Push Height: 20

## Tips & Tricks

1. **Collider Size**: Make sure your trigger collider is large enough to cover the zone radius
2. **CharacterController vs Rigidbody**: This system works with CharacterController (velocity-based), not Rigidbody physics
3. **Impulse vs Continuous**:
   - **Impulse Mode**: Instant velocity set, great for jump pads (values like 80-150)
   - **Continuous Mode**: Adds velocity every frame, great for updrafts (values like 50-100)
4. **Multiple Zones**: You can place multiple zones in your level for fun traversal
5. **Combine with Flight**: Works great with the flight system for extended air time
6. **Visual Feedback**: Add particle effects (smoke, wind, energy) for better player feedback
7. **Sound Design**: Add whoosh sounds when entering for better feel
8. **Check Console**: Debug logs show when player enters and what mode is active
9. **Start Small**: Begin with Push Force around 100 and adjust from there

## How It Works

1. Player steps into the trigger zone
2. Script checks if player is within horizontal radius
3. Script checks if player is below max height
4. If both true, applies continuous upward force
5. Force stops when player exits radius or reaches max height

## Debugging

- **Green cylinder in editor**: Shows the active zone area
- **Yellow arrow**: Shows push direction and approximate strength
- **Console logs**: Shows when player enters/exits zone
- **Gizmos not showing?**: Make sure "Show Gizmos" is checked and Gizmos are enabled in Scene view

## Have Fun!

Experiment with different values to create unique movement mechanics. Try combining multiple zones for interesting vertical traversal puzzles! 🎮✨
