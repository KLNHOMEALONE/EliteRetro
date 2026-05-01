using EliteRetro.Core.Entities;
using EliteRetro.Core.Managers;

namespace EliteRetro.Core.Systems;

/// <summary>
/// Ship spawning system — selects ship types based on danger level and altitude.
/// Implements pack spawning and slot allocation via LocalBubbleManager.
/// </summary>
public static class SpawnSystem
{
    private static readonly Random _rng = new Random();

    /// <summary>
    /// Danger level (0-7) based on distance from station and system government.
    /// </summary>
    public static byte CalculateDangerLevel(float altitude, GovernmentType government)
    {
        // Danger increases with altitude (distance from station)
        // Altitude 0-6 = safe, 7+ = dangerous
        byte level = (byte)Math.Clamp(altitude / 10, 0, 7);

        // Anarchy/Feudal systems are more dangerous
        if (government == GovernmentType.Anarchy)
            level = (byte)Math.Min(level + 2, 7);
        else if (government == GovernmentType.Feudal)
            level = (byte)Math.Min(level + 1, 7);

        return level;
    }

    /// <summary>
    /// Select a ship type to spawn based on danger level.
    /// </summary>
    public static ShipBlueprint SelectShipType(byte dangerLevel)
    {
        // Ship pools by danger level
        if (dangerLevel <= 1)
        {
            // Safe: traders, transports
            return _rng.NextDouble() < 0.5
                ? CreateTransporter()
                : CreateCobraMk3();
        }
        if (dangerLevel <= 3)
        {
            // Moderate: mix of traders and mild threats
            return _rng.NextDouble() switch
            {
                < 0.3f => CreateTransporter(),
                < 0.6f => CreateCobraMk3(),
                < 0.8f => CreateViper(),
                _ => CreateAdder(),
            };
        }
        if (dangerLevel <= 5)
        {
            // Dangerous: pirates, hunters
            return _rng.NextDouble() switch
            {
                < 0.2f => CreatePython(),
                < 0.5f => CreateViper(),
                < 0.7f => CreateAdder(),
                < 0.9f => CreateMamba(),
                _ => CreateAspMk2(),
            };
        }
        // Very dangerous: Thargoids, elite ships
        return _rng.NextDouble() switch
        {
            < 0.1f => CreateThargoid(),
            < 0.3f => CreatePython(),
            < 0.6f => CreateMamba(),
            < 0.8f => CreateFerDeLance(),
            _ => CreateKrait(),
        };
    }

    /// <summary>
    /// Try to spawn a ship in the local bubble.
    /// </summary>
    public static bool TrySpawnShip(LocalBubbleManager bubbleManager, byte dangerLevel, float altitude)
    {
        if (!ShouldSpawn(dangerLevel, altitude))
            return false;

        var blueprint = SelectShipType(dangerLevel);
        var instance = CreateInstance(blueprint, bubbleManager);
        if (instance == null)
            return false;

        return bubbleManager.TrySpawn(instance);
    }

    /// <summary>
    /// Pack spawn — spawn 2-4 ships of the same type together.
    /// </summary>
    public static int TrySpawnPack(LocalBubbleManager bubbleManager, byte dangerLevel, float altitude)
    {
        if (!ShouldSpawn(dangerLevel, altitude))
            return 0;

        var blueprint = SelectShipType(dangerLevel);
        int packSize = 2 + _rng.Next(3); // 2-4 ships
        int spawned = 0;

        for (int i = 0; i < packSize; i++)
        {
            var instance = CreateInstance(blueprint, bubbleManager, offset: i * 50);
            if (instance != null && bubbleManager.TrySpawn(instance))
                spawned++;
        }

        return spawned;
    }

    /// <summary>
    /// Determine if a ship should spawn based on danger level and altitude.
    /// </summary>
    private static bool ShouldSpawn(byte dangerLevel, float altitude)
    {
        // Higher danger = higher spawn probability
        // Low altitude (near station) = lower spawn probability
        float probability = (dangerLevel / 8f) * (1f - Math.Clamp(altitude / 30f, 0, 0.8f));
        return _rng.NextDouble() < probability;
    }

    /// <summary>
    /// Create a ship instance with random position and orientation.
    /// </summary>
    private static ShipInstance? CreateInstance(ShipBlueprint blueprint, LocalBubbleManager bubbleManager, float offset = 0)
    {
        // Random position outside player's immediate vicinity
        float angle = (float)_rng.NextDouble() * MathF.Tau;
        float distance = 200 + (float)_rng.NextDouble() * 300 + offset;
        float x = MathF.Cos(angle) * distance;
        float y = MathF.Sin(angle) * distance * 0.3f; // Flattened distribution
        float z = -(float)_rng.NextDouble() * distance;

        var instance = new ShipInstance(blueprint)
        {
            Position = new Vector3(x, y, z),
            Speed = blueprint.MaxSpeed * (0.3f + (float)_rng.NextDouble() * 0.4f),
            Aggression = (byte)_rng.Next(64),
        };

        // Random orientation facing roughly toward planet/player
        instance.Orientation = new OrientationMatrix
        {
            Nosev = Vector3.Normalize(-instance.Position),
            Roofv = Vector3.UnitY,
            Sidev = Vector3.UnitX,
        };
        instance.Orientation.Tidy();

        // Set personality based on ship type
        instance.AIState = (byte)ShipAIState.Patrol;

        return instance;
    }

    // Factory methods for ship blueprints
    private static ShipBlueprint CreateTransporter() => new()
    {
        Name = "Transporter",
        Model = TransporterModel.Create(24),
        MaxSpeed = 0.8f,
        MaxEnergy = 128,
        HullStrength = 100,
        ShieldStrength = 50,
        LaserPower = 0,
        ShipClass = (byte)NewbFlags.Trader,
        Bounty = 0,
        CargoCapacity = 8,
    };

    private static ShipBlueprint CreateCobraMk3() => new()
    {
        Name = "Cobra Mk3",
        Model = CobraMk3Model.Create(24),
        MaxSpeed = 1.2f,
        MaxEnergy = 160,
        HullStrength = 150,
        ShieldStrength = 100,
        LaserPower = 2,
        ShipClass = (byte)NewbFlags.None,
        Bounty = 0,
        CargoCapacity = 4,
    };

    private static ShipBlueprint CreateViper() => new()
    {
        Name = "Viper",
        Model = ViperModel.Create(24),
        MaxSpeed = 1.4f,
        MaxEnergy = 160,
        HullStrength = 120,
        ShieldStrength = 120,
        LaserPower = 2,
        ShipClass = (byte)NewbFlags.BountyHunter,
        Bounty = 0,
        CargoCapacity = 0,
    };

    private static ShipBlueprint CreateAdder() => new()
    {
        Name = "Adder",
        Model = AdderModel.Create(24),
        MaxSpeed = 1.0f,
        MaxEnergy = 140,
        HullStrength = 100,
        ShieldStrength = 80,
        LaserPower = 1,
        ShipClass = (byte)NewbFlags.Pirate,
        Bounty = 50,
        CargoCapacity = 6,
    };

    private static ShipBlueprint CreateMamba() => new()
    {
        Name = "Mamba",
        Model = MambaModel.Create(24),
        MaxSpeed = 1.5f,
        MaxEnergy = 180,
        HullStrength = 140,
        ShieldStrength = 140,
        LaserPower = 3,
        ShipClass = (byte)(NewbFlags.Pirate | NewbFlags.Hostile),
        Bounty = 200,
        CargoCapacity = 2,
    };

    private static ShipBlueprint CreatePython() => new()
    {
        Name = "Python",
        Model = PythonModel.Create(24),
        MaxSpeed = 1.1f,
        MaxEnergy = 200,
        HullStrength = 200,
        ShieldStrength = 160,
        LaserPower = 3,
        ShipClass = (byte)NewbFlags.None,
        Bounty = 100,
        CargoCapacity = 10,
    };

    private static ShipBlueprint CreateAspMk2() => new()
    {
        Name = "Asp Mk2",
        Model = AspMk2Model.Create(24),
        MaxSpeed = 1.3f,
        MaxEnergy = 180,
        HullStrength = 160,
        ShieldStrength = 140,
        LaserPower = 3,
        ShipClass = (byte)NewbFlags.BountyHunter,
        Bounty = 150,
        CargoCapacity = 6,
    };

    private static ShipBlueprint CreateFerDeLance() => new()
    {
        Name = "Fer-de-Lance",
        Model = FerDeLanceModel.Create(24),
        MaxSpeed = 1.6f,
        MaxEnergy = 200,
        HullStrength = 180,
        ShieldStrength = 160,
        LaserPower = 4,
        ShipClass = (byte)(NewbFlags.Hostile | NewbFlags.BountyHunter),
        Bounty = 500,
        CargoCapacity = 0,
    };

    private static ShipBlueprint CreateKrait() => new()
    {
        Name = "Krait",
        Model = KraitModel.Create(24),
        MaxSpeed = 1.4f,
        MaxEnergy = 180,
        HullStrength = 160,
        ShieldStrength = 140,
        LaserPower = 3,
        ShipClass = (byte)NewbFlags.Pirate,
        Bounty = 300,
        CargoCapacity = 4,
    };

    private static ShipBlueprint CreateThargoid() => new()
    {
        Name = "Thargoid",
        Model = ThargoidModel.Create(24),
        MaxSpeed = 1.8f,
        MaxEnergy = 255,
        HullStrength = 255,
        ShieldStrength = 255,
        LaserPower = 4,
        ShipClass = (byte)NewbFlags.Hostile,
        Bounty = 1000,
        CargoCapacity = 0,
    };
}
