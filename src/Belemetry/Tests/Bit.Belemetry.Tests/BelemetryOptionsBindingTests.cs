using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Bit.Belemetry.Tests;

/// <summary>
/// <see cref="BelemetryOptions"/> bound from an <c>appsettings.json</c> section, through the configuration binder's
/// source generator (<c>EnableConfigurationBindingGenerator</c> in the test project): the shape a trimmed or AOT app
/// binds with. Every option an app would put in configuration has to survive this, or the app silently keeps the
/// default - so this test names them all.
/// </summary>
[TestClass]
public class BelemetryOptionsBindingTests
{
    private const string Json = """
        {
          "Belemetry": {
            "Enabled": true,
            "ServiceName": "shop",
            "ServiceVersion": "3.1.0",
            "ServiceNamespace": "contoso",
            "ServiceInstanceId": "web-1",
            "Environment": "Production",
            "ResourceAttributes": { "team": "checkout", "tier": "1" },
            "Features": "Errors, PageViews, Logs, Traces",
            "Host": "Server",
            "UseEnvironmentVariables": false,
            "Debug": true,
            "Consent": { "Default": "Errors", "Persist": false, "HonorGlobalPrivacyControl": false, "PendingLimit": 50 },
            "Privacy": { "RedactQueryParameters": [ "ssn" ], "StripQueryString": true, "StripFragment": false, "ScrubPatterns": false, "CollectUserAgent": false, "BlockedAttributes": [ "user.email" ] },
            "Browser": {
              "Enabled": true, "Transport": "Relay", "TrackHashChanges": true, "TrackPageVisitTime": true,
              "InteractionEvents": [ "click", "submit" ], "CaptureInteractionText": true, "MaxInteractionTextLength": 32,
              "WebVitals": false, "DocumentLoad": false, "ResourceTiming": true, "LongTasks": true, "Fetch": false, "Xhr": false,
              "PropagateTraceContextTo": [ "same-origin", "^https://api\\.shop\\.test/" ], "IgnoreUrls": [ "/cdn/" ], "IgnoreErrorMessages": [ "Script error" ],
              "CaptureConsole": true, "ConsoleLevels": [ "error" ],
              "SessionInactivityTimeout": "00:10:00", "SessionMaxDuration": "08:00:00", "SessionSampleRatio": 0.5,
              "MaxQueueSize": 256, "MaxBatchSize": 25, "FlushInterval": "00:00:02", "Debug": true
            },
            "Logs": { "MinimumLevel": "Warning", "IncludeScopes": false, "IncludeMessageTemplate": true, "ExcludedCategories": [ "Microsoft.AspNetCore" ] },
            "Traces": { "Sources": [ "Shop.*" ], "IgnoredActivityNames": [ "Microsoft.AspNetCore.Components.HandleEvent" ], "SamplingRatio": 0.25, "MaxEventsPerSpan": 16, "MaxLinksPerSpan": 8 },
            "Metrics": { "Meters": [ "Shop" ], "ExportInterval": "00:00:30", "Temporality": "Delta", "HistogramBoundaries": [ 0, 10, 100 ], "CardinalityLimit": 500 },
            "Batch": { "MaxQueueSize": 4096, "MaxBatchSize": 256, "FlushInterval": "00:00:10" },
            "Persistence": { "Enabled": false, "Directory": "C:/telemetry", "MaxBytes": 1024, "BrowserMaxBytes": 2048, "MaxAge": "1.00:00:00", "RetryInterval": "00:05:00" },
            "Server": { "CollectDotnetTelemetry": true, "Features": "Errors, Traces", "IgnoredRequestPaths": [ "/health", "/alive" ] },
            "OtlpExporters": [
              {
                "Endpoint": "https://otlp.shop.test:4317", "TracesEndpoint": "https://traces.shop.test/v1/traces", "BrowserEndpoint": "/otlp",
                "Protocol": "Grpc", "Headers": { "x-api-key": "k" }, "Compression": "Gzip", "Timeout": "00:00:05",
                "IncludeInBrowser": false, "Signals": "Traces, Logs", "MinimumLogLevel": "Information"
              }
            ],
            "AzureMonitorExporters": [
              { "ConnectionString": "InstrumentationKey=11111111-2222-3333-4444-555555555555", "MaskIp": false, "Compression": "Gzip", "Timeout": "00:00:20", "EnableLiveMetrics": true, "IncludeInBrowser": false, "Signals": "Logs", "MinimumLogLevel": "Error" }
            ],
            "ConsoleExporter": { "IncludeInBrowser": false }
          }
        }
        """;

    private static BelemetryOptions Bind(string json)
    {
        var configuration = new ConfigurationBuilder().AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json))).Build();
        var options = new BelemetryOptions();
        configuration.GetSection("Belemetry").Bind(options);
        return options;
    }

    [TestMethod]
    public void EveryConfigurableOption_BindsFromAppsettings()
    {
        var o = Bind(Json);

        Assert.AreEqual("shop", o.ServiceName);
        Assert.AreEqual("3.1.0", o.ServiceVersion);
        Assert.AreEqual("contoso", o.ServiceNamespace);
        Assert.AreEqual("web-1", o.ServiceInstanceId);
        Assert.AreEqual("Production", o.Environment);
        Assert.AreEqual("checkout", o.ResourceAttributes["team"]);
        Assert.AreEqual("1", o.ResourceAttributes["tier"], "Configuration values are strings; the resource keeps them as such.");
        Assert.AreEqual(BelemetryFeatures.Errors | BelemetryFeatures.PageViews | BelemetryFeatures.Logs | BelemetryFeatures.Traces, o.Features);
        Assert.AreEqual(BelemetryHostKind.Server, o.Host);
        Assert.IsFalse(o.UseEnvironmentVariables);
        Assert.IsTrue(o.Debug);

        Assert.AreEqual(BelemetryFeatures.Errors, o.Consent.Default);
        Assert.IsFalse(o.Consent.Persist);
        Assert.IsFalse(o.Consent.HonorGlobalPrivacyControl);
        Assert.AreEqual(50, o.Consent.PendingLimit);

        CollectionAssert.Contains(o.Privacy.RedactQueryParameters, "ssn");
        CollectionAssert.Contains(o.Privacy.RedactQueryParameters, "token", "Binding a list adds to the defaults; it does not replace them.");
        Assert.IsTrue(o.Privacy.StripQueryString);
        Assert.IsFalse(o.Privacy.StripFragment);
        Assert.IsFalse(o.Privacy.ScrubPatterns);
        Assert.IsFalse(o.Privacy.CollectUserAgent);
        CollectionAssert.AreEqual(new[] { "user.email" }, o.Privacy.BlockedAttributes);

        var b = o.Browser;
        Assert.AreEqual(BelemetryBrowserTransport.Relay, b.Transport);
        Assert.IsTrue(b.TrackHashChanges);
        Assert.IsTrue(b.TrackPageVisitTime);
        // The binder appends to a list's defaults, so "click" is there twice; the browser config dedupes it (see BelemetryBrowserConfigFactoryTests).
        CollectionAssert.AreEqual(new[] { "click", "click", "submit" }, b.InteractionEvents);
        Assert.IsTrue(b.CaptureInteractionText);
        Assert.AreEqual(32, b.MaxInteractionTextLength);
        Assert.IsFalse(b.WebVitals);
        Assert.IsFalse(b.DocumentLoad);
        Assert.IsTrue(b.ResourceTiming);
        Assert.IsTrue(b.LongTasks);
        Assert.IsFalse(b.Fetch);
        Assert.IsFalse(b.Xhr);
        CollectionAssert.Contains(b.PropagateTraceContextTo, "^https://api\\.shop\\.test/");
        CollectionAssert.Contains(b.IgnoreUrls, "/cdn/");
        CollectionAssert.Contains(b.IgnoreErrorMessages, "Script error");
        Assert.IsTrue(b.CaptureConsole);
        CollectionAssert.Contains(b.ConsoleLevels, "error");
        Assert.AreEqual(TimeSpan.FromMinutes(10), b.SessionInactivityTimeout);
        Assert.AreEqual(TimeSpan.FromHours(8), b.SessionMaxDuration);
        Assert.AreEqual(0.5, b.SessionSampleRatio);
        Assert.AreEqual(256, b.MaxQueueSize);
        Assert.AreEqual(25, b.MaxBatchSize);
        Assert.AreEqual(TimeSpan.FromSeconds(2), b.FlushInterval);
        Assert.IsTrue(b.Debug);

        Assert.AreEqual(LogLevel.Warning, o.Logs.MinimumLevel);
        Assert.IsFalse(o.Logs.IncludeScopes);
        Assert.IsTrue(o.Logs.IncludeMessageTemplate);
        CollectionAssert.AreEqual(new[] { "Microsoft.AspNetCore" }, o.Logs.ExcludedCategories);

        CollectionAssert.Contains(o.Traces.Sources, "Shop.*");
        CollectionAssert.AreEqual(new[] { "Microsoft.AspNetCore.Components.HandleEvent" }, o.Traces.IgnoredActivityNames);
        Assert.AreEqual(0.25, o.Traces.SamplingRatio);
        Assert.AreEqual(16, o.Traces.MaxEventsPerSpan);
        Assert.AreEqual(8, o.Traces.MaxLinksPerSpan);

        CollectionAssert.Contains(o.Metrics.Meters, "Shop");
        Assert.AreEqual(TimeSpan.FromSeconds(30), o.Metrics.ExportInterval);
        Assert.AreEqual(BelemetryMetricTemporality.Delta, o.Metrics.Temporality);
        CollectionAssert.AreEqual(new double[] { 0, 10, 100 }, o.Metrics.HistogramBoundaries);
        Assert.AreEqual(500, o.Metrics.CardinalityLimit);

        Assert.AreEqual(4096, o.Batch.MaxQueueSize);
        Assert.AreEqual(256, o.Batch.MaxBatchSize);
        Assert.AreEqual(TimeSpan.FromSeconds(10), o.Batch.FlushInterval);

        Assert.IsFalse(o.Persistence.Enabled);
        Assert.AreEqual("C:/telemetry", o.Persistence.Directory);
        Assert.AreEqual(1024, o.Persistence.MaxBytes);
        Assert.AreEqual(2048, o.Persistence.BrowserMaxBytes);
        Assert.AreEqual(TimeSpan.FromDays(1), o.Persistence.MaxAge);
        Assert.AreEqual(TimeSpan.FromMinutes(5), o.Persistence.RetryInterval);

        Assert.IsTrue(o.Server.CollectDotnetTelemetry);
        Assert.AreEqual(BelemetryFeatures.Errors | BelemetryFeatures.Traces, o.Server.Features);
        CollectionAssert.AreEqual(new[] { "/health", "/alive" }, o.Server.IgnoredRequestPaths);

        var otlp = o.OtlpExporters.Single();
        Assert.AreEqual(new Uri("https://otlp.shop.test:4317"), otlp.Endpoint);
        Assert.AreEqual(new Uri("https://traces.shop.test/v1/traces"), otlp.TracesEndpoint);
        Assert.AreEqual(new Uri("/otlp", UriKind.Relative), otlp.BrowserEndpoint);
        Assert.AreEqual(BelemetryOtlpProtocol.Grpc, otlp.Protocol);
        Assert.IsTrue(otlp.IsProtocolSet);
        Assert.AreEqual("k", otlp.Headers["x-api-key"]);
        Assert.AreEqual(BelemetryCompression.Gzip, otlp.Compression);
        Assert.AreEqual(TimeSpan.FromSeconds(5), otlp.Timeout);
        Assert.IsTrue(otlp.IsTimeoutSet);
        Assert.IsFalse(otlp.IncludeInBrowser);
        Assert.AreEqual(BelemetrySignals.Traces | BelemetrySignals.Logs, otlp.Signals);
        Assert.AreEqual(LogLevel.Information, otlp.MinimumLogLevel);

        var azure = o.AzureMonitorExporters.Single();
        StringAssert.Contains(azure.ConnectionString, "InstrumentationKey=");
        Assert.IsFalse(azure.MaskIp);
        Assert.AreEqual(BelemetryCompression.Gzip, azure.Compression);
        Assert.AreEqual(TimeSpan.FromSeconds(20), azure.Timeout);
        Assert.IsTrue(azure.EnableLiveMetrics);
        Assert.IsFalse(azure.IncludeInBrowser);
        Assert.AreEqual(BelemetrySignals.Logs, azure.Signals);
        Assert.AreEqual(LogLevel.Error, azure.MinimumLogLevel);

        Assert.IsNotNull(o.ConsoleExporter);
        Assert.IsFalse(o.ConsoleExporter.IncludeInBrowser);

        Assert.IsEmpty(o.Exporters, "Code-only members are left alone.");
        Assert.IsEmpty(o.Processors);
    }

    [TestMethod]
    public void BoundOptions_ResolveLikeCodeConfiguredOnes()
    {
        var settings = new BelemetrySettings(Bind(Json), new BelemetryHostDetection(true));
        Assert.AreEqual("shop", settings.ServiceName);
        Assert.AreEqual(BelemetryHostKind.Server, settings.Host);
        Assert.IsTrue(settings.CollectDotnet);
        Assert.AreEqual(new Uri("https://otlp.shop.test:4317"), settings.OtlpExporters.Single().Endpoint);
        Assert.HasCount(1, settings.AzureMonitorExporters);
        Assert.IsTrue(settings.HasExporters);
        Assert.AreEqual("checkout", settings.Resource.Attributes["team"]);
    }

    [TestMethod]
    public void AnEmptySection_LeavesTheDefaults()
    {
        var o = Bind("""{ "Belemetry": {} }""");
        Assert.IsTrue(o.Enabled);
        Assert.AreEqual(BelemetryFeatures.All, o.Features);
        Assert.IsEmpty(o.OtlpExporters);
        Assert.IsNull(o.ConsoleExporter);
        Assert.AreEqual(BelemetryOtlpProtocol.HttpProtobuf, new BelemetryOtlpExporterOptions().Protocol);
    }
}
