using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Systems;

/// <summary>
/// Procedurally generates 8 galaxies of 256 star systems each using
/// the authentic BBC Elite Tribonacci twist algorithm.
/// All system data is derived deterministically from three 16-bit seeds.
/// </summary>
public class GalaxyGenerator
{
    private const int Galaxies = 8;
    private const int SystemsPerGalaxy = 256;

    /// <summary>
    /// Two-letter token table for system name generation (indices 129-159).
    /// Index 0 = skip. 31 entries total.
    /// </summary>
    private static readonly string[] _tokenTable = {
        "",     // 0 (skip)
        "LE", "XE", "GE", "IN", "EN", "VE", "ER", "US",  // 1-8
        "XE", "ZA", "SO", "CR", "DI", "RE", "A",  "ER",  // 9-16
        "DU", "CE", "BI", "RA", "LA", "VE", "TI", "ED",  // 17-24
        "RI", "US", "ER", "BE", "SA", "VI", "ON"         // 25-31
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
    /// Generate a single galaxy using the Tribonacci twist algorithm.
    /// </summary>
    public Galaxy GenerateGalaxy(int galaxyIndex)
    {
        var seed = GalaxySeed.Galaxy0System0;

        // Advance to target galaxy
        for (int g = 0; g < galaxyIndex; g++)
        {
            seed.NextGalaxy();
        }

        var systems = new StarSystem[SystemsPerGalaxy];
        for (int i = 0; i < SystemsPerGalaxy; i++)
        {
            systems[i] = GenerateSystem(galaxyIndex, i, seed);

            // Twist 4 times to advance to next system
            seed.Twist();
            seed.Twist();
            seed.Twist();
            seed.Twist();
        }

        return new Galaxy(galaxyIndex, systems);
    }

    /// <summary>
    /// Generate a single star system from the current seed state.
    /// </summary>
    private StarSystem GenerateSystem(int galaxyIndex, int systemIndex, GalaxySeed seed)
    {
        byte s0Lo = seed.W0Lo;
        byte s0Hi = seed.W0Hi;
        byte s1Lo = seed.W1Lo;
        byte s1Hi = seed.W1Hi;
        byte s2Lo = seed.W2Lo;
        byte s2Hi = seed.W2Hi;

        // Government: bits 3-5 of s1_lo
        int government = (s1Lo >> 3) & 0b111;

        // Economy: bits 0-2 of s0_hi
        int economy = s0Hi & 0b111;
        // Constraint: Anarchy (0) or Feudal (1) → force bit 1 (not rich)
        if (government <= 1)
            economy |= 0b010;

        // Flipped economy (3-bit inversion)
        int flippedEconomy = (~economy) & 0b111;

        // Tech level: flipped_economy + (s1_hi & 0b11) + (government / 2, rounding up)
        int techLevel = flippedEconomy + (s1Hi & 0b11) + ((government + 1) / 2);
        techLevel = Math.Clamp(techLevel, 0, 14);

        // Population: (techLevel * 4) + economy + government + 1
        int population = (techLevel * 4) + economy + government + 1;
        population = Math.Clamp(population, 1, 71);

        // Productivity: (flipped_economy + 3) * (government + 4) * population * 8
        long productivity = (long)(flippedEconomy + 3) * (government + 4) * population * 8;

        // Average radius: ((s2_hi & 0x0F) + 11) * 256 + s1_hi
        int radius = ((s2Hi & 0x0F) + 11) * 256 + s1Hi;

        // Galactic coordinates
        float galacticX = s1Hi;
        float galacticY = s0Hi >> 1;

        // Species
        string? species = null;
        if ((s2Lo & 0x80) != 0)
        {
            // Alien species
            int a1 = (s2Hi >> 2) & 0b111;
            int a2 = (s2Hi >> 5) & 0b111;
            int a3 = (s0Hi ^ s1Hi) & 0b111;
            int a4 = ((s2Hi & 0b11) + a3) & 0b111;

            string adj1 = a1 switch { 0 => "Large ", 1 => "Fierce ", 2 => "Small ", _ => "" };
            string adj2 = a2 switch
            {
                0 => "Green ", 1 => "Red ", 2 => "Yellow ",
                3 => "Blue ", 4 => "Black ", 5 => "Harmless ", _ => ""
            };
            string adj3 = a3 switch
            {
                0 => "Slimy ", 1 => "Bug-Eyed ", 2 => "Horned ",
                3 => "Bony ", 4 => "Fat ", 5 => "Furry ", _ => ""
            };
            string type = a4 switch
            {
                0 => "Rodents", 1 => "Frogs", 2 => "Lizards", 3 => "Lobsters",
                4 => "Birds", 5 => "Humanoids", 6 => "Felines", 7 => "Insects",
                _ => "Mammals"
            };

            species = adj1 + adj2 + adj3 + type;
        }
        else
        {
            species = "Human Colonials";
        }

        // Generate name
        string name = GenerateName(ref seed);

        // Map economy int to enum
        EconomyType economyType = (EconomyType)economy;
        GovernmentType govType = (GovernmentType)government;

        // Position in galaxy (spiral distribution based on galactic coords)
        var position = new Vector2(galacticX, galacticY);

        return new StarSystem(
            name, galaxyIndex, systemIndex, position,
            govType, economyType, techLevel, population, radius,
            (uint)(seed.W0 ^ (seed.W1 << 8) ^ (seed.W2 << 4)));
    }

    /// <summary>
    /// Generate system name using the cpl routine.
    /// Takes bits 0-4 of s2_hi after each twist to index the token table.
    /// </summary>
    private static string GenerateName(ref GalaxySeed seed)
    {
        // Backup seed for restoration
        var original = seed;

        bool fourTokens = (seed.W0Lo & 0x40) != 0;
        int tokenCount = fourTokens ? 4 : 3;

        var nameParts = new List<string>();

        for (int i = 0; i < tokenCount; i++)
        {
            // Take bits 0-4 of s2_hi
            int tokenIndex = seed.W2Hi & 0b11111;
            if (tokenIndex > 0 && tokenIndex < _tokenTable.Length)
            {
                nameParts.Add(_tokenTable[tokenIndex]);
            }
            seed.Twist();
        }

        // Restore original seed
        seed = original;

        return string.Concat(nameParts);
    }
}
