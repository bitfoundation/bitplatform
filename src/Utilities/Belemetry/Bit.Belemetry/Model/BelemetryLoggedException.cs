namespace Bit.Belemetry;

/// <summary>
/// An exception logged while a span was current (<see cref="BelemetrySpanEvent.LoggedAs"/>): enough to match it to an
/// exception recorded on that span, and to tell whether an exporter sends the log record. It holds no stack trace, so a
/// span that never ends keeps little alive.
/// </summary>
/// <param name="Type">Its <c>exception.type</c>.</param>
/// <param name="Message">Its <c>exception.message</c>.</param>
/// <param name="Severity">The severity of its log record.</param>
/// <param name="IsEvent">Whether its log record is an event (<see cref="IBelemetry.TrackException"/>), which every exporter's minimum level lets through.</param>
internal readonly record struct BelemetryLoggedException(string? Type, string? Message, BelemetrySeverity Severity, bool IsEvent)
{
    public static BelemetryLoggedException Of(BelemetryLogRecord log) =>
        new(log.Attributes.GetString(BelemetrySemanticConventions.ExceptionType), log.Attributes.GetString(BelemetrySemanticConventions.ExceptionMessage),
            log.Severity, log.EventName is not null);

    /// <summary>Whether <paramref name="attributes"/>, those of an exception event, describe the same exception.</summary>
    public bool Matches(BelemetryAttributes attributes) =>
        Type == attributes.GetString(BelemetrySemanticConventions.ExceptionType) && Message == attributes.GetString(BelemetrySemanticConventions.ExceptionMessage);

    /// <summary>Whether an exporter with the <paramref name="minimum"/> log level gets its log record.</summary>
    public bool Passes(BelemetrySeverity? minimum) => BelemetryExporterSet.PassesMinimumSeverity(IsEvent, Severity, minimum);
}
