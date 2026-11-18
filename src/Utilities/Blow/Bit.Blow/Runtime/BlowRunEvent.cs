namespace Bit.Blow;

/// <summary>
/// One thing that happened in a run. Events carry what to show - statuses, messages, timings - and never a node's
/// input or output values, so a log of them cannot leak a payload.
/// </summary>
public sealed record BlowRunEvent(BlowRunEventKind Kind, string RunId, DateTimeOffset Timestamp)
{
    /// <summary>The node the event is about, or null for the run's own events.</summary>
    public string? NodeId { get; init; }

    /// <summary>The edge a value went along, for <see cref="BlowRunEventKind.EdgeActivated"/>.</summary>
    public string? EdgeId { get; init; }

    /// <summary>The node's status after the event, or null for the run's own events and edge events.</summary>
    public BlowRunStatus? Status { get; init; }

    /// <summary>What to say about it: a progress note, a failure that is safe to show, why a node was skipped.</summary>
    public string? Message { get; init; }

    /// <summary>The attempt the event belongs to, starting at 1; 0 when the node has not started.</summary>
    public int Attempt { get; init; }

    /// <summary>How far along the node is, from 0 to 1, when its handler said.</summary>
    public double? Progress { get; init; }

    /// <summary>How long the runner waits before the next attempt, for <see cref="BlowRunEventKind.NodeRetrying"/>.</summary>
    public TimeSpan? RetryDelay { get; init; }

    /// <summary>How the run ended, for <see cref="BlowRunEventKind.RunFinished"/>.</summary>
    public BlowRunOutcome? Outcome { get; init; }
}
