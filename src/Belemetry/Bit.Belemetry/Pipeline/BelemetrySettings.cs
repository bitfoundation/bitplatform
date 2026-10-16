using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;

namespace Bit.Belemetry;

/// <summary>
/// <see cref="BelemetryOptions"/> resolved once against the host: the detected host kind, the environment variables,
/// the resource, and the effective switches every other component reads.
/// </summary>
internal sealed class BelemetrySettings
{
    public BelemetrySettings(IOptions<BelemetryOptions> options, BelemetryHostDetection detection)
        : this(options.Value, detection) { }

    /// <param name="options">The options to resolve.</param>
    /// <param name="detection">What the service collection told about the host.</param>
    /// <param name="isBrowser">Overrides the browser detection; for the tests, which cannot run under the WebAssembly runtime.</param>
    public BelemetrySettings(BelemetryOptions options, BelemetryHostDetection detection, bool? isBrowser = null)
    {
        Options = options;
        IsBrowser = isBrowser ?? OperatingSystem.IsBrowser();

        Host = options.Host is not BelemetryHostKind.Auto ? options.Host
             : IsBrowser ? BelemetryHostKind.WebAssembly
             : detection.HasWebHost ? BelemetryHostKind.Server
             : BelemetryHostKind.Hybrid;

        var useEnv = options.UseEnvironmentVariables && IsBrowser is false;

        ServiceName = options.ServiceName
                      ?? (useEnv ? GetEnv("OTEL_SERVICE_NAME") : null)
                      ?? Assembly.GetEntryAssembly()?.GetName().Name
                      ?? "unknown_service";

        ServiceVersion = options.ServiceVersion ?? GetEntryVersion();

        // OTEL_RESOURCE_ATTRIBUTES is read once: it is the lowest-precedence source of the resource (the OpenTelemetry
        // SDK spec's order), and the one place an orchestrator such as the Aspire AppHost hands a process its instance id.
        var environmentResource = useEnv ? ParseResourceAttributes(GetEnv("OTEL_RESOURCE_ATTRIBUTES")) : null;

        // A server is one of a few long-lived instances and should keep its name across restarts (Application Insights
        // counts instances by it): the orchestrator's id, the platform's, else the machine name - which two instances on
        // one machine share, as they do with the Azure Monitor OpenTelemetry exporter; set ServiceInstanceId then. A
        // device or a tab is one of many and gets a random id, which identifies nobody.
        ServiceInstanceId = options.ServiceInstanceId
                            ?? environmentResource?.GetString(BelemetrySemanticConventions.ServiceInstanceId)
                            ?? (Host is BelemetryHostKind.Server ? (useEnv ? BelemetryResourceDetector.GetPlatformInstanceId() : null) ?? Environment.MachineName : null)
                            ?? Guid.NewGuid().ToString();

        Resource = BuildResource(options, useEnv, environmentResource);

        CollectDotnet = options.Enabled && (Host is not BelemetryHostKind.Server || options.Server.CollectDotnetTelemetry);

        BrowserTransport = options.Browser.Transport is not BelemetryBrowserTransport.Auto ? options.Browser.Transport
                         : Host is BelemetryHostKind.Hybrid ? BelemetryBrowserTransport.Relay
                         : BelemetryBrowserTransport.Direct;

        OtlpExporters = ResolveOtlp(options, useEnv);
        AzureMonitorExporters = ResolveAzure(options, useEnv);

        TraceSources = [.. options.Traces.Sources];
        Meters = [.. options.Metrics.Meters];
        if (Host is BelemetryHostKind.Server)
        {
            TraceSources.Add("Microsoft.AspNetCore");
            Meters.AddRange(["Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.Server.Kestrel", "Microsoft.AspNetCore.Routing", "Microsoft.AspNetCore.Diagnostics"]);
        }
    }

    public BelemetryOptions Options { get; }

    public bool IsBrowser { get; }

    public BelemetryHostKind Host { get; }

    public bool IsServer => Host is BelemetryHostKind.Server;

    public string ServiceName { get; }

    public string? ServiceVersion { get; }

    public string ServiceInstanceId { get; }

    /// <summary>The .NET-side resource.</summary>
    public BelemetryResource Resource { get; }

    /// <summary>Whether the collectors (ILogger, Activity, Meter) run in this process.</summary>
    public bool CollectDotnet { get; }

    public BelemetryBrowserTransport BrowserTransport { get; }

    public IReadOnlyList<BelemetryOtlpExporterOptions> OtlpExporters { get; }

    public IReadOnlyList<BelemetryAzureMonitorExporterOptions> AzureMonitorExporters { get; }

    public List<string> TraceSources { get; }

    public List<string> Meters { get; }

    public bool HasExporters => OtlpExporters.Count > 0 || AzureMonitorExporters.Count > 0 || Options.ConsoleExporter is not null || Options.Exporters.Count > 0;

    /// <summary>The runtime feature switch behind the <c>MetricsSupport</c> MSBuild property.</summary>
    public const string MetricsSupportSwitch = "System.Diagnostics.Metrics.Meter.IsSupported";

    /// <summary>
    /// Whether <c>System.Diagnostics.Metrics</c> works in this process. An app can switch it off with
    /// <c>MetricsSupport=false</c> - the WebAssembly SDK's default since .NET 9 - and then every Meter is inert: the
    /// listener never sees a measurement, and nothing says so. Settable for the tests, which cannot flip the
    /// process-wide switch without turning off the meters of every other test in the run.
    /// </summary>
    public bool MetricsSupported { get; set; } = AppContext.TryGetSwitch(MetricsSupportSwitch, out var supported) is false || supported;

    /// <summary>
    /// The features the .NET collectors may produce regardless of user consent. On the server the pipeline is
    /// process-wide and governed by <see cref="BelemetryServerOptions.Features"/>; elsewhere consent decides.
    /// </summary>
    public BelemetryFeatures ConfiguredFeatures => Options.Enabled ? Options.Features : BelemetryFeatures.None;

    /// <summary>Whether <paramref name="name"/> matches one of <paramref name="patterns"/> (exact, or a prefix before a trailing <c>*</c>).</summary>
    public static bool Matches(string name, IReadOnlyList<string> patterns)
    {
        foreach (var pattern in patterns)
        {
            if (pattern == "*") return true;
            if (pattern.EndsWith('*'))
            {
                if (name.StartsWith(pattern.AsSpan(0, pattern.Length - 1), StringComparison.OrdinalIgnoreCase)) return true;
            }
            else if (string.Equals(name, pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static BelemetryAttributes? ParseResourceAttributes(string? value)
    {
        if (value is null) return null;

        var attributes = new BelemetryAttributes();
        foreach (var pair in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var eq = pair.IndexOf('=', StringComparison.Ordinal);
            if (eq > 0) attributes.Set(Uri.UnescapeDataString(pair[..eq].Trim()), Uri.UnescapeDataString(pair[(eq + 1)..].Trim()));
        }
        return attributes;
    }

    private BelemetryResource BuildResource(BelemetryOptions options, bool useEnv, BelemetryAttributes? environmentResource)
    {
        var attributes = new BelemetryAttributes();

        // OTEL_RESOURCE_ATTRIBUTES goes first so explicit options override it, as the OpenTelemetry SDK spec orders them.
        if (environmentResource is not null)
        {
            foreach (var (key, value) in environmentResource) attributes.Set(key, value);
        }

        attributes.Set(BelemetrySemanticConventions.ServiceName, ServiceName);
        attributes.Set(BelemetrySemanticConventions.ServiceVersion, ServiceVersion);
        attributes.Set(BelemetrySemanticConventions.ServiceNamespace, options.ServiceNamespace);
        attributes.Set(BelemetrySemanticConventions.ServiceInstanceId, ServiceInstanceId);
        attributes.Set(BelemetrySemanticConventions.DeploymentEnvironmentName, options.Environment);
        attributes.Set(BelemetrySemanticConventions.TelemetrySdkName, BelemetrySemanticConventions.SdkName);
        attributes.Set(BelemetrySemanticConventions.TelemetrySdkLanguage, "dotnet");
        attributes.Set(BelemetrySemanticConventions.TelemetrySdkVersion, SdkVersion);
        attributes.Set(BelemetrySemanticConventions.ProcessRuntimeName, ".NET");
        attributes.Set(BelemetrySemanticConventions.ProcessRuntimeVersion, Environment.Version.ToString());
        attributes.Set(BelemetrySemanticConventions.ProcessRuntimeDescription, RuntimeInformation.FrameworkDescription);
        attributes.Set(BelemetrySemanticConventions.OsType, GetOsType());
        attributes.Set(BelemetrySemanticConventions.OsDescription, RuntimeInformation.OSDescription);
        attributes.Set(BelemetrySemanticConventions.HostArch, RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant());

        if (Host is BelemetryHostKind.Server)
        {
            BelemetryResourceDetector.Detect(attributes, useEnv);
        }
        else if (IsBrowser is false)
        {
            attributes.Set(BelemetrySemanticConventions.ProcessPid, Environment.ProcessId);
        }

        foreach (var (key, value) in options.ResourceAttributes) attributes.Set(key, value);

        return new BelemetryResource(attributes);
    }

    private List<BelemetryOtlpExporterOptions> ResolveOtlp(BelemetryOptions options, bool useEnv)
    {
        var list = options.OtlpExporters.ToList();

        if (useEnv && list.Count == 0
            && (GetEnvUri("OTEL_EXPORTER_OTLP_ENDPOINT") ?? GetEnvUri("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT") ?? GetEnvUri("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT") ?? GetEnvUri("OTEL_EXPORTER_OTLP_METRICS_ENDPOINT")) is not null)
        {
            // The standard OTel variables alone are enough to turn the OTLP exporter on - this is how the Aspire
            // AppHost and most container platforms wire telemetry into an app without code changes.
            list.Add(new BelemetryOtlpExporterOptions());
        }

        foreach (var exporter in list)
        {
            if (useEnv)
            {
                // Like every variable here, these only fill in what the code left unset: explicit configuration wins,
                // as in the OpenTelemetry SDKs.
                exporter.Endpoint ??= GetEnvUri("OTEL_EXPORTER_OTLP_ENDPOINT");
                exporter.TracesEndpoint ??= GetEnvUri("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT");
                exporter.LogsEndpoint ??= GetEnvUri("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT");
                exporter.MetricsEndpoint ??= GetEnvUri("OTEL_EXPORTER_OTLP_METRICS_ENDPOINT");

                if (GetEnv("OTEL_EXPORTER_OTLP_HEADERS") is { } headers)
                {
                    foreach (var pair in headers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        var eq = pair.IndexOf('=', StringComparison.Ordinal);
                        if (eq > 0) exporter.Headers.TryAdd(Uri.UnescapeDataString(pair[..eq].Trim()), Uri.UnescapeDataString(pair[(eq + 1)..].Trim()));
                    }
                }

                if (exporter.IsProtocolSet is false && GetEnv("OTEL_EXPORTER_OTLP_PROTOCOL") is { } protocol)
                {
                    if (protocol.Equals("http/json", StringComparison.OrdinalIgnoreCase)) exporter.Protocol = BelemetryOtlpProtocol.HttpJson;
                    else if (protocol.Equals("http/protobuf", StringComparison.OrdinalIgnoreCase)) exporter.Protocol = BelemetryOtlpProtocol.HttpProtobuf;
                    else if (protocol.Equals("grpc", StringComparison.OrdinalIgnoreCase)) exporter.Protocol = BelemetryOtlpProtocol.Grpc;
                    else StartupWarnings.Add($"OTEL_EXPORTER_OTLP_PROTOCOL is '{protocol}', which Belemetry does not know (http/protobuf, http/json or grpc); the OTLP exporter uses {exporter.Protocol}.");
                }

                if (exporter.IsCompressionSet is false && GetEnv("OTEL_EXPORTER_OTLP_COMPRESSION") is { } compression)
                {
                    if (compression.Equals("gzip", StringComparison.OrdinalIgnoreCase)) exporter.Compression = BelemetryCompression.Gzip;
                    else if (compression.Equals("none", StringComparison.OrdinalIgnoreCase)) exporter.Compression = BelemetryCompression.None;
                }

                if (exporter.IsTimeoutSet is false && GetEnv("OTEL_EXPORTER_OTLP_TIMEOUT") is { } timeout
                    && int.TryParse(timeout, NumberStyles.Integer, CultureInfo.InvariantCulture, out var milliseconds) && milliseconds > 0)
                {
                    exporter.Timeout = TimeSpan.FromMilliseconds(milliseconds);
                }
            }

            exporter.Endpoint ??= BelemetryOtlpExporter.DefaultEndpoint(exporter.Protocol);
        }

        if (IsBrowser)
        {
            // fetch cannot send HTTP/2 trailers, so a page has no way to speak gRPC; posting the frames to the gRPC port
            // over HTTP/1.1 would only fail quietly at every flush. In WebAssembly the whole exporter - the .NET pipeline's
            // half included - becomes OTLP/HTTP to the BrowserEndpoint, when it has an absolute one; a relative one is
            // only resolvable by the script, and the .NET side would have nowhere to post.
            foreach (var exporter in list)
            {
                if (exporter.Protocol != BelemetryOtlpProtocol.Grpc) continue;

                if (exporter.BrowserEndpoint is { IsAbsoluteUri: true } browserEndpoint)
                {
                    exporter.Protocol = BelemetryOtlpProtocol.HttpProtobuf;
                    exporter.Endpoint = browserEndpoint;
                    exporter.TracesEndpoint = exporter.LogsEndpoint = exporter.MetricsEndpoint = null;
                }
            }

            if (list.RemoveAll(e => e.Protocol == BelemetryOtlpProtocol.Grpc) > 0)
            {
                StartupWarnings.Add("An OTLP exporter uses the gRPC protocol, which the browser cannot speak; it is skipped in WebAssembly. Use OTLP/HTTP there, or give it an absolute OTLP/HTTP BrowserEndpoint.");
            }
        }

        return list;
    }

    /// <summary>What resolving the options found wrong; the pipeline logs them as warnings once it has a logger.</summary>
    public List<string> StartupWarnings { get; } = [];

    private static Uri? GetEnvUri(string name) =>
        GetEnv(name) is { } value && Uri.TryCreate(value, UriKind.Absolute, out var uri) ? uri : null;

    private static List<BelemetryAzureMonitorExporterOptions> ResolveAzure(BelemetryOptions options, bool useEnv)
    {
        var list = options.AzureMonitorExporters.ToList();
        var envConnectionString = useEnv ? GetEnv("APPLICATIONINSIGHTS_CONNECTION_STRING") : null;

        if (list.Count == 0 && envConnectionString is not null)
        {
            list.Add(new BelemetryAzureMonitorExporterOptions { ConnectionString = envConnectionString });
        }

        foreach (var exporter in list)
        {
            exporter.ConnectionString ??= envConnectionString;
        }

        return list.Where(e => string.IsNullOrWhiteSpace(e.ConnectionString) is false).ToList();
    }

    private static string? GetEnv(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? GetEntryVersion()
    {
        var assembly = Assembly.GetEntryAssembly();
        var informational = assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (informational is not null)
        {
            // Strip the "+<commit sha>" SourceLink appends, which would otherwise make every build a new version.
            var plus = informational.IndexOf('+', StringComparison.Ordinal);
            return plus > 0 ? informational[..plus] : informational;
        }

        return assembly?.GetName().Version?.ToString();
    }

    private static string GetOsType()
    {
        if (OperatingSystem.IsBrowser()) return "browser";
        if (OperatingSystem.IsWindows()) return "windows";
        if (OperatingSystem.IsAndroid()) return "android";
        if (OperatingSystem.IsIOS() || OperatingSystem.IsMacCatalyst()) return "ios";
        if (OperatingSystem.IsMacOS()) return "darwin";
        if (OperatingSystem.IsLinux()) return "linux";
        if (OperatingSystem.IsFreeBSD()) return "freebsd";
        return RuntimeInformation.OSDescription.ToLower(CultureInfo.InvariantCulture);
    }

    /// <summary>The package version, reported as <c>telemetry.sdk.version</c>.</summary>
    public static string SdkVersion { get; } =
        typeof(BelemetrySettings).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion is { } v
            ? (v.IndexOf('+', StringComparison.Ordinal) is var plus and > 0 ? v[..plus] : v)
            : typeof(BelemetrySettings).Assembly.GetName().Version?.ToString() ?? "0.0.0";
}
