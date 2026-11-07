namespace Bit.Belemetry;

/// <summary>
/// An exception logged while a span was current (<see cref="BelemetrySpanEvent.LoggedAs"/>): enough to match it to an
/// exception recorded on that span, and to tell whether an exporter sends the log record. It holds no stack trace - only
/// its length and hash - so a span that never ends keeps little alive.
/// </summary>
/// <param name="type">Its <c>exception.type</c>.</param>
/// <param name="message">Its <c>exception.message</c>.</param>
/// <param name="stacktrace">Its <c>exception.stacktrace</c>.</param>
/// <param name="severity">The severity of its log record.</param>
/// <param name="isEvent">Whether its log record is an event (<see cref="IBelemetry.TrackException"/>), which every exporter's minimum level lets through.</param>
internal sealed class BelemetryLoggedException(string? type, string? message, string? stacktrace, BelemetrySeverity severity, bool isEvent)
{
    private readonly int _stackLength = stacktrace?.Length ?? -1;
    private readonly int _stackHash = stacktrace?.GetHashCode() ?? 0;
    private volatile bool _dropped;

    /// <summary>The logged exception of <paramref name="log"/>, a record that carries one, as privacy and the processors left it.</summary>
    public static BelemetryLoggedException Of(BelemetryLogRecord log) =>
        new(log.Attributes.GetString(BelemetrySemanticConventions.ExceptionType), log.Attributes.GetString(BelemetrySemanticConventions.ExceptionMessage),
            log.Attributes.GetString(BelemetrySemanticConventions.ExceptionStacktrace), log.Severity, log.EventName is not null);

    /// <summary>The severity of its log record.</summary>
    public BelemetrySeverity Severity => severity;

    /// <summary>Whether its log record is an event (<see cref="IBelemetry.TrackException"/>).</summary>
    public bool IsEvent => isEvent;

    /// <summary>
    /// Whether its log record was dropped before any exporter had it (a full queue or hold, a consent decision or revoke):
    /// it then stands for nothing, and the span sends its exception itself.
    /// </summary>
    public bool IsDropped => _dropped;

    /// <summary>Says its log record was dropped before any exporter had it (<see cref="IsDropped"/>).</summary>
    public void Drop() => _dropped = true;

    /// <summary>
    /// Whether <paramref name="attributes"/>, those of an exception event, describe the same exception: the same type,
    /// message and stack trace. Both sides write the stack as <c>Exception.ToString()</c>, so two failures that share a
    /// type and message still differ by where they happened. Without a stack trace on either side the identity is
    /// unknown, and it is not the same: the span keeps its exception.
    /// </summary>
    public bool Matches(BelemetryAttributes attributes)
    {
        if (_stackLength < 0 || attributes.GetString(BelemetrySemanticConventions.ExceptionStacktrace) is not { } stack) return false;

        return stack.Length == _stackLength && stack.GetHashCode() == _stackHash
               && type == attributes.GetString(BelemetrySemanticConventions.ExceptionType)
               && message == attributes.GetString(BelemetrySemanticConventions.ExceptionMessage);
    }

    /// <summary>Whether an exporter with the <paramref name="minimum"/> log level gets its log record.</summary>
    public bool Passes(BelemetrySeverity? minimum) => BelemetryExporterSet.PassesMinimumSeverity(isEvent, severity, minimum);
}
