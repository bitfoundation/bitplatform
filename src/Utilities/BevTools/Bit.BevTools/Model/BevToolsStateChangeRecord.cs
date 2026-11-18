namespace Bit.BevTools;

/// <summary>One recorded change of a registered state provider.</summary>
public sealed class BevToolsStateChangeRecord
{
    public required long Id { get; init; }

    public required DateTimeOffset At { get; init; }

    public required string Provider { get; init; }

    public string? Action { get; init; }

    public string? Detail { get; init; }

    /// <summary>Path → (before, after) for members whose display value changed.</summary>
    public required IReadOnlyList<BevToolsStateDiffEntry> Changes { get; init; }

    /// <summary>Flattened snapshot after the change (bounded), used by history browsing.</summary>
    public required IReadOnlyDictionary<string, string> Snapshot { get; init; }

    public long? TriggerEventId { get; init; }

    public long TimelineEventId { get; set; }

    /// <summary>The provider's token for the state after this change, when the provider can restore it (<see cref="IBevToolsStateRestorer"/>).</summary>
    public string? RestoreToken { get; init; }
}
