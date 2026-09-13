//+:cnd:noEmit
using System.ComponentModel;
using Boilerplate.Server.Api.Features.Diagnostic;
using Boilerplate.Server.Api.Features.Attachments;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Boilerplate.Server.Api.Infrastructure.DevMcp;

[Authorize(Policy = AppFeatures.System.DevMcp)]
public partial class DevMcpDiagnosticTools
{
    [AutoInject] private ServerApiSettings settings = default!;
    [AutoInject] private IConfiguration configuration = default!;
    [AutoInject] private IHostEnvironment environment = default!;
    [AutoInject] private TimeProvider timeProvider = default!;
    [AutoInject] private HealthCheckService healthCheckService = default!;
    [AutoInject] private IHttpContextAccessor httpContextAccessor = default!;
    [AutoInject] private ServerDiagnosticService diagnostic = default!;
    [AutoInject] private ILogger<DevMcpDiagnosticTools> logger = default!;

    /// <summary>Unguarded on purpose: nothing reaches a tool here without the endpoint's global-admin + 2FA authorization.</summary>
    private Guid CallerId => httpContextAccessor.HttpContext!.User.GetUserId();

    [McpServerTool(Name = nameof(GetDiagnosticReport))]
    [Description("Returns, as plain text, what the server makes of this very call: the client ip it resolved (the edge's address when a CDN is in front and its network is not trusted), whether the call arrived through the CDN, the protocol and endpoint, the trace id, who the caller is, the culture, every request header, and the environment/base url/web app url/server clock. It is the same report the anonymous /diagnostic page shows and the same one AppHub's GetDiagnosticReport returns over the websocket, so the three can be compared - which is the point when a proxy resolves a different client ip per path. Headers arrive as they were sent except the two that are credentials: Authorization is reduced to its scheme and Cookie to its cookie names, so no token or session value is in the answer. Nothing about other users, other requests or the database is in it either. For configuration rather than this request, use GetDeploymentInfo.")]
    public string GetDiagnosticReport()
    {
        return diagnostic.BuildReport(DiagnosticReportSource.DevMcp);
    }

    [McpServerTool(Name = nameof(GetDeploymentInfo))]
    [Description("Returns how this process is actually running: its effective configuration, not the contents of a file on disk. Secrets are never returned: identity-provider, SMS, push, recaptcha, AI, SMTP, Cloudflare, Application Insights and Sentry values are booleans or names only. Nothing about the current request is here - the client ip, the headers it arrived with and the per-request WebAppUrl belong to GetDiagnosticReport. Query filters, Hangfire job arguments and database rows are not part of this tool either. Rendering is absent unless the API is integrated with the web app: a standalone API serves no Blazor.")]
    public string GetDeploymentInfo()
    {
        var identity = settings.Identity;

        return DevMcpJson.Serialize(new
        {
            Hosting = new
            {
                environment.EnvironmentName,
                ApplicationVersion = typeof(Program).Assembly.GetName().Version?.ToString(),
                settings.TrustedOrigins,
                ForwardedHeaders = ReadForwardedHeaders(),
                SupportedCultures = CultureInfoManager.InvariantGlobalization
                    ? []
                    : CultureInfoManager.SupportedCultures.Select(c => c.Culture.Name).ToArray(),
                UtcNow = timeProvider.GetUtcNow(),
                TimeZone = TimeZoneInfo.Local.Id
            },
            //#if (api == "Integrated")
            Rendering = ReadRendering(),
            //#endif
            Caching = new
            {
                settings.ResponseCaching?.EnableOutputCaching,
                settings.ResponseCaching?.EnableCdnEdgeCaching,
                CloudflareZoneConfigured = ReadCloudflareConfigured()
            },
            Identity = new
            {
                identity.MaxPrivilegedSessionsCount,
                identity.SignIn.RequireConfirmedAccount,
                UnconfirmedUsersRetention = identity.UnconfirmedUsersRetention.ToString(),
                AccessTokenLifetime = identity.BearerTokenExpiration.ToString(),
                RefreshTokenLifetime = identity.RefreshTokenExpiration.ToString(),
                // Derived from the request, so this is what every token minted on this deployment carries.
                Issuer = httpContextAccessor.HttpContext?.Request.GetIssuer(),
                identity.Audience
            },
            BackgroundJobs = new
            {
                settings.Hangfire?.UseIsolatedStorage,
                JobExpiration = settings.Hangfire?.JobExpiration.ToString()
            },
            RetentionAndLimits = ReadRetentionAndLimits(),
            AI = ReadAi(),
            Capabilities = ReadCapabilities(),
            ForceUpdate = new
            {
                settings.SupportedAppVersions?.MinimumSupportedAndroidAppVersion,
                settings.SupportedAppVersions?.MinimumSupportedIosAppVersion,
                settings.SupportedAppVersions?.MinimumSupportedMacOSAppVersion,
                settings.SupportedAppVersions?.MinimumSupportedWindowsAppVersion,
                settings.SupportedAppVersions?.MinimumSupportedWebAppVersion
            }
        });
    }

    [McpServerTool(Name = nameof(GetHealth))]
    [Description("Runs the same health checks as GET /health and returns per-check status, duration, description, tags, the check's own diagnostic data and, for anything that did not report Healthy, the full exception including its inner exceptions and stack trace. Nothing here is redacted: a database or storage check's exception can carry a connection string, which is the point - the failure is what you came for - and it is why the whole endpoint is global-admin only and every unhealthy read is logged. A Degraded check is still HTTP 200 on /health and does not mean the process is out of rotation.")]
    public async Task<string> GetHealth(CancellationToken cancellationToken)
    {
        var report = await healthCheckService.CheckHealthAsync(cancellationToken);

        var unhealthy = report.Entries.Where(entry => entry.Value.Status is not HealthStatus.Healthy).Select(entry => entry.Key).ToArray();

        // A failing check's exception is unredacted and may carry a connection string, so who read it is worth a line.
        if (unhealthy.Length > 0)
        {
            logger.LogInformation("Dev MCP read {Status} health for {UserId}. Not healthy: {Checks}.", report.Status, CallerId, unhealthy);
        }

        return DevMcpJson.Serialize(new
        {
            Status = report.Status.ToString(),
            TotalDuration = report.TotalDuration,
            Checks = report.Entries.Select(entry => new
            {
                Name = entry.Key,
                Status = entry.Value.Status.ToString(),
                entry.Value.Duration,
                entry.Value.Description,
                entry.Value.Tags,
                // ToString() rather than the message alone: the inner exception is usually the one naming the cause.
                Exception = entry.Value.Exception?.ToString(),
                // Stringified because a check may put anything in here, and one unserializable value would fail the call.
                Data = entry.Value.Data.Count is 0 ? null : entry.Value.Data.ToDictionary(item => item.Key, item => item.Value?.ToString())
            })
        });
    }

    //#if (api == "Integrated")
    private object ReadRendering()
    {
        var section = configuration.GetSection("WebAppRender");
        var blazorMode = section["BlazorMode"];
        var prerenderEnabled = section.GetValue<bool>("PrerenderEnabled");
        return new
        {
            BlazorMode = blazorMode,
            PrerenderEnabled = prerenderEnabled,
            RenderMode = RenderModeName(blazorMode, prerenderEnabled)
        };
    }

    private static string? RenderModeName(string? blazorMode, bool prerenderEnabled)
        => blazorMode switch
        {
            "BlazorSsr" => null,
            "BlazorServer" => $"InteractiveServer (prerender: {prerenderEnabled})",
            "BlazorWebAssembly" => $"InteractiveWebAssembly (prerender: {prerenderEnabled})",
            "BlazorAuto" => $"InteractiveAuto (prerender: {prerenderEnabled})",
            _ => blazorMode
        };
    //#endif

    private object ReadForwardedHeaders()
    {
        var section = configuration.GetSection("ForwardedHeaders");
        return new
        {
            Configured = section.Exists(),
            ForwardedHeaders = section["ForwardedHeaders"],
            ForwardedHostHeaderName = section["ForwardedHostHeaderName"],
            AllowedHosts = section.GetSection("AllowedHosts").Get<string[]>() ?? [],
            KnownProxies = section.GetSection("KnownProxies").Get<string[]>() ?? [],
            KnownIPNetworks = section.GetSection("KnownIPNetworks").Get<string[]>() ?? []
        };
    }

    private object ReadRetentionAndLimits()
    {
        return new
        {
            //#if (signalR == true)
            AiChatImagesRetention = settings.AiChatImagesRetention.ToString(),
            HubMaximumReceiveMessageSize = configuration.GetValue<long?>("HubOptions:MaximumReceiveMessageSize"),
            //#endif
            AttachmentUploadSizeLimitBytes = AttachmentController.MaxUploadSizeBytes,
            //#if (signalR == true)
            SpeechUploadSizeLimitBytes = ChatbotController.MaxSpeechUploadSizeBytes
            //#endif
        };
    }

    private object? ReadAi()
    {
        //#if (signalR == true || database == "PostgreSQL" || database == "SqlServer")
        var ai = settings.AI;
        return new
        {
            Chat = new { Model = ai?.OpenAI?.ChatModel, EndpointConfigured = ai?.OpenAI?.ChatEndpoint is not null, KeyConfigured = string.IsNullOrWhiteSpace(ai?.OpenAI?.ChatApiKey) is false },
            Embedding = new { Model = ai?.OpenAI?.EmbeddingModel, EndpointConfigured = ai?.OpenAI?.EmbeddingEndpoint is not null || string.IsNullOrWhiteSpace(ai?.HuggingFace?.EmbeddingEndpoint) is false, KeyConfigured = string.IsNullOrWhiteSpace(ai?.OpenAI?.EmbeddingApiKey) is false || string.IsNullOrWhiteSpace(ai?.HuggingFace?.EmbeddingApiKey) is false },
            //#if (signalR == true)
            SpeechToText = new { Model = ai?.OpenAI?.SpeechToTextModel, EndpointConfigured = ai?.OpenAI?.SpeechToTextEndpoint is not null, KeyConfigured = string.IsNullOrWhiteSpace(ai?.OpenAI?.SpeechToTextApiKey) is false },
            TextToSpeech = new { Model = ai?.OpenAI?.TextToSpeechModel, EndpointConfigured = ai?.OpenAI?.TextToSpeechEndpoint is not null, KeyConfigured = string.IsNullOrWhiteSpace(ai?.OpenAI?.TextToSpeechApiKey) is false, Voice = ai?.OpenAI?.TextToSpeechVoice },
            //#endif
            //#if (database == "PostgreSQL" || database == "SqlServer")
            EmbeddingGenerationEnabledOnDbContext = AppDbContext.IsEmbeddingEnabled
            //#endif
        };
        //#else
        return null;
        //#endif
    }

    private object ReadCapabilities()
    {
        return new
        {
            TwilioSms = settings.Sms?.Configured is true,
            //#if (notification == true)
            Firebase = string.IsNullOrWhiteSpace(settings.AdsPushFirebase?.PrivateKey) is false,
            Apns = string.IsNullOrWhiteSpace(settings.AdsPushAPNS?.P8PrivateKey) is false,
            WebPushVapid = string.IsNullOrWhiteSpace(settings.AdsPushVapid?.PrivateKey) is false,
            //#endif
            //#if (captcha == "reCaptcha")
            Recaptcha = string.IsNullOrWhiteSpace(settings.GoogleRecaptchaSecretKey) is false,
            //#endif
            //#if (appInsights == true)
            // What is actually wired is the Azure Monitor OpenTelemetry exporter, off the same connection string the
            // client's Application Insights JS SDK binds (See AddOpenTelemetryExporters).
            AzureMonitorExporter = string.IsNullOrWhiteSpace(configuration["ApplicationInsights:ConnectionString"]) is false,
            //#endif
            OtlpExporter = string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]) is false
                           || string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_LOGS_ENDPOINT"]) is false,
            Sentry = string.IsNullOrWhiteSpace(configuration["Logging:Sentry:Dsn"]) is false,
            Smtp = string.IsNullOrWhiteSpace(configuration.GetConnectionString("smtp")) is false,
            Google = string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientId"]) is false,
            GitHub = string.IsNullOrWhiteSpace(configuration["Authentication:GitHub:ClientId"]) is false,
            Twitter = string.IsNullOrWhiteSpace(configuration["Authentication:Twitter:ConsumerKey"]) is false,
            Apple = string.IsNullOrWhiteSpace(configuration["Authentication:Apple:ClientId"]) is false,
            Facebook = string.IsNullOrWhiteSpace(configuration["Authentication:Facebook:AppId"]) is false,
            Keycloak = string.IsNullOrWhiteSpace(configuration["KEYCLOAK_HTTP"] ?? configuration["Authentication:Keycloak:KeycloakUrl"]) is false,
            AzureAD = string.IsNullOrWhiteSpace(configuration["Authentication:AzureAD:ClientId"]) is false
        };
    }

    private bool ReadCloudflareConfigured()
    {
        //#if (cloudflare == true)
        return settings.Cloudflare?.Configured is true;
        //#else
        return false;
        //#endif
    }
}
