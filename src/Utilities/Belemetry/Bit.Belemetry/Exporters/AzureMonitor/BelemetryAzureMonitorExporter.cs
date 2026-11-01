using System.Net.Http.Headers;
using System.Text.Json;

namespace Bit.Belemetry;

/// <summary>Exports to the Application Insights ingestion endpoint (<c>/v2.1/track</c>).</summary>
internal sealed class BelemetryAzureMonitorExporter : IBelemetryHttpExporter
{
    private readonly BelemetryAzureMonitorExporterOptions _options;
    private readonly AzureMonitorEnvelopeSerializer.Context _context;

    public BelemetryAzureMonitorExporter(BelemetryAzureMonitorExporterOptions options, string name)
    {
        _options = options;
        Name = name;
        ConnectionString = AzureMonitorConnectionString.Parse(options.ConnectionString!);
        _context = new AzureMonitorEnvelopeSerializer.Context(ConnectionString.InstrumentationKey, options.MaskIp);
        MinimumSeverity = BelemetryLogger.ToMinimumSeverity(options.MinimumLogLevel);
    }

    public string Name { get; }

    public TimeSpan Timeout => _options.Timeout;

    public AzureMonitorConnectionString ConnectionString { get; }

    internal BelemetryAzureMonitorExporterOptions Options => _options;

    // Authentication is the connection string's key (in the body) or a token from PrepareAsync.
    public IReadOnlyDictionary<string, string> Headers { get; } = new Dictionary<string, string>();

    public BelemetrySeverity? MinimumSeverity { get; }

    public IEnumerable<BelemetryHttpRequest> CreateRequests(BelemetryBatch batch)
    {
        var filtered = new BelemetryBatch(batch.Resource,
            _options.Signals.HasFlag(BelemetrySignals.Traces) ? batch.Spans : [],
            _options.Signals.HasFlag(BelemetrySignals.Logs) ? batch.Logs : [],
            _options.Signals.HasFlag(BelemetrySignals.Metrics) ? batch.Metrics : []);

        if (filtered.IsEmpty) yield break;

        var body = AzureMonitorEnvelopeSerializer.Serialize(filtered, _context, out var count);
        if (count == 0) yield break;

        yield return new BelemetryHttpRequest
        {
            Exporter = Name,
            Url = ConnectionString.TrackUrl,
            ContentType = "application/json",
            ContentEncoding = _options.Compression == BelemetryCompression.Gzip ? "gzip" : null,
            Body = body,
            Count = count,
        };
    }

    public bool PostsTo(string url) => url == ConnectionString.TrackUrl;

    public async ValueTask PrepareAsync(HttpRequestMessage message, CancellationToken cancellationToken)
    {
        if (_options.AccessTokenProvider is null) return;

        var token = await _options.AccessTokenProvider(cancellationToken);
        if (string.IsNullOrEmpty(token) is false) message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public bool IsRetryable(int statusCode) => BelemetryHttpSender.IsRetryable((System.Net.HttpStatusCode)statusCode);

    // A 206 is settled item by item (GetRetryForPartialSuccess); a 200 accepted everything.
    public string? DescribePartialSuccess(string? mediaType, ReadOnlySpan<byte> body, out long rejected)
    {
        rejected = 0;
        return null;
    }

    public BelemetryHttpRequest? GetRetryForPartialSuccess(BelemetryHttpRequest request, int statusCode, string? responseBody, out int rejected)
    {
        rejected = 0;
        if (statusCode != 206 || string.IsNullOrEmpty(responseBody)) return null;

        try
        {
            using var response = JsonDocument.Parse(responseBody);
            if (response.RootElement.TryGetProperty("errors", out var errors) is false || errors.ValueKind != JsonValueKind.Array) return null;

            // The items listed with a status the endpoint asks to retry go again; the others are refused for good.
            var retryIndexes = new HashSet<int>();
            var refused = new HashSet<int>();
            foreach (var error in errors.EnumerateArray())
            {
                if (error.TryGetProperty("index", out var index) is false || error.TryGetProperty("statusCode", out var code) is false) continue;
                (BelemetryHttpSender.IsRetryable((System.Net.HttpStatusCode)code.GetInt32()) ? retryIndexes : refused).Add(index.GetInt32());
            }

            rejected = refused.Count;
            if (retryIndexes.Count == 0) return null;

            using var original = JsonDocument.Parse(request.Body);
            var buffer = new System.Buffers.ArrayBufferWriter<byte>();
            var count = 0;
            using (var writer = new Utf8JsonWriter(buffer))
            {
                writer.WriteStartArray();
                var i = 0;
                foreach (var item in original.RootElement.EnumerateArray())
                {
                    if (retryIndexes.Contains(i++) is false) continue;
                    item.WriteTo(writer);
                    count++;
                }
                writer.WriteEndArray();
            }

            return new BelemetryHttpRequest
            {
                Exporter = request.Exporter,
                Url = request.Url,
                ContentType = request.ContentType,
                ContentEncoding = request.ContentEncoding,
                Headers = request.Headers,
                Body = buffer.WrittenSpan.ToArray(),
                // Its age is the original's (a stored retry still expires on time), its records are only the retried ones.
                Time = request.Time,
                Count = count,
            };
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException)
        {
            // Unreadable (an index or a status that is not a number, too): the accepted items are in, nothing to retry.
            rejected = 0;
            return null;
        }
    }
}
