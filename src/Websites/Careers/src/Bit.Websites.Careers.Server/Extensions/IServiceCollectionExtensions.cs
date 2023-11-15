using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.OpenApi.Models;
using Bit.Websites.Careers.Server;
using Bit.Websites.Careers.Client.Services.HttpMessageHandlers;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static void AddBlazor(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped(sp =>
        {
            IHttpClientFactory httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            return httpClientFactory.CreateClient("BlazorHttpClient");
            // This registers HttpClient for pre rendering & blazor server only, so to use http client to call 3rd party apis and other use cases,
            // either use services.AddHttpClient("NamedHttpClient") or services.AddHttpClient<TypedHttpClient>();
        });

        // In the Pre-Rendering mode, the configured HttpClient will use the access_token provided by the cookie in the request, so the pre-rendered content would be fitting for the current user.
        services.AddHttpClient("BlazorHttpClient")
            .AddHttpMessageHandler(sp => new RetryDelegatingHandler())
            .AddHttpMessageHandler(sp => new ExceptionDelegatingHandler())
            .ConfigurePrimaryHttpMessageHandler<HttpClientHandler>()
            .ConfigureHttpClient((sp, httpClient) =>
            {
                Uri.TryCreate(configuration.GetApiServerAddress(), UriKind.RelativeOrAbsolute, out var apiServerAddress);

                if (apiServerAddress!.IsAbsoluteUri is false)
                {
                    apiServerAddress = new Uri($"{sp.GetRequiredService<IHttpContextAccessor>().HttpContext!.Request.GetBaseUrl()}{apiServerAddress}");
                }

                httpClient.BaseAddress = apiServerAddress;
            });

        services.AddScoped<LazyAssemblyLoader>();

        services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents();
    }

    public static void AddSwaggerGen(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "Bit.Websites.Careers.Server.xml"));
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "Bit.Websites.Careers.Shared.xml"));
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
