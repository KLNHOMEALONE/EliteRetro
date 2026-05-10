using EliteRetro.Core.Entities;

namespace EliteRetro.Core.Managers;

/// <summary>
/// Sun proximity effects based on distance from player.
/// </summary>
public enum SunProximityEffect
{
    None = 0,
    HeatWarning = 1,    // >2.67r: cabin temperature rises
    FuelScoop = 2,       // >1.33r: fuel scooping available
    Fatal = 3            // <0.90r: fatal damage
}

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

    /// <summary>
    /// Check sun proximity and return current effect level.
    /// </summary>
    SunProximityEffect CheckSunProximity(IBubbleManager bubbleManager);

    /// <summary>
    /// Apply fuel scooping when within range of the sun.
    /// </summary>
    /// <param name="bubbleManager">World manager to check sun position.</param>
    /// <param name="fuelPerSecond">Fuel units added per second.</param>
    /// <returns>Fuel added this frame, or 0 if not in range.</returns>
    float ApplyFuelScoop(IBubbleManager bubbleManager, float fuelPerSecond);
}
