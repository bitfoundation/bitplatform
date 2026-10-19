namespace Bit.Belemetry.Tests.Pipeline;

// The environment is process-wide: these tests set and restore it, so they must not overlap with others.
[TestClass]
[DoNotParallelize]
public class BelemetrySettingsTests
{
    private static readonly string[] _envNames =
    [
        "OTEL_SERVICE_NAME", "OTEL_RESOURCE_ATTRIBUTES", "OTEL_EXPORTER_OTLP_ENDPOINT", "OTEL_EXPORTER_OTLP_HEADERS",
        "OTEL_EXPORTER_OTLP_PROTOCOL", "OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", "OTEL_EXPORTER_OTLP_LOGS_ENDPOINT", "OTEL_EXPORTER_OTLP_METRICS_ENDPOINT",
        "OTEL_EXPORTER_OTLP_TIMEOUT", "OTEL_EXPORTER_OTLP_COMPRESSION", "APPLICATIONINSIGHTS_CONNECTION_STRING",
    ];

    private const string ConnectionString = "InstrumentationKey=11111111-2222-3333-4444-555555555555;IngestionEndpoint=https://westeurope-5.in.applicationinsights.azure.com/";

    private Dictionary<string, string?> _savedEnv = [];

    [TestInitialize]
    public void SaveEnvironment()
    {
        _savedEnv = _envNames.ToDictionary(n => n, Environment.GetEnvironmentVariable);
        foreach (var name in _envNames) Environment.SetEnvironmentVariable(name, null);
    }

    [TestCleanup]
    public void RestoreEnvironment()
    {
        foreach (var (name, value) in _savedEnv) Environment.SetEnvironmentVariable(name, value);
    }

    [TestMethod]
    public void HostDetection()
    {
        Assert.AreEqual(BelemetryHostKind.Server, TestHost.CreateSettings(webHost: true).Host);
        Assert.AreEqual(BelemetryHostKind.Hybrid, TestHost.CreateSettings(webHost: false).Host);
        Assert.AreEqual(BelemetryHostKind.WebAssembly, TestHost.CreateSettings(o => o.Host = BelemetryHostKind.WebAssembly, webHost: true).Host);
        Assert.IsFalse(TestHost.CreateSettings().IsBrowser);
    }

    [TestMethod]
    public void CollectDotnet_DependsOnTheHost()
    {
        Assert.IsTrue(TestHost.CreateSettings().CollectDotnet);
        Assert.IsFalse(TestHost.CreateSettings(webHost: true).CollectDotnet);
        Assert.IsTrue(TestHost.CreateSettings(o => o.Server.CollectDotnetTelemetry = true, webHost: true).CollectDotnet);
        Assert.IsFalse(TestHost.CreateSettings(o => o.Enabled = false).CollectDotnet);
        Assert.AreEqual(BelemetryFeatures.None, TestHost.CreateSettings(o => o.Enabled = false).ConfiguredFeatures);
    }

    [TestMethod]
    public void Server_AddsAspNetCoreSourcesAndMeters()
    {
        var server = TestHost.CreateSettings(webHost: true);
        CollectionAssert.Contains(server.TraceSources, "Microsoft.AspNetCore");
        CollectionAssert.Contains(server.Meters, "Microsoft.AspNetCore.Hosting");
        CollectionAssert.Contains(server.Meters, "Microsoft.AspNetCore.Server.Kestrel");

        var hybrid = TestHost.CreateSettings();
        CollectionAssert.DoesNotContain(hybrid.TraceSources, "Microsoft.AspNetCore");

        var defaults = new BelemetrySettings(new BelemetryOptions(), new BelemetryHostDetection(false));
        CollectionAssert.AreEqual(new[] { "System.Net.Http", "Microsoft.AspNetCore.Components*", "Bit.Belemetry.*" }, defaults.TraceSources);
        Assert.IsTrue(BelemetrySettings.Matches("Bit.Belemetry.EntityFrameworkCore", defaults.TraceSources), "The instrumentation packages need no configuration.");
        CollectionAssert.AreEqual(new[] { "System.Net.Http", "Microsoft.AspNetCore.Components*", "System.Runtime" }, defaults.Meters);
    }

    [TestMethod]
    public void BrowserTransport_AutoResolution()
    {
        Assert.AreEqual(BelemetryBrowserTransport.Relay, TestHost.CreateSettings().BrowserTransport, "Hybrid relays.");
        Assert.AreEqual(BelemetryBrowserTransport.Direct, TestHost.CreateSettings(webHost: true).BrowserTransport);
        Assert.AreEqual(BelemetryBrowserTransport.Direct, TestHost.CreateSettings(o => o.Host = BelemetryHostKind.WebAssembly).BrowserTransport);
        Assert.AreEqual(BelemetryBrowserTransport.Relay, TestHost.CreateSettings(o => o.Browser.Transport = BelemetryBrowserTransport.Relay, webHost: true).BrowserTransport);
        Assert.AreEqual(BelemetryBrowserTransport.Direct, TestHost.CreateSettings(o => o.Browser.Transport = BelemetryBrowserTransport.Direct).BrowserTransport);
    }

    [TestMethod]
    public void Resource_HasServiceSdkAndProcessAttributes()
    {
        var settings = TestHost.CreateSettings(o =>
        {
            o.ServiceNamespace = "shop";
            o.Environment = "Staging";
            o.ResourceAttributes["custom"] = 5;
            o.ResourceAttributes["service.version"] = "override";
        }, webHost: true);

        var a = settings.Resource.Attributes;
        Assert.AreEqual("belemetry-tests", a["service.name"]);
        Assert.AreEqual("override", a["service.version"], "Explicit resource attributes win.");
        Assert.AreEqual("shop", a["service.namespace"]);
        Assert.AreEqual("instance-1", a["service.instance.id"]);
        Assert.AreEqual("Staging", a["deployment.environment.name"]);
        Assert.AreEqual("bit-belemetry", a["telemetry.sdk.name"]);
        Assert.AreEqual("dotnet", a["telemetry.sdk.language"]);
        Assert.AreEqual(BelemetrySettings.SdkVersion, a["telemetry.sdk.version"]);
        Assert.AreEqual(".NET", a["process.runtime.name"]);
        Assert.AreEqual((long)Environment.ProcessId, a["process.pid"]);
        Assert.AreEqual(Environment.MachineName, a["host.name"]);
        Assert.AreEqual(5L, a["custom"]);
        Assert.IsNotNull(a["os.type"]);
        Assert.IsFalse(BelemetrySettings.SdkVersion.Contains('+'), "The SourceLink commit suffix is stripped.");

        Assert.IsFalse(TestHost.CreateSettings().Resource.Attributes.Contains("host.name"), "Only a server reports its host name.");
        Assert.IsFalse(TestHost.CreateSettings().Resource.Attributes.Contains("service.namespace"));
        Assert.IsTrue(Guid.TryParse(new BelemetrySettings(new BelemetryOptions(), new BelemetryHostDetection(false)).ServiceInstanceId, out _));
    }

    [TestMethod]
    public void EnvironmentVariables_FillUnsetValues()
    {
        Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", "from-env");
        Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", "team=core, region=eu%20west,service.namespace=env-ns,bad");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://collector.env:4318");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS", "x-api-key=secret,x-other=a%3Db");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL", "http/json");
        Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING", ConnectionString);

        var settings = TestHost.CreateSettings(o =>
        {
            o.UseEnvironmentVariables = true;
            o.ServiceName = null;
            o.ServiceNamespace = "explicit-ns";
        });

        Assert.AreEqual("from-env", settings.ServiceName);
        var a = settings.Resource.Attributes;
        Assert.AreEqual("from-env", a["service.name"]);
        Assert.AreEqual("core", a["team"]);
        Assert.AreEqual("eu west", a["region"]);
        Assert.AreEqual("explicit-ns", a["service.namespace"], "Options override OTEL_RESOURCE_ATTRIBUTES.");
        Assert.IsFalse(a.Contains("bad"));

        var otlp = settings.OtlpExporters.Single();
        Assert.AreEqual(new Uri("http://collector.env:4318"), otlp.Endpoint);
        Assert.AreEqual("secret", otlp.Headers["x-api-key"]);
        Assert.AreEqual("a=b", otlp.Headers["X-OTHER"]);
        Assert.AreEqual(BelemetryOtlpProtocol.HttpJson, otlp.Protocol);

        Assert.AreEqual(ConnectionString, settings.AzureMonitorExporters.Single().ConnectionString);
        Assert.IsTrue(settings.HasExporters);
    }

    [TestMethod]
    public void EnvironmentVariables_DoNotOverrideExplicitOptions()
    {
        Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", "from-env");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://collector.env:4318");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS", "x-api-key=env");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL", "http/json");
        Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING", ConnectionString);

        var settings = TestHost.CreateSettings(o =>
        {
            o.UseEnvironmentVariables = true;
            o.AddOtlpExporter("http://explicit:4318", e =>
            {
                e.Headers["x-api-key"] = "explicit";
                // The default value, set explicitly: still a decision the environment must not overturn.
                e.Protocol = BelemetryOtlpProtocol.HttpProtobuf;
            });
            o.AddOtlpExporter();
            o.AddAzureMonitorExporter("InstrumentationKey=99999999-2222-3333-4444-555555555555");
            o.AddAzureMonitorExporter();
        });

        Assert.AreEqual("belemetry-tests", settings.ServiceName);
        Assert.HasCount(2, settings.OtlpExporters);
        Assert.AreEqual(new Uri("http://explicit:4318"), settings.OtlpExporters[0].Endpoint);
        Assert.AreEqual("explicit", settings.OtlpExporters[0].Headers["x-api-key"]);
        Assert.AreEqual(new Uri("http://collector.env:4318"), settings.OtlpExporters[1].Endpoint, "Only an unset endpoint comes from the environment.");
        Assert.AreEqual(BelemetryOtlpProtocol.HttpProtobuf, settings.OtlpExporters[0].Protocol, "A protocol set in code wins over OTEL_EXPORTER_OTLP_PROTOCOL.");
        Assert.AreEqual(BelemetryOtlpProtocol.HttpJson, settings.OtlpExporters[1].Protocol, "Only an unset protocol comes from the environment.");

        Assert.HasCount(2, settings.AzureMonitorExporters);
        StringAssert.Contains(settings.AzureMonitorExporters[0].ConnectionString, "99999999");
        Assert.AreEqual(ConnectionString, settings.AzureMonitorExporters[1].ConnectionString);
    }

    [TestMethod]
    public void EnvironmentVariables_PerSignalEndpointsTimeoutAndCompression()
    {
        // A per-signal endpoint alone turns the exporter on, as the base endpoint does.
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT", "http://logs.env:4318/custom/logs");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", "http://traces.env:4318/v1/traces");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_TIMEOUT", "2500");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_COMPRESSION", "gzip");

        var settings = TestHost.CreateSettings(o => o.UseEnvironmentVariables = true);
        var otlp = settings.OtlpExporters.Single();
        Assert.AreEqual(new Uri("http://localhost:4318"), otlp.Endpoint, "The base endpoint keeps its default.");
        Assert.AreEqual(new Uri("http://logs.env:4318/custom/logs"), otlp.LogsEndpoint);
        Assert.AreEqual(new Uri("http://traces.env:4318/v1/traces"), otlp.TracesEndpoint);
        Assert.IsNull(otlp.MetricsEndpoint);
        Assert.AreEqual(TimeSpan.FromMilliseconds(2500), otlp.Timeout);
        Assert.AreEqual(BelemetryCompression.Gzip, otlp.Compression);

        var explicitSettings = TestHost.CreateSettings(o =>
        {
            o.UseEnvironmentVariables = true;
            o.AddOtlpExporter(e =>
            {
                e.LogsEndpoint = new Uri("http://explicit/logs");
                e.Timeout = TimeSpan.FromSeconds(1);
                e.Compression = BelemetryCompression.None;
            });
        });
        var explicitOtlp = explicitSettings.OtlpExporters.Single();
        Assert.AreEqual(new Uri("http://explicit/logs"), explicitOtlp.LogsEndpoint);
        Assert.AreEqual(TimeSpan.FromSeconds(1), explicitOtlp.Timeout);
        Assert.AreEqual(BelemetryCompression.None, explicitOtlp.Compression, "A compression set in code, even the default, wins over the variable.");
        Assert.AreEqual(new Uri("http://traces.env:4318/v1/traces"), explicitOtlp.TracesEndpoint, "Only an unset endpoint comes from the environment.");
    }

    [TestMethod]
    public void EnvironmentVariables_GrpcProtocol()
    {
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "https://localhost:21031");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL", "grpc");

        var settings = TestHost.CreateSettings(o => o.UseEnvironmentVariables = true);
        var otlp = settings.OtlpExporters.Single();
        Assert.AreEqual(BelemetryOtlpProtocol.Grpc, otlp.Protocol);
        Assert.AreEqual(new Uri("https://localhost:21031"), otlp.Endpoint);
        Assert.IsEmpty(settings.StartupWarnings);

        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", null);
        Assert.AreEqual(new Uri("http://localhost:4317"), TestHost.CreateSettings(o =>
        {
            o.UseEnvironmentVariables = true;
            o.AddOtlpExporter();
        }).OtlpExporters.Single().Endpoint, "gRPC defaults to the OTLP gRPC port.");
        Assert.AreEqual(new Uri("http://localhost:4317"), TestHost.CreateSettings(o => o.AddOtlpExporter(e => e.Protocol = BelemetryOtlpProtocol.Grpc)).OtlpExporters.Single().Endpoint);
    }

    [TestMethod]
    public void EnvironmentVariables_UnknownProtocolIsReportedAndIgnored()
    {
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://collector.env:4318");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL", "http/thrift");

        var settings = TestHost.CreateSettings(o => o.UseEnvironmentVariables = true);
        Assert.AreEqual(BelemetryOtlpProtocol.HttpProtobuf, settings.OtlpExporters.Single().Protocol);
        StringAssert.Contains(settings.StartupWarnings.Single(), "http/thrift");
    }

    [TestMethod]
    public void EnvironmentVariables_AreIgnoredWhenDisabled()
    {
        Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", "from-env");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://collector.env:4318");
        Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING", ConnectionString);

        var settings = TestHost.CreateSettings(o =>
        {
            o.ServiceName = null;
            o.AddOtlpExporter();
            o.AddAzureMonitorExporter();
        });

        Assert.AreNotEqual("from-env", settings.ServiceName);
        Assert.AreEqual(new Uri("http://localhost:4318"), settings.OtlpExporters.Single().Endpoint);
        Assert.IsEmpty(settings.AzureMonitorExporters, "An Azure exporter without a connection string is dropped.");
    }

    [TestMethod]
    public void EnvironmentVariables_InvalidEndpointIsIgnored()
    {
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "not a uri");

        var settings = TestHost.CreateSettings(o => o.UseEnvironmentVariables = true);
        Assert.IsEmpty(settings.OtlpExporters);
        Assert.IsTrue(settings.HasExporters is false);
    }
}
