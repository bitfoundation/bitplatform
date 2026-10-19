using System.Collections.Concurrent;

namespace Bit.BevTools;

/// <summary>Live facts about one circuit. The circuit id is truncated because it is a bearer-like secret.</summary>
public sealed class BevCircuitInfo
{
    public required string ShortId { get; init; }

    public required long SessionId { get; init; }

    public DateTimeOffset OpenedAt { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ClosedAt { get; internal set; }

    public bool IsConnected { get; internal set; }

    public DateTimeOffset? LastConnectionChange { get; internal set; }

    public int Disconnects { get; internal set; }

    public long InboundMessages;

    public double InboundProcessingMs;

    public double MaxInboundMs;

    public DateTimeOffset? LastActivityAt { get; internal set; }

    public int Errors { get; internal set; }

    public bool IsClosed => ClosedAt is not null;

    public TimeSpan Duration => (ClosedAt ?? DateTimeOffset.UtcNow) - OpenedAt;
}

/// <summary>Process-wide registry of circuits observed by BevTools. Singleton; only aggregate facts cross session boundaries.</summary>
public sealed class BevCircuitRegistry
{
    private readonly ConcurrentDictionary<string, BevCircuitInfo> _circuits = new(StringComparer.Ordinal);
    private long _totalOpened;
    private long _totalClosed;

    public int ActiveCount => _circuits.Values.Count(c => !c.IsClosed);

    public int ConnectedCount => _circuits.Values.Count(c => c is { IsClosed: false, IsConnected: true });

    public long TotalOpened => Volatile.Read(ref _totalOpened);

    public long TotalClosed => Volatile.Read(ref _totalClosed);

    public IReadOnlyList<BevCircuitInfo> Circuits => _circuits.Values.OrderByDescending(c => c.OpenedAt).ToList();

    internal BevCircuitInfo Open(string circuitId, long sessionId)
    {
        Interlocked.Increment(ref _totalOpened);
        var info = new BevCircuitInfo { ShortId = Shorten(circuitId), SessionId = sessionId };
        _circuits[circuitId] = info;
        Prune();
        return info;
    }

    internal BevCircuitInfo? Get(string circuitId) => _circuits.TryGetValue(circuitId, out var info) ? info : null;

    internal void Close(string circuitId)
    {
        Interlocked.Increment(ref _totalClosed);
        if (_circuits.TryGetValue(circuitId, out var info))
        {
            info.ClosedAt = DateTimeOffset.UtcNow;
            info.IsConnected = false;
        }
    }

    private void Prune()
    {
        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-10);
        foreach (var (key, info) in _circuits)
        {
            if (info.ClosedAt is { } closed && closed < cutoff)
            {
                _circuits.TryRemove(key, out _);
            }
        }
    }

    internal static string Shorten(string circuitId) => circuitId.Length <= 8 ? circuitId : circuitId[..8] + "…";
}
