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

internal sealed record PendingStateChange(BevStateChangeInfo Info, string? Detail, DateTimeOffset At, long? TriggerEventId, Dictionary<string, string>? Snapshot, string? Error);

/// <summary>Tracks registered state providers, records changes and computes before/after diffs.</summary>
internal sealed class StateStore : IBevStateProviderRegistry, IDisposable
{
    private const int MaxArguments = 8;
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
                entry.Error = ex.GetType().Name + ": " + BevRedactor.RedactText(ex.Message);
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
        // Now, while the arguments are what the change was made with.
        var detail = DescribeDetail(info);
        try
        {
            snapshot = _session.Inspector.Flatten(entry.Provider.GetSnapshot());
        }
        catch (Exception ex)
        {
            error = ex.GetType().Name + ": " + BevRedactor.RedactText(ex.Message);
        }

        lock (entry.SyncRoot)
        {
            if (generation != entry.Generation)
            {
                return;
            }

            entry.PendingChanges[sequence] = new PendingStateChange(info, detail, at, triggerEventId, snapshot, error);
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
                    Detail = pending.Detail,
                    Changes = changes,
                    Snapshot = pending.Snapshot,
                    TriggerEventId = pending.TriggerEventId,
                    RestoreToken = entry.Provider is IBevStateRestorer && pending.Info?.RestoreToken is { Length: > 0 and <= 200 } token ? token : null,
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

        _session.Overhead.AddInspection(Stopwatch.GetTimestamp() - start);
        _session.Touch();
    }

    /// <summary>
    /// The provider's detail, then the change's arguments as the inspector shows them ("count=2, password=«redacted»"),
    /// by name only under <see cref="BevValueAccess.None"/>.
    /// </summary>
    private string? DescribeDetail(BevStateChangeInfo? info)
    {
        if (info?.Arguments is not { Count: > 0 } arguments)
        {
            return info?.Detail;
        }

        var inspector = _session.Inspector;
        var text = string.Join(", ", arguments.Take(MaxArguments).Select(a => inspector.ValueAccess == BevValueAccess.None ? a.Key : a.Key + "=" + inspector.Describe(a.Key, a.Value).Display))
            + (arguments.Count > MaxArguments ? $" … +{arguments.Count - MaxArguments}" : "");
        text = text.Length <= 300 ? text : text[..299] + "…";
        return string.IsNullOrEmpty(info.Detail) ? text : info.Detail + " · " + text;
    }

    /// <summary>
    /// Asks the provider of a recorded change to restore the state it had right after that change. Only where restoring
    /// is allowed (<see cref="BevToolsSession.AllowsStateRestore"/>), only for a provider that can (<see cref="IBevStateRestorer"/>),
    /// and only with the token it gave; returns what happened, never throws. Call on the session's dispatcher.
    /// </summary>
    public async Task<string> RestoreAsync(long changeId)
    {
        if (!_session.AllowsStateRestore)
        {
            return "Restoring state is not allowed outside Development in a process that serves many users.";
        }

        BevStateProviderEntry? entry;
        BevStateChangeRecord? change;
        lock (_lock)
        {
            entry = _entries.FirstOrDefault(e => e.History.FindByKey(static c => c.Id, changeId) is not null);
            change = entry?.History.FindByKey(static c => c.Id, changeId);
        }

        if (entry is null || change is null)
        {
            return "That change is no longer in the history.";
        }

        if (entry.Provider is not IBevStateRestorer restorer || change.RestoreToken is not { } token)
        {
            return entry.Name + " cannot restore this change: the provider gave it no restore token (IBevStateRestorer).";
        }

        try
        {
            await restorer.RestoreAsync(token);
        }
        catch (Exception ex)
        {
            _session.Errors.Record(ex, "bevtools", entry.Name + " failed to restore a change.");
            return entry.Name + " could not restore it: " + ex.GetType().Name + ": " + BevRedactor.RedactText(ex.Message);
        }

        _session.Timeline.Record(new BevToolsEvent
        {
            Kind = BevToolsEventKind.Custom,
            Category = "state-restore",
            Title = "Restored " + entry.Name + " to after " + (change.Action ?? "a change") + " (" + BevFmt.Time(change.At) + ")",
            Detail = "Restored from BevTools with the provider's token",
            ParentEventId = change.TimelineEventId > 0 ? change.TimelineEventId : null,
        });
        _session.Touch();
        return "Restored " + entry.Name + " to after " + (change.Action ?? "the change") + ".";
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
                        entry.Error = ex.GetType().Name + ": " + BevRedactor.RedactText(ex.Message);
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
