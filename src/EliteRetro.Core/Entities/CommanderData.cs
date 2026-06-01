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

    /// <summary>
    /// Fuel level (0-70). Stored as a fixed-point byte where 1 unit = 0.1 light-years,
    /// matching the authentic BBC Elite format. The maximum value of 70 represents a
    /// full tank of 7.0 light-years of hyperspace range.
    /// </summary>
    public int Fuel { get; set; } = 70;

    /// <summary>
    /// Cargo capacity in tons. Default 20 for the Cobra Mk III,
    /// matching the authentic BBC Elite commander file format (CRGO at offset 0x15).
    /// </summary>
    public int CargoCapacity { get; set; } = 20;

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
    /// Add a kill to TALLY. Returns true if a notable milestone was reached.
    /// </summary>
    public bool AddKill()
    {
        int oldTally = Tally;
        Tally++;

        // "RIGHT ON COMMANDER!" at every 256 kills (original Elite behavior)
        if (Tally > 0 && Tally % 256 == 0)
            return true;

        // Also check for rank promotion
        CombatRank oldRank = GetRankFromTally(oldTally);
        CombatRank newRank = GetRankFromTally(Tally);
        if (newRank > oldRank)
            return true;

        return false;
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

    /// <summary>
    /// Reset after player death and escape pod launch.
    /// Legal status cleared, all cargo lost, TALLY preserved.
    /// </summary>
    public void ResetAfterEscape()
    {
        LegalStatus = 0;
        CurrentBounty = 0;
        CargoHold.Clear();
        HasFuelScoops = false;
        HasECM = false;
        HasDockingComp = false;
    }

    // --- Equipment Flags ---

    /// <summary>Fuel scoops equipped.</summary>
    public bool HasFuelScoops { get; set; }

    /// <summary>E.C.M. system equipped.</summary>
    public bool HasECM { get; set; }

    /// <summary>Docking computer equipped.</summary>
    public bool HasDockingComp { get; set; }

    /// <summary>Energy bomb equipped.</summary>
    public bool HasEnergyBomb { get; set; }

    /// <summary>Escape pod equipped.</summary>
    public bool HasEscapePod { get; set; }
}
