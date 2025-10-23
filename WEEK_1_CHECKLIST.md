# ✅ WEEK 1 CHECKLIST: "Playable Loop" Sprint
## Days 1-5: Make it playable from menu to death to menu

---

## 🎯 **YOUR ONLY GOAL THIS WEEK**
> "Boot game → Click button → Play 5 platforms → Die → See stats → Back to menu"

If you finish this by Sunday, you're 40% done with your ENTIRE DEMO.

---

## 📅 DAY 1-2: TUTORIAL PLATFORM (6-8 hours)

### Monday Morning: Setup
- [ ] Open Unity project
- [ ] Create new scene: `Assets/Scenes/TutorialPlatform.unity`
- [ ] Save it

### Monday Afternoon: Build the Platform
- [ ] Create empty GameObject: "TutorialPlatform"
- [ ] Add a large plane/cube (Scale: 50x1x50) - this is your platform
- [ ] Add material with emission (make it glow slightly)
- [ ] Position at world origin (0, 0, 0)

### Monday Evening: Add Player Spawn
- [ ] Find your Player prefab (probably in `Assets/prefabs_made/`)
- [ ] Drag into scene at position (0, 2, 0) - above platform
- [ ] Test: Press Play - can you move/jump?
- [ ] ✅ **CHECKPOINT**: "I can control the player"

### Tuesday Morning: Tutorial Prompts
You need to create simple UI text that teaches controls.

**Option A: Quick & Dirty (15 minutes)**
- [ ] Create Canvas (UI → Canvas)
- [ ] Add Text (UI → Legacy → Text)
- [ ] Position in center of screen
- [ ] Text: "WASD = Move | SPACE = Jump | SHIFT = Sprint | CTRL = Slide"
- [ ] Make text BIG (Font size: 24)

**Option B: Proper Tutorial (1 hour)** - Use existing UI
- [ ] Find `UIManager` in your project
- [ ] Check if you have a tutorial panel already
- [ ] If yes: Enable it, set text
- [ ] If no: Use Option A

### Tuesday Afternoon: Add Enemies
- [ ] Find `SkullEnemy` prefab
- [ ] Drag 3 into scene, spread them out
- [ ] Test: Do they attack you when close?
- [ ] ✅ **CHECKPOINT**: "I can shoot enemies"

### Tuesday Evening: Add Chest + Elevator
- [ ] Find chest prefab (ChestController)
- [ ] Place 1 chest on platform
- [ ] Find elevator prefab (ElevatorController)
- [ ] Place elevator at edge of platform
- [ ] Test: Can you loot chest? Can you activate elevator?
- [ ] ✅ **CHECKPOINT**: "Tutorial platform is playable"

---

## 📅 DAY 3-4: PLATFORMS 2-5 (6-8 hours)

### Wednesday: Duplicate + Scale
- [ ] In Scene Hierarchy, duplicate TutorialPlatform (Ctrl+D)
- [ ] Rename to "Platform_02"
- [ ] Move it vertically: Y = +30 (above tutorial platform)
- [ ] Add 5 enemies (SkullEnemy)
- [ ] Add 1 chest
- [ ] Add 1 elevator

**REPEAT THIS 3 MORE TIMES** (Platforms 3, 4, 5)
- Platform_03: 8 enemies, 2 chests
- Platform_04: 10 enemies, 1 chest, 1 TowerController
- Platform_05: 1 GuardianEnemy (boss), 1 chest

**Pro Tip**: Stack them vertically (Y = 0, 30, 60, 90, 120)

### Thursday: Connect Elevators
- [ ] Open `ElevatorController.cs`
- [ ] Check: Does it have "nextSceneName" or "nextPlatform" variable?
- [ ] Set each elevator to load next platform scene OR teleport to next platform
- [ ] Test: Can you ride elevator from Platform 1 → 2 → 3?
- [ ] ✅ **CHECKPOINT**: "5 platforms exist and are connected"

---

## 📅 DAY 5: DEATH & MENU LOOP (4-5 hours)

### Friday Morning: Death Screen
- [ ] When player health hits 0, does `PlayerHealth.cs` trigger something?
- [ ] Check if `XPSummaryUI.cs` already exists (IT DOES!)
- [ ] Find the prefab/canvas for XP summary
- [ ] Test: Die intentionally - do you see a stats screen?

**If stats screen exists:**
- [ ] Make sure it shows: Platforms survived, enemies killed, gems collected
- [ ] Add button: "Return to Menu" → loads main menu scene

**If stats screen doesn't exist (Quick Fix):**
```csharp
// Add to PlayerHealth.cs in OnDeath():
Debug.Log("YOU DIED! Platforms: " + platformsCleared + " Kills: " + enemiesKilled);
// Load menu scene:
SceneManager.LoadScene("MainMenu");
```

### Friday Afternoon: Menu Flow
- [ ] Open your main menu scene
- [ ] Find "Start Game" button
- [ ] Make sure it loads TutorialPlatform scene
- [ ] Test FULL LOOP:
  1. Boot game
  2. Click "Start"
  3. Play through platforms
  4. Die
  5. See stats
  6. Return to menu
- [ ] ✅ **CHECKPOINT**: "Complete loop works!"

### Friday Evening: Quick Fixes
Fix any broken parts:
- [ ] Player spawns underground? → Adjust spawn height
- [ ] Enemies don't spawn? → Check spawner is enabled
- [ ] Elevator doesn't work? → Check scene name is correct
- [ ] UI is invisible? → Check Canvas render mode

---

## 🏁 WEEKEND CHECKPOINT (Sunday Evening)

### The Ultimate Test:
1. Close Unity
2. Reopen project
3. Press Play
4. Play through the loop WITHOUT opening any scripts
5. Answer honestly:

**Does it feel like a game?**
- ✅ **YES**: You crushed Week 1! Take Monday off, start Week 2 on Tuesday.
- ❌ **NO**: What's broken? Spend Monday-Tuesday fixing that ONE thing.

---

## 🚨 **IF YOU GET STUCK**

### Common Issues + Fixes

**"Player falls through ground"**
→ Add BoxCollider to platform, make sure player has CharacterController

**"Enemies don't attack"**
→ Check if they have `SkullEnemy.cs` attached and `player` reference is set

**"Can't shoot"**
→ Check `PlayerShooterOrchestrator` is enabled, left mouse button is bound

**"Elevator does nothing"**
→ Check `ElevatorController` has trigger collider, player has "Player" tag

**"Scene won't load"**
→ File → Build Settings → Add all 6 scenes (Menu + 5 platforms)

**"I'm overwhelmed"**
→ Do ONE task from the list. Just one. Then take a break. Come back, do another.

---

## 💡 **PROTIPS FOR STAYING MOTIVATED**

### Start Each Day With:
1. Open Unity
2. Press Play
3. Wall-run for 30 seconds
4. Remember why you built this
5. Pick ONE checkbox from today's list
6. Do that. Celebrate. Next checkbox.

### End Each Day With:
1. Save scene (Ctrl+S)
2. Git commit (or just Save Project)
3. Write down ONE thing that worked today
4. Example: "Enemies spawn and attack!"
5. That's a win. Tomorrow you build on it.

### When You Want to Quit:
Remember: You already built the HARD parts (movement, shooting, AI).  
This week is just stitching them together.  
You're not building new systems.  
You're arranging furniture in a house you already constructed.

---

## 📊 PROGRESS TRACKER

```
Week 1 Completion: [    ] 0%

Day 1-2: Tutorial Platform         [    ] 0/8 tasks
Day 3-4: Platforms 2-5             [    ] 0/6 tasks  
Day 5:   Death Loop                [    ] 0/5 tasks

Total: [    ] 0/19 tasks complete
```

Update this each day. Seeing progress = motivation fuel.

---

## 🎉 **WHEN YOU FINISH WEEK 1**

You will have:
- ✅ A playable game loop
- ✅ 5 platforms with escalating difficulty
- ✅ Working death/respawn
- ✅ Menu integration
- ✅ Proof that your systems actually work together

**This is HUGE.**

Most game projects die because devs can't get past this phase.  
You're about to be in the top 20% of game devs who have a PLAYABLE PROTOTYPE.

Now go check that first box. 📦

**Day 1, Task 1: Open Unity. You got this.** 🚀
