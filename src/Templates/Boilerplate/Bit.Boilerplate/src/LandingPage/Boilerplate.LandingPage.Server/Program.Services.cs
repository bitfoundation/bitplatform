//+:cnd:noEmit
using System.IO.Compression;
using System.Threading.RateLimiting;
using Bit.Belemetry;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Options;
using Boilerplate.LandingPage.Server.Features.Chat;
using Boilerplate.LandingPage.Server.Features.Email;
using Boilerplate.LandingPage.Server.Features.Waitlist;
using Boilerplate.LandingPage.Server.Features.StoreListing;

namespace Boilerplate.LandingPage.Server;

public static partial class Program
{
    extension(WebApplicationBuilder builder)
    {
        public void AddLandingPageServices()
        {
            var services = builder.Services;
            var configuration = builder.Configuration;

            LandingServerSettings settings = new();
            configuration.Bind(settings);
            services.AddSingleton(settings);

            services.AddSingleton<IValidateOptions<LandingServerSettings>, LandingServerSettingsValidator>();
            services.AddOptions<LandingServerSettings>()
                .Bind(configuration)
                .ValidateOnStart();

            builder.AddLandingOpenTelemetry();

            // The client's services, for prerendering; the server registers its direct clients after it
            services.AddLandingPageClientServices(configuration);

            // The page's script gets the browser's endpoint too, as it can't post to the server's gRPC one. Without the OTEL_
            // variables, which Aspire sets, the server posts there as well, rather than to Belemetry's localhost:4318.
            if (configuration.GetBrowserOtlpEndpoint() is { } browserOtlp)
            {
                var serverEndpointConfigured = string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]) is false;
                services.Configure<BelemetryOptions>(options => options.AddOtlpExporter(otlp =>
                {
                    otlp.BrowserEndpoint = browserOtlp.Endpoint;
                    if (serverEndpointConfigured is false)
                    {
                        otlp.Endpoint = browserOtlp.Endpoint;
                    }
                }));
            }

            services.AddRazorComponents()
                .AddInteractiveServerComponents()
                .AddInteractiveWebAssemblyComponents();

            // Pages set no antiforgery cookie, so a CDN can cache them
            services.AddSingleton<IAntiforgery, LandingAntiforgery>();

            services.AddHttpContextAccessor();
            services.TryAddSingleton(TimeProvider.System);

            // [mirror] response compression options - keep in sync with:
            // - src/Server/Boilerplate.Server.Shared/Infrastructure/Extensions/WebApplicationBuilderExtensions.cs
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

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddPolicy(LandingRateLimits.Waitlist, context => RateLimitPartition.GetSlidingWindowLimiter(context.GetClientIp(), _ => new()
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(10),
                    SegmentsPerWindow = 5,
                    QueueLimit = 0
                }));

                options.AddPolicy(LandingRateLimits.Chat, context => RateLimitPartition.GetSlidingWindowLimiter(context.GetClientIp(), _ => new()
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(10),
                    SegmentsPerWindow = 5,
                    QueueLimit = 0
                }));
            });

            services.AddOutputCache(options =>
            {
                // The addresses in these files come from the request's host
                options.AddPolicy(LandingOutputCaches.SeoFiles, policy => policy.Expire(TimeSpan.FromHours(1)).SetVaryByHost(true));
                // A failure isn't stored, so a recovery shows on the next probe, while a healthy mail server isn't signed in to on every probe
                options.AddPolicy(LandingOutputCaches.Health, policy => policy.Expire(TimeSpan.FromMinutes(5)).SetVaryByQuery([]));
            });

            services.AddLandingEmail(configuration, settings);
            services.AddLandingWaitlist();
            services.AddLandingChat(settings);
            services.AddLandingStoreListings();
        }
    }
}
