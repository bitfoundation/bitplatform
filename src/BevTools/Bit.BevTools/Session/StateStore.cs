using System.Diagnostics;

namespace Bit.BevTools;

public sealed class BevStateProviderEntry
{
    internal BevStateProviderEntry(IBevStateProvider provider, string origin, int historySize)
    {
        Provider = provider;
        Origin = origin;
        History = new RingBuffer<BevStateChangeRecord>(historySize);
    }

    public IBevStateProvider Provider { get; }

    public string Name => Provider.Name;

    public string? Description => Provider.Description;

    /// <summary>Where the provider came from: "DI", "extension", "runtime".</summary>
    public string Origin { get; }

    public int ChangeCount { get; internal set; }

    public DateTimeOffset? LastChangedAt { get; internal set; }

    internal RingBuffer<BevStateChangeRecord> History { get; }

    internal object SyncRoot { get; } = new();

    internal int Generation { get; set; }

    internal long NotificationSequence { get; set; }

    internal long NextCommitSequence { get; set; } = 1;

    internal Dictionary<long, PendingStateChange> PendingChanges { get; } = [];

    internal Dictionary<string, string>? LastSnapshot { get; set; }

    public BevStateChangeRecord[] Changes => History.ToArray();

    public string? Error { get; internal set; }
}

internal sealed record PendingStateChange(BevStateChangeInfo Info, DateTimeOffset At, long? TriggerEventId, Dictionary<string, string>? Snapshot, string? Error);

/// <summary>Tracks registered state providers, records changes and computes before/after diffs.</summary>
internal sealed class StateStore : IBevStateProviderRegistry, IDisposable
{
    private readonly BevToolsSession _session;
    private readonly BitBevToolsOptions _options;
    private readonly object _lock = new();
    private readonly List<BevStateProviderEntry> _entries = [];
    private readonly Dictionary<IBevStateProvider, Action<BevStateChangeInfo>> _handlers = new(ReferenceEqualityComparer.Instance);
    private long _nextId;

    public StateStore(BevToolsSession session, BitBevToolsOptions options)
    {
        _session = session;
        _options = options;
    }

    public IReadOnlyList<IBevStateProvider> Providers
    {
        get { lock (_lock) { return _entries.Select(e => e.Provider).ToArray(); } }
    }

    public BevStateProviderEntry[] Entries
    {
        get { lock (_lock) { return _entries.ToArray(); } }
    }

    public IDisposable Register(IBevStateProvider provider) => Attach(provider, "runtime");

    public IDisposable Attach(IBevStateProvider provider, string origin)
    {
        ArgumentNullException.ThrowIfNull(provider);
        lock (_lock)
        {
            if (_handlers.ContainsKey(provider))
            {
                // Already registered (a provider reachable both from DI and from an extension factory). Hand back a
                // no-op token so disposing this registration does not tear down the original one.
                return NullDisposable.Instance;
            }
        }

        var entry = new BevStateProviderEntry(provider, origin, _options.MaxStateChangesPerProvider);
        lock (_lock)
        {
            if (_handlers.ContainsKey(provider))
            {
                return NullDisposable.Instance;
            }

            _entries.Add(entry);
            Action<BevStateChangeInfo> handler = info => OnChanged(entry, info);
            _handlers[provider] = handler;
            provider.Changed += handler;
        }

        try
        {
            var snapshot = _session.Inspector.Flatten(provider.GetSnapshot());
            lock (entry.SyncRoot)
            {
                if (entry.LastSnapshot is null)
                {
                    entry.LastSnapshot = snapshot;
                }
            }
        }
        catch (Exception ex)
        {
            lock (entry.SyncRoot)
            {
                entry.Error = ex.GetType().Name + ": " + ex.Message;
            }
        }

        _session.Touch();
        return new Unsubscriber(this, provider);
    }

    private void Detach(IBevStateProvider provider)
    {
        lock (_lock)
        {
            var entry = _entries.FirstOrDefault(e => ReferenceEquals(e.Provider, provider));
            if (entry is not null)
            {
                lock (entry.SyncRoot)
                {
                    entry.Generation++;
                    entry.PendingChanges.Clear();
                }
            }

            if (_handlers.Remove(provider, out var handler))
            {
                provider.Changed -= handler;
            }

            _entries.RemoveAll(e => ReferenceEquals(e.Provider, provider));
        }

        _session.Touch();
    }

    private void OnChanged(BevStateProviderEntry entry, BevStateChangeInfo info)
    {
        if (!_session.IsEnabled || !_session.IsCapturing)
        {
            // While paused the last snapshot is left as it was, so the first change after resuming diffs against
            // the state the developer was looking at.
            return;
        }

        var start = Stopwatch.GetTimestamp();
        int generation;
        long sequence;
        lock (entry.SyncRoot)
        {
            generation = entry.Generation;
            sequence = ++entry.NotificationSequence;
        }

        Dictionary<string, string>? snapshot = null;
        string? error = null;
        var at = DateTimeOffset.UtcNow;
        var triggerEventId = _session.CurrentTriggerEventId;
        try
        {
            snapshot = _session.Inspector.Flatten(entry.Provider.GetSnapshot());
        }
        catch (Exception ex)
        {
            error = ex.GetType().Name + ": " + ex.Message;
        }

        lock (entry.SyncRoot)
        {
            if (generation != entry.Generation)
            {
                return;
            }

            entry.PendingChanges[sequence] = new PendingStateChange(info, at, triggerEventId, snapshot, error);
            if (entry.PendingChanges.Count > _options.MaxStateChangesPerProvider)
            {
                entry.Generation++;
                entry.NotificationSequence = 0;
                entry.NextCommitSequence = 1;
                entry.PendingChanges.Clear();
                entry.Error = "Concurrent state notification backlog exceeded the configured history limit; pending changes were dropped.";
                return;
            }

            while (entry.PendingChanges.Remove(entry.NextCommitSequence, out var pending))
            {
                entry.NextCommitSequence++;
                if (pending.Error is not null || pending.Snapshot is null)
                {
                    entry.Error = pending.Error;
                    continue;
                }

                entry.Error = null;
                var changes = BevObjectInspector.Diff(entry.LastSnapshot, pending.Snapshot);
                entry.LastSnapshot = pending.Snapshot;
                entry.ChangeCount++;
                entry.LastChangedAt = pending.At;

                var record = new BevStateChangeRecord
                {
                    Id = Interlocked.Increment(ref _nextId),
                    At = entry.LastChangedAt.Value,
                    Provider = entry.Name,
                    Action = pending.Info?.Action,
                    Detail = pending.Info?.Detail,
                    Changes = changes,
                    Snapshot = pending.Snapshot,
                    TriggerEventId = pending.TriggerEventId,
                };
                entry.History.Add(record);

                var title = string.IsNullOrEmpty(pending.Info?.Action) ? entry.Name + " changed" : entry.Name + ": " + pending.Info!.Action;
                record.TimelineEventId = _session.Timeline.Record(new BevToolsEvent
                {
                    Kind = BevToolsEventKind.StateChange,
                    Category = "state:" + entry.Name,
                    Title = title,
                    Detail = changes.Count == 0 ? "no observable member changes" : string.Join(", ", changes.Take(6).Select(c => c.Path + (c.Kind == "changed" ? ": " + c.Before + " → " + c.After : " (" + c.Kind + ")"))) + (changes.Count > 6 ? $" … +{changes.Count - 6}" : ""),
                    ParentEventId = record.TriggerEventId,
                    Data = new Dictionary<string, object?> { ["provider"] = entry.Name, ["changeId"] = record.Id, ["changes"] = changes.Count },
                });
            }
        }

        _session.Overhead.AddInspection(Stopwatch.GetElapsedTime(start).Ticks);
        _session.Touch();
    }

    public void ClearHistory()
    {
        BevStateProviderEntry[] entries;
        lock (_lock)
        {
            entries = _entries.ToArray();
        }

        foreach (var entry in entries)
        {
            int generation;
            lock (entry.SyncRoot)
            {
                generation = ++entry.Generation;
                entry.NotificationSequence = 0;
                entry.NextCommitSequence = 1;
                entry.PendingChanges.Clear();
                entry.History.Clear();
                entry.ChangeCount = 0;
                entry.LastChangedAt = null;
                entry.LastSnapshot = null;
                entry.Error = null;
            }

            try
            {
                var snapshot = _session.Inspector.Flatten(entry.Provider.GetSnapshot());
                lock (entry.SyncRoot)
                {
                    if (entry.Generation == generation && entry.LastSnapshot is null)
                    {
                        entry.LastSnapshot = snapshot;
                    }
                }
            }
            catch (Exception ex)
            {
                lock (entry.SyncRoot)
                {
                    if (entry.Generation == generation)
                    {
                        entry.Error = ex.GetType().Name + ": " + ex.Message;
                    }
                }
            }
        }

        _session.Touch();
    }

    public void Dispose()
    {
        lock (_lock)
        {
            foreach (var entry in _entries)
            {
                lock (entry.SyncRoot)
                {
                    entry.Generation++;
                    entry.PendingChanges.Clear();
                }
            }

            foreach (var (provider, handler) in _handlers)
            {
                try
                {
                    provider.Changed -= handler;
                }
                catch
                {
                    // provider may already be disposed
                }
            }

            _handlers.Clear();
            _entries.Clear();
        }
    }

    private sealed class Unsubscriber(StateStore store, IBevStateProvider provider) : IDisposable
    {
        public void Dispose() => store.Detach(provider);
    }

    private sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}
