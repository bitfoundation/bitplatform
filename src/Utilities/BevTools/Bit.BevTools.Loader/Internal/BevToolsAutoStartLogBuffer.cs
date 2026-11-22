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
    private int _held;
    // The scopes whose renderers run on this buffer's synchronization context: it ends with the last of them.
    private int _owners = 1;

    /// <summary>When capturing began: the first log entry this buffer was asked to keep.</summary>
    public DateTimeOffset? Since { get; private set; }

    /// <summary>Whose entries these are, as the end of a sentence: "in this process", "on this circuit's dispatcher".</summary>
    public string Owner { get; } = owner;

    /// <summary>How many entries were captured but did not fit, the oldest first.</summary>
    public int Dropped { get; private set; }

    public bool IsTaken => Volatile.Read(ref _taken);

    /// <summary>How many entries it holds now, each one a place in the budget all circuits share.</summary>
    public int Held => Volatile.Read(ref _held);

    /// <summary>Set by <see cref="BevToolsAutoStartLogs"/> while it counts this buffer among those a place can be taken from.</summary>
    internal bool Tracked { get; set; }

    public void Add(BevToolsAutoStartLogEntry entry)
    {
        var reserved = false;
        if (budget is not null)
        {
            bool hasRoom;
            lock (_lock)
            {
                if (_taken)
                {
                    return;
                }

                hasRoom = _entries.Count < _capacity;
            }

            // Outside this buffer's lock: when the budget is spent, the place is taken from another circuit's buffer,
            // under that buffer's lock, and two circuits doing so at once must not wait on each other.
            reserved = hasRoom && budget.TryReserve(this);
        }

        lock (_lock)
        {
            if (_taken)
            {
                if (reserved)
                {
                    budget!.Release(1);
                    budget.Forget(this);
                }

                return;
            }

            Since ??= entry.At;
            Count(entry);
            if (_entries.Count < _capacity && (budget is null || reserved))
            {
                _entries.Enqueue(entry);
                Volatile.Write(ref _held, _entries.Count);
                return;
            }

            if (reserved)
            {
                // Another entry filled the buffer while the place was being reserved.
                budget!.Release(1);
            }

            // Full, or no place to be had: the newest entry takes the place of this buffer's oldest, or, when it holds
            // none, is counted and not kept.
            Dropped++;
            if (_entries.Count == 0)
            {
                return;
            }

            _entries.Dequeue();
            _entries.Enqueue(entry);
        }
    }

    /// <summary>
    /// Gives this buffer's oldest entry's place in the budget to another circuit's buffer, which keeps its newest entry
    /// in it; false when it holds none or was taken. The entry is counted, as one that did not fit.
    /// </summary>
    internal bool GiveUpOldest()
    {
        lock (_lock)
        {
            if (_taken || _entries.Count == 0)
            {
                return false;
            }

            _entries.Dequeue();
            Dropped++;
            Volatile.Write(ref _held, _entries.Count);
            return true;
        }
    }

    /// <summary>Another scope's renderer runs on this buffer's synchronization context too; false once it was taken.</summary>
    internal bool Join()
    {
        lock (_lock)
        {
            if (_taken)
            {
                return false;
            }

            _owners++;
            return true;
        }
    }

    /// <summary>
    /// One of the scopes it was kept for ended. The last one to end lets the entries go, and with them their share of
    /// the budget all circuits draw on; until then they are still the other scopes' to take.
    /// </summary>
    internal void Leave()
    {
        lock (_lock)
        {
            // Under the lock, so a scope joining right now finds it either still owned or taken, never let go after.
            if (--_owners == 0)
            {
                Take(out _);
            }
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
            Volatile.Write(ref _held, 0);
            if (budget is not null)
            {
                budget.Release(entries.Length);
                budget.Forget(this);
            }

            return entries;
        }
    }
}
