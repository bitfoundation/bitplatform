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
    }

    public string Name { get; }

    public TimeSpan Timeout => _options.Timeout;

    public AzureMonitorConnectionString ConnectionString { get; }

    internal BelemetryAzureMonitorExporterOptions Options => _options;

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
        };
    }

    public async ValueTask PrepareAsync(HttpRequestMessage message, CancellationToken cancellationToken)
    {
        if (_options.AccessTokenProvider is null) return;

        var token = await _options.AccessTokenProvider(cancellationToken);
        if (string.IsNullOrEmpty(token) is false) message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public BelemetryHttpRequest? GetRetryForPartialSuccess(BelemetryHttpRequest request, int statusCode, string? responseBody)
    {
        if (statusCode != 206 || string.IsNullOrEmpty(responseBody)) return null;

        try
        {
            using var response = JsonDocument.Parse(responseBody);
            if (response.RootElement.TryGetProperty("errors", out var errors) is false || errors.ValueKind != JsonValueKind.Array) return null;

            var retryIndexes = new HashSet<int>();
            foreach (var error in errors.EnumerateArray())
            {
                if (error.TryGetProperty("index", out var index) && error.TryGetProperty("statusCode", out var code)
                    && BelemetryHttpSender.IsRetryable((System.Net.HttpStatusCode)code.GetInt32()))
                {
                    retryIndexes.Add(index.GetInt32());
                }
            }

            if (retryIndexes.Count == 0) return null;

            using var original = JsonDocument.Parse(request.Body);
            var buffer = new System.Buffers.ArrayBufferWriter<byte>();
            using (var writer = new Utf8JsonWriter(buffer))
            {
                writer.WriteStartArray();
                var i = 0;
                foreach (var item in original.RootElement.EnumerateArray())
                {
                    if (retryIndexes.Contains(i++)) item.WriteTo(writer);
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
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
