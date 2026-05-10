using Microsoft.Xna.Framework.Input;

namespace EliteRetro.Core.Input;

/// <summary>
/// Implementation of IInputService using MonoGame's Keyboard class.
/// </summary>
public class InputService : IInputService
{
    private KeyboardState _currentState;
    private KeyboardState _previousState;

    public void Update()
    {
        _previousState = _currentState;
        _currentState = Keyboard.GetState();
    }

    public bool IsKeyDown(Keys key) => _currentState.IsKeyDown(key);

    public bool IsKeyUp(Keys key) => _currentState.IsKeyUp(key);

    public bool IsKeyPressed(Keys key) => _currentState.IsKeyDown(key) && _previousState.IsKeyUp(key);

    public bool IsKeyReleased(Keys key) => _currentState.IsKeyUp(key) && _previousState.IsKeyDown(key);

    public KeyboardState GetState() => _currentState;
}
