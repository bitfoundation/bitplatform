using Microsoft.Extensions.Logging;

namespace Bit.BevTools;

/// <summary>
/// The newest entries captured for one owner - the process, or one circuit - until a session takes them over, with
/// counts per level of every entry captured, the ones that no longer fit included, as the session's log store keeps them.
/// Taken once: after that nothing is added, since the session records on its own from then on. A circuit's buffer also
/// draws on its process's budget (<paramref name="budget"/>), so many circuits together stay bounded too.
/// </summary>
internal sealed class BevToolsAutoStartLogBuffer(int capacity, string owner, BevToolsAutoStartLogs? budget = null)
{
    private readonly object _lock = new();
    private readonly Queue<BevToolsAutoStartLogEntry> _entries = new();
    // Per LogLevel (Trace … Critical).
    private readonly int[] _counts = new int[6];
    private readonly int _capacity = Math.Max(1, capacity);
    private bool _taken;

    /// <summary>When capturing began: the first log entry this buffer was asked to keep.</summary>
    public DateTimeOffset? Since { get; private set; }

    /// <summary>Whose entries these are, as the end of a sentence: "in this process", "on this circuit's dispatcher".</summary>
    public string Owner { get; } = owner;

    /// <summary>How many entries were captured but did not fit, the oldest first.</summary>
    public int Dropped { get; private set; }

    public bool IsTaken => Volatile.Read(ref _taken);

    public void Add(BevToolsAutoStartLogEntry entry)
    {
        lock (_lock)
        {
            if (_taken)
            {
                return;
            }

            Since ??= entry.At;
            if (_entries.Count == _capacity || (budget is not null && !budget.TryReserve()))
            {
                // Full, or the process's budget is spent: the newest entry takes the place of this buffer's oldest, or,
                // when it holds none, is counted and not kept.
                Dropped++;
                if (_entries.Count == 0)
                {
                    Count(entry);
                    return;
                }

                _entries.Dequeue();
            }

            _entries.Enqueue(entry);
            Count(entry);
        }
    }

    private void Count(BevToolsAutoStartLogEntry entry)
    {
        if (entry.Level is >= LogLevel.Trace and <= LogLevel.Critical)
        {
            _counts[(int)entry.Level]++;
        }
    }

    /// <summary>Hands the entries over, oldest first, and stops capturing; null when they were taken already.</summary>
    public BevToolsAutoStartLogEntry[]? Take(out int[] counts)
    {
        lock (_lock)
        {
            counts = (int[])_counts.Clone();
            if (_taken)
            {
                return null;
            }

            Volatile.Write(ref _taken, true);
            var entries = _entries.ToArray();
            _entries.Clear();
            budget?.Release(entries.Length);
            return entries;
        }
    }
}
