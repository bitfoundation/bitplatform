using System.Text.RegularExpressions;

namespace Bit.Belemetry.Tests.Interop;

[TestClass]
public class BelemetryBrowserConfigFactoryTests
{
    private const string ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000001;IngestionEndpoint=https://westeurope-5.in.applicationinsights.azure.com/;LiveEndpoint=https://westeurope.livediagnostics.monitor.azure.com/";

    private static JsonElement Render(Action<BelemetryOptions>? configure = null, bool webHost = false)
    {
        var settings = TestHost.CreateSettings(configure, webHost);
        using var document = JsonDocument.Parse(BelemetryBrowserConfigFactory.ToJson(BelemetryBrowserConfigFactory.Create(settings)));
        return document.RootElement.Clone();
    }

    /// <summary>A settings object with the library defaults (only the service identity and env vars pinned).</summary>
    private static void LibraryDefaults(BelemetryOptions o)
    {
        o.ServiceName = "MyApp";
        o.ServiceVersion = "1.0.0";
        o.Environment = "Production";
        o.UseEnvironmentVariables = false;
        o.Consent.Persist = true;
        o.Persistence.Enabled = true;
        o.Browser.Transport = BelemetryBrowserTransport.Direct;
    }

    private static string[] Keys(JsonElement element) => element.EnumerateObject().Select(p => p.Name).ToArray();

    private static string[] Strings(JsonElement element) => element.EnumerateArray().Select(e => e.GetString()!).ToArray();

    private static void AssertKeys(JsonElement element, params string[] expected) =>
        CollectionAssert.AreEquivalent(expected, Keys(element), string.Join(",", Keys(element)));

    [TestMethod]
    public void Defaults_MatchTheContractKeyByKey()
    {
        var config = Render(LibraryDefaults);

        AssertKeys(config, "service", "resource", "sdkVersion", "transport", "exporters", "features", "consent", "pageViews", "interactions",
                   "performance", "dependencies", "errors", "logs", "privacy", "session", "sampling", "batch", "persistence", "debug");

        // instanceId is left to the script: every page load is its own instance (null values are omitted).
        var service = config.GetProperty("service");
        AssertKeys(service, "name", "version", "environment");
        Assert.AreEqual("MyApp", service.GetProperty("name").GetString());
        Assert.AreEqual("1.0.0", service.GetProperty("version").GetString());
        Assert.AreEqual("Production", service.GetProperty("environment").GetString());

        Assert.AreEqual(JsonValueKind.Object, config.GetProperty("resource").ValueKind);
        Assert.AreEqual(BelemetrySettings.SdkVersion, config.GetProperty("sdkVersion").GetString());
        Assert.AreEqual("direct", config.GetProperty("transport").GetString());
        Assert.AreEqual(0, config.GetProperty("exporters").GetArrayLength());
        Assert.AreEqual(4095, config.GetProperty("features").GetInt32());
        Assert.IsFalse(config.GetProperty("debug").GetBoolean());

        var consent = config.GetProperty("consent");
        AssertKeys(consent, "default", "persist", "honorGlobalPrivacyControl", "pendingLimit");
        Assert.AreEqual(0, consent.GetProperty("default").GetInt32());
        Assert.IsTrue(consent.GetProperty("persist").GetBoolean());
        Assert.IsTrue(consent.GetProperty("honorGlobalPrivacyControl").GetBoolean());
        Assert.AreEqual(500, consent.GetProperty("pendingLimit").GetInt32());

        var pageViews = config.GetProperty("pageViews");
        AssertKeys(pageViews, "trackHashChanges", "trackVisitTime");
        Assert.IsFalse(pageViews.GetProperty("trackHashChanges").GetBoolean());
        Assert.IsFalse(pageViews.GetProperty("trackVisitTime").GetBoolean());

        var interactions = config.GetProperty("interactions");
        AssertKeys(interactions, "events", "captureText", "maxTextLength");
        CollectionAssert.AreEqual(new[] { "click" }, Strings(interactions.GetProperty("events")));
        Assert.IsFalse(interactions.GetProperty("captureText").GetBoolean());
        Assert.AreEqual(64, interactions.GetProperty("maxTextLength").GetInt32());

        var performance = config.GetProperty("performance");
        AssertKeys(performance, "webVitals", "documentLoad", "resources", "longTasks");
        Assert.IsTrue(performance.GetProperty("webVitals").GetBoolean());
        Assert.IsTrue(performance.GetProperty("documentLoad").GetBoolean());
        Assert.IsFalse(performance.GetProperty("resources").GetBoolean());
        Assert.IsFalse(performance.GetProperty("longTasks").GetBoolean());

        var dependencies = config.GetProperty("dependencies");
        AssertKeys(dependencies, "fetch", "xhr", "propagateTraceContextTo", "ignoreUrls");
        Assert.IsTrue(dependencies.GetProperty("fetch").GetBoolean());
        Assert.IsTrue(dependencies.GetProperty("xhr").GetBoolean());
        CollectionAssert.AreEqual(new[] { "same-origin" }, Strings(dependencies.GetProperty("propagateTraceContextTo")));
        CollectionAssert.AreEqual(new[] { "/_framework/", "/_blazor", "/_content/" }, Strings(dependencies.GetProperty("ignoreUrls")));

        var errors = config.GetProperty("errors");
        AssertKeys(errors, "ignoreMessages");
        CollectionAssert.AreEqual(new[] { "ResizeObserver loop" }, Strings(errors.GetProperty("ignoreMessages")));

        var logs = config.GetProperty("logs");
        AssertKeys(logs, "captureConsole", "consoleLevels");
        Assert.IsFalse(logs.GetProperty("captureConsole").GetBoolean());
        CollectionAssert.AreEqual(new[] { "error", "warn" }, Strings(logs.GetProperty("consoleLevels")));

        var privacy = config.GetProperty("privacy");
        AssertKeys(privacy, "redactQueryParameters", "stripQueryString", "stripFragment", "scrubPatterns", "collectUserAgent", "blockedAttributes");
        CollectionAssert.AreEqual(
            new[] { "token", "code", "password", "pwd", "secret", "key", "apikey", "api_key", "auth", "authorization", "session", "sid", "sig", "signature",
                    "email", "access_token", "id_token", "refresh_token" },
            Strings(privacy.GetProperty("redactQueryParameters")));
        Assert.IsFalse(privacy.GetProperty("stripQueryString").GetBoolean());
        Assert.IsTrue(privacy.GetProperty("stripFragment").GetBoolean());
        Assert.IsTrue(privacy.GetProperty("scrubPatterns").GetBoolean());
        Assert.IsTrue(privacy.GetProperty("collectUserAgent").GetBoolean());
        Assert.AreEqual(0, privacy.GetProperty("blockedAttributes").GetArrayLength());

        var session = config.GetProperty("session");
        AssertKeys(session, "inactivityTimeoutMinutes", "maxDurationMinutes");
        Assert.AreEqual(30, session.GetProperty("inactivityTimeoutMinutes").GetDouble());
        Assert.AreEqual(1440, session.GetProperty("maxDurationMinutes").GetDouble());

        var sampling = config.GetProperty("sampling");
        AssertKeys(sampling, "sessionRatio");
        Assert.AreEqual(1.0, sampling.GetProperty("sessionRatio").GetDouble());

        var batch = config.GetProperty("batch");
        AssertKeys(batch, "maxQueueSize", "maxBatchSize", "flushIntervalMs");
        Assert.AreEqual(1024, batch.GetProperty("maxQueueSize").GetInt32());
        Assert.AreEqual(100, batch.GetProperty("maxBatchSize").GetInt32());
        Assert.AreEqual(5000, batch.GetProperty("flushIntervalMs").GetInt32());

        var persistence = config.GetProperty("persistence");
        AssertKeys(persistence, "enabled", "maxBytes", "maxAgeHours", "retryIntervalMs");
        Assert.IsTrue(persistence.GetProperty("enabled").GetBoolean());
        Assert.AreEqual(1048576, persistence.GetProperty("maxBytes").GetInt64());
        Assert.AreEqual(48, persistence.GetProperty("maxAgeHours").GetDouble());
        Assert.AreEqual(60000, persistence.GetProperty("retryIntervalMs").GetInt32());
    }

    [TestMethod]
    public void CustomValues_AreCarriedOver()
    {
        var config = Render(o =>
        {
            LibraryDefaults(o);
            o.ServiceNamespace = "shop";
            o.ResourceAttributes["team"] = "core";
            o.ResourceAttributes["replicas"] = 3;
            o.ResourceAttributes["skipped"] = null;
            o.Features = BelemetryFeatures.Errors | BelemetryFeatures.PageViews;
            o.Consent.Default = BelemetryFeatures.Errors;
            o.Browser.Debug = true;
            o.Browser.FlushInterval = TimeSpan.FromMilliseconds(10);
            o.Browser.SessionInactivityTimeout = TimeSpan.FromMinutes(5);
            o.Browser.SessionSampleRatio = 0.25;
            o.Browser.CaptureInteractionText = true;
            o.Privacy.StripQueryString = true;
            o.Privacy.BlockedAttributes.Add("tenant.secret");
            o.Persistence.RetryInterval = TimeSpan.FromSeconds(15);
        });

        var service = config.GetProperty("service");
        Assert.AreEqual("shop", service.GetProperty("namespace").GetString());

        var resource = config.GetProperty("resource");
        AssertKeys(resource, "service.namespace", "team", "replicas");
        Assert.AreEqual("core", resource.GetProperty("team").GetString());
        Assert.AreEqual(3, resource.GetProperty("replicas").GetInt64());

        Assert.AreEqual(3, config.GetProperty("features").GetInt32());
        Assert.AreEqual(1, config.GetProperty("consent").GetProperty("default").GetInt32());
        Assert.IsTrue(config.GetProperty("debug").GetBoolean());
        Assert.AreEqual(250, config.GetProperty("batch").GetProperty("flushIntervalMs").GetInt32(), "The browser flush interval has a floor.");
        Assert.AreEqual(5, config.GetProperty("session").GetProperty("inactivityTimeoutMinutes").GetDouble());
        Assert.AreEqual(0.25, config.GetProperty("sampling").GetProperty("sessionRatio").GetDouble());
        Assert.IsTrue(config.GetProperty("interactions").GetProperty("captureText").GetBoolean());
        Assert.IsTrue(config.GetProperty("privacy").GetProperty("stripQueryString").GetBoolean());
        CollectionAssert.AreEqual(new[] { "tenant.secret" }, Strings(config.GetProperty("privacy").GetProperty("blockedAttributes")));
        Assert.AreEqual(15000, config.GetProperty("persistence").GetProperty("retryIntervalMs").GetInt32());
    }

    [TestMethod]
    [DataRow(0, 60000)]
    [DataRow(-5, 60000)]
    [DataRow(0.2, 1000)]
    public void RetryInterval_HasTheSameFallbackAsDotnet_AndAFloor(double seconds, int expected)
    {
        var config = Render(o =>
        {
            LibraryDefaults(o);
            o.Persistence.RetryInterval = TimeSpan.FromSeconds(seconds);
        });

        Assert.AreEqual(expected, config.GetProperty("persistence").GetProperty("retryIntervalMs").GetInt32());
    }

    [TestMethod]
    [DataRow(false, true)]
    [DataRow(true, false)]
    public void Disabled_TurnsEveryFeatureOff(bool enabled, bool browserEnabled)
    {
        var config = Render(o =>
        {
            LibraryDefaults(o);
            o.Enabled = enabled;
            o.Browser.Enabled = browserEnabled;
        });

        Assert.AreEqual(0, config.GetProperty("features").GetInt32());
    }

    [TestMethod]
    public void DirectTransport_ListsTheBrowserExporters()
    {
        var config = Render(o =>
        {
            LibraryDefaults(o);
            o.AddOtlpExporter("http://collector.test:4318", e =>
            {
                e.Protocol = BelemetryOtlpProtocol.HttpJson;
                e.Compression = BelemetryCompression.Gzip;
                e.Headers["x-api-key"] = "k";
                e.TracesEndpoint = new Uri("http://traces.test/custom/traces");
            });
            o.AddOtlpExporter("http://internal:4318", e => e.BrowserEndpoint = new Uri("https://public.test/otlp"));
            o.AddOtlpExporter("http://server-only:4318", e => e.IncludeInBrowser = false);
            o.AddOtlpExporter("http://metrics-only:4318", e => e.Signals = BelemetrySignals.Metrics);
            o.AddAzureMonitorExporter(ConnectionString, a =>
            {
                a.MaskIp = false;
                a.Compression = BelemetryCompression.Gzip;
                a.Signals = BelemetrySignals.Logs | BelemetrySignals.Metrics;
            });
            o.AddAzureMonitorExporter(ConnectionString, a => a.Signals = BelemetrySignals.Metrics);
            o.AddAzureMonitorExporter(ConnectionString, a => a.AccessTokenProvider = _ => ValueTask.FromResult<string?>("token"));
            o.AddConsoleExporter();
        });

        var exporters = config.GetProperty("exporters").EnumerateArray().ToList();
        CollectionAssert.AreEqual(new[] { "otlp", "otlp", "azureMonitor", "console" }, exporters.Select(e => e.GetProperty("type").GetString()).ToArray(),
                                  "Server-only, metrics-only and Entra ID exporters stay on the server.");

        var otlp = exporters[0];
        AssertKeys(otlp, "type", "endpoint", "tracesEndpoint", "protocol", "headers", "compression", "signals");
        Assert.AreEqual(3, otlp.GetProperty("signals").GetInt32(), "Traces and logs; metrics never come from the browser.");
        Assert.AreEqual("http://collector.test:4318/", otlp.GetProperty("endpoint").GetString());
        Assert.AreEqual("http://traces.test/custom/traces", otlp.GetProperty("tracesEndpoint").GetString());
        Assert.AreEqual("http/json", otlp.GetProperty("protocol").GetString());
        Assert.AreEqual("gzip", otlp.GetProperty("compression").GetString());
        Assert.AreEqual("k", otlp.GetProperty("headers").GetProperty("x-api-key").GetString());

        var browserEndpoint = exporters[1];
        AssertKeys(browserEndpoint, "type", "endpoint", "protocol", "compression", "signals");
        Assert.AreEqual("https://public.test/otlp", browserEndpoint.GetProperty("endpoint").GetString());
        Assert.AreEqual("http/protobuf", browserEndpoint.GetProperty("protocol").GetString());
        Assert.AreEqual("none", browserEndpoint.GetProperty("compression").GetString());

        var azure = exporters[2];
        AssertKeys(azure, "type", "connectionString", "maskIp", "compression", "signals");
        Assert.AreEqual(ConnectionString, azure.GetProperty("connectionString").GetString());
        Assert.IsFalse(azure.GetProperty("maskIp").GetBoolean());
        Assert.AreEqual("gzip", azure.GetProperty("compression").GetString());
        Assert.AreEqual(2, azure.GetProperty("signals").GetInt32());

        AssertKeys(exporters[3], "type");
    }

    [TestMethod]
    public void BoundLists_AreDeduplicated_WhereADuplicateWouldChangeBehavior()
    {
        // A configuration binder appends to a list's defaults: "InteractionEvents": ["click", "submit"] yields click twice.
        var config = Render(o =>
        {
            LibraryDefaults(o);
            o.Browser.InteractionEvents.AddRange(["click", "Submit"]);
            o.Browser.ConsoleLevels.AddRange(["error", "info"]);
        });

        CollectionAssert.AreEqual(new[] { "click", "Submit" }, Strings(config.GetProperty("interactions").GetProperty("events")));
        CollectionAssert.AreEqual(new[] { "error", "warn", "info" }, Strings(config.GetProperty("logs").GetProperty("consoleLevels")));
    }

    [TestMethod]
    public void GrpcExporter_ReachesTheBrowserOnlyThroughAnHttpBrowserEndpoint()
    {
        var config = Render(o =>
        {
            LibraryDefaults(o);
            o.AddOtlpExporter("https://aspire.test:21031", e => e.Protocol = BelemetryOtlpProtocol.Grpc);
            o.AddOtlpExporter("https://aspire.test:21031", e =>
            {
                e.Protocol = BelemetryOtlpProtocol.Grpc;
                e.BrowserEndpoint = new Uri("https://aspire.test:21032");
            });
        });

        var exporters = config.GetProperty("exporters").EnumerateArray().ToList();
        Assert.HasCount(1, exporters, "A page cannot speak gRPC: without a BrowserEndpoint the exporter stays on the server.");

        // In WebAssembly the .NET pipeline cannot speak gRPC either: the exporter becomes OTLP/HTTP to its absolute
        // BrowserEndpoint, and one without is dropped with a warning.
        var options = new BelemetryOptions();
        TestHost.ApplyTestDefaults(options);
        options.AddOtlpExporter("https://aspire.test:21031", e => e.Protocol = BelemetryOtlpProtocol.Grpc);
        options.AddOtlpExporter("https://aspire.test:21031", e =>
        {
            e.Protocol = BelemetryOtlpProtocol.Grpc;
            e.BrowserEndpoint = new Uri("https://aspire.test:21032");
            e.TracesEndpoint = new Uri("https://aspire.test:21031");
        });
        options.AddOtlpExporter("https://aspire.test:21031", e =>
        {
            e.Protocol = BelemetryOtlpProtocol.Grpc;
            e.BrowserEndpoint = new Uri("/otlp", UriKind.Relative);
        });
        var wasm = new BelemetrySettings(options, new BelemetryHostDetection(false), isBrowser: true);

        var kept = wasm.OtlpExporters.Single();
        Assert.AreEqual(BelemetryOtlpProtocol.HttpProtobuf, kept.Protocol);
        Assert.AreEqual(new Uri("https://aspire.test:21032"), kept.Endpoint);
        Assert.IsNull(kept.TracesEndpoint, "A per-signal gRPC address means nothing over HTTP to another host.");
        Assert.HasCount(1, wasm.StartupWarnings);
        StringAssert.Contains(wasm.StartupWarnings[0], "gRPC");
        var wasmConfig = BelemetryBrowserConfigFactory.Create(wasm);
        Assert.AreEqual("https://aspire.test:21032/", wasmConfig.Exporters.Single().Endpoint);
        Assert.AreEqual("http/protobuf", wasmConfig.Exporters.Single().Protocol);
        Assert.AreEqual("https://aspire.test:21032/", exporters[0].GetProperty("endpoint").GetString());
        Assert.AreEqual("http/protobuf", exporters[0].GetProperty("protocol").GetString());

        var ignored = Strings(config.GetProperty("dependencies").GetProperty("ignoreUrls"));
        Assert.IsTrue(ignored.Any(u => u.Contains("TraceService/Export")), "The gRPC method URLs are export traffic too.");
    }

    [TestMethod]
    public void ConsoleExporter_CanStayOnTheServer()
    {
        var config = Render(o =>
        {
            LibraryDefaults(o);
            o.AddConsoleExporter(c => c.IncludeInBrowser = false);
        });

        Assert.AreEqual(0, config.GetProperty("exporters").GetArrayLength());
    }

    [TestMethod]
    [DataRow(BelemetryBrowserTransport.Relay, BelemetryHostKind.Auto)]
    [DataRow(BelemetryBrowserTransport.Auto, BelemetryHostKind.Hybrid)]
    public void RelayTransport_OmitsTheExporters(BelemetryBrowserTransport transport, BelemetryHostKind host)
    {
        var config = Render(o =>
        {
            LibraryDefaults(o);
            o.Host = host;
            o.Browser.Transport = transport;
            o.AddOtlpExporter("http://collector.test:4318");
            o.AddAzureMonitorExporter(ConnectionString);
            o.AddConsoleExporter();
        });

        Assert.AreEqual("relay", config.GetProperty("transport").GetString());
        Assert.AreEqual(0, config.GetProperty("exporters").GetArrayLength());
    }

    [TestMethod]
    [DataRow(BelemetryBrowserTransport.Direct)]
    [DataRow(BelemetryBrowserTransport.Relay)]
    public void ExporterUrls_AreIgnoredByTheFetchInstrumentation(BelemetryBrowserTransport transport)
    {
        var config = Render(o =>
        {
            LibraryDefaults(o);
            o.Browser.Transport = transport;
            o.AddOtlpExporter("http://collector.test:4318", e =>
            {
                e.LogsEndpoint = new Uri("http://logs.test/ingest");
                e.BrowserEndpoint = new Uri("https://public.test/otlp");
            });
            o.AddAzureMonitorExporter(ConnectionString);
            o.AddAzureMonitorExporter("not a connection string");
        });

        var ignore = Strings(config.GetProperty("dependencies").GetProperty("ignoreUrls"));
        var expected = new[]
        {
            "/_framework/", "/_blazor", "/_content/",
            Regex.Escape("http://collector.test:4318/v1/traces"),
            Regex.Escape("http://logs.test/ingest"),
            Regex.Escape("http://collector.test:4318/v1/metrics"),
            Regex.Escape("https://public.test/otlp"),
            Regex.Escape("https://westeurope-5.in.applicationinsights.azure.com/"),
            Regex.Escape("https://westeurope.livediagnostics.monitor.azure.com/"),
        };
        CollectionAssert.AreEqual(expected, ignore, string.Join(" | ", ignore));
        Assert.IsTrue(Regex.IsMatch("http://collector.test:4318/v1/traces", ignore[3]));
    }

    [TestMethod]
    public void Json_IsCamelCase_AndOmitsNulls()
    {
        var settings = TestHost.CreateSettings(LibraryDefaults);
        var json = BelemetryBrowserConfigFactory.ToJson(BelemetryBrowserConfigFactory.Create(settings));

        Assert.DoesNotContain("null", json);
        Assert.IsFalse(Regex.IsMatch(json, "\"[A-Z][A-Za-z]*\":"), json);

        var element = BelemetryBrowserConfigFactory.ToElement(BelemetryBrowserConfigFactory.Create(settings));
        Assert.AreEqual(json, element.GetRawText());
    }
}
