using System.Diagnostics;

namespace Bit.Belemetry;

/// <summary>
/// Turns stopped <see cref="Activity"/> instances of the configured sources into <see cref="BelemetrySpan"/>s, using
/// the BCL <see cref="ActivityListener"/> - no OpenTelemetry SDK needed.
/// </summary>
internal sealed class BelemetryActivityCollector : IDisposable
{
    private readonly BelemetrySettings _settings;
    private readonly BelemetryPipeline _pipeline;
    private readonly ActivityListener _listener;
    private readonly double _ratio;

    public BelemetryActivityCollector(BelemetrySettings settings, BelemetryPipeline pipeline)
    {
        _settings = settings;
        _pipeline = pipeline;
        _ratio = settings.Options.Traces.SamplingRatio;

        _listener = new ActivityListener
        {
            ShouldListenTo = source => BelemetrySettings.Matches(source.Name, _settings.TraceSources),
            Sample = Sample,
            SampleUsingParentId = SampleUsingParentId,
            ActivityStopped = OnStopped,
        };

        ActivitySource.AddActivityListener(_listener);
    }

    private ActivitySamplingResult Sample(ref ActivityCreationOptions<ActivityContext> options)
    {
        // The exporter's own HTTP calls: create nothing, not even a propagation-only activity, so no traceparent
        // header leaks to the telemetry backend.
        if (BelemetrySuppression.IsActive) return ActivitySamplingResult.None;

        // Without consent for traces the activity is still created so W3C context keeps flowing to downstream
        // services (their own sampling and consent apply there), but nothing is recorded here.
        if (_pipeline.CanRecord(BelemetryFeatures.Traces) is false) return ActivitySamplingResult.PropagationData;

        if (options.Parent != default)
        {
            // Parent-based: follow the caller's decision, whether the parent is local or remote.
            return (options.Parent.TraceFlags & ActivityTraceFlags.Recorded) != 0
                ? ActivitySamplingResult.AllDataAndRecorded
                : ActivitySamplingResult.PropagationData;
        }

        return BelemetrySampler.ShouldSample(options.TraceId, _ratio)
            ? ActivitySamplingResult.AllDataAndRecorded
            : ActivitySamplingResult.PropagationData;
    }

    private ActivitySamplingResult SampleUsingParentId(ref ActivityCreationOptions<string> options)
    {
        if (BelemetrySuppression.IsActive) return ActivitySamplingResult.None;
        if (_pipeline.CanRecord(BelemetryFeatures.Traces) is false) return ActivitySamplingResult.PropagationData;

        // A non-W3C (hierarchical) parent id carries no sampling flag, so fall back to the ratio.
        return _ratio >= 1 ? ActivitySamplingResult.AllDataAndRecorded : ActivitySamplingResult.PropagationData;
    }

    private void OnStopped(Activity activity)
    {
        if (activity.Recorded is false || activity.IdFormat != ActivityIdFormat.W3C) return;

        try
        {
            _pipeline.Enqueue(Convert(activity, _settings.Options.Traces));
        }
        catch
        {
            // Never let telemetry break the instrumented code path.
        }
    }

    internal static BelemetrySpan Convert(Activity activity, BelemetryTracingOptions options)
    {
        var start = BelemetryTime.ToUnixNano(activity.StartTimeUtc);
        var span = new BelemetrySpan
        {
            TraceId = activity.TraceId.ToHexString(),
            SpanId = activity.SpanId.ToHexString(),
            ParentSpanId = activity.ParentSpanId == default ? null : activity.ParentSpanId.ToHexString(),
            TraceState = activity.TraceStateString,
            Flags = (int)activity.ActivityTraceFlags,
            Name = activity.DisplayName,
            Kind = activity.Kind switch
            {
                ActivityKind.Server => BelemetrySpanKind.Server,
                ActivityKind.Client => BelemetrySpanKind.Client,
                ActivityKind.Producer => BelemetrySpanKind.Producer,
                ActivityKind.Consumer => BelemetrySpanKind.Consumer,
                _ => BelemetrySpanKind.Internal,
            },
            StartTimeUnixNano = start,
            EndTimeUnixNano = start + (activity.Duration.Ticks * 100),
            Scope = new BelemetryScope(activity.Source.Name, activity.Source.Version),
            Feature = BelemetryFeatures.Traces,
        };

        foreach (var (key, value) in activity.TagObjects)
        {
            switch (key)
            {
                // Status conveyed through tags by instrumentations predating Activity.SetStatus.
                case "otel.status_code":
                    if (value is string code && span.Status == BelemetryStatusCode.Unset)
                    {
                        span.Status = code.Equals("ERROR", StringComparison.OrdinalIgnoreCase) ? BelemetryStatusCode.Error
                                    : code.Equals("OK", StringComparison.OrdinalIgnoreCase) ? BelemetryStatusCode.Ok
                                    : BelemetryStatusCode.Unset;
                    }
                    break;
                case "otel.status_description":
                    span.StatusMessage ??= value as string;
                    break;
                default:
                    span.Attributes.Set(key, value);
                    break;
            }
        }

        if (activity.Status != ActivityStatusCode.Unset)
        {
            span.Status = activity.Status == ActivityStatusCode.Error ? BelemetryStatusCode.Error : BelemetryStatusCode.Ok;
            span.StatusMessage = activity.Status == ActivityStatusCode.Error ? activity.StatusDescription : null;
        }

        foreach (var e in activity.Events)
        {
            if (span.Events.Count >= options.MaxEventsPerSpan) break;

            var spanEvent = new BelemetrySpanEvent { Name = e.Name, TimeUnixNano = BelemetryTime.ToUnixNano(e.Timestamp) };
            foreach (var (key, value) in e.Tags) spanEvent.Attributes.Set(key, value);
            span.Events.Add(spanEvent);
        }

        foreach (var link in activity.Links)
        {
            if (span.Links.Count >= options.MaxLinksPerSpan) break;

            var spanLink = new BelemetrySpanLink
            {
                TraceId = link.Context.TraceId.ToHexString(),
                SpanId = link.Context.SpanId.ToHexString(),
                TraceState = link.Context.TraceState,
            };
            if (link.Tags is not null)
            {
                foreach (var (key, value) in link.Tags) spanLink.Attributes.Set(key, value);
            }
            span.Links.Add(spanLink);
        }

        return span;
    }

    public void Dispose() => _listener.Dispose();
}
