using System.Linq;

namespace EliteRetro.Core.Entities;

/// <summary>
/// Combat rank based on TALLY (total kills).
/// </summary>
public enum CombatRank
{
    Harmless = 0,
    MostlyHarmless = 1,
    Poor = 2,
    Average = 3,
    AboveAverage = 4,
    Competent = 5,
    Dangerous = 6,
    Deadly = 7,
    Elite = 8
}

/// <summary>
/// Player commander persistent data.
/// Tracks kills (TALLY), credits, cargo, and legal status.
/// </summary>
public class CommanderData
{
    /// <summary>Total kills (TALLY, 16-bit in original).</summary>
    public int Tally { get; set; }

    /// <summary>Credit balance (×100 Cr).</summary>
    public int Credits { get; set; }

    /// <summary>Legal status: 0=clean, 1=fugitive, 2=offender, 3=criminal.</summary>
    public byte LegalStatus { get; set; }

    /// <summary>Current bounty on player's head (in credits).</summary>
    public int CurrentBounty { get; set; }

    /// <summary>Fuel level (0-70). Each unit = 1 light-year jump capacity.</summary>
    public int Fuel { get; set; } = 35;

    /// <summary>Cargo capacity in tons. Default 10.</summary>
    public int CargoCapacity { get; set; } = 10;

    /// <summary>Cargo hold (commodity index → tons).</summary>
    public Dictionary<int, int> CargoHold { get; } = new();

    /// <summary>Combat rank derived from TALLY.</summary>
    public CombatRank Rank => GetRankFromTally(Tally);

    /// <summary>Rank display name.</summary>
    public string RankName => Rank.ToString();

    /// <summary>
    /// Derive combat rank from TALLY kill count.
    /// </summary>
    public static CombatRank GetRankFromTally(int tally)
    {
        if (tally < 8) return CombatRank.Harmless;
        if (tally < 16) return CombatRank.MostlyHarmless;
        if (tally < 32) return CombatRank.Poor;
        if (tally < 64) return CombatRank.Average;
        if (tally < 128) return CombatRank.AboveAverage;
        if (tally < 512) return CombatRank.Competent;
        if (tally < 2560) return CombatRank.Dangerous;
        if (tally < 6400) return CombatRank.Deadly;
        return CombatRank.Elite;
    }

    /// <summary>
    /// Add a kill to TALLY.
    /// </summary>
    public void AddKill()
    {
        Tally++;
    }

    /// <summary>
    /// Add cargo to the hold. Returns amount actually added (may be 0 if full).
    /// </summary>
    public int AddCargo(int commodityIndex, int amount = 1)
    {
        int currentTotal = CargoHold.Values.Sum();
        int space = CargoCapacity - currentTotal;
        if (space <= 0) return 0;

        int toAdd = Math.Min(amount, space);
        if (CargoHold.TryGetValue(commodityIndex, out int current))
            CargoHold[commodityIndex] = current + toAdd;
        else
            CargoHold[commodityIndex] = toAdd;
        return toAdd;
    }
}
