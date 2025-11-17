//+:cnd:noEmit
using System.IO.Compression;
using System.Net;
//#if (appInsights == true)
using Azure.Monitor.OpenTelemetry.Profiler;
using Azure.Monitor.OpenTelemetry.AspNetCore;
//#endif
using Boilerplate.Server.Shared;
using Boilerplate.Server.Shared.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
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

        services.AddDistributedMemoryCache(); // For ASP.NET Core Output Caching
        services.AddFusionCache(); // For actual backend project usages

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

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.ConfigureHttpClient(httpClient =>
            {
                httpClient.DefaultRequestVersion = HttpVersion.Version30;
                httpClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
            });

            // Turn on resilience by default
            // http.AddStandardResilienceHandler();

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

                metrics.AddMeter(ActivitySource.Current.Name);
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddProcessor<AppOpenTelemetryProcessor>()
                                .AddAspNetCoreInstrumentation(options =>
                                {
                                    // Filter out Blazor static files and health checks requests.
                                    string[] toBeIgnoredSegments = ["/health",
                                        "/alive",
                                        "/_content",
                                        "/_framework"];

                                    options.Filter = context =>
                                    {
                                        foreach (var segment in toBeIgnoredSegments)
                                        {
                                            if (context.Request.Path.StartsWithSegments(segment, StringComparison.OrdinalIgnoreCase))
                                                return false;
                                        }

                                        return true;
                                    };
                                })
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation(options => options.Filter = (providerName, command) => command?.CommandText?.Contains("Hangfire") is false /* Ignore Hangfire */)
                    .AddHangfireInstrumentation();

                tracing.AddSource(ActivitySource.Current.Name);
            })
            .ConfigureResource(resource =>
            {
                resource.AddAzureAppServiceDetector()
                    .AddAzureContainerAppsDetector()
                    .AddAzureVMDetector()
                    .AddContainerDetector()
                    .AddHostDetector()
                    .AddOperatingSystemDetector()
                    .AddProcessDetector()
                    .AddProcessRuntimeDetector();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]) is false;

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        //#if (appInsights == true)
        var appInsightsConnectionString = string.IsNullOrWhiteSpace(builder.Configuration["ApplicationInsights:ConnectionString"]) is false ? builder.Configuration["ApplicationInsights:ConnectionString"] : null;

        if (appInsightsConnectionString is not null)
        {
            builder.Services.AddOpenTelemetry().UseAzureMonitor(options =>
            {
                builder.Configuration.Bind("ApplicationInsights", options);
            }).AddAzureMonitorProfiler();
        }
        //#endif

        return builder;
    }

    public static IHealthChecksBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddOutputCache(configureOptions: static caching =>
            caching.AddPolicy("HealthChecks",
            build: static policy => policy.Expire(TimeSpan.FromSeconds(10))));

        return builder.Services.AddHealthChecks()
            .AddDiskStorageHealthCheck(opt => opt.AddDrive(Path.GetPathRoot(Directory.GetCurrentDirectory())!, minimumFreeMegabytes: 5 * 1024), tags: ["live"]);
    }
}
