using UnityEngine;

// The contract that all player states must adhere to.
public interface IPlayerState
{
    // Called once when the state becomes active.
    void OnEnter(PlayerMovementManager manager);

    // Called once when the state is deactivated.
    void OnExit();

    // Handles player input (runs in Update).
    void HandleInput();
    
    // Runs every frame (for non-physics logic).
    void Update();

    // Runs on the physics tick.
    void FixedUpdate();
}
