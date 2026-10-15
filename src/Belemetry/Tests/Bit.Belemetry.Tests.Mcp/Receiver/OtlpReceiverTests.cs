using System.Net;
using System.Text;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Bit.Belemetry.Demo.Client.Services;
using Bit.Belemetry.Tests.Mcp.TestInfra;

namespace Bit.Belemetry.Tests.Mcp.Receiver;

/// <summary>
/// The in-process OTLP/JSON receiver behind the Live telemetry page: what it accepts, how it files records per
/// session, and that one reader never sees another's records.
/// </summary>
[TestClass]
public class OtlpReceiverTests
{
    private static BelemetryMcpServerFixture _server = null!;

    [ClassInitialize]
    public static async Task StartServerAsync(TestContext _) => _server = await BelemetryMcpServerFixture.StartAsync();

    [ClassCleanup]
    public static async Task StopServerAsync() => await _server.DisposeAsync();

    private static string NewSession() => Guid.NewGuid().ToString("N");

    private static string Logs(string sessionId, string eventName = "browser.page_view", string language = "webjs") => $$$"""
        {"resourceLogs":[{"resource":{"attributes":[
            {"key":"service.name","value":{"stringValue":"belemetry-demo"}},
            {"key":"telemetry.sdk.language","value":{"stringValue":"{{{language}}}"}}]},
          "scopeLogs":[{"scope":{"name":"bit.belemetry.page-views"},"logRecords":[
            {"timeUnixNano":"1760000000000000000","severityNumber":9,"severityText":"INFO","body":{"stringValue":"page view"},
             "eventName":"{{{eventName}}}","traceId":"0af7651916cd43dd8448eb211c80319c",
             "attributes":[{"key":"session.id","value":{"stringValue":"{{{sessionId}}}"}},
                           {"key":"url.path","value":{"stringValue":"/live"}},
                           {"key":"web_vital.value","value":{"doubleValue":1.5}}]}]}]}]}
        """;

    private static async Task<HttpResponseMessage> PostAsync(string signal, string json, string contentType = "application/json")
        => await _server.Http.PostAsync($"/otlp/v1/{signal}", new StringContent(json, Encoding.UTF8, contentType));

    private static async Task<TelemetryFeed> FeedAsync(string sessionId, bool shared = false)
        => (await _server.Http.GetFromJsonAsync<TelemetryFeed>($"/api/telemetry?sessionId={sessionId}&shared={(shared ? "true" : "false")}", new JsonSerializerOptions(JsonSerializerDefaults.Web)))!;

    [TestMethod]
    public async Task Logs_AreAccepted_AndReadBackUnderTheirSession()
    {
        var session = NewSession();

        var response = await PostAsync("logs", Logs(session));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("{}", await response.Content.ReadAsStringAsync());

        var feed = await FeedAsync(session);
        var record = feed.Records.Single();

        Assert.AreEqual("log", record.Signal);
        Assert.AreEqual("Event", record.Kind);
        Assert.AreEqual("browser", record.Source);
        Assert.AreEqual("browser.page_view /live", record.Name);
        Assert.AreEqual(session, record.SessionId);
        Assert.AreEqual("0af7651916cd43dd8448eb211c80319c", record.TraceId);
        Assert.AreEqual(1760000000000, record.Time, 1);
        Assert.AreEqual("1.5", record.Attributes["web_vital.value"]);
    }

    [TestMethod]
    public async Task ASession_NeverSeesAnotherSessionsRecords()
    {
        var mine = NewSession();
        var theirs = NewSession();

        await PostAsync("logs", Logs(mine));
        await PostAsync("logs", Logs(theirs));
        await PostAsync("logs", Logs(theirs));

        Assert.AreEqual(1, (await FeedAsync(mine)).Records.Length);
        Assert.AreEqual(2, (await FeedAsync(theirs)).Records.Length);
    }

    [TestMethod]
    public async Task Polling_WithAfter_ReturnsOnlyWhatIsNew()
    {
        var session = NewSession();

        await PostAsync("logs", Logs(session));
        var first = await FeedAsync(session);

        await PostAsync("logs", Logs(session, "demo.clicked"));
        var next = (await _server.Http.GetFromJsonAsync<TelemetryFeed>($"/api/telemetry?sessionId={session}&after={first.LastId}", new JsonSerializerOptions(JsonSerializerDefaults.Web)))!;

        Assert.AreEqual("demo.clicked", next.Records.Single().Name);
    }

    [TestMethod]
    public async Task Spans_AreDecoded_WithKindDurationAndStatus()
    {
        var session = NewSession();

        var json = $$$"""
            {"resourceSpans":[{"resource":{"attributes":[{"key":"telemetry.sdk.language","value":{"stringValue":"webjs"}}]},
              "scopeSpans":[{"scope":{"name":"bit.belemetry.http"},"spans":[
                {"traceId":"0af7651916cd43dd8448eb211c80319c","spanId":"b7ad6b7169203331","name":"GET","kind":3,
                 "startTimeUnixNano":"1760000000000000000","endTimeUnixNano":"1760000000250000000",
                 "status":{"code":2,"message":"boom"},
                 "attributes":[{"key":"session.id","value":{"stringValue":"{{{session}}}"}},
                               {"key":"http.response.status_code","value":{"intValue":"500"}}]}]}]}]}
            """;

        Assert.AreEqual(HttpStatusCode.OK, (await PostAsync("traces", json)).StatusCode);

        var span = (await FeedAsync(session)).Records.Single();

        Assert.AreEqual("span", span.Signal);
        Assert.AreEqual("Client", span.SpanKind);
        Assert.AreEqual(250, span.DurationMs!.Value, 0.01);
        Assert.AreEqual("500", span.Attributes["http.response.status_code"]);
        Assert.IsNotNull(span.Status);
    }

    [TestMethod]
    public async Task Metrics_WithNoSession_GoToTheSharedBucket()
    {
        var session = NewSession();

        const string Json = """
            {"resourceMetrics":[{"resource":{"attributes":[{"key":"service.name","value":{"stringValue":"belemetry-demo"}}]},
              "scopeMetrics":[{"scope":{"name":"Bit.Belemetry.Demo"},"metrics":[
                {"name":"demo.button.presses.test","unit":"{press}","sum":{"aggregationTemporality":2,"isMonotonic":true,
                 "dataPoints":[{"asInt":"3","timeUnixNano":"1760000000000000000"}]}}]}]}]}
            """;

        Assert.AreEqual(HttpStatusCode.OK, (await PostAsync("metrics", Json)).StatusCode);

        Assert.IsFalse((await FeedAsync(session)).Records.Any(r => r.Name == "demo.button.presses.test"));

        var metric = (await FeedAsync(session, shared: true)).Records.Last(r => r.Name == "demo.button.presses.test");

        Assert.AreEqual("Metric", metric.Kind);
        Assert.AreEqual("server", metric.Source);
        Assert.IsNull(metric.SessionId);
    }

    [TestMethod]
    public async Task SharedRecords_ThatShareATraceWithASession_AreHandedToIt_MarkedCorrelated()
    {
        var session = NewSession();
        var trace = Guid.NewGuid().ToString("N");

        await PostAsync("logs", Logs(session).Replace("0af7651916cd43dd8448eb211c80319c", trace, StringComparison.Ordinal));

        var serverSpan = $$$"""
            {"resourceSpans":[{"resource":{"attributes":[{"key":"telemetry.sdk.language","value":{"stringValue":"dotnet"}}]},
              "scopeSpans":[{"scope":{"name":"Microsoft.AspNetCore"},"spans":[
                {"traceId":"{{{trace}}}","spanId":"b7ad6b7169203331","name":"GET /api/demo/echo","kind":2,
                 "startTimeUnixNano":"1760000000000000000","endTimeUnixNano":"1760000000001000000"}]}]}]}
            """;

        await PostAsync("traces", serverSpan);

        var correlated = (await FeedAsync(session)).Records.Single(r => r.Signal == "span");

        Assert.IsTrue(correlated.Correlated);
        Assert.AreEqual("server", correlated.Source);
    }

    [TestMethod]
    public async Task Gzip_IsAccepted()
    {
        var session = NewSession();

        using var buffer = new MemoryStream();
        await using (var gzip = new System.IO.Compression.GZipStream(buffer, System.IO.Compression.CompressionLevel.Fastest, leaveOpen: true))
        {
            await gzip.WriteAsync(Encoding.UTF8.GetBytes(Logs(session)));
        }

        var content = new ByteArrayContent(buffer.ToArray());
        content.Headers.ContentType = new("application/json");
        content.Headers.ContentEncoding.Add("gzip");

        Assert.AreEqual(HttpStatusCode.OK, (await _server.Http.PostAsync("/otlp/v1/logs", content)).StatusCode);
        Assert.AreEqual(1, (await FeedAsync(session)).Records.Length);
    }

    [TestMethod]
    public async Task WhatIsNotOtlpJson_IsRefused()
    {
        Assert.AreEqual(HttpStatusCode.UnsupportedMediaType, (await PostAsync("logs", "x", "application/x-protobuf")).StatusCode);
        Assert.AreEqual(HttpStatusCode.UnsupportedMediaType, (await PostAsync("logs", "x", "text/plain")).StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, (await PostAsync("logs", "{ not json")).StatusCode);
    }

    [TestMethod]
    public async Task TheFeed_RequiresAWellFormedSessionId()
    {
        Assert.AreEqual(HttpStatusCode.BadRequest, (await _server.Http.GetAsync("/api/telemetry")).StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, (await _server.Http.GetAsync("/api/telemetry?sessionId=../../etc")).StatusCode);
    }

    [TestMethod]
    public void TheStore_IsBoundedPerSession()
    {
        var store = new TelemetryStore(TimeProvider.System);
        var session = NewSession();

        store.Add(Enumerable.Range(0, TelemetryStore.PerSessionLimit + 50).Select(i => new TelemetryRecord
        {
            Signal = "log",
            Kind = "Log",
            Source = "browser",
            Name = $"record {i}",
            SessionId = session,
        }));

        var records = store.Get(session, 0, includeShared: false).Records;

        Assert.AreEqual(TelemetryStore.PerSessionLimit, records.Length);
        Assert.AreEqual($"record {TelemetryStore.PerSessionLimit + 49}", records[^1].Name, "The oldest records are the ones dropped.");
    }

    [TestMethod]
    public async Task TheServerPipeline_ExportsToItsOwnReceiver()
    {
        // Program.cs turns on Server.CollectDotnetTelemetry and points the OTLP exporter at this process; the fixture
        // routes that traffic into the in-memory host. The demo echo endpoint logs under the demo's category.
        await _server.Http.GetAsync("/api/demo/echo");

        var store = _server.Services.GetRequiredService<TelemetryStore>();
        var deadline = DateTime.UtcNow.AddSeconds(20);

        while (DateTime.UtcNow < deadline)
        {
            var shared = store.Get(NewSession(), 0, includeShared: true).Records;

            if (shared.Any(r => r.Source == "server" && r.Name.Contains("/api/demo/echo", StringComparison.Ordinal))) return;

            await Task.Delay(250);
        }

        Assert.Fail("The server's request span for /api/demo/echo never reached the receiver.");
    }
}
