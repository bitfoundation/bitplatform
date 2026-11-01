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
        // Azure Monitor's list.
        foreach (var code in new[] { 408, 429, 439, 500, 502, 503, 504 }) Assert.IsTrue(BelemetryHttpSender.IsRetryable((HttpStatusCode)code), code.ToString());
        foreach (var code in new[] { 200, 206, 400, 401, 403, 404, 413, 501 }) Assert.IsFalse(BelemetryHttpSender.IsRetryable((HttpStatusCode)code), code.ToString());

        // The OTLP specification's: "All other 4xx or 5xx response status codes MUST NOT be retried".
        foreach (var code in new[] { 429, 502, 503, 504 }) Assert.IsTrue(BelemetryHttpSender.IsOtlpRetryable(code), code.ToString());
        foreach (var code in new[] { 400, 408, 413, 439, 500, 501 }) Assert.IsFalse(BelemetryHttpSender.IsOtlpRetryable(code), code.ToString());
    }

    [TestMethod]
    public async Task Otlp_DropsA500WithoutRetrying_AsTheSpecificationRequires()
    {
        var handler = new FakeHttpMessageHandler { Fallback = HttpStatusCode.InternalServerError };
        var warnings = new List<string>();
        using var sender = new BelemetryHttpSender(handler, (_, m) => _diagnostics.Add(m), (_, m) => warnings.Add(m)) { Backoff = [TimeSpan.Zero, TimeSpan.Zero] };
        var exporter = Otlp();

        Assert.AreEqual(BelemetrySendOutcome.Rejected, await sender.SendAsync(exporter, LogsRequest(exporter), retry: true, CancellationToken.None));
        Assert.HasCount(1, handler.Requests);
        StringAssert.Contains(warnings.Single(), "rejected the request with 500", "Data loss the app's owner can act on is a warning.");
    }

    [TestMethod]
    public async Task AzurePartialSuccess_OnTheLastAttempt_LeavesOnlyTheRejectedItemsToStore()
    {
        // A stored request is replayed with a single attempt. Storing the original after a 206 would resend the items
        // the backend already accepted, at every retry pass.
        var exporter = new BelemetryAzureMonitorExporter(new BelemetryAzureMonitorExporterOptions { ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000001" }, "azuremonitor");
        var request = exporter.CreateRequests(new BelemetryBatch(Records.Resource(), logs: [Records.Log("a"), Records.Log("b"), Records.Log("c")])).Single();
        request.Time = 1_000;
        var handler = new FakeHttpMessageHandler()
            .Enqueue((HttpStatusCode)206, r => r.Content = new StringContent("""{"itemsReceived":3,"itemsAccepted":2,"errors":[{"index":1,"statusCode":503}]}"""))
            .Enqueue(HttpStatusCode.ServiceUnavailable);
        using var sender = CreateSender(handler);

        var (outcome, remaining, delivered, rejected) = await sender.SendWithResultAsync(exporter, request, retry: false, CancellationToken.None);

        Assert.AreEqual(BelemetrySendOutcome.Failed, outcome);
        Assert.HasCount(2, handler.Requests, "The narrowed request is tried even on what was the last attempt.");
        Assert.AreNotSame(request, remaining);
        Assert.AreEqual(1, remaining.Count);
        Assert.AreEqual(1, JsonDocument.Parse(remaining.Body).RootElement.GetArrayLength());
        Assert.AreEqual(1_000, remaining.Time, "A stored retry keeps the original's age.");
        Assert.AreEqual(2, delivered);
        Assert.AreEqual(0, rejected);
    }

    [TestMethod]
    public async Task AzurePartialSuccess_CountsTheItemsRefusedForGood_AsRejected()
    {
        // 5 items: 2 accepted, 2 refused for good (400), 1 to retry (503) - which the retry delivers.
        var exporter = new BelemetryAzureMonitorExporter(new BelemetryAzureMonitorExporterOptions { ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000001" }, "azuremonitor");
        var request = exporter.CreateRequests(new BelemetryBatch(Records.Resource(), logs: [.. Enumerable.Range(0, 5).Select(i => Records.Log("m" + i))])).Single();
        var warnings = new List<string>();
        var handler = new FakeHttpMessageHandler()
            .Enqueue((HttpStatusCode)206, r => r.Content = new StringContent("""{"itemsReceived":5,"itemsAccepted":2,"errors":[{"index":0,"statusCode":400},{"index":3,"statusCode":400},{"index":4,"statusCode":503}]}"""))
            .Enqueue(HttpStatusCode.OK);
        using var sender = new BelemetryHttpSender(handler, (_, _) => { }, (_, message) => warnings.Add(message)) { Backoff = [TimeSpan.Zero] };

        var result = await sender.SendWithResultAsync(exporter, request, retry: false, CancellationToken.None);

        Assert.AreEqual(BelemetrySendOutcome.Delivered, result.Outcome);
        Assert.AreEqual(3, result.Delivered);
        Assert.AreEqual(2, result.Rejected);
        StringAssert.Contains(warnings.Single(), "rejected 2 of its items for good");

        // Nothing to retry: the request is "delivered", but the refused items are not.
        handler.Enqueue((HttpStatusCode)206, r => r.Content = new StringContent("""{"itemsReceived":5,"itemsAccepted":4,"errors":[{"index":1,"statusCode":400}]}"""));
        result = await sender.SendWithResultAsync(exporter, request, retry: false, CancellationToken.None);
        Assert.AreEqual((BelemetrySendOutcome.Delivered, 4, 1), (result.Outcome, result.Delivered, result.Rejected));
    }

    [TestMethod]
    public async Task OtlpPartialSuccess_CountsTheRejectedRecords_AsRejected()
    {
        var exporter = Otlp(o => o.Protocol = BelemetryOtlpProtocol.HttpJson);
        var request = exporter.CreateRequests(new BelemetryBatch(Records.Resource(), logs: [Records.Log("a"), Records.Log("b"), Records.Log("c")])).Single();
        var handler = new FakeHttpMessageHandler()
            .Enqueue(HttpStatusCode.OK, r => r.Content = new StringContent("""{"partialSuccess":{"rejectedLogRecords":"2","errorMessage":"too old"}}""", Encoding.UTF8, "application/json"));
        using var sender = CreateSender(handler);

        var result = await sender.SendWithResultAsync(exporter, request, retry: false, CancellationToken.None);

        Assert.AreEqual((BelemetrySendOutcome.Delivered, 1, 2), (result.Outcome, result.Delivered, result.Rejected));
    }

    [TestMethod]
    public async Task AzurePartialSuccess_ReadsABodyWithAByteOrderMark_OrInTheCharsetItNames()
    {
        var exporter = new BelemetryAzureMonitorExporter(new BelemetryAzureMonitorExporterOptions { ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000001" }, "azuremonitor");
        var request = exporter.CreateRequests(new BelemetryBatch(Records.Resource(), logs: [Records.Log("first"), Records.Log("second")])).Single();
        const string response = """{"itemsReceived":2,"itemsAccepted":1,"errors":[{"index":1,"statusCode":503}]}""";
        var handler = new FakeHttpMessageHandler()
            .Enqueue((HttpStatusCode)206, r => r.Content = new ByteArrayContent([.. Encoding.UTF8.Preamble, .. Encoding.UTF8.GetBytes(response)]))
            .Enqueue(HttpStatusCode.OK)
            .Enqueue((HttpStatusCode)206, r =>
            {
                r.Content = new ByteArrayContent(Encoding.Unicode.GetBytes(response));
                r.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json; charset=utf-16");
            })
            .Enqueue(HttpStatusCode.OK);
        using var sender = CreateSender(handler);

        // Unreadable, either would pass for a 206 with nothing to retry: the rejected item would be lost.
        Assert.AreEqual(BelemetrySendOutcome.Delivered, await sender.SendAsync(exporter, request, retry: false, CancellationToken.None));
        Assert.HasCount(2, handler.Requests);
        Assert.AreEqual(BelemetrySendOutcome.Delivered, await sender.SendAsync(exporter, request, retry: false, CancellationToken.None));
        Assert.HasCount(4, handler.Requests);
        Assert.AreEqual(1, JsonDocument.Parse(handler.Requests[3].Body).RootElement.GetArrayLength());
    }

    [TestMethod]
    public async Task AResponseBodyOverTheLimit_IsNotRead_PastTheLimit()
    {
        // A chunked body with no Content-Length: HttpClient must not buffer it whole before the sender looks at it.
        var content = new EndlessContent();
        var handler = new FakeHttpMessageHandler().Enqueue(HttpStatusCode.OK, r => r.Content = content);
        using var sender = CreateSender(handler);
        var exporter = Otlp(o => o.Protocol = BelemetryOtlpProtocol.HttpJson);

        Assert.AreEqual(BelemetrySendOutcome.Delivered, await sender.SendAsync(exporter, LogsRequest(exporter), retry: false, CancellationToken.None));
        Assert.IsLessThanOrEqualTo(BelemetryHttpSender.MaxResponseBytes + 64 * 1024L, content.Served, $"{content.Served} bytes read.");
    }

    /// <summary>
    /// A 64 MiB response body without a Content-Length, counting what was read of it - by the sender, or by HttpClient
    /// buffering it (which goes through SerializeToStreamAsync).
    /// </summary>
    private sealed class EndlessContent : HttpContent
    {
        public long Served => _stream.Served;

        private readonly EndlessStream _stream = new();

        protected override Task<Stream> CreateContentReadStreamAsync() => Task.FromResult<Stream>(_stream);

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => _stream.CopyToAsync(stream);

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }

        private sealed class EndlessStream : Stream
        {
            private const long Size = 64 * 1024 * 1024;

            public long Served { get; private set; }

            public override int Read(byte[] buffer, int offset, int count)
            {
                count = (int)Math.Min(count, Size - Served);
                Array.Fill(buffer, (byte)'x', offset, count);
                Served += count;
                return count;
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => Served; set => throw new NotSupportedException(); }
            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }

    [TestMethod]
    public async Task AzurePartialSuccess_ThatKeepsRejectingTheSameItems_Stops()
    {
        var exporter = new BelemetryAzureMonitorExporter(new BelemetryAzureMonitorExporterOptions { ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000001" }, "azuremonitor");
        var request = exporter.CreateRequests(new BelemetryBatch(Records.Resource(), logs: [Records.Log("only")])).Single();
        var handler = new FakeHttpMessageHandler();
        for (var i = 0; i < 10; i++) handler.Enqueue((HttpStatusCode)206, r => r.Content = new StringContent("""{"itemsReceived":1,"itemsAccepted":0,"errors":[{"index":0,"statusCode":500}]}"""));
        using var sender = CreateSender(handler);

        var outcome = await sender.SendAsync(exporter, request, retry: false, CancellationToken.None);

        Assert.AreEqual(BelemetrySendOutcome.Failed, outcome);
        Assert.HasCount(1, handler.Requests, "A request that does not shrink gets no extra attempt.");
    }

    [TestMethod]
    public async Task AzurePartialSuccess_ThatShrinksByOneItemAtATime_GetsOneExtraAttemptAtMost()
    {
        var exporter = new BelemetryAzureMonitorExporter(new BelemetryAzureMonitorExporterOptions { ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000001" }, "azuremonitor");
        var request = exporter.CreateRequests(new BelemetryBatch(Records.Resource(), logs: [.. Enumerable.Range(0, 10).Select(i => Records.Log("m" + i))])).Single();
        var handler = new FakeHttpMessageHandler();
        for (var items = 10; items > 1; items--)
        {
            var errors = string.Join(",", Enumerable.Range(1, items - 1).Select(i => $$"""{"index":{{i}},"statusCode":500}"""));
            var body = $$"""{"itemsReceived":{{items}},"itemsAccepted":1,"errors":[{{errors}}]}""";
            handler.Enqueue((HttpStatusCode)206, r => r.Content = new StringContent(body));
        }
        using var sender = CreateSender(handler);

        var result = await sender.SendWithResultAsync(exporter, request, retry: false, CancellationToken.None);

        Assert.AreEqual(BelemetrySendOutcome.Failed, result.Outcome);
        Assert.HasCount(2, handler.Requests, "A backend that accepts one item per answer cannot keep the delivery going.");
        Assert.AreEqual(8, result.Remaining.Count);
    }

    [TestMethod]
    public async Task SendsAUserAgent_UnlessTheExporterSetsOne()
    {
        var handler = new FakeHttpMessageHandler();
        using var sender = CreateSender(handler);

        var exporter = Otlp();
        await sender.SendAsync(exporter, LogsRequest(exporter), retry: false, CancellationToken.None);
        Assert.AreEqual("Bit.Belemetry/" + BelemetrySettings.SdkVersion, string.Join(" ", handler.Requests[0].Message.Headers.GetValues("User-Agent")));

        var custom = Otlp(o => o.Headers["User-Agent"] = "MyApp/1.0");
        await sender.SendAsync(custom, LogsRequest(custom), retry: false, CancellationToken.None);
        Assert.AreEqual("MyApp/1.0", string.Join(" ", handler.Requests[1].Message.Headers.GetValues("User-Agent")));
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
        const BelemetryOtlpProtocol http = BelemetryOtlpProtocol.HttpProtobuf;
        Assert.AreEqual("http://c.test:4318/v1/traces", BelemetryOtlpExporter.Resolve(null, new Uri("http://c.test:4318"), http, BelemetrySignals.Traces));
        Assert.AreEqual("http://c.test/otlp/v1/logs", BelemetryOtlpExporter.Resolve(null, new Uri("http://c.test/otlp"), http, BelemetrySignals.Logs));
        Assert.AreEqual("http://c.test/otlp/v1/logs", BelemetryOtlpExporter.Resolve(null, new Uri("http://c.test/otlp/"), BelemetryOtlpProtocol.HttpJson, BelemetrySignals.Logs));
        Assert.AreEqual("http://other.test/custom", BelemetryOtlpExporter.Resolve(new Uri("http://other.test/custom"), new Uri("http://c.test"), http, BelemetrySignals.Logs));
        Assert.AreEqual("http://localhost:4318/v1/metrics", BelemetryOtlpExporter.Resolve(null, null, http, BelemetrySignals.Metrics));
    }

    [TestMethod]
    public void OtlpResolve_Grpc_AppendsTheServiceMethods()
    {
        const BelemetryOtlpProtocol grpc = BelemetryOtlpProtocol.Grpc;
        Assert.AreEqual("http://c.test:4317/opentelemetry.proto.collector.trace.v1.TraceService/Export", BelemetryOtlpExporter.Resolve(null, new Uri("http://c.test:4317"), grpc, BelemetrySignals.Traces));
        Assert.AreEqual("https://c.test/opentelemetry.proto.collector.logs.v1.LogsService/Export", BelemetryOtlpExporter.Resolve(null, new Uri("https://c.test/"), grpc, BelemetrySignals.Logs));
        Assert.AreEqual("http://localhost:4317/opentelemetry.proto.collector.metrics.v1.MetricsService/Export", BelemetryOtlpExporter.Resolve(null, null, grpc, BelemetrySignals.Metrics));
        // OTEL_EXPORTER_OTLP_TRACES_ENDPOINT is a host for gRPC, not a full URL: the method is still appended.
        Assert.AreEqual("http://traces.test:4317/opentelemetry.proto.collector.trace.v1.TraceService/Export", BelemetryOtlpExporter.Resolve(new Uri("http://traces.test:4317"), new Uri("http://c.test"), grpc, BelemetrySignals.Traces));
    }

    [TestMethod]
    public async Task Grpc_FramesTheMessage_SendsHttp2_AndReadsTheStatusTrailer()
    {
        var handler = new FakeHttpMessageHandler().Enqueue(HttpStatusCode.OK, r => r.TrailingHeaders.Add("grpc-status", "0"));
        using var sender = CreateSender(handler);
        var exporter = Otlp(o =>
        {
            o.Protocol = BelemetryOtlpProtocol.Grpc;
            o.Headers["x-api-key"] = "secret";
        });
        var request = LogsRequest(exporter);
        Assert.AreEqual("application/grpc", request.ContentType);
        Assert.AreEqual("http://collector.test:4318/opentelemetry.proto.collector.logs.v1.LogsService/Export", request.Url);

        var outcome = await sender.SendAsync(exporter, request, retry: true, CancellationToken.None);

        Assert.AreEqual(BelemetrySendOutcome.Delivered, outcome);
        var sent = handler.Requests.Single();
        Assert.AreEqual(HttpVersion.Version20, sent.Message.Version);
        Assert.AreEqual(HttpVersionPolicy.RequestVersionExact, sent.Message.VersionPolicy);
        Assert.AreEqual("trailers", sent.Message.Headers.GetValues("te").Single());
        Assert.AreEqual("secret", sent.Message.Headers.GetValues("x-api-key").Single());
        Assert.AreEqual("application/grpc", sent.Message.Content!.Headers.ContentType!.MediaType);
        Assert.IsFalse(sent.Message.Headers.Contains("grpc-encoding"));

        // Length-prefixed message: compressed flag, big-endian length, then the very protobuf the HTTP exporter sends.
        Assert.AreEqual(0, sent.Body[0]);
        Assert.AreEqual(request.Body.Length, (sent.Body[1] << 24) | (sent.Body[2] << 16) | (sent.Body[3] << 8) | sent.Body[4]);
        CollectionAssert.AreEqual(request.Body, sent.Body[5..]);
    }

    [TestMethod]
    public async Task Grpc_Gzip_CompressesTheMessageAndFlagsIt()
    {
        var handler = new FakeHttpMessageHandler().Enqueue(HttpStatusCode.OK, r => r.TrailingHeaders.Add("grpc-status", "0"));
        using var sender = CreateSender(handler);
        var exporter = Otlp(o =>
        {
            o.Protocol = BelemetryOtlpProtocol.Grpc;
            o.Compression = BelemetryCompression.Gzip;
        });
        var request = LogsRequest(exporter);

        await sender.SendAsync(exporter, request, retry: false, CancellationToken.None);

        var sent = handler.Requests.Single();
        Assert.AreEqual("gzip", sent.Message.Headers.GetValues("grpc-encoding").Single());
        Assert.IsEmpty(sent.Message.Content!.Headers.ContentEncoding, "gRPC compresses the message, not the HTTP body.");
        Assert.AreEqual(1, sent.Body[0]);
        using var input = new GZipStream(new MemoryStream(sent.Body[5..]), CompressionMode.Decompress);
        using var output = new MemoryStream();
        input.CopyTo(output);
        CollectionAssert.AreEqual(request.Body, output.ToArray());
    }

    [TestMethod]
    [DataRow(14, true)] // UNAVAILABLE
    [DataRow(8, true)] // RESOURCE_EXHAUSTED
    [DataRow(4, true)] // DEADLINE_EXCEEDED
    [DataRow(3, false)] // INVALID_ARGUMENT
    [DataRow(16, false)] // UNAUTHENTICATED
    public async Task Grpc_StatusDecidesBetweenRetryAndDrop(int grpcStatus, bool retryable)
    {
        var handler = new FakeHttpMessageHandler()
            .Enqueue(HttpStatusCode.OK, r =>
            {
                r.TrailingHeaders.Add("grpc-status", grpcStatus.ToString());
                r.TrailingHeaders.Add("grpc-message", "nope%20really");
            })
            .Enqueue(HttpStatusCode.OK, r => r.TrailingHeaders.Add("grpc-status", "0"));
        using var sender = CreateSender(handler);
        var exporter = Otlp(o => o.Protocol = BelemetryOtlpProtocol.Grpc);

        var outcome = await sender.SendAsync(exporter, LogsRequest(exporter), retry: true, CancellationToken.None);

        if (retryable)
        {
            Assert.AreEqual(BelemetrySendOutcome.Delivered, outcome);
            Assert.HasCount(2, handler.Requests);
        }
        else
        {
            Assert.AreEqual(BelemetrySendOutcome.Rejected, outcome);
            Assert.HasCount(1, handler.Requests);
            Assert.IsTrue(_diagnostics.Any(d => d.Contains("nope really")), "The grpc-message is decoded into the diagnostic.");
        }
    }

    [TestMethod]
    public async Task Grpc_TrailersOnlyResponse_ReadsTheStatusFromTheHeaders()
    {
        var handler = new FakeHttpMessageHandler().Enqueue(HttpStatusCode.OK, r => r.Headers.Add("grpc-status", "0"));
        using var sender = CreateSender(handler);
        var exporter = Otlp(o => o.Protocol = BelemetryOtlpProtocol.Grpc);

        Assert.AreEqual(BelemetrySendOutcome.Delivered, await sender.SendAsync(exporter, LogsRequest(exporter), retry: false, CancellationToken.None));
    }

    [TestMethod]
    public async Task Grpc_MissingStatus_IsRetriedThenFails()
    {
        var handler = new FakeHttpMessageHandler { Fallback = HttpStatusCode.OK };
        using var sender = CreateSender(handler);
        var exporter = Otlp(o => o.Protocol = BelemetryOtlpProtocol.Grpc);

        Assert.AreEqual(BelemetrySendOutcome.Failed, await sender.SendAsync(exporter, LogsRequest(exporter), retry: true, CancellationToken.None));
        Assert.HasCount(3, handler.Requests);
    }

    [TestMethod]
    public async Task Grpc_HttpErrors_FollowTheHttpRules()
    {
        var handler = new FakeHttpMessageHandler().Enqueue(HttpStatusCode.NotFound);
        using var sender = CreateSender(handler);
        var exporter = Otlp(o => o.Protocol = BelemetryOtlpProtocol.Grpc);

        Assert.AreEqual(BelemetrySendOutcome.Rejected, await sender.SendAsync(exporter, LogsRequest(exporter), retry: true, CancellationToken.None));
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
        Assert.IsNull(exporter.GetRetryForPartialSuccess(requests[0], 200, """{"partialSuccess":{"rejectedSpans":"1"}}""", out _));
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
