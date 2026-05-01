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
    private const float BaseCollisionRadius = 120f;

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
                    ResolveCollision(a, b, bubbleManager);
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
        float dist = MathF.Sqrt(distSq);

        // Debug: log close approaches
        if (dist < combinedRadius * 3)
        {
            System.Diagnostics.Debug.WriteLine($"Close approach: {a.Blueprint.Name} vs {b.Blueprint.Name}, dist={dist:F1}, combinedRadius={combinedRadius:F1}");
        }

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
    /// Both take hull damage proportional to relative speed.
    /// Player collisions reduce shield/hull via LocalBubbleManager.
    /// Small ships (low vertex count) are destroyed instantly on collision.
    /// </summary>
    private static void ResolveCollision(ShipInstance a, ShipInstance b, LocalBubbleManager bubbleManager)
    {
        // Use speed scalars for impact force (velocity vectors not yet wired)
        float impactForce = Math.Abs(a.Speed) + Math.Abs(b.Speed);

        // Damage proportional to impact (higher multiplier for visible feedback)
        int damageA = (int)(impactForce * 5);
        int damageB = (int)(impactForce * 5);

        // Handle player collision specially
        if (a.SlotIndex == GameConstants.PlayerSlot)
        {
            ApplyPlayerDamage(a, damageA, b);
            bubbleManager.RaiseCollision(b.Blueprint?.Name ?? "Unknown");
        }
        else if (b.SlotIndex == GameConstants.PlayerSlot)
        {
            ApplyPlayerDamage(b, damageB, a);
            bubbleManager.RaiseCollision(a.Blueprint?.Name ?? "Unknown");
        }

        // Damage NPC ships normally
        if (a.SlotIndex != GameConstants.PlayerSlot)
        {
            bool destroyed = ResolveShipCollisionDamage(a, damageA);
            if (destroyed)
            {
                OnShipDestroyed(a, b, bubbleManager);
                a.IsActive = false;
            }
        }
        if (b.SlotIndex != GameConstants.PlayerSlot)
        {
            bool destroyed = ResolveShipCollisionDamage(b, damageB);
            if (destroyed)
            {
                OnShipDestroyed(b, a, bubbleManager);
                b.IsActive = false;
            }
        }

        // Separate ships to prevent repeated collisions
        if (a.IsActive && b.IsActive)
        {
            Vector3 separation = Vector3.Normalize(b.Position - a.Position) * 20f;
            a.Position -= separation * 0.5f;
            b.Position += separation * 0.5f;
        }
    }

    /// <summary>
    /// Apply collision damage to an NPC ship.
    /// Small ships (vertex count < 15) lose all shields and are destroyed instantly.
    /// Larger ships take proportional hull damage.
    /// </summary>
    private static bool ResolveShipCollisionDamage(ShipInstance ship, int damage)
    {
        int vertexCount = ship.Blueprint.Model.Vertices.Count;

        // Small ships: instant destruction on any collision
        if (vertexCount < 15)
        {
            System.Diagnostics.Debug.WriteLine($"[COLLISION] Small ship {ship.Blueprint.Name} (vertices={vertexCount}) instant-destroyed");
            ship.Energy = 0;
            ship.Hull = 0;
            return true;
        }

        // Larger ships: damage shields first, then hull
        if (ship.Energy > 0)
        {
            int shieldDmg = Math.Min(damage, ship.Energy);
            ship.Energy = (byte)(ship.Energy - shieldDmg);
            int hullDmg = damage - shieldDmg;
            if (hullDmg > 0)
            {
                bool destroyed = ship.TakeDamage(hullDmg);
                if (destroyed)
                    System.Diagnostics.Debug.WriteLine($"[COLLISION] Large ship {ship.Blueprint.Name} destroyed (hull depleted)");
                return destroyed;
            }
            return false;
        }

        bool hullDestroyed = ship.TakeDamage(damage);
        if (hullDestroyed)
            System.Diagnostics.Debug.WriteLine($"[COLLISION] Ship {ship.Blueprint.Name} hull-destroyed (no shields)");
        return hullDestroyed;
    }

    /// <summary>
    /// Called when a ship is destroyed — track kills and spawn cargo drops.
    /// </summary>
    private static void OnShipDestroyed(ShipInstance destroyed, ShipInstance? destroyer, LocalBubbleManager bubbleManager)
    {
        // Track kill if player was involved
        if (destroyer?.SlotIndex == GameConstants.PlayerSlot)
        {
            bubbleManager.Commander.AddKill();
        }

        // Spawn cargo canisters from destroyed ship's cargo
        SpawnCargoDrops(destroyed, bubbleManager);
    }

    /// <summary>
    /// Apply damage to player ship (shields first, then hull).
    /// </summary>
    private static void ApplyPlayerDamage(ShipInstance playerShip, int damage, ShipInstance other)
    {
        // Determine if hit is from front or rear
        Vector3 toOther = Vector3.Normalize(other.Position - playerShip.Position);
        float dot = Vector3.Dot(playerShip.Orientation.Nosev, toOther);
        bool hitFront = dot > 0;

        // Apply to shields first, then hull
        if (hitFront)
        {
            // Front shields absorb first
            // TODO: wire to LocalBubbleManager shield fields when shield system is implemented
            int shieldDmg = Math.Min(damage, playerShip.Energy);
            playerShip.Energy = (byte)(playerShip.Energy - shieldDmg);
            int hullDmg = damage - shieldDmg;
            if (hullDmg > 0)
            {
                bool destroyed = playerShip.TakeDamage(hullDmg);
                if (destroyed)
                {
                    // TODO: player death / escape pod
                    playerShip.IsActive = false;
                }
            }
        }
        else
        {
            // Rear hit - damage hull directly (no aft shields in simple mode)
            bool destroyed = playerShip.TakeDamage(damage);
            if (destroyed)
            {
                playerShip.IsActive = false;
            }
        }
    }

    /// <summary>
    /// Spawn cargo canisters from a destroyed ship.
    /// Each cargo item becomes a drifting canister entity.
    /// </summary>
    public static void SpawnCargoDrops(ShipInstance destroyed, LocalBubbleManager bubbleManager)
    {
        if (destroyed.Cargo.Count == 0) return;

        var rng = new Random((int)destroyed.Position.X * 17 + (int)destroyed.Position.Y * 31);
        var canisterBlueprint = new ShipBlueprint
        {
            Name = "Cargo Canister",
            Model = CanisterModel.Create(8),
            MaxSpeed = 0,
            MaxEnergy = 0,
            HullStrength = 1,
            ShieldStrength = 0,
        };

        foreach (var kvp in destroyed.Cargo)
        {
            for (int i = 0; i < kvp.Value; i++)
            {
                var canister = new ShipInstance(canisterBlueprint)
                {
                    Position = destroyed.Position + new Vector3(
                        (float)(rng.NextDouble() * 2 - 1) * 20,
                        (float)(rng.NextDouble() * 2 - 1) * 20,
                        (float)(rng.NextDouble() * 2 - 1) * 20
                    ),
                    Speed = 0,
                    Energy = 0,
                    Hull = 1,
                };
                canister.AddCargo(kvp.Key, 1);
                bubbleManager.TrySpawn(canister);
            }
        }
    }
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
