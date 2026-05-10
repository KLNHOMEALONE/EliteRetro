using EliteRetro.Core.Managers;

namespace EliteRetro.Core.Systems;

/// <summary>
/// Service responsible for the rotating universe simulation.
/// Strictly encapsulates the existing rotation and relative movement logic.
/// </summary>
public interface IWorldSimulationService : IDisposable
{
    /// <summary>
    /// Update the universe simulation state.
    /// </summary>
    void Update(IBubbleManager bubbleManager, float playerSpeed, float rollDelta, float pitchDelta, float moveStep);

    /// <summary>
    /// Enforce a minimum radial distance from a celestial body.
    /// </summary>
    void EnforceOverflyDistance(Entities.ShipInstance? body, float bodyRadius);

    /// <summary>
    /// Check for player collision with the planet surface.
    /// </summary>
    void CheckPlanetCollision(IGameContext context, IBubbleManager bubbleManager, ref float playerSpeed, ref bool planetHit, ref int damageFlashTimer);
}
