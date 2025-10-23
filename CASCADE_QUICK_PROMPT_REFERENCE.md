# ⚡ CASCADE QUICK PROMPT REFERENCE

## 🎯 COPY-PASTE THIS EVERY TIME

```
I need you to analyze my Unity game optimization data from Cascade Debug System.

**FOCUS:** [Animation/Movement/Combat/Performance]
**GOAL:** [Reduce lag/Fix stuttering/Optimize memory]

**ATTACHED FILES:**
- AI_Optimization_Report_[X].txt
- [ScriptName1]_[timestamp].txt
- [ScriptName2]_[timestamp].txt
- ContextualAnalysis_[timestamp].txt

**WHAT I NEED:**
1. Priority Assessment - What's the #1 issue?
2. Root Cause - WHY is it happening?
3. Specific Code Fixes - Show me before/after code
4. Implementation Order - Number fixes by impact
5. Verification - How do I know it worked?

**CONSTRAINTS:**
- Focus ONLY on HIGH/CRITICAL issues
- Provide immediately runnable code
- Keep explanations concise

**CONTEXT:**
[Your specific situation/recent changes/issues noticed]
```

---

## 🔥 ULTRA-SHORT VERSION (When You're in a Hurry)

```
Analyze this AI report and tell me:
1. #1 issue to fix
2. Which script to look at
3. The exact code fix
4. What values should change after

Attached: AI_Optimization_Report_[X].txt
```

---

## 📋 FILE ATTACHMENT GUIDE

### Always Attach:
- ✅ AI_Optimization_Report_[X].txt (START HERE!)

### Conditionally Attach:
- ✅ ContextualAnalysis.txt (if patterns/anomalies mentioned)
- ✅ Specific script logs (only the 2-3 mentioned in AI report)

### Don't Attach:
- ❌ All script logs at once (wastes context)
- ❌ Classic debug logs (unless specifically needed)

---

## 🎯 PROMPT SELECTION FLOWCHART

```
START HERE
    ↓
Is this your first analysis?
    ├─ YES → Use "ULTIMATE PROMPT" (comprehensive)
    └─ NO → Continue
         ↓
Do you know what's wrong?
    ├─ YES → Use "TARGETED FIX" prompt
    └─ NO → Use "QUICK SCAN" prompt
         ↓
After getting initial analysis:
    ↓
Need deep dive on specific issue?
    └─ Use "DEEP DIVE INVESTIGATION" prompt
```

---

## 💡 FILL-IN-THE-BLANK TEMPLATE

```
**[SYSTEM NAME] OPTIMIZATION**

Issue: [Describe what's wrong in 1 sentence]

**Attached:**
- AI_Optimization_Report_[X].txt
- [RelevantScript]_[timestamp].txt

**What I Need:**
1. [Specific question 1]
2. [Specific question 2]
3. [Specific question 3]

**Context:** [Any relevant info]
```

---

## 🚀 COMMON SCENARIOS

### Scenario 1: "Something is laggy"
```
**PERFORMANCE LAG ANALYSIS**

My game lags during [specific action].

Attached: AI_Optimization_Report_1.txt

What's causing the lag and how do I fix it?
```

---

### Scenario 2: "Animation is broken"
```
**ANIMATION ISSUE**

[Specific animation problem description]

Attached:
- AI_Optimization_Report_1.txt
- [AnimationController]_[timestamp].txt

Show me the fix with code.
```

---

### Scenario 3: "Did my fix work?"
```
**VERIFICATION CHECK**

I implemented [your previous fix].

Attached:
- BEFORE: AI_Optimization_Report_1.txt
- AFTER: AI_Optimization_Report_2.txt

Did it work? What improved?
```

---

## 🎯 KEY PHRASES TO USE

### For Focused Analysis:
- "Focus ONLY on HIGH/CRITICAL issues"
- "Ignore MEDIUM/LOW priority for now"
- "Start with the #1 highest impact issue"

### For Specific Fixes:
- "Show me exact code with line numbers"
- "Provide before/after code snippets"
- "Give me immediately runnable code"

### For Verification:
- "What values should change?"
- "How do I verify this worked?"
- "What should I see in the next report?"

### For Efficiency:
- "Keep explanations concise"
- "I'll ask follow-ups if needed"
- "Just the facts, no fluff"

---

## ⚠️ AVOID THESE PHRASES

❌ "Make it better" (too vague)
❌ "Optimize everything" (too broad)
❌ "What's wrong?" (attach AI report first!)
❌ "Fix all issues" (prioritize instead)

---

## 📊 EXPECTED AI RESPONSE FORMAT

**Good Response Includes:**
```
1. PRIORITY ASSESSMENT
   - #1 Issue: [Specific issue]
   - Impact: [X/10]
   - Affected Component: [ScriptName]

2. ROOT CAUSE
   - [2-3 sentence explanation]
   - Evidence: [Reference to log values]

3. CODE FIX
   Before:
   [code snippet]
   
   After:
   [code snippet]
   
   Explanation: [What this does]

4. VERIFICATION
   - Expected change: [Specific metric]
   - Watch for: [Potential side effects]
```

---

## 🔥 POWER USER TIPS

### Tip 1: Chain Prompts
```
First prompt: "What's the #1 issue?"
Second prompt: "Show me the fix for that"
Third prompt: "Any side effects to watch for?"
```

### Tip 2: Reference Previous Fixes
```
"You previously suggested [X]. I implemented it. 
Now the AI report shows [Y]. Is this expected?"
```

### Tip 3: Ask for Alternatives
```
"You suggested [solution A]. 
Is there a simpler alternative that's 80% as effective?"
```

### Tip 4: Request Explanations
```
"I don't understand why [X] causes [Y]. 
Explain in simple terms."
```

---

## 📁 FILE NAMING QUICK REFERENCE

```
CASCADE_DEBUG_EXPORTS/
├── IntelligentLogs/
│   └── [ScriptName]_[YYYYMMDD_HHMMSS].txt
│
├── ContextualTracking/
│   └── ContextualAnalysis_[YYYYMMDD_HHMMSS].txt
│
└── OptimizationReports/
    └── AI_Optimization_Report_[N]_[YYYYMMDD_HHMMSS].txt
```

**When referencing files in prompts:**
- Use full filename with timestamp
- Or use [X] as placeholder: "Report_[X].txt"

---

## 🎯 THE 3-SENTENCE PROMPT (Absolute Minimum)

```
Analyze AI_Optimization_Report_1.txt.
Tell me the #1 issue and the exact code fix.
What values should change after?
```

---

## 💾 BOOKMARK THIS PAGE!

Keep this open while optimizing. Copy-paste prompts as needed.

**Remember:**
- ✅ Attach AI report first
- ✅ Be specific about your goal
- ✅ Ask for exact code fixes
- ✅ Request verification steps
- ✅ Focus on HIGH/CRITICAL only

**Result:** Maximum value, minimum context waste! 🚀
