using System.Net;
using System.Net.Http.Headers;

namespace Bit.Belemetry.Tests.Http;

[TestClass]
public class BelemetryFileRequestStoreTests
{
    private string _directory = default!;

    [TestInitialize]
    public void Initialize() => _directory = Path.Combine(Path.GetTempPath(), "bit-belemetry-tests", Guid.NewGuid().ToString("N"));

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }

    private BelemetryFileRequestStore CreateStore(long maxBytes = 1024 * 1024, TimeSpan? maxAge = null) =>
        new(_directory, maxBytes, maxAge ?? TimeSpan.FromHours(48));

    private static BelemetryHttpRequest Request(long? time = null, int bodySize = 16, string exporter = "otlp") => new()
    {
        Exporter = exporter,
        Url = "http://collector.test:4318/v1/logs",
        ContentType = "application/x-protobuf",
        Body = Enumerable.Range(0, bodySize).Select(i => (byte)i).ToArray(),
        Time = time ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
    };

    private string[] StoredFiles() => Directory.Exists(_directory) ? Directory.GetFiles(_directory) : [];

    [TestMethod]
    public async Task SaveAndLoad_RoundTripsEveryField_ButTheHeaders()
    {
        var store = CreateStore();
        var request = Request();
        request.ContentEncoding = "gzip";
        request.Headers = new() { ["x-api-key"] = "secret" };

        await store.SaveAsync(request, CancellationToken.None);

        var files = StoredFiles();
        Assert.HasCount(1, files);
        StringAssert.EndsWith(files[0], $"-{request.Id}.blmreq");
        Assert.IsFalse(files.Any(f => f.EndsWith(".tmp", StringComparison.Ordinal)), "The temp file is renamed into place.");
        var json = File.ReadAllText(files[0]);
        Assert.DoesNotContain("secret", json, "Headers (API keys) are never written to disk.");
        Assert.DoesNotContain("headers", json);
        Assert.AreEqual("secret", request.Headers["x-api-key"], "The caller's request is left untouched.");

        var loaded = (await store.LoadAsync(10, CancellationToken.None)).Single();
        Assert.AreEqual(request.Id, loaded.Id);
        Assert.AreEqual("otlp", loaded.Exporter);
        Assert.AreEqual(request.Url, loaded.Url);
        Assert.AreEqual(request.ContentType, loaded.ContentType);
        Assert.AreEqual("gzip", loaded.ContentEncoding);
        Assert.IsNull(loaded.Headers);
        CollectionAssert.AreEqual(request.Body, loaded.Body);
        Assert.AreEqual(request.Time, loaded.Time);
    }

    [TestMethod]
    public async Task Load_ReturnsTheOldestFirst_UpToMax()
    {
        var store = CreateStore();
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var newest = Request(now);
        var oldest = Request(now - 2000);
        var middle = Request(now - 1000);
        await store.SaveAsync(newest, CancellationToken.None);
        await store.SaveAsync(oldest, CancellationToken.None);
        await store.SaveAsync(middle, CancellationToken.None);

        var loaded = await store.LoadAsync(2, CancellationToken.None);

        CollectionAssert.AreEqual(new[] { oldest.Id, middle.Id }, loaded.Select(r => r.Id).ToArray());
        Assert.HasCount(3, StoredFiles(), "Loading does not remove anything.");
    }

    [TestMethod]
    public async Task Delete_RemovesOnlyThatRequest()
    {
        var store = CreateStore();
        var first = Request();
        var second = Request();
        await store.SaveAsync(first, CancellationToken.None);
        await store.SaveAsync(second, CancellationToken.None);

        await store.DeleteAsync(first.Id, CancellationToken.None);

        Assert.AreEqual(second.Id, (await store.LoadAsync(10, CancellationToken.None)).Single().Id);
    }

    [TestMethod]
    public async Task Clear_RemovesEverything()
    {
        var store = CreateStore();
        await store.SaveAsync(Request(), CancellationToken.None);
        await store.SaveAsync(Request(), CancellationToken.None);

        await store.ClearAsync();

        Assert.IsEmpty(StoredFiles());
        Assert.IsEmpty(await store.LoadAsync(10, CancellationToken.None));
    }

    [TestMethod]
    public async Task MissingDirectory_IsEmpty_AndNothingFails()
    {
        var store = CreateStore();

        Assert.IsEmpty(await store.LoadAsync(10, CancellationToken.None));
        await store.DeleteAsync("nope", CancellationToken.None);
        await store.ClearAsync();

        Assert.IsFalse(Directory.Exists(_directory), "Nothing creates the folder until something is saved.");
    }

    [TestMethod]
    public async Task OverTheSizeLimit_EvictsTheOldest()
    {
        // The limit never goes below 64 KB; each request is ~40 KB once its body is base64-encoded in JSON.
        var store = CreateStore(maxBytes: 1);
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var first = Request(now - 2000, bodySize: 30_000);
        var second = Request(now - 1000, bodySize: 30_000);
        var third = Request(now, bodySize: 30_000);

        await store.SaveAsync(first, CancellationToken.None);
        Assert.HasCount(1, StoredFiles(), "A single request under the 64 KB floor is kept.");

        await store.SaveAsync(second, CancellationToken.None);
        await store.SaveAsync(third, CancellationToken.None);

        Assert.AreEqual(third.Id, (await store.LoadAsync(10, CancellationToken.None)).Single().Id);
        Assert.IsLessThanOrEqualTo(64 * 1024L, StoredFiles().Sum(f => new FileInfo(f).Length));
    }

    [TestMethod]
    public async Task ExpiredRequests_AreDiscardedOnLoad()
    {
        var store = CreateStore(maxAge: TimeSpan.FromHours(1));
        var expired = Request(DateTimeOffset.UtcNow.AddHours(-3).ToUnixTimeMilliseconds());
        var fresh = Request();
        await store.SaveAsync(expired, CancellationToken.None);
        await store.SaveAsync(fresh, CancellationToken.None);

        var loaded = await store.LoadAsync(10, CancellationToken.None);

        Assert.AreEqual(fresh.Id, loaded.Single().Id);
        Assert.HasCount(1, StoredFiles(), "The expired file is deleted.");
    }

    [TestMethod]
    public async Task UnreadableFiles_AreDeleted()
    {
        var store = CreateStore();
        await store.SaveAsync(Request(), CancellationToken.None);
        var corrupt = Path.Combine(_directory, "000000000000001-corrupt.blmreq");
        await File.WriteAllTextAsync(corrupt, "{ not json");

        var loaded = await store.LoadAsync(10, CancellationToken.None);

        Assert.HasCount(1, loaded);
        Assert.IsFalse(File.Exists(corrupt));
    }

    [TestMethod]
    public async Task Load_IgnoresHeadersAnOlderVersionStored()
    {
        Directory.CreateDirectory(_directory);
        var request = Request();
        var json = JsonSerializer.Serialize(request, BelemetryJsonContext.Default.BelemetryHttpRequest)
            .Replace("\"body\"", "\"headers\":{\"x-api-key\":\"stale\"},\"body\"", StringComparison.Ordinal);
        StringAssert.Contains(json, "stale");
        File.WriteAllText(Path.Combine(_directory, $"{request.Time:D15}-{request.Id}.blmreq"), json);

        var loaded = (await CreateStore().LoadAsync(10, CancellationToken.None)).Single();

        Assert.IsNull(loaded.Headers);
    }

    [TestMethod]
    public void DefaultDirectory_SanitizesTheServiceName()
    {
        var directory = BelemetryFileRequestStore.DefaultDirectory("My:App/1");

        Assert.AreEqual("bit-belemetry", Path.GetFileName(Path.GetDirectoryName(directory)));
        var leaf = Path.GetFileName(directory);
        Assert.IsFalse(leaf.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0, leaf);
        StringAssert.StartsWith(leaf, "My");
    }

    [TestMethod]
    public async Task Pipeline_PersistsFailedExports_AndRetriesThemLater()
    {
        var handler = new FakeHttpMessageHandler();
        // Retry-After: 0 keeps the sender's in-process retries instant; all three attempts fail.
        for (var i = 0; i < 3; i++) handler.Enqueue(HttpStatusCode.ServiceUnavailable, r => r.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.Zero));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new BelemetryHttpMessageHandlerProvider(handler));
        services.AddBitBelemetryServices(o =>
        {
            TestHost.ApplyTestDefaults(o);
            o.Consent.Default = BelemetryFeatures.All;
            o.Persistence.Enabled = true;
            o.Persistence.Directory = _directory;
            o.AddOtlpExporter("http://collector.test:4318", e =>
            {
                e.Signals = BelemetrySignals.Logs;
                e.Headers["x-api-key"] = "key-1";
            });
        });
        await using var provider = services.BuildServiceProvider();
        var pipeline = provider.GetRequiredService<BelemetryPipeline>();

        pipeline.Enqueue(Records.Log());
        await pipeline.FlushAsync(false, CancellationToken.None);

        Assert.HasCount(3, handler.Requests);
        Assert.AreEqual("key-1", handler.Requests[0].Message.Headers.GetValues("x-api-key").Single());
        var stored = (await pipeline.Exporters.Store!.LoadAsync(10, CancellationToken.None)).Single();
        Assert.AreEqual("otlp", stored.Exporter);
        Assert.AreEqual("http://collector.test:4318/v1/logs", stored.Url);
        Assert.IsNull(stored.Headers);
        Assert.IsFalse(StoredFiles().Any(f => File.ReadAllText(f).Contains("key-1", StringComparison.Ordinal)), "The API key is not on disk.");

        // The key is rotated while the request waits: the replay uses the exporter's current one.
        pipeline.Settings.OtlpExporters[0].Headers["x-api-key"] = "key-2";

        // Back online: the stored request is sent once and leaves the store.
        await pipeline.Exporters.RetryStoredAsync(persistenceAllowed: true, CancellationToken.None);

        Assert.HasCount(4, handler.Requests);
        CollectionAssert.AreEqual(stored.Body, handler.Requests[3].Body);
        Assert.AreEqual("key-2", handler.Requests[3].Message.Headers.GetValues("x-api-key").Single());
        Assert.IsEmpty(StoredFiles());
    }

    [TestMethod]
    public async Task Pipeline_WithoutPersistenceConsent_ClearsTheStore()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBitBelemetryServices(o =>
        {
            TestHost.ApplyTestDefaults(o);
            o.Persistence.Enabled = true;
            o.Persistence.Directory = _directory;
            o.AddOtlpExporter("http://collector.test:4318");
        });
        await using var provider = services.BuildServiceProvider();
        var pipeline = provider.GetRequiredService<BelemetryPipeline>();
        await pipeline.Exporters.Store!.SaveAsync(Request(), CancellationToken.None);

        await pipeline.Exporters.RetryStoredAsync(persistenceAllowed: false, CancellationToken.None);

        Assert.IsEmpty(StoredFiles());
    }

    [TestMethod]
    public async Task Pipeline_ABackendThatIsDown_HoldsBackOnlyItsOwnBacklog()
    {
        var sent = new List<string>();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new BelemetryHttpMessageHandlerProvider(new ByHost(sent, down: "down.test")));
        services.AddBitBelemetryServices(o =>
        {
            TestHost.ApplyTestDefaults(o);
            o.Persistence.Enabled = true;
            o.Persistence.Directory = _directory;
            o.AddOtlpExporter("http://down.test:4318");
            o.AddOtlpExporter("http://up.test:4318");
        });
        await using var provider = services.BuildServiceProvider();
        var pipeline = provider.GetRequiredService<BelemetryPipeline>();
        var store = pipeline.Exporters.Store!;
        var time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 1000;
        foreach (var (exporter, host, age) in new[] { ("otlp", "down", 3), ("otlp1", "up", 2), ("otlp", "down", 1), ("otlp1", "up", 0) })
        {
            var request = Request(time - age, exporter: exporter);
            request.Url = $"http://{host}.test:4318/v1/logs";
            await store.SaveAsync(request, CancellationToken.None);
        }

        await pipeline.Exporters.RetryStoredAsync(persistenceAllowed: true, CancellationToken.None);

        // One attempt at the backend that is down, and the other backend's backlog all the same.
        CollectionAssert.AreEqual(new[] { "down.test", "up.test", "up.test" }, sent);
        var left = await store.LoadAsync(10, CancellationToken.None);
        Assert.IsTrue(left.Count == 2 && left.All(r => r.Url.StartsWith("http://down.test", StringComparison.Ordinal)), string.Join(" | ", left.Select(r => r.Url)));
    }

    [TestMethod]
    public async Task Pipeline_DropsAStoredRequest_ForAnAddressItsExporterNoLongerPostsTo()
    {
        var sent = new List<string>();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new BelemetryHttpMessageHandlerProvider(new ByHost(sent, down: "none.test")));
        services.AddBitBelemetryServices(o =>
        {
            TestHost.ApplyTestDefaults(o);
            o.Persistence.Enabled = true;
            o.Persistence.Directory = _directory;
            o.AddOtlpExporter("http://new.test:4318", e => e.Headers["x-api-key"] = "key-2");
        });
        await using var provider = services.BuildServiceProvider();
        var pipeline = provider.GetRequiredService<BelemetryPipeline>();
        var request = Request();
        request.Url = "http://old.test:4318/v1/logs";
        await pipeline.Exporters.Store!.SaveAsync(request, CancellationToken.None);

        await pipeline.Exporters.RetryStoredAsync(persistenceAllowed: true, CancellationToken.None);

        Assert.IsEmpty(sent, "The current key must not go to the address the exporter used before.");
        Assert.IsEmpty(StoredFiles());
    }

    [TestMethod]
    public async Task Pipeline_DropsAStoredRequest_ForAnOldPathOnTheSameHost()
    {
        var sent = new List<string>();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new BelemetryHttpMessageHandlerProvider(new ByHost(sent, down: "none.test")));
        services.AddBitBelemetryServices(o =>
        {
            TestHost.ApplyTestDefaults(o);
            o.Persistence.Enabled = true;
            o.Persistence.Directory = _directory;
            o.AddOtlpExporter("http://new.test:4318", e => e.Headers["x-api-key"] = "key-2");
        });
        await using var provider = services.BuildServiceProvider();
        var pipeline = provider.GetRequiredService<BelemetryPipeline>();
        var request = Request();
        request.Url = "http://new.test:4318/v1/old/logs";
        await pipeline.Exporters.Store!.SaveAsync(request, CancellationToken.None);

        await pipeline.Exporters.RetryStoredAsync(persistenceAllowed: true, CancellationToken.None);

        Assert.IsEmpty(sent, "The current key must not go to an old path on the same host.");
        Assert.IsEmpty(StoredFiles());
    }

    [TestMethod]
    public async Task Pipeline_ARevokeWhileARequestWaitsForItsRetry_NeitherRetriesNorStoresIt()
    {
        BelemetryConsentState consent = null!;
        var handler = new FakeHttpMessageHandler { Fallback = HttpStatusCode.ServiceUnavailable };
        handler.Enqueue(HttpStatusCode.ServiceUnavailable, r =>
        {
            r.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.Zero);
            consent.Decide(BelemetryFeatures.All & ~BelemetryFeatures.Interactions);
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new BelemetryHttpMessageHandlerProvider(handler));
        services.AddBitBelemetryServices(o =>
        {
            TestHost.ApplyTestDefaults(o);
            o.Consent.Default = BelemetryFeatures.All;
            o.Persistence.Enabled = true;
            o.Persistence.Directory = _directory;
            o.AddOtlpExporter("http://collector.test:4318", e => e.Signals = BelemetrySignals.Logs);
        });
        await using var provider = services.BuildServiceProvider();
        var pipeline = provider.GetRequiredService<BelemetryPipeline>();
        consent = provider.GetRequiredService<BelemetryConsentState>();

        pipeline.Enqueue(Records.Log());
        await pipeline.FlushAsync(false, CancellationToken.None);

        Assert.HasCount(1, handler.Requests, "A request from before a revoke may hold the revoked feature's records: it is not retried.");
        Assert.IsEmpty(StoredFiles(), "Nor is it stored after the revoke cleared the store.");
    }

    [TestMethod]
    public async Task Pipeline_ARevokeDuringAResendPass_StopsIt()
    {
        BelemetryConsentState consent = null!;
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, _ => consent.Decide(BelemetryFeatures.All & ~BelemetryFeatures.Interactions));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new BelemetryHttpMessageHandlerProvider(handler));
        services.AddBitBelemetryServices(o =>
        {
            TestHost.ApplyTestDefaults(o);
            o.Consent.Default = BelemetryFeatures.All;
            o.Persistence.Enabled = true;
            o.Persistence.Directory = _directory;
            o.AddOtlpExporter("http://collector.test:4318");
        });
        await using var provider = services.BuildServiceProvider();
        var pipeline = provider.GetRequiredService<BelemetryPipeline>();
        consent = provider.GetRequiredService<BelemetryConsentState>();
        var time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 1000;
        await pipeline.Exporters.Store!.SaveAsync(Request(time - 2), CancellationToken.None);
        await pipeline.Exporters.Store!.SaveAsync(Request(time - 1), CancellationToken.None);

        await pipeline.Exporters.RetryStoredAsync(persistenceAllowed: true, CancellationToken.None);

        Assert.HasCount(1, handler.Requests, "A revoke clears the stored requests: the pass does not send the rest of them.");
    }

    /// <summary>Answers 503 for one host and 200 for the others, recording the hosts it was sent to.</summary>
    private sealed class ByHost(List<string> sent, string down) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            sent.Add(request.RequestUri!.Host);
            return Task.FromResult(new HttpResponseMessage(request.RequestUri.Host == down ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK) { Content = new ByteArrayContent([]) });
        }
    }
}
