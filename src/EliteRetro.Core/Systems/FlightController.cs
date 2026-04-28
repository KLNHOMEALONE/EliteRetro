using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace EliteRetro.Core.Systems;

/// <summary>
/// Translates keyboard input into pitch and roll angles for the
/// Minsky universe rotation system.
/// </summary>
public class FlightController
{
    private KeyboardState _previousState;

    /// <summary>
    /// Current roll angle (alpha). Positive = roll right (Q), negative = roll left (W).
    /// Range: -RollMax to +RollMax.
    /// </summary>
    public float RollAngle { get; private set; }

    /// <summary>
    /// Current pitch angle (beta). Positive = pitch up, negative = pitch down.
    /// Range: -PitchMax to +PitchMax.
    /// </summary>
    public float PitchAngle { get; private set; }

    /// <summary>
    /// Current view index: 0=front, 1=rear, 2=left, 3=right.
    /// </summary>
    public int ViewIndex { get; private set; }

    /// <summary>
    /// Player is pausing flight input.
    /// </summary>
    public bool IsPaused { get; private set; }

    /// <summary>
    /// Process keyboard input and update pitch/roll angles.
    /// Called once per frame.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        var state = Keyboard.GetState();
        RollAngle = 0;
        PitchAngle = 0;

        if (!IsPaused)
        {
            // Roll: Q (left/negative) and W (right/positive)
            if (state.IsKeyDown(Keys.Q))
                RollAngle = -GameConstants.RollMax;
            else if (state.IsKeyDown(Keys.W))
                RollAngle = GameConstants.RollMax;

            // Pitch: Up (positive) and Down (negative)
            if (state.IsKeyDown(Keys.Up))
                PitchAngle = GameConstants.PitchMax;
            else if (state.IsKeyDown(Keys.Down))
                PitchAngle = -GameConstants.PitchMax;

            // View switching: V key cycles through views
            if (state.IsKeyDown(Keys.V) && !_previousState.IsKeyDown(Keys.V))
                ViewIndex = (ViewIndex + 1) % 4;
        }

        // Pause toggle: P key
        if (state.IsKeyDown(Keys.P) && !_previousState.IsKeyDown(Keys.P))
            IsPaused = !IsPaused;

        _previousState = state;
    }

    /// <summary>
    /// Get the view axis flip factors for the current view.
    /// Returns (flipNose, flipSide) where true means invert that axis.
    /// </summary>
    public (bool flipNose, bool flipSide) GetViewFlips()
    {
        return ViewIndex switch
        {
            0 => (false, false),  // Front view
            1 => (true, false),   // Rear view (invert nose)
            2 => (false, true),   // Left view (invert side)
            3 => (false, false),  // Right view
            _ => (false, false)
        };
    }

    /// <summary>
    /// Reset to default state.
    /// </summary>
    public void Reset()
    {
        RollAngle = 0;
        PitchAngle = 0;
        ViewIndex = 0;
        IsPaused = false;
        _previousState = default;
    }
}
