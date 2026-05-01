namespace EliteRetro.Core.Entities;

/// <summary>
/// Runtime entity instance in the local bubble.
/// Wraps position, velocity, orientation, and state for any entity
/// (ship, planet, sun, missile, cargo canister).
/// </summary>
public class EntityInstance
{
    /// <summary>Position in world coordinates.</summary>
    public Vector3 Position;

    /// <summary>Velocity in world coordinates.</summary>
    public Vector3 Velocity;

    /// <summary>Orientation matrix (nosev/roofv/sidev).</summary>
    public OrientationMatrix Orientation;

    /// <summary>Speed scalar (units per frame).</summary>
    public float Speed;

    /// <summary>Current energy level (0-255).</summary>
    public byte Energy;

    /// <summary>Slot index in the local bubble (0 = planet, 1 = sun/station, 2+ = ships/etc).</summary>
    public int SlotIndex;

    /// <summary>Entity is alive and active.</summary>
    public bool IsActive;

    /// <summary>Entity is hostile to player.</summary>
    public bool IsHostile;

    /// <summary>AI aggression level (0-63).</summary>
    public byte Aggression;

    /// <summary>Reference to the static ship blueprint (null for planet/sun).</summary>
    public ShipModel? Model;

    /// <summary>Entity type identifier.</summary>
    public EntityType Type;

    public EntityInstance(Vector3 position, EntityType type = EntityType.Ship)
    {
        Position = position;
        Type = type;
        Orientation = OrientationMatrix.Identity;
        IsActive = true;
        SlotIndex = -1;
    }

    /// <summary>
    /// Move entity forward along its nose vector by current speed.
    /// </summary>
    public void MoveForward()
    {
        Position += Orientation.Nosev * Speed;
    }

    /// <summary>
    /// Apply Minsky universe rotation to this entity's position and orientation.
    /// Uses the authentic Elite per-component Minsky algorithm applied
    /// to both position and orientation vectors in sync.
    ///
    /// Combined roll-then-pitch formula:
    ///   K2 = y - alpha * x
    ///   z  = z + beta * K2
    ///   y  = K2 - beta * z       (uses updated z)
    ///   x  = x + alpha * y       (uses updated y)
    ///
    /// See: https://elite.bbcelite.com/deep_dives/rotating_the_universe.html
    /// </summary>
    public void ApplyUniverseRotation(float alpha, float beta)
    {
        // Rotate position using per-component Minsky
        float k2 = Position.Y - alpha * Position.X;
        Position.Z = Position.Z + beta * k2;
        Position.Y = k2 - beta * Position.Z;
        Position.X = Position.X + alpha * Position.Y;

        // Rotate orientation vectors using same per-component Minsky
        Orientation.ApplyMinskyRotation(alpha, beta);
    }

    /// <summary>
    /// Distance squared to another entity (avoids sqrt for comparisons).
    /// </summary>
    public float DistanceSquaredTo(EntityInstance other)
    {
        Vector3 diff = Position - other.Position;
        return diff.LengthSquared();
    }

    /// <summary>
    /// Distance to another entity.
    /// </summary>
    public float DistanceTo(EntityInstance other)
    {
        return Vector3.Distance(Position, other.Position);
    }
}

/// <summary>
/// Entity type classification.
/// </summary>
public enum EntityType
{
    Planet,
    Sun,
    Station,
    Ship,
    Missile,
    Cargo
}
