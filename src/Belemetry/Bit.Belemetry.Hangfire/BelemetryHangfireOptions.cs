using Hangfire.Common;

namespace Bit.Belemetry.Hangfire;

/// <summary>Which jobs get a span, and what it carries.</summary>
public sealed class BelemetryHangfireOptions
{
    /// <summary>
    /// Decides per job whether its executions get a span. Return <see langword="false"/> to skip one - e.g. a
    /// heartbeat job that runs every few seconds. Null (the default) records every job.
    /// </summary>
    public Func<Job, bool>? Filter { get; set; }

    /// <summary>
    /// Whether the trace context of the code that enqueued a job is stored with the job and becomes the parent of
    /// its execution span, so the request that scheduled work and the work itself form one trace. Defaults to
    /// <see langword="true"/>. Turn it off for jobs whose execution should start a trace of its own, e.g. recurring
    /// jobs, which otherwise all hang off the one trace that registered them.
    /// </summary>
    public bool PropagateTraceContext { get; set; } = true;
}
