# 🧠 CASCADE AI ANALYSIS PROMPT - COPY & PASTE TEMPLATE

## 📋 STANDARD ANALYSIS PROMPT

Copy this prompt and fill in the bracketed sections:

---

```
I need you to analyze my Unity game optimization data from the Cascade Intelligent Debug System.

**FOCUS AREA:** [Animation System / Movement System / Combat System / Performance / etc.]

**GOAL:** [Reduce lag / Fix stuttering / Optimize memory / Improve responsiveness / etc.]

**FILES TO ANALYZE:**
I'm attaching the following debug files from CASCADE_DEBUG_EXPORTS/:

1. **AI Optimization Report:**
   - OptimizationReports/AI_Optimization_Report_[X]_[timestamp].txt

2. **Specific Script Logs:** (choose relevant ones)
   - IntelligentLogs/[ScriptName1]_[timestamp].txt
   - IntelligentLogs/[ScriptName2]_[timestamp].txt
   - IntelligentLogs/[ScriptName3]_[timestamp].txt

3. **Contextual Analysis:**
   - ContextualTracking/ContextualAnalysis_[timestamp].txt

**WHAT I NEED:**

1. **Priority Assessment:**
   - Confirm the AI report's priority rankings
   - Identify the #1 issue to fix first

2. **Root Cause Analysis:**
   - Explain WHY each issue is happening
   - Reference specific values/patterns from the logs

3. **Specific Code Fixes:**
   - Provide exact code changes (not just suggestions)
   - Show before/after code snippets
   - Explain what each fix does

4. **Implementation Order:**
   - Number the fixes in order of impact
   - Estimate performance gain for each

5. **Verification Steps:**
   - How to verify each fix worked
   - What values should change in the next report

**CONSTRAINTS:**
- Focus ONLY on HIGH and CRITICAL priority issues
- Provide code that's immediately runnable
- Keep explanations concise and actionable

**CONTEXT:**
[Add any relevant context about your game, recent changes, or specific issues you've noticed]
```

---

## 🎯 SPECIALIZED PROMPTS

### For Animation System Issues

```
**ANIMATION SYSTEM OPTIMIZATION ANALYSIS**

I'm experiencing [stuttering/lag/incorrect animations] in my animation system.

**Attached Files:**
- AI_Optimization_Report_[X].txt
- LayeredHandAnimationController_[timestamp].txt
- IndividualLayeredHandController_[timestamp].txt
- PlayerAnimationStateManager_[timestamp].txt (if applicable)
- ContextualAnalysis_[timestamp].txt

**Specific Issues I've Noticed:**
- [e.g., "Hands twitch when jumping"]
- [e.g., "Animation state changes too frequently"]
- [e.g., "Layer weights oscillating"]

**What I Need:**
1. Identify which component is causing the issue
2. Show me the exact code fix with line numbers
3. Explain the root cause in 2-3 sentences
4. Tell me what values should stabilize after the fix

**My Animation Architecture:**
- Using Unity Animator with 4 layers (Base, Shooting, Emote, Ability)
- LayeredHandAnimationController coordinates 8 hand instances
- Movement states: Idle, Walk, Sprint, Jump, Land, Slide, Dive
```

---

### For Movement System Issues

```
**MOVEMENT SYSTEM OPTIMIZATION ANALYSIS**

I'm experiencing [jittery movement/input lag/physics issues] in my movement system.

**Attached Files:**
- AI_Optimization_Report_[X].txt
- AAAMovementController_[timestamp].txt
- CleanAAACrouch_[timestamp].txt
- EnergySystem_[timestamp].txt
- ContextualAnalysis_[timestamp].txt

**Specific Issues I've Noticed:**
- [e.g., "Player velocity oscillates"]
- [e.g., "Grounded state flickers"]
- [e.g., "Sprint doesn't feel responsive"]

**What I Need:**
1. Identify oscillating or rapidly changing values
2. Show me smoothing/damping solutions with code
3. Explain if it's Update() vs FixedUpdate() issue
4. Provide cooldown/threshold values to use

**My Movement Setup:**
- Character Controller based movement
- Sprint system tied to EnergySystem
- Crouch/slide mechanics in CleanAAACrouch
- Ground detection with coyote time
```

---

### For Performance Bottlenecks

```
**PERFORMANCE BOTTLENECK ANALYSIS**

My game is running at [X FPS] and I need to improve it to [Y FPS].

**Attached Files:**
- AI_Optimization_Report_[X].txt
- [Top 3-5 scripts with highest execution time]
- ContextualAnalysis_[timestamp].txt

**Performance Symptoms:**
- [e.g., "Frame drops during combat"]
- [e.g., "Stuttering when many enemies spawn"]
- [e.g., "Slow when opening inventory"]

**What I Need:**
1. Identify the #1 performance bottleneck
2. Show me caching/pooling solutions with code
3. Identify Update() methods that should use events instead
4. Suggest architectural changes if needed

**Target Platform:**
- [PC / Console / Mobile]
- Target FPS: [60 / 30 / etc.]
```

---

### For Memory Optimization

```
**MEMORY OPTIMIZATION ANALYSIS**

My game's memory usage is [X MB] and growing over time.

**Attached Files:**
- AI_Optimization_Report_[X].txt
- [Scripts with collections/lists/dictionaries]
- ContextualAnalysis_[timestamp].txt

**Memory Issues:**
- [e.g., "Memory grows from 500MB to 2GB over 10 minutes"]
- [e.g., "Garbage collection spikes every few seconds"]
- [e.g., "Suspected memory leak in [system]"]

**What I Need:**
1. Identify potential memory leaks (constantly increasing values)
2. Show me proper cleanup/disposal code
3. Identify collections that need capacity pre-allocation
4. Suggest object pooling where applicable

**Memory Targets:**
- Target memory usage: [X MB]
- Platform: [PC / Console / Mobile]
```

---

## 🔥 QUICK ANALYSIS PROMPTS

### For Rapid Issue Identification

```
**QUICK SCAN - WHAT'S WRONG?**

Attached: AI_Optimization_Report_[X].txt

Just tell me:
1. The #1 issue to fix right now
2. Which script file I should look at
3. One sentence explaining the problem
4. Estimated time to fix (quick/medium/complex)
```

---

### For Pattern Confirmation

```
**PATTERN VERIFICATION**

Attached: ContextualAnalysis_[timestamp].txt

I see alerts about [oscillation/spikes/stuck values] in [FieldName].

Questions:
1. Is this actually a problem or expected behavior?
2. If it's a problem, what's the fix?
3. What threshold/cooldown value should I use?
```

---

### For Before/After Comparison

```
**OPTIMIZATION VERIFICATION**

I implemented your previous suggestions. Here are before/after reports:

**Before:** AI_Optimization_Report_1_[timestamp].txt
**After:** AI_Optimization_Report_2_[timestamp].txt

Questions:
1. Did the optimization work? (compare impact scores)
2. What improved and by how much?
3. Any new issues introduced?
4. What should I optimize next?
```

---

## 💡 PRO TIPS FOR EFFECTIVE PROMPTS

### ✅ DO THIS:

1. **Be Specific About Your Goal**
   - ❌ "Make it faster"
   - ✅ "Reduce animation state changes from 20/sec to <5/sec"

2. **Attach Relevant Files Only**
   - ❌ Attach all 50 files
   - ✅ Attach AI report + 2-3 relevant script logs

3. **Mention What You've Tried**
   - "I already tried adding a cooldown but it didn't help"
   - This saves time on suggestions you've already tested

4. **Specify Your Constraints**
   - "Must maintain 60 FPS on mid-range PC"
   - "Cannot change the animation system architecture"

5. **Ask for Verification Steps**
   - "How do I know this fix worked?"
   - "What values should I see in the next report?"

### ❌ DON'T DO THIS:

1. **Vague Requests**
   - "Something is wrong, fix it"
   - AI needs to know WHAT you're trying to optimize

2. **Attach Everything**
   - Attaching 20 files wastes context
   - Start with AI report, then add specific logs as needed

3. **No Context**
   - "Why is this value changing?"
   - AI needs to know if it's a problem or expected behavior

4. **Ask for Generic Advice**
   - "How do I optimize Unity?"
   - Use the specific data from YOUR game!

---

## 🎯 WORKFLOW EXAMPLES

### Example 1: First-Time Analysis

```
**INITIAL GAME OPTIMIZATION ANALYSIS**

This is my first time using the Cascade Debug System. I played for 5 minutes and generated these files.

**Attached:**
- AI_Optimization_Report_1_[timestamp].txt

**My Game:**
- Third-person shooter with hand animations
- Movement system with sprint/crouch/slide
- Combat with shooting and abilities

**What I Need:**
1. Review the AI report and confirm priorities
2. Tell me which 3 script logs I should attach next for deeper analysis
3. Give me a roadmap: what to fix in what order
4. Estimate total optimization time

**Goal:** Smooth 60 FPS gameplay on mid-range PC
```

---

### Example 2: Targeted Fix

```
**TARGETED FIX - ANIMATION STUTTERING**

The AI report flagged "LayeredHandAnimationController.UpdateMovementAnimations() called every frame" as HIGH priority.

**Attached:**
- AI_Optimization_Report_1.txt (for context)
- LayeredHandAnimationController_[timestamp].txt (the problem script)

**What I Need:**
1. Show me the exact code fix with before/after
2. Explain why this causes stuttering
3. What cooldown value should I use?
4. Any side effects I should watch for?

**Current Behavior:**
- Hands twitch slightly during movement
- Console shows "UpdateMovementAnimations" spam
```

---

### Example 3: Deep Dive Investigation

```
**DEEP DIVE - OSCILLATING VELOCITY**

ContextualAnalysis flagged "AAAMovementController.velocity is Oscillating" but I don't understand why.

**Attached:**
- AAAMovementController_[timestamp].txt (full value history)
- ContextualAnalysis_[timestamp].txt (pattern detection)
- AI_Optimization_Report_1.txt (for context)

**What I Need:**
1. Analyze the velocity value history - what's the pattern?
2. Is this caused by input, physics, or animation?
3. Show me the smoothing/damping solution
4. Should I use Vector3.Lerp, SmoothDamp, or something else?

**Context:**
- Using Character Controller
- Movement in Update(), physics in FixedUpdate()
- Input from new Input System
```

---

## 🚀 ADVANCED PROMPTS

### For Architectural Review

```
**ARCHITECTURE REVIEW REQUEST**

Based on the optimization data, I want to know if my system architecture is fundamentally flawed.

**Attached:**
- AI_Optimization_Report_1.txt
- [All relevant script logs]
- ContextualAnalysis.txt

**Questions:**
1. Are there architectural anti-patterns in my data?
2. Should I refactor before optimizing?
3. What's the "right way" to structure [system]?
4. Will small fixes work or do I need a redesign?

**Current Architecture:**
[Briefly describe your system architecture]

**Willing to Refactor:** [Yes/No - and how much time you have]
```

---

### For Regression Detection

```
**REGRESSION DETECTION**

I made changes and something broke. Help me find what changed.

**Attached:**
- AI_Optimization_Report_BEFORE.txt
- AI_Optimization_Report_AFTER.txt
- [Relevant script logs from both sessions]

**What Changed:**
[List the changes you made]

**New Issues:**
[Describe what broke]

**What I Need:**
1. Compare before/after - what got worse?
2. Which of my changes caused the regression?
3. How do I fix it without reverting everything?
```

---

## 📊 INTERPRETING AI RESPONSES

### What to Expect

**Good AI Response Includes:**
- ✅ Specific line numbers and code snippets
- ✅ Before/after comparisons
- ✅ Explanation of root cause
- ✅ Verification steps
- ✅ Estimated impact

**If AI Response is Vague:**
- Attach more specific script logs
- Provide more context about your architecture
- Ask follow-up questions with examples

---

## 🎯 PROMPT TEMPLATES BY PRIORITY

### For CRITICAL Issues (Fix Immediately)

```
**CRITICAL ISSUE - IMMEDIATE FIX NEEDED**

AI Report flagged CRITICAL: [Issue description]

**Attached:**
- AI_Optimization_Report.txt
- [Affected script log]

**Impact:** [Game is unplayable / Major performance issue / etc.]

**What I Need:**
1. Immediate hotfix code (I'll optimize properly later)
2. Root cause explanation
3. Proper fix for next sprint

**Timeline:** Need fix in next [X hours/days]
```

---

### For HIGH Priority Issues (Fix This Session)

```
**HIGH PRIORITY OPTIMIZATION**

AI Report shows HIGH priority: [Issue description]
Impact Score: [X/10]

**Attached:**
- AI_Optimization_Report.txt
- [Relevant script logs]

**What I Need:**
1. Detailed code fix with explanation
2. Any architectural considerations
3. Testing steps to verify fix

**Timeline:** Fixing today
```

---

### For MEDIUM Priority Issues (Fix This Week)

```
**MEDIUM PRIORITY IMPROVEMENT**

AI Report suggests: [Issue description]

**Attached:**
- AI_Optimization_Report.txt

**What I Need:**
1. Is this worth fixing now or later?
2. If now, show me the fix
3. If later, what's the priority order?

**Timeline:** Planning this week's work
```

---

## 🔥 THE ULTIMATE PROMPT (Use This First!)

```
**COMPREHENSIVE GAME OPTIMIZATION ANALYSIS**

I'm using the Cascade Intelligent Debug System to optimize my Unity game.

**GAME INFO:**
- Type: [Third-person shooter / Platformer / etc.]
- Target: [PC / Console / Mobile]
- Current State: [Playable but laggy / Specific issues / etc.]

**ATTACHED FILES:**
1. AI_Optimization_Report_1_[timestamp].txt (START HERE)
2. ContextualAnalysis_[timestamp].txt (Pattern detection)
3. [Top 3 problematic scripts from AI report]

**MY OPTIMIZATION GOALS:**
1. Primary: [e.g., "Achieve stable 60 FPS"]
2. Secondary: [e.g., "Reduce memory usage"]
3. Tertiary: [e.g., "Improve input responsiveness"]

**WHAT I NEED FROM YOU:**

**Phase 1 - Assessment (Do This First):**
1. Review the AI Optimization Report
2. Confirm the priority rankings (CRITICAL, HIGH, MEDIUM)
3. Identify the #1 issue to fix first
4. Estimate total optimization effort (hours/days)

**Phase 2 - Root Cause Analysis:**
1. Explain WHY each HIGH/CRITICAL issue is happening
2. Reference specific values/patterns from the logs
3. Identify if it's a code issue, architecture issue, or Unity issue

**Phase 3 - Specific Fixes:**
1. Provide exact code changes (before/after snippets)
2. Number fixes in order of impact (1 = highest impact)
3. Include line numbers and file names
4. Explain what each fix does in 1-2 sentences

**Phase 4 - Verification:**
1. Tell me what values should change in the next report
2. Provide specific numbers (e.g., "changes should drop from 20/sec to <5/sec")
3. List any side effects to watch for

**CONSTRAINTS:**
- Focus ONLY on HIGH and CRITICAL issues (ignore MEDIUM for now)
- Provide code that's immediately runnable (no pseudocode)
- Keep explanations concise (I'll ask follow-ups if needed)
- Prioritize fixes by impact, not ease of implementation

**MY TECHNICAL LEVEL:**
[Beginner / Intermediate / Advanced] - adjust explanation depth accordingly

**ADDITIONAL CONTEXT:**
[Add any relevant info: recent changes, known issues, specific concerns, etc.]

Let's start with Phase 1 - review the AI report and tell me what to fix first!
```

---

## 💾 SAVE THIS PROMPT

**File Location:** Keep this in your project root or bookmark it!

**Usage:**
1. Generate debug data (play game for 2-5 minutes)
2. Copy the appropriate prompt template
3. Fill in the bracketed sections
4. Attach the relevant files
5. Send to AI (Cascade)
6. Get specific, actionable fixes!

---

## 🎉 YOU'RE READY!

With these prompts, you'll get:
- ✅ Focused analysis (no wasted context)
- ✅ Specific code fixes (not generic advice)
- ✅ Prioritized action items (fix high-impact first)
- ✅ Verification steps (know when it's fixed)
- ✅ Efficient communication (save time and tokens)

**Copy, paste, fill in, and optimize!** 🚀
