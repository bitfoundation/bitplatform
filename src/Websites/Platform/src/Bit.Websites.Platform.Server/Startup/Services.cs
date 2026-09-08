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
using ModelContextProtocol.Protocol;

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

        AddMcpServer(services);

        services.AddHostedService<CodebaseMemoryIndexService>();

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.All;
            options.ForwardedHostHeaderName = "X-Host";
            // The site runs behind a reverse proxy/CDN that is not on loopback; with the default
            // loopback-only trust lists, UseForwardedHeaders ignores X-Forwarded-For and
            // RemoteIpAddress stays the proxy address, collapsing the rate limiter above
            // into a single partition shared by all visitors.
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
            // With the trust lists cleared, only the RIGHTMOST X-Forwarded-For entry is consumed, which
            // the front end appends itself; anything a client puts in the header stays to its left and is
            // never read. Raising this limit would let a client pick its own rate-limiter partition, so it
            // is pinned here rather than left at the (currently identical) default.
            options.ForwardLimit = 1;
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

    /// <summary>
    /// Serves every tool of every MCP server listed in the repository's .mcp.json from this site's own
    /// /mcp endpoint (mapped in <see cref="Middlewares"/>), so that an agent pointed at bitplatform.dev/mcp
    /// gets all of them from a single connection. <see cref="McpProxyService"/> does the forwarding.
    /// </summary>
    private static void AddMcpServer(IServiceCollection services)
    {
        // Singleton: it keeps one session per upstream server, shared by every caller.
        services.AddSingleton<McpProxyService>();

        services.AddMcpServer(options =>
        {
            options.ServerInfo = new()
            {
                Name = "bitplatform",
                Title = "bit platform",
                Version = typeof(Services).Assembly.GetName().Version!.ToString()
            };
            options.ServerInstructions = "Provides the tools of every MCP server the bit platform team develops against, including the bit BlazorUI, Brouter, Butil, Bswup and Motion documentation servers.";
        })
            // Stateless: no session state is kept between requests, so the endpoint keeps working when the
            // site runs behind a load balancer without session affinity. Nothing is lost by it here, since
            // the proxied tools are documentation lookups that never call back into the client.
            .WithHttpTransport(options => options.Stateless = true)
            .WithListToolsHandler(async (request, cancellationToken) => new ListToolsResult
            {
                Tools = [.. await request.Services!.GetRequiredService<McpProxyService>().ListTools(cancellationToken)]
            })
            .WithCallToolHandler((request, cancellationToken) =>
                request.Services!.GetRequiredService<McpProxyService>().CallTool(request.Params!, cancellationToken));
    }
}
