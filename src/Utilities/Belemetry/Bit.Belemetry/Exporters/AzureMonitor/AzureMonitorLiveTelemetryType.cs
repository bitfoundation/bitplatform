namespace Bit.Belemetry;

/// <summary>
/// The Live Metrics telemetry types Belemetry streams samples of and computes custom charts over - the QuickPulse
/// <c>TelemetryType</c> values less <c>Metric</c> and <c>PerformanceCounter</c>, which it has no items of.
/// </summary>
internal enum AzureMonitorLiveTelemetryType
{
    /// <summary>A server or consumer span.</summary>
    Request,

    /// <summary>Any other span.</summary>
    Dependency,

    /// <summary>A log record with an exception, or an exception recorded on a span.</summary>
    Exception,

    /// <summary>A custom event (a log record with an event name).</summary>
    Event,

    /// <summary>Any other log record.</summary>
    Trace,
}
