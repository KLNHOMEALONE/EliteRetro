using Microsoft.Xna.Framework;

namespace EliteRetro.Core.Systems;

/// <summary>
/// Task scheduler driven by MainLoopCounter (MCNT).
/// Registers actions with (mask, offset) pairs — action fires when (mcnt & mask) == offset.
///
/// This spreads expensive operations across multiple frames instead of doing everything
/// each update. For example, instead of processing all 12 bubble slots every frame,
/// process slots 0-3 when mcnt & 7 == 0, slots 4-7 when mcnt & 7 == 1, etc.
///
/// Thread safety: not thread-safe. Designed for single-threaded MonoGame Update loop.
/// </summary>
public class TaskScheduler
{
    private readonly MainLoopCounter _mcnt;
    private readonly List<ScheduledTask> _tasks = new();

    /// <summary>Number of registered tasks.</summary>
    public int TaskCount => _tasks.Count;

    /// <summary>
    /// Create a task scheduler bound to a specific main loop counter.
    /// </summary>
    public TaskScheduler(MainLoopCounter mcnt)
    {
        _mcnt = mcnt;
    }

    /// <summary>
    /// Register a task that fires when (mcnt & mask) == offset.
    /// </summary>
    /// <param name="mask">Bitmask (e.g., 0b111 for every-8 scheduling, 0b1111 for every-16).</param>
    /// <param name="offset">Offset within cycle. For spreading N sub-tasks, use 0..N-1.</param>
    /// <param name="action">Action to execute when triggered.</param>
    public void RegisterTask(byte mask, byte offset, Action action)
    {
        _tasks.Add(new ScheduledTask(mask, offset, action));
    }

    /// <summary>
    /// Evaluate all registered tasks against current MCNT value.
    /// Call once per game update, after Decrement().
    /// </summary>
    public void Evaluate()
    {
        foreach (var task in _tasks)
        {
            if (_mcnt.Matches(task.Mask, task.Offset))
            {
                task.Action();
            }
        }
    }

    /// <summary>
    /// Evaluate only tasks matching a specific mask.
    /// Useful for grouping related tasks.
    /// </summary>
    public void EvaluateMask(byte mask)
    {
        foreach (var task in _tasks)
        {
            if (task.Mask == mask && _mcnt.Matches(task.Mask, task.Offset))
            {
                task.Action();
            }
        }
    }

    /// <summary>
    /// Clear all registered tasks.
    /// </summary>
    public void Clear()
    {
        _tasks.Clear();
    }

    private readonly struct ScheduledTask(byte mask, byte offset, Action action)
    {
        public readonly byte Mask = mask;
        public readonly byte Offset = offset;
        public readonly Action Action = action;
    }
}

/// <summary>
/// Extension methods for registering common Elite-style scheduled tasks.
/// </summary>
public static class TaskSchedulerExtensions
{
    /// <summary>
    /// Register a task that fires every N frames (power of 2: 4, 8, 16, 32, 64, 256).
    /// </summary>
    public static void RegisterEvery(this TaskScheduler scheduler, int interval, Action action)
    {
        byte mask = (byte)(interval - 1); // 8 → 0b111, 16 → 0b1111, etc.
        scheduler.RegisterTask(mask, 0, action);
    }

    /// <summary>
    /// Register a task that fires every N frames at a specific offset.
    /// Use to spread multiple sub-tasks within the same interval.
    /// </summary>
    public static void RegisterEvery(this TaskScheduler scheduler, int interval, int offset, Action action)
    {
        byte mask = (byte)(interval - 1);
        scheduler.RegisterTask(mask, (byte)offset, action);
    }
}
