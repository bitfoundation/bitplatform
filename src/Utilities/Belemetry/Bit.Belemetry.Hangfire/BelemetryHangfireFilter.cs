using System.Diagnostics;
using System.Globalization;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Server;

namespace Bit.Belemetry.Hangfire;

/// <summary>
/// Records a consumer span on the <see cref="SourceName"/> source for every job execution, parented to the trace
/// that enqueued the job: as a client filter it stores the W3C trace context of the enqueue in the job's parameters;
/// as a server filter it starts the span from them around <c>Perform</c>. A failed execution is an error span with
/// <c>error.type</c>. Registered by <c>UseBitBelemetry()</c> on the global configuration.
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

    private const string TraceParentParameter = "traceparent";
    private const string TraceStateParameter = "tracestate";
    private const string ActivityItem = "Bit.Belemetry.Activity";

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
        if (_source.HasListeners() is false) return;

        Activity? activity = null;
        try
        {
            var job = context.BackgroundJob.Job;
            if (_options.Filter is { } filter && filter(job) is false) return;

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
        if (context.Items.TryGetValue(ActivityItem, out var item) is false || item is not Activity activity) return;

        try
        {
            if (context.Exception is { } exception)
            {
                // Hangfire hands the job's own exception over wrapped in a JobPerformanceException; the wrapper says
                // nothing about what went wrong.
                if (exception is JobPerformanceException { InnerException: { } inner }) exception = inner;
                activity.SetTag(BelemetrySemanticConventions.ErrorType, exception.GetType().FullName);
                activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            }
            else if (context.Canceled)
            {
                activity.SetTag(BelemetrySemanticConventions.ErrorType, typeof(OperationCanceledException).FullName);
                activity.SetStatus(ActivityStatusCode.Error, "canceled");
            }
        }
        finally
        {
            context.Items.Remove(ActivityItem);
            activity.Stop();
        }
    }
}
