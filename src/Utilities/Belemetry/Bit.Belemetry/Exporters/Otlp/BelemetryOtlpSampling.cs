namespace Bit.Belemetry;

/// <summary>
/// How an OTLP request tells the backend how many items each one it carries stands for: the
/// <see cref="BelemetrySpan.SamplingRatio"/> and <see cref="BelemetryLogRecord.SamplingRatio"/> the Azure Monitor exporter
/// counts as <c>1 / ratio</c>. Written only for a ratio above 0 and below 1; at 1 the request is what it always was.
/// </summary>
/// <remarks>
/// A span of a trace <see cref="BelemetrySampler"/> kept carries it as OpenTelemetry's probability sampling writes it,
/// in its tracestate (<see cref="BelemetryTraceState.WithOtelThreshold"/>), which backends read as the span's adjusted
/// count. A log record has no tracestate, and a span the browser kept by its session (relayed: the decision is not the
/// trace id's, so a threshold would contradict its randomness), or one whose trace has a randomness of its own, gets the
/// <see cref="RatioAttribute"/> attribute instead. The records themselves are left alone: every exporter shares them.
/// </remarks>
internal static class BelemetryOtlpSampling
{
    /// <summary>The attribute carrying the ratio a log record, or a span without a threshold, was kept at (a double).</summary>
    public const string RatioAttribute = "belemetry.sampling.ratio";

    /// <summary>Whether the records of <paramref name="resource"/> are the browser's, kept by their session.</summary>
    public static bool IsSessionSampled(BelemetryResource resource) =>
        resource.Attributes.GetString(BelemetrySemanticConventions.TelemetrySdkLanguage) == "webjs";

    /// <summary>
    /// The tracestate to send for <paramref name="span"/>, and the ratio to add as <see cref="RatioAttribute"/> when the
    /// tracestate cannot carry it (null when there is none to add).
    /// </summary>
    public static string? TraceState(BelemetrySpan span, bool sessionSampled, out double? ratioAttribute)
    {
        ratioAttribute = null;
        var ratio = span.SamplingRatio;
        if ((ratio > 0 && ratio < 1) is false) return span.TraceState;

        if (sessionSampled is false && BelemetryTraceState.WithOtelThreshold(span.TraceState, span.TraceId, ratio) is { } traceState) return traceState;

        ratioAttribute = RatioOf(span.Attributes, ratio);
        return span.TraceState;
    }

    /// <summary>The ratio to add to <paramref name="log"/> as <see cref="RatioAttribute"/>, or null.</summary>
    public static double? RatioAttributeOf(BelemetryLogRecord log)
    {
        var ratio = log.SamplingRatio;
        return ratio > 0 && ratio < 1 ? RatioOf(log.Attributes, ratio) : null;
    }

    // A processor that set the attribute itself keeps its value.
    private static double? RatioOf(BelemetryAttributes attributes, double ratio) => attributes.Contains(RatioAttribute) ? null : ratio;
}
