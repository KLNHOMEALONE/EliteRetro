namespace EliteRetro.Core.Entities;

/// <summary>
/// Lightweight runtime ship instance in the local bubble.
/// Wraps a ShipBlueprint with mutable state: position, orientation,
/// speed, flags, and AI state. Designed to match the original Elite's
/// 36-byte ship data block.
/// </summary>
public class ShipInstance
{
    /// <summary>Reference to the static blueprint.</summary>
    public ShipBlueprint Blueprint { get; init; }

    /// <summary>Position in world coordinates.</summary>
    public Vector3 Position;

    /// <summary>Velocity in world coordinates.</summary>
    public Vector3 Velocity;

    /// <summary>Orientation matrix (nosev/roofv/sidev).</summary>
    public OrientationMatrix Orientation;

    /// <summary>Current speed (0 to MaxSpeed).</summary>
    public float Speed;

    /// <summary>Current energy (0-255).</summary>
    public byte Energy;

    /// <summary>Current hull integrity (0-255).</summary>
    public byte Hull;

    /// <summary>Slot index in the local bubble.</summary>
    public int SlotIndex;

    /// <summary>Entity is alive and active.</summary>
    public bool IsActive;

    /// <summary>Missiles locked on this ship.</summary>
    public bool IsMissileTarget;

    /// <summary>Ship is firing lasers.</summary>
    public bool IsFiring;

    /// <summary>AI state: 0=idle, 1=patrol, 2=chase, 3=flee, 4=dock.</summary>
    public byte AIState;

    /// <summary>Aggression level (0-63). Higher = more likely to attack.</summary>
    public byte Aggression;

    /// <summary>Target slot index (-1 = no target).</summary>
    public int TargetSlot;

    /// <summary>Cargo carried (by commodity index).</summary>
    public Dictionary<int, int> Cargo;

    /// <summary>Rotation timer for TIDY round-robin.</summary>
    public int TidyCounter;

    public ShipInstance(ShipBlueprint blueprint)
    {
        Blueprint = blueprint;
        Orientation = OrientationMatrix.Identity;
        IsActive = true;
        Hull = blueprint.HullStrength;
        Energy = blueprint.MaxEnergy;
        Speed = blueprint.MaxSpeed * 0.5f;
        SlotIndex = -1;
        TargetSlot = -1;
        Cargo = new Dictionary<int, int>();
    }

    /// <summary>
    /// Move forward along nose vector by current speed.
    /// </summary>
    public void MoveForward()
    {
        Position += Orientation.Nosev * Speed;
    }

    /// <summary>
    /// Apply Minsky universe rotation to position and orientation.
    /// </summary>
    public void ApplyUniverseRotation(float alpha, float beta)
    {
        Position = Orientation.InverseTransform(Position);
        Orientation.ApplyUniverseRotation(alpha, beta);
        Position = Orientation.Transform(Position);
    }

    /// <summary>
    /// Distance to another ship instance.
    /// </summary>
    public float DistanceTo(ShipInstance other)
    {
        return Vector3.Distance(Position, other.Position);
    }

    /// <summary>
    /// Distance squared (avoids sqrt for comparisons).
    /// </summary>
    public float DistanceSquaredTo(ShipInstance other)
    {
        Vector3 diff = Position - other.Position;
        return diff.LengthSquared();
    }

    /// <summary>
    /// Take damage: reduce hull, check destruction.
    /// </summary>
    public bool TakeDamage(int amount)
    {
        if (Hull <= 0) return true; // already destroyed
        Hull = (byte)Math.Max(0, Hull - amount);
        return Hull <= 0;
    }

    /// <summary>
    /// Add cargo of a given commodity type.
    /// </summary>
    public void AddCargo(int commodityIndex, int amount = 1)
    {
        if (Cargo.TryGetValue(commodityIndex, out int current))
            Cargo[commodityIndex] = current + amount;
        else
            Cargo[commodityIndex] = amount;
    }

    /// <summary>
    /// Face a target position by aligning nose vector.
    /// </summary>
    public void FaceTarget(Vector3 targetPos)
    {
        Vector3 direction = Vector3.Normalize(targetPos - Position);
        Orientation.Nosev = direction;
        // Recompute sidev and roofv to maintain orthonormality
        Vector3 up = Vector3.UnitY;
        Orientation.Sidev = Vector3.Normalize(Vector3.Cross(direction, up));
        Orientation.Roofv = Vector3.Cross(Orientation.Sidev, direction);
    }
}
