namespace Bit.Belemetry;

/// <summary>Exports over OTLP: HTTP with protobuf or JSON payloads, or gRPC.</summary>
internal sealed class BelemetryOtlpExporter(BelemetryOtlpExporterOptions options, BelemetryMetricTemporality temporality, string name) : IBelemetryHttpExporter
{
    /// <summary>The content type of a gRPC request; the sender frames and sends it over HTTP/2.</summary>
    internal const string GrpcContentType = "application/grpc";

    private const string TracesPath = "v1/traces";
    private const string LogsPath = "v1/logs";
    private const string MetricsPath = "v1/metrics";
    private const string TracesMethod = "opentelemetry.proto.collector.trace.v1.TraceService/Export";
    private const string LogsMethod = "opentelemetry.proto.collector.logs.v1.LogsService/Export";
    private const string MetricsMethod = "opentelemetry.proto.collector.metrics.v1.MetricsService/Export";

    public string Name { get; } = name;

    public TimeSpan Timeout => options.Timeout;

    internal BelemetryOtlpExporterOptions Options => options;

    public IReadOnlyDictionary<string, string> Headers => options.Headers;

    public BelemetrySeverity? MinimumSeverity { get; } = BelemetryLogger.ToMinimumSeverity(options.MinimumLogLevel);

    public IEnumerable<BelemetryHttpRequest> CreateRequests(BelemetryBatch batch)
    {
        var protocol = options.Protocol;
        var json = protocol == BelemetryOtlpProtocol.HttpJson;
        var contentType = protocol == BelemetryOtlpProtocol.Grpc ? GrpcContentType : json ? "application/json" : "application/x-protobuf";

        if (batch.Spans.Count > 0 && options.Signals.HasFlag(BelemetrySignals.Traces))
        {
            yield return Create(Resolve(options.TracesEndpoint, options.Endpoint, protocol, BelemetrySignals.Traces), contentType,
                json ? OtlpJsonSerializer.SerializeTraces(batch) : OtlpProtobufSerializer.SerializeTraces(batch));
        }

        if (batch.Logs.Count > 0 && options.Signals.HasFlag(BelemetrySignals.Logs))
        {
            yield return Create(Resolve(options.LogsEndpoint, options.Endpoint, protocol, BelemetrySignals.Logs), contentType,
                json ? OtlpJsonSerializer.SerializeLogs(batch) : OtlpProtobufSerializer.SerializeLogs(batch));
        }

        if (batch.Metrics.Count > 0 && options.Signals.HasFlag(BelemetrySignals.Metrics))
        {
            yield return Create(Resolve(options.MetricsEndpoint, options.Endpoint, protocol, BelemetrySignals.Metrics), contentType,
                json ? OtlpJsonSerializer.SerializeMetrics(batch, temporality) : OtlpProtobufSerializer.SerializeMetrics(batch, temporality));
        }
    }

    public ValueTask PrepareAsync(HttpRequestMessage message, CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public BelemetryHttpRequest? GetRetryForPartialSuccess(BelemetryHttpRequest request, int statusCode, string? responseBody)
    {
        // OTLP partial success means the server already decided which items it rejected and why; the spec says
        // the client must not retry them.
        return null;
    }

    private BelemetryHttpRequest Create(string url, string contentType, byte[] body)
    {
        return new BelemetryHttpRequest
        {
            Exporter = Name,
            Url = url,
            ContentType = contentType,
            ContentEncoding = options.Compression == BelemetryCompression.Gzip ? "gzip" : null,
            Body = body,
        };
    }

    /// <summary>
    /// The URL of one signal. Over HTTP a signal-specific endpoint is used as-is and otherwise the signal path is
    /// appended to the base endpoint, as <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is defined to behave. Over gRPC both are
    /// base addresses that the service method is appended to.
    /// </summary>
    internal static string Resolve(Uri? signalEndpoint, Uri? baseEndpoint, BelemetryOtlpProtocol protocol, BelemetrySignals signal)
    {
        var grpc = protocol == BelemetryOtlpProtocol.Grpc;
        var path = signal switch
        {
            BelemetrySignals.Traces => grpc ? TracesMethod : TracesPath,
            BelemetrySignals.Logs => grpc ? LogsMethod : LogsPath,
            _ => grpc ? MetricsMethod : MetricsPath,
        };

        if (signalEndpoint is not null && grpc is false) return signalEndpoint.ToString();

        var root = (signalEndpoint ?? baseEndpoint ?? DefaultEndpoint(protocol)).ToString();
        return root.EndsWith('/') ? root + path : root + "/" + path;
    }

    /// <summary>The OTLP default ports: 4318 for HTTP, 4317 for gRPC.</summary>
    internal static Uri DefaultEndpoint(BelemetryOtlpProtocol protocol) =>
        protocol == BelemetryOtlpProtocol.Grpc ? new Uri("http://localhost:4317") : new Uri("http://localhost:4318");
}
