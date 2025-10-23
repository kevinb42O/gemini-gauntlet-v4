// LEGACY FILE - USE UnifiedSlot.cs INSTEAD
// This file is kept for Unity Inspector compatibility
// All new functionality is in UnifiedSlot.cs

using UnityEngine;

/// <summary>
/// Legacy StashSlot - replaced by UnifiedSlot.cs
/// Kept for Unity Inspector compatibility only
/// </summary>
public class StashSlot : UnifiedSlot
{
    // Legacy compatibility methods
    public bool IsGemSlot => false; // Regular stash slots are never gem slots
    public bool HasItem => !IsEmpty;
    public bool isGemSlot => false; // Property version for legacy code
}
