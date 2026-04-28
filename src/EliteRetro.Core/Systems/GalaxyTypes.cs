using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Systems;

public enum GovernmentType
{
    Anarchy, Feudal, MultiGov, Dictatorship,
    Communist, Confederacy, Democracy, CorpState
}

public enum EconomyType
{
    RichIndustrial, AverageIndustrial, PoorIndustrial, MainlyIndustrial,
    MainlyAgricultural, RichAgricultural, AverageAgricultural, PoorAgricultural
}

public record struct StarSystem(
    string Name,
    int GalaxyIndex,
    int SystemIndex,
    Vector2 Position,
    GovernmentType Government,
    EconomyType Economy,
    int TechLevel,
    int Population,
    int Radius,
    uint Seed)
{
    public string Description => $"{Government} {Economy} tech level {TechLevel}";
}

public record struct Galaxy(int Index, StarSystem[] Systems);
