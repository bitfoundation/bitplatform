using System.Collections.Concurrent;
using Bit.Belemetry.Demo.Client.Services;

namespace Bit.Belemetry.Demo.Server.Services;

/// <summary>
/// The memory behind the Live telemetry page: the last few hundred records per browser session, plus one
/// shared bucket for what belongs to no session (the server's own request spans, logs and metrics).
/// </summary>
/// <remarks>
/// <para>
/// Nothing is persisted, and everything is bounded: <see cref="PerSessionLimit"/> records per session,
/// <see cref="MaxSessions"/> sessions (the least recently active is evicted first), and a session that
/// has been idle for <see cref="SessionIdleTimeout"/> is forgotten. A reader can only ask for the
/// session id their own browser generated, so nobody sees anyone else's clicks.
/// </para>
/// <para>
/// Two kinds of record arrive without a <c>session.id</c> and are attributed after the fact:
/// </para>
/// <list type="bullet">
/// <item>Metric points never carry one (it would multiply their cardinality by the number of
/// visitors). The WebAssembly pipeline's resource has a <c>service.instance.id</c> per page load, and the
/// spans and logs of that same resource do carry the session, so the instance id is mapped to it.</item>
/// <item>Server request spans and logs carry the trace id the browser propagated in <c>traceparent</c>.
/// They stay in the shared bucket and are handed to the session that owns the trace when it asks.</item>
/// </list>
/// </remarks>
public sealed class TelemetryStore(TimeProvider timeProvider)
{
    public const int PerSessionLimit = 400;
    public const int SharedLimit = 600;
    public const int MaxSessions = 2_000;
    public static readonly TimeSpan SessionIdleTimeout = TimeSpan.FromHours(1);

    private readonly ConcurrentDictionary<string, Bucket> _sessions = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _instanceSessions = new(StringComparer.Ordinal);
    private readonly Bucket _shared = new(SharedLimit, timeProvider.GetUtcNow());
    private long _sequence;
    private long _adds;

    /// <summary>The number of sessions currently held.</summary>
    public int SessionCount => _sessions.Count;

    /// <summary>Files <paramref name="records"/> and returns how many were accepted.</summary>
    public int Add(IEnumerable<TelemetryRecord> records)
    {
        var now = timeProvider.GetUtcNow();
        var count = 0;

        foreach (var record in records)
        {
            var instance = record.Resource.GetValueOrDefault("service.instance.id");
            var sessionId = record.SessionId;

            if (sessionId is not null && instance is not null && record.Source == "wasm")
            {
                _instanceSessions[instance] = sessionId;
            }
            else if (sessionId is null && instance is not null && record.Source == "wasm")
            {
                sessionId = _instanceSessions.GetValueOrDefault(instance);
            }

            var stored = record with { Id = Interlocked.Increment(ref _sequence), SessionId = sessionId };

            var bucket = sessionId is null || IsValidSessionId(sessionId) is false
                ? _shared
                : _sessions.GetOrAdd(sessionId, _ => new Bucket(PerSessionLimit, now));

            bucket.Add(stored, now);
            count++;
        }

        if (Interlocked.Add(ref _adds, count) % 500 < count || _sessions.Count > MaxSessions) Trim(now);

        return count;
    }

    /// <summary>
    /// The records of <paramref name="sessionId"/> with an id above <paramref name="after"/>, plus the shared
    /// records that share a trace with them, plus - with <paramref name="includeShared"/> - every shared record.
    /// </summary>
    public TelemetryFeed Get(string sessionId, long after, bool includeShared)
    {
        var now = timeProvider.GetUtcNow();
        var own = _sessions.TryGetValue(sessionId, out var bucket) ? bucket.Snapshot(now) : [];
        var traces = own.Select(r => r.TraceId).Where(t => t is not null).ToHashSet(StringComparer.Ordinal);

        var shared = _shared.Snapshot(now)
            .Where(r => includeShared || (r.TraceId is not null && traces.Contains(r.TraceId)))
            .Select(r => r.TraceId is not null && traces.Contains(r.TraceId) ? r with { Correlated = true } : r);

        var records = own.Concat(shared)
                         .Where(r => r.Id > after)
                         .OrderBy(r => r.Id)
                         .ToArray();

        return new TelemetryFeed
        {
            Records = records,
            LastId = Interlocked.Read(ref _sequence),
            SessionId = sessionId,
        };
    }

    /// <summary>Forgets everything (the tests use it).</summary>
    public void Clear()
    {
        _sessions.Clear();
        _instanceSessions.Clear();
        _shared.Clear();
    }

    /// <summary>
    /// Session ids are 32 hex characters (the browser script's format). Anything else is filed in the
    /// shared bucket rather than allowed to open buckets of arbitrary names.
    /// </summary>
    public static bool IsValidSessionId(string sessionId)
        => sessionId.Length is >= 8 and <= 64 && sessionId.All(c => char.IsAsciiLetterOrDigit(c) || c == '-');

    private void Trim(DateTimeOffset now)
    {
        foreach (var (key, bucket) in _sessions)
        {
            if (now - bucket.LastTouched > SessionIdleTimeout) _sessions.TryRemove(key, out _);
        }

        var excess = _sessions.Count - MaxSessions;
        if (excess > 0)
        {
            foreach (var key in _sessions.OrderBy(s => s.Value.LastTouched).Take(excess).Select(s => s.Key).ToArray())
            {
                _sessions.TryRemove(key, out _);
            }
        }

        if (_instanceSessions.Count > MaxSessions * 4) _instanceSessions.Clear();
    }

    private sealed class Bucket(int limit, DateTimeOffset created)
    {
        private readonly Queue<TelemetryRecord> _records = new();

        public DateTimeOffset LastTouched { get; private set; } = created;

        public void Add(TelemetryRecord record, DateTimeOffset now)
        {
            lock (_records)
            {
                _records.Enqueue(record);
                while (_records.Count > limit) _records.Dequeue();
                LastTouched = now;
            }
        }

        public TelemetryRecord[] Snapshot(DateTimeOffset now)
        {
            lock (_records)
            {
                LastTouched = now;
                return [.. _records];
            }
        }

        public void Clear()
        {
            lock (_records) _records.Clear();
        }
    }
}
