//+:cnd:noEmit
using System.Net;
using System.IO.Compression;
using Bit.Belemetry;
using Boilerplate.Server.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;
using Boilerplate.Server.Shared.Infrastructure.Services;
using Boilerplate.Shared.Features.Diagnostic;
//#if (redis == true)
using Microsoft.Extensions.Diagnostics.HealthChecks;
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
            if (settings.ResponseCaching?.EnableCdnEdgeCaching is not false || settings.ResponseCaching?.EnableOutputCaching is not false)
            {
                services.AddSingleton<AspNetCore.Antiforgery.IAntiforgery, SharedResponseCacheCompatibleAntiforgery>();
            }

            //#if(redis == true)
            // Add optional Redis connection for caching (ephemeral Redis without persistence)
            builder.AddKeyedRedisClient("redis-cache", settings => settings.DisableTracing = true /*FusionCache is already handling cache traces*/);

            // Losing the cache only costs speed (FusionCache keeps serving from memory), so it degrades the instance instead of draining it.
            // Aspire registers the check without tags, so its category is added here too (See HealthCheckCategories).
            services.PostConfigure<HealthCheckServiceOptions>(options =>
            {
                var redisCacheCheck = options.Registrations.SingleOrDefault(r => r.Name is "StackExchange.Redis_redis-cache");
                redisCacheCheck?.FailureStatus = HealthStatus.Degraded;
                redisCacheCheck?.Timeout = TimeSpan.FromSeconds(5);
                redisCacheCheck?.Tags.Add(HealthCheckCategories.Platform);
            });
            //#endif

            services
                //#if (redis == true)
                //#if (IsInsideProjectTemplate == true)
                /*
                //#endif
                .AddFusionCacheRedisDistributedLocker()
                .AddFusionCacheStackExchangeRedisBackplane()
                .ConfigureRedisOptions()
                //#if (IsInsideProjectTemplate == true)
                */
                //#endif
                //#endif
                .AddFusionCache()
                .AsHybridCache()
                .WithRegisteredMemoryCache()
                //#if (redis == true)
                //#if (IsInsideProjectTemplate == true)
                /*
                //#endif
                .WithRegisteredBackplane()
                .WithRegisteredDistributedCache()
                .WithRegisteredDistributedLocker()
                //#if (IsInsideProjectTemplate == true)
                */
                //#endif
                //#endif
                .WithDefaultEntryOptions(options => options.Size = AppMemoryCache.EstimatedEntrySizeInBytes)
                // Auto-clone hands out a copy instead of the cached instance, so code that mutates what it reads from the cache
                // breaks here the same way it would once you scale out and values start coming back deserialized from L2.
                // It costs a serialize/deserialize per read though (including every output cache hit, since FusionOutputCacheStore
                // shares these options), so it stays on in development to surface those bugs and off everywhere else.
                .WithOptions(options => options.DefaultEntryOptions.EnableAutoClone = builder.Environment.IsDevelopment())
                .WithSystemTextJsonSerializer()
                .WithCacheKeyPrefix("Boilerplate:Cache:");

            services.AddFusionOutputCache(); // For ASP.NET Core Output Caching with FusionCache

            // Registering Microsoft's IDistributedCache here doesn't mean you have to use it in your code. It's only for libraries that might rely on it.
            //#if (redis == true)
            //#if (IsInsideProjectTemplate == true)
            /*
            //#endif
            services.AddStackExchangeRedisCache(_ => { });
            //#if (IsInsideProjectTemplate == true)
            */
            //#endif
            //#else
            services.AddDistributedMemoryCache();
            //#endif

            services.AddHttpContextAccessor();

            // [mirror] response compression options - keep in sync with:
            // - src/LandingPage/Boilerplate.LandingPage.Server/Program.Services.cs
            services.AddResponseCompression(opts =>
            {
                opts.EnableForHttps = true;
                opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/octet-stream", "image/svg+xml"]).ToArray();
                //#if (framework == 'net11.0')
                opts.Providers.Add<ZstandardCompressionProvider>();
                //#endif
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
        /// Adds common services for API: service discovery, resilience, health checks, and telemetry.
        /// This project should be referenced by each service project in your solution.
        /// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
        /// </summary>
        private TBuilder AddServiceDefaults()
        {
            builder.ConfigureTelemetry();

            builder.Services.AddServiceDiscovery();

            builder.Services.ConfigureHttpClientDefaults(http =>
            {
                // HTTP/2 wherever the server offers it, and HTTP/1.1 elsewhere. OrHigher would mean HTTP/2 or nothing, which
                // fails on plain http, where Kestrel speaks HTTP/1.1 only (Server.Web's calls to a standalone api inside
                // Compose), and on https servers without HTTP/2, such as appleid.apple.com.
                http.ConfigureHttpClient(httpClient =>
                {
                    httpClient.DefaultRequestVersion = HttpVersion.Version20;
                    httpClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
                });

                // Turn on resilience by default, but never replay an unsafe method. A retried POST is a second
                // attempt at something the server may already have done: an authorization code redeemed twice reads
                // as code reuse, a captcha token verified twice comes back as a duplicate, a push notification sent
                // twice arrives twice. Retrying one is worth asking for per client (and a job-level retry, as the
                // Cloudflare purge uses, is usually the better place for it); losing one silently is not.
                http.AddStandardResilienceHandler(options => options.Retry.DisableForUnsafeHttpMethods());

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

        // [mirror] telemetry setup - keep in sync with:
        // - src/LandingPage/Boilerplate.LandingPage.Server/Infrastructure/Extensions/WebApplicationBuilderExtensions.cs (AddLandingTelemetry)
        private TBuilder ConfigureTelemetry()
        {
            if (builder.Environment.IsDevelopment() is false)
            {
                builder.Logging.AddSampler<AppLoggingSampler>();
            }

            builder.Services.AddBitBelemetryServices(options =>
            {
                options.Server.CollectDotnetTelemetry = true;
                options.Server.IgnoredRequestPaths.AddRange(["/health", "/alive", "/_content", "/_framework"]);

                options.AddTraceSources("Experimental.Microsoft.Extensions.AI", "SmsBridge", FusionCacheDiagnostics.ActivitySourceName)
                    .AddMeters("Experimental.Microsoft.Extensions.AI", FusionCacheDiagnostics.MeterName);

                options.Traces.IgnoredActivityNames.Add("Microsoft.AspNetCore.Components.HandleEvent");

                options.AddProcessor(new AppBelemetryProcessor(builder.Environment));
            });

            return builder;
        }

        public IHealthChecksBuilder AddDefaultHealthChecks()
        {
            builder.Services.AddOutputCache(configureOptions: static caching =>
                caching.AddPolicy("HealthChecks",
                build: static policy => policy.Expire(TimeSpan.FromSeconds(10)).SetVaryByQuery([])));

            return builder.Services.AddHealthChecks()
                .AddDiskStorageHealthCheck(options => options.AddDrive(Path.GetPathRoot(Directory.GetCurrentDirectory())!, minimumFreeMegabytes: 2 * 1024), name: "binStorage", tags: ["live", HealthCheckCategories.Platform]);
        }
    }
}
