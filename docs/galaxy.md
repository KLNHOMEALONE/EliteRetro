# Galaxy and System Generation Implementation Plan

> **Source:** BBC Elite procedural generation algorithms (BBC Micro cassette/disc version)
>
> **Implementation:** C# (.NET 9.0), MonoGame (EliteRetro.Core)

---

## Overview

Elite generates 8 galaxies of 256 systems (2048 total) from three 16-bit seed values using a Tribonacci ("twist") sequence. All system data — name, economy, government, tech level, population, productivity, market prices — is derived deterministically from these seeds.

**Starting seeds (Galaxy 0, System 0 — Tibedied):**
- `s0 = 0x5A4A`, `s1 = 0x0248`, `s2 = 0xB753`

---

## Seed Structure

Three 16-bit seeds stored little-endian (low byte first):

| Seed | Low byte | High byte |
|------|----------|-----------|
| s0   | QQ15     | QQ15+1    |
| s1   | QQ15+2   | QQ15+3    |
| s2   | QQ15+4   | QQ15+5    |

Individual bits map to system properties:

```
   s0_hi    s0_lo    s1_hi    s1_lo    s2_hi    s2_lo
 76543210 76543210 76543210 76543210 76543210 76543210

                                             ^------- Species is human
                                       ^^^----------- Species adjective 1
                                    ^^^-------------- Species adjective 2
      ^^----------------^^--------------------------- Species adjective 3
                                          ^^--------- Species type
                  ^^^^^^^^--------------^^^^--------- Average radius
                             ^^^--------------------- Government type
     ^^^--------------------------------------------- Prosperity level
     ^----------------------------------------------- Economy type
                        ^^--------------------------- Tech level
                  ^^^^^^^^--------------------------- Galactic x-coord
^^^^^^^^--------------------------------------------- Galactic y-coord
                                               ^-^^^^ Long-range dot size
                                                    ^ Short-range size
          ^------------------------------------------ Name length
                                       ^^^^^--------- Name token (x4)
     ^^^--------------------------------------------- Planet distance
                       ^^^--------------------------- Sun distance
                                          ^^--------- Sun x-y offset
```

---

## Task 1: GalaxySeed Struct and Twisting Logic

**Files:**
- Create: `src/EliteRetro.Core/Systems/GalaxySeed.cs`

```csharp
/// <summary>
/// Three 16-bit seeds that describe a system.
/// Twisting moves along the Tribonacci sequence to get the next system.
/// </summary>
public struct GalaxySeed
{
    public ushort W0; // s0
    public ushort W1; // s1
    public ushort W2; // s2

    public GalaxySeed(ushort w0, ushort w1, ushort w2)
    {
        W0 = w0; W1 = w1; W2 = w2;
    }

    public byte S0Lo => (byte)(W0 & 0xFF);
    public byte S0Hi => (byte)(W0 >> 8);
    public byte S1Lo => (byte)(W1 & 0xFF);
    public byte S1Hi => (byte)(W1 >> 8);
    public byte S2Lo => (byte)(W2 & 0xFF);
    public byte S2Hi => (byte)(W2 >> 8);

    /// <summary>
    /// Twist: advance one step in the Tribonacci sequence.
    /// s0' = s1, s1' = s2, s2' = s0 + s1 + s2 (16-bit wraparound)
    /// </summary>
    public void Twist()
    {
        ushort tmpLo = (byte)(W0 + W1);       // low byte of s0+s1
        ushort tmpHi = (byte)((W0 >> 8) + (W1 >> 8)); // high byte of s0+s1
        W0 = W1;
        W1 = W2;
        W2 = (ushort)(tmpLo + (tmpHi << 8) + W1); // tmp + new s1
    }

    /// <summary>
    /// Advance to the next galaxy: rotate each byte left by 1.
    /// After 8 galactic jumps, seeds return to the original.
    /// </summary>
    public void NextGalaxy()
    {
        W0 = RotateLeftBytePair(W0);
        W1 = RotateLeftBytePair(W1);
        W2 = RotateLeftBytePair(W2);
    }

    private static ushort RotateLeftBytePair(ushort val)
    {
        byte lo = (byte)(val & 0xFF);
        byte hi = (byte)(val >> 8);
        lo = (byte)((lo << 1) | (lo >> 7));
        hi = (byte)((hi << 1) | (hi >> 7));
        return (ushort)((hi << 8) | lo);
    }

    /// <summary>
    /// Create a deep copy of this seed.
    /// </summary>
    public GalaxySeed Copy() => new GalaxySeed(W0, W1, W2);
}
```

**Initial galaxy 0 seed:**
```csharp
public static readonly GalaxySeed Galaxy0System0 = new GalaxySeed(0x5A4A, 0x0248, 0xB753);
```

---

## Task 2: System Data Derivation (TT24/TT25)

**Files:**
- Modify: `src/EliteRetro.Core/Systems/GalaxyGenerator.cs`
- Modify: `src/EliteRetro.Core/Systems/GalaxyTypes.cs`

### Economy (QQ28)

Bits 0-2 of `s0_hi`:

| Bits | Prosperity | Economy Type (bit 2) |
|------|------------|----------------------|
| 0, 5 | Rich       | 0 = Industrial       |
| 1, 6 | Average    | 1 = Agricultural     |
| 2, 7 | Poor       |                      |
| 3, 4 | Mainly     |                      |

Combined into QQ28 (0-7):
- 0 = Rich Industrial, 1 = Average Industrial, 2 = Poor Industrial, 3 = Mainly Industrial
- 4 = Mainly Agricultural, 5 = Rich Agricultural, 6 = Average Agricultural, 7 = Poor Agricultural

**Constraint:** If government is Anarchy (0) or Feudal (1), force bit 1 of economy value → economy can't be Rich.

```csharp
int economyRaw = s0hi & 0b111;
if (government <= 1) economyRaw |= 0b010; // force not-rich for anarchy/feudal

// Map to QQ28: bits 0-2 → prosperity + type
// Bit 2 = economy type (0=Industrial, 1=Agricultural)
// Bits 0-1 within each type = prosperity
int prosperity = economyRaw & 0b011;
int economyType = (economyRaw & 0b100) >> 2; // 0=Ind, 1=Ag
// QQ28 mapping:
int qq28 = economyType == 0
    ? new[] { 0, 1, 2, 3 }[prosperity]       // Ind: Rich/Avg/Poor/Mainly
    : new[] { 4, 5, 6, 7 }[prosperity];      // Ag: Mainly/Rich/Avg/Poor
```

### Government (QQ4)

Bits 3-5 of `s1_lo`:

| Value | Government |
|-------|------------|
| 0     | Anarchy    |
| 1     | Feudal     |
| 2     | Multi-government |
| 3     | Dictatorship |
| 4     | Communist  |
| 5     | Confederacy |
| 6     | Democracy  |
| 7     | Corporate State |

```csharp
int government = (s1lo >> 3) & 0b111;
```

### Tech Level (QQ5)

```
techLevel = flipped_economy + (s1_hi AND %11) + (government / 2)
```

Where `flipped_economy` = economy bits inverted (3-bit), division rounds up (LSR + carry).

Range: 0-14 (displayed as 1-15).

```csharp
int flippedEconomy = (~economyRaw) & 0b111;
int techLevel = flippedEconomy + (s1hi & 0b11) + ((government + 1) / 2); // round up
techLevel = Math.Clamp(techLevel, 0, 14);
```

### Population (QQ6)

```
population = (techLevel * 4) + economy + government + 1
```

Stored as population × 10 (billions), displayed to 1 decimal place.
Range: 1-71 (0.1B to 7.1B).

```csharp
int population = (techLevel * 4) + economyRaw + government + 1;
population = Math.Clamp(population, 1, 71);
```

### Gross Productivity (QQ7)

```
productivity = (flipped_economy + 3) * (government + 4) * population * 8
```

Measured in millions of credits.
Range: 96 to 62,480 M CR.

```csharp
long productivity = (long)(flippedEconomy + 3) * (government + 4) * population * 8;
```

### Average Radius

```
radius = ((s2_hi AND %1111) + 11) * 256 + s1_hi
```

Range: 2,816 to 6,911 km.

```csharp
int radius = ((s2hi & 0x0F) + 11) * 256 + s1hi;
```

### Galactic Coordinates

```
x = s1_hi  (0-255)
y = s0_hi >> 1  (0-127, since chart is half as tall as wide)
```

```csharp
int galacticX = s1hi;
int galacticY = s0hi >> 1;
```

### Species Type

```csharp
if ((s2lo & 0x80) == 0)
{
    species = "Human Colonials";
}
else
{
    // Alien species: up to 3 adjectives + type
    int a1 = (s2hi >> 2) & 0b111; // bits 2-4
    int a2 = (s2hi >> 5) & 0b111; // bits 5-7
    int a3 = (s0hi ^ s1hi) & 0b111; // bits 0-2 of XOR
    int a4 = ((s2hi & 0b11) + a3) & 0b111; // bits 0-1 of s2hi added to a3

    string adj1 = a1 switch { 0 => "Large ", 1 => "Fierce ", 2 => "Small ", _ => "" };
    string adj2 = a2 switch { 0 => "Green ", 1 => "Red ", 2 => "Yellow ", 3 => "Blue ", 4 => "Black ", 5 => "Harmless ", _ => "" };
    string adj3 = a3 switch { 0 => "Slimy ", 1 => "Bug-Eyed ", 2 => "Horned ", 3 => "Bony ", 4 => "Fat ", 5 => "Furry ", _ => "" };
    string type = a4 switch { 0 => "Rodents", 1 => "Frogs", 2 => "Lizards", 3 => "Lobsters", 4 => "Birds", 5 => "Humanoids", 6 => "Felines", 7 => "Insects" };

    species = adj1 + adj2 + adj3 + type;
}
```

### Planet Spawn Position (SOLAR)

```
z_sign = ((s0_hi AND %111) + 6 + FIST_bit0) >> 1
x_sign = ROR(z_sign)  // rotate right through carry
y_sign = x_sign
```

### Sun Spawn Position (SOLAR)

```
z_sign = (s1_hi AND %111) OR %10000001  // always negative (behind player)
x_sign = s2_hi AND %11
y_sign = x_sign
```

---

## Task 3: System Name Generation (cpl routine)

**Files:**
- Modify: `src/EliteRetro.Core/Systems/GalaxyGenerator.cs`

### Two-Letter Token Table (QQ16)

31 entries, indexed 1-31 (0 = skip):

```
Index 129: "LE"   Index 137: "XE"   Index 145: "DU"   Index 153: "RI"
Index 130: "XE"   Index 138: "ZA"   Index 146: "CE"   Index 154: "US"
Index 131: "GE"   Index 139: "SO"   Index 147: "BI"   Index 155: "ER"
Index 132: "IN"   Index 140: "CR"   Index 148: "RA"   Index 156: "BE"
Index 133: "EN"   Index 141: "ON"   Index 149: "LA"   Index 157: "SA"
Index 134: "VE"   Index 142: "RE"   Index 150: "VE"   Index 158: "VI"
Index 135: "ER"   Index 143: "A"    Index 151: "TI"   Index 159: "NE"
Index 136: "US"   Index 144: "ND"   Index 152: "ED"
```

Note: Some tokens are 1 character ("A" at index 143), making names of odd length possible.

### Algorithm

```csharp
private static readonly string[] TOKENS = new string[32]
{
    "",     // 0 = skip
    "LE", "XE", "GE", "IN", "EN", "VE", "ER", "US",  // 1-8  (idx 129-136)
    "XE", "ZA", "SO", "CR", "ON", "RE", "A",  "ND",  // 9-16 (idx 137-144)
    "DU", "CE", "BI", "RA", "LA", "VE", "TI", "ED",  // 17-24(idx 145-152)
    "RI", "US", "ER", "BE", "SA", "VI", "NE", ""     // 25-31(idx 153-159) + padding
};

public static string GenerateName(GalaxySeed seed)
{
    var nameSeed = seed.Copy();
    int pairs = (seed.S0Lo & 0x40) != 0 ? 4 : 3; // bit 6 of s0_lo
    string name = "";
    bool carrySet = false;

    for (int i = 0; i < pairs; i++)
    {
        nameSeed.Twist();
        int tokenIndex = nameSeed.S2Hi & 0x1F; // bits 0-4 of s2_hi
        if (tokenIndex != 0)
        {
            name += TOKENS[tokenIndex];
        }
        carrySet = /* C flag from twist */;
    }

    // Capitalize: first letter upper, rest lower
    if (name.Length > 0)
        name = char.ToUpper(name[0]) + name.Substring(1).ToLower();

    return name;
}
```

**Note:** The C flag resulting from the final twist feeds into the Short-range Chart star size calculation.

### Examples

| System | Seeds (s0,s1,s2) | Name Generation |
|--------|-------------------|-----------------|
| Ra | Twist 0: skip, Twist 1: "RA", Twist 2: skip | "Ra" |
| Lave | Twist 0: "LA", Twist 1: "VE", Twist 2: skip | "Lave" |
| Tibedied | Twist 0: "TI", Twist 1: "BI", Twist 2: "DI", Twist 3: "ED" | "Tibedied" |

---

## Task 4: Galaxy Generation Loop

**Files:**
- Modify: `src/EliteRetro.Core/Systems/GalaxyGenerator.cs`

```csharp
public Galaxy GenerateGalaxy(int galaxyIndex)
{
    var seed = GalaxySeed.Galaxy0System0;

    // Advance to target galaxy
    for (int g = 0; g < galaxyIndex; g++)
    {
        seed.NextGalaxy();
    }

    var systems = new StarSystem[256];
    for (int i = 0; i < 256; i++)
    {
        systems[i] = GenerateSystem(galaxyIndex, i, seed);

        // Twist 4 times to get next system's seeds
        seed.Twist();
        seed.Twist();
        seed.Twist();
        seed.Twist();
    }

    return new Galaxy(galaxyIndex, systems);
}
```

**Why 4 twists?** Each twist advances one step in the Tribonacci sequence. The game twists 4 times between systems to spread the seed values more across the sequence, ensuring variety.

---

## Task 5: Market Commodity Generation

**Files:**
- Create: `src/EliteRetro.Core/Systems/MarketGenerator.cs`
- Create: `src/EliteRetro.Core/Systems/MarketTypes.cs`

### QQ23 Market Prices Table

Each commodity has 4 bytes:

| Byte | Field | Description |
|------|-------|-------------|
| 0 | `base_price` | Base price (e.g. 19=Food, 20=Textiles, 235=Narcotics) |
| 1 | `econ_factor` + units | Bits 0-4: economic factor value; Bit 7: sign; Bits 5-6: units |
| 2 | `base_quantity` | Base availability |
| 3 | `mask` | Price fluctuation mask |

### Known Commodities

| # | Name | Base Price | Econ Factor | Base Qty | Mask |
|---|------|-----------|-------------|----------|------|
| 0 | Food | 19 | -2 | 6 | 0x01 |
| 1 | Textiles | 20 | -1 | 10 | 0x03 |
| 2 | Narcotics | 235 | +8 | 8 | 0x78 |

*(Full table of 16+ items needs to be completed from the original game data)*

### Price Formula

```
price = (base_price + (random AND mask) + economy * economic_factor) * 4
```

Result is in decicredits (×10 displayed price for 1 decimal place).

- Negative economic factors → cheaper in Agricultural economies
- Positive economic factors → more expensive in Poor Agricultural systems
- Mask controls volatility: more bits = wider price swings

```csharp
public int CalculatePrice(CommodityTemplate commodity, int economyQQ28, byte randomValue)
{
    int econFactor = commodity.EconomicFactor; // signed
    int randomContribution = randomValue & commodity.Mask;
    int price = (commodity.BasePrice + randomContribution + economyQQ28 * econFactor) * 4;
    return price; // decicredits
}
```

### Availability Formula

```
quantity = max(0, (base_quantity + (random AND mask) - economy * economic_factor) mod 64)
```

```csharp
public int CalculateAvailability(CommodityTemplate commodity, int economyQQ28, byte randomValue)
{
    int econFactor = commodity.EconomicFactor; // signed
    int randomContribution = randomValue & commodity.Mask;
    int qty = (commodity.BaseQuantity + randomContribution - economyQQ28 * econFactor) % 64;
    return Math.Max(0, qty);
}
```

---

## Task 6: StarSystem and Galaxy Types

**Files:**
- Modify: `src/EliteRetro.Core/Systems/GalaxyTypes.cs`

```csharp
public enum GovernmentType { Anarchy, Feudal, MultiGov, Dictatorship, Communist, Confederacy, Democracy, CorpState }

public enum EconomyType
{
    RichIndustrial, AverageIndustrial, PoorIndustrial, MainlyIndustrial,
    MainlyAgricultural, RichAgricultural, AverageAgricultural, PoorAgricultural
}

public record struct StarSystem(
    string Name,
    int GalaxyIndex,
    int SystemIndex,
    Vector2 GalacticCoords,  // (x: 0-255, y: 0-127)
    GovernmentType Government,
    EconomyType Economy,
    int TechLevel,           // 1-15 (displayed)
    int Population,          // ×10 billions (1-71 → 0.1B to 7.1B)
    long Productivity,       // M CR
    int AverageRadiusKm,
    GalaxySeed Seed,
    string? Species = null
);

public record Galaxy(int Index, StarSystem[] Systems);
```

---

## Task 7: Verification Tests

**Files:**
- Create: `tests/EliteRetro.Tests/GalaxyTests.cs`

### Known-Answer Tests

| Galaxy | System | Name | X | Y | Government | Economy |
|--------|--------|------|---|---|------------|---------|
| 0 | 0 | Tibedied | 2 | 45 | ? | ? |
| 0 | 1 | Lave | 20 | 86 | Dictatorship | Agricultural (Rich) |
| 0 | 2 | ? | ? | ? | ? | ? |

**Lave verification (from source):**
- Seeds: `s0=0xAD38, s1=0x149C, s2=0x151D`
- Government: Dictatorship (3)
- Economy: Rich Agricultural (5)
- Tech Level: 5 (displayed)
- Population: 2.5 billion (25 × 10)
- Productivity: 7000 M CR
- Radius: 4116 km
- Species: Human Colonials

```csharp
[Fact]
public void Galaxy0_System0_Is_Tibedied()
{
    var gen = new GalaxyGenerator();
    var galaxy = gen.GenerateGalaxy(0);
    Assert.Equal("Tibedied", galaxy.Systems[0].Name);
}

[Fact]
public void Galaxy0_System1_Is_Lave()
{
    var gen = new GalaxyGenerator();
    var galaxy = gen.GenerateGalaxy(0);
    var lave = galaxy.Systems[1];
    Assert.Equal("Lave", lave.Name);
    Assert.Equal(GovernmentType.Dictatorship, lave.Government);
    Assert.Equal(EconomyType.RichAgricultural, lave.Economy);
    Assert.Equal(5, lave.TechLevel); // displayed value
    Assert.Equal(25, lave.Population); // 2.5 billion
    Assert.Equal(7000, lave.Productivity);
    Assert.Equal(4116, lave.AverageRadiusKm);
}
```

---

## Implementation Order

1. **GalaxySeed struct** — core data structure + twist/next-galaxy
2. **System data derivation** — TT24/TT25 logic (economy, gov, tech, pop, etc.)
3. **Name generation** — cpl routine with token table
4. **Galaxy generation loop** — 8 × 256 systems
5. **Market generator** — QQ23 table + price/availability formulas
6. **StarSystem/Galaxy types** — data model
7. **Verification tests** — known-answer tests against Lave, Tibedied, etc.

---

## Open Questions

1. **Full QQ23 commodity table** — Only 3 of ~16 items documented in source. Need complete table from original game disassembly.
2. **FIST (legal status)** — Affects planet spawn position. Need to define how this state is managed.
3. **Short-range/Long-range chart rendering** — Dot sizes and star sizes derived from seeds but rendering logic is separate.
4. **Galaxy 1-7 verification data** — Only Galaxy 0 has known-answer test cases documented.
