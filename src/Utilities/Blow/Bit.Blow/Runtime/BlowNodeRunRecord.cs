namespace Bit.Blow;

/// <summary>How one node fared in a run.</summary>
public sealed class BlowNodeRunRecord
{
    internal BlowNodeRunRecord(string nodeId) => NodeId = nodeId;

    /// <summary>The node.</summary>
    public string NodeId { get; }

    /// <summary>Where it ended: succeeded, failed, skipped or cancelled; queued when the run stopped first.</summary>
    public BlowRunStatus Status { get; internal set; } = BlowRunStatus.Queued;

    /// <summary>The last message: a progress note, why it failed (when safe to show), why it was skipped.</summary>
    public string? Message { get; internal set; }

    /// <summary>The attempts made, retries included; 0 when it never started.</summary>
    public int Attempts { get; internal set; }

    /// <summary>When its first attempt started.</summary>
    public DateTimeOffset? StartedAt { get; internal set; }

    /// <summary>When it settled.</summary>
    public DateTimeOffset? FinishedAt { get; internal set; }

    /// <summary>From its first attempt to settling, retries and waits included.</summary>
    public TimeSpan? Duration => StartedAt is { } s && FinishedAt is { } f ? f - s : null;

    /// <summary>The exception it failed with. For the application to log as it sees fit; never shown by the library.</summary>
    public Exception? Error { get; internal set; }

    /// <summary>What its handler produced, when it succeeded.</summary>
    public BlowNodeResult? Result { get; internal set; }

    /// <summary>
    /// True when it failed and its <see cref="BlowNodeRunOptions.ErrorOutput"/> carried the failure on to the nodes
    /// after it. Such a failure was dealt with, so it does not make the run <see cref="BlowRunOutcome.Failed"/>.
    /// </summary>
    public bool Handled { get; internal set; }

    /// <summary>True when its result came from the checkpoint the run resumed from.</summary>
    public bool Restored { get; internal set; }

    /// <summary>The values that reached it, when it ran.</summary>
    public IReadOnlyList<BlowRunInput> Inputs { get; internal set; } = [];
}
