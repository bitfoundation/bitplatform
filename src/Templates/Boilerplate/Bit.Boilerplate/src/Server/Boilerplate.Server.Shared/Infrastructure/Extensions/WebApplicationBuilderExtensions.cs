//+:cnd:noEmit
using System.Net;
using System.IO.Compression;
using System.Diagnostics.Metrics;
//#if (appInsights == true)
using Azure.Monitor.OpenTelemetry.Exporter;
//#endif
using Boilerplate.Server.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;
using Boilerplate.Server.Shared.Infrastructure.Services;
//#if (redis == true)
using ZiggyCreatures.Caching.Fusion.Locking.Distributed.Redis;
//#endif

namespace Microsoft.Extensions.Hosting;

public static class WebApplicationBuilderExtensions
{
    extension<TBuilder>(TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        public TBuilder AddServerSharedServices()
        {
            var services = builder.Services;
            var configuration = builder.Configuration;

            services.AddSharedProjectServices(configuration);

            builder.AddServiceDefaults();

            ServerSharedSettings settings = new();
            configuration.Bind(settings);
            services.AddSingleton(sp =>
            {
                return settings;
            });

            services.AddOutputCache(options =>
            {
                options.AddPolicy("AppResponseCachePolicy", policy =>
                {
                    var policyBuilder = policy.AddPolicy<AppResponseCachePolicy>();
                }, excludeDefaultPolicy: true);
            });
            if (settings.ResponseCaching?.EnableCdnEdgeCaching is true)
            {
                services.AddSingleton<AspNetCore.Antiforgery.IAntiforgery, SharedResponseCacheCompatibleAntiforgery>();
            }

            //#if(redis == true)
            // Add default Redis connection for Hangfire, SignalR backplane, and distributed locking (persistence Redis with AOF)
            builder.AddKeyedRedisClient("redis-persistent", settings => settings.DisableTracing = true);

            // Add optional Redis connection for caching (ephemeral Redis without persistence)
            builder.AddKeyedRedisClient("redis-cache", settings => settings.DisableTracing = true /*FusionCache is already handling cache traces*/);
            //#endif

            services
                //#if (redis == true)
                .AddFusionCacheRedisDistributedLocker()
                .AddFusionCacheStackExchangeRedisBackplane()
                .ConfigureRedisOptions()
                //#endif
                .AddFusionCache()
                .AsHybridCache()
                .WithRegisteredMemoryCache()
                //#if (redis == true)
                .WithRegisteredBackplane()
                .WithRegisteredDistributedCache()
                .WithRegisteredDistributedLocker()
                //#endif
                .WithDefaultEntryOptions(options => options.Size = 1)
                // Auto-clone cached objects to avoid further issues after scaling out and switching to distributed caching.
                .WithOptions(options => options.DefaultEntryOptions.EnableAutoClone = true)
                .WithSerializer(new FusionCacheSystemTextJsonSerializer())
                .WithCacheKeyPrefix("Boilerplate:Cache:");

            services.AddFusionOutputCache(); // For ASP.NET Core Output Caching with FusionCache

            // Registering Microsoft's IDistributedCache here doesn't mean you have to use it in your code. It's only for libraries that might rely on it.
            //#if(redis == true)
            services.AddStackExchangeRedisCache(_ => { });
            //#else
            services.AddDistributedMemoryCache();
            //#endif

            services.AddHttpContextAccessor();

            services.AddResponseCompression(opts =>
            {
                opts.EnableForHttps = true;
                opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/octet-stream"]).ToArray();
                opts.Providers.Add<BrotliCompressionProvider>();
                opts.Providers.Add<GzipCompressionProvider>();
            })
                .Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest)
                .Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);

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
        private TBuilder AddServiceDefaults()
        {
            builder.ConfigureOpenTelemetry();

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

        private TBuilder ConfigureOpenTelemetry()
        {
            builder.Logging.AddOpenTelemetry(options =>
            {
                options.IncludeScopes = true;
                options.IncludeFormattedMessage = true;
                builder.Configuration.Bind("Logging:OpenTelemetry", options);
            });

            if (builder.Environment.IsDevelopment() is false)
            {
                builder.Logging.AddSampler<AppLoggingSampler>();
            }

            builder.Services.AddOpenTelemetry()
                .WithMetrics(metrics =>
                {
                    metrics.AddAspNetCoreInstrumentation()
                        .AddFusionCacheInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation();

                    metrics.AddMeter(Meter.Current.Name)
                        .AddMeter("Experimental.Microsoft.Extensions.AI");
                })
                .WithTracing(tracing =>
                {
                    tracing.AddSource(ActivitySource.Current.Name)
                        .AddSource("Experimental.Microsoft.Extensions.AI")
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
                        .AddFusionCacheInstrumentation()
                        .AddEntityFrameworkCoreInstrumentation(options => options.Filter = (providerName, command) => command?.CommandText?.Contains("Hangfire") is false /* Ignore Hangfire */)
                        .AddHangfireInstrumentation();
                })
                .ConfigureResource(resource =>
                {
                    resource
                        .AddAzureAppServiceDetector()
                        .AddAzureContainerAppsDetector()
                        .AddAzureVMDetector()
                        .AddContainerDetector()
                        .AddHostDetector()
                        .AddOperatingSystemDetector()
                        .AddProcessDetector()
                        .AddProcessRuntimeDetector()
                        .AddService(builder.Environment.ApplicationName);
                });

            builder.AddOpenTelemetryExporters();

            return builder;
        }

        private TBuilder AddOpenTelemetryExporters()
        {
            var useOtlpExporter = string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]) is false
                || string.IsNullOrEmpty(builder.Configuration["OTEL_EXPORTER_OTLP_LOGS_ENDPOINT"]) is false;

            if (useOtlpExporter)
            {
                builder.Services
                    .AddOpenTelemetry()
                    .UseOtlpExporter();
            }

            //#if (appInsights == true)
            var appInsightsConnectionString = string.IsNullOrWhiteSpace(builder.Configuration["ApplicationInsights:ConnectionString"]) is false ? builder.Configuration["ApplicationInsights:ConnectionString"] : null;

            if (appInsightsConnectionString is not null)
            {
                builder.Services.AddOpenTelemetry().UseAzureMonitorExporter(options =>
                {
                    builder.Configuration.Bind("ApplicationInsights", options);
                });
            }
            //#endif

            return builder;
        }

        public IHealthChecksBuilder AddDefaultHealthChecks()
        {
            builder.Services.AddOutputCache(configureOptions: static caching =>
                caching.AddPolicy("HealthChecks",
                build: static policy => policy.Expire(TimeSpan.FromSeconds(10))));

            return builder.Services.AddHealthChecks()
                .AddDiskStorageHealthCheck(options => options.AddDrive(Path.GetPathRoot(Directory.GetCurrentDirectory())!, minimumFreeMegabytes: 5 * 1024), name: "binStorage", tags: ["live"]);
        }
    }
}
