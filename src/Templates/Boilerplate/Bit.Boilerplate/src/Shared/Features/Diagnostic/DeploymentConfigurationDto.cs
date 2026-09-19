//+:cnd:noEmit
namespace Boilerplate.Shared.Features.Diagnostic;

/// <summary>
/// Effective settings of this deployment that no health check reports, never a secret: a credential is a boolean saying
/// whether it is set. Under scale-out one instance answers; what holds for that process alone is grouped behind
/// <see cref="InstanceName"/>.
/// </summary>
public class DeploymentConfigurationDto
{
    /// <summary>The instance that answered: the values below are read from that process, not from the deployment.</summary>
    public string? InstanceName { get; set; }

    public string? Environment { get; set; }

    /// <summary>Of the answering instance alone - a rolling deployment leaves the others on the old one.</summary>
    public string? ApplicationVersion { get; set; }

    /// <summary>The operating system's, on the answering instance.</summary>
    public string? TimeZone { get; set; }

    /// <summary>The .NET the process runs on.</summary>
    public string? Runtime { get; set; }

    public string? OperatingSystem { get; set; }

    public int ProcessorCount { get; set; }

    /// <summary>What the runtime may use: in a container its limit, not the host's memory.</summary>
    public long AvailableMemoryBytes { get; set; }

    /// <summary>The process's working set the moment the report was read.</summary>
    public long UsedMemoryBytes { get; set; }

    /// <summary>Since the process started, which a deployment, a crash or an idle shutdown all reset.</summary>
    public TimeSpan Uptime { get; set; }

    /// <summary>Empty when the server runs culture-invariant.</summary>
    public string[] SupportedCultures { get; set; } = [];

    /// <summary>The origins CORS accepts, which is also what the forwarded host is checked against.</summary>
    public string[] TrustedOrigins { get; set; } = [];

    /// <summary>Trusted on top of <see cref="TrustedOrigins"/>, so an empty list is not the whole answer.</summary>
    public string? TrustedOriginsRegex { get; set; }

    /// <summary>Without it X-Forwarded-* is ignored: every client ip is the proxy's, and links carry the internal host.</summary>
    public bool ForwardedHeadersConfigured { get; set; }

    public bool OutputCachingEnabled { get; set; }

    public bool CdnEdgeCachingEnabled { get; set; }

    /// <summary>Null where the process renders nothing, which a standalone api does not.</summary>
    public string? BlazorMode { get; set; }

    public bool PrerenderEnabled { get; set; }

    public bool RequireConfirmedAccount { get; set; }

    public int MaxPrivilegedSessionsCount { get; set; }

    public TimeSpan AccessTokenLifetime { get; set; }

    public TimeSpan RefreshTokenLifetime { get; set; }

    /// <summary>In-memory Hangfire storage: jobs do not survive a restart, and no other instance sees them.</summary>
    public bool BackgroundJobsUseIsolatedStorage { get; set; }

    public TimeSpan BackgroundJobExpiration { get; set; }

    //#if (sentry == true)
    public bool SentryConfigured { get; set; }
    //#endif

    //#if (appInsights == true)
    public bool AzureMonitorConfigured { get; set; }
    //#endif

    public bool OtlpExporterConfigured { get; set; }

    //#if (notification == true)
    /// <summary>Web push signs its own messages, so nothing else says whether the browser channel can work.</summary>
    public bool WebPushConfigured { get; set; }
    //#endif

    public long AttachmentUploadSizeLimitBytes { get; set; }

    //#if (signalR == true)
    public long? HubMaximumReceiveMessageSize { get; set; }

    public TimeSpan AiChatImagesRetention { get; set; }
    //#endif

    /// <summary>Below these, a client is told to update before it may go on (See ForceUpdate).</summary>
    public string? MinimumSupportedAndroidAppVersion { get; set; }

    public string? MinimumSupportedIosAppVersion { get; set; }

    public string? MinimumSupportedMacOSAppVersion { get; set; }

    public string? MinimumSupportedWindowsAppVersion { get; set; }

    public string? MinimumSupportedWebAppVersion { get; set; }
}
