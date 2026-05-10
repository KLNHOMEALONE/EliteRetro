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

        // 4. Assemble HUDState
        return new HUDState
        {
            Speed = playerSpeed,
            Energy = playerManager.Ship.Energy,
            MaxEnergy = 255,
            Fuel = playerManager.Commander.Fuel,
            CabinTemp = 0,
            LaserTemp = 0,
            Altitude = altitude,
            EnergyBanks = 0,
            Missiles = playerManager.Missiles,
            MaxMissiles = 4,
            ShieldForward = playerManager.Ship.Energy, // Simplified mapping as baseline
            ShieldAft = playerManager.Ship.Energy,
            Pitch = pitchRate,
            Roll = rollRate,
            CompassHeading = cumulativeRoll,
            ECMBulbs = 0,
            ViewMode = viewModeName,
            StatusMessage = eventMessageTimer > 0 ? eventMessage : statusMsg,
            StatusColor = eventMessageTimer > 0 ? Color.Gold : statusColor,
            LegalStatus = playerManager.Commander.LegalStatus,
            CombatRank = playerManager.Commander.RankName,
            ShowHiddenEdges = showHiddenEdges
        };
    }
}
