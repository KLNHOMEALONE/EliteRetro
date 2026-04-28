namespace EliteRetro.Core.Rendering;

/// <summary>
/// 64-entry sine lookup table matching BBC Elite's 256-step circle (quarter = 64 steps).
/// Provides fast Sin() and Cos() without floating-point trig calls.
/// Values are precomputed sin(2π * step / 64) for step 0..63.
/// </summary>
public static class SineTable
{
    private static readonly float[] _sinTable;

    static SineTable()
    {
        _sinTable = new float[64];
        for (int i = 0; i < 64; i++)
        {
            _sinTable[i] = MathF.Sin(MathF.Tau * i / 64f);
        }
    }

    /// <summary>
    /// Get sin(2π * step / 64) for any integer step.
    /// Handles wrapping automatically.
    /// </summary>
    public static float Sin(int step)
    {
        step = ((step % 64) + 64) % 64;
        return _sinTable[step];
    }

    /// <summary>
    /// Get cos(2π * step / 64) for any integer step.
    /// Cos is sin offset by 16 steps (90 degrees).
    /// </summary>
    public static float Cos(int step)
    {
        return Sin(step + 16);
    }

    /// <summary>
    /// Get sin and cos together for efficiency (single table lookup each).
    /// </summary>
    public static (float sin, float cos) SinCos(int step)
    {
        step = ((step % 64) + 64) % 64;
        return (_sinTable[step], _sinTable[(step + 16) % 64]);
    }
}
