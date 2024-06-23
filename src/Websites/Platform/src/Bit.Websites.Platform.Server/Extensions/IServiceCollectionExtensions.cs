using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.OpenApi.Models;
using Bit.Websites.Platform.Server;
using Bit.Websites.Platform.Client.Services.HttpMessageHandlers;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static void AddBlazor(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient(sp =>
        {
            Uri.TryCreate(configuration.GetApiServerAddress(), UriKind.RelativeOrAbsolute, out var apiServerAddress);

            if (apiServerAddress!.IsAbsoluteUri is false)
            {
                apiServerAddress = new Uri($"{sp.GetRequiredService<IHttpContextAccessor>().HttpContext!.Request.GetBaseUrl()}{apiServerAddress}");
            }

            return new HttpClient(sp.GetRequiredService<RequestHeadersDelegationHandler>())
            {
                BaseAddress = apiServerAddress
            };
        });

        services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents();

        services.AddMvc();
    }

    public static void AddSwaggerGen(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "Bit.Websites.Platform.Server.xml"));
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "Bit.Websites.Platform.Shared.xml"));
        });
    }

    public static void AddHealthChecks(this IServiceCollection services, IWebHostEnvironment env, IConfiguration configuration)
    {
        var appSettings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>()!;

        var healthCheckSettings = appSettings.HealthCheckSettings;

        if (healthCheckSettings.EnableHealthChecks is false)
            return;

        services.AddHealthChecksUI(setupSettings: setup =>
        {
            setup.AddHealthCheckEndpoint("WebHealthChecks", env.IsDevelopment() ? "https://localhost:5051/healthz" : "/healthz");
        }).AddInMemoryStorage();

        var healthChecksBuilder = services.AddHealthChecks()
            .AddProcessAllocatedMemoryHealthCheck(maximumMegabytesAllocated: 6 * 1024)
            .AddDiskStorageHealthCheck(opt =>
                opt.AddDrive(Path.GetPathRoot(Directory.GetCurrentDirectory())!, minimumFreeMegabytes: 5 * 1024));
    }
}
