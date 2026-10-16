namespace Bit.Belemetry;

/// <summary>
/// Settings of an OTLP exporter - the vendor-neutral OpenTelemetry protocol understood by the OpenTelemetry
/// Collector, the Aspire dashboard, Grafana (Tempo/Loki/Mimir), Jaeger, Honeycomb, Datadog, New Relic, Elastic
/// and many more - over HTTP (protobuf or JSON) or gRPC.
/// </summary>
/// <remarks>See <see href="https://opentelemetry.io/docs/specs/otlp/"/>.</remarks>
public sealed class BelemetryOtlpExporterOptions
{
    private BelemetryOtlpProtocol? _protocol;
    private TimeSpan? _timeout;
    private BelemetryCompression? _compression;

    /// <summary>
    /// The base endpoint; <c>/v1/traces</c>, <c>/v1/logs</c> and <c>/v1/metrics</c> (or the gRPC service methods) are
    /// appended to it. Defaults to <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> when set (outside the browser), else
    /// <c>http://localhost:4318</c> for OTLP/HTTP and <c>http://localhost:4317</c> for gRPC.
    /// </summary>
    public Uri? Endpoint { get; set; }

    /// <summary>
    /// A full traces endpoint, used verbatim instead of <see cref="Endpoint"/> + <c>/v1/traces</c>. Defaults to
    /// <c>OTEL_EXPORTER_OTLP_TRACES_ENDPOINT</c> when set (outside the browser). With gRPC it is a base address
    /// (the service method is appended), as the specification defines the variable.
    /// </summary>
    public Uri? TracesEndpoint { get; set; }

    /// <summary>A full logs endpoint, used verbatim instead of <see cref="Endpoint"/> + <c>/v1/logs</c>. Defaults to <c>OTEL_EXPORTER_OTLP_LOGS_ENDPOINT</c> when set.</summary>
    public Uri? LogsEndpoint { get; set; }

    /// <summary>A full metrics endpoint, used verbatim instead of <see cref="Endpoint"/> + <c>/v1/metrics</c>. Defaults to <c>OTEL_EXPORTER_OTLP_METRICS_ENDPOINT</c> when set.</summary>
    public Uri? MetricsEndpoint { get; set; }

    /// <summary>
    /// The endpoint the browser script exports to, when it differs from <see cref="Endpoint"/> - typically a public,
    /// CORS-enabled route of a collector while the .NET side talks to it over an internal address. With
    /// <see cref="BelemetryOtlpProtocol.Grpc"/> this is the only way the browser exports: it posts OTLP/HTTP
    /// protobuf here, since a page cannot speak gRPC; in a WebAssembly app the .NET pipeline does the same, so the
    /// endpoint must be absolute there.
    /// </summary>
    public Uri? BrowserEndpoint { get; set; }

    /// <summary>
    /// The payload encoding. Defaults to <c>OTEL_EXPORTER_OTLP_PROTOCOL</c> when set (outside the browser), else
    /// <see cref="BelemetryOtlpProtocol.HttpProtobuf"/>. A value set in code always wins over the variable.
    /// </summary>
    public BelemetryOtlpProtocol Protocol
    {
        get => _protocol ?? BelemetryOtlpProtocol.HttpProtobuf;
        set => _protocol = value;
    }

    /// <summary>Whether <see cref="Protocol"/> was set explicitly (so the environment must not override it).</summary>
    internal bool IsProtocolSet => _protocol.HasValue;

    /// <summary>Extra request headers (e.g. an API key). Merged with <c>OTEL_EXPORTER_OTLP_HEADERS</c> outside the browser.</summary>
    /// <remarks>Headers used by the browser exporter are visible to anyone who opens the page; prefer <see cref="BelemetryBrowserTransport.Relay"/> or a collector without credentials for browser traffic.</remarks>
    public Dictionary<string, string> Headers { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The request-body compression (for gRPC, the message compression with <c>grpc-encoding</c>). Defaults to
    /// <c>OTEL_EXPORTER_OTLP_COMPRESSION</c> when set (outside the browser), else none.
    /// </summary>
    public BelemetryCompression Compression
    {
        get => _compression ?? BelemetryCompression.None;
        set => _compression = value;
    }

    /// <summary>Whether <see cref="Compression"/> was set explicitly (so the environment must not override it).</summary>
    internal bool IsCompressionSet => _compression.HasValue;

    /// <summary>
    /// The maximum time one export request may take. Defaults to <c>OTEL_EXPORTER_OTLP_TIMEOUT</c> (milliseconds) when
    /// set (outside the browser), else 10 seconds (the OTLP default).
    /// </summary>
    public TimeSpan Timeout
    {
        get => _timeout ?? TimeSpan.FromSeconds(10);
        set => _timeout = value;
    }

    /// <summary>Whether <see cref="Timeout"/> was set explicitly (so the environment must not override it).</summary>
    internal bool IsTimeoutSet => _timeout.HasValue;

    /// <summary>Whether the browser script exports to this endpoint too (with <see cref="BelemetryBrowserTransport.Direct"/>). Defaults to <see langword="true"/>.</summary>
    public bool IncludeInBrowser { get; set; } = true;

    /// <summary>The signals sent to this endpoint. Defaults to all three.</summary>
    public BelemetrySignals Signals { get; set; } = BelemetrySignals.All;

    /// <summary>
    /// The least severe log level this exporter receives from the .NET pipeline (its own logs and the records the
    /// browser relays); plain log records below it are left out of this exporter's batches only. Events - page views,
    /// interactions, web vitals, custom events, exceptions - are not logs to a log level and always pass, as do spans
    /// and metrics. <see langword="null"/> (the default) sends every collected record. Use it when one backend should
    /// get errors only while another gets everything, as a per-provider logging level would with two logging providers.
    /// </summary>
    public Microsoft.Extensions.Logging.LogLevel? MinimumLogLevel { get; set; }
}
