using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Bit.Belemetry.Tests.Http;

[TestClass]
public class BelemetryHttpSenderTests
{
    private readonly List<string> _diagnostics = [];

    private BelemetryHttpSender CreateSender(FakeHttpMessageHandler handler) => new(handler, (_, message) => _diagnostics.Add(message))
    {
        Backoff = [TimeSpan.Zero, TimeSpan.Zero],
    };

    private static BelemetryOtlpExporter Otlp(Action<BelemetryOtlpExporterOptions>? configure = null)
    {
        var options = new BelemetryOtlpExporterOptions { Endpoint = new Uri("http://collector.test:4318") };
        configure?.Invoke(options);
        return new BelemetryOtlpExporter(options, BelemetryMetricTemporality.Cumulative, "otlp");
    }

    private static BelemetryHttpRequest LogsRequest(BelemetryOtlpExporter exporter) =>
        exporter.CreateRequests(new BelemetryBatch(Records.Resource(), logs: [Records.Log()])).Single();

    [TestMethod]
    public async Task Delivers_WithHeadersAndContentType()
    {
        var handler = new FakeHttpMessageHandler();
        using var sender = CreateSender(handler);
        var exporter = Otlp(o =>
        {
            o.Headers["x-api-key"] = "secret";
            o.Headers["Authorization"] = "Basic abc";
            o.Headers["Content-Language"] = "en";
        });
        var request = LogsRequest(exporter);

        var outcome = await sender.SendAsync(exporter, request, retry: true, CancellationToken.None);

        Assert.AreEqual(BelemetrySendOutcome.Delivered, outcome);
        var sent = handler.Requests.Single();
        Assert.AreEqual(HttpMethod.Post, sent.Message.Method);
        Assert.AreEqual(new Uri("http://collector.test:4318/v1/logs"), sent.Message.RequestUri);
        Assert.AreEqual("application/x-protobuf", sent.Message.Content!.Headers.ContentType!.MediaType);
        Assert.AreEqual("secret", sent.Message.Headers.GetValues("x-api-key").Single());
        Assert.AreEqual("Basic abc", sent.Message.Headers.GetValues("Authorization").Single());
        Assert.AreEqual("en", sent.Message.Content.Headers.GetValues("Content-Language").Single(), "Content headers go on the content.");
        Assert.IsEmpty(sent.Message.Content.Headers.ContentEncoding);
        CollectionAssert.AreEqual(request.Body, sent.Body);
    }

    [TestMethod]
    public async Task Gzip_CompressesTheBody()
    {
        var handler = new FakeHttpMessageHandler();
        using var sender = CreateSender(handler);
        var exporter = Otlp(o => o.Compression = BelemetryCompression.Gzip);
        var request = LogsRequest(exporter);
        Assert.AreEqual("gzip", request.ContentEncoding);

        await sender.SendAsync(exporter, request, retry: false, CancellationToken.None);

        var sent = handler.Requests.Single();
        Assert.AreEqual("gzip", sent.Message.Content!.Headers.ContentEncoding.Single());
        using var input = new GZipStream(new MemoryStream(sent.Body), CompressionMode.Decompress);
        using var output = new MemoryStream();
        input.CopyTo(output);
        CollectionAssert.AreEqual(request.Body, output.ToArray());
    }

    [TestMethod]
    [DataRow(HttpStatusCode.ServiceUnavailable)]
    [DataRow(HttpStatusCode.TooManyRequests)]
    public async Task Retries_HonoringRetryAfter(HttpStatusCode status)
    {
        var handler = new FakeHttpMessageHandler()
            .Enqueue(status, r => r.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromMilliseconds(1200)))
            .Enqueue(HttpStatusCode.OK);
        using var sender = CreateSender(handler);
        var exporter = Otlp();

        var outcome = await sender.SendAsync(exporter, LogsRequest(exporter), retry: true, CancellationToken.None);

        Assert.AreEqual(BelemetrySendOutcome.Delivered, outcome);
        Assert.HasCount(2, handler.Requests);
        // RetryConditionHeaderValue carries whole seconds: 1200 ms is sent as "Retry-After: 1".
        Assert.IsGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(900), handler.Requests[1].Time - handler.Requests[0].Time);
    }

    [TestMethod]
    public async Task RetryAfterDate_InThePast_RetriesImmediately()
    {
        var handler = new FakeHttpMessageHandler()
            .Enqueue(HttpStatusCode.ServiceUnavailable, r => r.Headers.RetryAfter = new RetryConditionHeaderValue(DateTimeOffset.UtcNow.AddMinutes(-1)))
            .Enqueue(HttpStatusCode.OK);
        using var sender = CreateSender(handler);
        var exporter = Otlp();

        Assert.AreEqual(BelemetrySendOutcome.Delivered, await sender.SendAsync(exporter, LogsRequest(exporter), retry: true, CancellationToken.None));
        Assert.HasCount(2, handler.Requests);
    }

    [TestMethod]
    public async Task GivesUp_AfterTheBackoffSteps()
    {
        var handler = new FakeHttpMessageHandler { Fallback = HttpStatusCode.BadGateway };
        using var sender = CreateSender(handler);
        var exporter = Otlp();

        Assert.AreEqual(BelemetrySendOutcome.Failed, await sender.SendAsync(exporter, LogsRequest(exporter), retry: true, CancellationToken.None));
        Assert.HasCount(3, handler.Requests);

        handler.Requests.Clear();
        Assert.AreEqual(BelemetrySendOutcome.Failed, await sender.SendAsync(exporter, LogsRequest(exporter), retry: false, CancellationToken.None));
        Assert.HasCount(1, handler.Requests);
    }

    [TestMethod]
    [DataRow(HttpStatusCode.BadRequest)]
    [DataRow(HttpStatusCode.Unauthorized)]
    [DataRow(HttpStatusCode.NotFound)]
    public async Task DoesNotRetry_NonRetryableStatus(HttpStatusCode status)
    {
        var handler = new FakeHttpMessageHandler().Enqueue(status);
        using var sender = CreateSender(handler);
        var exporter = Otlp();

        Assert.AreEqual(BelemetrySendOutcome.Rejected, await sender.SendAsync(exporter, LogsRequest(exporter), retry: true, CancellationToken.None));
        Assert.HasCount(1, handler.Requests);
        Assert.IsTrue(_diagnostics.Any(d => d.Contains("dropped")));
    }

    [TestMethod]
    public async Task NetworkFailures_AreRetried()
    {
        var calls = 0;
        var handler = new ThrowingHandler(() => ++calls < 3);
        using var sender = new BelemetryHttpSender(handler, (_, _) => { }) { Backoff = [TimeSpan.Zero, TimeSpan.Zero] };
        var exporter = Otlp();

        Assert.AreEqual(BelemetrySendOutcome.Delivered, await sender.SendAsync(exporter, LogsRequest(exporter), retry: true, CancellationToken.None));
        Assert.AreEqual(3, calls);
    }

    [TestMethod]
    public async Task Cancellation_Fails()
    {
        var handler = new FakeHttpMessageHandler { Fallback = HttpStatusCode.ServiceUnavailable };
        using var sender = new BelemetryHttpSender(handler, (_, _) => { });
        var exporter = Otlp();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        Assert.AreEqual(BelemetrySendOutcome.Failed, await sender.SendAsync(exporter, LogsRequest(exporter), retry: true, cts.Token));
        Assert.HasCount(1, handler.Requests, "The default 1 s backoff was interrupted by the cancellation.");
    }

    [TestMethod]
    public void RetryableStatusCodes()
    {
        foreach (var code in new[] { 408, 429, 439, 500, 502, 503, 504 }) Assert.IsTrue(BelemetryHttpSender.IsRetryable((HttpStatusCode)code), code.ToString());
        foreach (var code in new[] { 200, 206, 400, 401, 403, 404, 413, 501 }) Assert.IsFalse(BelemetryHttpSender.IsRetryable((HttpStatusCode)code), code.ToString());
    }

    [TestMethod]
    public async Task AzurePartialSuccess_ResendsOnlyTheRejectedItems()
    {
        var handler = new FakeHttpMessageHandler()
            .Enqueue((HttpStatusCode)206, r => r.Content = new StringContent("""{"itemsReceived":2,"itemsAccepted":1,"errors":[{"index":1,"statusCode":500}]}""", Encoding.UTF8, "application/json"))
            .Enqueue(HttpStatusCode.OK);
        using var sender = CreateSender(handler);
        var exporter = new BelemetryAzureMonitorExporter(new BelemetryAzureMonitorExporterOptions
        {
            ConnectionString = "InstrumentationKey=11111111-2222-3333-4444-555555555555;IngestionEndpoint=https://ingest.test/",
            AccessTokenProvider = _ => ValueTask.FromResult<string?>("tok"),
        }, "azuremonitor");
        var request = exporter.CreateRequests(new BelemetryBatch(Records.Resource(), logs: [Records.Log("first"), Records.Log("second")])).Single();

        Assert.AreEqual(BelemetrySendOutcome.Delivered, await sender.SendAsync(exporter, request, retry: true, CancellationToken.None));

        Assert.HasCount(2, handler.Requests);
        Assert.AreEqual("Bearer tok", handler.Requests[0].Message.Headers.Authorization!.ToString());
        Assert.AreEqual(new Uri("https://ingest.test/v2.1/track"), handler.Requests[1].Message.RequestUri);
        using var resent = JsonDocument.Parse(handler.Requests[1].Body);
        Assert.AreEqual(1, resent.RootElement.GetArrayLength());
        Assert.AreEqual("second", resent.RootElement[0].GetProperty("data").GetProperty("baseData").GetProperty("message").GetString());
    }

    [TestMethod]
    public void OtlpResolve_AppendsSignalPaths()
    {
        Assert.AreEqual("http://c.test:4318/v1/traces", BelemetryOtlpExporter.Resolve(null, new Uri("http://c.test:4318"), "v1/traces"));
        Assert.AreEqual("http://c.test/otlp/v1/logs", BelemetryOtlpExporter.Resolve(null, new Uri("http://c.test/otlp"), "v1/logs"));
        Assert.AreEqual("http://c.test/otlp/v1/logs", BelemetryOtlpExporter.Resolve(null, new Uri("http://c.test/otlp/"), "v1/logs"));
        Assert.AreEqual("http://other.test/custom", BelemetryOtlpExporter.Resolve(new Uri("http://other.test/custom"), new Uri("http://c.test"), "v1/logs"));
        Assert.AreEqual("http://localhost:4318/v1/metrics", BelemetryOtlpExporter.Resolve(null, null, "v1/metrics"));
    }

    [TestMethod]
    public void OtlpExporter_CreatesOneRequestPerSignal()
    {
        var exporter = Otlp(o =>
        {
            o.Protocol = BelemetryOtlpProtocol.HttpJson;
            o.Signals = BelemetrySignals.Traces | BelemetrySignals.Metrics;
            o.MetricsEndpoint = new Uri("http://metrics.test/ingest");
        });
        var batch = new BelemetryBatch(Records.Resource(), [Records.Span()], [Records.Log()], Tests.Otlp.OtlpProtobufConformanceTests.MetricsSample());

        var requests = exporter.CreateRequests(batch).ToList();
        CollectionAssert.AreEqual(new[] { "http://collector.test:4318/v1/traces", "http://metrics.test/ingest" }, requests.Select(r => r.Url).ToArray());
        Assert.IsTrue(requests.All(r => r.ContentType == "application/json"));
        using var doc = JsonDocument.Parse(requests[0].Body);
        Assert.IsTrue(doc.RootElement.TryGetProperty("resourceSpans", out _));
        Assert.IsNull(exporter.GetRetryForPartialSuccess(requests[0], 200, """{"partialSuccess":{"rejectedSpans":"1"}}"""));
    }

    private sealed class ThrowingHandler(Func<bool> shouldThrow) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (shouldThrow()) throw new HttpRequestException("offline");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
