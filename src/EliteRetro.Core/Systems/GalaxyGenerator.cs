using EliteRetro.Core.Utilities;

namespace EliteRetro.Core.Systems;

/// <summary>
/// Procedurally generates 8 galaxies of 256 star systems each.
/// Uses seeded RNG ensuring identical galaxies across runs.
/// Algorithm inspired by original Elite's galaxy generation.
/// </summary>
public class GalaxyGenerator
{
    private const int Galaxies = 8;
    private const int SystemsPerGalaxy = 256;

    private static readonly string[] _namePrefixes = {
        "Al", "Be", "Ca", "Da", "Ed", "Fe", "Ga", "Ha",
        "Ir", "Je", "Ka", "La", "Ma", "Na", "Or", "Pe",
        "Qu", "Re", "Se", "Te", "Ur", "Ve", "Wa", "Xe",
        "Ya", "Ze", "An", "Bi", "Ci", "Di", "El", "Fl"
    };

    private static readonly string[] _nameSuffixes = {
        "a", "e", "i", "o", "u", "an", "en", "in",
        "on", "un", "ar", "er", "ir", "or", "ur", "as",
        "es", "is", "os", "us", "al", "el", "il", "ol",
        "ul", "ax", "ex", "ix", "ox", "ux", "ant", "ent"
    };

    /// <summary>
    /// Generate all 8 galaxies.
    /// </summary>
    public Galaxy[] GenerateAllGalaxies()
    {
        var galaxies = new Galaxy[Galaxies];
        for (int g = 0; g < Galaxies; g++)
        {
            galaxies[g] = GenerateGalaxy(g);
        }
        return galaxies;
    }

    /// <summary>
    /// Generate a single galaxy with deterministic seeded generation.
    /// </summary>
    public Galaxy GenerateGalaxy(int galaxyIndex)
    {
        var systems = new StarSystem[SystemsPerGalaxy];
        uint galaxySeed = (uint)(galaxyIndex * 31337 + 12345);
        var rng = new SeededRandom(galaxySeed);

        for (int i = 0; i < SystemsPerGalaxy; i++)
        {
            uint systemSeed = rng.Next();
            systems[i] = GenerateSystem(galaxyIndex, i, systemSeed, ref rng);
        }

        return new Galaxy(galaxyIndex, systems);
    }

    private StarSystem GenerateSystem(int galaxyIndex, int systemIndex, uint seed, ref SeededRandom rng)
    {
        // Generate name from seeded components
        var nameRng = new SeededRandom(seed);
        string name = nameRng.Pick(_namePrefixes) + nameRng.Pick(_nameSuffixes);
        if (nameRng.Next(10) < 3)
            name += " " + (nameRng.Next(9) + 1);

        // Position in galaxy (spiral-ish distribution)
        float angle = (systemIndex / (float)SystemsPerGalaxy) * MathF.Tau * 3 + galaxyIndex;
        float radius = 500 + (systemIndex / (float)SystemsPerGalaxy) * 2000;
        var position = new Vector2(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius);

        // Government derived from seed
        GovernmentType government = (GovernmentType)(seed % 8);

        // Economy derived from government and seed
        EconomyType economy = government switch
        {
            GovernmentType.Anarchy or GovernmentType.Dictatorship => (EconomyType)(seed % 3),
            GovernmentType.Democracy or GovernmentType.CorporateState => (EconomyType)(3 + seed % 3),
            _ => (EconomyType)(1 + seed % 4)
        };

        // Tech level
        TechLevel techLevel = (TechLevel)(seed % 6);

        // Population based on economy
        int population = economy switch
        {
            EconomyType.RichIndustrial or EconomyType.RichAgri => rng.Next(10, 50),
            EconomyType.AverageIndustrial or EconomyType.AverageAgri => rng.Next(5, 20),
            _ => rng.Next(1, 10)
        };

        // Radius in km
        int radiusKm = rng.Next(1000, 20000);

        return new StarSystem(
            name, galaxyIndex, systemIndex, position,
            government, economy, techLevel, population, radiusKm, seed);
    }
}
