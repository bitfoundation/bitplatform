namespace Bit.Belemetry;

/// <summary>The settings of Belemetry, configured through <c>AddBitBelemetryServices</c>.</summary>
public sealed class BelemetryOptions
{
    /// <summary>Master switch: when <see langword="false"/> nothing is collected or exported. Defaults to <see langword="true"/>.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// <c>service.name</c> - the logical name of the app, the key every backend groups by. Defaults to
    /// <c>OTEL_SERVICE_NAME</c> when set (outside the browser), else the entry assembly name.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary><c>service.version</c>. Defaults to the entry assembly's informational version.</summary>
    public string? ServiceVersion { get; set; }

    /// <summary><c>service.namespace</c>.</summary>
    public string? ServiceNamespace { get; set; }

    /// <summary><c>service.instance.id</c>. Defaults to a random id per process (per page load in the browser).</summary>
    public string? ServiceInstanceId { get; set; }

    /// <summary><c>deployment.environment.name</c>, e.g. <c>Production</c>.</summary>
    public string? Environment { get; set; }

    /// <summary>Extra resource attributes. Merged with <c>OTEL_RESOURCE_ATTRIBUTES</c> outside the browser.</summary>
    public Dictionary<string, object?> ResourceAttributes { get; } = [];

    /// <summary>
    /// The features switched on by configuration. Defaults to <see cref="BelemetryFeatures.All"/>. A feature is only
    /// collected when it is both in here and granted by the user (see <see cref="Consent"/>).
    /// </summary>
    public BelemetryFeatures Features { get; set; } = BelemetryFeatures.All;

    /// <summary>The host kind. Defaults to <see cref="BelemetryHostKind.Auto"/> (detected).</summary>
    public BelemetryHostKind Host { get; set; } = BelemetryHostKind.Auto;

    /// <summary>
    /// Whether the standard <c>OTEL_*</c> environment variables (<c>OTEL_SERVICE_NAME</c>, <c>OTEL_RESOURCE_ATTRIBUTES</c>,
    /// <c>OTEL_EXPORTER_OTLP_ENDPOINT</c>, <c>OTEL_EXPORTER_OTLP_HEADERS</c>, <c>OTEL_EXPORTER_OTLP_PROTOCOL</c>) and
    /// <c>APPLICATIONINSIGHTS_CONNECTION_STRING</c> fill in unset values. Ignored in the browser. Defaults to <see langword="true"/>.
    /// </summary>
    public bool UseEnvironmentVariables { get; set; } = true;

    /// <summary>Consent.</summary>
    public BelemetryConsentOptions Consent { get; } = new();

    /// <summary>Data minimization.</summary>
    public BelemetryPrivacyOptions Privacy { get; } = new();

    /// <summary>The browser script.</summary>
    public BelemetryBrowserOptions Browser { get; } = new();

    /// <summary><c>ILogger</c> collection.</summary>
    public BelemetryLoggingOptions Logs { get; } = new();

    /// <summary><c>Activity</c> collection.</summary>
    public BelemetryTracingOptions Traces { get; } = new();

    /// <summary><c>Meter</c> collection.</summary>
    public BelemetryMetricsOptions Metrics { get; } = new();

    /// <summary>Batching of the .NET pipeline.</summary>
    public BelemetryBatchOptions Batch { get; } = new();

    /// <summary>Offline persistence.</summary>
    public BelemetryPersistenceOptions Persistence { get; } = new();

    /// <summary>The server side.</summary>
    public BelemetryServerOptions Server { get; } = new();

    /// <summary>The OTLP exporters.</summary>
    public List<BelemetryOtlpExporterOptions> OtlpExporters { get; } = [];

    /// <summary>The Azure Monitor exporters.</summary>
    public List<BelemetryAzureMonitorExporterOptions> AzureMonitorExporters { get; } = [];

    /// <summary>The console exporter, when added.</summary>
    public BelemetryConsoleExporterOptions? ConsoleExporter { get; set; }

    /// <summary>Custom exporters (.NET side only).</summary>
    public List<IBelemetryExporter> Exporters { get; } = [];

    /// <summary>Custom processors, run in order on every .NET-side record after the built-in privacy processor.</summary>
    public List<IBelemetryProcessor> Processors { get; } = [];

    /// <summary>Whether Belemetry logs its own diagnostics (export failures, drops, consent changes) at Debug level. Defaults to <see langword="false"/>.</summary>
    public bool Debug { get; set; }

    /// <summary>Adds an OTLP/HTTP exporter.</summary>
    public BelemetryOptions AddOtlpExporter(Action<BelemetryOtlpExporterOptions>? configure = null)
    {
        var exporter = new BelemetryOtlpExporterOptions();
        configure?.Invoke(exporter);
        OtlpExporters.Add(exporter);
        return this;
    }

    /// <summary>Adds an OTLP/HTTP exporter posting to <paramref name="endpoint"/>.</summary>
    public BelemetryOptions AddOtlpExporter(string endpoint, Action<BelemetryOtlpExporterOptions>? configure = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(endpoint);
        return AddOtlpExporter(o =>
        {
            o.Endpoint = new Uri(endpoint, UriKind.Absolute);
            configure?.Invoke(o);
        });
    }

    /// <summary>Adds an Azure Monitor (Application Insights) exporter.</summary>
    public BelemetryOptions AddAzureMonitorExporter(Action<BelemetryAzureMonitorExporterOptions>? configure = null)
    {
        var exporter = new BelemetryAzureMonitorExporterOptions();
        configure?.Invoke(exporter);
        AzureMonitorExporters.Add(exporter);
        return this;
    }

    /// <summary>Adds an Azure Monitor (Application Insights) exporter for <paramref name="connectionString"/>.</summary>
    public BelemetryOptions AddAzureMonitorExporter(string connectionString, Action<BelemetryAzureMonitorExporterOptions>? configure = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionString);
        return AddAzureMonitorExporter(o =>
        {
            o.ConnectionString = connectionString;
            configure?.Invoke(o);
        });
    }

    /// <summary>Adds the console exporter (development only).</summary>
    public BelemetryOptions AddConsoleExporter(Action<BelemetryConsoleExporterOptions>? configure = null)
    {
        ConsoleExporter ??= new BelemetryConsoleExporterOptions();
        configure?.Invoke(ConsoleExporter);
        return this;
    }

    /// <summary>Adds a custom exporter (.NET side only).</summary>
    public BelemetryOptions AddExporter(IBelemetryExporter exporter)
    {
        ArgumentNullException.ThrowIfNull(exporter);
        Exporters.Add(exporter);
        return this;
    }

    /// <summary>Adds a custom processor.</summary>
    public BelemetryOptions AddProcessor(IBelemetryProcessor processor)
    {
        ArgumentNullException.ThrowIfNull(processor);
        Processors.Add(processor);
        return this;
    }
}
