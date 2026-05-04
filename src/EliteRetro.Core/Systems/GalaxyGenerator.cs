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
    /// Two-letter token table for system name generation (QQ16, indices 128-159).
    /// The 5-bit token index from s2_hi (0-31) maps as: 0=skip, 1-31=QQ16[129-159].
    /// Index 0 is never used (cpl routine skips it). 31 usable tokens.
    /// Source: authentic BBC Elite QQ16 token table.
    /// </summary>
    private static readonly string[] _tokenTable = {
        "",     // 0 (skip — QQ16[128]="AL" is never used)
        "LE", "XE", "GE", "ZA", "CE", "BI", "SO",  // 1-8   (QQ16[129-136])
        "US", "ES", "AR", "MA", "IN", "DI", "RE", "A",   // 9-16 (QQ16[137-144])
        "ER", "AT", "EN", "BE", "RA", "LA", "VE", "TI",  // 17-24 (QQ16[145-152])
        "ED", "OR", "QU", "AN", "TE", "IS", "RI", "ON"   // 25-31 (QQ16[153-159])
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

        uint flavourSeed = (uint)(seed.W0 ^ (seed.W1 << 8) ^ (seed.W2 << 4));
        string flavour = BuildFlavourText(name, species!, economyType, flavourSeed);

        return new StarSystem(
            name, galaxyIndex, systemIndex, position,
            govType, economyType, techLevel, population, radius,
            flavourSeed,
            productivity,
            species!,
            flavour);
    }

    /// <summary>Planet description line for galactic chart data panel (BBC Elite style).</summary>
    private static string BuildFlavourText(string name, string inhabitants, EconomyType economy, uint seed)
    {
        if (string.IsNullOrEmpty(name))
            name = "This System";

        string[] features =
        {
            "weird volcanoes", "icy caves", "historic caves", "mud ballet",
            "zero-G poets", "vacuum orchids", "glass forests", "dust hurricanes"
        };
        string[] fauna =
        {
            "mountain lobstoid", "tree wolf", "walking shrimp", "mud ape",
            "radio cockroach", "stellar mongoose", "fang truffle"
        };

        int fi = (int)(seed % features.Length);
        int fa = (int)((seed >> 8) % fauna.Length);
        string demonym = name.Length >= 3 ? $"{name}ian" : $"{name} locals";

        return $"The world {name} is fabled for its {features[fi]} and the {demonym} {fauna[fa]}.";
    }

    /// <summary>
    /// Generate system name using the cpl routine.
    /// Takes bits 0-4 of s2_hi (0-31) to index the token table.
    /// Index 0 is skipped (no token output), indices 1-31 map to QQ16[129-159].
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
            // Take bits 0-4 of s2_hi (0-31)
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
