using System.IO.Compression;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Bit.Websites.Platform.Server.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Extensions.AI;
using System.ClientModel.Primitives;
using Azure.Monitor.OpenTelemetry.AspNetCore;

namespace Bit.Websites.Platform.Server.Startup;

public static class Services
{
    public static void Add(IServiceCollection services, IWebHostEnvironment env, IConfiguration configuration)
    {
        // Services being registered here can get injected into controllers and services in Server project.

        AppSettings appSettings = new();

        configuration.GetSection(nameof(AppSettings)).Bind(appSettings);

        services.AddHttpClient<TelegramBotApiClient>();
        services.AddScoped<TelegramBotService>();

        services.AddClientSharedServices();

        services.AddExceptionHandler<ApiExceptionHandler>();

        services.AddBlazor(configuration);

        services
            .AddControllers();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy("MessageSubmit", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(5)
                    }));

            // Returns the error in the same shape as ApiExceptionHandler (Request-ID header + RestErrorInfo body),
            // so the client turns it into TooManyRequestsExceptions (a KnownException) instead of retrying the POST
            // and surfacing a generic unknown-error message.
            options.OnRejected = async (context, cancellationToken) =>
            {
                var response = context.HttpContext.Response;

                response.Headers.Append("Request-ID", context.HttpContext.TraceIdentifier);

                await response.WriteAsJsonAsync(new RestErrorInfo
                {
                    Key = nameof(TooManyRequestsExceptions),
                    Message = "You have sent too many messages. Please try again in a few minutes.",
                    ExceptionType = typeof(TooManyRequestsExceptions).FullName
                }, AppJsonContext.Default.RestErrorInfo, cancellationToken: cancellationToken);
            };
        });

        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = env.IsDevelopment();
        });

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.All;
            options.ForwardedHostHeaderName = "X-Host";
            // The site runs behind a reverse proxy/CDN that is not on loopback; with the default
            // loopback-only trust lists, UseForwardedHeaders ignores X-Forwarded-For and
            // RemoteIpAddress stays the proxy address, collapsing the rate limiter above
            // into a single partition shared by all visitors.
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        if (string.IsNullOrEmpty(appSettings?.OpenAI?.ChatApiKey) is false)
        {
            // https://github.com/dotnet/extensions/tree/main/src/Libraries/Microsoft.Extensions.AI.OpenAI#microsoftextensionsaiopenai
            services.AddChatClient(sp => new OpenAI.Chat.ChatClient(model: appSettings.OpenAI.ChatModel, credential: new(appSettings.OpenAI.ChatApiKey), options: new()
            {
                Endpoint = appSettings.OpenAI.ChatEndpoint,
                Transport = new HttpClientPipelineTransport(sp.GetRequiredService<IHttpClientFactory>().CreateClient("AI"))
            }).AsIChatClient())
            .UseLogging()
            .UseFunctionInvocation()
            .UseDistributedCache();
        }

        services.AddDistributedMemoryCache();

        services.AddResponseCaching();

        services.AddHttpContextAccessor();

        services.AddResponseCompression(opts =>
        {
            opts.EnableForHttps = true;
            opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/octet-stream"]).ToArray();
            opts.Providers.Add<BrotliCompressionProvider>();
            opts.Providers.Add<GzipCompressionProvider>();
        })
            .Configure<BrotliCompressionProviderOptions>(opt => opt.Level = CompressionLevel.Fastest)
            .Configure<GzipCompressionProviderOptions>(opt => opt.Level = CompressionLevel.Fastest);

        services.Configure<AppSettings>(configuration.GetSection(nameof(AppSettings)));

        services.AddTransient(sp => sp.GetRequiredService<IOptionsSnapshot<AppSettings>>().Value);

        // Add Azure Application Insights using OpenTelemetry if connection string is configured in appsettings.json
        var appInsightsConnectionString = configuration["ApplicationInsights:ConnectionString"];
        if (string.IsNullOrWhiteSpace(appInsightsConnectionString) is false)
        {
            services.AddOpenTelemetry().UseAzureMonitor(options =>
            {
                options.ConnectionString = appInsightsConnectionString;
            }).WithLogging(configureBuilder: null, configureOptions: options =>
            {
                options.IncludeScopes = true;
                options.IncludeFormattedMessage = true;
            });
        }

        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen();

        services.AddHealthChecks(env, configuration);
    }
}
