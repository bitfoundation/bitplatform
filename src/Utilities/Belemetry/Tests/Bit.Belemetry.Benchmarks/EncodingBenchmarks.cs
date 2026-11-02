namespace Bit.Belemetry.Benchmarks;

/// <summary>
/// One full export batch (512 records, the default <see cref="BelemetryBatchOptions.MaxBatchSize"/>) through each of
/// the hand-written encoders - the work the export loop does per request, on the browser's only thread in WebAssembly.
/// </summary>
[MemoryDiagnoser]
public class EncodingBenchmarks
{
    private BelemetryBatch _logs = default!;
    private BelemetryBatch _spans = default!;
    private AzureMonitorEnvelopeSerializer.Context _azure = default!;

    [GlobalSetup]
    public void Setup()
    {
        var resource = new BelemetryResource(new BelemetryAttributes()
            .Set("service.name", "bench").Set("service.version", "1.0.0").Set("telemetry.sdk.name", "bit-belemetry"));
        var now = BelemetryTime.NowUnixNano();

        var logs = new List<BelemetryLogRecord>();
        var spans = new List<BelemetrySpan>();
        for (var i = 0; i < 512; i++)
        {
            var log = new BelemetryLogRecord { TimeUnixNano = now + i, ObservedTimeUnixNano = now + i, Body = $"Order {i} placed", Scope = new BelemetryScope("Bench.Orders") };
            log.Attributes.Set("OrderId", i).Set("Channel", "web").Set("Total", 19.99).Set("session.id", "4bf92f3577b34da6a3ce929d0e0e4736");
            logs.Add(log);

            var span = new BelemetrySpan
            {
                TraceId = "0af7651916cd43dd8448eb211c80319c",
                SpanId = (0x1000000000000000L + i).ToString("x16"),
                Name = "GET",
                Kind = BelemetrySpanKind.Client,
                StartTimeUnixNano = now,
                EndTimeUnixNano = now + 1_000_000,
                Scope = new BelemetryScope("System.Net.Http", "10.0.0"),
            };
            span.Attributes.Set("http.request.method", "GET").Set("url.full", $"https://api.example.com/orders/{i}").Set("server.address", "api.example.com")
                .Set("server.port", 443).Set("http.response.status_code", 200);
            spans.Add(span);
        }

        _logs = new BelemetryBatch(resource, logs: logs);
        _spans = new BelemetryBatch(resource, spans: spans);
        _azure = new AzureMonitorEnvelopeSerializer.Context("00000000-0000-0000-0000-000000000001", true);
    }

    [Benchmark(Description = "OTLP protobuf: 512 logs")]
    public byte[] OtlpProtobufLogs() => OtlpProtobufSerializer.SerializeLogs(_logs);

    [Benchmark(Description = "OTLP JSON: 512 logs")]
    public byte[] OtlpJsonLogs() => OtlpJsonSerializer.SerializeLogs(_logs);

    [Benchmark(Description = "OTLP protobuf: 512 spans")]
    public byte[] OtlpProtobufSpans() => OtlpProtobufSerializer.SerializeTraces(_spans);

    [Benchmark(Description = "Azure Monitor: 512 logs")]
    public byte[] AzureLogs() => AzureMonitorEnvelopeSerializer.Serialize(_logs, _azure, out _);
}
