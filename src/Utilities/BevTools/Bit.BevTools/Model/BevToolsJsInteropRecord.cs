namespace Bit.BevTools;

public sealed class BevToolsJsInteropRecord
{
    public required long Id { get; init; }

    public required DateTimeOffset At { get; init; }

    public required BevToolsJsInteropDirection Direction { get; init; }

    public required string Identifier { get; init; }

    /// <summary>The call's duration; of a record that stands for several calls, the longest of them.</summary>
    public double? DurationMs { get; set; }

    /// <summary>
    /// How many calls the record stands for. The page hub reports the calls JavaScript makes to .NET one by one up to a
    /// limit per report window, and those past it as one entry per kind of call: the same identifier, second, argument
    /// count, sync or async, from an animation frame or not, and failed or not. Their individual times are not known.
    /// </summary>
    public int CallCount { get; init; } = 1;

    /// <summary>The sum of the durations of the calls a record of several stands for; null for a record of one call.</summary>
    public double? TotalDurationMs { get; set; }

    public bool Succeeded { get; set; } = true;

    public string? Error { get; set; }

    public int ArgumentCount { get; init; }

    public string? ArgumentTypes { get; init; }

    public string? ResultType { get; init; }

    public long? ComponentInstanceId { get; init; }

    public string? ComponentName { get; init; }

    public bool IsModuleCall { get; init; }

    public bool IsSync { get; init; }

    public long TimelineEventId { get; set; }

    /// <summary>High-resolution start timestamp, so the duration never depends on the timeline still holding the event.</summary>
    internal long StartTicks { get; init; }
}
