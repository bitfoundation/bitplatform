namespace Bit.Blow;

/// <summary>How a run ended.</summary>
public enum BlowRunOutcome
{
    /// <summary>Every node that ran succeeded. Nodes a branch passed by are skipped, which is not a failure.</summary>
    Succeeded,

    /// <summary>At least one node failed, other than a failure its <see cref="BlowNodeRunOptions.ErrorOutput"/> carried on.</summary>
    Failed,

    /// <summary>The run was cancelled through its <see cref="CancellationToken"/> before it finished.</summary>
    Cancelled,

    /// <summary>The run took longer than <see cref="BlowRunnerOptions.RunTimeout"/> and was stopped.</summary>
    TimedOut,

    /// <summary>The run never started: the graph has a cycle, a node has no handler, or validation found an error. See <see cref="BlowRunResult.Problems"/>.</summary>
    Refused,
}
