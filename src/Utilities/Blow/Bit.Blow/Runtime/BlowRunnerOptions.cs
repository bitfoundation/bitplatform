namespace Bit.Blow;

/// <summary>The settings of a <see cref="BlowRunner"/>.</summary>
public sealed class BlowRunnerOptions
{
    /// <summary>
    /// The most nodes working at once - in a run and the runs its nodes start together, a node waiting for its sub-run
    /// counting as none. 1 runs one node at a time in dependency order. At least 1; 4 by default.
    /// </summary>
    public int MaxConcurrency { get; set; } = 4;

    /// <summary>
    /// The longest a run may take, or null for no limit. A slower run ends as <see cref="BlowRunOutcome.TimedOut"/>.
    /// <see cref="Timeout.InfiniteTimeSpan"/>, or longer than a timer can wait (about 49 days), is no limit.
    /// </summary>
    public TimeSpan? RunTimeout { get; set; }

    /// <summary>When true, the first failure cancels the rest of the run. When false (the default), independent branches go on.</summary>
    public bool StopOnFailure { get; set; }

    /// <summary>What happens to a node no handler is registered for.</summary>
    public BlowMissingHandlerBehavior MissingHandler { get; set; } = BlowMissingHandlerBehavior.Refuse;

    /// <summary>
    /// Options for one node, over those its type was registered with - a timeout read from the node's data, say. Return
    /// null to keep the type's.
    /// </summary>
    public Func<BlowNode, BlowNodeRunOptions?>? NodeOptions { get; set; }

    /// <summary>
    /// The handler for a node whose type has none registered - for handlers chosen by something other than the type.
    /// Return null for none.
    /// </summary>
    public Func<BlowNode, BlowNodeHandler?>? ResolveHandler { get; set; }

    /// <summary>
    /// What to say about a failure that is not a <see cref="BlowNodeException"/>. Exception messages can carry
    /// connection strings, paths and payloads, so by default they are not used: the message is a plain "failed".
    /// </summary>
    public Func<Exception, string?>? DescribeError { get; set; }

    /// <summary>How deep runs may nest through <see cref="BlowNodeRunContext.RunSubgraphAsync"/>. 16 by default.</summary>
    public int MaxDepth { get; set; } = 16;

    /// <summary>The most attempts, retries included, one run may make in all, or null for no limit. A run past it fails its next node.</summary>
    public int? MaxExecutions { get; set; }

    /// <summary>The clock for timestamps, delays and timeouts. <see cref="TimeProvider.System"/> by default; replace it in tests.</summary>
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    /// <summary>What the runner says about the nodes it skips, stops and times out.</summary>
    public BlowRunMessages Messages { get; set; } = new();
}
