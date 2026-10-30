using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Boilerplate.Workbench.Infrastructure.Extensions;

public static class IWorkbenchServiceCollectionExtensions
{
    public const string ScreenshotsRateLimitPolicy = "screenshots";

    extension(IServiceCollection services)
    {
        public void AddWorkbenchServices(IConfiguration configuration)
        {
            services.AddScoped<WorkbenchScenarioState>();
            services.AddSingleton<WorkbenchScreenshotService>();
            services.AddScoped<PosDashboardDataService>();
            services.AddScoped<BuildingsService>();
            services.AddScoped<AssistantService>();

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddPolicy(ScreenshotsRateLimitPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = configuration.GetValue("Screenshots:PermitsPerMinutePerClient", 30),
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
            });
        }
    }
}
