namespace Bit.Belemetry.Tests.Pipeline;

/// <summary>
/// The relay as a trust boundary: on Blazor Server the page at the other end of a circuit can be anything that speaks
/// the interop protocol, and the queue it relays into is shared by every user.
/// </summary>
[TestClass]
public class BelemetryRelayTests
{
    private static BelemetryJsBridge Bridge(TestHost host) =>
        new(host.Settings, host.Pipeline, host.Consent, host.Services.GetRequiredService<BelemetryBrowserBridgeRegistry>());

    private static JsonElement Batch(int logs, string? resourceJson = null, int feature = 2048)
    {
        var entries = string.Join(",", Enumerable.Range(0, logs).Select(i => $$"""{ "time": 1700000000000, "severityNumber": 9, "body": "e{{i}}", "eventName": "e", "feature": {{feature}} }"""));
        return JsonDocument.Parse($$"""{ "resource": {{resourceJson ?? "{}"}}, "logs": [ {{entries}} ] }""").RootElement.Clone();
    }

    private static void Server(BelemetryOptions o)
    {
        o.Host = BelemetryHostKind.Server;
        o.Browser.Transport = BelemetryBrowserTransport.Relay;
    }

    [TestMethod]
    public async Task IsIgnored_WhenThePageWasNotToldToRelay()
    {
        await using var host = TestHost.Create(o => o.Host = BelemetryHostKind.Server); // Direct, the server default
        var bridge = Bridge(host);

        await bridge.RelayAsync(Batch(3));
        await host.FlushAsync();

        Assert.IsEmpty(host.Memory.Logs, "A page that exports itself has nothing to relay; a call is someone feeding the pipeline directly.");
    }

    [TestMethod]
    public async Task OnAServer_EachPageIsRateLimited()
    {
        await using var host = TestHost.Create(o =>
        {
            Server(o);
            o.Browser.MaxQueueSize = 10;
            o.Consent.PendingLimit = 5;
        });
        var bridge = Bridge(host);
        var other = Bridge(host);

        await bridge.RelayAsync(Batch(40));
        await bridge.RelayAsync(Batch(5));
        await other.RelayAsync(Batch(3));
        await host.FlushAsync();

        // A full page queue plus what a consent decision releases (10 + 5) as a burst; the rest of the minute's
        // budget refills far slower than this test runs.
        Assert.AreEqual(15 + 3, host.Memory.Logs.Count, "One page's flood costs the other pages nothing.");
    }

    [TestMethod]
    public async Task InWebAssemblyAndHybrid_ThePageIsTrusted()
    {
        await using var host = TestHost.Create(o =>
        {
            o.Browser.Transport = BelemetryBrowserTransport.Relay;
            o.Browser.MaxQueueSize = 10;
            o.Consent.PendingLimit = 0;
        });
        var bridge = Bridge(host);

        await bridge.RelayAsync(Batch(40));
        await host.FlushAsync();

        Assert.AreEqual(40, host.Memory.Logs.Count);
    }

    [TestMethod]
    public async Task OnAServer_TheServerSaysWhichServiceTheRecordsBelongTo()
    {
        await using var host = TestHost.Create(o =>
        {
            Server(o);
            o.Environment = "Production";
        });

        await Bridge(host).RelayAsync(Batch(1, """{ "service.name": "spoofed", "deployment.environment.name": "Staging", "browser.language": "en-US", "service.instance.id": "tab-1" }"""));
        await host.FlushAsync();

        var resource = host.Memory.LastResource!.Attributes;
        Assert.AreEqual("belemetry-tests", resource["service.name"]);
        Assert.AreEqual("1.2.3", resource["service.version"]);
        Assert.AreEqual("Production", resource["deployment.environment.name"]);
        Assert.AreEqual("en-US", resource["browser.language"], "The page describes itself.");
        Assert.AreEqual("tab-1", resource["service.instance.id"], "A tab is its own instance.");
        Assert.IsFalse(resource.Contains("process.pid"), "The server's process describes the server, not the page.");
        Assert.IsFalse(resource.Contains("os.type"));
    }

    [TestMethod]
    public async Task InHybrid_ThePageRunsOnTheDeviceTheProcessDescribes()
    {
        await using var host = TestHost.Create(o => o.Browser.Transport = BelemetryBrowserTransport.Relay);

        await Bridge(host).RelayAsync(Batch(1, """{ "browser.language": "en-US", "telemetry.sdk.language": "webjs" }"""));
        await host.FlushAsync();

        var resource = host.Memory.LastResource!.Attributes;
        Assert.AreEqual("en-US", resource["browser.language"]);
        Assert.AreEqual("webjs", resource["telemetry.sdk.language"]);
        Assert.IsTrue(resource.Contains("process.pid"));
    }

    [TestMethod]
    public async Task APagesBatches_ShareOneResource_SoTheyExportTogether()
    {
        await using var host = TestHost.Create(o => o.Browser.Transport = BelemetryBrowserTransport.Relay);
        var batches = new List<BelemetryBatch>();
        host.Memory.Exported += batches.Add;
        var bridge = Bridge(host);

        const string resource = """{ "browser.language": "en-US" }""";
        await bridge.RelayAsync(Batch(2, resource));
        await bridge.RelayAsync(Batch(3, resource));
        await bridge.RelayAsync(Batch(1, """{ "browser.language": "de-DE" }"""));
        await host.FlushAsync();

        Assert.HasCount(2, batches, "One batch per distinct resource, not one per relay call.");
        Assert.HasCount(5, batches[0].Logs);
        Assert.AreEqual("de-DE", batches[1].Resource.Attributes["browser.language"]);
    }

    [TestMethod]
    public async Task DropsFeaturesTheConfigurationSwitchedOff()
    {
        await using var host = TestHost.Create(o =>
        {
            o.Browser.Transport = BelemetryBrowserTransport.Relay;
            o.Features = BelemetryFeatures.All & ~BelemetryFeatures.Interactions;
        });
        var bridge = Bridge(host);

        await bridge.RelayAsync(Batch(2, feature: (int)BelemetryFeatures.Interactions));
        await bridge.RelayAsync(Batch(1, feature: (int)BelemetryFeatures.Events));
        await host.FlushAsync();

        Assert.AreEqual(BelemetryFeatures.Events, host.Memory.Logs.Single().Feature, "A page cannot consent to what the operator switched off.");
    }

    [TestMethod]
    public async Task OnAServer_TheIdsTheOperatorBlocks_AreRemovedFromRelayedRecords()
    {
        await using var host = TestHost.Create(o =>
        {
            Server(o);
            o.Privacy.BlockedAttributes.Add("user.id");
            o.Privacy.BlockedAttributes.Add("enduser.pseudo.id");
        });
        var bridge = Bridge(host);
        var batch = JsonDocument.Parse("""
            { "resource": {}, "logs": [ { "time": 1700000000000, "severityNumber": 9, "body": "e", "eventName": "e", "feature": 2048,
              "attributes": { "session.id": "s1", "enduser.pseudo.id": "p1", "user.id": "jane@example.com" } } ] }
            """).RootElement.Clone();

        await bridge.RelayAsync(batch);
        await host.FlushAsync();

        var attributes = host.Memory.Logs.Single().Attributes;
        Assert.AreEqual("s1", attributes["session.id"]);
        Assert.IsFalse(attributes.TryGetValue("enduser.pseudo.id", out _));
        Assert.IsFalse(attributes.TryGetValue("user.id", out _), "The page added it; the server's configuration keeps it out of the backend.");
    }
}
