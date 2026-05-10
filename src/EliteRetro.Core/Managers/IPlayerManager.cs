using EliteRetro.Core.Entities;

namespace EliteRetro.Core.Managers;

/// <summary>
/// Interface for managing player state, vitals, and commander data.
/// </summary>
public interface IPlayerManager
{
    /// <summary>Persistent commander data (credits, rank, cargo, fuel).</summary>
    CommanderData Commander { get; }

    /// <summary>The player's ship instance in the local bubble.</summary>
    ShipInstance Ship { get; }

    /// <summary>Player missiles remaining.</summary>
    byte Missiles { get; set; }

    /// <summary>Player shield strength (0-255, front).</summary>
    byte ShieldFront { get; set; }

    /// <summary>Player shield strength (0-255, aft).</summary>
    byte ShieldAft { get; set; }

    /// <summary>When true, no ships spawn via scheduler or random spawning.</summary>
    bool TargetPracticeMode { get; set; }
}
