//+:cnd:noEmit
using System.ComponentModel;
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

    /// <summary>
    /// Allow listed rather than deny listed: Authorization and Cookie are on the same request, and a deny list is one
    /// forgotten entry away from returning them.
    /// </summary>
    private static readonly string[] forwardingHeaderNames =
    [
        "Host", "Origin", "X-Origin", "Forwarded", "CDN-Loop",
        "X-Forwarded-For", "X-Forwarded-Proto", "X-Forwarded-Host", "X-Forwarded-Port", "X-Forwarded-Prefix",
        "X-Original-Host", "X-Original-Proto", "X-Original-URL",
        "CF-Connecting-IP", "CF-IPCountry", "CF-Ray", "CF-Visitor",
        "X-App-Version", "X-App-Platform"
    ];

    [McpServerTool(Name = nameof(GetDeploymentInfo))]
    [Description("Returns how this process is actually running: its effective configuration - not the contents of a file on disk - and what the current request shows about how traffic reaches it. Request carries the base url, the per-request WebAppUrl that ends up in the links the server mails, whether the call arrived through the CDN, and the forwarding/CDN headers it received; those headers are allow-listed, so Authorization and cookies are never among them. Other secrets are never returned either: identity-provider, SMS, push, recaptcha, AI, SMTP, Cloudflare, Application Insights and Sentry values are booleans or names only. Query filters, Hangfire job arguments and database rows are not part of this tool. Rendering is absent unless the API is integrated with the web app: a standalone API serves no Blazor.")]
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
            Request = ReadRequest(),
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

    /// <summary>
    /// What this very call shows about how requests reach the process: the readable half of the anonymous
    /// /api/v1/Diagnostic/PerformDiagnostic endpoint, without its side effects (it sends a test push and a test
    /// SignalR message) and without the headers that carry credentials.
    /// </summary>
    private object? ReadRequest()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
            return null;

        var request = httpContext.Request;

        string? webAppUrl;
        try
        {
            webAppUrl = request.GetWebAppUrl().ToString();
        }
        catch (BadRequestException exception)
        {
            webAppUrl = exception.Message;
        }

        return new
        {
            BaseUrl = request.GetBaseUrl().ToString(),
            WebAppUrl = webAppUrl,
            IsFromCDN = request.IsFromCDN(),
            Culture = CultureInfo.CurrentCulture.Name,
            UICulture = CultureInfo.CurrentUICulture.Name,
            //#if (multitenant == true)
            // Null for a global admin who never switched into a tenant - which is why QueryEntity reads with IgnoreQueryFilters.
            TenantIdClaim = httpContext.User.IsAuthenticated() ? httpContext.User.GetTenantId() : null,
            //#endif
            ReceivedHeaders = forwardingHeaderNames
                .Where(request.Headers.ContainsKey)
                .ToDictionary(name => name, name => request.Headers[name].ToString())
        };
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
