using EliteRetro.Core.Scenes;

namespace EliteRetro.Core;

/// <summary>
/// Interface for game-level operations and metadata access, 
/// decoupling scenes from the concrete GameInstance.
/// </summary>
public interface IGameContext
{
    /// <summary>Exit the application.</summary>
    void Exit();

    /// <summary>Replace the current scene stack with a new scene.</summary>
    void ChangeScene(GameScene scene);

    /// <summary>Push a new scene onto the stack.</summary>
    void PushScene(GameScene scene);

    /// <summary>Pop the top scene from the stack.</summary>
    void PopScene();

    /// <summary>When true, all rendered objects use white color.</summary>
    bool DrawWhite { get; set; }

    /// <summary>When true, hidden/invisible edges are drawn.</summary>
    bool DrawInvisible { get; set; }

    /// <summary>Centralized input service.</summary>
    Input.IInputService Input { get; }

    /// <summary>Player state manager.</summary>
    Managers.IPlayerManager PlayerManager { get; }

    /// <summary>World/entity manager.</summary>
    Managers.IBubbleManager BubbleManager { get; }

    /// <summary>Procedural audio manager.</summary>
    Audio.IAudioManager Audio { get; }

    /// <summary>Explosion effect service.</summary>
    Systems.IExplosionService Explosions { get; }

    /// <summary>HUD state calculation service.</summary>
    Systems.IHudService Hud { get; }

    /// <summary>Starfield (stardust) effect service.</summary>
    Systems.IStardustService Stardust { get; }
}
