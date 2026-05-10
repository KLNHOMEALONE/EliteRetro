using Microsoft.Xna.Framework.Input;

namespace EliteRetro.Core.Input;

/// <summary>
/// Service for centralized input handling, providing state snapshots and edge detection.
/// </summary>
public interface IInputService
{
    /// <summary>
    /// Update input state. Must be called once per frame.
    /// </summary>
    void Update();

    /// <summary>
    /// Returns true if the key is currently held down.
    /// </summary>
    bool IsKeyDown(Keys key);

    /// <summary>
    /// Returns true if the key is currently up.
    /// </summary>
    bool IsKeyUp(Keys key);

    /// <summary>
    /// Returns true only on the frame the key was first pressed (rising edge).
    /// </summary>
    bool IsKeyPressed(Keys key);

    /// <summary>
    /// Returns true only on the frame the key was released (falling edge).
    /// </summary>
    bool IsKeyReleased(Keys key);

    /// <summary>
    /// Gets the current full keyboard state.
    /// </summary>
    KeyboardState GetState();
}
