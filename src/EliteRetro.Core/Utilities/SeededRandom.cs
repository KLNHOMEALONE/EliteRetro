namespace EliteRetro.Core.Utilities;

/// <summary>
/// Deterministic seeded RNG matching original Elite's reproducibility requirements.
/// Same seed always produces identical sequence across platforms.
/// </summary>
public class SeededRandom
{
    private uint _state;

    public SeededRandom(uint seed)
    {
        _state = seed == 0 ? 1u : seed;
    }

    /// <summary>
    /// Generate next random uint using LCG (same as original Elite algorithm).
    /// </summary>
    public uint Next()
    {
        // Elite used: C = C * 5 + 1 (with EOR twist)
        _state = _state * 1664525u + 1013904223u;
        return _state;
    }

    /// <summary>
    /// Random int in [0, max).
    /// </summary>
    public int Next(int max)
    {
        if (max <= 0) return 0;
        return (int)(Next() % (uint)max);
    }

    /// <summary>
    /// Random int in [min, max).
    /// </summary>
    public int Next(int min, int max)
    {
        if (max <= min) return min;
        return min + Next(max - min);
    }

    /// <summary>
    /// Random float in [0, 1).
    /// </summary>
    public float NextFloat()
    {
        return (Next() >> 8) / 16777216f;
    }

    /// <summary>
    /// Pick random element from list.
    /// </summary>
    public T Pick<T>(IReadOnlyList<T> items)
    {
        return items[Next(items.Count)];
    }
}
