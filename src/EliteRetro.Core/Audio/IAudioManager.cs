namespace EliteRetro.Core.Audio;

/// <summary>
/// Interface for the procedural audio system.
/// </summary>
public interface IAudioManager : IDisposable
{
    bool IsInitialized { get; }

    /// <summary>
    /// Initialize audio system.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Play menu selection sound.
    /// </summary>
    void PlayMenuSelect();

    /// <summary>
    /// Play laser shot sound.
    /// </summary>
    void PlayLaser();

    /// <summary>
    /// Play laser hit sound.
    /// </summary>
    void PlayLaserHit();

    /// <summary>
    /// Play explosion sound.
    /// </summary>
    void PlayExplosion();
}
