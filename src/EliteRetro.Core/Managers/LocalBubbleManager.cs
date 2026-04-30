using EliteRetro.Core.Entities;
using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Managers;

/// <summary>
/// Manages entities within the player's local bubble (renderable area).
/// Uses a fixed slot array: slot 0 = planet, slot 1 = sun/station,
/// slots 2+ = ships, missiles, cargo. Enforces capacity limits and
/// handles spawn/despawn, culling, and safe zone triggers.
/// </summary>
public class LocalBubbleManager
{
    private readonly ShipInstance?[] _slots;
    private readonly int _capacity;
    private int _tidyIndex; // round-robin TIDY counter

    /// <summary>Planet entity (always in slot 0).</summary>
    public ShipInstance? Planet => _slots[GameConstants.PlanetSlot];

    /// <summary>Sun or station entity (slot 1, mutually exclusive).</summary>
    public ShipInstance? SunOrStation => _slots[GameConstants.SunStationSlot];

    /// <summary>Player position (at origin in local bubble coordinates).</summary>
    public Vector3 PlayerPosition { get; set; }

    public LocalBubbleManager(int capacity = GameConstants.MaxSlots)
    {
        _capacity = capacity;
        _slots = new ShipInstance[capacity];
    }

    /// <summary>
    /// Place an entity in a specific slot.
    /// </summary>
    public void SetSlot(int index, ShipInstance? entity)
    {
        if (index < 0 || index >= _capacity) return;
        _slots[index] = entity;
        if (entity != null)
            entity.SlotIndex = index;
    }

    /// <summary>
    /// Spawn a ship in the first available slot (from slot 2 upward).
    /// </summary>
    public bool TrySpawn(ShipInstance ship)
    {
        for (int i = GameConstants.FirstAvailableSlot; i < _capacity; i++)
        {
            if (_slots[i] == null || !_slots[i]!.IsActive)
            {
                _slots[i] = ship;
                ship.SlotIndex = i;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Despawn a ship by slot index.
    /// </summary>
    public void Despawn(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _capacity) return;
        if (_slots[slotIndex] != null)
            _slots[slotIndex]!.IsActive = false;
        _slots[slotIndex] = null;
    }

    /// <summary>
    /// Despawn a ship by instance.
    /// </summary>
    public void Despawn(ShipInstance ship)
    {
        Despawn(ship.SlotIndex);
    }

    /// <summary>
    /// Cull entities beyond the bubble radius.
    /// Called each frame to remove ships that have flown too far.
    /// </summary>
    public void CullBeyondBubble()
    {
        for (int i = GameConstants.FirstAvailableSlot; i < _capacity; i++)
        {
            if (_slots[i] != null && _slots[i]!.IsActive)
            {
                float distSq = _slots[i]!.Position.LengthSquared();
                float bubbleSq = (float)GameConstants.BubbleRadius * GameConstants.BubbleRadius;
                if (distSq > bubbleSq)
                    Despawn(i);
            }
        }
    }

    /// <summary>
    /// Check if player is within the safe zone of the planet's orbit point.
    /// Safe zone triggers station spawn and sun removal.
    /// </summary>
    public bool IsInSafeZone()
    {
        if (Planet == null) return false;

        // Orbit point: planet position + 2 * planetNosev * PlanetRadius
        Vector3 orbitPoint = Planet.Position + Planet.Orientation.Nosev * 2 * GameConstants.PlanetRadius;
        Vector3 diff = PlayerPosition - orbitPoint;
        return diff.LengthSquared() < GameConstants.SafeZoneRadius * GameConstants.SafeZoneRadius;
    }

    /// <summary>
    /// Spawn the space station and remove the sun.
    /// Station is placed at the orbit point, facing the planet.
    /// </summary>
    public void SpawnStation(ShipBlueprint stationBlueprint)
    {
        if (Planet == null) return;

        // Station at orbit point, nose inverted to face planet
        Vector3 orbitPoint = Planet.Position + Planet.Orientation.Nosev * 2 * GameConstants.PlanetRadius;
        var station = new ShipInstance(stationBlueprint)
        {
            Position = orbitPoint,
            Speed = 0
        };
        // Invert nose to face planet
        station.Orientation.Nosev = -Planet.Orientation.Nosev;
        Vector3 up = Vector3.UnitY;
        station.Orientation.Sidev = Vector3.Normalize(Vector3.Cross(station.Orientation.Nosev, up));
        station.Orientation.Roofv = Vector3.Cross(station.Orientation.Sidev, station.Orientation.Nosev);

        // Remove sun, place station
        Despawn(GameConstants.SunStationSlot);
        SetSlot(GameConstants.SunStationSlot, station);
    }

    /// <summary>
    /// Get all active ships (excluding planet and sun/station).
    /// </summary>
    public IEnumerable<ShipInstance> GetActiveShips()
    {
        for (int i = GameConstants.FirstAvailableSlot; i < _capacity; i++)
        {
            if (_slots[i] != null && _slots[i]!.IsActive)
                yield return _slots[i]!;
        }
    }

    /// <summary>
    /// Get all active entities including planet and sun/station.
    /// </summary>
    public IEnumerable<ShipInstance> GetAllActive()
    {
        for (int i = 0; i < _capacity; i++)
        {
            if (_slots[i] != null && _slots[i]!.IsActive)
                yield return _slots[i]!;
        }
    }

    /// <summary>
    /// Run TIDY orthonormalization on one entity (round-robin).
    /// Called once per frame to spread the cost.
    /// </summary>
    public void TidyOne()
    {
        for (int i = 0; i < _capacity; i++)
        {
            _tidyIndex = (_tidyIndex + 1) % _capacity;
            if (_slots[_tidyIndex] != null && _slots[_tidyIndex]!.IsActive)
            {
                _slots[_tidyIndex]!.Orientation.Tidy();
                return;
            }
        }
    }

    /// <summary>
    /// Apply universe rotation to all entities in the bubble.
    /// </summary>
    public void ApplyUniverseRotation(float alpha, float beta)
    {
        for (int i = 0; i < _capacity; i++)
        {
            if (_slots[i] != null && _slots[i]!.IsActive)
                _slots[i]!.ApplyUniverseRotation(alpha, beta);
        }
    }

    /// <summary>
    /// Clear all slots (used when jumping to another system).
    /// </summary>
    public void Clear()
    {
        for (int i = 0; i < _capacity; i++)
            _slots[i] = null;
    }

    // --- Sun distance effects ---

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
    /// Check sun proximity and return current effect level.
    /// </summary>
    public SunProximityEffect CheckSunProximity()
    {
        var sun = SunOrStation;
        if (sun == null || sun.Blueprint?.Name != "Sun")
            return SunProximityEffect.None;

        float planetDiameter = GameConstants.PlanetRadius * 2;
        float dist = Vector3.Distance(PlayerPosition, sun.Position);

        // Fatal: closer than 0.90 × planet diameter
        if (dist < planetDiameter * GameConstants.SunFatalDistanceMultiplier)
            return SunProximityEffect.Fatal;

        // Fuel scoop: closer than 1.33 × planet diameter
        if (dist < planetDiameter * GameConstants.SunFuelScoopDistanceMultiplier)
            return SunProximityEffect.FuelScoop;

        // Heat warning: closer than 2.67 × planet diameter
        if (dist < planetDiameter * GameConstants.SunHeatDistanceMultiplier)
            return SunProximityEffect.HeatWarning;

        return SunProximityEffect.None;
    }

    /// <summary>
    /// Apply fuel scooping when within range of the sun.
    /// </summary>
    /// <param name="fuelPerSecond">Fuel units added per second.</param>
    /// <returns>Fuel added this frame, or 0 if not in range.</returns>
    public float ApplyFuelScoop(float fuelPerSecond)
    {
        if (CheckSunProximity() != SunProximityEffect.FuelScoop)
            return 0f;

        // In a real implementation, this would be called with deltaTime
        // For now, return the per-frame amount (assumes 60fps)
        return fuelPerSecond / 60f;
    }

    /// <summary>
    /// Energy bomb effect: destroys all non-reserved entities within blast radius.
    /// Blast radius = 1.17 × planet diameter.
    /// </summary>
    /// <returns>Number of entities destroyed.</returns>
    public int DetonateEnergyBomb()
    {
        float blastRadius = GameConstants.PlanetRadius * 2 * GameConstants.EnergyBombMultiplier;
        float blastSq = blastRadius * blastRadius;
        int destroyed = 0;

        // Check all non-reserved slots (2+)
        for (int i = GameConstants.FirstAvailableSlot; i < _capacity; i++)
        {
            if (_slots[i] != null && _slots[i]!.IsActive)
            {
                float distSq = _slots[i]!.Position.LengthSquared();
                if (distSq < blastSq)
                {
                    Despawn(i);
                    destroyed++;
                }
            }
        }

        return destroyed;
    }
}
