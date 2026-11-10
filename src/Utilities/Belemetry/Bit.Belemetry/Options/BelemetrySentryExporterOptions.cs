namespace Bit.Belemetry;

/// <summary>
/// Settings of the Sentry exporter, which posts error events to the project's envelope endpoint
/// (<c>/api/{project}/envelope/</c>), so exceptions become Sentry Issues - grouped by their stack traces, symbolicated
/// from the PDBs uploaded to Sentry, and linked to their traces - without a Sentry SDK in the app. Spans, logs and
/// metrics are left to the other exporters (Sentry also takes OTLP traces and logs, through an OTLP exporter).
/// </summary>
public sealed class BelemetrySentryExporterOptions
{
    /// <summary>
    /// The project's DSN (<c>https://{public key}@{host}/{project id}</c>). The public key is public by design - Sentry's
    /// browser SDK puts it in every page - so the browser script gets it too; a legacy secret key in the DSN is never
    /// used or handed to the page. Defaults to <c>SENTRY_DSN</c> when set (outside the browser, with
    /// <see cref="BelemetryOptions.UseEnvironmentVariables"/>) - which never adds an exporter by itself. An invalid DSN
    /// skips the exporter, with a startup warning.
    /// </summary>
    public string? Dsn { get; set; }

    /// <summary>
    /// The Sentry release of every event. Defaults to <c>{service.name}@{service.version}</c> of the records'
    /// resource (the shape Sentry's own SDKs use), and to none without a service version.
    /// </summary>
    public string? Release { get; set; }

    /// <summary>
    /// The least severe record that becomes a Sentry event: a record carrying an exception (a log record with
    /// <c>exception.type</c> or <c>exception.message</c>, <see cref="IBelemetry.TrackException"/>, a JS error, an
    /// exception recorded on a span) becomes an error event with its stack trace, and a plain log record (no exception,
    /// not an event such as a page view) a message event. The default, <see cref="Microsoft.Extensions.Logging.LogLevel.Error"/>,
    /// is what Sentry's own logging integration sends as events. An exception recorded on a span counts as an error.
    /// </summary>
    public Microsoft.Extensions.Logging.LogLevel MinimumEventLevel { get; set; } = Microsoft.Extensions.Logging.LogLevel.Error;

    /// <summary>
    /// Whether the .NET exceptions nothing caught (<see cref="BelemetryErrorOptions.CaptureUnhandledExceptions"/>:
    /// <c>AppDomain.UnhandledException</c>, unobserved task exceptions, and what
    /// <see cref="Belemetry.TrackBitBelemetryUnhandledException"/> forwards) are sent to Sentry. Set it to
    /// <see langword="false"/> where a Sentry SDK runs as the crash reporter (Android, iOS and Mac Catalyst, for the
    /// native crashes Belemetry cannot see), which reports these as well. Defaults to <see langword="true"/>.
    /// </summary>
    public bool IncludeUnhandledExceptions { get; set; } = true;

    /// <summary>
    /// Whether Sentry is told not to infer the user's IP address from the request (<c>sdk.settings.infer_ip = never</c>),
    /// so it neither stores it nor derives a location from it. Defaults to <see langword="true"/>.
    /// </summary>
    public bool MaskIp { get; set; } = true;

    /// <summary>The request-body compression. Defaults to none.</summary>
    public BelemetryCompression Compression { get; set; } = BelemetryCompression.None;

    /// <summary>The maximum time one export request may take. Defaults to 30 seconds.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Whether the browser script sends its errors to Sentry too (with <see cref="BelemetryBrowserTransport.Direct"/>). Defaults to <see langword="true"/>.</summary>
    public bool IncludeInBrowser { get; set; } = true;
}
