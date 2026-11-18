namespace Bit.BevTools;

/// <summary>Live facts about one circuit. The circuit id is truncated because it is a bearer-like secret.</summary>
public sealed class BevToolsCircuitInfo
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
