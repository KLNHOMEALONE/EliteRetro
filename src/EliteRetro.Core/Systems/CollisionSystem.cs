using EliteRetro.Core.Entities;
using EliteRetro.Core.Managers;
using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Systems;

/// <summary>
/// Collision detection and resolution for local bubble entities.
/// Uses simple sphere-based collision with configurable radii.
/// </summary>
public static class CollisionSystem
{
    /// <summary>
    /// Base collision radius for ships (in local coordinates).
    /// Scaled by ship model size.
    /// </summary>
    private const float BaseCollisionRadius = 10f;

    /// <summary>
    /// Check all active entities for collisions.
    /// Called periodically via MCNT scheduler.
    /// </summary>
    public static void CheckCollisions(LocalBubbleManager bubbleManager)
    {
        var active = bubbleManager.GetAllActive();
        var list = active.ToList();

        for (int i = 0; i < list.Count; i++)
        {
            for (int j = i + 1; j < list.Count; j++)
            {
                var a = list[i];
                var b = list[j];

                if (!a.IsActive || !b.IsActive) continue;

                // Skip reserved slots vs each other
                if (a.SlotIndex < 2 && b.SlotIndex < 2) continue;

                if (CheckCollision(a, b))
                {
                    ResolveCollision(a, b);
                }
            }
        }
    }

    /// <summary>
    /// Check if two ships are colliding.
    /// Uses sphere-based collision with size-scaled radii.
    /// </summary>
    public static bool CheckCollision(ShipInstance a, ShipInstance b)
    {
        float radiusA = GetCollisionRadius(a);
        float radiusB = GetCollisionRadius(b);
        float combinedRadius = radiusA + radiusB;

        float distSq = a.DistanceSquaredTo(b);
        return distSq < combinedRadius * combinedRadius;
    }

    /// <summary>
    /// Get collision radius for a ship based on its model size.
    /// </summary>
    private static float GetCollisionRadius(ShipInstance ship)
    {
        // Estimate from model vertex count
        int vertexCount = ship.Blueprint.Model.Vertices.Count;
        float sizeFactor = 1f + (vertexCount / 20f);
        return BaseCollisionRadius * sizeFactor;
    }

    /// <summary>
    /// Resolve a collision between two ships.
    /// Both take hull damage proportional to relative velocity.
    /// </summary>
    private static void ResolveCollision(ShipInstance a, ShipInstance b)
    {
        // Calculate relative velocity
        Vector3 relVel = a.Velocity - b.Velocity;
        float impactForce = relVel.Length();

        // Damage proportional to impact
        int damageA = (int)(impactForce * 2);
        int damageB = (int)(impactForce * 2);

        bool aDestroyed = a.TakeDamage(damageA);
        bool bDestroyed = b.TakeDamage(damageB);

        if (aDestroyed)
            a.IsActive = false;
        if (bDestroyed)
            b.IsActive = false;

        // Separate ships to prevent repeated collisions
        if (!aDestroyed && !bDestroyed)
        {
            Vector3 separation = Vector3.Normalize(b.Position - a.Position) * 20f;
            a.Position -= separation * 0.5f;
            b.Position += separation * 0.5f;
        }
    }

    /// <summary>
    /// Check if a ship has crashed into the planet.
    /// </summary>
    public static bool CheckPlanetCrash(ShipInstance ship, ShipInstance? planet)
    {
        if (planet == null) return false;

        float planetRadius = GameConstants.PlanetRadius * 0.0001f; // Scale to local coords
        float shipRadius = GetCollisionRadius(ship) * 0.0001f;

        float dist = Vector3.Distance(ship.Position, planet.Position);
        return dist < (planetRadius + shipRadius);
    }

    /// <summary>
    /// Check if a ship has been sucked into the sun (fatal proximity).
    /// </summary>
    public static bool CheckSunFatal(ShipInstance ship, ShipInstance? sun)
    {
        if (sun == null) return false;

        float sunRadius = GameConstants.PlanetRadius * 80 * 0.0001f * 0.9f; // 0.90x planet diameter
        float shipRadius = GetCollisionRadius(ship) * 0.0001f;

        float dist = Vector3.Distance(ship.Position, sun.Position);
        return dist < (sunRadius + shipRadius);
    }
}
