namespace Bit.Belemetry;

/// <summary>Exports over OTLP/HTTP, as protobuf or JSON.</summary>
internal sealed class BelemetryOtlpExporter(BelemetryOtlpExporterOptions options, BelemetryMetricTemporality temporality, string name) : IBelemetryHttpExporter
{
    public string Name { get; } = name;

    public TimeSpan Timeout => options.Timeout;

    internal BelemetryOtlpExporterOptions Options => options;

    public IEnumerable<BelemetryHttpRequest> CreateRequests(BelemetryBatch batch)
    {
        var json = options.Protocol == BelemetryOtlpProtocol.HttpJson;
        var contentType = json ? "application/json" : "application/x-protobuf";

        if (batch.Spans.Count > 0 && options.Signals.HasFlag(BelemetrySignals.Traces))
        {
            yield return Create(Resolve(options.TracesEndpoint, "v1/traces"), contentType,
                json ? OtlpJsonSerializer.SerializeTraces(batch) : OtlpProtobufSerializer.SerializeTraces(batch));
        }

        if (batch.Logs.Count > 0 && options.Signals.HasFlag(BelemetrySignals.Logs))
        {
            yield return Create(Resolve(options.LogsEndpoint, "v1/logs"), contentType,
                json ? OtlpJsonSerializer.SerializeLogs(batch) : OtlpProtobufSerializer.SerializeLogs(batch));
        }

        if (batch.Metrics.Count > 0 && options.Signals.HasFlag(BelemetrySignals.Metrics))
        {
            yield return Create(Resolve(options.MetricsEndpoint, "v1/metrics"), contentType,
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
            Headers = new Dictionary<string, string>(options.Headers, StringComparer.OrdinalIgnoreCase),
            Body = body,
        };
    }

    private string Resolve(Uri? signalEndpoint, string path) => Resolve(signalEndpoint, options.Endpoint, path);

    /// <summary>
    /// The per-signal URL: a signal-specific endpoint is used as-is; otherwise the signal path is appended to the
    /// base endpoint, as <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is defined to behave.
    /// </summary>
    internal static string Resolve(Uri? signalEndpoint, Uri? baseEndpoint, string path)
    {
        if (signalEndpoint is not null) return signalEndpoint.ToString();

        var root = (baseEndpoint ?? new Uri("http://localhost:4318")).ToString();
        return root.EndsWith('/') ? root + path : root + "/" + path;
    }
}
