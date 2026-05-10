using EliteRetro.Core.Entities;
using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Managers;

/// <summary>
/// Manages the player's state, including commander data, ship instance,
/// and runtime vitals (shields, missiles). Single source of truth for player info.
/// </summary>
public class PlayerManager : IPlayerManager
{
    public CommanderData Commander { get; }
    public ShipInstance Ship { get; }
    public byte Missiles { get; set; } = 4;
    public byte ShieldFront { get; set; } = 255;
    public byte ShieldAft { get; set; } = 255;

    public PlayerManager()
    {
        Commander = new CommanderData();
        
        var playerBlueprint = new ShipBlueprint
        {
            Name = "Cobra Mk3",
            Model = CobraMk3Model.Create(2.0f),
            MaxSpeed = GameConstants.SpeedMax,
            MaxEnergy = 255,
            HullStrength = 255,
            ShieldStrength = 255,
        };

        Ship = new ShipInstance(playerBlueprint)
        {
            Position = Vector3.Zero,
            Speed = 0,
            Energy = 200,
            Hull = 255,
            SlotIndex = GameConstants.PlayerSlot,
            IsActive = true,
        };
    }
}
