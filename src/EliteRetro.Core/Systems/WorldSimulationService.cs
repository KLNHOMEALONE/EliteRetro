using EliteRetro.Core.Managers;

namespace EliteRetro.Core.Systems;

/// <summary>
/// Implementation of IWorldSimulationService.
/// VERBATIM preservation of the working universe simulation logic.
/// </summary>
public class WorldSimulationService : IWorldSimulationService
{
    private bool _disposed;

    public void Update(IBubbleManager bubbleManager, float playerSpeed, float rollDelta, float pitchDelta, float moveStep)
    {
        // VERBATIM: Rotate universe
        bubbleManager.ApplyUniverseRotation(-rollDelta, -pitchDelta);

        // VERBATIM: Move universe
        foreach (var entity in bubbleManager.GetAllActive())
        {
            if (entity.SlotIndex == GameConstants.PlayerSlot) continue;
            
            // Forward motion brings objects closer along -Z
            entity.Position.Z -= moveStep;

            // Entity's own forward motion
            if (entity.Speed != 0) 
                entity.MoveForward();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}
