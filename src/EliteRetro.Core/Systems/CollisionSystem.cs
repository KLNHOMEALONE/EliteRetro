using EliteRetro.Core.Entities;
using EliteRetro.Core.Managers;
using EliteRetro.Core.Utilities;
using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Systems;

/// <summary>
/// Collision detection and resolution for local bubble entities.
/// Uses simple bounding-sphere logic for speed and simplicity.
/// </summary>
public static class CollisionSystem
{
    private static readonly Random _rng = new Random();

    /// <summary>
    /// Checks for a collision between two ships.
    /// </summary>
    public static bool CheckCollision(ShipInstance shipA, ShipInstance shipB)
    {
        float combinedRadius = GetCollisionRadius(shipA) + GetCollisionRadius(shipB);
        float distSq = Vector3.DistanceSquared(shipA.Position, shipB.Position);

        return distSq < (combinedRadius * combinedRadius);
    }

    /// <summary>
    /// Resolves a collision between two ships, applying damage and effects.
    /// </summary>
    public static void ResolveCollision(ShipInstance shipA, ShipInstance shipB, LocalBubbleManager bubbleManager)
    {
        // Simple resolution: both ships take significant damage.
        // For Elite "feel", collisions are usually fatal for small ships.
        int damageA = 100;
        int damageB = 100;

        shipA.TakeDamage(damageA);
        shipB.TakeDamage(damageB);

        // Feedback for player
        if (shipA.SlotIndex == GameConstants.PlayerSlot || shipB.SlotIndex == GameConstants.PlayerSlot)
        {
            var other = shipA.SlotIndex == GameConstants.PlayerSlot ? shipB : shipA;
            bubbleManager.RaiseCollision(other.Blueprint.Name);
        }

        System.Diagnostics.Debug.WriteLine($"[COLLISION] {shipA.Blueprint.Name} vs {shipB.Blueprint.Name}");
    }

    /// <summary>
    /// Spawns cargo canisters when a ship is destroyed.
    /// </summary>
    public static void SpawnCargoDrops(ShipInstance ship, LocalBubbleManager bubbleManager)
    {
        if (ship.Cargo.Count == 0) return;

        foreach (var kvp in ship.Cargo)
        {
            for (int i = 0; i < kvp.Value; i++)
            {
                var model = CanisterModel.Create(8f);
                var blueprint = new ShipBlueprint
                {
                    Name = "Canister",
                    Model = model,
                    MaxSpeed = 0f,
                    MaxEnergy = 1,
                    HullStrength = 1,
                    ShieldStrength = 0,
                    IsCargo = true
                };

                var canister = new ShipInstance(blueprint)
                {
                    Position = ship.Position + new Vector3(
                        (float)_rng.NextDouble() * 10 - 5,
                        (float)_rng.NextDouble() * 10 - 5,
                        (float)_rng.NextDouble() * 10 - 5),
                    Speed = 0
                };
                canister.AddCargo(kvp.Key, 1);
                bubbleManager.TrySpawn(canister);
            }
        }
    }

    private static float GetCollisionRadius(ShipInstance ship)
    {
        // Approximate radius from model size
        return ship.Blueprint.Model?.Radius ?? 10f;
    }

    public enum PlanetCollisionType
    {
        None,
        Glancing, // shallow angle, slide/scrape
        Crash     // steep angle, immediate stop
    }

    public record struct PlanetCollisionResult(PlanetCollisionType Type, Vector3 PushBack);

    /// <summary>
    /// Check for planet collision with steep/glancing distinction.
    /// Steep hits cause a crash; glancing hits allow "scraping" with damage.
    /// </summary>
    public static PlanetCollisionResult CheckPlanetCollision(ShipInstance ship, ShipInstance? planet)
    {
        if (planet == null) return new PlanetCollisionResult(PlanetCollisionType.None, Vector3.Zero);

        float planetRadius = GameConstants.PlanetRadius;
        // ShipRadius removed from physical threshold to match visual AL bar zero-point
        float threshold = planetRadius; 

        Vector3 toShip = ship.Position - planet.Position;
        float dist = toShip.Length();

        if (dist >= threshold)
            return new PlanetCollisionResult(PlanetCollisionType.None, Vector3.Zero);

        // We are colliding. Determine angle of approach.
        // Forward is (0,0,1) in cockpit space. Direction to planet is -Normalize(toShip).
        Vector3 forward = new Vector3(0, 0, 1);
        Vector3 toPlanet = Vector3.Normalize(-ship.Position + planet.Position);
        float approachDot = Vector3.Dot(forward, toPlanet);

        // Push back vector (to move ship out of planet)
        Vector3 normal = Vector3.Normalize(toShip);
        // Strengthen pushback (2.0x depth) to prevent sticking to surface
        Vector3 pushBack = normal * (threshold - dist) * 2.0f;

        // LOGGING: include speed to verify if ship is stopped
        Logger.LogCollision(dist, planetRadius, approachDot, ship.Speed);

        // If approach angle > ~25 degrees (dot > 0.9), it's a direct crash.
        // Shallow angles are glancing scrapes.
        var type = approachDot > 0.9f ? PlanetCollisionType.Crash : PlanetCollisionType.Glancing;
        
        return new PlanetCollisionResult(type, pushBack);
    }

    [Obsolete("Use CheckPlanetCollision for more nuanced results")]
    public static bool CheckPlanetCrash(ShipInstance ship, ShipInstance? planet)
    {
        return CheckPlanetCollision(ship, planet).Type == PlanetCollisionType.Crash;
    }

    /// <summary>
    /// Check if a ship has been sucked into the sun (fatal proximity).
    /// </summary>
    public static bool CheckSunFatal(ShipInstance ship, ShipInstance? sun)
    {
        if (sun == null) return false;
        float fatalDist = GameConstants.PlanetRadius * 6 * GameConstants.SunFatalDistanceMultiplier;
        return Vector3.Distance(ship.Position, sun.Position) < fatalDist;
    }

    /// <summary>
    /// Main check for player collisions against all bubble entities.
    /// </summary>
    public static void CheckPlayerCollisions(LocalBubbleManager bubbleManager)
    {
        var player = bubbleManager.PlayerShip;
        if (player == null) return;

        foreach (var entity in bubbleManager.GetAllActive())
        {
            // Skip player and solar bodies - handled separately in FlightScene.cs 
            // (CheckPlanetCrash / CheckSunProximity) with correct radii.
            if (entity.SlotIndex == GameConstants.PlayerSlot) continue;
            if (entity.SlotIndex == GameConstants.PlanetSlot) continue;
            if (entity.SlotIndex == GameConstants.SunStationSlot) continue;
            if (!entity.IsActive) continue;
            if (entity.IsTargetPractice) continue; // Skip target practice ships

            if (CheckCollision(player, entity))
            {
                ResolveCollision(player, entity, bubbleManager);
            }
        }
    }
}
