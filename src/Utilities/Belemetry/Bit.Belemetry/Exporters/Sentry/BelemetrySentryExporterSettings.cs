namespace Bit.Belemetry;

/// <summary>A Sentry exporter's options and the DSN they name, parsed once as the options are resolved (<see cref="BelemetrySettings"/>).</summary>
/// <param name="Options">The options.</param>
/// <param name="Dsn">Their <see cref="BelemetrySentryExporterOptions.Dsn"/>, parsed.</param>
internal sealed record BelemetrySentryExporterSettings(BelemetrySentryExporterOptions Options, SentryDsn Dsn);
