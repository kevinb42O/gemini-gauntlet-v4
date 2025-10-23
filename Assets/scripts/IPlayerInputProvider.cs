using UnityEngine;

/// <summary>
/// COHERENCE CONTRACT: Input abstraction for crouch/slide/dive systems.
/// Ensures compile-time safety and single source of truth for input keys.
/// </summary>
public interface IPlayerInputProvider
{
    bool GetCrouchPressed();
    bool GetCrouchHeld();
    bool GetDivePressed();
    float GetHorizontalRaw();
    float GetVerticalRaw();
}

/// <summary>
/// Default implementation using Controls static class
/// </summary>
public class DefaultPlayerInput : IPlayerInputProvider
{
    public bool GetCrouchPressed() => Input.GetKeyDown(Controls.Crouch);
    public bool GetCrouchHeld() => Input.GetKey(Controls.Crouch);
    public bool GetDivePressed() => Input.GetKeyDown(Controls.Dive);
    public float GetHorizontalRaw() => Controls.HorizontalRaw();
    public float GetVerticalRaw() => Controls.VerticalRaw();
}
