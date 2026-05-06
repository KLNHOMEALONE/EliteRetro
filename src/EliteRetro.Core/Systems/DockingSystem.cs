using EliteRetro.Core.Entities;
using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Systems;

/// <summary>
/// Docking state machine stages.
/// </summary>
public enum DockingStage
{
    None = 0,
    Approaching = 1,    // Heading toward station zone
    Aligning = 2,       // Adjusting pitch/roll for slot
    Accelerating = 3,   // Thrusting toward dock point
    Docked = 4
}

/// <summary>
/// Docking system — 5 geometric checks and docking computer.
/// Implements original Elite docking mechanics.
/// </summary>
public static class DockingSystem
{
    // Fixed-point thresholds (original Elite uses 8-bit fixed-point where 255 = 1.0)

    /// <summary>nosev_z ≥ 214 — within 26° of head-on approach (fixed-point, ~0.84 = cos(33°)).</summary>
    public const int ApproachAngleThreshold = 214;

    /// <summary>z ≥ 89 — within 22° cone from station center.</summary>
    public const int SafeConeThreshold = 89;

    /// <summary>|roofv_x| ≥ 80 — slot within 33.6° of horizontal.</summary>
    public const int SlotHorizontalThreshold = 80;

    /// <summary>
    /// Check all 5 docking conditions. All must pass for successful dock.
    /// </summary>
    /// <param name="ship">The ship attempting to dock.</param>
    /// <param name="station">The station entity.</param>
    /// <returns>True if all 5 checks pass.</returns>
    public static bool CheckDockingClearance(ShipInstance ship, ShipInstance station)
    {
        // 1. Friendliness — station not hostile
        if (!CheckFriendliness(station))
            return false;

        // 2. Approach angle — nosev_z ≥ 214 (within 26° of head-on)
        if (!CheckApproachAngle(ship, station))
            return false;

        // 3. Heading — ship faces station (z-component of direction positive)
        if (!CheckHeading(ship, station))
            return false;

        // 4. Safe cone — position within 22° cone (z ≥ 89)
        if (!CheckSafeCone(ship, station))
            return false;

        // 5. Slot horizontal — |roofv_x| ≥ 80
        if (!CheckSlotHorizontal(station))
            return false;

        return true;
    }

    /// <summary>
    /// Check 1: Station is not hostile.
    /// </summary>
    public static bool CheckFriendliness(ShipInstance station)
    {
        var personality = GetPersonalityFlags(station.Blueprint.ShipClass);
        return !personality.HasFlag(NewbFlags.Hostile);
    }

    /// <summary>
    /// Convert ShipClass byte to appropriate NewbFlags personality.
    /// ShipClass: 0=Innocent, 1=BountyHunter, 2=Pirate, 3=Cop
    /// Maps to proper NEWB bit flags for behavior.
    /// </summary>
    private static NewbFlags GetPersonalityFlags(byte shipClass)
    {
        return shipClass switch
        {
            0 => NewbFlags.Trader | NewbFlags.Innocent,
            1 => NewbFlags.BountyHunter | NewbFlags.Innocent,
            2 => NewbFlags.Pirate,
            3 => NewbFlags.Cop | NewbFlags.Innocent,
            _ => NewbFlags.None,
        };
    }

    /// <summary>
    /// Check 2: Ship within 26° of head-on approach.
    /// nosev_z of ship relative to station ≥ 214 (fixed-point).
    /// </summary>
    public static bool CheckApproachAngle(ShipInstance ship, ShipInstance station)
    {
        Vector3 toStation = Vector3.Normalize(station.Position - ship.Position);
        float dot = Vector3.Dot(ship.Orientation.Nosev, toStation);

        // Convert to fixed-point (255 = 1.0) and check threshold
        int fixedDot = (int)(dot * 255);
        // NE-10 fix: was inverted (<=). Should be >= to require facing the station.
        // Approach angle threshold 214/255 ≈ 0.84 → ~33° cone.
        return fixedDot >= ApproachAngleThreshold;
    }

    /// <summary>
    /// Check 3: Ship faces station — z-component of direction to station is positive.
    /// </summary>
    public static bool CheckHeading(ShipInstance ship, ShipInstance station)
    {
        Vector3 toStation = station.Position - ship.Position;
        // Transform direction into ship's local space (X=right, Y=up, Z=forward)
        Vector3 localDir = ship.Orientation.Transform(Vector3.Normalize(toStation));
        return localDir.Z > 0;
    }

    /// <summary>
    /// Check 4: Ship within 22° safe cone from station center.
    /// z ≥ 89 (fixed-point) in station's local space.
    /// </summary>
    public static bool CheckSafeCone(ShipInstance ship, ShipInstance station)
    {
        Vector3 toShip = Vector3.Normalize(ship.Position - station.Position);
        // Transform into station's local space (X=right, Y=up, Z=forward)
        Vector3 localDir = station.Orientation.Transform(toShip);

        // z-component in fixed-point
        int fixedZ = (int)(localDir.Z * 255);
        return fixedZ >= SafeConeThreshold;
    }

    /// <summary>
    /// Check 5: Docking slot is within 33.6° of horizontal.
    /// |roofv_x| ≥ 80 (fixed-point).
    /// </summary>
    public static bool CheckSlotHorizontal(ShipInstance station)
    {
        float roofvX = station.Orientation.Roofv.X;
        int fixedX = (int)(Math.Abs(roofvX) * 255);
        return fixedX >= SlotHorizontalThreshold;
    }

    /// <summary>
    /// Distance check: ship within docking range of station.
    /// </summary>
    public static bool CheckDockingRange(ShipInstance ship, ShipInstance station, float maxRange = 300f)
    {
        return ship.DistanceTo(station) <= maxRange;
    }

    /// <summary>
    /// Docking computer state machine.
    /// Injects fake keypresses to automate approach and docking.
    /// Intentionally imperfect — can crash or miss slot.
    /// </summary>
    public static DockingStage UpdateDockingComputer(
        ShipInstance ship,
        ShipInstance station,
        DockingStage currentStage,
        out bool pitchUp,
        out bool pitchDown,
        out bool rollLeft,
        out bool rollRight,
        out bool thrust)
    {
        pitchUp = false;
        pitchDown = false;
        rollLeft = false;
        rollRight = false;
        thrust = false;

        switch (currentStage)
        {
            case DockingStage.None:
                // Not attempting to dock
                return DockingStage.None;

            case DockingStage.Approaching:
                // Stage 1: Head for station zone
                Vector3 toStation = station.Position - ship.Position;
                float dist = toStation.Length();

                if (dist > 500f)
                {
                    // Still far — thrust toward station
                    thrust = true;
                    return DockingStage.Approaching;
                }

                // Close enough to start aligning
                return DockingStage.Aligning;

            case DockingStage.Aligning:
                // Stage 2/3: Adjust pitch/roll to center station and align slot
                Vector3 toStationLocal = station.Position - ship.Position;
                float distAlign = toStationLocal.Length();
                Vector3 stationDir = Vector3.Normalize(toStationLocal);
                Vector3 localDir = ship.Orientation.Transform(stationDir);

                // Pitch to center station on crosshairs
                if (localDir.Y > 0.1f)
                    pitchDown = true;
                else if (localDir.Y < -0.1f)
                    pitchUp = true;

                // Roll to make slot horizontal
                // Check if station's roofv_x is level
                float stationRoll = station.Orientation.Roofv.X;
                if (stationRoll > 0.2f)
                    rollRight = true;
                else if (stationRoll < -0.2f)
                    rollLeft = true;

                // If aligned well enough, start accelerating
                if (Math.Abs(localDir.Y) < 0.05f && Math.Abs(stationRoll) < 0.1f)
                {
                    thrust = true;

                    // Check if we're close enough to attempt docking
                    if (distAlign < 100f && CheckDockingClearance(ship, station))
                        return DockingStage.Docked;
                }

                return DockingStage.Aligning;

            case DockingStage.Accelerating:
                thrust = true;

                // Minor corrections while accelerating
                Vector3 dir2 = Vector3.Normalize(station.Position - ship.Position);
                Vector3 local2 = ship.Orientation.Transform(dir2);
                float dist2 = ship.DistanceTo(station);
                if (local2.Y > 0.05f)
                    pitchDown = true;
                else if (local2.Y < -0.05f)
                    pitchUp = true;

                if (CheckDockingClearance(ship, station) && dist2 < 50f)
                    return DockingStage.Docked;

                // If we got too close without clearing, abort
                if (dist2 < 30f && !CheckDockingClearance(ship, station))
                    return DockingStage.Aligning;

                return DockingStage.Accelerating;

            case DockingStage.Docked:
                // Successfully docked
                return DockingStage.Docked;

            default:
                return DockingStage.None;
        }
    }

    /// <summary>
    /// Calculate docking point: 8 units from station through the slot.
    /// </summary>
    public static Vector3 CalculateDockingPoint(ShipInstance station)
    {
        // Docking point is 8 units from station center, along negative nosev
        return station.Position - station.Orientation.Nosev * 8f;
    }
}
