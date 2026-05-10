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

    public void EnforceOverflyDistance(Entities.ShipInstance? body, float bodyRadius)
    {
        if (body == null) return;
        Microsoft.Xna.Framework.Vector3 pos = body.Position;
        float dist = pos.Length();
        if (dist < 1f) return;
        float noseDot = pos.Z / dist;
        float angleFromNose = MathF.Acos(Microsoft.Xna.Framework.MathHelper.Clamp(noseDot, -1f, 1f));
        float sinAngle = MathF.Sin(angleFromNose);
        float minDist = bodyRadius + bodyRadius * 0.4f * sinAngle;
        if (dist < minDist) body.Position = (pos / dist) * minDist;
    }

    public void CheckPlanetCollision(IGameContext context, IBubbleManager bubbleManager, ref float playerSpeed, ref bool planetHit, ref int damageFlashTimer)
    {
        var player = bubbleManager.PlayerShip;
        var planet = bubbleManager.Planet;
        if (player != null && planet != null)
        {
            var col = CollisionSystem.CheckPlanetCollision(player, planet);
            if (col.Type == CollisionSystem.PlanetCollisionType.Crash)
            {
                context.Messages.Post("PLANET HIT", MessageType.General, int.MaxValue);
                playerSpeed = 0f;
                planetHit = true;
                return;
            }
            else if (col.Type == CollisionSystem.PlanetCollisionType.Glancing)
            {
                context.Messages.Post("ALTITUDE CRITICAL - SCRAPE!", MessageType.General, 60);
                player.TakeDamage(15);
                damageFlashTimer = 20;
                planet.Position -= col.PushBack;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}
