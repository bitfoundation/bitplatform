using System.Diagnostics;
using System.Globalization;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Server;

namespace Bit.Belemetry;

/// <summary>
/// Records a consumer span on the <see cref="SourceName"/> source for every job execution, parented to the trace
/// that enqueued the job: as a client filter it stores the W3C trace context of the enqueue in the job's parameters;
/// as a server filter it starts the span from them around <c>Perform</c>. A failed execution is an error span with
/// <c>error.type</c>. Every execution is also measured on the <see cref="MeterName"/> meter, whether or not its span
/// is recorded: the <c>hangfire.job.executions</c> counter and the <c>hangfire.job.duration</c> histogram (seconds),
/// by <c>job.type</c>, <c>job.method</c>, <c>job.outcome</c> and, unless it succeeded, <c>error.type</c>. Registered
/// by <c>UseBitBelemetry()</c> on the global configuration, which also adds the storage gauges
/// (<see cref="BelemetryHangfireOptions.StorageMetrics"/>).
/// </summary>
/// <remarks>
/// The span's kind is <see cref="ActivityKind.Consumer"/> - the job is a message taken off a queue - which is also
/// what makes Application Insights show executions in its requests table, with their failures and durations.
/// Reading the trace context back costs a storage round trip per parameter per execution (the trace parent, and
/// the trace state), like any job parameter.
/// </remarks>
public sealed class BelemetryHangfireFilter : IClientFilter, IServerFilter
{
    /// <summary>The <see cref="ActivitySource"/> name; the core collects every <c>Bit.Belemetry.*</c> source by default.</summary>
    public const string SourceName = "Bit.Belemetry.Hangfire";

    /// <summary>The <see cref="System.Diagnostics.Metrics.Meter"/> name; the core collects every <c>Bit.Belemetry.*</c> meter by default.</summary>
    public const string MeterName = "Bit.Belemetry.Hangfire";

    private const string TraceParentParameter = "traceparent";
    private const string TraceStateParameter = "tracestate";
    private const string ActivityItem = "Bit.Belemetry.Activity";
    private const string StartItem = "Bit.Belemetry.Start";

    private static readonly ActivitySource _source = new(SourceName, typeof(BelemetryHangfireFilter).Assembly.GetName().Version?.ToString());

    private readonly BelemetryHangfireOptions _options;

    /// <summary>Creates the filter.</summary>
    public BelemetryHangfireFilter(BelemetryHangfireOptions? options = null)
    {
        _options = options ?? new BelemetryHangfireOptions();
    }

    /// <inheritdoc />
    public void OnCreating(CreatingContext context)
    {
        if (_options.PropagateTraceContext is false) return;

        try
        {
            // Stored even when nothing listens here: the server that performs the job may be a different process,
            // with its own listeners, and a parameter is only written when there is a context to write.
            if (Activity.Current is { IdFormat: ActivityIdFormat.W3C } current && (_options.Filter is null || _options.Filter(context.Job)))
            {
                context.SetJobParameter(TraceParentParameter, current.Id);
                if (current.TraceStateString is { Length: > 0 } state) context.SetJobParameter(TraceStateParameter, state);
            }
        }
        catch
        {
            // Telemetry must never fail the enqueue.
        }
    }

    /// <inheritdoc />
    public void OnCreated(CreatedContext context)
    {
    }

    /// <inheritdoc />
    public void OnPerforming(PerformingContext context)
    {
        var measured = BelemetryHangfireMetrics.Enabled;
        var traced = _source.HasListeners();
        if (measured is false && traced is false) return;

        Activity? activity = null;
        try
        {
            var job = context.BackgroundJob.Job;
            if (_options.Filter is { } filter && filter(job) is false) return;

            // Apart from the span: the metrics are not sampled, so they count every execution, a dropped trace's too.
            if (measured) context.Items[StartItem] = Stopwatch.GetTimestamp();
            if (traced is false) return;

            var parent = default(ActivityContext);
            if (_options.PropagateTraceContext && context.GetJobParameter<string>(TraceParentParameter) is { Length: > 0 } traceparent)
            {
                ActivityContext.TryParse(traceparent, context.GetJobParameter<string>(TraceStateParameter), isRemote: true, out parent);
            }

            activity = _source.StartActivity($"JOB {job.Type.Name}.{job.Method.Name}", ActivityKind.Consumer, parent);
            if (activity is null) return;

            // Stored before anything that could throw, so OnPerformed always ends it: a span left current on a
            // long-lived worker thread would become the parent of every job that worker runs next.
            context.Items[ActivityItem] = activity;

            activity.SetTag("job.id", context.BackgroundJob.Id);
            activity.SetTag("job.type", job.Type.FullName);
            activity.SetTag("job.method", job.Method.Name);
            if (job.Queue is { Length: > 0 } queue) activity.SetTag("job.queue", queue);
            activity.SetTag("job.created_at", context.BackgroundJob.CreatedAt.ToString("O", CultureInfo.InvariantCulture));
        }
        catch
        {
            // Telemetry must never fail the job - nor leave a span current on the worker.
            if (activity is not null && context.Items.Remove(ActivityItem)) activity.Stop();
        }
    }

    /// <inheritdoc />
    public void OnPerformed(PerformedContext context)
    {
        // Hangfire hands the job's own exception over wrapped in a JobPerformanceException; the wrapper says nothing
        // about what went wrong.
        var exception = context.Exception is JobPerformanceException { InnerException: { } inner } ? inner : context.Exception;
        var errorType = exception is not null ? exception.GetType().FullName
                      : context.Canceled ? typeof(OperationCanceledException).FullName
                      : null;

        if (context.Items.Remove(StartItem, out var start) && start is long timestamp) Measure(context, timestamp, exception is not null, errorType);

        if (context.Items.TryGetValue(ActivityItem, out var item) is false || item is not Activity activity) return;

        try
        {
            if (errorType is not null)
            {
                activity.SetTag(BelemetrySemanticConventions.ErrorType, errorType);
                activity.SetStatus(ActivityStatusCode.Error, exception?.Message ?? "canceled");
            }
        }
        finally
        {
            context.Items.Remove(ActivityItem);
            activity.Stop();
        }
    }

    private static void Measure(PerformedContext context, long timestamp, bool failed, string? errorType)
    {
        try
        {
            var job = context.BackgroundJob.Job;
            // The job's type and method, never its id, queue or arguments: one series per kind of job and outcome.
            var tags = new TagList
            {
                { "job.type", job.Type.FullName },
                { "job.method", job.Method.Name },
                { "job.outcome", failed ? "failed" : context.Canceled ? "canceled" : "succeeded" },
            };
            if (errorType is not null) tags.Add(BelemetrySemanticConventions.ErrorType, errorType);

            BelemetryHangfireMetrics.Executions.Add(1, tags);
            BelemetryHangfireMetrics.Duration.Record(Stopwatch.GetElapsedTime(timestamp).TotalSeconds, tags);
        }
        catch
        {
            // Telemetry must never fail the job.
        }
    }
}
