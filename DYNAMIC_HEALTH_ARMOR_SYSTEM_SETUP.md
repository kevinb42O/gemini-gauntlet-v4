# Dynamic Health Regeneration & Armor Plate System - Complete Setup Guide

## 🎯 System Overview

This system implements:
- **Dynamic Health Regeneration**: Health regenerates after 5 seconds of not taking damage with a fast-then-slow curve
- **Armor Plate System**: Up to 3 plates can be equipped, each absorbing 1500 damage before breaking
- **Visual Feedback**: Health slider changes color (green → orange → red) and pulsates when critical
- **Shield UI**: Blue shield slider shows plate status (split into 3 segments)
- **Instant Regen Feature**: Applying plates at low health instantly triggers regeneration

---

## 📋 Files Created/Modified

### New Files Created:
1. `Assets/scripts/ArmorPlateSystem.cs` - Core plate management
2. `Assets/scripts/ArmorPlateItemData.cs` - Plate item scriptable object
3. `Assets/scripts/Audio/FIXSOUNDSCRIPTS/SoundEvents.cs` - Added armor break sound
4. `Assets/scripts/Audio/FIXSOUNDSCRIPTS/GameSoundsHelper.cs` - Added PlayArmorBreak method

### Modified Files:
1. `Assets/scripts/PlayerHealth.cs` - Added regeneration system and plate damage routing
2. `Assets/scripts/HealthEnergyUI.cs` - Added shield slider and visual effects

---

## 🔧 Unity Inspector Setup

### Step 1: Player GameObject Setup

**Add ArmorPlateSystem Component:**
1. Select your **Player** GameObject in the hierarchy
2. Click **Add Component** → Search for "ArmorPlateSystem"
3. Configure the following settings:
   - **Max Plates**: 3
   - **Plate Health**: 1500
   - **Right Hand Animator**: Drag your right hand Animator component here
   - **Plate Apply Animation Trigger**: "ApplyPlate" (create this trigger in your animator)
   - **Plate Application Delay**: 0.5 seconds

**Update PlayerHealth Component:**
1. Select your **Player** GameObject
2. Find the **PlayerHealth** component
3. Verify these settings:
   - **Max Health**: 5000 (updated from 100)
   - **Health Regen Rate**: 15 HP/sec
   - **Regen Delay After Damage**: 5 seconds
   - **Regen Burst Multiplier**: 3x (initial burst speed)
   - **Regen Burst Duration**: 2 seconds

---

### Step 2: UI Setup

**Health & Shield Sliders:**
1. Open your **Canvas** in the hierarchy
2. Find your **HealthEnergyUI** GameObject (or create one if it doesn't exist)
3. Add the **HealthEnergyUI** component if not present
4. Configure the following:

**UI References:**
- **Health Slider**: Drag your health slider here
- **Shield Slider**: Create a new slider above the health slider (see UI creation below)
- **Energy Slider**: Drag your energy slider here

**Slider Fill Images:**
- **Health Fill Image**: The fill image of your health slider
- **Shield Fill Image**: The fill image of your shield slider
- **Energy Fill Image**: The fill image of your energy slider

**Health Colors:**
- **Health Color Normal**: RGB(0, 204, 0) - Green
- **Health Color Low**: RGB(255, 128, 0) - Orange
- **Health Color Critical**: RGB(204, 26, 26) - Red
- **Low Health Threshold**: 0.3 (30%)
- **Critical Health Threshold**: 0.2 (20%)

**Shield Color:**
- **Shield Color**: RGB(0, 128, 255) - Blue

**Visual Effects:**
- **Regen Burst Scale Multiplier**: 1.2
- **Regen Burst Scale Duration**: 2 seconds
- **Critical Pulse Scale**: 1.15
- **Critical Pulse Duration**: 0.5 seconds

---

### Step 3: Create Shield Slider UI

**Creating the Shield Slider:**
1. In your Canvas, duplicate your existing Health Slider
2. Rename it to "ShieldSlider"
3. Position it **above** the Health Slider
4. Change the fill color to **Blue** (RGB: 0, 128, 255)
5. Set the slider's **Max Value** to 1 and **Value** to 0
6. Drag this new slider into the **Shield Slider** field in HealthEnergyUI

**Optional: Add Plate Segment Dividers:**
- Create 2 vertical lines (UI Images) on the shield slider to visually divide it into 3 segments
- Position them at 33% and 66% of the slider width

---

### Step 4: Create Armor Plate Item

**Create the Scriptable Object:**
1. In your Project window, navigate to a suitable folder (e.g., `Assets/Items/`)
2. Right-click → **Create** → **Inventory** → **Armor Plate**
3. Name it "ArmorPlate"
4. Configure the item:
   - **Item Name**: "Armor Plate"
   - **Description**: "Protective armor plating that absorbs damage before health is affected. Press C to equip up to 3 plates. Each plate provides 1500 shield points."
   - **Item Icon**: Assign a suitable icon sprite
   - **Item Type**: "ArmorPlate" (auto-set)
   - **Item Rarity**: 3 (Rare/Blue)
   - **Rarity Color**: RGB(0, 128, 255) - Blue
   - **Plate Shield Amount**: 1500
   - **Is Stackable**: ✓ (checked)

---

### Step 5: Add Plates to Chest Loot Tables

**Add to ChestInteractionSystem:**
1. Find your **ChestInteractionSystem** GameObject in the scene
2. In the Inspector, find the **Possible Items** list
3. Click the **+** button to add a new slot
4. Drag your **ArmorPlate** scriptable object into this slot
5. For testing, add it multiple times to increase spawn rate

**Testing Configuration:**
- Add the ArmorPlate item 3-5 times to the Possible Items list for high spawn rate
- This ensures chests will frequently contain plates during testing

---

### Step 6: Sound Setup

**Add Armor Break Sound:**
1. Find your **SoundEvents** scriptable object (usually in `Assets/Audio/`)
2. Open it in the Inspector
3. Find the **Player: Armor** section
4. Expand **Armor Break** array
5. Add a short, impactful sound effect (e.g., metal breaking, glass shattering)
6. Configure the sound:
   - **Volume**: 0.8-1.0
   - **Pitch**: 1.0
   - **Pitch Variation**: 0.05
   - **Cooldown**: 0.1 seconds (prevent spam)

**Recommended Sound Characteristics:**
- Duration: 0.2-0.5 seconds
- Type: Sharp, metallic, or crystalline breaking sound
- Should be distinct from damage sounds

**Add Armor Plate Apply Sound:**
1. In the same **Player: Armor** section
2. Find **Armor Plate Apply** field (single sound, not array)
3. Add a satisfying mechanical/tech sound (e.g., armor locking, tech activation)
4. Configure the sound:
   - **Volume**: 0.7-0.9
   - **Pitch**: 1.0
   - **Pitch Variation**: 0.03
   - **Loop**: false
   - **Cooldown**: 0 (system handles interruption automatically)

**Recommended Apply Sound Characteristics:**
- Duration: 0.5-1.5 seconds (can be longer, will be interrupted if needed)
- Type: Mechanical locking, tech activation, armor clamping sound
- Should feel satisfying and protective
- **Important**: Sound will be interrupted if another plate is applied quickly (no overlap)

---

### Step 7: Animation Setup (Optional)

**Create Plate Application Animation:**
1. Open your **Right Hand** Animator Controller
2. Create a new animation state called "ApplyPlate"
3. Add a trigger parameter called "ApplyPlate"
4. Create a transition from **Any State** → **ApplyPlate**
5. Set the transition condition to the "ApplyPlate" trigger
6. Animate the hand:
   - Quick motion toward the chest/torso area
   - Duration: ~0.3-0.5 seconds
   - Should feel like the player is attaching armor

**If you skip this step:**
- The system will still work perfectly
- You just won't see the hand animation when applying plates

---

## 🎮 How to Use In-Game

### Player Controls:
- **Press C**: Apply plates from inventory (fills all available slots automatically)
- **Automatic**: Health regenerates after 5 seconds of not taking damage
- **Automatic**: Plates absorb all damage before health is affected

### Visual Feedback:
- **Shield Slider (Blue)**: Shows above health bar when plates are equipped
  - Full bar = 3 plates
  - 2/3 bar = 2 plates
  - 1/3 bar = 1 plate
  - Hidden = 0 plates

- **Health Slider (Green/Orange/Red)**:
  - **Green**: Health above 30%
  - **Orange**: Health between 20-30%
  - **Red + Pulsating**: Health below 20%
  - **Scales up during regen burst**: Initial 2 seconds of regeneration

### System Behavior:
1. **Taking Damage**: Plates absorb damage first, then health
2. **Plate Breaking**: Sound plays, warning message appears
3. **Health Regeneration**: Starts 5 seconds after last damage
4. **Regen Curve**: Fast initial burst (3x speed) for 2 seconds, then normal speed
5. **Low Health Plate Application**: Instantly triggers regeneration (no 5-second wait)

---

## 🧪 Testing Checklist

### Basic Functionality:
- [ ] Player spawns with 5000 health and 0 plates
- [ ] Chests contain armor plates
- [ ] Plates can be picked up and stored in inventory
- [ ] Pressing C applies plates (up to 3 maximum)
- [ ] Shield slider appears when plates are equipped
- [ ] Shield slider shows correct fill level (1/3, 2/3, 3/3)

### Damage System:
- [ ] Taking damage depletes plates first
- [ ] Each plate absorbs 1500 damage before breaking
- [ ] Armor break sound plays when plate breaks
- [ ] Health only takes damage after all plates are gone
- [ ] Damage stops health regeneration

### Regeneration System:
- [ ] Health regenerates after 5 seconds of no damage
- [ ] Initial burst is faster (first 2 seconds)
- [ ] Health slider scales up during burst
- [ ] Regeneration stops when taking damage
- [ ] Regeneration stops at full health

### Visual Feedback:
- [ ] Health slider is green when healthy (>30%)
- [ ] Health slider turns orange when low (20-30%)
- [ ] Health slider turns red when critical (<20%)
- [ ] Health slider pulsates when critical
- [ ] Shield slider is blue
- [ ] Shield slider hides when no plates equipped

### Special Features:
- [ ] Applying plates at low health (<20%) instantly starts regeneration
- [ ] All plates and inventory plates are lost on death
- [ ] Plates do NOT persist between game and menu scenes

---

## 🐛 Troubleshooting

### Issue: Plates don't appear in chests
**Solution**: 
- Verify ArmorPlate item is added to ChestInteractionSystem's Possible Items list
- Check that the item's itemType is set to "ArmorPlate"

### Issue: Shield slider doesn't appear
**Solution**:
- Ensure Shield Slider is assigned in HealthEnergyUI component
- Check that ArmorPlateSystem component is on the Player GameObject
- Verify the shield slider GameObject is active in the hierarchy

### Issue: Health doesn't regenerate
**Solution**:
- Check PlayerHealth component has correct regen settings
- Ensure player is not taking continuous damage
- Verify the regeneration coroutine is starting (check console logs)

### Issue: Pressing C doesn't apply plates
**Solution**:
- Verify plates are in inventory (check inventory UI)
- Ensure ArmorPlateSystem component is on Player GameObject
- Check console for error messages
- Verify InventoryManager.Instance is not null

### Issue: Armor break sound doesn't play
**Solution**:
- Assign armor break sound in SoundEvents scriptable object
- Check that sound clip is not null
- Verify GameSounds.PlayArmorBreak is being called (check console)

### Issue: Health slider doesn't change color
**Solution**:
- Ensure Health Fill Image is assigned in HealthEnergyUI
- Check that color values are set correctly
- Verify the UpdateHealthUI method is being called

---

## 📊 System Values Reference

### Health System:
- **Max Health**: 5000 HP
- **Regen Rate**: 15 HP/second (normal)
- **Regen Rate (Burst)**: 45 HP/second (first 2 seconds)
- **Regen Delay**: 5 seconds after damage
- **Low Health Threshold**: 30% (1500 HP)
- **Critical Health Threshold**: 20% (1000 HP)

### Armor Plate System:
- **Max Plates**: 3
- **Plate Health**: 1500 HP each
- **Total Shield**: 4500 HP (with 3 plates)
- **Application Key**: C
- **Application Delay**: 0.5 seconds per plate

### Visual Effects:
- **Regen Burst Scale**: 1.2x (20% larger)
- **Critical Pulse Scale**: 1.15x (15% larger)
- **Pulse Duration**: 0.5 seconds per cycle

---

## 🎨 Customization Tips

### Adjusting Regeneration Speed:
- Increase **Health Regen Rate** for faster healing
- Decrease **Regen Delay After Damage** for quicker recovery
- Adjust **Regen Burst Multiplier** for more/less initial burst

### Adjusting Plate Strength:
- Modify **Plate Health** in ArmorPlateSystem for stronger/weaker plates
- Change **Max Plates** to allow more/fewer plates

### Adjusting Visual Effects:
- Increase **Regen Burst Scale Multiplier** for more dramatic effect
- Adjust **Critical Pulse Scale** for more/less pulsation
- Modify color thresholds for different warning levels

### Adjusting Difficulty:
- **Easier**: Increase max health, increase plate health, decrease regen delay
- **Harder**: Decrease max health, decrease plate health, increase regen delay

---

## 🚀 Advanced Features

### Instant Regeneration Feature:
When the player applies plates while health is below 20%, regeneration starts **immediately** instead of waiting 5 seconds. This rewards tactical plate usage and creates exciting comeback moments.

### Plate Damage Routing:
All damage is routed through the ArmorPlateSystem first. Plates absorb damage completely before any health is affected. Each plate must be fully depleted before the next plate starts taking damage.

### Dynamic Visual Feedback:
The health slider provides real-time feedback:
- **Size changes** during regeneration burst
- **Color changes** based on health percentage
- **Pulsation** when health is critical
- **Shield slider visibility** based on plate count

---

## 📝 Notes

- Plates are **consumable** items that do NOT persist between game sessions
- Plates are **stackable** in inventory (unlimited storage)
- Only **3 plates** can be equipped at once
- Plates are **lost on death** (both equipped and inventory)
- Health regeneration uses **unscaled time** (not affected by slow-motion effects)
- The system is fully integrated with your existing damage, inventory, and UI systems

---

## ✅ System Complete!

Your dynamic health regeneration and armor plate system is now fully implemented and ready to use! The system provides:
- ✓ Smooth health regeneration with visual feedback
- ✓ Strategic armor plate system
- ✓ Intuitive UI with color-coded health states
- ✓ Rewarding gameplay mechanics
- ✓ Full integration with existing systems

**Enjoy your new health and armor system!** 🎮🛡️
