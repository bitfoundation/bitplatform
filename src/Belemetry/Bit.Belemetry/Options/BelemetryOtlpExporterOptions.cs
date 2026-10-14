namespace Bit.Belemetry;

/// <summary>
/// Settings of an OTLP/HTTP exporter - the vendor-neutral OpenTelemetry protocol understood by the OpenTelemetry
/// Collector, the Aspire dashboard, Grafana (Tempo/Loki/Mimir), Jaeger, Honeycomb, Datadog, New Relic, Elastic
/// and many more.
/// </summary>
/// <remarks>See <see href="https://opentelemetry.io/docs/specs/otlp/#otlphttp"/>.</remarks>
public sealed class BelemetryOtlpExporterOptions
{
    /// <summary>
    /// The base endpoint; <c>/v1/traces</c>, <c>/v1/logs</c> and <c>/v1/metrics</c> are appended to it.
    /// Defaults to <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> when set (outside the browser), else <c>http://localhost:4318</c>.
    /// </summary>
    public Uri? Endpoint { get; set; }

    /// <summary>A full traces endpoint, used verbatim instead of <see cref="Endpoint"/> + <c>/v1/traces</c>.</summary>
    public Uri? TracesEndpoint { get; set; }

    /// <summary>A full logs endpoint, used verbatim instead of <see cref="Endpoint"/> + <c>/v1/logs</c>.</summary>
    public Uri? LogsEndpoint { get; set; }

    /// <summary>A full metrics endpoint, used verbatim instead of <see cref="Endpoint"/> + <c>/v1/metrics</c>.</summary>
    public Uri? MetricsEndpoint { get; set; }

    /// <summary>
    /// The endpoint the browser script exports to, when it differs from <see cref="Endpoint"/> - typically a public,
    /// CORS-enabled route of a collector while the .NET side talks to it over an internal address.
    /// </summary>
    public Uri? BrowserEndpoint { get; set; }

    /// <summary>The payload encoding. Defaults to <see cref="BelemetryOtlpProtocol.HttpProtobuf"/>.</summary>
    public BelemetryOtlpProtocol Protocol { get; set; } = BelemetryOtlpProtocol.HttpProtobuf;

    /// <summary>Extra request headers (e.g. an API key). Merged with <c>OTEL_EXPORTER_OTLP_HEADERS</c> outside the browser.</summary>
    /// <remarks>Headers used by the browser exporter are visible to anyone who opens the page; prefer <see cref="BelemetryBrowserTransport.Relay"/> or a collector without credentials for browser traffic.</remarks>
    public Dictionary<string, string> Headers { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The request-body compression. Defaults to none.</summary>
    public BelemetryCompression Compression { get; set; } = BelemetryCompression.None;

    /// <summary>The maximum time one export request may take. Defaults to 10 seconds (the OTLP default).</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Whether the browser script exports to this endpoint too (with <see cref="BelemetryBrowserTransport.Direct"/>). Defaults to <see langword="true"/>.</summary>
    public bool IncludeInBrowser { get; set; } = true;

    /// <summary>The signals sent to this endpoint. Defaults to all three.</summary>
    public BelemetrySignals Signals { get; set; } = BelemetrySignals.All;
}
