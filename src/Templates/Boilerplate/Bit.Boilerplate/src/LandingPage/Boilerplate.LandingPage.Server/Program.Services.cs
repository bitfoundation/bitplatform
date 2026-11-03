//+:cnd:noEmit
using System.IO.Compression;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.ResponseCompression;
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

            services.AddLandingPageClientServices(configuration);
            services.AddScoped<ISiteAddress, ConfiguredSiteAddress>();

            services.AddRazorComponents()
                .AddInteractiveServerComponents()
                .AddInteractiveWebAssemblyComponents();

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

            services.AddLandingEmail(configuration, settings);
            services.AddLandingWaitlist();
            services.AddLandingChat(settings);
            services.AddLandingStoreListings();
        }
    }
}
