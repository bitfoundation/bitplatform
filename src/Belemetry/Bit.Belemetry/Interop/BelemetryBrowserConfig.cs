namespace Bit.Belemetry;

/// <summary>
/// The configuration handed to the browser script, serialized camelCase (the "Browser config" contract in CLAUDE.md).
/// </summary>
internal sealed class BelemetryBrowserConfig
{
    public required ServiceConfig Service { get; set; }
    public Dictionary<string, object?> Resource { get; set; } = [];
    public required string SdkVersion { get; set; }
    public string Transport { get; set; } = "direct";
    public List<ExporterConfig> Exporters { get; set; } = [];
    public int Features { get; set; }
    public required ConsentConfig Consent { get; set; }
    public required PageViewsConfig PageViews { get; set; }
    public required InteractionsConfig Interactions { get; set; }
    public required PerformanceConfig Performance { get; set; }
    public required DependenciesConfig Dependencies { get; set; }
    public required ErrorsConfig Errors { get; set; }
    public required LogsConfig Logs { get; set; }
    public required PrivacyConfig Privacy { get; set; }
    public required SessionConfig Session { get; set; }
    public required SamplingConfig Sampling { get; set; }
    public required BatchConfig Batch { get; set; }
    public required PersistenceConfig Persistence { get; set; }
    public bool Debug { get; set; }

    internal sealed class ServiceConfig
    {
        public required string Name { get; set; }
        public string? Version { get; set; }
        public string? Namespace { get; set; }
        public string? InstanceId { get; set; }
        public string? Environment { get; set; }
    }

    internal sealed class ExporterConfig
    {
        public required string Type { get; set; }
        public string? Endpoint { get; set; }
        public string? TracesEndpoint { get; set; }
        public string? LogsEndpoint { get; set; }
        public string? Protocol { get; set; }
        public Dictionary<string, string>? Headers { get; set; }
        public string? Compression { get; set; }
        /// <summary>The <see cref="BelemetrySignals"/> bits the browser sends (traces 1, logs 2); null means both.</summary>
        public int? Signals { get; set; }
        public string? ConnectionString { get; set; }
        public bool? MaskIp { get; set; }
    }

    internal sealed class ConsentConfig
    {
        public int Default { get; set; }
        public bool Persist { get; set; }
        public bool HonorGlobalPrivacyControl { get; set; }
        public int PendingLimit { get; set; }
    }

    internal sealed class PageViewsConfig
    {
        public bool TrackHashChanges { get; set; }
        public bool TrackVisitTime { get; set; }
    }

    internal sealed class InteractionsConfig
    {
        public List<string> Events { get; set; } = [];
        public bool CaptureText { get; set; }
        public int MaxTextLength { get; set; }
    }

    internal sealed class PerformanceConfig
    {
        public bool WebVitals { get; set; }
        public bool DocumentLoad { get; set; }
        public bool Resources { get; set; }
        public bool LongTasks { get; set; }
    }

    internal sealed class DependenciesConfig
    {
        public bool Fetch { get; set; }
        public bool Xhr { get; set; }
        public List<string> PropagateTraceContextTo { get; set; } = [];
        public List<string> IgnoreUrls { get; set; } = [];
    }

    internal sealed class ErrorsConfig
    {
        public List<string> IgnoreMessages { get; set; } = [];
    }

    internal sealed class LogsConfig
    {
        public bool CaptureConsole { get; set; }
        public List<string> ConsoleLevels { get; set; } = [];
    }

    internal sealed class PrivacyConfig
    {
        public List<string> RedactQueryParameters { get; set; } = [];
        public bool StripQueryString { get; set; }
        public bool StripFragment { get; set; }
        public bool ScrubPatterns { get; set; }
        public bool CollectUserAgent { get; set; }
        public List<string> BlockedAttributes { get; set; } = [];
    }

    internal sealed class SessionConfig
    {
        public double InactivityTimeoutMinutes { get; set; }
        public double MaxDurationMinutes { get; set; }
    }

    internal sealed class SamplingConfig
    {
        public double SessionRatio { get; set; }
    }

    internal sealed class BatchConfig
    {
        public int MaxQueueSize { get; set; }
        public int MaxBatchSize { get; set; }
        public int FlushIntervalMs { get; set; }
    }

    internal sealed class PersistenceConfig
    {
        public bool Enabled { get; set; }
        public long MaxBytes { get; set; }
        public double MaxAgeHours { get; set; }
        public int RetryIntervalMs { get; set; }
    }
}
