using System.Diagnostics;

namespace Bit.BevTools;

/// <summary>Bounded, thread-safe unified activity timeline for one session.</summary>
internal sealed class Timeline : IBevToolsTimeline
{
    private readonly BevToolsSession _session;
    private readonly RingBuffer<BevToolsEvent> _events;
    private readonly object _idLock = new();
    private long _nextId;

    public Timeline(BevToolsSession session, int capacity)
    {
        _session = session;
        _events = new RingBuffer<BevToolsEvent>(capacity);
    }

    public bool IsEnabled => _session.IsEnabled;

    public int Count => _events.Count;

    public long Version => _events.Version;

    public long Record(BevToolsEvent bevToolsEvent)
    {
        if (!_session.IsEnabled || !_session.IsCapturing)
        {
            return -1;
        }

        return RecordUnconditionally(bevToolsEvent);
    }

    /// <summary>Records even while capture is paused. Only for the pause marker itself.</summary>
    internal long RecordUnconditionally(BevToolsEvent bevToolsEvent)
    {
        if (!_session.IsEnabled)
        {
            return -1;
        }

        long id;
        lock (_idLock)
        {
            // id assignment and insertion happen together so the buffer stays ordered by id under concurrent writers.
            id = ++_nextId;
            bevToolsEvent.Id = id;
            _events.Add(bevToolsEvent);
        }

        _session.Touch();
        return id;
    }

    public void Complete(long eventId, double durationMs, string? detail = null, BevToolsSeverity? severity = null)
    {
        Update(eventId, evt =>
        {
            evt.DurationMs = durationMs;
            if (detail is not null)
            {
                evt.Detail = detail;
            }

            if (severity is not null)
            {
                evt.Severity = severity.Value;
            }
        });
    }

    internal bool Update(long eventId, Action<BevToolsEvent> update, bool cloneData = false)
    {
        if (eventId <= 0 || !_events.TryReplaceByKey(static e => e.Id, eventId, current =>
            {
                var copy = Clone(current, cloneData);
                update(copy);
                return copy;
            }))
        {
            return false;
        }

        _session.Touch();
        return true;
    }

    private static BevToolsEvent Clone(BevToolsEvent source, bool cloneData) => new()
    {
        Id = source.Id,
        Timestamp = source.Timestamp,
        StartTicks = source.StartTicks,
        Kind = source.Kind,
        Category = source.Category,
        Title = source.Title,
        Detail = source.Detail,
        DurationMs = source.DurationMs,
        Severity = source.Severity,
        ParentEventId = source.ParentEventId,
        ComponentInstanceId = source.ComponentInstanceId,
        ComponentName = source.ComponentName,
        Data = cloneData && source.Data is not null ? new Dictionary<string, object?>(source.Data, StringComparer.Ordinal) : source.Data,
    };

    public IDisposable BeginActivity(string category, string title, string? detail = null, BevToolsEventKind kind = BevToolsEventKind.Custom)
    {
        var evt = new BevToolsEvent { Kind = kind, Category = category, Title = title, Detail = detail };
        var id = Record(evt);
        return new ActivityScope(this, id, evt.StartTicks);
    }

    public BevToolsEvent? Find(long id)
    {
        if (id <= 0)
        {
            return null;
        }

        return _events.FindByKey(static e => e.Id, id);
    }

    public BevToolsEvent[] Snapshot() => _events.ToArray();

    public void Clear()
    {
        _events.Clear();
        _session.Touch();
    }

    private sealed class ActivityScope(Timeline timeline, long id, long startTicks) : IDisposable
    {
        public void Dispose() => timeline.Complete(id, Stopwatch.GetElapsedTime(startTicks).TotalMilliseconds);
    }
}
