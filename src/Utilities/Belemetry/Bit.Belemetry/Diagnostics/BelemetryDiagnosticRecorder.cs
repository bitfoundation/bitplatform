using System.Diagnostics;

namespace Bit.Belemetry;

/// <summary>
/// One diagnostic timeline: two bounded rings (activity, and failures apart from it so chatter cannot evict the error
/// being diagnosed), the exception groups, the recorded snapshots and the automatic captures waiting for review. One per
/// process on WebAssembly and Hybrid; one per circuit (or prerendering request) on a server.
/// </summary>
/// <remarks>Every collection is bounded and every eviction is counted, so a capture says what it is missing.</remarks>
internal sealed class BelemetryDiagnosticRecorder
{
    private const int MaxReleasesPerGroup = 10;
    private const int MaxOccurrencesPerGroup = 5;
    private const int MaxRememberedTriggers = 128;
    private const int MaxRenderTypes = 256;

    private readonly object _lock = new();
    private readonly BelemetryDiagnosticsOptions _options;
    private readonly Queue<BelemetryDiagnosticEvent> _activity = new();
    private readonly Queue<BelemetryDiagnosticEvent> _failures = new();
    private readonly Dictionary<string, GroupState> _groups = new(StringComparer.Ordinal);
    private readonly List<BelemetryDiagnosticSnapshot> _snapshots = [];
    private readonly List<BelemetryDiagnosticCapture> _pending = [];
    private readonly Dictionary<string, DateTimeOffset> _lastTriggers = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _dropped = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (int Count, double Milliseconds)> _renders = new(StringComparer.Ordinal);
    private readonly long _start = Stopwatch.GetTimestamp();
    private long _sequence;
    private long _bytes;
    private int _capturesInProgress;

    public BelemetryDiagnosticRecorder(BelemetryDiagnosticsOptions options)
    {
        _options = options;
    }

    /// <summary>When the timeline started.</summary>
    public DateTimeOffset Started { get; } = DateTimeOffset.UtcNow;

    private readonly List<BelemetryDiagnosticsService> _owners = [];
    private long _purges;

    /// <summary>
    /// The session that takes browser evidence and snapshots for its automatic captures: the first service of the scope
    /// that still lives (a nested scope's service shares the timeline but not the circuit's browser bridge).
    /// </summary>
    public BelemetryDiagnosticsService? Owner
    {
        get
        {
            lock (_lock) return _owners.FirstOrDefault(o => o.IsDisposed is false);
        }
    }

    /// <summary>Whether the circuit it belonged to ended: nothing is attributed to it any more.</summary>
    public bool IsClosed { get; private set; }

    /// <summary>Adds a service that shares this timeline.</summary>
    public void AddOwner(BelemetryDiagnosticsService owner)
    {
        lock (_lock)
        {
            if (_owners.Contains(owner) is false) _owners.Add(owner);
        }
    }

    /// <summary>Removes a service; closes the timeline when <paramref name="closeWhenUnowned"/> and none is left. Returns how many are left.</summary>
    public int RemoveOwner(BelemetryDiagnosticsService owner, bool closeWhenUnowned)
    {
        lock (_lock)
        {
            _owners.Remove(owner);
            if (_owners.Count == 0 && closeWhenUnowned) IsClosed = true;
            return _owners.Count;
        }
    }

    /// <summary>Copies the events of another timeline (a service's before it found its circuit's).</summary>
    public void Import(BelemetryDiagnosticRecorder other)
    {
        foreach (var e in other.Events()) Add(e);
    }

    /// <summary>Raised (outside the lock) after an automatic capture was added for review.</summary>
    public event Action<BelemetryDiagnosticCapture>? CaptureAdded;

    public long NextSequence() => Interlocked.Increment(ref _sequence);

    public double ElapsedMilliseconds() => Stopwatch.GetElapsedTime(_start).TotalMilliseconds;

    public int Count
    {
        get
        {
            lock (_lock) return _activity.Count + _failures.Count;
        }
    }

    public void Add(BelemetryDiagnosticEvent e)
    {
        lock (_lock)
        {
            var ring = e.IsFailure ? _failures : _activity;
            ring.Enqueue(e);
            _bytes += e.EstimatedBytes;

            while (_activity.Count > Math.Max(1, _options.MaxEvents)) Evict(_activity, "activity_limit");
            while (_failures.Count > Math.Max(1, _options.MaxFailureEvents)) Evict(_failures, "failure_limit");

            // The memory budget evicts chatter first: the failures are what a capture is about.
            while (_bytes > Math.Max(16 * 1024, _options.MaxMemoryBytes) && _activity.Count + _failures.Count > 1)
            {
                Evict(_activity.Count > 0 ? _activity : _failures, "memory_limit");
            }
        }
    }

    private void Evict(Queue<BelemetryDiagnosticEvent> ring, string reason)
    {
        var evicted = ring.Dequeue();
        _bytes -= evicted.EstimatedBytes;
        CountDroppedLocked(reason, 1);
    }

    /// <summary>Counts events that were never recorded (a category switched off, a malformed browser event, ...).</summary>
    public void CountDropped(string reason, int count = 1)
    {
        if (count <= 0) return;
        lock (_lock) CountDroppedLocked(reason, count);
    }

    private void CountDroppedLocked(string reason, int count) => _dropped[reason] = _dropped.TryGetValue(reason, out var n) ? n + count : count;

    /// <summary>Every event, ordered by time and, for equal times, by arrival.</summary>
    public List<BelemetryDiagnosticEvent> Events()
    {
        List<BelemetryDiagnosticEvent> events;
        lock (_lock)
        {
            events = new List<BelemetryDiagnosticEvent>(_activity.Count + _failures.Count);
            events.AddRange(_activity);
            events.AddRange(_failures);
        }
        events.Sort(static (a, b) => a.TimeUnixNano != b.TimeUnixNano ? a.TimeUnixNano.CompareTo(b.TimeUnixNano) : a.Sequence.CompareTo(b.Sequence));
        return events;
    }

    public Dictionary<string, int> Dropped()
    {
        lock (_lock) return new Dictionary<string, int>(_dropped, StringComparer.Ordinal);
    }

    /// <summary>Adds an occurrence to its exception group, creating the group (and forgetting the least recently seen one past the limit).</summary>
    public void CountException(BelemetryDiagnosticException exception, BelemetryExceptionOccurrence occurrence)
    {
        lock (_lock)
        {
            if (_groups.TryGetValue(exception.Fingerprint, out var group) is false)
            {
                if (_groups.Count >= Math.Max(1, _options.MaxExceptionGroups))
                {
                    var oldest = _groups.Values.MinBy(g => g.LastSeen)!;
                    _groups.Remove(oldest.Fingerprint);
                    CountDroppedLocked("exception_group_limit", 1);
                }

                group = new GroupState
                {
                    Fingerprint = exception.Fingerprint,
                    Version = exception.CustomFingerprint ? "custom" : BelemetryExceptionFingerprint.Version,
                    ExceptionType = exception.Type,
                    MessageTemplate = exception.MessageTemplate,
                    Anchor = exception.Anchor,
                    Classification = exception.Classification,
                    FirstSeen = occurrence.Time,
                };
                _groups.Add(group.Fingerprint, group);
            }

            group.Count++;
            group.LastSeen = occurrence.Time;
            if (occurrence.Release is { } release && group.Releases.Contains(release) is false && group.Releases.Count < MaxReleasesPerGroup) group.Releases.Add(release);
            group.Occurrences.Add(occurrence);
            if (group.Occurrences.Count > MaxOccurrencesPerGroup) group.Occurrences.RemoveAt(0);
        }
    }


    public List<BelemetryExceptionGroup> Groups()
    {
        lock (_lock)
        {
            return _groups.Values
                .OrderByDescending(g => g.LastSeen)
                .Select(g => new BelemetryExceptionGroup
                {
                    Fingerprint = g.Fingerprint,
                    FingerprintVersion = g.Version,
                    ExceptionType = g.ExceptionType,
                    MessageTemplate = g.MessageTemplate,
                    Anchor = g.Anchor,
                    Classification = g.Classification,
                    Count = g.Count,
                    FirstSeen = g.FirstSeen,
                    LastSeen = g.LastSeen,
                    Releases = [.. g.Releases],
                    Occurrences = [.. g.Occurrences],
                })
                .ToList();
        }
    }

    public void AddSnapshot(BelemetryDiagnosticSnapshot snapshot)
    {
        lock (_lock)
        {
            _snapshots.Add(snapshot);
            while (_snapshots.Count > Math.Max(1, _options.Snapshots.MaxRecorded))
            {
                _snapshots.RemoveAt(0);
                CountDroppedLocked("snapshot_limit", 1);
            }
        }
    }

    public List<BelemetryDiagnosticSnapshot> Snapshots()
    {
        lock (_lock) return [.. _snapshots];
    }

    /// <summary>
    /// Whether a failure with this fingerprint may trigger an automatic capture now: not within the cooldown of the last
    /// one, not while too many captures are in progress, and only while the review queue has room.
    /// </summary>
    public bool TryBeginAutomaticCapture(string fingerprint, DateTimeOffset now) => TryBeginAutomaticCapture(fingerprint, now, out _);

    /// <param name="fingerprint">The trigger's key.</param>
    /// <param name="now">Now.</param>
    /// <param name="generation">The revocations so far: a capture built across a revoke is discarded (<see cref="EndAutomaticCapture"/>).</param>
    public bool TryBeginAutomaticCapture(string fingerprint, DateTimeOffset now, out long generation)
    {
        lock (_lock)
        {
            generation = _purges;
            PruneLocked(now);
            if (_capturesInProgress + _pending.Count >= Math.Max(1, _options.MaxPendingCaptures)) return false;
            if (_lastTriggers.TryGetValue(fingerprint, out var last) && now - last < _options.AutoCaptureCooldown) return false;

            if (_lastTriggers.Count >= MaxRememberedTriggers)
            {
                var oldest = _lastTriggers.MinBy(p => p.Value).Key;
                _lastTriggers.Remove(oldest);
            }

            _lastTriggers[fingerprint] = now;
            _capturesInProgress++;
            return true;
        }
    }

    public void EndAutomaticCapture(BelemetryDiagnosticCapture? capture, long generation = -1)
    {
        lock (_lock)
        {
            _capturesInProgress = Math.Max(0, _capturesInProgress - 1);

            // Built while the user revoked consent: it may hold what was just taken back.
            if (capture is not null && generation >= 0 && generation != _purges)
            {
                CountDroppedLocked("consent_revoked_capture", 1);
                capture = null;
            }
            if (capture is null) return;

            _pending.Add(capture);
            while (_pending.Count > Math.Max(1, _options.MaxPendingCaptures)) _pending.RemoveAt(0);
        }

        if (capture is not null)
        {
            try
            {
                CaptureAdded?.Invoke(capture);
            }
            catch
            {
                // A handler of the app's must not break the recorder.
            }
        }
    }

    public List<BelemetryDiagnosticCapture> Pending()
    {
        lock (_lock)
        {
            PruneLocked(DateTimeOffset.UtcNow);
            return [.. _pending];
        }
    }

    public bool RemovePending(BelemetryDiagnosticCapture capture)
    {
        lock (_lock) return _pending.Remove(capture);
    }

    private void PruneLocked(DateTimeOffset now)
    {
        var retention = _options.CaptureRetention > TimeSpan.Zero ? _options.CaptureRetention : TimeSpan.FromMinutes(30);
        _pending.RemoveAll(c => now - c.CreatedAt > retention);
    }

    /// <summary>
    /// Removes what belongs to <paramref name="features"/>: their events, and with Errors the exception groups. With
    /// <paramref name="discardCaptures"/> (a revoke) the captures waiting for review go too: they were made under the
    /// old grant and hold what the user just took back.
    /// </summary>
    public void Purge(BelemetryFeatures features, bool discardCaptures)
    {
        if (features == BelemetryFeatures.None) return;

        lock (_lock)
        {
            var removed = Filter(_activity, features) + Filter(_failures, features);
            if (removed > 0) CountDroppedLocked("consent_revoked", removed);

            if ((features & BelemetryFeatures.Errors) != 0)
            {
                _groups.Clear();
                _lastTriggers.Clear();
            }

            if (discardCaptures) _purges++;
            if (discardCaptures && _pending.Count > 0)
            {
                CountDroppedLocked("consent_revoked_capture", _pending.Count);
                _pending.Clear();
            }
        }
    }

    /// <summary>Adds one render of a component type (<see cref="BelemetryDiagnosticsOptions.ComponentRenderStatistics"/>).</summary>
    public void CountRender(string componentType, double milliseconds)
    {
        lock (_lock)
        {
            if (_renders.TryGetValue(componentType, out var stats))
            {
                _renders[componentType] = (stats.Count + 1, stats.Milliseconds + milliseconds);
            }
            else if (_renders.Count < MaxRenderTypes)
            {
                _renders[componentType] = (1, milliseconds);
            }
        }
    }

    /// <summary>The render count and total render time per component type, when measured.</summary>
    public Dictionary<string, (int Count, double Milliseconds)> RenderStats()
    {
        lock (_lock) return new Dictionary<string, (int Count, double Milliseconds)>(_renders, StringComparer.Ordinal);
    }

    private int Filter(Queue<BelemetryDiagnosticEvent> ring, BelemetryFeatures revoked)
    {
        var count = ring.Count;
        var kept = ring.Where(e => (e.Feature & revoked) == 0).ToList();
        if (kept.Count == count) return 0;

        ring.Clear();
        _bytes = 0;
        foreach (var e in kept) ring.Enqueue(e);
        foreach (var e in _activity.Concat(_failures)) _bytes += e.EstimatedBytes;
        return count - kept.Count;
    }

    /// <summary>Forgets everything (the app's own <see cref="IBelemetryDiagnostics.ClearAsync"/>).</summary>
    public void Clear()
    {
        lock (_lock)
        {
            _activity.Clear();
            _failures.Clear();
            _groups.Clear();
            _snapshots.Clear();
            _pending.Clear();
            _lastTriggers.Clear();
            _dropped.Clear();
            _renders.Clear();
            _bytes = 0;
        }
    }

    private sealed class GroupState
    {
        public required string Fingerprint { get; init; }

        public required string Version { get; init; }

        public required string ExceptionType { get; init; }

        public string? MessageTemplate { get; init; }

        public string? Anchor { get; init; }

        public string? Classification { get; init; }

        public int Count { get; set; }

        public DateTimeOffset FirstSeen { get; init; }

        public DateTimeOffset LastSeen { get; set; }

        public List<string> Releases { get; } = [];

        public List<BelemetryExceptionOccurrence> Occurrences { get; } = [];
    }
}
