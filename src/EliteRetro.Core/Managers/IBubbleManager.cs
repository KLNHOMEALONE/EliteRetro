using EliteRetro.Core.Entities;
using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Managers;

/// <summary>
/// Interface for managing entities within the player's local bubble.
/// </summary>
public interface IBubbleManager
{
    /// <summary>Raised when an entity is spawned or despawned.</summary>
    event EventHandler<EntityEventArgs>? EntityEvent;

    /// <summary>Raised when the player ship collides with another entity.</summary>
    event EventHandler<CollisionEventArgs>? CollisionEvent;

    /// <summary>Planet entity (always in slot 0).</summary>
    ShipInstance? Planet { get; }

    /// <summary>Sun or station entity (slot 1, mutually exclusive).</summary>
    ShipInstance? SunOrStation { get; }

    /// <summary>Player ship entity (slot 2, always present).</summary>
    ShipInstance? PlayerShip { get; }

    void SetSlot(int index, ShipInstance? entity);
    ShipInstance? GetSlot(int index);
    bool TrySpawn(ShipInstance ship);
    void Despawn(int slotIndex, string reason = "despawned");
    void Despawn(ShipInstance ship);
    void CullBeyondBubble();
    bool IsInSafeZone();
    void SpawnStation(ShipBlueprint stationBlueprint);
    IEnumerable<ShipInstance> GetActiveShips();
    IEnumerable<ShipInstance> GetAllActive();
    void TidyAllActive();
    void TidyOne();
    void ApplyUniverseRotation(float alpha, float beta);
    void CleanupExpired();
    void Clear();
    void RaiseCollision(string otherShipName);
}
