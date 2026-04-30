using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace EliteRetro.Core.Systems;

/// <summary>
/// Flight control state produced by input processing.
/// </summary>
public struct FlightControlState
{
    /// <summary>Roll angle in radians per frame (at 60fps). Positive = roll right.</summary>
    public float RollAngle;
    /// <summary>Pitch angle in radians per frame (at 60fps). Positive = pitch up.</summary>
    public float PitchAngle;
    /// <summary>Speed delta: +1 (accelerate), -1 (decelerate), 0 (none).</summary>
    public float SpeedDelta;
    /// <summary>View index: 0=front, 1=rear, 2=left, 3=right.</summary>
    public int ViewIndex;
    /// <summary>Pause toggle requested this frame.</summary>
    public bool PauseToggled;
    /// <summary>Whether the service is currently paused.</summary>
    public bool IsPaused;
}

/// <summary>
/// Processes keyboard input into flight control commands.
/// Provides consistent controls across all scenes that use flight input.
/// </summary>
public class FlightControlService
{
    private KeyboardState _previousState;
    private bool _isPaused;
    private int _currentViewIndex; // persistent view index across frames

    /// <summary>
    /// Process keyboard input and return flight control state.
    /// Called once per frame.
    ///
    /// Controls:
    /// - Left/Right Arrow: roll left/right
    /// - Up/Down Arrow: pitch down/up (inverted: Down = pitch up = pull stick back)
    /// - W/S: speed increase/decrease
    /// - V: view switch (cycles front → rear → left → right)
    /// - P or Space: pause toggle
    /// </summary>
    public FlightControlState Update(GameTime gameTime)
    {
        var state = Keyboard.GetState();
        var control = new FlightControlState();

        if (!_isPaused)
        {
            // Roll: Left Arrow (negative) and Right Arrow (positive)
            if (state.IsKeyDown(Keys.Left))
                control.RollAngle = -GameConstants.RollMax;
            else if (state.IsKeyDown(Keys.Right))
                control.RollAngle = GameConstants.RollMax;

            // Pitch: Up Arrow (negative = nose down) and Down Arrow (positive = nose up)
            // Inverted: Down = pull stick back = pitch up
            if (state.IsKeyDown(Keys.Up))
                control.PitchAngle = -GameConstants.PitchMax;
            else if (state.IsKeyDown(Keys.Down))
                control.PitchAngle = GameConstants.PitchMax;

            // Speed: W (increase) and S (decrease)
            if (state.IsKeyDown(Keys.W))
                control.SpeedDelta = 1f;
            else if (state.IsKeyDown(Keys.S))
                control.SpeedDelta = -1f;

            // View switching: V key cycles through views
            if (state.IsKeyDown(Keys.V) && !_previousState.IsKeyDown(Keys.V))
            {
                _currentViewIndex = (_currentViewIndex + 1) % 4;
            }
            control.ViewIndex = _currentViewIndex;
        }

        // Pause toggle: P key or Space
        bool currentPause = state.IsKeyDown(Keys.P) || state.IsKeyDown(Keys.Space);
        bool prevPause = _previousState.IsKeyDown(Keys.P) || _previousState.IsKeyDown(Keys.Space);
        if (currentPause && !prevPause)
        {
            _isPaused = !_isPaused;
            control.PauseToggled = true;
        }

        control.IsPaused = _isPaused;
        _previousState = state;
        return control;
    }

    /// <summary>
    /// Reset to default state.
    /// </summary>
    public void Reset()
    {
        _previousState = default;
        _isPaused = false;
    }
}
