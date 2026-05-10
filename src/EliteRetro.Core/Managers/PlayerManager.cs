using EliteRetro.Core.Entities;
using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Managers;

/// <summary>
/// Manages the player's state, including commander data, ship instance,
/// and runtime vitals (shields, missiles).
/// </summary>
public class PlayerManager
{
    /// <summary>Persistent commander data (credits, rank, cargo, fuel).</summary>
    public CommanderData Commander { get; } = new();

    /// <summary>The player's ship instance in the local bubble.</summary>
    public ShipInstance Ship { get; }

    /// <summary>Player missiles remaining.</summary>
    public byte Missiles { get; set; } = 4;

    /// <summary>Player shield strength (0-255, front).</summary>
    public byte ShieldFront { get; set; } = 200;

    /// <summary>Player shield strength (0-255, aft).</summary>
    public byte ShieldAft { get; set; } = 200;

    /// <summary>When true, no ships spawn via scheduler or random spawning.</summary>
    public bool TargetPracticeMode { get; set; }

    public PlayerManager()
    {
        // Create player ship blueprint
        var playerBlueprint = new ShipBlueprint
        {
            Name = "Player",
            Model = CobraMk3Model.Create(24),
            MaxSpeed = GameConstants.SpeedMax,
            MaxEnergy = 255,
            HullStrength = 255,
            ShieldStrength = 255,
            LaserPower = 2,
            ShipClass = (byte)NewbFlags.None,
        };

        // Initialize player ship instance at origin
        Ship = new ShipInstance(playerBlueprint)
        {
            Position = Vector3.Zero,
            Speed = 0,
            Energy = 200,
            Hull = 255,
            SlotIndex = GameConstants.PlayerSlot,
            IsActive = true,
        };
    }
}
