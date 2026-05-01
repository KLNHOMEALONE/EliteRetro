using EliteRetro.Core.Entities;

namespace EliteRetro.Core.Systems;

/// <summary>
/// Commodity types for trading (QQ23 commodity table).
/// 16 commodities matching original Elite.
/// </summary>
public enum CommodityType
{
    Food = 0,
    Textiles = 1,
    Narcotics = 2,
    Luxuries = 3,
    Furs = 4,
    LiquorWines = 5,
    Metals = 6,
    Gold = 7,
    Platinum = 8,
    GemStones = 9,
    Aliens = 10,
    Firearms = 11,
    Medical = 12,
    Machines = 13,
    Alcohols = 14,
    Computers = 15
}

/// <summary>
/// Static commodity data: base price, base quantity, price variance mask,
/// economy price factor, and economy quantity factor.
/// Matches original Elite QQ23 table.
/// </summary>
public readonly struct CommodityData
{
    public readonly string Name;
    public readonly int BasePrice;        // ×4 Cr (display price = BasePrice × 4)
    public readonly int BaseQuantity;     // 0-63 range
    public readonly int PriceRandomMask;  // rand & mask added to price
    public readonly int QuantityRandomMask; // rand & mask added to quantity
    public readonly int EconomyPriceFactor;  // × economy (0-7)
    public readonly int EconomyQuantityFactor; // - economy × factor
    public readonly int TechMin;          // minimum tech level to produce
    public readonly int TechMax;          // maximum tech level to produce

    public CommodityData(string name, int basePrice, int baseQuantity,
        int priceMask, int qtyMask, int ecoPrice, int ecoQty,
        int techMin, int techMax)
    {
        Name = name;
        BasePrice = basePrice;
        BaseQuantity = baseQuantity;
        PriceRandomMask = priceMask;
        QuantityRandomMask = qtyMask;
        EconomyPriceFactor = ecoPrice;
        EconomyQuantityFactor = ecoQty;
        TechMin = techMin;
        TechMax = techMax;
    }
}

/// <summary>
/// Market prices and availability for a single system.
/// Generated from system economy, tech level, and a random seed.
/// </summary>
public class MarketState
{
    /// <summary>Prices per commodity (index = CommodityType int value).</summary>
    public int[] Prices { get; } = new int[16];

    /// <summary>Availability per commodity (0-63 tons).</summary>
    public int[] Availability { get; } = new int[16];

    public MarketState() { }

    public MarketState(int[] prices, int[] availability)
    {
        Array.Copy(prices, Prices, 16);
        Array.Copy(availability, Availability, 16);
    }
}

/// <summary>
/// Market/trading system — commodity pricing and availability.
/// Implements QQ23 market table from original Elite.
/// </summary>
public static class MarketSystem
{
    /// <summary>
    /// Commodity data table (QQ23). 16 entries matching original Elite.
    /// Prices are ×4 Cr (e.g. Food base=20 → 80 Cr per ton).
    /// </summary>
    public static readonly CommodityData[] Commodities = {
        new("Food",            20, 35,  31,  63, -3,  6, 0, 3),
        new("Textiles",        15, 30,  15,  31, -1,  4, 0, 3),
        new("Narcotics",      150, 10, 127,  15, 15,  2, 8, 14),
        new("Luxuries",        90, 10,  63,  31, 11,  2, 5, 14),
        new("Furs",            70, 15,  63,  31,  5,  3, 2, 7),
        new("Liquor/Wines",    45, 20,  31,  31,  3,  4, 0, 5),
        new("Metals",          35, 25,  31,  63, -1,  5, 0, 4),
        new("Gold",           200,  5, 127,  15, 21,  1, 7, 14),
        new("Platinum",       300,  3, 127,  15, 29,  1, 9, 14),
        new("Gem-Str",        150,  8, 127,  15, 15,  2, 8, 14),
        new("Aliens",         100,  5, 127,  15, 11,  1, 5, 14),
        new("Firearms",        50, 20,  31,  31,  3,  4, 1, 6),
        new("Medical",         30, 25,  31,  63, -1,  5, 0, 4),
        new("Machines",        40, 20,  31,  31,  1,  4, 1, 5),
        new("Alcohols",        25, 30,  15,  31, -1,  4, 0, 3),
        new("Computers",       60, 15,  63,  31,  5,  3, 3, 8),
    };

    /// <summary>
    /// Calculate market prices and availability for a system.
    /// </summary>
    /// <param name="economy">Economy type (0-7).</param>
    /// <param name="techLevel">Tech level (0-14).</param>
    /// <param name="seed">Random seed for variation.</param>
    public static MarketState GenerateMarket(EconomyType economy, int techLevel, int seed)
    {
        int ecoIndex = (int)economy;
        var prices = new int[16];
        var availability = new int[16];
        var rng = new Random(seed);

        for (int i = 0; i < 16; i++)
        {
            var c = Commodities[i];

            // Price = (base + (rand & mask) + economy × factor) × 4
            int price = c.BasePrice + (rng.Next(c.PriceRandomMask + 1)) + ecoIndex * c.EconomyPriceFactor;
            prices[i] = price * 4; // ×4 Cr

            // Tech level adjustment: commodities outside tech range are scarce/expensive
            if (techLevel < c.TechMin)
            {
                prices[i] *= 2;
                availability[i] = 0;
            }
            else if (techLevel > c.TechMax)
            {
                prices[i] = prices[i] * 2 / 3;
            }

            // Availability = (base_qty + (rand & mask) - economy × factor) mod 64
            int qty = c.BaseQuantity + rng.Next(c.QuantityRandomMask + 1) - ecoIndex * c.EconomyQuantityFactor;
            availability[i] = Math.Clamp(qty & 63, 0, 63);
        }

        return new MarketState(prices, availability);
    }

    /// <summary>
    /// Buy commodity from market. Returns cost deducted, or -1 if can't afford / no stock.
    /// </summary>
    public static int Buy(MarketState market, CommanderData commander, CommodityType commodity, int amount = 1)
    {
        int idx = (int)commodity;
        if (market.Availability[idx] < amount)
            return -1;

        int cost = market.Prices[idx] * amount;
        if (commander.Credits < cost)
            return -1;

        commander.Credits -= cost;
        commander.AddCargo(idx, amount);
        market.Availability[idx] -= amount;

        return cost;
    }

    /// <summary>
    /// Sell commodity to market. Returns revenue, or -1 if no cargo.
    /// </summary>
    public static int Sell(MarketState market, CommanderData commander, CommodityType commodity, int amount = 1)
    {
        int idx = (int)commodity;
        if (!commander.CargoHold.TryGetValue(idx, out int held) || held < amount)
            return -1;

        int revenue = market.Prices[idx] * amount;
        commander.Credits += revenue;

        commander.CargoHold[idx] = held - amount;
        if (commander.CargoHold[idx] <= 0)
            commander.CargoHold.Remove(idx);

        market.Availability[idx] = Math.Clamp(market.Availability[idx] + amount, 0, 63);

        return revenue;
    }

    /// <summary>
    /// Get commodity name by type.
    /// </summary>
    public static string GetName(CommodityType type)
    {
        return Commodities[(int)type].Name;
    }

    /// <summary>
    /// Calculate total cargo tons in commander's hold.
    /// </summary>
    public static int TotalCargo(CommanderData commander)
    {
        int total = 0;
        foreach (var kvp in commander.CargoHold)
            total += kvp.Value;
        return total;
    }

    /// <summary>
    /// Calculate total cargo value at current market prices.
    /// </summary>
    public static int CargoValue(MarketState market, CommanderData commander)
    {
        int value = 0;
        foreach (var kvp in commander.CargoHold)
        {
            if (kvp.Key < 16 && kvp.Key >= 0)
                value += market.Prices[kvp.Key] * kvp.Value;
        }
        return value;
    }
}
