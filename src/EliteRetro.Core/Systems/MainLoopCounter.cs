namespace EliteRetro.Core.Systems;

/// <summary>
/// Main loop counter (MCNT) — authentic Elite-style frame-spread task scheduling.
///
/// An 8-bit counter cycles 0-255, decrementing each update. Tasks are scheduled
/// using modulo arithmetic: a task with mask M and offset O fires when (mcnt & M) == O.
/// This distributes work evenly across frames, preventing spikes.
///
/// In MonoGame, the counter decrements per update call (assumes ~60 updates/sec).
/// For variable timestep, use elapsed time to accumulate counter steps.
/// </summary>
public class MainLoopCounter
{
    private byte _mcnt;
    private float _tickAccumulator;
    private readonly float _tickInterval;

    /// <summary>Current counter value (0-255).</summary>
    public byte Value => _mcnt;

    /// <summary>
    /// Create a new main loop counter.
    /// </summary>
    /// <param name="initialValue">Starting value (default 0).</param>
    /// <param name="tickInterval">Seconds per tick for time-based decrement (default 1/60 ≈ 0.0167s).</param>
    public MainLoopCounter(byte initialValue = 0, float tickInterval = 1f / 60f)
    {
        _mcnt = initialValue;
        _tickInterval = tickInterval;
    }

    /// <summary>
    /// Decrement the counter by one, wrapping 0 → 255.
    /// Called once per game update.
    /// </summary>
    public void Decrement()
    {
        _mcnt--; // byte underflow wraps 0 → 255 automatically in C# unchecked context
    }

    /// <summary>
    /// Time-based decrement — accumulates elapsed time and decrements
    /// once per tick interval. Use for variable timestep.
    /// </summary>
    /// <param name="elapsedSeconds">Elapsed time since last update.</param>
    /// <returns>Number of ticks elapsed (0 or 1 for typical frame times).</returns>
    public int DecrementTimeBased(float elapsedSeconds)
    {
        _tickAccumulator += elapsedSeconds;
        int ticks = 0;

        while (_tickAccumulator >= _tickInterval)
        {
            Decrement();
            _tickAccumulator -= _tickInterval;
            ticks++;
        }

        return ticks;
    }

    /// <summary>
    /// Reset the counter to a specific value.
    /// Used after fueling, launching, hyperspace arrive (set to 0)
    /// or in-system jump (set to 1).
    /// </summary>
    public void Reset(byte value = 0)
    {
        _mcnt = value;
    }

    /// <summary>
    /// Check if the counter matches a scheduled task slot.
    /// Task fires when (mcnt & mask) == offset.
    /// </summary>
    /// <param name="mask">Bitmask for divisibility check (e.g., 0b111 for every 8).</param>
    /// <param name="offset">Offset within the cycle (0 to mask).</param>
    /// <returns>True if the task should fire this frame.</returns>
    public bool Matches(byte mask, byte offset)
    {
        return (_mcnt & mask) == offset;
    }
}
