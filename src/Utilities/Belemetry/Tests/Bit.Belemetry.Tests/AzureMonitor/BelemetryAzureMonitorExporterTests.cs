using System.Text;

namespace Bit.Belemetry.Tests.AzureMonitor;

[TestClass]
public class BelemetryAzureMonitorExporterTests
{
    private const string ConnectionString = "InstrumentationKey=11111111-2222-3333-4444-555555555555;IngestionEndpoint=https://ingest.test/";

    private static BelemetryAzureMonitorExporter Create(Action<BelemetryAzureMonitorExporterOptions>? configure = null)
    {
        var options = new BelemetryAzureMonitorExporterOptions { ConnectionString = ConnectionString };
        configure?.Invoke(options);
        return new BelemetryAzureMonitorExporter(options, "azuremonitor");
    }

    private static BelemetryHttpRequest Request(params string[] items) => new()
    {
        Exporter = "azuremonitor",
        Url = "https://ingest.test/v2.1/track",
        ContentType = "application/json",
        ContentEncoding = "gzip",
        Headers = new() { ["x"] = "y" },
        Body = Encoding.UTF8.GetBytes("[" + string.Join(",", items.Select(i => $"{{\"item\":\"{i}\"}}")) + "]"),
    };

    [TestMethod]
    public void PartialSuccess_RetriesOnlyRetryableItems()
    {
        var exporter = Create();
        var request = Request("a", "b", "c", "d");
        const string response = """
        { "itemsReceived": 4, "itemsAccepted": 1, "errors": [
            { "index": 0, "statusCode": 400, "message": "invalid" },
            { "index": 2, "statusCode": 503, "message": "busy" },
            { "index": 1, "statusCode": 439, "message": "quota" } ] }
        """;

        var retry = exporter.GetRetryForPartialSuccess(request, 206, response);

        Assert.IsNotNull(retry);
        using var body = JsonDocument.Parse(retry.Body);
        CollectionAssert.AreEqual(new[] { "b", "c" }, body.RootElement.EnumerateArray().Select(e => e.GetProperty("item").GetString()).ToArray());
        Assert.AreEqual(request.Url, retry.Url);
        Assert.AreEqual(request.Exporter, retry.Exporter);
        Assert.AreEqual("gzip", retry.ContentEncoding);
        Assert.AreEqual("y", retry.Headers["x"]);
        Assert.AreNotEqual(request.Id, retry.Id);
    }

    [TestMethod]
    public void PartialSuccess_NothingToRetry()
    {
        var exporter = Create();
        var request = Request("a", "b");

        Assert.IsNull(exporter.GetRetryForPartialSuccess(request, 200, """{"errors":[{"index":0,"statusCode":503}]}"""), "Only a 206 is partial.");
        Assert.IsNull(exporter.GetRetryForPartialSuccess(request, 206, """{"errors":[{"index":0,"statusCode":400}]}"""), "Nothing retryable.");
        Assert.IsNull(exporter.GetRetryForPartialSuccess(request, 206, """{"itemsAccepted":2}"""));
        Assert.IsNull(exporter.GetRetryForPartialSuccess(request, 206, "not json"));
        Assert.IsNull(exporter.GetRetryForPartialSuccess(request, 206, null));
    }

    [TestMethod]
    public void CreateRequests_TrackUrlSignalsAndCompression()
    {
        var exporter = Create(o => o.Compression = BelemetryCompression.Gzip);
        var batch = new BelemetryBatch(Records.Resource(), [Records.Span()], [Records.Log()]);

        var request = exporter.CreateRequests(batch).Single();
        Assert.AreEqual("https://ingest.test/v2.1/track", request.Url);
        Assert.AreEqual("application/json", request.ContentType);
        Assert.AreEqual("gzip", request.ContentEncoding);
        Assert.AreEqual("azuremonitor", request.Exporter);
        using (var doc = JsonDocument.Parse(request.Body)) Assert.AreEqual(2, doc.RootElement.GetArrayLength());

        var logsOnly = Create(o => o.Signals = BelemetrySignals.Logs).CreateRequests(batch).Single();
        using (var doc = JsonDocument.Parse(logsOnly.Body)) Assert.AreEqual("Microsoft.ApplicationInsights.Message", doc.RootElement[0].GetProperty("name").GetString());

        Assert.IsEmpty(Create(o => o.Signals = BelemetrySignals.Metrics).CreateRequests(batch));
        Assert.IsEmpty(exporter.CreateRequests(new BelemetryBatch(Records.Resource())));

        var idle = new BelemetryMetric { Name = "idle", Type = BelemetryMetricType.Sum, Points = { new BelemetryMetricPoint() } };
        Assert.IsEmpty(exporter.CreateRequests(new BelemetryBatch(Records.Resource(), metrics: [idle])), "No envelope, no request.");
    }

    [TestMethod]
    public async Task AccessTokenProvider_SetsBearer()
    {
        var exporter = Create(o => o.AccessTokenProvider = _ => ValueTask.FromResult<string?>("tok"));
        using var message = new HttpRequestMessage(HttpMethod.Post, "https://ingest.test/");
        await exporter.PrepareAsync(message, CancellationToken.None);
        Assert.AreEqual("Bearer", message.Headers.Authorization!.Scheme);
        Assert.AreEqual("tok", message.Headers.Authorization.Parameter);
    }

    [TestMethod]
    public void InvalidConnectionString_Throws() =>
        Assert.ThrowsExactly<FormatException>(() => new BelemetryAzureMonitorExporter(new BelemetryAzureMonitorExporterOptions { ConnectionString = "InstrumentationKey=x" }, "a"));
}
