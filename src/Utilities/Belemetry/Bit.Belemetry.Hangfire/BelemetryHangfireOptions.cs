using Hangfire.Common;

namespace Bit.Belemetry;

/// <summary>Which jobs get a span and are measured, what the span carries, and whether the storage is measured.</summary>
public sealed class BelemetryHangfireOptions
{
    /// <summary>
    /// Decides per job whether its executions get a span and are counted in the execution metrics. Return
    /// <see langword="false"/> to skip one - e.g. a heartbeat job that runs every few seconds. Null (the default)
    /// records every job.
    /// </summary>
    public Func<Job, bool>? Filter { get; set; }

    /// <summary>
    /// Whether the trace context of the code that enqueued a job is stored with the job and becomes the parent of
    /// its execution span, so the request that scheduled work and the work itself form one trace. Defaults to
    /// <see langword="true"/>. Turn it off for jobs whose execution should start a trace of its own, e.g. recurring
    /// jobs, which otherwise all hang off the one trace that registered them.
    /// </summary>
    public bool PropagateTraceContext { get; set; } = true;

    /// <summary>
    /// Whether the state of <c>JobStorage.Current</c> is reported as gauges on the <c>Bit.Belemetry.Hangfire</c>
    /// meter: <c>hangfire.jobs.enqueued</c>, <c>.processing</c>, <c>.scheduled</c>, <c>.failed</c> and
    /// <c>.retries</c>. Defaults to <see langword="true"/>. They cost one <c>GetStatistics()</c> query of the storage per
    /// collection (cached for a second, so the five gauges share it), and are storage-wide: every instance reports the
    /// same values, so a backend takes their maximum rather than their sum. Created once per process, by the first
    /// <c>UseBitBelemetry()</c> that asks for them; turn it off on all but one kind of instance to save the queries.
    /// </summary>
    public bool StorageMetrics { get; set; } = true;
}
