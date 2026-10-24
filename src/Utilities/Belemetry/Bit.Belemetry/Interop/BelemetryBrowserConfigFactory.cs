using System.Text.Json;
using System.Text.RegularExpressions;

namespace Bit.Belemetry;

/// <summary>Builds the browser script's configuration from the resolved settings.</summary>
internal static class BelemetryBrowserConfigFactory
{
    public static BelemetryBrowserConfig Create(BelemetrySettings settings)
    {
        var options = settings.Options;
        var browser = options.Browser;
        var relay = settings.BrowserTransport == BelemetryBrowserTransport.Relay;

        var config = new BelemetryBrowserConfig
        {
            Service = new()
            {
                Name = settings.ServiceName,
                Version = settings.ServiceVersion,
                Namespace = options.ServiceNamespace,
                // One instance id per page load: a browser tab is its own instance, not the server process.
                InstanceId = null,
                Environment = options.Environment,
            },
            SdkVersion = BelemetrySettings.SdkVersion,
            Transport = relay ? "relay" : "direct",
            Features = (int)(options.Enabled && browser.Enabled ? options.Features : BelemetryFeatures.None),
            Consent = new()
            {
                Default = (int)options.Consent.Default,
                Persist = options.Consent.Persist,
                HonorGlobalPrivacyControl = options.Consent.HonorGlobalPrivacyControl,
                PendingLimit = options.Consent.PendingLimit,
            },
            PageViews = new() { TrackHashChanges = browser.TrackHashChanges, TrackVisitTime = browser.TrackPageVisitTime },
            Interactions = new()
            {
                // Distinct: a configuration binder appends to the list's defaults, and a DOM event listed twice would
                // be listened to twice - two records per click.
                Events = [.. browser.InteractionEvents.Distinct(StringComparer.OrdinalIgnoreCase)],
                CaptureText = browser.CaptureInteractionText,
                MaxTextLength = browser.MaxInteractionTextLength,
                RageClicks = browser.TrackRageClicks,
            },
            Performance = new()
            {
                WebVitals = browser.WebVitals,
                DocumentLoad = browser.DocumentLoad,
                Resources = browser.ResourceTiming,
                LongTasks = browser.LongTasks,
            },
            Dependencies = new()
            {
                Fetch = browser.Fetch,
                Xhr = browser.Xhr,
                PropagateTraceContextTo = [.. browser.PropagateTraceContextTo],
                IgnoreUrls = [.. browser.IgnoreUrls],
            },
            Errors = new()
            {
                IgnoreMessages = [.. browser.IgnoreErrorMessages.Distinct(StringComparer.Ordinal)],
                DuplicateWindowMs = (int)Math.Clamp(options.Errors.DuplicateWindow.TotalMilliseconds, 0, int.MaxValue),
                MaxPerMinute = Math.Max(0, options.Errors.MaxPerMinute),
            },
            Logs = new() { CaptureConsole = browser.CaptureConsole, ConsoleLevels = [.. browser.ConsoleLevels.Distinct(StringComparer.OrdinalIgnoreCase)] },
            Privacy = new()
            {
                RedactQueryParameters = [.. options.Privacy.RedactQueryParameters.Distinct(StringComparer.OrdinalIgnoreCase)],
                StripQueryString = options.Privacy.StripQueryString,
                StripFragment = options.Privacy.StripFragment,
                ScrubPatterns = options.Privacy.ScrubPatterns,
                CollectUserAgent = options.Privacy.CollectUserAgent,
                BlockedAttributes = [.. options.Privacy.BlockedAttributes],
                RedactedAttributes = [.. options.Privacy.RedactedAttributes.Distinct(StringComparer.OrdinalIgnoreCase)],
            },
            Session = new()
            {
                InactivityTimeoutMinutes = browser.SessionInactivityTimeout.TotalMinutes,
                MaxDurationMinutes = browser.SessionMaxDuration.TotalMinutes,
            },
            Sampling = new() { SessionRatio = browser.SessionSampleRatio },
            Batch = new()
            {
                MaxQueueSize = browser.MaxQueueSize,
                MaxBatchSize = browser.MaxBatchSize,
                FlushIntervalMs = (int)Math.Max(250, browser.FlushInterval.TotalMilliseconds),
            },
            Persistence = new()
            {
                Enabled = options.Persistence.Enabled,
                MaxBytes = options.Persistence.BrowserMaxBytes,
                MaxAgeHours = options.Persistence.MaxAge.TotalHours,
                // Same fallback as the .NET export loop for a non-positive interval.
                RetryIntervalMs = options.Persistence.RetryInterval > TimeSpan.Zero
                    ? (int)Math.Min(int.MaxValue, Math.Max(1000, options.Persistence.RetryInterval.TotalMilliseconds))
                    : 60000,
            },
            MaxValueLength = Math.Max(0, options.MaxValueLength),
            Debug = browser.Debug,
        };

        if (options.ServiceNamespace is not null) config.Resource[BelemetrySemanticConventions.ServiceNamespace] = options.ServiceNamespace;
        foreach (var (key, value) in options.ResourceAttributes)
        {
            if (BelemetryAttributes.Normalize(value) is { } normalized) config.Resource[key] = normalized;
        }

        // Only a .NET pipeline running in the page exports through fetch. A server's exporters never do - and listing
        // them would publish internal collector addresses in every page; the script recognizes its own exporters' URLs
        // anyway. A WebAssembly client hands its list over when it starts the script (start merges ignoreUrls).
        if (settings.IsServer is false) AddExporterUrlsToIgnore(settings, config.Dependencies.IgnoreUrls);

        if (relay is false)
        {
            // The browser produces spans and logs only; its metrics-like records (web vitals) are log records.
            const BelemetrySignals browserSignals = BelemetrySignals.Traces | BelemetrySignals.Logs;

            foreach (var otlp in settings.OtlpExporters.Where(o => o.IncludeInBrowser && (o.Signals & browserSignals) != 0))
            {
                // A page cannot speak gRPC: such an exporter reaches the browser only through an OTLP/HTTP BrowserEndpoint.
                if (otlp.Protocol == BelemetryOtlpProtocol.Grpc && otlp.BrowserEndpoint is null) continue;

                var root = otlp.BrowserEndpoint ?? otlp.Endpoint;
                config.Exporters.Add(new()
                {
                    Type = "otlp",
                    Endpoint = root?.ToString(),
                    // A signal-specific endpoint only applies to the browser when the browser uses the same root.
                    TracesEndpoint = otlp.BrowserEndpoint is null && otlp.Signals.HasFlag(BelemetrySignals.Traces) ? otlp.TracesEndpoint?.ToString() : null,
                    LogsEndpoint = otlp.BrowserEndpoint is null && otlp.Signals.HasFlag(BelemetrySignals.Logs) ? otlp.LogsEndpoint?.ToString() : null,
                    Protocol = otlp.Protocol == BelemetryOtlpProtocol.HttpJson ? "http/json" : "http/protobuf",
                    Headers = BrowserHeaders(settings, otlp),
                    Compression = otlp.Compression == BelemetryCompression.Gzip ? "gzip" : "none",
                    Signals = (int)(otlp.Signals & browserSignals),
                });
            }

            foreach (var azure in settings.AzureMonitorExporters.Where(a => a.IncludeInBrowser && a.AccessTokenProvider is null && (a.Signals & browserSignals) != 0))
            {
                config.Exporters.Add(new()
                {
                    Type = "azureMonitor",
                    ConnectionString = azure.ConnectionString,
                    MaskIp = azure.MaskIp,
                    // The ingestion endpoint's CORS preflight allows Content-Encoding, so gzip works from the page too.
                    Compression = azure.Compression == BelemetryCompression.Gzip ? "gzip" : "none",
                    Signals = (int)(azure.Signals & browserSignals),
                });
            }

            if (options.ConsoleExporter is { IncludeInBrowser: true })
            {
                config.Exporters.Add(new() { Type = "console" });
            }
        }

        return config;
    }

    /// <summary>
    /// The headers the page sends to an OTLP exporter. A server renders this configuration into every page it serves,
    /// so its <see cref="BelemetryOtlpExporterOptions.Headers"/> - server configuration, often a real API key, or the
    /// Aspire dashboard's key from <c>OTEL_EXPORTER_OTLP_HEADERS</c> - stay on the server, and only the explicitly
    /// public <see cref="BelemetryOtlpExporterOptions.BrowserHeaders"/> go out. A WebAssembly or Hybrid app's own
    /// configuration is on the device already, so there both go.
    /// </summary>
    internal static Dictionary<string, string>? BrowserHeaders(BelemetrySettings settings, BelemetryOtlpExporterOptions otlp)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (settings.IsServer is false)
        {
            foreach (var (name, value) in otlp.Headers) headers[name] = value;
        }

        foreach (var (name, value) in otlp.BrowserHeaders) headers[name] = value;
        return headers.Count > 0 ? headers : null;
    }

    /// <summary>
    /// In WebAssembly .NET's HttpClient goes through <c>fetch</c>, and the exporter suppresses its own Activity, so its
    /// export calls carry no <c>traceparent</c>; without these entries the script's fetch instrumentation would trace
    /// every .NET export (in both transports).
    /// </summary>
    private static void AddExporterUrlsToIgnore(BelemetrySettings settings, List<string> ignoreUrls)
    {
        void Add(string? url)
        {
            if (string.IsNullOrEmpty(url)) return;
            var pattern = Regex.Escape(url);
            if (ignoreUrls.Contains(pattern) is false) ignoreUrls.Add(pattern);
        }

        foreach (var otlp in settings.OtlpExporters)
        {
            Add(BelemetryOtlpExporter.Resolve(otlp.TracesEndpoint, otlp.Endpoint, otlp.Protocol, BelemetrySignals.Traces));
            Add(BelemetryOtlpExporter.Resolve(otlp.LogsEndpoint, otlp.Endpoint, otlp.Protocol, BelemetrySignals.Logs));
            Add(BelemetryOtlpExporter.Resolve(otlp.MetricsEndpoint, otlp.Endpoint, otlp.Protocol, BelemetrySignals.Metrics));
            Add(otlp.BrowserEndpoint?.ToString());
        }

        foreach (var azure in settings.AzureMonitorExporters)
        {
            try
            {
                var connectionString = AzureMonitorConnectionString.Parse(azure.ConnectionString!);
                Add(connectionString.IngestionEndpoint.ToString());
                Add(connectionString.LiveEndpoint.ToString());
            }
            catch (Exception ex) when (ex is FormatException or ArgumentException)
            {
                // An invalid connection string has no exporter (and so no traffic) to ignore.
            }
        }
    }

    public static string ToJson(BelemetryBrowserConfig config) => JsonSerializer.Serialize(config, BelemetryJsonContext.Default.BelemetryBrowserConfig);

    public static JsonElement ToElement(BelemetryBrowserConfig config) => JsonSerializer.SerializeToElement(config, BelemetryJsonContext.Default.BelemetryBrowserConfig);
}
