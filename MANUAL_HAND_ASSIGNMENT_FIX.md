# 🔧 MANUAL HAND ASSIGNMENT - INSTANT FIX

## The Problem
Left hand visuals not registering automatically.

## The Solution
**Manual assignment fields** - drag and drop your hands directly!

---

## 🎯 How to Fix (30 seconds)

### Step 1: Find Your PlayerOverheatManager
- In Hierarchy, find the GameObject with `PlayerOverheatManager` component
- Select it

### Step 2: Find the New Section
In Inspector, scroll to find:
```
► Manual Hand Visual Assignment
   Manual Primary Hand Visuals    [None]
   Manual Secondary Hand Visuals  [None]
```

### Step 3: Assign Your Hands

**For LEFT hand (Primary):**
1. In Hierarchy, find your LEFT hand GameObject (the one with `HandOverheatVisuals` where `isPrimary = TRUE`)
2. Drag it into **"Manual Primary Hand Visuals"** field

**For RIGHT hand (Secondary):**
1. In Hierarchy, find your RIGHT hand GameObject (the one with `HandOverheatVisuals` where `isPrimary = FALSE`)
2. Drag it into **"Manual Secondary Hand Visuals"** field

### Step 4: Test
- Enter Play Mode
- Fire both hands
- Both should work! ✅

---

## 📋 Visual Guide

```
PlayerOverheatManager (Inspector)
├─ Sound Events: [Your SoundEvents asset]
├─ Global Heat Settings: ...
├─ Manual Hand Visual Assignment:
│  ├─ Manual Primary Hand Visuals:   [Drag LEFT hand here]  ← isPrimary=TRUE
│  └─ Manual Secondary Hand Visuals: [Drag RIGHT hand here] ← isPrimary=FALSE
└─ ...
```

---

## ✅ That's It!

The manual assignment **overrides** the automatic registration, so even if auto-registration fails, your hands will work.

**Both hands will now show overheat effects correctly!** 🔥

---

*No more diagnostic bullshit. Just drag and drop.* 😎
