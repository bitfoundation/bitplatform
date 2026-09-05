//+:cnd:noEmit
using System.ComponentModel;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ModelContextProtocol.Server;
using Boilerplate.Server.Api.Features.Attachments;

namespace Boilerplate.Server.Api.Infrastructure.DevMcp;

[Authorize(Policy = AppFeatures.System.DevMcp)]
public sealed class DevMcpConfigurationTools(
    ServerApiSettings settings,
    IConfiguration configuration,
    IHostEnvironment environment,
    TimeProvider timeProvider,
    HealthCheckService healthCheckService)
{
    [McpServerTool(Name = nameof(GetEffectiveConfiguration))]
    [Description("Returns the effective configuration of this running process, not the contents of a file on disk. Secrets are never returned: identity-provider, SMS, push, recaptcha, AI, SMTP, Cloudflare, Application Insights and Sentry values are booleans or names only. Query filters, Hangfire job arguments and database rows are not part of this tool. WebAppRender is absent when this process is a standalone API and does not host Blazor.")]
    public string GetEffectiveConfiguration()
    {
        var identity = settings.Identity;
        var cacheDefaults = new AppResponseCacheAttribute();

        return DevMcpJson.Serialize(new
        {
            Hosting = new
            {
                environment.EnvironmentName,
                ApplicationVersion = typeof(Program).Assembly.GetName().Version?.ToString(),
                settings.TrustedOrigins,
                ForwardedHeaders = ReadForwardedHeaders(),
                SupportedCultures = CultureInfoManager.InvariantGlobalization
                    ? Array.Empty<string>()
                    : CultureInfoManager.SupportedCultures.Select(c => c.Culture.Name).ToArray(),
                UtcNow = timeProvider.GetUtcNow(),
                TimeZone = TimeZoneInfo.Local.Id
            },
            Rendering = ReadRendering(),
            Caching = new
            {
                settings.ResponseCaching?.EnableOutputCaching,
                settings.ResponseCaching?.EnableCdnEdgeCaching,
                CloudflareZoneConfigured = ReadCloudflareConfigured(),
                AppResponseCacheDefaults = new
                {
                    cacheDefaults.MaxAge,
                    cacheDefaults.SharedMaxAge,
                    cacheDefaults.SkipOutputCache,
                    cacheDefaults.UserAgnostic,
                    cacheDefaults.CacheTagTemplate
                }
            },
            Identity = new
            {
                identity.MaxPrivilegedSessionsCount,
                identity.SignIn.RequireConfirmedAccount,
                UnconfirmedUsersRetention = identity.UnconfirmedUsersRetention.ToString(),
                AccessTokenLifetime = identity.BearerTokenExpiration.ToString(),
                RefreshTokenLifetime = identity.RefreshTokenExpiration.ToString(),
                identity.Issuer,
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
    [Description("Runs the same health checks as GET /health and returns per-check status and duration. A Degraded check is still HTTP 200 on /health and does not mean the process is out of rotation. Exception details are omitted so connection strings and tokens cannot leak.")]
    public async Task<string> GetHealth(CancellationToken cancellationToken)
    {
        var report = await healthCheckService.CheckHealthAsync(cancellationToken);

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
                entry.Value.Tags
            })
        });
    }

    private object? ReadRendering()
    {
        var section = configuration.GetSection("WebAppRender");
        if (section.Exists() is false)
            return null;

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
            SpeechUploadSizeLimitBytes = ChatbotController.MaxSpeechUploadSizeBytes,
            //#endif
            RateLimits = new object[]
            {
                new { Policy = RateLimitOptionsExtensions.IDENTITY, PermitLimit = RateLimitOptionsExtensions.IdentityPermitLimit, Window = RateLimitOptionsExtensions.Window.ToString(), Partition = "user-or-ip" },
                //#if (signalR == true)
                new { Policy = RateLimitOptionsExtensions.SPEECH, PermitLimit = RateLimitOptionsExtensions.SpeechPermitLimit, Window = RateLimitOptionsExtensions.Window.ToString(), Partition = "user-or-ip" },
                new { Policy = RateLimitOptionsExtensions.SPEECH_GLOBAL_IP, PermitLimit = RateLimitOptionsExtensions.SpeechGlobalIpPermitLimit, Window = RateLimitOptionsExtensions.Window.ToString(), Partition = "ip" }
                //#endif
            }
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
