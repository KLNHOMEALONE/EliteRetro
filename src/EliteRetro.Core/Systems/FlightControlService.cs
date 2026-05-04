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
    /// <summary>Speed rate in units/sec. Positive=accelerate, negative=decelerate, smoothly interpolated.</summary>
    public float SpeedDelta;
    /// <summary>View index: 0=front, 1=rear, 2=left, 3=right.</summary>
    public int ViewIndex;
    /// <summary>Laser fire requested this frame.</summary>
    public bool FireLaser;
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
    private float _rollRatePerSec;  // signed, radians/sec
    private float _pitchRatePerSec; // signed, radians/sec
    private float _speedRatePerSec; // signed, units/sec (positive=accel, negative=decel)

    /// <summary>
    /// Process keyboard input and return flight control state.
    /// Called once per frame.
    ///
    /// Controls:
    /// - Left/Right Arrow: roll left/right
    /// - Up/Down Arrow: pitch down/up (inverted: Down = pitch up = pull stick back)
    /// - W/S: speed increase/decrease
    /// - V: view switch (cycles front → rear → left → right)
    /// - Space: fire laser
    /// - P: pause toggle
    /// </summary>
    public FlightControlState Update(GameTime gameTime)
    {
        var state = Keyboard.GetState();
        var control = new FlightControlState();
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (dt <= 0) dt = 1f / 60f;

        if (!_isPaused)
        {
            float targetRoll = 0f;
            if (state.IsKeyDown(Keys.Left)) targetRoll = -1f;
            else if (state.IsKeyDown(Keys.Right)) targetRoll = 1f;

            float targetPitch = 0f;
            if (state.IsKeyDown(Keys.Up)) targetPitch = 1f;
            else if (state.IsKeyDown(Keys.Down)) targetPitch = -1f;

            // Smooth digital controls into a turn-rate (radians/sec).
            // RollAngle/PitchAngle are still expressed as radians-per-frame-at-60fps
            // so existing `dt * 60` scaling in scenes produces radians/sec behavior.
            float maxRollPerSec = GameConstants.RollMax * 60f;
            float maxPitchPerSec = GameConstants.PitchMax * 60f;
            float desiredRollPerSec = targetRoll * maxRollPerSec;
            float desiredPitchPerSec = targetPitch * maxPitchPerSec;

            float rollAccel = maxRollPerSec / MathF.Max(0.001f, GameConstants.TurnRampUpSeconds);
            float rollDecel = maxRollPerSec / MathF.Max(0.001f, GameConstants.TurnRampDownSeconds);
            float pitchAccel = maxPitchPerSec / MathF.Max(0.001f, GameConstants.TurnRampUpSeconds);
            float pitchDecel = maxPitchPerSec / MathF.Max(0.001f, GameConstants.TurnRampDownSeconds);

            _rollRatePerSec = MoveTowards(_rollRatePerSec, desiredRollPerSec,
                (MathF.Abs(desiredRollPerSec) > MathF.Abs(_rollRatePerSec) ? rollAccel : rollDecel) * dt);
            _pitchRatePerSec = MoveTowards(_pitchRatePerSec, desiredPitchPerSec,
                (MathF.Abs(desiredPitchPerSec) > MathF.Abs(_pitchRatePerSec) ? pitchAccel : pitchDecel) * dt);

            if (GameConstants.TurnQuantizationSteps > 0)
            {
                _rollRatePerSec = QuantizeSigned(_rollRatePerSec, maxRollPerSec, GameConstants.TurnQuantizationSteps);
                _pitchRatePerSec = QuantizeSigned(_pitchRatePerSec, maxPitchPerSec, GameConstants.TurnQuantizationSteps);
            }

            control.RollAngle = _rollRatePerSec / 60f;
            control.PitchAngle = _pitchRatePerSec / 60f;

            // Speed: W (increase) and S (decrease) with smooth acceleration/inertia.
            float targetSpeedRate = 0f;
            if (state.IsKeyDown(Keys.W))
                targetSpeedRate = GameConstants.SpeedMax;
            else if (state.IsKeyDown(Keys.S))
                targetSpeedRate = -GameConstants.SpeedMax;

            float speedAccel = GameConstants.SpeedAccel;
            float speedDecel = GameConstants.SpeedDecel;
            _speedRatePerSec = MoveTowards(_speedRatePerSec, targetSpeedRate,
                (MathF.Abs(targetSpeedRate) > MathF.Abs(_speedRatePerSec) ? speedAccel : speedDecel) * dt);

            control.SpeedDelta = _speedRatePerSec;

            // View switching: V key cycles through views
            if (state.IsKeyDown(Keys.V) && !_previousState.IsKeyDown(Keys.V))
            {
                _currentViewIndex = (_currentViewIndex + 1) % 4;
            }
            control.ViewIndex = _currentViewIndex;

            // Laser fire: Space key (continuous fire while held)
            control.FireLaser = state.IsKeyDown(Keys.Space);
        }
        else
        {
            // When paused, avoid accumulating/retaining turn rate.
            _rollRatePerSec = 0f;
            _pitchRatePerSec = 0f;
        }

        // Pause toggle: P key only
        bool currentPause = state.IsKeyDown(Keys.P);
        bool prevPause = _previousState.IsKeyDown(Keys.P);
        if (currentPause && !prevPause)
        {
            _isPaused = !_isPaused;
            control.PauseToggled = true;
        }

        control.IsPaused = _isPaused;
        _previousState = state;
        return control;
    }

    private static float MoveTowards(float current, float target, float maxDelta)
    {
        if (maxDelta <= 0) return current;
        float delta = target - current;
        if (MathF.Abs(delta) <= maxDelta) return target;
        return current + MathF.Sign(delta) * maxDelta;
    }

    private static float QuantizeSigned(float value, float maxAbs, int steps)
    {
        if (steps <= 1 || maxAbs <= 0f) return value;
        float clamped = MathF.Max(-maxAbs, MathF.Min(maxAbs, value));
        float t = (clamped / maxAbs + 1f) * 0.5f; // [-max..max] -> [0..1]
        float q = MathF.Round(t * steps) / steps;
        return (q * 2f - 1f) * maxAbs;
    }

    /// <summary>
    /// Reset to default state.
    /// </summary>
    public void Reset()
    {
        _previousState = default;
        _isPaused = false;
        _currentViewIndex = 0;
        _rollRatePerSec = 0f;
        _pitchRatePerSec = 0f;
    }
}
