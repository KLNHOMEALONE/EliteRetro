using Microsoft.Xna.Framework;
using EliteRetro.Core.HUD;
using EliteRetro.Core.Managers;
using EliteRetro.Core.Entities;

namespace EliteRetro.Core.Systems;

/// <summary>
/// Implementation of IHudService that assembles dashboard state from world data.
/// Owns the authentic BBC Elite altitude and status indicator logic.
/// </summary>
public class HudService : IHudService
{
    public HUDState CalculateState(
        IGameContext game,
        float playerSpeed,
        float pitchRate,
        float rollRate,
        float cumulativeRoll,
        int viewMode,
        string eventMessage,
        int eventMessageTimer,
        bool showHiddenEdges)
    {
        var bubbleManager = game.BubbleManager;
        var playerManager = game.PlayerManager;

        // 1. Status Message & Color
        var sunEffect = playerManager.CheckSunProximity(bubbleManager);
        string statusMsg = "";
        Color statusColor = Color.Gray;

        if (sunEffect == SunProximityEffect.Fatal)
        {
            statusMsg = "DANGER - FATAL PROXIMITY";
            statusColor = Color.Red;
        }
        else if (sunEffect == SunProximityEffect.FuelScoop)
        {
            statusMsg = "FUEL SCOOP ACTIVE";
            statusColor = Color.Green;
        }
        else if (sunEffect == SunProximityEffect.HeatWarning)
        {
            statusMsg = "HEAT WARNING";
            statusColor = Color.Orange;
        }

        if (bubbleManager.SunOrStation?.Blueprint?.Name == "Coriolis Station")
        {
            statusMsg = "STATION IN VIEW";
            statusColor = Color.Yellow;
        }

        // 2. Altitude Calculation (Authentic BBC Algorithm)
        int altitude = 255;
        var planet = bubbleManager.Planet;
        if (planet != null)
        {
            float dist = planet.Position.Length();
            // Nose dot relative to player view (player always at origin looking at Z+ internal)
            float noseDot = planet.Position.Z / dist; 
            float angleToCenter = MathF.Acos(MathHelper.Clamp(noseDot, -1, 1));
            float angularRadius = MathF.Asin(MathHelper.Clamp(GameConstants.PlanetRadius / dist, 0, 1));
            float clearanceAngle = angleToCenter - angularRadius;
            
            // Effective altitude combines physical height and angular clearance
            float height = dist - GameConstants.PlanetRadius;
            float clearanceBoost = Math.Max(0, clearanceAngle) * (GameConstants.PlanetRadius * 4.0f);
            float overflyClearance = GameConstants.PlanetRadius * 0.4f * MathF.Sin(angleToCenter);
            
            float effectiveAlt = Math.Max(height + clearanceBoost, overflyClearance);
            
            // Scale: 0.4 planet radii = max bar.
            altitude = MathHelper.Clamp((int)(effectiveAlt / (GameConstants.PlanetRadius * 0.4f) * 255), 0, 255);
        }

        // 3. View Mode Name
        string viewModeName = viewMode switch
        {
            1 => "REAR",
            2 => "LEFT",
            3 => "RIGHT",
            _ => "FRONT"
        };

        // 4. Space Compass Bearing
        Vector2 bearing = Vector2.Zero;
        var target = bubbleManager.SunOrStation;
        if (target != null)
        {
            // Space Compass is a 2D projection of the 3D unit vector to the target in player's local frame
            // Player is always at origin looking at Z+ ahead in local space after rotation.
            // But we already have the target position in bubble-space which IS rotated relative to player.
            // Z+ is ahead, X+ is right, Y+ is up.
            Vector3 rel = target.Position;
            
            if (rel.Length() > 1f)
            {
                Vector3 norm = Vector3.Normalize(rel);
                bearing = new Vector2(norm.X, -norm.Y); // Y is up in world, but down on screen
            }
        }

        // 5. Missile Lock Detection
        bool targetLocked = false;
        if (playerManager.Missiles > 0)
        {
            // Simple lock logic: any ship (not sun/planet) within 600m and near center view
            // Using logic similar to CombatService hit detection
            Vector3 forward = viewMode switch 
            { 
                0 => new Vector3(0, 0, 1), 
                1 => new Vector3(0, 0, -1), 
                2 => new Vector3(-1, 0, 0), 
                3 => new Vector3(1, 0, 0), 
                _ => new Vector3(0, 0, 1) 
            };

            foreach (var entity in bubbleManager.GetActiveShips()) 
            { 
                float distSq = entity.Position.LengthSquared(); 
                if (distSq > 600 * 600) continue; 

                float dist = (float)Math.Sqrt(distSq);
                float dot = (dist < 5.0f) ? 1.0f : Vector3.Dot(forward, entity.Position / dist); 
                if (dot >= 0.95f) 
                { 
                    targetLocked = true;
                    break;
                } 
            }
        }

        // 6. Assemble HUDState
        return new HUDState
        {
            Speed = playerSpeed,
            Energy = playerManager.Ship.Energy,
            MaxEnergy = 255,
            Fuel = playerManager.Commander.Fuel,
            CabinTemp = 0,
            LaserTemp = 0,
            Altitude = altitude,
            EnergyBanks = (int)MathF.Ceiling(playerManager.Ship.Shields / 16f), // Map 255 shields to ~16 banks
            Missiles = playerManager.Missiles,
            MaxMissiles = 4,
            ShieldForward = playerManager.ShieldFront,
            ShieldAft = playerManager.ShieldAft,
            Pitch = pitchRate,
            Roll = rollRate,
            CompassHeading = cumulativeRoll,
            ECMBulbs = 0,
            HasFuelScoop = playerManager.Commander.HasFuelScoops,
            HasECM = playerManager.Commander.HasECM,
            HasDockingComputer = playerManager.Commander.HasDockingComp,
            StationInView = bubbleManager.SunOrStation?.Blueprint?.Name == "Coriolis Station",
            TargetBearing = bearing,
            TargetLocked = targetLocked,
            ViewMode = viewModeName,
            StatusMessage = eventMessageTimer > 0 ? eventMessage : statusMsg,
            StatusColor = eventMessageTimer > 0 ? Color.Gold : statusColor,
            LegalStatus = playerManager.Commander.LegalStatus,
            CombatRank = playerManager.Commander.RankName,
            ShowHiddenEdges = showHiddenEdges
        };
    }
}
