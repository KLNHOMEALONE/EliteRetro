namespace EliteRetro.Core.Entities;

/// <summary>
/// Static ship blueprint: model geometry + gameplay characteristics.
/// One blueprint per ship type, shared across all instances.
/// </summary>
public class ShipBlueprint
{
    /// <summary>Ship type name (e.g. "Sidewinder").</summary>
    public string Name { get; init; } = "";

    /// <summary>Wireframe model definition.</summary>
    public ShipModel Model { get; init; } = new();

    /// <summary>Bounty in credits (0 = no bounty).</summary>
    public int Bounty { get; init; }

    /// <summary>Maximum speed (units per frame).</summary>
    public float MaxSpeed { get; init; }

    /// <summary>Energy capacity (0-255).</summary>
    public byte MaxEnergy { get; init; }

    /// <summary>Cargo capacity (number of tons).</summary>
    public int CargoCapacity { get; init; }

    /// <summary>Hull strength (0-255).</summary>
    public byte HullStrength { get; init; }

    /// <summary>Shield strength (0-255).</summary>
    public byte ShieldStrength { get; init; }

    /// <summary>Laser power (0 = none, 1-4 = beam/military/pulse/mining).</summary>
    public byte LaserPower { get; init; }

    /// <summary>Ship class: 0=innocent, 1=bounty hunter, 2=pirate, 3=cop.</summary>
    public byte ShipClass { get; init; }
}

/// <summary>
/// Ship class classification for AI behavior.
/// </summary>
public enum ShipClass : byte
{
    Innocent = 0,
    BountyHunter = 1,
    Pirate = 2,
    Cop = 3
}
