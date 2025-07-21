//+:cnd:noEmit
using System.IO.Compression;
using System.Net;
using Boilerplate.Server.Shared;
using Boilerplate.Server.Shared.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

public static class WebApplicationBuilderExtensions
{
    public static TBuilder AddServerSharedServices<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        services.AddSharedProjectServices(configuration);

        builder.AddServiceDefaults();

        services.AddSingleton(sp =>
        {
            ServerSharedSettings settings = new();
            configuration.Bind(settings);
            return settings;
        });

        services.AddOutputCache(options =>
        {
            options.AddPolicy("AppResponseCachePolicy", policy =>
            {
                var builder = policy.AddPolicy<AppResponseCachePolicy>();
            }, excludeDefaultPolicy: true);
        });
        services.AddDistributedMemoryCache();

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

        //#if (appInsights == true)
        services.AddApplicationInsightsTelemetry(configuration);
        //#endif

        services.AddAntiforgery();

        services.AddAuthorization();

        return builder;
    }

    /// <summary>
    /// Also knows as AddServiceDefaults
    /// Adds common services for API: service discovery, resilience, health checks, and OpenTelemetry.
    /// This project should be referenced by each service project in your solution.
    /// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
    /// </summary>
    private static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.ConfigureHttpClient(httpClient =>
            {
                httpClient.DefaultRequestVersion = HttpVersion.Version20;
                httpClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
            });

            // Turn on resilience by default
            http.AddStandardResilienceHandler();
            // Turn on service discovery by default
            http.AddServiceDiscovery();

            http.UseSocketsHttpHandler((handler, sp) =>
            {
                handler.EnableMultipleHttp2Connections = true;
                handler.EnableMultipleHttp3Connections = true;
                handler.PooledConnectionLifetime = TimeSpan.FromMinutes(15);
                handler.AutomaticDecompression = DecompressionMethods.All;
                handler.SslOptions = new()
                {
                    EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13
                };
            });
        });

        return builder;
    }

    private static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddProcessor<AppOpenTelemetryProcessor>()
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation(options => options.Filter = (providerName, command) => command?.CommandText?.Contains("Hangfire") is false /* Ignore Hangfire */)
                    .AddHangfireInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }

    private static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }
}
