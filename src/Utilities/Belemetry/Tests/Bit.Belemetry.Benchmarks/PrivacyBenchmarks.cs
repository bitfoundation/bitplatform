namespace Bit.Belemetry.Benchmarks;

/// <summary>
/// The privacy rules run on every record, and on every string attribute of it: they have to cost little for the values
/// that match no pattern - methods, routes, status texts - which are nearly all of them. Each case builds its record,
/// so subtract the matching <c>Build</c> baseline to read the cost of the rules alone.
/// </summary>
[MemoryDiagnoser]
public class PrivacyBenchmarks
{
    private readonly BelemetryPrivacy _privacy = new(new BelemetryPrivacyOptions());

    [Benchmark(Baseline = true, Description = "HttpClient span: build")]
    public BelemetrySpan BuildHttpSpan() => HttpSpan();

    [Benchmark(Description = "HttpClient span: build + privacy")]
    public BelemetrySpan HttpSpanWithPrivacy()
    {
        var span = HttpSpan();
        _privacy.Apply(span);
        return span;
    }

    [Benchmark(Description = "HttpClient span, URL without a query: build + privacy")]
    public BelemetrySpan HttpSpanWithoutQueryWithPrivacy()
    {
        var span = HttpSpan("https://api.example.com/orders/42");
        _privacy.Apply(span);
        return span;
    }

    [Benchmark(Description = "Structured log: build")]
    public BelemetryLogRecord BuildStructuredLog() => StructuredLog();

    [Benchmark(Description = "Structured log: build + privacy (an e-mail to scrub)")]
    public BelemetryLogRecord StructuredLogWithPrivacy()
    {
        var log = StructuredLog();
        _privacy.Apply(log);
        return log;
    }

    private static BelemetrySpan HttpSpan(string url = "https://api.example.com/orders/42?page=2&token=abc123") => new()
    {
        TraceId = "0af7651916cd43dd8448eb211c80319c",
        SpanId = "b7ad6b7169203331",
        Name = "GET",
        Kind = BelemetrySpanKind.Client,
        Attributes = new BelemetryAttributes()
            .Set("http.request.method", "GET")
            .Set("url.full", url)
            .Set("server.address", "api.example.com")
            .Set("server.port", 443)
            .Set("network.protocol.version", "1.1")
            .Set("http.response.status_code", 200),
    };

    private static BelemetryLogRecord StructuredLog()
    {
        var log = new BelemetryLogRecord { Body = "Order 42 placed by jane@example.com for 19.99 EUR" };
        log.Attributes.Set("OrderId", 42).Set("Email", "jane@example.com").Set("Total", 19.99).Set("Currency", "EUR")
            .Set("log.event.name", "OrderPlaced");
        return log;
    }
}
