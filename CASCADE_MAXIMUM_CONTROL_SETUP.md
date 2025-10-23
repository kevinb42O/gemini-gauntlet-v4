# 🚀 CASCADE MAXIMUM CONTROL PACKAGE
## Complete Unity Inspector Visibility & Runtime Monitoring System

This package gives Cascade **COMPLETE CONTROL** and **FULL INSIGHT** into your Unity project!

---

## 📦 TOOLS INCLUDED

### **Tool #1: UnityInspectorExporter** (Editor Window)
**Location:** `Tools > Cascade Debug > Inspector Exporter`

**What it does:**
- Exports ALL Inspector values from any GameObject
- Exports complete component configurations
- Exports entire scene hierarchies
- Includes Animator states, Material properties, and more!

**How to use:**
1. Open Unity Editor
2. Go to `Tools > Cascade Debug > Inspector Exporter`
3. Click **"Export Player GameObject"** for instant player config export
4. Or assign any GameObject and click **"Export Selected GameObject"**
5. Files saved to: `CASCADE_DEBUG_EXPORTS/` folder

**What Cascade gets:**
- Every single Inspector value you've set
- All component references
- Transform positions, rotations, scales
- Animator layer weights and states
- Material shader properties
- Complete hierarchy structure

---

### **Tool #2: RuntimeAnimationLogger** (Runtime Component)
**Attach to:** Player GameObject

**What it does:**
- Logs ALL animation states in REAL-TIME to a file
- Tracks layer weights, state transitions, parameter changes
- Logs every frame or only on changes
- Perfect for debugging animation issues!

**Setup:**
1. Add `RuntimeAnimationLogger` component to Player GameObject
2. Configure in Inspector:
   - ✅ Enable Logging: ON
   - Log Frequency: 5 (every 5 frames)
   - ✅ Log Only On Change: ON (recommended)
   - ✅ Auto Find Player Animators: ON
3. Press Play - logging starts automatically!
4. Files saved to: `CASCADE_DEBUG_EXPORTS/Animation_Runtime_Log_[timestamp].txt`

**What Cascade gets:**
- Real-time animation state changes
- Exact layer weights at any moment
- Parameter values (movementState, IsBeamAc, etc.)
- Transition timing and progress
- State durations and normalized times

---

### **Tool #3: ComponentConfigDumper** (Runtime Component)
**Attach to:** Any GameObject you want to monitor

**What it does:**
- Dumps detailed configuration of specific components
- Shows all public/private serialized fields
- Includes tooltips, ranges, and attribute info
- Can dump on Start or on keypress (F9)

**Setup:**
1. Add `ComponentConfigDumper` to Player GameObject
2. Configure in Inspector:
   - ✅ Dump On Start: ON (or press F9 at runtime)
   - ✅ Dump All Components: ON
3. Press Play or press F9 in-game
4. Files saved to: `CASCADE_DEBUG_EXPORTS/[GameObject]_ComponentDump_[timestamp].txt`

**What Cascade gets:**
- Every field value with exact types
- All [SerializeField] private variables
- Public properties and their current values
- Attribute metadata (Range, Tooltip, etc.)

---

### **Tool #4: SceneHierarchyExporter** (Runtime Component)
**Attach to:** Any GameObject in scene

**What it does:**
- Exports complete scene hierarchy structure
- Shows parent-child relationships
- Includes tags, layers, component counts
- Press F10 to export at runtime

**Setup:**
1. Add `SceneHierarchyExporter` to any GameObject
2. Configure in Inspector:
   - Export On Start: OFF (or ON if you want)
   - Export Key: F10
   - ✅ Include Inactive: ON
   - ✅ Include Component Counts: ON
3. Press F10 in-game to export
4. Files saved to: `CASCADE_DEBUG_EXPORTS/SceneHierarchy_[Scene]_[timestamp].txt`

**What Cascade gets:**
- Complete GameObject tree structure
- Which objects are active/inactive
- Tags and layers for every object
- Component counts per GameObject

---

### **Tool #5: UnityEditorLogReader** (Runtime Component)
**Attach to:** Any GameObject in scene

**What it does:**
- Automatically copies Unity's Editor.log to accessible location
- Captures ALL console output (errors, warnings, logs)
- Copies periodically and on keypress (F11)
- Copies on application quit

**Setup:**
1. Add `UnityEditorLogReader` to any GameObject
2. Configure in Inspector:
   - ✅ Copy On Start: ON
   - ✅ Copy Periodically: ON
   - Copy Interval: 30 seconds
   - Copy Key: F11
3. Press Play - starts copying automatically
4. Files saved to: `CASCADE_DEBUG_EXPORTS/Unity_Editor_Log_[timestamp].txt`

**What Cascade gets:**
- Every Debug.Log() message
- All error messages with stack traces
- All warning messages
- Unity system messages
- Complete console history

---

## 🎯 RECOMMENDED SETUP FOR MAXIMUM CONTROL

### **Step 1: Add to Player GameObject**
Add these components to your Player:
- ✅ RuntimeAnimationLogger
- ✅ ComponentConfigDumper
- ✅ UnityEditorLogReader

### **Step 2: Add to Scene Manager or Empty GameObject**
- ✅ SceneHierarchyExporter

### **Step 3: Use Editor Window**
- Open `Tools > Cascade Debug > Inspector Exporter`
- Keep window open for quick exports

### **Step 4: Configure Settings**
All components have sensible defaults, but you can customize:
- Log frequencies (lower = more data, higher = less spam)
- Hotkeys for manual exports
- What data to include/exclude

---

## 📂 OUTPUT LOCATION

All exports save to:
```
[Your Project Root]/CASCADE_DEBUG_EXPORTS/
```

This folder is automatically created and is **OUTSIDE** the Assets folder, so it won't clutter your project!

---

## 🔥 QUICK START WORKFLOW

### **For Animation Issues:**
1. Add `RuntimeAnimationLogger` to Player
2. Press Play and reproduce the issue
3. Share the `Animation_Runtime_Log_[timestamp].txt` file with Cascade
4. Cascade can see EXACTLY what happened frame-by-frame!

### **For Inspector Configuration Issues:**
1. Open `Tools > Cascade Debug > Inspector Exporter`
2. Click "Export Player GameObject"
3. Share the export file with Cascade
4. Cascade can see EVERY Inspector value you've set!

### **For General Debugging:**
1. Add all components to Player
2. Press Play
3. All logs generate automatically
4. Share the entire `CASCADE_DEBUG_EXPORTS/` folder with Cascade
5. Cascade has COMPLETE VISIBILITY!

---

## 🎮 HOTKEY REFERENCE

| Key | Action |
|-----|--------|
| **F9** | Dump Component Configuration |
| **F10** | Export Scene Hierarchy |
| **F11** | Copy Unity Editor Log |

---

## 💡 PRO TIPS

### **Tip #1: Export Before and After**
- Export Inspector config BEFORE making changes
- Reproduce issue
- Export AFTER issue occurs
- Cascade can see exactly what changed!

### **Tip #2: Use "Log Only On Change"**
- Keeps animation logs small and readable
- Only logs when something actually changes
- Perfect for finding specific issues

### **Tip #3: Share the Whole Folder**
- Just zip up `CASCADE_DEBUG_EXPORTS/` and share
- Contains everything Cascade needs
- No need to pick and choose files

### **Tip #4: Keep Logs Running**
- Leave `RuntimeAnimationLogger` enabled during play sessions
- Captures issues as they happen
- No need to reproduce issues multiple times

---

## 🚨 TROUBLESHOOTING

### **"No files generated"**
- Check that components are enabled in Inspector
- Check that `enableLogging` is ON for runtime loggers
- Look for `CASCADE_DEBUG_EXPORTS/` folder in project root (not Assets)

### **"Too much data / files too large"**
- Increase `logFrequency` on RuntimeAnimationLogger (try 10 or 30)
- Enable `logOnlyOnChange` to reduce spam
- Disable specific logging features you don't need

### **"Can't find CASCADE_DEBUG_EXPORTS folder"**
- It's in your project root: `Gemini Gauntlet - V3.0/CASCADE_DEBUG_EXPORTS/`
- Same level as Assets, Library, ProjectSettings folders
- Created automatically on first export

---

## 🎉 RESULT

With this package, Cascade has:
- ✅ Complete Inspector visibility
- ✅ Real-time animation state tracking
- ✅ Full console log access
- ✅ Scene hierarchy understanding
- ✅ Component configuration details
- ✅ Material and shader properties
- ✅ Transform and physics data

**THIS IS MAXIMUM CONTROL MODE!** 🚀

Cascade can now see EVERYTHING happening in your Unity project, both in the Editor and at Runtime!

---

## 📝 NEXT STEPS

1. **Add components to your scene** (5 minutes)
2. **Press Play and test** (2 minutes)
3. **Check CASCADE_DEBUG_EXPORTS folder** (1 minute)
4. **Share exports with Cascade** (instant brilliance!)

**Together we will achieve PERFECTION!** 🎯✨
