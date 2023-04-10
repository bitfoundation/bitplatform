using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Bit.Websites.Platform.Api;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static void AddSwaggerGen(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.OperationFilter<ODataOperationFilter>();
        });
    }

    public static void AddHealthChecks(this IServiceCollection services, IWebHostEnvironment env, IConfiguration configuration)
    {
        var appsettings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>();

        var healthCheckSettings = appsettings.HealthCheckSettings;

        if (healthCheckSettings.EnableHealthChecks is false)
            return;

        services.AddHealthChecksUI(setupSettings: setup =>
        {
            setup.AddHealthCheckEndpoint("TodoHealthChecks", env.IsDevelopment() ? "https://localhost:5001/healthz" : "/healthz");
        }).AddInMemoryStorage();

        var healthChecksBuilder = services.AddHealthChecks()
            .AddProcessAllocatedMemoryHealthCheck(maximumMegabytesAllocated: 6 * 1024)
            .AddDiskStorageHealthCheck(opt =>
                opt.AddDrive(Path.GetPathRoot(Directory.GetCurrentDirectory()), minimumFreeMegabytes: 5 * 1024));
    }
}
