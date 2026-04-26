namespace EliteRetro.Core.Systems;

public enum GovernmentType { Anarchy, Feudal, MultiGov, Dictatorship, Communist, Confederacy, Democracy, CorporateState }
public enum EconomyType { RichIndustrial, AverageIndustrial, PoorIndustrial, AverageAgri, RichAgri, MainlyAgri }
public enum TechLevel { StoneAge, Rural, Poor, Average, Rich, GalacticHubs }

public record struct StarSystem(
    string Name,
    int GalaxyIndex,
    int SystemIndex,
    Vector2 Position,
    GovernmentType Government,
    EconomyType Economy,
    TechLevel TechLevel,
    int Population,
    int Radius,
    uint Seed)
{
    public string Description => $"{Government} {Economy} tech level {TechLevel}";
}

public record struct Galaxy(int Index, StarSystem[] Systems);
