using Microsoft.Xna.Framework;
using EliteRetro.Core.Managers;
using EliteRetro.Core.Entities;

namespace EliteRetro.Core.Systems;

/// <summary>
/// Service for managing combat logic, laser fire, and hit detection.
/// </summary>
public interface ICombatService
{
    /// <summary>
    /// Update combat timers (cooldowns, flashes).
    /// </summary>
    void Update();

    /// <summary>
    /// Attempt to fire a laser from the player's current view.
    /// </summary>
    void FireLaser(IGameContext context, IBubbleManager bubbleManager, int viewMode);

    /// <summary>
    /// Frames remaining until next shot allowed.
    /// </summary>
    int LaserCooldown { get; }

    /// <summary>
    /// Frames remaining to show laser beam visual.
    /// </summary>
    int LaserFlashTimer { get; }
}
