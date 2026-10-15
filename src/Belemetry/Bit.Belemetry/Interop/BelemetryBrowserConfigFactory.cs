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
            PageViews = new() { TrackHashChanges = browser.TrackHashChanges },
            Interactions = new()
            {
                Events = [.. browser.InteractionEvents],
                CaptureText = browser.CaptureInteractionText,
                MaxTextLength = browser.MaxInteractionTextLength,
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
            Errors = new() { IgnoreMessages = [.. browser.IgnoreErrorMessages] },
            Logs = new() { CaptureConsole = browser.CaptureConsole, ConsoleLevels = [.. browser.ConsoleLevels] },
            Privacy = new()
            {
                RedactQueryParameters = [.. options.Privacy.RedactQueryParameters],
                StripQueryString = options.Privacy.StripQueryString,
                StripFragment = options.Privacy.StripFragment,
                ScrubPatterns = options.Privacy.ScrubPatterns,
                CollectUserAgent = options.Privacy.CollectUserAgent,
                BlockedAttributes = [.. options.Privacy.BlockedAttributes],
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
            Debug = browser.Debug,
        };

        if (options.ServiceNamespace is not null) config.Resource[BelemetrySemanticConventions.ServiceNamespace] = options.ServiceNamespace;
        foreach (var (key, value) in options.ResourceAttributes)
        {
            if (BelemetryAttributes.Normalize(value) is { } normalized) config.Resource[key] = normalized;
        }

        AddExporterUrlsToIgnore(settings, config.Dependencies.IgnoreUrls);

        if (relay is false)
        {
            // The browser produces spans and logs only; its metrics-like records (web vitals) are log records.
            const BelemetrySignals browserSignals = BelemetrySignals.Traces | BelemetrySignals.Logs;

            foreach (var otlp in settings.OtlpExporters.Where(o => o.IncludeInBrowser && (o.Signals & browserSignals) != 0))
            {
                var root = otlp.BrowserEndpoint ?? otlp.Endpoint;
                config.Exporters.Add(new()
                {
                    Type = "otlp",
                    Endpoint = root?.ToString(),
                    // A signal-specific endpoint only applies to the browser when the browser uses the same root.
                    TracesEndpoint = otlp.BrowserEndpoint is null && otlp.Signals.HasFlag(BelemetrySignals.Traces) ? otlp.TracesEndpoint?.ToString() : null,
                    LogsEndpoint = otlp.BrowserEndpoint is null && otlp.Signals.HasFlag(BelemetrySignals.Logs) ? otlp.LogsEndpoint?.ToString() : null,
                    Protocol = otlp.Protocol == BelemetryOtlpProtocol.HttpJson ? "http/json" : "http/protobuf",
                    Headers = otlp.Headers.Count > 0 ? new Dictionary<string, string>(otlp.Headers) : null,
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
            Add(BelemetryOtlpExporter.Resolve(otlp.TracesEndpoint, otlp.Endpoint, "v1/traces"));
            Add(BelemetryOtlpExporter.Resolve(otlp.LogsEndpoint, otlp.Endpoint, "v1/logs"));
            Add(BelemetryOtlpExporter.Resolve(otlp.MetricsEndpoint, otlp.Endpoint, "v1/metrics"));
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
