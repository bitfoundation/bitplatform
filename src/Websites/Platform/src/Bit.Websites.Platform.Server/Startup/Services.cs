using System.IO.Compression;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Bit.Websites.Platform.Server.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Extensions.AI;
using System.ClientModel.Primitives;
using Bit.Belemetry;

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

        // Add Azure Application Insights using Belemetry if connection string is configured in appsettings.json
        var appInsightsConnectionString = configuration["ApplicationInsights:ConnectionString"];
        if (string.IsNullOrWhiteSpace(appInsightsConnectionString) is false)
        {
            // Belemetry skips an exporter whose connection string it cannot parse and says so at Debug level only, which
            // nothing records on the server, so a broken one stops the startup here instead of silently sending nothing.
            var instrumentationKey = appInsightsConnectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Split('=', 2, StringSplitOptions.TrimEntries))
                .LastOrDefault(pair => pair.Length is 2 && string.Equals(pair[0], "InstrumentationKey", StringComparison.OrdinalIgnoreCase))?[1];
            if (Guid.TryParse(instrumentationKey, out _) is false)
                throw new InvalidOperationException("ApplicationInsights:ConnectionString has no valid InstrumentationKey.");

            services.AddBitBelemetryServices(options =>
            {
                // service.name (cloud_RoleName) comes from the OTEL_SERVICE_NAME environment variable, else the entry assembly name.
                options.Environment = env.EnvironmentName;
                // On an ASP.NET Core host Belemetry only serves its browser script unless its .NET pipeline is turned on.
                options.Server.CollectDotnetTelemetry = true;
                // Batches Application Insights could not take are kept on disk and sent once it is back, instead of being lost
                // with the outage: in {LocalApplicationData}/bit-belemetry/{service.name}, or Belemetry:Persistence:Directory.
                options.Server.Features |= BelemetryFeatures.Persistence;
                // Blazor's framework files and package assets would be a request span each, and the Blazor circuit and the
                // SignalR hub one request as long as the connection, which would skew every response time chart.
                options.Server.IgnoredRequestPaths.AddRange(["/_framework", "/_content", "/_blazor", "/app-hub"]);
                // Blazor's per-event spans and its and the runtime's meters, which Application Insights never got before.
                // Removed in code, as a list bound from configuration can only add to them.
                options.Traces.Sources.Remove("Microsoft.AspNetCore.Components*");
                options.Metrics.Meters.RemoveAll(meter => meter is "Microsoft.AspNetCore.Components*" or "System.Runtime");

                // appsettings.json is server-owned, so the Belemetry section there tunes all of the above without a redeploy,
                // e.g. Traces:SamplingRatio, which is 1.0 here: the exporter does not scale the counts Application Insights
                // shows by it, so a lower ratio lowers the request and failure counts too.
                configuration.GetSection("Belemetry").Bind(options);

                options.AddAzureMonitorExporter(appInsightsConnectionString, azure => azure.EnableLiveMetrics = true);
                options.AddProcessor(new TelegramBotTokenRedactor());
            });
        }

        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen();

        services.AddHealthChecks(configuration);
    }
}
