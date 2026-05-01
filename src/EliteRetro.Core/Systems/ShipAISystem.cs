using Microsoft.Xna.Framework;
using EliteRetro.Core.Managers;
using EliteRetro.Core.Entities;

namespace EliteRetro.Core.Entities
{
    /// <summary>
    /// NEWB personality flags (byte #37 in original Elite).
    /// Each bit controls a behavioral trait for NPC ships.
    /// </summary>
    [Flags]
    public enum NewbFlags : byte
    {
        None = 0,
        Trader = 1 << 0,       // Flies between station/planet
        BountyHunter = 1 << 1, // Attacks fugitives
        Hostile = 1 << 2,      // Attacks on sight
        Pirate = 1 << 3,       // Stops attacking in safe zone
        Docking = 1 << 4,      // Traders head to station (dynamic)
        Innocent = 1 << 5,     // Station defends them
        Cop = 1 << 6,          // Destroying makes player fugitive
        Scooped = 1 << 7,      // Docked or escape pod (dynamic)
    }
}

namespace EliteRetro.Core.Systems
{
    /// <summary>
    /// AI state machine states for ship behavior.
    /// </summary>
    public enum ShipAIState : byte
    {
        Idle = 0,
        Patrol = 1,
        Chase = 2,
        Flee = 3,
        Dock = 4,
        Bail = 5,      // Escape pod launched
    }

    /// <summary>
    /// Ship AI & Combat system — implements the authentic Elite TACTICS routine.
    /// Called via MCNT scheduler (every 8 frames, offsets 0-3 for 1-2 ships).
    /// </summary>
    public static class ShipAISystem
    {
        private static readonly Random _rng = new Random();

        /// <summary>
        /// Execute TACTICS routine for a single ship.
        /// Called by MCNT scheduler for 1-2 ships per frame.
        /// </summary>
        public static void ExecuteTactics(
            ShipInstance ship,
            ShipInstance? player,
            LocalBubbleManager bubbleManager,
            byte mcnt)
        {
            if (!ship.IsActive) return;

            // Energy recharge: +1 per iteration
            if (ship.Energy < ship.Blueprint.MaxEnergy)
                ship.Energy = (byte)Math.Min(ship.Energy + 1, ship.Blueprint.MaxEnergy);

            // Part 3: Targeting — find nearest hostile target
            ShipInstance? target = FindTarget(ship, player, bubbleManager);
            if (target == null)
            {
                ship.AIState = (byte)ShipAIState.Patrol;
                return;
            }

            ship.TargetSlot = target.SlotIndex;

            // Part 4: Energy check — 2.5% random roll, bail if low energy
            if (ShouldBail(ship, target))
            {
                ship.AIState = (byte)ShipAIState.Bail;
                return;
            }

            // Part 5: Missile decision
            if (ShouldFireMissile(ship, target))
            {
                // TODO: fire missile (Phase 6.6)
            }

            // Part 6: Laser firing
            if (CanFireLaser(ship, target, out bool accurate))
            {
                if (!accurate)
                    FireLaser(ship, target, false, bubbleManager);  // Fire but miss
                else
                    FireLaser(ship, target, true, bubbleManager);   // Register hit
            }

            // Part 7: Movement — determine direction based on personality
            Vector3 moveDir = CalculateMovementDirection(ship, target, bubbleManager);
            ApplyMovement(ship, moveDir, target.Position);
        }

        /// <summary>
        /// Find the nearest hostile target for a ship.
        /// </summary>
        private static ShipInstance? FindTarget(ShipInstance ship, ShipInstance? player, LocalBubbleManager bubbleManager)
        {
            var personality = (NewbFlags)ship.Blueprint.ShipClass;
            ShipInstance? nearest = null;
            float nearestDist = float.MaxValue;

            // Check player
            if (player != null && player.IsActive && IsHostileToward(ship, player, personality))
            {
                float d = ship.DistanceSquaredTo(player);
                if (d < nearestDist)
                {
                    nearestDist = d;
                    nearest = player;
                }
            }

            // Check other ships
            foreach (var other in bubbleManager.GetAllActive())
            {
                if (other == ship || !other.IsActive) continue;
                if (other.Blueprint.Name == "Planet" || other.Blueprint.Name == "Sun") continue;

                if (IsHostileToward(ship, other, personality))
                {
                    float d = ship.DistanceSquaredTo(other);
                    if (d < nearestDist)
                    {
                        nearestDist = d;
                        nearest = other;
                    }
                }
            }

            return nearest;
        }

        /// <summary>
        /// Check if a ship should be hostile toward a target.
        /// </summary>
        private static bool IsHostileToward(ShipInstance ship, ShipInstance target, NewbFlags personality)
        {
            var targetClass = (NewbFlags)target.Blueprint.ShipClass;

            if (personality.HasFlag(NewbFlags.Hostile))
                return true;

            if (personality.HasFlag(NewbFlags.BountyHunter))
                return !targetClass.HasFlag(NewbFlags.Cop);

            if (personality.HasFlag(NewbFlags.Pirate))
                return !targetClass.HasFlag(NewbFlags.Cop);

            if (personality.HasFlag(NewbFlags.Cop))
                return targetClass.HasFlag(NewbFlags.Pirate);

            return false;
        }

        /// <summary>
        /// Part 4: Check if ship should bail (launch escape pod).
        /// </summary>
        private static bool ShouldBail(ShipInstance ship, ShipInstance target)
        {
            if (_rng.Next(1000) < 25 && ship.Energy < ship.Blueprint.MaxEnergy * 0.25f)
                return true;

            if (ship.Energy < 10 && _rng.Next(10) == 0)
                return true;

            return false;
        }

        /// <summary>
        /// Part 5: Check if ship should fire a missile.
        /// </summary>
        private static bool ShouldFireMissile(ShipInstance ship, ShipInstance target)
        {
            // TODO: check missile count and E.C.M. when implemented
            return false;
        }

        /// <summary>
        /// Part 6: HITCH routine — check if target is in crosshairs.
        /// </summary>
        private static bool CanFireLaser(ShipInstance ship, ShipInstance target, out bool accurate)
        {
            accurate = false;

            if (ship.Blueprint.LaserPower == 0)
                return false;

            // Target must be in front
            Vector3 toTarget = Vector3.Normalize(target.Position - ship.Position);
            float dot = Vector3.Dot(ship.Orientation.Nosev, toTarget);
            if (dot < 0.9f)
                return false;

            // Project to local space (X=right, Y=up, Z=forward)
            Vector3 localTarget = ship.Orientation.Transform(target.Position - ship.Position);
            if (localTarget.Z <= 0)
                return false;

            // Crosshair alignment
            float xHi = Math.Abs(localTarget.X) / localTarget.Z;
            float yHi = Math.Abs(localTarget.Y) / localTarget.Z;

            float targetArea = ship.Blueprint.Model.Vertices.Count * 0.01f;
            float distSq = xHi * xHi + yHi * yHi;

            if (distSq < targetArea)
            {
                accurate = distSq < targetArea * 0.25f;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Part 6: Fire laser at target.
        /// </summary>
        private static void FireLaser(ShipInstance ship, ShipInstance target, bool hit, LocalBubbleManager bubbleManager)
        {
            if (hit)
            {
                int damage = ship.Blueprint.LaserPower * 16;
                bool destroyed = target.TakeDamage(damage);

                if (destroyed)
                {
                    target.IsActive = false;
                    OnAIDestroyedShip(target, ship, bubbleManager);
                }
                else
                    ship.Speed = Math.Max(0, ship.Speed - 0.5f);
            }

            ship.IsFiring = true;
        }

        /// <summary>
        /// Called when AI destroys a ship — track kills and spawn cargo.
        /// </summary>
        private static void OnAIDestroyedShip(ShipInstance destroyed, ShipInstance destroyer, LocalBubbleManager bubbleManager)
        {
            // Track kill if player was the target (player ship destroyed by AI)
            // For now just spawn cargo — player kills tracked via CollisionSystem

            // Spawn cargo canisters from destroyed ship
            CollisionSystem.SpawnCargoDrops(destroyed, bubbleManager);
        }

        /// <summary>
        /// Part 7: Calculate movement direction based on personality.
        /// </summary>
        private static Vector3 CalculateMovementDirection(ShipInstance ship, ShipInstance target, LocalBubbleManager bubbleManager)
        {
            var personality = (NewbFlags)ship.Blueprint.ShipClass;

            if (personality.HasFlag(NewbFlags.Trader) || ship.AIState == (byte)ShipAIState.Bail)
            {
                var planet = bubbleManager.Planet;
                if (planet != null)
                    return Vector3.Normalize(planet.Position - ship.Position);
            }

            if (personality.HasFlag(NewbFlags.Hostile) || personality.HasFlag(NewbFlags.BountyHunter))
            {
                float dist = ship.DistanceTo(target);
                if (dist > 500)
                    return Vector3.Normalize(target.Position - ship.Position);
                if (dist < 200)
                    return Vector3.Normalize(ship.Position - target.Position);
            }

            return Vector3.Normalize(
                ship.Orientation.Nosev * 0.7f +
                Vector3.Normalize(target.Position - ship.Position) * 0.3f);
        }

        /// <summary>
        /// Part 7: Apply movement.
        /// </summary>
        private static void ApplyMovement(ShipInstance ship, Vector3 direction, Vector3 targetPos)
        {
            ship.FaceTarget(ship.Position + direction * 100f);
            float aggressionFactor = 0.5f + (ship.Aggression / 63f) * 0.5f;
            ship.Speed = Math.Min(ship.Blueprint.MaxSpeed * aggressionFactor, ship.Speed + 0.5f);
        }

        /// <summary>
        /// Aggression-based turning probability.
        /// </summary>
        public static bool ShouldTurnTowardTarget(ShipInstance ship)
        {
            float probability = 0.3f + (ship.Aggression / 63f) * 0.65f;
            return (float)_rng.NextDouble() < probability;
        }

        /// <summary>
        /// Check if a ship should spawn (MCNT every 256, offset 0).
        /// </summary>
        public static bool ShouldSpawnShip(byte dangerLevel, byte altitude)
        {
            // TODO: implement with danger level and altitude
            return _rng.NextDouble() < 0.3;
        }
    }
}
